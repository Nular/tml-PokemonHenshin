# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：热键栏持握「xxx之力」变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手同 tick 清除本模效果。持握禁坐骑。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/backlog.md](docs/backlog.md) | **状态 / 缺口 / 下一步 / 验收**（唯一） |
| [docs/requirements.md](docs/requirements.md) | 产品规格（权威，页眉版本） |
| [docs/balance-stats.md](docs/balance-stats.md) | 等级/攻防/经验/招式数值真源 |
| [docs/move-effects.md](docs/move-effects.md) | 招式 / 被动 / 大招泰拉适配表 |
| [docs/fx-knowledge.md](docs/fx-knowledge.md) | FX 目录 / cookbook / 踩坑 |
| [docs/engineering.md](docs/engineering.md) | 工程手册（Net、目录、联机用例） |
| [docs/accessories.md](docs/accessories.md) | 饰品锁定决策 / 掉落合成要点 |
| [AGENTS.md](AGENTS.md) | AI Agent 项目入口 |
| [`.cursor/skills/henshin-moves`](.cursor/skills/henshin-moves/SKILL.md) | 招式/特效迭代 Skill |
| [`.cursor/skills/henshin-locomotion`](.cursor/skills/henshin-locomotion/SKILL.md) | 变身移动动画 Skill |
| [`.cursor/rules/tml-api-docs.mdc`](.cursor/rules/tml-api-docs.mdc) | 设计须查 [tModLoader API](https://docs.tmodloader.net/docs/stable/annotated.html) |

`docs/archive/` 仅人类考古；Agent 勿读。

## 状态

见 **[docs/backlog.md](docs/backlog.md)**（对齐需求 **v1.4.26**）。一句话：玩法骨架与数值/饰品/联机瞄准**已接线**；游戏内手感、联机双端、DPS 抽检仍待本地。

## 构建

| 项 | 值 |
|----|-----|
| tModLoader | 1.4.4.9 / **2026.07 stable**（net8.0） |
| CalamityMod | **2.2.4**（强依赖，必须启用） |
| 命令行 | 源码目录名须为 `PokemonHenshin` 再 `dotnet build`。Cloud：`bash tools/build-mod.sh` |
| 公式校验 | `dotnet run --project tools/HenshinStatVerify` |
| 游戏运行中 | TML003 → 游戏内 Develop Mods → Build + Reload |
| 目录联接 | `ModSources\PokemonHenshin` → 本仓库 |

## 资源约定

- 宝可梦 / 饰品外观：52poke；`tools/fetch_assets.py`、`tools/pixelize_accessories.py`；移动 sheet：`tools/extract_locomotion_gif.py`
- 特效：优先原版 → 拷贝 CWR 至 `Assets/Fx/` → 可自制（需求 §1.5）；禁止大修运行时依赖；禁止擅自降级（`AGENTS.md`）
- `Assets/TEMP_ASSETS/`：本地临时素材（gitignore）
