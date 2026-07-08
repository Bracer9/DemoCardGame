const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const manifestPath = path.join(root, 'wwwroot/config/ui-assets.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
const appSource = fs.readFileSync(path.join(root, 'wwwroot/app.js'), 'utf8');
const stylesSource = fs.readFileSync(path.join(root, 'wwwroot/styles.css'), 'utf8');

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
  assert.deepEqual(placeholders, [
    'status.beast-rage',
    'status.attack-sealed',
    'status.marked',
    'status.prey',
    'status.pact'
  ]);
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
  assert.match(html, new RegExp(`status_guard\\.png\\?v=${manifest.version}`));
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

test('rank 2 and rank 3 heroes use the advanced hero card background', () => {
  assert.ok(fs.existsSync(path.join(root, 'assets/ui/AdvancedHeroCardBackground.png')));
  assert.match(stylesSource, /data-card-type="Hero"\]\[data-hero-rank="2"\][\s\S]*AdvancedHeroCardBackground\.png/);
  assert.match(stylesSource, /data-card-type="Hero"\]\[data-hero-rank="3"\][\s\S]*AdvancedHeroCardBackground\.png/);
  assert.match(stylesSource, /\.rank-up-card\.rank-bg-advanced-hero[\s\S]*AdvancedHeroCardBackground\.png/);
  assert.match(appSource, /const newBgClass = afterRank >= 2 \? ' rank-bg-advanced-hero' : '';/);
  assert.match(appSource, /afterHeroRank > beforeHeroRank[\s\S]*afterHeroRank >= 2/);
});
