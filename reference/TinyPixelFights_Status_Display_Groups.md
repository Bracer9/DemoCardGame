# Tiny Pixel Fights 状态显示组设计

更新时间：2026-07-02

本文定义战斗中 Buff / Debuff 的“显示组”规则。它不是单纯的 UI 整理，而是之后新增 Trait、Role Action、遗物、天气、战场规则时必须遵守的机制边界。

核心目标：

- 避免每个 Trait / Role Action 都制造一个独立状态名、独立图标和独立认知负担。
- 让玩家先看到“当前角色实际受到什么影响”，再在详情里确认来源。
- 保留后端真实 status instance，避免破坏持续时间、驱散、净化、来源追踪和数值计算。
- 让之后的新机制优先复用现有显示组，只有真的形成新决策对象时才新增显示组。

## 1. 三层信息结构

状态信息分成三层，不要混在一起。

| 层级 | 用途 | 示例 |
| --- | --- | --- |
| 发动名 / 来源名 | 角色个性、演出、日志、语音 | 守护誓约、补给篮、黑暗契约 |
| 显示组 | 玩家快速判断当前局面 | 物防上升 +3、燃烧 2、无法反击 |
| 来源详情 | 规则精度、持续时间、是否不可驱散 | 守护誓约 +2，受到主动攻击后解除 |

角色卡右侧图标和右侧 HUD 的第一职责，是回答：

> 这个角色现在受到哪些类型的效果影响？

而不是把所有来源名都堆出来。

## 2. Status Instance 与 Display Group

后端规则层仍然保留每一个真实的 status instance，用于：

- 持续时间
- 来源追踪
- 驱散 / 净化
- 是否不可驱散
- 数值计算
- 日志与演出

UI 层按 `DisplayGroupId` 聚合。

示例：

| Instance 来源 | Instance 效果 | Display Group |
| --- | --- | --- |
| `guard-oath` | 物防 +2，受到主动攻击后解除 | `physical-defense-up` |
| `supply-basket` | 物防 +1，下次己方回合开始解除 | `physical-defense-up` |
| `reward-physical-defense` | 物防 +1，本局永久，不可驱散 | `physical-defense-up` |

卡面 / HUD 可以先显示：

```text
物防上升 +4
```

展开详情时再显示：

```text
来源：
- 守护誓约 +2，受到主动攻击后解除
- 补给篮 +1，下次己方回合开始解除
- 奖励：物防 +1，不可驱散
```

## 3. 驱散与净化规则

显示组聚合不改变规则层。

- 驱散一次 = 移除 1 个可驱散 buff instance。
- 净化一次 = 移除 1 个可净化 debuff instance。
- 不要因为 UI 合并显示，就一次删除整个显示组。
- 只有具体效果明确写“移除所有同类效果”时，才删除整个显示组。
- 默认可驱散 / 可净化，不需要在 UI 写明。
- 只有不可驱散 / 不可净化才在详情中写明。

如果同一个显示组中混有可驱散和不可驱散来源，驱散只从可驱散 instance 中选择。

## 4. 当前基础显示组

### 4.1 数值 Buff

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `attack-up` | 攻击上升 | 数值求和 | `harvest`, `pact`, `reward-attack` | 是 |
| `magic-attack-up` | 魔法攻击上升 | 数值求和 | `magic-power`, `charged`, `reward-magical-attack` | 是 |
| `physical-defense-up` | 物防上升 | 数值求和 | `guarded`, `supply-guard`, `reward-physical-defense` | 是 |
| `magical-defense-up` | 魔防上升 | 数值求和 | `reward-magical-defense` | 是 |
| `ap-up` | AP 增加 | 通常进入日志 / 资源变化，不常驻为角色状态 | `royal-command` | 谨慎 |

说明：

- 只要效果是攻击力提高，优先归入 `attack-up`，详情里写清楚是本回合、下一次攻击还是永久。
- 只对魔法伤害有效的攻击强化，归入 `magic-attack-up`。
- 不要给“补给守备”“守护誓约”“圣盾祝词”各自新增常驻显示组。

### 4.2 数值 Debuff

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `attack-down` | 攻击下降 | 数值求和 | `weakness` | 是 |
| `physical-defense-down` | 物防下降 | 数值求和 | `rage` 的代价部分 | 是 |
| `magical-defense-down` | 魔防下降 | 数值求和 | `rage` 的代价部分 | 是 |
| `damage-taken-up` | 易伤 | 按类型与数值聚合 | `searing-brand` | 是 |

说明：

- `weakness` 玩家侧可以显示为“衰弱 / 攻击下降”。如果未来衰弱成为重要关键词，再保留特殊名。
- `rage` 同时有正面与负面部分，UI 可以拆成“再行动机会”和“物防/魔防下降”显示。

### 4.3 持续伤害与触发伤害

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `burning` | 燃烧 | 层数求和 | 魔法师 Trait, `searing-brand` | 是 |
| `zero-damage-punish` | 破绽追伤 | 来源逐条显示或数值合计 | `prey` | 谨慎 |

说明：

- 燃烧是玩家容易理解的关键词，可以保留独立显示。
- `prey` 是否保留“猎物”作为关键词，取决于怪兽体系未来是否继续强化。如果只是“0 伤害追伤”，可以归入 `zero-damage-punish`。

### 4.4 行动限制 / 行动变化

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `cannot-attack` | 无法攻击 | 任一来源存在即显示 | `attack-sealed` | 是 |
| `cannot-counter` | 无法反击 | 任一来源存在即显示 | `challenged` | 是 |
| `extra-attack` | 可再次攻击 | 次数合计 | `star-reading`, `war-cry`, `harvest` 分支 | 是 |
| `cooldown` | 冷却中 | Role Action 按钮层显示，不作为角色 buff 常驻 | Role Action CD | 是 |

说明：

- 行动限制通常比数值变化更重要，卡面显示优先级要高。
- `extra-attack` 不一定必须作为卡面 buff 图标常驻。如果卡牌可行动提示已经足够清楚，可以只在详情层展示来源。

### 4.5 标记与概率效果

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `fate-mark` | 命运标记 | 特殊显示 | `fate-mark` | 谨慎 |
| `foresight` | 预见 | Aura / 全队效果 | 占卜师 Trait | 谨慎 |

说明：

- 概率类状态不适合过度抽象，否则玩家更难理解。
- 如果效果会显著改变预测结果，应在预测 HUD 中明确展示。

### 4.6 延迟状态

| Display Group ID | 显示名 | 聚合方式 | 当前来源示例 | 是否允许新增来源 |
| --- | --- | --- | --- | --- |
| `pending-harvest` | 播种 | 特殊显示 | `harvest-pending` | 谨慎 |
| `pending-charge` | 蓄能准备 | 特殊显示 | `charged-pending` | 谨慎 |
| `delayed-effect` | 延迟效果 | 来源逐条显示 | 未来天气 / 副官 / 遗物 | 是 |

说明：

- 播种 / 丰收是农民身份核心，暂时可以保留特殊名。
- 如果未来延迟效果过多，应统一显示为“延迟效果”，详情列来源。

### 4.7 特殊关键词

特殊关键词必须少。只有当它是角色身份、核心循环或长期决策对象时，才允许保留。

| Display Group ID | 显示名 | 当前来源示例 | 保留理由 |
| --- | --- | --- | --- |
| `harvest` | 丰收 | 农民 Trait / `field-work` | 农民核心循环 |
| `rage` | 狂怒 | `war-cry` | 高风险高收益身份标识 |
| `pact` | 契约 | `dark-pact` | 代价换爆发，角色感强 |
| `beast-rage` | 野兽之怒 | 怪兽 Trait | 传奇角色核心状态，不可驱散 |

新增特殊关键词前必须问：

1. 它是否持续影响玩家多个决策？
2. 它是否是某个角色 / 职业的核心身份？
3. 它是否无法用已有显示组清楚表达？

如果答案不是明确的“是”，不要新增特殊关键词。

## 5. 卡面显示规则

角色卡右侧最多显示 5 个显示组 icon。

建议优先级：

1. 行动限制：无法攻击、无法反击
2. 持续伤害：燃烧
3. 易伤 / 攻击下降
4. 防御变化
5. 攻击变化
6. 特殊关键词
7. 延迟效果

如果超过 5 个：

- 显示前 4 个高优先级组。
- 第 5 个显示 `+N`。
- inspector 展开完整列表。

## 6. Inspector 显示规则

右侧 Buff / Debuff HUD 显示 display group。

每个显示组条目应包含：

- 图标
- 显示名
- 总量 / 层数 / 状态
- 是否含不可驱散来源

展开详情时显示：

- 来源名
- 数值
- 持续时间
- 解除条件
- 不可驱散，仅当存在时显示

默认可驱散不写。

## 7. 新机制加入规则

新增 Trait / Role Action / 遗物 / 天气时，按以下顺序判断：

1. 能否归入已有显示组？
2. 是否只是已有数值变化的不同来源？
3. 是否需要玩家在卡面上快速识别？
4. 是否需要独立图标？
5. 是否会与驱散 / 净化 / 预测 / 日志发生交互？

只有当它满足“玩家必须独立识别”时，才允许新增 Display Group。

反例：

- “圣盾祝词：物防 +1”不应该新增 `holy-shield-blessing`，应进入 `physical-defense-up`。
- “农夫土墙：物防 +1”不应该新增 `farmer-wall`，应进入 `physical-defense-up`。

正例：

- “燃烧”会造成回合开始伤害，可以独立显示。
- “无法反击”直接影响攻击决策，可以独立显示。
- “播种 / 丰收”是农民核心循环，可以独立显示。

## 8. 当前需要重构的映射草案

| 当前 Status ID | 建议 Display Group | 备注 |
| --- | --- | --- |
| `magic-power` | `magic-attack-up` | 来源：占卜师 Trait |
| `charged` | `magic-attack-up` | 来源：魔法师 Role Action |
| `pact` | `attack-up` 或 `pact` | 如果强调契约身份，可保留特殊名 |
| `harvest` | `harvest` / `attack-up` | 建议保留特殊名，同时在详情写攻击 +2 |
| `reward-attack` | `attack-up` | 不可驱散 |
| `reward-magical-attack` | `magic-attack-up` | 不可驱散 |
| `guarded` | `physical-defense-up` | 来源：骑士 |
| `supply-guard` | `physical-defense-up` | 来源：农民 |
| `reward-physical-defense` | `physical-defense-up` | 不可驱散 |
| `reward-magical-defense` | `magical-defense-up` | 不可驱散 |
| `burning` | `burning` | 层数显示 |
| `weakness` | `attack-down` 或 `weakness` | 建议先显示为“衰弱 / 攻击下降” |
| `searing-brand` | `damage-taken-up` | 魔法易伤 |
| `marked` | `fate-mark` | 概率类，保留特殊 |
| `prey` | `zero-damage-punish` 或 `prey` | 取决于怪兽体系是否继续强化 |
| `attack-sealed` | `cannot-attack` | 行动限制 |
| `challenged` | `cannot-counter` | 行动限制 |
| `harvest-pending` | `pending-harvest` | 农民核心循环 |
| `charged-pending` | `pending-charge` | 魔法师延迟蓄能 |
| `rage` | `rage` + `physical-defense-down` + `magical-defense-down` | UI 可拆分显示 |
| `beast-rage` | `beast-rage` | 不可驱散，怪兽核心 |

## 9. 实装顺序建议

1. 先给 status metadata 增加 `DisplayGroupId`、`Tags`、`SourceAbilityId`，不改变玩法。
2. DTO 继续传 status instance，同时传 display group 信息。
3. 前端 inspector 先按 display group 聚合，卡面 icon 再聚合。
4. 驱散 / 净化逻辑从 hardcoded ID 逐步改成 tags。
5. 逐步把纯数值 status class 改成通用 modifier status。

不要第一步就大规模删除所有 class。先让显示和查询体系站稳，再整理后端实现。

## 10. 轻量版 Aura 折叠规则

在完整 Display Group 重构之前，先采用一个轻量规则，专门处理“活着就无条件持续生效”的光环类效果。

### 10.1 Aura 的判定

满足以下条件的效果，可以从普通 Buff / Debuff 显示中移出，归入 `aura` 显示组：

1. 来源角色只要存活就无条件生效。
2. 效果不需要玩家逐个角色判断是否能驱散。
3. 效果已经有其他 UI 或演出提示，例如 Trait 区、预测说明、伤害/治疗演出。
4. 效果本质是队伍规则或场上规则，而不是贴在某个角色身上的临时状态。

当前纳入 Aura 折叠的效果：

| Status ID | 来源 | 说明 |
| --- | --- | --- |
| `blessing` | 公主 Trait `saints-prayer` | 公主存活时，全队回合开始治疗。 |
| `foresight` | 占卜师 Trait `stargazers-aegis` | 占卜师存活时，全队概率减伤。 |
| `magic-power` | 占卜师 Trait `stargazers-aegis` | 占卜师存活时，全队魔法伤害与燃烧伤害增强。规则层仍保留真实 status，只在显示层折叠。 |
| `guard` | 骑士 Trait `interposing-shield` | 骑士存活且守护未消耗时，全队共享一次主动物理代伤。 |

暂不纳入：

| Status ID | 原因 |
| --- | --- |
| `team-shield` | 共享盾不是角色 Buff，已经作为战场中间的盾显示。 |

### 10.2 UI 表现

卡面右侧：

- 不显示 `isAura = true` 的单独状态图标。
- 这能避免公主、占卜师、骑士同时在场时，所有角色右侧被 Aura 图标刷满。

右侧 inspector：

- 普通 Buff / Debuff 正常显示。
- Aura 统一折叠成 1 条 `Aura / 光环`。
- 这条 `Aura` 固定放在其他 Buff / Debuff 的最下面。
- hover 到 `Aura` 条目时，展开二级说明，列出每个实际 Aura 的名称、时机和描述。

二级说明只负责展示，不改变规则。

### 10.3 后续新机制边界

新增光环类 Trait / 遗物 / 天气时，先判断是否应该进入 `aura` 折叠组。

应该进入 `aura` 的例子：

- “某角色存活时，全队物理伤害 +1。”
- “当前天气存在时，双方魔防 -1。”
- “某遗物永久使全队治疗量 +1。”

不应该进入 `aura` 的例子：

- 会被驱散、净化、消耗或转移的临时 Buff。
- 需要玩家选择是否攻击该角色来触发的标记。
- 会作为单体状态被 Role Action 指定处理的效果。

一句话原则：

> 如果它是“场上规则”，折叠进 Aura；如果它是“贴在角色身上的可处理状态”，留在 Buff / Debuff。
