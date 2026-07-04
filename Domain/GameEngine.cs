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

    public int DealTraitDamage(
        CharacterState target,
        int amount,
        DamageType damageType,
        Guid sourceCharacterId,
        string effectId,
        bool receivesMagicPowerBonus = false) =>
        _engine.DealTraitDamage(State, target, amount, damageType, sourceCharacterId, effectId, receivesMagicPowerBonus);

    public int DealAbsoluteDamage(
        CharacterState target,
        int amount,
        Guid sourceCharacterId,
        string effectId) =>
        _engine.DealAbsoluteDamage(State, target, amount, sourceCharacterId, effectId);

    public int GetActiveAttack(CharacterState character) => _engine.GetActiveAttack(character);
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
    public const int InitialBattlePoints = 5;
    public const int MaxBattlePoints = 15;
    public const int BattlePointGainCapPerTurn = 3;
    public const int FirstRewardRound = 3;
    public const int RewardRoundInterval = 3;
    public const int RewardSkipBattlePoints = 1;
    private const string BpReasonTurnStart = "turn-start";
    private const string BpReasonBreakEnemyShield = "break-enemy-shield";
    private const string BpReasonOwnTurnEnemyHpDamage = "own-turn-enemy-hp-damage";
    private const string BpReasonShieldFullyDeployed = "shield-fully-deployed";
    private const string BpReasonRewardSkip = "reward-skip";
    private const string BpReasonRoleActionCleanse = "role-action-cleanse";
    private const string BpReasonRoleActionDarkPact = "role-action-dark-pact";
    private const string BpSpendReasonRewardPurchase = "reward-purchase";
    private const string BpSpendReasonRewardReroll = "reward-reroll";
    private readonly TraitRegistry _traits;
    private readonly RoleActionRegistry _roleActions;
    private readonly Random _random = new();

    public GameEngine(TraitRegistry traits, RoleActionRegistry roleActions)
    {
        _traits = traits;
        _roleActions = roleActions;
    }

    internal bool Roll(double chance) => _random.NextDouble() < chance;
    internal int Next(int maxValue) => _random.Next(maxValue);

    public GameState CreateGame()
    {
        var player1 = new PlayerState { Id = Guid.NewGuid(), Name = "player.1" };
        var player2 = new PlayerState { Id = Guid.NewGuid(), Name = "player.2" };

        InitializeBattlePoints(player1);
        InitializeBattlePoints(player2);
        var firstPlayerId = Roll(0.5) ? player1.Id : player2.Id;

        var state = new GameState
        {
            ActivePlayerId = firstPlayerId,
            ActionPoints = MaxActionPoints,
            Phase = GamePhase.HeroDraft,
            OpeningFirstPlayerId = firstPlayerId
        };
        state.Players.Add(player1);
        state.Players.Add(player2);
        OpenOpeningHeroDrafts(state);
        return state;
    }

    public GameState CreateTestGame()
    {
        var player1 = new PlayerState { Id = Guid.NewGuid(), Name = "player.1" };
        var player2 = new PlayerState { Id = Guid.NewGuid(), Name = "player.2" };

        InitializeBattlePoints(player1);
        InitializeBattlePoints(player2);
        var firstPlayerId = Roll(0.5) ? player1.Id : player2.Id;

        var state = new GameState
        {
            ActivePlayerId = firstPlayerId,
            ActionPoints = MaxActionPoints,
            Phase = GamePhase.HeroDraft,
            OpeningFirstPlayerId = firstPlayerId,
            IsTestMode = true
        };
        state.Players.Add(player1);
        state.Players.Add(player2);
        OpenTestOpeningHeroDrafts(state);
        return state;
    }

    private static void InitializeBattlePoints(PlayerState player)
    {
        player.BattlePoints.Current = InitialBattlePoints;
        player.BattlePoints.Max = MaxBattlePoints;
        player.BattlePoints.GainedThisTurn = 0;
        player.BattlePoints.LastReasonId = null;
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

    private void OpenOpeningHeroDrafts(GameState state)
    {
        foreach (var player in state.Players)
        {
            player.Characters.Clear();
            var candidates = CreateHeroDraftCandidates(player);
            foreach (var definition in candidates)
                AddHero(player, definition, CharacterZone.DraftCandidate);
        }

        OpenPendingHeroDraft(state, state.ActivePlayer, HeroDraftKind.Opening);
    }

    private void OpenTestOpeningHeroDrafts(GameState state)
    {
        foreach (var player in state.Players)
        {
            player.Characters.Clear();
            foreach (var definition in CharacterCatalog.All)
                AddHero(player, definition, CharacterZone.DraftCandidate);
        }

        OpenPendingHeroDraft(state, state.ActivePlayer, HeroDraftKind.TestOpening);
    }

    private void OpenPendingHeroDraft(GameState state, PlayerState player, HeroDraftKind kind)
    {
        var candidates = kind is HeroDraftKind.Opening or HeroDraftKind.TestOpening
            ? player.Characters
                .Where(character => character.Zone == CharacterZone.DraftCandidate)
                .OrderBy(character => character.Slot)
                .Select(character => character.Definition)
                .ToList()
            : CreateHeroDraftCandidates(player);

        if (candidates.Count == 0)
            throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));

        state.PendingHeroDraft = new PendingHeroDraftState
        {
            PlayerId = player.Id,
            Kind = kind
        };
        state.PendingHeroDraft.CandidateKeys.AddRange(candidates.Select(definition => definition.Key));
        Log(state, L10n.Text(kind is HeroDraftKind.Opening or HeroDraftKind.TestOpening ? "log.heroDraftOpened" : "log.heroRecruitOpened",
            ("player", L10n.Player(player.Name))), "system");
    }

    private IReadOnlyList<CharacterDefinition> CreateHeroDraftCandidates(PlayerState player)
    {
        var ownedKeys = player.Characters
            .Where(character => character.IsInBattle)
            .Select(character => character.Definition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return CharacterCatalog.All
            .Where(definition => !ownedKeys.Contains(definition.Key))
            .OrderBy(_ => _random.Next())
            .Take(4)
            .ToList();
    }

    public void SelectHeroDraft(GameState state, Guid playerId, string characterKey)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (!draft.CandidateKeys.Contains(characterKey, StringComparer.OrdinalIgnoreCase))
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        var player = state.Players.Single(item => item.Id == playerId);
        if (player.Characters.Any(character => character.IsInBattle
                && string.Equals(character.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase)))
            throw new GameRuleException(L10n.Text("error.heroAlreadyOwned"));

        var character = draft.Kind switch
        {
            HeroDraftKind.Opening => ConfirmOpeningHero(player, characterKey),
            HeroDraftKind.TestOpening => AddTestOpeningHero(player, characterKey),
            _ => AddHero(player, CharacterCatalog.All.Single(item =>
                string.Equals(item.Key, characterKey, StringComparison.OrdinalIgnoreCase)))
        };

        GrantStartingMagicPower(player);
        Log(state, L10n.Text("log.heroDraftSelected",
            ("player", L10n.Player(player.Name)),
            ("character", L10n.Character(character.Definition.Key)),
            ("characterId", L10n.Raw(character.Id))), "system");

        if (draft.Kind is HeroDraftKind.Opening or HeroDraftKind.TestOpening)
        {
            if (draft.Kind == HeroDraftKind.TestOpening && player.ActiveCharacterCount < 4)
            {
                OpenPendingHeroDraft(state, player, HeroDraftKind.TestOpening);
                return;
            }

            if (draft.Kind == HeroDraftKind.TestOpening)
                player.Characters.RemoveAll(character => character.Zone == CharacterZone.DraftCandidate);

            var nextPlayer = state.Players.FirstOrDefault(item =>
                item.Characters.Any(character => character.Zone == CharacterZone.DraftCandidate));
            if (nextPlayer is not null)
            {
                state.ActivePlayerId = nextPlayer.Id;
                OpenPendingHeroDraft(state, nextPlayer, draft.Kind);
                return;
            }

            state.PendingHeroDraft = null;
            state.Phase = GamePhase.Playing;
            state.ActivePlayerId = state.OpeningFirstPlayerId ?? state.ActivePlayerId;
            Log(state, L10n.Text("log.firstPlayer", ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
            Log(state, L10n.Text("log.firstTurnNoStartEffects"), "system");
            if (!state.IsTestMode)
                TryOpenRewardWindowForActivePlayer(state);
            return;
        }

        state.PendingHeroDraft = null;
        EndRewardTurnIfResolved(state);
    }

    public void ResetHeroDraft(GameState state, Guid playerId)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (draft.Kind != HeroDraftKind.Recruit)
            throw new GameRuleException(L10n.Text("error.heroDraftResetUnavailable"));

        var player = state.Players.Single(item => item.Id == playerId);
        var cost = GetRewardResetCost(draft.ResetCount);
        if (!TrySpendBp(state, player, cost, BpSpendReasonRewardReroll))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        var candidates = CreateHeroDraftCandidates(player);
        if (candidates.Count == 0)
            throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));

        draft.ResetCount++;
        draft.CandidateKeys.Clear();
        draft.CandidateKeys.AddRange(candidates.Select(definition => definition.Key));
        Log(state, L10n.Text("log.rewardReset",
            ("player", L10n.Player(player.Name)),
            ("cost", L10n.Raw(cost)),
            ("count", L10n.Raw(draft.ResetCount))), "system");
    }

    private static CharacterState ConfirmOpeningHero(PlayerState player, string characterKey)
    {
        var selected = player.Characters.Single(character =>
            character.Zone == CharacterZone.DraftCandidate
            && string.Equals(character.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase));

        player.Characters.RemoveAll(character =>
            character.Zone == CharacterZone.DraftCandidate
            && !string.Equals(character.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase));

        selected.Zone = CharacterZone.Battlefield;
        selected.CurrentHp = selected.Definition.MaxHp;
        return selected;
    }

    private CharacterState AddTestOpeningHero(PlayerState player, string characterKey)
    {
        var selectedCandidate = player.Characters.Single(character =>
            character.Zone == CharacterZone.DraftCandidate
            && string.Equals(character.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase));

        player.Characters.Remove(selectedCandidate);
        var selected = AddHero(player, selectedCandidate.Definition);
        UnlockAllRoleActions(selected);
        return selected;
    }

    private void UnlockAllRoleActions(CharacterState character)
    {
        foreach (var action in _roleActions.GetUpgradeChoices(character.Definition.Key))
        {
            if (!character.RoleActionIds.Contains(action.Metadata.Id, StringComparer.OrdinalIgnoreCase))
                character.RoleActionIds.Add(action.Metadata.Id);
        }
    }

    private static CharacterState AddHero(PlayerState player, CharacterDefinition definition,
        CharacterZone zone = CharacterZone.Battlefield)
    {
        var slot = player.Characters
            .Where(character => character.Zone == zone)
            .Select(character => character.Slot)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        var character = new CharacterState
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Slot = slot,
            Definition = definition,
            CurrentHp = definition.MaxHp,
            Zone = zone
        };
        character.RoleActionIds.AddRange(definition.RoleActionIds ?? []);
        player.Characters.Add(character);
        return character;
    }

    public AttackResult Attack(GameState state, Guid attackerId, Guid defenderId)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        var attacker = state.FindCharacter(attackerId);
        var defender = state.FindCharacter(defenderId);
        ValidateAttack(state, attacker, defender);

        var context = new GameEngineContext(this, state);
        var attackerTrait = _traits.Get(attacker.Definition.TraitId);
        attackerTrait.OnAttackDeclared(context, attacker, defender);
        var counterBlocked = IsCounterAttackBlocked(defender);
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
        if (counterBlocked)
            counterPacket.Notes.Add(L10n.Text("note.counterBlockedByChallenge",
                ("character", L10n.Character(defender.Definition.Key)),
                ("characterId", L10n.Raw(defender.Id))));

        var attackerOwner = state.FindOwner(attacker);
        var defenderOwner = state.FindOwner(defender);
        var defenderShieldBefore = defenderOwner.SharedShield;

        ModifyDamage(state, attackPacket);
        ModifyDamage(state, counterPacket);

        attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - counterPacket.Amount);
        var actualAttackTarget = attackPacket.TargetCharacter;
        actualAttackTarget.CurrentHp = Math.Max(0, actualAttackTarget.CurrentHp - attackPacket.Amount);
        ResolvePreyZeroDamage(state, attackPacket);
        ResolvePactAfterActiveAttack(state, attacker, actualAttackTarget);
        ResolvePreyZeroDamage(state, counterPacket);

        var resolvedCollateral = attackPacket.Collateral
            .Concat(counterPacket.Collateral)
            .Select(collateral => (Collateral: collateral, Packet: ResolveCollateralDamage(state, collateral)))
            .ToArray();

        attacker.AttackUsesThisTurn++;
        state.ActionPoints -= attacker.Definition.Cost;
        state.ActionsTakenThisTurn++;
        state.ActiveAttacksTakenThisTurn++;

        foreach (var note in attackPacket.Notes.Concat(counterPacket.Notes).Concat(resolvedCollateral.SelectMany(item => item.Packet.Notes)))
            Log(state, note, "status");

        Log(state, L10n.Text("log.exchange",
                ("attacker", L10n.Character(attacker.Definition.Key)),
                ("attackerId", L10n.Raw(attacker.Id)),
                ("defender", L10n.Character(actualAttackTarget.Definition.Key)),
                ("defenderId", L10n.Raw(actualAttackTarget.Id)),
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
                ("source", L10n.Character(item.Collateral.Source.Definition.Key)),
                ("sourceId", L10n.Raw(item.Collateral.Source.Id)),
                ("character", L10n.Character(item.Packet.TargetCharacter.Definition.Key)),
                ("characterId", L10n.Raw(item.Packet.TargetCharacter.Id)),
                ("amount", L10n.Raw(item.Packet.Amount))), "physical");

        TryAwardEnemyShieldBreakBp(state, attackerOwner, defenderOwner, defenderShieldBefore, attackPacket.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, attackerOwner, defenderOwner, attackPacket.Amount);
        TryResolveRageShieldBreak(state, attacker, actualAttackTarget, defenderOwner, defenderShieldBefore, attackPacket.ShieldAbsorbed);

        var exchange = new AttackExchange(
            attacker,
            actualAttackTarget,
            attackPacket.Amount,
            counterPacket.Amount);
        attackerTrait.OnAfterExchange(context, attacker, exchange);

        var defeated = ResolveDefeats(state);
        EvaluateGameEnd(state);

        return new AttackResult(state, new CombatOutcome(
            attacker.Id,
            actualAttackTarget.Id,
            attackPacket.Amount,
            counterPacket.Amount,
            attackPacket.DamageType,
            counterPacket.DamageType,
            attackPacket.ShieldAbsorbed,
            counterPacket.ShieldAbsorbed,
            defenderOwner.SharedShield,
            attackerOwner.SharedShield,
            attackPacket.Notes.Concat(counterPacket.Notes).ToArray(),
            defeated));
    }

    public void DeployShield(GameState state)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
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
        var logKey = isReinforcing ? "log.shieldReinforced" : "log.shieldDeployed";
        Log(state, L10n.Text(logKey,
            ("player", L10n.Player(player.Name)),
            ("shield", L10n.Raw(player.SharedShield))), "shield");
        if (isReinforcing && player.ShieldDeploymentsThisTurn >= MaxShieldDeploymentsPerTurn)
            TryGainBp(state, player, 1, BpReasonShieldFullyDeployed);
    }

    public static bool CanReinforceShield(PlayerState player) =>
        player.ShieldDeploymentsThisTurn > 0 && player.SharedShield > 0;

    public static int GetShieldCost(int deploymentsThisTurn, int sharedShield) =>
        deploymentsThisTurn > 0 && sharedShield > 0 ? ReinforcedShieldCost : FirstShieldCost;

    public void EndTurn(GameState state)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
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
        ApplyPendingActionPointDebt(state, state.ActivePlayer);
        state.ActionsTakenThisTurn = 0;
        state.ActiveAttacksTakenThisTurn = 0;
        state.ActivePlayer.BattlePoints.GainedThisTurn = 0;

        if (state.ActivePlayer.SharedShield > 0)
            Log(state, L10n.Text("log.shieldExpired", ("player", L10n.Player(state.ActivePlayer.Name))), "shield");
        state.ActivePlayer.SharedShield = 0;
        state.ActivePlayer.SharedShieldPhysicalDefense = 0;
        state.ActivePlayer.SharedShieldMagicalDefense = 0;
        state.ActivePlayer.SharedShieldDefenseExpireOnTurnStartPlayerId = null;
        state.ActivePlayer.ShieldDeploymentsThisTurn = 0;

        foreach (var character in state.ActivePlayer.Characters)
        {
            character.BonusAttackUsesThisTurn = 0;
            character.HasActed = false;
            character.GuardConsumed = false;
            character.RoleActionsUsedThisTurn.Clear();
            TickRoleActionCooldowns(character);
        }

        Log(state, L10n.Text("log.turnStart",
            ("turn", L10n.Raw(state.TurnNumber)),
            ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
        TryGainBp(state, state.ActivePlayer, 1, BpReasonTurnStart);
        ProcessTurnStart(state);
        TryOpenRewardWindowForActivePlayer(state);
        ResolveDefeats(state);
        EvaluateGameEnd(state);
    }

    private void ApplyPendingActionPointDebt(GameState state, PlayerState player)
    {
        var debt = Math.Max(0, player.PendingActionPointDebt);
        if (debt <= 0)
            return;

        state.ActionPoints = Math.Max(0, state.ActionPoints - debt);
        player.PendingActionPointDebt = 0;
        Log(state, L10n.Text("log.actionPointDebt",
            ("player", L10n.Player(player.Name)),
            ("amount", L10n.Raw(debt))), "debuff");
    }

    public int GetActiveAttack(CharacterState character)
    {
        var damage = GetBaseAttack(character);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            damage = status.ModifyActiveAttack(character, damage);
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
        IsCounterAttackBlocked(character)
            ? 0
            : GetBaseCounterAttack(GetBaseAttack(character));

    public static int GetBaseCounterAttack(int attack) => Math.Max(1, attack - CounterAttackPenalty);

    public static bool IsCounterAttackBlocked(CharacterState character) =>
        character.Statuses.Any(status => status.Id == "trembling" && !status.Expired);

    public static bool IsActiveAttackBlocked(CharacterState character) =>
        character.Statuses.Any(status => status.BlocksActiveAttack && !status.Expired);

    public int GetPhysicalDefense(CharacterState character)
    {
        var defense = character.Definition.PhysicalDefense;
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyPhysicalDefense(defense);
        return defense;
    }

    public int GetMagicalDefense(CharacterState character)
    {
        var defense = character.Definition.MagicalDefense;
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyMagicalDefense(defense);
        return defense;
    }

    public int GetDefense(CharacterState character, DamageType damageType) => damageType switch
    {
        DamageType.Physical => GetPhysicalDefense(character),
        DamageType.Magical => GetMagicalDefense(character),
        _ => 0
    };

    public static int GetSharedShieldDefense(PlayerState player, DamageType damageType) => damageType switch
    {
        DamageType.Physical => player.SharedShieldPhysicalDefense,
        DamageType.Magical => player.SharedShieldMagicalDefense,
        _ => 0
    };

    public CharacterTrait GetTrait(CharacterState character) =>
        _traits.Get(character.Definition.TraitId);

    public IReadOnlyList<CharacterRoleAction> GetRoleActions(CharacterState character) =>
        _roleActions.GetMany(character.RoleActionIds);

    public IReadOnlyList<CharacterRoleAction> GetRoleActionUpgradeChoices(CharacterState character) =>
        character.IsAlive && character.RoleActionIds.Count == 0
            ? _roleActions.GetUpgradeChoices(character.Definition.Key)
            : [];

    private void ProcessTurnStart(GameState state)
    {
        var context = new GameEngineContext(this, state);
        if (state.ActivePlayer.SharedShieldDefenseExpireOnTurnStartPlayerId == state.ActivePlayerId)
        {
            state.ActivePlayer.SharedShieldPhysicalDefense = 0;
            state.ActivePlayer.SharedShieldMagicalDefense = 0;
            state.ActivePlayer.SharedShieldDefenseExpireOnTurnStartPlayerId = null;
        }

        foreach (var character in state.ActivePlayer.Characters.Where(character => character.IsAlive).ToArray())
        {
            foreach (var status in character.Statuses.ToArray())
                status.OnTurnStart(context, character);
            character.Statuses.RemoveAll(status => status.Expired);
        }

        ResolveDefeats(state);

        foreach (var character in state.ActivePlayer.Characters.Where(character => character.IsAlive).ToArray())
            _traits.Get(character.Definition.TraitId).OnTurnStart(context, character);
    }

    private void ModifyDamage(GameState state, DamagePacket packet)
    {
        var sourceOwner = state.FindOwner(packet.SourceCharacter);
        foreach (var status in packet.SourceCharacter.Statuses.Where(status => !status.Expired))
            status.ModifyOutgoingDamage(new GameEngineContext(this, state), packet.SourceCharacter, packet);

        foreach (var traitOwner in sourceOwner.Characters.Where(character => character.IsAlive))
            _traits.Get(traitOwner.Definition.TraitId).ModifyOutgoingDamage(
                new GameEngineContext(this, state), traitOwner, packet);

        var targetOwner = state.FindOwner(packet.TargetCharacter);
        if (!packet.IgnoresSharedShield)
            ApplySharedShield(targetOwner, packet);
        if (!packet.IgnoresTargetDefense)
            ApplyTypeDefense(packet);

        foreach (var status in packet.TargetCharacter.Statuses.Where(status => !status.Expired))
            status.ModifyIncomingDamage(new GameEngineContext(this, state), packet.TargetCharacter, packet);

        var incomingModifiers = targetOwner.Characters
            .Where(character => character.IsAlive)
            .OrderBy(character => _traits.Get(character.Definition.TraitId).IncomingModifierPriority)
            .ToArray();

        foreach (var traitOwner in incomingModifiers)
            _traits.Get(traitOwner.Definition.TraitId).ModifyIncomingDamage(
                new GameEngineContext(this, state), traitOwner, packet);

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
                ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
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
                ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
                ("damageType", L10n.Damage(packet.DamageType)),
                ("amount", L10n.Raw(increased))));
        }
    }

    private DamagePacket ResolveCollateralDamage(GameState state, CollateralDamage collateral)
    {
        var sourceOwner = state.FindOwner(collateral.Source);
        var targetOwner = state.FindOwner(collateral.Target);
        var shieldBefore = targetOwner.SharedShield;
        var packet = new DamagePacket
        {
            SourceCharacter = collateral.Source,
            TargetCharacter = collateral.Target,
            DamageType = collateral.DamageType,
            Source = DamageSource.Trait,
            Amount = collateral.Amount
        };
        ModifyDamage(state, packet);
        collateral.Target.CurrentHp = Math.Max(0, collateral.Target.CurrentHp - packet.Amount);
        ResolvePreyZeroDamage(state, packet);
        TryAwardEnemyShieldBreakBp(state, sourceOwner, targetOwner, shieldBefore, packet.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        return packet;
    }

    private static void ApplySharedShield(PlayerState targetOwner, DamagePacket packet)
    {
        if (packet.Amount <= 0 || targetOwner.SharedShield <= 0)
            return;

        ApplyShieldTypeDefense(targetOwner, packet);
        if (packet.Amount <= 0)
            return;

        var absorbed = Math.Min(packet.Amount, targetOwner.SharedShield);
        packet.Amount -= absorbed;
        packet.ShieldAbsorbed += absorbed;
        targetOwner.SharedShield -= absorbed;
        packet.Notes.Add(L10n.Text("note.shieldAbsorb",
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
            ("damageType", L10n.Damage(packet.DamageType)),
            ("amount", L10n.Raw(absorbed))));
    }

    private static void ApplyShieldTypeDefense(PlayerState targetOwner, DamagePacket packet)
    {
        var defense = GetSharedShieldDefense(targetOwner, packet.DamageType);
        if (defense > 0)
        {
            var reduced = Math.Min(packet.Amount, defense);
            packet.Amount -= reduced;
            packet.ShieldDefenseReduced += reduced;
            packet.Notes.Add(L10n.Text("note.shieldTypeDefense",
                ("damageType", L10n.Damage(packet.DamageType)),
                ("amount", L10n.Raw(reduced))));
            return;
        }

        if (defense < 0)
        {
            var increased = Math.Abs(defense);
            packet.Amount += increased;
            packet.ShieldDefenseReduced -= increased;
            packet.Notes.Add(L10n.Text("note.shieldTypeDefenseWeakness",
                ("damageType", L10n.Damage(packet.DamageType)),
                ("amount", L10n.Raw(increased))));
        }
    }

    internal BattlePointGainResult TryGainBp(
        GameState state,
        PlayerState player,
        int amount,
        string reasonId)
    {
        var requested = Math.Max(0, amount);
        if (requested == 0)
            return new BattlePointGainResult(
                player.Id, reasonId, amount, 0, 0,
                player.BattlePoints.Current, player.BattlePoints.Max, player.BattlePoints.GainedThisTurn);

        var turnCapacity = Math.Max(0, BattlePointGainCapPerTurn - player.BattlePoints.GainedThisTurn);
        var totalCapacity = Math.Max(0, player.BattlePoints.Max - player.BattlePoints.Current);
        var gained = Math.Min(requested, Math.Min(turnCapacity, totalCapacity));
        var blocked = requested - gained;

        if (gained > 0)
        {
            player.BattlePoints.Current += gained;
            player.BattlePoints.GainedThisTurn += gained;
            player.BattlePoints.LastReasonId = reasonId;
            Log(state, L10n.Text("log.bpGained",
                ("player", L10n.Player(player.Name)),
                ("amount", L10n.Raw(gained)),
                ("reason", L10n.BpReason(reasonId)),
                ("current", L10n.Raw(player.BattlePoints.Current)),
                ("max", L10n.Raw(player.BattlePoints.Max))), "buff");
        }

        if (blocked > 0)
        {
            Log(state, L10n.Text("log.bpGainCapped",
                ("player", L10n.Player(player.Name)),
                ("blocked", L10n.Raw(blocked)),
                ("reason", L10n.BpReason(reasonId))), "system");
        }

        return new BattlePointGainResult(
            player.Id, reasonId, requested, gained, blocked,
            player.BattlePoints.Current, player.BattlePoints.Max, player.BattlePoints.GainedThisTurn);
    }

    internal bool TrySpendBp(
        GameState state,
        PlayerState player,
        int amount,
        string reasonId)
    {
        var cost = Math.Max(0, amount);
        if (cost == 0)
            return true;
        if (player.BattlePoints.Current < cost)
            return false;

        player.BattlePoints.Current -= cost;
        player.BattlePoints.LastReasonId = reasonId;
        Log(state, L10n.Text("log.bpSpent",
            ("player", L10n.Player(player.Name)),
            ("amount", L10n.Raw(cost)),
            ("reason", L10n.BpReason(reasonId)),
            ("current", L10n.Raw(player.BattlePoints.Current)),
            ("max", L10n.Raw(player.BattlePoints.Max))), "system");
        return true;
    }

    public static bool IsRewardRound(int roundNumber) =>
        roundNumber >= FirstRewardRound
        && (roundNumber - FirstRewardRound) % RewardRoundInterval == 0;

    public void TryOpenRewardWindowForActivePlayer(GameState state)
    {
        if (state.IsTestMode)
            return;
        if (state.Phase != GamePhase.Playing || state.RewardWindow is not null)
            return;

        var round = (state.TurnNumber + 1) / 2;
        if (!IsRewardRound(round))
            return;

        var key = RewardWindowKey(state.ActivePlayer.Id, round);
        if (state.ResolvedRewardWindows.Contains(key))
            return;

        state.RewardWindow = new RewardWindowState
        {
            PlayerId = state.ActivePlayer.Id,
            RoundNumber = round,
            ResetCount = 0
        };
        RefreshRewardOptions(state.RewardWindow, state.ActivePlayer);
        Log(state, L10n.Text("log.rewardOpened",
            ("player", L10n.Player(state.ActivePlayer.Name)),
            ("round", L10n.Raw(round))), "system");
    }

    public void SelectReward(GameState state, string instanceId)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        var option = window.Options.SingleOrDefault(item => item.InstanceId == instanceId);
        if (option is null)
            throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        var player = state.ActivePlayer;
        if (!TrySpendBp(state, player, option.Cost, BpSpendReasonRewardPurchase))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        var definition = RewardCatalog.All.SingleOrDefault(item => item.Id == option.RewardId)
            ?? throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        if (definition.Kind == RewardKind.HeroRoleActionUpgrade)
        {
            if (!HasEligibleRoleActionUpgradeTarget(player))
                throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));
            state.PendingRoleActionUpgrade = new PendingRoleActionUpgradeState
            {
                PlayerId = player.Id,
                RewardId = option.RewardId
            };
            Log(state, L10n.Text("log.roleActionUpgradePending",
                ("player", L10n.Player(player.Name))), "system");
            CloseRewardWindow(state, window);
        }
        else if (definition.Kind == RewardKind.HeroRecruit)
        {
            if (!CanRecruitHero(player))
                throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));
            CloseRewardWindow(state, window);
            OpenPendingHeroDraft(state, player, HeroDraftKind.Recruit);
        }
        else
        {
            ApplyDummyReward(player, option.RewardId);
            window.Options.Remove(option);
            window.PurchaseCount++;
        }

        Log(state, L10n.Text("log.rewardPurchased",
            ("player", L10n.Player(player.Name)),
            ("reward", L10n.Reward(option.RewardId)),
            ("cost", L10n.Raw(option.Cost))), "system");
    }

    public void ResetRewardWindow(GameState state)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        var player = state.ActivePlayer;
        var cost = GetRewardResetCost(window.ResetCount);
        if (!TrySpendBp(state, player, cost, BpSpendReasonRewardReroll))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        window.ResetCount++;
        RefreshRewardOptions(window, player);
        Log(state, L10n.Text("log.rewardReset",
            ("player", L10n.Player(player.Name)),
            ("cost", L10n.Raw(cost)),
            ("count", L10n.Raw(window.ResetCount))), "system");
    }

    public void SkipRewardWindow(GameState state)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        var player = state.ActivePlayer;
        if (window.PurchaseCount == 0)
        {
            TryGainBp(state, player, RewardSkipBattlePoints, BpReasonRewardSkip);
            Log(state, L10n.Text("log.rewardSkipped",
                ("player", L10n.Player(player.Name)),
                ("amount", L10n.Raw(RewardSkipBattlePoints))), "system");
        }
        CloseRewardWindow(state, window);
        EndRewardTurnIfResolved(state);
    }

    public static int GetRewardResetCost(int resetCount) => resetCount == 0 ? 0 : 1;

    private RewardWindowState RequireActiveRewardWindow(GameState state)
    {
        if (state.RewardWindow is null)
            throw new GameRuleException(L10n.Text("error.rewardWindowClosed"));
        if (state.RewardWindow.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        return state.RewardWindow;
    }

    private void RefreshRewardOptions(RewardWindowState window, PlayerState player)
    {
        window.Options.Clear();
        var options = new List<RewardDefinition>();
        if (HasEligibleRoleActionUpgradeTarget(player))
            options.Add(RewardCatalog.HeroRoleActionUpgrade);
        if (CanRecruitHero(player))
            options.Add(RewardCatalog.HeroRecruit);

        options.AddRange(RewardCatalog.DummyRewards
            .Where(reward => options.All(option => option.Id != reward.Id))
            .OrderBy(_ => _random.Next())
            .Take(Math.Max(0, 3 - options.Count)));

        foreach (var reward in options)
            window.Options.Add(new RewardOptionState($"{reward.Id}-{Guid.NewGuid():N}", reward.Id, reward.Cost));
    }

    private bool HasEligibleRoleActionUpgradeTarget(PlayerState player) =>
        player.Characters.Any(character => GetRoleActionUpgradeChoices(character).Count > 0);

    private static bool CanRecruitHero(PlayerState player)
    {
        var ownedKeys = player.Characters
            .Where(character => character.IsInBattle)
            .Select(character => character.Definition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ownedKeys.Count < 4 && CharacterCatalog.All.Any(definition => !ownedKeys.Contains(definition.Key));
    }

    private static void CloseRewardWindow(GameState state, RewardWindowState window)
    {
        state.ResolvedRewardWindows.Add(RewardWindowKey(window.PlayerId, window.RoundNumber));
        state.RewardWindow = null;
    }

    private void EndRewardTurnIfResolved(GameState state)
    {
        if (state.Phase != GamePhase.Playing
            || state.RewardWindow is not null
            || state.PendingRoleActionUpgrade is not null
            || state.PendingHeroDraft is not null)
            return;

        var round = (state.TurnNumber + 1) / 2;
        if (!IsRewardRound(round))
            return;

        if (!state.ResolvedRewardWindows.Contains(RewardWindowKey(state.ActivePlayerId, round)))
            return;

        EndTurn(state);
    }

    private static string RewardWindowKey(Guid playerId, int roundNumber) => $"{playerId:N}:{roundNumber}";

    private static void ApplyDummyReward(PlayerState player, string rewardId)
    {
        foreach (var character in player.Characters.Where(character => character.IsInBattle))
        {
            switch (rewardId)
            {
                case "dummy-reward-a":
                    character.Statuses.Add(new RewardMagicalDefenseStatus(Guid.Empty));
                    break;
                case "dummy-reward-b" when character.Definition.AttackType == DamageType.Magical:
                    character.Statuses.Add(new RewardMagicalAttackStatus(Guid.Empty));
                    break;
                case "dummy-reward-c":
                    character.Statuses.Add(new RewardAttackStatus(Guid.Empty));
                    break;
            }
        }
    }

    public void SelectRoleActionUpgrade(GameState state, Guid characterId, string roleActionId)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        var pending = state.PendingRoleActionUpgrade
            ?? throw new GameRuleException(L10n.Text("error.noPendingRoleActionUpgrade"));
        if (pending.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));

        var character = state.FindCharacter(characterId);
        if (character.PlayerId != state.ActivePlayerId || !character.IsAlive)
            throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));
        if (character.RoleActionIds.Count > 0)
            throw new GameRuleException(L10n.Text("error.roleActionAlreadyUnlocked"));
        if (!_roleActions.IsUpgradeChoice(character.Definition.Key, roleActionId))
            throw new GameRuleException(L10n.Text("error.roleActionNotUpgradeChoice"));

        character.RoleActionIds.Add(roleActionId);
        state.PendingRoleActionUpgrade = null;
        Log(state, L10n.Text("log.roleActionUnlocked",
            ("character", L10n.Character(character.Definition.Key)),
            ("characterId", L10n.Raw(character.Id)),
            ("roleAction", L10n.RoleAction(roleActionId))), "buff");
        EndRewardTurnIfResolved(state);
    }

    public void UseRoleAction(GameState state, Guid characterId, string roleActionId, Guid? targetCharacterId)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);

        var actor = state.FindCharacter(characterId);
        if (actor.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.notActiveCharacter"));
        if (!actor.RoleActionIds.Contains(roleActionId))
            throw new GameRuleException(L10n.Text("error.roleActionNotUnlocked"));

        var action = _roleActions.Get(roleActionId);
        var unavailable = action.UnavailableReason(state, actor);
        if (unavailable is not null)
            throw new GameRuleException(unavailable);
        EnsureRoleActionTargetIsValidBeforePayment(state, actor, roleActionId, targetCharacterId);

        SpendRoleActionCost(state, action);
        switch (roleActionId)
        {
            case "saintly-prayer":
                UseSaintlyPrayer(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "royal-command":
                UseRoyalCommand(state, actor);
                break;
            case "guard-oath":
                UseGuardOath(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "raise-bulwark":
                UseRaiseBulwark(state, actor);
                break;
            case "arcane-channel":
                UseArcaneChannel(state, actor);
                break;
            case "searing-brand":
                UseSearingBrand(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "cleansing-herbs":
                UseCleansingHerbs(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "weakening-spores-action":
                UseWeakeningSporesAction(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "war-cry":
                UseWarCry(state, actor);
                break;
            case "challenge":
                UseChallenge(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "star-reading":
                UseStarReading(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "fate-mark":
                UseFateMark(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "predatory-gaze":
                UsePredatoryGaze(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "dark-pact":
                UseDarkPact(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "supply-basket":
                UseSupplyBasket(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "field-work":
                UseFieldWork(state, actor);
                break;
            default:
                throw new GameRuleException(L10n.Text("error.roleActionNotFound"));
        }

        if (!action.Metadata.IsRepeatable)
            actor.RoleActionsUsedThisTurn.Add(roleActionId);
        StartRoleActionCooldown(actor, action);
        state.ActionsTakenThisTurn++;
        ResolveDefeats(state);
        EvaluateGameEnd(state);
    }

    private static CharacterState RequireAllyTarget(GameState state, CharacterState actor, Guid? targetCharacterId)
    {
        if (targetCharacterId is null)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        var target = state.FindCharacter(targetCharacterId.Value);
        if (target.PlayerId != actor.PlayerId || !target.IsAlive)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        return target;
    }

    private static CharacterState RequireEnemyTarget(GameState state, CharacterState actor, Guid? targetCharacterId)
    {
        if (targetCharacterId is null)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        var target = state.FindCharacter(targetCharacterId.Value);
        if (target.PlayerId == actor.PlayerId || !target.IsAlive)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        return target;
    }

    private void SpendRoleActionCost(GameState state, CharacterRoleAction action)
    {
        if (state.ActionPoints < action.Metadata.BaseApCost)
            throw new GameRuleException(L10n.Text("error.notEnoughAp"));
        state.ActionPoints -= action.Metadata.BaseApCost;
    }

    private void UseSaintlyPrayer(GameState state, CharacterState actor, CharacterState target)
    {
        var before = target.CurrentHp;
        target.CurrentHp = Math.Min(target.Definition.MaxHp, target.CurrentHp + 2);
        var healed = target.CurrentHp - before;
        var debuff = target.Statuses.FirstOrDefault(status => !status.IsBuff && status.IsDispellable && !status.Expired);
        if (debuff is not null)
        {
            target.Statuses.Remove(debuff);
            state.ActionPoints += 1;
            Log(state, L10n.Text("log.roleActionCleanse",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status(debuff.Id))), "buff");
        }

        Log(state, L10n.Text("log.roleActionHeal",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(healed))), "heal");
    }

    private void UseRoyalCommand(GameState state, CharacterState actor)
    {
        state.ActionPoints += 2;
        state.ActivePlayer.PendingActionPointDebt += 1;
        Log(state, L10n.Text("log.royalCommand",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id))), "buff");
    }

    private void UseGuardOath(GameState state, CharacterState actor, CharacterState target)
    {
        var existing = target.Statuses.OfType<GuardOathStatus>().FirstOrDefault(status => !status.Expired);
        if (existing is not null)
            existing.AddStack();
        else
            target.Statuses.Add(new GuardOathStatus(actor.Id));
        Log(state, L10n.Text("log.guardOathApplied",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "buff");
    }

    private void UseRaiseBulwark(GameState state, CharacterState actor)
    {
        var owner = state.FindOwner(actor);
        if (owner.SharedShield <= 0)
            throw new GameRuleException(L10n.Text("error.roleActionRequiresShield"));
        owner.SharedShield = Math.Max(1, (int)Math.Ceiling(owner.SharedShield * 1.5));
        owner.SharedShieldPhysicalDefense += 2;
        owner.SharedShieldDefenseExpireOnTurnStartPlayerId = actor.PlayerId;
        Log(state, L10n.Text("log.bulwarkRaised",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id)),
            ("shield", L10n.Raw(owner.SharedShield)),
            ("pdef", L10n.Raw(owner.SharedShieldPhysicalDefense))), "shield");
    }

    private void UseWarCry(GameState state, CharacterState actor)
    {
        actor.BonusAttackUsesThisTurn = Math.Max(actor.BonusAttackUsesThisTurn, 1);
        actor.Statuses.RemoveAll(status => status.Id == "rage");
        actor.Statuses.Add(new RageStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.warCry",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id))), "buff");
    }

    private void UseArcaneChannel(GameState state, CharacterState actor)
    {
        actor.Statuses.RemoveAll(status => status.Id is "chant" or "chant-pending" or "attack-sealed");
        actor.Statuses.Add(new PendingChantStatus(actor.Id, actor.PlayerId, 2));
        actor.Statuses.Add(new AttackSealedStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.arcaneChannel",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id))), "magic");
    }

    private void UseSearingBrand(GameState state, CharacterState actor, CharacterState target)
    {
        AddBurning(target, actor.Id);
        target.Statuses.RemoveAll(status => status.Id == "void");
        target.Statuses.Add(new VoidStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("burning"))), "magic");
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("void"))), "magic");
    }

    private void UseCleansingHerbs(GameState state, CharacterState actor, CharacterState target)
    {
        var debuff = SelectCleansingHerbsDebuff(target);
        if (debuff is null)
        {
            Log(state, L10n.Text("log.roleActionNoCleanse",
                ("actor", L10n.Character(actor.Definition.Key)),
                ("actorId", L10n.Raw(actor.Id)),
                ("target", L10n.Character(target.Definition.Key)),
                ("targetId", L10n.Raw(target.Id))), "status");
            return;
        }

        target.Statuses.Remove(debuff);
        target.CurrentHp += 1;
        Log(state, L10n.Text("log.cleansingHerbs",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("status", L10n.Status(debuff.Id)),
            ("amount", L10n.Raw(1))), "heal");
    }

    private void UseWeakeningSporesAction(GameState state, CharacterState actor, CharacterState target)
    {
        var buff = SelectDispellableBuff(target);
        if (buff is not null)
        {
            target.Statuses.Remove(buff);
            Log(state, L10n.Text("log.attackBuffRemoved",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status(buff.Id))), "status");
        }
        else
        {
            Log(state, L10n.Text("log.roleActionNoDispel",
                ("actor", L10n.Character(actor.Definition.Key)),
                ("actorId", L10n.Raw(actor.Id)),
                ("target", L10n.Character(target.Definition.Key)),
                ("targetId", L10n.Raw(target.Id))), "status");
        }

        target.Statuses.RemoveAll(status => status.Id is "exhaustion" or "erosion");
        target.Statuses.Add(new ExhaustionStatus(actor.Id, target.PlayerId));
        target.Statuses.Add(new ErosionStatus(actor.Id, target.PlayerId));
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("exhaustion"))), "debuff");
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("erosion"))), "debuff");
    }

    private void UseChallenge(GameState state, CharacterState actor, CharacterState target)
    {
        target.Statuses.RemoveAll(status => status.Id == "trembling");
        target.Statuses.Add(new TremblingStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.challenge",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "debuff");
    }

    private void UseStarReading(GameState state, CharacterState actor, CharacterState target)
    {
        if (!CanReceiveStarReading(target))
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));

        target.BonusAttackUsesThisTurn++;
        Log(state, L10n.Text("log.starReading",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "buff");
    }

    private void UseFateMark(GameState state, CharacterState actor, CharacterState target)
    {
        target.Statuses.RemoveAll(status => status.Id == "marked");
        target.Statuses.Add(new FateMarkedStatus(actor.Id));
        Log(state, L10n.Text("log.fateMark",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "debuff");
    }

    private void UsePredatoryGaze(GameState state, CharacterState actor, CharacterState target)
    {
        target.Statuses.RemoveAll(status => status.Id == "prey");
        target.Statuses.Add(new PreyStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.predatoryGaze",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "debuff");
    }

    private void UseDarkPact(GameState state, CharacterState actor, CharacterState target)
    {
        var lifeCost = Math.Min(4, Math.Max(0, target.CurrentHp - 1));
        target.CurrentHp -= lifeCost;
        target.Statuses.RemoveAll(status => status.Id == "pact");
        target.Statuses.Add(new PactStatus(actor.Id));
        Log(state, L10n.Text("log.darkPact",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(lifeCost))), "debuff");
        if (target.CurrentHp * 2 < target.Definition.MaxHp)
            TryGainBp(state, state.ActivePlayer, 1, BpReasonRoleActionDarkPact);
    }

    private void UseSupplyBasket(GameState state, CharacterState actor, CharacterState target)
    {
        var before = target.CurrentHp;
        target.CurrentHp = Math.Min(target.Definition.MaxHp, target.CurrentHp + 1);
        target.Statuses.Add(new FortifyStatus(actor.Id, turns: 2));
        Log(state, L10n.Text("log.supplyBasket",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(target.CurrentHp - before))), "heal");
    }

    private void UseFieldWork(GameState state, CharacterState actor)
    {
        var hasHarvest = actor.Statuses.Any(status => status.Id == "harvest" && !status.Expired);
        var pendingHarvest = actor.Statuses.FirstOrDefault(status => status.Id == "harvest-pending" && !status.Expired);

        if (hasHarvest)
        {
            actor.BonusAttackUsesThisTurn = Math.Max(actor.BonusAttackUsesThisTurn, 1);
            Log(state, L10n.Text("log.fieldWorkDouble",
                ("character", L10n.Character(actor.Definition.Key)),
                ("characterId", L10n.Raw(actor.Id))), "buff");
            return;
        }

        if (pendingHarvest is not null)
        {
            var before = actor.CurrentHp;
            actor.CurrentHp = Math.Min(actor.Definition.MaxHp, actor.CurrentHp + 2);
            Log(state, L10n.Text("log.fieldWorkRest",
                ("character", L10n.Character(actor.Definition.Key)),
                ("characterId", L10n.Raw(actor.Id)),
                ("amount", L10n.Raw(actor.CurrentHp - before))), "heal");
            return;
        }

        actor.Statuses.RemoveAll(status => status.Id == "harvest-pending");
        actor.Statuses.Add(new PendingHarvestStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.fieldWorkSowing",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id))), "buff");
    }

    private static StatusEffect? SelectCleansingHerbsDebuff(CharacterState target)
    {
        var debuffs = target.Statuses
            .Where(status => !status.IsBuff && status.IsDispellable && !status.Expired)
            .ToList();
        return debuffs
            .Where(IsDamageDebuff)
            .FirstOrDefault()
            ?? debuffs.FirstOrDefault();
    }

    private static bool IsDamageDebuff(StatusEffect status) =>
        status.Id is "burning" or "void" or "exhaustion" or "erosion" or "prey" or "marked";

    private StatusEffect? SelectDispellableBuff(CharacterState target)
    {
        var buffs = target.Statuses
            .Where(status => status.IsBuff && status.IsDispellable && !status.Expired)
            .ToList();
        return buffs.Count == 0 ? null : buffs[Next(buffs.Count)];
    }

    private void TryAwardEnemyDamageBp(
        GameState state,
        PlayerState sourceOwner,
        PlayerState targetOwner,
        int damage)
    {
        if (damage <= 0 || sourceOwner.Id != state.ActivePlayerId || targetOwner.Id == sourceOwner.Id)
            return;

        TryGainBp(state, sourceOwner, 1, BpReasonOwnTurnEnemyHpDamage);
    }

    private void TryAwardEnemyShieldBreakBp(
        GameState state,
        PlayerState sourceOwner,
        PlayerState targetOwner,
        int shieldBefore,
        int shieldAbsorbed)
    {
        if (shieldBefore <= 0 || shieldAbsorbed <= 0 || targetOwner.SharedShield > 0
            || sourceOwner.Id != state.ActivePlayerId || targetOwner.Id == sourceOwner.Id)
            return;

        TryGainBp(state, sourceOwner, 1, BpReasonBreakEnemyShield);
    }

    private void TryResolveRageShieldBreak(
        GameState state,
        CharacterState attacker,
        CharacterState target,
        PlayerState targetOwner,
        int shieldBefore,
        int shieldAbsorbed)
    {
        if (shieldBefore <= 0
            || shieldAbsorbed <= 0
            || targetOwner.SharedShield > 0
            || !target.IsAlive
            || attacker.Statuses.All(status => status.Id != "rage" || status.Expired))
            return;

        var damage = GetActiveAttack(attacker);
        if (damage <= 0)
            return;

        DealAbsoluteDamage(state, target, damage, attacker.Id, L10n.RoleAction("war-cry"));
    }

    internal int DealTraitDamage(
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
            Source = DamageSource.Trait,
            Amount = amount,
            ReceivesMagicPowerBonus = receivesMagicPowerBonus
        };
        var sourceOwner = state.FindOwner(source);
        var targetOwner = state.FindOwner(target);
        var shieldBefore = targetOwner.SharedShield;
        ModifyDamage(state, packet);
        target.CurrentHp = Math.Max(0, target.CurrentHp - packet.Amount);
        ResolvePreyZeroDamage(state, packet);
        foreach (var note in packet.Notes)
            Log(state, note, "status");
        var effectArg = effectId is "burning" or "guard"
            ? L10n.Status(effectId)
            : L10n.Trait(effectId);
        Log(state, L10n.Text("log.effectDamage",
            ("effect", effectArg),
            ("source", L10n.Character(source.Definition.Key)),
            ("sourceId", L10n.Raw(source.Id)),
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(packet.Amount)),
            ("damageType", L10n.Damage(damageType))), damageType == DamageType.Physical ? "physical" : "magic");
        TryAwardEnemyShieldBreakBp(state, sourceOwner, targetOwner, shieldBefore, packet.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        return packet.Amount;
    }

    internal int DealAbsoluteDamage(
        GameState state,
        CharacterState target,
        int amount,
        Guid sourceCharacterId,
        string effectId)
        => DealAbsoluteDamage(state, target, amount, sourceCharacterId, L10n.Trait(effectId));

    private int DealAbsoluteDamage(
        GameState state,
        CharacterState target,
        int amount,
        Guid sourceCharacterId,
        LocalizedArg effectArg)
    {
        var source = state.FindCharacter(sourceCharacterId);
        var sourceOwner = state.FindOwner(source);
        var targetOwner = state.FindOwner(target);
        var dealt = Math.Max(0, amount);
        target.CurrentHp = Math.Max(0, target.CurrentHp - dealt);
        Log(state, L10n.Text("log.effectDamage",
            ("effect", effectArg),
            ("source", L10n.Character(source.Definition.Key)),
            ("sourceId", L10n.Raw(source.Id)),
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(dealt)),
            ("damageType", L10n.Damage(DamageType.Absolute))), "trait");
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, dealt);
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
        if (attacker.HasActed || IsActiveAttackBlocked(attacker))
            throw new GameRuleException(L10n.Text("error.alreadyActed"));
        if (attacker.Definition.Cost > state.ActionPoints)
            throw new GameRuleException(L10n.Text("error.notEnoughAp"));
    }

    private static void EnsurePlaying(GameState state)
    {
        if (state.Phase != GamePhase.Playing)
            throw new GameRuleException(L10n.Text("error.matchFinished"));
    }

    private static void EnsureNoRewardWindow(GameState state)
    {
        if (state.RewardWindow is not null)
            throw new GameRuleException(L10n.Text("error.rewardWindowOpen"));
    }

    private static void EnsureNoPendingRoleActionUpgrade(GameState state)
    {
        if (state.PendingRoleActionUpgrade is not null)
            throw new GameRuleException(L10n.Text("error.pendingRoleActionUpgrade"));
    }

    private static void EnsureNoPendingHeroDraft(GameState state)
    {
        if (state.PendingHeroDraft is not null)
            throw new GameRuleException(L10n.Text("error.pendingHeroDraft"));
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
            character.RoleActionsUsedThisTurn.Clear();
            character.RoleActionCooldowns.Clear();
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
            foreach (var character in state.Players.SelectMany(player => player.Characters).Where(character => character.IsAlive))
                _traits.Get(character.Definition.TraitId).OnCharacterDefeated(context, character, defeatedCharacter);
        }
        return defeated;
    }

    private static void StartRoleActionCooldown(CharacterState actor, CharacterRoleAction action)
    {
        if (action.Metadata.CooldownTurns <= 0)
            return;

        actor.RoleActionCooldowns[action.Metadata.Id] = action.Metadata.CooldownTurns + 1;
    }

    private static void TickRoleActionCooldowns(CharacterState character)
    {
        foreach (var id in character.RoleActionCooldowns.Keys.ToArray())
        {
            var next = character.RoleActionCooldowns[id] - 1;
            if (next <= 0)
                character.RoleActionCooldowns.Remove(id);
            else
                character.RoleActionCooldowns[id] = next;
        }
    }

    private static void EnsureRoleActionTargetIsValidBeforePayment(
        GameState state,
        CharacterState actor,
        string roleActionId,
        Guid? targetCharacterId)
    {
        switch (roleActionId)
        {
            case "saintly-prayer":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "guard-oath":
                if (targetCharacterId == actor.Id)
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "searing-brand":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
            case "cleansing-herbs":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "weakening-spores-action":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
            case "challenge":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
            case "star-reading":
                if (!CanReceiveStarReading(RequireAllyTarget(state, actor, targetCharacterId)))
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                break;
            case "fate-mark":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
            case "predatory-gaze":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
            case "dark-pact":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "supply-basket":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
        }
    }

    private static bool CanReceiveStarReading(CharacterState target) =>
        target.IsAlive
        && target.Definition.AttackType == DamageType.Magical
        && target.AttackUsesThisTurn > 0
        && target.HasActed
        && !IsActiveAttackBlocked(target);

    private void ResolvePreyZeroDamage(GameState state, DamagePacket packet)
    {
        if (packet.Amount != 0 || !packet.TargetCharacter.IsAlive)
            return;

        foreach (var prey in packet.TargetCharacter.Statuses.OfType<PreyStatus>().Where(status => !status.Expired).ToArray())
            DealAbsoluteDamage(state, packet.TargetCharacter, PreyStatus.AbsoluteDamage, prey.SourceCharacterId, L10n.RoleAction("predatory-gaze"));
    }

    private void ResolvePactAfterActiveAttack(GameState state, CharacterState attacker, CharacterState target)
    {
        foreach (var status in attacker.Statuses.OfType<PactStatus>().Where(status => !status.Expired).ToArray())
        {
            if (target.IsAlive)
                DealAbsoluteDamage(state, target, status.Magnitude, status.SourceCharacterId, L10n.Status("pact"));
            status.Consume();
        }
    }

    internal static void AddBurning(CharacterState target, Guid sourceCharacterId, int stacks = 1)
    {
        var burning = target.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired);
        if (burning is not null)
        {
            burning.AddStacks(stacks);
            return;
        }

        target.Statuses.Add(new BurningStatus(sourceCharacterId, target.PlayerId, stacks));
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
