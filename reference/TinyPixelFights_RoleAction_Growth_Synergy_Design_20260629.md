# Tiny Pixel Fights - Role Action / Trait / Hero Growth Synergy Design

更新时间：2026-07-07  
状态：当前机制对齐版。本文替代 2026-06-29 旧升级口径，用于后续英雄 Rank2 / Rank3 成长包设计。

主要参考：

- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_Soldier_Design_20260704.md`

## 1. 当前机制基线

### 1.1 通用属性定义

| 属性 | 当前口径 |
|---|---|
| `Cost` | 主动攻击消耗的 AP。后续仍应走 modifier 管线，不能把基础 Cost 当作最终 Cost。 |
| `Attack` | 基础攻击力。物理 / 魔法攻击共用数值字段，由 `AttackType` 决定伤害类型。 |
| `AttackType` | `Physical` 或 `Magical`。影响防御、通用状态、士兵 Rank1 攻击光环。 |
| `CurrentHp / MaxHp` | 真实 HP。所有写着“HP 伤害”的 Trait / Role Action 只看真实扣 HP。 |
| `Morale / MaxMorale` | 士气，默认 5 / 5。士气是每个角色自己的外接 HP。 |
| `PhysicalDefense / MagicalDefense` | 对应伤害类型的防御。绝对伤害不看防御。 |
| `SharedShield` | 玩家共享盾。普通伤害先扣盾；超过剩余盾值的部分继续经过目标自身防御、士气与 HP 结算。 |
| `BP` | 奖励资源。初始 5，上限 20；每 turn 实际获得上限 5。 |
| `AP` | 每 turn 行动资源，上限 5；王令等效果可以改变当 turn 或下 turn AP。 |
| `Status` | 可驱散 / 可净化的通用 Buff / Debuff 默认进入状态池；Aura 不是普通状态。 |
| `Aura` | 常驻队伍效果，不可驱散。同名 Aura 不叠加，不同 Aura 可并存。 |
| `Deputy` | Rank2 士兵可成为英雄副官。副官不增加按钮，但提供宿主被动与 Rank1 Aura。 |

### 1.2 伤害与士气

- 普通物理 / 魔法伤害在完成攻击、防御、状态、Trait 修正后，最终伤害先扣士气；士气不足时，溢出扣 HP。
- 士气伤害不是 HP 伤害，不触发“造成 HP 伤害时 / 至少 N 点 HP 伤害时”的效果。
- 只打士气、没有扣 HP 时，真实 HP 伤害为 0，会触发“造成 0 点 HP 伤害时”的效果。
- 绝对伤害直扣 HP，不经过士气、共享盾、防御。
- “失去 HP / 支付 HP 代价 / 自伤”直扣 HP，不经过士气。
- 普通治疗只回复 HP，不回复士气；士气通过 BP 结算恢复，不把治疗路线扩展成常规士气回复。
- 己方 turn 结束时，存活角色回复本 turn 实际获得 BP 等量士气，上限 5。

本文不引入额外伤害口径。后续任何新设计若需要判定伤害，都必须明确写 `MoraleDamage`、`HpDamage` 或 `AbsoluteDamage`。

### 1.3 BP、奖励窗口与部署中

- 每 turn 第一次成功使用 Role Action 获得 1 BP，受每 turn BP 获取上限 5 限制。
- 己方 turn 开始低保、造成敌方 HP 伤害、击破敌方共享盾、满盾、防御奖励、跳过奖励等 BP 获取也都受每 turn 上限 5 限制。
- 奖励窗口当前从 round 4 开始，每 4 round 出现一次。
- 第一次英雄 Role Action 解放奖励消耗 0 BP。
- 通过非开局奖励获得的新英雄 / 士兵进入“部署中”：不能主动攻击，不能使用 Role Action，不能被敌我双方攻击、Role Action、Trait、治疗、Buff 指定。由于奖励 round 会被跳过，部署中保护后续双方各 1 个正常 turn；双方各过 1 个正常 turn 后，到下一轮第一个 turn 开始时统一解除。
- 开局士兵选择当前只选择 1 名士兵。

### 1.4 英雄 Rank 结构

| Rank | 规则 |
|---|---|
| Rank0 | 初始阶段：Attack + Trait，Role Action 未解放。 |
| Rank1 | 解放 1 个基础 Role Action。8 名英雄各从 2 个基础方向中选择 1 个。升阶时回复 50% MaxHp。 |
| Rank2 | 属性提升 + 已选路线的 Trait 强化。升阶时回复 50% MaxHp。 |
| Rank3 | 解锁该路线最终 Role Action + 属性提升。终阶设计目标是升阶时 HP 全回复。 |

英雄最多 2 个 Role Action：Rank1 的基础行动 + Rank3 的最终行动。Rank2 不增加按钮，只强化角色身份。

英雄路线在 Rank1 确定：玩家解放哪个基础 Role Action，就进入对应路线。之后 Rank2 的属性 / Trait 强化、Rank3 的最终 Role Action 都沿该路线固定获得，不再进行额外分支选择。若未来想允许 Rank3 二次转向，应作为新的重大成长系统重新评估，而不是混入当前 Rank 规则。

Rank3 Role Action 是终局按钮，数值不应继续停留在固定 +1 / +2 的早期补丁口径。优先使用当前攻击力、当前防御、共享盾值、士气值、状态层数、净化数量、士兵攻击次数等可被成长、Aura、副官和状态一起放大的变量。固定小整数只用于附带状态、基础保底或 Cooldown，不作为主要爽点。

部分 Rank3 可以直接与士气互动：击溃敌方士气、在敌方士气归零时转化为终结伤害，或利用 0 HPDamage 窗口制造反外壳打法。但士气不是每个最终 Role Action 都必须触碰的资源；只有当它能强化该路线身份时才使用。士气伤害不算 HP 伤害；所有写 HPDamage 的触发仍只看真实 HPDamage。

Rank3 数值包络：1 AP 终局行动可以提供一次额外行动、一次当前攻击力级别的追加伤害、或一次关键控制；2 AP 终局行动可以影响主目标及相邻目标，或提供一次队伍级防守/补给。若同时拥有群体、绝对伤害、再行动、BP 奖励、士气操纵，多数情况下必须删掉其中一到两个轴，保留最能表达路线身份的一项。

### 1.5 士兵与副官基线

士兵详细规则以 `TinyPixelFights_Soldier_Design_20260704.md` 为准，本文只列英雄成长需要依赖的构筑信号：

| 士兵 | Rank1 Aura | Rank2 / 副官信号 |
|---|---|---|
| Cleric | 我方全体魔防 +1。 | `mend` 治疗 / 净化；副官在宿主治疗或净化成功后给目标护咒。 |
| Shieldmaiden | 我方全体物防 +1。 | `aegis-formation` 增加共享盾；副官在宿主触发盾 / 坚守链后保护低 HP 友方。 |
| Duelist | 我方物理攻击单位攻击 +2。 | `crimson-lunge` 给我方物理单位猛击；副官让宿主主动攻击造成 HP 伤害后获得强攻，并在攻击后追加绝对伤害。 |
| Arcanist | 我方魔法攻击单位攻击 +2。 | `astral-focus` 给我方魔法单位咏唱；副官让宿主获得魔涌并可再行动一次。 |

Rank2 士兵成为副官后，Rank1 Aura 不消失；Aura 来源视为宿主英雄存活并在战场。

## 2. 通用状态边界

新英雄路线优先复用下列通用状态：

| 状态 | 用途 |
|---|---|
| 强攻 | turn 型物理输出强化，主动物理 / 普通攻击伤害 x1.5。 |
| 猛击 | 次数型物理输出强化，下一次主动物理 / 普通攻击伤害 x2。 |
| 魔涌 | turn 型魔法输出强化，魔法伤害 / 普通攻击伤害 x1.5。 |
| 咏唱 | 次数型魔法输出强化，下一次魔法伤害 x2。 |
| 坚守 | 受到物理伤害 x0.5。 |
| 护咒 | 受到魔法伤害 x0.5。 |
| 脆弱 | 受到物理伤害 x1.25。 |
| 空虚 | 受到魔法伤害 x1.25。 |
| 力竭 | 造成物理伤害 x0.5。 |
| 磨损 | 造成魔法伤害 x0.5。 |
| 燃烧 | 己方 turn 开始消耗 1 层并受到 1 点魔法伤害。 |
| 战栗 | 按 turn 赋予，无法反击。 |

以下保持特殊系统，不强行塞进通用状态：

- 公主的回合开始治疗与祝福。
- 占卜师的星读魔力、预见、命运标记。
- 农民的播种 / 丰收循环。
- 怪物的猎物、黑暗契约、美女与野兽、野兽之怒。
- 骑士的守护、守护誓约、共享盾强化。
- BP、AP、士气、共享盾、奖励成长、部署中。

## 3. 英雄基础总览

当前基础值以 `Domain/CharacterDefinition.cs` 为准：

| Hero | Cost | ATK | HP | 类型 | 物防 | 魔防 | Rank0 Trait | Rank1 二选一 |
|---|---:|---:|---:|---|---:|---:|---|---|
| Princess | 1 | 1 | 12 | Magical | -1 | 1 | `saints-prayer` | `saintly-prayer` / `royal-command` |
| Oracle | 1 | 1 | 14 | Magical | -1 | 2 | `stargazers-aegis` | `star-reading` / `fate-mark` |
| Peasant | 1 | 2 | 16 | Physical | 0 | 0 | `spring-harvest` | `supply-basket` / `field-work` |
| Mage | 2 | 3 | 16 | Magical | -1 | 1 | `searing-mark` | `arcane-channel` / `searing-brand` |
| Druid | 1 | 1 | 16 | Magical | 0 | 1 | `weakening-spores` | `cleansing-herbs` / `weakening-spores-action` |
| Barbarian | 2 | 3 | 18 | Physical | -1 | 0 | `aftershock-axe` | `war-cry` / `challenge` |
| Monster | 3 | 4 | 22 | Physical | 1 | -1 | `predatory-instinct` | `predatory-gaze` / `dark-pact` |
| Knight | 3 | 3 | 24 | Physical | 1 | 0 | `interposing-shield` | `guard-oath` / `raise-bulwark` |

## 4. 英雄详细路线

### 4.1 Princess

Rank0 Trait：`saints-prayer`。己方 turn 开始，治疗我方存活且非部署中角色 1 HP，可超过 MaxHp 2 点。

#### Route A：Saint Queen

定位：治疗、净化、护咒、低速稳场。适合 Cleric / Shieldmaiden。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `saintly-prayer`：1 AP，治疗我方 1 体 2 HP；若目标有可净化 Debuff，净化 1 个并获得 1 AP。 |
| Rank2 | MaxHp +3，MagicalDefense +1。Trait 强化：每己方 turn 第一次 Princess 通过 Role Action 实际回复 HP 或成功净化时，目标获得 1 turn 护咒；回合开始治疗不触发该强化。 |
| Rank3 | MaxHp +4，PhysicalDefense +1，HP 全回复。解锁 `miracle-standard`：2 AP，选择我方 1 体，主目标净化全部可净化 Debuff，并治疗等同 Princess 当前 MaxHp 1/4、向上取整的 HP；相邻我方各净化 1 个 Debuff，并治疗主目标治疗量的一半、向上取整。若本行动成功净化至少 1 个 Debuff，我方共享盾 +Princess 当前 MagicalDefense +本次被影响目标数。Cooldown 1。 |

平衡边界：

- 治疗只修 HP，不回复士气；`miracle-standard` 的额外防守价值放在净化、HP 修复和共享盾上。
- Rank2 强化只看真实 HP 回复或成功净化，不能通过满血空治疗刷触发。
- `miracle-standard` 的盾量只看 Princess 当前魔防和受影响人数，避免按 Debuff 数量无限放大。

#### Route B：War Queen

定位：AP 前借、再行动、斩杀节奏。适合 Duelist / Arcanist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `royal-command`：1 AP，本 turn 获得 +2 AP；下个己方 turn 开始 AP -1。使用后下个己方 turn 不可用。 |
| Rank2 | MaxHp +2，Attack +1，PhysicalDefense +1。Trait 强化：`saints-prayer` 的回合开始治疗结束后，额外治疗当前 HP 比例最低的我方非部署中角色 1 HP，仍最多超过 MaxHp 2 点。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `edict-of-victory`：1 AP，选择我方 1 体，使其本 turn 可再次主动攻击；本 turn 下一次主动攻击附加 Princess 当前 Attack 点绝对伤害。若该目标本 turn 击败敌方角色，我方获得 1 BP 并清除这次王令产生的 AP debt；否则下个己方 turn AP debt +1。Cooldown 1。 |

平衡边界：

- `edict-of-victory` 的伤害吃 Princess 当前 Attack，因此会被魔法攻击 Aura 和副官成长放大。
- BP / 免 debt 奖励只看击败，不因只打士气触发。
- 与 `star-reading` 同队时需要在实现里检查同一角色额外攻击次数的上限 UI 是否清楚。

### 4.2 Oracle

Rank0 Trait：`stargazers-aegis`。队伍获得预见系减伤；Oracle 存活时，我方魔法攻击和燃烧伤害获得星读魔力 +1。

#### Route A：Astral Oracle

定位：魔法队再行动、咏唱兑现、燃烧放大。适合 Mage / Arcanist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `star-reading`：1 AP，选择已经主动攻击过的我方魔法单位，使其本 turn 可以再次主动攻击。 |
| Rank2 | MaxHp +2，MagicalDefense +1。Trait 强化：每 round 第一次预见成功降低伤害后，受保护角色获得 1 turn 护咒；若该角色是魔法攻击单位，改为获得 1 层咏唱。 |
| Rank3 | MaxHp +2，Attack +1，HP 全回复。解锁 `astral-alignment`：1 AP，选择我方魔法单位，获得 1 层咏唱和 1 次额外主动攻击机会。本 turn 该目标下一次消耗咏唱造成魔法伤害时，额外对同一目标造成 Oracle 当前 Attack 点魔法伤害，并使相邻敌人受到一半数值、向上取整的魔法伤害。Cooldown 1。 |

平衡边界：

- 星链追加伤害是普通魔法伤害，先扣士气；咏唱不回复士气，不改变 HPDamage 判定。
- 对 Arcanist 副官的“魔涌 + 再行动”有强协同，Rank3 实装时需要同 turn 额外攻击次数展示清楚。

#### Route B：Fate Dealer

定位：命运标记、敌方输出不确定性、反打窗口。适合 Duelist / Druid。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `fate-mark`：1 AP，标记敌方 1 体。其下一次主动攻击我方时，50% 伤害减半，50% 伤害 +1。 |
| Rank2 | MaxHp +2，Attack +1。Trait 强化：每己方 turn 第一次敌方 `marked` 被消耗后，若该次攻击造成 0 点 HP 伤害，攻击者获得 1 turn 战栗。 |
| Rank3 | MaxHp +3，MagicalDefense +1，HP 全回复。解锁 `thread-cut`：1 AP，选择敌方 1 体。统计目标身上的特殊印记与通用 Debuff 数量，每个可计数状态造成 2 点士气伤害，不设数量上限；士气伤害溢出时穿透士气并扣除 HP。若目标士气因此归 0，再造成 Oracle 当前 Attack 点魔法伤害并施加 1 turn 战栗。若目标没有任何可计数状态，改为只施加 `marked`。Cooldown 1。 |

平衡边界：

- `thread-cut` 是命运路线的士气互动：先扣士气，超过当前士气的部分直接扣 HP；目标士气归 0 后再接魔法伤害。
- Rank2 战栗只在真实 HPDamage 为 0 时触发；只打士气也算 0 HP 伤害。
- 不把命运标记改成通用 Debuff；它仍是特殊印记。

### 4.3 Peasant

Rank0 Trait：`spring-harvest`。可播种的 turn 内，Peasant 是本 turn 第一个发起主动攻击的角色时播种；下个己方 turn 获得丰收，攻击 +2。

#### Route A：Quartermaster

定位：低 AP 补给、坚守、队伍 HP 修复。适合 Shieldmaiden / Cleric。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `supply-basket`：0 AP，治疗我方 1 体 1 HP，并赋予 2 turn 坚守。 |
| Rank2 | MaxHp +4，PhysicalDefense +1。Trait 强化：丰收激活时，当前 HP 比例最低的我方非部署中角色获得 1 turn 坚守。 |
| Rank3 | MaxHp +4，MagicalDefense +1，HP 全回复。解锁 `field-rations`：1 AP，治疗所有我方非部署中角色等同 Peasant 当前 Attack 一半、向上取整的 HP；选择 1 个主目标额外治疗 Peasant 当前 Attack 点 HP。若主目标使用前 HP 低于一半，额外净化 1 个可净化 Debuff；主目标获得 1 turn 坚守和护咒。Cooldown 1。 |

平衡边界：

- `supply-basket` 和 `field-rations` 都只治疗 HP，不回复士气，避免后勤路线把士气外壳也一起补满造成拖局。
- `field-rations` 的治疗量吃 Peasant 当前 Attack，因此能被 Duelist Aura 或奖励攻击成长放大。
- 不直接给 BP，避免低费后勤变成资源发动机。

#### Route B：Militia Foreman

定位：播种 / 丰收节奏、士兵再行动、低费进攻队。适合 Duelist / Arcanist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `field-work`：1 AP。丰收中可再攻击一次；播种中回复自己 2 HP；否则获得播种。使用后下个己方 turn 不可用。 |
| Rank2 | MaxHp +3，Attack +1。Trait 强化：Peasant 的丰收攻击第一次造成敌方 HP 伤害时，我方获得 1 BP，受每 turn BP 上限限制。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `militia-call`：1 AP，选择我方 Peasant 或 Soldier，目标获得强攻或魔涌，按其攻击类型决定，并可再次主动攻击。该目标本 turn 下一次主动攻击额外附加 Peasant 当前 Attack 点同类型普通伤害；若目标是 Soldier，额外伤害再 +目标 SoldierRank。若目标本 turn 击败敌方角色，我方获得 1 BP。Cooldown 1。 |

平衡边界：

- Rank2 / Rank3 BP 都只看真实 HPDamage 或击败，不能靠士气伤害刷；`militia-call` 额外伤害是普通伤害，先扣士气。
- `militia-call` 不能指定部署中角色，不能指定已经成为副官的士兵。
- 与 Duelist Rank1 Aura 结合时输出很高，但低 HP 士兵仍容易被反制。

### 4.4 Mage

Rank0 Trait：`searing-mark`。主动攻击结算后，若目标仍存活，50% 赋予 1 层燃烧。

#### Route A：Stellar Archmage

定位：咏唱蓄力、单点爆发、燃烧收尾。适合 Arcanist / Oracle。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `arcane-channel`：2 AP，本 turn 不能再攻击；下个己方 turn 获得 2 层咏唱。 |
| Rank2 | MaxHp +2，Attack +1。Trait 强化：Mage 消耗咏唱造成魔法主动攻击后，若目标仍存活，必定赋予 1 层燃烧，不再掷 50%。 |
| Rank3 | MaxHp +2，Attack +1，HP 全回复。解锁 `starfall`：1 AP，选择敌方 1 体，造成当前攻击力点魔法伤害并赋予 1 层燃烧；若 Mage 当前有咏唱，按咏唱规则消耗 1 层并使本次魔法伤害翻倍。Cooldown 1。 |

平衡边界：

- `starfall` 是普通魔法伤害，不是绝对伤害；它的爽点来自当前攻击力结算和咏唱兑现，不额外操纵士气。
- 咏唱消耗后才放大，不应和魔涌 / 星读魔力在 UI 里隐藏叠加。
- Rank2 强化只改 Mage 自己的主动攻击，不改燃烧 tick 的触发概率。

#### Route B：Arcane Archivist

定位：刻印、空虚、Role Action 连锁。适合 Druid / Arcanist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `searing-brand`：1 AP，敌方 1 体获得 1 层燃烧和 1 turn 空虚。 |
| Rank2 | MaxHp +2，MagicalDefense +1。Trait 强化：`searing-mark` 若对已经拥有燃烧或空虚的目标发动，额外延长空虚 1 turn；没有空虚时赋予 1 turn 空虚。每己方 turn 1 次。 |
| Rank3 | MaxHp +2，Attack +1，HP 全回复。解锁 `archive-formula`：1 AP，选择敌方 1 体，目标获得 `archived`。本 turn 下一次我方 Role Action 对其造成伤害或施加 Debuff 后，额外造成 Mage 当前 Attack 点魔法伤害；若该 Role Action 本身造成了魔法伤害，额外伤害改为 Mage 当前 Attack +目标燃烧层数，并赋予等同目标当前通用 Debuff 数量、至少 1 层的燃烧。Cooldown 1。 |

平衡边界：

- `archived` 是特殊印记，不进入通用 Debuff 命名池；可以被净化。
- 额外魔法伤害是普通伤害，先扣士气；伤害与燃烧层数都会随 Mage 当前 Attack 和状态数量放大。
- 该路线给控制与连锁，不给再行动，避免与 Route A 抢同一爆发身份。

### 4.5 Druid

Rank0 Trait：`weakening-spores`。主动攻击后目标存活时，若造成 HP 伤害则 100% 发动；若造成 0 点 HP 伤害则 50% 发动。发动后移除目标 1 个可驱散 Buff，并赋予 2 turn 力竭和磨损。

#### Route A：Grove Keeper

定位：净化、反状态、护咒补血。适合 Cleric / Shieldmaiden。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `cleansing-herbs`：1 AP，移除我方 1 体的 1 个可净化 Debuff，优先移除伤害型 Debuff；成功时治疗 1 HP，可突破上限。 |
| Rank2 | MaxHp +3，MagicalDefense +1。Trait 强化：每己方 turn 第一次 Druid 成功净化 Debuff 后，目标获得 1 turn 护咒；若目标当前 HP 低于一半，额外治疗 1 HP。 |
| Rank3 | MaxHp +4，PhysicalDefense +1，HP 全回复。解锁 `grove-sanctuary`：2 AP，选择我方 1 体及相邻我方。主目标净化全部可净化 Debuff；相邻我方各净化 1 个 Debuff。每净化 1 个 Debuff，所有被影响目标治疗 Druid 当前 Attack 点 HP；若没有净化任何 Debuff，改为所有被影响目标获得 Druid 当前 MagicalDefense 层护咒。Cooldown 1。 |

平衡边界：

- 净化成功可以触发 Cleric 体系；治疗量随 Druid 当前 Attack 和净化数量放大。
- 治疗不回复士气，不改变部署中限制。
- 该路线克制状态，但主动伤害弱。

#### Route B：Wildspeaker

定位：驱散、力竭 / 磨损、猎群集火。适合 Duelist / Arcanist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `weakening-spores-action`：1 AP，移除敌方 1 体的 1 个可驱散 Buff，并施加 2 turn 力竭和磨损。 |
| Rank2 | MaxHp +2，Attack +1。Trait 强化：`weakening-spores` 通过主动攻击发动时，若本次没有移除 Buff，则额外施加 1 turn 脆弱或空虚，按目标攻击类型选择其较脆弱的一侧；每己方 turn 1 次。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `call-the-hunt`：1 AP，选择敌方 1 体及相邻敌人，赋予 `hunted`。本 turn 我方 Soldier 对 `hunted` 目标造成普通伤害时，最终伤害 +Druid 当前 Attack；每名 Soldier 各限 1 次。若任一 `hunted` 目标本 turn 被击败，我方获得 1 BP。Cooldown 1。 |

平衡边界：

- `call-the-hunt` 加的是普通伤害，仍先扣士气；强度随 Druid 当前 Attack 和参战 Soldier 数量放大。
- BP 只因击败获得一次，不能按士兵攻击次数重复刷。
- `hunted` 是特殊印记，不等同 `marked` 或通用 Debuff。

### 4.6 Barbarian

Rank0 Trait：`aftershock-axe`。主动攻击对目标造成至少 3 点 HP 伤害时，对目标相邻敌人造成已造成 HP 伤害 1/3、向上取整的物理伤害。

#### Route A：Radiant Berserker

定位：战吼、破盾、HP 伤害阈值。适合 Shieldmaiden / Cleric。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `war-cry`：1 AP，自身获得狂怒：物防 / 魔防 -2，攻击次数 +1；击破共有盾时，对攻击对象造成当前攻击力的绝对伤害。 |
| Rank2 | MaxHp +2，Attack +1。Trait 强化：Barbarian 处于狂怒时触发 `aftershock-axe` 不再要求至少 3 点真实 HP 伤害，余波伤害最低为 2。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `glory-roar`：1 AP，自身获得强攻 1 turn，并获得额外主动攻击次数，次数等同 Barbarian 当前 Attack / 3、向上取整。本 turn Barbarian 的 `aftershock-axe` 触发阈值降为造成任意 HP 伤害，余波伤害改为已造成 HP 伤害的一半、向上取整。每次主动攻击后自身失去 1 HP。Cooldown 1。 |

平衡边界：

- 失去 HP 直扣 HP，不经过士气；额外攻击次数随 Barbarian 当前 Attack 放大。
- `war-cry` 破盾追加是独立的绝对伤害，直扣 HP；普通攻击超过剩余共享盾的部分仍按通用穿透规则继续结算。
- Rank2 余波门槛严格看真实 HPDamage，只打士气不触发。

#### Route B：Dragon Raider

定位：战栗、破阵、让队友安全输出。适合 Duelist / Knight。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `challenge`：1 AP，敌方 1 体获得 1 turn 战栗，无法反击。 |
| Rank2 | MaxHp +3，PhysicalDefense +1。Trait 强化：`aftershock-axe` 触发时，相邻敌人除受到余波外还获得 1 turn 战栗。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `dragon-breaker`：1 AP，选择敌方 1 体。若敌方有共享盾，对其共享盾造成 Barbarian 当前 Attack 点伤害但不穿透 HP；若本次击破共享盾，目标及相邻敌人获得 1 turn 战栗和 1 turn 脆弱。若敌方没有共享盾，则对目标造成 Barbarian 当前 Attack 点物理伤害，并施加 1 turn 战栗和脆弱。Cooldown 1。 |

平衡边界：

- `dragon-breaker` 的盾伤害不造成 HP 伤害，不能触发 HPDamage 奖励；无盾模式是普通物理伤害，先扣士气。
- 该路线给队友开路，不应比 Route A 更擅长自己斩杀。
- 与 Duelist Aura 叠加时主要强在安全物理窗口。

### 4.7 Monster

Rank0 Trait：`predatory-instinct`。对非公主目标主动攻击造成 0 点 HP 伤害时，追击当前怪物攻击力点绝对伤害；若我方公主存活，追击 +1。攻击公主时造成当前怪物攻击力点绝对伤害，攻击后怪物受到造成伤害 2 倍的自伤但不会因此死亡。任意公主死亡后，怪物获得野兽之怒：攻击 +3，物防 / 魔防 -2。

#### Route A：Nightmare Fiend

定位：猎物、0 HP 伤害触发、反士气外壳。适合 Druid / Duelist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `predatory-gaze`：1 AP，敌方 1 体获得猎物；本 turn 每次受到 0 点 HP 伤害时，追加 2 点绝对伤害。 |
| Rank2 | MaxHp +3，MagicalDefense +1。Trait 强化：`predatory-instinct` 对拥有猎物的目标触发时，追加绝对伤害 +1；攻击公主不享受该 +1。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `nightmare-stare`：1 AP，选择敌方 1 体，目标和相邻敌人获得强化猎物：本 turn 第一次受到 0 点 HP 伤害时，追加 Monster 当前 Attack 点绝对伤害。主目标额外获得 1 turn 磨损。Cooldown 1。 |

平衡边界：

- 猎物触发看真实 HPDamage 为 0；只打士气会触发。
- 强化猎物追加是绝对伤害，直扣 HP；数值随 Monster 当前 Attack 放大。它通过 0 HPDamage 自然利用士气系统，但不直接削士气。
- 群体猎物很强，Rank3 只给主目标附带磨损。

#### Route B：Abyssal Queen

定位：黑暗契约、HP 代价、绝对伤害斩杀。适合 Cleric / Princess。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `dark-pact`：1 AP，我方 1 体失去最多 4 HP，至少保留 1 HP；获得契约：下一次主动攻击附加 4 点绝对伤害。若目标 HP 低于一半，我方获得 1 BP。 |
| Rank2 | MaxHp +4，Attack +1。Trait 强化：每己方 turn 第一次我方因 `sacrifice` 失去 HP 后，Monster 治疗 2 HP；若 Monster 自己是失去 HP 的角色，改为获得 1 turn 护咒。 |
| Rank3 | MaxHp +4，PhysicalDefense +1，HP 全回复。解锁 `abyssal-bargain`：1 AP，选择我方 1 体，目标失去最多 Monster 当前 Attack 点 HP，至少保留 1 HP；目标获得深渊契约：下一次主动攻击附加 Monster 当前 Attack 点绝对伤害，并可再次主动攻击。若目标本 turn 击败敌方角色，我方获得 1 BP，Monster 治疗等同本次 HP 代价的 HP。Cooldown 1。 |

平衡边界：

- 深渊契约附加绝对伤害直扣 HP，数值随 Monster 当前 Attack 放大。
- HP 代价不能被士气吸收，不能指定部署中角色。
- Rank2 自我修复每 turn 1 次，避免献祭循环无限拖局。

### 4.8 Knight

Rank0 Trait：`interposing-shield`。全队共享一次。骑士以外我方角色受到主动物理攻击且将受到 1 点及以上伤害时，骑士承担目标受到伤害的 1/3、向上取整；承担伤害再受骑士自身物防影响。

#### Route A：Holy Paladin

定位：守护誓约、单点保护、反物理防线。适合 Shieldmaiden / Cleric。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `guard-oath`：1 AP，自己以外的我方角色获得 1 层守护誓约；受到主动物理攻击时伤害 -2 并消耗 1 层。 |
| Rank2 | MaxHp +4，MagicalDefense +1。Trait 强化：`interposing-shield` 触发后，被保护目标获得 1 turn 坚守；若目标已有守护誓约，骑士自身获得 1 turn 护咒。 |
| Rank3 | MaxHp +4，PhysicalDefense +1，HP 全回复。解锁 `holy-bastion`：2 AP，选择我方 1 体，目标获得等同 Knight 当前 PhysicalDefense、至少 2 层的守护誓约，并获得 1 turn 护咒；Knight 获得 1 turn 坚守。我方共享盾增加 Knight 当前 PhysicalDefense + MagicalDefense 的总和。Cooldown 1。 |

平衡边界：

- 守护誓约仍只对主动物理攻击生效。
- 该路线强防守但不直接处理士气；需要队友输出。
- 共享盾只能吸收当前盾值，溢出伤害会穿透；`holy-bastion` 的盾量随 Knight 当前双防放大。

#### Route B：Dread Cavalier

定位：共享盾转进攻、破盾反打、骑士输出路线。适合 Barbarian / Duelist。

| Rank | 内容 |
|---|---|
| Rank1 | 解放 `raise-bulwark`：1 AP，我方拥有共享盾时可发动；共享盾 x1.5 向上取整，共享盾物防 +2，持续到下次己方 turn 开始。 |
| Rank2 | MaxHp +3，Attack +1。Trait 强化：每己方 turn 第一次我方共享盾被击破后，Knight 获得 1 turn 强攻；若 Knight 已行动，改为下个己方 turn 开始获得强攻。 |
| Rank3 | MaxHp +3，Attack +1，HP 全回复。解锁 `iron-charge`：1 AP，消耗我方全部共享盾，对敌方 1 体造成“消耗的共享盾值 + Knight 当前 Attack”的物理伤害，并施加 1 turn 战栗；若本次造成 HP 伤害，我方重新获得等同 HPDamage 的共享盾。若没有共享盾则不能发动。Cooldown 1。 |

平衡边界：

- `iron-charge` 是普通物理伤害，先扣目标士气；伤害随盾量和 Knight 当前 Attack 放大。
- 消耗共享盾是主动风险，不能同时享受“盾被敌人击破”的 Rank2 触发。
- 该路线牺牲防守换节奏，不应再获得额外共享盾。

## 5. 队伍协同参考

| 核心路线 | 推荐士兵 / 副官 | 构筑重点 |
|---|---|---|
| Saint Queen | Cleric + Shieldmaiden | 治疗、净化、护咒、坚守，拖到 Rank3 群体修复。 |
| War Queen | Duelist + Arcanist | AP 前借 + 再行动，物理或魔法核心吃 Aura 后斩杀。 |
| Astral Oracle | Arcanist + Mage | 咏唱、魔法再行动、燃烧 tick 受星读魔力放大。 |
| Fate Dealer | Duelist + Druid | 标记 / Debuff 让物理收割安全进场。 |
| Quartermaster | Shieldmaiden + Cleric | 0 AP 补给触发坚守 / 护咒链，稳住低费队。 |
| Militia Foreman | Duelist + Shieldmaiden | 低费士兵多次攻击，靠坚守和战栗降低反击风险。 |
| Stellar Archmage | Arcanist + Oracle | 咏唱爆发，士气先承伤后需要真实 HPDamage 才触发 BP。 |
| Arcane Archivist | Arcanist + Druid | 空虚、燃烧、力竭 / 磨损形成状态工具箱。 |
| Grove Keeper | Cleric + Shieldmaiden | 反状态、防御光环、长线续航。 |
| Wildspeaker | Duelist + Arcanist | 驱散 + Debuff + 士兵集火。 |
| Radiant Berserker | Shieldmaiden + Cleric | 战吼自降防，由坚守 / 护咒补风险。 |
| Dragon Raider | Duelist + Knight | 战栗开路，脆弱与物理 Aura 放大收割。 |
| Nightmare Fiend | Druid + Duelist | 猎物利用 0 HPDamage，绝对伤害越过士气。 |
| Abyssal Queen | Cleric + Princess | 献祭直扣 HP，靠治疗修复代价。 |
| Holy Paladin | Shieldmaiden + Cleric | 守护誓约 + 共享盾 + 防御 Aura。 |
| Dread Cavalier | Duelist + Barbarian | 盾转进攻，破阵后物理斩杀。 |

## 6. 实装检查点

1. Rank2 / Rank3 成长包必须走数据化奖励或稳定 modifier，不要在攻击、预测、UI 三处分别硬写。
2. 任何写“造成 HP 伤害”的效果，只检查真实 HPDamage。
3. MoraleDamage 不触发 HPDamage 相关 Trait / Role Action，也不阻止 0 HPDamage 触发。
4. 绝对伤害直扣 HP，不经过士气、共享盾、防御。
5. 普通治疗只回 HP；士气通过 turn 结束的 BP 恢复，不把治疗、净化或补给效果改写成士气回复。
6. Role Action 成功使用后的首次 +1 BP 由全局规则处理，单个 Role Action 不需要重复写。
7. BP 奖励都受每 turn 获取上限 5 限制。
8. 共享盾破碎时，溢出伤害继续经过目标自身防御、士气与 HP；破盾奖励与穿透伤害在同一次伤害包中都可能成立。
9. 部署中角色不能被敌我双方目标选择，也不能被 Aura 以外的治疗 / Buff / Trait 指定。
10. Rank1 士兵 Aura 在副官状态继续生效；同名 Aura 不叠加。
11. 预测 UI 需要同时展示士气与 HP 落点，尤其是 0 HPDamage 触发、绝对伤害与共有盾溢出穿透。
12. 本地化文本必须分别维护中文 / 日文，不要跨语言擅自润色。

## 7. 设计结论

新版成长的核心不是“每升一级多一个更大的数字”，而是让 Rank1 的路线选择在 Rank2 获得可见身份强化，并在 Rank3 解锁第二个按钮形成终局打法：

- Princess 在治疗稳场和 AP 指挥之间分化。
- Oracle 在魔法再行动和命运反制之间分化。
- Peasant 在补给防线和民兵节奏之间分化。
- Mage 在咏唱爆发和刻印连锁之间分化。
- Druid 在净化防守和猎群 Debuff 之间分化。
- Barbarian 在自我爆发和破阵控制之间分化。
- Monster 在猎物绝对伤害和献祭契约之间分化。
- Knight 在圣盾防守和盾转进攻之间分化。

后续实装顺序建议先做 Rank2 数据包与 UI 文案，再做 Rank3 第二 Role Action。Rank2 不增加按钮，风险低；Rank3 涉及预测、动画、日志、AI 提示与在线同步，应按英雄路线逐个落地。
