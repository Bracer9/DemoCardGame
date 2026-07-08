# Tiny Pixel Fights — Relic / Build Support Design

更新时间：2026-07-08  
状态：设计稿 + 临时接入。目标是把奖励阶段中的临时占位奖励扩展为“帮助 build 成型”的遗物池。

主要参考：

- `reference/TinyPixelFights_RoleAction_Growth_Synergy_Design_20260629.md`
- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_Soldier_Design_20260704.md`
- 当前实现：`Domain/RewardDefinitions.cs`、`Domain/RelicEffects.cs`、`Domain/RoleActions.cs`、`Domain/Traits.cs`

## 1. 设计目标

遗物不是第三套角色成长树，而是奖励阶段里帮助玩家把“已经露出苗头的 build”推成完整打法的构筑零件。

核心目标：

1. 让玩家在 round 4 之后能看见方向，而不是只拿泛用数值。
2. 让 Rank1 路线、士兵 Aura、副官、Rank3 终局行动之间形成明确联动。
3. 尽量复用当前真实状态：强攻、咏唱、坚守、护咒、燃烧、空虚、脆弱、力竭、磨损、战栗、猎物、共享盾、BP、AP、士气。
4. 不引入“有效伤害”概念。所有触发文本必须明确写 HP 伤害、士气伤害、绝对伤害或共享盾伤害。
5. 遗物强度服务 build，不把所有队伍都推向同一个泛用最优解。
6. 玩家可见效果文本要像 STS 遗物一样短：一句话说明触发条件和收益。细节、边界和实现说明放到“注意”或“实装落点”，不要塞进效果文本。

## 2. 当前奖励占位的替换方向

当前 `RewardKind.DummyStatus` 已经能把全队临时奖励状态挂到在场角色上：

| 当前 ID | 当前效果 | 建议遗物名 | 定位 |
|---|---|---|---|
| `dummy-reward-a` | 全队魔防 +1 | `relic-silver-ward-charm` 银护符 | 抗魔 / 治疗防线 / 对抗燃烧魔法队 |
| `dummy-reward-b` | 魔法攻击单位攻击 +1 | `relic-apprentice-star-ink` 见习星墨 | 魔法 / 咏唱 / 燃烧 build 起点 |
| `dummy-reward-c` | 全队攻击 +1 | `relic-war-council-banner` 军议战旗 | 泛用终局火力，价格应偏高 |
| 未接入状态 | 全队物防 +1 | `relic-black-iron-rivets` 黑铁铆钉 | 物防 / 盾墙 / 抗物理爆发 |

这四个可以作为第一批“静态数值遗物”，用于替换 dummy 奖励名和文案。后续 build 遗物应尽量少用永久 +1，而是提供触发、窗口、资源回收或状态组合。

当前临时接入（2026-07-08）：

- `dummy-reward-a/b/c` 购买后写入 `PlayerState.Relics`，由 `RelicEffects` 提供队伍级静态数值修正；它们不再作为 buff / debuff / status 挂到角色身上。
- 前端暂时把已购买遗物显示在画面左下的 `relic-overview` 独立 HUD 入口里，不再塞进角色 inspector，避免污染角色状态说明。
- PC：hover 遗物总览图标向上展开全部遗物名称与效果，移开收起。触屏设备：点击图标展开 / 收起，点击外部收起。
- 当前临时图标复用已有 UI PNG：`dummy-reward-a` -> `status.spell-ward`，`dummy-reward-b` -> `status.chant`，`dummy-reward-c` -> `status.strong-attack`。后续正式遗物资源到位后再替换为 `relic.*` 图标映射。

## 3. 阶段定义

遗物按奖励阶段分三层池。阶段不是硬锁，也可以用权重控制出现率。

| 阶段 | 推荐出现时机 | 推荐成本 | 设计职责 |
|---|---:|---:|---|
| Stage I：成型信号 | round 4 起 | 2-4 BP | 给玩家一个方向：魔法、物理、盾、治疗、士兵、献祭、Debuff。 |
| Stage II：引擎补件 | round 8 起 | 4-6 BP | 把方向变成循环：状态触发、BP 回收、AP 节奏、共享盾链。 |
| Stage III：终局放大器 | round 12 起，或队伍已有 Rank2/Rank3 | 6-9 BP | 配合 Rank3 与多角色协同，提供强烈但有条件的爽点。 |

出现建议：

- Stage I 遗物可以与英雄强化、士兵招募并列出现。
- Stage II 以后，若队伍已有对应标签角色或状态来源，提高相关遗物权重。
- Stage III 遗物若完全无对应 build，应降低出现率，避免玩家看到“很强但用不了”的死选项。

## 4. 当前 build 缺口

| Build | 已有支点 | 当前缺口 |
|---|---|---|
| 圣疗 / 防线 | Princess 祈祷路线、Cleric、Shieldmaiden、Knight | 治疗只修 HP，不影响士气；需要通过共享盾、护咒、BP 间接形成稳定防线。 |
| 盾墙 / 守护 | Knight、Shieldmaiden、共享盾、坚守、守护誓约 | 容易只变成拖延；需要破盾后反击、盾量转化或 AP/BP 回报。 |
| 魔法 / 咏唱 / 燃烧 | Mage、Oracle、Arcanist、咏唱、燃烧、空虚、星读魔力 | 前期缺稳定起手；燃烧本体伤害低且受魔防影响，缺少把层数一次性兑现的结算机制；中期需要“只打士气也有推进感”的状态收益。 |
| Debuff / 控制 | Druid、Mage 刻印路线、Duelist、Barbarian 挑衅 | 状态很多，但缺一个把多种 Debuff 串成胜利条件的引擎。 |
| 物理节奏 / 再行动 | War Queen、Barbarian、Duelist、Peasant 民兵 | 爆发窗口强，但需要避免只看 +Attack；应围绕无盾目标、战栗、AP debt、额外攻击。 |
| 士兵团 / 民兵 | Peasant、四类士兵 Rank1 Aura、Rank2 Role Action / 副官 | 士兵数量本身还缺奖励回报；需要让“招募士兵”成为 build，而不是只当副官素材。 |
| 猎物 / 绝对伤害 / 献祭 | Monster、Predatory Gaze、Dark Pact、Nightmare、Abyssal Bargain | 绝对伤害绕过士气很强，遗物必须给条件和节奏，不宜直接无脑加伤害。 |
| 士气 / BP 指挥 | BP 上限、每 turn 首次 Role Action +1 BP、士气随 BP 回合结束恢复 | 士气已经是外接 HP；遗物应通过 BP 获取、士气压力或共享盾联动，不直接治疗士气。 |

## 5. 遗物总表

### 5.1 静态数值遗物

这些用于替换当前 dummy 奖励，可以最早落地。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 实装落点 |
|---|---:|---:|---|---|---|
| `relic-silver-ward-charm` | I | 3 | common | 我方全体魔防 +1。 | 队伍级 `RelicEffects.ModifyMagicalDefense`。 |
| `relic-black-iron-rivets` | I | 3 | common | 我方全体物防 +1。 | 队伍级 `RelicEffects.ModifyPhysicalDefense`。 |
| `relic-apprentice-star-ink` | I | 4 | rare | 我方魔法攻击单位攻击 +1。 | 队伍级 `RelicEffects.ModifyBaseAttack`，限定魔法攻击单位。 |
| `relic-war-council-banner` | III | 7 | epic | 我方全体攻击 +1。 | 队伍级 `RelicEffects.ModifyBaseAttack`。 |

平衡备注：

- `war-council-banner` 不应过早出现。全队攻击 +1 会同时放大普通攻击、Rank3 当前攻击力系行动、绝对伤害取值和部分士兵 Aura。
- 物防 / 魔防 +1 是良性的 early relic，可以帮助慢速路线活到 Rank2。

### 5.2 圣疗 / 防线遗物

目标：让治疗、净化、护咒不只是“苟住”，而是能转换成共享盾、BP 和反打窗口。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-white-lily-censer` | I | 3 | common | 每回合首次主动治疗 HP 时，目标获得护咒。 | Princess 祈祷、Cleric、Peasant 补给 | 新增 team relic hook：`OnRoleActionHealResolved`，赋予 `SpellWardStatus`。 |
| `relic-cleanse-votive` | II | 5 | rare | 每回合首次成功净化时，获得 1 BP。 | Saint Queen、Druid 净化、Cleric | `TryGainBp(..., reason: relic-cleanse)`；只看成功移除 Debuff，受 BP 获取上限限制。 |
| `relic-oath-keystone` | III | 7 | epic | 每回合第 3 次获得坚守或护咒时，共享盾 +3。 | 防线、净化、盾墙 | 监听 `FortifyStatus` / `SpellWardStatus` 应用；队伍级每 turn 计数。 |

注意：

- 这些遗物不直接回复士气。它们通过 BP 与共享盾提高生存，不破坏“士气只通过 BP 回合结束恢复”的当前口径。
- `white-lily-censer` 只响应 Role Action，不响应 Princess 回合开始治疗，避免免费治疗光环过强。

### 5.3 盾墙 / 守护遗物

目标：让共享盾 build 不只是拖时间，而是能制造破盾惩罚和保护节奏。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-mason-token` | I | 3 | common | 每回合首次部署或强化共享盾时，低 HP 友方获得坚守。 | Knight、Shieldmaiden、Saint Queen | 复用低血选择逻辑，赋予 `FortifyStatus`。 |
| `relic-cracked-shield-bell` | II | 5 | rare | 每 round 1 次，共享盾被击破后，攻击者获得战栗。 | 盾墙、Raise Bulwark、Aegis Formation | 破盾事件 hook；赋予 `TremblingStatus`。 |
| `relic-kingwall-standard` | III | 8 | epic | 回合开始时，若共享盾为 0，获得共享盾 2。 | 盾墙、慢速防线 | 队伍级遗物；不触发“部署共享盾”奖励，避免 BP 循环。 |

注意：

- `cracked-shield-bell` 不改变“共享盾破碎不穿透”的核心规则，只增加破盾后的反击窗口。
- `kingwall-standard` 只能给基础盾，不应吃 `raise-bulwark` 的立即倍率，除非玩家之后主动消耗 AP 继续强化。

### 5.4 魔法 / 咏唱 / 燃烧遗物

目标：让 Mage / Oracle / Arcanist 的魔法链有早期入口、中期士气推进、终局爆点。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-apprentice-star-ink` | I | 4 | rare | 我方魔法攻击单位攻击 +1。 | Mage、Oracle、Druid、Arcanist | 复用 `RewardMagicalAttackStatus`。 |
| `relic-ember-astrolabe` | II | 5 | rare | 每回合首次赋予燃烧时，额外 +1 层。 | Searing Mark、Searing Brand、Starfall | 在 `AddBurning` 或状态应用后追加 `AddStacks(1)`。 |
| `relic-ashen-detonator` | II | 6 | rare | 每回合 1 次，命中 3 层以上燃烧时，引爆燃烧。 | 燃烧、咏唱、魔法连击 | 消耗全部燃烧，造成等同层数的燃爆魔法伤害；不受魔防，仍先扣士气。 |
| `relic-hollow-comet-lens` | III | 8 | epic | 每回合 1 次，魔法伤害只击伤士气时，目标获得空虚。 | Chant、Starfall、Astral Alignment、Archive Formula | Damage resolved hook；检查 `MoraleDamage > 0 && HpDamage == 0 && DamageType.Magical`，赋予 `VoidStatus`。 |

注意：

- `hollow-comet-lens` 让魔法队“剥士气”后仍有推进感，但不把士气伤害伪装成 HP 伤害。
- `ember-astrolabe` 会放大 Oracle 的星读魔力和燃烧伤害，需要限制每己方 turn 1 次。
- `ashen-detonator` 是燃烧 build 的层数兑现件。3 层阈值避免刚挂 1-2 层就被自动兑掉；它消耗燃烧，所以玩家需要在“继续堆层”与“现在引爆”之间取舍。燃爆不是绝对伤害，不绕过士气。

### 5.5 Debuff / 控制遗物

目标：把 Druid、Mage 刻印、Duelist、Barbarian 挑衅这几条控制线接起来。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-witch-bell` | I | 3 | common | 每回合首次赋予敌方 Debuff 时，追加战栗。 | Druid、Searing Brand、Challenge、Duelist | 状态应用 hook；通用 Debuff 指 `void/vulnerable/exhaustion/erosion/burning/trembling`。 |
| `relic-spore-press` | II | 5 | rare | 每回合首次驱散敌方 Buff 时，获得 1 BP。 | Druid 孢子路线、Cleansing Herbs | 只看敌方 Buff 被移除，不看净化友方 Debuff；受 BP 获取上限限制。 |
| `relic-plague-codex` | III | 8 | epic | 每回合 1 次，敌方同时拥有两类破绽时，受到 2 点士气伤害。 | Weakening Spores + Vulnerable/Void、Crimson Lunge | 两类破绽指 `exhaustion/erosion` 与 `vulnerable/void`。 |

注意：

- `plague-codex` 只打士气，不扣 HP，不触发 HPDamage 相关 Trait / Role Action。
- `witch-bell` 不应重复延长战栗，否则会过度压制反击。

### 5.6 物理节奏 / 再行动遗物

目标：强化 War Queen、Barbarian、Duelist、Peasant 民兵的“找窗口、连打、兑现 HP 伤害”玩法。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-red-whetstone` | I | 3 | common | 我方物理攻击单位攻击 +1。 | Barbarian、Knight、Monster、Peasant、Duelist | 建议新增 `RewardPhysicalAttackStatus`，与魔法攻击奖励对称。 |
| `relic-duelist-ticket` | II | 5 | rare | 每回合首次攻击无盾敌人前，物理攻击者获得强攻。 | Duelist Aura、War Queen、Barbarian | 类似 `DuelSenseTrait.OnAttackDeclared`，但全队每 turn 1 次。 |
| `relic-victory-drum` | III | 8 | epic | 每回合 1 次，额外攻击造成 HP 伤害时，获得 1 BP。 | Edict of Victory、Star Reading、Militia Call、Glory Roar | 给目标挂临时 relic marker；攻击结算看真实 HPDamage。 |

注意：

- `victory-drum` 只看真实 HP 伤害。只打士气不触发。
- `duelist-ticket` 与 Duelist Rank1 Aura 会叠加到“物理单位攻击 +2 + 强攻窗口”，成本不能太低。

### 5.7 士兵团 / 民兵遗物

目标：让“招募士兵并保留在场”成为 build，不只是拿副官素材。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-muster-papers` | I | 3 | common | 士兵招募成本 -1 BP。 | 士兵团、Peasant | Reward cost modifier；只影响 `soldier-recruit`，不会低于 1 BP。 |
| `relic-shared-drillbook` | II | 5 | rare | 若有 2 名以上士兵在场，士兵攻击 +1。 | Militia Foreman、Call the Hunt、士兵 Aura | 新增 soldier-only attack aura；不可驱散。 |
| `relic-veteran-captain-badge` | III | 7 | epic | 每回合首次使用士兵 Role Action 后，获得 1 BP。 | Rank2 士兵、Militia Call | Role Action 结算后检查 actor.CardType == Soldier；受 BP 获取上限限制。 |

注意：

- `shared-drillbook` 不影响副官状态下的士兵本体，但副官保留的 Rank1 Aura 仍照常生效。
- `muster-papers` 不应降低英雄招募成本，避免所有队伍都走扩编。

### 5.8 猎物 / 绝对伤害 / 献祭遗物

目标：给 Monster 路线和献祭 build 更清楚的节奏，但不直接把绝对伤害堆到失控。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-blood-coin` | I | 3 | common | 每回合首次支付 HP 后，获得 1 BP。 | Dark Pact、Abyssal Bargain | 监听非伤害型 HP loss；不响应敌方造成伤害；受 BP 获取上限限制。 |
| `relic-hunter-fang` | II | 5 | rare | 每回合首次对猎物造成绝对伤害后，目标获得脆弱。 | Predatory Gaze、Nightmare Stare | 绝对伤害结算后赋予 `VulnerableStatus`。 |
| `relic-abyss-contract-seal` | III | 8 | epic | 每回合 1 次，支付 HP 后获得护咒，并使下次攻击附加 2 绝对伤害。 | Dark Pact、Abyssal Bargain、Monster | 赋予 `SpellWardStatus` + 可复用或新增一次性 absolute marker。 |

注意：

- `hunter-fang` 的收益在下一次物理攻击或 Soldier 追击上兑现，不直接增加本次绝对伤害。
- `abyss-contract-seal` 必须限制每己方 turn 1 次，否则献祭链会滚太快。

### 5.9 士气 / BP 指挥遗物

目标：利用 BP 与士气恢复的现有关系，让指挥型队伍有经济身份。

| ID | 阶段 | 成本 | 稀有度 | 效果文本 | 适配 build | 实装落点 |
|---|---:|---:|---|---|---|---|
| `relic-campaign-ledger` | I | 3 | common | 回合结束时若 AP 为 0，获得 1 BP。 | 低 Cost 队、Royal Command、士兵团 | EndTurn 前 `TryGainBp`，之后按实际 BP 恢复士气；受 BP 获取上限限制。 |
| `relic-green-standard` | II | 5 | rare | 每回合首次击破敌方共享盾时，额外获得 1 BP。 | Barbarian、Knight Iron Charge、物理节奏 | 复用破盾 BP 事件；受 BP 获取上限限制。 |
| `relic-command-table` | III | 8 | epic | 每回合首次使用 2 AP 以上行动后，返还 1 AP。 | 高费行动、指挥、终局技能 | Role Action 支付后结算返还；不要求特定角色。 |

注意：

- 这些遗物不直接恢复士气，而是通过“实际获得 BP → turn end 士气恢复”的既有管线工作。
- `campaign-ledger` 会鼓励打空 AP，适合低 Cost / 多行动队，但受 BP cap 限制，不会无限刷。

## 6. 推荐奖励池配置

### Stage I 池

用于 round 4 起，帮助玩家看见方向。

- `relic-silver-ward-charm`
- `relic-black-iron-rivets`
- `relic-apprentice-star-ink`
- `relic-white-lily-censer`
- `relic-mason-token`
- `relic-witch-bell`
- `relic-red-whetstone`
- `relic-muster-papers`
- `relic-blood-coin`
- `relic-campaign-ledger`

### Stage II 池

用于 round 8 起，开始把单点效果变成循环。

- `relic-cleanse-votive`
- `relic-cracked-shield-bell`
- `relic-ember-astrolabe`
- `relic-ashen-detonator`
- `relic-spore-press`
- `relic-duelist-ticket`
- `relic-shared-drillbook`
- `relic-hunter-fang`
- `relic-green-standard`

### Stage III 池

用于 round 12 起，或队伍已有 Rank2 / Rank3 时提高权重。

- `relic-war-council-banner`
- `relic-oath-keystone`
- `relic-kingwall-standard`
- `relic-hollow-comet-lens`
- `relic-plague-codex`
- `relic-victory-drum`
- `relic-veteran-captain-badge`
- `relic-abyss-contract-seal`
- `relic-command-table`

## 7. 权重规则建议

遗物出现权重可以根据队伍状态动态调整，但不要完全过滤。玩家应该偶尔看到跨 build 选项，用来转向。

| 条件 | 提高权重 |
|---|---|
| 队伍有 Mage / Oracle / Arcanist 或魔法攻击单位 ≥2 | 魔法 / 咏唱 / 燃烧遗物 |
| 队伍有 Knight / Shieldmaiden 或共享盾相关 Role Action | 盾墙 / 守护遗物 |
| 队伍有 Princess 祈祷路线 / Cleric / Druid 净化路线 | 圣疗 / 净化遗物 |
| 队伍有 Druid / Duelist / Mage 刻印路线 / Barbarian 挑衅 | Debuff / 控制遗物 |
| 队伍有 War Queen / Barbarian / Duelist / 物理单位 ≥2 | 物理节奏遗物 |
| 士兵数量 ≥2 或 Peasant 民兵路线 | 士兵团遗物 |
| 队伍有 Monster 或 HP 代价 Role Action | 猎物 / 献祭遗物 |
| 队伍低 Cost 单位多、Role Action 使用频繁、Rank3 英雄存活 | BP / 指挥遗物 |

## 8. 实装边界建议

后续实装时，建议新增一个轻量 `RelicDefinition`，不要把所有遗物写进 `RewardDefinition` 的字符串 switch。

建议字段：

```csharp
public sealed record RelicDefinition(
    string Id,
    int Cost,
    string Rarity,
    RelicStage Stage,
    IReadOnlyList<string> BuildTags);
```

状态与触发建议：

- 静态数值遗物可以继续复用 `Reward*Status`，挂到全队角色上。
- 触发型遗物建议挂在 `PlayerState.Relics` 或类似队伍级容器里，不作为角色普通 Buff。
- 遗物触发计数应按 `turn` 或 `round` 存在队伍级 state 中，避免靠 status 堆在角色身上难以同步。
- 遗物说明文本进入 `wwwroot/locales/*.json`，C# 只保留稳定英文 ID。

需要新增的通用 hook：

| Hook | 用途 |
|---|---|
| `OnRoleActionResolved` | 治疗、净化、额外攻击、2 AP 行动返还。 |
| `OnStatusApplied` | 燃烧、Debuff、护咒、坚守链。 |
| `OnBuffDispelled / OnDebuffCleansed` | Druid / Cleric / Saint build 的 BP 回报。 |
| `OnDamageResolved` | 士气伤害但 0 HP 伤害、绝对伤害后续状态。 |
| `OnSharedShieldBroken` | 盾墙反击与额外 BP。 |
| `OnHpPaidOrSacrificed` | Monster / Dark Pact / Abyssal Bargain build。 |
| `OnTurnEndBeforeMoraleRecovery` | AP 用尽得 BP，然后进入既有士气恢复。 |

## 9. 第一批落地优先级

如果先做最小可玩版本，建议按以下顺序：

1. 替换 dummy 奖励：银护符、黑铁铆钉、见习星墨、军议战旗。
2. 做 4 个 Stage I build 信号：白百合香炉、石匠令、红磨刀石、募兵文书。
3. 做 5 个 Stage II 引擎：余烬星盘、灰烬引爆器、孢子压榨器、决斗券、碎盾铃。
4. 最后做 Stage III：空心彗镜、胜利鼓、瘟疫法典、指挥桌。

这样第一轮不会一次性引入太多 hook，同时已经能让魔法、盾墙、物理节奏、士兵团、Debuff 五条方向明显成形。
