# Pokemon Henshin — 开发计划

| 项 | 内容 |
|----|------|
| 版本 | 1.2 |
| 对齐需求 | `docs/requirements.md` **v1.2** |
| 状态 | **M0 已完成**（2026-09-04 代码 + 单机游戏内验收通过）；**当前执行 M1**（供未读聊天记录的 Agent 直接执行） |
| 参考实现 | `C:\Dev\projects\misc_prj\CalamityOverhaul`（只学模式，不照搬玩法；**禁止修改该仓库任何文件**） |
| 产出约束 | 本文件为计划；实现期再写游戏代码 |

**产品真相唯一来源：** `docs/requirements.md`。本计划冲突时以需求文档为准。

### 资源与特效硬约束（需求 §1.4 / §1.5）

| 类型 | 规则 |
|------|------|
| **宝可梦 Overlay / 图标** | 可从 [52poke 全国图鉴列表](https://wiki.52poke.com/wiki/宝可梦列表（按全国图鉴编号）) 自行获取对应精灵图，按 FormId 放入 `Assets/Forms/` |
| **特效（FX）** | **禁止**向仓库新增任何专用特效图片。视觉表现必须复用或参考：**原版** Dust/Projectile/Gore，或 **灾厄** 已有资产写法，或 **CalamityOverhaul** 的特效实现思路（可改代码组合与参数，不引入大修运行时依赖、不照搬玩法） |
| **允许** | 改 FX 逻辑、颜色、缩放、轨迹、生成数量；`TextureAssets` / 已有 Mod 贴图路径引用 |
| **禁止** | 自制 dust 图集、自制弹幕序列帧、为「更好看」加新的 FX png/gif |

实现招式与天气时：先在大修或原版中定位相近特效（火焰、水系弹、雨场、影球等），再裁剪参数接入本模。

---

## 1. 目标与范围

### 1.1 目标

在泰拉瑞亚 + tModLoader + 灾厄环境下，实现「**{宝可梦名}之力**」换皮武器模组：

- 热键栏**持握** = 变身（外观 Overlay、双招式、属性情境被动、仅变身生效的饰品）
- **取消持握**同 tick 清除本模全部相关效果
- 持握期间**禁止坐骑**
- 独立伤害类型 `HenshinDamage`，职业专精按 **k = 0.35** 折算
- 随 `ProgressStage` 进化（物品替换，保留前缀与收藏）
- 开局发放御三家；第一版约 **17 链 / 36 形态** + **12 饰品**
- **联机必须一致**（形态、伤害、进化、天气场伤害侧、地形变更）

### 1.2 里程碑（对齐需求 §13）

| 里程碑 | 一句话 | 完成标准（摘要） |
|--------|--------|------------------|
| **M0** | 小火龙竖切：状态机 + Overlay + 伤害 + 禁坐骑 + SyncForm | 持握变身/切换解除；第二客户端可见；坐骑不可用 |
| **M1** | 进度查询 + 进化 UI + 二阶演示 + 开局御三家 | 银行不自动进化；前缀/收藏继承 |
| **M2** | 火/水/飞情境被动子集 + 2 件饰品 | 未持握饰品不生效 |
| **M3** | 天气场一例、穿障一例、挖掘一例 | 硬限制达标；联机一致 |
| **M4** | 填满形态数值、饰品、每档 DPS 抽检 | 对标 80%～120% |
| **M5** | 负面用例、本地化、资源替换路径 | 发布候选 |

### 1.3 第一版明确不做 / 延后（防膨胀）

- **不做（需求 §1.3 + 本计划加严）：**

- 完整图鉴养成、PvP 属性克制、给敌人标宝可梦属性
- 第二套完整体型角色控制器；碰撞级别 B/C 以外的永久穿墙
- 依赖 InnoVault / SubworldLibrary（大修强依赖；本模不跟）
- 照搬役鬼、传说武器模块、鬼雨世界观等大修玩法内容
- 全球天气（档 &lt; 9 或招式未标注）
- 第一版穿障形态超过 2 个
- 未变身也有强力隐藏饰品效果
- **任何新增专用 FX 图片文件**（特效只复用原版 / 灾厄 / 大修写法）

**延后到 M4+ / 后续版本：**

- 碰撞级别 B 全面启用（M0～M3 仅 A；B 仅挖掘形态试点且可回退）
- 冷门属性全量（冰等可占位）
- 对标武器 ItemID 钉死表、招式精确弹幕参数
- 公开分发商标策略（工程上先做显示名/路径可配置）
- 全形态 52poke 精灵图精修裁切（M0 可用单帧；M4 前按表补齐）

---

## 2. 工程脚手架

### 2.1 解决方案 / 目录（M0 已定：扁平布局）

**仓库根即 tML 模组根**（模组内部名 = 文件夹名 `PokemonHenshin`）。`ModSources\PokemonHenshin` 是指向本仓库的目录联接，供游戏内 Build + Reload；命令行在仓库根 `dotnet build` 会经 `tMLMod.targets` 自动调用 tML 打包（游戏运行且启用本模时打包会被 TML003 拒绝，只能游戏内 Build + Reload）。

```
PokemonHenshin/                # 仓库根 = 模组根
  docs/                        # requirements.md（产品真相）、dev-plan.md（本文件）；buildIgnore
  PokemonHenshin.csproj        # 仅 Import ModSources\tModLoader.targets；docs\** 从编译排除
  build.txt                    # modReferences = CalamityMod；buildIgnore 含 docs、*.md、.git、.cursor、bin、obj
  description.txt / description_workshop.txt / icon.png
  PokemonHenshinMod.cs         # Mod 入口：Instance + HandlePacket → HenshinNet
  Localization/                # en-US / zh-Hans hjson（tML 首次加载会自动补缺失键）
  Assets/Forms/                # 宝可梦贴图，按 FormId 命名；物品图标直接复用同图
                               # 禁止：新增任何自制特效图；FX 用原版/灾厄/大修已有资源
  Content/
    Core/                      # FormDefinition（含 MoveA/MoveB）、FormRegistry、MoveSpec、PokemonType
    Damage/                    # HenshinDamage
    PlayerState/               # HenshinPlayer（唯一变身权威状态机）
    Visual/                    # HenshinOverlayLayer
    Combat/                    # HenshinForceItem 基类；Moves/ 招式弹幕
    Items/Forms/               # 各形态物品（CharmanderForce = L01_F01）
    Net/                       # HenshinNet + NetOp 枚举
    # M1+ 按需新增：Affinity/、Evolution/、Accessories/、WeatherField/、TerrainEdit/、Data/
```

**命名空间根：** `PokemonHenshin`  
子空间与上表模块一一对应，例如 `PokemonHenshin.Content.Core`。

### 2.2 Mod 元数据（`build.txt`，现役）

```
displayName = Pokemon Henshin
author = PokemonHenshin Team
version = 0.1.0
modReferences = CalamityMod
sortAfter = CalamityMod
```

- **强依赖 Calamity：** 需求明确平台含灾厄；进度档大量 `DownedBossSystem` 字段。`modReferences = CalamityMod`，无灾厄不可加载。
- **弱依赖策略：** 其他模组（Boss Checklist、Magic Storage 等）一律 `weakReferences` + 可选反射；**不要**引入 InnoVault。
- **钉死版本（2026-09-04）：** tModLoader **1.4.4.9 / 2026.07 stable（net8.0）**，CalamityMod **2.2.4**（需求 §12.8）。
- **编译期引用灾厄 dll：** M0 未引入（盗贼类走 `ModContent.TryFind`）。M1 起需要 `DownedBossSystem` 时，在游戏内 Workshop → Calamity → Extract 得到 `ModSources\ModAssemblies\CalamityMod_v*.dll`，在 csproj 加 `<Reference Include=... Private="false" />`。

### 2.3 对 Calamity 的访问策略

| 策略 | 用途 | 参考大修 |
|------|------|----------|
| 编译期引用 Calamity | `DownedBossSystem`、怒气/肾上腺素若公开 API 可直接用 | 大修用反射因弱引用；本模**强依赖可直接引用**公开类型 |
| 薄适配层 `CalamityProgressAdapter` | 把灾厄 `downed*` 映射到本模 `ProgressStage`；灾厄改名只改适配层 | `CWRRef.cs` 中 `DownedBossSystem` 属性表（`GetDownedDesertScourge` 等） |
| 禁止散落硬编码 | 物品/弹幕里不直接写十几个 `downedXxx` | 同左 |

**怒气 / 肾上腺素：** 优先走 Generic 伤害继承；若实测未计入，再在适配层读 `CalamityPlayer` 相关字段补乘（大修缓存见 `CWRRef` 的 `calPlayer_rage_*` / `adrenaline_*`）。

### 2.4 联机包设计（简化版大修信道）

大修：`CWRNetWork.cs` + `CWRNetChannel.cs` + 各功能 `*Net`（如 `Content/Wraiths/Runtime/WraithNet.cs`）。

本模裁剪：

- 单入口 `PokemonHenshinMod.HandlePacket` + `NetOp : byte` 枚举
- **不要**照搬按类型全名自动编号（小模组用显式枚举更稳、可读）
- 最小包（需求 §8）：`SyncForm`、`SyncPhasing`、`SyncWeatherField`、`RequestEvolve` / `ApplyEvolve`、`TerrainBudgetReject`

---

## 3. 从大修借鉴的模式清单

对每个点：**参考路径 → 本模如何裁剪**。

### 3.1 自定义 DamageClass / 伤害折算

| | |
|--|--|
| **参考** | `Content/DamageModify/EndlessDamageClass.cs`、`MeleeMagicDamageClass.cs`、`RangedMagicDamageClass.cs` |
| **学什么** | `GetModifierInheritance` / `GetEffectInheritance` 控制吃哪些职业词条；`Instance` 静态缓存 |
| **裁剪** | 新建 `HenshinDamage`：`Generic` → `StatInheritanceData.Full`；`Melee`/`Ranged`/`Magic`/`Summon`/`Throwing`（及灾厄盗贼类若可分辨）→ **damage/crit/attackSpeed 乘 k=0.35** 的自定义 `StatInheritanceData`；**不**对盗贼潜行偷袭、真近战专用做效果继承（除非招式标注真近战）。禁止「再乘一遍各职业满额」。每形态再乘 `HenshinDamageFactor`（默认 1.0）。 |

### 3.2 Player DrawLayer / 外观覆盖、隐藏盔甲

| | |
|--|--|
| **参考** | Overlay：`Content/Items/Stones/Marbles/MarbleShield.cs`（`MarbleShieldLayer`）、`Content/Scenarios/SupCal/End/EternalBlazingNow/EbnPlayerLayer.cs`；隐藏本体：`Content/Wraiths/Deaths/WraithSeizureHideOverride.cs`、`Content/LegendWeapon/HalibutLegend/HalibutPlayer.cs`（`HidePlayerTime` + `PreDrawPlayers`）、`Content/LegendWeapon/OnikiriLegend/OniFlashSteps/OniFlashStepHideOverride.cs` |
| **学什么** | `PlayerDrawLayer` + `GetDefaultVisibility` 读 ModPlayer；用 `PreDrawPlayers` 过滤名单隐藏玩家；退出同帧关 visibility |
| **裁剪** | `HenshinOverlayLayer`：居中绘制宝可梦贴图（待机/移动/跳跃/招式帧）；`HenshinPlayer.IsTransformed` 为 false 时 **visibility=false**（禁残留）。隐藏身体/盔甲：优先 `PlayerDrawLayer` 对身体层返回不可见，或短时 `PreDrawPlayers` 过滤 + 只画 Overlay；**第一版不吃盔甲染料**。隐身遵循原版。不引入 InnoVault 的 `PlayerOverride`，用 tML 原生钩子。 |

### 3.3 Buff / ModPlayer 状态机与清理

| | |
|--|--|
| **参考** | `Content/Wraiths/Runtime/WraithPlayer.cs`（标志位、Reset/清理、脏标记同步）；各类 Grab `*Player` 的进出清理 |
| **学什么** | 状态集中在 ModPlayer；退出路径明确；只清本模授予效果 |
| **裁剪** | `HenshinPlayer` 为唯一变身权威状态机（见 §4.2）。被动效果优先 **ModPlayer 标志**（`lavaImmune`、`gills` 等）而非永久 Buff；若用 Buff，须带本模专用 ID 列表，退出时只 `DelBuff` 这些。切换形态：先 `ExitForm()` 再 `EnterForm(newId)`。 |

### 3.4 联机同步（ModPacket）

| | |
|--|--|
| **参考** | `CWRNetWork.cs`、`CWRNetChannel.cs`、`Content/Wraiths/Runtime/WraithNet.cs`（`WraithNetOp`、服务端校验、StateSync、限流） |
| **学什么** | 操作码枚举；服务端权威；客户端请求→服务端校验→广播；防过时 revision |
| **裁剪** | 小型 `HenshinNet`：显式 `NetOp`；`SyncForm` 载荷为 `playerId + formNetId(+0=无)`；进化用 Request/Apply 双包；天气场与穿障由服务端改状态再广播。可不做大修级 pending 队列，但要有基础校验（whoAmI、是否持握之力）。 |

### 3.5 坐骑限制 / 强制下马

| | |
|--|--|
| **参考** | `Content/NPCs/Modifys/Crabulons/CrabulonPlayer.cs`（`mount.Dismount`）；`Content/NPCs/BrutalNPCs/**/**Grab*Player.cs`；`Content/TimeFreezes/WorldFreezePlayer.cs`（`controlMount = false`）；`Content/Wraiths/Deaths/WraithRevivalDeathPlayer.cs` |
| **学什么** | 每帧 `controlMount=false` + 已骑则 `Dismount`；本地玩家优先处理 |
| **裁剪** | `HenshinPlayer` 在 `IsHoldingForce` 时：`SetControls`/`PostUpdateEquips` 禁坐骑并下马；物品 tooltip 写明。取消持握后不拦。 |

### 3.6 天气 / 场域类效果

| | |
|--|--|
| **参考** | `Content/Wraiths/Abilities/GhostRains/GhostRainStorm.cs`（半径/时长常量）、`GhostRainProj.cs`（权威载体）、`GhostRainAmbience.cs`（本地演出与光照） |
| **学什么** | **伤害相关状态服务端权威**；客户端只做氛围；场有中心、半径、寿命 |
| **裁剪** | `WeatherFieldSystem`：服务端列表 `{center, radius, duration, tag∈{Rain,Thunder,Snow,Sand}, owner}`；默认半径 24 格、15s、同类 CD 30s。`SyncWeatherField` 同步。情境条件 `WeatherRain` 等**只读本模场 + 原版天气**，不改写世界永久降雨。全球天气仅档≥9 且招式标注。不复刻鬼雨滤镜强度，可用轻量粒子/着色。 |

### 3.7 地形编辑 / 挖砖限制

| | |
|--|--|
| **参考** | `Player.noBuilding`：`WraithRevivalDeathPlayer`、`KikasaDreamPlayer` 等；改砖同步：`Content/TileProcessors/RecoverUnknowTP.cs`（`WorldGen` + `NetMessage.SendTileSquare`）；大量 `WorldGen.KillTile` 用例 |
| **学什么** | 禁建造用 `noBuilding`；改世界后必须 `SendTileSquare`；服务端执行 |
| **裁剪** | `TerrainBudgetPlayer`：按 `ProgressStage` 实施需求 §7.2 半径/频率/可逆性；黑名单（箱、祭坛、生命水晶、床、TileEntity 等）；超预算发包 `TerrainBudgetReject` 并提示。穿障期间 `noBuilding` + 禁拾取/开箱。临时砖用本模 Tile + 倒计时还原（无大修完美对应，建议方案见 §4.7）。 |

### 3.8 物品进化 / 替换、前缀保留

| | |
|--|--|
| **参考** | 前缀搬运：`Content/Industrials/MaterialFlow/ItemPipelines/ItemPipelineTP.cs`（`new Item(...){ prefix = ... }`）；授予同步：`Content/Items/Melee/Arbiters/ArbiterManifestationNet.cs`（`GrantedItem` + `Prefix` + `SyncEquipment`）；槽位快照：`Content/Industrials/ChestNetSync.cs`（type/stack/prefix） |
| **学什么** | 换类型前保存 `prefix`/`favorited`/`stack`；服务端校验后改物品；联机 `SyncEquipment` |
| **裁剪** | `EvolutionService.TryReplace(Item, newType)`：读 `prefix`+`favorited` → `SetDefaults(newType)` → 写回；范围仅背包/热键栏/鼠标（需求 §4.3）。银行等需手动。确认 UI 后再 Apply。 |

### 3.9 进度（DownedBoss）查询适配

| | |
|--|--|
| **参考** | `CWRRef.cs`：`DownedBossSystem` 属性绑定与 `GetDownedBoomerDuke` 等（老公爵 = `downedBoomerDuke`） |
| **学什么** | 集中适配、字段名与灾厄 API 对齐、失败安全返回 false |
| **裁剪** | `ProgressStageService.GetProgressStage()` = 满足的最大档 1～12（需求 §4.1 表）。本模强依赖可直接读 `DownedBossSystem`；仍封装适配器便于版本升级。原版条件用 `NPC.downed*` / `Main.hardMode`。 |

### 3.10 数据驱动定义表 / 注册表

| | |
|--|--|
| **参考** | `Content/Wraiths/Core/WraithDefinition.cs`、`WraithRegistry.cs`、`WraithRoster.cs` |
| **学什么** | 定义抽象 + 注册表按 Key/网络 ID 查询；显示名可本地化 |
| **裁剪** | `FormDefinition`（FormId、显示名键、属性、Stage、进化自、招式 A/B 元数据、碰撞级别、穿障标志、贴图路径模板、`HenshinDamageFactor`）。`FormRegistry` 启动注册 36 形态。显示名/贴图路径可配置覆盖（需求 §12.7）。 |

### 3.11 项目结构与依赖写法

| | |
|--|--|
| **参考** | `build.txt`（`modReferences` / `weakReferences` / `sortAfter`）、`CWRMod.cs`（FindMod、HandlePacket、单例） |
| **学什么** | 入口瘦、功能按 Content 分区、跨模组探测集中 |
| **裁剪** | 见 §2；**不**依赖 InnoVault；Calamity 用强引用 + 薄适配。 |

### 3.12 无直接参考时的建议

| 需求点 | 结论 |
|--------|------|
| 持握热键栏选中 = 变身 | 无完美对应；建议每 tick 读 `player.inventory[player.selectedItem]`，排除 `mouseItem` 与非热键栏 |
| k=0.35 部分继承 | 大修只有 Full/None；**已采用主方案**：`new StatInheritanceData(0.35f, 0.35f, 0.35f, 0.35f, 0.35f)`（damage / crit / attackSpeed / armorPen / knockback 五维，tML 2026.07 可编译）。回退方案（钩子补乘）不再需要 |
| 进化确认 UI | 无对应；建议简易 `UIState` 确认框 + 仅服务端 Apply |
| 穿障卡墙安全传送 | 无完美对应；建议结束时 `Collision.SolidCollision` 检测，螺旋搜最近空位，失败则短定身 |
| 地形砖/分预算与临时还原 | 无预算系统；建议 `TerrainBudgetPlayer` 计数器 + 临时 Tile 倒计时列表 |
| 开局发三件御三家 | 近似 `ArbiterManifestationNet` 发物；建议 `ModPlayer.OnEnterWorld` + 世界/玩家 flag 防重复 |

---

## 4. 技术设计要点（接口级）

### 4.1 HenshinDamage + k=0.35

```text
class HenshinDamage : DamageClass
  GetModifierInheritance(dc):
    if dc == Generic → Full
    if dc in {Melee, Ranged, Magic, Summon, Throwing, Rogue?} →
      new StatInheritanceData(0.35f, 0.35f, 0.35f, 0.35f, 0.35f)  // 按 tML 字段对齐
    else → None
  GetEffectInheritance(dc):
    Generic 相关 true；职业特效（潜行偷袭、真近战）默认 false
```

- 物品/弹幕：`Item.DamageType = ModContent.GetInstance<HenshinDamage>()`
- 最终输出再乘 `FormDefinition.HenshinDamageFactor`
- **挂点：** 优先 DamageClass 继承 + 形态系数；若改用钩子回退方案，禁止与 Full 继承叠加（防双算）。
- **Rage/Adrenaline：** M0 未对照（**pending，移入 M1.1 一并做**）；未吃到则在适配层补乘（见 §2.3）。

**验收：** 只穿近战装 vs 只穿远程装，本模武器增幅约为同阶段近战/远程武器的约 35% 职业部分 + 100% Generic；混搭不超过需求 130% 抽检上限。

### 4.2 HenshinPlayer 状态机

```text
枚举 FormState { None, Active }

每 tick（建议 PostUpdate）：
  desired = ResolveHeldForm(player)  // 存活 && 热键栏选中之力
  if desired != current:
    ExitForm()                       // 清 Overlay、被动、招式 CD 状态、穿障、本模 Buff
    if desired != None: EnterForm(desired)
  if Active:
    EnforceNoMount()
    ApplyAffinityPassives()
    ApplyAccessoryGates()            // 仅变身时

死亡 / 强制解除 → ExitForm()
```

- **权威：** 服务端判定 `desired`，`SyncForm` 广播；客户端可预测，冲突以服务端为准。
- **禁坐骑：** `controlMount=false` + `mount.Active` 则 `Dismount`。
- **清理契约：** `ExitForm` 只碰本模字段与本模 Buff ID 白名单。

### 4.3 Overlay 绘制

- `HenshinOverlayLayer : PlayerDrawLayer`，位置建议 `AfterParent(PlayerDrawLayers.LastVanillaLayer)` 或 Wings 之后。
- 读 `HenshinPlayer.FormId` + 动画状态（Idle/Move/Jump/MoveA/MoveB）。
- 隐身：`drawInfo.shadow` / 原版 invis 规则；其他玩家可见相同 FormId（靠 SyncForm）。
- 退出同 tick：`FormId=0` → Layer 不可见。

### 4.4 FormDefinition 数据驱动

```text
FormDefinition {
  string FormId;              // "L01_F01"
  int NetworkId;              // 稳定 ushort，联机用
  string DisplayNameKey;
  string TexturePath;         // 可配置覆盖
  PokemonType Primary, Secondary?;
  int Stage;
  string EvolvesFrom?;        // FormId
  MoveSpec MoveA, MoveB;
  CollisionTier Collision;    // A/B/C
  bool GrantsPhasing;
  float HenshinDamageFactor;
  string Role;                // 战斗/功能/传说
}
```

招式 `MoveSpec` 必填：键位冲突等级、联机风险、碰撞级别、是否授予穿障（需求 §2.5）。

### 4.5 进化替换契约

```text
bool CanAutoEvolveLocation(slot) → 主背包+热键栏（inventory 0..49）+ mouseItem；**排除**钱币/弹药/垃圾桶/银行/保险箱/世界箱/地上/商店
bool MeetsTrigger(player, form) → ProgressStage 或指定 Boss（服务端）
RequestEvolve(player, slot) → 校验 → 打开确认 UI
ApplyEvolve(player, slot) →
  save prefix, favorited, stack
  SetDefaults(nextType)
  restore prefix, favorited
  SyncEquipment / SyncForm if held
失败 → 物品不变
```

### 4.6 ProgressStage 适配器

```text
int GetProgressStage() → max stage in 1..12 whose AnyCondition true
Stage 条件严格按需求 §4.1（含 downedBoomerDuke）
形态可用：GetProgressStage() >= form.Stage
```

### 4.7 天气场、穿障、地形预算

**天气场（参考 GhostRain* 裁剪）：**

- 服务端 `List&lt;WeatherField&gt;`；招式只 `WeatherFieldSystem.TrySpawn(...)`
- 伤害/潮湿判定读场；本地可画雨丝

**穿障（需求 §2.4 C）：**

- `HenshinPlayer.PhasingTimer/Cooldown`；单次 ≤2.0s（饰品可加，封顶 2.0）；CD≥8.0s
- Boss 交战（造成或受到）后 3s 内不可开
- 期间：`noBuilding`、禁拾取/开箱；`SyncPhasing`
- 结束卡墙：搜安全格传送，否则短定身，不处死
- 第一版仅 L06_F01（鬼斯通）带穿障

**地形预算（参考 noBuilding + SendTileSquare，预算自研）：**

```text
TryEditTile(player, action) →
  if phasing → reject
  if blacklisted → reject
  if over radius/rate budget → TerrainBudgetReject
  else server apply + sync
档位限额按需求 §7.2
```

---

## 5. 按里程碑拆任务

粒度：半天～2 天。每项含模块/类名、验收、风险。

### M0 — 小火龙竖切（已完成，2026-09-04）

#### M0.1 工程脚手架

- [x] 创建 tML 项目、`build.txt`、`PokemonHenshinMod.cs`、Localization、`.csproj`（扁平布局，见 §2.1）
- [x] 命名空间与 Content 文件夹按 §2.1 建好；`ModSources` 目录联接
- **验收：** 带灾厄可进游戏，模组列表可见 —— 通过
- **风险：** 灾厄/tML 版本不匹配 → 已钉死并写入 README / AGENTS.md

#### M0.2 FormDefinition + 注册表 + 小火龙物品

- [x] `FormDefinition`（含 `MoveA/MoveB`）、`FormRegistry`（FormId / NetworkId / ItemType 三向索引）
- [x] `Items/Forms/CharmanderForce`（`L01_F01`，NetworkId 1），显示名双语可本地化
- [x] 精灵图：52poke 小火龙页 HGSS 精灵 `Spr_4h_004.png`（原文件为 APNG，取首帧、裁透明边为 38×46）→ `Assets/Forms/L01_F01.png`；`icon.png` 同源
- **验收：** 创造栏可取、持握为武器、Overlay 显示该图 —— 通过

#### M0.3 HenshinDamage

- [x] `Damage/HenshinDamage.cs`（k=0.35 五维 `StatInheritanceData`；盗贼类 `TryFind("CalamityMod","RogueDamageClass")`）
- [x] 物品与两个弹幕 `DamageType` 挂上；`HenshinDamageFactor` 只在 `HenshinForceItem.ModifyWeaponDamage` 乘一次
- **验收：** 木桩可见「变身伤害」 —— 通过；**Rage / 肾上腺素是否计入未对照（pending，见 §4.1）**

#### M0.4 HenshinPlayer 状态机 + 禁坐骑

- [x] `PlayerState/HenshinPlayer.cs`：`PreUpdate` 判定 → 先 Exit 再 Enter；`Kill` / `UpdateDead` 兜底
- [x] 禁坐骑：`SetControls` + `PreUpdateMovement` + `PostUpdateEquips` 三挂点；tooltip 含提示
- **验收：** 持握变身、切换/死亡立刻解除、坐骑不可用且强制下马 —— 通过

#### M0.5 Overlay

- [x] `Visual/HenshinOverlayLayer.cs`（`AfterLastVanillaLayer`，脚底锚点，按朝向翻转）+ `HenshinPlayer.HideDrawLayers` 隐藏除本层外的全部层
- [x] 退出无残留
- **验收：** 本地可见贴图、取消持握立刻消失 —— 通过

#### M0.6 双招式占位（爪击 / 火花弹）

- [x] `Combat/Moves/ScratchSlashProj`（空贴图 `Projectile_0` + Smoke/Torch 尘）、`EmberBoltProj`（复用原版 `BallofFire` 贴图 + Torch 尘 + 着火）
- [x] 左键 A / 右键 B（`AltFunctionUse`；B 的键位冲突等级 `RightClick`）
- [x] **FX：** 仓库无任何新增图片
- **验收：** 两种手段都造成 HenshinDamage —— 通过

#### M0.7 SyncForm 联机

- [x] `Net/HenshinNet.cs`：`NetOp.SyncForm`（playerId + formNetId）
- [x] 服务端按自身视角校验后转发；不一致回发纠正；`SyncPlayer` 入场同步
- **验收：** 主机 ↔ 客户端互见形态 —— **双端实测 pending**（隔离服务端加载与世界生成已通过）

**M0 总验收：** 需求 §13 M0 —— 单机部分通过；联机互见待双端实测（§7.2 N1～N4）。

#### M0 实测踩坑（后续里程碑必读）

| 现象 | 根因（已反编译 tML 2026.07 确认） | 现役约定 |
|------|------|------|
| 左右键失效 | `ModType.NewInstance` 默认用 `Activator.CreateInstance` 建每个物品的 ModItem 实例，**不复制模板实例字段**；`SetStaticDefaults` 里赋给模板的实例字段在真实物品上为 null | 每形态共享数据一律放 `FormDefinition` 由 `FormRegistry` 持有；`HenshinForceItem.Definition` 按 `Type` 查注册表 |
| 出现第二只镜像宝可梦 | **WeaponDisplay** 在 `ModPlayer.ModifyDrawInfo` 直接把手持物品贴图塞进 `DrawDataCache`，绕过层系统（原版 `HeldItem` 与 WeaponOut 走层，能被 `HideDrawLayers` 隐藏） | `HenshinOverlayLayer.Draw` 先移除 `DrawDataCache` 中引用形态贴图 / 物品贴图的条目再画自己 |
| `Hide()` 对 `HeldItem` / `FrontAccFront` 的注意点 | 这两个 `Multiple` 层在 `DrawOrder` 里被包成 `PlayerDrawLayerSlot`，原层挂为 slot 的子层；对 `Layers` 中原层 `Hide()` 有效（子层不可见即不画） | 隐藏原皮遍历 `PlayerDrawLayerLoader.Layers` 即可，无需碰 `DrawOrder` |
| 绘制坐标 | 原版各层直接用 `drawInfo.Position`，不再加 `gfxOffY` | Overlay 亦不加 `gfxOffY` |

---

### M1 — 进度、进化、御三家

#### M1.1 ProgressStage 适配器

- [ ] `Core/ProgressStageService.cs` + `CalamityProgressAdapter.cs`
- [ ] 映射需求 §4.1 全部档（含 `downedBoomerDuke`）
- **验收：** 调试命令或 UI 显示当前档；击杀测试 Boss 后档位上升
- **风险：** 灾厄字段改名 → 单测/日志打印缺失字段

#### M1.2 进化替换契约

- [ ] `Evolution/EvolutionService.cs`
- [ ] 前缀 + 收藏继承；范围过滤
- **验收：** 热键栏进化保留前缀；银行中同物品不自动变
- **风险：** 收藏字段联机不同步 → 进化后 `SyncEquipment`

#### M1.3 进化确认 UI

- [ ] `Evolution/EvolutionConfirmUI.cs`
- [ ] `RequestEvolve` / `ApplyEvolve` 包
- **验收：** 条件满足→确认→替换；取消不变；作弊包被拒
- **风险：** UI 焦点抢输入 → 确认期间可暂停玩家用招

#### M1.4 二阶演示链

- [ ] `L01_F02` 火恐龙物品 + 进化关系（触发可用「档≥4」或调试）
- **验收：** 小火龙→火恐龙流程跑通
- **风险：** 无

#### M1.5 开局御三家

- [ ] `PlayerState/StarterGrantPlayer.cs`：小火龙/杰尼龟/妙蛙种子各 1
- [ ] 每玩家每世界一次 flag
- **验收：** 新玩家进世界背包有三件；重进不重复发放
- **风险：** 背包满 → 掉落世界或提示；需写清策略（建议优先空槽，满则掉落）

**M1 总验收：** 需求 §13 M1。

---

### M2 — 情境被动 + 饰品

#### M2.1 情境条件引擎

- [ ] `Affinity/ConditionId` 枚举（需求 §3.1）
- [ ] `Affinity/ConditionEvaluator.cs`（服务端）
- **验收：** 调试下 BiomeHell / WeatherRain / TargetOnFire 等抽测正确
- **风险：** 生物群落与灾厄 Zone 差异 → 火系地狱用原版 Layer + 可选硫磺火 Zone

#### M2.2 火 / 水 / 飞 被动子集

- [ ] `Affinity/TypePassives/FirePassive`、`WaterPassive`、`FlyingPassive`
- [ ] 挂到对应 FormDefinition
- **验收：** 持握生效、取消同 tick 消失；火：熔岩免疫；水：呼吸+水中移速；飞：能量飞行占位
- **风险：** 飞行与坐骑/翅膀冲突 → 变身已禁坐骑；翅膀可保留但飞行能量独立计量

#### M2.3 饰品框架 + A01、A07（或 A01+A02）

- [ ] `Accessories/HenshinAccessoryItem` 基类：未持握时效果关 + 描述 `【仅变身生效·当前未生效】`
- [ ] 实现至少 2 件（建议 A01 万能胶囊带 + A07 木炭袋）
- **验收：** 未持握无加成；持握后生效；脱下立刻无
- **风险：** 灾厄额外饰品栏 → 用标准 `UpdateAccessory`，不特殊 hook 也能进栏即可

**M2 总验收：** 需求 §13 M2。

---

### M3 — 天气场、穿障、挖掘

#### M3.1 天气场一例

- [ ] `WeatherField/*` + 水箭龟或占位招式「雨天气场」（可用临时测试招式挂小火龙 B 调试，合并前改回）
- [ ] `SyncWeatherField`；`WeatherRain` 条件读场
- **验收：** 场内潮湿/加成一致；过期消失；联机双方判定一致
- **风险：** 与灾厄酸雨并存 → 本模场不取消灾厄事件

#### M3.2 穿障一例（鬼斯通 L06_F01）

- [ ] 物品可创造获取；招式 B 穿障 1.5s
- [ ] 硬限制全实现 + `SyncPhasing` + 卡墙处理
- **验收：** 超时/CD/Boss 3s 窗/禁交互均符合；联机可见穿障状态
- **风险：** 穿墙物理不同步 → 服务端设标志，客户端表现跟随

#### M3.3 挖掘一例（地鼠 L10_F01）

- [ ] `TerrainEdit/TerrainBudgetPlayer` + 黑名单
- [ ] 招式挖掘遵守档≤6 预算
- **验收：** 超预算拒绝提示；不挖箱/祭坛；联机地形一致
- **风险：** 挖矿与原版 pickaxe 冲突 → 招式走自定义 KillTile 路径并计入预算

**M3 总验收：** 需求 §13 M3。

---

### M4 — 内容填满与平衡

#### M4.1 形态物品与招式骨架

- [ ] 按需求 §9 建齐 36 形态物品（可用代码生成器/表驱动减少样板）
- [ ] 招式按 §9.3 骨架实现；数值先占位再迭代
- [ ] **传说线优先验收：** 超梦（L14）、洛奇亚（L16）、烈空坐（L17）须在 M4 内可玩且强度落在对应档对标区间（对齐需求 §14 第 7 条）
- **验收：** 创造菜单可取齐；每档 1～3 只互竞可感知差异；三传说可创造获取并抽检 DPS
- **风险：** 工作量爆炸 → 按档分批（先 1～4，再 5～8，再 9～12；传说放入最后一批但不得删）

#### M4.2 饰品 12 件

- [ ] A01～A12 按需求 §10
- **验收：** 共鸣条件正确；未变身全无效
- **风险：** 无

#### M4.3 获取途径

- [ ] 掉落/合成/商店等（可并存）；同档难度同阶
- **验收：** 非创造可玩到各档至少 1 条获取链
- **风险：** 掉率需实机调

#### M4.4 DPS 抽检

- [ ] 配置对标 ItemID 表（需求 §11）
- [ ] 每档 80%～120% 记录
- **验收：** 文档化抽检表；极端混搭 ≤130%
- **风险：** 灾厄改数值 → 对标用配置文件

**M4 总验收：** 需求 §13 M4。

---

### M5 — 发布候选

#### M5.1 负面用例

- [ ] 死亡、快速切物品、进化当帧切形态、穿障结束卡墙、联机延迟进化、背包满发御三家
- **验收：** 需求 §14 清单勾完

#### M5.2 本地化与资源替换

- [ ] zh-Hans / en-US；显示名与贴图路径覆盖配置
- **验收：** 改配置可不重编译换皮（或热重载文档说明）

#### M5.3 打包与版本钉死

- [ ] `build.txt` version、依赖说明、已知问题
- **验收：** 干净档 + 灾厄可通关抽检一档

**M5 总验收：** 发布候选。

---

## 6. 数据与内容管线

| 阶段 | 形态表 | 招式 | 饰品 | 宝可梦图 | 特效 |
|------|--------|------|------|----------|------|
| M0 | 1 条 L01_F01 | A/B 占位 | 无 | 52poke 小火龙 | 仅原版/灾厄/大修复用 |
| M1 | +L01_F02；御三家三件 | 简占位 | 无 | 补杰尼龟/妙蛙种子/火恐龙 | 同上 |
| M2 | 仍少；火/水/飞被动 | 不变 | 2 件 | 按需 | 同上 |
| M3 | +穿障/挖掘/天气演示形态 | 真逻辑 | 可加 A11 | 按需 | 天气/影参考大修场与 Dust，无新图 |
| M4 | **36 形态满表** | 骨架→数值 | **12 件满** | **按 §9 从 52poke 补齐** | 逐招式映射已有 FX，禁止新图 |
| M5 | 冻结 ID | 微调 | 微调 | 路径可替换验收 | 回归：仓库无新增 Fx 图片 |

**仍待实现期填写（不挡 M0）：** 招式精确数值、对标 ItemID、饰品微调、商标策略（需求 §15）。

### 特效实现备忘（给实现 Agent）

写招式前先定「参考源」再编码，例如：

| 需求观感 | 优先参考 |
|----------|----------|
| 火花/火焰锥 | 原版火尘、火焰弹；大修火焰类 Dust 组合 |
| 水弹/雨场 | 原版水尘、雨；大修 `GhostRain*` 的场半径/寿命结构（换水尘，不搬鬼雨资产依赖） |
| 电光/落雷 | 原版雷电尘、暗影束类 |
| 影球/穿障残影 | 原版暗影尘、隐身尘；大修影系演出逻辑 |
| 挖掘碎屑 | 原版挖矿尘 / KillTile 粒子 |
| 龙息/终局大招 | 灾厄或大修大型弹幕的**参数与层级**，贴图仍用已有投射物/尘 |

---

## 7. 联机测试计划

### 7.1 环境

- 主机 + 至少 1 客户端；可选：主机为 Listen Server
- 双方模组版本哈希一致（tML 强制）

### 7.2 用例表

| ID | 步骤 | 期望 |
|----|------|------|
| N1 | 客户端持握小火龙 | 主机看到 Overlay/形态 |
| N2 | 主机切换取消持握 | 客户端同 tick 内形态消失 |
| N3 | 客户端骑乘中持握之力 | 强制下马；主机一致 |
| N4 | 客户端造成招式伤害 | 主机仇敌掉血一致（允许极短延迟） |
| N5 | 进化 Request→确认 | 仅申请人物品变；前缀保留；双方物品栏一致 |
| N6 | 银行内同种之力 | 不自动进化 |
| N7 | 天气场内另一玩家 | 情境/潮湿判定双方一致 |
| N8 | 穿障中 | 双方见状态；结束卡墙安全处理不desync |
| N9 | 挖掘超预算 | 发起方收到 Reject；世界砖未改 |
| N10 | 高延迟下快速切物品 | 最终形态与热键栏选中一致（服务端权威） |

### 7.3 回归节奏

- 每完成 M0/M1/M3 网同步相关任务跑 N1～N4
- M1 加 N5～N6；M3 加 N7～N9；M5 全跑 + N10

---

## 8. 明确不做 / 延后（执行清单）

1. 禁止纳入当前版本 PR：图鉴养成、敌人属性克制、PvP、永久穿墙、依赖 InnoVault、修改 CalamityOverhaul、>2 穿障形态、未变身强力饰品、**新增 FX 图片文件**
2. 延后：冷门属性全集、全球天气、对标 ID 钉死、商标最终文案、精灵图精修

---

## 9. 建议实现顺序（依赖图）

```text
[工程脚手架 M0.1]
        ↓
[FormDefinition/Registry + Charmander Item M0.2]
        ↓
[HenshinDamage M0.3] ──────────────┐
        ↓                          │
[HenshinPlayer 状态机+禁坐骑 M0.4] ←┤
        ↓                          │
[Overlay M0.5] ←── 依赖 Player 标志 │
        ↓                          │
[招式占位 M0.6] ←── Damage + Player ┘
        ↓
[SyncForm M0.7] ──→  ★ M0 完成
        ↓
[ProgressAdapter M1.1] → [EvolutionService M1.2] → [Evolve UI+Net M1.3] → [L01_F02 M1.4]
        ↓                                                         ↓
[StarterGrant M1.5] ←─────────────────────────────────────────────┘
        ↓
 ★ M1 完成
        ↓
[ConditionEvaluator M2.1] → [火/水/飞被动 M2.2] → [饰品框架+2件 M2.3]
        ↓
 ★ M2 完成
        ↓
        ├→ [WeatherField M3.1]
        ├→ [Phasing M3.2] （可并行，都依赖 Player+Net）
        └→ [TerrainBudget M3.3]
        ↓
 ★ M3 完成
        ↓
[批量形态/招式 M4.1] → [饰品满 M4.2] → [获取 M4.3] → [DPS 抽检 M4.4]
        ↓
 ★ M4 完成
        ↓
[负面用例 M5.1] → [本地化/换皮路径 M5.2] → [打包 M5.3]
        ↓
 ★ M5 发布候选
```

**并行建议：** Overlay 与 Damage 可在状态机骨架后并行；M3 三例可三人并行，但共享 `HenshinNet` 操作码表需先锁枚举。

---

## 10. Agent 开工检查清单（M0 第一天；已执行完毕，保留作后续里程碑开工范式）

1. 通读 `docs/requirements.md` **v1.2**（尤其 §1.4～1.5、§2、§8、§12、§13）
2. 只读浏览大修：`Content/DamageModify/*`、`WraithNet.cs`、`CrabulonPlayer` 下马、`MarbleShieldLayer`、`GhostRain*`、`CWRRef` Downed 段、`build.txt`
3. **不修改** CalamityOverhaul；**不新增**任何 FX 图片到本仓库
4. 创建 PokemonHenshin 模组工程（本计划 §2）
5. 从小火龙 52poke 图鉴页取精灵图 → `Assets/Forms/L01_F01`
6. 按 M0.1→M0.7 顺序实现；招式 FX 只拼原版/灾厄/大修已有 Dust·弹幕
7. 数值不足用占位，不阻塞竖切

---

## 11. 需求对照审阅结论（可行性）

| 维度 | 结论 |
|------|------|
| 与 requirements **v1.2** 对齐 | **通过**：含禁自制 FX、52poke 取图、持握变身、禁坐骑、伤害折算、进化、御三家、传说线、联机与 M0～M5 |
| 大修借鉴真实性 | **通过**：路径已核对；特效只学实现、不引运行时依赖 |
| 主要残留风险 | ① k=0.35 API；② Overlay 藏皮；③ M4 体量；④ 招式观感受「无新 FX 图」约束，靠组合原版尘弥补 |
| 总评 | **可行，批准按本计划开工 M0** |

---

## 12. 修订记录

| 版本 | 说明 |
|------|------|
| 1.0 | 初版：对齐 requirements v1.1；大修路径级借鉴与 M0～M5 |
| 1.1 | 需求对照审阅修订（进化槽位、k=0.35 回退、传说硬验收） |
| **1.2** | 对齐 requirements v1.2：禁止新增 FX 图；宝可梦图从 52poke 自取；招式 FX 参考表 |
| 1.2.1 | 洁癖收尾：修正验收条文交叉引用；与 AGENTS.md/README 同源 |
| 1.2.2 | M0 完成收尾（2026-09-04）：§2.1 定为扁平布局并写实际目录；§2.2 钉死 tML 2026.07 / Calamity 2.2.4；§3.12 k=0.35 主方案落地；M0 任务勾选、新增「实测踩坑」表；Rage/Adrenaline 对照与联机双端实测标 pending |
