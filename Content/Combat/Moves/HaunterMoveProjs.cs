using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Buffs;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 暗影球：尖端主球有伤；身后 4 节 lagged 不透明紫盘纯 VFX（等效「仅首节有伤」）。
	/// Fill #220033 / Border #330066，AlphaBlend，禁止 Additive SoftGlow。
	/// </summary>
	public class BigShadowBallProj : HenshinMoveProj
	{
		private const float BaseDiameter = 28f;
		private const int HistoryLen = 48;
		// 紧挨拖尾（等间距小滞后）；半径递减 → 共 5 节紫圆
		private static readonly int[] TrailLags = { 0, 1, 2, 3, 4 };
		private static readonly float[] TrailRadiusMul = { 1f, 0.82f, 0.64f, 0.48f, 0.36f };
		private static readonly Color Fill = new(0x22, 0x00, 0x33, 255);
		private static readonly Color Border = new(0x33, 0x00, 0x66, 255);

		private readonly Vector2[] _hist = new Vector2[HistoryLen];
		private int _histWrite;
		private int _histFilled;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamHostile;

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 135;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.ShadowBeamHostile);
			Homing = false;
		}

		public override void AI()
		{
			Homing = false;
			PushHistory(Projectile.Center);
			Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Vector2.Zero, 100, default, 1.15f).noGravity = true;
			if (Main.rand.NextBool(3))
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(1.2f, 1.2f),
					80, new Color(160, 60, 220), 1.2f).noGravity = true;
			}
			float pulse = 0.35f + 0.2f * (0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f));
			Lighting.AddLight(Projectile.Center, pulse, pulse * 0.2f, pulse * 1.2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
			SpawnPurpleBurst(target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			SpawnPurpleBurst(Projectile.Center);
		}

		private static void SpawnPurpleBurst(Vector2 at)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Pitch = -0.35f, Volume = 0.7f }, at);
			for (int i = 0; i < 22; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(4.5f, 4.5f);
				Dust.NewDustPerfect(at, DustID.Shadowflame, v, 60, new Color(180, 80, 255), 1.45f).noGravity = true;
			}
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(at, DustID.PurpleTorch, Main.rand.NextVector2Circular(3.2f, 3.2f), 80, default, 1.25f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float flicker = 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 16f + Projectile.identity);
			for (int i = TrailLags.Length - 1; i >= 0; i--)
			{
				Vector2 pos = SampleLag(TrailLags[i]);
				if (i > 0)
					pos += Main.rand.NextVector2Circular(1.6f, 1.6f); // 拖尾微抖
				float diam = BaseDiameter * TrailRadiusMul[i] * (i == 0 ? flicker : 1f);
				Color fill = Fill;
				Color border = Border;
				if (i == 0)
				{
					fill = Color.Lerp(Fill, new Color(0x55, 0x22, 0x77, 255), (flicker - 0.82f) / 0.36f);
					border = Color.Lerp(Border, new Color(0x66, 0x33, 0x99, 255), (flicker - 0.82f) / 0.36f);
				}
				HenshinFxDraw.DrawOpaqueDisk(pos, diam, fill, border);
			}
			return false;
		}

		private void PushHistory(Vector2 p)
		{
			_hist[_histWrite] = p;
			_histWrite = (_histWrite + 1) % HistoryLen;
			if (_histFilled < HistoryLen)
				_histFilled++;
		}

		private Vector2 SampleLag(int lagTicks)
		{
			if (_histFilled <= 0)
				return Projectile.Center;
			int lag = Math.Min(lagTicks, _histFilled - 1);
			int idx = (_histWrite - 1 - lag + HistoryLen * 2) % HistoryLen;
			return _hist[idx];
		}
	}

	/// <summary>
	/// 舌舔：自 MountedCenter 向鼠标（无自动索敌）伸出最长 20 格；线碰撞，命中即收舌。
	/// Extra98 条带绘制（无 CWR 运行时依赖）。时间轴启发自 GluttonousTongue。
	/// </summary>
	public class LickTongueProj : HenshinMoveProj
	{
		private const int ExtendTicks = 14;
		private const int RetractTicks = 14;
		private const float MaxLength = 20f * 16f;
		private const float MawOffset = 26f;
		private const float LineWidth = 20f;
		private const int ConfusedTicks = 40;

		private static Asset<Texture2D> _extra98;

		private Vector2 _root;
		private Vector2 _tip;
		private Vector2 _aimDir = Vector2.UnitX;
		private float _length;
		private float _retractFrom;
		private int _phase; // 0 extend, 1 retract
		private int _phaseTimer;
		private bool _hitLatch;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = ExtendTicks + RetractTicks + 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = ExtendTicks + RetractTicks;
			Projectile.ownerHitCheck = false;
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
				_extra98 ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Extra98", AssetRequestMode.ImmediateLoad);
				SoundEngine.PlaySound(SoundID.Item17 with { Pitch = -0.55f, Volume = 0.95f }, owner.Center);
				SoundEngine.PlaySound(SoundID.NPCHit18 with { Pitch = -0.25f, Volume = 0.7f }, owner.Center);
				ResolveAim(owner);
			}

			// 出舌后锁定方向，不跟鼠标也不索敌
			_root = owner.MountedCenter + _aimDir * MawOffset;

			_phaseTimer++;
			if (_phase == 0)
			{
				float t = MathHelper.Clamp(_phaseTimer / (float)ExtendTicks, 0f, 1f);
				float ease = 1f - (1f - t) * (1f - t);
				_length = MaxLength * ease;
				if (_phaseTimer >= ExtendTicks)
					BeginRetract();
			}
			else
			{
				float t = MathHelper.Clamp(_phaseTimer / (float)RetractTicks, 0f, 1f);
				float ease = t * t;
				_length = _retractFrom * (1f - ease);
				if (_phaseTimer >= RetractTicks || _length < 4f)
				{
					Projectile.Kill();
					return;
				}
			}

			_tip = _root + _aimDir * Math.Max(_length, 1f);
			Projectile.Center = Vector2.Lerp(_root, _tip, 0.55f);
			owner.direction = _aimDir.X >= 0f ? 1 : -1;
			Lighting.AddLight(Vector2.Lerp(_root, _tip, 0.5f), 0.45f, 0.15f, 0.35f);

			if (Main.rand.NextBool(4) && _length > 16f)
			{
				Vector2 sample = Vector2.Lerp(_root, _tip, Main.rand.NextFloat(0.2f, 0.95f));
				Dust.NewDustPerfect(sample, DustID.Shadowflame, _aimDir * Main.rand.NextFloat(0.5f, 2f), 120,
					new Color(220, 120, 200), 1.05f).noGravity = true;
			}
		}

		public override bool? CanDamage() => _phase == 0 && !_hitLatch ? null : false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_length < 8f)
				return false;
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _root, _tip, LineWidth, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, ConfusedTicks);
			target.velocity *= 0.25f;
			SoundEngine.PlaySound(SoundID.NPCHit13 with { Pitch = -0.4f, Volume = 1.05f }, target.Center);
			if (!_hitLatch)
			{
				_hitLatch = true;
				BeginRetract();
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 0.7f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_extra98 ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Extra98", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _extra98.Value;
			if (tex == null || _length < 4f)
				return false;

			Vector2 delta = _tip - _root;
			float len = delta.Length();
			if (len < 2f)
				return false;
			Vector2 dir = delta / len;
			float rot = dir.ToRotation() + MathHelper.PiOver2;
			Vector2 origin = tex.Size() * 0.5f;

			float step = Math.Max(6f, tex.Height * 0.28f);
			int segs = Math.Max(2, (int)(len / step) + 1);
			Color flesh = new(255, 140, 200, 255);
			Color dark = new(180, 70, 140, 240);
			for (int i = 0; i <= segs; i++)
			{
				float u = i / (float)segs;
				Vector2 pos = Vector2.Lerp(_root, _tip, u);
				float taper = MathHelper.Lerp(1.05f, 0.72f, u);
				float pulse = 1f + 0.06f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + u * 6f);
				Color c = Color.Lerp(dark, flesh, 0.35f + 0.45f * u);
				Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, c, rot, origin,
					new Vector2(0.55f * taper * pulse, 0.7f * taper), SpriteEffects.None, 0f);
			}

			Main.spriteBatch.Draw(tex, _tip - Main.screenPosition, null, flesh, rot, origin,
				new Vector2(0.72f, 0.85f), SpriteEffects.None, 0f);
			return false;
		}

		private void BeginRetract()
		{
			if (_phase == 1)
				return;
			_phase = 1;
			_phaseTimer = 0;
			_retractFrom = Math.Max(_length, 8f);
		}

		private void ResolveAim(Player owner)
		{
			Vector2 prefer = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
			if (prefer.LengthSquared() < 1f)
				prefer = new Vector2(owner.direction, 0f);
			_aimDir = Vector2.Normalize(prefer);
		}
	}

	/// <summary>
	/// 催眠术：localAI 闩只施加一次——屏幕内非 Boss 挂 HenshinSleepDebuff 5s；Boss Slow+减速。
	/// 无伤；SoftGlow 环演出。
	/// </summary>
	public class HypnosisWaveProj : HenshinMoveProj
	{
		private const int Life = 40;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life;
		}

		public override bool? CanDamage() => false;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active || p.dead)
			{
				Projectile.Kill();
				return;
			}

			Projectile.Center = p.Center;

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				ApplyScreenSleep(p);
				if (!Main.dedServ)
					SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.35f, Volume = 0.9f }, p.Center);
			}

			if (!Main.dedServ && Main.rand.NextBool(2))
			{
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(90f, 70f), DustID.Shadowflame,
					Vector2.Zero, 150, new Color(180, 80, 255), 1.15f).noGravity = true;
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(70f, 55f), DustID.MagicMirror,
					Vector2.Zero, 150, default, 1.0f).noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / (float)Life;
			float pulse = 0.85f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f);
			float expand = 1f + (1f - life) * 0.55f;
			float diam = 160f * expand * pulse;
			HenshinFxDraw.BeginAdditive();
			Color c = HenshinFxDraw.WithAlpha(new Color(180, 80, 255), 0.55f * life);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, c,
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, diam));
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(140, 60, 220), 0.32f * life),
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam * 1.1f));
			HenshinFxDraw.EndAdditive();
			return false;
		}

		private static void ApplyScreenSleep(Player owner)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Rectangle screen = GetActiveScreenBounds(owner);
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage)
					continue;
				if (!screen.Intersects(n.Hitbox))
					continue;

				bool isBoss = n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type];
				if (isBoss)
				{
					n.AddBuff(BuffID.Slow, 120);
					n.velocity *= 0.25f;
					n.netUpdate = true;
				}
				else if (n.CanBeChasedBy())
				{
					n.AddBuff(ModContent.BuffType<HenshinSleepDebuff>(), HenshinSleepDebuff.DefaultDuration);
				}
			}
		}

		private static Rectangle GetActiveScreenBounds(Player owner)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				Vector2 c = owner.Center;
				return new Rectangle((int)(c.X - 960), (int)(c.Y - 540), 1920, 1080);
			}

			return new Rectangle(
				(int)(Main.screenPosition.X - 64),
				(int)(Main.screenPosition.Y - 64),
				Main.screenWidth + 128,
				Main.screenHeight + 128);
		}
	}
}
