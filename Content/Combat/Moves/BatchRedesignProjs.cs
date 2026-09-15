using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Buffs;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>臂锤：身前小臂弧线砸下。</summary>
	public class ArmHammerSmashProj : HenshinMoveProj
	{
		private const int Life = 28;
		private Vector2 _anchor;
		private Vector2 _dir;
		private bool _smashed;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
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
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.MountedCenter;
				if (_dir.LengthSquared() < 1f) _dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				_anchor = p.MountedCenter + _dir * 56f;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.25f }, p.Center);
			}

			float t = 1f - Projectile.timeLeft / (float)Life;
			float raise = t < 0.35f
				? MathHelper.Lerp(-48f, -72f, t / 0.35f)
				: MathHelper.Lerp(-72f, 28f, MathHelper.Clamp((t - 0.35f) / 0.4f, 0f, 1f));
			float swing = t < 0.35f
				? MathHelper.Lerp(-1.1f, -1.35f, t / 0.35f)
				: MathHelper.Lerp(-1.35f, 0.55f, MathHelper.Clamp((t - 0.35f) / 0.45f, 0f, 1f));

			Projectile.Center = _anchor + new Vector2(0f, raise);
			Projectile.rotation = _dir.ToRotation() + swing * p.direction;

			if (!_smashed && t >= 0.55f)
			{
				_smashed = true;
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.1f, Volume = 0.55f }, Projectile.Center);
				for (int i = 0; i < 18; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Iron, Main.rand.NextVector2Circular(6f, 4f), 80, default, 1.35f).noGravity = true;
			}
		}

		public override bool? CanDamage() => _smashed ? null : false;

		public override bool PreDraw(ref Color lightColor)
		{
			float life = MathHelper.Clamp(1f - Projectile.timeLeft / (float)Life, 0.2f, 1f);
			int frame = HenshinFxDraw.AgeFrame(Life, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(210, 220, 235), 0.85f * life),
				1.35f, Projectile.rotation + MathHelper.Pi, frame);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(180, 200, 220), 0.35f * life), 0.7f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>金属爪：鼠标两侧两道极深铁色抓痕。</summary>
	public class MetalClawDirectorProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 12;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] != 0f || Projectile.owner != Main.myPlayer)
				return;
			Projectile.localAI[0] = 1f;

			Vector2 at = Projectile.Center;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - Main.player[Projectile.owner].Center;
			if (aim.LengthSquared() < 1f) aim = new Vector2(Main.player[Projectile.owner].direction, 0f);
			aim.Normalize();
			Vector2 side = new(-aim.Y, aim.X);
			SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.2f }, at);

			for (int i = -1; i <= 1; i += 2)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at + side * (i * 22f), aim * 0.01f,
					ModContent.ProjectileType<MetalClawSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.Delivery = MoveDelivery.MeleeArc;
				}
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class MetalClawSlashProj : HenshinMoveProj
	{
		private const int Life = 16;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 88;
			Projectile.height = 64;
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
				Projectile.rotation = Projectile.velocity.ToRotation();
				if (Projectile.velocity.LengthSquared() < 0.01f)
					Projectile.rotation = Main.player[Projectile.owner].direction > 0 ? 0f : MathHelper.Pi;
			}
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 10f), DustID.Iron,
				Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 70, new Color(160, 180, 200), 1.4f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / (float)Life;
			int frame = HenshinFxDraw.AgeFrame(Life, Projectile.timeLeft, 2, HenshinFxDraw.HitJaggedFrames);
			float rot = Projectile.rotation + MathHelper.Pi;
			HenshinFxDraw.BeginAdditive();
			for (int i = 0; i < 3; i++)
			{
				Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * (i * 14f - 14f);
				HenshinFxDraw.DrawHitJaggedFrame(pos,
					HenshinFxDraw.WithAlpha(new Color(200, 215, 230), 0.9f * life),
					1.55f + i * 0.12f, rot, (frame + i) % HenshinFxDraw.HitJaggedFrames);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>波导弹：大型冲击波球，强自带追踪。</summary>
	public class AuraSphereBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.extraUpdates = 1;
			InherentHoming = true;
			HomingTurnRate = 0.22f;
			HomingRangeTiles = 48f;
		}

		public override void AI()
		{
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - Projectile.Center;
				Projectile.velocity = (aim.LengthSquared() < 1f
					? new Vector2(Main.player[Projectile.owner].direction, 0f)
					: Vector2.Normalize(aim)) * 9f;
			}
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Projectile.rotation += 0.18f;
			Lighting.AddLight(Projectile.Center, 0.25f, 0.55f, 0.95f);
			if (Main.rand.NextBool(2))
			{
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), DustID.DungeonWater,
					-Projectile.velocity * 0.05f, 80, new Color(120, 190, 255), 1.5f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Pitch = 0.2f }, Projectile.Center);
			for (int i = 0; i < 20; i++)
				Dust.NewDustPerfect(target.Center, DustID.DungeonWater, Main.rand.NextVector2Circular(7f, 7f), 60, new Color(140, 210, 255), 1.6f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(80, 160, 255), 0.55f), 1.55f * pulse);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(200, 240, 255), 0.75f), 0.85f * pulse);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>陀螺球：旋转钢环砸向鼠标（玩家不变身）。</summary>
	public class GyroBallSmashProj : HenshinMoveProj
	{
		private float _spin;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Player p = Main.player[Projectile.owner];
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (dir.LengthSquared() < 1f) dir = new Vector2(p.direction, 0f);
				dir.Normalize();
				Projectile.velocity = dir * 16f;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.1f }, Projectile.Center);
			}
			_spin += 0.55f;
			Projectile.rotation = _spin;
			Dust.NewDustPerfect(Projectile.Center, DustID.Iron, Main.rand.NextVector2Circular(2f, 2f), 90, default, 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.BeginAdditive();
			for (int i = 0; i < 3; i++)
			{
				float ang = _spin + i * MathHelper.TwoPi / 3f;
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center + ang.ToRotationVector2() * 14f,
					HenshinFxDraw.WithAlpha(new Color(190, 200, 210), 0.55f), 0.35f);
			}
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(220, 230, 240), 0.7f), 0.75f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>隐形岩：鼠标上空悬浮尖球陷阱。</summary>
	public class StealthRockFieldProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SpikyBall;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 420;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 24;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.SpikyBall);
			if (Projectile.owner == Main.myPlayer)
				Projectile.Center = HenshinProjUtil.OwnerMouseWorld(Projectile) + new Vector2(0f, -48f);
		}

		public override void AI()
		{
			Projectile.velocity *= 0.9f;
			Projectile.rotation += 0.08f;
			Projectile.position.Y += MathF.Sin(Main.GameUpdateCount * 0.08f + Projectile.whoAmI) * 0.35f;
			Lighting.AddLight(Projectile.Center, 0.45f, 0.32f, 0.15f);
			if (Main.rand.NextBool(8))
				Dust.NewDustPerfect(Projectile.Center, DustID.Sand, Main.rand.NextVector2Circular(1.2f, 1.2f), 100, new Color(210, 160, 80), 1.1f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.SpikyBall].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(210, 150, 70, 230),
				Projectile.rotation, tex.Size() * 0.5f, 1.35f, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(230, 170, 70), 0.35f), 0.55f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>子弹拳：短距快速鞭拳。</summary>
	public class BulletPunchProj : HenshinMoveProj
	{
		private const int Life = 10;
		private Vector2 _dir;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 52;
			Projectile.height = 36;
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
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.MountedCenter;
				if (_dir.LengthSquared() < 1f) _dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.35f }, p.Center);
			}
			float t = 1f - Projectile.timeLeft / (float)Life;
			Projectile.Center = p.MountedCenter + _dir * MathHelper.Lerp(24f, 96f, t);
			Projectile.rotation = _dir.ToRotation();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / (float)Life;
			int frame = HenshinFxDraw.AgeFrame(Life, Projectile.timeLeft, 2, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 230, 200), 0.85f * life),
				0.95f, Projectile.rotation + MathHelper.Pi, frame);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>自我激励：给自己上 WorkUp buff。</summary>
	public class WorkUpBuffProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 20;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (Projectile.owner == Main.myPlayer)
					p.AddBuff(ModContent.BuffType<HenshinWorkUpBuff>(), HenshinWorkUpBuff.DurationTicks);
				SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.25f }, p.Center);
			}
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(p.Center + Main.rand.NextVector2Circular(40f, 40f), DustID.Cloud,
					new Vector2(0f, -2.5f), 80, new Color(120, 220, 255), 1.4f).noGravity = true;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>舍身冲撞：加长猛撞 + 命中额外爆炸。</summary>
	public class DoubleEdgeLungeProj : TakeDownLungeProj
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			base.OnHitNPC(target, hit, damageDone);
			if (Projectile.owner != Main.myPlayer) return;
			for (int i = 0; i < 8; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(36f, 36f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center + off, Vector2.Zero,
					ModContent.ProjectileType<GraySolarBurstVfxProj>(), 0, 0f, Projectile.owner);
				if (id >= 0) Main.projectile[id].Center = target.Center + off;
			}
			for (int i = 0; i < 22; i++)
				Dust.NewDustPerfect(target.Center, DustID.Smoke, Main.rand.NextVector2Circular(7f, 7f), 70, new Color(180, 170, 150), 1.6f).noGravity = true;
			SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.55f, Pitch = -0.15f }, target.Center);
		}
	}

	/// <summary>龙之俯冲：瞬抬升空 → 砸向鼠标。</summary>
	public class DragonDiveSlamProj : HenshinMoveProj
	{
		private const int Rise = 10, Hang = 6, Slam = 16;
		private int _phase;
		private Vector2 _target, _dir;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Rise + Hang + Slam + 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
				SoundEngine.PlaySound(SoundID.Item60 with { Pitch = 0.2f }, p.Center);
			}
			Projectile.Center = p.Center;
			if (Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 12);
			}

			int age = Rise + Hang + Slam + 8 - Projectile.timeLeft;
			if (age <= Rise)
			{
				_phase = 0;
				if (Projectile.owner == Main.myPlayer) p.velocity = new Vector2(0f, -22f);
			}
			else if (age <= Rise + Hang)
			{
				_phase = 1;
				if (Projectile.owner == Main.myPlayer) p.velocity *= 0.4f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
			}
			else
			{
				if (_phase != 2)
				{
					_phase = 2;
					_dir = _target - p.Center;
					if (_dir.LengthSquared() < 64f) _dir = new Vector2(0f, 1f);
					_dir.Normalize();
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.25f }, p.Center);
				}
				if (Projectile.owner == Main.myPlayer) p.velocity = _dir * 28f;
				Dust.NewDustPerfect(p.Center, DustID.FireworkFountain_Blue, -_dir * 3f + Main.rand.NextVector2Circular(2f, 2f), 80, new Color(80, 160, 255), 1.3f).noGravity = true;
			}
		}

		public override bool? CanDamage() => _phase == 2 ? null : false;

		public override bool PreDraw(ref Color lightColor)
		{
			if (_phase != 2) return false;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(100, 180, 255), 0.55f), new Vector2(0.45f, 1.4f), _dir.ToRotation());
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>画龙点睛：伏特攻击式闪现，0.5s 后超粗黑龙路径结算。</summary>
	public class DragonAscentBlinkProj : HenshinMoveProj
	{
		private const float MaxRange = 1280f;
		private Vector2 _from, _to;
		private bool _blinked;
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
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }

			if (!_blinked && Projectile.owner == Main.myPlayer)
			{
				_blinked = true;
				_from = p.Center;
				Vector2 cursor = HenshinProjUtil.OwnerMouseWorld(Projectile);
				Vector2 delta = cursor - _from;
				_to = delta.LengthSquared() > MaxRange * MaxRange ? _from + Vector2.Normalize(delta) * MaxRange : cursor;
				p.Teleport(_to, -1);
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 20 + p.GetModPlayer<HenshinPlayer>().LungeIFrameBonus);
				SoundEngine.PlaySound(SoundID.Item60 with { Pitch = -0.1f }, p.Center);
				for (int i = 0; i < 16; i++)
				{
					Dust.NewDustPerfect(_from, DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, Color.Black, 1.4f).noGravity = true;
					Dust.NewDustPerfect(p.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, Color.Black, 1.4f).noGravity = true;
				}
			}

			Projectile.Center = p.Center;
			if (Projectile.timeLeft > 10 && _blinked)
			{
				for (float u = 0f; u <= 1f; u += 0.1f)
				{
					if (!Main.rand.NextBool(3)) continue;
					Dust.NewDustPerfect(Vector2.Lerp(_from, _to, u), DustID.Smoke, Vector2.Zero, 120, new Color(20, 20, 20), 1.1f).noGravity = true;
				}
			}

			if (Projectile.timeLeft == 10 && Projectile.owner == Main.myPlayer)
			{
				Vector2 trail = _to - _from;
				if (trail.LengthSquared() < 4f) trail = new Vector2(p.direction * 48f, 0f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), _from, trail,
					ModContent.ProjectileType<DragonAscentTrailProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.Delivery = MoveDelivery.Lunge;
				}
				SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.35f, Volume = 0.7f }, _to);
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class DragonAscentTrailProj : HenshinMoveProj
	{
		private const int Life = 48;
		private Vector2 _from, _to;
		private bool _init;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			if (!_init)
			{
				_init = true;
				_from = Projectile.Center;
				_to = _from + Projectile.velocity;
				Projectile.Center = Vector2.Lerp(_from, _to, 0.5f);
			}
			float life = Projectile.timeLeft / (float)Life;
			for (float u = 0f; u <= 1f; u += 0.05f)
			{
				if (!Main.rand.NextBool(4)) continue;
				Dust.NewDustPerfect(Vector2.Lerp(_from, _to, u), DustID.Shadowflame, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, Color.Black, 1.3f * life).noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!_init) return false;
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, 72f, ref _);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_init) return false;
			float life = Projectile.timeLeft / (float)Life;
			Vector2 delta = _to - _from;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Vector2.Lerp(_from, _to, 0.5f),
				HenshinFxDraw.WithAlpha(new Color(0, 0, 0), 0.95f * life),
				new Vector2(Math.Max(1f, delta.Length() / 32f), 2.4f), delta.ToRotation());
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Vector2.Lerp(_from, _to, 0.5f),
				HenshinFxDraw.WithAlpha(new Color(40, 20, 60), 0.55f * life),
				new Vector2(Math.Max(1f, delta.Length() / 36f), 1.4f), delta.ToRotation());
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>神鸟猛击：6 只红色勇鸟残影依次冲刺。</summary>
	public class SkyAttackSextetDirectorProj : HenshinMoveProj
	{
		private int _spawned;
		private Vector2 _target;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 72;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
				int npc = FindNear(_target, 40f * 16f);
				if (npc >= 0) _target = Main.npc[npc].Center;
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.15f }, p.Center);
			}
			if (Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 8);
			}
			if (Projectile.owner == Main.myPlayer && _spawned < 6 && Projectile.timeLeft % 8 == 0)
			{
				float ang = MathHelper.TwoPi * _spawned / 6f;
				Vector2 spawn = p.Center + ang.ToRotationVector2() * 64f + new Vector2(0f, -40f);
				Vector2 vel = Vector2.Normalize(_target - spawn) * 18f;
				int per = Math.Max(1, Projectile.damage / 6);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<SkyAttackRedBirdProj>(), per, 1.5f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Math.Max(1, (Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage) / 6);
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.Delivery = MoveDelivery.Lunge;
				}
				_spawned++;
			}
		}

		private static int FindNear(Vector2 at, float range)
		{
			int best = -1;
			float bestD = range * range;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy()) continue;
				float d = Vector2.DistanceSquared(n.Center, at);
				if (d < bestD) { bestD = d; best = i; }
			}
			return best;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class SkyAttackRedBirdProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Raven;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 48;
			Projectile.tileCollide = false;
			Projectile.penetrate = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.scale = 1.25f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
			=> Main.instance.LoadProjectile(ProjectileID.Raven);

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Red, -Projectile.velocity * 0.08f, 80, new Color(255, 60, 40), 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.Raven].Value;
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Raven]);
			Rectangle frame = tex.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 3) % frames);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(255, 70, 55, 230),
				Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center - Projectile.velocity * 0.4f,
				HenshinFxDraw.WithAlpha(new Color(255, 80, 40), 0.55f), new Vector2(1.4f, 0.45f), Projectile.rotation);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>远距咬碎：钉在鼠标处咬合。</summary>
	public class BiteArcAtMouseProj : HenshinMoveProj
	{
		private const int Life = 18;
		private const float VisualScale = 4f;
		private Vector2 _fixed;
		private float _size = 1f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_fixed = Projectile.owner == Main.myPlayer ? HenshinProjUtil.OwnerMouseWorld(Projectile) : Projectile.Center;
				_size = Projectile.ai[0] > 0.1f ? Projectile.ai[0] : 1f;
				float s = _size * VisualScale;
				Projectile.width = (int)(64 * s);
				Projectile.height = (int)(56 * s);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.35f }, _fixed);
			}
			Projectile.Center = _fixed;
		}

		public override bool? CanDamage()
		{
			int age = Life - Projectile.timeLeft;
			return age <= 6 || (age >= 9 && age <= 15) ? null : false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0) target.AddBuff(BuffID.BrokenArmor, (int)Projectile.ai[1]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			float t = 1f - Projectile.timeLeft / (float)Life;
			float close = t < 0.45f ? t / 0.45f : 1f - (t - 0.45f) * 0.35f;
			close = MathHelper.Clamp(close, 0f, 1f);
			float s = _size * VisualScale;
			float openGap = MathHelper.Lerp(18f, 2f, close) * s;
			Color fang = new(18, 18, 22, 240);
			Vector2 origin = Projectile.Center - Main.screenPosition;
			int teeth = 6;
			float span = 44f * s;
			for (int i = 0; i < teeth; i++)
			{
				float x = ((i + 0.5f) / teeth - 0.5f) * span;
				float w = (6f + (i % 2 == 0 ? 2f : 0f)) * s;
				float h = (12f + (i % 2 == 0 ? 3f : 0f)) * s;
				DrawTooth(pixel, origin + new Vector2(x, -openGap), w, h, fang, tipDown: true);
				DrawTooth(pixel, origin + new Vector2(x, openGap), w, h, fang, tipDown: false);
			}
			return false;
		}

		private static void DrawTooth(Texture2D pixel, Vector2 center, float w, float h, Color fill, bool tipDown)
		{
			w = MathHelper.Clamp(w, 3f, 112f);
			h = MathHelper.Clamp(h, 6f, 144f);
			for (int layer = 0; layer < 5; layer++)
			{
				float u = layer / 4f;
				float lw = MathHelper.Lerp(w, 1f, u);
				float y = tipDown ? MathHelper.Lerp(0f, h, u) : MathHelper.Lerp(0f, -h, u);
				Main.spriteBatch.Draw(pixel, center + new Vector2(0f, y), new Rectangle(0, 0, 1, 1), fill, 0f,
					new Vector2(0.5f, 0.5f), new Vector2(lw, Math.Max(2f, h / 5f)), SpriteEffects.None, 0f);
			}
		}
	}
}
