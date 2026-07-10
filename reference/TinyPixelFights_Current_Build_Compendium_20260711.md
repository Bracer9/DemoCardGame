# Tiny Pixel Fights - Current Build Compendium

更新时间：2026-07-11  
状态：当前源码 + 当前设定文档对齐版。本文记录“现在真实能玩到的构筑方向”，不是未来愿望池。

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

- Stage I：round 4 起。
- Stage II：round 8 起。
- Stage III：round 12 起，或队伍已有 Hero Rank2 / Soldier Rank2。
- 当前遗物选项是未拥有遗物中随机 3 个，没有按队伍 build tag 加权。

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
| 炎上 / 咏唱 / 魔法 | Mage、Oracle 星读线、Arcanist | Arcanist、Cleric、Shieldmaiden | 星墨、余烬星盘、空心彗镜、灰烬引爆器、余燃香炉 | 高 | 早期没有星墨 / Arcanist 时，燃烧伤害很容易只打士气。 |
| 物理连击 / 再行动 | War Queen、Barbarian 荣耀线、Peasant 民兵线、Duelist | Duelist、副官 Duelist、Shieldmaiden | 红磨刀石、决斗券、绿旗、胜利鼓、红沙漏 | 高 | 真正爆发多在 Rank3 或拿到连击遗物后，前期依赖 Duelist Aura。 |
| 盾墙 / 守护 | Knight 双线、Shieldmaiden、Peasant 补给线 | Shieldmaiden、副官 Shieldmaiden、Cleric | 黑铁铆钉、石匠令、碎盾铃、王墙军旗、壁垒锤 | 高 | 共享盾每 turn 会清空；王墙军旗前，盾墙需要持续 AP 投入。 |
| 治疗 / 净化 / 续航 | Saint Queen、Grove Keeper、Quartermaster、Cleric | Cleric、副官 Cleric、Shieldmaiden | 银护符；盾系遗物作为替代防线 | 中低 | 专属治疗 / 净化遗物尚未实装，净化价值受对局 Debuff 数量影响。 |
| Debuff / 控制 / 士气压制 | Fate Dealer、Druid 孢子线、Mage 归档线、Dragon Raider | Arcanist、Duelist | 空心彗镜，燃烧遗物可兼容 | 中低 | 文档中的控制遗物未接入，缺少从“挂 Debuff”到“胜利条件”的遗物闭环。 |
| 士兵团 / 民兵 | Militia Foreman、Wildspeaker、Rank2 士兵 | Duelist、Arcanist、Shieldmaiden、Cleric | 物理 / 魔法静态遗物、胜利鼓、红沙漏 | 中低 | 士兵专属遗物未接入；需要多次士兵招募和升级，成型慢。 |
| 猎物 / 绝对伤害 / 献祭 | Monster 双线、Duelist 副官、War Queen | Cleric、Duelist、Arcanist | 红磨刀石、军议战旗，部分物理连击遗物 | 中 | 猎物 / 献祭专属遗物未接入，主要靠英雄自身机制撑。 |
| BP / AP / 士气经济 | War Queen、Dark Pact、Field Work、破盾队 | Duelist、Shieldmaiden、Cleric | 绿旗、胜利鼓 | 中低 | BP / AP 专属遗物未接入；BP 上限 5 使大经济回合容易封顶。 |

## 2. 英雄路线索引

### 2.1 Princess

#### Saint Queen

成型方向：治疗、净化、护咒、共享盾补强。

路线内容：

- Rank1 `saintly-prayer`：1 AP 治疗 2 HP；若净化 Debuff，获得 1 AP。
- Rank2：MaxHp +3、魔防 +1；主动治疗 / 净化成功后给目标护咒。
- Rank3 `miracle-standard`：2 AP 范围治疗与净化；若净化成功，按 Princess 当前魔防 + 影响人数增加共享盾。

推荐组合：

- 士兵：Cleric 是主轴，Shieldmaiden 是副轴。
- 副官：Cleric 副官让治疗 / 净化链额外给护咒；Shieldmaiden 副官让盾和坚守链更稳。
- 遗物：银护符、黑铁铆钉、石匠令、王墙军旗、壁垒锤。

成型方式：

1. Rank1 先拿 `saintly-prayer`，用第一次 0 BP 英雄训练完成路线锁定。
2. 早期优先拿 Cleric 或 Shieldmaiden 到 Rank1，补魔防 / 物防 Aura。
3. 中期如果对局 Debuff 多，推进 Princess Rank2；如果压力来自物理，则优先 Shieldmaiden Rank2 或盾系遗物。
4. Rank3 后以 `miracle-standard` 清 Debuff、回血、补共享盾，进入拖长战线。

当前问题：

- 专属圣疗 / 净化遗物还没有接入。现在这条线更像“角色内置续航 + 盾遗物借力”，而不是完整遗物体系。
- 如果对手 Debuff 少，`miracle-standard` 的共享盾收益会变低。

#### War Queen

成型方向：AP 前借、额外攻击、绝对伤害斩杀。

路线内容：

- Rank1 `royal-command`：1 AP 换本 turn +2 AP，下个己方 turn AP -1。
- Rank2：MaxHp +2、Attack +1、物防 +1；回合开始治疗后额外治疗低 HP 友方 1 HP。
- Rank3 后 Princess 攻击类型变为物理；`edict-of-victory` 给我方 1 体额外攻击，攻击后追加 Princess 当前 Attack 的绝对伤害，击败则获得 BP 并抵消 AP debt。

推荐组合：

- 士兵：Duelist 最优先，Arcanist 次优。
- 副官：Duelist 副官让宿主攻击后追加绝对伤害；Arcanist 副官适合魔法宿主，通过魔涌 + 再行动扩展节奏。
- 遗物：红磨刀石、决斗券、胜利鼓、红沙漏、军议战旗。

成型方式：

1. Rank1 解 `royal-command` 后，先把队伍构造成能花掉额外 AP 的形状。
2. Duelist Rank1 的物理攻击 +2 Aura 是 War Queen 后期转物理后的关键补强。
3. Rank3 后用 `edict-of-victory` 交给高攻击单位、带强攻 / 猛击单位、或已经能斩杀的单位。
4. 胜利鼓与红沙漏把“额外攻击”转成 BP 和第三击爆发，是这条线的真正终局拼图。

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

- 士兵：Arcanist 是核心，Cleric 负责防守，Shieldmaiden 保护蓄力单位。
- 副官：Arcanist 副官给魔涌并可再行动，适合 Mage、Oracle 或魔法化队伍核心。
- 遗物：见习星墨、余烬星盘、空心彗镜、灰烬引爆器、余燃香炉、军议战旗。

成型方式：

1. 先找 Mage 或 Arcanist，让 `star-reading` 有可重复攻击的魔法核心。
2. Rank1 Arcanist Aura 给全体魔法攻击 +2，是这条线的基础数值。
3. Mage `arcane-channel` 或 Arcanist `astral-focus` 提供咏唱，Oracle Rank3 负责把咏唱变成一轮爆发。
4. 空心彗镜让只打士气的魔法伤害也能推进空虚，避免魔法队早期“打了但没进 HP”的空转感。

当前问题：

- `star-reading` 要求目标已经攻击过，所以 AP 规划要求较高。
- 如果没有 Arcanist 或星墨，魔法再行动容易被士气吃掉，实际 HP 压力不足。

#### Fate Dealer

成型方向：标记、Debuff 计数、士气击穿、战栗控制。

路线内容：

- Rank1 `fate-mark`：标记敌人，使其下一次攻击我方时随机减半或 +1。
- Rank2：MaxHp +2、Attack +1。当前源码未见 `fate-mark` 路线的额外 Trait 强化 hook。
- Rank3 `thread-cut`：统计目标身上的印记 / 猎物 / 燃烧 / 空虚 / 力竭 / 磨损 / 战栗 / 脆弱等，每个造成 2 点士气伤害；士气归零后再造成 Oracle 当前 Attack 的魔法伤害并施加战栗。

推荐组合：

- 士兵：Arcanist 触发魔涌链，Duelist 负责物理收割。
- 副官：Arcanist 副官适合让 Oracle 自己获得魔涌与再行动；Duelist 副官适合把控制后窗口转成 HP 压力。
- 遗物：空心彗镜最贴合；燃烧遗物可以通过 Mage 归档线兼容。

成型方式：

1. 用 Mage `searing-brand`、Druid `weakening-spores-action`、Monster `predatory-gaze` 或 Barbarian `challenge` 堆可计数状态。
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

- 士兵：Shieldmaiden 与 Cleric。
- 副官：Shieldmaiden 副官使坚守 / 盾链保护低 HP 友方；Cleric 副官给护咒。
- 遗物：石匠令、黑铁铆钉、银护符、王墙军旗。

成型方式：

1. 早期靠 `supply-basket` 的 0 AP 稳住物理压力。
2. Shieldmaiden 的 `shield-drill` 会被补给篮的坚守触发，形成“主目标坚守 + 低 HP 友军坚守”。
3. Rank3 后 Peasant 当前 Attack 会影响全队治疗量，因此 Duelist Aura、红磨刀石、军议战旗都能间接放大后勤。

当前问题：

- 这条线能活，但缺少专属续航遗物来决定胜法。
- 治疗不回士气，所以在士气被打空后的防守价值主要还是靠坚守 / 护咒。

#### Militia Foreman

成型方向：士兵再行动、民兵群攻、物理 / 魔法通吃。

路线内容：

- Rank1 `field-work`：丰收中可再攻击；播种中则自己回复 2 HP；否则获得播种。
- Rank2：MaxHp +3、Attack +1；丰收中造成 HP 伤害时，每 turn 1 次获得 1 BP。
- Rank3 `militia-call`：选择 Peasant 或士兵，获得额外主动攻击；物理单位获得强攻，魔法单位获得魔涌；下一次主动攻击额外 +Peasant 当前 Attack，若目标是士兵再 +士兵当前 Rank。

推荐组合：

- 士兵：Duelist、Arcanist 是输出核心，Shieldmaiden / Cleric 负责站场。
- 副官：通常不急着把核心士兵转副官，因为 `militia-call` 需要士兵在场攻击。
- 遗物：红磨刀石、见习星墨、胜利鼓、红沙漏、军议战旗。

成型方式：

1. 先至少保留一名输出士兵在场，Duelist 或 Arcanist 都可以。
2. Rank1 士兵 Aura 是成型地基：Duelist 给物理 +2，Arcanist 给魔法 +2。
3. Rank3 后 `militia-call` 让士兵吃到强攻 / 魔涌、额外攻击和 Peasant 攻击力加成。
4. 胜利鼓把额外攻击造成 HP 伤害转成 BP，红沙漏奖励第三次主动攻击。

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

- 士兵：Arcanist 是核心，Shieldmaiden 保护蓄力，Cleric 补护咒。
- 副官：Arcanist 副官让 Mage 使用魔法链后获得魔涌并可再行动。
- 遗物：见习星墨、余烬星盘、空心彗镜、灰烬引爆器、余燃香炉。

成型方式：

1. Rank1 先用 `arcane-channel` 做下回合爆发预备。
2. Arcanist Rank1 Aura 与星墨会直接提高 Mage 当前 Attack。
3. Rank2 后普通攻击稳定上燃烧，配合余烬星盘快速堆层。
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

- 士兵：Arcanist 必备度最高；Druid / Oracle Fate 负责补 Debuff 计数。
- 副官：Arcanist 副官最优，Duelist 副官用于后续收 HP。
- 遗物：余烬星盘、灰烬引爆器、余燃香炉、空心彗镜、见习星墨。

成型方式：

1. 早期用 `searing-brand` 保证燃烧 + 空虚两个状态。
2. 余烬星盘让首次燃烧 +1 层，快速达到灰烬引爆器的 3 层门槛。
3. `archive-formula` 先按燃烧层数增伤，再按 Debuff 数量反过来加燃烧层数，是当前最完整的燃烧闭环。
4. 余燃香炉走持续蚕食；灰烬引爆器走攒层爆发。

当前问题：

- 如果同时拿灰烬引爆器和余燃香炉，要注意两者方向不同：一个消耗层数打爆发，一个保留燃烧打持续。

### 2.5 Druid

#### Grove Keeper

成型方向：净化、护咒、范围治疗。

路线内容：

- Rank1 `cleansing-herbs`：净化我方 1 个 Debuff 并治疗 1 HP。
- Rank2：MaxHp +3、魔防 +1；净化草药成功后给护咒，低于半血时额外治疗 1 HP。
- Rank3 `grove-sanctuary`：目标净化全部 Debuff，相邻我方各净化 1 个；若净化成功，范围内每人按 Druid 当前 Attack x 净化总数治疗；若没有净化，改为范围护咒。

推荐组合：

- 士兵：Cleric、Shieldmaiden。
- 副官：Cleric 副官让净化成功后再给护咒；Shieldmaiden 副官补坚守。
- 遗物：银护符、黑铁铆钉、石匠令。专属净化遗物当前未接入。

成型方式：

1. 只有对面确实会挂 Debuff 时，优先走这条线。
2. Cleric + Druid 可以把一次净化转成治疗、护咒、可能的额外护咒。
3. Rank3 是反 Debuff 队的终局按钮，Debuff 越多治疗越高。

当前问题：

- 当前已接入遗物不支持净化经济或净化收束。
- 如果对局 Debuff 很少，Grove Keeper 的终局价值会变成范围护咒，强度明显下降。

#### Wildspeaker

成型方向：Debuff、猎群标记、士兵协同。

路线内容：

- Rank1 `weakening-spores-action`：移除敌方可驱散 Buff，并施加力竭和磨损。
- Rank2：MaxHp +2、Attack +1；Druid Trait 未能移除 Buff 时，额外按目标攻击类型施加脆弱或空虚。
- Rank3 `call-the-hunt`：目标及相邻敌人获得猎群标记；每名我方士兵首次主动攻击每个标记目标时，额外 +Druid 当前 Attack 的普通伤害；标记目标被击败时获得 1 BP。

推荐组合：

- 士兵：Duelist、Arcanist，最好至少两名在场。
- 副官：一般优先保留士兵在场；若只需要 Aura，可把 Rank2 士兵转副官。
- 遗物：红磨刀石、见习星墨、胜利鼓、红沙漏、军议战旗。

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

- 士兵：Duelist 放大物理攻击，Cleric 修复自伤和低防风险。
- 副官：Duelist 副官提供物攻 +2、攻击后绝对伤害；Cleric 副官提供护咒。
- 遗物：红磨刀石、决斗券、绿旗、胜利鼓、红沙漏、军议战旗。

成型方式：

1. Duelist Rank1 Aura 是最早的火力台阶。
2. `war-cry` 提供第二次攻击，是胜利鼓 / 红沙漏的前置。
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

- 士兵：Duelist、Shieldmaiden。
- 副官：Duelist 副官提高 HP 压力；Shieldmaiden 副官提高站场。
- 遗物：红磨刀石、绿旗、胜利鼓、红沙漏。碎盾铃 / 壁垒锤是防守方盾遗物，不是破盾方收益。

成型方式：

1. 早期用 `challenge` 创造无反击攻击窗口。
2. 中期用 Duelist Aura 和红磨刀石提高 `dragon-breaker` 破盾量。
3. Rank3 后破盾成功会给范围战栗 + 脆弱，再由物理队收割。

当前问题：

- 对手不使用共享盾时，Dragon Raider 更像单体控制打手。
- 破盾经济目前只有绿旗，缺少更多“破盾后推进”的进攻遗物。

### 2.7 Monster

#### Nightmare Fiend

成型方向：0 HPDamage、猎物、绝对伤害追击。

路线内容：

- Rank1 `predatory-gaze`：敌方 1 体获得猎物，本回合每次受到 0 HP 伤害时追加 2 绝对伤害。
- Rank2：MaxHp +3、魔防 +1；Monster Trait 对猎物目标触发时追加绝对伤害 +1。
- Rank3 `nightmare-stare`：目标及相邻敌人获得噩梦猎物；每个目标第一次受到 0 HP 伤害时，追加 Monster 当前 Attack 的绝对伤害并消耗；主目标额外获得磨损。

推荐组合：

- 士兵：Duelist 用物理 Aura 和副官补 HP 伤害，Cleric 维持 Monster，Arcanist 可触发魔法侧再行动。
- 副官：Duelist 副官让 Monster 攻击后追加 2 绝对伤害；Cleric 副官补防。
- 遗物：红磨刀石、军议战旗会提高 Monster 当前 Attack，从而提高 Trait / 噩梦猎物绝对伤害。

成型方式：

1. 用共享盾、士气或高防造成 0 HPDamage，反而触发猎物追击。
2. `predatory-gaze` 适合挂给高防 / 有士气的目标。
3. Rank3 后用范围噩梦猎物制造多个“只要没扣 HP 就直扣 HP”的反外壳窗口。

当前问题：

- 猎物专属遗物尚未接入，缺少 Rare / Epic 拼图。
- 这条线理解成本高，预测必须清楚显示 0 HPDamage 与绝对追击。

#### Abyssal Queen

成型方向：献祭、契约、额外攻击、绝对伤害斩杀。

路线内容：

- Rank1 `dark-pact`：我方 1 体失去最多 4 HP，至少保留 1；获得契约，下一次主动攻击后追加 4 绝对伤害；若低于半血，获得 1 BP。
- Rank2：MaxHp +4、Attack +1。当前源码未见 `dark-pact` 路线的额外 Trait 强化 hook。
- Rank3 `abyssal-bargain`：我方 1 体失去最多 Monster 当前 Attack 的 HP，获得额外主动攻击；下一次攻击后追加 Monster 当前 Attack 的绝对伤害，击败则获得 1 BP，并按支付 HP 治疗 Monster。

推荐组合：

- 士兵：Cleric 修复支付 HP，Duelist 提供物理 Aura 和副官追击。
- 副官：Cleric 副官适合宿主治疗链；Duelist 副官适合把攻击变成双绝对追击。
- 遗物：军议战旗、红磨刀石、胜利鼓。献祭专属遗物当前未接入。

成型方式：

1. 早期 `dark-pact` 用在能安全攻击的单位上，压低 HP 换 BP 和绝对伤害。
2. Cleric / Princess / Peasant 负责修复支付后的 HP。
3. Rank3 后 `abyssal-bargain` 最好交给高攻击或可再行动单位，追击绝对伤害直接绕过士气。

当前问题：

- 没有献祭遗物，Dark Pact 的经济和爆发都靠英雄自身。
- Rank2 当前只有属性台阶，献祭线从 Rank1 到 Rank3 中间缺少明显的引擎升级。
- 支付 HP 不走士气，错误使用会把核心送进斩杀线。

### 2.8 Knight

#### Holy Paladin

成型方向：守护誓约、共享盾、双防防线。

路线内容：

- Rank1 `guard-oath`：给自己以外的我方单位守护誓约，受主动物理攻击时伤害 -2 并消耗 1 层。
- Rank2：MaxHp +4、魔防 +1；替身之盾触发后，被保护目标获得坚守；若目标已有守护誓约，Knight 获得护咒。
- Rank3 `holy-bastion`：给我方 1 体守护誓约层数，层数为 Knight 当前物防至少 2，并给护咒；Knight 自身获得坚守；共享盾增加 Knight 当前物防 + 魔防。

推荐组合：

- 士兵：Shieldmaiden、Cleric。
- 副官：Shieldmaiden 副官能让盾 / 坚守链保护低 HP 友方；Cleric 副官给护咒。
- 遗物：黑铁铆钉、银护符、石匠令、王墙军旗、壁垒锤。

成型方式：

1. Shieldmaiden Rank1 Aura 和黑铁铆钉提高物防，也提高 `holy-bastion` 的守护层数价值。
2. Cleric Aura / 银护符补魔防，使 `holy-bastion` 的共享盾增量更高。
3. 王墙军旗提供每回合基础盾，壁垒锤惩罚破盾攻击者。

当前问题：

- 完全防守可能缺少收割手段，建议带一个 Duelist / Barbarian / Mage 作为输出端。

#### Dread Cavalier

成型方向：共享盾转伤害、铁壁冲锋、破盾后反打。

路线内容：

- Rank1 `raise-bulwark`：我方有共享盾时可发动，共享盾 x1.5 向上取整，并给共享盾物防 +2。
- Rank2：MaxHp +3、Attack +1；我方共享盾每己方 turn 首次被击破后，Knight 获得强攻。
- Rank3 `iron-charge`：消耗我方全部共享盾，对敌方 1 体造成消耗盾值 + Knight 当前 Attack 的物理伤害并施加战栗；若造成 HP 伤害，返还该 HPDamage 一半的共享盾。

推荐组合：

- 士兵：Shieldmaiden 是核心，Duelist 提供物理 Aura。
- 副官：Shieldmaiden 副官增强盾链；Duelist 副官提高铁壁冲锋后的收割。
- 遗物：黑铁铆钉、石匠令、王墙军旗、红磨刀石、军议战旗。

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
- 但当前圣疗 / 净化遗物未接入，Cleric 更像稳定器，不是单独胜利条件。

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

### 4.1 Stage I：方向信号

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 银护符 | 3 | 我方全体魔防 +1。 | 治疗、抗魔、圣疗、盾墙。 |
| 黑铁铆钉 | 3 | 我方全体物防 +1。 | 盾墙、守护、抗物理。 |
| 见习星墨 | 4 | 我方魔法攻击单位攻击 +1。 | 魔法、燃烧、咏唱。 |
| 石匠令 | 3 | 每回合首次部署或强化共享盾时，低 HP 友方获得坚守。 | 盾墙、补给、防守。 |
| 红磨刀石 | 3 | 我方物理攻击单位攻击 +1。 | 物理、连击、Monster、Knight 冲锋。 |

### 4.2 Stage II：引擎补件

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 余烬星盘 | 5 | 每回合首次赋予燃烧时，额外 +1 层。 | 燃烧堆层。 |
| 空心彗镜 | 5 | 每回合首次只击伤士气的魔法伤害，赋予空虚。 | 魔法士气、Fate、Mage。 |
| 碎盾铃 | 5 | 每回合 1 次，共享盾被击破后，攻击者获得战栗。 | 防守盾墙。 |
| 决斗券 | 5 | 每回合首次攻击无盾敌人前，物理攻击者获得强攻。 | 物理连击。 |
| 绿旗 | 5 | 每回合首次击破敌方共享盾时，额外获得 1 BP。 | 破盾、物理节奏、BP。 |

### 4.3 Stage III：终局放大器

| 遗物 | 成本 | 当前效果 | 适合 build |
|---|---:|---|---|
| 灰烬引爆器 | 8 | 每回合 1 次，命中 3 层以上燃烧时，引爆燃烧。 | 燃烧爆发。 |
| 余燃香炉 | 8 | 燃烧基础伤害 +1。 | 燃烧持续蚕食。 |
| 王墙军旗 | 8 | 回合开始时，若共享盾为 0，获得共享盾 2。 | 盾墙复盾。 |
| 壁垒锤 | 8 | 共享盾被击破时，攻击者受到 3 士气伤害。 | 盾墙反击。 |
| 胜利鼓 | 8 | 每回合 1 次，额外攻击造成 HP 伤害时，获得 1 BP。 | 物理 / 魔法再行动。 |
| 红沙漏 | 8 | 每回合第 3 次主动攻击伤害 +3。 | 多段攻击爆发。 |
| 军议战旗 | 7 | 我方全体攻击 +1。 | 泛用终局火力。 |

## 5. 当前缺口与 pacing 诊断

### 5.1 已经比较完整的方向

魔法燃烧线比较完整：

- Common 有见习星墨。
- Rare 有余烬星盘、空心彗镜。
- Epic 有灰烬引爆器和余燃香炉两个收束方向。
- 英雄有 Mage 双线、Oracle 星读线，士兵有 Arcanist。

物理连击线也比较完整：

- Common 有红磨刀石。
- Rare 有决斗券、绿旗。
- Epic 有胜利鼓、红沙漏。
- 英雄有 War Queen、Radiant Berserker、Militia Foreman，士兵有 Duelist。

盾墙线比较完整：

- Common 有黑铁铆钉、石匠令。
- Rare 有碎盾铃。
- Epic 有王墙军旗、壁垒锤。
- 英雄有 Knight 双线，士兵有 Shieldmaiden。

### 5.2 当前明显缺拼图的方向

治疗 / 净化缺遗物终局：

- `relic-white-lily-censer`、`relic-cleanse-votive`、`relic-oath-keystone`、`relic-saint-bell` 在设计文档里存在，但当前源码没有接入。
- 结果是 Saint Queen、Grove Keeper、Quartermaster 可以续航，但缺少“拿到 Epic 后 build 成形”的瞬间。

Debuff / 控制缺遗物闭环：

- `relic-witch-bell`、`relic-spore-press`、`relic-cracked-mask`、`relic-plague-codex`、`relic-lockjaw-mask` 当前未接入。
- Fate Dealer、Wildspeaker、Dragon Raider 的状态堆叠目前主要靠英雄自身，缺少遗物把控制转化成士气压制或 HP 斩杀。

士兵团缺专属遗物：

- `relic-muster-papers`、`relic-shared-drillbook`、`relic-veteran-captain-badge`、`relic-command-sergeant-seal`、`relic-company-standard` 当前未接入。
- Militia Foreman 和 Wildspeaker 需要多名士兵，但奖励阶段还没有帮助招募、升级、保留士兵的遗物。

猎物 / 献祭缺专属遗物：

- `relic-blood-coin`、`relic-night-bait`、`relic-hunter-fang`、`relic-predator-crown`、`relic-abyss-contract-seal` 当前未接入。
- Nightmare Fiend 与 Abyssal Queen 当前主要靠 Monster 本体强度，缺少遗物路线成型感。

BP / AP / 士气经济缺专属遗物：

- `relic-campaign-ledger`、`relic-supply-cart`、`relic-brass-order`、`relic-command-table`、`relic-war-room-map` 当前未接入。
- 现在只有绿旗、胜利鼓在补 BP 侧，无法构成完整经济 build。

### 5.3 奖励 pacing 风险

当前源码中奖励窗口是 round 4 起每 4 round 一次。这会让需要多个奖励阶段的 build 明显变慢：

- 士兵团至少需要招募士兵、升级士兵、再拿英雄 Rank3 或遗物，成型点偏晚。
- 治疗 / Debuff / 猎物 / 献祭因为专属遗物未接入，即使进入 Stage III 也可能刷不到真正收束件。
- 当前遗物刷新没有 build tag 权重，可能出现玩家已经走燃烧线但刷出盾 / 物理遗物的情况。
- Stage III 可以因已有 Rank2 单位提前开放，但如果玩家没有足够 BP，Epic 仍然只是可见不可买。

## 6. 实用成型模板

### 模板 A：燃烧爆发

核心：

- Mage 走 `searing-brand` -> `archive-formula`。
- Oracle 走 `star-reading` -> `astral-alignment`。
- Arcanist 至少 Rank1，最好 Rank2。

关键遗物：

- 见习星墨。
- 余烬星盘。
- 灰烬引爆器或余燃香炉。

打法：

1. 用 `searing-brand` 稳定挂燃烧 + 空虚。
2. Arcanist 给魔法攻击 +2，触发魔涌。
3. 余烬星盘把燃烧推到 2 层，Mage Rank3 或多次攻击推到 3 层。
4. 灰烬引爆器负责爆发，余燃香炉负责持续伤害。

### 模板 B：第三击物理爆发

核心：

- Barbarian 走 `war-cry` -> `glory-roar`，或 Princess 走 War Queen。
- Duelist Rank1 / Rank2。
- 可带 Peasant `militia-call`。

关键遗物：

- 红磨刀石。
- 决斗券。
- 胜利鼓。
- 红沙漏。

打法：

1. Duelist Aura 和红磨刀石抬物理攻击。
2. 用 `war-cry`、`edict-of-victory`、`militia-call` 制造额外攻击。
3. 第三次主动攻击吃红沙漏 +3。
4. 额外攻击若造成 HP 伤害，胜利鼓返 1 BP。

### 模板 C：盾墙铁壁冲锋

核心：

- Knight 走 `raise-bulwark` -> `iron-charge`。
- Shieldmaiden Rank1 / Rank2。
- Peasant Quartermaster 或 Cleric 保持队伍状态。

关键遗物：

- 黑铁铆钉。
- 石匠令。
- 王墙军旗。
- 壁垒锤。

打法：

1. 用基础部署或 `aegis-formation` 建盾。
2. `raise-bulwark` 增厚共享盾并触发石匠令 / Shieldmaiden。
3. 等盾值足够后，`iron-charge` 把盾转成物理伤害。
4. 若只打士气，返盾不会触发；要尽量在敌方士气低时冲锋。

### 模板 D：猎物绝对追击

核心：

- Monster 走 `predatory-gaze` -> `nightmare-stare`。
- Duelist Rank1 或副官。
- Princess 存活时，Monster 对非公主 0 HPDamage 追击 +1。

关键遗物：

- 红磨刀石。
- 军议战旗。
- 决斗券可用于 Monster 的物理攻击窗口。

打法：

1. 给目标挂猎物。
2. 通过高防、士气、共享盾或低伤攻击制造 0 HPDamage。
3. 猎物 / 噩梦猎物触发绝对伤害，直接扣 HP。
4. Duelist 副官可以在 Monster 攻击后再补 2 绝对伤害。

### 模板 E：民兵士兵团

核心：

- Peasant 走 `field-work` -> `militia-call`。
- 至少 2 名士兵，其中 Duelist / Arcanist 优先。
- Druid `call-the-hunt` 可作为第二核心。

关键遗物：

- 红磨刀石或见习星墨。
- 胜利鼓。
- 红沙漏。
- 军议战旗。

打法：

1. 先让输出士兵到 Rank1，开启攻击 Aura。
2. Peasant Rank3 后用 `militia-call` 给士兵额外攻击和伤害。
3. Druid Rank3 可给敌方挂猎群标记，让每名士兵首次攻击都吃 Druid 当前 Attack 加成。
4. 当前缺士兵专属遗物，成型较慢，建议不要只押这一条线。

## 7. 设计后续建议

如果下一步要解决“有些 build 不成形”，优先级建议：

1. 先接入治疗 / 净化、Debuff / 控制、士兵团、猎物 / 献祭、BP / AP 这五组未实装遗物中的 Common + Rare，而不是只补 Epic。
2. 给遗物刷新加入 build tag 权重：根据已选英雄路线、在场士兵、已持遗物提高相关候选出现率。
3. 检查奖励窗口节奏。当前源码是 round 4 起每 4 round，如果目标是更快成型，需要重新评估周期或奖励购买能力。
4. 士兵团需要特别照顾 pacing：至少要有降低士兵招募 / 升级成本，或让多士兵在场更早产生收益的遗物。
5. 绝对伤害和士气互动要继续保持边界：普通伤害先打士气；绝对伤害直扣 HP；HPDamage 触发只看真实 HPDamage。
