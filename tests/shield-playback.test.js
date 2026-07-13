const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

const appSource = fs.readFileSync('wwwroot/app.js', 'utf8');
const engineSource = fs.readFileSync('Domain/GameEngine.cs', 'utf8');

function functionSource(name) {
  const start = appSource.indexOf(`function ${name}(`);
  assert.notEqual(start, -1, `${name} not found`);
  const bodyStart = appSource.indexOf('{', start);
  let depth = 0;
  for (let index = bodyStart; index < appSource.length; index++) {
    if (appSource[index] === '{') depth++;
    if (appSource[index] !== '}') continue;
    depth--;
    if (depth === 0) return appSource.slice(start, index + 1);
  }
  throw new Error(`${name} is incomplete`);
}

function arg(value) {
  return { kind: 'raw', value };
}

test('pending shield removal is owned by IDs and includes turn-start expiry', () => {
  const context = { lastAnimatedLogSequence: 10 };
  vm.createContext(context);
  vm.runInContext(`${functionSource('logArg')}\n${functionSource('hasPendingShieldRemovalForPlayer')}`, context);

  const player = {
    id: 'player-a',
    name: 'Player',
    sharedShield: 0,
    characters: [{ id: 'a-mage', key: 'mage' }]
  };
  const opponent = {
    id: 'player-b',
    name: 'AI',
    sharedShield: 0,
    characters: [{ id: 'b-mage', key: 'mage' }]
  };
  const state = {
    players: [player, opponent],
    log: [
      { sequence: 11, message: { key: 'note.shieldAbsorb', args: { characterId: arg('a-mage'), remaining: arg(4) } } },
      { sequence: 12, message: { key: 'log.shieldExpired', args: { player: arg('Player'), playerId: arg('player-a') } } }
    ]
  };

  assert.equal(context.hasPendingShieldRemovalForPlayer(state, player), true);
  assert.equal(context.hasPendingShieldRemovalForPlayer(state, opponent), false);

  state.log = [
    { sequence: 11, message: { key: 'note.shieldAbsorb', args: { characterId: arg('a-mage'), remaining: arg(0) } } }
  ];
  assert.equal(context.hasPendingShieldRemovalForPlayer(state, player), true);
  assert.equal(context.hasPendingShieldRemovalForPlayer(state, opponent), false);
});

test('shield visual follows remaining values and expires only on the expiry event', () => {
  const badgeValues = [];
  const classes = new Set();
  const dome = {
    dataset: {},
    classList: {
      add: (...names) => names.forEach(name => classes.add(name)),
      remove: (...names) => names.forEach(name => classes.delete(name)),
      toggle: (name, enabled) => enabled ? classes.add(name) : classes.delete(name)
    },
    setAttribute: () => {}
  };
  const context = {
    renderShieldBadge: (_badge, value) => badgeValues.push(value),
    shieldBadgeForDome: () => ({})
  };
  vm.createContext(context);
  vm.runInContext([
    functionSource('cancelStaleShieldBreak'),
    functionSource('updateShieldVisualValue'),
    functionSource('expireShieldVisual')
  ].join('\n'), context);

  context.updateShieldVisualValue(dome, {}, 4);
  assert.equal(dome.dataset.visualShieldValue, '4');
  assert.equal(classes.has('active'), true);
  assert.equal(classes.has('reinforced'), true);

  context.updateShieldVisualValue(dome, {}, 3);
  assert.equal(dome.dataset.visualShieldValue, '3');
  assert.equal(classes.has('active'), true);

  context.expireShieldVisual(dome, {});
  assert.deepEqual(badgeValues, [4, 3, 0]);
  assert.equal(classes.has('active'), false);
  assert.equal(dome.dataset.visualShieldValue, undefined);
});

test('shield expiry logs carry a stable player ID', () => {
  assert.match(engineSource, /"log\.shieldExpired"[\s\S]*?\("playerId", L10n\.Raw\(state\.ActivePlayer\.Id\)\)/);
});
