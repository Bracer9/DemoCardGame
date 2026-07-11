const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const rewardDefinitions = fs.readFileSync('Domain/RewardDefinitions.cs', 'utf8');
const relicDefinitions = fs.readFileSync('Domain/RelicDefinitions.cs', 'utf8');
const heroGrowthDefinitions = fs.readFileSync('Domain/HeroGrowthDefinitions.cs', 'utf8');
const gameState = fs.readFileSync('Domain/GameState.cs', 'utf8');
const gameEngine = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const simpleAiService = fs.readFileSync('Services/SimpleAiService.cs', 'utf8');
const gameDtos = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');
const stylesSource = fs.readFileSync('wwwroot/styles.css', 'utf8');
const zhLocale = JSON.parse(fs.readFileSync('wwwroot/locales/zh.json', 'utf8'));
const jaLocale = JSON.parse(fs.readFileSync('wwwroot/locales/ja.json', 'utf8'));
const uiAssets = JSON.parse(fs.readFileSync('wwwroot/config/ui-assets.json', 'utf8'));

test('relic rewards use a top-level category and a pending child menu', () => {
  assert.match(rewardDefinitions, /RelicChoice/);
  assert.match(rewardDefinitions, /new\("relic-choice", 0, "category", RewardKind\.RelicChoice\)/);
  assert.match(gameState, /public sealed class PendingRelicRewardState/);
  assert.match(gameState, /public PendingRelicRewardState\? PendingRelicReward/);
  assert.match(gameEngine, /definition\.Kind == RewardKind\.RelicChoice/);
  assert.match(gameEngine, /state\.PendingRelicReward = new PendingRelicRewardState/);
  const relicChoiceBranch = gameEngine.match(/else if \(definition\.Kind == RewardKind\.RelicChoice\)[\s\S]*?\n        \}/)?.[0] || '';
  assert.doesNotMatch(relicChoiceBranch, /RefreshRelicRewardOptions/);
  assert.match(relicChoiceBranch, /state\.PendingRelicReward\.Options\.AddRange\(window\.RelicOptions\)/);
  assert.match(gameEngine, /SelectPendingRelicReward/);
  assert.match(gameEngine, /ConfirmRewardPurchase\(state, window, player, option\);[\s\S]*?ApplyDummyReward\(state, player, option\.RewardId\);[\s\S]*?pending\.Options\.Remove\(option\);/);
  assert.match(gameEngine, /public void ResetRewardWindow\(GameState state\)[\s\S]*?ResetPendingRelicReward\(state, window, pendingRelic, player\);[\s\S]*?throw new GameRuleException\(L10n\.Text\("error\.rewardResetUnavailable"\)\);/);
  assert.match(gameEngine, /RelicEffects\.AddRelic\(player, rewardId\)/);
  assert.match(gameState, /public List<PlayerRelicState> Relics/);
  assert.match(gameDtos, /public sealed record RelicView\(string Id\)/);
  assert.match(gameDtos, /IReadOnlyList<RelicView> Relics/);
  assert.match(gameDtos, /player\.Relics\.Select\(relic => new RelicView\(relic\.Id\)\)\.ToArray\(\)/);
  assert.match(gameDtos, /public sealed record PendingRelicRewardView/);
  assert.match(appSource, /const isRelicChild = Boolean\(reward && relicReward\?\.canChoose\)/);
  assert.match(appSource, /ui\.rewardSkip\.hidden = isRelicChild/);
  assert.match(appSource, /ui\.rewardReset\.hidden = !isRelicChild/);
  assert.match(appSource, /rewardInlineBack/);
  assert.match(appSource, /function renderRelicOverview\(player\)/);
  assert.match(appSource, /ui\.relicOverviewButton\.innerHTML/);
  assert.doesNotMatch(appSource, /inspector-relics/);
  assert.doesNotMatch(stylesSource, /\.relic-overview\s*\{[\s\S]*?z-index\s*:\s*220/);
  assert.match(stylesSource, /\.relic-overview\s*\{[\s\S]*?z-index\s*:\s*23/);
  assert.match(stylesSource, /body:not\(\.touch-mode\) \.relic-overview:hover,[\s\S]*?\.relic-overview\.expanded\s*\{[\s\S]*?z-index\s*:\s*240/);
  assert.match(stylesSource, /\.relic-overview-detail\s*\{[\s\S]*?z-index\s*:\s*96/);
  assert.match(stylesSource, /\.relic-overview-detail\s*\{[\s\S]*?font-family\s*:\s*var\(--font-hand\)/);
  assert.match(stylesSource, /\.relic-overview-detail header span,[\s\S]*?\.relic-overview-detail li p\s*\{[\s\S]*?font-family\s*:\s*var\(--font-hand\)/);
  assert.match(stylesSource, /\.reward-window,[\s\S]*?\.relic-overview-detail\s*\{[\s\S]*?font-family\s*:\s*var\(--font-hand\)/);
});

test('formal relic pool is wired into reward choices', () => {
  assert.match(rewardDefinitions, /public static IReadOnlyList<RewardDefinition> RelicRewards/);
  assert.match(rewardDefinitions, /RelicCatalog\.All/);
  assert.match(gameEngine, /RewardCatalog\.RelicRewards/);
  assert.doesNotMatch(gameEngine, /RewardCatalog\.DummyRewards\s*[\r\n\s]*\.OrderBy/);
  assert.match(gameEngine, /RelicCatalog\.Find\(reward\.Id\)/);
  assert.match(gameEngine, /relic-ashen-detonator/);
  assert.match(gameEngine, /relic-red-hourglass/);
  assert.equal(uiAssets.icons['relic.funeral-coin'].source, 'relics/relic-funeral-coin.png');
  assert.doesNotMatch(appSource, /relic-silver-ward-charm/);
  assert.match(appSource, /kind === 'reward'/);
});

test('all relic rarities stay eligible with only soft hero and late-epic weights', () => {
  assert.doesNotMatch(relicDefinitions, /RelicStage/);
  assert.doesNotMatch(gameEngine, /relic\.Stage|Stage <= maxStage/);
  assert.match(gameEngine, /RelicBaseSelectionWeight = 10/);
  assert.match(gameEngine, /RelicEarlyEpicSelectionWeight = 5/);
  assert.match(gameEngine, /RelicHeroTagSelectionMultiplier = 2/);
  assert.match(gameEngine, /EpicRelicWeightBonusRound = 12/);
  assert.match(gameEngine, /\.Where\(reward => !owned\.Contains\(reward\.Id\)\)[\s\S]*?\.ToArray\(\)/);
  assert.match(gameEngine, /relic\.BuildTags\.Any\(heroTags\.Contains\)/);
  assert.match(gameEngine, /weight \*= RelicHeroTagSelectionMultiplier/);
  assert.match(gameEngine, /string\.Equals\(reward\.Rarity, "epic", StringComparison\.OrdinalIgnoreCase\)/);
  assert.match(gameEngine, /SelectWeightedRelicRewards\(candidates, player, roundNumber, 3\)/);
  assert.match(heroGrowthDefinitions, /character\.HeroRank == 0 \|\| selectedPath is null/);
  assert.match(heroGrowthDefinitions, /SelectMany\(path => path\.RelicTags\)/);
});

test('AI only enters the relic menu when one of the offered relics is affordable', () => {
  assert.match(simpleAiService, /option\.RewardId != "relic-choice"[\s\S]*?reward\.RelicOptions\.Any\(relic => player\.BattlePoints\.Current >= relic\.Cost\)/);
  assert.doesNotMatch(simpleAiService, /RelicCatalog\.All\.Any/);
});

test('formal relic catalog contains exactly the designed 25 relics with matching localization', () => {
  const expected = [
    'relic-apprentice-star-ink', 'relic-mason-token', 'relic-red-whetstone',
    'relic-muster-papers', 'relic-mercy-cup', 'relic-witch-bell',
    'relic-ember-astrolabe', 'relic-hollow-comet-lens', 'relic-white-lily-censer',
    'relic-duelist-ticket', 'relic-command-sergeant-seal', 'relic-night-bait',
    'relic-command-table', 'relic-echo-crystal', 'relic-green-standard', 'relic-blood-coin',
    'relic-astral-prism', 'relic-ashen-detonator', 'relic-plague-codex',
    'relic-predator-crown', 'relic-red-hourglass', 'relic-kingwall-standard',
    'relic-saint-chalice', 'relic-company-standard', 'relic-funeral-coin'
  ].sort();
  const actual = [...relicDefinitions.matchAll(/new\("(relic-[a-z0-9-]+)"/g)]
    .map(match => match[1])
    .sort();
  assert.deepEqual(actual, expected);
  for (const locale of [zhLocale, jaLocale]) {
    const localized = Object.keys(locale.rewards)
      .filter(id => id.startsWith('relic-') && id !== 'relic-choice')
      .sort();
    assert.deepEqual(localized, expected);
  }
});

test('all formal relics use their dedicated UI artwork', () => {
  const relicIds = [...relicDefinitions.matchAll(/new\("(relic-[a-z0-9-]+)"/g)].map(match => match[1]);
  for (const relicId of relicIds) {
    const assetId = `relic.${relicId.slice('relic-'.length)}`;
    const asset = uiAssets.icons[assetId];
    assert.ok(asset, `missing UI asset manifest entry for ${relicId}`);
    assert.equal(asset.category, 'relic');
    assert.equal(asset.source, `relics/${relicId}.png`);
  }
  assert.match(appSource, /startsWith\('relic-'\)/);
  assert.match(appSource, /option\.kind !== 'RelicChoice'/);
  assert.match(appSource, /reward-relic-art/);
  assert.match(stylesSource, /\.reward-relic-card \.reward-relic-art[\s\S]*?background:transparent !important/);
});

test('new relic engines are connected to their shared battle events', () => {
  assert.match(gameEngine, /relic-muster-papers[\s\S]*?reward\.Cost - 1/);
  assert.match(gameEngine, /TriggerActiveHealingRelics[\s\S]*?relic-saint-chalice[\s\S]*?relic-mercy-cup/);
  assert.match(gameEngine, /NotifyDebuffApplied[\s\S]*?relic-plague-codex[\s\S]*?relic-witch-bell/);
  assert.match(gameEngine, /TriggerRoleActionRelics[\s\S]*?relic-command-sergeant-seal[\s\S]*?relic-command-table/);
  assert.match(gameEngine, /TriggerNightBaitAfterDamage[\s\S]*?relic-night-bait/);
  assert.match(gameEngine, /ResolveEchoCrystalAfterDamageSequence[\s\S]*?relic-echo-crystal/);
  assert.match(gameEngine, /relic-green-standard[\s\S]*?RefundActionPointFromRelic/);
  assert.match(gameEngine, /TriggerHpPaymentRelic[\s\S]*?relic-blood-coin/);
  assert.match(gameEngine, /packet\.ConsumedChant[\s\S]*?relic-astral-prism/);
  assert.match(gameEngine, /relic-ashen-detonator[\s\S]*?stacks \* 2/);
  assert.match(gameEngine, /ApplyPredatorCrown[\s\S]*?relic-predator-crown/);
  assert.match(gameEngine, /PhysicalActiveAttacksTakenThisTurn != 2[\s\S]*?relic-red-hourglass/);
  assert.match(gameEngine, /highestPhysicalDefense[\s\S]*?highestMagicalDefense[\s\S]*?relic-kingwall-standard/);
  assert.match(gameEngine, /ApplyCompanyStandard[\s\S]*?relic-company-standard/);
  assert.match(gameEngine, /HasActiveFuneralCoin[\s\S]*?relic-funeral-coin/);
});
