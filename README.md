# Pokemon Henshin（宝可梦之力）

泰拉瑞亚 + 灾厄模组：持握「xxx之力」变身对应宝可梦并使用双招式；松手效果消失。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/requirements.md](docs/requirements.md) | 需求规格（权威，v1.2） |
| [docs/dev-plan.md](docs/dev-plan.md) | 开发计划 M0～M5 |
| [AGENTS.md](AGENTS.md) | 给 AI Agent 的项目入口 |

## 状态

**M0（小火龙竖切）代码已完成**：持握变身状态机、Overlay、`HenshinDamage`（k=0.35）、禁坐骑、爪击/火花双招式、`SyncForm` 联机同步。游戏内验收待执行；下一步 **M1**。

## 构建

| 项 | 值 |
|----|----|
| tModLoader | 1.4.4.9 / **2026.07 stable**（net8.0） |
| CalamityMod | **2.2.4**（强依赖，必须启用） |
| 命令行 | 仓库根 `dotnet build` → 自动打包到 `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod` |
| 游戏内 | `ModSources\PokemonHenshin` 为指向本仓库的目录联接，可 Build + Reload |

## 资源约定（摘要）

- 宝可梦外观：可从 [52poke 全国图鉴](https://wiki.52poke.com/wiki/宝可梦列表（按全国图鉴编号）) 自行获取。
- 特效：不新增 FX 图片；复用原版 / 灾厄 / 参考隔壁 CalamityOverhaul 的实现方式。
