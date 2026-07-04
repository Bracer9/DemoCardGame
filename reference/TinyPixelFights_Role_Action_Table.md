# Tiny Pixel Fights — Trait / Role Action 表

更新时间：2026-07-04

本表记录当前 Trait / Role Action 的设计基准与下一轮状态整合目标。部分内容是“待实装调整”，不代表当前代码已经全部完成迁移。以后新增、删除、改数值或改交互时，优先同步维护本表。

相关文档：

- `reference/TinyPixelFights_Ability_Taxonomy_Phase3A.md`
- `reference/TinyPixelFights_Status_Taxonomy.md`
- `reference/TinyPixelFights_Status_Display_Groups.md`
- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_RoleAction_Implementation_20260630.md`

## 1. 系统口径

### Trait

Trait 是角色自带、自动触发或持续生效的特性。玩家不直接按按钮发动。UI 层统一显示为“Trait / 特性”。

### Role Action

Role Action 是角色被选中后，在左侧 HUD 第三区域显示的职业行动按钮。当前通过奖励“英雄职业训练”解锁：选择一名可升级英雄，再从该英雄两个 Lv1 Basic Role Action 中选择 1 个。

### 输入方式

- `Click`：点击按钮后立即发动。
- `TargetSelect`：点击按钮后选择目标，或从施法者卡牌拖出绿色抛物线选择合法目标。

### 使用限制

- 默认 `Repeatable = No`：同一角色同一 Role Action 每个己方 turn 最多使用 1 次。
- `Cooldown`：使用后需要跳过多少个己方 turn 才能再次使用。
- Role Action 默认不消耗主动攻击权。是否封印攻击、追加攻击次数或改变攻击次数，由具体效果决定。
- “本 turn 第一个主动攻击角色”只统计主动攻击，不统计 Role Action。

## 2. 通用状态总表

| 状态 | 类型 | 规则 | 叠加 |
| --- | --- | --- | --- |
| 强攻 | Buff | 主动物理 / 普通攻击伤害 ×1.5，向上取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 猛击 | Buff | 下一次主动物理 / 普通攻击伤害 ×2。 | 按次数叠加，效果不叠加。 |
| 魔涌 | Buff | 魔法伤害 / 普通攻击伤害 ×1.5，向上取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 咏唱 | Buff | 下一次魔法伤害 ×2。 | 按次数叠加，效果不叠加。 |
| 坚守 | Buff | 受到物理伤害 ×0.5。 | 按 turn 叠加持续时间，效果不叠加。 |
| 护咒 | Buff | 受到魔法伤害 ×0.5。 | 按 turn 叠加持续时间，效果不叠加。 |
| 脆弱 | Debuff | 受到物理伤害 ×1.25，向上取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 空虚 | Debuff | 受到魔法伤害 ×1.25，向上取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 力竭 | Debuff | 造成物理伤害 ×0.5，向下取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 磨损 | Debuff | 造成魔法伤害 ×0.5，向下取整。 | 按 turn 叠加持续时间，效果不叠加。 |
| 燃烧 / 炎上 | Debuff | 己方 turn 开始时消耗 1 层并受到 1 点魔法伤害。后端保留伤害可提高接口。 | 可叠层，伤害不叠加。 |
| 战栗 | Debuff | 无法反击。 | 一次性赋予，持续 turn 不叠加。 |

通用状态默认可驱散 / 可净化。游戏文本不用特别写；只有不可驱散才写。

## 3. Trait 总表

| 角色 | Trait ID | 名称 | Trigger | Scope | 调整后效果 | 状态归类 |
| --- | --- | --- | --- | --- | --- | --- |
| 公主 | `saints-prayer` | 圣女的祝福 | TurnStart | Team | 公主存活且轮到己方 turn 开始时，我方所有存活角色 HP +1，最多超过最大 HP 2 点。 | Aura / 特殊治疗 |
| 占卜师 | `stargazers-aegis` | 星读加护 | OnDamage / Continuous | Team | 我方受到伤害时，30% 使该伤害 ×0.5。我方魔法和燃烧伤害 +1。 | Aura |
| 农民 | `spring-harvest` | 春播秋收 | OnAttackDeclared | Self | 可播种的 turn 内，若农民是本 turn 第一个发起主动攻击的角色，则触发播种 / 丰收循环。Role Action 不影响这个条件。 | 特殊循环 |
| 魔法师 | `searing-mark` | 灼热刻印 | OnAttackResolved | Enemy | 主动攻击结算后，若目标仍存活，50% 赋予 1 层燃烧。 | 燃烧 |
| 德鲁伊 | `weakening-spores` | 衰弱孢子 | OnAttackResolved | Enemy | 主动攻击造成 HP 伤害时 100% 发动；未造成 HP 伤害时 50% 发动。发动时随机移除目标 1 个可驱散 Buff，并赋予目标 2 turn 力竭和磨损；即刻生效。 | 力竭 + 磨损 |
| 狂战士 | `aftershock-axe` | 战斧余波 | OnAttackResolved | EnemyTeam | 主动攻击对目标造成至少 3 点 HP 伤害时，对被攻击目标相邻的存活敌人造成“已造成伤害 1/3，向上取整”的物理伤害。若没有邻居则不造成额外伤害。 | 事件伤害 |
| 怪物 | `predatory-instinct` | 美女与野兽 | OnAttackResolved / OnCharacterDefeated | Enemy / Self | 对非公主目标主动攻击造成 0 点 HP 伤害时，追击当前怪物攻击力点绝对伤害；若我方公主存活，追击 +1。攻击公主时造成当前怪物攻击力点绝对伤害，攻击后怪物受到造成伤害 2 倍的自伤，但不会因此死亡。任意公主死亡后，怪物获得野兽之怒：基础攻击 +2。 | 特殊 Trait / 野兽之怒 |
| 骑士 | `interposing-shield` | 替身之盾 | OnDamaged | Ally | 对骑士以外我方角色受到的主动物理攻击，全队共享 1 次：我方将受到 1 点及以上主动物理伤害时，骑士承担目标受到伤害的 1/3，向上取整；该承担伤害会再受骑士自身物防影响。 | Aura / 反应规则 |

## 4. Trait 相关状态

| Status / 显示 | 类型 | 来源 Trait | 效果 |
| --- | --- | --- | --- |
| 圣女的祝福 | Aura | `saints-prayer` | 回合开始治疗。 |
| 星读加护 | Aura | `stargazers-aegis` | 概率减伤、魔法与燃烧伤害 +1。 |
| 播种 | 特殊状态 | `spring-harvest` | 下个己方 turn 开始时转入丰收。 |
| 丰收 | 特殊循环 / 可挂靠强攻 | `spring-harvest` | 农民本 turn 输出强化。后续可转为强攻。 |
| 燃烧 | 通用 Debuff | `searing-mark` | 己方 turn 开始消耗 1 层并受到魔法伤害。 |
| 力竭 | 通用 Debuff | `weakening-spores` | 造成物理伤害 ×0.5。 |
| 磨损 | 通用 Debuff | `weakening-spores` | 造成魔法伤害 ×0.5。 |
| 野兽之怒 | 特殊 Buff | `predatory-instinct` | 基础攻击 +2；不可驱散。 |
| 守护 / 替身之盾 | Aura | `interposing-shield` | 骑士存活时提供一次性反应保护。 |

## 5. Role Action 总表

| 角色 | ID | 名称 | Input | Target | Cost | Repeatable | Cooldown | 调整后效果 | 状态归类 |
| --- | --- | --- | --- | --- | ---: | --- | ---: | --- | --- |
| 公主 | `saintly-prayer` | 圣女祈祷 | TargetSelect | AllyCard | 1 AP | No | 0 | 治疗目标 2 HP；若目标有可净化 Debuff，净化 1 个并获得 1 AP。 | 治疗 / 净化 / AP |
| 公主 | `royal-command` | 王令 | Click | ActionPointPanel | 1 AP | No | 1 | 本 turn +2 AP；下个己方 turn 开始 AP -1。使用后下个己方 turn 不可用。 | 资源规则 |
| 骑士 | `guard-oath` | 守护誓约 | TargetSelect | AllyCard | 1 AP | No | 0 | 自身以外的我方角色获得 1 层守护誓约。受到主动物理攻击时，伤害 -2 并消耗 1 层。可叠层，可驱散；驱散时整类移除。 | 特殊次数 Buff |
| 骑士 | `raise-bulwark` | 举盾号令 | Click | OwnShield | 1 AP | No | 0 | 我方拥有共享盾时可发动；共享盾层数 ×1.5，向上取整。共享盾物防 +2，持续 2 turn。 | 盾规则 / 特殊物防 |
| 狂战士 | `war-cry` | 战吼 | Click | SelfCard | 1 AP | No | 0 | 自身获得狂怒：物防 / 魔防 -2，持续到下个己方 turn；本 turn 主动攻击次数 +1；若击破共有盾，对攻击对象追加当前攻击力点绝对伤害。 | 特殊强化 / 行动次数 |
| 狂战士 | `challenge` | 破势 | TargetSelect | EnemyCard | 1 AP | No | 0 | 目标获得战栗，1 turn 不能反击。 | 战栗 |
| 魔法师 | `arcane-channel` | 秘术蓄能 | Click | SelfCard | 2 AP | No | 0 | 本 turn 不能再主动攻击；下个己方 turn 获得 2 层咏唱。 | 咏唱 |
| 魔法师 | `searing-brand` | 灼热刻印 | TargetSelect | EnemyCard | 1 AP | No | 0 | 目标获得 1 层燃烧，并获得 1 层空虚。 | 燃烧 + 空虚 |
| 占卜师 | `star-reading` | 星读 | TargetSelect | AllyCard | 1 AP | No | 0 | 只能选择已经主动攻击过的我方魔法攻击单位。目标本 turn 可以再次主动攻击 1 次。 | 行动次数系统 |
| 占卜师 | `fate-mark` | 命运标记 | TargetSelect | EnemyCard | 1 AP | No | 0 | 目标获得命运标记：下一次主动攻击我方时，50% 伤害减半，50% 伤害 +1，随后解除。 | 特殊印记 |
| 怪物 | `predatory-gaze` | 捕食凝视 | TargetSelect | EnemyCard | 1 AP | No | 0 | 目标获得猎物：本 turn 每次受到 0 点 HP 伤害时，追加 2 点绝对伤害。 | 特殊印记 |
| 怪物 | `dark-pact` | 黑暗契约 | TargetSelect | AllyCard, SelfCard | 1 AP | No | 0 | 目标失去最多 4 HP，至少保留 1 HP；获得黑暗契约：下一次主动攻击附加 4 点绝对伤害；若目标 HP 低于一半，额外获得 1 BP。 | 特殊 Buff |
| 农民 | `supply-basket` | 补给篮 | TargetSelect | AllyCard | 0 AP | No | 0 | 治疗我方 1 体 1 HP，并赋予 2 层坚守。 | 治疗 + 坚守 |
| 农民 | `field-work` | 田间劳作 | Click | SelfCard | 1 AP | No | 1 | 如果已有丰收，本 turn 攻击次数 +1；如果处于播种中，回复自己 2 HP；如果两者都没有，获得播种。 | 特殊循环 |
| 德鲁伊 | `cleansing-herbs` | 净化草药 | TargetSelect | AllyCard | 1 AP | No | 0 | 移除目标 1 个可净化 Debuff，优先移除伤害型 Debuff；成功时目标 HP +1，可突破上限。 | 净化 / 治疗 |
| 德鲁伊 | `weakening-spores-action` | 衰弱孢子 | TargetSelect | EnemyCard | 1 AP | No | 0 | 随机移除目标 1 个可驱散 Buff，并施加 2 turn 力竭和磨损。 | 驱散 + 力竭 + 磨损 |

## 6. Role Action 相关状态

| Status / 显示 | 类型 | 来源 Role Action | 效果 |
| --- | --- | --- | --- |
| 守护誓约 | 特殊 Buff | `guard-oath` | 受到主动物理攻击时，伤害 -2 并消耗 1 层。可叠层，可驱散；驱散时整类移除。 |
| 坚守 | 通用 Buff | `supply-basket` | 受到物理伤害 ×0.5；补给篮赋予 2 层，己方 turn 结束时消耗 1 层。 |
| 战栗 | 通用 Debuff | `challenge` | 无法反击。 |
| 咏唱 | 通用 Buff | `arcane-channel` | 下一次魔法伤害 ×2。 |
| 燃烧 | 通用 Debuff | `searing-brand` | 己方 turn 开始消耗 1 层并受到魔法伤害。 |
| 空虚 | 通用 Debuff | `searing-brand` | 受到魔法伤害 ×1.25。 |
| 力竭 | 通用 Debuff | `weakening-spores-action` | 造成物理伤害 ×0.5。 |
| 磨损 | 通用 Debuff | `weakening-spores-action` | 造成魔法伤害 ×0.5。 |
| 命运标记 | 特殊 Debuff | `fate-mark` | 下一次主动攻击我方时随机修正伤害。 |
| 猎物 | 特殊 Debuff | `predatory-gaze` | 本 turn 0 点 HP 伤害触发绝对伤害。 |
| 黑暗契约 | 特殊 Buff | `dark-pact` | 下一次主动攻击附加 4 点绝对伤害。 |

## 7. 后端实现位置

| 内容 | 文件 |
| --- | --- |
| 角色基础数据与 Trait ID | `Domain/CharacterDefinition.cs` |
| Trait metadata / hook / registry | `Domain/Traits.cs` |
| Role Action metadata / upgrade choices | `Domain/RoleActions.cs` |
| Trait / Role Action 执行分发与效果 | `Domain/GameEngine.cs` |
| Status 生命周期与 modifier | `Domain/StatusEffects.cs` |
| 角色状态、使用记录、冷却记录 | `Domain/GameState.cs` |
| DTO 输出 | `Api/GameDtos.cs` |
| 预测 | `Api/AttackPreviewService.cs` |
| HUD、按钮、目标选择、拖拽抛物线 | `wwwroot/app.js` |
| 显示文本 | `wwwroot/locales/ja.json`, `wwwroot/locales/zh.json` |

## 8. 维护规则

新增或修改 Trait / Role Action 时至少检查：

1. 是否能复用本表通用状态。
2. 是否是 Aura、特殊状态、资源规则或事件伤害，不要强行塞进通用状态。
3. `Domain/Traits.cs` 或 `Domain/RoleActions.cs`：metadata、触发、scope、effect、tags、cost、target、repeatable、cooldown。
4. `Domain/GameEngine.cs` / `Domain/StatusEffects.cs`：执行效果和状态生命周期。
5. `Api/AttackPreviewService.cs`：如果会影响主动攻击、反击、盾、防御、额外伤害或死亡结果，需要同步预测。
6. `wwwroot/app.js`：目标选择、拖拽、按钮可用性、HUD、日志演出。
7. `wwwroot/locales/ja.json` / `zh.json`：名称、按钮短名、描述、日志、状态文本。
8. `reference/TinyPixelFights_Status_Display_Groups.md`：确认显示组是否需要更新。
9. 本表。
