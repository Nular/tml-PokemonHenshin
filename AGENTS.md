# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手失效。联机必需。

## 怎么跑

仓库根即 tML 模组根（内部名 `PokemonHenshin`）。

- 命令行：根目录 `dotnet build` → 打包到 `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod`；游戏运行且启用本模时会 **TML003**，只能游戏内 Build + Reload
- 游戏内：`ModSources\PokemonHenshin` 目录联接 → Workshop → Develop Mods → Build + Reload
- 版本钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**（必须启用）

## 技术栈

C# / tModLoader / `modReferences = CalamityMod`。进度用 **反射** `CalamityProgressAdapter`（**无需** Extract dll）。只读参考 `../CalamityOverhaul`（禁止改、禁止运行时依赖）。

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（v1.3） |
| `docs/move-effects.md` | 招式/被动/大招泰拉适配表（已落地行以代码为准） |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `Assets/Forms/` · `Assets/Accessories/` | 36 形态 + 饰品图（非 FX；A13+ 暂复用旧图） |
| `Content/Core/` | FormDefinition / Registry / ProgressStage / Keybinds / 反射适配 |
| `Content/PlayerState/` | HenshinPlayer（能量分存、突进 CD、饰品标志）+ StarterGrant |
| `Content/Combat/` · `Items/Forms/` | HenshinForceItem + 36 形态；`Moves/RedesignedMoveProjs` 点名招式弹 |
| `Content/Visual/` | UltimateEnergyUI（右下角能量条） |
| `Content/Accessories/` · `Items/Accessories/` | 仅变身生效饰品 A01～A21 |
| `Content/Affinity/` · `Evolution/` · `WeatherField/` · `TerrainEdit/` · `Loot/` · `Net/` | 被动 / 进化 / 天气 / 挖掘 / 获取 / NetOp |
| `tools/fetch_assets.py` | 从 52poke 拉图（buildIgnore） |
| 特效 | **禁止**新增 FX 图；点名招式以原版贴图/尘为主（不运行时生成灾厄弹）；CWR 只读参考 |
| 本地化 | HJSON 含引号/`\n` 须用 `"..."` 或 `'''...'''` |

## 当前状态与下一步

- **代码（2026-09-05 / `4a29052`+手感补丁）：** 被动+技能1/2+能量大招；点名招式（御三家一阶/皮卡丘/腕力/波波/凯西）已重做；能量 = Tooltip + 物品底栏 + 右下角条；撞击/电光一闪 **2s CD + 0.25s 无敌**。
- **已知缺口：** 无现役 `GrantsPhasing`；A11 咒符布无消费者；**未点名形态大招**精修 pending。
- **验证：** 御三家二阶需 **史莱姆神/鹿角怪**（非史莱姆王）；`/henshin stage`；大招默认 **Mouse3**；游戏运行中用游戏内 Build + Reload。
- **下一步：** 游戏内验收、联机双端、其余大招、DPS 对标、Rage/肾上腺素。
- 新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`。
