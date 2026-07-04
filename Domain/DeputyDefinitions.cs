namespace TinyPixelFights.Domain;

public enum DeputyStatKind
{
    Attack,
    MaxHp,
    PhysicalDefense,
    MagicalDefense
}

public sealed record DeputyDefinition(
    string Id,
    string SoldierKey,
    DeputyStatKind StatKind,
    int StatValue);

public static class DeputyCatalog
{
    private static readonly IReadOnlyList<DeputyDefinition> Definitions =
    [
        new("deputy-cleric", "cleric", DeputyStatKind.MaxHp, 4),
        new("deputy-shieldmaiden", "shieldmaiden", DeputyStatKind.PhysicalDefense, 2),
        new("deputy-duelist", "duelist", DeputyStatKind.Attack, 2),
        new("deputy-arcanist", "arcanist", DeputyStatKind.MagicalDefense, 2)
    ];

    public static IReadOnlyList<DeputyDefinition> All => Definitions;

    public static DeputyDefinition? FindById(string? id) =>
        id is null ? null : Definitions.FirstOrDefault(definition => definition.Id == id);

    public static DeputyDefinition? FindBySoldierKey(string soldierKey) =>
        Definitions.FirstOrDefault(definition => definition.SoldierKey == soldierKey);
}
