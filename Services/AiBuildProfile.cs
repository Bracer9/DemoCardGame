using TinyPixelFights.Domain;

namespace TinyPixelFights.Services;

internal sealed class AiBuildProfile
{
    private readonly Dictionary<string, int> _tagWeights = new(StringComparer.OrdinalIgnoreCase);

    public bool HasSignals => _tagWeights.Count > 0;

    public int Score(IEnumerable<string> tags) =>
        tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Sum(tag => _tagWeights.GetValueOrDefault(tag, 0));

    public static AiBuildProfile Create(PlayerState player, Guid? excludedCharacterId = null)
    {
        var profile = new AiBuildProfile();

        foreach (var character in player.Characters.Where(character =>
                     character.Id != excludedCharacterId
                     && (character.IsInBattle || character.Zone == CharacterZone.Deputy)))
        {
            if (character.Definition.CardType == CardType.Hero)
            {
                var selectedPath = HeroGrowthCatalog.Find(character);
                if (selectedPath is not null)
                {
                    profile.Add(selectedPath.RelicTags, 4);
                    continue;
                }

                foreach (var path in HeroGrowthCatalog.All.Where(path =>
                             path.HeroKey.Equals(character.Definition.Key, StringComparison.OrdinalIgnoreCase)))
                    profile.Add(path.RelicTags, 1);
                continue;
            }

            var deputy = DeputyCatalog.FindBySoldierKey(character.Definition.Key);
            if (deputy is not null)
                profile.Add(deputy.BuildTags, character.SoldierRank >= 2 ? 3 : 2);
        }

        foreach (var relicState in player.Relics)
        {
            var relic = RelicCatalog.Find(relicState.Id);
            if (relic is not null)
                profile.Add(relic.BuildTags, 3);
        }

        return profile;
    }

    public static IReadOnlyList<string> GetHeroTags(string heroKey, string? selectedRoleActionId = null)
    {
        var selectedPath = HeroGrowthCatalog.FindByBaseRoleAction(selectedRoleActionId);
        if (selectedPath is not null
            && selectedPath.HeroKey.Equals(heroKey, StringComparison.OrdinalIgnoreCase))
            return selectedPath.RelicTags;

        return HeroGrowthCatalog.All
            .Where(path => path.HeroKey.Equals(heroKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => path.RelicTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> GetHeroTags(CharacterState hero) =>
        GetHeroTags(hero.Definition.Key, hero.HeroPathRoleActionId);

    public static IReadOnlyList<string> GetSoldierTags(string soldierKey) =>
        DeputyCatalog.FindBySoldierKey(soldierKey)?.BuildTags ?? [];

    private void Add(IEnumerable<string> tags, int weight)
    {
        foreach (var tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
            _tagWeights[tag] = _tagWeights.GetValueOrDefault(tag, 0) + Math.Max(0, weight);
    }
}
