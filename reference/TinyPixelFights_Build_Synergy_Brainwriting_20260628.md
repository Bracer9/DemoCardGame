# Tiny Pixel Fights — Advanced Class & Build Synergy Brainwriting

日期：2026-06-28  
阶段：BP Core 与 Dummy Reward Window 已完成后，准备进入英雄/普通兵/Role Action/升级系统前  
关联文档：

- `GDD.md`
- `reference/TinyPixelFights_Action_Ability_Brainwriting_20260628.md`
- `reference/TinyPixelFights_PvP_GrowthPrototype_Plan_20260628.md`
- `reference/TinyPixelFights_BP_System.md`
- `reference/TinyPixelFights_Dummy_Reward_Window.md`

本文不是最终规则表，而是 build brainwriting。目标是把英雄进阶、普通兵进阶、副官、BP 奖励、Role Action、Trait、遗物和天气可能产生的构筑快感尽量展开，方便之后挑选低风险路线进入 prototype。

---

## 1. 总原则

### 1.1 构筑快感来自“行动身份”，不是只来自数值叠加

如果进阶 class 只是攻击 +1、HP +2、物防 +1，确实容易平衡，但不会产生 JRPG 养成感。玩家想看到的是：

- 公主从“会祈祷的低攻角色”变成“大神官”或“暗黑女王”。
- 骑士从“会代伤的肉盾”变成“圣骑冲锋”或“黑骑突袭”。
- 德鲁伊从“攻击附带衰弱”变成“森林守护者”或“野性猎群指挥者”。
- 普通兵从工具人变成能作为副官改变英雄手感的队伍成员。

所以每个进阶方向都应该尽量回答：

1. 它改变了玩家这一回合想做什么？
2. 它让哪个资源变得更有价值？
3. 它和哪些队友互相放大？
4. 它有什么明显反制或代价？

### 1.2 每张卡仍然最多一个 Role Action

为了防止 UI 和认知爆炸，依然采用上一份文档的原则：

```text
每张卡：
- 一定拥有 Attack。
- 最多拥有 1 个 Role Action。
- 可以拥有若干 Trait / Attack Trigger / Aura / Modifier。
```

进阶 class 不应该让卡牌多出一排按钮，而应该强化、替换或扭转已有 Role Action。

### 1.3 构筑轴

后续 build 可以围绕这些轴展开：

| 构筑轴 | 代表行为 | 典型收益 | 典型风险 |
|---|---|---|---|
| Shield / 盾轴 | 加盾、护盾反击、破盾奖励 | 防守、拖时间、保护关键角色 | 容易拖局，被反盾/绝对伤害克制 |
| Heal / 治疗轴 | 治疗、净化、超量生命 | 容错、长线成长 | 对爆发较弱，可能节奏慢 |
| AP Tempo / AP 节奏 | 本回合 AP 增减、下回合还债 | 爆发回合、连动 | 很容易失控成先手滚雪球 |
| BP Economy / BP 经济 | 额外 BP、折扣、skip 收益 | 更快成型 | 可能变成不战斗也赚钱 |
| Mark / 标记轴 | 标记目标、集中火力 | 战术焦点、斩杀 | 被净化或目标死亡打断 |
| Burn / DoT 轴 | 炎上、延迟伤害 | 跨回合压力 | 需要 UI 清楚，容易被净化 |
| Dispel / Cleanse 轴 | 驱散敌方 buff，净化己方 debuff | 反构筑能力强 | 空放无效，需要目标有状态 |
| Charge / 蓄力轴 | 放弃当回合换下回合爆发 | 规划感强 | 被集火/驱散/延迟惩罚 |
| Sacrifice / 代价轴 | 失去 HP、牺牲盾/BP 换强效 | 强烈个性，爆发高 | 平衡危险，可能自毁 |
| Fate / 随机控制 | 重掷、锁定、概率改写 | 选择感、惊喜 | 如果太随机会失去公平感 |
| Position / 站位轴 | 邻接、边缘、前排后排 | 战术空间 | 当前 UI 尚未强化站位 |
| Type / 类型轴 | 物理/魔法/绝对、防御弱点 | 目标选择 | 数值表复杂度上升 |

---

## 2. 名单总览

### 2.1 英雄与双进阶

| 基础英雄 | 进阶 A | 进阶 B | 主要分叉 |
|---|---|---|---|
| Princess | `High Priestess` | `Dark Lord Princess` | 神圣治疗/净化 vs 黑暗指挥/牺牲 |
| Oracle | `Astral Oracle` | `Fate Dealer` | 稳定预见/星术 vs 奖励/概率/重掷 |
| Peasant | `Harvest Guard` | `Harvest Saint` | 民兵守护/补给盾 vs 丰收治疗/BP |
| Barbarian | `Radiant Berserker` | `Dragon Raider` | 正面斩击/余波 vs 机动破盾/龙威 |
| Monster | `Mirror Fiend` | `Abyssal Queen` | 反射/镜像/陷阱 vs 绝对伤害/献祭 |
| Knight | `Dread Cavalier` | `Holy Paladin` | 黑骑突击/反击 vs 圣骑守护/护盾 |
| Mage | `Stellar Archmage` | `Arcane Archivist` | 星火爆发/魔法伤害 vs 书卷控制/复制 |
| Druid | `Grove Keeper` | `Wildspeaker` | 森林治疗/净化 vs 野性标记/猎群 |

### 2.2 普通兵与单进阶

| 普通兵 | 进阶 | 初始定位 | 进阶后定位 |
|---|---|---|---|
| Cleric | `Saint Cleric` | 治疗/小净化 | 强治疗、祝福、副官治疗 modifier |
| Shieldmaiden | `Aegis Shieldmaiden` | 加盾/防御 | 圣盾、护盾联动、副官护盾 modifier |
| Duelist | `Crimson Duelist` | 标记/斩杀 | 高爆发、追击、副官斩杀 modifier |
| Arcanist | `Astral Arcanist` | 魔法蓄力/小法术 | 星术穿透、魔法增幅、副官魔法 modifier |

---

## 3. 英雄进阶 Brainwriting

### 3.1 Princess

基础幻想：王族、治疗、鼓舞、AP/BP 指挥。她的问题是当前只能低效攻击，必须给玩家“我命令队伍/我祈祷拯救”的操作感。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Saint's Prayer` | 友方卡 | 治疗 2，若目标有 debuff，净化 1 个 | 治疗/净化轴入口 |
| `Royal Command` | AP 区域 | 本回合 +1 AP，下回合 AP -1 或本回合 BP 上限 -1 | AP tempo 入口 |

#### High Priestess

立绘感受：黑白金红祭司袍、权杖、神圣权威。方向不是纯软奶妈，而是“高位神官，能把队伍从崩溃边缘拉回来”。

机制方向：

- Role Action 强化 `Saint's Prayer`：治疗后赋予 `Blessing`，目标下一次受到伤害 -1。
- 若选择过 `Royal Command`，进阶后变成 `Sacred Command`：本回合 +1 AP，同时最低 HP 友方治疗 1。
- Trait：每个己方回合第一次有效治疗额外 +1 BP，受单回合上限限制。
- Trait 备选：被治疗目标若是普通兵，普通兵获得 1 层 `Devotion`，作为未来副官素材成长。

协同：

- 与 `Saint Cleric`：双治疗、净化、祝福叠加，形成圣职 sustain 轴。
- 与 `Aegis Shieldmaiden`：盾 + 祝福减伤，保护低 HP 关键英雄。
- 与 `Grove Keeper`：净化 + 自然回复，打状态战极强。
- 与 `Holy Paladin`：神圣防线，适合慢速 BP reward build。
- 与 `Astral Oracle`：预见 + 祝福，形成高可读但难击穿的防线。

风险：

- 治疗轴最容易拖局。治疗量必须有 AP 成本和有效目标限制。
- 如果治疗也给 BP，必须只在有效治疗时给。

#### Dark Lord Princess

立绘感受：黑红礼服、剑、王冠、冷酷统治。她不是牧师，而是把队伍当棋子的女王。

机制方向：

- Role Action 强化 `Royal Command`：本回合 +2 AP，但下回合 AP -1，或公主失去 1 HP。
- 若选择过 `Saint's Prayer`，进阶后变成 `Blood Benediction`：治疗 3，但目标获得 `Debt`，下回合开始失去 1 HP 或 BP。
- Trait：友方阵亡时获得 1 BP，并让全队下次攻击 +1。每回合最多触发一次。
- Trait 备选：消耗 BP 购买奖励后，最低 HP 友方获得 +1 攻击一回合。

协同：

- 与 `Monster / Abyssal Queen`：牺牲、阵亡、绝对伤害、黑暗统治主题强。
- 与 `Crimson Duelist`：AP 爆发给斩杀角色更多行动窗口。
- 与 `Dragon Raider`：高 AP 回合打破盾后连续收割。
- 与 `Fate Dealer`：赌 BP 和 AP，形成高风险经济爆发。
- 与 `Dread Cavalier`：黑暗骑兵队，突击 + 指挥 + 反击。

风险：

- AP +2 极危险，必须显著还债。
- 友方阵亡收益不能太高，否则玩家会故意献祭普通兵。

---

### 3.2 Oracle

基础幻想：预见、命运、概率、奖励窗口信息。她应该能把随机变成构筑的一部分。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Star Reading` | 友方卡 | 目标获得一次确定性减伤 -1 | 稳定防守、保护关键单位 |
| `Fate Mark` | 敌方卡 | 标记目标，我方下次攻击该目标最低伤害 +1 或无视一点随机减伤 | 集火和爆发 |

#### Astral Oracle

立绘感受：白袍、星杖、紫水晶、庄严星象。方向是“稳定命运”，把概率变成可计划防线。

机制方向：

- `Star Reading` 强化：保护目标与相邻友方，或同时给魔防 +1。
- `Fate Mark` 强化：标记目标受到魔法伤害 +1。
- Trait：我方第一次受到魔法伤害时，若 Oracle 在场，50% 减伤改为一次确定减伤。
- Reward 方向：奖励窗口首次 reset 后，至少保留一个同稀有度选项。

协同：

- 与 `Stellar Archmage`：魔法标记 + 星火爆发。
- 与 `Astral Arcanist`：普通兵魔法增幅，魔法 build 基础件。
- 与 `Grove Keeper`：保护状态单位，防止被秒。
- 与 `High Priestess`：预见 + 祝福慢速防线。
- 与 `Abyssal Queen`：通过标记制造“你防也会被绝对伤害惩罚”的压力。

风险：

- 稳定减伤容易强到无聊，需要限制次数。
- 奖励窗口操控很诱人，但实现复杂，先做战斗内效果。

#### Fate Dealer

立绘感受：黑红礼服、卡牌、骰子、怀表。方向是“命运赌徒”，把 reward、BP、随机都纳入玩法。

机制方向：

- `Star Reading` 变体：目标下次受伤有 50% 完全减 2，50% 无减伤。风险高。
- `Fate Mark` 变体：标记时掷骰，1-3 获得 1 BP，4-6 标记增强；失败时敌方获得一个小补偿。
- Trait：每个奖励窗口可锁定 1 张奖励后 reset 其他选项。
- Trait 备选：每次使用 reset 后，下一次 Role Action 成本 -1，最低 1。

协同：

- 与 `Dark Lord Princess`：高风险 BP/AP 经济。
- 与 `Peasant / Harvest Saint`：skip、补给、BP 低保构筑。
- 与 `Crimson Duelist`：赌到高伤窗口后快速斩杀。
- 与 `Arcane Archivist`：书卷/重掷/复制，操作感复杂但很有味道。

风险：

- 太随机会让玩家觉得不是自己赢的。
- 奖励操控与 BP 经济要防止无限 reset。

---

### 3.3 Peasant

基础幻想：低 cost、补给、播种收获、后勤。她的 build 价值应该是“让队伍运转”，不是变成最强输出。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Supply Basket` | 友方卡 | 治疗 1；若满血，下一次攻击 +1 | 小支援 |
| `Field Work` | 自己/AP 区域 | 下个己方回合获得 `Harvest`，第一次行动 +2 攻或 cost -1 | 投资型 tempo |

#### Harvest Guard

立绘感受：麦穗、金甲、长柄权杖，像民兵队长/丰收守卫。

机制方向：

- `Supply Basket` 强化：治疗后给物防 +1 一回合。
- `Field Work` 强化：收获时同时给共享盾 +1。
- Trait：我方第一次强化共享盾时，最低 HP 友方治疗 1。
- Trait 备选：普通兵被攻击后，Peasant 可获得 `Militia`，下次攻击 +1。

协同：

- 与 `Shieldmaiden / Aegis Shieldmaiden`：后勤 + 盾阵。
- 与 `Holy Paladin`：骑士守护 + 农民修盾，慢速阵地战。
- 与 `High Priestess`：治疗不会浪费，适合长期堆 BP。
- 与 `Duelist`：满血给攻击 +1，支援刺客斩杀。

风险：

- 加盾 + 治疗组合会拖局。
- Peasant 低 cost 如果收益太多，会变成任何 build 必带。

#### Harvest Saint

立绘感受：白金丰收圣女、果篮、麦穗法杖，更偏丰饶祝福。

机制方向：

- `Supply Basket` 强化：治疗 2，满血时赋予 `Fruitful`，下次有效 Role Action 返还 1 BP。
- `Field Work` 强化：下回合开始时，如果 Peasant 存活，获得 1 BP 或治疗全体 1。
- Trait：奖励窗口 skip 时，最低 HP 友方治疗 1。

协同：

- 与 `Fate Dealer`：skip / reset / BP 经济。
- 与 `High Priestess`：治疗构筑极强。
- 与 `Arcane Archivist`：攒 BP 买奖励，慢热但上限高。
- 与 `Abyssal Queen`：黑暗献祭后靠丰收恢复，有危险的“献祭农场”味道。

风险：

- Skip 给收益可能导致玩家不买奖励，只屯资源。
- 需要限制每奖励窗口一次或每回合一次。

---

### 3.4 Barbarian

基础幻想：高攻、冲动、余波、破阵。她不该获得太多稳定防御，强点应该是风险换爆发。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `War Cry` | 自己 | 下次攻击 +1，本回合防御 -1 | 爆发准备 |
| `Provoke` | 敌方卡 | 目标攻击其他单位时伤害 -1，攻击 Barbarian 时无惩罚 | 控制火力 |

#### Radiant Berserker

立绘感受：白金巨斧、阳光符文、正面战神。不是黑暗狂暴，而是荣耀决斗和重击。

机制方向：

- `War Cry` 强化：下次攻击造成 3+ HP 伤害时，触发强化余波。
- `Provoke` 强化：被挑衅目标若攻击 Barbarian，Barbarian 反击 +1。
- Trait：破盾时获得 `Glory`，下次攻击 +1 或获得 1 BP。
- Trait 备选：攻击拥有 shield 的目标时，伤害先对盾 +1。

协同：

- 与 `Oracle / Fate Mark`：标记保证重击阈值。
- 与 `Crimson Duelist`：一个破盾，一个斩杀。
- 与 `Dark Lord Princess`：AP 爆发让重击连动。
- 与 `Aegis Shieldmaiden`：保护自降防的 Barbarian。

风险：

- 4 攻基础已经很高，所有增伤都必须带阈值或代价。

#### Dragon Raider

立绘感受：龙骑、长枪、红黑鳞甲，方向是机动、破盾、猎杀边缘目标。

机制方向：

- `War Cry` 强化：下次攻击若击破盾，对目标追加 1 物理伤害。
- `Provoke` 变体：`Dragon Roar`，拖到敌方卡，目标与相邻敌人获得 `Shaken`，下次攻击 -1。
- Trait：攻击边缘/孤立目标时 +1 伤害。
- Trait 备选：破盾后获得一次 `Raid`，下次 Role Action cost -1。

协同：

- 与 `Dread Cavalier`：双骑兵突袭，打边缘和破盾。
- 与 `Abyssal Queen`：一边破盾一边用绝对伤害惩罚防御。
- 与 `Astral Oracle`：标记弱点后龙骑收割。
- 与 `Shieldmaiden`：己方盾保护龙骑进场。

风险：

- 站位系统尚弱，边缘/邻接收益需要 UI 支持。

---

### 3.5 Monster

基础幻想：反防御、绝对伤害、美女与野兽、公主死亡触发、黑暗代价。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Predatory Gaze` | 敌方卡 | 标记猎物，若目标下次受到 0 攻击伤害，追加绝对伤害 | 反防御陷阱 |
| `Dark Pact` | 友方/自己 | 目标失去 1 HP，获得攻击 +1 或 1 BP | 代价换力量 |

#### Mirror Fiend

立绘感受：镜子、破碎自我、暗色诱惑。方向是反射、复制、错位。

机制方向：

- `Predatory Gaze` 强化：目标被标记期间，第一次获得治疗/祝福时，Monster 复制一半效果或将其转为标记倒计时。
- `Dark Pact` 强化：契约目标受到下一次伤害时，反射 1 点给攻击者。
- Trait：第一次受到致命伤时，如果敌方有 `Marked Prey`，消耗标记免死并对标记目标造成 1 绝对伤害。
- Trait 备选：敌方获得 buff 时，Mirror Fiend 可获得一个弱化版镜像 buff。

协同：

- 与 `Fate Dealer`：镜子 + 赌局，操纵对手选择。
- 与 `High Priestess`：敌方治疗越多，镜子越有价值。
- 与 `Druid / Spore`：驱散敌方 buff 后，镜魔复制/惩罚剩余关键 buff。
- 与 `Crimson Duelist`：镜像标记给 Duelist 斩杀窗口。

风险：

- 复制效果容易让规则难懂。
- 免死机制必须很稀有，且有明确视觉预告。

#### Abyssal Queen

立绘感受：红黑魔杖、深渊火焰、魔王气质。方向是绝对伤害、献祭、黑暗成长。

机制方向：

- `Predatory Gaze` 强化：绝对伤害提高，但只能对非公主或非神圣目标满额。
- `Dark Pact` 强化：可消耗 1 BP 或 1 友军 HP，让 Monster 下一次攻击获得绝对追击条件。
- Trait：任意公主阵亡后获得 `Abyssal Rage`，攻击 +2，不可驱散。
- Trait 备选：友方每次通过代价失去 HP，Monster 获得一层 `Abyss`，满 3 层触发一次绝对伤害。

协同：

- 与 `Dark Lord Princess`：黑暗统治与献祭 AP。
- 与 `Harvest Saint`：牺牲后用丰收回血，形成危险循环。
- 与 `Cleric / Saint Cleric`：把献祭的副作用补回。
- 与 `Dread Cavalier`：突击压血，深渊收割。

风险：

- 绝对伤害是最危险的 build 轴，必须限制频率。
- 献祭普通兵如果收益过高，会鼓励故意送死。

---

### 3.6 Knight

基础幻想：守护、盾、骑兵、代伤。新立绘明确分成黑骑突击和白骑圣盾。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Guard Oath` | 友方卡 | 目标下一次受到主动物理伤害 -2，骑士代受 1 | 指定保护 |
| `Raise Bulwark` | 我方共享盾 | 共享盾 +2，骑士行动结束 | 盾阵专家 |

#### Dread Cavalier

立绘感受：黑马、黑甲、长剑、红披风。方向不是纯防御，而是“进攻型骑士，冲锋与反击”。

机制方向：

- `Guard Oath` 变体：守护目标受到攻击后，Knight 获得 `Vengeance`，下次攻击 +1。
- `Raise Bulwark` 变体：若本回合盾被击破，Knight 下回合第一次攻击 cost -1。
- Role Action 备选：`Dark Charge`，拖到敌方卡，若己方有盾，消耗 1 盾造成额外 1 物理伤害。
- Trait：攻击刚破盾的敌方角色时 +1。

协同：

- 与 `Dragon Raider`：双突击，专打盾后窗口。
- 与 `Dark Lord Princess`：AP 爆发让骑兵连续突击。
- 与 `Crimson Duelist`：Knight 打开护盾/压低血线，Duelist 斩杀。
- 与 `Aegis Shieldmaiden`：提供盾资源供黑骑转化成进攻。

风险：

- 如果骑士既能防又能高攻，会压过 Barbarian。
- 黑骑应消耗盾或承担代价，不能白拿进攻。

#### Holy Paladin

立绘感受：白马、白金盔甲、大盾、圣骑士。方向是阵地防守和神圣保护。

机制方向：

- `Guard Oath` 强化：可保护物理或魔法伤害一次。
- `Raise Bulwark` 强化：共享盾 +2，并给最低 HP 友军 `Sanctuary`，下一次受到伤害 -1。
- Trait：盾吸收伤害后，若 Knight 在场，最低 HP 友军治疗 1，每回合一次。
- Trait 备选：共享盾被破坏时获得 1 BP。

协同：

- 与 `High Priestess`：圣盾 + 祝福 + 治疗，防线最厚。
- 与 `Saint Cleric`：治疗和护盾互相补。
- 与 `Astral Oracle`：确定减伤、魔防、预见。
- 与 `Harvest Guard`：修盾和民兵守护。

风险：

- 防御 build 的终极形态，需要明确反制：绝对伤害、驱散、破盾奖励、AP tempo。

---

### 3.7 Mage

基础幻想：高魔攻、炎上、蓄力、魔法爆发。新立绘分成星辰大法师与书卷档案师。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Arcane Channel` | 自己 | 本回合不攻击，下次魔法攻击 +2 | 蓄力爆发 |
| `Searing Brand` | 敌方卡 | 施加燃烧标记，下回合魔法伤害 | DoT/控制 |

#### Stellar Archmage

立绘感受：白金星辰法袍、太阳/星盘法杖。方向是高位星火爆发，清晰的魔法 carry。

机制方向：

- `Arcane Channel` 强化：充能后下一次魔法攻击 +2，且若击破盾，追加 `Starfall` 1 魔法伤害。
- `Searing Brand` 强化：炎上目标受到魔法伤害 +1。
- Trait：每次敌方身上有 `Burning` 或 `Fate Mark` 时，Mage 的预测显示最低伤害提高。
- Trait 备选：魔法攻击击败敌人时，随机敌人获得 `Spark`。

协同：

- 与 `Astral Oracle`：Fate Mark + 魔法增伤。
- 与 `Astral Arcanist`：魔法副官/普通兵增幅。
- 与 `Druid / Spore Witch`：降低魔防，魔法爆发。
- 与 `Dragon Raider`：先破盾再星火穿透。

风险：

- 4 攻基础加充能极易爆表。蓄力必须消耗行动，且可被打断/驱散。

#### Arcane Archivist

立绘感受：黑色斗篷、书、羽笔、飞舞纸页。方向是书卷、复制、记录、控制，而不是纯输出。

机制方向：

- `Arcane Channel` 变体：选择一个友方 Role Action，记录其类型；下次 Mage 的 Role Action 可复制一个弱化版效果。
- `Searing Brand` 变体：给敌方施加 `Scripted Fate`，目标下次行动后受到 1 魔法伤害或失去 1 BP。
- Trait：每次奖励窗口购买法术/遗物类奖励后，Mage 获得 `Page`；满 2 页后下一次 Role Action cost -1。
- Trait 备选：第一次看到奖励窗口时，额外显示一个“法术注释”，让一个奖励费用 -1。

协同：

- 与 `Fate Dealer`：奖励/重掷/书卷经济。
- 与 `High Priestess`：复制治疗的弱化版，形成魔法支援。
- 与 `Druid`：复制净化/驱散可能很有趣，但需要限制。
- 与 `Saint Cleric`：书卷和治疗轴结合，长线控场。

风险：

- 复制机制开发复杂，第一版可先做“记录最近一次友方 Role Action，Mage 下次获得固定小收益”。

---

### 3.8 Druid

基础幻想：净化、自然回复、驱散、野性伙伴。新立绘明确分成鹿系森林守护和狼系野性猎群。

推荐基础 Role Action 候选：

| Basic Ability | 目标 | 效果草案 | 构筑意义 |
|---|---|---|---|
| `Cleansing Herbs` | 友方卡/状态 | 净化 1 debuff，成功则治疗 1 | 反状态 |
| `Weakening Spores` | 敌方卡/状态 | 驱散 1 buff，并给下回合攻击 -2 | 控制 |

#### Grove Keeper

立绘感受：鹿、白绿长袍、花草、安静自然。方向是森林守护、净化与恢复。

机制方向：

- `Cleansing Herbs` 强化：净化后赋予 `Regrowth`，下个己方回合治疗 1。
- `Weakening Spores` 变体：驱散成功后，最低 HP 友军治疗 1。
- Trait：友方每次失去 debuff 时，Grove Keeper 获得 1 层 `Seed`；满 2 层给共享盾 +1 或治疗最低 HP 友军 1。
- Trait 备选：自然回复不超上限，避免拖局。

协同：

- 与 `High Priestess`：治疗/净化同盟。
- 与 `Holy Paladin`：自然回复保护盾阵。
- 与 `Saint Cleric`：双净化，专克 burn/weakness/mark。
- 与 `Harvest Saint`：自然与丰收主题，BP sustain。

风险：

- 反状态角色如果太强，会让状态 build 没法玩。
- 净化最好不是全体，保持目标选择。

#### Wildspeaker

立绘感受：鹿角/狼伙伴、叶片披风，方向是野性标记、猎群协同、反控制。

机制方向：

- `Cleansing Herbs` 变体：净化友方后，友方获得 `Wild`，下次攻击被标记目标 +1。
- `Weakening Spores` 强化：驱散成功时给目标 `Prey Mark`，我方普通兵攻击该目标 +1。
- Trait：每回合第一次普通兵攻击被标记目标时，Druid 获得 1 BP 或治疗该普通兵 1。
- Trait 备选：狼伙伴可视为一个不可攻击的 aura，不占队伍位。

协同：

- 与 `Duelist / Crimson Duelist`：标记猎物后斩杀。
- 与 `Barbarian / Dragon Raider`：猎群破盾和追击。
- 与 `Monster / Mirror Fiend`：猎物标记和捕食标记互相强化。
- 与 `Shieldmaiden`：保护普通兵猎群。

风险：

- 普通兵攻击 +1 很容易让低成本单位变高效。需要标记条件。

---

## 4. 普通兵 Brainwriting

普通兵只有一个进阶 class 更清晰：玩家不用在普通兵身上再做大量分支选择，但可以通过“是否进阶、是否叠加、是否成为副官”产生构筑。

### 4.1 Cleric -> Saint Cleric

初始 Cleric：

- 定位：辅助/治疗/小净化。
- Attack：低魔法或低物理都可以，建议魔法 1 或物理 1，避免治疗兵拿来打人成为主价值。
- Role Action `Mend`：拖到友方，治疗 1。若目标有 debuff，治疗改为净化 1 + 治疗 1。

进阶 Saint Cleric：

- `Mend` 强化为 `Sacred Bell`：治疗 2；若目标已经满血，赋予 `Blessing`。
- Trait：每回合第一次有效治疗返还 1 BP 或使下次治疗 cost -1，二选一后续测试。
- 副官效果：附给英雄后，英雄的 Role Action 若作用于友方，额外治疗 1；若该 Role Action 已治疗，则额外净化 1。

强协同：

- `High Priestess + Saint Cleric`：治疗上限最高。
- `Dark Lord Princess + Saint Cleric`：黑暗代价后补血。
- `Grove Keeper + Saint Cleric`：反状态/净化队。
- `Holy Paladin + Saint Cleric`：圣盾慢速队。
- `Abyssal Queen + Saint Cleric`：献祭 build 的安全阀。

反制：

- 高爆发斩杀。
- 治疗抑制类遗物/天气。
- 标记集火，迫使治疗被动跟不上。

### 4.2 Shieldmaiden -> Aegis Shieldmaiden

初始 Shieldmaiden：

- 定位：物理高防弱攻，盾阵普通兵。
- Attack：低攻击，最好 1-2。
- Role Action `Brace`：拖到我方共享盾，盾 +1。

进阶 Aegis Shieldmaiden：

- `Brace` 强化为 `Aegis Standard`：盾 +2；如果已有盾，最低 HP 友方获得一次魔法减伤 -1。
- Trait：盾吸收伤害后，Shieldmaiden 若在场，有机会获得 1 BP 或给自己物防 +1 一回合。
- 副官效果：附给英雄后，英雄使用 Role Action 后我方共享盾 +1；如果英雄是 Knight，额外强化守护效果。

强协同：

- `Holy Paladin + Aegis Shieldmaiden`：纯圣盾阵。
- `Dread Cavalier + Aegis Shieldmaiden`：把盾转化为冲锋燃料。
- `Harvest Guard + Aegis Shieldmaiden`：修盾后勤。
- `Stellar Archmage + Aegis Shieldmaiden`：保护蓄力法师。
- `Fate Dealer + Aegis Shieldmaiden`：慢慢攒 BP 重掷奖励。

反制：

- Monster 的绝对伤害。
- Druid 的驱散/弱化。
- 破盾收益 build。

### 4.3 Duelist -> Crimson Duelist

初始 Duelist：

- 定位：物理高攻脆皮，单点斩杀。
- Attack：高于普通兵平均，防御低。
- Role Action `Feint`：拖到敌方，目标获得 `Opening`，我方下一次攻击该目标 +1。

进阶 Crimson Duelist：

- `Feint` 强化为 `Crimson Opening`：标记目标；若目标 HP 低于一半，下次攻击额外 +1。
- Trait：Duelist 击败带标记目标时，获得 1 BP 或立刻清除自身 HasActed 的一次性标志，后者很危险，先只脑暴。
- 副官效果：附给英雄后，英雄攻击被自己 Role Action 标记的目标时 +1 伤害；若击败目标，获得 1 BP。

强协同：

- `Fate Mark Oracle + Crimson Duelist`：命运标记斩杀。
- `Wildspeaker + Crimson Duelist`：猎物标记斩杀。
- `Dark Lord Princess + Crimson Duelist`：AP 爆发收割。
- `Dread Cavalier + Crimson Duelist`：骑兵压血，决斗者补刀。
- `Mirror Fiend + Crimson Duelist`：镜像标记制造陷阱。

反制：

- 守护/盾。
- 预见减伤。
- 斩杀前被集火。

### 4.4 Arcanist -> Astral Arcanist

初始 Arcanist：

- 定位：魔法高攻脆皮或高魔防法术兵。
- Attack：魔法 2-3。
- Role Action `Focus`：拖到自己，下次魔法攻击 +1。

进阶 Astral Arcanist：

- `Focus` 强化为 `Astral Focus`：下次魔法攻击 +1，并对带 mark/burning/weakness 的目标再 +1。
- Trait：每次友方施加标记或燃烧，Arcanist 获得一层 `Star Charge`；满 2 层下次攻击魔防穿透 1。
- 副官效果：附给英雄后，英雄的魔法伤害 +1；若英雄 Role Action 是标记/燃烧/衰弱，则该状态额外持续或额外附加魔防 -1。

强协同：

- `Stellar Archmage + Astral Arcanist`：纯魔法爆发。
- `Astral Oracle + Astral Arcanist`：星术标记。
- `Druid Spore + Astral Arcanist`：魔防降低后爆发。
- `Arcane Archivist + Astral Arcanist`：书卷复制/魔法副官。
- `Mage + Oracle + Arcanist` 是未来魔法队核心。

反制：

- 高魔防单位。
- Oracle 敌方预见。
- 净化/驱散充能状态。

---

## 5. 从开局 1 英雄 + 2 普通兵模拟构筑路线

### 5.1 开局选择的基本问题

玩家开局从 3 个随机英雄选 1，再从 4 个普通兵选 2。此时构筑还很早，但已经应该出现方向：

- 选 Princess：倾向治疗/AP 指挥，普通兵优先 Cleric/Shieldmaiden 或 Duelist。
- 选 Oracle：倾向标记/魔法/奖励操控，普通兵优先 Arcanist/Duelist。
- 选 Peasant：倾向 BP/补给/低 cost，普通兵可任意，但 Shieldmaiden/Cleric 最稳。
- 选 Barbarian：倾向破盾/高攻，普通兵优先 Duelist/Shieldmaiden。
- 选 Monster：倾向反防御/献祭，普通兵优先 Cleric/Duelist。
- 选 Knight：倾向盾/保护或骑兵突击，普通兵优先 Shieldmaiden/Duelist。
- 选 Mage：倾向魔法爆发，普通兵优先 Arcanist/Shieldmaiden。
- 选 Druid：倾向状态控制/净化/猎群，普通兵优先 Cleric/Duelist/Arcanist。

### 5.2 开局三件套示例

| 开局 | 早期玩法 | 第一优先奖励 | 成型方向 |
|---|---|---|---|
| Princess + Cleric + Shieldmaiden | 治疗和盾保核心 | 英雄升级或 Saint Cleric | 圣职防线 |
| Princess + Duelist + Cleric | 指挥 Duelist 斩杀，Cleric 补血 | Princess 升级 | 指挥斩杀 |
| Oracle + Arcanist + Duelist | 标记后物理/魔法都能吃收益 | Arcanist 进阶或 Oracle 升级 | 星术集火 |
| Oracle + Shieldmaiden + Arcanist | 预见保护脆皮法术兵 | Astral Arcanist | 稳定魔法队 |
| Peasant + Shieldmaiden + Cleric | 补给、修盾、慢慢攒 BP | Peasant 升级或遗物 | 后勤防线 |
| Peasant + Duelist + Arcanist | 低 cost 支援双输出 | Duelist/Arcanist 进阶 | 经济爆发 |
| Barbarian + Duelist + Shieldmaiden | 高压攻击，盾保脆皮 | Barbarian 升级 | 破盾斩杀 |
| Barbarian + Cleric + Duelist | 自降防爆发后治疗补救 | Crimson Duelist | 风险爆发 |
| Monster + Cleric + Duelist | 代价换攻，Cleric 补血，Duelist 收割 | Monster 升级 | 黑暗斩杀 |
| Monster + Shieldmaiden + Arcanist | 盾诱导对手，魔法补伤害 | Monster 或 Arcanist | 反防御 |
| Knight + Shieldmaiden + Cleric | 标准乌龟阵 | Holy Paladin 或 Aegis | 圣盾 |
| Knight + Shieldmaiden + Duelist | 盾转突击，Duelist 补刀 | Dread Cavalier | 骑兵突击 |
| Mage + Arcanist + Shieldmaiden | 盾保护蓄力法师 | Stellar Archmage | 星火炮台 |
| Mage + Arcanist + Cleric | 法师爆发，Cleric 防被秒 | Astral Arcanist | 魔法 sustain |
| Druid + Cleric + Shieldmaiden | 净化/驱散/盾 | Grove Keeper | 反状态防线 |
| Druid + Duelist + Arcanist | 标记猎物，双输出击杀 | Wildspeaker | 猎群魔刺 |

---

## 6. 构筑 Archetype 大脑暴

下面不是最终平衡方案，而是“玩家可能会感觉自己在玩某种流派”的草图。

### 6.1 圣职铁壁

核心件：`High Priestess` + `Holy Paladin` + `Saint Cleric` + `Aegis Shieldmaiden`

玩法：

- 盾保护队伍。
- 祝福减少下一次伤害。
- 治疗修复穿透盾的伤害。
- BP 用来买遗物和升级防线。

成型路径：

1. Princess + Cleric + Shieldmaiden 开局。
2. Cleric 进阶 Saint Cleric。
3. 获取 Knight，进阶 Holy Paladin。
4. Shieldmaiden 成为 Princess 或 Knight 副官。

快感：

- 队伍像堡垒一样稳定。
- 每次对手差一点击杀失败，玩家会有强烈安全感。

反制：

- Abyssal Queen 绝对伤害。
- Dragon Raider 破盾奖励。
- Druid 驱散祝福。
- AP tempo 爆发在治疗前斩杀。

### 6.2 黑暗王令斩杀

核心件：`Dark Lord Princess` + `Crimson Duelist` + `Dread Cavalier`

玩法：

- Princess 用 AP 指挥创造爆发回合。
- Dread Cavalier 消耗盾或压血。
- Crimson Duelist 标记斩杀。

成型路径：

1. Princess + Duelist + Shieldmaiden 开局。
2. Princess 走 Royal Command。
3. Duelist 进阶 Crimson。
4. 后续获取 Knight 走 Dread Cavalier。

快感：

- 一个回合突然从控制局面变成击杀。
- 代价和爆发很有黑暗女王味道。

反制：

- 下回合 AP 还债。
- 如果第一轮爆发没杀掉，会被治疗/反击惩罚。
- 预见和守护可拆掉斩杀线。

### 6.3 星术炮台

核心件：`Stellar Archmage` + `Astral Oracle` + `Astral Arcanist`

玩法：

- Oracle 标记敌人。
- Arcanist 聚焦。
- Mage 蓄力后打高魔法爆发。

成型路径：

1. Mage + Arcanist + Shieldmaiden 开局。
2. Mage 走 Arcane Channel。
3. Arcanist 进阶 Astral。
4. 获取 Oracle，进阶 Astral Oracle。

快感：

- 预测面板出现很高的魔法伤害。
- 通过标记和充能规划大回合。

反制：

- 高魔防/魔防天气。
- Druid 净化标记。
- Duelist 抢杀脆皮法师。

### 6.4 命运赌场

核心件：`Fate Dealer` + `Arcane Archivist` + `Harvest Saint`

玩法：

- 重掷奖励。
- 锁定奖励。
- skip 和 BP 折扣滚雪球。
- 书卷记录奖励或 Role Action。

成型路径：

1. Oracle + Peasant + Cleric 开局。
2. Oracle 走 Fate Dealer。
3. Peasant 走 Harvest Saint。
4. 获取 Mage 走 Arcane Archivist。

快感：

- 玩家觉得自己在“经营命运”，而不是只打战斗。
- 高 BP 让奖励选择有更多贪心空间。

反制：

- 前期战斗力弱。
- 如果对手高压，没等成型就会被击穿。
- 实现复杂，最晚测试。

### 6.5 丰收后勤队

核心件：`Harvest Saint` + `Saint Cleric` + 任意高消耗英雄

玩法：

- Peasant 提供 BP/治疗/补给。
- Cleric 稳定治疗。
- 养一个高价值英雄作为 carry。

适合 carry：

- Stellar Archmage。
- Radiant Berserker。
- Abyssal Queen。
- Dread Cavalier。

快感：

- 普通角色被玩家养成真正后勤核心。
- “弱小农民支撑大英雄”的叙事感很强。

反制：

- 集火 Peasant。
- 禁疗/燃烧/毒类天气。
- 破坏 BP 经济的遗物。

### 6.6 猎群标记

核心件：`Wildspeaker` + `Crimson Duelist` + `Dragon Raider`

玩法：

- Druid 标记猎物。
- Duelist 吃标记斩杀。
- Dragon Raider 攻击孤立或边缘目标。

成型路径：

1. Druid + Duelist + Arcanist/Shieldmaiden。
2. Druid 走 Wildspeaker。
3. Duelist 进阶。
4. 获取 Barbarian 走 Dragon Raider。

快感：

- 玩家像在指挥猎群围杀。
- 标记目标后多单位协同很明显。

反制：

- Shield/Guard 改变目标。
- Cleanse 移除标记。
- 站位如果不清晰，体验会打折。

### 6.7 深渊献祭

核心件：`Abyssal Queen` + `Dark Lord Princess` + `Saint Cleric` 或 `Harvest Saint`

玩法：

- 通过失去 HP、消耗 BP、友方阵亡触发强力黑暗收益。
- Cleric/Harvest 把代价补回来。

快感：

- 很强的黑暗 JRPG build 感。
- 玩家有“我在玩危险力量”的情绪。

反制：

- 直接击杀核心，别给她积层。
- 禁疗。
- 公主免疫/神圣单位对深渊的限制。

风险：

- 最容易失控。需要硬限制每回合触发次数。

### 6.8 镜像陷阱

核心件：`Mirror Fiend` + `Fate Dealer` + `Grove Keeper`

玩法：

- Fate 让对手难以判断概率。
- Mirror 复制/反射对手 buff。
- Grove Keeper 净化己方，避免被同样状态反噬。

快感：

- 对手的强 buff 反过来成为自己的资源。
- 很“恶魔少女玩弄战场”的感觉。

反制：

- 少上 buff，直接用纯伤害攻击。
- 绝对伤害绕过反射。
- 先杀 Mirror。

### 6.9 圣盾骑兵

核心件：`Holy Paladin` + `Aegis Shieldmaiden` + `Radiant Berserker`

玩法：

- 盾阵保 Barbarian。
- Barbarian 等大斩击窗口。
- Paladin 保关键单位。

快感：

- 重装队伍稳步推进。
- 既有防守又有大斧爆发。

反制：

- 魔法穿透。
- 绝对伤害。
- Dispel 去掉祝福。

### 6.10 黑骑破阵

核心件：`Dread Cavalier` + `Dragon Raider` + `Crimson Duelist`

玩法：

- 双突击破盾。
- Duelist 收割。
- Princess 或 Peasant 提供 AP/补给。

快感：

- 全队都是进攻角色，节奏压迫感强。
- 黑骑和龙骑视觉风格也很统一。

反制：

- 预见和圣盾。
- 控制关键突击者。
- 让他们 AP 还债后无力。

### 6.11 燃烧星火

核心件：`Stellar Archmage` + `Astral Arcanist` + `Spore Witch/Wildspeaker`

玩法：

- Burn、mark、weakness 都成为魔法爆发的前置。
- Arcanist 对带状态目标增伤。

快感：

- 状态越多，法术越疼。
- 每个状态都像在布置最终火力。

反制：

- Grove Keeper / Saint Cleric 净化。
- 高魔防。
- 快速击杀 Mage。

### 6.12 书卷圣堂

核心件：`Arcane Archivist` + `High Priestess` + `Saint Cleric`

玩法：

- Archivist 记录治疗/净化型 Role Action。
- 神圣队不只防守，还通过复制维持稳定。

快感：

- “法术书记录神术”的奇妙 build。
- 玩家有工具箱感。

反制：

- 书卷复制如果需要准备，打断准备。
- 直接集火 Archivist。

### 6.13 反状态净化队

核心件：`Grove Keeper` + `Saint Cleric` + `High Priestess`

玩法：

- 对方上 burn、weakness、mark 都被净化。
- 净化还能转化为治疗或 BP。

快感：

- 专门克制状态队。
- 玩家会觉得自己读懂了对手构筑。

反制：

- 纯伤害和 AP burst。
- 绝对伤害。
- 净化目标不足时她们效率下降。

### 6.14 低费连动队

核心件：`Peasant` + `Cleric` + `Duelist` + AP/BP reward

玩法：

- 多个低 cost 行动堆叠 BP。
- 治疗、标记、攻击分散使用。

快感：

- 每回合能做很多小动作。
- 适合新手理解“队伍协作”。

反制：

- AoE/余波。
- 对低 HP 单位逐个击破。
- 行动多但单次收益低。

### 6.15 破盾经济

核心件：`Dragon Raider` + `Radiant Berserker` + `Dread Cavalier`

玩法：

- 对手开盾越多，自己破盾越赚 BP 或伤害。
- 把对手防御动作变成进攻资源。

快感：

- 反防御流派。
- 看到对手开盾不是头疼，而是兴奋。

反制：

- 不堆盾，改用治疗/预见。
- 用反击或控制拖住破盾者。

### 6.16 普通兵军团

核心件：`Wildspeaker` + 多个普通兵 + soldier reward

玩法：

- 不急着收集多个英雄，而是强化普通兵。
- Wildspeaker 让普通兵打标记目标更强。

快感：

- “英雄带领小队”的最核心方向。
- 普通兵不是填充，而是真正 build 件。

反制：

- AoE/邻接伤害。
- 斩杀普通兵，削弱副官素材。

### 6.17 英雄双核队

核心件：任意两个进阶英雄 + 一个副官

示例：

- `High Priestess + Stellar Archmage + Astral Arcanist 副官`：保护法师炮台。
- `Dark Lord Princess + Abyssal Queen + Saint Cleric 副官`：黑暗代价循环。
- `Astral Oracle + Crimson Duelist + Duelist 副官`：命运斩杀。
- `Holy Paladin + Radiant Berserker + Aegis 副官`：盾保大斧。

快感：

- 玩家会感觉自己不是只升级一个角色，而是把队伍拼成机器。

风险：

- 需要清晰 UI 表示谁是核心、谁是副官、谁的 modifier 正在生效。

---

## 7. 英雄和普通兵协同矩阵

### 7.1 英雄 x 普通兵

| 英雄 | Cleric / Saint | Shieldmaiden / Aegis | Duelist / Crimson | Arcanist / Astral |
|---|---|---|---|---|
| Princess | 治疗叠加，神圣队；黑暗公主可用 Cleric 补代价 | 保护公主，High Priestess 强防线 | Royal Command 给 Duelist 斩杀窗口 | AP 指挥给 Arcanist 蓄力或魔法队补 tempo |
| Oracle | 保证 Cleric 不死，Fate 可操纵治疗奖励 | 预见 + 护盾慢速稳 | Fate Mark + Duelist 斩杀 | 星术核心组合，魔法 build 起点 |
| Peasant | 双后勤，低费 sustain | 修盾/补给，普通兵防线 | 补给满血加攻给 Duelist | 低费给 Arcanist 准备回合 |
| Barbarian | 自降防后靠治疗补 | 盾保护高攻前排 | 双物理斩杀，破盾后收割 | Arcanist 标记魔弱，Barbarian 补物理压力 |
| Monster | 献祭后治疗，黑暗循环 | 用盾诱导 0 伤和绝对追击 | 标记陷阱 + 斩杀 | 魔法状态配合绝对伤害压盾队 |
| Knight | 骑士代伤后治疗 | 最自然盾阵组合 | 骑士保护 Duelist 或开路 | 盾保护法术兵，魔法/物理双轴 |
| Mage | 保护蓄力后治疗 | 盾保护炮台 | Duelist 收割被烧残目标 | 核心魔法组合 |
| Druid | 双净化，反状态 | 盾和自然回复 | 猎物标记斩杀 | 状态 + 魔法增幅 |

### 7.2 进阶英雄之间的强协同

| 组合 | 主题 | 说明 |
|---|---|---|
| High Priestess + Holy Paladin | 圣盾防线 | 治疗、祝福、护盾、守护四层保护 |
| High Priestess + Grove Keeper | 净化治疗 | 状态队克星 |
| High Priestess + Stellar Archmage | 圣职炮台 | 治疗保护蓄力法师 |
| Dark Lord Princess + Abyssal Queen | 黑暗王权 | AP/献祭/绝对伤害 |
| Dark Lord Princess + Dread Cavalier | 黑骑指挥 | AP 爆发突击 |
| Dark Lord Princess + Crimson Duelist | 王令斩杀 | 指挥给刺客斩杀 |
| Astral Oracle + Stellar Archmage | 星术法核 | 标记 + 魔法爆发 |
| Astral Oracle + Holy Paladin | 命运圣盾 | 稳定减伤叠盾 |
| Fate Dealer + Arcane Archivist | 奖励操控 | reset、锁定、书卷复制 |
| Fate Dealer + Harvest Saint | BP 赌场 | skip 和 reward economy |
| Harvest Guard + Holy Paladin | 民兵堡垒 | 修盾 + 圣骑守护 |
| Harvest Saint + Abyssal Queen | 献祭农场 | 高风险自损后恢复 |
| Radiant Berserker + Astral Oracle | 荣耀重击 | 标记保证重击阈值 |
| Dragon Raider + Dread Cavalier | 双骑突击 | 破盾、边缘、突击 |
| Mirror Fiend + Fate Dealer | 镜中命运 | 随机/复制/反射 |
| Mirror Fiend + Grove Keeper | 安全镜像 | 自己净化，复制敌方 |
| Wildspeaker + Crimson Duelist | 猎群斩杀 | 标记目标多单位集火 |
| Wildspeaker + Dragon Raider | 野性突袭 | 猎群 + 龙威 |
| Arcane Archivist + High Priestess | 记录神术 | 复制治疗/净化的弱版 |
| Arcane Archivist + Druid | 记录自然 | 复制净化/驱散，需严格限制 |

### 7.3 进阶英雄 x 副官

| 进阶英雄 | 最适副官 | 效果想象 |
|---|---|---|
| High Priestess | Saint Cleric | 治疗 Role Action 额外净化或祝福 |
| High Priestess | Aegis Shieldmaiden | 治疗后加盾，圣职盾线 |
| Dark Lord Princess | Crimson Duelist | 指挥后目标被标记，方便斩杀 |
| Dark Lord Princess | Saint Cleric | 抵消黑暗代价 |
| Astral Oracle | Astral Arcanist | Fate Mark 额外魔防 -1 |
| Fate Dealer | Crimson Duelist | 赌到标记后爆发 |
| Harvest Guard | Aegis Shieldmaiden | 修盾效率极高 |
| Harvest Saint | Saint Cleric | 超强后勤，需要防拖局 |
| Radiant Berserker | Crimson Duelist | 重击后斩杀阈值降低 |
| Dragon Raider | Aegis Shieldmaiden | 有盾就有突击燃料 |
| Mirror Fiend | Saint Cleric | 契约/反射后补血 |
| Abyssal Queen | Saint Cleric | 献祭安全阀 |
| Dread Cavalier | Crimson Duelist | 突击目标更容易被收割 |
| Holy Paladin | Aegis Shieldmaiden | 护盾专家完全体 |
| Stellar Archmage | Astral Arcanist | 魔法伤害和标记增幅 |
| Arcane Archivist | Astral Arcanist | 法术记录和魔法副官 |
| Grove Keeper | Saint Cleric | 双净化治疗 |
| Wildspeaker | Crimson Duelist | 猎物标记斩杀 |

---

## 8. 奖励选择如何服务 build

奖励池不应该只是随机给强东西，而应该让玩家看到“我现在正在走某条路线”。

### 8.1 早期奖励

目标：让开局方向清晰。

- 英雄 Basic Ability 解锁。
- 获取指定普通兵。
- 普通兵进阶素材。
- 低费用遗物：治疗 +1、破盾 +1 BP、第一次 Role Action 返还 1 BP。

### 8.2 中期奖励

目标：让队伍从“角色组合”变成“构筑”。

- 新英雄加入。
- 普通兵进阶。
- 英雄第二次升级。
- 副官槽解锁。
- Role Action 强化。

### 8.3 后期奖励

目标：让 build 有终局身份。

- 英雄进阶 class。
- 进阶普通兵成为副官。
- 强遗物。
- 天气/战场规则选择。

### 8.4 保底应按构筑轴，而不是只按稀有度

如果玩家已经有 Mage + Arcanist，奖励池应稍微倾向：

- 魔法伤害。
- 标记。
- Arcanist 进阶。
- Mage 升级。

如果玩家是 Knight + Shieldmaiden，则倾向：

- 护盾。
- 守护。
- 防御转进攻。

这不是让玩家永远拿到想要的，而是避免完全抽不到核心件。

---

## 9. 遗物和天气的构筑接口

### 9.1 遗物应放大 build，不替代角色

好的遗物：

- “每回合第一次有效 Role Action 返还 1 BP。”
- “破盾时，攻击者获得 +1 攻击到回合结束。”
- “治疗溢出时，转化为 1 点共享盾，每回合一次。”
- “标记目标死亡时，返还 1 BP。”

不好的遗物：

- “全员攻击 +3。”
- “所有治疗翻倍且无上限。”
- “每次行动都 +1 BP。”

### 9.2 天气应制造阶段变化

天气示例：

- `Starfall Night`：魔法 Role Action cost -1，魔法伤害 +0，不直接增伤，鼓励法术行动。
- `Siege Rain`：共享盾上限 -1，但破盾奖励 +1 BP。
- `Harvest Wind`：治疗 +1，但主动攻击造成 HP 伤害不再给 BP。
- `Blood Moon`：治疗 -1，绝对伤害 +1 次数限制。
- `Sacred Dawn`：每回合第一次净化额外治疗 1。

天气要避免让某一方突然什么都不能玩。最好是双方公开、持续短、可计划。

---

## 10. 平衡风险总表

| 风险 | 可能来源 | 防线 |
|---|---|---|
| 治疗拖局 | High Priestess、Saint Cleric、Grove Keeper | 治疗有效才触发、不能无限超上限、禁疗天气 |
| AP 滚雪球 | Dark Lord Princess、Fate Dealer | 下回合还债、不能和大量治疗同包 |
| BP 刷分 | Harvest Saint、Fate Dealer、Role Action | 单回合上限 3、有效行动才给 |
| 盾无敌 | Holy Paladin、Aegis、Harvest Guard | 破盾奖励、绝对伤害、盾上限 |
| 绝对伤害无解 | Abyssal Queen、Monster | 条件触发、次数限制、神圣单位限制 |
| 魔法爆表 | Stellar Archmage、Astral Arcanist、Oracle | 蓄力可打断、魔防/净化/高 cost |
| 标记斩杀过强 | Wildspeaker、Crimson Duelist、Oracle | 标记可净化、守护可转移、标记不叠加 |
| 奖励操控太强 | Fate Dealer、Arcane Archivist | 每窗口次数限制、锁定/重掷成本 |
| 普通兵变必带 | Peasant、Cleric、Shieldmaiden | 普通兵数值低，副官占用单位资源 |

---

## 11. 最值得先做的 5 条 prototype 路线

为了避免一次做爆，建议从这些路线里选。

### 11.1 Knight + Shieldmaiden 盾线

原因：最符合 Role Action 拖到共享盾的 UI 试点。

要验证：

- 拖角色到共享盾是否直觉。
- 加盾是否比公共防御指令更有角色感。
- 盾 build 是否拖局。

### 11.2 Princess + Cleric 治疗线

原因：解决“公主只能攻击”的最大痛点。

要验证：

- 拖到友方治疗是否直觉。
- 治疗行动是否值得花 AP。
- 治疗是否给 BP。

### 11.3 Oracle + Duelist 标记斩杀线

原因：能快速验证“Role Action 创造攻击收益”。

要验证：

- 标记是否能让低攻击/高攻击选择更有趣。
- 标记可否被净化。
- 是否导致固定先标记再打的公式化。

### 11.4 Mage + Arcanist 魔法蓄力线

原因：验证蓄力和魔法 build。

要验证：

- 放弃本回合攻击换下回合爆发是否好玩。
- 蓄力如何被打断。
- 魔法 UI 是否清楚。

### 11.5 Druid + Cleric 净化线

原因：状态系统会越来越多，必须早测试净化/驱散。

要验证：

- 状态图标能否作为拖动目标。
- 玩家能否理解可净化/可驱散。
- 净化是否太克制状态流。

---

## 12. 总结

这份 brainwriting 的核心方向是：

```text
角色进阶不是单纯变强，
而是让角色在队伍中的动词发生变化。
```

英雄的两个进阶 class 应该像两条不同人生路线：

- Princess 可以成为救赎队伍的 High Priestess，也可以成为用代价换胜利的 Dark Lord Princess。
- Knight 可以成为守护阵线的 Holy Paladin，也可以成为把护盾转为突击燃料的 Dread Cavalier。
- Mage 可以成为纯魔法爆发的 Stellar Archmage，也可以成为操纵书卷和奖励的 Arcane Archivist。
- Druid 可以成为净化恢复的 Grove Keeper，也可以成为猎群标记的 Wildspeaker。

普通兵则不需要多分支，而是成为 build 的齿轮：

- Cleric 保生命。
- Shieldmaiden 保盾。
- Duelist 收割。
- Arcanist 放大魔法。

副官系统最适合做 modifier，而不是新按钮。进阶普通兵成为副官后，应该改写英雄已有 Attack / Role Action / Trait，让玩家感到“这名士兵陪伴英雄成长后，改变了英雄的打法”。

最终希望玩家从开局 1 英雄 + 2 普通兵开始，就能逐渐形成类似这些身份：

- 圣盾防线
- 黑暗王令斩杀
- 星术炮台
- 命运赌场
- 丰收后勤
- 猎群标记
- 深渊献祭
- 黑骑破阵
- 燃烧星火
- 书卷圣堂

这些 build 不一定都要进入第一版。第一版只要证明一个关键点：

> 玩家不再只是问“谁打谁最赚”，而是开始问“这一回合我用谁的职业行动来打开我的构筑路线”。
