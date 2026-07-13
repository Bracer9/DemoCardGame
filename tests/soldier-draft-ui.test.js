const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const app = fs.readFileSync('wwwroot/app.js', 'utf8');
const styles = fs.readFileSync('wwwroot/styles.css', 'utf8');
const engine = fs.readFileSync('Domain/GameEngine.cs', 'utf8');

test('soldier draft cards reuse the battlefield attack display language', () => {
  assert.match(app, /class="hero-draft-card-attack"/);
  assert.match(app, /damageTypeGlyph\(candidate\.attackType\)/);
  assert.match(styles, /\.hero-draft-card-attack[\s\S]*?left:var\(--card-attack-left\);[\s\S]*?top:var\(--card-attack-top\);/);
  assert.match(styles, /\.hero-draft-card-attack>span::before\s*\{[\s\S]*?content:"⚔";/);
  assert.match(styles, /\.hero-draft-card \.hero-draft-card-attack>strong[\s\S]*?font-size:var\(--card-attack-font-size\);/);
  assert.match(styles, /\.hero-draft-card \.hero-draft-card-attack>\.attack-type-label[\s\S]*?font-size:var\(--card-attack-type-size\);/);
});

test('jester is the final soldier draft candidate', () => {
  assert.match(engine, /CharacterCatalog\.Soldiers[\s\S]*?OrderBy\(definition => definition\.Key == "jester" \? 1 : 0\)[\s\S]*?ThenBy\(definition => definition\.Key\)/);
});
