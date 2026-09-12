# Pokemon Henshin — 饰品现役手册

| 项 | 内容 |
|----|------|
| 角色 | 锁定决策、命名、Delivery、掉落/合成、特殊挂钩（**现役**） |
| 效果数字 | **`Content/Accessories/HenshinAccCatalog.cs`** + `docs/requirements.md` §6 / §10 |
| 产品规则 | requirements §6 / §10 |
| 来源 | 自原饰品施工图抽出；含已删 AccStat 旧表的全文仅人类可查 `docs/archive/`（**Agent 勿打开**） |

---

## 0. 已锁定决策

1. **学习装置范围：** 仅热键栏 **0–9**。不含背包 10–49、鼠标、猪猪、保险箱、银行、地上物。  
2. **同族不互斥：** 碎片 / 普通 / 超级是不同 `Item.type`，可同时装备并全部生效。  
3. **超级 2×：** 倍率类 ×2；冷却类减半；开关类保持并给附加价值。目录里 Super 数字已是 2×，运行时不再乘。  
4. **体量：** A01–A28 各 6 碎片 + 普通 + 超级；另 **A29 电气球**（仅 `L04_F01` 皮卡丘）。  
5. **不变之石：** 未变身也挡进化。**诅咒之符灼烧**未变身也生效；穿墙仍仅变身。  
6. **贴图：** `tools/fetch_assets.py` → `_src_hires/`；`tools/pixelize_accessories.py` → 64×64 `Axx` / `_Super` / `_Shard`。S1–S6 共用 `_Shard` + 片号角标。  
7. **广角镜 / 诅咒之符：** 共用 `MoveDelivery`；广角镜 **不含 Beam/Field**；诅咒符穿墙含 Beam、**不含 Field**。暴风是 Barrage 穿透弹。  
8. **无条件加伤**进之力 `ModifyWeaponDamage`（面板可见）。有条件走命中。  
9. **饰品伤不对齐**之力 DPS 80–120% 窗。  
10. **仅变身生效**（除不变之石、诅咒焰）。

### 明确不做

- 饰品互相合成成新件；饰品自身升级 / 吃经验  
- 赶进度腰带、进化催促、给敌人打宝可梦属性、未变身强力隐藏效果  
- 改 CalamityOverhaul；编译期不 `using CalamityMod`

---

## 1. 官方名与贴图（DisplayName）

名称与袋内图以 [52poke 道具列表](https://wiki.52poke.com/wiki/道具列表) 为准。内部 `ModItem.Name` 保持稳定映射（A01–A21 旧 Name 指向普通成品；碎片 `_S1`…`_S6`；超级 `_Super`）。

| 家族 | 官方中文 | 英文 | 备注 |
|------|----------|------|------|
| A01 | 特性胶囊 | Ability Capsule | |
| A02 | 力量头带 | Muscle Band | |
| A03 | 心之水滴 | Soul Dew | |
| A04 | 轻石 | Float Stone | |
| A05 | 气势头带 | Focus Band | |
| A06 | 达人带 | Expert Belt | |
| A07 | 木炭 | Charcoal | 火共鸣 |
| A08 | 神秘水滴 | Mystic Water | 水共鸣 |
| A09 | 磁铁 | Magnet | 电共鸣 |
| A10 | 锐利鸟嘴 | Sharp Beak | 飞共鸣 |
| A11 | 诅咒之符 | Spell Tag | 穿墙 + 灼烧 |
| A12 | 龙之牙 | Dragon Fang | 龙共鸣 |
| A13 | 广角镜 | Wide Lens | |
| A14 | 讲究头带 | Choice Band | |
| A15 | 焦点镜 | Scope Lens | |
| A16 | 生命宝珠 | Life Orb | |
| A17 | 贝壳之铃 | Shell Bell | |
| A18 | 凸凸头盔 | Rocky Helmet | |
| A19 | 充电电池 | Cell Battery | |
| A20 | 光之黏土 | Light Clay | |
| A21 | 弱点保险 | Weakness Policy | |
| A22 | 学习装置 | Exp. Share | 热键栏分经验 |
| A23 | 幸运蛋 | Lucky Egg | |
| A24 | 不变之石 | Everstone | 未变身也生效 |
| A25 | 黑带 | Black Belt | |
| A26 | 吃剩的东西 | Leftovers | |
| A27 | 气势披带 | Focus Sash | |
| A28 | 进化奇石 | Eviolite | 有下一形态时加防 |
| A29 | 电气球 | Light Ball | 仅皮卡丘 |

运行时贴图：`PokemonHenshin/Assets/Accessories/Axx`（Super / Shard 后缀）。缺文件 fallback `A01` 并 Warn 一次。

---

## 2. 架构要点

- **数据驱动：** 数字 / 拆分 / 掉落 / 合成 / 图 / 官方名只在 `HenshinAccCatalog`。  
- **物品：** 通用 `HenshinAccItem` + Loader `AddContent`；禁止 200+ 手写 class。  
- **新效果：** 先扩 `AccStat` 枚举 + `HenshinPlayer.ApplyAccStat`，再写 Catalog。**不要**从归档施工图中部旧 AccStat 表抄数字或已删字段（`AffinityAmp`、`FallDmgReductionSet`、`GuardCut*`、`TilePierceField`、`AccActive` 等已退役）。

---

## 3. `MoveDelivery` 与饰品作用集

```text
None, MeleeArc, Lunge, StrikeFall, Bolt, Spread, Barrage,
Beam, AoEBurst, DoTBind, Field, Dig, Blink
```

| 饰品 | 集合 |
|------|------|
| 广角镜追踪 | `{ Bolt, Spread, Barrage, DoTBind }` 不含 Beam/Field |
| 诅咒之符穿墙 | 上表 + Beam，不含 Field |
| 黑带短距伤 | `{ MeleeArc, Lunge, StrikeFall }` |
| 黑带撞击 CD | `Lunge` 或 `RequiresLungeCooldown` |

招式自带索敌（`InherentHoming` / 出生已 Homing）**不吃**广角镜。广角镜圆形索敌：碎片 8 / 成品 16 / 超级 32 格；新锁 60° 半角；断锁 2×（需求 §6）。

工厂 → Delivery 以代码 `FormItemUtil` 为准；子弹须继承母弹 `Delivery` / Homing / 穿墙标（`HenshinAccGlobalProjectile.OnSpawn`）。

---

## 4. 获取与合成

- 普通：`S1+S2+S3+S4` @ 工匠。  
- 超级：`普通+S5+S6` **以及** `S1…S6` 两条都要。  
- 掉率：匣 **10%**；事件小怪 **2%**；Boss **25%**（专家袋可再 Roll）；掉的是表内**对应碎片**。  
- 每家族 S1–S6 主题来源（矿/匣/事件/Boss）以实现 Catalog / `HenshinDropDatabase` 为准；施工史对照见归档（Agent 勿读）。  
- 与之力 Boss 掉落并存，勿删形态掉落。

贴图脚本：`tools/fetch_assets.py`、`tools/pixelize_accessories.py`。

---

## 5. 特殊挂钩（实现锚点）

| 饰品 | 要点 |
|------|------|
| 气势披带 | `ModifyHurt`；HP% 门槛 + CD；仅变身 |
| 不变之石 | 未变身也 Apply → `EverstoneBlock`；挡进化 UI / 命令 |
| 学习装置 / 幸运蛋 | `GrantKillExperience`：热键栏分享 / 持握倍率 |
| 诅咒之符 | 变身穿墙按 Delivery；灼烧戴上即烧、每 30s；超级免疫 |
| 广角镜 | `ApplyAccessoryHoming`；自带追踪整段跳过 |
| 剩饭 vs 贝壳 | 剩饭持续回血；贝壳命中回复（共用相关 CD 规则见 Catalog） |
| 电气球 | `RequiredFormId = L04_F01`；`FormAtkMul` / `FormDefMul` / `UseTimeMul` |

---

## 6. 本地化

物品 DisplayName / Tooltip 键随 `HenshinAccItem` 内部名；生效标签、未生效提示与 requirements §6 对齐。中文值若以 `{`/`[` 开头须双引号。
