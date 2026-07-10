using TinyPixelFights.Api;
using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

public sealed record SimpleAiAdvanceResult(int Steps, bool Advanced);

public sealed class SimpleAiService
{
    private const int MaxSteps = 80;
    private readonly GameEngine _engine;
    private readonly AttackPreviewService _previews;

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
        ["arcanist"] = 70
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

    public SimpleAiService(GameEngine engine, AttackPreviewService previews)
    {
        _engine = engine;
        _previews = previews;
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

        if (draft.Kind is HeroDraftKind.SoldierOpening or HeroDraftKind.SoldierRecruit)
        {
            var key = ChooseByPreference(draft.CandidateKeys, SoldierPreference);
            if (key is null)
                return false;

            var owner = state.Players.Single(player => player.Id == aiPlayerId);
            var upgradeTarget = owner.Characters
                .Where(character => character.IsInBattle
                    && character.Definition.CardType == CardType.Soldier
                    && character.Definition.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                    && character.SoldierRank < 2)
                .OrderByDescending(character => character.SoldierRank)
                .FirstOrDefault();

            if (draft.Kind == HeroDraftKind.SoldierRecruit && owner.ActiveCharacterCount >= 4)
            {
                if (upgradeTarget is null)
                    _engine.CancelSoldierRecruitDraft(state, aiPlayerId);
                else
                    _engine.UpgradeSoldierFromDraft(state, aiPlayerId, key, upgradeTarget.Id);
                return true;
            }

            _engine.SelectSoldierDraft(state, aiPlayerId, [key]);
            return true;
        }

        var heroKey = ChooseByPreference(draft.CandidateKeys, HeroPreference);
        if (heroKey is null)
            return false;

        _engine.SelectHeroDraft(state, aiPlayerId, heroKey);
        return true;
    }

    private bool HandleRoleActionUpgrade(GameState state, Guid aiPlayerId)
    {
        var owner = state.Players.Single(player => player.Id == aiPlayerId);
        var hero = owner.Characters
            .Where(character => _engine.CanUpgradeHeroRank(character))
            .OrderBy(character => character.HeroRank)
            .ThenByDescending(character => HeroPreference.GetValueOrDefault(character.Definition.Key, 0))
            .FirstOrDefault();
        if (hero is null)
            return false;

        var roleActionId = "";
        if (hero.HeroRank == 0)
        {
            var choices = _engine.GetRoleActionUpgradeChoices(hero);
            roleActionId = choices.FirstOrDefault(action =>
                    PreferredHeroPath.TryGetValue(hero.Definition.Key, out var preferred)
                    && action.Metadata.Id.Equals(preferred, StringComparison.OrdinalIgnoreCase))
                ?.Metadata.Id
                ?? choices.FirstOrDefault()?.Metadata.Id
                ?? "";
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
        if (state.PendingRelicReward is { PlayerId: var pendingRelicPlayerId } pendingRelic
            && pendingRelicPlayerId == player.Id)
        {
            var relicOption = pendingRelic.Options
                .Where(option => player.BattlePoints.Current >= option.Cost)
                .OrderByDescending(option => RewardScore(option, player))
                .ThenBy(option => option.Cost)
                .FirstOrDefault();
            if (relicOption is null || RewardScore(relicOption, player) <= 0)
            {
                _engine.ReturnToRewardWindow(state, player.Id);
                return true;
            }

            _engine.SelectReward(state, relicOption.InstanceId);
            return true;
        }

        var affordable = reward.Options
            .Where(option => player.BattlePoints.Current >= option.Cost)
            .ToList();
        if (affordable.Count == 0)
        {
            _engine.SkipRewardWindow(state);
            return true;
        }

        var option = affordable
            .OrderByDescending(option => RewardScore(option, player))
            .ThenBy(option => option.Cost)
            .FirstOrDefault();
        if (option is null || RewardScore(option, player) <= 0)
        {
            _engine.SkipRewardWindow(state);
            return true;
        }

        _engine.SelectReward(state, option.InstanceId);
        return true;
    }

    private static int RewardScore(RewardOptionState option, PlayerState player)
    {
        if (option.RewardId == "soldier-recruit"
            && player.ActiveCharacterCount >= 4
            && !player.Characters.Any(character => character.IsInBattle
                && character.Definition.CardType == CardType.Soldier
                && character.SoldierRank < 2))
            return -100;
        if (option.RewardId == "relic-choice"
            && !RewardCatalog.DummyRewards.Any(reward => player.BattlePoints.Current >= reward.Cost))
            return -100;

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
            .Where(item => item.Score > int.MinValue)
            .OrderByDescending(item => item.Score)
            .ToList();

        var best = candidates.FirstOrDefault();
        if (best.Attacker is null || best.Target is null || best.Score < 8)
            return false;

        _engine.Attack(state, best.Attacker.Id, best.Target.Id);
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

    private static string? ChooseByPreference(IEnumerable<string> keys, IReadOnlyDictionary<string, int> preference) =>
        keys.OrderByDescending(key => preference.GetValueOrDefault(key, 0)).FirstOrDefault();
}
