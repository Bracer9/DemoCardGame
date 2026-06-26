namespace TinyPixelFights.Domain;

public sealed class CharacterState
{
    public required Guid Id { get; init; }
    public required Guid PlayerId { get; init; }
    public required int Slot { get; init; }
    public required CharacterDefinition Definition { get; init; }
    public required int CurrentHp { get; set; }
    public CharacterZone Zone { get; set; } = CharacterZone.Battlefield;
    public bool HasActed { get; set; }
    public bool GuardConsumed { get; set; }
    public bool DefeatLogged { get; set; }
    public List<StatusEffect> Statuses { get; } = [];
    public bool IsInBattle => Zone == CharacterZone.Battlefield;
    public bool IsAlive => IsInBattle && CurrentHp > 0;
}

public sealed class PlayerState
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public List<CharacterState> Characters { get; } = [];
    public int SharedShield { get; set; }
    public int ShieldDeploymentsThisTurn { get; set; }
    public int ActiveCharacterCount => Characters.Count(character => character.IsInBattle);
    public bool IsDefeated => Characters.All(character => !character.IsAlive);
}

public sealed record GameLogEntry(int Sequence, int Turn, LocalizedText Message, string Tone);

public sealed class GameState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public List<PlayerState> Players { get; } = [];
    public required Guid ActivePlayerId { get; set; }
    public int TurnNumber { get; set; } = 1;
    public required int ActionPoints { get; set; }
    public int ActionsTakenThisTurn { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.Playing;
    public Guid? WinnerPlayerId { get; set; }
    public bool IsDraw { get; set; }
    public List<GameLogEntry> Log { get; } = [];
    public int LogSequence { get; set; }

    public PlayerState ActivePlayer => Players.Single(player => player.Id == ActivePlayerId);
    public PlayerState Opponent => Players.Single(player => player.Id != ActivePlayerId);

    public CharacterState FindCharacter(Guid id) =>
        Players.SelectMany(player => player.Characters).Single(character => character.Id == id);

    public PlayerState FindOwner(CharacterState character) =>
        Players.Single(player => player.Id == character.PlayerId);
}
