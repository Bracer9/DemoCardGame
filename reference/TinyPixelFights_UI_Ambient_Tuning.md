# Tiny Pixel Fights UI Ambient Tuning

本文件记录战斗界面的“战场呼吸层”“可行动提示”“中央 HUD 材质感”的调参方式。

这些参数只影响视觉表现，不影响战斗逻辑、事件绑定、AP、技能、在线同步或数值计算。

## 1. 共用呼吸节拍

位置：`wwwroot/styles.css` 顶部 `:root`。

```css
--ready-pulse-duration: 1.35s;
```

这个参数同时控制：

- 左侧 AP 珠子的可用呼吸。
- 卡牌 cost 珠子的可用呼吸。
- 可行动卡牌内部旧金令光的呼吸。

调法：

- 想整体慢一点：增大，例如 `1.6s`。
- 想整体快一点：减小，例如 `1.1s`。

不要分别去改 AP、cost、可行动卡的动画时长，否则它们会变回“各跳各的”。

## 2. 战场 ambient overlay

位置：`wwwroot/styles.css` 顶部 `:root`。

```css
--battlefield-ambient-opacity: .82;
--battlefield-candle-opacity: .52;
--battlefield-sigil-opacity: .36;
--battlefield-dust-opacity: .28;
--battlefield-ambient-duration: 6.8s;
--battlefield-dust-duration: 12s;
```

调法：

- 整张地图太亮或太花：降低 `--battlefield-ambient-opacity`。
- 烛火感太强：降低 `--battlefield-candle-opacity`。
- 中央罗盘/魔法阵呼吸太明显：降低 `--battlefield-sigil-opacity`。
- 灰尘/细碎光纹太明显：降低 `--battlefield-dust-opacity`。
- 环境变化太快：增大 `--battlefield-ambient-duration` 和 `--battlefield-dust-duration`。

实现位置：

```css
.battlefield-surface::before
.battlefield-surface::after
@keyframes battlefieldCandleBreath
@keyframes battlefieldDustDrift
```

## 3. 上下金色 particles / 空气流动

HTML 层：

```html
<div class="battlefield-atmosphere battlefield-atmosphere-bottom" aria-hidden="true"></div>
<div class="battlefield-atmosphere battlefield-atmosphere-top" aria-hidden="true"></div>
```

CSS 参数：

```css
--battlefield-particle-opacity: .46;
--battlefield-particle-duration: 13s;
--battlefield-particle-top: 78px;
--battlefield-particle-bottom: 86px;
--battlefield-particle-height: 310px;
```

调法：

- 金粉太明显：降低 `--battlefield-particle-opacity`。
- 金粉太慢/太快：调整 `--battlefield-particle-duration`，数值越大越慢。
- 调整上方粒子位置：改 `--battlefield-particle-top`。
- 调整下方粒子位置：改 `--battlefield-particle-bottom`。
- 扩大/缩小粒子存在区域：改 `--battlefield-particle-height`。

注意：particle 使用 `background-position` 按 tile 尺寸无缝滚动，避免循环回到起点时出现瞬移。

## 4. 可行动卡牌旧金令光

位置：`wwwroot/styles.css` 顶部 `:root`。

```css
--actionable-card-corner-size: 52px;
--actionable-card-aura-opacity: .50;
--actionable-card-aura-spread: 34px;
```

调法：

- 可行动提示太弱：提高 `--actionable-card-aura-opacity` 或 `--actionable-card-aura-spread`。
- 可行动提示太抢人物：降低 `--actionable-card-aura-opacity`。
- 四角令光太长/太短：调整 `--actionable-card-corner-size`。
- 呼吸速度：改 `--ready-pulse-duration`。

当前令光不会再让整圈 box-shadow 高频扩缩，也不会让角线 filter/drop-shadow 高频闪。角线保持稳定，只有卡面内部暖光跟随 AP/cost 的节拍做低频明暗呼吸。

作用选择器：

```css
.fighter-card.can-act:not(.selected):not(.target-selected):not(.drop-ready):not(.acted):not(.defeated)
```

只作用于“当前可行动、未选择、未行动、未死亡”的卡。选中卡、目标卡、已行动卡不会叠加这层提示。

## 5. 中央 HUD 金属质感

位置：`wwwroot/styles.css` 顶部 `:root`。

```css
--command-hud-sheen-cycle: 6s;
--command-hud-sheen-travel-duration: 1.6s;
--command-hud-sheen-opacity: .30;
--command-hud-sheen-height: 46px;
--command-hud-sheen-width: 72px;
--command-hud-sheen-start-x: -48px;
--command-hud-sheen-end-x: 136px;
```

这次已经把“出现频率”和“单次滑行速度”拆开：

- `--command-hud-sheen-cycle`：每隔多久出现一次扫光。数值越大，出现越少。
- `--command-hud-sheen-travel-duration`：一次扫光从左滑到右要多久。数值越大，滑得越慢。

其他调法：

- TURN/ROUND 扫光太亮：降低 `--command-hud-sheen-opacity`。
- 扫光高度太高/太矮：调整 `--command-hud-sheen-height`。
- 扫光块太宽/太窄：调整 `--command-hud-sheen-width`。
- 扫光轨迹左端太靠外/太靠内：调整 `--command-hud-sheen-start-x`。
- 扫光轨迹右端太靠外/太靠内：调整 `--command-hud-sheen-end-x`。

实现位置：

```html
<i class="hud-sheen" aria-hidden="true"></i>
```

```css
.hud-sheen
.shield-command::after
.end-turn::after
@keyframes commandPlateLineBreath
@keyframes commandLabelBreath
@keyframes commandMainBreath
```

```js
startHudSheenLoop()
```

说明：

- TURN/ROUND 的扫光由 `wwwroot/app.js` 的 `startHudSheenLoop()` 驱动。
- CSS 变量只负责参数，不再用一条 CSS keyframe 同时控制“多久出现一次”和“滑多快”。
- 防御和结束回合不做整块扫光，避免 PNG 透明边缘被照出来。
- 两侧 HUD 只保留细旧金线与文字的低频暗金呼吸。

## 6. 低 HP 暗红呼吸

`app.js` 会在角色满足低 HP 条件时给卡牌加：

```css
low-hp
```

当前阈值沿用 voice system 的 `select-low-hp` 判断：当前 HP 大于 0，且小于等于最大 HP 的 25% 向上取整。

视觉参数：

```css
--low-hp-card-pulse-duration: .95s;
```

调法：

- 濒死感太强：增大 duration 或降低 `.fighter-card.low-hp` 的暗红 box-shadow。
- 濒死感太弱：缩短 duration 或增强暗红 box-shadow。

## 7. 未来：战场天气系统

现在的 `battlefield-atmosphere` 只是表现层，不改变规则。

如果之后正式加入战场天气 / round buff，建议建立稳定概念：

- weather / battlefield-effect 使用稳定英文 ID。
- 规则层决定它是否影响 AP、Cost、攻击、治疗、盾或回合开始事件。
- UI 层根据 ID 显示对应气氛层，例如火星、薄雾、魔力潮汐、王都纹章等。
- 日志、hover 说明、音效、图标和双语文本同步更新。

## 8. Reduced motion

如果系统设置了：

```css
prefers-reduced-motion: reduce
```

战场呼吸、金色 particles、可行动卡牌令光、中央 HUD 扫光、两侧 HUD 呼吸、低 HP 呼吸等非必要循环动画会关闭。
