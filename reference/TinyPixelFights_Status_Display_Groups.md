# Tiny Pixel Fights 状态显示组规则

更新时间：2026-07-04

本文只记录当前实装需要遵守的显示边界。完整状态词库与数值统一方案见：

- `reference/TinyPixelFights_Trait_RoleAction_Status_Unification_Draft.md`
- `reference/TinyPixelFights_Role_Action_Table.md`

## 1. 当前结论

状态显示不再按“来源”展开。

通用状态已经被统一成明确关键词，例如：

| 类型 | 状态 |
| --- | --- |
| Buff | 强攻、猛击、魔涌、咏唱、坚守、护咒 |
| Debuff | 脆弱、空虚、力竭、磨损、燃烧、战栗 |

这些状态本身就是玩家要理解的对象。无论来源是 Trait、Role Action、奖励、天气还是遗物，UI 都只显示同一个状态名、层数 / 次数 / 持续时间。

例：

- 骑士“守护誓约”赋予 `坚守`。
- 农民“补给篮”也赋予 `坚守`。
- 角色右侧和 inspector 只显示 `坚守`，不展开“来自骑士 / 来自农民”。

这样可以避免卡面右侧和 inspector 被来源名挤爆。

## 2. 只有 Aura 需要折叠展开

Aura 指“来源角色只要活着就无条件持续生效”的全队效果。

当前 Aura：

| Aura Status ID | 来源 | 显示规则 |
| --- | --- | --- |
| `blessing` | 公主 Trait | 不在卡面右侧单独显示，inspector 中折叠进 Aura |
| `foresight` | 占卜师 Trait | 不在卡面右侧单独显示，inspector 中折叠进 Aura |
| `magic-power` | 占卜师 Trait | 规则层可保留真实 status，但显示层折叠进 Aura |
| `guard` | 骑士 Trait | 不在卡面右侧单独显示，inspector 中折叠进 Aura |

Aura 不是普通 Buff：

- 不应被驱散。
- 不应占用每张卡右侧的 buff/debuff icon 位。
- 只在 inspector 的 Aura 条目中占一行。
- hover Aura 条目时，再显示二级 HUD，列出具体 Aura 效果。

## 3. 普通状态显示规则

角色卡右侧显示：

- 通用 Buff / Debuff
- 少数特殊状态，例如 `harvest`、`pact`、`prey`、`beast-rage`
- 不显示 Aura
- 不显示共享盾；共享盾属于战场单位 / 队伍资源

Inspector 右侧显示：

1. 普通 Buff / Debuff 列表
2. 特殊状态
3. 最下面单独一条 Aura 折叠项

通用状态不再显示来源详情。

默认可驱散 / 可净化不写。只有不可驱散或不可净化才写。

## 4. 驱散与净化

显示规则不改变后端规则。

- 驱散一次 = 移除 1 个可驱散 Buff status instance。
- 净化一次 = 移除 1 个可净化 Debuff status instance。
- Aura 不属于可驱散 Buff。
- 默认状态可驱散；不可驱散才显式 override。

## 5. 新机制加入时的判断

新增 Trait / Role Action / 遗物 / 天气时，优先判断：

1. 能否复用现有通用状态？
2. 如果只是来源不同，不新增状态名。
3. 如果是角色存活即生效的全队规则，考虑是否归入 Aura。
4. 只有当玩家必须单独识别它，且无法用现有状态表达时，才新增特殊状态。

新增特殊状态前必须能回答：

- 它是否会长期影响多个决策？
- 它是否是某个角色 / 职业的核心身份？
- 它是否无法被现有通用状态表达？

否则不要新增。
