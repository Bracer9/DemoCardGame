const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');
const stylesSource = fs.readFileSync('wwwroot/styles.css', 'utf8');

test('drag target lookup can see valid cards behind overlays', () => {
  assert.match(appSource, /function targetAtClientPoint\(clientX, clientY, selector, validator = null\)/);
  assert.match(appSource, /document\.elementsFromPoint\(clientX, clientY\)/);
  assert.match(appSource, /targetAtClientPoint\(event\.clientX, event\.clientY, selector, candidate =>/);
});

test('document-level drag and drop fallback can resolve card targets', () => {
  assert.match(appSource, /document\.addEventListener\('dragover', event => \{/);
  assert.match(appSource, /event\.preventDefault\(\);\s*document\.querySelectorAll\('\.drop-ready'\)/);
  assert.match(appSource, /document\.addEventListener\('drop', event => \{/);
  assert.match(appSource, /if \(!roleActionDragActive && selectedAttacker\) \{\s*finishAttackArrow\(target\);\s*chooseDefender\(target\.dataset\.id\);/);
});

test('card drag handlers use current card state instead of first render state', () => {
  assert.doesNotMatch(appSource, /if \(card\.dataset\.side === 'opponent' && !card\.classList\.contains\('defeated'\) && !card\.classList\.contains\('deploying'\)\) \{/);
  assert.match(appSource, /card\.addEventListener\('dragover', event => \{\s*if \(roleActionDragActive\s*\|\| card\.dataset\.side !== 'opponent'\s*\|\| card\.classList\.contains\('defeated'\)\s*\|\| card\.classList\.contains\('deploying'\)\) return;/);
  assert.match(appSource, /card\.addEventListener\('drop', event => \{\s*if \(roleActionDragActive\s*\|\| card\.dataset\.side !== 'opponent'\s*\|\| card\.classList\.contains\('defeated'\)\s*\|\| card\.classList\.contains\('deploying'\)\) return;/);
});

test('direct attack dragging does not play select voice', () => {
  const dragStartBlock = appSource.slice(
    appSource.indexOf("card.addEventListener('dragstart'"),
    appSource.indexOf("card.addEventListener('dragend'")
  );
  assert.match(dragStartBlock, /sound\.emit\('ui\.card-select'\)/);
  assert.doesNotMatch(dragStartBlock, /emitSelectVoice/);
  assert.match(appSource, /function onCardClick\(element\)[\s\S]*if \(isSelecting\) \{[\s\S]*emitSelectVoice\(element\)/);
});

test('persistent team shield does not intercept attack drag targeting', () => {
  assert.match(stylesSource, /#app\.dragging-attack \.persistent-team-shield\.active,\s*body\.dragging-attack \.persistent-team-shield\.active \{ pointer-events:none; \}/);
});

test('shield hit animation does not re-anchor persistent shield geometry', () => {
  assert.match(appSource, /shieldBlock\(target, amount, player\?\.sharedShield \|\| 0\)/);
  assert.match(appSource, /function shieldBlock\(target, amount, remaining\)/);
  const shieldBlockSource = appSource.slice(
    appSource.indexOf('function shieldBlock('),
    appSource.indexOf('function animateBpRecoveryToHp(')
  );
  assert.match(shieldBlockSource, /const dome = target\.dataset\.side === 'active' \? ui\.activeShieldDome : ui\.opponentShieldDome/);
  assert.doesNotMatch(shieldBlockSource, /renderPersistentShield/);
  assert.doesNotMatch(shieldBlockSource, /playerId|expectedPlayerId|teamShieldVisualForPlayer/);
  assert.match(appSource, /dome\.dataset\.pendingBreak = 'true'/);
});

test('persistent shield geometry reuses stable card bounds during card transitions', () => {
  assert.match(appSource, /const shieldLayoutCache = new WeakMap\(\)/);
  assert.match(appSource, /function stableShieldLayout\(dome, cards, force = false\)/);
  assert.match(appSource, /if \(!force && cached\?\.signature === signature\) return cached/);
  assert.match(appSource, /const layout = cards\.length > 0 \? stableShieldLayout\(dome, cards, forceLayout\) : null/);
  assert.match(appSource, /renderPersistentShield\(ui\.activeShieldDome, ui\.activeCards, viewer, false, true\)/);
});
