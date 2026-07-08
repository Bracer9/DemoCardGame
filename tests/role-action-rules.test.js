const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const gameTypesSource = fs.readFileSync('Domain/GameTypes.cs', 'utf8');
const engineSource = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const roleActionsSource = fs.readFileSync('Domain/RoleActions.cs', 'utf8');
const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');
const zh = JSON.parse(fs.readFileSync('wwwroot/locales/zh.json', 'utf8'));
const ja = JSON.parse(fs.readFileSync('wwwroot/locales/ja.json', 'utf8'));

function methodBody(source, methodName) {
  const start = source.indexOf(`private ${methodName}`);
  assert.notEqual(start, -1, `${methodName} not found`);
  const next = source.indexOf('\n    private ', start + 1);
  return source.slice(start, next === -1 ? source.length : next);
}

test('role action damage has a distinct source and morale-aware effect logs', () => {
  const dealRoleActionDamage = methodBody(engineSource, 'int DealRoleActionDamage');
  const logEffectDamage = methodBody(engineSource, 'void LogEffectDamage');

  assert.match(gameTypesSource, /RoleAction/);
  assert.match(dealRoleActionDamage, /Source = DamageSource\.RoleAction/);
  assert.doesNotMatch(dealRoleActionDamage, /Source = DamageSource\.Trait/);
  assert.match(logEffectDamage, /log\.effectDamageWithMorale/);
  assert.match(logEffectDamage, /packet\.FinalCharacterDamage/);
  assert.match(logEffectDamage, /packet\.MoraleDamage/);
  assert.equal(zh.messages['log.effectDamageWithMorale'].includes('士气'), true);
  assert.equal(ja.messages['log.effectDamageWithMorale'].includes('士気'), true);
  assert.match(appSource, /case 'log\.effectDamageWithMorale'/);
});

test('starfall is a 1 AP attack-scaling role action with a systemic chant payoff', () => {
  const starfall = methodBody(engineSource, 'void UseStarfall');

  assert.match(roleActionsSource, /"starfall"[\s\S]*RoleActionTargetKind\.EnemyCard\], 1,/);
  assert.match(starfall, /Math\.Max\(1, GetActiveAttack\(state, actor\)\)/);
  assert.doesNotMatch(starfall, /ConsumeStack/);
  assert.match(starfall, /DealRoleActionDamage\(state, target, damage, DamageType\.Magical/);
  assert.doesNotMatch(starfall, /var damage = 2/);
  assert.doesNotMatch(appSource, /roleActionPrediction/);
  assert.match(zh.roleActions.starfall.description, /当前攻击力/);
  assert.match(ja.roleActions.starfall.description, /現在攻撃力/);
});
