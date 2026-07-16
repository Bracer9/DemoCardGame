const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const gameTypes = fs.readFileSync('Domain/GameTypes.cs', 'utf8');
const gameState = fs.readFileSync('Domain/GameState.cs', 'utf8');
const gameDtos = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const gameSession = fs.readFileSync('Services/GameSession.cs', 'utf8');
const easyAi = fs.readFileSync('Services/SimpleAiService.cs', 'utf8');
const normalAi = fs.readFileSync('Services/NormalAiService.cs', 'utf8');
const program = fs.readFileSync('Program.cs', 'utf8');
const index = fs.readFileSync('wwwroot/index.html', 'utf8');
const app = fs.readFileSync('wwwroot/app.js', 'utf8');

test('AI games persist an explicit Easy or Normal difficulty and default to Easy', () => {
  assert.match(gameTypes, /enum AiDifficulty\s*\{\s*Easy,\s*Normal\s*\}/s);
  assert.match(gameState, /AiDifficulty AiDifficulty \{ get; set; \} = AiDifficulty\.Easy/);
  assert.match(gameSession, /NewAiGame\(AiDifficulty difficulty = AiDifficulty\.Easy\)/);
  assert.match(gameSession, /slot\.State\.AiDifficulty = difficulty/);
  assert.match(program, /request\.Difficulty, "normal"/);
  assert.match(gameDtos, /state\.AiPlayerId is null \? null : state\.AiDifficulty\.ToString\(\)/);
});

test('Easy keeps the existing battle flow while only Normal can enter role-action decisions', () => {
  assert.match(easyAi, /state\.AiDifficulty == AiDifficulty\.Normal && _normal\.TryUseRoleAction\(state\)/);
  assert.doesNotMatch(easyAi, /_engine\.UseRoleAction/);
  assert.match(normalAi, /_previews\.Create\(state, actor\.Id, action\.Metadata\.Id, targetId\)/);
  assert.match(normalAi, /_engine\.UseRoleAction\(state, selected\.ActorId, selected\.RoleActionId, selected\.TargetId\)/);
});

test('Normal limits role actions, values BP opportunity, and can bank BP at reward windows', () => {
  assert.match(normalAi, /owner\.FirstRoleActionBpGrantedThisTurn/);
  assert.match(normalAi, /GameEngine\.BattlePointGainCapPerTurn - owner\.BattlePoints\.GainedThisTurn/);
  assert.match(normalAi, /metadata\.Tags\.Contains\("battle-point"/);
  assert.match(normalAi, /ShouldBankBattlePoints/);
  assert.match(easyAi, /state\.AiDifficulty == AiDifficulty\.Normal\s*&& _normal\.ShouldBankBattlePoints/s);
});

test('both AI difficulties take the first free role-action upgrade instead of silently leaving it', () => {
  assert.match(easyAi, /var freeRoleActionUpgrade = affordable\.FirstOrDefault/);
  assert.match(easyAi, /option\.RewardId == RewardCatalog\.HeroRoleActionUpgrade\.Id && option\.Cost == 0/);
  assert.match(easyAi, /_engine\.SelectReward\(state, freeRoleActionUpgrade\.InstanceId\)/);
});

test('AI mode opens a two-choice secondary menu and sends the selected difficulty', () => {
  assert.match(index, /id="ai-difficulty-menu"/);
  assert.match(index, /data-ai-difficulty="easy"/);
  assert.match(index, /data-ai-difficulty="normal"/);
  assert.match(app, /ui\.startAi\?\.addEventListener\('click', showAiDifficultyMenu\)/);
  assert.match(app, /JSON\.stringify\(\{ difficulty: aiDifficulty \}\)/);
  assert.match(app, /startAiGame\(button\.dataset\.aiDifficulty\)/);
});
