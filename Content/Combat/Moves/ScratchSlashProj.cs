using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 爪击（招式 A 占位）：跟随玩家、朝向前方的短命近身命中盒。
	/// 不绘制自身贴图（复用原版空贴图 Projectile_0），观感全靠原版 Dust —— 不新增任何 FX 图片。
	/// </summary>
	public class ScratchSlashProj : ModProjectile
	{
		private const int Lifetime = 12;
		private const int Reach = 30;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
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

			// 首帧锁定朝向；随后跟随玩家。
			if (Projectile.ai[0] == 0f)
				Projectile.ai[0] = owner.direction;
			int dir = Projectile.ai[0] >= 0f ? 1 : -1;

			Projectile.Center = owner.MountedCenter + new Vector2(dir * Reach, -4f);
			Projectile.velocity = Vector2.Zero;

			// 前 8 帧刷爪痕尘：三道斜线，自上而下。
			if (Projectile.timeLeft > Lifetime - 8)
			{
				float t = (Lifetime - Projectile.timeLeft) / 8f;
				for (int line = -1; line <= 1; line++)
				{
					Vector2 pos = Projectile.Center + new Vector2(dir * (line * 6f - 10f + t * 24f), line * 8f - 14f + t * 28f);
					Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, new Vector2(dir * 1.5f, 0.6f), 120, Color.White, 0.9f);
					d.noGravity = true;
					d.fadeIn = 0.6f;
				}
				if (Main.rand.NextBool(2))
				{
					Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.Torch, new Vector2(dir * 2f, -1f), 0, default, 1.1f);
					spark.noGravity = true;
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Blood, hit.HitDirection * 2f, -1f);
				d.noGravity = false;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}
}
