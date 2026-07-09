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

test('persistent team shield does not intercept attack drag targeting', () => {
  assert.match(stylesSource, /#app\.dragging-attack \.persistent-team-shield\.active,\s*body\.dragging-attack \.persistent-team-shield\.active \{ pointer-events:none; \}/);
});
