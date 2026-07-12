const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const app = fs.readFileSync('wwwroot/app.js', 'utf8');
const styles = fs.readFileSync('wwwroot/styles.css', 'utf8');

test('status inspector aligns to the character inspector top edge and stays within the stage', () => {
  assert.match(app, /const characterTop = Number\.parseFloat\(ui\.inspector\.style\.top\) - ui\.inspector\.offsetHeight \/ 2/);
  assert.match(app, /\{ topEdge: characterTop \}/);
  assert.doesNotMatch(app, /panel\.style\.maxHeight/);
  assert.match(app, /new ResizeObserver/);
  assert.match(styles, /\.character-inspector,.status-inspector\s*\{[\s\S]*?max-height:none;[\s\S]*?overflow:visible/);
  assert.match(styles, /\.aura-group:hover \.aura-detail,[\s\S]*?overflow:visible/);
});
