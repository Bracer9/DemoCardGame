# Tiny Pixel Fights — Codex 交接备忘录

> 日期：2026-06-28  
> 用途：给新的 Chat / Codex 快速接手项目。本文只写“接手时必须知道的工作方式和当前易踩坑”，不替代 `AGENTS.md`、`GDD.md` 或专项文档。

---

## 1. 接手后先读这 6 份

1. `AGENTS.md`  
   项目级规则。尤其注意：默认中文沟通；不要擅自 commit；C# 规则层不能写中文/日文显示文本；新增机制要考虑预测、日志、动画、音效、在线同步、本地化和文档。

2. `GDD.md`  
   Living GDD。重大机制、UI 大方向和平衡原则要回到这里更新，但不要把所有实现细节都塞进去。

3. `reference/TinyPixelFights_Project_Context.md`  
   项目整体结构和现有战斗系统概览。给“完全不知道项目的人”补上下文。

4. `reference/TinyPixelFights_Extensibility_Principles.md`  
   最重要的架构心法：不要写窄特例；优先用通用 modifier/status/resource/global effect 概念。默认可驱散，例外显式标记。

5. `reference/TinyPixelFights_PvP_GrowthPrototype_Plan_20260628.md`  
   当前进入的新阶段路线：BP → Reward Window → 真实奖励/英雄/普通兵/副官/遗物/天气。

6. 最近正在动的专项文档：
   - `reference/TinyPixelFights_BP_System.md`
   - `reference/TinyPixelFights_Dummy_Reward_Window.md`
   - `reference/TinyPixelFights_Defense_System.md`
   - `reference/TinyPixelFights_UI_Tuning_Guide.md`
   - `reference/TinyPixelFights_Battle_Voice_System.md`

---

## 2. 当前真实状态一句话

项目已经不是最初的 8 张卡作业原型，而是一个本地/在线统一的 PC 对战原型；现在正在把它扩展成“战术 JRPG 卡牌战斗”的 PvP growth prototype。

当前重点不再是单纯调 UI，而是建立能承载后续系统的底座：

- 物理/魔法防御已进入基础属性。
- BP 已进入玩家级资源。
- 固定 Round 奖励窗口已进入后端状态与在线同步。
- 临时三档奖励已作为“遗物/成长奖励最小原型”接入。
- 后续很可能继续接英雄升级、普通兵、招募、副官、遗物、天气/round buff。

---

## 3. 当前已接入但仍是 prototype 的系统

### BP

位置：`PlayerState.BattlePoints`、`GameEngine.TryGainBp/TrySpendBp`、前端 BP HUD。

当前口径：

- 初始 5。
- 上限 10。
- 每回合获得上限 3。
- 回合开始 +1。
- 破坏敌方共有盾 +1。
- 己方回合对敌方造成 HP 伤害 +1。
- 将防御阵型展开到满层 +1。

注意：BP 现在是奖励消费资源，不是经验条。不要把它做成复杂等级系统。

### Reward Window

位置：`RewardWindowState`、`RewardDefinitions.cs`、`GameEngine.SelectReward/ResetRewardWindow/SkipRewardWindow`、`Program.cs` API、前端 `reward-window`。

当前口径：

- Round 1 首次出现，之后每 3 Round。
- 每名玩家同一奖励 Round 只处理一次。
- reset 第一次免费，第二次开始 1 BP。
- skip 尝试获得 +1 BP。
- 开窗期间禁止攻击/防御/结束回合等战斗操作。

当前临时奖励：

- 2 BP：当前在场角色魔法防御 +1。
- 4 BP：当前在场的魔法攻击角色攻击 +1。
- 6 BP：当前在场角色攻击 +1。

这些可以保留为未来 relic/reward effect 的 base，但以后应该逐步把 `dummy-reward-*` 改成正式 reward/relic ID。

### 怪兽二阶段

`beast-rage` 已改为：任意一方公主阵亡后，存活怪兽获得野兽之怒，基础攻击 +2。  
这是通过 `CharacterSkill.OnCharacterDefeated` 通用阵亡 hook 实现的，不要退回到在 GameEngine 里写怪兽专用逻辑。

---

## 4. 代码结构接手提示

- 规则核心在 `Domain/`：
  - `GameEngine.cs`：回合、攻击、盾、BP、Reward Window 等流程。
  - `Skills.cs`：角色技能。新增技能优先通过 hook / modifier，不要在 GameEngine 写技能特判。
  - `StatusEffects.cs`：buff/debuff/status。显示文本不在这里。
  - `CharacterDefinition.cs`：基础角色数值。
  - `RewardDefinitions.cs`：当前 dummy reward 定义。

- API/DTO 在 `Api/` 和 `Program.cs`。
  - 在线模式和本地模式共用大部分规则。
  - 新状态要进 `GameDtos.cs`，否则前端/在线视角看不到。

- 前端主要在：
  - `wwwroot/app.js`
  - `wwwroot/styles.css`
  - `wwwroot/index.html`
  - `wwwroot/locales/ja.json`
  - `wwwroot/locales/zh.json`

前端现在偏大，别大规模重写。优先局部修改、保持现有 DOM/事件绑定。

---

## 5. 最容易踩坑的地方

### 1. 不要在 C# 写显示文本

规则 ID 用英文 kebab-case。  
中日文本写进 `wwwroot/locales/*.json`。  
技能、状态、奖励、日志都要走本地化。

### 2. 不要为单个技能发明窄属性

反例：`IsDruidDispelTarget`、`IsAttackBuffDispelTarget`。  
正例：`IsDispellable`、`IsAttackBuff`、`ModifyBaseAttack`、`ModifyPhysicalDefense`。

用户非常在意这个，后续开发必须从延展性考虑。

### 3. UI 是 1920×1080 虚拟舞台

现在不是传统响应式布局，而是内部 1920×1080，外层整体 scale。  
不要随手改回流式 responsive。  
UI 调参写在 `reference/TinyPixelFights_UI_Tuning_Guide.md`。

### 4. 浏览器自动验证不可靠

用户环境里 Codex browser 连接长期不稳定。  
不要花大量时间试图截图验证。  
优先：

- 代码检查
- 构建
- 可调 CSS 变量
- 让用户用 DevTools 确认视觉

### 5. 构建可能被运行中的游戏锁住

如果普通 `dotnet build` 报 dll/exe 被 `TinyPixelFights` 进程锁定，不一定是代码错。  
可以用：

```powershell
dotnet build -p:UseAppHost=false -o $env:TEMP\TinyPixelFightsVerify
```

### 6. 工作区经常是 dirty

当前项目里经常有用户新素材、临时资源、未提交改动。  
不要自动删除、移动、重命名用户资源。  
`git status --short` 只作为参考，不要擅自清理。

---

## 6. 新系统开发的推荐节奏

每次用户提出新系统时，建议按这个顺序：

1. 先确认它支持 GDD 的哪条设计支柱。
2. 写一份简短实装文档到 `reference/`。
3. 明确后端状态、DTO、前端 UI、日志、动画/音效、本地化、在线同步。
4. 做最小可玩的 prototype。
5. 跑：

```powershell
node --check wwwroot\app.js
Get-Content -Encoding UTF8 wwwroot\locales\zh.json | ConvertFrom-Json > $null
Get-Content -Encoding UTF8 wwwroot\locales\ja.json | ConvertFrom-Json > $null
dotnet build -p:UseAppHost=false -o $env:TEMP\TinyPixelFightsVerify
```

6. 更新对应专项文档；重大方向再更新 `GDD.md`。

不要一口气把完整系统做完。用户现在明确采用“分阶段 prototype”。

---

## 7. 和用户协作时的注意事项

- 用户希望中文沟通，直接、快、少废话。
- 用户非常讨厌“为了架构而架构”，但也讨厌临时硬编码导致未来扩展困难。要在两者之间取平衡。
- 用户经常会先说“先别实装，讨论”，这时不要动文件。
- 用户说“小改动/快一点”时，不要大范围检索和重构。
- 用户会用 DevTools 调 CSS；需要把可调参数集中为变量，并写入 UI tuning 文档。
- 如果用户指出视觉问题，不要辩解“正常”。先判断是否与他的美术目标冲突。

---

## 8. 下一阶段最可能的方向

按目前路线，后继者很可能要处理：

1. 把 dummy reward 升级成正式 reward/relic effect 管线。
2. 引入英雄升级或局内成长。
3. 引入普通士兵/队伍人数变化。
4. 引入副官槽。
5. 引入天气/round buff。
6. 继续优化 BP、奖励窗口、战斗可读性和语音/VFX。

建议优先建立通用 reward effect / modifier / team effect 结构，而不是给每个奖励在 `SelectReward` 里继续 `switch` 下去。

---

## 9. 最后提醒

这个项目的核心不是“把卡牌规则越堆越复杂”，而是：

- 战术选择有代价。
- 资源决策有重量。
- 角色像活人，有成长、有声音、有状态。
- 玩家能清楚看懂为什么发生了某个结果。

所有新机制都应该服务这四点。
