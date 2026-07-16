using TinyPixelFights.Api;
using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

public sealed record SimpleAiAdvanceResult(int Steps, bool Advanced);

public sealed class SimpleAiService
{
    private const int MaxSteps = 80;
    private readonly GameEngine _engine;
    private readonly AttackPreviewService _previews;
    private readonly NormalAiService _normal;

    private static readonly IReadOnlyDictionary<string, int> HeroPreference = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["mage"] = 90,
        ["knight"] = 82,
        ["princess"] = 78,
        ["barbarian"] = 74,
        ["monster"] = 70,
        ["oracle"] = 64,
        ["druid"] = 58,
        ["peasant"] = 52
    };

    private static readonly IReadOnlyDictionary<string, int> SoldierPreference = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["shieldmaiden"] = 90,
        ["cleric"] = 82,
        ["duelist"] = 74,
        ["arcanist"] = 70,
        ["jester"] = 68
    };

    private static readonly IReadOnlyDictionary<string, string> PreferredHeroPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["princess"] = "royal-command",
        ["oracle"] = "star-reading",
        ["peasant"] = "field-work",
        ["mage"] = "arcane-channel",
        ["druid"] = "weakening-spores-action",
        ["barbarian"] = "war-cry",
        ["monster"] = "predatory-gaze",
        ["knight"] = "guard-oath"
    };

    public SimpleAiService(GameEngine engine, AttackPreviewService previews, NormalAiService normal)
    {
        _engine = engine;
        _previews = previews;
        _normal = normal;
    }

    public SimpleAiAdvanceResult Advance(GameState state)
    {
        var aiPlayerId = state.AiPlayerId;
        if (aiPlayerId is null)
            return new SimpleAiAdvanceResult(0, false);

        var steps = 0;
        while (steps < MaxSteps && ShouldAdvance(state, aiPlayerId.Value))
        {
            var advanced = TryAdvanceOneStep(state, aiPlayerId.Value);
            if (!advanced)
                break;
            steps++;
        }

        return new SimpleAiAdvanceResult(steps, steps > 0);
    }

    private static bool ShouldAdvance(GameState state, Guid aiPlayerId) =>
        state.Phase != GamePhase.Finished
        && (state.ActivePlayerId == aiPlayerId
            || state.RewardWindow?.PlayerId == aiPlayerId
            || state.PendingHeroDraft?.PlayerId == aiPlayerId
            || state.PendingRoleActionUpgrade?.PlayerId == aiPlayerId
            || state.PendingRelicReward?.PlayerId == aiPlayerId);

    private bool TryAdvanceOneStep(GameState state, Guid aiPlayerId)
    {
        if (state.PendingHeroDraft?.PlayerId == aiPlayerId)
            return HandleHeroDraft(state, aiPlayerId);
        if (state.PendingRoleActionUpgrade?.PlayerId == aiPlayerId)
            return HandleRoleActionUpgrade(state, aiPlayerId);
        if (state.RewardWindow?.PlayerId == aiPlayerId)
            return HandleReward(state);
        if (state.ActivePlayerId == aiPlayerId && state.Phase == GamePhase.Playing)
            return HandleBattleAction(state);

        return false;
    }

    private bool HandleHeroDraft(GameState state, Guid aiPlayerId)
    {
        var draft = state.PendingHeroDraft;
        if (draft is null || draft.PlayerId != aiPlayerId)
            return false;

        var owner = state.Players.Single(player => player.Id == aiPlayerId);

        if (draft.Kind is HeroDraftKind.SoldierOpening or HeroDraftKind.SoldierRecruit)
        {
            var choice = ChooseSoldierDraft(draft, owner);
            if (choice.Key is null)
            {
                if (draft.Kind == HeroDraftKind.SoldierRecruit)
                {
                    _engine.CancelSoldierRecruitDraft(state, aiPlayerId);
                    return true;
                }
                return false;
            }

            if (draft.Kind == HeroDraftKind.SoldierRecruit && owner.ActiveCharacterCount >= 4)
            {
                if (choice.UpgradeTarget is null)
                    _engine.CancelSoldierRecruitDraft(state, aiPlayerId);
                else
                    _engine.UpgradeSoldierFromDraft(state, aiPlayerId, choice.Key, choice.UpgradeTarget.Id);
                return true;
            }

            _engine.SelectSoldierDraft(state, aiPlayerId, [choice.Key]);
            return true;
        }

        var heroKey = ChooseHeroDraft(draft.CandidateKeys, owner);
        if (heroKey is null)
            return false;

        _engine.SelectHeroDraft(state, aiPlayerId, heroKey);
        return true;
    }

    private bool HandleRoleActionUpgrade(GameState state, Guid aiPlayerId)
    {
        var owner = state.Players.Single(player => player.Id == aiPlayerId);
        var candidates = owner.Characters
            .Where(character => _engine.CanUpgradeHeroRank(character))
            .Select(character => new
            {
                Hero = character,
                Weight = 10
                    + (3 - character.HeroRank) * 3
                    + ScoreHeroBuildFit(owner, character) * 2
                    + HeroPreference.GetValueOrDefault(character.Definition.Key, 0) / 30
            })
            .ToArray();
        if (candidates.Length == 0)
            return false;
        var hero = ChooseWeighted(candidates, candidate => candidate.Weight).Hero;

        var roleActionId = "";
        if (hero.HeroRank == 0)
        {
            var choices = _engine.GetRoleActionUpgradeChoices(hero);
            var profile = AiBuildProfile.Create(owner, hero.Id);
            var weightedChoices = choices
                .Select(action => new
                {
                    Action = action,
                    Path = HeroGrowthCatalog.FindByBaseRoleAction(action.Metadata.Id),
                    IsPreferred = PreferredHeroPath.TryGetValue(hero.Definition.Key, out var preferred)
                        && action.Metadata.Id.Equals(preferred, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray();
            if (weightedChoices.Length > 0)
            {
                roleActionId = ChooseWeighted(weightedChoices, choice =>
                    8
                    + (choice.Path is null ? 0 : profile.Score(choice.Path.RelicTags) * 2)
                    + (choice.IsPreferred ? 2 : 0)).Action.Metadata.Id;
            }
        }

        _engine.SelectRoleActionUpgrade(state, hero.Id, roleActionId);
        return true;
    }

    private bool HandleReward(GameState state)
    {
        var reward = state.RewardWindow;
        if (reward is null)
            return false;

        var player = state.Players.Single(item => item.Id == reward.PlayerId);
        var profile = AiBuildProfile.Create(player);
        if (state.PendingRelicReward is { PlayerId: var pendingRelicPlayerId } pendingRelic
            && pendingRelicPlayerId == player.Id)
        {
            var relicChoices = pendingRelic.Options
                .Where(option => player.BattlePoints.Current >= option.Cost)
                .Select(option => new
                {
                    Option = option,
                    Weight = RewardScore(option, player, profile)
                })
                .Where(choice => choice.Weight > 0)
                .ToArray();
            if (relicChoices.Length == 0)
            {
                _engine.ReturnToRewardWindow(state, player.Id);
                return true;
            }

            var relicOption = ChooseWeighted(relicChoices, choice => choice.Weight).Option;
            _engine.SelectReward(state, relicOption.InstanceId);
            return true;
        }

        var affordable = reward.Options
            .Where(option => player.BattlePoints.Current >= option.Cost)
            .Where(option => option.RewardId != "relic-choice"
                || reward.RelicOptions.Any(relic => player.BattlePoints.Current >= relic.Cost))
            .ToList();
        if (affordable.Count == 0)
        {
            _engine.SkipRewardWindow(state);
            return true;
        }

        var freeRoleActionUpgrade = affordable.FirstOrDefault(option =>
            option.RewardId == RewardCatalog.HeroRoleActionUpgrade.Id && option.Cost == 0);
        if (freeRoleActionUpgrade is not null)
        {
            _engine.SelectReward(state, freeRoleActionUpgrade.InstanceId);
            return true;
        }

        if (state.AiDifficulty == AiDifficulty.Normal
            && _normal.ShouldBankBattlePoints(state, reward, affordable))
        {
            _engine.SkipRewardWindow(state);
            return true;
        }

        var rewardChoices = affordable
            .Select(option => new
            {
                Option = option,
                Weight = RewardScore(option, player, profile)
            })
            .Where(choice => choice.Weight > 0)
            .ToArray();
        if (rewardChoices.Length == 0)
        {
            _engine.SkipRewardWindow(state);
            return true;
        }

        var option = ChooseWeighted(rewardChoices, choice => choice.Weight).Option;
        _engine.SelectReward(state, option.InstanceId);
        return true;
    }

    private static int RewardScore(RewardOptionState option, PlayerState player, AiBuildProfile profile)
    {
        if (option.RewardId == "soldier-recruit"
            && player.ActiveCharacterCount >= 4
            && !player.Characters.Any(character => character.IsInBattle
                && character.Definition.CardType == CardType.Soldier
                && character.SoldierRank < 2))
            return -100;

        var relic = RelicCatalog.Find(option.RewardId);
        if (relic is not null)
        {
            var rarityBonus = relic.Rarity.ToLowerInvariant() switch
            {
                "epic" => 4,
                "rare" => 2,
                _ => 0
            };
            return 20 + profile.Score(relic.BuildTags) * 2 + rarityBonus - option.Cost;
        }

        return option.RewardId switch
    {
        "hero-role-action-upgrade" => 100,
        "hero-recruit" => 82,
        "soldier-recruit" => 74,
        "relic-choice" => 62,
        "dummy-reward-c" => 42,
        "dummy-reward-b" => 38,
        "dummy-reward-a" => 34,
        _ => 20
    } - option.Cost;
    }

    private bool HandleBattleAction(GameState state)
    {
        if (TryAssignDeputy(state))
            return true;

        if (state.AiDifficulty == AiDifficulty.Normal && _normal.TryUseRoleAction(state))
            return true;

        if (TryAttack(state))
            return true;

        if (CanDeployUsefulShield(state))
        {
            _engine.DeployShield(state);
            return true;
        }

        _engine.EndTurn(state);
        return true;
    }

    private bool TryAttack(GameState state)
    {
        var owner = state.ActivePlayer;
        var enemy = state.Opponent;
        var candidates = owner.Characters
            .Where(character => character.IsAlive
                && character.IsInBattle
                && !GameEngine.IsDeploying(character)
                && !character.HasActed
                && character.Definition.Cost <= state.ActionPoints)
            .SelectMany(attacker => enemy.Characters
                .Where(target => target.IsAlive && target.IsInBattle && !GameEngine.IsDeploying(target))
                .Select(target => ScoreAttack(state, attacker, target)))
            .Where(item => item.Score >= 8)
            .ToList();

        if (candidates.Count == 0)
            return false;

        var selected = ChooseWeighted(candidates, candidate => Math.Clamp(candidate.Score, 1, 120));
        _engine.Attack(state, selected.Attacker!.Id, selected.Target!.Id);
        return true;
    }

    private (CharacterState? Attacker, CharacterState? Target, int Score) ScoreAttack(
        GameState state,
        CharacterState attacker,
        CharacterState target)
    {
        AttackPreview preview;
        try { preview = _previews.Create(state, attacker.Id, target.Id); }
        catch { return (null, null, int.MinValue); }
        if (!preview.IsValid)
            return (null, null, int.MinValue);

        var hpDamage = preview.Attack.HpDamageMax;
        var moraleDamage = preview.Attack.MoraleDamageMax;
        var score = hpDamage * 14
            + moraleDamage * 3
            + preview.Attack.ShieldAbsorb * 5
            - preview.Counter.HpDamageMax * 8
            - attacker.Definition.Cost;

        if (hpDamage >= target.CurrentHp)
            score += 120;
        if (preview.Attack.ShieldWillAbsorb && preview.Attack.ShieldAbsorb > 0)
            score += 18;
        if (target.Definition.CardType == CardType.Hero)
            score += 8;
        if (attacker.CurrentHp <= preview.Counter.HpDamageMax)
            score -= 80;

        return (attacker, target, score);
    }

    private bool CanDeployUsefulShield(GameState state)
    {
        var owner = state.ActivePlayer;
        var cost = GameEngine.GetShieldCost(owner);
        if (state.ActionPoints < cost)
            return false;
        if (!GameEngine.CanDeployShield(state))
            return false;
        return owner.SharedShield <= 2 || owner.Characters.Any(character => character.IsAlive && character.CurrentHp * 2 <= _engine.GetMaxHp(character));
    }

    private string? ChooseHeroDraft(IEnumerable<string> keys, PlayerState owner)
    {
        var profile = AiBuildProfile.Create(owner);
        var activeAttackTypes = owner.Characters
            .Where(character => character.IsAlive && character.IsInBattle)
            .Select(character => character.Definition.AttackType)
            .ToHashSet();

        var candidates = keys
            .Select(key => CharacterCatalog.Heroes.FirstOrDefault(definition =>
                definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            .Where(definition => definition is not null)
            .Select(definition => new
            {
                Definition = definition!,
                Weight = 10
                    + profile.Score(AiBuildProfile.GetHeroTags(definition!.Key)) * 2
                    + HeroPreference.GetValueOrDefault(definition.Key, 0) / 25
                    + (activeAttackTypes.Contains(definition.AttackType) ? 0 : 2)
            })
            .ToArray();
        return candidates.Length == 0
            ? null
            : ChooseWeighted(candidates, candidate => candidate.Weight).Definition.Key;
    }

    private (string? Key, CharacterState? UpgradeTarget) ChooseSoldierDraft(
        PendingHeroDraftState draft,
        PlayerState owner)
    {
        var profile = AiBuildProfile.Create(owner);
        var teamIsFull = draft.Kind == HeroDraftKind.SoldierRecruit && owner.ActiveCharacterCount >= 4;
        var activeAttackTypes = owner.Characters
            .Where(character => character.IsAlive && character.IsInBattle)
            .Select(character => character.Definition.AttackType)
            .ToHashSet();

        var candidates = draft.CandidateKeys
            .Select(key => new
            {
                Key = key,
                Definition = CharacterCatalog.Soldiers.FirstOrDefault(definition =>
                    definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase)),
                UpgradeTarget = FindSoldierUpgradeTarget(owner, key)
            })
            .Where(choice => choice.Definition is not null && (!teamIsFull || choice.UpgradeTarget is not null))
            .Select(choice => new
            {
                choice.Key,
                choice.UpgradeTarget,
                Weight = 10
                    + profile.Score(AiBuildProfile.GetSoldierTags(choice.Key)) * 2
                    + SoldierPreference.GetValueOrDefault(choice.Key, 0) / 25
                    + (activeAttackTypes.Contains(choice.Definition!.AttackType) ? 0 : 2)
                    + (choice.UpgradeTarget?.SoldierRank ?? 0) * 3
                    - owner.Characters.Count(character => character.IsInBattle
                        && character.Definition.Key.Equals(choice.Key, StringComparison.OrdinalIgnoreCase)) * 4
            })
            .ToArray();
        if (candidates.Length == 0)
            return default;

        var selected = ChooseWeighted(candidates, choice => Math.Max(1, choice.Weight));
        return (selected.Key, selected.UpgradeTarget);
    }

    private static CharacterState? FindSoldierUpgradeTarget(PlayerState owner, string key) =>
        owner.Characters
            .Where(character => character.IsInBattle
                && character.Definition.CardType == CardType.Soldier
                && character.Definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                && character.SoldierRank < 2)
            .OrderByDescending(character => character.SoldierRank)
            .ThenByDescending(character => character.CurrentHp)
            .FirstOrDefault();

    private static int ScoreHeroBuildFit(PlayerState owner, CharacterState hero) =>
        AiBuildProfile.Create(owner, hero.Id).Score(AiBuildProfile.GetHeroTags(hero));

    private bool TryAssignDeputy(GameState state)
    {
        var owner = state.ActivePlayer;
        if (owner.ActiveCharacterCount < 4)
            return false;

        var soldiers = owner.Characters
            .Where(soldier => _engine.GetAssignDeputyDisabledReason(state, soldier) is null)
            .ToArray();
        var heroes = owner.Characters
            .Where(hero => hero.Definition.CardType == CardType.Hero
                && hero.IsAlive
                && hero.IsInBattle
                && !GameEngine.IsDeploying(hero)
                && hero.DeputySoldierId is null
                && hero.DeputyEffectId is null)
            .ToArray();

        var choices = soldiers
            .SelectMany(soldier => heroes.Select(hero => new
            {
                Soldier = soldier,
                Hero = hero,
                Score = ScoreDeputyAssignment(owner, soldier, hero)
            }))
            .Where(choice => choice.Score >= 10)
            .ToArray();

        if (choices.Length == 0)
            return false;

        var selected = ChooseWeighted(choices, choice => choice.Score);
        _engine.AssignDeputy(state, selected.Soldier.Id, selected.Hero.Id);
        return true;
    }

    private int ScoreDeputyAssignment(PlayerState owner, CharacterState soldier, CharacterState hero)
    {
        var deputy = DeputyCatalog.FindBySoldierKey(soldier.Definition.Key);
        if (deputy is null)
            return int.MinValue;

        var attackType = GameEngine.GetAttackType(hero);
        var statScore = deputy.StatKind switch
        {
            DeputyStatKind.PhysicalAttack when attackType == DamageType.Physical => 18,
            DeputyStatKind.MagicalAttack when attackType == DamageType.Magical => 18,
            DeputyStatKind.PhysicalAttack or DeputyStatKind.MagicalAttack => -100,
            DeputyStatKind.Attack => 12,
            DeputyStatKind.PhysicalDefense => 10 + Math.Max(0, 1 - _engine.GetPhysicalDefense(hero)),
            DeputyStatKind.MagicalDefense => 10 + Math.Max(0, 1 - _engine.GetMagicalDefense(hero)),
            _ => 8
        };
        if (statScore < 0)
            return statScore;

        var profile = AiBuildProfile.Create(owner, soldier.Id);
        var hostTags = AiBuildProfile.GetHeroTags(hero);
        var directMatches = deputy.BuildTags.Intersect(hostTags, StringComparer.OrdinalIgnoreCase).Count();
        var lostUnitValue = soldier.Definition.Attack * 3 + soldier.Definition.Cost * 2 + 4;
        return statScore
            + profile.Score(deputy.BuildTags) * 2
            + directMatches * 6
            - lostUnitValue;
    }

    private T ChooseWeighted<T>(IReadOnlyList<T> choices, Func<T, int> weightSelector)
    {
        var weights = choices.Select(choice => Math.Max(1, weightSelector(choice))).ToArray();
        var total = weights.Sum();
        var roll = _engine.Next(total);
        for (var index = 0; index < choices.Count; index++)
        {
            roll -= weights[index];
            if (roll < 0)
                return choices[index];
        }

        return choices[^1];
    }
}
