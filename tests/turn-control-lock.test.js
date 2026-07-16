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

test('zero AP automatically ends only when no free action remains', () => {
  assert.match(appSource, /function shouldAutoEndTurn\(\)[\s\S]*game\.actionPoints === 0[\s\S]*!hasExecutableTurnAction\(game\)/);
  assert.match(appSource, /function hasExecutableTurnAction\(state\)[\s\S]*state\.canDeployShield[\s\S]*action\.enabled && Number\(action\.cost\) <= Number\(state\.actionPoints\)[\s\S]*character\.canAct \|\| character\.canAssignAsDeputy/);
  assert.match(appSource, /async function runScheduledAutoEndTurn\(turnKey\)[\s\S]*while \(\(busy \|\| eventPlayback\)[\s\S]*await endTurn\(\)/);
  assert.match(appSource, /syncSelectedInspector\(\);\s*scheduleAutoEndTurn\(\);\s*scheduleAiAdvance\(\);/);
});
