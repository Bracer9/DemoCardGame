# Tiny Pixel Fights — Character Zone / 队伍人数变化设计

更新时间：2026-06-25

## 目的

之后游戏会出现队伍成员增减、复活、临时离队、召唤、墓地、后备席等机制。角色是否仍然占据战场位置，不能再只靠 `CurrentHp > 0` 或前端 `display:none` 判断。

这次改动把“角色是否仍在当前战场/手牌队列中”抽象为后端规则层概念。

## 当前实现

后端 `CharacterState` 新增通用区域：

```csharp
public CharacterZone Zone { get; set; } = CharacterZone.Battlefield;
public bool IsInBattle => Zone == CharacterZone.Battlefield;
public bool IsAlive => IsInBattle && CurrentHp > 0;
```

当前已有 zone：

| Zone | 含义 |
|---|---|
| `Battlefield` | 角色仍在当前战场/手牌队列中 |
| `Defeated` | 角色已经阵亡并离开战场，但角色实例仍保留在对局状态中 |

玩家状态提供：

```csharp
ActiveCharacterCount
```

用于实时判断当前队伍中仍在战场上的人数。

## 为什么不直接删除角色

死亡后不从 `PlayerState.Characters` 里删除角色实例，原因是：

- 日志和动画仍需要定位刚死亡的角色。
- 在线同步需要双方从同一份完整状态得到一致结果。
- 之后可能出现复活、墓地、战后结算、永久成长、剧情检查。
- 角色永久身份不应因为一次战斗离场而丢失。

因此：

- 规则层保留角色实例。
- 战场显示层根据 `IsInBattle` 决定是否显示。
- 死亡演出期间，前端可以临时保留刚死亡的卡片，演出结束后再移除。

## 当前死亡流程

1. 角色 HP 归零。
2. `ResolveDefeats` 检测 `IsInBattle && CurrentHp <= 0`。
3. 角色进入 `CharacterZone.Defeated`。
4. 清空状态，标记 `DefeatLogged`。
5. 写入 `log.defeated`，并附带非显示用 `characterId`，供前端精确定位动画目标。
6. DTO 仍传出该角色，但标记：
   - `isInBattle: false`
   - `zone: "Defeated"`
7. 前端如果发现这是新的死亡 log，会临时显示该卡，播放死亡切割动画。
8. 动画结束后重新 render，该角色不再出现在手牌/敌方队列中。
9. 剩余卡片自动重新居中/对称排布。

## 前端显示规则

默认显示：

```text
character.isInBattle === true
```

临时显示：

```text
character.id 在 pendingDefeatAnimationIds 中
```

这样可以保证：

- 死亡不是瞬间消失。
- 死亡动画只播放一次。
- 播完后卡片从队伍中移除。
- 我方扇形和敌方平铺都会根据剩余人数重新对称。

## 之后扩展建议

如果之后加入更多队伍区域，不要新增类似 `IsBenchCharacter`、`IsSummonedCharacter` 这种过窄字段。优先扩展 `CharacterZone` 或引入更完整的 zone/slot 模型。

可能的未来 zone：

| Zone | 用途 |
|---|---|
| `Bench` | 后备席 |
| `Graveyard` | 墓地，可被复活或检索 |
| `SummonQueue` | 即将入场 |
| `Removed` | 本场战斗完全移除 |
| `StoryLocked` | 剧情导致暂不可用 |

重要原则：

- `CharacterDefinition` 仍然只是角色模板。
- `CharacterState` 保存局内状态和当前 zone。
- 永久爬塔角色、剧情角色、战斗角色之后应分层，不要混在一个字段里。
- UI 不应自行决定角色是否离场，只展示后端给出的 zone 结果。
