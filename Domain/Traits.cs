namespace TinyPixelFights.Domain;

public sealed record TraitMetadata(
    string Id,
    TraitTriggerKind TriggerKind,
    TraitScopeKind ScopeKind,
    TraitEffectKind EffectKind);

public sealed class DamagePacket
{
    public required CharacterState SourceCharacter { get; init; }
    public required CharacterState TargetCharacter { get; set; }
    public required DamageType DamageType { get; init; }
    public required DamageSource Source { get; init; }
    public required int Amount { get; set; }
    public bool ReceivesMagicPowerBonus { get; init; }
    public bool CanConsumeChargeStatuses { get; init; } = true;
    public bool ConsumedChant { get; set; }
    public bool PredatorCrownApplied { get; set; }
    public bool IgnoresSharedShield { get; set; }
    public bool IgnoresTargetDefense { get; set; }
    public bool BlockedBySharedShield { get; set; }
    public int ShieldDefenseReduced { get; set; }
    public int DefenseReduced { get; set; }
    public int ShieldAbsorbed { get; set; }
    public int FinalCharacterDamage { get; set; }
    public int MoraleDamage { get; set; }
    public int HpDamage { get; set; }
    public List<CollateralDamage> Collateral { get; } = [];
    public List<LocalizedText> Notes { get; } = [];
}

public sealed record CollateralDamage(
    CharacterState Source,
    CharacterState Target,
    int Amount,
    DamageType DamageType,
    string EffectId);

public sealed record AttackExchange(
    CharacterState Attacker,
    CharacterState Defender,
    int AttackDamageDealt,
    int CounterDamageDealt);

public abstract class CharacterTrait
{
    public abstract TraitMetadata Metadata { get; }
    public virtual int IncomingModifierPriority => 100;

    public virtual bool IsReady(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive || state.Phase != GamePhase.Playing)
            return false;
        if (GameEngine.IsDeploying(owner))
            return false;

        return true;
    }

    public virtual LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive) return L10n.Text("reason.defeated");
        if (GameEngine.IsDeploying(owner)) return L10n.Text("reason.deploying");
        if (state.Phase == GamePhase.Finished) return L10n.Text("reason.matchFinished");
        return null;
    }

    public virtual void OnTurnStart(GameEngineContext context, CharacterState owner) { }
    public virtual void OnAttackDeclared(GameEngineContext context, CharacterState owner, CharacterState target) { }
    public virtual void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange) { }
    public virtual void OnAllyDefeated(GameEngineContext context, CharacterState owner, CharacterState defeatedAlly) { }
    public virtual void OnCharacterDefeated(GameEngineContext context, CharacterState owner, CharacterState defeatedCharacter)
    {
        if (owner.PlayerId == defeatedCharacter.PlayerId)
            OnAllyDefeated(context, owner, defeatedCharacter);
    }
}

internal static class HeroRankRules
{
    public static bool HasRank2Path(CharacterState owner, string baseRoleActionId) =>
        owner.Definition.CardType == CardType.Hero
        && owner.HeroRank >= 2
        && string.Equals(owner.HeroPathRoleActionId, baseRoleActionId, StringComparison.OrdinalIgnoreCase);
}

public sealed class SaintsPrayerTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "saints-prayer",
        TraitTriggerKind.TurnStart,
        TraitScopeKind.Team,
        TraitEffectKind.Heal);

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (!owner.IsAlive || owner.PlayerId != context.State.ActivePlayerId)
            return;

        foreach (var ally in context.State.FindOwner(owner).Characters
                     .Where(character => character.IsAlive && !GameEngine.IsDeploying(character)))
        {
            var cap = context.GetMaxHp(ally) + 2;
            if (ally.CurrentHp >= cap)
                continue;

            ally.CurrentHp++;
            context.Log(L10n.Text("log.healed",
                ("effect", L10n.Status("blessing")),
                ("character", L10n.Character(ally.Definition.Key)),
                ("characterId", L10n.Raw(ally.Id)),
                ("amount", L10n.Raw(1))), "heal");
        }

        if (HeroRankRules.HasRank2Path(owner, "royal-command"))
        {
            var target = context.State.FindOwner(owner).Characters
                .Where(character => character.IsAlive && !GameEngine.IsDeploying(character))
                .OrderBy(character => context.GetMaxHp(character) == 0 ? 1 : (double)character.CurrentHp / context.GetMaxHp(character))
                .ThenBy(character => character.CurrentHp)
                .FirstOrDefault();
            if (target is not null && target.CurrentHp < context.GetMaxHp(target) + 2)
            {
                target.CurrentHp++;
                context.Log(L10n.Text("log.healed",
                    ("effect", L10n.Trait("saints-prayer")),
                    ("character", L10n.Character(target.Definition.Key)),
                    ("characterId", L10n.Raw(target.Id)),
                    ("amount", L10n.Raw(1))), "heal");
            }
        }
    }
}

public sealed class StargazersAegisTrait : CharacterTrait
{
    public override int IncomingModifierPriority => 10;
    public override TraitMetadata Metadata { get; } = new(
        "stargazers-aegis",
        TraitTriggerKind.Continuous,
        TraitScopeKind.Team,
        TraitEffectKind.DamageModifier);

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        var hasMagicPower = packet.SourceCharacter.Statuses.Any(status =>
            !status.Expired && status.Id == "magic-power" && status.SourceCharacterId == owner.Id);
        if (packet.DamageType == DamageType.Magical && packet.ReceivesMagicPowerBonus && hasMagicPower)
        {
            packet.Amount++;
            packet.Notes.Add(L10n.Text("note.magicBonus",
                ("effect", L10n.Trait("stargazers-aegis")),
                ("character", L10n.Character(owner.Definition.Key)),
                ("characterId", L10n.Raw(owner.Id)),
                ("amount", L10n.Raw(1))));
        }
    }

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (packet.Amount <= 0)
            return;

        if (!context.Roll(0.30))
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(0, (int)Math.Ceiling(packet.Amount * 0.5));
        packet.Notes.Add(L10n.Text("note.foresightReduction",
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("characterId", L10n.Raw(packet.TargetCharacter.Id)),
            ("damageType", L10n.Damage(packet.DamageType)),
            ("amount", L10n.Raw(before - packet.Amount))));

        if (HeroRankRules.HasRank2Path(owner, "star-reading")
            && owner.TraitsUsedThisTurn.Add("stargazers-aegis-rank2"))
        {
            if (GameEngine.GetAttackType(packet.TargetCharacter) == DamageType.Magical)
                packet.TargetCharacter.Statuses.Add(new ChantStatus(owner.Id));
            else
                GameEngine.AddSpellWard(packet.TargetCharacter, owner.Id);
        }
    }
}

public sealed class SpringHarvestTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "spring-harvest",
        TraitTriggerKind.OnAttackDeclared,
        TraitScopeKind.Self,
        TraitEffectKind.Status);

    public override bool IsReady(GameState state, CharacterState owner) =>
        base.IsReady(state, owner)
        && owner.PlayerId == state.ActivePlayerId
        && !owner.HasActed
        && owner.Definition.Cost <= state.ActionPoints
        && state.ActiveAttacksTakenThisTurn == 0
        && owner.Statuses.All(status => status.Id != "harvest" || status.Expired);

    public override LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive) return L10n.Text("reason.defeated");
        if (GameEngine.IsDeploying(owner)) return L10n.Text("reason.deploying");
        if (state.Phase == GamePhase.Finished) return L10n.Text("reason.matchFinished");
        if (owner.PlayerId != state.ActivePlayerId) return L10n.Text("reason.opponentTurn");
        if (owner.HasActed) return L10n.Text("reason.alreadyActed");
        if (owner.Definition.Cost > state.ActionPoints) return L10n.Text("reason.notEnoughAp");
        if (owner.Statuses.Any(status => status.Id == "harvest" && !status.Expired))
            return L10n.Text("reason.harvestActive");
        return state.ActiveAttacksTakenThisTurn == 0 ? null : L10n.Text("reason.notFirstAttack");
    }

    public override void OnAttackDeclared(GameEngineContext context, CharacterState owner, CharacterState target)
    {
        if (context.State.ActiveAttacksTakenThisTurn != 0
            || owner.Statuses.Any(status => status.Id == "harvest" && !status.Expired))
            return;

        owner.Statuses.RemoveAll(status => status.Id == "harvest-pending");
        owner.Statuses.Add(new PendingHarvestStatus(owner.Id, owner.PlayerId));
        if (HeroRankRules.HasRank2Path(owner, "supply-basket"))
        {
            var lowHpAlly = context.State.FindOwner(owner).Characters
                .Where(character => character.IsAlive && !GameEngine.IsDeploying(character))
                .OrderBy(character => context.GetMaxHp(character) == 0 ? 1 : (double)character.CurrentHp / context.GetMaxHp(character))
                .ThenBy(character => character.CurrentHp)
                .FirstOrDefault();
            if (lowHpAlly is not null)
                GameEngine.AddFortify(lowHpAlly, owner.Id);
        }
        context.Log(L10n.Text("log.sowing",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id))), "buff");
    }

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!HeroRankRules.HasRank2Path(owner, "field-work")
            || exchange.AttackDamageDealt <= 0
            || owner.TraitsUsedThisTurn.Contains("spring-harvest-rank2-bp")
            || owner.Statuses.All(status => status.Id != "harvest" || status.Expired))
            return;

        context.GainBattlePoint(context.State.FindOwner(owner), 1, "own-turn-enemy-hp-damage");
        owner.TraitsUsedThisTurn.Add("spring-harvest-rank2-bp");
    }
}

public sealed class SearingMarkTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "searing-mark",
        TraitTriggerKind.OnAttackResolved,
        TraitScopeKind.Enemy,
        TraitEffectKind.Status);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!exchange.Defender.IsAlive)
            return;

        var guaranteed = HeroRankRules.HasRank2Path(owner, "arcane-channel");
        if (!guaranteed && !context.Roll(0.50))
            return;

        var burningWasNew = GameEngine.AddBurning(exchange.Defender, owner.Id, state: context.State);
        context.NotifyDebuffApplied(owner, exchange.Defender, "burning", burningWasNew);
        if (!exchange.Defender.IsAlive)
            return;
        if (HeroRankRules.HasRank2Path(owner, "searing-brand"))
        {
            var voidWasNew = GameEngine.AddVoid(exchange.Defender, owner.Id);
            context.NotifyDebuffApplied(owner, exchange.Defender, "void", voidWasNew);
        }
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(exchange.Defender.Definition.Key)),
            ("characterId", L10n.Raw(exchange.Defender.Id)),
            ("status", L10n.Status("burning"))), "magic");
    }
}

public sealed class WeakeningSporesTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "weakening-spores",
        TraitTriggerKind.OnAttackResolved,
        TraitScopeKind.Enemy,
        TraitEffectKind.Dispel);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!exchange.Defender.IsAlive)
            return;

        var chance = exchange.AttackDamageDealt > 0 ? 1.0 : 0.50;
        if (!context.Roll(chance))
        {
            context.Log(L10n.Text("log.traitFailed",
                ("trait", L10n.Trait("weakening-spores")),
                ("character", L10n.Character(owner.Definition.Key)),
                ("characterId", L10n.Raw(owner.Id))), "status");
            return;
        }

        var attackBuffs = exchange.Defender.Statuses
            .Where(status => !status.Expired
                && status.IsBuff
                && status.IsDispellable)
            .ToList();
        var attackBuff = attackBuffs.Count == 0
            ? null
            : attackBuffs[context.Next(attackBuffs.Count)];
        var removedBuff = attackBuff is not null;
        if (attackBuff is not null)
        {
            exchange.Defender.Statuses.Remove(attackBuff);
            context.Log(L10n.Text("log.attackBuffRemoved",
                ("character", L10n.Character(exchange.Defender.Definition.Key)),
                ("characterId", L10n.Raw(exchange.Defender.Id)),
                ("status", L10n.Status(attackBuff.Id))), "status");
        }

        var exhaustionWasNew = GameEngine.AddExhaustion(exchange.Defender, owner.Id);
        context.NotifyDebuffApplied(owner, exchange.Defender, "exhaustion", exhaustionWasNew);
        if (!exchange.Defender.IsAlive)
            return;
        var erosionWasNew = GameEngine.AddErosion(exchange.Defender, owner.Id);
        context.NotifyDebuffApplied(owner, exchange.Defender, "erosion", erosionWasNew);
        if (!exchange.Defender.IsAlive)
            return;
        if (!removedBuff && HeroRankRules.HasRank2Path(owner, "weakening-spores-action"))
        {
            if (GameEngine.GetAttackType(exchange.Defender) == DamageType.Physical)
            {
                var vulnerableWasNew = GameEngine.AddVulnerable(exchange.Defender, owner.Id);
                context.NotifyDebuffApplied(owner, exchange.Defender, "vulnerable", vulnerableWasNew);
            }
            else
            {
                var voidWasNew = GameEngine.AddVoid(exchange.Defender, owner.Id);
                context.NotifyDebuffApplied(owner, exchange.Defender, "void", voidWasNew);
            }
        }
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(exchange.Defender.Definition.Key)),
            ("characterId", L10n.Raw(exchange.Defender.Id)),
            ("status", L10n.Status("exhaustion"))), "status");
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(exchange.Defender.Definition.Key)),
            ("characterId", L10n.Raw(exchange.Defender.Id)),
            ("status", L10n.Status("erosion"))), "status");
    }
}

public sealed class AftershockAxeTrait : CharacterTrait
{
    public const int TriggerDamage = 3;

    public override TraitMetadata Metadata { get; } = new(
        "aftershock-axe",
        TraitTriggerKind.OnAttackResolved,
        TraitScopeKind.EnemyTeam,
        TraitEffectKind.Damage);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        var ragingRank2 = HeroRankRules.HasRank2Path(owner, "war-cry")
            && owner.Statuses.Any(status => status.Id == "rage" && !status.Expired);
        if (exchange.AttackDamageDealt < TriggerDamage && !ragingRank2)
            return;

        var defenderOwner = context.State.FindOwner(exchange.Defender);
        var neighbours = defenderOwner.Characters
            .Where(character => character.IsAlive
                && !GameEngine.IsDeploying(character)
                && Math.Abs(character.Slot - exchange.Defender.Slot) == 1)
            .ToList();

        if (neighbours.Count == 0)
            return;

        var damage = Math.Max(ragingRank2 ? 2 : 1, (int)Math.Ceiling(exchange.AttackDamageDealt / 3.0));
        foreach (var target in neighbours)
        {
            context.DealTraitDamage(target, damage, DamageType.Physical, owner.Id, "aftershock-axe");
            if (target.IsAlive && HeroRankRules.HasRank2Path(owner, "challenge"))
            {
                var tremblingWasNew = GameEngine.AddTrembling(target, owner.Id);
                context.NotifyDebuffApplied(owner, target, "trembling", tremblingWasNew);
            }
        }
    }
}

public sealed class PredatoryInstinctTrait : CharacterTrait
{
    public const int PrincessBonusDamage = 1;
    public const int PrincessBacklashMultiplier = 2;

    public override TraitMetadata Metadata { get; } = new(
        "predatory-instinct",
        TraitTriggerKind.OnAttackResolved,
        TraitScopeKind.Enemy,
        TraitEffectKind.Damage);

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (packet.Source != DamageSource.ActiveAttack
            || packet.SourceCharacter.Id != owner.Id
            || packet.TargetCharacter.Definition.Key != "princess")
            return;

        packet.IgnoresSharedShield = true;
        packet.IgnoresTargetDefense = true;
    }

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (exchange.Defender.Definition.Key == "princess")
        {
            DealPrincessBacklash(context, owner, context.GetActiveAttack(owner));
            return;
        }

        if (!exchange.Defender.IsAlive
            || exchange.AttackDamageDealt != 0)
            return;

        var hasPrincess = context.State.FindOwner(owner).Characters.Any(character =>
            character.IsAlive && !GameEngine.IsDeploying(character) && character.Definition.Key == "princess");
        var preyBonus = HeroRankRules.HasRank2Path(owner, "predatory-gaze")
            && exchange.Defender.Statuses.Any(status => status.Id is "prey" or "nightmare-prey" && !status.Expired)
                ? 1
                : 0;
        var damage = context.GetActiveAttack(owner) + (hasPrincess ? PrincessBonusDamage : 0) + preyBonus;
        context.DealAbsoluteDamage(exchange.Defender, damage, owner.Id, "predatory-instinct");
    }

    private static void DealPrincessBacklash(GameEngineContext context, CharacterState owner, int princessDamage)
    {
        var requested = Math.Max(0, princessDamage * PrincessBacklashMultiplier);
        if (requested <= 0)
            return;

        var dealt = Math.Min(requested, Math.Max(0, owner.CurrentHp - 1));
        if (dealt <= 0)
            return;

        owner.CurrentHp -= dealt;
        context.Log(L10n.Text("log.effectDamage",
            ("effect", L10n.Trait("predatory-instinct")),
            ("source", L10n.Character(owner.Definition.Key)),
            ("sourceId", L10n.Raw(owner.Id)),
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("amount", L10n.Raw(dealt)),
            ("damageType", L10n.Damage(DamageType.Absolute))), "trait");
    }

    public override void OnCharacterDefeated(
        GameEngineContext context,
        CharacterState owner,
        CharacterState defeatedCharacter)
    {
        if (defeatedCharacter.Definition.Key != "princess"
            || owner.Statuses.Any(status => status.Id == "beast-rage" && !status.Expired))
            return;

        owner.Statuses.Add(new BeastRageStatus(defeatedCharacter.Id));
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("status", L10n.Status("beast-rage"))), "buff");
    }
}

public sealed class InterposingShieldTrait : CharacterTrait
{
    public override int IncomingModifierPriority => 20;
    public override TraitMetadata Metadata { get; } = new(
        "interposing-shield",
        TraitTriggerKind.OnDamaged,
        TraitScopeKind.Ally,
        TraitEffectKind.Shield);

    public override bool IsReady(GameState state, CharacterState owner) =>
        base.IsReady(state, owner) && !owner.GuardConsumed;

    public override LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        var reason = base.UnavailableReason(state, owner);
        if (reason is not null) return reason;
        return owner.GuardConsumed ? L10n.Text("reason.guardConsumed") : null;
    }

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (owner.GuardConsumed
            || packet.Amount <= 0
            || packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Physical
            || packet.TargetCharacter.Id == owner.Id)
            return;

        var guardDamage = Math.Max(1, (int)Math.Ceiling(packet.Amount / 3.0));
        packet.Amount = Math.Max(0, packet.Amount - guardDamage);
        packet.Collateral.Add(new CollateralDamage(owner, owner, guardDamage, DamageType.Physical, "guard"));
        packet.Notes.Add(L10n.Text("note.guardRedirect",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("target", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("targetId", L10n.Raw(packet.TargetCharacter.Id)),
            ("amount", L10n.Raw(guardDamage))));
        if (HeroRankRules.HasRank2Path(owner, "guard-oath"))
            GameEngine.AddFortify(packet.TargetCharacter, owner.Id);
        owner.GuardConsumed = true;
    }
}

public sealed class FieldMedicTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "field-medic",
        TraitTriggerKind.ManualCheck,
        TraitScopeKind.Ally,
        TraitEffectKind.Status);
}

public sealed class ShieldDrillTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "shield-drill",
        TraitTriggerKind.ManualCheck,
        TraitScopeKind.Ally,
        TraitEffectKind.Status);
}

public sealed class DuelSenseTrait : CharacterTrait
{
    public const int AbsoluteDamage = 2;

    public override TraitMetadata Metadata { get; } = new(
        "duel-sense",
        TraitTriggerKind.OnAttackResolved,
        TraitScopeKind.Self,
        TraitEffectKind.Status);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (owner.PlayerId != context.State.ActivePlayerId
            || exchange.Attacker.Id != owner.Id
            || exchange.Defender.PlayerId == owner.PlayerId)
            return;

        if (exchange.AttackDamageDealt > 0 && owner.TraitsUsedThisTurn.Add(Metadata.Id))
        {
            GameEngine.AddStrongAttack(owner, owner.Id);
            context.Log(L10n.Text("log.statusApplied",
                ("character", L10n.Character(owner.Definition.Key)),
                ("characterId", L10n.Raw(owner.Id)),
                ("status", L10n.Status("strong-attack"))), "buff");
        }

        if (owner.SoldierRank >= 1
            && exchange.Defender.IsAlive
            && owner.TraitsUsedThisTurn.Add("duel-sense-absolute"))
            context.DealAbsoluteDamage(exchange.Defender, AbsoluteDamage, owner.Id, "duel-sense");
    }
}

public sealed class ArcaneResonanceTrait : CharacterTrait
{
    public override TraitMetadata Metadata { get; } = new(
        "arcane-resonance",
        TraitTriggerKind.ManualCheck,
        TraitScopeKind.Self,
        TraitEffectKind.Status);
}

public sealed class TraitRegistry
{
    private readonly IReadOnlyDictionary<string, CharacterTrait> _traits;

    public TraitRegistry()
    {
        CharacterTrait[] traits =
        [
            new SaintsPrayerTrait(),
            new StargazersAegisTrait(),
            new SpringHarvestTrait(),
            new SearingMarkTrait(),
            new WeakeningSporesTrait(),
            new AftershockAxeTrait(),
            new PredatoryInstinctTrait(),
            new InterposingShieldTrait(),
            new FieldMedicTrait(),
            new ShieldDrillTrait(),
            new DuelSenseTrait(),
            new ArcaneResonanceTrait()
        ];

        _traits = traits.ToDictionary(trait => trait.Metadata.Id);
    }

    public CharacterTrait Get(string id) => _traits[id];
}
