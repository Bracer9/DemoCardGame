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
    private const int AttackBonus = 3;
    private const int DefensePenalty = 2;

    public override int Magnitude => AttackBonus;
    public override bool IsAttackBuff => true;
    public override bool IsDispellable => false;
    public override int ModifyBaseAttack(int attack) => attack + AttackBonus;
    public override int ModifyPhysicalDefense(int defense) => defense - DefensePenalty;
    public override int ModifyMagicalDefense(int defense) => defense - DefensePenalty;
}

public sealed class MagicPowerStatus(Guid sourceCharacterId)
    : StatusEffect("magic-power", false, sourceCharacterId)
{
    public override int Magnitude => 1;
    public override bool IsDispellable => false;
}

public sealed class DeployingStatus(Guid sourceCharacterId, Guid ownerPlayerId, int readyOnTurnNumber)
    : StatusEffect("deploying", true, sourceCharacterId)
{
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public int ReadyOnTurnNumber { get; } = Math.Max(1, readyOnTurnNumber);
    public override bool IsDispellable => false;
    public override bool BlocksActiveAttack => true;

    public void ExpireIfReady(int turnNumber)
    {
        if (turnNumber >= ReadyOnTurnNumber)
            Expired = true;
    }

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        ExpireIfReady(context.State.TurnNumber);
    }
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

public abstract class TurnDurationStatus : StatusEffect
{
    public const int DefaultTurns = 2;

    protected TurnDurationStatus(
        string id,
        bool isBuff,
        Guid sourceCharacterId,
        int turns = DefaultTurns)
        : base(id, isBuff, sourceCharacterId)
    {
        RemainingTurns = Math.Max(1, turns);
    }

    public int RemainingTurns { get; private set; }
    public override int Magnitude => RemainingTurns;

    public void AddTurns(int turns = 1) =>
        RemainingTurns += Math.Max(1, turns);

    public void RefreshTurns(int turns) =>
        RemainingTurns = Math.Max(RemainingTurns, Math.Max(1, turns));

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != owner.PlayerId)
            return;

        RemainingTurns--;
        Expired = RemainingTurns <= 0;
    }
}

public sealed class PendingChantStatus(Guid sourceCharacterId, Guid triggerPlayerId, int stacks)
    : StatusEffect("chant-pending", true, sourceCharacterId)
{
    public Guid TriggerPlayerId { get; } = triggerPlayerId;
    public override int Magnitude => Math.Max(1, stacks);

    public override void OnTurnStart(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != TriggerPlayerId || Expired)
            return;

        owner.Statuses.RemoveAll(status => status.Id == "chant");
        owner.Statuses.Add(new ChantStatus(SourceCharacterId, Magnitude));
        Expired = true;
        context.Log(L10n.Text("log.statusApplied",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("status", L10n.Status("chant"))), "magic");
    }
}

public sealed class ChantStatus(Guid sourceCharacterId, int stacks = 1)
    : StatusEffect("chant", true, sourceCharacterId)
{
    public int Stacks { get; private set; } = Math.Max(1, stacks);
    public override int Magnitude => Stacks;

    public void AddStacks(int stacks = 1) => Stacks += Math.Max(1, stacks);

    public void ConsumeStack()
    {
        Stacks--;
        Expired = Stacks <= 0;
    }

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != owner.PlayerId)
            return;

        Stacks--;
        Expired = Stacks <= 0;
    }

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Magical
            || !packet.CanConsumeChargeStatuses
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount *= 2;
        Stacks--;
        packet.ConsumedChant = true;
        if (Stacks <= 0)
            Expired = true;
        packet.Notes.Add(L10n.Text("note.chantMagic",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class MightyStrikeStatus(Guid sourceCharacterId, int stacks = 1)
    : StatusEffect("mighty-strike", true, sourceCharacterId)
{
    public int Stacks { get; private set; } = Math.Max(1, stacks);
    public override int Magnitude => Stacks;
    public override bool IsAttackBuff => true;

    public void AddStacks(int stacks = 1) => Stacks += Math.Max(1, stacks);

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId != owner.PlayerId)
            return;

        Stacks--;
        Expired = Stacks <= 0;
    }

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount *= 2;
        Stacks--;
        if (Stacks <= 0)
            Expired = true;
        packet.Notes.Add(L10n.Text("note.mightyStrikePhysical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
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

public sealed class VoidStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("void", false, sourceCharacterId, turns)
{
    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Magical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, (int)Math.Ceiling(packet.Amount * 1.25));
        packet.Notes.Add(L10n.Text("note.voidMagic",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class VulnerableStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("vulnerable", false, sourceCharacterId, turns)
{
    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, (int)Math.Ceiling(packet.Amount * 1.25));
        packet.Notes.Add(L10n.Text("note.vulnerablePhysical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class ExhaustionStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("exhaustion", false, sourceCharacterId, turns)
{
    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, packet.Amount / 2);
        packet.Notes.Add(L10n.Text("note.exhaustionPhysical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class ErosionStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("erosion", false, sourceCharacterId, turns)
{
    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Magical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, packet.Amount / 2);
        packet.Notes.Add(L10n.Text("note.erosionMagical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class StrongAttackStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("strong-attack", true, sourceCharacterId, turns)
{
    public override bool IsAttackBuff => true;

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, (int)Math.Ceiling(packet.Amount * 1.5));
        packet.Notes.Add(L10n.Text("note.strongAttack",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class MagicSurgeStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("magic-surge", true, sourceCharacterId, turns)
{
    public override bool IsAttackBuff => true;

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.SourceCharacter.Id != owner.Id
            || packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Magical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, (int)Math.Ceiling(packet.Amount * 1.5));
        packet.Notes.Add(L10n.Text("note.magicSurge",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
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
        context.Log(L10n.Text("log.harvestActivated",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id))), "buff");
    }
}

public sealed class FortifyStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("fortify", true, sourceCharacterId, turns)
{
    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, packet.Amount / 2);
        packet.Notes.Add(L10n.Text("note.fortifyPhysical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class SpellWardStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("spell-ward", true, sourceCharacterId, turns)
{
    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.DamageType != DamageType.Magical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(1, packet.Amount / 2);
        packet.Notes.Add(L10n.Text("note.spellWardMagical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
}

public sealed class GuardOathStatus(Guid sourceCharacterId, int stacks = 1)
    : StatusEffect("guard-oath", true, sourceCharacterId)
{
    public const int PhysicalReduction = 2;
    public int Stacks { get; private set; } = Math.Max(1, stacks);
    public override int Magnitude => Stacks;

    public void AddStack(int amount = 1) => Stacks += Math.Max(1, amount);

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.Source != DamageSource.ActiveAttack
            || packet.DamageType != DamageType.Physical
            || packet.Amount <= 0)
            return;

        var before = packet.Amount;
        packet.Amount = Math.Max(0, packet.Amount - PhysicalReduction);
        Stacks--;
        if (Stacks <= 0)
            Expired = true;
        packet.Notes.Add(L10n.Text("note.guardOathPhysical",
            ("character", L10n.Character(owner.Definition.Key)),
            ("characterId", L10n.Raw(owner.Id)),
            ("before", L10n.Raw(before)),
            ("after", L10n.Raw(packet.Amount))));
    }
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

public sealed class TremblingStatus(Guid sourceCharacterId, int turns = TurnDurationStatus.DefaultTurns)
    : TurnDurationStatus("trembling", false, sourceCharacterId, turns)
{
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
                ("characterId", L10n.Raw(owner.Id)),
                ("before", L10n.Raw(before)),
                ("after", L10n.Raw(packet.Amount))));
        }
        else
        {
            packet.Amount += Magnitude;
            packet.Notes.Add(L10n.Text("note.fateMarkAmplified",
                ("character", L10n.Character(owner.Definition.Key)),
                ("characterId", L10n.Raw(owner.Id)),
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
    public const int AbsoluteDamage = 4;
    public override int Magnitude => AbsoluteDamage;
    public override bool IsAttackBuff => true;

    public void Consume() => Expired = true;
}

public sealed class VictoryEdictStatus(Guid sourceCharacterId, int absoluteDamage)
    : StatusEffect("edict-of-victory", true, sourceCharacterId)
{
    public override int Magnitude => Math.Max(0, absoluteDamage);
    public override bool IsAttackBuff => true;
    public void Consume() => Expired = true;
}

public sealed class AstralAlignmentStatus(Guid sourceCharacterId, Guid ownerPlayerId, int bonusDamage)
    : StatusEffect("astral-alignment", true, sourceCharacterId)
{
    private bool _used;
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public override int Magnitude => Math.Max(0, bonusDamage);
    public override bool IsAttackBuff => true;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == OwnerPlayerId)
            Expired = true;
    }

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired || _used || packet.SourceCharacter.Id != owner.Id || packet.DamageType != DamageType.Magical || packet.Amount <= 0)
            return;

        packet.Amount += Magnitude;
        var targetOwner = context.State.FindOwner(packet.TargetCharacter);
        var splash = Math.Max(1, (int)Math.Ceiling(Magnitude / 2.0));
        foreach (var adjacent in targetOwner.Characters.Where(character =>
                     character.IsAlive
                     && !GameEngine.IsDeploying(character)
                     && Math.Abs(character.Slot - packet.TargetCharacter.Slot) == 1))
            packet.Collateral.Add(new CollateralDamage(owner, adjacent, splash, DamageType.Magical, "astral-alignment"));
        _used = true;
        Expired = true;
    }
}

public sealed class MilitiaCallStatus(Guid sourceCharacterId, Guid ownerPlayerId, int bonusDamage)
    : StatusEffect("militia-call", true, sourceCharacterId)
{
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public override int Magnitude => Math.Max(0, bonusDamage);
    public override bool IsAttackBuff => true;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == OwnerPlayerId)
            Expired = true;
    }

    public override void ModifyOutgoingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired || packet.SourceCharacter.Id != owner.Id || packet.Source != DamageSource.ActiveAttack || packet.Amount <= 0)
            return;

        packet.Amount += Magnitude;
        Expired = true;
    }
}

public sealed class HuntedStatus(Guid sourceCharacterId, Guid ownerPlayerId, int bonusDamage)
    : StatusEffect("hunted", false, sourceCharacterId)
{
    private readonly HashSet<Guid> _usedSoldiers = [];
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public override int Magnitude => Math.Max(0, bonusDamage);

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == OwnerPlayerId)
            Expired = true;
    }

    public override void ModifyIncomingDamage(GameEngineContext context, CharacterState owner, DamagePacket packet)
    {
        if (Expired
            || packet.TargetCharacter.Id != owner.Id
            || packet.SourceCharacter.Definition.CardType != CardType.Soldier
            || packet.Amount <= 0
            || _usedSoldiers.Contains(packet.SourceCharacter.Id))
            return;

        packet.Amount += Magnitude;
        _usedSoldiers.Add(packet.SourceCharacter.Id);
    }
}

public sealed class NightmarePreyStatus(Guid sourceCharacterId, Guid expirePlayerId, int absoluteDamage)
    : StatusEffect("nightmare-prey", false, sourceCharacterId)
{
    public Guid ExpirePlayerId { get; } = expirePlayerId;
    public override int Magnitude => Math.Max(0, absoluteDamage);

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }

    public void Consume() => Expired = true;
}

public sealed class AbyssalBargainStatus(Guid sourceCharacterId, int absoluteDamage)
    : StatusEffect("abyssal-bargain", true, sourceCharacterId)
{
    public int LifeCost { get; init; }
    public override int Magnitude => Math.Max(0, absoluteDamage);
    public override bool IsAttackBuff => true;
    public void Consume() => Expired = true;
}

public sealed class ArchivedStatus(Guid sourceCharacterId, Guid expirePlayerId)
    : StatusEffect("archived", false, sourceCharacterId)
{
    public Guid ExpirePlayerId { get; } = expirePlayerId;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == ExpirePlayerId)
            Expired = true;
    }
}

public sealed class GloryRoarStatus(Guid sourceCharacterId, Guid ownerPlayerId)
    : StatusEffect("glory-roar", true, sourceCharacterId)
{
    public Guid OwnerPlayerId { get; } = ownerPlayerId;
    public override int Magnitude => 1;

    public override void OnTurnEnd(GameEngineContext context, CharacterState owner)
    {
        if (context.State.ActivePlayerId == OwnerPlayerId)
            Expired = true;
    }
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
