using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 龙尾（哈克龙）/ 钢尾（大钢蛇，ai1≥0.5 走铁板节分支）：甩尾抽打。
	/// 蓄(0-8)尾巴向身后卷起 → 甩(8-16)加速扫向鼠标 → 抽(16-24)尾梢滞后 5 tick 才到最远点并「啪」(tick 18，Item153 + 命中环帧) → 收(24-36)。
	/// 13 个控制点，每点读延迟的挥角（同 GrassWhipProj 的延迟角控制点）。保留大幅击退。
	/// ai0 = Dust（可选，默认 Cloud）；ai1≥0.5 = 钢尾；ai2 = 朝向弧度 + AimBias（拥有者首帧写入并 netUpdate，0 = 未锁定）。
	/// 贴图 Assets/Fx/DragonTailWhip/DragonTail.png：4 帧 24x20（0 身节 1 带环身节 2 细节 3 尾梢），朝 +x，中心 (12,10)。
	/// </summary>
	public class DragonTailWhipProj : HenshinMoveProj
	{
		private const int Lifetime = 36;
		private const int CrackTick = 18;
		private const float Length = 15f * 16f;
		private const int Segs = 12;
		private const float TipLag = 5f;
		private const float AimBias = 10f;
		private const float BaseKnockBack = 22f;

		private Vector2[] _pts;
		private Vector2[] _prevPts;
		private int _ptsAge = -1;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool Aimed => Projectile.ai[2] > 0.5f;
		private float Angle => Projectile.ai[2] - AimBias;
		private int Age => (int)Projectile.localAI[0];

		public override void SetDefaults()
		{
			Projectile.width = 32;
			Projectile.height = 32;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime + 30; // 按 localAI0 手动结束，等待同步期间不倒计时消失
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.knockBack = BaseKnockBack;
		}

		public override bool ShouldUpdatePosition() => false;

		private Vector2[] Pts(Player owner, int age)
		{
			if (_pts == null || _ptsAge != age)
			{
				_prevPts = _ptsAge == age - 1 ? _pts : null;
				_pts = ControlPoints(owner, age);
				_ptsAge = age;
			}
			return _pts;
		}

		// 后摆角 φ(τ)：0→Φ 蓄，Φ→0 甩（先慢后快），之后 0。
		private static float Phi(float tau)
		{
			const float Max = 2.2f;
			if (tau <= 0f)
				return 0f;
			if (tau < 8f)
				return Max * (1f - (1f - tau / 8f) * (1f - tau / 8f));
			if (tau < 16f)
			{
				float u = (tau - 8f) / 8f;
				return Max * (1f - u * u);
			}
			return 0f;
		}

		private static float LenFactor(float tau)
		{
			if (tau < 8f)
				return 0.5f;
			if (tau < 16f)
				return 0.5f + 0.5f * (tau - 8f) / 8f;
			if (tau < 24f)
				return 1f;
			return 1f - 0.65f * (tau - 24f) / 12f;
		}

		/// <summary>13 个控制点（0=尾根，12=尾梢）。每点读延迟 s*TipLag tick 的挥角；收尾时叠一点松弛的 S 波。</summary>
		private Vector2[] ControlPoints(Player owner, float tau)
		{
			Vector2[] pts = new Vector2[Segs + 1];
			float baseAng = Angle;
			float side = Math.Cos(baseAng) >= 0f ? -1f : 1f;
			float segLen = Length / Segs * LenFactor(tau);
			float slack = tau > 24f ? (tau - 24f) / 12f : 0f;
			pts[0] = owner.MountedCenter;
			for (int i = 1; i <= Segs; i++)
			{
				float s = (i - 0.5f) / Segs;
				float a = baseAng + side * Phi(tau - s * TipLag) + side * 0.4f * slack * (float)Math.Sin(s * 6.2832f + tau * 0.4f);
				pts[i] = pts[i - 1] + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * segLen;
			}
			return pts;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			// 360° 朝鼠标：仅拥有者求一次，弧度写 ai2 并 netUpdate；其它端等同步后才开始计时。
			if (!Aimed)
			{
				if (Projectile.owner != Main.myPlayer)
					return;
				Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
				Projectile.ai[2] = aim.ToRotation() + AimBias;
				Projectile.netUpdate = true;
				if (Math.Abs(aim.X) > 0.05f)
					owner.ChangeDir(aim.X > 0f ? 1 : -1);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.25f }, owner.MountedCenter);
			}

			Projectile.localAI[0] += 1f;
			int age = Age;
			if (age >= Lifetime)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			Projectile.velocity = Vector2.Zero;
			Projectile.knockBack = Math.Max(BaseKnockBack, Projectile.knockBack);

			Vector2[] pts = Pts(owner, age);
			if (age == CrackTick && !Main.dedServ)
				SoundEngine.PlaySound(SoundID.Item153 with { Pitch = -0.3f }, pts[Segs]);
			if (age >= CrackTick - 2 && age <= CrackTick + 6)
				Lighting.AddLight(pts[Segs], 0.3f, 0.5f, 0.85f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Aimed)
				return false;
			int age = Age;
			if (age < 9 || age > 28)
				return false;
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;
			Vector2[] pts = Pts(owner, age);
			Vector2[] prev = _prevPts;
			float point = 0f;
			for (int i = 1; i <= Segs; i++)
			{
				float w = i >= Segs - 3 ? 34f : 24f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), pts[i - 1], pts[i], w, ref point))
					return true;
				// 扫掠：同序号控制点上一 tick → 本 tick，防止快速甩动时穿过窄敌人
				if (prev != null && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), prev[i], pts[i], w, ref point))
					return true;
			}
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.Knockback *= 2.2f;
			if (Aimed)
				modifiers.Knockback += 8f; // 原客户端 velocity +14 的推力折进同步的击退
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!Aimed)
				return;
			// 命中环：取目标最近的控制点
			Player owner = Main.player[Projectile.owner];
			Vector2[] pts = Pts(owner, Age);
			Vector2 at = pts[Segs];
			float best = float.MaxValue;
			for (int i = 3; i <= Segs; i++)
			{
				float d = Vector2.DistanceSquared(pts[i], target.Center);
				if (d < best)
				{
					best = d;
					at = pts[i];
				}
			}
			H4Fx.Hit(Projectile, at, H4Fx.StyleDragon, 1.2f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;
			int age = Age;
			Vector2[] pts = Pts(owner, age);
			float opacity = Math.Min(1f, (Lifetime - age) / 6f);

			if (Projectile.ai[1] > 0.5f)
			{
				// 钢尾：自绘 Assets/Fx/IronTail（铁板节 ×2 交替 + 末端尖刺锤）。13 个控制点按 1.5 倍步长重采样为 9 个点（8 节）。
				const int n = 8;
				for (int i = 0; i < n; i++)
				{
					Vector2 a = pts[(int)Math.Round(i * 1.5)];
					Vector2 b = pts[(int)Math.Round((i + 1) * 1.5)];
					Vector2 d = b - a;
					float r = d.LengthSquared() > 0.01f ? d.ToRotation() : Angle;
					bool isTip = i == n - 1;
					H5Fx.Draw("IronTail", 3, isTip ? 2 : i % 2, (a + b) * 0.5f, new Vector2(26f, 18f), r,
						isTip ? 1.3f : 0.7f + i * 0.04f, opacity, Math.Cos(r) < 0.0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
				}
				return false;
			}

			Texture2D tex = H4Fx.Tail();
			int fh = tex.Height / 4;
			Color c = new Color(Math.Max(lightColor.R, (byte)120), Math.Max(lightColor.G, (byte)130), Math.Max(lightColor.B, (byte)150)) * opacity;
			Vector2 origin = new(12f, 10f);
			for (int i = 1; i <= Segs; i++)
			{
				Vector2 d = pts[i] - pts[i - 1];
				float len = d.Length();
				if (len < 0.5f)
					continue;
				float rot = d.ToRotation();
				float taper = 1.25f - 0.65f * (i / (float)Segs); // 根粗梢细
				int frame = i == Segs ? 3 : (i >= Segs - 3 ? 2 : i % 3 == 2 ? 1 : 0);
				SpriteEffects fx = Math.Cos(rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
				Vector2 mid = (pts[i] + pts[i - 1]) * 0.5f;
				Vector2 scale = new(Math.Max(0.35f, len * 1.2f / 24f), taper);
				if (i == Segs)
				{
					mid = pts[i];
					scale = new Vector2(1f, 1f);
				}
				Main.EntitySpriteDraw(tex, (mid - Main.screenPosition).Floor(), new Rectangle(0, fh * frame, tex.Width, fh), c, rot,
					origin, scale, fx, 0f);
			}

			// 「啪」：尾梢处 DragonBurst 5 帧 × 3 tick
			int cf = (age - CrackTick) / 3;
			if (cf >= 0 && cf < 5)
				H4Fx.DrawFrame(H4Fx.Burst(H4Fx.StyleDragon), 5, cf, pts[Segs], Color.White, 0f, cf == 0 ? 1.1f : 0.9f);
			return false;
		}
	}
}
