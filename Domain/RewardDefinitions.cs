namespace TinyPixelFights.Domain;

public sealed record RewardDefinition(
    string Id,
    int Cost,
    string Rarity,
    RewardKind Kind = RewardKind.DummyStatus);

public enum RewardKind
{
    DummyStatus,
    HeroRoleActionUpgrade
}

public static class RewardCatalog
{
    public static IReadOnlyList<RewardDefinition> DummyRewards { get; } =
    [
        new("dummy-reward-a", 2, "common"),
        new("dummy-reward-b", 4, "rare"),
        new("dummy-reward-c", 6, "epic")
    ];

    public static RewardDefinition HeroRoleActionUpgrade { get; } =
        new("hero-role-action-upgrade", 4, "rare", RewardKind.HeroRoleActionUpgrade);

    public static IReadOnlyList<RewardDefinition> All { get; } =
    [
        HeroRoleActionUpgrade,
        ..DummyRewards
    ];
}
