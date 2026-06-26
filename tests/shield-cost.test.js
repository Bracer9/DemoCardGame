const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const engineSource = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const dtoSource = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');

test('shield costs depend on whether a shared shield still exists', () => {
  assert.match(engineSource, /MaxActionPoints\s*=\s*5/);
  assert.match(engineSource, /FirstShieldCost\s*=\s*2/);
  assert.match(engineSource, /ReinforcedShieldCost\s*=\s*1/);
  assert.match(engineSource, /ReinforcedShieldBonus\s*=\s*2/);
  assert.match(engineSource, /CanReinforceShield\(PlayerState player\)/);
  assert.match(engineSource, /player\.SharedShield\s*\+\s*ReinforcedShieldBonus/);
  assert.doesNotMatch(engineSource, /ReinforcedShieldValue/);
  assert.match(engineSource, /state\.ActionPoints\s*-=\s*shieldCost/);
  assert.match(dtoSource, /int NextShieldCost/);
  assert.match(dtoSource, /GameEngine\.GetShieldCost\(state\.ActivePlayer\.ShieldDeploymentsThisTurn,\s*state\.ActivePlayer\.SharedShield\)/);
  assert.match(appSource, /game\.nextShieldCost/);
  assert.match(appSource, /canReinforceShield\s*=\s*game\.shieldDeploymentsThisTurn > 0 && activeSharedShield > 0/);
  assert.match(appSource, /SHIELD \+2/);
  assert.doesNotMatch(appSource, /<span>1 AP \/ SHIELD 2<\/span>/);
  assert.doesNotMatch(appSource, /SHIELD 4/);
});
