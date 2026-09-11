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
	/// 龙尾：仅星尘龙身/尾节鞭弧（15 格），无头节；挥扫方向修正；极强击退。
	/// ai0 = Dust（可选，默认 Cloud）；ai1≥0.5 = 钢尾铁色遮罩。
	/// </summary>
	public class DragonTailWhipProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private const int SwingTicks = 16;
		private const float Length = 15f * 16f;
		private const float LineWidth = 28f;
		private const int SegmentCount = 8;
		private const float BaseKnockBack = 22f;

		private Vector2 _aim;
		private int _facing = 1;
		private bool _initialized;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.StardustDragon4;

		public override void SetDefaults()
		{
			Projectile.width = 32;
			Projectile.height = 32;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.knockBack = BaseKnockBack;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			if (!_initialized)
			{
				_initialized = true;
				Main.instance.LoadProjectile(ProjectileID.StardustDragon2);
				Main.instance.LoadProjectile(ProjectileID.StardustDragon3);
				Main.instance.LoadProjectile(ProjectileID.StardustDragon4);

				_aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_aim.LengthSquared() < 1f)
					_aim = new Vector2(owner.direction, 0f);
				_aim.Normalize();
				_facing = _aim.X >= 0f ? 1 : -1;
				if (Math.Abs(_aim.X) < 0.05f)
					_facing = owner.direction;

				Projectile.knockBack = Math.Max(BaseKnockBack, Projectile.knockBack);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.25f }, owner.MountedCenter);
			}

			Projectile.Center = owner.MountedCenter;
			Projectile.velocity = Vector2.Zero;
			Projectile.knockBack = Math.Max(BaseKnockBack, Projectile.knockBack);

			int dustId = Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : DustID.Cloud;
			Vector2[] path = BuildPath(owner.MountedCenter);
			if (path.Length >= 2 && Main.rand.NextBool(2))
			{
				int idx = Main.rand.Next(1, path.Length);
				Dust d = Dust.NewDustPerfect(path[idx], dustId, _aim * 1.5f + Main.rand.NextVector2Circular(1.2f, 1.2f),
					100, new Color(180, 210, 255), 1.15f);
				d.noGravity = true;
			}

			Lighting.AddLight(owner.MountedCenter + _aim * (Length * 0.45f), 0.25f, 0.45f, 0.7f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;

			Vector2[] path = BuildPath(owner.MountedCenter);
			for (int i = 0; i < path.Length - 1; i++)
			{
				float _ = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
					    path[i], path[i + 1], LineWidth, ref _))
					return true;
			}
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.Knockback *= 2.2f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.velocity += _aim * 14f;
			int dustId = Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : DustID.Cloud;
			for (int i = 0; i < 5; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, dustId, Main.rand.NextVector2Circular(3.5f, 3.5f),
					80, new Color(170, 200, 255), 1.25f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;

			Vector2[] path = BuildPath(owner.MountedCenter);
			if (path.Length < 2)
				return false;

			float life = Projectile.timeLeft / (float)Lifetime;
			bool iron = Projectile.ai[1] > 0.5f;
			// 钢尾底色仍用龙尾色，再叠指定灰罩；非铁尾用原青白
			Color tint = Color.Lerp(new Color(140, 190, 255), Color.White, 0.35f) * life;

			// 仅尾/身：近根用尾(4)，中段身(2/3)交替，尖端再用尾 — 不用头(1)
			int[] segTypes =
			{
				ProjectileID.StardustDragon4,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon4
			};

			int drawCount = Math.Min(SegmentCount, path.Length - 1);
			for (int i = 0; i < drawCount; i++)
			{
				Vector2 a = path[i];
				Vector2 b = path[i + 1];
				Vector2 mid = (a + b) * 0.5f;
				Vector2 delta = b - a;
				float rot = delta.LengthSquared() > 0.01f ? delta.ToRotation() : _aim.ToRotation();

				int texId = segTypes[Math.Min(i, segTypes.Length - 1)];
				Texture2D tex = ProjectileBorrow.RequestProjectileTexture(texId);
				int frames = Math.Max(1, Main.projFrames[texId]);
				Rectangle src = tex.Frame(1, frames, 0, 0);
				float scale = 0.85f + i * 0.03f;

				Main.EntitySpriteDraw(tex, mid - Main.screenPosition, src, tint,
					rot + MathHelper.PiOver2, src.Size() * 0.5f, scale, SpriteEffects.None);

				// 钢尾：叠加 Color(0,0,16,80) 灰罩
				if (iron)
				{
					Color mask = new Color(0, 0, 16, 80);
					Main.EntitySpriteDraw(tex, mid - Main.screenPosition, src, mask,
						rot + MathHelper.PiOver2, src.Size() * 0.5f, scale, SpriteEffects.None);
				}
			}

			HenshinFxDraw.BeginAdditive();
			Vector2 tip = path[path.Length - 1];
			Color glow = iron ? new Color(120, 125, 140) : new Color(120, 180, 255);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, tip,
				HenshinFxDraw.WithAlpha(glow, 0.45f * life), 0.55f);
			HenshinFxDraw.EndAdditive();

			return false;
		}

		/// <summary>鞭弧：从身前外侧甩向瞄准方向（修正原先反方向）。</summary>
		private Vector2[] BuildPath(Vector2 origin)
		{
			float age = Lifetime - Projectile.timeLeft;
			float swingT = MathHelper.Clamp(age / (float)SwingTicks, 0f, 1f);
			float eased = swingT * swingT * (3f - 2f * swingT);

			// 反转：由身前外侧扫回瞄准轴（原先 back→front 读感反了）
			float start = 0.95f;
			float end = -0.15f;
			float tipAng = MathHelper.Lerp(start, end, eased) * _facing;

			var points = new Vector2[SegmentCount + 1];
			for (int i = 0; i <= SegmentCount; i++)
			{
				float u = i / (float)SegmentCount;
				float localAng = tipAng * (0.25f + 0.75f * u);
				Vector2 dir = _aim.RotatedBy(localAng);
				Vector2 perp = new Vector2(-dir.Y, dir.X);
				float bulge = (float)Math.Sin(u * MathHelper.Pi) * 18f * (1f - eased * 0.4f) * _facing;
				points[i] = origin + dir * (Length * u) + perp * bulge * 0.2f;
			}
			return points;
		}
	}
}
