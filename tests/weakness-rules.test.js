const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const statusSource = fs.readFileSync('Domain/StatusEffects.cs', 'utf8');
const skillSource = fs.readFileSync('Domain/Skills.cs', 'utf8');
const previewSource = fs.readFileSync('Api/AttackPreviewService.cs', 'utf8');
const assetManifest = JSON.parse(fs.readFileSync('wwwroot/config/ui-assets.json', 'utf8'));

function classBody(source, className) {
  const start = source.indexOf(`class ${className}`);
  assert.notEqual(start, -1, `${className} class not found`);
  const next = source.indexOf('\npublic sealed class ', start + 1);
  return source.slice(start, next === -1 ? source.length : next);
}

test('weakening spores dispels one attack buff immediately and keeps delayed weakness -2', () => {
  const beastRageStatus = classBody(statusSource, 'BeastRageStatus');
  const magicPowerStatus = classBody(statusSource, 'MagicPowerStatus');
  const harvestStatus = classBody(statusSource, 'HarvestStatus');
  const pendingHarvestStatus = classBody(statusSource, 'PendingHarvestStatus');

  assert.match(statusSource, /class WeaknessStatus[\s\S]*?Magnitude => 2;[\s\S]*?damage - Magnitude/);
  assert.match(statusSource, /virtual bool IsAttackBuff => false/);
  assert.match(statusSource, /virtual bool IsDispellable => true/);
  assert.doesNotMatch(statusSource, /IsAttackEnhancement/);
  assert.doesNotMatch(statusSource, /IsAttackBuffDispelTarget/);
  assert.match(beastRageStatus, /IsAttackBuff => true/);
  assert.match(beastRageStatus, /IsDispellable => false/);
  assert.match(magicPowerStatus, /IsAttackBuff => true/);
  assert.doesNotMatch(magicPowerStatus, /IsDispellable => false/);
  assert.match(harvestStatus, /IsAttackBuff => true/);
  assert.doesNotMatch(harvestStatus, /IsDispellable => false/);
  assert.doesNotMatch(pendingHarvestStatus, /IsDispellable => false/);
  assert.doesNotMatch(pendingHarvestStatus, /IsAttackBuff => true/);
  assert.match(skillSource, /status\.IsBuff[\s\S]*?status\.IsDispellable/);
  assert.match(skillSource, /context\.Next\(attackBuffs\.Count\)/);
  assert.match(skillSource, /Statuses\.Remove\(attackBuff\)/);
  assert.match(skillSource, /new PendingWeaknessStatus\(owner\.Id, exchange\.Defender\.PlayerId\)/);
  assert.match(skillSource, /status\.Id == "magic-power"/);
  assert.doesNotMatch(statusSource, /ExtendedWeaknessStatus/);
  assert.doesNotMatch(previewSource, /weaknessEnragedMonster/);
  assert.equal(assetManifest.bindings.statuses['magic-power'], 'status.magic-power');
  assert.equal(assetManifest.bindings.statuses['weakness-extended'], undefined);
});
