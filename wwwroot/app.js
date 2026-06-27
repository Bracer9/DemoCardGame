const ui = {
  viewport: document.querySelector('#game-viewport'), stage: document.querySelector('#game-root'),
  app: document.querySelector('#app'), opponentCards: document.querySelector('#opponent-cards'), activeCards: document.querySelector('#active-cards'),
  opponentName: document.querySelector('#opponent-name'), bottomName: document.querySelector('#bottom-name'), activePlayer: document.querySelector('#active-player'),
  turn: document.querySelector('#turn-number'), round: document.querySelector('#round-number'), ap: document.querySelector('#ap-pips'), apCurrent: document.querySelector('#ap-current'), apMaximum: document.querySelector('#ap-maximum'), instruction: document.querySelector('#instruction'),
  preview: document.querySelector('#preview-panel'), previewAttacker: document.querySelector('#preview-attacker'), previewDefender: document.querySelector('#preview-defender'),
  previewDamage: document.querySelector('#preview-damage'), previewCounter: document.querySelector('#preview-counter'), previewSkill: document.querySelector('#preview-skill'),
  previewNotes: document.querySelector('#preview-notes'), confirm: document.querySelector('#confirm-attack'), log: document.querySelector('#battle-log'),
  toast: document.querySelector('#toast'), curtain: document.querySelector('#turn-curtain'), curtainPlayer: document.querySelector('#curtain-player'), fx: document.querySelector('#fx-layer'),
  gameOver: document.querySelector('#game-over'), resultTitle: document.querySelector('#result-title'), resultCopy: document.querySelector('#result-copy'),
  inspector: document.querySelector('#character-inspector'), apHud: document.querySelector('#action-point-hud'),
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

resizeGameStage();

const i18n = window.TinyPixelI18n;
const art = window.TinyPixelAssets;

ui.startScreen = document.querySelector('#start-screen');
ui.startTrigger = document.querySelector('#start-trigger');
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
const audioGateButtons = [ui.startTrigger, document.querySelector('#create-room'), document.querySelector('#join-room')].filter(Boolean);
audioGateButtons.forEach(button => { button.disabled = true; });
const audioLoadPromise = sound.load('/config/audio.json')
  .catch(error => console.warn(error.message))
  .finally(() => audioGateButtons.forEach(button => { button.disabled = false; }));
const voiceLoadPromise = voice.load('/config/voice.json')
  .catch(error => console.warn(error.message));
art.load('/config/ui-assets.json').then(() => { if (game) render(); }).catch(error => console.warn(error.message));
document.addEventListener('pointerdown', () => sound.unlock({ primeUnrequested: false }), { capture: true });
document.addEventListener('keydown', () => sound.unlock({ primeUnrequested: false }), { capture: true });
document.addEventListener('visibilitychange', () => { if (!document.hidden) sound.ensureBgmPlaying(); });

let game = null;
let selectedAttacker = null;
let selectedDefender = null;
let preview = null;
let busy = false;
let hasStarted = false;
let dealing = false;
let initialLoadPromise = null;
let dragArrowOrigin = null;
let sessionMode = null;
let playerToken = localStorage.getItem('tpf-online-player-token') || '';
let room = null;
let pollTimer = null;
let lastGameJson = '';
let endTurnQueued = false;
let lastApSnapshot = null;
let lastAnimatedLogSequence = 0;
let eventPlayback = false;
let resultAudioGameId = null;
const recentSoundEvents = new Map();
const VOICE_REACTION_DELAY_MS = 760;
const ORDINARY_HIT_VOICE_REACTION_DELAY_MS = 980;
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
function emitDamageTakenVoice(characterKey, amount, state, options = {}) {
  const damage = Number(amount || 0);
  if (damage <= 0 || !characterKey) return;
  const character = characterByKey(state, characterKey);
  if (character && character.currentHp <= 0 && options.suppressIfDefeated !== false) return;
  const type = damageVoiceType(characterKey, damage, state);
  if (!type) return;
  emitVoiceDelayed(type, characterKey, {
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
  if (character && character.currentHp <= 0) return 'death';
  return damage >= heavyDamageThreshold(character) ? 'heavy-damage-taken' : 'damage-taken';
}
function defenderDefeatedByExchange(defenderKey, attackDamage, state) {
  const defender = characterByKey(state, defenderKey);
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
  if (dealt > taken) return 'attack-preview-overpower';
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
  const headers = { 'Content-Type': 'application/json', ...(playerToken ? { 'X-Player-Token': playerToken } : {}), ...(options.headers || {}) };
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
  resetEventCursor(game);
  render();
};

function render() {
  if (!game) return;
  const active = game.players.find(player => player.id === game.activePlayerId);
  const me = game.players.find(player => player.id === game.viewerPlayerId);
  const opponent = game.players.find(player => player.id !== game.viewerPlayerId);
  ui.turn.textContent = String(game.turnNumber).padStart(2, '0');
  ui.round.textContent = String(game.roundNumber).padStart(2, '0');
  ui.activePlayer.textContent = i18n.playerName(active.name);
  ui.bottomName.textContent = i18n.playerName(me.name);
  ui.opponentName.textContent = i18n.playerName(opponent.name);
  renderShieldBadge(ui.activeShield, me.sharedShield);
  renderShieldBadge(ui.opponentShield, opponent.sharedShield);
  const activeSharedShield = active?.sharedShield || 0;
  const canReinforceShield = game.shieldDeploymentsThisTurn > 0 && activeSharedShield > 0;
  const shieldUnavailable = !game.canDeployShield || !game.canControl || dealing;
  ui.shieldButton.disabled = false;
  ui.shieldButton.classList.toggle('unavailable', shieldUnavailable);
  ui.shieldButton.setAttribute('aria-disabled', String(shieldUnavailable));
  ui.shieldButton.classList.toggle('deployed', me.sharedShield > 0);
  ui.shieldButton.classList.toggle('reinforce-ready', canReinforceShield);
  ui.shieldButton.classList.toggle('reinforced', me.sharedShield > 2);
  if (!canReinforceShield && game.shieldDeploymentsThisTurn < 2) {
    ui.shieldButton.innerHTML = `${art.icon('event.shield', { size: 'md', label: i18n.t('defenseFormation'), className: 'command-icon' })}<span>${game.nextShieldCost} AP / SHIELD 2</span><b>${i18n.t('defenseFormation')}</b>`;
    ui.shieldButton.setAttribute('aria-label', `${game.nextShieldCost} AP / ${i18n.t('defenseFormation')} / SHIELD 2`);
  } else if (canReinforceShield) {
    const nextShield = activeSharedShield + 2;
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
  ui.activeCards.innerHTML = renderCardRow(activeCharacters, true);
  ui.opponentCards.innerHTML = renderCardRow(opponentCharacters, false);
  renderPersistentShield(ui.activeShieldDome, ui.activeCards, me.sharedShield, hasPendingShieldBreakForPlayer(game, me));
  renderPersistentShield(ui.opponentShieldDome, ui.opponentCards, opponent.sharedShield, hasPendingShieldBreakForPlayer(game, opponent));
  bindCards();
  renderLog();
  updateInstruction();
  renderGameOver();
  ui.app.classList.toggle('turn-locked', !game.canControl);
  ui.app.classList.toggle('attacker-selected', Boolean(selectedAttacker));
  ui.endTurn.disabled = !game.canControl || game.phase === 'Finished';
  ui.endTurn.classList.toggle('queued', endTurnQueued);
  ui.endTurn.classList.toggle('ap-empty-ready', game.canControl && game.phase !== 'Finished' && game.actionPoints === 0 && !endTurnQueued);
  ui.endTurn.setAttribute('aria-busy', String(endTurnQueued));
  document.querySelector('#new-game').disabled = !game.isHost;
  document.querySelector('#play-again').disabled = !game.isHost;
  art.hydrate(document);
}

function renderShieldBadge(element, value) {
  element.innerHTML = value > 0 ? `${art.icon('status.team-shield', { size: 'xs', label: 'Shared shield' })}<span>SHARED SHIELD</span><b>${value}</b>` : '';
  element.classList.toggle('active', value > 0);
}

function hasPendingShieldBreakForPlayer(state, player) {
  if (!state || !player || Number(player.sharedShield || 0) > 0) return false;
  const characterKeys = new Set((player.characters || []).map(character => character.key));
  return (state.log || []).some(entry =>
    entry.sequence > lastAnimatedLogSequence
    && entry.message?.key === 'note.shieldAbsorb'
    && characterKeys.has(String(logArg(entry, 'character') || '')));
}

function renderPersistentShield(dome, row, value, preserveUntilBreak = false) {
  const cards = [...row.querySelectorAll('.fighter-card')];
  const isOpponentShield = dome === ui.opponentShieldDome;
  dome.classList.toggle('opponent-facing', isOpponentShield);
  if (value <= 0 || cards.length === 0) {
    if (preserveUntilBreak && cards.length > 0 && dome.classList.contains('active')) {
      dome.dataset.pendingBreak = 'true';
      dome.classList.add('pending-break');
      renderPersistentShield(dome, row, Number(dome.dataset.visualShieldValue || 1), false);
      return;
    }
    dome.classList.remove('active', 'forming', 'breaking', 'pending-break');
    delete dome.dataset.pendingBreak;
    delete dome.dataset.visualShieldValue;
    return;
  }
  dome.dataset.visualShieldValue = String(value);
  const first = stageRect(cards[0]), last = stageRect(cards[cards.length - 1]);
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
  return (player?.characters || []).filter(character => character.isInBattle || pendingDefeatIds.has(character.id));
}

function renderCardRow(characters, isActiveSide) {
  return characters.map((character, index) => cardMarkup(character, isActiveSide, index, characters.length)).join('');
}

function cardMarkup(card, isActiveSide, index = 0, count = 1) {
  const classes = ['fighter-card'];
  const canConsiderCost = isActiveSide && game?.canControl && card.isAlive && !card.hasActed;
  if (canConsiderCost && card.cost <= game.actionPoints) classes.push('affordable-cost');
  if (canConsiderCost && card.cost > game.actionPoints) classes.push('unaffordable-cost');
  if (card.canAct) classes.push('can-act');
  if (card.hasActed) classes.push('acted');
  if (!card.isAlive) classes.push('defeated');
  if (!card.isInBattle) classes.push('leaving-battle');
  classes.push('full-art-card');
  if (dealing) classes.push('deal-hidden');
  if (card.id === selectedAttacker) classes.push('selected');
  if (card.id === selectedDefender) classes.push('target-selected');
  const translatedStatuses = card.statuses.map(status => i18n.status(status));
  const statuses = translatedStatuses.map(status => `<span class="status-chip ${status.isBuff ? '' : 'debuff'}" title="${escapeHtml(status.description)}">${art.icon(art.forStatus(status.id), { size: 'xs', label: status.name })}<span>${escapeHtml(status.name)}</span></span>`).join('');
  const localizedSkill = i18n.skill(card.skill.id);
  localizedSkill.kind = i18n.skillKind(card.skill.kind);
  const skillClass = card.skill.isReady ? 'ready' : 'disabled';
  const over = card.currentHp > card.maxHp ? 'hp-over' : '';
  const hpRatio = card.maxHp > 0 ? Math.max(0, Math.min(1, card.currentHp / card.maxHp)) : 0;
  const hpEmpty = card.currentHp <= 0 ? ' hp-empty' : '';
  const cardDescription = truncateCardText(localizedSkill.card, 28);
  const portraitUrl = card.coloredAssetUrl || card.assetUrl;
  const costCrystals = Array.from({ length: Math.max(0, card.cost) }, () => '<i class="cost-crystal" aria-hidden="true"></i>').join('');
  return `<article class="${classes.join(' ')}" style="${cardPoseStyle(isActiveSide, index, count)}" data-id="${card.id}" data-key="${card.key}" data-side="${isActiveSide ? 'active' : 'opponent'}" data-zone="${escapeHtml(card.zone || (card.isInBattle ? 'Battlefield' : 'Defeated'))}" draggable="${card.canAct}">
    <div class="card-name">${escapeHtml(i18n.characterName(card.key))}</div><div class="cost-orb" aria-label="Cost ${card.cost}">${costCrystals}</div>
    <div class="type-rune ${card.attackType === 'Magical' ? 'magic' : ''}">${escapeHtml(i18n.damageType(card.attackType))}</div>
    <div class="portrait-wrap"><img class="portrait" src="${portraitUrl}" alt="${escapeHtml(i18n.characterName(card.key))}"></div>
    <div class="status-stack">${statuses}</div>
    <div class="skill-panel ${skillClass}" title="${escapeHtml(i18n.message(card.skill.unavailableReason))}">
      <div class="skill-title"><span>${escapeHtml(localizedSkill.name)}</span><b class="skill-kind">${escapeHtml(localizedSkill.kind)}</b></div>
      <p class="skill-description">${escapeHtml(cardDescription)}</p>
    </div>
    <div class="stat-orb attack"><span>ATK</span><strong>${card.attack}</strong></div>
    <div class="stat-orb hp${hpEmpty}" style="--hp-ratio:${hpRatio.toFixed(3)}"><span>HP</span><strong class="${over}">${card.currentHp}<small>/${card.maxHp}</small></strong></div>
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
    card.addEventListener('mouseenter', () => showCharacterInspector(card));
    card.addEventListener('mouseleave', hideCharacterInspector);
    card.addEventListener('click', () => onCardClick(card));
    card.addEventListener('dragstart', event => {
      hideCharacterInspector();
      if (!card.classList.contains('can-act')) { event.preventDefault(); return; }
      const wasAlreadySelected = selectedAttacker === card.dataset.id;
      selectedAttacker = card.dataset.id; selectedDefender = null; closePreview();
      if (!wasAlreadySelected) {
        sound.emit('ui.card-select');
        emitSelectVoice(card);
      }
      document.querySelectorAll('.fighter-card.selected').forEach(element => element.classList.remove('selected'));
      card.classList.add('selected');
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
      hideAttackArrow();
    });
    if (card.dataset.side === 'opponent' && !card.classList.contains('defeated')) {
      card.addEventListener('dragover', event => { if (selectedAttacker) { event.preventDefault(); card.classList.add('drop-ready'); const point = clientToStage(event.clientX, event.clientY); updateAttackArrow(point.x, point.y, card); } });
      card.addEventListener('dragleave', () => card.classList.remove('drop-ready'));
      card.addEventListener('drop', event => { event.preventDefault(); card.classList.remove('drop-ready'); hideAttackArrow(); chooseDefender(card.dataset.id); });
    }
  });
}

function startAttackArrow(card) {
  const rect = stageRect(card);
  const isActiveCard = card.dataset.side === 'active';
  const lift = isActiveCard ? cssPixelVar('--active-selected-lift', 0) : 0;
  const alreadyLifted = card.classList.contains('selected') && Math.abs(cssTranslateY(card)) > Math.max(12, lift * .5);
  dragArrowOrigin = { x: rect.left + rect.width / 2, y: rect.top + rect.height * 0.5 - (alreadyLifted ? 0 : lift) };
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
  ui.app.classList.remove('dragging-attack');
  document.body.classList.remove('dragging-attack');
  ui.attackArrow.classList.remove('active', 'locked');
}

function showCharacterInspector(element) {
  const card = findCard(element.dataset.id);
  if (!card || dealing) return;
  hideShieldInspector();
  const translatedStatuses = card.statuses.map(status => i18n.status(status));
  const localizedSkill = i18n.skill(card.skill.id);
  localizedSkill.kind = i18n.skillKind(card.skill.kind);
  const statusMarkup = translatedStatuses.length
    ? translatedStatuses.map(status => `<li class="${status.isBuff ? 'buff' : 'debuff'}">
        <div class="effect-title">${art.icon(art.forStatus(status.id), { size: 'md', label: status.name })}<div><b>${escapeHtml(status.name)}</b><em>${status.isBuff ? i18n.t('buff') : i18n.t('debuff')}${status.isAura ? ` / ${i18n.t('aura')}` : ''}</em></div></div>
        <small>${escapeHtml(status.timing || i18n.t('always'))}</small><p>${escapeHtml(status.description)}</p>
      </li>`).join('')
    : `<li class="empty-status">${i18n.t('noEffects')}</li>`;
  const attackDelta = card.attack - card.baseAttack;
  const attackDisplay = attackDelta === 0 ? `${card.attack}` : `${card.attack} (${attackDelta > 0 ? '+' : ''}${attackDelta})`;
  ui.inspector.innerHTML = `<header><span>${i18n.t('unitDossier')}</span><strong>${escapeHtml(i18n.characterName(card.key))}</strong></header>
    <div class="inspector-stats">
      <div class="stat-card stat-attack"><span>ATK</span><b>${escapeHtml(attackDisplay)}</b></div>
      <div class="stat-card stat-hp"><span>HP</span><b>${card.currentHp}/${card.maxHp}</b></div>
      <div class="stat-card stat-cost"><span>COST</span><b>${card.cost}</b></div>
      <div class="stat-card stat-type"><span>TYPE</span><b class="damage-type ${card.attackType === 'Magical' ? 'magic' : ''}">${escapeHtml(i18n.damageType(card.attackType))}</b></div>
    </div>
    <section class="inspector-skill ${card.skill.isReady ? 'ready' : 'disabled'}">
      <div class="inspector-skill-heading">${art.icon(art.forSkill(card.skill.id), { size: 'md', label: localizedSkill.name })}<div><span>SKILL / ${escapeHtml(localizedSkill.kind)}</span><b>${escapeHtml(localizedSkill.name)}</b><p>${escapeHtml(localizedSkill.description)}</p></div></div>
      ${card.skill.unavailableReason ? `<small>${escapeHtml(i18n.message(card.skill.unavailableReason))}</small>` : ''}
    </section>`;
  ui.statusInspector.innerHTML = `<header><span>${i18n.t('liveEffects')}</span><strong>${i18n.t('effects')}</strong><b>${card.statuses.length}</b></header>
    <section class="inspector-effects"><ul>${statusMarkup}</ul></section>`;
  const rect = stageRect(element);
  ui.inspector.classList.add('open');
  ui.inspector.setAttribute('aria-hidden', 'false');
  ui.statusInspector.classList.add('open');
  ui.statusInspector.setAttribute('aria-hidden', 'false');
  art.hydrate(ui.inspector);
  art.hydrate(ui.statusInspector);
  positionInspector(ui.inspector, Math.max(16, rect.left - ui.inspector.offsetWidth - 14), rect);
  positionInspector(ui.statusInspector, Math.min(STAGE_WIDTH - ui.statusInspector.offsetWidth - 16, rect.right + 14), rect);
}

function positionInspector(panel, left, targetRect) {
  const halfHeight = panel.offsetHeight / 2;
  const top = Math.min(STAGE_HEIGHT - halfHeight - 18, Math.max(halfHeight + 82, targetRect.top + targetRect.height / 2));
  panel.style.left = `${left}px`;
  panel.style.top = `${top}px`;
}

function hideCharacterInspector() {
  ui.inspector.classList.remove('open');
  ui.inspector.setAttribute('aria-hidden', 'true');
  ui.statusInspector.classList.remove('open');
  ui.statusInspector.setAttribute('aria-hidden', 'true');
}

function showShieldInspector() {
  if (!game || dealing) return;
  hideCharacterInspector();
  const active = game.players.find(player => player.id === game.activePlayerId);
  const shield = active?.sharedShield || 0;
  const canReinforceShield = game.shieldDeploymentsThisTurn > 0 && shield > 0;
  const lightState = shield > 0 && shield <= 2 ? 'current' : !canReinforceShield && game.shieldDeploymentsThisTurn < 2 ? 'next' : '';
  const reinforcedState = shield > 2 ? 'current' : canReinforceShield ? 'next' : '';
  const stateLabel = shield > 0 ? i18n.t('shieldCurrent', { value: shield }) : i18n.t('shieldNone');
  ui.shieldInspector.innerHTML = `<header><span>TACTICAL COMMAND</span><strong>${i18n.t('defenseFormation')}</strong><b>${game.nextShieldCost} AP</b></header>
    <p class="shield-rule-lead">${i18n.t('shieldLead')}</p>
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

function onCardClick(element) {
  if (busy) return;
  const id = element.dataset.id;
  if (element.dataset.side === 'active') {
    if (!element.classList.contains('can-act')) { sound.emit('ui.invalid-action'); showToast(i18n.t('cannotAct')); return; }
    const isSelecting = selectedAttacker !== id;
    selectedAttacker = isSelecting ? id : null; selectedDefender = null; closePreview();
    if (isSelecting) {
      sound.emit('ui.card-select');
      emitSelectVoice(element);
    }
    render(); return;
  }
  if (!selectedAttacker) { sound.emit('ui.invalid-action'); showToast(i18n.t('selectAttackerFirst')); return; }
  if (!element.classList.contains('defeated')) chooseDefender(id);
}

async function chooseDefender(id) {
  selectedDefender = id;
  try {
    const payload = await gameApi(`/game/preview?attackerId=${selectedAttacker}&defenderId=${id}`);
    preview = payload.data;
    if (!preview.isValid) throw new Error(i18n.message(preview.error));
    render();
    renderPreview();
    emitAttackPreviewVoice(findCard(selectedAttacker), findCard(selectedDefender), preview);
  } catch (error) { sound.emit('ui.invalid-action'); showToast(error.message); }
}

function renderPreview() {
  const attacker = findCard(selectedAttacker), defender = findCard(selectedDefender);
  ui.previewAttacker.textContent = attacker ? i18n.characterName(attacker.key) : '—';
  ui.previewDefender.textContent = defender ? i18n.characterName(defender.key) : '—';
  setForecast(ui.previewDamage, preview.attack, i18n.t('damageDealt'), art.forDamageType(preview.attack.damageType));
  setForecast(ui.previewCounter, preview.counter, i18n.t('counterTaken'), 'event.counter');
  ui.previewSkill.className = `preview-skill ${preview.skillConditionPossible ? '' : 'inactive'}`;
  const localizedSkill = i18n.skill(preview.skillId);
  ui.previewSkill.innerHTML = `<strong>${escapeHtml(localizedSkill.name)}</strong><p>${escapeHtml(i18n.message(preview.skillForecast))}</p>`;
  ui.previewNotes.innerHTML = preview.notes.map(note => `<li>${escapeHtml(i18n.message(note))}</li>`).join('');
  art.hydrate(ui.preview);
  ui.preview.classList.add('open'); ui.preview.setAttribute('aria-hidden', 'false');
}

function setForecast(element, forecast, label, iconId) {
  const value = forecast.min === forecast.max ? forecast.max : `${forecast.min}–${forecast.max}`;
  element.className = `forecast-box ${forecast.damageType === 'Magical' ? 'magic' : 'physical'}`;
  element.innerHTML = `<div class="forecast-heading">${art.icon(iconId, { size: 'sm', label })}<small>${label} / ${i18n.damageType(forecast.damageType)}</small></div><strong>${value}</strong>`;
}

async function executeAttack() {
  if (busy || !selectedAttacker || !selectedDefender) return;
  sound.emit('ui.attack-confirm');
  busy = true; ui.confirm.disabled = true;
  const attackerId = selectedAttacker, defenderId = selectedDefender;
  closePreview();
  try {
    const payload = await gameApi('/game/attack', { method: 'POST', body: JSON.stringify({ attackerId, defenderId }) });
    sound.emit('ui.ap-spend');
    game = payload.data; lastGameJson = JSON.stringify(game); selectedAttacker = null; selectedDefender = null; preview = null; render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; ui.confirm.disabled = false; }
}

async function endTurn() {
  if (!game?.canControl || game.phase === 'Finished' || endTurnQueued) return;
  sound.emit('ui.turn-end');
  endTurnQueued = true;
  ui.endTurn.classList.add('queued');
  ui.endTurn.classList.remove('ap-empty-ready');
  ui.endTurn.setAttribute('aria-busy', 'true');
  while (busy) await wait(40);
  if (!game?.canControl || game.phase === 'Finished') {
    endTurnQueued = false;
    ui.endTurn.classList.remove('queued');
    ui.endTurn.setAttribute('aria-busy', 'false');
    return;
  }
  busy = true;
  try {
    const payload = await gameApi('/game/end-turn', { method: 'POST', body: '{}' });
    sound.emit('turn.change');
    game = payload.data; lastGameJson = JSON.stringify(game); selectedAttacker = null; selectedDefender = null; closePreview();
    ui.curtainPlayer.textContent = i18n.playerName(game.activePlayerName); ui.curtain.classList.remove('play'); void ui.curtain.offsetWidth; ui.curtain.classList.add('play');
    await wait(620); render(); await wait(430); ui.curtain.classList.remove('play');
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally {
    busy = false;
    endTurnQueued = false;
    ui.endTurn.classList.remove('queued');
    ui.endTurn.setAttribute('aria-busy', 'false');
  }
}

async function deployShield() {
  if (busy || !game?.canControl || !game?.canDeployShield) return;
  sound.emit('ui.shield-command');
  busy = true;
  ui.shieldButton.disabled = true;
  selectedAttacker = null; selectedDefender = null; closePreview(); hideCharacterInspector(); hideShieldInspector();
  try {
    const payload = await gameApi('/game/shield', { method: 'POST', body: '{}' });
    sound.emit('ui.ap-spend');
    game = payload.data; lastGameJson = JSON.stringify(game);
    render();
    await playNewLogEvents(game);
  } catch (error) { showToast(error.message); }
  finally { busy = false; if (game) render(); }
}

async function newGame() {
  if (busy || !game?.isHost) { showToast(i18n.t('hostOnlyRestart')); return; }
  busy = true;
  try {
    sound.emit('game.restart');
    const payload = await gameApi('/game/new', { method: 'POST', body: '{}' });
    if (sessionMode === 'online' && room) room.dealStarted = false;
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; closePreview();
    dealing = true; render();
    await playDealSequence();
  }
  catch (error) { showToast(error.message); } finally { busy = false; }
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
    game = payload.data; lastGameJson = JSON.stringify(game); lastApSnapshot = null; resetEventCursor(game); selectedAttacker = null; selectedDefender = null; closePreview();
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
  if (!playerName) return;
  if (emitSound) sound.emit('turn.change');
  ui.curtainPlayer.textContent = i18n.playerName(playerName);
  ui.curtain.classList.remove('play');
  void ui.curtain.offsetWidth;
  ui.curtain.classList.add('play');
  await wait(1000);
  ui.curtain.classList.remove('play');
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
  for (let index = 0; index < 4; index++) {
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
  physical: 'event.physical', magical: 'event.magical', counter: 'event.counter', skill: 'event.skill',
  status: 'event.status-tick', heal: 'event.heal', shield: 'event.shield', death: 'event.death'
});

const eventLabelKey = Object.freeze({
  [eventIcon.physical]: 'eventPhysical', [eventIcon.magical]: 'eventMagical', [eventIcon.counter]: 'eventCounter',
  [eventIcon.skill]: 'eventSkill', [eventIcon.status]: 'eventStatus', [eventIcon.heal]: 'eventHeal',
  [eventIcon.shield]: 'eventShield', [eventIcon.death]: 'eventDeath'
});

function eventLabel(iconId) {
  return i18n.t(eventLabelKey[iconId] || 'eventSkill');
}

function effectLabel(arg) {
  if (!arg) return i18n.t('eventSkill');
  return arg.kind === 'status' ? i18n.status({ id: arg.value, magnitude: 0 }).name : i18n.skill(arg.value).name;
}

function resetEventCursor(state = game) {
  lastAnimatedLogSequence = Math.max(0, ...(state?.log || []).map(entry => entry.sequence || 0));
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

function playerForCharacterKey(state, key) {
  return state?.players.find(player => player.characters.some(card => card.key === key)) || null;
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
  el.className = `event-burst ${options.tone || 'skill'}`;
  el.style.left = `${point.x}px`; el.style.top = `${point.y}px`;
  el.innerHTML = `${art.icon(iconId, { size: 'lg', label: title, className: 'event-burst-primary' })}${secondary}<span class="event-burst-copy"><strong>${escapeHtml(title)}</strong>${amount}</span>`;
  ui.fx.appendChild(el); art.hydrate(el);
  setTimeout(() => el.remove(), options.duration || 1250);
  return wait(options.hold || 450);
}

async function playExchangeEvent(entry, state) {
  const attackerKey = logArg(entry, 'attacker');
  const defenderKey = logArg(entry, 'defender');
  const attacker = characterElementByKey(state, attackerKey);
  const defender = characterElementByKey(state, defenderKey);
  if (!attacker || !defender) return;
  const attackType = logArg(entry, 'attackType') || 'Physical';
  const counterType = logArg(entry, 'counterType') || 'Physical';
  const attackDamage = Number(logArg(entry, 'attackDamage') || 0);
  const counterDamage = Number(logArg(entry, 'counterDamage') || 0);
  const attackShieldAbsorbed = Number(logArg(entry, 'attackShieldAbsorbed') || 0);
  const counterShieldAbsorbed = Number(logArg(entry, 'counterShieldAbsorbed') || 0);
  const defeatedTarget = defenderDefeatedByExchange(defenderKey, attackDamage, state);
  const attackerVoiceType = defeatedTarget ? 'defeat' : 'attack-declare';
  const defenderVoiceType = defeatedTarget ? null : damageVoiceType(defenderKey, attackDamage, state);
  sound.emit('combat.target-lock');
  emitVoice(attackerVoiceType, attackerKey, { targetCharacterId: defenderKey, damageType: attackType, amount: attackDamage });
  if (defenderVoiceType)
    emitVoiceDelayed(defenderVoiceType, defenderKey, { source: 'active-attack', damageType: attackType, attackerCharacterId: attackerKey, amount: attackDamage }, voiceReactionDelay(attackerVoiceType, defenderVoiceType));
  if (attackType === 'Magical') sound.emit('combat.magic-active');
  if (attackType === 'Physical' && attackDamage > 0 && attackShieldAbsorbed === 0) sound.emit('combat.physical-hit');
  if (counterType === 'Physical' && counterDamage > 0 && counterShieldAbsorbed === 0) sound.emit('combat.physical-hit');
  if (attackDamage === 0 && attackShieldAbsorbed === 0) sound.emit('combat.no-damage');
  if (counterDamage === 0 && counterShieldAbsorbed === 0) emitSoundThrottled('combat.no-damage', 180);
  const combatLink = showCombatLink(attacker, defender);
  launchFx(attacker, defender, attackType); launchFx(defender, attacker, counterType);
  await wait(180);
  if (attackType === 'Magical' && attackDamage > 0 && attackShieldAbsorbed === 0) sound.emit('combat.magic-impact');
  if (counterType === 'Magical' && counterDamage > 0 && counterShieldAbsorbed === 0) sound.emit('combat.magic-counter');
  await Promise.all([
    eventBurst(defender, art.forDamageType(attackType), { title: attackType === 'Magical' ? i18n.t('eventMagical') : i18n.t('eventPhysical'), amount: `-${attackDamage}`, tone: attackType === 'Magical' ? 'magic' : 'physical' }),
    eventBurst(attacker, eventIcon.counter, { title: i18n.t('eventCounter'), secondaryIconId: art.forDamageType(counterType), amount: `-${counterDamage}`, tone: 'counter' })
  ]);
  defender.classList.add(attackType === 'Magical' ? 'impact-magic' : 'impact-physical');
  attacker.classList.add(counterType === 'Magical' ? 'impact-magic' : 'impact-physical');
  setTimeout(() => defender.classList.remove('impact-magic', 'impact-physical'), 520);
  setTimeout(() => attacker.classList.remove('impact-magic', 'impact-physical'), 520);
  if (attackDamage > 0) damageFloat(defender, attackDamage, attackType);
  if (counterDamage > 0) damageFloat(attacker, counterDamage, counterType);
  await wait(470);
  if (combatLink) {
    combatLink.classList.add('leaving');
    setTimeout(() => combatLink.remove(), 320);
  }
}

async function playLogEntry(entry, state) {
  const key = entry?.message?.key;
  const characterKey = logArg(entry, 'character');
  const target = characterElementFromLog(state, entry);
  const effect = logArgObject(entry, 'effect');
  const status = logArgObject(entry, 'status');
  const skill = logArgObject(entry, 'skill');
  const effectIcon = effect?.kind === 'status' ? art.forStatus(effect.value) : art.forSkill(effect?.value);
  const amount = Number(logArg(entry, 'amount') || 0);

  switch (key) {
    case 'log.exchange':
      await playExchangeEvent(entry, state); break;
    case 'note.shieldAbsorb': {
      const player = playerForCharacterKey(state, characterKey);
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
    case 'note.complacencyReduction':
      sound.emit('status.complacency-consumed');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'shield-complacency', magnitude: 0 }).name, secondaryIconId: art.forStatus('shield-complacency'), amount: `-${amount}`, tone: 'status' }); break;
    case 'note.magicBonus':
      sound.emit('status.magic-bonus');
      await eventBurst(target, eventIcon.skill, { title: i18n.skill('stargazers-aegis').name, secondaryIconId: art.forSkill('stargazers-aegis'), amount: `+${amount}`, tone: 'skill' }); break;
    case 'note.guardRedirect':
      sound.emit('skill.guard-trigger');
      await Promise.all([
        eventBurst(target, eventIcon.skill, { title: i18n.skill('interposing-shield').name, secondaryIconId: art.forSkill('interposing-shield'), amount: `-${amount}`, tone: 'skill' }),
        eventBurst(characterElementByKey(state, logArg(entry, 'target')), eventIcon.skill, { title: i18n.skill('interposing-shield').name, secondaryIconId: art.forSkill('interposing-shield'), amount: `-${amount}`, tone: 'skill' })
      ]); break;
    case 'log.effectDamage': {
      const type = logArg(entry, 'damageType') || 'Physical';
      if (effect?.kind === 'status' && effect.value === 'burning') sound.emit('status.burning-tick');
      else if (effect?.kind === 'skill' && effect.value === 'aftershock-axe') sound.emit('skill.aftershock-axe');
      else if (effect?.kind === 'skill' && effect.value === 'predatory-instinct') {
        sound.emit('skill.predatory-instinct');
        sound.emit('combat.absolute-damage');
      }
      else if (effect?.kind === 'skill') sound.emit('combat.skill-damage');
      else if (type === 'Absolute' && amount > 0) sound.emit('combat.absolute-damage');
      else if (type === 'Physical' && amount > 0) sound.emit('combat.physical-hit');
      if (effect?.kind === 'status')
        await eventBurst(target, eventIcon.status, { title: effectLabel(effect), secondaryIconId: effectIcon, tone: 'status' });
      else
        await eventBurst(characterElementByKey(state, logArg(entry, 'source')), eventIcon.skill, { title: effectLabel(effect), secondaryIconId: effectIcon, tone: 'skill' });
      await eventBurst(target, art.forDamageType(type), { title: i18n.damageType(type), amount: `-${amount}`, tone: type === 'Magical' ? 'magic' : type === 'Physical' ? 'physical' : 'skill' });
      if (target && amount > 0) damageFloat(target, amount, type);
      emitDamageTakenVoice(characterKey, amount, state, { source: effect?.kind === 'status' ? 'status' : 'skill', damageType: type, skillId: effect?.kind === 'skill' ? effect.value : undefined, statusId: effect?.kind === 'status' ? effect.value : undefined });
      await wait(340); break;
    }
    case 'log.collateralDamage':
      if (effect?.value === 'guard') sound.emit('skill.guard-collateral');
      else if (amount > 0) sound.emit('combat.physical-hit');
      await eventBurst(target, eventIcon.skill, { title: effectLabel(effect), secondaryIconId: effectIcon, amount: `-${amount}`, tone: 'skill' });
      if (target && amount > 0) damageFloat(target, amount, 'Physical');
      emitDamageTakenVoice(characterKey, amount, state, { source: 'collateral', damageType: 'Physical', statusId: effect?.value });
      await wait(340); break;
    case 'log.healed':
      emitSoundThrottled('status.blessing-heal', 450);
      await eventBurst(target, eventIcon.heal, { title: effectLabel(effect), secondaryIconId: effectIcon, amount: `+${amount}`, tone: 'heal' }); break;
    case 'log.statusApplied':
      if (status?.value === 'burning') sound.emit('status.burning-applied');
      if (status?.value === 'weakness-pending') sound.emit('status.weakness-pending');
      if (status?.value === 'beast-rage') sound.emit('status.beast-rage');
      await eventBurst(target, eventIcon.status, { title: effectLabel(status), secondaryIconId: art.forStatus(status?.value), tone: 'status' }); break;
    case 'log.attackBuffRemoved':
      sound.emit('status.expired');
      await eventBurst(target, eventIcon.status, { title: effectLabel(status), secondaryIconId: art.forStatus(status?.value), amount: '×', tone: 'status' }); break;
    case 'log.weaknessActivated':
      sound.emit('status.weakness-activated');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'weakness', magnitude: 0 }).name, secondaryIconId: art.forStatus('weakness'), tone: 'status' }); break;
    case 'log.harvestActivated':
      sound.emit('status.harvest-activated');
      await eventBurst(target, eventIcon.status, { title: i18n.status({ id: 'harvest', magnitude: 0 }).name, secondaryIconId: art.forStatus('harvest'), tone: 'status' }); break;
    case 'log.sowing':
      sound.emit('status.sowing');
      await eventBurst(target, eventIcon.skill, { title: i18n.skill('spring-harvest').name, secondaryIconId: art.forSkill('spring-harvest'), tone: 'skill' }); break;
    case 'log.skillFailed':
      if (skill?.value === 'weakening-spores') sound.emit('skill.weakness-failed');
      await eventBurst(target, eventIcon.skill, { title: effectLabel(skill), secondaryIconId: art.forSkill(skill?.value), amount: '×', tone: 'skill' }); break;
    case 'log.defeated':
      sound.emit('combat.character-defeated');
      emitVoice('death', characterKey);
      await eventBurst(target, eventIcon.death, { tone: 'death', hold: 430 });
      if (target) target.classList.add('death-strike');
      await wait(780); break;
    case 'log.shieldDeployed':
    case 'log.shieldReinforced': {
      sound.emit(key === 'log.shieldDeployed' ? 'shield.deploy' : 'shield.reinforce');
      sound.emit('status.complacency-applied');
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

async function playNewLogEvents(state) {
  const entries = (state?.log || []).filter(entry => entry.sequence > lastAnimatedLogSequence).sort((a, b) => a.sequence - b.sequence);
  let shouldRenderAfter = false;
  for (const entry of entries) {
    await playLogEntry(entry, state);
    if (entry.message?.key === 'log.defeated') shouldRenderAfter = true;
    lastAnimatedLogSequence = Math.max(lastAnimatedLogSequence, entry.sequence || 0);
  }
  if (shouldRenderAfter && state === game) render();
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
    <path class="combat-link-glow" d="${path}"/>
    <path class="combat-link-core" d="${path}" stroke="url(#${gradientId})"/>
    <circle class="combat-link-source" cx="${from.x}" cy="${from.y}" r="7"/>
    <g class="combat-link-head" transform="translate(${to.x} ${to.y})">
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
function damageFloat(target, amount, type) { emitSoundThrottled('combat.hp-loss', 90); const p = center(target), el = document.createElement('b'); el.className = `damage-float ${type === 'Magical' ? 'magic' : type === 'Absolute' ? 'absolute' : 'physical'}`; el.textContent = `-${amount}`; el.style.left = `${p.x}px`; el.style.top = `${p.y}px`; ui.fx.appendChild(el); setTimeout(() => el.remove(), 850); }
function playShieldFormationAnimation(dome) {
  dome.classList.remove('forming'); void dome.offsetWidth; dome.classList.add('forming');
  return wait(620).then(() => dome.classList.remove('forming'));
}
function shieldBlock(target, amount, remaining) {
  const p = center(target), text = document.createElement('b');
  target.classList.add('shield-block'); setTimeout(() => target.classList.remove('shield-block'), 520);
  text.className = 'shield-float'; text.textContent = `SHIELD -${amount}`; text.style.left = `${p.x}px`; text.style.top = `${p.y}px`; ui.fx.appendChild(text);
  const dome = target.dataset.side === 'active' ? ui.activeShieldDome : ui.opponentShieldDome;
  if (remaining <= 0) breakShieldDome(dome);
  else {
    dome.classList.remove('hit'); void dome.offsetWidth; dome.classList.add('hit');
    setTimeout(() => dome.classList.remove('hit'), 430);
  }
  setTimeout(() => text.remove(), 920);
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
function updateInstruction() { const card = findCard(selectedAttacker); const name = card ? i18n.characterName(card.key) : ''; ui.instruction.innerHTML = card ? `<span class="instruction-icon">◆</span><div><strong>${escapeHtml(i18n.t('currentSelected', { name }))}</strong><small>${i18n.t('selectUpperEnemy')}</small></div>` : `<span class="instruction-icon">◆</span><div><strong>${i18n.t('selectCard')}</strong><small>${i18n.t('selectTarget')}</small></div>`; }
function findCard(id) { return game?.players.flatMap(player => player.characters).find(card => card.id === id); }
function closePreview() { ui.preview.classList.remove('open'); ui.preview.setAttribute('aria-hidden', 'true'); }
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

function renderAudioControls() {
  if (!ui.soundToggle || !sound.getSettings) return;
  const settings = sound.getSettings();
  updateAudioGroupControls('bgm', settings);
  updateAudioGroupControls('sfx', settings);
  updateAudioGroupControls('voice', settings);
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
      resetEventCursor(game);
    } else {
      await loadGame();
    }
    selectedAttacker = null; selectedDefender = null; closePreview();
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
  try {
    await refreshRoom();
    // An open forecast must never pause server synchronization. If the remote
    // player advances the game, the changed state below closes the stale panel.
    if (!room?.started || dealing || !hasStarted) return;
    const payload = await gameApi('/game/state');
    const json = JSON.stringify(payload.data);
    if (json === lastGameJson) return;
    const previousGameId = game?.gameId;
    const oldActive = game?.activePlayerId;
    const isNewGame = Boolean(previousGameId && previousGameId !== payload.data.gameId);
    if (isNewGame) dealing = true;
    game = payload.data; lastGameJson = json;
    if (isNewGame) lastApSnapshot = null;
    selectedAttacker = null; selectedDefender = null; closePreview(); render();
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
  } finally { eventPlayback = false; }
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
ui.endTurn.addEventListener('click', endTurn);
ui.shieldButton.addEventListener('click', deployShield);
ui.shieldButton.addEventListener('mouseenter', showShieldInspector);
ui.shieldButton.addEventListener('mouseleave', hideShieldInspector);
ui.shieldButton.addEventListener('focus', showShieldInspector);
ui.shieldButton.addEventListener('blur', hideShieldInspector);
document.querySelector('#new-game').addEventListener('click', newGame);
document.querySelector('#play-again').addEventListener('click', newGame);
ui.startTrigger.addEventListener('click', startLocalGame);
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
  hideCharacterInspector(); hideShieldInspector();
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
ui.sfxVolume.addEventListener('change', () => setAudioGroupVolume('sfx', ui.sfxVolume.value, true));
ui.voiceVolume.addEventListener('change', () => setAudioGroupVolume('voice', ui.voiceVolume.value, true));
document.addEventListener('click', event => {
  if (ui.audioMenu?.contains(event.target)) return;
  setAudioMenuOpen(false);
});
document.addEventListener('click', event => {
  if (!selectedAttacker || busy || dealing) return;
  const interactive = event.target instanceof Element
    ? event.target.closest('.fighter-card,.preview-panel,.command-deck,.topbar,.battle-log-shell,.action-point-hud,.character-inspector,.shield-inspector,.game-over,.start-screen,.deal-sequence')
    : null;
  if (interactive) return;
  selectedAttacker = null;
  selectedDefender = null;
  closePreview();
  hideAttackArrow();
  render();
});
document.querySelector('#toggle-log').addEventListener('click', () => { sound.emit('ui.panel-toggle'); ui.log.classList.toggle('open'); });
document.addEventListener('dragover', event => {
  if (!dragArrowOrigin) return;
  const target = event.target instanceof Element ? event.target.closest('.fighter-card[data-side="opponent"]:not(.defeated)') : null;
  const point = clientToStage(event.clientX, event.clientY);
  updateAttackArrow(point.x, point.y, target);
});
window.addEventListener('resize', () => {
  resizeGameStage();
  if (!game || dealing) return;
  const viewer = game.players.find(player => player.id === game.viewerPlayerId);
  const opponent = game.players.find(player => player.id !== game.viewerPlayerId);
  renderPersistentShield(ui.activeShieldDome, ui.activeCards, viewer.sharedShield);
  renderPersistentShield(ui.opponentShieldDome, ui.opponentCards, opponent.sharedShield);
});
window.visualViewport?.addEventListener('resize', () => {
  resizeGameStage();
});
document.addEventListener('keydown', event => {
  if (!hasStarted) return;
  if (event.key === 'Escape') {
    setAudioMenuOpen(false);
    if (ui.preview.classList.contains('open')) sound.emit('ui.preview-cancel');
    selectedAttacker = null; selectedDefender = null; hideAttackArrow(); closePreview(); render();
  }
});

initialLoadPromise = i18n.load().then(() => {
  applyStaticTranslations();
  return bootstrapModes();
}).catch(error => showToast(error.message));
