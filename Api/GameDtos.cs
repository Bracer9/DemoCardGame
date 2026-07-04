using TinyPixelFights.Domain;

namespace TinyPixelFights.Api;

public sealed record GameView(
    Guid GameId, int TurnNumber, int RoundNumber, int ActionPoints, int MaxActionPoints,
    bool CanDeployShield, int NextShieldCost, int ShieldDeploymentsThisTurn, Guid ActivePlayerId,
    string ActivePlayerName, string Phase, Guid? WinnerPlayerId, bool IsDraw,
    Guid ViewerPlayerId, bool CanControl, bool IsHost, IReadOnlyList<PlayerView> Players,
    RewardWindowView? RewardWindow, PendingRoleActionUpgradeView? PendingRoleActionUpgrade,
    HeroDraftView? HeroDraft,
    IReadOnlyList<GameLogEntry> Log);

public sealed record PlayerView(Guid Id, string Name, bool IsActive, int SharedShield,
    int SharedShieldPhysicalDefense, int SharedShieldMagicalDefense,
    int ActiveCharacterCount, BattlePointView BattlePoints, IReadOnlyList<CharacterView> Characters);

public sealed record BattlePointView(
    int Current,
    int Max,
    int GainedThisTurn,
    int GainCapPerTurn,
    string? LastReasonId);

public sealed record CharacterView(
    Guid Id, string Key, string AssetUrl, string ColoredAssetUrl, int Slot, int Cost, int Attack, int BaseAttack,
    string CardType, int SoldierRank,
    string AttackType, int PhysicalDefense, int BasePhysicalDefense, int MagicalDefense, int BaseMagicalDefense,
    int CurrentHp, int MaxHp, bool IsAlive, bool IsInBattle, string Zone, bool HasActed, bool CanAct,
    IReadOnlyList<TraitView> Traits, IReadOnlyList<RoleActionView> RoleActions,
    IReadOnlyList<RoleActionView> RoleActionChoices, IReadOnlyList<StatusView> Statuses,
    DeputyView? Deputy, DeputyPreviewView? DeputyPreview,
    bool CanAssignAsDeputy, string? AssignDeputyDisabledReason);

public sealed record TraitView(
    string Id,
    string TriggerKind,
    string ScopeKind,
    string EffectKind,
    bool IsReady,
    LocalizedText? UnavailableReason);

public sealed record RoleActionView(
    string Id,
    int SlotIndex,
    string ActivationMode,
    IReadOnlyList<string> ValidTargetKinds,
    int Cost,
    bool IsRepeatable,
    int CooldownTurns,
    int CooldownRemaining,
    bool Enabled,
    LocalizedText? DisabledReason);
public sealed record StatusView(string Id, bool IsBuff, int Magnitude, bool IsAura = false, bool IsDispellable = true);
public sealed record DeputyView(Guid SoldierId, string SoldierKey, string EffectId, string StatKind, int StatValue);
public sealed record DeputyPreviewView(string EffectId, string StatKind, int StatValue);
public sealed record AttackRequest(Guid AttackerId, Guid DefenderId);
public sealed record SelectRewardRequest(string InstanceId);
public sealed record SelectHeroDraftRequest(string CharacterKey);
public sealed record SelectSoldierDraftRequest(IReadOnlyList<string> CharacterKeys);
public sealed record UpgradeSoldierDraftRequest(string CharacterKey, Guid TargetCharacterId);
public sealed record SelectRoleActionUpgradeRequest(Guid CharacterId, string RoleActionId);
public sealed record UseRoleActionRequest(Guid CharacterId, string RoleActionId, Guid? TargetCharacterId);
public sealed record AssignDeputyRequest(Guid SoldierId, Guid HeroId);
public sealed record ApiEnvelope<T>(T Data, CombatOutcome? Combat = null, LocalizedText? Message = null);

public sealed record PendingRoleActionUpgradeView(Guid PlayerId, string RewardId, bool CanChoose);

public sealed record HeroDraftView(
    Guid PlayerId,
    string Kind,
    bool CanChoose,
    int ResetCount,
    int NextResetCost,
    int MaxSelections,
    IReadOnlyList<string> SelectedKeys,
    IReadOnlyList<HeroDraftCandidateView> Candidates);

public sealed record HeroDraftCandidateView(
    string Key,
    string CardType,
    string AssetUrl,
    string ColoredAssetUrl,
    int Cost,
    int Attack,
    int MaxHp,
    string AttackType,
    int PhysicalDefense,
    int MagicalDefense,
    string TraitId);

public sealed record RewardWindowView(
    Guid PlayerId,
    int RoundNumber,
    int ResetCount,
    int NextResetCost,
    int PurchaseCount,
    bool CanChoose,
    IReadOnlyList<RewardOptionView> Options);

public sealed record RewardOptionView(
    string InstanceId,
    string RewardId,
    int Cost,
    string Rarity,
    bool CanAfford);

public sealed record DamageForecast(
    int Min, int Max, string DamageType, int ShieldDefenseReduction, int DefenseReduction,
    int ReductionChancePercent, bool ShieldWillAbsorb, int ShieldAbsorb, bool GuardWillTrigger, int GuardDamage);

public sealed record AttackPreview(
    bool IsValid, LocalizedText? Error, Guid AttackerId, Guid DefenderId, int Cost,
    DamageForecast Attack, DamageForecast Counter, string TraitId, bool TraitConditionPossible,
    LocalizedText TraitForecast, IReadOnlyList<LocalizedText> Notes);

public sealed class GameViewFactory
{
    private readonly GameEngine _engine;
    public GameViewFactory(GameEngine engine) => _engine = engine;
    public GameView Create(GameState state) => Create(state, state.ActivePlayerId, true);

    public GameView Create(GameState state, Guid viewerPlayerId, bool isHost)
    {
        var players = state.Players.Select(player => new PlayerView(
            player.Id, player.Name, player.Id == state.ActivePlayerId, player.SharedShield,
            player.SharedShieldPhysicalDefense, player.SharedShieldMagicalDefense,
            player.ActiveCharacterCount,
            new BattlePointView(
                player.BattlePoints.Current,
                player.BattlePoints.Max,
                player.BattlePoints.GainedThisTurn,
                GameEngine.BattlePointGainCapPerTurn,
                player.BattlePoints.LastReasonId),
            player.Characters.OrderBy(character => character.Slot)
                .Select(character => CreateCharacter(state, player, character, viewerPlayerId)).ToArray())).ToArray();

        return new GameView(
            state.Id, state.TurnNumber, (state.TurnNumber + 1) / 2, state.ActionPoints,
            GameEngine.MaxActionPoints,
            state.Phase == GamePhase.Playing && state.RewardWindow is null && state.PendingRoleActionUpgrade is null
                && state.PendingHeroDraft is null
                && state.ActionPoints >= GameEngine.GetShieldCost(state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayer.SharedShield)
                && state.ActivePlayer.ShieldDeploymentsThisTurn < GameEngine.MaxShieldDeploymentsPerTurn,
            GameEngine.GetShieldCost(state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayer.SharedShield),
            state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayerId, state.ActivePlayer.Name,
            state.Phase.ToString(), state.WinnerPlayerId, state.IsDraw, viewerPlayerId,
            state.Phase == GamePhase.Playing && state.ActivePlayerId == viewerPlayerId
                && state.RewardWindow is null && state.PendingRoleActionUpgrade is null && state.PendingHeroDraft is null,
            isHost, players, CreateRewardWindow(state, viewerPlayerId),
            CreatePendingRoleActionUpgrade(state, viewerPlayerId), CreateHeroDraft(state, viewerPlayerId),
            state.Log.TakeLast(30).ToArray());
    }

    private static PendingRoleActionUpgradeView? CreatePendingRoleActionUpgrade(GameState state, Guid viewerPlayerId) =>
        state.PendingRoleActionUpgrade is null
            ? null
            : new PendingRoleActionUpgradeView(
                state.PendingRoleActionUpgrade.PlayerId,
                state.PendingRoleActionUpgrade.RewardId,
                state.PendingRoleActionUpgrade.PlayerId == viewerPlayerId && state.ActivePlayerId == viewerPlayerId);

    private static HeroDraftView? CreateHeroDraft(GameState state, Guid viewerPlayerId)
    {
        if (state.PendingHeroDraft is null)
            return null;

        var definitions = CharacterCatalog.All.ToDictionary(item => item.Key);
        return new HeroDraftView(
            state.PendingHeroDraft.PlayerId,
            state.PendingHeroDraft.Kind.ToString(),
            state.PendingHeroDraft.PlayerId == viewerPlayerId && state.ActivePlayerId == viewerPlayerId,
            state.PendingHeroDraft.ResetCount,
            GameEngine.GetRewardResetCost(state.PendingHeroDraft.ResetCount),
            state.PendingHeroDraft.MaxSelections,
            state.PendingHeroDraft.SelectedKeys.ToArray(),
            state.PendingHeroDraft.CandidateKeys
                .Where(definitions.ContainsKey)
                .Select(key =>
                {
                    var definition = definitions[key];
                    return new HeroDraftCandidateView(
                        definition.Key,
                        definition.CardType.ToString(),
                        $"/assets/{definition.AssetFile}",
                        $"/assets/{definition.ColoredAssetFile}",
                        definition.Cost,
                        definition.Attack,
                        definition.MaxHp,
                        definition.AttackType.ToString(),
                        definition.PhysicalDefense,
                        definition.MagicalDefense,
                        definition.TraitId);
                }).ToArray());
    }

    private static RewardWindowView? CreateRewardWindow(GameState state, Guid viewerPlayerId)
    {
        if (state.RewardWindow is null)
            return null;

        var player = state.Players.Single(item => item.Id == state.RewardWindow.PlayerId);
        var definitions = RewardCatalog.All.ToDictionary(item => item.Id);
        return new RewardWindowView(
            state.RewardWindow.PlayerId,
            state.RewardWindow.RoundNumber,
            state.RewardWindow.ResetCount,
            GameEngine.GetRewardResetCost(state.RewardWindow.ResetCount),
            state.RewardWindow.PurchaseCount,
            state.RewardWindow.PlayerId == viewerPlayerId && state.ActivePlayerId == viewerPlayerId,
            state.RewardWindow.Options.Select(option =>
            {
                var definition = definitions[option.RewardId];
                return new RewardOptionView(
                    option.InstanceId,
                    option.RewardId,
                    option.Cost,
                    definition.Rarity,
                    player.BattlePoints.Current >= option.Cost
                    && (definition.Kind != RewardKind.HeroRecruit || player.ActiveCharacterCount < 4));
            }).ToArray());
    }

    private CharacterView CreateCharacter(GameState state, PlayerState player, CharacterState character,
        Guid viewerPlayerId)
    {
        var trait = _engine.GetTrait(character);
        var currentAttack = _engine.GetActiveAttack(character);
        if (character.Definition.AttackType == DamageType.Magical
            && character.Statuses.Any(status => status.Id == "magic-power" && !status.Expired))
            currentAttack++;

        var statuses = character.Statuses.Where(status => !status.Expired)
            .Select(status => new StatusView(
                status.Id,
                status.IsBuff,
                status.Magnitude,
                IsAura: status.Id == "magic-power",
                IsDispellable: status.IsDispellable)).ToList();

        if (player.Characters.Any(ally => ally.IsAlive && ally.IsInBattle && ally.Definition.Key == "princess"))
            statuses.Add(new StatusView("blessing", true, 1, true, false));
        if (player.Characters.Any(ally => ally.IsAlive && ally.IsInBattle && ally.Definition.Key == "oracle"))
            statuses.Add(new StatusView("foresight", true, 1, true, false));
        var knight = player.Characters.FirstOrDefault(ally => ally.IsAlive
            && ally.IsInBattle && ally.Definition.Key == "knight" && !ally.GuardConsumed);
        if (knight is not null && character.IsAlive)
            statuses.Add(new StatusView("guard", true, 1, true, false));

        var canAct = state.Phase == GamePhase.Playing && state.RewardWindow is null && state.PendingRoleActionUpgrade is null
            && state.PendingHeroDraft is null
            && player.Id == state.ActivePlayerId
            && player.Id == viewerPlayerId && character.IsAlive && !character.HasActed
            && !GameEngine.IsActiveAttackBlocked(character)
            && character.Definition.Cost <= state.ActionPoints;

        var canUseRoleActions = player.Id == viewerPlayerId && state.PendingHeroDraft is null;
        var roleActions = _engine.GetRoleActions(character)
            .Select((action, index) => new RoleActionView(
                action.Metadata.Id,
                index,
                action.Metadata.ActivationMode.ToString(),
                action.Metadata.ValidTargetKinds.Select(kind => kind.ToString()).ToArray(),
                action.Metadata.BaseApCost,
                action.Metadata.IsRepeatable,
                action.Metadata.CooldownTurns,
                character.RoleActionCooldowns.GetValueOrDefault(action.Metadata.Id),
                canUseRoleActions && action.IsAvailable(state, character),
                canUseRoleActions
                    ? action.UnavailableReason(state, character)
                    : L10n.Text("reason.opponentTurn")))
            .ToArray();
        var canChooseUpgrade = state.PendingRoleActionUpgrade is not null
            && state.PendingRoleActionUpgrade.PlayerId == player.Id
            && state.ActivePlayerId == player.Id;
        var roleActionChoices = canChooseUpgrade
            ? _engine.GetRoleActionUpgradeChoices(character)
                .Select((action, index) => new RoleActionView(
                    action.Metadata.Id,
                    index,
                    action.Metadata.ActivationMode.ToString(),
                    action.Metadata.ValidTargetKinds.Select(kind => kind.ToString()).ToArray(),
                    action.Metadata.BaseApCost,
                    action.Metadata.IsRepeatable,
                    action.Metadata.CooldownTurns,
                    0,
                    true,
                    null))
                .ToArray()
            : [];

        var deputyDefinition = DeputyCatalog.FindById(character.DeputyEffectId);
        var deputySoldier = character.DeputySoldierId is null
            ? null
            : state.Players.SelectMany(item => item.Characters)
                .FirstOrDefault(item => item.Id == character.DeputySoldierId);
        var deputy = deputyDefinition is null || deputySoldier is null
            ? null
            : new DeputyView(
                deputySoldier.Id,
                deputySoldier.Definition.Key,
                deputyDefinition.Id,
                deputyDefinition.StatKind.ToString(),
                deputyDefinition.StatValue);
        var deputyPreviewDefinition = character.Definition.CardType == CardType.Soldier
            ? DeputyCatalog.FindBySoldierKey(character.Definition.Key)
            : null;
        var deputyPreview = deputyPreviewDefinition is null
            ? null
            : new DeputyPreviewView(
                deputyPreviewDefinition.Id,
                deputyPreviewDefinition.StatKind.ToString(),
                deputyPreviewDefinition.StatValue);
        var assignDeputyDisabledReason = player.Id == viewerPlayerId
            ? _engine.GetAssignDeputyDisabledReason(state, character)
            : "opponent-turn";

        return new CharacterView(
            character.Id, character.Definition.Key, $"/assets/{character.Definition.AssetFile}",
            $"/assets/{GetColoredAssetFile(character)}",
            character.Slot, character.Definition.Cost, currentAttack, _engine.GetBaseAttack(character),
            character.Definition.CardType.ToString(), character.SoldierRank,
            character.Definition.AttackType.ToString(),
            _engine.GetPhysicalDefense(character), character.Definition.PhysicalDefense,
            _engine.GetMagicalDefense(character), character.Definition.MagicalDefense,
            character.CurrentHp, _engine.GetMaxHp(character),
            character.IsAlive, character.IsInBattle, character.Zone.ToString(), character.HasActed, canAct,
            [
                new TraitView(
                    trait.Metadata.Id,
                    trait.Metadata.TriggerKind.ToString(),
                    trait.Metadata.ScopeKind.ToString(),
                    trait.Metadata.EffectKind.ToString(),
                    trait.IsReady(state, character),
                    trait.UnavailableReason(state, character))
            ],
            roleActions,
            roleActionChoices,
            statuses,
            deputy,
            deputyPreview,
            assignDeputyDisabledReason is null,
            assignDeputyDisabledReason);
    }

    private static string GetColoredAssetFile(CharacterState character) =>
        character.Definition.CardType == CardType.Soldier
        && character.SoldierRank >= 2
        && character.Definition.Rank2ColoredAssetFile is not null
            ? character.Definition.Rank2ColoredAssetFile
            : character.Definition.ColoredAssetFile;
}
