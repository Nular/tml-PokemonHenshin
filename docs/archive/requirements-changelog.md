# 归档：requirements 修订流水

## 16. 修订记录

| 版本 | 变更 |
|------|------|
| 1.0 | 初版开发基线（核心规则、进度、形态骨架、联机与里程碑） |
| 1.1 | 持握时禁止坐骑；开局发放御三家；后期加入超梦/洛奇亚/烈空坐；清理上下文黑话 |
| **1.2** | 明确：无自制 FX 图，特效只复用/参考原版·灾厄·大修；宝可梦外观资源可从 52poke 全国图鉴自行获取 |
| 1.2.1 | 洁癖：修正错别字；仓库补 README / AGENTS 入口指针 |
| **1.3** | 战斗改为被动+技能1/2+能量大招；新增 A13～A21；招式泰拉适配表见 move-effects.md |
| 1.3.1 | 洁癖：§1.1/§14/状态栏与 README·AGENTS·dev-plan 对齐现役；注明无穿障形态 |
| **1.4** | 物品实例等级/经验；分阶段等级硬顶；经验公式与 Boss 动态 XP；推翻 StageDamage，攻防对齐比目鱼有效 DPS；变身替换盔甲防御；能量池 1000+软顶；进化须进度+等级双条件；数值表见 `docs/balance-stats.md` |
| 1.4.1 | 招式平衡：段数慎改；MoveRefRate 为参考；按命中难度/距离/风险柔性调倍率（见 balance-stats §7） |
| 1.4.3 | 击杀 XP × 世界档；`ExpNeeded` × 物品带；世界字 `EXP +X` / `LEVEL UP!`（不跟世界档折算当前 Xp） |
| **1.4.5** | 洁癖：§9 档位与 `FormDefinition.Stage` 对齐；获取途径写开局/Boss/合成 |
| **1.4.6** | 洁癖：产品口径只写现役；未接线代码集中到 §12.1；去掉每档数量上限 |
| **1.4.7** | 饰品重构：28 家族碎片/普通/超级；52poke 官方名；不变之石未变身也挡进化；广角镜按 Delivery 追踪；掉率匣 10% / 事件 2% / Boss 25% |
| **1.4.8** | 龙之波动爆炸碎片（原版 620）命中改为 `1 × EnergyGainFactor`；击杀能量不变。洁癖：明确**仅打标弹**走碎屑；入口文档对齐 A28 / 页眉版本 |
| **1.4.9** | 变身属性面板：物品栏右侧入口，汇总当前形态/特性/属性/饰品加成（§2.10） |
| **1.4.10** | 广角镜菱形索敌：碎片 8 / 成品 16 / 超级 32 格（max）；新锁 60° 半角；锁死后可掉头，断锁 2× |
| **1.4.11** | 诅咒之符去幽灵共鸣；戴上即受原版诅咒焰（碎片 3s / 普通 5s / 每 30s；超级免疫；未变身也烧）。若干招默认撞墙，诅咒符按 Delivery 再穿 |
| **1.4.12** | 暴风改为 Barrage 穿透飞弹（不是 Field）；广角镜仍不含 Field；诅咒符穿墙含 Beam、不含 Field。暴风默认贴地，戴符 Barrage 穿墙 |
| **1.4.13** | 神奇糖果：金美味同材料合成；使用令物品栏第一格之力 +1 级（守硬顶）；每个 `npc.boss` 5%（袋内不额外 roll） |
| **1.4.14** | 洁癖：入口文档对齐糖果；§8 NetOp 指向代码枚举；施工图 `SyncForceProgress` 更正为现役 `SyncEnergy` |
| **1.4.15** | 御三家改绑角色档：建角 `AddStartingItems` + 旧档 `PostUpdateMiscEffects` 入包（不用 `OnEnterWorld`）。属性面板入口下移 64px 避开原版图鉴。HJSON 以 `{`/`[` 开头的值须加引号。`TilePierceEligible` 去掉 Field，与 §6 对齐 |
| **1.4.16** | 变身地图头像：形态全身图缩小填入原版玩家头像 RT（`HenshinMapHeadLayer` + `headOnlyRender` 分上下文藏层）。虫洞药水仍点原版头像坐标 |
| **1.4.17** | 联机：`SyncAim` + 壳弹/OnSpawn 字段重建**已接线**（双端验收 pending）。清单 `docs/fx-knowledge.md`。洁癖：入口文档区分「已接线」与「已双端验收」 |
| **1.4.18** | 世界档/`100` 改为**获取硬顶**（`CanGainExperience`）：不截存档等级；卡顶/满级不加 XP、不飘 `EXP +X`。攻防跟存档 Level。洁癖对齐入口文档 |
| **1.4.19** | 广角镜索敌改圆形（仍 8/16/32）；招式自带追踪不吃广角镜，恢复全向掉头手感 |
| **1.4.20** | 之力禁用原版词缀，专属蓄能/铁壁/猛攻；暴击对齐原版 Crit；A15/着火升档仅 Crit 后 ×4；超暴击偏红橙大飘字 |
| **1.4.21** | 变身属性面板：标题栏拖动（`HenshinClientConfig` 持久化）+ 复位；招式威力%；会心/超会心段；出伤按武器/命中/其它乘区展示 |
| **1.4.22** | 大招释放后约 2.5s（150 tick）禁止一切能量回复（`UltEnergyLockoutTicks`）；移除弹上能量系数 / fail-closed。`SourceMoveSlot` 仍供 UltDamageBonus 等读出弹槽。洁癖对齐入口与 `balance-stats` |
| **1.4.23** | 饰品重平衡：删死路径（AffinityAmp/FallDmg/GuardCut/TilePierceField/AccActive）；A02 +10% 纯伤；A15 暴击率/升档/暴伤三轴；A10 飞伤+飞行时间%；A25 闪避；惩罚对冲（讲究/宝珠/弱点保险）；贝壳回复翻倍；凸凸防御点；剩饭 3/6 HP/s |
| **1.4.24** | 变身可选移动动画：`FormLocomotionSpec`（Idle/Run；Jump/Fall/Swim 槽位预留→Run）；皮卡丘 L04_F01 接线；统一画高 64；资源朝右；物品/地图仍静帧；地面 Run 随 `|vx|` 变速。Skill：`.cursor/skills/henshin-locomotion` |
| **1.4.25** | A29 电气球：仅皮卡丘（`L04_F01`）；普通形态攻防 +100% + 攻速 10%；超级 +200% + 攻速 20%；碎片奇攻/偶防各 25%。新增 `RequiredFormId` / `FormAtkMul` / `FormDefMul` / `UseTimeMul`（攻速不占 CooldownCut 硬顶）。接受皮卡丘永久毕业 |
| **1.4.26** | 洁癖：FX §1.5 允许自制（优先拷贝）；项目管理段迁 backlog/archive；`dev-plan`→`engineering`+`backlog`；`accessory-rework`→`accessories` |

---

**Agent 勿读本文件。** 现役需求页眉版本见 `docs/requirements.md`。
