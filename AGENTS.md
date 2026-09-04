# Pokemon Henshin — Agent 入口

## 定位

泰拉瑞亚 + tModLoader + 灾厄模组：持握「{宝可梦}之力」换皮变身、双招式、情境被动；松手失效。联机必需。

## 怎么跑

仓库根即 tML 模组根（扁平布局，模组内部名 `PokemonHenshin`）。

- 命令行：仓库根执行 `dotnet build`，`tMLMod.targets` 会自动调用 tML 打包到 `Documents\My Games\Terraria\tModLoader\Mods\PokemonHenshin.tmod`。游戏正在运行且启用本模时打包会报 TML003，此时只能走游戏内构建
- 游戏内：`ModSources\PokemonHenshin` 是指向本仓库的目录联接，Workshop → Develop Mods → Build + Reload
- 钉死版本：**tModLoader 1.4.4.9 / 2026.07 stable（net8.0）**，**CalamityMod 2.2.4**；必须启用 Calamity 才能加载

## 技术栈

C# / tModLoader / 强依赖 CalamityMod（`build.txt` `modReferences`）。M0 代码不直接引用灾厄类型（盗贼类走 `ModContent.TryFind`）；需要 `DownedBossSystem` 时（M1）再在游戏内 Extract 灾厄 dll 到 `ModSources\ModAssemblies` 并加 csproj Reference。参考实现只读：`../CalamityOverhaul`（禁止改该仓库、禁止运行时依赖它）。

## 目录与约定

| 路径 | 角色 |
|------|------|
| `docs/requirements.md` | **产品唯一真相**（当前 v1.2） |
| `docs/dev-plan.md` | 开发计划与任务拆解（对齐需求；冲突以需求为准） |
| `Assets/Forms/` | 仅宝可梦精灵图（从 52poke 全国图鉴自取；`L01_F01.png` 取自 HGSS 精灵 `Spr_4h_004`，取首帧裁边） |
| `Content/Core/` | `FormDefinition`（含 `MoveA/MoveB`）/ `FormRegistry` / `MoveSpec` / `PokemonType` |
| `Content/Damage/` | `HenshinDamage`（k=0.35 唯一职业折算挂点） |
| `Content/PlayerState/` | `HenshinPlayer` 变身状态机（唯一权威；含禁坐骑、HideDrawLayers、网络判脏） |
| `Content/Visual/` | `HenshinOverlayLayer`（会先清掉其他模组塞进 `DrawDataCache` 的物品贴图，见 dev-plan「M0 实测踩坑」） |
| `Content/Combat/` | `HenshinForceItem` 物品基类 + `Moves/` 招式弹幕（贴图全部复用原版） |
| `Content/Items/Forms/` | 各形态物品（当前 `CharmanderForce` = L01_F01） |
| `Content/Net/` | `HenshinNet`（显式 `NetOp` 枚举；现有 `SyncForm`） |
| 特效 | **禁止**新增 FX 图片；复用原版 / 灾厄 / 大修写法 |

## 当前状态与下一步

- 状态：**M0 完成**（2026-09-04 单机游戏内验收通过：持握变身 / 解除 / 禁坐骑 / 双招式）。pending：联机双端互见实测（dev-plan §7.2 N1～N4）、Rage/肾上腺素是否计入 HenshinDamage。
- 下一步：按 `dev-plan.md` §5 **M1**（ProgressStage 适配器 → 进化替换 → 确认 UI → L01_F02 → 御三家发放）；M1.1 需先在游戏内 Extract 灾厄 dll。
- 新增形态：继承 `HenshinForceItem`，在 `CreateDefinition` 分配唯一 `NetworkId`，注册自动发生在 `SetStaticDefaults`。
- **tML 坑：** ModItem 每个物品是新实例，模板实例字段不会被复制——每形态共享数据只放 `FormDefinition`，别放 ModItem 实例字段。
- 开局发御三家；持握禁坐骑；后期含超梦/洛奇亚/烈空坐。
