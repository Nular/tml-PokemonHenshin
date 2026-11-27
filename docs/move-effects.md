# 招式泰拉适配表（权威）

**Status：** Implemented（全形态接线）；Wave1～Wave3 / **Stage7+ 单机已验收**；肉后特效批 R2–R7（§4.16–§4.20）**本地已验收 2026-11-27，剩联机**；**v1.4 数值已接线**（含大招后 `UltEnergyLockout`；游戏内 DPS 抽检待本地）。联机瞄准/壳弹**代码已接线**，双端验收 pending（清单 `docs/fx-knowledge.md`）。  
**版本：** 1.3（表结构）；变更记录至 **1.47（2026-11-27）**  
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

对齐 requirements §1.5：**优先**原版复用 → 拷贝 CWR 至 `Assets/Fx/` → 可自制 FX。禁止大修运行时依赖；禁止擅自降级。Cookbook：`docs/fx-knowledge.md`；踩坑：`AGENTS.md`。

**技能效果专项（2026-11-27，未验收）：** 全部招式的施放音 / 命中音换成自制音效（`Assets/Sounds`），并加拖尾、白闪、震屏、爆炸配方、大招 4 tick 表现顿帧（`fx-knowledge` §4.14）。本表各招的伤害、判定、冷却、机制**都不变**；下文描述里写到的原版音效名（如 `Item14` 爆音、`Item122` 雷声）多数已被自制音替换，以代码为准。

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
| **Lunge** | 身体突进撞击（命中生成 `TackleHitFxProj` 冲击星，火系染橙；出招/刹车期间不沿线撒粒子） |
| **StrikeFall** | 跃起后下落砸击（鲤鱼王跃起用 `SplashFallProj` 水柱，`FormItemUtil.SplashStrike`；电系仍用 `StrikeFallProj`） |
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
| L01_F01 | 小火龙 | **猛火**：HP&lt;50% 时火系招式伤害 +20% | 火花 Bolt+OnFire（自绘 `EmberBolt` 4 帧火球，无拖尾粒子） | 抓 MeleeArc（三道**平行**爪痕+扩盒） | 火焰漩涡 HomingLock：自玩家射出，自绘 `FireSpin` 3 臂火旋涡（原 Typhoon 染色已弃），锁定首敌；**撞实心停飞但不 Kill**，跑完生命周期 | 耗 100；无额外后摇 | 三平行爪痕尘；EmberBolt / FireSpin 贴图+`_Light`（不生成灾厄弹） | Low | Done |
| L01_F02 | 火恐龙 | **猛火**：同上 +20% | 龙之波动 Nebula×10（0.7/不追踪/碰炸紫） | 火焰牙 BiteArc+OnFire（**VisualScale×4**） | 闪焰冲锋 Lunge+Recoil25%+OnFire | 耗 100；Recoil | 星云奥秘外观连发；紫染爆炸碎片；火焰牙**两对大弧牙×4**；闪焰冲锋 **32 格** Fire 帧环绕；出招无沿线粒子，命中 `TackleHitFxProj` 橙色冲击星 | Medium | Done |
| L01_F03 | 喷火龙 | **太阳之力**：白天全招式伤害 +20%；每次造成招式伤害自损 1 HP（不死于该扣） | 喷射火焰 FlameCone×10 | 龙爪 Scratch火（**20 格**） | 过热 MouseAoE+OnFire；**5s 伤×0.5** | 耗 100；5s 伤害减半 | 喷射：真 Flames 射程约 **54 格** + **Fire 帧**；龙爪 HitJagged **帧**；过热：半径 **15 格**、5 段脉冲 + Fire/FlashImpact **帧** | Medium | Done |

### 4.2 草系链 L03

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L03_F01 | 妙蛙种子 | **茂盛**：HP&lt;50% 时草系招式伤害 +20% | 藤鞭 Whip（皮鞭级；自绘 `VineWhip` 藤蔓逐段平铺，360° 朝鼠标；40 tick 蓄/甩/抽/收，鞭梢 tick 21「啪」+`VineWhipCrack`） | 撞击 Lunge（命中生成 `TackleHitFxProj` 冲击星） | 种子机关枪 Barrage×64+EasyCrit（`SeedGunDirectorProj` + 自绘 `SeedShot`，无拖尾） | 耗 100 | `VineWhip` 藤蔓鞭身；`SeedShot` 种子束状连发（无拖尾） | Low |
| L03_F02 | 妙蛙草 | **茂盛**：同上 +20% | 飞叶快刀 Spread×5（吹叶机 Leaf；枪口仅 5 颗草屑） | 咬住 BiteArc 尖牙（**VisualScale×4**） | 种子炸弹 Bolt+EasyCrit（自绘 `SeedBomb` 种荚；落点 `SeedBombBlastProj` 6 帧爆圈 AoE；落点爆直径 **~10 格**；无拖尾，仅 4 颗草屑） | 耗 100 | 真 Leaf + 枪口 5 颗草屑；咬合尖牙×4；种荚 + `SeedBombBlast` 6 帧爆圈（无金刺） | Low | Done |
| L03_F03 | 妙蛙花 | **叶绿素**：白天移速 +35% | 污泥炸弹 Bolt+Poison | 花瓣舞 AoEBurst；释放后自身短混乱（1.5s 轻失控或伤害反噬 debuff） | 日光束 ChargeBeam（蓄力 ~0.8s 后强光束） | 耗 100；蓄力前摇 | 污泥炸后毒气瓶 ToxicCloud 簇 DoT；花瓣半径15格；日棱金束 | Medium | Done |

### 4.3 水系链 L02

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L02_F01 | 杰尼龟 | **激流**：HP&lt;50% 时水系招式伤害 +20% | 水枪 AquaScepter（水弹贴图，飞行中无粒子） | 撞击 Lunge（命中生成 `TackleHitFxProj` 冲击星） | 泡沫光线 Barrage×64 束状高速 | 耗 100 | 海蓝权杖水流；泡泡**错落密束**+破裂小泡（少蓝水尘） | Low |
| L02_F02 | 卡咪龟 | **激流**：同上 +20% | 泡沫光线 Barrage×**16**（壳弹+Load Bubble；基准速 **20.8**） | 咬住 BiteArc | 潮旋 专属 `WhirlpoolUltProj`（168t：卷起 12t→旋转吸引 9 段（age 12–132，每 15t ×0.5，判定圈随卷起放大）→水柱 18t 托起 ×1.4；命中圆 9 格/吸力圈 18 格；Boss/击退免疫者不被吸；Slow+Wet） | 耗 100 | 须 `LoadProjectile(Bubble)`；错落密束+破裂；勿等玩家先用泡泡枪 | Low | Done |
| L02_F03 | 水箭龟 | **雨盘**：雨天或夜晚每 **90 tick**（1.5s）回 2 HP | 水炮 Beam（粗水柱；**命中墙=命中怪**锁长；**持续多段伤再淡出**，不穿透） | 波导弹 Bolt+强追踪（极慢攻速） | 加农水炮 Beam（**仍穿墙穿怪**，更粗）；释放后 **休整 ~1.5s**（禁技能1/2） | 耗 100；休整 | `WaterJetProj`：枪口渐进；水炮宽~2.5 格锁长多段；加农宽~4.5 格+每3击爆 | Medium | Done |

### 4.4 超能链 L12

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L12_F01 | 凯西 | **同步**：自身获得 OnFire/Poison/Electrify 时，最近敌对复制同 debuff（短 CD） | 念力 Bolt+Stun（弱，短硬直） | 意念头锤 StrikeFall（幽灵锤下砸 AoE，技能槽 ×**2.0**） | 精神强念 Scatter×32 彩虹杖可见弹；单发 ×0.6；生成 0.5s 后追踪 | 耗满 | 念力：`PsychicWaveBolt` ShadowBeamFriendly；**48 格**索敌、弹射下一目标（最多 2 击）；意念头锤；粉紫散射+延迟追踪 | Medium |
| L12_F02 | 胡地 | **同步**：同上 | 精神强念 鼠位×3 延迟追（不可穿墙/穿怪） | 真气拳 白气上扬无爆 | 预知未来 屏内夜光标记 1s 显形后追爆（IgnoreDef） | 耗 100 | 彩虹杖×3；FocusPunch；FairyQueenMagicItemShot | Medium | Accepted |

### 4.5 龙系链 L07

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L07_F01 | 迷你龙 | **蜕皮**：每 5s 约 15% 概率清除自身 1 个可清除 debuff | 龙息 Spread/Bolt+Stun（概率短僵；**线在实心截断**） | 咬住 MeleeArc | 龙之怒 Barrage（12/32 抖动球+5格爆；**大招仍穿墙**） | 耗 100 | 龙息：**128 格** Fire 帧线；怒：`DragonRageBarrage` SoftGlow 球 | Low | Done |
| L07_F02 | 哈克龙 | **蜕皮**：同上 | 龙之波动 Nebula×10 | 龙尾 星尘龙鞭 15格极强击退 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**；诅咒符 Barrage 穿墙） | 耗 100 | 龙波；`DragonTailWhip`；暴风 WeatherPain 主+**4伴随**穿透牵引 | Medium | Accepted |
| L07_F03 | 快龙 | **多重鳞片**：满 HP 时受到伤害 ×0.2（即减伤 80%）；掉血后失效至回满 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**） | **（1.42 起换为翅膀攻击：360° 两下挥扫 ×0.95×2，见 §4.16）** 旧：龙之俯冲 瞬抬升空→垂直砸 | 逆鳞 3s 身周火球；结束后 **自身混乱 ~2s** | 耗 100；混乱后摇 | `DragonDiveSlam`；逆鳞同前 | Medium | Done |

### 4.6 钢/超能链 L08（Excel：铁哑铃→金属怪）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L08_F01 | 金属怪 | **恒净之躯**：免疫本模关注的 debuff（着火/毒/感电/缓速等可列白名单） | 念力 Bolt+Stun | 臂锤 Kenney `fist_smash` 下砸（贴图再转 180°）+落地挤压 | 金属爪 鼠标双侧 `scratch_down` 镜像，±30°，自上而下划出/抹掉，持续伤直径≈16格 | 耗 100 | `Assets/Fx/Kenney`；无 FlashImpact 堆叠 | Low | Done |
| L08_F02 | 巨金怪 | **恒净之躯**：同上 | 精神强念 胡地式鼠位×3 | 彗星拳 真气拳式+3×StarWrath | 破坏光线 Beam 自缓加粗；释放后 **休整 ~2s** | 耗 100；休整 | `AlakazamPsychic`；`CometPunch`+StarWrath；`SustainedBeam` 粗 | Medium | Accepted |

### 4.7 龙/地链 L15

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L15_F01 | 圆陆鲨 | **粗糙皮肤**：受击反弹 **0.35×** 当前变身招式基准伤害；且受击后短时攻击 +10%（2s） | 龙之怒 Barrage（**技能球撞实心爆**） | 隐形岩 鼠标上空悬浮尖球陷阱 | 流沙地狱 DoTBind（沙漩涡） | 耗 100 | `StealthRockField`（尖球换色）；流沙同前 | Medium | Done |
| L15_F02 | 烈咬陆鲨 | **粗糙皮肤**：同上 | 龙之波动 Nebula×10 | 咬碎 鼠标处远距咬合+DefDown | 流星群 64×StarWrath；释放后 **自身攻击 −15% 持续 5s** | 耗 100；攻降后摇 | `DracoMeteorDirector` 狂星之怒壳+金粉爆 | Medium | Accepted |

### 4.8 幽灵链 L06

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L06_F01 | 鬼斯通 | **飘浮**：飞行能量视为强化（默认池 ×2.5，仍禁坐骑） | 暗影球 Bolt+DefDown（BrokenArmor；**撞墙**） | 舌舔 20格线+Stun（短硬直） | 催眠术 `HenshinSleepDebuff` 5s（屏内；Boss 改 Slow ~2s） | 耗 100 | 暗影球不透明紫盘链；舌舔 Extra98；睡眠 `DrawEffects(255,255,255,100)`+zzZ | Medium | Accepted |
| L06_F02 | 耿鬼 | **飘浮**：同上强化飞 | 污泥炸弹 Bolt+Poison | 暗影抓 紫龙爪20格+ShadowFlame（360°） | 恶之波动 32暗影球穿墙追踪爆 | 耗 100 | `ShadowClawSlash`；`DarkPulseBarrage`×32 | Medium | Done |

### 4.9 水/飞鱼链 L09

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L09_F01 | 鲤鱼王 | **优游自如**：雨天或夜晚移速 +25% | 跃起 StrikeFall→`SplashFallProj` 水柱（高跳砸地，低伤） | 撞击 Lunge（`TackleHitFxProj` 冲击星） | 抓狂 Barrage（爪痕改画 `Assets/Fx/Scratch`，不再拼 HitJagged）；伤害随 **已损失 HP%** 提高（最高约 +80%） | 耗 100 | `SplashColumn` 水柱跃起；抓狂（`Scratch` 贴图爪痕）：身周半径 **15 格** 杂乱交错爪痕多段 | Low |
| L09_F02 | 暴鲤龙 | **自信过度**：击杀叠攻 +20%/层，最多 2 层，每层 12s | 水炮 Beam（粗柱；**命中墙=命中怪**锁长多段） | 咬碎 MeleeArc+DefDown（**VisualScale×4**） | 破坏光线 Beam；释放后 **休整 ~2s** | 耗 100；休整 | `WaterJet` 跟鼠标；破灭 `SustainedBeam`（方向跟鼠标） | Medium | Done |

### 4.10 格斗链 L05

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L05_F01 | 腕力 | **毅力**：自身存在异常 debuff 时全招式伤害 +25% | 岩石封锁 Cross×4 收拢碎裂（Boulder 棕染 50%） | 子弹拳 短距快速鞭拳（判定略扩；自绘 `BulletPunch` 拳头+速度线，360° 朝鼠标） | 十字劈 X 形剑气 + 前飞 64 格穿透（`CrossSlash` 贴图，无粒子） | 耗 100 | `Assets/Fx/BulletPunch`、`Assets/Fx/CrossSlash` | Medium | Done |
| L05_F02 | 豪力 | **毅力**：同上 +25% | 岩崩 AoEBurst+Stun（概率） | 劈瓦 MeleeArc（对高防目标额外 +25% 伤；破「减伤 buff」语义） | 爆裂拳 MeleeArc+Stun（必短硬直） | 耗 100 | 岩崩3石微偏（`|vx|≤8`）+落地**1格**爆；劈瓦 **20 格**线斩；爆拳巨大拳套前挥+半径**20格**石爆 | Medium | Done |
| L05_F03 | 怪力 | **毅力**：同上 +25% | 尖石攻击 三角三刺穿透+命中爆 | 十字劈 360°弧形X | 近身战 高频+全程无敌+跟随；释放后 **防御 −20% 持续 5s** | 耗 100；防降后摇 | 三角 Boulder；贝塞尔弧 X；HitJagged 纠向 | Medium | Accepted |

### 4.11 飞行链 L11

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L11_F01 | 波波 | **锐利目光**：全招式伤害 ×1.2 | 起风 GroundCyclone（朝鼠标飞出；落地后水平贴地滚动；本体原版 Typhoon，云尘 1/5 tick） | 啄 Cone~16格尖角 AoE（自绘 `PeckBeak` 尖角喙刺，360° 朝鼠标） | 燕返 锁定后 5 次瞬身往返斩（每斩 ×0.6 + 终结爆 ×2，单目标合计 ≈4.2；整段无敌；不再共用撞击突进 CD；起终点在方块内则缩短半径（≥40px）、斩不穿墙；`AerialAceUltProj`+`AerialAceSlashProj`+`AerialAceBurstProj`；旧 `AerialAceLungeProj` 暂留未用） | 耗 100 | Typhoon 深蓝；射出后贴地滚 | Medium |
| L11_F02 | 大比鸟 | **锐利目光**：×1.2 | 暴风 Barrage 穿透飞弹+Stun（**默认撞实心贴地**） | **（1.42 起换为空气斩 ×1.9，穿透气刃 360°）** 旧：勇鸟猛攻 Lunge+Recoil（青焰，降倍率） | **（1.42 起换为热风 3 道扇形热浪 ×3.4）** 旧：自我激励 SelfBuff：攻击 +20% / 12s | 耗 100 | 勇鸟青焰梭形；大招纯 buff `WorkUpBuff` | Medium | Done |

### 4.12 电系链 L04

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L04_F01 | 皮卡丘 | **静电**：被敌对接触/近战打中时，反弹 Electrify（短 CD） | 电击 Bolt+Electrify **5s**（**撞墙**；戴电气球：弹速+100%/+200%、穿3、加宽；暖金；自绘 `ThunderShockBolt` 4 帧锯齿电弧（原 Kenney spark 已弃）；沿飞行方向判定长度 = 宽度×2） | 电光一闪 Blink（指针最近敌；身周自绘 `BlinkSpark` 4 帧 + 路径形态残影；落点贴身 **3** 段、单段 ×**3.0**；撞击冷却 **3s** + 0.25s 无敌；一屏；电气球冷却 -20%/-40%） | 十万伏特：向前曲折天雷（≤64 格）；50% Electrify；命中后 16 格内最多 **三叉**（分叉不再分）；0.5s 后对感电敌再射（不再分叉）；广角镜软瞄光标 4/8 格；暖金 `(255,168,28)` | 耗满；大招 ×**4.8**（§7.1）；闪 `MultiHit` Factor 0.25 | `SkyBoltLightning` 暖金 + 分叉 `ai2=2`；闪 `BlinkStrike` + `BlinkStrikeGhost` | Medium | Done |
| L04_F02 | 雷丘 | **静电**：同上 | 十万伏特 SkyBolt 向鼠标（≤**128** 格；穿怪；**不**挂感电二次；命中三叉同上；广角镜软瞄；暖金） | **（1.42 起换为放电 Discharge ×1.3：身周 8 向电弧 12 格、命中感电，见 §4.16）** 旧：伏特攻击：闪现到鼠标 + Recoil + i 帧；加粗闪电拖尾 60tick、面板×**3.0**、同怪最多 **6** 段 + 形态残影；暖金 | 打雷：屏内每敌头上一道更粗落雷；同暖金 | 耗 100；伏特 Recoil | S1=`ThunderboltSkill`；拖尾=`VoltTackleTrail`；大招落雷 ai0=1 | Medium | Done |

### 4.13 岩/钢蛇链 L13

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L13_F01 | 大岩蛇 | **坚硬脑袋**：免疫 Recoil 自伤；防御 ×1.2 | 岩石封锁 Bolt+Slow | 陀螺球 旋转钢环砸出（不变身；**无**撞击 CD/无敌） | 岩崩 AoEBurst+Stun | 耗 100 | 大招岩崩 **12** 石、±12格、微偏（`|vx|≤8`）+落地1格爆 | Low | Done |
| L13_F02 | 大钢蛇 | **坚硬脑袋**：同上 | 岩崩 AoEBurst+Stun | 钢尾 龙尾式+铁色遮罩 | **（1.42 起换为闪光炮 ×3.8：光弹 40% + 闪光爆 60%，见 §4.16）** 旧：舍身冲撞 加长猛撞+额外爆炸（被动免 Recoil） | 耗 100；Recoil 被被动抵消 | `IronTailWhip`；`DoubleEdgeLunge` 约44格 | Medium | Done |

### 4.14 传说 L16 / L14 / L17

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L16_F01 | 洛奇亚 | **压迫感**：全招式伤害 +50% | 空气爆炸（zh 名「气旋攻击」，key `Aeroblast`）朝鼠标路径爆（≤128格，撞墙贴壁停；半径4格；吸16格） | 神鸟猛击 **（1.42 起为聚光后冲出单只火鸟 ×2.4）** 旧：6只红鸟分波（**无**释放者无敌） | 暴风（1.42 起，key `Hurricane`；原名气旋攻击）半径32格 Field | 耗 100 | `AirBurst` 路径节点；`SkyAttackSextetDirector`+红鸟；气旋同前 | High | Done |
| L14_F01 | 超梦 | **压迫感**：+50% | 精神强念 胡地式×6穿墙 | 意念头锤 MeleeArc+Stun（技能槽 ×**2.0**） | 精神击破 指针16格选敌→8格渐显64暗影球齐冲 | 耗满 | `MewtwoPsychic`；`MewtwoPsystrike` IgnoreDef | High | Accepted |
| L17_F01 | 烈空坐 | **气闸**：全招式伤害 +70% | 龙之波动 Nebula×10 | 咬碎 鼠标处远距咬合+DefDown | 画龙点睛 闪现到鼠标（≤80格）→**立刻**青色 SoftGlow 散落光迹；路径上 **Life≈48 依次**反色 GraySolarBurst；生成时**单次**路径伤；释放后 **本模招式伤害 ×0.8 / 5s** | 耗 100；伤降后摇 | `BiteArcAtMouse`；`DragonAscentBlink`+`DragonAscentTrail`（cyan SoftGlow streaks；`GraySolarBurstVfxProj` invert/`ai0=1`） | High | Done |

### 4.15 地鼠链 L10（Excel 外补全）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L10_F01 | 地鼠 | **沙隐**（补全）：地下/洞穴移速 +15%；`pickSpeed` −0.25（另叠地面属性 −0.40） | 掷泥 Bolt+Slow（泥浆降命中感→减速） | 重踏 Bulldoze（Field；向鼠标所在左右侧贴地依次破土 9 道土浪，间距 30px、每 3tick 一道、约 18 格；台阶≤3 格爬升、坑≤10 格下探、更深/墙止步；同目标一次、×1.15、Slow 90t；`BulldozeProj`，原撞击已替换） | 挖洞爆发 Dig→AoEBurst（短潜行出土爆炸；吃挖掘预算；出土爆 `DigBurst` 土喷泉贴图，锚在出土点） | 耗 100；挖砖需服务端校验 | 泥尘；出土碎屑 KillTile 尘 | High |
| L10_F02 | 三地鼠 | **沙隐**：同上 | 三连刺 Barrage/MeleeArc（三连戳） | 挖洞 Dig（短位移潜地，可接出土） | 地裂 AoEBurst/Field（地面裂伤波；可轻改地形须预算） | 耗 100；地形变更 High 同步 | 三连刺 **8 格**；挖洞 `DigLungeProj` 冲 **20 格**+镐力走廊挖砖；地裂波前小石 | High | Done |

---

### 4.16 肉后 R2 重做批（1.42，2026-11-26；Stage≥5 的 24 个之力；本地已验收 2026-11-27，剩联机）

原则：肉后之力不再有身体冲撞类（`Lunge` / `LungeProj` / `TakeDownLungeProj`）招式。下表「换招」= 招式本身换了，「重做」= 机制、倍率、判定、冷却不变只换视觉与粒子。贴图均为 `Assets/Fx/` 自绘，沿线粒子基本删除，命中只留 1～3 颗。

| 之力 | 槽 | 旧 → 新 | 类型 | 说明 |
|------|----|---------|------|------|
| 喷火龙 | 大招 | 过热 | 重做 | `OverheatBlastProj`：6 帧爆炎环（160px，×3=480 判定圈不变） |
| 水箭龟 | 技能1 / 技能2 / 大招 | 水炮 / 波导弹 / 水炮大招 | 重做 | `WaterStream`/`WaterCannon` 平铺水柱 + `WaterSplash` 命中；`AuraSphere` 蓝球 + `AuraSphereBurst`；追踪逻辑不变 |
| 妙蛙花 | 技能2 / 大招 | 花瓣舞 / 日光束 | 重做 | 双圈反向旋转花瓣 `Petal`；蓄力 `SolarOrb` 太阳球 + 单道粗柱 `SolarBeamBody`（原 5 线棱镜束） |
| 雷丘 | 技能2 | 伏特攻击 → **放电** | **换招** | 原为闪现冲撞；新 `H2DischargeProj`：身周 8 向电弧 12 格，命中感电（`H2Moves.Discharge`，×1.3）。**1.43 起回退为伏特攻击（`VoltTackleBlink`），放电类保留未接线** |
| 豪力 / 怪力 | 技能2 / 大招 | 劈瓦、爆裂拳 / 十字劈（短）、近身战、尖石 | 重做 | `BrickBreak`、`DynamicFist`、`FlurryHit`、`StoneSpike`；十字劈复用 `CrossSlash` |
| 鬼斯通 / 耿鬼 | 全 | 暗影球、舌舔、催眠术 / 污泥炸弹、恶之波动 | 重做 | `ShadowBall`、`Lick`、`Hypnosis`、`SludgeBomb`、`DarkPulse`；暗影爪不动 |
| 胡地 / 超梦 | 全 | 精神强念、真气拳、预知未来 / 精神强念、精神击破 | 重做 | `Psychic`、`FocusPunch` 等，念球/念力环；超梦球为青蓝版 |
| 金属怪 / 巨金怪 | 技能2 / 大招 | 臂锤、金属爪 / 彗星拳 | 重做 | `ArmHammer`（落点改 360° 朝鼠标）、`MetalClaw` 6 帧、`MeteorFist` |
| 大岩蛇 / 大钢蛇 | 技能2 / 大招 | 陀螺球、岩崩、铁尾 | 重做 | `GyroDisc`、`RockFall`、`IronTail`（`DragonTailWhipProj` 内铁尾分支） |
| 大钢蛇 | 大招 | 舍身冲撞 → **闪光炮** | **换招** | 原为身体冲撞；新 `FormItemUtil.FlashCannonUlt`（`FlashCannonOrb`/`FlashCannonBlast`），总倍率 ×3.8：**光弹占 40%、闪光爆占 60%**（同一目标合计 100%；爆圈 5.5 格半径会打到圈内多个敌人，其余敌人各吃 60%）。**1.43 起见 §4.17：光弹 35% 穿透 4 + 闪光爆 50%（8 格）+ 余辉 5×8%，单体 ×4.75**。光弹按 `Bolt`、爆圈按 `AoEBurst` 交付 |
| 三地鼠 | 技能2 / 大招 | 挖洞 / 地震 | 重做 | `H6DirtBurst` 出入口；`H6QuakeRock` 两侧岩刺依次顶起 |
| 圆陆鲨 / 烈咬陆鲨 | 技能1 / 大招 | 隐形岩、流沙地狱 / 流星群 | 重做 | `H6StealthShard`（判定 88×88 对齐，联机偏移改首 tick 校正）、`H6SandTomb`（半径 128px）、`H6Meteor` |
| 迷你龙 / 哈克龙 / 快龙 | 技能1 / 大招 | 龙息、龙之怒、龙尾、逆鳞 | 重做 | `DragonOrb`/`DragonBurst`/`OutrageOrb`/`OutrageBurst`，龙尾去重击粒子 |
| 快龙 | 技能2 | 龙之俯冲 → **翅膀攻击** | **换招** | 原为垂砸冲撞；新 `WingAttackProj`：360° 朝鼠标，24 tick 两下挥扫，每目标只中 2 次（`localNPCHitCooldown` = `Stroke2Start` = 12），×0.95×2 = 1.9（同旧倍率） |
| 大比鸟 | 技能2 | 勇鸟猛攻 → **空气斩** | **换招** | 原为冲撞 + 6 鸟；新 `H7AirSlashProj` 射出穿透气刃（`AirSlashBlade`），×1.9（复查后由 2.2 降；同档远程技能无代价） |
| 大比鸟 | 大招 | 自我激励 → **热风** | **换招** | 原为 0.01 倍 buff；新 `H7HeatWaveDirectorProj` 3 道扇形热浪，总 ×3.4（每道 damage/3 四舍五入，至少 1；Bulbapedia/PokémonDB：大比鸟可学热风，Gen5 教学、Gen9 TM68） |
| 大比鸟 / 快龙 | 技能1 | 暴风 | 小改 | `WeatherPainHurricane` 裸 `LoadProjectile` → `SafeLoadProjectile` |
| 洛奇亚 | 技能1 | 空气爆炸 → 气旋攻击（中文名） | 重做 | 机制 `AirBurst` 不变，改画 `WindRing`；本地化 key 仍 `Aeroblast`（en `Aeroblast`，zh 值改「气旋攻击」） |
| 洛奇亚 | 技能2 | 神鸟猛击 | 重做 | 6 只红鸟 → `H7SkyAttackProj` 前摇聚光后冲出单只火鸟（`SkyAttackBird`），×2.4。**1.43 起回退为旧版 `SkyAttackLungeProj`：吟唱→冲撞→6 只火鸟贴图小鸟（`H7SkyBirdCrossProj`），×1.8（见 §4.17）** |
| 洛奇亚 | 大招 | 气旋攻击 → 暴风（名） | 重做 | `HurricaneField` 改 `WindRing` 旋转风场；本地化名 → `Hurricane` |
| 烈空坐 | 大招 | 画龙点睛 | 重做 | 路径删上百次 SoftGlow，改 `DragonStreak` 龙影；闪现不变；**1.43 起：路径一击 ×0.6 + 10 个爆点各 ×0.15 的 AoE（`DragonAscentBlastProj`）；旧“10 次延迟爆有伤害”为误写，爆点此前从未有伤害** |
| 其余（暗影爪、龙之波动、咬碎、破坏光线、念力、岩石封锁等） | | | 保留 | 已验收共用类，未动 |

Tooltip 同步：雷丘（放电）、大钢蛇（闪光炮）、快龙（翅膀攻击）、大比鸟（空气斩、热风）、洛奇亚（zh 气旋攻击、神鸟猛击、暴风 / en Aeroblast, Sky Attack, Hurricane），zh/en 已改。死代码（无招式引用，待清理，未删）：`DoubleEdgeUlt` / `DoubleEdgeLungeProj`、`BraveBirdSkill` / `BraveBirdLungeProj` / `BraveBirdCrossProj`、`WorkUpUlt` / `WorkUpBuffProj`、`VoltTackleBlink` / `VoltTackleBlinkProj` / `VoltTackleTrailProj`、`DragonDiveSlam` / `DragonDiveSlamProj`、`SkyAttackSextet` / `SkyAttackSextetDirectorProj` / `SkyAttackRedBirdProj`、`MouseVortexProj`。

### 4.17 肉后 R3 批（1.43，2026-11-27；本地已验收，剩联机）

| 招式 | 改动 | 倍率 |
|------|------|------|
| 喷射火焰（喷火龙） | 自伤来自白天被动「太阳之力」每命中扣 1 HP；加 `SolarDrainGate` 每 30 tick 至多 1 次 | 不变 |
| 伏特攻击（雷丘技能2） | 回退（放电弃用），Tooltip 同步 | 旧值 |
| 咬碎（远距，烈咬陆鲨/烈空坐） | 改用近身同款 `BiteJaws` 贴图，删 MagicPixel 方块牙 | 不变 |
| 龙之波动 | 自绘 `PulseBolt` 弹体、`PulseHit` 命中环、`PulseShard` 碎片；删飞行尘与死亡尘 | 不变 |
| 龙尾 / 钢尾 | `DragonTailWhipProj` 重写为 GrassWhip 式真鞭（蓄→甩→抽→收，Lifetime 36，tick 18 啪）；方向由 `ai2` 同步 | 1.35 / 1.6 |
| 翅膀攻击 | 第一击 ×0.8 + 第二击 ×1.1（射程 ×1.2、击退 ×1.8、Slow 90 tick），风刃/羽毛纯绘制 | 总 1.9 |
| 画龙点睛 | 路径一击 ×0.6 + 10 个爆点各 ×0.15（半径 88 AoE） | 单目标约 4.5～5.25 |
| 水炮 | 长度 64→88 格；喷出 10 tick；连发不重置；转向限速 0.16 rad/tick | 不变 |
| 流沙地狱 | 判定半径 128→176 px，吸力 224→400 px，改为直接位移（不穿墙） | 不变 |
| 地震 | 重写为逐刺落点判定（爬台阶 3 格、下坑 10 格），射程 392→480 px；刺贴图 80×68 | 3.6 |
| 岩崩 | 落石/碎石圈 `HandlesOwnHoming`，不再被广角镜追踪抖动；落石贴图重画 | 不变 |
| 劈瓦 | 手刀自上而下弧形劈砍，扇形 11 射线 180 px（射程由 20 格缩为 11.25 格）；360° 朝鼠标 | 1.4 |
| 闪光炮 | 光弹 ×0.35 穿透 4；闪光爆 ×0.50 半径 8 格；余辉 `FlashCannonGlowProj` 5 跳×0.08 | 单体 1.25×3.8 = 4.75 |
| 念力波纹（精神强念 / 预知未来） | 3 道同心弧波纹弹，按形态变色（胡地金 / 巨金怪青 / 超梦紫，variant 存 `ai[1]`） | 不变 |
| 暗影球 | 弹体缩小，28 格内最多弹跳 5 次，每次 ×0.8 | 单体 1.35，全中约 4.98 |
| 意念头锤 | 自绘 `ZenHammer` 贴图，缩放 1.6，落锤念力环 | 2.0 |
| 真气拳 | 蓄力光环、拳影、破防 4 秒、击退 +4、震屏 | 1.8 |
| 神鸟猛击（洛奇亚） | 回退旧版 `SkyAttackLungeProj` + 6 只火鸟贴图小鸟 | 1.8 |
| 空气斩 | 不再贴地被擦掉（弹心埋入实心块连续 >6 tick 才散）；身后 `AirStreak` 残痕，命中两侧闪光 | 1.9 |
| 热风 | 3 道扇形 ±0.17 rad，判定 262 px，不撞墙消失；`HeatWaveStreak` 气流带；灼烧 240 tick | 3.4 |
### 4.18 肉后 R4 批（1.44，2026-11-27；本地已验收，剩联机）

| 招式 | 改动 | 倍率 |
|------|------|------|
| 无限大招（调试物品） | 去掉合成配方，只能由调试手段获得；本地化补 `UltChargeDebug` 提示 | - |
| 劈瓦 | 判定改为贴图同款弧线：`ArcR=175`、`ArcWidth=44`，12 条射线按当前帧的弧头/弧尾角度判定（`FrameEndDeg`），与 `PreDraw` 同帧 | 1.4 不变 |
| 龙之波动 | 命中碎裂：删 12 枚小碎片，改生成 1 枚 `DragonPulseBlastProj`（半径 120 px AoE，`PulseBlast.png` 6 帧）；原碎片每枚 0.65x | 不变（爆点约 1.5x） |
| 翅膀攻击 | 新手绘 `WingFeather` 羽毛（4 帧）；第二击加 `WingFeatherPullProj`：0 伤害、40 tick，前 30 tick 把半径 22 内小怪拉向身前；Boss 与 `knockBackResist<=0` 不拉；仅服务端/单机改位置 | 总 1.9 不变 |
| 逆鳞 | 持续 180→132 tick，发射间隔 8→4 tick，总发数 22→33；单发 ×0.7；`OutrageUlt.UseTime` 190→142 | 总伤约 +5% |
| 念力波纹 / 精神强念 / 预知未来 | `H3Fx.DrawRipple`：本体 + 2 圈回波，按形态变色（胡地金 / 巨金怪青 / 超梦紫）；命中 / 撞墙环同色 | 不变 |
| 彗星拳（落星） | 新手绘 `CometStar` 贴图，替换 StarWrath+PinkTorch 拼贴 | 不变 |
| 胡地（金色统一） | 精神强念、预知未来预兆、真气拳环一律金色 `RingGold` | 不变 |
| 精神击破 | 自动索敌，60 tick 无目标自毁；`ai0 = whoAmI+1` 同步 | 4.8 不变 |
| 水炮 | 持续引导：按住期间只存在 1 道水柱（连发不再各开一道）；松手淡出 8 tick；截止于方块或首个 NPC 外缘（不穿透）；每 3 tick 判定一次；水花仅在截断端生成 | 不变（DPS 约 34/秒，需实测） |
| 热风 | 不再被广角镜追踪（`HandlesOwnHoming`）；`localNPCHitCooldown` 60→8，总伤分摊 `damage/3`→`damage/8` | 总倍率 +12% |
| 岩崩（落石） | `H5FallingRockProj` 广角镜追踪命中后停追（`ai[2]=1`），距目标 <40 px 不再转向 | 不变 |

### 4.19 劈瓦 / 岩崩 / 画龙点睛 / 翅膀攻击 / 预知未来 R5（1.45，2026-11-27；本地已验收，剩联机）

| 招式 | 改动 | 伤害 |
|------|------|------|
| 劈瓦 | 判定改为实心扇形（圆心=玩家 MountedCenter，半径 184 px，角度 -100° 到当前帧末角 +2°，随帧累积）；40 px 内整圈必中，贴脸可中；目标取 7x7 采样点+最近点；`BrickBreak.png` 5 帧 400x400 与判定同半径；`localNPCHitCooldown=Lifetime(20)` 同一敌人只中一次 | 不变 |
| 岩崩（岩石） | `H5FallingRockProj` 首次命中后 `ai[2]=1`（netUpdate 同步），此后每 tick 强制 `Homing=false`、`HomingTargetWhoAmI=-1`，不再被广角镜改向 | 不变 |
| 画龙点睛 | 路径爆炸 `BurstCount` 10→20（每 2 tick 一个）；`DragonAscentBlastProj.idStaticNPCHitCooldown` 14→16；爆炸占比 0.12、路径占比 0.6 不变；每颗爆炸粒子 3→1 | 单体上限 (0.6+3×0.12)×5.0=4.8 倍，在 5.0 内 |
| 翅膀攻击 | 羽毛汇集 30 tick 结束时，拥有者端生成 `WingFeatherBurstProj`（半径 90 px，5 帧 `WingBurst.png`，每目标一次，不加粒子）；牵引弹带 `BurstDamage = damage×0.7/0.95` | 汇集爆炸 0.7 倍 |
| 预知未来（夜光弹） | `FlightSpeed` 11→16.5，转向 lerp 0.22→0.33（+50%） | 不变 |

### 4.20 劈瓦动效 / 岩崩防上飞 R6–R7（1.46，2026-11-27；本地已验收，剩联机）

| 招式 | 改动 | 伤害 |
|------|------|------|
| 劈瓦（豪力技能2，`BrickBreakProj`） | 判定不动（`ArcR=184`、`ArcStartDeg=-100`、`FrameEndDeg={-62,-30,6,38,38}`、贴脸 40 px 必中、帧 4 不判、同敌一次）。`BrickBreak.png` 重画为 5 帧 400×400：只画发光手刀、外缘细弧斩痕、落点冲击星，扇形内部不填色、无网格/抖点/粒子（R6）；F0–F3 加一条从玩家身后连到掌根的前臂，补上手与身体间的空白（R7） | 不变 |
| 岩崩（`H5FallingRockProj`，豪力/大岩蛇/大钢蛇共用） | 修广角镜下偶发弹飞上天：拥有者端石头与任一敌对 NPC 重叠、或越过锁定目标中心高度即永久停追（`ai[2]=1` + `netUpdate`）；追踪方向夹在竖直向下 ±55°（`MaxSteerDeg`）；每 tick `velocity.Y ≥ 6`（`MinFallSpeed`）；删旧 40 px 停转区。诅咒之符 super 穿地下落为饰品设计，未改 | 不变 |

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
| 1.25 | 2026-09-12 | 全体远程飞弹（Bolt/Barrage 外飞壳）寿命约 ×1.5；念力索敌 **32→48 格**；暴风 `SuckRange` 同步 ×1.5；不改初速/伤倍 |
| 1.26 | 2026-09-13 | 电气球成品/超级：电击×4+穿3、3s/60格感电无连锁十万伏特×1；广角镜普通/超级瞬转；雷丘十万伏特=SkyBolt≤128无连锁；伏特闪现+拖尾×3/6段；卡咪龟泡×16速+30% |
| 1.27 | 2026-09-13 | 十万伏特命中三叉（16格、最多3、分叉不再分）；电击感电 5s；伏特拖尾加粗+形态残影；广角镜/超级十万伏特光标软瞄 4/8 格 |
| 1.28 | 2026-09-13 | 电气球电击弹速改为 +100%/+200%、加宽；攻速 +20%/+40%；电光一闪冷却 -20%/-40%（本地验收 OK） |
| 1.29 | 2026-09-13 | 手感批：种子炸弹直径~10格+草绿爆；尖牙 VisualScale×4；起风朝鼠标飞出后贴地滚；水炮/加农粗×2、水炮锁长持续多段；喷射火焰射程×2 |
| 1.30 | 2026-09-14 | 种爆去 FlashImpact 金刺（仅绿）；起风确认贴地滚；本批本地验收 OK |
| 1.305 | 2026-09-15 | 撞击族改版：臂锤/金属爪/波导弹/舍身冲撞/陀螺球/隐形岩/子弹拳；大比鸟勇鸟降技能2+自我激励；快龙垂砸；烈空坐伏特式画龙；洛奇亚六红鸟；烈咬远距咬 |
| 1.31 | 2026-09-25 | 改版抛光：烈空咬碎跟鼠标；画龙路径灰日耀/Flash 闪光爆；空气爆炸改路径≤128/半径4/吸16；神鸟/陀螺球去误用无敌与撞击 CD；子弹拳扩线判；臂锤/金属爪接 FlashImpact·LightShot |
| 1.32 | 2026-09-25 | 臂锤/金属爪改接 Kenney Particle/Smoke（非堆叠）；子弹拳恢复原特效仅略扩判定；画龙路径闪光改 Kenney star |
| 1.33 | 2026-09-25 | 金属爪：镜像 scratch 渐进绘制+持续伤16格；臂锤：Kenney fightFist 下砸拳头 |
| 1.34 | 2026-09-25 | 画龙：闪现立刻青光迹 + 路径延迟依次反色日耀爆 + 单次路径伤；线段个体再 +100%；金属爪 ±30°+自上而下抹掉；臂锤贴图转 180°+落地挤压；后摇口径改为伤倍 ×0.8（非防御） |
| 1.35 | 2026-09-26 | 皮卡丘电光一闪：Kenney spark 身周；单段 ×3.0、同单位 3 段；撞击冷却 2s→3s（`LungeCooldownTicks` 180）；`BalanceTag.MultiHit` |
| 1.36 | 2026-09-27 | 皮卡丘/雷丘电系招式统一暖金 `SparkGold`：十万伏特、落雷、伏特攻击、电击 |
| 1.37 | 2026-09-27 | 暖金压深 `(255,168,28)` / 芯 `(255,206,64)`；电光一闪路径形态残影（无伤）；电击沿飞行方向判定×2（用户确认） |
| 1.38 | 2026-09-27 | 撞墙反馈批（语义/数值不变，仅加反馈，见 `fx-knowledge` §6「撞墙反馈」）：电击、水枪、掷泥、种子机关枪、胡地精神强念、火焰漩涡、起风、暴风、水炮墙截断、龙息墙截断；穿墙弹保持原样 |
| 1.39 | 2026-11-26 | 360° 近战批：三连刺、抓、龙爪、咬住、火焰牙、咬碎（近身）、暗影抓由只能左右改为**朝鼠标 360°**；换自绘贴图+命中特效，出招不撒沿线粒子（数值/判定长度不变，见 `fx-knowledge` §5.3）；本地验收通过，联机 pending |
| 1.40 | 2026-11-26 | 肉前特效批：火花/火焰漩涡、撞击（非火系）/跃起（鲤鱼王）/抓狂、藤鞭/种子机关枪/种子炸弹、电击/电光一闪/啄/燕返、子弹拳/十字劈/挖洞爆发换自绘贴图；水枪/起风/飞叶快刀/岩石封锁/掷泥/意念头锤只减粒子（数值/判定/冷却不变，见 `fx-knowledge` §4.9）；构建通过；复查修复：火花/火焰漩涡辉光按帧、火系冲撞去沿线粒子、电击/水枪去飞行粒子；本地验收与联机 pending |
| 1.41 | 2026-11-26 | 肉前特效批 R2（构建待游戏关闭后验）：卡咪龟大招潮旋重做为 `WhirlpoolUltProj`（168t 卷起/旋转吸引/水柱，吸力只在服务端）；妙蛙种子藤鞭重做为 40 tick 延迟挥角鞭（`GrassWhipProj`，tick 21「啪」）；波波大招燕返改为锁定 5 斩+终结爆（`AerialAceUltProj`，单目标≈4.2×）；地鼠技能2 由撞击改为重踏 `BulldozeProj`（贴地 9 道土浪）；详见 `fx-knowledge` §4.9；本地验收与联机 pending |
| 1.42 | 2026-11-26 | 肉后 R2：24 个之力重做 FX；换招 6 处（雷丘放电、大钢蛇闪光炮、快龙翅膀攻击、大比鸟空气斩/热风）；肉后无冲撞类招式；见 §4.16；构建通过，游戏内/联机 pending；R3 复查修：翅膀攻击每目标 2 次、空气斩 ×1.9、闪光炮 40/60 与 Delivery、洛奇亚 A key 回 `Aeroblast`、热风取整 |
| 1.43 | 2026-11-27 | 肉后 R3 批：见 §4.17（龙系/水地/岩崩劈瓦闪光炮/超能鬼/飞行组重做，雷丘与洛奇亚技能2回退，喷射火焰自伤限频）；C# 构建通过，游戏内/联机 pending |
| 1.44 | 2026-11-27 | 肉后 R4 批：见 §4.18（水炮持续引导、热风/岩崩广角镜排除、龙之波动爆点、翅膀牵引、逆鳞提频、念力波纹三色、精神击破索敌、劈瓦弧线判定） |
| 1.45 | 2026-11-27 | R5 五项修订：见 §4.19（劈瓦实心扇形判定、岩崩命中停追、画龙点睛 20 爆、翅膀羽毛汇集爆炸、预知未来提速） |
| 1.46 | 2026-11-27 | R6–R7：见 §4.20（劈瓦手刀+斩痕+前臂贴图、岩崩防上飞）；§4.16–§4.20 肉后特效批 R2–R7 用户本地验收通过，剩联机 |
| 1.47 | 2026-11-27 | §1.3 补技能效果专项说明（自制音效 + 拖尾 / 打击感 / 爆炸，玩法不变，原版音效名以代码为准）；未验收 |
