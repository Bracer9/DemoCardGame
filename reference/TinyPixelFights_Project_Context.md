# Tiny Pixel Fights 项目上下文说明（给外部 GPT / 大改造讨论用）

> 更新时间：2026-06-23  
> 目的：让一个完全不了解本项目的网页端 GPT，尽快获得接近当前主要开发者的上下文，用于和用户讨论后续大规模系统扩充。  
> 定位：介于 SRS（软件需求规格）和 HLD（高层设计）之间。不是完整工程规范，不追求过度工程化；重点是真实机制、当前流程、代码结构、扩展点与已知取舍。

---

## 1. 项目概述

`Tiny Pixel Fights` 是一个双人对战卡牌原型。最初来自 Game Design 课程作业中的打印卡牌游戏，后来被实现为网页可玩的版本，并进一步整合了本地双人模式和简易在线房间模式。

游戏目前是：

- 后端：C# / ASP.NET Core minimal API
- 前端：原生 HTML / CSS / JavaScript
- 模式：
  - 本地模式：同一台电脑上两名玩家轮流操作
  - 在线模式：一个房间，两名玩家通过链接对战
- 美术风格：中世纪极简暗黑风，红色 accent，局部参考 Persona 式强对比 HUD
- UI 语言：日语 / 中文双语切换，当前主要 UI 文本和机制文本都走 JSON localization
- 当前目标体验：
  - 熟练前约 10 分钟
  - 熟练后约 5 分钟
  - 快节奏、有意外性，但保留一定战略选择

这个项目不是大型可扩展卡牌引擎，但已经具备较明确的规则层、状态层、UI 资源层和在线同步边界。后续扩充时应该优先保持“规则集中在后端，表现集中在前端，文本集中在 locale，资源由 manifest 管理”的原则。

---

## 2. 当前游戏核心体验

玩家双方各随机获得 4 名角色。每个角色有：

- cost：主动进攻消耗的 AP
- attack：基础攻击
- HP：生命值
- attack type：物理 / 魔法
- skill：主动或被动技能

玩家轮流行动。行动方有固定 AP 上限，目前为 5。行动方可以：

1. 选择己方一个可行动角色
2. 选择敌方目标并查看预测
3. 确认主动进攻
4. 或消耗 AP 展开 / 强化共享盾
5. 最后结束回合

目标是击败对方全部角色。

当前游戏强调：

- 选择角色行动有 cost 代价
- 主动攻击会受到反击
- 盾牌可以提高生存，但不应变成唯一最优
- 每个职业的技能和基础数值要符合直觉
- 技能间有一定 synergy / 相性
- 视觉反馈很重要，特别是在线模式下非行动方需要看清“谁打谁、发生了什么”

### 2.1 一局游戏的真实玩家流程

当前玩家实际体验大致如下：

1. **打开页面**
   - 首先看到暗黑中世纪风格的标题界面。
   - 玩家可以在封面切换语言：日文 / 中文。
   - 玩家选择本地对战或在线房间。

2. **选择模式**
   - 本地模式：直接进入同一台电脑轮流操作的对战。
   - 在线模式：
     - 房主创建房间，获得邀请链接。
     - 第二名玩家通过链接加入。
     - 房主开始对战。

3. **洗牌与发牌**
   - 游戏不会突然直接显示手牌。
   - 进入发牌层后，画面显示牌堆和羊皮手套提示。
   - 玩家点击牌堆后，播放发牌动画与音效。
   - 在线模式下，只要一方点击牌堆，双方都会同步进入发牌动画。
   - 发牌结束后，播放回合转场动画，明确显示第一回合行动方。

4. **战场布局**
   - 行动视角始终保证“自己在下，对手在上”。
   - 每名玩家有 4 张角色卡。
   - 卡面包含：
     - cost
     - 攻击力
     - 当前 HP / 最大 HP
     - 攻击类型
     - 角色立绘
     - 技能短描述
     - buff / debuff 小图标
   - 正常角色使用彩色立绘；已行动或阵亡角色用 CSS 灰阶和暗化表现，不再切旧黑白图片。

5. **玩家回合**
   - 左下角 AP HUD 明确显示当前 AP。
   - 可行动角色卡可以被点击或拖拽。
   - 选择己方角色时，卡牌出现金白四角光标。
   - 选择或瞄准敌方角色时，敌方卡牌出现红色四角光标。
   - 鼠标指针也会随状态变化：
     - 默认羽毛笔
     - hover 我方可行动卡：皮革手套
     - 选中角色后 hover 敌方卡：交叉剑

6. **攻击预测**
   - 玩家选中己方角色和敌方目标后，会打开预测面板。
   - 预测面板显示：
     - 我方主动攻击预计伤害范围
     - 对方反击预计伤害范围
     - 攻击类型图标
     - 可能触发的技能
     - 盾、预见、守护、有恃无恐等重要修正提示
   - 预测不是纯装饰，它是当前游戏中帮助玩家理解“选择有代价”的关键 UI。

7. **确认攻击与演出**
   - 玩家确认攻击后，画面会用红色命令线连接攻击者和目标。
   - 随后根据战斗日志播放事件 icon：
     - 物理攻击
     - 魔法攻击
     - 反击
     - 技能
     - 状态
     - 治疗
     - 盾
     - 死亡
   - 同时播放伤害数字、HP 扣减、盾牌破裂、技能音效等。
   - 这部分尤其重要，因为在线模式下非行动方不能只看到“血突然少了”，必须知道是谁打了谁、什么效果触发了。

8. **防御阵型**
   - 行动方也可以消耗 AP 展开共享盾。
   - 盾牌在四张卡片上方/下方以蓝色弧形护罩显示。
   - 对手视角下，护罩方向会镜像，像是挡在两队之间。
   - 盾被打碎时有破碎粒子和音效。
   - hover 防御按钮会显示规则说明。

9. **hover 信息**
   - hover 角色卡时，卡牌左右延伸黑框：
     - 左侧显示角色属性和技能详情，重点展示技能说明。
     - 右侧显示当前 buff / debuff 详情。
   - 卡面本身只保留短技能描述，避免文本挤爆卡面。

10. **结束回合**
    - 玩家点击结束回合后，播放 turn curtain。
    - 轮到对方时，本方按钮变灰不可用。
    - 在线模式下，通过轮询同步对方操作，并播放相同的战斗演出。

11. **胜负**
    - 一方所有角色 HP 归零即失败。
    - 双方同时全灭则平局。
    - 游戏结束时播放胜利 / 失败 / 平局音效和结果面板。

### 2.2 当前体验重点与不要忽视的部分

后续讨论大改造时，不要只看数值和代码。这个游戏目前真实体验的核心由几层共同构成：

- **短局战斗节奏**：玩家应该频繁做小决策，而不是陷入复杂资源管理。
- **清楚的战斗反馈**：尤其在线模式中，非行动方必须看懂发生了什么。
- **角色刻板印象**：公主脆但有强辅助，骑士硬且守护，魔法使高攻脆皮，农民朴素但能播种收获。
- **选择有代价**：高 cost、高攻击、开盾、强化盾、先手集火等都应有明显机会成本。
- **UI 是机制的一部分**：预测面板、buff hover、盾牌弧形、事件 icon、音效共同承担规则解释。

如果新增系统让玩家更难理解“刚刚发生了什么”，即使规则本身有趣，也需要配套演出和说明。

---

## 3. 主要规则系统

### 3.1 初始发牌

角色池固定 8 名角色，随机洗牌后每方 4 名。

重要约束：

- 每名玩家开局至少拥有 1 张 cost 1 角色。
- 后端 `GameEngine.CreateGame()` 负责洗牌、分配、修正 cost 1 分布。
- 在线模式下，只有一方玩家点击牌堆即可触发发牌；另一方通过房间状态同步看到发牌动画。

发牌阶段前端流程：

1. 游戏开始
2. 显示洗牌 / 牌堆界面
3. 玩家点击牌堆
4. 播放发牌动画
5. 发牌结束后插入 turn curtain，显示第一回合行动方

### 3.2 回合与 AP

当前固定：

- `MaxActionPoints = 5`
- 每回合开始时 AP 重置为 5
- 每名角色每回合最多主动攻击一次
- 攻击消耗该角色 cost
- 行动结束后该角色 `HasActed = true`
- 行动过或阵亡角色在 UI 上灰阶显示

如果还有 AP 但没有角色可行动，前端 / 后端曾修复过相关结束回合问题。后续扩展要避免“玩家被卡住不能结束回合”的情况。

### 3.3 主动攻击与反击

主动攻击的核心规则：

- 攻击方选择己方角色作为 attacker
- 选择敌方角色作为 defender
- 攻击造成伤害与反击伤害视为同时发生
- 反击不再等值于攻击，而是：

```text
counter = max(1, defender base attack - 1)
```

再受状态、盾、技能等修正。

注意：

- 反击来自被攻击角色
- 反击可以被“有恃无恐”等状态降低
- 当前实现中主动攻击和反击都通过 `DamagePacket` 走统一修正流程
- 主动攻击后，攻击者设为已行动并扣 AP

### 3.4 伤害类型

后端枚举 `DamageType`：

- `Physical`
- `Magical`
- `Absolute`

绝对伤害当前只用于怪兽技能：

- 直接扣 HP
- 不受盾、buff、debuff、伤害修正影响

`DamageSource`：

- `ActiveAttack`
- `CounterAttack`
- `Skill`
- `Status`

目前很多技能判断依赖 source，例如骑士守护只对主动、物理攻击生效。

### 3.5 共享盾 / 防御阵型

当前盾牌系统是全队共享，不绑定单张牌。

常量：

```csharp
FirstShieldCost = 2
FirstShieldValue = 2
ReinforcedShieldCost = 1
ReinforcedShieldBonus = 2
MaxShieldDeploymentsPerTurn = 2
```

当前逻辑：

- 第一次展开盾：消耗 2 AP，获得共享盾 2
- 如果当前回合已经展开过盾，且共享盾仍大于 0，可以强化：
  - 消耗 1 AP
  - 在现有共享盾基础上 +2
  - 给全体存活角色赋予 `shield-complacency`（有恃无恐）
- 如果第一次盾已经被打破，则不能“强化”，下一次只能重新按第一层盾处理
- 共享盾会吸收所有类型的普通可修正伤害
- 共享盾归属于玩家队伍，回合切换到该玩家行动时会被清空

`shield-complacency`：

- 中文概念名：有恃无恐
- 本质是 debuff
- 只在强化盾时产生
- 效果：该角色造成反击伤害时 -1
- 发动一次或到下次自己回合开始时消失

这个设计用于解决“双方无限开盾最优”的问题，让强化盾更偏防守并降低下次反击威慑。

---

## 4. 当前角色与技能

角色定义在 `Domain/CharacterDefinition.cs`。

当前 8 名角色：

| key | 概念 | cost | attack | HP | 类型 | skill |
|---|---|---:|---:|---:|---|---|
| `peasant` | 农民 | 1 | 2 | 16 | Physical | `spring-harvest` |
| `princess` | 公主 | 1 | 1 | 12 | Physical | `saints-prayer` |
| `mage` | 魔法使 | 2 | 4 | 16 | Magical | `searing-mark` |
| `oracle` | 占卜师 | 1 | 1 | 14 | Magical | `stargazers-aegis` |
| `knight` | 骑士 | 3 | 3 | 24 | Physical | `interposing-shield` |
| `druid` | 德鲁伊 | 1 | 1 | 16 | Magical | `weakening-spores` |
| `barbarian` | 狂战士 | 2 | 4 | 18 | Physical | `aftershock-axe` |
| `monster` | 怪兽 | 3 | 3 | 22 | Physical | `predatory-instinct` |

### 4.1 公主：`saints-prayer`

被动。

在公主存活且轮到己方行动时：

- 己方所有存活角色 HP +1
- 最多超过上限 2 点
- UI 作为 aura 状态 `blessing` 显示

### 4.2 占卜师：`stargazers-aegis`

被动。

两部分：

1. 预见减伤：
   - 己方全体受到物理伤害时 25% 概率 -1
   - 己方全体受到魔法伤害时 50% 概率 -1
2. 魔法增伤：
   - 占卜师存活时，己方全体获得 `magic-power`
   - 魔法伤害 +1
   - 该 buff 是攻击强化，但不可被德鲁伊驱散

注意：`foresight` 减伤作为 aura 表示，不应被德鲁伊当作攻击 buff 移除。

### 4.3 农民：`spring-harvest`

被动，但与主动进攻时机有关。

真正的播种 / 收获交替：

- 如果农民是本方回合第一张被打出的卡，并且当前没有 harvest，则获得 `harvest-pending`
- 到下次己方回合开始时转化为 `harvest`
- `harvest`：主动攻击 +2，持续到该己方回合结束
- `harvest-pending` 现在也被视为攻击 buff 驱散目标，这样德鲁伊提前驱散农民的延迟收益才有意义

### 4.4 魔法使：`searing-mark`

主动。

主动攻击后：

- 若目标仍存活，50% 概率附加 `burning`
- `burning` 在目标所属玩家下次回合开始时造成 1 点魔法伤害，然后消失
- 如果燃烧伤害受己方占卜师 `magic-power` 影响，则可以 +1

### 4.5 德鲁伊：`weakening-spores`

主动。

主动攻击后，如果目标仍存活：

- 若本次主动攻击造成伤害：100% 发动
- 若未造成伤害：50% 发动
- 发动后：
  - 移除目标身上一个随机可驱散 Buff
  - 移除已有 weakness / weakness-pending
  - 赋予 `weakness-pending`

`weakness-pending`：

- 到目标所属玩家下次回合开始时，转化为 `weakness`

`weakness`：

- 主动攻击伤害 -2
- 持续到该玩家回合结束

当前可被衰弱孢子移除的 Buff 包括：

- `beast-rage`
- `magic-power`
- `harvest`
- `harvest-pending`

### 4.6 狂战士：`aftershock-axe`

主动。

主动攻击如果对目标造成至少 3 点伤害：

- 对目标相邻的一个存活敌人造成 1 点物理技能伤害
- 如果左右都有相邻角色，随机一个
- 如果只有一个相邻角色，则必定命中该角色
- 如果没有相邻角色，不造成额外伤害

### 4.7 怪兽：`predatory-instinct`

主动 + 条件被动。

主动部分：

- 技能名本地化为“美女与野兽”等对应语言文本
- 若主动攻击没有造成伤害
- 且目标不是公主
- 则追击造成 3 点绝对伤害
- 如果己方公主存活，绝对伤害 +1

条件被动：

- 如果己方公主死亡
- 怪兽获得 `beast-rage`
- `beast-rage`：基础攻击 +2
- 该 buff 是攻击强化，但不可被德鲁伊驱散

### 4.8 骑士：`interposing-shield`

被动。

守护规则：

- 骑士存活且本回合守护未消耗时，全队显示 `guard` aura
- 当敌方主动物理攻击命中骑士以外的己方角色
- 且共享盾没有完全消化该伤害后
- 骑士介入：
  - 原目标受到的主动伤害 -1
  - 骑士承受 1 点物理 collateral damage
  - 全队共享一次，发动后 `GuardConsumed = true`

不对以下情况生效：

- 魔法主动攻击
- 技能伤害
- 状态伤害
- 攻击目标就是骑士
- 伤害已被共享盾完全吸收

---

## 5. 状态系统

状态基类在 `Domain/StatusEffects.cs`。

重要属性：

- `Id`
- `IsBuff`
- `Magnitude`
- `SourceCharacterId`
- `Expired`
- `IsAttackBuff`
- `IsDispellable`：是否可被驱散类效果移除；默认`true`，不可驱散状态需显式 override 为`false`

重要生命周期 hook：

- `OnTurnStart`
- `OnTurnEnd`
- `ModifyBaseAttack`
- `ModifyActiveAttack`

当前状态：

| id | 类型 | 说明 |
|---|---|---|
| `beast-rage` | buff | 怪兽因己方公主死亡获得，基础攻击 +2；不可驱散 |
| `magic-power` | buff | 占卜师存活时赋予，魔法伤害 +1 |
| `burning` | debuff | 下次自己回合开始受到魔法伤害 |
| `weakness-pending` | debuff | 延迟转化为 weakness |
| `weakness` | debuff | 主动攻击伤害 -2 |
| `harvest-pending` | buff | 农民播种后的延迟攻击 buff |
| `harvest` | buff | 农民收获，主动攻击 +2 |
| `shield-complacency` | debuff | 强化盾副作用，反击伤害 -1 |

另外 UI 中会显示一些 aura 状态，但它们不一定真实存在于 `Statuses` 列表，例如：

- `blessing`
- `foresight`
- `team-shield`
- `guard`

这些在 `GameViewFactory` 中根据队伍状态补进 DTO，供前端展示。

---

## 6. 后端代码结构

### 6.1 目录总览

```text
Domain/
  CharacterDefinition.cs   角色静态定义
  GameTypes.cs             枚举、本地化文本结构
  GameState.cs             游戏状态模型
  GameEngine.cs            核心规则引擎
  Skills.cs                技能系统
  StatusEffects.cs         状态系统

Api/
  GameDtos.cs              后端状态转前端 DTO
  AttackPreviewService.cs  攻击预测

Services/
  GameSession.cs           本地内存 session 池
  OnlineGameSession.cs     在线房间与座位/session

wwwroot/
  index.html               页面结构
  styles.css               主要视觉与动画
  app.js                   前端状态、交互、渲染、动画同步
  audio.js                 音频管理
  i18n.js                  多语言渲染
  ui-assets.js             UI 图标资源加载
  locales/*.json           日文/中文文本
  config/audio.json        音效事件映射
  config/ui-assets.json    UI 图标 manifest

assets/
  colored_portraits/       当前实际使用的角色彩色立绘
  audio/                   运行时音效
  ui/                      图标资源

tests/
  *.test.js                Node 测试，覆盖本地化、音频、UI manifest、部分规则
```

### 6.2 设计边界

后端负责：

- 随机发牌
- AP、回合、攻击、反击、盾、技能、状态
- 胜负判定
- 攻击预测数据
- 结构化日志
- 本地/在线模式统一规则

前端负责：

- 显示游戏状态
- 选牌、拖拽、攻击预测面板
- 发牌动画、回合转场、伤害/技能/状态演出
- 音频播放
- 多语言文本渲染
- 在线模式轮询同步

原则：

- C# 里不应该出现日文/中文显示文本
- C# 只返回 localization key + typed args
- 前端用 locale JSON 渲染文本
- 资源路径和事件映射尽量放 manifest，不散落硬编码

---

## 7. API 与在线模式

### 7.1 本地 API

主要 endpoint：

- `GET /api/game/state`
- `POST /api/game/new`
- `GET /api/game/preview?attackerId=&defenderId=`
- `POST /api/game/attack`
- `POST /api/game/shield`
- `POST /api/game/end-turn`

本地模式使用 `GameSession`，通过前端 `X-Local-Session` header 按浏览器 token 隔离内存状态。不同设备或不同浏览器可各玩各的本地 / AI / 测试模式，不需要数据库；Render 重启或实例休眠后这些本地状态不会持久保留。

### 7.2 在线 API

主要 endpoint：

- `POST /api/online/room/create`
- `POST /api/online/room/join`
- `GET /api/online/room`
- `POST /api/online/game/new`
- `POST /api/online/game/deal`
- `GET /api/online/game/state`
- `GET /api/online/game/preview`
- `POST /api/online/game/attack`
- `POST /api/online/game/shield`
- `POST /api/online/game/end-turn`

在线模式特征：

- 当前只支持一个双人房间
- 房主创建房间，第二人加入
- 使用 token 标识 seat
- host 才能开始 / 重开
- 每 500ms 前端轮询在线状态
- 服务器不推送 WebSocket
- 在线模式和本地模式共享同一个 `GameEngine`

这不是正式多人服务器架构，而是为了快速 playtest 的简易在线同步。后续若扩展到多房间、断线重连、观战、匹配，需要重构 `OnlineGameSession`。

---

## 8. 前端流程与 UI

### 8.1 页面结构

`index.html` 中主要区域：

- start screen：封面、语言切换、本地/在线模式选择
- online lobby：创建/加入房间
- deal sequence：洗牌、点击牌堆、发牌动画
- topbar：回合数、round、audio menu、new game
- opponent zone：上方敌方卡牌
- command deck：盾牌按钮、指令提示、结束回合
- active zone：下方己方卡牌
- AP HUD：左下角 AP 显示
- preview panel：攻击预测
- battle log：战斗日志
- inspectors：卡牌 hover 详情、盾牌 hover 详情
- fx layer：所有战斗演出和浮动数字

### 8.2 视角

无论本地还是在线：

- 当前 viewer 自己的队伍在下方
- 对手在上方
- 在线模式下，服务器为不同玩家生成不同 viewer 视角

### 8.3 选牌和攻击

当前前端交互：

- 点击己方可行动卡：选中
- 再点击敌方卡：请求 preview
- 确认后攻击
- 也可以拖动己方卡，用红色命令线指向敌方卡
- 选中己方卡后点击空白区域会取消选择
- 我方选中卡有金白四角光标
- 敌方目标有红色四角光标
- 鼠标 cursor 有定制：
  - 默认羽毛笔
  - hover 我方可行动卡：皮革手套
  - 选中攻击者后 hover 敌方：交叉剑

### 8.4 动画 / 演出

前端 `app.js` 根据后端日志和 combat outcome 播放演出。

包括：

- 发牌动画
- turn curtain
- 选中光标
- 拖拽攻击线
- 确认攻击后 attacker-to-target link
- 物理 / 魔法 / 反击 / 技能 / 状态 / 治疗 / 盾 / 死亡等 event icon burst
- 伤害数字
- 盾牌展开、强化、命中、破碎、消失
- 死亡卡片切断效果

目前很多演出是根据 log key 分支处理，扩展技能时要同步考虑：

- 后端是否产生足够结构化的 log
- 前端是否能根据 log key 找到正确目标
- 是否需要新增 event icon / sound

---

## 9. 美术与资源系统

### 9.1 角色立绘

当前实际使用：

```text
assets/colored_portraits/*.png
```

旧黑白立绘仍在 `assets/*.png`，但不再用于卡面切换。现在：

- 正常状态使用彩色立绘
- 已行动 / 阵亡通过 CSS 对彩色立绘做灰阶、暗化
- 各角色用 `data-key` + CSS variable 调整立绘缩放和位置

不要轻易回到“彩色图和黑白图切文件”的方案，因为两套图虽然分辨率相近，但人物位置和大小不完全一致，会导致状态切换时跳动。

### 9.2 UI icon

图标 manifest：

```text
wwwroot/config/ui-assets.json
```

资源：

```text
assets/ui/events/
assets/ui/statuses/
assets/ui/skills/
```

运行时：

- `wwwroot/ui-assets.js` 加载 manifest
- `art.icon(...)` 生成图标 HTML
- `art.forStatus(...)`, `art.forDamageType(...)` 等做映射

新增技能 / 状态 / event 时应该：

1. 放图标资源
2. 更新 `ui-assets.json`
3. 更新 locale 文本
4. 前端演出逻辑按需处理

### 9.3 Audio

音频 manifest：

```text
wwwroot/config/audio.json
```

音频管理：

```text
wwwroot/audio.js
```

当前有三类 bus：

- `bgm`
- `sfx`
- `ui`

右上角 AUDIO 下拉菜单：

- BGM 开关 + 音量
- 音效开关 + 音量
- 音效包含 `sfx` 和 `ui`

浏览器自动播放策略曾导致 BGM 首次播放不稳定，所以 `audio.js` 有 unlock / pending events / prime 等逻辑。不要轻易删。

---

## 10. Localization

locale 文件：

```text
wwwroot/locales/ja.json
wwwroot/locales/zh.json
```

前端：

```text
wwwroot/i18n.js
```

原则：

- C# 不写日文/中文显示文本
- 后端用 `LocalizedText(Key, Args)` 表达消息
- Args 带类型，例如：
  - raw
  - character
  - player
  - damageType
  - skill
  - status
  - ui
- 前端根据类型查对应 locale

已有测试会检查：

- 日中 JSON schema 一致
- C# 不包含日文/中文显示文本
- C# 引用的 localization ID 存在

后续新增规则时必须同步 locale。

---

## 11. 当前测试与质量门槛

常用验证：

```powershell
node --check wwwroot\app.js
node --test tests\*.test.js
dotnet build --no-restore
```

已有测试覆盖：

- audio director 行为和 manifest 资源存在
- localization schema 与 C# 文本污染
- 在线 preview 不阻塞轮询
- shield cost 规则
- UI asset manifest
- 8 类 event 演出是否接入
- 攻击 link 演出
- 德鲁伊驱散攻击 buff 规则

注意：

- 这些测试不是完整规则模拟测试。
- 复杂平衡和流程 bug 仍主要靠 playtest。
- 后续大改造前建议补一些规则级单元测试，尤其是新系统会影响多个技能时。

---

## 12. 已知设计取舍与风险

### 12.1 当前在线模式只是 playtest 级别

`OnlineGameSession` 只支持一个房间，不适合正式部署。

如果讨论正式联机，需要考虑：

- 多房间管理
- 房间生命周期
- 断线重连
- 状态持久化
- WebSocket / SSE
- 防作弊
- 服务器权威状态

但除非用户明确要正式上线，不要一上来把项目重构成大型服务端架构。

### 12.2 前端 app.js 已经比较大

`wwwroot/app.js` 同时处理：

- state rendering
- input
- online polling
- animation
- combat log playback
- audio event triggering

如果后续系统大扩充，可能需要拆分，但不要为了“漂亮架构”而提前大拆。

建议优先按功能边界渐进拆分：

- `render/cards`
- `combat-animations`
- `online-client`
- `commands`

### 12.3 日志驱动演出有上限

当前很多动画靠解析 log key 和 args。

优点：

- 不需要另建复杂 event stream
- 与在线同步自然兼容

缺点：

- 新技能如果 log 不够结构化，前端很难知道应该演谁
- log 文本和演出事件耦合风险变高

如果后续加入大量复杂技能，建议考虑在 API 中增加更明确的 `CombatEvent[]`，但不要立刻废除现有 log。

### 12.4 平衡仍处于原型阶段

已经做过多轮 playtest 调整：

- HP 整体提高
- AP 从 3 调到 4，再到 5
- 反击削弱
- 盾牌机制多次简化和调整
- 德鲁伊用于克制攻击 buff
- 怪兽、公主、占卜师等多轮技能修正

后续大改造时，应该先明确：

- 目标单局时长
- 是否允许更复杂资源系统
- 是否加入牌库 / 抽牌 / 装备 / 地形 / 职业成长等
- 是否仍保持“8 张角色牌对战”的核心

---

## 13. 适合扩展的方向

下面只是讨论入口，不是已决定方案。

### 13.1 战斗系统扩展

可能方向：

- 更多行动类型：防御、蓄力、换位、技能单独释放
- 更多状态层：护甲、流血、眩晕、嘲讽、诅咒
- 攻击范围：单体、相邻、全体、列
- 位置系统：当前 slot 已存在，可扩展
- 先后手补偿：先手 AP 限制、后手 bonus、选择先后手等

注意不要破坏当前“短局、直观、快速”的核心。

### 13.2 卡牌/角色成长

可能方向：

- 每局前选择角色
- 随机 draft
- 角色升级
- 装备 / 遗物
- Roguelike 关卡结构

如果走 roguelike，需要重新定义：

- 单局战斗 vs 长线 progression
- 战斗后奖励
- 角色死亡是否永久
- 在线模式是否仍支持

### 13.3 技能系统重构

当前技能通过 C# class hook 实现，适合手写少量技能。

若技能数量暴增，可讨论：

- 是否引入 data-driven skill
- 是否只把数值 data-driven，复杂逻辑仍写 C#
- 是否设计统一 CombatEvent
- 是否建立规则测试 DSL

建议不要一开始就做完整脚本语言。

---

## 14. 和外部 GPT 讨论时的建议提示词

可以把本文档贴给另一个 GPT，并这样开始：

> 你现在扮演 Tiny Pixel Fights 的系统设计合作者。请先阅读这个项目上下文，不要急着给重构方案。这个游戏是一个 C# 后端 + 原生 HTML/JS 前端的双人战斗卡牌原型，已经有本地和在线模式。我接下来想大规模扩展机制，但希望保持快节奏、可 playtest、不要 overengineer。请先确认你理解当前规则、流程、代码边界，然后和我一起讨论新系统是否会破坏节奏、如何影响现有技能、需要怎样的最小实现路径。

讨论大改造时，建议让 GPT 先回答：

1. 这个新想法影响哪些现有机制？
2. 它会让单局变快还是变慢？
3. 它是否强化了当前核心价值：快节奏、有战略代价、有意外性？
4. 它需要改后端规则、前端表现、localization、audio、UI assets 哪几层？
5. 有没有更小的原型实现方式？
6. 哪些情况必须补测试？

---

## 15. 对后续协作者的提醒

这个项目最大的隐性需求不是“架构优雅”，而是：

- 用户正在边设计边 playtest
- 机制会频繁改变
- 很多规则来自实际游玩反馈
- 所以实现应该可改、可验证、可回退

在建议大改造时，请避免：

- 上来要求全项目重写
- 把在线模式设计成正式商业服务器
- 把所有技能改成复杂脚本系统
- 忽视现有中文/日文 localization
- 忽视前端演出和音效，因为 playtest 反馈已经证明“看不懂发生了什么”是核心问题

更好的工作方式是：

1. 先和用户确认目标体验
2. 用小表格列出新机制影响面
3. 先做规则原型
4. 再补 UI / 演出 / 音效
5. 最后更新 docs 和测试
