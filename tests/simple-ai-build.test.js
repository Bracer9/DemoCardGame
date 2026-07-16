const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const ai = fs.readFileSync('Services/SimpleAiService.cs', 'utf8');
const profile = fs.readFileSync('Services/AiBuildProfile.cs', 'utf8');
const deputies = fs.readFileSync('Domain/DeputyDefinitions.cs', 'utf8');

test('AI build profile reuses hero, relic, and soldier/deputy tags', () => {
  assert.match(profile, /HeroGrowthCatalog\.Find\(character\)/);
  assert.match(profile, /profile\.Add\(selectedPath\.RelicTags, 4\)/);
  assert.match(profile, /profile\.Add\(relic\.BuildTags, 3\)/);
  assert.match(profile, /profile\.Add\(deputy\.BuildTags/);
  assert.match(deputies, /IReadOnlyList<string> BuildTags/);
});

test('AI uses soft weighted randomness for build choices instead of a fixed best option', () => {
  assert.match(ai, /private T ChooseWeighted<T>/);
  assert.match(ai, /var roll = _engine\.Next\(total\)/);
  assert.match(ai, /ChooseWeighted\(weightedChoices/);
  assert.match(ai, /ChooseWeighted\(relicChoices/);
  assert.match(ai, /ChooseWeighted\(rewardChoices/);
  assert.match(ai, /ChooseWeighted\(candidates, candidate => Math\.Clamp\(candidate\.Score, 1, 120\)\)/);
});

test('AI recruit and deputy choices are build-aware but reject useless attack stat matches', () => {
  assert.match(ai, /profile\.Score\(AiBuildProfile\.GetHeroTags/);
  assert.match(ai, /profile\.Score\(AiBuildProfile\.GetSoldierTags/);
  assert.match(ai, /profile\.Score\(relic\.BuildTags\) \* 2/);
  assert.match(ai, /TryAssignDeputy\(state\)/);
  assert.match(ai, /DeputyStatKind\.PhysicalAttack when attackType == DamageType\.Physical/);
  assert.match(ai, /DeputyStatKind\.MagicalAttack when attackType == DamageType\.Magical/);
  assert.match(ai, /DeputyStatKind\.PhysicalAttack or DeputyStatKind\.MagicalAttack => -100/);
});
