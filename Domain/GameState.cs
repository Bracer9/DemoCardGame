namespace TinyPixelFights.Domain;

public sealed class CharacterState
{
    public required Guid Id { get; init; }
    public required Guid PlayerId { get; init; }
    public required int Slot { get; init; }
    public required CharacterDefinition Definition { get; init; }
    public required int CurrentHp { get; set; }
    public int Morale { get; set; } = 5;
    public int MaxMorale { get; set; } = 5;
    public CharacterZone Zone { get; set; } = CharacterZone.Battlefield;
    public int AttackUsesThisTurn { get; set; }
    public int BonusAttackUsesThisTurn { get; set; }
    public int MaxAttackUsesThisTurn => 1 + Math.Max(0, BonusAttackUsesThisTurn);
    public bool HasActed
    {
        get => AttackUsesThisTurn >= MaxAttackUsesThisTurn;
        set => AttackUsesThisTurn = value ? MaxAttackUsesThisTurn : 0;
    }
    public bool GuardConsumed { get; set; }
    public bool DefeatLogged { get; set; }
    public int SoldierRank { get; set; }
    public int HeroRank { get; set; }
    public string? HeroPathRoleActionId { get; set; }
    public List<StatusEffect> Statuses { get; } = [];
    public List<string> RoleActionIds { get; } = [];
    public HashSet<string> RoleActionsUsedThisTurn { get; } = [];
    public Dictionary<string, int> RoleActionCooldowns { get; } = [];
    public HashSet<string> TraitsUsedThisTurn { get; } = [];
    public Guid? DeputySoldierId { get; set; }
    public Guid? DeputyHostHeroId { get; set; }
    public string? DeputyEffectId { get; set; }
    public bool IsInBattle => Zone == CharacterZone.Battlefield;
    public bool IsDraftCandidate => Zone == CharacterZone.DraftCandidate;
    public bool IsAlive => (IsInBattle || IsDraftCandidate) && CurrentHp > 0;
}

public sealed class PendingRoleActionUpgradeState
{
    public required Guid PlayerId { get; init; }
    public required string RewardId { get; init; }
    public string? RewardInstanceId { get; init; }
}

public enum HeroDraftKind
{
    Opening,
    Recruit,
    TestOpening,
    SoldierOpening,
    SoldierRecruit
}

public sealed class PendingHeroDraftState
{
    public required Guid PlayerId { get; set; }
    public required HeroDraftKind Kind { get; init; }
    public string? RewardInstanceId { get; init; }
    public int ResetCount { get; set; }
    public int MaxSelections { get; init; } = 1;
    public List<string> CandidateKeys { get; } = [];
    public List<string> SelectedKeys { get; } = [];
}

public sealed class PlayerState
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public List<CharacterState> Characters { get; } = [];
    public BattlePointState BattlePoints { get; } = new();
    public int SharedShield { get; set; }
    public int SharedShieldPhysicalDefense { get; set; }
    public int SharedShieldMagicalDefense { get; set; }
    public Guid? SharedShieldDefenseExpireOnTurnStartPlayerId { get; set; }
    public int ShieldDeploymentsThisTurn { get; set; }
    public int PendingActionPointDebt { get; set; }
    public bool HasClaimedFreeHeroRoleActionUpgrade { get; set; }
    public bool FirstRoleActionBpGrantedThisTurn { get; set; }
    public HashSet<string> DeputyPassivesUsedThisTurn { get; } = [];
    public int ActiveCharacterCount => Characters.Count(character => character.IsInBattle);
    public bool IsDefeated => Characters.All(character => !character.IsAlive);
}

public sealed class RewardWindowState
{
    public required Guid PlayerId { get; set; }
    public required int RoundNumber { get; set; }
    public int ResetCount { get; set; }
    public int PurchaseCount { get; set; }
    public List<RewardOptionState> Options { get; } = [];
}

public sealed record RewardOptionState(
    string InstanceId,
    string RewardId,
    int Cost);

public sealed class BattlePointState
{
    public int Current { get; set; }
    public int Max { get; set; }
    public int GainedThisTurn { get; set; }
    public string? LastReasonId { get; set; }
}

public sealed record BattlePointGainResult(
    Guid PlayerId,
    string ReasonId,
    int Requested,
    int Gained,
    int Blocked,
    int Current,
    int Max,
    int GainedThisTurn);

public sealed record GameLogEntry(int Sequence, int Turn, LocalizedText Message, string Tone);

public sealed class GameState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public List<PlayerState> Players { get; } = [];
    public required Guid ActivePlayerId { get; set; }
    public int TurnNumber { get; set; } = 1;
    public required int ActionPoints { get; set; }
    public int ActionsTakenThisTurn { get; set; }
    public int ActiveAttacksTakenThisTurn { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.Playing;
    public Guid? WinnerPlayerId { get; set; }
    public bool IsDraw { get; set; }
    public List<GameLogEntry> Log { get; } = [];
    public int LogSequence { get; set; }
    public RewardWindowState? RewardWindow { get; set; }
    public PendingRoleActionUpgradeState? PendingRoleActionUpgrade { get; set; }
    public PendingHeroDraftState? PendingHeroDraft { get; set; }
    public Guid? OpeningFirstPlayerId { get; set; }
    public Guid? LocalViewerPlayerId { get; set; }
    public Guid? AiPlayerId { get; set; }
    public bool IsTestMode { get; set; }
    public HashSet<string> ResolvedRewardWindows { get; } = [];

    public PlayerState ActivePlayer => Players.Single(player => player.Id == ActivePlayerId);
    public PlayerState Opponent => Players.Single(player => player.Id != ActivePlayerId);

    public CharacterState FindCharacter(Guid id) =>
        Players.SelectMany(player => player.Characters).Single(character => character.Id == id);

    public PlayerState FindOwner(CharacterState character) =>
        Players.Single(player => player.Id == character.PlayerId);
}
