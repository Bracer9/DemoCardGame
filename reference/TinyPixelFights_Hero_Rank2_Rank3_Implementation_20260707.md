# Tiny Pixel Fights - Hero Rank2 / Rank3 Implementation

更新时间：2026-07-07

## 范围

本次实装根据 `reference/TinyPixelFights_RoleAction_Growth_Synergy_Design_20260629.md`，接入英雄 Rank2 / Rank3 成长，并扩充奖励阶段的“英雄职业训练”。

## 成长流程

- Rank0 -> Rank1：购买英雄职业训练后，选择 1 个基础 Role Action，并固定该英雄后续路线。升阶时回复 50% MaxHp。第一次英雄职业训练消耗 0 BP。
- Rank1 -> Rank2：再次购买英雄职业训练后，不再选择分支，直接沿 Rank1 已固定路线获得属性提升和 Trait 强化。升阶时回复 50% MaxHp。
- Rank2 -> Rank3：再次购买英雄职业训练后，不再选择分支，直接沿已固定路线解锁最终 Role Action，并获得属性提升。升阶时 HP 全回复。

英雄路线通过 `HeroPathRoleActionId` 固定；英雄当前阶级通过 `HeroRank` 保存。

## 主要代码

- `Domain/HeroGrowthDefinitions.cs`：集中定义 16 条英雄路线的 Rank2 属性、Rank3 属性、最终 Role Action 与 Rank3 立绘。
- `Domain/GameState.cs`：新增 `HeroRank` 与 `HeroPathRoleActionId`。
- `Domain/GameEngine.cs`：扩充英雄职业训练、Rank2 / Rank3 升阶、属性计算、Rank3 Role Action 规则效果。
- `Domain/RoleActions.cs`：注册 16 个 Rank3 Role Action。
- `Domain/Traits.cs`：接入可嵌入现有触发点的 Rank2 Trait 强化。
- `Domain/StatusEffects.cs`：新增少量 Rank3 用的临时状态。
- `Api/GameDtos.cs`：向前端暴露 `HeroRank`、`CanHeroRankUpgrade`，并在 Rank3 时切换路线立绘。
- `wwwroot/app.js`：Rank0 继续二选一；Rank1 / Rank2 点击可升级英雄后直接升阶；Rank2 -> Rank3 复用既有升阶演出，并在最终 Role Action 说明中追加当前预测短句。
- `wwwroot/locales/zh.json` / `wwwroot/locales/ja.json`：补齐奖励、Rank3 Role Action、状态、日志与 Trait Rank2 文本。
- `wwwroot/config/ui-assets.json`：Rank3 Role Action 与新增状态复用对应英雄 Trait 图标。

## 立绘

Rank3 立绘来自：

- `assets/rank3_Heroines_Portraits/`

英雄达到 Rank3 后，前端卡面使用对应路线立绘，并复用既有 rank-up transform 演出。

## 设计落地说明

Rank3 效果以能够嵌入当前系统为优先。少数文档中会要求复杂监听或跨系统触发的设计，实装时收束为等价路线身份、较低结构风险的版本。例如 `archive-formula` 目前实现为“归档刻印 + 即时魔法伤害 / 燃烧连锁”，没有新增全局监听“下一次任意 Role Action”的大结构。

## 验证

已通过：

- `node --check wwwroot/app.js`
- `node` JSON parse：`wwwroot/locales/zh.json`、`wwwroot/locales/ja.json`、`wwwroot/config/ui-assets.json`
- `dotnet build`
