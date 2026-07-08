class AudioDirector {
  constructor() {
    this.manifest = null;
    this.tracks = new Map();
    this.muted = false;
    this.settingsKey = 'tiny-pixel-fights-audio-v1';
    this.groupSettings = this.loadSettings();
    this.unlocked = false;
    this.pendingEvents = new Set();
    this.lastError = null;
    this.oneShotTemplates = new Map();
    this.bgmRetryTimer = null;
    this.bgmRetryAttempts = 0;
  }

  defaultSettings() {
    return {
      bgm: { enabled: true, volume: 1, trackId: null },
      sfx: { enabled: true, volume: 1 },
      voice: { enabled: true, volume: 1 }
    };
  }

  loadSettings() {
    const defaults = this.defaultSettings();
    try {
      const raw = localStorage.getItem(this.settingsKey);
      if (!raw) return defaults;
      return this.normalizeSettings(JSON.parse(raw));
    } catch {
      return defaults;
    }
  }

  normalizeSettings(value) {
    const defaults = this.defaultSettings();
    const normalized = { bgm: { ...defaults.bgm }, sfx: { ...defaults.sfx }, voice: { ...defaults.voice } };
    for (const group of Object.keys(normalized)) {
      if (!value?.[group]) continue;
      normalized[group].enabled = value[group].enabled !== false;
      normalized[group].volume = this.clampVolume(value[group].volume);
      if (group === 'bgm' && typeof value[group].trackId === 'string')
        normalized[group].trackId = value[group].trackId;
    }
    return normalized;
  }

  saveSettings() {
    try {
      localStorage.setItem(this.settingsKey, JSON.stringify(this.groupSettings));
    } catch {}
  }

  clampVolume(value) {
    const volume = Number(value);
    if (!Number.isFinite(volume)) return 1;
    return Math.max(0, Math.min(1, volume));
  }

  groupForBus(bus) {
    if (bus === 'bgm') return 'bgm';
    if (bus === 'voice') return 'voice';
    return 'sfx';
  }

  getSettings() {
    return this.normalizeSettings(this.groupSettings);
  }

  getBgmTracks() {
    if (!this.manifest) return [];
    return Object.entries(this.manifest.tracks || {})
      .filter(([, definition]) => definition.bus === 'bgm')
      .map(([id, definition]) => ({ id, title: definition.title || id }));
  }

  defaultBgmTrackId() {
    const configured = this.manifest?.defaultBgmTrackId;
    if (configured && this.tracks.get(configured)?.definition.bus === 'bgm') return configured;
    return this.getBgmTracks()[0]?.id || null;
  }

  getCurrentBgmTrackId(fallbackId = null) {
    const saved = this.groupSettings.bgm?.trackId;
    if (saved && this.tracks.get(saved)?.definition.bus === 'bgm') return saved;
    if (fallbackId && this.tracks.get(fallbackId)?.definition.bus === 'bgm') return fallbackId;
    return this.defaultBgmTrackId();
  }

  setBgmTrack(trackId) {
    if (!trackId || this.tracks.get(trackId)?.definition.bus !== 'bgm') return false;
    this.groupSettings.bgm.trackId = trackId;
    this.saveSettings();
    if (this.unlocked && !this.muted && this.isGroupEnabled('bgm'))
      this.playBgmTrack(trackId);
    else
      this.applyVolumes();
    return true;
  }

  isGroupEnabled(groupOrBus) {
    const group = this.groupForBus(groupOrBus);
    return this.groupSettings[group]?.enabled !== false;
  }

  setGroupEnabled(groupOrBus, enabled) {
    const group = this.groupForBus(groupOrBus);
    this.groupSettings[group].enabled = Boolean(enabled);
    this.saveSettings();
    this.applyVolumes();
    if (group === 'bgm' && this.groupSettings[group].enabled) this.ensureBgmPlaying();
  }

  setGroupVolume(groupOrBus, volume) {
    const group = this.groupForBus(groupOrBus);
    this.groupSettings[group].volume = this.clampVolume(volume);
    this.saveSettings();
    this.applyVolumes();
    if (group === 'bgm' && this.groupSettings[group].enabled) this.ensureBgmPlaying();
  }

  async load(manifestUrl) {
    const response = await fetch(manifestUrl, { cache: 'no-cache' });
    if (!response.ok) throw new Error(`Audio manifest: ${response.status}`);
    this.manifest = await response.json();

    for (const [id, definition] of Object.entries(this.manifest.tracks || {})) {
      const audio = new Audio(definition.source);
      audio.loop = Boolean(definition.loop);
      audio.preload = definition.preload || 'metadata';
      const entry = { definition, audio, requested: false, primed: false, unlockPromise: null };
      this.tracks.set(id, entry);
      if (this.unlocked) this.prime(entry);
    }
    this.applyVolumes();
    if (this.unlocked) this.flushPendingEvents();
  }

  unlock({ primeUnrequested = true } = {}) {
    // A gesture that happens before the manifest has created Audio elements
    // cannot authorize playback later. Do not record a false unlock.
    if (this.tracks.size === 0) return false;
    const hasRequestedAudio = [...this.tracks.values()].some(entry => entry.requested)
      || this.pendingEvents.size > 0;
    if (!hasRequestedAudio && !primeUnrequested) return false;

    this.unlocked = true;
    this.bgmRetryAttempts = 0;
    // Flush requested music first so audio.play() is invoked with sound during
    // this exact trusted gesture. Priming first can defer the unmute until
    // after a slow first download, which Safari/Chrome may block.
    this.flushPendingEvents();
    for (const entry of this.tracks.values()) {
      if (entry.requested) {
        if ((entry.audio.paused || entry.audio.muted) && !entry.unlockPromise)
          this.startBgm(entry);
      } else if (primeUnrequested) {
        this.prime(entry);
      }
    }
    this.ensureBgmPlaying();
    return true;
  }

  flushPendingEvents() {
    const events = [...this.pendingEvents];
    this.pendingEvents.clear();
    for (const eventName of events) this.emit(eventName);
  }

  prime(entry) {
    if (entry.definition.bus !== 'bgm' || entry.primed || entry.unlockPromise) return;
    const { audio } = entry;
    audio.muted = true;
    audio.volume = 0;
    try {
      const playResult = audio.play();
      entry.unlockPromise = Promise.resolve(playResult).then(() => {
        entry.primed = true;
        entry.unlockPromise = null;
        if (entry.requested) this.startBgm(entry);
      }).catch(error => {
        entry.unlockPromise = null;
        this.lastError = error;
      });
    } catch (error) {
      entry.unlockPromise = null;
      this.lastError = error;
    }
  }

  emit(eventName) {
    if (!this.manifest || !this.unlocked) { this.pendingEvents.add(eventName); return; }
    const cues = this.manifest.events?.[eventName] || [];
    for (const cue of cues) this.play(cue);
  }

  play(trackId) {
    const entry = this.tracks.get(trackId);
    if (!entry || this.muted) return;
    const { definition, audio } = entry;
    const group = this.groupForBus(definition.bus);

    if (definition.bus === 'bgm') {
      this.playBgmTrack(this.getCurrentBgmTrackId(trackId));
      return;
    }

    if (!this.isGroupEnabled(group)) return;
    const instance = audio.cloneNode(true);
    instance.volume = this.volumeFor(definition);
    instance.play().catch(() => {});
  }

  playBgmTrack(trackId) {
    const entry = this.tracks.get(trackId);
    if (!entry || entry.definition.bus !== 'bgm' || this.muted) return;
    for (const [id, other] of this.tracks) {
      if (other.definition.bus !== 'bgm' || id === trackId) continue;
      other.requested = false;
      other.audio.muted = true;
      if (!other.audio.paused && typeof other.audio.pause === 'function') other.audio.pause();
      try { other.audio.currentTime = 0; } catch {}
    }
    entry.requested = true;
    if (!this.isGroupEnabled('bgm')) { entry.audio.muted = true; return; }
    if (!entry.audio.paused && !entry.audio.muted) return;
    if (!entry.unlockPromise) this.startBgm(entry);
  }

  playOneShotSource(source, options = {}) {
    if (!source || this.muted || !this.unlocked) return;
    const definition = {
      source,
      bus: options.bus || 'sfx',
      volume: options.volume ?? 1
    };
    if (!this.isGroupEnabled(definition.bus)) return;
    const template = this.oneShotTemplate(source);
    const audio = template.cloneNode(true);
    audio.preload = 'auto';
    audio.volume = this.volumeFor(definition);
    audio.play().catch(error => { this.lastError = error; });
  }

  preloadOneShotSources(sources = []) {
    for (const source of sources) this.oneShotTemplate(source);
  }

  oneShotTemplate(source) {
    if (this.oneShotTemplates.has(source)) return this.oneShotTemplates.get(source);
    const audio = new Audio(source);
    audio.preload = 'auto';
    try { audio.load(); } catch {}
    this.oneShotTemplates.set(source, audio);
    return audio;
  }

  startBgm(entry) {
    const { definition, audio } = entry;
    if (this.muted || !this.isGroupEnabled(definition.bus)) { audio.muted = true; return; }
    const targetVolume = this.volumeFor(definition);
    const fadeMs = Number(definition.fadeInMs || 0);
    if (entry.primed) {
      try { audio.currentTime = 0; } catch {}
    }
    audio.muted = this.muted;
    audio.volume = fadeMs > 0 ? 0 : targetVolume;
    const beginFade = () => {
      if (fadeMs > 0 && !this.muted) this.fade(audio, targetVolume, fadeMs);
    };
    if (!audio.paused) beginFade();
    else audio.play().then(() => {
      this.lastError = null;
      this.bgmRetryAttempts = 0;
      beginFade();
    }).catch(error => {
      this.lastError = error;
      if (entry.requested) this.scheduleBgmRetry();
    });
  }

  ensureBgmPlaying() {
    if (!this.manifest || !this.unlocked || this.muted || !this.isGroupEnabled('bgm')) return false;
    let requested = false;
    for (const entry of this.tracks.values()) {
      if (entry.definition.bus !== 'bgm' || !entry.requested) continue;
      requested = true;
      if ((entry.audio.paused || entry.audio.muted) && !entry.unlockPromise)
        this.startBgm(entry);
    }
    return requested;
  }

  scheduleBgmRetry() {
    if (this.bgmRetryTimer || this.bgmRetryAttempts >= 3) return;
    const delay = 500 * 2 ** this.bgmRetryAttempts;
    this.bgmRetryAttempts++;
    this.bgmRetryTimer = setTimeout(() => {
      this.bgmRetryTimer = null;
      this.ensureBgmPlaying();
    }, delay);
  }

  setMuted(value) {
    this.muted = Boolean(value);
    this.applyVolumes();
    if (!this.muted) this.ensureBgmPlaying();
  }

  toggleMuted() {
    this.setMuted(!this.muted);
    return this.muted;
  }

  applyVolumes() {
    for (const entry of this.tracks.values()) {
      const { definition, audio } = entry;
      audio.volume = entry.primed && !entry.requested ? 0 : this.volumeFor(definition);
      audio.muted = this.muted || !this.isGroupEnabled(definition.bus) || (definition.bus === 'bgm' && !entry.requested);
    }
  }

  volumeFor(definition) {
    const busVolume = Number(this.manifest?.buses?.[definition.bus] ?? 1);
    const groupVolume = this.groupSettings[this.groupForBus(definition.bus)]?.volume ?? 1;
    return Math.max(0, Math.min(1, busVolume * Number(definition.volume ?? 1) * groupVolume));
  }

  fade(audio, targetVolume, durationMs) {
    const startedAt = performance.now();
    const step = now => {
      const progress = Math.min(1, (now - startedAt) / durationMs);
      audio.volume = targetVolume * progress;
      if (progress < 1 && !audio.paused) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }
}

window.AudioDirector = AudioDirector;
