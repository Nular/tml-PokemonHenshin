# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品（不变之石、诅咒焰除外）；松手失效。联机必需。

## 怎么跑

仓库根即 tML 模组根（内部名 `PokemonHenshin`）。

- **目录名必须是 `PokemonHenshin`**（tML 用源码目录名当内部名）。本地：`ModSources\PokemonHenshin` 联接本仓库后 `dotnet build`，或游戏内 Workshop → Develop Mods → Build + Reload（游戏运行且启用本模时命令行会 **TML003**）。
- **Cloud：** `bash tools/build-mod.sh`（见下）。**不要**在名为 `workspace` 的根目录直接 `dotnet build`。
- **公式校验：** `dotnet run --project tools/HenshinStatVerify`
- 版本钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**（必须启用）

## Cursor Cloud specific instructions

无头 Linux（Cloud Agent）环境下的构建与验证：

- **环境准备（幂等）：** `bash tools/cloud-agent-setup.sh`。装 net8.0 SDK（`~/.dotnet`，软链到 `/usr/local/bin/dotnet`）、下载并解压 tModLoader `v2026.07.3.0`（`~/tModLoader`），并复刻 `ModSources` 开发布局：在 `~/Documents/My Games/Terraria/tModLoader/ModSources/` 生成 `tModLoader.targets` 并把 `PokemonHenshin` 软链指向本仓库。该脚本已配置为 environment.json 的 `install`。
- **构建：** `bash tools/build-mod.sh`（等价 `dotnet build` 但走 `ModSources/PokemonHenshin` 软链）。**不要**在仓库根直接 `dotnet build`——tML 用**源码目录名**当模组内部名，直接在 `/workspace` 构建会产出错误的 `workspace.tmod`；走软链才得到 `PokemonHenshin.tmod`（输出到 `~/.local/share/Terraria/tModLoader/Mods/`）。
- **CalamityMod 不阻断构建：** 代码只用**反射**访问灾厄，编译期无 `using CalamityMod`，故无灾厄也能编译打包成合法 `.tmod`。灾厄仅在**运行时**为强依赖。
- **无头加载自检：** `dotnet ~/tModLoader/tModLoader.dll -server -nosteam` 会发现并尝试加载 `PokemonHenshin`，因缺 CalamityMod 报 `Missing mod: CalamityMod required by PokemonHenshin`（预期）——证明 `.tmod` 合法且依赖接线正确。
- **无法在云端跑的部分：** 实际进游戏测试需图形 Terraria 客户端 + Steam 创意工坊的 CalamityMod 2.2.4，无头 VM 不具备；游戏内玩法/特效/DPS 验收仍须本地。公式可用 `tools/HenshinStatVerify`。

## 技术栈

C# / tModLoader / `modReferences = CalamityMod`。进度用 **反射** `CalamityProgressAdapter`（**无需** Extract dll）。只读参考 `../CalamityOverhaul`（禁止改、**禁止运行时依赖**）。

**全局 API 规范：** 设计/实现须参考 [tModLoader stable 类表](https://docs.tmodloader.net/docs/stable/annotated.html)，按需点进具体类页；项目 Rule `.cursor/rules/tml-api-docs.mdc`（alwaysApply）。

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（页眉版本；等级/攻防/能量/进化/XP） |
| `docs/balance-stats.md` | **数值数字权威**（等级带、经验、MidAtk/Def、种族 Mod、MoveRefRate） |
| `Content/Core/HenshinStatService.cs` · `FormStatTable.cs` | 上表公式的代码入口（无 Terraria 依赖，供 `tools/HenshinStatVerify`） |
| `docs/move-effects.md` | 招式/被动/大招泰拉适配表 |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `docs/fx-knowledge.md` | FX 目录 / cookbook / 踩坑（Living；特效改动先查这里） |
| `.cursor/skills/henshin-moves/` | 招式迭代 Skill：语义→**数值门**→预期效果确认→实现 |
| `.cursor/rules/tml-api-docs.mdc` | **alwaysApply**：设计须查 tModLoader stable API |
| `Assets/Forms/` · `Assets/Accessories/` · `Assets/Items/` | 36 形态 + 饰品 64×64（`Axx`/`_Super`/`_Shard`；高清 `_src_hires/`；脚本 `tools/pixelize_accessories.py`）；消耗品 `RareCandy.png` |
| `Assets/Fx/` | CWR **拷贝**贴图（无运行时依赖）：SoftGlow / ThunderTrail / Fire(4×4) / Flashimpact(4×2) / HitJagged(1×2) / DiffusionCircle(360) / Cyclone / Fog / LightBeam / LightShot / TearFlame / Extra98 |
| `Content/Combat/Moves/HenshinFxDraw.cs` | Additive 绘制：`DrawContinuousBeam` / SheetFrame / `ScaleForWorldDiameter` |
| `Content/Combat/` · `Items/Forms/` · `Items/Consumables/` | HenshinForceItem + 36 形态；`RareCandy` |
| `Content/PlayerState/` · `Visual/` · `Accessories/` · 其它 | HenshinPlayer / Overlay / 属性面板 / 饰品 / 进化 / 糖果掉落 Net |

## 特效踩坑与禁止降级（必读）

1. **禁止擅自降级：** 用户点名参考效果必须按规格落地；跳过原版 AI、A=0「假 Additive」、纯尘冒充成品等，**未经确认不得当作成品**。招式迭代流程见 `.cursor/skills/henshin-moves`。
2. **懒加载贴图：** 壳弹只画不真生成 → 须 `LoadProjectile` / `ProjectileBorrow`（Bubble 踩坑）。
3. **Additive 保 Alpha；暗色抬亮：** `A=0` 全透明。`#2108ad` 等深色在 Additive 下几乎不可见 → 光晕用抬亮同色相（如 `DragonHaloLit`）。
4. **连续光束：** 优先 `DrawContinuousBeam`（SoftGlow 沿路径拉长 + 密叠）；厚度以格为单位（水炮≈1.25、加农≈2.5、日光束≈2）。间距过大 → 虚线。MagicPixel 可用，但**无封顶通天拉伸**易白屏，须控制 destination/scale。
5. **大图按世界直径缩放：** `DiffusionCircle` 360px 等须 `ScaleForWorldDiameter(tex, diameterPx)`；裸 `scale=1.7` / `width/96` 会画出超大圈。
6. **Sprite sheet：** Fire / Flashimpact / HitJagged **禁止整图绘制**，用 `HenshinFxDraw.Draw*Frame`。
7. **SpawnAtMouse：** `NewProjectile` 坐标是左上角；大 hitbox 须事后 `Center = MouseWorld`；改尺寸先存 Center。
8. **CWR：** 只读抄逻辑；贴图拷入 `Assets/Fx`；`build.txt` **不得** `modReferences` 大修。
9. **勿硬套自管位移原版 AI**（Nebula 等）：壳弹自管飞行，亡时再真生成爆炸碎片。`RetargetAsHenshin` 的原版弹默认按完整命中给能；要削弱须显式 `MarkCrumb`（现役仅龙之波动 620）。
10. **HJSON：** 值以 `{` 或 `[` 开头必须双引号（如 `PassiveAlways: "{0}：{1}"`），否则当对象/数组解析，模组加载失败并被禁用。

## 当前状态与下一步

- **招式/FX（至 2026-09-07）：** 36 形态接线；Wave1～Wave3 / Stage7+ **已验收**。清单 `docs/move-effects.md`，cookbook `docs/fx-knowledge.md`。
- **数值（2026-09-09～10）：** v1.4 已接线。击杀 XP × 世界档；`ExpNeeded` × 物品带。龙之波动爆炸碎片命中 `1×Factor`（须 `MarkCrumb`）；击杀不变。DPS 抽检仍待本地。
- **饰品（2026-09-10）：** 28 家族逻辑已接线；A01–A28 普通/超级/碎片 64×64 pixeloe。广角镜菱形索敌碎片 8 / 成品 16 / 超级 32 格，新锁 60° 半角、锁死后可掉头（需求 §6；`HenshinProjUtil.HomingAI`）。重跑：`python tools/pixelize_accessories.py`。**游戏内图标/合成/掉落/索敌手感验收仍待本地**。
- **2026-09-10～11：** 中文 loc 以 `{` 开头的 StatsUI 行已加引号。属性面板入口下移 64px 避开原版图鉴。御三家改 `AddStartingItems` + `PostUpdateMiscEffects` 入包（角色档 `starterGranted`；禁止 `OnEnterWorld`）；**进世界发放已本地验收**。
- **已知缺口：** 联机双端实测 / DPS 抽检 PS7/9/12 / Rage pending。污泥毒云等未打标 Retarget 弹仍走完整命中能。未接线代码见 `docs/requirements.md` §12.1。饰品图标/合成/掉落与神奇糖果待游戏内验收。
- **验证：** 游戏内 Build + Reload（TML003）；大招默认 Mouse3；`/henshin stats`、`/henshin setlevel`；`tools/HenshinStatVerify`。
- **下一步：** 游戏内验饰品（含新图标）与神奇糖果 → 联机 → DPS 抽检 → Rage。新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`。
