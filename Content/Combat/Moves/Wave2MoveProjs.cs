using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>粗穿透光束（破坏光线 / 水炮 / 龙之怒 / 日光束发射段）。ai0=Dust。</summary>
	public class ThickBeamProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PurpleLaser;

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 48;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.PurpleTorch;
			Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.9f);
			Dust.NewDustPerfect(Projectile.Center, dust, -Projectile.velocity * 0.05f, 80, default, 1.55f).noGravity = true;
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), dust, Vector2.Zero, 100, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
		}
	}

	/// <summary>指针落点大爆。ai0=Dust，ai1=可选 Buff。</summary>
	public class MouseAoEBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 160;
			Projectile.height = 160;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 18;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(70f, 70f), dust, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1.5f);
				d.noGravity = true;
			}
			if (Projectile.timeLeft == 17)
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
			if (IgnoreDefensePartial)
			{ /* tagged via IHenshinMoveProj */ }
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>日光束蓄力导演：约 0.8s 蓄力后朝鼠标发射 ThickBeam。</summary>
	public class ChargeBeamDirectorProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 55;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.ChlorophyteWeapon;
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(owner.Center + Main.rand.NextVector2CircularEdge(28f, 28f), dust, Vector2.Zero, 100, default, 1.2f).noGravity = true;

			if (Projectile.timeLeft == 8 && Projectile.owner == Main.myPlayer)
			{
				Vector2 dir = Main.MouseWorld - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, dir * 18f,
					ModContent.ProjectileType<ThickBeamProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, dust);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
				{
					tagged.EasyCrit = EasyCrit;
					tagged.IgnoreDefensePartial = IgnoreDefensePartial;
				}
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>落石导演：ai1=数量，落在鼠标附近。</summary>
	public class RockSlideDirectorProj : HenshinMoveProj
	{
		private int _spawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			int total = (int)System.Math.Max(1, Projectile.ai[1]);
			if (_spawned >= total)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.timeLeft % 4 == 0)
			{
				Vector2 spawn = Projectile.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), -180f - Main.rand.NextFloat(40f));
				Vector2 vel = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 12f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<FallingBoulderProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_spawned++;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class FallingBoulderProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Boulder;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.penetrate = 2;
			Projectile.scale = 0.55f;
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.35f;
			Projectile.rotation += 0.15f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Vector2.Zero, 100, new Color(160, 110, 70), 0.9f).noGravity = true;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(4f, 4f), 80, new Color(160, 110, 70), 1.2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 40);
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 20);
		}

		public override Color? GetAlpha(Color lightColor) => new Color(180, 120, 70, 220);
	}

	/// <summary>流星群：从上往鼠标区砸 ai1 枚。</summary>
	public class MeteorBarrageDirectorProj : HenshinMoveProj
	{
		private int _spawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			int total = (int)System.Math.Max(1, Projectile.ai[1]);
			if (_spawned >= total)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.timeLeft % 5 == 0)
			{
				Vector2 target = Projectile.Center + Main.rand.NextVector2Circular(90f, 40f);
				Vector2 spawn = target + new Vector2(Main.rand.NextFloat(-40f, 40f), -220f);
				Vector2 vel = (target - spawn).SafeNormalize(Vector2.UnitY) * 16f;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<MeteorShardProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_spawned++;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class MeteorShardProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.scale = 1.1f;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Vector2.Zero, 80, default, 1.3f).noGravity = true;
			Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Vector2.Zero, 100, default, 0.8f).noGravity = true;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			for (int i = 0; i < 14; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(5f, 5f), 60, default, 1.4f).noGravity = true;
		}
	}

	/// <summary>火焰锥连发：短焰×N。</summary>
	public class FlameConeDirectorProj : HenshinMoveProj
	{
		private int _fired;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 28;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			int total = (int)System.Math.Max(1, Projectile.ai[1]);
			if (Projectile.owner == Main.myPlayer && _fired < total && Projectile.timeLeft % 2 == 0)
			{
				Vector2 dir = Main.MouseWorld - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				float spread = (_fired / (float)total - 0.5f) * 0.55f;
				Vector2 vel = dir.RotatedBy(spread) * 11f;
				int dmg = System.Math.Max(1, Projectile.damage / 4);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + dir * 12f, vel,
					ModContent.ProjectileType<EmberBoltProj>(), dmg, Projectile.knockBack * 0.4f, Projectile.owner);
				_fired++;
			}
			if (_fired >= total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>飞叶快刃：锥形散射×5，射弹与吹叶机相同（ProjectileID.Leaf），绿粒子。</summary>
	public class LeafSpreadProj : HenshinMoveProj
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
		}

		public override void AI()
		{
			if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Player owner = Main.player[Projectile.owner];
				Vector2 dir = Main.MouseWorld - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				int per = System.Math.Max(1, Projectile.damage / 3);
				int leafType = ProjectileBorrow.ItemShoot(ItemID.LeafBlower);
				if (leafType <= 0)
					leafType = ProjectileID.Leaf;
				for (int i = -2; i <= 2; i++)
				{
					Vector2 vel = dir.RotatedBy(i * 0.18f) * 13f;
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, vel,
						leafType, per, Projectile.knockBack, Projectile.owner);
					if (id >= 0)
					{
						Projectile leaf = Main.projectile[id];
						ProjectileBorrow.RetargetAsHenshin(leaf);
						leaf.DamageType = HenshinDamage.Instance;
						leaf.friendly = true;
						leaf.hostile = false;
						for (int d = 0; d < 3; d++)
						{
							Dust dust = Dust.NewDustPerfect(leaf.Center, DustID.Grass, vel * 0.15f + Main.rand.NextVector2Circular(1.2f, 1.2f), 80, new Color(80, 200, 90), 1.15f);
							dust.noGravity = true;
						}
					}
				}
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>种子炸弹：抛物落地爆。</summary>
	public class SeedBombProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Seed;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.scale = 1.4f;
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.25f;
			Projectile.rotation += 0.2f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Grass, Vector2.Zero, 100, default, 1.1f).noGravity = true;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<MouseAoEBurstProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, DustID.Grass);
			}
		}
	}

	/// <summary>身周持续风场。ai1=时长暗示（timeLeft 由工厂设）。</summary>
	public class HurricaneFieldProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 200;
			Projectile.height = 200;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = p.Center;
			for (int i = 0; i < 3; i++)
			{
				Vector2 edge = Main.rand.NextVector2CircularEdge(90f, 90f);
				Dust.NewDustPerfect(Projectile.Center + edge, DustID.Cloud, edge.RotatedBy(1.2f) * 0.08f, 100, new Color(80, 120, 220), 1.3f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 30);
			if (Main.rand.NextBool(4))
				target.AddBuff(BuffID.Confused, 20);
			Vector2 knock = target.Center - Projectile.Center;
			if (knock != Vector2.Zero)
				target.velocity += Vector2.Normalize(knock) * 3.5f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>落雷×3 于鼠标区。</summary>
	public class ThunderPillarUltProj : HenshinMoveProj
	{
		private int _fired;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			if (_fired < 3 && Projectile.timeLeft % 10 == 0)
			{
				Vector2 pos = Projectile.Center + new Vector2((_fired - 1) * 64f, 0f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero,
					ModContent.ProjectileType<ThunderPillarBoltProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_fired++;
			}
			if (_fired >= 3 && Projectile.timeLeft < 5)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class ThunderPillarBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 180;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 20;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			if (Projectile.timeLeft == 19)
				SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);
			for (int i = 0; i < 8; i++)
			{
				float y = Main.rand.NextFloat(-80f, 80f);
				Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), y), DustID.Electric, new Vector2(0f, 4f), 60, default, 1.4f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 150);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>近身战：短时多段贴身打击。</summary>
	public class CloseCombatDirectorProj : HenshinMoveProj
	{
		private int _hits;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 48;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			int dir = p.direction;
			Projectile.Center = p.MountedCenter + new Vector2(dir * 36f, 0f);
			if (Projectile.timeLeft % 8 == 0)
			{
				_hits++;
				SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
				for (int i = 0; i < 6; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.2f).noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>逆鳞：连续爪击导演。</summary>
	public class OutrageDirectorProj : HenshinMoveProj
	{
		private int _swings;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.Center;
			if (Projectile.owner == Main.myPlayer && _swings < 5 && Projectile.timeLeft % 9 == 0)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, Vector2.Zero,
					ModContent.ProjectileType<ScratchSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0)
					Main.projectile[id].ai[0] = DustID.Torch;
				_swings++;
			}
			if (_swings >= 5 && Projectile.timeLeft < 10)
			{
				owner.AddBuff(BuffID.Confused, 120);
				Projectile.Kill();
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>预知未来：蓄力圈后指针 IgnoreDef 爆。</summary>
	public class FutureSightProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f), DustID.MagicMirror, Vector2.Zero, 100, default, 1.3f).noGravity = true;

			if (Projectile.timeLeft == 8 && Projectile.owner == Main.myPlayer)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<MouseAoEBurstProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, DustID.PurpleTorch);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.IgnoreDefensePartial = true;
				SoundEngine.PlaySound(SoundID.Item29, Projectile.Center);
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>恶之波动 / 暗锥：类似啄但暗影。</summary>
	public class DarkPulseConeProj : HenshinMoveProj
	{
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = Main.MouseWorld - owner.Center;
				if (_dir == Vector2.Zero) _dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}
			float t = 1f - Projectile.timeLeft / 14f;
			float reach = 220f * t;
			Projectile.Center = owner.MountedCenter + _dir * reach;
			Projectile.width = (int)(36 + t * 50);
			Projectile.height = Projectile.width;
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Shadowflame, _dir * 2f, 80, default, 1.35f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>抓狂：残血加伤乱打。</summary>
	public class FlailBarrageProj : HenshinMoveProj
	{
		private int _hits;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = p.Center + new Vector2(p.direction * 28f, 0f);
			float missing = 1f - p.statLife / (float)System.Math.Max(1, p.statLifeMax2);
			Projectile.localAI[1] = 1f + missing * 0.8f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Water, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.2f).noGravity = true;
			if (Projectile.timeLeft % 5 == 0)
				_hits++;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= Projectile.localAI[1] > 0 ? Projectile.localAI[1] : 1f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>咬住/咬碎：身前两段咬合；尖牙用 destination Rectangle 画三角（禁 MagicPixel 无源矩形缩放，易拉成通天黑条）。ai0=体型倍率。</summary>
	public class BiteArcProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private int _dir;
		private float _size = 1f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.ownerHitCheck = true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = owner.direction;
				_size = Projectile.ai[0] > 0.1f ? Projectile.ai[0] : 1f;
				Projectile.width = (int)(64 * _size);
				Projectile.height = (int)(56 * _size);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.35f }, owner.Center);
			}
			Projectile.Center = owner.MountedCenter + new Vector2(_dir * (40f + 8f * _size), 0f);

			int phase = Lifetime - Projectile.timeLeft;
			if (phase == 2 || phase == 10)
			{
				for (int i = 0; i < 6; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Blood, new Vector2(_dir * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(-2f, 2f)), 80, default, 1.2f);
			}
		}

		public override bool? CanDamage()
		{
			int age = Lifetime - Projectile.timeLeft;
			if (age <= 6 || (age >= 9 && age <= 15))
				return null;
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff(BuffID.BrokenArmor, (int)Projectile.ai[1]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			float t = 1f - Projectile.timeLeft / (float)Lifetime;
			float close = t < 0.45f ? t / 0.45f : 1f - (t - 0.45f) * 0.35f;
			close = MathHelper.Clamp(close, 0f, 1f);
			float openGap = MathHelper.Lerp(18f, 2f, close) * _size;
			Color fang = new(18, 18, 22, 240);
			Color edge = new(55, 55, 62, 220);

			Vector2 origin = Projectile.Center - Main.screenPosition;
			int teeth = _size >= 1.35f ? 6 : 5;
			float span = 44f * _size;
			for (int i = 0; i < teeth; i++)
			{
				float u = (i + 0.5f) / teeth - 0.5f;
				float x = u * span;
				float toothW = (6f + (i % 2 == 0 ? 2f : 0f)) * _size;
				float toothH = (12f + (i % 2 == 0 ? 3f : 0f)) * _size;
				// 上牙尖向下
				DrawToothTri(pixel, src, origin + new Vector2(x, -openGap), toothW, toothH, fang, edge, tipDown: true);
				// 下牙尖向上
				DrawToothTri(pixel, src, origin + new Vector2(x, openGap), toothW, toothH, fang, edge, tipDown: false);
			}
			return false;
		}

		/// <summary>用多层 destination Rectangle 叠成小三角尖牙（像素尺寸硬封顶，避免通天拉伸）。</summary>
		private static void DrawToothTri(Texture2D pixel, Rectangle src, Vector2 baseCenter, float w, float h, Color fill, Color edge, bool tipDown)
		{
			w = MathHelper.Clamp(w, 3f, 28f);
			h = MathHelper.Clamp(h, 6f, 36f);
			const int layers = 5;
			for (int layer = 0; layer < layers; layer++)
			{
				float k = layer / (float)(layers - 1);
				float layerW = MathHelper.Lerp(w, 1.5f, k);
				float y = tipDown ? baseCenter.Y + k * h : baseCenter.Y - k * h - 1f;
				int rw = System.Math.Max(1, (int)System.Math.Round(layerW));
				int rh = System.Math.Max(1, (int)System.Math.Round(h / layers + 0.6f));
				var rect = new Rectangle((int)System.Math.Round(baseCenter.X - rw * 0.5f), (int)System.Math.Round(y), rw, rh);
				Main.spriteBatch.Draw(pixel, rect, src, layer == 0 ? edge : fill);
			}
		}
	}

	/// <summary>龙之波动：从角色连发 10 枚星云奥秘外观弹（70%、不穿透、不追踪），碰撞后走原版爆炸碎片。</summary>
	public class NebulaPulseDirectorProj : HenshinMoveProj
	{
		private const int Total = 10;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Total + 4;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			Projectile.Center = owner.MountedCenter;
			int fired = (int)Projectile.localAI[0];
			if (fired >= Total)
			{
				Projectile.Kill();
				return;
			}
			// 每帧一发，从角色中心朝指针扇出
			if (Projectile.localAI[1] > 0f)
			{
				Projectile.localAI[1] -= 1f;
				return;
			}
			Projectile.localAI[1] = 1f;

			Vector2 dir = Main.MouseWorld - owner.MountedCenter;
			if (dir.LengthSquared() < 1f)
				dir = new Vector2(owner.direction, 0f);
			dir.Normalize();
			float spread = (fired - (Total - 1) * 0.5f) * 0.06f;
			Vector2 vel = dir.RotatedBy(spread) * 12f;
			int per = System.Math.Max(1, Projectile.damage / 4);
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + dir * 12f, vel,
				ModContent.ProjectileType<NebulaPulseShardProj>(), per, Projectile.knockBack, Projectile.owner);
			if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
				tagged.EasyCrit = EasyCrit;

			Projectile.localAI[0] = fired + 1;
			if (fired + 1 >= Total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 星云奥秘同款外观直飞弹：自管运动（不套原版 AI，避免位置/显隐异常），
	/// 0.7 缩放、穿透 1、无追踪；亡时生成原版 NebulaArcanumExplosionShotShard。
	/// </summary>
	public class NebulaPulseShardProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.NebulaArcanum;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 75;
			Projectile.tileCollide = true;
			Projectile.scale = 0.7f;
			Projectile.extraUpdates = 0;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.NebulaArcanum);
			Main.instance.LoadProjectile(ProjectileID.NebulaArcanumExplosionShotShard);
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Player owner = Main.player[Projectile.owner];
				Vector2 dir = Main.MouseWorld - owner.MountedCenter;
				if (dir.LengthSquared() < 1f)
					dir = new Vector2(owner.direction, 0f);
				Projectile.velocity = Vector2.Normalize(dir) * 12f;
			}
		}

		public override void AI()
		{
			// 锁定初速，禁止任何追踪/漂浮
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.localAI[1] = Projectile.velocity.X;
				Projectile.localAI[2] = Projectile.velocity.Y;
			}
			Projectile.velocity = new Vector2(Projectile.localAI[1], Projectile.localAI[2]);
			Projectile.rotation += 0.2f;
			Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0.75f);
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.08f, 120, default, 1.15f).noGravity = true;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
			Color nebula = new(180, 90, 255);
			for (int i = 0; i < 28; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(6f, 6f);
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 80, nebula, Main.rand.NextFloat(1.2f, 1.8f));
				d.noGravity = true;
				if (Main.rand.NextBool())
					Dust.NewDustPerfect(Projectile.Center, DustID.CrystalPulse, v * 0.6f, 100, nebula, 1.3f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.7f, 0.25f, 1.0f);

			if (Projectile.owner != Main.myPlayer)
				return;
			int shards = 12;
			int shardDmg = System.Math.Max(1, (int)(Projectile.damage * 0.65f));
			for (int i = 0; i < shards; i++)
			{
				Vector2 vel = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.85f, 1.3f);
				int id = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, vel,
					ProjectileID.NebulaArcanumExplosionShotShard, shardDmg, Projectile.knockBack * 0.8f, Projectile.owner);
				if (id < 0)
					continue;
				Projectile p = Main.projectile[id];
				ProjectileBorrow.RetargetAsHenshin(p);
				p.scale *= 0.7f;
				p.DamageType = HenshinDamage.Instance;
				p.penetrate = 1;
				p.GetGlobalProjectile<HenshinNebulaShardTintGlobal>().TintPurple = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.NebulaArcanum);
			Color tint = new(210, 140, 255);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, tint,
				Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>龙之波动爆炸碎片强制紫染色（原版 shard 在光照下偏棕）。</summary>
	public sealed class HenshinNebulaShardTintGlobal : GlobalProjectile
	{
		public override bool InstancePerEntity => true;
		public bool TintPurple;

		public override Color? GetAlpha(Projectile projectile, Color lightColor)
		{
			if (!TintPurple || projectile.type != ProjectileID.NebulaArcanumExplosionShotShard)
				return null;
			return new Color(200, 110, 255, 220);
		}

		public override bool PreDraw(Projectile projectile, ref Color lightColor)
		{
			if (!TintPurple || projectile.type != ProjectileID.NebulaArcanumExplosionShotShard)
				return true;
			Main.instance.LoadProjectile(projectile.type);
			Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[projectile.type].Value;
			Color c = new(210, 120, 255, 230);
			Main.EntitySpriteDraw(tex, projectile.Center - Main.screenPosition, null, c,
				projectile.rotation, tex.Size() * 0.5f, projectile.scale, SpriteEffects.None);
			// 追加紫尘，盖住原版棕焰感
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(projectile.Center, DustID.PurpleTorch, projectile.velocity * 0.1f, 100, new Color(180, 90, 255), 1.2f).noGravity = true;
			return false;
		}
	}

	/// <summary>龙息短锥雾。</summary>
	public class DragonBreathConeProj : HenshinMoveProj
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
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = Main.MouseWorld - owner.Center;
				if (_dir == Vector2.Zero) _dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}
			float t = 1f - Projectile.timeLeft / 16f;
			Projectile.Center = owner.MountedCenter + _dir * (40f + t * 100f);
			Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, _dir * 1.5f, 100, default, 1.3f).noGravity = true;
			Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, _dir, 100, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 30);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>花瓣舞环身。</summary>
	public class PetalDanceFieldProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 140;
			Projectile.height = 140;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 72;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = p.Center;
			Dust.NewDustPerfect(p.Center + Main.rand.NextVector2CircularEdge(55f, 55f), DustID.Firework_Pink, Vector2.Zero, 80, default, 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>掷泥弹。</summary>
	public class MudSlapBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DirtBall;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
		}

		public override void AI()
		{
			Projectile.rotation += 0.2f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, Vector2.Zero, 100, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 120);
		}
	}

	/// <summary>短十字劈（无前冲）。ai2=0 短，CrossChopArc 负责长。</summary>
	public class CrossChopShortProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			int dir = owner.direction;
			Projectile.Center = owner.MountedCenter + new Vector2(dir * 42f, 0f);
			for (int arm = -1; arm <= 1; arm += 2)
			{
				for (int i = 0; i < 3; i++)
				{
					float t = i / 3f;
					Vector2 pos = Projectile.Center + new Vector2(dir * (t * 28f - 10f), arm * (t * 28f - 14f));
					Dust.NewDustPerfect(pos, DustID.Blood, new Vector2(dir, arm) * 1.5f, 80, default, 1.25f).noGravity = true;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>三连刺。</summary>
	public class TripleStabProj : HenshinMoveProj
	{
		private int _n;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 24;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.Center;
			if (Projectile.owner == Main.myPlayer && _n < 3 && Projectile.timeLeft % 7 == 0)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, Vector2.Zero,
					ModContent.ProjectileType<GenericSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, DustID.Dirt);
				_n++;
			}
			if (_n >= 3)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>劈瓦：对高防额外伤。</summary>
	public class BrickBreakProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 12;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
			Projectile.ownerHitCheck = true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter + new Vector2(owner.direction * 36f, 8f);
			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, new Vector2(0f, 2f), 80, default, 1.2f).noGravity = true;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (target.defense >= 20)
				modifiers.FinalDamage *= 1.25f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>指针水/沙漩涡（潮旋 / 流沙）。</summary>
	public class MouseVortexProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 18;
		}

		public override void AI()
		{
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Water;
			Vector2 offset = Main.rand.NextVector2CircularEdge(28f, 28f);
			Dust.NewDustPerfect(Projectile.Center + offset, dust, offset.RotatedBy(1.5f) * 0.05f, 100, default, 1.35f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 90);
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
			Projectile.velocity *= 0.1f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>爆裂拳：短 Lunge + Stun。</summary>
	public class DynamicPunchProj : HenshinMoveProj
	{
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
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
				if (_dir == Vector2.Zero) _dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
			}
			if (Projectile.owner == Main.myPlayer)
			{
				p.velocity = _dir * 12f;
				if (Projectile.timeLeft >= 8)
				{
					p.immune = true;
					p.immuneTime = System.Math.Max(p.immuneTime, 12);
				}
			}
			Projectile.Center = p.Center;
			Dust.NewDustPerfect(p.Center, DustID.Blood, -_dir * 2f, 80, default, 1.3f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 45);
			target.velocity *= 0.15f;
			SoundEngine.PlaySound(SoundID.Item14, target.Center);
			for (int i = 0; i < 16; i++)
				Dust.NewDustPerfect(target.Center, DustID.Smoke, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.4f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>中功率穿透电（雷丘技能）。</summary>
	public class MidThunderBeamProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagnetSphereBolt;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 2;
			Projectile.scale = 1.3f;
		}

		public override void AI()
		{
			Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Vector2.Zero, 80, default, 1.3f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 90);
		}
	}

	/// <summary>强化暗影球。</summary>
	public class BigShadowBallProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamHostile;

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.penetrate = 2;
			Projectile.scale = 1.5f;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Vector2.Zero, 100, default, 1.3f).noGravity = true;
			Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.5f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
		}
	}

	/// <summary>污泥可见毒弹。</summary>
	public class SludgeBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ToxicBubble;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.scale = 1.2f;
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.12f;
			Dust.NewDustPerfect(Projectile.Center, DustID.CorruptGibs, Vector2.Zero, 100, default, 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Poisoned, 240);
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.CorruptGibs, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.2f);
		}
	}

	/// <summary>强化催眠波。</summary>
	public class HypnosisWaveProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 160;
			Projectile.height = 120;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 36;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			int dir = p.direction;
			Projectile.Center = p.Center + new Vector2(dir * 50f, 0f);
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 40f), DustID.Shadowflame, Vector2.Zero, 150, default, 1.2f).noGravity = true;
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(40f, 30f), DustID.MagicMirror, Vector2.Zero, 150, default, 1.0f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (target.boss)
			{
				target.AddBuff(BuffID.Slow, 120);
				target.velocity *= 0.25f;
			}
			else
			{
				target.AddBuff(BuffID.Slow, 300);
				target.velocity *= 0.05f;
				target.AddBuff(BuffID.Confused, 180);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 0.2f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>舌舔扇形 Stun。</summary>
	public class LickFanProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Projectile.Center = owner.MountedCenter + new Vector2(owner.direction * 38f, 4f);
			Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, new Vector2(owner.direction * 2f, 0f), 120, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 40);
			target.velocity *= 0.2f;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 0.7f;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>空气爆炸 EasyCrit。</summary>
	public class AirBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 80;
			Projectile.height = 80;
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
			if (Projectile.timeLeft == 11)
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(35f, 35f), DustID.Cloud, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.4f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>尖石可见弹。</summary>
	public class StoneEdgeBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Boulder;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.scale = 0.35f;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Vector2.Zero, 100, default, 0.9f).noGravity = true;
		}

		public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 210, 220);
	}

	/// <summary>挖洞大招强化出土爆。</summary>
	public class DigUltBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 120;
			Projectile.height = 80;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 20;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			Projectile.Center = p.Center;
			if (Projectile.timeLeft == 19)
			{
				SoundEngine.PlaySound(SoundID.Item14, p.Center);
				p.velocity.Y = -10f;
			}
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 30f), DustID.Dirt, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.4f);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>地裂波。</summary>
	public class QuakeWaveProj : HenshinMoveProj
	{
		private float _reach;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 28;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			_reach += 14f;
			Projectile.Center = p.Center;
			Projectile.width = (int)(40 + _reach);
			Projectile.height = 48;
			for (int i = 0; i < 4; i++)
			{
				float x = Main.rand.NextFloat(-_reach, _reach);
				Dust.NewDustPerfect(p.Center + new Vector2(x, 16f), DustID.Stone, new Vector2(0f, -2f), 80, default, 1.3f);
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>共鸣强弹（技能强念）。</summary>
	public class StrongPsychicBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.RainbowRodBullet;

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.penetrate = 2;
			Projectile.scale = 1.25f;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, 0.12f);
			Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, Vector2.Zero, 80, new Color(220, 120, 255), 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 200);
		}

		public override Color? GetAlpha(Color lightColor) => new Color(230, 140, 255, 200);
	}
}
