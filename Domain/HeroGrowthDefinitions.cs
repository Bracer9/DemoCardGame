namespace TinyPixelFights.Domain;

public sealed record HeroStatGrowth(
    int MaxHp = 0,
    int Attack = 0,
    int PhysicalDefense = 0,
    int MagicalDefense = 0);

public sealed record HeroGrowthPathDefinition(
    string HeroKey,
    string BaseRoleActionId,
    string PathId,
    HeroStatGrowth Rank2Stats,
    HeroStatGrowth Rank3Stats,
    string Rank3RoleActionId,
    string Rank3PortraitFile);

public static class HeroGrowthCatalog
{
    public static IReadOnlyList<HeroGrowthPathDefinition> All { get; } =
    [
        new("princess", "saintly-prayer", "saint-queen",
            new(MaxHp: 3, MagicalDefense: 1),
            new(MaxHp: 4, PhysicalDefense: 1),
            "miracle-standard", "rank3_Heroines_Portraits/Saint_Queen.png"),
        new("princess", "royal-command", "war-queen",
            new(MaxHp: 2, Attack: 1, PhysicalDefense: 1),
            new(MaxHp: 3, Attack: 1),
            "edict-of-victory", "rank3_Heroines_Portraits/War_Queen.png"),

        new("oracle", "star-reading", "astral-oracle",
            new(MaxHp: 2, MagicalDefense: 1),
            new(MaxHp: 2, Attack: 1),
            "astral-alignment", "rank3_Heroines_Portraits/Astral_Oracle.png"),
        new("oracle", "fate-mark", "fate-dealer",
            new(MaxHp: 2, Attack: 1),
            new(MaxHp: 3, MagicalDefense: 1),
            "thread-cut", "rank3_Heroines_Portraits/Fate_Dealer.png"),

        new("peasant", "supply-basket", "quartermaster",
            new(MaxHp: 4, PhysicalDefense: 1),
            new(MaxHp: 4, MagicalDefense: 1),
            "field-rations", "rank3_Heroines_Portraits/Quartermaster.png"),
        new("peasant", "field-work", "militia-foreman",
            new(MaxHp: 3, Attack: 1),
            new(MaxHp: 3, Attack: 1),
            "militia-call", "rank3_Heroines_Portraits/Militia_Foreman.png"),

        new("mage", "arcane-channel", "stellar-archmage",
            new(MaxHp: 2, Attack: 1),
            new(MaxHp: 2, Attack: 1),
            "starfall", "rank3_Heroines_Portraits/Stellar_Archmage.png"),
        new("mage", "searing-brand", "arcane-archivist",
            new(MaxHp: 2, MagicalDefense: 1),
            new(MaxHp: 2, Attack: 1),
            "archive-formula", "rank3_Heroines_Portraits/Arcane_Archivist.png"),

        new("druid", "cleansing-herbs", "grove-keeper",
            new(MaxHp: 3, MagicalDefense: 1),
            new(MaxHp: 4, PhysicalDefense: 1),
            "grove-sanctuary", "rank3_Heroines_Portraits/Grove_Keeper.png"),
        new("druid", "weakening-spores-action", "wildspeaker",
            new(MaxHp: 2, Attack: 1),
            new(MaxHp: 3, Attack: 1),
            "call-the-hunt", "rank3_Heroines_Portraits/Wildspeaker.png"),

        new("barbarian", "war-cry", "radiant-berserker",
            new(MaxHp: 2, Attack: 1),
            new(MaxHp: 3, Attack: 1),
            "glory-roar", "rank3_Heroines_Portraits/Radiant_Berserker.png"),
        new("barbarian", "challenge", "dragon-raider",
            new(MaxHp: 3, PhysicalDefense: 1),
            new(MaxHp: 3, Attack: 1),
            "dragon-breaker", "rank3_Heroines_Portraits/Dragon_Raider.png"),

        new("monster", "predatory-gaze", "nightmare-fiend",
            new(MaxHp: 3, MagicalDefense: 1),
            new(MaxHp: 3, Attack: 1),
            "nightmare-stare", "rank3_Heroines_Portraits/Nightmare_Fiend.png"),
        new("monster", "dark-pact", "abyssal-queen",
            new(MaxHp: 4, Attack: 1),
            new(MaxHp: 4, PhysicalDefense: 1),
            "abyssal-bargain", "rank3_Heroines_Portraits/Abyssal_Queen.png"),

        new("knight", "guard-oath", "holy-paladin",
            new(MaxHp: 4, MagicalDefense: 1),
            new(MaxHp: 4, PhysicalDefense: 1),
            "holy-bastion", "rank3_Heroines_Portraits/Holy_Paladin.png"),
        new("knight", "raise-bulwark", "dread-cavalier",
            new(MaxHp: 3, Attack: 1),
            new(MaxHp: 3, Attack: 1),
            "iron-charge", "rank3_Heroines_Portraits/Dread_Cavalier.png")
    ];

    private static readonly IReadOnlyDictionary<string, HeroGrowthPathDefinition> ByBaseRoleAction =
        All.ToDictionary(path => path.BaseRoleActionId, StringComparer.OrdinalIgnoreCase);

    public static HeroGrowthPathDefinition? FindByBaseRoleAction(string? roleActionId) =>
        roleActionId is not null && ByBaseRoleAction.TryGetValue(roleActionId, out var path)
            ? path
            : null;

    public static HeroGrowthPathDefinition? Find(CharacterState character) =>
        character.Definition.CardType == CardType.Hero
            ? FindByBaseRoleAction(character.HeroPathRoleActionId)
            : null;

    public static HeroStatGrowth GetTotalStats(CharacterState character)
    {
        var path = Find(character);
        if (path is null)
            return new HeroStatGrowth();

        var rank = Math.Clamp(character.HeroRank, 0, 3);
        var rank2 = rank >= 2 ? path.Rank2Stats : new HeroStatGrowth();
        var rank3 = rank >= 3 ? path.Rank3Stats : new HeroStatGrowth();
        return new HeroStatGrowth(
            rank2.MaxHp + rank3.MaxHp,
            rank2.Attack + rank3.Attack,
            rank2.PhysicalDefense + rank3.PhysicalDefense,
            rank2.MagicalDefense + rank3.MagicalDefense);
    }
}
