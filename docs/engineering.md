# Pokemon Henshin — 工程手册

| 项 | 内容 |
|----|------|
| 角色 | 目录、Net、关键类型、联机测试、大修借鉴裁剪（**现役**） |
| 产品规则 | `docs/requirements.md`（冲突以需求为准） |
| 状态/缺口 | `docs/backlog.md` |
| 来源 | 自原开发计划抽出；历史全文仅人类可查 `docs/archive/`（**Agent 勿打开**） |

---

## 1. 仓库与构建

仓库根 = tML 模组根；**目录名必须是 `PokemonHenshin`**。Cloud 根名为 `workspace` 时用 `bash tools/build-mod.sh`。`ModSources\PokemonHenshin` 联接本仓库。游戏运行且启用本模时命令行打包会 **TML003** → 改用游戏内 Build + Reload。公式：`dotnet run --project tools/HenshinStatVerify`。

```
PokemonHenshin/
  docs/           # requirements / backlog / engineering / accessories / balance / move / fx；buildIgnore
  docs/archive/   # 历史全文；Agent 禁止读/维护
  tools/          # cloud-agent-setup、build-mod、HenshinStatVerify、fetch/pixelize/extract_locomotion
  Assets/Forms/ · Forms/Locomotion/ · Accessories/ · Items/ · Fx/
  Content/Core · Damage · PlayerState · Visual · Combat · Affinity · Evolution
         Accessories · Items/Forms · Items/Consumables · WeatherField · TerrainEdit · Loot · Net · Prefixes
```

`build.txt`：`modReferences = CalamityMod`；`sortAfter = CalamityMod`。钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**。禁止 `modReferences` 大修 / InnoVault。

HJSON：值以 `{` 或 `[` 开头必须双引号，否则模组加载失败。

`Assets/TEMP_ASSETS/`：本地临时素材（如 locomotion GIF）；**已 gitignore**，勿提交。

---

## 2. Calamity 访问

| 策略 | 现役 |
|------|------|
| 反射 `CalamityProgressAdapter` 读 `DownedBossSystem` | **采用** |
| csproj 编译期引用 Calamity dll | **未采用** |
| 盗贼伤害类 | `ModContent.TryFind("CalamityMod","RogueDamageClass")` |

**Rage / 肾上腺素：** pending 实测（见 backlog）。只读参考 `../CalamityOverhaul`：禁止改、禁止运行时依赖。

---

## 3. 联机（NetOp）

单入口 `PokemonHenshinMod.HandlePacket` + `Content/Net/HenshinNet.cs` 显式 `NetOp` 枚举（不要按类型全名自动编号）。

现役相关：`SyncForm`、`RequestEvolve` / `ApplyEvolve`、`TerrainBudgetReject`、`RequestRareCandy` / `ApplyForceProgress`、`SyncAim`（主人鼠标；读 `HenshinPlayer.GetMouseWorld`）、能量/进度同步等——**以代码枚举为准**。

射弹 AI/Draw/Colliding **禁止** `Main.MouseWorld`；贴图/方向/锚点 **禁止**只写在 `OnSpawn` 私有字段（旁观端用 `Ensure*` 重建）。清单：`docs/fx-knowledge.md`「联机视觉/指向」。

---

## 4. 关键类型契约（摘要）

### 4.1 `HenshinDamage`（k=0.35）

`Generic` → Full；近战/远程/魔法/召唤/投掷（及可分辨的 Rogue）→ `StatInheritanceData(0.35,…)`；职业特效默认不继承。再乘 `FormDefinition.HenshinDamageFactor`。

### 4.2 `HenshinPlayer` 状态机

每 tick 解析热键栏选中之力 → 与当前形态不同则 `ExitForm` 再 `EnterForm`。退出只清本模字段 / 本模 Buff。持握时禁坐骑（`controlMount=false` + `Dismount`）。权威在服务端，`SyncForm` 广播。

### 4.3 Overlay / 地图头像

`HenshinOverlayLayer`：静帧 + bob；可选 `FormLocomotionSpec`（Idle/Run；统一画高 64；地面 Run 随 `|vx|` 变速）。流程：`.cursor/skills/henshin-locomotion`。  
`HenshinMapHeadLayer`：全身图缩进原版头像 RT；`HideDrawLayers` 按 `headOnlyRender` 分上下文。

### 4.4 `FormDefinition` / 注册表

`FormId`、稳定 `NetworkId`、贴图、可选 `Locomotion`、属性、`Stage`、进化链、`MoveSpec`、碰撞级别等。新形态 `NetworkId` 从 37 起；共享数据只放定义表，不散落在物品类。

### 4.5 进化

范围：主背包+热键栏+鼠标；排除银行等。双条件：进度档 + `Level >= BandMin[next]`。保存 prefix/favorited → `SetDefaults` → 写回；确认 UI 后服务端 Apply。不变之石：`EverstoneBlock`（未变身也 Apply）。

### 4.6 天气场 / 穿障 / 地形

产品**不走**天气场与穿障管线（见 requirements §12.1）。地形：`TerrainBudgetPlayer` 按档预算 + 黑名单；超预算 `TerrainBudgetReject`。

### 4.7 御三家发放

角色档 `starterGranted`；建角 `AddStartingItems`；旧档 `PostUpdateMiscEffects` + `GetItem`。**禁止** `OnEnterWorld`。

---

## 5. 大修借鉴（压缩）

| 主题 | 学什么 | 本模裁剪 |
|------|--------|----------|
| DamageClass | 继承乘区 | `HenshinDamage` k=0.35 |
| DrawLayer / 隐藏 | visibility 跟状态 | Overlay + 地图头像层 |
| ModPlayer 清理 | 退出路径明确 | 只清本模 |
| ModPacket | 显式 Op、服务端权威 | `HenshinNet` |
| 禁坐骑 | 每帧下马 | 持握时 |
| 天气场 | 服务端权威场 | 管线保留、无形态调用 |
| 改砖 | `SendTileSquare` | `TerrainBudgetPlayer` |
| 进化换类型 | 保 prefix | `EvolutionService` |
| DownedBoss | 集中适配 | `CalamityProgressAdapter` |
| 定义表 | Registry | `FormDefinition` / `FormRegistry` |

---

## 6. 联机测试用例

| ID | 步骤 | 期望 |
|----|------|------|
| N1 | 客户端持握小火龙 | 主机看到 Overlay/形态 |
| N2 | 主机切换取消持握 | 客户端同 tick 内形态消失 |
| N3 | 客户端骑乘中持握之力 | 强制下马；主机一致 |
| N4 | 客户端造成招式伤害 | 主机仇敌掉血一致（允许极短延迟） |
| N5 | 进化 Request→确认 | 仅申请人物品变；前缀保留；双方物品栏一致 |
| N6 | 银行内同种之力 | 不自动进化 |
| N7 | 天气场内另一玩家 | （管线无形态调用；若启用则情境双方一致） |
| N8 | 穿障中 | （现役无形态穿障；若启用则双方见状态） |
| N9 | 挖掘超预算 | 发起方 Reject；世界砖未改 |
| N10 | 高延迟下快速切物品 | 最终形态与热键栏一致（服务端权威） |
| N11 | 双方变身看地图；虫洞点队友 | 头像为缩小形态图；可传送并耗药 |
| N12 | 客户端持握妙蛙种子甩藤鞭；主机另一向看 | 主机见鞭指向**释放者**鼠标，不跟主机指针 |

回归：改 Net / Overlay / 进化 / 地形后至少跑 N1–N6；瞄准/壳弹改动加 N12；地图头像加 N11。

---

## 7. FX 资源策略（与需求 §1.5 对齐）

**优先：** 原版真弹 / 壳弹复用 → **拷贝** CWR 贴图进 `Assets/Fx/`（无运行时依赖）→ 需要时可自制 FX 图放入 `Assets/Fx/`。  
禁止：`GetMod("CalamityOverhaul")`、擅自降级点名规格。细节与 cookbook：`docs/fx-knowledge.md`；踩坑硬列表：`AGENTS.md`。
