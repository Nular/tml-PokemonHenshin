using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>H5（金属怪/巨金怪/大岩蛇/大钢蛇）自绘贴图的懒加载与取帧。仅在客户端 PreDraw 调用。</summary>
	public static class H5Fx
	{
		private static readonly System.Collections.Generic.Dictionary<string, Texture2D> Cache = new();

		public static Texture2D Sheet(string name)
		{
			if (Cache.TryGetValue(name, out Texture2D t) && t != null && !t.IsDisposed)
				return t;
			t = ModContent.Request<Texture2D>($"PokemonHenshin/Assets/Fx/{name}/{name}", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			Cache[name] = t;
			return t;
		}

		/// <summary>竖排条带画一帧（Alpha 混合）。origin 为单帧内像素。
		/// <paramref name="color"/>：null = 白色（不受光，glow 招式）；lit 招式传 lightColor 或 <see cref="HenshinFxDraw.Lit"/>；最终色 = color × alpha。</summary>
		public static void Draw(string name, int frames, int frame, Vector2 world, Vector2 originPx, float rot, Vector2 scale, float alpha = 1f, SpriteEffects fx = SpriteEffects.None,
			Color? color = null)
		{
			Texture2D tex = Sheet(name);
			int fh = tex.Height / frames;
			frame = Math.Clamp(frame, 0, frames - 1);
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			Main.EntitySpriteDraw(tex, (world - Main.screenPosition).Floor(), src, (color ?? Color.White) * alpha, rot, originPx, scale, fx, 0f);
		}

		public static void Draw(string name, int frames, int frame, Vector2 world, Vector2 originPx, float rot, float scale, float alpha = 1f, SpriteEffects fx = SpriteEffects.None,
			Color? color = null)
			=> Draw(name, frames, frame, world, originPx, rot, new Vector2(scale), alpha, fx, color);

		/// <summary>从父射弹派生子射弹并继承招式来源（与金属爪导演同一套）。</summary>
		public static int SpawnChild(Projectile parent, int type, Vector2 pos, Vector2 vel, int damage, float kb, float ai0 = 0f, float ai1 = 0f, MoveDelivery delivery = MoveDelivery.MeleeArc)
		{
			int id = Projectile.NewProjectile(parent.GetSource_FromThis(), pos, vel, type, damage, kb, parent.owner, ai0, ai1);
			if (id < 0) return -1;
			Projectile c = Main.projectile[id];
			c.originalDamage = parent.originalDamage > 0 ? parent.originalDamage : parent.damage;
			HenshinAccGlobalProjectile.CopyMoveOrigin(parent, c);
			if (c.ModProjectile is IHenshinMoveProj tagged)
				tagged.Delivery = delivery;
			return id;
		}
	}

	/// <summary>闪光炮（大钢蛇大招，原作：钢系特殊招式，胶囊/等级可学，非身体冲撞）：胸前蓄光 → 朝鼠标（360°）射出银白光弹 → 命中或撞墙大范围闪光爆。
	/// 伤害分配：光弹 0.35×（穿透 4），闪光爆 0.5×（8 格），余辉 5 跳×0.08×；同一目标合计 1.25× 大招倍率；爆圈内其余敌人受 0.5× + 余辉。</summary>
	public class FlashCannonChargeProj : HenshinMoveProj
	{
		private const int ChargeTicks = 20;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
		private Vector2 _dir = Vector2.UnitX;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = ChargeTicks + 1;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		private Vector2 OrbPos(Player p, float t) => p.MountedCenter + _dir * (26f + 10f * t);

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active || p.dead) { Projectile.Kill(); return; }
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.MountedCenter;
			if (aim.LengthSquared() > 4f) _dir = Vector2.Normalize(aim);
			int age = ChargeTicks + 1 - Projectile.timeLeft;
			Projectile.Center = p.MountedCenter;
			// R3 手感（G2）：起手只留共享 UltCast（根弹各端自动播），删 Item29；蓄光光照随蓄力变强。
			float charge = age / (float)ChargeTicks;
			// 光照第 2 轮 D 组：蓄光稍亮（×1.2）；蓄力弹（导演）活着时在玩家中心外缘发少量钻石粒子（只用同步的 Center，避开胸前光球）。
			Lighting.AddLight(OrbPos(p, charge), new Vector3(0.6f, 0.9f, 1.2f) * (0.6f + 0.9f * charge));
			HenshinUltGlow.Emit(Projectile, 60f, DustID.GemDiamond, Vector3.Zero);
			if (age == ChargeTicks)
			{
				// 开炮：各端播钢系施放音 + 枪口 S 爆（原 Item72 只在主人端响）；主人端重档后坐震屏。
				Vector2 muzzle = OrbPos(p, 1f);
				HenshinSfx.Play(SfxKey.SteelCast, muzzle, 1f, 0.2f);
				HenshinExplosion.Spawn(muzzle, BlastSize.S, CannonTint, 90f, kenneyFrame: false);
				HenshinHitFeel.Shake(Projectile.owner, muzzle, HitTier.Heavy, -_dir);
			}
			if (age == ChargeTicks && Projectile.owner == Main.myPlayer)
			{
				Vector2 at = OrbPos(p, 1f);
				int dmg = Math.Max(1, (int)(Projectile.damage * 0.35f));
				H5Fx.SpawnChild(Projectile, ModContent.ProjectileType<FlashCannonShotProj>(), at, _dir * 22f, dmg, Math.Max(6f, Projectile.knockBack), 0f, 0f, MoveDelivery.Bolt);
				// 蓄光完成，全部伤害已转交光弹/闪光爆
			}
		}

		internal static readonly Color CannonTint = new(150, 215, 255);

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) return false;
			int age = ChargeTicks + 1 - Projectile.timeLeft;
			float t = MathHelper.Clamp(age / (float)ChargeTicks, 0f, 1f);
			Vector2 pos = OrbPos(p, t);
			float scale = MathHelper.Lerp(0.35f, 1.5f, t * t);
			H5Fx.Draw("FlashCannonOrb", 4, (int)(Main.GameUpdateCount / 3) % 4, pos, new Vector2(26f, 26f), Main.GameUpdateCount * 0.08f, scale);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos, HenshinFxDraw.WithAlpha(new Color(160, 220, 255), 0.35f * t), 0.5f + t * 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	public class FlashCannonShotProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 55;
			Projectile.tileCollide = true;
			Projectile.penetrate = 4; // R3：穿透 4 个敌人，穿完/撞墙/飞满才起爆
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
			Projectile.ignoreWater = true;
		}

		// R3 手感（G2）：HenshinTrail 光尾代替固定 64px 直光束，沿真实弹道收尖（速度 22 → 约 220px）。
		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10);

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.8f, 1.05f, 1.25f);
			// 光照第 2 轮 D 组：蓄力弹已死，光弹（单颗，N=1）接着发；已有强光，粒子不另加光。
			HenshinUltGlow.Emit(Projectile, 44f, DustID.GemDiamond, Vector3.Zero);
		}

		// 穿透命中：同步钢火花（光 + S 爆 + 钢命中音）+ 打击感（出自大招槽 → Ultimate 档，20 tick 内只震一次）。
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.SteelSpark, 1.2f, Projectile.owner, 0f, SfxKey.SteelHit);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), FlashCannonChargeProj.CannonTint);
		}

		public override void OnKill(int timeLeft)
		{
			if (Projectile.owner != Main.myPlayer) return;
			// 光弹 0.35×（可穿透多个）→ 闪光爆 0.5×（半径 8 格）→ 余辉 5 跳 × 0.08×；同一目标合计 0.35+0.5+0.4 = 1.25× 大招倍率
			int dmg = Math.Max(1, (int)(Projectile.damage * (0.5f / 0.35f)));
			int id = H5Fx.SpawnChild(Projectile, ModContent.ProjectileType<FlashCannonBlastProj>(), Projectile.Center, Vector2.Zero, dmg, Math.Max(8f, Projectile.knockBack), 0f, 0f, MoveDelivery.AoEBurst);
			if (id >= 0) Main.projectile[id].Center = Projectile.Center;
			int glowDmg = Math.Max(1, (int)(Projectile.damage * (0.08f / 0.35f) + 0.5f));
			int gid = H5Fx.SpawnChild(Projectile, ModContent.ProjectileType<FlashCannonGlowProj>(), Projectile.Center, Vector2.Zero, glowDmg, 0f, 0f, 0f, MoveDelivery.AoEBurst);
			if (gid >= 0) Main.projectile[gid].Center = Projectile.Center;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinTrail.Strip(Projectile, new Color(110, 200, 255), 30f, new Color(240, 250, 255), 0.4f, 0f, 0.95f);
			H5Fx.Draw("FlashCannonOrb", 4, (int)(Main.GameUpdateCount / 3) % 4, Projectile.Center, new Vector2(26f, 26f), Projectile.rotation, 1.45f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(FlashCannonChargeProj.CannonTint, 0.55f), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 70f));
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	public class FlashCannonBlastProj : HenshinMoveProj
	{
		private const int Life = 18;
		private const float Radius = 128f; // 8 格（R3：原 5.5 格）
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = (int)(Radius * 2f);
			Projectile.height = (int)(Radius * 2f);
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// R3 手感（G2）：自制大爆音（各端）+ 主人端大招震屏（20 tick 窗口，命中再震不叠）。
				HenshinSfx.Play(SfxKey.BlastL, Projectile.Center);
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate);
			}
			int age = Life - Projectile.timeLeft;
			HenshinExplosion.Pulse(Projectile.Center, FlashCannonChargeProj.CannonTint, BlastSize.L, age / (float)Life, Radius * 2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), FlashCannonChargeProj.CannonTint);
			// 每次施放第一下命中补一记 HitUlt（同步特效弹播，各端都听得到）。
			if (H3Fx.TryUltBeat(Projectile.owner))
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.SteelSpark, 1.6f, Projectile.owner, 0f, SfxKey.HitUlt);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 c = Projectile.Center;
			Vector2 closest = Vector2.Clamp(c, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, c) <= Radius * Radius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			int frame = Math.Min(5, age * 6 / Life);
			// 帧内光环外径≈88px(art 44)，按 Radius 对齐
			H5Fx.Draw("FlashCannonBlast", 6, frame, Projectile.Center, new Vector2(50f, 50f), 0f, Radius / 44f);
			HenshinFxDraw.BeginAdditive();
			// R3：白热核心闪拉长到 10 tick、更亮；再叠 L 号爆炸配方（双冲击环 + 放射亮条，不叠 Kenney 火烟）。
			if (age < 10)
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(220, 242, 255), 0.75f * (1f - age / 10f)), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, Radius * 1.6f));
			HenshinExplosion.Draw(Projectile.Center, BlastSize.L, FlashCannonChargeProj.CannonTint, age / (float)Life, Radius * 2f, 0f, kenneyFrame: false);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>闪光炮余辉（R3 新增）：爆心留 1 秒银白光场，半径 8 格，每 12 tick 一跳共 5 跳（每跳 0.08× 大招倍率），不放粒子。</summary>
	public class FlashCannonGlowProj : HenshinMoveProj
	{
		private const int Life = 60;
		private const float Radius = 128f;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = (int)(Radius * 2f);
			Projectile.height = (int)(Radius * 2f);
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			// 光照第 2 轮 D 组：余辉稍亮（×1.2）+ 光场外缘少量钻石粒子（光弹已死，余辉单颗 N=1 接着发）。
			float k = 1.2f * Projectile.timeLeft / Life;
			Lighting.AddLight(Projectile.Center, 0.5f * k + 0.1f, 0.65f * k + 0.1f, 0.75f * k + 0.1f);
			HenshinUltGlow.Emit(Projectile, Radius, DustID.GemDiamond, Vector3.Zero, everyTicks: 3);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 c = Projectile.Center;
			Vector2 closest = Vector2.Clamp(c, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, c) <= Radius * Radius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fade = Projectile.timeLeft / (float)Life;
			float pulse = 0.85f + 0.15f * (float)Math.Sin(Projectile.timeLeft * 0.5f);
			H5Fx.Draw("FlashCannonBlast", 6, 5, Projectile.Center, new Vector2(50f, 50f), 0f, Radius / 44f, 0.55f * fade);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(170, 220, 255), 0.22f * fade * pulse), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, Radius * 1.7f));
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>岩崩落石（大岩蛇大招 / 大钢蛇·豪力技能共用）：自绘岩块 + 落地碎石圈。ai0&gt;0.5 落地 AoE；ai1&gt;0.5 大块（大招）。</summary>
	public class H5FallingRockProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
		private int _variant;

		// R4：恢复广角镜追踪，但由本弹自己调用 HomingAI（PostAI 不再代劳），以便命中后停追、贴脸不抖。
		// 乱颤根因：穿透 2 命中后仍在敌人体内，HomingAI 每 tick 把速度按 turnRate(=1 瞬转)拧向敌人中心，
		// 穿过中心后方向反向 → 左右/上下来回翻转。ai[2]=1 表示已命中，之后只受重力。
		// R6：仍偶发弹飞上天。根因：广角镜锁定后不因方向断锁、turnRate=1 瞬转保速，
		// 石头穿过目标却没触发 OnHitNPC（目标 npc.immune 共享无敌帧中）→ 掉头冲回目标（向上），
		// 回程才命中置 ai[2]=1，带着向上的速度弹道飞高。现改为：
		// ai[2]=1 永久停追的触发 = 首次命中 / hitbox 与任一敌对 NPC 重叠（不论伤害、无敌帧）/ 越过锁定目标中心高度；
		// 追踪转向限幅在竖直向下 ±MaxSteerDeg 内；任何时候 velocity.Y ≥ MinFallSpeed。
		public override bool HandlesOwnHoming => true;

		private const float MaxSteerDeg = 55f;   // 追踪后速度方向与竖直向下夹角上限
		private const float MinFallSpeed = 6f;   // 速度 Y 下限（向下），出生 ≥10（Wave2MoveProjs RockSlideDirectorProj）

		// R3 手感（G2）：只加表现（拖尾 / 落地音 / 爆炸 / 震屏），追踪与停追逻辑一行未动。
		private static readonly Color DustTint = new(215, 180, 130);
		private bool IsUltRock => Projectile.ai[1] > 0.5f;

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 8);

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.penetrate = 2;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			_variant = Projectile.identity % 3;
			if (Projectile.ai[1] > 0.5f)
			{
				Vector2 c = Projectile.Center;
				Projectile.width = 36;
				Projectile.height = 36;
				Projectile.Center = c;
			}
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.35f;
			Projectile.rotation += 0.15f * (Projectile.velocity.X >= 0f ? 1f : -1f);
			// R5: ai[2]=1 = 已命中过，彻底脱离广角镜（Homing 关、锁定清除），任何读 Homing 的路径都不再碰它。
			if (Projectile.ai[2] >= 0.5f && (Homing || HomingTargetWhoAmI != -1))
			{
				Homing = false;
				HomingTargetWhoAmI = -1;
			}
			if (Homing && Projectile.ai[2] < 0.5f && Projectile.owner == Main.myPlayer && ShouldStopHoming())
			{
				Projectile.ai[2] = 1f; Homing = false; HomingTargetWhoAmI = -1;
				Projectile.netUpdate = true;
			}
			if (Homing && Projectile.ai[2] < 0.5f)
			{
				HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
				ClampSteer();
			}
			// 落石铁律：任何时候都不向上（也兜住远端位置/速度校正）。
			if (Projectile.velocity.Y < MinFallSpeed)
				Projectile.velocity.Y = MinFallSpeed;
			// 光照第 2 轮 D 组：岩崩大招 12 块，导演（40 tick）比落石（90 tick）先死 → 大块按 whoAmI % 4 抽样发黄晶粒子 + 色光（lit 大招例外）。
			if (IsUltRock)
				HenshinUltGlow.Emit(Projectile, 40f, DustID.GemTopaz, new Vector3(0.4f, 0.32f, 0.2f), sampleN: 4, forceLight: true,
					yieldToType: ModContent.ProjectileType<RockSlideDirectorProj>());
		}

		/// <summary>停追条件：与任一敌对 NPC 重叠（含无敌帧、假人、不受伤），或已越过锁定目标中心高度。</summary>
		private bool ShouldStopHoming()
		{
			Rectangle box = Projectile.Hitbox;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (n.active && !n.friendly && box.Intersects(n.Hitbox))
					return true;
			}
			int lockId = HomingTargetWhoAmI;
			return (uint)lockId < Main.maxNPCs && Main.npc[lockId].active
				&& Projectile.Center.Y >= Main.npc[lockId].Center.Y;
		}

		/// <summary>追踪转向限幅：方向夹在竖直向下 ±MaxSteerDeg 内，保速。</summary>
		private void ClampSteer()
		{
			Vector2 v = Projectile.velocity;
			float speed = v.Length();
			if (speed < 0.01f)
				return;
			float maxRad = MathHelper.ToRadians(MaxSteerDeg);
			float ang = (float)Math.Atan2(v.X, v.Y); // 相对 +Y（向下）的偏角
			if (Math.Abs(ang) > maxRad)
			{
				ang = Math.Sign(ang) * maxRad;
				Projectile.velocity = new Vector2((float)Math.Sin(ang), (float)Math.Cos(ang)) * speed;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[2] < 0.5f)
			{
				Projectile.ai[2] = 1f; Homing = false; HomingTargetWhoAmI = -1; // 命中后停追；ai 随 netUpdate 同步
				Projectile.netUpdate = true;
			}
			target.AddBuff(BuffID.Slow, 40);
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 20);
		}

		public override void OnKill(int timeLeft)
		{
			// OnKill 各端都跑：落地音（节流，12 石不再 12 声 Item14）、爆炸配方、主人端震屏都放这里；删 3 颗石屑尘。
			// 大招：每次施放第一块落地补一记 BlastL（90 tick 窗口），之后每块 RockHit；技能 3 石只 RockHit。
			if (IsUltRock && HenshinSfx.PlayCooldown(SfxKey.BlastL, Projectile.Center, 90))
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate, Vector2.UnitY);
			else
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, IsUltRock ? HitTier.Heavy : HitTier.Light, Vector2.UnitY);
			HenshinSfx.PlayCooldown(SfxKey.RockHit, Projectile.Center, 4, IsUltRock ? 1f : 0.85f);
			// 光照 A 组：石头是 lit 招式，落地爆炸按 glow 例外保留，强度减半。
			HenshinExplosion.Spawn(Projectile.Center, IsUltRock ? BlastSize.M : BlastSize.S, DustTint, IsUltRock ? 96f : 56f, kenneyFrame: false, strength: 0.5f);
			if (Projectile.ai[0] > 0.5f && Projectile.owner == Main.myPlayer)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<H5RockBurstProj>(), Projectile.damage, Projectile.knockBack * 0.85f, Projectile.owner, Projectile.ai[1]);
				if (id >= 0)
				{
					Main.projectile[id].Center = Projectile.Center;
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.EasyCrit = EasyCrit;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float s = Projectile.ai[1] > 0.5f ? 1.3f : 0.95f;
			// 短尘线：暖灰外晕 + 淡白芯，比石块窄，不盖住贴图。光照 A 组：尘线和石块都吃光（lit），暗处变暗。
			HenshinTrail.Strip(Projectile, DustTint, IsUltRock ? 22f : 14f, new Color(255, 240, 215), 0.3f, 0f, 0.55f, lit: true);
			H5Fx.Draw("RockFall", 6, _variant, Projectile.Center, new Vector2(26f, 26f), Projectile.rotation, s, color: lightColor);
			return false;
		}
	}

	/// <summary>岩崩落地碎石圈：1 格半径（大块 1.5 格）短命范围伤，贴图碎石帧，无粒子堆。</summary>
	public class H5RockBurstProj : HenshinMoveProj
	{
		private const int Life = 12;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
		private float Radius => Projectile.ai[0] > 0.5f ? 24f : 16f;

		// R3：碎石圈原地不动，不吃广角镜追踪。
		public override bool HandlesOwnHoming => true;
		public override bool ShouldUpdatePosition() => false;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 c = Projectile.Center;
			Vector2 closest = Vector2.Clamp(c, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, c) <= Radius * Radius;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 40);
			target.velocity *= 0.2f;
			// 打击感（G2）：大岩蛇大招 → Ultimate 档（20 tick 只震一次），技能 → Light。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(215, 180, 130), Projectile.Center, 0.5f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			int frame = 3 + Math.Min(2, age * 3 / Life);
			// 光照 A 组：碎石圈 lit；中心常贴地，用 LightAt（实心格往上取一格）。
			H5Fx.Draw("RockFall", 6, frame, Projectile.Center, new Vector2(26f, 26f), 0f, Radius / 18f, 1f - age / (float)Life * 0.4f,
				color: HenshinFxDraw.LightAt(Projectile.Center));
			return false;
		}

		public override bool? CanDamage() => Projectile.timeLeft > 2 ? null : false;
	}
}
