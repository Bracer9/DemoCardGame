# Tiny Pixel Fights — 音频资产与事件映射

> 更新日期：2026-06-23  
> 实装状态：BGM、用户原始素材、用户指定的 `fireball`，以及从三个 Library 补入的战斗／状态／UI／联网／结算音效，已接入本地与在线共用的事件系统。

完整的已完成／待制作音效清单见 [`assets/audio/SFX_CHECKLIST.md`](../assets/audio/SFX_CHECKLIST.md)。

## 2026-06-23 音频控制 UI 更新

- 右上角 `AUDIO` 现在展开为中世纪暗黑风下拉菜单。
- BGM 与音效分开控制：BGM 控制 `bgm` bus；音效控制 `sfx` 与 `ui` bus。
- 两组都支持独立 ON/OFF 与 0–100% 音量滑块。
- 玩家设置保存在浏览器 localStorage，不影响服务器同步，也不写入 C# 战斗规则。
- 运行时音量计算为：`manifest bus volume × track volume × user group volume`。

## 1. 架构

```text
游戏／UI 的稳定事件 ID
        ↓ wwwroot/config/audio.json 中的 events
音轨 ID、音量与总线
        ↓ tracks
assets 中的实际音频文件
```

| 文件／目录 | 职责 |
|---|---|
| `assets/audio/` | 音效文件；英文 kebab-case WAV 原始文件与浏览器运行用 MP3 |
| `assets/Ashen Banner.mp3` | 当前战斗 BGM |
| `wwwroot/config/audio.json` | 音轨、总线、音量及事件映射的唯一清单 |
| `wwwroot/audio.js` | 加载、浏览器音频解锁、BGM 循环／淡入及可重叠 SFX 播放 |
| `wwwroot/app.js` | 在 UI 操作和同步战斗事件发生时，只触发稳定事件 ID |

音频文件路径不会写进 C# 战斗规则。在线模式也不传输音频：服务器同步结构化战斗事件，两端各自依据同一事件播放本地资源。

## 2. 总线

| Bus | 当前总音量 | 用途 |
|---|---:|---|
| `bgm` | 0.42 | 循环背景音乐 |
| `sfx` | 0.85 | 攻击、技能、状态和护盾反馈 |
| `ui` | 0.70 | 选牌、确认、发牌等操作反馈 |

实际音量为：`Bus 音量 × Track volume`。

## 3. 当前音轨

| Track ID | 文件 | Bus | Track volume | 用途 |
|---|---|---|---:|---|
| `bgm.battle` | `assets/Ashen Banner.mp3` | bgm | 1.00 | 对局 BGM，循环并淡入 |
| `sfx.magic-attack` | `assets/audio/fireball.mp3` | sfx | 0.78 | 主动魔法攻击；用户指定替换旧素材 |
| `sfx.weakness-activated` | `assets/audio/weakness-activated.mp3` | sfx | 0.78 | 所有目标的待机衰弱在其下回合开始时正式生效 |
| `sfx.card-pickup-confirm` | `assets/audio/card-pickup-confirm.mp3` | ui | 0.82 | 选中可行动卡牌、确认结束回合 |
| `sfx.deck-touch` | `assets/audio/deck-touch.mp3` | ui | 0.90 | 打开洗牌层、等待玩家Touch前播放 |
| `sfx.physical-hit` | `assets/audio/physical-hit.mp3` | sfx | 0.74 | 未被共享盾／守护拦截并实际造成伤害的物理命中 |
| `sfx.shield-block` | `assets/audio/shield-block.mp3` | sfx | 0.75 | 物理伤害被共享盾吸收，或骑士守护介入 |
| `sfx.burning-trigger` | `assets/audio/burning-trigger.mp3` | sfx | 0.76 | 炎上赋予成功及炎上伤害生效；共用同一素材 |
| `sfx.magic-impact` | Helton Yan `Energetic Impact` | sfx | 0.76 | 魔法主动攻击命中 |
| `sfx.magic-counter` | Helton Yan `Magic Sparkles` | sfx | 0.72 | 魔法反击命中 |
| `sfx.character-defeated` | `assets/audio/character-defeated.mp3` | sfx | 0.90 | 卡牌断裂／无法战斗；Kenney素材库 |
| `sfx.shield-deploy` | `assets/audio/shield-deploy.mp3` | sfx | 0.72 | 第一层共有盾展开 |
| `sfx.shield-reinforce` | `assets/audio/shield-reinforce.mp3` | sfx | 0.82 | 第二层共有盾强化 |
| `sfx.shield-break` | `assets/audio/shield-break.mp3` | sfx | 0.88 | 共有盾破碎；Kenney素材库 |
| `sfx.aftershock-axe` | `assets/audio/aftershock-axe.mp3` | sfx | 0.86 | 战斧余波；TomMusic素材库 |
| `sfx.interposing-shield` | `assets/audio/interposing-shield.mp3` | sfx | 0.82 | 骑士守护；TomMusic素材库 |
| `sfx.attack-confirm` | `assets/audio/attack-confirm.mp3` | ui | 0.80 | 确认进攻；Kenney UI素材库 |
| `sfx.preview-cancel` | `assets/audio/preview-cancel.mp3` | ui | 0.70 | 关闭预测；Kenney UI素材库 |
| `sfx.shield-command` | `assets/audio/shield-command.mp3` | ui | 0.82 | 防御阵型按钮；Kenney UI素材库 |
| `sfx.language-toggle` | `assets/audio/language-toggle.mp3` | ui | 0.65 | 语言切换；Kenney UI素材库 |
| `sfx.audio-toggle` | `assets/audio/audio-toggle.mp3` | ui | 0.65 | 音频开关；Kenney UI素材库 |
| `sfx.panel-toggle` | `assets/audio/panel-toggle.mp3` | ui | 0.62 | 日志面板；Kenney UI素材库 |
| `sfx.copy-confirm` | `assets/audio/copy-confirm.mp3` | ui | 0.72 | 复制邀请链接；Kenney UI素材库 |
| `sfx.deck-shuffle` | `assets/audio/deck-shuffle.mp3` | ui | 0.72 | 实际触碰牌堆的确认音；Kenney Casino素材库，V1.16与Touch前提示用途互换 |
| `sfx.card-deal` | `assets/audio/card-deal.mp3` | ui | 0.70 | 发牌展开；Kenney Casino素材库 |
| `sfx.card-place` | `assets/audio/card-place.mp3` | ui | 0.42 | 单张卡牌落位；Kenney Casino素材库 |
| `sfx.ap-spend` | `assets/audio/ap-spend.mp3` | ui | 0.62 | 成功支付AP；Kenney Casino素材库 |
| `sfx.blessing-heal` | Helton Yan `Healing Gusts` | sfx | 0.60 | 圣女祝福回复 |
| `sfx.foresight-proc` | `assets/audio/foresight-proc.mp3` | sfx | 0.66 | 预见减伤成功；TomMusic素材库 |
| `sfx.magic-bonus` | `assets/audio/magic-bonus.mp3` | sfx | 0.62 | 占卜师魔法增伤；TomMusic素材库 |
| `sfx.beast-rage` | `assets/audio/beast-rage.mp3` | sfx | 0.84 | 野兽之怒赋予；TomMusic素材库 |
| `sfx.buff-applied` | `assets/audio/buff-applied.mp3` | sfx | 0.55 | 通用Buff赋予回退声 |
| `sfx.debuff-applied` | `assets/audio/debuff-applied.mp3` | sfx | 0.55 | 通用Debuff赋予回退声 |
| `sfx.status-expired` | `assets/audio/status-expired.mp3` | sfx | 0.48 | Buff被驱散/到期 |
| `sfx.beauty-and-beast-chase` | `assets/audio/beauty-and-beast-chase.mp3` | sfx | 0.86 | 美女与野兽绝对追击；TomMusic素材库 |
| `sfx.room-created` | `assets/audio/room-created.mp3` | ui | 0.65 | 在线创建房间；Kenney UI素材库 |
| `sfx.room-joined` | `assets/audio/room-joined.mp3` | ui | 0.65 | 在线加入房间；Kenney UI素材库 |
| `sfx.opponent-joined` | `assets/audio/opponent-joined.mp3` | ui | 0.72 | 对手加入；Kenney UI素材库 |
| `sfx.network-error` | `assets/audio/network-error.mp3` | ui | 0.68 | 在线请求失败；Kenney UI素材库 |
| `sfx.victory-sting` | `assets/audio/victory-sting.mp3` | sfx | 0.90 | 我方胜利；Kenney Impact素材库 |
| `sfx.defeat-sting` | `assets/audio/defeat-sting.mp3` | sfx | 0.78 | 我方失败；Kenney Impact素材库 |
| `sfx.draw-sting` | `assets/audio/draw-sting.mp3` | sfx | 0.76 | 平局；Kenney Impact素材库 |
| `sfx.result-panel` | `assets/audio/result-panel.mp3` | ui | 0.58 | 结果面板展开；Kenney Casino素材库 |

## 4. 事件映射

| Event ID | Track ID | 触发条件 |
|---|---|---|
| `game.start` | `bgm.battle` | 本地或在线对局开始 |
| `game.restart` | `bgm.battle` | 再战／新对局 |
| `ui.card-select` | `sfx.card-pickup-confirm` | 点击或开始拖动一张可行动卡牌；取消选择不播放 |
| `ui.turn-end` | `sfx.card-pickup-confirm` | 合法点击结束回合 |
| `ui.deck-touch` | `sfx.deck-shuffle` | 任意一方触碰牌堆；在线双方随后都会进入发牌演出 |
| `combat.magic-active` | `sfx.magic-attack` | 主动攻击的伤害类型为魔法 |
| `combat.physical-hit` | `sfx.physical-hit` | 主动、反击或技能物理伤害实际命中，且该次攻击没有被共享盾吸收 |
| `combat.shield-block` | `sfx.shield-block` | 物理伤害被共享盾吸收，或骑士守护介入 |
| `status.weakness-activated` | `sfx.weakness-activated` | `log.weaknessActivated` 进入同步演出时间线 |
| `status.burning-applied` | `sfx.burning-trigger` | 炎上成功附加 |
| `status.burning-tick` | `sfx.burning-trigger` | 炎上在回合开始造成伤害 |
| `combat.magic-impact` | `sfx.magic-impact` | 主动魔法攻击实际命中且未被盾吸收 |
| `combat.magic-counter` | `sfx.magic-counter` | 魔法反击实际命中且未被盾吸收 |
| `combat.character-defeated` | `sfx.character-defeated` | 死亡图标及卡牌断裂演出 |
| `shield.deploy` | `sfx.shield-deploy` | 第一层共有盾展开 |
| `shield.reinforce` | `sfx.shield-reinforce` | 第二层共有盾强化 |
| `shield.break` | `sfx.shield-break` | 共有盾归零并破碎 |
| `skill.aftershock-axe` | `sfx.aftershock-axe` | 战斧余波实际触发 |
| `skill.guard-trigger` | `sfx.interposing-shield` | 骑士守护实际介入 |
| `ui.attack-confirm` | `sfx.attack-confirm` | 确认主动进攻 |
| `ui.preview-cancel` | `sfx.preview-cancel` | 点击关闭或Escape关闭预测 |
| `ui.shield-command` | `sfx.shield-command` | 点击防御阵型指令 |
| `ui.language-toggle` | `sfx.language-toggle` | 切换中／日文 |
| `ui.audio-toggle` | `sfx.audio-toggle` | 音频开关 |
| `ui.panel-toggle` | `sfx.panel-toggle` | 战况日志开关 |
| `online.link-copied` | `sfx.copy-confirm` | 邀请链接复制成功 |
| `ui.shuffle` | `sfx.deck-touch` | 洗牌层打开、等待玩家Touch前播放 |
| `ui.deal` | `sfx.card-deal` | 发牌阶段开始 |
| `ui.card-place` | `sfx.card-place` | 每张卡的落位动画完成 |
| `ui.ap-spend` | `sfx.ap-spend` | 攻击／防御指令经服务器确认并扣除AP |

## 5. 添加或替换音效

### 2026-06-23 Library 扩充

- `fireball.mp3` 是主动魔法攻击的正式发动声；旧的 `active-magic-attack` 运行文件已清理，不再参与事件映射。
- 发牌流程现包含洗牌、发牌展开、逐张落位与发牌完成提示。
- 战斗现包含目标锁定、0伤害、绝对伤害、通用技能伤害、HP减少、魔法／物理命中、盾受击／破碎／到期与角色阵亡。
- 角色机制现覆盖祝福治疗、预见、魔法增伤、播种、收获、衰弱附着／生效／失败、战斧余波、美女与野兽、野兽之怒、骑士守护及代受伤害。
- V1.16起，衰弱孢子成功驱散攻击强化Buff时触发 `status.expired`，与被移除状态图标同步播放。
- 在线流程现包含创建房间、加入房间、对手加入及用户主动请求失败；胜利、失败、平局与结果面板也按各自玩家视角播放。
- 具体文件来源与“用户素材／Codex 从 Library 补入”的区分，以 [`assets/audio/SFX_CHECKLIST.md`](../assets/audio/SFX_CHECKLIST.md) 的 A1、A2 为准；运行时唯一映射仍以 `wwwroot/config/audio.json` 为准。


### 替换现有音效

运行时只保留 `assets/audio/` 下的英文 kebab-case MP3 文件；素材库原始文件可以继续放在 `assets/audio_library/`。替换音效时更新同名 MP3 与 `wwwroot/config/audio.json`；若浏览器仍使用旧缓存，强制刷新一次。

### 添加新音效

1. 将文件放入 `assets/audio/`，使用英文 kebab-case，例如：

   ```text
   character-defeated.mp3
   shield-deployed.mp3
   healing-trigger.mp3
   ```

2. 在 `wwwroot/config/audio.json` 的 `tracks` 中注册稳定 Track ID：

   ```json
   "sfx.character-defeated": {
     "source": "/assets/audio/character-defeated.mp3",
     "bus": "sfx",
     "volume": 0.8,
     "preload": "auto"
   }
   ```

3. 在 `events` 中绑定语义事件：

   ```json
   "combat.character-defeated": ["sfx.character-defeated"]
   ```

4. 在对应 UI／战斗演出节点调用：

   ```js
   sound.emit('combat.character-defeated');
   ```

一个事件可以播放多个 Track，一个 Track 也可以被多个事件复用。角色技能和音效文件可以替换，但事件 ID 应尽量保持稳定。

## 6. 维护原则

- 文件名只描述声音用途，不包含角色显示语言。
- 中日文切换不会改变 Event ID 或 Track ID。
- 随机结果必须在服务器结算后，通过同步日志触发音效，不能由两端各自随机判断。
- 高频短音效应避免过长尾音；同一时刻的攻击和反击允许重叠播放。
- 不要让浏览器直接预载高码率 WAV。当前 7 个 WAV 原始文件约 20 MB，运行用 MP3 合计约 416 KB，能显著减少 Cloudflare 临时隧道中的取消流与首次加载压力。
- `node --test tests\\*.test.js` 会检查事件引用、文件存在性和音效文件命名。
