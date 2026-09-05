# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：热键栏持握「xxx之力」变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手同 tick 清除本模效果。持握禁坐骑。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/requirements.md](docs/requirements.md) | 需求规格（权威，**v1.3**） |
| [docs/move-effects.md](docs/move-effects.md) | 招式 / 被动 / 大招泰拉适配表 |
| [docs/dev-plan.md](docs/dev-plan.md) | 开发计划（冲突以需求为准） |
| [AGENTS.md](AGENTS.md) | AI Agent 项目入口 |

## 状态

**现役代码（2026-09-05 / `532cd2b`）：** 36 形态 + **A01～A21**；被动+技能1/2+能量大招（默认 Mouse3）；能量 UI（Tooltip / 物品底栏 / 右下角条）；全形态招式/大招特效已接线（见 `docs/move-effects.md`）；进化确认为 UIState，**进度档提升（Boss）时弹出**（`/henshin evolve` 可手动）。

**待游戏内验收：** Build + Reload 后验手感/特效/联机；调试 `/henshin stage`、`/henshin evolve`。  
**注意：** 御三家二阶进度看 **史莱姆神 / 鹿角怪**，不是史莱姆王。

后置：联机双端实测、DPS 对标、Rage/肾上腺素、M5 发布打磨。

## 构建

| 项 | 值 |
|----|----|
| tModLoader | 1.4.4.9 / **2026.07 stable**（net8.0） |
| CalamityMod | **2.2.4**（强依赖，必须启用） |
| 命令行 | 仓库根 `dotnet build` → `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod` |
| 游戏运行中 | 会 TML003 → 改用游戏内 Develop Mods → Build + Reload |
| 目录联接 | `ModSources\PokemonHenshin` → 本仓库 |

## 资源约定

- 宝可梦 / 饰品外观：52poke；脚本见 `tools/fetch_assets.py`（A13+ 暂复用 A01～A12 贴图）
- 特效：不新增 FX 图片；原版贴图/尘 + 染色 + 自写 AI；可参考灾厄/CalamityOverhaul 写法（不运行时依赖 CWR）
