const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const manifestPath = path.join(root, 'wwwroot/config/ui-assets.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
const appSource = fs.readFileSync(path.join(root, 'wwwroot/app.js'), 'utf8');

test('UI asset manifest bindings reference registered icons', () => {
  for (const [group, bindings] of Object.entries(manifest.bindings)) {
    for (const [semanticId, assetId] of Object.entries(bindings)) {
      assert.ok(manifest.icons[assetId], `${group}.${semanticId} references missing ${assetId}`);
    }
  }
});

test('every configured PNG source exists and has a valid PNG header', () => {
  const missing = [];
  for (const [assetId, definition] of Object.entries(manifest.icons)) {
    if (!definition.source) continue;
    const file = path.join(root, 'assets/ui', definition.source);
    if (!fs.existsSync(file)) { missing.push(`${assetId}: ${definition.source}`); continue; }
    const bytes = fs.readFileSync(file);
    assert.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a', `${assetId} is not a PNG`);
    assert.ok(bytes.readUInt32BE(16) > 0 && bytes.readUInt32BE(20) > 0, `${assetId} has invalid dimensions`);
  }
  assert.deepEqual(missing, []);
});

test('only explicitly missing status artwork uses placeholders', () => {
  const placeholders = Object.entries(manifest.icons)
    .filter(([, definition]) => !definition.source)
    .map(([assetId]) => assetId);
  assert.deepEqual(placeholders, ['status.shield-complacency', 'status.beast-rage']);
});

test('rendered image URLs include the manifest version cache key', async () => {
  const context = {
    window: { dispatchEvent: () => {} },
    CustomEvent: class {},
    document: {},
    fetch: async () => ({ ok: true, json: async () => manifest })
  };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(root, 'wwwroot/ui-assets.js'), 'utf8'), context);
  await context.window.TinyPixelAssets.load();
  const html = context.window.TinyPixelAssets.icon('status.guard');
  assert.match(html, /status_guard\.png\?v=2/);
});

test('all eight event assets are wired into synchronized battle playback', () => {
  const eventIds = ['physical', 'magical', 'counter', 'trait', 'status-tick', 'heal', 'shield', 'death'];
  for (const id of eventIds) {
    assert.ok(manifest.icons[`event.${id}`], `missing event.${id} artwork`);
    assert.match(appSource, new RegExp(`event\\.${id.replace('-', '\\-')}`), `event.${id} is not used by the battle presenter`);
  }
  assert.match(appSource, /async function playNewLogEvents/);
  assert.match(appSource, /await playNewLogEvents\(game\)/);
  assert.match(appSource, /case 'log\.exchange'/);
  assert.match(appSource, /case 'log\.effectDamage'/);
  assert.match(appSource, /case 'log\.defeated'/);
});

test('confirmed attacks render a synchronized attacker-to-target link', () => {
  assert.match(appSource, /async function playExchangeEvent[\s\S]*showCombatLink\(attacker, defender\)/);
  assert.match(appSource, /function showCombatLink\(attacker, defender\)/);
  assert.match(appSource, /combat-link-head/);
});
