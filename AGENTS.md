# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身、双招式、情境被动；松手失效。联机必需。

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
| `docs/requirements.md` | **产品唯一真相**（v1.2） |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `Assets/Forms/` · `Assets/Accessories/` | 36 形态 + 12 饰品图（非 FX） |
| `Content/Core/` | FormDefinition / Registry / ProgressStage / 反射适配 |
| `Content/PlayerState/` | HenshinPlayer（权威状态机）+ StarterGrant |
| `Content/Combat/` · `Items/Forms/` | HenshinForceItem + 36 形态（三文件分组） |
| `Content/Accessories/` · `Items/Accessories/` | 仅变身生效饰品 A01～A12 |
| `Content/Affinity/` · `Evolution/` · `WeatherField/` · `TerrainEdit/` · `Loot/` · `Net/` | 被动 / 进化 / 天气 / 挖掘 / 获取 / NetOp |
| `tools/fetch_assets.py` | 从 52poke 拉图（buildIgnore） |
| 特效 | **禁止**新增 FX 图；复用原版/灾厄/大修写法 |
| 本地化 | HJSON 含引号/`\n` 须用 `"..."` 或 `'''...'''` |

## 当前状态与下一步

- **代码：** M0～M4 已落地（2026-09-04）：36 形态、12 饰品、进度/进化/御三家、被动、天气/穿障/挖掘、获取占位。
- **下一步：** 游戏内验收 A1→A4；然后 M5（负面用例、换皮路径、打包）与 DPS 精调。
- 调试：`/henshin stage`、`/henshin evolve`
- pending：联机双端实测、Rage/肾上腺素、DPS 对标精表
- 新形态：继承 `HenshinForceItem`，唯一 `NetworkId`（1～36 已满，新内容从 37 起）；共享数据只放 `FormDefinition`（tML 不复制 ModItem 实例字段）
