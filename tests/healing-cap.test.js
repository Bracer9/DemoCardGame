const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const engineSource = fs.readFileSync('Domain/GameEngine.cs', 'utf8');

test('healing does not lower hp that is already above the normal cap', () => {
  assert.match(engineSource, /private int Heal\(CharacterState target, int amount\)/);
  assert.match(engineSource, /var cappedHp = Math\.Min\(GetMaxHp\(target\), target\.CurrentHp \+ amount\);/);
  assert.match(engineSource, /target\.CurrentHp = Math\.Max\(target\.CurrentHp, cappedHp\);/);
  assert.doesNotMatch(engineSource, /CurrentHp\s*=\s*Math\.Min\(GetMaxHp\([^)]*\),[^;]*CurrentHp\s*\+/);
});

test('mend uses the shared healing helper for its hp recovery', () => {
  assert.match(engineSource, /private void UseMend[\s\S]*var healed = Heal\(target, 3\);[\s\S]*L10n\.Raw\(healed\)/);
});
