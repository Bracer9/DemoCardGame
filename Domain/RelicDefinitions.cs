namespace TinyPixelFights.Domain;

public sealed record RelicDefinition(
    string Id,
    int Cost,
    string Rarity,
    IReadOnlyList<string> BuildTags);

public static class RelicCatalog
{
    public static readonly RelicDefinition ApprenticeStarInk =
        new("relic-apprentice-star-ink", 4, "common", ["magic", "burning", "debuff"]);
    public static readonly RelicDefinition MasonToken =
        new("relic-mason-token", 3, "common", ["shield", "healing", "fortify"]);
    public static readonly RelicDefinition RedWhetstone =
        new("relic-red-whetstone", 3, "common", ["physical", "absolute", "soldier"]);
    public static readonly RelicDefinition MusterPapers =
        new("relic-muster-papers", 2, "common", ["soldier", "reward"]);
    public static readonly RelicDefinition MercyCup =
        new("relic-mercy-cup", 4, "common", ["healing", "shield"]);
    public static readonly RelicDefinition WitchBell =
        new("relic-witch-bell", 3, "common", ["debuff", "control"]);

    public static readonly RelicDefinition EmberAstrolabe =
        new("relic-ember-astrolabe", 5, "rare", ["burning", "debuff"]);
    public static readonly RelicDefinition HollowCometLens =
        new("relic-hollow-comet-lens", 5, "rare", ["magic", "debuff", "morale"]);
    public static readonly RelicDefinition WhiteLilyCenser =
        new("relic-white-lily-censer", 5, "rare", ["healing", "shield", "spell-ward"]);
    public static readonly RelicDefinition DuelistTicket =
        new("relic-duelist-ticket", 5, "rare", ["physical", "absolute", "soldier"]);
    public static readonly RelicDefinition CommandSergeantSeal =
        new("relic-command-sergeant-seal", 5, "rare", ["soldier", "role-action", "ap"]);
    public static readonly RelicDefinition NightBait =
        new("relic-night-bait", 5, "rare", ["prey", "absolute", "debuff"]);
    public static readonly RelicDefinition CommandTable =
        new("relic-command-table", 6, "rare", ["role-action", "ap", "support"]);
    public static readonly RelicDefinition EchoCrystal =
        new("relic-echo-crystal", 5, "rare", ["magic", "chant"]);
    public static readonly RelicDefinition GreenStandard =
        new("relic-green-standard", 5, "rare", ["physical", "shield", "ap"]);
    public static readonly RelicDefinition BloodCoin =
        new("relic-blood-coin", 5, "rare", ["sacrifice", "shield"]);

    public static readonly RelicDefinition AstralPrism =
        new("relic-astral-prism", 8, "epic", ["magic", "chant"]);
    public static readonly RelicDefinition AshenDetonator =
        new("relic-ashen-detonator", 8, "epic", ["magic", "burning", "debuff"]);
    public static readonly RelicDefinition PlagueCodex =
        new("relic-plague-codex", 8, "epic", ["debuff", "control", "absolute"]);
    public static readonly RelicDefinition AttritionLedger =
        new("relic-attrition-ledger", 8, "epic", ["debuff", "control", "morale"]);
    public static readonly RelicDefinition PredatorCrown =
        new("relic-predator-crown", 8, "epic", ["prey", "absolute"]);
    public static readonly RelicDefinition RedHourglass =
        new("relic-red-hourglass", 8, "epic", ["physical", "role-action", "soldier"]);
    public static readonly RelicDefinition KingwallStandard =
        new("relic-kingwall-standard", 8, "epic", ["shield", "defense"]);
    public static readonly RelicDefinition SaintChalice =
        new("relic-saint-chalice", 8, "epic", ["healing", "shield", "morale"]);
    public static readonly RelicDefinition CompanyStandard =
        new("relic-company-standard", 8, "epic", ["soldier", "physical", "magic"]);
    public static readonly RelicDefinition FuneralCoin =
        new("relic-funeral-coin", 8, "epic", ["sacrifice", "absolute", "ap"]);

    public static IReadOnlyList<RelicDefinition> All { get; } =
    [
        ApprenticeStarInk,
        MasonToken,
        RedWhetstone,
        MusterPapers,
        MercyCup,
        WitchBell,
        EmberAstrolabe,
        HollowCometLens,
        WhiteLilyCenser,
        DuelistTicket,
        CommandSergeantSeal,
        NightBait,
        CommandTable,
        EchoCrystal,
        GreenStandard,
        BloodCoin,
        AstralPrism,
        AshenDetonator,
        PlagueCodex,
        AttritionLedger,
        PredatorCrown,
        RedHourglass,
        KingwallStandard,
        SaintChalice,
        CompanyStandard,
        FuneralCoin
    ];

    public static RelicDefinition? Find(string id) =>
        All.FirstOrDefault(relic => string.Equals(relic.Id, id, StringComparison.OrdinalIgnoreCase));
}
