using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 遗留飞行粗束（默认模式）。龙怒/破灭/水柱已迁至 SustainedBeam / WaterJet。
	/// ai0=Dust；ai1=onHitBuff；ai2 保留兼容。
	/// </summary>
	public class ThickBeamProj : HenshinMoveProj
	{
		public const int ModeDefault = 0;
		public const int ModeDragonRage = 1;
		public const int ModeHyperBeam = 2;
		public const int ModeHydro = 3;

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
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 vel = Projectile.velocity;
			if (vel.LengthSquared() < 0.01f)
				vel = Vector2.UnitX;
			Vector2 dir = Vector2.Normalize(vel);
			Vector2 tip = Projectile.Center;
			Vector2 tail = tip - dir * MathHelper.Clamp(vel.Length() * 2.2f, 28f, 72f);
			HenshinFxDraw.BeginAdditive();
			Color c = HenshinFxDraw.WithAlpha(new Color(200, 140, 255), 0.8f);
			HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, tail, tip, c, 16f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, tip, HenshinFxDraw.WithAlpha(Color.White, 0.55f), 0.28f);
			HenshinFxDraw.EndAdditive();
			return true;
		}
	}

	/// <summary>
	/// 持续瞄准光束（破坏光线等）。龙之怒已迁至 <see cref="DragonRageBarrageProj"/>。
	/// ai2：2=破坏光线（加粗 + 自缓）。ShootSpeed=0；AI 每帧重瞄。
	/// </summary>
	public class SustainedBeamProj : HenshinMoveProj
	{
		public const int ModeHyperBeam = 2;
		public const float MaxLength = 96f * 16f;
		public const int LifeHyper = 90;

		private Vector2 _from;
		private Vector2 _to;
		private int _life = LifeHyper;
		private bool _thick;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = LifeHyper;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 4;
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

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_life = LifeHyper;
				Projectile.timeLeft = _life;
				_thick = Projectile.ai[2] > 1.5f; // ModeHyperBeam
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.DeathLaser);
				// 起手音交给共享 UltCast（大招根弹各端自动播），删 Item67 免叠；这里只补开炮后坐的主人端大招震屏。
				HenshinHitFeel.Shake(Projectile.owner, owner.MountedCenter, HitTier.Ultimate);
			}

			owner.AddBuff(BuffID.Slow, 15);
			owner.velocity *= 0.94f;

			_from = owner.MountedCenter;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * MaxLength;
			Projectile.Center = Vector2.Lerp(_from, _to, 0.35f);
			owner.direction = aim.X >= 0f ? 1 : -1;

			// R3 手感（G2）：删掉沿线紫尘；光沿光束每 6 格一点（只取屏幕附近），枪口单独一团强光。
			float env = BeamEnvelope();
			// 光照第 2 轮 E 组：破坏光线（只 ModeHyperBeam = 大招）沿线光 ×1.3。
			Vector3 beamLight = new Vector3(0.75f, 0.3f, 1f) * env * (_thick ? 1.3f : 1f);
			Lighting.AddLight(_from + aim * 24f, beamLight * 1.6f);
			if (_thick && env > 0.05f)
			{
				// 大招发光粒子：光束两侧交替，离中线 40px（光晕半宽 24px 之外），不盖光束；紫火把自带光，不另 AddLight。
				// 位置只由主人位置 + 同步鼠标算出（_from/aim 各端自算）。
				float along = Main.rand.NextFloat(96f, 900f);
				float side = (Main.GameUpdateCount & 1) == 0 ? 1f : -1f;
				Vector2 perp = new(-aim.Y, aim.X);
				HenshinUltGlow.Emit(_from + aim * along + perp * (40f * side), Projectile.owner, Projectile.whoAmI, 8f,
					DustID.PurpleTorch, Vector3.Zero, everyTicks: 1);
			}
			for (float d = 96f; d < MaxLength; d += 96f)
			{
				Vector2 at = _from + aim * d;
				if (Vector2.DistanceSquared(at, owner.Center) > 1400f * 1400f)
					break;
				Lighting.AddLight(at, beamLight);
			}

			// R4（G3 跨组需求，破坏光线收束）：进入末 12% 淡出那一 tick，枪口 M 爆 + BlastL（各端 AI，只限 ModeHyperBeam）。
			if (_thick && Projectile.timeLeft == Math.Max(1, (int)(_life * 0.12f)))
			{
				Vector2 mouth = _from + aim * 40f;
				HenshinSfx.Play(SfxKey.BlastL, mouth, 0.75f, -0.3f);
				HenshinExplosion.Spawn(mouth, BlastSize.M, BeamTint, 150f, kenneyFrame: false);
			}
		}

		/// <summary>光束包络：前 10% 冲出、末 12% 收束，中间满亮。PreDraw 与光照共用。</summary>
		private float BeamEnvelope()
		{
			float env = Projectile.timeLeft / (float)Math.Max(1, _life);
			if (env > 0.9f)
				return (1f - env) / 0.1f;
			if (env < 0.12f)
				return env / 0.12f;
			return 1f;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			float width = _thick ? 56f : 32f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, width, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);

			// 打击感（G2）：每次命中白闪 + 光；大招震屏 20 tick 一次（持续低频轰鸣）。
			Vector2 along = _to - _from;
			float len = along.Length();
			Vector2 hitAt = target.Center;
			if (len > 1f)
			{
				Vector2 dir = along / len;
				hitAt = _from + dir * MathHelper.Clamp(Vector2.Dot(target.Center - _from, dir), 0f, len);
			}
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), BeamTint, hitAt);
			// 命中点：每 3 次命中生成一个同步的光束火花（光 + S 爆 + 命中音，各端都看得到听得到）；每次施放第一下换 HitUlt + M 爆。
			if (H3Fx.TryUltBeat(Projectile.owner, LifeHyper + 10))
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), hitAt, H3Fx.BeamSpark, 2f, Projectile.owner, 0f, SfxKey.HitUlt, BlastSize.M);
			else if (++_hitCount % 3 == 0)
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), hitAt, H3Fx.BeamSpark, 1.2f, Projectile.owner, 0f, SfxKey.HitHeavy, BlastSize.S);
		}

		private int _hitCount;
		private static readonly Color BeamTint = new(200, 90, 255);

		public override bool PreDraw(ref Color lightColor)
		{
			float env = BeamEnvelope();

			float coreW = _thick ? 14f : 8f;
			float envW = _thick ? 48f : 28f;
			HenshinFxDraw.BeginAdditive();
			Color envelope = HenshinFxDraw.WithAlpha(new Color(160, 40, 220), 0.75f * env);
			Color core = HenshinFxDraw.WithAlpha(new Color(240, 200, 255), 0.95f * env);
			HenshinFxDraw.DrawContinuousBeam(_from, _to, core, envelope, coreW, envW);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _to, envelope,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, _thick ? 42f : 28f));
			// 枪口闪：常亮一团 + 开炮前 12 tick 的白热大闪（按 Projectile 寿命算，各端一致）。
			int age = _life - Projectile.timeLeft;
			float muzzle = age < 12 ? 1f - age / 12f : 0f;
			Vector2 mouth = _from + (_to - _from).SafeNormalize(Vector2.UnitX) * 18f;
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, mouth, HenshinFxDraw.WithAlpha(new Color(200, 110, 255), 0.6f * env),
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, (_thick ? 70f : 46f) * (0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 30f))));
			if (muzzle > 0f)
				HenshinExplosion.Draw(mouth, BlastSize.M, BeamTint, 1f - muzzle, _thick ? 150f : 100f, 0f, kenneyFrame: false);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 龙之怒连射导演：沿瞄准直线间歇吐出抖动球体。
	/// ai2：0=技能 12 发；1=大招 32 发。
	/// </summary>
	public class DragonRageBarrageProj : HenshinMoveProj
	{
		public const int ModeSkill = 0;
		public const int ModeUlt = 1;
		public const int CountSkill = 12;
		public const int CountUlt = 32;

		private int _fired;
		private int _total;
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool IsUlt => (int)Projectile.ai[2] == ModeUlt;

		// 施放音在 AI 首帧自己放：技能 DragonCast；大招 UltCast（claim 后共享 UltCast 不再播，这里补上，不叠两声）。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_total = IsUlt ? CountUlt : CountSkill;
				int interval = IsUlt ? 1 : 2;
				Projectile.localAI[1] = interval;
				Projectile.timeLeft = _total * interval + 12;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				if (IsUlt)
				{
					HenshinSfx.Play(SfxKey.UltCast, owner.MountedCenter);
					HenshinHitFeel.Shake(Projectile.owner, owner.MountedCenter, HitTier.Heavy, _dir);
				}
				else
					HenshinSfx.PlayCooldown(SfxKey.DragonCast, owner.MountedCenter, 6);
			}

			Projectile.Center = owner.MountedCenter;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
			if (aim.LengthSquared() > 1f)
				_dir = Vector2.Normalize(aim);
			owner.direction = _dir.X >= 0f ? 1 : -1;

			if (_fired >= _total)
			{
				if (Projectile.timeLeft > 6)
					Projectile.timeLeft = 6;
				return;
			}

			int intervalTicks = Math.Max(1, (int)Projectile.localAI[1]);
			int age = (_total * intervalTicks + 12) - Projectile.timeLeft;
			if (Projectile.owner == Main.myPlayer && age >= 0 && age % intervalTicks == 0)
			{
				float phase = _fired * 0.73f;
				int perOrb = Math.Max(1, Projectile.damage / (IsUlt ? 6 : 4));
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(),
					owner.MountedCenter + _dir * 12f, _dir * DragonRageOrbProj.Speed,
					ModContent.ProjectileType<DragonRageOrbProj>(), perOrb, Projectile.knockBack * 0.35f, Projectile.owner,
					phase, 0f, IsUlt ? 1f : 0f);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
				{
					tagged.EasyCrit = EasyCrit;
					tagged.IgnoreDefensePartial = IgnoreDefensePartial;
				}
				_fired++;
			}
		}
	}

	/// <summary>
	/// 龙之怒球体：直径约 1.5 格，沿直线前进并垂直抖动；命中后直径 5 格小爆。
	/// ai0=抖动相位；ai2=1 大招（略亮）。贴图复用 SoftGlow（原版球色/AI 不合适）。
	/// </summary>
	public class DragonRageOrbProj : HenshinMoveProj
	{
		public const float Speed = 15.5f;
		public const float BurstDiameterTiles = 5f;
		public const float OrbDiameterTiles = 1.5f;
		public const int Life = 135;

		private static readonly Color DragonEdge = new(33, 8, 173);   // #2108ad
		private static readonly Color DragonCore = new(231, 206, 57); // #e7ce39
		/// <summary>Additive 下 #2108ad 过暗几乎看不见，抬亮保留蓝紫相作光晕。</summary>
		private static readonly Color DragonHaloLit = new(95, 55, 255);

		private Vector2 _dir;
		private Vector2 _origin;
		private float _dist;
		private float _phase;
		private bool _burst;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private static Color MixDragon(float t) => Color.Lerp(DragonEdge, DragonCore, MathHelper.Clamp(t, 0f, 1f));

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10, 1);

		public override void SetDefaults()
		{
			int size = (int)(OrbDiameterTiles * 16f); // ~24
			Projectile.width = size;
			Projectile.height = size;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_phase = Projectile.ai[0];
				_dir = Projectile.velocity;
				if (_dir.LengthSquared() < 0.01f)
					_dir = new Vector2(1f, 0f);
				_dir.Normalize();
				_origin = Projectile.Center;
				_dist = 0f;
				Projectile.velocity = Vector2.Zero;
				// 连发出膛声：本端 5 tick 节流（大招 32 发约 6 声，技能 12 发约 5 声），不会叠成一团。
				HenshinSfx.PlayCooldown(SfxKey.DragonCast, _origin, 5, 0.5f, Projectile.ai[2] > 0.5f ? -0.15f : 0.1f);
			}

			_dist += Speed / (1 + Projectile.extraUpdates);
			Vector2 perp = new Vector2(-_dir.Y, _dir.X);
			float wobble = MathF.Sin(_dist * 0.085f + _phase) * 10f; // ~0.6 格垂直抖
			Projectile.Center = _origin + _dir * _dist + perp * wobble;
			bool skillOrb = Projectile.ai[2] < 0.5f;
			if (skillOrb && !HenshinTileRay.OwnerPierces(Projectile, MoveDelivery.Barrage)
				&& Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height, acceptTopSurfaces: false))
			{
				SpawnBurst(Projectile.Center);
				Projectile.Kill();
				return;
			}
			// 光照第 2 轮 E 组：大招球光 ×1.25。
			Lighting.AddLight(Projectile.Center, skillOrb ? new Vector3(0.55f, 0.4f, 0.95f) : new Vector3(0.69f, 0.5f, 1.19f));
			// 大招发光粒子：导演（44 tick）比球（135 tick）先死，改由球按 whoAmI % 16 抽样发（32 球约 2 颗在发）；
			// 球身直径 ~26px，粒子落在半径 18–25px 外缘；extraUpdates=1，只在最后一次更新发。
			if (!skillOrb && Projectile.numUpdates == 0)
				HenshinUltGlow.Emit(Projectile, 22f, DustID.BlueTorch, Vector3.Zero, sampleN: 16);
			// H4：球体改手绘贴图，不再逐 tick 撒尘。
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnBurst(target.Center);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), DragonHaloLit, target.Center);
		}

		private void SpawnBurst(Vector2 center)
		{
			if (_burst)
				return;
			_burst = true;

			// H4：手绘爆环（5 格直径 ≈ 52px × 1.5）；旧 3 颗尘删去，爆炸配方 M 由 MouseAoEBurstProj（DungeonWater）画。
			if (Projectile.owner != Main.myPlayer)
				return;
			// 命中音由特效弹首帧各端播：龙系命中 4 tick 节流，大招另补一声 HitUlt。
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
				ModContent.ProjectileType<H4HitFxProj>(), 0, 0f, Projectile.owner, H4Fx.StyleDragon,
				G1HitSfx.Code(SfxKey.DragonHit, 4, Projectile.ai[2] > 0.5f), 1.5f);

			float diameter = BurstDiameterTiles * 16f;
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
				ModContent.ProjectileType<MouseAoEBurstProj>(), Math.Max(1, Projectile.damage), 2.5f, Projectile.owner,
				DustID.DungeonWater);
			if (id >= 0)
			{
				Projectile burst = Main.projectile[id];
				burst.timeLeft = 14;
				burst.width = (int)diameter;
				burst.height = (int)diameter;
				burst.Center = center;
				if (burst.ModProjectile is IHenshinMoveProj tagged)
				{
					tagged.EasyCrit = EasyCrit;
					tagged.IgnoreDefensePartial = IgnoreDefensePartial;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			bool ult = Projectile.ai[2] > 0.5f;
			float body = OrbDiameterTiles * 16f * (ult ? 1.08f : 1f); // ~1.5 格
			// 蓝紫尾 + 金芯（球体抖动，尾巴跟着摆）。
			HenshinTrail.Strip(Projectile, DragonHaloLit, body * 0.75f, new Color(255, 230, 150), 0.34f, 0f, ult ? 0.85f : 0.7f);
			// H4：手绘球体贴图（Assets/Fx/DragonOrb/DragonOrb，4 帧 × 3 tick，32px 帧内球身 26px）+ 一层淡光晕。
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(DragonHaloLit, 0.3f), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, body * 1.8f));
			HenshinFxDraw.EndAdditive();
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			H4Fx.DrawFrame(H4Fx.Orb(false), 4, frame, Projectile.Center, Color.White, 0f, body / 26f);
			return false;
		}
	}

	/// <summary>
	/// 水柱：朝鼠标持续喷射。
	/// ai2=0 水炮（R4 持续引导）：按住技能键期间只存在 1 道水柱，跟鼠标转（转速受限），松手 PumpReleaseTicks 后消失；
	/// 水柱每 tick 被第一个敌人或实心方块截断（不穿透），每 PumpHitInterval tick 对线上敌人造成一次伤害。
	/// ai2=1 加农：一次性、穿透、更高频/更粗/更深色，每 3 击半径 5 格水爆（未改）。
	/// </summary>
	public class WaterJetProj : HenshinMoveProj
	{
		public const int ModePump = 0;
		public const int ModeCannon = 1;
		public const float PumpLength = 88f * 16f; // R3：射程 64→88 格（水炮），加农仍为 80 格
		public const float CannonLength = 80f * 16f;
		public const int Life = 72; // 仅加农使用
		public const int GrowTicks = 24; // 仅加农使用
		public const int PumpGrowTicks = 10; // R3：水炮喷出时间 24→10 tick，起手更快
		public const float PumpTurnRate = 0.16f; // R3：水炮每 tick 最大转向角（rad），避免甩鼠标时水柱瞬移
		public const int PumpHitInterval = 3; // R4：按住时每 3 tick 一次伤害（20 次/秒，旧版两道重叠水柱合计约 21 次/秒）
		public const int PumpReleaseTicks = 8; // R4：松手后淡出
		public const float CannonBurstRadius = 5f * 16f;

		private static readonly Color WaterCore = new(120, 200, 255);
		private static readonly Color WaterGlow = new(40, 110, 220);
		private static readonly Color CannonCore = new(30, 90, 200);
		private static readonly Color CannonGlow = new(15, 50, 140);

		private Vector2 _from;
		private Vector2 _to;
		private Vector2 _aimDir = Vector2.UnitX;
		private int _npcHits;
		private bool _aimInit;
		private int _age; // 水炮：存活 tick（各端自算）
		private bool _released => Projectile.ai[0] == 1f; // 水炮：已松手。仅拥有者判 controlUseItem 后写 ai[0]=1 + netUpdate，其他端只读
		private int _relTicks;
		private bool _hitNpc; // 水炮：本 tick 被敌人截断
		private bool _hitWall; // 水炮：本 tick 被方块截断
		private static Asset<Texture2D> _pumpTex;
		private static Asset<Texture2D> _cannonTex;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WaterStream;

		private int JetMode => (int)Projectile.ai[2];
		private bool IsCannon => JetMode == ModeCannon;

		// 施放音自己放：水炮 WaterCast；加农（只作大招）UltCast + 低频大爆，claim 后共享 UltCast 不叠。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 5;
		}

		public override bool ShouldUpdatePosition() => false;

		private float CurrentLength()
		{
			float grow;
			if (IsCannon)
			{
				grow = MathHelper.Clamp((Life - Projectile.timeLeft) / (float)GrowTicks, 0f, 1f);
				grow = grow * grow * (3f - 2f * grow);
			}
			else
			{
				// 水炮：喷出用 ease-out；之后始终满长，由截断逻辑缩短
				grow = MathHelper.Clamp(_age / (float)PumpGrowTicks, 0f, 1f);
				grow = 1f - (1f - grow) * (1f - grow);
			}
			float maxLen = IsCannon ? CannonLength : PumpLength;
			return maxLen * grow;
		}

		private float WidthMul()
			=> IsCannon || !_released ? 1f : MathHelper.Clamp(1f - _relTicks / (float)PumpReleaseTicks, 0f, 1f);

		/// <summary>沿 dir 的第一个敌人距离（到其中心的投影）；无则返回 maxLen+1。</summary>
		private float FirstNpcDistance(Vector2 dir, float maxLen)
		{
			float best = maxLen + 1f;
			Vector2 end = _from + dir * maxLen;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.dontTakeDamage || n.lifeMax <= 0)
					continue;
				float _ = 0f;
				if (!Collision.CheckAABBvLineCollision(n.Hitbox.TopLeft(), n.Hitbox.Size(), _from, end, 40f, ref _))
					continue;
				float d = MathHelper.Clamp(Vector2.Dot(n.Center - _from, dir), 32f, maxLen);
				if (d < best)
					best = d;
			}
			return best;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (IsCannon)
					Projectile.localNPCHitCooldown = 3;
				else
				{
					Projectile.localNPCHitCooldown = PumpHitInterval;
					// R4：自动连发（autoReuse）每 26 tick 会再生成一枚；已有未松手（同步的 ai[0]!=1；出生时 ai0 是 DustID.Water，仅作占位）的水炮时，新枚立即消失，保证按住期间只有 1 道水柱。
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile o = Main.projectile[i];
						if (o.active && o.whoAmI != Projectile.whoAmI && o.owner == Projectile.owner && o.type == Projectile.type
							&& (int)o.ai[2] == ModePump && o.localAI[0] == 1f && o.ModProjectile is WaterJetProj w && !w._released)
						{
							Projectile.Kill();
							return;
						}
					}
				}
				if (IsCannon)
				{
					HenshinSfx.Play(SfxKey.UltCast, owner.MountedCenter);
					HenshinSfx.Play(SfxKey.BlastM, owner.MountedCenter, 0.8f, -0.35f);
					HenshinSfx.Play(SfxKey.WaterCast, owner.MountedCenter, 1f, -0.4f);
					HenshinHitFeel.Shake(Projectile.owner, owner.MountedCenter, HitTier.Ultimate);
					_cannonMuzzlePending = true;
				}
				else
					HenshinSfx.Play(SfxKey.WaterCast, owner.MountedCenter);
			}

			if (!IsCannon)
			{
				// 持续引导：按住期间保活；仅拥有者判定松手，写 ai[0]=1 并同步，其他端只读 ai[0]，淡出由各端按同一标志自走。
				if (Projectile.owner == Main.myPlayer && !_released
					&& !(owner.controlUseItem && !owner.CCed && owner.HeldItem?.ModItem is HenshinForceItem))
				{
					Projectile.ai[0] = 1f;
					Projectile.netUpdate = true;
				}
				if (_released)
				{
					_relTicks++;
					if (_relTicks >= PumpReleaseTicks)
					{
						Projectile.Kill();
						return;
					}
					Projectile.timeLeft = PumpReleaseTicks - _relTicks + 1;
				}
				else
					Projectile.timeLeft = 5;
				_age++;
			}

			_from = owner.MountedCenter;
			{
				// 鼠标经 NetOp.SyncAim 同步，禁用 Main.MouseWorld。
				Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - _from;
				if (aim.LengthSquared() < 1f)
					aim = new Vector2(owner.direction, 0f);
				Vector2 want = Vector2.Normalize(aim);
				if (IsCannon || !_aimInit)
					_aimDir = want;
				else
				{
					// 水炮：每 tick 最多转 PumpTurnRate
					float cur = _aimDir.ToRotation();
					float d = MathHelper.WrapAngle(want.ToRotation() - cur);
					_aimDir = (cur + MathHelper.Clamp(d, -PumpTurnRate, PumpTurnRate)).ToRotationVector2();
				}
				_aimInit = true;
			}

			float len = CurrentLength();
			_hitNpc = false;
			_hitWall = false;
			// 加农保持穿墙穿怪；水炮每 tick 被第一个实心方块 / 敌人截断（不穿透）。诅咒之符 Beam 跳过墙截断。
			if (!IsCannon)
			{
				float full = len;
				if (!HenshinTileRay.OwnerPierces(Projectile, MoveDelivery.Beam))
				{
					float blocked = HenshinTileRay.FirstSolidDistance(_from, _aimDir, len);
					if (blocked + 4f < len)
					{
						len = Math.Max(32f, blocked);
						_hitWall = true;
					}
				}
				float npcD = FirstNpcDistance(_aimDir, len);
				if (npcD < len)
				{
					len = npcD;
					_hitNpc = true;
					_hitWall = false;
				}
				// 命中水花：每 10 tick 一次，粒子少
				if ((_hitNpc || _hitWall) && !_released && _age % 10 == 0)
				{
					Vector2 hitAt = _from + _aimDir * len;
					SpawnPumpSplash(hitAt);
					if (_hitWall)
					{
						HenshinTileImpact.At(hitAt, -_aimDir, TileImpactKind.Water, 1f, sound: false);
						if (!Main.dedServ && _age % 30 == 0)
							SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.5f }, hitAt);
					}
				}
			}
			_to = _from + _aimDir * len;
			Projectile.Center = len > 1f ? Vector2.Lerp(_from, _to, 0.3f) : _from;
			owner.direction = _aimDir.X >= 0f ? 1 : -1;
			if (_cannonMuzzlePending)
			{
				// 加农出膛：枪口 M 档水爆（不叠火烟帧），各端都跑。
				_cannonMuzzlePending = false;
				HenshinExplosion.Spawn(_from + _aimDir * 36f, BlastSize.M, new Color(90, 170, 255), 150f, kenneyFrame: false, strength: 0.6f);
			}
			// 光照：水是 lit，水柱本体逐段吃光（见 PreDraw），技能版不加光；加农（大招，mixed）只留枪口一盏小灯（≤0.2）。
			if (IsCannon && !Main.dedServ)
			{
				// 光照第 2 轮 E 组（§4.15 大招例外）：枪口灯 0.2→0.3 略亮；沿水柱每 3 格一盏小蓝灯（≤0.2/通道），
				// 灯先照亮水柱所在格，PreDraw 取光时水柱自然偏蓝。只取玩家附近 ~88 格。按加农淡入淡出缩放。用灯不用粒子（plan-review §7 二选一）。
				float fade = MathHelper.Clamp((Life - Projectile.timeLeft) / 6f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
				Lighting.AddLight(_from + _aimDir * 40f, 0.1f * fade, 0.18f * fade, 0.3f * fade);
				for (float d = 48f; d < len; d += 48f)
				{
					Vector2 at = _from + _aimDir * d;
					if (Vector2.DistanceSquared(at, owner.Center) > 1400f * 1400f)
						break;
					Lighting.AddLight(at, 0.07f * fade, 0.13f * fade, 0.2f * fade);
				}
			}

			// 水柱本体已是自绘贴图（H1），不再沿线撒粒子。
		}

		public override bool? CanDamage()
		{
			// 水炮松手淡出阶段停伤。加农始终可伤。
			if (!IsCannon && _released)
				return false;
			return null;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if ((_to - _from).LengthSquared() < 4f)
				return false;
			if (!IsCannon && _released)
				return false;
			float _ = 0f;
			// 水炮 ~2.5 格；加农 ~4.5 格（×2 加粗）
			float width = (IsCannon ? 72f : 40f) * WidthMul();
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, width, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 水炮：截断与水花已在 AI 里按线处理；这里只加白闪 + 命中光（Light 档，不震）。
			if (!IsCannon)
			{
				HenshinHitFeel.OnHit(Projectile, target, HitTier.Light, WaterCore, target.Center, 0.5f);
				return;
			}
			// 加农：Ultimate 档（主人端 20 tick 一次大震 + 顿帧）；旧每击 3 颗水尘删去。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), WaterCore, target.Center, 0.5f);

			_npcHits++;
			if (_npcHits % 3 == 0 && Projectile.owner == Main.myPlayer)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<MouseAoEBurstProj>(), Math.Max(1, Projectile.damage / 2), 3f, Projectile.owner, DustID.Water);
				if (id >= 0)
				{
					Projectile burst = Main.projectile[id];
					burst.timeLeft = 16;
					burst.width = (int)(CannonBurstRadius * 2f);
					burst.height = (int)(CannonBurstRadius * 2f);
					burst.Center = target.Center;
				}
				// 溅花特效弹首帧各端播命中音（每次施放补一声 HitUlt）；水爆声由 MouseAoEBurstProj 首帧播。旧 4 颗尘 + 只主人听得到的 Item85 删去。
				H1HitFxProj.Spawn(Projectile, target.Center, H1HitFxProj.KindSplash, 2.2f, G1HitSfx.Code(Projectile, SfxKey.WaterHit, 4));
			}
		}

		private bool _cannonMuzzlePending;

		private void SpawnPumpSplash(Vector2 at)
		{
			// 命中溅花（每 10 tick 一个）：特效弹首帧各端播水声，6 tick 节流；旧 1 颗尘删去。
			H1HitFxProj.Spawn(Projectile, at, H1HitFxProj.KindSplash, 1f, G1HitSfx.Code(SfxKey.WaterHit, 6));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if ((_to - _from).LengthSquared() < 4f)
				return false;

			float env;
			if (!IsCannon)
				env = _released ? WidthMul() : 1f;
			else
			{
				env = Projectile.timeLeft / (float)Life;
				if (env > 0.88f)
					env = (1f - env) / 0.12f;
				else if (env < 0.1f)
					env = env / 0.1f;
				else
					env = 1f;
			}

			// H1：自绘水柱贴图（竖排 4 帧，横向 24 art px = 48 px 一块，首尾可拼接）平铺到 _from→_to；
			// 水炮 Assets/Fx/WaterJet/WaterStream（高 28px×1.4≈40），加农 WaterCannon（高 48px×1.5=72，与碰撞宽度一致）。
			Texture2D tex = IsCannon ? H1Tex.Get(ref _cannonTex, "WaterJet/WaterCannon") : H1Tex.Get(ref _pumpTex, "WaterJet/WaterStream");
			float k = IsCannon ? 1.5f : 1.4f;
			Vector2 delta = _to - _from;
			float len = delta.Length();
			if (len < 1f)
				return false;
			Vector2 n = delta / len;
			float rot = delta.ToRotation();
			float wMul = WidthMul();
			int frame = (int)(Main.GameUpdateCount / 3 % 4);
			int fh = tex.Height / 4;
			float alpha = Math.Min(1f, env * 1.2f);
			// 接缝修复：旧版每块位置 .Floor() 取整，斜向时相邻块各自舍入最多差 1px，露出细缝；末块源宽 int 截断也会短一截。
			// 现在按 8 texel 一片沿线浮点定位（不取整），每片往前多画 2px 盖住下一片起点（满透明度时贴图 alpha 只有 0/255，重叠看不出）；
			// 淡入淡出（alpha<1）时不重叠，免得半透明叠出深色条。末片按剩余长度拉伸，正好落在 _to。
			// 每片单独按沿线插值取光（光照 C 组：水是 lit），光照台阶从每 67px 一级缩到每 11px 一级。
			const int SliceTex = 8;
			int slicesPerTile = Math.Max(1, tex.Width / SliceTex);
			float slicePx = SliceTex * k;
			float overlap = alpha >= 0.99f ? 2f : 0f;
			int slices = (int)Math.Ceiling(len / slicePx);
			Vector2 origin = new(0f, fh * 0.5f);
			for (int i = 0; i < slices; i++)
			{
				float start = i * slicePx;
				float part = Math.Min(slicePx, len - start);
				if (part <= 0.01f)
					break;
				int sw = Math.Min(SliceTex, Math.Max(1, (int)Math.Ceiling(part / k)));
				float drawLen = i < slices - 1 ? part + overlap : part;
				// 光照第 2 轮 E 组：keepHue 保蓝（暖光下不发灰）+ darkFade 暗处整片变半透明（0.35 + 0.65·k），不再是一整条灰板。
				Color c = HenshinFxDraw.LitSegment(_from, _to, (start + part * 0.5f) / len, Color.White, keepHue: true, darkFade: true) * alpha;
				Rectangle src = new(i % slicesPerTile * SliceTex, fh * frame, sw, fh);
				Main.EntitySpriteDraw(tex, _from + n * start - Main.screenPosition, src, c, rot,
					origin, new Vector2(drawLen / sw, k * wMul), SpriteEffects.None, 0f);
			}
			// 芯线（光照第 2 轮 E 组）：沿中线一条很淡的蓝色加色细线，亮度 0.3 + 0.7·k（暗处仍能看出水柱走向），不发光、不加粒子。
			Texture2D glow = HenshinFxDraw.SoftGlow;
			if (glow != null && alpha > 0.01f)
			{
				float coreW = (IsCannon ? 14f : 8f) * wMul;
				float gw = Math.Max(1f, glow.Width) * 0.55f; // SoftGlow 亮芯约占 55%
				float stepPx = slicePx * 2f;
				Vector2 coreScale = new(stepPx * 1.6f / gw, coreW / gw);
				HenshinFxDraw.BeginAdditive();
				for (float d = stepPx * 0.5f; d < len; d += stepPx)
				{
					float t = d / len;
					float coreK = 0.3f + 0.7f * HenshinFxDraw.LightK(HenshinFxDraw.LightAlong(_from, _to, t));
					// Additive 批次按 src.rgb × src.A 叠加：RGB 直接乘强度、A 保持 255，实际加上的就是 0.2·coreK（WithAlpha 会平方成 0.04，几乎看不见）。
					Color coreC = new Color(150, 210, 255) * (0.2f * coreK * alpha);
					coreC.A = 255;
					HenshinFxDraw.DrawAdditiveCentered(glow, _from + n * d, coreC, coreScale, rot);
				}
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>指针落点大爆。ai0=Dust，ai1=可选 Buff。Torch=过热：半径 15 格、5 段高伤脉冲。</summary>
	public class MouseAoEBurstProj : HenshinMoveProj
	{
		private const int OverheatLife = 30;
		private const int DefaultLife = 18;
		private int _lifetime;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool IsOverheat => (Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch) == DustID.Torch;

		public override void SetDefaults()
		{
			Projectile.width = 160;
			Projectile.height = 160;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = DefaultLife;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (IsOverheat)
				{
					_lifetime = OverheatLife;
					Projectile.timeLeft = OverheatLife;
					Vector2 c = Projectile.Center;
					Projectile.width = 480;
					Projectile.height = 480;
					Projectile.Center = c;
					Projectile.localNPCHitCooldown = 6;
				}
				else if ((Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch) == DustID.Grass)
				{
					// 种子炸弹：稍长可见窗，配合雷管式浓爆
					_lifetime = 24;
					Projectile.timeLeft = 24;
				}
				else
					_lifetime = DefaultLife;
			}

			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			float radius = IsOverheat ? 220f : Math.Max(24f, Projectile.width * 0.45f);
			if (dust == DustID.DungeonWater)
			{
				// 龙之怒球爆（G1）：每 tick 6 颗尘删去，改 M 档爆炸配方光（各端 AI）。
				HenshinExplosion.Pulse(Projectile.Center, DragonBurstTint, BlastSize.M, BurstT, Projectile.width);
				// R4（G3 跨组需求，圆陆鲨龙之怒）：球爆首帧小爆声，本端 8 tick 节流；命中 DragonHit 由命中环弹播。
				if (Projectile.localAI[1] == 0f)
					HenshinSfx.PlayCooldown(SfxKey.BlastS, Projectile.Center, 8, 0.7f);
			}
			else if (dust == DustID.Water)
			{
				// 加农水爆（G1）：每 tick 6 颗水尘删去，改 M 档爆炸配方光 + 首帧中爆声。
				// 光照 C 组：水爆是 lit 招式的爆炸，光 ×0.45。
				// 光照第 2 轮 E 组：加农末段水爆算大招，光 0.45→0.6；外缘少量蓝宝石光点（自带光，每 3 tick 至多 1 颗）。
				HenshinExplosion.Pulse(Projectile.Center, WaterBurstTint, BlastSize.M, BurstT, Projectile.width, 0.6f);
				HenshinUltGlow.Emit(Projectile, Projectile.width * 0.5f, DustID.GemSapphire, Vector3.Zero, everyTicks: 3);
				if (Projectile.localAI[1] == 0f)
					HenshinSfx.PlayCooldown(SfxKey.BlastM, Projectile.Center, 8);
			}
			else if (dust == DustID.Grass)
			{
				Lighting.AddLight(Projectile.Center, 0.35f, 1.15f, 0.35f);
				Color green = new(70, 240, 95);
				int count = Projectile.timeLeft > _lifetime - 4 ? 18 : 10;
				for (int i = 0; i < count; i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
						DustID.Grass, Main.rand.NextVector2Circular(5.5f, 5.5f), 30, green, Main.rand.NextFloat(1.5f, 2.3f));
					d.noGravity = true;
					d.color = green;
				}
				for (int i = 0; i < 5; i++)
				{
					Dust t = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius * 0.6f, radius * 0.6f),
						DustID.ChlorophyteWeapon, Main.rand.NextVector2Circular(3f, 3f), 50, green, 1.5f);
					t.noGravity = true;
					t.color = green;
				}
			}
			else
			{
				for (int i = 0; i < (IsOverheat ? 10 : 6); i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius), dust, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1.5f);
					d.noGravity = true;
				}
			}
			Projectile.localAI[1] = 1f;

			int age = _lifetime - Projectile.timeLeft;
			if (IsOverheat && age % 6 == 0 && age / 6 < 5)
			{
				SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f }, Projectile.Center);
			}
			else if (!IsOverheat && Projectile.timeLeft == _lifetime - 1 && dust != DustID.Water && dust != DustID.DungeonWater)
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); // 龙之怒 / 加农两种由 G1 自制音接管（见上）
		}

		private static readonly Color DragonBurstTint = new(110, 80, 255);
		private static readonly Color WaterBurstTint = new(80, 160, 255);

		private int _burstLife;

		/// <summary>爆炸配方进度 0..1（按本弹首帧看到的实际寿命：生成方会把 18 改成 14/16）。</summary>
		private float BurstT
		{
			get
			{
				if (_burstLife <= 0)
					_burstLife = Math.Max(1, Projectile.timeLeft);
				return MathHelper.Clamp(1f - Projectile.timeLeft / (float)_burstLife, 0f, 0.999f);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
			int d = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			if (d == DustID.Water || d == DustID.DungeonWater)
				HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light),
					d == DustID.Water ? WaterBurstTint : DragonBurstTint, target.Center, d == DustID.Water ? 0.5f : 1f);
		}

		public override bool? CanDamage()
		{
			if (!IsOverheat)
				return null;
			int age = _lifetime - Projectile.timeLeft;
			// 5 段脉冲窗口：每 6 tick 开 2 tick
			return age % 6 <= 1 && age / 6 < 5;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			float life = Projectile.timeLeft / (float)Math.Max(1, _lifetime);
			int fireFrame = HenshinFxDraw.AgeFrame(_lifetime, Projectile.timeLeft, 2, HenshinFxDraw.FireFrames);
			int flashFrame = HenshinFxDraw.AgeFrame(_lifetime, Projectile.timeLeft, 3, HenshinFxDraw.FlashImpactFrames);
			HenshinFxDraw.BeginAdditive();
			float diam = Math.Max(16f, Projectile.width);
			if (dust == DustID.Torch)
			{
				Color ring = HenshinFxDraw.WithAlpha(new Color(255, 140, 40), 0.75f * life);
				Color fire = HenshinFxDraw.WithAlpha(new Color(255, 90, 30), 0.85f * life);
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam * (IsOverheat ? 1.05f : 1f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ring, ringScale * (1f + (1f - life) * 0.25f));
				HenshinFxDraw.DrawFireFrame(Projectile.Center, fire, IsOverheat ? 2.4f : 1.1f, Main.GlobalTimeWrappedHourly * 3f, fireFrame);
				HenshinFxDraw.DrawFlashImpactFrame(Projectile.Center, HenshinFxDraw.WithAlpha(new Color(255, 200, 120), 0.5f * life),
					IsOverheat ? 1.1f : 0.55f, 0f, flashFrame);
			}
			else if (dust == DustID.PurpleTorch)
			{
				Color ring = HenshinFxDraw.WithAlpha(new Color(200, 100, 255), 0.8f * life);
				Color core = HenshinFxDraw.WithAlpha(new Color(240, 180, 255), 0.75f * life);
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ring, ringScale * (1f + (1f - life) * 0.2f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, core,
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam * 0.45f));
			}
			else if (dust == DustID.Water)
			{
				// 加农溅射水爆：环直径对齐 projectile.width（加农约 10 格宽 hitbox / 直径）
				// 光照 C 组：水是 lit。圈与泡沫的颜色先乘中心光（黑处变暗、不自发光），强度再降；爆炸 strength 0.5。
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				Color wl = HenshinFxDraw.LightAt(Projectile.Center);
				// 光照第 2 轮 E 组：保色吃光（LitHueFrom），暖光下水爆不发灰。
				Color ring = HenshinFxDraw.WithAlpha(HenshinFxDraw.LitHueFrom(wl, new Color(40, 120, 220)), 0.65f * life);
				Color foam = HenshinFxDraw.WithAlpha(HenshinFxDraw.LitHueFrom(wl, new Color(140, 210, 255)), 0.55f * life);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ring, ringScale * (1f + (1f - life) * 0.3f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, foam,
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam * 0.5f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, foam, ringScale * 0.55f);
				HenshinExplosion.Draw(Projectile.Center, BlastSize.M, WaterBurstTint, BurstT, diam, 0f, kenneyFrame: false, strength: 0.5f);
			}
			else if (dust == DustID.DungeonWater)
			{
				// 龙之怒命中爆：边缘 #2108ad / 芯 #e7ce39，直径约 5 格（width=80）
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				Color edge = HenshinFxDraw.WithAlpha(new Color(33, 8, 173), 0.8f * life);
				Color core = HenshinFxDraw.WithAlpha(new Color(231, 206, 57), 0.9f * life);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, edge, ringScale * (1f + (1f - life) * 0.25f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, core,
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam * 0.55f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, core, ringScale * 0.45f);
				HenshinExplosion.Draw(Projectile.Center, BlastSize.M, DragonBurstTint, BurstT, diam, 0f, kenneyFrame: false);
			}
			else if (dust == DustID.Grass)
			{
				// 种子炸弹：仅绿色扩散环/雾/芯光，不用 FlashImpact（避免金白尖刺）
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				float expand = 1f + (1f - life) * 0.45f;
				Color ring = HenshinFxDraw.WithAlpha(new Color(60, 230, 70), 0.95f * life);
				Color ringOuter = HenshinFxDraw.WithAlpha(new Color(30, 160, 50), 0.7f * life);
				Color core = HenshinFxDraw.WithAlpha(new Color(120, 255, 140), 0.9f * life);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ringOuter, ringScale * expand * 1.15f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ring, ringScale * expand);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(80, 220, 100), 0.55f * life),
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.Fog, diam * 0.85f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, core,
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam * 0.65f));
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>日光束蓄力导演：约 0.8s 金尘蓄力后朝鼠标发射持续 SolarPrismBeam。</summary>
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
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.GoldFlame;
			if (dust == DustID.ChlorophyteWeapon)
				dust = DustID.GoldFlame;
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(owner.Center + Main.rand.NextVector2CircularEdge(28f, 28f), dust, Vector2.Zero, 100, new Color(255, 220, 80), 1.2f).noGravity = true;
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(owner.Center + Main.rand.NextVector2CircularEdge(22f, 22f), DustID.YellowTorch, Vector2.Zero, 80, default, 1.1f).noGravity = true;

			if (Projectile.timeLeft == 8 && Projectile.owner == Main.myPlayer)
			{
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, dir * 16f,
					ModContent.ProjectileType<SolarPrismBeamProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
				{
					tagged.EasyCrit = EasyCrit;
					tagged.IgnoreDefensePartial = IgnoreDefensePartial;
				}
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			float charge = 1f - Projectile.timeLeft / 55f;
			HenshinFxDraw.BeginAdditive();
			Color gold = HenshinFxDraw.WithAlpha(new Color(255, 210, 70), 0.55f + charge * 0.4f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, gold, 0.35f + charge * 0.55f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 日光束发射段：蓄力后持续金棱会聚束（~100 tick），跟随鼠标；5 线收敛 + 连续线伤。
	/// </summary>
	public class SolarPrismBeamProj : HenshinMoveProj
	{
		public const float MaxLength = 64f * 16f;
		public const int Life = 100;

		private Vector2 _from;
		private Vector2 _to;
		private bool _inited;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.LastPrismLaser;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 4;
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
			if (!_inited)
			{
				_inited = true;
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.LastPrism);
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.LastPrismLaser);
				SoundEngine.PlaySound(SoundID.Item67 with { Pitch = 0.2f }, owner.MountedCenter);
			}
			_from = owner.MountedCenter;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * MaxLength;
			Projectile.Center = Vector2.Lerp(_from, _to, 0.4f);
			owner.direction = aim.X >= 0f ? 1 : -1;
			Lighting.AddLight(Projectile.Center, 1.1f, 0.9f, 0.35f);
			if (Projectile.timeLeft % 2 == 0)
				Dust.NewDustPerfect(Vector2.Lerp(_from, _to, Main.rand.NextFloat()), DustID.GoldFlame, Vector2.Zero, 80, new Color(255, 230, 100), 1.3f).noGravity = true;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			Vector2 dir = _to - _from;
			if (dir.LengthSquared() < 1f)
				return false;
			float spread = CurrentSpread();
			for (int i = -2; i <= 2; i++)
			{
				Vector2 tip = _from + dir.RotatedBy(i * spread);
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, tip, 32f, ref _))
					return true;
			}
			return false;
		}

		private float CurrentSpread()
		{
			float t = 1f - Projectile.timeLeft / (float)Life;
			return MathHelper.Lerp(0.12f, 0.008f, MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(t / 0.45f, 0f, 1f)));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_inited)
				return false;
			float env = Projectile.timeLeft / (float)Life;
			if (env > 0.92f)
				env = (1f - env) / 0.08f;
			else if (env < 0.1f)
				env = env / 0.1f;
			else
				env = 1f;

			Texture2D prism = ProjectileBorrow.RequestProjectileTexture(ProjectileID.LastPrism);
			Vector2 dir = _to - _from;
			float rot = dir.ToRotation();
			float spread = CurrentSpread();

			HenshinFxDraw.BeginAdditive();
			Color gold = HenshinFxDraw.WithAlpha(new Color(255, 220, 90), 0.95f * env);
			Color glow = HenshinFxDraw.WithAlpha(new Color(255, 180, 40), 0.65f * env);
			Color white = HenshinFxDraw.WithAlpha(new Color(255, 250, 220), 0.9f * env);
			for (int i = -2; i <= 2; i++)
			{
				float ang = i * spread;
				Vector2 tip = _from + dir.RotatedBy(ang);
				float coreW = 10f - Math.Abs(i) * 1.2f;
				float glowW = 28f - Math.Abs(i) * 3f;
				HenshinFxDraw.DrawContinuousBeam(_from, tip, white, glow, coreW, glowW);
			}
			// 主轴约 2 格宽
			HenshinFxDraw.DrawContinuousBeam(_from, _to, gold, glow, 14f, 32f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _from, gold,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 36f));
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _to, gold,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 28f));
			HenshinFxDraw.DrawAdditiveCentered(prism, _from, white, 0.55f, rot + Main.GlobalTimeWrappedHourly * 2f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>落石导演：ai1=数量，落在鼠标附近。</summary>
	/// <summary>岩崩导演：ai1=数量，ai2=水平半宽（像素，0→80）。落点在指针区，下落微偏指针。</summary>
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

		// R3 手感（G2）：施放音自己播（各端 AI 首帧）。同一弹种既是大钢蛇/豪力技能（3 石）又是大岩蛇大招（12 石，ai2=192），
		// 声明后共享 UltCast 不再自动播，所以大招在这里自己补 UltCast，技能播 RockCast。ai2 随生成包同步，各端判断一致。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				Player caster = Main.player[Projectile.owner];
				if (Projectile.ai[2] > 150f)
					HenshinSfx.Play(SfxKey.UltCast, caster.active ? caster.MountedCenter : Projectile.Center);
				else
					HenshinSfx.Play(SfxKey.RockCast, Projectile.Center);
			}
			// 光照第 2 轮 E 组：大招（ai2=192）落区外缘少量黄宝石光点 + 一盏土色灯（lit 大招 0.4，宝石光弱故 forceLight）。
			// 放在主人早退之前，各端都发；位置 = 同步的 Projectile.Center。落石在 H5（D 组），这里只由导演发（导演活到最后一块石头生成）。
			if (Projectile.ai[2] > 150f)
				HenshinUltGlow.Emit(Projectile, Projectile.ai[2], DustID.GemTopaz, new Vector3(0.4f, 0.32f, 0.2f), forceLight: true, dustScale: 1.2f);
			if (Projectile.owner != Main.myPlayer)
				return;
			int total = (int)System.Math.Max(1, Projectile.ai[1]);
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// 每 4 tick 一石；留余量避免未发完就结束
				Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, total * 4 + 8);
			}
			if (_spawned >= total)
			{
				Projectile.Kill();
				return;
			}
			if (Projectile.timeLeft % 4 == 0)
			{
				float halfW = Projectile.ai[2] > 1f ? Projectile.ai[2] : 80f;
				Vector2 target = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfW, halfW), Main.rand.NextFloat(-24f, 24f));
				Vector2 spawn = target + new Vector2(Main.rand.NextFloat(-halfW * 0.25f, halfW * 0.25f), -180f - Main.rand.NextFloat(40f));
				Vector2 toTarget = target - spawn;
				if (toTarget.Y < 8f)
					toTarget.Y = 8f;
				Vector2 vel = toTarget.SafeNormalize(Vector2.UnitY) * 15f;
				vel.X = MathHelper.Clamp(vel.X, -8f, 8f);
				vel.Y = Math.Max(10f, vel.Y);
				// ai0=1：落地触发 1 格半径范围伤
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<H5FallingRockProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 1f, halfW > 150f ? 1f : 0f);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_spawned++;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>下落棕石。ai0&gt;0.5：着地/死亡时生成半径 1 格范围伤（岩崩）。</summary>
	public class FallingBoulderProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Boulder;

		public override bool HandlesOwnHoming => true; // 纯重力落石，不吃广角镜追踪（与岩崩同法）

		private static readonly Color RockTint = new(200, 150, 95);

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 8);

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
			// 每 tick 一颗石尘删去，改土色短尾（PreDraw）。
		}

		public override void OnKill(int timeLeft)
		{
			// 落地：岩石碎声（本端 4 tick 节流，几块同时落只响一两声）+ S 档小爆（各端 OnKill 都跑）+ 少量碎石。
			HenshinSfx.PlayCooldown(SfxKey.RockHit, Projectile.Center, 4, 0.8f);
			HenshinExplosion.Spawn(Projectile.Center, BlastSize.S, RockTint, 64f * Math.Max(0.5f, Projectile.scale / 0.55f), kenneyFrame: false, strength: 0.5f);
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(4f, 4f), 80, new Color(160, 110, 70), 1.2f);

			if (Projectile.ai[0] > 0.5f && Projectile.owner == Main.myPlayer)
			{
				Vector2 at = Projectile.Center;
				int aoe = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
					ModContent.ProjectileType<RockShatterBurstProj>(), Projectile.damage, Projectile.knockBack * 0.85f, Projectile.owner, 1f);
				if (aoe >= 0)
				{
					Main.projectile[aoe].Center = at;
					if (Main.projectile[aoe].ModProjectile is IHenshinMoveProj tagged)
						tagged.EasyCrit = EasyCrit;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 土色短尾（宽随石块缩放），先画再由原版画本体。
			// 光照 C 组：石头是 lit。尾巴吃光；落地前的 SoftGlow 微闪删去（石头不发光）。
			HenshinTrail.Strip(Projectile, new Color(170, 120, 75), 26f * Projectile.scale, new Color(235, 200, 150), 0.3f, 0f, 0.55f, lit: true);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 40);
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 20);
			// 碎石是附带伤害：固定 Light 档（不跟大招槽升档，免得爆裂拳 5 块碎石抢走终结震屏）。
			HenshinHitFeel.OnHit(Projectile, target, HitTier.Light, RockTint, target.Center, 0.5f);
		}

		// 光照 C 组：原版画本体，颜色乘环境光（原为固定棕色，黑处全亮）。
		public override Color? GetAlpha(Color lightColor) => new Color(180, 120, 70, 220).MultiplyRGBA(lightColor);
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

	/// <summary>火焰锥连发：朝鼠标连续 Flames（85）喷射柱，数量来自 ai1。</summary>
	public class FlameConeDirectorProj : HenshinMoveProj
	{
		private int _fired;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// 喷射火焰：借的原版 Flames 不出声，施放 / 续喷声由本导演弹在各端放。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			// 各端：起喷一声，之后每 8 tick 低一点的续喷声（10 连喷约 3 声，不成噪音）；枪口火光。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.PlayCooldown(SfxKey.FireCast, owner.MountedCenter, 8);
			}
			else
				HenshinSfx.PlayCooldown(SfxKey.FireCast, owner.MountedCenter, 8, 0.55f, -0.15f);
			{
				Vector2 lit = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
				lit = lit.LengthSquared() < 1f ? new Vector2(owner.direction, 0f) : Vector2.Normalize(lit);
				Lighting.AddLight(owner.MountedCenter + lit * 40f, 1.0f, 0.5f, 0.15f);
			}
			if (Projectile.owner == Main.myPlayer && _fired < total && Projectile.timeLeft % 2 == 0)
			{
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				float spread = (_fired / (float)total - 0.5f) * 0.28f;
				Vector2 vel = dir.RotatedBy(spread) * 12f;
				int dmg = System.Math.Max(1, Projectile.damage / 4);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + dir * 14f, vel,
					ProjectileID.Flames, dmg, Projectile.knockBack * 0.4f, Projectile.owner);
				if (id >= 0)
				{
					Projectile flame = Main.projectile[id];
					ProjectileBorrow.RetargetAsHenshin(flame);
					flame.DamageType = HenshinDamage.Instance;
					flame.friendly = true;
					flame.hostile = false;
					flame.penetrate = 4;
					flame.timeLeft = Math.Min(flame.timeLeft, 72);
				}
				_fired++;
			}
			if (_fired >= total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
			if (dir.LengthSquared() < 1f)
				dir = new Vector2(owner.direction, 0f);
			dir.Normalize();
			int frame = HenshinFxDraw.AgeFrame(28, Projectile.timeLeft, 2, HenshinFxDraw.FireFrames);
			HenshinFxDraw.BeginAdditive();
			Color c = HenshinFxDraw.WithAlpha(new Color(255, 120, 40), 0.75f);
			HenshinFxDraw.DrawFireFrame(owner.MountedCenter + dir * 28f, c, 0.7f, dir.ToRotation(), frame);
			HenshinFxDraw.DrawFireFrame(owner.MountedCenter + dir * 48f, HenshinFxDraw.WithAlpha(new Color(255, 160, 60), 0.55f), 0.5f, dir.ToRotation(), (frame + 3) % HenshinFxDraw.FireFrames);
			// 枪口小 TearFlame 单舌（非整 sheet 叠画）
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.TearFlame, owner.MountedCenter + dir * 18f,
				HenshinFxDraw.WithAlpha(new Color(255, 100, 40), 0.45f), 0.22f, dir.ToRotation());
			HenshinFxDraw.EndAdditive();
			return false;
		}
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

		private bool _castSfx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			// 施放音各端都播（_castSfx 防重，本弹活 2 tick）；之力不再响 Item1。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.GrassCast, Main.player[Projectile.owner].MountedCenter);
				HenshinSfx.Play(SfxKey.Swing, Main.player[Projectile.owner].MountedCenter, 0.6f, 0.4f);
			}
			if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Player owner = Main.player[Projectile.owner];
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
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
						// 粒子纪律：叶本体就是主体，枪口草屑去掉（原来只有主人端看得到）。
					}
				}
			}
			// R4 复查 M2：不再首帧 Kill，靠 timeLeft=2 自然消失（无伤、不画；生成叶子有 localAI[0] 防重），远端才跑得到 AI 播施放音。
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>种子炸弹：自绘种荚（Assets/Fx/SeedBomb/SeedBomb）抛物线飞行，不沿线撒粒子；落点生成 SeedBombBlastProj（320 直径 AoE + 6 帧爆圈），爆点仅 4 颗草屑。</summary>
	public class SeedBombProj : HenshinMoveProj
	{
		private static Asset<Texture2D> _pod;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 135;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
		}

		// 大招根弹：共享 UltCast 施放时自动响（不 claim）。
		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 12);

		public override void AI()
		{
			Projectile.velocity.Y += 0.25f;
			Projectile.rotation += 0.2f;
			// 光照 C 组：种荚是 lit，自己不发光（原 AddLight 0.85 删去）。
			// 光照第 2 轮 E 组（§4.15 大招例外）：飞行中外缘少量绿火把光点（自带光，代替色光），种荚半径 ~14px，粒子落在 22–30px。
			HenshinUltGlow.Emit(Projectile, 27f, DustID.GreenTorch, Vector3.Zero);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_pod ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SeedBomb/SeedBomb", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _pod.Value;
			// 种荚抛物线拖尾：草绿粗带 + 嫩黄绿芯。
			// 光照 C 组：尾巴吃光；本体去掉 140/150/120 下限，直接乘环境光。
			// 光照第 2 轮 E 组：拖尾与本体保色吃光（keepHue / LitHueFrom），暖光下不发灰。
			HenshinTrail.Strip(Projectile, new Color(90, 220, 80), 18f, new Color(225, 255, 170), 0.36f, 0f, 0.85f, lit: true, keepHue: true);
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), null, HenshinFxDraw.LitHueFrom(lightColor, Color.White), Projectile.rotation,
				new Vector2(tex.Width * 0.5f, tex.Height * 0.5f), 1.25f, SpriteEffects.None, 0f);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			// 大爆炸 + 草系命中两层（各端 OnKill）；光照由 SeedBombBlastProj 的爆炸配方接手（原来这里只亮 1 tick）。
			HenshinSfx.Play(SfxKey.BlastL, Projectile.Center);
			HenshinSfx.Play(SfxKey.GrassHit, Projectile.Center, 1f, -0.3f);
			Vector2 at = Projectile.Center;
			if (!Main.dedServ)
			{
				for (int i = 0; i < 2; i++)
				{
					Dust d = Dust.NewDustPerfect(at, DustID.Grass, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.6f, 1.2f), 40, default, 1.4f);
					d.noGravity = true;
				}
			}

			if (Projectile.owner == Main.myPlayer)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
					ModContent.ProjectileType<SeedBombBlastProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].Center = at;
					Main.projectile[id].netUpdate = true;
				}
			}
		}
	}

	/// <summary>身周持续风场。可见 Typhoon + Cyclone 旋转。半径 32 格。</summary>
	public class HurricaneFieldProj : HenshinMoveProj
	{
		private const float RadiusTiles = 32f;
		private static int BoxPx => (int)(RadiusTiles * 16f * 2f);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

		public override void SetDefaults()
		{
			Projectile.width = BoxPx;
			Projectile.height = BoxPx;
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
			Projectile.rotation += 0.18f;
			float edgeR = RadiusTiles * 16f;
			// R3 手感（洛奇亚大招）：起手风暴声 + 风环大爆 + 大招震屏；持续低风声每 30 tick；收束一声；场心 + 场边 8 盏灯。施放音由共享 UltCast 负责。
			int age = 120 - Projectile.timeLeft;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.WindCast, p.Center, 1f, -0.55f);
				HenshinSfx.Play(SfxKey.BlastM, p.Center, 0.8f, 0.15f);
				HenshinExplosion.Spawn(p.Center, BlastSize.L, HurricaneTint, edgeR * 1.1f, kenneyFrame: false, strength: 0.6f);
				HenshinHitFeel.Shake(Projectile.owner, p.Center, HitTier.Ultimate);
			}
			else if (age % 30 == 0 && Projectile.timeLeft > 20)
				HenshinSfx.Play(SfxKey.WindCast, p.Center, 0.55f, -0.75f);
			if (Projectile.timeLeft <= 14 && Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				HenshinSfx.Play(SfxKey.WindCast, p.Center, 0.7f, -0.2f);
			}
			if (!Main.dedServ)
			{
				// 光照 C 组：风大招是 mixed，只留场心一盏小灯（≤0.2）；场边 8 盏灯删去（风不发光，范围由云尘提示）。
				float a = Math.Min(1f, age / 10f) * Math.Min(1f, Projectile.timeLeft / 14f);
				// 光照第 2 轮 E 组（§4.15 大招例外）：场心灯 0.2→0.4 略亮；外环之外（1.15 倍半径）少量钻石光点（自带光）。
				Lighting.AddLight(p.Center, 0.22f * a, 0.3f * a, 0.4f * a);
				HenshinUltGlow.Emit(Projectile, edgeR * 1.15f, DustID.GemDiamond, Vector3.Zero, dustScale: 1.3f);
			}
			// 场边云尘 = 范围提示（F14），保留
			for (int i = 0; i < 1; i++)
			{
				Vector2 edge = Main.rand.NextVector2CircularEdge(edgeR, edgeR);
				Dust.NewDustPerfect(Projectile.Center + edge, DustID.Cloud, edge.RotatedBy(1.2f) * 0.04f, 100, new Color(80, 120, 220), 1.3f).noGravity = true;
			}
		}

		private static readonly Color HurricaneTint = new(170, 220, 255);

		/// <summary>场心 + 半径 r 上 8 点的平均环境光（大风环吃光用）。</summary>
		private static Color AvgRingLight(Vector2 c, float r)
		{
			Vector3 sum = HenshinFxDraw.LightAt(c).ToVector3();
			for (int i = 0; i < 8; i++)
				sum += HenshinFxDraw.LightAt(c + (i * MathHelper.PiOver4).ToRotationVector2() * r).ToVector3();
			return new Color(sum / 9f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 30);
			if (Main.rand.NextBool(4))
				target.AddBuff(BuffID.Confused, 20);
			Vector2 knock = target.Center - Projectile.Center;
			if (knock != Vector2.Zero)
				target.velocity += Vector2.Normalize(knock) * 3.5f;
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, HurricaneTint, target.Center, 0.5f);
			H7HitFxProj.Spawn(Projectile, target.Center, 0, 0.7f, H7HitFxProj.TierIndex(tier));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 自绘 WindRing 两层反向旋转（按世界直径缩放）；不再依赖原版 Typhoon 贴图
			Texture2D ring = H7Tex.Get("WindRing");
			float diamPx = RadiusTiles * 16f * 2f;
			float a = Math.Min(1f, (120 - Projectile.timeLeft) / 10f) * Math.Min(1f, Projectile.timeLeft / 14f);
			int fr = (int)(Main.GameUpdateCount / 3) & 3;
			// 光照 C 组（mixed）：外环 = 风暴本体，颜色乘光（黑处变暗、不自发光）；内环 = 小发光部分，强度 0.65→0.4。
			// shortcut: 外环直径约 59 格，只取场心 + 8 个环上点的平均光，不逐段；若用户看到半边洞穴半边地表时亮度不对再拆段画。
			Color ringLight = AvgRingLight(Projectile.Center, diamPx * 0.46f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveSheet(ring, 1, 4, fr, Projectile.Center,
				HenshinFxDraw.WithAlpha(HenshinFxDraw.LitHueFrom(ringLight, new Color(190, 230, 255)), 0.5f * a), diamPx * 0.92f / 112f, Projectile.rotation); // 光照第 2 轮 E 组：保色
			HenshinFxDraw.DrawAdditiveSheet(ring, 1, 4, (fr + 2) & 3, Projectile.Center,
				HenshinFxDraw.WithAlpha(Color.White, 0.4f * a), diamPx * 0.5f / 112f, -Projectile.rotation * 1.3f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
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
				Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), y), DustID.YellowTorch, new Vector2(0f, 4f), 60, HenshinFxDraw.SparkGold, 1.4f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 150);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>近身战：短时多段贴身打击；可见 HitJagged 冲击。</summary>
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
				if (Projectile.owner == Main.myPlayer)
				{
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
						ModContent.ProjectileType<GenericSlashProj>(), Math.Max(1, Projectile.damage / 3), Projectile.knockBack * 0.3f, Projectile.owner, DustID.Blood, dir);
					if (id >= 0)
						Main.projectile[id].ai[2] = 1f; // jagged flash
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			float rot = p.direction > 0 ? 0.4f : MathHelper.Pi - 0.4f;
			int jagged = HenshinFxDraw.AgeFrame(48, Projectile.timeLeft, 4, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center, HenshinFxDraw.WithAlpha(new Color(255, 220, 200), 0.7f), 0.85f, rot, jagged);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>逆鳞：3s 吟唱，身周持续释放追踪火球；结束自身混乱。</summary>
	public class OutrageDirectorProj : HenshinMoveProj
	{
		private const int FireInterval = 4;
		// R4：22 发 -> 33 发（+50%），间隔 8 -> 4 tick，吟唱 180 -> 132 tick；单发倍率 x0.7 使总伤害 22 -> 23.1 发等效（约 +5%）。
		private const int Lifetime = 132;
		private const float PerShotDamageScale = 0.7f;

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
			owner.velocity *= 0.92f;
			owner.AddBuff(BuffID.Slow, 10);

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.CultistBossFireBall);
				// 起手：共享 UltCast 已由根弹旗播（旧 Item20 删去免叠），这里只加一声低沉龙吼 + 中震。
				HenshinSfx.Play(SfxKey.DragonCast, owner.Center, 1f, -0.45f);
				HenshinHitFeel.Shake(Projectile.owner, owner.Center, HitTier.Heavy);
			}
			// 狂暴期身周红光（旧版导演弹不发光）。光照第 2 轮 E 组：×1.3。
			Lighting.AddLight(owner.Center, 1.17f, 0.42f, 0.16f);
			// 大招发光粒子：导演活满 132 tick（火球在 Stage7/C 组，不碰），由导演发；火把 / 紫火把交替，落在红环（半径 62–75px）外缘 72–99px。
			HenshinUltGlow.Emit(Projectile, 90f, (Main.GameUpdateCount / 2 & 1) == 0 ? DustID.Torch : DustID.PurpleTorch, Vector3.Zero);
			if (Projectile.timeLeft == 1)
			{
				// 收尾爆发（纯表现，不加伤害）：大爆声 + 大招命中音 + L 档红爆 + 主人端大震。
				HenshinSfx.Play(SfxKey.BlastL, owner.Center);
				HenshinSfx.PlayCooldown(SfxKey.HitUlt, owner.Center, 20);
				HenshinExplosion.Spawn(owner.Center, BlastSize.L, new Color(255, 80, 50), 300f);
				HenshinHitFeel.Shake(Projectile.owner, owner.Center, HitTier.Ultimate);
			}

			if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % FireInterval == 0)
			{
				float ang = Main.rand.NextFloat(MathHelper.TwoPi);
				Vector2 spawn = owner.Center + ang.ToRotationVector2() * 48f;
				Vector2 vel = ang.ToRotationVector2() * 7f;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<OutrageFireballProj>(), Math.Max(1, (int)(Projectile.damage * PerShotDamageScale)), Projectile.knockBack * 0.6f,
					Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}

			if (Projectile.timeLeft == 1)
				owner.AddBuff(BuffID.Confused, 120);
		}

		public override bool? CanDamage() => false;

		/// <summary>H4：狂暴期身周脉动红环（OutrageBurst 第 4 帧环形），随时间收紧再扩散，表达「失控乱舞」。</summary>
		public override bool PreDraw(ref Color lightColor)
		{
			float pulse = 0.5f + 0.5f * MathF.Sin(Projectile.timeLeft * 0.35f);
			float fade = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
			Texture2D ring = H4Fx.Burst(H4Fx.StyleOutrage);
			HenshinFxDraw.BeginAdditive();
			int h = ring.Height / 5;
			Rectangle src = new(0, h * 3, ring.Width, h);
			Main.EntitySpriteDraw(ring, (Projectile.Center - Main.screenPosition).Floor(), src,
				HenshinFxDraw.WithAlpha(new Color(255, 120, 60), (0.35f + 0.3f * pulse) * fade), 0f,
				new Vector2(ring.Width * 0.5f, h * 0.5f), 2.4f + pulse * 0.5f, SpriteEffects.None, 0f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>预知未来：紫 SoftGlow 蓄力圈后发射 SustainedBeam HyperBeam（IgnoreDef）。</summary>
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
			Player owner = Main.player[Projectile.owner];
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f), DustID.MagicMirror, Vector2.Zero, 100, new Color(200, 120, 255), 1.3f).noGravity = true;

			if (Projectile.timeLeft == 8 && Projectile.owner == Main.myPlayer)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<SustainedBeamProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
					DustID.PurpleTorch, 0f, SustainedBeamProj.ModeHyperBeam);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.IgnoreDefensePartial = true;
				SoundEngine.PlaySound(SoundID.Item29, Projectile.Center);
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			float charge = 1f - Projectile.timeLeft / 70f;
			HenshinFxDraw.BeginAdditive();
			Color purple = HenshinFxDraw.WithAlpha(new Color(180, 90, 255), 0.5f + charge * 0.45f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, purple, 0.5f + charge * 0.7f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(160, 80, 255), 0.35f * charge), 0.9f + charge * 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>恶之波动 / 暗锥：ShadowBeam 贴图锥 + 紫 LightShot 扇。</summary>
	public class DarkPulseConeProj : HenshinMoveProj
	{
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly;

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
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.ShadowBeamFriendly);
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
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

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 from = owner.MountedCenter;
			float t = 1f - Projectile.timeLeft / 14f;
			Texture2D shadow = ProjectileBorrow.RequestProjectileTexture(ProjectileID.ShadowBeamFriendly);
			HenshinFxDraw.BeginAdditive();
			Color purple = HenshinFxDraw.WithAlpha(new Color(160, 60, 220), 0.85f);
			for (int i = -2; i <= 2; i++)
			{
				Vector2 tip = from + _dir.RotatedBy(i * 0.12f) * (80f + t * 160f);
				HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, from, tip, purple, 12f);
				HenshinFxDraw.DrawBeamSegment(shadow, from, tip, HenshinFxDraw.WithAlpha(new Color(120, 40, 180), 0.7f), 8f);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>抓狂：残血加伤乱打。</summary>
	/// <summary>抓狂：以玩家为圆心半径 15 格，短时刷出杂乱交错爪痕多段伤害；已损 HP% 提伤。</summary>
	public class FlailBarrageProj : HenshinMoveProj
	{
		private const int Lifetime = 40;
		private const float RadiusTiles = 15f;
		private const int MaxScratches = 18;
		private const float LineWidth = 40f;
		private const float LineSpacing = 14f;

		private struct Scratch
		{
			public Vector2 Mid;
			public float Rot;
			public float Len;
			public int Birth;
		}

		private readonly Scratch[] _scratches = new Scratch[MaxScratches];
		private int _count;
		private static Asset<Texture2D> _clawSheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = (int)(RadiusTiles * 16f * 2f);
			Projectile.height = (int)(RadiusTiles * 16f * 2f);
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 4;
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
			float missing = 1f - p.statLife / (float)System.Math.Max(1, p.statLifeMax2);
			Projectile.localAI[1] = 1f + missing * 0.8f;

			int age = Lifetime - Projectile.timeLeft;
			if (age % 2 == 0 && _count < MaxScratches)
			{
				float radius = RadiusTiles * 16f;
				Vector2 mid = p.Center + Main.rand.NextVector2Circular(radius * 0.92f, radius * 0.92f);
				float rot = Main.rand.NextFloat(MathHelper.TwoPi);
				float len = Main.rand.NextFloat(3.5f, 5.5f) * 16f;
				_scratches[_count++] = new Scratch
				{
					Mid = mid,
					Rot = rot,
					Len = len,
					Birth = age
				};
				// 每道爪痕一声快挥（限频 4 tick，避免 20 声糊成噪音）；爪痕本体是 Scratch 贴图，点缀尘去掉。
				HenshinSfx.PlayCooldown(SfxKey.Swing, mid, 4, 0.75f, Main.rand.NextFloat(-0.2f, 0.25f));
				Lighting.AddLight(mid, 0.25f, 0.27f, 0.32f); // 光照 C 组：mixed，≤0.2（原 0.4）；光照第 2 轮 E 组大招例外 → 0.32
			}
			// 光照第 2 轮 E 组：爪痕区（半径 15 格）外缘少量蓝宝石光点（自带光）。位置 = 同步的玩家中心。
			HenshinUltGlow.Emit(Projectile, RadiusTiles * 16f * 1.1f, DustID.GemSapphire, Vector3.Zero, dustScale: 1.3f);
		}

		private int _lastHitFx = -100;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player p = Main.player[Projectile.owner];
			float maxR = RadiusTiles * 16f;
			Vector2 targetCenter = targetHitbox.Center.ToVector2();
			if (Vector2.DistanceSquared(p.Center, targetCenter) > (maxR + 64f) * (maxR + 64f))
				return false;
			int age = Lifetime - Projectile.timeLeft;
			for (int i = 0; i < _count; i++)
			{
				ref Scratch s = ref _scratches[i];
				// 爪痕存活期内保持较宽命中（比视觉略宽）
				if (age - s.Birth > 14)
					continue;
				Vector2 dir = new Vector2((float)System.Math.Cos(s.Rot), (float)System.Math.Sin(s.Rot));
				Vector2 perp = new Vector2(-dir.Y, dir.X);
				Vector2 half = dir * (s.Len * 0.5f);
				for (int line = -1; line <= 1; line++)
				{
					Vector2 o = s.Mid + perp * (line * LineSpacing) - half;
					Vector2 e = s.Mid + perp * (line * LineSpacing) + half;
					float point = 0f;
					if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), o, e, LineWidth, ref point))
						return true;
				}
				// 爪痕中心附近额外圆判，提高命中率
				if (Vector2.DistanceSquared(s.Mid, targetCenter) < 36f * 36f)
					return true;
			}
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= Projectile.localAI[1] > 0 ? Projectile.localAI[1] : 1f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 大招档（TierOf）：白闪 + 震屏（20 tick 一次，乱打时成节奏脉冲）。血尘 3 颗 → 同步命中星（每 6 tick 最多 1 个，带命中音）。
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Light);
			HenshinHitFeel.Flash(target, tier);
			HenshinHitFeel.Shake(Projectile.owner, target.Center, tier);
			int age = Lifetime - Projectile.timeLeft;
			if (age - _lastHitFx >= 6)
			{
				_lastHitFx = age;
				TackleHitFxProj.Spawn(Projectile, target.Center, 0, SfxKey.HitHeavy, tier);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_clawSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Scratch/Scratch", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _clawSheet.Value;
			int frameH = tex.Height / 5;
			int age = Lifetime - Projectile.timeLeft;
			for (int i = 0; i < _count; i++)
			{
				ref Scratch s = ref _scratches[i];
				int life = age - s.Birth;
				if (life < 0 || life > 14)
					continue;
				// 与 Scratch 同节奏：0/1/2 各 2 tick，3 停 4 tick，4 碎开淡出。
				int frame = life < 2 ? 0 : life < 4 ? 1 : life < 6 ? 2 : life < 10 ? 3 : 4;
				float opacity = frame == 4 ? System.Math.Clamp((14 - life) / 4f, 0f, 1f) : 1f;
				Vector2 dir = new Vector2((float)System.Math.Cos(s.Rot), (float)System.Math.Sin(s.Rot));
				Vector2 start = s.Mid - dir * (s.Len * 0.5f);
				SpriteEffects fx = System.Math.Cos(s.Rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
				Rectangle src = new(0, frameH * frame, tex.Width, frameH);
				// 光照 C 组：爪痕 mixed，按爪痕中点取光，再向白色拉 0.4（原 Color.White 不吃光）。
				Color sc = Color.Lerp(HenshinFxDraw.LightAt(s.Mid), Color.White, 0.15f);
				Main.EntitySpriteDraw(tex, (start - Main.screenPosition).Floor(), src, sc * opacity, s.Rot,
					new Vector2(0f, frameH * 0.5f), s.Len / tex.Width, fx, 0f);
			}
			return false;
		}
	}

	/// <summary>
	/// 咬住/火焰牙/咬碎：360° 朝鼠标的上下獠牙合拢（两次咬合）。
	/// ai0=体型倍率；ai1=破甲 tick（咬碎）；ai2=灼烧 tick（火焰牙）。ai 槽已满，方向弧度由 SendExtraAI 同步（拥有者客户端首帧求一次，netUpdate 补发）。
	/// 贴图 Assets/Fx/Bite/BiteJaws(+_Light)、FireFang(+_Light)，各 6 帧 × 3 tick，64×56 art px（scale 2 → 128×112 px），原点 (64,56)；
	/// 画面世界尺寸沿用旧 64×56×s（s=size×VisualScale）→ 绘制缩放 = s/2。朝左半圈 FlipVertically 保持上颚在上。
	/// </summary>
	public class BiteArcProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private const float VisualScale = 4f;
		private const int SheetFrames = 6;
		private const int SheetTicksPerFrame = 3;
		private float _size = 1f;
		private float _aim;
		private bool _hasAim;
		private static Asset<Texture2D> _jaws, _jawsLight, _fire, _fireLight;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool IsFire => Projectile.ai[2] > 0f;
		private bool IsCrunch => Projectile.ai[1] > 0f;
		private Vector2 Dir => _aim.ToRotationVector2();

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = (int)(64 * VisualScale);
			Projectile.height = (int)(56 * VisualScale);
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.ownerHitCheck = true;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			base.SendExtraAI(writer);
			writer.Write(_hasAim);
			if (_hasAim)
				writer.Write(_aim);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			base.ReceiveExtraAI(reader);
			_hasAim = reader.ReadBoolean();
			if (_hasAim)
				_aim = reader.ReadSingle();
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
				_size = Projectile.ai[0] > 0.1f ? Projectile.ai[0] : 1f;
				if (Projectile.owner == Main.myPlayer)
				{
					// 360° 朝鼠标：拥有者客户端只求一次，之后经 extra AI 同步给其他端
					Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
					_aim = aim.ToRotation();
					_hasAim = true;
					if (Math.Abs(aim.X) > 0.05f)
						owner.ChangeDir(aim.X > 0f ? 1 : -1);
					Projectile.netUpdate = true;
				}
				float s0 = _size * VisualScale;
				Projectile.width = (int)(64 * s0);
				Projectile.height = (int)(56 * s0);
				// 起手（各端）：本弹已 claim，之力不再叠一声 Item1。火焰牙加一层火系施放。
				HenshinSfx.Play(SfxKey.Swing, owner.Center, 1f, -0.3f);
				if (IsFire)
					HenshinSfx.Play(SfxKey.FireCast, owner.Center, 0.7f);
			}
			if (!_hasAim)
				return; // 远端等待首包弧度
			float reach = _size * VisualScale;
			Vector2 dir = Dir;
			Projectile.Center = owner.MountedCenter + dir * (40f + 8f * reach);

			int phase = Lifetime - Projectile.timeLeft;
			if (phase == 6 || phase == 12)
			{
				// 两次咬合瞬间：保留合齿声；咬合尘去掉（命中尘交给 BiteHitFxProj）。
				// R4（G3 跨组需求）：咬碎与远距咬碎同款钢"咔"，替原版 Item10；咬住 / 火焰牙保留合齿声。
				if (IsCrunch)
					HenshinSfx.Play(SfxKey.SteelHit, Projectile.Center, 0.6f, -0.2f);
				else
					SoundEngine.PlaySound(SoundID.Item10 with { Pitch = -0.1f, Volume = 0.5f }, Projectile.Center);
			}
			// 光照 C 组：牙是实体。咬住 lit 不加光；咬碎 / 火焰牙 mixed，光 ≤0.2（原 0.8 / 0.9）。
			if (IsCrunch && (phase is >= 6 and <= 8 || phase is >= 12 and <= 14))
				Lighting.AddLight(Projectile.Center, 0.14f, 0.11f, 0.2f); // 咬合瞬间钢紫灯（G3 需求）
			if (IsFire)
				Lighting.AddLight(Projectile.Center, 0.2f, 0.09f, 0.02f);
		}

		public override bool? CanDamage()
		{
			if (!_hasAim)
				return false;
			int age = Lifetime - Projectile.timeLeft;
			if (age <= 6 || (age >= 9 && age <= 15))
				return null;
			return false;
		}

		/// <summary>按方向旋转的判定框（SAT：OBB 对 AABB），OBB 半长 32s（沿咬向）× 28s（上下颚方向）。</summary>
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!_hasAim)
				return false;
			float s = _size * VisualScale;
			float hx = 32f * s, hy = 28f * s;
			Vector2 f = Dir;
			Vector2 n = new(-f.Y, f.X);
			Vector2 d = targetHitbox.Center.ToVector2() - Projectile.Center;
			Vector2 th = targetHitbox.Size() * 0.5f;
			if (Math.Abs(d.X) > th.X + Math.Abs(f.X) * hx + Math.Abs(n.X) * hy)
				return false;
			if (Math.Abs(d.Y) > th.Y + Math.Abs(f.Y) * hx + Math.Abs(n.Y) * hy)
				return false;
			if (Math.Abs(Vector2.Dot(d, f)) > hx + Math.Abs(f.X) * th.X + Math.Abs(f.Y) * th.Y)
				return false;
			if (Math.Abs(Vector2.Dot(d, n)) > hy + Math.Abs(n.X) * th.X + Math.Abs(n.Y) * th.Y)
				return false;
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff(BuffID.BrokenArmor, (int)Projectile.ai[1]);
			if (Projectile.ai[2] > 0)
				target.AddBuff(BuffID.OnFire, (int)Projectile.ai[2]);
			// 0 伤害纯视觉命中：落在目标碰撞盒内离咬点最近处
			Vector2 hitPos = Vector2.Clamp(Projectile.Center, target.position, target.BottomRight);
			// 咬合 = 重命中（技能档；咬碎、火焰牙同档，换色）。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy),
				IsFire ? new Color(255, 140, 50) : IsCrunch ? new Color(190, 160, 255) : new Color(255, 235, 225), hitPos,
				IsFire || IsCrunch ? 1f : 0.5f);
			if (Projectile.owner != Main.myPlayer)
				return;
			int kind = IsFire ? 1 : IsCrunch ? 2 : 0;
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), hitPos, Vector2.Zero,
				ModContent.ProjectileType<BiteHitFxProj>(), 0, 0f, Projectile.owner, kind, _aim);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_hasAim)
				return false;
			// 贴图懒加载：PreDraw 不会在专用服务器上调用
			Asset<Texture2D> baseAsset, lightAsset;
			if (IsFire)
			{
				_fire ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/FireFang", AssetRequestMode.ImmediateLoad);
				_fireLight ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/FireFang_Light", AssetRequestMode.ImmediateLoad);
				baseAsset = _fire;
				lightAsset = _fireLight;
			}
			else
			{
				_jaws ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteJaws", AssetRequestMode.ImmediateLoad);
				_jawsLight ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteJaws_Light", AssetRequestMode.ImmediateLoad);
				baseAsset = _jaws;
				lightAsset = _jawsLight;
			}
			Texture2D tex = baseAsset.Value;
			int frameH = tex.Height / SheetFrames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / SheetTicksPerFrame, 0, SheetFrames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			float s = _size * VisualScale;
			float scale = s * 0.5f; // 64 art px × 2 = 128 px → 世界宽 64s
			float rot = _aim;
			bool left = Math.Cos(rot) < 0;
			SpriteEffects fx = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			Vector2 pos = Projectile.Center - Main.screenPosition;
			// 咬碎：咬合帧沉重抖动
			if (IsCrunch && (frame == 2 || frame == 4))
				pos += new Vector2((float)Math.Sin(Projectile.timeLeft * 5.1f), (float)Math.Cos(Projectile.timeLeft * 4.3f)) * 3f;
			pos = pos.Floor();
			float opacity = frame == SheetFrames - 1 ? Projectile.timeLeft / (float)SheetTicksPerFrame : 1f;
			// 光照 C 组：牙本体直接乘环境光（去掉 120/110/100 下限）。
			Color c = lightColor;
			if (IsCrunch)
				c = MultiplyColor(c, new Color(185, 175, 220));
			Main.EntitySpriteDraw(tex, pos, src, c * opacity, rot, origin, scale, fx, 0f);
			// 叠加层：Additive 保 Alpha（透明度进 Alpha 通道，不当 A=0 成品）。只给火焰牙 / 咬碎（mixed）；咬住是 lit，不画发光层。
			if (IsFire || IsCrunch)
			{
				Color lc = IsCrunch ? new Color(200, 160, 255) : Color.White;
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(lightAsset.Value, pos, src, lc * opacity, rot, origin, scale, fx, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}

		private static Color MultiplyColor(Color a, Color b)
			=> new(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255);
	}

	/// <summary>咬合命中：0 伤害纯视觉。ai0=种类（0 咬住 / 1 火焰牙 / 2 咬碎），ai1=咬向弧度。贴图 Assets/Fx/Bite/BiteHit(+_Light)，5 帧 × 3 tick。</summary>
	public class BiteHitFxProj : ModProjectile
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = Frames * TicksPerFrame;
		private static Asset<Texture2D> _sheet;
		private static Asset<Texture2D> _light;

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
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				int kind = (int)Projectile.ai[0];
				if (!Main.dedServ)
				{
					// 命中音（各端；本弹随生成包同步）：咬住/咬碎 = 重命中（咬碎更低沉），火焰牙 = 火系命中 + 轻命中。
					if (kind == 1)
					{
						HenshinSfx.PlayCooldown(SfxKey.FireHit, Projectile.Center, 3);
						HenshinSfx.PlayCooldown(SfxKey.HitLight, Projectile.Center, 3, 0.8f);
					}
					else
						HenshinSfx.PlayCooldown(SfxKey.HitHeavy, Projectile.Center, 3, kind == 2 ? 1f : 0.8f, kind == 2 ? -0.2f : 0f);
					// 光照 C 组：命中闪是 glow 例外；咬住（lit）光脉冲 ×0.5。
					HenshinExplosion.SpawnLight(Projectile.Center, kind == 1 ? new Color(255, 140, 50) : kind == 2 ? new Color(190, 160, 255) : new Color(255, 235, 225), BlastSize.S,
						kind == 0 ? 0.5f : 1f);
					Vector2 d = Projectile.ai[1].ToRotationVector2();
					int count = kind == 2 ? 3 : 2;
					for (int i = 0; i < count; i++)
					{
						Vector2 v = Main.rand.NextVector2Circular(3f, 3f) + d * 1.2f;
						int type = kind == 1 ? DustID.Torch : kind == 2 ? (i % 2 == 0 ? DustID.Stone : DustID.Silver) : DustID.Blood;
						Dust dust = Dust.NewDustPerfect(Projectile.Center, type, v, kind == 0 ? 80 : 0, default, kind == 1 ? 1.3f : 1.1f);
						dust.noGravity = kind == 1;
					}
				}
			}
			// 光照 C 组：咬住（ai0=0）不再自加光，命中光只靠上面的弱脉冲；火焰牙 / 咬碎保留短光。
			if (Projectile.timeLeft > Lifetime - 6 && Projectile.ai[0] != 0f)
				Lighting.AddLight(Projectile.Center, Projectile.ai[0] == 1f ? 0.8f : 0.3f, Projectile.ai[0] == 1f ? 0.35f : 0.3f, Projectile.ai[0] == 1f ? 0.1f : 0.35f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteHit", AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteHit_Light", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			int kind = (int)Projectile.ai[0];
			float scale = kind == 2 ? 2.2f : 1.6f;
			Color tint = kind == 1 ? new Color(255, 170, 90) : kind == 2 ? new Color(200, 180, 235) : Color.White;
			// 光照 C 组：本体乘环境光（去掉 140/130/120 下限）；冲击发光层保留（命中闪例外），咬住减半。
			Color c = new(lightColor.R * tint.R / 255, lightColor.G * tint.G / 255, lightColor.B * tint.B / 255);
			Main.EntitySpriteDraw(tex, pos, src, c, 0f, origin, scale, SpriteEffects.None, 0f);
			if (frame <= 2)
			{
				Color lc = kind == 1 ? new Color(255, 150, 60) : kind == 2 ? new Color(200, 160, 255) : HenshinFxDraw.WithAlpha(Color.White, 0.5f);
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_light.Value, pos, src, lc, 0f, origin, scale, SpriteEffects.None, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>龙之波动：从角色连发 10 枚星云奥秘外观弹（70%、不穿透、不追踪），碰撞后走原版爆炸碎片。</summary>
	public class NebulaPulseDirectorProj : HenshinMoveProj
	{
		private const int Total = 10;
		private bool _castSfx;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			// 施放音各端一次（放在主人门之前；localAI 归主人逻辑用，这里用私有位防重）。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.DragonCast, Main.player[Projectile.owner].MountedCenter, 1f, 0.15f);
			}
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

			Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
			if (dir.LengthSquared() < 1f)
				dir = new Vector2(owner.direction, 0f);
			dir.Normalize();
			// 直线连发：同向速度，仅出生点轻微错落（不转角度）
			Vector2 perp = new Vector2(-dir.Y, dir.X);
			Vector2 spawn = owner.MountedCenter + dir * 12f + perp * ((fired - (Total - 1) * 0.5f) * 3.5f);
			Vector2 vel = dir * 12f;
			int per = System.Math.Max(1, Projectile.damage / 4);
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
				ModContent.ProjectileType<NebulaPulseShardProj>(), per, Projectile.knockBack, Projectile.owner);
			if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
			{
				tagged.EasyCrit = EasyCrit;
				tagged.Homing = Homing;
				tagged.HomingTurnRate = HomingTurnRate;
				tagged.HomingRangeTiles = Math.Max(tagged.HomingRangeTiles, HomingRangeTiles);
				tagged.Delivery = Delivery == MoveDelivery.None ? MoveDelivery.Barrage : Delivery;
			}

			Projectile.localAI[0] = fired + 1;
			if (fired + 1 >= Total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 龙之波动直飞弹：自管运动（不套原版 AI），自绘 PulseBolt 4 帧贴图（Assets/Fx/DragonPulse）；
	/// 穿透 1；广角镜可将 Homing 打开并由全局 AI 转向。
	/// 亡时生成 12 枚碎片（沿用原版 NebulaArcanumExplosionShotShard 做运动/判定，外观改画 PulseShard）+ 自绘命中环。
	/// </summary>
	public class NebulaPulseShardProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10);

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 113;
			Projectile.tileCollide = true;
			Projectile.scale = 0.7f;
			Projectile.extraUpdates = 0;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.NebulaArcanumExplosionShotShard);
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Player owner = Main.player[Projectile.owner];
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (dir.LengthSquared() < 1f)
					dir = new Vector2(owner.direction, 0f);
				Projectile.velocity = Vector2.Normalize(dir) * 12f;
			}
		}

		public override void AI()
		{
			if (Projectile.velocity.LengthSquared() > 0.01f)
				Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.2f, 0.55f, 0.85f);
		}

		public override void OnKill(int timeLeft)
		{
			// R4：命中不再撒 12 枚碎片，改一枚龙气冲击波（DragonPulseBlastProj：AoE + 自绘 PulseBlast 动画）。
			// G1：爆声 / 爆光改由冲击波首帧各端放（旧 Item14 与 1 tick AddLight 删去）。

			if (Projectile.owner != Main.myPlayer)
				return;
			// 预算：旧 12 碎片每枚 0.65x，单体约 2~4 枚命中（约 1.3~2.6x per）；冲击波单次 1.5x per。
			int blastDmg = System.Math.Max(1, (int)(Projectile.damage * 1.5f));
			int blastId = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<DragonPulseBlastProj>(), blastDmg, Projectile.knockBack, Projectile.owner);
			if (blastId >= 0 && Main.projectile[blastId].ModProjectile is IHenshinMoveProj blastTag)
			{
				blastTag.EasyCrit = EasyCrit;
				blastTag.Delivery = Delivery;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 青白细尾（10 发直线连射，尾巴连成一束龙气）。
			HenshinTrail.Strip(Projectile, new Color(80, 170, 255), 13f, new Color(220, 240, 255), 0.36f, 0f, 0.75f);
			Texture2D tex = H4Fx.PulseBolt();
			int fh = tex.Height / 4;
			int frame = (int)(Main.GameUpdateCount / 4 % 4);
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), new Rectangle(0, fh * frame, tex.Width, fh), Color.White,
				Projectile.rotation, new Vector2(tex.Width * 0.62f, fh * 0.5f), 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>
	/// 龙之波动爆炸碎片：紫染色 + 命中能量走碎屑（1×Factor）。
	/// 只挂在原版 620 上；变身时玩家自己的星云奥秘碎片不会带 CrumbHitEnergy。
	/// </summary>
	public sealed class HenshinNebulaShardTintGlobal : GlobalProjectile
	{
		public const string CrumbHitContext = "HenshinCrumbHit";

		public override bool InstancePerEntity => true;
		public bool TintPurple;
		public bool CrumbHitEnergy;

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
			=> entity.type == ProjectileID.NebulaArcanumExplosionShotShard;

		public static void MarkCrumb(Projectile proj, bool tintPurple = false)
		{
			if (proj == null || !proj.active)
				return;
			if (!proj.TryGetGlobalProjectile(out HenshinNebulaShardTintGlobal g))
				return;
			g.CrumbHitEnergy = true;
			if (tintPurple)
				g.TintPurple = true;
		}

		/// <summary>
		/// 本模 Retarget 的爆炸碎片。优先读同步标记；
		/// ExtraAI 未到时，用 HenshinDamage 的 620 兜底（原版星云奥秘仍是 Magic，不会误伤）。
		/// </summary>
		public static bool UsesCrumbHitEnergy(Projectile proj)
		{
			if (proj == null)
				return false;
			if (proj.ModProjectile is DragonPulseBlastProj)
				return true; // R4：龙之波动冲击波替代旧碎片，同样按碎屑能量
			if (proj.TryGetGlobalProjectile(out HenshinNebulaShardTintGlobal g) && g.CrumbHitEnergy)
				return true;
			return proj.type == ProjectileID.NebulaArcanumExplosionShotShard
				&& proj.DamageType == HenshinDamage.Instance;
		}

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source?.Context == CrumbHitContext)
				MarkCrumb(projectile, tintPurple: projectile.type == ProjectileID.NebulaArcanumExplosionShotShard);
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
		{
			bitWriter.WriteBit(TintPurple);
			bitWriter.WriteBit(CrumbHitEnergy);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			TintPurple = bitReader.ReadBit();
			CrumbHitEnergy = bitReader.ReadBit();
		}

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
			// 碎片：自绘 PulseShard（单帧 16x16），不再画原版贴图、不撒尘
			Texture2D tex = H4Fx.PulseShard();
			Main.EntitySpriteDraw(tex, (projectile.Center - Main.screenPosition).Floor(), null, Color.White,
				projectile.velocity.ToRotation() + MathHelper.PiOver4, tex.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>龙息：128 格持续火线，Fire 帧动画 + SoftGlow 尖端；高频线伤。禁用飞散 Flames。</summary>
	public class DragonBreathConeProj : HenshinMoveProj
	{
		public const float BreathLength = 128f * 16f;
		public const int Life = 52;
		public const float LineWidth = 48f;

		private Vector2 _from;
		private Vector2 _to;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 5;
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

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// 龙息：龙系施放 + 一层低沉火声（同一 tick 两个不同键，不算叠同声）。
				HenshinSfx.Play(SfxKey.DragonCast, owner.Center);
				HenshinSfx.Play(SfxKey.FireCast, owner.Center, 0.6f, -0.3f);
			}

			_from = owner.MountedCenter;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * BreathLength;
			if (!HenshinTileRay.OwnerPierces(Projectile, MoveDelivery.Spread))
			{
				float blocked = HenshinTileRay.FirstSolidDistance(_from, aim, BreathLength);
				_to = _from + aim * blocked;
				if (blocked < BreathLength - 8f)
					HenshinTileImpact.At(_to, -aim, TileImpactKind.DragonFire, material: Projectile.timeLeft % 4 == 0);
			}
			Projectile.Center = Vector2.Lerp(_from, _to, 0.2f);
			owner.direction = aim.X >= 0f ? 1 : -1;
			Lighting.AddLight(Projectile.Center, 0.85f, 0.3f, 0.75f);

			// H4：火柱已由 Fire 帧密铺表达，不再沿线/枪口撒尘（粒子会挡住火帧）。
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, LineWidth, ref _);
		}

		private int _fxAge = -99;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 30);
			// H4：高频线伤，命中特效每 12 tick 至多一个。
			int age = Life - Projectile.timeLeft;
			if (age - _fxAge >= 12)
			{
				_fxAge = age;
				H4Fx.Hit(Projectile, target.Center, H4Fx.StyleDragon, 1.0f, G1HitSfx.Code(SfxKey.DragonHit, 6));
				HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(220, 90, 255), target.Center);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float env = Projectile.timeLeft / (float)Life;
			if (env > 0.9f)
				env = (1f - env) / 0.1f;
			else if (env < 0.12f)
				env = env / 0.12f;
			else
				env = 1f;

			Vector2 dir = _to - _from;
			float rot = dir.ToRotation();
			int baseFrame = HenshinFxDraw.AgeFrame(Life, Projectile.timeLeft, 2, HenshinFxDraw.FireFrames);
			Color pink = HenshinFxDraw.WithAlpha(new Color(220, 90, 255), 0.85f * env);
			Color orange = HenshinFxDraw.WithAlpha(new Color(255, 110, 80), 0.75f * env);

			HenshinFxDraw.BeginAdditive();
			// SoftGlow 底纹填缝 + Fire 帧密铺（128 格射程，约每 0.5 格一段）
			HenshinFxDraw.DrawContinuousBeam(_from, _to,
				HenshinFxDraw.WithAlpha(new Color(255, 140, 220), 0.45f * env),
				HenshinFxDraw.WithAlpha(new Color(180, 60, 255), 0.35f * env),
				14f, 28f);
			const int segments = 56;
			Vector2 n = Vector2.Normalize(dir);
			Vector2 perp = new Vector2(-n.Y, n.X);
			for (int i = 0; i < segments; i++)
			{
				float u = (i + 0.5f) / segments;
				Vector2 pos = Vector2.Lerp(_from, _to, u);
				float lateral = MathF.Sin(u * 18f + Projectile.timeLeft * 0.12f) * (4f + u * 6f);
				pos += perp * lateral;
				float sc = 0.32f + u * 0.38f;
				int frame = (baseFrame + i * 2) % HenshinFxDraw.FireFrames;
				HenshinFxDraw.DrawFireFrame(pos, pink, sc, rot, frame);
				if (i % 2 == 0)
					HenshinFxDraw.DrawFireFrame(pos + n * 8f, orange, sc * 0.7f, rot, (frame + 4) % HenshinFxDraw.FireFrames);
			}
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _to, pink,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 36f) * env);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _from + n * 24f, orange,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 28f) * env);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>花瓣舞环身：半径 15 格；FlowerPetal 壳弹环绕。</summary>
	public class PetalDanceFieldProj : HenshinMoveProj
	{
		private const int PetalCount = 8;
		private const float OrbitRadius = 220f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlowerPetal;

		public override void SetDefaults()
		{
			Projectile.width = 480;
			Projectile.height = 480;
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
			Projectile.rotation += 0.12f;
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.FlowerPetal);
			Dust.NewDustPerfect(p.Center + Main.rand.NextVector2CircularEdge(OrbitRadius * 0.9f, OrbitRadius * 0.9f), DustID.Firework_Pink, Vector2.Zero, 80, default, 1.1f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.FlowerPetal);
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.FlowerPetal);
			for (int i = 0; i < PetalCount; i++)
			{
				float ang = Projectile.rotation + MathHelper.TwoPi * i / PetalCount;
				Vector2 pos = Projectile.Center + ang.ToRotationVector2() * OrbitRadius;
				// 光照 C 组：花瓣是 lit，每片按自己位置取光（原固定粉色，黑处全亮）。
				Color c = HenshinFxDraw.LitHue(pos, new Color(255, 180, 220, 220)); // 光照第 2 轮 E 组：保色
				Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, c, ang + MathHelper.PiOver2, tex.Size() * 0.5f, 1.25f, SpriteEffects.None);
			}
			return false;
		}
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
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
		}

		private static readonly Color MudTint = new(170, 120, 70);
		private bool _castSfx;

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 8);
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void AI()
		{
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.RockCast, Projectile.Center, 0.8f, 0.2f);
			}
			Projectile.rotation += 0.2f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Mud);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 120);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), MudTint, null, 0.5f);
		}

		public override void OnKill(int timeLeft)
		{
			// 各端：泥块砸中的闷声 + 小号泥爆（不叠火烟帧）。
			if (timeLeft <= 0)
				return;
			HenshinSfx.PlayCooldown(SfxKey.RockHit, Projectile.Center, 2, 0.8f, 0.2f);
			HenshinExplosion.Spawn(Projectile.Center, BlastSize.S, MudTint, 44f, kenneyFrame: false, strength: 0.5f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 泥点短拖尾（棕，偏暗、半透明），之后照常画原版 DirtBall 贴图。光照 C 组：拖尾吃光。
			HenshinTrail.Strip(Projectile, MudTint, 11f, new Color(225, 190, 140), 0.35f, 0f, 0.55f, lit: true);
			return true;
		}
	}

	/// <summary>短十字劈（无前冲）：可见 X 形 LightShot。</summary>
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

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 14f;
			Color c = HenshinFxDraw.WithAlpha(new Color(255, 230, 210), 0.85f * life);
			Vector2 c0 = Projectile.Center;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, c0 + new Vector2(-28f, -28f), c0 + new Vector2(28f, 28f), c, 14f);
			HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, c0 + new Vector2(28f, -28f), c0 + new Vector2(-28f, 28f), c, 14f);
			int jaggedFrame = HenshinFxDraw.AgeFrame(14, Projectile.timeLeft, 4, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.DrawHitJaggedFrame(c0, HenshinFxDraw.WithAlpha(Color.White, 0.6f * life), 0.7f, 0f, jaggedFrame);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>三连刺导演：错开生成 GroundSpikeStab。</summary>
	public class TripleStabProj : HenshinMoveProj
	{
		private int _n;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// R3 手感：每根刺冒出时自己响 RockCast（GroundSpikeStabProj 首 tick），之力不再响 UseSound。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
				// 360° 朝鼠标：施放时锁定一次方向，三刺同向；角度经 ai1 随生成同步（联机安全）。
				if (_n == 0)
				{
					Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
					Projectile.localAI[1] = aim.ToRotation();
					if (Math.Abs(aim.X) > 0.05f)
						owner.ChangeDir(aim.X > 0f ? 1 : -1);
				}
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, Vector2.Zero,
					ModContent.ProjectileType<GroundSpikeStabProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, _n, Projectile.localAI[1]);
				_n++;
			}
			if (_n >= 3)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>土棕尖刺 jab：可达 8 格；ai0 = 第几道（0..2，沿垂直方向错开），ai1 = 朝向弧度（360°）。</summary>
	public class GroundSpikeStabProj : HenshinMoveProj
	{
		private const int Lifetime = 10;
		private const float ReachTiles = 8f;

		private Vector2 Dir => Projectile.ai[1].ToRotationVector2();

		private Vector2 BasePos(Player owner)
		{
			Vector2 d = Dir;
			return owner.MountedCenter + new Vector2(-d.Y, d.X) * ((Projectile.ai[0] - 1f) * 10f);
		}

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.ownerHitCheck = false;
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
				// R3 手感：岩刺破土声，三刺音高逐根升高（原 Item1 挥剑声）。
				HenshinSfx.Play(SfxKey.RockCast, owner.Center, 0.9f, -0.15f + 0.12f * Projectile.ai[0]);
			}
			float age = Lifetime - Projectile.timeLeft;
			float jab = MathHelper.Clamp(age / 4f, 0f, 1f);
			float reach = 28f + jab * (ReachTiles * 16f - 28f);
			Vector2 dir = Dir;
			Projectile.Center = BasePos(owner) + dir * reach;
			// 光照 C 组：土刺是 lit，刺尖暖土色灯删去。
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 origin = BasePos(owner);
			Vector2 tip = origin + Dir * (ReachTiles * 16f);
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, tip, 18f, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			Vector2 origin = BasePos(owner);
			Vector2 dir = Dir;
			float along = MathHelper.Clamp(Vector2.Dot(target.Center - origin, dir), 0f, ReachTiles * 16f);
			Vector2 hitPos = origin + dir * along;
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(220, 160, 90), hitPos, 0.5f);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), hitPos, Vector2.Zero,
				ModContent.ProjectileType<TripleStabHitFxProj>(), 0, 0f, Projectile.owner, Projectile.ai[1]);
		}

		// 贴图 Assets/Fx/TripleStab/TripleStab.png：5 帧 × 2 tick，132×22/帧，刺根在 (2,11)，全长 128 px = 8 格。
		private const int SheetFrames = 5;
		private const int SheetTicksPerFrame = 2;
		private static Asset<Texture2D> _sheet;

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/TripleStab/TripleStab", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / SheetFrames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / SheetTicksPerFrame, 0, SheetFrames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Player owner = Main.player[Projectile.owner];
			float rot = Projectile.ai[1];
			// 朝左半圈时上下翻转，让受光面（贴图上半）始终朝上。
			bool left = Math.Cos(rot) < 0;
			SpriteEffects fx = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 drawOrigin = new(2f, left ? frameH - 11f : 11f);
			// 光照 C 组：去掉 120/110/100 下限。刺长 8 格，按 4 段（每段 33 px）沿刺取光；取样点在实心块里时 LightAt 自动上移到空气格。
			// 末帧碎开时淡出。贴图已预乘，整色相乘即可。
			float opacity = frame == SheetFrames - 1 ? 0.45f + 0.55f * (Projectile.timeLeft / (float)SheetTicksPerFrame) : 1f;
			Vector2 baseW = BasePos(owner);
			Vector2 d = Dir;
			Vector2 tipW = baseW + d * (ReachTiles * 16f);
			const int Pieces = 4;
			int pw = (tex.Width + Pieces - 1) / Pieces;
			for (int i = 0; i < Pieces; i++)
			{
				int x0 = i * pw;
				int w = Math.Min(pw, tex.Width - x0);
				if (w <= 0)
					break;
				float t = MathHelper.Clamp((x0 + w * 0.5f - 2f) / (ReachTiles * 16f), 0f, 1f);
				Color c = HenshinFxDraw.LitSegment(baseW, tipW, t, Color.White);
				Rectangle piece = new(src.X + x0, src.Y, w, src.Height);
				Vector2 at = (baseW + d * (x0 - 2f) - Main.screenPosition).Floor();
				Main.EntitySpriteDraw(tex, at, piece, c * opacity, rot, new Vector2(0f, drawOrigin.Y), 1f, fx, 0f);
			}
			return false;
		}

		// R3 手感：收刺时的 2 颗土尘删掉（末帧碎开贴图 + 命中岩屑已够，少粒子）。
	}

	/// <summary>三连刺命中：岩屑迸散（纯视觉，0 伤害）。ai0 = 刺的朝向弧度。贴图 Assets/Fx/TripleStab/TripleStabHit(+_Light)。</summary>
	public class TripleStabHitFxProj : ModProjectile
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = Frames * TicksPerFrame;
		private static Asset<Texture2D> _sheet;
		private static Asset<Texture2D> _light;

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
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// R3 手感：重命中音（6 tick 冷却，三刺连中时回落 RockHit）+ 衰减光；3 颗碎屑尘删掉（迸散贴图已够）。
				if (!HenshinSfx.PlayCooldown(SfxKey.HitHeavy, Projectile.Center, 6, 0.85f, -0.1f))
					HenshinSfx.PlayCooldown(SfxKey.RockHit, Projectile.Center, 3);
				HenshinExplosion.SpawnLight(Projectile.Center, new Color(230, 170, 100), BlastSize.S, 0.5f); // 光照 C 组：lit 招式命中光 1→0.5
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/TripleStab/TripleStabHit", AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/TripleStab/TripleStabHit_Light", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			// 贴图朝右迸散；按刺的角度旋转，朝左半圈时上下翻转，让下落的碎屑仍朝下。
			float rot = Projectile.ai[0];
			SpriteEffects fx = Math.Cos(rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
			// 光照 C 组（mixed）：岩屑本体吃光，去掉下限（命中点常贴地，用 LightAt 取空气格）；下面的冲击光层保留。
			Color c = HenshinFxDraw.LightAt(Projectile.Center);
			Main.EntitySpriteDraw(tex, pos, src, c, rot, origin, 1f, fx, 0f);
			if (frame <= 1)
			{
				// 光层黑底不透明：真 Additive，顶点色保留 A=255。
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_light.Value, pos, src, Color.White, rot, origin, 1f, fx, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>劈瓦：空手道手刀自上而下（相对瞄准方向 -100°→+38°）下劈，半径 11.5 格；360° 朝鼠标；判定为随挥臂展开的实心扇形（含贴身）。</summary>
	public class BrickBreakProj : HenshinMoveProj
	{
		private const int Lifetime = 20;
		// 判定 = 实心扇形：玩家中心起，半径 ArcR，角度 ArcStartDeg → 当前帧弧尾。
		// 画面只画手刀 + 肩到掌的前臂（R7）+ 外缘细弧斩痕 + 落点冲击，不涂满扇形（R6）；贴图 BrickBreak.png 帧 400x400，斩痕外缘 89*2=178 px，手刀/斩痕前沿 = FrameEndDeg。
		private const float ArcR = 184f;
		private const float ArcStartDeg = -100f;
		private static readonly float[] FrameEndDeg = { -62f, -30f, 6f, 38f, 38f };
		private static int ChopFrame(int age) => age < 2 ? 0 : age < 4 ? 1 : age < 6 ? 2 : age < 11 ? 3 : 4;
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BoxingGlove;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.ownerHitCheck = false;
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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				HenshinSfx.Play(SfxKey.SwingHeavy, owner.Center, 0.9f, 0.1f);
			}
			Projectile.Center = owner.MountedCenter + _dir * 48f;
			// 劈瓦斩线由 BrickBreak 贴图承担，出招不沿线撒粒子。落点那一拍只在掌缘点一盏短光（不涂面）。
			int age = Lifetime - Projectile.timeLeft;
			// 光照 C 组：劈瓦 mixed，落点光 ≤0.2（原 0.7）。
			if (age >= 5 && age <= 9)
				Lighting.AddLight(owner.MountedCenter + _dir.RotatedBy(MathHelper.ToRadians(38f) * (_dir.X < 0f ? -1f : 1f)) * 160f, 0.2f, 0.16f, 0.1f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 from = owner.MountedCenter;
			float rot = _dir.ToRotation();
			float sign = _dir.X < 0f ? -1f : 1f;
			int age = Lifetime - Projectile.timeLeft;
			int frame = ChopFrame(age);
			if (frame == 4) // 淡出帧：不再判定
				return false;
			// 判定 = 实心扇形（含玩家贴身处）：目标矩形取 7x7 采样点 + 离玩家最近点，任一点落在扇形内即中。
			float lo = MathHelper.ToRadians(ArcStartDeg), hi = MathHelper.ToRadians(FrameEndDeg[frame] + 2f);
			bool In(Vector2 p)
			{
				Vector2 v = p - from;
				float d2 = v.LengthSquared();
				if (d2 > ArcR * ArcR) return false;
				if (d2 < 40f * 40f) return true;
				if (frame == 0) return false; // R5: frame 0 = wind-up, only the point-blank zone is live // 贴脸：近身一律算
				float a = MathHelper.WrapAngle((v.ToRotation() - rot) * sign);
				return a >= lo && a <= hi;
			}
			Vector2 near = Vector2.Clamp(from, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			if (In(near)) return true;
			for (int i = 0; i < 7; i++)
				for (int j = 0; j < 7; j++)
					if (In(new Vector2(targetHitbox.X + targetHitbox.Width * i / 6f, targetHitbox.Y + targetHitbox.Height * j / 6f)))
						return true;
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (target.defense >= 20)
				modifiers.FinalDamage *= 1.25f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 命中星首帧各端放重击声（4 tick 节流）；Heavy 档白闪 + 中震。
			H2HitFxProj.Spawn(Projectile, target.Center, 0.8f, _dir.ToRotation(), G1HitSfx.Code(SfxKey.HitHeavy, 4));
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(255, 200, 140), target.Center, 0.5f);
		}

		private static ReLogic.Content.Asset<Texture2D> _chopSheet;

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.localAI[0] == 0f)
				return false;
			Player owner = Main.player[Projectile.owner];
			float rot = _dir.ToRotation();
			// 劈瓦：自绘 BrickBreak 手刀下劈，F0-F3 前臂从玩家身后连到掌根（5 帧 400x400：举掌 / 劈至 -30° / +6° / +38° 落点冲击 / 余波；原点=帧中心=玩家，1:1）。
			Texture2D tex = (_chopSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/BrickBreak/BrickBreak", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 5;
			int age = Lifetime - Projectile.timeLeft;
			int frame = ChopFrame(age);
			float opacity = frame == 4 ? Math.Min(1f, Projectile.timeLeft / 6f) : 1f;
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			// 光照 C 组（mixed）：手臂 + 前臂吃光（取前臂中段，玩家前方 3 格）。
			// shortcut: BrickBreak.png 没有 _Light 遮罩，刃口 / 落点星分不出来，整图向白色拉 0.5 当 mixed；要刃口全亮需补一张 BrickBreak_Light.png 再叠 Additive。
			Color armLight = HenshinFxDraw.LightAt(owner.MountedCenter + _dir * 48f);
			Color body = Color.Lerp(armLight, Color.White, 0.15f);
			Main.EntitySpriteDraw(tex, (owner.MountedCenter - Main.screenPosition).Floor(), src, body * opacity,
				rot, new Vector2(tex.Width * 0.5f, fh * 0.5f), 1f, fx, 0f);
			return false;
		}
	}

	/// <summary>指针水/沙漩涡（潮旋 / 流沙）：沙直径 16 格；可见 Cyclone+Fog。</summary>
	public class MouseVortexProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

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

		private bool IsSand =>
			(int)Projectile.ai[0] == DustID.Sand || Projectile.ai[2] > 0.5f;

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (IsSand)
				{
					Vector2 c = Projectile.Center;
					Projectile.width = 256;
					Projectile.height = 256;
					Projectile.Center = c;
				}
			}
			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Water;
			Projectile.rotation += IsSand ? 0.22f : 0.18f;
			float edge = IsSand ? 110f : 28f;
			Vector2 offset = Main.rand.NextVector2CircularEdge(edge, edge);
			Dust.NewDustPerfect(Projectile.Center + offset, dust, offset.RotatedBy(1.5f) * 0.05f, 100, default, 1.35f).noGravity = true;
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.Typhoon);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 90);
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
			Projectile.velocity *= 0.1f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			bool sand = IsSand;
			const float sandDiamPx = 16f * 16f;

			Texture2D typhoon = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Typhoon);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			Rectangle frame = typhoon.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 4) % frames);

			if (sand)
			{
				// 保留 Typhoon 涡旋结构：压蓝（B≈0）+ 降低壳 Alpha，少贡献色相
				Color shell = new Color(200, 120, 20, 110);
				float shellScale = sandDiamPx / Math.Max(1, frame.Width);
				Main.EntitySpriteDraw(typhoon, Projectile.Center - Main.screenPosition, frame, shell,
					Projectile.rotation, frame.Size() * 0.5f, shellScale, SpriteEffects.None);

				// 高不透明琥珀叠层主导色相
				Color core = HenshinFxDraw.WithAlpha(new Color(255, 170, 40), 0.92f);
				Color outer = HenshinFxDraw.WithAlpha(new Color(200, 120, 20), 0.88f);
				Color fogC = HenshinFxDraw.WithAlpha(new Color(230, 150, 30), 0.75f);
				float cycScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.Cyclone, sandDiamPx);
				float fogScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.Fog, sandDiamPx * 0.95f);
				float glowScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, sandDiamPx * 0.55f);

				HenshinFxDraw.BeginAdditive();
				HenshinFxDraw.DrawCyclone(Projectile.Center, outer, cycScale * 1.05f, -Projectile.rotation);
				HenshinFxDraw.DrawCyclone(Projectile.Center, core, cycScale * 0.92f, Projectile.rotation * 0.85f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center, fogC, fogScale, Projectile.rotation * 0.5f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 190, 50), 0.7f), glowScale);
				HenshinFxDraw.EndAdditive();
				return false;
			}

			Color tint = HenshinFxDraw.WithAlpha(new Color(60, 140, 255), 0.85f);
			Color waterFog = HenshinFxDraw.WithAlpha(new Color(100, 180, 255), 0.4f);
			Color waterShell = new Color(70, 130, 230, 200);
			float waterShellScale = 1.25f;
			Main.EntitySpriteDraw(typhoon, Projectile.Center - Main.screenPosition, frame, waterShell,
				Projectile.rotation, frame.Size() * 0.5f, waterShellScale, SpriteEffects.None);

			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawCyclone(Projectile.Center, tint, 1.35f, -Projectile.rotation);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center, waterFog, 1.0f, Projectile.rotation * 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>爆裂拳：朝指针挥出巨大拳套（不冲刺）；终点岩石碎裂 + ai0 半径格数 AoE。</summary>
	public class DynamicPunchProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private const float Reach = 14f * 16f;
		private const float GloveScale = 3.5f;
		private Vector2 _dir;
		private Vector2 _origin;
		private bool _burstSpawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BoxingGlove;

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 8);

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.ownerHitCheck = false;
		}

		private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				_origin = owner.MountedCenter;
				// 起手只留共享 UltCast（根弹旗），旧 Item1 删去免叠；出拳风声晚 3 tick 再起。
			}
			if (Projectile.timeLeft == Lifetime - 3)
				HenshinSfx.Play(SfxKey.SwingHeavy, owner.Center, 1f, -0.3f);
			// 沿瞄准方向挥出，不推动玩家
			float u = MathHelper.SmoothStep(0f, 1f, Progress);
			Projectile.Center = _origin + _dir * (48f + Reach * u);
			Projectile.rotation = _dir.ToRotation(); // 只给残影取朝向
			// 拳头由 DynamicFist 贴图承担，出招不撒尘。光照 C 组：拳 mixed，暖光 0.7→≤0.2。
			// 光照第 2 轮 E 组（§4.15 大招例外）：暖光 0.2→0.4；巨拳（约 154×119px）外缘 80–110px 少量火把光点（自带光）。
			Lighting.AddLight(Projectile.Center, 0.4f, 0.27f, 0.1f);
			HenshinUltGlow.Emit(Projectile, 100f, DustID.Torch, Vector3.Zero);

			if (!_burstSpawned && Projectile.timeLeft <= 4)
				SpawnRockBurst(Projectile.Center);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 45);
			target.velocity *= 0.15f;
			// Ultimate 档：白闪 + 主人端大震（20 tick 窗口一次）+ 表现顿帧。先于石爆调用，顿帧才会触发。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(255, 170, 70), target.Center);
			if (!_burstSpawned)
				SpawnRockBurst(target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			if (!_burstSpawned)
				SpawnRockBurst(Projectile.Center);
		}

		private void SpawnRockBurst(Vector2 at)
		{
			if (_burstSpawned || Projectile.owner != Main.myPlayer)
				return;
			_burstSpawned = true;
			float radiusTiles = Projectile.ai[0] > 0.5f ? Projectile.ai[0] : 20f;
			// 旧 Item14（只主人听得到）+ 4 颗石尘删去：命中星首帧各端放大爆 + HitUlt，终点 L 档爆炸配方由 RockShatterBurstProj 画。
			H2HitFxProj.Spawn(this.Projectile, at, 2.2f, _dir.ToRotation(), G1HitSfx.Code(SfxKey.BlastL, 10, ult: true));
			HenshinHitFeel.Shake(Projectile.owner, at, HitTier.Ultimate, _dir);

			int aoe = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
				ModContent.ProjectileType<RockShatterBurstProj>(), Projectile.damage, Projectile.knockBack * 1.2f, Projectile.owner, radiusTiles);
			if (aoe >= 0)
			{
				Main.projectile[aoe].Center = at;
				if (Main.projectile[aoe].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}
			for (int i = 0; i < 5; i++)
			{
				Vector2 shardVel = Main.rand.NextVector2Circular(4f, 4f);
				shardVel.Y -= 3f;
				int sid = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, shardVel,
					ModContent.ProjectileType<FallingBoulderProj>(), Math.Max(1, Projectile.damage / 5), 2f, Projectile.owner);
				if (sid >= 0 && Main.projectile[sid].ModProjectile is IHenshinMoveProj st)
					st.EasyCrit = EasyCrit;
			}
		}

		private static ReLogic.Content.Asset<Texture2D> _fistSheet;

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.localAI[0] == 0f)
				return false;
			float rot = _dir.ToRotation();
			// 爆裂拳：自绘 DynamicFist 巨拳（2 帧：0 蓄、1 速度线），指节朝 +X；朝左上下翻转。
			Texture2D fist = (_fistSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DynamicFist/DynamicFist", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			int fh = fist.Height / 2;
			Rectangle src = new(0, Progress > 0.15f ? fh : 0, fist.Width, fh);
			SpriteEffects sfx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			// 巨拳残影（3 张，加色暖白、渐淡），表达冲拳速度；不是钉身近战，可用 oldPos。
			HenshinFxDraw.BeginAdditive();
			HenshinTrail.Afterimage(Projectile, fist, src, src.Size() * 0.5f, HenshinFxDraw.WithAlpha(new Color(255, 200, 140), 0.8f),
				GloveScale * 0.5f, sfx, count: 3, step: 2, alpha0: 0.45f);
			HenshinFxDraw.EndAdditive();
			// 光照 C 组：拳头本体吃光（原 Color.White）；上面的加色残影 = 发光部分，保留。
			Main.EntitySpriteDraw(fist, (Projectile.Center - Main.screenPosition).Floor(), src, lightColor,
				rot, src.Size() * 0.5f, GloveScale * 0.5f, sfx, 0f);

			HenshinFxDraw.BeginAdditive();
			if (_burstSpawned || Projectile.timeLeft <= 6)
			{
				int flash = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 2, HenshinFxDraw.FlashImpactFrames);
				HenshinFxDraw.DrawFlashImpactFrame(Projectile.Center, HenshinFxDraw.WithAlpha(new Color(255, 220, 160), 0.85f), 0.9f, rot, flash);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>爆裂拳终点石爆：ai0=半径格数；短命范围伤 + 碎石视觉。</summary>
	public class RockShatterBurstProj : HenshinMoveProj
	{
		private const int Life = 16;
		private float _radiusPx;
		private int _lifetime;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
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
				_lifetime = Life;
				float tiles = Projectile.ai[0] > 0.5f ? Projectile.ai[0] : 20f;
				_radiusPx = tiles * 16f;
				int size = (int)(_radiusPx * 2f);
				Vector2 c = Projectile.Center;
				Projectile.width = size;
				Projectile.height = size;
				Projectile.Center = c;
				// 小石爆（岩崩落地 / 尖石 / 落石，ai0≤2）各端首帧放岩石碎声；大石爆（爆裂拳）的声由命中星放。
				if (!BigBurst)
					HenshinSfx.PlayCooldown(SfxKey.RockHit, Projectile.Center, 4);
			}
			// 每 tick 8 颗石尘（一次爆 128 颗）删去，改爆炸配方：大爆 L（环扩到真实半径，当范围提示），小爆 S。
			// 光照 C 组：小石爆（岩崩落地 / 尖石 / 落石）是 lit 招式的爆炸，光 ×0.45；大石爆（爆裂拳终点，mixed 大招）不变。
			HenshinExplosion.Pulse(Projectile.Center, BigBurst ? BigTint : SmallTint, BigBurst ? BlastSize.L : BlastSize.S, BurstT, BurstDiameter,
				BigBurst ? 1f : 0.45f);
		}

		private static readonly Color BigTint = new(255, 160, 70);
		private static readonly Color SmallTint = new(210, 160, 100);
		private bool BigBurst => Projectile.ai[0] > 2f;
		private float BurstT => MathHelper.Clamp(1f - Projectile.timeLeft / (float)Math.Max(1, _lifetime), 0f, 0.999f);
		private float BurstDiameter => BigBurst ? _radiusPx * 2f : Math.Min(96f, _radiusPx * 2f + 40f);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 40);
			target.velocity *= 0.2f;
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, BigBurst ? HitTier.Heavy : HitTier.Light),
				BigBurst ? BigTint : SmallTint, target.Center, BigBurst ? 1f : 0.5f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float a = Projectile.timeLeft / (float)_lifetime;
			float diam = _radiusPx * 2f * (0.85f + (1f - a) * 0.25f);
			bool rockLand = Projectile.ai[0] > 0.5f && Projectile.ai[0] <= 2f;
			HenshinFxDraw.BeginAdditive();
			float circScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
			if (rockLand)
			{
				// 岩崩落地：仅石色扩散，去掉 FlashImpact 金光。光照 C 组：石尘圈颜色乘中心光（黑处不自发光）。
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(180, 130, 80).MultiplyRGBA(HenshinFxDraw.LightAt(Projectile.Center)), 0.45f * a), circScale);
			}
			else
			{
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 170, 70), 0.75f * a), circScale);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 200, 110), 0.55f * a), circScale * 0.55f);
				int flash = HenshinFxDraw.AgeFrame(_lifetime, Projectile.timeLeft, 2, HenshinFxDraw.FlashImpactFrames);
				HenshinFxDraw.DrawFlashImpactFrame(Projectile.Center, HenshinFxDraw.WithAlpha(new Color(255, 230, 180), 0.7f * a), 1.1f, 0f, flash);
			}
			if (_lifetime > 0)
				HenshinExplosion.Draw(Projectile.Center, BigBurst ? BlastSize.L : BlastSize.S, BigBurst ? BigTint : SmallTint, BurstT, BurstDiameter, 0f, kenneyFrame: BigBurst,
					strength: BigBurst ? 1f : 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>中功率穿透电（雷丘技能）；轻量 SoftGlow 抛光。</summary>
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
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 2;
			Projectile.scale = 1.3f;
		}

		public override void AI()
		{
			Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, Vector2.Zero, 80, HenshinFxDraw.SparkGold, 1.3f).noGravity = true;
			Lighting.AddLight(Projectile.Center, HenshinFxDraw.SparkGold.ToVector3());
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 90);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 dir = Projectile.velocity.LengthSquared() > 0.01f ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, Projectile.Center - dir * 18f, Projectile.Center + dir * 10f,
				HenshinFxDraw.WithAlpha(HenshinFxDraw.SparkGold, 0.75f), 10f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(HenshinFxDraw.SparkGoldCore, 0.55f), 0.3f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>污泥炸弹毒弹：自绘 4 帧污泥球（Assets/Fx/SludgeBomb），命中播 SludgeHit，毒云簇保留（伤害来源）。妙蛙花共用。</summary>
	public class SludgeBoltProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 105;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
		}

		// R3 手感（G2）：紫外晕 + 毒绿芯拖尾；施放音在 AI 首帧（各端），落点 PoisonHit + S 爆炸配方。
		private static readonly Color TrailGlow = new(170, 70, 230);
		private static readonly Color TrailCore = new(200, 255, 120);

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 12);
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.WaterCast, Projectile.Center, 1f, -0.3f);
			}
			Projectile.velocity.Y += 0.12f;
			// 光照 C 组：毒泥是 lit，自身紫光（0.55）删去。
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			// 光照 C 组：拖尾吃光，泥球本体乘环境光（原 Color.White）。
			HenshinTrail.Strip(Projectile, TrailGlow, 15f, TrailCore, 0.32f, 0f, 0.8f, lit: true);
			H3Fx.Draw(H3Fx.SludgeBomb, 4, frame, Projectile.Center, 0f, 1.2f, lightColor);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Poisoned, 240);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), TrailGlow, null, 0.5f);
		}

		public override void OnKill(int timeLeft)
		{
			if (Projectile.owner != Main.myPlayer)
				return;

			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.SludgeHit, 1f, Projectile.owner, 0f, SfxKey.PoisonHit, BlastSize.S, soft: true);
			// 毒气瓶式毒云簇（原版 AI）
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.ToxicCloud);
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.ToxicCloud2);
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.ToxicCloud3);
			int cloudDmg = Math.Max(1, Projectile.damage / 3);
			int[] cloudTypes = { ProjectileID.ToxicCloud, ProjectileID.ToxicCloud2, ProjectileID.ToxicCloud3 };
			for (int i = 0; i < 5; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(18f, 18f);
				int type = cloudTypes[i % cloudTypes.Length];
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + off,
					Main.rand.NextVector2Circular(1.5f, 1.5f), type, cloudDmg, 0.5f, Projectile.owner);
				if (id >= 0)
				{
					Projectile cloud = Main.projectile[id];
					ProjectileBorrow.RetargetAsHenshin(cloud);
					cloud.friendly = true;
					cloud.hostile = false;
					cloud.DamageType = HenshinDamage.Instance;
				}
			}
		}
	}

	/// <summary>空气爆炸 EasyCrit：三段脉冲 + 缩小 DiffusionCircle/Fog。</summary>
	/// <summary>
	/// 空气爆炸：自释放者朝鼠标方向逐渐铺开爆炸节点路径（≤128 格），
	/// 撞实心则紧贴再爆一发后停；每节点半径 4 格，并吸引 16 格内敌人。
	/// </summary>
	public class AirBurstProj : HenshinMoveProj
	{
		private const float MaxTiles = 128f;
		private const float StepTiles = 4f;
		private const float RadiusTiles = 4f;
		private const float PullTiles = 16f;
		private const int SpawnInterval = 3;
		private const int PulseLife = 14;

		private Vector2 _origin;
		private Vector2 _dir;
		private float _pathLen;
		private int _spawned;
		private int _maxNodes;
		private readonly Vector2[] _nodes = new Vector2[40];
		private readonly int[] _nodeAge = new int[40];
		private int _activeNode = -1;
		private int _activeTicks;
		private static readonly Color AeroTint = new(170, 215, 255);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// R3 手感：命中重档（白闪 + 光 + 震屏节流）+ 同步风闪（命中音在 H7HitFxProj 首 tick）。
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, AeroTint, target.Center, 0.5f);
			H7HitFxProj.Spawn(Projectile, target.Center, 0, 0.6f, H7HitFxProj.TierIndex(tier));
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_origin = p.MountedCenter;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - _origin;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				float maxPx = MaxTiles * 16f;
				float blocked = HenshinTileRay.FirstSolidDistance(_origin, _dir, maxPx);
				// 撞墙：路径含紧贴物块处的最后一爆
				_pathLen = Math.Min(maxPx, blocked);
				_maxNodes = Math.Max(1, (int)MathF.Ceiling(_pathLen / (StepTiles * 16f)));
				_maxNodes = Math.Min(_maxNodes, _nodes.Length);
				Projectile.Center = _origin;
				// R3 手感：起手改风系施放音（原 Item14 爆音），已声明 ClaimCastSound。
				HenshinSfx.Play(SfxKey.WindCast, _origin, 1f, -0.1f);
			}

			int age = 120 - Projectile.timeLeft;
			if (_spawned < _maxNodes && age % SpawnInterval == 0)
			{
				float u = (_spawned + 1f) / _maxNodes;
				Vector2 at = _origin + _dir * (_pathLen * u);
				_nodes[_spawned] = at;
				_nodeAge[_spawned] = 0;
				_activeNode = _spawned;
				_activeTicks = PulseLife;
				_spawned++;
				Projectile.Center = at;
				// R3 手感：每爆点 S 爆炸配方（核心白闪 + 环 + 衰减光），声音节流（约每 3 个爆点一声，音高沿路径升高）；
				// 最后一爆 M 爆 + 中爆声 + 重档震屏。旧的 2 颗云尘删掉。
				bool last = _spawned >= _maxNodes;
				// 光照 C 组：风是 mixed，爆点 strength 0.5，最后一爆 0.6。
				HenshinExplosion.Spawn(at, last ? BlastSize.M : BlastSize.S, AeroTint, RadiusTiles * 16f * (last ? 2.4f : 1.6f), kenneyFrame: false,
					strength: last ? 0.6f : 0.5f);
				if (last)
				{
					HenshinSfx.Play(SfxKey.BlastM, at, 0.85f, 0.2f);
					HenshinHitFeel.Shake(Projectile.owner, at, HitTier.Heavy, _dir);
				}
				else
					HenshinSfx.PlayCooldown(SfxKey.BlastS, at, 8, 0.75f, 0.05f + 0.25f * _spawned / _maxNodes);
				PullEnemies(at);
			}

			for (int i = 0; i < _spawned; i++)
				_nodeAge[i]++;
			if (_activeTicks > 0)
				_activeTicks--;

			if (_spawned >= _maxNodes && _activeTicks <= 0 && age > _maxNodes * SpawnInterval + PulseLife)
				Projectile.Kill();
		}

		private void PullEnemies(Vector2 center)
		{
			float pullR = PullTiles * 16f;
			float pullR2 = pullR * pullR;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage || !n.CanBeChasedBy())
					continue;
				float d2 = Vector2.DistanceSquared(n.Center, center);
				if (d2 > pullR2 || d2 < 4f)
					continue;
				Vector2 pull = center - n.Center;
				pull.Normalize();
				float falloff = 1f - MathF.Sqrt(d2) / pullR;
				n.velocity = Vector2.Lerp(n.velocity, pull * (10f * falloff), 0.45f);
			}
		}

		public override bool? CanDamage()
			=> _activeNode >= 0 && _activeTicks > PulseLife - 5 ? null : false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_activeNode < 0 || _activeTicks <= 0)
				return false;
			float r = RadiusTiles * 16f;
			Vector2 c = _nodes[_activeNode];
			Vector2 closest = Vector2.Clamp(c, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, c) <= r * r;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D ring = H7Tex.Get("WindRing"); // 自绘 3 臂风旋，112px/帧
			HenshinFxDraw.BeginAdditive();
			for (int i = 0; i < _spawned; i++)
			{
				float life = 1f - MathHelper.Clamp(_nodeAge[i] / (float)PulseLife, 0f, 1f);
				if (life <= 0.02f)
					continue;
				float diam = RadiusTiles * 16f * 2f * (0.8f + (1f - life) * 0.4f);
				// 光照 C 组（mixed）：风旋一半吃该点光、一半保留发光，强度 0.9→0.65。
				Color wind = Color.Lerp(HenshinFxDraw.LightAt(_nodes[i]), Color.White, 0.5f);
				HenshinFxDraw.DrawAdditiveSheet(ring, 1, 4, (_nodeAge[i] / 3) & 3, _nodes[i],
					HenshinFxDraw.WithAlpha(wind, 0.65f * life), diam / 112f, _nodeAge[i] * 0.35f + i);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
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
			Projectile.timeLeft = 75;
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

	/// <summary>挖洞大招强化出土爆：可见土环 + 小 Boulder 碎屑。</summary>
	public class DigUltBurstProj : HenshinMoveProj
	{
		private static Asset<Texture2D> _burstSheet;
		private Vector2 _ground;
		private bool _groundSet;

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
			if (!_groundSet)
			{
				_groundSet = true;
				_ground = p.Bottom; // 土喷泉锚在出土点，玩家弹起后不跟着走
			}
			// 光照第 2 轮 E 组（§4.15 大招例外）：土喷泉（112px 帧）外缘少量黄宝石光点 + 一盏土色灯（宝石光弱，forceLight）。
			// _ground 各端首帧由同步的玩家位置自算（不是主人专有字段）。碎石 28 tick 只比本弹多 8 tick、且同类被技能共用，不在碎石上发。
			HenshinUltGlow.Emit(_ground - new Vector2(0f, 40f), Projectile.owner, Projectile.whoAmI, 64f, DustID.GemTopaz,
				new Vector3(0.45f, 0.32f, 0.15f) * Math.Min(1f, Projectile.timeLeft / 6f), forceLight: true, dustScale: 1.2f);
			if (Projectile.timeLeft == 19)
			{
				// 破土（各端）：中爆 + 岩系命中两层替掉 Item14；土爆配方（M，土色、不叠火烟帧）锚在出土点；主人端竖直震屏。
				HenshinSfx.Play(SfxKey.BlastM, _ground);
				HenshinSfx.Play(SfxKey.RockHit, _ground, 1f, -0.3f);
				HenshinExplosion.Spawn(_ground - new Vector2(0f, 24f), BlastSize.M, new Color(200, 140, 80), 150f, kenneyFrame: false, strength: 0.6f);
				HenshinHitFeel.Shake(Projectile.owner, _ground, HitTier.Heavy, Vector2.UnitY);
				p.velocity.Y = -10f;
				for (int i = 0; i < 2; i++)
					Dust.NewDustPerfect(_ground + Main.rand.NextVector2Circular(24f, 4f), DustID.Dirt, new Vector2(Main.rand.NextFloat(-2f, 2f), -4f), 80, default, 1.3f);
				if (Projectile.owner == Main.myPlayer)
				{
					for (int i = 0; i < 4; i++)
					{
						Vector2 vel = Main.rand.NextVector2Circular(5f, 5f) + new Vector2(0f, -3f);
						int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.Center, vel,
							ModContent.ProjectileType<FallingBoulderProj>(), Math.Max(1, Projectile.damage / 10), 1f, Projectile.owner);
						if (id >= 0)
						{
							Main.projectile[id].scale = 0.25f;
							Main.projectile[id].timeLeft = 28;
							Main.projectile[id].penetrate = 1;
						}
					}
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 出自大招槽 → 大招档（震屏 7.5 + 白闪；本弹钉在玩家身上，不做顿帧），命中星带岩系命中音 + HitUlt。
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.Flash(target, tier);
			HenshinHitFeel.Shake(Projectile.owner, target.Center, tier, Vector2.UnitY);
			TackleHitFxProj.Spawn(Projectile, target.Center, DustID.Dirt, SfxKey.RockHit, tier);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_groundSet)
				return false;
			// DigBurst.png：5 帧 112x112，原点 = 底边中心上提 6px（落脚线）。光照 C 组：土是 lit，取出土点上方一格空气的光，只向白色拉 0.15（原 0.45）。
			Texture2D tex = (_burstSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DigBurst/DigBurst", AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 5;
			int age = 20 - Projectile.timeLeft;
			int frame = age < 3 ? 0 : age < 7 ? 1 : age < 12 ? 2 : age < 17 ? 3 : 4;
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			Color c = Color.Lerp(HenshinFxDraw.LightAt(_ground - new Vector2(0f, 24f)), Color.White, 0.15f) * (frame == 4 ? System.Math.Min(1f, Projectile.timeLeft / 3f) : 1f);
			Main.EntitySpriteDraw(tex, (_ground - Main.screenPosition).Floor(), src, c, 0f, new Vector2(tex.Width * 0.5f, fh - 6f), 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>
	/// 地震（三地鼠大招）：以主人为中心向两侧扩散，沿途地面依次顶起岩刺（H6QuakeRock，4 帧 × 5 tick）；
	/// 判定范围 / 倍率沿用旧版（每 tick 外扩 14px，28 tick），保留少量落石子弹，屏幕轻震。
	/// 状态全由弹幕年龄 + 世界地形推出，各端自算。
	/// </summary>
	public class QuakeWaveProj : HenshinMoveProj
	{
		// R3：刺贴图 48×40→80×68 px，中刺约 56px 高（重踏 40px）；射程 392→480 px；判定为逐刺 64×76 矩形（刺出现后 48 tick 内）
		private const int Life = 30;
		private const float Speed = 16f;
		private const float First = 36f;
		private const float Spacing = 40f;
		private const int TicksPerFrame = 5;
		private const int SpikeLife = 20;
		private const int HitTicks = 15;
		private const int MaxSpikes = 16;
		private float _reach;
		private readonly float[] _sx = new float[2 * MaxSpikes];
		private readonly float[] _sy = new float[2 * MaxSpikes];
		private readonly int[] _sn = new int[2];
		private static ReLogic.Content.Asset<Texture2D> _rock;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		/// <summary>逐刺落点：每 tick 沿两侧推进，爬台阶最多 3 格、下坑最多 10 格，撞墙截断。</summary>
		private void ComputeSpikes()
		{
			float ox = Projectile.localAI[1];
			int row0 = (int)(Projectile.localAI[2] / 16f);
			for (int si = 0; si < 2; si++)
			{
				float dir = si == 0 ? -1f : 1f;
				int row = row0;
				int n = 0;
				for (int i = 0; i < MaxSpikes; i++)
				{
					float dist = First + i * Spacing;
					if (dist > Speed * Life)
						break;
					float x = ox + dir * dist;
					int tx = (int)(x / 16f);
					int r = row;
					if (H6Fx.Solid(tx, r))
					{
						int climbed = 0;
						while (H6Fx.Solid(tx, r - 1) && climbed < 3)
						{
							r--;
							climbed++;
						}
						if (H6Fx.Solid(tx, r - 1))
							break;
					}
					else
					{
						int dropped = 0;
						while (!H6Fx.Solid(tx, r) && dropped < 10)
						{
							r++;
							dropped++;
						}
						if (!H6Fx.Solid(tx, r))
							break;
					}
					row = r;
					_sx[si * MaxSpikes + i] = x;
					_sy[si * MaxSpikes + i] = r * 16f;
					n++;
				}
				_sn[si] = n;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			int age = Life - Projectile.timeLeft;
			for (int si = 0; si < 2; si++)
			{
				for (int i = 0; i < _sn[si]; i++)
				{
					int t = age - ((int)MathF.Ceiling((First + i * Spacing) / Speed) - 1);
					if (t < 0 || t >= HitTicks)
						continue;
					int k = si * MaxSpikes + i;
					if (new Rectangle((int)_sx[k] - 32, (int)_sy[k] - 76, 64, 80).Intersects(targetHitbox))
						return true;
				}
			}
			return false;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			_reach += Speed;
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.localAI[1] = p.Center.X;
				Projectile.localAI[2] = p.Bottom.Y;
				// R3 手感：自建震屏迁到 HenshinHitFeel.Shake（大招档，保留竖直轴向；只震施放者）；
				// 起手 = 大爆声 + 低沉岩石声 + 脚下 L 爆光环（土色、不叠火烟）。施放音由共享 UltCast 负责。
				HenshinHitFeel.Shake(Projectile.owner, p.Center, HitTier.Ultimate, Vector2.UnitY);
				HenshinSfx.Play(SfxKey.BlastL, p.Center, 1f, -0.25f);
				HenshinSfx.Play(SfxKey.RockCast, p.Center, 0.9f, -0.45f);
				HenshinExplosion.Spawn(p.Bottom, BlastSize.L, QuakeTint, 260f, kenneyFrame: false, strength: 0.6f); // 光照 C 组：大招里的土爆 0.6
			}
			ComputeSpikes();
			// 岩刺依次顶出：低沉破土声（节流 5 tick）+ 每根刺出土 8 tick 内一盏土色灯；刺推到最远时第二次震屏（重档）。旧的地面石尘删掉。
			int qAge = Life - Projectile.timeLeft;
			for (int si = 0; si < 2; si++)
			{
				for (int i = 0; i < _sn[si]; i++)
				{
					int t = qAge - ((int)MathF.Ceiling((First + i * Spacing) / Speed) - 1);
					if (t < 0)
						break;
					if (t >= 8)
						continue;
					Vector2 at = new(_sx[si * MaxSpikes + i], _sy[si * MaxSpikes + i] - 24f);
					float k = 1f - t / 8f;
					Lighting.AddLight(at, 0.28f * k, 0.2f * k, 0.09f * k); // 光照 C 组：地震 mixed，每刺灯 0.55→≤0.2；光照第 2 轮 E 组大招例外 → 0.28
					if (t == 0)
						HenshinSfx.PlayCooldown(SfxKey.RockHit, at, 5, 0.8f, -0.35f);
					// 光照第 2 轮 E 组：刚顶出的刺（t==0）上方 ~50px 处少量黄宝石光点（刺高 ~56px，粒子落在刺尖之上，不盖刺）。
					// 刺位置由施放时的玩家位置（localAI，各端首帧自算）+ 地形推出，各端一致。两侧各用一个抽样键错峰，每侧约 0.4 颗/tick。
					if (t == 0)
						HenshinUltGlow.Emit(at - new Vector2(0f, 50f), Projectile.owner, Projectile.whoAmI + si, 10f, DustID.GemTopaz, Vector3.Zero, dustScale: 1.2f);
				}
			}
			if (qAge == 25)
				HenshinHitFeel.Shake(Projectile.owner, p.Center, HitTier.Heavy, Vector2.UnitY);

			if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 5 == 0)
			{
				float side = Main.rand.NextBool() ? 1f : -1f;
				Vector2 spawn = p.Center + new Vector2(side * _reach * Main.rand.NextFloat(0.4f, 1f), 8f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, new Vector2(side * 2f, -4f),
					ModContent.ProjectileType<FallingBoulderProj>(), Math.Max(1, Projectile.damage / 8), 1f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].scale = 0.28f;
					Main.projectile[id].timeLeft = 24;
					Main.projectile[id].penetrate = 1;
				}
			}
		}

		private static readonly Color QuakeTint = new(235, 175, 100);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, QuakeTint, target.Center, 0.5f);
			H6Fx.SpawnBurst(Projectile, target.Center, H6Fx.KindStone, 1f, H6Fx.FxOf(tier));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_rock ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6QuakeRock/H6QuakeRock", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _rock.Value;
			int fh = tex.Height / 4;
			int age = Life - Projectile.timeLeft;
			for (int side = -1; side <= 1; side += 2)
			{
				int si = side < 0 ? 0 : 1;
				for (int i = 0; i < _sn[si]; i++)
				{
					float dist = First + i * Spacing;
					int arrive = (int)MathF.Ceiling(dist / Speed) - 1;
					int t = age - arrive;
					if (t < 0)
						break;
					if (t >= SpikeLife)
						continue;
					float x = _sx[si * MaxSpikes + i];
					float gy = _sy[si * MaxSpikes + i];
					int frame = Math.Min(t / TicksPerFrame, 3);
					// 光照 C 组：每根刺单独取光（gy 是地面实心格顶，gy-8 在上面的空气格），下限随 H6Fx.Lit 默认 40。
					Color c = H6Fx.Lit(new Vector2(x, gy - 8f));
					if (t >= SpikeLife - 3)
						c *= (SpikeLife - t) / 3f;
					SpriteEffects fx = ((i + side) & 1) == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
					Main.EntitySpriteDraw(tex, (new Vector2(x, gy + 2f) - Main.screenPosition).Floor(), new Rectangle(0, fh * frame, tex.Width, fh),
						c, 0f, new Vector2(tex.Width * 0.5f, 62f), 1f, fx, 0f);
				}
			}
			return false;
		}
	}

	/// <summary>共鸣强弹（技能强念）。</summary>
	public class StrongPsychicBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.RainbowRodBullet;

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 105;
			Projectile.tileCollide = false;
			Projectile.penetrate = 2;
			Projectile.scale = 1.25f;
			InherentHoming = true;
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
