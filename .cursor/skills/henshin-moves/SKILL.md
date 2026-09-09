---
name: henshin-moves
description: >-
  Iterates Pokemon Henshin move/skill/ultimate combat logic and VFX for this
  Terraria+tModLoader+Calamity mod. Use when designing or changing skills,
  ultimates, FormItemUtil factories, Wave2/Shared/Redesigned move projectiles,
  HenshinFxDraw, move FX polish, or when the user asks to improve a move's feel
  or visuals. Read Pokemon move meaning and Terraria projectile IDs first, propose
  an expected-effect design for user confirmation, then implement.
---

# Henshin Moves — 招式迭代

启发用工作流：先想清「玩家应看见/感到什么」，再选泰拉积木实现。不规定唯一写法；鼓励举一反三。

## 现役真相（短指针，勿复制成第二套表）


| 权威                               | 管什么                                                  |
| -------------------------------- | ---------------------------------------------------- |
| `docs/requirements.md`           | 产品规则（含 v1.4 等级/攻防/能量/进化双条件）                          |
| `docs/balance-stats.md`          | **数值数字权威**（已接线 `HenshinStatService`）：等级带、经验、攻防、能量、MoveRefRate |
| `docs/move-effects.md`           | 招式玩法语义 / 接线状态                                        |
| `docs/fx-knowledge.md`           | FX cookbook、本模已用手法、贴图与踩坑                             |
| `AGENTS.md`                      | 构建、目录、硬约束入口                                          |
| `.cursor/rules/tml-api-docs.mdc` | **全局 alwaysApply**：tModLoader stable API（类表入口见下）     |


改特效、招式或数值前先扫一眼上表对应行；缺项就地补进权威文档，不要只写在聊天里。

## 外链与只读参考（需要时再打开）

- 原版射弹 ID：[https://terraria.wiki.gg/zh/wiki/%E5%B0%84%E5%BC%B9_ID](https://terraria.wiki.gg/zh/wiki/%E5%B0%84%E5%BC%B9_ID)  
按候选 ID **有目的**查外观/AI，不要整表灌进上下文。
- 宝可梦招式含义：[https://wiki.52poke.com/wiki/%E6%8B%9B%E5%BC%8F%E5%88%97%E8%A1%A8](https://wiki.52poke.com/wiki/%E6%8B%9B%E5%BC%8F%E5%88%97%E8%A1%A8)  
做某招时打开**该招式页**，抓住「在对战里意味着什么」，再泰拉化。
- tModLoader API（stable 类表导航）：[https://docs.tmodloader.net/docs/stable/annotated.html](https://docs.tmodloader.net/docs/stable/annotated.html)  
**全局规范**见 `.cursor/rules/tml-api-docs.mdc`（alwaysApply）。设计/实现挂钩、字段、生命周期时查此站；从类表点进具体页（如同目录下 `class_mod_projectile.html`、`class_projectile.html`、`class_mod_item.html`、`class_mod_player.html`）。**有目的**打开当前用到的类，勿整表灌进上下文。
- 特效学习与贴图：`../CalamityOverhaul`（相对本仓库）  
**只读**抄逻辑 / **拷贝**贴图进 `Assets/Fx/`；**禁止** `modReferences` 大修、禁止运行时依赖 CWR、禁止改 CWR 源码。
- 比目鱼阶段伤害形状（只读）：`../CalamityOverhaul/Content/LegendWeapon/HalibutLegend/HalibutOverride.cs` 的 `DamageDictionary`；本模用有效 DPS 对齐，见 `docs/balance-stats.md`。

## 开工前必做（确认门）

在写/改实现代码之前：

1. **读语义**：52poke 该招式页（类型、类别、效果叙述、主题意象）。
2. **找积木**：射弹 ID 表 + `docs/fx-knowledge.md` 速查 +（可选）CWR 同类特效；API 行为不确定时查 tModLoader 对应类页；想几种「真生成 / 壳弹+Load / 本模自绘」组合，择优或组合。
3. **数值门（必做，柔性）：**
  - 标明槽位（技能1 / 技能2 / 大招）与 `BalanceTag`（Standard / HighFrequency / MultiHit / WideAoE / Ultimate）。
  - 估算 `MoveRefRate` 作**参考**（见 `docs/balance-stats.md` §7），结合命中难度、攻击距离、风险再定倍率——追踪远程宜低、短近战宜高。
  - **段数谨慎改动**（影响手感）；失衡时优先调单段 `DamageMultiplier` / `EnergyGainFactor`。
  - 填写拟定 `EnergyGainFactor`（大招槽为 0）。
  - **同一招式名**若出现在不同形态或技能/大招不同槽，必须分列倍率，禁止默认抄同一数字。
  - 面板伤来自 `FinalAttack`（等级带基准 × 种族 `AttackMod` × `HenshinDamageFactor`）。§7 窗用 `MoveRefRateNormalized`。
4. **写设计方案（最终预期效果）**：用玩家主观语言描述，同时当作验收标准。建议覆盖：
  - 一眼能认出是哪招（形、色、节奏）
  - 空间尺度（约几格宽/长/半径）
  - 时间感（蓄力 / 持续 / 命中消散）
  - 命中反馈（粒子、爆炸、是否穿透）
  - 明确「不要长成什么样」（例如别做成另一招的金棱镜）
  - 一句数值预期（相对标准技偏高/偏低/爆发）
5. **向用户确认**该描述是否符合预期；**未确认不开工**。用户改口则以新描述为准。

不确定语义、候选射弹、数值门或是否「降级」时：**先问用户**，不要擅自定案。

## 设计理念（举一反三）

- **语义先行，泰拉落地**：52poke 给「这招是什么」；泰拉给「用什么弹、什么绘制、什么手感」。不要机械 1:1 复刻回合制数值。
- **频次与倍率一体，段数慎动**：超模时优先降单段倍率与充能 Factor，勿为过公式砍段；大招用充能成本换爆发，可同名强化。风险高/难命中可抬倍率，安全追踪可压倍率。
- **优先可辨认的积木**：原版真弹或已验收 cookbook（Leaf / Bubble Load / Nebula 壳 / ThunderTrail…）往往比从零 Dust 更稳；CWR 是学习样本，不是依赖。
- **尺度用「格」说话**：宽、长、半径用物块直觉描述，再换成像素（×16）。
- **Additive 与颜色**：保留 `Color.A`；过暗的色（如深靛蓝）在 Additive 下几乎不可见 → 光晕可抬亮同色相。
- **MagicPixel 可用**：合法工具。失控的通天 `scale`/无界拉伸会造成白屏或黑条——用 destination 矩形、封顶宽高，或 SoftGlow 密叠等可控画法。失败案例不等于禁用工具。
- **大贴图按世界直径缩放**：如 `DiffusionCircle`（360px）用「目标直径 / 贴图边长」，禁止凭感觉裸 `scale`。
- **Sheet 按帧采**：`Fire` / `Flashimpact` / `HitJagged01` 禁止整表当一帧画。
- **生成点**：`NewProjectile` 坐标是左上角；大 hitbox 事后设 `Center`（含 `SpawnAtMouse`）。

## 红线（写死）

1. **禁止擅自降级**：用户点名的效果或已确认的预期描述，不得用「有伤无光 / 纯尘占位 / 跳过自管 AI 只留爆炸 / A=0 假 Additive」等顶替成品；要改须再确认。
2. **禁止 CWR 运行时依赖**；贴图只拷贝进本模。
3. **禁止生成灾厄弹**当本模伤害载体（进度反射除外，见 AGENTS）。
4. **壳弹贴图必须 `LoadProjectile`**（或等价 Request），勿赌玩家先用过原版武器。
5. **玩法数值冲突以 `requirements` / `balance-stats` / `move-effects` 为准**；本 Skill 不另立第二套数值表。禁止只做特效而忽视充能与同档 DPS 窗；MoveRefRate 是参考，允许按风险/命中难度合理偏离。

## 实现落点（启发，非强制结构）

常见改动面：`FormItemUtil` / 各 `*Force` 形态类、`Content/Combat/Moves/*`、`HenshinFxDraw`、`Assets/Fx/`。新手法验证后，把「可复用结论」写回 `docs/fx-knowledge.md`（一行速查或 cookbook），方便下次 Agent。数值改动回写 `docs/balance-stats.md`（异常名单）与必要时 `move-effects` 语义行。

## 验收

- **主观**：对照开工前用户确认的「最终预期效果」描述。
- **数值**：MoveRefRate / 充能手感符合 `balance-stats`；同档 DPS 窗见 requirements §2.6。
- **客观习惯**：游戏运行中 → 游戏内 Build + Reload（TML003）；大招默认 Mouse3。

