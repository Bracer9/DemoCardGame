# Tiny Pixel Fights — UI 美术资产嵌入系统

> 实装状态：A 类战斗事件图标已接入同步战斗演出时间线；B 类状态图标已接入卡面与详情；C 类技能徽记已接入详情及技能／状态结算演出。正式图片可以通过清单逐张替换。

## 1. 为什么这样组织

这套系统把三个容易纠缠的东西拆开：

```text
游戏数据的稳定 ID
        ↓ bindings（语义绑定）
美术资产 ID
        ↓ icons（资源定义）
实际 PNG / WebP 文件
```

例如，后端只需要继续提供状态 ID `burning`。前端配置将它绑定到 `status.burning`，再由资产定义决定使用哪张图片。以后更换画风、文件格式或目录，不需要修改战斗规则、角色数据或 UI 渲染代码。

系统由三部分组成：

| 文件 | 职责 |
|---|---|
| `wwwroot/config/ui-assets.json` | 唯一资产清单：ID、路径、占位符、颜色语义与数据绑定 |
| `wwwroot/ui-assets.js` | 通用图标组件：生成标记、解析路径、监听加载、处理失败回退 |
| `wwwroot/styles.css` | 统一尺寸、形状、色调以及正式图／占位图的切换规则 |

正式资源建议放在：

```text
assets/ui/
├─ events/       # A 类
├─ statuses/     # B 类
└─ skills/       # C 类
```

`Program.cs` 已将整个 `assets` 目录作为 `/assets` 提供，因此这里不需要增加新的后端接口。

## 2. 现在已经嵌入在哪里

### A 类：通用战斗事件

- 攻击预测：物理／魔法伤害、反击。
- 战斗日志：依据日志的 `tone` 自动显示攻击、魔法、治疗、盾牌、状态、死亡等图标。
- 防御阵形按钮及双方共享盾值 HUD。
- 实际结算演出：物理攻击、魔法攻击与反击显示在受击对象上；技能、状态、治疗和死亡显示在对应角色上；共享盾的展开、吸收及消失显示在对应队伍或被保护角色上。
- 结算用 A 类主图标采用约 116 px 的强调规格，并同时显示当前语言对应的事件含义、技能名或状态名；不要求玩家只凭短暂图形猜测结果。
- 联机模式以服务器的结构化日志序号作为统一事件流。行动方提交后与非行动方轮询到结果后都会播放同一顺序，不各自猜测技能结果。

### B 类：Buff／Debuff

- 卡面左上方的状态短标签。
- 鼠标悬停卡片后，右侧状态详情框的大图标。
- 公主祝福、预见、防御阵形与守护等 Aura 也使用同一套状态映射。

### C 类：角色技能徽记

- 八项技能已经在清单注册。
- 鼠标悬停卡片后，左侧技能详情使用对应徽记。
- 技能发动、技能失败、连锁伤害及技能造成的状态会在结算时复用对应徽记，不需要另建第二套 ID。

同一个 `ui-asset-icon` 组件支持 `xs / sm / md / lg` 四种显示规格，因此同一张源图可安全地出现在卡面、日志和详情面板中。

## 3. 你拿到一张正式图以后怎么嵌入

以物理伤害图标为例。

1. 把图片放到：

   ```text
   assets/ui/events/icon_event_physical.png
   ```

2. 打开 `wwwroot/config/ui-assets.json`，找到：

   ```json
   "event.physical": {
     "category": "event",
     "source": null,
     "fallback": "⚔",
     "tone": "physical"
   }
   ```

3. 只修改 `source`：

   ```json
   "source": "events/icon_event_physical.png"
   ```

4. 刷新游戏。所有使用 `event.physical` 的位置会一起换成正式图片。

不需要修改 `app.js`、HTML、C# 或翻译文件。可以逐张加入，尚未完成的图片继续显示占位符。

### 路径规则

- 相对路径以 manifest 的 `basePath`（当前为 `/assets/ui`）为基准。
- 也支持以 `/` 开头的站内绝对路径。
- 路径不存在或图片损坏时，正式图保持不可见，自动显示 `fallback`；不会出现浏览器破图。
- 修改 `basePath` 可以整体迁移 UI 资源，而无需逐项改路径。

## 4. A、B、C 类推荐路径表

### A 类

| 资产 ID | 推荐 `source` |
|---|---|
| `event.physical` | `events/icon_event_physical.png` |
| `event.magical` | `events/icon_event_magical.png` |
| `event.counter` | `events/icon_event_counter.png` |
| `event.skill` | `events/icon_event_skill.png` |
| `event.status-tick` | `events/icon_event_status_tick.png` |
| `event.heal` | `events/icon_event_heal.png` |
| `event.shield` | `events/icon_event_shield.png` |
| `event.death` | `events/icon_event_death.png` |

### B 类

| 资产 ID | 推荐 `source` |
|---|---|
| `status.blessing` | `statuses/status_blessing.png` |
| `status.foresight` | `statuses/status_foresight.png` |
| `status.team-shield` | `statuses/status_team_shield.png` |
| `status.shield-complacency` | `statuses/status_shield_complacency.png` |
| `status.beast-rage` | `statuses/status_beast_rage.png` |
| `status.guard` | `statuses/status_guard.png` |
| `status.burning` | `statuses/status_burning.png` |
| `status.weakness-pending` | `statuses/status_weakness_pending.png` |
| `status.weakness` | `statuses/status_weakness.png` |
| `status.harvest-pending` | `statuses/status_harvest_pending.png` |
| `status.harvest` | `statuses/status_harvest.png` |

### C 类（已预留并可用）

| 资产 ID | 推荐 `source` |
|---|---|
| `skill.saints-prayer` | `skills/skill_saints_prayer.png` |
| `skill.stargazers-aegis` | `skills/skill_stargazers_aegis.png` |
| `skill.spring-harvest` | `skills/skill_spring_harvest.png` |
| `skill.searing-mark` | `skills/skill_searing_mark.png` |
| `skill.weakening-spores` | `skills/skill_weakening_spores.png` |
| `skill.aftershock-axe` | `skills/skill_aftershock_axe.png` |
| `skill.predatory-instinct` | `skills/skill_predatory_instinct.png` |
| `skill.interposing-shield` | `skills/skill_interposing_shield.png` |

## 5. 怎样增加一种新状态或技能

假设以后增加中毒状态，后端状态 ID 为 `poisoned`：

1. 在 `icons` 增加美术定义：

   ```json
   "status.poisoned": {
     "category": "status",
     "source": "statuses/status_poisoned.png",
     "fallback": "✣",
     "tone": "debuff"
   }
   ```

2. 在 `bindings.statuses` 增加：

   ```json
   "poisoned": "status.poisoned"
   ```

完成。所有通用状态 UI 会自动使用新图标。新增技能同理，在 `icons` 和 `bindings.skills` 各加一项即可。

`category` 决定基本外形，`tone` 决定占位期和边缘的颜色语义。正式图片不应该包含日语或中文文字；名称仍由现有 i18n 系统显示。

## 6. 维护约定

- **ID 是契约，文件名不是。** 已上线后尽量不改 `event.physical` 这类资产 ID；换图只改 `source`。
- **一项语义只注册一次。** 同一物理伤害图不要在卡面、日志和预测面板分别配置三次。
- **图片不携带规则。** 图标只负责表达，伤害类型和状态判定仍来自游戏数据。
- **允许渐进替换。** `source: null` 是合法状态，不是错误。
- **不要把 D／E 类动画帧硬塞进图标组件。** 通用／专属 VFX 以后可以沿用相同 manifest 思想，但应由独立动画播放器管理时序、目标和图层。
- **保留透明安全边距。** A／B 图标最终导出 64×64 即可；主体约占画布 68%，避免在 16～36 px 显示时被裁切。

## 7. 提交前检查

- 图片透明背景、没有文字、边框或角色肖像。
- 在卡面 13～16 px 的最小尺寸仍能辨认轮廓。
- 中文和日文模式下含义一致。
- 临时把路径写错一次，确认仍显示占位符而非破图。
- 一种状态同时检查卡面标签和右侧详情框。
- A 类同时检查攻击预测、战斗日志与盾牌 HUD。

生成用的视觉规范与 Prompt 仍集中在 [TinyPixelFights_UI_Asset_Prompts.md](TinyPixelFights_UI_Asset_Prompts.md)，本文件只负责工程嵌入和维护规则。
