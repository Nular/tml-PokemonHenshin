using System;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Core;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	public enum HitTier : byte { Light, Heavy, Ultimate }

	/// <summary>
	/// 共享打击感（只做表现，不碰伤害 / 判定 / 位移 / 寿命，不发包）：
	/// 震屏（仅本机主人）、Ultimate 顿帧（仅本机主人，冻结<b>绘制</b>位置与动画 N tick）、NPC 白闪、命中光脉冲。
	/// <b>本类从不播放声音</b>：命中音放在命中特效弹的 AI 首帧，由 HenshinSfx 负责（这样所有端都能听到）。
	/// <para>接入：OnHitNPC 里 <c>HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), tint);</c></para>
	/// </summary>
	public static class HenshinHitFeel
	{
		public const string ShakeId = "HenshinHit";

		/// <summary>Ultimate 震屏 + 顿帧的本机冷却：每 20 tick 最多一次（64 发子弹也只触发一次）。</summary>
		public const int UltimateWindowTicks = 20;

		/// <summary>Heavy 震屏的本机节流。</summary>
		public const int HeavyShakeCooldown = 8;

		public readonly struct TierSpec
		{
			public readonly float ShakeStrength;
			public readonly float ShakeCycles;
			public readonly int ShakeFrames;
			public readonly int HitStopTicks;
			public readonly int FlashTicks;
			public readonly float FlashPower;
			public readonly float Diameter;
			public readonly float Knockback;
			public readonly BlastSize LightSize;
			public readonly float LightScale;

			public TierSpec(float shake, float cycles, int frames, int stop, int flashTicks, float flashPower, float diameter, float knockback, BlastSize lightSize, float lightScale)
			{
				ShakeStrength = shake;
				ShakeCycles = cycles;
				ShakeFrames = frames;
				HitStopTicks = stop;
				FlashTicks = flashTicks;
				FlashPower = flashPower;
				Diameter = diameter;
				Knockback = knockback;
				LightSize = lightSize;
				LightScale = lightScale;
			}
		}

		// 档位表（初值，游戏内手调后只改这里）。
		private static readonly TierSpec LightSpec = new(0f, 0f, 0, 0, 6, 0.55f, 40f, 3f, BlastSize.S, 0.5f);
		private static readonly TierSpec HeavySpec = new(3.5f, 10f, 10, 0, 8, 0.75f, 72f, 6f, BlastSize.S, 1f);
		private static readonly TierSpec UltSpec = new(7.5f, 12f, 18, 4, 12, 0.9f, 140f, 10f, BlastSize.M, 1f);

		public static TierSpec Spec(HitTier tier) => tier switch
		{
			HitTier.Light => LightSpec,
			HitTier.Heavy => HeavySpec,
			_ => UltSpec
		};

		/// <summary>推荐命中 sprite 世界直径：40 / 72 / 140。</summary>
		public static float RecommendedDiameter(HitTier tier) => Spec(tier).Diameter;

		/// <summary>推荐击退（仅供新招参考，不覆盖已调好的招）：3 / 6 / 10。</summary>
		public static float RecommendedKnockback(HitTier tier) => Spec(tier).Knockback;

		/// <summary>
		/// 共用弹幕类按出弹槽定档：出自大招槽（SourceMoveSlot == Ultimate）→ Ultimate，否则用类给的默认档。
		/// 例：LungeProj 既是鲤鱼王撞击又是火恐龙闪焰冲锋大招，写 <c>TierOf(Projectile, HitTier.Light)</c> 即可。
		/// </summary>
		public static HitTier TierOf(Projectile p, HitTier skillDefault)
		{
			if (p == null)
				return skillDefault;
			if (p.TryGetGlobalProjectile(out HenshinAccGlobalProjectile gp) && gp.HasSourceMoveSlot)
				return gp.SourceMoveSlot == MoveSlot.Ultimate ? HitTier.Ultimate : skillDefault;
			if (p.ModProjectile is IHenshinMoveProj hm && hm.HasSourceMoveSlot)
				return hm.SourceMoveSlot == MoveSlot.Ultimate ? HitTier.Ultimate : skillDefault;
			return skillDefault;
		}

		/// <summary>
		/// 一个调用点：放在 OnHitNPC（只在主人端跑）。白闪（加强）+ 光脉冲 + 震屏 + Ultimate 顿帧。不放声音。
		/// </summary>
		/// <param name="tint">光脉冲颜色（暗色自动抬亮）。</param>
		/// <param name="at">命中点；null = 弹幕与目标连线上贴近目标的一点。</param>
		/// <param name="lightScale">命中光脉冲的额外倍率；1 = 旧行为。命中闪算 glow 例外，lit 招式（水 / 石 / 土 / 草 / 风）建议 0.5。</param>
		public static void OnHit(Projectile p, NPC target, HitTier tier, Color tint, Vector2? at = null, float lightScale = 1f)
		{
			if (p == null || target == null || Main.dedServ)
				return;
			Flash(target, tier);

			TierSpec spec = Spec(tier);
			Vector2 hitPos = at ?? Vector2.Lerp(target.Center, p.Center, 0.35f);
			if (lightScale > 0f)
				HenshinExplosion.SpawnLight(hitPos, tint, spec.LightSize, spec.LightScale * lightScale);

			Vector2 dir = p.velocity.LengthSquared() > 1f ? Vector2.Normalize(p.velocity) : SafeDir(target.Center - p.Center);
			bool fired = Shake(p.owner, hitPos, tier, dir);
			if (fired && tier == HitTier.Ultimate)
				HitStop(p, spec.HitStopTicks);
		}

		/// <summary>
		/// 震屏，只在 <paramref name="owner"/> 是本机玩家时生效；固定 uniqueIdentity <see cref="ShakeId"/>（同 id 替换不叠加）。
		/// Ultimate 每 <see cref="UltimateWindowTicks"/> tick 最多一次；Heavy 每 <see cref="HeavyShakeCooldown"/> tick 最多一次，且不打断正在进行的 Ultimate 震。
		/// 返回是否真的震了。
		/// </summary>
		public static bool Shake(int owner, Vector2 at, HitTier tier, Vector2? dir = null)
		{
			if (Main.dedServ || owner != Main.myPlayer)
				return false;
			TierSpec spec = Spec(tier);
			if (spec.ShakeStrength <= 0f)
				return false;
			uint now = Main.GameUpdateCount;
			if (tier == HitTier.Ultimate)
			{
				if (_hasUlt && now - _lastUlt < UltimateWindowTicks)
					return false;
				_hasUlt = true;
				_lastUlt = now;
				_ultShakeUntil = now + (uint)spec.ShakeFrames;
			}
			else
			{
				if (now < _ultShakeUntil || (_hasHeavy && now - _lastHeavy < HeavyShakeCooldown))
					return false;
				_hasHeavy = true;
				_lastHeavy = now;
			}
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(at, SafeDir(dir ?? Vector2.UnitY), spec.ShakeStrength, spec.ShakeCycles, spec.ShakeFrames, 1600f, ShakeId));
			return true;
		}

		private static bool _hasUlt;
		private static uint _lastUlt;
		private static bool _hasHeavy;
		private static uint _lastHeavy;
		private static uint _ultShakeUntil;

		/// <summary>NPC 白闪（本端）。所有端经 HitEffect 已有基础白闪；这里在主人端按档位加长加亮。</summary>
		public static void Flash(NPC npc, HitTier tier)
		{
			if (npc == null || !npc.active)
				return;
			TierSpec spec = Spec(tier);
			npc.GetGlobalNPC<HenshinHitFlashGlobalNPC>().Flash(spec.FlashTicks, spec.FlashPower);
		}

		/// <summary>
		/// 表现顿帧：本机主人端在 <paramref name="ticks"/> tick（按 <see cref="Main.GameUpdateCount"/>，与 extraUpdates 无关）内
		/// 用命中瞬间的位置 / 朝向 / 动画帧 / ai / timeLeft 去<b>画</b>这颗弹；逻辑、位移、伤害、寿命照常，到期后画面跟上真实位置。
		/// 远端不做（远端也不震屏）。
		/// </summary>
		public static void HitStop(Projectile p, int ticks)
		{
			if (Main.dedServ || p == null || !p.active || ticks <= 0 || p.owner != Main.myPlayer)
				return;
			if (p.TryGetGlobalProjectile(out HenshinHitStopGlobalProjectile gp))
				gp.Begin(p, ticks);
		}

		private static Vector2 SafeDir(Vector2 v) => v.LengthSquared() > 0.0001f ? Vector2.Normalize(v) : Vector2.UnitY;
	}

	/// <summary>表现顿帧的快照（只挂本模弹幕）。绘制时临时换成快照字段，画完立即还原。</summary>
	public sealed class HenshinHitStopGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		private uint _until;
		private Vector2 _position;
		private Vector2 _velocity;
		private float _rotation;
		private int _frame;
		private int _frameCounter;
		private int _timeLeft;
		private int _direction;
		private int _spriteDirection;
		private readonly float[] _ai = new float[Projectile.maxAI];
		private readonly float[] _localAI = new float[Projectile.maxAI];

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
			=> lateInstantiation && entity.ModProjectile != null && entity.ModProjectile.Mod == Mod;

		internal bool Active => Main.GameUpdateCount < _until;

		internal void Begin(Projectile p, int ticks)
		{
			if (Active)
				return;
			_until = Main.GameUpdateCount + (uint)ticks;
			_position = p.position;
			_velocity = p.velocity;
			_rotation = p.rotation;
			_frame = p.frame;
			_frameCounter = p.frameCounter;
			_timeLeft = p.timeLeft;
			_direction = p.direction;
			_spriteDirection = p.spriteDirection;
			Array.Copy(p.ai, _ai, Math.Min(p.ai.Length, _ai.Length));
			Array.Copy(p.localAI, _localAI, Math.Min(p.localAI.Length, _localAI.Length));
		}

		/// <summary>交换真实字段与快照（调用两次即还原）。</summary>
		internal void Swap(Projectile p)
		{
			(p.position, _position) = (_position, p.position);
			(p.velocity, _velocity) = (_velocity, p.velocity);
			(p.rotation, _rotation) = (_rotation, p.rotation);
			(p.frame, _frame) = (_frame, p.frame);
			(p.frameCounter, _frameCounter) = (_frameCounter, p.frameCounter);
			(p.timeLeft, _timeLeft) = (_timeLeft, p.timeLeft);
			(p.direction, _direction) = (_direction, p.direction);
			(p.spriteDirection, _spriteDirection) = (_spriteDirection, p.spriteDirection);
			int n = Math.Min(p.ai.Length, _ai.Length);
			for (int i = 0; i < n; i++)
				(p.ai[i], _ai[i]) = (_ai[i], p.ai[i]);
			n = Math.Min(p.localAI.Length, _localAI.Length);
			for (int i = 0; i < n; i++)
				(p.localAI[i], _localAI[i]) = (_localAI[i], p.localAI[i]);
		}
	}

	/// <summary>包住 Main.DrawProj：处于顿帧的弹幕用快照画，画完（含异常）还原。仅客户端挂钩。</summary>
	internal sealed class HenshinHitFeelSystem : ModSystem
	{
		public override void Load()
		{
			if (!Main.dedServ)
				Terraria.On_Main.DrawProj += DrawProjFrozen;
		}

		public override void Unload()
		{
			if (!Main.dedServ)
				Terraria.On_Main.DrawProj -= DrawProjFrozen;
		}

		private static void DrawProjFrozen(Terraria.On_Main.orig_DrawProj orig, Main self, int i)
		{
			Projectile p = Main.projectile[i];
			if (p.owner != Main.myPlayer || !p.TryGetGlobalProjectile(out HenshinHitStopGlobalProjectile gp) || !gp.Active)
			{
				orig(self, i);
				return;
			}
			gp.Swap(p);
			try
			{
				orig(self, i);
			}
			finally
			{
				gp.Swap(p);
			}
		}
	}
}
