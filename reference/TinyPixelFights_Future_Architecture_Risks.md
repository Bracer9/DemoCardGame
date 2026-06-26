# Tiny Pixel Fights — 后续完整版开发前的架构风险与扩展方向

> 记录日期：2026-06-24  
> 目的：在当前双人对战原型进入 Git 稳定提交前，记录未来扩展到爬塔、永久成长、天气、遗物、副官、可变 Cost/AP 时必须注意的结构性问题。  
> 本文不是立即重构计划，而是后续开新 branch 做完整版开发时的判断基准。

## 1. 总体判断

当前项目很适合现在的目标：

- 8 名角色的双人对战原型
- 本地 / 在线 playtest
- 固定角色基础数值
- 固定技能
- 一场战斗内的 Buff / Debuff / 盾 / 反击 / 预见 / 绝对伤害

但如果之后进入“爬塔 + 永久成长 + 更多角色 + 天气 + 遗物 + 副官 + 可变 Cost/AP”的完整版方向，现在的结构会开始出现明显压力。

问题不是当前代码不能用，而是当前代码仍然是“单场固定对战原型”的结构。继续直接往里面塞永久成长和全局机制，会很容易变成拆东墙补西墙。

最需要提前建立的概念是：

```text
角色模板 Character Template
↓
永久角色实例 Run / Persistent Character
↓
单场战斗角色状态 Battle Character State
↓
当前实际数值 Effective Stats
```

现在项目主要只有：

```text
CharacterDefinition + CharacterState
```

这对当前版本够用，但不足以承载未来的完整版。

## 2. 未来会变动的内容

后续设计已经明确会出现“局内”和“永久”两种生命周期：

- 局内：一场对战内有效。
- 永久：一轮爬塔 / 一次 run 内持续有效。

未来可能变动的内容包括：

| 类型 | 局内变化 | 永久变化 |
|---|---|---|
| 角色基础攻击力 | Buff、Debuff、天气、技能临时修正 | 升级、事件、遗物、训练 |
| 角色 HP / Max HP | 战斗中临时上限变化、损伤、治疗 | 永久成长、诅咒、奖励 |
| 角色副官栏槽位 | 本场战斗临时增加 / 封锁 | 爬塔中永久增加 / 减少 |
| 角色技能可用性 | 沉默、封印、技能冷却、天气禁用 | 解锁、遗忘、替换、升级 |
| 角色名 | 局内称号、诅咒、觉醒名 | 永久改名、称号、职业变化 |
| 总 AP / AP 上限 | 天气、Boss 技能、状态影响 | 遗物、路线奖励、永久惩罚 |
| Buff / Debuff 效果量 | 技能强化、天气放大、遗物加成 | 永久强化某类状态 |
| 角色池 | 本场召唤、临时参战 | 解锁新角色、传奇角色、普通角色 |

这些都要求系统能区分：

- 基础模板值
- 永久修正
- 局内修正
- 当前结算值

## 3. 当前主要结构性风险

### 3.1 基础值和最终值尚未分离

当前代码中大量地方直接使用：

```csharp
character.Definition.Cost
character.Definition.Attack
character.Definition.MaxHp
character.Definition.SkillId
character.Definition.Key
```

这隐含了一个假设：

```text
模板值 = 当前最终值
```

未来这会变成问题。

例如：

- 怪兽获得野兽之怒后 Cost +1。
- 天气让全员本回合 Cost -1。
- 遗物让全员 Cost +1，但攻击力翻倍。
- 角色永久 Max HP +5。
- 技能局内被封印。

这些都不应该修改 `CharacterDefinition`。`CharacterDefinition` 应该只是卡面模板。

未来应该引入类似概念：

```csharp
GetEffectiveCost(state, character, actionContext)
GetEffectiveAttack(state, character, attackContext)
GetEffectiveMaxHp(state, character)
GetEffectiveSkillAvailability(state, character, skillId)
```

也就是说：

```text
Definition = 原始模板
Effective = 当前上下文中的最终计算结果
```

### 3.2 缺少 Run / Campaign 层

当前 `GameState` 是一场对战的状态。

它适合存：

- 当前回合
- 当前 AP
- 玩家
- 角色当前 HP
- 当前 Buff / Debuff
- 战斗日志

但它不适合长期存：

- 爬塔进度
- 永久角色成长
- 永久遗物
- 永久技能解锁 / 封印
- 副官栏槽位
- 战斗外事件结果
- 下一场战斗带入哪些角色

未来应建立：

```text
RunState
├─ roster / 永久角色实例
├─ relics / 遗物
├─ map / 当前层、节点、事件
├─ global progression
└─ currentBattle: BattleState?
```

当前 `GameState` 可以继续存在，但概念上应逐渐变成 `BattleState`。

### 3.3 预测逻辑和实际结算逻辑存在重复

当前 `AttackPreviewService` 会手动预测：

- 伤害范围
- 预见概率
- 盾吸收
- 骑士守护
- 怪兽技能条件
- 德鲁伊技能条件
- Cost 校验

实际战斗结算又在 `GameEngine.Attack()` 中进行。

目前角色少、机制少，仍然可维护。但未来如果加入天气、遗物、副官、技能封印、Cost 修正、攻击类型变化，预测和实际结算很容易不一致。

未来应尽量做到：

```text
Preview 和 Attack 使用同一个规则计算服务
```

至少应让两者共享：

- EffectiveCost
- EffectiveAttack
- DamageModifier pipeline
- Skill availability
- Shield / Guard / Foresight 预估规则

### 3.4 技能绑定仍是模板固定 SkillId

当前角色定义中有：

```csharp
CharacterDefinition.SkillId
```

这意味着一个角色默认拥有一个固定技能。

未来如果出现：

- 技能局内不可用
- 技能永久解锁 / 失效
- 副官给予额外技能
- 遗物给予技能
- 天气禁用某类技能
- 角色技能升级或替换

单一 `SkillId` 会不够。

未来更合理的是：

```text
CharacterTemplate: 初始技能
RunCharacter: 永久技能状态 / 解锁 / 替换
BattleCharacter: 局内技能可用性 / 冷却 / 封印
SkillSource: 技能来源，如角色本体、副官、遗物、战场
```

### 3.5 状态系统方向正确，但不应承载所有规则来源

当前 `StatusEffect` 已经能处理：

- 回合开始
- 回合结束
- 修改基础攻击
- 修改主动攻击
- 是否 Buff
- 是否可驱散

这是好的方向。

但未来不应该把所有机制都塞进 `StatusEffect`。

例如：

- 天气不是角色状态。
- 遗物不是角色状态。
- 永久成长不是角色状态。
- 副官槽位不是角色状态。

未来更好的方向是：

```text
状态、遗物、天气、副官、角色成长
都可以是 modifier source
```

也就是说，它们都能提供 modifier，但它们不一定都是 status。

### 3.6 角色名变化缺少实例层支持

当前角色显示名主要来自：

```text
character.Definition.Key -> locale
```

如果未来角色名可变，例如：

- 觉醒怪兽
- 受诅咒的骑士
- 玩家自定义名字
- 永久称号
- 局内状态名变化

就需要在角色实例层支持：

```text
DisplayNameOverride
Title
LocalizedNameKeyOverride
NameModifier
```

而且仍然不能在 C# 中写中文或日文显示文本。

### 3.7 AP 上限还是常量

当前：

```csharp
public const int MaxActionPoints = 5;
```

以及回合开始：

```csharp
state.ActionPoints = MaxActionPoints;
```

未来如果出现：

- 遗物 AP +1
- 天气 AP -1
- Boss 技能降低本回合 AP
- 某场战斗 AP 上限特殊

就需要改成：

```csharp
GetEffectiveMaxActionPoints(state, player)
GetTurnStartActionPoints(state, player)
```

AP 上限、当前 AP、行动 Cost 都应进入同一套行动经济计算。

## 4. Cost / AP 可变性的建议模型

未来不应直接修改角色基础 Cost。

建议概念：

```text
CharacterDefinition.Cost = 卡面基础 Cost
EffectiveCost = 当前上下文中的实际行动消耗
```

实际行动时，所有地方都使用 `EffectiveCost`：

- 是否可行动
- 预测面板
- 实际扣 AP
- 卡面当前 Cost 显示
- hover 详情
- 在线同步

可能的计算顺序：

```text
1. 从基础 Cost 开始
2. 应用角色自身状态修正
3. 应用队伍光环修正
4. 应用战场 / 天气修正
5. 应用遗物 / 装备 / 事件修正
6. 应用当前行动类型修正
7. 应用一次性临时修正
8. 最终 Cost 至少为 1，除非机制明确写成免费行动
```

例子：

| 机制 | 来源层 | 表达方式 |
|---|---|---|
| 怪兽之怒后 Cost +1 | 角色状态 | CostModifier +1，来源 `beast-rage` |
| 天气全员本回合 Cost -1 | 战场/天气 | 全局 CostModifier -1，生命周期本回合 |
| 遗物全员 Cost +1 攻击翻倍 | 遗物 | CostModifier +1 + AttackModifier ×2 |

注意：遗物同时影响 Cost 和攻击力时，应拆成两个 modifier，而不是写成一个巨大 if。

## 5. 未来应优先建立的核心概念

不建议马上大重构，但进入完整版 branch 后，应优先建立这些边界：

```text
CharacterTemplate
RunCharacter / CharacterInstance
BattleCharacterState
RunState
BattleState
EffectiveStats / RulesCalculator
ModifierSource
```

### 5.1 CharacterTemplate

角色原始模板。

包含：

- key
- 默认美术资源
- 基础 Cost
- 基础 Attack
- 基础 Max HP
- 默认攻击类型
- 默认技能

它不应该被局内或永久成长直接修改。

### 5.2 RunCharacter

一轮爬塔中的永久角色实例。

包含：

- template key
- 永久攻击修正
- 永久 HP 修正
- 永久 Cost 修正
- 永久技能解锁 / 禁用
- 副官槽位
- 永久名字 / 称号
- 装备或长期状态

### 5.3 BattleCharacterState

一场战斗内的角色状态。

包含：

- 当前 HP
- 本场战斗中的临时 Max HP 修正
- 是否已行动
- 局内 Buff / Debuff
- 局内技能封印 / 冷却
- 局内名称变化
- 所属 slot

### 5.4 RulesCalculator / EffectiveStats

统一计算当前实际数值。

负责：

- EffectiveCost
- EffectiveAttack
- EffectiveCounterAttack
- EffectiveMaxHp
- SkillAvailability
- AP 上限
- 行动是否合法

Preview 和实际结算都应该调用它。

### 5.5 ModifierSource

任何能改变规则的来源。

可能来源：

- Status
- Skill
- Aura
- Weather
- Relic
- Adjutant
- Event
- Run upgrade

每个来源提供自己的 modifier，规则计算器统一收集和应用。

## 6. 当前还能继续使用的部分

不是所有东西都需要推倒。

当前比较健康、可以继续保留的方向：

- C# 后端作为唯一规则来源。
- 前端只负责表现，不自己决定规则。
- localization 放在 `wwwroot/locales/*.json`。
- UI 资源通过 manifest 管理。
- 音频通过 `audio.json` 管理。
- 状态使用稳定 ID。
- 技能使用稳定 ID。
- 绝对伤害、盾、预见、守护等机制已经形成较清晰的规则概念。

## 7. 建议的工作流

当前版本如果已经接近一个可 playtest 状态，建议：

1. 在当前 branch 完成最后的小修。
2. 确认测试通过。
3. 提交一个稳定 commit。
4. 打一个 tag，例如 `prototype-v1-pvp-stable`。
5. 从这个 commit checkout 新 branch 做完整版开发。

推荐分支结构：

```text
main / prototype-stable
└─ feature/roguelike-architecture
   ├─ run-state
   ├─ effective-stats
   ├─ modifier-system
   └─ campaign-ui
```

如果改动很大，不建议直接在当前稳定 branch 上继续开发。

## 8. 后续开发时的底线

每次加入新机制前，先问：

```text
这是角色模板变化、永久实例变化、战斗状态变化，
还是全局规则变化？
```

如果这个问题回答不清楚，就不要急着实装。

一句话总结：

> 当前原型可以作为稳定基线保留。完整版开发前最重要的不是先加更多内容，而是先把“模板 / 永久实例 / 战斗状态 / 当前有效值”这四层分清楚。

