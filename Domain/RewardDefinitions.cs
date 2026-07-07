namespace TinyPixelFights.Domain;

public sealed record RewardDefinition(
    string Id,
    int Cost,
    string Rarity,
    RewardKind Kind = RewardKind.DummyStatus);

public enum RewardKind
{
    DummyStatus,
    HeroRoleActionUpgrade,
    HeroRecruit,
    SoldierRecruit
}

public static class RewardCatalog
{
    public static IReadOnlyList<RewardDefinition> DummyRewards { get; } =
    [
        new("dummy-reward-a", 3, "common"),
        new("dummy-reward-b", 4, "rare"),
        new("dummy-reward-c", 6, "epic")
    ];

    public static RewardDefinition HeroRoleActionUpgrade { get; } =
        new("hero-role-action-upgrade", 4, "rare", RewardKind.HeroRoleActionUpgrade);

    public static RewardDefinition HeroRecruit { get; } =
        new("hero-recruit", 6, "rare", RewardKind.HeroRecruit);

    public static RewardDefinition SoldierRecruit { get; } =
        new("soldier-recruit", 4, "common", RewardKind.SoldierRecruit);

    public static IReadOnlyList<RewardDefinition> All { get; } =
    [
        HeroRoleActionUpgrade,
        HeroRecruit,
        SoldierRecruit,
        ..DummyRewards
    ];
}
