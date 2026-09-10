using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	public static class HenshinProjUtil
	{
		/// <summary>无饰品、招式自带追踪时的菱形索敌格数（对齐旧 HomingAI 480px 轴向）。</summary>
		public const float DefaultHomingTiles = 30f;

		/// <summary>新锁目标须落在飞行方向 60° 半角内（cos 60° = 0.5）。</summary>
		public const float HomingAcquireMinDot = 0.5f;

		/// <summary>锁上后曼哈顿距离超过索敌半径该倍数则断锁。</summary>
		public const float HomingLockBreakMul = 2f;

		public static float ManhattanPx(Vector2 a, Vector2 b)
			=> Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

		public static bool InForwardCone(Vector2 aim, Vector2 toTarget, float minDot)
		{
			if (aim.LengthSquared() < 0.0001f || toTarget.LengthSquared() < 0.0001f)
				return false;
			aim.Normalize();
			toTarget.Normalize();
			return Vector2.Dot(aim, toTarget) >= minDot;
		}

		/// <summary>
		/// 广角镜：按 Delivery 开追踪；转向与索敌格数取 max。
		/// 招式本来就会追时，索敌格数与饰品取 max，避免碎片把自带范围削短。
		/// </summary>
		public static void ApplyAccessoryHoming(IHenshinMoveProj tagged, bool accShouldHome, float accTurn, float accRangeTiles)
		{
			bool inherent = tagged.Homing;
			if (accShouldHome)
				tagged.Homing = true;
			if (tagged.Homing && accTurn > 0f)
				tagged.HomingTurnRate = Math.Max(tagged.HomingTurnRate, accTurn);
			if (!tagged.Homing || accRangeTiles <= 0f)
				return;
			float own = tagged.HomingRangeTiles > 0f
				? tagged.HomingRangeTiles
				: (inherent ? DefaultHomingTiles : 0f);
			tagged.HomingRangeTiles = own > 0f ? Math.Max(own, accRangeTiles) : accRangeTiles;
		}

		/// <summary>
		/// 叶绿弹式追踪：菱形索敌；新锁须在当前速度 60° 半角内；锁死后可掉头追，直到 2× 半径断锁。
		/// </summary>
		public static void HomingAI(Projectile proj, bool homing, float turnRate)
		{
			if (!homing || !proj.friendly || proj.velocity.LengthSquared() < 0.01f)
				return;

			IHenshinMoveProj tagged = proj.ModProjectile as IHenshinMoveProj;
			float acquireTiles = tagged != null && tagged.HomingRangeTiles > 0f
				? tagged.HomingRangeTiles
				: DefaultHomingTiles;
			float acquirePx = acquireTiles * 16f;
			float breakPx = acquirePx * HomingLockBreakMul;

			int prevLock = tagged?.HomingTargetWhoAmI ?? -1;
			NPC target = null;
			int locked = prevLock;
			if ((uint)locked < Main.maxNPCs)
			{
				NPC n = Main.npc[locked];
				if (n.active && n.CanBeChasedBy(proj, ignoreDontTakeDamage: true) && !n.dontTakeDamage
					&& ManhattanPx(proj.Center, n.Center) < breakPx)
					target = n;
				else if (tagged != null)
					tagged.HomingTargetWhoAmI = -1;
			}

			if (target == null)
			{
				target = AcquireHomingTarget(proj, acquirePx);
				if (tagged != null)
					tagged.HomingTargetWhoAmI = target != null ? target.whoAmI : -1;
			}

			if (tagged != null && tagged.HomingTargetWhoAmI != prevLock)
				proj.netUpdate = true;

			if (target == null)
				return;

			Vector2 desired = target.Center - proj.Center;
			if (desired.LengthSquared() < 0.0001f)
				return;
			desired.Normalize();
			float speed = proj.velocity.Length();
			proj.velocity = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(proj.velocity), desired, turnRate)) * speed;
		}

		private static NPC AcquireHomingTarget(Projectile proj, float acquirePx)
		{
			NPC bestNpc = null;
			float best = acquirePx;
			Vector2 aim = proj.velocity;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy(proj))
					continue;
				float d = ManhattanPx(proj.Center, n.Center);
				if (d >= best)
					continue;
				if (!InForwardCone(aim, n.Center - proj.Center, HomingAcquireMinDot))
					continue;
				if (proj.tileCollide && !Collision.CanHit(proj.Center, 1, 1, n.position, n.width, n.height))
					continue;
				best = d;
				bestNpc = n;
			}
			return bestNpc;
		}
	}

		public abstract class HenshinMoveProj : ModProjectile, IHenshinMoveProj
		{
			public bool EasyCrit { get; set; }
			public bool Homing { get; set; }
			public float HomingTurnRate { get; set; } = 0.08f;
			public float HomingRangeTiles { get; set; }
			public int HomingTargetWhoAmI { get; set; } = -1;
			public bool IgnoreDefensePartial { get; set; }
			public MoveDelivery Delivery { get; set; }
			public virtual bool HandlesOwnHoming => false;

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

			// ai2>=1.5：近身战子斩，钉在生成点；否则贴玩家前方
			if (Projectile.ai[2] > 1.5f)
			{
				if (Projectile.localAI[0] == 0f)
				{
					Projectile.localAI[0] = 1f;
					Projectile.localAI[1] = Projectile.Center.X;
					Projectile.localAI[2] = Projectile.Center.Y;
					if (Projectile.velocity.LengthSquared() > 0.00001f)
						Projectile.rotation = Projectile.velocity.ToRotation();
					else
						Projectile.rotation = dir > 0 ? 0f : MathHelper.Pi;
				}
				Projectile.Center = new Vector2(Projectile.localAI[1], Projectile.localAI[2]);
			}
			else
				Projectile.Center = owner.MountedCenter + new Vector2(dir * Reach, -4f);
			Projectile.velocity = Vector2.Zero;

			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Smoke;
			if (Projectile.timeLeft > Lifetime - 8)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), dust, new Vector2(dir * 2f, 0f), 100, default, 1.1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float life = Projectile.timeLeft / (float)Lifetime;
			// HitJagged01 rot=0 尖端朝左；朝右攻击需 +Pi
			float rot;
			if (Projectile.ai[2] > 1.5f)
				rot = Projectile.rotation + MathHelper.Pi;
			else
				rot = dir > 0 ? MathHelper.Pi + 0.35f : -0.35f;
			int frame = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			float scale = 0.75f + (Projectile.ai[2] > 0.5f ? 0.25f : 0f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 230, 210), 0.8f * life),
				scale, rot, frame, SpriteEffects.None);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	public class GenericBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
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
		public override bool HandlesOwnHoming => true;
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
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagnetSphereBolt;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
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
		public override bool HandlesOwnHoming => true;
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

	/// <summary>
	/// 念力：ShadowBeamFriendly 壳弹。32 格索敌；命中后弹射下一目标，最多 2 击；短 Confused。
	/// </summary>
	public class PsychicWaveBoltProj : HenshinMoveProj
	{
		private const float SeekTiles = 32f;
		private const int MaxHits = 2;
		private const float FlightSpeed = 20f;

		private int _hitCount;
		private int _hitA = -1;
		private int _hitB = -1;
		private int _lockNpc = -1;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = MaxHits;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.ShadowBeamFriendly);
			if (Projectile.velocity.LengthSquared() < 0.01f)
				Projectile.velocity = Vector2.UnitX * FlightSpeed;
			else
				Projectile.velocity = Vector2.Normalize(Projectile.velocity) * FlightSpeed;

			_lockNpc = FindTarget(Projectile.Center, excludeA: -1, excludeB: -1);
			if (_lockNpc >= 0)
			{
				Vector2 to = Main.npc[_lockNpc].Center - Projectile.Center;
				if (to.LengthSquared() > 0.01f)
					Projectile.velocity = Vector2.Normalize(to) * FlightSpeed;
			}
		}

		public override void AI()
		{
			if (_lockNpc < 0 || !IsValidTarget(_lockNpc))
				_lockNpc = FindTarget(Projectile.Center, _hitA, _hitB);

			if (_lockNpc >= 0)
			{
				Vector2 desired = Main.npc[_lockNpc].Center - Projectile.Center;
				if (desired.LengthSquared() > 0.01f)
				{
					desired.Normalize();
					Vector2 cur = Projectile.velocity.LengthSquared() > 0.01f
						? Vector2.Normalize(Projectile.velocity)
						: desired;
					Projectile.velocity = Vector2.Normalize(Vector2.Lerp(cur, desired, 0.35f)) * FlightSpeed;
				}
			}
			else if (Projectile.velocity.LengthSquared() < 0.01f)
				Projectile.velocity = Vector2.UnitX * FlightSpeed;

			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0.85f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (_hitCount == 0)
				_hitA = target.whoAmI;
			else
				_hitB = target.whoAmI;
			_hitCount++;

			target.AddBuff(BuffID.Confused, 40);

			if (Projectile.owner == Main.myPlayer)
			{
				int ring = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<PsychicHitRingProj>(), 0, 0f, Projectile.owner);
				if (ring >= 0)
					Main.projectile[ring].Center = target.Center;
			}

			if (_hitCount >= MaxHits)
			{
				Projectile.Kill();
				return;
			}

			_lockNpc = FindTarget(Projectile.Center, _hitA, _hitB);
			if (_lockNpc < 0)
			{
				Projectile.Kill();
				return;
			}

			Vector2 to = Main.npc[_lockNpc].Center - Projectile.Center;
			if (to.LengthSquared() > 0.01f)
				Projectile.velocity = Vector2.Normalize(to) * FlightSpeed;
			Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 45);
		}

		private static bool IsValidTarget(int who)
		{
			if (who < 0 || who >= Main.maxNPCs)
				return false;
			NPC n = Main.npc[who];
			return n.active && !n.friendly && n.life > 0 && n.CanBeChasedBy();
		}

		private static int FindTarget(Vector2 from, int excludeA, int excludeB)
		{
			float best = SeekTiles * 16f * SeekTiles * 16f;
			int found = -1;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (i == excludeA || i == excludeB)
					continue;
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Vector2.DistanceSquared(from, n.Center);
				if (d < best)
				{
					best = d;
					found = i;
				}
			}
			return found;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.ShadowBeamFriendly);
			Rectangle frame = tex.Frame();
			Vector2 origin = frame.Size() * 0.5f;
			float rot = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame,
				new Color(210, 160, 255), rot, origin, 1.15f, SpriteEffects.None, 0);

			HenshinFxDraw.BeginAdditive();
			Vector2 back = Projectile.velocity.LengthSquared() > 0.01f
				? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
			for (int i = 1; i <= 4; i++)
			{
				Vector2 pos = Projectile.Center - back * (i * 10f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos,
					HenshinFxDraw.WithAlpha(new Color(180, 100, 255), 0.45f * (1f - i * 0.12f)), 0.28f);
			}
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(220, 160, 255), 0.55f), 0.4f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>念力命中光圈：半径 1 格紫环，快速渐隐。</summary>
	public class PsychicHitRingProj : HenshinMoveProj
	{
		private const int Life = 12;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.damage = 0;
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			float fade = Projectile.timeLeft / (float)Life;
			float diam = 16f; // 1 格
			HenshinFxDraw.BeginAdditive();
			float scale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(190, 90, 255), 0.85f * fade), scale);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(220, 140, 255), 0.7f * fade), 0.45f);
			HenshinFxDraw.EndAdditive();
			return false;
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
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DirtBall;

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

				// 出土小 Boulder 装饰（低伤）
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0f, -3f),
					ModContent.ProjectileType<FallingBoulderProj>(), Math.Max(1, Projectile.damage / 6), 1f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].scale = 0.25f;
					Main.projectile[id].timeLeft = 30;
				}
			}
			for (int i = 0; i < 4; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.35f).noGravity = true;
			Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(2f, 2f), 100, new Color(160, 110, 70), 1.1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.DirtBall);
			Texture2D dirt = ProjectileBorrow.RequestProjectileTexture(ProjectileID.DirtBall);
			for (int i = 0; i < 5; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(12f, 12f);
				Main.EntitySpriteDraw(dirt, Projectile.Center + off - Main.screenPosition, null,
					new Color(160, 110, 70, 200), Main.rand.NextFloat(MathHelper.TwoPi), dirt.Size() * 0.5f, 0.7f, SpriteEffects.None);
			}
			return false;
		}
	}

	/// <summary>
	/// 三地鼠挖洞突进：朝鼠标冲 20 格，从第 3 格起清 5 格宽走廊（玩家镐力；无预算）。
	/// 直接平移玩家，避免 velocity 顶墙中止。
	/// </summary>
	public class DigLungeProj : HenshinMoveProj
	{
		private const int Life = 24;
		private const float DashTiles = 20f;
		private Vector2 _dir;
		private Vector2 _startCenter;
		private float _speed;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
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
			Player p = Main.player[Projectile.owner];
			if (!p.active || p.dead)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = Main.MouseWorld - p.Center;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				_speed = DashTiles * 16f / Life;
				_startCenter = p.Center;
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.35f }, p.Center);
			}

			if (Projectile.owner == Main.myPlayer)
			{
				// 不用 velocity 顶墙（会中止突进）；直接平移 + 清零速度
				p.velocity = Vector2.Zero;
				p.Center += _dir * _speed;
				p.fallStart = (int)(p.position.Y / 16f);
				p.immune = true;
				p.immuneTime = System.Math.Max(p.immuneTime, 12);

				Vector2 perp = new Vector2(-_dir.Y, _dir.X);
				var budget = p.GetModPlayer<TerrainEdit.TerrainBudgetPlayer>();

				void MineCorridorAt(Vector2 sample)
				{
					for (int o = -2; o <= 2; o++)
					{
						Vector2 off = perp * (o * 16f);
						int tx = (int)((sample.X + off.X) / 16f);
						int ty = (int)((sample.Y + off.Y) / 16f);
						budget.TryMineWithPlayerPick(tx, ty, maxReachTiles: 22);
					}
				}

				// 从挖掘方向第 3 格起清整条走廊，避免贴脸墙立刻卡住
				for (float dist = 3f * 16f; dist <= DashTiles * 16f; dist += 8f)
					MineCorridorAt(_startCenter + _dir * dist);

				// 每帧再清身前一小段
				for (float ahead = 3f * 16f; ahead <= 48f; ahead += 8f)
					MineCorridorAt(p.Center + _dir * ahead);
			}

			Projectile.Center = p.Center;
			Dust.NewDustPerfect(p.Center, DustID.Dirt, -_dir * 3f + Main.rand.NextVector2Circular(2f, 2f), 80, default, 1.4f).noGravity = true;
			Dust.NewDustPerfect(p.Center, DustID.Sand, Main.rand.NextVector2Circular(3f, 3f), 100, new Color(200, 170, 90), 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item14, target.Center);
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(target.Center, DustID.Dirt, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.4f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(200, 160, 80), 0.55f), 0.65f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(180, 140, 70), 0.35f), 0.9f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
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
		public override bool HandlesOwnHoming => true;
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

	/// <summary>突进伤害盒：全程维持含竖直方向的速度；SoftGlow 残影 + 尘迹。闪焰冲锋：多线火径+包裹焰+收尾减速。</summary>
	public class LungeProj : HenshinMoveProj
	{
		private const int BrakeTicks = 10;

		private Vector2 _dir;
		private int _dashLife = 16;
		private int _lifetime = 16;
		private float _speed = 8.5f;
		private bool _flare;
		private Vector2 _lastTrail;
		private float _cruiseSpeed;

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

		private bool ExtendedDash => Projectile.ai[2] > 0.5f;
		private bool InBrake => ExtendedDash && Projectile.timeLeft <= BrakeTicks;

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

				// 火特效仅 Torch；ai2 只控射程。长距冲刺一律带刹车（闪焰/猛撞式）。
				_flare = Projectile.ai[0] == DustID.Torch;
				float reachTiles = Projectile.ai[2] > 0.5f ? Projectile.ai[2] : 0f;
				if (reachTiles >= 1f)
				{
					_dashLife = System.Math.Clamp((int)(reachTiles * 0.75f), 18, 40);
					float reachPx = reachTiles * 16f;
					_speed = reachPx / _dashLife;
					_lifetime = _dashLife + BrakeTicks;
					Projectile.timeLeft = _lifetime;
					Projectile.localNPCHitCooldown = _dashLife;
				}
				else
				{
					_dashLife = 16;
					_lifetime = 16;
				}
				_cruiseSpeed = System.Math.Max(p.maxRunSpeed, 6f);
				_lastTrail = p.Center;
			}

			if (Projectile.owner == Main.myPlayer)
			{
				if (InBrake)
				{
					// 较快平滑回到巡航速度（类克盾收尾），避免冲完长距离滑行
					float t = 1f - Projectile.timeLeft / (float)BrakeTicks;
					float targetSpd = MathHelper.Lerp(_speed, _cruiseSpeed, MathHelper.SmoothStep(0f, 1f, t));
					p.velocity = Vector2.Lerp(p.velocity, _dir * targetSpd, 0.35f);
				}
				else
					p.velocity = _dir * _speed;
			}

			Projectile.Center = p.Center;
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Cloud;

			if (_flare && !InBrake)
			{
				Vector2 perp = new Vector2(-_dir.Y, _dir.X);
				// 贴身包裹火焰
				for (int i = 0; i < 5; i++)
				{
					Vector2 wrap = p.Center + Main.rand.NextVector2Circular(22f, 26f);
					Dust d = Dust.NewDustPerfect(wrap, DustID.Torch,
						-_dir * Main.rand.NextFloat(0.5f, 2.5f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
						50, default, Main.rand.NextFloat(1.3f, 1.9f));
					d.noGravity = true;
				}
				// 错落多线火径
				if (Vector2.DistanceSquared(_lastTrail, p.Center) > 18f * 18f)
				{
					_lastTrail = p.Center;
					for (int lane = -2; lane <= 2; lane++)
					{
						float side = lane * 10f + Main.rand.NextFloat(-2f, 2f);
						Vector2 basePos = p.Center + perp * side;
						for (int i = 0; i < 3; i++)
						{
							Dust trail = Dust.NewDustPerfect(basePos - _dir * (i * 7f), DustID.Torch,
								-_dir * Main.rand.NextFloat(0.4f, 1.2f) + perp * Main.rand.NextFloat(-0.4f, 0.4f),
								70, default, Main.rand.NextFloat(1.1f, 1.55f));
							trail.noGravity = true;
							trail.fadeIn = 1.05f;
						}
					}
				}
			}
			else if (!_flare)
			{
				Dust.NewDustPerfect(p.Center, dust, -p.velocity * 0.2f, 100, default, 1.2f).noGravity = true;
				Dust.NewDustPerfect(p.Center - _dir * 12f, dust, -_dir * 2f, 120, default, 1.0f).noGravity = true;
			}
			else if (InBrake && Main.rand.NextBool(2))
			{
				Dust.NewDustPerfect(p.Center, DustID.Torch, -_dir * 1.5f + Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.2f).noGravity = true;
			}

			int immuneGate = BrakeTicks + System.Math.Max(8, _dashLife * 5 / 8);
			if (Projectile.timeLeft >= immuneGate && Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = System.Math.Max(p.immuneTime, 15);
			}
		}

		public override bool? CanDamage() => InBrake ? false : null;

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
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
			if (dust == DustID.Torch)
			{
				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(target.Center, DustID.Torch, Main.rand.NextVector2Circular(6f, 6f), 60, default, 1.5f).noGravity = true;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (!ExtendedDash || Projectile.owner != Main.myPlayer)
				return;
			Player p = Main.player[Projectile.owner];
			if (!p.active)
				return;
			// 最终贴回巡航量级，避免残留冲刺速度
			float spd = p.velocity.Length();
			if (spd > _cruiseSpeed * 1.15f)
				p.velocity = Vector2.Normalize(p.velocity) * _cruiseSpeed;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = InBrake
				? Projectile.timeLeft / (float)BrakeTicks * 0.45f
				: System.Math.Min(1f, Projectile.timeLeft / (float)System.Math.Max(1, _dashLife));
			HenshinFxDraw.BeginAdditive();
			if (_flare)
			{
				int fireFrame = HenshinFxDraw.AgeFrame(_lifetime, Projectile.timeLeft, 2, HenshinFxDraw.FireFrames);
				Vector2 perp = new Vector2(-_dir.Y, _dir.X);
				// 贴身包裹：多层 Fire + SoftGlow，持续到冲撞段结束（刹车段渐弱）
				for (int ring = 0; ring < 4; ring++)
				{
					float ang = ring * MathHelper.PiOver2 + Projectile.timeLeft * 0.18f;
					Vector2 wrap = Projectile.Center + new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * (14f + ring * 3f);
					HenshinFxDraw.DrawFireFrame(wrap,
						HenshinFxDraw.WithAlpha(new Color(255, 160, 50), 0.7f * life),
						0.42f + ring * 0.05f, ang, (fireFrame + ring) % HenshinFxDraw.FireFrames);
				}
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 120, 30), 0.7f * life), 0.85f);
				HenshinFxDraw.DrawFireFrame(Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 200, 80), 0.85f * life), 0.7f, _dir.ToRotation(), fireFrame);

				if (!InBrake)
				{
					// 错落多条火径线
					for (int lane = -2; lane <= 2; lane++)
					{
						for (int i = 1; i <= 4; i++)
						{
							Vector2 pos = Projectile.Center - _dir * (i * 16f) + perp * (lane * 9f);
							Color c = HenshinFxDraw.WithAlpha(new Color(255, 140, 40), 0.5f * life * (1f - i * 0.1f));
							HenshinFxDraw.DrawFireFrame(pos, c, 0.32f + i * 0.03f, _dir.ToRotation(),
								(fireFrame + i + lane + 2) % HenshinFxDraw.FireFrames);
							HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos,
								HenshinFxDraw.WithAlpha(new Color(255, 90, 20), 0.28f * life), 0.26f);
						}
					}
				}
			}
			else
			{
				for (int i = 1; i <= 3; i++)
				{
					Vector2 pos = Projectile.Center - _dir * (i * 14f);
					Color c = HenshinFxDraw.WithAlpha(new Color(220, 230, 255), 0.45f * life * (1f - i * 0.2f));
					HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos, c, 0.28f);
				}
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(Color.White, 0.55f * life), 0.4f);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	public class BeamBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
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
