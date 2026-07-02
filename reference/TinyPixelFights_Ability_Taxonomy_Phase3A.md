# Tiny Pixel Fights — Phase 3A Ability Taxonomy Scaffold

> 日期：2026-06-29  
> 状态：已实装 / 见 `reference/TinyPixelFights_Phase3A_Implementation_20260629.md`  
> 目标：不改变玩法，只重整卡牌类型、能力结构和行动语义；为之后 Role Action、英雄升级、普通兵、副官、遗物和天气系统打地基。
> 参考：`reference/TinyPixelFights_Action_Ability_Brainwriting_20260628.md`

---

## 1. 这次重构要解决什么

当前项目把很多不同规则都叫 `Skill`：

- 攻击时触发的效果。
- 回合开始或持续生效的被动。
- 受击、阵亡、破盾等反应效果。
- 队伍光环。
- 攻击、防御、Cost、AP、BP、奖励等 modifier。

这在 8 张英雄卡阶段还能维持，但进入英雄成长、普通兵、副官、遗物、天气后会迅速混乱。尤其是当前 `Active Skill` 实际大多不是玩家主动按按钮发动，而是普通攻击附带触发；继续沿用 `SkillKind.Active/Passive` 会误导后续设计和代码。

Phase 3A 的核心是：

```text
Skill 退场。
Ability 成为规则模块总称。
Ability 顶层只有 RoleAction 与 Trait。
Trait 内部再用触发时点、作用范围和效果类型描述。
Action 是玩家主动选择的操作。
```

这一步不新增玩法，只改变语义和数据结构。

---

## 2. Scope

### 做

- 新增 `CardType`：`Hero / Soldier`。
- 新增顶层 `AbilityKind`：`RoleAction / Trait`。
- 新增 Trait 后端描述字段：
  - `TraitTriggerKind`
  - `TraitScopeKind`
  - `TraitEffectKind`
- 新增 Role Action 的发动模式：
  - `Immediate`
  - `Targeted`
- 当前 8 名英雄全部标记为 `Hero`。
- 当前角色定义中的旧能力 ID 迁移为 `TraitId / InnateTraitIds`。
- 当前 8 个旧能力实现全部重命名并迁移成 `CharacterTrait`，而不是 Role Action。
- 为英雄的 Role Action 槽位预留结构：基础/初级阶段最多 1 个，进阶 Class 后最多 2 个。
- 为 Role Action 预留两种输入：直接点击发动、点击按钮后选择目标。目标选择可以点击合法目标，也可以拖出抛物线到目标。
- 后端、DTO、前端本地化和新文档中废除 `skill` 命名；Phase 3A 完成后活跃代码不再保留 `CharacterSkill / SkillRegistry / SkillId / card.skill / locales.skills`。
- UI 语义改成左侧 HUD 三块：基础状态 / Trait / Role Action。

### 不做

- 不新增真正的 Role Action 行为。
- 不新增普通兵。
- 不新增英雄升级。
- 不改战斗数值。
- 不改当前 8 个英雄实际效果。
- 不改当前攻击预测结果。
- 不改在线同步流程。

Phase 3A 是 taxonomy scaffold，不是玩法 phase。

---

## 3. 新术语

### CardType

```csharp
public enum CardType
{
    Hero,
    Soldier
}
```

当前 8 张传奇角色卡都是 `Hero`。  
未来 Duelist / Shieldmaiden / Arcanist / Cleric 等普通兵是 `Soldier`。

### Ability

`Ability` 是角色拥有的规则模块总称。它不是“主动技能”的同义词。

顶层只分两类：

```csharp
public enum AbilityKind
{
    RoleAction,
    Trait
}
```

### RoleAction

玩家主动按按钮发动的职业行动。  
它不是普通攻击，也不等于旧的 Active Skill。

未来角色 select 后，左侧 HUD 第三区域最多显示 2 个 Role Action 按钮。玩家视角就是：

```text
Role Action 1
Role Action 2
```

上限为 2。  
8 个英雄升级到进阶 Class 时，可以解锁第二个 Role Action。

这个决定覆盖 brainwriting 旧稿里“每张卡最多 1 个 Role Action”的保守假设。当前项目方向已经进入英雄成长和进阶 Class，因此英雄卡需要第二个按钮作为成长空间；但上限仍必须是 2，避免把 UI 变成按钮面板。

建议槽位语义：

| 槽位 | 解锁阶段 | 用途 |
|---|---|---|
| RoleActionSlot 1 | 基础/初级成长 | 表达角色职业身份的核心行动 |
| RoleActionSlot 2 | 进阶 Class | 表达进阶分支带来的新行动或质变行动 |

普通兵原则上只使用 1 个 Role Action，副官不直接增加新按钮，而是修改英雄现有 Attack / Role Action / Trait。

### Trait

Trait 是不需要玩家直接按按钮发动的规则特性。  
UI 层统一显示为 `Trait / 特性`，后端再细分：

```text
Trait
├─ TriggerKind：什么时候触发
├─ ScopeKind：影响谁
└─ EffectKind：做什么
```

过去的攻击触发、被动、反应、光环、modifier 都归入 Trait，而不是和 RoleAction 平级。

---

## 4. Trait 后端分类

### TraitTriggerKind

```csharp
public enum TraitTriggerKind
{
    Continuous,
    TurnStart,
    TurnEnd,
    OnAttack,
    OnAttackDeclared,
    OnAttackResolved,
    OnDamaged,
    OnShieldBreak,
    OnCharacterDefeated,
    OnRewardWindow,
    ManualCheck
}
```

说明：

- `OnAttack` / `OnAttackResolved` 覆盖旧“主动技能随攻击触发”。
- `Continuous` 覆盖光环和常驻 modifier。
- `OnDamaged`、`OnShieldBreak`、`OnCharacterDefeated` 覆盖反应型 Trait。
- `ManualCheck` 用于某些由系统主动查询的 modifier，不直接对应事件。

### TraitScopeKind

```csharp
public enum TraitScopeKind
{
    Self,
    Ally,
    Enemy,
    Team,
    EnemyTeam,
    Global
}
```

### TraitEffectKind

```csharp
public enum TraitEffectKind
{
    Damage,
    Heal,
    Shield,
    Status,
    Dispel,
    DamageModifier,
    DefenseModifier,
    CostModifier,
    ActionPointModifier,
    BattlePointModifier,
    RewardModifier,
    TargetingRule
}
```

这些分类是为了查询、预测、日志、UI 标签和未来奖励筛选，不要求第一版把每个 effect 都做成数据驱动脚本。

---

## 5. Role Action 发动方式

Role Action 在 UI 上都是按钮，但按钮按下后的流程有两种。

### 5.1 Immediate

点击按钮后直接进入 preview / confirm 或直接执行。

适合：

- 自身强化。
- 全队鼓舞。
- 本回合 AP 变化。
- 自我蓄力。

例：

```text
魔法使い：魔力蓄积
点击按钮 → preview → confirm → 自己获得 Charged
```

### 5.2 Targeted / TargetSelect

点击按钮后进入目标选择状态。玩家可以直接点击合法目标，也可以从按钮拖出抛物线箭头到目标。  
这个未来会做，Phase 3A 只预留接口。

适合：

- 单体治疗。
- 单体 buff。
- 单体 debuff。
- 破盾。
- 对敌方角色造成特殊伤害。
- 选择 AP / BP / 状态图标 / 盾等 UI 目标。

例：

```text
公主：圣女祈祷
点击 Role Action 按钮
→ 合法友方卡高亮
→ 点击目标或拖出抛物线到目标
→ preview / confirm
→ 治疗目标
```

目标选择的 UI 约定：

- Role Action 按钮本身是起点。
- `Targeted` 类型按下后，合法目标高亮。
- 玩家可以点击目标，也可以从按钮拖出抛物线光线到目标。
- 两者只是输入方式不同，不应影响后端规则、预测、日志或动画结算。
- 拖拽只是输入表现，不应产生另一套后端逻辑。
- 最终都提交同一种 `RoleActionRequest`。

### 5.3 RoleActionTargetKind

```csharp
public enum RoleActionTargetKind
{
    None,
    SelfCard,
    AllyCard,
    EnemyCard,
    OwnShield,
    EnemyShield,
    ActionPointPanel,
    BattlePointMedal,
    StatusIcon,
    EmptySlot
}
```

注意：共有盾在普通攻击中不作为可选目标；但 Role Action 可以选择 `OwnShield / EnemyShield` 作为效果对象，例如强化盾或破盾。

---

## 6. 左侧 HUD 目标结构

角色 select 后，左侧 HUD 未来固定三块。

### 6.1 基础状态区

沿用当前上方 stats：

- ATK
- HP
- COST
- TYPE
- 物防
- 魔防

### 6.2 Trait 区

显示当前角色拥有的 Trait。

UI 层统一叫：

```text
Trait / 特性
```

不把 Attack Trigger / Passive / Reactive 在 UI 主结构中拆成不同大类。  
如果需要解释触发方式，在 Trait 详情中用短标签或描述说明。

### 6.3 Role Action 区

显示 0～2 个 Role Action 按钮。

- 未解锁时可以显示锁定占位，也可以不显示。
- 解锁 1 个时显示一个按钮。
- 进阶 Class 后可解锁第二个按钮。
- 按钮需要显示 cost、名称、是否可用。
- 不可用时显示原因，例如 AP 不足、角色已行动、目标不存在。
- `Immediate` 类型按钮点击后直接 preview / confirm 或执行。
- `Targeted` 类型按钮点击后进入目标选择，可点击目标或从按钮拖拽抛物线到目标。

这一区域现在先设计结构，不要求 Phase 3A 立刻显示真实 Role Action。当前 8 名英雄旧能力全部是 Trait，因此 Role Action 区可以隐藏，或者显示未来占位。

---

## 7. 当前 8 名英雄映射

Phase 3A 不改变效果，只改语义。

| 角色 | 旧 ID | 新 AbilityKind | TraitTriggerKind | TraitScopeKind | TraitEffectKind |
|---|---|---|---|---|---|
| Princess | `saints-prayer` | Trait | TurnStart / Continuous | Team | Heal |
| Oracle | `stargazers-aegis` | Trait | Continuous | Team | DamageModifier / DefenseModifier |
| Peasant | `spring-harvest` | Trait | OnAttack / TurnStart | Self | Status / DamageModifier |
| Mage | `searing-mark` | Trait | OnAttackResolved | Enemy | Status / Damage |
| Druid | `weakening-spores` | Trait | OnAttackResolved | Enemy | Dispel / Status / DamageModifier |
| Barbarian | `aftershock-axe` | Trait | OnAttackResolved | EnemyTeam | Damage |
| Monster | `predatory-instinct` | Trait | OnAttackResolved | Enemy | Damage |
| Monster | `beast-rage-trigger` | Trait | OnCharacterDefeated | Self | DamageModifier |
| Knight | `interposing-shield` | Trait | OnDamaged | Ally | Shield / DamageModifier |

重要结论：

```text
当前 8 个旧能力没有真正的 Role Action。
```

因此 Phase 3A 完成后，当前 8 名角色的 Role Action 区可以为空或显示“未解锁”。真正的 Role Action 从 Phase 3B 起接入。

---

## 8. 后端结构建议

不要过度工程化，不要现在就做完整脚本化能力系统。  
但 class 边界要从现在开始摆正。

核心原则：

```text
Ability metadata 负责“它是什么”。
现有 C# 具体类负责“它怎么结算”。
RoleAction request 负责“玩家选择了什么”。
Trait trigger context 负责“系统什么时候自动触发”。
```

也就是说，这阶段不需要把所有能力做成 JSON 脚本；但需要让后端能稳定查询 Trait 的 trigger、scope、effect，以及未来 Role Action 的目标类型和按钮槽位。

重要决定：

```text
不保留 CharacterSkill 作为兼容层。
当前所有“旧技能”本质都是 Trait，因此直接迁移为 CharacterTrait。
Ability 是分类概念，不一定需要立刻成为所有规则类的共同抽象基类。
```

### 8.1 Metadata

```csharp
public sealed record AbilityMetadata(
    string Id,
    AbilityKind Kind,
    IReadOnlySet<string> Tags);

public sealed record TraitMetadata(
    string Id,
    TraitTriggerKind TriggerKind,
    TraitScopeKind ScopeKind,
    TraitEffectKind EffectKind,
    IReadOnlySet<string> Tags);

public sealed record RoleActionMetadata(
    string Id,
    RoleActionActivationMode ActivationMode,
    IReadOnlySet<RoleActionTargetKind> ValidTargetKinds,
    int BaseApCost,
    bool EndsCharacterAction,
    int SlotIndex);

public enum RoleActionActivationMode
{
    Immediate,
    Targeted
}
```

`AbilityMetadata` 可作为查询/DTO 的轻量总称。  
当前真正承载规则 hook 的类应是 `CharacterTrait`，不是 `CharacterAbility`。

`SlotIndex` 建议只允许 0 或 1。  
如果未来某个进阶 Class 是“替换第一个 Role Action”而不是“新增第二个”，也通过同一套槽位表达。

Role Action 的 AP cost 这里先保留 `BaseApCost`，但不要把它视为最终 AP cost。它可以是 0，0 AP Role Action 仍可通过消耗角色行动、次数限制、状态条件、debt 或 HP / shield 代价形成约束。未来应走 modifier 管线：

```text
final role action AP cost
= base role action AP cost
+ character status modifier
+ class modifier
+ relic modifier
+ weather / battlefield modifier
+ temporary rule modifier
```

这和角色攻击 Cost 的未来方向一致。当前不设计 BP 作为 Role Action 的支付成本；BP 属于奖励购买/成长经济，也可以作为行动结果被少量奖励，但不进入 Role Action 按钮的支付校验。

### 8.2 类结构

Phase 3A 推荐直接迁移命名，不保留旧 Skill 体系：

```text
Skills.cs               -> Traits.cs
CharacterSkill          -> CharacterTrait
SkillRegistry           -> TraitRegistry
SkillKind               -> 移除，改由 TraitMetadata 描述
SkillId                 -> TraitId / InnateTraitIds
```

现有 hook 结构适合 Trait，不适合 Role Action。  
因此应该把当前抽象类改成：

```csharp
public abstract class CharacterTrait
{
    public abstract TraitMetadata Metadata { get; }

    public virtual void OnTurnStart(...) { }
    public virtual void OnAttackDeclared(...) { }
    public virtual void ModifyOutgoingDamage(...) { }
    public virtual void ModifyIncomingDamage(...) { }
    public virtual void OnAfterExchange(...) { }
    public virtual void OnCharacterDefeated(...) { }
}
```

Role Action 不应该塞进 `CharacterTrait`。  
Phase 3B 再新增独立的 Role Action 定义/接口：

```csharp
public interface IRoleActionDefinition
{
    RoleActionMetadata RoleAction { get; }
    RoleActionPreview Preview(RoleActionContext context);
    RoleActionResult Execute(RoleActionContext context);
}
```

这样边界更干净：

```text
CharacterTrait = 自动触发/持续/反应规则。
RoleActionDefinition = 玩家点击按钮主动发出的职业行动。
Ability = Trait 与 RoleAction 的共同分类语言。
```

这次重构的风险控制不靠保留旧命名，而靠“不改玩法、不改 hook 逻辑、不改数值”。

### 8.3 CharacterDefinition

当前旧结构：

```text
SkillId
```

目标：

```text
CardType
InnateTraitIds
UnlockedRoleActionIds
TraitIds
```

Phase 3A 最小口径：

```text
CardType = Hero
InnateTraitIds = [旧能力 ID 对应的 Trait]
```

后续成长系统再把 `UnlockedRoleActionIds`、`SelectedBasicAbilityPackageId`、`AdvancedClassId` 接进 `CharacterState`。

### 8.4 CharacterState

Phase 3A 可先不新增成长字段。  
但文档上预留：

```text
SelectedBasicAbilityPackageId
AdvancedClassId
UnlockedRoleActionIds   // 英雄上限 2，普通兵原则上上限 1
TraitIds
```

建议不要只保存 `RoleActionId` 单值。  
从现在开始就按 list/slots 设计，否则进阶 Class 解锁第二个按钮时又要改 CharacterState、DTO、UI 和存档。

可以采用：

```text
RoleActionSlots[0]
RoleActionSlots[1]
```

或：

```text
UnlockedRoleActionIds ordered list, max 2
```

当前阶段更推荐 ordered list，简单、够用、不会过度设计。真正需要替换/锁定槽位时，再加 slot object。

---

## 9. Action 接口预留

Phase 3A 不实装 Role Action，但接口方向要确定。

未来需要：

```text
PreviewRoleAction(actorId, roleActionId, targetRef?)
ExecuteRoleAction(actorId, roleActionId, targetRef?)
```

TargetRef 可以先设计为：

```csharp
public sealed record ActionTargetRef(
    RoleActionTargetKind Kind,
    Guid? PlayerId,
    Guid? CharacterId,
    string? StatusId);
```

目标选择归 UI，合法性归后端。  
前端可以通过点击按钮进入 targeting mode；如果是 Targeted 类型，也可以从按钮拖出抛物线箭头。无论点击还是拖拽，发给后端的都是同一个 `targetRef`。

建议从 Phase 3B 开始让玩家操作统一成 Action request，而不是在 controller 里继续散落多个专用入口：

```csharp
public enum ActionKind
{
    Attack,
    RoleAction,
    DeployShield,
    RewardSelect,
    RewardReset,
    RewardSkip,
    EndTurn
}
```

Role Action request 可以长这样：

```csharp
public sealed record RoleActionRequest(
    Guid ActorId,
    string RoleActionId,
    ActionTargetRef? Target);
```

这不是要求 Phase 3A 立刻重构所有 action controller，而是给后续拓展留同一条路：Attack、RoleAction、Reward、Shield 都是玩家 action；Trait 是系统自动反应。

---

## 10. DTO / 前端迁移方向

当前前端可能读取：

```text
card.skill
```

目标：

```text
card.traits[]
card.roleActions[]
```

Phase 3A 不保留 `card.skill` 兼容层。  
当前 8 名英雄只有 Trait，因此 DTO 应直接输出：

```json
"traits": [
  {
    "id": "searing-mark",
    "triggerKind": "OnAttackResolved",
    "scopeKind": "Enemy",
    "effectKind": "Status"
  }
],
"roleActions": []
```

UI 只消费 `traits[]` 与 `roleActions[]`。  
如果某处仍依赖 `card.skill`，应在这次重构中一并改掉，而不是留下 fallback。

Role Action DTO 需要从一开始就能支持 0～2 个按钮：

```json
"roleActions": [
  {
    "id": "royal-command",
    "slotIndex": 0,
    "activationMode": "Targeted",
    "validTargetKinds": ["AllyCard", "ActionPointPanel"],
    "cost": 1,
    "enabled": true,
    "disabledReason": null
  }
]
```

按钮显示文本仍然全部来自 locale。DTO 不返回中文/日文描述。

---

## 11. 本地化方向

当前：

```json
"skills": {
  "searing-mark": {
    "name": "...",
    "description": "...",
    "card": "..."
  }
}
```

目标：

```json
"traits": {
  "searing-mark": {
    "name": "...",
    "description": "...",
    "card": "..."
  }
}
```

新增：

```json
"abilityKinds": {
  "Trait": "特性",
  "RoleAction": "职业行动"
},
"traitLabels": {
  "title": "特性"
},
"roleActionLabels": {
  "title": "职业行动"
}
```

未来 Role Action 另开：

```json
"roleActions": {
  "royal-command": {
    "name": "...",
    "description": "...",
    "button": "..."
  }
}
```

Phase 3A 完成后，活跃 locale 不再使用 `skills` 作为当前规则文本入口。  
如果旧文档或历史资料里还有 `skills`，只作为历史记录，不作为运行时来源。

如果需要 hover 里显示更细标签：

```json
"traitTriggers": {
  "OnAttackResolved": "攻击触发",
  "TurnStart": "回合开始",
  "Continuous": "常驻",
  "OnDamaged": "反应"
}
```

但卡面主 UI 不拆 Trait 子类。

---

## 12. 文档迁移 Scope

Phase 3A 需要同步：

- `GDD.md`：把“技能”改成“能力 / Trait / Role Action”的项目术语。
- `reference/TinyPixelFights_Skills.md`：迁移为 `TinyPixelFights_Traits.md` 或明确标记为历史文档，不再作为当前规则入口。
- `reference/TinyPixelFights_Project_Context.md`：更新当前架构。
- `reference/TinyPixelFights_PvP_GrowthPrototype_Plan_20260628.md`：标记 Phase 3A 进入执行。
- `reference/TinyPixelFights_UI_Tuning_Guide.md`：如果左侧 HUD 区块命名变化。

历史 brainwriting、课程原始资料、playtest 记录不需要清理旧词。

---

## 13. 不变性要求

Phase 3A 完成后，以下必须不变：

- 当前 8 名英雄实际效果。
- 攻击预测结果。
- 实际伤害结果。
- BP 获取与奖励窗口。
- 共有盾、物防、魔防、骑士、怪兽等现有机制。
- 在线同步。
- 音效与语音触发。

这次重构应该是：

```text
same gameplay
cleaner taxonomy
future-proof action/ability boundary
```

---

## 14. 实装步骤建议

### Step 1：新增类型

- `CardType`
- `AbilityKind`
- `TraitTriggerKind`
- `TraitScopeKind`
- `TraitEffectKind`
- `RoleActionActivationMode`
- `RoleActionTargetKind`

### Step 2：新增 metadata

- `AbilityMetadata`
- `TraitMetadata`
- `RoleActionMetadata`

### Step 3：角色定义加入 CardType 与 ability ids

- 当前 8 人全部 `Hero`。
- 旧能力 ID 迁移为 `InnateTraitIds`。

### Step 4：注册表迁移

- `SkillRegistry` 重命名/迁移为 `TraitRegistry`。
- 当前 8 个能力类重命名为 `CharacterTrait` 派生类。
- 不保留 `SkillRegistry` 包装层。
- `AbilityRegistry` 暂时不是必需品；如果新增，只作为查询聚合，不作为旧 Skill 的新壳。

### Step 5：DTO / UI 语义迁移

- DTO 输出 `abilities` 或 `traits/roleActions`。
- 前端左侧 HUD 结构改为基础状态 / Trait / Role Action。
- 当前没有 Role Action 时，第三区可以为空或隐藏。
- Role Action UI 先按 2 个槽位布局，但没有真实数据时不显示按钮。
- 给 `Targeted` Role Action 预留 targeting mode，不在 Phase 3A 接实际技能。

### Step 6：本地化迁移

- `skills` → `traits`。
- 未来新增 `roleActions`。
- 新增 ability kind 文本。
- 不保留运行时 `skills` fallback。

### Step 7：文档同步

- 更新 GDD 和相关 reference。
- 明确真正 Role Action pipeline 属于 Phase 3B。

---

## 15. 风险与防线

### 风险：过度工程化

不要现在做完整脚本化能力系统。  
只要让后端能区分 RoleAction 与 Trait，并让 Trait 拥有触发/范围/效果 metadata 就够。

### 风险：改名过大导致功能破坏

这次不通过保留旧命名来降低风险。  
风险控制方式改为：

- 保持现有 hook 方法和调用时机不变。
- 保持现有 8 个效果实现不变。
- 保持当前预测、日志、动画、音效触发不变。
- 只改类名、字段名、DTO 名和 locale 入口。
- 用 `rg "Skill|skill|skills"` 做残留检查。

也就是说：

```text
玩法不动。
语义换轨。
旧命名清零。
```

### 风险：Role Action 按钮太多

上限为 2：

- 基础/初级阶段最多 1 个。
- 进阶 Class 可解锁第 2 个。
- 普通兵原则上最多 1 个，除非后期特殊设计。
- 副官不直接增加按钮，只修改已有行动。

### 风险：目标选择 UI 复杂

Phase 3B 时统一用：

```text
button -> targeting mode -> targetRef -> preview/execute
```

点击目标和从按钮拖出抛物线只是输入方式不同，后端 action request 一致。

特别注意：Role Action 从按钮拖出抛物线，不等于把角色卡拖出去。  
这能在玩家心智上区分：

```text
拖角色卡 = Attack
拖 Role Action 按钮 = 职业行动
```

以后这两条线可以共用视觉组件，但不能共用语义。

---

## 16. Phase 3A 完成判定

完成后应满足：

- 代码层不再新增 `Skill` 命名。
- 活跃代码层不保留 `CharacterSkill / SkillRegistry / SkillId`。
- DTO 不再输出 `card.skill`。
- locale 运行时入口不再使用 `skills`。
- 后端核心类型出现 `CardType` 与顶层 `AbilityKind.RoleAction/Trait`。
- Trait 有 trigger/scope/effect metadata。
- Role Action 有 activation mode、target kind、cost、是否消耗行动等 metadata 预留。
- Role Action 槽位上限明确：英雄最多 2，进阶 Class 可解锁第 2 个。
- 当前 8 名英雄全部 `CardType=Hero`。
- 当前 8 名英雄旧能力全部映射为 Trait。
- UI 不再显示 `SKILL / Active / Passive` 旧口径。
- 左侧 HUD 文档口径为：基础状态 / Trait / Role Action。
- 游戏玩法、数值、预测、在线同步不变。
- `node --check wwwroot/app.js` 通过。
- locale JSON parse 通过。
- `dotnet build -p:UseAppHost=false -o $env:TEMP\TinyPixelFightsVerify` 通过。

---

## 17. 后续 Phase 关系

```text
Phase 3A：Ability taxonomy scaffold
Phase 3B：Role Action pipeline
Phase 3C：Ability package / unlock / upgrade
Phase 3D：普通兵
Phase 3E：开局 draft / 队伍构筑
```

Phase 3A 如果做干净，后面 Role Action、升级、普通兵、副官、遗物和天气都能接到同一套语义里；如果继续保留 Skill 作为核心词，后面每个系统都会长成特例。

---

## 18. 一句话结论

这次不是简单把 `Skill` 改名成 `Ability`。  
真正目标是确立：

```text
CardType 决定卡牌身份。
AbilityKind 只区分 RoleAction 与 Trait。
Trait 用 trigger/scope/effect 描述自动规则。
RoleAction 是 select 后左侧 HUD 里的职业行动按钮，上限 2。
Action 是玩家实际发出的操作请求。
```

这样才不会在英雄升级、普通兵、副官、遗物和天气系统进入时继续拆东墙补西墙。
