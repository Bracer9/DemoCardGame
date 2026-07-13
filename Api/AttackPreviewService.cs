using TinyPixelFights.Domain;

namespace TinyPixelFights.Api;

public sealed class AttackPreviewService
{
    private sealed record RelicDamageForecast(
        DamageForecast Forecast,
        bool AstralPrism,
        bool AshenDetonator,
        int RemainingSharedShield);

    private sealed record CollateralForecast(
        CharacterState Target,
        DamageForecast Forecast);

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
        var attackerOwner = state.FindOwner(attacker);
        var defenderOwner = state.FindOwner(defender);
        var duelistTicketWillTrigger = !monsterPrincessAttack
            && attackerAttackType == DamageType.Physical
            && defenderOwner.SharedShield <= 0
            && RelicEffects.HasRelic(attackerOwner, "relic-duelist-ticket")
            && !attackerOwner.RelicsUsedThisTurn.Contains("relic-duelist-ticket");
        var companyStandardBonus = attacker.Definition.CardType == CardType.Soldier
            && RelicEffects.HasRelic(attackerOwner, "relic-company-standard")
                ? attackerOwner.Characters.Count(character =>
                    character.Definition.CardType == CardType.Soldier
                    && character.IsAlive
                    && character.IsInBattle
                    && !GameEngine.IsDeploying(character))
                : 0;
        var redHourglassBonus = !monsterPrincessAttack
            && attackerAttackType == DamageType.Physical
            && state.PhysicalActiveAttacksTakenThisTurn == 2
            && RelicEffects.HasRelic(attackerOwner, "relic-red-hourglass")
            && !attackerOwner.RelicsUsedThisTurn.Contains("relic-red-hourglass")
                ? _engine.GetActiveAttack(state, attacker)
                : 0;
        var attackPacketBase = _engine.GetEffectiveActiveAttack(state, attacker)
            + companyStandardBonus
            + redHourglassBonus;
        var astralAlignment = attacker.Statuses.OfType<AstralAlignmentStatus>()
            .FirstOrDefault(status => !status.Expired && attackerAttackType == DamageType.Magical);
        var militiaCall = attacker.Statuses.OfType<MilitiaCallStatus>()
            .FirstOrDefault(status => !status.Expired);
        var hunted = defender.Statuses.OfType<HuntedStatus>()
            .FirstOrDefault(status => !status.Expired
                && attacker.Definition.CardType == CardType.Soldier
                && !status.HasTriggeredFor(attacker.Id));
        var attackBase = monsterPrincessAttack
            ? ForecastAbsoluteDamage(state, attacker, defender, attackPacketBase)
            : ForecastOutgoingDamage(attacker, attackPacketBase,
                attackerAttackType,
                DamageSource.ActiveAttack,
                receivesMagicPowerBonus: true,
                grantStrongAttack: duelistTicketWillTrigger);
        var jesterTraitWillApply = attacker.Definition.Key == "jester"
            && defenderOwner.SharedShield <= 0
            && !attacker.TraitsUsedThisTurn.Contains("malicious-jest");
        var jesterAuraBonus = !monsterPrincessAttack
            && attackerAttackType is DamageType.Physical or DamageType.Magical
            && GameEngine.HasActiveRank1SoldierAura(attackerOwner, "jester")
            && (jesterTraitWillApply || defender.Statuses.Any(status => !status.IsBuff && !status.Expired))
            ? 1
            : 0;
        attackBase += jesterAuraBonus;
        var attackPacket = monsterPrincessAttack
            ? new DamageForecast(attackBase, attackBase, DamageType.Absolute.ToString(), 0, 0, 0, false, 0, false, 0)
            : ForecastActiveAttackDamage(state, attacker, defender, attackBase, monsterPrincessAttack);
        attackPacket = ForecastDamageLanding(defender, attackPacket);
        var attackHp = attackPacket;
        var attack = ApplyZeroHpTriggerForecast(state, defender, attackPacket);
        var remainingDefenderShield = Math.Max(0, defenderOwner.SharedShield - attackPacket.ShieldAbsorb);
        var attritionLedgerBonus = ForecastAttritionLedgerBonus(state, attacker, defender);
        var attritionLedgerBefore = attack;
        if (attritionLedgerBonus > 0)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack, attritionLedgerBonus);
        var attritionLedgerWillTrigger = attritionLedgerBonus > 0
            && (attack.HpDamageMin != attritionLedgerBefore.HpDamageMin
                || attack.HpDamageMax != attritionLedgerBefore.HpDamageMax);
        var chantRelicForecast = ForecastChantAndBurningRelics(
            state, attacker, defender, attackPacket, attack, remainingDefenderShield);
        attack = chantRelicForecast.Forecast;

        var victoryEdict = attacker.Statuses.OfType<VictoryEdictStatus>().FirstOrDefault(status => !status.Expired);
        var victoryEdictWillTrigger = victoryEdict is not null && attack.HpDamageMin < defender.CurrentHp;
        if (victoryEdictWillTrigger)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack,
                ForecastStatusAbsoluteDamage(state, victoryEdict!, defender));

        var pact = attacker.Statuses.OfType<PactStatus>().FirstOrDefault(status => !status.Expired);
        var pactWillTrigger = pact is not null && attack.HpDamageMin < defender.CurrentHp;
        if (pactWillTrigger)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack,
                ForecastStatusAbsoluteDamage(state, pact!, defender));

        var abyssalBargain = attacker.Statuses.OfType<AbyssalBargainStatus>().FirstOrDefault(status => !status.Expired);
        var abyssalBargainWillTrigger = abyssalBargain is not null && attack.HpDamageMin < defender.CurrentHp;
        if (abyssalBargainWillTrigger)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack,
                ForecastStatusAbsoluteDamage(state, abyssalBargain!, defender));
        var rageShieldBreakBonus = ForecastRageShieldBreakBonus(state, attacker, defender, attack);
        if (rageShieldBreakBonus > 0)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack,
                ForecastAbsoluteDamage(state, attacker, defender, rageShieldBreakBonus));
        var duelSenseBonus = ForecastDuelSenseAbsoluteBonus(state, attacker);
        var duelSenseBefore = attack;
        if (duelSenseBonus > 0)
            attack = AddDirectHpDamageIfTargetSurvives(defender, attack,
                ForecastAbsoluteDamage(state, attacker, defender, duelSenseBonus));
        var duelSenseWillTrigger = duelSenseBonus > 0
            && (attack.HpDamageMin != duelSenseBefore.HpDamageMin || attack.HpDamageMax != duelSenseBefore.HpDamageMax);
        var monsterFollowUpDamage = ForecastMonsterFollowUpDamage(state, attacker, defender);
        var monsterFollowUpPossible = monsterFollowUpDamage > 0
            && attackHp.HpDamageMin == 0
            && attack.HpDamageMin < defender.CurrentHp;
        if (monsterFollowUpPossible)
        {
            var followUpDamage = ForecastAbsoluteDamage(state, attacker, defender, monsterFollowUpDamage);
            attack = attackHp.HpDamageMax == 0
                ? AddDirectHpDamageIfTargetSurvives(defender, attack, followUpDamage)
                : AddPossibleDirectHpDamageIfTargetSurvives(defender, attack, followUpDamage);
        }
        var astralSplashForecasts = ForecastAstralSplash(
            state, attacker, defender, astralAlignment, chantRelicForecast.RemainingSharedShield);
        var nightBaitWillTrigger = attackHp.HpDamageMax == 0
            && (attackHp.Max > 0 || attackHp.ShieldWillAbsorb)
            && RelicEffects.HasRelic(attackerOwner, "relic-night-bait")
            && !attackerOwner.RelicsUsedThisTurn.Contains("relic-night-bait");
        var greenStandardWillTrigger = defenderOwner.SharedShield > 0
            && attackHp.ShieldAbsorb >= defenderOwner.SharedShield
            && RelicEffects.HasRelic(attackerOwner, "relic-green-standard")
            && !attackerOwner.RelicsUsedThisTurn.Contains("relic-green-standard");
        var echoCrystalWillTrigger = attackerAttackType == DamageType.Magical
            && attacker.Statuses.Any(status => status.Id == "chant" && !status.Expired)
            && attackPacketBase > 0
            && RelicEffects.HasRelic(attackerOwner, "relic-echo-crystal")
            && !attackerOwner.RelicsUsedThisTurn.Contains("relic-echo-crystal");
        var counterBase = ForecastOutgoingDamage(defender, _engine.GetCounterAttack(state, defender),
            defenderAttackType, DamageSource.CounterAttack, receivesMagicPowerBonus: false);
        if (counterBase > 0
            && jesterTraitWillApply
            && defender.Statuses.All(status => status.Expired || status.Id !=
                (defenderAttackType == DamageType.Physical ? "exhaustion" : "erosion")))
            counterBase = Math.Max(1, counterBase / 2);
        var counter = ForecastDamage(state, defender, attacker, counterBase,
            defenderAttackType, DamageSource.CounterAttack);
        counter = ForecastDamageLanding(attacker, counter);
        var counterHp = counter;
        counter = ApplyZeroHpTriggerForecast(state, attacker, counter);
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
        if (duelistTicketWillTrigger)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-duelist-ticket"))));
        if (companyStandardBonus > 0)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-company-standard"))));
        if (redHourglassBonus > 0)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-red-hourglass"))));
        if (jesterAuraBonus > 0)
            notes.Add(L10n.Text("preview.trait.jesterAura"));
        if (astralAlignment is not null)
            notes.Add(L10n.Text("preview.roleAction.damageBonus",
                ("roleAction", L10n.RoleAction("astral-alignment")),
                ("value", L10n.Raw(astralAlignment.Magnitude))));
        if (militiaCall is not null)
            notes.Add(L10n.Text("preview.roleAction.damageBonus",
                ("roleAction", L10n.RoleAction("militia-call")),
                ("value", L10n.Raw(militiaCall.Magnitude))));
        if (hunted is not null)
            notes.Add(L10n.Text("preview.roleAction.damageBonus",
                ("roleAction", L10n.RoleAction("call-the-hunt")),
                ("value", L10n.Raw(hunted.Magnitude))));
        foreach (var splash in astralSplashForecasts)
            notes.Add(L10n.Text("preview.roleAction.astralAlignmentSplash",
                ("character", L10n.Character(splash.Target.Definition.Key)),
                ("min", L10n.Raw(splash.Forecast.Min)),
                ("max", L10n.Raw(splash.Forecast.Max))));
        if (chantRelicForecast.AstralPrism)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-astral-prism"))));
        if (chantRelicForecast.AshenDetonator)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-ashen-detonator"))));
        if (nightBaitWillTrigger)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-night-bait"))));
        if (greenStandardWillTrigger)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-green-standard"))));
        if (echoCrystalWillTrigger)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-echo-crystal"))));
        if (attritionLedgerWillTrigger)
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-attrition-ledger"))));
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
        if (victoryEdictWillTrigger)
            notes.Add(L10n.Text("preview.roleAction.absoluteFollowUp",
                ("roleAction", L10n.RoleAction("edict-of-victory")),
                ("value", L10n.Raw(ForecastStatusAbsoluteDamage(state, victoryEdict!, defender)))));
        if (pactWillTrigger)
            notes.Add(L10n.Text("preview.roleAction.darkPact",
                ("value", L10n.Raw(ForecastStatusAbsoluteDamage(state, pact!, defender)))));
        if (abyssalBargainWillTrigger)
            notes.Add(L10n.Text("preview.roleAction.absoluteFollowUp",
                ("roleAction", L10n.RoleAction("abyssal-bargain")),
                ("value", L10n.Raw(ForecastStatusAbsoluteDamage(state, abyssalBargain!, defender)))));
        if (defender.Statuses.Any(status => status.Id == "prey" && !status.Expired) && attackHp.HpDamageMin == 0)
            notes.Add(L10n.Text("preview.roleAction.predatoryGaze",
                ("value", L10n.Raw(PreyStatus.AbsoluteDamage))));
        var nightmarePrey = defender.Statuses.OfType<NightmarePreyStatus>().FirstOrDefault(status => !status.Expired);
        if (nightmarePrey is not null && attackHp.HpDamageMin == 0)
            notes.Add(L10n.Text("preview.roleAction.nightmareStare",
                ("value", L10n.Raw(ForecastStatusAbsoluteDamage(state, nightmarePrey, defender)))));
        if (attacker.Statuses.Any(status => status.Id == "prey" && !status.Expired) && counterHp.HpDamageMin == 0)
            notes.Add(L10n.Text("preview.roleAction.predatoryGazeCounter",
                ("value", L10n.Raw(PreyStatus.AbsoluteDamage))));

        return new AttackPreview(true, null, attacker.Id, defender.Id, attacker.Definition.Cost,
            attack, counter, trait.Metadata.Id, possible, traitText, notes);
    }

    private static DamageForecast ApplyZeroHpTriggerForecast(
        GameState state,
        CharacterState target,
        DamageForecast forecast)
    {
        var zeroHpFollowUps = target.Statuses
            .Where(status => !status.Expired && status is PreyStatus or NightmarePreyStatus)
            .ToArray();
        if (zeroHpFollowUps.Length == 0 || forecast.HpDamageMin > 0)
            return forecast;

        var followUpDamage = zeroHpFollowUps.Sum(status => ForecastStatusAbsoluteDamage(state, status, target));

        if (forecast.HpDamageMax == 0)
            return AddDirectHpDamage(forecast, followUpDamage);

        var triggeredTotal = forecast.Min + followUpDamage;
        var triggeredHp = forecast.HpDamageMin + followUpDamage;
        return forecast with
        {
            Min = Math.Min(triggeredTotal, forecast.Max),
            Max = Math.Max(triggeredTotal, forecast.Max),
            HpDamageMin = Math.Min(triggeredHp, forecast.HpDamageMax),
            HpDamageMax = Math.Max(triggeredHp, forecast.HpDamageMax)
        };
    }

    private static int ForecastStatusAbsoluteDamage(
        GameState state,
        StatusEffect status,
        CharacterState target)
    {
        var source = state.FindCharacter(status.SourceCharacterId);
        return ForecastAbsoluteDamage(state, source, target, status.Magnitude);
    }

    private RelicDamageForecast ForecastChantAndBurningRelics(
        GameState state,
        CharacterState attacker,
        CharacterState defender,
        DamageForecast baseForecast,
        DamageForecast currentForecast,
        int remainingSharedShield,
        int? targetMoraleOverride = null,
        int? targetHpOverride = null)
    {
        var owner = state.FindOwner(attacker);
        var result = currentForecast;
        var targetHp = Math.Max(0, targetHpOverride ?? defender.CurrentHp);
        var baseCanHit = baseForecast.Max > 0 && currentForecast.HpDamageMin < targetHp;
        var baseAlwaysHits = baseForecast.Min > 0 && currentForecast.HpDamageMax < targetHp;
        var astralPrism = baseCanHit
            && GameEngine.GetAttackType(attacker) == DamageType.Magical
            && attacker.Statuses.Any(status => status.Id == "chant" && !status.Expired)
            && RelicEffects.HasRelic(owner, "relic-astral-prism")
            && !owner.RelicsUsedThisTurn.Contains("relic-astral-prism");
        if (astralPrism)
        {
            var prismBase = ForecastOutgoingDamage(
                attacker,
                _engine.GetEffectiveActiveAttack(state, attacker),
                DamageType.Magical,
                DamageSource.Trait,
                receivesMagicPowerBonus: false,
                canConsumeChargeStatuses: false,
                canApplyOneShotStatuses: false);
            var prism = ForecastDamage(state, attacker, defender, prismBase,
                DamageType.Magical, DamageSource.Trait,
                canApplyHunted: baseForecast.Max <= 0,
                sharedShieldOverride: remainingSharedShield);
            result = AppendNormalDamage(
                defender, result, prism, baseAlwaysHits, targetMoraleOverride, targetHp);
            remainingSharedShield = Math.Max(0, remainingSharedShield - prism.ShieldAbsorb);
        }

        var burning = defender.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired);
        var ashenCanTrigger = baseForecast.Max > 0 && result.HpDamageMin < targetHp;
        var ashenAlwaysTriggers = baseAlwaysHits && result.HpDamageMax < targetHp;
        var ashenDetonator = ashenCanTrigger
            && burning is { Stacks: >= 3 }
            && RelicEffects.HasRelic(owner, "relic-ashen-detonator")
            && !owner.RelicsUsedThisTurn.Contains("relic-ashen-detonator");
        if (ashenDetonator)
        {
            var detonationBase = ForecastOutgoingDamage(
                attacker,
                burning!.Stacks * 2,
                DamageType.Magical,
                DamageSource.Trait,
                receivesMagicPowerBonus: false,
                canConsumeChargeStatuses: false,
                canApplyOneShotStatuses: false);
            var detonation = ForecastDamage(state, attacker, defender, detonationBase, DamageType.Magical,
                DamageSource.Trait,
                ignoreDefense: true,
                canApplyHunted: baseForecast.Max <= 0,
                sharedShieldOverride: remainingSharedShield);
            result = AppendNormalDamage(
                defender, result, detonation, ashenAlwaysTriggers, targetMoraleOverride, targetHp);
            remainingSharedShield = Math.Max(0, remainingSharedShield - detonation.ShieldAbsorb);
        }

        return new RelicDamageForecast(result, astralPrism, ashenDetonator, remainingSharedShield);
    }

    private static DamageForecast AppendNormalDamage(
        CharacterState target,
        DamageForecast current,
        DamageForecast extra,
        bool guaranteed,
        int? targetMoraleOverride = null,
        int? targetHpOverride = null)
    {
        var targetHp = Math.Max(0, targetHpOverride ?? target.CurrentHp);
        var targetMorale = Math.Max(0, targetMoraleOverride ?? target.Morale);
        if (Enum.TryParse<DamageType>(current.DamageType, out var currentType)
            && currentType == DamageType.Absolute)
        {
            var landedExtra = ForecastDamageLanding(target, extra, targetMorale);
            var survivingBaseMax = Math.Min(current.Max, Math.Max(0, targetHp - 1));
            return current with
            {
                Min = current.Min + (guaranteed ? landedExtra.Min : 0),
                Max = Math.Max(current.Max, survivingBaseMax + landedExtra.Max),
                MoraleDamageMin = current.MoraleDamageMin + (guaranteed ? landedExtra.MoraleDamageMin : 0),
                MoraleDamageMax = Math.Max(current.MoraleDamageMax, landedExtra.MoraleDamageMax),
                HpDamageMin = current.HpDamageMin + (guaranteed ? landedExtra.HpDamageMin : 0),
                HpDamageMax = Math.Max(current.HpDamageMax, survivingBaseMax + landedExtra.HpDamageMax)
            };
        }

        var survivingNormalBaseMax = Math.Min(
            current.Max,
            Math.Max(0, targetMorale + targetHp - 1));
        var combined = current with
        {
            Min = current.Min + (guaranteed ? extra.Min : 0),
            Max = Math.Max(current.Max, survivingNormalBaseMax + extra.Max),
            ShieldWillAbsorb = current.ShieldWillAbsorb || extra.ShieldWillAbsorb,
            ShieldAbsorb = current.ShieldAbsorb + extra.ShieldAbsorb
        };
        return ForecastDamageLanding(target, combined, targetMorale);
    }

    private static int ForecastAbsoluteDamage(
        GameState state,
        CharacterState source,
        CharacterState target,
        int amount)
    {
        var damage = Math.Max(0, amount);
        var owner = state.FindOwner(source);
        return damage > 0
            && RelicEffects.HasRelic(owner, "relic-predator-crown")
            && target.Statuses.Any(status => status.Id == "prey" && !status.Expired)
                ? (int)Math.Ceiling(damage * 1.5)
                : damage;
    }

    private static int ForecastAttritionLedgerBonus(
        GameState state,
        CharacterState attacker,
        CharacterState defender)
    {
        var owner = state.FindOwner(attacker);
        var damage = GameEngine.GetAttritionLedgerDamage(defender);
        return RelicEffects.HasRelic(owner, "relic-attrition-ledger")
            ? ForecastAbsoluteDamage(state, attacker, defender, damage)
            : 0;
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

    private static DamageForecast AddPossibleDirectHpDamageIfTargetSurvives(
        CharacterState target,
        DamageForecast forecast,
        int amount)
    {
        var triggered = AddDirectHpDamageIfTargetSurvives(target, forecast, amount);
        return forecast with
        {
            Max = Math.Max(forecast.Max, triggered.Max),
            HpDamageMax = Math.Max(forecast.HpDamageMax, triggered.HpDamageMax)
        };
    }

    private int ForecastMonsterFollowUpDamage(
        GameState state,
        CharacterState attacker,
        CharacterState defender)
    {
        if (attacker.Definition.Key != "monster" || defender.Definition.Key == "princess")
            return 0;

        var hasPrincess = state.FindOwner(attacker).Characters.Any(character =>
            character.IsAlive && !GameEngine.IsDeploying(character) && character.Definition.Key == "princess");
        var preyBonus = HeroRankRules.HasRank2Path(attacker, "predatory-gaze")
            && defender.Statuses.Any(status => status.Id is "prey" or "nightmare-prey" && !status.Expired)
                ? 1
                : 0;
        return _engine.GetActiveAttack(state, attacker)
            + (hasPrincess ? PredatoryInstinctTrait.PrincessBonusDamage : 0)
            + preyBonus;
    }

    private IReadOnlyList<CollateralForecast> ForecastAstralSplash(
        GameState state,
        CharacterState attacker,
        CharacterState defender,
        AstralAlignmentStatus? alignment,
        int remainingShield)
    {
        if (alignment is null)
            return [];

        var targetOwner = state.FindOwner(defender);
        var splashBase = Math.Max(1, (int)Math.Ceiling(alignment.Magnitude / 2.0));

        var forecasts = new List<CollateralForecast>();
        foreach (var character in targetOwner.Characters
            .Where(character => character.IsAlive
                && !GameEngine.IsDeploying(character)
                && Math.Abs(character.Slot - defender.Slot) == 1))
        {
            var forecast = ForecastCollateralDamage(
                state, attacker, character, splashBase, DamageType.Magical, remainingShield);
            forecasts.Add(new CollateralForecast(character, forecast));
            remainingShield = Math.Max(0, remainingShield - forecast.ShieldAbsorb);
        }

        return forecasts;
    }

    private static DamageForecast ForecastDamageLanding(
        CharacterState target,
        DamageForecast forecast,
        int? moraleOverride = null)
    {
        if (Enum.TryParse<DamageType>(forecast.DamageType, out var damageType) && damageType == DamageType.Absolute)
            return forecast with
            {
                MoraleDamageMin = 0,
                MoraleDamageMax = 0,
                HpDamageMin = forecast.Min,
                HpDamageMax = forecast.Max
            };

        var morale = Math.Max(0, moraleOverride ?? target.Morale);
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
            return ForecastDamage(state, attacker, defender, attackBase, attackerAttackType, DamageSource.ActiveAttack,
                ignoreShield: ignoreShield,
                ignoreDefense: false);

        var reduced = ForecastDamage(state, attacker, defender, attackBase / 2, attackerAttackType, DamageSource.ActiveAttack,
            ignoreShield: ignoreShield,
            ignoreDefense: false);
        var amplified = ForecastDamage(state, attacker, defender, attackBase + FateMarkedStatus.DamageBonus, attackerAttackType, DamageSource.ActiveAttack,
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

    private DamageForecast ForecastDamage(GameState state, CharacterState sourceCharacter, CharacterState target, int baseDamage,
        DamageType type, DamageSource source, bool ignoreShield = false, bool ignoreDefense = false,
        bool canApplyHunted = true, int? sharedShieldOverride = null)
    {
        var owner = state.FindOwner(target);
        var sharedShield = ignoreShield ? 0 : Math.Max(0, sharedShieldOverride ?? owner.SharedShield);
        var shieldDefense = 0;
        var damageAfterShieldDefense = baseDamage;
        if (sharedShield > 0 && damageAfterShieldDefense > 0)
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

        var shield = sharedShield > 0 && damageAfterShieldDefense > 0;
        var shieldAbsorb = shield ? Math.Min(sharedShield, damageAfterShieldDefense) : 0;
        var damageAfterShield = Math.Max(0, damageAfterShieldDefense - shieldAbsorb);

        var effectiveDefense = ignoreDefense ? 0 : _engine.GetDefense(state, target, type);
        var defense = effectiveDefense > 0
            ? Math.Min(damageAfterShield, effectiveDefense)
            : effectiveDefense < 0 && damageAfterShield > 0
                ? effectiveDefense
                : 0;
        var damageAfterDefense = defense >= 0
            ? Math.Max(0, damageAfterShield - defense)
            : damageAfterShield + Math.Abs(defense);
        damageAfterDefense = ForecastIncomingStatusDamage(
            sourceCharacter, target, damageAfterDefense, type, source, canApplyHunted);
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
        bool receivesMagicPowerBonus,
        bool grantStrongAttack = false,
        bool canConsumeChargeStatuses = true,
        bool canApplyOneShotStatuses = true)
    {
        var damage = Math.Max(0, amount);
        foreach (var status in source.Statuses.Where(status => !status.Expired))
        {
            if (damage <= 0)
                break;
            damage = status.Id switch
            {
                "chant" when canConsumeChargeStatuses && type == DamageType.Magical => damage * 2,
                "mighty-strike" when damageSource == DamageSource.ActiveAttack && type == DamageType.Physical =>
                    damage * 2,
                "strong-attack" when damageSource == DamageSource.ActiveAttack && type == DamageType.Physical =>
                    Math.Max(1, (int)Math.Ceiling(damage * 1.5)),
                "magic-surge" when damageSource == DamageSource.ActiveAttack && type == DamageType.Magical =>
                    Math.Max(1, (int)Math.Ceiling(damage * 1.5)),
                "astral-alignment" when canApplyOneShotStatuses && type == DamageType.Magical => damage + status.Magnitude,
                "militia-call" when canApplyOneShotStatuses && damageSource == DamageSource.ActiveAttack => damage + status.Magnitude,
                "exhaustion" when type == DamageType.Physical => Math.Max(1, damage / 2),
                "erosion" when type == DamageType.Magical => Math.Max(1, damage / 2),
                _ => damage
            };
        }

        if (grantStrongAttack
            && type == DamageType.Physical
            && damageSource == DamageSource.ActiveAttack
            && source.Statuses.All(status => status.Id != "strong-attack" || status.Expired))
            damage = Math.Max(1, (int)Math.Ceiling(damage * 1.5));

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
        CharacterState sourceCharacter,
        CharacterState target,
        int amount,
        DamageType type,
        DamageSource source,
        bool canApplyHunted)
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
                "hunted" when status is HuntedStatus hunted
                    && canApplyHunted
                    && sourceCharacter.Definition.CardType == CardType.Soldier
                    && !hunted.HasTriggeredFor(sourceCharacter.Id) => damage + hunted.Magnitude,
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

    private (bool, LocalizedText) ForecastTrait(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack) => attacker.Definition.Key switch
    {
        "peasant" => attacker.Statuses.Any(status => status.Id == "harvest" && !status.Expired)
            ? (false, L10n.Text("preview.trait.harvestActive"))
            : state.ActiveAttacksTakenThisTurn == 0
                ? (true, L10n.Text("preview.trait.sowing"))
                : (false, L10n.Text("preview.trait.notFirstAttack")),
        "mage" => (true, L10n.Text(HeroRankRules.HasRank2Path(attacker, "arcane-channel")
            ? "preview.trait.burningBoosted" : "preview.trait.burning")),
        "druid" => ForecastDruidTrait(defender, attack),
        "barbarian" => ForecastBarbarianTrait(attacker, attack),
        "monster" => ForecastMonsterTrait(state, attacker, defender, attack),
        "jester" => ForecastJesterTrait(state, attacker, defender),
        _ => (false, L10n.Text("preview.trait.none"))
    };

    private static (bool, LocalizedText) ForecastBarbarianTrait(
        CharacterState attacker,
        DamageForecast attack)
    {
        var ragingRank2 = HeroRankRules.HasRank2Path(attacker, "war-cry")
            && attacker.Statuses.Any(status => status.Id == "rage" && !status.Expired);
        if (ragingRank2)
            return (true, L10n.Text("preview.trait.aftershockRage"));

        return (attack.HpDamageMax >= AftershockAxeTrait.TriggerDamage,
            L10n.Text(attack.HpDamageMin >= AftershockAxeTrait.TriggerDamage
                ? "preview.trait.aftershockGuaranteed" : "preview.trait.aftershockPossible"));
    }

    private static (bool, LocalizedText) ForecastJesterTrait(
        GameState state,
        CharacterState attacker,
        CharacterState defender)
    {
        if (attacker.TraitsUsedThisTurn.Contains("malicious-jest"))
            return (false, L10n.Text("preview.trait.none"));
        if (state.FindOwner(defender).SharedShield > 0)
            return (false, L10n.Text("preview.trait.maliciousJestShielded"));

        var targetAlreadyHadDebuff = defender.Statuses.Any(status => !status.IsBuff && !status.Expired);
        return (true, L10n.Text("preview.trait.maliciousJest",
            ("status", L10n.Status(GameEngine.GetAttackType(defender) == DamageType.Physical ? "exhaustion" : "erosion")),
            ("spread", L10n.Raw(attacker.SoldierRank >= 1 && targetAlreadyHadDebuff ? 1 : 0))));
    }

    internal DamageForecast ForecastRoleActionDamage(
        GameState state,
        CharacterState actor,
        CharacterState target,
        int amount,
        DamageType damageType,
        out int remainingSharedShield,
        int? targetMoraleOverride = null,
        int? targetHpOverride = null)
    {
        var packet = ForecastRoleActionPrimaryDamage(
            state, actor, target, amount, damageType, targetMoraleOverride);
        var result = ApplyZeroHpTriggerForecast(state, target, packet);
        var targetOwner = state.FindOwner(target);
        var relics = ForecastChantAndBurningRelics(
            state,
            actor,
            target,
            packet,
            result,
            Math.Max(0, targetOwner.SharedShield - packet.ShieldAbsorb),
            targetMoraleOverride,
            targetHpOverride);
        remainingSharedShield = relics.RemainingSharedShield;
        return relics.Forecast;
    }

    internal DamageForecast ForecastRoleActionPrimaryDamage(
        GameState state,
        CharacterState actor,
        CharacterState target,
        int amount,
        DamageType damageType,
        int? targetMoraleOverride = null)
    {
        var outgoing = ForecastOutgoingDamage(
            actor,
            Math.Max(0, amount),
            damageType,
            DamageSource.RoleAction,
            receivesMagicPowerBonus: damageType == DamageType.Magical);
        return ForecastDamageLanding(
            target,
            ForecastDamage(state, actor, target, outgoing, damageType, DamageSource.RoleAction),
            targetMoraleOverride);
    }

    internal DamageForecast ForecastCollateralDamage(
        GameState state,
        CharacterState actor,
        CharacterState target,
        int amount,
        DamageType damageType,
        int? sharedShieldOverride = null)
    {
        var outgoing = ForecastOutgoingDamage(
            actor,
            Math.Max(0, amount),
            damageType,
            DamageSource.Trait,
            receivesMagicPowerBonus: false,
            canConsumeChargeStatuses: false,
            canApplyOneShotStatuses: false);
        var packet = ForecastDamageLanding(target,
            ForecastDamage(state, actor, target, outgoing, damageType, DamageSource.Trait,
                sharedShieldOverride: sharedShieldOverride));
        return ApplyZeroHpTriggerForecast(state, target, packet);
    }

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

    private (bool, LocalizedText) ForecastMonsterTrait(GameState state, CharacterState attacker,
        CharacterState defender, DamageForecast attack)
    {
        if (defender.Definition.Key == "princess")
            return (true, L10n.Text("preview.trait.beautyPrincessBacklashTrait"));

        var hasPrincess = state.FindOwner(attacker).Characters.Any(character =>
            character.IsAlive && !GameEngine.IsDeploying(character) && character.Definition.Key == "princess");
        var preyBonus = HeroRankRules.HasRank2Path(attacker, "predatory-gaze")
            && defender.Statuses.Any(status => status.Id is "prey" or "nightmare-prey" && !status.Expired)
                ? 1
                : 0;
        var damage = _engine.GetActiveAttack(state, attacker)
            + (hasPrincess ? PredatoryInstinctTrait.PrincessBonusDamage : 0)
            + preyBonus;
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
