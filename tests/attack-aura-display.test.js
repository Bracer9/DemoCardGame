const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const engine = fs.readFileSync('Domain/GameEngine.cs', 'utf8');
const dtos = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const preview = fs.readFileSync('Api/AttackPreviewService.cs', 'utf8');
const app = fs.readFileSync('wwwroot/app.js', 'utf8');

test('attack auras share the effective attack path used by cards and previews', () => {
  assert.match(engine, /GetEffectiveActiveAttack\(GameState state, CharacterState character\)/);
  assert.match(engine, /Amount = GetEffectiveActiveAttack\(state, attacker\)/);
  assert.match(engine, /GetAttackAuraBonus\(GameState state, CharacterState character\)/);
  assert.match(dtos, /int Attack, int BaseAttack, int AttackAuraBonus/);
  assert.match(dtos, /GetEffectiveActiveAttack\(state, character\)/);
  assert.match(dtos, /GetBaseAttack\(character\), attackAuraBonus/);
  assert.match(preview, /GetEffectiveActiveAttack\(state, attacker\)/);
  assert.match(app, /function effectiveCardAttack\(card\)/);
  assert.match(app, /baseAttack \+ auraBonus/);
  assert.match(app, /<strong>\$\{displayedAttack\}<\/strong>/);
});
