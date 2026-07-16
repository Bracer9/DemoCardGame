# Tiny Pixel Fights - Soldier Design Baseline

更新时间：2026-07-12

本文定义当前 5 个士兵的基础属性、初始 Trait、成长终点 Role Action 与英雄联动。
设计基准参考：

- `reference/TinyPixelFights_Role_Action_Table.md`
- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`

## 1. 设计口径

- 普通兵统一称为 `Soldier / 士兵`。
- 士兵初始没有 Role Action，只有 Attack + 初始 Trait。
- 士兵最终升级时只解锁 1 个固定 Role Action，不做二选一。
- 士兵初始 Trait 不新增专属 Buff / Debuff，只使用通用状态或既有系统事件。
- 士兵 Rank1 会在初始 Trait 上附加一个持续光环；光环只要来源士兵存活并在战场就生效，不作为普通 Buff / Debuff，不可驱散。
- Rank2 士兵成为副官后，Rank1 光环继续生效；此时光环来源视为“副官绑定的宿主英雄存活并在战场”。
- 同名 Rank1 士兵光环不叠加；不同士兵光环可以同时存在。
- 通用状态默认可驱散 / 可净化，文本不额外写“可驱散”。
- 士兵不应压过英雄；初期价值来自协同，终局价值来自成为体系零件。

## 2. 通用状态引用

本设计只使用以下已定义通用状态：

| 状态 | 类型 | 摘要 |
|---|---|---|
| 强攻 | Buff | 主动物理 / 普通攻击伤害 x1.5，向上取整。 |
| 咏唱 | Buff | 下一次魔法伤害 x2。 |
| 坚守 | Buff | 受到物理伤害 x0.5。 |
| 护咒 | Buff | 受到魔法伤害 x0.5。 |
| 脆弱 | Debuff | 受到物理伤害 x1.25，向上取整。 |
| 空虚 | Debuff | 受到魔法伤害 x1.25，向上取整。 |
| 战栗 | Debuff | 无法反击。 |

## 3. 士兵总览

| Soldier ID | 名称 | 进阶名 | Cost | ATK | HP | 攻击类型 | 物防 | 魔防 | 定位 |
|---|---|---|---:|---:|---:|---|---:|---:|---|
| `cleric` | 牧师 | `saint-cleric` | 1 | 1 | 14 | Magical | 0 | 1 | 治疗 / 净化响应，保护低魔防队友 |
| `shieldmaiden` | 盾卫 | `aegis-shieldmaiden` | 2 | 1 | 20 | Physical | 1 | 0 | 共享盾 / 坚守响应，保护蓄力与低 HP 核心 |
| `duelist` | 决斗士 | `crimson-duelist` | 1 | 3 | 12 | Physical | 0 | -1 | 标记 / Debuff 收割，低费物理爆发 |
| `arcanist` | 秘术士 | `astral-arcanist` | 2 | 3 | 12 | Magical | -1 | 1 | 魔法 / 状态共鸣，放大魔法队 |
| `jester` | 弄臣 | `masque-jester` | 1 | 2 | 13 | Magical | -1 | 0 | Debuff 启动 / 扩散，混合队伍控制 |

## 4. 成长阶段

| 阶段 | 士兵规则 |
|---|---|
| Rank0 | 初始属性 + 初始 Trait，无 Role Action。 |
| Rank1 | 小幅属性成长；强化初始 Trait；Trait 额外附加一个持续队伍光环。 |
| Rank2 | 在 Rank1 基础上最大 HP +5，升阶瞬间 HP 全回复；进阶为唯一高级兵种，解锁固定 Role Action。 |

士兵升至 Rank1 时，当前 HP 回复新的最大 HP 的 50%；升至 Rank2 时，当前 HP 回复到新的最大 HP。

## 5. Cleric / Saint Cleric

### Rank0 属性

| Cost | ATK | HP | 攻击类型 | 物防 | 魔防 |
|---:|---:|---:|---|---:|---:|
| 1 | 1 | 14 | Magical | 0 | 1 |

### 初始 Trait

| Field | Value |
|---|---|
| ID | `field-medic` |
| 名称 | 战地医护 |
| Trigger | 每个己方 turn 第一次我方 Role Action 造成有效治疗，或通过 Role Action 成功净化 1 个 Debuff 后 |
| Scope | 该被治疗 / 被净化的我方角色 |
| Effect | 目标获得 1 turn 护咒 |
| Limit | 每个己方 turn 1 次；不因无效治疗触发；不响应 turn start、Aura、Trait 造成的治疗 |

### Rank1

- HP +2。
- `field-medic` 触发后，若目标当前 HP 低于一半，额外回复 3 HP。
- 持续光环：只要 Cleric 存活并在战场，所有我方角色魔防 +1。

### Rank2

- 在 Rank1 基础上最大 HP +5。
- 升至 Rank2 时，当前 HP 回复到新的最大 HP。
- 解锁固定 Role Action：

| Field | Value |
|---|---|
| ID | `mend` |
| 名称 | 应急治疗 |
| Input | TargetSelect |
| Target | AllyCard |
| Cost | 1 AP |
| Repeatable | No |
| Cooldown | 0 |
| Tags | heal, cleanse, holy, soldier |
| Effect | 治疗目标 2 HP；若目标有 Debuff，先净化 1 个 Debuff，并使目标获得 1 turn 护咒 |

### 明确联动

| 对象 | 联动点 |
|---|---|
| Princess `saintly-prayer` | 主动治疗 / 净化触发 `field-medic`，让被治疗者顺带获得护咒。Princess 的 turn start 光环不触发。 |
| Druid `cleansing-herbs` | Role Action 净化成功触发 `field-medic`，形成净化 + 护咒链。 |
| Monster `dark-pact` | 黑暗契约失血后，Cleric 用 `mend` 修复代价。 |
| Barbarian `war-cry` | 狂战士自降防后，Cleric 提供魔法承伤保护。 |

## 6. Shieldmaiden / Aegis Shieldmaiden

### Rank0 属性

| Cost | ATK | HP | 攻击类型 | 物防 | 魔防 |
|---:|---:|---:|---|---:|---:|
| 2 | 1 | 20 | Physical | 1 | 0 |

### 初始 Trait

| Field | Value |
|---|---|
| ID | `shield-drill` |
| 名称 | 盾阵训练 |
| Trigger | 每个己方 turn 第一次我方共享盾增加，或我方角色获得坚守时 |
| Scope | 当前 HP 比例最低的我方存活角色 |
| Effect | 目标获得 1 turn 坚守 |
| Limit | 每个己方 turn 1 次；不响应自身本次 Trait 赋予的坚守 |

### Rank1

- HP +2。
- 持续光环：只要 Shieldmaiden 存活并在战场，所有我方角色物防 +1。
- `shield-drill` 触发目标若已经拥有坚守，则改为延长 1 turn。

### Rank2

- 在 Rank1 基础上最大 HP +5。
- 升至 Rank2 时，当前 HP 回复到新的最大 HP。
- 解锁固定 Role Action：

| Field | Value |
|---|---|
| ID | `aegis-formation` |
| 名称 | 盾卫阵列 |
| Input | Click |
| Target | OwnShield |
| Cost | 1 AP |
| Repeatable | No |
| Cooldown | 0 |
| Tags | shield, guard, soldier |
| Effect | 若我方没有共享盾，获得 1 层共享盾；若已有共享盾，共享盾 +2。该共享盾增加可以触发 `shield-drill` |

### 明确联动

| 对象 | 联动点 |
|---|---|
| Knight `raise-bulwark` | 骑士加厚共享盾会触发 `shield-drill`，形成盾 + 坚守。 |
| Knight `guard-oath` | 守护誓约给坚守，触发 `shield-drill` 补保护另一个低 HP 目标。 |
| Peasant `supply-basket` | 补给篮给坚守，触发 `shield-drill`，形成低费防线。 |
| Mage `arcane-channel` | Shieldmaiden 保护蓄力中的 Mage，帮助下回合咏唱兑现。 |

## 7. Duelist / Crimson Duelist

### Rank0 属性

| Cost | ATK | HP | 攻击类型 | 物防 | 魔防 |
|---:|---:|---:|---|---:|---:|
| 1 | 3 | 12 | Physical | 0 | -1 |

### 初始 Trait

| Field | Value |
|---|---|
| ID | `duel-sense` |
| 名称 | 决斗嗅觉 |
| Trigger | 每个己方 turn 第一次 Duelist 主动攻击对敌人造成 HP 伤害后 |
| Scope | Self |
| Effect | Duelist 获得 1 turn 强攻 |
| Limit | 每个己方 turn 1 次 |

### Rank1

- HP +1。
- 每个己方 turn 1 次，Duelist 主动攻击后，若目标仍存活，对目标追加 2 点绝对伤害。
- 持续光环：只要 Duelist 存活并在战场，我方物理攻击单位攻击 +2。

### Rank2

- 在 Rank1 基础上最大 HP +5。
- 升至 Rank2 时，当前 HP 回复到新的最大 HP。
- 解锁固定 Role Action：

| Field | Value |
|---|---|
| ID | `crimson-lunge` |
| 名称 | 致命架势 |
| Input | TargetSelect |
| Target | Allied physical unit, including self |
| Cost | 1 AP |
| Repeatable | No |
| Cooldown | 0 |
| Tags | physical, mighty-strike, support, soldier |
| Effect | 目标获得 `mighty-strike`（下一次主动物理伤害 ×2） |

### 明确联动

| 对象 | 联动点 |
|---|---|
| Knight / Shield 系 | 敌方共有盾被打空后，Duelist 获得强攻与绝对伤害窗口；`crimson-lunge` 可把猛击交给物理主攻兑现。 |
| Druid `weakening-spores-action` / `weakening-spores` | 力竭 / 磨损降低敌方输出，让物理单位更安全地等待猛击窗口。 |
| Mage `searing-brand` / `searing-mark` | 燃烧 / 空虚压低敌方血线，配合物理单位用猛击收割。 |
| Barbarian `challenge` | 战栗创造安全进攻窗口，便于物理单位兑现猛击。 |

## 8. Arcanist / Astral Arcanist

### Rank0 属性

| Cost | ATK | HP | 攻击类型 | 物防 | 魔防 |
|---:|---:|---:|---|---:|---:|
| 2 | 3 | 12 | Magical | -1 | 1 |

### 初始 Trait

| Field | Value |
|---|---|
| ID | `arcane-resonance` |
| 名称 | 秘术共鸣 |
| Trigger | 每个己方 turn 第一次我方 Hero 造成魔法伤害，或赋予燃烧 / 空虚 / 力竭 / 磨损后 |
| Scope | Rank0: highest-attack allied magical Hero; Rank1+: all allied magical units |
| Effect | 魔攻最高的我方魔法英雄获得 `magic-surge`（主动魔法伤害 ×1.5，向上取整） |
| Limit | 每个己方 turn 1 次 |

### Rank1

- HP +2。
- `arcane-resonance` 的目标扩展为我方全体魔法单位。
- `arcane-resonance` 若由 Role Action 触发，`magic-surge` 持续时间 +1 turn。
- 持续光环：只要 Arcanist 存活并在战场，我方魔法攻击单位攻击 +2。

### Rank2

- 在 Rank1 基础上最大 HP +5。
- 升至 Rank2 时，当前 HP 回复到新的最大 HP。
- 解锁固定 Role Action：

| Field | Value |
|---|---|
| ID | `astral-focus` |
| 名称 | 星界聚焦 |
| Input | TargetSelect |
| Target | Allied magical unit, including self |
| Cost | 1 AP |
| Repeatable | No |
| Cooldown | 0 |
| Tags | magic, chant, support, soldier |
| Effect | 目标获得 `chant`（下一次魔法伤害 ×2） |

### 明确联动

| 对象 | 联动点 |
|---|---|
| Mage `searing-brand` / `arcane-channel` | Mage 赋予燃烧 / 空虚或造成魔法伤害，触发 `arcane-resonance`，获得持续魔法增伤窗口；`astral-focus` 提供咏唱爆发。 |
| Oracle `star-reading` | 让已经攻击过的 Mage 等魔法单位再行动，扩大 `magic-surge` 与咏唱兑现窗口。 |
| Druid `weakening-spores-action` | 赋予力竭 / 磨损，触发 Arcanist 共鸣。 |
| Princess `royal-command` | AP 前借让 Arcanist 有空间同时使用 `astral-focus` 与攻击。 |

## 9. Jester / Masque Jester

### Rank0 属性

| Cost | ATK | HP | 攻击类型 | 物防 | 魔防 |
|---:|---:|---:|---|---:|---:|
| 1 | 2 | 13 | Magical | -1 | 0 |

### 初始 Trait：恶意戏谑

每个己方 turn 1 次：主动攻击没有共有盾保护的敌人前，50% 赋予与目标攻击类型对应的 2 turn 力竭或磨损；若本次主动攻击造成 HP 伤害，则主目标改为必定赋予。共有盾完全挡住攻击时不会消耗本 turn 的触发机会。

### Rank1

- Max HP +2。
- 若主目标在攻击前已经拥有任意 Debuff，同时对每个相邻敌人独立进行一次 50% 判定，赋予与该目标攻击类型对应的力竭或磨损。
- 持续光环：我方主动攻击拥有 Debuff 的敌人时，伤害 +1；同名光环不叠加。

### Rank2

- 在 Rank1 基础上最大 HP +5，升阶时当前 HP 全回复。
- `恶意戏谑` 强化：主目标与每个扩散目标不再进行 50% 判定，确定赋予与各自攻击类型对应的力竭或磨损。
- 解锁固定 Role Action：

| Field | Value |
|---|---|
| ID | `mocking-curtain-call` |
| 名称 | 虚假谢幕 |
| Input | TargetSelect |
| Target | EnemyCard |
| Cost | 1 AP |
| Repeatable | No |
| Cooldown | 0 |
| Tags | debuff, control, soldier |
| Effect | 敌方1体获得2 turn脆弱和空虚。 |

### 副官

- `deputy-jester`：宿主攻击 +1。
- 每个己方 turn 1 次，宿主主动攻击敌人，或以 Role Action 指定敌人并成功结算后，若主目标仍存活，根据目标攻击类型赋予2 turn力竭或磨损。
- 副官只处理主目标，且在原行动完整结算后触发；不要求原目标已有 Debuff，不要求原行动造成 HP 伤害。
- Rank1 光环在成为副官后继续生效。

### 明确联动

| 对象 | 联动点 |
|---|---|
| Fate Dealer `fate-mark` / `thread-cut` | 副官在敌方 Role Action 后补充输出削弱，帮助命运路线维持可计数 Debuff。 |
| Arcane Archivist `archive-formula` | 弄臣负责早期挂力竭 / 磨损，Rank2 的脆弱 / 空虚提高归档术式的混合输出价值。 |
| Wildspeaker `call-the-hunt` | 弄臣扩散 Debuff，猎群标记再由多名士兵转换成攻击压力。 |
| Dragon Raider `dragon-breaker` | 战栗 / 脆弱与弄臣的输出削弱组成控制窗口。 |

## 10. 开局组合参考

| 开局英雄 | 推荐士兵 | 初期打法 |
|---|---|---|
| Princess | Cleric + Shieldmaiden | 治疗 / 护咒 / 坚守保护低 HP 公主。 |
| Knight | Shieldmaiden + Cleric | 盾与坚守连锁，Cleric 修复穿透伤害。 |
| Oracle | Duelist + Arcanist | 标记触发 Duelist，魔法 / 状态触发 Arcanist。 |
| Mage | Arcanist + Shieldmaiden | Arcanist 放大魔法，Shieldmaiden 保护蓄力。 |
| Druid | Cleric + Duelist | 净化触发 Cleric，力竭 / 磨损给 Duelist 收割。 |
| Barbarian | Shieldmaiden + Cleric | Shieldmaiden / Cleric 补偿战吼后的防御风险。 |
| Monster | Cleric + Duelist | Cleric 修复献祭，Duelist 利用猎物 / Debuff 窗口收割。 |
| Peasant | Shieldmaiden + Duelist | 低费多行动，补给 / 坚守保护 Duelist 斩杀窗口。 |

## 11. 实装检查点

1. Soldier 需要 `CardType.Soldier`，不要复用 Hero 成长路径。
2. Rank0 Trait 不新增专属状态 ID。
3. Rank2 Role Action 尽量只复用通用状态、治疗、净化、共享盾。
4. `shield-drill` 必须防止自身赋予坚守后递归触发。
5. `duel-sense` 第一版只认通用 Debuff，不认命运标记、猎物、黑暗契约等特殊状态。
6. `arcane-resonance` 需要通用事件 hook：魔法伤害造成、燃烧 / 空虚 / 力竭 / 磨损赋予成功。
7. 预测 UI 需要显示强攻、猛击、魔涌、咏唱、脆弱、空虚对本次伤害的影响。
