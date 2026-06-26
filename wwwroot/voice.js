class VoiceDirector {
  constructor(audioDirector) {
    this.audio = audioDirector;
    this.manifest = null;
    this.lastGlobalAt = -Infinity;
    this.lastByCharacter = new Map();
    this.lastByType = new Map();
  }

  async load(manifestUrl) {
    const response = await fetch(manifestUrl, { cache: 'no-cache' });
    if (!response.ok) throw new Error(`Voice manifest: ${response.status}`);
    const manifest = await response.json();
    this.manifest = manifest;
    if (manifest.autoIndex) {
      const indexResponse = await fetch(manifest.autoIndex, { cache: 'no-cache' });
      if (!indexResponse.ok) throw new Error(`Voice index: ${indexResponse.status}`);
      this.manifest = this.mergeManifest(manifest, await indexResponse.json());
    }
    this.preloadConfiguredSources();
  }

  emit(cue) {
    if (!this.manifest || !cue?.type) return false;
    const characterId = cue.actorCharacterId || cue.characterId;
    if (!characterId) return false;
    const pool = this.poolFor(characterId, cue.type);
    if (!pool?.sources?.length) return false;
    if (!this.matchesConditions(pool.conditions, cue)) return false;

    const defaults = this.manifest.defaults || {};
    const priority = Number(pool.priority ?? this.priorityFor(cue.type));
    const now = performance.now();
    if (!this.canPlay({ characterId, type: cue.type, priority, now, pool, defaults })) return false;

    const source = this.choose(pool.sources);
    this.lastGlobalAt = now;
    this.lastByCharacter.set(characterId, { at: now, priority });
    this.lastByType.set(cue.type, now);
    const play = () => this.audio?.playOneShotSource(source, {
      bus: pool.bus || defaults.bus || 'voice',
      volume: this.volumeForCue({ characterId, type: cue.type, pool, defaults })
    });
    const delayMs = this.delayForCue({ type: cue.type, pool, defaults });
    if (delayMs > 0) {
      window.setTimeout(play, delayMs);
    } else {
      play();
    }
    return true;
  }

  poolFor(characterId, type) {
    return this.manifest?.pools?.[characterId]?.[type] || null;
  }

  mergeManifest(manifest, index) {
    const pools = this.clonePools(index?.pools || {});
    for (const [characterId, characterPools] of Object.entries(manifest.pools || {})) {
      pools[characterId] ||= {};
      for (const [type, pool] of Object.entries(characterPools || {})) {
        const indexed = pools[characterId][type] || {};
        pools[characterId][type] = {
          ...indexed,
          ...pool,
          sources: pool.sources?.length ? pool.sources : (indexed.sources || [])
        };
      }
    }
    return { ...manifest, pools };
  }

  clonePools(pools) {
    const result = {};
    for (const [characterId, characterPools] of Object.entries(pools || {})) {
      result[characterId] = {};
      for (const [type, pool] of Object.entries(characterPools || {})) {
        result[characterId][type] = { ...pool, sources: [...(pool.sources || [])] };
      }
    }
    return result;
  }

  choose(sources) {
    return sources[Math.floor(Math.random() * sources.length)];
  }

  priorityFor(type) {
    return this.manifest?.priorities?.[type] ?? 20;
  }

  volumeForCue({ characterId, type, pool, defaults }) {
    const baseVolume = Number(defaults.volume ?? 1);
    const characterVolume = Number(this.manifest?.characterVolumes?.[characterId] ?? 1);
    const typeVolume = Number(this.manifest?.typeVolumes?.[type] ?? 1);
    const poolVolume = Number(pool.volume ?? 1);
    return this.clampVolume(baseVolume * characterVolume * typeVolume * poolVolume);
  }

  delayForCue({ type, pool, defaults }) {
    const value = Number(pool.delayMs ?? this.manifest?.typeDelaysMs?.[type] ?? defaults.delayMs ?? 0);
    if (!Number.isFinite(value)) return 0;
    return Math.max(0, value);
  }

  clampVolume(value) {
    if (!Number.isFinite(value)) return 1;
    return Math.max(0, Math.min(2, value));
  }

  canPlay({ characterId, type, priority, now, pool, defaults }) {
    const globalCooldownMs = Number(pool.globalCooldownMs ?? defaults.globalCooldownMs ?? 250);
    if (now - this.lastGlobalAt < globalCooldownMs && priority < 80) return false;

    const typeCooldownMs = Number(pool.typeCooldownMs ?? defaults.typeCooldownMs ?? 700);
    if (now - (this.lastByType.get(type) || -Infinity) < typeCooldownMs && priority < 80) return false;

    const characterCooldownMs = Number(pool.characterCooldownMs ?? defaults.characterCooldownMs ?? 1200);
    const lastCharacter = this.lastByCharacter.get(characterId);
    if (lastCharacter && now - lastCharacter.at < characterCooldownMs && priority <= lastCharacter.priority) return false;

    return true;
  }

  matchesConditions(conditions, cue) {
    if (!conditions) return true;
    if (conditions.amountAtLeast !== undefined && Number(cue.amount || 0) < Number(conditions.amountAtLeast)) return false;
    if (conditions.damageTypes?.length && !conditions.damageTypes.includes(cue.damageType)) return false;
    if (conditions.statusIds?.length && !conditions.statusIds.includes(cue.statusId)) return false;
    if (conditions.skillIds?.length && !conditions.skillIds.includes(cue.skillId)) return false;
    if (conditions.tags?.length && !conditions.tags.every(tag => cue.tags?.includes(tag))) return false;
    return true;
  }

  preloadConfiguredSources() {
    const preloadTypes = new Set(this.manifest?.preloadTypes || []);
    if (!preloadTypes.size || !this.audio?.preloadOneShotSources) return;
    const sources = [];
    for (const characterPools of Object.values(this.manifest?.pools || {})) {
      for (const [type, pool] of Object.entries(characterPools || {})) {
        if (!preloadTypes.has(type)) continue;
        sources.push(...(pool.sources || []));
      }
    }
    this.audio.preloadOneShotSources([...new Set(sources)]);
  }
}

window.VoiceDirector = VoiceDirector;
