# Tiny Pixel Fights — 战场角色配音系统

> 当前状态：V1 自动目录索引  
> 资源目录：`assets/audio/voice/`  
> 配置文件：`wwwroot/config/voice.json`  
> 运行代码：`wwwroot/voice.js`

---

## 1. 当前设计结论

角色配音不再把具体日语文件名写进 `voice.json`。

现在规则是：

```text
assets/audio/voice/{characterId}/{voiceType}/
        ↓
/api/audio/voice-index 自动扫描目录
        ↓
VoiceDirector 随机播放该目录下的音频
```

所以你以后往某个目录里新增 `.wav / .mp3 / .ogg / .m4a`，只要刷新页面，就会进入随机池。

`dotnet clean` 不影响这件事；它不是编译产物，而是运行时目录扫描。

---

## 2. 当前启用的语音触发

| Type | 触发条件 | 说话者 |
|---|---|---|
| `select` | 选择/拖起可行动卡片 | 被选择角色 |
| `attack-preview-overpower` | 选择攻击目标并弹出预测；预测净收益 ≥ 2 | 攻击者 |
| `attack-preview-disadvantage` | 选择攻击目标并弹出预测；预测净损失 ≥ 1 | 攻击者 |
| `attack-preview-even` | 选择攻击目标并弹出预测；不属于碾压或劣势 | 攻击者 |
| `attack-declare` | 主动攻击演出开始 | 攻击者 |
| `defeat` | 主动攻击并击败 target | 攻击者 |
| `death` | 角色死亡，不区分来源 | 死亡角色 |
| `damage-taken` | 受到实际 HP 伤害 | 受伤角色 |
| `heavy-damage-taken` | 单次实际 HP 伤害 ≥ 当前最大 HP 的 1/4 | 受伤角色 |

已预留但未接逻辑：

| Type | 用途 |
|---|---|
| `level-up` | 未来升级系统 |
| `lieutenant-joined` | 未来副官加入 |
| `ultra-skill` | 未来大招/必杀系统 |

---

## 3. 目录规则

角色文件夹必须使用游戏内部稳定 ID：

```text
assets/audio/voice/
  peasant/
  princess/
  mage/
  oracle/
  knight/
  druid/
  barbarian/
  monster/
```

注意：

- 魔法师的内部 ID 是 `mage`，不是 `magician`。
- 如果目录叫 `magician`，游戏不会匹配到魔法师。

每个角色下面放语音类型目录：

```text
assets/audio/voice/mage/
  select/
  attack-preview-overpower/
  attack-preview-disadvantage/
  attack-preview-even/
  attack-declare/
  defeat/
  death/
  damage-taken/
  heavy-damage-taken/
  level-up/
  lieutenant-joined/
```

例：

```text
assets/audio/voice/mage/select/008_覚悟はいい？.wav
assets/audio/voice/mage/select/009_次はあいつか.wav
assets/audio/voice/mage/select/012_ターゲット確認.wav
```

只要这些文件在同一个目录下，触发 `mage + select` 时就会随机播放其中一个。

---

## 4. 文件格式规则

当前自动索引支持：

```text
.wav
.mp3
.ogg
.m4a
```

所以以后你把：

```text
select/008_覚悟はいい？.wav
```

换成：

```text
select/008_覚悟はいい？.mp3
```

不需要改 JS，不需要改 C#，也不需要手动改 `voice.json`。

只要文件还在：

```text
assets/audio/voice/{characterId}/{voiceType}/
```

就能被 `/api/audio/voice-index` 扫到。

---

## 5. `voice.json` 的职责

现在 `voice.json` 不负责列出具体文件名。

它只负责：

- 默认音量
- 角色音量倍率
- 语音类型音量倍率
- 语音类型触发后延迟
- 冷却时间
- 大伤害阈值
- 需要提前预热的语音类型
- 优先级
- 保留类型
- 指定自动索引 API

当前核心配置：

```json
{
  "version": 2,
  "autoIndex": "/api/audio/voice-index",
  "defaults": {
    "bus": "voice",
    "volume": 0.9,
    "delayMs": 0
  },
  "thresholds": {
    "heavyDamageTakenMaxHpRatio": 0.25
  },
  "preloadTypes": [
    "select"
  ],
  "characterVolumes": {
    "princess": 1,
    "mage": 1
  },
  "typeVolumes": {
    "select": 1,
    "attack-preview-overpower": 1,
    "attack-preview-disadvantage": 1,
    "attack-preview-even": 1,
    "attack-declare": 1,
    "damage-taken": 1
  },
  "typeDelaysMs": {
    "select": 0,
    "attack-preview-overpower": 360,
    "attack-preview-disadvantage": 360,
    "attack-preview-even": 360,
    "attack-declare": 0,
    "defeat": 0,
    "death": 0,
    "damage-taken": 0,
    "heavy-damage-taken": 0
  }
}
```

如果以后某个角色/某个触发需要特殊音量、特殊冷却、或只想指定某几个文件，也可以在 `pools` 里覆盖。

覆盖规则：

- 默认：使用目录自动扫描结果。
- 如果 `voice.json` 某个 pool 明确写了 `sources`，则该 pool 使用手写 `sources`。
- `preloadTypes` 用来提前预热高频语音类型。当前只预热 `select`，避免选择角色时第一次播放吞掉开头。

语音最终音量由多层倍率相乘：

```text
最终音量 =
  Audio 菜单里的 VOICE 音量
  × audio.json 里的 voice bus 音量
  × voice.json defaults.volume
  × characterVolumes[characterId]
  × typeVolumes[voiceType]
  × pools[characterId][voiceType].volume
```

常用调整：

```json
{
  "characterVolumes": {
    "princess": 0.82,
    "mage": 1,
    "monster": 0.9
  },
  "typeVolumes": {
    "select": 0.9,
    "death": 1.05
  },
  "pools": {
    "princess": {
      "select": {
        "volume": 0.86
      }
    }
  }
}
```

建议优先把太响的角色往下压，不要把安静素材大幅拉高，避免浏览器播放时失真。

### 按语音类型延迟播放

如果想让某类台词在满足触发条件后稍微晚一点播放，改：

```json
"typeDelaysMs": {
  "select": 0,
  "attack-preview-overpower": 360,
  "attack-preview-disadvantage": 360,
  "attack-preview-even": 360,
  "attack-declare": 0,
  "defeat": 0,
  "death": 0,
  "damage-taken": 0,
  "heavy-damage-taken": 0
}
```

含义：

- 单位是毫秒。
- 语音通过触发条件、冷却、优先级检查后，再按这里的时间延后播放。
- `defaults.delayMs` 是全局默认延迟。
- `typeDelaysMs[type]` 是某一类台词的默认延迟。
- `pools[characterId][voiceType].delayMs` 可以覆盖到某个角色的某类台词。

例：只让公主的选择台词晚 120ms：

```json
"pools": {
  "princess": {
    "select": {
      "delayMs": 120
    }
  }
}
```

注意：这不是攻击方和受击方之间的“对话间隔”。主动攻击时仍然有一层顺序控制：攻击方先说，过一小段时间后受击方再说。`typeDelaysMs` 会叠加在各自台词本身上，用来微调某类声音与画面演出的贴合。

---

## 6. 后端自动索引接口

新增 API：

```text
GET /api/audio/voice-index
```

它会扫描：

```text
assets/audio/voice/
```

并返回：

```json
{
  "version": 1,
  "pools": {
    "mage": {
      "select": {
        "sources": [
          "/assets/audio/voice/mage/select/008_...",
          "/assets/audio/voice/mage/select/009_..."
        ]
      }
    }
  }
}
```

路径会自动 URL encode，所以日语文件名可以正常访问。

---

## 7. 音量控制

`AudioDirector` 现在有独立的 `voice` bus。

右上角音频菜单：

```text
BGM
SFX
VOICE
```

VOICE 可以单独开关和调音量。

`select` 语音会在 manifest 加载后提前建立音频模板，播放时复用同源资源，降低浏览器第一次解码或音频设备刚唤醒时吞掉开头 0.x 秒的概率。如果某个台词文件本身开头贴得太紧，仍建议在素材前方保留约 80–150ms 的极短静音。

### 开发者用总线音量

开发者侧的 BGM、音效、UI 音效、角色语音总线在：

```text
wwwroot/config/audio.json
```

当前结构：

```json
{
  "buses": {
    "bgm": 0.42,
    "sfx": 0.85,
    "ui": 0.7,
    "voice": 0.9
  }
}
```

含义：

| Bus | 用途 | 开发者调法 |
|---|---|---|
| `bgm` | 对局背景音乐 | 调 `audio.json` 的 `buses.bgm` |
| `sfx` | 战斗、技能、状态等音效 | 调 `audio.json` 的 `buses.sfx` |
| `ui` | 按钮、发牌、房间、菜单等 UI 音效 | 调 `audio.json` 的 `buses.ui` |
| `voice` | 角色台词 | 调 `audio.json` 的 `buses.voice`，或更推荐在 `voice.json` 细调 |

普通 BGM / SFX 的最终音量：

```text
最终音量 =
  玩家音频菜单里的对应组音量
  × audio.json 的 buses[bus]
  × audio.json 的 tracks[trackId].volume
```

例：

```json
"buses": {
  "bgm": 0.42,
  "sfx": 0.85,
  "ui": 0.7,
  "voice": 0.9
},
"tracks": {
  "sfx.physical-hit": {
    "bus": "sfx",
    "volume": 0.74
  }
}
```

调法建议：

- BGM 整体太大：先调 `buses.bgm`。
- 所有战斗音效整体太大：先调 `buses.sfx`。
- 所有按钮/发牌/房间音效太大：先调 `buses.ui`。
- 某一个音效太大：调对应 `tracks[trackId].volume`。
- 角色台词整体太大：可以调 `buses.voice`。
- 某个角色或某类台词太大：优先调 `voice.json` 的 `characterVolumes` / `typeVolumes`。

注意：玩家右上角 UI 调的是本机运行时音量；开发者这里调的是默认混音平衡。两者会相乘。

---

## 8. 冷却与优先级

当前默认：

```json
"globalCooldownMs": 260,
"typeCooldownMs": 700,
"characterCooldownMs": 1200
```

优先级：

| Type | Priority |
|---|---:|
| `death` | 100 |
| `ultra-skill` | 95 |
| `level-up` | 90 |
| `defeat` | 85 |
| `heavy-damage-taken` | 70 |
| `lieutenant-joined` | 70 |
| `attack-declare` | 40 |
| `damage-taken` | 25 |
| `attack-preview-overpower` | 18 |
| `attack-preview-disadvantage` | 18 |
| `attack-preview-even` | 18 |
| `select` | 10 |

大伤害会触发 `heavy-damage-taken`，不会再额外触发普通 `damage-taken`。

死亡和击败优先级高，可以越过普通冷却。

主动攻击时还有一层播放仲裁，避免双方台词同时挤在一起：

```text
攻击方台词先播放
  defeat > attack-declare

短间隔：
  attack-declare → damage-taken：约 980ms
  其他攻击方 → 受击方：约 760ms

受击方台词再播放
  death > heavy-damage-taken > damage-taken
```

也就是说：

- 攻击方每次主动攻击最多说一句。
- 如果攻击直接击败目标，攻击方说 `defeat`，不会再说 `attack-declare`。
- 受击方每次结算最多说一句。
- 如果角色死亡，受击方说 `death`，不会再说 `damage-taken` 或 `heavy-damage-taken`。
- 反击伤害暂时不触发额外角色台词，避免一次普通攻击产生太多语音。

### 攻击预测语音

当玩家选择攻击目标并成功弹出攻击预测面板时，攻击者会根据预测结果播放一条判断局势的语音。

当前三类：

| Type | 条件 | 含义 |
|---|---|---|
| `attack-preview-overpower` | `预测造成伤害 - 预测受到伤害 >= 2` | 碾压 |
| `attack-preview-disadvantage` | `预测受到伤害 - 预测造成伤害 >= 1` | 劣势 |
| `attack-preview-even` | 其他情况 | 基本势均力敌 |

如果预测伤害是范围，例如 `2–4`，语音分类使用中点 `(min + max) / 2`，避免只看最大值导致误判。

目录例：

```text
assets/audio/voice/princess/attack-preview-overpower/
assets/audio/voice/princess/attack-preview-disadvantage/
assets/audio/voice/princess/attack-preview-even/
```

这三类语音属于“预测阶段”，不是正式攻击阶段。正式攻击确认后仍然使用：

```text
attack-declare / defeat
```

---

## 9. 维护方式

### 添加新语音

例如给魔法师增加选择语音：

```text
assets/audio/voice/mage/select/new-line-01.wav
assets/audio/voice/mage/select/new-line-02.mp3
```

然后刷新页面即可。

不需要：

- 改 C#
- 改 JS
- 改 `voice.json`
- `dotnet clean`

### 添加新角色语音

例：

```text
assets/audio/voice/oracle/select/select-01.wav
assets/audio/voice/oracle/death/death-01.wav
```

只要 `oracle` 是游戏内部 character ID，就能自动匹配。

### 添加新语音类型

如果未来实装 `ultra-skill`：

```text
assets/audio/voice/mage/ultra-skill/ultra-skill-01.mp3
```

然后在代码里某个大招触发点调用：

```js
emitVoice('ultra-skill', 'mage');
```

---

## 10. 未来副官与升级

预留目录：

```text
assets/audio/voice/lieutenants/_placeholder/lieutenant-joined/
assets/audio/voice/progression/level-up/
```

未来副官如果成为独立单位，建议：

```text
assets/audio/voice/lieutenants/{lieutenantId}/lieutenant-joined/
```

如果副官只是主将装备槽，也可以先继续放在主将目录：

```text
assets/audio/voice/{characterId}/lieutenant-joined/
```

这取决于未来副官系统的数据结构。

---

## 11. 当前代码边界

- C# 只新增了一个目录索引 API。
- C# 战斗规则没有任何角色语音判断。
- `VoiceDirector` 只读 cue 和 manifest。
- `AudioDirector` 负责最终播放。
- 语音资源路径不进入战斗规则。

这套结构适合之后继续加角色、加副官、加升级、换 mp3、换语言版本。
