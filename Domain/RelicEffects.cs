namespace TinyPixelFights.Domain;

public static class RelicEffects
{
    public static void AddRelic(PlayerState player, string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return;
        if (player.Relics.Any(relic => string.Equals(relic.Id, relicId, StringComparison.OrdinalIgnoreCase)))
            return;

        player.Relics.Add(new PlayerRelicState(relicId));
    }

    public static int ModifyBaseAttack(PlayerState owner, CharacterState character, int attack)
    {
        var result = attack;
        if (HasRelic(owner, "dummy-reward-c"))
            result++;
        if (HasRelic(owner, "dummy-reward-b") && GameEngine.GetAttackType(character) == DamageType.Magical)
            result++;
        return result;
    }

    public static int ModifyPhysicalDefense(PlayerState owner, int defense)
    {
        var result = defense;
        if (HasRelic(owner, "dummy-reward-physical-defense"))
            result++;
        return result;
    }

    public static int ModifyMagicalDefense(PlayerState owner, int defense)
    {
        var result = defense;
        if (HasRelic(owner, "dummy-reward-a"))
            result++;
        return result;
    }

    private static bool HasRelic(PlayerState owner, string relicId) =>
        owner.Relics.Any(relic => string.Equals(relic.Id, relicId, StringComparison.OrdinalIgnoreCase));
}
