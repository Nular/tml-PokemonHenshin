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
			"Psychic/PsychicRingA", "Psychic/PsychicRingM", "Psychic/PsychicRingT"
		};
		private static readonly int[] HitFrames = { 5, 4, 5, 5, 5, 5, 5, 5, 5 };
		public const int HitTicksPerFrame = 3;

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

		/// <summary>仅拥有者客户端生成 0 伤害命中动画（随弹同步到其他客户端）。</summary>
		public static void SpawnHit(IEntitySource src, Vector2 pos, int kind, float scale, int owner, float rot = 0f)
		{
			if (owner != Main.myPlayer)
				return;
			int id = Projectile.NewProjectile(src, pos, Vector2.Zero, ModContent.ProjectileType<H3HitFxProj>(), 0, 0f,
				owner, kind, rot, scale);
			if (id >= 0)
				Main.projectile[id].Center = pos;
		}
	}

	/// <summary>H3 命中/施放特效：ai0=kind，ai1=旋转，ai2=缩放。纯视觉，不造成伤害。</summary>
	public class H3HitFxProj : ModProjectile
	{
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
				Projectile.timeLeft = H3Fx.HitFrameCount((int)Projectile.ai[0]) * H3Fx.HitTicksPerFrame;
			}
			Projectile.localAI[1]++;
			if (Projectile.localAI[1] < 8f)
			{
				int k = (int)Projectile.ai[0];
				Vector3 c = k >= H3Fx.RingGold ? H3Fx.VariantLight[Math.Min(k - H3Fx.RingGold, 2)] : new Vector3(0.35f, 0.2f, 0.5f);
				Lighting.AddLight(Projectile.Center, c);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int kind = (int)Projectile.ai[0];
			float scale = Projectile.ai[2] > 0.01f ? Projectile.ai[2] : 1f;
			int frame = (int)(Projectile.localAI[1] / H3Fx.HitTicksPerFrame);
			H3Fx.Draw(H3Fx.HitSheet(kind), H3Fx.HitFrameCount(kind), frame, Projectile.Center, Projectile.ai[1], scale, Color.White);
			return false;
		}
	}
}
