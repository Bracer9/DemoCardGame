const ui = {
  viewport: document.querySelector('#game-viewport'), stage: document.querySelector('#game-root'),
  app: document.querySelector('#app'), opponentCards: document.querySelector('#opponent-cards'), activeCards: document.querySelector('#active-cards'),
  heroDraftBanner: document.querySelector('#hero-draft-banner'),
  opponentName: document.querySelector('#opponent-name'), bottomName: document.querySelector('#bottom-name'), activePlayer: document.querySelector('#active-player'),
  hudCluster: document.querySelector('.hud-cluster'), turn: document.querySelector('#turn-number'), round: document.querySelector('#round-number'), ap: document.querySelector('#ap-pips'), apCurrent: document.querySelector('#ap-current'), apMaximum: document.querySelector('#ap-maximum'), instruction: document.querySelector('#instruction'),
  preview: document.querySelector('#preview-panel'), previewAttacker: document.querySelector('#preview-attacker'), previewDefender: document.querySelector('#preview-defender'),
  previewDamage: document.querySelector('#preview-damage'), previewCounter: document.querySelector('#preview-counter'), previewTrait: document.querySelector('#preview-trait'),
  previewNotes: document.querySelector('#preview-notes'), confirm: document.querySelector('#confirm-attack'), cancelPreview: document.querySelector('#cancel-preview'), log: document.querySelector('#battle-log'),
  rewardWindow: document.querySelector('#reward-window'), rewardOptions: document.querySelector('#reward-options'), rewardTitle: document.querySelector('#reward-window h2'), rewardSubtitle: document.querySelector('#reward-window .reward-header p'), rewardReset: document.querySelector('#reward-reset'), rewardInlineBack: document.querySelector('#reward-inline-back'), rewardSkip: document.querySelector('#reward-skip'), rewardWaiting: document.querySelector('#reward-waiting'), rewardChildBack: document.querySelector('#reward-child-back'),
  heroDraftWindow: document.querySelector('#hero-draft-window'), heroDraftOptions: document.querySelector('#hero-draft-options'), heroDraftTitle: document.querySelector('#hero-draft-title'), heroDraftSubtitle: document.querySelector('#hero-draft-subtitle'), heroDraftWaiting: document.querySelector('#hero-draft-waiting'), heroDraftUpgrade: document.querySelector('#hero-draft-upgrade'), heroDraftConfirm: document.querySelector('#hero-draft-confirm'), heroDraftReset: document.querySelector('#hero-draft-reset'), heroDraftBack: document.querySelector('#hero-draft-back'),
  deputyConfirm: document.querySelector('#deputy-confirm-modal'), deputyConfirmTitle: document.querySelector('#deputy-confirm-title'), deputyConfirmSoldier: document.querySelector('#deputy-confirm-soldier'), deputyConfirmHero: document.querySelector('#deputy-confirm-hero'), deputyConfirmEffect: document.querySelector('#deputy-confirm-effect'), deputyConfirmNote: document.querySelector('#deputy-confirm-note'), deputyConfirmOk: document.querySelector('#deputy-confirm-ok'), deputyConfirmCancel: document.querySelector('#deputy-confirm-cancel'),
  toast: document.querySelector('#toast'), curtain: document.querySelector('#turn-curtain'), curtainPlayer: document.querySelector('#curtain-player'), fx: document.querySelector('#fx-layer'),
  gameOver: document.querySelector('#game-over'), resultTitle: document.querySelector('#result-title'), resultCopy: document.querySelector('#result-copy'),
  inspector: document.querySelector('#character-inspector'), apHud: document.querySelector('#action-point-hud'),
  roleActionInspector: document.querySelector('#role-action-inspector'),
  bpHud: document.querySelector('#battle-point-hud'), bpInspector: document.querySelector('#bp-inspector'), activeBpValue: document.querySelector('#active-bp-value'), activeBpTurnGain: document.querySelector('#active-bp-turn-gain'),
  relicOverview: document.querySelector('#relic-overview'), relicOverviewButton: document.querySelector('#relic-overview-button'), relicOverviewDetail: document.querySelector('#relic-overview-detail'),
  statusInspector: document.querySelector('#status-inspector'), shieldInspector: document.querySelector('#shield-inspector'), shieldButton: document.querySelector('#deploy-shield'),
  activeShield: document.querySelector('#active-shield'), opponentShield: document.querySelector('#opponent-shield'),
  activeShieldDome: document.querySelector('#active-shield-dome'), opponentShieldDome: document.querySelector('#opponent-shield-dome'),
  attackArrow: document.querySelector('#attack-drag-arrow')
};
ui.endTurn = document.querySelector('#end-turn');

const STAGE_WIDTH = 1920;
const STAGE_HEIGHT = 1080;
let stageScale = 1;

function resizeGameStage() {
  if (!ui.stage) return;
  const viewport = window.visualViewport || window;
  const width = viewport.width || window.innerWidth;
  const height = viewport.height || window.innerHeight;
  const offsetLeft = viewport.offsetLeft || 0;
  const offsetTop = viewport.offsetTop || 0;
  stageScale = Math.min(width / STAGE_WIDTH, height / STAGE_HEIGHT);
  const scaledWidth = STAGE_WIDTH * stageScale;
  const scaledHeight = STAGE_HEIGHT * stageScale;
  ui.stage.style.left = `${offsetLeft + Math.max(0, (width - scaledWidth) / 2)}px`;
  ui.stage.style.top = `${offsetTop + Math.max(0, (height - scaledHeight) / 2)}px`;
  ui.stage.style.transform = `scale(${stageScale})`;
}

function stageRootRect() {
  return ui.stage?.getBoundingClientRect() || { left: 0, top: 0, width: STAGE_WIDTH, height: STAGE_HEIGHT };
}

function clientToStage(x, y) {
  const rect = stageRootRect();
  return {
    x: (x - rect.left) / stageScale,
    y: (y - rect.top) / stageScale
  };
}

function stageRect(element) {
  const rect = element.getBoundingClientRect();
  const root = stageRootRect();
  return {
    left: (rect.left - root.left) / stageScale,
    top: (rect.top - root.top) / stageScale,
    width: rect.width / stageScale,
    height: rect.height / stageScale,
    get right() { return this.left + this.width; },
    get bottom() { return this.top + this.height; }
  };
}

const touchModeQuery = window.matchMedia?.('(pointer: coarse)');

function isMacDevice() {
  const platform = navigator.userAgentData?.platform || navigator.platform || '';
  const userAgent = navigator.userAgent || '';
  return (/mac/i.test(platform) || /Macintosh/i.test(userAgent)) && Number(navigator.maxTouchPoints || 0) <= 1;
}

function isIosDevice() {
  const platform = navigator.userAgentData?.platform || navigator.platform || '';
  const userAgent = navigator.userAgent || '';
  const maxTouchPoints = Number(navigator.maxTouchPoints || 0);
  return /iPad|iPhone|iPod/i.test(platform)
    || /iPad|iPhone|iPod/i.test(userAgent)
    || (/Macintosh/i.test(userAgent) && maxTouchPoints > 1);
}

function isTouchMode() {
  return Boolean(touchModeQuery?.matches);
}

function syncDeviceFxMode() {
  document.body.classList.toggle('mac-low-motion', isMacDevice());
  document.body.classList.toggle('ios-low-motion', isIosDevice());
}

function syncTouchMode() {
  document.body.classList.toggle('touch-mode', isTouchMode());
}

syncDeviceFxMode();
syncTouchMode();
touchModeQuery?.addEventListener?.('change', syncTouchMode);

function isTouchPointer(event) {
  return isTouchMode() && (event.pointerType === 'touch' || event.pointerType === 'pen');
}

function suppressNextTouchClick() {
  suppressTouchClickUntil = performance.now() + 650;
}

function shouldSuppressTouchClick() {
  return performance.now() < suppressTouchClickUntil;
}

function clearTouchDropReady() {
  document.querySelectorAll('.drop-ready').forEach(element => element.classList.remove('drop-ready'));
}

function touchPoint(event) {
  return { clientX: event.clientX, clientY: event.clientY, ...clientToStage(event.clientX, event.clientY) };
}

function targetAtClientPoint(clientX, clientY, selector, validator = null) {
  if (!selector) return null;
  const elements = typeof document.elementsFromPoint === 'function'
    ? document.elementsFromPoint(clientX, clientY)
    : [document.elementFromPoint(clientX, clientY)];
  for (const element of elements) {
    const target = element instanceof Element ? element.closest(selector) : null;
    if (target && (!validator || validator(target))) return target;
  }
  return null;
}

function targetUnderPointer(event, selector, validator = null) {
  return targetAtClientPoint(event.clientX, event.clientY, selector, validator);
}

function beginTouchAttack(card, event) {
  if (!isTouchPointer(event) || busy || dealing || ui.preview.classList.contains('open')) return false;
  if (event.target instanceof Element && event.target.closest('.status-chip,.deputy-badge')) {
    event.preventDefault();
    event.stopPropagation();
    suppressNextTouchClick();
    inspectedCardId = card.dataset.id;
    showCharacterInspector(card);
    return true;
  }
  if (!card.classList.contains('can-act') || card.dataset.side !== 'active') return false;
  event.preventDefault();
  event.stopPropagation();
  suppressNextTouchClick();
  const wasAlreadySelected = selectedAttacker === card.dataset.id;
  selectedAttacker = card.dataset.id;
  selectedDefender = null;
  inspectedCardId = card.dataset.id;
  closePreview();
  if (!wasAlreadySelected) {
    sound.emit('ui.card-select');
    emitSelectVoice(card);
  }
  document.querySelectorAll('.fighter-card.selected').forEach(element => element.classList.remove('selected'));
  card.classList.add('selected');
  ui.app.classList.add('attacker-selected');
  showCharacterInspector(card);
  updateInstruction();
  touchDrag = {
    mode: 'attack',
    pointerId: event.pointerId,
    sourceElement: card,
    startClientX: event.clientX,
    startClientY: event.clientY,
    active: false,
    currentTarget: null
  };
  card.setPointerCapture?.(event.pointerId);
  return true;
}

function beginTouchRoleAction(button, event) {
  if (!isTouchPointer(event) || busy || dealing || ui.preview.classList.contains('open')) return false;
  if (!button || button.disabled || button.classList.contains('choice')) return false;
  const characterId = button.dataset.characterId;
  const roleActionId = button.dataset.roleActionId;
  if (!characterId || !roleActionId || button.dataset.roleActionMode !== 'Targeted') return false;
  const targets = String(button.dataset.roleActionTargets || '').split(',').filter(Boolean);
  if (!targets.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind))) return false;
  event.preventDefault();
  event.stopPropagation();
  suppressNextTouchClick();
  showRoleActionInspector(button);
  touchDrag = {
    mode: 'role-action',
    pointerId: event.pointerId,
    sourceElement: characterElementById(characterId) || button,
    characterId,
    roleActionId,
    targets,
    startClientX: event.clientX,
    startClientY: event.clientY,
    active: false,
    currentTarget: null
  };
  button.setPointerCapture?.(event.pointerId);
  return true;
}

function activateTouchDrag(state) {
  if (!state || state.active) return;
  state.active = true;
  if (state.mode === 'role-action') {
    beginRoleActionTargeting(state.characterId, state.roleActionId, state.targets, { renderFirst: false, startArrow: false });
    roleActionDragActive = true;
  }
  if (state.mode === 'attack') hideCharacterInspector();
  startAttackArrow(state.sourceElement, state.mode === 'role-action' ? 'role-action' : 'attack');
  ui.app.classList.add('dragging-attack');
  document.body.classList.add('dragging-attack');
  updateInstruction();
}

function updateTouchDrag(event) {
  const state = touchDrag;
  if (!state || event.pointerId !== state.pointerId) return;
  event.preventDefault();
  event.stopPropagation();
  const distance = Math.hypot(event.clientX - state.startClientX, event.clientY - state.startClientY);
  if (!state.active && distance < 8) return;
  activateTouchDrag(state);
  const selector = state.mode === 'role-action'
    ? roleActionCardTargetSelector(pendingRoleAction)
    : '.fighter-card[data-side="opponent"]:not(.defeated):not(.deploying)';
  const target = targetUnderPointer(event, selector, candidate =>
    state.mode !== 'role-action' || canRoleActionTargetCard(pendingRoleAction, candidate));
  clearTouchDropReady();
  if (target) target.classList.add('drop-ready');
  state.currentTarget = target;
  const point = touchPoint(event);
  updateAttackArrow(point.x, point.y, target);
}

function endTouchDrag(event, cancelled = false) {
  const state = touchDrag;
  if (!state || event.pointerId !== state.pointerId) return;
  event.preventDefault();
  event.stopPropagation();
  suppressNextTouchClick();
  const target = !cancelled && state.active ? state.currentTarget : null;
  clearTouchDropReady();
  ui.app.classList.remove('dragging-attack');
  document.body.classList.remove('dragging-attack');
  touchDrag = null;
  if (target && state.mode === 'attack') {
    finishAttackArrow(target);
    chooseDefender(target.dataset.id);
    return;
  }
  if (target && state.mode === 'role-action' && pendingRoleAction) {
    const action = pendingRoleAction;
    finishAttackArrow(target);
    useRoleAction(action.characterId, action.roleActionId, target.dataset.id);
    return;
  }
  if (state.active) {
    if (state.mode === 'role-action') cancelAiming();
    else finishAttackArrow();
  }
}

function parseCssTimeVariable(name, fallbackMs) {
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  if (!value) return fallbackMs;
  const number = Number.parseFloat(value);
  if (!Number.isFinite(number)) return fallbackMs;
  return value.endsWith('ms') ? number : number * 1000;
}

function cssVariable(name, fallback = '') {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
}

function startHudSheenLoop() {
  const sheen = document.querySelector('.hud-sheen');
  if (!sheen) return;

  const reducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)');
  let timer = null;
  let animation = null;

  const clear = () => {
    if (timer) window.clearTimeout(timer);
    timer = null;
    animation?.cancel();
    animation = null;
    sheen.style.opacity = '0';
  };

  const play = () => {
    if (reducedMotion?.matches) {
      clear();
      return;
    }

    const cycleMs = Math.max(500, parseCssTimeVariable('--command-hud-sheen-cycle', 6000));
    const travelMs = Math.max(120, Math.min(cycleMs, parseCssTimeVariable('--command-hud-sheen-travel-duration', 1600)));
    const opacity = Number.parseFloat(cssVariable('--command-hud-sheen-opacity', '.30')) || .30;
    const startX = cssVariable('--command-hud-sheen-start-x', '-48px');
    const endX = cssVariable('--command-hud-sheen-end-x', '136px');

    animation?.cancel();
    animation = sheen.animate([
      { opacity: 0, transform: `translateY(-50%) translateX(${startX})`, offset: 0 },
      { opacity: 0, transform: `translateY(-50%) translateX(${startX})`, offset: .10 },
      { opacity, transform: `translateY(-50%) translateX(${startX})`, offset: .24 },
      { opacity, transform: `translateY(-50%) translateX(calc(${endX} - 28px))`, offset: .70 },
      { opacity: opacity * .35, transform: `translateY(-50%) translateX(calc(${endX} - 10px))`, offset: .88 },
      { opacity: 0, transform: `translateY(-50%) translateX(${endX})`, offset: 1 }
    ], {
      duration: travelMs,
      easing: 'cubic-bezier(.22,.61,.36,1)',
      fill: 'forwards'
    });

    timer = window.setTimeout(play, cycleMs);
  };

  reducedMotion?.addEventListener?.('change', () => {
    clear();
    play();
  });

  play();
}

resizeGameStage();
startHudSheenLoop();

const i18n = window.TinyPixelI18n;
const art = window.TinyPixelAssets;

ui.startScreen = document.querySelector('#start-screen');
ui.startTrigger = document.querySelector('#start-trigger');
ui.startTest = document.querySelector('#start-test');
ui.startAi = document.querySelector('#start-ai');
ui.dealSequence = document.querySelector('#deal-sequence');
ui.dealStatus = document.querySelector('#deal-status');
ui.dealCaption = document.querySelector('.deal-caption strong');
ui.dealDeck = document.querySelector('#deal-deck');
ui.soundToggle = document.querySelector('#sound-toggle');
ui.audioMenu = document.querySelector('#audio-menu');
ui.audioPanel = document.querySelector('#audio-panel');
ui.bgmToggle = document.querySelector('#bgm-toggle');
ui.sfxToggle = document.querySelector('#sfx-toggle');
ui.voiceToggle = document.querySelector('#voice-toggle');
ui.bgmVolume = document.querySelector('#bgm-volume');
ui.sfxVolume = document.querySelector('#sfx-volume');
ui.voiceVolume = document.querySelector('#voice-volume');
ui.bgmVolumeValue = document.querySelector('#bgm-volume-value');
ui.sfxVolumeValue = document.querySelector('#sfx-volume-value');
ui.voiceVolumeValue = document.querySelector('#voice-volume-value');
ui.bgmTrackSelect = document.querySelector('#bgm-track-select');
ui.languageToggle = document.querySelector('#language-toggle');
ui.modeSelect = document.querySelector('#mode-select');
ui.onlineLobby = document.querySelector('#online-lobby');
ui.lobbyEntry = document.querySelector('#lobby-entry');
ui.lobbyWaiting = document.querySelector('#lobby-waiting');
ui.lobbyBack = document.querySelector('#lobby-back');
ui.playerName = document.querySelector('#player-name');
ui.roomCode = document.querySelector('#room-code');
ui.joinFields = document.querySelector('#join-fields');
ui.lobbyRole = document.querySelector('#lobby-role');
ui.lobbyStatus = document.querySelector('#lobby-status');
ui.joinLink = document.querySelector('#join-link');
ui.lobbyPlayers = document.querySelector('#lobby-players');
ui.startMatch = document.querySelector('#start-match');

const sound = new window.AudioDirector();
const voice = new window.VoiceDirector(sound);
const audioGateButtons = [ui.startTrigger, ui.startTest, ui.startAi, document.querySelector('#create-room'), document.querySelector('#join-room')].filter(Boolean);
audioGateButtons.forEach(button => { button.disabled = true; });
const audioLoadPromise = sound.load('/config/audio.json')
  .catch(error => console.warn(error.message))
  .finally(() => {
    audioGateButtons.forEach(button => { button.disabled = false; });
    renderAudioControls();
  });
const voiceLoadPromise = voice.load('/config/voice.json')
  .catch(error => console.warn(error.message));
art.load('/config/ui-assets.json').then(() => { if (game) render(); }).catch(error => console.warn(error.message));
document.addEventListener('pointerdown', () => sound.unlock({ primeUnrequested: false }), { capture: true });
document.addEventListener('keydown', () => sound.unlock({ primeUnrequested: false }), { capture: true });
document.addEventListener('visibilitychange', () => { if (!document.hidden) sound.ensureBgmPlaying(); });
window.addEventListener('focus', () => sound.ensureBgmPlaying());

let game = null;
let selectedAttacker = null;
let selectedDefender = null;
let inspectedCardId = null;
let suppressedOpponentInspectorId = null;
let selectedHeroDraftKey = null;
let selectedHeroDraftKeys = [];
let soldierUpgradeKey = null;
let pendingRoleAction = null;
let pendingDeputy = null;
let pendingDeputyConfirm = null;
let preview = null;
let busy = false;
let hasStarted = false;
let dealing = false;
let initialLoadPromise = null;
let dragArrowOrigin = null;
let dragArrowSourceElement = null;
let roleActionDragActive = false;
let touchDrag = null;
let suppressTouchClickUntil = 0;
let sessionMode = null;
const LOCAL_SESSION_TOKEN_KEY = 'tpf-local-session-token';
function createLocalSessionToken() {
  const bytes = new Uint8Array(16);
  if (window.crypto?.getRandomValues) {
    window.crypto.getRandomValues(bytes);
    return Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
}
function loadLocalSessionToken() {
  let token = localStorage.getItem(LOCAL_SESSION_TOKEN_KEY);
  if (!token) {
    token = createLocalSessionToken();
    localStorage.setItem(LOCAL_SESSION_TOKEN_KEY, token);
  }
  return token;
}
let localSessionToken = loadLocalSessionToken();
let playerToken = localStorage.getItem('tpf-online-player-token') || '';
let room = null;
let pollTimer = null;
let lastGameJson = '';
let endTurnQueued = false;
let lastApSnapshot = null;
let lastBpGainSnapshot = null;
let lastRewardRenderKey = '';
let lastAnimatedLogSequence = 0;
let eventPlayback = false;
let resultAudioGameId = null;
let pendingVisualBaselines = null;
let rewardVisualHold = false;
let aiAdvancing = false;
let aiAdvanceTimer = null;
let turnCurtainLock = false;
const cardRenderSignatures = new WeakMap();
const recentSoundEvents = new Map();
const VOICE_REACTION_DELAY_MS = 760;
const ORDINARY_HIT_VOICE_REACTION_DELAY_MS = 980;
const GUARD_VOICE_REACTION_DELAY_MS = 520;
const GUARDED_TARGET_VOICE_REACTION_DELAY_MS = 1320;
function emitSoundThrottled(eventName, cooldownMs = 300) {
  const now = performance.now();
  if (now - (recentSoundEvents.get(eventName) || -Infinity) < cooldownMs) return;
  recentSoundEvents.set(eventName, now);
  sound.emit(eventName);
}
function emitVoice(type, characterOrKey, options = {}) {
  const actorCharacterId = typeof characterOrKey === 'string' ? characterOrKey : characterOrKey?.key;
  if (!actorCharacterId) return false;
  return voice.emit({ type, actorCharacterId, ...options });
}
function emitVoiceDelayed(type, characterOrKey, options = {}, delayMs = VOICE_REACTION_DELAY_MS) {
  window.setTimeout(() => emitVoice(type, characterOrKey, options), delayMs);
}
function emitSelectVoice(characterOrElement) {
  const character = characterOrElement instanceof Element ? findCard(characterOrElement.dataset.id) : characterOrElement;
  if (!character?.key) return false;
  if (isLowHpForSelect(character) && hasVoiceSources(character.key, 'select-low-hp'))
    return emitVoice('select-low-hp', character.key, {
      currentHp: Number(character.currentHp || 0),
      maxHp: Number(character.maxHp || 0),
      lowHpThreshold: selectLowHpThreshold(character)
    });
  return emitVoice('select', character.key);
}
function hasVoiceSources(characterKey, type) {
  return Boolean(voice.poolFor?.(characterKey, type)?.sources?.length);
}
function voiceReactionDelay(attackerType, defenderType) {
  if (attackerType === 'attack-declare' && defenderType === 'damage-taken') return ORDINARY_HIT_VOICE_REACTION_DELAY_MS;
  return VOICE_REACTION_DELAY_MS;
}
function characterByKey(state, key) {
  return state?.players.flatMap(player => player.characters).find(card => card.key === key) || null;
}
function characterByIdData(state, id) {
  return id ? state?.players.flatMap(player => player.characters).find(card => String(card.id) === String(id)) || null : null;
}
function characterFromLogData(state, entry, name = 'character') {
  return characterByIdData(state, logArg(entry, `${name}Id`)) || characterByKey(state, logArg(entry, name));
}
function damageVoiceTypeForCharacter(character, amount) {
  const damage = Number(amount || 0);
  if (damage <= 0 || !character) return null;
  if (character.currentHp <= 0) return 'death';
  return damage >= heavyDamageThreshold(character) ? 'heavy-damage-taken' : 'damage-taken';
}
function emitDamageTakenVoice(characterOrKey, amount, state, options = {}) {
  const damage = Number(amount || 0);
  if (damage <= 0 || !characterOrKey) return;
  const character = typeof characterOrKey === 'object' ? characterOrKey : characterByKey(state, characterOrKey);
  if (character && character.currentHp <= 0 && options.suppressIfDefeated !== false) return;
  const type = damageVoiceTypeForCharacter(character, damage);
  if (!type) return;
  emitVoiceDelayed(type, character?.key || characterOrKey, {
    ...options,
    amount: damage,
    maxHp: Number(character?.maxHp || 0),
    heavyThreshold: heavyDamageThreshold(character)
  });
}
function heavyDamageThreshold(character) {
  const maxHp = Number(character?.maxHp || 0);
  return Math.max(1, Math.ceil(maxHp * Number(voice.manifest?.thresholds?.heavyDamageTakenMaxHpRatio ?? 0.25)));
}
function selectLowHpThreshold(character) {
  const maxHp = Number(character?.maxHp || 0);
  return Math.max(1, Math.ceil(maxHp * Number(voice.manifest?.thresholds?.selectLowHpMaxHpRatio ?? 0.25)));
}
function isLowHpForSelect(character) {
  const currentHp = Number(character?.currentHp || 0);
  const maxHp = Number(character?.maxHp || 0);
  return maxHp > 0 && currentHp > 0 && currentHp <= selectLowHpThreshold(character);
}
function damageVoiceType(characterKey, amount, state) {
  const damage = Number(amount || 0);
  if (damage <= 0 || !characterKey) return null;
  const character = characterByKey(state, characterKey);
  return damageVoiceTypeForCharacter(character, damage);
}

function emitGuardVoice(entry) {
  const knight = characterFromLogData(game, entry);
  const knightKey = knight?.key || logArg(entry, 'character');
  const amount = Number(logArg(entry, 'amount') || 0);
  if (!knightKey || amount <= 0) return;
  emitVoiceDelayed('damage-taken', knightKey, {
    source: 'guard',
    damageType: 'Physical',
    statusId: 'guard',
    amount
  }, GUARD_VOICE_REACTION_DELAY_MS);
}

async function playGuardRedirectEvent(entry, state, options = {}) {
  const amount = Number(logArg(entry, 'amount') || 0);
  const guardTarget = characterElementFromLog(state, entry);
  const protectedTarget = characterElementFromLog(state, entry, 'target');
  sound.emit('trait.guard-trigger');
  if (options.emitVoice) emitGuardVoice(entry);
  await Promise.all([
    eventBurst(guardTarget, eventIcon.trait, { title: i18n.trait('interposing-shield').name, secondaryIconId: art.forTrait('interposing-shield'), amount: `-${amount}`, tone: 'trait' }),
    eventBurst(protectedTarget, eventIcon.trait, { title: i18n.trait('interposing-shield').name, secondaryIconId: art.forTrait('interposing-shield'), amount: `-${amount}`, tone: 'trait' })
  ]);
}
function defenderDefeatedByExchange(defenderRef, attackDamage, state) {
  const defender = typeof defenderRef === 'object' ? defenderRef : characterByKey(state, defenderRef);
  return Number(attackDamage || 0) > 0 && Boolean(defender && defender.currentHp <= 0);
}
function forecastVoiceValue(forecast) {
  const min = Number(forecast?.min ?? forecast?.max ?? 0);
  const max = Number(forecast?.max ?? forecast?.min ?? min);
  return (min + max) / 2;
}
function attackPreviewVoiceType(previewData) {
  const dealt = forecastVoiceValue(previewData?.attack);
  const taken = forecastVoiceValue(previewData?.counter);
  if (dealt - taken >= 2) return 'attack-preview-overpower';
  if (taken - dealt >= 2) return 'attack-preview-disadvantage';
  return 'attack-preview-even';
}
function emitAttackPreviewVoice(attacker, defender, previewData) {
  if (!attacker?.key || !defender?.key || !previewData?.isValid) return;
  const type = attackPreviewVoiceType(previewData);
  emitVoice(type, attacker.key, {
    targetCharacterId: defender.key,
    previewDamageDealt: forecastVoiceValue(previewData.attack),
    previewDamageTaken: forecastVoiceValue(previewData.counter)
  });
}
const dragGhost = document.createElement('span');
dragGhost.style.cssText = 'position:fixed;left:-100px;top:-100px;width:1px;height:1px;opacity:0;';
document.body.appendChild(dragGhost);

const api = async (url, options = {}) => {
  const isOnlineApi = url.startsWith('/api/online');
  const isLocalGameApi = url.startsWith('/api/game');
  const headers = {
    'Content-Type': 'application/json',
    ...(isOnlineApi && playerToken ? { 'X-Player-Token': playerToken } : {}),
    ...(isLocalGameApi && localSessionToken ? { 'X-Local-Session': localSessionToken } : {}),
    ...(options.headers || {})
  };
  const response = await fetch(url, { ...options, headers });
  const payload = await response.json();
  if (!response.ok) {
    const error = new Error(payload.error ? i18n.message(payload.error) : i18n.t('communicationError'));
    error.status = response.status;
    throw error;
  }
  return payload;
};

const gameEndpoint = path => `${sessionMode === 'online' ? '/api/online' : '/api'}${path}`;
const gameApi = (path, options) => api(gameEndpoint(path), options);

const loadGame = async () => {
  const payload = await gameApi('/game/state');
  game = payload.data;
  lastGameJson = JSON.stringify(game);
  lastApSnapshot = null;
  lastBpGainSnapshot = null;
  resetEventCursor(game);
  render();
};

function canUseLocalControls() {
  return Boolean(game?.canControl && !turnCurtainLock);
}

function setTurnCurtainLock(locked) {
  if (turnCurtainLock === locked) return;
  turnCurtainLock = locked;
  if (game) render();
}

function render() {
  if (!game) return;
  const active = game.players.find(player => player.id === game.activePlayerId);
  const me = game.players.find(player => player.id === game.viewerPlayerId);
  const opponent = game.players.find(player => player.id !== game.viewerPlayerId);
  ui.turn.textContent = String(game.turnNumber).padStart(2, '0');
  ui.round.textContent = String(game.roundNumber).padStart(2, '0');
  ui.hudCluster?.classList.toggle('reward-next', Boolean(game.nextRoundIsRewardRound && !game.rewardWindow));
  ui.activePlayer.textContent = i18n.playerName(active.name);
  ui.bottomName.textContent = i18n.playerName(me.name);
  ui.opponentName.textContent = i18n.playerName(opponent.name);
  renderShieldBadge(ui.activeShield, me.sharedShield);
  renderShieldBadge(ui.opponentShield, opponent.sharedShield);
  renderBattlePoints(me);
  renderRelicOverview(me);
  renderRewardWindow();
  renderHeroDraftWindow();
  renderRewardChildBack();
  const activeSharedShield = active?.sharedShield || 0;
  const shieldActionIsReinforce = game.nextShieldCost === 1 && activeSharedShield > 0;
  const shieldActionIsOpen = game.nextShieldCost === 2 && activeSharedShield <= 0;
  const canReinforceShield = game.canDeployShield && shieldActionIsReinforce;
  const localControls = canUseLocalControls();
  const shieldUnavailable = !game.canDeployShield || !localControls || dealing || Boolean(game.pendingRoleActionUpgrade) || Boolean(game.heroDraft) || Boolean(game.pendingRelicReward);
  ui.shieldButton.disabled = shieldUnavailable;
  ui.shieldButton.classList.toggle('unavailable', shieldUnavailable);
  ui.shieldButton.setAttribute('aria-disabled', String(shieldUnavailable));
  ui.shieldButton.classList.toggle('deployed', me.sharedShield > 0);
  ui.shieldButton.classList.toggle('reinforce-ready', canReinforceShield);
  ui.shieldButton.classList.toggle('reinforced', me.sharedShield > 2);
  if (shieldActionIsOpen) {
    ui.shieldButton.innerHTML = `${art.icon('event.shield', { size: 'md', label: i18n.t('defenseFormation'), className: 'command-icon' })}<span>${game.nextShieldCost} AP / SHIELD 2</span><b>${i18n.t('defenseFormation')}</b>`;
    ui.shieldButton.setAttribute('aria-label', `${game.nextShieldCost} AP / ${i18n.t('defenseFormation')} / SHIELD 2`);
  } else if (shieldActionIsReinforce) {
    const nextShield = Math.min(5, activeSharedShield + 2);
    ui.shieldButton.innerHTML = `${art.icon('event.shield', { size: 'md', label: i18n.t('reinforceFormation'), className: 'command-icon' })}<span>${game.nextShieldCost} AP / SHIELD +2 → ${nextShield}</span><b>${i18n.t('reinforceFormation')}</b>`;
    ui.shieldButton.setAttribute('aria-label', `${game.nextShieldCost} AP / ${i18n.t('reinforceFormation')} / SHIELD +2 / ${nextShield}`);
  } else {
    ui.shieldButton.innerHTML = `${art.icon('event.shield', { size: 'md', label: i18n.t('deployed'), className: 'command-icon' })}<span>MAXIMUM</span><b>${i18n.t('deployed')}</b>`;
    ui.shieldButton.setAttribute('aria-label', i18n.t('deployed'));
  }
  ui.apCurrent.textContent = game.actionPoints;
  ui.apMaximum.textContent = game.maxActionPoints;
  ui.apHud.classList.toggle('depleted', game.actionPoints === 0);
  ui.apHud.classList.toggle('ap-panel--native-five', game.maxActionPoints === 5);
  ui.apHud.classList.toggle('ap-panel--dynamic-capacity', game.maxActionPoints !== 5);
  const apContext = `${game.gameId}:${game.turnNumber}:${game.activePlayerId}:${game.viewerPlayerId}`;
  const spentAp = lastApSnapshot?.context === apContext && lastApSnapshot.actionPoints > game.actionPoints
    ? Math.min(lastApSnapshot.actionPoints - game.actionPoints, Math.max(0, game.maxActionPoints - game.actionPoints))
    : 0;
  const apOrbs = [
    ...Array.from({ length: game.actionPoints }, () => '<i class="ap-pip ap-orb filled ap-orb--filled"></i>'),
    ...Array.from({ length: spentAp }, () => '<i class="ap-pip ap-orb ap-orb--spent" aria-hidden="true"></i>'),
    ...Array.from({ length: Math.max(0, game.maxActionPoints - game.actionPoints - spentAp) }, () => '<i class="ap-pip ap-orb ap-orb--empty"></i>')
  ];
  ui.ap.innerHTML = apOrbs.join('');
  ui.apHud.classList.toggle('ap-spending', spentAp > 0);
  if (spentAp > 0) {
    ui.ap.querySelectorAll('.ap-orb--spent').forEach(orb => {
      orb.addEventListener('animationend', () => orb.remove(), { once: true });
    });
  }
  lastApSnapshot = { context: apContext, actionPoints: game.actionPoints };
  const pendingDefeatIds = pendingDefeatAnimationIds(game);
  const activeCharacters = visibleBattleCharacters(me, pendingDefeatIds);
  const opponentCharacters = visibleBattleCharacters(opponent, pendingDefeatIds);
  ui.activeCards.dataset.count = String(activeCharacters.length);
  ui.opponentCards.dataset.count = String(opponentCharacters.length);
  renderBattleCardRow(ui.activeCards, activeCharacters, true);
  renderBattleCardRow(ui.opponentCards, opponentCharacters, false);
  renderPersistentShield(ui.activeShieldDome, ui.activeCards, me, hasPendingShieldBreakForPlayer(game, me));
  renderPersistentShield(ui.opponentShieldDome, ui.opponentCards, opponent, hasPendingShieldBreakForPlayer(game, opponent));
  bindCards();
  renderLog();
  updateInstruction();
  renderGameOver();
  ui.app.classList.toggle('turn-locked', !localControls);
  ui.app.classList.toggle('role-action-upgrade-pending', Boolean(game.pendingRoleActionUpgrade?.canChoose));
  ui.app.classList.toggle('hero-draft-pending', Boolean(game.heroDraft));
  ui.app.classList.toggle('relic-reward-pending', Boolean(game.pendingRelicReward));
  ui.app.classList.toggle('hero-draft-opening', game.heroDraft?.kind === 'Opening' || game.heroDraft?.kind === 'TestOpening');
  ui.app.classList.toggle('soldier-draft-pending', game.heroDraft?.kind === 'SoldierOpening' || game.heroDraft?.kind === 'SoldierRecruit');
  ui.app.classList.toggle('soldier-upgrade-targeting', Boolean(soldierUpgradeKey));
  ui.app.classList.toggle('hero-draft-can-choose', Boolean(game.heroDraft?.canChoose));
  ui.app.classList.toggle('dealing', dealing);
  renderHeroDraftBanner();
  ui.app.classList.toggle('role-action-targeting', Boolean(pendingRoleAction));
  ui.app.classList.toggle('deputy-targeting', Boolean(pendingDeputy));
  ui.app.classList.toggle('attacker-selected', Boolean(selectedAttacker));
  ui.endTurn.disabled = !localControls || game.phase === 'Finished' || Boolean(game.pendingRoleActionUpgrade) || Boolean(game.heroDraft) || Boolean(game.pendingRelicReward);
  ui.endTurn.classList.toggle('queued', endTurnQueued);
  ui.endTurn.classList.toggle('ap-empty-ready', localControls && game.phase !== 'Finished' && game.actionPoints === 0 && !endTurnQueued);
  ui.endTurn.setAttribute('aria-busy', String(endTurnQueued));
  document.querySelector('#new-game').disabled = !game.isHost;
  document.querySelector('#play-again').disabled = !game.isHost;
  art.hydrate(document);
  syncCombatInteractionUi({ syncInspector: false });
  syncSelectedInspector();
  scheduleAiAdvance();
}

function renderRewardWindow() {
  const reward = game?.rewardWindow;
  if (!ui.rewardWindow) return;
  const deferred = isRewardPresentationDeferred();
  const relicReward = game?.pendingRelicReward;
  const isRelicChild = Boolean(reward && relicReward?.canChoose);
  const rewardSource = isRelicChild ? relicReward : reward;
  const rewardChildOpen = Boolean(reward)
    && (Boolean(game?.pendingRoleActionUpgrade) || Boolean(game?.heroDraft && game.heroDraft.kind !== 'Opening' && game.heroDraft.kind !== 'TestOpening'));
  const open = Boolean(reward?.canChoose) && !rewardChildOpen && !deferred;
  ui.rewardWindow.classList.toggle('open', open);
  ui.rewardWindow.classList.toggle('can-choose', Boolean(reward?.canChoose));
  ui.rewardWindow.classList.toggle('relic-child', isRelicChild);
  ui.rewardWindow.classList.toggle('has-four-options', Number(rewardSource?.options?.length || 0) > 3);
  ui.rewardWindow.setAttribute('aria-hidden', String(!open));
  if (deferred) return;
  if (rewardChildOpen && reward?.canChoose) return;
  if (!reward || !reward.canChoose || !rewardSource) {
    ui.rewardOptions.innerHTML = '';
    lastRewardRenderKey = '';
    if (ui.rewardTitle) ui.rewardTitle.textContent = i18n.t('rewardTitle');
    if (ui.rewardSubtitle) ui.rewardSubtitle.textContent = i18n.t('rewardSubtitle');
    if (ui.rewardReset) ui.rewardReset.hidden = true;
    if (ui.rewardInlineBack) ui.rewardInlineBack.hidden = true;
    if (ui.rewardSkip) ui.rewardSkip.hidden = false;
    return;
  }

  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  hideAttackArrow();
  hideCharacterInspector();
  hideShieldInspector();
  closePreview();
  if (ui.rewardTitle) ui.rewardTitle.textContent = i18n.t(isRelicChild ? 'rewardRelicTitle' : 'rewardTitle');
  if (ui.rewardSubtitle) ui.rewardSubtitle.textContent = i18n.t(isRelicChild ? 'rewardRelicSubtitle' : 'rewardSubtitle');
  ui.rewardWaiting.hidden = reward.canChoose;
  const rewardKey = [
    i18n.language,
    isRelicChild ? 'relic' : 'top',
    reward.playerId,
    reward.roundNumber,
    rewardSource.resetCount,
    rewardSource.canChoose,
    ...(rewardSource.options || []).map(option => `${option.instanceId}:${option.rewardId}:${option.cost}:${option.kind}:${option.canAfford}`)
  ].join('|');
  if (rewardKey !== lastRewardRenderKey) {
    lastRewardRenderKey = rewardKey;
    ui.rewardOptions.innerHTML = (rewardSource.options || []).map((option, index) => {
      const localized = i18n.reward(option.rewardId);
      const disabled = !reward.canChoose || !option.canAfford;
      const isCategory = option.kind === 'RelicChoice';
      const costLabel = isCategory ? i18n.t('rewardOpenRelics') : `${option.cost} BP`;
      const isRelic = option.kind !== 'RelicChoice' && String(option.rewardId || '').startsWith('relic-');
      const iconMarkup = isRelic
        ? `<span class="reward-relic-art">${art.icon(relicIconId(option.rewardId), { size: 'lg', label: localized.name })}</span>`
        : '';
      return `<button class="reward-card ${isRelic ? 'relic-reward-card' : ''} ${option.canAfford ? '' : 'unaffordable'}" type="button" data-reward-instance="${escapeHtml(option.instanceId)}" data-index="${index}" ${disabled ? 'disabled' : ''}>
        <small>${escapeHtml(option.rarity || 'COMMON')}</small>
        ${iconMarkup}
        <strong>${escapeHtml(localized.name)}</strong>
        <p>${escapeHtml(localized.description)}</p>
        <b>${escapeHtml(costLabel)}</b>
        ${option.canAfford ? '' : `<em>${escapeHtml(i18n.t('rewardCannotAfford'))}</em>`}
      </button>`;
    }).join('');
  }

  const resetCost = Number(rewardSource.nextResetCost || 0);
  const viewer = game.players.find(player => player.id === game.viewerPlayerId);
  const canAffordReset = Number(viewer?.battlePoints?.current || 0) >= resetCost;
  ui.rewardReset.hidden = !isRelicChild;
  ui.rewardReset.disabled = !reward.canChoose || !canAffordReset;
  ui.rewardReset.innerHTML = `<span>${escapeHtml(i18n.t('rewardReset'))}</span><b>${escapeHtml(resetCost === 0 ? i18n.t('rewardResetFree') : i18n.t('rewardResetCost', { cost: resetCost }))}</b>`;
  if (ui.rewardInlineBack) {
    ui.rewardInlineBack.hidden = !isRelicChild;
    ui.rewardInlineBack.disabled = !isRelicChild || !reward.canChoose;
    ui.rewardInlineBack.innerHTML = `<span>${escapeHtml(i18n.t('rewardBack'))}</span>`;
  }
  ui.rewardSkip.hidden = isRelicChild;
  ui.rewardSkip.disabled = !reward.canChoose;
  ui.rewardSkip.innerHTML = `<span>${escapeHtml(Number(reward.purchaseCount || 0) > 0 ? i18n.t('rewardExit') : i18n.t('rewardSkip', { amount: Number(reward.skipBattlePoints || 0) }))}</span>`;
}

function renderHeroDraftWindow() {
  const draft = game?.heroDraft;
  if (!ui.heroDraftWindow) return;
  const isOpening = draft?.kind === 'Opening' || draft?.kind === 'TestOpening';
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  const deferred = isRewardPresentationDeferred();
  const open = Boolean(draft?.canChoose) && !isOpening && !deferred;
  ui.heroDraftWindow.classList.toggle('open', open);
  ui.heroDraftWindow.setAttribute('aria-hidden', String(!open));
  if (deferred) return;
  if (!draft || isOpening || !draft.canChoose) {
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    ui.heroDraftOptions.innerHTML = '';
    if (ui.heroDraftUpgrade) ui.heroDraftUpgrade.hidden = true;
    if (ui.heroDraftConfirm) ui.heroDraftConfirm.hidden = true;
    if (ui.heroDraftReset) ui.heroDraftReset.hidden = true;
    if (ui.heroDraftBack) ui.heroDraftBack.hidden = true;
    return;
  }

  const candidates = draft.candidates || [];
  selectedHeroDraftKeys = selectedHeroDraftKeys.filter(key => candidates.some(candidate => candidate.key === key));
  if (soldierUpgradeKey && (!candidates.some(candidate => candidate.key === soldierUpgradeKey) || draft.kind !== 'SoldierRecruit'))
    soldierUpgradeKey = null;
  if (isSoldierDraft)
    selectedHeroDraftKey = null;
  if (!candidates.some(candidate => candidate.key === selectedHeroDraftKey))
    selectedHeroDraftKey = null;

  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  pendingRoleAction = null;
  pendingDeputy = null;
  hideAttackArrow();
  if (!selectedHeroDraftKey) hideCharacterInspector();
  hideShieldInspector();
  hideBpInspector();
  closePreview();

  const titleKey = isSoldierDraft
    ? (draft.kind === 'SoldierOpening' ? 'soldierDraftTitleOpening' : 'soldierDraftTitleRecruit')
    : (isOpening ? 'heroDraftTitleOpening' : 'heroDraftTitleRecruit');
  const subtitleKey = isSoldierDraft
    ? (draft.kind === 'SoldierOpening' ? 'soldierDraftSubtitleOpening' : 'soldierDraftSubtitleRecruit')
    : (isOpening ? 'heroDraftSubtitleOpening' : 'heroDraftSubtitleRecruit');
  ui.heroDraftTitle.textContent = i18n.t(titleKey);
  const handFull = viewerActiveCharacterCount() >= 4;
  const upgradeKey = selectedRecruitSoldierUpgradeKey();
  ui.heroDraftSubtitle.textContent = draft.kind === 'SoldierRecruit' && handFull
    ? i18n.t(upgradeKey ? 'soldierDraftSubtitleFullUpgradeReady' : 'soldierDraftSubtitleFull')
    : i18n.t(subtitleKey);
  ui.heroDraftWaiting.textContent = draft.canChoose ? '' : i18n.t('heroDraftWaiting');
  ui.heroDraftWaiting.hidden = draft.canChoose;
  ui.heroDraftOptions.innerHTML = candidates.map(candidate => {
    const selected = isSoldierDraft
      ? selectedHeroDraftKeys.includes(candidate.key)
      : candidate.key === selectedHeroDraftKey;
    const ownedSoldier = isSoldierDraft ? findOwnedSoldier(candidate.key) : null;
    return `<div class="hero-draft-choice ${selected ? 'selected' : ''}" data-hero-key="${escapeHtml(candidate.key)}">
      <button class="hero-draft-card" type="button" data-hero-key="${escapeHtml(candidate.key)}" data-card-type="${escapeHtml(candidate.cardType || '')}" ${draft.canChoose ? '' : 'disabled'}>
        <img src="${escapeHtml(candidate.coloredAssetUrl || candidate.assetUrl)}" alt="${escapeHtml(i18n.characterName(candidate.key))}">
        <strong>${escapeHtml(i18n.characterName(candidate.key))}</strong>
        <em>${escapeHtml(i18n.damageType(candidate.attackType))}</em>
      </button>
      ${ownedSoldier ? `<small class="hero-draft-owned">${escapeHtml(i18n.t(ownedSoldier.soldierRank >= 2 ? 'soldierDraftMaxRank' : 'soldierDraftOwnedHint'))}</small>` : ''}
      ${!isSoldierDraft && selected && draft.canChoose ? `<button class="hero-draft-recruit-confirm" type="button" data-hero-key="${escapeHtml(candidate.key)}">${escapeHtml(i18n.t('heroDraftConfirm'))}</button>` : ''}
    </div>`;
  }).join('');
  const selectedOwnedSoldier = draft.kind === 'SoldierRecruit' && selectedHeroDraftKeys.length === 1
    ? findOwnedSoldier(selectedHeroDraftKeys[0])
    : null;
  if (ui.heroDraftUpgrade) {
    ui.heroDraftUpgrade.hidden = !(draft.canChoose && selectedOwnedSoldier && Number(selectedOwnedSoldier.soldierRank || 0) < 2);
    ui.heroDraftUpgrade.disabled = !draft.canChoose || !selectedOwnedSoldier || Number(selectedOwnedSoldier.soldierRank || 0) >= 2;
    ui.heroDraftUpgrade.textContent = i18n.t('soldierDraftUpgrade');
  }
  if (ui.heroDraftConfirm) {
    ui.heroDraftConfirm.hidden = !isSoldierDraft;
    ui.heroDraftConfirm.disabled = !draft.canChoose
      || selectedHeroDraftKeys.length === 0
      || (draft.kind === 'SoldierRecruit' && handFull);
    ui.heroDraftConfirm.textContent = i18n.t('heroDraftConfirm');
  }
  if (ui.heroDraftReset) {
    const resetCost = Number(draft.nextResetCost || 0);
    const viewer = game.players.find(player => player.id === game.viewerPlayerId);
    const canAffordReset = Number(viewer?.battlePoints?.current || 0) >= resetCost;
    const isSoldierRecruit = draft.kind === 'SoldierRecruit';
    ui.heroDraftReset.hidden = isSoldierDraft && !isSoldierRecruit;
    ui.heroDraftReset.disabled = !draft.canChoose || !canAffordReset;
    ui.heroDraftReset.innerHTML = `<span>${escapeHtml(i18n.t('rewardReset'))}</span><b>${escapeHtml(resetCost === 0 ? i18n.t('rewardResetFree') : i18n.t('rewardResetCost', { cost: resetCost }))}</b>`;
  }
  if (ui.heroDraftBack) {
    const isRewardDraft = draft.kind === 'Recruit' || draft.kind === 'SoldierRecruit';
    ui.heroDraftBack.hidden = !isRewardDraft;
    ui.heroDraftBack.disabled = !draft.canChoose || !game?.rewardWindow;
    ui.heroDraftBack.innerHTML = `<span>${escapeHtml(i18n.t('rewardBack'))}</span>`;
  }
  if (selectedHeroDraftKey && !isSoldierDraft) {
    const selectedCard = [...ui.heroDraftOptions.querySelectorAll('.hero-draft-card[data-hero-key]')]
      .find(card => card.dataset.heroKey === selectedHeroDraftKey);
    if (selectedCard) showHeroDraftCandidateInspector(selectedHeroDraftKey, selectedCard);
  }
}

function renderRewardChildBack() {
  if (!ui.rewardChildBack) return;
  const open = Boolean(game?.rewardWindow && game?.pendingRoleActionUpgrade?.canChoose) && !isRewardPresentationDeferred();
  ui.rewardChildBack.hidden = !open;
  ui.rewardChildBack.classList.toggle('open', open);
  ui.rewardChildBack.disabled = !open;
  ui.rewardChildBack.innerHTML = `<span>${escapeHtml(i18n.t('rewardBack'))}</span>`;
}

function isRewardPresentationDeferred() {
  return rewardVisualHold || eventPlayback || aiAdvancing || endTurnQueued;
}

function syncHeroDraftSelectionUi() {
  const draft = game?.heroDraft;
  if (!draft || !ui.heroDraftOptions) return;
  const isSoldierDraft = draft.kind === 'SoldierOpening' || draft.kind === 'SoldierRecruit';
  const handFull = viewerActiveCharacterCount() >= 4;
  const selectedOwnedSoldier = draft.kind === 'SoldierRecruit' && selectedHeroDraftKeys.length === 1
    ? findOwnedSoldier(selectedHeroDraftKeys[0])
    : null;

  ui.heroDraftOptions.querySelectorAll('.hero-draft-choice[data-hero-key]').forEach(choice => {
    const key = choice.dataset.heroKey;
    choice.querySelector('.hero-draft-card')?.classList.add('hero-draft-entered');
    const selected = isSoldierDraft
      ? selectedHeroDraftKeys.includes(key)
      : key === selectedHeroDraftKey;
    choice.classList.toggle('selected', selected);

    const existingConfirm = choice.querySelector(':scope > .hero-draft-recruit-confirm');
    if (!isSoldierDraft && selected && draft.canChoose && !existingConfirm) {
      const button = document.createElement('button');
      button.className = 'hero-draft-recruit-confirm';
      button.type = 'button';
      button.dataset.heroKey = key;
      button.textContent = i18n.t('heroDraftConfirm');
      choice.appendChild(button);
    } else if ((isSoldierDraft || !selected || !draft.canChoose) && existingConfirm) {
      existingConfirm.remove();
    }
  });

  const subtitleKey = isSoldierDraft
    ? (draft.kind === 'SoldierOpening' ? 'soldierDraftSubtitleOpening' : 'soldierDraftSubtitleRecruit')
    : 'heroDraftSubtitleRecruit';
  const upgradeKey = selectedRecruitSoldierUpgradeKey();
  ui.heroDraftSubtitle.textContent = draft.kind === 'SoldierRecruit' && handFull
    ? i18n.t(upgradeKey ? 'soldierDraftSubtitleFullUpgradeReady' : 'soldierDraftSubtitleFull')
    : i18n.t(subtitleKey);

  if (ui.heroDraftUpgrade) {
    ui.heroDraftUpgrade.hidden = !(draft.canChoose && selectedOwnedSoldier && Number(selectedOwnedSoldier.soldierRank || 0) < 2);
    ui.heroDraftUpgrade.disabled = !draft.canChoose || !selectedOwnedSoldier || Number(selectedOwnedSoldier.soldierRank || 0) >= 2;
    ui.heroDraftUpgrade.textContent = i18n.t('soldierDraftUpgrade');
  }
  if (ui.heroDraftConfirm) {
    ui.heroDraftConfirm.hidden = !isSoldierDraft;
    ui.heroDraftConfirm.disabled = !draft.canChoose
      || selectedHeroDraftKeys.length === 0
      || (draft.kind === 'SoldierRecruit' && handFull);
    ui.heroDraftConfirm.textContent = i18n.t('heroDraftConfirm');
  }

  if (selectedHeroDraftKey && !isSoldierDraft) {
    const selectedCard = [...ui.heroDraftOptions.querySelectorAll('.hero-draft-card[data-hero-key]')]
      .find(card => card.dataset.heroKey === selectedHeroDraftKey);
    if (selectedCard) showHeroDraftCandidateInspector(selectedHeroDraftKey, selectedCard);
  } else if (isTouchMode() && isSoldierDraft && ui.inspector.classList.contains('draft-inspector-only') && ui.inspector.dataset.draftKey) {
    return;
  } else {
    hideCharacterInspector();
  }
}

function renderHeroDraftBanner() {
  if (!ui.heroDraftBanner) return;
  const draft = game?.heroDraft;
  const open = (draft?.kind === 'Opening' || draft?.kind === 'TestOpening') && !dealing;
  ui.heroDraftBanner.classList.toggle('open', Boolean(open));
  ui.heroDraftBanner.setAttribute('aria-hidden', String(!open));
  if (!open) {
    ui.heroDraftBanner.innerHTML = '';
    return;
  }
  const instructionKey = draft.kind === 'TestOpening' ? 'heroDraftInstructionTest' : 'heroDraftInstruction';
  const hintKey = draft.kind === 'TestOpening' ? 'heroDraftOpeningHintTest' : 'heroDraftOpeningHint';
  ui.heroDraftBanner.innerHTML = `<strong>${escapeHtml(i18n.t(draft.canChoose ? instructionKey : 'heroDraftWaiting'))}</strong><small>${escapeHtml(draft.canChoose ? i18n.t(hintKey) : i18n.t('heroDraftWaiting'))}</small>`;
}

function showHeroDraftCandidateInspector(characterKey, element) {
  if (!ui.inspector || !game?.heroDraft || !element) return;
  const candidate = (game.heroDraft.candidates || []).find(item => item.key === characterKey);
  if (!candidate) {
    hideCharacterInspector();
    return;
  }
  const trait = i18n.trait(candidate.traitId);
  hideShieldInspector();
  hideRoleActionInspector();
  ui.statusInspector.classList.remove('open');
  ui.statusInspector.setAttribute('aria-hidden', 'true');
  ui.inspector.innerHTML = `<header>
      <span>${escapeHtml(i18n.t('unitDossier'))}</span>
      <strong>${escapeHtml(i18n.characterName(candidate.key))}</strong>
    </header>
    <div class="inspector-stats">
      <div class="stat-card stat-attack"><span>ATK</span><b>${candidate.attack}</b></div>
      <div class="stat-card stat-hp"><span>HP</span><b>${candidate.maxHp}/${candidate.maxHp}</b></div>
      <div class="stat-card stat-cost"><span>COST</span><b>${candidate.cost}</b></div>
      <div class="stat-card stat-type"><span>TYPE</span><b class="damage-type ${candidate.attackType === 'Magical' ? 'magic' : ''}">${escapeHtml(i18n.damageType(candidate.attackType))}</b></div>
      <div class="stat-card stat-pdef"><span>P.DEF</span>${defenseMarkup(candidate.physicalDefense, 'b')}</div>
      <div class="stat-card stat-mdef"><span>M.DEF</span>${defenseMarkup(candidate.magicalDefense, 'b')}</div>
    </div>
    <section class="inspector-trait ready">
      <div class="inspector-trait-heading">${art.icon(art.forTrait(candidate.traitId), { size: 'md', label: trait.name })}<div><span>TRAIT</span><b>${escapeHtml(trait.name)}</b><p>${escapeHtml(trait.description)}</p></div></div>
    </section>`;
  ui.inspector.classList.add('open', 'draft-inspector-only');
  ui.inspector.setAttribute('aria-hidden', 'false');
  ui.inspector.dataset.draftKey = characterKey;
  art.hydrate(ui.inspector);
  const rect = stageRect(element);
  positionInspector(ui.inspector, Math.max(16, rect.left - ui.inspector.offsetWidth - 14), rect);
}

function renderShieldBadge(element, value) {
  element.innerHTML = value > 0 ? `${art.icon('status.team-shield', { size: 'xs', label: 'Shared shield' })}<span>SHARED SHIELD</span><b>${value}</b>` : '';
  element.classList.toggle('active', value > 0);
}

function renderBattlePoints(me) {
  if (!ui.bpHud) return;
  const format = bp => bp ? `${bp.current}/${bp.max}` : '0/0';
  const gainedThisTurn = me?.isActive ? Math.max(0, Number(me?.battlePoints?.gainedThisTurn || 0)) : 0;
  const gainContext = `${game?.gameId || ''}:${game?.turnNumber || 0}:${game?.activePlayerId || ''}:${me?.id || ''}`;
  ui.activeBpValue.textContent = format(me?.battlePoints);
  if (ui.activeBpTurnGain) {
    ui.activeBpTurnGain.textContent = gainedThisTurn > 0 ? `+${gainedThisTurn}` : '';
    ui.activeBpTurnGain.classList.toggle('active', gainedThisTurn > 0);
    const previous = lastBpGainSnapshot?.context === gainContext ? Number(lastBpGainSnapshot.gained || 0) : gainedThisTurn;
    if (gainedThisTurn > previous) {
      ui.activeBpTurnGain.classList.remove('gain-pop');
      void ui.activeBpTurnGain.offsetWidth;
      ui.activeBpTurnGain.classList.add('gain-pop');
      ui.activeBpTurnGain.addEventListener('animationend', () => ui.activeBpTurnGain.classList.remove('gain-pop'), { once: true });
    }
    lastBpGainSnapshot = { context: gainContext, gained: gainedThisTurn };
  }
  ui.activeBpValue.parentElement?.setAttribute('title', me?.battlePoints?.lastReasonId ? i18n.bpReason(me.battlePoints.lastReasonId) : '');
}

function clearBpTurnGainDisplay() {
  if (!ui.activeBpTurnGain) return;
  ui.activeBpTurnGain.classList.remove('active', 'gain-pop');
  ui.activeBpTurnGain.textContent = '';
}

function relicIconId(relicId) {
  if (String(relicId || '').startsWith('relic-')) {
    return `relic.${String(relicId).slice('relic-'.length)}`;
  }
  return {
    'dummy-reward-a': 'status.spell-ward',
    'dummy-reward-b': 'status.chant',
    'dummy-reward-c': 'status.strong-attack',
  }[relicId] || 'event.trait';
}

function renderRelicOverview(player) {
  if (!ui.relicOverview || !ui.relicOverviewButton || !ui.relicOverviewDetail) return;
  const relics = Array.isArray(player?.relics) ? player.relics : [];
  const hasRelics = relics.length > 0;
  ui.relicOverview.hidden = !hasRelics;
  ui.relicOverview.setAttribute('aria-hidden', String(!hasRelics));
  if (!hasRelics) {
    setRelicOverviewOpen(false);
    ui.relicOverviewButton.innerHTML = '';
    ui.relicOverviewDetail.innerHTML = '';
    return;
  }

  ui.relicOverviewButton.innerHTML = `${art.icon('event.trait', { size: 'lg', label: i18n.t('relicOverview') })}<b>${escapeHtml(relics.length)}</b>`;
  ui.relicOverviewButton.setAttribute('aria-label', i18n.t('relicOverview'));
  ui.relicOverviewDetail.innerHTML = `<header><span>${escapeHtml(i18n.t('relicOverview'))}</span><strong>${escapeHtml(i18n.t('relics'))}</strong><b>${escapeHtml(relics.length)}</b></header>
    <ul>${relics.map(relic => {
      const localized = i18n.reward(relic.id);
      return `<li>
        <div class="effect-title">${art.icon(relicIconId(relic.id), { size: 'md', label: localized.name })}<div><b>${escapeHtml(localized.name)}</b><em>${escapeHtml(i18n.t('always'))}</em></div></div>
        <p>${escapeHtml(localized.description)}</p>
      </li>`;
    }).join('')}</ul>`;
  art.hydrate(ui.relicOverview);
}

function setRelicOverviewOpen(open) {
  if (!ui.relicOverview || !ui.relicOverviewButton || !ui.relicOverviewDetail) return;
  ui.relicOverview.classList.toggle('expanded', open);
  ui.relicOverviewButton.setAttribute('aria-expanded', String(open));
  ui.relicOverviewDetail.setAttribute('aria-hidden', String(!open));
}

function hasPendingShieldBreakForPlayer(state, player) {
  if (!state || !player || Number(player.sharedShield || 0) > 0) return false;
  const characterKeys = new Set((player.characters || []).map(character => character.key));
  return (state.log || []).some(entry =>
    entry.sequence > lastAnimatedLogSequence
    && entry.message?.key === 'note.shieldAbsorb'
    && characterKeys.has(String(logArg(entry, 'character') || '')));
}

const shieldLayoutCache = new WeakMap();

function stableShieldLayout(dome, cards, force = false) {
  const signature = `${stageScale.toFixed(6)}:${dealing ? 'dealing' : 'ready'}:${cards.map(card => card.dataset.id || '').join('|')}`;
  const cached = shieldLayoutCache.get(dome);
  if (!force && cached?.signature === signature) return cached;
  const firstRect = stageRect(cards[0]);
  const lastRect = stageRect(cards[cards.length - 1]);
  const layout = {
    signature,
    first: { left: firstRect.left, top: firstRect.top, bottom: firstRect.bottom },
    last: { right: lastRect.right }
  };
  shieldLayoutCache.set(dome, layout);
  return layout;
}

function renderPersistentShield(dome, row, playerOrShieldValue, preserveUntilBreak = false, forceLayout = false) {
  const player = typeof playerOrShieldValue === 'object' && playerOrShieldValue !== null ? playerOrShieldValue : null;
  const value = player ? Number(player.sharedShield || 0) : Number(playerOrShieldValue || 0);
  const cards = [...row.querySelectorAll('.fighter-card')];
  const layout = cards.length > 0 ? stableShieldLayout(dome, cards, forceLayout) : null;
  if (!layout) shieldLayoutCache.delete(dome);
  const isOpponentShield = dome === ui.opponentShieldDome;
  dome.classList.toggle('opponent-facing', isOpponentShield);
  dome.dataset.shield = String(value);
  dome.dataset.pdef = String(player?.sharedShieldPhysicalDefense || 0);
  dome.dataset.mdef = String(player?.sharedShieldMagicalDefense || 0);
  if (value <= 0 || cards.length === 0) {
    if (preserveUntilBreak && cards.length > 0 && dome.classList.contains('active')) {
      dome.dataset.pendingBreak = 'true';
      dome.classList.add('pending-break');
      renderPersistentShield(dome, row, Number(dome.dataset.visualShieldValue || 1), false, forceLayout);
      return;
    }
    dome.classList.remove('active', 'forming', 'breaking', 'pending-break');
    delete dome.dataset.pendingBreak;
    delete dome.dataset.visualShieldValue;
    dome.setAttribute('aria-hidden', 'true');
    return;
  }
  dome.dataset.visualShieldValue = String(value);
  dome.setAttribute('aria-hidden', 'false');
  const { first, last } = layout;
  const height = value > 2 ? 46 : 38;
  const edgeOffset = value > 2 ? 18 : 13;
  const centerOffset = cssPixelVar('--team-shield-center-offset', 0);
  dome.style.left = `${first.left - 8}px`;
  dome.style.top = `${isOpponentShield ? first.bottom - (height - edgeOffset) + centerOffset : first.top - edgeOffset - centerOffset}px`;
  dome.style.width = `${last.right - first.left + 16}px`;
  dome.style.height = `${height}px`;
  dome.classList.add('active');
  dome.classList.toggle('reinforced', value > 2);
}

function visibleBattleCharacters(player, pendingDefeatIds = new Set()) {
  return (player?.characters || []).filter(character =>
    character.isInBattle
    || character.zone === 'DraftCandidate'
    || pendingDefeatIds.has(character.id));
}

function renderCardRow(characters, isActiveSide, options = {}) {
  return characters.map((character, index) => cardMarkup(character, isActiveSide, index, characters.length, options)).join('');
}

function renderBattleCardRow(container, characters, isActiveSide) {
  if (!container) return;
  const canReuseCards = !(game?.heroDraft?.kind === 'Opening' || game?.heroDraft?.kind === 'TestOpening');
  if (!canReuseCards) {
    container.innerHTML = renderCardRow(characters, isActiveSide);
    return;
  }

  const existingById = new Map(
    [...container.querySelectorAll(':scope > .fighter-card[data-id]')]
      .map(element => [String(element.dataset.id), element])
  );
  const nextElements = characters.map((character, index) => {
    const markup = cardMarkup(character, isActiveSide, index, characters.length, { includeInteractionState: false });
    const existing = existingById.get(String(character.id));
    const template = document.createElement('template');
    template.innerHTML = markup.trim();
    const element = template.content.firstElementChild;
    if (existing) {
      if (cardRenderSignatures.get(existing) !== markup) {
        morphElement(existing, element);
        cardRenderSignatures.set(existing, markup);
      }
      return existing;
    }
    cardRenderSignatures.set(element, markup);
    return element;
  });
  const nextSet = new Set(nextElements);
  nextElements.forEach((element, index) => {
    const current = container.children[index];
    if (current === element) return;
    container.insertBefore(element, current || null);
  });
  [...container.children].forEach(child => {
    if (!nextSet.has(child)) child.remove();
  });
}

function effectiveCardAttack(card) {
  const attack = Number(card?.attack || 0);
  const baseAttack = Number(card?.baseAttack || 0);
  const auraBonus = Number(card?.attackAuraBonus || 0);
  return Math.max(attack, baseAttack + auraBonus);
}

function morphElement(target, source) {
  if (!(target instanceof Element) || !(source instanceof Element) || target.tagName !== source.tagName) {
    target.replaceWith(source);
    return source;
  }
  syncElementAttributes(target, source);
  const targetChildren = [...target.childNodes];
  const sourceChildren = [...source.childNodes];
  const max = Math.max(targetChildren.length, sourceChildren.length);
  for (let index = 0; index < max; index++) {
    const current = targetChildren[index];
    const next = sourceChildren[index];
    if (!next) {
      current?.remove();
      continue;
    }
    if (!current) {
      target.appendChild(next.cloneNode(true));
      continue;
    }
    if (current.nodeType === Node.TEXT_NODE && next.nodeType === Node.TEXT_NODE) {
      if (current.nodeValue !== next.nodeValue) current.nodeValue = next.nodeValue;
      continue;
    }
    if (current.nodeType !== next.nodeType) {
      current.replaceWith(next.cloneNode(true));
      continue;
    }
    if (current instanceof Element && next instanceof Element && current.tagName === next.tagName) {
      morphElement(current, next);
      continue;
    }
    current.replaceWith(next.cloneNode(true));
  }
  return target;
}

function syncElementAttributes(target, source) {
  const preserved = new Set(['data-card-bound']);
  [...target.attributes].forEach(attribute => {
    if (preserved.has(attribute.name)) return;
    if (!source.hasAttribute(attribute.name)) target.removeAttribute(attribute.name);
  });
  [...source.attributes].forEach(attribute => {
    if (target.getAttribute(attribute.name) !== attribute.value)
      target.setAttribute(attribute.name, attribute.value);
  });
}

function defenseMarkup(value, tagName = 'strong') {
  const numericValue = Number(value || 0);
  const displayValue = Math.abs(numericValue);
  const className = numericValue < 0
    ? ' class="defense-negative"'
    : numericValue > 0
      ? ' class="defense-positive"'
      : '';
  return `<${tagName}${className}>${displayValue}</${tagName}>`;
}

function statDeltaDisplay(current, base, { absoluteCurrent = false } = {}) {
  const currentValue = Number(current || 0);
  const baseValue = Number(base || 0);
  const delta = currentValue - baseValue;
  const currentDisplay = absoluteCurrent ? Math.abs(currentValue) : currentValue;
  return delta === 0 ? `${currentDisplay}` : `${currentDisplay} (${delta > 0 ? '+' : ''}${delta})`;
}

function defenseDeltaMarkup(current, base) {
  const currentValue = Number(current || 0);
  const className = currentValue < 0 ? 'defense-negative' : currentValue > 0 ? 'defense-positive' : '';
  return `<b${className ? ` class="${className}"` : ''}>${escapeHtml(statDeltaDisplay(current, base, { absoluteCurrent: true }))}</b>`;
}

function damageTypeGlyph(type) {
  const localized = String(i18n.damageType(type) || type || '').trim();
  return localized ? localized[0] : '';
}

const AURA_DISPLAY_STATUS_IDS = new Set(['blessing', 'foresight', 'guard', 'magic-power']);
const AURA_DISPLAY_ORDER = ['guard', 'blessing', 'foresight', 'magic-power'];
function isAuraDisplayStatus(status) {
  return Boolean(status?.isAura) || AURA_DISPLAY_STATUS_IDS.has(status?.id);
}
function sortAuraDisplayStatuses(statuses) {
  return [...statuses].sort((a, b) => {
    const left = AURA_DISPLAY_ORDER.indexOf(a?.id);
    const right = AURA_DISPLAY_ORDER.indexOf(b?.id);
    const leftOrder = left >= 0 ? left : AURA_DISPLAY_ORDER.length;
    const rightOrder = right >= 0 ? right : AURA_DISPLAY_ORDER.length;
    return leftOrder - rightOrder || String(a?.name || '').localeCompare(String(b?.name || ''));
  });
}

function primaryTrait(card) {
  return Array.isArray(card?.traits) && card.traits.length > 0 ? card.traits[0] : null;
}

function cardMarkup(card, isActiveSide, index = 0, count = 1, options = {}) {
  const includeInteractionState = options.includeInteractionState !== false;
  const classes = ['fighter-card'];
  const visualBaseline = hasUnplayedLogEvents(game) ? pendingVisualBaselines?.get(String(card.id)) : null;
  const visualCurrentHp = Number(visualBaseline?.currentHp ?? card.currentHp);
  const visualMorale = Number(visualBaseline?.morale ?? card.morale);
  const visualIsAlive = Boolean(visualBaseline?.isAlive ?? card.isAlive);
  const visualIsInBattle = Boolean(visualBaseline?.isInBattle ?? card.isInBattle);
  const visualIsDraftCandidate = card.zone === 'DraftCandidate';
  const isPendingDefeatAnimation = !card.isAlive
    && (game?.log || []).some(entry =>
      entry.sequence > lastAnimatedLogSequence
      && entry.message?.key === 'log.defeated'
      && String(logArg(entry, 'characterId') || '') === String(card.id));
  const visuallyAlive = visualIsAlive || isPendingDefeatAnimation;
  const isDeploying = (card.statuses || []).some(status => status.id === 'deploying');
  const localControls = canUseLocalControls();
  const hasEnabledRoleAction = isActiveSide && localControls
    && Array.isArray(card.roleActions) && card.roleActions.some(action => action.enabled);
  const hasAvailableAction = Boolean(card.canAct || hasEnabledRoleAction);
  const canConsiderAttackCost = isActiveSide && localControls && visuallyAlive && !card.hasActed;
  if (canConsiderAttackCost && card.cost <= game.actionPoints) classes.push('affordable-cost');
  if (canConsiderAttackCost && card.cost > game.actionPoints) classes.push('unaffordable-cost');
  if (isActiveSide && localControls && visuallyAlive && card.hasActed) classes.push('attack-spent');
  if (localControls && card.canAct && !isPendingDefeatAnimation) classes.push('can-act');
  if (isActiveSide && Array.isArray(card.roleActions) && card.roleActions.length && visuallyAlive)
    classes.push('role-action-owner');
  if (card.deputy) classes.push('has-deputy');
  if (game?.pendingRoleActionUpgrade?.canChoose && isActiveSide && card.canHeroRankUpgrade)
    classes.push('upgrade-selectable');
  if (pendingRoleAction && isActiveSide && visuallyAlive && !isDeploying)
    classes.push('role-action-target');
  if (pendingDeputy && isActiveSide && visuallyAlive && !isDeploying && card.cardType === 'Hero' && !card.deputy)
    classes.push('deputy-target', 'role-action-target');
  if (isLowHpForSelect({ ...card, currentHp: visualCurrentHp })) classes.push('low-hp');
  if (isActiveSide && localControls && visuallyAlive && !hasAvailableAction) classes.push('acted');
  if (isDeploying) classes.push('deploying');
  const soldierUpgradePreviewKey = selectedRecruitSoldierUpgradeKey();
  if ((soldierUpgradeKey || soldierUpgradePreviewKey) && isActiveSide && visuallyAlive && card.cardType === 'Soldier' && card.key === (soldierUpgradeKey || soldierUpgradePreviewKey) && Number(card.soldierRank || 0) < 2) {
    classes.push('soldier-upgrade-candidate');
    if (soldierUpgradeKey) classes.push('soldier-upgrade-target', 'role-action-target');
  }
  if (!visuallyAlive) classes.push('defeated');
  if (visualIsDraftCandidate) classes.push('draft-candidate');
  if (includeInteractionState && visualIsDraftCandidate && inspectedCardId === card.id) classes.push('draft-inspected', 'selected');
  if (!visualIsInBattle && !visualIsDraftCandidate && !isPendingDefeatAnimation) classes.push('leaving-battle');
  if (isPendingDefeatAnimation) classes.push('pending-defeat');
  classes.push('full-art-card');
  if (dealing) classes.push('deal-hidden');
  if (includeInteractionState && card.id === selectedAttacker) classes.push('selected');
  if (includeInteractionState && card.id === selectedDefender) classes.push('target-selected');
  const translatedStatuses = card.statuses.map(status => i18n.status(status));
  const visibleStatuses = translatedStatuses.filter(status => !isAuraDisplayStatus(status));
  const statuses = visibleStatuses.map(status => `<span class="status-chip ${status.isBuff ? '' : 'debuff'}" title="${escapeHtml(status.description)}">${art.icon(art.forStatus(status.id), { size: 'xs', label: status.name })}<span>${escapeHtml(status.name)}</span></span>`).join('');
  const trait = primaryTrait(card);
  const localizedTrait = i18n.trait(trait?.id, card.cardType === 'Hero' ? card.heroRank : card.soldierRank, card.heroPathRoleActionId);
  localizedTrait.kind = i18n.traitTrigger(trait?.triggerKind);
  const traitClass = trait?.isReady !== false ? 'ready' : 'disabled';
  const over = visualCurrentHp > card.maxHp ? 'hp-over' : '';
  const hpRatio = card.maxHp > 0 ? Math.max(0, Math.min(1, visualCurrentHp / card.maxHp)) : 0;
  const hpEmpty = visualCurrentHp <= 0 ? ' hp-empty' : '';
  const morale = Math.max(0, visualMorale);
  const maxMorale = Math.max(0, Number(card.maxMorale ?? 0));
  const moraleRatio = maxMorale > 0 ? Math.max(0, Math.min(1, morale / maxMorale)) : 0;
  const moraleTone = moraleRatio <= .25 ? 'low' : moraleRatio >= .72 ? 'high' : 'mid';
  const displayedAttack = effectiveCardAttack(card);
  const cardDescription = truncateCardText(localizedTrait.card, 28);
  const portraitUrl = card.coloredAssetUrl || card.assetUrl;
  const portraitMarkup = `<img class="portrait" src="${portraitUrl}" alt="${escapeHtml(i18n.characterName(card.key))}">`;
  const costCrystals = Array.from({ length: Math.max(0, card.cost) }, () => '<i class="cost-crystal" aria-hidden="true"></i>').join('');
  const deputyBadge = card.deputy
    ? `<span class="deputy-badge" title="${escapeHtml(i18n.deputy(card.deputy.effectId).name)}">${escapeHtml(i18n.t('deputyShort'))}</span>`
    : '';
  const deputyStack = card.deputy
    ? `<div class="deputy-stack-card" aria-hidden="true">
        <img src="${escapeHtml(card.deputy.coloredAssetUrl || card.deputy.assetUrl)}" alt="">
        <span>${escapeHtml(i18n.characterName(card.deputy.soldierKey))}</span>
      </div>`
    : '';
  const draftConfirm = includeInteractionState && (game?.heroDraft?.kind === 'Opening' || game?.heroDraft?.kind === 'TestOpening') && game.heroDraft.canChoose
    && isActiveSide && visualIsDraftCandidate && inspectedCardId === card.id
    ? `<button class="hero-draft-confirm-card" type="button" data-hero-key="${escapeHtml(card.key)}">${escapeHtml(i18n.t('heroDraftConfirm'))}</button>`
    : '';
  return `<article class="${classes.join(' ')}" style="${cardPoseStyle(isActiveSide, index, count)}--morale-ratio:${moraleRatio.toFixed(3)};" data-id="${card.id}" data-key="${card.key}" data-card-type="${escapeHtml(card.cardType || '')}" data-soldier-rank="${Number(card.soldierRank || 0)}" data-hero-rank="${Number(card.heroRank || 0)}" data-hero-path-role-action-id="${escapeHtml(card.heroPathRoleActionId || '')}" data-current-hp="${visualCurrentHp}" data-max-hp="${Number(card.maxHp || 0)}" data-morale="${morale}" data-max-morale="${maxMorale}" data-side="${isActiveSide ? 'active' : 'opponent'}" data-zone="${escapeHtml(card.zone || (card.isInBattle ? 'Battlefield' : 'Defeated'))}" draggable="${localControls && card.canAct}">
    ${deputyStack}
    <div class="card-front">
      ${draftConfirm}
      ${deputyBadge}
      <div class="card-name">${escapeHtml(i18n.characterName(card.key))}</div><div class="cost-orb" aria-label="Cost ${card.cost}">${costCrystals}</div>
      <div class="type-rune ${card.attackType === 'Magical' ? 'magic' : ''}">${escapeHtml(i18n.damageType(card.attackType))}</div>
      <div class="portrait-wrap">${portraitMarkup}</div>
      <div class="status-stack">${statuses}</div>
      <div class="trait-panel ${traitClass}" title="${escapeHtml(i18n.message(trait?.unavailableReason))}">
        <div class="trait-title"><span>${escapeHtml(localizedTrait.name)}</span><b class="trait-kind">${escapeHtml(localizedTrait.kind)}</b></div>
        <p class="trait-description">${escapeHtml(cardDescription)}</p>
      </div>
       <div class="stat-orb attack"><span>ATK</span><strong>${displayedAttack}</strong><em class="attack-type-label">${escapeHtml(damageTypeGlyph(card.attackType))}</em></div>
      <div class="stat-orb defense"><span>物防</span>${defenseMarkup(card.physicalDefense)}<span>魔防</span>${defenseMarkup(card.magicalDefense)}</div>
      <i class="morale-ring morale-${moraleTone}" aria-hidden="true" title="Morale ${morale}/${maxMorale}"></i>
      <div class="stat-orb hp${hpEmpty}" style="--hp-ratio:${hpRatio.toFixed(3)}"><span>HP</span><strong class="${over}">${visualCurrentHp}<small>/${card.maxHp}</small></strong></div>
    </div>
  </article>`;
}

function cardPoseStyle(isActiveSide, index, count) {
  if (!isActiveSide) return '--pose-y:0px;--pose-rot:0deg;';
  const center = (Math.max(1, count) - 1) / 2;
  const offset = index - center;
  const normalized = center === 0 ? 0 : offset / center;
  const rotation = normalized * 8;
  const ySlot = Math.abs(normalized) > 0.66 ? 'var(--active-hand-edge-y)' : 'var(--active-hand-center-y)';
  const y = `calc(var(--active-hand-y) + ${ySlot})`;
  return `--pose-y:${y};--pose-rot:${rotation.toFixed(2)}deg;`;
}

function bindCards() {
  document.querySelectorAll('.fighter-card').forEach(card => {
    if (card.dataset.cardBound === 'true') return;
    card.dataset.cardBound = 'true';
    card.querySelector('.hero-draft-confirm-card')?.addEventListener('click', event => {
      event.stopPropagation();
      selectHeroDraft(card.dataset.key, { animateOpening: true });
    });
    card.addEventListener('click', event => {
      if (shouldSuppressTouchClick()) {
        event.preventDefault();
        event.stopPropagation();
        return;
      }
      onCardClick(card);
    });
    card.addEventListener('pointerdown', event => beginTouchAttack(card, event));
    card.addEventListener('pointermove', updateTouchDrag);
    card.addEventListener('pointerup', event => endTouchDrag(event));
    card.addEventListener('pointercancel', event => endTouchDrag(event, true));
    card.addEventListener('dragstart', event => {
      if (!card.classList.contains('can-act')) { event.preventDefault(); return; }
      const wasAlreadySelected = selectedAttacker === card.dataset.id;
      selectedAttacker = card.dataset.id; selectedDefender = null; inspectedCardId = card.dataset.id; closePreview();
      if (!wasAlreadySelected) {
        sound.emit('ui.card-select');
      }
      document.querySelectorAll('.fighter-card.selected').forEach(element => element.classList.remove('selected'));
      card.classList.add('selected');
      hideCharacterInspector();
      event.dataTransfer.setData('text/plain', selectedAttacker);
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setDragImage(dragGhost, 0, 0);
      startAttackArrow(card);
      ui.app.classList.add('dragging-attack');
      document.body.classList.add('dragging-attack');
      updateInstruction();
    });
    card.addEventListener('dragend', () => {
      document.querySelectorAll('.drop-ready').forEach(element => element.classList.remove('drop-ready'));
      ui.app.classList.remove('dragging-attack');
      document.body.classList.remove('dragging-attack');
      finishAttackArrow();
    });
    card.addEventListener('mouseenter', () => {
      if (card.dataset.side === 'opponent' && !card.classList.contains('defeated') && !card.classList.contains('deploying'))
        showOpponentHoverInspector(card);
    });
    card.addEventListener('mouseleave', () => {
      if (card.dataset.side === 'opponent') hideOpponentHoverInspector(card);
    });
    card.addEventListener('dragover', event => {
      if (roleActionDragActive
        || card.dataset.side !== 'opponent'
        || card.classList.contains('defeated')
        || card.classList.contains('deploying')) return;
      if (selectedAttacker) {
        event.preventDefault();
        card.classList.add('drop-ready');
        const point = clientToStage(event.clientX, event.clientY);
        updateAttackArrow(point.x, point.y, card);
      }
    });
    card.addEventListener('dragleave', () => {
      if (card.dataset.side === 'opponent') card.classList.remove('drop-ready');
    });
    card.addEventListener('drop', event => {
      if (roleActionDragActive
        || card.dataset.side !== 'opponent'
        || card.classList.contains('defeated')
        || card.classList.contains('deploying')) return;
      event.preventDefault();
      card.classList.remove('drop-ready');
      finishAttackArrow(card);
      chooseDefender(card.dataset.id);
    });
    card.addEventListener('dragover', event => {
      if (!roleActionDragActive || !pendingRoleAction || !canRoleActionTargetCard(pendingRoleAction, card)) return;
      event.preventDefault();
      card.classList.add('drop-ready');
      const point = clientToStage(event.clientX, event.clientY);
      updateAttackArrow(point.x, point.y, card);
    });
    card.addEventListener('dragleave', () => {
      if (roleActionDragActive) card.classList.remove('drop-ready');
    });
    card.addEventListener('drop', event => {
      if (!roleActionDragActive || !pendingRoleAction || !canRoleActionTargetCard(pendingRoleAction, card)) return;
      event.preventDefault();
      card.classList.remove('drop-ready');
      const action = pendingRoleAction;
      finishAttackArrow(card);
      useRoleAction(action.characterId, action.roleActionId, card.dataset.id);
    });
  });
}

function startAttackArrow(sourceElement, mode = 'attack') {
  const rect = stageRect(sourceElement);
  const isActiveCard = sourceElement.classList.contains('fighter-card') && sourceElement.dataset.side === 'active';
  const lift = isActiveCard ? cssPixelVar('--active-selected-lift', 0) : 0;
  const alreadyLifted = sourceElement.classList.contains('selected') && Math.abs(cssTranslateY(sourceElement)) > Math.max(12, lift * .5);
  dragArrowOrigin = { x: rect.left + rect.width / 2, y: rect.top + rect.height * 0.5 - (alreadyLifted ? 0 : lift) };
  dragArrowSourceElement?.classList.remove('drag-source', 'drag-cancel-pulse');
  dragArrowSourceElement = sourceElement;
  dragArrowSourceElement.classList.add('drag-source');
  ui.attackArrow.classList.toggle('role-action', mode === 'role-action');
  ui.attackArrow.classList.remove('release-pop');
  ui.attackArrow.classList.add('active');
  updateAttackArrow(dragArrowOrigin.x, dragArrowOrigin.y - 72);
}

function cssPixelVar(name, fallback = 0) {
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  const number = Number.parseFloat(value);
  return Number.isFinite(number) ? number : fallback;
}

function cssTranslateY(element) {
  const transform = getComputedStyle(element).transform;
  if (!transform || transform === 'none') return 0;
  const match = transform.match(/^matrix\(([^)]+)\)$/);
  if (match) {
    const parts = match[1].split(',').map(part => Number.parseFloat(part.trim()));
    return Number.isFinite(parts[5]) ? parts[5] : 0;
  }
  const match3d = transform.match(/^matrix3d\(([^)]+)\)$/);
  if (match3d) {
    const parts = match3d[1].split(',').map(part => Number.parseFloat(part.trim()));
    return Number.isFinite(parts[13]) ? parts[13] : 0;
  }
  return 0;
}

function updateAttackArrow(pointerX, pointerY, snapTarget = null) {
  if (!dragArrowOrigin) return;
  let endX = pointerX, endY = pointerY;
  if (snapTarget) {
    const rect = stageRect(snapTarget);
    endX = rect.left + rect.width / 2;
    endY = rect.top + rect.height / 2;
  }
  const dx = endX - dragArrowOrigin.x;
  const dy = endY - dragArrowOrigin.y;
  const distance = Math.hypot(dx, dy);
  const arcLift = Math.min(210, Math.max(72, distance * 0.2));
  const controlX = dragArrowOrigin.x + dx * 0.5;
  const controlY = dragArrowOrigin.y + dy * 0.5 - arcLift;
  const path = `M ${dragArrowOrigin.x.toFixed(1)} ${dragArrowOrigin.y.toFixed(1)} Q ${controlX.toFixed(1)} ${controlY.toFixed(1)}, ${endX.toFixed(1)} ${endY.toFixed(1)}`;
  const glow = ui.attackArrow.querySelector('.attack-arc-glow');
  const core = ui.attackArrow.querySelector('.attack-arc-core');
  const origin = ui.attackArrow.querySelector('.attack-arc-origin');
  const head = ui.attackArrow.querySelector('.attack-arc-head');
  const gradient = ui.attackArrow.querySelector('#attack-arc-gradient');
  glow.setAttribute('d', path);
  core.setAttribute('d', path);
  origin.setAttribute('cx', dragArrowOrigin.x);
  origin.setAttribute('cy', dragArrowOrigin.y);
  head.setAttribute('transform', `translate(${endX.toFixed(1)} ${endY.toFixed(1)})`);
  gradient.setAttribute('x1', dragArrowOrigin.x);
  gradient.setAttribute('y1', dragArrowOrigin.y);
  gradient.setAttribute('x2', endX);
  gradient.setAttribute('y2', endY);
  ui.attackArrow.classList.toggle('locked', Boolean(snapTarget));
}

function hideAttackArrow() {
  dragArrowOrigin = null;
  roleActionDragActive = false;
  dragArrowSourceElement?.classList.remove('drag-source');
  dragArrowSourceElement = null;
  ui.app.classList.remove('dragging-attack');
  document.body.classList.remove('dragging-attack');
  ui.attackArrow.classList.remove('active', 'locked', 'role-action');
}

function finishAttackArrow(targetElement = null) {
  if (targetElement) {
    sound.emit('combat.target-lock');
    spawnDragReleaseBurst(targetElement, ui.attackArrow.classList.contains('role-action'));
    targetElement.classList.remove('target-release-pulse');
    void targetElement.offsetWidth;
    targetElement.classList.add('target-release-pulse');
    setTimeout(() => targetElement.classList.remove('target-release-pulse'), 520);
    ui.attackArrow.classList.add('release-pop');
    setTimeout(() => ui.attackArrow.classList.remove('release-pop'), 180);
    dragArrowOrigin = null;
    roleActionDragActive = false;
    dragArrowSourceElement?.classList.remove('drag-source');
    dragArrowSourceElement = null;
    ui.app.classList.remove('dragging-attack');
    document.body.classList.remove('dragging-attack');
    setTimeout(() => ui.attackArrow.classList.remove('active', 'locked', 'role-action', 'release-pop'), 140);
    return;
  } else if (dragArrowSourceElement) {
    const source = dragArrowSourceElement;
    source.classList.remove('drag-cancel-pulse');
    void source.offsetWidth;
    source.classList.add('drag-cancel-pulse');
    setTimeout(() => source.classList.remove('drag-cancel-pulse'), 360);
  }
  hideAttackArrow();
}

function spawnDragReleaseBurst(targetElement, isRoleAction = false) {
  if (!targetElement || !ui.fx) return;
  const rect = stageRect(targetElement);
  const burst = document.createElement('div');
  burst.className = `drag-target-burst${isRoleAction ? ' role-action' : ''}`;
  burst.style.left = `${rect.left + rect.width / 2}px`;
  burst.style.top = `${rect.top + rect.height / 2}px`;
  burst.innerHTML = '<span></span><i class="s1"></i><i class="s2"></i><i class="s3"></i><i class="s4"></i>';
  ui.fx.appendChild(burst);
  setTimeout(() => burst.remove(), 520);
}

function showCharacterInspector(element) {
  const card = findCard(element.dataset.id);
  if (!card || dealing || ui.preview.classList.contains('open')) {
    hideCharacterInspector();
    return;
  }
  ui.inspector.classList.remove('draft-inspector-only');
  hideShieldInspector();
  hideRoleActionInspector();
  const translatedStatuses = card.statuses.map(status => i18n.status(status));
  const auraStatuses = sortAuraDisplayStatuses(translatedStatuses.filter(isAuraDisplayStatus));
  const visibleStatuses = translatedStatuses.filter(status => !isAuraDisplayStatus(status));
  const trait = primaryTrait(card);
  const localizedTrait = i18n.trait(trait?.id, card.cardType === 'Hero' ? card.heroRank : card.soldierRank, card.heroPathRoleActionId);
  localizedTrait.kind = i18n.traitTrigger(trait?.triggerKind);
  const visibleStatusMarkup = visibleStatuses.map(status => `<li class="${status.isBuff ? 'buff' : 'debuff'}">
        <div class="effect-title">${art.icon(art.forStatus(status.id), { size: 'md', label: status.name })}<div><b>${escapeHtml(status.name)}</b><em>${status.isBuff ? i18n.t('buff') : i18n.t('debuff')}${status.isAura ? ` / ${i18n.t('aura')}` : ''}${status.isDispellable === false ? ` / ${i18n.t('notDispellable')}` : ''}</em></div></div>
        <small>${escapeHtml(status.timing || i18n.t('always'))}</small><p>${escapeHtml(status.description)}</p>
      </li>`).join('');
  const auraMarkup = auraStatuses.length
    ? `<li class="buff aura-group">
        <div class="effect-title">${art.icon(art.forStatus(auraStatuses[0]?.id || 'foresight'), { size: 'md', label: i18n.t('aura') })}<div><b>${escapeHtml(i18n.t('aura'))}</b><em>×${escapeHtml(auraStatuses.length)}</em></div></div>
        <small>${escapeHtml(i18n.t('always'))}</small><p>${escapeHtml(auraStatuses.map(status => status.name).join(' / '))}</p>
        <div class="aura-detail" aria-hidden="true">
          <header><span>${escapeHtml(i18n.t('aura'))}</span><strong>${escapeHtml(i18n.t('effects'))}</strong></header>
          <ul>${auraStatuses.map(status => `<li>
            <div class="effect-title">${art.icon(art.forStatus(status.id), { size: 'md', label: status.name })}<div><b>${escapeHtml(status.name)}</b><em>${status.isDispellable === false ? escapeHtml(i18n.t('notDispellable')) : escapeHtml(i18n.t('aura'))}</em></div></div>
            <small>${escapeHtml(status.timing || i18n.t('always'))}</small>
            <p>${escapeHtml(status.description)}</p>
          </li>`).join('')}</ul>
        </div>
      </li>`
    : '';
  const statusMarkup = visibleStatusMarkup || auraMarkup
    ? `${visibleStatusMarkup}${auraMarkup}`
    : `<li class="empty-status">${i18n.t('noEffects')}</li>`;
  const displayedAttack = effectiveCardAttack(card);
  const attackDelta = displayedAttack - Number(card.baseAttack || 0);
  const attackDisplay = attackDelta === 0 ? `${displayedAttack}` : `${displayedAttack} (${attackDelta > 0 ? '+' : ''}${attackDelta})`;
  const morale = Math.max(0, Number(card.morale ?? 0));
  const maxMorale = Math.max(0, Number(card.maxMorale ?? 0));
  const isUpgradeChoiceMode = Boolean(game?.pendingRoleActionUpgrade?.canChoose)
    && Array.isArray(card.roleActionChoices) && card.roleActionChoices.length > 0;
  const visibleRoleActions = isUpgradeChoiceMode ? card.roleActionChoices : card.roleActions;
  const showRoleActions = !game?.heroDraft;
  const deputyMarkup = deputyInspectorMarkup(card);
  const roleActionMarkup = Array.isArray(visibleRoleActions) && visibleRoleActions.length
    ? visibleRoleActions.map(action => {
        const localized = i18n.roleAction(action.id);
        const summary = roleActionSummary(localized.description);
        const disabledText = action.disabledReason ? i18n.message(action.disabledReason) : '';
        const disabled = !isUpgradeChoiceMode && (!canUseLocalControls() || !action.enabled);
        const cooldownRemaining = Number(action.cooldownRemaining || 0);
        const cooldownTurns = Number(action.cooldownTurns || 0);
        const costLabel = cooldownRemaining > 0 ? `CD ${cooldownRemaining}` : `${action.cost} AP`;
        const draggable = !isUpgradeChoiceMode && !disabled && Array.isArray(action.validTargetKinds)
          && action.validTargetKinds.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind));
        return `<button class="role-action-button ${isUpgradeChoiceMode ? 'choice' : ''} ${disabled ? 'disabled' : 'enabled'}" type="button"
          data-character-id="${escapeHtml(card.id)}" data-role-action-id="${escapeHtml(action.id)}" data-role-action-mode="${escapeHtml(action.activationMode)}"
          data-role-action-targets="${escapeHtml((action.validTargetKinds || []).join(','))}"
          data-role-action-name="${escapeHtml(localized.name)}" data-role-action-description="${escapeHtml(localized.description)}"
          data-role-action-cost="${escapeHtml(action.cost)}" data-role-action-summary="${escapeHtml(summary)}"
          data-role-action-cooldown="${escapeHtml(cooldownTurns)}" data-role-action-cooldown-remaining="${escapeHtml(cooldownRemaining)}"
          ${draggable ? 'draggable="true"' : ''} ${disabled ? 'disabled' : ''}>
          <span>${escapeHtml(localized.button || localized.name)}</span><small>${escapeHtml(costLabel)}</small>
          <em>${escapeHtml(summary)}</em>
        </button>`;
      }).join('')
    : `<p class="role-action-empty">${escapeHtml(i18n.t('roleActionLocked'))}</p>`;
  ui.inspector.innerHTML = `<header><span>${i18n.t('unitDossier')}</span><div class="inspector-name-row"><strong>${escapeHtml(i18n.characterName(card.key))}</strong><b class="inspector-morale">${escapeHtml(i18n.t('moraleDamageShort'))} ${morale}/${maxMorale}</b></div></header>
    <div class="inspector-stats">
      <div class="stat-card stat-attack"><span>ATK</span><b>${escapeHtml(attackDisplay)}</b></div>
      <div class="stat-card stat-hp"><span>HP</span><b>${card.currentHp}/${card.maxHp}</b></div>
      <div class="stat-card stat-cost"><span>COST</span><b>${card.cost}</b></div>
      <div class="stat-card stat-type"><span>TYPE</span><b class="damage-type ${card.attackType === 'Magical' ? 'magic' : ''}">${escapeHtml(i18n.damageType(card.attackType))}</b></div>
      <div class="stat-card stat-pdef"><span>P.DEF</span>${defenseDeltaMarkup(card.physicalDefense, card.basePhysicalDefense)}</div>
      <div class="stat-card stat-mdef"><span>M.DEF</span>${defenseDeltaMarkup(card.magicalDefense, card.baseMagicalDefense)}</div>
    </div>
    <section class="inspector-trait ${trait?.isReady !== false ? 'ready' : 'disabled'}">
      <div class="inspector-trait-heading">${art.icon(art.forTrait(trait?.id), { size: 'md', label: localizedTrait.name })}<div><span>TRAIT / ${escapeHtml(localizedTrait.kind)}</span><b>${escapeHtml(localizedTrait.name)}</b><p>${escapeHtml(localizedTrait.description)}</p></div></div>
      ${trait?.unavailableReason ? `<small>${escapeHtml(i18n.message(trait.unavailableReason))}</small>` : ''}
    </section>
    ${deputyMarkup}
    ${showRoleActions ? `<section class="inspector-role-actions">
      <div class="inspector-section-label">${escapeHtml(isUpgradeChoiceMode ? i18n.t('roleActionChooseTitle') : i18n.t('roleActionTitle'))}</div>
      <div class="role-action-list">${roleActionMarkup}</div>
    </section>` : ''}`;
  const visibleStatusCount = visibleStatuses.length + (auraStatuses.length ? 1 : 0);
  ui.statusInspector.innerHTML = `<header><span>${i18n.t('liveEffects')}</span><strong>${i18n.t('effects')}</strong><b>${visibleStatusCount}</b></header>
    <section class="inspector-effects"><ul>${statusMarkup}</ul></section>`;
  const rect = stageRect(element);
  ui.inspector.classList.add('open');
  ui.inspector.setAttribute('aria-hidden', 'false');
  const showStatusInspector = !game?.heroDraft;
  ui.statusInspector.classList.toggle('open', showStatusInspector);
  ui.statusInspector.setAttribute('aria-hidden', String(!showStatusInspector));
  art.hydrate(ui.inspector);
  if (showStatusInspector) art.hydrate(ui.statusInspector);
  positionInspector(ui.inspector, Math.max(16, rect.left - ui.inspector.offsetWidth - 14), rect);
  if (showStatusInspector) {
    const characterTop = Number.parseFloat(ui.inspector.style.top) - ui.inspector.offsetHeight / 2;
    positionInspector(
      ui.statusInspector,
      Math.min(STAGE_WIDTH - ui.statusInspector.offsetWidth - 16, rect.right + 14),
      rect,
      { topEdge: characterTop }
    );
  }
}

function positionInspector(panel, left, targetRect, options = {}) {
  const halfHeight = panel.offsetHeight / 2;
  const centeredTopEdge = targetRect.top + targetRect.height / 2 - halfHeight;
  const requestedTopEdge = Number.isFinite(options.topEdge) ? options.topEdge : centeredTopEdge;
  const topEdge = Math.max(18, Math.min(STAGE_HEIGHT - panel.offsetHeight - 18, requestedTopEdge));
  panel.style.maxHeight = `${Math.max(220, STAGE_HEIGHT - topEdge - 18)}px`;
  panel.style.left = `${left}px`;
  panel.style.top = `${topEdge + panel.offsetHeight / 2}px`;
}

function hideCharacterInspector() {
  ui.inspector.classList.remove('draft-inspector-only');
  ui.inspector.classList.remove('open');
  ui.inspector.setAttribute('aria-hidden', 'true');
  delete ui.inspector.dataset.draftKey;
  ui.statusInspector.classList.remove('open');
  ui.statusInspector.setAttribute('aria-hidden', 'true');
  hideRoleActionInspector();
}

function deputyStatText(statKind, value) {
  const label = i18n.t(`deputyStat.${statKind}`) || statKind;
  return `${label} +${Number(value || 0)}`;
}

function deputyInspectorMarkup(card) {
  if (card?.deputy) {
    const deputy = i18n.deputy(card.deputy.effectId);
    return `<section class="inspector-deputy">
      <div class="inspector-section-label">${escapeHtml(i18n.t('deputyTitle'))}</div>
      <div class="deputy-current">
        <b>${escapeHtml(deputy.name)}</b>
        <small>${escapeHtml(i18n.characterName(card.deputy.soldierKey))} / ${escapeHtml(deputyStatText(card.deputy.statKind, card.deputy.statValue))}</small>
        <p>${escapeHtml(deputy.passive)}</p>
      </div>
    </section>`;
  }

  if (!card?.deputyPreview || card.cardType !== 'Soldier') return '';
  const deputy = i18n.deputy(card.deputyPreview.effectId);
  const disabled = !card.canAssignAsDeputy;
  const reason = card.assignDeputyDisabledReason ? i18n.t(`deputyDisabled.${card.assignDeputyDisabledReason}`) : '';
  return `<section class="inspector-deputy">
    <div class="inspector-section-label">${escapeHtml(i18n.t('deputyTitle'))}</div>
    <button class="deputy-assign-button ${disabled ? 'disabled' : 'enabled'}" type="button"
      data-soldier-id="${escapeHtml(card.id)}" data-deputy-id="${escapeHtml(card.deputyPreview.effectId)}"
      ${disabled ? 'disabled' : ''}>
      <span>${escapeHtml(i18n.t('assignDeputy'))}</span>
      <small>${escapeHtml(deputyStatText(card.deputyPreview.statKind, card.deputyPreview.statValue))}</small>
      <em>${escapeHtml(disabled ? reason : deputy.passive)}</em>
    </button>
  </section>`;
}

function roleActionSummary(description) {
  const text = String(description || '').trim();
  if (!text) return '';
  const sentence = text.split(/(?<=[。！？!?])/u)[0] || text;
  return truncateCardText(sentence, 48);
}

function showRoleActionInspector(button) {
  if (!ui.roleActionInspector || !button) return;
  const name = button.dataset.roleActionName || '';
  const description = button.dataset.roleActionDescription || '';
  const cost = button.dataset.roleActionCost || '';
  const cooldown = Number(button.dataset.roleActionCooldown || 0);
  const cooldownRemaining = Number(button.dataset.roleActionCooldownRemaining || 0);
  const targets = String(button.dataset.roleActionTargets || '').split(',').filter(Boolean);
  const targetText = targets.length ? targets.join(' / ') : button.dataset.roleActionMode || '';
  const cooldownText = cooldownRemaining > 0 ? `CD ${cooldownRemaining}` : cooldown > 0 ? `Cooldown ${cooldown}` : '';
  const formattedDescription = escapeHtml(description).replace(/\n/g, '<br>');
  const canUse = !button.disabled && !button.classList.contains('choice');
  const isTargeted = button.dataset.roleActionMode === 'Targeted'
    && targets.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind));
  ui.roleActionInspector.innerHTML = `<header><span>ROLE ACTION</span><strong>${escapeHtml(name)}</strong><b>${escapeHtml(cost)} AP</b></header>
    <p>${formattedDescription}</p>
    ${targetText || cooldownText ? `<footer>${escapeHtml([targetText, cooldownText].filter(Boolean).join(' / '))}</footer>` : ''}
    ${canUse ? `<button class="role-action-detail-use" type="button"
      data-character-id="${escapeHtml(button.dataset.characterId || '')}"
      data-role-action-id="${escapeHtml(button.dataset.roleActionId || '')}"
      data-role-action-mode="${escapeHtml(button.dataset.roleActionMode || '')}"
      data-role-action-targets="${escapeHtml(targets.join(','))}">${escapeHtml(i18n.t(isTargeted ? 'roleActionTargetButton' : 'roleActionUseButton'))}</button>` : ''}`;
  const buttonRect = stageRect(button);
  const inspectorRect = stageRect(ui.inspector);
  ui.roleActionInspector.classList.add('open');
  ui.roleActionInspector.setAttribute('aria-hidden', 'false');
  const detailWidth = ui.roleActionInspector.offsetWidth;
  let left = inspectorRect.left - detailWidth - 12;
  if (left < 12) left = inspectorRect.right + 12;
  if (left + detailWidth > STAGE_WIDTH - 12) left = Math.max(12, inspectorRect.left - detailWidth - 12);
  const top = Math.min(STAGE_HEIGHT - ui.roleActionInspector.offsetHeight - 18, Math.max(96, buttonRect.top - 18));
  ui.roleActionInspector.style.left = `${left}px`;
  ui.roleActionInspector.style.top = `${top}px`;
}

function hideRoleActionInspector() {
  if (!ui.roleActionInspector) return;
  ui.roleActionInspector.classList.remove('open');
  ui.roleActionInspector.setAttribute('aria-hidden', 'true');
}

function syncCombatInteractionUi({ syncInspector = true } = {}) {
  document.querySelectorAll('.fighter-card[data-id]').forEach(card => {
    const id = card.dataset.id;
    const cardData = findCard(id);
    const visuallyTargetable = !card.classList.contains('defeated') && !card.classList.contains('deploying');
    const isDraftCandidate = card.dataset.zone === 'DraftCandidate' || card.classList.contains('draft-candidate');
    const soldierUpgradeTarget = Boolean(soldierUpgradeKey)
      && card.dataset.side === 'active'
      && card.dataset.cardType === 'Soldier'
      && cardData?.key === soldierUpgradeKey
      && Number(card.dataset.soldierRank || 0) < 2
      && visuallyTargetable;
    const deputyTarget = Boolean(pendingDeputy)
      && card.dataset.side === 'active'
      && card.dataset.cardType === 'Hero'
      && !cardData?.deputy
      && visuallyTargetable;
    const roleActionTarget = Boolean(pendingRoleAction)
      && card.dataset.side === 'active'
      && visuallyTargetable;

    card.classList.toggle('selected', id === selectedAttacker || (isDraftCandidate && id === inspectedCardId));
    card.classList.toggle('target-selected', id === selectedDefender);
    card.classList.toggle('draft-inspected', isDraftCandidate && id === inspectedCardId);
    card.classList.toggle('deputy-target', deputyTarget);
    card.classList.toggle('soldier-upgrade-target', soldierUpgradeTarget);
    card.classList.toggle('role-action-target', roleActionTarget || deputyTarget || soldierUpgradeTarget);
  });
  ui.app.classList.toggle('attacker-selected', Boolean(selectedAttacker));
  ui.app.classList.toggle('role-action-targeting', Boolean(pendingRoleAction));
  ui.app.classList.toggle('deputy-targeting', Boolean(pendingDeputy));
  ui.app.classList.toggle('soldier-upgrade-targeting', Boolean(soldierUpgradeKey));
  updateInstruction();
  if (syncInspector) syncSelectedInspector();
}

function syncSelectedInspector() {
  if (!inspectedCardId || dealing || ui.preview.classList.contains('open')) {
    hideCharacterInspector();
    return;
  }
  const selectedCard = characterElementById(inspectedCardId);
  if (!selectedCard || selectedCard.classList.contains('defeated')) {
    hideCharacterInspector();
    return;
  }
  showCharacterInspector(selectedCard);
}

function showOpponentHoverInspector(element) {
  if (busy || dealing || ui.preview.classList.contains('open') || element.classList.contains('defeated')) return;
  if (suppressedOpponentInspectorId === element.dataset.id) return;
  inspectedCardId = element.dataset.id;
  showCharacterInspector(element);
}

function hideOpponentHoverInspector(element) {
  if (suppressedOpponentInspectorId === element.dataset.id) {
    suppressedOpponentInspectorId = null;
  }
  if (inspectedCardId !== element.dataset.id) return;

  inspectedCardId = selectedAttacker || null;
  if (selectedAttacker) {
    syncSelectedInspector();
  } else {
    hideCharacterInspector();
  }
}

function suppressOpponentHoverInspector(element) {
  if (ui.preview.classList.contains('open')) return;
  if (suppressedOpponentInspectorId === element.dataset.id) {
    suppressedOpponentInspectorId = null;
    inspectedCardId = element.dataset.id;
    showCharacterInspector(element);
    return;
  }
  suppressedOpponentInspectorId = element.dataset.id;
  if (inspectedCardId === element.dataset.id) {
    inspectedCardId = selectedAttacker || null;
    syncSelectedInspector();
  }
}

function showShieldInspector() {
  if (!game || dealing) return;
  const active = game.players.find(player => player.id === game.activePlayerId);
  const shield = active?.sharedShield || 0;
  const shieldPhysicalDefense = Number(active?.sharedShieldPhysicalDefense || 0);
  const shieldMagicalDefense = Number(active?.sharedShieldMagicalDefense || 0);
  const shieldActionIsReinforce = game.nextShieldCost === 1 && shield > 0;
  const shieldActionIsOpen = game.nextShieldCost === 2 && shield <= 0;
  const lightState = shield > 0 && shield <= 2 ? 'current' : shieldActionIsOpen ? 'next' : '';
  const reinforcedState = shield > 2 ? 'current' : shieldActionIsReinforce ? 'next' : '';
  const stateLabel = shield > 0 ? i18n.t('shieldCurrent', { value: shield }) : i18n.t('shieldNone');
  ui.shieldInspector.innerHTML = `<header><span>TACTICAL COMMAND</span><strong>${i18n.t('defenseFormation')}</strong><b>${game.nextShieldCost} AP</b></header>
    <p class="shield-rule-lead">${i18n.t('shieldLead')}</p>
    <section class="shield-tier shield-status-panel current">
      <span>SHIELD STATUS</span>
      <div class="shield-status-grid">
        <div><small>${i18n.t('shieldStatShield')}</small><b>${shield}</b></div>
        <div><small>${i18n.t('shieldStatPdef')}</small><b>${shieldPhysicalDefense}</b></div>
        <div><small>${i18n.t('shieldStatMdef')}</small><b>${shieldMagicalDefense}</b></div>
      </div>
      <p>${i18n.t('shieldDefenseOrder')}</p>
    </section>
    <div class="shield-tier-list">
      <section class="shield-tier ${lightState}">
        <div><span>FIRST FORM</span><b>${i18n.t('shieldFirst')}</b><em>2 AP / SHIELD 2</em></div>
        <p>${i18n.t('shieldFirstBody')}</p>
      </section>
      <section class="shield-tier reinforced ${reinforcedState}">
        <div><span>REINFORCED</span><b>${i18n.t('shieldSecond')}</b><em>+1 AP / SHIELD +2</em></div>
        <p>${i18n.t('shieldSecondBody')}</p>
      </section>
    </div>
    <ul class="shield-rule-notes">
      <li>${i18n.t('shieldNote1')}</li>
      <li>${i18n.t('shieldNote2')}</li>
      <li>${i18n.t('shieldNote3')}</li>
      <li>${i18n.t('shieldNote4')}</li>
    </ul>
    <footer>${stateLabel}</footer>`;
  const rect = stageRect(ui.shieldButton);
  ui.shieldInspector.classList.add('open');
  ui.shieldInspector.setAttribute('aria-hidden', 'false');
  positionInspector(ui.shieldInspector, Math.max(16, rect.left - ui.shieldInspector.offsetWidth - 14), rect);
}

function hideShieldInspector() {
  ui.shieldInspector.classList.remove('open');
  ui.shieldInspector.setAttribute('aria-hidden', 'true');
}

function showShieldDomeInspector(dome) {
  if (!dome?.classList.contains('active') || dealing) return;
  hideRoleActionInspector();
  const shield = Number(dome.dataset.shield || 0);
  if (shield <= 0) return;
  const pdef = Number(dome.dataset.pdef || 0);
  const mdef = Number(dome.dataset.mdef || 0);
  ui.shieldInspector.innerHTML = `<header><span>FIELD OBJECT</span><strong>${i18n.t('sharedShieldObject')}</strong></header>
    <section class="shield-tier shield-status-panel current">
      <span>SHIELD STATUS</span>
      <div class="shield-status-grid">
        <div><small>${i18n.t('shieldStatShield')}</small><b>${shield}</b></div>
        <div><small>${i18n.t('shieldStatPdef')}</small><b>${pdef}</b></div>
        <div><small>${i18n.t('shieldStatMdef')}</small><b>${mdef}</b></div>
      </div>
      <p>${i18n.t('shieldDefenseOrder')}</p>
    </section>`;
  const rect = stageRect(dome);
  ui.shieldInspector.classList.add('open');
  ui.shieldInspector.setAttribute('aria-hidden', 'false');
  positionInspector(ui.shieldInspector, Math.max(16, rect.left - ui.shieldInspector.offsetWidth - 14), rect);
}

function showBpInspector() {
  if (!game || dealing || !ui.bpHud || !ui.bpInspector) return;
  hideShieldInspector();
  const me = game.players.find(player => player.id === game.viewerPlayerId);
  const bp = me?.battlePoints;
  const current = bp ? `${bp.current}/${bp.max}` : '0/0';
  const gainCap = Math.max(0, Number(bp?.gainCapPerTurn || 5));
  ui.bpInspector.innerHTML = `<header><span>TACTICAL RESOURCE</span><strong>${i18n.t('bpTitle')}</strong><b>${current}</b></header>
    <p class="shield-rule-lead">${i18n.t('bpLead')}</p>
    <div class="shield-tier-list bp-rule-list">
      <section class="shield-tier current"><div><span>BASE</span><b>${i18n.t('bpRuleTurnStart')}</b><em>+1 BP</em></div></section>
      <section class="shield-tier"><div><span>ATTACK</span><b>${i18n.t('bpRuleDamage')}</b><em>+1 BP</em></div></section>
      <section class="shield-tier"><div><span>SHIELD</span><b>${i18n.t('bpRuleBreakShield')}</b><em>+1 BP</em></div></section>
      <section class="shield-tier"><div><span>COMMAND</span><b>${i18n.t('bpRuleFullShield')}</b><em>+1 BP</em></div></section>
      <section class="shield-tier"><div><span>ROLE</span><b>${i18n.t('bpRuleFirstRoleAction')}</b><em>+1 BP</em></div></section>
    </div>
    <ul class="shield-rule-notes">
      <li>${i18n.t('bpNote1', { cap: gainCap })}</li>
      <li>${i18n.t('bpNote2')}</li>
      <li>${i18n.t('bpNoteMoraleRecovery')}</li>
    </ul>`;
  const rect = stageRect(ui.bpHud);
  ui.bpInspector.classList.add('open');
  ui.bpInspector.setAttribute('aria-hidden', 'false');
  positionInspector(ui.bpInspector, Math.max(16, rect.left - ui.bpInspector.offsetWidth - 14), rect);
}

function hideBpInspector() {
  if (!ui.bpInspector) return;
  ui.bpInspector.classList.remove('open');
  ui.bpInspector.setAttribute('aria-hidden', 'true');
}

function roleActionTargetKinds(action) {
  if (Array.isArray(action?.targetKinds)) return action.targetKinds;
  if (Array.isArray(action?.validTargetKinds)) return action.validTargetKinds;
  return [];
}

function canRoleActionTargetCard(action, element) {
  const targets = roleActionTargetKinds(action);
  if (!element || element.classList.contains('defeated') || element.classList.contains('deploying')) return false;
  if (action?.roleActionId === 'guard-oath' && element.dataset.id === action.characterId) return false;
  if (action?.roleActionId === 'star-reading') return canStarReadingTarget(element.dataset.id);
  if (targets.includes('SelfCard') && element.dataset.id === action.characterId) return true;
  if (targets.includes('EnemyCard') && element.dataset.side === 'opponent') return true;
  if (targets.includes('AllyCard') && element.dataset.side === 'active') return true;
  return false;
}

function roleActionCardTargetSelector(action) {
  const targets = roleActionTargetKinds(action);
  const selectors = [];
  if (targets.includes('SelfCard') && !targets.includes('AllyCard'))
    selectors.push(`.fighter-card[data-id="${CSS.escape(action.characterId)}"]:not(.defeated):not(.deploying)`);
  if (targets.includes('AllyCard')) {
    if (action?.roleActionId === 'guard-oath')
      selectors.push(`.fighter-card[data-side="active"]:not(.defeated):not(.deploying):not([data-id="${CSS.escape(action.characterId)}"])`);
    else if (action?.roleActionId === 'star-reading')
      selectors.push('.fighter-card[data-side="active"]:not(.defeated):not(.deploying).attack-spent');
    else
      selectors.push('.fighter-card[data-side="active"]:not(.defeated):not(.deploying)');
  }
  if (targets.includes('EnemyCard')) selectors.push('.fighter-card[data-side="opponent"]:not(.defeated):not(.deploying)');
  return selectors.join(',');
}

function canStarReadingTarget(id) {
  const card = findCard(id);
  if (!card || !card.isAlive || card.attackType !== 'Magical' || !card.hasActed) return false;
  if ((card.statuses || []).some(status => status.id === 'deploying')) return false;
  return !(card.statuses || []).some(status => status.id === 'attack-sealed');
}

function roleActionTargetInstructionKey(targetKinds = []) {
  if (targetKinds.includes('EnemyCard')) return 'roleActionSelectEnemy';
  if (targetKinds.includes('AllyCard') || targetKinds.includes('SelfCard'))
    return 'roleActionSelectAlly';
  return 'roleActionSelectTarget';
}

function cancelAiming({ rerender = false } = {}) {
  const hadAiming = Boolean(selectedAttacker || pendingRoleAction || pendingDeputy || inspectedCardId || ui.preview.classList.contains('open'));
  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  pendingRoleAction = null;
  pendingDeputy = null;
  closePreview();
  hideAttackArrow();
  hideCharacterInspector();
  if (hadAiming && rerender) render();
  else if (hadAiming) syncCombatInteractionUi({ syncInspector: false });
}

function beginRoleActionTargeting(characterId, roleActionId, targetKinds, { renderFirst = true, startArrow = true } = {}) {
  pendingRoleAction = { characterId, roleActionId, targetKinds };
  pendingDeputy = null;
  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  closePreview();
  hideCharacterInspector();
  if (renderFirst) syncCombatInteractionUi({ syncInspector: false });
  if (startArrow) {
    const caster = characterElementById(characterId);
    if (caster) startAttackArrow(caster, 'role-action');
  }
  updateInstruction();
}

function beginDeputyTargeting(soldierId) {
  pendingDeputy = { soldierId };
  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  pendingRoleAction = null;
  closePreview();
  hideAttackArrow();
  hideCharacterInspector();
  sound.emit('ui.card-select');
  syncCombatInteractionUi({ syncInspector: false });
  showToast(i18n.t('deputySelectHero'));
}

function canAssignDeputyToCard(element) {
  return Boolean(pendingDeputy
    && element?.dataset?.side === 'active'
    && element.dataset.cardType === 'Hero'
    && !findCard(element.dataset.id)?.deputy
    && !element.classList.contains('defeated')
    && !element.classList.contains('deploying'));
}

async function assignDeputy(soldierId, heroId) {
  const soldier = findCard(soldierId);
  const hero = findCard(heroId);
  const deputy = i18n.deputy(soldier?.deputyPreview?.effectId);
  openDeputyConfirm({ soldierId, heroId, soldier, hero, deputy });
}

function openDeputyConfirm({ soldierId, heroId, soldier, hero, deputy }) {
  pendingDeputyConfirm = { soldierId, heroId };
  const soldierName = i18n.characterName(soldier?.key);
  const heroName = i18n.characterName(hero?.key);
  ui.deputyConfirmTitle.textContent = i18n.t('deputyConfirmTitle');
  ui.deputyConfirmSoldier.textContent = soldierName;
  ui.deputyConfirmHero.textContent = heroName;
  ui.deputyConfirmEffect.textContent = i18n.t('deputyConfirmEffect', { deputy: deputy.name });
  ui.deputyConfirmNote.textContent = i18n.t('deputyConfirmNotice');
  ui.deputyConfirmOk.textContent = i18n.t('assignDeputy');
  ui.deputyConfirmCancel.textContent = i18n.t('close');
  ui.deputyConfirm.classList.add('open');
  ui.deputyConfirm.setAttribute('aria-hidden', 'false');
  sound.emit('ui.card-select');
}

function closeDeputyConfirm() {
  pendingDeputyConfirm = null;
  ui.deputyConfirm.classList.remove('open');
  ui.deputyConfirm.setAttribute('aria-hidden', 'true');
}

async function confirmDeputyAssignment() {
  if (!pendingDeputyConfirm || busy) return;
  const { soldierId, heroId } = pendingDeputyConfirm;

  busy = true;
  ui.deputyConfirmOk.disabled = true;
  try {
    const payload = await gameApi('/game/deputy/assign', { method: 'POST', body: JSON.stringify({ soldierId, heroId }) });
    game = payload.data;
    lastGameJson = JSON.stringify(game);
    pendingDeputy = null;
    closeDeputyConfirm();
    inspectedCardId = heroId;
    render();
    animateDeputyAssigned(heroId);
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    ui.deputyConfirmOk.disabled = false;
  }
}

function animateDeputyAssigned(heroId) {
  requestAnimationFrame(() => {
    const card = characterElementById(heroId);
    if (!card) return;
    card.classList.remove('deputy-bound-pulse');
    void card.offsetWidth;
    card.classList.add('deputy-bound-pulse');
    setTimeout(() => card.classList.remove('deputy-bound-pulse'), 1100);
  });
}

function inspectPassiveCard(id) {
  const isSameCard = inspectedCardId === id && !selectedAttacker;
  const card = findCard(id);
  selectedAttacker = null;
  selectedDefender = null;
  pendingRoleAction = null;
  pendingDeputy = null;
  closePreview();
  hideAttackArrow();
  inspectedCardId = isSameCard ? null : id;
  if (!isSameCard) {
    sound.emit('ui.card-select');
    if (card?.zone === 'DraftCandidate') emitSelectVoice(card);
  }
  if (card?.zone === 'DraftCandidate') render();
  else syncCombatInteractionUi();
}

function onCardClick(element) {
  if (busy) return;
  const id = element.dataset.id;
  if (pendingDeputy) {
    if (!canAssignDeputyToCard(element)) {
      pendingDeputy = null;
      syncCombatInteractionUi({ syncInspector: false });
      return;
    }
    assignDeputy(pendingDeputy.soldierId, id);
    return;
  }
  if (soldierUpgradeKey) {
    if (element.dataset.side === 'active'
      && element.dataset.cardType === 'Soldier'
      && element.dataset.key === soldierUpgradeKey
      && Number(element.dataset.soldierRank || 0) < 2) {
      upgradeSoldierFromDraft(soldierUpgradeKey, id);
    } else {
      sound.emit('ui.invalid-action');
      showToast(i18n.t('soldierDraftUpgradeTargetHint'));
    }
    return;
  }
  if (pendingRoleAction) {
    if (!canRoleActionTargetCard(pendingRoleAction, element)) {
      finishAttackArrow();
      cancelAiming();
      return;
    }
    finishAttackArrow(element);
    useRoleAction(pendingRoleAction.characterId, pendingRoleAction.roleActionId, id);
    return;
  }
  if (element.dataset.side === 'active') {
    if (game?.pendingRoleActionUpgrade?.canChoose) {
      if (!element.classList.contains('upgrade-selectable')) {
        sound.emit('ui.invalid-action');
        showToast(i18n.t('roleActionUpgradeInvalid'));
        return;
      }
      const cardData = findCard(id);
      if (cardData && Number(cardData.heroRank || 0) > 0) {
        selectRoleActionUpgrade(id, '');
        return;
      }
      selectedAttacker = null; selectedDefender = null; inspectedCardId = id; closePreview();
      sound.emit('ui.card-select');
      render();
      return;
    }
    if (!element.classList.contains('can-act')) {
      inspectPassiveCard(id);
      return;
    }
    const isSelecting = selectedAttacker !== id;
    selectedAttacker = isSelecting ? id : null; selectedDefender = null; inspectedCardId = isSelecting ? id : null; closePreview();
    if (isSelecting) {
      sound.emit('ui.card-select');
      emitSelectVoice(element);
    }
    syncCombatInteractionUi(); return;
  }
  if (element.classList.contains('defeated') || element.classList.contains('deploying')) return;
  if (!selectedAttacker) { suppressOpponentHoverInspector(element); return; }
  inspectedCardId = id;
  chooseDefender(id);
}

async function chooseDefender(id) {
  selectedDefender = id;
  try {
    const payload = await gameApi(`/game/preview?attackerId=${selectedAttacker}&defenderId=${id}`);
    preview = payload.data;
    if (!preview.isValid) throw new Error(i18n.message(preview.error));
    syncCombatInteractionUi({ syncInspector: false });
    renderPreview();
    emitAttackPreviewVoice(findCard(selectedAttacker), findCard(selectedDefender), preview);
  } catch (error) {
    selectedDefender = null;
    syncCombatInteractionUi();
    sound.emit('ui.invalid-action');
    showToast(error.message);
  }
}

function renderPreview() {
  const attacker = findCard(selectedAttacker), defender = findCard(selectedDefender);
  ui.previewAttacker.textContent = attacker ? i18n.characterName(attacker.key) : '—';
  ui.previewDefender.textContent = defender ? i18n.characterName(defender.key) : '—';
  setForecast(ui.previewDamage, preview.attack, i18n.t('damageDealt'), art.forDamageType(preview.attack.damageType));
  setForecast(ui.previewCounter, preview.counter, i18n.t('counterTaken'), 'event.counter');
  ui.previewTrait.className = `preview-trait ${preview.traitConditionPossible ? '' : 'inactive'}`;
  const localizedTrait = i18n.trait(preview.traitId);
  ui.previewTrait.innerHTML = `<strong>${escapeHtml(localizedTrait.name)}</strong><p>${escapeHtml(i18n.message(preview.traitForecast))}</p>`;
  ui.previewNotes.innerHTML = preview.notes.map(note => `<li>${escapeHtml(i18n.message(note))}</li>`).join('');
  art.hydrate(ui.preview);
  ui.preview.classList.add('open'); ui.preview.setAttribute('aria-hidden', 'false');
  hideCharacterInspector();
}

function setForecast(element, forecast, label, iconId) {
  const value = forecastRange(forecast.min, forecast.max);
  const moraleValue = forecastRange(forecast.moraleDamageMin ?? 0, forecast.moraleDamageMax ?? 0);
  const hpValue = forecastRange(forecast.hpDamageMin ?? 0, forecast.hpDamageMax ?? 0);
  const forecastClass = forecast.damageType === 'Magical' ? 'magic' : forecast.damageType === 'Absolute' ? 'absolute' : 'physical';
  element.className = `forecast-box ${forecastClass}`;
  element.innerHTML = `<div class="forecast-heading">${art.icon(iconId, { size: 'sm', label })}<small>${label} / ${i18n.damageType(forecast.damageType)}</small></div><div class="forecast-values"><strong>${escapeHtml(String(value))}</strong><div class="forecast-landing"><span>${escapeHtml(i18n.t('moraleDamageShort'))} -${escapeHtml(String(moraleValue))}</span><span>HP -${escapeHtml(String(hpValue))}</span></div></div>`;
}

function forecastRange(min, max) {
  const low = Number(min ?? 0);
  const high = Number(max ?? low);
  return low === high ? String(high) : `${low}~${high}`;
}

async function executeAttack() {
  if (busy || !selectedAttacker || !selectedDefender) return;
  sound.emit('ui.attack-confirm');
  busy = true; ui.confirm.disabled = true;
  const attackerId = selectedAttacker, defenderId = selectedDefender;
  closePreview();
  try {
    pendingVisualBaselines = captureCharacterVisualBaselines(game);
    const payload = await gameApi('/game/attack', { method: 'POST', body: JSON.stringify({ attackerId, defenderId }) });
    sound.emit('ui.ap-spend');
    game = payload.data; lastGameJson = JSON.stringify(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingDeputy = null; preview = null; render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; ui.confirm.disabled = false; }
}

async function endTurn() {
  if (!canUseLocalControls() || game.phase === 'Finished' || endTurnQueued) return;
  sound.emit('ui.turn-end');
  endTurnQueued = true;
  ui.endTurn.classList.add('queued');
  ui.endTurn.classList.remove('ap-empty-ready');
  ui.endTurn.setAttribute('aria-busy', 'true');
  while (busy) await wait(40);
  if (!canUseLocalControls() || game.phase === 'Finished') {
    endTurnQueued = false;
    ui.endTurn.classList.remove('queued');
    ui.endTurn.setAttribute('aria-busy', 'false');
    return;
  }
  busy = true;
  try {
    pendingVisualBaselines = captureCharacterVisualBaselines(game);
    const endingPlayer = game.players.find(player => player.id === game.viewerPlayerId);
    const bpRecoveryAmount = Math.max(0, Number(endingPlayer?.battlePoints?.gainedThisTurn || 0));
    const payload = await gameApi('/game/end-turn', { method: 'POST', body: '{}' });
    await animateBpRecoveryToHp(bpRecoveryAmount, payload.data);
    game = payload.data; lastGameJson = JSON.stringify(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview();
    await playTurnCurtain(game.activePlayerName, true);
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally {
    busy = false;
    endTurnQueued = false;
    ui.endTurn.classList.remove('queued');
    ui.endTurn.setAttribute('aria-busy', 'false');
    if (game) render();
  }
}

async function deployShield() {
  if (busy || !canUseLocalControls() || !game?.canDeployShield) return;
  sound.emit('ui.shield-command');
  busy = true;
  ui.shieldButton.disabled = true;
  selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview(); hideCharacterInspector(); hideShieldInspector();
  try {
    pendingVisualBaselines = captureCharacterVisualBaselines(game);
    const payload = await gameApi('/game/shield', { method: 'POST', body: '{}' });
    sound.emit('ui.ap-spend');
    game = payload.data; lastGameJson = JSON.stringify(game);
    render();
    await playNewLogEvents(game, { applyFinalVisualState: false });
  } catch (error) {
    showToast(error.message);
    if (game) render();
  }
  finally {
    busy = false;
    ui.shieldButton.disabled = false;
  }
}

async function selectReward(instanceId) {
  if (busy || !game?.rewardWindow?.canChoose || !instanceId) return;
  sound.emit('ui.confirm');
  busy = true;
  try {
    const payload = await gameApi('/game/reward/select', { method: 'POST', body: JSON.stringify({ instanceId }) });
    game = payload.data; lastGameJson = JSON.stringify(game); pendingRoleAction = null; pendingDeputy = null; render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; if (game) render(); }
}

async function resetRewardWindow() {
  if (busy || !game?.rewardWindow?.canChoose) return;
  sound.emit('ui.panel-toggle');
  busy = true;
  try {
    const payload = await gameApi('/game/reward/reset', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game); pendingRoleAction = null; pendingDeputy = null; render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; if (game) render(); }
}

async function skipRewardWindow() {
  if (busy || !game?.rewardWindow?.canChoose) return;
  sound.emit('ui.cancel');
  busy = true;
  try {
    const payload = await gameApi('/game/reward/skip', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game); pendingRoleAction = null; pendingDeputy = null; render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; if (game) render(); }
}

async function selectHeroDraft(characterKey, { animateOpening = false } = {}) {
  if (busy || !game?.heroDraft?.canChoose || !characterKey) return;
  sound.emit('ui.confirm');
  busy = true;
  try {
    if (animateOpening) {
      const isTestOpening = game?.heroDraft?.kind === 'TestOpening';
      document.querySelectorAll('.fighter-card.draft-candidate[data-side="active"]').forEach(card => {
        card.classList.toggle('draft-selected', card.dataset.key === characterKey);
        card.classList.toggle('draft-leaving', !isTestOpening && card.dataset.key !== characterKey);
      });
      await wait(isTestOpening ? 160 : 360);
    }
    const payload = await gameApi('/game/hero-draft/select', {
      method: 'POST',
      body: JSON.stringify({ characterKey })
    });
    game = payload.data; lastGameJson = JSON.stringify(game);
    pendingRoleAction = null;
    pendingDeputy = null;
    selectedHeroDraftKey = null;
    selectedAttacker = null;
    selectedDefender = null;
    inspectedCardId = null;
    render();
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    if (game) render();
  }
}

async function resetHeroDraft() {
  if (busy || !game?.heroDraft?.canChoose || !['Recruit', 'SoldierRecruit'].includes(game.heroDraft.kind)) return;
  sound.emit('ui.confirm');
  busy = true;
  try {
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    const payload = await gameApi('/game/hero-draft/reset', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game);
    render();
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    if (game) render();
  }
}

async function returnToRewardWindow() {
  if (busy || !game?.rewardWindow?.canChoose || !(game?.heroDraft || game?.pendingRoleActionUpgrade || game?.pendingRelicReward)) return;
  sound.emit('ui.cancel');
  busy = true;
  try {
    const payload = await gameApi('/game/reward/back', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game);
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    pendingRoleAction = null;
    pendingDeputy = null;
    selectedAttacker = null;
    selectedDefender = null;
    inspectedCardId = null;
    hideAttackArrow();
    hideCharacterInspector();
    hideRoleActionInspector();
    render();
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    if (game) render();
  }
}

function findOwnedSoldier(key) {
  const viewer = game?.players?.find(player => player.id === game.viewerPlayerId);
  const matches = viewer?.characters?.filter(card => card.isInBattle && card.cardType === 'Soldier' && card.key === key) || [];
  return matches.find(card => Number(card.soldierRank || 0) < 2) || matches[0] || null;
}

function viewerActiveCharacterCount() {
  const viewer = game?.players?.find(player => player.id === game.viewerPlayerId);
  return viewer?.characters?.filter(card => card.isInBattle).length || 0;
}

function selectedRecruitSoldierUpgradeKey() {
  if (game?.heroDraft?.kind !== 'SoldierRecruit' || selectedHeroDraftKeys.length !== 1) return null;
  const key = selectedHeroDraftKeys[0];
  const owned = findOwnedSoldier(key);
  return owned && Number(owned.soldierRank || 0) < 2 ? key : null;
}

async function selectSoldierDraft() {
  if (busy || !game?.heroDraft?.canChoose || !selectedHeroDraftKeys.length) return;
  if (game.heroDraft.kind === 'SoldierRecruit' && viewerActiveCharacterCount() >= 4) {
    sound.emit('ui.invalid-action');
    showToast(i18n.t('soldierDraftFullUpgradeOnly'));
    return;
  }
  sound.emit('ui.confirm');
  busy = true;
  try {
    const payload = await gameApi('/game/hero-draft/soldier/select', {
      method: 'POST',
      body: JSON.stringify({ characterKeys: selectedHeroDraftKeys })
    });
    game = payload.data; lastGameJson = JSON.stringify(game);
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    render();
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    if (game) render();
  }
}

async function upgradeSoldierFromDraft(characterKey, targetCharacterId) {
  if (busy || !game?.heroDraft?.canChoose || game.heroDraft.kind !== 'SoldierRecruit') return;
  const beforeCard = findCard(targetCharacterId);
  sound.emit('ui.confirm');
  busy = true;
  rewardVisualHold = true;
  try {
    const payload = await gameApi('/game/hero-draft/soldier/upgrade', {
      method: 'POST',
      body: JSON.stringify({ characterKey, targetCharacterId })
    });
    game = payload.data; lastGameJson = JSON.stringify(game);
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    render();
    await animateSoldierRankUp(targetCharacterId, beforeCard);
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    rewardVisualHold = false;
    busy = false;
    if (game) render();
  }
}

async function cancelSoldierRecruitDraft() {
  if (busy || !game?.heroDraft?.canChoose || game.heroDraft.kind !== 'SoldierRecruit') return;
  sound.emit('ui.cancel');
  busy = true;
  try {
    const payload = await gameApi('/game/hero-draft/soldier/cancel', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game);
    selectedHeroDraftKey = null;
    selectedHeroDraftKeys = [];
    soldierUpgradeKey = null;
    render();
    await playNewLogEvents(game);
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  } finally {
    busy = false;
    if (game) render();
  }
}

function animateSoldierRankUp(targetCharacterId, beforeCard = null) {
  return new Promise(resolve => {
    requestAnimationFrame(() => {
      const card = document.querySelector(`.fighter-card[data-id="${CSS.escape(String(targetCharacterId))}"]`);
      if (!card) { resolve(); return; }
      const afterCard = findCard(targetCharacterId);
      const rect = stageRect(card);
      const overlay = document.createElement('div');
      overlay.className = 'rank-up-transform';
      overlay.style.left = `${rect.left}px`;
      overlay.style.top = `${rect.top}px`;
      overlay.style.width = `${rect.width}px`;
      overlay.style.height = `${rect.height}px`;
      const beforeRank = Number(beforeCard?.soldierRank || 0);
      const afterRank = Number(afterCard?.soldierRank || beforeRank);
      const oldBgClass = beforeRank < 1 ? ' rank-bg-normal' : '';
      const newBgClass = afterRank < 1 ? ' rank-bg-normal' : '';
      const rankLabel = afterRank >= 2 ? 'RANK II' : afterRank === 1 ? 'RANK I' : 'RANK';
      overlay.innerHTML = `
        <div class="rank-up-card old${oldBgClass}"><img src="${escapeHtml(beforeCard?.coloredAssetUrl || beforeCard?.assetUrl || afterCard?.assetUrl || '')}" alt=""></div>
        <div class="rank-up-card new${newBgClass}"><img src="${escapeHtml(afterCard?.coloredAssetUrl || afterCard?.assetUrl || '')}" alt=""></div>
        <span>${rankLabel}</span>`;
      ui.fx.appendChild(overlay);
      card.classList.remove('soldier-rank-up-flash');
      void card.offsetWidth;
      card.classList.add('soldier-rank-up-flash');
      sound.emit('status.buff-applied');
      setTimeout(() => {
        card.classList.remove('soldier-rank-up-flash');
        overlay.remove();
        resolve();
      }, 1280);
    });
  });
}

function animateHeroRankUp(targetCharacterId, beforeCard = null) {
  return new Promise(resolve => {
    requestAnimationFrame(() => {
      const card = document.querySelector(`.fighter-card[data-id="${CSS.escape(String(targetCharacterId))}"]`);
      if (!card) { resolve(); return; }
      const afterCard = findCard(targetCharacterId);
      const rect = stageRect(card);
      const overlay = document.createElement('div');
      overlay.className = 'rank-up-transform';
      overlay.style.left = `${rect.left}px`;
      overlay.style.top = `${rect.top}px`;
      overlay.style.width = `${rect.width}px`;
      overlay.style.height = `${rect.height}px`;
      const beforeRank = Number(beforeCard?.heroRank || 0);
      const afterRank = Number(afterCard?.heroRank || beforeRank);
      const oldBgClass = beforeRank >= 2 ? ' rank-bg-advanced-hero' : '';
      const newBgClass = afterRank >= 2 ? ' rank-bg-advanced-hero' : '';
      const rankLabel = afterRank >= 3 ? 'RANK III' : afterRank >= 2 ? 'RANK II' : afterRank === 1 ? 'RANK I' : 'RANK';
      overlay.innerHTML = `
        <div class="rank-up-card old${oldBgClass}"><img src="${escapeHtml(beforeCard?.coloredAssetUrl || beforeCard?.assetUrl || afterCard?.assetUrl || '')}" alt=""></div>
        <div class="rank-up-card new${newBgClass}"><img src="${escapeHtml(afterCard?.coloredAssetUrl || afterCard?.assetUrl || '')}" alt=""></div>
        <span>${rankLabel}</span>`;
      ui.fx.appendChild(overlay);
      card.classList.remove('soldier-rank-up-flash');
      void card.offsetWidth;
      card.classList.add('soldier-rank-up-flash');
      sound.emit('status.buff-applied');
      setTimeout(() => {
        card.classList.remove('soldier-rank-up-flash');
        overlay.remove();
        resolve();
      }, 1280);
    });
  });
}

async function animateMonsterRage(card) {
  if (!card) return;
  card.classList.remove('monster-rage-transform');
  void card.offsetWidth;
  card.classList.add('monster-rage-transform');
  await wait(1040);
  card.classList.remove('monster-rage-transform');
}

function beginSoldierUpgradeTargeting() {
  const draft = game?.heroDraft;
  if (!draft?.canChoose || draft.kind !== 'SoldierRecruit' || selectedHeroDraftKeys.length !== 1) return;
  const key = selectedHeroDraftKeys[0];
  const owned = findOwnedSoldier(key);
  if (!owned || Number(owned.soldierRank || 0) >= 2) {
    sound.emit('ui.invalid-action');
    showToast(i18n.t('soldierDraftMaxRank'));
    return;
  }
  soldierUpgradeKey = key;
  sound.emit('ui.card-select');
  showToast(i18n.t('soldierDraftUpgradeTargetHint'));
  render();
}

async function selectRoleActionUpgrade(characterId, roleActionId) {
  if (busy || !game?.pendingRoleActionUpgrade?.canChoose) return;
  sound.emit('ui.confirm');
  busy = true;
  const beforeCard = findCard(characterId);
  const beforeHeroRank = Number(beforeCard?.heroRank || 0);
  try {
    const payload = await gameApi('/game/role-action/upgrade', {
      method: 'POST',
      body: JSON.stringify({ characterId, roleActionId })
    });
    game = payload.data; lastGameJson = JSON.stringify(game);
    const afterCard = findCard(characterId);
    const afterHeroRank = Number(afterCard?.heroRank || 0);
    const shouldAnimateHeroRankChange = beforeCard?.cardType === 'Hero'
      && afterHeroRank > beforeHeroRank
      && afterHeroRank >= 2;
    pendingRoleAction = null;
    pendingDeputy = null;
    inspectedCardId = characterId;
    render();
    if (shouldAnimateHeroRankChange) await animateHeroRankUp(characterId, beforeCard);
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; if (game) render(); }
}

async function useRoleAction(characterId, roleActionId, targetCharacterId = null) {
  if (busy || !canUseLocalControls()) return;
  sound.emit('ui.confirm');
  busy = true;
  pendingRoleAction = null;
  selectedAttacker = null;
  selectedDefender = null;
  hideAttackArrow();
  hideRoleActionInspector();
  try {
    pendingVisualBaselines = captureCharacterVisualBaselines(game);
    const payload = await gameApi('/game/role-action/use', {
      method: 'POST',
      body: JSON.stringify({ characterId, roleActionId, targetCharacterId })
    });
    sound.emit('ui.ap-spend');
    game = payload.data; lastGameJson = JSON.stringify(game);
    pendingRoleAction = null;
    pendingDeputy = null;
    selectedAttacker = null; selectedDefender = null; inspectedCardId = characterId; closePreview();
    render();
    await playNewLogEvents(game, { applyFinalVisualState: false });
  } catch (error) {
    sound.emit('ui.invalid-action');
    showToast(error.message);
  }
  finally { busy = false; if (game) render(); }
}

async function newGame() {
  if (busy || !game?.isHost) { showToast(i18n.t('hostOnlyRestart')); return; }
  busy = true;
  try {
    sound.emit('game.restart');
    const path = sessionMode === 'test' ? '/game/test/new' : sessionMode === 'ai' ? '/game/ai/new' : '/game/new';
    const payload = await gameApi(path, { method: 'POST', body: '{}' });
    if (sessionMode === 'online' && room) room.dealStarted = false;
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; lastBpGainSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview();
    dealing = true; render();
    await playDealSequence();
  }
  catch (error) { showToast(error.message); } finally { busy = false; }
}

function shouldAdvanceAi() {
  return sessionMode === 'ai'
    && hasStarted
    && game
    && game.phase !== 'Finished'
    && !game.canControl
    && !game.rewardWindow?.canChoose
    && !game.heroDraft?.canChoose
    && !game.pendingRoleActionUpgrade?.canChoose
    && !game.pendingRelicReward?.canChoose
    && !busy
    && !dealing
    && !eventPlayback
    && !aiAdvancing;
}

function scheduleAiAdvance() {
  if (aiAdvanceTimer || !shouldAdvanceAi()) return;
  aiAdvanceTimer = window.setTimeout(() => {
    aiAdvanceTimer = null;
    advanceAiTurn();
  }, 420);
}

async function advanceAiTurn() {
  if (!shouldAdvanceAi()) return;
  aiAdvancing = true;
  const oldActivePlayerId = game?.activePlayerId;
  try {
    const payload = await gameApi('/game/ai/advance', { method: 'POST', body: '{}' });
    const nextGame = payload.data;
    const changed = JSON.stringify(nextGame) !== lastGameJson;
    if (!changed) return;
    pendingVisualBaselines = captureCharacterVisualBaselines(game);
    game = nextGame;
    lastGameJson = JSON.stringify(game);
    pendingRoleAction = null;
    pendingDeputy = null;
    selectedAttacker = null;
    selectedDefender = null;
    inspectedCardId = null;
    closePreview();
    if (oldActivePlayerId && oldActivePlayerId !== game.activePlayerId)
      turnCurtainLock = true;
    render();
    await playNewLogEvents(game);
    if (oldActivePlayerId && oldActivePlayerId !== game.activePlayerId)
      await playTurnCurtain(game.activePlayerName, true);
  } catch (error) {
    showToast(error.message);
  } finally {
    aiAdvancing = false;
    if (game) render();
  }
}

async function startLocalGame() {
  if (hasStarted || busy) return;
  sessionMode = 'local';
  hasStarted = true;
  busy = true;
  sound.emit('game.start');
  sound.unlock({ primeUnrequested: false });
  try {
    const payload = await gameApi('/game/new', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; lastBpGainSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview();
    dealing = true; render();
    openDealLayer();
    ui.startScreen.classList.add('leaving');
    await wait(470);
    await playDealSequence(true);
  } catch (error) {
    hasStarted = false; dealing = false;
    ui.startScreen.classList.remove('leaving');
    closeDealLayer();
    showToast(error.message);
  } finally { busy = false; }
}

async function startAiGame() {
  if (hasStarted || busy) return;
  sessionMode = 'ai';
  hasStarted = true;
  busy = true;
  sound.emit('game.start');
  sound.unlock({ primeUnrequested: false });
  try {
    const payload = await gameApi('/game/ai/new', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; lastBpGainSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview();
    dealing = true; render();
    openDealLayer();
    ui.startScreen.classList.add('leaving');
    await wait(470);
    await playDealSequence(true);
  } catch (error) {
    hasStarted = false; dealing = false;
    ui.startScreen.classList.remove('leaving');
    closeDealLayer();
    showToast(error.message);
  } finally { busy = false; if (game) render(); }
}

async function startTestGame() {
  if (hasStarted || busy) return;
  sessionMode = 'test';
  hasStarted = true;
  busy = true;
  sound.emit('game.start');
  sound.unlock({ primeUnrequested: false });
  try {
    const payload = await gameApi('/game/test/new', { method: 'POST', body: '{}' });
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; lastBpGainSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; pendingDeputy = null; closePreview();
    dealing = true; render();
    openDealLayer();
    ui.startScreen.classList.add('leaving');
    await wait(470);
    await playDealSequence(true);
  } catch (error) {
    hasStarted = false; dealing = false;
    ui.startScreen.classList.remove('leaving');
    closeDealLayer();
    showToast(error.message);
  } finally { busy = false; }
}

function openDealLayer() {
  ui.dealStatus.textContent = 'SHUFFLING';
  ui.dealCaption.textContent = i18n.t('dealShuffle');
  ui.dealSequence.classList.remove('waiting-for-touch');
  ui.fx.classList.add('dealing-fx');
  ui.dealSequence.classList.add('open');
  ui.dealSequence.setAttribute('aria-hidden', 'false');
}

function closeDealLayer() {
  ui.dealSequence.classList.remove('open', 'waiting-for-touch');
  ui.dealSequence.setAttribute('aria-hidden', 'true');
  ui.fx.classList.remove('dealing-fx');
}

async function playTurnCurtain(playerName = game?.activePlayerName, emitSound = true) {
  if (!playerName) {
    setTurnCurtainLock(false);
    return;
  }
  setTurnCurtainLock(true);
  try {
    if (emitSound) sound.emit('turn.change');
    ui.curtainPlayer.textContent = i18n.playerName(playerName);
    ui.curtain.classList.remove('play');
    void ui.curtain.offsetWidth;
    ui.curtain.classList.add('play');
    await wait(1000);
    ui.curtain.classList.remove('play');
  } finally {
    setTurnCurtainLock(false);
  }
}

async function playDealSequence(alreadyOpen = false) {
  if (!alreadyOpen) openDealLayer();
  sound.emit('ui.shuffle');
  await wait(780);
  // Ensure the next deck-touch gesture always sees real Audio elements. This
  // closes the first-load race without preventing the game if audio failed.
  await audioLoadPromise;
  ui.dealStatus.textContent = 'TOUCH THE DECK';
  ui.dealCaption.textContent = i18n.t(sessionMode === 'online' ? 'dealTouchOnline' : 'dealTouch');
  ui.dealSequence.classList.add('waiting-for-touch');
  await waitForDeckTouch();
  sound.emit('ui.deck-touch');
  ui.dealSequence.classList.remove('waiting-for-touch');
  ui.dealStatus.textContent = 'DEALING';
  ui.dealCaption.textContent = i18n.t('dealDealing');
  sound.emit('ui.deal');

  const top = [...ui.opponentCards.querySelectorAll('.fighter-card')];
  const bottom = [...ui.activeCards.querySelectorAll('.fighter-card')];
  const targets = [];
  const dealCount = Math.max(top.length, bottom.length);
  for (let index = 0; index < dealCount; index++) {
    if (top[index]) targets.push(top[index]);
    if (bottom[index]) targets.push(bottom[index]);
  }
  await Promise.all(targets.map((target, index) => wait(index * DEAL_CARD_STAGGER_MS).then(() => dealCardTo(target, index))));
  dealing = false;
  await wait(DEAL_CLOSE_DELAY_MS);
  closeDealLayer();
  render();
  await playTurnCurtain(game.activePlayerName, true);
  sound.emit('game.match-start');
}

function waitForDeckTouch() {
  if (sessionMode !== 'online')
    return new Promise(resolve => ui.dealDeck.addEventListener('click', resolve, { once: true }));

  return new Promise((resolve, reject) => {
    let finished = false;
    let signaling = false;
    let polling = false;
    let timer = null;
    const cleanup = () => {
      if (timer) clearInterval(timer);
      ui.dealDeck.removeEventListener('click', signalDeal);
    };
    const finish = () => {
      if (finished) return;
      finished = true;
      cleanup();
      resolve();
    };
    const fail = error => {
      if (finished) return;
      finished = true;
      cleanup();
      reject(error);
    };
    const signalDeal = async () => {
      if (signaling || finished) return;
      signaling = true;
      try {
        await gameApi('/game/deal', { method: 'POST', body: '{}' });
        if (room) room.dealStarted = true;
        finish();
      } catch (error) {
        signaling = false;
        showToast(error.message);
      }
    };
    const checkRemoteDeal = async () => {
      if (polling || finished) return;
      polling = true;
      try {
        const payload = await api('/api/online/room');
        room = payload.data;
        if (room.dealStarted) finish();
      } catch (error) {
        if (error.status === 401) fail(error);
      } finally { polling = false; }
    };
    ui.dealDeck.addEventListener('click', signalDeal);
    timer = setInterval(checkRemoteDeal, 250);
    if (room?.dealStarted) finish();
    else checkRemoteDeal();
  });
}

function dealCardTo(target, index) {
  return new Promise(resolve => {
    const rect = stageRect(target);
    const card = document.createElement('i');
    card.className = 'deal-card-back';
    card.style.width = `${rect.width}px`; card.style.height = `${rect.height}px`;
    const startX = STAGE_WIDTH / 2 - rect.width / 2;
    const startY = STAGE_HEIGHT / 2 - rect.height / 2;
    card.style.left = `${startX}px`; card.style.top = `${startY}px`;
    ui.fx.appendChild(card);
    const dx = rect.left - startX, dy = rect.top - startY;
    const rotation = (index % 2 ? 1 : -1) * (4 + index * .5);
    const animation = card.animate([
      { transform: `translate(0,0) scale(.32) rotate(${rotation * -1}deg)`, opacity: .25 },
      { opacity: 1, offset: .2 },
      { transform: `translate(${dx}px,${dy}px) scale(1) rotate(${rotation}deg)`, opacity: 1 }
    ], { duration: DEAL_CARD_TRAVEL_MS, easing: 'cubic-bezier(.18,.78,.25,1)', fill: 'forwards' });
    animation.onfinish = () => {
      sound.emit('ui.card-place');
      target.classList.remove('deal-hidden'); target.classList.add('dealt'); card.remove();
      setTimeout(() => target.classList.remove('dealt'), 420);
      resolve();
    };
  });
}

const eventIcon = Object.freeze({
  physical: 'event.physical', magical: 'event.magical', counter: 'event.counter', trait: 'event.trait',
  status: 'event.status-tick', heal: 'event.heal', shield: 'event.shield', death: 'event.death'
});

const eventLabelKey = Object.freeze({
  [eventIcon.physical]: 'eventPhysical', [eventIcon.magical]: 'eventMagical', [eventIcon.counter]: 'eventCounter',
  [eventIcon.trait]: 'eventTrait', [eventIcon.status]: 'eventStatus', [eventIcon.heal]: 'eventHeal',
  [eventIcon.shield]: 'eventShield', [eventIcon.death]: 'eventDeath'
});

function eventLabel(iconId) {
  return i18n.t(eventLabelKey[iconId] || 'eventTrait');
}

function effectLabel(arg) {
  if (!arg) return i18n.t('eventTrait');
  if (arg.kind === 'status') return i18n.status({ id: arg.value, magnitude: 0 }).name;
  if (arg.kind === 'roleAction') return i18n.roleAction(arg.value).name;
  if (arg.kind === 'reward') return i18n.reward(arg.value).name;
  return i18n.trait(arg.value).name;
}

function resetEventCursor(state = game) {
  lastAnimatedLogSequence = Math.max(0, ...(state?.log || []).map(entry => entry.sequence || 0));
  pendingVisualBaselines = null;
}

function captureCharacterVisualBaselines(state = game) {
  if (!state) return null;
  const baselines = new Map();
  for (const character of state.players.flatMap(player => player.characters)) {
    baselines.set(String(character.id), {
      currentHp: character.currentHp,
      morale: character.morale,
      isAlive: character.isAlive,
      isInBattle: character.isInBattle
    });
  }
  return baselines;
}

function hasUnplayedLogEvents(state = game) {
  return Boolean((state?.log || []).some(entry => entry.sequence > lastAnimatedLogSequence));
}

function pendingDefeatAnimationIds(state = game) {
  return new Set((state?.log || [])
    .filter(entry => entry.sequence > lastAnimatedLogSequence && entry.message?.key === 'log.defeated')
    .map(entry => String(logArg(entry, 'characterId') || ''))
    .filter(Boolean));
}

function logArg(entry, name) {
  return entry?.message?.args?.[name]?.value;
}

function logArgObject(entry, name) {
  return entry?.message?.args?.[name] || null;
}

function characterElementByKey(state, key) {
  const character = state?.players.flatMap(player => player.characters).find(card => card.key === key);
  return character ? document.querySelector(`[data-id="${character.id}"]`) : null;
}

function characterElementById(id) {
  return id ? document.querySelector(`[data-id="${CSS.escape(String(id))}"]`) : null;
}

function characterElementFromLog(state, entry, name = 'character') {
  return characterElementById(logArg(entry, `${name}Id`)) || characterElementByKey(state, logArg(entry, name));
}
function primaryCharacterElementFromLog(state, entry) {
  return characterElementFromLog(state, entry)
    || characterElementFromLog(state, entry, 'target')
    || characterElementFromLog(state, entry, 'actor')
    || characterElementFromLog(state, entry, 'source');
}

function playerForCharacterKey(state, key) {
  return state?.players.find(player => player.characters.some(card => card.key === key)) || null;
}

function playerForCharacterFromLog(state, entry, name = 'character') {
  const id = logArg(entry, `${name}Id`);
  if (id) {
    const player = state?.players.find(player => player.characters.some(card => String(card.id) === String(id)));
    if (player) return player;
  }
  return playerForCharacterKey(state, logArg(entry, name));
}

function playerFromLog(state, entry) {
  const playerName = logArg(entry, 'player');
  return state?.players.find(player => player.name === playerName) || null;
}

function teamVisual(player) {
  if (!player || !game) return null;
  const isViewer = player.id === game.viewerPlayerId;
  const row = isViewer ? ui.activeCards : ui.opponentCards;
  const dome = isViewer ? ui.activeShieldDome : ui.opponentShieldDome;
  const cards = [...row.querySelectorAll('.fighter-card')];
  if (!cards.length) return { point: center(row), dome };
  const first = stageRect(cards[0]), last = stageRect(cards[cards.length - 1]);
  return { point: { x: (first.left + last.right) / 2, y: isViewer ? first.top + Math.min(first.height * .22, 72) : first.bottom - Math.min(first.height * .22, 72) }, dome };
}

function eventBurst(target, iconId, options = {}) {
  if (!target) return Promise.resolve();
  const point = target instanceof Element ? center(target) : target;
  const el = document.createElement('div');
  const secondary = options.secondaryIconId
    ? art.icon(options.secondaryIconId, { size: 'md', label: options.secondaryLabel || options.secondaryIconId, className: 'event-burst-secondary' })
    : '';
  const amount = options.amount === undefined || options.amount === null ? '' : `<b>${escapeHtml(options.amount)}</b>`;
  const title = options.title || eventLabel(iconId);
  el.className = `event-burst ${options.tone || 'trait'}`;
  el.style.left = `${point.x}px`; el.style.top = `${point.y}px`;
  el.innerHTML = `${art.icon(iconId, { size: 'lg', label: title, className: 'event-burst-primary' })}${secondary}<span class="event-burst-copy"><strong>${escapeHtml(title)}</strong>${amount}</span>`;
  ui.fx.appendChild(el); art.hydrate(el);
  setTimeout(() => el.remove(), options.duration || 1250);
  return wait(options.hold || 450);
}

async function roleActionBurst(roleActionId, target, options = {}) {
  const action = i18n.roleAction(roleActionId);
  const roleActionIcon = art.forRoleAction(roleActionId);
  await eventBurst(target, options.iconId || roleActionIcon, {
    title: action.name,
    secondaryIconId: options.secondaryIconId || (options.iconId ? roleActionIcon : null),
    amount: options.amount,
    tone: options.tone || 'trait',
    hold: options.hold
  });
}

async function playExchangeEvent(entry, state, options = {}) {
  const attackerCharacter = characterFromLogData(state, entry, 'attacker');
  const defenderCharacter = characterFromLogData(state, entry, 'defender');
  const attackerKey = attackerCharacter?.key || logArg(entry, 'attacker');
  const defenderKey = defenderCharacter?.key || logArg(entry, 'defender');
  const attacker = characterElementFromLog(state, entry, 'attacker');
  const defender = characterElementFromLog(state, entry, 'defender');
  if (!attacker || !defender) return;
  const attackType = logArg(entry, 'attackType') || 'Physical';
  const counterType = logArg(entry, 'counterType') || 'Physical';
  const attackDamage = Number(logArg(entry, 'attackDamage') || 0);
  const counterDamage = Number(logArg(entry, 'counterDamage') || 0);
  const attackMoraleDamage = Number(logArg(entry, 'attackMoraleDamage') || 0);
  const counterMoraleDamage = Number(logArg(entry, 'counterMoraleDamage') || 0);
  const attackShieldAbsorbed = Number(logArg(entry, 'attackShieldAbsorbed') || 0);
  const counterShieldAbsorbed = Number(logArg(entry, 'counterShieldAbsorbed') || 0);
  const guardRedirects = options.guardRedirects || [];
  const hasGuardRedirect = guardRedirects.length > 0;
  const defeatedTarget = defenderDefeatedByExchange(defenderCharacter || defenderKey, attackDamage, state);
  const attackerVoiceType = defeatedTarget ? 'defeat' : 'attack-declare';
  const defenderVoiceType = defeatedTarget ? null : damageVoiceTypeForCharacter(defenderCharacter, attackDamage);
  sound.emit('combat.target-lock');
  emitVoice(attackerVoiceType, attackerKey, { targetCharacterId: defenderKey, damageType: attackType, amount: attackDamage });
  if (hasGuardRedirect) guardRedirects.forEach(emitGuardVoice);
  if (defenderVoiceType)
    emitVoiceDelayed(defenderVoiceType, defenderKey, { source: 'active-attack', damageType: attackType, attackerCharacterId: attackerKey, amount: attackDamage }, hasGuardRedirect ? GUARDED_TARGET_VOICE_REACTION_DELAY_MS : voiceReactionDelay(attackerVoiceType, defenderVoiceType));
  if (attackType === 'Magical') sound.emit('combat.magic-active');
  if (attackType === 'Physical' && attackDamage > 0 && attackShieldAbsorbed === 0) sound.emit('combat.physical-hit');
  if (counterType === 'Physical' && counterDamage > 0 && counterShieldAbsorbed === 0) sound.emit('combat.physical-hit');
  if (attackDamage === 0 && attackMoraleDamage === 0 && attackShieldAbsorbed === 0) sound.emit('combat.no-damage');
  if (counterDamage === 0 && counterMoraleDamage === 0 && counterShieldAbsorbed === 0) emitSoundThrottled('combat.no-damage', 180);
  const combatLink = showCombatLink(attacker, defender);
  const attackHadImpact = attackDamage > 0 || attackMoraleDamage > 0 || attackShieldAbsorbed > 0;
  const counterHadImpact = counterDamage > 0 || counterMoraleDamage > 0 || counterShieldAbsorbed > 0;
  if (attackHadImpact) launchFx(attacker, defender, attackType);
  if (counterHadImpact) launchFx(defender, attacker, counterType);
  await wait(180);
  if (attackType === 'Magical' && attackDamage > 0 && attackShieldAbsorbed === 0) sound.emit('combat.magic-impact');
  if (counterType === 'Magical' && counterDamage > 0 && counterShieldAbsorbed === 0) sound.emit('combat.magic-counter');
  for (const guardEntry of guardRedirects) {
    await playGuardRedirectEvent(guardEntry, state, { emitVoice: false });
  }
  await Promise.all([
    eventBurst(defender, art.forDamageType(attackType), { title: attackType === 'Magical' ? i18n.t('eventMagical') : i18n.t('eventPhysical'), amount: attackMoraleDamage > 0 || attackDamage <= 0 ? null : `-${attackDamage}`, tone: attackType === 'Magical' ? 'magic' : 'physical' }),
    eventBurst(attacker, eventIcon.counter, { title: i18n.t('eventCounter'), secondaryIconId: art.forDamageType(counterType), amount: counterMoraleDamage > 0 || counterDamage <= 0 ? null : `-${counterDamage}`, tone: 'counter' })
  ]);
  defender.classList.add(attackType === 'Magical' ? 'impact-magic' : 'impact-physical');
  attacker.classList.add(counterType === 'Magical' ? 'impact-magic' : 'impact-physical');
  setTimeout(() => defender.classList.remove('impact-magic', 'impact-physical'), 520);
  setTimeout(() => attacker.classList.remove('impact-magic', 'impact-physical'), 520);
  await Promise.all([
    playLayeredDamageFloats(defender, { moraleAmount: attackMoraleDamage, hpAmount: attackDamage, type: attackType }),
    playLayeredDamageFloats(attacker, { moraleAmount: counterMoraleDamage, hpAmount: counterDamage, type: counterType })
  ]);
  await wait(470);
  if (combatLink) {
    combatLink.classList.add('leaving');
    setTimeout(() => combatLink.remove(), 320);
  }
}

async function playLogEntry(entry, state) {
  const key = entry?.message?.key;
  const characterData = characterFromLogData(state, entry)
    || characterFromLogData(state, entry, 'target')
    || characterFromLogData(state, entry, 'actor')
    || characterFromLogData(state, entry, 'source');
  const characterKey = characterData?.key || logArg(entry, 'character') || logArg(entry, 'target') || logArg(entry, 'actor') || logArg(entry, 'source');
  const target = primaryCharacterElementFromLog(state, entry);
  const effect = logArgObject(entry, 'effect');
  const status = logArgObject(entry, 'status');
  const trait = logArgObject(entry, 'trait');
  const effectIcon = effect?.kind === 'status'
    ? art.forStatus(effect.value)
    : effect?.kind === 'roleAction'
      ? art.forRoleAction(effect.value)
      : effect?.kind === 'reward'
        ? relicIconId(effect.value)
        : art.forTrait(effect?.value);
  const amount = Number(logArg(entry, 'amount') || 0);

  switch (key) {
    case 'log.exchange':
      await playExchangeEvent(entry, state); break;
    case 'note.shieldAbsorb': {
      const player = playerForCharacterFromLog(state, entry);
      if (logArg(entry, 'damageType') === 'Physical') sound.emit('combat.shield-block');
      else sound.emit('combat.shield-block-magic');
      await eventBurst(target, eventIcon.shield, { amount: `-${amount}`, tone: 'shield' });
      if (!player?.sharedShield) sound.emit('shield.break');
      else sound.emit('shield.hit');
      if (target) shieldBlock(target, amount, player?.sharedShield || 0);
      await wait(player?.sharedShield > 0 ? 160 : 560);
      break;
    }
    case 'note.foresightReduction':
      sound.emit('status.foresight-proc');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'foresight', magnitude: 0 }).name, secondaryIconId: art.forStatus('foresight'), amount: `-${amount}`, tone: 'status' }); break;
    case 'note.magicBonus':
      sound.emit('status.magic-bonus');
      await eventBurst(target, eventIcon.trait, { title: i18n.trait('stargazers-aegis').name, secondaryIconId: art.forTrait('stargazers-aegis'), amount: `+${amount}`, tone: 'trait' }); break;
    case 'note.chantMagic':
      sound.emit('status.magic-bonus');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'chant', magnitude: amount }).name, secondaryIconId: art.forStatus('chant'), tone: 'magic' }); break;
    case 'note.strongAttack':
      sound.emit('status.buff-applied');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'strong-attack', magnitude: 0 }).name, secondaryIconId: art.forStatus('strong-attack'), tone: 'physical' }); break;
    case 'note.mightyStrikePhysical':
      sound.emit('status.buff-applied');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'mighty-strike', magnitude: 0 }).name, secondaryIconId: art.forStatus('mighty-strike'), tone: 'physical' }); break;
    case 'note.magicSurge':
      sound.emit('status.magic-bonus');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'magic-surge', magnitude: 0 }).name, secondaryIconId: art.forStatus('magic-surge'), tone: 'magic' }); break;
    case 'note.voidMagic':
      sound.emit('status.magic-bonus');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'void', magnitude: amount }).name, secondaryIconId: art.forStatus('void'), tone: 'magic' }); break;
    case 'note.exhaustionPhysical':
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'exhaustion', magnitude: amount }).name, secondaryIconId: art.forStatus('exhaustion'), tone: 'status' }); break;
    case 'note.erosionMagical':
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'erosion', magnitude: amount }).name, secondaryIconId: art.forStatus('erosion'), tone: 'status' }); break;
    case 'note.fortifyPhysical':
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'fortify', magnitude: amount }).name, secondaryIconId: art.forStatus('fortify'), tone: 'status' }); break;
    case 'note.guardOathPhysical':
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'guard-oath', magnitude: amount }).name, secondaryIconId: art.forStatus('guard-oath'), tone: 'status' }); break;
    case 'note.guardRedirect':
      await playGuardRedirectEvent(entry, state, { emitVoice: true }); break;
    case 'log.effectDamage':
    case 'log.effectDamageWithMorale': {
      const type = logArg(entry, 'damageType') || 'Physical';
      const hpAmount = key === 'log.effectDamageWithMorale' ? Number(logArg(entry, 'hpAmount') || 0) : amount;
      const moraleAmount = key === 'log.effectDamageWithMorale' ? Number(logArg(entry, 'moraleAmount') || 0) : 0;
      const totalAmount = key === 'log.effectDamageWithMorale' ? Math.max(amount, hpAmount + moraleAmount) : amount;
      if (effect?.kind === 'status' && effect.value === 'burning') sound.emit('status.burning-tick');
      else if (effect?.kind === 'trait' && effect.value === 'aftershock-axe') sound.emit('trait.aftershock-axe');
      else if (effect?.kind === 'trait' && effect.value === 'predatory-instinct') {
        sound.emit('trait.predatory-instinct');
        sound.emit('combat.absolute-damage');
      }
      else if (effect?.kind === 'trait') sound.emit('combat.trait-damage');
      else if (effect?.kind === 'roleAction') sound.emit('combat.trait-damage');
      else if (type === 'Absolute' && totalAmount > 0) sound.emit('combat.absolute-damage');
      else if (type === 'Physical' && totalAmount > 0) sound.emit('combat.physical-hit');
      if (effect?.kind === 'status')
        await eventBurst(target, eventIcon.status, { title: effectLabel(effect), secondaryIconId: effectIcon, tone: 'status' });
      else
        await eventBurst(characterElementFromLog(state, entry, 'source'), eventIcon.trait, { title: effectLabel(effect), secondaryIconId: effectIcon, tone: 'trait' });
      await eventBurst(target, art.forDamageType(type), { title: i18n.damageType(type), amount: moraleAmount > 0 || totalAmount <= 0 ? null : `-${totalAmount}`, tone: type === 'Magical' ? 'magic' : type === 'Physical' ? 'physical' : 'trait' });
      if (target) await playLayeredDamageFloats(target, { moraleAmount, hpAmount, type });
      emitDamageTakenVoice(characterData || characterKey, hpAmount, state, { source: effect?.kind === 'status' ? 'status' : effect?.kind === 'roleAction' ? 'role-action' : 'trait', damageType: type, traitId: effect?.kind === 'trait' ? effect.value : undefined, statusId: effect?.kind === 'status' ? effect.value : undefined });
      await wait(340); break;
    }
    case 'log.moraleDamage': {
      if (effect?.kind === 'roleAction') sound.emit('combat.trait-damage');
      await eventBurst(target, effectIcon || eventIcon.status, { title: effectLabel(effect), secondaryIconId: effectIcon ? eventIcon.status : null, tone: 'status' });
      if (target) {
        animateMoraleRingLoss(target, amount);
        moraleFloat(target, amount);
      }
      await wait(340); break;
    }
    case 'log.collateralDamage':
      if (effect?.value === 'guard') sound.emit('trait.guard-collateral');
      else if (amount > 0) sound.emit('combat.physical-hit');
      await eventBurst(target, eventIcon.trait, { title: effectLabel(effect), secondaryIconId: effectIcon, amount: amount > 0 ? `-${amount}` : null, tone: 'trait' });
      if (target && amount > 0) damageFloat(target, amount, 'Physical');
      if (effect?.value !== 'guard')
        emitDamageTakenVoice(characterData || characterKey, amount, state, { source: 'collateral', damageType: 'Physical', statusId: effect?.value });
      await wait(340); break;
    case 'log.healed':
      emitSoundThrottled('status.blessing-heal', 450);
      await eventBurst(target, eventIcon.heal, { title: effectLabel(effect), secondaryIconId: effectIcon, amount: `+${amount}`, tone: 'heal' }); break;
    case 'log.roleActionCleanse':
      sound.emit('status.expired');
      await eventBurst(target, eventIcon.status, { title: effectLabel(status), secondaryIconId: art.forStatus(status?.value), amount: '×', tone: 'status' }); break;
    case 'log.roleActionHeal':
      emitSoundThrottled('status.blessing-heal', 450);
      await roleActionBurst('mend', characterElementFromLog(state, entry, 'actor'), { iconId: eventIcon.heal, tone: 'heal', hold: 260 });
      await eventBurst(characterElementFromLog(state, entry, 'target'), eventIcon.heal, { title: i18n.roleAction('mend').name, amount: `+${amount}`, tone: 'heal' }); break;
    case 'log.statusApplied':
      if (status?.value === 'burning') sound.emit('status.burning-applied');
      if (status?.value === 'chant' || status?.value === 'magic-surge') sound.emit('status.magic-bonus');
      if (status?.value === 'spell-ward' || status?.value === 'fortify' || status?.value === 'strong-attack' || status?.value === 'mighty-strike') sound.emit('status.buff-applied');
      if (status?.value === 'exhaustion' || status?.value === 'erosion' || status?.value === 'void' || status?.value === 'trembling' || status?.value === 'vulnerable') sound.emit('status.debuff-applied');
      if (status?.value === 'beast-rage') {
        sound.emit('status.beast-rage');
        await animateMonsterRage(target);
      }
      await eventBurst(target, eventIcon.status, { title: effectLabel(status), secondaryIconId: art.forStatus(status?.value), tone: 'status' }); break;
    case 'log.deputyTriggered': {
      const deputy = logArgObject(entry, 'deputy');
      const deputyName = deputy?.kind === 'deputy' ? i18n.deputy(deputy.value).name : i18n.t('deputyTitle');
      if (status?.value === 'chant' || status?.value === 'magic-surge') sound.emit('status.magic-bonus');
      else sound.emit('status.buff-applied');
      await eventBurst(characterElementFromLog(state, entry, 'target'), eventIcon.trait, {
        title: deputyName,
        secondaryIconId: art.forStatus(status?.value),
        tone: status?.value === 'chant' || status?.value === 'magic-surge' ? 'magic' : status?.value === 'strong-attack' ? 'physical' : 'trait'
      });
      break;
    }
    case 'log.attackBuffRemoved':
      sound.emit('status.expired');
      await eventBurst(target, eventIcon.status, { title: effectLabel(status), secondaryIconId: art.forStatus(status?.value), amount: '×', tone: 'status' }); break;
    case 'log.harvestActivated':
      sound.emit('status.harvest-activated');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'harvest', magnitude: 0 }).name, secondaryIconId: art.forStatus('harvest'), tone: 'status' }); break;
    case 'log.sowing':
      sound.emit('status.sowing');
      await eventBurst(target, eventIcon.trait, { title: i18n.trait('spring-harvest').name, secondaryIconId: art.forTrait('spring-harvest'), tone: 'trait' }); break;
    case 'log.supplyBasket':
      emitSoundThrottled('status.blessing-heal', 450);
      sound.emit('status.buff-applied');
      await roleActionBurst('supply-basket', characterElementFromLog(state, entry, 'actor'), { iconId: eventIcon.heal, tone: 'heal', hold: 240 });
      await eventBurst(characterElementFromLog(state, entry, 'target'), eventIcon.heal, { title: i18n.roleAction('supply-basket').name, secondaryIconId: art.forStatus('fortify'), amount: `+${amount}`, tone: 'heal' }); break;
    case 'log.fieldWorkDouble':
      sound.emit('status.buff-applied');
      await roleActionBurst('field-work', target, { secondaryIconId: art.forStatus('harvest'), tone: 'trait' }); break;
    case 'log.fieldWorkRest':
      emitSoundThrottled('status.blessing-heal', 450);
      await roleActionBurst('field-work', target, { iconId: eventIcon.heal, amount: `+${amount}`, tone: 'heal' }); break;
    case 'log.fieldWorkSowing':
      sound.emit('status.sowing');
      await roleActionBurst('field-work', target, { secondaryIconId: art.forStatus('harvest-pending'), tone: 'trait' }); break;
    case 'log.aegisFormation': {
      sound.emit('shield.reinforce');
      const visual = teamVisual(playerForCharacterFromLog(state, entry));
      await roleActionBurst('aegis-formation', characterElementFromLog(state, entry), { iconId: eventIcon.shield, amount: `+${logArg(entry, 'shield') || ''}`, tone: 'shield', hold: 220 });
      await eventBurst(visual?.point, eventIcon.shield, { title: i18n.roleAction('aegis-formation').name, amount: `+${logArg(entry, 'shield') || ''}`, tone: 'shield' });
      if (visual?.dome) await playShieldFormationAnimation(visual.dome);
      break;
    }
    case 'log.crimsonLunge':
      sound.emit('combat.trait-damage');
      sound.emit('status.buff-applied');
      await roleActionBurst('crimson-lunge', characterElementFromLog(state, entry, 'actor'), { secondaryIconId: art.forStatus('mighty-strike'), tone: 'physical', hold: 240 });
      await eventBurst(characterElementFromLog(state, entry, 'target'), eventIcon.status, { title: i18n.status({ id: 'mighty-strike', magnitude: 0 }).name, secondaryIconId: art.forStatus('mighty-strike'), tone: 'physical' }); break;
    case 'log.traitFailed':
      if (trait?.value === 'weakening-spores') sound.emit('status.debuff-applied');
      await eventBurst(target, eventIcon.trait, { title: effectLabel(trait), secondaryIconId: art.forTrait(trait?.value), amount: '×', tone: 'trait' }); break;
    case 'log.defeated':
      sound.emit('combat.character-defeated');
      emitVoice('death', characterKey);
      await eventBurst(target, eventIcon.death, { tone: 'death', hold: 430 });
      if (target) target.classList.add('death-strike');
      await wait(780); break;
    case 'log.shieldDeployed':
    case 'log.shieldReinforced': {
      sound.emit(key === 'log.shieldDeployed' ? 'shield.deploy' : 'shield.reinforce');
      const visual = teamVisual(playerFromLog(state, entry));
      await eventBurst(visual?.point, eventIcon.shield, { amount: `+${logArg(entry, 'shield') || ''}`, tone: 'shield' });
      if (visual?.dome) await playShieldFormationAnimation(visual.dome);
      break;
    }
    case 'log.shieldExpired': {
      sound.emit('shield.expire');
      const visual = teamVisual(playerFromLog(state, entry));
      await eventBurst(visual?.point, eventIcon.shield, { amount: '×', tone: 'shield' }); break;
    }
  }
}

async function playNewLogEvents(state, options = {}) {
  const entries = (state?.log || []).filter(entry => entry.sequence > lastAnimatedLogSequence).sort((a, b) => a.sequence - b.sequence);
  const pendingGuardRedirects = [];
  let shouldRenderAfter = false;
  for (const entry of entries) {
    if (entry.message?.key === 'note.guardRedirect') {
      pendingGuardRedirects.push(entry);
      lastAnimatedLogSequence = Math.max(lastAnimatedLogSequence, entry.sequence || 0);
      continue;
    }
    if (entry.message?.key === 'log.exchange' && pendingGuardRedirects.length) {
      await playExchangeEvent(entry, state, { guardRedirects: pendingGuardRedirects.splice(0) });
    } else {
      if (pendingGuardRedirects.length) {
        for (const guardEntry of pendingGuardRedirects.splice(0)) {
          await playLogEntry(guardEntry, state);
        }
      }
      await playLogEntry(entry, state);
    }
    if (entry.message?.key === 'log.defeated') shouldRenderAfter = true;
    lastAnimatedLogSequence = Math.max(lastAnimatedLogSequence, entry.sequence || 0);
  }
  if (pendingGuardRedirects.length) {
    for (const guardEntry of pendingGuardRedirects.splice(0)) {
      await playLogEntry(guardEntry, state);
      lastAnimatedLogSequence = Math.max(lastAnimatedLogSequence, guardEntry.sequence || 0);
    }
  }
  const shouldApplyFinalVisualState = options.applyFinalVisualState !== false
    && (Boolean(pendingVisualBaselines) || shouldRenderAfter);
  pendingVisualBaselines = null;
  if (shouldApplyFinalVisualState && state === game) render();
}

function launchFx(from, to, type) {
  const a = center(from), b = center(to), fx = document.createElement('i');
  fx.className = `fx-projectile ${type === 'Magical' ? 'magic' : 'physical'}`; fx.style.left = `${a.x}px`; fx.style.top = `${a.y}px`; ui.fx.appendChild(fx);
  fx.animate([{ transform: 'translate(0,0) scale(.5)', opacity: 0 }, { opacity: 1, offset: .18 }, { transform: `translate(${b.x-a.x}px,${b.y-a.y}px) scale(1.3)`, opacity: 0 }], { duration: 330, easing: 'cubic-bezier(.3,.8,.3,1)' }).onfinish = () => fx.remove();
}
function showCombatLink(attacker, defender) {
  if (!attacker || !defender) return null;
  const from = center(attacker), to = center(defender);
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const distance = Math.hypot(dx, dy);
  const arcLift = Math.min(230, Math.max(80, distance * 0.22));
  const controlX = from.x + dx * 0.5;
  const controlY = from.y + dy * 0.5 - arcLift;
  const headAngle = Math.atan2(to.y - controlY, to.x - controlX) * 180 / Math.PI;
  const path = `M ${from.x.toFixed(1)} ${from.y.toFixed(1)} Q ${controlX.toFixed(1)} ${controlY.toFixed(1)}, ${to.x.toFixed(1)} ${to.y.toFixed(1)}`;
  const gradientId = `combat-arc-gradient-${Date.now().toString(36)}-${Math.floor(Math.random() * 10000)}`;
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.classList.add('combat-target-link');
  svg.setAttribute('viewBox', `0 0 ${STAGE_WIDTH} ${STAGE_HEIGHT}`);
  svg.setAttribute('aria-hidden', 'true');
  svg.innerHTML = `
    <defs>
      <linearGradient id="${gradientId}" gradientUnits="userSpaceOnUse" x1="${from.x}" y1="${from.y}" x2="${to.x}" y2="${to.y}">
        <stop offset="0%" stop-color="var(--combat-arc-tail)" stop-opacity=".18"></stop>
        <stop offset="54%" stop-color="var(--combat-arc-red)" stop-opacity=".9"></stop>
        <stop offset="100%" stop-color="var(--combat-arc-hot)" stop-opacity="1"></stop>
      </linearGradient>
    </defs>
    <path class="combat-link-glow" d="${path}" pathLength="1"/>
    <path class="combat-link-core" d="${path}" pathLength="1" stroke="url(#${gradientId})"/>
    <circle class="combat-link-source" cx="${from.x}" cy="${from.y}" r="7"/>
    <g class="combat-link-head" transform="translate(${to.x} ${to.y}) rotate(${headAngle.toFixed(1)})">
      <path class="combat-head-chevron" d="M -18 -9 L 2 0 L -18 9"></path>
      <circle class="combat-head-aura" r="20"></circle>
      <circle class="combat-head-ring" r="11"></circle>
      <circle class="combat-head-core" r="5"></circle>
      <circle class="combat-head-particle p1" cx="18" cy="-5" r="2.2"></circle>
      <circle class="combat-head-particle p2" cx="-14" cy="8" r="1.8"></circle>
      <circle class="combat-head-particle p3" cx="8" cy="17" r="1.5"></circle>
      <circle class="combat-head-particle p4" cx="-21" cy="-7" r="1.3"></circle>
    </g>`;
  ui.fx.appendChild(svg);
  return svg;
}
function playLayeredDamageFloats(target, { moraleAmount = 0, hpAmount = 0, type = 'Physical' } = {}) {
  const morale = Math.max(0, Number(moraleAmount || 0));
  const hp = Math.max(0, Number(hpAmount || 0));
  if (!target || (!morale && !hp)) return Promise.resolve();
  if (morale > 0) {
    animateMoraleRingLoss(target, morale);
    moraleFloat(target, morale);
    if (hp > 0)
      window.setTimeout(() => damageFloat(target, hp, type, { label: 'HP' }), 210);
    return wait(360);
  }
  damageFloat(target, hp, type);
  return Promise.resolve();
}

function damageFloat(target, amount, type, options = {}) {
  const value = Math.max(0, Number(amount || 0));
  if (!options.force && value <= 0) return;
  if (value > 0) emitSoundThrottled('combat.hp-loss', 90);
  if (value > 0) animateHpOrbLoss(target, value);
  const p = center(target), el = document.createElement('b');
  const tone = type === 'Magical' ? 'magic' : type === 'Absolute' ? 'absolute' : 'physical';
  el.className = `damage-float ${tone}${value <= 0 ? ' zero' : ''}`;
  el.textContent = options.label ? `${options.label} -${value}` : `-${value}`;
  el.style.left = `${p.x}px`;
  el.style.top = `${p.y}px`;
  ui.fx.appendChild(el);
  setTimeout(() => el.remove(), 850);
}

function updatePendingVisualHp(card, currentHp) {
  const id = card?.dataset?.id;
  const baseline = id && pendingVisualBaselines?.get(String(id));
  if (baseline) baseline.currentHp = currentHp;
}

function updatePendingVisualMorale(card, morale) {
  const id = card?.dataset?.id;
  const baseline = id && pendingVisualBaselines?.get(String(id));
  if (baseline) baseline.morale = morale;
}

function animateHpOrbLoss(card, amount) {
  if (!card) return;
  const orb = card.querySelector('.stat-orb.hp');
  const value = Math.max(0, Number(amount || 0));
  const maxHp = Math.max(0, Number(card.dataset.maxHp || 0));
  const currentHp = Math.max(0, Number(card.dataset.currentHp || 0));
  if (!orb || !maxHp || !value) return;
  const nextHp = Math.max(0, currentHp - value);
  if (nextHp >= currentHp) return;
  const strong = orb.querySelector('strong');
  const from = Math.max(0, Math.min(1, currentHp / maxHp));
  const to = Math.max(0, Math.min(1, nextHp / maxHp));
  const durationMs = 340;
  const startedAt = performance.now();
  card.dataset.currentHp = String(nextHp);
  updatePendingVisualHp(card, nextHp);
  orb.classList.toggle('hp-empty', nextHp <= 0);
  orb.classList.remove('hp-damaged', 'hp-syncing');
  void orb.offsetWidth;
  orb.classList.add('hp-damaged', 'hp-syncing');
  if (strong) {
    strong.classList.toggle('hp-over', nextHp > maxHp);
    strong.innerHTML = `${nextHp}<small>/${maxHp}</small>`;
    strong.classList.remove('hp-changing');
    void strong.offsetWidth;
    strong.classList.add('hp-changing');
  }
  const step = now => {
    const progress = Math.min(1, (now - startedAt) / durationMs);
    const eased = 1 - Math.pow(1 - progress, 3);
    orb.style.setProperty('--hp-ratio', (from + (to - from) * eased).toFixed(3));
    if (progress < 1) requestAnimationFrame(step);
    else {
      orb.style.setProperty('--hp-ratio', to.toFixed(3));
      window.setTimeout(() => {
        orb.classList.remove('hp-damaged', 'hp-syncing');
        strong?.classList.remove('hp-changing');
      }, 170);
    }
  };
  requestAnimationFrame(step);
}

function moraleFloat(target, amount) {
  const value = Math.max(0, Number(amount || 0));
  if (!value) return;
  const p = center(target), el = document.createElement('b');
  el.className = 'damage-float morale';
  el.textContent = `${i18n.t('moraleDamageShort')} -${value}`;
  el.style.left = `${p.x - 10}px`;
  el.style.top = `${p.y - 70}px`;
  ui.fx.appendChild(el);
  setTimeout(() => el.remove(), 850);
}

function playShieldFormationAnimation(dome) {
  dome.classList.remove('forming'); void dome.offsetWidth; dome.classList.add('forming');
  return wait(620).then(() => dome.classList.remove('forming'));
}
function shieldBlock(target, amount, remaining) {
  const dome = target.dataset.side === 'active' ? ui.activeShieldDome : ui.opponentShieldDome;
  // render() owns shield geometry; hit playback must never re-anchor against moving cards.
  const p = center(target), text = document.createElement('b');
  target.classList.add('shield-block'); setTimeout(() => target.classList.remove('shield-block'), 520);
  text.className = 'shield-float'; text.textContent = `SHIELD -${amount}`; text.style.left = `${p.x}px`; text.style.top = `${p.y}px`; ui.fx.appendChild(text);
  if (remaining <= 0) {
    dome.dataset.pendingBreak = 'true';
    dome.classList.add('pending-break');
    breakShieldDome(dome);
  }
  else {
    dome.classList.remove('hit'); void dome.offsetWidth; dome.classList.add('hit');
    setTimeout(() => dome.classList.remove('hit'), 430);
  }
  setTimeout(() => text.remove(), 920);
}
function animateBpRecoveryToHp(amount, finalState = null) {
  const value = Math.max(0, Number(amount || 0));
  if (!value || !ui.fx) return Promise.resolve();
  const source = ui.activeBpTurnGain?.classList.contains('active') ? ui.activeBpTurnGain : ui.activeBpValue;
  const start = center(source || ui.bpHud);
  const targets = [...ui.activeCards.querySelectorAll('.fighter-card:not(.defeated) .stat-orb.hp')];
  if (!targets.length) {
    clearBpTurnGainDisplay();
    return Promise.resolve();
  }
  const hasMoraleRecoveryTarget = targets.some(target => {
    const card = target.closest('.fighter-card');
    const morale = Math.max(0, Number(card?.dataset.morale || 0));
    const maxMorale = Math.max(0, Number(card?.dataset.maxMorale || 0));
    return maxMorale > 0 && morale < maxMorale;
  });
  if (!hasMoraleRecoveryTarget) {
    clearBpTurnGainDisplay();
    return Promise.resolve();
  }
  clearBpTurnGainDisplay();
  emitSoundThrottled('status.blessing-heal', 360);
  const centerIndex = (targets.length - 1) / 2;
  const staggerMs = 64;
  const flightMs = 1020;
  targets.forEach((target, index) => {
    const end = center(target);
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    const distance = Math.hypot(dx, dy);
    const lift = Math.max(34, Math.min(86, distance * .18));
    const drift = (index - centerIndex) * 7;
    const sidePull = dx >= 0 ? 18 : -18;
    const token = document.createElement('b');
    token.className = 'bp-recovery-fly';
    token.textContent = `+${value}`;
    token.style.left = `${start.x}px`;
    token.style.top = `${start.y}px`;
    token.style.setProperty('--mx', `${dx * .44 + sidePull + drift}px`);
    token.style.setProperty('--my', `${dy * .38 - lift}px`);
    token.style.setProperty('--nx', `${dx * .84 + drift * .32}px`);
    token.style.setProperty('--ny', `${dy * .84 - 10}px`);
    token.style.setProperty('--tx', `${end.x - start.x}px`);
    token.style.setProperty('--ty', `${end.y - start.y}px`);
    token.style.setProperty('--delay', `${index * staggerMs}ms`);
    ui.fx.appendChild(token);
    window.setTimeout(() => {
      const card = target.closest('.fighter-card');
      const finalCharacter = characterByIdData(finalState, card?.dataset?.id);
      animateMoraleRingRecovery(card, value, { finalMorale: finalCharacter?.morale });
      target.classList.remove('bp-recovery-hit');
      void target.offsetWidth;
      target.classList.add('bp-recovery-hit');
    }, 760 + index * staggerMs);
    window.setTimeout(() => {
      token.remove();
      target.classList.remove('bp-recovery-hit');
    }, flightMs + 120 + index * staggerMs);
  });
  const totalMs = flightMs + Math.max(0, targets.length - 1) * staggerMs;
  return wait(totalMs + 120);
}

function animateMoraleRingRecovery(card, amount, options = {}) {
  if (!card) return;
  const ring = card.querySelector('.morale-ring');
  const maxMorale = Math.max(0, Number(card.dataset.maxMorale || 0));
  const currentMorale = Math.max(0, Number(card.dataset.morale || 0));
  const recovery = Math.max(0, Number(amount || 0));
  if (!ring || !maxMorale || !recovery) return;
  const finalMorale = options.finalMorale === undefined ? null : Number(options.finalMorale);
  const nextMorale = Number.isFinite(finalMorale)
    ? Math.max(0, Math.min(maxMorale, finalMorale))
    : Math.min(maxMorale, currentMorale + recovery);
  if (nextMorale <= currentMorale) return;
  const from = Math.max(0, Math.min(1, currentMorale / maxMorale));
  const to = Math.max(0, Math.min(1, nextMorale / maxMorale));
  const durationMs = 360;
  const startedAt = performance.now();
  card.dataset.morale = String(nextMorale);
  updatePendingVisualMorale(card, nextMorale);
  ring.title = `Morale ${nextMorale}/${maxMorale}`;
  ring.classList.remove('morale-recovering');
  void ring.offsetWidth;
  ring.classList.add('morale-recovering');
  const step = now => {
    const progress = Math.min(1, (now - startedAt) / durationMs);
    const eased = 1 - Math.pow(1 - progress, 3);
    card.style.setProperty('--morale-ratio', (from + (to - from) * eased).toFixed(3));
    if (progress < 1) requestAnimationFrame(step);
    else {
      card.style.setProperty('--morale-ratio', to.toFixed(3));
      window.setTimeout(() => ring.classList.remove('morale-recovering'), 180);
    }
  };
  requestAnimationFrame(step);
}

function animateMoraleRingLoss(card, amount) {
  if (!card) return;
  const ring = card.querySelector('.morale-ring');
  const maxMorale = Math.max(0, Number(card.dataset.maxMorale || 0));
  const currentMorale = Math.max(0, Number(card.dataset.morale || 0));
  const loss = Math.max(0, Number(amount || 0));
  if (!ring || !maxMorale || !loss) return;
  const nextMorale = Math.max(0, currentMorale - loss);
  if (nextMorale >= currentMorale) return;
  const from = Math.max(0, Math.min(1, currentMorale / maxMorale));
  const to = Math.max(0, Math.min(1, nextMorale / maxMorale));
  const durationMs = 320;
  const startedAt = performance.now();
  card.dataset.morale = String(nextMorale);
  ring.title = `Morale ${nextMorale}/${maxMorale}`;
  ring.classList.remove('morale-damaged');
  void ring.offsetWidth;
  ring.classList.add('morale-damaged');
  const step = now => {
    const progress = Math.min(1, (now - startedAt) / durationMs);
    const eased = 1 - Math.pow(1 - progress, 3);
    card.style.setProperty('--morale-ratio', (from + (to - from) * eased).toFixed(3));
    if (progress < 1) requestAnimationFrame(step);
    else {
      card.style.setProperty('--morale-ratio', to.toFixed(3));
      window.setTimeout(() => ring.classList.remove('morale-damaged'), 180);
    }
  };
  requestAnimationFrame(step);
}
function breakShieldDome(dome) {
  const wasActive = dome.classList.contains('active');
  const shouldHideAfterBreak = dome.dataset.pendingBreak === 'true' || !wasActive;
  // State polling/rendering may already contain the post-hit shield value. Briefly
  // restore the visual shell so both the acting and observing client still see it break.
  if (!wasActive) dome.classList.add('active');
  const rect = stageRect(dome), particles = document.createElement('i');
  dome.classList.add('breaking');
  particles.className = 'shield-particle-field';
  particles.style.left = `${rect.left}px`; particles.style.top = `${rect.top}px`;
  particles.style.width = `${rect.width}px`; particles.style.height = `${rect.height}px`;
  particles.innerHTML = Array.from({length:28}, (_, index) => {
    const x = (index / 27) * 100;
    const tx = ((index % 7) - 3) * 14;
    const ty = 28 + (index % 5) * 12;
    return `<i style="--x:${x}%;--tx:${tx}px;--ty:${ty}px;--r:${index * 47}deg;--delay:${(index % 4) * 28}ms"></i>`;
  }).join('');
  ui.fx.appendChild(particles);
  setTimeout(() => {
    particles.remove(); dome.classList.remove('breaking', 'pending-break');
    if (shouldHideAfterBreak) dome.classList.remove('active', 'reinforced');
    delete dome.dataset.pendingBreak;
    delete dome.dataset.visualShieldValue;
  }, 900);
}
function center(element) { const r = stageRect(element); return { x: r.left + r.width/2, y: r.top + r.height/2 }; }

function renderLog() {
  ui.log.innerHTML = [...game.log].reverse().map(entry => `<div class="log-entry ${entry.tone}"><b>T${String(entry.turn).padStart(2,'0')}</b>${art.icon(art.forLogTone(entry.tone), { size: 'xs', label: entry.tone })}<span>${escapeHtml(i18n.message(entry.message))}</span></div>`).join('');
  art.hydrate(ui.log);
}
function renderGameOver() {
  const over = game.phase === 'Finished';
  ui.gameOver.classList.toggle('open', over);
  ui.gameOver.setAttribute('aria-hidden', String(!over));
  if (!over) return;
  const winner = game.players.find(p => p.id === game.winnerPlayerId);
  ui.resultTitle.textContent = game.isDraw ? 'DRAW' : 'VICTORY';
  ui.resultCopy.textContent = game.isDraw ? i18n.t('drawCopy') : i18n.t('victoryCopy', { name: i18n.playerName(winner?.name || '') });
  if (resultAudioGameId !== game.gameId) {
    resultAudioGameId = game.gameId;
    sound.emit(game.isDraw ? 'game.draw' : game.winnerPlayerId === game.viewerPlayerId ? 'game.victory' : 'game.defeat');
    setTimeout(() => sound.emit('game.result-panel'), 180);
  }
}
function updateInstruction() {
  if (game?.heroDraft) {
    ui.instruction.innerHTML = `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t(game.heroDraft.canChoose ? 'heroDraftInstruction' : 'heroDraftWaiting'))}</strong><small>${escapeHtml(i18n.t('heroDraftInstructionHint'))}</small></div>`;
    return;
  }
  if (game?.pendingRoleActionUpgrade?.canChoose) {
    ui.instruction.innerHTML = `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('roleActionUpgradeInstruction'))}</strong><small>${escapeHtml(i18n.t('roleActionUpgradeHint'))}</small></div>`;
    return;
  }
  if (game?.pendingRelicReward?.canChoose) {
    ui.instruction.innerHTML = `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('rewardRelicInstruction'))}</strong><small>${escapeHtml(i18n.t('rewardRelicHint'))}</small></div>`;
    return;
  }
  if (pendingRoleAction) {
    const action = i18n.roleAction(pendingRoleAction.roleActionId);
    ui.instruction.innerHTML = `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('roleActionTargetInstruction', { name: action.name }))}</strong><small>${escapeHtml(i18n.t(roleActionTargetInstructionKey(pendingRoleAction.targetKinds)))}</small></div>`;
    return;
  }
  if (pendingDeputy) {
    ui.instruction.innerHTML = `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('deputyTargetInstruction'))}</strong><small>${escapeHtml(i18n.t('deputyTargetHint'))}</small></div>`;
    return;
  }
  const card = findCard(selectedAttacker);
  const name = card ? i18n.characterName(card.key) : '';
  ui.instruction.innerHTML = card ? `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('currentSelected', { name }))}</strong><small>${i18n.t('selectUpperEnemy')}</small></div>` : `<span class="instruction-icon">◆</span><div><strong>${i18n.t('selectCard')}</strong><small>${i18n.t('selectTarget')}</small></div>`;
}
function findCard(id) { return game?.players.flatMap(player => player.characters).find(card => card.id === id); }
function closePreview() { ui.preview.classList.remove('open'); ui.preview.setAttribute('aria-hidden', 'true'); }

function cancelAttackPreview() {
  if (ui.preview.classList.contains('open')) sound.emit('ui.preview-cancel');
  selectedAttacker = null;
  selectedDefender = null;
  inspectedCardId = null;
  hideAttackArrow();
  closePreview();
  syncCombatInteractionUi({ syncInspector: false });
}
function showToast(value) { ui.toast.textContent = typeof value === 'string' ? value : i18n.message(value); ui.toast.classList.add('show'); clearTimeout(showToast.timer); showToast.timer = setTimeout(() => ui.toast.classList.remove('show'), 2200); }
function escapeHtml(value='') { return String(value).replace(/[&<>'"]/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[char])); }
function truncateCardText(value='', limit=28) { const chars = [...String(value)]; return chars.length <= limit ? value : `${chars.slice(0, limit - 1).join('')}…`; }
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
const DEAL_CARD_STAGGER_MS = 370;
const DEAL_CARD_TRAVEL_MS = 680;
const DEAL_CLOSE_DELAY_MS = 450;

function setAudioMenuOpen(open) {
  ui.audioMenu?.classList.toggle('open', open);
  ui.audioPanel?.setAttribute('aria-hidden', String(!open));
  ui.soundToggle?.setAttribute('aria-expanded', String(open));
}

function updateAudioGroupControls(group, settings) {
  const enabled = settings[group].enabled;
  const percent = Math.round(settings[group].volume * 100);
  const controls = {
    bgm: { toggle: ui.bgmToggle, slider: ui.bgmVolume, value: ui.bgmVolumeValue },
    sfx: { toggle: ui.sfxToggle, slider: ui.sfxVolume, value: ui.sfxVolumeValue },
    voice: { toggle: ui.voiceToggle, slider: ui.voiceVolume, value: ui.voiceVolumeValue }
  }[group];
  if (!controls?.toggle || !controls.slider || !controls.value) return;
  const { toggle, slider, value } = controls;
  toggle.textContent = enabled ? 'ON' : 'OFF';
  toggle.classList.toggle('off', !enabled);
  toggle.setAttribute('aria-pressed', String(enabled));
  toggle.closest('.audio-control-row')?.classList.toggle('off', !enabled);
  slider.value = String(percent);
  value.textContent = `${percent}%`;
}

function renderBgmTrackSelect() {
  if (!ui.bgmTrackSelect || !sound.getBgmTracks) return;
  const tracks = sound.getBgmTracks();
  const currentTrackId = sound.getCurrentBgmTrackId?.() || '';
  const optionsMarkup = tracks.map(track =>
    `<option value="${escapeHtml(track.id)}">${escapeHtml(track.title)}</option>`).join('');
  if (ui.bgmTrackSelect.dataset.optionsMarkup !== optionsMarkup) {
    ui.bgmTrackSelect.innerHTML = optionsMarkup;
    ui.bgmTrackSelect.dataset.optionsMarkup = optionsMarkup;
  }
  ui.bgmTrackSelect.disabled = tracks.length === 0;
  ui.bgmTrackSelect.value = currentTrackId;
}

function renderAudioControls() {
  if (!ui.soundToggle || !sound.getSettings) return;
  const settings = sound.getSettings();
  updateAudioGroupControls('bgm', settings);
  updateAudioGroupControls('sfx', settings);
  updateAudioGroupControls('voice', settings);
  renderBgmTrackSelect();
  const hasAnySound = settings.bgm.enabled || settings.sfx.enabled || settings.voice.enabled;
  ui.soundToggle.classList.toggle('muted', !hasAnySound);
  ui.soundToggle.querySelector('span').textContent = hasAnySound ? i18n.t('audio') : i18n.t('muted');
}

function toggleAudioGroup(group) {
  sound.unlock();
  const settings = sound.getSettings();
  const enabled = !settings[group].enabled;
  if (enabled) sound.setGroupEnabled(group, true);
  if ((group === 'sfx' || group === 'voice') && enabled) sound.emit('ui.audio-toggle');
  if (group === 'bgm') sound.emit('ui.audio-toggle');
  if (!enabled) sound.setGroupEnabled(group, false);
  renderAudioControls();
}

function setAudioGroupVolume(group, value, preview = false) {
  sound.unlock();
  sound.setGroupVolume(group, Number(value) / 100);
  if (preview && group === 'sfx' && sound.getSettings().sfx.enabled) sound.emit('ui.audio-toggle');
  if (preview && group === 'voice' && sound.getSettings().voice.enabled) emitVoice('select', 'princess');
  renderAudioControls();
}

function selectBgmTrack(trackId) {
  sound.unlock();
  if (sound.setBgmTrack?.(trackId)) sound.emit('ui.audio-toggle');
  renderAudioControls();
}

function applyStaticTranslations() {
  document.querySelectorAll('[data-i18n]').forEach(element => { element.textContent = i18n.t(element.dataset.i18n); });
  document.querySelectorAll('[data-i18n-aria]').forEach(element => { element.setAttribute('aria-label', i18n.t(element.dataset.i18nAria)); });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => { element.setAttribute('placeholder', i18n.t(element.dataset.i18nPlaceholder)); });
  ui.languageToggle.querySelector('span').textContent = i18n.t('languageButton');
  ui.languageToggle.setAttribute('aria-label', i18n.t('languageAria'));
  renderAudioControls();
  document.querySelector('#new-game').setAttribute('aria-label', i18n.t('newGame'));
  document.querySelector('#opponent-zone').setAttribute('aria-label', i18n.t('opponent'));
  document.querySelector('#active-zone').setAttribute('aria-label', i18n.t('activeSide'));
  document.querySelector('.command-deck').setAttribute('aria-label', i18n.t('actionPanel'));
  ui.opponentShield.setAttribute('aria-label', i18n.t('sharedShieldEnemy'));
  ui.activeShield.setAttribute('aria-label', i18n.t('sharedShieldAlly'));
  if (ui.dealStatus.textContent === 'SHUFFLING') ui.dealCaption.textContent = i18n.t('dealShuffle');
  if (ui.dealStatus.textContent === 'TOUCH THE DECK') ui.dealCaption.textContent = i18n.t(sessionMode === 'online' ? 'dealTouchOnline' : 'dealTouch');
  if (ui.dealStatus.textContent === 'DEALING') ui.dealCaption.textContent = i18n.t('dealDealing');
}

function showOnlineLobby() {
  sessionMode = 'online';
  ui.modeSelect.hidden = true;
  ui.onlineLobby.hidden = false;
  ui.lobbyBack.hidden = Boolean(room || playerToken);
}

function showModeSelect() {
  if (room || playerToken) return;
  sessionMode = null;
  ui.onlineLobby.hidden = true;
  ui.modeSelect.hidden = false;
}

function saveOnlineIdentity(identity) {
  playerToken = identity.token;
  localStorage.setItem('tpf-online-player-token', playerToken);
}

function clearOnlineIdentity() {
  localStorage.removeItem('tpf-online-player-token');
  playerToken = '';
  room = null;
}

async function createRoom() {
  sound.unlock();
  try {
    const payload = await api('/api/online/room/create', { method:'POST', body:JSON.stringify({ playerName:ui.playerName.value }) });
    saveOnlineIdentity(payload.data);
    sound.emit('online.room-created');
    history.replaceState(null, '', location.pathname);
    await refreshRoom();
    startPolling();
  } catch (error) { sound.emit('online.request-failed'); showToast(error.message); }
}

async function joinRoom() {
  sound.unlock();
  try {
    const payload = await api('/api/online/room/join', { method:'POST', body:JSON.stringify({ code:ui.roomCode.value, playerName:ui.playerName.value }) });
    saveOnlineIdentity(payload.data);
    sound.emit('online.room-joined');
    history.replaceState(null, '', location.pathname);
    await refreshRoom();
    startPolling();
  } catch (error) { sound.emit('online.request-failed'); showToast(error.message); }
}

async function refreshRoom() {
  if (!playerToken) return;
  const previousPlayerCount = room?.players?.length || 0;
  const payload = await api('/api/online/room');
  room = payload.data;
  if (previousPlayerCount === 1 && room.players.length === 2) sound.emit('online.opponent-joined');
  showOnlineLobby();
  renderRoom();
  if (room.started && !hasStarted) await enterOnlineGame();
}

function renderRoom() {
  if (!room) return;
  ui.lobbyEntry.hidden = true;
  ui.lobbyWaiting.hidden = false;
  ui.lobbyBack.hidden = true;
  ui.lobbyRole.textContent = `PLAYER ${room.seat} / ${i18n.t(room.isHost ? 'host' : 'guest')}`;
  const ready = room.players.length === 2 && room.players.every(player => player.name);
  ui.lobbyStatus.textContent = ready ? i18n.t(room.isHost ? 'playerJoinedHost' : 'joinedWaitingHost') : i18n.t('waitingPlayer');
  ui.lobbyPlayers.innerHTML = room.players.map(player => `<div class="lobby-player ${player.isConnected ? 'connected' : ''}">PLAYER ${player.seat}<b>${escapeHtml(player.name || i18n.t('waitingSeat'))}</b></div>`).join('');
  ui.joinLink.value = `${location.origin}${location.pathname}?join=${room.roomCode}`;
  document.querySelector('#join-link-row').hidden = !room.isHost;
  ui.startMatch.hidden = !room.isHost;
  ui.startMatch.disabled = !ready;
}

async function startOnlineMatch() {
  if (hasStarted || busy || !room?.isHost) return;
  busy = true;
  sound.emit('game.start');
  sound.unlock({ primeUnrequested: false });
  try {
    const payload = await gameApi('/game/new', { method:'POST', body:'{}' });
    room.started = true;
    room.dealStarted = false;
    await enterOnlineGame(payload.data);
  } catch (error) {
    showToast(error.message);
  } finally { busy = false; }
}

async function enterOnlineGame(initialGame = null) {
  if (hasStarted) return;
  sessionMode = 'online';
  hasStarted = true;
  busy = true;
  sound.emit('game.start');
  try {
    if (initialGame) {
      game = initialGame;
      lastGameJson = JSON.stringify(game);
      lastApSnapshot = null;
      lastBpGainSnapshot = null;
      resetEventCursor(game);
    } else {
      await loadGame();
    }
    selectedAttacker = null; selectedDefender = null; inspectedCardId = null; closePreview();
    dealing = true; render(); openDealLayer();
    ui.startScreen.classList.add('leaving');
    await wait(470);
    await playDealSequence(true);
  } catch (error) {
    hasStarted = false; dealing = false;
    ui.startScreen.classList.remove('leaving'); closeDealLayer();
    showToast(error.message);
  } finally { busy = false; }
}

async function pollOnlineState() {
  if (sessionMode !== 'online' || !playerToken || busy || eventPlayback) return;
  eventPlayback = true;
  let changedState = false;
  try {
    await refreshRoom();
    // An open forecast must never pause server synchronization. If the remote
    // player advances the game, the changed state below closes the stale panel.
    if (!room?.started || dealing || !hasStarted) return;
    const payload = await gameApi('/game/state');
    const json = JSON.stringify(payload.data);
    if (json === lastGameJson) return;
    changedState = true;
    const previousGameId = game?.gameId;
    const oldActive = game?.activePlayerId;
    const isNewGame = Boolean(previousGameId && previousGameId !== payload.data.gameId);
    if (isNewGame) dealing = true;
    pendingVisualBaselines = isNewGame ? null : captureCharacterVisualBaselines(game);
    game = payload.data; lastGameJson = json;
    if (isNewGame) {
      lastApSnapshot = null;
      lastBpGainSnapshot = null;
    }
    if (oldActive && oldActive !== game.activePlayerId)
      turnCurtainLock = true;
    selectedAttacker = null; selectedDefender = null; inspectedCardId = null; pendingRoleAction = null; closePreview(); render();
    if (isNewGame) {
      resetEventCursor(game);
      sound.emit('game.restart');
      openDealLayer();
      await playDealSequence(true);
      return;
    }
    if (oldActive && oldActive !== game.activePlayerId) {
      await playTurnCurtain(game.activePlayerName, true);
    }
    await playNewLogEvents(game);
  } catch (error) {
    if (error.status === 401) {
      clearOnlineIdentity();
      clearInterval(pollTimer); pollTimer = null;
    }
    showToast(error.message);
  } finally {
    eventPlayback = false;
    if (changedState && game) render();
  }
}

function startPolling() {
  if (!pollTimer) pollTimer = setInterval(pollOnlineState, 500);
}

async function bootstrapModes() {
  const joinCode = new URLSearchParams(location.search).get('join');
  if (joinCode) {
    showOnlineLobby();
    ui.roomCode.value = joinCode.toUpperCase();
  }
  if (!playerToken) return;
  sessionMode = 'online';
  showOnlineLobby();
  try { await refreshRoom(); startPolling(); }
  catch { clearOnlineIdentity(); if (!joinCode) showModeSelect(); }
}

document.querySelector('#confirm-attack').addEventListener('click', executeAttack);
ui.cancelPreview.addEventListener('click', cancelAttackPreview);
ui.endTurn.addEventListener('click', endTurn);
ui.shieldButton.addEventListener('click', deployShield);
ui.shieldButton.addEventListener('mouseenter', showShieldInspector);
ui.shieldButton.addEventListener('mouseleave', hideShieldInspector);
ui.shieldButton.addEventListener('focus', showShieldInspector);
ui.shieldButton.addEventListener('blur', hideShieldInspector);
[ui.activeShieldDome, ui.opponentShieldDome].forEach(dome => {
  dome?.addEventListener('mouseenter', () => showShieldDomeInspector(dome));
  dome?.addEventListener('mouseleave', hideShieldInspector);
  dome?.addEventListener('focus', () => showShieldDomeInspector(dome));
  dome?.addEventListener('blur', hideShieldInspector);
});
ui.bpHud?.addEventListener('mouseenter', showBpInspector);
ui.bpHud?.addEventListener('mouseleave', hideBpInspector);
ui.bpHud?.addEventListener('focusin', showBpInspector);
ui.bpHud?.addEventListener('focusout', hideBpInspector);
ui.relicOverviewButton?.addEventListener('click', event => {
  if (!isTouchMode()) return;
  event.stopPropagation();
  setRelicOverviewOpen(!ui.relicOverview?.classList.contains('expanded'));
});
ui.rewardOptions.addEventListener('click', event => {
  const card = event.target instanceof Element ? event.target.closest('.reward-card[data-reward-instance]') : null;
  if (!card || card.disabled) return;
  selectReward(card.dataset.rewardInstance);
});
ui.heroDraftOptions?.addEventListener('click', event => {
  const confirm = event.target instanceof Element ? event.target.closest('.hero-draft-recruit-confirm[data-hero-key]') : null;
  if (confirm) {
    selectHeroDraft(confirm.dataset.heroKey);
    return;
  }
  const card = event.target instanceof Element ? event.target.closest('.hero-draft-card[data-hero-key]') : null;
  if (!card || card.disabled) return;
  const key = card.dataset.heroKey;
  const draft = game?.heroDraft;
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  if (isSoldierDraft) {
    const shouldHideTouchInspector = isTouchMode()
      && ui.inspector.classList.contains('open')
      && ui.inspector.classList.contains('draft-inspector-only')
      && ui.inspector.dataset.draftKey === key;
    soldierUpgradeKey = null;
    if (selectedHeroDraftKeys.includes(key)) {
      selectedHeroDraftKeys = selectedHeroDraftKeys.filter(item => item !== key);
      card.classList.add('hover-suppressed');
    } else {
      const max = Number(draft?.maxSelections || 1);
      selectedHeroDraftKeys = max <= 1 ? [key] : [...selectedHeroDraftKeys, key].slice(0, max);
      card.classList.remove('hover-suppressed');
    }
    selectedHeroDraftKey = null;
    if (isTouchMode()) {
      if (shouldHideTouchInspector) hideCharacterInspector();
      else showHeroDraftCandidateInspector(key, card);
    } else {
      hideCharacterInspector();
    }
  } else {
    selectedHeroDraftKey = key;
    card.classList.remove('hover-suppressed');
  }
  sound.emit('ui.card-select');
  syncHeroDraftSelectionUi();
});
ui.heroDraftOptions?.addEventListener('animationend', event => {
  if (event.animationName === 'rewardCardEnter' && event.target instanceof Element) {
    event.target.classList.add('hero-draft-entered');
  }
});
ui.heroDraftOptions?.addEventListener('mouseout', event => {
  const card = event.target instanceof Element ? event.target.closest('.hero-draft-card.hover-suppressed') : null;
  if (card && !card.contains(event.relatedTarget)) card.classList.remove('hover-suppressed');

  const draftCard = event.target instanceof Element ? event.target.closest('.hero-draft-card[data-hero-key]') : null;
  if (!draftCard || draftCard.contains(event.relatedTarget)) return;
  const draft = game?.heroDraft;
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  if (isTouchMode() && isSoldierDraft) return;
  if (isSoldierDraft) hideCharacterInspector();
});
ui.heroDraftOptions?.addEventListener('mouseover', event => {
  const card = event.target instanceof Element ? event.target.closest('.hero-draft-card[data-hero-key]') : null;
  const draft = game?.heroDraft;
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  if (isTouchMode() && isSoldierDraft) return;
  if (card && (isSoldierDraft || selectedHeroDraftKey === card.dataset.heroKey))
    showHeroDraftCandidateInspector(card.dataset.heroKey, card);
});
ui.heroDraftOptions?.addEventListener('focusin', event => {
  const card = event.target instanceof Element ? event.target.closest('.hero-draft-card[data-hero-key]') : null;
  const draft = game?.heroDraft;
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  if (isTouchMode() && isSoldierDraft) return;
  if (card && (isSoldierDraft || selectedHeroDraftKey === card.dataset.heroKey))
    showHeroDraftCandidateInspector(card.dataset.heroKey, card);
});
ui.heroDraftOptions?.addEventListener('focusout', event => {
  const card = event.target instanceof Element ? event.target.closest('.hero-draft-card[data-hero-key]') : null;
  const next = event.relatedTarget instanceof Element ? event.relatedTarget.closest('.hero-draft-card[data-hero-key]') : null;
  const draft = game?.heroDraft;
  const isSoldierDraft = draft?.kind === 'SoldierOpening' || draft?.kind === 'SoldierRecruit';
  if (isTouchMode() && isSoldierDraft) return;
  if (card && isSoldierDraft && next !== card) hideCharacterInspector();
});
ui.heroDraftOptions?.addEventListener('mouseleave', () => {
  if (isTouchMode()) return;
  if (!selectedHeroDraftKey) hideCharacterInspector();
});
ui.heroDraftReset?.addEventListener('click', resetHeroDraft);
ui.heroDraftBack?.addEventListener('click', returnToRewardWindow);
ui.rewardChildBack?.addEventListener('click', returnToRewardWindow);
ui.rewardInlineBack?.addEventListener('click', returnToRewardWindow);
ui.heroDraftConfirm?.addEventListener('click', selectSoldierDraft);
ui.heroDraftUpgrade?.addEventListener('click', beginSoldierUpgradeTargeting);
ui.rewardReset.addEventListener('click', resetRewardWindow);
ui.rewardSkip.addEventListener('click', skipRewardWindow);
ui.deputyConfirmOk?.addEventListener('click', confirmDeputyAssignment);
ui.deputyConfirmCancel?.addEventListener('click', () => {
  sound.emit('ui.cancel');
  closeDeputyConfirm();
});
ui.deputyConfirm?.addEventListener('click', event => {
  if (event.target === ui.deputyConfirm || event.target?.classList?.contains('deputy-confirm-backdrop')) {
    sound.emit('ui.cancel');
    closeDeputyConfirm();
  }
});
ui.inspector.addEventListener('click', event => {
  const deputyButton = event.target instanceof Element ? event.target.closest('.deputy-assign-button[data-soldier-id]') : null;
  if (deputyButton) {
    event.preventDefault();
    event.stopPropagation();
    if (!deputyButton.disabled) beginDeputyTargeting(deputyButton.dataset.soldierId);
    return;
  }
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id]') : null;
  if (!button || button.disabled) return;
  const characterId = button.dataset.characterId;
  const roleActionId = button.dataset.roleActionId;
  if (!characterId || !roleActionId) return;

  if (button.classList.contains('choice')) {
    selectRoleActionUpgrade(characterId, roleActionId);
    return;
  }

  if (isTouchMode()) {
    showRoleActionInspector(button);
    return;
  }

  const targets = String(button.dataset.roleActionTargets || '').split(',').filter(Boolean);
  const isTargetedRoleAction = button.dataset.roleActionMode === 'Targeted';
  if (isTargetedRoleAction && targets.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind))) {
    beginRoleActionTargeting(characterId, roleActionId, targets);
    sound.emit('ui.card-select');
    showToast(i18n.t(roleActionTargetInstructionKey(targets)));
    return;
  }

  useRoleAction(characterId, roleActionId);
});
ui.inspector.addEventListener('pointerdown', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id][draggable="true"]') : null;
  if (button) beginTouchRoleAction(button, event);
});
ui.inspector.addEventListener('mouseover', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id]') : null;
  if (button) showRoleActionInspector(button);
});
ui.inspector.addEventListener('mouseout', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id]') : null;
  if (!button) return;
  const next = event.relatedTarget instanceof Element ? event.relatedTarget.closest('.role-action-button[data-role-action-id]') : null;
  if (next === button) return;
  if (event.relatedTarget instanceof Element && ui.roleActionInspector?.contains(event.relatedTarget)) return;
  hideRoleActionInspector();
});
ui.roleActionInspector?.addEventListener('mouseleave', hideRoleActionInspector);
ui.roleActionInspector?.addEventListener('click', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-detail-use[data-role-action-id]') : null;
  if (!button) return;
  event.preventDefault();
  event.stopPropagation();
  const characterId = button.dataset.characterId;
  const roleActionId = button.dataset.roleActionId;
  if (!characterId || !roleActionId) return;
  const targets = String(button.dataset.roleActionTargets || '').split(',').filter(Boolean);
  const isTargetedRoleAction = button.dataset.roleActionMode === 'Targeted';
  if (isTargetedRoleAction && targets.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind))) {
    beginRoleActionTargeting(characterId, roleActionId, targets);
    sound.emit('ui.card-select');
    showToast(i18n.t(roleActionTargetInstructionKey(targets)));
    return;
  }
  useRoleAction(characterId, roleActionId);
});
ui.inspector.addEventListener('focusin', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id]') : null;
  if (button) showRoleActionInspector(button);
});
ui.inspector.addEventListener('focusout', event => {
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id]') : null;
  if (button) hideRoleActionInspector();
});
ui.inspector.addEventListener('dragstart', event => {
  hideRoleActionInspector();
  const button = event.target instanceof Element ? event.target.closest('.role-action-button[data-role-action-id][draggable="true"]') : null;
  if (!button || button.disabled || button.classList.contains('choice')) {
    event.preventDefault();
    return;
  }
  const characterId = button.dataset.characterId;
  const roleActionId = button.dataset.roleActionId;
  if (!characterId || !roleActionId) {
    event.preventDefault();
    return;
  }
  const targets = String(button.dataset.roleActionTargets || '').split(',').filter(Boolean);
  const isTargetedRoleAction = button.dataset.roleActionMode === 'Targeted';
  if (!isTargetedRoleAction || !targets.some(kind => ['SelfCard', 'AllyCard', 'EnemyCard'].includes(kind))) {
    event.preventDefault();
    return;
  }
  beginRoleActionTargeting(characterId, roleActionId, targets, { renderFirst: false, startArrow: false });
  roleActionDragActive = true;
  event.dataTransfer.setData('text/plain', `${characterId}:${roleActionId}`);
  event.dataTransfer.effectAllowed = 'move';
  event.dataTransfer.setDragImage(dragGhost, 0, 0);
  startAttackArrow(characterElementById(characterId) || button, 'role-action');
  ui.app.classList.add('dragging-attack');
  document.body.classList.add('dragging-attack');
  updateInstruction();
});
ui.inspector.addEventListener('dragend', () => {
  document.querySelectorAll('.drop-ready').forEach(element => element.classList.remove('drop-ready'));
  ui.app.classList.remove('dragging-attack');
  document.body.classList.remove('dragging-attack');
  if (roleActionDragActive) cancelAiming();
  else hideAttackArrow();
});
document.querySelector('#new-game').addEventListener('click', newGame);
document.querySelector('#play-again').addEventListener('click', newGame);
ui.startTrigger.addEventListener('click', startLocalGame);
ui.startTest?.addEventListener('click', startTestGame);
ui.startAi?.addEventListener('click', startAiGame);
document.querySelector('#open-online').addEventListener('click', showOnlineLobby);
ui.lobbyBack.addEventListener('click', showModeSelect);
document.querySelector('#create-room').addEventListener('click', createRoom);
document.querySelector('#join-room').addEventListener('click', joinRoom);
ui.startMatch.addEventListener('click', startOnlineMatch);
document.querySelector('#copy-link').addEventListener('click', async () => {
  try {
    await navigator.clipboard.writeText(ui.joinLink.value);
    sound.emit('online.link-copied');
    showToast(i18n.t('inviteCopied'));
  } catch {
    ui.joinLink.select();
    showToast(i18n.t('copyLink'));
  }
});
ui.languageToggle.addEventListener('click', () => {
  sound.emit('ui.language-toggle');
  i18n.toggle();
  applyStaticTranslations();
  hideCharacterInspector(); hideShieldInspector(); hideBpInspector();
  if (room) renderRoom();
  if (game) render();
  if (preview && selectedAttacker && selectedDefender && ui.preview.classList.contains('open')) renderPreview();
});
ui.soundToggle.addEventListener('click', event => {
  event.stopPropagation();
  sound.unlock();
  sound.emit('ui.audio-toggle');
  setAudioMenuOpen(!ui.audioMenu.classList.contains('open'));
});
ui.audioPanel.addEventListener('click', event => event.stopPropagation());
ui.bgmToggle.addEventListener('click', () => toggleAudioGroup('bgm'));
ui.sfxToggle.addEventListener('click', () => toggleAudioGroup('sfx'));
ui.voiceToggle.addEventListener('click', () => toggleAudioGroup('voice'));
ui.bgmVolume.addEventListener('input', () => setAudioGroupVolume('bgm', ui.bgmVolume.value));
ui.sfxVolume.addEventListener('input', () => setAudioGroupVolume('sfx', ui.sfxVolume.value));
ui.voiceVolume.addEventListener('input', () => setAudioGroupVolume('voice', ui.voiceVolume.value));
ui.bgmTrackSelect.addEventListener('change', () => selectBgmTrack(ui.bgmTrackSelect.value));
ui.sfxVolume.addEventListener('change', () => setAudioGroupVolume('sfx', ui.sfxVolume.value, true));
ui.voiceVolume.addEventListener('change', () => setAudioGroupVolume('voice', ui.voiceVolume.value, true));
document.addEventListener('click', event => {
  if (ui.audioMenu?.contains(event.target)) return;
  setAudioMenuOpen(false);
});
document.addEventListener('click', event => {
  if (!isTouchMode() || !ui.relicOverview?.classList.contains('expanded')) return;
  if (event.target instanceof Element && ui.relicOverview.contains(event.target)) return;
  setRelicOverviewOpen(false);
});
document.addEventListener('click', event => {
  if ((!selectedAttacker && !inspectedCardId && !pendingRoleAction) || busy || dealing) return;
  const interactive = event.target instanceof Element
    ? event.target.closest('.fighter-card,.preview-panel,.reward-window,.hero-draft-window,.deputy-confirm-modal,.command-deck,.topbar,.battle-log-shell,.action-point-hud,.battle-point-hud,.relic-overview,.character-inspector,.status-inspector,.shield-inspector,.bp-inspector,.game-over,.start-screen,.deal-sequence')
    : null;
  if (interactive) return;
  cancelAiming();
});
document.querySelector('#toggle-log').addEventListener('click', () => { sound.emit('ui.panel-toggle'); ui.log.classList.toggle('open'); });
document.addEventListener('mousemove', event => {
  if (!pendingRoleAction || roleActionDragActive || !dragArrowOrigin) return;
  const selector = roleActionCardTargetSelector(pendingRoleAction);
  const target = event.target instanceof Element && selector ? event.target.closest(selector) : null;
  const point = clientToStage(event.clientX, event.clientY);
  updateAttackArrow(point.x, point.y, target);
});
document.addEventListener('pointermove', updateTouchDrag, { passive: false });
document.addEventListener('pointerup', event => endTouchDrag(event), { passive: false });
document.addEventListener('pointercancel', event => endTouchDrag(event, true), { passive: false });
document.addEventListener('dragover', event => {
  if (!dragArrowOrigin) return;
  const selector = roleActionDragActive
    ? roleActionCardTargetSelector(pendingRoleAction)
    : '.fighter-card[data-side="opponent"]:not(.defeated):not(.deploying)';
  const target = targetAtClientPoint(event.clientX, event.clientY, selector, candidate =>
    !roleActionDragActive || canRoleActionTargetCard(pendingRoleAction, candidate));
  if (target) {
    event.preventDefault();
    document.querySelectorAll('.drop-ready').forEach(element => {
      if (element !== target) element.classList.remove('drop-ready');
    });
    target.classList.add('drop-ready');
  }
  const point = clientToStage(event.clientX, event.clientY);
  updateAttackArrow(point.x, point.y, target);
});
document.addEventListener('drop', event => {
  if (event.defaultPrevented || !dragArrowOrigin) return;
  const selector = roleActionDragActive
    ? roleActionCardTargetSelector(pendingRoleAction)
    : '.fighter-card[data-side="opponent"]:not(.defeated):not(.deploying)';
  const target = targetAtClientPoint(event.clientX, event.clientY, selector, candidate =>
    !roleActionDragActive || canRoleActionTargetCard(pendingRoleAction, candidate));
  if (!target) return;
  event.preventDefault();
  target.classList.remove('drop-ready');
  if (roleActionDragActive && pendingRoleAction) {
    const action = pendingRoleAction;
    finishAttackArrow(target);
    useRoleAction(action.characterId, action.roleActionId, target.dataset.id);
    return;
  }
  if (!roleActionDragActive && selectedAttacker) {
    finishAttackArrow(target);
    chooseDefender(target.dataset.id);
  }
});
window.addEventListener('resize', () => {
  resizeGameStage();
  if (!game || dealing) return;
  const viewer = game.players.find(player => player.id === game.viewerPlayerId);
  const opponent = game.players.find(player => player.id !== game.viewerPlayerId);
  renderPersistentShield(ui.activeShieldDome, ui.activeCards, viewer, false, true);
  renderPersistentShield(ui.opponentShieldDome, ui.opponentCards, opponent, false, true);
});
window.visualViewport?.addEventListener('resize', () => {
  resizeGameStage();
});
document.addEventListener('keydown', event => {
  if (!hasStarted) return;
  if (event.key === 'Escape') {
    setAudioMenuOpen(false);
    if (ui.preview.classList.contains('open')) sound.emit('ui.preview-cancel');
    cancelAiming();
  }
});

initialLoadPromise = i18n.load().then(() => {
  applyStaticTranslations();
  return bootstrapModes();
}).catch(error => showToast(error.message));
