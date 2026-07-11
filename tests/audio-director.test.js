const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const projectRoot = path.resolve(__dirname, '..');
const projectFile = fs.readFileSync(path.join(projectRoot, 'TinyPixelFights.csproj'), 'utf8');
const projectAudioManifest = JSON.parse(fs.readFileSync(path.join(projectRoot, 'wwwroot/config/audio.json'), 'utf8'));

class FakeAudio {
  constructor(source) {
    this.source = source;
    this.paused = true;
    this.muted = false;
    this.volume = 1;
    this.currentTime = 0;
    this.playCalls = 0;
    this.failNextPlay = false;
  }

  play() {
    this.playCalls++;
    if (this.failNextPlay) {
      this.failNextPlay = false;
      this.paused = true;
      return Promise.reject(new Error('blocked'));
    }
    this.paused = false;
    return Promise.resolve();
  }

  cloneNode() { return new FakeAudio(this.source); }
}

const manifest = {
  buses: { bgm: 0.5 },
  tracks: {
    battle: { source: '/battle.mp3', bus: 'bgm', loop: true, fadeInMs: 0 }
  },
  events: { 'game.start': ['battle'] }
};

function createDirector() {
  const timers = [];
  const context = {
    window: {}, FakeAudio, Audio: FakeAudio,
    fetch: async () => ({ ok: true, json: async () => manifest }),
    performance: { now: () => 0 },
    requestAnimationFrame: () => {},
    setTimeout: callback => { timers.push(callback); return timers.length; },
    clearTimeout: () => {},
    console
  };
  vm.runInNewContext(fs.readFileSync('wwwroot/audio.js', 'utf8'), context);
  const director = new context.window.AudioDirector();
  director.__timers = timers;
  return director;
}

test('a gesture before the manifest does not create a false unlock', async () => {
  const director = createDirector();
  assert.equal(director.unlock(), false);
  director.emit('game.start');
  await director.load('/config/audio.json');

  const entry = director.tracks.get('battle');
  assert.equal(entry.audio.playCalls, 0);
  assert.equal(director.unlock(), true);
  await new Promise(resolve => setImmediate(resolve));

  assert.equal(entry.requested, true);
  assert.equal(entry.audio.paused, false);
  assert.equal(entry.audio.muted, false);
});

test('a generic gesture does not pre-prime unrequested BGM', async () => {
  const director = createDirector();
  await director.load('/config/audio.json');
  const entry = director.tracks.get('battle');

  assert.equal(director.unlock({ primeUnrequested: false }), false);
  assert.equal(entry.audio.playCalls, 0);
  assert.equal(director.unlocked, false);
});

test('a later user gesture retries requested BGM that was paused', async () => {
  const director = createDirector();
  await director.load('/config/audio.json');
  director.emit('game.start');
  director.unlock();
  await new Promise(resolve => setImmediate(resolve));

  const entry = director.tracks.get('battle');
  const previousCalls = entry.audio.playCalls;
  entry.audio.paused = true;
  director.unlock();
  await Promise.resolve();

  assert.ok(entry.audio.playCalls > previousCalls);
  assert.equal(entry.audio.paused, false);
});

test('a failed requested BGM play is retried shortly after', async () => {
  const director = createDirector();
  await director.load('/config/audio.json');
  const entry = director.tracks.get('battle');
  director.unlocked = true;
  entry.audio.failNextPlay = true;

  director.playBgmTrack('battle');
  await new Promise(resolve => setImmediate(resolve));

  assert.equal(entry.audio.paused, true);
  assert.equal(director.__timers.length, 1);
  director.__timers.shift()();
  await new Promise(resolve => setImmediate(resolve));

  assert.equal(entry.requested, true);
  assert.equal(entry.audio.paused, false);
  assert.ok(entry.audio.playCalls >= 2);
});

test('project audio manifest references existing tracks and valid event cues', () => {
  for (const [eventId, cues] of Object.entries(projectAudioManifest.events)) {
    for (const cue of cues)
      assert.ok(projectAudioManifest.tracks[cue], `${eventId} references missing track ${cue}`);
  }

  for (const [trackId, definition] of Object.entries(projectAudioManifest.tracks)) {
    if (!definition.source.startsWith('/assets/')) continue;
    const relativePath = decodeURIComponent(definition.source.slice('/assets/'.length));
    assert.ok(fs.existsSync(path.join(projectRoot, 'assets', relativePath)), `${trackId} source is missing: ${definition.source}`);
  }
});

test('runtime audio manifest does not depend on the source library', () => {
  for (const [trackId, definition] of Object.entries(projectAudioManifest.tracks)) {
    assert.ok(!definition.source.includes('/audio_library/'), `${trackId} points at source library: ${definition.source}`);
  }
});

test('source audio library is excluded from build and publish output', () => {
  assert.ok(projectFile.includes('Exclude="assets\\audio_library\\**\\*"'));
});

test('sound effect filenames use portable English kebab-case names', () => {
  const files = fs.readdirSync(path.join(projectRoot, 'assets/audio'))
    .filter(file => /\.(?:wav|mp3|ogg)$/i.test(file));
  assert.ok(files.length > 0);
  for (const file of files) {
    assert.match(file, /^[a-z0-9]+(?:-[a-z0-9]+)*\.mp3$/i);
  }
});

test('all runtime audio files are registered in the manifest', () => {
  const files = fs.readdirSync(path.join(projectRoot, 'assets/audio'))
    .filter(file => /\.mp3$/i.test(file));
  const registered = new Set(Object.values(projectAudioManifest.tracks)
    .filter(definition => definition.source.startsWith('/assets/audio/'))
    .map(definition => path.basename(decodeURIComponent(definition.source))));

  for (const file of files)
    assert.ok(registered.has(file), `${file} exists in assets/audio but is not registered in audio.json`);
});

test('runtime sound pass is mapped and fireball replaces the old magic attack cue', () => {
  assert.equal(projectAudioManifest.tracks['sfx.magic-attack'].source, '/assets/audio/fireball.mp3');
  const requiredEvents = [
    'combat.magic-impact', 'combat.magic-counter', 'combat.character-defeated',
    'shield.deploy', 'shield.reinforce', 'shield.break',
    'trait.aftershock-axe', 'trait.guard-trigger', 'ui.attack-confirm',
    'ui.shuffle', 'ui.deal', 'ui.card-place', 'ui.ap-spend'
  ];
  for (const eventId of requiredEvents)
    assert.ok(projectAudioManifest.events[eventId]?.length, `${eventId} is not mapped`);
});
