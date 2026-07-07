# Tiny Pixel Fights - 士气系统实装文档 V2

更新时间：2026-07-06

本文只定义 **士气 Morale** 系统本体，以及士气引入后必须同步调整的伤害落点、BP 统计、预测、UI、现有 Trait / Role Action 判定边界。

本文不定义部署规则，不改共享盾规则，不改副官规则，不改补员规则，不新增 Buff / Debuff。

---

## 1. 系统目标

士气是每个角色独立拥有的外接承伤资源，用于让角色在 HP 被击穿前先承受一部分战斗压力。

核心口径：

```text
既有伤害管线计算出“将要作用到角色身上的最终伤害”后，非绝对伤害先扣士气。
士气不足时，剩余伤害穿透到 HP。
绝对伤害是绝对的，直接扣 HP，不经过士气。
```

士气解决的是：

```text
伤害进入 HP 太快，角色在 build 成型前退场。
```

---

## 2. 士气基础规则

### 2.1 字段

每个角色实例新增：

```csharp
public int Morale { get; set; }
public int MaxMorale { get; set; } = 5;
```

### 2.2 初始值

战斗开始时，所有初始在场角色：

```text
Morale = 5
MaxMorale = 5
```

V1 不做角色差异化士气上限。

新创建的角色实例默认也初始化为：

```text
Morale = MaxMorale = 5
```

如果之后有复归 / 继承类系统，再由对应系统覆盖该初始化值。本文不定义复归规则。

### 2.3 士气不是状态

士气不是：

```text
Buff
Debuff
StatusEffect
DisplayGroup
Aura
Shield
HP
治疗量
```

因此：

```text
士气不能被驱散。
士气不能被净化。
士气不能被偷取。
士气不能被复制。
士气不显示在 Buff / Debuff 列表。
士气不生成 status instance。
```

### 2.4 士气可以穿透

士气不是不可穿透盾。

```text
最终伤害 <= 当前士气：只扣士气，不扣 HP。
最终伤害 > 当前士气：士气扣到 0，剩余伤害扣 HP。
```

例：

```text
目标：士气 5，HP 12
最终伤害 3
结果：士气 2，HP 12
```

```text
目标：士气 2，HP 12
最终伤害 5
结果：士气 0，HP 9
```

### 2.5 士气不会被治疗恢复

普通治疗只恢复 HP，不恢复士气。

```text
治疗 HP +2：只影响 HP。
士气不变。
```

士气只通过本文的“回合结束士气恢复”规则恢复。

### 2.6 士气不触发治疗相关 Trait

士气恢复不是治疗。

因此士气恢复不触发：

```text
Cleric `field-medic`
任何“造成有效治疗”触发
任何“治疗成功后”触发
```

---

## 3. 士气在伤害管线中的位置

### 3.1 基本管线

士气不负责重新计算攻击、防御、倍率、弱点、光环、反应防御等。

士气只接收既有伤害管线输出的：

```text
FinalCharacterDamage
```

即：

```text
攻击 / 技能原始伤害
→ 攻击方 modifier
→ 目标防御 / 承伤 modifier
→ 既有防御系统 / 反应规则
→ 得到将要扣角色 HP 的 FinalCharacterDamage
→ 士气承伤
→ HP 承伤
```

本文不改变既有共享盾、守护、预见、防御、状态倍率的计算方式。士气只处理“已经要进入角色承伤层”的伤害。

### 3.2 士气承伤伪代码

```csharp
DamageResult ApplyMoraleAndHpDamage(
    CharacterInstance target,
    int finalCharacterDamage,
    DamageKind damageKind)
{
    var result = new DamageResult();
    result.FinalCharacterDamage = Math.Max(0, finalCharacterDamage);

    if (result.FinalCharacterDamage <= 0)
        return result;

    if (damageKind == DamageKind.LifeLoss ||
        damageKind == DamageKind.HpCost ||
        damageKind == DamageKind.SacrificeCost)
    {
        int hpDamage = result.FinalCharacterDamage;
        target.HP -= hpDamage;
        result.HpDamage = hpDamage;
        result.BypassedMorale = true;
        return result;
    }

    int moraleDamage = Math.Min(target.Morale, result.FinalCharacterDamage);
    target.Morale -= moraleDamage;

    int hpDamageRemaining = result.FinalCharacterDamage - moraleDamage;
    target.HP -= hpDamageRemaining;

    result.MoraleDamage = moraleDamage;
    result.HpDamage = hpDamageRemaining;
    return result;
}
```

死亡检查仍然只看 HP：

```csharp
if (target.HP <= 0)
{
    DefeatCharacter(target);
}
```

士气降为 0 不会导致退场。

---

## 4. 伤害类型边界

### 4.1 经过士气的伤害

以下都属于伤害，会先扣士气：

| 来源 | 是否经过士气 | 说明 |
|---|---:|---|
| 主动物理攻击 | 是 | 先走既有物理伤害计算，再扣士气。 |
| 主动魔法攻击 | 是 | 先走既有魔法伤害计算，再扣士气。 |
| 反击伤害 | 是 | 反击最终伤害先扣士气。 |
| Trait 造成的非绝对伤害 | 是 | 除非该 Trait 明确是 HP 失去 / 代价。 |
| Role Action 造成的非绝对伤害 | 是 | 如果文本是“造成伤害”，且不是绝对伤害，则先扣士气。 |
| 状态伤害 | 是 | 例如燃烧 tick。 |
| 事件非绝对伤害 | 是 | 例如战斧余波。 |

### 4.2 不经过士气的 HP 失去 / 代价

以下直接扣 HP，不经过士气：

| 来源 | 是否经过士气 | 说明 |
|---|---:|---|
| 失去 HP | 否 | 文本写“失去 HP”时直接扣 HP。 |
| 支付 HP 作为费用 | 否 | 成本不能被士气抵消。 |
| 献祭 / 契约代价 | 否 | 例如黑暗契约的 HP 失去。 |
| 自伤代价 | 否 | 如果语义是代价 / 反噬，而不是敌方造成伤害。 |
| 绝对伤害 | 否 | 绝对伤害直接扣 HP，不经过士气、共有盾或防御。 |

推荐枚举：

```csharp
public enum DamageKind
{
    Physical,
    Magical,
    Absolute,
    StatusDamage,
    TraitDamage,
    RoleActionDamage,
    EventDamage,

    LifeLoss,
    HpCost,
    SacrificeCost
}
```

判定原则：

```text
“造成 X 点伤害” = 走士气。
“失去 X 点 HP” = 不走士气。
“支付 X HP” = 不走士气。
“受到反噬 / 自伤”如果是角色技能代价，默认不走士气。
```

---

## 5. DamageResult 必须扩展

引入士气后，伤害结果需要明确拆分士气承伤与真实 HP 承伤。

### 5.1 必要字段

```csharp
public sealed class DamageResult
{
    public int FinalCharacterDamage { get; set; }

    public int MoraleDamage { get; set; }
    public int HpDamage { get; set; }

    public bool BypassedMorale { get; set; }
    public bool DealtHpDamage => HpDamage > 0;
    public bool WasZeroHpDamage => HpDamage == 0;
}
```

### 5.2 字段含义

| 字段 | 含义 |
|---|---|
| `FinalCharacterDamage` | 已经过既有防御 / 状态 / 光环 / 反应规则后，将要作用到角色的伤害。 |
| `MoraleDamage` | 本次由士气承受的伤害。 |
| `HpDamage` | 本次由 HP 承受的伤害。 |
| `BypassedMorale` | 本次是否因为 HP 失去 / 成本等原因绕过士气。 |

---

## 6. HPDamage 与 MoraleDamage 判定边界

```text
MoraleDamage 不等于 HPDamage。
MoraleDamage 不触发任何写着“造成 HP 伤害时”的 Trait / Role Action。
MoraleDamage 不阻止任何写着“造成 0 点 HP 伤害时”的 Trait / Role Action。
```

现有文本中写 HP 伤害的效果，继续严格检查真实 `HpDamage`。

```text
“造成 HP 伤害时” = HpDamage > 0。
“造成 0 点 HP 伤害时” = HpDamage == 0。
“造成至少 N 点 HP 伤害时” = HpDamage >= N。
```

不要把 HP 伤害、0 点 HP 伤害、至少 N 点 HP 伤害统一替换成其它全局概念。

如果某个具体角色因为士气系统变得过强或过弱，之后单独调整该角色文本和机制，不做全局概念替换。

---

## 7. 现有 Trait 判定调整

### 7.1 Druid `weakening-spores`

原逻辑：

```text
主动攻击造成 HP 伤害时 100% 发动；未造成 HP 伤害时 50% 发动。
```

士气后改为：

```text
主动攻击造成 HP 伤害时 100% 发动。
主动攻击未造成 HP 伤害时 50% 发动。
```

说明：

```text
如果攻击只打掉士气，没有扣 HP，按未造成 HP 伤害处理，50%。
如果攻击被完全挡住、最终伤害为 0，才按 50%。
```

### 7.2 Barbarian `aftershock-axe`

原逻辑：

```text
主动攻击对目标造成至少 3 点 HP 伤害时，触发战斧余波。
```

士气后改为：

```text
主动攻击对目标造成至少 3 点 HP 伤害时，触发战斧余波。
```

余波伤害量使用：

```text
本次主动攻击对主目标造成的真实 HP 伤害
```

只打掉士气、没有扣 HP 时不触发。

余波本身是事件物理伤害，会对余波目标正常走士气。

### 7.3 Monster `predatory-instinct`

#### 非公主目标

原逻辑：

```text
对非公主目标主动攻击造成 0 点 HP 伤害时，追击绝对伤害。
```

士气后改为：

```text
对非公主目标主动攻击造成 0 点 HP 伤害时，追击绝对伤害。
```

说明：

```text
如果怪物攻击造成了 3 点士气伤害、0 点 HP 伤害，仍然视为 0 点 HP 伤害。
触发 0 HP 伤害追击。
```

追击绝对伤害直接扣 HP，不经过士气。

#### 攻击公主

攻击公主时造成的绝对伤害：

```text
属于绝对伤害，直接扣公主 HP，不经过士气。
```

攻击后的怪物自伤 / 反噬：

```text
按 HP 失去 / 反噬代价处理，不经过士气。
仍然遵守“不会因此死亡”的最低 HP 规则。
```

这样可以避免士气完全抵消怪物攻击公主的代价。

### 7.4 Mage `searing-mark`

原逻辑：

```text
主动攻击结算后，若目标仍存活，50% 赋予燃烧。
```

士气后保持：

```text
只检查目标是否仍存活。
```

说明：

```text
如果攻击只造成士气伤害，目标仍存活，可以正常判定是否赋予燃烧。
```

### 7.5 Oracle `stargazers-aegis`

星读加护的减伤属于伤害 modifier。

士气后口径：

```text
先进行星读加护等伤害修正。
得到最终角色伤害后，再进入士气。
```

如果减伤后仍有伤害，即使只扣士气，也不算造成 HP 伤害。

魔法和燃烧伤害 +1：

```text
先增加对应伤害，再进入士气。
```

### 7.6 Knight `interposing-shield`

骑士替身之盾的触发检查应发生在士气之前。

推荐口径：

```text
当骑士以外我方角色将受到 1 点及以上主动物理角色伤害时触发。
这里的“将受到”指士气承伤前的 FinalCharacterDamage。
```

骑士承担的伤害：

```text
作为对骑士造成的物理伤害处理。
经过骑士自身防御后，再扣骑士士气，溢出扣 HP。
```

说明：

```text
士气不会让骑士守护失效。
只要本次攻击在士气承伤前确实会对被保护者产生角色伤害，骑士仍可介入。
```

### 7.7 Farmer `spring-harvest`

春播秋收检查的是主动攻击顺序，不检查 HP 伤害。

士气后不变。

### 7.8 Princess `saints-prayer`

圣女的祝福是 HP 治疗。

士气后不变：

```text
恢复 HP，不恢复士气。
不触发士气恢复。
```

---

## 8. 现有 Role Action 判定调整

### 8.1 Princess `saintly-prayer`

效果：

```text
治疗目标 HP；若目标有可净化 Debuff，净化 1 个并获得资源效果。
```

士气后：

```text
治疗只恢复 HP，不恢复士气。
净化不影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 8.2 Princess `royal-command`

王令不造成伤害，不治疗。

士气后：

```text
不直接影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

说明：

```text
Role Action 首次 BP 不限制类型，所以王令也可以触发。
```

### 8.3 Knight `guard-oath`

守护誓约属于士气前的伤害修正 / 特殊防御 Buff。

士气后：

```text
受到主动物理攻击时，先按守护誓约减少伤害。
减少后的 FinalCharacterDamage 再进入士气。
```

如果伤害被减到 0：

```text
MoraleDamage = 0，HpDamage = 0。
```

### 8.4 Knight `raise-bulwark`

该 Role Action 修改既有防御系统。

士气后：

```text
不直接影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 8.5 Barbarian `war-cry`

战吼本身不造成伤害。

士气后：

```text
战吼产生的狂怒 / 额外攻击次数 / 防御代价照常。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

战吼引发的追加绝对伤害：

```text
属于绝对伤害，直接扣目标 HP，不经过士气。
```

### 8.6 Barbarian `challenge`

破势只赋予战栗。

士气后：

```text
不直接影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 8.7 Mage `arcane-channel`

秘术蓄能不造成即时伤害。

士气后：

```text
不直接影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

后续咏唱强化的魔法伤害：

```text
先计算咏唱倍率，再进入士气。
```

### 8.8 Mage `searing-brand`

灼热刻印赋予燃烧和空虚。

士气后：

```text
赋予状态本身不扣士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

后续燃烧伤害：

```text
属于魔法状态伤害，先扣士气，溢出扣 HP。
```

空虚：

```text
先放大后续魔法伤害，再进入士气。
```

### 8.9 Oracle `star-reading`

星读只改变行动次数。

士气后：

```text
不直接影响士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

被星读允许的再次主动攻击：

```text
照常造成伤害；最终伤害进入士气。
```

### 8.10 Oracle `fate-mark`

命运标记修改目标下一次主动攻击造成的伤害。

士气后：

```text
先结算命运标记的伤害随机修正。
修正后的 FinalCharacterDamage 再进入士气。
```

### 8.11 Monster `predatory-gaze`

捕食凝视赋予猎物。

士气后：

```text
猎物触发条件保持“0 点 HP 伤害”。
```

即：

```text
目标受到攻击，如果 HpDamage = 0，触发猎物追加 2 点绝对伤害。
如果只打掉士气、没有扣 HP，仍然触发猎物。
```

猎物追加的绝对伤害：

```text
直接扣 HP，不经过士气。
该追加伤害不递归触发猎物。
```

### 8.12 Monster `dark-pact`

黑暗契约分两段：

#### 目标失去 HP

```text
目标失去最多 4 HP，至少保留 1 HP。
```

这是 HP 失去 / 契约代价：

```text
不经过士气。
直接扣 HP。
```

#### 下一次主动攻击附加绝对伤害

```text
下一次主动攻击附加 4 点绝对伤害。
```

这是攻击伤害：

```text
直接扣 HP，不经过士气。
```

#### HP 低于一半获得 BP

判定使用真实 HP：

```text
黑暗契约失去 HP 后，如果目标 HP 低于一半，获得 BP。
```

士气不参与该判定。

### 8.13 Peasant `supply-basket`

补给篮治疗和赋予坚守。

士气后：

```text
治疗只恢复 HP，不恢复士气。
坚守影响后续物理伤害，先减伤，再进入士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

注意：

```text
即使 Cost = 0 AP，只要成功使用，并且是本 turn 第一次 Role Action，也触发 +1 BP。
```

### 8.14 Peasant `field-work`

田间劳作可能回复自己 HP、获得播种、增加攻击次数。

士气后：

```text
回复自己 2 HP 只恢复 HP，不恢复士气。
增加攻击次数后造成的攻击伤害照常进入士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 8.15 Druid `cleansing-herbs`

净化草药净化并治疗。

士气后：

```text
净化不影响士气。
治疗只恢复 HP，不恢复士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 8.16 Druid `weakening-spores-action`

衰弱孢子移除 Buff 并施加力竭 / 磨损。

士气后：

```text
不直接影响士气。
力竭 / 磨损影响后续伤害输出，先修正伤害，再进入士气。
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

---

## 9. 士兵 Trait / Role Action 判定调整

### 9.1 Cleric `field-medic`

战地医护触发条件：

```text
Role Action 造成有效治疗，或通过 Role Action 成功净化 1 个 Debuff。
```

士气后：

```text
士气恢复不是治疗。
士气恢复不触发 field-medic。
```

有效治疗仍然只看 HP：

```text
目标 HP 实际增加 > 0，才算有效治疗。
```

### 9.2 Cleric `mend`

应急治疗：

```text
治疗 HP，不恢复士气。
净化不影响士气。
第一次成功 Role Action 可触发 +1 BP。
```

### 9.3 Shieldmaiden `shield-drill`

盾阵训练不直接改士气。

士气后：

```text
坚守影响后续物理伤害，先减伤，再进入士气。
```

### 9.4 Shieldmaiden `aegis-formation`

该 Role Action 不直接改士气。

士气后：

```text
如果这是本 turn 第一次成功使用 Role Action，触发 Role Action BP +1。
```

### 9.5 Duelist `duel-sense`

决斗嗅觉提供强攻。

士气后：

```text
强攻先放大物理伤害，再进入士气。
```

Rank1 额外绝对伤害：

```text
属于绝对伤害，直接扣 HP，不经过士气。
```

### 9.6 Duelist `crimson-lunge`

绯红突刺赋予战栗 / 脆弱。

士气后：

```text
不直接影响士气。
脆弱影响后续物理承伤，先放大伤害，再进入士气。
第一次成功 Role Action 可触发 +1 BP。
```

### 9.7 Arcanist `arcane-resonance`

原触发：

```text
我方 Hero 造成魔法伤害，或赋予燃烧 / 空虚 / 力竭 / 磨损后。
```

士气后：

```text
“造成魔法伤害”的判定不引入全局总伤害概念；保持现有实现口径，除非后续单独修改该角色文本。
```

也就是说：

```text
Hero 造成魔法攻击但只扣了目标士气、没有扣 HP 时，不因士气伤害自动视为造成 HP 伤害。
```

赋予状态的触发分支不变。

### 9.8 Arcanist `astral-focus`

星界聚焦赋予咏唱或空虚。

士气后：

```text
不直接影响士气。
咏唱 / 空虚影响后续伤害，再进入士气。
第一次成功 Role Action 可触发 +1 BP。
```

---

## 10. BP 与士气恢复

### 10.1 每 turn BP 获取统计

每名玩家每个己方 turn 记录：

```csharp
public int BpEarnedThisTurn { get; set; }
public bool FirstRoleActionBpGrantedThisTurn { get; set; }
```

己方 turn 开始时重置：

```csharp
BpEarnedThisTurn = 0;
FirstRoleActionBpGrantedThisTurn = false;
```

### 10.2 每 turn BP 获取上限

每个己方 turn，实际获得 BP 总量最多为 5。

```text
BP 获取上限 = 5 / turn
```

该上限包括：

```text
低保 BP
第一次 Role Action BP
技能额外 BP
其他奖励 BP
```

### 10.3 BP 获取统一入口

所有 BP 获取必须走统一函数。

```csharp
int TryGainBattlePoint(PlayerState player, int amount, string reason)
{
    int remainingCap = 5 - player.BpEarnedThisTurn;
    int actualGain = Math.Clamp(amount, 0, remainingCap);

    if (actualGain <= 0)
        return 0;

    player.BattlePoints += actualGain;
    player.BpEarnedThisTurn += actualGain;

    LogBpGain(player, actualGain, reason);
    return actualGain;
}
```

### 10.4 低保 BP

如果当前规则已有每 turn 低保 BP，则低保也必须走 `TryGainBattlePoint`。

```text
低保 +1 BP 计入 BpEarnedThisTurn。
低保 +1 BP 计入每 turn 5 点上限。
低保 +1 BP 会参与回合结束士气恢复。
```

### 10.5 第一次成功使用 Role Action 获得 BP

每个己方 turn，第一次成功使用 Role Action 时：

```text
获得 1 BP。
```

规则：

```text
不限制 Role Action 类型。
不限制 AP 消耗。
不限制是否造成伤害。
不限制是否治疗 / 净化 / 资源 / 支援。
每个己方 turn 只触发一次。
非法使用 / 目标无效 / 发动失败不触发。
```

伪代码：

```csharp
void OnRoleActionResolved(PlayerState player, RoleActionResult result)
{
    if (!result.Success)
        return;

    if (!player.FirstRoleActionBpGrantedThisTurn)
    {
        player.FirstRoleActionBpGrantedThisTurn = true;
        TryGainBattlePoint(player, 1, "first-role-action");
    }
}
```

如果本 turn BP 已经达到 5，上述 Role Action 仍然应将：

```csharp
FirstRoleActionBpGrantedThisTurn = true;
```

置为 true，防止后续 Role Action 再尝试触发首次奖励。

### 10.6 BP 消耗不影响士气恢复

士气恢复看的是：

```text
本 turn 实际获得了多少 BP
```

不是看：

```text
回合结束时当前还持有多少 BP
```

因此：

```text
本 turn 获得 3 BP，随后花掉 2 BP。
回合结束仍按 BpEarnedThisTurn = 3 恢复士气。
```

### 10.7 回合结束士气恢复

己方 turn 结束时：

```text
MoraleRecovery = BpEarnedThisTurn
```

所有我方存活角色：

```text
Morale = min(MaxMorale, Morale + MoraleRecovery)
```

由于 `BpEarnedThisTurn` 每 turn 上限为 5，所以士气恢复每 turn 自然最多 5。

例：

```text
本 turn 低保 +1，第一次 Role Action +1，其他来源 +2。
BpEarnedThisTurn = 4。
己方 turn 结束时，所有存活我方角色士气 +4，上限 5。
```

---

## 11. 预测 UI 要求

攻击 / 伤害预测必须显示士气与 HP 的拆分。

### 11.1 基础显示

示例：

```text
预计伤害：5
士气 -3
HP -2
```

如果只打士气：

```text
预计伤害：3
士气 -3
HP 不变
```

如果不造成任何角色伤害：

```text
预计伤害：0
士气不变
HP 不变
```

### 11.2 死亡预测

死亡预测只看 HP。

```text
如果 PredictedHpDamage 会使 HP <= 0，显示退场预警。
如果只打空士气，不显示退场预警。
```

### 11.3 触发预测

涉及 HP 伤害条件的 Trait 预测要保持真实 HP 伤害口径：

```text
Druid weakening-spores：显示 100% / 50% 时看 PredictedHpDamage > 0。
Barbarian aftershock-axe：显示是否触发时看 PredictedHpDamage >= 3。
Monster predatory-instinct：显示 0 HP 伤害追击时看 PredictedHpDamage == 0。
Predatory-gaze 猎物：显示触发时看 PredictedHpDamage == 0。
Arcanist arcane-resonance：不引入全局总伤害概念，保持现有实现口径。
```

---

## 12. UI 显示

### 12.1 角色卡

角色卡显示：

```text
HP 12 / 12
士气 3 / 5
```

或 pip：

```text
士气 ●●●○○
```

### 12.2 Inspector

Inspector 显示：

```text
士气：3 / 5
```

士气不进入 Buff / Debuff 列表。

### 12.3 日志

伤害日志：

```text
{target} 的士气承受了 {moraleDamage} 点伤害。
```

穿透日志：

```text
{target} 的士气承受了 {moraleDamage} 点伤害，HP 受到 {hpDamage} 点伤害。
```

HP 失去日志：

```text
{target} 失去 {hpDamage} HP。
```

士气恢复日志：

```text
本 turn 获得 {bp} BP，己方全体士气恢复 {bp}。
```

---

## 13. DTO

### 13.1 Character DTO

新增：

```csharp
public int Morale { get; set; }
public int MaxMorale { get; set; }
```

### 13.2 Attack / Damage Preview DTO

新增：

```csharp
public int PredictedFinalCharacterDamage { get; set; }
public int PredictedMoraleDamage { get; set; }
public int PredictedHpDamage { get; set; }
public bool PredictedDefeat { get; set; }
```

### 13.3 Battle Log DTO

如果已有伤害日志 DTO，追加：

```csharp
public int MoraleDamage { get; set; }
public int HpDamage { get; set; }
public bool BypassedMorale { get; set; }
```

---

## 14. 推荐实装位置

| 内容 | 文件 |
|---|---|
| `Morale`, `MaxMorale` 字段 | `Domain/GameState.cs` |
| 角色实例初始化 | 创建 `CardInstance` / `CharacterInstance` 的位置 |
| 士气伤害拆分 | `Domain/GameEngine.cs` 的角色伤害结算函数 |
| `DamageResult` 扩展 | 现有 DamageResult 所在文件 |
| HP 伤害判定复核 | `Domain/Traits.cs`, `Domain/GameEngine.cs` |
| Role Action 首次 BP | `Domain/GameEngine.cs` Role Action resolve 之后 |
| BP 获取统一入口 | `Domain/GameEngine.cs` / PlayerState helper |
| 回合结束士气恢复 | `Domain/GameEngine.cs` EndTurn 流程 |
| 预测拆分 | `Api/AttackPreviewService.cs` |
| DTO | `Api/GameDtos.cs` |
| UI | `wwwroot/app.js` |
| 文本 | `wwwroot/locales/zh.json`, `wwwroot/locales/ja.json` |

---

## 15. 必测用例

### 15.1 初始化

```text
战斗开始时，所有角色 Morale = 5 / 5。
```

### 15.2 士气完全吸收

```text
目标士气 5，HP 12。
最终角色伤害 3。
结果：士气 2，HP 12。
DamageResult: MoraleDamage 3, HpDamage 0。
```

### 15.3 士气穿透

```text
目标士气 2，HP 12。
最终角色伤害 5。
结果：士气 0，HP 9。
DamageResult: MoraleDamage 2, HpDamage 3。
```

### 15.4 HP 失去不经过士气

```text
目标士气 5，HP 12。
黑暗契约使其失去 4 HP。
结果：士气 5，HP 8。
```

### 15.5 绝对伤害直接扣 HP

```text
目标士气 5，HP 12。
受到 4 点绝对伤害。
结果：士气 5，HP 8。
```

### 15.6 治疗不恢复士气

```text
目标士气 1，HP 8 / 12。
治疗 2 HP。
结果：士气 1，HP 10 / 12。
```

### 15.7 士气恢复不触发治疗 Trait

```text
Cleric 在场。
回合结束因 BP 恢复士气。
不触发 field-medic。
```

### 15.8 Druid HP 伤害判定

```text
Druid 主动攻击目标。
造成 3 点士气伤害、0 点 HP 伤害。
weakening-spores 按未造成 HP 伤害，50% 判定。
```

### 15.9 Barbarian 战斧余波

```text
Barbarian 主动攻击目标。
造成 4 点士气伤害、0 点 HP 伤害。
aftershock-axe 不触发。
```

### 15.10 Monster 0 HP 伤害追击

```text
Monster 主动攻击非公主目标。
造成 3 点士气伤害、0 点 HP 伤害。
predatory-instinct 触发 0 HP 伤害追击。
```

### 15.11 Monster 真正 0 角色伤害触发

```text
Monster 主动攻击非公主目标。
最终角色伤害 0。
HPDamage = 0。
predatory-instinct 触发 0 HP 伤害追击。
追击绝对伤害直接扣 HP。
```

### 15.12 猎物判定

```text
目标有猎物。
本次攻击造成 2 点士气伤害、0 点 HP 伤害。
猎物触发。
```

```text
目标有猎物。
本次攻击 HPDamage = 0。
猎物触发 2 点绝对伤害。
该绝对伤害不递归触发猎物。
```

### 15.13 Arcanist 魔法伤害触发

```text
我方 Hero 造成 3 点魔法士气伤害、0 点 HP 伤害。
不因士气伤害自动视为造成 HP 伤害。
```

### 15.14 Role Action 第一次使用 BP

```text
本 turn 第一次成功使用 Role Action。
获得 1 BP。
FirstRoleActionBpGrantedThisTurn = true。
```

### 15.15 0 AP Role Action 也可触发首次 BP

```text
本 turn 第一次成功使用 Peasant supply-basket。
Cost = 0 AP。
仍获得 1 BP。
```

### 15.16 Role Action 首次 BP 只触发一次

```text
同一己方 turn 内第一次 Role Action 获得 1 BP。
第二次 Role Action 不再因首次使用获得 BP。
```

### 15.17 BP 上限

```text
本 turn 已实际获得 5 BP。
后续 BP 来源实际获得 0。
BpEarnedThisTurn 仍为 5。
```

### 15.18 BP 消耗不影响士气恢复

```text
本 turn 获得 4 BP，随后花掉 3 BP。
回合结束仍按 BpEarnedThisTurn = 4 恢复士气。
```

### 15.19 回合结束士气恢复

```text
本 turn BpEarnedThisTurn = 3。
己方存活角色士气分别为 0、2、5。
回合结束后变为 3、5、5。
```

### 15.20 预测一致性

```text
AttackPreview 显示士气伤害 2、HP 伤害 3。
实际结算必须得到 MoraleDamage 2、HpDamage 3。
```

---
