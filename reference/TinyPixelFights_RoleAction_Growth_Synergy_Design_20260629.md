# Tiny Pixel Fights - Role Action / Trait / Growth Synergy Design

日期：2026-06-29  
状态：Phase 3A refactoring 进行中后的实装前设计参考  
关联文档：

- `GDD.md`
- `reference/TinyPixelFights_Ability_Taxonomy_Phase3A.md`
- `reference/TinyPixelFights_Action_Ability_Brainwriting_20260628.md`
- `reference/TinyPixelFights_Build_Synergy_Brainwriting_20260628.md`

本文目标不是继续扩大狂想，而是把 8 名英雄、4 名普通兵、Role Action、Trait、升级阶段和シナジー効果整理成后续可以落地的数据设计参考。

优先级说明：旧 brainwriting 中“每张卡最多 1 个 Role Action”的保守假设已经被 Phase 3A 覆盖。本文采用当前新口径：英雄最多 2 个 Role Action，普通兵最多 1 个，副官不增加按钮。

---

## 1. 当前已确定前提

Phase 3A 已经确定的架构边界：

```text
AbilityKind 只有两类：
- RoleAction：玩家选中角色后，在左侧 HUD 第三区点击按钮主动发动。
- Trait：不需要玩家直接点击的特性，包括攻击触发、常驻、反应、光环、modifier。

Attack 不是 Ability。Attack 是所有单位默认拥有的基础行动。
```

Role Action 上限：

| 卡牌类型 | Role Action 上限 | 解锁阶段 |
|---|---:|---|
| Hero | 2 | 第一次英雄升级解锁 Slot 1；第三次升级转职后解锁 Slot 2 |
| Soldier | 1 | 初始没有；第二次普通兵升级进阶后解锁 |
| Adjutant | 0 | 不增加按钮，只修改英雄 Attack / RoleAction / Trait |

英雄成长阶段：

| 阶段 | 内容 | 设计目的 |
|---|---|---|
| Lv0 白卡 | Attack + 初始 Trait | 保留角色个性，但不要一开始给太多按钮 |
| Lv1 第一次升级 | 从 2 个 Basic Role Action 中选择 1 个 | 决定本局基础打法 |
| Lv2 第二次升级 | stats + Trait / 已选 Basic Role Action 强化 | 让成长可见，但不增加按钮 |
| Lv3 第三次升级 | 选择 2 个 Advanced Class 之一，换立绘，解锁 Advanced Role Action | 形成终局职业身份和 build 方向 |

升级奖励必须是玩家可感知的正向收益。数值平衡削弱、触发限制收紧、上限下调等调整不能伪装成“强化”放进升级奖励里；如果某个基础能力过强，应在基础版本或全局平衡表中调整，而不是让玩家花 BP 买负体验。

普通兵成长阶段：

| 阶段 | 内容 | 设计目的 |
|---|---|---|
| Rank0 | Attack + 初始 Trait，无 Role Action | 开局认知负担低，但仍有队伍协同 |
| Rank1 | stats + 初始 Trait 强化 | 让普通兵开始像队伍零件 |
| Rank2 | 固定进阶 Class，解锁唯一 Role Action | 让普通兵从零件变成 build 支柱或副官素材 |

---

## 2. 设计原则

### 2.1 シナジー不靠写死角色名

后端应尽量避免：

```text
if hero == princess && soldier == cleric
```

更好的方向是使用通用 tags / events：

```text
HealingDone
CleanseSucceeded
ShieldGained
ShieldBroken
MarkApplied
EnemyDebuffed
MagicDamageDealt
SacrificePaid
RoleActionUsed
SoldierActed
RewardSkipped
```

这样 Cleric 和 Princess 的协同不是因为写死“公主 + 牧师”，而是因为：

```text
Princess 的 Role Action 带 Heal tag。
Cleric 的 Trait 响应 Heal tag。
High Priestess 的 Advanced Role Action 也带 Heal / Bless tag。
```

### 2.2 初期普通兵靠 Trait 产生配合

普通兵初期没有 Role Action，但不能只是数值填充。每个普通兵 Rank0 必须满足：

- 自己的基础 stats 有明确功能。
- 初始 Trait 能和至少 2 个英雄方向明显配合。
- Trait 描述短，触发条件可见。
- 不会让玩家开局必须读长规则。

### 2.3 Role Action 必须改变玩家动词

好的 Role Action 应让玩家问：

```text
这一回合我应该攻击，还是治疗、守护、标记、蓄力、指挥、献祭、净化？
```

不好的 Role Action：

- 只是攻击 +1。
- 没有代价的免费资源。
- 自动变成每回合固定最优。
- 效果难以 preview。

### 2.4 数值保守，组合有味道

当前 AP 上限 5，BP 上限 10，单回合 BP 获取上限 3。  
因此第一版 Role Action 尽量控制在：

- AP cost 可以是 0 / 1 / 2；常见值仍是 1 或 2 AP。
- 0 AP Role Action 不等于无约束免费行动，必须至少有一个限制：消耗本角色行动、每回合/每局次数、需要特定状态、产生 debt、消耗 HP / shield / status stack。
- 治疗：1-2 点。
- 直接伤害：1-3 点。
- 防御/减伤：1-2 点。
- BP：当前不作为 Role Action 支付成本，只作为奖励购买资源，以及少数有效行动的结果奖励；不要直接大量生成。
- Trait 触发：优先每回合一次。

---

## 3. 通用标签与状态建议

### 3.1 Ability Tags

| Tag | 作用 |
|---|---|
| `heal` | 治疗或恢复 HP |
| `cleanse` | 移除己方 debuff |
| `dispel` | 移除敌方 buff |
| `shield` | 增加或修改共享盾 |
| `guard` | 保护/代伤/改写目标 |
| `mark` | 标记敌人，引导集火 |
| `burn` | 炎上/持续魔法压力 |
| `weakness` | 降低攻击、防御或 cost |
| `charge` | 蓄力，下回合或下一次行动强化 |
| `command` | AP / 行动 tempo / 指挥 |
| `sacrifice` | 失去 HP / shield / status stack 等战斗资源换收益；当前不支付 BP |
| `fate` | 概率、重掷、奖励操控 |
| `magic` | 魔法伤害或魔法 modifier |
| `soldier` | 普通兵相关增益 |

### 3.2 Role Action 表格写法约定

后续所有 Role Action 表格必须把 UI 输入方式和目标类型拆开写。

| Field | Value | Meaning |
|---|---|---|
| `Input` | `Click` | 选中角色后点击 Role Action 按钮立刻发动，不需要拖动目标。`Target` 只表示隐含作用对象，例如 `SelfCard`、`OwnBoard`、`ActionPointPanel`、`None` |
| `Input` | `TargetSelect` | 选中角色后点击 Role Action 按钮进入目标选择。玩家可以直接点击合法目标，也可以从按钮拖出抛物线到合法目标。`Target` 必须列出可选目标类型 |
| `Target` | `SelfCard` | 自身。若 `Input=Click`，表示隐含目标，不需要拖自己 |
| `Target` | `AllyCard` / `EnemyCard` | 友方/敌方卡牌，需要 `TargetSelect` |
| `Target` | `OwnShield` / `EnemyShield` | 我方/敌方共享盾区域，通常需要 `TargetSelect` |
| `Target` | `ActionPointPanel` | AP 区域。当前更推荐 `Click` 隐含作用于我方 AP，只有特别强调拖拽仪式感时才用 `TargetSelect` |
| `Target` | `StatusIcon` | 状态图标。第一版可延后，先通过选择卡牌后自动选择可净化/可驱散状态 |
| `Target` | `None` | 无目标的全局或自身范围效果 |

后端 `RoleActionActivationMode.Immediate` 可以对应 `Input=Click`，`RoleActionActivationMode.Targeted` 可以对应 `Input=TargetSelect`。本文后续用 `Input` 写法，避免把“怎么发动”和“作用到谁”混在 `Target` 里。`TargetSelect` 的交互实现可以同时支持点击目标和拖拽抛物线，两者提交同一种 action request。

### 3.3 常用 Status

| Status | 建议效果 | 设计用途 |
|---|---|---|
| `blessing` | 下一次受到伤害 -1，触发后移除 | 神圣防线 |
| `foresight` | 下一次受到指定类型伤害 -1 | 占卜/预见 |
| `marked` | 下一次我方攻击该目标 +1，触发后移除 | 集火 |
| `hunted` | 普通兵攻击该目标 +1，持续到回合结束 | 猎群 |
| `burning` | 回合开始受到 1 魔法伤害，或下次魔法伤害 +1 | 魔法压力 |
| `weakness` | 下一次攻击 -2，触发后移除 | 控制 |
| `charged` | 下一次魔法伤害 +2，触发后移除 | 蓄力爆发 |
| `guarded` | 下一次主动伤害被保护/减免 | 骑士保护 |
| `commanded` | 下一次行动获得指定 bonus | 指挥链 |
| `debt` | 下回合 AP -1 或 HP -1 | AP 还债 |
| `rage` | 下次攻击 +1，可叠 2 层 | 狂战 |
| `prey` | 受到 0 伤害时触发绝对伤害 | 反防御陷阱 |
| `pact` | 因牺牲获得的攻击/BP/绝对伤害前置 | 黑暗代价 |
| `nature-regen` | 回合开始治疗 1，一次或两次后消失 | 德鲁伊持续回复 |

这些 status 不一定都要第一版实现。建议第一批优先：

```text
blessing / foresight / marked / burning / weakness / charged / guarded
```

---

## 4. 总览数据表

### 4.1 英雄基础与成长总览

当前基础 stats 来自 `Domain/CharacterDefinition.cs`。Lv2 和 Lv3 stats 是设计建议，后续需要拉表和 playtest。

| Hero | Lv0 stats | Lv0 Trait | Lv1 Basic Role Action 二选一 | Lv2 成长建议 | Lv3 Advanced Class 二选一 | 主要 build 轴 |
|---|---|---|---|---|---|---|
| Princess | C1 / A1 / HP12 / 物理 / P-1 M1 | `saints-prayer` 弱治疗光环 | `saintly-prayer` / `royal-command` | HP+2；治疗或指挥强化 | `High Priestess` / `Dark Lord Princess` | heal, command, sacrifice |
| Oracle | C1 / A1 / HP14 / 魔法 / P-1 M2 | `stargazers-aegis` 预见光环 | `star-reading` / `fate-mark` | HP+2；预见或标记强化 | `Astral Oracle` / `Fate Dealer` | foresight, mark, reward |
| Peasant | C1 / A2 / HP16 / 物理 / P0 M0 | `spring-harvest` 播种 | `supply-basket` / `field-work` | HP+2；后勤或收获强化 | `Harvest Guard` / `Harvest Saint` | support, shield, BP |
| Barbarian | C2 / A4 / HP18 / 物理 / P-1 M0 | `aftershock-axe` 余波 | `war-cry` / `challenge` | HP+2；风险收益强化 | `Radiant Berserker` / `Dragon Raider` | burst, break-shield |
| Monster | C3 / A3 / HP22 / 物理 / P1 M-1 | `predatory-instinct` 反防御 | `predatory-gaze` / `dark-pact` | HP+2；prey 或 pact 强化 | `Mirror Fiend` / `Abyssal Queen` | absolute, sacrifice |
| Knight | C3 / A3 / HP24 / 物理 / P1 M-1 | `interposing-shield` 守护 | `guard-oath` / `raise-bulwark` | HP+2；守护或盾强化 | `Dread Cavalier` / `Holy Paladin` | guard, shield, charge |
| Mage | C2 / A4 / HP16 / 魔法 / P0 M1 | `searing-mark` 炎上 | `arcane-channel` / `searing-brand` | HP+1；蓄力或燃烧强化 | `Stellar Archmage` / `Arcane Archivist` | magic, burn, copy |
| Druid | C1 / A1 / HP16 / 魔法 / P0 M1 | `weakening-spores` 衰弱/驱散 | `cleansing-herbs` / `weakening-spores-action` | HP+2；净化或胞子强化 | `Grove Keeper` / `Wildspeaker` | cleanse, dispel, hunt |

### 4.2 普通兵基础与成长总览

| Soldier | Rank0 stats | Rank0 Trait | Rank1 | Rank2 Class | Rank2 Role Action | 至少两条明显英雄协同 |
|---|---|---|---|---|---|---|
| Cleric | C1 / A1 / HP12 / 魔法 / P-1 M1 | `field-medic` | HP+2；Trait 每回合次数或条件强化 | `Saint Cleric` | `mend` | Princess 治疗轴；Druid 净化轴；Dark Lord / Monster 代价轴 |
| Shieldmaiden | C2 / A1 / HP18 / 物理 / P1 M0 | `shield-drill` | HP+2，PDef+1；盾协同强化 | `Aegis Shieldmaiden` | `aegis-formation` | Knight 盾轴；Peasant 后勤盾；Mage 蓄力保护 |
| Duelist | C1 / A3 / HP12 / 物理 / P0 M-1 | `duel-sense` | HP+1；对 mark/debuff 目标更强 | `Crimson Duelist` | `crimson-lunge` | Oracle 标记；Druid 猎物；Monster prey；Mage burn |
| Arcanist | C2 / A3 / HP12 / 魔法 / P-1 M1 | `arcane-resonance` | HP+2，MDef+1；魔法共鸣强化 | `Astral Arcanist` | `astral-focus` | Mage 蓄力/燃烧；Oracle 星术；Druid 胞子魔防；Princess AP 指挥 |

---

## 5. 英雄详细设计

### 5.1 Princess

核心定位：治疗、鼓舞、AP tempo、神圣或黑暗统治。  
风险：她 cost 低，如果治疗、AP、BP 同时强，会成为必选。

#### Lv0 Trait

| Field | Value |
|---|---|
| ID | `saints-prayer` |
| Trigger | `TurnStart` |
| Scope | `Team` |
| Effect | `Heal` |
| 建议调整 | 保持当前弱治疗光环，但未来若治疗系 Role Action 接入，光环治疗不应给 BP |

#### Lv1 Basic Role Action A

| Field | Value |
|---|---|
| ID | `saintly-prayer` |
| Input | `TargetSelect` |
| Target | `AllyCard`, `StatusIcon` |
| Cost | 1 AP |
| Tags | `heal`, `cleanse`, `holy` |
| Effect | 治疗目标 2；若目标有 debuff，净化 1 个可净化 debuff |
| BP | 有效治疗或净化成功时 +1 BP，受回合上限 |

#### Lv1 Basic Role Action B

| Field | Value |
|---|---|
| ID | `royal-command` |
| Input | `Click` |
| Target | `ActionPointPanel` |
| Cost | 1 AP |
| Tags | `command`, `action-point` |
| Effect | 本回合 +1 AP；下个己方回合开始获得 `debt`：AP -1 |
| BP | 默认不给 BP，因为它改变 AP 经济 |

#### Lv2

- Stats：HP +2。
- Trait 强化：`saints-prayer` 的回合开始治疗若实际生效，目标获得 `blessing`。每回合一次，只在确实治疗到 HP 时触发，避免空触发刷收益。
- 若选择 `saintly-prayer`：目标 HP 低于一半时，治疗 +1；若本次净化成功，额外给目标 `blessing`。
- 若选择 `royal-command`：`debt` 保留，但目标本回合第一次造成 HP 伤害时获得 1 BP，受单回合 BP 上限限制。

#### Lv3A - High Priestess

立绘与人设：白金神官，救赎、祝福、净化。  
Class stats：HP +2，MDef +1，上限建议 MDef 2。

Advanced Role Action:

| Field | Value |
|---|---|
| ID | `sanctuary-hymn` |
| Slot | 2 |
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 2 AP |
| Tags | `heal`, `blessing`, `shield`, `holy` |
| Effect | 目标获得 `blessing`；若目标本回合已被治疗，额外给我方共享盾 +1 |
| Link with Slot 1 | 选过 `saintly-prayer` 时，`sanctuary-hymn` 对同一目标治疗 1；选过 `royal-command` 时，本回合第一次使用 `sanctuary-hymn` 后最低 HP 友军治疗 1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `divine-order` | 每回合第一次有效治疗或净化后，若目标是 Hero，目标获得 `blessing`；若目标是 Soldier，目标获得下次攻击 +1。每回合一次 |

シナジー：

- Cleric 初始 `field-medic` 会放大 `saintly-prayer` 与 `sanctuary-hymn`。
- Shieldmaiden 的盾增益会让 `blessing + shield` 防线更厚。
- Holy Paladin 共享 `guard / shield / blessing` 标签，形成圣职铁壁。
- Grove Keeper 的净化可以触发治疗链，但需要每回合次数限制。

#### Lv3B - Dark Lord Princess

立绘与人设：黑红女王，命令、代价、爆发。  
Class stats：Attack +1，HP +2，PDef 从 -1 提到 0。

Advanced Role Action:

| Field | Value |
|---|---|
| ID | `blood-edict` |
| Slot | 2 |
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 1 AP |
| Tags | `command`, `sacrifice`, `attack-modifier` |
| Effect | 目标失去 1 HP，获得 `commanded`：下一次攻击 +2；若该攻击没有造成 HP 伤害，目标回合结束再失去 1 HP |
| Link with Slot 1 | 选过 `royal-command` 时，`blood-edict` 目标下一次攻击若击败敌人，获得 1 BP；选过 `saintly-prayer` 时，`blood-edict` 目标先治疗 1 再支付代价 |

Advanced Trait:

| ID | Effect |
|---|---|
| `tyrants-debt` | 每回合第一次友方因 `sacrifice` 失去 HP 时，我方获得 1 BP 或公主获得 `rage`。二选一需要在实现前决定，不要两者都有 |

シナジー：

- Duelist 初始 `duel-sense` 能吃 `commanded` 后的斩杀窗口。
- Cleric 初始 `field-medic` 能补黑暗代价。
- Abyssal Queen 的 `pact / sacrifice` 与 `blood-edict` 共用事件。
- Dread Cavalier 可以把 AP 爆发转成破盾突击。

---

### 5.2 Oracle

核心定位：预见、标记、命运、奖励操控。  
风险：概率和奖励控制太强会让玩家觉得不是自己打赢。

#### Lv0 Trait

| Field | Value |
|---|---|
| ID | `stargazers-aegis` |
| Trigger | `Continuous` |
| Scope | `Team` |
| Effect | `DamageModifier` |
| 建议调整 | 保持当前预见光环；Role Action 接入后可把概率减伤逐步迁移为可控 `foresight` |

#### Lv1 Basic Role Action A

| ID | `star-reading` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 1 AP |
| Tags | `foresight`, `defense`, `fate` |
| Effect | 目标获得 `foresight`：下一次受到伤害 -1 |

#### Lv1 Basic Role Action B

| ID | `fate-mark` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `mark`, `fate` |
| Effect | 目标获得 `marked`：下一次我方攻击该目标时最低伤害 +1，触发后移除 |

#### Lv2

- Stats：HP +2。
- `star-reading` 强化：若目标是 Soldier，额外给 MDef +1 一回合。
- `fate-mark` 强化：被标记目标受到魔法伤害时，额外降低 MDef 1 到回合结束。
- Trait 强化：`stargazers-aegis` 第一次成功减伤后，受保护目标获得 `foresight`。每回合一次。

#### Lv3A - Astral Oracle

Class stats：MDef 保持 2，HP +2。  
高级身份：稳定星术、确定性保护。

Advanced Role Action:

| ID | `astral-alignment` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` 或 `AllyCard` |
| Cost | 2 AP |
| Tags | `foresight`, `mark`, `magic` |
| Effect | 选择敌方：施加 `astral-seal`，下一次魔法伤害 +1；选择友方：赋予 `foresight` 且 MDef +1 一回合 |
| Link | 选过 `star-reading` 时友方模式更强；选过 `fate-mark` 时敌方模式更强 |

Advanced Trait:

| ID | Effect |
|---|---|
| `fixed-star` | 每回合第一次 `foresight` 被消耗时，若伤害类型是魔法，额外减 1 |

シナジー：

- Arcanist 初始 `arcane-resonance` 响应 `magic / mark`。
- Mage 的 `starfall` 吃 `astral-seal`。
- Shieldmaiden 保护脆皮星术队。
- Holy Paladin 与 Astral Oracle 形成确定性防线。

#### Lv3B - Fate Dealer

Class stats：HP +2，Attack 不增加。  
高级身份：赌徒、奖励操控、风险回报。

Advanced Role Action:

| ID | `twist-the-odds` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `fate`, `mark`, `reward` |
| Effect | 目标获得 `gamble-mark`：下一次我方攻击该目标时，50% 伤害 +2，50% 无加成；不论成败，若目标本回合被击败，获得 1 BP |
| Link | 选过 `fate-mark` 时 `gamble-mark` 同时视为 `marked`；选过 `star-reading` 时失败结果改为我方最低 HP 单位获得 `foresight` |

Advanced Trait:

| ID | Effect |
|---|---|
| `loaded-dice` | 奖励窗口每次出现时，第一次 reset 可以锁定一个奖励不被刷新。Phase 3B 可先不做，等 Reward 扩展 |

シナジー：

- Duelist 初始 `duel-sense` 可以吃 `marked / gamble-mark`。
- Peasant / Harvest Saint 支撑 BP 经济。
- Arcane Archivist 以后可以复制 fate 类 Role Action，但必须限制次数。

---

### 5.3 Peasant

核心定位：低 cost、后勤、收获、普通兵队长。  
风险：低 cost 单位若同时给 AP、BP、治疗、攻击，会变成万金油。

#### Lv0 Trait

| ID | `spring-harvest` |
|---|---|
| Trigger | `OnAttackDeclared` |
| Scope | `Self` |
| Effect | `Status` |
| 建议 | 保留播种/收获，但与后续 `field-work` 共用 `harvest` tag |

#### Lv1 Basic Role Action A

| ID | `supply-basket` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 1 AP |
| Tags | `heal`, `soldier`, `support` |
| Effect | 治疗 1；若目标满血，目标下次攻击 +1 |

#### Lv1 Basic Role Action B

| ID | `field-work` |
|---|---|
| Input | `Click` |
| Target | `SelfCard` |
| Cost | 1 AP |
| Tags | `harvest`, `charge`, `support` |
| Effect | 获得 `harvest-pending`：下个己方回合第一次行动 cost -1，最低为 1，或攻击 +1。两种效果二选一需要实现时固定 |

#### Lv2

- Stats：HP +2。
- `supply-basket` 强化：目标是 Soldier 时，额外 HP +1。
- `field-work` 强化：如果下回合成功触发 harvest，获得 1 BP，每回合一次。
- Trait 强化：`spring-harvest` 可与 Soldier tag 互动，普通兵第一次攻击 harvest 目标时 +1。

#### Lv3A - Harvest Guard

Class stats：PDef +1，HP +2。  
高级身份：民兵队长、修盾、普通兵防线。

Advanced Role Action:

| ID | `militia-wall` |
|---|---|
| Input | `TargetSelect` |
| Target | `OwnShield` |
| Cost | 2 AP |
| Tags | `shield`, `soldier`, `guard` |
| Effect | 我方共享盾 +2；若场上有 Soldier，额外给一个最低 HP Soldier `blessing` |
| Link | 选过 `supply-basket` 时最低 HP Soldier 治疗 1；选过 `field-work` 时下回合第一次 DeployShield cost -1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `village-line` | 每回合第一次 Soldier 受到伤害时，若我方有共享盾，伤害 -1 |

シナジー：

- Shieldmaiden 初始 `shield-drill` 与 `militia-wall` 直接叠盾。
- Holy Paladin 形成盾墙。
- Wildspeaker 的普通兵猎群需要 Harvest Guard 保命。

#### Lv3B - Harvest Saint

Class stats：MDef +1，HP +2。  
高级身份：丰饶圣女、BP 后勤、治疗补给。

Advanced Role Action:

| ID | `harvest-feast` |
|---|---|
| Input | `Click` |
| Target | `None` |
| Cost | 2 AP |
| Tags | `heal`, `battle-point`, `support` |
| Effect | 全体友方治疗 1；如果本次奖励窗口曾 skip，额外获得 1 BP。每个奖励窗口限一次 |
| Link | 选过 `supply-basket` 时最低 HP 友方额外治疗 1；选过 `field-work` 时 `harvest-feast` 后获得 `harvest-pending` |

Advanced Trait:

| ID | Effect |
|---|---|
| `fruitful-mercy` | 每次跳过奖励时，最低 HP 友方治疗 1。每个奖励窗口一次 |

シナジー：

- Fate Dealer 的 reward 操控与 Harvest Saint 的 skip 收益形成经济流派。
- Dark Lord Princess / Abyssal Queen 的 sacrifice 代价可以被 Harvest Saint 修复。
- Cleric 与 Harvest Saint 容易拖局，需要禁疗/爆发反制。

---

### 5.4 Barbarian

核心定位：高攻、风险、破盾、挑衅。  
风险：基础 A4 已经很高，任何增伤都必须带代价或阈值。

#### Lv0 Trait

| ID | `aftershock-axe` |
|---|---|
| Trigger | `OnAttackResolved` |
| Scope | `EnemyTeam` |
| Effect | `Damage` |

#### Lv1 Basic Role Action A

| ID | `war-cry` |
|---|---|
| Input | `Click` |
| Target | `SelfCard` |
| Cost | 1 AP |
| Tags | `charge`, `attack-modifier`, `sacrifice` |
| Effect | 获得 `rage`：下次攻击 +1；直到下个己方回合 PDef/MDef -1 |

#### Lv1 Basic Role Action B

| ID | `challenge` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `guard`, `weakness`, `taunt` |
| Effect | 目标获得 `challenged`：下回合攻击非 Barbarian 目标时伤害 -1；若攻击 Barbarian，无惩罚 |

#### Lv2

- Stats：HP +2。
- `war-cry` 强化：若下一次攻击击破盾，获得 1 BP。
- `challenge` 强化：被 challenge 的目标如果攻击 Barbarian，Barbarian 获得 `rage`。
- Trait 强化：`aftershock-axe` 对 shield absorbed 也计入触发阈值；若本次余波击破共享盾，Barbarian 获得 `rage`。

#### Lv3A - Radiant Berserker

Class stats：HP +2，PDef 从 -1 提到 0。  
高级身份：荣耀重击、正面破阵。

Advanced Role Action:

| ID | `glory-cleave` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `damage`, `break-shield`, `rage` |
| Effect | 对目标发动一次特殊物理攻击，基础伤害为 2；若目标 shield absorbed 大于 0，额外对目标 HP 造成 1 伤害 |
| Link | 选过 `war-cry` 时消耗 `rage` 可让 `glory-cleave` +1；选过 `challenge` 时攻击 challenged 目标 cost -1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `radiant-fury` | 每回合第一次击破盾或造成 3+ HP 伤害时，获得 `glory`，下次攻击 +1，最多 1 层 |

シナジー：

- Oracle / Astral Oracle 的 mark 保证重击阈值。
- Shieldmaiden 提供盾保护自降防阶段。
- Crimson Duelist 收割被斩残目标。

#### Lv3B - Dragon Raider

Class stats：Attack +1，MDef 保持 0，HP +1。  
高级身份：龙骑、破盾、边缘猎杀。

Advanced Role Action:

| ID | `dragon-raid` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyShield` 或 `EnemyCard` |
| Cost | 2 AP |
| Tags | `break-shield`, `weakness`, `damage` |
| Effect | 若目标是 EnemyShield，直接对盾造成 2 点破盾；若目标是 EnemyCard，施加 `shaken`：下次攻击 -1 |
| Link | 选过 `war-cry` 时破盾后自身获得 `rage`；选过 `challenge` 时 `shaken` 持续到目标下次行动结束 |

Advanced Trait:

| ID | Effect |
|---|---|
| `raider-instinct` | 攻击带 `marked / hunted / shaken` 的目标时，若目标 HP 低于一半，伤害 +1 |

シナジー：

- Wildspeaker 的 hunted 让 Dragon Raider 有明确猎物。
- Dread Cavalier 共同形成破盾突击。
- Aegis Shieldmaiden 保护 Dragon Raider 的高风险进攻。

---

### 5.5 Monster

核心定位：反防御、绝对伤害、契约、黑暗代价。  
风险：绝对伤害绕过太多系统，必须条件明确且次数少。

#### Lv0 Trait

| ID | `predatory-instinct` |
|---|---|
| Trigger | `OnAttackResolved / OnCharacterDefeated` |
| Scope | `Enemy / Self` |
| Effect | `Damage / DamageModifier` |

#### Lv1 Basic Role Action A

| ID | `predatory-gaze` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `mark`, `absolute`, `prey` |
| Effect | 目标获得 `prey`：下次受到 0 点 HP 伤害时，追加 1 绝对伤害，触发后移除 |

#### Lv1 Basic Role Action B

| ID | `dark-pact` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` 或 `SelfCard` |
| Cost | 1 AP |
| Tags | `sacrifice`, `attack-modifier`, `battle-point` |
| Effect | 目标失去 1 HP，获得 `pact`：下次攻击 +1；若目标 HP 低于一半，改为获得 1 BP |

#### Lv2

- Stats：HP +2。
- `predatory-gaze` 强化：prey 目标若被 shield 完全吸收伤害，也视为 0 HP 伤害并触发。
- `dark-pact` 强化：目标是 Soldier 时，失去 HP 后获得 `blessing`，避免普通兵直接送死。
- Trait 强化：`predatory-instinct` 每局第一次触发暴走时，Monster 额外治疗 2 并获得 `rage`。

#### Lv3A - Mirror Fiend

Class stats：MDef 从 -1 提到 0，HP +2。  
高级身份：镜像、反射、复制。

Advanced Role Action:

| ID | `mirror-snare` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `mirror`, `mark`, `dispel` |
| Effect | 目标获得 `mirror-snare`：目标下次获得 buff / heal 时，我方 Monster 复制一个弱化版效果；若无法复制，则对目标造成 1 魔法伤害 |
| Link | 选过 `predatory-gaze` 时 `mirror-snare` 同时视为 `prey`；选过 `dark-pact` 时复制成功后治疗契约目标 1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `broken-reflection` | 每回合第一次敌方获得可驱散 buff 时，Mirror Fiend 获得 1 层 `reflection`；满 2 层后下一次被攻击时反射 1 伤害 |

シナジー：

- Druid 的 dispel/cleanse 让镜像队能安全处理状态。
- Fate Dealer 增加对手决策压力。
- Cleric 能修复 pact / reflection 后的损耗。

#### Lv3B - Abyssal Queen

Class stats：Attack +1，HP +2，MDef 仍 -1。  
高级身份：深渊、绝对伤害、献祭。

Advanced Role Action:

| ID | `abyssal-command` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `absolute`, `sacrifice`, `mark` |
| Effect | 让最低 HP 友方失去 1 HP，对带 `prey / marked / hunted` 的目标造成 1 绝对伤害；若目标没有标记，则先施加 `prey` |
| Link | 选过 `predatory-gaze` 时绝对伤害目标限制更宽；选过 `dark-pact` 时支付 HP 代价后 pact 目标获得 `rage` |

Advanced Trait:

| ID | Effect |
|---|---|
| `abyssal-rage` | 任意 Princess 阵亡后，Abyssal Queen 攻击 +2，不可驱散。每局一次 |

シナジー：

- Dark Lord Princess 与 Abyssal Queen 共用 sacrifice 事件。
- Saint Cleric / Harvest Saint 修复失血代价。
- Duelist 收割被绝对伤害压低的目标。

---

### 5.6 Knight

核心定位：守护、盾、代伤、骑兵。  
风险：防御 build 拖局，黑骑 build 抢 Barbarian 的输出定位。

#### Lv0 Trait

| ID | `interposing-shield` |
|---|---|
| Trigger | `OnDamaged` |
| Scope | `Ally` |
| Effect | `Shield / DamageModifier` |

#### Lv1 Basic Role Action A

| ID | `guard-oath` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 2 AP |
| Tags | `guard`, `defense` |
| Effect | 目标获得 `guarded`：下一次主动攻击伤害 -2，骑士承受 1 伤害 |

#### Lv1 Basic Role Action B

| ID | `raise-bulwark` |
|---|---|
| Input | `TargetSelect` |
| Target | `OwnShield` |
| Cost | 2 AP |
| Tags | `shield`, `guard` |
| Effect | 我方共享盾 +2 |

#### Lv2

- Stats：HP +2。
- `guard-oath` 强化：可保护魔法伤害；若保护的是魔法伤害，Knight 获得 MDef +1 到下个己方回合。
- `raise-bulwark` 强化：若本回合没有攻击过，额外 shield +1。
- Trait 强化：`interposing-shield` 触发后，Knight 获得 `guarded`，下一次受到的主动伤害 -1。

#### Lv3A - Dread Cavalier

Class stats：Attack +1，HP +1，PDef +1，MDef 仍 -1。  
高级身份：黑骑突击，盾转进攻。

Advanced Role Action:

| ID | `dark-charge` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `damage`, `shield`, `charge` |
| Effect | 若我方共享盾 >0，消耗 1 盾，对目标造成 2 物理伤害并施加 `shaken`；无盾时只能造成 1 物理伤害 |
| Link | 选过 `raise-bulwark` 时本回合先加盾再冲锋有明确连动；选过 `guard-oath` 时守护目标被攻击后 `dark-charge` cost -1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `vengeful-rider` | 每回合第一次我方共享盾被击破或 guarded 目标受伤后，Dread Cavalier 获得 `vengeance`，下一次攻击 +1 |

シナジー：

- Shieldmaiden 初始 `shield-drill` 提供冲锋燃料。
- Dark Lord Princess 给 AP 爆发。
- Crimson Duelist 接在 `shaken` 后收割。

#### Lv3B - Holy Paladin

Class stats：HP +2，PDef +1，MDef 从 -1 提到 0。  
高级身份：圣盾、防线、守护。

Advanced Role Action:

| ID | `sanctuary` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` 或 `OwnShield` |
| Cost | 2 AP |
| Tags | `shield`, `guard`, `blessing`, `holy` |
| Effect | 选择友方：目标获得 `blessing` 和 `guarded`；选择共享盾：盾 +2，最低 HP 友方获得 `blessing` |
| Link | 选过 `guard-oath` 时友方模式额外治疗 1；选过 `raise-bulwark` 时盾模式额外 shield +1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `holy-aegis` | 每回合第一次共享盾吸收伤害后，最低 HP 友方治疗 1 |

シナジー：

- Aegis Shieldmaiden 是最直接搭档。
- High Priestess 形成圣盾防线。
- Radiant Berserker 在盾保护下获得安全输出窗口。

---

### 5.7 Mage

核心定位：魔法爆发、燃烧、蓄力、书卷复制。  
风险：A4 魔法已高，直接加伤要靠蓄力、标记或可打断窗口。

#### Lv0 Trait

| ID | `searing-mark` |
|---|---|
| Trigger | `OnAttackResolved` |
| Scope | `Enemy` |
| Effect | `Status` |

#### Lv1 Basic Role Action A

| ID | `arcane-channel` |
|---|---|
| Input | `Click` |
| Target | `SelfCard` |
| Cost | 1 AP |
| Tags | `charge`, `magic` |
| Effect | 获得 `charged`：下一次魔法伤害 +2；本回合不能再攻击 |

#### Lv1 Basic Role Action B

| ID | `searing-brand` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `burn`, `magic`, `mark` |
| Effect | 目标获得 `burning`；目标受到的下一次魔法伤害 +1 |

#### Lv2

- Stats：HP +1。
- `arcane-channel` 强化：charged 若被驱散，Mage 获得 1 BP 作为补偿，每回合一次。
- `searing-brand` 强化：若目标已有 burning，改为降低 MDef 1 到回合结束。
- Trait 强化：`searing-mark` 成功施加 burning 后，Mage 获得 `charged-small`：下一次魔法伤害 +1。

#### Lv3A - Stellar Archmage

Class stats：MDef +1，HP +1。  
高级身份：星火爆发。

Advanced Role Action:

| ID | `starfall` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `magic`, `damage`, `charge` |
| Effect | 对目标造成 2 魔法伤害；若 Mage 有 `charged`，消耗并改为 3 魔法伤害，且若击破盾，追加 1 魔法伤害 |
| Link | 选过 `arcane-channel` 时更稳定；选过 `searing-brand` 时对 burning 目标 +1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `stellar-calculation` | 攻击或 Role Action 命中 `marked / burning / astral-seal` 目标时，预测最低伤害 +1。每回合一次 |

シナジー：

- Arcanist 初始 `arcane-resonance` 让魔法伤害链成型。
- Astral Oracle 提供 astral-seal。
- Shieldmaiden 保护蓄力回合。

#### Lv3B - Arcane Archivist

Class stats：HP +2，MDef +1，Attack 不增加。  
高级身份：书卷、记录、复制弱版行动。

Advanced Role Action:

| ID | `archive-formula` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 2 AP |
| Tags | `copy`, `support`, `magic` |
| Effect | 目标获得 `archived`：目标下一次 Role Action 结算后，额外触发一个弱版后效。Heal 后效为 heal 1，Shield 后效为 shield +1，Mark 后效为延长 1 次，Magic 后效为 +1 damage |
| Link | 选过 `arcane-channel` 时 Mage 自己获得 `charged`；选过 `searing-brand` 时 archived 的 magic 后效可施加 burning |

Advanced Trait:

| ID | Effect |
|---|---|
| `living-grimoire` | 每次购买 Role Action / Trait 相关奖励后，Arcane Archivist 获得 1 `page`。2 page 后下一次 `archive-formula` cost -1 |

シナジー：

- High Priestess 的 heal / bless 被书卷复制后形成圣堂队。
- Druid 的 cleanse / dispel 被弱复制后形成工具箱。
- Fate Dealer reward 控制与 page 经济适配，但实现复杂。

---

### 5.8 Druid

核心定位：净化、驱散、衰弱、自然回复、猎群。  
风险：状态系统必须 UI 清楚，否则玩家不知道为什么技能有效或无效。

#### Lv0 Trait

| ID | `weakening-spores` |
|---|---|
| Trigger | `OnAttackResolved` |
| Scope | `Enemy` |
| Effect | `Dispel / Status` |

#### Lv1 Basic Role Action A

| ID | `cleansing-herbs` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` 或 `StatusIcon` |
| Cost | 1 AP |
| Tags | `cleanse`, `heal`, `nature` |
| Effect | 移除 1 个可净化 debuff；如果成功，治疗 1 |

#### Lv1 Basic Role Action B

| ID | `weakening-spores-action` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` 或 `StatusIcon` |
| Cost | 1 AP |
| Tags | `dispel`, `weakness`, `nature` |
| Effect | 移除 1 个可驱散 buff，并施加 `weakness` |

#### Lv2

- Stats：HP +2。
- `cleansing-herbs` 强化：无 debuff 可净化时，改为治疗 1；若目标已满血，改为获得 `nature-regen`。
- `weakening-spores-action` 强化：若成功驱散，额外降低目标 MDef 1 到回合结束。
- Trait 强化：`weakening-spores` 成功施加 debuff 或驱散 buff 后，Druid 获得 `nature-regen`。每回合一次。

#### Lv3A - Grove Keeper

Class stats：MDef +1，HP +2。  
高级身份：森林守护、净化、持续恢复。

Advanced Role Action:

| ID | `grove-sanctuary` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` |
| Cost | 2 AP |
| Tags | `cleanse`, `heal`, `nature` |
| Effect | 净化目标 1 个 debuff，目标获得 `nature-regen`；若无 debuff，直接治疗 2 |
| Link | 选过 `cleansing-herbs` 时 `nature-regen` 多持续 1 次；选过 `weakening-spores-action` 时净化成功后对随机敌方施加 weak 1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `rooted-mercy` | 每回合第一次我方成功 cleanse 后，我方共享盾 +1 或最低 HP 友方治疗 1。实现时二选一 |

シナジー：

- Cleric 初始 `field-medic` 与 cleanse/heal 强配合。
- High Priestess 组成反状态治疗队。
- Shieldmaiden 提供盾，防止净化队被 burst 击穿。

#### Lv3B - Wildspeaker

Class stats：Attack +1，HP +1，MDef 保持 1。  
高级身份：野性猎群、标记、普通兵指挥。

Advanced Role Action:

| ID | `call-the-hunt` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 2 AP |
| Tags | `mark`, `hunted`, `soldier`, `nature` |
| Effect | 目标获得 `hunted`：本回合前两次 Soldier 对该目标造成的伤害 +1；如果目标被击败，获得 1 BP |
| Link | 选过 `weakening-spores-action` 时 hunted 目标同时获得 weakness；选过 `cleansing-herbs` 时第一次 Soldier 攻击 hunted 目标后治疗该 Soldier 1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `pack-instinct` | 每回合第一次 Soldier 攻击 marked / hunted 目标时，Druid 获得 1 BP 或该 Soldier 获得 `blessing`。建议先做 blessing，避免 BP 刷分 |

シナジー：

- Duelist 初始 `duel-sense` 明确吃 hunted。
- Arcanist 可把 weakness 转成魔法爆发。
- Dragon Raider 与 Wildspeaker 形成猎群突袭。

---

## 6. 普通兵详细设计

### 6.1 Cleric -> Saint Cleric

定位：治疗放大、净化辅助、代价 build 的安全阀。  
初始没有 Role Action，靠 Trait 配合英雄。

#### Rank0

Stats：C1 / A1 / HP12 / Magical / PDef -1 / MDef 1

Trait:

| Field | Value |
|---|---|
| ID | `field-medic` |
| Trigger | `ManualCheck / TurnStart` |
| Scope | `Team` |
| Effect | `Heal` |
| Text | 每回合第一次我方通过 Role Action 治疗或净化成功时，最低 HP 友方治疗 1 |
| Limit | 每回合一次 |

初期协同保证：

- Princess 的 `saintly-prayer` 触发额外治疗。
- Druid 的 `cleansing-herbs` 触发额外治疗。
- Dark Lord Princess / Monster sacrifice 后，后续治疗链价值更高。

#### Rank1

- HP +2。
- `field-medic` 强化：如果触发目标是 Hero，目标获得 `blessing`；若目标 HP 仍低于一半，额外治疗 1。

#### Rank2 - Saint Cleric

Stats：HP +2，MDef +1。

Role Action:

| ID | `mend` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` 或 `StatusIcon` |
| Cost | 1 AP |
| Tags | `heal`, `cleanse`, `holy`, `soldier` |
| Effect | 治疗 2；若目标有 debuff，改为净化 1 并治疗 1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `saintly-bells` | 每回合第一次有效治疗溢出时，目标获得 `blessing` |

副官方向：

- 附给英雄后：英雄 Role Action 若作用于友方，额外 heal 1；若该 Role Action 已带 `heal` tag，则改为额外 cleanse 1。

---

### 6.2 Shieldmaiden -> Aegis Shieldmaiden

定位：初期保护、盾轴齿轮、蓄力队防线。  
初始没有 Role Action，但 stats 和 Trait 足以让她成为盾 build 明确信号。

#### Rank0

Stats：C2 / A1 / HP18 / Physical / PDef 1 / MDef 0

Trait:

| Field | Value |
|---|---|
| ID | `shield-drill` |
| Trigger | `OnShieldGain / ManualCheck` |
| Scope | `Team` |
| Effect | `Shield` |
| Text | 每回合第一次我方通过行动获得共享盾时，额外 shield +1 |
| Limit | 每回合一次 |

初期协同保证：

- Knight 的 `raise-bulwark` 直接变强。
- Peasant / Harvest Guard 的修盾路线变强。
- Mage 的 `arcane-channel` 需要保护，Shieldmaiden 提供安全回合。

#### Rank1

- HP +2。
- PDef +1，达到 PDef 2。
- `shield-drill` 强化：若触发来自 Hero Role Action，最低 HP Soldier 获得 `blessing`。

#### Rank2 - Aegis Shieldmaiden

Stats：HP +2，MDef +1。

Role Action:

| ID | `aegis-formation` |
|---|---|
| Input | `TargetSelect` |
| Target | `OwnShield` |
| Cost | 2 AP |
| Tags | `shield`, `guard`, `soldier` |
| Effect | 共享盾 +2；本回合下一次魔法伤害 -1 |

Advanced Trait:

| ID | Effect |
|---|---|
| `aegis-discipline` | 共享盾吸收伤害后，Aegis Shieldmaiden 获得 MDef +1 到下回合。每回合一次 |

副官方向：

- 附给英雄后：英雄使用 Role Action 后，我方共享盾 +1。若英雄 Role Action 带 `shield / guard` tag，额外给最低 HP 友方 `blessing`。

---

### 6.3 Duelist -> Crimson Duelist

定位：低 cost 物理收割、吃标记、吃 debuff。  
初始没有 Role Action，但能让玩家看到“标记后让 Duelist 上”。

#### Rank0

Stats：C1 / A3 / HP12 / Physical / PDef 0 / MDef -1

Trait:

| Field | Value |
|---|---|
| ID | `duel-sense` |
| Trigger | `ManualCheck / OnAttackDeclared` |
| Scope | `Self` |
| Effect | `DamageModifier` |
| Text | 攻击带 `marked / hunted / prey / burning / weakness` 的敌人时，伤害 +1。每回合一次 |

初期协同保证：

- Oracle 的 `fate-mark`。
- Druid 的 `weakening-spores-action` / Wildspeaker hunted。
- Monster 的 `predatory-gaze`。
- Mage 的 `searing-brand`。

#### Rank1

- HP +1。
- `duel-sense` 强化：若目标 HP 低于一半，额外 +1；若本次攻击击败带 mark/debuff 的目标，获得 1 BP。

#### Rank2 - Crimson Duelist

Stats：Attack +1，HP +1。

Role Action:

| ID | `crimson-lunge` |
|---|---|
| Input | `TargetSelect` |
| Target | `EnemyCard` |
| Cost | 1 AP |
| Tags | `damage`, `mark`, `execute`, `soldier` |
| Effect | 对目标造成 1 物理伤害；若目标带 mark/debuff，改为 2；若击败目标，获得 1 BP |

Advanced Trait:

| ID | Effect |
|---|---|
| `red-finale` | 每回合第一次击败带 mark/debuff 的敌人时，Crimson Duelist 获得 `blessing` 或我方获得 1 BP。建议先做 blessing |

副官方向：

- 附给英雄后：英雄攻击被自己 Role Action 标记或 debuff 的目标时 +1 伤害；若击败目标，获得 1 BP，每回合一次。

---

### 6.4 Arcanist -> Astral Arcanist

定位：魔法增幅、状态放大、魔防穿透。  
初始没有 Role Action，但能让魔法和状态队早期可见。

#### Rank0

Stats：C2 / A3 / HP12 / Magical / PDef -1 / MDef 1

Trait:

| Field | Value |
|---|---|
| ID | `arcane-resonance` |
| Trigger | `ManualCheck / OnAttackResolved` |
| Scope | `Team` |
| Effect | `DamageModifier` |
| Text | 每回合第一次我方 Hero 造成魔法伤害或施加魔法状态后，Arcanist 获得 `charged-small`：下次魔法攻击 +1 |
| Limit | 每回合一次 |

初期协同保证：

- Mage 的 `searing-brand` / `arcane-channel`。
- Oracle 的 `fate-mark` 或 Astral Oracle 的 magic mark。
- Druid 的 weakness / dispel 后降低魔防。
- Princess 的 `royal-command` 让 Arcanist 有 AP 使用窗口。

#### Rank1

- HP +2。
- MDef +1，达到 MDef 2。
- `arcane-resonance` 强化：如果触发源是 Role Action，Arcanist 的下次魔法攻击同时降低目标 MDef 1 到回合结束。

#### Rank2 - Astral Arcanist

Stats：Attack +1。

Role Action:

| ID | `astral-focus` |
|---|---|
| Input | `TargetSelect` |
| Target | `AllyCard` 或 `EnemyCard` |
| Cost | 1 AP |
| Tags | `magic`, `charge`, `mark`, `soldier` |
| Effect | 选择友方：目标下次魔法伤害 +1；选择敌方：目标 MDef -1 到回合结束 |

Advanced Trait:

| ID | Effect |
|---|---|
| `star-circuit` | 每回合第一次敌方获得 `marked / burning / weakness` 时，Astral Arcanist 获得 `charged-small` |

副官方向：

- 附给英雄后：英雄 Role Action 带 `magic / mark / burn / weakness` tag 时，目标 MDef -1 到回合结束；若英雄造成魔法伤害，伤害 +1，每回合一次。

---

## 7. 初期队伍协同表

开局目标：玩家只有 1 个英雄 + 2 个普通兵时，也能看出“我选这两个普通兵是为了让英雄路线更明显”。

| 开局 | 初期协同 | 可发展方向 |
|---|---|---|
| Princess + Cleric + Shieldmaiden | 治疗触发 Cleric，Shieldmaiden 保公主 | High Priestess 圣职铁壁；Dark Lord 用 Cleric 补代价 |
| Princess + Duelist + Cleric | Royal Command 给 Duelist 斩杀窗口，Cleric 补血 | 王令斩杀 |
| Oracle + Duelist + Arcanist | Fate Mark 让 Duelist 吃 +1，Arcanist 吃 magic/mark | Astral Oracle 星术炮台；Fate Dealer 命运斩杀 |
| Oracle + Shieldmaiden + Arcanist | 预见 + 盾保护脆皮魔法队 | 稳定魔法防线 |
| Peasant + Shieldmaiden + Cleric | 低费后勤，盾与治疗都能被放大 | 丰收后勤，民兵堡垒 |
| Peasant + Duelist + Arcanist | Peasant 补给给输出，低 cost 多动作 | 低费连动 |
| Barbarian + Shieldmaiden + Cleric | Shieldmaiden 保自降防，Cleric 修风险 | 荣耀重击 |
| Barbarian + Duelist + Shieldmaiden | Barbarian 破盾压血，Duelist 收割 | 破盾斩杀 |
| Monster + Cleric + Duelist | Pact 失血后 Cleric 修，Prey 后 Duelist 打 | 深渊献祭 |
| Monster + Shieldmaiden + Arcanist | 盾诱导 0 伤，Arcanist 补魔法压力 | 反防御 |
| Knight + Shieldmaiden + Cleric | 盾与守护被 Shieldmaiden 放大，Cleric 修代伤 | 圣盾防线 |
| Knight + Shieldmaiden + Duelist | 骑士开路，Duelist 收割 shaken/marked 目标 | 黑骑突击 |
| Mage + Arcanist + Shieldmaiden | Arcanist 吃魔法触发，Shieldmaiden 保蓄力 | 星火炮台 |
| Mage + Arcanist + Cleric | 魔法爆发 + 续航 | 魔法 sustain |
| Druid + Cleric + Shieldmaiden | 净化触发 Cleric，盾保慢速队 | 反状态防线 |
| Druid + Duelist + Arcanist | weakness/hunted 让 Duelist 与 Arcanist 都吃收益 | 猎群魔刺 |

---

## 8. 中后期流派基础

### 8.1 圣职铁壁

核心件：`High Priestess` + `Holy Paladin` + `Saint Cleric` + `Aegis Shieldmaiden`

玩法：

- `sanctuary-hymn` 与 `sanctuary` 叠加 blessing / guarded。
- `field-medic` 和 `mend` 修复穿透伤害。
- `shield-drill` 和 `aegis-formation` 增厚共享盾。

反制：

- Abyssal Queen 条件绝对伤害。
- Dragon Raider 破盾。
- Druid / Wildspeaker 对 blessing / guarded 做 dispel 或 mark 集火。

### 8.2 王令斩杀

核心件：`Dark Lord Princess` + `Crimson Duelist` + `Dread Cavalier`

玩法：

- `royal-command` 创造 AP 窗口。
- `blood-edict` 给 Duelist 或 Cavalier 攻击修正。
- `dark-charge` 打开护盾，`crimson-lunge` 收割。

风险：

- AP debt。
- 如果没斩杀，失血代价和下回合 AP 缺口会被反打。

### 8.3 星术炮台

核心件：`Stellar Archmage` + `Astral Oracle` + `Astral Arcanist`

玩法：

- `fate-mark / astral-alignment` 给目标 magic vulnerability。
- `arcane-channel` 蓄力。
- `astral-focus` 和 `starfall` 爆发。

反制：

- 高 MDef。
- Druid cleanse / dispel。
- 快速击杀 Mage 或 Arcanist。

### 8.4 命运经济

核心件：`Fate Dealer` + `Harvest Saint` + `Arcane Archivist`

玩法：

- Reward reset / lock / skip 形成资源路线。
- `harvest-feast` 把 skip 转成 sustain。
- `archive-formula` 放大关键 Role Action。

风险：

- 战斗力慢热。
- 实装复杂，建议后测。

### 8.5 猎群标记

核心件：`Wildspeaker` + `Crimson Duelist` + `Dragon Raider`

玩法：

- `call-the-hunt` 赋予 hunted。
- Duelist 吃 marked/debuff。
- Dragon Raider 攻击 hunted/shaken 目标。

反制：

- cleanse mark。
- guard 转移伤害。
- 先杀普通兵，断猎群。

### 8.6 深渊献祭

核心件：`Abyssal Queen` + `Dark Lord Princess` + `Saint Cleric`

玩法：

- `dark-pact / blood-edict / abyssal-command` 通过失血换爆发。
- Saint Cleric 修复代价。
- Abyssal Queen 用绝对伤害结束防御队。

风险：

- 如果治疗被禁，自己崩盘。
- sacrifice 触发必须每回合限次。

### 8.7 黑骑破阵

核心件：`Dread Cavalier` + `Dragon Raider` + `Aegis Shieldmaiden`

玩法：

- Aegis 给盾。
- Dread Cavalier 消耗盾冲锋。
- Dragon Raider 破敌盾。

反制：

- 不堆盾，改用治疗/预见。
- Weakness 降低突击收益。

### 8.8 反状态净化

核心件：`Grove Keeper` + `Saint Cleric` + `High Priestess`

玩法：

- Cleanse 触发 heal。
- Healing 触发 blessing。
- 对 burn / mark / weakness 队形成反制。

风险：

- 对手不上状态时效率下降。
- 纯爆发和绝对伤害仍能穿透。

### 8.9 书卷圣堂

核心件：`Arcane Archivist` + `High Priestess` + `Grove Keeper`

玩法：

- `archive-formula` 复制 heal/cleanse 的弱后效。
- 不是最高输出，但工具箱强。

风险：

- 后端需要记录上一次 Role Action tags 与后效，复杂。
- 每回合限制必须硬。

### 8.10 普通兵军团

核心件：`Wildspeaker` + 任意 2 个进阶普通兵 + Soldier rewards

玩法：

- 普通兵初期 Trait 先形成方向。
- Rank2 解锁 Role Action 后，普通兵开始像小队齿轮。
- Wildspeaker 的 hunted 明确要求普通兵参与攻击。

风险：

- 普通兵太强会压过英雄。
- 需要确保英雄仍是 build 核心。

---

## 9. 普通兵初始 Trait 的最低协同保证

| Soldier | 初始 Trait | 保证协同 1 | 保证协同 2 | 额外协同 |
|---|---|---|---|---|
| Cleric | `field-medic` 响应 heal/cleanse | Princess `saintly-prayer` | Druid `cleansing-herbs` | Dark Lord / Monster sacrifice 后恢复 |
| Shieldmaiden | `shield-drill` 响应 shield gain | Knight `raise-bulwark` | Peasant `militia-wall` | Mage 蓄力保护，Holy Paladin 防线 |
| Duelist | `duel-sense` 攻击 marked/debuff +1 | Oracle `fate-mark` | Druid `call-the-hunt` | Monster `prey`，Mage `burning` |
| Arcanist | `arcane-resonance` 响应 magic/status | Mage `searing-brand` | Oracle `astral-alignment` | Druid weakness，Princess AP 指挥 |

这张表是普通兵设计底线。若某个普通兵初始 Trait 在开局阶段没有至少两条明显英雄配合，就不应进入第一版。

---

## 10. 后端接口需求

### 10.1 RoleActionMetadata

建议字段：

```csharp
public sealed record RoleActionMetadata(
    string Id,
    int SlotIndex,
    RoleActionActivationMode ActivationMode,
    IReadOnlySet<RoleActionTargetKind> ValidTargetKinds,
    int BaseApCost,
    bool EndsCharacterAction,
    IReadOnlySet<string> Tags);
```

需要从一开始支持：

- 英雄 0-2 个 roleActions。
- 普通兵 0-1 个 roleActions。
- `ActivationMode` 只表达 UI 输入方式：`Immediate` = `Input: Click`，`Targeted` = `Input: TargetSelect`。
- `ValidTargetKinds` 只表达可作用目标，不再混入“是否需要拖动”的判断。
- Role Action 的 AP cost 走 modifier 管线，不把 `BaseApCost` 视为最终 AP cost。
- `BaseApCost` 可以是 0。0 AP Role Action 仍可通过 `EndsCharacterAction`、次数限制、状态条件、debt 或 HP / shield 代价形成约束。
- 当前不设计 BP 作为 Role Action 的支付成本。BP 可以被 Role Action 奖励、影响获取上限或触发奖励侧效果，但不进入按钮支付校验。
- `SlotIndex` 支持 0/1。

### 10.2 RoleActionDefinition

```csharp
public interface IRoleActionDefinition
{
    RoleActionMetadata Metadata { get; }
    RoleActionPreview Preview(RoleActionContext context);
    RoleActionResult Execute(RoleActionContext context);
}
```

Preview 至少要返回：

- final AP cost，可能为 0。
- 是否消耗行动。
- 目标合法性。
- 预计 heal / damage / shield / status。
- 是否会奖励或触发 BP 获取，不是支付 BP。
- 由哪些 Trait / modifier 改写。

### 10.3 Trait 对 Role Action 的响应

当前 Trait hook 多围绕攻击。后续需要加通用事件：

```text
OnRoleActionPreview
OnRoleActionExecuting
OnRoleActionResolved
OnHealingDone
OnCleanseSucceeded
OnDispelSucceeded
OnShieldGained
OnMarkApplied
OnSacrificePaid
CollectRoleActionCostModifiers
CollectRoleActionEffectModifiers
```

关键是不要为每个角色写专用 hook。比如 Cleric 不需要 `OnPrincessPrayer`，只需要响应 `OnHealingDone / OnCleanseSucceeded`。

`CollectRoleActionCostModifiers` 第一版只修改 AP cost。若未来要加入 HP / shield / status stack 代价，也应另开清晰的 resource cost 结构，不要把它们混进 `BaseApCost`。BP 暂时不进入 Role Action cost。

### 10.4 ActionTargetRef

```csharp
public sealed record ActionTargetRef(
    RoleActionTargetKind Kind,
    Guid? PlayerId,
    Guid? CharacterId,
    string? StatusId);
```

需要支持：

- `AllyCard`
- `EnemyCard`
- `OwnShield`
- `EnemyShield`
- `ActionPointPanel`
- `BattlePointMedal`
- `StatusIcon`
- `None`

第一版推荐只做：

```text
None / AllyCard / EnemyCard / OwnShield / ActionPointPanel
```

StatusIcon 可以等 Druid/Cleric 净化 UI 更稳定后接。

### 10.5 Upgrade Package

英雄第一次升级不应直接写成“给 roleActionId”。建议是 Ability Package：

```csharp
public sealed record AbilityPackageDefinition(
    string Id,
    string CharacterKey,
    IReadOnlyList<string> UnlockRoleActionIds,
    IReadOnlyList<string> AddTraitIds,
    IReadOnlySet<string> Tags);
```

英雄 Lv2 和 Lv3 需要：

```text
StatPatch
TraitUpgradePatch
RoleActionUpgradePatch
AdvancedClassDefinition
```

普通兵需要：

```text
SoldierRankDefinition
SoldierClassUpgradeDefinition
```

### 10.6 日志与本地化

每个 Role Action 必须有：

- `roleActions.{id}.name`
- `roleActions.{id}.description`
- `roleActions.{id}.button`
- 失败原因：AP 不足、目标非法、角色已行动、无可净化状态等。
- 战斗日志 key，不在 C# 写中文/日文。

---

## 11. 实装优先级建议

为了避免一次做爆，建议按可验证协同链推进。

### Prototype A：Knight + Shieldmaiden

目标：验证 shield Role Action 和普通兵初始 Trait。

实现：

- `raise-bulwark`
- `shield-drill`
- `aegis-formation` 可暂缓

检查：

- Role Action 按钮 -> 选择盾是否直觉。
- `shield-drill` 是否让 Shieldmaiden 即使没按钮也有存在感。

### Prototype B：Princess + Cleric

目标：验证 heal/cleanse tag 与普通兵初始 Trait。

实现：

- `saintly-prayer`
- `field-medic`
- `mend` 可暂缓到普通兵 Rank2。

检查：

- 治疗是否值得 AP。
- 有效治疗才给 BP 是否清楚。

### Prototype C：Oracle + Duelist

目标：验证 mark -> attack modifier -> execute。

实现：

- `fate-mark`
- `duel-sense`

检查：

- 玩家能否自然理解“先标记，再让 Duelist 打”。
- 是否变成固定公式。

### Prototype D：Mage + Arcanist

目标：验证 magic/status synergy 和蓄力。

实现：

- `arcane-channel`
- `searing-brand`
- `arcane-resonance`

检查：

- 放弃本回合攻击换爆发是否好玩。
- 魔法增幅是否过高。

### Prototype E：Druid + Cleric / Duelist

目标：验证 cleanse/dispel/weakness UI。

实现：

- `cleansing-herbs`
- `weakening-spores-action`
- `field-medic`
- `duel-sense`

检查：

- 玩家能否理解哪些状态可净化/驱散。
- 状态队是否被净化完全克死。

---

## 12. 平衡防线

| 风险 | 来源 | 防线 |
|---|---|---|
| 治疗拖局 | High Priestess, Saint Cleric, Grove Keeper | 有效治疗才触发，治疗量小，禁疗/绝对伤害/爆发反制 |
| AP 滚雪球 | Royal Command, Dark Lord Princess | 明确 debt，下回合 AP -1 或本回合 BP 上限 -1 |
| BP 刷分 | Harvest Saint, Fate Dealer, Role Action reward | 单回合 BP 上限 3，自动 Trait 默认不给 BP |
| 盾无敌 | Holy Paladin, Aegis Shieldmaiden | 破盾奖励，Dragon Raider，Abyssal Queen |
| 标记斩杀过强 | Oracle, Wildspeaker, Duelist | mark 可 cleanse，守护可转移，mark 不叠加 |
| 魔法爆表 | Stellar Archmage, Astral Arcanist | 蓄力可打断，高 MDef，burn 可净化 |
| sacrifice 循环 | Dark Lord, Abyssal Queen, Cleric | sacrifice 每回合触发次数限制，治疗有效目标限制 |
| 普通兵压过英雄 | Duelist / Arcanist 高效率 | 普通兵低 HP，Role Action 解锁晚，英雄仍有 2 个按钮 |
| 文本复杂 | 所有 Role Action | Button 文案短，hover 详述，日志解释 |

---

## 13. 结论

下一阶段的设计目标不是让每张卡拥有更多文字，而是让每张卡在队伍中拥有更明确的位置。

早期：

```text
1 个英雄 + 2 个普通兵
```

玩家应该已经能看出：

- Cleric 是为了治疗/净化链。
- Shieldmaiden 是为了盾和保护。
- Duelist 是为了标记后的斩杀。
- Arcanist 是为了魔法和状态放大。

中期：

```text
英雄 Lv1 / Lv2 + 普通兵 Rank1
```

玩家开始围绕 Basic Role Action 和普通兵 Trait 形成打法。

后期：

```text
进阶英雄 Slot 2 Role Action + 进阶普通兵 Role Action + 副官 modifier
```

玩家会感觉自己在组装一台队伍机器，而不是单纯堆数字。

这份文档的核心落点：

```text
Hero 的成长靠两个 Role Action 形成职业路线。
Soldier 的价值先靠初始 Trait 进入构筑，后靠进阶 Role Action 成为支柱。
シナジー効果靠 tags / events / status 发生，不靠写死角色组合。
```
