using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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
	/// ai2：2=破坏光线。ShootSpeed=0；AI 每帧重瞄。
	/// </summary>
	public class SustainedBeamProj : HenshinMoveProj
	{
		public const int ModeHyperBeam = 2;
		public const float MaxLength = 96f * 16f;
		public const int LifeHyper = 90;

		private Vector2 _from;
		private Vector2 _to;
		private int _life = LifeHyper;

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
				Main.instance.LoadProjectile(ProjectileID.DeathLaser);
				SoundEngine.PlaySound(SoundID.Item67 with { Pitch = -0.1f }, owner.Center);
			}

			_from = owner.MountedCenter;
			Vector2 aim = Main.MouseWorld - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * MaxLength;
			Projectile.Center = Vector2.Lerp(_from, _to, 0.35f);
			owner.direction = aim.X >= 0f ? 1 : -1;

			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.PurpleTorch;
			Lighting.AddLight(Projectile.Center, 0.75f, 0.3f, 1f);
			if (Projectile.timeLeft % 2 == 0)
			{
				Vector2 sample = Vector2.Lerp(_from, _to, Main.rand.NextFloat(0.05f, 0.95f));
				Dust.NewDustPerfect(sample, dust, aim * 1.5f, 80, default, 1.35f).noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, 32f, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float env = Projectile.timeLeft / (float)Math.Max(1, _life);
			if (env > 0.9f)
				env = (1f - env) / 0.1f;
			else if (env < 0.12f)
				env = env / 0.12f;
			else
				env = 1f;

			HenshinFxDraw.BeginAdditive();
			Color envelope = HenshinFxDraw.WithAlpha(new Color(160, 40, 220), 0.75f * env);
			Color core = HenshinFxDraw.WithAlpha(new Color(240, 200, 255), 0.95f * env);
			HenshinFxDraw.DrawContinuousBeam(_from, _to, core, envelope, 8f, 28f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _to, envelope,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 28f));
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
				_dir = Main.MouseWorld - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				SoundEngine.PlaySound(SoundID.Item20 with { Pitch = IsUlt ? 0.2f : 0.05f }, owner.Center);
			}

			Projectile.Center = owner.MountedCenter;
			Vector2 aim = Main.MouseWorld - owner.MountedCenter;
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
		public const int Life = 90;

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
			}

			_dist += Speed / (1 + Projectile.extraUpdates);
			Vector2 perp = new Vector2(-_dir.Y, _dir.X);
			float wobble = MathF.Sin(_dist * 0.085f + _phase) * 10f; // ~0.6 格垂直抖
			Projectile.Center = _origin + _dir * _dist + perp * wobble;
			Lighting.AddLight(Projectile.Center, 0.55f, 0.4f, 0.95f);

			if (Projectile.timeLeft % 2 == 0)
			{
				Color mix = MixDragon(Main.rand.NextFloat(0.15f, 0.85f));
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
					DustID.Torch, _dir * 0.6f + Main.rand.NextVector2Circular(0.8f, 0.8f), 80, mix, Main.rand.NextFloat(0.85f, 1.2f));
				d.noGravity = true;
				d.color = mix;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnBurst(target.Center);
		}

		private void SpawnBurst(Vector2 center)
		{
			if (_burst)
				return;
			_burst = true;

			for (int i = 0; i < 10; i++)
			{
				Color mix = MixDragon(Main.rand.NextFloat());
				Dust d = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(16f, 16f),
					DustID.Torch, Main.rand.NextVector2Circular(4.5f, 4.5f), 50, mix, 1.35f);
				d.noGravity = true;
				d.color = mix;
			}

			if (Projectile.owner != Main.myPlayer)
				return;

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
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.25f }, center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float env = MathHelper.Clamp(Projectile.timeLeft / 12f, 0.35f, 1f);
			bool ult = Projectile.ai[2] > 0.5f;
			float body = OrbDiameterTiles * 16f * (ult ? 1.08f : 1f); // ~1.5 格
			// 外层蓝紫光晕（须抬亮，否则 Additive 下 #2108ad 几乎不可见）
			float haloScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, body * 2.35f);
			float midScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, body * 1.65f);
			float coreScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, body * 0.95f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(DragonHaloLit, 0.55f * env), haloScale);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(DragonHaloLit, 0.7f * env), midScale);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(DragonCore, 0.95f * env), coreScale);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 水柱：朝鼠标持续喷射。枪口渐进伸长（~24 tick 满长）。
	/// ai2=0 水炮：不穿透，命中后渐缩消失 + 水蓝粒子；ai2=1 加农：穿透、更高频/更粗/更深色，每 3 击半径 5 格水爆。
	/// </summary>
	public class WaterJetProj : HenshinMoveProj
	{
		public const int ModePump = 0;
		public const int ModeCannon = 1;
		public const float PumpLength = 64f * 16f;
		public const float CannonLength = 80f * 16f;
		public const int Life = 72;
		public const int GrowTicks = 24;
		public const int PumpFadeTicks = 20;
		public const float CannonBurstRadius = 5f * 16f;

		private static readonly Color WaterCore = new(120, 200, 255);
		private static readonly Color WaterGlow = new(40, 110, 220);
		private static readonly Color CannonCore = new(30, 90, 200);
		private static readonly Color CannonGlow = new(15, 50, 140);

		private Vector2 _from;
		private Vector2 _to;
		private Vector2 _aimDir = Vector2.UnitX;
		private int _npcHits;
		private bool _stopped;
		private float _lockedLen = -1f;
		private int _fadeMax;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WaterStream;

		private int JetMode => (int)Projectile.ai[2];
		private bool IsCannon => JetMode == ModeCannon;

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
			if (_lockedLen > 0f)
				return _lockedLen;
			float grow = MathHelper.Clamp((Life - Projectile.timeLeft) / (float)GrowTicks, 0f, 1f);
			grow = grow * grow * (3f - 2f * grow);
			float maxLen = IsCannon ? CannonLength : PumpLength;
			return maxLen * grow;
		}

		private float WidthMul()
		{
			if (!_stopped || _fadeMax <= 0)
				return 1f;
			return MathHelper.Clamp(Projectile.timeLeft / (float)_fadeMax, 0f, 1f);
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
				Main.instance.LoadProjectile(ProjectileID.WaterStream);
				if (IsCannon)
					Projectile.localNPCHitCooldown = 3;
				else
					Projectile.localNPCHitCooldown = 6;
				SoundEngine.PlaySound(SoundID.Item21, owner.Center);
			}

			_from = owner.MountedCenter;
			if (!_stopped)
			{
				Vector2 aim = Main.MouseWorld - _from;
				if (aim.LengthSquared() < 1f)
					aim = new Vector2(owner.direction, 0f);
				_aimDir = Vector2.Normalize(aim);
			}

			float len = CurrentLength();
			_to = _from + _aimDir * len;
			Projectile.Center = len > 1f ? Vector2.Lerp(_from, _to, 0.3f) : _from;
			owner.direction = _aimDir.X >= 0f ? 1 : -1;
			Lighting.AddLight(Projectile.Center, IsCannon ? 0.15f : 0.25f, IsCannon ? 0.3f : 0.5f, IsCannon ? 0.85f : 0.95f);

			if (!_stopped && len > 8f && Projectile.timeLeft % 2 == 0)
			{
				Vector2 sample = Vector2.Lerp(_from, _to, Main.rand.NextFloat(0.08f, 0.9f));
				Dust d = Dust.NewDustPerfect(sample, DustID.Water, _aimDir * 2f, 80, IsCannon ? CannonGlow : WaterCore, IsCannon ? 1.5f : 1.3f);
				d.noGravity = true;
				d.color = IsCannon ? CannonCore : WaterCore;
			}
		}

		public override bool? CanDamage() => _stopped && !IsCannon ? false : null;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_stopped || (_to - _from).LengthSquared() < 4f)
				return false;
			float _ = 0f;
			// 水炮 ~1.25 格；加农 ~2.25 格
			float width = (IsCannon ? 36f : 20f) * WidthMul();
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, width, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Color splash = IsCannon ? CannonCore : WaterCore;
			int count = IsCannon ? 14 : 10;
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(18f, 18f),
					DustID.Water, Main.rand.NextVector2Circular(IsCannon ? 5.5f : 3.5f, IsCannon ? 5.5f : 3.5f), 40, splash, IsCannon ? 1.6f : 1.35f);
				d.noGravity = true;
				d.color = splash;
			}

			if (!IsCannon)
			{
				// 不立即 Kill：锁长 + 渐缩淡出
				_stopped = true;
				_lockedLen = Math.Max(32f, Vector2.Distance(_from, target.Center));
				_aimDir = Vector2.Normalize(_to - _from);
				if (_aimDir.LengthSquared() < 0.01f)
					_aimDir = new Vector2(Main.player[Projectile.owner].direction, 0f);
				_to = _from + _aimDir * _lockedLen;
				_fadeMax = PumpFadeTicks;
				if (Projectile.timeLeft > PumpFadeTicks)
					Projectile.timeLeft = PumpFadeTicks;
				return;
			}

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
				for (int i = 0; i < 18; i++)
				{
					Dust d = Dust.NewDustPerfect(target.Center, DustID.Water,
						Main.rand.NextVector2CircularEdge(CannonBurstRadius * 0.15f, CannonBurstRadius * 0.15f) * Main.rand.NextFloat(0.6f, 1.4f),
						50, CannonGlow, 1.7f);
					d.noGravity = true;
					d.color = CannonCore;
				}
				SoundEngine.PlaySound(SoundID.Item85 with { Volume = 0.7f }, target.Center);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if ((_to - _from).LengthSquared() < 4f)
				return false;

			float env = Projectile.timeLeft / (float)Life;
			if (_stopped && _fadeMax > 0)
				env = WidthMul();
			else if (env > 0.88f)
				env = (1f - env) / 0.12f;
			else if (env < 0.1f)
				env = env / 0.1f;
			else
				env = 1f;

			float wMul = WidthMul();
			Color core = HenshinFxDraw.WithAlpha(IsCannon ? CannonCore : WaterCore, (IsCannon ? 0.95f : 0.9f) * env);
			Color glow = HenshinFxDraw.WithAlpha(IsCannon ? CannonGlow : WaterGlow, (IsCannon ? 0.72f : 0.62f) * env);
			// 水炮 1~1.5 格；加农 2~2.5 格（16px/格）
			float coreW = (IsCannon ? 18f : 10f) * wMul;
			float glowW = (IsCannon ? 40f : 22f) * wMul;
			float flow01 = (Main.GlobalTimeWrappedHourly * (IsCannon ? 2.4f : 1.8f) + Projectile.identity * 0.17f) % 1f;

			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawContinuousBeam(_from, _to, core, glow, coreW, glowW);
			HenshinFxDraw.DrawWaterFlowRipples(_from, _to,
				HenshinFxDraw.WithAlpha(IsCannon ? CannonCore : WaterCore, 0.55f * env),
				(IsCannon ? 32f : 18f) * wMul, flow01);
			float tipScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, glowW * 1.1f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _to, glow, tipScale);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, _from, core, tipScale * 0.75f);
			HenshinFxDraw.EndAdditive();
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
				else
					_lifetime = DefaultLife;
			}

			int dust = Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Torch;
			float radius = IsOverheat ? 220f : Math.Max(24f, Projectile.width * 0.45f);
			if (dust == DustID.DungeonWater)
			{
				Color edge = new(33, 8, 173);
				Color core = new(231, 206, 57);
				for (int i = 0; i < 6; i++)
				{
					Color mix = Color.Lerp(edge, core, Main.rand.NextFloat());
					Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
						DustID.Torch, Main.rand.NextVector2Circular(3.5f, 3.5f), 60, mix, 1.35f);
					d.noGravity = true;
					d.color = mix;
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

			int age = _lifetime - Projectile.timeLeft;
			if (IsOverheat && age % 6 == 0 && age / 6 < 5)
			{
				SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f }, Projectile.Center);
			}
			else if (!IsOverheat && Projectile.timeLeft == _lifetime - 1)
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
		}

		public override bool? CanDamage()
		{
			if (!IsOverheat)
				return null;
			int age = _lifetime - Projectile.timeLeft;
			// 5 段脉冲窗口：每 6 tick 开 2 tick
			return age % 6 <= 1 && age / 6 < 5;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
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
				float ringScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				Color ring = HenshinFxDraw.WithAlpha(new Color(40, 120, 220), 0.8f * life);
				Color foam = HenshinFxDraw.WithAlpha(new Color(140, 210, 255), 0.75f * life);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, ring, ringScale * (1f + (1f - life) * 0.3f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, foam,
					HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam * 0.5f));
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, foam, ringScale * 0.55f);
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
				Vector2 dir = Main.MouseWorld - owner.Center;
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
				Main.instance.LoadProjectile(ProjectileID.LastPrism);
				Main.instance.LoadProjectile(ProjectileID.LastPrismLaser);
				SoundEngine.PlaySound(SoundID.Item67 with { Pitch = 0.2f }, owner.MountedCenter);
			}
			_from = owner.MountedCenter;
			Vector2 aim = Main.MouseWorld - _from;
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
			for (int i = 0; i < 18; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(5f, 5f), 80, new Color(160, 110, 70), 1.3f);
			for (int i = 0; i < 6; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// SoftGlow 落地前微闪（OnKill 时已死，这里用寿命末段）
			if (Projectile.timeLeft <= 6 || Projectile.velocity.Y > 10f)
			{
				HenshinFxDraw.BeginAdditive();
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 200, 120), 0.45f), 0.35f);
				HenshinFxDraw.EndAdditive();
			}
			return true;
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

	/// <summary>火焰锥连发：朝鼠标连续 Flames（85）喷射柱，数量来自 ai1。</summary>
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
					flame.timeLeft = Math.Min(flame.timeLeft, 36);
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
			Vector2 dir = Main.MouseWorld - owner.Center;
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

	/// <summary>身周持续风场。可见 Typhoon + Cyclone 旋转。</summary>
	public class HurricaneFieldProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

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
			Projectile.rotation += 0.18f;
			Main.instance.LoadProjectile(ProjectileID.Typhoon);
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

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.Typhoon);
			Texture2D typhoon = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Typhoon);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			Rectangle frame = typhoon.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 4) % frames);
			Color tint = new(90, 140, 230, 200);
			Main.EntitySpriteDraw(typhoon, Projectile.Center - Main.screenPosition, frame, tint,
				Projectile.rotation, frame.Size() * 0.5f, 1.35f, SpriteEffects.None);

			HenshinFxDraw.BeginAdditive();
			Color cyc = HenshinFxDraw.WithAlpha(new Color(100, 160, 255), 0.75f);
			HenshinFxDraw.DrawCyclone(Projectile.Center, cyc, 1.4f, -Projectile.rotation * 1.2f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center, HenshinFxDraw.WithAlpha(new Color(140, 180, 255), 0.4f), 1.1f);
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
				Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), y), DustID.Electric, new Vector2(0f, 4f), 60, default, 1.4f).noGravity = true;
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

	/// <summary>逆鳞：连续爪击导演；子 slash 带 jagged。</summary>
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
					ModContent.ProjectileType<GenericSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, DustID.Torch, owner.direction);
				if (id >= 0)
					Main.projectile[id].ai[2] = 1f;
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
				Main.instance.LoadProjectile(ProjectileID.ShadowBeamFriendly);
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

	/// <summary>咬住/咬碎：身前两段咬合；尖牙用 destination Rectangle 画三角（MagicPixel 须封顶宽高，忌无源矩形通天缩放）。ai0=体型倍率。</summary>
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
			if (Projectile.ai[2] > 0)
				target.AddBuff(BuffID.OnFire, (int)Projectile.ai[2]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			float t = 1f - Projectile.timeLeft / (float)Lifetime;
			float close = t < 0.45f ? t / 0.45f : 1f - (t - 0.45f) * 0.35f;
			close = MathHelper.Clamp(close, 0f, 1f);
			float openGap = MathHelper.Lerp(18f, 2f, close) * _size;
			bool fireFang = Projectile.ai[2] > 0;
			Color fang = fireFang ? new Color(40, 18, 12, 240) : new Color(18, 18, 22, 240);
			Color edge = fireFang ? new Color(255, 120, 40, 220) : new Color(55, 55, 62, 220);

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

	/// <summary>龙息：128 格持续火线，Fire 帧动画 + SoftGlow 尖端；高频线伤。禁用飞散 Flames。</summary>
	public class DragonBreathConeProj : HenshinMoveProj
	{
		public const float BreathLength = 128f * 16f;
		public const int Life = 52;
		public const float LineWidth = 48f;

		private Vector2 _from;
		private Vector2 _to;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
				SoundEngine.PlaySound(SoundID.Item34 with { Pitch = -0.15f }, owner.Center);
			}

			_from = owner.MountedCenter;
			Vector2 aim = Main.MouseWorld - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * BreathLength;
			Projectile.Center = Vector2.Lerp(_from, _to, 0.2f);
			owner.direction = aim.X >= 0f ? 1 : -1;
			Lighting.AddLight(Projectile.Center, 0.85f, 0.3f, 0.75f);

			// 仅枪口短尘，不生成飞散 Flames
			if (Main.rand.NextBool())
			{
				Dust.NewDustPerfect(_from + aim * 20f, DustID.PurpleTorch, aim * 3f + Main.rand.NextVector2Circular(1f, 1f), 100, new Color(220, 120, 255), 1.25f).noGravity = true;
				Dust.NewDustPerfect(_from + aim * 28f, DustID.Torch, aim * 2.5f, 100, default, 1.1f).noGravity = true;
			}
			if (Projectile.timeLeft % 3 == 0)
			{
				Vector2 mid = Vector2.Lerp(_from, _to, Main.rand.NextFloat(0.1f, 0.6f));
				Dust.NewDustPerfect(mid, DustID.PurpleTorch, aim * 1.2f, 120, new Color(200, 100, 255), 1.0f).noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, LineWidth, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.rand.NextBool(3))
				target.AddBuff(BuffID.Confused, 30);
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
			Main.instance.LoadProjectile(ProjectileID.FlowerPetal);
			Dust.NewDustPerfect(p.Center + Main.rand.NextVector2CircularEdge(OrbitRadius * 0.9f, OrbitRadius * 0.9f), DustID.Firework_Pink, Vector2.Zero, 80, default, 1.1f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.FlowerPetal);
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.FlowerPetal);
			for (int i = 0; i < PetalCount; i++)
			{
				float ang = Projectile.rotation + MathHelper.TwoPi * i / PetalCount;
				Vector2 pos = Projectile.Center + ang.ToRotationVector2() * OrbitRadius;
				Color c = new(255, 180, 220, 220);
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
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, Vector2.Zero,
					ModContent.ProjectileType<GroundSpikeStabProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner, _n);
				_n++;
			}
			if (_n >= 3)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>土棕尖刺 jab：可达 8 格。</summary>
	public class GroundSpikeStabProj : HenshinMoveProj
	{
		private const int Lifetime = 10;
		private const float ReachTiles = 8f;
		private int _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Stinger;

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
				_dir = owner.direction;
				Main.instance.LoadProjectile(ProjectileID.Stinger);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.2f }, owner.Center);
			}
			float age = Lifetime - Projectile.timeLeft;
			float jab = MathHelper.Clamp(age / 4f, 0f, 1f);
			float offsetY = (Projectile.ai[0] - 1f) * 10f;
			float reach = 28f + jab * (ReachTiles * 16f - 28f);
			Projectile.Center = owner.MountedCenter + new Vector2(_dir * reach, offsetY);
			Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, new Vector2(_dir * 2f, 0f), 100, new Color(160, 110, 60), 1.1f).noGravity = true;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 origin = owner.MountedCenter + new Vector2(0f, (Projectile.ai[0] - 1f) * 10f);
			Vector2 tip = origin + new Vector2(_dir * ReachTiles * 16f, 0f);
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, tip, 18f, ref _);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Stinger);
			float rot = _dir > 0 ? 0f : MathHelper.Pi;
			Color tint = new(180, 120, 70, 230);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, tint, rot, tex.Size() * 0.5f, 1.15f, SpriteEffects.None);
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			int tipW = 8;
			int tipH = 18;
			var rect = new Rectangle(
				(int)(Projectile.Center.X - Main.screenPosition.X - tipW * 0.5f + _dir * 10f),
				(int)(Projectile.Center.Y - Main.screenPosition.Y - tipH * 0.5f),
				tipW, tipH);
			Main.spriteBatch.Draw(pixel, rect, src, new Color(120, 80, 40, 200));
			return false;
		}
	}

	/// <summary>劈瓦：约 20 格线斩；HitJagged/FlashImpact 按帧；BoxingGlove 可选。</summary>
	public class BrickBreakProj : HenshinMoveProj
	{
		private const int Lifetime = 20;
		private const float Reach = 20f * 16f;
		private const float LineWidth = 40f;
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BoxingGlove;

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
				Main.instance.LoadProjectile(ProjectileID.BoxingGlove);
				_dir = Main.MouseWorld - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				SoundEngine.PlaySound(SoundID.Item1, owner.Center);
			}
			Projectile.Center = owner.MountedCenter + _dir * 48f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, _dir * 2f, 80, default, 1.2f).noGravity = true;
			Dust.NewDustPerfect(owner.MountedCenter + _dir * (Reach * 0.5f), DustID.Stone, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.0f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 from = owner.MountedCenter;
			Vector2 to = from + _dir * Reach;
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), from, to, LineWidth, ref _);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (target.defense >= 20)
				modifiers.FinalDamage *= 1.25f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			float rot = _dir.ToRotation();
			Texture2D glove = ProjectileBorrow.RequestProjectileTexture(ProjectileID.BoxingGlove);
			// BoxingGlove 贴图朝向需顺时针 90° 才对准斩线
			Main.EntitySpriteDraw(glove, Projectile.Center - Main.screenPosition, null, Color.White,
				rot + MathHelper.PiOver2, glove.Size() * 0.5f, 1.15f, SpriteEffects.None);

			float life = Projectile.timeLeft / (float)Lifetime;
			int jagged = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			int flash = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 2, HenshinFxDraw.FlashImpactFrames);
			float jaggedRot = rot + MathHelper.Pi; // HitJagged tip 沿 +X；+180° 朝目标劈砍
			HenshinFxDraw.BeginAdditive();
			// 沿 20 格线多段绘制，特效与伤害范围对齐
			for (int i = 1; i <= 5; i++)
			{
				float u = i / 5.5f;
				Vector2 pos = owner.MountedCenter + _dir * (Reach * u);
				float sc = 0.85f + u * 0.45f;
				HenshinFxDraw.DrawHitJaggedFrame(pos, HenshinFxDraw.WithAlpha(new Color(255, 240, 220), 0.75f * life), sc, jaggedRot, (jagged + i) % HenshinFxDraw.HitJaggedFrames);
			}
			Vector2 mid = owner.MountedCenter + _dir * (Reach * 0.45f);
			HenshinFxDraw.DrawFlashImpactFrame(mid, HenshinFxDraw.WithAlpha(new Color(255, 220, 160), 0.6f * life), 0.55f, rot, flash);
			HenshinFxDraw.EndAdditive();
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
			Main.instance.LoadProjectile(ProjectileID.Typhoon);
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
			Color tint = sand
				? HenshinFxDraw.WithAlpha(new Color(220, 190, 90), 0.85f)
				: HenshinFxDraw.WithAlpha(new Color(60, 140, 255), 0.85f);
			Color fogC = sand
				? HenshinFxDraw.WithAlpha(new Color(210, 175, 85), 0.5f)
				: HenshinFxDraw.WithAlpha(new Color(100, 180, 255), 0.4f);

			Texture2D typhoon = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Typhoon);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			Rectangle frame = typhoon.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 4) % frames);
			Color shell = sand ? new Color(220, 190, 90, 200) : new Color(70, 130, 230, 200);
			float shellScale = sand ? 2.4f : 1.25f;
			Main.EntitySpriteDraw(typhoon, Projectile.Center - Main.screenPosition, frame, shell,
				Projectile.rotation, frame.Size() * 0.5f, shellScale, SpriteEffects.None);

			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawCyclone(Projectile.Center, tint, sand ? 2.6f : 1.35f, -Projectile.rotation);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center, fogC, sand ? 2.0f : 1.0f, Projectile.rotation * 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>爆裂拳：SoftGlow 拳光；命中 Flashimpact + DiffusionCircle；Confused 保留。</summary>
	public class DynamicPunchProj : HenshinMoveProj
	{
		private Vector2 _dir;
		private Vector2 _hitPos;
		private int _hitFlash;

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
			if (_hitFlash > 0)
				_hitFlash--;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 45);
			target.velocity *= 0.15f;
			SoundEngine.PlaySound(SoundID.Item14, target.Center);
			for (int i = 0; i < 16; i++)
				Dust.NewDustPerfect(target.Center, DustID.Smoke, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.4f).noGravity = true;
			_hitPos = target.Center;
			_hitFlash = 10;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 180, 100), 0.75f), 0.55f);
			if (_hitFlash > 0)
			{
				float a = _hitFlash / 10f;
				int flashFrame = HenshinFxDraw.AgeFrame(10, _hitFlash, 2, HenshinFxDraw.FlashImpactFrames);
				HenshinFxDraw.DrawFlashImpactFrame(_hitPos, HenshinFxDraw.WithAlpha(new Color(255, 220, 160), 0.9f * a), 0.55f, 0f, flashFrame);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, _hitPos,
					HenshinFxDraw.WithAlpha(new Color(255, 160, 60), 0.7f * a), 1.2f + (1f - a) * 0.4f);
			}
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
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 2;
			Projectile.scale = 1.3f;
		}

		public override void AI()
		{
			Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Vector2.Zero, 80, default, 1.3f).noGravity = true;
			Lighting.AddLight(Projectile.Center, 0.4f, 0.55f, 1f);
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
				HenshinFxDraw.WithAlpha(new Color(160, 200, 255), 0.75f), 10f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(180, 220, 255), 0.55f), 0.3f);
			HenshinFxDraw.EndAdditive();
			return true;
		}
	}

	/// <summary>强化暗影球：加强 SoftGlow 与尘。</summary>
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
			if (Main.rand.NextBool())
				Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(1.5f, 1.5f), 80, new Color(160, 60, 220), 1.4f).noGravity = true;
			Lighting.AddLight(Projectile.Center, 0.55f, 0.15f, 0.7f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(140, 40, 200), 0.7f), 0.55f);
			HenshinFxDraw.EndAdditive();
			return true;
		}

		public override Color? GetAlpha(Color lightColor) => new Color(200, 120, 255, 220);
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

	/// <summary>强化催眠波：SoftGlow 环可见。</summary>
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

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 36f;
			float pulse = 0.8f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f);
			HenshinFxDraw.BeginAdditive();
			Color c = HenshinFxDraw.WithAlpha(new Color(180, 80, 255), 0.55f * life);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, c, 1.1f * pulse);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(140, 60, 220), 0.35f * life), 1.3f * pulse);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>舌舔扇形 Stun：短弧 SoftGlow。</summary>
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

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = owner.direction;
			float life = Projectile.timeLeft / 14f;
			Color c = HenshinFxDraw.WithAlpha(new Color(220, 120, 255), 0.7f * life);
			Vector2 from = owner.MountedCenter;
			Vector2 tip = Projectile.Center + new Vector2(dir * 20f, 0f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawBeamSegment(HenshinFxDraw.LightShot, from, tip, c, 16f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, c, 0.4f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>空气爆炸 EasyCrit：DiffusionCircle + Cloud 可见爆。</summary>
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

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 12f;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(200, 230, 255), 0.75f * life), 1.0f + (1f - life) * 0.5f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(180, 210, 255), 0.5f * life), 0.9f);
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

	/// <summary>挖洞大招强化出土爆：可见土环 + 小 Boulder 碎屑。</summary>
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
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 30f), DustID.Dirt, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.4f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 20f;
			HenshinFxDraw.BeginAdditive();
			Color sand = HenshinFxDraw.WithAlpha(new Color(180, 140, 80), 0.7f * life);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center, sand, 1.4f + (1f - life) * 0.5f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, Projectile.Center, sand, 1.1f, Main.GlobalTimeWrappedHourly);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>地裂波：扩展 reach 上小 Boulder 弹跳 + 石裂纹尘。</summary>
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
			for (int i = 0; i < 5; i++)
			{
				float x = Main.rand.NextFloat(-_reach, _reach);
				Dust.NewDustPerfect(p.Center + new Vector2(x, 16f), DustID.Stone, new Vector2(0f, -2.5f), 80, default, 1.3f);
			}

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

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			Main.instance.LoadProjectile(ProjectileID.Boulder);
			Texture2D boulder = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Boulder);
			for (int i = -3; i <= 3; i++)
			{
				if (i == 0) continue;
				float x = i / 3f * _reach;
				Vector2 pos = p.Center + new Vector2(x, 12f + MathF.Abs(i) * 2f);
				float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + i) * 4f;
				Main.EntitySpriteDraw(boulder, pos + new Vector2(0f, bob) - Main.screenPosition, null,
					new Color(160, 130, 100, 200), 0.2f * i, boulder.Size() * 0.5f, 0.22f, SpriteEffects.None);
			}
			return false;
		}
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
