# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品（不变之石、诅咒焰除外）；松手失效。联机必需。

**开工顺序：** 本文件 → `docs/backlog.md`（要做什么）→ 按任务打开下表权威文档。**禁止读取或维护 `docs/archive/`。**

## 怎么跑

仓库根即 tML 模组根（内部名 `PokemonHenshin`）。

- **目录名必须是 `PokemonHenshin`**。本地：`ModSources\PokemonHenshin` 联接后 `dotnet build`，或游戏内 Develop Mods → Build + Reload（游戏运行启用本模时命令行会 **TML003**）。
- **Cloud：** `bash tools/build-mod.sh`（勿在名为 `workspace` 的根直接 `dotnet build`）。准备：`bash tools/cloud-agent-setup.sh`。
- **公式：** `dotnet run --project tools/HenshinStatVerify`
- 钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**（运行时强依赖；编译期反射，无灾厄也能出 `.tmod`）

## 技术栈

C# / tModLoader / `modReferences = CalamityMod`。进度反射 `CalamityProgressAdapter`。只读参考 `../CalamityOverhaul`（禁止改、禁止运行时依赖）。API：[tModLoader stable 类表](https://docs.tmodloader.net/docs/stable/annotated.html)；Rule `.cursor/rules/tml-api-docs.mdc`。

## 真相地图

| 路径 | 角色 |
|------|------|
| `docs/backlog.md` | **状态 / 缺口 / 下一步 / 验收**（唯一） |
| `docs/requirements.md` | **产品规则**（页眉版本；饰品 §6/§10；未接线 §12.1） |
| `docs/balance-stats.md` | 等级/攻防/XP/招式**数字** |
| `Content/Accessories/HenshinAccCatalog.cs` | 饰品**数字** |
| `docs/accessories.md` | 饰品锁定决策 / Delivery / 掉落合成要点 |
| `docs/move-effects.md` | 招式语义 |
| `docs/fx-knowledge.md` | FX cookbook / 联机视觉清单 |
| `docs/engineering.md` | 目录、Net、关键契约、联机用例 |
| `.cursor/skills/henshin-moves/` · `henshin-locomotion/` | 招式 / 移动动画迭代流程 |
| `docs/archive/` | 历史全文 — **Agent 禁止读/改**（Rule `.cursor/rules/archive-do-not-read.mdc`，`globs: docs/archive/**`） |

代码入口：`HenshinStatService` / `FormStatTable`；`HenshinNet`；`HenshinFxDraw`；`Assets/Forms` · `Locomotion` · `Accessories` · `Items` · `Fx`。临时 GIF：`Assets/TEMP_ASSETS/`（gitignore，勿提交）。

## 硬禁止（踩坑）

1. **禁止擅自降级**点名规格；流程见 `henshin-moves`。  
2. 壳弹懒加载：`SafeLoadProjectile`；禁在 `dedServ` / `Main.instance==null` 调 `LoadProjectile`。  
3. Additive 保 Alpha；暗色抬亮。  
4. 连续光束优先 `DrawContinuousBeam`；忌通天 MagicPixel。  
5. 大图用 `ScaleForWorldDiameter`。  
6. Fire / Flashimpact / HitJagged **禁止整图**，用 `Draw*Frame`。  
7. `SpawnAtMouse`：事后把 `Center` 设到主人 `GetMouseWorld`。  
8. CWR：只读抄逻辑；贴图拷 `Assets/Fx`；可自制 FX（优先拷贝）；禁大修 `modReferences`。  
9. 勿硬套自管位移原版 AI；削弱能量须 `MarkCrumb`。  
10. HJSON 以 `{`/`[` 开头的值须双引号。  
11. 联机：禁 `Main.MouseWorld`；禁只写 `OnSpawn` 私有字段（见 fx-knowledge）。  
12. 禁 Truncate 写回 `Level`/`Xp`；硬顶只挡获取。

## 当前指针

见 **`docs/backlog.md`**（需求 **v1.4.26**）。
