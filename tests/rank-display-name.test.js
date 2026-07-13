const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const definitions = fs.readFileSync('Domain/CharacterDefinition.cs', 'utf8');
const dtos = fs.readFileSync('Api/GameDtos.cs', 'utf8');
const app = fs.readFileSync('wwwroot/app.js', 'utf8');
const zh = JSON.parse(fs.readFileSync('wwwroot/locales/zh.json', 'utf8'));
const ja = JSON.parse(fs.readFileSync('wwwroot/locales/ja.json', 'utf8'));

const soldierNameIds = [
  'saint-cleric',
  'aegis-shieldmaiden',
  'crimson-duelist',
  'astral-arcanist',
  'masque-jester'
];

const heroNameIds = [
  'saint-queen', 'war-queen', 'astral-oracle', 'fate-dealer',
  'quartermaster', 'militia-foreman', 'stellar-archmage', 'arcane-archivist',
  'grove-keeper', 'wildspeaker', 'radiant-berserker', 'dragon-raider',
  'nightmare-fiend', 'abyssal-queen', 'holy-paladin', 'dread-cavalier'
];

test('ranked characters expose their current display name id', () => {
  assert.match(definitions, /string\? Rank2NameId = null/);
  for (const id of soldierNameIds)
    assert.match(definitions, new RegExp(`Rank2NameId: "${id}"`));
  assert.match(dtos, /string DisplayNameId/);
  assert.match(dtos, /character\.HeroRank >= 3[\s\S]*?return path\.PathId/);
  assert.match(dtos, /character\.SoldierRank >= 2[\s\S]*?return character\.Definition\.Rank2NameId/);
  assert.match(app, /characterOrKey\.displayNameId \|\| characterOrKey\.key/);
});

test('all advanced hero and soldier names are localized', () => {
  for (const locale of [zh, ja]) {
    for (const id of [...soldierNameIds, ...heroNameIds])
      assert.ok(locale.characters[id], `missing characters.${id}`);
  }
});
