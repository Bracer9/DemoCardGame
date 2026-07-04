# Tiny Pixel Fights — Agent Instructions

本文件是项目级开发指令。当前 best practice 是保留在项目 root 的 `AGENTS.md`，不要迁到空的 `.agents/` 目录。`.agents/` 仅作为未来可选辅助目录，不是主入口。

与用户沟通默认使用中文。除非用户明确要求，不要提交 git commit，不要进行破坏性清理。

## 项目定位

这是一个正在从课程作业原型扩展成战术 JRPG 卡牌游戏的项目。当前核心是本地/在线二人对战；未来可能扩展爬塔、英雄成长、普通士兵、副官、round buff、天气/战场规则和视觉小说演出。

重大机制、UI 方向和平衡决策必须回到 [GDD.md](GDD.md)。GDD 是 living document，不是一次写死的说明书。

## 架构原则

- 优先考虑延展性，而不是只修当前一例。
- 不要为单个技能新增过窄的专用属性或专用流程。优先设计通用概念，例如 `IsDispellable`，而不是 `IsDruidDispelTarget` 或 `IsAttackBuffDispelTarget`。
- 默认规则应符合系统常识；例外才显式标记。例如 buff 默认可驱散，不可驱散 buff 才 override。
- C# 规则层不能出现中文或日文显示文本。显示名称、技能描述、状态描述和 UI 文本必须放在 `wwwroot/locales/*.json`。
- 规则 ID、资源 ID、事件 ID 应稳定、英文、kebab-case。
- 新机制需要同时考虑：后端规则、预测、在线同步、日志/事件、动画、音效、UI hover 说明、双语文本、测试和文档。
- 不要为了“漂亮架构”过度重构；但当机制明显会横向扩展时，要先抽象出稳定的概念边界。

## 新机制讨论检查表

每次用户提出新机制、技能或系统，先从以下角度过一遍：

1. 支持 GDD 的哪条设计支柱？
2. 是否创造有意义的取舍、风险回报、预测对手或长期规划？
3. 是否会制造固定最优解或无限僵持？
4. 与现有角色、共有盾、反击、预见、衰弱、绝对伤害、AP/Cost 的交互是什么？
5. 是否需要通用 modifier / status / aura / resource / global effect，而不是写死在某个角色中？
6. 玩家能否通过预测、UI、图标、动画、音效、语音和日志理解发生了什么？
7. 是否影响在线同步、回放式日志演出、hover 说明、本地化和测试？
8. 是否需要更新 `GDD.md` 或 `reference/` 文档？

## Cost / AP 相关提醒

角色基础 Cost 不应永远被视为最终 Cost。未来可能有：

- 角色 buff/debuff 改变自身 Cost。
- 天气、战场、回合规则改变全队或全局 Cost。
- 遗物、装备、事件改变 Cost 或 AP 上限。
- 技能对某类角色、某类攻击或某个行动临时修正 Cost。

因此新增 Cost/AP 机制时，应优先考虑“基础值 + 多来源 modifier 管线”的方向，而不是在攻击、按钮、预测、AP 校验等位置分别硬改。

详细设计原则见：

- `reference/TinyPixelFights_Extensibility_Principles.md`

## UI / UX 原则

- 当前布局是 PC 优先的 1920×1080 虚拟舞台，外围整体缩放。不要随意改回响应式流式布局。
- UI 风格是中世纪战争会议桌：羊皮纸地图、黑铁、深皮革、旧金、战术令牌。避免现代电竞、玻璃拟态、赛博斜切、网页表单感。
- 卡牌、HUD、hover inspector、预测框、战况日志、声音菜单要保持同一视觉语言。
- 可行动、选中、目标、护盾破碎、伤害、技能、buff/debuff、死亡都必须有清楚反馈。
- 不要依赖浏览器截图自动验证；用户环境下 Codex 浏览器连接长期不稳定。优先通过代码检查、构建和可调 CSS 变量支持用户用 DevTools 视觉确认。
- UI 调参说明应写入 `reference/TinyPixelFights_UI_Tuning_Guide.md`。

## 声音、语音与资源

- 音效和语音要走配置/目录索引，不要在 JS/C# 中硬编码具体日语文件名。
- 角色语音目录规则见 `reference/TinyPixelFights_Battle_Voice_System.md`。
- 音效映射和 checklist 见 `reference/TinyPixelFights_Audio.md` 与 `assets/audio/SFX_CHECKLIST.md`。
- 新增资源 ID 应稳定、英文、kebab-case。资源文件可以有用户原始文件名，但接入层应使用清晰映射。
- 新增 UI / VFX / 音频分类时，同步更新对应 reference 文档。

## 文档与课程方法

- 课程笔记在 `reference/GameDesign/`。涉及 GDD、机制设计、概率、系统循环、playtest 反馈时，应优先参考这些笔记。
- GDD 负责记录项目方向和重大决策。
- `reference/` 下的专项文档负责记录实现细节、调参方式和资源映射。
- 文档要有效、简洁、可维护；不要为了完整而写成没人看的长文。

## 中文 / 日文文档读取

- 不要再用 PowerShell `Get-Content` 读取中文或日文 Markdown / JSON 正文；它在当前环境中经常把 UTF-8 显示成乱码，浪费时间。
- 读取中文、日文或混合文本文件时，默认使用：`node tools/read-utf8.mjs <file> [startLine] [lineCount]`。
- 搜索仍优先使用 `rg`；需要查看上下文时，再用 `tools/read-utf8.mjs` 按行读取。
- 修改中文文档时，必须保持 UTF-8，不要因为 PowerShell 显示乱码就误判文件内容损坏。

## 验证与安全

- 修改 JS 后运行：`node --check wwwroot/app.js`。
- 修改 C# 或整体逻辑后运行：`dotnet build`。
- 修改 JSON 后至少用 `ConvertFrom-Json` 检查。
- 不要删除用户资源、音频、图片或未提交改动，除非用户明确要求。
- 工作区可能已有用户改动；只处理当前任务相关文件。
- 不要把缓存、素材库临时目录或无关大文件纳入变更。
