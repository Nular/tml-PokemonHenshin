# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：热键栏持握「xxx之力」变身对应宝可梦并使用双招式；松手同 tick 清除本模效果。持握禁坐骑。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/requirements.md](docs/requirements.md) | 需求规格（权威，v1.2） |
| [docs/dev-plan.md](docs/dev-plan.md) | 开发计划 M0～M5 |
| [AGENTS.md](AGENTS.md) | AI Agent 项目入口 |

## 状态

**M0～M4 代码已落地**（2026-09-04）：36 形态 + 12 饰品 + 进度/进化/御三家 + 情境被动 + 天气场/穿障/挖掘 + 获取占位。

**待你游戏内验收**（Build + Reload）：A1 进化/御三家 → A2 被动/饰品门控 → A3 天气/穿障/挖掘 → A4 内容总检。调试：`/henshin stage`、`/henshin evolve`。

后置：M5 发布打磨、联机双端实测、DPS 精调。

## 构建

| 项 | 值 |
|----|----|
| tModLoader | 1.4.4.9 / **2026.07 stable**（net8.0） |
| CalamityMod | **2.2.4**（强依赖，必须启用） |
| 命令行 | 仓库根 `dotnet build` → `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod` |
| 游戏运行中 | 会 TML003 → 改用游戏内 Develop Mods → Build + Reload |
| 目录联接 | `ModSources\PokemonHenshin` → 本仓库 |

## 资源约定

- 宝可梦 / 饰品外观：52poke（图鉴与[道具列表](https://wiki.52poke.com/wiki/道具列表)）；脚本见 `tools/fetch_assets.py`
- 特效：不新增 FX 图片；复用原版 / 灾厄 / 参考 CalamityOverhaul 写法
