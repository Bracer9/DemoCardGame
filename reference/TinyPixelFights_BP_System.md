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

- **有意义的战术选择**：玩家会思考如何通过进攻、破盾和防御投入积累成长资源。
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
| 己方回合内对敌方造成 HP 伤害 | +1 |
| 防御阵型完全展开 | +1 |

补充规则：

- 所有来源都必须走统一方法，例如 `TryGainBp(...)`。
- 多个来源可以在同一行动中依次尝试获得 BP，但最终受单回合上限和总上限限制。
- BP 消耗不会影响已经获得的成长，只影响当前是否买得起未来奖励。
- 如果因为本回合上限或总上限导致只获得部分 BP，日志需要说明。
- 主动/被动技能触发、天气、战场任务、成长奖励等暂时不纳入 Phase 1，但后续接入时仍必须走同一条 `TryGainBp(...)` 管线，不能在单个技能里散写 BP 加算。

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
- 主动/被动技能触发给 BP。
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

### 6.2 己方回合内对敌方造成 HP 伤害 +1

在 `Attack()`、技能伤害、绝对伤害等实际扣 HP 之后才能判断。

条件：

- 伤害来源属于当前行动方。
- 目标属于敌方。
- 实际扣到了目标 HP，最终伤害量 `> 0`。
- 反击造成伤害不触发这个来源。
- 只打到共有盾但没有扣 HP，不触发这个来源。

建议 reasonId：

```text
own-turn-enemy-hp-damage
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

### 6.4 防御阵型完全展开 +1

当前共有盾按“是否达到当前最大盾值”判断完全展开：

```text
第一次：展开共有盾
后续：若当前盾值为1～3，强化共有盾；本次防御指令强化后达到5即完全展开
```

Phase 1 只有在当前共有盾通过防御指令强化到5时给 +1 BP。5不是共有盾全局上限，遗物或技能仍可突破5。

reasonId：

```text
shield-fully-deployed
```

注意：

- 如果第一次部署后盾被打破，下一次不再是“强化”，而是重新展开，因此不算“完全展开”。重新展开后的盾若继续通过防御指令强化到5，则算作完全展开。
- 这个奖励是为了让“防御投入”也能成为成长路线的一部分，但数值只有 +1，避免盾牌再次变成固定最优解。
- 如果未来共有盾最大层数、部署次数或盾形态被天气/遗物改变，应从统一的盾状态判断“是否达到当前规则下的完全展开”，不要写死为第二次。

### 6.5 后续技能 BP 接入原则（暂不实装）

主动/被动技能触发 BP 暂时不进入 Phase 1，但这是之后很可能接入的来源。不要在每个技能里手写 `TryGainBp`，否则很快散架。

长期更推荐新增通用事件通道，例如：

```csharp
context.NotifySkillTriggered(owner, skillId, SkillTriggerKind.Active);
```

因为未来以下系统都可能监听同一个事件：

- 技能发动动画
- 技能语音
- BP 奖励
- 成就/任务
- 遗物充能

如果之后决定给技能 BP，原则是：只有技能真实产生战斗结果或日志时才给；尝试但失败、只是“可用”、只是预测阶段都不给。

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
turn-start
break-enemy-shield
own-turn-enemy-hp-damage
shield-fully-deployed
active-skill-triggered（后续）
passive-skill-triggered（后续）
reward-purchase（后续）
reward-skip（后续）
reward-reroll（后续）
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

- 单一 BP HUD 放在战场右侧中段：左边一个战功徽章，右边上下两行数值。
- 上行：敌方 BP / 上限。
- 下行：我方 BP / 上限。
- 样式：旧金币 / 战功徽章 / 令牌感，不做现代进度条。
- 显示格式：`5/10`。
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
<aside id="battle-point-hud" class="battle-point-hud">
  <div class="bp-medal"></div>
  <div class="bp-values">
    <div class="bp-row bp-row--opponent">
      <span>ENEMY</span>
      <strong id="opponent-bp-value">5/10</strong>
    </div>
    <div class="bp-row bp-row--active">
      <span>YOU</span>
      <strong id="active-bp-value">5/10</strong>
    </div>
  </div>
</aside>
```

徽章素材使用 `assets/ui/hud/battle/ui_bp_medal2.png`。位置和大小通过 `wwwroot/styles.css` 的 `--bp-*` 变量调整。

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
- 己方回合内对敌方造成 HP 伤害 +1。
- 只打到盾但没有 HP 伤害，不触发 HP 伤害 BP。
- 打破敌方共有盾 +1。
- 防御阵型完全展开 +1。
- 同一次行动多来源触发时，总进账不超过本回合剩余额度。
- BP 满时不会继续增加，并写出可理解日志。

### 12.2 技能触发（后续阶段）

- Phase 1 暂不接主动/被动技能 BP。
- 后续接入时必须先有统一技能事件或统一通知入口。
- 主动技能未实际生效时不给 BP。
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
8. 接己方回合敌方 HP 伤害 +1。
9. 接破盾 +1。
10. 接防御阵型完全展开 +1。
11. 手动打一局，观察是否变成刷 BP 游戏。

不要一开始就接所有技能。可以先让回合低保、敌方 HP 伤害、破盾、防御完全展开跑通，再进入下一阶段。
