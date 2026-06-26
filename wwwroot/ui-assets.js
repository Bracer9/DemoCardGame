(() => {
  const escape = value => String(value ?? '').replace(/[&<>'"]/g, char => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
  })[char]);
  const safeClass = value => String(value ?? '').toLowerCase().replace(/[^a-z0-9-]/g, '-');

  class UiAssetRegistry {
    constructor() {
      this.version = 0;
      this.basePath = '/assets/ui';
      this.icons = {};
      this.bindings = {};
      this.loaded = false;
    }

    async load(url = '/config/ui-assets.json') {
      const response = await fetch(url, { cache: 'no-cache' });
      if (!response.ok) throw new Error(`UI asset manifest could not be loaded (${response.status}).`);
      const manifest = await response.json();
      if (!manifest.icons || typeof manifest.icons !== 'object') throw new Error('UI asset manifest has no icons map.');
      this.version = manifest.version || 1;
      this.basePath = String(manifest.basePath || '/assets/ui').replace(/\/$/, '');
      this.icons = manifest.icons;
      this.bindings = manifest.bindings || {};
      this.loaded = true;
      window.dispatchEvent(new CustomEvent('tiny-pixel-assets-ready', { detail: { version: this.version } }));
      return this;
    }

    binding(group, key, fallback = null) {
      return this.bindings[group]?.[key] || fallback;
    }

    forDamageType(type) { return this.binding('damageTypes', type, 'event.physical'); }
    forLogTone(tone) { return this.binding('logTones', tone, 'event.skill'); }
    forStatus(id) { return this.binding('statuses', id, `status.${id || 'unknown'}`); }
    forSkill(id) { return this.binding('skills', id, `skill.${id || 'unknown'}`); }

    resolveSource(source) {
      if (!source) return null;
      if (/^data:/i.test(source)) return source;
      const resolved = /^(?:https?:|\/)/i.test(source)
        ? source
        : `${this.basePath}/${String(source).replace(/^\//, '')}`;
      const separator = String(resolved).includes('?') ? '&' : '?';
      return `${resolved}${separator}v=${encodeURIComponent(this.version)}`;
    }

    icon(id, options = {}) {
      const definition = this.icons[id] || {};
      const category = definition.category || String(id || 'unknown').split('.')[0] || 'unknown';
      const source = this.resolveSource(definition.source);
      const size = ['xs', 'sm', 'md', 'lg'].includes(options.size) ? options.size : 'sm';
      const label = options.label || id || 'UI icon';
      const classes = [
        'ui-asset-icon', `ui-asset-icon--${size}`, `ui-asset-icon--${safeClass(category)}`,
        `tone-${safeClass(definition.tone || 'neutral')}`, source ? 'has-source' : 'is-placeholder',
        options.className || ''
      ].filter(Boolean).join(' ');
      const image = source ? `<img data-ui-asset-image src="${escape(source)}" alt="" draggable="false">` : '';
      return `<span class="${escape(classes)}" data-ui-asset="${escape(id || 'unknown')}" role="img" aria-label="${escape(label)}">${image}<i class="ui-asset-fallback" aria-hidden="true">${escape(definition.fallback || '?')}</i></span>`;
    }

    hydrate(root = document) {
      root.querySelectorAll?.('[data-ui-asset-image]').forEach(image => {
        if (image.dataset.uiAssetBound === 'true') return;
        image.dataset.uiAssetBound = 'true';
        const shell = image.closest('.ui-asset-icon');
        const loaded = () => shell?.classList.add('has-image');
        const failed = () => shell?.classList.add('image-failed');
        image.addEventListener('load', loaded, { once: true });
        image.addEventListener('error', failed, { once: true });
        if (image.complete) image.naturalWidth > 0 ? loaded() : failed();
      });
    }
  }

  window.TinyPixelAssets = new UiAssetRegistry();
})();
