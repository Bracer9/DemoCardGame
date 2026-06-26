const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');

test('an open attack preview does not suspend online state polling', () => {
  assert.doesNotMatch(appSource, /dealing\s*\|\|\s*preview\s*\|\|\s*!hasStarted/);
  assert.match(appSource, /selectedAttacker = null; selectedDefender = null; closePreview\(\); render\(\);/);
});
