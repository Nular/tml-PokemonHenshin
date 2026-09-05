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

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（v1.3） |
| `docs/move-effects.md` | 招式/被动/大招泰拉适配表（v1.3 全形态已接线） |
| `docs/dev-plan.md` | 计划与任务（冲突以需求为准） |
| `Assets/Forms/` · `Assets/Accessories/` | 36 形态 + 饰品图（非 FX；A13+ 暂复用旧图） |
| `Assets/Fx/` | 从 CWR **拷贝**的 trail 贴图（`ThunderTrail`/`SoftGlow`/`LightShot`）；无 CWR 运行时依赖 |
| `Content/Core/` | FormDefinition / Registry / ProgressStage / Keybinds / 反射适配 |
| `Content/PlayerState/` | HenshinPlayer（能量分存、突进 CD、饰品标志）+ StarterGrant |
| `Content/Combat/` · `Items/Forms/` | HenshinForceItem + 36 形态；`SkyBoltLightning` / `RedesignedMoveProjs` / `Wave2MoveProjs` |
| `Content/Visual/` | UltimateEnergyUI（右下角能量条） |
| `Content/Accessories/` · `Items/Accessories/` | 仅变身生效饰品 A01～A21 |
| `Content/Affinity/` · `Evolution/` · `WeatherField/` · `TerrainEdit/` · `Loot/` · `Net/` | 被动 / 进化 / 天气 / 挖掘 / 获取 / NetOp |
| `tools/fetch_assets.py` | 从 52poke 拉图（buildIgnore） |
| 特效 | **优先**原版 `LoadProjectile` / `NewProjectile` 复用；CWR 只读抄逻辑，贴图可拷入 `Assets/Fx`；**禁止**运行时依赖 CWR / 生成灾厄弹；**禁止擅自降级**（见下） |
| 本地化 | HJSON 含引号/`\n` 须用 `"..."` 或 `'''...'''` |

## 特效踩坑与禁止降级（必读）

1. **禁止擅自降级：** 用户点名的参考效果（如神匠霹雳天雷、星云奥秘、吹叶机叶）必须按规格落地。MagicPixel 粗条、跳过原版 AI、A=0「假 Additive」等简化，**未经用户确认不得当作成品**。
2. **懒加载贴图：** `TextureAssets.Projectile[id]` 未触达前是 1×1 占位。壳弹只画 `Bubble`、从不 `NewProjectile(Bubble)` → 首次无图；用过泡泡枪后才亮。飞叶因真生成 `Leaf` 故正常。壳弹必须 `Main.instance.LoadProjectile` / `ProjectileBorrow.RequestProjectileTexture`。
3. **Additive + A=0 = 全透明：** XNA `BlendState.Additive` 常用 SourceAlpha；`color.A = 0` 会「有伤无光」。天雷须保留 Alpha，并用本模 `Assets/Fx/ThunderTrail`（黑底白电，Additive 下黑变透明）。
4. **勿硬套会自管位移的原版 AI：** 跳过 `NebulaArcanum` AI 会导致不飞/不画、只剩远处爆炸碎片。应对：自管壳弹 + 原版贴图/`LoadProjectile`，亡时再生成原版爆炸碎片并紫染色。
5. **CWR：** 只读参考路径/包络/宽度；贴图拷入 `Assets/Fx`；`build.txt` **不得** `modReferences` 大修。

## 当前状态与下一步

- **代码（2026-09-05）：** 被动+技能1/2+能量大招；全 36 形态接线；能量 UI；撞击/电光一闪 2s CD + 0.25s 无敌。
- **本轮特效修补：** 皮卡丘/雷丘天雷（`SkyBoltLightning` + `Assets/Fx`）；泡沫 `LoadProjectile(Bubble)`；飞叶=`Leaf`；咬住/咬碎尖牙 Rectangle；龙之波动=星云外观×10（0.7、不追踪）+ 紫染爆炸。
- **进化：** UIState 确认框；**ProgressStage 上升时**弹出；`/henshin evolve` 可补弹。
- **已知缺口：** 无现役 `GrantsPhasing`；A11 无消费者；游戏内手感/联机/DPS 验收 pending。
- **验证：** 御三家二阶看 **史莱姆神/鹿角怪**；大招默认 **Mouse3**；游戏运行中用游戏内 Build + Reload。
- **下一步：** 验收清单、联机双端、DPS 对标、Rage/肾上腺素。
- 新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`。
