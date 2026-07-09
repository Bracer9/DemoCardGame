# Tiny Pixel Fights - 士兵系统实装记录

日期：2026-07-04

## 实装范围

本次加入 4 名 `Soldier / 士兵`，并实装 Rank0 到 Rank2 的基础成长、进阶立绘和 Rank2 Role Action。

| ID | Rank0 Trait | Rank1 | Rank2 Role Action |
| --- | --- | --- | --- |
| `cleric` | `field-medic` | HP+2，低血目标护咒更久 | `mend` |
| `shieldmaiden` | `shield-drill` | HP+2，物防+1，强化坚守延长 | `aegis-formation` |
| `duelist` | `duel-sense` | HP+1，首次主动 HP 伤害后追加绝对伤害 | `crimson-lunge` |
| `arcanist` | `arcane-resonance` | HP+2，魔防+1，Role Action 触发时魔涌持续更久 | `astral-focus` |

所有士兵效果复用既有通用状态，不新增士兵专属状态。

## 流程

### 开局

英雄选择结束后，进入士兵选择：

- 复用现有英雄招募/选择 UI。
- 显示 4 名士兵候选。
- 最多选择 2 名士兵。
- 不显示 reset。
- 确认后加入队伍。

测试模式跳过该流程，仍然用于快速测试英雄与 Role Action。

### 奖励阶段

Round 奖励选项中加入 `soldier-recruit`，位置在新英雄/英雄训练之后、临时遗物奖励之前。

- 队伍已满 4 人时，该选项锁定。
- 选择后复用现有招募 UI，显示 4 名士兵。
- 选择未拥有士兵：加入队伍。
- 选择已拥有士兵：可以招募重复士兵；也可以点击强化，再选择场上/手牌中的同名士兵，使该士兵升 1 Rank。
- Rank 最高为 2。
- Rank2 后切换进阶立绘，并解锁对应 Role Action。

英雄招募也同步受队伍上限 4 人限制。

## 主要文件

- `Domain/CharacterDefinition.cs`：新增 4 名士兵定义、`Hero/Soldier` 分类入口、Rank2 立绘字段。
- `Domain/GameState.cs`：新增士兵 Rank、士兵选择 draft 状态。
- `Domain/GameEngine.cs`：开局士兵选择、奖励招募、强化升阶、Rank 属性与 Rank2 Role Action 解锁。
- `Domain/Traits.cs`：新增 4 个士兵 Trait。
- `Domain/RoleActions.cs`：注册 4 个士兵 Rank2 Role Action。
- `Domain/StatusEffects.cs`：补充通用状态 `strong-attack`、`spell-ward`、`vulnerable`。
- `Api/GameDtos.cs` / `Program.cs`：同步 DTO 与本地/在线接口。
- `wwwroot/app.js` / `wwwroot/index.html`：复用英雄选择 UI，加入多选、确认、强化目标选择。
- `wwwroot/locales/zh.json` / `wwwroot/locales/ja.json`：新增双语文本。

## 后续注意

- 士兵 UI 不另起一套，继续复用英雄招募窗口。
- 士兵数值和平衡以 `reference/TinyPixelFights_Soldier_Design_20260704.md` 为基准。
- 若之后加入副官系统，不要把当前 `SoldierRank` 直接等同于副官成长；副官应作为新的关系/槽位系统接入。
