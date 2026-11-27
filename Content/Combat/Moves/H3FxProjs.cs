using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>H3 组（鬼斯通/耿鬼/胡地/超梦）共用的贴图懒加载、单帧绘制与命中特效生成。贴图在 Assets/Fx 下。</summary>
	public static class H3Fx
	{
		public const int ShadowHit = 0;
		public const int LickHit = 1;
		public const int SludgeHit = 2;
		public const int PsychicRing = 3;
		public const int FocusHit = 4;
		public const int DarkPulseRing = 5;
		// 念力命中环三色（与波纹射弹同色）：胡地金 / 巨金怪青蓝 / 超梦紫
		public const int RingGold = 6;
		public const int RingCyan = 7;
		public const int RingPurple = 8;
		// 无贴图命中点（只画加法光 + 爆炸配方 + 播音）：给没有命中特效弹的钢 / 岩 / 光束招同步命中音与光。
		public const int SteelSpark = 9;
		public const int RockBurst = 10;
		public const int BeamSpark = 11;

		public const string ShadowBall = "ShadowBall/ShadowBall";
		public const string SludgeBomb = "SludgeBomb/SludgeBomb";
		public const string LickTongue = "Lick/LickTongue";
		public const string Hypnosis = "Hypnosis/HypnosisRing";
		public const string PsychicOrb = "Psychic/PsychicOrb";
		public const string PsychicOrbM = "Psychic/PsychicOrbM";
		public const string PsychicRingSheet = "Psychic/PsychicRing";
		public const string FocusFist = "FocusPunch/FocusFist";
		public const string FocusAura = "FocusPunch/FocusAura";
		public const string ZenHammer = "ZenHammer/ZenHammer";

		// 念力波纹射弹（4 帧，朝右，核心在 (9,20)）：0 胡地金 / 1 巨金怪青蓝 / 2 超梦紫。variant 由射弹 ai 同步。
		public static readonly string[] PsychicWave = { "Psychic/PsychicWaveA", "Psychic/PsychicWaveM", "Psychic/PsychicWaveT" };
		public static readonly Vector2 PsychicWaveOrigin = new(9f, 20f);

		// 命中特效表：kind → 贴图 / 帧数（每帧 3 tick）
		private static readonly string[] HitPath =
		{
			"ShadowBall/ShadowBallHit", "Lick/LickHit", "SludgeBomb/SludgeHit",
			"Psychic/PsychicRing", "FocusPunch/FocusHit", "DarkPulse/DarkPulseRing",
			"Psychic/PsychicRingA", "Psychic/PsychicRingM", "Psychic/PsychicRingT",
			null, null, null
		};
		private static readonly int[] HitFrames = { 5, 4, 5, 5, 5, 5, 5, 5, 5, 0, 0, 0 };
		public const int HitTicksPerFrame = 3;

		// 每种命中的加法光色（暗色由 HenshinExplosion.Lift 抬亮）与可见直径（≈ 单帧宽，px，乘 scale）。念力三色固定：胡地金 / 巨金怪青蓝 / 超梦紫。
		private static readonly Color[] HitTint =
		{
			new(170, 80, 255), new(255, 120, 200), new(180, 90, 255),
			new(255, 130, 230), new(255, 200, 120), new(150, 60, 235),
			new(255, 205, 70), new(80, 215, 255), new(205, 110, 255),
			new(190, 225, 255), new(235, 190, 130), new(200, 90, 255)
		};
		private static readonly float[] HitDiameter = { 60f, 48f, 56f, 96f, 60f, 112f, 96f, 96f, 96f, 48f, 48f, 56f };

		public static Color Tint(int kind) => HitTint[Math.Clamp(kind, 0, HitTint.Length - 1)];
		public static float Diameter(int kind) => HitDiameter[Math.Clamp(kind, 0, HitDiameter.Length - 1)];
		public static bool HasSheet(int kind) => HitPath[Math.Clamp(kind, 0, HitPath.Length - 1)] != null;

		// ai0 打包：bit0-3 kind，bit4-9 音效（SfxKey+1，0 = 不播），bit10-11 爆炸（0 无 / 1 S / 2 M / 3 L）。整数 < 2^12，float 精确同步。
		private const int KindMask = 15;
		private const int SfxShift = 4;
		private const int SfxMask = 63;
		private const int BlastShift = 10;

		public static int UnpackKind(float ai0) => (int)ai0 & KindMask;
		internal static SfxKey? UnpackSfx(float ai0)
		{
			int s = ((int)ai0 >> SfxShift) & SfxMask;
			return s == 0 ? null : (SfxKey)(s - 1);
		}
		public static BlastSize? UnpackBlast(float ai0)
		{
			int b = ((int)ai0 >> BlastShift) & 3;
			return b == 0 ? null : (BlastSize)(b - 1);
		}

		private static uint _ultBeatAt;
		private static bool _ultBeatAny;

		/// <summary>
		/// 大招"重音"节拍：本机主人端每 <paramref name="window"/> tick 最多返回一次 true（多发大招只给第一下配 HitUlt + 大爆）。
		/// 只在 OnHitNPC（主人端）里调；结果用来决定生成的命中特效弹带哪个音 / 多大爆炸，特效弹同步给所有端。
		/// </summary>
		public static bool TryUltBeat(int owner, int window = 60)
		{
			if (owner != Main.myPlayer)
				return false;
			uint now = Main.GameUpdateCount;
			if (_ultBeatAny && now - _ultBeatAt < (uint)window)
				return false;
			_ultBeatAny = true;
			_ultBeatAt = now;
			return true;
		}

		/// <summary>念力色变体 0 胡地金 / 1 巨金怪青蓝 / 2 超梦紫 → 对应命中环 kind。</summary>
		public static int RingKind(int variant) => RingGold + Math.Clamp(variant, 0, 2);
		public static readonly Vector3[] VariantLight = { new(0.6f, 0.45f, 0.1f), new(0.15f, 0.4f, 0.6f), new(0.4f, 0.2f, 0.6f) };

		/// <summary>波纹射弹：本体脉动 + 2 圈向外扩散并淡出的回波（一圈 28 tick，程序化）。AlphaBlend，不用加法。</summary>
		public static void DrawRipple(int variant, Vector2 center, float rot, float scale, float alpha)
		{
			variant = Math.Clamp(variant, 0, 2);
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			Vector2 fwd = rot.ToRotationVector2();
			for (int k = 0; k < 2; k++)
			{
				float p = ((Main.GameUpdateCount + k * 14) % 28) / 28f;
				Draw(PsychicWave[variant], 4, (frame + k * 2) % 4, center - fwd * (p * 14f), rot,
					scale * (0.7f + 0.9f * p), Color.White * (alpha * 0.55f * (1f - p)), SpriteEffects.None, PsychicWaveOrigin);
			}
			float pulse = 1f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.4f);
			Draw(PsychicWave[variant], 4, frame, center, rot, scale * pulse, Color.White * alpha, SpriteEffects.None, PsychicWaveOrigin);
		}

		private static readonly Dictionary<string, Asset<Texture2D>> Cache = new();

		public static string HitSheet(int kind) => HitPath[Math.Clamp(kind, 0, HitPath.Length - 1)];
		public static int HitFrameCount(int kind) => HitFrames[Math.Clamp(kind, 0, HitFrames.Length - 1)];

		public static Texture2D Get(string path)
		{
			if (!Cache.TryGetValue(path, out Asset<Texture2D> a))
			{
				a = ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/" + path, AssetRequestMode.ImmediateLoad);
				Cache[path] = a;
			}
			return a.Value;
		}

		/// <summary>竖条多帧贴图取一帧，绕帧中心绘制（AlphaBlend；贴图 alpha 只有 0/255）。</summary>
		public static void Draw(string path, int frames, int frame, Vector2 world, float rot, float scale, Color color,
			SpriteEffects fx = SpriteEffects.None, Vector2? origin = null)
		{
			if (Main.dedServ)
				return;
			Texture2D tex = Get(path);
			int fh = tex.Height / frames;
			frame = Math.Clamp(frame, 0, frames - 1);
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			Main.EntitySpriteDraw(tex, (world - Main.screenPosition).Floor(), src, color, rot, origin ?? src.Size() * 0.5f, scale, fx, 0f);
		}

		/// <summary>
		/// 仅拥有者客户端生成 0 伤害命中动画（随弹同步到其他客户端）。
		/// <paramref name="sfx"/>：命中音，由特效弹在各端 AI 首帧播（PlayCooldown 节流）；<paramref name="blast"/>：叠一层 HenshinExplosion 爆炸配方。
		/// 两者默认不带，旧调用方行为只多了加法光层。
		/// </summary>
		internal static void SpawnHit(IEntitySource src, Vector2 pos, int kind, float scale, int owner, float rot = 0f,
			SfxKey? sfx = null, BlastSize? blast = null)
		{
			if (owner != Main.myPlayer)
				return;
			int packed = Math.Clamp(kind, 0, KindMask)
				| ((sfx.HasValue ? (int)sfx.Value + 1 : 0) & SfxMask) << SfxShift
				| (blast.HasValue ? (int)blast.Value + 1 : 0) << BlastShift;
			int id = Projectile.NewProjectile(src, pos, Vector2.Zero, ModContent.ProjectileType<H3HitFxProj>(), 0, 0f,
				owner, packed, rot, scale);
			if (id >= 0)
				Main.projectile[id].Center = pos;
		}
	}

	/// <summary>
	/// H3 命中/施放特效：ai0 = 打包的 kind / 音效 / 爆炸（见 <see cref="H3Fx.SpawnHit"/>），ai1=旋转，ai2=缩放。纯视觉，不造成伤害。
	/// R3 手感：手绘帧下垫一层加法柔光、上叠一次白热核心闪和同帧加法亮层；光照改 HenshinExplosion.Pulse（先亮后衰，全寿命）；
	/// 命中音在各端 AI 首帧播。
	/// </summary>
	public class H3HitFxProj : ModProjectile
	{
		private int Kind => H3Fx.UnpackKind(Projectile.ai[0]);
		private float Scale => Projectile.ai[2] > 0.01f ? Projectile.ai[2] : 1f;
		private float VisualDiameter => H3Fx.Diameter(Kind) * Scale;

		private int Life
		{
			get
			{
				int kind = Kind;
				int life = H3Fx.HasSheet(kind) ? H3Fx.HitFrameCount(kind) * H3Fx.HitTicksPerFrame : 0;
				BlastSize? blast = H3Fx.UnpackBlast(Projectile.ai[0]);
				if (blast.HasValue || !H3Fx.HasSheet(kind))
					life = Math.Max(life, HenshinExplosion.Duration(blast ?? BlastSize.S));
				return Math.Max(1, life);
			}
		}

		private BlastSize LightSize => H3Fx.UnpackBlast(Projectile.ai[0]) ?? BlastSize.S;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = 60;
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
				Projectile.timeLeft = Life;
				// 命中音：各端首帧各播一次（不走 OnHitNPC，不套 owner 门）；同键本端节流，多发招不叠音。
				SfxKey? sfx = H3Fx.UnpackSfx(Projectile.ai[0]);
				if (sfx.HasValue)
				{
					SfxKey key = sfx.Value;
					int cd = key is SfxKey.HitUlt or SfxKey.BlastL ? 12 : key == SfxKey.BlastM ? 6 : 4;
					HenshinSfx.PlayCooldown(key, Projectile.Center, cd);
				}
			}
			Projectile.localAI[1]++;
			float t = MathHelper.Clamp(Projectile.localAI[1] / Life, 0f, 1f);
			bool blast = H3Fx.UnpackBlast(Projectile.ai[0]).HasValue;
			HenshinExplosion.Pulse(Projectile.Center, H3Fx.Tint(Kind), LightSize, t, VisualDiameter * 1.2f, blast ? 1f : 0.8f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int kind = Kind;
			float scale = Scale;
			float age = Projectile.localAI[1];
			float t = MathHelper.Clamp(age / Life, 0f, 1f);
			Color tint = HenshinExplosion.Lift(H3Fx.Tint(kind));
			float d = VisualDiameter;
			Texture2D glow = HenshinFxDraw.SoftGlow;

			// 底层柔光：比手绘帧大一圈，(1-t)^1.5 收。
			float bloom = MathF.Pow(1f - t, 1.5f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(glow, Projectile.Center, HenshinFxDraw.WithAlpha(tint, 0.5f * bloom),
				HenshinFxDraw.ScaleForWorldDiameter(glow, d * 1.5f));
			HenshinFxDraw.EndAdditive();

			int frame = (int)(age / H3Fx.HitTicksPerFrame);
			bool sheet = H3Fx.HasSheet(kind) && frame < H3Fx.HitFrameCount(kind);
			if (sheet)
				H3Fx.Draw(H3Fx.HitSheet(kind), H3Fx.HitFrameCount(kind), frame, Projectile.Center, Projectile.ai[1], scale, Color.White);

			HenshinFxDraw.BeginAdditive();
			// 同帧加法亮层（当 _Light 用）：前半段把手绘帧再加一遍，命中瞬间"发光"而不是平涂。
			if (sheet && t < 0.6f)
				H3Fx.Draw(H3Fx.HitSheet(kind), H3Fx.HitFrameCount(kind), frame, Projectile.Center, Projectile.ai[1], scale,
					HenshinFxDraw.WithAlpha(Color.White, 0.45f * (1f - t / 0.6f)));
			// 白热核心闪：前 30%。
			if (t < 0.3f)
				HenshinFxDraw.DrawAdditiveCentered(glow, Projectile.Center,
					HenshinFxDraw.WithAlpha(Color.Lerp(tint, Color.White, 0.65f), 0.75f * (1f - t / 0.3f)),
					HenshinFxDraw.ScaleForWorldDiameter(glow, d * 0.55f));
			BlastSize? blast = H3Fx.UnpackBlast(Projectile.ai[0]);
			if (blast.HasValue || !H3Fx.HasSheet(kind))
				HenshinExplosion.Draw(Projectile.Center, blast ?? BlastSize.S, tint, t, d * 1.2f, Projectile.ai[1], kenneyFrame: false);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
