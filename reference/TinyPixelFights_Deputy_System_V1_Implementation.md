# Tiny Pixel Fights — 副官系统 V1 实装记录

更新时间：2026-07-16

## 本次实装范围

- Rank2 士兵可以在己方回合任命为副官。
- 每名英雄最多拥有 1 名副官。
- 士兵成为副官后进入 `Deputy` zone，不再显示为手牌 / 战场卡，不再攻击、承伤或使用 Role Action。
- 副官绑定不可撤销；V1 为本场战斗内永久。
- Rank2 士兵成为副官后，Rank1 光环继续生效；光环来源跟随宿主英雄，宿主存活且在战场时有效。
- 新增弄臣 / `deputy-jester`：宿主攻击 +1；每己方 turn 1 次，宿主敌方主动行动结算后，主目标根据攻击类型获得力竭或磨损。
- 副官定义补充通用 `BuildTags`，供 AI 招募、遗物与副官宿主的轻量构筑评分复用。
- AI 队伍满员时会尝试任命 Rank2 士兵为副官；只排除攻击类型完全不匹配的组合，其余合法组合按 Build Tags 加权随机选择。

## 任命条件

士兵必须满足：

- `CardType.Soldier`
- Rank2
- 我方、存活、在战场
- 尚未成为副官
- 本 turn 未主动攻击、未使用 Role Action
- 当前有合法的我方存活英雄，且该英雄没有副官

任命不消耗 AP / BP，也不消耗攻击权。

## 副官效果

| 副官 | 来源士兵 | 基础加成 | V1 被动 |
|---|---|---:|---|
| 战地医护 | Cleric | 魔防 +2 | 每己方 turn 1 次，宿主职业行动成功治疗或净化后，目标获得护咒；若目标低于半血则护咒 2 turn。 |
| 盾阵辅佐 | Shieldmaiden | 物防 +2 | 每己方 turn 1 次，宿主强化共有盾后，HP比例最低的我方角色获得坚守。 |
| 决斗副手 | Duelist | 物攻 +2 | 每己方 turn 1 次，宿主主动攻击对敌人造成 HP 伤害后获得强攻；本次攻击后若目标仍存活，追加 2 点绝对伤害。 |
| 秘术参谋 | Arcanist | 魔攻 +2 | 每己方 turn 1 次，宿主造成魔法伤害，或通过职业行动施加燃烧 / 空虚 / 力竭 / 磨损后，宿主获得魔涌，并可以再次主动攻击。 |
| 幻戏辅佐 | Jester | 攻击 +1 | 每己方 turn 1 次，宿主主动攻击敌人，或以敌人为目标的职业行动成功结算后，若主目标仍存活，根据目标攻击类型赋予 2 turn 力竭或磨损。 |

## 实装入口

- 副官定义与 Build Tags：`Domain/DeputyDefinitions.cs`
- AI 构筑画像：`Services/AiBuildProfile.cs`
- AI 加权选择与自动任命：`Services/SimpleAiService.cs`
- 角色状态：`CharacterState.DeputySoldierId / DeputyHostHeroId / DeputyEffectId`
- 副官 zone：`CharacterZone.Deputy`
- 任命规则：`GameEngine.AssignDeputy`
- DTO：`DeputyView / DeputyPreviewView`
- API：
  - `/api/game/deputy/assign`
  - `/api/online/game/deputy/assign`
- 前端：
  - Rank2 士兵 Inspector 显示“任命副官”
  - 进入副官目标选择模式后，高亮合法英雄
  - 任命后英雄卡显示副官徽章，英雄 Inspector 显示副官信息
- 弄臣接入测试：`tests/jester-soldier.test.js`

## V1 暂不做

- 副官解除 / 替换
- 逐英雄、逐士兵的专属相性表
- 副官专属 Role Action
- 副官拖拽选择
- 跨战斗持久化保存
