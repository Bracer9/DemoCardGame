namespace TinyPixelFights.Domain;

public abstract class StatusEffect
{
    protected StatusEffect(string id, bool isBuff, Guid sourceCharacterId)
    {
        Id = id;
        IsBuff = isBuff;
        SourceCharacterId = sourceCharacterId;
    }

    public string Id { get; }
    public bool IsBuff { get; }
    public Guid SourceCharacterId { get; }
    public virtual int Magnitude => 0;
    public virtual bool IsAttackBuff => false;
    public virtual bool IsDispellable => true;
    public bool Expired { get; protected set; }

    public virtual void OnTurnStart(GameEngineContext context, CharacterState owner) { }
    public virtual void OnTurnEnd(GameEngineContext context, CharacterState owner) { }
    public virtual int ModifyBaseAttack(int attack) => attack;
    public virtual int ModifyActiveAttack(int damage) => damage;
}

public sealed class BeastRageStatus(Guid sourceCharacterId)
    : StatusEffect("beast-rage", true, sourceCharacterId)
{
    public override int Magnitude => 2;
    public override bool IsAttackBuff => true;
    public override bool IsDispellable => false;
    public override int ModifyBaseAttack(int attack) => attack + Magnitude;
}

public sealed class MagicPowerStatus(Guid sourceCharacterId)
    : StatusEffect("magic-power", true, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsAttackBuff => true;
}

public sealed class BurningStatus(Guid sourceCharacterId, Guid triggerPlayerId)
    : StatusEffect("burning", false, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;
    public override int Magnitude => 1;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        context.DealSkillDamage(owner, 1, DamageType.Magical, SourceCharacterId, "burning", receivesMagicPowerBonus: true);
        Expired = true;
    }
}

public sealed class PendingWeaknessStatus(Guid sourceCharacterId, Guid triggerPlayerId)
    : StatusEffect("weakness-pending", false, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        owner.Statuses.Add(new WeaknessStatus(SourceCharacterId, TriggerPlayerId));
        Expired = true;
        context.Log(L10n.Text("log.weaknessActivated", ("character", L10n.Character(owner.Definition.Key))), "status");
    }
}

public sealed class WeaknessStatus(Guid sourceCharacterId, Guid activePlayerId)
    : StatusEffect("weakness", false, sourceCharacterId)
{
    public Guid ActivePlayerId { get; } = activePlayerId;
    public override int Magnitude => 2;
    public override int ModifyActiveAttack(int damage) => Math.Max(0, damage - Magnitude);

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ActivePlayerId)
            Expired = true;
    }
}

public sealed class HarvestStatus(Guid sourceCharacterId, Guid activePlayerId)
    : StatusEffect("harvest", true, sourceCharacterId)
{
    public Guid ActivePlayerId { get; } = activePlayerId;
    public override int Magnitude => 2;
    public override bool IsAttackBuff => true;
    public override int ModifyActiveAttack(int damage) => damage + 2;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ActivePlayerId)
            Expired = true;
    }
}

public sealed class PendingHarvestStatus(Guid sourceCharacterId, Guid triggerPlayerId)
    : StatusEffect("harvest-pending", true, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        owner.Statuses.Add(new HarvestStatus(SourceCharacterId, TriggerPlayerId));
        Expired = true;
        context.Log(L10n.Text("log.harvestActivated", ("character", L10n.Character(owner.Definition.Key))), "buff");
    }
}

public sealed class ShieldComplacencyStatus(Guid triggerPlayerId, int counterReduction)
    : StatusEffect("shield-complacency", false, Guid.Empty)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;
    public override int Magnitude { get; } = Math.Clamp(counterReduction, 1, 2);

    public void Consume() => Expired = true;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == TriggerPlayerId)
            Expired = true;
    }
}
