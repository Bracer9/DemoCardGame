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
    public void GainBattlePoint(PlayerState player, int amount, string reasonId) =>
        _engine.TryGainBp(State, player, amount, reasonId);

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

    public int GetActiveAttack(CharacterState character) => _engine.GetActiveAttack(State, character);
    public int GetMaxHp(CharacterState character) => _engine.GetMaxHp(character);
    public void NotifyDebuffApplied(
        CharacterState source,
        CharacterState target,
        string statusId,
        bool wasNewDistinct) =>
        _engine.NotifyDebuffApplied(State, source, target, statusId, wasNewDistinct);
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
    private sealed record ActiveHealingResult(CharacterState Target, int Requested, int Restored)
    {
        public int Overheal => Math.Max(0, Requested - Restored);
    }

    public const int MaxActionPoints = 5;
    public const int FirstShieldCost = 2;
    public const int ReinforcedShieldCost = 1;
    public const int FirstShieldValue = 2;
    public const int ReinforcedShieldBonus = 2;
    public const int DefenseCommandReinforcedShieldCap = 5;
    public const int MaxDefenseCommandReinforceSourceShield = 3;
    public const int CounterAttackPenalty = 1;
    public const int InitialBattlePoints = 5;
    public const int MaxBattlePoints = 20;
    public const int BattlePointGainCapPerTurn = 5;
    public const int FirstRewardRound = 4;
    public const int RewardRoundInterval = 4;
    public const int RewardSkipBattlePoints = 2;
    public const int ThreadCutMoraleDamagePerMark = 2;
    private const int RelicBaseSelectionWeight = 10;
    private const int RelicEarlyEpicSelectionWeight = 5;
    private const int RelicHeroTagSelectionMultiplier = 2;
    private const int EpicRelicWeightBonusRound = 12;
    private const string BpReasonTurnStart = "turn-start";
    private const string BpReasonBreakEnemyShield = "break-enemy-shield";
    private const string BpReasonOwnTurnEnemyHpDamage = "own-turn-enemy-hp-damage";
    private const string BpReasonShieldFullyDeployed = "shield-fully-deployed";
    private const string BpReasonRewardSkip = "reward-skip";
    private const string BpReasonRoleActionCleanse = "role-action-cleanse";
    private const string BpReasonRoleActionDarkPact = "role-action-dark-pact";
    private const string BpReasonFirstRoleAction = "first-role-action";
    private const string BpReasonRank3Kill = "rank3-kill";
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

    public static int GetMaxActionPoints(PlayerState player) =>
        MaxActionPoints + player.Characters.Count(character =>
            character.Definition.CardType == CardType.Hero
            && character.HeroRank >= 3
            && character.IsAlive
            && character.IsInBattle)
        + (HasActiveFuneralCoin(player) ? 2 : 0);

    private static bool HasActiveFuneralCoin(PlayerState player) =>
        RelicEffects.HasRelic(player, "relic-funeral-coin")
        && player.Characters.Any(character =>
            character.Definition.Key == "princess"
            && character.Zone == CharacterZone.Defeated)
        && player.Characters.Any(character =>
            character.Definition.Key == "monster"
            && character.IsAlive
            && character.IsInBattle);

    private static void ClampActionPointsToActiveMax(GameState state) =>
        state.ActionPoints = Math.Min(state.ActionPoints, GetMaxActionPoints(state.ActivePlayer));

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
            character.IsAlive && !IsDeploying(character) && character.Definition.Key == "oracle");
        if (oracle is null)
            return;

        foreach (var character in player.Characters.Where(character => character.IsAlive && !IsDeploying(character)))
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
            foreach (var definition in CharacterCatalog.Heroes)
                AddHero(player, definition, CharacterZone.DraftCandidate);
        }

        OpenPendingHeroDraft(state, state.ActivePlayer, HeroDraftKind.TestOpening);
    }

    private void OpenPendingHeroDraft(GameState state, PlayerState player, HeroDraftKind kind, string? rewardInstanceId = null)
    {
        var candidates = kind is HeroDraftKind.Opening or HeroDraftKind.TestOpening
            ? player.Characters
                .Where(character => character.Zone == CharacterZone.DraftCandidate)
                .OrderBy(character => character.Slot)
                .Select(character => character.Definition)
                .ToList()
            : kind is HeroDraftKind.SoldierOpening or HeroDraftKind.SoldierRecruit
                ? CreateSoldierDraftCandidates(player)
            : CreateHeroDraftCandidates(player);

        if (candidates.Count == 0)
            throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));

        state.PendingHeroDraft = new PendingHeroDraftState
        {
            PlayerId = player.Id,
            Kind = kind,
            RewardInstanceId = rewardInstanceId,
            MaxSelections = 1
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

        return CharacterCatalog.Heroes
            .Where(definition => !ownedKeys.Contains(definition.Key))
            .OrderBy(_ => _random.Next())
            .Take(4)
            .ToList();
    }

    private IReadOnlyList<CharacterDefinition> CreateSoldierDraftCandidates(PlayerState player) =>
        CharacterCatalog.Soldiers
            .OrderBy(definition => definition.Key)
            .ToList();

    public void SelectHeroDraft(GameState state, Guid playerId, string characterKey)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (!draft.CandidateKeys.Contains(characterKey, StringComparer.OrdinalIgnoreCase))
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        var player = state.Players.Single(item => item.Id == playerId);
        if (draft.Kind is HeroDraftKind.SoldierOpening or HeroDraftKind.SoldierRecruit)
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        if (player.Characters.Any(character => character.IsInBattle
                && string.Equals(character.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase)))
            throw new GameRuleException(L10n.Text("error.heroAlreadyOwned"));

        if (draft.Kind == HeroDraftKind.Recruit)
            ConfirmPendingRewardPurchase(state, player, draft.RewardInstanceId);

        var character = draft.Kind switch
        {
            HeroDraftKind.Opening => ConfirmOpeningHero(player, characterKey),
            HeroDraftKind.TestOpening => AddTestOpeningHero(player, characterKey),
            _ => AddHero(player, CharacterCatalog.Heroes.Single(item =>
                string.Equals(item.Key, characterKey, StringComparison.OrdinalIgnoreCase)))
        };
        if (draft.Kind == HeroDraftKind.Recruit)
            AddDeploying(state, character);

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

            if (!state.IsTestMode)
            {
                var firstPlayer = state.Players.Single(item => item.Id == (state.OpeningFirstPlayerId ?? state.ActivePlayerId));
                state.ActivePlayerId = firstPlayer.Id;
                OpenPendingHeroDraft(state, firstPlayer, HeroDraftKind.SoldierOpening);
                return;
            }

            StartBattleAfterDrafts(state);
            return;
        }

        state.PendingHeroDraft = null;
        if (draft.Kind == HeroDraftKind.Recruit)
            RefreshRewardOptionsIfOpen(state, player);
        EndRewardTurnIfResolved(state);
    }

    public void SelectSoldierDraft(GameState state, Guid playerId, IReadOnlyList<string> characterKeys)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (draft.Kind is not (HeroDraftKind.SoldierOpening or HeroDraftKind.SoldierRecruit))
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        var selectedKeys = characterKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (selectedKeys.Length == 0 || selectedKeys.Length > draft.MaxSelections)
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));
        if (selectedKeys.Any(key => !draft.CandidateKeys.Contains(key, StringComparer.OrdinalIgnoreCase)))
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        var player = state.Players.Single(item => item.Id == playerId);
        if (draft.Kind == HeroDraftKind.SoldierRecruit)
            ConfirmPendingRewardPurchase(state, player, draft.RewardInstanceId);

        foreach (var key in selectedKeys)
        {
            if (player.ActiveCharacterCount >= 4)
                throw new GameRuleException(L10n.Text("error.teamFull"));
            var definition = CharacterCatalog.Soldiers.Single(item =>
                string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
            var soldier = AddHero(player, definition);
            if (draft.Kind == HeroDraftKind.SoldierRecruit)
                AddDeploying(state, soldier);
            Log(state, L10n.Text("log.soldierDraftSelected",
                ("player", L10n.Player(player.Name)),
                ("character", L10n.Character(soldier.Definition.Key)),
                ("characterId", L10n.Raw(soldier.Id))), "system");
        }

        CompleteSoldierDraft(state, player, draft);
    }

    public void UpgradeSoldierFromDraft(GameState state, Guid playerId, string characterKey, Guid targetCharacterId)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (draft.Kind != HeroDraftKind.SoldierRecruit)
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));
        if (!draft.CandidateKeys.Contains(characterKey, StringComparer.OrdinalIgnoreCase))
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        var player = state.Players.Single(item => item.Id == playerId);
        var target = player.Characters.SingleOrDefault(character => character.Id == targetCharacterId)
            ?? throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        if (!target.IsInBattle
            || target.Definition.CardType != CardType.Soldier
            || !string.Equals(target.Definition.Key, characterKey, StringComparison.OrdinalIgnoreCase)
            || target.SoldierRank >= 2)
            throw new GameRuleException(L10n.Text("error.soldierUpgradeInvalid"));

        ConfirmPendingRewardPurchase(state, player, draft.RewardInstanceId);
        PromoteSoldier(target);
        Log(state, L10n.Text("log.soldierRankUp",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("rank", L10n.Raw(target.SoldierRank))), "buff");
        CompleteSoldierDraft(state, player, draft);
    }

    private void CompleteSoldierDraft(GameState state, PlayerState player, PendingHeroDraftState draft)
    {
        state.PendingHeroDraft = null;
        if (draft.Kind == HeroDraftKind.SoldierOpening)
        {
            var nextPlayer = state.Players.FirstOrDefault(item =>
                item.Id != player.Id && item.ActiveCharacterCount == 1);
            if (nextPlayer is not null)
            {
                state.ActivePlayerId = nextPlayer.Id;
                OpenPendingHeroDraft(state, nextPlayer, HeroDraftKind.SoldierOpening);
                return;
            }

            StartBattleAfterDrafts(state);
            return;
        }

        RefreshRewardOptionsIfOpen(state, player);
        EndRewardTurnIfResolved(state);
    }

    private void StartBattleAfterDrafts(GameState state)
    {
        state.PendingHeroDraft = null;
        state.Phase = GamePhase.Playing;
        state.ActivePlayerId = state.OpeningFirstPlayerId ?? state.ActivePlayerId;
        Log(state, L10n.Text("log.firstPlayer", ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
        Log(state, L10n.Text("log.firstTurnNoStartEffects"), "system");
        if (!state.IsTestMode)
            TryOpenRewardWindowForActivePlayer(state);
    }

    public void ResetHeroDraft(GameState state, Guid playerId)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (draft.Kind is not (HeroDraftKind.Recruit or HeroDraftKind.SoldierRecruit))
            throw new GameRuleException(L10n.Text("error.heroDraftResetUnavailable"));

        var player = state.Players.Single(item => item.Id == playerId);
        var cost = GetRewardResetCost(draft.ResetCount);
        if (!TrySpendBp(state, player, cost, BpSpendReasonRewardReroll))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        var candidates = draft.Kind == HeroDraftKind.SoldierRecruit
            ? CharacterCatalog.Soldiers.OrderBy(_ => _random.Next()).ToList()
            : CreateHeroDraftCandidates(player);
        if (candidates.Count == 0)
            throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));

        draft.ResetCount++;
        draft.SelectedKeys.Clear();
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

    private void PromoteSoldier(CharacterState soldier)
    {
        var beforeRank = soldier.SoldierRank;
        soldier.SoldierRank = Math.Min(2, soldier.SoldierRank + 1);
        if (beforeRank < 2 && soldier.SoldierRank >= 2)
            soldier.CurrentHp = Math.Max(soldier.CurrentHp, GetMaxHp(soldier));
        else if (soldier.SoldierRank > beforeRank)
            RecoverRankUpHp(soldier);

        if (soldier.SoldierRank >= 2)
        {
            var actionId = soldier.Definition.Key switch
            {
                "cleric" => "mend",
                "shieldmaiden" => "aegis-formation",
                "duelist" => "crimson-lunge",
                "arcanist" => "astral-focus",
                _ => null
            };
            if (actionId is not null && !soldier.RoleActionIds.Contains(actionId, StringComparer.OrdinalIgnoreCase))
            soldier.RoleActionIds.Add(actionId);
        }
    }

    private int RecoverRankUpHp(CharacterState character)
    {
        var amount = (int)Math.Ceiling(GetMaxHp(character) * 0.5);
        return Heal(character, amount);
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
        var counterDamageBeforeShieldResolution = GetCounterAttack(state, defender);
        var monsterPrincessAttack = IsMonsterPrincessAttack(attacker, defender);
        var attackerAttackType = GetAttackType(attacker);
        var defenderAttackType = GetAttackType(defender);

        var attackPacket = new DamagePacket
        {
            SourceCharacter = attacker,
            TargetCharacter = defender,
            DamageType = monsterPrincessAttack ? DamageType.Absolute : attackerAttackType,
            Source = DamageSource.ActiveAttack,
            Amount = GetEffectiveActiveAttack(state, attacker),
            ReceivesMagicPowerBonus = attackerAttackType == DamageType.Magical,
            IgnoresSharedShield = monsterPrincessAttack,
            IgnoresTargetDefense = monsterPrincessAttack
        };
        var counterPacket = new DamagePacket
        {
            SourceCharacter = defender,
            TargetCharacter = attacker,
            DamageType = defenderAttackType,
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

        TriggerDuelistTicket(state, attacker, defenderOwner);
        ModifyDamage(state, attackPacket);
        ModifyDamage(state, counterPacket);
        var defenderShieldAfterAttackPacket = defenderOwner.SharedShield;

        attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - counterPacket.Amount);
        var actualAttackTarget = attackPacket.TargetCharacter;
        actualAttackTarget.CurrentHp = Math.Max(0, actualAttackTarget.CurrentHp - attackPacket.Amount);
        ResolvePreyZeroDamage(state, attackPacket);
        TriggerRelicsAfterDamageResolved(state, attackPacket);
        ResolveVictoryEdictAfterActiveAttack(state, attacker, actualAttackTarget);
        ResolvePactAfterActiveAttack(state, attacker, actualAttackTarget);
        ResolveAbyssalBargainAfterActiveAttack(state, attacker, actualAttackTarget);
        ResolveGloryRoarAfterActiveAttack(state, attacker);
        ResolveHuntedAfterActiveAttack(state, attacker, actualAttackTarget);
        ResolvePreyZeroDamage(state, counterPacket);
        TriggerRelicsAfterDamageResolved(state, counterPacket);

        var resolvedCollateral = attackPacket.Collateral
            .Concat(counterPacket.Collateral)
            .Where(collateral => !IsDeploying(collateral.Target))
            .Select(collateral => (Collateral: collateral, Packet: ResolveCollateralDamage(state, collateral)))
            .ToArray();

        ResolveEchoCrystalAfterDamageSequence(state, attackPacket);
        ResolveEchoCrystalAfterDamageSequence(state, counterPacket);
        foreach (var item in resolvedCollateral)
            ResolveEchoCrystalAfterDamageSequence(state, item.Packet);

        attacker.AttackUsesThisTurn++;
        state.ActionPoints -= attacker.Definition.Cost;
        ApplyPendingRelicActionPointRefunds(state, attacker);
        state.ActionsTakenThisTurn++;
        state.ActiveAttacksTakenThisTurn++;
        if (attackerAttackType == DamageType.Physical)
            state.PhysicalActiveAttacksTakenThisTurn++;

        foreach (var note in attackPacket.Notes.Concat(counterPacket.Notes).Concat(resolvedCollateral.SelectMany(item => item.Packet.Notes)))
            Log(state, note, "status");

        Log(state, L10n.Text("log.exchange",
                ("attacker", L10n.Character(attacker.Definition.Key)),
                ("attackerId", L10n.Raw(attacker.Id)),
                ("defender", L10n.Character(actualAttackTarget.Definition.Key)),
                ("defenderId", L10n.Raw(actualAttackTarget.Id)),
                ("attackDamage", L10n.Raw(attackPacket.Amount)),
                ("attackMoraleDamage", L10n.Raw(attackPacket.MoraleDamage)),
                ("attackType", L10n.Damage(attackPacket.DamageType)),
                ("attackDefenseReduced", L10n.Raw(attackPacket.DefenseReduced)),
                ("attackShieldAbsorbed", L10n.Raw(attackPacket.ShieldAbsorbed)),
                ("counterDamage", L10n.Raw(counterPacket.Amount)),
                ("counterMoraleDamage", L10n.Raw(counterPacket.MoraleDamage)),
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

        TryAwardEnemyShieldBreakBp(state, attacker, attackerOwner, defenderOwner, defenderShieldBefore,
            attackPacket.ShieldAbsorbed, shieldRemainingAfterDamage: defenderShieldAfterAttackPacket);
        TryAwardEnemyDamageBp(state, attackerOwner, defenderOwner, attackPacket.Amount);
        TryResolveRageShieldBreak(state, attacker, actualAttackTarget, defenderOwner, defenderShieldBefore, attackPacket.ShieldAbsorbed);
        if (attackerAttackType == DamageType.Magical && attackPacket.Amount > 0)
        {
            TriggerArcaneResonance(state, attacker, fromRoleAction: false);
            TriggerDeputyArcanist(state, attacker, fromRoleAction: false);
        }
        TriggerDeputyDuelistAfterActiveAttack(state, attacker, actualAttackTarget, attackPacket.Amount);

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

    private static bool IsMonsterPrincessAttack(CharacterState attacker, CharacterState defender) =>
        attacker.Definition.Key == "monster" && defender.Definition.Key == "princess";

    public void DeployShield(GameState state)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        var player = state.ActivePlayer;
        var isReinforcing = CanReinforceShield(player);
        if (player.SharedShield > 0 && !isReinforcing)
            throw new GameRuleException(L10n.Text("error.shieldMaxed"));
        var shieldCost = GetShieldCost(player);
        if (state.ActionPoints < shieldCost)
            throw new GameRuleException(L10n.Text("error.notEnoughAp"));

        state.ActionPoints -= shieldCost;
        player.ShieldDeploymentsThisTurn++;
        if (isReinforcing)
        {
            player.SharedShield = Math.Min(DefenseCommandReinforcedShieldCap, player.SharedShield + ReinforcedShieldBonus);
        }
        else
        {
            player.SharedShield = FirstShieldValue;
        }
        var logKey = isReinforcing ? "log.shieldReinforced" : "log.shieldDeployed";
        Log(state, L10n.Text(logKey,
            ("player", L10n.Player(player.Name)),
            ("shield", L10n.Raw(player.SharedShield))), "shield");
        TriggerShieldDrill(state, player.Characters.FirstOrDefault(character =>
            character.IsAlive && character.Definition.TraitId == "shield-drill"));
        TriggerMasonToken(state, player);
        if (isReinforcing && !CanReinforceShield(player))
            TryGainBp(state, player, 1, BpReasonShieldFullyDeployed);
    }

    public static bool CanReinforceShield(PlayerState player) =>
        player.SharedShield > 0 && player.SharedShield <= MaxDefenseCommandReinforceSourceShield;

    public static bool CanDeployShield(GameState state)
    {
        var player = state.ActivePlayer;
        return state.ActionPoints >= GetShieldCost(player)
            && (player.SharedShield <= 0 || CanReinforceShield(player));
    }

    public static int GetShieldCost(PlayerState player) =>
        CanReinforceShield(player) ? ReinforcedShieldCost : FirstShieldCost;

    public void EndTurn(GameState state)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        var context = new GameEngineContext(this, state);

        foreach (var character in state.Players.SelectMany(player => player.Characters))
        {
            foreach (var status in character.Statuses.ToArray())
                status.OnTurnEnd(context, character);
            character.Statuses.RemoveAll(status => status.Expired);
        }

        RecoverMoraleAtTurnEnd(state.ActivePlayer);

        state.ActivePlayerId = state.Opponent.Id;
        state.TurnNumber++;
        state.ActionPoints = GetMaxActionPoints(state.ActivePlayer);
        ApplyPendingActionPointDebt(state, state.ActivePlayer);
        state.ActionsTakenThisTurn = 0;
        state.ActiveAttacksTakenThisTurn = 0;
        state.PhysicalActiveAttacksTakenThisTurn = 0;
        state.PendingRelicActionPointRefunds = 0;
        state.ActivePlayer.BattlePoints.GainedThisTurn = 0;
        state.ActivePlayer.FirstRoleActionBpGrantedThisTurn = false;
        state.ActivePlayer.DeputyPassivesUsedThisTurn.Clear();
        state.ActivePlayer.RelicsUsedThisTurn.Clear();

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
            character.TraitsUsedThisTurn.Clear();
            character.RoleActionsUsedThisTurn.Clear();
            TickRoleActionCooldowns(character);
        }

        Log(state, L10n.Text("log.turnStart",
            ("turn", L10n.Raw(state.TurnNumber)),
            ("player", L10n.Player(state.ActivePlayer.Name))), "turn");
        TryGainBp(state, state.ActivePlayer, 1, BpReasonTurnStart);
        TriggerKingwallStandard(state, state.ActivePlayer);
        ProcessTurnStart(state);
        TryOpenRewardWindowForActivePlayer(state);
        ResolveDefeats(state);
        EvaluateGameEnd(state);
    }

    private static void RecoverMoraleAtTurnEnd(PlayerState player)
    {
        var recovery = Math.Max(0, player.BattlePoints.GainedThisTurn);
        if (recovery <= 0)
            return;

        foreach (var character in player.Characters.Where(character => character.IsAlive))
            character.Morale = Math.Min(character.MaxMorale, character.Morale + recovery);
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

    public int GetActiveAttack(GameState state, CharacterState character)
    {
        var damage = GetBaseAttack(state, character);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            damage = status.ModifyActiveAttack(character, damage);
        return Math.Max(0, damage);
    }

    public int GetEffectiveActiveAttack(GameState state, CharacterState character) =>
        GetActiveAttack(state, character);

    public int GetActiveAttack(CharacterState character)
    {
        var damage = GetBaseAttack(character);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            damage = status.ModifyActiveAttack(character, damage);
        return Math.Max(0, damage);
    }

    public int GetBaseAttack(GameState state, CharacterState character)
    {
        var owner = state.FindOwner(character);
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var attack = character.Definition.Attack
            + growth.Attack
            + GetDeputyAttackBonus(character)
            + GetSoldierAttackAuraBonus(state, character);
        attack = RelicEffects.ModifyBaseAttack(owner, character, attack);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            attack = status.ModifyBaseAttack(attack);
        return Math.Max(0, attack);
    }

    public int GetBaseAttack(CharacterState character)
    {
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var attack = character.Definition.Attack + growth.Attack + GetDeputyAttackBonus(character);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            attack = status.ModifyBaseAttack(attack);
        return Math.Max(0, attack);
    }

    public int GetCounterAttack(GameState state, CharacterState character) =>
        IsCounterAttackBlocked(character)
            ? 0
            : GetBaseCounterAttack(GetBaseAttack(state, character));

    public int GetCounterAttack(CharacterState character) =>
        IsCounterAttackBlocked(character)
            ? 0
            : GetBaseCounterAttack(GetBaseAttack(character));

    public static int GetBaseCounterAttack(int attack) => Math.Max(1, attack - CounterAttackPenalty);

    public static DamageType GetAttackType(CharacterState character) =>
        character.Definition.Key == "princess"
        && character.HeroRank >= 3
        && string.Equals(character.HeroPathRoleActionId, "royal-command", StringComparison.OrdinalIgnoreCase)
            ? DamageType.Physical
            : character.Definition.AttackType;

    public static bool IsCounterAttackBlocked(CharacterState character) =>
        character.Statuses.Any(status => status.Id == "trembling" && !status.Expired);

    public static bool IsActiveAttackBlocked(CharacterState character) =>
        character.Statuses.Any(status => status.BlocksActiveAttack && !status.Expired);

    public int GetPhysicalDefense(GameState state, CharacterState character)
    {
        var owner = state.FindOwner(character);
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var defense = character.Definition.PhysicalDefense
            + growth.PhysicalDefense
            + GetDeputyStatBonus(character, DeputyStatKind.PhysicalDefense)
            + GetSoldierDefenseAuraBonus(state, character, DamageType.Physical);
        defense = RelicEffects.ModifyPhysicalDefense(owner, defense);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyPhysicalDefense(defense);
        return defense;
    }

    public int GetPhysicalDefense(CharacterState character)
    {
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var defense = character.Definition.PhysicalDefense + growth.PhysicalDefense + GetDeputyStatBonus(character, DeputyStatKind.PhysicalDefense);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyPhysicalDefense(defense);
        return defense;
    }

    public int GetMagicalDefense(GameState state, CharacterState character)
    {
        var owner = state.FindOwner(character);
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var defense = character.Definition.MagicalDefense
            + growth.MagicalDefense
            + GetDeputyStatBonus(character, DeputyStatKind.MagicalDefense)
            + GetSoldierDefenseAuraBonus(state, character, DamageType.Magical);
        defense = RelicEffects.ModifyMagicalDefense(owner, defense);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyMagicalDefense(defense);
        return defense;
    }

    public int GetMagicalDefense(CharacterState character)
    {
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var defense = character.Definition.MagicalDefense + growth.MagicalDefense + GetDeputyStatBonus(character, DeputyStatKind.MagicalDefense);
        foreach (var status in character.Statuses.Where(status => !status.Expired))
            defense = status.ModifyMagicalDefense(defense);
        return defense;
    }

    public int GetMaxHp(CharacterState character)
    {
        var growth = HeroGrowthCatalog.GetTotalStats(character);
        var maxHp = character.Definition.Key switch
        {
            "cleric" when character.SoldierRank >= 1 => character.Definition.MaxHp + 2,
            "shieldmaiden" when character.SoldierRank >= 1 => character.Definition.MaxHp + 2,
            "duelist" when character.SoldierRank >= 1 => character.Definition.MaxHp + 1,
            "arcanist" when character.SoldierRank >= 1 => character.Definition.MaxHp + 2,
            _ => character.Definition.MaxHp
        };
        maxHp += growth.MaxHp;
        if (character.Definition.CardType == CardType.Soldier && character.SoldierRank >= 2)
            maxHp += 5;
        return maxHp + GetDeputyStatBonus(character, DeputyStatKind.MaxHp);
    }

    private int Heal(CharacterState target, int amount)
    {
        if (amount <= 0)
            return 0;

        var before = target.CurrentHp;
        var cappedHp = Math.Min(GetMaxHp(target), target.CurrentHp + amount);
        target.CurrentHp = Math.Max(target.CurrentHp, cappedHp);
        return target.CurrentHp - before;
    }

    private ActiveHealingResult HealFromRoleAction(CharacterState target, int amount)
    {
        var requested = Math.Max(0, amount);
        return new ActiveHealingResult(target, requested, Heal(target, requested));
    }

    private static int GetDeputyStatBonus(CharacterState character, DeputyStatKind kind)
    {
        if (!character.IsInBattle || character.CurrentHp <= 0)
            return 0;
        var deputy = DeputyCatalog.FindById(character.DeputyEffectId);
        return deputy is not null && deputy.StatKind == kind ? deputy.StatValue : 0;
    }

    private static int GetDeputyAttackBonus(CharacterState character)
    {
        if (!character.IsInBattle || character.CurrentHp <= 0)
            return 0;
        var deputy = DeputyCatalog.FindById(character.DeputyEffectId);
        if (deputy is null)
            return 0;
        return deputy.StatKind switch
        {
            DeputyStatKind.Attack => deputy.StatValue,
            DeputyStatKind.PhysicalAttack when GetAttackType(character) == DamageType.Physical => deputy.StatValue,
            DeputyStatKind.MagicalAttack when GetAttackType(character) == DamageType.Magical => deputy.StatValue,
            _ => 0
        };
    }

    private static int GetSoldierAttackAuraBonus(GameState state, CharacterState character)
    {
        if (!character.IsInBattle || character.CurrentHp <= 0 || IsDeploying(character))
            return 0;

        var owner = state.FindOwner(character);
        return GetAttackType(character) switch
        {
            DamageType.Physical when HasActiveRank1SoldierAura(owner, "duelist") => 2,
            DamageType.Magical when HasActiveRank1SoldierAura(owner, "arcanist") => 2,
            _ => 0
        };
    }

    public int GetAttackAuraBonus(GameState state, CharacterState character) =>
        GetSoldierAttackAuraBonus(state, character);

    private static int GetSoldierDefenseAuraBonus(GameState state, CharacterState character, DamageType damageType)
    {
        if (!character.IsInBattle || character.CurrentHp <= 0 || IsDeploying(character))
            return 0;

        var owner = state.FindOwner(character);
        return damageType switch
        {
            DamageType.Physical when HasActiveRank1SoldierAura(owner, "shieldmaiden") => 1,
            DamageType.Magical when HasActiveRank1SoldierAura(owner, "cleric") => 1,
            _ => 0
        };
    }

    public static bool HasActiveRank1SoldierAura(PlayerState player, string soldierKey) =>
        player.Characters.Any(character =>
            IsActiveRank1SoldierAuraSource(player, character)
            && character.SoldierRank >= 1
            && string.Equals(character.Definition.Key, soldierKey, StringComparison.OrdinalIgnoreCase));

    private static bool IsActiveRank1SoldierAuraSource(PlayerState player, CharacterState soldier)
    {
        if (soldier.Definition.CardType != CardType.Soldier || soldier.CurrentHp <= 0 || IsDeploying(soldier))
            return false;
        if (soldier.IsAlive && soldier.IsInBattle)
            return true;
        if (soldier.Zone != CharacterZone.Deputy || soldier.DeputyHostHeroId is not { } hostId)
            return false;

        return player.Characters.Any(character =>
            character.Id == hostId
            && character.DeputySoldierId == soldier.Id
            && character.IsAlive
            && character.IsInBattle);
    }

    public int GetDefense(GameState state, CharacterState character, DamageType damageType) => damageType switch
    {
        DamageType.Physical => GetPhysicalDefense(state, character),
        DamageType.Magical => GetMagicalDefense(state, character),
        _ => 0
    };

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

    public static bool IsDeploying(CharacterState character) =>
        character.Statuses.Any(status => status.Id == "deploying" && !status.Expired);

    private static void AddDeploying(GameState state, CharacterState character)
    {
        character.Statuses.RemoveAll(status => status.Id == "deploying");
        character.Statuses.Add(new DeployingStatus(character.Id, character.PlayerId, GetDeployingReadyTurnNumber(state)));
    }

    private static int GetDeployingReadyTurnNumber(GameState state) =>
        CurrentRoundNumber(state) * 2 + 3;

    public IReadOnlyList<CharacterRoleAction> GetRoleActionUpgradeChoices(CharacterState character) =>
        character.IsAlive && character.Definition.CardType == CardType.Hero && character.HeroRank == 0
            ? _roleActions.GetUpgradeChoices(character.Definition.Key)
            : [];

    public bool CanUpgradeHeroRank(CharacterState character) =>
        character.IsAlive
        && character.Definition.CardType == CardType.Hero
        && character.HeroRank < 3
        && (character.HeroRank == 0
            ? _roleActions.GetUpgradeChoices(character.Definition.Key).Count > 0
            : HeroGrowthCatalog.Find(character) is not null);

    public string? GetAssignDeputyDisabledReason(GameState state, CharacterState soldier)
    {
        if (soldier.Definition.CardType != CardType.Soldier)
            return "not-soldier";
        if (soldier.Zone == CharacterZone.Deputy || soldier.DeputyHostHeroId.HasValue)
            return "already-deputy";
        if (soldier.SoldierRank < 2)
            return "not-rank2";
        if (!soldier.IsAlive || !soldier.IsInBattle)
            return "not-in-battle";
        if (state.Phase != GamePhase.Playing || state.RewardWindow is not null
            || state.PendingRoleActionUpgrade is not null || state.PendingHeroDraft is not null)
            return "not-playing";
        if (soldier.PlayerId != state.ActivePlayerId)
            return "opponent-turn";
        if (soldier.AttackUsesThisTurn > 0 || soldier.RoleActionsUsedThisTurn.Count > 0)
            return "already-acted";
        if (DeputyCatalog.FindBySoldierKey(soldier.Definition.Key) is null)
            return "no-deputy-definition";
        var owner = state.FindOwner(soldier);
        return owner.Characters.Any(IsLegalDeputyHost)
            ? null
            : "no-legal-hero";
    }

    public void AssignDeputy(GameState state, Guid soldierId, Guid heroId)
    {
        EnsurePlaying(state);
        EnsureNoRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);

        var soldier = state.FindCharacter(soldierId);
        var hero = state.FindCharacter(heroId);
        var reason = GetAssignDeputyDisabledReason(state, soldier);
        if (reason is not null)
            throw new GameRuleException(L10n.Text($"error.assignDeputy.{reason}"));
        if (hero.PlayerId != soldier.PlayerId || !IsLegalDeputyHost(hero))
            throw new GameRuleException(L10n.Text("error.assignDeputy.invalidHero"));

        var definition = DeputyCatalog.FindBySoldierKey(soldier.Definition.Key)
            ?? throw new GameRuleException(L10n.Text("error.assignDeputy.no-deputy-definition"));

        soldier.Zone = CharacterZone.Deputy;
        soldier.DeputyHostHeroId = hero.Id;
        soldier.DeputyEffectId = definition.Id;
        hero.DeputySoldierId = soldier.Id;
        hero.DeputyEffectId = definition.Id;
        hero.CurrentHp = Math.Min(GetMaxHp(hero), hero.CurrentHp);

        Log(state, L10n.Text("log.deputyAssigned",
            ("soldier", L10n.Character(soldier.Definition.Key)),
            ("soldierId", L10n.Raw(soldier.Id)),
            ("hero", L10n.Character(hero.Definition.Key)),
            ("heroId", L10n.Raw(hero.Id)),
            ("deputy", L10n.Deputy(definition.Id))), "buff");
    }

    private static bool IsLegalDeputyHost(CharacterState character) =>
        character.Definition.CardType == CardType.Hero
        && character.IsAlive
        && character.IsInBattle
        && !IsDeploying(character)
        && character.DeputySoldierId is null
        && character.DeputyEffectId is null;

    private void ProcessTurnStart(GameState state)
    {
        var context = new GameEngineContext(this, state);
        ExpireReadyDeployingStatuses(state);
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

        GrantStartingMagicPower(state.ActivePlayer);

        ResolveDefeats(state);

        foreach (var character in state.ActivePlayer.Characters.Where(character => character.IsAlive).ToArray())
        {
            if (!IsDeploying(character))
                _traits.Get(character.Definition.TraitId).OnTurnStart(context, character);
        }
    }

    private static void ExpireReadyDeployingStatuses(GameState state)
    {
        foreach (var character in state.Players.SelectMany(player => player.Characters))
        {
            foreach (var status in character.Statuses.OfType<DeployingStatus>().Where(status => !status.Expired).ToArray())
                status.ExpireIfReady(state.TurnNumber);
            character.Statuses.RemoveAll(status => status.Expired);
        }
    }

    private void ModifyDamage(GameState state, DamagePacket packet)
    {
        var sourceOwner = state.FindOwner(packet.SourceCharacter);
        ApplyCompanyStandard(state, sourceOwner, packet);
        TriggerRedHourglass(state, sourceOwner, packet);
        ApplyPredatorCrown(state, sourceOwner, packet);
        foreach (var status in packet.SourceCharacter.Statuses.Where(status => !status.Expired))
            status.ModifyOutgoingDamage(new GameEngineContext(this, state), packet.SourceCharacter, packet);

        foreach (var traitOwner in sourceOwner.Characters.Where(character => character.IsAlive && !IsDeploying(character)))
            _traits.Get(traitOwner.Definition.TraitId).ModifyOutgoingDamage(
                new GameEngineContext(this, state), traitOwner, packet);

        var targetOwner = state.FindOwner(packet.TargetCharacter);
        if (!packet.IgnoresSharedShield)
            ApplySharedShield(targetOwner, packet);
        if (packet.BlockedBySharedShield)
        {
            packet.Amount = 0;
            return;
        }
        if (!packet.IgnoresTargetDefense)
            ApplyTypeDefense(state, packet);

        foreach (var status in packet.TargetCharacter.Statuses.Where(status => !status.Expired))
            status.ModifyIncomingDamage(new GameEngineContext(this, state), packet.TargetCharacter, packet);

        var incomingModifiers = targetOwner.Characters
            .Where(character => character.IsAlive && !IsDeploying(character))
            .OrderBy(character => _traits.Get(character.Definition.TraitId).IncomingModifierPriority)
            .ToArray();

        foreach (var traitOwner in incomingModifiers)
            _traits.Get(traitOwner.Definition.TraitId).ModifyIncomingDamage(
                new GameEngineContext(this, state), traitOwner, packet);

        if (packet.DamageType == DamageType.Absolute)
            ApplyDirectHpDamage(packet);
        else
            ApplyMoraleDamage(packet.TargetCharacter, packet);
    }

    private static void ApplyDirectHpDamage(DamagePacket packet)
    {
        var finalDamage = Math.Max(0, packet.Amount);
        packet.FinalCharacterDamage = finalDamage;
        packet.MoraleDamage = 0;
        packet.HpDamage = finalDamage;
        packet.Amount = finalDamage;
    }

    private static void ApplyMoraleDamage(CharacterState target, DamagePacket packet)
    {
        var finalDamage = Math.Max(0, packet.Amount);
        packet.FinalCharacterDamage = finalDamage;
        if (finalDamage <= 0)
        {
            packet.Amount = 0;
            return;
        }

        var moraleDamage = Math.Min(Math.Max(0, target.Morale), finalDamage);
        target.Morale = Math.Max(0, target.Morale - moraleDamage);
        var hpDamage = Math.Max(0, finalDamage - moraleDamage);

        packet.MoraleDamage = moraleDamage;
        packet.HpDamage = hpDamage;
        packet.Amount = hpDamage;
    }

    private void ApplyTypeDefense(GameState state, DamagePacket packet)
    {
        if (packet.Amount <= 0)
            return;

        var defense = GetDefense(state, packet.TargetCharacter, packet.DamageType);
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
        if (IsDeploying(collateral.Target))
            return new DamagePacket
            {
                SourceCharacter = collateral.Source,
                TargetCharacter = collateral.Target,
                DamageType = collateral.DamageType,
                Source = DamageSource.Trait,
                Amount = 0
            };

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
        TriggerRelicsAfterDamageResolved(state, packet);
        TryAwardEnemyShieldBreakBp(state, collateral.Source, sourceOwner, targetOwner, shieldBefore,
            packet.ShieldAbsorbed, deferActionPointRefund: true);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        return packet;
    }

    private static void ApplySharedShield(PlayerState targetOwner, DamagePacket packet)
    {
        if (packet.Amount <= 0 || targetOwner.SharedShield <= 0)
            return;

        ApplyShieldTypeDefense(targetOwner, packet);
        if (packet.Amount <= 0)
        {
            packet.BlockedBySharedShield = true;
            return;
        }

        var absorbed = Math.Min(packet.Amount, targetOwner.SharedShield);
        packet.Amount = 0;
        packet.BlockedBySharedShield = true;
        packet.ShieldAbsorbed += absorbed;
        targetOwner.SharedShield -= absorbed;
        ClearShieldLayerIfBroken(targetOwner);
        packet.Notes.Add(L10n.Text("note.shieldAbsorb",
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
            ("damageType", L10n.Damage(packet.DamageType)),
            ("amount", L10n.Raw(absorbed))));
    }

    private static void ClearShieldLayerIfBroken(PlayerState player)
    {
        if (player.SharedShield > 0)
            return;

        player.SharedShield = 0;
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
        if (!ShouldOpenRewardWindowForActivePlayer(state))
            return;

        var round = CurrentRoundNumber(state);

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

    private bool ShouldOpenRewardWindowForActivePlayer(GameState state)
    {
        if (state.IsTestMode)
            return false;
        if (state.Phase != GamePhase.Playing || state.RewardWindow is not null)
            return false;

        var round = CurrentRoundNumber(state);
        if (!IsRewardRound(round))
            return false;

        return !state.ResolvedRewardWindows.Contains(RewardWindowKey(state.ActivePlayer.Id, round));
    }

    private static int CurrentRoundNumber(GameState state) => (state.TurnNumber + 1) / 2;

    public RewardKind SelectReward(GameState state, string instanceId)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        if (state.PendingRelicReward is { PlayerId: var pendingRelicPlayerId } pendingRelic
            && pendingRelicPlayerId == state.ActivePlayerId)
            return SelectPendingRelicReward(state, window, pendingRelic, instanceId);

        var option = window.Options.SingleOrDefault(item => item.InstanceId == instanceId);
        if (option is null)
            throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        var definition = RewardCatalog.All.SingleOrDefault(item => item.Id == option.RewardId)
            ?? throw new GameRuleException(L10n.Text("error.rewardNotFound"));
        var player = state.ActivePlayer;
        if (player.BattlePoints.Current < option.Cost)
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        if (definition.Kind == RewardKind.HeroRoleActionUpgrade)
        {
            if (!HasEligibleRoleActionUpgradeTarget(player))
                throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));
            state.PendingRoleActionUpgrade = new PendingRoleActionUpgradeState
            {
                PlayerId = player.Id,
                RewardId = option.RewardId,
                RewardInstanceId = option.InstanceId
            };
            Log(state, L10n.Text("log.roleActionUpgradePending",
                ("player", L10n.Player(player.Name))), "system");
        }
        else if (definition.Kind == RewardKind.HeroRecruit)
        {
            if (!CanRecruitHero(player))
                throw new GameRuleException(L10n.Text("error.noHeroRecruitTarget"));
            OpenPendingHeroDraft(state, player, HeroDraftKind.Recruit, option.InstanceId);
        }
        else if (definition.Kind == RewardKind.SoldierRecruit)
        {
            if (!CanRecruitSoldier(player))
                throw new GameRuleException(L10n.Text("error.noSoldierRecruitTarget"));
            OpenPendingHeroDraft(state, player, HeroDraftKind.SoldierRecruit, option.InstanceId);
        }
        else if (definition.Kind == RewardKind.RelicChoice)
        {
            state.PendingRelicReward = new PendingRelicRewardState
            {
                PlayerId = player.Id,
                RewardId = option.RewardId,
                RewardInstanceId = option.InstanceId,
                ResetCount = window.RelicResetCount
            };
            state.PendingRelicReward.Options.AddRange(window.RelicOptions);
        }
        else
        {
            ConfirmRewardPurchase(state, window, player, option);
            ApplyDummyReward(state, player, option.RewardId);
            RefreshRewardOptionsIfOpen(state, player);
        }

        return definition.Kind;
    }

    public void ResetRewardWindow(GameState state)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        var player = state.ActivePlayer;
        if (state.PendingRelicReward is { PlayerId: var pendingRelicPlayerId } pendingRelic
            && pendingRelicPlayerId == player.Id)
        {
            ResetPendingRelicReward(state, window, pendingRelic, player);
            return;
        }

        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        EnsureNoPendingRelicReward(state);
        throw new GameRuleException(L10n.Text("error.rewardResetUnavailable"));
    }

    public void SkipRewardWindow(GameState state)
    {
        EnsurePlaying(state);
        EnsureNoPendingRoleActionUpgrade(state);
        EnsureNoPendingHeroDraft(state);
        EnsureNoPendingRelicReward(state);
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
        options.Add(RewardCatalog.SoldierRecruit);
        options.Add(RewardCatalog.RelicChoice);

        foreach (var reward in options)
            window.Options.Add(new RewardOptionState(
                $"{reward.Id}-{Guid.NewGuid():N}",
                reward.Id,
                GetRewardOptionCost(player, reward)));
        window.RelicResetCount = 0;
        RefreshRelicRewardOptions(window.RelicOptions, player, window.RoundNumber);
    }

    private static int GetRewardOptionCost(PlayerState player, RewardDefinition reward)
    {
        if (reward.Kind == RewardKind.HeroRoleActionUpgrade && !player.HasClaimedFreeHeroRoleActionUpgrade)
            return 0;
        if (reward.Kind == RewardKind.SoldierRecruit
            && RelicEffects.HasRelic(player, "relic-muster-papers"))
            return Math.Max(1, reward.Cost - 1);
        return reward.Cost;
    }

    private static void RefreshRewardOptionCosts(RewardWindowState window, PlayerState player)
    {
        for (var index = 0; index < window.Options.Count; index++)
        {
            var option = window.Options[index];
            var reward = RewardCatalog.All.FirstOrDefault(definition => definition.Id == option.RewardId);
            if (reward is not null)
                window.Options[index] = option with { Cost = GetRewardOptionCost(player, reward) };
        }
    }

    private void RefreshRelicRewardOptions(List<RewardOptionState> options, PlayerState player, int roundNumber)
    {
        options.Clear();
        var owned = player.Relics.Select(relic => relic.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = RewardCatalog.RelicRewards
            .Where(reward => !owned.Contains(reward.Id))
            .ToArray();

        foreach (var reward in SelectWeightedRelicRewards(candidates, player, roundNumber, 3))
        {
            options.Add(new RewardOptionState(
                $"{reward.Id}-{Guid.NewGuid():N}",
                reward.Id,
                GetRewardOptionCost(player, reward)));
        }
    }

    private IReadOnlyList<RewardDefinition> SelectWeightedRelicRewards(
        IReadOnlyCollection<RewardDefinition> candidates,
        PlayerState player,
        int roundNumber,
        int count)
    {
        var heroTags = player.Characters
            .Where(character => character.Definition.CardType == CardType.Hero && character.IsInBattle)
            .SelectMany(HeroGrowthCatalog.GetRelicTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remaining = candidates.ToList();
        var selected = new List<RewardDefinition>(Math.Min(count, remaining.Count));

        while (selected.Count < count && remaining.Count > 0)
        {
            var totalWeight = remaining.Sum(reward => GetRelicSelectionWeight(reward, heroTags, roundNumber));
            var roll = _random.Next(totalWeight);
            var selectedIndex = 0;

            for (var index = 0; index < remaining.Count; index++)
            {
                roll -= GetRelicSelectionWeight(remaining[index], heroTags, roundNumber);
                if (roll >= 0)
                    continue;

                selectedIndex = index;
                break;
            }

            selected.Add(remaining[selectedIndex]);
            remaining.RemoveAt(selectedIndex);
        }

        return selected;
    }

    private static int GetRelicSelectionWeight(
        RewardDefinition reward,
        IReadOnlySet<string> heroTags,
        int roundNumber)
    {
        var relic = RelicCatalog.Find(reward.Id);
        var isEpic = string.Equals(reward.Rarity, "epic", StringComparison.OrdinalIgnoreCase);
        var weight = isEpic && roundNumber < EpicRelicWeightBonusRound
            ? RelicEarlyEpicSelectionWeight
            : RelicBaseSelectionWeight;
        if (relic is not null && relic.BuildTags.Any(heroTags.Contains))
            weight *= RelicHeroTagSelectionMultiplier;
        return weight;
    }

    private RewardKind SelectPendingRelicReward(GameState state, RewardWindowState window,
        PendingRelicRewardState pending, string instanceId)
    {
        var player = state.ActivePlayer;
        var option = pending.Options.SingleOrDefault(item => item.InstanceId == instanceId);
        if (option is null)
            throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        var definition = RewardCatalog.All.SingleOrDefault(item => item.Id == option.RewardId)
            ?? throw new GameRuleException(L10n.Text("error.rewardNotFound"));
        if (definition.Kind != RewardKind.DummyStatus)
            throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        ConfirmRewardPurchase(state, window, player, option);
        ApplyDummyReward(state, player, option.RewardId);
        RefreshRewardOptionCosts(window, player);
        pending.Options.Remove(option);
        var windowOption = window.RelicOptions.SingleOrDefault(item => item.InstanceId == option.InstanceId);
        if (windowOption is not null)
            window.RelicOptions.Remove(windowOption);
        return definition.Kind;
    }

    private void ResetPendingRelicReward(GameState state, RewardWindowState window,
        PendingRelicRewardState pending, PlayerState player)
    {
        var cost = GetRewardResetCost(pending.ResetCount);
        if (!TrySpendBp(state, player, cost, BpSpendReasonRewardReroll))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        pending.ResetCount++;
        window.RelicResetCount = pending.ResetCount;
        RefreshRelicRewardOptions(window.RelicOptions, player, window.RoundNumber);
        pending.Options.Clear();
        pending.Options.AddRange(window.RelicOptions);
        Log(state, L10n.Text("log.rewardReset",
            ("player", L10n.Player(player.Name)),
            ("cost", L10n.Raw(cost)),
            ("count", L10n.Raw(pending.ResetCount))), "system");
    }

    private bool HasEligibleRoleActionUpgradeTarget(PlayerState player) =>
        player.Characters.Any(CanUpgradeHeroRank);

    private static bool CanRecruitHero(PlayerState player)
    {
        var ownedKeys = player.Characters
            .Where(character => character.IsInBattle)
            .Select(character => character.Definition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return player.ActiveCharacterCount < 4
            && CharacterCatalog.Heroes.Any(definition => !ownedKeys.Contains(definition.Key));
    }

    private static bool CanRecruitSoldier(PlayerState player) =>
        CharacterCatalog.Soldiers.Count > 0;

    private void ConfirmPendingRewardPurchase(GameState state, PlayerState player, string? rewardInstanceId)
    {
        if (string.IsNullOrWhiteSpace(rewardInstanceId))
            return;

        var window = RequireActiveRewardWindow(state);
        if (window.PlayerId != player.Id)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));

        var option = window.Options.SingleOrDefault(item => item.InstanceId == rewardInstanceId);
        if (option is null)
            throw new GameRuleException(L10n.Text("error.rewardNotFound"));

        ConfirmRewardPurchase(state, window, player, option);
    }

    private void ConfirmRewardPurchase(GameState state, RewardWindowState window, PlayerState player, RewardOptionState option)
    {
        if (!TrySpendBp(state, player, option.Cost, BpSpendReasonRewardPurchase))
            throw new GameRuleException(L10n.Text("error.notEnoughBp"));

        window.PurchaseCount++;
        if (option.RewardId == RewardCatalog.HeroRoleActionUpgrade.Id && option.Cost == 0)
            player.HasClaimedFreeHeroRoleActionUpgrade = true;
        Log(state, L10n.Text("log.rewardPurchased",
            ("player", L10n.Player(player.Name)),
            ("reward", L10n.Reward(option.RewardId)),
            ("cost", L10n.Raw(option.Cost))), "system");
    }

    private void RefreshRewardOptionsIfOpen(GameState state, PlayerState player)
    {
        if (state.RewardWindow is { } window && window.PlayerId == player.Id)
            RefreshRewardOptions(window, player);
    }

    public void ReturnToRewardWindow(GameState state, Guid playerId)
    {
        EnsurePlaying(state);
        var window = RequireActiveRewardWindow(state);
        if (window.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));

        if (state.PendingRoleActionUpgrade is { PlayerId: var pendingUpgradePlayerId }
            && pendingUpgradePlayerId == playerId)
        {
            state.PendingRoleActionUpgrade = null;
            return;
        }

        if (state.PendingHeroDraft is { PlayerId: var pendingDraftPlayerId } draft
            && pendingDraftPlayerId == playerId
            && draft.Kind is HeroDraftKind.Recruit or HeroDraftKind.SoldierRecruit)
        {
            state.PendingHeroDraft = null;
            return;
        }

        if (state.PendingRelicReward is { PlayerId: var pendingRelicPlayerId }
            && pendingRelicPlayerId == playerId)
        {
            state.PendingRelicReward = null;
            return;
        }

        throw new GameRuleException(L10n.Text("error.noRewardChildMenu"));
    }

    public void CancelSoldierRecruitDraft(GameState state, Guid playerId)
    {
        var draft = state.PendingHeroDraft
            ?? throw new GameRuleException(L10n.Text("error.noHeroDraft"));
        if (draft.PlayerId != playerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));
        if (draft.Kind != HeroDraftKind.SoldierRecruit)
            throw new GameRuleException(L10n.Text("error.heroDraftInvalidChoice"));

        state.PendingHeroDraft = null;
        EndRewardTurnIfResolved(state);
    }

    private static void CloseRewardWindow(GameState state, RewardWindowState window)
    {
        state.ResolvedRewardWindows.Add(RewardWindowKey(window.PlayerId, window.RoundNumber));
        state.RewardWindow = null;
        state.PendingRelicReward = null;
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

    private static void ApplyDummyReward(GameState state, PlayerState player, string rewardId)
    {
        var previousMaxActionPoints = GetMaxActionPoints(player);
        RelicEffects.AddRelic(player, rewardId);
        if (player.Id != state.ActivePlayerId)
            return;

        var gainedMaxActionPoints = Math.Max(0, GetMaxActionPoints(player) - previousMaxActionPoints);
        if (gainedMaxActionPoints > 0)
            state.ActionPoints = Math.Min(GetMaxActionPoints(player), state.ActionPoints + gainedMaxActionPoints);
    }

    public void SelectRoleActionUpgrade(GameState state, Guid characterId, string roleActionId)
    {
        EnsurePlaying(state);
        var pending = state.PendingRoleActionUpgrade
            ?? throw new GameRuleException(L10n.Text("error.noPendingRoleActionUpgrade"));
        if (pending.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.opponentTurn"));

        var character = state.FindCharacter(characterId);
        if (character.PlayerId != state.ActivePlayerId || !character.IsAlive)
            throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));
        if (!CanUpgradeHeroRank(character))
            throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));
        if (character.HeroRank == 0 && !_roleActions.IsUpgradeChoice(character.Definition.Key, roleActionId))
            throw new GameRuleException(L10n.Text("error.roleActionNotUpgradeChoice"));

        var player = state.Players.Single(item => item.Id == state.ActivePlayerId);
        ConfirmPendingRewardPurchase(state, player, pending.RewardInstanceId);
        var unlockedRoleActionId = PromoteHeroRank(character, roleActionId);
        state.PendingRoleActionUpgrade = null;
        Log(state, L10n.Text(character.HeroRank == 1 ? "log.roleActionUnlocked" : "log.heroRankUp",
            ("character", L10n.Character(character.Definition.Key)),
            ("characterId", L10n.Raw(character.Id)),
            ("roleAction", L10n.RoleAction(unlockedRoleActionId)),
            ("rank", L10n.Raw(character.HeroRank))), "buff");
        RefreshRewardOptionsIfOpen(state, player);
        EndRewardTurnIfResolved(state);
    }

    private string PromoteHeroRank(CharacterState character, string selectedRoleActionId)
    {
        if (character.Definition.CardType != CardType.Hero || character.HeroRank >= 3)
            throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));

        if (character.HeroRank == 0)
        {
            character.RoleActionIds.Add(selectedRoleActionId);
            character.HeroPathRoleActionId = selectedRoleActionId;
            character.HeroRank = 1;
            RecoverRankUpHp(character);
            return selectedRoleActionId;
        }

        var path = HeroGrowthCatalog.Find(character)
            ?? throw new GameRuleException(L10n.Text("error.noRoleActionUpgradeTarget"));

        character.HeroRank++;
        if (character.HeroRank == 2)
        {
            RecoverRankUpHp(character);
            return path.BaseRoleActionId;
        }

        if (!character.RoleActionIds.Contains(path.Rank3RoleActionId, StringComparer.OrdinalIgnoreCase))
            character.RoleActionIds.Add(path.Rank3RoleActionId);
        character.CurrentHp = GetMaxHp(character);
        return path.Rank3RoleActionId;
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
            case "mend":
                UseMend(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "aegis-formation":
                UseAegisFormation(state, actor);
                break;
            case "crimson-lunge":
                UseCrimsonLunge(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "astral-focus":
                UseAstralFocus(state, actor, targetCharacterId);
                break;
            case "miracle-standard":
                UseMiracleStandard(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "edict-of-victory":
                UseEdictOfVictory(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "astral-alignment":
                UseAstralAlignment(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "thread-cut":
                UseThreadCut(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "field-rations":
                UseFieldRations(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "militia-call":
                UseMilitiaCall(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "starfall":
                UseStarfall(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "archive-formula":
                UseArchiveFormula(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "grove-sanctuary":
                UseGroveSanctuary(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "call-the-hunt":
                UseCallTheHunt(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "glory-roar":
                UseGloryRoar(state, actor);
                break;
            case "dragon-breaker":
                UseDragonBreaker(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "nightmare-stare":
                UseNightmareStare(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            case "abyssal-bargain":
                UseAbyssalBargain(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "holy-bastion":
                UseHolyBastion(state, actor, RequireAllyTarget(state, actor, targetCharacterId));
                break;
            case "iron-charge":
                UseIronCharge(state, actor, RequireEnemyTarget(state, actor, targetCharacterId));
                break;
            default:
                throw new GameRuleException(L10n.Text("error.roleActionNotFound"));
        }

        TriggerRoleActionRelics(state, actor, action);
        if (!action.Metadata.IsRepeatable)
            actor.RoleActionsUsedThisTurn.Add(roleActionId);
        StartRoleActionCooldown(actor, action);
        state.ActionsTakenThisTurn++;
        TryGrantFirstRoleActionBp(state, state.ActivePlayer);
        ResolveDefeats(state);
        EvaluateGameEnd(state);
    }

    private void TryGrantFirstRoleActionBp(GameState state, PlayerState player)
    {
        if (player.FirstRoleActionBpGrantedThisTurn)
            return;

        player.FirstRoleActionBpGrantedThisTurn = true;
        TryGainBp(state, player, 1, BpReasonFirstRoleAction);
    }

    private static CharacterState RequireAllyTarget(GameState state, CharacterState actor, Guid? targetCharacterId)
    {
        if (targetCharacterId is null)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        var target = state.FindCharacter(targetCharacterId.Value);
        if (target.PlayerId != actor.PlayerId || !target.IsAlive || IsDeploying(target))
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        return target;
    }

    private static CharacterState RequireEnemyTarget(GameState state, CharacterState actor, Guid? targetCharacterId)
    {
        if (targetCharacterId is null)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        var target = state.FindCharacter(targetCharacterId.Value);
        if (target.PlayerId == actor.PlayerId || !target.IsAlive || IsDeploying(target))
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
        var healing = HealFromRoleAction(target, 2);
        var healed = healing.Restored;
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
        if (HeroRankRules.HasRank2Path(actor, "saintly-prayer") && (healed > 0 || debuff is not null))
            AddSpellWard(target, actor.Id);
        TriggerActiveHealingRelics(state, actor, [healing]);
        TriggerFieldMedic(state, actor, target, healed > 0 || debuff is not null);
        TriggerDeputyCleric(state, actor, target, healed > 0 || debuff is not null);
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
        TriggerShieldDrill(state, actor);
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
        TriggerShieldDrill(state, actor);
        TriggerDeputyShieldmaiden(state, actor);
        TriggerMasonToken(state, owner);
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
        ApplyBurning(state, actor, target);
        if (!target.IsAlive)
            return;
        ApplyVoid(state, actor, target);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("burning"))), "magic");
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("void"))), "magic");
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
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
        var healingResults = new List<ActiveHealingResult>();
        var primaryHealing = HealFromRoleAction(target, 1);
        healingResults.Add(primaryHealing);
        var healed = primaryHealing.Restored;
        Log(state, L10n.Text("log.cleansingHerbs",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("status", L10n.Status(debuff.Id)),
            ("amount", L10n.Raw(healed))), "heal");
        if (HeroRankRules.HasRank2Path(actor, "cleansing-herbs"))
        {
            AddSpellWard(target, actor.Id);
            if (target.CurrentHp * 2 < GetMaxHp(target))
                healingResults.Add(HealFromRoleAction(target, 1));
        }
        TriggerActiveHealingRelics(state, actor, healingResults);
        TriggerFieldMedic(state, actor, target, true);
        TriggerDeputyCleric(state, actor, target, true);
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

        ApplyExhaustion(state, actor, target);
        if (!target.IsAlive)
            return;
        ApplyErosion(state, actor, target);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("exhaustion"))), "debuff");
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("erosion"))), "debuff");
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseChallenge(GameState state, CharacterState actor, CharacterState target)
    {
        ApplyTrembling(state, actor, target);
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
        var wasNew = target.Statuses.All(status => status.Id != "marked" || status.Expired);
        target.Statuses.RemoveAll(status => status.Id == "marked");
        target.Statuses.Add(new FateMarkedStatus(actor.Id));
        NotifyDebuffApplied(state, actor, target, "marked", wasNew);
        Log(state, L10n.Text("log.fateMark",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id))), "debuff");
    }

    private void UsePredatoryGaze(GameState state, CharacterState actor, CharacterState target)
    {
        var wasNew = target.Statuses.All(status => status.Id != "prey" || status.Expired);
        target.Statuses.RemoveAll(status => status.Id == "prey");
        target.Statuses.Add(new PreyStatus(actor.Id, actor.PlayerId));
        NotifyDebuffApplied(state, actor, target, "prey", wasNew);
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
        TriggerHpPaymentRelic(state, actor, lifeCost);
        target.Statuses.RemoveAll(status => status.Id == "pact");
        target.Statuses.Add(new PactStatus(actor.Id));
        Log(state, L10n.Text("log.darkPact",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(lifeCost))), "debuff");
        if (target.CurrentHp * 2 < GetMaxHp(target))
            TryGainBp(state, state.ActivePlayer, 1, BpReasonRoleActionDarkPact);
    }

    private void UseSupplyBasket(GameState state, CharacterState actor, CharacterState target)
    {
        var healing = HealFromRoleAction(target, 1);
        var healed = healing.Restored;
        AddFortify(target, actor.Id, turns: 2);
        Log(state, L10n.Text("log.supplyBasket",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(healed))), "heal");
        TriggerShieldDrill(state, actor);
        TriggerDeputyShieldmaiden(state, actor);
        TriggerActiveHealingRelics(state, actor, [healing]);
        TriggerDeputyCleric(state, actor, target, healed > 0);
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
            var healing = HealFromRoleAction(actor, 2);
            var healed = healing.Restored;
            Log(state, L10n.Text("log.fieldWorkRest",
                ("character", L10n.Character(actor.Definition.Key)),
                ("characterId", L10n.Raw(actor.Id)),
                ("amount", L10n.Raw(healed))), "heal");
            TriggerActiveHealingRelics(state, actor, [healing]);
            TriggerDeputyCleric(state, actor, actor, healed > 0);
            return;
        }

        actor.Statuses.RemoveAll(status => status.Id == "harvest-pending");
        actor.Statuses.Add(new PendingHarvestStatus(actor.Id, actor.PlayerId));
        Log(state, L10n.Text("log.fieldWorkSowing",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id))), "buff");
    }

    private void UseMend(GameState state, CharacterState actor, CharacterState target)
    {
        var debuff = target.Statuses.FirstOrDefault(status => !status.IsBuff && status.IsDispellable && !status.Expired);
        if (debuff is not null)
        {
            target.Statuses.Remove(debuff);
            Log(state, L10n.Text("log.roleActionCleanse",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status(debuff.Id))), "buff");
        }

        var healing = HealFromRoleAction(target, 3);
        var healed = healing.Restored;
        Log(state, L10n.Text("log.roleActionHeal",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(healed))), "heal");
        if (debuff is not null)
        {
            AddSpellWard(target, actor.Id, 2);
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status("spell-ward"))), "buff");
        }
        TriggerActiveHealingRelics(state, actor, [healing]);
        TriggerFieldMedic(state, actor, target, true, grantWard: debuff is null);
        TriggerDeputyCleric(state, actor, target, healed > 0 || debuff is not null);
    }

    private void UseAegisFormation(GameState state, CharacterState actor)
    {
        var owner = state.FindOwner(actor);
        owner.SharedShield = owner.SharedShield <= 0 ? 1 : owner.SharedShield + 2;
        Log(state, L10n.Text("log.aegisFormation",
            ("character", L10n.Character(actor.Definition.Key)),
            ("characterId", L10n.Raw(actor.Id)),
            ("shield", L10n.Raw(owner.SharedShield))), "shield");
        TriggerShieldDrill(state, actor);
        TriggerDeputyShieldmaiden(state, actor);
        TriggerMasonToken(state, owner);
    }

    private void UseCrimsonLunge(GameState state, CharacterState actor, CharacterState target)
    {
        if (!CanReceiveMightyStrike(actor, target))
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));

        AddMightyStrike(target, actor.Id);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("mighty-strike"))), "buff");
    }

    private void UseAstralFocus(GameState state, CharacterState actor, Guid? targetCharacterId)
    {
        if (targetCharacterId is null)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
        var target = state.FindCharacter(targetCharacterId.Value);
        if (!CanReceiveChant(actor, target))
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));

        AddChant(target, actor.Id);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("chant"))), "buff");
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseMiracleStandard(GameState state, CharacterState actor, CharacterState target)
    {
        var owner = state.FindOwner(actor);
        var affected = GetAlliesInArea(owner, target).ToArray();
        var healingResults = new List<ActiveHealingResult>();
        var totalCleansed = 0;
        var mainCleanse = CleanseDebuffs(target, all: true);
        totalCleansed += mainCleanse;
        var mainHeal = (int)Math.Ceiling(GetMaxHp(actor) / 4.0);
        var mainHealing = HealFromRoleAction(target, mainHeal);
        healingResults.Add(mainHealing);
        var healed = mainHealing.Restored;
        TriggerFieldMedic(state, actor, target, healed > 0 || mainCleanse > 0);
        TriggerDeputyCleric(state, actor, target, healed > 0 || mainCleanse > 0);

        foreach (var ally in affected.Where(ally => ally.Id != target.Id))
        {
            var cleansed = CleanseDebuffs(ally, all: false);
            totalCleansed += cleansed;
            var healing = HealFromRoleAction(ally, Math.Max(1, (int)Math.Ceiling(mainHeal / 2.0)));
            healingResults.Add(healing);
            var amount = healing.Restored;
            TriggerFieldMedic(state, actor, ally, amount > 0 || cleansed > 0);
            TriggerDeputyCleric(state, actor, ally, amount > 0 || cleansed > 0);
        }

        if (totalCleansed > 0)
            IncreaseSharedShield(state, owner,
                Math.Max(0, GetMagicalDefense(state, actor)) + affected.Length, actor);

        TriggerActiveHealingRelics(state, actor, healingResults);

        Log(state, L10n.Text("log.rank3RoleAction",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("roleAction", L10n.RoleAction("miracle-standard"))), "heal");
    }

    private void UseEdictOfVictory(GameState state, CharacterState actor, CharacterState target)
    {
        target.BonusAttackUsesThisTurn++;
        target.Statuses.RemoveAll(status => status.Id == "edict-of-victory");
        target.Statuses.Add(new VictoryEdictStatus(actor.Id, GetActiveAttack(state, actor)));
        state.ActivePlayer.PendingActionPointDebt += 1;
        Log(state, L10n.Text("log.rank3RoleAction",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("roleAction", L10n.RoleAction("edict-of-victory"))), "buff");
    }

    private void UseAstralAlignment(GameState state, CharacterState actor, CharacterState target)
    {
        if (GetAttackType(target) != DamageType.Magical)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));

        AddChant(target, actor.Id);
        target.BonusAttackUsesThisTurn++;
        target.Statuses.RemoveAll(status => status.Id == "astral-alignment");
        target.Statuses.Add(new AstralAlignmentStatus(actor.Id, actor.PlayerId, GetActiveAttack(state, actor)));
        Log(state, L10n.Text("log.rank3RoleAction",
            ("actor", L10n.Character(actor.Definition.Key)),
            ("actorId", L10n.Raw(actor.Id)),
            ("roleAction", L10n.RoleAction("astral-alignment"))), "magic");
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseThreadCut(GameState state, CharacterState actor, CharacterState target)
    {
        var count = CountThreadCutMarks(target);
        if (count == 0)
        {
            var wasNew = target.Statuses.All(status => status.Id != "marked" || status.Expired);
            target.Statuses.RemoveAll(status => status.Id == "marked");
            target.Statuses.Add(new FateMarkedStatus(actor.Id));
            NotifyDebuffApplied(state, actor, target, "marked", wasNew);
            Log(state, L10n.Text("log.fateMark",
                ("actor", L10n.Character(actor.Definition.Key)),
                ("actorId", L10n.Raw(actor.Id)),
                ("target", L10n.Character(target.Definition.Key)),
                ("targetId", L10n.Raw(target.Id))), "debuff");
            return;
        }

        DealMoraleDamage(state, target, ThreadCutMoraleDamagePerMark * count, actor.Id, "thread-cut");
        if (target.Morale <= 0 && target.IsAlive)
        {
            DealRoleActionDamage(state, target, GetActiveAttack(state, actor), DamageType.Magical, actor.Id, "thread-cut");
            ApplyTrembling(state, actor, target);
        }
    }

    private void UseFieldRations(GameState state, CharacterState actor, CharacterState target)
    {
        var owner = state.FindOwner(actor);
        var baseHeal = Math.Max(1, (int)Math.Ceiling(GetActiveAttack(state, actor) / 2.0));
        var healingResults = new List<ActiveHealingResult>();
        foreach (var ally in owner.Characters.Where(character => character.IsAlive && !IsDeploying(character)))
        {
            var healing = HealFromRoleAction(ally, baseHeal);
            healingResults.Add(healing);
            var amount = healing.Restored;
            TriggerDeputyCleric(state, actor, ally, amount > 0);
        }

        var wasLow = target.CurrentHp * 2 < GetMaxHp(target);
        var extraHealing = HealFromRoleAction(target, GetActiveAttack(state, actor));
        healingResults.Add(extraHealing);
        var extra = extraHealing.Restored;
        var cleansed = wasLow ? CleanseDebuffs(target, all: false) : 0;
        AddFortify(target, actor.Id);
        AddSpellWard(target, actor.Id);
        TriggerShieldDrill(state, actor);
        TriggerDeputyShieldmaiden(state, actor);
        TriggerDeputyCleric(state, actor, target, extra > 0 || cleansed > 0);
        TriggerActiveHealingRelics(state, actor, healingResults);
    }

    private void UseMilitiaCall(GameState state, CharacterState actor, CharacterState target)
    {
        if (target.Definition.Key != "peasant" && target.Definition.CardType != CardType.Soldier)
            throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));

        if (GetAttackType(target) == DamageType.Physical)
            AddStrongAttack(target, actor.Id);
        else
            AddMagicSurge(target, actor.Id);
        target.BonusAttackUsesThisTurn++;
        var bonus = GetActiveAttack(state, actor) + (target.Definition.CardType == CardType.Soldier ? target.SoldierRank : 0);
        target.Statuses.RemoveAll(status => status.Id == "militia-call");
        target.Statuses.Add(new MilitiaCallStatus(actor.Id, actor.PlayerId, bonus));
    }

    private void UseStarfall(GameState state, CharacterState actor, CharacterState target)
    {
        var damage = Math.Max(1, GetActiveAttack(state, actor));
        DealRoleActionDamage(state, target, damage, DamageType.Magical, actor.Id, "starfall");
        if (!target.IsAlive)
            return;
        ApplyBurning(state, actor, target);
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseArchiveFormula(GameState state, CharacterState actor, CharacterState target)
    {
        var archivedWasNew = target.Statuses.All(status => status.Id != "archived" || status.Expired);
        target.Statuses.RemoveAll(status => status.Id == "archived");
        target.Statuses.Add(new ArchivedStatus(actor.Id, actor.PlayerId));
        NotifyDebuffApplied(state, actor, target, "archived", archivedWasNew);
        if (!target.IsAlive)
            return;
        var hadMagicDebuff = target.Statuses.Any(status => !status.Expired && status.Id is "burning" or "void");
        var damage = GetActiveAttack(state, actor) + (hadMagicDebuff ? target.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired)?.Stacks ?? 0 : 0);
        DealRoleActionDamage(state, target, Math.Max(1, damage), DamageType.Magical, actor.Id, "archive-formula");
        ApplyBurning(state, actor, target, Math.Max(1, CountCommonDebuffs(target)));
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseGroveSanctuary(GameState state, CharacterState actor, CharacterState target)
    {
        var owner = state.FindOwner(actor);
        var affected = GetAlliesInArea(owner, target).ToArray();
        var totalCleansed = 0;
        totalCleansed += CleanseDebuffs(target, all: true);
        foreach (var ally in affected.Where(ally => ally.Id != target.Id))
            totalCleansed += CleanseDebuffs(ally, all: false);

        if (totalCleansed > 0)
        {
            var healingResults = new List<ActiveHealingResult>();
            foreach (var ally in affected)
            {
                var healing = HealFromRoleAction(ally, GetActiveAttack(state, actor) * totalCleansed);
                healingResults.Add(healing);
                var healed = healing.Restored;
                TriggerFieldMedic(state, actor, ally, healed > 0);
                TriggerDeputyCleric(state, actor, ally, healed > 0);
            }
            TriggerActiveHealingRelics(state, actor, healingResults);
        }
        else
        {
            var layers = Math.Max(1, GetMagicalDefense(state, actor));
            foreach (var ally in affected)
                AddSpellWard(ally, actor.Id, layers);
        }
    }

    private void UseCallTheHunt(GameState state, CharacterState actor, CharacterState target)
    {
        foreach (var enemy in GetEnemiesInArea(state.FindOwner(target), target))
        {
            var wasNew = enemy.Statuses.All(status => status.Id != "hunted" || status.Expired);
            enemy.Statuses.RemoveAll(status => status.Id == "hunted");
            enemy.Statuses.Add(new HuntedStatus(actor.Id, actor.PlayerId, GetActiveAttack(state, actor)));
            NotifyDebuffApplied(state, actor, enemy, "hunted", wasNew);
        }
    }

    private void UseGloryRoar(GameState state, CharacterState actor)
    {
        AddStrongAttack(actor, actor.Id);
        actor.BonusAttackUsesThisTurn += Math.Max(1, (int)Math.Ceiling(GetActiveAttack(state, actor) / 3.0));
        actor.Statuses.RemoveAll(status => status.Id == "glory-roar");
        actor.Statuses.Add(new GloryRoarStatus(actor.Id, actor.PlayerId));
    }

    private void UseDragonBreaker(GameState state, CharacterState actor, CharacterState target)
    {
        var targetOwner = state.FindOwner(target);
        if (targetOwner.SharedShield > 0)
        {
            var before = targetOwner.SharedShield;
            targetOwner.SharedShield = Math.Max(0, targetOwner.SharedShield - GetActiveAttack(state, actor));
            if (before > 0 && targetOwner.SharedShield == 0)
            {
                ClearShieldLayerIfBroken(targetOwner);
                TryAwardEnemyShieldBreakBp(
                    state,
                    actor,
                    state.FindOwner(actor),
                    targetOwner,
                    before,
                    before);
                foreach (var enemy in GetEnemiesInArea(targetOwner, target))
                    ApplyTremblingAndVulnerable(state, enemy, actor);
            }
            return;
        }

        DealRoleActionDamage(state, target, GetActiveAttack(state, actor), DamageType.Physical, actor.Id, "dragon-breaker");
        ApplyTremblingAndVulnerable(state, target, actor);
    }

    private void UseNightmareStare(GameState state, CharacterState actor, CharacterState target)
    {
        foreach (var enemy in GetEnemiesInArea(state.FindOwner(target), target))
        {
            var wasNew = enemy.Statuses.All(status => status.Id != "nightmare-prey" || status.Expired);
            enemy.Statuses.RemoveAll(status => status.Id == "nightmare-prey");
            enemy.Statuses.Add(new NightmarePreyStatus(actor.Id, actor.PlayerId, GetActiveAttack(state, actor)));
            NotifyDebuffApplied(state, actor, enemy, "nightmare-prey", wasNew);
        }
        ApplyErosion(state, actor, target);
        TriggerArcaneResonance(state, actor, fromRoleAction: true);
        TriggerDeputyArcanist(state, actor, fromRoleAction: true);
    }

    private void UseAbyssalBargain(GameState state, CharacterState actor, CharacterState target)
    {
        var lifeCost = Math.Min(GetActiveAttack(state, actor), Math.Max(0, target.CurrentHp - 1));
        target.CurrentHp -= lifeCost;
        TriggerHpPaymentRelic(state, actor, lifeCost);
        target.BonusAttackUsesThisTurn++;
        target.Statuses.RemoveAll(status => status.Id == "abyssal-bargain");
        target.Statuses.Add(new AbyssalBargainStatus(actor.Id, GetActiveAttack(state, actor)) { LifeCost = lifeCost });
    }

    private void UseHolyBastion(GameState state, CharacterState actor, CharacterState target)
    {
        var stacks = Math.Max(2, GetPhysicalDefense(state, actor));
        var oath = target.Statuses.OfType<GuardOathStatus>().FirstOrDefault(status => !status.Expired);
        if (oath is not null)
            oath.AddStack(stacks);
        else
            target.Statuses.Add(new GuardOathStatus(actor.Id, stacks));
        AddSpellWard(target, actor.Id);
        AddFortify(actor, actor.Id);
        var owner = state.FindOwner(actor);
        IncreaseSharedShield(state, owner,
            Math.Max(0, GetPhysicalDefense(state, actor)) + Math.Max(0, GetMagicalDefense(state, actor)), actor);
    }

    private void UseIronCharge(GameState state, CharacterState actor, CharacterState target)
    {
        var owner = state.FindOwner(actor);
        var consumed = owner.SharedShield;
        if (consumed <= 0)
            throw new GameRuleException(L10n.Text("error.roleActionRequiresShield"));
        owner.SharedShield = 0;
        ClearShieldLayerIfBroken(owner);
        var hpDamage = DealRoleActionDamage(state, target, consumed + GetActiveAttack(state, actor), DamageType.Physical, actor.Id, "iron-charge");
        ApplyTrembling(state, actor, target);
        if (hpDamage > 0)
            IncreaseSharedShield(state, owner, hpDamage, actor);
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

    private static bool HasCommonDebuff(CharacterState target) =>
        CountCommonDebuffs(target) > 0;

    private static int CountCommonDebuffs(CharacterState target) =>
        target.Statuses.Count(status => !status.Expired && !status.IsBuff
            && status.Id is "burning" or "void" or "exhaustion" or "erosion" or "trembling" or "vulnerable");

    private void TriggerFieldMedic(GameState state, CharacterState source, CharacterState target, bool didHealOrCleanse, bool grantWard = true)
    {
        if (!didHealOrCleanse || source.PlayerId != target.PlayerId || IsDeploying(target))
            return;

        var cleric = state.FindOwner(source).Characters.FirstOrDefault(character =>
            character.IsAlive && !IsDeploying(character) && character.Definition.TraitId == "field-medic");
        if (cleric is null)
            return;

        if (grantWard)
        {
            AddSpellWard(target, cleric.Id);
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status("spell-ward"))), "buff");
        }
        if (cleric.SoldierRank >= 1 && target.CurrentHp * 2 < GetMaxHp(target))
        {
            var healed = Heal(target, 3);
            Log(state, L10n.Text("log.roleActionHeal",
                ("actor", L10n.Character(cleric.Definition.Key)),
                ("actorId", L10n.Raw(cleric.Id)),
                ("target", L10n.Character(target.Definition.Key)),
                ("targetId", L10n.Raw(target.Id)),
                ("amount", L10n.Raw(healed))), "heal");
        }
    }

    internal static void AddSpellWard(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var ward = target.Statuses.OfType<SpellWardStatus>().FirstOrDefault(status => !status.Expired);
        if (ward is not null)
            ward.AddTurns(turns);
        else
            target.Statuses.Add(new SpellWardStatus(sourceCharacterId, turns));
    }

    internal static void AddFortify(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var fortify = target.Statuses.OfType<FortifyStatus>().FirstOrDefault(status => !status.Expired);
        if (fortify is not null)
            fortify.AddTurns(turns);
        else
            target.Statuses.Add(new FortifyStatus(sourceCharacterId, turns));
    }

    internal static bool AddVoid(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var status = target.Statuses.OfType<VoidStatus>().FirstOrDefault(status => !status.Expired);
        if (status is not null)
        {
            status.AddTurns(turns);
            return false;
        }
        target.Statuses.Add(new VoidStatus(sourceCharacterId, turns));
        return true;
    }

    internal static bool AddVulnerable(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var status = target.Statuses.OfType<VulnerableStatus>().FirstOrDefault(status => !status.Expired);
        if (status is not null)
        {
            status.AddTurns(turns);
            return false;
        }
        target.Statuses.Add(new VulnerableStatus(sourceCharacterId, turns));
        return true;
    }

    internal static bool AddExhaustion(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var status = target.Statuses.OfType<ExhaustionStatus>().FirstOrDefault(status => !status.Expired);
        if (status is not null)
        {
            status.AddTurns(turns);
            return false;
        }
        target.Statuses.Add(new ExhaustionStatus(sourceCharacterId, turns));
        return true;
    }

    internal static bool AddErosion(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var status = target.Statuses.OfType<ErosionStatus>().FirstOrDefault(status => !status.Expired);
        if (status is not null)
        {
            status.AddTurns(turns);
            return false;
        }
        target.Statuses.Add(new ErosionStatus(sourceCharacterId, turns));
        return true;
    }

    internal static bool AddTrembling(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var status = target.Statuses.OfType<TremblingStatus>().FirstOrDefault(status => !status.Expired);
        if (status is not null)
        {
            status.RefreshTurns(turns);
            return false;
        }
        target.Statuses.Add(new TremblingStatus(sourceCharacterId, turns));
        return true;
    }

    private void ApplyBurning(GameState state, CharacterState source, CharacterState target, int stacks = 1)
    {
        var wasNew = AddBurning(target, source.Id, stacks, state);
        NotifyDebuffApplied(state, source, target, "burning", wasNew);
    }

    private void ApplyVoid(GameState state, CharacterState source, CharacterState target, int turns = TurnDurationStatus.DefaultTurns)
    {
        var wasNew = AddVoid(target, source.Id, turns);
        NotifyDebuffApplied(state, source, target, "void", wasNew);
    }

    private void ApplyVulnerable(GameState state, CharacterState source, CharacterState target, int turns = TurnDurationStatus.DefaultTurns)
    {
        var wasNew = AddVulnerable(target, source.Id, turns);
        NotifyDebuffApplied(state, source, target, "vulnerable", wasNew);
    }

    private void ApplyExhaustion(GameState state, CharacterState source, CharacterState target, int turns = TurnDurationStatus.DefaultTurns)
    {
        var wasNew = AddExhaustion(target, source.Id, turns);
        NotifyDebuffApplied(state, source, target, "exhaustion", wasNew);
    }

    private void ApplyErosion(GameState state, CharacterState source, CharacterState target, int turns = TurnDurationStatus.DefaultTurns)
    {
        var wasNew = AddErosion(target, source.Id, turns);
        NotifyDebuffApplied(state, source, target, "erosion", wasNew);
    }

    private void ApplyTrembling(GameState state, CharacterState source, CharacterState target, int turns = TurnDurationStatus.DefaultTurns)
    {
        var wasNew = AddTrembling(target, source.Id, turns);
        NotifyDebuffApplied(state, source, target, "trembling", wasNew);
    }

    internal void NotifyDebuffApplied(
        GameState state,
        CharacterState source,
        CharacterState target,
        string statusId,
        bool wasNewDistinct,
        bool allowWitchBell = true)
    {
        var sourceOwner = state.FindOwner(source);
        if (!target.IsAlive || sourceOwner.Id == target.PlayerId)
            return;

        if (wasNewDistinct)
        {
            var distinctDebuffs = target.Statuses
                .Where(status => !status.Expired && !status.IsBuff)
                .Select(status => status.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            if (distinctDebuffs >= 3
                && RelicEffects.TryUseTurnRelic(sourceOwner, "relic-plague-codex"))
            {
                LogRelicTriggered(state, sourceOwner, "relic-plague-codex", target);
                DealAbsoluteDamage(state, target, distinctDebuffs, source.Id,
                    L10n.Reward("relic-plague-codex"));
            }
        }

        if (!allowWitchBell
            || !target.IsAlive
            || !RelicEffects.TryUseTurnRelic(sourceOwner, "relic-witch-bell"))
            return;

        var existingTrembling = target.Statuses
            .OfType<TremblingStatus>()
            .FirstOrDefault(status => !status.Expired);
        var tremblingWasNew = existingTrembling is null;
        if (existingTrembling is not null)
            existingTrembling.AddTurns(TurnDurationStatus.DefaultTurns);
        else
            target.Statuses.Add(new TremblingStatus(source.Id, TurnDurationStatus.DefaultTurns));

        LogRelicTriggered(state, sourceOwner, "relic-witch-bell", target);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("trembling"))), "debuff");
        NotifyDebuffApplied(state, source, target, "trembling", tremblingWasNew, allowWitchBell: false);
    }

    private void TriggerShieldDrill(GameState state, CharacterState? source)
    {
        if (source is null)
            return;

        var owner = state.FindOwner(source);
        var shieldmaidens = owner.Characters
            .Where(character => character.IsAlive
                && !IsDeploying(character)
                && character.Definition.TraitId == "shield-drill"
                && !character.TraitsUsedThisTurn.Contains("shield-drill"))
            .ToArray();
        if (shieldmaidens.Length == 0)
            return;

        var target = owner.Characters
            .Where(character => character.IsAlive && !IsDeploying(character))
            .OrderBy(character => GetMaxHp(character) == 0 ? 1 : (double)character.CurrentHp / GetMaxHp(character))
            .ThenBy(character => character.Definition.CardType == CardType.Hero ? 0 : 1)
            .ThenBy(character => character.CurrentHp)
            .FirstOrDefault();
        if (target is null)
            return;

        foreach (var shieldmaiden in shieldmaidens)
        {
            AddFortify(target, shieldmaiden.Id);
            shieldmaiden.TraitsUsedThisTurn.Add("shield-drill");
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status("fortify"))), "buff");
        }
    }

    private void TriggerArcaneResonance(GameState state, CharacterState source, bool fromRoleAction)
    {
        var owner = state.FindOwner(source);
        var arcanist = owner.Characters.FirstOrDefault(character =>
            character.IsAlive
            && !IsDeploying(character)
            && character.Definition.TraitId == "arcane-resonance"
            && !character.TraitsUsedThisTurn.Contains("arcane-resonance"));
        if (arcanist is null)
            return;

        var turns = fromRoleAction && arcanist.SoldierRank >= 1
            ? 3
            : TurnDurationStatus.DefaultTurns;
        var targets = SelectArcaneResonanceTargets(state, owner, arcanist).ToArray();
        if (targets.Length == 0)
            return;

        arcanist.TraitsUsedThisTurn.Add("arcane-resonance");
        foreach (var target in targets)
        {
            AddMagicSurge(target, arcanist.Id, turns);
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(target.Definition.Key)),
                ("characterId", L10n.Raw(target.Id)),
                ("status", L10n.Status("magic-surge"))), "buff");
        }
    }

    private IEnumerable<CharacterState> SelectArcaneResonanceTargets(
        GameState state,
        PlayerState owner,
        CharacterState arcanist)
    {
        var magicalUnits = owner.Characters
            .Where(character => character.IsInBattle
                && character.IsAlive
                && !IsDeploying(character)
                && GetAttackType(character) == DamageType.Magical);

        if (arcanist.SoldierRank >= 1)
            return magicalUnits.OrderBy(character => character.Slot);

        return magicalUnits
            .Where(character => character.Definition.CardType == CardType.Hero)
            .OrderByDescending(character => GetActiveAttack(state, character))
            .ThenBy(character => character.Slot)
            .Take(1);
    }

    private bool TryUseDeputyPassive(GameState state, CharacterState host, string deputyEffectId)
    {
        if (!host.IsAlive || IsDeploying(host) || host.DeputyEffectId != deputyEffectId)
            return false;
        var owner = state.FindOwner(host);
        if (host.DeputySoldierId is not { } soldierId
            || owner.Characters.All(character =>
                character.Id != soldierId
                || character.Zone != CharacterZone.Deputy
                || character.DeputyHostHeroId != host.Id
                || character.CurrentHp <= 0
                || character.DeputyEffectId != deputyEffectId))
            return false;

        return owner.DeputyPassivesUsedThisTurn.Add($"{host.Id:N}:{deputyEffectId}");
    }

    private void LogDeputyTriggered(GameState state, CharacterState host, string deputyEffectId, string statusId, CharacterState target)
    {
        Log(state, L10n.Text("log.deputyTriggered",
            ("host", L10n.Character(host.Definition.Key)),
            ("hostId", L10n.Raw(host.Id)),
            ("deputy", L10n.Deputy(deputyEffectId)),
            ("target", L10n.Character(target.Definition.Key)),
            ("targetId", L10n.Raw(target.Id)),
            ("status", L10n.Status(statusId))), "buff");
    }

    private void TriggerDeputyCleric(GameState state, CharacterState host, CharacterState target, bool didHealOrCleanse)
    {
        if (!didHealOrCleanse || host.PlayerId != target.PlayerId || IsDeploying(target) || !TryUseDeputyPassive(state, host, "deputy-cleric"))
            return;

        var turns = target.CurrentHp * 2 < GetMaxHp(target) ? 2 : 1;
        AddSpellWard(target, host.Id, turns);
        LogDeputyTriggered(state, host, "deputy-cleric", "spell-ward", target);
    }

    private void TriggerDeputyShieldmaiden(GameState state, CharacterState host)
    {
        if (!TryUseDeputyPassive(state, host, "deputy-shieldmaiden"))
            return;

        var owner = state.FindOwner(host);
        var target = owner.Characters
            .Where(character => character.IsAlive && !IsDeploying(character))
            .OrderBy(character => GetMaxHp(character) == 0 ? 1 : (double)character.CurrentHp / GetMaxHp(character))
            .ThenBy(character => character.Definition.CardType == CardType.Hero ? 0 : 1)
            .ThenBy(character => character.CurrentHp)
            .FirstOrDefault();
        if (target is null)
            return;

        AddFortify(target, host.Id);
        LogDeputyTriggered(state, host, "deputy-shieldmaiden", "fortify", target);
    }

    private void TriggerDeputyDuelistAfterActiveAttack(GameState state, CharacterState host, CharacterState target, int hpDamage)
    {
        if (host.PlayerId == target.PlayerId || !target.IsAlive || !TryUseDeputyPassive(state, host, "deputy-duelist"))
            return;

        if (hpDamage > 0)
        {
            AddStrongAttack(host, host.Id);
            LogDeputyTriggered(state, host, "deputy-duelist", "strong-attack", host);
        }
        DealAbsoluteDamage(state, target, DuelSenseTrait.AbsoluteDamage, host.Id, L10n.Trait("duel-sense"));
    }

    private void TriggerDeputyArcanist(GameState state, CharacterState host, bool fromRoleAction)
    {
        if (!TryUseDeputyPassive(state, host, "deputy-arcanist"))
            return;

        AddMagicSurge(host, host.Id, fromRoleAction ? 3 : TurnDurationStatus.DefaultTurns);
        host.BonusAttackUsesThisTurn++;
        LogDeputyTriggered(state, host, "deputy-arcanist", "magic-surge", host);
    }

    private void TriggerDuelistTicket(GameState state, CharacterState attacker, PlayerState targetOwner)
    {
        var owner = state.FindOwner(attacker);
        if (owner.Id != state.ActivePlayerId
            || targetOwner.Id == owner.Id
            || targetOwner.SharedShield > 0
            || GetAttackType(attacker) != DamageType.Physical
            || !RelicEffects.TryUseTurnRelic(owner, "relic-duelist-ticket"))
            return;

        AddStrongAttack(attacker, attacker.Id);
        LogRelicTriggered(state, owner, "relic-duelist-ticket", attacker);
    }

    private void TriggerRedHourglass(GameState state, PlayerState owner, DamagePacket packet)
    {
        if (packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Physical
            || packet.SourceCharacter.PlayerId != state.ActivePlayerId
            || owner.Id != state.ActivePlayerId
            || state.PhysicalActiveAttacksTakenThisTurn != 2
            || packet.Amount <= 0
            || !RelicEffects.TryUseTurnRelic(owner, "relic-red-hourglass"))
            return;

        packet.Amount += GetActiveAttack(state, packet.SourceCharacter);
        LogRelicTriggered(state, owner, "relic-red-hourglass", packet.SourceCharacter);
    }

    private void ApplyCompanyStandard(GameState state, PlayerState owner, DamagePacket packet)
    {
        if (packet.Source != DamageSource.ActiveAttack
            || packet.SourceCharacter.Definition.CardType != CardType.Soldier
            || packet.Amount <= 0
            || !RelicEffects.HasRelic(owner, "relic-company-standard"))
            return;

        var soldiersInBattle = owner.Characters.Count(character =>
            character.Definition.CardType == CardType.Soldier
            && character.IsAlive
            && character.IsInBattle
            && !IsDeploying(character));
        if (soldiersInBattle <= 0)
            return;

        packet.Amount += soldiersInBattle;
        LogRelicTriggered(state, owner, "relic-company-standard", packet.SourceCharacter);
    }

    private void ApplyPredatorCrown(GameState state, PlayerState owner, DamagePacket packet)
    {
        if (packet.PredatorCrownApplied
            || packet.DamageType != DamageType.Absolute
            || packet.Amount <= 0
            || !RelicEffects.HasRelic(owner, "relic-predator-crown")
            || packet.TargetCharacter.Statuses.All(status => status.Id != "prey" || status.Expired))
            return;

        packet.Amount = (int)Math.Ceiling(packet.Amount * 1.5);
        packet.PredatorCrownApplied = true;
        LogRelicTriggered(state, owner, "relic-predator-crown", packet.TargetCharacter);
    }

    private void TriggerRelicsAfterDamageResolved(GameState state, DamagePacket packet)
    {
        var sourceOwner = state.FindOwner(packet.SourceCharacter);
        TriggerNightBaitAfterDamage(state, sourceOwner, packet);
        if (packet.FinalCharacterDamage <= 0 || !packet.TargetCharacter.IsAlive)
            return;

        if (packet.DamageType == DamageType.Magical
            && packet.MoraleDamage > 0
            && packet.HpDamage == 0
            && RelicEffects.TryUseTurnRelic(sourceOwner, "relic-hollow-comet-lens"))
        {
            ApplyVoid(state, packet.SourceCharacter, packet.TargetCharacter);
            LogRelicTriggered(state, sourceOwner, "relic-hollow-comet-lens", packet.TargetCharacter);
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
                ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
                ("status", L10n.Status("void"))), "magic");
        }

        if (packet.ConsumedChant
            && packet.DamageType == DamageType.Magical
            && packet.TargetCharacter.IsAlive
            && RelicEffects.TryUseTurnRelic(sourceOwner, "relic-astral-prism"))
        {
            LogRelicTriggered(state, sourceOwner, "relic-astral-prism", packet.TargetCharacter);
            DealRelicDamage(state, packet.TargetCharacter, GetActiveAttack(state, packet.SourceCharacter),
                DamageType.Magical, packet.SourceCharacter.Id, "relic-astral-prism");
        }

        var burning = packet.TargetCharacter.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired);
        if (packet.TargetCharacter.IsAlive
            && burning is not null
            && burning.Stacks >= 3
            && RelicEffects.TryUseTurnRelic(sourceOwner, "relic-ashen-detonator"))
        {
            var stacks = burning.Stacks;
            packet.TargetCharacter.Statuses.Remove(burning);
            LogRelicTriggered(state, sourceOwner, "relic-ashen-detonator", packet.TargetCharacter);
            DealRelicDamage(state, packet.TargetCharacter, stacks * 2, DamageType.Magical, packet.SourceCharacter.Id,
                "relic-ashen-detonator", ignoreDefense: true);
        }
    }

    private void TriggerNightBaitAfterDamage(GameState state, PlayerState sourceOwner, DamagePacket packet)
    {
        var resolvedDamageEvent = packet.FinalCharacterDamage > 0
            || packet.ShieldAbsorbed > 0
            || packet.DefenseReduced != 0
            || packet.ShieldDefenseReduced != 0;
        if (!resolvedDamageEvent
            || packet.HpDamage != 0
            || !packet.TargetCharacter.IsAlive
            || sourceOwner.Id == packet.TargetCharacter.PlayerId
            || !RelicEffects.TryUseTurnRelic(sourceOwner, "relic-night-bait"))
            return;

        var target = packet.TargetCharacter;
        var wasNew = target.Statuses.All(status => status.Id != "prey" || status.Expired);
        target.Statuses.RemoveAll(status => status.Id == "prey");
        target.Statuses.Add(new PreyStatus(packet.SourceCharacter.Id, packet.SourceCharacter.PlayerId));
        LogRelicTriggered(state, sourceOwner, "relic-night-bait", target);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("prey"))), "debuff");
        NotifyDebuffApplied(state, packet.SourceCharacter, target, "prey", wasNew);
    }

    private void ResolveEchoCrystalAfterDamageSequence(GameState state, DamagePacket packet)
    {
        if (!packet.ConsumedChant || !packet.SourceCharacter.IsAlive)
            return;

        var owner = state.FindOwner(packet.SourceCharacter);
        if (!RelicEffects.TryUseTurnRelic(owner, "relic-echo-crystal"))
            return;

        AddChant(packet.SourceCharacter, packet.SourceCharacter.Id);
        LogRelicTriggered(state, owner, "relic-echo-crystal", packet.SourceCharacter);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(packet.SourceCharacter.Definition.Key)),
            ("characterId", L10n.Raw(packet.SourceCharacter.Id)),
            ("status", L10n.Status("chant"))), "magic");
    }

    private void TriggerKingwallStandard(GameState state, PlayerState player)
    {
        if (player.SharedShield > 0 || !RelicEffects.HasRelic(player, "relic-kingwall-standard"))
            return;

        var characters = player.Characters
            .Where(character => character.IsAlive && character.IsInBattle && !IsDeploying(character))
            .ToArray();
        if (characters.Length == 0)
            return;

        var highestPhysicalDefense = characters.Max(character => GetPhysicalDefense(state, character));
        var highestMagicalDefense = characters.Max(character => GetMagicalDefense(state, character));
        var shield = Math.Max(0, highestPhysicalDefense) + Math.Max(0, highestMagicalDefense);
        if (shield <= 0)
            return;

        IncreaseSharedShield(state, player, shield, triggerResponses: false);
        TriggerShieldDrill(state, player.Characters.FirstOrDefault(character =>
            character.IsAlive
            && !IsDeploying(character)
            && character.Definition.TraitId == "shield-drill"));
        LogRelicTriggered(state, player, "relic-kingwall-standard");
    }

    private void TriggerMasonToken(GameState state, PlayerState owner)
    {
        if (!RelicEffects.TryUseTurnRelic(owner, "relic-mason-token"))
            return;

        var target = LowestHpAlly(owner);
        if (target is null)
            return;

        AddFortify(target, target.Id);
        LogRelicTriggered(state, owner, "relic-mason-token", target);
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("status", L10n.Status("fortify"))), "buff");
    }

    private int IncreaseSharedShield(
        GameState state,
        PlayerState owner,
        int amount,
        CharacterState? source = null,
        bool triggerResponses = true)
    {
        var gained = Math.Max(0, amount);
        if (gained <= 0)
            return 0;

        owner.SharedShield += gained;
        if (!triggerResponses)
            return gained;

        TriggerShieldDrill(state, source);
        if (source is not null)
            TriggerDeputyShieldmaiden(state, source);
        TriggerMasonToken(state, owner);
        return gained;
    }

    private void TriggerActiveHealingRelics(
        GameState state,
        CharacterState healer,
        IReadOnlyCollection<ActiveHealingResult> healingResults)
    {
        if (healingResults.Count == 0)
            return;

        var owner = state.FindOwner(healer);
        var combined = healingResults
            .Where(result => result.Requested > 0)
            .GroupBy(result => result.Target.Id)
            .Select(group => new ActiveHealingResult(
                group.First().Target,
                group.Sum(result => result.Requested),
                group.Sum(result => result.Restored)))
            .ToArray();
        if (combined.Length == 0)
            return;

        var highestActual = combined
            .Where(result => result.Restored > 0)
            .OrderByDescending(result => result.Restored)
            .ThenBy(result => result.Target.Slot)
            .FirstOrDefault();
        if (highestActual is not null
            && RelicEffects.TryUseTurnRelic(owner, "relic-white-lily-censer"))
        {
            AddSpellWard(highestActual.Target, healer.Id);
            LogRelicTriggered(state, owner, "relic-white-lily-censer", highestActual.Target);
            Log(state, L10n.Text("log.statusApplied",
                ("character", L10n.Character(highestActual.Target.Definition.Key)),
                ("characterId", L10n.Raw(highestActual.Target.Id)),
                ("status", L10n.Status("spell-ward"))), "buff");
        }

        if (highestActual is not null
            && RelicEffects.TryUseTurnRelic(owner, "relic-saint-chalice"))
        {
            var moraleBefore = highestActual.Target.Morale;
            highestActual.Target.Morale = Math.Min(
                highestActual.Target.MaxMorale,
                highestActual.Target.Morale + highestActual.Restored);
            var moraleRestored = highestActual.Target.Morale - moraleBefore;
            var shieldGained = IncreaseSharedShield(
                state,
                owner,
                highestActual.Restored + moraleRestored,
                healer);
            LogRelicTriggered(state, owner, "relic-saint-chalice", highestActual.Target);
            Log(state, L10n.Text("log.relicHealingResolved",
                ("relic", L10n.Reward("relic-saint-chalice")),
                ("character", L10n.Character(highestActual.Target.Definition.Key)),
                ("characterId", L10n.Raw(highestActual.Target.Id)),
                ("hp", L10n.Raw(highestActual.Restored)),
                ("morale", L10n.Raw(moraleRestored)),
                ("shield", L10n.Raw(shieldGained))), "heal");
        }

        var highestOverheal = combined
            .Where(result => result.Overheal > 0)
            .OrderByDescending(result => result.Overheal)
            .ThenBy(result => result.Target.Slot)
            .FirstOrDefault();
        if (highestOverheal is null)
            return;

        var mercyShield = Math.Min(highestOverheal.Overheal, GetActiveAttack(state, healer));
        if (mercyShield <= 0
            || !RelicEffects.TryUseTurnRelic(owner, "relic-mercy-cup"))
            return;

        var mercyGained = IncreaseSharedShield(state, owner, mercyShield, healer);
        LogRelicTriggered(state, owner, "relic-mercy-cup", highestOverheal.Target);
        Log(state, L10n.Text("log.relicShieldGained",
            ("relic", L10n.Reward("relic-mercy-cup")),
            ("amount", L10n.Raw(mercyGained))), "shield");
    }

    private void TriggerRoleActionRelics(
        GameState state,
        CharacterState actor,
        CharacterRoleAction action)
    {
        var owner = state.FindOwner(actor);
        if (owner.Id != state.ActivePlayerId)
            return;

        if (actor.Definition.CardType == CardType.Soldier
            && RelicEffects.TryUseTurnRelic(owner, "relic-command-sergeant-seal"))
            RefundActionPointFromRelic(state, owner, "relic-command-sergeant-seal", actor);

        if (action.Metadata.BaseApCost == 2
            && RelicEffects.TryUseTurnRelic(owner, "relic-command-table"))
            RefundActionPointFromRelic(state, owner, "relic-command-table", actor);
    }

    private void RefundActionPointFromRelic(
        GameState state,
        PlayerState owner,
        string relicId,
        CharacterState? source = null)
    {
        var before = state.ActionPoints;
        state.ActionPoints = Math.Min(GetMaxActionPoints(owner), state.ActionPoints + 1);
        var refunded = state.ActionPoints - before;
        LogRelicTriggered(state, owner, relicId, source);
        Log(state, L10n.Text("log.relicApRefunded",
            ("relic", L10n.Reward(relicId)),
            ("amount", L10n.Raw(refunded))), "system");
    }

    private void ApplyPendingRelicActionPointRefunds(GameState state, CharacterState source)
    {
        var pending = Math.Max(0, state.PendingRelicActionPointRefunds);
        state.PendingRelicActionPointRefunds = 0;
        for (var index = 0; index < pending; index++)
            RefundActionPointFromRelic(state, state.ActivePlayer, "relic-green-standard", source);
    }

    private void TriggerHpPaymentRelic(GameState state, CharacterState actor, int hpPaid)
    {
        if (hpPaid <= 0)
            return;

        var owner = state.FindOwner(actor);
        if (!RelicEffects.TryUseTurnRelic(owner, "relic-blood-coin"))
            return;

        var gained = IncreaseSharedShield(state, owner, hpPaid, actor);
        LogRelicTriggered(state, owner, "relic-blood-coin", actor);
        Log(state, L10n.Text("log.relicShieldGained",
            ("relic", L10n.Reward("relic-blood-coin")),
            ("amount", L10n.Raw(gained))), "shield");
    }

    private CharacterState? LowestHpAlly(PlayerState owner) =>
        owner.Characters
            .Where(character => character.IsAlive && !IsDeploying(character))
            .OrderBy(character => GetMaxHp(character) == 0 ? 1 : (double)character.CurrentHp / GetMaxHp(character))
            .ThenBy(character => character.CurrentHp)
            .FirstOrDefault();

    private void LogRelicTriggered(GameState state, PlayerState owner, string relicId, CharacterState? target = null)
    {
        Log(state, L10n.Text("log.relicTriggered",
            ("player", L10n.Player(owner.Name)),
            ("relic", L10n.Reward(relicId)),
            ("target", target is null ? L10n.Raw("") : L10n.Character(target.Definition.Key)),
            ("targetId", target is null ? L10n.Raw("") : L10n.Raw(target.Id))), "system");
    }

    internal static void AddStrongAttack(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var strongAttack = target.Statuses.OfType<StrongAttackStatus>().FirstOrDefault(status => !status.Expired);
        if (strongAttack is not null)
            strongAttack.AddTurns(turns);
        else
            target.Statuses.Add(new StrongAttackStatus(sourceCharacterId, turns));
    }

    internal static void AddMagicSurge(CharacterState target, Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    {
        var magicSurge = target.Statuses.OfType<MagicSurgeStatus>().FirstOrDefault(status => !status.Expired);
        if (magicSurge is not null)
            magicSurge.AddTurns(turns);
        else
            target.Statuses.Add(new MagicSurgeStatus(sourceCharacterId, turns));
    }

    private static void AddMightyStrike(CharacterState target, Guid sourceCharacterId, int stacks = 1)
    {
        var mightyStrike = target.Statuses.OfType<MightyStrikeStatus>().FirstOrDefault(status => !status.Expired);
        if (mightyStrike is not null)
            mightyStrike.AddStacks(stacks);
        else
            target.Statuses.Add(new MightyStrikeStatus(sourceCharacterId, stacks));
    }

    private static void AddChant(CharacterState target, Guid sourceCharacterId, int stacks = 1)
    {
        var chant = target.Statuses.OfType<ChantStatus>().FirstOrDefault(status => !status.Expired);
        if (chant is not null)
            chant.AddStacks(stacks);
        else
            target.Statuses.Add(new ChantStatus(sourceCharacterId, stacks));
    }

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
        int hpDamage)
    {
        if (hpDamage <= 0 || sourceOwner.Id != state.ActivePlayerId || targetOwner.Id == sourceOwner.Id)
            return;

        TryGainBp(state, sourceOwner, 1, BpReasonOwnTurnEnemyHpDamage);
    }

    private void TryAwardEnemyShieldBreakBp(
        GameState state,
        CharacterState source,
        PlayerState sourceOwner,
        PlayerState targetOwner,
        int shieldBefore,
        int shieldAbsorbed,
        bool deferActionPointRefund = false,
        int? shieldRemainingAfterDamage = null)
    {
        var remainingShield = shieldRemainingAfterDamage ?? targetOwner.SharedShield;
        if (shieldBefore <= 0 || shieldAbsorbed <= 0 || remainingShield > 0
            || sourceOwner.Id != state.ActivePlayerId || targetOwner.Id == sourceOwner.Id)
            return;

        TryGainBp(state, sourceOwner, 1, BpReasonBreakEnemyShield);
        if (RelicEffects.TryUseTurnRelic(sourceOwner, "relic-green-standard"))
        {
            if (deferActionPointRefund)
                state.PendingRelicActionPointRefunds++;
            else
                RefundActionPointFromRelic(state, sourceOwner, "relic-green-standard", source);
        }
        TriggerKnightShieldBreakRank2(state, targetOwner);
    }

    private void TriggerKnightShieldBreakRank2(GameState state, PlayerState owner)
    {
        var knight = owner.Characters.FirstOrDefault(character =>
            character.IsAlive
            && !IsDeploying(character)
            && HeroRankRules.HasRank2Path(character, "raise-bulwark")
            && !character.TraitsUsedThisTurn.Contains("interposing-shield-rank2-shieldbreak"));
        if (knight is null)
            return;

        AddStrongAttack(knight, knight.Id);
        knight.TraitsUsedThisTurn.Add("interposing-shield-rank2-shieldbreak");
        Log(state, L10n.Text("log.statusApplied",
            ("character", L10n.Character(knight.Definition.Key)),
            ("characterId", L10n.Raw(knight.Id)),
            ("status", L10n.Status("strong-attack"))), "buff");
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

        var damage = GetActiveAttack(state, attacker);
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
        if (IsDeploying(target))
            return 0;

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
        TriggerRelicsAfterDamageResolved(state, packet);
        foreach (var note in packet.Notes)
            Log(state, note, "status");
        var effectArg = effectId is "burning" or "guard"
            ? L10n.Status(effectId)
            : L10n.Trait(effectId);
        LogEffectDamage(state, packet, effectArg, damageType == DamageType.Physical ? "physical" : "magic");
        TryAwardEnemyShieldBreakBp(state, source, sourceOwner, targetOwner, shieldBefore, packet.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        ResolveEchoCrystalAfterDamageSequence(state, packet);
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
        if (IsDeploying(target))
            return 0;

        var source = state.FindCharacter(sourceCharacterId);
        var sourceOwner = state.FindOwner(source);
        var targetOwner = state.FindOwner(target);
        var packet = new DamagePacket
        {
            SourceCharacter = source,
            TargetCharacter = target,
            DamageType = DamageType.Absolute,
            Source = DamageSource.Trait,
            Amount = Math.Max(0, amount),
            IgnoresSharedShield = true,
            IgnoresTargetDefense = true
        };
        ApplyPredatorCrown(state, sourceOwner, packet);
        ApplyDirectHpDamage(packet);
        target.CurrentHp = Math.Max(0, target.CurrentHp - packet.Amount);
        Log(state, L10n.Text("log.effectDamage",
            ("effect", effectArg),
            ("source", L10n.Character(source.Definition.Key)),
            ("sourceId", L10n.Raw(source.Id)),
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
            ("amount", L10n.Raw(packet.Amount)),
            ("damageType", L10n.Damage(DamageType.Absolute))), "trait");
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        return packet.Amount;
    }

    private static void ValidateAttack(GameState state, CharacterState attacker, CharacterState defender)
    {
        if (attacker.PlayerId != state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.notActiveCharacter"));
        if (defender.PlayerId == state.ActivePlayerId)
            throw new GameRuleException(L10n.Text("error.cannotAttackAlly"));
        if (!attacker.IsAlive || !defender.IsAlive)
            throw new GameRuleException(L10n.Text("error.defeatedCharacter"));
        if (IsDeploying(attacker) || IsDeploying(defender))
            throw new GameRuleException(L10n.Text("error.deploying"));
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

    private static void EnsureNoPendingRelicReward(GameState state)
    {
        if (state.PendingRelicReward is not null)
            throw new GameRuleException(L10n.Text("error.pendingRelicReward"));
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
            character.TraitsUsedThisTurn.Clear();
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
            foreach (var character in state.Players.SelectMany(player => player.Characters)
                         .Where(character => character.IsAlive && !IsDeploying(character)))
                _traits.Get(character.Definition.TraitId).OnCharacterDefeated(context, character, defeatedCharacter);
        }
        ClampActionPointsToActiveMax(state);
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
            case "mend":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "crimson-lunge":
                if (targetCharacterId is null || !CanReceiveMightyStrike(actor, state.FindCharacter(targetCharacterId.Value)))
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                break;
            case "astral-focus":
                if (targetCharacterId is null || !CanReceiveChant(actor, state.FindCharacter(targetCharacterId.Value)))
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                break;
            case "miracle-standard":
            case "edict-of-victory":
            case "field-rations":
            case "grove-sanctuary":
            case "holy-bastion":
                RequireAllyTarget(state, actor, targetCharacterId);
                break;
            case "astral-alignment":
                if (GetAttackType(RequireAllyTarget(state, actor, targetCharacterId)) != DamageType.Magical)
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                break;
            case "militia-call":
                {
                    var target = RequireAllyTarget(state, actor, targetCharacterId);
                    if (target.Definition.Key != "peasant" && target.Definition.CardType != CardType.Soldier)
                        throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                    break;
                }
            case "abyssal-bargain":
                if (RequireAllyTarget(state, actor, targetCharacterId).CurrentHp <= 1)
                    throw new GameRuleException(L10n.Text("error.roleActionInvalidTarget"));
                break;
            case "thread-cut":
            case "starfall":
            case "archive-formula":
            case "call-the-hunt":
            case "dragon-breaker":
            case "nightmare-stare":
            case "iron-charge":
                RequireEnemyTarget(state, actor, targetCharacterId);
                break;
        }
    }

    private static bool CanReceiveStarReading(CharacterState target) =>
        target.IsAlive
        && !IsDeploying(target)
        && GetAttackType(target) == DamageType.Magical
        && target.AttackUsesThisTurn > 0
        && target.HasActed
        && !IsActiveAttackBlocked(target);

    private static bool CanReceiveMightyStrike(CharacterState actor, CharacterState target) =>
        target.PlayerId == actor.PlayerId
        && target.IsAlive
        && !IsDeploying(target)
        && GetAttackType(target) == DamageType.Physical;

    private static bool CanReceiveChant(CharacterState actor, CharacterState target) =>
        target.PlayerId == actor.PlayerId
        && target.IsAlive
        && !IsDeploying(target)
        && GetAttackType(target) == DamageType.Magical;

    private int DealRoleActionDamage(
        GameState state,
        CharacterState target,
        int amount,
        DamageType damageType,
        Guid sourceCharacterId,
        string roleActionId)
    {
        if (IsDeploying(target) || amount <= 0)
            return 0;

        var source = state.FindCharacter(sourceCharacterId);
        var sourceOwner = state.FindOwner(source);
        var targetOwner = state.FindOwner(target);
        var shieldBefore = targetOwner.SharedShield;
        var packet = new DamagePacket
        {
            SourceCharacter = source,
            TargetCharacter = target,
            DamageType = damageType,
            Source = DamageSource.RoleAction,
            Amount = amount,
            ReceivesMagicPowerBonus = damageType == DamageType.Magical
        };
        ModifyDamage(state, packet);
        target.CurrentHp = Math.Max(0, target.CurrentHp - packet.Amount);
        ResolvePreyZeroDamage(state, packet);
        TriggerRelicsAfterDamageResolved(state, packet);
        foreach (var note in packet.Notes)
            Log(state, note, "status");
        LogEffectDamage(state, packet, L10n.RoleAction(roleActionId), damageType == DamageType.Physical ? "physical" : "magic");
        TryAwardEnemyShieldBreakBp(state, source, sourceOwner, targetOwner, shieldBefore, packet.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        ResolveEchoCrystalAfterDamageSequence(state, packet);
        return packet.Amount;
    }

    private void LogEffectDamage(GameState state, DamagePacket packet, LocalizedArg effectArg, string tone)
    {
        var logKey = packet.MoraleDamage > 0 ? "log.effectDamageWithMorale" : "log.effectDamage";
        Log(state, L10n.Text(logKey,
            ("effect", effectArg),
            ("source", L10n.Character(packet.SourceCharacter.Definition.Key)),
            ("sourceId", L10n.Raw(packet.SourceCharacter.Id)),
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
            ("amount", L10n.Raw(packet.FinalCharacterDamage)),
            ("hpAmount", L10n.Raw(packet.Amount)),
            ("moraleAmount", L10n.Raw(packet.MoraleDamage)),
            ("damageType", L10n.Damage(packet.DamageType))), tone);
    }

    private int DealRelicDamage(
        GameState state,
        CharacterState target,
        int amount,
        DamageType damageType,
        Guid sourceCharacterId,
        string relicId,
        bool ignoreDefense = false)
    {
        if (IsDeploying(target) || amount <= 0)
            return 0;

        var source = state.FindCharacter(sourceCharacterId);
        var sourceOwner = state.FindOwner(source);
        var targetOwner = state.FindOwner(target);
        var shieldBefore = targetOwner.SharedShield;
        var packet = new DamagePacket
        {
            SourceCharacter = source,
            TargetCharacter = target,
            DamageType = damageType,
            Source = DamageSource.Trait,
            Amount = amount,
            ReceivesMagicPowerBonus = false,
            CanConsumeChargeStatuses = false,
            IgnoresTargetDefense = ignoreDefense
        };
        ModifyDamage(state, packet);
        target.CurrentHp = Math.Max(0, target.CurrentHp - packet.Amount);
        ResolvePreyZeroDamage(state, packet);
        TriggerNightBaitAfterDamage(state, sourceOwner, packet);
        foreach (var note in packet.Notes)
            Log(state, note, "status");
        LogEffectDamage(state, packet, L10n.Reward(relicId), damageType == DamageType.Magical ? "magic" : "physical");
        TryAwardEnemyShieldBreakBp(state, source, sourceOwner, targetOwner, shieldBefore, packet.ShieldAbsorbed);
        TryAwardEnemyDamageBp(state, sourceOwner, targetOwner, packet.Amount);
        return packet.Amount;
    }

    private void DealMoraleDamage(GameState state, CharacterState target, int amount, Guid sourceCharacterId, string roleActionId)
    {
        var damage = Math.Min(Math.Max(0, target.Morale), Math.Max(0, amount));
        if (damage <= 0)
            return;
        target.Morale = Math.Max(0, target.Morale - damage);
        var source = state.FindCharacter(sourceCharacterId);
        Log(state, L10n.Text("log.moraleDamage",
            ("effect", L10n.RoleAction(roleActionId)),
            ("source", L10n.Character(source.Definition.Key)),
            ("sourceId", L10n.Raw(source.Id)),
            ("character", L10n.Character(target.Definition.Key)),
            ("characterId", L10n.Raw(target.Id)),
                ("amount", L10n.Raw(damage))), "status");
    }

    private static IEnumerable<CharacterState> GetAlliesInArea(PlayerState owner, CharacterState center) =>
        owner.Characters.Where(character =>
            character.IsAlive
            && !IsDeploying(character)
            && Math.Abs(character.Slot - center.Slot) <= 1);

    private static IEnumerable<CharacterState> GetEnemiesInArea(PlayerState owner, CharacterState center) =>
        owner.Characters.Where(character =>
            character.IsAlive
            && !IsDeploying(character)
            && Math.Abs(character.Slot - center.Slot) <= 1);

    private static int CleanseDebuffs(CharacterState target, bool all)
    {
        var debuffs = target.Statuses
            .Where(status => !status.IsBuff && status.IsDispellable && !status.Expired)
            .ToArray();
        if (debuffs.Length == 0)
            return 0;
        foreach (var debuff in all ? debuffs : debuffs.Take(1))
            target.Statuses.Remove(debuff);
        return all ? debuffs.Length : 1;
    }

    private static int CountThreadCutMarks(CharacterState target) =>
        target.Statuses.Count(status => !status.Expired
            && status.Id is "marked" or "prey" or "nightmare-prey" or "hunted"
                or "burning" or "void" or "exhaustion" or "erosion" or "trembling" or "vulnerable");

    private void ApplyTremblingAndVulnerable(GameState state, CharacterState target, CharacterState actor)
    {
        ApplyTrembling(state, actor, target);
        if (target.IsAlive)
            ApplyVulnerable(state, actor, target);
    }

    private void ResolvePreyZeroDamage(GameState state, DamagePacket packet)
    {
        if (packet.Amount != 0 || !packet.TargetCharacter.IsAlive)
            return;

        foreach (var prey in packet.TargetCharacter.Statuses.OfType<PreyStatus>().Where(status => !status.Expired).ToArray())
            DealAbsoluteDamage(state, packet.TargetCharacter, PreyStatus.AbsoluteDamage, prey.SourceCharacterId, L10n.RoleAction("predatory-gaze"));
        foreach (var prey in packet.TargetCharacter.Statuses.OfType<NightmarePreyStatus>().Where(status => !status.Expired).ToArray())
        {
            DealAbsoluteDamage(state, packet.TargetCharacter, prey.Magnitude, prey.SourceCharacterId, L10n.RoleAction("nightmare-stare"));
            prey.Consume();
        }
    }

    private void ResolveVictoryEdictAfterActiveAttack(GameState state, CharacterState attacker, CharacterState target)
    {
        foreach (var status in attacker.Statuses.OfType<VictoryEdictStatus>().Where(status => !status.Expired).ToArray())
        {
            if (target.IsAlive)
                DealAbsoluteDamage(state, target, status.Magnitude, status.SourceCharacterId, L10n.RoleAction("edict-of-victory"));
            if (target.CurrentHp <= 0)
            {
                TryGainBp(state, state.ActivePlayer, 1, BpReasonRank3Kill);
                state.ActivePlayer.PendingActionPointDebt = Math.Max(0, state.ActivePlayer.PendingActionPointDebt - 1);
            }
            status.Consume();
        }
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

    private void ResolveAbyssalBargainAfterActiveAttack(GameState state, CharacterState attacker, CharacterState target)
    {
        foreach (var status in attacker.Statuses.OfType<AbyssalBargainStatus>().Where(status => !status.Expired).ToArray())
        {
            if (target.IsAlive)
                DealAbsoluteDamage(state, target, status.Magnitude, status.SourceCharacterId, L10n.RoleAction("abyssal-bargain"));
            if (target.CurrentHp <= 0)
            {
                TryGainBp(state, state.ActivePlayer, 1, BpReasonRank3Kill);
                var source = state.FindCharacter(status.SourceCharacterId);
                Heal(source, status.LifeCost);
            }
            status.Consume();
        }
    }

    private static void ResolveGloryRoarAfterActiveAttack(GameState state, CharacterState attacker)
    {
        if (attacker.Statuses.Any(status => status.Id == "glory-roar" && !status.Expired))
            attacker.CurrentHp = Math.Max(1, attacker.CurrentHp - 1);
    }

    private void ResolveHuntedAfterActiveAttack(GameState state, CharacterState attacker, CharacterState target)
    {
        if (target.CurrentHp > 0 || target.Statuses.All(status => status.Id != "hunted" || status.Expired))
            return;
        TryGainBp(state, state.ActivePlayer, 1, BpReasonRank3Kill);
    }

    internal static bool AddBurning(CharacterState target, Guid sourceCharacterId, int stacks = 1, GameState? state = null)
    {
        var amount = Math.Max(1, stacks);
        if (state is not null)
        {
            var sourceOwner = state.Players.FirstOrDefault(player =>
                player.Characters.Any(character => character.Id == sourceCharacterId));
            if (sourceOwner is not null && RelicEffects.TryUseTurnRelic(sourceOwner, "relic-ember-astrolabe"))
                amount++;
        }

        var burning = target.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired);
        if (burning is not null)
        {
            burning.AddStacks(amount);
            return false;
        }

        target.Statuses.Add(new BurningStatus(sourceCharacterId, target.PlayerId, amount));
        return true;
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
