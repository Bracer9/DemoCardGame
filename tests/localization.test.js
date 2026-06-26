const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const readJson = relative => JSON.parse(fs.readFileSync(path.join(root, relative), 'utf8'));
const flattenKeys = (value, prefix = '') => Object.entries(value).flatMap(([key, child]) => {
  const full = prefix ? `${prefix}.${key}` : key;
  return child && typeof child === 'object' && !Array.isArray(child) ? flattenKeys(child, full) : [full];
}).sort();

test('Japanese and Chinese resources have identical schemas', () => {
  assert.deepEqual(
    flattenKeys(readJson('wwwroot/locales/zh.json')),
    flattenKeys(readJson('wwwroot/locales/ja.json'))
  );
});

test('C# source contains no Japanese or Chinese display text', () => {
  const files = [];
  const visit = directory => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      if (['bin', 'obj', '.git'].includes(entry.name)) continue;
      const full = path.join(directory, entry.name);
      if (entry.isDirectory()) visit(full);
      else if (entry.name.endsWith('.cs')) files.push(full);
    }
  };
  visit(root);
  const offenders = files.filter(file => /[\u3040-\u30ff\u3400-\u9fff]/u.test(fs.readFileSync(file, 'utf8')));
  assert.deepEqual(offenders.map(file => path.relative(root, file)), []);
});

test('frontend does not translate server prose with regex', () => {
  const app = fs.readFileSync(path.join(root, 'wwwroot/app.js'), 'utf8');
  const i18n = fs.readFileSync(path.join(root, 'wwwroot/i18n.js'), 'utf8');
  assert.equal(app.includes('translateText'), false);
  assert.equal(i18n.includes('translateText'), false);
});

test('every static frontend UI key exists in both locales', () => {
  const app = fs.readFileSync(path.join(root, 'wwwroot/app.js'), 'utf8');
  const html = fs.readFileSync(path.join(root, 'wwwroot/index.html'), 'utf8');
  const keys = new Set([
    ...[...app.matchAll(/i18n\.t\('([^']+)'/g)].map(match => match[1]),
    ...[...html.matchAll(/data-i18n(?:-aria|-placeholder)?="([^"]+)"/g)].map(match => match[1]),
    'host', 'guest', 'playerJoinedHost', 'joinedWaitingHost', 'dealTouch', 'dealTouchOnline'
  ]);
  for (const locale of ['ja', 'zh']) {
    const ui = readJson(`wwwroot/locales/${locale}.json`).ui;
    assert.deepEqual([...keys].filter(key => !(key in ui)).sort(), [], `${locale} is missing UI keys`);
  }
});

test('structured domain messages render independently in Japanese and Chinese', async () => {
  const context = {
    window: {},
    document: { documentElement: { lang: '' } },
    localStorage: { getItem: () => null, setItem: () => {} },
    fetch: async url => ({ json: async () => readJson(`wwwroot${url}`) })
  };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(root, 'wwwroot/i18n.js'), 'utf8'), context);
  const i18n = context.window.TinyPixelI18n;
  await i18n.load();
  const exchange = {
    key: 'log.exchange',
    args: {
      attacker: { kind: 'character', value: 'peasant' },
      defender: { kind: 'character', value: 'princess' },
      attackDamage: { kind: 'raw', value: 2 },
      attackType: { kind: 'damageType', value: 'Physical' },
      counterDamage: { kind: 'raw', value: 0 },
      counterType: { kind: 'damageType', value: 'Physical' }
    }
  };
  assert.match(i18n.message(exchange), /農民.*姫.*物理/u);
  assert.equal(i18n.skill('predatory-instinct').name, '美女と野獣');
  assert.match(i18n.skill('predatory-instinct').card, /姫死亡後攻撃\+2/u);
  assert.equal(i18n.status({ id: 'beast-rage', magnitude: 2 }).name, '野獣の怒り');
  i18n.setLanguage('zh');
  const chinese = i18n.message(exchange);
  assert.match(chinese, /农民.*公主.*物理/u);
  assert.doesNotMatch(chinese, /[\u3040-\u30ff]/u);
  assert.equal(i18n.skill('interposing-shield').name, '替身之盾');
  assert.equal(i18n.status({ id: 'shield-complacency', magnitude: 2 }).description.includes('-2'), true);
  const beauty = i18n.skill('predatory-instinct');
  assert.equal(beauty.name, '美女与野兽');
  assert.match(beauty.card, /公主阵亡后攻击\+2/u);
  assert.match(beauty.description, /基础攻击\+2/u);
  const rage = i18n.status({ id: 'beast-rage', magnitude: 2 });
  assert.equal(rage.name, '野兽之怒');
  assert.match(rage.description, /主动进攻和反击/u);
});

test('all localization IDs referenced by C# exist in both resources', () => {
  const cs = [];
  const visit = directory => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      if (['bin', 'obj', '.git'].includes(entry.name)) continue;
      const full = path.join(directory, entry.name);
      if (entry.isDirectory()) visit(full);
      else if (entry.name.endsWith('.cs')) cs.push(fs.readFileSync(full, 'utf8'));
    }
  };
  visit(root);
  const source = cs.join('\n');
  const ids = {
    messages: [...source.matchAll(/"((?:message|error|reason|preview|log|note)\.[a-zA-Z0-9.]+)"/g)].map(match => match[1]),
    skills: [...source.matchAll(/L10n\.Skill\("([^"]+)"\)/g)].map(match => match[1]),
    statuses: [...source.matchAll(/L10n\.Status\("([^"]+)"\)/g)].map(match => match[1])
  };
  for (const locale of ['ja', 'zh']) {
    const resource = readJson(`wwwroot/locales/${locale}.json`);
    for (const [section, values] of Object.entries(ids)) {
      assert.deepEqual([...new Set(values)].filter(key => !(key in resource[section])).sort(), [],
        `${locale} is missing C# ${section} IDs`);
    }
  }
});
