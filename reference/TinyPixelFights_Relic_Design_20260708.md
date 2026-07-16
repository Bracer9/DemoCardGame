# Tiny Pixel Fights — Relic / Build Support Design

更新时间：2026-07-11
状态：26 件目标池已实装；目录、效果、宽松英雄 tag 权重、双语文本、日志与攻击预测已同步。

主要参考：

- `reference/TinyPixelFights_Current_Build_Compendium_20260711.md`
- `reference/TinyPixelFights_RoleAction_Growth_Synergy_Design_20260629.md`
- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_Soldier_Design_20260704.md`
- 当前实现：`Domain/RelicDefinitions.cs`、`Domain/RelicEffects.cs`、`Domain/GameEngine.cs`

## 1. 设计边界

遗物总数上限是 **26 件**。正确结构是：

- 16 件 Common / Rare 地基与组件，可以同时服务多个体系。
- 10 件 Epic 终局遗物，对应 9 条体系终点；Debuff 体系分为“状态种类”和“持续层数”两条收束方向。
- 终局遗物不强制一体系一件；成型过程尽量借用共通组件。
- BP、AP、咏唱、强攻、猎物、共有盾等是通用接口，不因为一个标签就扩成独立五件套。
- 同一英雄的两条 Rank3 路线不能同时入队，但可以分别进入不同体系权重。

终局遗物必须通过以下检查：

1. 收益直接改变战场，而不是只给奖励阶段货币。
2. 常规终局遗物应每 turn 可重复兑现或持续生效。
3. 若要求阵亡、献祭等不可逆代价，收益也必须是持续且足以改变余下战斗的。
4. 触发条件只描述通用战斗事件，不写“某 Rank3 行动影响几名目标”一类角色说明书。
5. 主要数值优先随攻击、防御、治疗量、状态种类或在场单位数量成长，不以固定 +1 / +2 充当终局爽点。

## 2. 九个构筑体系

| 体系 | 主要英雄路线 | 士兵 / 副官 | 共用地基与组件 | 终局遗物 |
|---|---|---|---|---|
| 魔法蓄力 | Astral Oracle、Stellar Archmage | Arcanist | 见习星墨、空心彗镜、回声水晶、指挥桌 | 星界棱镜 |
| 炎上结算 | Arcane Archivist、Stellar Archmage | Arcanist | 见习星墨、余烬星盘、空心彗镜 | 灰烬引爆器 |
| Debuff 收割 | Fate Dealer、Arcane Archivist、Wildspeaker、Dragon Raider | Arcanist、Duelist | 女巫铃、空心彗镜、夜饵；燃烧也计入 Debuff | 瘟疫法典 / 蚕食账簿 |
| 绝对伤害 | Nightmare Fiend、War Queen、Abyssal Queen | Duelist | 红磨刀石、决斗券、夜饵 | 掠食者王冠 |
| 物理连击 | War Queen、Radiant Berserker、Dread Cavalier、Dragon Raider | Duelist | 红磨刀石、决斗券、绿旗、指挥桌 | 红沙漏 |
| 共有盾 | Holy Paladin、Dread Cavalier、Quartermaster | Shieldmaiden | 石匠令、慈悲杯、白百合香炉、绿旗、指挥桌 | 王墙军旗 |
| 治疗净化 | Saint Queen、Quartermaster、Grove Keeper | Cleric、Shieldmaiden | 慈悲杯、白百合香炉、石匠令、指挥桌 | 圣徒圣杯 |
| 士兵团 | Militia Foreman、Wildspeaker | 四类士兵 | 征募令、军士指挥印；按攻击类型借星墨 / 磨刀石，物理士兵可借绿旗 | 连队军旗 |
| 公主献祭 | Monster 任一路线、低投入 Princess、兼容第二英雄 | Duelist、Cleric | 血币、夜饵、决斗券、慈悲杯等共用组件 | 丧仪金币 |

这九条是出现率识别和 Epic 投放的体系。一个阵容可以同时命中多个体系，例如 Arcane Archivist + Fate Dealer 同时命中炎上与 Debuff；Nightmare Fiend + War Queen 同时命中绝对伤害与物理连击。

英雄路线覆盖检查：

| 英雄 | Route A | Route B | 额外交叉 |
|---|---|---|---|
| Princess | Saint Queen -> 治疗净化 / 共有盾 | War Queen -> 物理连击 / 绝对伤害 | 低投入 Princess 是公主献祭组件 |
| Oracle | Astral Oracle -> 魔法蓄力 | Fate Dealer -> Debuff 收割 | 两线都可借魔法组件 |
| Peasant | Quartermaster -> 治疗净化 / 共有盾 | Militia Foreman -> 士兵团 / 物理连击 | 当前 Attack 同时影响治疗与民兵追加伤害 |
| Mage | Stellar Archmage -> 魔法蓄力 / 炎上 | Arcane Archivist -> 炎上 / Debuff 收割 | 两线共用见习星墨与 Arcanist |
| Druid | Grove Keeper -> 治疗净化 | Wildspeaker -> Debuff 收割 / 士兵团 | Cleric 与攻击型士兵分别接两线 |
| Barbarian | Radiant Berserker -> 物理连击 | Dragon Raider -> 物理连击 / Debuff 收割 | 战栗与脆弱进入状态计数 |
| Monster | Nightmare Fiend -> 绝对伤害 | Abyssal Queen -> 绝对伤害 / 治疗修复 | 两线都可进入公主献祭 |
| Knight | Holy Paladin -> 共有盾 / 治疗净化 | Dread Cavalier -> 共有盾 / 物理连击 | 共享盾既能防守也能转进攻 |

## 3. 通用结算口径

- “每回合”按当前 battle turn 计数，进入下一个 turn 时重置。
- 实际治疗量与过量治疗量分开统计。过量治疗量是一次主动治疗原本可恢复的 HP 减去真实恢复量，最低为 0；即使真实恢复量为 0，只要过量治疗量大于 0，慈悲杯仍可触发。
- 被 BP / AP 上限拦截的资源返还不计；要求实际治疗、共有盾增加或状态成功的遗物，严格按各自实现边界判断。
- 遗物赋予强攻、魔涌、坚守、护咒、空虚、脆弱、战栗时，使用状态默认 2 turn。
- 普通物理 / 魔法伤害仍先扣士气；绝对伤害直接扣 HP；士气伤害不算 HP 伤害。
- 追加伤害在前一段结算后触发；目标已经被击败时不再追击。
- 静态修正对之后招募的单位同样生效。遗物唯一，不可重复持有。
- “在场士兵数”只统计存活、未部署中且仍作为士兵在战场上的单位；副官不计入。
- 公主献祭条件检查我方 Princess 是否已经被击败，不要求遗物在死亡发生前持有。

## 4. 共用地基与组件：16 件

### 4.1 Common：6 件

| 遗物 | 成本 | 标签 | 玩家文本 | 实现边界 | 源码状态 |
|---|---:|---|---|---|---|
| 见习星墨<br>`relic-apprentice-star-ink` | 4 BP | `magic` `burning` `debuff` | 我方魔法攻击单位攻击 +1。 | 按单位当前攻击类型动态判断。 | 已接入 |
| 红磨刀石<br>`relic-red-whetstone` | 3 BP | `physical` `absolute` `soldier` | 我方物理攻击单位攻击 +1。 | 按单位当前攻击类型动态判断；物理士兵与物理 Monster 均获得。 | 已接入 |
| 石匠令<br>`relic-mason-token` | 3 BP | `shield` `healing` `fortify` | 每回合首次增加共有盾时，低 HP 友方获得坚守。 | 要求盾值实际增加；按 HP 比例选择存活在场目标，平手按队伍顺序。 | 已接入 |
| 征募令<br>`relic-muster-papers` | 2 BP | `soldier` `reward` | 士兵招募成本 -1 BP。 | 最低 1 BP；不影响士兵强化、英雄招募和英雄训练。以当前 4 BP 招募价计算，第二次招募时回本。 | 已接入 |
| 慈悲杯<br>`relic-mercy-cup` | 4 BP | `healing` `shield` | 每回合首次产生过量治疗时，将其中最多等同治疗者当前攻击力的数值转化为共有盾。 | 只取本次最高单体过量治疗量；即使实际恢复 0 HP 也可触发。自动治疗、升级回复和士气恢复不触发。 | 已接入 |
| 女巫铃<br>`relic-witch-bell` | 3 BP | `debuff` `control` | 每回合首次赋予敌方 Debuff 时，追加战栗。 | 原 Debuff 必须成功生效；已有战栗时增加 2 turn；追加的战栗不再次触发本遗物，目标需存活。 | 已接入 |

### 4.2 Rare：10 件

| 遗物 | 成本 | 标签 | 玩家文本 | 实现边界 | 源码状态 |
|---|---:|---|---|---|---|
| 余烬星盘<br>`relic-ember-astrolabe` | 5 BP | `burning` `debuff` | 每回合首次赋予燃烧时，额外 +1 层。 | 在本次赋予量上增加；燃烧 tick 不触发。 | 已接入 |
| 空心彗镜<br>`relic-hollow-comet-lens` | 5 BP | `magic` `debuff` `morale` | 每回合首次只击伤士气的魔法伤害，赋予空虚。 | 检查 `Magical && MoraleDamage > 0 && HpDamage == 0`；目标需存活。 | 已接入 |
| 白百合香炉<br>`relic-white-lily-censer` | 5 BP | `healing` `shield` `spell-ward` | 每回合首次主动治疗 HP 时，目标获得护咒。 | 要求 Role Action 实际恢复 HP；群疗取实际治疗量最高的目标，平手按队伍顺序；自动治疗、吸血和升级回复不触发。 | 已接入 |
| 决斗券<br>`relic-duelist-ticket` | 5 BP | `physical` `absolute` `soldier` | 每回合首次攻击无共有盾保护的敌人前，物理攻击者获得强攻。 | 只响应主动攻击；在攻击开始时检查目标方共有盾。 | 已接入 |
| 军士指挥印<br>`relic-command-sergeant-seal` | 5 BP | `soldier` `role-action` `ap` | 每回合首次使用士兵 Role Action 后，返还 1 AP。 | 先支付完整成本，成功结算后返还；副官宿主行动不触发，AP 不超过上限。 | 已接入 |
| 夜饵<br>`relic-night-bait` | 5 BP | `prey` `absolute` `debuff` | 每回合首次未削减敌人士气与 HP 时，赋予猎物。 | 共有盾完全吸收或双防归零满足；只打士气不满足；要求发生伤害结算且目标存活。 | 已接入 |
| 指挥桌<br>`relic-command-table` | 6 BP | `role-action` `ap` `support` | 每回合首次使用 2 AP Role Action 后，返还 1 AP。 | 按最终实际支付成本判断；成功结算后返还，AP 不超过上限。 | 已接入 |
| 回声水晶<br>`relic-echo-crystal` | 5 BP | `magic` `chant` | 每回合首次消耗咏唱后，重新获得 1 层咏唱。 | 在本次伤害及其追加段全部结算后赋予，不能回头放大刚刚结算的同一次伤害。 | 已接入 |
| 绿旗<br>`relic-green-standard` | 5 BP | `physical` `shield` `ap` | 每回合首次击破敌方共有盾后，返还 1 AP。 | 共有盾只吸收当前盾值，溢出伤害继续结算；返还 AP 只用于后续行动，且不超过 AP 上限。 | 已按 AP 效果接入 |
| 血币<br>`relic-blood-coin` | 5 BP | `sacrifice` `shield` | 每回合首次支付 HP 后，获得等同支付量的共有盾。 | 只响应技能代价实际扣除的 HP；受到伤害和主动失去 HP 不触发，共有盾增加可以触发通用盾接口。 | 已接入 |

## 5. 终局遗物：10 件

### 5.1 魔法蓄力：星界棱镜

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-astral-prism` / 8 BP |
| 玩家文本 | 每回合首次消耗咏唱造成魔法伤害时，追加施术者当前攻击力的魔法伤害。 |
| 结算 | 对同一存活目标追加一段普通魔法伤害，仍先扣士气；追加段不再消费咏唱，也不递归触发本遗物。 |
| 成型意义 | 咏唱不只把一次攻击翻倍，还稳定产生一段随魔攻、Aura 与 Rank 成长的终局追加伤害。 |
| 源码状态 | 已接入。 |

### 5.2 炎上结算：灰烬引爆器

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-ashen-detonator` / 8 BP |
| 玩家文本 | 每回合 1 次，命中 3 层以上燃烧时，消耗全部燃烧并造成层数 x2 的魔法伤害。 |
| 结算 | 在原伤害后检查存活目标；燃爆无视魔防，但仍先扣士气，不是绝对伤害。 |
| 成型意义 | 把铺层转换为明确爆发，解决燃烧基础伤害低、受魔防后缺少收尾的问题。 |
| 源码状态 | 已按层数 x2 接入。 |

### 5.3 Debuff 收割：瘟疫法典

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-plague-codex` / 8 BP |
| 玩家文本 | 每回合首次使敌人新增第 3 种或更多 Debuff 时，造成等同其 Debuff 种类数的绝对伤害。 |
| 结算 | 只在不同状态 ID 的种类数真正增加后检查；叠层或刷新同一种 Debuff 不触发，目标已被击败则不触发。 |
| 成型意义 | 让控制不只拖延，而能把多种状态转成直接 HP 压力；Fate Dealer 负责士气，法典负责收尾。 |
| 源码状态 | 已接入。 |

### 5.3b Debuff 蚕食：蚕食账簿

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-attrition-ledger` / 8 BP |
| 玩家文本 | 目标身上所有常规 Debuff 层数合计大于 8 时，该目标每次被我方主动攻击后额外受到 1 点绝对伤害；合计大于 12 时改为 2 点。不限次数。 |
| 结算 | 常规 Debuff 取燃烧层数，以及空虚、脆弱、力竭、磨损、战栗的剩余回合数；总和大于 8 时追加 1 点，大于 12 时追加 2 点。只响应主动攻击，追加伤害为绝对伤害，不受共享盾、物防或魔防影响；原攻击已击败目标时不追加。 |
| 成型意义 | 让弄臣、Dragon Raider、Wildspeaker 等持续维持 Debuff 的路线拥有独立终局收益；与瘟疫法典的“多种状态”路线分开。Fate Dealer 仍可用断线压士气，再由蚕食账簿把每次主动攻击转成 HP 压力。 |
| 源码状态 | 已接入，并纳入主动攻击预测。 |

### 5.4 绝对伤害：掠食者王冠

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-predator-crown` / 8 BP |
| 玩家文本 | 我方对猎物造成的绝对伤害 x1.5，向上取整。 |
| 结算 | 只放大已经成立的绝对伤害，不新增伤害段；Monster Trait、噩梦猎物、War Queen 与 Duelist 追击分别计算。 |
| 成型意义 | 把猎物变成全队绝对伤害的共同放大接口，同时避免再追加一整段 Attack 与现有多重追击相乘。 |
| 源码状态 | 已接入。 |

### 5.5 物理连击：红沙漏

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-red-hourglass` / 8 BP |
| 玩家文本 | 每回合第 3 次物理主动攻击，伤害 +攻击者当前攻击力。 |
| 结算 | 在同一个攻击伤害包中增加数值；不是追加攻击或第二段伤害。伤害先扣共有盾，溢出部分继续经过目标防御、士气与 HP。 |
| 成型意义 | 奖励再行动、低 Cost 单位和士兵连击，数值随真正的攻击核心成长，不停留在固定 +3。 |
| 源码状态 | 已按攻击者当前攻击力接入。 |

### 5.6 共有盾：王墙军旗

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-kingwall-standard` / 8 BP |
| 玩家文本 | 己方回合开始时，若共有盾为 0，获得等同我方最高物防 +最高魔防的共有盾。 |
| 结算 | 两个最高值可以来自不同存活在场单位；这次自动复盾不触发石匠令，避免自循环。 |
| 成型意义 | 把双防 Aura、Shieldmaiden、Cleric 与 Knight 的防线投入转成每 turn 可重复的基础盾。 |
| 源码状态 | 已按最高物防 + 最高魔防接入。 |

### 5.7 治疗净化：圣徒圣杯

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-saint-chalice` / 8 BP |
| 玩家文本 | 每回合首次主动治疗 HP 后，治疗量最高的目标恢复等量士气；共有盾增加其实际恢复的 HP 与士气之和。 |
| 结算 | 只取本次实际 HP 治疗量最高的单体；该目标恢复等同其实际 HP 治疗量的士气，上限 5，再把实际 HP 治疗量与实际士气恢复量相加为共有盾。自动治疗、升级回复和士气恢复不触发；圣杯恢复士气不算治疗，也不递归。 |
| 成型意义 | 一次主动治疗同时修复 HP 外壳、士气外壳并建立共有盾；共有盾还可供铁壁冲锋转为进攻。与慈悲杯同时持有时，同一次范围治疗可以分别结算最高单体实际治疗与最高单体过量治疗。 |
| 源码状态 | 已接入。 |

### 5.8 士兵团：连队军旗

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-company-standard` / 8 BP |
| 玩家文本 | 士兵主动攻击伤害 +我方在场士兵数。 |
| 结算 | 每次主动攻击计算时动态统计；增加的是同类型普通伤害，仍先扣士气；副官不计数。 |
| 成型意义 | 奖励保留多名士兵在场，而不是把所有 Rank2 士兵都转成副官；额外攻击也能兑现。 |
| 源码状态 | 已接入。 |

### 5.9 公主献祭：丧仪金币

| 字段 | 内容 |
|---|---|
| ID / 成本 | `relic-funeral-coin` / 8 BP |
| 玩家文本 | 公主阵亡且我方怪物存活时，AP 上限 +2。 |
| 结算 | 这是持续队伍修正，不是一次性触发；购买时公主已经阵亡也立即生效；最后一名存活在场 Monster 被击败后失效。若在持有者当前 turn 的奖励阶段激活，当前 AP 同时 +2、但不超过新上限。 |
| 成型意义 | 公主死亡已经让 Monster 获得野兽之怒；遗物再把永久减员转成余下队伍每 turn 可使用的战场资源，形成真正的献祭终局。 |
| 源码状态 | 已接入。 |

## 6. 成型路径与共享关系

| 体系 | 推荐三件拼图 | 第二套可借组件 |
|---|---|---|
| 魔法蓄力 | 见习星墨 + 回声水晶 + 星界棱镜 | 空心彗镜、指挥桌、Arcanist Aura / 咏唱 |
| 炎上结算 | 见习星墨 + 余烬星盘 + 灰烬引爆器 | 空心彗镜给归档 / Fate 补 Debuff |
| Debuff 收割 | 女巫铃 + 空心彗镜 + 瘟疫法典 / 蚕食账簿 | 夜饵、余烬星盘、各英雄自带战栗 / 脆弱 / 力竭 / 磨损 |
| 绝对伤害 | 决斗券 + 夜饵 + 掠食者王冠 | 红磨刀石、Duelist 副官；各类既有绝对追击均吃 x1.5 |
| 物理连击 | 红磨刀石 + 决斗券 + 红沙漏 | 绿旗、指挥桌、士兵额外攻击 |
| 共有盾 | 石匠令 + 绿旗 + 王墙军旗 | 慈悲杯、白百合香炉、指挥桌、双防 Aura |
| 治疗净化 | 慈悲杯 + 白百合香炉 + 圣徒圣杯 | 石匠令、指挥桌、Cleric 副官；Epic 修复士气，并把实际 HP / 士气恢复转为共有盾 |
| 士兵团 | 征募令 + 军士指挥印 + 连队军旗 | 物理借红磨刀石 / 绿旗，魔法借见习星墨 |
| 公主献祭 | 血币 + 既有攻击 / 治疗组件 + 丧仪金币 | Nightmare 借夜饵 / 掠食者王冠；Abyssal 借慈悲杯 |

“三件拼图”不是强制套装判定，只说明正常 pacing 下用两件共享组件加一件终局遗物即可形成清楚打法。玩家不需要集齐该行才允许 Epic 出现。

## 7. 宽松随机权重

所有 rarity 从第一次遗物选择起都可以出现。Common / Rare / Epic 不参与资格门槛；Epic 只在后期获得轻微额外权重。

抽取规则：

1. Common / Rare 基础权重为 10；Epic 在 round 12 前为 5，round 12 起提高到 10。
2. 收集我方所有在战场英雄提供的路线 tag；不要求英雄存活。
3. Rank0 英雄尚未锁线，同时提供两条可能路线的 tag 并集。
4. Rank1 及以上英雄只提供已经选择路线的 tag；Rank2 / Rank3 不再额外增加权重。
5. 遗物任一 `BuildTag` 命中队伍英雄 tag 时，最终权重 x2。
6. 同一遗物无论命中几个 tag、几名英雄，都只乘 2 一次，不继续叠加。
7. 每次按权重无放回抽取 3 件；无关遗物和早期 Epic 始终保留出现可能。

因此：Common / Rare 为无关 10、相关 20；round 12 前 Epic 为无关 5、相关 10；round 12 起 Epic 为无关 10、相关 20。

明确不参与权重的内容：

- round / rarity / Rank 形成的硬资格门槛。
- 英雄 Rank 高低、存活状态和同路线英雄数量。
- 士兵、Rank2 副官、Aura、当前状态与野兽之怒。
- 已拥有的同体系遗物、双 Rank3 组合或所谓“build 已成型”判断。

权重只负责让角色可能的发展方向稍微更常见，不负责替玩家收束路线，也不提供保底。公主献祭由 Monster 路线本身提供 `sacrifice` tag，不需要等 Princess 死亡后才进入池。

## 8. 源码实装状态

26 件遗物池已经完成：

- `RelicCatalog.All` 恰好为 26 件：新增 `relic-attrition-ledger` 作为 Debuff 深度路线终点。
- `RelicStage` 已移除；所有 rarity 均可出现，继续使用路线 x2 与 round 12 Epic 软提升。
- 主动治疗批次、Debuff 成功赋予、HP 支付与咏唱消费已经形成通用事件边界，群体治疗和同目标多段治疗按一次 Role Action 聚合。
- 攻击预测已纳入决斗券、连队军旗、红沙漏、星界棱镜、灰烬引爆器、夜饵、绿旗、回声水晶和掠食者王冠的可预知影响。
- 中日文本、遗物 HUD 图标映射、战况日志和在线状态使用现有 DTO / 同步链路，无新增客户端私有状态。
- 自动检查覆盖 26 个唯一 ID、双语 26 项一一对应、关键事件 hook 与旧遗物运行代码清理。
