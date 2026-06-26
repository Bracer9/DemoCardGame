# Tiny Pixel Fights — 音效制作与接入 Checklist

> 更新日期：2026-06-23  
> 范围：当前双人对战本体的第一套完整音效。未来 Roguelike 内容不计入本表。

## 状态规则

- `[x]`：已有运行用文件，已经在 `audio.json` 注册，并在正确事件上触发。
- `[ ]`：仍缺少专属素材或尚未接入。
- **部分完成**：当前复用其他音效，游戏已有声音，但以后仍建议制作专属版本。
- **P0**：影响战斗因果理解；**P1**：主要手感；**P2**：环境与细节润色。
- 🟦 **用户首批**：最初由用户单独准备并命名用途的素材。
- 🟨 **用户指定**：用户后来明确指定用途的单独素材。
- 🟩 **Library补入**：本次从 `assets/audio_library` 按文件名语义选择并接入的素材。

一项只有同时满足以下条件才能勾选：

1. `assets/audio/` 中存在英文 kebab-case运行文件；若来自素材库，来源路径必须记录在本表。
2. 已导出浏览器运行用 MP3／OGG。
3. `wwwroot/config/audio.json` 中存在 Track 与 Event 映射。
4. 游戏在正确结算时调用稳定 Event ID。
5. 在线行动方和观察方都能在同步演出中听到需要同步的战斗音效。

## A. 当前文件盘点

| 状态 | 文件 | 来源 | 当前用途 |
|---|---|---|---|
| [x] | `burning-trigger.mp3` | 🟦 用户首批 | 炎上赋予成功、炎上伤害生效 |
| [x] | `card-pickup-confirm.mp3` | 🟦 用户首批 | 选取卡牌、结束回合 |
| [x] | `deck-touch.mp3` | 🟩 Kenney Casino `deck-touch.mp3` | 打开洗牌层、等待玩家Touch前播放（来源变更为Kenney Casino） |
| [x] | `physical-hit.mp3` | 🟦 用户首批 | 未被盾吸收的物理命中 |
| [x] | `shield-block.mp3` | 🟦 用户首批 | 物理伤害被共有盾吸收 |
| [x] | `weakness-activated.mp3` | 🟦 用户首批 | 待机衰弱在目标回合开始时正式生效 |
| [x] | `magic-impact.mp3` | 🟩 Helton Yan `Energetic Impact` | 主动魔法命中 |
| [x] | `magic-counter.mp3` | 🟩 Helton Yan `Magic Sparkles` | 魔法反击命中 |
| [x] | `shield-deploy.mp3` | 🟨 用户指定 | 第一层共有盾展开 |
| [x] | `shield-reinforce.mp3` | 🟨 用户指定 | 第二层共有盾强化 |
| [x] | `interposing-shield.mp3` | 🟩 TomMusic `Sword Blocked 3.ogg` | 骑士守护发动 |
| [x] | `aftershock-axe.mp3` | 🟩 TomMusic `chop 3.ogg` | 战斧余波发动 |
| [x] | `character-defeated.mp3` | 🟩 Kenney `impactGlass_heavy_000.ogg` | 卡牌断裂／无法战斗 |
| [x] | `shield-break.mp3` | 🟩 Kenney `impactGlass_heavy_001.ogg` | 共有盾归零破碎 |
| [x] | `attack-confirm.mp3` | 🟩 Kenney UI `click1.ogg` | 确认主动进攻 |
| [x] | `preview-cancel.mp3` | 🟩 Kenney UI `click2.ogg` | 关闭战斗预测 |
| [x] | `shield-command.mp3` | 🟩 Kenney UI `click3.ogg` | 点击防御阵型指令 |
| [x] | `copy-confirm.mp3` | 🟩 Kenney UI `click5.ogg` | 邀请链接复制成功 |
| [x] | `language-toggle.mp3` | 🟩 Kenney UI `switch1.ogg` | 语言切换 |
| [x] | `audio-toggle.mp3` | 🟩 Kenney UI `switch2.ogg` | 音频开关 |
| [x] | `panel-toggle.mp3` | 🟩 Kenney UI `switch3.ogg` | 战况日志开关 |
| [x] | `deck-shuffle.mp3` | 🟩 Kenney Casino `card-shuffle.ogg` | 实际触碰牌堆的确认音（与Touch前提示用途互换） |
| [x] | `card-deal.mp3` | 🟩 Kenney Casino `card-fan-1.ogg` | 发牌阶段开始 |
| [x] | `card-place.mp3` | 🟩 Kenney Casino `card-place-2.ogg` | 每张卡落位 |
| [x] | `ap-spend.mp3` | 🟩 Kenney Casino `chip-lay-1.ogg` | 攻击或防御指令成功支付AP |

### A1. 来源区分

- **Codex 从素材库补入**：上表标为 🟩 的文件，以及下表全部文件。源文件继续保留在 `assets/audio_library/`，运行文件统一放在 `assets/audio/`。

### A2. 本轮由 Codex 从 Library 补入并实际接线

| 运行文件 | 素材库来源 | 实际用途 |
|---|---|---|
| `match-start.mp3` / `rematch-confirm.mp3` / `turn-change.mp3` | 🟩 Kenney Casino 卡牌声 | 发牌结束、再战、行动权交接 |
| `invalid-action.mp3` / `target-lock.mp3` | 🟩 Kenney UI | 无效操作、攻击目标锁定 |
| `no-damage.mp3` | 🟩 TomMusic `Sword Blocked 1` | 未被盾吸收但最终为0伤害 |
| `absolute-damage.mp3` / `beauty-and-beast-chase.mp3` | 🟩 TomMusic `Rock Meteor Throw 1/2` | 怪兽绝对伤害、美女与野兽追击 |
| `skill-damage.mp3` | 🟩 TomMusic `Spell Impact 3` | 无专属素材的技能伤害回退声 |
| `hp-loss.mp3` | 🟩 Kenney Impact `impactPunch_medium_000` | HP实际减少的短促强调 |
| `shield-block-magic.mp3` / `shield-expire.mp3` | 🟩 TomMusic `Ice Freeze 1/2` | 魔法被盾吸收、共有盾自然到期 |
| `shield-hit.mp3` | 🟩 Kenney Impact `impactGlass_light_000` | 盾受击后仍有剩余 |
| `complacency-applied.mp3` / `complacency-consumed.mp3` | 🟩 Kenney UI `switch5/6` | 有恃无恐赋予／消耗 |
| `guard-collateral.mp3` | 🟩 TomMusic `Sword Impact Hit 1` | 骑士代受1点伤害 |
| `weakness-pending.mp3` / `weakness-failed.mp3` | 🟩 TomMusic `Waterspray 1` / Kenney UI `switch7` | 衰弱孢子附着／失败 |
| `sowing.mp3` / `harvest-activated.mp3` | 🟩 Kenney Casino `chip-lay-2` / `chips-stack-2` | 播种／收获 |
| `blessing-heal.mp3` | 🟩 Helton Yan `MAGAngl_BUFF-Simple Heal_HY_PC-001.wav` | 圣女祝福回复；同批多目标播放节流 |
| `foresight-proc.mp3` / `magic-bonus.mp3` | 🟩 TomMusic `Sword Parry 1` / `Firebuff 1` | 预见减伤／魔法增伤 |
| `beast-rage.mp3` | 🟩 TomMusic `Sword Unsheath 2` | 野兽之怒赋予 |
| `room-created.mp3` / `room-joined.mp3` / `opponent-joined.mp3` / `network-error.mp3` | 🟩 Kenney UI `switch8/9/10/13` | 在线房间反馈 |
| `victory-sting.mp3` / `defeat-sting.mp3` / `draw-sting.mp3` | 🟩 Kenney Impact | 胜利／失败／平局 |
| `result-panel.mp3` | 🟩 Kenney Casino `card-fan-2` | 结果面板展开 |
| `buff-applied.mp3` / `debuff-applied.mp3` / `status-expired.mp3` | 🟩 TomMusic / Kenney UI | 已注册的未来通用回退声；当前尚未统一触发 |

## B. BGM与全局流程

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 对局战斗BGM | P0 | `game.start` / `game.restart` | `Ashen Banner.mp3` | 已循环播放并淡入 |
| [ ] 标题／房间等待BGM | P2 | `bgm.title` | `title-ambient.mp3` | 可比战斗BGM安静，进入对局时淡出 |
| [x] 对局开始短促提示 | P1 | `game.match-start` | `match-start.mp3` | 🟩 发牌完成后播放 |
| [x] 再战确认 | P2 | `game.restart` | `rematch-confirm.mp3` | 🟩 与BGM重启同时触发 |

## C. UI、卡牌与发牌

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 选中／拿起可行动卡牌 | P1 | `ui.card-select` | `card-pickup-confirm.mp3` | 点击与开始拖拽均已接入 |
| [x] 点击结束回合 | P1 | `ui.turn-end` | `card-pickup-confirm.mp3` | 当前与拿牌共用 |
| [x] 触碰牌堆 | P1 | `ui.deck-touch` | `deck-shuffle.mp3` | 🟩 在线一方点击后双方进入发牌；V1.16与Touch前提示音互换 |
| [x] Touch前提示／洗牌 | P1 | `ui.shuffle` | `deck-touch.mp3` | 🟦 打开洗牌层、等待玩家Touch前播放；V1.16与触碰牌堆音互换 |
| [x] 发牌展开 | P1 | `ui.deal` | `card-deal.mp3` | 🟩 发牌阶段开始时播放 |
| [x] 单张卡牌落位 | P1 | `ui.card-place` | `card-place.mp3` | 🟩 八张牌依次出现时逐张播放，音量已压低 |
| [x] 确认主动进攻 | P1 | `ui.attack-confirm` | `attack-confirm.mp3` | 🟩 按下预测面板的确认按钮时播放 |
| [x] 取消／关闭战斗预测 | P2 | `ui.preview-cancel` | `preview-cancel.mp3` | 🟩 点击关闭或Escape时播放 |
| [x] 无效操作／非法选取 | P1 | `ui.invalid-action` | `invalid-action.mp3` | 🟩 无法行动、未选攻击者及预测失败 |
| [x] AP支付 | P2 | `ui.ap-spend` | `ap-spend.mp3` | 🟩 攻击或防御指令由服务器确认成功后播放一次 |
| [x] 防御阵型按钮确认 | P1 | `ui.shield-command` | `shield-command.mp3` | 🟩 按钮操作声，区别于盾本身展开声 |
| [x] 语言切换 | P2 | `ui.language-toggle` | `language-toggle.mp3` | 🟩 封面UI使用 |
| [x] 音频开／关 | P2 | `ui.audio-toggle` | `audio-toggle.mp3` | 🟩 关闭前／重新开启后播放 |
| [x] 战况日志开／关 | P2 | `ui.panel-toggle` | `panel-toggle.mp3` | 🟩 已接入日志面板 |

## D. 回合与行动权

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 行动权交接／新回合 | P0 | `turn.change` | `turn-change.mp3` | 🟩 本地与在线状态同步均已接入 |
| [ ] 成为我方行动 | P1 | `turn.yours` | `your-turn.mp3` | 在线非行动方尤其需要；可与交接声组合 |
| [ ] 状态回合开始结算 | P1 | `turn.status-resolution` | `status-resolution.mp3` | 炎上、治疗、衰弱等开始前的总提示，可选 |
| [ ] 无合法行动自动结束 | P2 | `turn.auto-end` | `auto-end-turn.mp3` | 该机制实装后再接入 |

## E. 通用攻击、伤害与死亡

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 主动物理攻击命中 | P0 | `combat.physical-hit` | `physical-hit.mp3` | 实际造成伤害且未被共有盾吸收 |
| [x] 物理反击命中 | P0 | `combat.physical-hit` | `physical-hit.mp3` | 当前复用主动物理命中 |
| [x] 主动魔法攻击发动 | P0 | `combat.magic-active` | `fireball.mp3` | 🟨 已按用户指定替换旧素材 |
| [x] 魔法命中／爆发 | P0 | `combat.magic-impact` | `magic-impact.mp3` | 🟩 与施法声分开，实际命中时播放 |
| [x] 魔法反击 | P1 | `combat.magic-counter` | `magic-counter.mp3` | 🟩 魔法反击实际命中时播放 |
| [x] 攻击造成0伤害 | P0 | `combat.no-damage` | `no-damage.mp3` | 🟩 预见、衰弱或数值归零且没有盾吸收时 |
| [x] 绝对伤害 | P0 | `combat.absolute-damage` | `absolute-damage.mp3` | 🟩 怪兽追击时与专属追击声组合 |
| [x] 通用技能伤害 | P1 | `combat.skill-damage` | `skill-damage.mp3` | 🟩 没有专属音效的技能伤害回退 |
| [x] HP减少／受创强调 | P1 | `combat.hp-loss` | `hp-loss.mp3` | 🟩 与伤害数字同步，短时间内节流 |
| [x] 角色无法战斗／卡牌断裂 | P0 | `combat.character-defeated` | `character-defeated.mp3` | 🟩 与死亡Event图标和切断动画同步 |
| [x] 红色锁定线出现 | P2 | `combat.target-lock` | `target-lock.mp3` | 🟩 确认进攻后连接攻击者与目标 |

## F. 共有盾与骑士守护

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 第一层共有盾展开 | P0 | `shield.deploy` | `shield-deploy.mp3` | 🟩 2 AP→盾2，与蓝色弧形形成同步 |
| [x] 第二层盾强化 | P0 | `shield.reinforce` | `shield-reinforce.mp3` | 🟩 追加1 AP→盾4 |
| [x] 物理伤害被共有盾吸收 | P0 | `combat.shield-block` | `shield-block.mp3` | 已接入 |
| [x] 魔法／技能／状态伤害被盾吸收 | P0 | `combat.shield-block-magic` | `shield-block-magic.mp3` | 🟩 与物理格挡区分 |
| [x] 盾受到冲击但仍有剩余 | P1 | `shield.hit` | `shield-hit.mp3` | 🟩 与对应吸收声组合播放 |
| [x] 共有盾完全破碎 | P0 | `shield.break` | `shield-break.mp3` | 🟩 与整层碎裂粒子同步 |
| [x] 共有盾自然到期 | P1 | `shield.expire` | `shield-expire.mp3` | 🟩 比破碎更轻 |
| [x] 有恃无恐赋予 | P1 | `status.complacency-applied` | `complacency-applied.mp3` | 🟩 展开／强化共有盾后播放 |
| [x] 有恃无恐在反击后消耗 | P2 | `status.complacency-consumed` | `complacency-consumed.mp3` | 🟩 反击减伤结算时播放 |
| [x] 骑士守护发动 | P0 | `skill.guard-trigger` | `interposing-shield.mp3` | 🟩 已从共有盾格挡声拆为专属声音 |
| [x] 骑士代受1点伤害 | P1 | `skill.guard-collateral` | `guard-collateral.mp3` | 🟩 与守护介入声分成前后两段 |

## G. 状态、Buff与恢复

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 炎上赋予成功 | P0 | `status.burning-applied` | `burning-trigger.mp3` | 已接入 |
| [x] 炎上伤害生效 | P0 | `status.burning-tick` | `burning-trigger.mp3` | 当前与赋予共用 |
| [x] 衰弱孢子附着／待生效 | P1 | `status.weakness-pending` | `weakness-pending.mp3` | 🟩 普通目标获得待生效状态 |
| [x] 衰弱正式生效 | P0 | `status.weakness-activated` | `weakness-activated.mp3` | 所有目标统一在自己的下回合开始时生效 |
| [x] 衰弱赋予失败 | P1 | `skill.weakness-failed` | `weakness-failed.mp3` | 🟩 无伤害时50%失败，与失败图标同步 |
| [x] 丰收／攻击强化 | P1 | `status.harvest-activated` | `harvest-activated.mp3` | 🟩 農民下回合攻击+2 |
| [x] 春播／待收获 | P1 | `status.sowing` | `sowing.mp3` | 🟩 農民首次进攻后的播种提示 |
| [x] 公主祝福治疗 | P0 | `status.blessing-heal` | `blessing-heal.mp3` | 🟩 同批多目标恢复采用450ms节流 |
| [x] 占卜师预见减伤成功 | P0 | `status.foresight-proc` | `foresight-proc.mp3` | 🟩 概率减伤真正发生时播放 |
| [x] 占卜师魔法伤害+1 | P1 | `status.magic-bonus` | `magic-bonus.mp3` | 🟩 结算日志出现魔法增伤时播放 |
| [x] 野兽之怒触发 | P0 | `status.beast-rage` | `beast-rage.mp3` | 🟩 公主阵亡后怪兽基础攻击+2 |
| [ ] 普通Buff赋予回退音 | P2 | `status.buff-applied` | `buff-applied.mp3` | 未来新Buff无专属音效时使用 |
| [ ] 普通Debuff赋予回退音 | P2 | `status.debuff-applied` | `debuff-applied.mp3` | 未来新Debuff无专属音效时使用 |
| [x] 攻击Buff被衰弱孢子驱散 | P1 | `status.expired` | `status-expired.mp3` | 🟩 与被移除Buff图标及×标记同步；自然到期仍不单独播放 |

## H. 八名角色专属技能

| Checklist | 优先级 | Skill / Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 姫「聖女の祈り」 | P0 | `status.blessing-heal` | `blessing-heal.mp3` | 🟩 通过祝福实际回复事件完成 |
| [x] 占い師「星読みの加護」 | P0 | `status.foresight-proc` / `status.magic-bonus` | `foresight-proc.mp3` / `magic-bonus.mp3` | 🟩 两种实际效果分别发声 |
| [x] 農民「春蒔き・収穫」 | P1 | `status.sowing` / `status.harvest-activated` | `sowing.mp3` / `harvest-activated.mp3` | 🟩 播种与收获使用两个变体 |
| [x] 魔法使い「灼熱の刻印」 | P0 | `skill.searing-mark`相关状态事件 | `burning-trigger.mp3` | 通过炎上赋予／生效事件完成 |
| [x] ドルイド「衰弱の胞子」 | P0 | `status.weakness-pending` / `status.weakness-activated` / `skill.weakness-failed` | 三组对应运行文件 | 🟩 附着、正式生效、失败均已区分 |
| [x] バーバリアン「戦斧の余波」 | P0 | `skill.aftershock-axe` | `aftershock-axe.mp3` | 🟩 ≥3伤害后波及邻居时 |
| [x] モンスター「美女と野獣」绝对追击 | P0 | `skill.predatory-instinct` | `beauty-and-beast-chase.mp3` | 🟩 与绝对伤害声组合 |
| [x] モンスター「野獣の怒り」 | P0 | `status.beast-rage` | `beast-rage.mp3` | 🟩 公主阵亡后的二阶段转换 |
| [x] 騎士「身代わりの盾」 | P0 | `skill.guard-trigger` | `interposing-shield.mp3` | 🟩 守护介入时播放专属格挡声 |

## I. 在线房间与网络状态

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 创建房间成功 | P2 | `online.room-created` | `room-created.mp3` | 🟩 仅创建者本地播放 |
| [x] 加入房间成功 | P2 | `online.room-joined` | `room-joined.mp3` | 🟩 加入方本地播放 |
| [x] 对手加入／房间准备完成 | P1 | `online.opponent-joined` | `opponent-joined.mp3` | 🟩 房间人数1→2时播放 |
| [x] 邀请链接复制成功 | P2 | `online.link-copied` | `copy-confirm.mp3` | 🟩 短促UI确认 |
| [ ] 对手暂时断线 | P1 | `online.opponent-disconnected` | `opponent-disconnected.mp3` | 需要先有明确断线状态UI |
| [ ] 对手重新连接 | P1 | `online.opponent-reconnected` | `opponent-reconnected.mp3` | 不应与房间首次加入混淆 |
| [x] 创建／加入房间请求失败 | P1 | `online.request-failed` | `network-error.mp3` | 🟩 只在用户主动请求失败时播放 |

## J. 胜负结果

| Checklist | 优先级 | Event ID | 建议文件名 | 说明 |
|---|:---:|---|---|---|
| [x] 我方胜利 | P0 | `game.victory` | `victory-sting.mp3` | 🟩 按viewer视角独立判断 |
| [x] 我方失败 | P0 | `game.defeat` | `defeat-sting.mp3` | 🟩 按viewer视角独立判断 |
| [x] 平局 | P1 | `game.draw` | `draw-sting.mp3` | 🟩 双方各自播放平局声 |
| [x] 结果面板出现 | P2 | `game.result-panel` | `result-panel.mp3` | 🟩 在胜负提示后180ms播放 |

## K. 推荐制作顺序

### 第一批：战斗可读性 P0

- [x] `turn-change.mp3` 🟩
- [x] `magic-impact.mp3` 🟩
- [x] `no-damage.mp3` 🟩
- [x] `absolute-damage.mp3` 🟩
- [x] `character-defeated.mp3` 🟩
- [x] `shield-deploy.mp3` 🟩
- [x] `shield-reinforce.mp3` 🟩
- [x] `shield-break.mp3` 🟩
- [x] `blessing-heal.mp3` 🟩
- [x] `foresight-proc.mp3` 🟩
- [x] `aftershock-axe.mp3` 🟩
- [x] `beauty-and-beast-chase.mp3` 🟩
- [x] `beast-rage.mp3` 🟩
- [x] `interposing-shield.mp3` 🟩
- [x] `victory-sting.mp3` 🟩
- [x] `defeat-sting.mp3` 🟩

### 第二批：操作手感 P1

- [x] `deck-shuffle.mp3` 🟩
- [x] `card-deal.mp3`／`card-place.mp3` 🟩
- [x] `attack-confirm.mp3` 🟩
- [x] `invalid-action.mp3` 🟩
- [ ] `your-turn.mp3`
- [x] `weakness-pending.mp3` 🟩
- [x] `weakness-failed.mp3` 🟩
- [x] `sowing.mp3` 🟩
- [x] `harvest-activated.mp3` 🟩
- [x] `magic-bonus.mp3` 🟩
- [x] 在线创建、加入与对手加入提示 🟩
- [ ] 在线断线与重连提示（尚无明确同步事件）

### 第三批：润色 P2

- [x] 面板、语言、日志、AP支付等轻量UI音 🟩
- [ ] 标题环境BGM
- [ ] 状态解除与通用Buff／Debuff回退音
- [x] 红色锁定线与结果面板辅助音 🟩

## L. 接入位置

- 音轨和事件映射：`wwwroot/config/audio.json`
- 音频播放系统：`wwwroot/audio.js`
- UI与同步战斗演出触发：`wwwroot/app.js`
- 完整维护说明：`reference/TinyPixelFights_Audio.md`
- 自动校验：`tests/audio-director.test.js`

新增素材后不要仅因为“文件已经放进文件夹”就勾选；必须确认配置、触发点和在线双方表现都已经完成。
