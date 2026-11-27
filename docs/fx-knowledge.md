# 变身招式 FX 知识库

**Status：** Living（资源/手法目录；玩法数值以 `docs/move-effects.md` / 代码为准）  
**版本日期：** 2026-11-27

## 1. 权威与硬约束

| 优先级 | 文件 | 管什么 |
|--------|------|--------|
| 1 | `docs/requirements.md` §1.5 | 产品 FX 策略 |
| 2 | `docs/move-effects.md` + `docs/balance-stats.md` + 代码 | 招式语义；数值；实现 |
| 3 | **本文件** | VFX 目录、cookbook、踩坑 |
| — | `docs/backlog.md` | 缺口 / 下一步 |

本文件**不**改写玩法数值。状态与待办不写在此，见 backlog。

### 硬约束（与 AGENTS.md / 需求 §1.5 对齐）

1. **禁止 CWR 运行时依赖**（`build.txt` 不得 `modReferences` 大修；禁止 `GetMod("CalamityOverhaul")`）。
2. **资源优先级：** 原版复用 → **拷贝** CWR 进 `Assets/Fx/` → **可自制** FX 图（须规格确认）。见 §7。
3. **禁止擅自降级：** 跳过原版自管 AI 只留爆炸、`A=0` 假 Additive、纯尘冒充成品等，未经用户确认不得当作成品。MagicPixel **可用**（须控 destination/scale）。
4. **懒加载贴图：** 壳弹须 `SafeLoadProjectile`；禁在 `dedServ` / `Main.instance==null` 调 `LoadProjectile`。
5. **优先**原版真弹或壳弹；**禁止**生成灾厄弹作本模伤害载体。
6. **Sprite sheet：** `Fire` / `Flashimpact` / `HitJagged01` 禁止整图；用 `HenshinFxDraw.Draw*Frame`。

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
| **穿墙** | 真 Leaf 默认撞墙；诅咒之符 Spread 时 GlobalProjectile 继承 Delivery 并 `PostAI` 保 `tileCollide=false` |
| **为何 Accepted** | 真生成会顺带加载贴图；与吹叶机同外观 |
| **勿做** | 只刷 `DustID.Grass` 当成品；有伤无叶；为穿墙改成壳弹 |

### 2.2 泡沫光线 — `LoadProjectile(Bubble)` + `BorrowedVisualBoltProj`

| 项 | 内容 |
|----|------|
| **文件** | `RedesignedMoveProjs.cs`（`BarrageDirectorProj` ModeBubble）+ `BorrowedVisualBoltProj`；`ProjectileBorrow.cs` |
| **形态** | `L02_F02` 卡咪龟 Skill1；杰尼龟大招等同管线 |
| **手法** | **禁止**裸 `NewProjectile(Bubble)` 当伤害弹；壳弹 `ai` 传贴图 ID=`ProjectileID.Bubble`（**410**）；强制 `LoadProjectile(Bubble)` |
| **踩坑** | 未用过泡泡枪时 `TextureAssets.Projectile[Bubble]` 是 1×1 占位 → 无图。**联机：** `OnSpawn` 只在 `NewProjectile` 端调用；壳弹 `_texType` 若只在 OnSpawn 赋值，旁观端为 0，PreDraw 回退 `ProjectileID.Seed` → 别人的泡沫光线看起来像种子机关枪。须在 AI/PreDraw 从已同步的 `ai0` 再解析并 `SafeLoadProjectile`。专用服/`Main.instance==null` 跳过 Load。 |
| **勿做** | 等玩家先用泡泡枪；用水尘线冒充泡沫束；把贴图身份只写在 OnSpawn 的私有字段里 |

### 2.3 龙之波动 — NebulaArcanum 壳 + 爆炸碎片

| 项 | 内容 |
|----|------|
| **文件** | `Wave2MoveProjs.cs` → `NebulaPulseDirectorProj` / `NebulaPulseShardProj` |
| **形态** | 火恐龙、哈克龙、烈咬陆鲨、烈空坐等 |
| **手法** | 连发约 ×10；壳弹自管飞行（**不**跑原版 Nebula AI）；`LoadProjectile(NebulaArcanum)`（**617**）；亡时 `NewProjectile(NebulaArcanumExplosionShotShard)`（**620**）紫染；scale≈0.7、不追踪 |
| **踩坑** | 跳过自管位移只留爆炸 →「远处紫碎片有伤无弹」。`RetargetAsHenshin` 后的 620 走 `HenshinDamage`，若不打 `CrumbHitEnergy` 会按完整 `EnergyOnHit` 结算（10×12 碎片瞬间灌满能量条） |
| **能量** | 碎片命中 `1 × EnergyGainFactor`（须 `MarkCrumb` / ExtraAI）；击杀仍 `55 × Factor`。主弹 10 发仍 `12 × Factor`。其它 `RetargetAsHenshin` 弹（污泥毒云、真 Flames 等）**不会**自动走碎屑 |
| **延迟散射大招** | **大招后 ~2.5s 禁止一切充能**（`UltEnergyLockoutTicks`）。勿再钉弹上能量系数 / fail-closed；`SourceMoveSlot` 仅供 UltDamageBonus 等读出弹槽 |
| **勿做** | 直接挂原版 Nebula AI；灾厄龙弹；整招改 MultiHit 来压碎片充能（会误伤主弹） |

### 2.4 天雷 — `SkyBoltLightning` + ThunderTrail Additive 保 Alpha

| 项 | 内容 |
|----|------|
| **文件** | `SkyBoltLightning.cs`；贴图 `Assets/Fx/ThunderTrail` + `SoftGlow` + `LightShot` |
| **形态** | 皮卡丘大招（≤64 格+连锁+命中三叉）；雷丘技能1（≤128 格、无连锁、三叉）；电气球被动（无连锁、可三叉）；分叉/感电二次/`ai2≥1.5` 不再分叉；雷丘大招落雷 |
| **手法** | 折线多段 trail；**Additive 时保留 `Color.A`**；色为暖金 `SparkGold` `(255,168,28)` / 芯 `(255,206,64)`（避免近白）；前进模式 `ai1≥64` = 最大长度（像素）；命中后 16 格内最多 3 条分叉 |
| **复用** | `VoltTackleTrailProj`：加粗折线 + 形态贴图残影（长而淡） |
| **CWR** | 只读抄路径/包络；贴图已拷贝，无运行时依赖 |
| **勿做** | `A=0`；依赖大修 `CWRAsset`；把端点只写在 `OnSpawn` 私有字段（拖尾用生成点+velocity 同步） |

### 2.5 Typhoon 壳 — 火焰漩涡 / 起风

| 项 | 内容 |
|----|------|
| **文件** | `FlareBoltUltProj`（小火龙大招）；`GroundCycloneProj`（波波起风） |
| **贴图** | `ProjectileID.Typhoon`（**409**）；橙红 / 深蓝染色 + `LoadProjectile` |
| **火焰漩涡** | 默认 `tileCollide`；撞实心 **停飞不 Kill**，继续转圈伤到 `timeLeft`。诅咒符 Bolt 穿墙则不停 |
| **起风** | 从角色朝鼠标飞出；落地后水平贴地滚动（旧滚动）。诅咒符 Bolt 穿墙则不贴地。`HandlesOwnHoming` |
| **勿做** | 生成灾厄台风弹；Invisible hitbox 只伤；起风出生即扫格贴地 |

### 2.6 Boulder — 岩石封锁

| 项 | 内容 |
|----|------|
| **文件** | `RockTombDirectorProj` / `RockTombShardProj` |
| **形态** | 腕力 / 大岩蛇 Skill1；岩崩 `FallingBoulderProj` 亦用 Boulder |
| **手法** | 四向收拢；`ProjectileID.Boulder`（**99**）；棕染约 50% |
| **勿做** | 仅 DustID.Stone 环；无可见石块 |

### 2.7 咬住/咬碎/火焰牙 — destination Rectangle 尖牙

| 项 | 内容 |
|----|------|
| **文件** | `BiteArcProj` |
| **形态** | 卡咪龟/妙蛙草咬住；烈咬陆鲨/暴鲤龙/烈空坐咬碎；火恐龙火焰牙等 |
| **手法** | 身前咬合两拍；MagicPixel **但**每层 `destination Rectangle` 硬封顶宽高叠三角尖牙；整体 **VisualScale×4**（相对 size 倍率保留） |
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
| FireSpin/火焰漩涡 | L01_F01 Ult | HomingLock | `FlareBoltUltProj` Typhoon；**撞墙停飞不 Kill** | Typhoon Shell | 可见涡 | Accepted |
| DragonPulse/龙之波动 | L01_F02,4 | Bolt×10 | NebulaPulse* **直线连发** | Nebula 617/620 | 紫炸 | Accepted |
| FireFang/火焰牙 | L01_F02,4 | MeleeArc+OnFire | BiteArc **两对大弧牙**+OnFire；**VisualScale×4** | BiteArc cookbook | 火焰牙 | Accepted |
| FlareBlitz/闪焰冲锋 | L01_F02,4 | Lunge+Recoil | `LungeProj` **32格** 多线火径+包裹焰+收尾减速 | 火尘残影 | 可见冲锋 | Accepted |
| Flamethrower/喷射火焰 | L01_F03,7 | FlameCone | 真 Flames 射程×2（timeLeft72）+ **Fire 帧**（枪口小 TearFlame） | **Flames(85)** 真焰柱 | 喷火柱 | Accepted |
| DragonClaw/龙爪 | L01_F03,7 | Scratch | ScratchSlash **20格** + HitJagged **帧** | — | 远爪 | Accepted |
| Overheat/过热 | L01_F03 Ult | MouseAoE | 半径15格、5脉冲；Fire/Flash **帧** | InfernoFriendlyBlast | 可见火环 | Accepted |

### 4.2 水系 L02

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| WaterGun/水枪 | L02_F01,1 | Aqua | AquaScepter←WaterStream | WaterStream Shell | OK | Accepted |
| Tackle/撞击 | 多形态 | Lunge | `LungeProj` SoftGlow 残影 | — | 2s CD | Accepted |
| BubbleBeam/泡沫光线 | L02_F01 Ult / L02_F02 S1 | Barrage | Barrage+Borrowed Bubble（F02：**16** 泡、速 **20.8**） | Bubble Load | 密泡 | Accepted |
| Bite/咬住 | L02_F02,4 | BiteArc | `BiteArcProj` **VisualScale×4** | cookbook | 尖牙 | Accepted |
| Whirlpool/潮旋 | L02_F02 Ult | DoTBind | MouseVortex Cyclone 蓝 | Typhoon 蓝染 / `Assets/Fx/Cyclone` | 可见涡 | Implemented |
| HydroPump/水炮 | L02_F03,7 / L09 | Beam | `WaterJetProj` 宽×2；命中锁长**持续多段**再淡出 | SoftGlow 水柱+流动波节 | 水柱 | Accepted |
| SkullBash/火箭头锤 | L02_F03,7 | Charge→Lunge | Lunge 长 use | — | OK | Accepted |
| HydroCannon/加农水炮 | L02_F03 Ult | Beam | `WaterJet` cannon：宽×2+穿透+每3击爆 | 同水炮加粗+流动 | 粗柱 | Accepted |

### 4.3 草系 L03

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| VineWhip/藤鞭 | L03_F01,1 | Whip | GrassWhip | 草尘鞭 | OK | Accepted |
| SeedGun/种子机关枪 | L03_F01 Ult | Barrage | Barrage Seed | Seed Shell | OK | Accepted |
| RazorLeaf/飞叶快刀 | L03_F02,4 | Spread×5 | LeafSpread **New Leaf** | Leaf 206 | 标杆 | Accepted |
| Bite/咬住 | L03_F02,4 | BiteArc | BiteArc **VisualScale×4** | cookbook | OK | Accepted |
| SeedBomb/种子炸弹 | L03_F02 Ult | Bolt/AoE | 加大种弹+绿光拖尾；落点纯绿浓爆（DiffusionCircle/Fog/SoftGlow+绿尘；**禁 FlashImpact**） | Seed+草绿爆 | 种爆 | Accepted |
| SludgeBomb/污泥炸弹 | L03_F03,7 | Bolt+Poison | SludgeBolt + 毒气瓶 ToxicCloud 簇 | ToxicBubble→ToxicCloud | 毒云DoT | Accepted |
| PetalDance/花瓣舞 | L03_F03,7 | AoEBurst | 半径 **15格** FlowerPetal 壳环 | FlowerPetal Load | 可见瓣 | Accepted |
| SolarBeam/日光束 | L03_F03 Ult | ChargeBeam | 蓄力→`SolarPrismBeam` **~100tick** 跟鼠标会聚 | **LastPrism 金光** | 持续金棱 | Accepted |

### 4.4 电系 L04

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| ThunderShock/电击 | L04_F01 | Bolt | 金 spark + LightShot；沿飞行方向判定长度 = 宽度×2 | — | 暖金 | Done |
| QuickAttack/电光一闪 | L04_F01 | Blink | 身周 spark + 路径形态残影（无伤）；落点 3 段 ×3.0；冷却 3s | Kenney spark + 形态帧 | 残影 | Done |
| Thunderbolt/十万伏特 | L04_F01 Ult / L04_F02 S1 | Beam | SkyBolt 暖金（F02≤128 无连锁） | SkyBolt cookbook | 标杆 | Done |
| VoltTackle/伏特攻击（**1.42 起退役，雷丘技能2 = 放电，见 §4.10**） | L04_F02 | Blink+Trail | VoltTackle 暖金折线 + 金残影 | SkyBolt 折线 | 暖金 | Done |
| Thunder/打雷 | L04_F02 Ult | Pillar | SkyBolt 落雷同暖金 | 同天雷 | 暖金 | Done |

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
| DragonBreath/龙息 | L07_F01,6 | Spread+Stun | **128格** Fire 帧线；禁飞散 Flames；**线在实心截断**（诅咒符 Spread 穿） | Flames/紫火锥；`Assets/Fx/Fire` 按帧 | 火息线 | Implemented |
| Bite/咬住 | L07_F01,6 | BiteArc | BiteArc | cookbook | OK | Accepted |
| DragonRage/龙之怒 | L07_F01 Ult / L15_F01 S1 | Barrage | `DragonRageBarrage` 技能12/大招32 抖动球；**技能球撞实心爆，大招仍穿墙** | SoftGlow≈1.5格（晕抬亮#2108ad/芯#e7ce39）+5格爆 | 球体连射 | Implemented |
| DragonPulse | L07_F02,8 | Nebula×10 | NebulaPulse | cookbook | OK | Accepted |
| DragonTail/龙尾 | L07_F02,8 | Melee | DragonTailWhip 星尘龙节链15格强击退 | StardustDragon1–4 | 鞭弧 | Accepted |
| Hurricane/暴风 | L07_F02 Ult | Barrage | WeatherPain **直立帧** 主+**4伴随** 穿透牵引；命中4侧摆；**默认撞实心贴地**（诅咒符 Barrage 穿墙） | WeatherPainShot | 大招风团 | Accepted |
| Hurricane/暴风 | L07_F03 S1 / L11_F02 S1 | Barrage | WeatherPain 直立帧+穿透牵引；命中左右摆（不自旋）；**默认撞实心贴地** | WeatherPainShot | 天候棒 | Accepted |
| DragonDive/龙之俯冲（**1.42 起退役，快龙技能2 = 翅膀攻击，见 §4.10**） | L07_F03 S2 | Slam | `DragonDiveSlam` 瞬抬升空→垂直砸 | SoftGlow | 垂砸 | Done |
| Outrage/逆鳞 | L07_F03 Ult | Barrage | 3s CultistBossFireBall 壳追踪爆+Confused | CultistBossFireBall | 身周火球 | Accepted |

### 4.8 其余线路（钢超 / 鱼 / 地鼠 / 飞 / 超能 / 岩钢 / 陆鲨 / 传说）

| MoveKey/CN | Forms (Stage) | Playstyle | Current | Recommended | Feel | Status |
|------------|---------------|-----------|---------|-------------|------|--------|
| Confusion/念力 | L08_F01 / L12_F01 | Bolt+Stun | `PsychicWaveBolt` 48格索敌弹射×2 + **命中紫环1格** | ShadowBeam 294 | 波动链 | Accepted |
| ArmHammer/臂锤 | L08_F01 S2 | MeleeSmash | Kenney `fist_smash` 下砸（贴图再转 180°）+落地挤压；无 FlashImpact | `Assets/Fx/Kenney` | 重拳砸地 | Done |
| MetalClaw/金属爪 | L08_F01 Ult | DualSlash | 双侧 `scratch_down` 镜像 ±30°；自上而下划出/抹掉；持续伤直径≈16格 | Kenney scratch | 双侧爪痕 | Done |
| TakeDown/猛撞 | （无现役形态槽；类仍在） | Lunge+Recoil | TakeDownLunge 闪焰式冲+灰 SolarWhipSwordExplosion×4–6 0伤+土黄尘 | SolarWhipSwordExplosion | 金属冲（历史/可复用） | Legacy |
| Psychic/精神强念 | L08_F02 | Bolt+DefDown | AlakazamPsychic 鼠位×3（同胡地） | RainbowRod | 三连追 | Accepted |
| Psychic/精神强念 | L12_F02 | Bolt+DefDown | AlakazamPsychic 鼠位×3 穿墙穿怪否+延迟追 | RainbowRod | 三连追 | Accepted |
| MeteorMash/彗星拳 | L08_F02,10 | Melee | CometPunch 拳套+3×StarWrath 穿墙降 | BoxingGlove/StarWrath | 拳+星 | Accepted |
| HyperBeam/破坏光线 | L08_F02 / L09_F02 Ult | Beam | SustainedBeam 自缓+大招加粗 | DeathLaser 粗壳 | 持续粗束 | Accepted |
| Splash/跃起 | L09_F01,1 | StrikeFall | StrikeFall Water | — | 低伤 | Accepted |
| Flail/抓狂 | L09_F01 Ult | Barrage | FlailBarrage **15格三线爪** 宽判+抓尘 | 多爪环身 | 可见多段 | Accepted |
| HydroPump | L09_F02,9 | Beam | `WaterJetProj` 宽×2+锁长多段 | 同水箭龟水柱 | 水柱 | Accepted |
| Crunch/咬碎 | L09_F02 | BiteArc | BiteArc size↑ **VisualScale×4** | cookbook | OK | Accepted |
| Crunch/咬碎 | L15_F02 / L17 | MeleeArc | BiteArcAtMouse 鼠标处远距咬合 | BiteArc cookbook | 远距尖牙 | Accepted |
| MudSlap/掷泥 | L10_F01,3 | Bolt+Slow | MudSlap←DirtBall | DirtBall | OK | Accepted |
| Dig/挖洞 | L10_F02 S2 | DigLunge | `DigLungeProj` 冲20格+镐力走廊 | 土尘 SoftGlow | 挖进突 | Implemented |
| TripleDig/三连刺 | L10_F02,6 | Barrage | GroundSpikeStab **8格** | 三道可见刺/爪 | 可见刺 | Implemented |
| Earthquake/地裂 | L10_F02 Ult | AoE/Field | QuakeWave 小 Boulder 波前 | 裂纹+石 | 可见波 | Implemented |
| Gust/起风 | L11_F01,2 | GroundCyclone | 朝鼠标飞出；落地后水平贴地滚动 | Typhoon 深蓝 | 贴地风 | Accepted |
| Peck/啄 | L11_F01,2 | Cone | PeckCone | — | 尘锥 | Accepted |
| AerialAce/燕返 | L11_F01 Ult | Lunge | Lunge SoftGlow | — | OK | Accepted |
| BraveBird/勇鸟猛攻（**1.42 起退役，大比鸟技能2 = 空气斩 ×1.9**） | L11_F02 S2 | Lunge+Recoil | `BraveBirdLunge` 白拖尾梭形冲+交叉 Raven（降倍率技能2） | 闪焰式加速 | 梭形鸟群 | Accepted |
| WorkUp/自我激励（**1.42 起退役，大比鸟大招 = 热风 ×3.4**） | L11_F02 Ult | SelfBuff | `WorkUpBuffProj` 攻击 +20% / 12s | SoftGlow | 纯 buff | Done |
| ZenHeadbutt/意念头锤 | L12_* / L14 | StrikeFall | ZenHammer←Paladin锤 | PaladinsHammer | 较好 | Accepted |
| Psychic scatter Ult | L12_F01 Ult | Scatter | Barrage Rainbow | RainbowRod | OK | Accepted |
| FocusPunch/真气拳 | L12_F02 S2 | Melee | FocusPunch 白气上扬无石爆 | BoxingGlove+Fog | 白气拳 | Accepted |
| FutureSight/预知未来 | L12_F02 Ult | MarkBolt | FutureSightNightglow 1s显形后追爆 | FairyQueenMagicItemShot | 夜光标记 | Accepted |
| RockTomb | L13_F01,5 | RockTomb | Boulder | cookbook | OK | Accepted |
| GyroBall/陀螺球 | L13_F01 S2 | Smash | `GyroBallSmashProj` 旋转钢环砸出（**无**撞击 CD/释放者无敌） | SoftGlow 钢环 | 钢环砸 | Done |
| RockSlide | L13_F01 Ult / L13_F02 S1 | RockSlide* | 大招**12**石±12格微偏+落地1格爆**无金光**；技能3石同偏 | Boulder+RockShatter | 宽密落地爆 | Accepted |
| IronTail/钢尾 | L13_F02,9 | Whip | DragonTailWhip 底色+叠 `Color(0,0,16,80)` | StardustDragon | 铁灰罩 | Accepted |
| DoubleEdge/舍身冲撞（**1.42 起退役，大钢蛇大招 = 闪光炮 ×3.8，光弹 40% + 闪光爆 60%**） | L13_F02 Ult | Lunge+Recoil | `DoubleEdgeLunge` 加长猛撞+额外爆炸（被动免 Recoil） | SolarWhip 灰爆 | 钢冲 | Done |
| Aeroblast/空气爆炸（zh 名「气旋攻击」，key `Aeroblast`；1.42 起改画 `WindRing`） | L16_F01 S1 | Spread | AirBurst 朝鼠标路径节点爆（≤128格；撞实心贴壁；半径**4格**；吸**16格**） | DiffusionCircle/Fog | 路径爆+吸引 | Accepted |
| CycloneAttack→Hurricane/暴风（**1.42 起大招 key 为 `Hurricane`**，`WindRing` 风场） | L16_F01 Ult | Field | HurricaneField 半径**32格**；名键独立于 Aeroblast | ScaleForWorldDiameter | 大风场 | Accepted |
| SkyAttack/神鸟猛击（**1.42 起单只火鸟 `H7SkyAttackProj`，旧六鸟 `SkyAttackSextet` 退役**） | L16_F01 | Barrage | SkyAttackSextet 6红鸟分波（**无**释放者无敌/撞击 CD） | Raven 红染 | 六鸟 | Accepted |
| Psystrike/精神击破 | L14 Ult | MarkOrb | MewtwoPsystrike 指针16格选敌→8格渐显64暗影球齐冲 | DrawOpaqueDisk | 环伺齐射 | Accepted |
| Psychic/精神强念 | L14_F01 | Bolt | AlakazamPsychic×6 穿墙 | RainbowRod | 六连追 | Accepted |
| DragonRage | L15_F01,6 | Barrage | `DragonRageBarrage` mode=skill 12发 | SoftGlow 抖动球+5格爆 | 可见 | Implemented |
| Tackle | L15_F01,6 | Lunge | Lunge Dirt SoftGlow | — | OK | Accepted |
| SandTomb/流沙地狱 | L15_F01 Ult | DoTBind | 直径**16格** Typhoon壳压蓝 + 高不透明琥珀 Cyclone/Fog | Typhoon+Cyclone 深沙黄 | 琥珀涡 | Accepted |
| DracoMeteor/流星群 | L15_F02 Ult | Barrage | 64×StarWrath 壳朝下（rot−Pi/2）+金粉爆 | StarWrath | 天降星雨 | Accepted |
| DragonAscent/画龙点睛 | L17 Ult | Blink+Trail | 闪现立刻青色 SoftGlow 散落光迹；路径 Life≈48 **依次**反色 GraySolarBurst；生成时单次路径伤 | SoftGlow + GraySolarBurst invert | 青光迹+冷日耀 | Done |
| DragonPulse | L17 / L15_F02 | Nebula | NebulaPulse | cookbook | OK | Accepted |
| BulletPunch/子弹拳 | L05_F01 S2 | Melee | 短距鞭拳；判定略扩；单段 HitJagged（无新特效堆叠） | HitJagged | 快拳 | Done |

> **注：** **Stage 6 / Wave3 / Stage7+ 均已验收（2026-09-07）**。Stage7+ 当时含钢尾灰罩、猛撞灰日耀、暗影抓/恶波动、彗星拳 StarWrath、破灭自缓加粗、龙俯冲/画龙点睛纯黑龙、逆鳞火球、流星群64、洛奇亚三招、超梦强念×6+精神击破64球。**2026-09-25 改版批：** 画龙现为闪现+青 SoftGlow 光迹+延迟反色日耀（旧纯黑龙已退役）；金属怪臂锤/金属爪 Kenney；洛奇亚路径气爆；神鸟/陀螺去误无敌。

### 4.9 肉前特效批（2026-11-26，本地已验收，联机 pending）
共性：自绘贴图在 `Assets/Fx/<招式>/`，PreDraw 里 `ModContent.Request(..., ImmediateLoad)` 缓存到 static `Asset` 字段，不碰 `LoadProjectile`；出招沿线不撒粒子；数值/判定/冷却均未改；用 `OwnerMouseWorld` / `OwnerAimDir`，不用 `Main.MouseWorld`。

| 招式 | 类 | 贴图 | 要点 |
|---|---|---|---|
| 火花（L01_F01 S1） | `EmberBoltProj` | `EmberBolt` 36×22×4 + `_Light` | 朝右，rotation=速度方向，朝左半圈 FlipVertically 并镜像原点 Y；`_Light` 加法 ×0.7，与本体同一帧矩形；命中 12→3 颗 |
| 火焰漩涡（L01_F01 大招） | `FlareBoltUltProj` | `FireSpin` 56×56×4 + `_Light` | 3 臂每帧转 30° 无缝；帧号 `timeLeft/3 % 4`；`_Light` ×0.5，与本体同一帧矩形；只留撞墙停住后 1 颗/12tick 火星 |
| 撞击（非火系，含妙蛙种子/杰尼龟/鲤鱼王/地鼠） | `LungeProj` → `TackleHitFxProj` | `TackleHit` 24×24×5 + `_Light` | 出招不撒尘/不拖尾；命中生成 0 伤害冲击星，`ai0`=Dust ID 染色（Water 水蓝/Grass 草绿/其余奶白），只在拥有者端生成；火系（Torch）同走此星，`Tint` 染橙，出招/刹车期间也不撒粒子 |
| 跃起（鲤鱼王） | `SplashFallProj`（`FormItemUtil.SplashStrike`） | `SplashColumn` 20×44×5 + `_Light` | 原点 (10,42) 水柱脚底；判定同 `StrikeFallProj`（40×80/18 tick）；`StrikeFallProj` 留给电系，勿改；非拥有者端等 `ai0` 同步后才绘制 |
| 抓狂（鲤鱼王） | `FlailBarrageProj` | 共用 `Scratch/Scratch.png` | 按 `Len/贴图宽` 缩放，帧节奏 2/2/2/4/4 tick；每道 2 颗烟，命中血 3 颗 |
| 水枪（杰尼龟） | `AquaScepterShotProj` | 原版水弹 | 只降粒子：飞行中不撒水滴（水弹贴图本体可见） |
| 藤鞭（L03_F01 S1） | `GrassWhipProj` | `VineWhip` 24×20×3 | 二次贝塞尔逐段（≤20px）平铺，段朝切线旋转，朝左半圈 FlipVertically；末 6 tick 淡出；命中 2 颗草屑 |
| 种子机关枪（L03_F01 大招） | `SeedGunDirectorProj` + `SeedGunShotProj` | `SeedShot` 18×10 | 施放时 `OwnerAimDir` 锁方向；EasyCrit 经 ai1 同步；无拖尾；撞墙仍走 `HenshinTileImpact` Seed；旧 `BarrageDirectorProj.ModeSeed` 已无人用 |
| 飞叶快刀（L03_F02 S1） | `LeafSpreadProj` | 原版 Leaf | 只降粒子：枪口 15→5 |
| 种子炸弹（L03_F02 大招） | `SeedBombProj` + `SeedBombBlastProj` | `SeedBomb` 22×22、`SeedBombBlast` 160×160×6 | 爆圈 320×320 草系 AoE，24 tick，同弹 6 tick；整数 2 倍绘制；爆点只 4 颗草屑；爆点音效只播一次；爆点生成后 `Center` 校正并 `netUpdate = true`（让远端位置对） |
| 电击（L04_F01 S1） | `ThunderBoltHenshinProj` | `ThunderShockBolt` 20×9×4 | Color.White 自发光；飞行中无粒子；去 LightShot 光束段 |
| 电光一闪（L04_F01 S2） | `BlinkStrikeProj` 系 | `BlinkSpark` 13×13×4 | 身周 4 星换自绘；起止点 Dust 每端 2 颗；落点 Dust 概率降；残影/传送不动 |
| 啄（L11_F01 S2） | `PeckConeProj` | `PeckBeak` 134×20×4 | 贴图根 (3,10)，全长 254px=16 格判定；方向 `_dir`；朝左 FlipVertically；沿线粒子全删，仅 timeLeft==10 刺尖 1 颗风尘 |
| 燕返（L11_F01 大招） | `AerialAceLungeProj : LungeProj`（`FormItemUtil.AerialAceLunge`） | `AerialAceDash` 48×22×3 | 只覆写 PreDraw+OnHitNPC；工厂是整份初始化器（`MoveSpec` 属性 init），改 `Lunge` 数值时要同步；拖尾沿 `player.velocity`；基类 `LungeProj.AI` 仍每 tick 2 颗 Cloud |
| 起风（L11_F01 S1） | `GroundCycloneProj` | 原版 Typhoon | 只降粒子：每 5 tick 一次——贴地滚动时只出 `GroundDebris`，否则一缕云尘 |
| 子弹拳（L05_F01 S2） | `BulletPunchProj` | `BulletPunch` 48×20×3 | 拳头在右端，原点 (79,20) px；朝左上下翻转；不再画 HitJagged |
| 十字劈（Machop 大招） | `CrossChopArcProj` + `CrossChopDashProj` | `CrossSlash` 56×56×5 | 以射弹 Center 为中心按 `_dir` 旋转；X 对称不翻转；前飞段用同图最亮帧 ×0.6；`DrawCross` 为 `internal static`；`CrossChopArcXProj`（豪力）未动 |
| 挖洞爆发（L10_F01 大招） | `DigUltBurstProj` | `DigBurst` 56×56×5 | 首个 AI tick 记 `p.Bottom` 当锚点（非 OnSpawn，各端自算）；`Lerp(lightColor, White, 0.45)` 抬亮；删 Fog/DiffusionCircle，出土瞬间 3 颗 |
| 岩石封锁 / 掷泥 / 意念头锤 | `RockTombShardProj` / `MudSlapBoltProj` / `ZenHammerSmashProj` | 原版 | 只降粒子：碎裂 18→3；掷泥删尾迹；头锤落地 72→6、删每 tick 拖尾 |

保留不动：泡沫光线、潮旋水分支、十万伏特、龙之波动（`NebulaPulse*` 多形态共用，爆炸 28+12 偏多，待统筹）、念力、精神强念、咬住、闪焰冲锋（走 `LungeProj`）。

已知：`VineWhip`/`SeedShot`/`SeedBomb` 的 pxt check 有 1 WARN（内容贴帧边，藤段需首尾相接/单帧无邻帧）；其余新图 0 WARN。`LungeProj` 共享改动影响所有非火系撞击（命中尘 24→3 颗并多一个冲击星），逐形态未看。

#### R2 重做批（2026-11-26，本地已验收，联机 pending）

| 招式 | 类 | 贴图 | 要点 |
|---|---|---|---|
| 潮旋（L02_F02 大招） | `WhirlpoolUltProj`（`WhirlpoolUltProjs.cs`；工厂 `FormItemUtil.WhirlpoolUlt`） | `WhirlpoolRing` 192×192×8 + `_Light`；`WhirlpoolSpout` 112×184×6 + `_Light`（原点 art(28,88)） | 168t 全由 `timeLeft` 推出：0–12 卷起放大、12–150 旋转吸引（9 段：age 12–132 每 15t ×0.5，`Colliding` 半径随卷起放大、水柱前 10t 收口）、150–168 水柱托起 + ×1.4（`localAI[1]` 标志只触发一次）；`ShouldUpdatePosition=false`（广角镜不漂移）；命中圆 144px、吸力圈 288px、水柱期 7 格；吸力只在 `netMode != MultiplayerClient` 做，`netUpdate` 每 3t；Boss/`knockBackResist<=0` 不被吸；首 tick `Center`=`OwnerMouseWorld`；Ring 1.5 倍绘制（非整数）+ 反向旋转淡圈；`MouseVortex*` 未动（流沙地狱仍用） |
| 藤鞭（L03_F01 S1） | `GrassWhipProj`（在 `RedesignedMoveProjs.cs`，不在 G3GrassProjs.cs） | `VineWhip` 24×20×4 + `VineWhipCrack` 32×32×4 | 40 tick：蓄 8/甩 8/抽 12/收 12；13 控制点各读延迟 5tick 的挥角，鞭梢 tick 21「啪」（Item153 + Crack 闪光）；判定 12 线段 + 同序号控制点上一 tick→本 tick 扫掠线、age 10–32；控制点每 tick 缓存一次；拥有者求一次 `OwnerAimDir` 写 `ai1`（弧度+10）+`netUpdate`，远端等 `ai1` 同步后才计时/绘制；挥动无粒子，命中 2 颗草屑；`Range` 176/倍率/冷却不变；`VineWhip` pxt 1 WARN（藤段贴边，有意） |
| 燕返（L11_F01 大招） | `AerialAceUltProj`（拥有者端写 `p.Center`）+ `AerialAceSlashProj`（线段判定）+ `AerialAceBurstProj`（`AerialAceUltProjs.cs`；工厂 `FormItemUtil.AerialAceUlt`） | `AerialAceSlash` 80×80×4 | 锁定鼠标 520px 内最近敌（无敌用 320px 虚拟中心），5 斩星形（起终点在方块内沿轴缩短到空位 ≥40px，斩 `Colliding` 加 `CanHitLine` 不穿墙；不设 `RequiresLungeCooldown`，冷却中不白扣能量；轴线每次转 144°，各距中心 150px），斩 ×0.6×5 + 终结爆 ×2 ≈ 4.2；整段无敌+落地 40t，清速度/`fallStart`；斩/爆 spawn 带 `ai0=目标 ai1=tick ai2=序号 velocity=终点-起点`，`ShouldUpdatePosition=false`，旁观者同数据重画；残影复用 `BlinkStrikeGhostProj`；旧 `AerialAceLungeProj`/`AerialAceDash` 暂留未用 |
| 重踏（L10_F01 S2） | `BulldozeProj`（`BulldozeProjs.cs`；内联 `MoveSpec` 于 `DiglettForce.CreateMoveB`） | `Bulldoze` 36×40×5 | `MoveDelivery.Field`，`ShootSpeed=1` 只取左右；9 道土浪间距 30px、每 3tick 一道，推进 10px/tick；前 4 帧有 36×42 判定，寿命 39t；起点首 tick 锁进 `localAI[0..1]`（出生 X + 主人脚底 Y，之后不跟主人）；每 tick 按起点行+物块重算地面行（台阶≤3 爬升、坑≤10 下探，否则止步，不改地形）；同目标一次、Slow 90t；每道 1 颗 Dirt，命中 2 颗；状态全由出生速度+地形+年龄推出，无 OnSpawn 私有字段 |

未验：`WhirlpoolUltProj` 与 Homing/TilePierce 交互；藤鞭左向蓄力方向；燕返卡块时落点回退、`p.CCed` 中断；重踏斜坡/半砖、空中施放；各招联机旁观者一致性。

### Sleep Buff cookbook（Accepted 2026-09-07）
- `HenshinSleepDebuff` + `HenshinNpcGlobalNPC`：`PreAI=false` 定身、`CanHitPlayer=false`、首伤 `FinalDamage*=2` 后 DelBuff；**不清**已在飞敌弹。催眠施加端跳过 Boss。
- 睡眠 VFX：`DrawEffects` 强制 `Color(255,255,255,100)` + 头顶 3 路 `z/Z/Zz`。
- **禁止：** SoftGlow 几何罩；PostDraw 克隆贴图叠罩；弱 `Lerp→White` / `GetAlpha` 漂白（亮部不可见）。现役白即为用户确认规格。

### 经验世界字（`HenshinXpPopupSystem`，2026-09-09）
- `EXP +X` 钉击杀坐标；`LEVEL UP!` 跟玩家、连升连弹（错开 22 tick）。Boss 更厚描边+金白闪光。文案固定英文，不绑 buff、不绑已死 NPC。画法对齐睡眠 zzZ（`MouseText` + 描边）。
- **已达获取硬顶或满级：不生成 `EXP +X`**（`applied==0`）；击杀能量照给。口径见需求 §4.6.2。
- 时长约 1.5–2.2 秒（普通 EXP 90 tick / Boss 132 / 升级 108）；前 8% 淡入，45% 起渐隐到 0，到期移除（不突然消失）。

---

### 4.10 肉后特效批（2026-11-26；R2–R7 本地已验收 2026-11-27，剩联机）

语义见 `move-effects` §4.16。共同做法：

- 贴图在 `Assets/Fx/<名>/`（竖排帧条，scale 2），各组的加载器懒加载（`AsyncLoad`），专用服不 `LoadProjectile`（用 `SafeLoadProjectile`）。
- 命中特效统一由**拥有者端**生成零伤害一次性射弹，旁观者可见：`H1HitFxProj`（H1）、`H2HitFxProj`（H2）、`H3HitFxProj`（H3）、`H4HitFxProj`（H4）、`H5` 在 `H5MoveProjs.cs` 内、`H6BurstFxProj`（H6）、`H7HitFxProj`（H7）。
- 出招不沿线撒粒子；命中 1～3 颗。Additive 一律 `WithAlpha`，按帧绘制，禁整图。
- 方向：360° 招的瞄准角由拥有者写 `ai`/`localAI` 并 `netUpdate`，不用 `Main.MouseWorld`。

| 组 | 文件 | 共享工厂 / 类归属 |
|----|------|-------------------|
| H1 喷火龙/水箭龟/妙蛙花 | `H1Projs.cs`、`H1Moves.cs` | `H1Moves.Overheat/AuraSphere/PetalDance/SolarBeam`；`WaterJetProj` 在 `Wave2MoveProjs.cs`（水箭龟两招共用） |
| H2 雷丘/豪力/怪力 | `H2Projs.cs` | `H2Moves.Discharge`；劈瓦/爆裂拳/尖石/十字劈/近身战改在 `MachampMoveProjs.cs`、`SharedMoveProjs.cs` |
| H3 鬼斯通/耿鬼/胡地/超梦 | `H3FxProjs.cs` | `H3Fx`（贴图 + `Draw` + `SpawnHit`）；弹类在 `HaunterMoveProjs.cs`、`AlakazamMoveProjs.cs`；胡地/超梦共用 `AlakazamPsychic` 类，超梦用青蓝版 |
| H4 快龙/迷你龙/哈克龙 | `H4Moves.cs` | `FormItemUtil.WingAttack`（`StarterLines.cs`）、`WingAttackProj`；`DragonRageOrbProj`（`Wave2MoveProjs.cs`，龙之怒与 Gible 技能1 共用）；`DragonTailWhipProj` 同时画龙尾与铁尾（`ai1>0.5`） |
| H5 钢/岩系 | `H5MoveProjs.cs` | `FormItemUtil.FlashCannonUlt`（光弹 40% + 闪光爆 60%，`H5Fx.SpawnChild` 带 `delivery` 参数：光弹 `Bolt`、爆圈 `AoEBurst`）、`H5FallingRockProj`（`_variant` = `Projectile.identity % 3`，各端一致）；`RockSlideDirectorProj` 豪力/大岩蛇/大钢蛇共用；`SustainedBeamProj` 破坏光线与暴鲤龙共用，未动 |
| H6 地鼠/陆鲨 | `H6Projs.cs` | `H6SandTombProj`（仅圆陆鲨；卡咪龟潮旋仍 `WhirlpoolUlt`）；`MouseVortex` 的 `ProjectileType` 已指向它；`H6StoneBurst` 地震与隐形岩共用 |
| H7 飞行/传说 | `H7Projs.cs` | `FormItemUtil.AirSlash/HeatWaveUlt/SkyAttackBird`（`StarterLines.cs`）；`WindRing` 由 `AirBurstProj`（洛奇亚技能1）与 `HurricaneFieldProj`（洛奇亚大招）共用，放大 ≈9 倍 |

已知局限：暴风 `WeatherPainHurricane` 仍用原版天候棒贴图；`DragonStreak` 去掉头尾各 1 美术像素以过 pxt 0 WARN；`WaterStream`/`WaterCannon`/`SolarBeamBody` 的「触帧边」WARN 为首尾平铺所需（故意）。

### 4.11 肉后 R3 批（2026-11-27，本地已验收，剩联机）

语义与倍率见 `move-effects` §4.17。实现要点：

- **H5 岩崩 / 闪光炮**（`H5MoveProjs.cs`）：会被广角镜 `HomingAI` 追踪抖动的穿透/定点弹，一律 `HandlesOwnHoming => true`；碎石圈再 `ShouldUpdatePosition() => false`。
- **扇形/多射线判定**（劈瓦）：按「已扫过角度」取 11 根射线，`localNPCHitCooldown = Lifetime` 每目标一次；瞄左时垂直翻转贴图与角度。
- **逐刺落点判定**（地震，仿重踏）：首 tick 记起点，`ComputeSpikes` 每 tick 算落点，PreDraw 与判定共用。
- **自管位移牵引**（流沙地狱）：地面怪 AI 每 tick 覆盖速度，牵引须直接改 `position`（用 `Collision.TileCollision` 防穿墙）。
- **ai[] 同步变体**：念力波纹 variant 存 `ai[1]`、暗影球弹跳计数存 `ai[0]`，不靠 OnSpawn 私有字段；龙尾方向用 `ai2` + `netUpdate`。
- **穿墙判定**（空气斩）：`tileCollide=false` + 弹心埋入实心块连续 >6 tick 才 Kill，避免贴地被擦掉。
- **被动自伤限频**：「太阳之力」每命中扣血须带间隔门（`SolarDrainGate`），否则多段招式每秒扣数十血。
- 新贴图目录：`DragonPulse`、`DragonTailWhip`、`AirStreak`、`HeatWaveStreak`、`FocusPunch/FocusAura`、`ZenHammer`、`WingAttack/WingGust`；重画：`H6SandTomb`、`H6QuakeRock`、`BrickBreak`、`RockFall`。
### 4.12 肉后 R4 批（2026-11-27，本地已验收，剩联机）

语义与倍率见 `move-effects` §4.18。实现要点：

- **持续引导水柱**（水炮 `WaterJetProj`，仅 `ai2=0`）：按住时只保留 1 道。新生成者先 Kill 同主人旧柱；每 tick 续命 `timeLeft=5`，续命条件是 `owner.controlUseItem && !CCed && HeldItem 为 HenshinForceItem`；松手进入 8 tick 淡出，期间不判定。瞄向用 `OwnerMouseWorld`（NetOp.SyncAim），转向限速 0.16 rad/tick，禁 `Main.MouseWorld`。每 tick 重算长度：先 `HenshinTileRay.FirstSolidDistance` 截方块，再 `FirstNpcDistance` 截首个 NPC（取 NPC 沿射线投影与 Hitbox 相交的较小值），所以不穿透；伤害用 `localNPCHitCooldown=3`。水花只在截断端 10 tick 一次，且用 `H1HitFxProj`（自带客户端判断）。
- **广角镜排除**：不想被 `HomingAI` 追踪抖动的弹，写 `HandlesOwnHoming => true`（热风 `H7HeatWaveProj`、落石 `FallingBoulderProj`）。想自己追踪的（`H5FallingRockProj`），在自己的 `AI()` 里调 `HenshinProjUtil.HomingAI`。
- **命中停追**：`OnHitNPC` 时置 `ai[2]=1` + `netUpdate`，之后不再转向（R6 起不再只靠命中，见 §4.13；40 px 停转区已删）。
- **持续多跳招式的倍率分摊**：缩短 `localNPCHitCooldown` 时同步改 `damage/N`，使总伤保持在设计值（热风 3→8 间隔、`damage/3`→`damage/8`）。
- **牵引型 0 伤弹**（`WingFeatherPullProj`）：只在 `netMode != MultiplayerClient` 改 `velocity/position` 并 `netUpdate`；Boss 与 `knockBackResist<=0` 跳过。
- **爆点替换碎片**：龙之波动命中改 1 枚 AoE 弹，取代 12 枚碎片，减少弹数与粒子。
- **弧线判定与贴图同源**：劈瓦的判定射线与 `PreDraw` 共用 `ChopFrame(age)` 与 `FrameEndDeg`，贴图改动时两处一起改。
- **H3 FX 工具**（`H3FxProjs.cs`）：`H3Fx.DrawRipple` 画念力波纹；`SpawnHit` 仅拥有者客户端生成；新增 kind 6/7/8（`RingGold/RingCyan/RingPurple`）；`RingKind(variant)` 取色。
- 新贴图：`DragonPulse/PulseBlast`、`WingAttack/WingFeather`、`CometStar/CometStar`、`Psychic/PsychicRingA|M|T`。

### 4.13 肉后 R5–R7（2026-11-27，本地已验收，剩联机）

语义与倍率见 `move-effects` §4.19–§4.20。实现要点：

- **实心扇形判定**（劈瓦 `BrickBreakProj`，`Wave2MoveProjs.cs`）：圆心 = 玩家 `MountedCenter`，半径 184，角度从 -100° 累积到当前帧末角 +2°；40 px 内整圈必中；目标取 7×7 采样点 + 最近点。贴图 5 帧 400×400、帧中心 = 玩家，与判定同半径。
- **近战贴图只画动作，不涂判定区**：劈瓦画手刀 + 外缘细弧斩痕（贴判定外缘扫过，外缘最亮、往内往尾变淡橙）+ 落点冲击；从玩家身后画前臂连到掌根，肩端藏在玩家身下。全像素 alpha 0/255，无网格/抖点，无粒子。
- **下落追踪弹防掉头**（岩崩 `H5FallingRockProj`）：共享 `HomingAI` 在广角镜 `HomingTurn=1.0` 时会瞬间把速度整个转向目标且保速；穿过目标后会掉头朝上。修法全在本类内：拥有者端「与任一敌对 NPC 重叠」或「越过目标中心高度」即 `ai[2]=1` 永久停追；追踪后方向夹在竖直向下 ±55°；每 tick `velocity.Y ≥ 6`。不依赖 `OnHitNPC`（共享 `npc.immune` 无敌期内穿身不会触发）。其它用共享 `HomingAI` 的下落类弹未查同类问题。
- **汇集爆炸只由拥有者生成**（翅膀攻击 `WingFeatherBurstProj`，`R4DragonProjs.cs`）：牵引弹结束时拥有者端生成，避免多端重复。

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
| **Implemented**（2026-11-26，本地已验收，联机 pending） | 自绘 `Assets/Fx/TripleStab/TripleStab.png`：5 帧×2 tick 土棕岩刺，132×22/帧，刺根 (2,11)，全长 128 px＝判定线 8 格；受光抬亮，末帧碎开淡出，Kill 落土屑。命中 `TripleStabHitFxProj`（0 伤，ai0＝弧度）：`TripleStabHit.png`＋`_Light` 5 帧×3 tick 岩屑迸散，光层真 Additive 保 A。贴图 PreDraw 懒加载；去掉 AI 里的 `LoadProjectile(Stinger)` |
| **360°**（2026-11-26） | 导演弹在拥有者端施放首刺时用 `OwnerAimDir` 锁一次方向，弧度经 `GroundSpikeStabProj` 的 **ai1** 随生成同步；三刺沿垂直方向 ±10 px 错开；判定线、命中投影都按方向；绘制 `rotation`，朝左半圈 `FlipVertically`（origin y 取 frameH−11）。`TripleStabHitFxProj` ai0 改为弧度，同样旋转+翻转 |

#### 360° 近战批（2026-11-26，本地已验收，联机 pending）

原先按 `owner.direction` 只能左右出招，现统一改为朝鼠标 360°。共同规矩：拥有者端算一次 `HenshinProjUtil.OwnerAimDir`，`owner.ChangeDir` 跟瞄准 X；方向随生成同步（ai 槽）或首帧 `netUpdate`；绘制 `rotation`＋朝左半圈 `FlipVertically`；命中由拥有者端生成 0 伤纯视觉 HitFx 弹；贴图 PreDraw 懒加载；Additive 层保 A。

| 招式 | 类 | 方向同步 | 贴图 | 要点 |
|----|----|----|----|----|
| 抓 / 龙爪 | `ScratchSlashProj`＋`ScratchHitFxProj` | ai1＝弧度+10（0＝未锁），首帧 `netUpdate`；ai2 外观（0 抓 / 1 龙爪，工厂按 reach>8 格设） | `Assets/Fx/Scratch/Scratch.png` 60×50×5（三道弯月爪痕）；`Assets/Fx/DragonClaw/DragonClaw.png` 320×58×5 | 三条 ±16 px 线段判定；龙爪沿线叠 `DrawHitJaggedFrame`；fullbright，原点左端中线 |
| 咬住 / 火焰牙 / 咬碎 | `BiteArcProj`＋`BiteHitFxProj` | ai0/1/2 已占（体型/破甲/灼烧），弧度走 `SendExtraAI`，首帧 `netUpdate`；远端收到前不画不判 | `Assets/Fx/Bite/`：`BiteJaws`（咬住、咬碎共用）、`FireFang`（两对大弧牙），各带 `_Light`，6 帧×3 tick；`BiteHit(+_Light)` 5 帧×3 tick | 判定 OBB vs AABB（SAT），半长 32s×28s；咬碎钢紫调＋咬合抖动＋命中 2.2 倍；`BiteArcAtMouseProj` 不动 |
| 暗影抓 | `ShadowClawSlashProj`（导演）→`ShadowClawStrikeProj`＋`ShadowClawHitFxProj` | 弧度经 Strike 的 ai1 随生成同步 | `Assets/Fx/ShadowClaw/`：`ShadowClaw` 328×92×6（爪根 (4,46)）、`ShadowClawHit` 80×80×5，各带 `_Light` | 三条 26 px 间距 20 格线段；命中 ShadowFlame 180 tick；光层须压暗（`#1a0840`/`#34116e`/`#4e2090`）否则冲白 |

不在本批：起风 / 暴风（设计即贴地滚）、远距咬碎（已跟鼠标）、近身战附带 `GenericSlashProj`。

**粒子纪律（本批）**：贴图已表达轨迹，出招期间**不沿线撒 Dust**（会挡住贴图）；只在咬合/命中瞬间留 2～4 颗点缀（咬碎最多 4）。新增同类贴图招式照此办理。

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

- **目标：** **连续火焰喷射**；PreDraw 用 **Fire 帧**；枪口可选小 TearFlame；真 Flames 寿命上限 **72**（射程约 ×2）。
- **复用：** `Flames` **85** 真生成 Retarget；`DrawFireFrame`。
- **Files：** `FlameConeDirectorProj`。

### HydroPump / HydroCannon（水炮 / 加农水炮）— Implemented

- **目标：** **粗水柱**，跟鼠标；枪口约 24 tick 渐进伸长；水炮宽 **≈2～3 格**（不穿透，**命中墙=命中怪**锁长后约 **54 tick** 持续多段伤再 **20 tick** 淡出）；加农 **≈4～5 格**（**仍穿墙穿怪**，每 3 击半径 5 格水爆）。诅咒符 Beam 让水炮穿墙但仍撞怪锁长。
- **复用：** `WaterJetProj`：`DrawContinuousBeam` + `DrawWaterFlowRipples`（流动波节）；工厂 `FormItemUtil.WaterJet`。
- **Files：** `WaterJetProj`；Blastoise / Gyarados。

### DragonRage（龙之怒）— Implemented

- **非光束：** 技能 **12** / 大招 **32** 发 SoftGlow 球体（直径 **≈1.5 格**），直线 + 垂直抖动；外晕抬亮蓝紫 + 金芯 `#e7ce39`；尘粒两色插值；命中直径 **5 格**。技能球撞实心即爆；**迷你龙大招仍穿墙**。诅咒符 Barrage 让技能球也穿墙。
- **为何不用原版球弹：** 色/AI 绑死；要精确龙色 → SoftGlow 壳。
- **Files：** `DragonRageBarrageProj` / `DragonRageOrbProj`；`FormItemUtil.DragonRage`。

### DrawContinuousBeam（防白屏 / 防虚线）

- SoftGlow 沿路径 **拉长 + 密叠**（spacing≈戳记长度×0.22），厚度允许到约 3 格。`MagicPixel` 亦可用，但须封顶宽高；无界拉伸易白屏。
- `ScaleForWorldDiameter`：DiffusionCircle（360px）等大图必须按世界直径换算，禁止裸 `scale=1.7`（会画出超大圈）。
- **Additive 暗色：** `#2108ad` 作 SoftGlow 色几乎不可见 → 光晕用抬亮同色相。
- **Typhoon 改沙黄：** 蓝贴图乘黄 → 绿；保壳时压蓝（B≈0）+ 降壳 Alpha，再用高不透明琥珀 Cyclone/Fog 盖色。

### SpawnAtMouse 中心校正（踩坑）

- `Projectile.NewProjectile(spawn,…)` 的 `spawn` 是 **左上角**。大 width/height（过热 480、流沙 256 等）会偏到鼠标右下。
- **修正：** `HenshinForceItem.FireMove` 在 `SpawnAtMouse` 时把弹幕 `Center` 校正到**主人**鼠标（`HenshinPlayer.GetMouseWorld`）；AI 内改尺寸须先存 `Center` 再还原。

### 联机视觉/指向（踩坑 + 检验清单）

根因：**钩子在所有端跑，但读到的状态只有本机才有。** 伤害常在主人端结算所以「打人是对的」，画面按旁观者的本地默认值画。

tML：`OnSpawn` **只**在 `NewProjectile` 那一端调用。旁观端只有 `SetDefaults` + 已同步的 `ai[]` / `velocity` / `Center`。大修瞄准走 InnoVault `PlayerNetwork`；本模禁止该依赖，用 `NetOp.SyncAim` + `HenshinProjUtil.OwnerMouseWorld`。UI（`Main.mouseItem`）仍用本机鼠标。

| 类别 | 本机才有的状态 | 旁观端表现 | 已见例子 | 怎么写 |
|------|----------------|------------|----------|--------|
| **A. 本机指针** | `Main.MouseWorld`、`Main.screenPosition`（当索敌/落点） | 别人的鞭/束跟着我的鼠标 | 藤鞭、龙息、水炮 | AI/Draw/Colliding 用 `OwnerMouseWorld`；变身时 `SyncAim` |
| **B. OnSpawn 私有字段** | `_texType` `_dir` `_anchor` `_center` `_startOff` | 默认贴图/方向/原点；或长成另一招 | 泡沫→种子；尖石短刺；岩封锁不收拢；精神击破球飘向原点 | 从 `ai[]`/`velocity`/`Center` 重建（`Ensure*`）；禁止 OnSpawn 清零 velocity 再当唯一方向源 |
| **C. 无主端生成** | `NewProjectile` 未挡 `owner == myPlayer` | 弹数翻倍、声音叠 | 导演弹、破裂碎片 | 生成/改砖/传送只主人端 |
| **D. 旁观端 Kill** | 用本地失败条件 `Kill()` | 我这边招式提前消失 | 精神击破导演找不到怪 | 无目标/失败只主人 `Kill` |
| **E. 懒加载贴图** | `TextureAssets` 未 Load | 1×1 空白；专用服裸 `LoadProjectile` 会 NRE | 泡沫未用过泡泡枪；listen 服 EnsureVisuals | 绘制前 `SafeLoadProjectile` / `RequestProjectileTexture` |

写招式过一遍：私有字段旁观端第一帧从哪来；有无本机鼠标/屏幕当玩法输入；生成是否只主人；`Kill` 是否本地失败条件；贴图有无 Load；默认回退会不会长成另一招。

**可接受：** 尘粒 `Main.rand`、折线电形状每端略不同；主人位移靠原版弹幕包插值。

**代码已接线、双端验收 pending：** `SyncAim`、壳弹 `EnsureVisuals`、尖石/岩封锁/精神击破锚点重建。

### DrawOpaqueDisk / Extra98（鬼斯通）

- **深紫实心圆**（`#220033`/`#330066`）禁止 Additive SoftGlow（暗色几乎不可见）；用 `HenshinFxDraw.DrawOpaqueDisk`（AlphaBlend + DiffusionCircle）。
- **暗影球拖尾：** 路径 history lag 画 5 节递减盘，仅 tip hitbox 有伤（等价「首节伤、尾纯 VFX」）。
- **舌舔：** `Assets/Fx/Extra98` 竖条沿根→尖重叠绘制；`ShouldUpdatePosition=false`；线碰撞；命中收舌。

### 撞墙反馈 `HenshinTileImpact`（代码已接线，本地已验收，联机 pending）

| 项 | 内容 |
|----|------|
| **文件** | `HenshinTileImpact.cs`：`FromCollide`（飞弹 `OnTileCollide`）/ `At`（线截断点）/ `GroundDebris`（贴地卷尘） |
| **三层** | ① `Collision.HitTiles` 按物块材质喷碎屑 ② 属性尘沿撞击面法线喷（侧墙横喷 / 地面上喷 / 天花板下喷） ③ 原版 `SoundID`，**按 kind 节流** |
| **法线** | `OnTileCollide` 里比较 `velocity` 与 `oldVelocity`：X 被清零=侧墙，Y 被清零=地/顶；线类用 `-aim` |
| **预设（当前主选）** | Electric `Item93` 4t · Water `SplashWeak` 3t · Mud `Dig`+`SplashWeak` 3t · Seed `Grass` 4t · Psychic `Item9` 3t · Fire `Item20` 6t · Wind `DD2_BetsyWindAttack` 8t · DragonFire `Item20` 16t（备选：Electric `Item94`、Water `Item85`、Psychic `Item27`、Fire `Item74`、Wind `Item1`，待试听定稿） |
| **接线** | 电击、水枪、掷泥、种子机关枪（种子分支）、胡地精神强念：撞墙即 `FromCollide`；火焰漩涡首次停飞 Fire 爆 + 贴墙磨火星；起风/暴风首次落地 Wind 气浪，掉头/下台阶小气团（20 tick 冷却），贴地每 5 tick 卷尘；水炮墙截断 `Splash` + 锁长期反溅；龙息截断点持续龙火 |
| **规则** | 只挂 `OnTileCollide` / 截断分支，**不挂 `OnKill`**（命中/超时不出墙效）；纯本地粒子+声，不生成射弹、不发包，`Main.dedServ` 早退；穿墙（诅咒之符、超梦强念、加农）天然不触发 |
| **勿做** | 每发撞墙用 FlashImpact / DiffusionCircle 大图（连发糊屏）；撞墙效果强于同招命中；不节流的连发音 |

验收通过后再升入 §2 Accepted cookbook。

---

## 7. CWR 拷贝库存（只读参考）

### 7.1 本模 `Assets/Fx/`（截至 2026-11-26）

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
| `Kenney/`（25 张：fist / scratch / slash / spark / flash / muzzle / scorch / star / twirl + `ExplosionStrip`） | 臂锤、金属爪、电光一闪等 | **2026-09-25 引入**；Kenney CC0，**非** CWR 拷贝 |

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

> 旧「禁止新增 FX 图」口径已废（requirements **v1.4.26**）：允许自制；优先拷贝。

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
| 撞墙反馈 / 墙截断射线 | `HenshinTileImpact.cs` / `HenshinTileRay.cs` |
| 撞击族/传说改版批（ArmHammer、MetalClaw、AuraSphere、GyroBall、StealthRock、BulletPunch、DoubleEdge、DragonDive、DragonAscent） | `BatchRedesignProjs.cs` |
| Stage7 招式（TakeDown、ShadowClaw、DarkPulse、CometPunch、Outrage、DracoMeteor 等） | `Stage7MoveProjs.cs` |
| 胡地（Psychic、FutureSight、FocusPunch） | `AlakazamMoveProjs.cs` |
| 怪力（StoneEdge、CrossChop、CloseCombat） | `MachampMoveProjs.cs` |
| 龙尾鞭 | `DragonTailWhipProj.cs` |
| 肉后特效批 H1–H7（分组见 §4.10） | `H1Projs.cs` / `H1Moves.cs`、`H2Projs.cs`、`H3FxProjs.cs`、`H4Moves.cs`、`H5MoveProjs.cs`、`H6Projs.cs`、`H7Projs.cs` |
| 龙之波动爆点、翅膀羽毛牵引/汇集爆炸 | `R4DragonProjs.cs` |
| 无限大招调试物品 | `Content/Items/Debug/UltChargeDebugItem.cs` |
| 绘制积木（Additive 批次、Sheet 帧、连续束、世界直径） | `HenshinFxDraw.cs` |
| FX 贴图 | `Assets/Fx/`（§7） |
| 形态接线 | `StarterLines.cs` / `CombatLinesA.cs` / `UtilityAndLegend.cs` |
| 玩法表 | `docs/move-effects.md` |
| 缺口/下一步 | `docs/backlog.md` |
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
| 2.1 Living | 2026-09-10 | 龙之波动 620 碎片命中能量改为碎屑 `1×Factor`；击杀不变 |
| 2.2 Living | 2026-09-10 | 诅咒之符 + 诅咒焰；火焰漩涡停飞不 Kill；水炮墙=怪；暴风默认贴地；龙息截断；技能龙怒撞实心 |
| 2.3 Living | 2026-09-10 | 暴风 Delivery=Barrage（不是 Field）；诅咒符穿墙不含 Field |
| 2.4 Living | 2026-09-11 | 联机 `SyncAim`：招式指向效果读主人鼠标，禁止射弹 AI 直接 `Main.MouseWorld` |
| 2.5 Living | 2026-09-11 | 壳弹 `BorrowedVisualBoltProj` 旁观端从 `ai0` 解析贴图；修泡沫光线联机画成种子 |
| 2.6 Living | 2026-09-11 | 联机检验清单（与瞄准/OnSpawn 踩坑合并为单节）；尖石/岩封锁/精神击破球同样按 OnSpawn 字段重建 |
| 2.7 Living | 2026-09-11 | `SafeLoadProjectile` 跳过专用服；卡顶不生成 `EXP +X` |
| 2.8 Living | 2026-09-12 | 对齐 requirements v1.4.26：允许自制 FX；优先拷贝；入口改 backlog |
| 2.9 Living | 2026-09-12 | Bolt/Barrage 外飞壳寿命约 ×1.5；念力索敌 **48 格**；暴风 `SuckRange` 同步；初速/伤倍不动 |
| 2.10 Living | 2026-09-13 | 手感批：种爆~10格草绿环；尖牙×4；起风飞出+贴地滚；水炮/加农粗×2+水炮锁长多段；喷射射程×2 |
| 2.11 Living | 2026-09-14 | 起风确认贴地滚（非爬墙）；种爆去 FlashImpact 金刺，仅绿光/绿尘；本地验收 OK |
| 2.12 Living | 2026-09-25 | 撞击族/传说改版批回写：画龙青 SoftGlow + 延迟反色日耀 + 单次路径伤；臂锤/金属爪 Kenney；勇鸟→S2+WorkUp Ult；龙俯冲垂砸；陀螺/舍身；去误无敌；旧纯黑龙/TakeDown 槽位退役说明 |
| 2.13 Living | 2026-09-26 | 电光一闪：身周 Kenney spark（非落点尘爆）；单段 ×3.0 ×3 段；撞击冷却 3s |
| 2.14 Living | 2026-09-27 | 皮卡丘/雷丘电系招式统一暖金 `SparkGold` `(255,244,150)`：十万伏特、落雷、伏特攻击、电击 |
| 2.15 Living | 2026-09-27 | 暖金压深 `(255,168,28)` / 芯 `(255,206,64)`；电光一闪路径残影；电击沿飞行方向判定×2 |
| 2.16 Living | 2026-09-27 | §6 撞墙反馈 `HenshinTileImpact`：材质碎屑+属性尘+节流音；10 个在役撞墙/截断/贴地弹接线；穿墙弹不变 |
| 2.17 Living | 2026-11-26 | 洁癖对齐：§7.1 补 `Kenney/` 库存；§8 补 6 个未索引的招式/绘制文件 |
| 2.18 Living | 2026-11-26 | §4.10 肉后特效批：24 个之力 FX 重做/换招的实现要点与共享工厂归属；见 `move-effects` §4.16 |
| 2.19 Living | 2026-11-27 | §4.11 肉后 R3 批实现要点（自管牵引、逐刺判定、HandlesOwnHoming、ai[] 变体、被动自伤限频）；见 `move-effects` §4.17 |
| 2.20 Living | 2026-11-27 | 增 §4.12 肉后 R4 实现要点（持续引导水柱、广角镜排除/命中停追、牵引弹、弧线判定同源），语义见 `move-effects` §4.18 |
| 2.21 Living | 2026-11-27 | 增 §4.13 R5–R7 实现要点（劈瓦实心扇形 + 只画手刀/斩痕/前臂、岩崩防上飞、汇集爆炸拥有者生成）；§4.10–§4.13 标本地已验收、剩联机；§8 补 H1–H7、`R4DragonProjs.cs`、调试物品 |

---

*End of `docs/fx-knowledge.md`.*
