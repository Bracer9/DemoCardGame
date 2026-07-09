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
        if (HasRelic(owner, "dummy-reward-c") || HasRelic(owner, "relic-war-council-banner"))
            result++;
        if ((HasRelic(owner, "dummy-reward-b") || HasRelic(owner, "relic-apprentice-star-ink"))
            && GameEngine.GetAttackType(character) == DamageType.Magical)
            result++;
        if (HasRelic(owner, "relic-red-whetstone") && GameEngine.GetAttackType(character) == DamageType.Physical)
            result++;
        return result;
    }

    public static int ModifyPhysicalDefense(PlayerState owner, int defense)
    {
        var result = defense;
        if (HasRelic(owner, "dummy-reward-physical-defense") || HasRelic(owner, "relic-black-iron-rivets"))
            result++;
        return result;
    }

    public static int ModifyMagicalDefense(PlayerState owner, int defense)
    {
        var result = defense;
        if (HasRelic(owner, "dummy-reward-a") || HasRelic(owner, "relic-silver-ward-charm"))
            result++;
        return result;
    }

    public static bool HasRelic(PlayerState owner, string relicId) =>
        owner.Relics.Any(relic => string.Equals(relic.Id, relicId, StringComparison.OrdinalIgnoreCase));

    public static bool TryUseTurnRelic(PlayerState owner, string relicId)
    {
        if (!HasRelic(owner, relicId))
            return false;
        return owner.RelicsUsedThisTurn.Add(relicId);
    }
}
