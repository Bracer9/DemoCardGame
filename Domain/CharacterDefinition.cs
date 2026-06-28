namespace TinyPixelFights.Domain;

public sealed record CharacterDefinition(
    string Key,
    string AssetFile,
    string ColoredAssetFile,
    int Cost,
    int Attack,
    int MaxHp,
    DamageType AttackType,
    int PhysicalDefense,
    int MagicalDefense,
    string SkillId);

public static class CharacterCatalog
{
    public static IReadOnlyList<CharacterDefinition> All { get; } =
    [
        new("peasant", "Peasant.png", "New_Portraits/Peasant2.png", 1, 2, 16, DamageType.Physical, 0, 0, "spring-harvest"),
        new("princess", "Princess.png", "New_Portraits/Princess2.png", 1, 1, 12, DamageType.Physical, -1, 1, "saints-prayer"),
        new("mage", "Mage.png", "New_Portraits/Mage2.png", 2, 4, 16, DamageType.Magical, 0, 1, "searing-mark"),
        new("oracle", "Oracle.png", "New_Portraits/Oracle2.png", 1, 1, 14, DamageType.Magical, -1, 2, "stargazers-aegis"),
        new("knight", "Knight.png", "New_Portraits/Knight2.png", 3, 3, 24, DamageType.Physical, 1, -1, "interposing-shield"),
        new("druid", "Druid.png", "New_Portraits/Druid2.png", 1, 1, 16, DamageType.Magical, 0, 1, "weakening-spores"),
        new("barbarian", "Barbarian.png", "New_Portraits/Barbarian2.png", 2, 4, 18, DamageType.Physical, -1, 0, "aftershock-axe"),
        new("monster", "Monster.png", "New_Portraits/Monster2.png", 3, 3, 22, DamageType.Physical, 1, -1, "predatory-instinct")
    ];
}
