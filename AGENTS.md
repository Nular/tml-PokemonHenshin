# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄：持握「{宝可梦}之力」换皮变身；**持握被动 + 技能1/2 + 能量大招**；仅变身生效饰品；松手失效。联机必需。

## 怎么跑

仓库根即 tML 模组根（内部名 `PokemonHenshin`）。

- 命令行：根目录 `dotnet build` → 打包到 `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod`；游戏运行且启用本模时会 **TML003**，只能游戏内 Build + Reload
- 游戏内：`ModSources\PokemonHenshin` 目录联接 → Workshop → Develop Mods → Build + Reload
- 版本钉死：**tML 1.4.4.9 / 2026.07（net8.0）**，**CalamityMod 2.2.4**（必须启用）

## 技术栈

C# / tModLoader / `modReferences = CalamityMod`。进度用 **反射** `CalamityProgressAdapter`（**无需** Extract dll）。只读参考 `../CalamityOverhaul`（禁止改、**禁止运行时依赖**）。

**全局 API 规范：** 设计/实现须参考 [tModLoader stable 类表](https://docs.tmodloader.net/docs/stable/annotated.html)，按需点进具体类页；项目 Rule `.cursor/rules/tml-api-docs.mdc`（alwaysApply）。

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（v1.3） |
| `docs/move-effects.md` | 招式/被动/大招泰拉适配表 |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `docs/fx-knowledge.md` | FX 目录 / cookbook / 踩坑（Living；特效改动先查这里） |
| `.cursor/skills/henshin-moves/` | 招式迭代 Skill：语义→预期效果确认→实现 |
| `.cursor/rules/tml-api-docs.mdc` | **alwaysApply**：设计须查 tModLoader stable API |
| `Assets/Forms/` · `Assets/Accessories/` | 36 形态 + 饰品图（非 FX；A13+ 暂复用旧图） |
| `Assets/Fx/` | CWR **拷贝**贴图（无运行时依赖）：SoftGlow / ThunderTrail / Fire(4×4) / Flashimpact(4×2) / HitJagged(1×2) / DiffusionCircle(360) / Cyclone / Fog / LightBeam / LightShot / TearFlame |
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

- **代码（2026-09-06）：** 全 36 形态接线；**脚下**大招能量条（`UltimateEnergyUI`）+ 满充金尘；Stage 6+ FX 主路径已落地。详见 `docs/fx-knowledge.md`、`docs/move-effects.md`。
- **已验收基线（Stage≤5 cookbook）：** 天雷 / 泡沫 Load / 飞叶 Leaf / 咬合尖牙 / 龙波 Nebula（直线连发）。
- **已验收（能量 UI）：** 脚下条 + 满充金尘（2026-09-06）。
- **已验收（Wave1 手感，2026-09-06）：** 抓狂（15 格三线爪）/ 火焰牙（两对大弧牙）/ 闪焰冲锋（32 格多线火径+包裹焰+收尾减速）/ 泡沫光线（窄直线+破裂）/ 念力（ShadowBeam 索敌弹射×2+命中紫环）。
- **已验收（Stage 6，2026-09-06）：** 豪力（岩崩微偏+落地1格爆 / 劈瓦 / 爆裂拳挥拳石爆）/ 迷你龙 / 三地鼠 / 圆陆鲨（含流沙 Typhoon+琥珀）；大岩蛇岩崩 12 石±12 格。
- **待游戏内验收：** Stage 7+ → 联机/DPS。
- **进化：** UIState；**ProgressStage 上升时**弹出；`/henshin evolve` 可补弹。
- **已知缺口：** 无现役 `GrantsPhasing`；A11 无消费者；联机/DPS pending；`StoneEdge` / `DracoMeteor` 等仍 NeedsUpgrade。
- **验证：** 游戏运行中用游戏内 Build + Reload（TML003）；大招默认 **Mouse3**。
- **下一步：** Stage 7+ → 联机双端 → DPS 对标 → Rage/肾上腺素。
- 新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`。
