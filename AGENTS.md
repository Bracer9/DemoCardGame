# Tiny Pixel Fights — Codex Project Instructions

这个项目会频繁增加、删除和调整机制。处理任何新机制、技能、状态、UI 文本或资源时，必须优先考虑延展性，而不是只修当前一例。

## 架构原则

- 不要为单个技能新增过窄的专用属性或专用流程。优先设计通用概念，例如 `IsDispellable`，而不是 `IsDruidDispelTarget` 或 `IsAttackBuffDispelTarget`。
- 默认规则应符合系统常识；例外才显式标记。例如 Buff 默认可驱散，不可驱散 Buff 才 override。
- C# 规则层不能出现中文或日文显示文本。显示名称、技能描述、状态描述和 UI 文本必须放在 `wwwroot/locales/*.json`。
- 规则 ID、资源 ID、事件 ID 应保持稳定、英文、kebab-case。
- 新机制需要同时考虑：后端规则、预测、在线同步、日志/事件、动画、音效、UI hover 说明、双语文本、测试和文档。
- 不要为了“漂亮架构”过度重构；但当机制明显会横向扩展时，要先抽象出稳定的概念边界。

## 新机制讨论时必须检查

每次用户提出新机制或技能，先从以下角度过一遍：

1. 是否影响游戏节奏和 5 分钟左右熟练局目标？
2. 是否支持核心价值：快节奏、清楚代价、有战略性、有意外但可理解？
3. 是否会制造固定最优解？
4. 与现有角色、盾、反击、预见、衰弱、绝对伤害、AP/Cost 的交互是什么？
5. 是否需要通用 modifier / status / aura / global effect，而不是写死在某个角色中？
6. 玩家能否通过 UI、预测、图标、音效和日志理解发生了什么？
7. 是否需要更新 `reference/` 文档和测试？

## Cost / AP 相关提醒

角色的基础 Cost 不应永远被视为最终 Cost。未来可能有：

- 角色 Buff/Debuff 改变自身 Cost。
- 天气、战场、回合规则改变全队或全局 Cost。
- 遗物、装备、事件改变 Cost 或 AP 上限。
- 技能对某类角色、某类攻击或某个行动临时修正 Cost。

因此新增 Cost/AP 机制时，应优先考虑“基础值 + 多来源 modifier 管线”的方向，而不是在攻击、按钮、预测、AP 校验等位置分别硬改。

详细设计原则见：

- `reference/TinyPixelFights_Extensibility_Principles.md`

