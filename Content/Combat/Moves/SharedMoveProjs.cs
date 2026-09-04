using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	public static class HenshinProjUtil
	{
		public static void HomingAI(Projectile proj, bool homing, float turnRate)
		{
			if (!homing || proj.velocity.LengthSquared() < 0.01f)
				return;
			NPC target = null;
			float best = 480f * 480f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = proj.DistanceSQ(n.Center);
				if (d < best)
				{
					best = d;
					target = n;
				}
			}
			if (target == null)
				return;
			Vector2 desired = target.Center - proj.Center;
			desired.Normalize();
			float speed = proj.velocity.Length();
			proj.velocity = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(proj.velocity), desired, turnRate)) * speed;
		}
	}

	public abstract class HenshinMoveProj : ModProjectile, IHenshinMoveProj
	{
		public bool EasyCrit { get; set; }
		public bool Homing { get; set; }
		public float HomingTurnRate { get; set; } = 0.08f;
		public bool IgnoreDefensePartial { get; set; }

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (IgnoreDefensePartial)
				modifiers.DefenseEffectiveness *= 0.25f;
		}
	}

	public class GenericSlashProj : HenshinMoveProj
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

	public class GenericBoltProj : HenshinMoveProj
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
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
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
			if (Projectile.ai[2] > 0)
				target.AddBuff(BuffID.Slow, (int)Projectile.ai[2]);
		}

		public override void OnKill(int timeLeft) => SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
	}

	public class WaterBoltHenshinProj : HenshinMoveProj
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
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 100, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
			if (Projectile.ai[2] > 0)
				target.AddBuff(BuffID.Slow, (int)Projectile.ai[2]);
		}
	}

	public class ThunderBoltHenshinProj : HenshinMoveProj
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
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0f, 0f, 100, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 120);
		}
	}

	public class ShadowBallHenshinProj : HenshinMoveProj
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
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
		}
	}

	public class RainFieldProj : HenshinMoveProj
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
			var tag = Projectile.ai[0] > 0
				? (WeatherField.WeatherTag)(byte)Projectile.ai[0]
				: WeatherField.WeatherTag.Rain;
			if (Projectile.owner == Main.myPlayer || Main.netMode == NetmodeID.Server)
			{
				WeatherField.WeatherFieldSystem.TrySpawn(
					Main.player[Projectile.owner].Center,
					tag,
					(byte)Projectile.owner);
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class PhaseTriggerProj : HenshinMoveProj
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

	public class DigBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
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

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>落雷 / 定点打击。</summary>
	public class StrikeFallProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 80;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 18;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 18;
		}

		public override void AI()
		{
			if (Projectile.ai[0] == 0f && Projectile.owner == Main.myPlayer)
			{
				Projectile.Center = Main.MouseWorld;
				Projectile.ai[0] = 1f;
				Projectile.netUpdate = true;
			}
			int dust = Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : DustID.Electric;
			for (int i = 0; i < 3; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 32f), dust, Vector2.UnitY * 2f, 80, default, 1.4f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>漩涡缠绕：附着目标 DoT + 减速。</summary>
	public class VortexBindProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, 0.15f);
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			Vector2 offset = Main.rand.NextVector2CircularEdge(20f, 20f);
			Dust.NewDustPerfect(Projectile.Center + offset, dust, offset.SafeNormalize(Vector2.UnitY) * -1.5f, 100, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 60);
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
			Projectile.Center = target.Center;
			Projectile.velocity *= 0.2f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>突进伤害盒：全程维持含竖直方向的速度。</summary>
	public class LungeProj : HenshinMoveProj
	{
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 16;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 16;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = Main.MouseWorld - p.Center;
				if (_dir == Vector2.Zero)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
			}

			// 每帧重写速度，避免重力吃掉竖直分量（速度/距离约为原 50%）
			if (Projectile.owner == Main.myPlayer)
				p.velocity = _dir * 8.5f;

			Projectile.Center = p.Center;
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Cloud;
			Dust.NewDustPerfect(p.Center, dust, -p.velocity * 0.2f, 100, default, 1.2f).noGravity = true;

			// 突进前段短无敌
			if (Projectile.timeLeft >= 10 && Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = System.Math.Max(p.immuneTime, 15);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Smoke;
			SoundEngine.PlaySound(SoundID.Item14, target.Center);
			for (int i = 0; i < 16; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, dust, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.45f);
				d.noGravity = Main.rand.NextBool();
			}
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(target.Center, DustID.Smoke, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.3f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class BeamBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PurpleLaser;

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 5;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 2;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate * 0.5f);
			Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.8f);
			Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 1.3f).noGravity = true;
		}
	}

	public class AoEBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 12;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			Projectile.Center = p.Center;
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Water;
			for (int i = 0; i < 4; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), dust, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.3f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Vector2 knock = target.Center - Main.player[Projectile.owner].Center;
			if (knock != Vector2.Zero)
				target.velocity += Vector2.Normalize(knock) * 6f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class SelfGuardProj : HenshinMoveProj
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
			Projectile.damage = 0;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (p.active)
			{
				p.AddBuff(BuffID.Ironskin, 180);
				var hp = p.GetModPlayer<PlayerState.HenshinPlayer>();
				hp.GuardBonusTimer = 180;
				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(p.Center, DustID.MagicMirror, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.2f).noGravity = true;
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class SleepWaveProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 120;
			Projectile.height = 120;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 30;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI()
		{
			Projectile.Center = Main.player[Projectile.owner].Center;
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.Shadowflame, Vector2.Zero, 150, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 180);
			target.velocity *= 0.2f;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 0.15f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}
}
