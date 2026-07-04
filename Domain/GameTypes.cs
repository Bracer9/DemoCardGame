namespace TinyPixelFights.Domain;

public enum DamageType
{
    Physical,
    Magical,
    Absolute
}

public enum DamageSource
{
    ActiveAttack,
    CounterAttack,
    Trait,
    Status
}

public enum CardType
{
    Hero,
    Soldier
}

public enum AbilityKind
{
    RoleAction,
    Trait
}

public enum TraitTriggerKind
{
    Continuous,
    TurnStart,
    TurnEnd,
    OnAttack,
    OnAttackDeclared,
    OnAttackResolved,
    OnDamaged,
    OnShieldBreak,
    OnCharacterDefeated,
    OnRewardWindow,
    ManualCheck
}

public enum TraitScopeKind
{
    Self,
    Ally,
    Enemy,
    Team,
    EnemyTeam,
    Global
}

public enum TraitEffectKind
{
    Damage,
    Heal,
    Shield,
    Status,
    Dispel,
    DamageModifier,
    DefenseModifier,
    CostModifier,
    ActionPointModifier,
    BattlePointModifier,
    RewardModifier,
    TargetingRule
}

public enum RoleActionActivationMode
{
    Immediate,
    Targeted
}

public enum RoleActionTargetKind
{
    None,
    SelfCard,
    AllyCard,
    EnemyCard,
    OwnShield,
    EnemyShield,
    ActionPointPanel,
    BattlePointMedal,
    EmptySlot
}

public enum GamePhase
{
    HeroDraft,
    Playing,
    Finished
}

public enum CharacterZone
{
    Battlefield,
    DraftCandidate,
    Defeated
}

public sealed record LocalizedArg(string Kind, object? Value);

public sealed record LocalizedText(
    string Key,
    IReadOnlyDictionary<string, LocalizedArg>? Args = null);

public static class L10n
{
    public static LocalizedText Text(string key, params (string Name, LocalizedArg Value)[] args) =>
        new(key, args.Length == 0 ? null : args.ToDictionary(item => item.Name, item => item.Value));

    public static LocalizedArg Raw(object? value) => new("raw", value);
    public static LocalizedArg Character(string key) => new("character", key);
    public static LocalizedArg Player(string value) => new("player", value);
    public static LocalizedArg Damage(DamageType value) => new("damageType", value.ToString());
    public static LocalizedArg Trait(string id) => new("trait", id);
    public static LocalizedArg RoleAction(string id) => new("roleAction", id);
    public static LocalizedArg Status(string id) => new("status", id);
    public static LocalizedArg Reward(string id) => new("reward", id);
    public static LocalizedArg BpReason(string id) => new("bpReason", id);
    public static LocalizedArg Ui(string key) => new("ui", key);
}
