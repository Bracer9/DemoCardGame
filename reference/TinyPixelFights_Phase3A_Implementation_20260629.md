# Tiny Pixel Fights — Phase 3A 实装记录

日期：2026-06-29

## 目标

Phase 3A 的目标是“不改变当前玩法，只重整 Ability 语义地基”。

这次实装后，运行时代码不再把当前 8 张英雄卡的固有能力称为 Skill。当前这些能力统一归类为 Trait；未来玩家主动点击/拖动发动的职业行动，会作为 Role Action 接入，而不是塞回 Trait 或旧 Skill 里。

## 本次完成内容

### 后端规则层

- `Domain/Skills.cs` 迁移为 `Domain/Traits.cs`。
- `CharacterSkill` 迁移为 `CharacterTrait`。
- `SkillRegistry` 迁移为 `TraitRegistry`。
- `SkillId` 迁移为 `TraitId`。
- `DamageSource.Skill` 迁移为 `DamageSource.Trait`。
- `L10n.Skill(...)` 迁移为 `L10n.Trait(...)`。
- `DealSkillDamage(...)` 迁移为 `DealTraitDamage(...)`。

新增基础 taxonomy：

- `CardType`
  - `Hero`
  - `Soldier`
- `AbilityKind`
  - `Trait`
  - `RoleAction`
- `TraitTriggerKind`
  - `Continuous`
  - `TurnStart`
  - `TurnEnd`
  - `OnAttack`
  - `OnAttackDeclared`
  - `OnAttackResolved`
  - `OnDamaged`
  - `OnShieldBreak`
  - `OnCharacterDefeated`
  - `OnRewardWindow`
  - `ManualCheck`
- `TraitScopeKind`
  - `Self`
  - `Ally`
  - `Enemy`
  - `Team`
  - `EnemyTeam`
  - `Global`
- `TraitEffectKind`
  - `Damage`
  - `Heal`
  - `Shield`
  - `Status`
  - `Dispel`
  - `DamageModifier`
  - `DefenseModifier`
  - `CostModifier`
  - `ActionPointModifier`
  - `BattlePointModifier`
  - `RewardModifier`
  - `TargetingRule`
- `RoleActionActivationMode`
  - `Immediate`
  - `Targeted`
- `RoleActionTargetKind`
  - `None`
  - `SelfCard`
  - `AllyCard`
  - `EnemyCard`
  - `OwnShield`
  - `EnemyShield`
  - `ActionPointPanel`
  - `BattlePointMedal`
  - `StatusIcon`
  - `EmptySlot`

当前 8 张卡全部标记为 `CardType.Hero`。普通兵 `Soldier` 只建立概念入口，本次不新增普通兵卡。

### DTO / API

`CharacterView` 现在输出：

- `cardType`
- `traits`
- `roleActions`

其中：

- `traits` 当前包含角色固有 Trait。
- `roleActions` 当前为空数组，作为未来 Role Action UI 的稳定接口。

`TraitView` 输出：

- `id`
- `triggerKind`
- `scopeKind`
- `effectKind`
- `isReady`
- `unavailableReason`

`RoleActionView` 已预留：

- `id`
- `slotIndex`
- `activationMode`
- `validTargetKinds`
- `cost`
- `enabled`
- `disabledReason`

本次不实装任何 Role Action 行为。

### 前端 UI / 本地化

- `card.skill` 迁移为 `card.traits`。
- 卡面能力区、左侧 inspector、攻击预测框都改为 Trait 语义。
- `preview-skill` / `skill-panel` / `inspector-skill` 等运行时 DOM/CSS 命名迁移为 `preview-trait` / `trait-panel` / `inspector-trait`。
- `wwwroot/locales/*.json` 中：
  - `skills` 迁移为 `traits`
  - `skillKinds` 移除
  - 新增 `abilityKinds`
  - 新增 `traitTriggers`
  - `preview.skill.*` 迁移为 `preview.trait.*`
  - `log.skillFailed` 迁移为 `log.traitFailed`
- UI 文本仍然走 locale，不进入 C# 规则层。

### 资源与音频

为了让运行时资源 key 与新语义一致，同时不删除旧素材，本次复制了新的 trait 命名资源：

- `assets/ui/events/icon_event_trait.png`
- `assets/ui/traits/trait_*.png`
- `assets/audio/trait-damage.mp3`

运行时 manifest 现在使用：

- `event.trait`
- `trait.saints-prayer`
- `trait.stargazers-aegis`
- `trait.spring-harvest`
- `trait.searing-mark`
- `trait.weakening-spores`
- `trait.aftershock-axe`
- `trait.predatory-instinct`
- `trait.interposing-shield`
- `combat.trait-damage`

旧重复资源已在后续清理中删除；运行时只使用 `trait-damage.mp3` 与 `trait_*` 图标资源。

### 测试

本次同步更新测试用例：

- 本地化测试改为检查 `traits` 与 `L10n.Trait(...)`。
- UI 资源测试改为检查 `event.trait`。
- 音频测试改为检查 `trait.*` 事件。
- 弱化/驱散规则测试改为读取 `Domain/Traits.cs`。

通过验证：

```powershell
node --check wwwroot/app.js
node --check wwwroot/i18n.js
node --check wwwroot/voice.js
dotnet build
node --test tests\*.test.js
```

结果：

- `dotnet build`：0 warnings / 0 errors
- `node --test tests\*.test.js`：23 pass / 0 fail

## 非目标

本次没有做：

- 新增 Role Action 的实际按钮 UI。
- 新增 Role Action 的结算逻辑。
- 多 Trait 槽位或 Trait 解锁/锁定。
- 普通兵卡数据。
- 旧历史文档的大规模清理。
- 删除旧 `skill_*` 素材文件。

## 后续接入建议

### Role Action

未来 Role Action 应从 `roleActions` DTO 接入 UI。建议角色 select 后左侧 HUD 分三段：

1. 基础状态
2. Trait 列表
3. Role Action 按钮

Role Action 分两种：

- `Immediate`：点击按钮后立即发动。
- `Targeted`：点击或拖动按钮后选择目标，目标类型由 `validTargetKinds` 决定。

不要把 Role Action 塞进 Trait hook。

### Trait 扩展

当前仍是一名角色一个固有 Trait。未来如果需要：

- 英雄升级解锁新 Trait
- 普通兵拥有简单 Trait
- 遗物临时赋予 Trait
- 天气/战场提供 Global Trait

建议把 `CharacterDefinition.TraitId` 升级为 Trait 槽位或来源列表，而不是在 `GameEngine` 中写角色特判。

### 文档清理

历史文档中仍可能出现 Skill 作为旧讨论词。后续如果要整理文档，建议优先处理：

- `reference/TinyPixelFights_Skills.md`：迁移为当前规则入口 `TinyPixelFights_Traits.md`。
- `reference/TinyPixelFights_Localization.md`：把新增角色/能力的说明从 `skills` 改为 `traits`。
- `reference/TinyPixelFights_UI_Asset_Embedding.md`：把 C 类资源从 `skills/skill_*` 改为 `traits/trait_*`。
- `reference/TinyPixelFights_Battle_Voice_System.md`：把未来 `ultra-skill` 统一改为 `ultra-action` 或具体 Role Action 语义。

## 设计结论

Phase 3A 完成后，当前项目的能力语义变为：

- Trait：角色固有、持续、触发、响应、光环或修正类能力。
- Role Action：未来玩家通过按钮主动发动的职业行动。
- Status / Modifier：局内可变化、可驱散或不可驱散的状态和修正。

这给后续英雄成长、普通兵、副官、遗物、天气、战场规则和职业行动留出了比旧 Skill 更清晰的接入位置。

## 2026-06-29 Role Action 骨架补充

本次在不改变当前玩法的前提下，把 Role Action 从“只存在于 DTO 空数组”推进为可查询、可显示的骨架：

- 新增 `Domain/RoleActions.cs`，提供 `RoleActionMetadata`、`CharacterRoleAction` 与 `RoleActionRegistry`。
- `CharacterDefinition` 预留 `RoleActionIds`，`CharacterState` 持有局内 Role Action ID 列表。
- `GameEngine` 通过 `GetRoleActions(...)` 输出角色当前拥有的 Role Action。
- `CharacterView.roleActions` 现在会输出真实列表、slot、activation mode、target kinds、cost、enabled 与 disabled reason。
- 当前 8 名英雄默认仍没有解锁 Role Action，因此玩法不变。
- 左侧角色 HUD 新增第三段 `Role Action` 区域；没有解锁时显示未解锁提示。
- 新增 `roleActions.{id}.name / description / button` 本地化入口，C# 仍不承载中日文显示文本。

这一步仍不包含 Role Action 的点击、目标选择、preview、结算、日志、动画、音效或 BP 触发。那些属于 Phase 3B pipeline。
