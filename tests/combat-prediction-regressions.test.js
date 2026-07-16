const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const engine = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const statuses = fs.readFileSync('Domain/StatusEffects.cs', 'utf8');
const traits = fs.readFileSync('Domain/Traits.cs', 'utf8');
const preview = fs.readFileSync('Api/AttackPreviewService.cs', 'utf8');
const rolePreview = fs.readFileSync('Api/RoleActionPreviewService.cs', 'utf8');
const program = fs.readFileSync('Program.cs', 'utf8');
const app = fs.readFileSync('wwwroot/app.js', 'utf8');
const gdd = fs.readFileSync('GDD.md', 'utf8');
const zh = JSON.parse(fs.readFileSync('wwwroot/locales/zh.json', 'utf8'));
const ja = JSON.parse(fs.readFileSync('wwwroot/locales/ja.json', 'utf8'));

function methodBody(source, signature) {
  const start = source.indexOf(signature);
  assert.notEqual(start, -1, `${signature} not found`);
  const next = source.indexOf('\n    private ', start + signature.length);
  return source.slice(start, next === -1 ? source.length : next);
}

test('shared shield absorbs only its current value and lets overflow continue', () => {
  const applyShield = methodBody(engine, 'private static void ApplySharedShield');
  assert.match(applyShield, /packet\.Amount -= absorbed/);
  assert.match(applyShield, /packet\.BlockedBySharedShield = packet\.Amount <= 0/);
  assert.doesNotMatch(applyShield, /packet\.Amount = 0/);
  assert.match(preview, /damageAfterShieldDefense - shieldAbsorb/);
  assert.match(gdd, /超过剩余盾值的部分会穿透/);
  assert.match(zh.ui.shieldNote1, /穿透/);
  assert.match(ja.ui.shieldNote1, /貫通/);
});

test('rank 3 absolute follow-ups only reward kills caused by their own damage', () => {
  for (const signature of [
    'private void ResolveVictoryEdictAfterActiveAttack',
    'private void ResolveAbyssalBargainAfterActiveAttack'
  ]) {
    const body = methodBody(engine, signature);
    assert.match(body, /var followUpDefeatedTarget = false/);
    assert.match(body, /followUpDefeatedTarget = !target\.IsAlive/);
    assert.match(body, /if \(followUpDefeatedTarget\)/);
    assert.doesNotMatch(body, /if \(target\.CurrentHp <= 0\)/);
  }
});

test('attack forecast includes current rank 3 damage statuses and actual ordering', () => {
  assert.match(preview, /"astral-alignment" when canApplyOneShotStatuses && type == DamageType\.Magical/);
  assert.match(preview, /"militia-call" when canApplyOneShotStatuses && damageSource == DamageSource\.ActiveAttack/);
  assert.match(preview, /status is HuntedStatus hunted/);
  assert.match(statuses, /bool HasTriggeredFor\(Guid soldierId\)/);
  assert.match(preview, /status is PreyStatus or NightmarePreyStatus/);
  assert.ok(
    preview.indexOf('ApplyNoDamageTriggerForecast(state, defender, attackPacket)')
      < preview.indexOf('var chantRelicForecast = ForecastChantAndBurningRelics('),
    'no-damage prey effects must be forecast before post-damage relics'
  );
  assert.match(preview, /VictoryEdictStatus/);
  assert.match(preview, /AbyssalBargainStatus/);
  assert.match(preview, /RemainingSharedShield/);
  assert.match(preview, /sharedShieldOverride: remainingSharedShield/);
});

test('monster and prey absolute follow-ups require both morale and HP damage to be zero', () => {
  assert.match(traits, /AttackMoraleDamageDealt/);
  assert.match(traits, /exchange\.AttackDamageDealt != 0\s*\|\| exchange\.AttackMoraleDamageDealt != 0/);
  const prey = methodBody(engine, 'private void ResolvePreyNoDamage');
  assert.match(prey, /packet\.HpDamage != 0 \|\| packet\.MoraleDamage != 0/);
  const nightBait = methodBody(engine, 'private void TriggerNightBaitAfterDamage');
  assert.match(nightBait, /packet\.HpDamage != 0[\s\S]*?packet\.MoraleDamage != 0/);
  assert.match(preview, /forecast\.HpDamageMin > 0\s*\|\| forecast\.MoraleDamageMin > 0/);
  assert.match(preview, /forecast\.HpDamageMax == 0\s*&& forecast\.MoraleDamageMax == 0/);
  assert.match(zh.traits['predatory-instinct'].description, /士气与HP/);
  assert.match(ja.traits['predatory-instinct'].description, /士気・HP/);
});

test('jester and growth-path trait forecasts mirror declaration-time combat rules', () => {
  assert.match(preview, /jesterTraitWillApply[\s\S]*CombineForecasts\(reducedCounter, counterForecast\)/);
  const jesterEligibility = preview.match(/var jesterTraitWillApply =[\s\S]*?;/)?.[0] || '';
  assert.doesNotMatch(
    jesterEligibility,
    /SoldierRank/
  );
  assert.match(preview, /jesterTraitGuaranteed = jesterTraitWillApply && attacker\.SoldierRank >= 2/);
  assert.match(preview, /HeroRankRules\.HasRank2Path\(attacker, "arcane-channel"\)/);
  assert.match(preview, /preview\.trait\.aftershockRage/);
  assert.match(preview, /_engine\.GetActiveAttack\(state, attacker\)/);
});

test('role actions expose structured numeric previews before execution', () => {
  for (const id of ['thread-cut', 'starfall', 'archive-formula', 'dragon-breaker', 'iron-charge'])
    assert.match(rolePreview, new RegExp(`case "${id}"`));
  assert.match(program, /api\/online\/game\/role-action\/preview/);
  assert.match(program, /api\/game\/role-action\/preview/);
  assert.match(app, /pendingRoleActionExecution/);
  assert.match(app, /async function previewRoleAction/);
  assert.match(app, /async function executePreview/);
  for (const status of ['burning', 'trembling', 'strong-attack', 'magic-surge', 'spell-ward', 'fortify'])
    assert.match(rolePreview, new RegExp(`"${status}"`));
});

test('role action damage resolves and forecasts astral alignment collateral', () => {
  const dealRoleActionDamage = methodBody(engine, 'private int DealRoleActionDamage');
  assert.match(dealRoleActionDamage, /packet\.Collateral/);
  assert.match(dealRoleActionDamage, /ResolveCollateralDamage/);
  assert.match(dealRoleActionDamage, /shieldRemainingAfterMainPacket/);
  assert.match(dealRoleActionDamage, /ApplyPendingRelicActionPointRefunds/);
  const collateral = methodBody(engine, 'private DamagePacket ResolveCollateralDamage');
  assert.match(collateral, /CanConsumeChargeStatuses = false/);
  assert.match(rolePreview, /ForecastAstralAlignmentSplash/);
  assert.match(preview, /sharedShieldOverride/);
});
