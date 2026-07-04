namespace TinyPixelFights.Domain;

public sealed record RoleActionMetadata(
    string Id,
    RoleActionActivationMode ActivationMode,
    IReadOnlyList<RoleActionTargetKind> ValidTargetKinds,
    int BaseApCost,
    IReadOnlyList<string> Tags,
    bool IsRepeatable = false,
    int CooldownTurns = 0);

public sealed class CharacterRoleAction(RoleActionMetadata metadata)
{
    public RoleActionMetadata Metadata { get; } = metadata;

    public bool IsAvailable(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive || state.Phase != GamePhase.Playing)
            return false;
        if (owner.PlayerId != state.ActivePlayerId)
            return false;
        if (!Metadata.IsRepeatable && owner.RoleActionsUsedThisTurn.Contains(Metadata.Id))
            return false;
        if (owner.RoleActionCooldowns.GetValueOrDefault(Metadata.Id) > 0)
            return false;
        if (state.RewardWindow is not null)
            return false;
        if (state.PendingRoleActionUpgrade is not null)
            return false;
        if (state.PendingHeroDraft is not null)
            return false;
        if (Metadata.BaseApCost > state.ActionPoints)
            return false;
        if (Metadata.Id == "raise-bulwark" && state.FindOwner(owner).SharedShield <= 0)
            return false;

        return true;
    }

    public LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive) return L10n.Text("reason.defeated");
        if (state.Phase == GamePhase.Finished) return L10n.Text("reason.matchFinished");
        if (owner.PlayerId != state.ActivePlayerId) return L10n.Text("reason.opponentTurn");
        if (!Metadata.IsRepeatable && owner.RoleActionsUsedThisTurn.Contains(Metadata.Id))
            return L10n.Text("reason.alreadyActed");
        var cooldown = owner.RoleActionCooldowns.GetValueOrDefault(Metadata.Id);
        if (cooldown > 0)
            return L10n.Text("reason.roleActionCooldown", ("turns", L10n.Raw(cooldown)));
        if (state.RewardWindow is not null) return L10n.Text("error.rewardWindowOpen");
        if (state.PendingRoleActionUpgrade is not null) return L10n.Text("error.pendingRoleActionUpgrade");
        if (state.PendingHeroDraft is not null) return L10n.Text("error.pendingHeroDraft");
        if (Metadata.BaseApCost > state.ActionPoints) return L10n.Text("reason.notEnoughAp");
        if (Metadata.Id == "raise-bulwark" && state.FindOwner(owner).SharedShield <= 0)
            return L10n.Text("error.roleActionRequiresShield");
        return null;
    }
}

public sealed class RoleActionRegistry
{
    private readonly IReadOnlyDictionary<string, CharacterRoleAction> _actions;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _upgradeChoices;

    public RoleActionRegistry()
    {
        CharacterRoleAction[] actions =
        [
            new(new RoleActionMetadata(
                "saintly-prayer",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                1,
                ["heal", "cleanse", "holy"])),
            new(new RoleActionMetadata(
                "royal-command",
                RoleActionActivationMode.Immediate,
                [RoleActionTargetKind.ActionPointPanel],
                1,
                ["command", "action-point"],
                CooldownTurns: 1)),
            new(new RoleActionMetadata(
                "guard-oath",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                1,
                ["guard", "defense"])),
            new(new RoleActionMetadata(
                "raise-bulwark",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.OwnShield],
                1,
                ["shield", "guard"])),
            new(new RoleActionMetadata(
                "arcane-channel",
                RoleActionActivationMode.Immediate,
                [RoleActionTargetKind.SelfCard],
                2,
                ["charge", "magic"])),
            new(new RoleActionMetadata(
                "searing-brand",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["burn", "magic", "mark"])),
            new(new RoleActionMetadata(
                "cleansing-herbs",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                1,
                ["cleanse", "heal", "nature"])),
            new(new RoleActionMetadata(
                "weakening-spores-action",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["dispel", "exhaustion", "erosion", "nature"])),
            new(new RoleActionMetadata(
                "war-cry",
                RoleActionActivationMode.Immediate,
                [RoleActionTargetKind.SelfCard],
                1,
                ["charge", "attack-modifier", "sacrifice"])),
            new(new RoleActionMetadata(
                "challenge",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["pressure", "counter-lock", "physical"])),
            new(new RoleActionMetadata(
                "star-reading",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                1,
                ["foresight", "defense", "fate"])),
            new(new RoleActionMetadata(
                "fate-mark",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["mark", "fate"])),
            new(new RoleActionMetadata(
                "predatory-gaze",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["mark", "absolute", "prey"])),
            new(new RoleActionMetadata(
                "dark-pact",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard, RoleActionTargetKind.SelfCard],
                1,
                ["sacrifice", "attack-modifier", "battle-point"])),
            new(new RoleActionMetadata(
                "supply-basket",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                0,
                ["heal", "soldier", "support"])),
            new(new RoleActionMetadata(
                "field-work",
                RoleActionActivationMode.Immediate,
                [RoleActionTargetKind.SelfCard],
                1,
                ["harvest", "charge", "support"],
                CooldownTurns: 1)),
            new(new RoleActionMetadata(
                "mend",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard],
                1,
                ["heal", "cleanse", "ward"])),
            new(new RoleActionMetadata(
                "aegis-formation",
                RoleActionActivationMode.Immediate,
                [RoleActionTargetKind.OwnShield],
                1,
                ["shield", "guard"])),
            new(new RoleActionMetadata(
                "crimson-lunge",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.EnemyCard],
                1,
                ["trembling", "vulnerable", "physical"])),
            new(new RoleActionMetadata(
                "astral-focus",
                RoleActionActivationMode.Targeted,
                [RoleActionTargetKind.AllyCard, RoleActionTargetKind.EnemyCard],
                1,
                ["chant", "void", "magic"]))
        ];

        _actions = actions.ToDictionary(action => action.Metadata.Id);
        _upgradeChoices = new Dictionary<string, IReadOnlyList<string>>
        {
            ["princess"] = ["saintly-prayer", "royal-command"],
            ["knight"] = ["guard-oath", "raise-bulwark"],
            ["mage"] = ["arcane-channel", "searing-brand"],
            ["barbarian"] = ["war-cry", "challenge"],
            ["oracle"] = ["star-reading", "fate-mark"],
            ["monster"] = ["predatory-gaze", "dark-pact"],
            ["peasant"] = ["supply-basket", "field-work"],
            ["druid"] = ["cleansing-herbs", "weakening-spores-action"]
        };
    }

    public CharacterRoleAction Get(string id) => _actions[id];

    public IReadOnlyList<CharacterRoleAction> GetMany(IEnumerable<string> ids) =>
        ids.Where(_actions.ContainsKey).Select(id => _actions[id]).ToArray();

    public IReadOnlyList<CharacterRoleAction> GetUpgradeChoices(string characterKey) =>
        _upgradeChoices.TryGetValue(characterKey, out var ids) ? GetMany(ids) : [];

    public bool IsUpgradeChoice(string characterKey, string roleActionId) =>
        _upgradeChoices.TryGetValue(characterKey, out var ids) && ids.Contains(roleActionId);
}
