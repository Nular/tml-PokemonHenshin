# Pokemon Henshin — 缺口与下一步

| 项 | 内容 |
|----|------|
| 角色 | **状态 / 缺口 / 下一步的唯一现役源**（AGENTS / README 只链到此） |
| 对齐需求 | `docs/requirements.md` **v1.4.26** |
| 更新日期 | 2026-09-12 |

产品规则以 `docs/requirements.md` 为准。历史里程碑/修订流水仅人类可查 `docs/archive/`（**Agent 禁止打开**）。

---

## 1. 已接线摘要（勿当待办）

- 36 形态 + A01–A29（含皮卡丘电气球）；被动 + 技能1/2 + 能量大招
- v1.4 数值：击杀 XP × 世界档；`ExpNeeded` × 物品带；世界档/`100` **只挡获取**；卡顶/满级不飘 `EXP +X`；大招后 ~2.5s `UltEnergyLockoutTicks`
- 龙之波动爆炸碎片命中 `1×Factor`（须 `MarkCrumb`）
- 皮卡丘 Idle/Run 移动动画（用户确认手感 OK）；其它形态仍单帧+bob
- `SyncAim` / 壳弹旁观端 `Ensure*` **代码已接线**（双端验收仍 pending）
- 之力专属前缀蓄能/铁壁/猛攻；暴击对齐原版 Crit
- 御三家建角发放 + 旧档补发（进世界已本地验收）
- 变身地图头像、属性面板拖动：**代码已接线**（游戏内/联机验收 pending）
- 远程飞弹（Bolt/Barrage 外飞壳）寿命约 ×1.5；念力索敌 48 格（**本地验收 OK**）

---

## 2. 待本地验收（优先）

| ID | 项 | 备注 |
|----|-----|------|
| L1 | 饰品重平衡 + A29 电气球手感 | 数字以 `HenshinAccCatalog` 为准 |
| L2 | 专属前缀重铸 + 超暴击飘字 | |
| L3 | 神奇糖果合成/使用/Boss 5% | |
| L4 | 饰品图标 / 合成 / 掉落 | 见 `docs/accessories.md` |
| L5 | 属性面板拖动与文案 | |
| L6 | 获取硬顶：进世界不 Truncate；卡顶不飘 EXP | 公式已验，游戏内待签 |
| L7 | 地图头像 + 虫洞药水 | 需求 §2.3 |

---

## 3. 联机双端（pending）

用例表见 `docs/engineering.md` §联机测试。重点：

- 瞄准 / 壳弹旁观贴图（已接线）
- N12 藤鞭指向释放者鼠标
- 地图头像 / 虫洞
- 能量 / XP 同步观感

---

## 4. 内容与平衡缺口

- DPS 抽检 PS7 / 9 / 12（对标 requirements §11）
- Rage / 肾上腺素是否计入 `HenshinDamage`（pending 实测）
- 裸 `LoadProjectile` 专用服风险未扫完（泡沫已 `SafeLoadProjectile`）
- 未打标 Retarget 弹（如污泥毒云）仍走完整命中能
- 未接线管线**保持不动**：requirements §12.1
- 逐招精标定回写 `move-effects.md`（balance-stats §7.1 点名项代码已改）
- 对标武器具体 ItemID；Boss XP 白名单（若偏差过大）
- 其它形态移动动画（可选；Skill：`henshin-locomotion`）

---

## 5. 下一步顺序

1. 游戏内验饰品（含 A29）+ 专属前缀 / 超暴击飘字  
2. 神奇糖果与图标 / 合成 / 掉落  
3. 联机双端（含 N12、地图头像 / 虫洞）  
4. DPS 抽检 → Rage  
5. 新形态：继承 `HenshinForceItem`，`NetworkId` 从 37 起；共享数据只放 `FormDefinition`

**验证命令：** 游戏内 Build + Reload（TML003）；大招默认 Mouse3；`/henshin stats`、`/henshin setlevel`；`dotnet run --project tools/HenshinStatVerify`。

---

## 6. 验收清单（现役）

从原 requirements §14 迁出；改产品规则时同步改此表。

1. 持握之力：外观、被动、技能1/2、能量大招、变身饰品同时生效；坐骑不可用。  
2. 取消持握：上述本模效果同 tick 消失；坐骑恢复可用。  
3. 开局可获得御三家三件之力。  
4. 进化符合双条件（进度档 + 等级进入下一形态 `BandMin`）、替换范围与前缀/`Level`/`Xp` 继承。  
5. 各档登场以 requirements §9.2 为准；同档互竞可感知。  
6. 持握被动与属性生存/机动按 requirements §2.5 / §3.2 工作。  
7. 后期存在超梦、洛奇亚、烈空坐等更强传说向之力。  
8. 联机下形态、能量、等级/经验与伤害无明显分歧。  
9. 地鼠线改地形遵守 requirements §7 预算与黑名单。  
10. DPS 落在对标区间内（含等级段底/段顶抽检）。  
11. 招式观感遵守 requirements §1.5（优先拷贝/复用，可自制进 `Assets/Fx/`）。  
12. 变身 Overlay 宝可梦图来自约定图鉴来源（或可替换路径）。  
13. 物品描述含获取/进化条件（含等级要求）；进度档提升有提示。  
14. 变身时盔甲防御被形态防御取代，饰品防御仍生效。  
15. 不够等级时即使已击败对应 Boss，也不得进化。  
16. 神奇糖果：金美味同材料可合成；使用使物品栏第一格之力 +1 级（守硬顶 / 满级拒用）；任意 `npc.boss` 5%，专家袋不额外 roll。

---

## 7. 仍待填写（不阻塞骨架）

- 对标武器的具体 ItemID（随灾厄版本）  
- 饰品精确数值微调（游戏内手感后回写 Catalog）  
- Boss XP 白名单  
- 各形态 52poke 精灵图精修清单  
- 公开分发时的显示名 / 商标策略（工程上须已支持替换）
