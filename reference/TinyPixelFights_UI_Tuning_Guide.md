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
- [5. TURN / ROUND](#5-turn--round)
- [6. 右上菜单](#6-右上菜单)
- [7. 玩家名字](#7-玩家名字)
- [8. 中央防御 / 结束回合按钮](#8-中央防御--结束回合按钮)
- [9. Action Point](#9-action-point)
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
--active-card-scale: 0.95;
--card-aspect: 1.22;
```

- `--card-w-base`：卡牌基础宽度。
- `--active-card-scale`：只控制我方手牌大小。当前为 `0.95`。
- `--card-aspect`：卡牌高宽比。

想整体放大/缩小卡牌，优先改：

```css
--card-w-base
```

想只放大/缩小我方手牌，改：

```css
--active-card-scale
```

例如：

```css
--active-card-scale: 1.05; /* 我方手牌放大 5% */
--active-card-scale: .96;  /* 我方手牌缩小 4% */
```

### 敌方卡牌大小

```css
--opponent-card-scale: 0.82;
```

敌方卡牌宽度 = `--card-w-base × --opponent-card-scale`。

当前我方手牌大小来自：

```css
--card-w-base
--active-card-scale
--card-w: calc(var(--card-w-base) * var(--active-card-scale))
```

也就是说：

- 想同时改我方和敌方的基础卡牌大小：改 `--card-w-base`。
- 想只改我方大小：改 `--active-card-scale`。
- 想只改敌方大小：改 `--opponent-card-scale`。

### 敌方整排位置

```css
--opponent-cards-x: 0px;
--opponent-cards-y: 26px;
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
--active-hand-y: -28px;
--active-selected-lift: 116px;
--card-selected-motion-duration: .48s;
--card-selected-motion-ease: cubic-bezier(.18, 1.18, .22, 1);
--active-hand-edge-y: calc(var(--card-h) * .095);
--active-hand-center-y: calc(var(--card-h) * .028);
```

- `--active-hand-y`：整组手牌上下偏移。
- `--active-selected-lift`：选中卡牌向上抽出的高度。
- `--card-selected-motion-duration`：选中卡牌抽出/放大的动画时长。通常不要动；selected 后卡本体应保持稳定，不参与伪 Live2D 呼吸。
- `--card-selected-motion-ease`：选中卡牌抽出的缓动曲线。一般不需要动，除非想减少/增加末端弹性。
- `--active-hand-edge-y`：两侧卡牌的额外下沉。
- `--active-hand-center-y`：中间卡牌的额外下沉。

单张卡牌旋转角度在：

```css
.active-zone .fighter-card:nth-child(...)
```

### 卡牌 hover 浮动

hover 我方可行动且未选中的卡牌时，会有轻微浮动，让卡片像被手指自然托起。卡牌被选中 `.selected` 后，卡片本体不再继续浮动，因为 selected 状态下角色立绘本身会呼吸；两种动效叠在一起会显得晕和不稳定。

```css
--card-motion-duration: .12s;
--card-motion-ease: cubic-bezier(.05, .92, .2, 1);
--card-float-hover-lift: 12px;
--card-float-distance: 8px;
--card-float-duration: 2.1s;
--card-float-delay: .12s;
```

调法：

- hover 起手托起太慢/太快：改 `--card-motion-duration`。数值越小，鼠标放上去时卡片越快抬起。
- hover 起手曲线太拖/太冲：改 `--card-motion-ease`。一般优先只调 duration。
- hover 时基础抬起高度：改 `--card-float-hover-lift`。
- 浮动上下幅度：改 `--card-float-distance`。这里使用正数，数值越大浮动越明显。
- 浮动速度：改 `--card-float-duration`。数值越小越快。
- 出鞘结束到慢速漂浮开始的间隔：改 `--card-float-delay`。通常保持接近 `--card-motion-duration`。

实现上不要用 hover animation 直接改整张卡的 `transform`，因为卡牌的扇形位置、选中抽出高度、目标锁定都共用 `transform`，会和 transition 抢控制权，造成“等半秒后突然上拱”的感觉。当前做法是：

- `transform` 只负责 hover 当下的“卡出鞘”：快速上抬，到顶端减速收住。
- `@keyframes cardHoverFloat` 只动画独立的 `translate` 属性，负责托起后的慢速漂浮。当前 loop 延迟到出鞘结束后才启动，并且从顶端往下沉再回升，避免刚到顶端又继续往上冲。
- hover 浮动 selector 是 `.fighter-card.can-act:not(.selected):hover`。
- selected 状态会强制 `translate:0 0`，不使用卡片浮动，只保留选中抽出和角色立绘呼吸。

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

### 卡牌人物轻微呼吸

当前试验版使用 CSS 伪 Live2D 呼吸感，只作用于：

- `full-art-card`
- 被选中的卡牌 `.selected`
- 未阵亡

未选中的卡牌不会呼吸。这个版本的目标是让“当前被唤醒/拿起的卡牌”更有活人感，而不是让全场一直动。

变量：

```css
--selected-portrait-breathe-duration: 3.8s;
--selected-portrait-breathe-scale: 1.014;
--selected-portrait-breathe-y: -3px;
```

调法：

- 呼吸速度：改 `--selected-portrait-breathe-duration`。数值越小越快。
- 呼吸放大幅度：改 `--selected-portrait-breathe-scale`。
- 呼吸上下起伏：改 `--selected-portrait-breathe-y`。
- 如果觉得像“上下跳”，先减小 `breathe-y`，例如 `-2px`。
- 如果觉得太像整张图在缩放，先减小 `breathe-scale`，例如 `1.012`。
- 当前 keyframe 在吸气顶点有短暂停顿，让 selected 角色有轻微呼吸感，而不是机械上下移动。顶点停留目前比上一版短一些，避免人物放大后“呆住”。默认参数偏克制；如果想更明显，优先小幅增加 `--selected-portrait-breathe-scale` 到 `1.018` 左右，而不是大幅增加上下位移。

不同卡位仍有轻微错相 delay。系统设置 `prefers-reduced-motion: reduce` 时会自动关闭呼吸动画。

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

### 卡牌物防 / 魔防数字

物防 / 魔防区域指卡片左下角的 `物防 数字 / 魔防 数字`。它和攻击力使用同一套字体、颜色和阴影，只是独立控制位置与大小。

```css
--card-defense-left: 14px;
--card-defense-right: auto;
--card-defense-bottom: 34px;
--active-card-defense-scale: 1.10;
--opponent-card-defense-scale: .92;
--card-defense-label-size: 13px;
--card-defense-number-size: 21px;
--card-defense-gap-x: 5px;
--card-defense-gap-y: 1px;
```

调法：

- 物防 / 魔防整体左右：左下布局优先改 `--card-defense-left`；如果要改到右侧，把 `--card-defense-right` 设为具体 px，并把 `--card-defense-left` 设为 `auto`。
- 物防 / 魔防整体上下：改 `--card-defense-bottom`。
- 我方物防 / 魔防整体大小：改 `--active-card-defense-scale`。
- 敌方物防 / 魔防整体大小：改 `--opponent-card-defense-scale`。
- `P / M` 标签大小：改 `--card-defense-label-size`。
- 防御数字大小：改 `--card-defense-number-size`。
- 标签和数字的左右距离：改 `--card-defense-gap-x`。
- 两行之间的距离：改 `--card-defense-gap-y`。

注意：防御栏必须覆盖旧 `.stat-orb span` 的绝对定位，否则 `P / M` 会叠在一起。不要把它改回 `position:absolute`。

如果防御值为负数，卡面和详情会显示红色绝对值，不显示负号。颜色由 `.defense-negative` 控制。

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

## 5. TURN / ROUND

变量：

```css
--hud-midline-y: 543px;
--hud-turn-frame-left: 815px;
--hud-turn-frame-top: calc(var(--hud-midline-y) - 72.5px);
--hud-turn-frame-width: 290px;
--hud-turn-frame-height: 145px;
--hud-turn-label-size: 12px;
--hud-turn-number-size: 16px;
--hud-turn-padding-top: 22px;
--hud-turn-padding-x: 39px;
--hud-turn-padding-bottom: 28px;
--hud-turn-label-y: 0px;
--hud-turn-number-y: 0px;
```

调法：

- TURN / ROUND 这个 frame 整体左右位置：改 `--hud-turn-frame-left`。
- 中排 AP 光球、TURN / ROUND、BP 的共同水平线：改 `--hud-midline-y`。
- TURN / ROUND 这个 frame 整体上下位置：优先跟随 `--hud-midline-y`，单独微调才改 `--hud-turn-frame-top`。数值越小越靠上。
- 框整体宽度：改 `--hud-turn-frame-width`。
- 框整体高度：改 `--hud-turn-frame-height`。
- 当前 TURN / ROUND frame 宽高已经可以独立调整，不再强制 `高度 = 宽度 / 2`。如果高度改得过多，PNG frame 会有轻微拉伸；小范围微调没问题。
- TURN / ROUND 标签大小：改 `--hud-turn-label-size`。
- 数字大小：改 `--hud-turn-number-size`。
- 文字整体在框里上下不对：改 `--hud-turn-padding-top` 或 `--hud-turn-label-y / --hud-turn-number-y`。

现在 TURN / ROUND 和 Action Point 不再强制绑定。TURN / ROUND 当前放在防御与结束回合按钮之间的中部位置，单独用自己的 left/top 调整。

---

## 6. 右上菜单

变量：

```css
--hud-top-menu-width: 250px;
--hud-top-menu-right: 32px;
--hud-top-menu-top: 58px;
--hud-top-menu-button-size: 14px;
--audio-panel-width: 283px;
--audio-panel-gap: 12px;
--audio-panel-padding: 16px;
--audio-panel-row-height: 54px;
```

控制“音声 / NEW”的位置、大小和字体。

当前右上布局是：

```text
ENEMY / 玩家名
音声 / NEW
```

也就是玩家信息在上，声音与重开 HUD 在下。

调法：

- 声音 / NEW 整体左右：改 `--hud-top-menu-right`。
- 声音 / NEW 整体上下：改 `--hud-top-menu-top`。
- 声音 / NEW 框宽度：改 `--hud-top-menu-width`。
- 声音 / NEW 按钮字大小：改 `--hud-top-menu-button-size`。
- 展开菜单宽度：改 `--audio-panel-width`。
- 展开菜单离主按钮距离：改 `--audio-panel-gap`。
- 展开菜单内边距：改 `--audio-panel-padding`。
- 展开菜单每一行高度：改 `--audio-panel-row-height`。

展开菜单现在使用黑铁 / 深皮革 / 旧金风格。不要再加现代透明玻璃、赛博斜切或网页表单风格。

---

## 7. 玩家名字

当前已经去掉玩家铭牌图片，只保留文字。

变量：

```css
--hud-player-right: 28px;
--hud-player-enemy-top: -38px;
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
--command-label-size: 15px;
--command-main-size: 16px;
--command-content-y: 0px;
--command-label-y: 0px;
--command-main-y: 0px;
--command-button-width: 300px;
--command-button-height: calc(var(--command-button-width) / 3);
--command-button-padding-x: 34px;
--command-hover-lift: 4px;
--command-pressed-drop: 3px;
--command-hover-brightness: 1.22;
--command-hover-glow: none;
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
- Hover 时按钮上浮幅度：改 `--command-hover-lift`。
- 点击时按钮下压幅度：改 `--command-pressed-drop`。
- Hover 时整体提亮强度：改 `--command-hover-brightness`。
- Hover 盒子外光：默认保持 `--command-hover-glow: none`。不要轻易给这个 PNG 按钮加 `box-shadow`，否则透明边缘会露出一圈脏框。

左右按钮应使用同一框体素材，其中一边镜像。不要再让左右使用不同底图导致高度不齐。

中央“防御 / 结束回合”是高频关键交互，hover 和 pressed 反馈应该明显。当前 hover 会上浮、提亮素材和文字；pressed 会下压并变暗。为了避免 PNG 素材透明边缘显出脏圈，默认不使用按钮盒子的外发光或矩形阴影。禁用状态不应用这些交互反馈。

### 共有盾光罩位置

共有盾光罩的位置由卡牌行实时计算，然后再用一个统一偏移量往战场中间推：

```css
--team-shield-center-offset: 5px;
```

调法：

- 数值为正：我方盾向上，敌方盾向下，也就是两边都往战场中间推。
- 数值为 `0px`：回到贴近卡片边缘的默认计算位置。
- 如果觉得盾离卡太远，减小到 `2px` 或 `3px`。
- 如果觉得还太贴边，增加到 `8px` 左右。

这个变量同时影响我方和敌方的共有盾展开位置，不改变盾的宽度、高度和破碎动画。

---

## 9. Action Point

当前 AP HUD 不再使用外框图片，只保留：

- `ACTION POINT` 文字。
- AP 光球。

变量：

```css
--ap-panel-left: 95px;
--ap-panel-top: calc(var(--hud-midline-y) - 63px);
--ap-panel-width: 362px;
--ap-panel-height: 129px;
--ap-title-size: 26px;
--ap-orb-size: 30px;
--ap-title-top: 0px;
--ap-title-left: 126px;
--ap-orb-top: 48px;
--ap-orb-left: 124px;
--ap-orb-gap: 7px;
```

调法：

- AP 整体左右：改 `--ap-panel-left`。
- AP 整体上下：优先跟随 `--hud-midline-y`，单独微调才改 `--ap-panel-top`。
- AP 整体可用区域宽高：改 `--ap-panel-width / --ap-panel-height`。
- ACTION POINT 字大小：改 `--ap-title-size`。
- AP 光球大小：改 `--ap-orb-size`。
- 文字位置：改 `--ap-title-top / --ap-title-left`。
- 光球位置：改 `--ap-orb-top / --ap-orb-left`。
- 光球间距：改 `--ap-orb-gap`。

当前 AP HUD 已经去掉以下旧显示层，因此这些变量不再保留为主调参入口：

- `--ap-kicker-size`
- `--ap-number-size`
- `--ap-fraction-size`
- `--ap-footer-size`
- `--ap-title-right`
- `--ap-orb-right`

如果以后重新启用 `ap-value` 数字层或 footer 文案，再重新设计并加入新的明确变量，不要把旧变量直接塞回主调参区。

AP 光球应与卡牌上方 cost 点使用同一视觉语言。

---

## 9.5 Battle Point

BP HUD 当前是一个独立战功资源显示：

- 左侧：`ui_bp_medal2.png` 战功徽章。
- 右侧：我方 `当前BP / 上限` 与本 turn 增量。

变量：

```css
--bp-hud-left: 1482px;
--bp-hud-top: calc(var(--hud-midline-y) - 20px);
--bp-medal-size: 92px;
--bp-row-width: 162px;
--bp-row-height: 40px;
--bp-row-gap: 18px;
--bp-row-offset-x: 82px;
--bp-number-size: 30px;
--bp-label-size: 10px;
```

调法：

- 整体位置：左右改 `--bp-hud-left`；上下优先跟随 `--hud-midline-y`，单独微调才改 `--bp-hud-top`。
- 左侧徽章大小：改 `--bp-medal-size`。
- 右侧两行数值框宽高：改 `--bp-row-width / --bp-row-height`。
- 上下两行间距：改 `--bp-row-gap`。
- 数值框相对徽章的水平距离：改 `--bp-row-offset-x`。
- BP 数字大小：改 `--bp-number-size`。
- `ENEMY / YOU` 标签大小：改 `--bp-label-size`。

BP 数字字体走卡牌左上角攻击力同系字体：`--card-attack-font-family`，没有定义时回退到 `--font-display`。

遗物总览 HUD：

```css
--relic-overview-left: var(--ap-panel-left);
--relic-overview-top: 895px;
--relic-overview-button-size: 76px;
--relic-overview-detail-width: 430px;
```

- 入口默认和 AP HUD 左侧对齐：`--relic-overview-left`。
- 入口上下位置：`--relic-overview-top`。
- 单个遗物总览图标大小：`--relic-overview-button-size`。
- 展开羊皮纸宽度：`--relic-overview-detail-width`。

---

## 10. Hover 详情框

变量：

```css
--hud-estimation-frame-brown: url("/assets/ui/hud/battle/ui_estimation_frame_brown.png");
--hud-estimation-frame-light: url("/assets/ui/hud/battle/ui_estimation_frame_light.png");
--hud-estimation-frame-dark: url("/assets/ui/hud/battle/ui_estimation_frame_dark.png");
--character-inspector-bg: var(--hud-estimation-frame-light);
--status-inspector-bg: var(--hud-estimation-frame-light);
--shield-inspector-bg: var(--hud-estimation-frame-light);
--character-inspector-width: 380px;
--status-inspector-width: 350px;
--shield-inspector-width: 430px;
--inspector-padding-x: 30px;
--inspector-padding-y: 28px;
--inspector-guide-inset: 18px;
--inspector-frame-zoom: 120%;
--inspector-radius: 26px;
--inspector-guide-radius: 15px;
--shield-inspector-padding-x: 33px;
--shield-inspector-padding-y: 31px;
--shield-inspector-padding-bottom: 28px;
--shield-inspector-guide-inset: 18px;
--shield-inspector-frame-zoom: 120%;
--shield-inspector-radius: 26px;
--shield-inspector-guide-radius: 15px;
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
- 外框圆角裁切：`--inspector-radius / --shield-inspector-radius`。
- 内侧参考线圆角：`--inspector-guide-radius / --shield-inspector-guide-radius`。

当前 hover 详情框使用浅色 frame，内部内容采用“羊皮纸上的手写战斗笔记”方向：深褐墨迹、淡旧金细线、少量暗红 debuff 标记、少量灰青盾牌标记。不要再给内部信息块套透明玻璃、四宫格收纳框、现代状态胶囊或大面积黑灰底。

### Hover 框内部排版与字体

角色 hover 左框、右框状态列表，以及防御按钮 hover 说明框现在共用一套“手写档案 + 墨迹账本 + 批注列表”的内部视觉。外框可以继续使用 PNG/SVG 材质，内部信息不采用透明玻璃、四宫格数据仓、扁平状态胶囊或现代 dashboard 风格。

布局原则：

- 角色基础数值是纵向账本行，不是四宫格。
- 技能是纸面批注，不是独立按钮/卡片。
- buff / debuff 是竖向批注列表，不是状态收纳盒。
- 只有细线和留白分层，不用大面积底色分层。

当前角色基础数值栏采用紧凑 2×2 手写账本：

```css
.character-inspector .inspector-stats {
  grid-template-columns:1fr 1fr;
  gap:4px 24px;
}
```

调法：

- 想让四个属性更紧：减小 `gap` 的第二个值，例如 `4px 16px`。
- 想让四个属性更松：增大 `gap` 的第二个值，例如 `4px 32px`。
- 属性标签宽度：调 `.character-inspector .inspector-stats span` 的 `min-width`，同时保持 `.character-inspector .inspector-stats>div` 的第一列宽度一致。
- 属性行水平对齐：属性行使用 `align-items:center`，不要改回 `baseline`，否则 COST / TYPE 很容易上下错位。
- 属性名和数值字体：都使用 `--font-hand`。如果觉得“数字大、标签小”，同时调整 `.character-inspector .inspector-stats span` 和 `.character-inspector .inspector-stats b` 的 `font-size`，不要只改数字。
- 属性数值大小：优先调 `--inspector-stat-value-size`；如果只想调 hover 左框，可直接调 `.character-inspector .inspector-stats b` 的 `font-size`。
- 技能区和属性区之间的距离：调 `.character-inspector .inspector-skill { margin-top: ... }`。
- COST 行不显示 `AP`，因为上下文已经明确是行动点，保留会破坏与 HP / TYPE 的对称。
- ATK 发生变化时，直接显示为同一字号的 `当前值 (+变化)` 或 `当前值 (-变化)`，例如 `2 (+1)`。不要用小号 `base` 脚注插入同一行，否则会破坏账本式对齐。
- HP 显示为同一字号的 `当前/最大`，例如 `12/12`。不要把 `/最大值` 做成小号尾巴。
- TYPE 的物理/魔法文本使用和主数值一致的深色墨迹，不再用红/紫色跳出。如果以后需要强调类型，优先加小图标，不要改成高饱和彩色文字。

主要调参入口：

```css
--inspector-kicker-size: 10px;
--inspector-title-size: 26px;
--inspector-body-size: 14px;
--inspector-stat-label-size: 10px;
--inspector-stat-value-size: 29px;
--inspector-skill-name-size: 20px;
--inspector-skill-body-size: 15px;
--inspector-effect-name-size: 16px;
--inspector-effect-body-size: 13px;
--inspector-skill-icon-size: 42px;
--inspector-status-icon-size: 28px;
--inspector-effect-icon-size: var(--inspector-status-icon-size);
--active-card-status-icon-size: var(--inspector-status-icon-size);
--opponent-card-status-icon-size: 28px;
--card-status-icon-size: var(--active-card-status-icon-size);
```

调法：

- 左右 hover 框标题太大/太小：改 `--inspector-title-size`。
- ATK / HP / COST / TYPE 数字太大/太小：改 `--inspector-stat-value-size`。
- 技能名太大/太小：改 `--inspector-skill-name-size`。
- 技能说明正文太大/太小：改 `--inspector-skill-body-size`。
- 右侧 buff / debuff 名称太大/太小：改 `--inspector-effect-name-size`。
- 右侧 buff / debuff 说明正文太大/太小：改 `--inspector-effect-body-size`。
- Hover 左框技能图标太大/太小：改 `--inspector-skill-icon-size`。
- Hover 右框 buff / debuff 状态图标太大/太小：改 `--inspector-status-icon-size`。
- 我方卡面右上角 buff / debuff 图标太大/太小：改 `--active-card-status-icon-size`。
- 敌方卡面右上角 buff / debuff 图标太大/太小：改 `--opponent-card-status-icon-size`。
- `--card-status-icon-size` 是内部默认值，通常不直接调。

### Hover 左右框图标调节

当前 hover 详情框里的图标分两类：

- 左框技能图标：`.inspector-skill-heading .ui-asset-icon`
- 右框 buff / debuff 状态图标：`.inspector-effects .ui-asset-icon`

两者现在可以分开调：

```css
--inspector-skill-icon-size: 42px;
--inspector-status-icon-size: 28px;
```

左侧技能区现在是两列结构：左边技能图标，右边依次是 `SKILL / 类型`、技能名、技能详情。这样技能图标变大时，不会在技能名和详情之间产生大段空白。

如果技能详情和技能名之间距离不合适，调：

```css
.character-inspector .inspector-skill-heading>div p {
  margin-top: 6px;
}
```

右侧状态图标变大时，状态条目的左侧留白会自动跟随：

```css
.inspector-effects li {
  padding-left: calc(var(--inspector-status-icon-size) + 12px);
}
```

`--inspector-effect-icon-size` 目前只作为旧名字兼容入口，默认指向 `--inspector-status-icon-size`。新调整优先使用 `--inspector-skill-icon-size` 和 `--inspector-status-icon-size`。

注意：当前手写纸面风格下，图标不应做成现代按钮或玻璃徽章。优先保持无边框、低阴影、像纸上盖章/小插图一样嵌在文本旁边。

颜色入口：

```css
--inspector-ink: #21130b;
--inspector-ink-soft: #5f4126;
--inspector-line: rgba(84, 52, 24, .38);
--inspector-gold-ink: #8c642f;
--inspector-red-ink: #6d1721;
--inspector-blue-ink: #315f67;
```

这些变量只控制 hover / shield 说明框内部，不影响卡面、攻击预测框或战斗 HUD。

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
--inspector-frame-zoom: 120%;
--shield-inspector-frame-zoom: 120%;
```

调法：

- 还看到脏边：加大到 `116%` 或 `120%`。
- 边框花纹被裁太多：降低到 `108%`。
- 这个参数只放大背景素材，不改变 HTML 框大小，也不改变文字位置。

### Hover 框四角残留素材相框边角时

如果四个角还能看到 PNG 原本自带的直角相框痕迹，调圆角裁切：

```css
--inspector-radius: 26px;
--shield-inspector-radius: 26px;
--inspector-guide-radius: 15px;
--shield-inspector-guide-radius: 15px;
```

调法：

- 角落残留还明显：把外框 radius 加到 `32px` 或 `38px`。
- 圆角吃掉太多内容/花纹：降到 `18px` 或 `22px`。
- 内侧淡线不贴合外框：同步调整 guide radius。

### Hover 框材质替换

三张可用材质：

```css
--hud-estimation-frame-brown
--hud-estimation-frame-light
--hud-estimation-frame-dark
```

当前默认：

```css
--character-inspector-bg: var(--hud-estimation-frame-light);
--status-inspector-bg: var(--hud-estimation-frame-light);
--shield-inspector-bg: var(--hud-estimation-frame-light);
```

如果想让左侧角色详情框临时换成深色，需要同时重新调整内部文字和内容块颜色，否则浅色纸面文本会不适配：

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
--preview-panel-bg: var(--hud-estimation-frame-light);
--preview-panel-width: 430px;
--preview-panel-padding-x: 34px;
--preview-panel-padding-y: 31px;
--preview-panel-guide-inset: 18px;
--preview-panel-frame-zoom: 120%;
--preview-panel-radius: 26px;
--preview-panel-guide-radius: 15px;
--preview-action-button-width: 112px;
--preview-action-button-height: 82px;
--preview-action-button-gap: 120px;
--preview-action-button-y: 0px;
--preview-forecast-icon-size: 30px;
```

攻击预测栏现在和 hover / 防御说明框统一，使用浅色 estimation frame，并采用“羊皮纸手写战斗批注”内部排版。

当前默认：

```css
--preview-panel-bg: var(--hud-estimation-frame-light);
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
- 外框圆角裁切：`--preview-panel-radius`。
- 内侧参考线圆角：`--preview-panel-guide-radius`。
- 伤害预测图标大小：`--preview-forecast-icon-size`。
- 底部 decide / cancel 按钮宽度：`--preview-action-button-width`。
- 底部 decide / cancel 按钮高度：`--preview-action-button-height`。
- 两个按钮之间距离：`--preview-action-button-gap`。
- 两个按钮整体上下位置：`--preview-action-button-y`。
- 内部伤害格样式：`.forecast-box`。
- 技能预测格样式：`.preview-skill`。
- 底部按钮图片：`.decide-action` 使用 `assets/ui/hud/battle/decide.png`，`.cancel-action` 使用 `assets/ui/hud/battle/cancel.png`。

底部 decide / cancel 的常驻动画只允许改变 `filter`，不要改变 `transform`。当前是低频光晕呼吸，不再做闪烁或上下浮动。这样常驻光效不会和 hover 的上浮、高亮、点击下压互相抢控制权。  
如果想改常驻光效速度，调整 `.preview-panel.open .preview-actions .preview-action` 里的 `previewCommandGlow` 动画时长；如果想改 hover 反馈，调整 `.preview-actions .preview-action:hover` 的 `filter / transform`。

当前内部排版原则：

- 双方名字是纸面上的对战记录，不用黑色底板。
- 伤害预测是纵向账本行，不再使用左右并排的深色盒子。
- 技能预测是纸面批注，结构是 `技能名 + 说明`。
- notes 是细字脚注，没内容时隐藏。
- 执行攻击 / 取消按钮只显示 PNG 素材，不显示文字。文字只保留在 `aria-label` 里，方便无障碍和本地化。

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
--preview-panel-frame-zoom: 120%;
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
--font-hand
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

`--font-hand` 是 hover 详情框、手写账本、状态批注等位置使用的“手写/书写体”字体栈。当前没有随项目打包字体文件，所以它会优先使用玩家系统已有字体：

```css
--font-hand: "Klee One", "Yuji Syuku", "Hannotate SC", "Hannotate TC", "STKaiti", "KaiTi", "Yu Mincho", "Hiragino Mincho ProN", serif;
```

如果以后准备稳定字体效果，建议放一个 `.woff2` 到 `wwwroot/assets/fonts/`，再用 `@font-face` 指向它。这样 Windows / Mac / 朋友电脑看到的 hover 字体才会完全一致。

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

当前表现已经不是硬箭头，而是一条黄色抛物线光弧：从我方卡牌中心出发，拖动时跟随鼠标，悬停到敌方卡牌时吸附到敌方卡牌中心。

锚点和曲线公式在 [wwwroot/app.js](../wwwroot/app.js)；颜色、粗细、前端光球和粒子在 [wwwroot/styles.css](../wwwroot/styles.css)。

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

### 抛物线弧度

曲线在 `updateAttackArrow()` 内：

```js
const arcLift = Math.min(210, Math.max(72, distance * 0.2));
const controlX = dragArrowOrigin.x + dx * 0.5;
const controlY = dragArrowOrigin.y + dy * 0.5 - arcLift;
const path = `M ... Q ${controlX} ${controlY}, ${endX} ${endY}`;
```

- `distance * 0.2`：距离越远，抛物线越高。
- `72`：最小弧高。
- `210`：最大弧高。
- 想更像高抛弧线：把 `0.2` 或最大值 `210` 调大。
- 想更贴近直线：把 `0.2` 或最小值 `72` 调小。

### 光弧粗细、颜色与前端光球

主要变量在 `:root`：

```css
--attack-arc-tail: #80683d;
--attack-arc-gold: #f0c76b;
--attack-arc-hot: #fff3b6;
--attack-arc-core-width: 3px;
--attack-arc-glow-width: 13px;
--attack-arc-head-scale: 1;
```

常用调法：

- 核心光线粗细：`--attack-arc-core-width`。
- 外围光晕粗细：`--attack-arc-glow-width`。
- 光线尾部颜色：`--attack-arc-tail`。
- 主体金色：`--attack-arc-gold`。
- 鼠标前端最亮颜色：`--attack-arc-hot`。
- 前端光球整体大小：`--attack-arc-head-scale`。

对应 CSS 结构：

```css
.attack-drag-arrow .attack-arc-glow
.attack-drag-arrow .attack-arc-core
.attack-drag-arrow .attack-arc-origin
.attack-drag-arrow .attack-arc-head
.attack-drag-arrow .attack-head-particle
.attack-drag-arrow.locked ...
```

注意：这条线现在不使用 `marker` 箭头头，也不使用 `stroke-dasharray` 虚线。不要再按旧箭头的方式调。

### 攻击确认后的红色连接光弧

玩家确认攻击后，会短暂显示一条从攻击者中心到目标中心的红色战斗光弧。它和拖拽阶段的黄色光弧使用同一套视觉语言：抛物线、圆润发光、前端光球和少量粒子；只是颜色变成红色，用来表示攻击已经成立。

生成位置：

```js
showCombatLink(attacker, defender)
```

红色光弧的曲线公式：

```js
const arcLift = Math.min(230, Math.max(80, distance * 0.22));
```

调法和黄色拖拽光弧一致：

- 更高的攻击弧线：调大 `0.22` 或 `230`。
- 更贴近直线：调小 `0.22` 或 `80`。

主要变量：

```css
--combat-arc-tail: #6f1019;
--combat-arc-red: #e21b32;
--combat-arc-hot: #ffd0bd;
--combat-arc-core-width: 4px;
--combat-arc-glow-width: 16px;
--combat-arc-head-scale: 1.05;
```

- 核心红线粗细：`--combat-arc-core-width`。
- 外围红色光晕粗细：`--combat-arc-glow-width`。
- 红线尾部颜色：`--combat-arc-tail`。
- 主体红色：`--combat-arc-red`。
- 前端最亮颜色：`--combat-arc-hot`。
- 目标端光球大小：`--combat-arc-head-scale`。

注意：攻击确认红线也不再使用虚线、链条、旋转圆圈或 marker 箭头头。

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
