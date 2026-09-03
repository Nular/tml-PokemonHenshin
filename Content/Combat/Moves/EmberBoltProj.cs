using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 火花弹（招式 B 占位）：小型火球，微重力、命中附着火。
	/// 贴图复用原版火球（BallofFire），尾迹复用原版 Torch Dust —— 不新增任何 FX 图片。
	/// </summary>
	public class EmberBoltProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 4;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
			Projectile.aiStyle = -1;
			Projectile.light = 0.5f;
		}

		public override void AI()
		{
			Projectile.rotation += 0.25f * Projectile.direction;
			Projectile.velocity.Y += 0.08f;
			if (Projectile.velocity.Y > 12f)
				Projectile.velocity.Y = 12f;

			if (Projectile.wet)
			{
				Extinguish();
				return;
			}

			Lighting.AddLight(Projectile.Center, 0.9f, 0.45f, 0.1f);

			for (int i = 0; i < 2; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
					Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.4f);
				d.noGravity = true;
				d.velocity *= 0.5f;
			}
			if (Main.rand.NextBool(4))
			{
				Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -0.5f, 150, default, 0.8f);
				smoke.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 180);
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
			for (int i = 0; i < 12; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 80, default, 1.6f);
				d.noGravity = true;
				d.velocity = Main.rand.NextVector2Circular(3f, 3f);
			}
		}

		private void Extinguish()
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -1f, 150, default, 1f);
				d.noGravity = true;
			}
			Projectile.Kill();
		}
	}
}
