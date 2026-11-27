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
	/// 暗影球：自绘 4 帧紫黑球（Assets/Fx/ShadowBall）+ 两道滞后残影；命中/落地播 ShadowBallHit 帧动画。
	/// 不沿线撒粒子，不用 Additive。
	/// R3：球更小、速度 ×1.5；命中后在附近敌人间弹跳最多 5 次，第 n 次弹跳伤害 ×0.8^n（ai0 = 已弹跳次数，随弹同步）。
	/// 6 个不同敌人全中总量 = 1.35 × (1+0.8+0.64+0.51+0.41+0.33) ≈ 1.35 × 3.69 ≈ 倍率 4.98（单体仍 1.35）。
	/// </summary>
	public class BigShadowBallProj : HenshinMoveProj
	{
		private const int MaxBounces = 5;
		private const float BounceRange = 28f * 16f;
		private const float BounceDamageFalloff = 0.8f;
		private const float SpeedBoost = 1.5f;
		private readonly System.Collections.Generic.List<int> _hit = new(); // 仅拥有者端使用

		// R3 手感：暗紫拖尾（HenshinTrail）代替两道滞后残影；施放 / 命中用自制幽灵音。
		private static readonly Color TrailGlow = new(150, 60, 255);
		private static readonly Color TrailCore = new(230, 190, 255);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 14);
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // 每个敌人只吃一次
			Projectile.ignoreWater = true;
		}

		// OnSpawn 只在发射端跑，加速后的 velocity 随 NewProjectile 同步给其他端。
		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Homing = false;
			Projectile.velocity *= SpeedBoost;
		}

		public override void AI()
		{
			Homing = false;
			if (Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				HenshinSfx.Play(SfxKey.GhostCast, Projectile.Center);
			}
			float pulse = 0.35f + 0.2f * (0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f));
			Lighting.AddLight(Projectile.Center, pulse, pulse * 0.2f, pulse * 1.2f);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.SourceDamage *= MathF.Pow(BounceDamageFalloff, Projectile.ai[0]);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
			if (Projectile.owner != Main.myPlayer)
				return;

			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), TrailGlow);
			_hit.Add(target.whoAmI);
			NPC next = null;
			float best = BounceRange * BounceRange;
			if (Projectile.ai[0] < MaxBounces)
			{
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (!n.active || !n.CanBeChasedBy() || _hit.Contains(i))
						continue;
					float d = Projectile.DistanceSQ(n.Center);
					if (d < best)
					{
						best = d;
						next = n;
					}
				}
			}

			if (next == null)
			{
				Projectile.Kill();
				return;
			}

			Projectile.ai[0]++;
			float speed = Math.Max(Projectile.velocity.Length(), 9f * SpeedBoost);
			Projectile.velocity = Projectile.DirectionTo(next.Center) * speed;
			Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);
			Projectile.netUpdate = true;
			// 弹跳命中：小号暗影爆 + 幽灵命中音（特效弹各端首帧播）。
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.ShadowHit, 0.55f, Projectile.owner, 0f, SfxKey.GhostHit);
		}

		// 末次命中、无下一个目标、落地、超时都会走到 OnKill：命中动画只在这里生成一次（S 爆炸配方 + 闷一点的小爆音）。
		public override void OnKill(int timeLeft)
		{
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.ShadowHit, 1f, Projectile.owner, 0f, SfxKey.GhostHit, BlastSize.S);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			HenshinTrail.Strip(Projectile, TrailGlow, 18f, TrailCore, 0.34f, 0f, 0.85f);
			H3Fx.Draw(H3Fx.ShadowBall, 4, frame, Projectile.Center, 0f, 0.75f, Color.White);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(TrailGlow, 0.35f), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 44f));
			HenshinFxDraw.EndAdditive();
			return false;
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

		private Vector2 _root;
		private Vector2 _tip;
		private Vector2 _aimDir = Vector2.UnitX;
		private float _length;
		private float _retractFrom;
		private int _phase; // 0 extend, 1 retract
		private int _phaseTimer;
		private bool _hitLatch;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// 伸舌音（Item17 + NPCHit18）在 AI 首帧各端播，声明后之力不再叠 Item20。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			// 命中音挪到命中特效弹首帧（原 NPCHit13 只有主人听得到）。
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.LickHit, 0.9f, Projectile.owner, 0f, SfxKey.GhostHit);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(255, 120, 200), target.Center);
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

		// 贴图 Assets/Fx/Lick/LickTongue：帧0 = 舌身（28×20 px，可沿长度平铺），帧1 = 舌尖。
		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 delta = _tip - _root;
			float len = delta.Length();
			if (len < 4f)
				return false;
			Vector2 dir = delta / len;
			float rot = dir.ToRotation();
			// 朝左半圈上下翻转，让舌面高光始终朝上。
			SpriteEffects fx = dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			const float seg = 28f;
			Texture2D tex = H3Fx.Get(H3Fx.LickTongue);
			int fh = tex.Height / 2;
			Rectangle bodySrc = new(0, 0, tex.Width, fh);
			Rectangle tipSrc = new(0, fh, tex.Width, fh);
			Vector2 origin = bodySrc.Size() * 0.5f;
			int n = len > seg ? (int)MathF.Ceiling((len - seg) / seg) : 0;
			float step = n > 0 ? (len - seg) / n : 0f;
			for (int i = 0; i < n; i++)
			{
				Vector2 p = _root + dir * (step * (i + 0.5f) + seg * 0.5f);
				Main.EntitySpriteDraw(tex, (p - Main.screenPosition).Floor(), bodySrc, Color.White, rot, origin,
					new Vector2(step / seg * 1.06f, 1f), fx, 0f);
			}
			Vector2 tipPos = _tip - dir * (seg * 0.5f);
			Main.EntitySpriteDraw(tex, (tipPos - Main.screenPosition).Floor(), tipSrc, Color.White, rot, origin, 1f, fx, 0f);
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

			// 起手只留共享 UltCast（大招根弹自动播），删掉原 Item8，免得叠两声。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				ApplyScreenSleep(p);
			}

			int age = Life - Projectile.timeLeft;
			// 场域光：整段先亮后衰，照亮身周（光点随环外扩）。
			HenshinExplosion.Pulse(Projectile.Center, SpiralTint, BlastSize.L, age / (float)Life, 420f, 0.8f);
			// 螺旋张满的一拍：低沉幽灵音 + 主人端大招震屏 + 已入睡敌人白闪（睡眠 buff 由服务器同步，各端读到即闪，纯表现）。
			if (age == SleepBeatAge && Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				HenshinSfx.Play(SfxKey.GhostHit, p.Center, 1f, -0.45f);
				HenshinHitFeel.Shake(Projectile.owner, p.Center, HitTier.Ultimate);
				if (!Main.dedServ)
				{
					int sleepType = ModContent.BuffType<HenshinSleepDebuff>();
					for (int i = 0; i < Main.maxNPCs; i++)
					{
						NPC n = Main.npc[i];
						if (n.active && n.HasBuff(sleepType))
							HenshinHitFeel.Flash(n, HitTier.Heavy);
					}
				}
			}
		}

		private const int SleepBeatAge = 14;
		private static readonly Color SpiralTint = new(210, 110, 255);

		// 贴图 Assets/Fx/Hypnosis/HypnosisRing：6 帧催眠螺旋，随寿命推进并缓慢旋转；末段淡出。
		// R3：螺旋下垫一层加法紫光（随螺旋张开变大），张满那一拍叠一圈 L 号冲击环（不叠 Kenney 火烟）。
		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			int frame = Math.Min(5, age * 6 / Life);
			float fade = Math.Min(1f, Projectile.timeLeft / 10f);
			float rot = age * 0.07f;
			float open = MathHelper.Clamp(age / (float)SleepBeatAge, 0f, 1f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(SpiralTint, 0.45f * fade), HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 160f + 260f * open));
			HenshinFxDraw.EndAdditive();
			H3Fx.Draw(H3Fx.Hypnosis, 6, frame, Projectile.Center, rot, 2.5f, Color.White * fade);
			H3Fx.Draw(H3Fx.Hypnosis, 6, Math.Max(0, frame - 1), Projectile.Center, -rot * 1.3f + 1f, 1.5f, Color.White * (0.7f * fade));
			int beat = age - SleepBeatAge;
			int beatLife = HenshinExplosion.Duration(BlastSize.L);
			if (beat >= 0 && beat < beatLife)
			{
				HenshinFxDraw.BeginAdditive();
				HenshinExplosion.Draw(Projectile.Center, BlastSize.L, SpiralTint, beat / (float)beatLife, 420f, 0f, kenneyFrame: false);
				HenshinFxDraw.EndAdditive();
			}
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
