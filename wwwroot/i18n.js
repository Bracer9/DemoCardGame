(() => {
  const resources = {};
  let language;
  try { language = localStorage.getItem('tpf-language') === 'zh' ? 'zh' : 'ja'; }
  catch { language = 'ja'; }

  const load = async () => {
    const [ja, zh] = await Promise.all([
      fetch('/locales/ja.json').then(response => response.json()),
      fetch('/locales/zh.json').then(response => response.json())
    ]);
    resources.ja = ja;
    resources.zh = zh;
    setLanguage(language);
  };

  const current = () => resources[language] || resources.ja || {};
  const fallback = () => resources.ja || {};
  const format = (value, params = {}) => Object.entries(params).reduce(
    (text, [key, replacement]) => text.replaceAll(`{${key}}`, String(replacement ?? '')),
    String(value ?? ''));
  const lookup = (section, key) => current()[section]?.[key] ?? fallback()[section]?.[key];

  const t = (key, params) => format(lookup('ui', key) ?? key, params);
  const characterName = key => lookup('characters', key) ?? key;
  const playerName = value => lookup('players', value) ?? value;
  const damageType = value => lookup('damageTypes', value) ?? value;
  const skillKind = value => lookup('skillKinds', value) ?? value;
  const skill = id => {
    const value = lookup('skills', id) || {};
    return { id, name: value.name ?? id, description: value.description ?? '', card: value.card ?? value.description ?? '' };
  };
  const status = value => {
    const localized = lookup('statuses', value.id) || {};
    const params = { magnitude: value.magnitude };
    return {
      ...value,
      name: format(localized.name ?? value.id, params),
      timing: format(localized.timing ?? '', params),
      description: format(localized.description ?? '', params)
    };
  };

  const localizeArg = arg => {
    if (!arg || typeof arg !== 'object') return arg ?? '';
    switch (arg.kind) {
      case 'character': return characterName(arg.value);
      case 'player': return playerName(arg.value);
      case 'damageType': return damageType(arg.value);
      case 'skill': return skill(arg.value).name;
      case 'status': return status({ id: arg.value, magnitude: 0 }).name;
      case 'ui': return t(arg.value);
      default: return arg.value ?? '';
    }
  };
  const message = value => {
    if (value == null) return '';
    if (typeof value === 'string') return value;
    const template = lookup('messages', value.key) ?? value.key;
    const args = Object.fromEntries(Object.entries(value.args || {}).map(([key, arg]) => [key, localizeArg(arg)]));
    return format(template, args);
  };

  const setLanguage = value => {
    language = value === 'zh' ? 'zh' : 'ja';
    try { localStorage.setItem('tpf-language', language); } catch { /* storage may be unavailable */ }
    document.documentElement.lang = language === 'zh' ? 'zh-CN' : 'ja';
  };
  const toggle = () => setLanguage(language === 'ja' ? 'zh' : 'ja');

  setLanguage(language);
  window.TinyPixelI18n = {
    load, t, message, toggle, setLanguage,
    get language() { return language; },
    characterName, playerName, damageType, skillKind, skill, status
  };
})();
