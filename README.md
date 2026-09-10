# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：热键栏持握「xxx之力」变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手同 tick 清除本模效果。持握禁坐骑。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/requirements.md](docs/requirements.md) | 需求规格（权威，页眉版本） |
| [docs/balance-stats.md](docs/balance-stats.md) | 等级/攻防/经验/招式数值真源 |
| [docs/move-effects.md](docs/move-effects.md) | 招式 / 被动 / 大招泰拉适配表 |
| [docs/dev-plan.md](docs/dev-plan.md) | 开发计划（冲突以需求为准） |
| [docs/fx-knowledge.md](docs/fx-knowledge.md) | Stage 6+ FX 目录 / cookbook / 踩坑（Living） |
| [AGENTS.md](AGENTS.md) | AI Agent 项目入口 |
| [`.cursor/skills/henshin-moves`](.cursor/skills/henshin-moves/SKILL.md) | 招式/特效迭代 Skill（预期效果确认门） |
| [`.cursor/rules/tml-api-docs.mdc`](.cursor/rules/tml-api-docs.mdc) | **alwaysApply**：设计须查 [tModLoader API 类表](https://docs.tmodloader.net/docs/stable/annotated.html) |

## 状态

**现役代码（2026-09-10）：** 36 形态 + A01–A28（碎片/普通/超级）；被动+技能1/2+能量大招；**v1.4 已接线**（含龙之波动爆炸碎片命中 `1×Factor`）。开背包右侧**变身属性面板**已接线（需求 §2.9）。游戏内 DPS 抽检、联机双端、饰品图标/合成/掉落、属性面板点开验收仍待本地。

**下一步：** 游戏内验属性面板与饰品 → 联机双端实测 → DPS 对标 PS7/9/12 → Rage。调试 `/henshin stage`、`/henshin evolve`、`/henshin stats`、`/henshin setlevel`。

## 构建

| 项 | 值 |
|----|----|
| tModLoader | 1.4.4.9 / **2026.07 stable**（net8.0） |
| CalamityMod | **2.2.4**（强依赖，必须启用） |
| 命令行 | 源码目录名须为 `PokemonHenshin` 再 `dotnet build`。Cloud：`bash tools/build-mod.sh` |
| 公式校验 | `dotnet run --project tools/HenshinStatVerify` |
| 游戏运行中 | 会 TML003 → 改用游戏内 Develop Mods → Build + Reload |
| 目录联接 | `ModSources\PokemonHenshin` → 本仓库 |

## 资源约定

- 宝可梦 / 饰品外观：52poke；拉取 `tools/fetch_assets.py`，饰品像素化 `tools/pixelize_accessories.py`（64×64：`Axx` / `_Super` / `_Shard`）
- 特效：优先原版 `LoadProjectile`/真弹；CWR 只读抄逻辑，trail 可贴图拷入 `Assets/Fx/`（无运行时依赖）；**禁止擅自降级**（见 `AGENTS.md`）
