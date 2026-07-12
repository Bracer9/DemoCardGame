namespace TinyPixelFights.Domain;

public enum DeputyStatKind
{
    Attack,
    PhysicalAttack,
    MagicalAttack,
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
        new("deputy-cleric", "cleric", DeputyStatKind.MagicalDefense, 2),
        new("deputy-shieldmaiden", "shieldmaiden", DeputyStatKind.PhysicalDefense, 2),
        new("deputy-duelist", "duelist", DeputyStatKind.PhysicalAttack, 2),
        new("deputy-arcanist", "arcanist", DeputyStatKind.MagicalAttack, 2),
        new("deputy-jester", "jester", DeputyStatKind.Attack, 1)
    ];

    public static IReadOnlyList<DeputyDefinition> All => Definitions;

    public static DeputyDefinition? FindById(string? id) =>
        id is null ? null : Definitions.FirstOrDefault(definition => definition.Id == id);

    public static DeputyDefinition? FindBySoldierKey(string soldierKey) =>
        Definitions.FirstOrDefault(definition => definition.SoldierKey == soldierKey);
}
