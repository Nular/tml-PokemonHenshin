using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>通用近战挥击（复用空贴图 + Dust）。ai[0]=DustID。</summary>
	public class GenericSlashProj : ModProjectile
	{
		private const int Lifetime = 12;
		private const int Reach = 32;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 44;
			Projectile.height = 44;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
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
			Projectile.Center = owner.MountedCenter + new Vector2(dir * Reach, -4f);
			Projectile.velocity = Vector2.Zero;

			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Smoke;
			if (Projectile.timeLeft > Lifetime - 8)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), dust, new Vector2(dir * 2f, 0f), 100, default, 1.1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>通用弹道。Texture 复用原版投射物 ID（ai[2] 存贴图用 ProjectileID，默认火球）。</summary>
	public class GenericBoltProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 140;
			Projectile.tileCollide = true;
			Projectile.light = 0.4f;
		}

		public override void AI()
		{
			Projectile.rotation += 0.2f;
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, 1.2f);
			d.noGravity = true;
			d.velocity *= 0.3f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
		}
	}

	/// <summary>水系弹，贴图复用水栓。</summary>
	public class WaterBoltHenshinProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WaterBolt;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = true;
		}

		public override void AI()
		{
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 100, default, 1.1f).noGravity = true;
		}
	}

	/// <summary>电系弹。</summary>
	public class ThunderBoltHenshinProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagnetSphereBolt;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.light = 0.6f;
		}

		public override void AI()
		{
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0f, 0f, 100, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 120);
		}
	}

	/// <summary>影球。</summary>
	public class ShadowBallHenshinProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamHostile;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 160;
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150, default, 1.1f).noGravity = true;
		}
	}

	/// <summary>雨天气场触发弹（瞬发，生成场）。</summary>
	public class RainFieldProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.damage = 0;
		}

		public override void AI()
		{
			if (Projectile.owner == Main.myPlayer || Main.netMode == NetmodeID.Server)
			{
				WeatherField.WeatherFieldSystem.TrySpawn(
					Main.player[Projectile.owner].Center,
					WeatherField.WeatherTag.Rain,
					(byte)Projectile.owner);
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>穿障触发（无伤害）。</summary>
	public class PhaseTriggerProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (p.active)
				p.GetModPlayer<PlayerState.HenshinPlayer>().TryStartPhasing();
			for (int i = 0; i < 12; i++)
			{
				Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(3f, 3f), 150, default, 1.2f);
				d.noGravity = true;
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>挖掘：朝向准星挖一格。</summary>
	public class DigBurstProj : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.timeLeft = 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = p.MountedCenter + Vector2.Normalize(Main.MouseWorld - p.MountedCenter) * 32f;
			if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 7)
			{
				int tx = (int)(Projectile.Center.X / 16f);
				int ty = (int)(Projectile.Center.Y / 16f);
				p.GetModPlayer<TerrainEdit.TerrainBudgetPlayer>().TryMineTile(tx, ty);
			}
			Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.2f).noGravity = true;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}
}
