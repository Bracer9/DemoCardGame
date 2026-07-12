namespace TinyPixelFights.Domain;

public sealed record CharacterDefinition(
    string Key,
    CardType CardType,
    string AssetFile,
    string ColoredAssetFile,
    int Cost,
    int Attack,
    int MaxHp,
    DamageType AttackType,
    int PhysicalDefense,
    int MagicalDefense,
    string TraitId,
    IReadOnlyList<string>? RoleActionIds = null,
    string? Rank2ColoredAssetFile = null);

public static class CharacterCatalog
{
    public static IReadOnlyList<CharacterDefinition> All { get; } =
    [
        new("peasant", CardType.Hero, "Peasant.png", "New_Portraits/Peasant2.png", 1, 2, 16, DamageType.Physical, 0, 0, "spring-harvest"),
        new("princess", CardType.Hero, "Princess.png", "New_Portraits/Princess2.png", 1, 1, 12, DamageType.Magical, -1, 1, "saints-prayer"),
        new("mage", CardType.Hero, "Mage.png", "New_Portraits/Mage2.png", 2, 3, 16, DamageType.Magical, -1, 1, "searing-mark"),
        new("oracle", CardType.Hero, "Oracle.png", "New_Portraits/Oracle2.png", 1, 1, 14, DamageType.Magical, -1, 2, "stargazers-aegis"),
        new("knight", CardType.Hero, "Knight.png", "New_Portraits/Knight2.png", 3, 3, 24, DamageType.Physical, 1, 0, "interposing-shield"),
        new("druid", CardType.Hero, "Druid.png", "New_Portraits/Druid2.png", 1, 1, 16, DamageType.Magical, 0, 1, "weakening-spores"),
        new("barbarian", CardType.Hero, "Barbarian.png", "New_Portraits/Barbarian2.png", 2, 3, 18, DamageType.Physical, -1, 0, "aftershock-axe"),
        new("monster", CardType.Hero, "Monster.png", "New_Portraits/Monster2.png", 3, 4, 22, DamageType.Physical, 1, -1, "predatory-instinct"),

        new("cleric", CardType.Soldier, "New_Portraits/Cleric.png", "New_Portraits/Cleric.png", 1, 1, 14, DamageType.Magical, 0, 1, "field-medic",
            Rank2ColoredAssetFile: "New_Portraits/SaintCleric.png"),
        new("shieldmaiden", CardType.Soldier, "New_Portraits/Shieldmaiden.png", "New_Portraits/Shieldmaiden.png", 2, 1, 20, DamageType.Physical, 1, 0, "shield-drill",
            Rank2ColoredAssetFile: "New_Portraits/AegisShieldmaiden.png"),
        new("duelist", CardType.Soldier, "New_Portraits/Duelist.png", "New_Portraits/Duelist.png", 1, 2, 12, DamageType.Physical, 0, -1, "duel-sense",
            Rank2ColoredAssetFile: "New_Portraits/CrimsonDuelist.png"),
        new("arcanist", CardType.Soldier, "New_Portraits/Arcanist.png", "New_Portraits/Arcanist.png", 2, 2, 12, DamageType.Magical, -1, 1, "arcane-resonance",
            Rank2ColoredAssetFile: "New_Portraits/AstralArcanist.png"),
        new("jester", CardType.Soldier, "New_Portraits/Jester.png", "New_Portraits/Jester.png", 1, 2, 13, DamageType.Magical, -1, 0, "malicious-jest",
            Rank2ColoredAssetFile: "New_Portraits/MasqueJester.png")
    ];

    public static IReadOnlyList<CharacterDefinition> Heroes { get; } =
        All.Where(definition => definition.CardType == CardType.Hero).ToArray();

    public static IReadOnlyList<CharacterDefinition> Soldiers { get; } =
        All.Where(definition => definition.CardType == CardType.Soldier).ToArray();
}
