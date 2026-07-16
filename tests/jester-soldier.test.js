const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const characterDefinitions = fs.readFileSync('Domain/CharacterDefinition.cs', 'utf8');
const deputyDefinitions = fs.readFileSync('Domain/DeputyDefinitions.cs', 'utf8');
const traits = fs.readFileSync('Domain/Traits.cs', 'utf8');
const roleActions = fs.readFileSync('Domain/RoleActions.cs', 'utf8');
const engine = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const simpleAi = fs.readFileSync('Services/SimpleAiService.cs', 'utf8');
const dtos = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const preview = fs.readFileSync('Api/AttackPreviewService.cs', 'utf8');
const uiAssets = JSON.parse(fs.readFileSync('wwwroot/config/ui-assets.json', 'utf8'));
const zh = JSON.parse(fs.readFileSync('wwwroot/locales/zh.json', 'utf8'));
const ja = JSON.parse(fs.readFileSync('wwwroot/locales/ja.json', 'utf8'));

test('jester soldier is wired through rank, role action, deputy, aura, and preview paths', () => {
  assert.match(characterDefinitions, /new\("jester", CardType\.Soldier/);
  assert.match(characterDefinitions, /New_Portraits\/Jester\.png/);
  assert.match(characterDefinitions, /New_Portraits\/MasqueJester\.png/);
  assert.match(traits, /class MaliciousJestTrait/);
  assert.match(traits, /DebuffApplicationChancePercent = 50/);
  assert.match(traits, /!guaranteed[\s\S]*?source\.SoldierRank < 2[\s\S]*?!context\.Roll\(DebuffApplicationChancePercent \/ 100\.0\)/);
  assert.match(traits, /OnAfterExchange[\s\S]*?exchange\.AttackDamageDealt <= 0[\s\S]*?ApplyOutputDebuff\(context, owner, exchange\.Defender, guaranteed: true\)/);
  assert.match(traits, /AttackMarker[\s\S]*?TraitsUsedThisTurn\.Add\(Metadata\.Id\)/);
  assert.match(traits, /GetMatchingOutputDebuffId\(target\)/);
  assert.match(traits, /FindOwner\(target\)\.SharedShield > 0[\s\S]*?TraitsUsedThisTurn\.Add\(Metadata\.Id\)/);
  assert.match(traits, /Math\.Abs\(character\.Slot - target\.Slot\) ===? 1/);
  assert.match(roleActions, /"mocking-curtain-call"/);
  assert.match(engine, /"jester" => "mocking-curtain-call"/);
  assert.match(engine, /ApplyJesterAura\(state, packet\)/);
  assert.match(engine, /TriggerDeputyJesterAfterEnemyAction/);
  assert.match(deputyDefinitions, /new\("deputy-jester", "jester", DeputyStatKind\.Attack, 1,[\s\S]*?\["debuff", "control", "role-action", "support", "soldier"\]\)/);
  assert.match(dtos, /malicious-jest-aura/);
  assert.match(preview, /jesterAuraBonus/);
  assert.match(preview, /ForecastJesterTrait\(state, attacker, defender, attack\)/);
  assert.match(preview, /attack\.HpDamageMin > 0/);
  assert.match(preview, /preview\.trait\.maliciousJestHpGuaranteed/);
  assert.match(preview, /preview\.trait\.maliciousJestHpPossible/);
  assert.match(preview, /CombineForecasts\(reducedCounter, counterForecast\)/);
  assert.match(preview, /jesterAuraBonusPossible[\s\S]*?CombineForecasts\(attackPacket, boostedAttackPacket\)/);
  assert.match(preview, /jesterTraitGuaranteed[\s\S]*?\? reducedCounter/);
  assert.match(preview, /defenderOwner\.SharedShield <= 0[\s\S]*?malicious-jest/);
});

test('jester localization and icon binding are complete in both languages', () => {
  for (const locale of [zh, ja]) {
    assert.ok(locale.characters.jester);
    assert.ok(locale.deputies['deputy-jester']);
    assert.ok(locale.roleActions['mocking-curtain-call']);
    assert.ok(locale.traits['malicious-jest']);
    assert.ok(locale.traits['malicious-jest'].ranks['2']);
    assert.ok(locale.statuses['malicious-jest-aura']);
    assert.ok(locale.messages['preview.trait.jesterAura']);
    assert.ok(locale.messages['preview.trait.maliciousJestHpGuaranteed']);
    assert.ok(locale.messages['preview.trait.maliciousJestHpPossible']);
  }
  assert.match(zh.traits['malicious-jest'].description, /力竭.*磨损/);
  assert.match(zh.messages['preview.trait.maliciousJestHpGuaranteed'], /力竭.*磨损/);
  assert.match(ja.traits['malicious-jest'].description, /力尽き.*摩耗/);
  assert.match(ja.messages['preview.trait.maliciousJestHpGuaranteed'], /力尽き.*摩耗/);
  assert.ok(uiAssets.icons['status.malicious-jest-aura']);
  assert.equal(uiAssets.bindings.statuses['malicious-jest-aura'], 'status.malicious-jest-aura');
  assert.equal(uiAssets.icons['trait.malicious-jest'].source, 'traits/malicious-jest.png');
  assert.equal(uiAssets.bindings.traits['malicious-jest'], 'trait.malicious-jest');
});

test('jester is included in the AI soldier preference order', () => {
  assert.match(simpleAi, /\["shieldmaiden"\]\s*=\s*90/);
  assert.match(simpleAi, /\["cleric"\]\s*=\s*82/);
  assert.match(simpleAi, /\["duelist"\]\s*=\s*74/);
  assert.match(simpleAi, /\["arcanist"\]\s*=\s*70/);
  assert.match(simpleAi, /\["jester"\]\s*=\s*68/);
});
