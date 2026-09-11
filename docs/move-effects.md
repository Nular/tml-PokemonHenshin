# 招式泰拉适配表（权威）

**Status：** Implemented（全形态接线）；Wave1～Wave3 / **Stage7+ 单机已验收**；**v1.4 数值已接线**（含大招后 `UltEnergyLockout`；游戏内 DPS 抽检待本地）。联机瞄准/壳弹**代码已接线**，双端验收 pending（清单 `docs/fx-knowledge.md`）。  
**版本：** 1.3（表结构）；变更记录至 **1.24（2026-09-10）**  
**冲突处理：** 与 `docs/requirements.md` 冲突时以需求为准；**已实现招式**以代码为准并回写本表。

---

## 1. 引言

### 1.1 源表

权威输入：策划 Excel「宝可梦.xlsx」（本机路径，**仓库不收录**）。  
列：姓名 · 被动 · 技能1 · 技能2 · 大招。Excel 文案为宝可梦语义，本文件给出**泰拉瑞亚可验收**的落地规格。

- Excel「铁哑铃」→ 本模 **金属怪**（`L08_F01`）。
- **地鼠线**（`L10_F01` / `L10_F02`）不在 Excel 中，按同结构补全（战斗招式可改地形），表中标注「补全」。

### 1.2 战斗模型

持握「{宝可梦}之力」期间：

| 槽位 | 输入 | 说明 |
|------|------|------|
| **被动** | 持续 | 持握即生效；可与 §3.2 属性生存/机动并存，持握被动优先 |
| **技能1** | 默认左键 | 主输出或主题招式 |
| **技能2** | 默认右键 | 副招式（近战/突进/辅助等） |
| **大招** | Mod 热键 | 仅当该形态能量满后可释放 |

取消持握：立刻失去外观、被动、招式与大招可用性；能量按 `FormId` **分存**，解除变身**暂停积攒**（不清空，除非饰品/规则另有说明）。

### 1.3 特效（FX）约束

- **优先原版复用：** 有现成 `Item.shoot` / `ProjectileID` 则 `NewProjectile` 或壳弹 + `LoadProjectile` 画同贴图（例：吹叶机 `Leaf`、泡泡 `Bubble`、星云奥秘外观/爆炸碎片）。
- **CWR：** 只读参考写法与参数；**禁止**运行时依赖 / `GetMod("CalamityOverhaul")`。需要其 trail 贴图时，**拷贝**进本模 `Assets/Fx/`（现有：`ThunderTrail`、`SoftGlow`、`LightShot`）。
- **禁止擅自降级：** 不得用 A=0 假 Additive、「跳过原版 AI 只留爆炸」、纯尘占位等简化顶替点名规格；改效果须用户确认。`MagicPixel` 可用但须可控尺寸（忌通天拉伸）。
- 表中「VFX note」指定手法与参考方向；冲突时以代码 + `AGENTS.md` 特效踩坑为准并回写本表。

---

## 2. 能量规则摘要

> **v1.4 数值真源：** `docs/requirements.md` §2.5 与 `docs/balance-stats.md` §6。下表为摘要；与旧「满值 100 / 命中 ICD」冲突时以 v1.4 + `HenshinStatService` 为准（**代码已切 EnergyMax=1000**）。

| 项 | 默认值（v1.4 设计） |
|----|--------|
| 满值 | **1000**（每 `FormId` 独立；形态可覆写） |
| 大招消耗 | 一次耗尽（A20 光之黏土：普通保留 40% / 超级 80%） |
| 命中积攒 | `12 × EnergyGainFactor`（无 tick ICD；见下「每秒软顶」） |
| 碎片命中 | `1 × EnergyGainFactor`（须 `MarkCrumb`；**现役仅**龙之波动 620。未打标 Retarget 弹仍走上一行） |
| 击杀积攒 | `55 × EnergyGainFactor`（碎片击杀仍走本条） |
| 自然积攒 | `0.15 / tick`；Boss 附近 ×3 |
| 每秒软顶 | 命中+击杀能量滚动 1s 内合计 **≤ 1000**（被动除外；极端兜底） |
| EnergyGainFactor | Standard 1.0 / HF 0.35 / MultiHit 0.25 / WideAoE 0.45 / Ult 0 |
| 大招后充能锁定 | 释放成功后约 **2.5s**（150 tick）禁止一切能量回复（`UltEnergyLockoutTicks`） |
| 释放键 | `ModKeybind`（可配置；未满提示不足） |
| 联机 | 能量与大招释放 **服务端权威** 同步（含 level/xp） |
| UI | Tooltip 显示该形态能量；变身时**角色脚下**能量条；满充时金色向上发散尘（约 1 格） |

招式伤害倍率与频次预算（`MoveRefRate`）见 `docs/balance-stats.md` §7（**参考窗，非铁律**；段数慎改；追踪易命中宜低、短近战高风险宜高）；**同一招式名在不同槽位/形态可有不同倍率**。

饰品修正（仅变身生效）：A19 命中/击杀能量 +30%；A20 大招后留 40%（超级 80%）；A21 大招伤害 +50% 且能量获取 ×0.80（超级 +100% / ×0.60）。讲究头带（A14）禁用技能2与大招。

**后摇 / aftermath：** 部分大招附带「休整」「伤害减半」「自损」「防御下降」等，写在表列「Energy/aftermath」；实现为短时自 debuff 或固定自伤，不受「坚硬脑袋」以外的反伤被动影响（自损类见各行）。

> 表内「耗 100」表示**耗尽满条**，不是字面扣 100。

---

## 3. 玩法代号（Playstyle）词表

技能格写法示例：`火花 Bolt+OnFire` · `种子机关枪 Barrage+EasyCrit`。

| 代号 | 含义 |
|------|------|
| **MeleeArc** | 近战弧形挥击（爪/鞭/尾/拳），短距离碰撞 |
| **Bolt** | 单体射弹 |
| **Spread** | 扇形或多发散射 |
| **Barrage** | 短时连发（如 5 连种子） |
| **Beam** | 直线/光束；可穿多目标或长距离 |
| **ChargeBeam** | 蓄力后发射的强光束（前摇不可移动或减速） |
| **Lunge** | 身体突进撞击 |
| **StrikeFall** | 跃起后下落砸击 |
| **AoEBurst** | 落点/自身周围爆发 |
| **DoTBind** | 「漩涡缠绕」：短时 DoT + 减速（非物理绑人） |
| **Field** | 场地 / 大范围持续区 |
| **SelfGuard** | 自身减伤/护盾类短 buff |
| **SelfBuff** | 自身增益（攻/速等） |
| **Dig** | 挖洞位移 / 出土爆发（接挖掘预算） |
| **Blink** | 短距瞬移（本表仅作备用代号；Excel 当前形态未用） |
| **EasyCrit** | 易暴：释放该招时 `GetCritChance(HenshinDamage)` **+35**（原版一次判定）；**不**再进升档池。A15/着火升档仅在已 Crit 时再 ×2（超暴约 ×4，偏红橙大飘字） |
| **Recoil** | 命中后自损（比例或固定）；「坚硬脑袋」可免 Recoil 自伤 |
| **Stun** | 短硬直 / 无法行动（对 NPC：停 AI 或减速极强，时长短） |
| **Sleep** | 更长无法行动（催眠）；Boss 大幅衰减时长或改为强减速 |
| **DefDown** | 目标临时易伤 / 降防 |
| **Slow** | 目标减速 |
| **OnFire / Poison / Electrify** | 着火 / 中毒 / 感电类 debuff |
| **IgnoreDef** | 伤害结算无视或部分无视目标防御 |
| **TrueMelee** | 标注真近战时才吃真近战加成（默认多数 MeleeArc 仍走 HenshinDamage） |

**漩涡类**（火焰漩涡、潮旋、流沙地狱）一律 **DoTBind**，不做真束缚物理。

**鬼斯通特记：** 被动「飘浮」= 强化飞行能量（默认池 ×2.5，仍禁坐骑）。舌舔用短 Stun。大招催眠术。

---

## 4. 全形态适配表

列说明：含 **Code**=Done（v1.3 已接线）。

- **Passive**：可验收数值。
- **Skill1 / Skill2 / Ultimate**：玩法代号 + 简述。
- **Energy/aftermath**：大招耗能与后摇。
- **VFX note**：无新图前提下的手法。
- **Net risk**：Low / Medium / High（位移、场地、挖砖、多段同步越高）。

列外加 **Code**：`Done` = 已接线；验收见计划验收清单。

### 4.1 火系链 L01

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk | Code |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|------|
| L01_F01 | 小火龙 | **猛火**：HP&lt;50% 时火系招式伤害 +20% | 火花 Bolt+OnFire | 抓 MeleeArc（三道**平行**爪痕+扩盒） | 火焰漩涡 HomingLock：自玩家射出，原版 Typhoon 橙红染色，锁定首敌；**撞实心停飞但不 Kill**，跑完生命周期 | 耗 100；无额外后摇 | 三平行爪痕尘；台风染色火矢（不生成灾厄弹） | Low | Done |
| L01_F02 | 火恐龙 | **猛火**：同上 +20% | 龙之波动 Nebula×10（0.7/不追踪/碰炸紫） | 火焰牙 BiteArc+OnFire | 闪焰冲锋 Lunge+Recoil25%+OnFire | 耗 100；Recoil | 星云奥秘外观连发；紫染爆炸碎片；火焰牙**两对大弧牙**；闪焰冲锋 **32 格** Fire 帧环绕+火径 | Medium | Done |
| L01_F03 | 喷火龙 | **太阳之力**：白天全招式伤害 +20%；每次造成招式伤害自损 1 HP（不死于该扣） | 喷射火焰 FlameCone×10 | 龙爪 Scratch火（**20 格**） | 过热 MouseAoE+OnFire；**5s 伤×0.5** | 耗 100；5s 伤害减半 | 喷射：真 Flames + **Fire 帧**；龙爪 HitJagged **帧**；过热：半径 **15 格**、5 段脉冲 + Fire/FlashImpact **帧** | Medium | Done |

### 4.2 草系链 L03

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L03_F01 | 妙蛙种子 | **茂盛**：HP&lt;50% 时草系招式伤害 +20% | 藤鞭 Whip（皮鞭级草尘鞭） | 撞击 Lunge+撞击爆 | 种子机关枪 Barrage×64+EasyCrit | 耗 100 | 草/叶绿尘鞭身；Seed 束状连发 | Low |
| L03_F02 | 妙蛙草 | **茂盛**：同上 +20% | 飞叶快刀 Spread×5（吹叶机 Leaf） | 咬住 BiteArc 尖牙 | 种子炸弹 Bolt/AoEBurst+EasyCrit（落点小爆） | 耗 100 | 真 Leaf + 绿尘；咬合尖牙 | Low | Done |
| L03_F03 | 妙蛙花 | **叶绿素**：白天移速 +35% | 污泥炸弹 Bolt+Poison | 花瓣舞 AoEBurst；释放后自身短混乱（1.5s 轻失控或伤害反噬 debuff） | 日光束 ChargeBeam（蓄力 ~0.8s 后强光束） | 耗 100；蓄力前摇 | 污泥炸后毒气瓶 ToxicCloud 簇 DoT；花瓣半径15格；日棱金束 | Medium | Done |

### 4.3 水系链 L02

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L02_F01 | 杰尼龟 | **激流**：HP&lt;50% 时水系招式伤害 +20% | 水枪 AquaScepter | 撞击 Lunge+撞击爆 | 泡沫光线 Barrage×64 束状高速 | 耗 100 | 海蓝权杖水流；泡泡**错落密束**+破裂小泡（少蓝水尘） | Low |
| L02_F02 | 卡咪龟 | **激流**：同上 +20% | 泡沫光线 Barrage（壳弹+Load Bubble） | 咬住 BiteArc | 潮旋 DoTBind（水漩涡缠绕） | 耗 100 | 须 `LoadProjectile(Bubble)`；错落密束+破裂；勿等玩家先用泡泡枪 | Low | Done |
| L02_F03 | 水箭龟 | **雨盘**：雨天或夜晚每 **90 tick**（1.5s）回 2 HP | 水炮 Beam（强水柱；**命中墙=命中怪**，锁长渐缩） | 火箭头锤 Charge→Lunge（短蓄力后头槌） | 加农水炮 Beam（**仍穿墙穿怪**）；释放后 **休整 ~1.5s**（禁技能1/2） | 耗 100；休整 | `WaterJetProj`：枪口渐进；水炮命中渐缩；加农穿透+每3击爆 | Medium | Done |

### 4.4 超能链 L12

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L12_F01 | 凯西 | **同步**：自身获得 OnFire/Poison/Electrify 时，最近敌对复制同 debuff（短 CD） | 念力 Bolt+Stun（弱，短硬直） | 意念头锤 StrikeFall（幽灵锤下砸 AoE，技能槽 ×**2.0**） | 精神强念 Scatter×32 彩虹杖可见弹；单发 ×0.6；生成 0.5s 后追踪 | 耗满 | 念力：`PsychicWaveBolt` ShadowBeamFriendly；**32 格**索敌、弹射下一目标（最多 2 击）；意念头锤；粉紫散射+延迟追踪 | Medium |
| L12_F02 | 胡地 | **同步**：同上 | 精神强念 鼠位×3 延迟追（不可穿墙/穿怪） | 真气拳 白气上扬无爆 | 预知未来 屏内夜光标记 1s 显形后追爆（IgnoreDef） | 耗 100 | 彩虹杖×3；FocusPunch；FairyQueenMagicItemShot | Medium | Accepted |

### 4.5 龙系链 L07

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L07_F01 | 迷你龙 | **蜕皮**：每 5s 约 15% 概率清除自身 1 个可清除 debuff | 龙息 Spread/Bolt+Stun（概率短僵；**线在实心截断**） | 咬住 MeleeArc | 龙之怒 Barrage（12/32 抖动球+5格爆；**大招仍穿墙**） | 耗 100 | 龙息：**128 格** Fire 帧线；怒：`DragonRageBarrage` SoftGlow 球 | Low | Done |
| L07_F02 | 哈克龙 | **蜕皮**：同上 | 龙之波动 Nebula×10 | 龙尾 星尘龙鞭 15格极强击退 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**；诅咒符 Barrage 穿墙） | 耗 100 | 龙波；`DragonTailWhip`；暴风 WeatherPain 主+**4伴随**穿透牵引 | Medium | Accepted |
| L07_F03 | 快龙 | **多重鳞片**：满 HP 时受到伤害 ×0.2（即减伤 80%）；掉血后失效至回满 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**） | 龙之俯冲 星尘龙路径冲 | 逆鳞 3s 身周火球；结束后 **自身混乱 ~2s** | 耗 100；混乱后摇 | 暴风 WeatherPain 技能档；`StardustPathLunge`；逆鳞 CultistBossFireBall 壳 | Medium | Accepted |

### 4.6 钢/超能链 L08（Excel：铁哑铃→金属怪）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L08_F01 | 金属怪 | **恒净之躯**：免疫本模关注的 debuff（着火/毒/感电/缓速等可列白名单） | 念力 Bolt+Stun | 撞击 Lunge | 猛撞 闪焰式冲+灰日耀VFX+Recoil25% | 耗 100；Recoil | 念力同凯西；`TakeDownLunge` + SolarWhipSwordExplosion 0伤 | Low | Accepted |
| L08_F02 | 巨金怪 | **恒净之躯**：同上 | 精神强念 胡地式鼠位×3 | 彗星拳 真气拳式+3×StarWrath | 破坏光线 Beam 自缓加粗；释放后 **休整 ~2s** | 耗 100；休整 | `AlakazamPsychic`；`CometPunch`+StarWrath；`SustainedBeam` 粗 | Medium | Accepted |

### 4.7 龙/地链 L15

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L15_F01 | 圆陆鲨 | **粗糙皮肤**：受击反弹 **0.35×** 当前变身招式基准伤害；且受击后短时攻击 +10%（2s） | 龙之怒 Barrage（**技能球撞实心爆**） | 撞击 Lunge | 流沙地狱 DoTBind（沙漩涡） | 耗 100 | 龙怒 `DragonRageBarrage` 技能12发；流沙直径 **16 格** Typhoon压蓝壳+琥珀 Cyclone/Fog | Medium | Done |
| L15_F02 | 烈咬陆鲨 | **粗糙皮肤**：同上 | 龙之波动 Nebula×10 | 咬碎 BiteArc+DefDown（更大） | 流星群 64×StarWrath；释放后 **自身攻击 −15% 持续 5s** | 耗 100；攻降后摇 | `DracoMeteorDirector` 狂星之怒壳+金粉爆 | Medium | Accepted |

### 4.8 幽灵链 L06

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L06_F01 | 鬼斯通 | **飘浮**：飞行能量视为强化（默认池 ×2.5，仍禁坐骑） | 暗影球 Bolt+DefDown（BrokenArmor；**撞墙**） | 舌舔 20格线+Stun（短硬直） | 催眠术 `HenshinSleepDebuff` 5s（屏内；Boss 改 Slow ~2s） | 耗 100 | 暗影球不透明紫盘链；舌舔 Extra98；睡眠 `DrawEffects(255,255,255,100)`+zzZ | Medium | Accepted |
| L06_F02 | 耿鬼 | **飘浮**：同上强化飞 | 污泥炸弹 Bolt+Poison | 暗影抓 紫龙爪20格+ShadowFlame | 恶之波动 32暗影球穿墙追踪爆 | 耗 100 | `ShadowClawSlash`；`DarkPulseBarrage`×32 | Medium | Accepted |

### 4.9 水/飞鱼链 L09

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L09_F01 | 鲤鱼王 | **优游自如**：雨天或夜晚移速 +25% | 跃起 StrikeFall（高跳砸地，低伤） | 撞击 Lunge | 抓狂 Barrage；伤害随 **已损失 HP%** 提高（最高约 +80%） | 耗 100 | 水花跃起；抓狂：身周半径 **15 格** 杂乱交错爪痕多段 | Low |
| L09_F02 | 暴鲤龙 | **自信过度**：击杀叠攻 +20%/层，最多 2 层，每层 12s | 水炮 Beam（**命中墙=命中怪**） | 咬碎 MeleeArc+DefDown | 破坏光线 Beam；释放后 **休整 ~2s** | 耗 100；休整 | `WaterJet` 跟鼠标；破灭 `SustainedBeam`（方向跟鼠标） | Medium | Done |

### 4.10 格斗链 L05

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L05_F01 | 腕力 | **毅力**：自身存在异常 debuff 时全招式伤害 +25% | 岩石封锁 Cross×4 收拢碎裂（Boulder 棕染 50%） | 撞击 Lunge（2s CD + 0.25s 无敌；速/距约半） | 十字劈 X 形剑气 + 前飞 64 格穿透 | 耗 100 | 可见四石；X 尘 + 前冲残影 | Medium |
| L05_F02 | 豪力 | **毅力**：同上 +25% | 岩崩 AoEBurst+Stun（概率） | 劈瓦 MeleeArc（对高防目标额外 +25% 伤；破「减伤 buff」语义） | 爆裂拳 MeleeArc+Stun（必短硬直） | 耗 100 | 岩崩3石微偏（`|vx|≤8`）+落地**1格**爆；劈瓦 **20 格**线斩；爆拳巨大拳套前挥+半径**20格**石爆 | Medium | Done |
| L05_F03 | 怪力 | **毅力**：同上 +25% | 尖石攻击 三角三刺穿透+命中爆 | 十字劈 360°弧形X | 近身战 高频+全程无敌+跟随；释放后 **防御 −20% 持续 5s** | 耗 100；防降后摇 | 三角 Boulder；贝塞尔弧 X；HitJagged 纠向 | Medium | Accepted |

### 4.11 飞行链 L11

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L11_F01 | 波波 | **锐利目光**：全招式伤害 ×1.2 | 起风 GroundCyclone（原版 Typhoon **单帧**深蓝贴地；盒随帧） | 啄 Cone~16格尖角 AoE | 燕返 Lunge（短 useTime；与撞击共用突进 CD） | 耗 100 | 单团贴地旋风；尖角尘锥 | Medium |
| L11_F02 | 大比鸟 | **锐利目光**：×1.2 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**） | 燕返 Lunge+EasyCrit（必易暴语义） | 勇鸟猛攻 Lunge+Recoil | 耗 100；Recoil | 暴风 WeatherPain 直立帧+穿透牵引（技能档）；燕返双弧；勇鸟梭形+交叉鸟 | Medium | Done |

### 4.12 电系链 L04

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L04_F01 | 皮卡丘 | **静电**：被敌对接触/近战打中时，反弹 Electrify（短 CD） | 电击 Bolt+Electrify（**撞墙**） | 电光一闪 Blink（指针最近敌；落点电爆伤；2s CD + 0.25s 无敌；一屏） | 十万伏特：一道向前曲折天雷（≤64 格）；50% Electrify；0.5s 后对感电敌再射 | 耗满；大招 ×**4.8**（§7.1） | `SkyBoltLightning` + `Assets/Fx/ThunderTrail`（抄 PRT_SkyBolt，无 CWR 运行时） | Medium | Done |
| L04_F02 | 雷丘 | **静电**：同上 | 十万伏特 Bolt+Electrify（可链式；**撞墙、穿怪**） | 伏特攻击 Lunge+Recoil+Electrify | 打雷：屏内每敌头上一道更粗落雷 | 耗 100；伏特 Recoil | 同天雷管线 ai0=1、更粗；落点 SoftGlow | Medium | Done |

### 4.13 岩/钢蛇链 L13

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L13_F01 | 大岩蛇 | **坚硬脑袋**：免疫 Recoil 自伤；防御 ×1.2 | 岩石封锁 Bolt+Slow | 撞击 Lunge | 岩崩 AoEBurst+Stun | 耗 100 | 大招岩崩 **12** 石、±12格、微偏（`|vx|≤8`）+落地1格爆 | Low | Done |
| L13_F02 | 大钢蛇 | **坚硬脑袋**：同上 | 岩崩 AoEBurst+Stun | 钢尾 龙尾式+铁色遮罩 | 猛撞 闪焰式冲+灰日耀（被动免 Recoil） | 耗 100；Recoil 被被动抵消 | `IronTailWhip`；`TakeDownLunge` | Medium | Accepted |

### 4.14 传说 L16 / L14 / L17

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L16_F01 | 洛奇亚 | **压迫感**：全招式伤害 +50% | 空气爆炸 三段脉冲+小光罩 | 神鸟猛击 0.5s 无敌吟唱后冲+6鸟 | 气旋攻击 半径32格 Field | 耗 100 | `AirBurst`；`SkyAttackLunge`；`HurricaneField` 名键 `CycloneAttack` | High | Accepted |
| L14_F01 | 超梦 | **压迫感**：+50% | 精神强念 胡地式×6穿墙 | 意念头锤 MeleeArc+Stun（技能槽 ×**2.0**） | 精神击破 指针16格选敌→8格渐显64暗影球齐冲 | 耗满 | `MewtwoPsychic`；`MewtwoPsystrike` IgnoreDef | High | Accepted |
| L17_F01 | 烈空坐 | **气闸**：全招式伤害 +70% | 龙之波动 Nebula×10 | 咬碎 BiteArc+DefDown | 画龙点睛 长星尘龙路径冲（纯黑骨节）；释放后 **防御 −20% 持续 5s** | 耗 100；防降后摇 | `StardustPathLunge` ai2=1 全黑 `0,0,0,255` | High | Accepted |

### 4.15 地鼠链 L10（Excel 外补全）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L10_F01 | 地鼠 | **沙隐**（补全）：地下/洞穴移速 +15%；`pickSpeed` −0.25（另叠地面属性 −0.40） | 掷泥 Bolt+Slow（泥浆降命中感→减速） | 撞击 Lunge | 挖洞爆发 Dig→AoEBurst（短潜行出土爆炸；吃挖掘预算） | 耗 100；挖砖需服务端校验 | 泥尘；出土碎屑 KillTile 尘 | High |
| L10_F02 | 三地鼠 | **沙隐**：同上 | 三连刺 Barrage/MeleeArc（三连戳） | 挖洞 Dig（短位移潜地，可接出土） | 地裂 AoEBurst/Field（地面裂伤波；可轻改地形须预算） | 耗 100；地形变更 High 同步 | 三连刺 **8 格**；挖洞 `DigLungeProj` 冲 **20 格**+镐力走廊挖砖；地裂波前小石 | High | Done |

---

## 5. 实现备注（给编码）

1. **键位：** 技能1=`None`（左键），技能2=`RightClick`，大招=`ModKeybind`。表中 Lunge/Charge 注意与原版右键交互的冲突等级。
2. **EasyCrit：** 抬高 `GetCritChance(HenshinDamage)`（+35）；与 A15 焦点镜升档分离——升档仅在已 Crit 时再 ×2（超暴 ×4，偏红橙大飘字）。
3. **Recoil：** 统一走安全自损；坚硬脑袋免疫；生命宝珠（A16）的 −1HP 与 Recoil 分开结算。
4. **Boss：** Sleep/长 Stun 须衰减；DoTBind 对 Boss 缩短时长或降 DoT。
5. **联机 High：** 挖洞、地裂、大型场、强位移须服务端生成与拒绝超预算。
6. **验收：** 逐形态 A1→玩法语义可辨；再对标同档武器 DPS 80%～120%（需求 §2.6）。
7. 未接线管线见 `docs/requirements.md` §12.1。

---

## 6. 变更记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-09-04 | 初版：Excel 全表泰拉适配 + 地鼠补全；Status=Design |
| 1.2 | 2026-09-05 | 点名 7 形态手感落地；能量 UI C |
| 1.3 | 2026-09-05 | 全 36 形态招式/大招按 Redesigned+Wave2 积木接线；Code 列 Done |
| 1.4 | 2026-09-06 | Stage 6+ FX 分批抛光：ThickBeam 模式/日光束金棱/花瓣壳环/Cyclone 涡等；见 `docs/fx-knowledge.md` |
| 1.5 | 2026-09-06 | Sheet 帧修复 + 射程/持续束：`SustainedBeam`/`WaterJet`/`DigLunge`；Fire/Flash/HitJagged 按帧 |
| 1.6 | 2026-09-06 | 水柱宽度/渐缩/流动；日光束约 2 格；龙之怒改球体连射（非光束）；见 `docs/fx-knowledge.md` 0.9 |
| 1.7 | 2026-09-06 | 大招能量条改角色脚下（非右下角）；满充金色向上发散尘约 1 格 |
| 1.8 | 2026-09-06 | Wave1 落地并验收：抓狂15格三线爪；火焰牙两对弧牙；闪焰32格多线火径+收尾减速；泡沫窄直线+破裂；念力 ShadowBeam 索敌弹射×2+命中紫环；龙波直线 |
| 1.9 | 2026-09-06 | Stage6 修：大岩蛇岩崩12石±12格微偏；豪力岩崩微偏；爆裂拳挥拳+20格石爆；流沙保Typhoon+琥珀叠层 |
| 1.10 | 2026-09-06 | Stage6 验收：岩崩加大微偏（`|vx|≤8`）+落地1格范围伤；整波 Accepted |
| 1.11 | 2026-09-06 | 岩崩落地去金光；污泥毒气瓶毒云；大比鸟三招重做；金属怪+喷火龙/妙蛙花/水箭龟 Accepted |
| 1.12 | 2026-09-06 | 暴风直立帧；哈克龙大招/快龙技能 WeatherPain；大招主+4伴随穿透牵引 |
| 1.13 | 2026-09-06 | 大比鸟/哈克龙/快龙暴风验收 Accepted |
| 1.14 | 2026-09-07 | Wave3 验收 Accepted：鬼斯通（含睡眠白 DrawEffects 255,255,255,100+zzZ）/怪力/哈克龙龙尾/胡地 |
| 1.15 | 2026-09-07 | Stage7+ 改版落地：钢尾/猛撞/暗影抓/恶波动/彗星拳/破灭自缓加粗/巨金怪强念=胡地/龙俯冲与画龙点睛星尘龙/逆鳞火球/流星群64 StarWrath/空气爆三段/神鸟吟唱/气旋32格 |
| 1.16 | 2026-09-07 | Stage7+ **Accepted**；画龙点睛纯黑；`CycloneAttack` 名键；超梦强念×6穿墙+精神击破64暗影球 |
| 1.17 | 2026-09-09 | 能量/倍率指针对齐 requirements **v1.4** + `docs/balance-stats.md`（当时文档先切、代码尚未切） |
| 1.18 | 2026-09-09 | 注明 MoveRefRate 柔性：段数慎改、按风险/命中难度偏置 |
| 1.19 | 2026-09-09 | v1.4 数值接线：等级/攻防/能量 1000；§7.1 皮卡丘大招 ×4.8、凯西/超梦技能槽意念头锤 ×2.0 |
| 1.20 | 2026-09-09 | 被动数字对齐 `FormPassiveApplier` |
| 1.21 | 2026-09-09 | 洁癖：去掉形态行上的否定口径；未接线见 requirements §12.1 |
| 1.22 | 2026-09-10 | 龙之波动爆炸碎片命中改为 `1 × Factor`（须 `MarkCrumb`；仅 620）；击杀能量不变 |
| 1.23 | 2026-09-10 | 诅咒之符去共鸣。火焰漩涡撞墙停飞不 Kill；水炮墙=怪锁长渐缩（加农仍穿）；电击/十万伏特/暗影球撞墙；龙息线截断；技能龙怒撞实心爆（迷你龙大招仍穿）；暴风默认贴地。喷射火焰/飞叶仍真弹，诅咒符 Spread `PostAI` 保穿墙 |
| 1.24 | 2026-09-10 | 暴风改为 Barrage 穿透飞弹（不是 Field）；广角镜仍不含 Field；诅咒符穿墙含 Beam、不含 Field |
