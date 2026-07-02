# Tiny Pixel Fights — Trait / Role Action 实装表

更新时间：2026-07-02

本表记录当前已经实装、会影响战斗规则的 Trait 与 Role Action。  
以后新增、删除、改数值或改交互时，优先同步维护本表。

相关文档：

- `reference/TinyPixelFights_Ability_Taxonomy_Phase3A.md`：Ability / Trait / Role Action 的术语边界。
- `reference/TinyPixelFights_Status_Display_Groups.md`：Buff / Debuff 显示组边界。
- `reference/TinyPixelFights_RoleAction_Implementation_20260630.md`：Role Action 系统实现说明。

## 1. 系统口径

### Trait

Trait 是角色自带、自动触发或持续生效的特性。玩家不直接按按钮发动。  
UI 层统一显示为“Trait / 特性”，后端用触发时点、作用范围和效果类型描述。

### Role Action

Role Action 是角色被选中后，在左侧 HUD 第三区域显示的职业行动按钮。  
当前通过奖励“英雄职业训练”解锁：选择一名可升级英雄，再从该英雄两个 Lv1 Basic Role Action 中选择 1 个。

### 当前输入方式

- `Click`：点击按钮后立即发动。
- `TargetSelect`：点击按钮后选择目标，或从按钮 / 施法者卡牌拖出绿色抛物线选择合法目标。

### 当前使用限制

- `Repeatable = No`：同一角色同一 Role Action 每个己方回合最多使用 1 次。
- `Cooldown`：使用后需要跳过多少个己方回合才能再次使用。
- Role Action 默认不消耗主动攻击权。是否封印攻击、追加攻击次数或改变攻击次数，由具体效果决定。
- “本回合第一个主动攻击角色”只统计主动攻击，不统计 Role Action。

## 2. 当前实装 Trait 总表

| 角色 | Trait ID | 名称 | Trigger | Scope | Effect | 当前实际效果 |
| --- | --- | --- | --- | --- | --- | --- |
| 公主 | `saints-prayer` | 圣女的祝福 | TurnStart | Team | Heal | 公主存活且轮到己方回合开始时，我方所有存活角色 HP +1，最多超过最大 HP 2 点。 |
| 占卜师 | `stargazers-aegis` | 星读加护 | Continuous | Team | DamageModifier | 我方受到伤害时，物理 25% / 魔法 50% 概率伤害 -1；我方魔法伤害与燃烧伤害 +1。 |
| 农民 | `spring-harvest` | 春播秋收 | OnAttackDeclared | Self | Status | 若农民是本回合第一个主动攻击角色，且没有丰收，则获得播种；下个己方回合开始转为丰收，主动攻击 +2。Role Action 不会打断这个“第一次主动攻击”条件。 |
| 魔法师 | `searing-mark` | 灼热刻印 | OnAttackResolved | Enemy | Status | 主动攻击结算后，若目标仍存活，50% 赋予 1 层燃烧。 |
| 德鲁伊 | `weakening-spores` | 衰弱孢子 | OnAttackResolved | Enemy | Dispel / Status | 主动攻击造成 HP 伤害时 100% 发动；未造成 HP 伤害时 50% 发动。发动时随机移除目标 1 个可驱散 Buff，并赋予待生效衰弱；目标下个行动回合主动攻击伤害 -2。 |
| 狂战士 | `aftershock-axe` | 战斧余波 | OnAttackResolved | EnemyTeam | Damage | 主动攻击对目标造成至少 3 点 HP 伤害时，对被攻击目标相邻的 1 名存活敌人造成 1 点物理伤害。若只有 1 个邻居则必定命中；若没有邻居则不造成额外伤害。 |
| 怪物 | `predatory-instinct` | 美女与野兽 | OnAttackResolved / OnCharacterDefeated | Enemy / Self | Damage / DamageModifier | 对非公主目标主动攻击造成 0 点 HP 伤害时，追击 3 点绝对伤害；若我方公主存活，追击 +1。攻击公主时无视共享盾，但不无视公主自身防御；攻击后怪物受到造成伤害 2 倍的自伤，但不会因此死亡。任意公主死亡后，怪物获得野兽之怒：基础攻击 +2。 |
| 骑士 | `interposing-shield` | 替身之盾 | OnDamaged | Ally | Shield / DamageModifier | 对骑士以外我方角色受到的主动物理攻击，全队共享 1 次：目标受到的主动伤害 -1，骑士承担 1 点物理伤害；该承担伤害会再受骑士自身物防影响。若攻击被共享盾完全吸收，则不发动。 |

## 3. Trait 相关 Status 总表

| Status ID | 类型 | Display Group 建议 | 来源 Trait | 当前效果 |
| --- | --- | --- | --- | --- |
| `blessing` | Aura / Buff | `heal-aura` 或来源详情 | `saints-prayer` | UI 用于说明公主全队回合开始治疗。实际治疗由 Trait 在回合开始直接结算。 |
| `foresight` | Aura / Buff | `foresight` | `stargazers-aegis` | UI 用于说明概率减伤。实际减伤由 Trait 在受伤时结算。 |
| `magic-power` | Aura / Buff | `magic-attack-up` | `stargazers-aegis` | 魔法伤害与燃烧伤害 +1。 |
| `harvest-pending` | Buff | `pending-harvest` | `spring-harvest` | 下个己方回合开始时获得 `harvest`。 |
| `harvest` | Buff | `harvest` / `attack-up` | `spring-harvest` | 主动攻击 +2，持续到本回合结束。 |
| `burning` | Debuff | `burning` | `searing-mark` / `searing-brand` | 回合开始时消耗 1 层并受到 1 点魔法伤害；可叠层。 |
| `weakness-pending` | Debuff | `delayed-effect` / `attack-down` | `weakening-spores` | 下个目标行动回合转为 `weakness`。 |
| `weakness` | Debuff | `attack-down` | `weakening-spores` / `weakening-spores-action` | 主动攻击伤害 -2，持续到目标行动回合结束。 |
| `aftershock-axe` | Trait Damage ID | 无常驻显示 | `aftershock-axe` | 用于战斧余波的额外物理伤害事件。 |
| `predatory-instinct` | Trait Damage ID | 无常驻显示 | `predatory-instinct` | 用于美女与野兽的绝对追击 / 公主反噬事件。 |
| `beast-rage` | Buff | `beast-rage` | `predatory-instinct` | 基础攻击 +2；不可驱散。 |
| `guard` | Aura / Buff | `guard` | `interposing-shield` | UI 用于说明骑士守护。实际守护由骑士 Trait 一次性结算。 |

## 4. Role Action 相关 Status 总表

| Status ID | 类型 | Display Group 建议 | 来源 Role Action | 当前效果 |
| --- | --- | --- | --- | --- |
| `guarded` | Buff | `physical-defense-up` | `guard-oath` | 物防 +2；下一次受到主动攻击后解除。 |
| `rage` | Buff / 代价 | `rage` + `physical-defense-down` + `magical-defense-down` | `war-cry` | 物防/魔防 -2；本回合攻击次数 +1；击破共享盾时对攻击对象追加当前攻击力的绝对伤害。 |
| `challenged` | Debuff | `cannot-counter` | `challenge` | 本回合无法反击。 |
| `charged-pending` | Buff | `pending-charge` | `arcane-channel` | 下个己方回合开始时转化为 `charged`。 |
| `charged` | Buff | `magic-attack-up` | `arcane-channel` | 自己的魔法攻击 +2。 |
| `attack-sealed` | Debuff | `cannot-attack` | `arcane-channel` | 本回合无法主动攻击。 |
| `searing-brand` | Debuff | `damage-taken-up` | `searing-brand` | 本回合受到的魔法伤害 +1。 |
| `marked` | Debuff | `fate-mark` | `fate-mark` | 下一次主动攻击我方目标时，50% 伤害减半，50% 伤害 +1，随后解除。 |
| `prey` | Debuff | `zero-damage-punish` / `prey` | `predatory-gaze` | 本回合每次受到 0 点 HP 伤害时，追加 2 点绝对伤害。 |
| `pact` | Buff | `attack-up` / `pact` | `dark-pact` | 下一次主动攻击 +4，发动后解除。 |
| `supply-guard` | Buff | `physical-defense-up` | `supply-basket` | 物防 +1，持续到下次己方回合开始。 |

## 5. 当前实装 Role Action 总表

| 角色 | ID | 名称 | Input | Target | Cost | Repeatable | Cooldown | Tags | 当前实际效果 | 备注 |
| --- | --- | --- | --- | --- | ---: | --- | ---: | --- | --- | --- |
| 公主 | `saintly-prayer` | 圣女祈祷 | TargetSelect | AllyCard | 1 AP | No | 0 | heal, cleanse, holy | 治疗目标 2 HP；若目标有可净化 debuff，净化 1 个并获得 1 BP。 | BP 受单回合获取上限制约。 |
| 公主 | `royal-command` | 王令 | Click | ActionPointPanel | 1 AP | No | 1 | command, action-point | 本回合 +2 AP；下个己方回合开始 AP -1。使用后下个己方回合不可用。 | 改变 AP 经济，默认不给 BP。 |
| 骑士 | `guard-oath` | 守护誓约 | TargetSelect | AllyCard | 2 AP | No | 0 | guard, defense | 自身以外的我方角色获得 `guarded`：物防 +2，持续到下一次受到主动攻击之后。 | 不能选择骑士自身。 |
| 骑士 | `raise-bulwark` | 举盾号令 | Click | OwnShield | 2 AP | No | 0 | shield, guard | 我方拥有共享盾时可发动；共享盾 +2，共享盾物防 +2，持续到下次己方回合开始。 | 当前盾不是实体目标，因此点击后直接发动。 |
| 狂战士 | `war-cry` | 战吼 | Click | SelfCard | 1 AP | No | 0 | charge, attack-modifier, sacrifice | 自身获得 `rage`：直到下个己方回合，物防/魔防 -2；本回合攻击次数 +1；击破共享盾时，对攻击对象造成当前攻击力的绝对伤害。 | 使用后不消耗攻击权。 |
| 狂战士 | `challenge` | 破势 | TargetSelect | EnemyCard | 1 AP | No | 0 | pressure, counter-lock, physical | 目标获得 `challenged`：本回合内无法反击。 | 保留 ID 兼容既有接口；显示名不再使用“挑衅”。 |
| 魔法师 | `arcane-channel` | 秘术蓄能 | Click | SelfCard | 1 AP | No | 0 | charge, magic | 本回合不能再主动攻击；下个己方回合获得 `charged`，自己的魔法攻击 +2。 | 使用后不视为主动攻击。 |
| 魔法师 | `searing-brand` | 灼热刻印 | TargetSelect | EnemyCard | 1 AP | No | 0 | burn, magic, mark | 目标获得 1 层 `burning`，并获得 `searing-brand`：本回合受到的魔法伤害 +1。 | 燃烧可叠层；目标回合开始时每次消耗 1 层。 |
| 占卜师 | `star-reading` | 星读 | TargetSelect | AllyCard | 1 AP | No | 0 | foresight, defense, fate | 只能选择已经主动攻击过的我方魔法攻击单位。目标本回合可以再次主动攻击 1 次。 | 用追加攻击上限实现，兼容未来多次攻击机制。 |
| 占卜师 | `fate-mark` | 命运标记 | TargetSelect | EnemyCard | 1 AP | No | 0 | mark, fate | 目标获得 `marked`：下一次主动攻击我方时，50% 伤害减半，50% 伤害 +1，随后解除。 | 预测中显示为伤害范围和说明。 |
| 怪物 | `predatory-gaze` | 捕食凝视 | TargetSelect | EnemyCard | 1 AP | No | 0 | mark, absolute, prey | 目标获得 `prey`：本回合每次受到 0 点 HP 伤害时，追加 2 点绝对伤害。 | 可多次触发；绝对伤害本身不会递归触发 `prey`。 |
| 怪物 | `dark-pact` | 黑暗契约 | TargetSelect | AllyCard, SelfCard | 1 AP | No | 0 | sacrifice, attack-modifier, battle-point | 目标失去 4 HP，获得 `pact`：下一次主动攻击 +4；若目标 HP 低于一半，额外获得 1 BP。 | HP 失去是代价，不走盾 / 防御 / 预见；`pact` 发动后解除。 |
| 农民 | `supply-basket` | 补给篮 | TargetSelect | AllyCard | 0 AP | No | 0 | heal, soldier, support | 治疗我方 1 体 1 HP，并赋予 `supply-guard`：物防 +1，持续到下次己方回合开始。 | 0 AP 但仍遵守默认每回合一次。 |
| 农民 | `field-work` | 田间劳作 | Click | SelfCard | 1 AP | No | 1 | harvest, charge, support | 如果已有 `harvest`，本回合攻击次数 +1；如果已有 `harvest-pending`，回复自己 2 HP；如果两者都没有，获得 `harvest-pending`。 | 用于在播种 / 丰收节奏中切换续航与行动次数。 |
| 德鲁伊 | `cleansing-herbs` | 净化草药 | TargetSelect | AllyCard | 1 AP | No | 0 | cleanse, heal, nature | 移除目标 1 个可净化 debuff，优先移除伤害型 debuff；成功时目标 HP +1，可突破上限。 | 没有可净化 debuff 时不治疗。 |
| 德鲁伊 | `weakening-spores-action` | 衰弱孢子 | TargetSelect | EnemyCard | 1 AP | No | 0 | dispel, weakness, nature | 随机移除目标 1 个可驱散 buff，并施加 `weakness`。 | 即时衰弱，不是待生效衰弱。 |

## 6. 后端实现位置

| 内容 | 文件 |
| --- | --- |
| 角色基础数据与 Trait ID | `Domain/CharacterDefinition.cs` |
| Trait metadata / hook / registry | `Domain/Traits.cs` |
| Role Action metadata / upgrade choices | `Domain/RoleActions.cs` |
| Trait / Role Action 执行分发与效果 | `Domain/GameEngine.cs` |
| Status 生命周期与数值 modifier | `Domain/StatusEffects.cs` |
| 角色状态、使用记录、冷却记录 | `Domain/GameState.cs` |
| DTO 输出 | `Api/GameDtos.cs` |
| 预测 | `Api/AttackPreviewService.cs` |
| HUD、按钮、目标选择、拖拽抛物线 | `wwwroot/app.js` |
| 显示文本 | `wwwroot/locales/ja.json`, `wwwroot/locales/zh.json` |

## 7. 维护规则

新增或修改 Trait / Role Action 时至少检查：

1. `Domain/Traits.cs` 或 `Domain/RoleActions.cs`：metadata、触发、scope、effect、tags、cost、target、repeatable、cooldown。
2. `Domain/GameEngine.cs` / `Domain/StatusEffects.cs`：执行效果和状态生命周期。
3. `Api/AttackPreviewService.cs`：如果会影响主动攻击、反击、盾、防御、额外伤害或死亡结果，需要同步预测。
4. `wwwroot/app.js`：目标选择、拖拽、按钮可用性、HUD、日志演出。
5. `wwwroot/locales/ja.json` / `zh.json`：名称、按钮短名、描述、日志、状态文本。
6. `reference/TinyPixelFights_Status_Display_Groups.md`：确认是否复用现有显示组，避免新增无意义 buff/debuff 图标。
7. 本表。

