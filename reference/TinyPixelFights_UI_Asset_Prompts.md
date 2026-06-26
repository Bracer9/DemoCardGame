# Tiny Pixel Fights — UI / VFX 资产分类与生成 Prompt

> 用途：为战斗反馈、状态显示和八名角色的专属技能建立统一视觉语言。  
> 输出目标：供 GPT 图像生成或 Stable Diffusion 生成透明 PNG，再由 HTML／CSS／Web Animation 负责移动、缩放、闪烁和时序。  
> 当前阶段：A／B 类的可替换占位与嵌入系统已经实装，C 类已注册并可直接替换。工程接入方法见 [TinyPixelFights_UI_Asset_Embedding.md](TinyPixelFights_UI_Asset_Embedding.md)。

---

## 1. 设计目标

这套 UI 首先要解决的不是“画面不够华丽”，而是让玩家在很短时间内理解：

> 谁发动 → 什么效果 → 作用于谁 → 数值怎样变化 → 为什么死亡

因此所有图形都必须：

1. **小尺寸仍可辨认**：图标缩到 24～32 px 时仍能认出轮廓。
2. **通用反馈保持一致**：所有角色掉血、治疗、死亡使用同一视觉语法。
3. **专属效果一眼认出来源**：八项技能拥有不同的徽记和瞬时特效。
4. **动画短促**：单段一般控制在 0.25～0.8 秒，不让熟练玩家约5分钟的对局被演出明显拖长。
5. **不在图片中生成文字**：技能名、伤害数字和 Buff 名称继续由 HTML 显示，以支持日语／中文切换。

### 与现有画面的统一风格

- **明确采用日式二次元像素游戏语言**：参考 16／32-bit 时代 JRPG 的战斗 UI、状态图标与技能特效，不采用写实欧美奇幻插画。
- 使用清晰的像素块、阶梯状斜线、有限色板与少量手工抖色；轮廓需要像经过像素画师逐点整理，而不是把写实图片简单降分辨率。
- 中世纪暗黑、皇家战术沙盘、黑铁与旧羊皮纸质感。
- 近黑底、象牙白主轮廓、少量旧金。
- 红色作为攻击、危险和行动强调色。
- 青蓝色用于公共盾牌与守护。
- 紫色用于魔法与占星。
- 病绿色用于衰弱、孢子和负面状态。
- 图形具有锋利、不对称、剪纸般的 JRPG UI 动势，但不照搬任何现有作品的标志或版式。
- 这里的“二次元”指日式 JRPG 的造型概括与色彩设计；A／B／C 类仍然是符号图标，不生成角色脸、人物立绘或写实物体摄影。

### 建议色板

| 用途 | 色值 |
|---|---|
| 深黑 | `#09090B` |
| 黑铁 | `#202126` |
| 象牙白 | `#E8DDCC` |
| 强调红 | `#C61F32` |
| 暗红 | `#711724` |
| 旧金 | `#C7A45A` |
| 盾牌青蓝 | `#65DDEB` |
| 魔法紫 | `#8B64C7` |
| 炎上橙红 | `#E4572E` |
| 衰弱病绿 | `#78966A` |

---

## 2. 需要多少类、多少件资产

完整规划为 **5 类、44 件**。

| 类别 | 数量 | 通用／唯一 | 用途 |
|---|---:|---|---|
| A. 战斗事件图标 | 8 | 全角色通用 | 在提示条、战斗日志、伤害数字旁说明事件性质 |
| B. Buff／Debuff 状态图标 | 10 | 机制唯一、角色间共用 | 卡片状态、悬停详情、状态发动提示 |
| C. 角色技能徽记 | 8 | 每名角色唯一 | 技能发动条、卡面技能区、日志来源标记 |
| D. 通用战斗 VFX 图层 | 7 | 全角色通用 | 攻击、命中、治疗、盾牌和死亡动画 |
| E. 专属技能 VFX 图层 | 11 | 每项技能／阶段唯一 | 表明具体技能如何触发和产生后果 |
| **合计** | **44** | 通用15／状态10／角色专属19 | 形成完整战斗反馈语法 |

### 推荐制作顺序

#### 第一批：下一轮 playtest 必需，共 25 件

- A 类 8 件全部。
- B 类 10 件全部。
- D 类 7 件全部。

这批完成后，即使专属技能暂时只显示名称和状态图标，玩家也能看懂攻击、掉血、炎上、治疗和死亡。

#### 第二批：角色辨识度，共 8 件

- C 类八个技能徽记。

#### 第三批：表现强化，共 11 件

- E 类专属技能 VFX。

---

## 3. 输出规格

尺寸应由资产的实际显示范围决定，不统一强制生成大图。下面是建议值；如果 GPT 或模型只提供固定档位，使用最接近的档位生成后再缩小即可。

| 资产类型 | 建议生成尺寸 | 建议导出尺寸 | 游戏内典型显示 |
|---|---:|---:|---:|
| A 类战斗事件图标 | `256 × 256` | `64 × 64` PNG | 24～40 px |
| B 类状态图标 | `256 × 256` | `64 × 64` PNG | 24～40 px |
| C 类技能徽记 | `512 × 512` | `128 × 128` PNG | 48～96 px |
| D／E 类单卡VFX | `512 × 512` | 依实际覆盖范围导出 | 约一张卡片范围 |
| D／E 类全队横向VFX | 约 `1024 × 384` | 依战场宽度导出 | 四张相邻卡片范围 |

### 图标与徽记

- 背景透明。
- 主体占画布约 68%，四周留出安全边距。
- 一个文件只有一个居中图形，不要文字、数字、边框底板或角色肖像。
- 尽量使用 2～4 个大形状，不要依赖细碎装饰。
- 最终以游戏内实际显示尺寸检查清晰度；没有必要仅为简单图标保留超大母图。
- 缩放时使用 nearest-neighbor／关闭抗锯齿，避免像素边缘被双线性插值磨糊。

### VFX 图层

- 生成的是可叠加的光、裂痕、粒子、斩击或符文，不要画完整场景和卡牌。
- 最好将核心光效、粒子和残影分层生成，CSS 动画分别控制。
- 如果特效只覆盖局部，不应为了填满正方形而扩大空白画布。

### 不建议让生成模型直接制作 Sprite Sheet

GPT／SD 很难保证连续帧一致。更可靠的方法是：

1. 生成一张清晰的核心图层。
2. 必要时再生成粒子图层和残影图层。
3. 在 CSS 中通过 `opacity`、`transform`、`filter`、遮罩和裁切完成动画。

---

## 4. 通用 Prompt 模板

下面各表中的 `Prompt Core` 需要接在对应模板的 `{SUBJECT}` 位置。

### 4.1 GPT 图像生成：图标／徽记模板

```text
Create one production-ready pixel-art game UI icon for a Japanese dark-fantasy JRPG tactical card game. Subject: {SUBJECT}. Authentic hand-crafted 16/32-bit Japanese RPG pixel-art aesthetic, anime-inspired shape language, deliberate pixel clusters, stepped diagonal edges, limited color palette, selective one-pixel highlights and restrained dithering. Stylized medieval heraldic silhouette, simplified black iron and aged ivory materials, angular asymmetrical JRPG interface energy, not realistic, not painterly. High contrast and readable at 24 pixels, centered with generous empty margin. One isolated symbol only, transparent background, crisp hard pixel edges, no antialiasing, no frame, no card, no character portrait, no letters, no words, no numbers, no watermark.
```

### 4.2 Stable Diffusion：图标／徽记基础正向 Prompt

```text
(single pixel-art game UI icon:1.35), {SUBJECT}, Japanese dark fantasy JRPG, anime-inspired symbolic design, authentic hand-crafted 16-bit and 32-bit RPG interface art, deliberate pixel clusters, stepped diagonal edges, limited color palette, restrained dithering, crisp hard pixels, simplified medieval heraldry, stylized black iron and aged ivory, angular asymmetrical silhouette, high contrast, readable at tiny size, centered, isolated, transparent background, production sprite asset, nearest-neighbor appearance
```

### 4.3 Stable Diffusion：图标统一 Negative Prompt

```text
text, letters, words, numbers, logo, watermark, signature, border, frame, button, card, full scene, landscape, character portrait, face, hands, photorealistic, realistic western fantasy illustration, realistic metal rendering, painterly, oil painting, concept art, smooth vector art, 3d render, mockup, anti-aliased edges, soft gradients, multiple icons, icon sheet, clutter, excessive detail, thin unreadable lines, blurry, low contrast, cropped, opaque background
```

### 4.4 GPT 图像生成：VFX 模板

```text
Create one isolated pixel-art combat VFX overlay for a Japanese dark-fantasy JRPG tactical card game. Effect: {SUBJECT}. Authentic hand-crafted 16/32-bit anime game VFX, deliberate pixel clusters, stepped motion arcs, limited palette, crisp hard pixel edges, restrained dithering and no antialiasing. Bold angular motion and graphic Japanese RPG interface energy, with stylized black-iron fragments, aged-ivory highlights and a restrained accent color; not realistic and not painterly. Designed to overlay one character card without hiding the portrait. Transparent background, generous empty space, no environment, no card, no character, no text, no border, no watermark. Production-ready transparent PNG.
```

全队效果在 Prompt 末尾补充覆盖范围，不强制模型使用固定像素：

```text
Wide transparent overlay spanning four adjacent character cards, approximately 8:3 aspect ratio.
```

### 4.5 Stable Diffusion：VFX 基础 Prompt

```text
(isolated pixel-art game VFX overlay:1.35), {SUBJECT}, Japanese dark fantasy JRPG, anime game combat effect, authentic hand-crafted 16-bit and 32-bit pixel art, deliberate pixel clusters, stepped motion arcs, crisp hard pixels, no antialiasing, restrained dithering, angular graphic motion, controlled pixel particles, high contrast, limited palette, transparent background, production sprite effect, no scene
```

### 4.6 Stable Diffusion：VFX Negative Prompt

```text
text, letters, numbers, logo, watermark, character, face, body, card, frame, environment, landscape, full illustration, photorealistic, realistic fire, realistic smoke, realistic western fantasy illustration, painterly, oil painting, concept art, smooth vector effect, 3d render, anti-aliased edges, soft blurry gradients, rectangular background, black background, white background, excessive smoke, clutter, blurry edges, cropped effect, multiple panels, sprite sheet
```

> 如果模型不能稳定输出透明背景，可改成纯灰或纯绿色背景生成，再进行抠图。不要使用黑色背景，因为本项目大量粒子和描边本身就是黑色。

---

## 5. A 类：通用战斗事件图标（8件）

| ID／文件名 | 用途 | Prompt Core |
|---|---|---|
| `icon_event_physical.png` | 主动物理、技能物理伤害 | `a heavy medieval sword cutting diagonally through a cracked iron plate, one sharp crimson slash, ivory blade edge` |
| `icon_event_magical.png` | 主动魔法、技能魔法伤害 | `a faceted arcane star piercing a broken circular rune, violet core with a thin cyan edge, black iron geometry` |
| `icon_event_counter.png` | 反击 | `two opposed sword tips forming a hooked return arrow, defensive reversal rather than a normal attack, ivory and dark red` |
| `icon_event_skill.png` | 技能发动提示 | `an eight-point royal sigil opening like a sharp eye, small crimson spark in the center, ivory and black iron` |
| `icon_event_status_tick.png` | 回合开始的炎上／状态结算 | `a small hourglass pierced by a pulsing rune, one falling ember, ominous delayed-effect symbol` |
| `icon_event_heal.png` | HP回复、祝福回复 | `a medieval chalice holding a single bright heart-shaped droplet, aged gold and warm ivory rays` |
| `icon_event_shield.png` | 防御阵形、盾吸收 | `a broad royal heater shield protecting four tiny diamond marks beneath one cyan arch, black steel and cyan light` |
| `icon_event_death.png` | 战斗不能／退场 | `a character card silhouette severed by one ruthless diagonal cut, broken ivory fragments and a narrow crimson line` |

### 防御阵形图标完整 GPT Prompt 示例

```text
Create one production-ready pixel-art game UI icon for a Japanese dark-fantasy JRPG tactical card game. Subject: a broad royal heater shield protecting four tiny diamond marks beneath one cyan arch, representing one shared shield pool for an entire four-character team. Authentic hand-crafted 16/32-bit Japanese RPG pixel-art aesthetic, anime-inspired heraldic shape language, deliberate pixel clusters, stepped diagonal edges, limited color palette, selective one-pixel highlights and restrained dithering. Stylized black steel and aged ivory, luminous cyan edge, subtle royal crest, angular asymmetrical silhouette, not realistic and not painterly. High contrast and readable at 24 pixels, centered with generous empty margin. One isolated symbol only, transparent background, crisp hard pixel edges, no antialiasing, no frame, no card, no character portrait, no letters, no words, no numbers, no watermark.
```

---

## 6. B 类：Buff／Debuff 状态图标（11件）

这些图标表示“角色当前处于什么状态”，不等同于技能本身的徽记。

| 状态ID／文件名 | 类型 | 来源／含义 | Prompt Core |
|---|---|---|---|
| `status_blessing.png` | Buff / Aura | 公主：每个己方回合开始回复1 HP | `a crowned white lily cradling one heart-shaped golden droplet, gentle radial halo, sacred blessing` |
| `status_foresight.png` | Buff / Aura | 占卜师：概率减伤、魔法与炎上+1 | `an open eye inside a rotating astrolabe, one violet star reflected in the pupil, protective ivory orbit` |
| `status_team_shield.png` | Buff / Aura | 公共防御阵形、全队共享盾值 | `four small shield studs linked beneath one thin cyan protective dome, shared team defense` |
| `status_shield_complacency.png` | Debuff / Consumable | 有恃无恐／盾への慢心：下一次反击伤害降低 | `a protected warrior leaning carelessly behind a cyan shield while a downward-pointing crossed-swords symbol grows dull, overconfidence caused by safety, muted crimson and cyan` |
| `status_beast_rage.png` | Buff / Permanent | 美女与野兽：我方公主阵亡后，怪物基础攻击+2 | `a black beast crown roaring beneath one fallen ivory rose, two upward crimson claw marks, permanent royal grief transformed into fury` |
| `status_guard.png` | Buff / Aura | 骑士：一次主动物理代伤 | `a tall knight shield stepping in front of a smaller allied crest, one incoming arrow splitting on the shield` |
| `status_burning.png` | Debuff | 炎上：下个目标方回合开始受魔法伤害 | `a branded ember rune biting into the corner of a torn card silhouette, orange-red flame with violet magical core` |
| `status_weakness_pending.png` | Debuff / Pending | 孢子附着、下回合转为衰弱 | `pale green spores clinging around a small hourglass and an intact sword, delayed weakening` |
| `status_weakness.png` | Debuff | 下个目标回合主动攻击伤害-2 | `a cracked downward-pointing sword wrapped in fungal threads and pale spores, sickly green accent` |
| `status_harvest_pending.png` | Buff / Pending | 农民播种、下个己方回合丰收 | `one seed resting in a dark furrow beneath a thin rising crescent, quiet anticipation, muted gold` |
| `status_harvest.png` | Buff | 本回合攻击+2 | `a full wheat sheaf crossed with an upward sickle blade, explosive golden harvest rays, one crimson accent` |

### 状态图标共用规则

- Buff 的彩色光向上或向外扩张。
- Debuff 的形状向下、收缩、破裂或缠绕。
- Pending 状态统一加入小型沙漏／月相，不需要另外生成“等待”文字。
- Aura 是否仍由来源角色维持，可由 CSS 增加环形边框，不应画死在图标中。

---

## 7. C 类：八名角色专属技能徽记（8件）

技能徽记用于技能发动条、卡面技能区和战斗日志。它应表示“是谁的哪项技能”，而状态图标表示“目标现在怎么了”。

| 角色／文件名 | 技能 | Prompt Core |
|---|---|---|
| 姫 `skill_saints_prayer.png` | 聖女の祈り | `a small royal crown above a white lily and folded prayer ribbons, sacred golden rays, compassionate but authoritative` |
| 占い師 `skill_stargazers_aegis.png` | 星読みの加護 | `a crystal sphere held inside a precise astrolabe, an eye-shaped constellation crossing it, violet and cyan starlight` |
| 農民 `skill_spring_harvest.png` | 春蒔き秋実り | `a seed and a wheat ear connected by a circular sickle, clear two-phase seasonal cycle, iron and harvest gold` |
| 魔法使い `skill_searing_mark.png` | 灼熱の刻印 | `a sharp arcane brand shaped like a spiral flame inside a broken magic circle, violet center and orange-red edges` |
| ドルイド `skill_weakening_spores.png` | 衰弱の胞子 | `a crescent leaf surrounding one mushroom cap releasing five deliberate spores, occult natural magic, sickly green` |
| バーバリアン `skill_aftershock_axe.png` | 戦斧の余波 | `a brutal double-headed battle axe splitting a circular shockwave toward one side, ivory steel and crimson force` |
| モンスター `skill_predatory_instinct.png` | 美女と野獣 | `a black fanged beast maw biting through a broken shield around a stark red zero, paired with one elegant ivory rose, beauty and beast, unstoppable absolute damage` |
| 騎士 `skill_interposing_shield.png` | 身代わりの盾 | `a massive tower shield crossing in front of an allied crest, a broken arrow and one blue-white impact spark` |

---

## 8. D 类：通用战斗 VFX 图层（7件）

| 文件名 | 播放时机 | Prompt Core |
|---|---|---|
| `vfx_physical_slash.png` | 物理攻击命中 | `one broad diagonal sword slash, ivory-hot cutting edge, crimson trailing wedge, a few black iron sparks, very clear direction` |
| `vfx_magic_impact.png` | 魔法攻击命中 | `a compact violet arcane starburst collapsing inward then exploding outward, thin cyan runes and black shards` |
| `vfx_damage_hit.png` | 任意实际掉 HP | `a tight radial impact crack with four ivory shards and a red center pulse, designed behind a floating damage number` |
| `vfx_heal_pulse.png` | 任意回复 HP | `a warm gold upward pulse, three soft heart-like droplets and thin sacred rings, restrained not cute` |
| `vfx_shield_form.png` | 展开／强化防御阵形 | `a thin cyan protective arc drawing itself from the center outward, royal geometric filigree and faint blue particles, wide team overlay` |
| `vfx_shield_break.png` | 共享盾归零 | `a cyan glass-like royal barrier splitting into long angular fragments and dissolving particles, wide team overlay` |
| `vfx_death_cut.png` | 角色 HP 归零 | `two crossing black-and-ivory guillotine slashes with one narrow crimson seam, a few card-paper fragments, fast decisive defeat` |

### 通用死亡演出建议

死亡不要直接把卡片设为不可见。推荐组合：

1. `vfx_death_cut` 快速划过。
2. 卡面沿斜线错位 6～12 px。
3. 颜色抽离为灰白。
4. 卡片碎片或灰尘消散。
5. 最后保留暗色败退卡位，而不是让阵列突然塌缩。

---

## 9. E 类：专属技能 VFX 图层（11件）

| 文件名 | 技能阶段 | Prompt Core |
|---|---|---|
| `vfx_princess_blessing_team.png` | 公主回合开始、全队回复 | `four thin golden prayer rays rising beneath four card positions, a crowned lily sigil at the center, warm ivory healing motes, wide team overlay` |
| `vfx_oracle_foresight_proc.png` | 预见成功减伤／魔法强化 | `a translucent astrolabe eye snapping open over one target, violet star refraction bending an incoming strike, precise cyan orbit lines` |
| `vfx_peasant_sowing.png` | 农民首行动播种 | `a swift dark soil arc and five glowing seeds dropping into a narrow furrow, muted gold promise, minimal particles` |
| `vfx_peasant_harvest.png` | 下回合获得攻击+2 | `a sudden upward fan of golden wheat blades behind one card, one sickle-shaped red energy arc, triumphant but brief` |
| `vfx_mage_searing_mark.png` | 50%成功附加炎上 | `a spiral flame brand stamping onto the target, violet magical center, orange-red ring closing like a seal` |
| `vfx_burning_tick.png` | 炎上在回合开始造成伤害 | `the same spiral brand flaring vertically from inside the target silhouette, one focused magical flame column, clear delayed detonation` |
| `vfx_druid_spores_attach.png` | 孢子成功附着 | `a curved ribbon of pale green spores traveling from caster to target, five large readable motes, occult leaf-shaped trail` |
| `vfx_weakness_activate.png` | 下回合转为衰弱 | `fungal threads tightening around a spectral sword until it cracks downward, sickly green dust collapsing inward` |
| `vfx_barbarian_aftershock.png` | 主动伤害达到3点后波及相邻敌人 | `a brutal crescent axe shockwave launching sideways from a heavily struck target toward one neighboring card, ivory edge and crimson mass` |
| `vfx_monster_predatory_strike.png` | 主动伤害为0时发动绝对伤害追击 | `two enormous black-red fang silhouettes bypassing a shattered defense and snapping shut around the target center, a stark white number 3 impact, feral and instantaneous` |
| `vfx_knight_intercept.png` | 骑士替队友承担1伤害 | `a steel-blue tower shield silhouette sliding violently in from the side, stopping a physical slash, one white impact spark and redirected fragments` |

### 全队祝福 VFX 完整 GPT Prompt 示例

```text
Create one isolated wide pixel-art combat VFX overlay for a Japanese dark-fantasy JRPG tactical card game. Effect: four thin golden prayer rays rising beneath four adjacent character-card positions, a small crowned white-lily sigil at the center, warm ivory healing motes traveling upward to each card, restrained sacred power rather than a bright holy explosion. Authentic hand-crafted 16/32-bit anime game VFX, deliberate pixel clusters, stepped light rays, limited palette, crisp hard pixel edges, restrained dithering and no antialiasing. Bold angular Japanese RPG interface energy, clear readable motion paths, not realistic and not painterly, designed not to hide card portraits. Transparent background, generous empty space, no environment, no cards, no characters, no text, no border, no watermark. Production-ready transparent PNG, approximately 8:3 aspect ratio.
```

### 炎上结算 VFX 完整 GPT Prompt 示例

```text
Create one isolated pixel-art combat VFX overlay for a Japanese dark-fantasy JRPG tactical card game. Effect: a sharp spiral fire brand already embedded in the target suddenly flares into one narrow vertical magical flame, violet core and orange-red outer edge, indicating delayed burning damage at turn start. Authentic hand-crafted 16/32-bit anime game VFX, deliberate pixel clusters, stepped flame edges, limited palette, crisp hard pixels, restrained dithering and no antialiasing. Fast, compact, dangerous, not realistic and not painterly, easy to read over one character card without hiding the portrait. Transparent background, no environment, no card, no character, no text, no border, no watermark. Production-ready transparent PNG sized for one character-card overlay.
```

---

## 10. UI 动画中的使用方式

生成资产之后，不应只把图标贴上去。每个战斗事件建议遵循统一顺序：

| 阶段 | UI／VFX |
|---|---|
| 1. 来源锁定 | 发动者卡片提亮，显示 C 类技能徽记或 A 类攻击图标 |
| 2. 技能宣言 | 0.3～0.5秒技能名条；被动成功触发才播放，不要每次都重复 |
| 3. 作用路径 | 箭头、斩击、魔法轨迹或专属 VFX 指向目标 |
| 4. 结算 | 通用命中 VFX + HP数字；盾吸收则优先播放盾牌反馈 |
| 5. 状态变化 | B 类图标出现、闪动或从 Pending 转为 Active |
| 6. 死亡 | 先显示直接死因，再播放通用死亡切断动画 |

### 例：炎上致死

```text
炎上图标放大并显示“炎上”
→ vfx_burning_tick
→ 魔法伤害数字
→ HP归零
→ icon_event_death + vfx_death_cut
→ 卡片灰化退场
```

玩家由此能理解“不是回合开始随机少了一张卡，而是上回合魔法使施加的炎上完成了结算”。

### 例：骑士守护

```text
攻击者的物理攻击轨迹指向原目标
→ skill_interposing_shield 徽记亮起
→ vfx_knight_intercept 从骑士方向切入
→ 原目标显示伤害-1
→ 骑士显示肩代伤害1
→ status_guard 图标熄灭
```

---

## 11. 生成与筛选检查表

每张图生成后检查：

- 缩到 32 px 是否仍能看懂？
- 是否只包含一个主体？
- 是否意外生成文字、数字、卡片或人物？
- 黑色部分在当前暗色背景上是否有象牙白／彩色轮廓？
- 红、蓝、紫、绿是否遵循既定语义？
- Buff 与 Debuff 的动势是否容易区分？
- 专属技能是否能与其状态图标建立联系，但又不完全相同？
- VFX 是否留有足够透明区域，不遮住卡牌核心信息？
- 是否能通过 CSS 动画，而不是依赖模型生成连续帧？

建议每个 Prompt 先生成 4 个候选，只选择轮廓最清楚的一版。确定第一个通用图标后，把它作为后续生成的视觉参考图，以保持线宽、旧化程度和颜色一致。

---

## 12. 文件目录建议

```text
assets/ui/
├─ events/       # A 类通用事件图标
├─ statuses/     # B 类 Buff / Debuff
├─ skills/       # C 类技能徽记
└─ vfx/
   ├─ common/    # D 类通用战斗VFX
   └─ skills/    # E 类专属技能VFX
```

文件名使用稳定英文 ID，不将日语或中文写入文件名。以后增加语言时不需要复制图片；界面文字仍由 `wwwroot/i18n.js` 提供。
