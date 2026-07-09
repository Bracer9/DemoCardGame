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

## 4. 体系遗物设计原则

遗物池按体系组织，而不是按单件效果平铺。Common / Rare 负责铺地基，Epic 负责把同一套地基收束成不同胜法。

- Common：给方向信号，尽量是容易理解的数值、成本或状态入口。
- Rare：补循环，让已有角色、士兵、Aura、副官开始互相吃到收益。
- Epic：每个体系至少 2 个，分别代表不同终局方向。Epic 是拼图最后一块，不是角色专属说明书。
- 玩家可见文本保持短句。需要精确定义的边界放在“实装备注”，不要塞进效果文本。
- 同一体系的两个 Epic 应共享 Common / Rare 地基，但推动不同打法，例如“层数爆发”和“持续蚕食”。

## 5. 体系遗物设计

### 5.1 炎上 / 咏唱 / 魔法

构筑目标：用 Mage / Oracle / Arcanist 稳定制造魔法伤害与燃烧层数，最终选择“攒层引爆”或“强化燃烧本体”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-apprentice-star-ink` | 4 | 我方魔法攻击单位攻击 +1。 | 魔法队基础火力。 |
| Rare | `relic-ember-astrolabe` | 5 | 每回合首次赋予燃烧时，额外 +1 层。 | 燃烧层数地基。 |
| Rare | `relic-hollow-comet-lens` | 5 | 每回合首次只击伤士气的魔法伤害，赋予空虚。 | 剥士气也有推进。 |
| Epic A | `relic-ashen-detonator` | 8 | 每回合 1 次，命中 3 层以上燃烧时，引爆燃烧。 | 层数爆发。 |
| Epic B | `relic-smoldering-censer` | 8 | 燃烧基础伤害 +1。 | 持续蚕食。 |

实装备注：

- `ashen-detonator` 消耗目标全部燃烧，造成等同层数的燃爆魔法伤害；不受魔防，仍先扣士气，不是绝对伤害。
- `smoldering-censer` 只改燃烧回合结算的基础伤害；仍是魔法伤害，仍先扣士气，仍不算 HP 伤害除非实际溢出到 HP。
- `hollow-comet-lens` 检查 `MoraleDamage > 0 && HpDamage == 0 && DamageType.Magical`，不引入有效伤害概念。

### 5.2 盾墙 / 守护

构筑目标：用 Knight / Shieldmaiden / 共享盾撑住战线，最终选择“稳定复盾”或“破盾反击”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-black-iron-rivets` | 3 | 我方全体物防 +1。 | 抗物理基础。 |
| Common | `relic-mason-token` | 3 | 每回合首次部署或强化共享盾时，低 HP 友方获得坚守。 | 防线保护入口。 |
| Rare | `relic-cracked-shield-bell` | 5 | 每 round 1 次，共享盾被击破后，攻击者获得战栗。 | 破盾惩罚。 |
| Epic A | `relic-kingwall-standard` | 8 | 回合开始时，若共享盾为 0，获得共享盾 2。 | 稳定复盾。 |
| Epic B | `relic-bastion-hammer` | 8 | 共享盾被击破时，攻击者受到 3 点士气伤害。 | 盾墙反击。 |

实装备注：

- `kingwall-standard` 不触发“部署共享盾”类遗物，避免自循环。
- `bastion-hammer` 只打士气，不扣 HP，不触发 HPDamage 相关 Trait / Role Action。
- 共享盾破碎仍然不穿透到 HP。

### 5.3 圣疗 / 净化

构筑目标：用 Princess 祈祷路线、Cleric、Druid 净化维持 HP 与状态优势，最终选择“护盾化”或“团队回血”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-silver-ward-charm` | 3 | 我方全体魔防 +1。 | 抗魔基础。 |
| Common | `relic-white-lily-censer` | 3 | 每回合首次主动治疗 HP 时，目标获得护咒。 | 治疗转防护。 |
| Rare | `relic-cleanse-votive` | 5 | 每回合首次成功净化时，获得 1 BP。 | 净化转经济。 |
| Epic A | `relic-oath-keystone` | 7 | 每回合第 3 次获得坚守或护咒时，共享盾 +3。 | 防护链收束。 |
| Epic B | `relic-saint-bell` | 8 | 每回合首次净化后，全队 HP +1。 | 团队续航。 |

实装备注：

- 这些遗物不直接恢复士气。士气仍只通过 BP 回合结束恢复。
- `white-lily-censer` 只响应主动治疗，避免免费回合开始治疗直接滚雪球。
- `saint-bell` 是治疗 HP，不恢复士气，不算净化本身造成的 BP。

### 5.4 Debuff / 控制

构筑目标：把 Druid、刻印 Mage、Duelist、Barbarian 的控制状态串起来，最终选择“士气压制”或“弱点斩杀”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-witch-bell` | 3 | 每回合首次赋予敌方 Debuff 时，追加战栗。 | 控制入口。 |
| Rare | `relic-spore-press` | 5 | 每回合首次驱散敌方 Buff 时，获得 1 BP。 | Druid / 净化经济。 |
| Rare | `relic-cracked-mask` | 5 | 战栗目标受到的士气伤害 +1。 | 控制转士气压力。 |
| Epic A | `relic-plague-codex` | 8 | 敌方获得第 3 个 Debuff 时，受到 4 点士气伤害。 | 状态堆叠压制。 |
| Epic B | `relic-lockjaw-mask` | 8 | 战栗目标受到的第一次 HP 伤害 +2。 | 弱点斩杀。 |

实装备注：

- `plague-codex` 只打士气，不触发 HPDamage 条件。
- `lockjaw-mask` 只增加真实 HP 伤害；如果本次只打士气，额外值不触发。
- Debuff 计数应按不同状态 ID 计算，不按层数计算。

### 5.5 物理节奏 / 再行动

构筑目标：用 War Queen、Barbarian、Duelist、物理士兵制造攻击窗口，最终选择“连击经济”或“第三击爆发”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-red-whetstone` | 3 | 我方物理攻击单位攻击 +1。 | 物理队基础火力。 |
| Rare | `relic-duelist-ticket` | 5 | 每回合首次攻击无盾敌人前，物理攻击者获得强攻。 | 开窗口。 |
| Rare | `relic-green-standard` | 5 | 每回合首次击破敌方共享盾时，额外获得 1 BP。 | 破盾经济。 |
| Epic A | `relic-victory-drum` | 8 | 每回合 1 次，额外攻击造成 HP 伤害时，获得 1 BP。 | 连击经济。 |
| Epic B | `relic-red-hourglass` | 8 | 每回合第 3 次主动攻击伤害 +3。 | 第三击爆发。 |

实装备注：

- `victory-drum` 只看真实 HP 伤害。只打士气不触发。
- `red-hourglass` 增加的是本次普通伤害，仍先扣士气，不是绝对伤害。
- `duelist-ticket` 与 Duelist Rank1 物理攻击 Aura 可叠加，成本不宜下调。

### 5.6 士兵团 / 民兵

构筑目标：让招募、保留、升级士兵本身成为 build，最终选择“士兵 Role Action 循环”或“多士兵攻击压制”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-muster-papers` | 3 | 士兵招募成本 -1 BP。 | 扩编入口。 |
| Rare | `relic-shared-drillbook` | 5 | 若有 2 名以上士兵在场，士兵攻击 +1。 | 保留士兵奖励。 |
| Rare | `relic-veteran-captain-badge` | 5 | 每回合首次使用士兵 Role Action 后，获得 1 BP。 | 士兵行动经济。 |
| Epic A | `relic-command-sergeant-seal` | 8 | 每回合首次使用士兵 Role Action 后，返还 1 AP。 | 士兵技能循环。 |
| Epic B | `relic-company-standard` | 8 | 每回合第 3 次士兵攻击伤害 +3。 | 多士兵压制。 |

实装备注：

- `muster-papers` 只影响士兵招募，不影响英雄招募，且成本最低为 1 BP。
- `shared-drillbook` 只影响在场士兵；副官保留的 Rank1 Aura 继续通过 Aura 系统生效。
- `company-standard` 增加的是普通伤害，仍先扣士气。

### 5.7 猎物 / 绝对伤害 / 献祭

构筑目标：给 Monster、Dark Pact、Abyssal Bargain 明确节奏，最终选择“猎物追杀”或“献祭爆发”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-blood-coin` | 3 | 每回合首次支付 HP 后，获得 1 BP。 | 献祭经济。 |
| Rare | `relic-night-bait` | 5 | 每回合首次造成 0 点 HP 伤害时，赋予猎物。 | 猎物入口。 |
| Rare | `relic-hunter-fang` | 5 | 每回合首次对猎物造成绝对伤害后，目标获得脆弱。 | 追杀铺垫。 |
| Epic A | `relic-predator-crown` | 8 | 猎物受到 0 点 HP 伤害时，追加 2 绝对伤害。 | 猎物追杀。 |
| Epic B | `relic-abyss-contract-seal` | 8 | 每回合 1 次，支付 HP 后，下次攻击附加 2 绝对伤害。 | 献祭爆发。 |

实装备注：

- 绝对伤害直接扣 HP，不走士气。
- `night-bait` 和 Monster 的“0 点 HP 伤害追击”共享真实 HPDamage 口径；只打士气也算 0 HP 伤害。
- `abyss-contract-seal` 只响应支付 / 献祭 HP，不响应受到敌方伤害。

### 5.8 BP / 指挥 / 士气

构筑目标：利用 BP 获取、AP 节奏和士气回合结束恢复，最终选择“高费行动循环”或“BP 引擎转 AP”。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-campaign-ledger` | 3 | 回合结束时若 AP 为 0，获得 1 BP。 | 打空 AP 的经济身份。 |
| Rare | `relic-supply-cart` | 5 | 回合结束时，若本回合获得 3 点以上 BP，共享盾 +2。 | BP 转防线。 |
| Rare | `relic-brass-order` | 5 | 每回合首次获得第 5 点 BP 时，获得强攻。 | BP 上限奖励。 |
| Epic A | `relic-command-table` | 8 | 每回合首次使用 2 AP 以上行动后，返还 1 AP。 | 高费行动循环。 |
| Epic B | `relic-war-room-map` | 8 | 每回合第 3 次获得 BP 时，获得 1 AP。 | BP 引擎转 AP。 |

实装备注：

- BP 获取仍受每 turn 5 点上限限制。
- `supply-cart` 不恢复士气，只把高 BP 回合转换成共享盾。
- `war-room-map` 只响应成功获得 BP，不响应被 BP cap 拦下的溢出。

### 5.9 中立静态遗物

这些遗物不定义 build，只作为早期容错或终局泛用选项。权重应低于命中体系的遗物。

| 层级 | ID | 成本 | 效果文本 | 作用 |
|---|---|---:|---|---|
| Common | `relic-silver-ward-charm` | 3 | 我方全体魔防 +1。 | 抗魔容错。 |
| Common | `relic-black-iron-rivets` | 3 | 我方全体物防 +1。 | 抗物理容错。 |
| Epic | `relic-war-council-banner` | 7 | 我方全体攻击 +1。 | 泛用终局火力。 |

平衡备注：

- `war-council-banner` 不属于任何体系的两个 Epic 分叉；它是保底泛用项，出现权重应低。
- 全队攻击 +1 会放大当前攻击力系行动、普通攻击与部分 Aura，不应过早出现。

## 6. 推荐奖励池配置

Stage 池仍按阶段投放，但权重应优先命中玩家已有体系。Epic 展示时，尽量保证同一体系的两个终局方向都有机会出现，而不是只刷一个孤立强牌。

### Stage I 池

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

- `relic-ember-astrolabe`
- `relic-hollow-comet-lens`
- `relic-cracked-shield-bell`
- `relic-cleanse-votive`
- `relic-spore-press`
- `relic-cracked-mask`
- `relic-duelist-ticket`
- `relic-green-standard`
- `relic-shared-drillbook`
- `relic-veteran-captain-badge`
- `relic-night-bait`
- `relic-hunter-fang`
- `relic-supply-cart`
- `relic-brass-order`

### Stage III 双 Epic 分叉

| 体系 | Epic A | Epic B |
|---|---|---|
| 炎上 / 魔法 | `relic-ashen-detonator` | `relic-smoldering-censer` |
| 盾墙 / 守护 | `relic-kingwall-standard` | `relic-bastion-hammer` |
| 圣疗 / 净化 | `relic-oath-keystone` | `relic-saint-bell` |
| Debuff / 控制 | `relic-plague-codex` | `relic-lockjaw-mask` |
| 物理节奏 | `relic-victory-drum` | `relic-red-hourglass` |
| 士兵团 | `relic-command-sergeant-seal` | `relic-company-standard` |
| 猎物 / 献祭 | `relic-predator-crown` | `relic-abyss-contract-seal` |
| BP / 指挥 | `relic-command-table` | `relic-war-room-map` |

泛用 Epic：

- `relic-war-council-banner`

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

- 静态数值遗物继续走 `RelicEffects` 队伍级修正，不挂角色普通 Buff。
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

如果先做最小可玩版本，不建议一次把全部体系铺开。先选 3 个体系做完整 common / rare / 双 epic 闭环，再扩展其余体系。

1. 替换 dummy 奖励：银护符、黑铁铆钉、见习星墨、军议战旗。
2. 先落地炎上体系：见习星墨、余烬星盘、空心彗镜、灰烬引爆器、余燃香炉。
3. 同步落地盾墙体系：黑铁铆钉、石匠令、碎盾铃、王墙军旗、壁垒锤。
4. 再落地物理节奏体系：红磨刀石、决斗券、绿旗、胜利鼓、红沙漏。
5. 确认 hook、日志、预测、奖励权重稳定后，再补圣疗、Debuff、士兵团、猎物和 BP 指挥体系。

这样第一批就能验证“同一地基分成两个 Epic 方向”的核心目标，而不是只验证一堆零散遗物能否触发。
