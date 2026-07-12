# Tiny Pixel Fights - Current Build Compendium

更新时间：2026-07-11  
状态：当前源码 + 当前设定文档对齐版；26 件遗物池、全 rarity 候选、宽松英雄 tag 权重与九个 Build 终点均已实装。

主要依据：

- `Domain/CharacterDefinition.cs`
- `Domain/HeroGrowthDefinitions.cs`
- `Domain/RoleActions.cs`
- `Domain/Traits.cs`
- `Domain/StatusEffects.cs`
- `Domain/RelicDefinitions.cs`
- `Domain/RelicEffects.cs`
- `Domain/GameEngine.cs`
- `reference/TinyPixelFights_RoleAction_Growth_Synergy_Design_20260629.md`
- `reference/TinyPixelFights_Soldier_Design_20260704.md`
- `reference/TinyPixelFights_Relic_Design_20260708.md`

## 0. 当前构筑环境

### 0.1 成长与奖励节奏

当前源码中，奖励窗口从 round 4 开始，每 4 round 出现一次。

英雄成长：

| Rank | 当前作用 |
|---|---|
| Rank0 | 初始 Trait + 普通攻击，没有 Role Action。 |
| Rank1 | 解锁一个基础 Role Action，并固定后续路线。第一次英雄训练消耗 0 BP。升阶回复 50% MaxHp。 |
| Rank2 | 沿 Rank1 路线获得属性提升；多数路线有 Trait 强化，源码未见强化 hook 的路线在下文单独标注。升阶回复 50% MaxHp。 |
| Rank3 | 解锁该路线最终 Role Action，获得属性提升，HP 全回复。每名存活且在场的 Rank3 英雄使队伍 AP 上限 +1。 |

士兵成长：

| Rank | 当前作用 |
|---|---|
| Rank0 | 初始 Trait + 普通攻击。 |
| Rank1 | HP 小幅成长，Trait 强化，并提供持续 Aura。升阶回复 50% MaxHp。 |
| Rank2 | MaxHp +5，HP 全回复，解锁固定 Role Action，可成为副官。 |

遗物池：

- 所有 rarity 从第一次遗物选择起都可能出现，不按 round 或 Rank 写死资格。
- 所有未拥有遗物都留在候选池；无关路线遗物不会被过滤。
- Common / Rare 基础权重 10；Epic 在 round 12 前为 5、round 12 起提高到 10。
- 命中英雄路线 tag 后最终权重 x2；同一遗物只乘一次。
- Rank0 英雄同时提供两条可能路线的 tag；Rank1 锁线后只提供实际路线 tag。

### 0.2 士气与伤害口径

士气是每个角色自己的外接 HP，初始 5 / 5。

- 普通物理 / 魔法伤害先扣士气，士气不足才扣 HP。
- 士气伤害不是 HP 伤害。
- 只打士气时，真实 HPDamage 为 0。
- 绝对伤害直接扣 HP，不经过士气、共享盾、防御。
- 治疗只回复 HP，不回复士气。
- 己方 turn 结束时，存活角色回复本 turn 实际获得 BP 等量士气，上限 5。

这个口径直接影响 build：燃烧、溅射、Rank3 魔法伤害、铁壁冲锋等普通伤害都可能先被士气吃掉；怪物、猎物、黑暗契约、胜利敕令、决斗嗅觉等绝对伤害才是真正直击 HP。

## 1. Build 总览

| Build | 当前核心 | 推荐士兵 / 副官 | 核心遗物 | 当前成型度 | 主要 pacing 问题 |
|---|---|---|---|---|---|
| 魔法蓄力 | Astral Oracle、Stellar Archmage | Arcanist | 见习星墨、回声水晶、指挥桌；星界棱镜收束 | 高 | 需要实测咏唱返还与追加段在多次再行动下的峰值。 |
| 炎上结算 | Arcane Archivist、Stellar Archmage | Arcanist | 见习星墨、余烬星盘；灰烬引爆器收束 | 高 | 当前引爆伤害仍偏低，需要按层数 x2 调整。 |
| Debuff 收割 | Fate Dealer、Arcane Archivist、Wildspeaker、Dragon Raider | Arcanist、Duelist | 女巫铃、空心彗镜、夜饵；瘟疫法典或蚕食账簿收束 | 高 | 需要实测持续 Debuff 层数达到 9 层以上的速度。 |
| 绝对伤害 | Nightmare Fiend、War Queen、Abyssal Queen | Duelist | 红磨刀石、决斗券、夜饵；掠食者王冠收束 | 高 | 需要观察猎物覆盖率与多段绝对追击的乘算峰值。 |
| 物理连击 | War Queen、Radiant Berserker、Dread Cavalier、Dragon Raider | Duelist | 红磨刀石、决斗券、绿旗、指挥桌；红沙漏收束 | 高 | 第 3 次物理攻击已经按当前攻击力成长，主要风险转为再行动爆发上限。 |
| 共有盾 | Holy Paladin、Dread Cavalier、Quartermaster | Shieldmaiden | 石匠令、慈悲杯、绿旗、指挥桌；王墙军旗收束 | 高 | 动态复盾与治疗转盾可能形成过长防线，需要实测终局回合长度。 |
| 治疗净化 | Saint Queen、Quartermaster、Grove Keeper | Cleric、Shieldmaiden | 慈悲杯、白百合香炉、石匠令；圣徒圣杯收束 | 高 | 过量治疗、士气与共有盾已闭环，需观察是否仍缺少终结速度。 |
| 士兵团 | Militia Foreman、Wildspeaker | 四类士兵 | 征募令、军士指挥印；按攻击类型借绿旗 / 星墨 / 磨刀石；连队军旗收束 | 中高 | 遗物已齐，主要问题仍是招募与升级占用多个奖励阶段。 |
| 公主献祭 | Monster 任一路线、低投入 Princess、兼容第二英雄 | Duelist、Cleric | 血币、夜饵及攻击 / 治疗组件；丧仪金币收束 | 中高 | 效果已齐，但触发依赖永久减员，强度需要按真实达成率观察。 |

## 1.1 正确的阵容成长模型

原来的分析真正的问题，是没有分清“主要平衡目标”和“超级大晚期的可选扩展”。正确模型是：

- 主要平衡目标：2 名互相协同的 Rank3 英雄，加上体系需要的 Rank2 士兵或副官。多数正常长度对局在这里就应基本成形，对应遗物 tag 和主要出现率必须全部到位。
- 超级大晚期不是统一阵容。英雄中心 build 可以继续扩成 4 名 Rank3 英雄并配置更多副官；士兵中心 build 可以保留 1-2 名 Rank2 士兵在场，由 2-3 名 Rank3 英雄带队，不必向四英雄阵容收敛。

4 名 Rank3 英雄各带副官只是英雄中心 build 的一种理论上限：

- 4 名不同英雄全部达到 Rank3；每名存活且在场的 Rank3 英雄使 AP 上限 +1，因此完整终局的 AP 上限是基础 5 + 4。
- 每名英雄各携带 1 名 Rank2 士兵副官，共 4 名副官。副官不占战场角色位。
- Rank2 士兵成为副官后，其 Rank1 Aura 继续生效；同名 Aura 不叠加，不同 Aura 可以同时生效。
- 副官仍提供宿主属性和宿主被动。即使 Aura 不叠加，同类副官放在不同宿主上仍可能因宿主属性 / 被动而有价值。

英雄不能重复入队，因此同一英雄的两条路线互斥：Saint Queen / War Queen、Astral Oracle / Fate Dealer、Quartermaster / Militia Foreman、Stellar Archmage / Arcane Archivist、Grove Keeper / Wildspeaker、Radiant Berserker / Dragon Raider、Nightmare Fiend / Abyssal Queen、Holy Paladin / Dread Cavalier。任何 build 表都不能把同一组中的两个形态写成队友。

士兵是否转为副官由 build 决定。治疗、物理追击、魔法爆发等英雄中心 build 可以把士兵转为副官腾出英雄位；Militia Foreman、Wildspeaker 等士兵中心 build 则应保留关键 Rank2 士兵在场攻击。文档必须先说明双 Rank3 时如何成形，再说明该 build 自己的超晚期扩展方向。

## 1.2 英雄路线横向协同矩阵

下表中的搭配全部来自不同英雄，不包含互斥形态。没有列出的组合不是不能共存，而是当前没有足够强的直接接口，不应获得很高的体系遗物权重。

| 核心路线 | Rank3 的终局接口 | 可共存的高价值英雄路线 | 实际相乘点 | 士兵 / 副官补件 |
|---|---|---|---|---|
| Saint Queen | `miracle-standard`：范围治疗 / 净化，并按净化与魔防补共享盾 | Grove Keeper、Quartermaster、Holy Paladin、Abyssal Queen | 净化、团队治疗、共享盾和 HP 支付修复构成长线循环 | Cleric 提供魔防与治疗响应；Shieldmaiden 提供物防与盾链 |
| War Queen | `edict-of-victory`：给单位额外攻击，攻击后追加 Princess 当前攻击力的绝对伤害 | Radiant Berserker、Militia Foreman、Dread Cavalier、Abyssal Queen | 把多段攻击、铁壁冲锋或献祭攻击继续转成额外攻击与绝对伤害 | Duelist 强化 Rank3 后的物理攻击与追击；Cleric 修复献祭 / 自伤 |
| Astral Oracle | `astral-alignment`：给魔法单位咏唱、额外攻击，下一次魔法伤害追加并溅射 | Stellar Archmage、Arcane Archivist、Wildspeaker、Militia Foreman | 魔力、咏唱、额外攻击直接放大 Mage；与魔法 Debuff / 魔法士兵过渡阵容相容 | Arcanist 是主副官；Cleric / Shieldmaiden 保护低防魔法核心 |
| Fate Dealer | `thread-cut`：按目标标记 / Debuff 数造成士气伤害，士气归零后追加魔法伤害 | Arcane Archivist、Wildspeaker、Dragon Raider、Nightmare Fiend | 燃烧、虚空、力竭、磨损、战栗、脆弱、猎物都能增加 Thread Cut 计数 | Arcanist 放大击穿士气后的魔法伤害；Duelist 补绝对追击 |
| Quartermaster | `field-rations`：全队治疗，主目标额外治疗、坚守、护咒，低血时净化 | Abyssal Queen、Saint Queen、Holy Paladin、Radiant Berserker | 修复 HP 支付 / 自伤，并把低血目标重新拉回可行动区间 | Cleric 继续把治疗转护咒；Shieldmaiden 把坚守转成盾线响应 |
| Militia Foreman | `militia-call`：给 Peasant / 士兵额外攻击与一次性增伤 | Wildspeaker、War Queen、Astral Oracle、Radiant Berserker | 与在场士兵和猎群标记相乘；War Queen 可继续追加额外攻击 | Duelist / Arcanist 应保留在场作为攻击核心，不以全部转副官为目标 |
| Stellar Archmage | `starfall`：当前攻击力魔法伤害并赋予燃烧 | Astral Oracle、War Queen、Wildspeaker、Saint Queen | Oracle 提供咏唱 / 额外攻击；War Queen 提供再攻击和绝对收尾；防守英雄保护蓄力 | Arcanist 主放大，Shieldmaiden / Cleric 保护蓄力窗口 |
| Arcane Archivist | `archive-formula`：按燃烧层数增伤，并按 Debuff 数补燃烧 | Fate Dealer、Wildspeaker、Astral Oracle、Dragon Raider | 多种 Debuff 同时服务燃烧堆层、Thread Cut 与控制收割 | Arcanist 提供魔涌 / 咏唱；Duelist 为后续 HP 收割补绝对伤害 |
| Grove Keeper | `grove-sanctuary`：范围净化后按净化数治疗；无 Debuff 时范围护咒 | Saint Queen、Quartermaster、Holy Paladin、Fate Dealer | 与治疗、护咒、共享盾形成防守端；Fate Dealer 提供另一条进攻胜法 | Cleric 强化净化响应；Shieldmaiden 补物理防线 |
| Wildspeaker | `call-the-hunt`：范围猎群标记，士兵首次攻击获得 Druid 当前攻击力 | Militia Foreman、Nightmare Fiend、Fate Dealer、Arcane Archivist | 由在场士兵触发；猎物和 Debuff 可继续被 Monster / Oracle 转换 | 至少保留 1 名 Rank2 攻击型士兵在场；副官只是其余英雄的补件 |
| Radiant Berserker | `glory-roar`：强攻 + 按攻击力增加额外攻击，每次攻击后失去 1 HP | War Queen、Saint Queen、Quartermaster、Abyssal Queen | War Queen 放大多段攻击；治疗英雄修复自伤；Abyssal Queen 增加绝对追击 | Duelist 提供物攻和追击；Cleric 修复并给护咒 |
| Dragon Raider | `dragon-breaker`：破盾或造成物理伤害，并赋予范围战栗 / 脆弱 | Fate Dealer、Dread Cavalier、War Queen、Arcane Archivist | 战栗 / 脆弱进入 Thread Cut 与 Archive 计数，破盾后由物理队收尾 | Shieldmaiden 提供己方盾线；Duelist 放大破盾后的物理窗口 |
| Nightmare Fiend | `nightmare-stare`：范围噩梦猎物，首次 0 HP 伤害追加绝对伤害 | Wildspeaker、War Queen、Fate Dealer、Dread Cavalier | 猎群 / 标记增加状态密度；额外攻击和只打士气的冲锋都能制造 0 HP 触发 | Duelist 追加绝对追击；防御副官帮助 Monster 存活到触发窗口 |
| Abyssal Queen | `abyssal-bargain`：支付友军 HP，获得额外攻击与攻击后绝对伤害 | Quartermaster、Saint Queen、War Queen、Radiant Berserker | 治疗英雄修复支付；War Queen / Berserker 把一次支付变成连续攻击 | Cleric 是稳定修复端；Duelist 增加攻击后的绝对伤害 |
| Holy Paladin | `holy-bastion`：守护誓约、护咒、坚守和共享盾 | Saint Queen、Quartermaster、Grove Keeper、Fate Dealer | 三种防守接口保护全队，Fate Dealer 提供不依赖普通攻击的士气推进 | Shieldmaiden 与 Cleric 分别补物防 / 魔防和宿主被动 |
| Dread Cavalier | `iron-charge`：消耗共享盾造成物理伤害；真实 HPDamage 时返盾 | War Queen、Fate Dealer、Nightmare Fiend、Dragon Raider | 战栗进入 Thread Cut；只打士气可触发噩梦猎物；War Queen 负责绝对收尾 | Shieldmaiden 提供盾值；Duelist 提供物攻与追击 |

## 1.3 主要成型点与分流终局

遗物概率首先服务双 Rank3 成型点，不能等玩家进入超级大晚期才开始识别 build：

| 阶段 | 常见阵容状态 | 遗物池职责 |
|---|---|---|
| 地基 | Rank0 英雄 + 开局士兵 | Rank0 已把两条可能路线 tag 放入轻微权重；所有 rarity 与其他路线遗物仍可出现 |
| 基本成型 | 2 名兼容英雄达到 Rank3 + 体系需要的 Rank2 士兵在场或成为副官 | 阵容已经能稳定执行打法，但遗物池不因此进一步收束或保底 |
| 英雄中心超晚期 | 3-4 名不同 Rank3 英雄 + 若干 Rank2 副官 | 扩充防守、收割或资源端；Rank 数量不继续叠加遗物权重 |
| 士兵中心超晚期 | 2-3 名 Rank3 英雄 + 1-2 名在场 Rank2 士兵 + 其余副官 | 继续放大士兵攻击和 Role Action；士兵与副官不参与遗物权重 |

下面先写必须优先成立的双 Rank3 核心，再写可选超晚期扩展。斜线只表示替代选择，不表示同一英雄的两个形态同时入队。

| Build | 双 Rank3 基本成型 | Rank2 士兵 / 副官 | 可选超晚期扩展 | 遗物收束 |
|---|---|---|---|---|
| 燃烧爆发 | Stellar Archmage / Arcane Archivist + Astral Oracle | Arcanist Rank2 在场或绑定 Mage；防守副官保护核心 | 可加入 War Queen 提供额外攻击，再加入防守 / Debuff 英雄 | 燃烧堆层后引爆；双 Rank3 时相关 tag 已全开 |
| 燃烧蚕食 / Debuff | Arcane Archivist + Fate Dealer | Arcanist Rank2；Cleric / Shieldmaiden 稳住长线 | 加入 Wildspeaker 扩充 Debuff，再加入 Saint Queen / Holy Paladin | 强化燃烧结算，或把多 Debuff 转成士气与 HP 压力 |
| 物理多段 / 绝对追击 | War Queen + Radiant Berserker | Duelist Rank2 在场或绑定主攻击手；Cleric 修复自伤 | 加入 Dread Cavalier、Abyssal Queen 或其他收割英雄 | 第三击、额外攻击 BP、攻击后绝对伤害 |
| 圣疗盾墙 | Saint Queen + Holy Paladin / Quartermaster | Cleric、Shieldmaiden 至少一名达到 Rank2 | 可继续加入 Grove Keeper、Quartermaster 或 Fate Dealer | 治疗 / 净化转防护、共享盾或团队续航 |
| 破盾士气收割 | Dread Cavalier / Dragon Raider + Fate Dealer | Shieldmaiden Rank2 提供盾；Duelist / Arcanist 补收割 | 加入 War Queen、Nightmare Fiend 或另一名防守英雄 | 破盾经济、战栗士气压制、HP / 绝对伤害收尾 |
| 猎物绝对伤害 | Nightmare Fiend + Fate Dealer / War Queen | Duelist Rank2 提供追击；防守士兵制造安全窗口 | 加入 Dread Cavalier、Wildspeaker 或防守英雄 | 0 HP 触发扩张、猎物绝对伤害收束 |
| 献祭契约 / 绝对伤害 | Abyssal Queen + Quartermaster / Saint Queen | Cleric Rank2 优先；Duelist 绑定攻击核心 | 加入 Radiant Berserker 或 War Queen 扩大攻击次数 | 治疗组件修复 HP 代价，夜饵 + 掠食者王冠收束绝对伤害 |
| 野兽继承：猎物 | Nightmare Fiend + Fate Dealer / Dread Cavalier | Princess 保持 Rank0 / Rank1 作为献祭组件；Duelist Rank2 绑定 Monster 或留场 | 公主死亡后再施放 `nightmare-stare`；可加入防守英雄保护低双防 Monster | 夜饵、掠食者王冠；丧仪金币完成公主献祭终局 |
| 野兽继承：契约 | Abyssal Queen + Quartermaster | Princess 保持 Rank0 / Rank1；Cleric Rank2 修复剩余队伍，Duelist 绑定攻击核心 | `dark-pact` 将公主压到低 HP，死亡后再使用 `abyssal-bargain` | 白百合香炉、夜饵、掠食者王冠；丧仪金币完成公主献祭终局 |
| 民兵猎群 | Militia Foreman + Wildspeaker | Duelist / Arcanist Rank2 保留在场；第二士兵可按局势选择 | 继续维持 2 英雄 + 2 士兵，或加入第 3 名英雄并保留至少 1 名攻击士兵 | 招募 / 升级节奏、多士兵攻击收益；不向四英雄强制收敛 |

## 1.4 遗物出现率规则

遗物抽取不是路线推荐器，只是在完整随机池上增加轻微角色倾向。

| 条件 | 权重 |
|---|---:|
| Common / Rare | 10 |
| round 12 前的 Epic | 5 |
| round 12 起的 Epic | 10 |
| 任一 `BuildTag` 命中当前英雄路线 tag | 最终权重 x2，最多一次 |

路线 tag 口径：

1. Rank0 英雄提供两条未来路线 tag 的并集，让玩家在锁线前就可能看到两边组件。
2. Rank1 及以上只使用已选择路线 tag；Rank2 / Rank3 不追加权重。
3. 同一遗物命中多个英雄或多个 tag 都不叠加，仍然只乘 2 一次。
4. 英雄只要仍在战场 roster 中就提供 tag，不检查存活；士兵、副官、Aura、状态和已有遗物不参与。
5. 所有未拥有遗物始终是候选。其他路线 Common / Rare 保持 10；其他路线 Epic 前期为 5、round 12 后为 10。
6. 每次按权重无放回抽取 3 件，不保底、不固定同体系数量、不隐藏跨路线选项。

## 2. 英雄路线索引

### 2.1 Princess

#### Saint Queen

成型方向：治疗、净化、护咒、共享盾补强。

路线内容：

- Rank1 `saintly-prayer`：1 AP 治疗 2 HP；若净化 Debuff，获得 1 AP。
- Rank2：MaxHp +3、魔防 +1；主动治疗 / 净化成功后给目标护咒。
- Rank3 `miracle-standard`：2 AP 范围治疗与净化；若净化成功，按 Princess 当前魔防 + 影响人数增加共享盾。

推荐组合：

- 搭配英雄：Holy Paladin / Quartermaster 是主要双 Rank3 防线；Grove Keeper 扩大净化续航，Abyssal Queen 提供需要修复的 HP 支付端。
- 士兵：Cleric 是主轴，Shieldmaiden 是副轴。
- 副官：Cleric 副官让治疗 / 净化链额外给护咒；Shieldmaiden 副官让盾和坚守链更稳。
- 遗物：慈悲杯、白百合香炉、石匠令、圣徒圣杯；若队伍偏共有盾可借王墙军旗。

成型方式：

1. Rank1 先拿 `saintly-prayer`，用第一次 0 BP 英雄训练完成路线锁定。
2. 早期优先拿 Cleric 或 Shieldmaiden 到 Rank1，补魔防 / 物防 Aura。
3. 中期如果对局 Debuff 多，推进 Princess Rank2；如果压力来自物理，则优先 Shieldmaiden Rank2 或盾系遗物。
4. Rank3 后以 `miracle-standard` 清 Debuff、回血、补共享盾，进入拖长战线。

当前问题：

- 慈悲杯与圣徒圣杯已经让过量治疗、实际治疗、士气和共有盾互相转换；下一步关注点是防线是否过厚以及收割速度是否足够。
- 如果对手 Debuff 少，`miracle-standard` 的共享盾收益会变低。

#### War Queen

成型方向：AP 前借、额外攻击、绝对伤害斩杀。

路线内容：

- Rank1 `royal-command`：1 AP 换本 turn +2 AP，下个己方 turn AP -1。
- Rank2：MaxHp +2、Attack +1、物防 +1；回合开始治疗后额外治疗低 HP 友方 1 HP。
- Rank3 后 Princess 攻击类型变为物理；`edict-of-victory` 给我方 1 体额外攻击，攻击后追加 Princess 当前 Attack 的绝对伤害，击败则获得 BP 并抵消 AP debt。

推荐组合：

- 搭配英雄：Radiant Berserker 是主要双 Rank3 多段爆发；Dread Cavalier、Abyssal Queen、Militia Foreman 分别提供冲锋、献祭追击和士兵攻击窗口。
- 士兵：Duelist 最优先，Arcanist 次优。
- 副官：Duelist 副官让宿主攻击后追加绝对伤害；Arcanist 副官适合魔法宿主，通过魔涌 + 再行动扩展节奏。
- 遗物：红磨刀石、决斗券、红沙漏；若绝对伤害占比高可借夜饵与掠食者王冠。

成型方式：

1. Rank1 解 `royal-command` 后，先把队伍构造成能花掉额外 AP 的形状。
2. Duelist Rank1 的物理攻击 +2 Aura 是 War Queen 后期转物理后的关键补强。
3. Rank3 后用 `edict-of-victory` 交给高攻击单位、带强攻 / 猛击单位、或已经能斩杀的单位。
4. 红沙漏让第三次物理主动攻击在同一伤害包中增加攻击者当前 Attack，是这条线的终局拼图。

当前问题：

- Rank3 前，`royal-command` 只是资源前借，要求队伍已经有足够行动可买。
- 如果没有 Duelist Aura 或红磨刀石，War Queen 自己的物理化收益不够明显。

### 2.2 Oracle

#### Astral Oracle

成型方向：魔法再行动、咏唱兑现、魔法溅射。

路线内容：

- Rank1 `star-reading`：让已经主动攻击过的我方魔法单位再次攻击。
- Rank2：MaxHp +2、魔防 +1；预见成功后，魔法单位获得咏唱，非魔法单位获得护咒。
- Rank3 `astral-alignment`：给魔法单位咏唱 + 额外攻击；下一次魔法伤害追加 Oracle 当前 Attack，并对相邻敌人造成追加值一半的魔法溅射。

推荐组合：

- 搭配英雄：Mage 二选一走 Stellar Archmage 或 Arcane Archivist，是主要双 Rank3 魔法核心；Wildspeaker 提供 Debuff，Militia Foreman 支持魔法士兵过渡阵容。
- 士兵：Arcanist 是核心，Cleric 负责防守，Shieldmaiden 保护蓄力单位。
- 副官：Arcanist 副官给魔涌并可再行动，适合 Mage、Oracle 或魔法化队伍核心。
- 遗物：见习星墨、回声水晶、指挥桌、星界棱镜；空心彗镜用于转入 Debuff。

成型方式：

1. 先找 Mage 或 Arcanist，让 `star-reading` 有可重复攻击的魔法核心。
2. Rank1 Arcanist Aura 给全体魔法攻击 +2，是这条线的基础数值。
3. Mage `arcane-channel` 或 Arcanist `astral-focus` 提供咏唱；回声水晶让每 turn 首次消耗后返还 1 层，Oracle Rank3 再把咏唱变成一轮爆发。
4. 空心彗镜把只打士气的魔法伤害转成空虚，让魔法蓄力自然接入 Debuff 体系。

当前问题：

- `star-reading` 要求目标已经攻击过，所以 AP 规划要求较高。
- 如果没有 Arcanist 或星墨，魔法再行动容易被士气吃掉，实际 HP 压力不足。

#### Fate Dealer

成型方向：标记、Debuff 计数、士气击穿、战栗控制。

路线内容：

- Rank1 `fate-mark`：标记敌人，使其下一次攻击我方时随机减半或 +1。
- Rank2：MaxHp +2、Attack +1。当前源码未见 `fate-mark` 路线的额外 Trait 强化 hook。
- Rank3 `thread-cut`：统计目标身上的印记 / 猎物 / 燃烧 / 空虚 / 力竭 / 磨损 / 战栗 / 脆弱等，每个造成 2 点士气伤害，超过当前士气的部分扣 HP；士气归零后再造成 Oracle 当前 Attack 的魔法伤害并施加战栗。

推荐组合：

- 搭配英雄：Arcane Archivist / Wildspeaker 是主要双 Rank3 状态核心；Dragon Raider 提供范围战栗与脆弱，Nightmare Fiend 提供猎物标记和绝对伤害收尾。
- 士兵：Arcanist 触发魔涌链，Duelist 负责物理收割。
- 副官：Arcanist 副官适合让 Oracle 自己获得魔涌与再行动；Duelist 副官适合把控制后窗口转成 HP 压力。
- 遗物：女巫铃、空心彗镜、夜饵、瘟疫法典 / 蚕食账簿；若与 Arcane Archivist 组队可借余烬星盘。

成型方式：

1. 用 Mage `searing-brand`、Druid `weakening-spores-action`、Monster `predatory-gaze` 或 Barbarian `challenge` 堆可计数状态；女巫铃把每 turn 第一个成功 Debuff 再扩成战栗。
2. `thread-cut` 先打士气，目标士气归零后才接魔法伤害。
3. 后续用 Duelist / Barbarian / Monster 对失去士气外壳的目标收 HP。

当前问题：

- Debuff / 控制体系遗物还没有接入，Fate Dealer 的终局闭环主要靠英雄而不是遗物。
- 需要多个状态源才能让 `thread-cut` 有爽感；单 Oracle 很难独立成型。
- Rank2 当前只有属性台阶，缺少把 `fate-mark` 提前变成 build 引擎的中期强化。

### 2.3 Peasant

#### Quartermaster

成型方向：低 AP 补给、坚守、队伍 HP 修复。

路线内容：

- Rank1 `supply-basket`：0 AP 治疗 1 HP，并赋予 2 turn 坚守。
- Rank2：MaxHp +4、物防 +1；播种触发时，低 HP 友方获得坚守。
- Rank3 `field-rations`：1 AP 全队按 Peasant 当前 Attack 的一半回血，主目标额外回血、获得坚守和护咒；若治疗前低于半血，净化 1 个 Debuff。

推荐组合：

- 搭配英雄：Abyssal Queen 是最直接的双 Rank3 献祭修复组合；Saint Queen / Holy Paladin 组成长线防线，Radiant Berserker 提供稳定自伤目标。
- 士兵：Shieldmaiden 与 Cleric。
- 副官：Shieldmaiden 副官使坚守 / 盾链保护低 HP 友方；Cleric 副官给护咒。
- 遗物：慈悲杯、白百合香炉、圣徒圣杯；石匠令与王墙军旗可把治疗线接入共有盾。

成型方式：

1. 早期靠 `supply-basket` 的 0 AP 稳住物理压力。
2. Shieldmaiden 的 `shield-drill` 会被补给篮的坚守触发，形成“主目标坚守 + 低 HP 友军坚守”。
3. Rank3 后 Peasant 当前 Attack 会影响全队治疗量，因此 Duelist Aura 与红磨刀石仍能间接放大后勤。

当前问题：

- 慈悲杯与圣徒圣杯已经把过量、士气和共有盾串成资源优势；需要实测群疗一次形成的总防护是否过高。
- 治疗不回士气，所以在士气被打空后的防守价值主要还是靠坚守 / 护咒。

#### Militia Foreman

成型方向：士兵再行动、民兵群攻、物理 / 魔法通吃。

路线内容：

- Rank1 `field-work`：丰收中可再攻击；播种中则自己回复 2 HP；否则获得播种。
- Rank2：MaxHp +3、Attack +1；丰收中造成 HP 伤害时，每 turn 1 次获得 1 BP。
- Rank3 `militia-call`：选择 Peasant 或士兵，获得额外主动攻击；物理单位获得强攻，魔法单位获得魔涌；下一次主动攻击额外 +Peasant 当前 Attack，若目标是士兵再 +士兵当前 Rank。

推荐组合：

- 搭配英雄：Wildspeaker 是最重要的双 Rank3 士兵体系搭档；War Queen 追加攻击，Astral Oracle 支持 Arcanist 士兵，Radiant Berserker 补物理多段输出。
- 士兵：Duelist、Arcanist 是输出核心，Shieldmaiden / Cleric 负责站场。
- 副官：通常不急着把核心士兵转副官，因为 `militia-call` 需要士兵在场攻击。
- 遗物：征募令、军士指挥印、连队军旗；物理士兵借红磨刀石，魔法士兵借见习星墨。

成型方式：

1. 先至少保留一名输出士兵在场，Duelist 或 Arcanist 都可以。
2. Rank1 士兵 Aura 是成型地基：Duelist 给物理 +2，Arcanist 给魔法 +2。
3. Rank3 后 `militia-call` 让士兵吃到强攻 / 魔涌、额外攻击和 Peasant 攻击力加成。
4. 连队军旗按在场士兵数提高每次士兵主动攻击，额外攻击同样兑现。

当前问题：

- 士兵专属遗物没有接入，导致这条线现在更像“借用物理 / 魔法连击遗物”。
- 招募、升级士兵都要吃奖励阶段，成型速度慢。

### 2.4 Mage

#### Stellar Archmage

成型方向：蓄力、咏唱、单点魔法爆发。

路线内容：

- Rank1 `arcane-channel`：本回合不能再攻击，下个己方回合获得 2 层咏唱。
- Rank2：MaxHp +2、Attack +1；Mage 攻击后赋予燃烧变为必定触发。
- Rank3 `starfall`：对敌方 1 体造成 Mage 当前 Attack 的魔法伤害并赋予燃烧。

推荐组合：

- 搭配英雄：Astral Oracle 是最重要的双 Rank3 魔法爆发搭档；War Queen 提供额外攻击与绝对收尾，Wildspeaker 补 Debuff，Saint Queen 保护蓄力回合。
- 士兵：Arcanist 是核心，Shieldmaiden 保护蓄力，Cleric 补护咒。
- 副官：Arcanist 副官让 Mage 使用魔法链后获得魔涌并可再行动。
- 遗物：见习星墨、回声水晶、指挥桌、星界棱镜；若队伍稳定叠燃烧可借余烬星盘与灰烬引爆器。

成型方式：

1. Rank1 先用 `arcane-channel` 做下回合爆发预备。
2. Arcanist Rank1 Aura 与星墨会直接提高 Mage 当前 Attack。
3. Rank2 后普通攻击稳定上燃烧；余烬星盘让本回合首次燃烧再补 1 层。
4. Rank3 后 `starfall` 是稳定当前攻击力魔法伤害 + 燃烧入口，适合接咏唱 / 魔涌 / Oracle 再行动。

当前问题：

- `arcane-channel` 有一回合延迟，需要 Shieldmaiden / Cleric 帮忙护住。
- 没有咏唱或魔涌时，`starfall` 只是稳定魔法伤害，爽感依赖配套。

#### Arcane Archivist

成型方向：燃烧、空虚、Debuff 数量、归档爆发。

路线内容：

- Rank1 `searing-brand`：敌方 1 体获得燃烧和空虚。
- Rank2：MaxHp +2、魔防 +1；Mage 攻击触发燃烧时额外赋予空虚。
- Rank3 `archive-formula`：敌方获得归档刻印，并受到 Mage 当前 Attack 的魔法伤害；若已有燃烧，伤害额外 +燃烧层数；随后按目标当前通用 Debuff 数量施加燃烧，至少 1 层。

推荐组合：

- 搭配英雄：Fate Dealer / Wildspeaker 是主要双 Rank3 状态组合；Astral Oracle 走魔法再行动，Dragon Raider 补战栗与脆弱。
- 士兵：Arcanist 必备度最高；Duelist 可负责目标士气击穿后的物理 / 绝对伤害收割。
- 副官：Arcanist 副官最优，Duelist 副官用于后续收 HP。
- 遗物：见习星墨、余烬星盘、灰烬引爆器；与 Fate Dealer 组队时借女巫铃、空心彗镜与瘟疫法典 / 蚕食账簿。

成型方式：

1. 早期用 `searing-brand` 保证燃烧 + 空虚两个状态。
2. 余烬星盘让首次燃烧额外 +1 层，快速达到灰烬引爆器的 3 层门槛。
3. `archive-formula` 先按燃烧层数增伤，再按 Debuff 数量反过来加燃烧层数，是当前最完整的燃烧闭环。
4. 灰烬引爆器把 3 层以上燃烧一次结算为层数 x2 的魔法伤害。

当前问题：

- 引爆会清空燃烧，施放前要确认本回合能达到 3 层并值得结算，不能见到燃烧就提前消费。

### 2.5 Druid

#### Grove Keeper

成型方向：净化、护咒、范围治疗。

路线内容：

- Rank1 `cleansing-herbs`：净化我方 1 个 Debuff 并治疗 1 HP。
- Rank2：MaxHp +3、魔防 +1；净化草药成功后给护咒，低于半血时额外治疗 1 HP。
- Rank3 `grove-sanctuary`：目标净化全部 Debuff，相邻我方各净化 1 个；若净化成功，范围内每人按 Druid 当前 Attack x 净化总数治疗；若没有净化，改为范围护咒。

推荐组合：

- 搭配英雄：Saint Queen / Quartermaster 是主要双 Rank3 续航组合；Holy Paladin 放大防线，Fate Dealer 提供独立的士气推进胜法。
- 士兵：Cleric、Shieldmaiden。
- 副官：Cleric 副官让净化成功后再给护咒；Shieldmaiden 副官补坚守。
- 遗物：慈悲杯、白百合香炉、圣徒圣杯；可借石匠令把护咒链接入共有盾。

成型方式：

1. 只有对面确实会挂 Debuff 时，优先走这条线。
2. Cleric + Druid 可以把一次净化转成治疗、护咒、可能的额外护咒。
3. Rank3 是反 Debuff 队的终局按钮，Debuff 越多治疗越高。

当前问题：

- 治疗 / 净化的遗物转换已经接入；这条线剩余风险是对局没有 Debuff 时，Grove Keeper 本人的终局按钮仍偏防守。
- 如果对局 Debuff 很少，Grove Keeper 的终局价值会变成范围护咒，强度明显下降。

#### Wildspeaker

成型方向：Debuff、猎群标记、士兵协同。

路线内容：

- Rank1 `weakening-spores-action`：移除敌方可驱散 Buff，并施加力竭和磨损。
- Rank2：MaxHp +2、Attack +1；Druid Trait 未能移除 Buff 时，额外按目标攻击类型施加脆弱或空虚。
- Rank3 `call-the-hunt`：目标及相邻敌人获得猎群标记；每名我方士兵首次主动攻击每个标记目标时，额外 +Druid 当前 Attack 的普通伤害；标记目标被击败时获得 1 BP。

推荐组合：

- 搭配英雄：Militia Foreman 是最重要的双 Rank3 士兵搭档；Nightmare Fiend 利用猎群攻击制造猎物窗口，Fate Dealer / Arcane Archivist 消费状态数量。
- 士兵：Duelist、Arcanist，最好至少两名在场。
- 副官：一般优先保留士兵在场；若只需要 Aura，可把 Rank2 士兵转副官。
- 遗物：征募令、军士指挥印、连队军旗；Debuff 侧可借女巫铃、空心彗镜与瘟疫法典 / 蚕食账簿。

成型方式：

1. 早期 `weakening-spores-action` 降低敌方输出，同时触发 Arcanist 共鸣。
2. 中期招募并升级士兵，至少让 Duelist / Arcanist 到 Rank1。
3. Rank3 后先挂 `call-the-hunt`，再让多个士兵分别攻击标记目标。

当前问题：

- 士兵专属遗物没有接入，导致这条线缺少“多士兵攻击”的遗物奖励。
- 如果只有 1 名士兵，`call-the-hunt` 的收益不够终局。

### 2.6 Barbarian

#### Radiant Berserker

成型方向：强攻、额外攻击、战斧余波群伤。

路线内容：

- Rank1 `war-cry`：自身获得狂怒，物防 / 魔防 -2，额外攻击 +1；击破共有盾时对攻击对象造成当前攻击力绝对伤害。
- Rank2：MaxHp +2、Attack +1；狂怒中触发战斧余波时，余波最低 2。
- Rank3 `glory-roar`：获得强攻，并按 Barbarian 当前 Attack / 3 向上取整获得额外主动攻击次数；每次主动攻击后失去 1 HP，不低于 1。

推荐组合：

- 搭配英雄：War Queen 是最重要的双 Rank3 多段爆发搭档；Quartermaster / Saint Queen 修复自伤，Abyssal Queen 增加献祭与绝对追击。
- 士兵：Duelist 放大物理攻击，Cleric 修复自伤和低防风险。
- 副官：Duelist 副官提供物攻 +2、攻击后绝对伤害；Cleric 副官提供护咒。
- 遗物：红磨刀石、决斗券、红沙漏。

成型方式：

1. Duelist Rank1 Aura 是最早的火力台阶。
2. `war-cry` 提供第二次攻击，为红沙漏的第三次物理攻击做准备。
3. Rank3 后 `glory-roar` 把当前 Attack 转成额外攻击次数；所有攻击力遗物和 Aura 都会放大它。

当前问题：

- 自降双防很危险，缺 Shieldmaiden / Cleric 时容易被反手惩罚。
- 红沙漏要求本回合第 3 次主动攻击，Rank3 前较难稳定触发。

#### Dragon Raider

成型方向：破盾、战栗、脆弱、物理开路。

路线内容：

- Rank1 `challenge`：敌方 1 体获得战栗，无法反击。
- Rank2：MaxHp +3、物防 +1；战斧余波触发时，相邻敌人获得战栗。
- Rank3 `dragon-breaker`：若敌方有共享盾，减少其共享盾 Barbarian 当前 Attack；若击破，目标及相邻敌人获得战栗和脆弱。若敌方无盾，则造成当前 Attack 物理伤害并施加战栗和脆弱。

推荐组合：

- 搭配英雄：Fate Dealer 是主要双 Rank3 士气压制搭档；Dread Cavalier 组成破盾冲锋，War Queen 负责破盾后收尾，Arcane Archivist 消费战栗 / 脆弱计数。
- 士兵：Duelist、Shieldmaiden。
- 副官：Duelist 副官提高 HP 压力；Shieldmaiden 副官提高站场。
- 遗物：红磨刀石、决斗券、绿旗、红沙漏；状态侧可借女巫铃、空心彗镜与瘟疫法典 / 蚕食账簿。

成型方式：

1. 早期用 `challenge` 创造无反击攻击窗口。
2. 中期用 Duelist Aura 和红磨刀石提高 `dragon-breaker` 破盾量。
3. Rank3 后破盾成功会给范围战栗 + 脆弱；绿旗返还 1 AP，让物理队在同一 turn 继续收割。

当前问题：

- 对手不使用共享盾时，Dragon Raider 更像单体控制打手。
- 26 件目标池不为破盾单独设置 Epic；Rare 绿旗负责把首次破盾转成 1 AP，终局仍借物理连击或 Debuff 收割完成。

### 2.7 Monster

#### Nightmare Fiend

成型方向：0 HPDamage、猎物、绝对伤害追击。

路线内容：

- Rank1 `predatory-gaze`：敌方 1 体获得猎物，本回合每次受到 0 HP 伤害时追加 2 绝对伤害。
- Rank2：MaxHp +3、魔防 +1；Monster Trait 对猎物目标触发时追加绝对伤害 +1。
- Rank3 `nightmare-stare`：目标及相邻敌人获得噩梦猎物；每个目标第一次受到 0 HP 伤害时，追加 Monster 当前 Attack 的绝对伤害并消耗；主目标额外获得磨损。

推荐组合：

- 搭配英雄：Fate Dealer / War Queen 是主要双 Rank3 收束搭档；Dread Cavalier 制造只打士气的窗口，Wildspeaker 连接猎群与猎物体系。
- 献祭组件：Princess 通常保持 Rank0 / Rank1，不占双 Rank3 核心名额；死亡后应先让 Monster 获得野兽之怒，再施放 `nightmare-stare`。
- 士兵：Duelist 用物理 Aura 和副官补 HP 伤害，Cleric 维持 Monster，Arcanist 可触发魔法侧再行动。
- 副官：Duelist 副官让 Monster 攻击后追加 2 绝对伤害；Cleric 副官补防。
- 遗物：夜饵、掠食者王冠；红磨刀石提高物理 Monster 当前 Attack。走公主献祭时再取丧仪金币。

成型方式：

1. 用共享盾、士气或高防造成 0 HPDamage，反而触发猎物追击。
2. `predatory-gaze` 适合挂给高防 / 有士气的目标。
3. Rank3 后用范围噩梦猎物制造多个“只要没扣 HP 就直扣 HP”的反外壳窗口。

当前问题：

- 夜饵与掠食者王冠已经接入；现在的主要门槛是玩家能否从预测中理解“0 HPDamage 反而触发追击”。
- 这条线理解成本高，预测必须清楚显示 0 HPDamage 与绝对追击。

#### Abyssal Queen

成型方向：献祭、契约、额外攻击、绝对伤害斩杀。

路线内容：

- Rank1 `dark-pact`：我方 1 体失去最多 4 HP，至少保留 1；获得契约，下一次主动攻击后追加 4 绝对伤害；若低于半血，获得 1 BP。
- Rank2：MaxHp +4、Attack +1。当前源码未见 `dark-pact` 路线的额外 Trait 强化 hook。
- Rank3 `abyssal-bargain`：我方 1 体失去最多 Monster 当前 Attack 的 HP，获得额外主动攻击；下一次攻击后追加 Monster 当前 Attack 的绝对伤害，击败则获得 1 BP，并按支付 HP 治疗 Monster。

推荐组合：

- 搭配英雄：Quartermaster / Saint Queen 是主要双 Rank3 修复端；War Queen 扩大额外攻击和绝对伤害，Radiant Berserker 提供高频攻击宿主。
- 献祭组件：Princess 通常保持 Rank0 / Rank1；`dark-pact` / `abyssal-bargain` 可以把她压到 1 HP，但当前规则不会由 HP 支付直接击败她。
- 士兵：Cleric 修复支付 HP，Duelist 提供物理 Aura 和副官追击。
- 副官：Cleric 副官适合宿主治疗链；Duelist 副官适合把攻击变成双绝对追击。
- 遗物：血币、夜饵、掠食者王冠；慈悲杯 / 白百合香炉修复 HP 代价。走公主献祭时再取丧仪金币。

成型方式：

1. 早期 `dark-pact` 用在能安全攻击的单位上，压低 HP 换 BP 和绝对伤害；血币让每 turn 第一次真实 HP 支付同时建立等量共有盾。
2. Cleric / Princess / Peasant 负责修复支付后的 HP。
3. Rank3 后 `abyssal-bargain` 最好交给高攻击或可再行动单位，追击绝对伤害直接绕过士气。

当前问题：

- 血币与丧仪金币已经接入；需要观察 HP 支付转盾后是否让契约的风险回报过于偏向收益。
- Rank2 当前只有属性台阶，献祭线从 Rank1 到 Rank3 中间缺少明显的引擎升级。
- 支付 HP 不走士气，错误使用会把核心送进斩杀线。

#### 跨路线 Build：野兽继承 / 公主献祭

定位：把低投入 Princess 从前期支援位转换为 Monster 的永久野兽之怒，并由 Monster 与另一名英雄组成真正的双 Rank3 核心。

基本阵容：

- 猎物方向：Nightmare Fiend Rank3 + Fate Dealer / Dread Cavalier Rank3 + Duelist Rank2 + Princess Rank0 / Rank1。
- 契约方向：Abyssal Queen Rank3 + Quartermaster Rank3 + Cleric Rank2 + Princess Rank0 / Rank1。
- Princess 不绑定关键副官，也不继续投入 Rank2 / Rank3 训练；第一份免费训练可以选择祈祷维持前期，或选择王令换一次进攻节奏。

成型流程：

1. 公主存活阶段，Monster 的 0 HPDamage 追击获得 +1；先利用公主 Trait / Rank1 Role Action 和士兵能力完成过渡。
2. Abyssal Queen 可以用 HP 支付把公主压到 1 HP；Nightmare Fiend 方向则通过站位和防守优先级主动把公主暴露为敌方目标。现有 HP 支付不能直接击败公主。
3. 公主死亡后，Monster 失去“公主存活追击 +1”，并永久获得野兽之怒：攻击 +3、物防 / 魔防 -2。对动态追击而言净收益是 +2 攻击力，但 Monster 更容易被反杀。
4. `nightmare-stare` 与 `abyssal-bargain` 都在施放时记录 Monster 当前攻击力，因此应在野兽之怒触发后施放。
5. 猎物方向由 Fate Dealer / Dread Cavalier 制造 Debuff、低士气和 0 HPDamage 窗口；契约方向由 Quartermaster / Cleric 修复剩余队伍并让高频攻击手兑现绝对伤害。

遗物地基：

- 丧仪金币在公主阵亡且 Monster 存活时持续提供 AP 上限 +2；不是一次性 BP。
- 血币把每 turn 第一次技能 HP 支付转为等量共有盾，帮助契约方向度过支付后的反击窗口。
- 夜饵把 0 HPDamage 转成猎物入口。
- 两个 Monster 方向都可借夜饵与掠食者王冠兑现绝对伤害；契约方向额外借慈悲杯等治疗组件修复 HP 代价。

权重信号：

- Monster 在 Rank0 时同时提供两条可能路线的 tag，其中包含 `sacrifice`；不需要等 Rank3 或公主死亡。
- Monster 锁定任一路线后仍保留 `sacrifice` tag，因此 Nightmare Fiend 与 Abyssal Queen 都可能看到丧仪金币。
- Princess、`beast-rage`、第二名 Rank3 和公主是否已经死亡都不追加权重；其他路线遗物始终保留基础权重。

### 2.8 Knight

#### Holy Paladin

成型方向：守护誓约、共享盾、双防防线。

路线内容：

- Rank1 `guard-oath`：给自己以外的我方单位守护誓约，受主动物理攻击时伤害 -2 并消耗 1 层。
- Rank2：MaxHp +4、魔防 +1；替身之盾触发后，被保护目标获得坚守；若目标已有守护誓约，Knight 获得护咒。
- Rank3 `holy-bastion`：给我方 1 体守护誓约层数，层数为 Knight 当前物防至少 2，并给护咒；Knight 自身获得坚守；共享盾增加 Knight 当前物防 + 魔防。

推荐组合：

- 搭配英雄：Saint Queen / Quartermaster 是主要双 Rank3 防线；Grove Keeper 补范围净化，Fate Dealer 提供不依赖物理攻击的士气推进。
- 士兵：Shieldmaiden、Cleric。
- 副官：Shieldmaiden 副官能让盾 / 坚守链保护低 HP 友方；Cleric 副官给护咒。
- 遗物：石匠令、慈悲杯、白百合香炉、指挥桌、王墙军旗；治疗较多时可借圣徒圣杯。

成型方式：

1. Shieldmaiden Rank1 Aura 提高物防，也提高 `holy-bastion` 的守护层数价值。
2. Cleric Aura 补魔防，白百合香炉把主动治疗继续转成护咒。
3. 王墙军旗按我方最高物防 +最高魔防提供每回合基础盾，双防 Aura 都能进入成长。

当前问题：

- 完全防守可能缺少收割手段，建议带一个 Duelist / Barbarian / Mage 作为输出端。

#### Dread Cavalier

成型方向：共享盾转伤害、铁壁冲锋、破盾后反打。

路线内容：

- Rank1 `raise-bulwark`：我方有共享盾时可发动，共享盾 x1.5 向上取整，并给共享盾物防 +2。
- Rank2：MaxHp +3、Attack +1；我方共享盾每己方 turn 首次被击破后，Knight 获得强攻。
- Rank3 `iron-charge`：消耗我方全部共享盾，对敌方 1 体造成消耗盾值 + Knight 当前 Attack 的物理伤害并施加战栗；若造成 HP 伤害，返还等同该 HPDamage 的共享盾。

推荐组合：

- 搭配英雄：War Queen / Fate Dealer 是主要双 Rank3 收束搭档；Nightmare Fiend 利用 0 HPDamage 窗口，Dragon Raider 连接破盾、战栗与脆弱。
- 士兵：Shieldmaiden 是核心，Duelist 提供物理 Aura。
- 副官：Shieldmaiden 副官增强盾链；Duelist 副官提高铁壁冲锋后的收割。
- 遗物：石匠令、王墙军旗；进攻侧使用红磨刀石、决斗券、绿旗和红沙漏。

成型方式：

1. 先有共享盾来源：基础部署、Shieldmaiden `aegis-formation`、王墙军旗。
2. `raise-bulwark` 增厚盾并触发 Shieldmaiden / 石匠令。
3. Rank3 后把大盾转为 `iron-charge` 伤害；如果打进 HP，会返还共享盾继续防守。

当前问题：

- 铁壁冲锋要求我方已有盾，且普通伤害先扣士气。
- 如果只打士气，返盾不会触发，因为返盾看真实 HPDamage。

## 3. 士兵与副官索引

### 3.1 Cleric

定位：治疗 / 净化响应，魔防 Aura，护咒链。

关键能力：

- Rank1 Aura：我方全体魔防 +1。
- Trait：我方主动治疗或净化成功时，目标获得护咒；Rank1 后若目标低于半血，额外治疗 3 HP。
- Rank2 `mend`：1 AP 净化 1 个 Debuff 并治疗 3 HP；若净化成功，目标获得 2 turn 护咒。
- 副官：宿主治疗或净化成功后，目标获得护咒；目标低于半血时护咒持续更久。

适合 build：

- Saint Queen、Grove Keeper、Quartermaster、Abyssal Queen、Radiant Berserker。

成型判断：

- 如果队伍靠支付 HP、自伤、蓄力或长线防守，Cleric 是高优先级。
- 圣疗 / 净化遗物已经接入；Cleric 仍是体系稳定器而不是单独胜利条件，这符合士兵定位。

### 3.2 Shieldmaiden

定位：共享盾 / 坚守响应，物防 Aura。

关键能力：

- Rank1 Aura：我方全体物防 +1。
- Trait：每己方 turn 1 次，我方共享盾增加或我方角色获得坚守时，当前 HP 比例最低友军获得坚守。
- Rank2 `aegis-formation`：1 AP；无盾时获得共享盾 1，有盾时共享盾 +2。
- 副官：宿主触发盾 / 坚守链后，低 HP 友军获得坚守。

适合 build：

- Holy Paladin、Dread Cavalier、Quartermaster、Saint Queen、Stellar Archmage。

成型判断：

- 如果 build 需要蓄力、拖回合、铁壁冲锋或保护低 HP 核心，Shieldmaiden 是最稳定士兵。
- Rank1 Aura 即可明显改善队伍抗物理，Rank2 后可转副官但会失去在场攻击与 Role Action。

### 3.3 Duelist

定位：物理 Aura、强攻、猛击、绝对追击。

关键能力：

- Rank1 Aura：我方物理攻击单位攻击 +2。
- Trait：自身主动攻击第一次对敌人造成 HP 伤害后，获得强攻；Rank1 后若目标仍存活，攻击后追加 2 绝对伤害。
- Rank2 `crimson-lunge`：1 AP，我方物理单位获得猛击，下一次主动物理伤害 x2。
- 副官：宿主物攻 +2；宿主主动攻击后，若目标仍存活，追加 2 绝对伤害；若本次造成 HP 伤害，宿主获得强攻。

适合 build：

- War Queen、Radiant Berserker、Dragon Raider、Militia Foreman、Nightmare Fiend、Abyssal Queen、Dread Cavalier。

成型判断：

- 当前物理 build 的地基几乎都经过 Duelist。
- Duelist 副官特别适合高频攻击或需要补直击 HP 的宿主。

### 3.4 Arcanist

定位：魔法 Aura、魔涌、咏唱、魔法再行动。

关键能力：

- Rank1 Aura：我方魔法攻击单位攻击 +2。
- Trait：每己方 turn 1 次，我方魔法伤害或魔法状态链触发时，魔攻最高的我方英雄获得魔涌；Rank1 后扩展到我方全体魔法单位，Role Action 触发时持续更久。
- Rank2 `astral-focus`：1 AP，我方魔法单位获得咏唱，下一次魔法伤害 x2。
- 副官：宿主魔攻 +2；触发后宿主获得魔涌并可再行动一次。

适合 build：

- Astral Oracle、Stellar Archmage、Arcane Archivist、Fate Dealer、Wildspeaker、魔法侧 War Queen。

成型判断：

- 当前魔法 build 的最关键士兵。
- 如果队伍有 Mage 或 Oracle 星读线，Arcanist Rank1 往往比再招一个英雄更快让 build 成形。

## 4. 当前已接入遗物索引

当前 `RelicCatalog` 已收束为 26 件。下面只列战斗判断需要的短口径，精确实现边界见 `TinyPixelFights_Relic_Design_20260708.md`。

### 4.1 Common：方向信号

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 见习星墨 | 4 | 我方魔法攻击单位攻击 +1。 | 魔法、燃烧、咏唱。 |
| 石匠令 | 3 | 每回合首次增加共有盾时，低 HP 友方获得坚守。 | 盾墙、补给、防守。 |
| 红磨刀石 | 3 | 我方物理攻击单位攻击 +1。 | 物理、连击、Monster、Knight 冲锋。 |
| 征募令 | 2 | 士兵招募成本 -1 BP，最低 1。 | 士兵团、早期扩编。 |
| 慈悲杯 | 4 | 每回合首次过量治疗按治疗者攻击力上限转为共有盾。 | 治疗、盾墙、契约修复。 |
| 女巫铃 | 3 | 每回合首次成功赋予敌方 Debuff 时追加战栗。 | Debuff、控制、炎上交叉。 |

### 4.2 Rare：引擎补件

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 余烬星盘 | 5 | 每回合首次赋予燃烧时，额外 +1 层。 | 燃烧堆层。 |
| 空心彗镜 | 5 | 每回合首次只击伤士气的魔法伤害，赋予空虚。 | 魔法士气、Fate、Mage。 |
| 白百合香炉 | 5 | 每回合首次主动治疗实际回复 HP 时，最高治疗目标获得护咒。 | 治疗、护咒、盾墙。 |
| 决斗券 | 5 | 每回合首次攻击无盾敌人前，物理攻击者获得强攻。 | 物理连击。 |
| 军士指挥印 | 5 | 每回合首次使用士兵 Role Action 后返还 1 AP。 | 士兵团、行动经济。 |
| 夜饵 | 5 | 每回合首次对敌人造成 0 HP 伤害时赋予猎物。 | 猎物、绝对伤害。 |
| 指挥桌 | 6 | 每回合首次使用 2 AP Role Action 后返还 1 AP。 | 各类 Rank3 终结技。 |
| 回声水晶 | 5 | 每回合首次消耗咏唱后，在伤害结算完毕时返还 1 层。 | 魔法蓄力。 |
| 绿旗 | 5 | 每回合首次击破敌方共有盾后返还 1 AP。 | 破盾、物理节奏。 |
| 血币 | 5 | 每回合首次支付 HP 后获得等量共有盾。 | 契约、公主献祭。 |

### 4.3 Epic：终局放大器

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 星界棱镜 | 8 | 首次消耗咏唱造成魔法伤害时，追加施术者当前攻击力的魔法伤害。 | 魔法蓄力。 |
| 灰烬引爆器 | 8 | 首次命中 3 层以上燃烧时，消耗全部层数并造成层数 x2 的无视魔防魔法伤害。 | 燃烧爆发。 |
| 瘟疫法典 | 8 | 首次新增第 3 种或更多 Debuff 时，造成种类数绝对伤害。 | Debuff 收割。 |
| 蚕食账簿 | 8 | 常规 Debuff 总层数大于 8 时，主动攻击追加 1 点绝对伤害；大于 12 时追加 2 点。 | Debuff 蚕食。 |
| 掠食者王冠 | 8 | 我方对猎物造成的绝对伤害 x1.5，向上取整。 | 猎物、绝对伤害。 |
| 红沙漏 | 8 | 每回合第 3 次物理主动攻击增加攻击者当前攻击力。 | 多段物理爆发。 |
| 王墙军旗 | 8 | 己方回合开始且共有盾为 0 时，按最高物防 + 最高魔防复盾。 | 盾墙复盾。 |
| 圣徒圣杯 | 8 | 首次主动治疗让最高治疗目标恢复等量士气，并按实际 HP + 士气恢复量增加共有盾。 | 治疗净化。 |
| 连队军旗 | 8 | 士兵主动攻击伤害增加我方在场士兵数。 | 士兵团。 |
| 丧仪金币 | 8 | 公主阵亡且我方怪物存活时，AP 上限 +2。 | 公主献祭。 |

## 5. 当前实装与 pacing 诊断

### 5.1 26 件目标池中的九个体系终点

- 魔法蓄力：星界棱镜。
- 炎上结算：灰烬引爆器。
- Debuff 收割：瘟疫法典（状态种类）或蚕食账簿（持续层数）。
- 绝对伤害：掠食者王冠。
- 物理连击：红沙漏。
- 共有盾：王墙军旗。
- 治疗净化：圣徒圣杯。
- 士兵团：连队军旗。
- 公主献祭：丧仪金币。

九个体系共用 16 件 Common / Rare。BP、AP、咏唱、猎物、护咒等是连接体系的接口，不额外占用终局遗物名额。

### 5.2 当前源码状态

26 件遗物池已经完成：

- 原 25 件遗物完成迁移后，新增 `relic-attrition-ledger` 作为第 26 件终局遗物。
- 主动治疗、Debuff 赋予、HP 支付和咏唱消费使用通用事件边界，不依赖某一个英雄的专用判断。
- 中日文本、HUD 图标、日志、奖励成本、AP 上限与攻击预测已经同步。
- 当前没有遗物内容缺口，后续工作转为真实对局的出现率、pacing 与数值验证。

### 5.3 奖励 pacing 风险

当前源码中奖励窗口是 round 4 起每 4 round 一次。这会让需要多个奖励阶段的 build 明显变慢：

- 士兵团至少需要招募士兵、升级士兵、再拿英雄 Rank3 或遗物，成型点偏晚。
- 九个体系的组件与终局遗物都已进入同一个 26 件池；Debuff 体系拥有状态种类与持续层数两个终点，随机权重仍不提供硬保底。
- 宽松权重不会保证刷到当前路线遗物；这是保留转型和随机感的预期结果，不应再加硬保底修正。
- Epic 从第一次选择就可能出现，但早期 BP 不足时会保持不可购买状态；round 12 后仅提高出现率。

## 6. 实用成型模板

下表是双 Rank3 左右的主要成型点。终局遗物只有一件；“组件”可以跨体系借用，不是套装锁。

| 体系 | 双 Rank3 核心 | 士兵 / 副官 | 共享组件 | 终局遗物 | 战斗循环 |
|---|---|---|---|---|---|
| 魔法蓄力 | Astral Oracle + Stellar Archmage | Arcanist Rank2 | 见习星墨、回声水晶、指挥桌 | 星界棱镜 | 先给咏唱 / 魔涌；每 turn 首次消耗咏唱后返还 1 层，并追加一段当前 Attack 魔法伤害。 |
| 炎上结算 | Arcane Archivist + Astral Oracle / Fate Dealer | Arcanist Rank2 | 见习星墨、余烬星盘、空心彗镜 | 灰烬引爆器 | 用 Role Action 与 Debuff 把燃烧叠到 3 层以上，再一次结算层数 x2。 |
| Debuff 收割 | Fate Dealer + Arcane Archivist / Wildspeaker / Dragon Raider | Arcanist 或 Duelist | 女巫铃、空心彗镜、夜饵 | 瘟疫法典 / 蚕食账簿 | 瘟疫法典奖励三种以上不同 Debuff；蚕食账簿在持续层数超过 8 / 12 后把每次主动攻击转成 1 / 2 点绝对伤害。 |
| 绝对伤害 | Nightmare Fiend + War Queen / Fate Dealer | Duelist Rank2 | 红磨刀石、决斗券、夜饵 | 掠食者王冠 | 让共有盾、士气或高防制造 0 HPDamage，再把所有对猎物的既有绝对追击放大至 x1.5。 |
| 物理连击 | War Queen + Radiant Berserker / Dread Cavalier | Duelist Rank2 | 红磨刀石、决斗券、绿旗 / 指挥桌 | 红沙漏 | 破盾或 2 AP 行动返还 AP，用再行动与低 Cost 物理单位凑第三次主动攻击，再增加攻击者当前 Attack。 |
| 共有盾 | Holy Paladin + Dread Cavalier / Quartermaster | Shieldmaiden Rank2 | 石匠令、慈悲杯、绿旗 / 指挥桌 | 王墙军旗 | 双防 Aura 提高每 turn 基础盾；治疗溢出继续补盾，Dread Cavalier 可把盾转伤害。 |
| 治疗净化 | Saint Queen + Grove Keeper / Quartermaster | Cleric Rank2 | 慈悲杯、白百合香炉、石匠令 | 圣徒圣杯 | 慈悲杯把过量治疗转盾；圣杯让最高单体实际治疗同时恢复等量士气，并按实际恢复的 HP +士气增加共有盾。 |
| 士兵团 | Militia Foreman + Wildspeaker | 至少 2 名在场士兵 | 征募令、军士指挥印、红磨刀石 / 见习星墨 / 绿旗 | 连队军旗 | 保留士兵在场，Role Action 或物理破盾返 AP，所有士兵攻击按在场士兵数增伤。 |
| 公主献祭 | Monster Rank3 + 兼容第二 Rank3；Princess 保持 Rank0 / Rank1 | Duelist 或 Cleric | 血币、夜饵、攻击 / 治疗组件 | 丧仪金币 | HP 支付先转成盾度过反击；公主阵亡后 Monster 获得野兽之怒，并在存活期间由丧仪金币持续提供 AP 上限 +2。 |

## 7. 遗物实装与后续验证

当前已经完成：

1. `RelicCatalog` 收束为 26 件，并完成 16 件共享组件与 10 件 Epic 终点；Debuff 体系保留两条不同收束方向。
2. 复用英雄路线 `RelicTags` 单次 x2 加权，不增加 Rank3 判定、状态信号或硬路线过滤。
3. 所有 rarity 始终可见；round 12 只把 Epic 基础权重从 5 提高到 10。
4. 遗物继续遵守伤害边界：普通伤害先打士气；绝对伤害直扣 HP；写 HP 伤害的触发只看真实 `HpDamage`。

下一轮完整对局重点记录：第一件相关遗物出现回合、跨路线遗物购买率、Epic 早期购买率、双 Rank3 成型回合、治疗队平均结束回合和公主献祭实际达成率。先根据这些指标调整数值与权重，不增加新过滤层。
