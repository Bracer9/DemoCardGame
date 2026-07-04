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
        new("dummy-reward-a", 4, "common"),
        new("dummy-reward-b", 6, "rare"),
        new("dummy-reward-c", 8, "epic")
    ];

    public static RewardDefinition HeroRoleActionUpgrade { get; } =
        new("hero-role-action-upgrade", 8, "rare", RewardKind.HeroRoleActionUpgrade);

    public static RewardDefinition HeroRecruit { get; } =
        new("hero-recruit", 10, "rare", RewardKind.HeroRecruit);

    public static RewardDefinition SoldierRecruit { get; } =
        new("soldier-recruit", 6, "common", RewardKind.SoldierRecruit);

    public static IReadOnlyList<RewardDefinition> All { get; } =
    [
        HeroRoleActionUpgrade,
        HeroRecruit,
        SoldierRecruit,
        ..DummyRewards
    ];
}
