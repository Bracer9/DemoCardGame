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
    Skill,
    Status
}

public enum SkillKind
{
    Active,
    Passive
}

public enum GamePhase
{
    Playing,
    Finished
}

public enum CharacterZone
{
    Battlefield,
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
    public static LocalizedArg Skill(string id) => new("skill", id);
    public static LocalizedArg Status(string id) => new("status", id);
    public static LocalizedArg Ui(string key) => new("ui", key);
}
