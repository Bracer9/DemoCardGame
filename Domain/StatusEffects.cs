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
    public virtual bool BlocksActiveAttack => false;
    public bool Expired { get; protected set; }

    public virtual void OnTurnStart(GameEngineContext context, CharacterState owner) { }
    public virtual void OnTurnEnd(GameEngineContext context, CharacterState owner) { }
    public virtual int ModifyBaseAttack(int attack) => attack;
    public virtual int ModifyActiveAttack(CharacterState owner, int damage) => damage;
    public virtual void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet) { }
    public virtual int ModifyPhysicalDefense(int defense) => defense;
    public virtual int ModifyMagicalDefense(int defense) => defense;
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
    public override bool IsDispellable => false;
}

public sealed class BurningStatus(Guid sourceCharacterId, Guid triggerPlayerId, int stacks = 1)
    : StatusEffect("burning", false, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;
    public int Stacks { get; private set; } = Math.Max(1, stacks);
    public override int Magnitude => Stacks;

    public void AddStacks(int amount = 1) => Stacks += Math.Max(1, amount);

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        context.DealTraitDamage(owner, 1, DamageType.Magical, SourceCharacterId, "burning", receivesMagicPowerBonus: true);
        Stacks--;
        Expired = Stacks <= 0;
    }
}

public sealed class PendingChargedStatus(Guid sourceCharacterId, Guid triggerPlayerId)
    : StatusEffect("charged-pending", true, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        owner.Statuses.RemoveAll(status => status.Id == "charged");
        owner.Statuses.Add(new ChargedStatus(SourceCharacterId, TriggerPlayerId));
        Expired = true;
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(owner.Definition.Key)),
            ("status", L10n.Status("charged"))), "magic");
    }
}

public sealed class ChargedStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("charged", true, sourceCharacterId)
{
    public const int DamageBonus = 2;
    public Guid ExpirePlayerId { get; } = expirePlayerId;
    public override int Magnitude => DamageBonus;

    public override int ModifyActiveAttack(CharacterState owner, int damage)
    {
        if (owner.Definition.AttackType != DamageType.Magical)
            return damage;

        return damage + Magnitude;
    }

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (!Expired
            && packet.Source == DamageSource.ActiveAttack
            && packet.SourceCharacter.Id == owner.Id
            && packet.DamageType == DamageType.Magical
            && packet.Amount > 0)
            packet.Notes.Add(L10n.Text("note.chargedMagic",
                ("character", L10n.Character(owner.Definition.Key)),
                ("amount", L10n.Raw(Magnitude))));
    }

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }
}

public sealed class AttackSealedStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("attack-sealed", false, sourceCharacterId)
{
    public Guid ExpirePlayerId { get; } = expirePlayerId;
    public override bool BlocksActiveAttack => true;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }
}

public sealed class SearingBrandStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("searing-brand", false, sourceCharacterId)
{
    public const int MagicDamageBonus = 1;
    public Guid ExpirePlayerId { get; } = expirePlayerId;
    public override int Magnitude => MagicDamageBonus;

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Magical
            || packet.Amount <= 0)
            return;

        packet.Amount += Magnitude;
        packet.Notes.Add(L10n.Text("note.searingBrand",
            ("character", L10n.Character(owner.Definition.Key)),
            ("amount", L10n.Raw(Magnitude))));
    }

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
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
    public override int ModifyActiveAttack(CharacterState owner, int damage) => Math.Max(0, damage - Magnitude);

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
    public override int ModifyActiveAttack(CharacterState owner, int damage) => damage + 2;

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

public sealed class SupplyGuardStatus(Guid sourceCharacterId, Guid triggerPlayerId)
    : StatusEffect("supply-guard", true, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;
    public override int Magnitude => 1;
    public override int ModifyPhysicalDefense(int defense) => defense + Magnitude;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == TriggerPlayerId)
            Expired = true;
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

public sealed class GuardedStatus(Guid sourceCharacterId)
    : StatusEffect("guarded", true, sourceCharacterId)
{
    public override int Magnitude => 2;
    public override int ModifyPhysicalDefense(int defense) => defense + Magnitude;

    public void Consume() => Expired = true;
}

public sealed class RageStatus(Guid sourceCharacterId, Guid ownerPlayerId)
    : StatusEffect("rage", true, sourceCharacterId)
{
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public override int Magnitude => 2;
    public override int ModifyPhysicalDefense(int defense) => defense - Magnitude;
    public override int ModifyMagicalDefense(int defense) => defense - Magnitude;

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == OwnerPlayerId)
            Expired = true;
    }
}

public sealed class ChallengedStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("challenged", false, sourceCharacterId)
{
    public Guid ExpirePlayerId { get; } = expirePlayerId;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }
}

public sealed class FateMarkedStatus(Guid sourceCharacterId)
    : StatusEffect("marked", false, sourceCharacterId)
{
    public const int DamageBonus = 1;
    public override int Magnitude => DamageBonus;

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.Source != DamageSource.ActiveAttack
            || packet.SourceCharacter.Id != owner.Id)
            return;

        var marker = context.State.FindCharacter(SourceCharacterId);
        if (packet.TargetCharacter.PlayerId != marker.PlayerId)
            return;

        if (context.Roll(0.5))
        {
            var before = packet.Amount;
            packet.Amount /= 2;
            packet.Notes.Add(L10n.Text("note.fateMarkReduced",
                ("character", L10n.Character(owner.Definition.Key)),
                ("before", L10n.Raw(before)),
                ("after", L10n.Raw(packet.Amount))));
        }
        else
        {
            packet.Amount += Magnitude;
            packet.Notes.Add(L10n.Text("note.fateMarkAmplified",
                ("character", L10n.Character(owner.Definition.Key)),
                ("amount", L10n.Raw(Magnitude))));
        }

        Expired = true;
    }
}

public sealed class PreyStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("prey", false, sourceCharacterId)
{
    public const int AbsoluteDamage = 2;
    public Guid ExpirePlayerId { get; } = expirePlayerId;
    public override int Magnitude => AbsoluteDamage;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }
}

public sealed class PactStatus(Guid sourceCharacterId)
    : StatusEffect("pact", true, sourceCharacterId)
{
    public const int AttackBonus = 4;
    public override int Magnitude => AttackBonus;
    public override bool IsAttackBuff => true;

    public override int ModifyActiveAttack(CharacterState owner, int damage) => damage + Magnitude;

    public void Consume() => Expired = true;
}

public sealed class RewardAttackStatus(Guid sourceCharacterId)
    : StatusEffect("reward-attack", true, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsAttackBuff => true;
    public override bool IsDispellable => false;
    public override int ModifyBaseAttack(int attack) => attack + Magnitude;
}

public sealed class RewardMagicalAttackStatus(Guid sourceCharacterId)
    : StatusEffect("reward-magical-attack", true, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsAttackBuff => true;
    public override bool IsDispellable => false;

    public override int ModifyBaseAttack(int attack) => attack + Magnitude;
}

public sealed class RewardPhysicalDefenseStatus(Guid sourceCharacterId)
    : StatusEffect("reward-physical-defense", true, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsDispellable => false;
    public override int ModifyPhysicalDefense(int defense) => defense + Magnitude;
}

public sealed class RewardMagicalDefenseStatus(Guid sourceCharacterId)
    : StatusEffect("reward-magical-defense", true, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsDispellable => false;
    public override int ModifyMagicalDefense(int defense) => defense + Magnitude;
}
