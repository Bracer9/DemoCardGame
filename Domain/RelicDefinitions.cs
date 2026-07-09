namespace TinyPixelFights.Domain;

public enum RelicStage
{
    One,
    Two,
    Three
}

public sealed record RelicDefinition(
    string Id,
    int Cost,
    string Rarity,
    RelicStage Stage,
    IReadOnlyList<string> BuildTags);

public static class RelicCatalog
{
    public static readonly RelicDefinition SilverWardCharm =
        new("relic-silver-ward-charm", 3, "common", RelicStage.One, ["healing", "defense"]);
    public static readonly RelicDefinition BlackIronRivets =
        new("relic-black-iron-rivets", 3, "common", RelicStage.One, ["shield", "defense"]);
    public static readonly RelicDefinition ApprenticeStarInk =
        new("relic-apprentice-star-ink", 4, "common", RelicStage.One, ["magic", "burning"]);
    public static readonly RelicDefinition MasonToken =
        new("relic-mason-token", 3, "common", RelicStage.One, ["shield"]);
    public static readonly RelicDefinition RedWhetstone =
        new("relic-red-whetstone", 3, "common", RelicStage.One, ["physical"]);

    public static readonly RelicDefinition EmberAstrolabe =
        new("relic-ember-astrolabe", 5, "rare", RelicStage.Two, ["magic", "burning"]);
    public static readonly RelicDefinition HollowCometLens =
        new("relic-hollow-comet-lens", 5, "rare", RelicStage.Two, ["magic", "morale"]);
    public static readonly RelicDefinition CrackedShieldBell =
        new("relic-cracked-shield-bell", 5, "rare", RelicStage.Two, ["shield", "control"]);
    public static readonly RelicDefinition DuelistTicket =
        new("relic-duelist-ticket", 5, "rare", RelicStage.Two, ["physical"]);
    public static readonly RelicDefinition GreenStandard =
        new("relic-green-standard", 5, "rare", RelicStage.Two, ["physical", "shield"]);

    public static readonly RelicDefinition AshenDetonator =
        new("relic-ashen-detonator", 8, "epic", RelicStage.Three, ["magic", "burning"]);
    public static readonly RelicDefinition SmolderingCenser =
        new("relic-smoldering-censer", 8, "epic", RelicStage.Three, ["magic", "burning"]);
    public static readonly RelicDefinition KingwallStandard =
        new("relic-kingwall-standard", 8, "epic", RelicStage.Three, ["shield"]);
    public static readonly RelicDefinition BastionHammer =
        new("relic-bastion-hammer", 8, "epic", RelicStage.Three, ["shield", "morale"]);
    public static readonly RelicDefinition VictoryDrum =
        new("relic-victory-drum", 8, "epic", RelicStage.Three, ["physical"]);
    public static readonly RelicDefinition RedHourglass =
        new("relic-red-hourglass", 8, "epic", RelicStage.Three, ["physical"]);
    public static readonly RelicDefinition WarCouncilBanner =
        new("relic-war-council-banner", 7, "epic", RelicStage.Three, ["generic"]);

    public static IReadOnlyList<RelicDefinition> All { get; } =
    [
        SilverWardCharm,
        BlackIronRivets,
        ApprenticeStarInk,
        MasonToken,
        RedWhetstone,
        EmberAstrolabe,
        HollowCometLens,
        CrackedShieldBell,
        DuelistTicket,
        GreenStandard,
        AshenDetonator,
        SmolderingCenser,
        KingwallStandard,
        BastionHammer,
        VictoryDrum,
        RedHourglass,
        WarCouncilBanner
    ];

    public static RelicDefinition? Find(string id) =>
        All.FirstOrDefault(relic => string.Equals(relic.Id, id, StringComparison.OrdinalIgnoreCase));
}
