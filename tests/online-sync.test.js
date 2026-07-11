const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');

test('online polling continues with an open attack preview and closes stale previews after state changes', () => {
  const pollSource = appSource.slice(
    appSource.indexOf('async function pollOnlineState()'),
    appSource.indexOf('function startPolling()')
  );
  const pollingGuard = pollSource.slice(0, pollSource.indexOf('eventPlayback = true'));

  assert.doesNotMatch(pollingGuard, /\bpreview\b/);
  assert.match(pollSource, /selectedAttacker = null; selectedDefender = null; inspectedCardId = null;/);
  assert.match(pollSource, /closePreview\(\); render\(\);/);
});
