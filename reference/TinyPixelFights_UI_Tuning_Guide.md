# Tiny Pixel Fights UI 调参说明

本文记录当前战斗界面的主要 CSS 调整入口。现在项目采用“1920×1080 虚拟舞台 + 外层等比缩放”的方式：

- `#game-root` 固定为 `1920px × 1080px`。
- 浏览器窗口大小只影响外层整体缩放。
- 舞台内部不要再使用 `vw / vh / clamp()` 控制战斗布局。
- 在 Windows 1920×1080 上调好后，MacBook / 其他桌面设备应看到同一套内部布局，只是整体等比缩放。

主要修改文件：

```text
wwwroot/styles.css
wwwroot/app.js
```

---

## 目录

- [1. 调参原则](#1-调参原则)
- [2. 虚拟舞台](#2-虚拟舞台)
- [3. 卡牌大小与位置](#3-卡牌大小与位置)
- [4. 卡牌内部人物立绘](#4-卡牌内部人物立绘)
- [5. 顶部 TURN / ROUND](#5-顶部-turn--round)
- [6. 右上菜单](#6-右上菜单)
- [7. 玩家名字](#7-玩家名字)
- [8. 中央防御 / 结束回合按钮](#8-中央防御--结束回合按钮)
- [9. 左上 Action Point](#9-左上-action-point)
- [10. Hover 详情框](#10-hover-详情框)
- [11. 预览框与日志](#11-预览框与日志)
- [12. 字体与颜色](#12-字体与颜色)
- [13. 攻击拖拽箭头](#13-攻击拖拽箭头)
- [14. 不要再改的东西](#14-不要再改的东西)

---

## 1. 调参原则

优先改 `:root` 里的变量，不要先去改组件 selector。

现在 `styles.css` 顶部有两块最重要的变量区：

```css
/* Battle HUD tuning knobs: plain px values for manual visual tuning. */
/* 1920x1080 virtual-stage tuning. */
```

规则：

- 想整体调位置、大小、字体，先找变量。
- 想临时试效果，可以在 DevTools 里改变量；满意后再复制回 `styles.css`。
- 不要恢复 `vw / vh / clamp()` 来调战斗界面。
- 竖屏只显示旋转提示，不做完整适配。

---

## 2. 虚拟舞台

基础变量：

```css
--stage-width: 1920px;
--stage-height: 1080px;
```

不要轻易修改这两个值。它们是整个“等比缩放方案”的基准。

相关结构：

```html
<div id="game-viewport">
  <div id="game-root">
    <div id="app">...</div>
  </div>
</div>
```

`app.js` 会根据窗口大小计算 `#game-root` 的 `scale()`。鼠标坐标也会被换算回 1920×1080 舞台坐标，所以拖拽、箭头、特效定位才会正常。

---

## 3. 卡牌大小与位置

### 全体卡牌大小

```css
--card-w-base: 338px;
--card-aspect: 1.22;
```

- `--card-w-base`：我方卡牌基础宽度。
- `--card-aspect`：卡牌高宽比。

想整体放大/缩小卡牌，优先改：

```css
--card-w-base
```

### 敌方卡牌大小

```css
--opponent-card-scale: .72;
```

敌方卡牌宽度 = `--card-w-base × --opponent-card-scale`。

### 敌方整排位置

```css
--opponent-cards-x: 0px;
--opponent-cards-y: 105px;
```

- `x` 正数：向右。
- `x` 负数：向左。
- `y` 正数：向下。
- `y` 负数：向上。

### 我方手牌整体位置

主要 selector：

```css
.active-zone
```

当前关键值：

```css
.active-zone {
  bottom: 34px;
  width: 1180px;
  max-width: 1180px;
}
```

- 想让我方整排牌上移：增加 `bottom`。
- 想让我方整排牌下移：减少 `bottom`。
- 想让扇形摊得更宽：增加 `width / max-width`。
- 想让扇形更紧：减少 `width / max-width`。

### 我方扇形手牌姿态

```css
--active-hand-y: -36px;
--active-selected-lift: 116px;
--active-hand-edge-y: calc(var(--card-h) * .095);
--active-hand-center-y: calc(var(--card-h) * .028);
```

- `--active-hand-y`：整组手牌上下偏移。
- `--active-selected-lift`：选中卡牌向上抽出的高度。
- `--active-hand-edge-y`：两侧卡牌的额外下沉。
- `--active-hand-center-y`：中间卡牌的额外下沉。

单张卡牌旋转角度在：

```css
.active-zone .fighter-card:nth-child(...)
```

---

## 4. 卡牌内部人物立绘

当前人物立绘统一通过 CSS 控制，不再使用旧黑白立绘。

常见入口：

```css
.portrait-wrap
.portrait-wrap img
.fighter-card.full-art-card
```

如果想统一调整所有人物大小，找全局 portrait 相关变量或 `.portrait-wrap img` 的 `transform / object-position`。不要给单独角色写特殊缩放，除非素材本身确实异常且只是临时测试。

行动结束 / 阵亡的黑白效果应继续用 CSS filter 实现，不要切回旧图。

### 卡牌攻击力数字

攻击力区域指卡片左上角的双剑图标和攻击力数字。现在可以直接在 `:root` 里调：

```css
--card-attack-top: 10px;
--card-attack-left: 12px;
--card-attack-gap: 7px;
--active-card-attack-scale: 1.18;
--opponent-card-attack-scale: 1;
--card-attack-font-family: "Cinzel", "Trajan Pro", Georgia, "Times New Roman", serif;
--card-attack-font-size: 36px;
--card-attack-color: #17110d;
--card-attack-text-shadow: 0 1px 0 #f0d79d, 0 -1px 0 #fff2c733, 1px 0 0 #f0d79d, -1px 0 0 #f0d79d, 0 2px 2px #000, 0 0 8px #f2d08370;
--card-attack-icon-size: 29px;
--card-attack-icon-color: #18110d;
--card-attack-icon-shadow: 0 1px 0 #ead19a, 1px 0 0 #ead19a, -1px 0 0 #ead19a, 0 2px 2px #000, 0 0 6px #f2d0835c;
```

调法：

- 攻击力整体位置：改 `--card-attack-top / --card-attack-left`。
- 双剑和数字的距离：改 `--card-attack-gap`。
- 我方攻击力整体大小：改 `--active-card-attack-scale`。
- 敌方攻击力整体大小：改 `--opponent-card-attack-scale`。
- 数字大小：改 `--card-attack-font-size`。
- 数字字体：改 `--card-attack-font-family`。
- 数字颜色：改 `--card-attack-color`。
- 数字描边/阴影：改 `--card-attack-text-shadow`。
- 双剑大小：改 `--card-attack-icon-size`。
- 双剑颜色：改 `--card-attack-icon-color`。
- 双剑阴影：改 `--card-attack-icon-shadow`。

如果卡片被选为攻击目标时出现红色目标框，攻击力区域会保持在目标框角标之上，避免红框压住数字导致颜色或发光变怪。

### 卡牌 HP 区域大小

HP 区域指卡片下方中间的血球、当前 HP 数字以及右侧 `/最大 HP`。现在可以按敌我双方分别调节：

```css
--active-card-hp-scale: 1.22;
--opponent-card-hp-scale: 1;
```

调法：

- 我方 HP 区域变大/变小：改 `--active-card-hp-scale`。
- 敌方 HP 区域变大/变小：改 `--opponent-card-hp-scale`。
- `1` 是原始大小。
- `1.15` 是轻微放大。
- `1.22` 是当前推荐值，让我方近景卡牌的 HP 信息比敌方更明显。

这个 scale 只作用于 `.stat-orb.hp`，不会改变卡片大小、立绘大小、攻击力、cost 点或状态图标。

---

## 5. 顶部 TURN / ROUND

变量：

```css
--hud-turn-frame-width: 360px;
--hud-turn-frame-top: 41px;
--hud-turn-label-size: 12px;
--hud-turn-number-size: 16px;
--hud-turn-padding-top: 22px;
--hud-turn-padding-x: 54px;
--hud-turn-padding-bottom: 28px;
--hud-turn-label-y: 0px;
--hud-turn-number-y: 0px;
```

调法：

- 框整体太小/太大：改 `--hud-turn-frame-width`。
- 框整体上下：改 `--hud-turn-frame-top`。
- TURN / ROUND 标签大小：改 `--hud-turn-label-size`。
- 数字大小：改 `--hud-turn-number-size`。
- 文字整体在框里上下不对：改 `--hud-turn-padding-top` 或 `--hud-turn-label-y / --hud-turn-number-y`。

---

## 6. 右上菜单

变量：

```css
--hud-top-menu-width: 250px;
--hud-top-menu-right: 24px;
--hud-top-menu-top: 12px;
--hud-top-menu-button-size: 14px;
```

控制“音声 / NEW”的位置、大小和字体。

---

## 7. 玩家名字

当前已经去掉玩家铭牌图片，只保留文字。

变量：

```css
--hud-player-right: 28px;
--hud-player-enemy-top: 39px;
--hud-player-you-bottom: 22px;
--hud-player-gap: 10px;
--hud-player-label-size: 16px;
--hud-player-name-size: 24px;
--hud-player-text-y: 0px;
```

调法：

- 两个玩家名整体左右：改 `--hud-player-right`。
- 敌方名字上下：改 `--hud-player-enemy-top`。
- 我方名字上下：改 `--hud-player-you-bottom`。
- `ENEMY / YOU` 与玩家名间距：改 `--hud-player-gap`。
- 标签字体：改 `--hud-player-label-size`。
- 玩家名字字体：改 `--hud-player-name-size`。

---

## 8. 中央防御 / 结束回合按钮

变量：

```css
--command-label-size: 8px;
--command-main-size: 14px;
--command-content-y: 0px;
--command-label-y: 0px;
--command-main-y: 0px;
--command-button-width: 300px;
--command-button-height: calc(var(--command-button-width) / 3);
--command-button-padding-x: 34px;
--command-center-gap: 220px;
--command-deck-top: 50%;
```

调法：

- 两个按钮整体大小：改 `--command-button-width`。
- 两个按钮间距：改 `--command-center-gap`。
- 两个按钮整体上下：改 `--command-deck-top`。
- 小字大小：改 `--command-label-size`。
- 主文字大小：改 `--command-main-size`。
- 按钮内文字整体上下：改 `--command-content-y`。
- 小字单独上下：改 `--command-label-y`。
- 主文字单独上下：改 `--command-main-y`。

左右按钮应使用同一框体素材，其中一边镜像。不要再让左右使用不同底图导致高度不齐。

---

## 9. 左上 Action Point

当前 AP HUD 不再使用外框图片，只保留：

- `ACTION POINT` 文字。
- AP 光球。

变量：

```css
--ap-panel-top: 120px;
--ap-title-size: 30px;
--ap-orb-size: 36px;
--ap-title-top: -39px;
--ap-title-left: 66px;
--ap-orb-top: 20px;
--ap-orb-left: 63px;
--hud-ap-pip-size: 13px;
--hud-ap-pip-gap: 7px;
```

调法：

- 整个 AP 区上下：改 `--ap-panel-top`。
- ACTION POINT 字大小：改 `--ap-title-size`。
- AP 光球大小：改 `--ap-orb-size` 或 `--hud-ap-pip-size`。
- 文字位置：改 `--ap-title-top / --ap-title-left`。
- 光球位置：改 `--ap-orb-top / --ap-orb-left`。
- 光球间距：改 `--hud-ap-pip-gap`。

AP 光球应与卡牌上方 cost 点使用同一视觉语言。

---

## 10. Hover 详情框

变量：

```css
--hud-estimation-frame-brown: url("/assets/ui/hud/battle/ui_estimation_frame_brown.png");
--hud-estimation-frame-light: url("/assets/ui/hud/battle/ui_estimation_frame_light.png");
--hud-estimation-frame-dark: url("/assets/ui/hud/battle/ui_estimation_frame_dark.png");
--character-inspector-bg: var(--hud-estimation-frame-brown);
--status-inspector-bg: var(--hud-estimation-frame-brown);
--shield-inspector-bg: var(--hud-estimation-frame-brown);
--character-inspector-width: 380px;
--status-inspector-width: 350px;
--shield-inspector-width: 430px;
--inspector-padding-x: 30px;
--inspector-padding-y: 28px;
--inspector-guide-inset: 18px;
--inspector-frame-zoom: 115%;
--shield-inspector-padding-x: 33px;
--shield-inspector-padding-y: 31px;
--shield-inspector-padding-bottom: 28px;
--shield-inspector-guide-inset: 18px;
--shield-inspector-frame-zoom: 115%;
--inspector-title-size: 26px;
--inspector-body-size: 14px;
```

说明：

- 左侧角色属性/技能详情：`--character-inspector-width`。
- 右侧 buff/debuff：`--status-inspector-width`。
- 防御按钮 hover 说明：`--shield-inspector-width`。
- 标题字体：`--inspector-title-size`。
- 正文字体：`--inspector-body-size`。
- 左右 hover 框内容安全区：`--inspector-padding-x / --inspector-padding-y`。
- 防御 hover 框内容安全区：`--shield-inspector-padding-x / --shield-inspector-padding-y / --shield-inspector-padding-bottom`。
- 内侧参考线位置：`--inspector-guide-inset / --shield-inspector-guide-inset`。
- PNG 透明边缘裁切：`--inspector-frame-zoom / --shield-inspector-frame-zoom`。

### Hover 框内容溢出素材边缘时

这些 PNG 素材外圈带有透明边缘，视觉边框不等于元素盒子的真实边界。如果文字、图标或列表看起来压到边框外，优先调安全区 padding：

```css
--inspector-padding-x: 34px;
--inspector-padding-y: 32px;
```

数值越大，内容越往中间收。代价是可用内容空间变小；如果收进去后内容太挤，再同步加大：

```css
--character-inspector-width
--status-inspector-width
--shield-inspector-width
```

如果只是那条淡金色内框线位置不对，调：

```css
--inspector-guide-inset
--shield-inspector-guide-inset
```

这个只影响装饰参考线，不直接改变内容位置。

### Hover 框边缘出现透明脏边时

如果框大小已经包住内容，但边缘能看到 PNG 自带的半透明纹理、透明残边或地图透出来的脏边，不要继续改 width/padding，改背景裁切倍率：

```css
--inspector-frame-zoom: 112%;
--shield-inspector-frame-zoom: 112%;
```

调法：

- 还看到脏边：加大到 `116%` 或 `120%`。
- 边框花纹被裁太多：降低到 `108%`。
- 这个参数只放大背景素材，不改变 HTML 框大小，也不改变文字位置。

### Hover 框材质替换

三张可用材质：

```css
--hud-estimation-frame-brown
--hud-estimation-frame-light
--hud-estimation-frame-dark
```

当前默认：

```css
--character-inspector-bg: var(--hud-estimation-frame-brown);
--status-inspector-bg: var(--hud-estimation-frame-brown);
--shield-inspector-bg: var(--hud-estimation-frame-brown);
```

如果想让左侧角色详情框换成深色：

```css
--character-inspector-bg: var(--hud-estimation-frame-dark);
```

如果想让右侧 buff/debuff 框换成亮色：

```css
--status-inspector-bg: var(--hud-estimation-frame-light);
```

如果三处都想统一成同一张素材，就把三个变量都指向同一个 frame。

---

## 11. 预览框与日志

攻击预览：

```css
--hud-estimation-frame-brown: url("/assets/ui/hud/battle/ui_estimation_frame_brown.png");
--hud-estimation-frame-light: url("/assets/ui/hud/battle/ui_estimation_frame_light.png");
--hud-estimation-frame-dark: url("/assets/ui/hud/battle/ui_estimation_frame_dark.png");
--preview-panel-bg: var(--hud-estimation-frame-dark);
--preview-panel-width: 430px;
--preview-panel-padding-x: 34px;
--preview-panel-padding-y: 31px;
--preview-panel-guide-inset: 18px;
--preview-panel-frame-zoom: 112%;
```

攻击预测栏现在使用 `assets/ui/hud/battle/` 下的 estimation frame 材质。

当前默认：

```css
--preview-panel-bg: var(--hud-estimation-frame-dark);
```

如果想快速试其他两张：

```css
--preview-panel-bg: var(--hud-estimation-frame-brown);
--preview-panel-bg: var(--hud-estimation-frame-light);
--preview-panel-bg: var(--hud-estimation-frame-dark);
```

调法：

- 预测栏整体宽度：`--preview-panel-width`。
- 预测栏内容与边框距离：`--preview-panel-padding-x / --preview-panel-padding-y`。
- 背景材质：`--preview-panel-bg`。
- 淡金色内框参考线位置：`--preview-panel-guide-inset`。
- PNG 透明边缘裁切：`--preview-panel-frame-zoom`。
- 内部伤害格样式：`.forecast-box`。
- 技能预测格样式：`.preview-skill`。
- 攻击确认按钮：`.confirm-attack`。

### 预测栏内容溢出素材边缘时

优先调：

```css
--preview-panel-padding-x: 40px;
--preview-panel-padding-y: 36px;
```

数值越大，内容越往材质中央收。  
如果内容收进去以后整体太窄，再调大：

```css
--preview-panel-width
```

如果只是内框线和素材花纹不对齐，调：

```css
--preview-panel-guide-inset
```

### 预测栏边缘出现透明脏边时

如果素材边缘出现奇怪的半透明纹理，说明 PNG 外圈透明区被铺满显示了。此时调：

```css
--preview-panel-frame-zoom: 112%;
```

调法：

- 还看到边缘残纹：提高到 `116%` 或 `120%`。
- 外框花纹被裁得太狠：降低到 `108%`。
- 这个参数只裁背景素材，不影响框实际大小、文字大小或内部排版。

### 预测栏关闭方式

预测栏右上角关闭按钮已经取消。现在预测栏会在以下情况关闭：

- 点击预测栏外的战场空白区域。
- 重新选择攻击者或目标。
- 执行攻击后状态刷新。
- 回合、房间或游戏状态变化导致面板关闭。

战况日志：

```css
--battle-log-width: 330px;
--battle-log-right: 28px;
--battle-log-bottom: 150px;
--battle-log-tab-size: 9px;
--battle-log-entry-size: 9px;
```

---

## 12. 字体与颜色

字体变量：

```css
--font-display
--font-ui
```

日文默认使用：

```css
--font-display: "Yu Mincho", "Hiragino Mincho ProN", "Noto Serif JP", "Noto Serif SC", Georgia, serif;
--font-ui: "Yu Gothic", "Hiragino Kaku Gothic ProN", "Noto Sans JP", "Noto Sans SC", "Microsoft YaHei", sans-serif;
```

中文模式会通过：

```css
html[lang="zh-CN"]
```

切换到更适合中文的字体栈。

常用颜色变量：

```css
--ui-ivory
--ui-muted
--ui-gold
--ui-dark-gold
--ui-red
--ui-dark-red
--ui-cyan
--ui-border
```

---

## 13. 攻击拖拽箭头

攻击拖拽箭头的锚点在 [wwwroot/app.js](../wwwroot/app.js) 里，不在 CSS 变量里。

### 出发点：我方卡牌上的位置

函数：

```js
function startAttackArrow(card) {
  const rect = stageRect(card);
  dragArrowOrigin = {
    x: rect.left + rect.width / 2,
    y: rect.top + rect.height / 2
  };
}
```

调法：

```js
x: rect.left + rect.width * 0.5
y: rect.top + rect.height * 0.5
```

- `x` 的 `0.5` 是卡牌横向正中。
- `y` 的 `0.5` 是卡牌纵向正中。
- 想让箭头从更上方出发：把 `0.5` 改小，例如 `0.42`。
- 想让箭头从更下方出发：把 `0.5` 改大，例如 `0.58`。

当前推荐值：双方都用 `0.5`，也就是正中心。这样最稳定，也不容易出现折返感。

注意：直接拖动一张未预先选中的我方卡牌时，代码会额外扣掉 `--active-selected-lift`，让箭头出发点按“卡牌被抽起后的位置”计算。否则浏览器还没来得及应用 selected transform 时，箭头会从原始卡牌位置出发，看起来偏下。

### 末端：对方卡牌上的吸附位置

函数：

```js
function updateAttackArrow(pointerX, pointerY, snapTarget = null) {
  if (snapTarget) {
    const rect = stageRect(snapTarget);
    endX = rect.left + rect.width / 2;
    endY = rect.top + rect.height / 2;
  }
}
```

调法同上：

```js
endX = rect.left + rect.width * 0.5;
endY = rect.top + rect.height * 0.5;
```

- `0.5 / 0.5`：对方卡牌正中。
- `0.5 / 0.4`：对方卡牌中上。
- `0.5 / 0.6`：对方卡牌中下。

如果箭头看起来“贴脸太近”或“折返”，优先微调 `y`，不要先改曲线公式。

### 箭头粗细与颜色

视觉在 [wwwroot/styles.css](../wwwroot/styles.css)：

```css
.attack-drag-arrow .arrow-shadow
.attack-drag-arrow .arrow-core
.attack-drag-arrow marker path
.attack-drag-arrow .arrow-origin
.attack-drag-arrow.locked .arrow-core
```

常用调法：

- 线条粗细：改 `.arrow-core` 的 `stroke-width`。
- 阴影厚度：改 `.arrow-shadow` 的 `stroke-width`。
- 虚线节奏：改 `.arrow-core` 的 `stroke-dasharray`。
- 普通状态颜色：改 `.arrow-core` 的 `stroke`。
- 锁定目标时颜色：改 `.attack-drag-arrow.locked .arrow-core` 的 `stroke`。
- 箭头头部颜色：改 `.attack-drag-arrow marker path`。

---

## 14. 不要再改的东西

除非决定废弃虚拟舞台方案，否则不要恢复这些写法：

```css
vw
vh
clamp(...)
@media (max-width: ...)
@media (max-height: ...)
```

例外：

- 竖屏提示可以保留 media query。
- 非战斗页面如果以后独立做响应式，可以另开页面级规则。

目前 `styles.css` 中战斗布局只保留一个竖屏提示 media query：

```css
@media (orientation: portrait) and (max-width: 1100px)
```

这就是为了避免不同设备触发不同卡牌/HUD布局。
