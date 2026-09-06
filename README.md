# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：热键栏持握「xxx之力」变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手同 tick 清除本模效果。持握禁坐骑。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/requirements.md](docs/requirements.md) | 需求规格（权威，**v1.3**） |
| [docs/move-effects.md](docs/move-effects.md) | 招式 / 被动 / 大招泰拉适配表 |
| [docs/dev-plan.md](docs/dev-plan.md) | 开发计划（冲突以需求为准） |
| [docs/fx-knowledge.md](docs/fx-knowledge.md) | Stage 6+ FX 目录 / cookbook / 踩坑（Living） |
| [AGENTS.md](AGENTS.md) | AI Agent 项目入口 |
| [`.cursor/skills/henshin-moves`](.cursor/skills/henshin-moves/SKILL.md) | 招式/特效迭代 Skill（预期效果确认门） |
| [`.cursor/rules/tml-api-docs.mdc`](.cursor/rules/tml-api-docs.mdc) | **alwaysApply**：设计须查 [tModLoader API 类表](https://docs.tmodloader.net/docs/stable/annotated.html) |

## 状态

**现役代码（2026-09-06）：** 36 形态 + **A01～A21**；被动+技能1/2+能量大招（默认 Mouse3）；**脚下**能量条 + 满充金尘；全形态招式接线；Stage 6+ FX 抛光（水柱/日棱/破灭/龙怒球体/龙息/挖洞等，见 `docs/fx-knowledge.md`）；进化 UIState（档位提升弹窗，`/henshin evolve` 可补）。

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
- 特效：优先原版 `LoadProjectile`/真弹；CWR 只读抄逻辑，trail 可贴图拷入 `Assets/Fx/`（无运行时依赖）；**禁止擅自降级**（见 `AGENTS.md`）
