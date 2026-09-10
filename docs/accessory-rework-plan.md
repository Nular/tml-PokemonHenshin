# 饰品重构实现计划（审阅稿）

| 项 | 内容 |
|----|------|
| 状态 | **已实现，待游戏内验收** |
| 日期 | 2026-09-09 |
| 基线 | `origin/main` @ `06509c0`（neat-freak 文档对齐）。能量软顶 90→1000 在 PR #7，合入与否不阻塞本计划（实现时按当时 main 的常量）。 |
| 权威 | 产品规则仍以 `docs/requirements.md` 为准；**本文件是本次饰品重构的施工图**。实现后把已落地规则回写 requirements §6 / §10，本文件可删或改成「已完成」归档。 |
| 道具名/图 | [52poke 道具列表](https://wiki.52poke.com/wiki/道具列表) 官方中文名 + `Bag {中文名} SV Sprite.png` |

本文给主 Agent 与 subagent 共用：数字、枚举、文件归属、掉落、合成、验收都写在这里。**不要在聊天里另发明一套。** 冲突时：用户审阅批注 > 本文件 > 旧 requirements §10 > 现役代码。

---

## 0. 已锁定决策（不要在实现时再改）

1. **学习装置范围：** 仅热键栏 **0–9**（10 格）。不含背包 10–49、鼠标、猪猪、保险箱、银行、地上物。
2. **叠乘/叠加：同族不互斥。** 碎片、普通、超级是不同 `Item.type`，允许同时装备并**全部生效**（对标原版毁灭者徽章与复仇者徽章并存）。泰拉「同 type 不能双戴」仍然成立，因此不会出现两件完全相同的普通力量头带。
3. **超级 2× 规则：** 倍率类 ×2；冷却类减半（更短）；开关类保持开关，并给附加价值（见各家族表）。
4. **范围：** 旧 A01–A21 **全部**改碎片线；新 7 件也全部做。共 **28 家族 ×（6 碎片 + 1 普通 + 1 超级）= 224 物品**。
5. **不变之石：** **唯一破例**——未变身也生效（戴着就挡进化）。
6. **贴图（2026-09-10）：** `fetch_assets` 拉 52poke 袋内图 → 高清备份 `_src_hires/`；`tools/pixelize_accessories.py`（pixeloe）产出 **64×64** 的 `Axx.png` / `Axx_Super.png`（粗金边+闪点）/ `Axx_Shard.png`（碎片剪影）。S1–S6 共用 `_Shard`，库存仍叠片号角标（不生成 168 张）。
7. **广角镜 / 诅咒之符：** 共用 `MoveDelivery` 分类；**作用集合不同（方案 B）**。广角镜 **不含 Beam**。
8. **无条件加伤**并进之力 `ModifyWeaponDamage`（面板看得到）。有条件（Boss / 着火 / 大招槽）仍走命中。
9. **饰品伤不对齐之力 DPS 80–120% 窗**；对标同阶段灾厄 / 大修饰品量级。
10. **仅变身生效**（除不变之石）。未变身：占用栏、显示未生效标签、本模效果不加。共鸣不满足：占用栏、显示未生效。

### 0.1 明确不做

- 饰品互相合成（力量头带 + 广角镜 → 新件）。只有「本家族碎片 → 本家族普通 / 超级」。
- 饰品自身升级 / 吃经验。
- 赶进度腰带、进化催促。
- 给敌人打宝可梦属性、未变身强力隐藏效果。
- 新增招式 FX 图片（超级闪光是 **饰品图标**，走 `Assets/Accessories/`，合法）。
- 改 CalamityOverhaul；编译期不 `using CalamityMod`（掉落用原版 ID + 现有反射 / `Mod.TryFind`）。

---

## 1. 52poke 名称与贴图审阅（必须按此改）

现役 **DisplayName 与贴图错位严重**：`tools/fetch_assets.py` 已按官网图下 A01–A12，但中文名是自造的；A13–A21 代码里复用 A01–A12 贴图。A05 用了气势披带图却做减伤；A06 用了生命宝珠图却叫灾限核心。

### 1.1 旧 21 件更名

| 家族 | 现 DisplayName / 类名 | **52poke 官方名** | 英文 | 袋内图文件名 | 备注 |
|------|----------------------|-------------------|------|--------------|------|
| A01 | 万能胶囊带 / AbilityCapsuleBelt | **特性胶囊** | Ability Capsule | `Bag 特性胶囊 SV Sprite.png` | 原作是消耗品；本模泰拉化为冷却。类名可留。 |
| A02 | 专注头带 / MuscleBand | **力量头带** | Muscle Band | `Bag 力量头带 SV Sprite.png` | Tooltip 原文已是力量头带，显示名错了。 |
| A03 | 气息挂坠 / SoulDewPendant | **心之水滴** | Soul Dew | `Bag 心之水滴 SV Sprite.png` | |
| A04 | 轻身脚环 / FloatStoneAnklet | **轻石** | Float Stone | `Bag 轻石 SV Sprite.png` | |
| A05 | 守护徽章 / FocusSashBadge | **气势头带** | Focus Band | `Bag 气势头带 SV Sprite.png` | **必须换图**。现图是披带。减伤对应「有时撑 1HP」的泰拉化。 |
| A06 | 灾限核心 / LifeOrbCore | **达人带** | Expert Belt | `Bag 达人带 SV Sprite.png` | **必须换图**。现图是生命宝珠。`HenshinDamageFactor +0.05` 保留。 |
| A07 | 木炭袋 / CharcoalBag | **木炭** | Charcoal | `Bag 木炭 SV Sprite.png` | |
| A08 | 水囊 / MysticWaterPouch | **神秘水滴** | Mystic Water | `Bag 神秘水滴 SV Sprite.png` | |
| A09 | 电池片 / MagnetChip | **磁铁** | Magnet | `Bag 磁铁 SV Sprite.png` | 不要用充电电池（那是 A19）。 |
| A10 | 薄翼膜 / SharpBeakMembrane | **锐利鸟嘴** | Sharp Beak | `Bag 锐利鸟嘴 SV Sprite.png` | |
| A11 | 咒符布 / SpellTagCloth | **诅咒之符** | Spell Tag | `Bag 诅咒之符 SV Sprite.png` | 效果改为射弹穿墙。 |
| A12 | 龙牙饰 / DragonFangCharm | **龙之牙** | Dragon Fang | `Bag 龙之牙 SV Sprite.png` | |
| A13 | 广角镜 | **广角镜** | Wide Lens | `Bag 广角镜 SV Sprite.png` | 现复用 A02 图，必须换成自己的。 |
| A14 | 讲究头带 | **讲究头带** | Choice Band | `Bag 讲究头带 SV Sprite.png` | |
| A15 | 焦点镜 | **焦点镜** | Scope Lens | `Bag 焦点镜 SV Sprite.png` | |
| A16 | 生命宝珠 | **生命宝珠** | Life Orb | `Bag 生命宝珠 SV Sprite.png` | |
| A17 | 贝壳之铃 | **贝壳之铃** | Shell Bell | `Bag 贝壳之铃 SV Sprite.png` | |
| A18 | 凸凸头盔 | **凸凸头盔** | Rocky Helmet | `Bag 凸凸头盔 SV Sprite.png` | |
| A19 | 充能束带 | **充电电池** | Cell Battery | `Bag 充电电池 SV Sprite.png` | 能量获取。原作是一次性；本模持续饰品。 |
| A20 | 余韵挂坠 | **光之黏土** | Light Clay | `Bag 光之黏土 SV Sprite.png` | 「余韵」→ 墙/场残留加长，对应大招留能。 |
| A21 | 爆发腕章 | **弱点保险** | Weakness Policy | `Bag 弱点保险 SV Sprite.png` | 「被打爆输出」→ 大招伤害↑、充能↓。 |

### 1.2 新 7 件（官方名，不用俗称当 DisplayName）

| 家族 | 官方名 | 英文 | 袋内图 | 用户口头 | 效果摘要 |
|------|--------|------|--------|----------|----------|
| A22 | **学习装置** | Exp. Share | `Bag 学习装置 SV Sprite.png` | — | 热键栏其它之力分经验 |
| A23 | **幸运蛋** | Lucky Egg | `Bag 幸运蛋 SV Sprite.png` | — | 持握 XP↑ |
| A24 | **不变之石** | Everstone | `Bag 不变之石 SV Sprite.png` | 「不变石」 | 戴着阻止进化（未变身也生效） |
| A25 | **黑带** | Black Belt | `Bag 黑带 SV Sprite.png` | 近战/撞击件 | 短距伤 + 撞击 CD |
| A26 | **吃剩的东西** | Leftovers | `Bag 吃剩的东西 SV Sprite.png` | 「剩饭」（非官方） | 持续回血 |
| A27 | **气势披带** | Focus Sash | `Bag 气势披带 SV Sprite.png` | — | 致死留 1（碎片 ≥60% 血 / 成品 ≥50% / 超级 ≥30%），60s CD |
| A28 | **进化奇石** | Eviolite | `Bag 进化奇石 SV Sprite.png` | — | 还能进化的形态加防 |

Tooltip 第一行用 52poke 官方说明（可略缩）；第二行起写本模效果。英文 `en-US` 用上表英文名。

---

## 2. 目标架构（脚手架必须一次搭对）

### 2.1 设计原则

- **数据驱动：** 28 家族的数字、碎片拆分、合成、掉落、贴图文件名全部写在 **一个目录类**，禁止在 224 个 `ModItem` 子类里抄数字。
- **物品用 `AddContent` 生成：** 一个 `HenshinAccItem : ModItem`，构造时注入 `familyId + piece`。`override string Name` 稳定内部名（见 2.3）。不要手写 224 个 class。
- **删掉** `Content/Items/Accessories/HenshinAccessories.cs` 里 21 个空壳 class（或改成 `[Obsolete]` 过渡一 PR 后删除）。内部名若变了，旧存档物品会丢——**内部名必须稳定映射**，见下。

### 2.2 内部名稳定映射（存档兼容）

tML 用 `ModItem.Name` 存盘。旧名必须继续指向 **普通成品**：

| 旧 Name（现役 class） | 新普通成品 Name | 碎片 | 超级 |
|----------------------|-----------------|------|------|
| `A01AbilityCapsuleBelt` | 同左 | `A01AbilityCapsuleBelt_S1` … `_S6` | `A01AbilityCapsuleBelt_Super` |
| `A02MuscleBand` | 同左 | `_S1`–`_S6` | `_Super` |
| …A03–A21 同规则 | | | |
| （新） | `A22ExpShare` | `_S1`–`_S6` | `_Super` |
| | `A23LuckyEgg` | | |
| | `A24Everstone` | | |
| | `A25BlackBelt` | | |
| | `A26Leftovers` | | |
| | `A27FocusSash` | | |
| | `A28Eviolite` | | |

`HenshinAccItem.Name` 按上表返回。旧玩家已合成的「万能胶囊带」读档后变成「特性胶囊」普通成品（效果升级为碎片线的普通件），可接受。

### 2.3 新增 / 重写文件（归属，禁止越界）

| 路径 | 职责 | 谁改 |
|------|------|------|
| `Content/Accessories/AccFamilyId.cs` | `enum AccFamilyId : byte` A01–A28 | 脚手架 |
| `Content/Accessories/AccPiece.cs` | `enum AccPiece { S1=1,…S6=6, Normal=10, Super=20 }` | 脚手架 |
| `Content/Accessories/MoveDelivery.cs` | 招式交付枚举 | 脚手架 |
| `Content/Accessories/HenshinAccCatalog.cs` | **唯一数字/拆分/掉落/合成/图/官方名表** | 脚手架先骨架，WP-C 填满 |
| `Content/Accessories/HenshinAccItem.cs` | 通用 ModItem（UpdateAccessory / Tooltip / Texture） | 脚手架 |
| `Content/Accessories/HenshinAccessoryItem.cs` | 旧基类：改为 thin wrapper 或删除；逻辑迁到 `HenshinAccItem` | 脚手架 |
| `Content/Accessories/HenshinAccLoader.cs` | `ModSystem.OnModLoad` / `ILoadable`：循环 28×8 `AddContent` | 脚手架 |
| `Content/Accessories/HenshinAccGlobalProjectile.cs` | OnSpawn：子弹继承 Homing / Delivery / 穿墙 | WP-B |
| `Content/Core/MoveSpec.cs` | 加 `MoveDelivery Delivery`；废弃作为判定的 `IsRangedProjectile`（可留字段但工厂改写 Delivery） | WP-B |
| `Content/Items/Forms/StarterLines.cs` | 每个工厂赋 `Delivery` | WP-B **独占此文件工厂段** |
| `Content/Combat/HenshinForceItem.cs` | FireMove 打标；ModifyWeaponDamage 吃无条件乘区；Tooltip | WP-B + WP-D |
| `Content/Combat/Moves/SharedMoveProjs.cs` | `IHenshinMoveProj` 加 `Delivery`；HomingAI 不变 | WP-B |
| `Content/PlayerState/HenshinPlayer.cs` | 新字段、Reset、命中/受伤/XP/能量/披带/剩饭 | WP-D **独占运行时字段** |
| `Content/Evolution/EvolutionService.cs` + ConfirmUI | 不变之石门闩 | WP-E |
| `Content/Loot/HenshinLoot.cs` | **删光旧 AddAcc 占位配方**；只保留之力掉落；饰品掉落/配方走 Catalog | WP-F |
| `Content/Loot/HenshinDropDatabase.cs` | `ModifyNPCLoot` / `ModifyItemLoot`：碎片 + 之力 Boss/匣/袋 | WP-F |
| `Localization/zh-Hans_*.hjson` + `en-US_*.hjson` | 物品名/说明/角标/生效标签 | WP-G |
| `tools/fetch_assets.py` | ACC_FILES A01–A28 → 袋内图 | WP-H |
| `tools/pixelize_accessories.py` | 64×64 pixeloe + Super 金边闪点 + `_Shard` 剪影 | WP-H |
| `tools/make_super_accessory_sprites.py` | **已弃用**：转发到 `pixelize_accessories.py` | WP-H |
| `docs/requirements.md` §6 §10 | 实现结束后回写（本计划落地后再改，避免审阅期双源） | 收尾 |
| `docs/accessory-rework-plan.md` | 本文件 | 主 Agent |

`Items/Accessories/HenshinAccessories.cs`：脚手架完成后删除，避免 21 个 class 与 Loader 抢同一个 Name。

### 2.4 `HenshinAccCatalog` 每条家族必填字段

```text
AccFamilyDef {
  AccFamilyId Id
  string LegacyTypeName          // 存档 Name
  string OfficialZh              // 52poke
  string OfficialEn
  string WikiBagFile             // "Bag 力量头带 SV Sprite.png"
  string TexturePath             // "PokemonHenshin/Assets/Accessories/A02"
  string SuperTexturePath        // ".../A02_Super"
  string ShardTexturePath        // ".../A02_Shard"（S1–S6）
  PokemonType Resonance          // None = 通用
  bool WorksUntransformed        // 仅 A24
  int MinStage                   // 普通成品出现的进度锚（F1–F4 须在此之前可凑齐）
  int SuperMinStage              // F5–F6 / 超级锚
  ItemRarity NormalRarity / SuperRarity
  AccStatLine[] Shard1..Shard6   // 可装备时 Apply 的操作列表
  AccStatLine[] NormalStats      // = Shard1–4 的并集（完整值，不是四次相加）
  AccStatLine[] SuperStats       // 2× 规则展开后的列表（写死数字，禁止运行时再 ×2，避免双乘）
  AccLootSpec ShardLoot[1..6]
}
```

**Apply 时只读当前 piece 对应的那张 Stat 表。** Super 的数字在表里已经是 2×，代码不要再乘。

### 2.5 `AccStatLine`（唯一允许的运行时效果种类）

实现一个 `void Apply(HenshinPlayer hp, AccStatLine line)`。禁止家族 class 私自加字段却不登记。

| `AccStat` | 叠法 | 面板？ | 说明 |
|-----------|------|--------|------|
| `DamageBonus` | `+=` | 是 | `HenshinDamageBonus` |
| `DamageFactorBonus` | `+=` | 是 | `HenshinDamageFactorBonus`（A06） |
| `MeleeDeliveryDamage` | `+=` | 是* | 仅 `MeleeArc/Lunge/StrikeFall` 命中乘；面板用注释行「近战招式 +X%」 |
| `BossDamageBonus` | `+=` | 否 | 命中且 `target.boss` |
| `OnFireTargetBonus` | `+=` | 否 | |
| `UltDamageBonus` | `+=` | 否 | `LastMoveSlot==Ultimate` |
| `IncomingMul` | `*=` | 否 | 如 0.94；多件再乘 |
| `CooldownMul` | `*=` | 否 | 招式 UseTime |
| `DashCooldownMul` | `*=` | 否 | |
| `LungeCooldownMul` | `*=` | 否 | `StartLungeCooldown` 乘 |
| `MoveSpeedBonus` | `+=` | 否 | |
| `WaterSpeedBonus` | `+=` | 否 | |
| `FlightEnergySec` | `+=` | 否 | |
| `FallDmgReductionSet` | `min(现,值)` | 否 | 坠落承伤比例，越小越好；默认 1 |
| `AffinityAmp` | `+=` | 否 | |
| `EnergyGainMul` | `*=` | 否 | 命中+击杀能量 |
| `UltRetain` | `max(现,值)` | 否 | 大招保留比例 |
| `XpHeldMul` | `+=` | 否 | 幸运蛋，持握那只 |
| `XpHotbarShareMul` | `+=` | 否 | 学习装置，其它 9 格各复制 |
| `HomingTurn` | `max(现,值)` + 开标志 | 否 | 广角镜角速度 |
| `HomingRange` | `max(现,值)` | 否 | 广角镜菱形索敌格数。碎片 8、成品 16、超级 32；断锁 2×。新锁 60° 半角 |
| `HomingEnableBoltSpreadBarrage` | OR | 否 | |
| `TilePierceBoltSpreadBarrageDotBind` | OR | 否 | 诅咒之符（不含 Beam 以外再加 Beam） |
| `TilePierceBeam` | OR | 否 | 诅咒之符梁 |
| `ChoiceLockSkill2` | OR | 否 | |
| `ChoiceLockUlt` | OR | 否 | |
| `ChoiceDamage` | `+=` | 是 | 讲究头带伤，无条件 |
| `LifeOrbDamage` | `+=` | 是 | |
| `LifeOrbHpDrain` | OR | 否 | 有则扣 HP（闸门共用） |
| `ShellBellHeal` | `+=`（整数 HP） | 否 | 共用 CD |
| `RockyHelmetScale` | `+=` | 否 | 受击反伤倍率（1.0 基准） |
| `LeftoversHpPerSec` | `+=` | 否 | |
| `FocusSash` | OR | 否 | 开启致死留 1 |
| `FocusSashHpPct` | **min**（越小越强） | 否 | 触发时当前 HP 须 ≥ max×该比例。碎片 0.60、成品 0.50、超级 0.30。未装备时不写（保持 1.0 哨兵） |
| `FocusSashCdSec` | **min** | 否 | 触发后 CD 秒。碎片 90、成品 60、超级 30 |
| `EvioliteDefMul` | `+=` | 否 | 仅当 `FindEvolutionOf(current)!=null` |
| `EverstoneBlock` | OR | 否 | 未变身也 Apply |
| `CritUpgradeChance` | `+=` | 否 | 焦点镜 |
| `PassiveEnergyMul` | `+=` | 否 | 只乘 0.15/tick 被动（A19 碎片用） |

\*近战伤：`ModifyHitNPC` 里看 `force.GetMove(LastMoveSlot).Delivery`。

**禁止**再引入未列在此表的偷偷摸摸字段。要新效果先改本计划此表。

---

## 3. 招式交付枚举（WP-B 的核心）

### 3.1 枚举

```csharp
public enum MoveDelivery : byte
{
    None = 0,
    MeleeArc,     // 爪/鞭/尾/拳弧
    Lunge,        // 身体撞击（含 RequiresLungeCooldown 的突进）
    StrikeFall,   // 下落砸
    Bolt,         // 单体飞行弹
    Spread,       // 扇形/锥
    Barrage,      // 连发/多子弹
    Beam,         // 直线梁（含蓄力梁、水柱、加农）
    AoEBurst,     // 落点/自身爆发
    DoTBind,      // 缠绕飞行弹
    Field,        // 场地
    Dig,          // 挖洞
    Blink         // 瞬移打击
}
```

`MoveSpec.Delivery` 必填。`IHenshinMoveProj.Delivery` 由 `FireMove` 写入。子弹 `OnSpawn` 从 parent 拷贝。

### 3.2 饰品作用集合（方案 B）

| 饰品 | 集合 |
|------|------|
| 广角镜追踪 | `{ Bolt, Spread, Barrage, DoTBind }` **不含 Beam** |
| 诅咒之符穿墙 | `{ Bolt, Spread, Barrage, DoTBind, Beam }` |
| 黑带短距伤 | `{ MeleeArc, Lunge, StrikeFall }` |
| 黑带撞击 CD | `Delivery==Lunge` **或** `RequiresLungeCooldown`（Blink / DigLunge / BraveBird 也吃 CD 减） |

内置追踪（强念等 AI 写死 `HomingAI(..., true)`）**不关**。广角镜是额外转弯。实现时：`Homing = 招式自己要追 \|\| (AccHoming && set.Contains(Delivery))`。招式自己要追的，继续用自己的 turnRate 与饰品 `max`。

`HomingAI` 在 `velocity≈0` 时 return —— Beam/场地本来就不该被广角镜弯折。

### 3.3 工厂 → Delivery（`FormItemUtil` 全表，WP-B 按此改，不要猜）

| 工厂 | Delivery | 备注 |
|------|----------|------|
| Slash / Scratch / VineWhip / BiteArc / CrossChop* / ShadowClawSlash / DragonTailWhip / IronTailWhip / LickFan / BrickBreak / TripleStab / FocusPunch / CometPunch / TakeDownUlt | MeleeArc | 弧、鞭、拳、猛撞灰日耀走弧则 MeleeArc |
| Lunge / BraveBirdUlt / AerialAceBlink / BlinkStrike / Dig / DigUlt / DragonDive / DragonAscentUlt / FlareBlitzUlt / CloseCombatUlt | Lunge | Blink 位移仍吃撞击 CD |
| Strike | StrikeFall | |
| FireBolt / WaterBolt / Bolt / FlareUlt / AquaGun / MudSlap / MidThunder / BigShadowBall / SludgeBolt / StrongPsychic / AlakazamPsychic / MewtwoPsychic / SeedBombUlt | Bolt | MidThunder 是电球不是梁 |
| PeckCone / FlameCone / LeafSpread / DragonBreath / DarkPulseCone / AirBurst | Spread | |
| BubbleBarrage / SeedBarrageUlt / DragonRage / MeteorBarrageUlt / RockSlideX / FlailUlt / MewtwoPsystrikeUlt | Barrage | 导演弹 Delivery=Barrage，子弹继承 |
| Beam / ThickBeam / SustainedBeam / WaterJet / ChargeBeamUlt / HyperBeamUlt / ThunderboltUlt / ThunderPillarUlt / DragonPulse | Beam | **龙之波动导演弹标 Beam**（直线连发星云，不追踪） |
| AoE / MouseAoE / FutureSightUlt / QuakeUlt / PetalDance / HurricaneField / WeatherPainHurricane / SkyAttack | AoEBurst 或 Field | Petal/Hurricane/WeatherPain→**Field**；RockTomb 落点→AoEBurst；ThunderboltUlt 是天雷柱→**Beam**（已列） |
| Vortex / MouseVortex | DoTBind | |
| Field() | Field | 无形态调用，仍赋值 |
| Sleep / HypnosisUlt | Field | 范围控制 |
| GroundCyclone / StoneEdge | Bolt 或 Barrage | GroundCyclone→Bolt；StoneEdge→Barrage |
| ZenHammer | MeleeArc | 贴身锤 |
| ResonanceScatterUlt | Barrage | |
| OutrageUlt | Barrage | 火球雨 |

WP-B 改工厂时：**每个 `new MoveSpec` 必须有 `Delivery =`。** 漏了编不过（可用 `#nullable` 或分析器；最低限度 PR 内 grep `new()` 无 Delivery）。

`IsRangedProjectile`：工厂可继续写，但 **FireMove 禁止再用它决定 Homing**。可标 `[Obsolete]`。

### 3.4 子弹继承（防止「只有母弹打标」）

`HenshinAccGlobalProjectile.OnSpawn`：

1. 若 `ModProjectile is IHenshinMoveProj child` 且 `source` 为 `EntitySource_Parent { Entity: Projectile parent }` 且 parent 也是 `IHenshinMoveProj`：拷贝 `Homing, HomingTurnRate, Delivery, EasyCrit, IgnoreDefensePartial`。
2. 再读 owner 的 `HenshinPlayer`：若穿墙集合命中 `child.Delivery`，设 `Projectile.tileCollide = false`。
3. 所有本模 `NewProjectile` 必须用 `Projectile.GetSource_FromThis()` / `player.GetSource_FromThis()`，禁止 `null` source。WP-B 扫一遍 `NewProjectile(`。

---

## 4. 面板伤

`HenshinForceItem.ModifyWeaponDamage`（变身时）：

```text
damage *= 1 + HenshinDamageBonus
damage *= 1 + HenshinDamageFactorBonus
damage *= 1 + ChoiceDamage
damage *= 1 + LifeOrbDamage
```

`ModifyHitNPC` **不要再乘一遍**上述四项（现在 A02/A14/A16 在 hit 上乘，改完只留 hit：Boss / 着火 / 大招 / 近战 Delivery / 暴击升档 / aftermath）。

Tooltip：`ForceStats` 那行继续用 `GetWeaponDamage`（会吃 ModifyWeaponDamage）。另加一行浅色：`近战招式 +X%`（若黑带>0）、`对Boss 命中 +X%`（若龙之牙>0）——数字来自当前 `HenshinPlayer`，未变身不显示加成。

---

## 5. 叠加、Reset、联机

### 5.1 每 tick Reset（`HenshinPlayer.ResetEffects` 或现有清零块）

所有 `+=` 字段归 0，所有 `*=` 字段归 1，所有 OR 归 false，`UltRetain` 归 0，`FallDmgReductionSet` 归 1，`HomingTurn` 归默认 0。然后 `UpdateAccessory` 按装备件数累加。

**禁止**在 Apply 里写 `= 0.12f` 覆盖（广角镜多件应 `max` 角速度 + 伤害类 `+=`）。

### 5.2 同族 4 碎片 + 普通 + 超级

允许。力量头带例：S1–S4 合计 +6%，普通再 +6%，超级再 +12% → 最多 +24% 该家族（占 6 栏）。这是刻意的栏位税，对标多徽章。

### 5.3 联机

饰品效果走原版 `UpdateAccessory`（双端）。XP 只在服务端 `GrantKillExperience` 写入物品并 `SendEnergy`/`SyncForceProgress`。不变之石在 **服务端** `RequestEvolve` 拒绝。客户端 UI 在戴着石头时隐藏/禁用确认钮，防止误点。

---

## 6. 家族效果表（普通 = S1–S4 并集；超级数字已是 2×）

碎片「约 1/4」：能拆的拆成不同轴；不能拆的把同一轴切成约 1/4。S5/S6 是超级材料，**也可装备**，强度约普通预算的 1/4（或一个附加轴）。

下列 **普通** 列 = 成品；**超级** 列 = 成品 2× 规则后的最终数。碎片列只写该片 Apply 的线。

### 6.1 通用战斗

**A01 特性胶囊** MinStage 1 / Super 5  
普通：冷却 ×0.95  
超级：冷却 ×0.90  
- S1 冷却 ×0.9875（约 1/4 的 5% 减免）  
- S2 冷却 ×0.9875  
- S3 冷却 ×0.9875  
- S4 冷却 ×0.9875  
- S5 被动能量 ×1.10  
- S6 冷却 ×0.9875  
（四件 S1–S4 连乘 ≈0.95。实现时用「冷却减免加法」更稳：`CooldownCut += 0.0125`，最终 `CooldownMul = 1 - min(0.20, Cut)`。**采用加法减免。** 普通 Cut=0.05；超级 Cut=0.10；每碎片 Cut=0.0125。）

**A02 力量头带** Min 3 / Super 6  
普通 DamageBonus +6%；超级 +12%。碎片各 +1.5%。S5 +1.5%；S6 CritUpgrade +2%。

**A03 心之水滴** Min 4 / Super 7  
普通 AffinityAmp +20%；超级 +40%。碎片各 +5%。S5 +5%；S6 在超能或龙共鸣时再 +5% DamageBonus（无共鸣则 0）。

**A04 轻石** Min 5 / Super 8  
普通：飞行 +1s、移速 +5%；超级：+2s、+10%。  
S1 移速 +1.25%；S2 移速 +1.25%；S3 飞行 +0.25s；S4 飞行 +0.25s；S5 移速 +1.25%；S6 飞行 +0.25s。  
普通并集用满值 +1s/+5%，不要把 S1–S4 再加一遍到普通表（普通表写满值）。

**A05 气势头带** Min 7 / Super 10  
普通 IncomingMul ×0.94（Cut 6%）；超级 Cut 12%（×0.88）。加法 IncomingCut，封顶 0.30。碎片各 Cut 1.5%。S5 Cut 1.5%；S6 受伤后 0.5s 内再 Cut +3%（实现为短 Timer，可放 WP-D 用已有 GuardBonusTimer）。

**A06 达人带** Min 9 / Super 12  
普通 DamageFactorBonus +0.05；超级 +0.10。碎片各 +0.0125。S5 +0.0125；S6 BossDamage +3%。

**A13 广角镜** Min 3 / Super 6  
普通：开启 Bolt/Spread/Barrage/DoTBind 追踪，Turn=0.12，菱形索敌 **16 格**。  
超级：同上 Turn=0.20，索敌 **32 格**，并 +5% DamageBonus（附加价值）。  
S1 只开 Bolt，Turn=0.06，索敌 8 格；S2 开 Spread，8 格；S3 开 Barrage，8 格；S4 开 DoTBind，8 格；S5 Turn max 0.08 + Bolt + 8 格；S6 DamageBonus +1.5%（无索敌）。  
多件 Turn 与索敌格数取 **max**；Enable 集合 **并**。  
追踪手感（叶绿弹式）：新锁须在**当前速度方向 60° 半角**内（出生帧即朝鼠标，不会锁身后）；锁死后可掉头追；曼哈顿距离超过索敌 **2×** 断锁；会撞墙的弹新锁要 `CanHit`。

**A14 讲究头带** Min 4 / Super 7  
普通：锁技能2+大招，ChoiceDamage +50%。  
超级：仍锁两项，ChoiceDamage +100%。  
S1 ChoiceDamage +12.5% 不锁；S2 +12.5% 不锁；S3 锁技能2；S4 锁大招；S5 +12.5%；S6 +12.5%。  
锁是 OR：只戴 S3 仍能放大招。普通/超级锁两项。

**A15 焦点镜** Min 5 / Super 8  
普通 CritUpgrade +10%；超级 +20%。碎片各 +2.5%。S5 +2.5%；S6 EasyCrit 再 +5%（加到现有 EasyCrit 升档池）。

**A16 生命宝珠** Min 5 / Super 8  
普通：LifeOrbDamage +20% + 扣 1HP。超级：+40% + 扣 1HP（扣血不 ×2，附加价值改成闸门 8 tick→4 tick）。  
S1 +5% 不扣血；S2 +5% 不扣血；S3 +5%；S4 开启扣血；S5 +5%；S6 +5%。  
扣血 OR。闸门：有 Super 则 4 tick，否则 8。

**A17 贝壳之铃** Min 4 / Super 7  
普通回 2HP / 30 tick；超级 4HP / 15 tick。  
S1 +1HP 30tick；S2 +1HP；S3 CD 仍 30 但 +0（占位改：S3 使 CD 25）；S4 +0 且 CD 25；普通并集 2HP/30。实现：Heal 加法，CD 取已装备件里的 **最短**。  
S5 +1HP；S6 CD 20。

**A18 凸凸头盔** Min 6 / Super 9  
普通反伤 1.0× 武器伤 / 45 tick；超级 2.0× / 22 tick。  
S1 0.25×；S2 0.25×；S3 0.25×；S4 0.25×；S5 0.25×；S6 CD 缩短。Scale 加法，CD 最短。

**A19 充电电池** Min 3 / Super 6  
普通 EnergyGainMul ×1.30（即 +30%）；超级 ×1.60。用加法 EnergyGainAdd，普通 +0.30，超级 +0.60，碎片 +0.075。S5 +0.075；S6 PassiveEnergyMul +0.25。

**A20 光之黏土** Min 6 / Super 9  
普通 UltRetain max 0.20；超级 0.40。碎片各 max 0.05。多件 **max**（不是加到 1.2）。S5 0.05；S6 大招后 3s 冷却 Cut +2%（Timer）。

**A21 弱点保险** Min 7 / Super 10  
普通 UltDamage +25% 且 EnergyGainMul ×0.80。超级 Ult +50% 且 ×0.60。  
能量惩罚用乘法，多件连乘。  
S1 Ult +6.25% 无惩罚；S2 Ult +6.25%；S3 Ult +6.25%；S4 EnergyGain ×0.80（整份惩罚在 S4，避免四次 0.8 叠死）；S5 Ult +6.25%；S6 EnergyGain ×0.90。  
普通表：Ult +25% + ×0.80（不要按碎片再乘一遍）。

### 6.2 共鸣

共鸣不满足：整件（含碎片）Apply 跳过。超级同样要共鸣。

**A07 木炭** 火 Min 2 / Super 5  
普通：着火目标 +8%。超级 +16%。  
S1 OnFire +2%；S2 火系招式（`CountsAsFireMove`）DamageBonus +2%；S3 CritUpgrade +2%（仅着火目标，WP-D 在 hit 里判）；S4 OnFire +2%；S5 OnFire +2%；S6 火系 +2%。  
普通并集：OnFire +8%（S2/S3 的附加轴也写入普通：火系 +2%、着火升暴 +2%）。超级各项 ×2。

**A08 神秘水滴** 水 Min 2 / Super 5  
普通水中移速 +10%；超级 +20%。碎片 +2.5%。S5 +2.5%；S6 水中再 FlightEnergy +0.5s。

**A09 磁铁** 电 Min 4 / Super 7  
普通 DashCooldown ×0.85（Cut 15%）；超级 Cut 30%。加法 DashCut。碎片 Cut 3.75%。S5 3.75%；S6 冲刺距离/速度 +1（`TryDash` 里速度 12→12+bonus）。

**A10 锐利鸟嘴** 飞 Min 5 / Super 8  
普通坠落承伤比例 0.25（减 75%）；超级 0.10。`FallDmgReductionSet` 取最小。  
S1 0.80；S2 0.80；S3 0.70；S4 0.70；普通写 0.25。S5 移速 +2%；S6 飞行 +0.5s。

**A11 诅咒之符** 幽灵 Min 6 / Super 9  
普通：Bolt/Spread/Barrage/DoTBind/**Beam** 穿墙。  
超级：同上 + 穿透 +1（`Projectile.penetrate`：若 >0 且 !=-1 则 +1，OnSpawn 做）。  
S1 只 Bolt 穿墙；S2 Spread；S3 Barrage；S4 DoTBind；S5 Beam；S6 penetrate +1。  
普通并集含 S1–S4+应对齐「与广角镜同类 + Beam」→ 普通应 **含 Beam**。把 Beam 放进普通并集（S5 单独戴也能开 Beam；普通成品直接含 Beam，即使配方是 S1–S4）。**普通表显式包含 Beam 穿墙**，不依赖 S5。

**A12 龙之牙** 龙 Min 8 / Super 11  
普通 Boss +6%；超级 +12%。碎片 +1.5%。S5 +1.5%；S6 DamageBonus +2%（通用）。

**A25 黑带** 格斗共鸣 **或** 通用？用户要近战件。定为 **通用**（近战交付就加，不要求格斗形态，否则喷火龙爪击吃不到）。Min 3 / Super 6  
普通：MeleeDeliveryDamage +12%，LungeCooldownMul ×0.85。  
超级：+24%，×0.70。  
S1 近战 +3%；S2 近战 +3%；S3 LungeCut 7.5%；S4 LungeCut 7.5%；S5 近战 +3%；S6 撞击命中短暂immune +5 tick（与现有 Lunge 15 tick 取 max）。

### 6.3 经验 / 进化 / 生存（新）

**A22 学习装置** Min 2 / Super 5  
普通：其它热键栏之力各复制 **40%** 击杀 XP（持握那只仍 100%）。超级 **80%**。  
碎片各 10%。S5 10%；S6 持握那只再 +10%（与幸运蛋叠）。  
扫描 `inventory[0..9]`，跳过 `selectedItem`，跳过非 `HenshinForceItem`。服务端 `TryAddExperience` 每只各写；弹出只显示持握那只的 EXP（避免 9 个字）。可选：其它格物品 inv 闪光 1 tick（不必）。

**A23 幸运蛋** Min 1 / Super 4  
普通持握 XP +50%；超级 +100%。碎片 +12.5%。S5 +12.5%；S6 +12.5%。与学习装置的 S6 加法叠。

**A24 不变之石** Min 1 / Super 4；**WorksUntransformed**  
S1–S6 / 普通 / 超级：皆 `EverstoneBlock` OR。  
附加：普通 IncomingCut 2%；超级 IncomingCut 4% + Eviolite 式「未进化再 +6% 防」（与 A28 叠）。碎片各 IncomingCut 0.5%。  
超级附加价值不是「双倍阻止」。

**A26 吃剩的东西** Min 3 / Super 6  
普通 1 HP / 秒（60 tick 1 点）；超级 2 HP / 秒。与贝壳之铃 **分开发放**，但若同一 tick 都要奶，贝壳走命中、剩饭走 PostUpdate。剩饭不触发生命宝珠。  
S1 0.25/s（实现：每 240 tick +1，或多件累加到 float 池每 60 tick floor）。用 `LeftoversHpPerSec` 浮点，每 60 tick `Heal(floor(sum))` 余数留存。  
S5 +0.25/s；S6 低于 50% HP 时再 +0.25/s。

**A27 气势披带** Min 4 / Super 8  

门槛是「当前生命 **≥ max × 比例**」时，本击若将致死则留 1HP。比例越低越强（残血也能撑）。

| 件 | 血量门槛 | CD | 其它 |
|----|----------|----|------|
| 碎片 S1–S6 | **≥ 60%** | 90s | 均带致死留 1 |
| 普通成品 | **≥ 50%** | **60s** | IncomingCut 4% |
| 超级 | **≥ 30%** | **30s** | IncomingCut 8% + 触发后 1s 无敌 |

多件同族叠：门槛取 **min**（戴超级则以 30% 为准），CD 取 **min**。  

S1 IncomingCut 2%；S2 IncomingCut 2%；S3–S6 无额外汇入减伤，但仍有 60% 门槛的留 1。普通并集含 S1–S2 的 Cut 4%。  
实现见 §9.1。必须服务端。

**A28 进化奇石** Min 4 / Super 7  
普通：当前形态 `FindEvolutionOf != null` 时 FinalDefense 乘区 +20%（变身防御）。超级 +40%。  
无下一阶段（喷火龙、超梦等）→ 0。  
碎片各 +5%。S5 +5%；S6 未进化时 DamageBonus +3%。  
接到 `HenshinPlayer` 变身防御加法处（现 FinalDefense 加上之后再 `* (1+Eviolite)`）。

---

## 7. 获取与合成

### 7.1 规则

- **废除**现役所有「落星+单材料 → 成品」配方。
- 普通：`S1+S2+S3+S4` @ `TinkerersWorkbench`。
- 超级：`普通+S5+S6` **以及** `S1+S2+S3+S4+S5+S6`（两条都要）。
- 不消耗额外金币。工作站：普通工匠、超级秘银砧（MinStage≥5）或精金（≥8）——按 `SuperMinStage`：≤6 工匠，7–9 秘银，≥10 叶绿/远古操纵机。
- F1 以 **合成** 为主（主题矿/材料），保证能开荒。
- F2 **钓鱼匣**（主题匣，含困难模式匣）。
- F3 **事件**敌或事件 Boss。
- F4 **主题 Boss**（对齐 MinStage）。
- F5 更高一档矿 **或** 下一档 Boss。
- F6 再下一档 Boss / 晚事件。
- 掉率（非合成）：匣 **10%**；事件小怪 **2%**（每只，需 `player.RollLuck`）；Boss **25%**（专家袋可再 Roll 一次，用 `npc.boss` / `IsBossForXp`）。掉的是该 Boss **表内对应的那一枚碎片**，不是随机饰品、也不是直接掉普通成品。多节 Boss 只在最终节结算。多人掉在 Boss 处，归属 `lastInteraction`。
- 灾厄 Boss：`NPC.FullName` / `ModNPC.Mod == Calamity` + `npc.boss`，用 Catalog 里写的 **内部名字符串** `CalamityMod/DesertScourgeHead` 等，`ModContent.TryFind<ModNPC>` 缓存 type。找不到则跳过（无灾厄时原版路径仍在）。

### 7.2 匣子 ID（原版）

实现用 `ItemID.*Crate` / `ItemID.*CrateHard`。1.4.4 有：Wooden, Iron, Golden, Jungle, Sky, Corrupt, Crimson, Hallowed, Dungeon, Oasis, Frozen, Ocean, Lava（黑曜石）, 以及 Pearlwood/Mythril/Titanium 等困难匣。钓鱼掉落：`ModPlayer.CatchFish` 或 `ItemLoader` 开匣 `RightClick`/`OpenVanillaBag`——**用 `ModItem.CanRightClick` 不行**。正确钩子：`GlobalItem.OpenCrate`（tML `ModifyItemLoot` + `ItemID.Sets.IsFishingCrate`）或 `GlobalItem.OnConsumeItem`。WP-F 查 [ModItem / GlobalItem](https://docs.tmodloader.net/docs/stable/annotated.html) 的 crate loot API：优先 `ModifyItemLoot` + `ItemLoot.Add` 条件匣。

### 7.3 每家族来源（主题浅关联）

格式：S1 / S2 / S3 / S4 / S5 / S6。

| 家族 | S1 合成 | S2 匣 | S3 事件 | S4 Boss | S5 | S6 |
|------|---------|-------|---------|---------|----|----|
| A01 特性胶囊 | 生命水晶×1 + 落星×1 @工作台 | 木匣 | 哥布林战旗兵 | 史莱姆王 | 金/铂矿×8 合成 | 蜂后 |
| A02 力量头带 | 脚镣×1 + 石块×20 | 铁匣 | 血月脸怪/血僵尸 | 克眼 | 地狱石×5 合成 | 世界恶 Boss（脑/虫） |
| A03 心之水滴 | 魔力水晶×1 + 坠落星×3 | 天空匣 | 流星头 | 骷髅王 | 光明之魂×5 | 史莱姆神或肉山 |
| A04 轻石 | 羽毛×10 + 云块×20 | 天空匣 | 鸟妖 | 骷髅王 | 羽落药水材料 + 魂 | 双足翼龙 |
| A05 气势头带 | 绷带×1 + 生命水晶 | 地牢匣 | 日食 | 肉山 | 神圣锭×5 | 世纪之花 |
| A06 达人带 | 复仇者徽章 **不可作材料（循环）** → 叶绿×5 + 魂×10 | 神圣匣 | 火星入侵 | 月亮领主 | 星旋碎片×6 | 亵渎天神 |
| A07 木炭 | 地狱石锭×3 | 熔岩匣 | 日食（噬魂怪） | 肉山 | 硫磺火相关：困难匣或灾厄硫磺叶 | 灾厄之影 |
| A08 神秘水滴 | 珊瑚×8 + 贝壳 | 海洋匣 | 海盗 | 克眼（雨天击杀不强制） | 利维坦（灾厄）否则猪鱼公爵 | 猪鲨 |
| A09 磁铁 | 电线×20 + 铁锭×5 | 金匣 | 火星器械 | 机械骷髅王 | 电线×50 + 圣锭 | 星流前置：星神游龙或月总 |
| A10 锐利鸟嘴 | 巨型鸟妖羽毛×1 | 天空匣 | 日食飞龙 | 双足翼龙 | 魂×8 + 羽毛 | 石巨人 |
| A11 诅咒之符 | 暗影之魂×5 + 腐肉/椎骨 | 腐化/猩红匣 | 日食 | 世界恶 Boss | 世花 | 幽花（灾厄）否则月总 |
| A12 龙之牙 | 力量之魂×5 + 魂之汤？ → 神圣锭×6 | 神圣匣 | 日食 | 任一机械 | 石巨人 | 犽戎否则月总 |
| A13 广角镜 | 透镜×5 | 木匣/铁匣 | 哥布林弓手 | 克眼 | 黑透镜×1 合成 | 机械一王 |
| A14 讲究头带 | 再生手环×1 + 黑带材料皮革（皮革×5） | 铁匣 | 哥布林战士 | 蜂后 | 战士徽章 | 世花 |
| A15 焦点镜 | 黑透镜×1 + 透镜×3 | 地牢匣 | 日食 | 骷髅王 | 机械眼（克脑掉的那种物品）×1 | 月总 |
| A16 生命宝珠 | 生命水晶×3 + 红心 | 金匣 | 血月 | 肉山 | 生命果×3 合成 | 世花 |
| A17 贝壳之铃 | 贝壳×5 + 珊瑚 | 海洋匣 | 海盗 | 克眼 | 利维坦/猪鲨 | 老公爵否则月总 |
| A18 凸凸头盔 | 石块×50 + 骨头×10 | 地牢匣 | 哥布林 | 骷髅王 | 海龟甲×1 | 石巨人 |
| A19 充电电池 | 电线×30 + 星星×3 | 金匣 | 火星 | 机械毁灭者 | 圣锭×5 | 星流/月总 |
| A20 光之黏土 | 魔力水晶×2 + 土块×50 | 神圣匣 | 妖精（南瓜月） | 世花 | 灵能锭（叶绿）×5 | 月总 |
| A21 弱点保险 | 恐惧之魂×5 | 神圣匣 | 南瓜月 | 石巨人 | 月总 | 亵渎 |
| A22 学习装置 | 书×5 + 骨头×10 @书架 | 地牢匣 | 日食（模仿者） | 骷髅王 | 叶绿×5 | 幻灵/月总 |
| A23 幸运蛋 | 金鸟/金青蛙 **或** 太阳花×5 + 坠落星×5 | 丛林匣 | 史莱姆雨（史莱姆王额外） | 蜂后 | 世花 | 龙嵩否则丛林匣困难 |
| A24 不变之石 | 石块×99 + 琥珀×1 | 花岗岩/大理石：金匣 | 花岗岩元素 | 大岩石怪（Onix 掉落 Boss 用骷髅王） | 石巨人 | 亵渎 |
| A25 黑带 | 皮革×5 + 脚镣 | 铁匣 | 哥布林战士 | 蜂后 | 战士徽章 | 世花 |
| A26 吃剩的东西 | 任意食物 `ItemID.CookedFish`×3 + 碗 | 绿洲匣 | 感恩节火鸡？→ 史莱姆雨 | 史莱姆王 | 猪鲨 | 转轮王否则海盗 |
| A27 气势披带 | 绷带×2 + 生命水晶 | 血匣：腐化匣 | 血月 | 肉山 | 石巨人 | 月总 |
| A28 进化奇石 | 陨石锭×5 + 生命水晶 | 金匣 | 流星 | 鹿角怪/史莱姆神 | 叶绿×8 | 幽花否则世花 |

WP-F 把上表译成 `ItemID` / `NPCID` / 灾厄内部名。灾厄名用字符串，失败则该格只保留原版后备（表中「否则」）。

**S4 与现有之力 Boss 掉落并存**，不要删皮卡丘掉落。

### 7.4 开匣实现注意

一人写 `HenshinAccLoot`：`ModifyItemLoot` 对每个 crate type 加 `ItemDropWithConditionRule`。不要在 `CatchFish` 里直接给成品。

---

## 8. 贴图脚本（WP-H，已落地 2026-09-10）

### 8.1 `tools/fetch_assets.py`

`ACC_FILES`：A01–A28 的 `Bag {官方名} SV Sprite.png` → 写入 `Assets/Accessories/`（再拷/备份到 `_src_hires/`）。失败则 fallback `Bag {名} Sprite.png`。

### 8.2 `tools/pixelize_accessories.py`（现役）

依赖：`pip install pixeloe pillow numpy opencv-python-headless`。

输入优先 `Assets/Accessories/_src_hires/Axx.png`，输出同目录：

| 文件 | 内容 |
|------|------|
| `Axx.png` | pixeloe 对比感知像素化，短边约 56 再 pad 到 **64×64** |
| `Axx_Super.png` | 自普通图重建：加亮 + **多层金边/外发光** + 确定性十字闪点（种子=`Axx`） |
| `Axx_Shard.png` | 自普通图裁不规则晶体剪影 + 边缘压暗（种子=`shard:Axx`） |

**禁止**给 legacy pixeloe 传 `contrast=` / `saturation=`（会把 uint8 压成近黑）；色调用 PIL 后处理。

重跑：`python tools/pixelize_accessories.py`。`make_super_accessory_sprites.py` 仅作转发兼容。

### 8.3 运行时贴图路径

`HenshinAccItem.Texture`：Super → `_Super`；S1–S6 → `_Shard`；普通 → `Axx`。库存/世界仍画片号角标区分六片。缺文件时 fallback `A01` 并 `Logger.Warn` 一次。

---

## 9. 特殊挂钩实现要点（WP-D / E）

### 9.1 气势披带

`ModifyHurt`：仅变身；`FocusSash` 真；`FocusSashCd==0`；`statLife >= ceil(statLifeMax2 * FocusSashHpPct)`（碎片 0.60 / 成品 0.50 / 超级 0.30，多件取 min）；本次伤害将导致 `statLife - Incoming <= 0`。则 `modifiers.SetMaxDamage(statLife-1)` 或等价 tML 1.4.4 API（查 `HurtModifiers`）。然后 CD = 已装备最短秒×60。仅当装备超级时 `Player.immuneTime = max(..., 60)`。

### 9.2 不变之石

`HenshinAccItem.UpdateAccessory`：**先** `if (WorksUntransformed) Apply`，**再** `if (!IsTransformed) return`。  
`EvolutionService.MeetsTrigger` 开头：若玩家任一栏饰品（`player.armor[3..]` + 灾厄额外栏走 `Player.IsItemSlotUnlockedAndUsable` / 标准 `UpdateEquips` 已 Apply 的 flag）`EverstoneBlock`，return false。  
`EvolutionConfirmUI`：flag 真则不弹出；`/henshin evolve` 提示被石头阻止。

饰品栏遍历不要自己扫错：用 Apply 里设的 `hp.EverstoneBlock`（未变身也 Apply）。

### 9.3 学习装置 / 幸运蛋

`GrantKillExperience`：

```text
int held = amount * (1 + XpHeldMul)
force.TryAddExperience(held)
if (XpHotbarShareMul > 0)
  for i in 0..9 where i != selectedItem
    if inventory[i].ModItem is HenshinForceItem other
      other.TryAddExperience(round(amount * XpHotbarShareMul))
SyncForceProgress 持握 + 有变化的其它格
```

满级截断已有。

### 9.4 诅咒之符穿透

OnSpawn：`penetrate>0 && penetrate!=-1` 才 `+= AccPenetrateAdd`。无限穿透不改。

### 9.5 广角镜与写死 Homing

`HomingAI(proj, homing || acc, max(turn, accTurn))` 其中 acc 仅当 Delivery 在集合内。招式写死 `true` 时即使无饰品也追。

索敌为**曼哈顿菱形**（`|dx|+|dy|`），格数来自饰品 `HomingRange`（碎片 8 / 成品 16 / 超级 32，叠戴 max）。无饰品的自带追踪默认 30 格。自带追踪与饰品格数取 max，避免碎片削短。

新锁：当前速度方向 **60° 半角**（`dot ≥ 0.5`）+ 菱形半径内最近；`tileCollide` 时还要 `Collision.CanHit`。出生点在玩家、速度朝鼠标，因此不会第一帧锁背后。锁上后跟同一目标，可掉头；曼哈顿 ≥ 2× 索敌则断锁再找。

### 9.6 剩饭 vs 贝壳

贝壳：命中、短 CD。剩饭：PostUpdate 计时。可以同一秒都奶。生命宝珠扣血仍走命中闸门。

---

## 10. 本地化键

```
Items.{LegacyTypeName}.DisplayName
Items.{LegacyTypeName}.Tooltip
Items.{LegacyTypeName}_S{n}.DisplayName   // 「力量头带碎片Ⅰ」
Items.{LegacyTypeName}_S{n}.Tooltip
Items.{LegacyTypeName}_Super.DisplayName  // 「超级力量头带」
Items.{LegacyTypeName}_Super.Tooltip
Accessories.ActiveTag / InactiveTag / UntransformedActiveTag（不变之石用「戴着即生效」）
Accessories.ShardTag: 碎片 {0}/6
Accessories.RecipeHint: 合成：Ⅰ–Ⅳ→成品；成品+Ⅴ+Ⅵ或Ⅰ–Ⅵ→超级
```

碎片 DisplayName：`{官方名}碎片{中文数字一二三四五六}`。超级前缀「超级」。英文 `Super Muscle Band` / `Muscle Band Shard I`。

Tooltip 结构：官网一句 + 本片效果 + 合成提示 + 生效标签。

---

## 11. 验收清单（实现 PR 必须逐项可勾）

### 11.1 名称与图

- [x] 28 个基础 PNG 源自 52poke 袋内图（高清在 `_src_hires/`；游戏用 64×64 像素版）；文件名 A01–A28 与家族表一致  
- [x] 28 个 `_Super.png` 粗金边 + 闪点；28 个 `_Shard.png` 碎片剪影（S1–S6 共用 + 角标）  
- [ ] 中文 DisplayName = 官方名（吃剩的东西、不变之石、黑带、特性胶囊、气势头带 ≠ 披带）  
- [ ] A05 图是头带、A27 图是披带、A16 图是生命宝珠、A06 图是达人带  

### 11.2 合成

- [ ] 旧落星成品配方全部消失  
- [ ] 每家族：4 碎片→普通；普通+5+6→超级；6 碎片→超级  
- [ ] 缺 S5 不能合成超级  

### 11.3 叠加

- [ ] 同时装备 A02 S1 与 A02 普通：面板伤约为 +1.5% 与 +6% **相加**  
- [ ] 两件不同家族伤害饰品相加  
- [ ] 未变身：除不变之石外数值不加  

### 11.4 追踪 / 穿墙

- [ ] 皮卡丘电击 + 广角镜：转弯  
- [ ] 喷火龙火花 + 广角镜：转弯（重力仍在）  
- [ ] 朝鼠标开火、身后有近怪：第一帧**不**锁身后  
- [ ] 锁上后目标绕到身后：可以掉头继续追  
- [ ] 碎片 / 成品 / 超级菱形索敌约 8 / 16 / 32 格（叠戴 max）  
- [ ] 水炮/日光束 + 广角镜：**不**转弯  
- [ ] 水炮 + 诅咒之符：穿墙  
- [ ] 爪击 + 广角镜：不追踪  
- [ ] 连发子弹继承母弹 Delivery/Homing  

### 11.5 面板

- [ ] 变身 + 力量头带：之力 Tooltip 攻击数字上升  
- [ ] 龙之牙：主数字不变，另有对 Boss 行  
- [ ] 讲究头带：主数字含 +50%  

### 11.6 新件

- [ ] 学习装置：热键栏另一只之力 XP 增加；背包第 11 格不加  
- [ ] 幸运蛋：持握 XP 增加  
- [ ] 不变之石：未持握之力时进化 UI 不出现 / 服务端拒绝  
- [ ] 黑带：撞击 CD 缩短；爪击伤害提高；电击不提高  
- [ ] 吃剩的东西：站桩回血  
- [ ] 气势披带：仅碎片时 ≥60% 血致死留 1、CD 90s；仅成品 ≥50% / 60s；仅超级 ≥30% / 30s；叠件门槛与 CD 取 min；CD 内不再触发  
- [ ] 进化奇石：小火龙变身防上升；喷火龙无下一阶则不加  

### 11.7 共鸣 / 破例

- [ ] 木炭非火形态：未生效标签、无着火加成  
- [ ] 不变之石非变身：仍挡进化  

### 11.8 构建

- [ ] `bash tools/build-mod.sh` 通过  
- [ ] 无头加载仍因缺灾厄报 TML 缺模（合法）  
- [ ] `dotnet run --project tools/HenshinStatVerify` 仍过（若未改公式）  

游戏内手感验收 Cloud 做不到，计划在 PR 描述标「需本地」。

---

## 12. 工作包与 subagent 边界（防止互相打脸）

**主 Agent：** 搭脚手架（§2 文件空壳 + Loader 能 AddContent 224 件占位效果 0）→ 合到同一分支 → **再** 开 subagent。每个 subagent 只改「独占」列。主 Agent 负责合并、冲突、跑 build。

| ID | 标题 | 独占文件 | 可读不可改 | 完成定义 |
|----|------|----------|------------|----------|
| WP-A | 脚手架 | 2.3 表中 Accessories 新文件、删除旧 21 class、csproj 无需改 | Player/ForceItem 只留 TODO 注释 | 224 物品在创造菜单出现；戴上无效果不崩 |
| WP-B | Delivery+弹幕 | MoveSpec、FormItemUtil 工厂、HenshinForceItem.FireMove、IHenshinMoveProj、GlobalProjectile、扫 NewProjectile source | Catalog 只读 | 11.4 逻辑在代码层完备；工厂无 Delivery 漏网 |
| WP-C | 填 Catalog | **只** `HenshinAccCatalog.cs` | 其它 | 28 家族数字/掉落/合成/贴图路径按 §6–§7 填完 |
| WP-D | Player 运行时 | HenshinPlayer、HenshinForceItem 的 ModifyWeaponDamage/Tooltip | Catalog 只读 | Apply 全 Stat；Reset；披带/剩饭/宝珠/头盔/选择锁 |
| WP-E | 进化+XP | EvolutionService、EvolutionConfirmUI、GrantKillExperience、HenshinNet 若需同步多格 XP | Player 字段只读使用 | 11.6 学习装置/蛋/石头 |
| WP-F | 掉落合成 | HenshinLoot.cs（删旧配方）、HenshinAccLoot.cs | Catalog 只读 | 11.2；匣/Boss/事件按 §7 |
| WP-G | 本地化 | 两个 hjson | Catalog 官方名 | 键齐、无缺 DisplayName |
| WP-H | 贴图脚本 | tools/*.py、生成 PNG 提交仓库 | — | 11.1；脚本幂等 |

**并行允许：** WP-B ∥ WP-C ∥ WP-H（脚手架之后）。WP-D 依赖 A+C。WP-E 依赖 D 字段。WP-F 依赖 C。WP-G 依赖 C 的 Name 表。  
**禁止并行改同一文件。** HenshinPlayer 只有 WP-D 写。

### 12.1 给 subagent 的固定前言（每人都带）

```
仓库：Pokemon Henshin，内部名 PokemonHenshin。
施工图：docs/accessory-rework-plan.md —— 数字与枚举以该文件为准，不要发明。
tML API：https://docs.tmodloader.net/docs/stable/annotated.html 按需打开 ModItem/ModPlayer/GlobalItem/HurtModifiers。
禁止：改 CalamityOverhaul；新增 FX 图；在仓库根 dotnet build（用 tools/build-mod.sh）。
禁止：改你独占列表以外的文件。需要新字段先停下来写在计划 AccStat 表，不要私加。
叠：同族碎片/普通/超级全部生效，不要写互斥。
不变之石：未变身也 Apply。
广角镜集合不含 Beam；诅咒之符含 Beam。
```

### 12.2 脚手架验收（主 Agent 做完再开人）

- [ ] `AccFamilyId` 28 个  
- [ ] Loader 注册 224 `HenshinAccItem`  
- [ ] 创造栏能搜「力量头带」占位  
- [ ] `HenshinPlayer` 已有 §2.5 全部字段并 Reset  
- [ ] `ApplyAccStat` 空实现或 switch 全分支 stub  
- [ ] 旧 `HenshinAccessories.cs` 已删且旧 Name 仍指向普通件  
- [ ] build-mod 通过  

---

## 13. 对标强度（给填表时的手感，不是 DPS 窗）

| 进度 | 灾厄近似 | 本模普通件 |
|------|----------|------------|
| 肉前 | 早期徽章 5–8% 类伤 | 力量头带 +6% |
| 机械后 | 复仇者 12% 全伤 | 超级力量头带 +12% |
| 世花–石巨人 | 毁灭者 10%+8% 暴 | 焦点镜 10% 升档（更稀有） |
| 月总后 | 多种 10–20% 包 | 达人带 +5% factor 叠种族 |

讲究头带 +50% 锁技能是抉择件，超级 +100% 仍锁，对标「剪选项换爆发」。栏位够就可以碎片+成品再叠，这是玩家用栏位买的，不削。

---

## 14. 风险与实现顺序

1. **224 物品创造栏噪音：** 用 `CreativeItemSacrificesCatalog` 和研究分组；`ModSide` 正常。可 `Item.SetNameOverride` 不需要。考虑 `ContentSamples` 无特殊处理。  
2. **冷却乘法叠爆：** A01 用 Cut 加法封顶 0.20。  
3. **EnergyGain 连乘：** A21 S4 整份惩罚只在成品/S4，避免四碎片全是 ×0.8。  
4. **气势披带 vs 原版神级：** 不再要求满血；碎片 60% / 成品 50% / 超级 30% + CD。残血更能撑的是超级，碎片只在较健康时触发。  
5. **学习装置联机：** 只同步发生变化的格子，避免 10 格全量。  
6. **旧配方世界：** 合成表刷新即可；已造出的旧成品变成更名后的普通件。

**顺序：** 审阅通过 → WP-A 脚手架 + 开 PR → WP-H 贴图（可并行）→ WP-B + WP-C 并行 → WP-D → WP-E + WP-F + WP-G → 主 Agent 勾验收 → 回写 requirements §6/§10 → 删除或归档本文件。

---

## 15. 请审阅时重点看的开放点（已尽量定死，仅确认）

下列是本计划里的 **推荐默认**，若你不改就按此做：

1. A06→达人带、A19→充电电池、A20→光之黏土、A21→弱点保险（原作效果泰拉化，不是 1:1）。  
2. 「剩饭」DisplayName 用官网 **吃剩的东西**；「不变石」用 **不变之石**。  
3. 黑带 **不要求格斗共鸣**（所有近战交付都吃）。  
4. 诅咒之符 **普通成品含 Beam 穿墙**（S1–S4 配方仍能做出带 Beam 的成品）。  
5. 学习装置复制 **40% / 超级 80%**；幸运蛋 **+50% / 超级 +100%**。  
6. 气势披带血量门槛：**碎片 ≥60% / 成品 ≥50% / 超级 ≥30%**（已按此修订；叠件取 min）。

回一句「按计划执行」或点名要改的行号即可开工。
