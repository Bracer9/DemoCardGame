# Tiny Pixel Fights — Dummy Reward Window 实装文档

> 日期：2026-06-28  
> 阶段：PvP Growth Prototype Phase 2 — Dummy Reward Window  
> 目标：先验证“固定 Round 奖励窗口 + BP 消费 + reset / skip”的流程体验。当前带有临时战斗奖励效果，用于 playtest 感受强弱节奏；之后可被正式遗物/成长系统替换。

---

## 1. 这一阶段要验证什么

Phase 2 不追求真实成长内容，只验证三件事：

1. 奖励窗口能否按固定 Round 节奏自然插入战斗。
2. 玩家是否能理解 BP 是“现在花掉换奖励，还是跳过攒起来”的资源。
3. reset / skip / 购买的交互节奏是否舒服，不拖垮战斗。

这一步完成后，后续英雄升级、普通兵、遗物、天气奖励都应接入同一个 Reward Window 框架，而不是各自做一套弹窗。

---

## 2. 第一版规则

### 2.1 触发节奏

默认规则：

- Round 1 出现第一次奖励窗口。
- 之后每隔 3 Round 出现一次。
- 触发时机：符合条件的玩家回合开始时。
- 每名玩家在同一个奖励 Round 内最多处理一次奖励窗口。

推荐配置：

```csharp
public const int FirstRewardRound = 1;
public const int RewardRoundInterval = 3;
```

判断函数建议：

```csharp
IsRewardRound(round) =>
    round >= FirstRewardRound
    && (round - FirstRewardRound) % RewardRoundInterval == 0;
```

### 2.2 Dummy 奖励

第一版固定三张 dummy reward：

| ID | 显示定位 | BP 费用 | 实际效果 |
|---|---|---:|---|
| dummy-reward-a | 小奖励 | 2 | 我方当前在场角色魔法防御 +1 |
| dummy-reward-b | 中奖励 | 4 | 我方当前在场的魔法攻击角色攻击 +1 |
| dummy-reward-c | 大奖励 | 6 | 我方当前在场角色攻击 +1 |

这些效果目前是临时遗物模拟：通过不可驱散的局内 buff/status 表现，方便后续替换成正式遗物、英雄成长或战场奖励系统。它们只应用于当前在场角色，不先处理未来新增入队单位的继承问题。

买不起：

- 奖励仍然显示。
- 按钮 disabled 或显示“BP不足”状态。
- 可查看说明，但不能购买。

### 2.3 Reset

每次奖励窗口：

- 第一次 reset 免费。
- 第二次开始每次消耗 1 BP。
- 如果 BP 不足，则 reset 按钮 disabled。

第一版可以允许多次 reset，只要从第二次开始扣 1 BP；如果 playtest 发现拖节奏，再改为“最多 reset 2 次”。

推荐字段：

```csharp
int ResetCount;
int NextResetCost => ResetCount == 0 ? 0 : 1;
```

### 2.4 Skip

Skip 不购买奖励，并关闭本次奖励窗口。

当前按用户口径暂定：

- skip 额外获得 +1 BP。
- reasonId：`reward-skip`

备注：用户写的是“dp”，但当前系统内没有 DP 资源。Phase 2 暂按 BP 处理；如果之后 DP 被定义为独立资源，再从这里拆出去。

---

## 3. 后端状态设计

不要把 Reward Window 做成前端临时 UI。它必须是后端状态，因为在线模式两端都要看到一致流程。

建议新增：

```csharp
public sealed class RewardWindowState
{
    public bool IsOpen { get; set; }
    public Guid PlayerId { get; set; }
    public int RoundNumber { get; set; }
    public int ResetCount { get; set; }
    public List<RewardOptionState> Options { get; } = [];
}

public sealed record RewardOptionState(
    string InstanceId,
    string RewardId,
    int Cost);
```

挂在 `GameState`：

```csharp
public RewardWindowState? RewardWindow { get; set; }
public HashSet<string> ResolvedRewardWindows { get; } = [];
```

`ResolvedRewardWindows` key 建议：

```text
{playerId}:{roundNumber}
```

这样可以防止同一玩家同一 Round 重复弹窗。

---

## 4. 核心流程

### 4.1 创建窗口

在以下位置尝试打开奖励窗口：

1. 新游戏创建后，如果 Round 1 需要奖励，则为先手玩家打开。
2. `EndTurn()` 切换行动玩家、处理回合开始 BP 后，检查当前 round 是否需要奖励。

推荐顺序：

```text
切换 ActivePlayer
TurnNumber++
重置 AP / 行动状态
回合开始 BP 低保
ProcessTurnStart
TryOpenRewardWindowForActivePlayer
```

注意：如果奖励未来会影响回合开始效果，这个顺序可能要调整。Phase 2 的 dummy reward 不影响战斗，所以先放在回合开始效果之后，减少对现有机制干扰。

### 4.2 窗口打开时锁定行动

当 `RewardWindow.IsOpen == true` 且属于当前 viewer：

- 不能攻击。
- 不能开盾。
- 不能结束回合。
- 只能选择奖励、reset 或 skip。

当 viewer 不是当前奖励窗口拥有者：

- 显示等待提示，例如“相手が報酬を選択中”。
- 不显示可点三张奖励。
- 在线模式中避免双方同时操作同一窗口。

### 4.3 购买奖励

API：

```text
POST /api/game/reward/select
```

Request：

```csharp
public sealed record SelectRewardRequest(string InstanceId);
```

流程：

```text
确认窗口存在
确认窗口属于当前行动玩家
确认 InstanceId 存在
确认 BP 足够
SpendBp(cost, "reward-purchase")
写日志
标记该 player/round resolved
关闭窗口
```

Dummy reward 暂时不改变任何角色属性。

### 4.4 Reset

API：

```text
POST /api/game/reward/reset
```

流程：

```text
确认窗口存在
确认窗口属于当前行动玩家
cost = ResetCount == 0 ? 0 : 1
如果 cost > 0，SpendBp(cost, "reward-reroll")
ResetCount++
重新生成 3 个 dummy options
写日志
保持窗口打开
```

### 4.5 Skip

API：

```text
POST /api/game/reward/skip
```

流程：

```text
确认窗口存在
确认窗口属于当前行动玩家
TryGainBp(+1, "reward-skip")
写日志
标记该 player/round resolved
关闭窗口
```

Skip 给 BP 也应走现有 `TryGainBp`，受 BP 总上限影响；是否受“单回合 BP 获取上限”建议第一版仍然受限制，避免 skip 成为额外刷 BP 通道。

---

## 5. 奖励生成器

第一版不要做复杂权重。

建议新增轻量 service：

```csharp
public sealed class RewardService
{
    public IReadOnlyList<RewardOptionState> GenerateDummyOptions(GameState state, PlayerState player);
}
```

第一版可以固定返回 A/B/C，也可以从 dummy 池里随机三张。为了验证 reset，建议至少做“随机顺序 + stable InstanceId”。

Dummy reward 定义建议：

```csharp
public sealed record RewardDefinition(
    string Id,
    int Cost,
    string Rarity);
```

显示文本放入 `wwwroot/locales/*.json`：

```json
"rewards": {
  "dummy-reward-a": {
    "name": "...",
    "description": "..."
  }
}
```

---

## 6. DTO 与在线同步

`GameView` 需要新增：

```csharp
RewardWindowView? RewardWindow
```

DTO：

```csharp
public sealed record RewardWindowView(
    bool IsOpen,
    Guid PlayerId,
    int RoundNumber,
    int ResetCount,
    int NextResetCost,
    bool CanChoose,
    IReadOnlyList<RewardOptionView> Options);

public sealed record RewardOptionView(
    string InstanceId,
    string RewardId,
    int Cost,
    bool CanAfford);
```

`CanChoose` 由后端根据 viewer / active player 判断，避免前端猜。

---

## 7. 前端 UI

### 7.1 视觉方向

奖励窗口参考 STS 的“选择事件/奖励”节奏：

- 背景压暗。
- 中央出现三张羊皮卷轴式奖励框。
- 整体风格接近当前 hover inspector / combat forecast：羊皮纸、旧金、黑铁、手写感。
- 不使用现代卡片商店 UI，不使用玻璃拟态。

素材：

- 奖励框背景优先使用现有 HUD PNG，例如：
  - `assets/ui/hud/battle/ui_estimation_frame_light.png`
  - `assets/ui/hud/battle/ui_estimation_frame_brown.png`
  - `assets/ui/hud/battle/ui_estimation_frame_dark.png`
- 如果后续有专用卷轴素材，再替换 CSS 变量，不改 DOM。

建议 CSS 变量：

```css
--reward-overlay-bg: rgba(0, 0, 0, .58);
--reward-card-bg: var(--hud-estimation-frame-light);
--reward-card-width: 340px;
--reward-card-height: 470px;
--reward-card-gap: 34px;
--reward-card-radius: 28px;
```

### 7.2 DOM 结构

```html
<aside id="reward-window" class="reward-window" aria-hidden="true">
  <div class="reward-backdrop"></div>
  <section class="reward-panel">
    <header class="reward-header">
      <small>TACTICAL REWARD</small>
      <h2>戦利品を選択</h2>
      <p>BPを消費して報酬を得るか、スキップして戦功を温存します。</p>
    </header>

    <div id="reward-options" class="reward-options"></div>

    <footer class="reward-actions">
      <button id="reward-reset"></button>
      <button id="reward-skip"></button>
    </footer>
  </section>
</aside>
```

三张奖励卡由 JS 渲染：

```html
<button class="reward-card">
  <small>COMMON</small>
  <strong>Dummy Reward A</strong>
  <p>Dummy text...</p>
  <b>2 BP</b>
</button>
```

### 7.3 动画

打开：

- 背景 180ms 淡入。
- 三张卷轴卡从下方轻微上浮进入。
- 三张卡可以错开 40ms，增加仪式感。

关闭：

- 选择 / skip 后快速淡出，不要拖太久。

hover：

- 奖励卡轻微上浮。
- 旧金边缘变亮。
- BP 不足时不 hover 上浮，只显示压暗与 cost 红色提示。

---

## 8. 本地化

新增 section：

```json
"rewards": {
  "dummy-reward-a": { "name": "...", "description": "..." },
  "dummy-reward-b": { "name": "...", "description": "..." },
  "dummy-reward-c": { "name": "...", "description": "..." }
}
```

新增 UI：

```text
rewardTitle
rewardSubtitle
rewardReset
rewardResetFree
rewardResetCost
rewardSkip
rewardCannotAfford
opponentChoosingReward
```

新增日志：

```text
log.rewardOpened
log.rewardPurchased
log.rewardReset
log.rewardSkipped
log.bpSpent
```

新增 BP reason：

```text
reward-purchase
reward-reroll
reward-skip
```

---

## 9. 完成标准

Phase 2 完成时应满足：

- Round 1 当前行动玩家回合开始时出现奖励窗口。
- 之后每 2 Round 再出现。
- 每名玩家每个奖励 Round 只处理一次。
- 三张 dummy reward 显示费用。
- BP 足够时可以购买，购买后扣 BP 并关闭窗口。
- BP 不足时不能购买。
- 第一次 reset 免费。
- 第二次开始 reset 消耗 1 BP。
- skip 关闭窗口，并尝试获得 +1 BP。
- reward window 打开时，攻击、开盾、结束回合都不可操作。
- 在线模式中窗口只允许当前玩家选择，对手看到等待状态。
- 文本全部走 locales，C# 不写中日文显示文本。
- `dotnet build`、`node --check wwwroot/app.js`、JSON parse 通过。

---

## 10. 不在本阶段做

- 真实属性奖励。
- 英雄升级。
- 普通兵加入或强化。
- 副官。
- 遗物。
- 天气 / Round Buff。
- 奖励稀有度权重和保底。
- 奖励获得后的复杂 VFX。

---

## 11. 推荐实装顺序

1. 新增 RewardWindowState / RewardOptionState。
2. 新增 RewardService，生成 dummy A/B/C。
3. 在 GameState 挂 RewardWindow 与 resolved key。
4. 新游戏和 EndTurn 后尝试打开窗口。
5. DTO 暴露 RewardWindowView。
6. 新增 select / reset / skip API。
7. 前端加 reward modal DOM / render / 操作绑定。
8. reward window 打开时锁住战斗操作。
9. 本地化与日志。
10. 验证本地和在线模式。
