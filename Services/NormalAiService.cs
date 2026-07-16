using TinyPixelFights.Api;
using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

public sealed class NormalAiService
{
    private const int MinimumRoleActionScore = 14;

    private static readonly HashSet<string> HarmfulStatusIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "attack-sealed",
        "burning",
        "void",
        "exhaustion",
        "erosion",
        "trembling",
        "vulnerable",
        "marked",
        "prey",
        "hunted",
        "nightmare-prey"
    };

    private readonly GameEngine _engine;
    private readonly RoleActionPreviewService _previews;

    public NormalAiService(GameEngine engine, RoleActionPreviewService previews)
    {
        _engine = engine;
        _previews = previews;
    }

    public bool TryUseRoleAction(GameState state)
    {
        if (state.Phase != GamePhase.Playing
            || state.RewardWindow is not null
            || state.PendingHeroDraft is not null
            || state.PendingRoleActionUpgrade is not null)
            return false;

        var owner = state.ActivePlayer;
        // Normal intentionally stops after one role action per turn. This keeps it readable,
        // preserves AP for attacks, and makes the first-role-action BP decision meaningful.
        if (owner.FirstRoleActionBpGrantedThisTurn)
            return false;

        var profile = AiBuildProfile.Create(owner);
        var choices = owner.Characters
            .Where(actor => actor.IsAlive && actor.IsInBattle && !GameEngine.IsDeploying(actor))
            .SelectMany(actor => _engine.GetRoleActions(actor)
                .Where(action => action.IsAvailable(state, actor))
                .SelectMany(action => PotentialTargets(state, actor, action)
                    .Select(targetId => ScoreChoice(state, owner, profile, actor, action, targetId))))
            .Where(choice => choice is not null && choice.Score >= MinimumRoleActionScore)
            .Select(choice => choice!)
            .ToArray();

        if (choices.Length == 0)
            return false;

        var selected = ChooseWeighted(choices, choice => Math.Clamp(choice.Score, 1, 140));
        _engine.UseRoleAction(state, selected.ActorId, selected.RoleActionId, selected.TargetId);
        return true;
    }

    public bool ShouldBankBattlePoints(
        GameState state,
        RewardWindowState window,
        IReadOnlyList<RewardOptionState> affordable)
    {
        var player = state.ActivePlayer;
        var gainCapacity = Math.Min(
            GameEngine.BattlePointGainCapPerTurn - player.BattlePoints.GainedThisTurn,
            player.BattlePoints.Max - player.BattlePoints.Current);
        if (window.PurchaseCount > 0 || gainCapacity <= 0)
            return false;

        if (affordable.Any(option => option.RewardId == "hero-role-action-upgrade"))
            return false;

        if (affordable.Any(option => option.RewardId == "hero-recruit")
            && player.ActiveCharacterCount < 4)
            return false;

        var canGrowSoldier = player.Characters.Any(character => character.IsInBattle
            && character.Definition.CardType == CardType.Soldier
            && character.SoldierRank < 2);
        if (affordable.Any(option => option.RewardId == "soldier-recruit")
            && (player.ActiveCharacterCount < 4 || canGrowSoldier))
            return false;

        var profile = AiBuildProfile.Create(player);
        var hasAffordableBuildRelic = window.RelicOptions
            .Where(option => option.Cost <= player.BattlePoints.Current)
            .Select(option => RelicCatalog.Find(option.RewardId))
            .Any(relic => relic is not null && profile.Score(relic.BuildTags) >= 2);
        if (hasAffordableBuildRelic || player.BattlePoints.Current >= 8)
            return false;

        var bankChance = player.BattlePoints.Current switch
        {
            <= 3 => 90,
            <= 5 => 70,
            _ => 45
        };
        return _engine.Next(100) < bankChance;
    }

    private NormalRoleActionChoice? ScoreChoice(
        GameState state,
        PlayerState owner,
        AiBuildProfile profile,
        CharacterState actor,
        CharacterRoleAction action,
        Guid? targetId)
    {
        var preview = _previews.Create(state, actor.Id, action.Metadata.Id, targetId);
        if (!preview.IsValid)
            return null;

        var score = -preview.Cost * 9
            + profile.Score(action.Metadata.Tags)
            + ScoreBattlePointOpportunity(owner, action.Metadata);
        foreach (var effect in preview.Effects)
            score += ScoreEffect(state, owner, effect);

        if (preview.Effects.Count == 0)
            score -= 12;
        return new NormalRoleActionChoice(actor.Id, action.Metadata.Id, targetId, score);
    }

    private static int ScoreBattlePointOpportunity(PlayerState owner, RoleActionMetadata metadata)
    {
        var gainCapacity = Math.Min(
            GameEngine.BattlePointGainCapPerTurn - owner.BattlePoints.GainedThisTurn,
            owner.BattlePoints.Max - owner.BattlePoints.Current);
        if (gainCapacity <= 0)
            return 0;

        var score = owner.BattlePoints.Current <= 5 ? 18 : 12;
        if (gainCapacity >= 2 && metadata.Tags.Contains("battle-point", StringComparer.OrdinalIgnoreCase))
            score += 8;
        return score;
    }

    private int ScoreEffect(GameState state, PlayerState owner, RoleActionEffectForecast effect)
    {
        CharacterState? target = null;
        if (effect.TargetId is { } targetId)
        {
            try { target = state.FindCharacter(targetId); }
            catch { }
        }

        var amount = Math.Max(0, effect.Max);
        var targetIsEnemy = target is not null && target.PlayerId != owner.Id;
        return effect.Kind switch
        {
            "damage" when effect.Damage is not null => ScoreDamage(target, effect.Damage),
            "healing" => amount * 14 + ScoreCriticalHealing(target, amount),
            "morale-healing" => amount * 5,
            "shared-shield" => amount * 7,
            "shield-defense" => amount * 4,
            "bonus-attacks" => amount * (12 + (target is null ? 0 : _engine.GetActiveAttack(state, target) * 6)),
            "action-points" => amount * 16,
            "action-point-debt" => -amount * 10,
            "status-damage" => amount * 8,
            "hp-cost" => -amount * 5 - ScoreDangerousHpCost(target, amount),
            "morale-damage" => amount * 5,
            "hp-damage" => amount * 14,
            "shared-shield-damage" => amount * 8,
            "shared-shield-cost" => -amount * 5,
            "status-layers" or "status-turns" => ScoreStatus(effect.DetailId, amount, targetIsEnemy),
            _ => amount * 2
        };
    }

    private static int ScoreDamage(CharacterState? target, DamageForecast damage)
    {
        var score = damage.HpDamageMax * 16
            + damage.MoraleDamageMax * 4
            + damage.ShieldAbsorb * 6;
        if (target is not null && damage.HpDamageMax >= target.CurrentHp)
            score += 60;
        return score;
    }

    private int ScoreCriticalHealing(CharacterState? target, int amount)
    {
        if (target is null || amount <= 0)
            return 0;
        return target.CurrentHp * 2 <= _engine.GetMaxHp(target) ? 10 : 0;
    }

    private static int ScoreDangerousHpCost(CharacterState? target, int amount) =>
        target is not null && target.CurrentHp - amount <= 2 ? 30 : 0;

    private static int ScoreStatus(string? detailId, int amount, bool targetIsEnemy)
    {
        if (amount <= 0)
            return 0;
        var harmful = detailId is not null && HarmfulStatusIds.Contains(detailId);
        if (harmful)
            return targetIsEnemy ? amount * 8 : -amount * 10;
        return targetIsEnemy ? -amount * 5 : amount * 8;
    }

    private static IReadOnlyList<Guid?> PotentialTargets(
        GameState state,
        CharacterState actor,
        CharacterRoleAction action)
    {
        var targets = new List<Guid?>();
        var kinds = action.Metadata.ValidTargetKinds;
        if (action.Metadata.ActivationMode == RoleActionActivationMode.Immediate
            || kinds.Any(kind => kind is RoleActionTargetKind.None
                or RoleActionTargetKind.OwnShield
                or RoleActionTargetKind.EnemyShield
                or RoleActionTargetKind.ActionPointPanel
                or RoleActionTargetKind.BattlePointMedal
                or RoleActionTargetKind.EmptySlot))
            targets.Add(null);

        if (kinds.Contains(RoleActionTargetKind.SelfCard))
            targets.Add(actor.Id);
        if (kinds.Contains(RoleActionTargetKind.AllyCard))
        {
            targets.AddRange(state.ActivePlayer.Characters
                .Where(target => target.IsAlive && target.IsInBattle && !GameEngine.IsDeploying(target))
                .Select(target => (Guid?)target.Id));
        }
        if (kinds.Contains(RoleActionTargetKind.EnemyCard))
        {
            targets.AddRange(state.Opponent.Characters
                .Where(target => target.IsAlive && target.IsInBattle && !GameEngine.IsDeploying(target))
                .Select(target => (Guid?)target.Id));
        }

        if (targets.Count == 0)
            targets.Add(null);
        return targets.Distinct().ToArray();
    }

    private T ChooseWeighted<T>(IReadOnlyList<T> choices, Func<T, int> weightSelector)
    {
        var weights = choices.Select(choice => Math.Max(1, weightSelector(choice))).ToArray();
        var roll = _engine.Next(weights.Sum());
        for (var index = 0; index < choices.Count; index++)
        {
            roll -= weights[index];
            if (roll < 0)
                return choices[index];
        }

        return choices[^1];
    }

    private sealed record NormalRoleActionChoice(
        Guid ActorId,
        string RoleActionId,
        Guid? TargetId,
        int Score);
}
