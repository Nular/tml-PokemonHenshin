using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 爪击：身前三条平行下滑爪痕；命中盒覆盖三线宽度。
	/// </summary>
	public class ScratchSlashProj : HenshinMoveProj
	{
		private const int Lifetime = 14;
		private const int Reach = 48;
		private const float LineSpacing = 16f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.ai[1] == 0f)
				Projectile.ai[1] = owner.direction;
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;

			Projectile.Center = owner.MountedCenter + new Vector2(dir * Reach, 0f);
			Projectile.velocity = Vector2.Zero;

			if (Projectile.timeLeft > Lifetime - 10)
			{
				float t = (Lifetime - Projectile.timeLeft) / 10f;
				// 三条平行线：竖直等距，同步向前下滑
				for (int line = -1; line <= 1; line++)
				{
					float yBase = line * LineSpacing;
					for (int seg = 0; seg < 3; seg++)
					{
						float along = -18f + t * 40f + seg * 8f;
						Vector2 pos = Projectile.Center + new Vector2(dir * along, yBase + t * 6f);
						Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, new Vector2(dir * 2.4f, 0.3f), 80, Color.White, 1.35f);
						d.noGravity = true;
						d.fadeIn = 0.9f;
						Dust ember = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(dir * 1.4f, 0.2f), 100, default, 1.2f);
						ember.noGravity = true;
					}
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			Vector2 origin = owner.MountedCenter + new Vector2(dir * (Reach - 20f), 0f);
			Vector2 tip = owner.MountedCenter + new Vector2(dir * (Reach + 28f), 0f);
			for (int line = -1; line <= 1; line++)
			{
				Vector2 o = origin + new Vector2(0f, line * LineSpacing);
				Vector2 e = tip + new Vector2(0f, line * LineSpacing);
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), o, e, 14f, ref point))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Blood, hit.HitDirection * 2.5f, -1.2f);
				d.noGravity = false;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}
}
