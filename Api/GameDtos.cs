using TinyPixelFights.Domain;

namespace TinyPixelFights.Api;

public sealed record GameView(
    Guid GameId, int TurnNumber, int RoundNumber, int ActionPoints, int MaxActionPoints,
    bool CanDeployShield, int NextShieldCost, int ShieldDeploymentsThisTurn, Guid ActivePlayerId,
    string ActivePlayerName, string Phase, Guid? WinnerPlayerId, bool IsDraw,
    Guid ViewerPlayerId, bool CanControl, bool IsHost, IReadOnlyList<PlayerView> Players,
    IReadOnlyList<GameLogEntry> Log);

public sealed record PlayerView(Guid Id, string Name, bool IsActive, int SharedShield,
    int ActiveCharacterCount, IReadOnlyList<CharacterView> Characters);

public sealed record CharacterView(
    Guid Id, string Key, string AssetUrl, string ColoredAssetUrl, int Slot, int Cost, int Attack, int BaseAttack,
    string AttackType, int PhysicalDefense, int MagicalDefense,
    int CurrentHp, int MaxHp, bool IsAlive, bool IsInBattle, string Zone, bool HasActed, bool CanAct,
    SkillView Skill, IReadOnlyList<StatusView> Statuses);

public sealed record SkillView(string Id, string Kind, bool IsReady, LocalizedText? UnavailableReason);
public sealed record StatusView(string Id, bool IsBuff, int Magnitude, bool IsAura = false);
public sealed record AttackRequest(Guid AttackerId, Guid DefenderId);
public sealed record ApiEnvelope<T>(T Data, CombatOutcome? Combat = null, LocalizedText? Message = null);

public sealed record DamageForecast(
    int Min, int Max, string DamageType, int DefenseReduction, int ReductionChancePercent, bool ShieldWillAbsorb,
    int ShieldAbsorb, bool GuardWillTrigger, int GuardDamage);

public sealed record AttackPreview(
    bool IsValid, LocalizedText? Error, Guid AttackerId, Guid DefenderId, int Cost,
    DamageForecast Attack, DamageForecast Counter, string SkillId, bool SkillConditionPossible,
    LocalizedText SkillForecast, IReadOnlyList<LocalizedText> Notes);

public sealed class GameViewFactory
{
    private readonly GameEngine _engine;
    public GameViewFactory(GameEngine engine) => _engine = engine;
    public GameView Create(GameState state) => Create(state, state.ActivePlayerId, true);

    public GameView Create(GameState state, Guid viewerPlayerId, bool isHost)
    {
        var players = state.Players.Select(player => new PlayerView(
            player.Id, player.Name, player.Id == state.ActivePlayerId, player.SharedShield,
            player.ActiveCharacterCount,
            player.Characters.OrderBy(character => character.Slot)
                .Select(character => CreateCharacter(state, player, character, viewerPlayerId)).ToArray())).ToArray();

        return new GameView(
            state.Id, state.TurnNumber, (state.TurnNumber + 1) / 2, state.ActionPoints,
            GameEngine.MaxActionPoints,
            state.Phase == GamePhase.Playing && state.ActionPoints >= GameEngine.GetShieldCost(state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayer.SharedShield)
                && state.ActivePlayer.ShieldDeploymentsThisTurn < GameEngine.MaxShieldDeploymentsPerTurn,
            GameEngine.GetShieldCost(state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayer.SharedShield),
            state.ActivePlayer.ShieldDeploymentsThisTurn, state.ActivePlayerId, state.ActivePlayer.Name,
            state.Phase.ToString(), state.WinnerPlayerId, state.IsDraw, viewerPlayerId,
            state.Phase == GamePhase.Playing && state.ActivePlayerId == viewerPlayerId,
            isHost, players, state.Log.TakeLast(30).ToArray());
    }

    private CharacterView CreateCharacter(GameState state, PlayerState player, CharacterState character,
        Guid viewerPlayerId)
    {
        var skill = _engine.GetSkill(character);
        var currentAttack = _engine.GetActiveAttack(character);
        if (character.Definition.AttackType == DamageType.Magical
            && character.Statuses.Any(status => status.Id == "magic-power" && !status.Expired))
            currentAttack++;

        var statuses = character.Statuses.Where(status => !status.Expired)
            .Select(status => new StatusView(status.Id, status.IsBuff, status.Magnitude)).ToList();

        if (player.Characters.Any(ally => ally.IsAlive && ally.Definition.Key == "princess"))
            statuses.Add(new StatusView("blessing", true, 1, true));
        if (player.Characters.Any(ally => ally.IsAlive && ally.Definition.Key == "oracle"))
            statuses.Add(new StatusView("foresight", true, 1, true));
        if (player.SharedShield > 0)
            statuses.Add(new StatusView("team-shield", true, player.SharedShield, true));

        var knight = player.Characters.FirstOrDefault(ally => ally.IsAlive
            && ally.Definition.Key == "knight" && !ally.GuardConsumed);
        if (knight is not null && character.IsAlive)
            statuses.Add(new StatusView("guard", true, 1, true));

        var canAct = state.Phase == GamePhase.Playing && player.Id == state.ActivePlayerId
            && player.Id == viewerPlayerId && character.IsAlive && !character.HasActed
            && character.Definition.Cost <= state.ActionPoints;

        return new CharacterView(
            character.Id, character.Definition.Key, $"/assets/{character.Definition.AssetFile}",
            $"/assets/{character.Definition.ColoredAssetFile}",
            character.Slot, character.Definition.Cost, currentAttack, _engine.GetBaseAttack(character),
            character.Definition.AttackType.ToString(), _engine.GetPhysicalDefense(character), _engine.GetMagicalDefense(character),
            character.CurrentHp, character.Definition.MaxHp,
            character.IsAlive, character.IsInBattle, character.Zone.ToString(), character.HasActed, canAct,
            new SkillView(skill.Metadata.Id, skill.Metadata.Kind.ToString(),
                skill.IsReady(state, character), skill.UnavailableReason(state, character)), statuses);
    }
}
