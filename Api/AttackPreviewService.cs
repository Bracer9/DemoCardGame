using TinyPixelFights.Domain;

namespace TinyPixelFights.Api;

public sealed class AttackPreviewService
{
    private readonly GameEngine _engine;
    public AttackPreviewService(GameEngine engine) => _engine = engine;

    public AttackPreview Create(GameState state, Guid attackerId, Guid defenderId)
    {
        CharacterState attacker;
        CharacterState defender;
        try { attacker = state.FindCharacter(attackerId); defender = state.FindCharacter(defenderId); }
        catch { return Invalid(attackerId, defenderId, L10n.Text("error.characterNotFound")); }

        var error = Validate(state, attacker, defender);
        if (error is not null) return Invalid(attackerId, defenderId, error);

        var attackBase = _engine.GetActiveAttack(attacker);
        if (attacker.Definition.AttackType == DamageType.Magical
            && attacker.Statuses.Any(status => status.Id == "magic-power" && !status.Expired))
            attackBase++;

        var attack = ForecastDamage(state, defender, attackBase, attacker.Definition.AttackType, DamageSource.ActiveAttack);
        var counter = ForecastDamage(state, attacker, _engine.GetCounterAttack(defender),
            defender.Definition.AttackType, DamageSource.CounterAttack);
        var skill = _engine.GetSkill(attacker);
        var (possible, skillText) = ForecastSkill(state, attacker, defender, attack);

        var notes = new List<LocalizedText>
        {
            L10n.Text("preview.simultaneous"), L10n.Text("preview.counterRule")
        };
        if (attack.ReductionChancePercent > 0)
            notes.Add(L10n.Text("preview.targetForesight", ("chance", L10n.Raw(attack.ReductionChancePercent))));
        if (counter.ReductionChancePercent > 0)
            notes.Add(L10n.Text("preview.attackerForesight", ("chance", L10n.Raw(counter.ReductionChancePercent))));
        if (attack.DefenseReduction > 0)
            notes.Add(L10n.Text("preview.targetDefense",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(attack.DamageType))),
                ("value", L10n.Raw(attack.DefenseReduction))));
        if (attack.DefenseReduction < 0)
            notes.Add(L10n.Text("preview.targetDefenseWeakness",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(attack.DamageType))),
                ("value", L10n.Raw(Math.Abs(attack.DefenseReduction)))));
        if (counter.DefenseReduction > 0)
            notes.Add(L10n.Text("preview.attackerDefense",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(counter.DamageType))),
                ("value", L10n.Raw(counter.DefenseReduction))));
        if (counter.DefenseReduction < 0)
            notes.Add(L10n.Text("preview.attackerDefenseWeakness",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(counter.DamageType))),
                ("value", L10n.Raw(Math.Abs(counter.DefenseReduction)))));
        var reduction = GameEngine.GetCounterDebuffReduction(defender);
        if (reduction > 0)
            notes.Add(L10n.Text("preview.complacency", ("value", L10n.Raw(reduction))));
        if (attack.ShieldWillAbsorb)
            notes.Add(L10n.Text("preview.targetShield", ("value", L10n.Raw(attack.ShieldAbsorb))));
        if (counter.ShieldWillAbsorb)
            notes.Add(L10n.Text("preview.attackerShield", ("value", L10n.Raw(counter.ShieldAbsorb))));
        if (attack.GuardWillTrigger) notes.Add(L10n.Text("preview.guard"));

        return new AttackPreview(true, null, attacker.Id, defender.Id, attacker.Definition.Cost,
            attack, counter, skill.Metadata.Id, possible, skillText, notes);
    }

    private DamageForecast ForecastDamage(GameState state, CharacterState target, int baseDamage,
        DamageType type, DamageSource source)
    {
        var owner = state.FindOwner(target);
        var effectiveDefense = _engine.GetDefense(target, type);
        var defense = effectiveDefense > 0
            ? Math.Min(Math.Max(0, baseDamage), effectiveDefense)
            : effectiveDefense < 0 && baseDamage > 0
                ? effectiveDefense
                : 0;
        var damageAfterDefense = defense >= 0
            ? Math.Max(0, baseDamage - defense)
            : baseDamage + Math.Abs(defense);
        var hasOracle = owner.Characters.Any(character => character.IsAlive && character.Definition.Key == "oracle");
        var chance = hasOracle ? (type == DamageType.Physical ? 25 : 50) : 0;
        var min = Math.Max(0, damageAfterDefense - (chance > 0 ? 1 : 0));
        var max = Math.Max(0, damageAfterDefense);
        var shield = owner.SharedShield > 0 && max > 0;
        var shieldAbsorb = shield ? Math.Min(owner.SharedShield, max) : 0;
        if (shield) { min = Math.Max(0, min - owner.SharedShield); max = Math.Max(0, max - owner.SharedShield); }
        var knight = owner.Characters.FirstOrDefault(character => character.IsAlive
            && character.Definition.Key == "knight" && !character.GuardConsumed);
        var guard = source == DamageSource.ActiveAttack && type == DamageType.Physical
            && target.Definition.Key != "knight" && knight is not null && max > 0;
        if (guard) { min = Math.Max(0, min - 1); max = Math.Max(0, max - 1); }
        return new DamageForecast(min, max, type.ToString(), defense, chance, shield, shieldAbsorb, guard, guard ? 1 : 0);
    }

    private static (bool, LocalizedText) ForecastSkill(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack) => attacker.Definition.Key switch
    {
        "peasant" => attacker.Statuses.Any(status => status.Id == "harvest" && !status.Expired)
            ? (false, L10n.Text("preview.skill.harvestActive"))
            : state.ActionsTakenThisTurn == 0
                ? (true, L10n.Text("preview.skill.sowing"))
                : (false, L10n.Text("preview.skill.notFirstAttack")),
        "mage" => (true, L10n.Text(attacker.Statuses.Any(status =>
            status.Id == "magic-power" && !status.Expired)
            ? "preview.skill.burningBoosted" : "preview.skill.burning")),
        "druid" => ForecastDruidSkill(defender, attack),
        "barbarian" => (attack.Max >= AftershockAxeSkill.TriggerDamage,
            L10n.Text(attack.Min >= AftershockAxeSkill.TriggerDamage
                ? "preview.skill.aftershockGuaranteed" : "preview.skill.aftershockPossible")),
        "monster" => ForecastMonsterSkill(state, attacker, defender, attack),
        _ => (false, L10n.Text("preview.skill.none"))
    };

    private static (bool, LocalizedText) ForecastDruidSkill(CharacterState defender, DamageForecast attack)
    {
        var guaranteed = attack.Min > 0;
        var noDamage = attack.Max == 0;
        var key = (guaranteed, noDamage) switch
        {
            (true, _) => "preview.skill.weaknessGuaranteed",
            (false, true) => "preview.skill.weaknessChance",
            _ => "preview.skill.weaknessVariable"
        };
        return (true, L10n.Text(key));
    }

    private static (bool, LocalizedText) ForecastMonsterSkill(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack)
    {
        if (defender.Definition.Key == "princess")
            return (false, L10n.Text("preview.skill.beautyPrincessImmune"));

        var hasPrincess = state.FindOwner(attacker).Characters.Any(character =>
            character.IsAlive && character.Definition.Key == "princess");
        var damage = PredatoryInstinctSkill.AbsoluteDamage
            + (hasPrincess ? PredatoryInstinctSkill.PrincessBonusDamage : 0);
        var key = attack.Max == 0 ? "preview.skill.beautyGuaranteed"
            : attack.Min == 0 ? "preview.skill.beautyPossible"
            : "preview.skill.beautyUnavailable";
        return (attack.Min == 0, L10n.Text(key, ("amount", L10n.Raw(damage))));
    }

    private static LocalizedText? Validate(GameState state, CharacterState attacker, CharacterState defender)
    {
        if (state.Phase != GamePhase.Playing) return L10n.Text("error.matchFinished");
        if (attacker.PlayerId != state.ActivePlayerId) return L10n.Text("error.notActiveCharacter");
        if (defender.PlayerId == state.ActivePlayerId) return L10n.Text("error.cannotAttackAlly");
        if (!attacker.IsAlive || !defender.IsAlive) return L10n.Text("error.defeatedSelection");
        if (attacker.HasActed) return L10n.Text("error.alreadyActed");
        if (attacker.Definition.Cost > state.ActionPoints) return L10n.Text("error.notEnoughAp");
        return null;
    }

    private static AttackPreview Invalid(Guid attackerId, Guid defenderId, LocalizedText error) => new(
        false, error, attackerId, defenderId, 0,
        new DamageForecast(0, 0, DamageType.Physical.ToString(), 0, 0, false, 0, false, 0),
        new DamageForecast(0, 0, DamageType.Physical.ToString(), 0, 0, false, 0, false, 0),
        string.Empty, false, L10n.Text("preview.skill.none"), []);
}
