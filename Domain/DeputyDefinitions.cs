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
    int StatValue,
    IReadOnlyList<string> BuildTags);

public static class DeputyCatalog
{
    private static readonly IReadOnlyList<DeputyDefinition> Definitions =
    [
        new("deputy-cleric", "cleric", DeputyStatKind.MagicalDefense, 2,
            ["healing", "support", "defense", "spell-ward", "soldier"]),
        new("deputy-shieldmaiden", "shieldmaiden", DeputyStatKind.PhysicalDefense, 2,
            ["shield", "defense", "fortify", "support", "soldier"]),
        new("deputy-duelist", "duelist", DeputyStatKind.PhysicalAttack, 2,
            ["physical", "absolute", "debuff", "soldier"]),
        new("deputy-arcanist", "arcanist", DeputyStatKind.MagicalAttack, 2,
            ["magic", "burning", "debuff", "role-action", "chant", "soldier"]),
        new("deputy-jester", "jester", DeputyStatKind.Attack, 1,
            ["debuff", "control", "role-action", "support", "soldier"])
    ];

    public static IReadOnlyList<DeputyDefinition> All => Definitions;

    public static DeputyDefinition? FindById(string? id) =>
        id is null ? null : Definitions.FirstOrDefault(definition => definition.Id == id);

    public static DeputyDefinition? FindBySoldierKey(string soldierKey) =>
        Definitions.FirstOrDefault(definition => definition.SoldierKey == soldierKey);
}
