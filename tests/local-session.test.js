const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');
const gameSessionSource = fs.readFileSync('Services/GameSession.cs', 'utf8');
const programSource = fs.readFileSync('Program.cs', 'utf8');

test('local game requests carry a browser-scoped session token', () => {
  assert.match(appSource, /LOCAL_SESSION_TOKEN_KEY = 'tpf-local-session-token'/);
  assert.match(appSource, /localStorage\.setItem\(LOCAL_SESSION_TOKEN_KEY, token\)/);
  assert.match(appSource, /isLocalGameApi && localSessionToken \? \{ 'X-Local-Session': localSessionToken \}/);
  assert.match(appSource, /isOnlineApi && playerToken \? \{ 'X-Player-Token': playerToken \}/);
});

test('local GameSession stores independent in-memory states by token', () => {
  assert.match(programSource, /AddHttpContextAccessor\(\)/);
  assert.match(gameSessionSource, /ConcurrentDictionary<string, LocalGameSlot>/);
  assert.match(gameSessionSource, /private const string LocalSessionHeader = "X-Local-Session"/);
  assert.match(gameSessionSource, /private const int MaxSessions = 64/);
  assert.match(gameSessionSource, /TimeSpan\.FromHours\(12\)/);
  assert.match(gameSessionSource, /_sessions\.GetOrAdd\(token, _ => new LocalGameSlot\(_engine\.CreateGame\(\), now\)\)/);
});
