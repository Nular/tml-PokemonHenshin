using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 放电（雷丘 Discharge）：以自身为中心向 8 个方向放出电弧，一次施放同怪只命中一次。
	/// 贴图 Assets/Fx/Discharge/Discharge.png：4 帧竖排，原点 = 左端中线（art 3,11）。
	/// 方向固定（k*45°+22.5°），不依赖鼠标，联机无需同步瞄准。
	/// </summary>
	public class H2DischargeProj : HenshinMoveProj
	{
		private const int Lifetime = 16;
		private const float Reach = 12f * 16f;
		private const int Arcs = 8;
		private static Asset<Texture2D> _sheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.ownerHitCheck = false;
		}

		private static Vector2 ArcDir(int k) => (MathHelper.TwoPi * (k + 0.5f) / Arcs).ToRotationVector2();

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.1f }, owner.Center);
			}
			Lighting.AddLight(Projectile.Center, 0.9f, 0.6f, 0.1f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 o = Main.player[Projectile.owner].MountedCenter;
			for (int k = 0; k < Arcs; k++)
			{
				float _ = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), o, o + ArcDir(k) * Reach, 26f, ref _))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 90);
			if (Main.dedServ)
				return;
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(target.Center, DustID.YellowTorch, Main.rand.NextVector2Circular(3f, 3f), 80, HenshinFxDraw.SparkGold, 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			Texture2D tex = (_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Discharge/Discharge", AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 4;
			int age = Lifetime - Projectile.timeLeft;
			float fade = Math.Min(1f, Projectile.timeLeft / 4f);
			float grow = Math.Min(1f, (age + 1) / 4f);
			Vector2 o = (owner.MountedCenter - Main.screenPosition).Floor();
			for (int k = 0; k < Arcs; k++)
			{
				Rectangle src = new(0, fh * ((age / 2 + k) % 4), tex.Width, fh);
				Vector2 d = ArcDir(k);
				SpriteEffects fx = d.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
				float sc = Reach / (tex.Width - 8f) * grow;
				Main.EntitySpriteDraw(tex, o, src, Color.White * fade, d.ToRotation(), new Vector2(6f, fh * 0.5f), sc, fx, 0f);
			}
			return false;
		}
	}

	/// <summary>纯视觉命中星（0 伤害）：FlurryHit 4 帧，ai0 = 缩放（0 = 1），ai1 = 旋转，ai2 = G1HitSfx 命中音码。</summary>
	public class H2HitFxProj : ModProjectile
	{
		private const int Lifetime = 8;
		private static Asset<Texture2D> _sheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
		}

		public override bool? CanDamage() => false;

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			// ai2 = G1HitSfx 命中音码：随生成包同步，首帧各端播。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				G1HitSfx.Play(Projectile.ai[2], Projectile.Center);
			}
			float sc = Projectile.ai[0] > 0.01f ? Projectile.ai[0] : 1f;
			HenshinExplosion.Pulse(Projectile.Center, new Color(255, 200, 140), BlastSize.S, (Lifetime - Projectile.timeLeft) / (float)Lifetime, 48f * sc, 0.6f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = (_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/FlurryHit/FlurryHit", AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 4;
			int frame = Math.Min(3, (Lifetime - Projectile.timeLeft) / 2);
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			float sc = Projectile.ai[0] > 0.01f ? Projectile.ai[0] : 1f;
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), src, Color.White, Projectile.ai[1], src.Size() * 0.5f, sc, SpriteEffects.None, 0f);
			return false;
		}

		/// <summary>仅拥有者端调用；射弹由 Terraria 正常同步给旁观者。</summary>
		public static void Spawn(Projectile src, Vector2 at, float scale, float rot, float sfx = 0f)
		{
			if (src.owner != Main.myPlayer)
				return;
			int id = Projectile.NewProjectile(src.GetSource_FromThis(), at, Vector2.Zero, ModContent.ProjectileType<H2HitFxProj>(), 0, 0f, src.owner, scale, rot, sfx);
			if (id >= 0)
				Main.projectile[id].Center = at;
		}
	}
	/// <summary>H2 组（雷丘/豪力/怪力）新招 MoveSpec。</summary>
	internal static class H2Moves
	{
		/// <summary>放电：自身 8 向电弧 12 格，一次施放同怪只吃一段；感电 1.5s。WideAoE。</summary>
		public static MoveSpec Discharge(string nameKey, float mult = 1.3f)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<H2DischargeProj>(),
				DamageMultiplier = mult,
				UseTime = 30,
				ShootSpeed = 0f,
				Knockback = 3f,
				BalanceTag = BalanceTag.WideAoE
			};
	}
}
