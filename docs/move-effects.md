# 招式泰拉适配表（权威）

**Status：** Implemented（点名招式已按表落地并经手感迭代；全表仍待游戏内验收；未点名大招精修 pending）  
**版本：** 1.2（2026-09-05）  
**冲突处理：** 与 `docs/requirements.md` 冲突时以需求为准；**已实现招式**以代码为准并回写本表；未实现行仍以本表为设计规格。

---

## 1. 引言

### 1.1 源表

权威输入：[宝可梦.xlsx](file:///c:/Users/yuanu/xwechat_files/wxid_c4olaula6xc522_09c6/temp/RWTemp/2026-09/d03ae102dff8b4de328d76495433270f/宝可梦.xlsx)  
列：姓名 · 被动 · 技能1 · 技能2 · 大招。Excel 文案为宝可梦语义，本文件给出**泰拉瑞亚可验收**的落地规格。

- Excel「铁哑铃」→ 本模 **金属怪**（`L08_F01`）。
- **地鼠线**（`L10_F01` / `L10_F02`）不在 Excel 中，按同结构补全（挖掘向），表中标注「补全」。

### 1.2 战斗模型

持握「{宝可梦}之力」期间：

| 槽位 | 输入 | 说明 |
|------|------|------|
| **被动** | 持续 | 持握即生效；可与属性情境弱加成并存，持握被动优先 |
| **技能1** | 默认左键 | 主输出或主题招式 |
| **技能2** | 默认右键 | 副招式（近战/突进/辅助等） |
| **大招** | Mod 热键 | 仅当该形态能量满后可释放 |

取消持握：立刻失去外观、被动、招式与大招可用性；能量按 `FormId` **分存**，解除变身**暂停积攒**（不清空，除非饰品/规则另有说明）。

### 1.3 特效（FX）约束

- **禁止**为本模新增专用特效图片（无自制 dust 贴图、弹幕序列帧、光效图集）。
- 视觉必须：复用原版 / 复用或参考 Calamity / **只读参考** `CalamityOverhaul` 的 Dust·弹幕写法与参数组合（不引入该模组运行时依赖，不拷贝其玩法内容）。
- 表中「VFX note」只指定手法与参考方向，不授权新贴图。

---

## 2. 能量规则摘要

| 项 | 默认值 |
|----|--------|
| 满值 | **100**（每 `FormId` 独立） |
| 大招消耗 | 一次耗尽 **100**（A20 余韵挂坠：释放后保留 20%） |
| 命中积攒 | 技能1/2 命中敌对 +**小额**（建议 3～6 / 有效命中，有内置 CD 防弹幕刷） |
| 击杀积攒 | +**中额**（建议 12～20） |
| 自然积攒 | 持握且近期有战斗：缓慢涨；Boss 战可加快；挂机几乎不涨 |
| 释放键 | `ModKeybind`（可配置；未满提示不足） |
| 联机 | 能量与大招释放 **服务端权威** 同步 |
| UI | Tooltip 显示该形态能量；物品图标底栏进度条；变身时屏幕**右下角**怒气风格条（无新 UI 贴图） |

饰品修正（仅变身生效）：A19 命中能量 +30%；A20 大招后留 20%；A21 大招伤害 +25% 且非大招能量获取 −20%。讲究头带（A14）禁用技能2与大招。

**后摇 / aftermath：** 部分大招附带「休整」「伤害减半」「自损」「防御下降」等，写在表列「Energy/aftermath」；实现为短时自 debuff 或固定自伤，不受「坚硬脑袋」以外的反伤被动影响（自损类见各行）。

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
| **Field** | 天气/地形场（接 `WeatherField` 等） |
| **SelfGuard** | 自身减伤/护盾类短 buff |
| **SelfBuff** | 自身增益（攻/速等） |
| **Phase** | 短时穿障（本表 **鬼斯通线不采用**；Excel 无穿障招） |
| **Dig** | 挖洞位移 / 出土爆发（接挖掘预算） |
| **Blink** | 短距瞬移（本表仅作备用代号；Excel 当前形态未用） |
| **EasyCrit** | 易暴：提高暴击档或「易双倍」；与焦点镜（A15）叠乘规则：先 EasyCrit 判定，再滚 10% 升档 |
| **Recoil** | 命中后自损（比例或固定）；「坚硬脑袋」可免 Recoil 自伤 |
| **Stun** | 短硬直 / 无法行动（对 NPC：停 AI 或减速极强，时长短） |
| **Sleep** | 更长无法行动（催眠）；Boss 大幅衰减时长或改为强减速 |
| **DefDown** | 目标临时易伤 / 降防 |
| **Slow** | 目标减速 |
| **OnFire / Poison / Electrify** | 着火 / 中毒 / 感电类 debuff |
| **IgnoreDef** | 伤害结算无视或部分无视目标防御 |
| **TrueMelee** | 标注真近战时才吃真近战加成（默认多数 MeleeArc 仍走 HenshinDamage） |

**漩涡类**（火焰漩涡、潮旋、流沙地狱）一律 **DoTBind**，不做真束缚物理。

**鬼斯通特记：** 被动「飘浮」= **强化飞行能量**（近似无限飞，仍禁坐骑），**不是**永久穿障。舌舔用短 **Stun**，不用 Phase。大招为催眠术。形态 **不**因旧设计保留 `GrantsPhasing` 招式；若代码仍有穿障标志，以实现本表为准改为关闭。

---

## 4. 全形态适配表

列说明：

- **Passive**：可验收数值。
- **Skill1 / Skill2 / Ultimate**：玩法代号 + 简述。
- **Energy/aftermath**：大招耗能与后摇。
- **VFX note**：无新图前提下的手法。
- **Net risk**：Low / Medium / High（位移、场地、挖砖、穿障、多段同步越高）。

所有行 Status = **Design（待实现验收）**。

### 4.1 火系链 L01

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L01_F01 | 小火龙 | **猛火**：HP&lt;50% 时火系招式伤害 +20% | 火花 Bolt+OnFire | 抓 MeleeArc（三道**平行**爪痕+扩盒） | 火焰漩涡 HomingLock：自玩家射出，原版 Typhoon 橙红染色，锁定首敌 | 耗 100；无额外后摇 | 三平行爪痕尘；台风染色火矢（不生成灾厄弹） | Low |
| L01_F02 | 火恐龙 | **猛火**：同上 +20% | 龙之波动 Beam（龙系冲击波） | 火焰牙 MeleeArc+OnFire | 闪焰冲锋 Lunge+Recoil+OnFire（自损约造成伤害的 25%） | 耗 100；Recoil | 龙波：紫/火尘柱；冲锋：身周火尘+突进残影 | Medium |
| L01_F03 | 喷火龙 | **太阳之力**：白天全招式伤害 +25%；每次造成招式伤害自损 1 HP（不死于该扣） | 喷射火焰 Spread/Bolt+OnFire（强焰） | 龙爪 MeleeArc | 过热 AoEBurst+OnFire；释放后 **5s 本模伤害 ×0.5** | 耗 100；5s 伤害减半 | 过热：大范围火爆尘+灾厄火系参数参考 | Medium |

### 4.2 草系链 L03

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L03_F01 | 妙蛙种子 | **茂盛**：HP&lt;50% 时草系招式伤害 +20% | 藤鞭 Whip（皮鞭级草尘鞭） | 撞击 Lunge+撞击爆 | 种子机关枪 Barrage×64+EasyCrit | 耗 100 | 草/叶绿尘鞭身；Seed 束状连发 | Low |
| L03_F02 | 妙蛙草 | **茂盛**：同上 +20% | 飞叶快刀 Spread（叶片扇形） | 咬住 MeleeArc | 种子炸弹 Bolt/AoEBurst+EasyCrit（落点小爆） | 耗 100 | 叶刃尘；种子落地 AoE 尘 | Low |
| L03_F03 | 妙蛙花 | **叶绿素**：白天移速 +35% | 污泥炸弹 Bolt+Poison | 花瓣舞 AoEBurst；释放后自身短混乱（1.5s 轻失控或伤害反噬 debuff） | 日光束 ChargeBeam（蓄力 ~0.8s 后强光束） | 耗 100；蓄力前摇 | 污泥：毒尘；花瓣：粉尘环；日光束：原版叶绿光束类尘线 | Medium |

### 4.3 水系链 L02

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L02_F01 | 杰尼龟 | **激流**：HP&lt;50% 时水系招式伤害 +20% | 水枪 AquaScepter | 撞击 Lunge+撞击爆 | 泡沫光线 Barrage×64 束状高速 | 耗 100 | 海蓝权杖水流；泡泡枪密集连发 | Low |
| L02_F02 | 卡咪龟 | **激流**：同上 +20% | 泡沫光线 Spread+Slow | 咬住 MeleeArc | 潮旋 DoTBind（水漩涡缠绕） | 耗 100 | 环状水尘+减速场感 | Low |
| L02_F03 | 水箭龟 | **雨盘**：雨天或夜晚每秒回 2 HP | 水炮 Beam（强水柱） | 火箭头锤 Charge→Lunge（短蓄力后头槌） | 加农水炮 Beam；释放后 **休整 ~1.5s**（禁技能1/2） | 耗 100；休整 | 水炮粗柱；加农：更大水柱+冲击尘 | Medium |

### 4.4 超能链 L12

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L12_F01 | 凯西 | **同步**：自身获得 OnFire/Poison/Electrify 时，最近敌对复制同 debuff（短 CD） | 念力 Bolt+Stun（弱，短硬直） | 意念头锤 StrikeFall（幽灵锤下砸 AoE，约 ×5 面板） | 精神强念 Scatter×32 彩虹杖可见弹；单发 ×2；生成 0.5s 后追踪 | 耗 100 | 放大幽灵锤；粉紫弹+延迟追踪 | Medium |
| L12_F02 | 胡地 | **同步**：同上 | 精神强念 Bolt+DefDown | 真气拳 Charge→MeleeArc（蓄力拳） | 预知未来 ChargeBeam+IgnoreDef（蓄力后无视防御一击） | 耗 100；长蓄力 | 蓄力圈尘→爆发紫光 | Medium |

### 4.5 龙系链 L07

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L07_F01 | 迷你龙 | **蜕皮**：每 5s 约 15% 概率清除自身 1 个可清除 debuff | 龙息 Spread/Bolt+Stun（概率短僵） | 咬住 MeleeArc | 龙之怒 Beam（固定主题冲击波，中等伤） | 耗 100 | 龙息雾尘；怒波直线尘 | Low |
| L07_F02 | 哈克龙 | **蜕皮**：同上 | 龙之波动 Beam | 龙尾 MeleeArc（高击退） | 暴风 Spread/AoEBurst+Stun | 耗 100 | 云/风尘；可参考雨场粒子换风 | Medium |
| L07_F03 | 快龙 | **多重鳞片**：满 HP 时受到伤害 ×0.2（即减伤 80%）；掉血后失效至回满 | 暴风 AoEBurst+Stun | 龙之俯冲 Lunge+Stun | 逆鳞 Barrage/MeleeArc 连段；结束后 **自身混乱 ~2s** | 耗 100；混乱后摇 | 俯冲残影；逆鳞多段爪+火/龙尘 | Medium |

### 4.6 钢/超能链 L08（Excel：铁哑铃→金属怪）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L08_F01 | 金属怪 | **恒净之躯**：免疫本模关注的 debuff（着火/毒/感电/缓速等可列白名单） | 念力 Bolt+Stun | 撞击 Lunge | 猛撞 Lunge+Recoil（自损约 25% 造成伤害） | 耗 100；Recoil | 金属撞击火花尘 | Low |
| L08_F02 | 巨金怪 | **恒净之躯**：同上 | 精神强念 Bolt+DefDown | 彗星拳 MeleeArc；命中概率 SelfBuff 攻击 +10%（叠最多 2 层，8s） | 破坏光线 Beam；释放后 **休整 ~2s** | 耗 100；休整 | 彗星拳光拳尘；破灭光线粗束 | Medium |

### 4.7 龙/地链 L15

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L15_F01 | 圆陆鲨 | **粗糙皮肤**：受击反弹 **0.35×** 当前变身招式基准伤害；且受击后短时攻击 +10%（2s） | 龙之怒 Beam | 撞击 Lunge | 流沙地狱 DoTBind（沙漩涡） | 耗 100 | 沙尘环；龙怒冲击波 | Medium |
| L15_F02 | 烈咬陆鲨 | **粗糙皮肤**：同上 | 龙之波动 Beam | 咬碎 MeleeArc+DefDown | 流星群 Barrage/AoEBurst；释放后 **自身攻击 −15% 持续 5s** | 耗 100；攻降后摇 | 多段流星弹（复用石/火弹）+坠落尘 | Medium |

### 4.8 幽灵链 L06

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L06_F01 | 鬼斯通 | **飘浮**：飞行能量视为强化（默认池 ×2.5 或近似无限飞，仍禁坐骑）；**不**给永久穿墙；**无 Phase 招式** | 暗影球 Bolt+DefDown | 舌舔 MeleeArc+Stun（短硬直，**非**穿障） | 催眠术 Sleep（对普通怪强；Boss 改为强 Slow ~2s） | 耗 100 | 影球暗影尘；舌舔近距；催眠：催眠符号尘/暗影 | Medium |
| L06_F02 | 耿鬼 | **飘浮**：同上强化飞 | 污泥炸弹 Bolt+Poison | 暗影爪 MeleeArc+EasyCrit | 恶之波动 Beam/Spread+EasyCrit | 耗 100 | 毒污泥；暗影爪；暗波动紫色锥形尘 | Medium |

### 4.9 水/飞鱼链 L09

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L09_F01 | 鲤鱼王 | **优游自如**：雨天或夜晚移速 +25% | 跃起 StrikeFall（高跳砸地，低伤） | 撞击 Lunge | 抓狂 Barrage；伤害随 **已损失 HP%** 提高（最高约 +80%） | 耗 100 | 水花跃起；抓狂乱打尘 | Low |
| L09_F02 | 暴鲤龙 | **自信过度**：击杀叠攻 +20%/层，最多 2 层，每层 12s | 水炮 Beam | 咬碎 MeleeArc+DefDown | 破坏光线 Beam；释放后 **休整 ~2s** | 耗 100；休整 | 水炮；破灭光线 | Medium |

### 4.10 格斗链 L05

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L05_F01 | 腕力 | **毅力**：自身存在异常 debuff 时全招式伤害 +30% | 岩石封锁 Cross×4 收拢碎裂（Boulder 棕染 50%） | 撞击 Lunge（2s CD + 0.25s 无敌；速/距约半） | 十字劈 X 形剑气 + 前飞 64 格穿透 | 耗 100 | 可见四石；X 尘 + 前冲残影 | Medium |
| L05_F02 | 豪力 | **毅力**：同上 +30% | 岩崩 AoEBurst+Stun（概率） | 劈瓦 MeleeArc（对高防目标额外 +25% 伤；破「减伤 buff」语义） | 爆裂拳 MeleeArc+Stun（必短硬直） | 耗 100 | 落石；手刀；爆拳冲击波尘 | Medium |
| L05_F03 | 怪力 | **毅力**：同上 +30% | 尖石攻击 Bolt+EasyCrit | 十字劈 MeleeArc+EasyCrit | 近身战 Barrage/MeleeArc；释放后 **防御 −20% 持续 5s** | 耗 100；防降后摇 | 尖石；连殴近战尘 | Medium |

### 4.11 飞行链 L11

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L11_F01 | 波波 | **锐利目光**：全招式伤害 ×1.2 | 起风 GroundCyclone（原版 Typhoon **单帧**深蓝贴地；盒随帧） | 啄 Cone~16格尖角 AoE | 燕返 Lunge（短 useTime；与撞击共用突进 CD） | 耗 100 | 单团贴地旋风；尖角尘锥 | Medium |
| L11_F02 | 大比鸟 | **锐利目光**：×1.2 | 暴风 AoEBurst+Stun | 燕返 Lunge+EasyCrit（必易暴语义） | 勇鸟猛攻 Lunge+Recoil | 耗 100；Recoil | 强风场尘；全身能量撞 | Medium |

### 4.12 电系链 L04

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L04_F01 | 皮卡丘 | **静电**：被敌对接触/近战打中时，反弹 Electrify（短 CD） | 电击 Bolt+Electrify | 电光一闪 Blink（指针最近敌；落点电爆伤；2s CD + 0.25s 无敌；一屏） | 十万伏特 PierceBeam×10；50% Electrify；0.5s 后对感电敌再射 | 耗 100 | 粗电束；感电延迟再射 | Medium |
| L04_F02 | 雷丘 | **静电**：同上 | 十万伏特 Beam+Electrify（可链式） | 伏特攻击 Lunge+Recoil+Electrify | 打雷 AoEBurst/Beam+Electrify（落雷感；概率再跳） | 耗 100；伏特 Recoil | 落雷：原版雷电/暗影束尘组合 | Medium |

### 4.13 岩/钢蛇链 L13

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L13_F01 | 大岩蛇 | **坚硬脑袋**：免疫 Recoil 自伤；防御 ×1.2 | 岩石封锁 Bolt+Slow | 撞击 Lunge | 岩崩 AoEBurst+Stun | 耗 100 | 岩石弹与落石尘 | Low |
| L13_F02 | 大钢蛇 | **坚硬脑袋**：同上 | 岩崩 AoEBurst+Stun | 铁尾 MeleeArc+DefDown | 舍身冲撞 Lunge+Recoil（本被动免自伤） | 耗 100；Recoil 被被动抵消 | 铁尾金属火花；全力冲撞 | Medium |

### 4.14 传说 L16 / L14 / L17

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L16_F01 | 洛奇亚 | **压迫感**：全招式伤害 ×1.5 | 空气爆炸 Bolt/AoEBurst+EasyCrit | 神鸟猛击 Charge→Lunge | 气旋攻击 AoEBurst/Field+DefDown（必降防） | 耗 100 | 气压爆；蓄力猛击；龙卷风尘柱 | High |
| L14_F01 | 超梦 | **压迫感**：×1.5 | 精神强念 Bolt+DefDown | 意念头锤 MeleeArc+Stun | 精神击破 Beam/AoEBurst+IgnoreDef | 耗 100 | 强念紫爆；实体化念力刃/球 | High |
| L17_F01 | 烈空坐 | **气闸**：无视天气/昼夜，全招式伤害 ×1.7 | 龙之波动 Beam | 咬碎 MeleeArc+DefDown | 画龙点睛 Lunge/Beam+IgnoreDef；释放后 **防御 −20% 持续 5s** | 耗 100；防降后摇 | 龙波；终局一击大尘柱（灾厄/大修大型弹参数） | High |

### 4.15 地鼠链 L10（Excel 外补全）

| FormId | Name | Passive (Terraria) | Skill1 | Skill2 | Ultimate | Energy/aftermath | VFX note | Net risk |
|--------|------|-------------------|--------|--------|----------|------------------|----------|----------|
| L10_F01 | 地鼠 | **沙隐**（补全）：地下/洞穴移速 +20%；挖速 +30% | 掷泥 Bolt+Slow（泥浆降命中感→减速） | 撞击 Lunge | 挖洞爆发 Dig→AoEBurst（短潜行出土爆炸；吃挖掘预算） | 耗 100；挖砖需服务端校验 | 泥尘；出土碎屑 KillTile 尘 | High |
| L10_F02 | 三地鼠 | **沙隐**：同上 | 三连刺 Barrage/MeleeArc（三连戳） | 挖洞 Dig（短位移潜地，可接出土） | 地裂 AoEBurst/Field（地面裂伤波；可轻改地形须预算） | 耗 100；地形变更 High 同步 | 三刺；挖洞；地裂纹石尘 | High |

---

## 5. 实现备注（给编码）

1. **键位：** 技能1=`None`（左键），技能2=`RightClick`，大招=`ModKeybind`。表中 Lunge/Charge 注意与原版右键交互的冲突等级。
2. **穿障：** 本表无形态以招式授予 Phase；鬼斯通/耿鬼仅飘浮飞行。代码侧现役形态均未设 `GrantsPhasing=true`（已与本表对齐）；穿障管线与 A11 保留待未来形态。
3. **EasyCrit：** 与 A15 焦点镜共用升档管线；超暴击伤害 ×4。
4. **Recoil：** 统一走安全自损；坚硬脑袋免疫；生命宝珠（A16）的 −1HP 与 Recoil 分开结算。
5. **Boss：** Sleep/长 Stun 须衰减；DoTBind 对 Boss 缩短时长或降 DoT。
6. **联机 High：** 挖洞、地裂、大型场、强位移须服务端生成与拒绝超预算。
7. **验收：** 逐形态 A1→玩法语义可辨；再对标同档武器 DPS 80%～120%（需求 §2.6）。

---

## 6. 变更记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-09-04 | 初版：Excel 全表泰拉适配 + 地鼠补全；Status=Design |
