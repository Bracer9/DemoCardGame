const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const gameTypesSource = fs.readFileSync('Domain/GameTypes.cs', 'utf8');
const engineSource = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const roleActionsSource = fs.readFileSync('Domain/RoleActions.cs', 'utf8');
const roleActionPreviewSource = fs.readFileSync('Api/RoleActionPreviewService.cs', 'utf8');
const programSource = fs.readFileSync('Program.cs', 'utf8');
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
  assert.match(roleActionPreviewSource, /case "starfall"[\s\S]*ForecastDamage/);
  assert.match(programSource, /game\/role-action\/preview/);
  assert.match(appSource, /previewRoleAction[\s\S]*game\/role-action\/preview/);
  assert.match(zh.roleActions.starfall.description, /当前攻击力/);
  assert.match(ja.roleActions.starfall.description, /現在攻撃力/);
});

test('iron charge restores shared shield equal to actual HP damage', () => {
  const ironCharge = methodBody(engineSource, 'void UseIronCharge');

  assert.match(ironCharge, /IncreaseSharedShield\(state, owner, hpDamage, actor\)/);
  assert.doesNotMatch(ironCharge, /Ceiling\(hpDamage \/ 2\.0\)/);
  assert.match(zh.roleActions['iron-charge'].description, /等同该HP伤害/);
  assert.match(ja.roleActions['iron-charge'].description, /同じ値/);
});
