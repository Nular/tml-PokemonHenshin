---
name: henshin-locomotion
description: >-
  Iterates Pokemon Henshin post-transform locomotion (idle/run/jump) visuals for
  this Terraria+tModLoader+Calamity mod. Use when adding or changing form movement
  animations, GIF-to-sprite-sheet pipelines, FormLocomotionSpec / FormAnimClip,
  HenshinOverlayLayer frame drawing, or when the user asks to improve transformed
  walk/idle look. Confirm facing/height/state scope first, then implement.
---

# Henshin Locomotion — 变身移动动画迭代

启发用工作流：先定状态范围与资源朝向，再拆 GIF、接线 `FormLocomotionSpec`。不规定唯一艺术风格；**资源朝向统一、代码不写状态特例翻转**。

## 现役真相（短指针）

| 权威 | 管什么 |
|------|--------|
| `docs/requirements.md` §2.3 | 外观：可选移动动画；缺省单帧+bob；物品/地图静帧 |
| `Content/Core/FormLocomotion.cs` | `FormLocomotionState` / `FormAnimClip` / `FormLocomotionSpec` |
| `Content/Visual/HenshinOverlayLayer.cs` | 采帧绘制 + 统一画高；无 Locomotion 时 bob |
| `Content/PlayerState/HenshinPlayer.cs` | 本地帧计时（`LocomotionFrame` / `LocomotionState`） |
| `Assets/Forms/Locomotion/` | 横条 sheet：`{FormId}_Idle.png` / `_Run.png`（+ JSON 旁证） |
| `tools/extract_locomotion_gif.py` | GIF → sheet + JSON |
| `AGENTS.md` | 构建、目录入口 |
| `docs/backlog.md` | 其它形态动画是否排期 |

`Assets/TEMP_ASSETS/` 可放源 GIF（已 gitignore，勿提交）。

## 外链（需要时再打开）

- tModLoader API 类表：[https://docs.tmodloader.net/docs/stable/annotated.html](https://docs.tmodloader.net/docs/stable/annotated.html)  
  点进 **`PlayerDrawLayer`**（`DrawData` + `sourceRectangle`）。全局规范见 `.cursor/rules/tml-api-docs.mdc`。
- Wiki：[https://github.com/tModLoader/tModLoader/wiki/](https://github.com/tModLoader/tModLoader/wiki/)  
  「Player Item Animation」只管**武器挥舞帧**，**不用于**变身移动机。移动外观走本模 Overlay 层。

## 开工前确认门（未确认不开工）

1. **状态范围：** 仅 Idle+Run？Jump/Fall/Swim 有独立素材还是复用 Run？
2. **朝向：** 各 clip 资源是否已统一朝右（或统一朝左）？**禁止**「Idle 朝左、Run 朝右」靠代码特例补救——应在拆帧时 `--flip-h`。
3. **目标身高：** 默认 `FormLocomotionSpec.TargetDrawHeight = 64`（对齐 Forms 静帧）。要改须明确像素值。
4. **地图头像 / 物品栏：** 默认仍用静帧 `TexturePath`；若要动，单独确认。
5. **向用户确认**上述点后再写实现。

## 标准流程

1. **准备 GIF/帧** → `python tools/extract_locomotion_gif.py <gif> Assets/Forms/Locomotion/{FormId}_{Idle|Run}.png [--flip-h]`  
   - 产出横条 PNG + 同名 JSON（帧数/宽高/`durationsMs`）。JSON **仅旁证**；C# clip **写死常量**（避免运行时解析）。
   - 脚底对齐：脚本按内容 bbox 裁切后底部对齐到共享画布。
2. **填 `FormLocomotionSpec`**（Idle + Run 必填；Jump/Fall/Swim 可空 → `GetClip` 回退 Run → Idle）。  
   - `FacesLeft`：朝右片 = `false`。  
   - 时长：`MsToTicks(ms)`（60 TPS）；不均帧用 `DurationsTicks[]`。
3. **接线形态**：`FormItemUtil.Def(..., locomotion: ...)`；`TexturePath` 仍指向静帧（物品/地图）。
4. **Overlay 已通用**：有 `Locomotion` 则采帧+缩放、无 bob；无则旧路径。勿为单形态复制一层。
5. **回写**：需求 §2.3 / AGENTS 若行为变更；新踩坑补进本 Skill「踩坑」节。

## 状态机（现役）

| 条件 | `ResolveState` | 实际 clip（无独立素材时） |
|------|----------------|---------------------------|
| 着地且 \|vx\|≤0.1 且非游泳语义 | Idle | Idle |
| 水平移动 | Run | Run |
| 空中向上 | Jump | → Run |
| 空中向下 | Fall | → Run |
| wet 且 vy≠0 | Swim | → Run |

帧在 `HenshinPlayer.PreUpdate` 推进；状态切换重置帧。跟 velocity，**无需 Net 同步帧**。  
地面 **Run** 播放速度按 `|vx| / RunAnimRefSpeed(3)` 缩放（夹在 0.6～2.25）；Idle / 空中复用 Run 仍用素材时长。

## 设计红线

1. **tML 不直接播 GIF** — 必须 sheet/序列帧。
2. **朝向在资源层统一** — 代码只认 clip.`FacesLeft`，不做 Idle/Run 分叉翻转。
3. **物品贴图 ≠ 动画 sheet** — `HenshinForceItem.Texture` 继续静帧，避免创造栏闪烁。
4. **有真实跑动动画时禁止再叠 bob**。
5. **HideDrawLayers / WeaponDisplay** — Overlay 仍须清掉外来同贴图 `DrawData`（静帧 + sheet 都要考虑）。
6. **退出变身当帧**清 `LocomotionFrame`；层 visibility 已随 `IsTransformed` 关。

## 验收清单

- 站立 Idle、水平移动 Run；跳/落/水中为 Run（或独立片若已接线）
- 左右朝向正确；身高约 64px 与其它静帧接近
- 物品栏 / 地图头像仍静帧；松手无残留
- 其它 `Locomotion==null` 形态外观不变
- 游戏内 Build + Reload（命令行遇 TML003 时用游戏内构建）

## 实现落点

- 类型：`Content/Core/FormLocomotion.cs`、`FormDefinition.Locomotion`
- 绘制：`Content/Visual/HenshinOverlayLayer.cs`
- 帧：`HenshinPlayer`（Enter/Exit/PreUpdate）
- 接线：`FormItemUtil.Def` + 各 `*Force`；样例 `PikachuLocomotion()`
- 资源：`Assets/Forms/Locomotion/`；脚本 `tools/extract_locomotion_gif.py`
