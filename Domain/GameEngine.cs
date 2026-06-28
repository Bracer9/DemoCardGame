namespace TinyPixelFights.Domain;

public sealed class GameEngineContext
{
    private readonly GameEngine _engine;

    internal GameEngineContext(GameEngine engine, GameState state)
    {
        _engine = engine;
        State = state;
    }

    public GameState State { get; }
    public bool Roll(double chance) => _engine.Roll(chance);
    public int Next(int maxValue) => _engine.Next(maxValue);
    public void Log(LocalizedText message, string tone = "neutral") => _engine.Log(State, message, tone);

    public int DealSkillDamage(
        CharacterState target,
        int amount,
        DamageType damageType,
        Guid sourceCharacterId,
        string effectId,
        bool receivesMagicPowerBonus = false) =>
        _engine.DealSkillDamage(State, target, amount, damageType, sourceCharacterId, effectId, receivesMagicPowerBonus);

    public int DealAbsoluteDamage(
        CharacterState target,
        int amount,
        Guid sourceCharacterId,
        string effectId) =>
        _engine.DealAbsoluteDamage(State, target, amount, sourceCharacterId, effectId);
}

public sealed record AttackResult(GameState State, CombatOutcome Outcome);

public sealed record CombatOutcome(
    Guid AttackerId,
    Guid DefenderId,
    int AttackDamage,
    int CounterDamage,
    DamageType AttackType,
    DamageType CounterType,
    int AttackShieldAbsorbed,
    int CounterShieldAbsorbed,
    int AttackShieldRemaining,
    int CounterShieldRemaining,
    IReadOnlyList<LocalizedText> Notes,
    IReadOnlyList<Guid> DefeatedCharacterIds);

public sealed class GameRuleException(LocalizedText error) : Exception(error.Key)
{
    public LocalizedText Error { get; } = error;
}

public sealed class GameEngine
{
    public const int MaxActionPoints = 5;
    public const int FirstShieldCost = 2;
    public const int ReinforcedShieldCost = 1;
    public const int FirstShieldValue = 2;
    public const int ReinforcedShieldBonus = 2;
    public const int MaxShieldDeploymentsPerTurn = 2;
    public const int CounterAttackPenalty = 1;
    private const int ShieldModifierPriority = 20;

    private readonly SkillRegistry _skills;
    private readonly Random _random = new();

    public GameEngine(SkillRegistry skills)
    {
        _skills = skills;
    }

    internal bool Roll(double chance) => _random.NextDouble() < chance;
    internal int Next(int maxValue) => _random.Next(maxValue);

    public GameState CreateGame()
    {
        var definitions = CharacterCatalog.All.OrderBy(_ => _random.Next()).ToList();
        if (definitions.Count % 2 != 0)
            throw new InvalidOperationException("Character count must be divisible between two players.");
        if (definitions.Count(definition => definition.Cost == 1) < 2)
            throw new InvalidOperationException("At least two cost-one characters are required.");

        var handSize = definitions.Count / 2;
        var player1Definitions = definitions.Take(handSize).ToList();
        var player2Definitions = definitions.Skip(handSize).ToList();
        EnsureCostOneOpeningHand(player1Definitions, player2Definitions);
        EnsureCostOneOpeningHand(player2Definitions, player1Definitions);

        var player1 = new PlayerState { Id = Guid.NewGuid(), Name = "player.1" };
        var player2 = new PlayerState { Id = Guid.NewGuid(), Name = "player.2" };

        AddOpeningHand(player1, player1Definitions);
        AddOpeningHand(player2, player2Definitions);

        var state = new GameState
        {
            ActivePlayerId = Roll(0.5) ? player1.Id : player2.Id,
            ActionPoints = MaxActionPoints
        };
        state.Players.Add(player1);
        state.Players.Add(player2);
        GrantStartingMagicPower(player1);
        GrantStartingMagicPower(player2);
        Log(state, L10n.Text("log.firstPlayer", ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
        Log(state, L10n.Text("log.firstTurnNoStartEffects"), "system");
        return state;
    }

    private static void GrantStartingMagicPower(PlayerState player)
    {
        var oracle = player.Characters.FirstOrDefault(character =>
            character.IsAlive && character.Definition.Key == "oracle");
        if (oracle is null)
            return;

        foreach (var character in player.Characters.Where(character => character.IsAlive))
        {
            if (character.Statuses.Any(status => status.Id == "magic-power" && !status.Expired))
                continue;

            character.Statuses.Add(new MagicPowerStatus(oracle.Id));
        }
    }

    private void EnsureCostOneOpeningHand(
        List<CharacterDefinition> recipient,
        List<CharacterDefinition> donor)
    {
        if (recipient.Any(definition => definition.Cost == 1))
            return;

        var donorCostOneIndices = donor
            .Select((definition, index) => (definition, index))
            .Where(entry => entry.definition.Cost == 1)
            .Select(entry => entry.index)
            .ToArray();
        if (donorCostOneIndices.Length < 2)
            throw new InvalidOperationException("Cost-one characters cannot be distributed to both players.");

        var recipientSwapIndices = recipient
            .Select((definition, index) => (definition, index))
            .Where(entry => entry.definition.Cost != 1)
            .Select(entry => entry.index)
            .ToArray();
        var donorIndex = donorCostOneIndices[_random.Next(donorCostOneIndices.Length)];
        var recipientIndex = recipientSwapIndices[_random.Next(recipientSwapIndices.Length)];
        (recipient[recipientIndex], donor[donorIndex]) = (donor[donorIndex], recipient[recipientIndex]);
    }

    private static void AddOpeningHand(PlayerState player, IReadOnlyList<CharacterDefinition> definitions)
    {
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            player.Characters.Add(new CharacterState
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                Slot = index,
                Definition = definition,
                CurrentHp = definition.MaxHp
            });
        }
    }

    public AttackResult Attack(GameState state, Guid attackerId, Guid defenderId)
    {
        EnsurePlaying(state);
        var attacker = state.FindCharacter(attackerId);
        var defender = state.FindCharacter(defenderId);
        ValidateAttack(state, attacker, defender);

        var context = new GameEngineContext(this, state);
        var attackerSkill = _skills.Get(attacker.Definition.SkillId);
        attackerSkill.OnAttackDeclared(context, attacker, defender);
        var counterDebuffReduction = GetCounterDebuffReduction(defender);
        var counterDamageBeforeShieldResolution = GetCounterAttack(defender);

        var attackPacket = new DamagePacket
        {
            SourceCharacter = attacker,
            TargetCharacter = defender,
            DamageType = attacker.Definition.AttackType,
            Source = DamageSource.ActiveAttack,
            Amount = GetActiveAttack(attacker),
            ReceivesMagicPowerBonus = attacker.Definition.AttackType == DamageType.Magical
        };
        var counterPacket = new DamagePacket
        {
            SourceCharacter = defender,
            TargetCharacter = attacker,
            DamageType = defender.Definition.AttackType,
            Source = DamageSource.CounterAttack,
            Amount = counterDamageBeforeShieldResolution
        };
        if (counterDebuffReduction > 0)
            counterPacket.Notes.Add(L10n.Text("note.complacencyReduction",
                ("character", L10n.Character(defender.Definition.Key)),
                ("amount", L10n.Raw(counterDebuffReduction))));
        ConsumeCounterDebuff(defender);

        ModifyDamage(state, attackPacket);
        ModifyDamage(state, counterPacket);

        attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - counterPacket.Amount);
        defender.CurrentHp = Math.Max(0, defender.CurrentHp - attackPacket.Amount);

        var resolvedCollateral = attackPacket.Collateral
            .Concat(counterPacket.Collateral)
            .Select(collateral => (Collateral: collateral, Packet: ResolveCollateralDamage(state, collateral)))
            .ToArray();

        attacker.HasActed = true;
        state.ActionPoints -= attacker.Definition.Cost;
        state.ActionsTakenThisTurn++;

        foreach (var note in attackPacket.Notes.Concat(counterPacket.Notes).Concat(resolvedCollateral.SelectMany(item => item.Packet.Notes)))
            Log(state, note, "status");

        Log(state, L10n.Text("log.exchange",
                ("attacker", L10n.Character(attacker.Definition.Key)),
                ("defender", L10n.Character(defender.Definition.Key)),
                ("attackDamage", L10n.Raw(attackPacket.Amount)),
                ("attackType", L10n.Damage(attackPacket.DamageType)),
                ("attackDefenseReduced", L10n.Raw(attackPacket.DefenseReduced)),
                ("attackShieldAbsorbed", L10n.Raw(attackPacket.ShieldAbsorbed)),
                ("counterDamage", L10n.Raw(counterPacket.Amount)),
                ("counterType", L10n.Damage(counterPacket.DamageType)),
                ("counterDefenseReduced", L10n.Raw(counterPacket.DefenseReduced)),
                ("counterShieldAbsorbed", L10n.Raw(counterPacket.ShieldAbsorbed))),
            attackPacket.DamageType == DamageType.Physical ? "physical" : "magic");

        foreach (var item in resolvedCollateral)
            Log(state, L10n.Text("log.collateralDamage",
                ("effect", L10n.Status(item.Collateral.EffectId)),
                ("character", L10n.Character(item.Packet.TargetCharacter.Definition.Key)),
                ("amount", L10n.Raw(item.Packet.Amount))), "physical");

        var exchange = new AttackExchange(
            attacker,
            defender,
            attackPacket.Amount,
            counterPacket.Amount);
        attackerSkill.OnAfterExchange(context, attacker, exchange);

        var defeated = ResolveDefeats(state);
        EvaluateGameEnd(state);

        return new AttackResult(state, new CombatOutcome(
            attacker.Id,
            defender.Id,
            attackPacket.Amount,
            counterPacket.Amount,
            attackPacket.DamageType,
            counterPacket.DamageType,
            attackPacket.ShieldAbsorbed,
            counterPacket.ShieldAbsorbed,
            state.FindOwner(defender).SharedShield,
            state.FindOwner(attacker).SharedShield,
            attackPacket.Notes.Concat(counterPacket.Notes).ToArray(),
            defeated));
    }

    public void DeployShield(GameState state)
    {
        EnsurePlaying(state);
        var player = state.ActivePlayer;
        if (player.ShieldDeploymentsThisTurn >= MaxShieldDeploymentsPerTurn)
            throw new GameRuleException(L10n.Text("error.shieldMaxed"));
        var isReinforcing = CanReinforceShield(player);
        var shieldCost = GetShieldCost(player.ShieldDeploymentsThisTurn, player.SharedShield);
        if (state.ActionPoints < shieldCost)
            throw new GameRuleException(L10n.Text("error.notEnoughAp"));

        state.ActionPoints -= shieldCost;
        player.ShieldDeploymentsThisTurn++;
        player.SharedShield = isReinforcing
            ? player.SharedShield + ReinforcedShieldBonus
            : FirstShieldValue;
        var counterReduction = isReinforcing ? 1 : 0;
        foreach (var character in player.Characters.Where(character => character.IsAlive))
        {
            character.Statuses.RemoveAll(status => status.Id == "shield-complacency");
            if (counterReduction > 0)
                character.Statuses.Add(new ShieldComplacencyStatus(player.Id, counterReduction));
        }
        var logKey = isReinforcing ? "log.shieldReinforced" : "log.shieldDeployed";
        Log(state, L10n.Text(logKey,
            ("player", L10n.Player(player.Name)),
            ("shield", L10n.Raw(player.SharedShield)),
            ("reduction", L10n.Raw(counterReduction))), "shield");
    }

    public static bool CanReinforceShield(PlayerState player) =>
        player.ShieldDeploymentsThisTurn > 0 && player.SharedShield > 0;

    public static int GetShieldCost(int deploymentsThisTurn, int sharedShield) =>
        deploymentsThisTurn > 0 && sharedShield > 0 ? ReinforcedShieldCost : FirstShieldCost;

    public void EndTurn(GameState state)
    {
        EnsurePlaying(state);
        var context = new GameEngineContext(this, state);

        foreach (var character in state.ActivePlayer.Characters)
        {
            foreach (var status in character.Statuses.ToArray())
                status.OnTurnEnd(context, character);
            character.Statuses.RemoveAll(status => status.Expired);
        }

        state.ActivePlayerId = state.Opponent.Id;
        state.TurnNumber++;
        state.ActionPoints = MaxActionPoints;
        state.ActionsTakenThisTurn = 0;

        if (state.ActivePlayer.SharedShield > 0)
            Log(state, L10n.Text("log.shieldExpired", ("player", L10n.Player(state.ActivePlayer.Name))), "shield");
        state.ActivePlayer.SharedShield = 0;
        state.ActivePlayer.ShieldDeploymentsThisTurn = 0;

        foreach (var character in state.ActivePlayer.Characters)
        {
            character.HasActed = false;
            character.GuardConsumed = false;
        }

        Log(state, L10n.Text("log.turnStart",
            ("turn", L10n.Raw(state.TurnNumber)),
            ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
        ProcessTurnStart(state);
        ResolveDefeats(state);
        EvaluateGameEnd(state);
    }

    public int GetActiveAttack(CharacterState character)
    {
        var damage = GetBaseAttack(character);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            damage = status.ModifyActiveAttack(damage);
        return Math.Max(0, damage);
    }

    public int GetBaseAttack(CharacterState character)
    {
        var attack = character.Definition.Attack;
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            attack = status.ModifyBaseAttack(attack);
        return Math.Max(0, attack);
    }

    public int GetCounterAttack(CharacterState character) =>
        Math.Max(0, GetBaseCounterAttack(GetBaseAttack(character)) - GetCounterDebuffReduction(character));

    public static int GetBaseCounterAttack(int attack) => Math.Max(1, attack - CounterAttackPenalty);

    public int GetPhysicalDefense(CharacterState character) =>
        character.Definition.PhysicalDefense;

    public int GetMagicalDefense(CharacterState character) =>
        character.Definition.MagicalDefense;

    public int GetDefense(CharacterState character, DamageType damageType) => damageType switch
    {
        DamageType.Physical => GetPhysicalDefense(character),
        DamageType.Magical => GetMagicalDefense(character),
        _ => 0
    };

    public static int GetCounterDebuffReduction(CharacterState character) =>
        character.Statuses.OfType<ShieldComplacencyStatus>()
            .Where(status => !status.Expired)
            .Select(status => status.Magnitude)
            .DefaultIfEmpty(0)
            .Max();

    private static void ConsumeCounterDebuff(CharacterState character)
    {
        foreach (var status in character.Statuses.OfType<ShieldComplacencyStatus>().Where(status => !status.Expired))
            status.Consume();
    }

    public CharacterSkill GetSkill(CharacterState character) =>
        _skills.Get(character.Definition.SkillId);

    private void ProcessTurnStart(GameState state)
    {
        var context = new GameEngineContext(this, state);

        foreach (var character in state.ActivePlayer.Characters.Where(character => character.IsAlive).ToArray())
        {
            foreach (var status in character.Statuses.ToArray())
                status.OnTurnStart(context, character);
            character.Statuses.RemoveAll(status => status.Expired);
        }

        ResolveDefeats(state);

        foreach (var character in state.ActivePlayer.Characters.Where(character => character.IsAlive).ToArray())
            _skills.Get(character.Definition.SkillId).OnTurnStart(context, character);
    }

    private void ModifyDamage(GameState state, DamagePacket packet)
    {
        var sourceOwner = state.FindOwner(packet.SourceCharacter);
        foreach (var skillOwner in sourceOwner.Characters.Where(character => character.IsAlive))
            _skills.Get(skillOwner.Definition.SkillId).ModifyOutgoingDamage(
                new GameEngineContext(this, state), skillOwner, packet);

        ApplyTypeDefense(packet);

        var targetOwner = state.FindOwner(packet.TargetCharacter);
        var incomingModifiers = targetOwner.Characters
            .Where(character => character.IsAlive)
            .OrderBy(character => _skills.Get(character.Definition.SkillId).IncomingModifierPriority)
            .ToArray();

        foreach (var skillOwner in incomingModifiers
                     .Where(character => _skills.Get(character.Definition.SkillId).IncomingModifierPriority < ShieldModifierPriority))
            _skills.Get(skillOwner.Definition.SkillId).ModifyIncomingDamage(
                new GameEngineContext(this, state), skillOwner, packet);

        ApplySharedShield(targetOwner, packet);

        foreach (var skillOwner in incomingModifiers
                     .Where(character => _skills.Get(character.Definition.SkillId).IncomingModifierPriority >= ShieldModifierPriority))
            _skills.Get(skillOwner.Definition.SkillId).ModifyIncomingDamage(
                new GameEngineContext(this, state), skillOwner, packet);

        packet.Amount = Math.Max(0, packet.Amount);
    }

    private void ApplyTypeDefense(DamagePacket packet)
    {
        if (packet.Amount <= 0)
            return;

        var defense = GetDefense(packet.TargetCharacter, packet.DamageType);
        if (defense > 0)
        {
            var reduced = Math.Min(packet.Amount, defense);
            packet.Amount -= reduced;
            packet.DefenseReduced += reduced;
            packet.Notes.Add(L10n.Text("note.typeDefense",
                ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
                ("damageType", L10n.Damage(packet.DamageType)),
                ("amount", L10n.Raw(reduced))));
            return;
        }

        if (defense < 0)
        {
            var increased = Math.Abs(defense);
            packet.Amount += increased;
            packet.DefenseReduced -= increased;
            packet.Notes.Add(L10n.Text("note.typeDefenseWeakness",
                ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
                ("damageType", L10n.Damage(packet.DamageType)),
                ("amount", L10n.Raw(increased))));
        }
    }

    private DamagePacket ResolveCollateralDamage(GameState state, CollateralDamage collateral)
    {
        var packet = new DamagePacket
        {
            SourceCharacter = collateral.Source,
            TargetCharacter = collateral.Target,
            DamageType = collateral.DamageType,
            Source = DamageSource.Skill,
            Amount = collateral.Amount
        };
        ModifyDamage(state, packet);
        collateral.Target.CurrentHp = Math.Max(0, collateral.Target.CurrentHp - packet.Amount);
        return packet;
    }

    private static void ApplySharedShield(PlayerState targetOwner, DamagePacket packet)
    {
        if (packet.Amount <= 0 || targetOwner.SharedShield <= 0)
            return;

        var absorbed = Math.Min(packet.Amount, targetOwner.SharedShield);
        packet.Amount -= absorbed;
        packet.ShieldAbsorbed += absorbed;
        targetOwner.SharedShield -= absorbed;
        packet.Notes.Add(L10n.Text("note.shieldAbsorb",
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("damageType", L10n.Damage(packet.DamageType)),
            ("amount", L10n.Raw(absorbed))));
    }

    internal int DealSkillDamage(
        GameState state,
        CharacterState target,
        int amount,
        DamageType damageType,
        Guid sourceCharacterId,
        string effectId,
        bool receivesMagicPowerBonus = false)
    {
        var source = state.FindCharacter(sourceCharacterId);
        var packet = new DamagePacket
        {
            SourceCharacter = source,
            TargetCharacter = target,
            DamageType = damageType,
            Source = DamageSource.Skill,
            Amount = amount,
            ReceivesMagicPowerBonus = receivesMagicPowerBonus
        };
        ModifyDamage(state, packet);
        target.CurrentHp = Math.Max(0, target.CurrentHp - packet.Amount);
        foreach (var note in packet.Notes)
            Log(state, note, "status");
        var effectArg = effectId is "burning" or "guard"
            ? L10n.Status(effectId)
            : L10n.Skill(effectId);
        Log(state, L10n.Text("log.effectDamage",
            ("effect", effectArg),
            ("source", L10n.Character(source.Definition.Key)),
            ("character", L10n.Character(target.Definition.Key)),
            ("amount", L10n.Raw(packet.Amount)),
            ("damageType", L10n.Damage(damageType))), damageType == DamageType.Physical ? "physical" : "magic");
        return packet.Amount;
    }

    internal int DealAbsoluteDamage(
        GameState state,
        CharacterState target,
        int amount,
        Guid sourceCharacterId,
        string effectId)
    {
        _ = state.FindCharacter(sourceCharacterId);
        var dealt = Math.Max(0, amount);
        target.CurrentHp = Math.Max(0, target.CurrentHp - dealt);
        Log(state, L10n.Text("log.effectDamage",
            ("effect", L10n.Skill(effectId)),
            ("source", L10n.Character(state.FindCharacter(sourceCharacterId).Definition.Key)),
            ("character", L10n.Character(target.Definition.Key)),
            ("amount", L10n.Raw(dealt)),
            ("damageType", L10n.Damage(DamageType.Absolute))), "skill");
        return dealt;
    }

    private static void ValidateAttack(GameState state, CharacterState attacker, CharacterState defender)
    {
        if (attacker.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.notActiveCharacter"));
        if (defender.PlayerId == state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.cannotAttackAlly"));
        if (!attacker.IsAlive || !defender.IsAlive)
            throw new GameRuleException(L10n.Text("error.defeatedCharacter"));
        if (attacker.HasActed)
            throw new GameRuleException(L10n.Text("error.alreadyActed"));
        if (attacker.Definition.Cost > state.ActionPoints)
            throw new GameRuleException(L10n.Text("error.notEnoughAp"));
    }

    private static void EnsurePlaying(GameState state)
    {
        if (state.Phase != GamePhase.Playing)
            throw new GameRuleException(L10n.Text("error.matchFinished"));
    }

    private IReadOnlyList<Guid> ResolveDefeats(GameState state)
    {
        var defeated = new List<Guid>();
        var newlyDefeated = new List<CharacterState>();
        foreach (var character in state.Players.SelectMany(player => player.Characters)
                     .Where(character => character.IsInBattle && character.CurrentHp <= 0))
        {
            if (character.CurrentHp != 0)
                character.CurrentHp = 0;
            character.Zone = CharacterZone.Defeated;
            character.HasActed = true;
            character.Statuses.Clear();
            if (!character.DefeatLogged)
            {
                character.DefeatLogged = true;
                defeated.Add(character.Id);
                newlyDefeated.Add(character);
                Log(state, L10n.Text("log.defeated",
                    ("character", L10n.Character(character.Definition.Key)),
                    ("characterId", L10n.Raw(character.Id))), "defeat");
            }
        }

        var context = new GameEngineContext(this, state);
        foreach (var defeatedCharacter in newlyDefeated)
        {
            var owner = state.FindOwner(defeatedCharacter);
            if (defeatedCharacter.Definition.Key == "oracle")
                RemoveMagicPowerFromOwner(owner, defeatedCharacter.Id);
            foreach (var ally in owner.Characters.Where(character => character.IsAlive))
                _skills.Get(ally.Definition.SkillId).OnAllyDefeated(context, ally, defeatedCharacter);
        }
        return defeated;
    }

    private static void RemoveMagicPowerFromOwner(PlayerState owner, Guid oracleId)
    {
        foreach (var ally in owner.Characters)
            ally.Statuses.RemoveAll(status => status.Id == "magic-power" && status.SourceCharacterId == oracleId);
    }

    private static void EvaluateGameEnd(GameState state)
    {
        var defeatedPlayers = state.Players.Where(player => player.IsDefeated).ToList();
        if (defeatedPlayers.Count == 0)
            return;

        state.Phase = GamePhase.Finished;
        if (defeatedPlayers.Count == 2)
        {
            state.IsDraw = true;
            state.WinnerPlayerId = null;
            return;
        }

        state.WinnerPlayerId = state.Players.Single(player => !player.IsDefeated).Id;
    }

    internal void Log(GameState state, LocalizedText message, string tone)
    {
        state.Log.Add(new GameLogEntry(++state.LogSequence, state.TurnNumber, message, tone));
        if (state.Log.Count > 80)
            state.Log.RemoveAt(0);
    }
}
