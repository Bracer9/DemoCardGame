using TinyPixelFights.Domain;

namespace TinyPixelFights.Api;

public sealed class RoleActionPreviewService
{
    private sealed record HealingForecast(CharacterState Target, int Requested, int Restored)
    {
        public int Overheal => Math.Max(0, Requested - Restored);
    }

    private readonly GameEngine _engine;
    private readonly AttackPreviewService _damagePreviews;

    public RoleActionPreviewService(GameEngine engine, AttackPreviewService damagePreviews)
    {
        _engine = engine;
        _damagePreviews = damagePreviews;
    }

    public RoleActionPreview Create(
        GameState state,
        Guid actorId,
        string roleActionId,
        Guid? targetCharacterId)
    {
        CharacterState actor;
        CharacterState? target = null;
        try
        {
            actor = state.FindCharacter(actorId);
            if (targetCharacterId is not null)
                target = state.FindCharacter(targetCharacterId.Value);
        }
        catch
        {
            return Invalid(actorId, targetCharacterId, roleActionId, L10n.Text("error.characterNotFound"));
        }

        var action = _engine.GetRoleActions(actor)
            .FirstOrDefault(candidate => string.Equals(
                candidate.Metadata.Id,
                roleActionId,
                StringComparison.OrdinalIgnoreCase));
        if (action is null)
            return Invalid(actorId, targetCharacterId, roleActionId, L10n.Text("error.roleActionNotUnlocked"));
        roleActionId = action.Metadata.Id;

        var unavailable = action.UnavailableReason(state, actor);
        if (unavailable is not null)
            return Invalid(actorId, targetCharacterId, roleActionId, unavailable);

        try
        {
            GameEngine.EnsureRoleActionTargetIsValid(state, actor, roleActionId, targetCharacterId);
        }
        catch (GameRuleException exception)
        {
            return Invalid(actorId, targetCharacterId, roleActionId, exception.Error);
        }

        var effects = new List<RoleActionEffectForecast>();
        var notes = new List<LocalizedText>();
        var healingForecasts = new List<HealingForecast>();
        ForecastEffects(state, actor, target, roleActionId, effects, notes, healingForecasts);
        ForecastActiveHealingRelics(state, actor, healingForecasts, effects, notes);
        ForecastRoleActionRelics(state, actor, action.Metadata, roleActionId, target, effects);
        return new RoleActionPreview(
            true,
            null,
            actor.Id,
            target?.Id,
            roleActionId,
            action.Metadata.BaseApCost,
            effects,
            notes);
    }

    private void ForecastEffects(
        GameState state,
        CharacterState actor,
        CharacterState? target,
        string roleActionId,
        List<RoleActionEffectForecast> effects,
        List<LocalizedText> notes,
        List<HealingForecast> healingForecasts)
    {
        var owner = state.FindOwner(actor);
        int? remainingShieldAfterMagicalDamage = null;
        switch (roleActionId)
        {
            case "saintly-prayer":
            {
                var ally = RequireTarget(target);
                var healed = RestoredHealing(ally, 2);
                var cleanses = CountDispellableDebuffs(ally) > 0;
                var didHealOrCleanse = healed > 0 || cleanses;
                var fieldMedic = FindFieldMedic(state, actor);
                healed += ForecastFieldMedicBonusHealing(fieldMedic, ally, ally.CurrentHp + healed, didHealOrCleanse);
                AddHealing(effects, ally, healed);
                TrackHealing(healingForecasts, ally, 2, RestoredHealing(ally, 2));
                if (cleanses)
                    AddEffect(effects, "action-points", actor, 1, detailId: "cleanse-refund");
                if (HeroRankRules.HasRank2Path(actor, "saintly-prayer") && didHealOrCleanse)
                    AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
                if (fieldMedic is not null && didHealOrCleanse)
                    AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
                break;
            }
            case "royal-command":
                AddEffect(effects, "action-points", actor, 2);
                AddEffect(effects, "action-point-debt", actor, 1);
                break;
            case "guard-oath":
                AddEffect(effects, "status-layers", RequireTarget(target), 1, detailId: "guard-oath");
                break;
            case "raise-bulwark":
            {
                var next = Math.Max(1, (int)Math.Ceiling(owner.SharedShield * 1.5));
                AddEffect(effects, "shared-shield", actor, next - owner.SharedShield);
                AddEffect(effects, "shield-defense", actor, 2, detailId: DamageType.Physical.ToString());
                break;
            }
            case "arcane-channel":
                AddEffect(effects, "status-layers", actor, 2, detailId: "chant-pending");
                AddEffect(effects, "status-turns", actor, 1, detailId: "attack-sealed");
                break;
            case "searing-brand":
                AddEffect(effects, "status-layers", RequireTarget(target), BurningStacksAdded(state, actor), detailId: "burning");
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "void");
                break;
            case "cleansing-herbs":
                ForecastCleansingHerbs(state, actor, RequireTarget(target), effects, healingForecasts);
                break;
            case "weakening-spores-action":
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "exhaustion");
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "erosion");
                break;
            case "war-cry":
                AddEffect(effects, "bonus-attacks", actor, Math.Max(0, 1 - actor.BonusAttackUsesThisTurn));
                AddEffect(effects, "status-turns", actor, TurnDurationStatus.DefaultTurns, detailId: "rage");
                break;
            case "challenge":
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "trembling");
                break;
            case "star-reading":
                AddEffect(effects, "bonus-attacks", RequireTarget(target), 1);
                break;
            case "fate-mark":
                AddEffect(effects, "status-layers", RequireTarget(target), FateMarkedStatus.DamageBonus, detailId: "marked");
                break;
            case "predatory-gaze":
                AddEffect(effects, "status-damage", RequireTarget(target), PreyStatus.AbsoluteDamage, detailId: "prey");
                break;
            case "dark-pact":
            {
                var ally = RequireTarget(target);
                var lifeCost = Math.Min(4, Math.Max(0, ally.CurrentHp - 1));
                AddEffect(effects, "hp-cost", ally, lifeCost);
                ForecastHpPaymentRelic(state, actor, lifeCost, effects, notes);
                AddEffect(effects, "status-damage", ally, PactStatus.AbsoluteDamage, detailId: "pact");
                break;
            }
            case "supply-basket":
            {
                var ally = RequireTarget(target);
                var restored = RestoredHealing(ally, 1);
                AddHealing(effects, ally, restored);
                TrackHealing(healingForecasts, ally, 1, restored);
                AddEffect(effects, "status-turns", ally, 2, detailId: "fortify");
                break;
            }
            case "field-work":
                ForecastFieldWork(actor, effects, healingForecasts);
                break;
            case "mend":
            {
                var ally = RequireTarget(target);
                var healing = RestoredHealing(ally, 3);
                var hpAfterHealing = ally.CurrentHp + healing;
                var fieldMedic = FindFieldMedic(state, actor);
                healing += ForecastFieldMedicBonusHealing(fieldMedic, ally, hpAfterHealing, didHealOrCleanse: true);
                AddHealing(effects, ally, healing);
                TrackHealing(healingForecasts, ally, 3, RestoredHealing(ally, 3));
                AddEffect(effects, "status-turns", ally, 2, detailId: "spell-ward");
                break;
            }
            case "aegis-formation":
                AddEffect(effects, "shared-shield", actor, owner.SharedShield <= 0 ? 1 : 2);
                break;
            case "crimson-lunge":
                AddEffect(effects, "status-layers", RequireTarget(target), 1, detailId: "mighty-strike");
                break;
            case "astral-focus":
                AddEffect(effects, "status-layers", RequireTarget(target), 1, detailId: "chant");
                break;
            case "mocking-curtain-call":
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "vulnerable");
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "void");
                break;
            case "miracle-standard":
                ForecastMiracleStandard(state, actor, RequireTarget(target), effects, healingForecasts);
                break;
            case "edict-of-victory":
                AddEffect(effects, "bonus-attacks", RequireTarget(target), 1);
                AddEffect(effects, "status-damage", RequireTarget(target), _engine.GetActiveAttack(state, actor), detailId: "edict-of-victory");
                AddEffect(effects, "action-point-debt", actor, 1);
                break;
            case "astral-alignment":
                AddEffect(effects, "status-layers", RequireTarget(target), 1, detailId: "chant");
                AddEffect(effects, "bonus-attacks", RequireTarget(target), 1);
                AddEffect(effects, "status-damage", RequireTarget(target), _engine.GetActiveAttack(state, actor), detailId: "astral-alignment");
                break;
            case "thread-cut":
                remainingShieldAfterMagicalDamage = ForecastThreadCut(
                    state, actor, RequireTarget(target), effects, notes);
                break;
            case "field-rations":
                ForecastFieldRations(state, actor, RequireTarget(target), effects, healingForecasts);
                break;
            case "militia-call":
            {
                var ally = RequireTarget(target);
                AddEffect(effects, "bonus-attacks", ally, 1);
                AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns,
                    detailId: GameEngine.GetAttackType(ally) == DamageType.Physical ? "strong-attack" : "magic-surge");
                var bonus = _engine.GetActiveAttack(state, actor)
                    + (ally.Definition.CardType == CardType.Soldier ? ally.SoldierRank : 0);
                AddEffect(effects, "status-damage", ally, bonus, detailId: "militia-call");
                break;
            }
            case "starfall":
            {
                var enemy = RequireTarget(target);
                var damage = ForecastDamage(
                    state,
                    actor,
                    enemy,
                    Math.Max(1, _engine.GetActiveAttack(state, actor)),
                    DamageType.Magical,
                    out var remainingShield);
                AddDamage(effects, enemy, damage);
                remainingShieldAfterMagicalDamage = remainingShield;
                if (damage.HpDamageMin < enemy.CurrentHp)
                    AddEffect(effects, "status-layers", enemy, BurningStacksAdded(state, actor), detailId: "burning");
                break;
            }
            case "archive-formula":
            {
                var enemy = RequireTarget(target);
                var burning = enemy.Statuses.OfType<BurningStatus>().FirstOrDefault(status => !status.Expired);
                var hasMagicDebuff = enemy.Statuses.Any(status => !status.Expired && status.Id is "burning" or "void");
                var amount = _engine.GetActiveAttack(state, actor) + (hasMagicDebuff ? burning?.Stacks ?? 0 : 0);
                var damage = ForecastDamage(
                    state, actor, enemy, Math.Max(1, amount), DamageType.Magical, out var remainingShield);
                AddDamage(effects, enemy, damage);
                remainingShieldAfterMagicalDamage = remainingShield;
                AddEffect(effects, "status-layers", enemy,
                    BurningStacksAdded(state, actor, Math.Max(1, CountCommonDebuffs(enemy))), detailId: "burning");
                break;
            }
            case "grove-sanctuary":
                ForecastGroveSanctuary(state, actor, RequireTarget(target), effects, healingForecasts);
                break;
            case "call-the-hunt":
                foreach (var enemy in CharactersInArea(state.FindOwner(RequireTarget(target)), RequireTarget(target)))
                    AddEffect(effects, "status-damage", enemy, _engine.GetActiveAttack(state, actor), detailId: "hunted");
                break;
            case "glory-roar":
                AddEffect(effects, "status-turns", actor, TurnDurationStatus.DefaultTurns, detailId: "strong-attack");
                AddEffect(effects, "bonus-attacks", actor,
                    Math.Max(1, (int)Math.Ceiling(_engine.GetActiveAttack(state, actor) / 3.0)));
                break;
            case "dragon-breaker":
                ForecastDragonBreaker(state, actor, RequireTarget(target), effects, notes);
                break;
            case "nightmare-stare":
                foreach (var enemy in CharactersInArea(state.FindOwner(RequireTarget(target)), RequireTarget(target)))
                    AddEffect(effects, "status-damage", enemy, _engine.GetActiveAttack(state, actor), detailId: "nightmare-prey");
                AddEffect(effects, "status-turns", RequireTarget(target), TurnDurationStatus.DefaultTurns, detailId: "erosion");
                break;
            case "abyssal-bargain":
            {
                var ally = RequireTarget(target);
                var lifeCost = Math.Min(_engine.GetActiveAttack(state, actor), Math.Max(0, ally.CurrentHp - 1));
                AddEffect(effects, "hp-cost", ally, lifeCost);
                ForecastHpPaymentRelic(state, actor, lifeCost, effects, notes);
                AddEffect(effects, "bonus-attacks", ally, 1);
                AddEffect(effects, "status-damage", ally, _engine.GetActiveAttack(state, actor), detailId: "abyssal-bargain");
                break;
            }
            case "holy-bastion":
            {
                var ally = RequireTarget(target);
                AddEffect(effects, "status-layers", ally,
                    Math.Max(2, _engine.GetPhysicalDefense(state, actor)), detailId: "guard-oath");
                AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
                AddEffect(effects, "status-turns", actor, TurnDurationStatus.DefaultTurns, detailId: "fortify");
                AddEffect(effects, "shared-shield", actor,
                    Math.Max(0, _engine.GetPhysicalDefense(state, actor))
                    + Math.Max(0, _engine.GetMagicalDefense(state, actor)));
                break;
            }
            case "iron-charge":
                ForecastIronCharge(state, actor, RequireTarget(target), effects);
                break;
        }

        ForecastAstralAlignmentSplash(
            state, actor, target, effects, remainingShieldAfterMagicalDamage);
    }

    private void ForecastAstralAlignmentSplash(
        GameState state,
        CharacterState actor,
        CharacterState? target,
        List<RoleActionEffectForecast> effects,
        int? remainingShieldAfterMagicalDamage)
    {
        var alignment = actor.Statuses.OfType<AstralAlignmentStatus>()
            .FirstOrDefault(status => !status.Expired);
        if (alignment is null || target is null || remainingShieldAfterMagicalDamage is null)
            return;

        var targetOwner = state.FindOwner(target);
        var remainingShield = remainingShieldAfterMagicalDamage.Value;
        var splashBase = Math.Max(1, (int)Math.Ceiling(alignment.Magnitude / 2.0));

        foreach (var adjacent in targetOwner.Characters.Where(character =>
                     character.IsAlive
                     && !GameEngine.IsDeploying(character)
                     && Math.Abs(character.Slot - target.Slot) == 1))
        {
            var splash = _damagePreviews.ForecastCollateralDamage(
                state, actor, adjacent, splashBase, DamageType.Magical, remainingShield);
            AddDamage(effects, adjacent, splash);
            remainingShield = Math.Max(0, remainingShield - splash.ShieldAbsorb);
        }
    }

    private void ForecastCleansingHerbs(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<HealingForecast> healingForecasts)
    {
        if (CountDispellableDebuffs(target) <= 0)
            return;

        var maxHp = _engine.GetMaxHp(target);
        var first = Math.Min(1, Math.Max(0, maxHp - target.CurrentHp));
        var afterFirst = target.CurrentHp + first;
        var requested = 1;
        if (HeroRankRules.HasRank2Path(actor, "cleansing-herbs") && afterFirst * 2 < maxHp)
            requested++;
        var healing = Math.Min(requested, Math.Max(0, maxHp - target.CurrentHp));
        var fieldMedic = FindFieldMedic(state, actor);
        healing += ForecastFieldMedicBonusHealing(
            fieldMedic, target, target.CurrentHp + healing, didHealOrCleanse: true);
        AddHealing(effects, target, healing);
        TrackHealing(healingForecasts, target, requested,
            Math.Min(requested, Math.Max(0, maxHp - target.CurrentHp)));
        if (HeroRankRules.HasRank2Path(actor, "cleansing-herbs"))
            AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
        if (fieldMedic is not null)
            AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
    }

    private static void ForecastRoleActionRelics(
        GameState state,
        CharacterState actor,
        RoleActionMetadata metadata,
        string roleActionId,
        CharacterState? target,
        List<RoleActionEffectForecast> effects)
    {
        var owner = state.FindOwner(actor);
        var projectedActionPoints = Math.Max(0, state.ActionPoints - metadata.BaseApCost);
        if (roleActionId == "royal-command")
            projectedActionPoints += 2;
        if (roleActionId == "saintly-prayer"
            && target is not null
            && CountDispellableDebuffs(target) > 0)
            projectedActionPoints++;

        var refundAttempts = 0;
        if (actor.Definition.CardType == CardType.Soldier
            && RelicEffects.HasRelic(owner, "relic-command-sergeant-seal")
            && !owner.RelicsUsedThisTurn.Contains("relic-command-sergeant-seal"))
            refundAttempts++;
        if (metadata.BaseApCost == 2
            && RelicEffects.HasRelic(owner, "relic-command-table")
            && !owner.RelicsUsedThisTurn.Contains("relic-command-table"))
            refundAttempts++;

        var beforeRefunds = projectedActionPoints;
        for (var index = 0; index < refundAttempts; index++)
            projectedActionPoints = Math.Min(GameEngine.GetMaxActionPoints(owner), projectedActionPoints + 1);
        var refunded = projectedActionPoints - beforeRefunds;
        if (refunded > 0)
            AddEffect(effects, "action-points", actor, refunded, detailId: "relic-refund");
    }

    private void ForecastActiveHealingRelics(
        GameState state,
        CharacterState healer,
        IReadOnlyCollection<HealingForecast> healingForecasts,
        List<RoleActionEffectForecast> effects,
        List<LocalizedText> notes)
    {
        if (healingForecasts.Count == 0)
            return;

        var owner = state.FindOwner(healer);
        var combined = healingForecasts
            .Where(forecast => forecast.Requested > 0)
            .GroupBy(forecast => forecast.Target.Id)
            .Select(group => new HealingForecast(
                group.First().Target,
                group.Sum(forecast => forecast.Requested),
                group.Sum(forecast => forecast.Restored)))
            .ToArray();
        var highestActual = combined
            .Where(forecast => forecast.Restored > 0)
            .OrderByDescending(forecast => forecast.Restored)
            .ThenBy(forecast => forecast.Target.Slot)
            .FirstOrDefault();

        if (highestActual is not null
            && RelicEffects.HasRelic(owner, "relic-white-lily-censer")
            && !owner.RelicsUsedThisTurn.Contains("relic-white-lily-censer"))
        {
            AddEffect(effects, "status-turns", highestActual.Target,
                TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-white-lily-censer"))));
        }

        if (highestActual is not null
            && RelicEffects.HasRelic(owner, "relic-saint-chalice")
            && !owner.RelicsUsedThisTurn.Contains("relic-saint-chalice"))
        {
            var moraleRestored = Math.Min(
                highestActual.Restored,
                Math.Max(0, highestActual.Target.MaxMorale - highestActual.Target.Morale));
            if (moraleRestored > 0)
                AddEffect(effects, "morale-healing", highestActual.Target, moraleRestored);
            AddEffect(effects, "shared-shield", healer, highestActual.Restored + moraleRestored);
            notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-saint-chalice"))));
        }

        var highestOverheal = combined
            .Where(forecast => forecast.Overheal > 0)
            .OrderByDescending(forecast => forecast.Overheal)
            .ThenBy(forecast => forecast.Target.Slot)
            .FirstOrDefault();
        if (highestOverheal is null
            || !RelicEffects.HasRelic(owner, "relic-mercy-cup")
            || owner.RelicsUsedThisTurn.Contains("relic-mercy-cup"))
            return;

        var shield = Math.Min(highestOverheal.Overheal, _engine.GetActiveAttack(state, healer));
        if (shield <= 0)
            return;
        AddEffect(effects, "shared-shield", healer, shield);
        notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-mercy-cup"))));
    }

    private static void ForecastHpPaymentRelic(
        GameState state,
        CharacterState actor,
        int hpPaid,
        List<RoleActionEffectForecast> effects,
        List<LocalizedText> notes)
    {
        var owner = state.FindOwner(actor);
        if (hpPaid <= 0
            || !RelicEffects.HasRelic(owner, "relic-blood-coin")
            || owner.RelicsUsedThisTurn.Contains("relic-blood-coin"))
            return;

        AddEffect(effects, "shared-shield", actor, hpPaid);
        notes.Add(L10n.Text("preview.relicTrigger", ("relic", L10n.Reward("relic-blood-coin"))));
    }

    private void ForecastFieldWork(
        CharacterState actor,
        List<RoleActionEffectForecast> effects,
        List<HealingForecast> healingForecasts)
    {
        if (actor.Statuses.Any(status => status.Id == "harvest" && !status.Expired))
        {
            AddEffect(effects, "bonus-attacks", actor, Math.Max(0, 1 - actor.BonusAttackUsesThisTurn));
            return;
        }

        if (actor.Statuses.Any(status => status.Id == "harvest-pending" && !status.Expired))
        {
            var restored = RestoredHealing(actor, 2);
            AddHealing(effects, actor, restored);
            TrackHealing(healingForecasts, actor, 2, restored);
        }
        else
            AddEffect(effects, "status-layers", actor, 1, detailId: "harvest-pending");
    }

    private void ForecastMiracleStandard(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<HealingForecast> healingForecasts)
    {
        var affected = CharactersInArea(state.FindOwner(actor), target).ToArray();
        var fieldMedic = FindFieldMedic(state, actor);
        var mainHeal = (int)Math.Ceiling(_engine.GetMaxHp(actor) / 4.0);
        foreach (var ally in affected)
        {
            var requested = ally.Id == target.Id ? mainHeal : Math.Max(1, (int)Math.Ceiling(mainHeal / 2.0));
            var primary = RestoredHealing(ally, requested);
            var cleansed = CountDispellableDebuffs(ally) > 0;
            var didHealOrCleanse = primary > 0 || cleansed;
            var total = primary + ForecastFieldMedicBonusHealing(
                fieldMedic, ally, ally.CurrentHp + primary, didHealOrCleanse);
            AddHealing(effects, ally, total);
            TrackHealing(healingForecasts, ally, requested, primary);
            if (fieldMedic is not null && didHealOrCleanse)
                AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
        }

        var totalCleansed = CountDispellableDebuffs(target)
            + affected.Where(ally => ally.Id != target.Id).Count(ally => CountDispellableDebuffs(ally) > 0);
        if (totalCleansed > 0)
            AddEffect(effects, "shared-shield", actor,
                Math.Max(0, _engine.GetMagicalDefense(state, actor)) + affected.Length);
    }

    private int? ForecastThreadCut(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<LocalizedText> notes)
    {
        var count = CountThreadCutMarks(target);
        if (count <= 0)
        {
            AddEffect(effects, "status-layers", target, FateMarkedStatus.DamageBonus, detailId: "marked");
            notes.Add(L10n.Text("preview.roleAction.threadCutMark"));
            return null;
        }

        var total = GameEngine.ThreadCutMoraleDamagePerMark * count;
        var moraleDamage = Math.Min(Math.Max(0, target.Morale), total);
        var hpDamage = total - moraleDamage;
        AddEffect(effects, "morale-damage", target, moraleDamage);
        if (hpDamage > 0)
            AddEffect(effects, "hp-damage", target, hpDamage);

        if (target.Morale - moraleDamage <= 0 && target.CurrentHp - hpDamage > 0)
        {
            var magical = ForecastDamage(
                state,
                actor,
                target,
                _engine.GetActiveAttack(state, actor),
                DamageType.Magical,
                out var remainingShield,
                targetMoraleOverride: 0,
                targetHpOverride: target.CurrentHp - hpDamage);
            AddDamage(effects, target, magical);
            AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "trembling");
            return remainingShield;
        }

        return null;
    }

    private void ForecastFieldRations(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<HealingForecast> healingForecasts)
    {
        var owner = state.FindOwner(actor);
        var baseHeal = Math.Max(1, (int)Math.Ceiling(_engine.GetActiveAttack(state, actor) / 2.0));
        foreach (var ally in owner.Characters.Where(character => character.IsAlive && !GameEngine.IsDeploying(character)))
        {
            var requested = baseHeal + (ally.Id == target.Id ? _engine.GetActiveAttack(state, actor) : 0);
            var restored = RestoredHealing(ally, requested);
            AddHealing(effects, ally, restored);
            if (ally.Id == target.Id)
            {
                var baseRestored = RestoredHealing(ally, baseHeal);
                TrackHealing(healingForecasts, ally, baseHeal, baseRestored);
                TrackHealing(healingForecasts, ally, _engine.GetActiveAttack(state, actor), restored - baseRestored);
            }
            else
            {
                TrackHealing(healingForecasts, ally, baseHeal, restored);
            }
        }
        AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "fortify");
        AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
    }

    private void ForecastGroveSanctuary(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<HealingForecast> healingForecasts)
    {
        var affected = CharactersInArea(state.FindOwner(actor), target).ToArray();
        var totalCleansed = CountDispellableDebuffs(target)
            + affected.Where(ally => ally.Id != target.Id).Count(ally => CountDispellableDebuffs(ally) > 0);
        if (totalCleansed <= 0)
        {
            var layers = Math.Max(1, _engine.GetMagicalDefense(state, actor));
            foreach (var ally in affected)
                AddEffect(effects, "status-layers", ally, layers, detailId: "spell-ward");
            return;
        }

        var requested = _engine.GetActiveAttack(state, actor) * totalCleansed;
        var fieldMedic = FindFieldMedic(state, actor);
        foreach (var ally in affected)
        {
            var primary = RestoredHealing(ally, requested);
            var total = primary + ForecastFieldMedicBonusHealing(
                fieldMedic, ally, ally.CurrentHp + primary, didHealOrCleanse: primary > 0);
            AddHealing(effects, ally, total);
            TrackHealing(healingForecasts, ally, requested, primary);
            if (fieldMedic is not null && primary > 0)
                AddEffect(effects, "status-turns", ally, TurnDurationStatus.DefaultTurns, detailId: "spell-ward");
        }
    }

    private void ForecastDragonBreaker(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects,
        List<LocalizedText> notes)
    {
        var targetOwner = state.FindOwner(target);
        if (targetOwner.SharedShield > 0)
        {
            var shieldDamage = Math.Min(targetOwner.SharedShield, _engine.GetActiveAttack(state, actor));
            AddEffect(effects, "shared-shield-damage", target, shieldDamage);
            notes.Add(L10n.Text("preview.roleAction.dragonBreakerShield"));
            if (shieldDamage >= targetOwner.SharedShield)
            {
                foreach (var enemy in CharactersInArea(targetOwner, target))
                {
                    AddEffect(effects, "status-turns", enemy, TurnDurationStatus.DefaultTurns, detailId: "trembling");
                    AddEffect(effects, "status-turns", enemy, TurnDurationStatus.DefaultTurns, detailId: "vulnerable");
                }
            }
            return;
        }

        var damage = ForecastDamage(
            state, actor, target, _engine.GetActiveAttack(state, actor), DamageType.Physical);
        AddDamage(effects, target, damage);
        AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "trembling");
        if (damage.HpDamageMin < target.CurrentHp)
            AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "vulnerable");
    }

    private void ForecastIronCharge(
        GameState state,
        CharacterState actor,
        CharacterState target,
        List<RoleActionEffectForecast> effects)
    {
        var owner = state.FindOwner(actor);
        var consumed = owner.SharedShield;
        AddEffect(effects, "shared-shield-cost", actor, consumed);
        var damage = ForecastDamage(
            state,
            actor,
            target,
            consumed + _engine.GetActiveAttack(state, actor),
            DamageType.Physical);
        AddDamage(effects, target, damage);
        AddEffect(effects, "status-turns", target, TurnDurationStatus.DefaultTurns, detailId: "trembling");
        var primaryDamage = _damagePreviews.ForecastRoleActionPrimaryDamage(
            state,
            actor,
            target,
            consumed + _engine.GetActiveAttack(state, actor),
            DamageType.Physical);
        if (primaryDamage.HpDamageMax > 0)
            AddEffect(effects, "shared-shield", actor,
                primaryDamage.HpDamageMin, primaryDamage.HpDamageMax, "hp-damage-restored");
    }

    private DamageForecast ForecastDamage(
        GameState state,
        CharacterState actor,
        CharacterState target,
        int amount,
        DamageType damageType) =>
        ForecastDamage(state, actor, target, amount, damageType, out _);

    private DamageForecast ForecastDamage(
        GameState state,
        CharacterState actor,
        CharacterState target,
        int amount,
        DamageType damageType,
        out int remainingSharedShield,
        int? targetMoraleOverride = null,
        int? targetHpOverride = null) =>
        _damagePreviews.ForecastRoleActionDamage(
            state,
            actor,
            target,
            amount,
            damageType,
            out remainingSharedShield,
            targetMoraleOverride,
            targetHpOverride);

    private static IEnumerable<CharacterState> CharactersInArea(PlayerState owner, CharacterState center) =>
        owner.Characters.Where(character =>
            character.IsAlive
            && !GameEngine.IsDeploying(character)
            && Math.Abs(character.Slot - center.Slot) <= 1);

    private int RestoredHealing(CharacterState target, int requested) =>
        Math.Min(Math.Max(0, requested), Math.Max(0, _engine.GetMaxHp(target) - target.CurrentHp));

    private static CharacterState? FindFieldMedic(GameState state, CharacterState source) =>
        state.FindOwner(source).Characters.FirstOrDefault(character =>
            character.IsAlive
            && !GameEngine.IsDeploying(character)
            && character.Definition.TraitId == "field-medic");

    private int ForecastFieldMedicBonusHealing(
        CharacterState? fieldMedic,
        CharacterState target,
        int hpAfterPrimary,
        bool didHealOrCleanse)
    {
        if (!didHealOrCleanse
            || fieldMedic is null
            || fieldMedic.SoldierRank < 1
            || hpAfterPrimary * 2 >= _engine.GetMaxHp(target))
            return 0;

        return Math.Min(3, Math.Max(0, _engine.GetMaxHp(target) - hpAfterPrimary));
    }

    private static int CountDispellableDebuffs(CharacterState target) =>
        target.Statuses.Count(status => !status.Expired && !status.IsBuff && status.IsDispellable);

    private static int CountThreadCutMarks(CharacterState target) =>
        target.Statuses.Count(status => !status.Expired
            && status.Id is "marked" or "prey" or "nightmare-prey" or "hunted"
                or "burning" or "void" or "exhaustion" or "erosion" or "trembling" or "vulnerable");

    private static int CountCommonDebuffs(CharacterState target) =>
        target.Statuses.Count(status => !status.Expired && !status.IsBuff
            && status.Id is "burning" or "void" or "exhaustion" or "erosion" or "trembling" or "vulnerable");

    private static int BurningStacksAdded(GameState state, CharacterState actor, int baseStacks = 1)
    {
        var owner = state.FindOwner(actor);
        var amount = Math.Max(1, baseStacks);
        return RelicEffects.HasRelic(owner, "relic-ember-astrolabe")
            && !owner.RelicsUsedThisTurn.Contains("relic-ember-astrolabe")
                ? amount + 1
                : amount;
    }

    private static CharacterState RequireTarget(CharacterState? target) =>
        target ?? throw new InvalidOperationException("Validated role action target is missing.");

    private static void AddDamage(
        List<RoleActionEffectForecast> effects,
        CharacterState target,
        DamageForecast damage) =>
        effects.Add(new RoleActionEffectForecast(
            "damage",
            target.Id,
            damage.Min,
            damage.Max,
            damage.DamageType,
            damage));

    private static void AddHealing(
        List<RoleActionEffectForecast> effects,
        CharacterState target,
        int amount) => AddEffect(effects, "healing", target, amount);

    private static void TrackHealing(
        List<HealingForecast> forecasts,
        CharacterState target,
        int requested,
        int restored) =>
        forecasts.Add(new HealingForecast(target, Math.Max(0, requested), Math.Max(0, restored)));

    private static void AddEffect(
        List<RoleActionEffectForecast> effects,
        string kind,
        CharacterState target,
        int value,
        string? detailId = null) =>
        AddEffect(effects, kind, target, value, value, detailId);

    private static void AddEffect(
        List<RoleActionEffectForecast> effects,
        string kind,
        CharacterState target,
        int min,
        int max,
        string? detailId = null) =>
        effects.Add(new RoleActionEffectForecast(kind, target.Id, min, max, detailId));

    private static RoleActionPreview Invalid(
        Guid actorId,
        Guid? targetId,
        string roleActionId,
        LocalizedText error) =>
        new(false, error, actorId, targetId, roleActionId, 0, [], []);
}
