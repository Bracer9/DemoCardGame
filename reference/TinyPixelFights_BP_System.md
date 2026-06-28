# Tiny Pixel Fights — BP 系统实装设计文档

> 日期：2026-06-28  
> 阶段：PvP Growth Prototype Phase 1 — BP Core  
> 目标：先建立一个稳定、可显示、可记录、可扩展的局内成长货币底座。暂不实装奖励窗口、英雄升级、普通兵、副官、遗物或天气。

---

## 1. 这次要做的是什么

BP 是 **Battle Point / 战功点**。它不是经验条，不是评价分，也不是 AP 的替代品。

当前定位：

- BP 是每名玩家独立拥有的局内资源。
- BP 用于未来购买三选一奖励、英雄升级、普通兵获取/强化、遗物或天气相关选择。
- Phase 1 只验证 BP 的获取、上限、每回合获取上限、UI 显示、日志解释和在线同步。
- 奖励消费先不做真实内容，但要把未来 `SpendBp` / reward window 接口想清楚。

设计支柱对应：

- **有意义的战术选择**：玩家会思考如何通过攻击、破盾、技能触发积累成长资源。
- **清楚的因果反馈**：BP 获得必须通过 UI 和日志说明来源。
- **活着并会成长的角色卡牌**：BP 是后续英雄成长、普通兵成长和副官系统的燃料。
- **变化的战场**：未来 BP 可以连接奖励窗口、遗物、天气和战场任务。

---

## 2. 第一版规则口径

采用 `reference/TinyPixelFights_PvP_GrowthPrototype_Plan_20260628.md` 的 Phase 1 规则：

| 项目 | 第一版数值 |
|---|---:|
| 初始 BP | 5 |
| BP 上限 | 10 |
| 单回合 BP 获取上限 | 3 |
| 己方回合开始低保 | +1 |
| 破坏敌方共有盾 | +1 |
| 主动攻击造成 HP 伤害 | +1 |
| 主动技能实际发动 | +2 |
| 被动技能实际触发 | +1 |

补充规则：

- 所有来源都必须走统一方法，例如 `TryGainBp(...)`。
- 多个来源可以在同一行动中依次尝试获得 BP，但最终受单回合上限和总上限限制。
- BP 消耗不会影响已经获得的成长，只影响当前是否买得起未来奖励。
- 如果因为本回合上限或总上限导致只获得部分 BP，日志需要说明。
- 技能 BP 只能在技能真实进入战斗结果或日志时获得，不能因为“技能按钮可用”就获得。

---

## 3. 不在本阶段做的事

Phase 1 不做：

- 三选一奖励窗口。
- Dummy reward 购买。
- 英雄升级。
- 普通兵。
- 副官。
- 遗物。
- 天气。
- BP 飞行动画或复杂特效。

本阶段只要做到：打一局时 BP 稳定增长，双方都看得到，日志说得清楚，在线不会串号。

---

## 4. 推荐架构

### 4.1 为什么需要新 class

不建议把 BP 写成 `PlayerState.CurrentBp`、`PlayerState.MaxBp`、`PlayerState.BpGainedThisTurn` 三个散字段后到处读写。原因：

- 后续 BP 会被奖励、遗物、天气、战场任务、落后补偿等多来源影响。
- 需要统一处理“本回合上限”和“总上限”。
- UI 需要显示最近一次来源和本回合已获得量。
- 日志需要知道完整获得量、实际获得量、被 cap 掉的量。
- 将来可能会有“获得 BP 时触发遗物充能”这类监听。

建议新增轻量 class：

```csharp
public sealed class BattlePointState
{
    public int Current { get; set; }
    public int Max { get; set; }
    public int GainedThisTurn { get; set; }
    public string? LastReasonId { get; set; }
}
```

挂在 `PlayerState` 上：

```csharp
public BattlePointState BattlePoints { get; } = new();
```

### 4.2 配置集中管理

建议先放在 `GameEngine` 常量或单独配置 class：

```csharp
public const int InitialBattlePoints = 5;
public const int MaxBattlePoints = 10;
public const int BattlePointGainCapPerTurn = 3;
```

如果很快进入 reward / relic / weather，再迁移成：

```csharp
public sealed record BattlePointConfig(
    int Initial,
    int Max,
    int GainCapPerTurn);
```

第一版不必过度抽象，但不要把 `5 / 10 / 3` 写散。

### 4.3 交易结果对象

`TryGainBp` 不应该只返回 bool。它应该返回实际变化，方便日志、UI 和未来触发器使用：

```csharp
public sealed record BattlePointGainResult(
    Guid PlayerId,
    string ReasonId,
    int Requested,
    int Gained,
    int BlockedByTurnCap,
    int BlockedByMax,
    int Current,
    int Max,
    int GainedThisTurn);
```

第一版可以少写字段，但至少要知道：

- 想获得多少。
- 实际获得多少。
- 当前 BP。
- 是否被上限截断。

---

## 5. 核心方法设计

### 5.1 获得 BP

建议放在 `GameEngine`：

```csharp
internal BattlePointGainResult TryGainBp(
    GameState state,
    PlayerState player,
    int amount,
    string reasonId);
```

职责：

1. amount <= 0 时直接返回 0。
2. 计算本回合剩余可获得量：`cap - GainedThisTurn`。
3. 计算总 BP 剩余容量：`Max - Current`。
4. 实际获得量取三者最小值。
5. 更新 `Current`、`GainedThisTurn`、`LastReasonId`。
6. 写日志。
7. 返回结果。

注意：不要在技能或攻击代码里直接 `player.BattlePoints.Current += 1`。

### 5.2 消耗 BP

Phase 1 可以先写接口但暂时不调用：

```csharp
internal bool TrySpendBp(
    GameState state,
    PlayerState player,
    int amount,
    string reasonId);
```

Phase 2 reward window 会使用它。第一版可以只设计，不实装也可以；但如果实装，必须只做扣 BP 和日志，不要接奖励内容。

---

## 6. 获取时机接入点

### 6.1 回合开始低保 +1

当前 `EndTurn()` 流程是：

```text
当前玩家 OnTurnEnd
切换 ActivePlayer
TurnNumber++
AP 重置
清新 ActivePlayer 盾/行动状态
log.turnStart
ProcessTurnStart
```

BP 低保建议插在：

```text
切换 ActivePlayer 后
重置该玩家 GainedThisTurn
log.turnStart 之后或之前均可，但建议紧跟 turnStart 之后
ProcessTurnStart 之前
```

推荐顺序：

```text
log.turnStart
ResetBpTurn(activePlayer)
TryGainBp(activePlayer, 1, "turn-start")
ProcessTurnStart
```

理由：被动技能如果在 `ProcessTurnStart` 内触发并给 BP，应计入同一回合上限，且低保先占 1 点。

### 6.2 主动攻击造成 HP 伤害 +1

在 `Attack()` 中，`ModifyDamage()`、盾吸收、守护、扣 HP 之后才能判断。

条件：

- 来源是主动攻击。
- `attackPacket.Amount > 0`。
- 伤害实际扣到了防守方目标 HP。
- 反击造成伤害不触发这个来源。

建议 reasonId：

```text
active-attack-hp-damage
```

### 6.3 破坏敌方共有盾 +1

需要判断本次行动是否让敌方 `SharedShield` 从正数变成 0。

建议不要在 UI 或日志里猜，应在规则层记录：

- 攻击结算前目标 owner 的 `SharedShield`。
- 攻击结算后目标 owner 的 `SharedShield`。
- 如果 `before > 0 && after == 0`，攻击方获得 +1 BP。

reasonId：

```text
break-enemy-shield
```

注意：

- 如果是燃烧、余波等技能伤害打破盾，是否算“破盾”需要明确。
- Phase 1 建议先只要“由当前行动者造成的伤害打破敌方盾”都算，避免玩家觉得看不懂。
- 如果之后发现刷 BP，可细分为主动攻击破盾 / 技能破盾。

### 6.4 主动技能实际发动 +2

当前技能没有统一的“技能发动事件”。这是 BP 系统最大风险点。

Phase 1 不建议在每个技能里手写 `TryGainBp`，否则很快散架。推荐新增通用通道：

```csharp
context.NotifySkillTriggered(owner, skillId, SkillTriggerKind.Active);
```

或先用更轻量的：

```csharp
context.GainBpForSkillTrigger(owner, skillId, "active-skill-triggered");
```

但长期更推荐“事件通知”，因为未来：

- 技能发动动画
- 技能语音
- BP 奖励
- 成就/任务
- 遗物充能

都可能监听同一个事件。

第一版可先只覆盖已有主动技能：

- 魔法使い：灼熱の刻印成功判定进入技能效果时。
- ドルイド：衰弱の胞子实际发动时，包括 50% 成功或 100% 成功。
- バーバリアン：戦斧の余波实际造成余波或满足触发条件时。
- モンスター：美女と野獣实际触发绝对追击时。

如果主动技能“尝试但失败”，例如 50% 未触发，原则上不给 BP，除非日志明确写“技能发动但失败”。当前建议：**只有实际生效才给**。

### 6.5 被动技能实际触发 +1

被动技能只在“产生了可见战斗结果或日志”时给。

候选：

- 姫：圣女の祈り实际治疗至少 1 HP 时。
- 占い師：预见实际减伤时；星読みの魔力实际加魔法伤害时。
- 農民：春蒔き/豊穣实际播种或获得攻击 buff 时。
- 騎士：守护实际肩代わり时。
- モンスター：野獣の怒り获得时，如果它被视为怪物技能被动结果，可给 +1；但要注意不与美女と野獣主动追击重复。

风险：

- 占い師的预见可能频繁触发。
- 姫每回合治疗多个角色可能刷 BP。
- 同一被动在一次连锁里可能触发多次。

Phase 1 建议加入“每个技能每次事件最多给一次”的去重：

```text
同一个 log/event chain 中，同一 owner + skillId + reasonId 最多给一次 BP。
```

如果实现成本高，第一版可以先保守：只给关键被动 BP，例如守护、播种、治疗至少一次，而不把每个减伤都接入。

---

## 7. 日志、本地化与事件

新增本地化 key 建议：

```text
log.bpGained
log.bpGainCapped
log.bpMaxed
log.bpSpent
```

参数：

```text
player
amount
requested
current
max
reason
blocked
```

reason 也需要本地化，不要在 C# 写中文/日文。

建议 reason IDs：

```text
bp.turn-start
bp.break-enemy-shield
bp.active-attack-hp-damage
bp.active-skill-triggered
bp.passive-skill-triggered
bp.reward-purchase
bp.reward-skip
bp.reward-reroll
```

显示文本放在 `wwwroot/locales/*.json`。

---

## 8. DTO 与在线同步

`PlayerView` 需要加入 BP 信息。

推荐新增 DTO：

```csharp
public sealed record BattlePointView(
    int Current,
    int Max,
    int GainedThisTurn,
    int GainCapPerTurn,
    string? LastReasonId);
```

然后：

```csharp
public sealed record PlayerView(
    Guid Id,
    string Name,
    bool IsActive,
    int SharedShield,
    int ActiveCharacterCount,
    BattlePointView BattlePoints,
    IReadOnlyList<CharacterView> Characters);
```

理由：

- 前端 hover 可以显示 `本回合 2 / 3`。
- 在线双方通过同一个 `GameView` 同步，不需要额外 API。
- 后续 reward window 能直接判断当前玩家 BP 是否足够。

---

## 9. UI 第一版

BP 必须常驻显示。

建议布局：

- 我方 BP：右下 `YOU / 玩家名` 附近，但避开手牌。
- 敌方 BP：右上 `ENEMY / 玩家名` 附近。
- 样式：旧金币 / 战功徽章 / 令牌感，不做现代进度条。
- 显示格式：`BP 5` 或 `戦功 5`，先用短文字。
- hover：显示当前 BP、上限、本回合已获得、单回合上限、最近来源。

动画第一版：

- BP 数字变化时轻微闪一下。
- 不做飞行粒子。
- 不做大型奖励演出。

后续 Phase 2 reward window 再考虑更明显的获得/消费动画。

---

## 10. 前端结构建议

新增渲染函数：

```js
function renderBattlePoints() {}
function bpReasonText(reasonId) {}
```

不要把 BP 显示写进 player name 字符串里。它应该是独立 HUD。

建议 DOM：

```html
<div id="opponent-bp" class="bp-hud bp-hud--opponent"></div>
<div id="active-bp" class="bp-hud bp-hud--active"></div>
```

或跟随现有玩家铭牌附近插入，但不要挡住卡牌、command HUD 或战况日志。

---

## 11. 与未来系统的接口

### 11.1 Reward Window

Phase 2 需要：

- 读取当前玩家 BP。
- 购买时调用 `TrySpendBp`。
- 买不起时按钮 disabled，但仍可看奖励说明。
- skip 不扣 BP。
- reroll 是否扣 BP 暂不定，第一版可免费 1 次。

### 11.2 英雄升级

英雄升级不应该直接改 BP，而是作为 reward effect：

```text
RewardEffect: unlock-skill / increase-stat / add-hero-upgrade-token
Cost: BP
```

BP 只负责支付，不负责知道英雄升级具体内容。

### 11.3 普通兵 / 副官

普通兵加入、升阶、副官附着，都应作为 reward effect 或后续 unit system 的行为。BP 只作为支付资源。

### 11.4 遗物 / 天气

未来遗物可能监听：

- 获得 BP
- 消耗 BP
- 本回合 BP 达到上限
- 因上限溢出 BP

因此 `TryGainBp` / `TrySpendBp` 返回交易结果很重要。

---

## 12. 测试清单

### 12.1 后端规则

- 新游戏双方 BP 都是 5。
- BP 不超过 10。
- 己方回合开始 +1。
- 每回合 BP 获取最多 3。
- `GainedThisTurn` 在该玩家新回合开始时重置。
- A 回合获得的是 A 的 BP，B 不变。
- B 回合获得的是 B 的 BP，A 不变。
- 主动攻击造成 HP 伤害 +1。
- 只打到盾但没有 HP 伤害，不触发 HP 伤害 BP。
- 打破敌方共有盾 +1。
- 同一次行动多来源触发时，总进账不超过本回合剩余额度。
- BP 满时不会继续增加，并写出可理解日志。

### 12.2 技能触发

- 主动技能未实际生效时不给 BP。
- 主动技能实际生效时 +2。
- 被动技能实际触发时 +1。
- 同一技能在同一连锁中不重复刷 BP。

### 12.3 UI / 在线

- 本地模式双方 BP 显示正确。
- 在线模式房主与加入者看到同一 BP 状态。
- 回合交换后 UI 不串号。
- hover 能看到本回合已获得量和上限。
- 日志显示 BP 来源。

---

## 13. 第一版完成标准

完成 BP Core 时，应达到：

- 不接奖励系统也能完整打一局。
- BP 正常增长、被 cap、被总上限限制。
- UI 常驻显示双方 BP。
- 日志解释 BP 来源和 cap。
- 在线同步稳定。
- `dotnet build` 通过。
- `node --check wwwroot/app.js` 通过。

---

## 14. 实装顺序建议

1. 新增 `BattlePointState`，挂到 `PlayerState`。
2. 新游戏初始化双方 BP。
3. 新增 `TryGainBp` / `ResetBpTurn`。
4. 回合开始低保 +1。
5. DTO 暴露 BP。
6. 前端显示双方 BP HUD。
7. 日志与本地化。
8. 接主动攻击 HP 伤害 +1。
9. 接破盾 +1。
10. 接主动技能 / 被动技能实际触发。
11. 手动打一局，观察是否变成刷 BP 游戏。

不要一开始就接所有技能。可以先让回合低保、主动 HP 伤害、破盾跑通，再逐个接技能来源。

