namespace TinyPixelFights.Domain;

public sealed record SkillMetadata(
    string Id,
    SkillKind Kind);

public sealed class DamagePacket
{
    public required CharacterState SourceCharacter { get; init; }
    public required CharacterState TargetCharacter { get; init; }
    public required DamageType DamageType { get; init; }
    public required DamageSource Source { get; init; }
    public required int Amount { get; set; }
    public bool ReceivesMagicPowerBonus { get; init; }
    public int ShieldAbsorbed { get; set; }
    public List<CollateralDamage> Collateral { get; } = [];
    public List<LocalizedText> Notes { get; } = [];
}

public sealed record CollateralDamage(
    CharacterState Target,
    int Amount,
    DamageType DamageType,
    string EffectId);

public sealed record AttackExchange(
    CharacterState Attacker,
    CharacterState Defender,
    int AttackDamageDealt,
    int CounterDamageDealt);

public abstract class CharacterSkill
{
    public abstract SkillMetadata Metadata { get; }
    public virtual int IncomingModifierPriority => 100;

    public virtual bool IsReady(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive || state.Phase != GamePhase.Playing)
            return false;

        if (Metadata.Kind == SkillKind.Passive)
            return true;

        return owner.PlayerId == state.ActivePlayerId
            && !owner.HasActed
            && owner.Definition.Cost <= state.ActionPoints;
    }

    public virtual LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        if (!owner.IsAlive) return L10n.Text("reason.defeated");
        if (state.Phase == GamePhase.Finished) return L10n.Text("reason.matchFinished");
        if (Metadata.Kind == SkillKind.Passive) return null;
        if (owner.PlayerId != state.ActivePlayerId) return L10n.Text("reason.opponentTurn");
        if (owner.HasActed) return L10n.Text("reason.alreadyActed");
        if (owner.Definition.Cost > state.ActionPoints) return L10n.Text("reason.notEnoughAp");
        return null;
    }

    public virtual void OnTurnStart(GameEngineContext context, CharacterState owner) { }
    public virtual void OnAttackDeclared(GameEngineContext context, CharacterState owner, CharacterState target) { }
    public virtual void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange) { }
    public virtual void OnAllyDefeated(GameEngineContext context, CharacterState owner, CharacterState defeatedAlly) { }
}

public sealed class SaintsPrayerSkill : CharacterSkill
{
    public override SkillMetadata Metadata { get; } = new(
        "saints-prayer",
        SkillKind.Passive);

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (!owner.IsAlive || owner.PlayerId != context.State.ActivePlayerId)
            return;

        foreach (var ally in context.State.FindOwner(owner).Characters.Where(character => character.IsAlive))
        {
            var cap = ally.Definition.MaxHp + 2;
            if (ally.CurrentHp >= cap)
                continue;

            ally.CurrentHp++;
            context.Log(L10n.Text("log.healed",
                ("effect", L10n.Status("blessing")),
                ("character", L10n.Character(ally.Definition.Key)),
                ("amount", L10n.Raw(1))), "heal");
        }
    }
}

public sealed class StargazersAegisSkill : CharacterSkill
{
    public override int IncomingModifierPriority => 10;
    public override SkillMetadata Metadata { get; } = new(
        "stargazers-aegis",
        SkillKind.Passive);

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        var hasMagicPower = packet.SourceCharacter.Statuses.Any(status =>
            !status.Expired && status.Id == "magic-power" && status.SourceCharacterId == owner.Id);
        if (packet.DamageType == DamageType.Magical && packet.ReceivesMagicPowerBonus && hasMagicPower)
        {
            packet.Amount++;
            packet.Notes.Add(L10n.Text("note.magicBonus",
                ("effect", L10n.Skill("stargazers-aegis")),
                ("character", L10n.Character(owner.Definition.Key)),
                ("amount", L10n.Raw(1))));
        }
    }

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (packet.Amount <= 0)
            return;

        var chance = packet.DamageType == DamageType.Physical ? 0.25 : 0.50;
        if (!context.Roll(chance))
            return;

        packet.Amount = Math.Max(0, packet.Amount - 1);
        packet.Notes.Add(L10n.Text("note.foresightReduction",
            ("character", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("damageType", L10n.Damage(packet.DamageType)),
            ("amount", L10n.Raw(1))));
    }
}

public sealed class SpringHarvestSkill : CharacterSkill
{
    public override SkillMetadata Metadata { get; } = new(
        "spring-harvest",
        SkillKind.Passive);

    public override bool IsReady(GameState state, CharacterState owner) =>
        base.IsReady(state, owner)
        && owner.PlayerId == state.ActivePlayerId
        && !owner.HasActed
        && owner.Definition.Cost <= state.ActionPoints
        && state.ActionsTakenThisTurn == 0
        && owner.Statuses.All(status => status.Id != "harvest" || status.Expired);

    public override LocalizedText? UnavailableReason(GameState state, CharacterState owner)
    {
        var baseReason = base.UnavailableReason(state, owner);
        if (baseReason is not null) return baseReason;
        if (owner.Statuses.Any(status => status.Id == "harvest" && !status.Expired))
            return L10n.Text("reason.harvestActive");
        return state.ActionsTakenThisTurn == 0 ? null : L10n.Text("reason.notFirstAttack");
    }

    public override void OnAttackDeclared(GameEngineContext context, CharacterState owner, CharacterState target)
    {
        if (context.State.ActionsTakenThisTurn != 0
            || owner.Statuses.Any(status => status.Id == "harvest" && !status.Expired))
            return;

        owner.Statuses.RemoveAll(status => status.Id == "harvest-pending");
        owner.Statuses.Add(new PendingHarvestStatus(owner.Id, owner.PlayerId));
        context.Log(L10n.Text("log.sowing",
            ("character", L10n.Character(owner.Definition.Key))), "buff");
    }
}

public sealed class SearingMarkSkill : CharacterSkill
{
    public override SkillMetadata Metadata { get; } = new(
        "searing-mark",
        SkillKind.Active);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!exchange.Defender.IsAlive || !context.Roll(0.50))
            return;

        exchange.Defender.Statuses.RemoveAll(status => status.Id == "burning");
        exchange.Defender.Statuses.Add(new BurningStatus(owner.Id, exchange.Defender.PlayerId));
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(exchange.Defender.Definition.Key)),
            ("status", L10n.Status("burning"))), "magic");
    }
}

public sealed class WeakeningSporesSkill : CharacterSkill
{
    public override SkillMetadata Metadata { get; } = new(
        "weakening-spores",
        SkillKind.Active);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!exchange.Defender.IsAlive)
            return;

        var chance = exchange.AttackDamageDealt > 0 ? 1.0 : 0.50;
        if (!context.Roll(chance))
        {
            context.Log(L10n.Text("log.skillFailed",
                ("skill", L10n.Skill("weakening-spores")),
                ("character", L10n.Character(owner.Definition.Key))), "status");
            return;
        }

        exchange.Defender.Statuses.RemoveAll(status =>
            status.Id is "weakness" or "weakness-pending");

        var attackBuffs = exchange.Defender.Statuses
            .Where(status => !status.Expired
                && status.IsBuff
                && status.IsDispellable)
            .ToList();
        var attackBuff = attackBuffs.Count == 0
            ? null
            : attackBuffs[context.Next(attackBuffs.Count)];
        if (attackBuff is not null)
        {
            exchange.Defender.Statuses.Remove(attackBuff);
            context.Log(L10n.Text("log.attackBuffRemoved",
                ("character", L10n.Character(exchange.Defender.Definition.Key)),
                ("status", L10n.Status(attackBuff.Id))), "status");
        }

        exchange.Defender.Statuses.Add(new PendingWeaknessStatus(owner.Id, exchange.Defender.PlayerId));
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(exchange.Defender.Definition.Key)),
            ("status", L10n.Status("weakness-pending"))), "status");
    }
}

public sealed class AftershockAxeSkill : CharacterSkill
{
    public const int TriggerDamage = 3;

    public override SkillMetadata Metadata { get; } = new(
        "aftershock-axe",
        SkillKind.Active);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (exchange.AttackDamageDealt < TriggerDamage)
            return;

        var defenderOwner = context.State.FindOwner(exchange.Defender);
        var neighbours = defenderOwner.Characters
            .Where(character => character.IsAlive && Math.Abs(character.Slot - exchange.Defender.Slot) == 1)
            .ToList();

        if (neighbours.Count == 0)
            return;

        var target = neighbours[context.Next(neighbours.Count)];
        context.DealSkillDamage(target, 1, DamageType.Physical, owner.Id, "aftershock-axe");
    }
}

public sealed class PredatoryInstinctSkill : CharacterSkill
{
    public const int AbsoluteDamage = 3;
    public const int PrincessBonusDamage = 1;

    public override SkillMetadata Metadata { get; } = new(
        "predatory-instinct",
        SkillKind.Active);

    public override void OnAfterExchange(GameEngineContext context, CharacterState owner, AttackExchange exchange)
    {
        if (!exchange.Defender.IsAlive
            || exchange.Defender.Definition.Key == "princess"
            || exchange.AttackDamageDealt != 0)
            return;

        var hasPrincess = context.State.FindOwner(owner).Characters.Any(character =>
            character.IsAlive && character.Definition.Key == "princess");
        var damage = AbsoluteDamage + (hasPrincess ? PrincessBonusDamage : 0);
        context.DealAbsoluteDamage(exchange.Defender, damage, owner.Id, "predatory-instinct");
    }

    public override void OnAllyDefeated(
        GameEngineContext context,
        CharacterState owner,
        CharacterState defeatedAlly)
    {
        if (defeatedAlly.Definition.Key != "princess"
            || owner.Statuses.Any(status => status.Id == "beast-rage" && !status.Expired))
            return;

        owner.Statuses.Add(new BeastRageStatus(defeatedAlly.Id));
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(owner.Definition.Key)),
            ("status", L10n.Status("beast-rage"))), "buff");
    }
}

public sealed class InterposingShieldSkill : CharacterSkill
{
    public override int IncomingModifierPriority => 20;
    public override SkillMetadata Metadata { get; } = new(
        "interposing-shield",
        SkillKind.Passive);

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

        packet.Amount--;
        packet.Collateral.Add(new CollateralDamage(owner, 1, DamageType.Physical, "guard"));
        packet.Notes.Add(L10n.Text("note.guardRedirect",
            ("character", L10n.Character(owner.Definition.Key)),
            ("target", L10n.Character(packet.TargetCharacter.Definition.Key)),
            ("amount", L10n.Raw(1))));
        owner.GuardConsumed = true;
    }
}

public sealed class SkillRegistry
{
    private readonly IReadOnlyDictionary<string, CharacterSkill> _skills;

    public SkillRegistry()
    {
        CharacterSkill[] skills =
        [
            new SaintsPrayerSkill(),
            new StargazersAegisSkill(),
            new SpringHarvestSkill(),
            new SearingMarkSkill(),
            new WeakeningSporesSkill(),
            new AftershockAxeSkill(),
            new PredatoryInstinctSkill(),
            new InterposingShieldSkill()
        ];

        _skills = skills.ToDictionary(skill => skill.Metadata.Id);
    }

    public CharacterSkill Get(string id) => _skills[id];
}
