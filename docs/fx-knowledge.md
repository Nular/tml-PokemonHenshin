# 变身招式 FX 知识库

**Status：** Living（资源/手法目录；玩法数值以 `docs/move-effects.md` / 代码为准）  
**版本日期：** 2026-09-06

## 1. 权威与硬约束

| 优先级 | 文件 | 管什么 |
|--------|------|--------|
| 1 | `docs/requirements.md` | 产品规则 |
| 2 | `docs/move-effects.md` + `docs/balance-stats.md` + 代码 | 招式语义；数值数字权威；实现 |
| 3 | **本文件 `docs/fx-knowledge.md`** | VFX 复用目录、手法 cookbook、升级规格 |

本文件**不**改写玩法数值；迭代特效时优先查「当前实现 / Recommended / SpecReady / Locked」。

### 硬约束（与 AGENTS.md 对齐）

1. **禁止 CWR 运行时依赖**（`build.txt` 不得 `modReferences` 大修；禁止 `GetMod("CalamityOverhaul")`）。
2. **允许**把 CWR 贴图**拷贝**进本模 `Assets/Fx/`（见 §7；2026-09-06 已扩拷一批）。
3. **禁止擅自降级：** 跳过原版自管 AI 只留爆炸、`BlendState.Additive` + `color.A = 0`「假发光」、纯尘冒充成品等，未经用户确认不得当作成品。MagicPixel **可用**（须控制 destination/scale；无封顶通天拉伸易白屏，属实现错误而非禁令）。
4. **懒加载贴图：** 壳弹只画原版图、从不 `NewProjectile` 该 type 时，必须 `Main.instance.LoadProjectile` / `ProjectileBorrow.RequestProjectileTexture`（见 Bubble 踩坑）。
5. **优先**原版 `NewProjectile` 真弹，或壳弹 + `LoadProjectile` 画同贴图 + 自管 AI；**禁止**生成灾厄弹。
6. **Sprite sheet：** `Fire`（4×4）、`Flashimpact`（4×2）、`HitJagged01`（1×2）禁止整图 `DrawAdditiveCentered`；用 `HenshinFxDraw.Draw*Frame` / `SheetFrame`，帧=`AgeFrame(lifetime,timeLeft,ticksPerFrame,total)`。SoftGlow/Cyclone/Fog/DiffusionCircle/LightShot/LightBeam/TearFlame 可整图。

| 手法 | 何时用 | 例子 |
|------|--------|------|
| **真生成** `NewProjectile(ProjectileID.X)` | 原版 AI/贴图可接受，或可 Retarget 为 Henshin | `Leaf` 飞叶 |
| **壳弹 + LoadProjectile** | 要控伤害/穿透/联机，只要贴图/爆炸碎片 | Bubble、NebulaArcanum、Boulder、Typhoon |

---

## 2. Accepted cookbook（Stage ≤5 已验收手法）

以下模式视为可复用成品；Stage 6+ 升级应**对齐同一质量条**，勿降级回纯 Dust/Invisible。

### 2.1 飞叶快刀 — 真 `NewProjectile(Leaf)`

| 项 | 内容 |
|----|------|
| **文件** | `Wave2MoveProjs.cs` → `LeafSpreadProj`；`FormItemUtil.LeafSpread` |
| **形态** | `L03_F02` 妙蛙草 Skill1 |
| **手法** | Director 扇出 5 枚；`ProjectileBorrow.ItemShoot(ItemID.LeafBlower)` 回退 `ProjectileID.Leaf`（**206**）；`RetargetAsHenshin` |
| **为何 Accepted** | 真生成会顺带加载贴图；与吹叶机同外观 |
| **勿做** | 只刷 `DustID.Grass` 当成品；有伤无叶 |

### 2.2 泡沫光线 — `LoadProjectile(Bubble)` + `BorrowedVisualBoltProj`

| 项 | 内容 |
|----|------|
| **文件** | `RedesignedMoveProjs.cs`（`BarrageDirectorProj` ModeBubble）+ `BorrowedVisualBoltProj`；`ProjectileBorrow.cs` |
| **形态** | `L02_F02` 卡咪龟 Skill1；杰尼龟大招等同管线 |
| **手法** | **禁止**裸 `NewProjectile(Bubble)` 当伤害弹；壳弹 `ai` 传贴图 ID=`ProjectileID.Bubble`（**410**）；强制 `LoadProjectile(Bubble)` |
| **踩坑** | 未用过泡泡枪时 `TextureAssets.Projectile[Bubble]` 是 1×1 占位 → 无图 |
| **勿做** | 等玩家先用泡泡枪；用水尘线冒充泡沫束 |

### 2.3 龙之波动 — NebulaArcanum 壳 + 爆炸碎片

| 项 | 内容 |
|----|------|
| **文件** | `Wave2MoveProjs.cs` → `NebulaPulseDirectorProj` / `NebulaPulseShardProj` |
| **形态** | 火恐龙、哈克龙、烈咬陆鲨、烈空坐等 |
| **手法** | 连发约 ×10；壳弹自管飞行（**不**跑原版 Nebula AI）；`LoadProjectile(NebulaArcanum)`（**617**）；亡时 `NewProjectile(NebulaArcanumExplosionShotShard)`（**620**）紫染；scale≈0.7、不追踪 |
| **踩坑** | 跳过自管位移只留爆炸 →「远处紫碎片有伤无弹」 |
| **勿做** | 直接挂原版 Nebula AI；灾厄龙弹 |

### 2.4 天雷 — `SkyBoltLightning` + ThunderTrail Additive 保 Alpha

| 项 | 内容 |
|----|------|
| **文件** | `SkyBoltLightning.cs`；贴图 `Assets/Fx/ThunderTrail` + `SoftGlow` + `LightShot` |
| **形态** | 皮卡丘大招；雷丘大招（`ai0=1` 落雷、更粗） |
| **手法** | 折线多段 trail；**Additive 时保留 `Color.A`**；黑底白电图在 Additive 下黑变透明 |
| **CWR** | 只读抄路径/包络；贴图已拷贝，无运行时依赖 |
| **勿做** | `A=0`；依赖大修 `CWRAsset` |

### 2.5 Typhoon 壳 — 火焰漩涡 / 贴地旋风

| 项 | 内容 |
|----|------|
| **文件** | `FlareBoltUltProj`（小火龙大招）；`GroundCycloneProj`（波波起风） |
| **贴图** | `ProjectileID.Typhoon`（**409**）；橙红 / 深蓝染色 + `LoadProjectile` |
| **勿做** | 生成灾厄台风弹；Invisible hitbox 只伤 |

### 2.6 Boulder — 岩石封锁

| 项 | 内容 |
|----|------|
| **文件** | `RockTombDirectorProj` / `RockTombShardProj` |
| **形态** | 腕力 / 大岩蛇 Skill1；岩崩 `FallingBoulderProj` 亦用 Boulder |
| **手法** | 四向收拢；`ProjectileID.Boulder`（**99**）；棕染约 50% |
| **勿做** | 仅 DustID.Stone 环；无可见石块 |

### 2.7 咬住/咬碎 — destination Rectangle 尖牙

| 项 | 内容 |
|----|------|
| **文件** | `BiteArcProj` |
| **形态** | 卡咪龟/妙蛙草咬住；烈咬陆鲨/暴鲤龙/烈空坐咬碎等 |
| **手法** | 身前咬合两拍；MagicPixel **但**每层 `destination Rectangle` 硬封顶宽高叠三角尖牙 |
| **勿做** | `Draw(..., scale: huge)` 拉成通天黑条；纯 Dust 当咬合 |

---

## 3. 原版射弹速查

ID 来源：[Terraria Wiki Projectile IDs](https://terraria.wiki.gg/wiki/Projectile_IDs) / tModLoader `ProjectileID`。  
**推荐：** `New` = 可真生成（常需 Retarget）；`Shell` = 壳弹 + `LoadProjectile`；`Frag` = 仅爆炸/碎片真生成。

### Beam / Laser

| ID | 名 | 推荐 | 备注 |
|----|-----|------|------|
| 633 | `LastPrism` | Shell（自管） | **Locked：日光束专用**金阳光；勿给龙之怒 |
| 632 | `LastPrismLaser` | Shell | 日光束射线段外观 |
| 88 | `PurpleLaser` | Shell/参考 | 现 `ThickBeamProj` 贴图；水炮需升级 |
| 100 | `DeathLaser` | Shell | 红激光柱 / 龙怒包络备选 |
| 260 | `HeatRay` | Shell | 热射线备选（非日光束金光） |
| 294 / 290 | `ShadowBeamFriendly` / `Hostile` | Shell | 暗影束/球壳常用 |
| 255 | `MagnetSphereBolt` | Shell/贴图 | 雷丘中电束贴图 |
| 461 | `ChargedBlasterLaser` | Shell | 蓄力炮激光备选 |
| 440 | `LaserMachinegunLaser` | 慎用 | 易过碎 |

### Petal / Leaf / Grass

| ID | 名 | 推荐 | 备注 |
|----|-----|------|------|
| 206 | `Leaf` | **New** | 飞叶 Accepted |
| 221 / 248 | `FlowerPetal` / `FlowerPowPetal` | New/Shell | 花瓣舞目标 |
| 226 / 227 | `CrystalLeaf` / `CrystalLeafShot` | Shell | 叶绿；**勿**当 SolarBeam 主色 |
| 229 | `ChlorophyteOrb` | Shell | 叶绿球 |
| 51 | `Seed` | Shell（Barrage） | 种子机关枪 |

### Fire / Water / Rock / Wind

| ID | 名 | 推荐 | 备注 |
|----|-----|------|------|
| 85 | `Flames` | **New/Shell** | 喷火器连续焰；喷射火焰 / 龙息 |
| 15 | `BallofFire` | Shell | 火花 / EmberBolt |
| 95 / 296 / 295 | CursedFlame / InfernoBlast / InfernoBolt | Shell/Frag | 狱火/诅咒焰 |
| 34 | `Flamelash` | Shell | 焰鞭弹 |
| 409 | `Typhoon` | Shell | 火焰漩涡 / 起风 / 沙涡 Accepted 基线 |
| 22 / 27 | `WaterStream` / `WaterBolt` | Shell | 水枪 / 粗水柱 |
| 410 | `Bubble` | Shell+**Load** | 泡沫 Accepted |
| 523 | `ToxicBubble` | Shell | 污泥可见泡 |
| 99 | `Boulder` | Shell | 岩石封锁 / 岩崩 |
| 17 | `DirtBall` | Shell | 掷泥 |
| 261 | `BoulderStaffOfEarth` | 慎用 | 空贴图风险；优先 99 |

### Electric / Shadow / Psychic / Melee

| ID | 名 | 推荐 | 备注 |
|----|-----|------|------|
| （自绘） | `SkyBoltLightning` + Fx | 本模 | Accepted 天雷 |
| 79 | `RainbowRodBullet` | Shell | 精神强念 / 共鸣散射 |
| 617–620 | NebulaArcanum 族 | Shell+Frag | 龙波 Accepted |
| 271 / 262 | `BoxingGlove` / `GolemFist` | Shell | 拳击冲击 |
| 116 / 132 / 156 | Sword/Terra/Light Beam | Shell | 剑气 / 十字劈参考 |
| — | BiteArc destination fang | 本模 | Accepted |

---

## 4. 全招式 FX 目录

**Status 词表：**

| Status | 含义 |
|--------|------|
| **Accepted** | Stage≤5 级已抛光，或同 cookbook 复用 |
| **NeedsUpgrade** | 仍偏 Dust/Invisible/弱壳，待升级 |
| **SpecReady** | §5 已写可施工规格（代码 `Stage==6` 点名招） |
| **Locked** | 规格已锁方向（§6；日光束=金棱镜蓄力一射） |

Playstyle 代号同 `move-effects.md`。类名默认在 `Content/Combat/Moves/`。  
表中 **Stage** 为代码 `FormDefinition.Stage`（与需求档位偶有偏差时以代码为准，文中另注）。

### 4.1 火系 L01

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| Ember/火花 | L01_F01,1 | Bolt+OnFire | `EmberBoltProj`←BallofFire | BallofFire Shell | 可用 | Accepted |
| Scratch/抓 | L01_F01,1 | MeleeArc | `ScratchSlashProj` | 自绘爪痕 | 平行爪 | Accepted |
| FireSpin/火焰漩涡 | L01_F01 Ult | HomingLock | `FlareBoltUltProj` Typhoon | Typhoon Shell | 可见涡 | Accepted |
| DragonPulse/龙之波动 | L01_F02,4 | Bolt×10 | NebulaPulse* **直线连发** | Nebula 617/620 | 紫炸 | Accepted |
| FireFang/火焰牙 | L01_F02,4 | MeleeArc+OnFire | BiteArc **两对大弧牙**+OnFire | BiteArc cookbook | 火焰牙 | Accepted |
| FlareBlitz/闪焰冲锋 | L01_F02,4 | Lunge+Recoil | `LungeProj` **32格** 多线火径+包裹焰+收尾减速 | 火尘残影 | 可见冲锋 | Accepted |
| Flamethrower/喷射火焰 | L01_F03,7 | FlameCone | 真 Flames + **Fire 帧**（枪口小 TearFlame） | **Flames(85)** 真焰柱 | 喷火柱 | Accepted |
| DragonClaw/龙爪 | L01_F03,7 | Scratch | ScratchSlash **20格** + HitJagged **帧** | — | 远爪 | Accepted |
| Overheat/过热 | L01_F03 Ult | MouseAoE | 半径15格、5脉冲；Fire/Flash **帧** | InfernoFriendlyBlast | 可见火环 | Accepted |

### 4.2 水系 L02

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| WaterGun/水枪 | L02_F01,1 | Aqua | AquaScepter←WaterStream | WaterStream Shell | OK | Accepted |
| Tackle/撞击 | 多形态 | Lunge | `LungeProj` SoftGlow 残影 | — | 2s CD | Accepted |
| BubbleBeam/泡沫光线 | L02_F01 Ult / L02_F02 S1 | Barrage | Barrage+Borrowed Bubble **窄直线**速度随机+破裂小泡 | Bubble Load | 密泡 | Accepted |
| Bite/咬住 | L02_F02,4 | BiteArc | `BiteArcProj` | cookbook | 尖牙 | Accepted |
| Whirlpool/潮旋 | L02_F02 Ult | DoTBind | MouseVortex Cyclone 蓝 | Typhoon 蓝染 / `Assets/Fx/Cyclone` | 可见涡 | Implemented |
| HydroPump/水炮 | L02_F03,7 / L09 | Beam | `WaterJetProj` 枪口渐进；命中不穿透+渐缩 | SoftGlow 水柱+流动波节 | 水柱 | Accepted |
| SkullBash/火箭头锤 | L02_F03,7 | Charge→Lunge | Lunge 长 use | — | OK | Accepted |
| HydroCannon/加农水炮 | L02_F03 Ult | Beam | `WaterJet` cannon：穿透+每3击爆 | 同水炮加粗+流动 | 粗柱 | Accepted |

### 4.3 草系 L03

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| VineWhip/藤鞭 | L03_F01,1 | Whip | GrassWhip | 草尘鞭 | OK | Accepted |
| SeedGun/种子机关枪 | L03_F01 Ult | Barrage | Barrage Seed | Seed Shell | OK | Accepted |
| RazorLeaf/飞叶快刀 | L03_F02,4 | Spread×5 | LeafSpread **New Leaf** | Leaf 206 | 标杆 | Accepted |
| Bite/咬住 | L03_F02,4 | BiteArc | BiteArc | cookbook | OK | Accepted |
| SeedBomb/种子炸弹 | L03_F02 Ult | Bolt/AoE | SeedBomb | Seed+爆 | OK | Accepted |
| SludgeBomb/污泥炸弹 | L03_F03,7 | Bolt+Poison | SludgeBolt + 毒气瓶 ToxicCloud 簇 | ToxicBubble→ToxicCloud | 毒云DoT | Accepted |
| PetalDance/花瓣舞 | L03_F03,7 | AoEBurst | 半径 **15格** FlowerPetal 壳环 | FlowerPetal Load | 可见瓣 | Accepted |
| SolarBeam/日光束 | L03_F03 Ult | ChargeBeam | 蓄力→`SolarPrismBeam` **~100tick** 跟鼠标会聚 | **LastPrism 金光** | 持续金棱 | Accepted |

### 4.4 电系 L04

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| ThunderShock/电击 | L04_F01,2 | Bolt | ThunderBoltHenshin | — | OK | Accepted |
| QuickAttack/电光一闪 | L04_F01,2 | Blink | BlinkStrike | SoftGlow 落点 | OK | Accepted |
| Thunderbolt/十万伏特 | L04_F01 Ult / L04_F02 S1 | Beam | SkyBolt / MidThunder+SoftGlow | SkyBolt cookbook | 标杆 | Accepted |
| VoltTackle/伏特攻击 | L04_F02,5 | Lunge+Recoil | Lunge Electric SoftGlow | — | OK | Accepted |
| Thunder/打雷 | L04_F02 Ult | Pillar | ThunderPillar→SkyBolt ai0=1 | 同天雷 | OK | Accepted |

### 4.5 格斗 L05

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| RockTomb/岩石封锁 | L05_F01,3 | Cross×4 | RockTomb* Boulder | Boulder | 可见石 | Accepted |
| CrossChop/十字劈 | L05_F01 Ult | Melee | CrossChopArc/Dash | SwordBeam 系 | 尘+冲 | Accepted |
| RockSlide/岩崩 | L05_F02,6 | AoEBurst | 3石微偏（`|vx|≤8`）+落地1格爆**无金光** | Boulder+RockShatter | 微偏落地爆 | Accepted |
| BrickBreak/劈瓦 | L05_F02,6 | MeleeArc | **20格**线 + HitJagged/Flash **帧** | BoxingGlove+冲击 | 可见 | Accepted |
| DynamicPunch/爆裂拳 | L05_F02 Ult | Melee+Stun | **巨大拳套前挥**+石碎+半径**20格**爆（不冲刺） | BoxingGlove+RockShatterBurst | 拳爆 | Accepted |
| CrossChop(短) | L05_F03,9 | Melee | CrossChopArcX 360°带弧度X（双对角外凸朝瞄准） | ContinuousBeam | 弧X | Accepted |
| StoneEdge/尖石攻击 | L05_F03,9 | Melee | StoneEdgeDirector 三角刺×3穿透+命中爆散石 | Boulder TriangleList | 三角戳穿透 | Accepted |
| CloseCombat/近身战 | L05_F03 Ult | Barrage | CloseCombatFury 高频+全程无敌+跟随 | HitJagged 纠向 | 狂殴 | Accepted |

### 4.6 幽灵 L06

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| ShadowBall/暗影球 | L06_F01,8 | Bolt | BigShadowBall 5×不透明紫盘（尖伤+lag尾） | DrawOpaqueDisk #220033/#330066 | 可见链 | Accepted |
| Lick/舌舔 | L06_F01,8 | Melee+Stun | LickTongue Extra98 20格线 | Extra98 条带 | 伸收回缩 | Accepted |
| Hypnosis/催眠术 | L06_F01 Ult | Sleep | HypnosisWave 屏内 SleepDebuff / Boss Slow | SoftGlow 环 + 睡眠白 | 睡眠环 | Accepted |
| SludgeBomb | L06_F02,10 | Bolt | 同污泥+ToxicCloud 簇 | ToxicCloud | 毒云 | Accepted |
| ShadowClaw/暗影爪 | L06_F02,10 | Slash | ShadowClawSlash 紫爪20格+ShadowFlame | Scratch 式 | 紫远爪 | Accepted |
| DarkPulse/恶之波动 | L06_F02 Ult | Barrage | DarkPulseBarrage×32 穿墙追踪爆 | BigShadowBall 风格 | 身周弹幕 | Accepted |

### 4.7 龙系 L07

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| DragonBreath/龙息 | L07_F01,6 | Spread+Stun | **128格** Fire 帧线；禁飞散 Flames | Flames/紫火锥；`Assets/Fx/Fire` 按帧 | 火息线 | Implemented |
| Bite/咬住 | L07_F01,6 | BiteArc | BiteArc | cookbook | OK | Accepted |
| DragonRage/龙之怒 | L07_F01 Ult / L15_F01 S1 | Barrage | `DragonRageBarrage` 技能12/大招32 抖动球 | SoftGlow≈1.5格（晕抬亮#2108ad/芯#e7ce39）+5格爆 | 球体连射 | Implemented |
| DragonPulse | L07_F02,8 | Nebula×10 | NebulaPulse | cookbook | OK | Accepted |
| DragonTail/龙尾 | L07_F02,8 | Melee | DragonTailWhip 星尘龙节链15格强击退 | StardustDragon1–4 | 鞭弧 | Accepted |
| Hurricane/暴风 | L07_F02 Ult | Bolt+Orbit | WeatherPain **直立帧** 主+**4伴随** 穿透牵引；命中4侧摆 | WeatherPainShot | 大招风团 | Accepted |
| Hurricane/暴风 | L07_F03 S1 / L11_F02 S1 | Bolt+Orbit | WeatherPain 直立帧+穿透牵引；命中左右摆（不自旋） | WeatherPainShot | 天候棒 | Accepted |
| DragonDive/龙之俯冲 | L07_F03,11 | Lunge | StardustPathLunge 半透明星尘龙路径伤 | StardustDragon2–4 | 路径龙 | Accepted |
| Outrage/逆鳞 | L07_F03 Ult | Barrage | 3s CultistBossFireBall 壳追踪爆+Confused | CultistBossFireBall | 身周火球 | Accepted |

### 4.8 其余线路（钢超 / 鱼 / 地鼠 / 飞 / 超能 / 岩钢 / 陆鲨 / 传说）

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| Confusion/念力 | L08_F01 / L12_F01 | Bolt+Stun | `PsychicWaveBolt` 32格索敌弹射×2 + **命中紫环1格** | ShadowBeam 294 | 波动链 | Accepted |
| TakeDown/猛撞 | L08_F01/L13 Ult | Lunge+Recoil | TakeDownLunge 闪焰式冲+灰 SolarWhipSwordExplosion×4–6 0伤+土黄尘 | SolarWhipSwordExplosion | 金属冲 | Accepted |
| Psychic/精神强念 | L08_F02 | Bolt+DefDown | AlakazamPsychic 鼠位×3（同胡地） | RainbowRod | 三连追 | Accepted |
| Psychic/精神强念 | L12_F02 | Bolt+DefDown | AlakazamPsychic 鼠位×3 穿墙穿怪否+延迟追 | RainbowRod | 三连追 | Accepted |
| MeteorMash/彗星拳 | L08_F02,10 | Melee | CometPunch 拳套+3×StarWrath 穿墙降 | BoxingGlove/StarWrath | 拳+星 | Accepted |
| HyperBeam/破坏光线 | L08_F02 / L09_F02 Ult | Beam | SustainedBeam 自缓+大招加粗 | DeathLaser 粗壳 | 持续粗束 | Accepted |
| Splash/跃起 | L09_F01,1 | StrikeFall | StrikeFall Water | — | 低伤 | Accepted |
| Flail/抓狂 | L09_F01 Ult | Barrage | FlailBarrage **15格三线爪** 宽判+抓尘 | 多爪环身 | 可见多段 | Accepted |
| HydroPump | L09_F02,9 | Beam | `WaterJetProj` | 同水箭龟水柱 | 水柱 | Accepted |
| Crunch/咬碎 | L09_F02 / L15_F02 / L17 | BiteArc | BiteArc size↑ | cookbook | OK | Accepted |
| MudSlap/掷泥 | L10_F01,3 | Bolt+Slow | MudSlap←DirtBall | DirtBall | OK | Accepted |
| Dig/挖洞 | L10_F02 S2 | DigLunge | `DigLungeProj` 冲20格+镐力走廊 | 土尘 SoftGlow | 挖进突 | Implemented |
| TripleDig/三连刺 | L10_F02,6 | Barrage | GroundSpikeStab **8格** | 三道可见刺/爪 | 可见刺 | Implemented |
| Earthquake/地裂 | L10_F02 Ult | AoE/Field | QuakeWave 小 Boulder 波前 | 裂纹+石 | 可见波 | Implemented |
| Gust/起风 | L11_F01,2 | GroundCyclone | Typhoon 深蓝 | cookbook | OK | Accepted |
| Peck/啄 | L11_F01,2 | Cone | PeckCone | — | 尘锥 | Accepted |
| AerialAce/燕返 | L11_F01 Ult | Lunge | Lunge SoftGlow | — | OK | Accepted |
| AerialAce/燕返 | L11_F02 S2 | BlinkSlash | `AerialAceBlink` 双切回起点+双弧羽径 | 禁 Electric 尘 | 往返弧 | Accepted |
| BraveBird/勇鸟猛攻 | L11_F02 Ult | Lunge+Recoil | `BraveBirdLunge` 白拖尾梭形冲+交叉 Raven | 闪焰式加速 | 梭形鸟群 | Accepted |
| ZenHeadbutt/意念头锤 | L12_* / L14 | StrikeFall | ZenHammer←Paladin锤 | PaladinsHammer | 较好 | Accepted |
| Psychic scatter Ult | L12_F01 Ult | Scatter | Barrage Rainbow | RainbowRod | OK | Accepted |
| FocusPunch/真气拳 | L12_F02 S2 | Melee | FocusPunch 白气上扬无石爆 | BoxingGlove+Fog | 白气拳 | Accepted |
| FutureSight/预知未来 | L12_F02 Ult | MarkBolt | FutureSightNightglow 1s显形后追爆 | FairyQueenMagicItemShot | 夜光标记 | Accepted |
| RockTomb | L13_F01,5 | RockTomb | Boulder | cookbook | OK | Accepted |
| RockSlide | L13_F01 Ult / L13_F02 S1 | RockSlide* | 大招**12**石±12格微偏+落地1格爆**无金光**；技能3石同偏 | Boulder+RockShatter | 宽密落地爆 | Accepted |
| IronTail/钢尾 | L13_F02,9 | Whip | DragonTailWhip 底色+叠 `Color(0,0,16,80)` | StardustDragon | 铁灰罩 | Accepted |
| Aeroblast/空气爆炸 | L16_F01 S1 | AoE | AirBurst 三段脉冲+缩小 DiffusionCircle | ScaleForWorldDiameter | 三段小爆 | Accepted |
| CycloneAttack/气旋攻击 | L16_F01 Ult | Field | HurricaneField 半径**32格**；名键独立于 Aeroblast | ScaleForWorldDiameter | 大风场 | Accepted |
| SkyAttack/神鸟猛击 | L16_F01 | ChargeLunge | SkyAttackLunge 0.5s无敌吟唱后冲+6鸟 | Raven 交叉 | 蓄力冲 | Accepted |
| Psystrike/精神击破 | L14 Ult | MarkOrb | MewtwoPsystrike 指针16格选敌→8格渐显64暗影球齐冲 | DrawOpaqueDisk | 环伺齐射 | Accepted |
| Psychic/精神强念 | L14_F01 | Bolt | AlakazamPsychic×6 穿墙 | RainbowRod | 六连追 | Accepted |
| DragonRage | L15_F01,6 | Barrage | `DragonRageBarrage` mode=skill 12发 | SoftGlow 抖动球+5格爆 | 可见 | Implemented |
| Tackle | L15_F01,6 | Lunge | Lunge Dirt SoftGlow | — | OK | Accepted |
| SandTomb/流沙地狱 | L15_F01 Ult | DoTBind | 直径**16格** Typhoon壳压蓝 + 高不透明琥珀 Cyclone/Fog | Typhoon+Cyclone 深沙黄 | 琥珀涡 | Accepted |
| DracoMeteor/流星群 | L15_F02 Ult | Barrage | 64×StarWrath 壳朝下（rot−Pi/2）+金粉爆 | StarWrath | 天降星雨 | Accepted |
| DragonAscent/画龙点睛 | L17 Ult | Lunge | 长星尘龙路径冲，全骨节纯黑 `0,0,0,255` | StardustDragon | 黑龙径 | Accepted |
| DragonPulse | L17 / L15_F02 | Nebula | NebulaPulse | cookbook | OK | Accepted |

> **注：** **Stage 6 / Wave3 / Stage7+ 均已验收（2026-09-07）**。Stage7+ 含钢尾灰罩、猛撞灰日耀、暗影抓/恶波动、彗星拳 StarWrath、破灭自缓加粗、龙俯冲/画龙点睛纯黑龙、逆鳞火球、流星群64、洛奇亚三招、超梦强念×6+精神击破64球。

### Sleep Buff cookbook（Accepted 2026-09-07）
- `HenshinSleepDebuff` + `HenshinNpcGlobalNPC`：`PreAI=false` 定身、`CanHitPlayer=false`、首伤 `FinalDamage*=2` 后 DelBuff；**不清**已在飞敌弹。催眠施加端跳过 Boss。
- 睡眠 VFX：`DrawEffects` 强制 `Color(255,255,255,100)` + 头顶 3 路 `z/Z/Zz`。
- 经验 VFX：`HenshinXpPopupSystem` 世界字 `EXP +X`（钉击杀坐标）/ `LEVEL UP!`（跟玩家、连升连弹）；Boss 更厚描边+金闪。不绑 buff。
- **禁止：** SoftGlow 几何罩；PostDraw 克隆贴图叠罩；弱 `Lerp→White` / `GetAlpha` 漂白（亮部不可见）。现役白即为用户确认规格。

---

## 5. Stage 6 SpecReady（可施工规格 — 已落地，保留作规格档案）

仅覆盖计划点名的 **代码 `Stage==6`** 形态招式。原则：留玩法管线，换可见外壳；禁把 LastPrism 金光给龙之怒。

### 5.1 `L05_F02` 豪力 — RockSlide / BrickBreak / DynamicPunch

#### RockSlide（岩崩）Skill1 / 大岩蛇大招共享

| 项 | 现役 |
|----|------|
| **Target visual** | 指针区上方棕石先后砸下；轨迹微偏鼠标（`|vx|≤8`）；着地碎裂 + **半径 1 格**范围伤；**无 FlashImpact 金光**（仅石色扩散+尘） |
| **数量/宽** | 豪力/大钢蛇技能 **3**、半宽 ±80px；大岩蛇大招 **12**、半宽 ±192px（约 ±12 格） |
| **实现** | `RockSlideDirectorProj`：`ai1`=count，`ai2`=半宽；落石 `FallingBoulderProj` ai0=1→小半径 `RockShatterBurst` |
| **Forbidden** | 纯垂直雨；仅 Invisible Quake 盒；岩崩落地对角金闪 |
| **Files** | `Wave2MoveProjs.cs`；`FormItemUtil.RockSlideX`；Machoke / Onix / Steelix |

#### BrickBreak（劈瓦）Skill2

| 项 | 规格 |
|----|------|
| **Target visual** | 身前短距「手刀/瓦碎」：可见冲击弧或拳印一帧 + 碎瓦尘；高防 +25% 逻辑保留 |
| **ProjectileID** | `BoxingGlove` **271** 或 `GolemFist` **262**：**Shell+Load**；或短距 `SwordBeam` **116**。碰撞仍用短命中盒。冲击可用已拷 `Assets/Fx/HitJagged01` / `Flashimpact` |
| **Tint/scale** | 冷白/浅金冲击；scale≈1.2；寿命 10～14 tick |
| **Forbidden** | `PreDraw=>false` 仅 Blood 尘当成品；无封顶通天拉伸当「光线」 |
| **Files** | `BrickBreakProj`；可选新 `MeleeImpactShellProj`；`FormItemUtil.BrickBreak` |

#### DynamicPunch（爆裂拳）Ultimate — **Implemented**

| 项 | 现役 |
|----|------|
| **Target visual** | **不冲刺**；朝指针挥出巨大 `BoxingGlove`（scale≈3.5，约 14 格）；终点石屑碎裂 + `RockShatterBurstProj` 范围爆；Confused 保留 |
| **AoE** | 大招半径 **20 格**（`Ai0=20`）；真气拳技能半径 **8** |
| **Forbidden** | 推玩家 velocity 当成品；无拳无石只 Confused |
| **Files** | `DynamicPunchProj`；`RockShatterBurstProj`；`FormItemUtil.DynamicPunchUlt` |

---

### 5.2 `L07_F01` 迷你龙 — DragonBreath / Bite / DragonRage

#### DragonBreath（龙息）Skill1

| 项 | 规格 |
|----|------|
| **Target visual** | 短锥**有色龙雾/焰雾**向前喷 0.3～0.5s，非纯 Cloud 尘点 |
| **ProjectileID** | `Flames` **85** 扇形真生成（Retarget）或壳弹画 Flames/CursedFlame；辅 `DustID.PurpleTorch`；可选 `Assets/Fx/Fire` / `Fog` / `TearFlame01` |
| **Tint/scale** | 紫粉 `Color(200,120,255)` + 微火橙；锥长约 8～12 格 |
| **Forbidden** | 仅 `PreDraw false` + Cloud 尘；Invisible 盒 |
| **Files** | `DragonBreathConeProj`；`FormItemUtil.DragonBreath`；`CombatLinesA.cs` Dratini |

#### Bite（咬住）Skill2

| 项 | 规格 |
|----|------|
| **Target visual** | 保持 Accepted BiteArc 尖牙 |
| **ProjectileID** | 自绘 destination fang（现实现） |
| **Tint/scale** | 默认 size；可略小 |
| **Forbidden** | 退回 GenericSlash |
| **Files** | `BiteArcProj`（通常无需改） |

#### DragonRage（龙之怒）Ultimate — **Implemented**

| 项 | 现役 |
|----|------|
| **Target visual** | 技能 **12** / 大招 **32** 发 SoftGlow 球体（直径 **≈1.5 格**），沿瞄准直线前进并垂直抖动；外晕蓝紫 + 金芯 `#e7ce39`；尘粒为两色插值；命中直径 **5 格** 小爆 |
| **实现** | `DragonRageBarrageProj` + `DragonRageOrbProj`；`FormItemUtil.DragonRage`；**非**光束、**非** LastPrism |
| **色** | 边 `#2108ad`（Additive 光晕须抬亮 `DragonHaloLit`）；芯 `#e7ce39`；粒子 `Color.Lerp` |
| **Forbidden** | 金色棱镜当主视觉；DiffusionCircle 裸大 scale |
| **Files** | `Wave2MoveProjs.cs`；Dratini Ultimate / Gible Skill1 |

---

### 5.3 `L10_F02` 三地鼠 — TripleStab / Dig / Quake

#### TripleStab（三连刺）Skill1

| 项 | 规格 |
|----|------|
| **Target visual** | 三道错开时间的**可见尖刺/爪痕**向前戳，非三次 Invisible GenericSlash |
| **ProjectileID** | Shell 短矛：可借 `Stinger` **55** / `JungleSpike` **176** Load；或三道窄 Rectangle 尖刺（封顶尺寸，BiteArc 纪律）；可选 `HitJagged01` |
| **Tint/scale** | 土棕；间距 6～8 tick；reach≈腕力爪 |
| **Forbidden** | 三次无图 slash；通天拉伸 |
| **Files** | `TripleStabProj`；勿只 `GenericSlashProj`+Dirt |

#### Dig（挖洞）Skill2

| 项 | 规格 |
|----|------|
| **Target visual** | 潜地：玩家处土喷泉 → 向鼠标突刺 **20 格** → 途中镐挖走廊；路径挖砖**不吃** TerrainBudget |
| **ProjectileID** | `DigLungeProj`；出土/碎屑 DirtBall 视觉 |
| **Tint/scale** | Dirt/Stone；从前方第 3 格起挖 5 格宽 |
| **Forbidden** | 完全无土只瞬移；用 velocity 顶墙导致突进中断 |
| **Files** | `DigLungeProj`；`FormItemUtil.Dig`；`TryMineWithPlayerPick`（路径免预算） |

#### Quake / Earthquake（地裂）Ultimate

| 项 | 规格 |
|----|------|
| **Target visual** | 自脚下向外扩展的**地面裂纹波**：石块蹦起 + 裂线尘带；非单纯 widen hitbox |
| **ProjectileID** | 沿波前 Shell 小 `Boulder` 弹跳或石 Gore；命中仍 `QuakeWaveProj` |
| **Tint/scale** | 灰石；reach 与现 `_reach` 同步 |
| **Forbidden** | Invisible 扩展盒；无裂纹 |
| **Files** | `QuakeWaveProj`；`FormItemUtil.QuakeUlt` |

---

### 5.4 `L15_F01` 圆陆鲨 — DragonRage / Tackle / SandTomb

#### DragonRage（龙之怒）Skill1 — **Implemented**

| 项 | 现役 |
|----|------|
| **Target visual** | 同大招视觉语言；**12** 发、略弱伤 |
| **实现** | `FormItemUtil.DragonRage(..., ult: false)` → `DragonRageBarrageProj.ModeSkill` |
| **Forbidden** | 金棱镜；细紫尘线当成品 |
| **Files** | `UtilityAndLegend.cs` Gible MoveA；共享 `DragonRageBarrageProj` |

#### Tackle / Lunge（撞击）Skill2

| 项 | 规格 |
|----|------|
| **Target visual** | 保持 Lunge；可加土尘残影区分龙怒 |
| **ProjectileID** | `LungeProj` |
| **Tint/scale** | DustID.Dirt |
| **Forbidden** | — |
| **Files** | 通常保持；非本轮强制 |

#### SandTomb（流沙地狱）Ultimate — **Implemented**

| 项 | 现役 |
|----|------|
| **Target visual** | 指针处**可见沙涡**：保留 **Typhoon 壳**（压蓝 B≈0、壳 Alpha≈110）+ 高不透明琥珀 Cyclone/Fog/SoftGlow；直径 **16 格** |
| **踩坑** | Typhoon 蓝底 × 黄 tint → 绿；勿只加深黄乘色。须压壳蓝贡献 + 上层高 A 琥珀主导色相（调低上层 Alpha 会更露蓝） |
| **Forbidden** | 仅沙尘无贴图；删 Typhoon 壳当「改色捷径」；Invisible 72×72 |
| **Files** | `MouseVortexProj` 沙分支；`FormItemUtil.MouseVortex`；Gible Ultimate |

---

## 6. Stage 7 Locked / 升级笔记

以下为 Stage≥7 点名约束；**SolarBeam 为 Locked**。施工前玩法数值仍以 `move-effects.md` 为准（本文件只锁 VFX 方向）。

### SolarBeam（日光束）— **Implemented**（原 Locked 规格已落地）

- **语义：** 蓄力约 **0.8s** → 持续金棱会聚束 **~100 tick** 跟鼠标（保留 charge-then-fire，非按住从零蓄力）。
- **视觉：** **GOLDEN 阳光**（金黄白）；5 线会聚 + 连续线伤；**不要**叶绿绿当主色。
- **复用：** `LastPrism` **633** + `LastPrismLaser` **632** 外观；自管 AI。
- **Files：** `ChargeBeamDirectorProj`；`SolarPrismBeamProj`；`FormItemUtil.ChargeBeamUlt`。

### PetalDance（花瓣舞）— Implemented

- **目标：** 半径 **15 格**环身旋转花瓣，非仅 `Firework_Pink` 尘。
- **复用：** `FlowerPetal` **221** 壳画旋转环（禁真生成失控 AI）。
- **Files：** `PetalDanceFieldProj`。

### Flamethrower（喷射火焰）— Implemented

- **目标：** **连续火焰喷射**；PreDraw 用 **Fire 帧**；枪口可选小 TearFlame。
- **复用：** `Flames` **85** 真生成 Retarget；`DrawFireFrame`。
- **Files：** `FlameConeDirectorProj`。

### HydroPump / HydroCannon（水炮 / 加农水炮）— Implemented

- **目标：** **粗水柱**，跟鼠标；枪口约 24 tick 渐进伸长；水炮宽 **≈1～1.5 格**（不穿透，命中渐缩）；加农 **≈2～2.5 格**（穿透，每 3 击半径 5 格水爆）。
- **复用：** `WaterJetProj`：`DrawContinuousBeam` + `DrawWaterFlowRipples`（流动波节）；工厂 `FormItemUtil.WaterJet`。
- **Files：** `WaterJetProj`；Blastoise / Gyarados。

### DragonRage（龙之怒）— Implemented

- **非光束：** 技能 **12** / 大招 **32** 发 SoftGlow 球体（直径 **≈1.5 格**），直线 + 垂直抖动；外晕抬亮蓝紫 + 金芯 `#e7ce39`；尘粒两色插值；命中直径 **5 格**。
- **为何不用原版球弹：** 色/AI 绑死；要精确龙色 → SoftGlow 壳。
- **Files：** `DragonRageBarrageProj` / `DragonRageOrbProj`；`FormItemUtil.DragonRage`。

### DrawContinuousBeam（防白屏 / 防虚线）

- SoftGlow 沿路径 **拉长 + 密叠**（spacing≈戳记长度×0.22），厚度允许到约 3 格。`MagicPixel` 亦可用，但须封顶宽高；无界拉伸易白屏。
- `ScaleForWorldDiameter`：DiffusionCircle（360px）等大图必须按世界直径换算，禁止裸 `scale=1.7`（会画出超大圈）。
- **Additive 暗色：** `#2108ad` 作 SoftGlow 色几乎不可见 → 光晕用抬亮同色相。
- **Typhoon 改沙黄：** 蓝贴图乘黄 → 绿；保壳时压蓝（B≈0）+ 降壳 Alpha，再用高不透明琥珀 Cyclone/Fog 盖色。

### SpawnAtMouse 中心校正（踩坑）

- `Projectile.NewProjectile(spawn,…)` 的 `spawn` 是 **左上角**。大 width/height（过热 480、流沙 256 等）会偏到鼠标右下。
- **修正：** `HenshinForceItem.FireMove` 在 `SpawnAtMouse` 时 `Main.projectile[id].Center = Main.MouseWorld`；AI 内改尺寸须先存 `Center` 再还原。

### DrawOpaqueDisk / Extra98（鬼斯通）

- **深紫实心圆**（`#220033`/`#330066`）禁止 Additive SoftGlow（暗色几乎不可见）；用 `HenshinFxDraw.DrawOpaqueDisk`（AlphaBlend + DiffusionCircle）。
- **暗影球拖尾：** 路径 history lag 画 5 节递减盘，仅 tip hitbox 有伤（等价「首节伤、尾纯 VFX」）。
- **舌舔：** `Assets/Fx/Extra98` 竖条沿根→尖重叠绘制；`ShouldUpdatePosition=false`；线碰撞；命中收舌。

---

## 7. CWR 拷贝库存（只读参考）

### 7.1 本模 `Assets/Fx/`（截至 2026-09-06）

路径约定：`PokemonHenshin/Assets/Fx/...`；代码 `ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/...")`。  
**纪律：** 只在本模路径引用；**禁止**运行时读 CWR 资源。

| 文件 | 用途（预期） | 拷贝状态 |
|------|--------------|----------|
| `ThunderTrail.png` | 天雷折线 | 已有（早先） |
| `SoftGlow.png` | 落点/辉光 | 已有（早先） |
| `LightShot.png` | 电光尖端 / 束头 | 已有（早先） |
| `Cyclone.png` | SandTomb / 潮旋 / 暴风可见涡 | **已拷 2026-09-06** |
| `Fire.png` | 龙息 / 喷射火焰包络 | **已拷 2026-09-06**；**4×4 sheet（帧 128×128）**，须 `DrawFireFrame` |
| `TearFlame01.png` | 枪口小焰舌（单图 OK） | **已拷 2026-09-06**；勿沿束重复叠画成假 sheet |
| `LightBeam.png` | 龙之怒 / 水柱包络（非金棱镜） | **已拷 2026-09-06** |
| `Flashimpact.png` | 劈瓦/爆裂拳冲击闪 | **已拷 2026-09-06**；**4×2 sheet（帧 512×512）**，须 `DrawFlashImpactFrame` |
| `HitJagged01.png` | 近战冲击锯齿 | **已拷 2026-09-06**；**1×2 sheet（帧 256×128）**，须 `DrawHitJaggedFrame` |
| `Fog.png` | 龙雾/沙雾辅层 | **已拷 2026-09-06** |
| `DiffusionCircle.png` | 冲击环 / 加农水花环 / 不透明紫盘底 | **已拷 2026-09-06** |
| `Extra98.png` | 舌舔肉质条带（竖条灰阶） | **已拷 2026-09-06** |

### 7.2 CWR 只读源目录

`C:\Dev\projects\misc_prj\CalamityOverhaul\Assets\Masking\`  
（`CWRConstant.Masking = "CalamityOverhaul/Assets/Masking/"`）

上表文件均由此目录拷入本模；另有 `LightShotAlt.png`、`Smoke*.png`、`SlashBrush*`、`Airflow.png` 等**未拷**，需要时再追加。

### 7.3 后续可选追加（未拷）

| CWR 文件 | 建议用于 | 优先级 |
|----------|----------|--------|
| `Smoke.png` / `SmokeWisp01.png` | 沙尘/爆烟 | 低 |
| `SlashBrush01～04.png` | 三连刺/近战弧 | 低 |
| `HitSparkSheet01.png` | 拳击火花表 | 低 |
| `Airflow.png` | 风系加强 | 低（Stage7+） |
| `FireBall.png` | 火球备选 | 低 |

> `dev-plan` 旧文「禁止新增 FX 图」以 **AGENTS 现行例外（可拷贝至 Assets/Fx）** 为准。

---

## 8. 实现索引（快速跳转）

| 积木 | 主文件 |
|------|--------|
| 壳弹借用说明 | `ProjectileBorrow.cs` |
| 泡沫/种子连发 | `RedesignedMoveProjs.cs` |
| 天雷 | `SkyBoltLightning.cs` |
| Leaf/Nebula/Bite/Stage6 弱招 | `Wave2MoveProjs.cs` |
| 大比鸟暴风/燕返/勇鸟 | `PidgeotMoveProjs.cs` |
| 鬼斯通暗影球/舌舔/催眠 | `HaunterMoveProjs.cs` |
| Lunge/Dig/Slash 共享 | `SharedMoveProjs.cs` |
| 火花 | `EmberBoltProj.cs` |
| 爪痕 | `ScratchSlashProj.cs` |
| FX 贴图 | `Assets/Fx/`（§7） |
| 形态接线 | `StarterLines.cs` / `CombatLinesA.cs` / `UtilityAndLegend.cs` |
| 玩法表 | `docs/move-effects.md` |
| 本知识库 | `docs/fx-knowledge.md` |

---

## 9. 变更记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 0.1 Draft | 2026-09-06 | 研究初稿：cookbook、原版速查、全招式表、Stage6 SpecReady、Stage7 Locked、CWR 库存 |
| 0.2 Living | 2026-09-06 | 落盘润色；修正 § 交叉引用；登记 2026-09-06 已拷 CWR 八图 + 原有三图；SpecReady/Locked 引用已拷贴图 |
| 0.3 Living | 2026-09-06 | Stage 6–12 FX 落地：`HenshinFxDraw`；ThickBeam modes；SolarPrismBeam；大量 Status→Implemented |
| 0.4 Living | 2026-09-06 | Owner 审阅：花瓣禁真生成失控 AI；精神击破紫环；火焰牙改 BiteArc+OnFire；挖洞大招土环 |
| 0.5 Living | 2026-09-06 | Sheet 帧修复：Fire 4×4 / Flash 4×2 / HitJagged 1×2；SustainedBeam / WaterJet / DigLunge；射程同步 |
| 0.6 Living | 2026-09-06 | **SpawnAtMouse**：`FireMove` 后 `Center=MouseWorld`；大 AoE 改尺寸须保中心。**DrawBeamChain**：长束步进短段均匀 Alpha（`DrawBeamSegment`>120px 自动转链）。水柱渐进伸长；日棱开扇再会聚；龙怒蓝金双色+Skill 弱档；挖洞无预算/平移 |
| 0.8 Living | 2026-09-06 | 连续束 SoftGlow 拉长密叠（修复无界拉伸白屏/虚线）；水柱流动+命中渐缩；龙之怒 12/32 球 |
| 0.9 Living | 2026-09-06 | 光束宽度对齐格数；龙息密铺；DiffusionCircle 世界直径；龙怒 1.5 格+抬亮蓝晕；洁癖对齐 |
| 1.0 Living | 2026-09-06 | 澄清：`MagicPixel` 可用（忌无界拉伸）；新增项目 Skill `henshin-moves` |
| 1.1 Living | 2026-09-06 | Wave1 手感落地并**验收 Accepted**：抓狂三线爪宽判；火焰牙张口；闪焰多线火径+包裹+收尾减速；泡沫窄直线+破裂；念力索敌弹射+命中紫环；龙波直线 |
| 1.2 Living | 2026-09-06 | 全局 Rule `tml-api-docs.mdc`：设计须参考 tModLoader stable 类表 |
| 1.3 Living | 2026-09-06 | Stage6 修：岩崩微偏+大岩蛇12石±12格；爆裂拳巨大拳套前挥+20格石爆（不冲刺）；流沙保 Typhoon+压蓝+高A琥珀叠层 |
| 1.4 Living | 2026-09-06 | Stage6 **Accepted**：岩崩 `|vx|≤8` + 落地1格爆；整波验收回写 |
| 1.5 Living | 2026-09-06 | 岩崩落地去金光；污泥 ToxicCloud；大比鸟天候棒暴风/燕返双弧/勇鸟梭形+交叉鸟；金属怪+御三家终阶 Accepted |
| 1.6 Living | 2026-09-06 | 暴风直立帧（禁自旋）；哈克龙大招/快龙技能改 WeatherPain；大招主+4伴随、穿透牵引；洁癖对齐 |
| 1.7 Living | 2026-09-06 | 大比鸟/哈克龙/快龙暴风 **Accepted** |
| 1.8 Living | 2026-09-07 | Wave3 Accepted（睡眠白等） |
| 1.9 Living | 2026-09-07 | Stage7+ 落地：钢尾铁罩；猛撞灰日耀；暗影抓/恶波动32；彗星拳 StarWrath；破灭自缓加粗；星尘路径俯冲/画龙点睛；逆鳞火球；64 StarWrath；空气三段；神鸟吟唱；气旋32格 |
| 2.0 Living | 2026-09-07 | Stage7+ **Accepted**；画龙点睛改纯黑龙；气旋独立名键 `CycloneAttack`；超梦强念×6穿墙+精神击破64球 |

---

*End of `docs/fx-knowledge.md`.*
