# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手失效。联机必需。

## 怎么跑

仓库根即 tML 模组根（内部名 `PokemonHenshin`）。

- 命令行：根目录 `dotnet build` → 打包到 `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod`；游戏运行且启用本模时会 **TML003**，只能游戏内 Build + Reload
- 游戏内：`ModSources\PokemonHenshin` 目录联接 → Workshop → Develop Mods → Build + Reload
- 版本钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**（必须启用）

## Cursor Cloud specific instructions

无头 Linux（Cloud Agent）环境下的构建与验证：

- **环境准备（幂等）：** `bash tools/cloud-agent-setup.sh`。装 net8.0 SDK（`~/.dotnet`，软链到 `/usr/local/bin/dotnet`）、下载并解压 tModLoader `v2026.07.3.0`（`~/tModLoader`），并复刻 `ModSources` 开发布局：在 `~/Documents/My Games/Terraria/tModLoader/ModSources/` 生成 `tModLoader.targets` 并把 `PokemonHenshin` 软链指向本仓库。该脚本已配置为 environment.json 的 `install`。
- **构建：** `bash tools/build-mod.sh`（等价 `dotnet build` 但走 `ModSources/PokemonHenshin` 软链）。**不要**在仓库根直接 `dotnet build`——tML 用**源码目录名**当模组内部名，直接在 `/workspace` 构建会产出错误的 `workspace.tmod`；走软链才得到 `PokemonHenshin.tmod`（输出到 `~/.local/share/Terraria/tModLoader/Mods/`）。
- **CalamityMod 不阻断构建：** 代码只用**反射**访问灾厄，编译期无 `using CalamityMod`，故无灾厄也能编译打包成合法 `.tmod`。灾厄仅在**运行时**为强依赖。
- **无头加载自检：** `dotnet ~/tModLoader/tModLoader.dll -server -nosteam` 会发现并尝试加载 `PokemonHenshin`，因缺 CalamityMod 报 `Missing mod: CalamityMod required by PokemonHenshin`（预期）——证明 `.tmod` 合法且依赖接线正确。
- **无法在云端跑的部分：** 实际进游戏测试需图形 Terraria 客户端 + Steam 创意工坊的 CalamityMod 2.2.4，无头 VM 不具备；游戏内玩法/特效验收仍须本地。

## 技术栈

C# / tModLoader / `modReferences = CalamityMod`。进度用 **反射** `CalamityProgressAdapter`（**无需** Extract dll）。只读参考 `../CalamityOverhaul`（禁止改、**禁止运行时依赖**）。

**全局 API 规范：** 设计/实现须参考 [tModLoader stable 类表](https://docs.tmodloader.net/docs/stable/annotated.html)，按需点进具体类页；项目 Rule `.cursor/rules/tml-api-docs.mdc`（alwaysApply）。

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（v1.4：含等级/攻防/能量/进化双条件） |
| `docs/balance-stats.md` | **数值真源表**（等级带、经验、MidAtk/Def、种族 Mod、MoveRefRate；**已接线** `HenshinStatService`） |
| `docs/move-effects.md` | 招式/被动/大招泰拉适配表 |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `docs/fx-knowledge.md` | FX 目录 / cookbook / 踩坑（Living；特效改动先查这里） |
| `.cursor/skills/henshin-moves/` | 招式迭代 Skill：语义→**数值门**→预期效果确认→实现 |
| `.cursor/rules/tml-api-docs.mdc` | **alwaysApply**：设计须查 tModLoader stable API |
| `Assets/Forms/` · `Assets/Accessories/` | 36 形态 + 饰品图（非 FX；A13+ 暂复用旧图） |
| `Assets/Fx/` | CWR **拷贝**贴图（无运行时依赖）：SoftGlow / ThunderTrail / Fire(4×4) / Flashimpact(4×2) / HitJagged(1×2) / DiffusionCircle(360) / Cyclone / Fog / LightBeam / LightShot / TearFlame / Extra98 |
| `Content/Combat/Moves/HenshinFxDraw.cs` | Additive 绘制：`DrawContinuousBeam` / SheetFrame / `ScaleForWorldDiameter` |
| `Content/Combat/` · `Items/Forms/` | HenshinForceItem + 36 形态；`Wave2MoveProjs`（水柱/日棱/龙怒球/破灭等） |
| `Content/PlayerState/` · `Visual/` · `Accessories/` · 其它 | HenshinPlayer / 脚下能量条 UI / 饰品 / 被动进化天气挖掘 Net |

## 特效踩坑与禁止降级（必读）

1. **禁止擅自降级：** 用户点名参考效果必须按规格落地；跳过原版 AI、A=0「假 Additive」、纯尘冒充成品等，**未经确认不得当作成品**。招式迭代流程见 `.cursor/skills/henshin-moves`。
2. **懒加载贴图：** 壳弹只画不真生成 → 须 `LoadProjectile` / `ProjectileBorrow`（Bubble 踩坑）。
3. **Additive 保 Alpha；暗色抬亮：** `A=0` 全透明。`#2108ad` 等深色在 Additive 下几乎不可见 → 光晕用抬亮同色相（如 `DragonHaloLit`）。
4. **连续光束：** 优先 `DrawContinuousBeam`（SoftGlow 沿路径拉长 + 密叠）；厚度以格为单位（水炮≈1.25、加农≈2.5、日光束≈2）。间距过大 → 虚线。MagicPixel 可用，但**无封顶通天拉伸**易白屏，须控制 destination/scale。
5. **大图按世界直径缩放：** `DiffusionCircle` 360px 等须 `ScaleForWorldDiameter(tex, diameterPx)`；裸 `scale=1.7` / `width/96` 会画出超大圈。
6. **Sprite sheet：** Fire / Flashimpact / HitJagged **禁止整图绘制**，用 `HenshinFxDraw.Draw*Frame`。
7. **SpawnAtMouse：** `NewProjectile` 坐标是左上角；大 hitbox 须事后 `Center = MouseWorld`；改尺寸先存 Center。
8. **CWR：** 只读抄逻辑；贴图拷入 `Assets/Fx`；`build.txt` **不得** `modReferences` 大修。
9. **勿硬套自管位移原版 AI**（Nebula 等）：壳弹自管飞行，亡时再真生成爆炸碎片。

## 当前状态与下一步

- **代码（2026-09-07）：** 全 36 形态接线；脚下能量条；Stage 6+ FX；**Wave3 / Stage7+ 已验收**。详见 `docs/fx-knowledge.md`、`docs/move-effects.md`。
- **已验收基线（Stage≤5 cookbook）：** 天雷 / 泡沫 Load / 飞叶 Leaf / 咬合尖牙 / 龙波 Nebula（直线连发）。
- **已验收（能量 UI）：** 脚下条 + 满充金尘（2026-09-06）。
- **已验收（Wave1，2026-09-06）：** 抓狂 / 火焰牙 / 闪焰 / 泡沫 / 念力。
- **已验收（Stage 6，2026-09-06）：** 豪力 / 迷你龙 / 三地鼠 / 圆陆鲨；大岩蛇岩崩。
- **已验收（御三家终阶+金属怪，2026-09-06）：** 喷火龙 / 妙蛙花 / 水箭龟 / 金属怪。
- **已验收（暴风 + 大比鸟，2026-09-06）：** 大比鸟三招 / 哈克龙·快龙暴风。
- **已验收（Wave3，2026-09-07）：** 鬼斯通 / 哈克龙龙尾 / 怪力 / 胡地。
- **已验收（Stage7+，2026-09-07）：** 钢尾（`0,0,16,80` 罩）/ 猛撞灰日耀 / 暗影抓+影炎 / 恶波动×32 / 彗星拳+StarWrath / 破灭自缓加粗 / 巨金怪强念=胡地 / 龙俯冲·画龙点睛纯黑星尘龙 / 逆鳞火球 / 流星群64 / 空气爆三段 / 神鸟吟唱 / 气旋32格（`CycloneAttack`）/ 超梦强念×6穿墙·精神击破64球。
- **进化：** UIState；ProgressStage 上升或升级越过 `BandMin` 可弹；`/henshin evolve` 可补弹。进化须 `ProgressStage >= next.Stage` **且** `Level >= BandMin[next.Stage]`，并继承 Level/Xp。
- **数值（2026-09-09）：** `docs/requirements.md` v1.4 + `docs/balance-stats.md`：**物品等级/经验、FinalAttack/FinalDefense、能量池 1000、进化双条件已接线**。攻防按 Level 所在等级带插值。游戏内 DPS 抽检仍待本地。
- **已知缺口：** 无现役 `GrantsPhasing`；A11 无消费者；联机双端实测 / DPS 抽检 / Rage pending。
- **验证：** 游戏内 Build + Reload（TML003）；大招默认 Mouse3；`/henshin stats`、`/henshin setlevel`。
- **下一步：** 联机 → DPS 抽检 PS7/9/12 → Rage。
- 新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`。
