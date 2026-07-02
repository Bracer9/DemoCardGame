# Tiny Pixel Fights — Role Action 第一版实装记录

日期：2026-06-30

## 本次目标

把 Phase 3A 预留的 Role Action 骨架推进到第一版可玩状态：

- Round 3 奖励中固定出现一个泛用英雄职业训练奖励。
- 购买该奖励后，玩家选择一名可升级英雄。
- 在该英雄左侧 HUD 的 Role Action 区域中二选一。
- 被选择的 Role Action 本场战斗内解锁，另一个消失。
- 解锁后，玩家可从左侧 HUD 发动该 Role Action。

当前只实装公主和骑士的初始 Role Action。

## 奖励流程

奖励 ID：

- `hero-role-action-upgrade`

规则：

- 从 Round 3 开始出现奖励窗口。
- Round 3 如果当前玩家有可升级英雄，奖励窗口中固定保留一个 `hero-role-action-upgrade` 槽位。
- 购买后关闭奖励窗口，进入 `PendingRoleActionUpgrade` 状态。
- 待升级状态下不能攻击、展开盾或结束回合。
- 玩家点击己方可升级英雄后，左侧 HUD 显示两个 Role Action 选项。
- 选择其中一个后，写入该角色的 `RoleActionIds`。

## 已实装 Role Action

### Princess

#### `saintly-prayer`

- Input：TargetSelect
- Target：AllyCard
- Cost：1 AP
- Tags：heal, cleanse, holy
- 效果：目标恢复 2 HP，不突破上限；若目标有可净化 debuff，则净化 1 个。
- BP：净化成功时 +1 BP，受单回合 BP 获取上限制约。

#### `royal-command`

- Input：Click
- Target：ActionPointPanel
- Cost：1 AP
- Tags：command, action-point
- 效果：本回合 AP +2；下个己方回合开始 AP -1。
- BP：默认不给 BP。
- 实装备注：debt 是玩家资源债务，不绑在角色身上，避免角色死亡后逃避代价。

### Knight

#### `guard-oath`

- Input：TargetSelect
- Target：AllyCard
- Cost：2 AP
- Tags：guard, defense
- 效果：目标获得 `guarded`，物理防御 +2，持续到下一次受到主动攻击之后。

#### `raise-bulwark`

- Input：TargetSelect
- Target：OwnShield
- Cost：2 AP
- Tags：shield, guard
- 效果：我方拥有共享盾时可发动，共享盾 +4。
- 当前 UI 备注：因为盾暂时不是实体目标，按钮点击后直接发动；后续如果做盾/资源区域可选目标，可以复用 `RoleActionTargetKind.OwnShield`。

## 后端结构

主要文件：

- `Domain/RoleActions.cs`
- `Domain/GameEngine.cs`
- `Domain/GameState.cs`
- `Domain/RewardDefinitions.cs`
- `Api/GameDtos.cs`
- `Program.cs`

关键状态：

- `CharacterState.RoleActionIds`
- `GameState.PendingRoleActionUpgrade`
- `PlayerState.PendingActionPointDebt`

关键 API：

- `POST /api/game/role-action/upgrade`
- `POST /api/game/role-action/use`
- `POST /api/online/game/role-action/upgrade`
- `POST /api/online/game/role-action/use`

## 前端交互

主要文件：

- `wwwroot/app.js`
- `wwwroot/styles.css`
- `wwwroot/locales/ja.json`
- `wwwroot/locales/zh.json`

流程：

1. 购买英雄职业训练奖励。
2. 点击可升级英雄。
3. 左侧 HUD 的 Role Action 区显示两个候选按钮。
4. 选择一个后解锁。
5. 之后点击该英雄，Role Action 区显示已解锁按钮。
6. TargetSelect 类型先进入目标选择状态，再点击友方卡。
7. `royal-command` / `raise-bulwark` 当前直接按钮发动。

## 当前限制

- 尚未做 Role Action preview。
- 尚未做 Role Action 专用动画、音效、语音。
- 尚未做从 Role Action 按钮拖出抛物线到目标。
- 当前不再使用 `StatusIcon` 目标类型。净化/驱散类 Role Action 通过选择角色卡发动，由后端按规则自动选择可净化 debuff 或可驱散 buff。
- 当前只有公主和骑士可通过奖励解锁初始 Role Action。

## 后续扩展方向

- Phase 3B 后续应加入 Role Action preview。
- TargetSelect 应支持点击目标与拖拽抛物线两种输入。
- 普通兵 Rank2 Role Action 可复用同一套 metadata / DTO / endpoint。
- 英雄进阶 Class 解锁第二个 Role Action 时，只需要向 `RoleActionIds` 增加第二个 ID。
- 副官、遗物、天气应通过 modifier 改写 Role Action cost、target、tag 或效果，不应把特例写进前端。
