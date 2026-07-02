# Tiny Pixel Fights — Action / Ability / Skill 语义重整 Brainwriting

日期：2026-06-28  
阶段：BP Core 与 Dummy Reward Window 已进入实装后，准备进入英雄/普通兵/升级系统前  
目的：统一“技能到底是什么”的语境，避免在英雄成长、普通兵、进阶 class、副官和遗物接入时继续把攻击触发、职业行动、被动特性混成一个概念。

---

## 1. 这份文档的核心判断

现在最重要的问题不是“再给每个角色做几个技能”，而是：

> 当前卡牌在美术和叙事上是角色，但规则上大多数时候只是攻击数值载体。

如果继续在这个基础上直接做英雄升级、普通兵强化、转职和副官，成长内容会自然滑向：

- 攻击 +1
- HP +2
- 物防 +1
- 魔防 +1
- 攻击时额外触发一点效果

这些不是不能有，但如果主要成长都围绕攻击效率，游戏会更像“有漂亮角色皮的数值互砍”，而不是“玩家培养一支有职责分工的小队”。

所以在进入普通兵和英雄升级前，需要先统一一个更清楚的语义：

- **Action / 行动**：玩家主动选择的操作。
- **Attack / 普通攻击**：所有单位都有的基础行动，拖到敌方卡牌发动。
- **Role Action / 职业行动**：角色职业身份最核心的非攻击主动行动。UI 上由选中角色后的左侧 HUD 按钮发动，再进入目标选择或直接执行。
- **Trait / 特性**：不需要玩家直接点选的规则能力，包含攻击触发、常驻被动、反应能力、光环和各种规则 modifier。
- **Ability / 能力**：角色拥有的规则模块总称，可以是 Role Action，也可以是 Trait。
- **Upgrade Talent / 升级天赋**：升级时获得的选择项，它不一定是一个新按钮，可能是解锁、替换或强化 Role Action / Trait / Attack。

建议之后少用泛泛的“技能”来描述系统设计。玩家界面上应逐步改成更清楚的 `Trait` 与 `Role Action` 两栏；设计文档和代码层最好改用更精确的 `Action / RoleAction / Trait / Ability / Talent`。

---

## 2. 当前项目事实：文档与代码里“技能”的含义

从当前 source code 看，技能已经不是一个清晰概念，而是一个历史包袱。

### 2.1 角色定义层

`Domain/CharacterDefinition.cs` 里，每个角色只有一个 `SkillId`：

```text
Key
AssetFile
ColoredAssetFile
Cost
Attack
MaxHp
AttackType
PhysicalDefense
MagicalDefense
SkillId
```

这说明当前角色模型默认“每张卡只有一个技能”。后续如果要有英雄升级、技能锁定、二选一、进阶 class、普通兵技能、副官加成，这个结构肯定不够。

### 2.2 类型层

`Domain/GameTypes.cs` 里目前只有：

```csharp
public enum SkillKind
{
    Active,
    Passive
}
```

但这里的 `Active` 并不等于“玩家主动选择一个技能按钮”。当前文档 `reference/TinyPixelFights_Skills.md` 也写得很清楚：

> 主动技能：角色被命令主动攻击时自动发动，不设置独立技能按钮。

也就是说，当前 `Active Skill` 实际上更接近：

> Attack Trigger / 攻击触发技

它是攻击动作上的附加规则，而不是独立行动。

### 2.3 规则层

`Domain/Skills.cs` 的 `CharacterSkill` 有这些 hook：

- `OnTurnStart`
- `OnAttackDeclared`
- `ModifyOutgoingDamage`
- `ModifyIncomingDamage`
- `OnAfterExchange`
- `OnCharacterDefeated`

这套结构非常适合做“攻击触发技”和“被动特性”，但它不是一个完整的“玩家可选择行动系统”。例如它不能自然表达：

- 骑士点击职业行动后选择共享盾，强化盾。
- 公主点击职业行动后选择 AP 区域，获得本回合 AP 但下回合付代价。
- 德鲁伊点击职业行动后选择一个 buff 图标，净化/驱散。
- 牧师点击职业行动后选择友方卡，治疗或祝福。
- 占卜师点击职业行动后选择奖励窗口，预知/重掷/锁定一个奖励。

这些都不是攻击触发，也不是被动，它们需要独立的 action pipeline。

### 2.4 可控行动层

当前玩家真正能主动做的事主要是：

- `Attack()`
- `DeployShield()`
- `SelectReward()`
- `ResetRewardWindow()`
- `SkipRewardWindow()`
- `EndTurn()`

其中只有 `Attack()` 是角色卡牌动作；`DeployShield()` 是公共指令，不属于某个角色。前端拖拽也主要服务于“拖攻击者到敌方卡牌”。

所以玩家体感上会变成：

> 我选了角色，但角色唯一能主动表达自己的方式还是打人。

这个问题会在公主、德鲁伊、骑士、未来牧师/术士/盾兵上尤其明显。

### 2.5 BP / Reward 当前实装事实

当前代码里 BP 已经有稳定底座：

- 初始 BP：5
- 上限：10
- 单回合获取上限：3
- 回合开始低保
- 造成敌方 HP 伤害
- 破坏敌方共有盾
- 完全展开防御阵型
- reward purchase / reroll / skip 消耗或获得 BP

Dummy reward 已经不只是“纯扣 BP”，它顺便接了未来遗物 baseline：通过不可驱散 status 给当前在场角色加魔防、魔法攻击或攻击。

这很好，因为 reward pipeline 已经能承载未来成长内容；但也意味着下一步更不能糊涂地把“奖励给一个技能”写死成“给角色多一个攻击触发器”。

---

## 3. 现在的设计问题：角色幻想和玩家动词不匹配

当前 8 个角色的美术、语音、名字都在传达“她们是不同的人”。但行动层基本只有：

```text
选择角色 -> 拖向敌人 -> 攻击 -> 结算攻击附加效果/被动
```

这让角色身份被压扁。

### 3.1 公主的问题

公主现在的核心价值是回血光环，但玩家并不是“操作公主进行祈祷/鼓舞/指挥”，而是公主站在那里自动回血。她攻击时只是 1 点物理攻击，操作体验和职业幻想几乎不匹配。

如果未来公主升级只是让回血更多，那她会变成“行走的 aura”，不是玩家培养的角色。

### 3.2 骑士的问题

骑士应该是守护、代伤、防御阵型、站在前面的角色。但现在守护是被动自动触发，玩家没有“我让骑士保护了她”的操作感。

骑士攻击当然可以存在，但“骑士只能通过攻击表达自己”非常可惜。

### 3.3 德鲁伊的问题

德鲁伊现在稍微好一些，因为衰弱/驱散有明确功能。但她仍然被绑定在“攻击后触发”。如果德鲁伊想净化友军、驱散敌方 buff、播撒自然回复，都必须先打人，这会限制自然系辅助/控制角色的幻想。

### 3.4 普通兵的问题会更早爆炸

即将加入四类普通兵：

- Duelist
- Shieldmaiden
- Arcanist
- Cleric

如果她们也都只是攻击，那么 `Cleric` 会变成“拿法杖敲人的牧师”，`Shieldmaiden` 会变成“防高但只会打人的盾卫”。这和我们希望普通兵降低认知负担、同时支撑队伍结构的目标冲突。

---

## 4. 建议采用的新术语

### 4.1 Ability / 能力

`Ability` 是总称，表示“卡牌拥有的非纯数值规则”。

为了降低玩家心智负担，顶层只保留两类：

| 类型 | 是否玩家主动选择 | 是否消耗 AP | 是否占用行动 | 例子 |
|---|---|---:|---:|---|
| Role Action | 是 | 通常是 | 通常是 | 骑士强化盾、公主祈祷、牧师治疗 |
| Trait | 否 | 否 | 否 | 炎上触发、公主治疗、骑士代伤、占卜师光环、魔法伤害 modifier |

`Attack` 是基础行动，不需要作为 Ability 顶层类型参与角色成长选择。所有单位默认拥有 Attack。

过去讨论里的 `AttackTriggerTrait`、`PassiveTrait`、`ReactiveTrait`、`Aura`、`Modifier` 不应与 `RoleAction` 平级，而应理解为 Trait 的后端描述字段：

```text
Trait
- TriggerKind：OnAttack / TurnStart / TurnEnd / OnDamaged / OnShieldBreak / OnAllyDefeated / Continuous...
- ScopeKind：Self / Ally / Enemy / Team / Global...
- EffectKind：DamageModifier / DefenseModifier / CostModifier / Heal / Shield / Status / BP / RewardModifier...
```

也就是说，UI 层玩家只看到 `Trait`；hover 里可以用小标签提示“攻击触发 / 常驻 / 反应 / 光环”。代码层保留触发时点、作用范围和效果类型，避免结算混乱。

### 4.2 Action / 行动

`Action` 是玩家可以选择的操作。

当前应该至少有：

- `AttackAction`
- `DeployShieldCommand`
- `RoleAction`
- `EndTurn`
- `RewardSelect`
- `RewardReset`
- `RewardSkip`

未来也可以有：

- `AttachAdjutant`
- `StackSoldier`
- `UpgradeHero`
- `SwapUnit`

### 4.3 Skill / 技能

建议不要在设计层继续把 `Skill` 当成技术总称。

可以这样处理：

- 玩家界面上：短期可以兼容旧 `SKILL` 文案，但新 HUD 建议直接分成 `Trait` 与 `Role Action`。
- 文档语义上：`Skill` 是旧名，逐步替换为 `Ability`。
- 代码迁移上：当前 `CharacterSkill` 可以先保留，但新系统应新增更明确的结构，而不是继续把所有东西塞进 `SkillKind.Active/Passive`。

### 4.4 Role Action / 职业行动

`Role Action` 是这次最关键的新概念。

定义：

> 角色被选中后，在左侧 HUD 的 `Role Action` 区域点击职业行动按钮，执行符合自身职业幻想的主动行动。

第一版交互建议：

- 选中角色后，左侧 HUD 显示三个区域：Status / Trait / Role Action。
- 点击 Role Action 按钮后进入目标选择状态。
- 合法目标高亮；目标可以是友方卡、敌方卡、自己、共享盾、AP 区域、BP 徽章、状态图标、空位。
- 如果行动没有目标，例如自我蓄力或全体鼓舞，可以点击按钮后直接 preview / confirm。
- 有 AP cost。
- 大多数情况下消耗该角色本回合行动。

这样玩家的动词会从：

> 谁打谁？

扩展为：

> 她这一回合是攻击、守护、治疗、净化、蓄力、指挥，还是攒资源？

---

## 5. 关键原则：每张卡最多一个职业行动

我同意用户的直觉：职业行动应是选中角色后的明确按钮，而不是继续把所有意图都塞进拖拽。即便改成按钮，一个角色同时有多个职业行动也会让 UI 和玩家认知迅速爆炸。

建议第一版规则：

```text
每张在场卡：
- 一定拥有 Attack。
- 最多拥有 1 个 Role Action。
- 可以拥有若干 Trait，但它们不是玩家额外选择的按钮。
```

升级与进阶不应该不断给角色加新按钮，而应该主要做三件事：

1. **解锁 Role Action**
2. **替换 Role Action 的形态**
3. **给 Role Action / Attack / Trait 增加 modifier**

这样系统既能有成长组合，又不会变成一张卡四五个按钮。

---

## 6. 那么“第一次升级二选一技能”到底是什么？

建议把它改名为：

> Basic Ability Choice / 初级能力选择

它不一定是一个“技能按钮”，而是一包能力变化。

第一版可以这样定义：

```text
英雄白卡：
- 只有 Attack。
- 可以看到两个锁定的 Basic Ability 候选。

第一次英雄升级：
- 从两个 Basic Ability Package 中选择一个。
- 该 package 通常会解锁或定义这名英雄的 Role Action。
- 它也可以附带一个小 Trait；如果这个 Trait 随攻击触发，就在后端标记为 `TriggerKind = OnAttack`。
```

也就是说，升级二选一不再是：

> 选一个技能，塞到 SkillId。

而是：

> 选择这名英雄在本局里的基础战斗身份。

### 6.1 为什么不是直接给两个主动技能？

因为如果每个英雄都能：

- 攻击
- 主动技 A
- 主动技 B
- 被动
- 终极技

那 UI、预测、AI/在线同步、平衡都会爆。对战 prototype 还没到这一步。

### 6.2 为什么也不能全是被动？

如果第一次升级二选一都是被动，玩家仍然没有“操作角色”的感觉。辅助角色会继续站着自动生效，低攻击角色会继续不知道该不该拿去攻击。

所以建议规则：

> 每个英雄第一次升级的两个选项里，至少一个必须明显改变玩家可主动选择的行动；最好两个都围绕 Role Action 的不同方向展开。

---

## 7. 进阶 class 的定位

第三次英雄升级选择进阶 class 时，不建议简单加一个“高级技能按钮”。更好的定位是：

> Advanced Class 改写角色的核心行动模式。

它可以：

- 强化 Role Action。
- 替换 Role Action 的目标类型。
- 让 Attack 与 Role Action 产生联动。
- 新增强 Trait。
- 改变基础属性和防御倾向。
- 改变卡面立绘。

但它最好不要让角色从“1 个职业行动”变成“2 个职业行动”。如果真的有第二个特殊行动，也应该是非常稀有的终局例外，而不是系统常态。

### 7.1 推荐成长结构

```text
Level 0 / 白卡：
- Attack
- 锁定的能力预览

Level 1 / 初级能力：
- 选择 Basic Ability A 或 B
- 解锁 1 个 Role Action 或核心 Trait package

Level 2 / 熟练：
- 属性成长
- 或让已选 Basic Ability 获得小幅强化

Level 3 / 进阶 class：
- 选择 Advanced Class A 或 B
- 立绘变化
- 属性倾向变化
- Role Action 进化
- 获得 Advanced Trait
```

### 7.2 搭配组合怎么产生？

组合来自两层：

1. Level 1 的 Basic Ability 选择。
2. Level 3 的 Advanced Class 选择。

例如公主：

- Level 1 选了 `Prayer`：更偏治疗。
- Level 3 选了 `Dark Lord Princess`：治疗变成带代价的黑暗祝福。

或者：

- Level 1 选了 `Command`：更偏 AP/BP 调度。
- Level 3 选了 `High Priestess`：指挥变成神圣鼓舞，附带净化/治疗。

这样同一个进阶 class 也会因为初级能力不同而形成不同局内构筑。

---

## 8. 当前 8 名英雄的现有技能重分类

| 角色 | 当前 SkillId | 当前代码含义 | 新语义建议 |
|---|---|---|---|
| Princess | `saints-prayer` | 回合开始光环治疗 | Trait：TurnStart / Team / Heal，不是职业行动 |
| Oracle | `stargazers-aegis` | 概率减伤 + 魔法增伤光环 | Trait：Continuous / Team / DamageModifier |
| Peasant | `spring-harvest` | 首次攻击播种，下回合强化 | Trait：OnAttack + Delayed / Self / AttackModifier |
| Mage | `searing-mark` | 攻击后 50% 炎上 | Trait：OnAttack / Enemy / Status |
| Druid | `weakening-spores` | 攻击后驱散/衰弱 | Trait：OnAttack / Enemy / Dispel + Status，未来可拆成 Role Action |
| Barbarian | `aftershock-axe` | 高伤后邻接溅射 | Trait：OnAttackResolved / EnemyAdjacent / Damage |
| Monster | `predatory-instinct` | 0 伤绝对追击 + 公主死亡暴走 | Trait：OnZeroDamage / OnAllyPrincessDefeated |
| Knight | `interposing-shield` | 守护代伤 | Trait：Reactive / Ally / Guard |

这个表的结论很重要：

> 现有 8 个“技能”里，几乎没有一个是真正的 Role Action。

所以如果我们不新增 Role Action 系统，只是在 `SkillId` 上做锁定/解锁，角色行动范围仍然太窄。

---

## 9. 职业行动的 UI 目标类型

职业行动不再要求拖动卡牌触发。更推荐的 UI 流程是：

```text
选中角色 -> 左侧 HUD 显示 Role Action 按钮 -> 点击按钮 -> 高亮合法目标 -> 点击目标 -> preview / confirm -> 执行
```

这样 Attack 仍然可以保留拖拽手感，而 Role Action 用按钮表达“这名角色正在执行自己的职业工作”。两者在玩家心智上会更清晰。

建议第一批目标类型：

| Target Kind | 目标 | 例子 |
|---|---|---|
| EnemyCard | 敌方卡牌 | 德鲁伊施加衰弱、占卜师标记 |
| AllyCard | 友方卡牌 | 公主治疗、牧师祝福、骑士守护指定角色 |
| SelfCard | 自己 | 狂战士蓄怒、法师蓄力 |
| OwnShield | 我方共享盾 | 骑士强化盾、盾兵架盾 |
| EnemyShield | 敌方共享盾 | 破盾、腐蚀盾 |
| ActionPointPanel | AP 区域 | 公主指挥、农民补给 |
| BpMedal | BP 徽章 | 占卜师预支/锁定命运、商人类未来单位 |
| StatusIcon | 状态图标 | 德鲁伊净化/驱散、牧师驱散 debuff |
| EmptySlot | 空位/后备位 | 召唤、换位、布阵，后期再考虑 |

第一版不要全做。建议先做：

- AllyCard
- OwnShield
- ActionPointPanel
- EnemyCard

这四个足够覆盖骑士、公主、德鲁伊、普通牧师/盾兵。

---

## 10. Role Action 与 BP 的关系

原计划里有“主动技能 +2 BP、被动触发 +1 BP”。但在新语义下要重新判断。

如果 `Active Skill` 仍是“攻击触发技”，那奖励它 +2 BP 会把游戏继续推向攻击中心。

更合理的是：

```text
攻击造成 HP 伤害：+1 BP
破坏敌方共享盾：+1 BP
有效 Role Action：+1 BP
高价值/有代价 Role Action：+2 BP
自动被动：默认不给 BP，除非是稀有且可见的战术触发
单回合总获取上限仍为 3
```

“有效 Role Action”必须有实际效果：

- 治疗必须真的恢复 HP。
- 净化必须真的移除 debuff。
- 驱散必须真的移除 buff。
- 强化盾必须真的增加盾值。
- 指挥 AP 必须真的改变本回合 AP。

避免玩家空放治疗、空放净化刷 BP。

### 10.1 被动是否给 BP？

谨慎。

被动是系统自动触发，不是玩家当前回合做出的直接选择。默认给 BP 会让公主/占卜师这类光环角色在不操作时自动赚钱，可能和“BP 奖励主动决策”的方向冲突。

建议：

- `TriggerKind = Continuous / TurnStart / TurnEnd` 的 Trait 默认不给 BP。
- `TriggerKind = OnDamaged / OnShieldBreak / OnAllyDefeated` 等反应型 Trait 如果非常稀有、明确改变局势，可以给 +1，但必须逐个标记。
- Role Action 是 BP 与角色主动性结合的主要入口。

---

## 11. 英雄方向 Brainwriting

以下不是最终数值，只是方向草案。重点是让每名英雄都有“攻击以外的动词”。

### 11.1 Princess / 公主

当前问题：她是治疗与指挥幻想最强的角色，但当前玩家只能拿她打 1 点物理。

核心身份：

- 治疗
- 鼓舞
- AP/BP 调度
- 祝福
- 高级方向可分为神圣支援与黑暗统治

Basic Ability 候选：

1. **Saint's Prayer / 圣女祈祷**
   - Role Action：选择友方卡牌。
   - 效果：治疗 2，若目标身上有 debuff，额外净化 1 个可净化 debuff。
   - 代价：消耗 1 AP，公主本回合行动结束。
   - 设计意图：让公主从自动回血光环变成玩家主动治疗的角色。

2. **Royal Command / 王令指挥**
   - Role Action：选择 AP 区域。
   - 效果：本回合 +1 AP；下个己方回合开始时 AP -1，或本回合获得的 BP 上限 -1。
   - 代价：消耗 1 AP，公主本回合行动结束。
   - 设计意图：制造“现在爆发 vs 之后还债”的 JRPG 指挥感。

Advanced Class A：**High Priestess / 大神官**

- 立绘参考：白金黑红祭司袍、公主权杖方向。
- 强化 Saint's Prayer：治疗后赋予 `Blessing`，目标下次受到伤害 -1。
- 强化 Royal Command：指挥还会给一名最低 HP 友军治疗 1。
- Trait：公主在场时，第一次有效治疗额外 +1 BP，受回合上限限制。

Advanced Class B：**Dark Lord Princess / 暗黑女王**

- 立绘参考：黑红披风、剑、威严统治。
- 强化 Saint's Prayer：治疗改为 `Blood Benediction`，治疗 3，但公主失去 1 HP 或目标获得一个轻微代价状态。
- 强化 Royal Command：本回合 +2 AP，但下个己方回合 AP -1，且公主获得“冷酷统治”不可驱散状态。
- Trait：当友军阵亡时获得 BP 或给全队攻击 +1 一回合。

设计警告：

- AP 增加非常危险，因为它改变行动经济。必须有清楚代价。
- 公主不能同时是最强治疗、最强 AP、最强 BP，否则会成为必选。

### 11.2 Oracle / 占卜师

进阶立绘观察：

- 白袍星杖方向：神圣、星象、保护、命运引导。
- 黑裙卡牌怀表方向：赌博、命运操纵、时间、重掷。

核心身份：

- 预见
- 概率控制
- 奖励/抽选/重掷
- 魔法队支援
- 不是直接输出角色

Basic Ability 候选：

1. **Star Reading / 星读**
   - Role Action：选择友方卡牌。
   - 效果：目标获得 `Foresight`，下一次受到伤害时固定减 1，而不是概率。
   - 代价：1 AP。
   - 设计意图：把当前全队概率光环收束成玩家选择保护谁。

2. **Fate Mark / 命运标记**
   - Role Action：选择敌方卡牌。
   - 效果：标记目标；我方下一次攻击该目标时，伤害预测更稳定，或最低伤害 +1。
   - 代价：1 AP。
   - 设计意图：占卜师不是打人，而是决定战局焦点。

Advanced Class A：**Astral Oracle / 星界神谕者**

- 强化 Star Reading：同时保护目标与相邻友军，或给目标附加魔防 +1。
- 强化 Fate Mark：标记目标受到魔法伤害 +1。
- Trait：奖励窗口出现时，第一次 reset 免费次数 +1 或能预览一个隐藏奖励权重。

Advanced Class B：**Fate Dealer / 命运赌徒**

- 强化 Star Reading：保护变成 50% 完全闪避 / 50% 无效，风险更高。
- 强化 Fate Mark：标记目标时掷骰，可能获得 BP，也可能给敌方 BP。
- Trait：每个奖励窗口可锁定一个选项后 reset 另外两个。

设计警告：

- 操纵奖励窗口很有味道，但开发上会碰 reward service、UI、在线同步。不要第一版就做。
- 第一版先做战斗内 Star Reading / Fate Mark。

### 11.3 Peasant / 农村少女

进阶立绘观察：

- 金色麦穗重装方向：从农民成长为丰收守护者/民兵队长。
- 白金丰收圣女方向：从村姑成长为丰收祭司/补给圣女。

核心身份：

- 低 cost
- 补给
- 播种/收获
- 修盾
- 小治疗
- 让弱角色通过规划变强

Basic Ability 候选：

1. **Supply Basket / 补给篮**
   - Role Action：选择友方卡牌。
   - 效果：治疗 1；如果目标满血，则给目标下一次攻击 +1。
   - 代价：1 AP。
   - 设计意图：让农民低 cost 有支援价值，而不是只能用 1 AP 打 2。

2. **Field Work / 田间劳作**
   - Role Action：选择 AP 区域或自己。
   - 效果：获得 `Harvest Pending`；下个己方回合第一次行动 cost -1 或攻击 +2。
   - 代价：1 AP。
   - 设计意图：保留现有播种/收获特色，但变成玩家主动投资。

Advanced Class A：**Harvest Guard / 丰收卫士**

- 强化 Supply Basket：治疗后给目标物防 +1 一回合。
- 强化 Field Work：收获时同时强化共享盾。
- Trait：每次强化共享盾时，农民若在场可治疗最低 HP 友军 1。

Advanced Class B：**Harvest Saint / 丰收圣女**

- 强化 Supply Basket：治疗 1 变治疗 2，满血时给祝福。
- 强化 Field Work：下回合开始获得额外 BP 或低保治疗。
- Trait：奖励窗口 skip 时，额外治疗最低 HP 友军 1。

设计警告：

- 农民如果 1 AP 又补 AP 又治疗又加攻，会成为万能单位。
- 她的强点应该是效率和准备，不是单回合爆发。

### 11.4 Barbarian / 狂战士

进阶立绘观察：

- 金白巨斧方向：更像荣耀战斧、正面突破、太阳战狂。
- 龙骑方向：更像掠袭、机动、怪物伙伴、空中突击。

核心身份：

- 高攻
- 风险回报
- 溅射
- 挑衅
- 自伤/牺牲防御换爆发

Basic Ability 候选：

1. **War Cry / 战吼**
   - Role Action：选择自己。
   - 效果：本回合下一次攻击 +1，但本回合魔防/物防 -1。
   - 代价：1 AP。
   - 设计意图：让狂战士可以选择蓄力爆发，而不是永远直接砍。

2. **Provoke / 挑衅**
   - Role Action：选择敌方卡牌。
   - 效果：目标下回合若攻击，优先/只能攻击狂战士，或攻击其他目标时伤害 -1。
   - 代价：1 AP。
   - 设计意图：给高攻脆皮一点战术控制，制造风险。

Advanced Class A：**Crimson Warlord / 赤斧战王**

- 强化 War Cry：若攻击造成 3+ 伤害，触发更强余波。
- 强化 Provoke：被挑衅目标攻击狂战士时，狂战士反击 +1。
- Trait：每次击破盾或击败敌人，获得一层 Rage。

Advanced Class B：**Dragon Raider / 龙骑掠袭者**

- 强化 War Cry：下一次攻击可无视一部分盾或对盾额外伤害。
- 强化 Provoke：挑衅变成龙威，影响目标及相邻敌人。
- Trait：攻击边缘/孤立目标时额外 +1 伤害。

设计警告：

- 她已经攻击高，Role Action 不能只是无脑 +攻。
- 必须有防御下降、目标限制、下回合代价等风险。

### 11.5 Monster / 恶魔少女

进阶立绘观察：

- 镜子方向：自我、诱惑、反射、交换、诅咒。
- 红黑魔杖方向：深渊、绝对伤害、献祭、支配。

核心身份：

- 反常规
- 绝对伤害
- 诱导对手防御后反杀
- 与公主/死亡事件绑定
- 可能拥有代价机制

Basic Ability 候选：

1. **Predatory Gaze / 捕食凝视**
   - Role Action：选择敌方卡牌。
   - 效果：目标获得 `Marked Prey`；如果目标下次受到 0 点攻击伤害，额外受到绝对伤害。
   - 代价：1 AP。
   - 设计意图：把当前“0伤追击”从隐藏攻击触发变成玩家主动布置陷阱。

2. **Dark Pact / 黑暗契约**
   - Role Action：选择友方卡牌或自己。
   - 效果：目标失去 1 HP，获得攻击 +1 或 BP +1。
   - 代价：1 AP。
   - 设计意图：恶魔少女通过代价换资源/力量。

Advanced Class A：**Mirror Fiend / 镜魔**

- 强化 Predatory Gaze：目标受到的下一次治疗/祝福转为伤害或被复制给怪物。
- 强化 Dark Pact：契约目标受到攻击时，部分伤害反射给攻击者。
- Trait：第一次受到致命伤时，若敌方有 Marked Prey，可消耗标记免死。

Advanced Class B：**Abyssal Queen / 深渊女王**

- 强化 Predatory Gaze：绝对伤害提高，但对公主/神圣单位有特殊限制。
- 强化 Dark Pact：可献祭友军状态或 BP，换取更高爆发。
- Trait：任意公主阵亡后获得不可驱散的 Abyssal Rage。

设计警告：

- 绝对伤害是最危险的伤害类型。它绕过盾、防御、预见，必须少量、带条件、有清楚预告。
- 她很适合做“反防御”角色，但不能让防御完全失去意义。

### 11.6 Knight / 骑士

核心身份：

- 守护
- 代伤
- 加盾
- 防御姿态
- 让玩家主动保护关键角色

Basic Ability 候选：

1. **Guard Oath / 守护誓约**
   - Role Action：选择友方卡牌。
   - 效果：直到下个己方回合，目标受到的第一次主动物理伤害 -2，骑士承受 1 点伤害。
   - 代价：2 AP。
   - 设计意图：把被动代伤变成玩家指定保护对象。

2. **Raise Bulwark / 举盾固阵**
   - Role Action：选择我方共享盾。
   - 效果：共享盾 +2；若已有盾，额外赋予全队一次反击减伤。
   - 代价：2 AP。
   - 设计意图：让骑士成为“盾系统专家”，不是普通公共指令的旁观者。

Advanced Class A：**Aegis Knight / 圣盾骑士**

- 强化 Guard Oath：可保护魔法伤害一次。
- 强化 Raise Bulwark：盾存在时全队魔防 +1。
- Trait：盾被打破时，骑士获得 BP 或反击强化。

Advanced Class B：**Vanguard Knight / 前锋骑士**

- 强化 Guard Oath：保护后骑士下一次攻击 +1。
- 强化 Raise Bulwark：强化盾后骑士可获得一次低 cost 攻击机会。
- Trait：骑士攻击拥有护盾的敌人时破盾效率提高。

设计警告：

- 防御行动容易拖局。骑士的保护要创造反击窗口，而不是无限延长时间。

### 11.7 Mage / 魔法使

核心身份：

- 魔法爆发
- 炎上
- 蓄力
- 破防/破盾
- 范围压制

Basic Ability 候选：

1. **Arcane Channel / 魔力蓄积**
   - Role Action：选择自己。
   - 效果：获得 `Charged`；下次魔法攻击 +2 或炎上概率提高。
   - 代价：1 AP，不能同回合攻击。
   - 设计意图：给法师一个“我这回合准备，下回合爆发”的动词。

2. **Searing Brand / 灼热刻印**
   - Role Action：选择敌方卡牌施加燃烧标记；也可以保留为 OnAttack Trait。
   - 效果：目标下回合开始受魔法伤害，或受到下一次魔法攻击 +1。
   - 代价：1-2 AP。
   - 设计意图：把当前概率炎上改为更可规划的魔法控制。

Advanced Class A：**Pyromancer / 炎术师**

- 强化 Searing Brand：燃烧可扩散或破盾后额外触发。
- Trait：攻击燃烧目标时获得 BP 或额外伤害。

Advanced Class B：**Runesage / 符文贤者**

- 强化 Arcane Channel：可给友方魔法单位充能。
- Trait：魔法伤害会临时降低目标魔防。

设计警告：

- 法师已经 4 攻，任何直接增伤都要小心。
- 蓄力行动可以自然地用“放弃本回合攻击”作为代价。

### 11.8 Druid / 德鲁伊

核心身份：

- 净化
- 驱散
- 衰弱
- 自然回复
- 把状态系统玩得最清楚

Basic Ability 候选：

1. **Cleansing Herbs / 净化草药**
   - Role Action：选择友方卡牌或状态图标。
   - 效果：移除 1 个 debuff；如果成功，治疗 1。
   - 代价：1 AP。
   - 设计意图：让德鲁伊不必攻击队友才能体现辅助价值。

2. **Weakening Spores / 衰弱胞子**
   - Role Action：选择敌方卡牌。
   - 效果：移除 1 个可驱散 buff，并给目标下回合攻击 -2。
   - 代价：1 AP。
   - 设计意图：把当前攻击触发控制技转成明确的控制行动。

Advanced Class A：**Grove Keeper / 林庭守护者**

- 强化 Cleansing Herbs：净化后赋予自然回复。
- Trait：友方被净化时，德鲁伊获得 BP 或给共享盾 +1。

Advanced Class B：**Spore Witch / 胞子女巫**

- 强化 Weakening Spores：可同时驱散 buff 并降低魔防。
- Trait：敌方获得 buff 时，有机会给其附加弱化标记。

设计警告：

- 德鲁伊是状态系统压力测试角色。她的 UI 必须解释“哪些状态可驱散/可净化”。

---

## 12. 普通兵设计：简单，但必须有职业身份

普通兵的目标不是做成小英雄，而是降低开局认知负担、补足队伍动词。

建议普通兵第一版有两种可选路线：

### 12.1 保守路线

```text
初始普通兵：
- Attack only

第一次普通兵强化：
- 解锁固定 Role Action
```

优点：开局最简单。  
缺点：前期仍然有“普通兵只会攻击”的问题。

### 12.2 推荐路线

```text
初始普通兵：
- Attack
- 1 个非常短的 Role Action

普通兵强化：
- 提升属性
- 最后一级强化这个 Role Action
```

优点：从第一局就能验证角色行动空间。  
缺点：开局信息量略增。

我更推荐第二条，但文案必须短。

### 12.3 四类普通兵草案

| Class | 基础定位 | Role Action | 强化方向 |
|---|---|---|---|
| Duelist | 物理高攻脆皮 | `Feint`：选择敌人，下次攻击该目标 +1 | Crimson 偏斩杀，Phantom 偏闪避/反击 |
| Shieldmaiden | 物理高防弱攻 | `Brace`：选择共享盾，盾 +1 | Iron 偏物防，Aegis 偏魔防/群体护盾 |
| Arcanist | 魔法高攻脆皮/高魔防 | `Focus`：选择自己，下次魔法攻击 +1 | Ember 偏燃烧，Astral 偏预见/魔防穿透 |
| Cleric | 治疗/驱散辅助 | `Mend`：选择友军，治疗 1 | White 偏净化，Saint 偏祝福/复苏 |

普通兵的 Role Action 描述应尽量在一行内说完。

---

## 13. 副官系统应怎么接

副官如果直接给英雄新增一个按钮，会再次制造 UI 爆炸。

建议副官定位为：

> Soldier as Modifier / 普通兵成为英雄能力的 modifier。

副官不提供新的 Role Action，而是修改英雄已有的 Attack / Role Action / Trait。

例子：

| 副官来源 | 附加到英雄后的效果 |
|---|---|
| Duelist | 英雄攻击被其 Role Action 标记的目标时 +1 伤害 |
| Shieldmaiden | 英雄使用 Role Action 后，我方共享盾 +1 |
| Arcanist | 英雄 Role Action 造成/强化的下次伤害变为魔法或额外 +1 |
| Cleric | 英雄 Role Action 若作用于友方，额外治疗 1 或净化 1 |

这样副官系统会形成组合：

```text
英雄自身 Basic Ability
+ 英雄 Advanced Class
+ 副官 Modifier
+ 遗物 Modifier
```

但玩家仍然只需要操作：

```text
Attack 或 1 个 Role Action
```

这是控制复杂度的关键。

---

## 14. 奖励系统与 Ability 的连接方式

未来三选一奖励不应该只给“数值 +1”。可以有这些类型：

### 14.1 Unlock Reward

- 解锁英雄 Basic Ability。
- 解锁普通兵 Role Action。
- 解锁副官槽。

### 14.2 Upgrade Reward

- 已有 Role Action +1 效果。
- Role Action cost -1，但最低为 1。
- Role Action 增加一个新目标类型。

### 14.3 Class Reward

- 英雄进入 Advanced Class。
- 普通兵进阶为 Crimson / Phantom / Iron / Aegis 等。

### 14.4 Modifier Reward

- 本局所有治疗 +1。
- 第一次有效 Role Action 返还 1 BP。
- 共享盾被强化时，随机友军治疗 1。

### 14.5 Acquisition Reward

- 获取新普通兵。
- 获取新英雄。
- 获取副官素材。

奖励窗口的 UI 需要显示：

- 它是解锁、升级、进阶、遗物、招募中的哪一种。
- 消耗 BP。
- 作用对象。
- 是否需要之后在手牌/队伍里手动执行。

---

## 15. 对 GDD 的建议修改方向

后续 GDD 应修改以下段落。

### 15.1 核心战斗循环

当前 GDD 的循环是：

```text
选择行动 -> 用角色攻击 / 展开共有盾 / 结束回合
```

建议改成：

```text
选择行动
-> 用角色攻击
-> 使用角色职业行动
-> 使用公共战术指令
-> 处理奖励窗口
-> 结束回合
```

### 15.2 角色与小队

当前写“主动或被动技能”。建议改为：

- 基础攻击
- 职业行动
- 被动/触发特性
- 成长能力槽

### 15.3 技能、buff 与 debuff

当前“主动技能：角色主动攻击时触发”需要保留为历史定义，但新增正式术语：

- Role Action
- Trait

Trait 的 hover / 后端标签可以包含：

- 攻击触发：`TriggerKind = OnAttack`
- 常驻：`TriggerKind = Continuous`
- 回合触发：`TriggerKind = TurnStart / TurnEnd`
- 反应：`TriggerKind = OnDamaged / OnShieldBreak / OnAllyDefeated`
- 光环：`ScopeKind = Team / Global`
- 规则修改：`EffectKind = DamageModifier / CostModifier / RewardModifier`

### 15.4 BP

GDD 里当前 BP 获取规则还没有写 Role Action。建议待 Role Action 试点后改为：

```text
回合低保：+1
主动攻击造成生命伤害：+1
破坏敌方共有盾：+1
有效职业行动：+1 或 +2
单回合最多获得 3
```

是否保留“完全展开防御阵型 +1 BP”，需要 playtest 后决定。它现在在代码里存在，但长期可能会鼓励固定先开盾。

---

## 16. 推荐下一步开发顺序调整

BP Core 和 Dummy Reward 已经完成后，不建议立刻进入“英雄/普通兵分类 + 技能锁定”。建议中间插入一个语义与行动系统 phase。

### Phase 3A：Ability Taxonomy Scaffold

目标：不改变玩法，只重整数据语义。

- 新增 `CardType`：Hero / Soldier。
- 新增 `AbilityKind` 概念：RoleAction / Trait。
- 新增 Trait 细分字段：`TriggerKind` / `ScopeKind` / `EffectKind`。
- 当前 `CharacterSkill` 暂时映射为旧 `SkillAbility`。
- UI 文案短期可兼容旧 `SKILL`，但新左侧 HUD 应分成 `Trait` 与 `Role Action`。

### Phase 3B：Role Action Pipeline

目标：让角色可以执行非攻击行动。

先做两个试点：

1. Knight：点击 Role Action 后选择共享盾，强化盾。
2. Princess：点击 Role Action 后选择 AP 区域或友方卡，指挥/治疗。

需要：

- RoleAction preview。
- 目标合法性判断。
- AP cost。
- 是否消耗行动。
- 日志。
- BP 有效行动奖励。
- 在线同步。

### Phase 3C：Skill Lock / Unlock

目标：把升级二选一接到 Role Action / Ability package。

- 白卡英雄显示锁定能力。
- 第一次升级选择一个 Basic Ability package。
- package 解锁或定义该英雄的 Role Action。

### Phase 3D：普通兵

目标：普通兵不是小英雄，但有明确动词。

- Duelist / Shieldmaiden / Arcanist / Cleric。
- 每个最多 1 个短 Role Action。
- 强化时修改该 Role Action。

### Phase 3E：Start Draft

目标：从随机 3 英雄选 1 + 四普通兵选 2 开始正式 playtest。

---

## 17. 技术架构草案

不要一次性重构成大型卡牌引擎，但可以为未来留接口。

### 17.1 类型草案

```csharp
public enum CardType
{
    Hero,
    Soldier
}

public enum AbilityKind
{
    RoleAction,
    Trait
}

public enum TraitTriggerKind
{
    Continuous,
    TurnStart,
    TurnEnd,
    OnAttack,
    OnAttackResolved,
    OnDamaged,
    OnShieldBreak,
    OnAllyDefeated,
    OnRewardWindow
}

public enum TraitScopeKind
{
    Self,
    Ally,
    Enemy,
    Team,
    Global
}

public enum TraitEffectKind
{
    Damage,
    Heal,
    Shield,
    Status,
    Dispel,
    DamageModifier,
    DefenseModifier,
    CostModifier,
    BpModifier,
    RewardModifier
}

public enum ActionKind
{
    Attack,
    RoleAction,
    DeployShield,
    AttachAdjutant,
    RewardSelect,
    RewardReset,
    RewardSkip,
    EndTurn
}

public enum RoleActionTargetKind
{
    SelfCard,
    AllyCard,
    EnemyCard,
    OwnShield,
    EnemyShield,
    ActionPointPanel,
    BpMedal,
    StatusIcon
}
```

### 17.2 角色定义草案

```text
CharacterDefinition
- Key
- CardType
- BaseClass
- CurrentClass / AdvancedClass candidates
- Cost
- Attack
- MaxHp
- AttackType
- Defense
- BasicAbilityPackageCandidates
- AdvancedClassCandidates
- RoleActionId
- TraitIds
- InnateTraits
```

### 17.3 角色状态草案

```text
CharacterState
- Level / Rank
- UnlockedAbilityIds
- SelectedBasicAbilityId
- AdvancedClassId
- RoleActionId
- TraitIds
- AdjutantId
- HasActed
- Statuses
```

### 17.4 行动接口草案

```text
PreviewRoleAction(actorId, target)
ExecuteRoleAction(actorId, target)
ResolveTraitTriggers(triggerContext)
CollectRuleModifiers(ruleContext)
```

Role Action 和 Attack 一样，需要：

- 后端合法性校验
- preview
- AP 扣除
- HasActed 标记
- 日志
- BP 结算
- 动画事件
- 在线同步

---

## 18. 风险与防线

### 18.1 风险：行动太多

防线：

- 每张卡最多 1 个 Role Action。
- 普通兵 Role Action 一句话。
- 高级 class 修改已有行动，不加新按钮。

### 18.2 风险：防御/治疗拖局

防线：

- 治疗和加盾必须受 AP、行动次数、BP、上限或持续时间限制。
- 高效治疗最好需要目标已受伤。
- 加盾不能无限叠，或会被破盾奖励反制。

### 18.3 风险：AP 操作太强

防线：

- AP 增加必须有延期代价。
- AP 增加不能同时带大量治疗/伤害。
- 预测必须显示下回合代价。

### 18.4 风险：BP 刷分

防线：

- 有效行动才给 BP。
- 自动光环默认不给 BP。
- 单回合获取上限 3 坚决保留。

### 18.5 风险：代码层继续把所有东西叫 Skill

防线：

- 新增文档术语。
- 新增 ability/action 数据结构。
- 当前 `Skill` 作为兼容层保留，逐步迁移。

---

## 19. 最小可测试方案

如果只做最小实验，我建议：

1. 不改全部 8 名英雄。
2. 先做 Role Action pipeline。
3. 只接两个角色：
   - Knight：点击 Role Action 后选择共享盾，盾 +2，消耗 2 AP，骑士行动结束。
   - Princess：点击 Role Action 后选择友方，治疗 2，消耗 1 AP，公主行动结束。
4. BP 规则新增：
   - 有效 Role Action +1 BP。
5. 打 5 局观察：
   - 玩家是否自然使用非攻击行动。
   - 是否仍然只是攻击最优。
   - 防御/治疗是否拖局。
   - 点击 Role Action 后选择目标是否直觉。

如果这个试点成立，再接德鲁伊净化/驱散和普通兵。

---

## 20. 方向性总结

这次讨论的核心不是“技能名字怎么改”，而是要确认 Tiny Pixel Fights 的玩家动词是否足够支撑“角色养成”。

当前答案是：还不够。

BP 和奖励窗口已经给了成长系统入口，但角色行动层仍然太窄。现在如果直接上英雄升级和普通兵，成长内容会被迫围绕攻击数值和被动效果转。为了避免这个方向，下一步应先建立：

```text
Attack + Role Action + Trait
```

这不是给每张卡三个按钮，而是把角色能力拆清：

- Attack 是所有人的基础。
- Role Action 是玩家主动表达职业身份的唯一额外行动。
- Trait 是角色个性、攻击触发、光环和进阶 class 的规则支撑。

升级二选一应改理解为 `Ability Package`，主要用于解锁或改写 Role Action；进阶 class 不应无限增加按钮，而应让已有 Role Action 和 Trait 发生质变；副官也不应加新按钮，而应作为 modifier 附着到英雄已有行动上。

如果这条路线成立，游戏的核心选择会从：

> 谁打谁最赚？

变成：

> 这一回合我让谁攻击、谁守护、谁治疗、谁净化、谁蓄力、谁指挥，以及我是否用 BP 买成长？

这才更接近战术 JRPG 卡牌游戏，也更接近“我在培养这些角色”的感觉。
