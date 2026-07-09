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

        var monsterPrincessAttack = attacker.Definition.Key == "monster" && defender.Definition.Key == "princess";
        var attackerAttackType = GameEngine.GetAttackType(attacker);
        var defenderAttackType = GameEngine.GetAttackType(defender);
        var attackBase = monsterPrincessAttack
            ? _engine.GetActiveAttack(state, attacker)
            : ForecastOutgoingDamage(attacker, _engine.GetActiveAttack(state, attacker),
                attackerAttackType,
                DamageSource.ActiveAttack,
                receivesMagicPowerBonus: true);
        var attack = monsterPrincessAttack
            ? new DamageForecast(attackBase, attackBase, DamageType.Absolute.ToString(), 0, 0, 0, false, 0, false, 0)
            : ForecastActiveAttackDamage(state, attacker, defender, attackBase, monsterPrincessAttack);
        attack = ForecastDamageLanding(defender, attack);
        var attackHp = attack;
        attack = ApplyPreyForecast(defender, attack);
        var pactBonus = attacker.Statuses.Any(status => status.Id == "pact" && !status.Expired)
            ? PactStatus.AbsoluteDamage
            : 0;
        if (pactBonus > 0)
            attack = AddDirectHpDamage(attack, pactBonus);
        var rageShieldBreakBonus = ForecastRageShieldBreakBonus(state, attacker, defender, attack);
        if (rageShieldBreakBonus > 0)
            attack = AddDirectHpDamage(attack, rageShieldBreakBonus);
        var duelSenseBonus = ForecastDuelSenseAbsoluteBonus(state, attacker);
        var duelSenseBefore = attack;
        if (duelSenseBonus > 0)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack, duelSenseBonus);
        var duelSenseWillTrigger = duelSenseBonus > 0
            && (attack.HpDamageMin != duelSenseBefore.HpDamageMin || attack.HpDamageMax != duelSenseBefore.HpDamageMax);
        var counterBase = ForecastOutgoingDamage(defender, _engine.GetCounterAttack(state, defender),
            defenderAttackType, DamageSource.CounterAttack, receivesMagicPowerBonus: false);
        var counter = ForecastDamage(state, attacker, counterBase, defenderAttackType, DamageSource.CounterAttack);
        counter = ForecastDamageLanding(attacker, counter);
        var counterHp = counter;
        counter = ApplyPreyForecast(attacker, counter);
        var trait = _engine.GetTrait(attacker);
        var (possible, traitText) = ForecastTrait(state, attacker, defender, attackHp);

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
        if (attack.ShieldDefenseReduction > 0)
            notes.Add(L10n.Text("preview.targetShieldDefense",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(attack.DamageType))),
                ("value", L10n.Raw(attack.ShieldDefenseReduction))));
        if (attack.ShieldDefenseReduction < 0)
            notes.Add(L10n.Text("preview.targetShieldDefenseWeakness",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(attack.DamageType))),
                ("value", L10n.Raw(Math.Abs(attack.ShieldDefenseReduction)))));
        if (counter.ShieldDefenseReduction > 0)
            notes.Add(L10n.Text("preview.attackerShieldDefense",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(counter.DamageType))),
                ("value", L10n.Raw(counter.ShieldDefenseReduction))));
        if (counter.ShieldDefenseReduction < 0)
            notes.Add(L10n.Text("preview.attackerShieldDefenseWeakness",
                ("damageType", L10n.Damage(Enum.Parse<DamageType>(counter.DamageType))),
                ("value", L10n.Raw(Math.Abs(counter.ShieldDefenseReduction)))));
        if (attack.ShieldWillAbsorb)
            notes.Add(L10n.Text("preview.targetShield", ("value", L10n.Raw(attack.ShieldAbsorb))));
        if (counter.ShieldWillAbsorb)
            notes.Add(L10n.Text("preview.attackerShield", ("value", L10n.Raw(counter.ShieldAbsorb))));
        if (attack.GuardWillTrigger) notes.Add(L10n.Text("preview.guard"));
        if (GameEngine.IsCounterAttackBlocked(defender))
            notes.Add(L10n.Text("preview.counterBlockedByChallenge"));
        if (monsterPrincessAttack)
            notes.Add(L10n.Text("preview.trait.beautyPrincessBacklash",
                ("min", L10n.Raw(attack.Min * PredatoryInstinctTrait.PrincessBacklashMultiplier)),
                ("max", L10n.Raw(attack.Max * PredatoryInstinctTrait.PrincessBacklashMultiplier))));
        if (attackerAttackType == DamageType.Magical
            && attacker.Statuses.Any(status => status.Id == "chant" && !status.Expired))
            notes.Add(L10n.Text("preview.roleAction.arcaneChannel",
                ("value", L10n.Raw(2))));
        if (defender.Statuses.Any(status => status.Id == "void" && !status.Expired)
            && attackerAttackType == DamageType.Magical)
            notes.Add(L10n.Text("preview.roleAction.searingBrand",
                ("value", L10n.Raw("×1.25"))));
        if (attacker.Statuses.Any(status => status.Id == "void" && !status.Expired)
            && defenderAttackType == DamageType.Magical)
            notes.Add(L10n.Text("preview.roleAction.searingBrandCounter",
                ("value", L10n.Raw("×1.25"))));
        if (rageShieldBreakBonus > 0)
            notes.Add(L10n.Text("preview.roleAction.warCryShieldBreak",
                ("value", L10n.Raw(rageShieldBreakBonus))));
        if (duelSenseWillTrigger)
            notes.Add(L10n.Text("preview.trait.duelSenseBonus",
                ("value", L10n.Raw(duelSenseBonus))));
        if (attacker.Statuses.Any(status => status.Id == "marked" && !status.Expired))
            notes.Add(L10n.Text("preview.roleAction.fateMark"));
        if (pactBonus > 0)
            notes.Add(L10n.Text("preview.roleAction.darkPact",
                ("value", L10n.Raw(pactBonus))));
        if (defender.Statuses.Any(status => status.Id == "prey" && !status.Expired) && attackHp.HpDamageMin == 0)
            notes.Add(L10n.Text("preview.roleAction.predatoryGaze",
                ("value", L10n.Raw(PreyStatus.AbsoluteDamage))));
        if (attacker.Statuses.Any(status => status.Id == "prey" && !status.Expired) && counterHp.HpDamageMin == 0)
            notes.Add(L10n.Text("preview.roleAction.predatoryGazeCounter",
                ("value", L10n.Raw(PreyStatus.AbsoluteDamage))));

        return new AttackPreview(true, null, attacker.Id, defender.Id, attacker.Definition.Cost,
            attack, counter, trait.Metadata.Id, possible, traitText, notes);
    }

    private static DamageForecast ApplyPreyForecast(CharacterState target, DamageForecast forecast)
    {
        if (target.Statuses.All(status => status.Id != "prey" || status.Expired) || forecast.HpDamageMin > 0)
            return forecast;

        if (forecast.HpDamageMax == 0)
            return AddDirectHpDamage(forecast, PreyStatus.AbsoluteDamage);

        var triggeredTotal = forecast.Min + PreyStatus.AbsoluteDamage;
        var triggeredHp = forecast.HpDamageMin + PreyStatus.AbsoluteDamage;
        return forecast with
        {
            Min = Math.Min(triggeredTotal, forecast.Max),
            Max = Math.Max(triggeredTotal, forecast.Max),
            HpDamageMin = Math.Min(triggeredHp, forecast.HpDamageMax),
            HpDamageMax = Math.Max(triggeredHp, forecast.HpDamageMax)
        };
    }

    private static DamageForecast AddDirectHpDamage(DamageForecast forecast, int amount)
    {
        var damage = Math.Max(0, amount);
        return damage <= 0
            ? forecast
            : forecast with
            {
                Min = forecast.Min + damage,
                Max = forecast.Max + damage,
                HpDamageMin = forecast.HpDamageMin + damage,
                HpDamageMax = forecast.HpDamageMax + damage
            };
    }

    private static DamageForecast AddDirectHpDamageIfTargetSurvives(CharacterState target, DamageForecast forecast, int amount)
    {
        var damage = Math.Max(0, amount);
        var targetHp = Math.Max(0, target.CurrentHp);
        if (damage <= 0 || targetHp <= 0 || forecast.HpDamageMin >= targetHp)
            return forecast;

        var survivingHpMax = Math.Min(forecast.HpDamageMax, targetHp - 1);
        var hpDamageMax = Math.Max(forecast.HpDamageMax, survivingHpMax + damage);
        var totalMaxWithBonus = forecast.HpDamageMax < targetHp
            ? forecast.Max + damage
            : forecast.Min + Math.Min(Math.Max(0, forecast.Max - forecast.Min), Math.Max(0, survivingHpMax - forecast.HpDamageMin)) + damage;

        return forecast with
        {
            Min = forecast.Min + damage,
            Max = Math.Max(forecast.Max, totalMaxWithBonus),
            HpDamageMin = forecast.HpDamageMin + damage,
            HpDamageMax = hpDamageMax
        };
    }

    private static DamageForecast ForecastDamageLanding(CharacterState target, DamageForecast forecast)
    {
        if (Enum.TryParse<DamageType>(forecast.DamageType, out var damageType) && damageType == DamageType.Absolute)
            return forecast with
            {
                MoraleDamageMin = 0,
                MoraleDamageMax = 0,
                HpDamageMin = forecast.Min,
                HpDamageMax = forecast.Max
            };

        var morale = Math.Max(0, target.Morale);
        return forecast with
        {
            MoraleDamageMin = Math.Min(morale, forecast.Min),
            MoraleDamageMax = Math.Min(morale, forecast.Max),
            HpDamageMin = Math.Max(0, forecast.Min - morale),
            HpDamageMax = Math.Max(0, forecast.Max - morale)
        };
    }

    private DamageForecast ForecastActiveAttackDamage(
        GameState state,
        CharacterState attacker,
        CharacterState defender,
        int attackBase,
        bool ignoreShield)
    {
        var marked = attacker.Statuses.Any(status => status.Id == "marked" && !status.Expired);
        var attackerAttackType = GameEngine.GetAttackType(attacker);
        if (!marked)
            return ForecastDamage(state, defender, attackBase, attackerAttackType, DamageSource.ActiveAttack,
                ignoreShield: ignoreShield,
                ignoreDefense: false);

        var reduced = ForecastDamage(state, defender, attackBase / 2, attackerAttackType, DamageSource.ActiveAttack,
            ignoreShield: ignoreShield,
            ignoreDefense: false);
        var amplified = ForecastDamage(state, defender, attackBase + FateMarkedStatus.DamageBonus, attackerAttackType, DamageSource.ActiveAttack,
            ignoreShield: ignoreShield,
            ignoreDefense: false);
        return CombineForecasts(reduced, amplified);
    }

    private static DamageForecast CombineForecasts(DamageForecast first, DamageForecast second) => first with
    {
        Min = Math.Min(first.Min, second.Min),
        Max = Math.Max(first.Max, second.Max),
        ShieldDefenseReduction = Math.Abs(second.ShieldDefenseReduction) > Math.Abs(first.ShieldDefenseReduction)
            ? second.ShieldDefenseReduction
            : first.ShieldDefenseReduction,
        DefenseReduction = Math.Abs(second.DefenseReduction) > Math.Abs(first.DefenseReduction)
            ? second.DefenseReduction
            : first.DefenseReduction,
        ReductionChancePercent = Math.Max(first.ReductionChancePercent, second.ReductionChancePercent),
        ShieldWillAbsorb = first.ShieldWillAbsorb || second.ShieldWillAbsorb,
        ShieldAbsorb = Math.Max(first.ShieldAbsorb, second.ShieldAbsorb),
        GuardWillTrigger = first.GuardWillTrigger || second.GuardWillTrigger,
        GuardDamage = Math.Max(first.GuardDamage, second.GuardDamage)
    };

    private DamageForecast ForecastDamage(GameState state, CharacterState target, int baseDamage,
        DamageType type, DamageSource source, bool ignoreShield = false, bool ignoreDefense = false)
    {
        var owner = state.FindOwner(target);
        var shieldDefense = 0;
        var damageAfterShieldDefense = baseDamage;
        if (!ignoreShield && owner.SharedShield > 0 && damageAfterShieldDefense > 0)
        {
            var effectiveShieldDefense = GameEngine.GetSharedShieldDefense(owner, type);
            shieldDefense = effectiveShieldDefense > 0
                ? Math.Min(damageAfterShieldDefense, effectiveShieldDefense)
                : effectiveShieldDefense < 0
                    ? effectiveShieldDefense
                    : 0;
            damageAfterShieldDefense = shieldDefense >= 0
                ? Math.Max(0, damageAfterShieldDefense - shieldDefense)
                : damageAfterShieldDefense + Math.Abs(shieldDefense);
        }

        var shield = !ignoreShield && owner.SharedShield > 0 && damageAfterShieldDefense > 0;
        var shieldAbsorb = shield ? Math.Min(owner.SharedShield, damageAfterShieldDefense) : 0;
        var damageAfterShield = shield
            ? 0
            : damageAfterShieldDefense;

        var effectiveDefense = ignoreDefense ? 0 : _engine.GetDefense(state, target, type);
        var defense = effectiveDefense > 0
            ? Math.Min(damageAfterShield, effectiveDefense)
            : effectiveDefense < 0 && damageAfterShield > 0
                ? effectiveDefense
                : 0;
        var damageAfterDefense = defense >= 0
            ? Math.Max(0, damageAfterShield - defense)
            : damageAfterShield + Math.Abs(defense);
        damageAfterDefense = ForecastIncomingStatusDamage(target, damageAfterDefense, type, source);
        var max = Math.Max(0, damageAfterDefense);
        var oracleReduced = Math.Max(0, (int)Math.Ceiling(damageAfterDefense * 0.5));
        var hasOracle = owner.Characters.Any(character =>
            character.IsAlive && !GameEngine.IsDeploying(character) && character.Definition.Key == "oracle");
        var chance = hasOracle && max > 0 && oracleReduced != max ? 30 : 0;
        var min = chance > 0 ? oracleReduced : max;
        var knight = owner.Characters.FirstOrDefault(character => character.IsAlive
            && !GameEngine.IsDeploying(character)
            && character.Definition.Key == "knight" && !character.GuardConsumed);
        var guard = source == DamageSource.ActiveAttack && type == DamageType.Physical
            && target.Definition.Key != "knight" && knight is not null && max > 0;
        var guardDamage = guard ? Math.Max(1, (int)Math.Ceiling(max / 3.0)) : 0;
        if (guard)
        {
            min = Math.Max(0, min - Math.Max(1, (int)Math.Ceiling(min / 3.0)));
            max = Math.Max(0, max - guardDamage);
        }
        return new DamageForecast(min, max, type.ToString(), shieldDefense, defense, chance, shield, shieldAbsorb, guard, guardDamage);
    }

    private static int ForecastOutgoingDamage(
        CharacterState source,
        int amount,
        DamageType type,
        DamageSource damageSource,
        bool receivesMagicPowerBonus)
    {
        var damage = Math.Max(0, amount);
        foreach (var status in source.Statuses.Where(status => !status.Expired))
        {
            if (damage <= 0)
                break;
            damage = status.Id switch
            {
                "chant" when type == DamageType.Magical => damage * 2,
                "mighty-strike" when damageSource == DamageSource.ActiveAttack && type == DamageType.Physical =>
                    damage * 2,
                "strong-attack" when damageSource == DamageSource.ActiveAttack && type == DamageType.Physical =>
                    Math.Max(1, (int)Math.Ceiling(damage * 1.5)),
                "magic-surge" when damageSource == DamageSource.ActiveAttack && type == DamageType.Magical =>
                    Math.Max(1, (int)Math.Ceiling(damage * 1.5)),
                "exhaustion" when type == DamageType.Physical => Math.Max(1, damage / 2),
                "erosion" when type == DamageType.Magical => Math.Max(1, damage / 2),
                _ => damage
            };
        }

        if (type == DamageType.Magical
            && damageSource == DamageSource.ActiveAttack
            && receivesMagicPowerBonus
            && source.Statuses.Any(status => status.Id == "magic-power" && !status.Expired))
            damage++;

        return Math.Max(0, damage);
    }

    private static bool ShouldForecastDuelSenseAbsolute(CharacterState attacker) =>
        attacker.Definition.Key == "duelist"
        && attacker.SoldierRank >= 1
        && !attacker.TraitsUsedThisTurn.Contains("duel-sense-absolute");

    private static bool ShouldForecastDeputyDuelistStrike(GameState state, CharacterState attacker) =>
        attacker.DeputyEffectId == "deputy-duelist"
        && !state.FindOwner(attacker).DeputyPassivesUsedThisTurn.Contains($"{attacker.Id:N}:deputy-duelist");

    private static int ForecastDuelSenseAbsoluteBonus(GameState state, CharacterState attacker) =>
        ShouldForecastDuelSenseAbsolute(attacker)
            || ShouldForecastDeputyDuelistStrike(state, attacker)
            ? DuelSenseTrait.AbsoluteDamage
            : 0;

    private static bool IsCommonDebuff(StatusEffect status) =>
        !status.Expired
        && !status.IsBuff
        && status.Id is "burning" or "void" or "exhaustion" or "erosion" or "trembling" or "vulnerable";

    private static int ForecastIncomingStatusDamage(
        CharacterState target,
        int amount,
        DamageType type,
        DamageSource source)
    {
        var damage = Math.Max(0, amount);
        foreach (var status in target.Statuses.Where(status => !status.Expired))
        {
            if (damage <= 0)
                break;
            damage = status.Id switch
            {
                "void" when type == DamageType.Magical => Math.Max(1, (int)Math.Ceiling(damage * 1.25)),
                "vulnerable" when type == DamageType.Physical => Math.Max(1, (int)Math.Ceiling(damage * 1.25)),
                "fortify" when type == DamageType.Physical => Math.Max(1, damage / 2),
                "spell-ward" when type == DamageType.Magical => Math.Max(1, damage / 2),
                "guard-oath" when source == DamageSource.ActiveAttack && type == DamageType.Physical =>
                    Math.Max(0, damage - GuardOathStatus.PhysicalReduction),
                _ => damage
            };
        }

        return Math.Max(0, damage);
    }

    private int ForecastRageShieldBreakBonus(
        GameState state,
        CharacterState attacker,
        CharacterState defender,
        DamageForecast attack)
    {
        if (attacker.Statuses.All(status => status.Id != "rage" || status.Expired))
            return 0;

        var defenderOwner = state.FindOwner(defender);
        return defenderOwner.SharedShield > 0 && attack.ShieldAbsorb >= defenderOwner.SharedShield
            ? _engine.GetActiveAttack(state, attacker)
            : 0;
    }

    private static (bool, LocalizedText) ForecastTrait(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack) => attacker.Definition.Key switch
    {
        "peasant" => attacker.Statuses.Any(status => status.Id == "harvest" && !status.Expired)
            ? (false, L10n.Text("preview.trait.harvestActive"))
            : state.ActiveAttacksTakenThisTurn == 0
                ? (true, L10n.Text("preview.trait.sowing"))
                : (false, L10n.Text("preview.trait.notFirstAttack")),
        "mage" => (true, L10n.Text(attacker.Statuses.Any(status =>
            status.Id == "magic-power" && !status.Expired)
            ? "preview.trait.burningBoosted" : "preview.trait.burning")),
        "druid" => ForecastDruidTrait(defender, attack),
        "barbarian" => (attack.HpDamageMax >= AftershockAxeTrait.TriggerDamage,
            L10n.Text(attack.HpDamageMin >= AftershockAxeTrait.TriggerDamage
                ? "preview.trait.aftershockGuaranteed" : "preview.trait.aftershockPossible")),
        "monster" => ForecastMonsterTrait(state, attacker, defender, attack),
        _ => (false, L10n.Text("preview.trait.none"))
    };

    private static (bool, LocalizedText) ForecastDruidTrait(CharacterState defender, DamageForecast attack)
    {
        var guaranteed = attack.HpDamageMin > 0;
        var noDamage = attack.HpDamageMax == 0;
        var key = (guaranteed, noDamage) switch
        {
            (true, _) => "preview.trait.sporesGuaranteed",
            (false, true) => "preview.trait.sporesChance",
            _ => "preview.trait.sporesVariable"
        };
        return (true, L10n.Text(key));
    }

    private static (bool, LocalizedText) ForecastMonsterTrait(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack)
    {
        if (defender.Definition.Key == "princess")
            return (true, L10n.Text("preview.trait.beautyPrincessBacklashTrait"));

        var hasPrincess = state.FindOwner(attacker).Characters.Any(character =>
            character.IsAlive && !GameEngine.IsDeploying(character) && character.Definition.Key == "princess");
        var damage = attacker.Definition.Attack
            + (hasPrincess ? PredatoryInstinctTrait.PrincessBonusDamage : 0);
        var key = attack.HpDamageMax == 0 ? "preview.trait.beautyGuaranteed"
            : attack.HpDamageMin == 0 ? "preview.trait.beautyPossible"
            : "preview.trait.beautyUnavailable";
        return (attack.HpDamageMin == 0, L10n.Text(key, ("amount", L10n.Raw(damage))));
    }

    private static LocalizedText? Validate(GameState state, CharacterState attacker, CharacterState defender)
    {
        if (state.Phase != GamePhase.Playing) return L10n.Text("error.matchFinished");
        if (state.RewardWindow is not null) return L10n.Text("error.rewardWindowOpen");
        if (state.PendingRoleActionUpgrade is not null) return L10n.Text("error.pendingRoleActionUpgrade");
        if (state.PendingHeroDraft is not null) return L10n.Text("error.pendingHeroDraft");
        if (attacker.PlayerId != state.ActivePlayerId) return L10n.Text("error.notActiveCharacter");
        if (defender.PlayerId == state.ActivePlayerId) return L10n.Text("error.cannotAttackAlly");
        if (!attacker.IsAlive || !defender.IsAlive) return L10n.Text("error.defeatedSelection");
        if (GameEngine.IsDeploying(attacker) || GameEngine.IsDeploying(defender)) return L10n.Text("error.deploying");
        if (attacker.HasActed || GameEngine.IsActiveAttackBlocked(attacker)) return L10n.Text("error.alreadyActed");
        if (attacker.Definition.Cost > state.ActionPoints) return L10n.Text("error.notEnoughAp");
        return null;
    }

    private static AttackPreview Invalid(Guid attackerId, Guid defenderId, LocalizedText error) => new(
        false, error, attackerId, defenderId, 0,
        new DamageForecast(0, 0, DamageType.Physical.ToString(), 0, 0, 0, false, 0, false, 0),
        new DamageForecast(0, 0, DamageType.Physical.ToString(), 0, 0, 0, false, 0, false, 0),
        string.Empty, false, L10n.Text("preview.trait.none"), []);
}
