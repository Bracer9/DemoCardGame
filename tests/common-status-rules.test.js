const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const statusSource = fs.readFileSync('Domain/StatusEffects.cs', 'utf8');
const traitSource = fs.readFileSync('Domain/Traits.cs', 'utf8');
const previewSource = fs.readFileSync('Api/AttackPreviewService.cs', 'utf8');
const assetManifest = JSON.parse(fs.readFileSync('wwwroot/config/ui-assets.json', 'utf8'));

function classBody(source, className) {
  const start = source.indexOf(`class ${className}`);
  assert.notEqual(start, -1, `${className} class not found`);
  const next = source.indexOf('\npublic sealed class ', start + 1);
  return source.slice(start, next === -1 ? source.length : next);
}

test('common statuses and aura dispel rules are wired correctly', () => {
  const beastRageStatus = classBody(statusSource, 'BeastRageStatus');
  const magicPowerStatus = classBody(statusSource, 'MagicPowerStatus');
  const harvestStatus = classBody(statusSource, 'HarvestStatus');
  const pendingHarvestStatus = classBody(statusSource, 'PendingHarvestStatus');
  const guardOathStatus = classBody(statusSource, 'GuardOathStatus');

  assert.match(statusSource, /class ExhaustionStatus[\s\S]*?DamageType\.Physical[\s\S]*?packet\.Amount = Math\.Max\(1, packet\.Amount \/ 2\)/);
  assert.match(statusSource, /class ErosionStatus[\s\S]*?DamageType\.Magical[\s\S]*?packet\.Amount = Math\.Max\(1, packet\.Amount \/ 2\)/);
  assert.doesNotMatch(statusSource, /class WeaknessStatus/);
  assert.doesNotMatch(statusSource, /class PendingWeaknessStatus/);
  assert.match(statusSource, /virtual bool IsAttackBuff => false/);
  assert.match(statusSource, /virtual bool IsDispellable => true/);
  assert.doesNotMatch(statusSource, /IsAttackEnhancement/);
  assert.doesNotMatch(statusSource, /IsAttackBuffDispelTarget/);
  assert.match(beastRageStatus, /IsAttackBuff => true/);
  assert.match(beastRageStatus, /IsDispellable => false/);
  assert.match(magicPowerStatus, /StatusEffect\("magic-power", false, sourceCharacterId\)/);
  assert.doesNotMatch(magicPowerStatus, /IsAttackBuff => true/);
  assert.match(magicPowerStatus, /IsDispellable => false/);
  assert.match(harvestStatus, /IsAttackBuff => true/);
  assert.doesNotMatch(harvestStatus, /IsDispellable => false/);
  assert.doesNotMatch(pendingHarvestStatus, /IsDispellable => false/);
  assert.doesNotMatch(pendingHarvestStatus, /IsAttackBuff => true/);
  assert.match(traitSource, /status\.IsBuff[\s\S]*?status\.IsDispellable/);
  assert.match(traitSource, /context\.Next\(attackBuffs\.Count\)/);
  assert.match(traitSource, /Statuses\.Remove\(attackBuff\)/);
  assert.match(traitSource, /new ExhaustionStatus\(owner\.Id, exchange\.Defender\.PlayerId\)/);
  assert.match(traitSource, /new ErosionStatus\(owner\.Id, exchange\.Defender\.PlayerId\)/);
  assert.match(traitSource, /status\.Id == "magic-power"/);
  assert.match(guardOathStatus, /StatusEffect\("guard-oath", true, sourceCharacterId\)/);
  assert.match(guardOathStatus, /public int Stacks/);
  assert.match(guardOathStatus, /packet\.Source != DamageSource\.ActiveAttack/);
  assert.doesNotMatch(guardOathStatus, /IsDispellable => false/);
  assert.match(previewSource, /ForecastOutgoingDamage/);
  assert.match(previewSource, /"chant" when type == DamageType\.Magical => damage \* 2/);
  assert.match(previewSource, /"guard-oath" when source == DamageSource\.ActiveAttack && type == DamageType\.Physical/);
  assert.doesNotMatch(previewSource, /weaknessEnragedMonster/);
  assert.equal(assetManifest.bindings.statuses['magic-power'], 'status.magic-power');
  assert.equal(assetManifest.bindings.statuses.exhaustion, 'status.exhaustion');
  assert.equal(assetManifest.bindings.statuses.erosion, 'status.erosion');
  assert.equal(assetManifest.bindings.statuses['guard-oath'], 'status.guard-oath');
});
