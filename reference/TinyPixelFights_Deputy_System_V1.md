# Tiny Pixel Fights — 副官系统 V1

更新时间：2026-07-04

本文定义第一版 `Deputy / 副官` 系统。目标是在不显著增加 UI、状态图标和玩家认知负担的前提下，让 Rank2 士兵在后期拥有新的战略用途：牺牲一个可行动士兵，换取一个英雄的永久强化，并腾出一个手牌 / 队伍 slot。

相关文档：

- `reference/TinyPixelFights_Role_Action_Table.md`
- `reference/TinyPixelFights_Soldier_Design_20260704.md`
- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_Status_Display_Groups.md`

---

## 1. 设计目标

副官系统用于解决三个问题：

1. **Rank2 士兵的后期去向**  
   士兵 Rank2 解锁固定 Role Action 后，玩家可以选择继续把它作为可行动单位使用，也可以把它转化为英雄的长期副官。

2. **手牌 / 队伍 slot 压力**  
   士兵转为副官后离开手牌区，不再占用可上手 / 可行动 slot。

3. **让士兵成为体系零件**  
   士兵不应压过英雄。副官给英雄提供小幅永久数值 + 一个低频、易懂、复用通用状态的副官特性。

副官不是新的英雄成长树，也不是新的 Role Action。V1 不做 8 英雄 × 4 士兵的专属相性表。

---

## 2. 核心口径

### 2.1 副官是什么

副官是一个绑定在英雄身上的永久被动附件。

```text
Rank2 士兵 → 任命为某个英雄的副官 → 士兵离开手牌区 → 英雄获得副官加成
```

副官提供：

1. 一个固定基础数值加成；
2. 一个根据该士兵 Trait 改写而来的副官特性。

### 2.2 副官不是什么

副官 V1 不做以下内容：

- 不给英雄新增一个额外 Role Action 按钮。
- 不继承士兵 Rank2 Role Action。
- 不新增专属 Buff / Debuff。
- 不新增英雄专属副官效果矩阵。
- 不参与普通驱散 / 净化。
- 不作为卡面右侧常驻 Buff 图标显示。

如果玩家想继续使用士兵的 Attack、Trait 和 Rank2 Role Action，就保留士兵作为可行动卡。成为副官后，士兵失去主动行动能力。

---

## 3. 任命规则

### 3.1 开放条件

一个士兵可以任命为副官，需要满足：

| 条件 | 规则 |
|---|---|
| 卡牌类型 | 必须是 `CardType.Soldier`。 |
| Rank | 必须达到 Rank2。 |
| 阵营 | 必须是我方士兵。 |
| 状态 | 不能已经是副官。 |
| 行动状态 | 本己方 turn 内未主动攻击、未使用 Role Action。 |
| 目标 | 必须选择我方存活 Hero。 |
| 英雄限制 | 目标英雄当前没有副官。 |

V1 推荐：任命不消耗 AP，不消耗攻击权，不给 BP。

### 3.2 不可逆规则

任命后不可撤销。

```text
任命后：
- 士兵进入 Deputy zone；
- 不再显示为手牌 / 可行动卡；
- 不再能攻击；
- 不再能使用 Rank2 Role Action；
- 不再能成为敌人目标；
- 不再承伤；
- 不再触发原本作为独立士兵时的 Trait；
- 只保留副官加成与副官特性。
```

### 3.3 死亡与失效

V1 建议：副官绑定英雄。

| 情况 | 规则 |
|---|---|
| 宿主英雄存活 | 副官效果正常生效。 |
| 宿主英雄死亡 | 副官效果暂停；副官不返回手牌。 |
| 宿主英雄复活 | 如果游戏支持复活，副官效果恢复。 |
| 战斗结束 | 跟随当前成长系统结算。V1 若没有跨战斗副官保存，可以先按本场战斗永久处理。 |

如果项目已有 run-level 持久化，再把 `DeputyAssignment` 保存到 run state；否则先做 battle-level permanent。

---

## 4. UI 流程

### 4.1 士兵 Inspector

当玩家点击 Rank2 士兵卡时，在左侧 / 右侧 Inspector 的士兵信息下方增加按钮：

```text
[任命副官]
```

按钮 tooltip：

```text
将该 Rank2 士兵任命为一名英雄的副官。
士兵会离开手牌区，不再能行动；目标英雄获得永久加成。
不可撤销。
```

按钮灰掉时显示原因：

| 灰掉原因 | 文案 |
|---|---|
| 未达到 Rank2 | 只有 Rank2 士兵可以任命为副官。 |
| 士兵已行动 | 该士兵本 turn 已行动，不能任命为副官。 |
| 已经是副官 | 该士兵已经是副官。 |
| 没有合法英雄 | 没有可以任命的英雄。 |
| 敌方 turn | 只能在己方 turn 任命副官。 |

### 4.2 选择目标英雄

点击 `[任命副官]` 后进入目标选择模式：

```text
请选择一名英雄作为副官宿主
```

合法目标高亮：

- 我方 Hero；
- 存活；
- 当前没有副官。

不合法目标不高亮。右键 / ESC 取消。

### 4.3 确认弹窗

因为任命不可逆，V1 建议使用一个简单 confirm：

```text
确定让「{士兵名}」成为「{英雄名}」的副官吗？

{士兵名} 将离开手牌区，不能再攻击或使用 Role Action。
{英雄名} 将获得：
- {基础数值加成}
- {副官特性摘要}
```

### 4.4 任命后显示

任命成功后：

1. 士兵卡从手牌区移除，释放 slot。
2. 英雄卡获得一个小型副官徽章，不占 Buff / Debuff 图标位。
3. 英雄 Inspector 中在 Trait 下方显示副官信息。

示例：

```text
Trait：灼热刻印
副官：秘术士
永久加成：魔防 +1
副官特性：每己方 turn 1 次，宿主造成魔法伤害或赋予燃烧 / 空虚 / 力竭 / 磨损后，获得魔涌。
Role Action：灼热刻印
```

### 4.5 日志

任命时写入战斗日志：

```text
牧师成为公主的副官。公主获得魔防 +2 与「战地医护」。
```

副官触发时写入简短日志：

```text
副官「牧师」触发：目标获得护咒。
```

---

## 5. 状态与显示组规则

副官自身不是普通 Buff / Debuff。

| 内容 | 处理方式 |
|---|---|
| 副官绑定 | `DeputyAssignment` / `HeroPassiveAttachment`，不是 StatusEffect。 |
| 基础数值加成 | 进入角色有效属性计算，不进 Buff 列表。 |
| 副官特性 | 作为被动 hook 存在，不进 Buff 列表。 |
| 副官触发产生的状态 | 使用现有通用状态，例如强攻、魔涌、坚守、护咒。 |
| 驱散 / 净化 | 不能驱散副官；但副官产生的普通 Buff 可以被驱散。 |
| UI | 英雄 Inspector 显示“副官”小节；卡面最多显示副官小徽章。 |

如果当前 UI 必须把所有长期信息都放到 Display Group，可以把副官归入一个低优先级信息组：

```text
DisplayGroupId: deputy
显示名: 副官
类型: Informational / Passive
是否 Buff: No
是否 Debuff: No
是否可驱散: No
卡面优先级: 极低
Inspector: 显示
```

不要给 4 个副官分别新增 `cleric-deputy-buff`、`shieldmaiden-deputy-buff` 等状态。

---

## 6. 副官效果总表

V1 每个士兵只有一个固定副官效果。所有英雄通用。

| 士兵 | 副官 ID | 基础数值加成 | 副官特性 | 复用状态 |
|---|---|---|---|---|
| 牧师 / Saint Cleric | `deputy-cleric` | 魔防 +2 | 每己方 turn 1 次，宿主的 Role Action 造成有效治疗，或宿主的 Role Action 成功净化 1 个 Debuff 后，被治疗 / 被净化目标获得护咒。若目标当前 HP 低于一半，护咒持续改为 2 turn。 | 护咒 |
| 盾卫 / Aegis Shieldmaiden | `deputy-shieldmaiden` | 物防 +2 | 每己方 turn 1 次，宿主使我方共享盾增加，或宿主给我方角色赋予坚守时，当前 HP 比例最低的我方存活角色获得 1 turn 坚守；若目标已有坚守，则延长 1 turn。不响应本副官赋予的坚守。 | 坚守 |
| 决斗士 / Crimson Duelist | `deputy-duelist` | 物攻 +2 | 每己方 turn 1 次，宿主主动攻击对敌人造成 HP 伤害后获得强攻；本次攻击后若目标仍存活，追加 2 点绝对伤害。 | 强攻 / 绝对伤害 |
| 秘术士 / Astral Arcanist | `deputy-arcanist` | 魔攻 +2 | 每己方 turn 1 次，宿主造成魔法伤害，或宿主成功赋予燃烧 / 空虚 / 力竭 / 磨损后，宿主获得魔涌，并可以再次主动攻击。若由宿主的 Role Action 触发，魔涌持续时间 +1 turn。 | 魔涌 |

说明：

- “宿主”指被选为副官目标的英雄。
- “通用 Debuff”指燃烧、脆弱、空虚、力竭、磨损、战栗等通用 Debuff。V1 不包括命运标记、猎物、黑暗契约等特殊状态。
- 副官特性触发产生的强攻、魔涌、坚守、护咒仍是普通 Buff，默认可驱散。
- 副官本体和基础数值加成不可驱散。

---

## 7. 设计理由

### 7.1 为什么不继承士兵 Rank2 Role Action

如果副官让英雄获得额外 Role Action，会立刻引发 UI 和认知负担问题：

```text
英雄自身 Role Action
+ 副官 Role Action
+ 英雄 Trait
+ 副官 Trait
+ 现有 Buff / Debuff
```

这会让左侧 HUD、按钮 CD、目标选择、拖拽逻辑和文本说明全部变复杂。

V1 的取舍是：

```text
保留士兵：多一个可行动单位 + 士兵 Trait + 士兵 Rank2 Role Action，但占 slot。
任命副官：失去士兵行动与 Role Action，换英雄永久强化并腾 slot。
```

这个选择清楚，玩家也容易理解。

### 7.2 为什么每个英雄不做专属副官效果

不做 8 × 4 专属表，是为了避免系统爆炸。

V1 只做 4 个副官模板：

```text
牧师：治疗 / 净化 → 护咒
盾卫：盾 / 坚守 → 坚守
决斗士：打 Debuff 目标 → 强攻
秘术士：魔法 / 状态 → 魔涌
```

英雄差异来自“谁更适合触发这些模板”，不是来自每个组合都写一条特殊规则。

### 7.3 为什么基础加成不作为 Buff 显示

副官的基础加成是永久成长，不是战斗中可驱散状态。

因此：

- 物攻 +2 只体现在物理攻击单位的攻击数值上；
- 魔攻 +2 只体现在魔法攻击单位的攻击数值上；
- 物防 +2 直接体现在物防数值上；
- 魔防 +2 直接体现在魔防数值上；

Inspector 展开时说明来源即可。

---

## 8. 英雄推荐搭配

以下只是推荐，不是专属规则。

| 英雄 | 推荐副官 | 理由 |
|---|---|---|
| 公主 | 牧师、盾卫 | 公主主动治疗 / 净化可以触发牧师副官；盾卫提高公主生存。 |
| 骑士 | 盾卫、牧师 | 骑士的盾与守护体系容易触发盾卫副官；牧师提升团队治疗链。 |
| 狂战士 | 盾卫、牧师、决斗士 | 盾卫 / 牧师补偿战吼后的防御风险；决斗士提高收割能力。 |
| 魔法师 | 秘术士、盾卫 | 秘术士通过魔法伤害、燃烧、空虚给魔法师魔涌；盾卫保护蓄能窗口。 |
| 占卜师 | 秘术士、决斗士 | 秘术士放大魔法 / 状态节奏；决斗士配合命运标记后的集火窗口，但命运标记本身不算通用 Debuff。 |
| 怪物 | 决斗士、牧师 | 决斗士放大怪物对 Debuff 目标的爆发；牧师提升魔防，帮助承受魔法压力。 |
| 农民 | 盾卫、决斗士、牧师 | 农民补给篮和坚守体系适合盾卫；决斗士配合低费收割；牧师强化治疗支援。 |
| 德鲁伊 | 牧师、决斗士、秘术士 | 德鲁伊净化触发牧师；力竭 / 磨损给决斗士创造目标；施加状态可触发秘术士。 |

---

## 9. 实装结构建议

### 9.1 GameState

新增或扩展：

```csharp
public enum CardZone
{
    Hand,
    Field,
    Discard,
    Deputy
}

public sealed class DeputyAssignment
{
    public string SoldierInstanceId { get; init; }
    public string HostHeroInstanceId { get; init; }
    public string DeputyEffectId { get; init; }
}
```

英雄状态中增加：

```csharp
public string? DeputySoldierInstanceId { get; set; }
```

士兵状态中增加：

```csharp
public string? DeputyHostHeroInstanceId { get; set; }
```

### 9.2 定义表

新增 `DeputyDefinitions.cs` 或放入现有 Soldier definition：

```csharp
public sealed class DeputyDefinition
{
    public string Id { get; init; }
    public string SourceSoldierId { get; init; }
    public StatModifier PermanentModifier { get; init; }
    public string PassiveHookId { get; init; }
    public string[] Tags { get; init; }
}
```

建议 tags：

| 副官 | Tags |
|---|---|
| `deputy-cleric` | `deputy`, `heal`, `cleanse`, `holy`, `soldier` |
| `deputy-shieldmaiden` | `deputy`, `shield`, `guard`, `soldier` |
| `deputy-duelist` | `deputy`, `physical`, `debuff-synergy`, `soldier` |
| `deputy-arcanist` | `deputy`, `magic`, `charge`, `status-synergy`, `soldier` |

### 9.3 GameEngine API

新增命令：

```text
AssignDeputy(soldierInstanceId, heroInstanceId)
```

校验顺序：

1. 找到士兵 instance。
2. 确认是我方 Soldier。
3. 确认 Rank2。
4. 确认当前 zone 是 Hand / Field 中可任命区域。
5. 确认本 turn 未攻击、未使用 Role Action。
6. 找到目标英雄。
7. 确认目标是我方 Hero、存活、无副官。
8. 设置士兵 zone = Deputy。
9. 写入双方绑定关系。
10. 应用基础数值加成。
11. 刷新 DTO。
12. 写入日志。

### 9.4 事件 hook

副官特性可以复用现有 Trait hook 思路，但 owner 改为宿主英雄。

| 副官 | 需要监听 |
|---|---|
| 牧师 | `OnRoleActionResolved`，检查宿主是否是施法者，是否造成有效治疗或成功净化。 |
| 盾卫 | `OnShieldChanged`、`OnStatusApplied`，检查宿主是否是来源，防止本副官递归触发。 |
| 决斗士 | `OnAttackDeclared`，检查宿主是否主动攻击通用 Debuff 目标。 |
| 秘术士 | `OnDamageResolved`、`OnStatusApplied`，检查宿主是否造成魔法伤害或赋予指定状态。 |

每个副官特性都需要每己方 turn 1 次限制。

可以在 hero turn state 中记录：

```csharp
HashSet<string> UsedDeputyPassiveIdsThisTurn;
```

key 推荐：

```text
{hostHeroInstanceId}:{deputyEffectId}
```

### 9.5 属性计算

副官基础加成进入有效属性计算：

```text
EffectiveAttack = BaseAttack + RewardAttack + DeputyAttack + ...
EffectivePhysicalDefense = BasePhysicalDefense + RewardPhysicalDefense + DeputyPhysicalDefense + ...
EffectiveMagicalDefense = BaseMagicalDefense + RewardMagicalDefense + DeputyMagicalDefense + ...
EffectiveMaxHp = BaseMaxHp + RewardMaxHp + DeputyMaxHp + ...
```

V1 不把 Deputy stat 做成 StatusEffect。

### 9.6 DTO

Hero DTO 增加：

```json
"deputy": {
  "soldierInstanceId": "...",
  "soldierId": "arcanist",
  "name": "秘术士",
  "effectId": "deputy-arcanist",
  "statText": "魔防 +1",
  "passiveText": "每己方 turn 1 次，宿主造成魔法伤害或赋予燃烧 / 空虚 / 力竭 / 磨损后，获得魔涌。"
}
```

Soldier DTO 增加：

```json
"canAssignAsDeputy": true,
"assignDeputyDisabledReason": null,
"deputyPreview": {
  "effectId": "deputy-arcanist",
  "statText": "魔防 +1",
  "passiveText": "..."
}
```

### 9.7 前端

涉及位置：

| 功能 | 位置 |
|---|---|
| 士兵 Inspector 显示任命按钮 | `wwwroot/app.js` |
| 任命目标选择模式 | 复用 Role Action TargetSelect 高亮逻辑 |
| 确认弹窗 | 可以先用简单 modal / confirm |
| 英雄卡副官徽章 | 英雄 card render |
| 英雄 Inspector 副官小节 | Inspector render |
| 本地化 | `wwwroot/locales/zh.json`, `ja.json` |

---

## 10. 平衡参数

### 10.1 初版推荐参数

| 参数 | V1 推荐 |
|---|---|
| 任命 AP 消耗 | 0 AP |
| 任命是否消耗攻击权 | 不消耗 |
| 任命是否给 BP | 不给 |
| 每个英雄副官数 | 1 |
| 每个士兵可任命次数 | 1 |
| 副官基础加成 | 只给 1 个小数值轴 |
| 副官特性触发频率 | 每己方 turn 最多 1 次 |
| 副官产生状态 | 只用通用状态 |
| 副官是否可驱散 | 不可驱散；因为不是 Buff |

### 10.2 防止滥用

V1 建议加两条限制：

1. 士兵本 turn 已攻击或使用 Role Action 后，不能再任命为副官。  
   防止玩家先白嫖士兵行动，再立刻腾 slot。

2. 副官任命释放 slot 后，不额外立即补抽；沿用现有手牌 refill 规则。  
   如果当前系统会立即补位，建议增加“每己方 turn 最多任命 1 名副官”的限制。

---

## 11. 测试用例

### 11.1 基础任命

| 场景 | 预期 |
|---|---|
| Rank1 士兵点击 Inspector | 不显示或灰掉“任命副官”。 |
| Rank2 士兵点击 Inspector | 显示“任命副官”。 |
| 点击任命按钮 | 进入选择英雄模式。 |
| 选择已有副官的英雄 | 不可选。 |
| 选择存活且无副官的英雄 | 弹出确认。 |
| 确认后 | 士兵离开手牌区；英雄获得副官信息。 |

### 11.2 行动限制

| 场景 | 预期 |
|---|---|
| 士兵本 turn 已主动攻击 | 不能任命。 |
| 士兵本 turn 已使用 Role Action | 不能任命。 |
| 任命后的士兵 | 不能攻击，不能使用 Role Action，不能被选为目标。 |

### 11.3 副官特性

| 副官 | 测试 | 预期 |
|---|---|---|
| 牧师 | 公主带牧师副官，使用圣女祈祷治疗目标 | 目标获得护咒。 |
| 盾卫 | 骑士带盾卫副官，使用举盾号令增加共享盾 | 当前 HP 比例最低友军获得坚守。 |
| 决斗士 | 任意英雄带决斗士副官，攻击带燃烧目标 | 攻击前获得强攻。 |
| 秘术士 | 魔法师带秘术士副官，使用灼热刻印赋予燃烧 / 空虚 | 魔法师获得魔涌，并可以再次主动攻击。 |

### 11.4 状态显示

| 场景 | 预期 |
|---|---|
| 英雄带副官 | 卡面显示副官小徽章，但不占 Buff / Debuff icon 位。 |
| 打开英雄 Inspector | 显示副官名称、永久加成、副官特性。 |
| 副官触发产生强攻 / 魔涌 / 坚守 / 护咒 | 这些状态正常进入 Buff 显示组。 |
| 德鲁伊驱散 | 只能驱散副官产生的普通 Buff，不能驱散副官本体。 |

---

## 12. V2 扩展方向

V1 稳定后再考虑：

1. 英雄与副官的推荐相性加成，但不要做 32 条全专属规则。
2. 副官可以跨战斗保存。
3. 副官可以被替换，但旧副官转为退役 / 消耗。
4. 特定奖励可以让一个英雄拥有第二副官。
5. 高稀有士兵拥有更强副官模板。

V1 不建议做这些，先验证“Rank2 士兵转为英雄永久附件”的基础循环是否好玩。

---

## 13. 一句话总结

副官系统 V1 的核心是：

```text
Rank2 士兵可以牺牲自己的行动卡身份，成为一名英雄的永久副官。
副官提供一个小数值加成和一个继承自士兵 Trait 的低频被动。
副官不新增状态体系，不新增 Role Action，不参与驱散净化，只在 Inspector 中作为英雄的被动附件显示。
```
