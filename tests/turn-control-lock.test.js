const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');

test('local controls stay locked until the turn curtain finishes', () => {
  assert.match(appSource, /let turnCurtainLock = false;/);
  assert.match(appSource, /function canUseLocalControls\(\)[\s\S]*game\?\.canControl && !turnCurtainLock/);
  assert.match(appSource, /async function playTurnCurtain[\s\S]*setTurnCurtainLock\(true\)[\s\S]*finally[\s\S]*setTurnCurtainLock\(false\)/);
  assert.match(appSource, /ui\.app\.classList\.toggle\('turn-locked', !localControls\)/);
  assert.match(appSource, /ui\.endTurn\.disabled = !localControls/);
  assert.match(appSource, /ui\.shieldButton\.disabled = shieldUnavailable/);
  assert.match(appSource, /if \(oldActivePlayerId && oldActivePlayerId !== game\.activePlayerId\)[\s\S]*turnCurtainLock = true;[\s\S]*render\(\);/);
  assert.match(appSource, /if \(oldActive && oldActive !== game\.activePlayerId\)[\s\S]*turnCurtainLock = true;[\s\S]*render\(\);/);
});
