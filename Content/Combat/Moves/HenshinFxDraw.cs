using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 本模 <c>Assets/Fx/*</c> Additive 绘制助手。Additive 时必须保留 Color.A（禁止 A=0 假发光）。
	/// Fire / Flashimpact / HitJagged01 为 sprite sheet，须用 SheetFrame API，禁止整图绘制。
	/// SoftGlow / Cyclone / Fog / DiffusionCircle / LightShot / LightBeam / TearFlame 可整图绘制。
	/// </summary>
	public static class HenshinFxDraw
	{
		public const int FireColumns = 4;
		public const int FireRows = 4;
		public const int FireFrames = FireColumns * FireRows;
		public const int FlashImpactColumns = 4;
		public const int FlashImpactRows = 2;
		public const int FlashImpactFrames = FlashImpactColumns * FlashImpactRows;
		public const int HitJaggedColumns = 1;
		public const int HitJaggedRows = 2;
		public const int HitJaggedFrames = HitJaggedColumns * HitJaggedRows;

		private static Asset<Texture2D> _softGlow;
		private static Asset<Texture2D> _lightShot;
		private static Asset<Texture2D> _lightBeam;
		private static Asset<Texture2D> _cyclone;
		private static Asset<Texture2D> _fire;
		private static Asset<Texture2D> _tearFlame;
		private static Asset<Texture2D> _flashImpact;
		private static Asset<Texture2D> _hitJagged;
		private static Asset<Texture2D> _fog;
		private static Asset<Texture2D> _diffusionCircle;
		private static Asset<Texture2D> _thunderTrail;

		public static Texture2D SoftGlow => Ensure(ref _softGlow, "SoftGlow");
		public static Texture2D LightShot => Ensure(ref _lightShot, "LightShot");
		public static Texture2D LightBeam => Ensure(ref _lightBeam, "LightBeam");
		public static Texture2D Cyclone => Ensure(ref _cyclone, "Cyclone");
		public static Texture2D Fire => Ensure(ref _fire, "Fire");
		public static Texture2D TearFlame => Ensure(ref _tearFlame, "TearFlame01");
		public static Texture2D FlashImpact => Ensure(ref _flashImpact, "Flashimpact");
		public static Texture2D HitJagged => Ensure(ref _hitJagged, "HitJagged01");
		public static Texture2D Fog => Ensure(ref _fog, "Fog");
		public static Texture2D DiffusionCircle => Ensure(ref _diffusionCircle, "DiffusionCircle");
		public static Texture2D ThunderTrail => Ensure(ref _thunderTrail, "ThunderTrail");
		public static Texture2D Extra98 => Ensure(ref _extra98, "Extra98");

		private static Asset<Texture2D> _extra98;

		private static Texture2D Ensure(ref Asset<Texture2D> asset, string name)
		{
			asset ??= ModContent.Request<Texture2D>($"PokemonHenshin/Assets/Fx/{name}", AssetRequestMode.ImmediateLoad);
			return asset.Value;
		}

		/// <summary>进入 Additive 批次；调用方负责 <see cref="EndAdditive"/>。</summary>
		public static void BeginAdditive()
		{
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
		}

		public static void EndAdditive()
		{
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
		}

		/// <summary>
		/// 整图中心对齐 Additive 绘制（仅 SoftGlow/Cyclone/Fog/DiffusionCircle/LightShot/LightBeam/TearFlame）。
		/// Fire / FlashImpact / HitJagged 请用 Sheet API。
		/// </summary>
		public static void DrawAdditiveCentered(Texture2D tex, Vector2 worldPos, Color colorWithAlpha, float scale, float rotation = 0f)
		{
			if (tex == null || colorWithAlpha.A == 0)
				return;
			Vector2 origin = tex.Size() * 0.5f;
			Main.spriteBatch.Draw(tex, worldPos - Main.screenPosition, null, colorWithAlpha, rotation, origin, scale, SpriteEffects.None, 0f);
		}

		public static void DrawAdditiveCentered(Texture2D tex, Vector2 worldPos, Color colorWithAlpha, Vector2 scale, float rotation = 0f)
		{
			if (tex == null || colorWithAlpha.A == 0)
				return;
			Vector2 origin = tex.Size() * 0.5f;
			Main.spriteBatch.Draw(tex, worldPos - Main.screenPosition, null, colorWithAlpha, rotation, origin, scale, SpriteEffects.None, 0f);
		}

		public static Rectangle SheetFrame(Texture2D tex, int columns, int rows, int frameIndex)
		{
			columns = Math.Max(1, columns);
			rows = Math.Max(1, rows);
			int total = columns * rows;
			int frame = ((frameIndex % total) + total) % total;
			int col = frame % columns;
			int row = frame / columns;
			int fw = Math.Max(1, tex.Width / columns);
			int fh = Math.Max(1, tex.Height / rows);
			return new Rectangle(col * fw, row * fh, fw, fh);
		}

		public static void DrawAdditiveSheet(Texture2D tex, int columns, int rows, int frameIndex, Vector2 worldPos, Color colorWithAlpha, float scale, float rotation = 0f)
		{
			DrawAdditiveSheet(tex, columns, rows, frameIndex, worldPos, colorWithAlpha, new Vector2(scale), rotation);
		}

		public static void DrawAdditiveSheet(Texture2D tex, int columns, int rows, int frameIndex, Vector2 worldPos, Color colorWithAlpha, Vector2 scale, float rotation = 0f, SpriteEffects effects = SpriteEffects.None)
		{
			if (tex == null || colorWithAlpha.A == 0)
				return;
			Rectangle src = SheetFrame(tex, columns, rows, frameIndex);
			Vector2 origin = new Vector2(src.Width * 0.5f, src.Height * 0.5f);
			Main.spriteBatch.Draw(tex, worldPos - Main.screenPosition, src, colorWithAlpha, rotation, origin, scale, effects, 0f);
		}

		public static void DrawFireFrame(Vector2 pos, Color c, float scale, float rot, int frame)
			=> DrawAdditiveSheet(Fire, FireColumns, FireRows, frame, pos, c, scale, rot);

		public static void DrawFlashImpactFrame(Vector2 pos, Color c, float scale, float rot, int frame)
			=> DrawAdditiveSheet(FlashImpact, FlashImpactColumns, FlashImpactRows, frame, pos, c, scale, rot);

		public static void DrawHitJaggedFrame(Vector2 pos, Color c, float scale, float rot, int frame, SpriteEffects effects = SpriteEffects.None)
			=> DrawAdditiveSheet(HitJagged, HitJaggedColumns, HitJaggedRows, frame, pos, c, new Vector2(scale), rot, effects);

		/// <summary>
		/// 不透明实心圆 + 描边（须在 AlphaBlend 批次下调用；深紫禁止 Additive）。
		/// 用 DiffusionCircle 按世界直径缩放，fill/border 的 A 应接近 255。
		/// </summary>
		public static void DrawOpaqueDisk(Vector2 worldPos, float diameterPx, Color fillOpaque, Color borderOpaque)
		{
			Texture2D tex = DiffusionCircle;
			if (tex == null)
				return;
			float scale = ScaleForWorldDiameter(tex, diameterPx);
			float borderScale = ScaleForWorldDiameter(tex, diameterPx + 4f);
			Vector2 origin = tex.Size() * 0.5f;
			Vector2 pos = worldPos - Main.screenPosition;
			Main.spriteBatch.Draw(tex, pos, null, borderOpaque, 0f, origin, borderScale, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(tex, pos, null, fillOpaque, 0f, origin, scale, SpriteEffects.None, 0f);
		}

		/// <summary>按弹龄取帧：<c>(lifetime - timeLeft) / ticksPerFrame % totalFrames</c>。</summary>
		public static int AgeFrame(int lifetime, int timeLeft, int ticksPerFrame, int totalFrames)
		{
			ticksPerFrame = Math.Max(1, ticksPerFrame);
			totalFrames = Math.Max(1, totalFrames);
			int age = Math.Max(0, lifetime - timeLeft);
			return (age / ticksPerFrame) % totalFrames;
		}

		/// <summary>
		/// 连续光束：SoftGlow 沿路径拉长密叠。
		/// width 为世界像素厚度；SoftGlow 软边按 ~0.55 有效直径换算。
		/// MagicPixel 亦可用（须封顶尺寸）；本路径默认 SoftGlow 以免无界拉伸白屏。
		/// </summary>
		public static void DrawContinuousBeam(Vector2 from, Vector2 to, Color coreColor, Color glowColor, float coreWidth, float glowWidth)
		{
			Vector2 delta = to - from;
			float len = delta.Length();
			if (len < 1.5f || (coreColor.A == 0 && glowColor.A == 0))
				return;
			Texture2D glow = SoftGlow;
			if (glow == null)
				return;

			float rot = delta.ToRotation();
			Vector2 dir = delta / len;
			// 允许到约 3 格（48px）；SoftGlow 等比缩放不会通天白屏
			float cw = MathHelper.Clamp(coreWidth, 1f, 48f);
			float gw = MathHelper.Clamp(glowWidth, 4f, 56f);
			const float SoftFill = 0.55f; // SoftGlow 亮芯约占贴图比例
			float tex = Math.Max(1f, glow.Width);

			// Y=厚度，X=沿路径拉长，使相邻戳记重叠成实线
			float coreSy = cw / (tex * SoftFill);
			float haloSy = gw / (tex * SoftFill);
			Vector2 coreScale = new Vector2(coreSy * 3.2f, coreSy);
			Vector2 haloScale = new Vector2(haloSy * 2.8f, haloSy);
			Vector2 origin = glow.Size() * 0.5f;

			if (glowColor.A > 0)
			{
				Color halo = glowColor;
				halo.A = (byte)Math.Min(halo.A, (byte)150);
				float along = tex * haloScale.X * SoftFill;
				float spacing = Math.Max(2.5f, along * 0.28f);
				for (float d = 0f; d <= len + 0.01f; d += spacing)
				{
					Vector2 pos = from + dir * Math.Min(d, len) - Main.screenPosition;
					Main.spriteBatch.Draw(glow, pos, null, halo, rot, origin, haloScale, SpriteEffects.None, 0f);
				}
			}

			if (coreColor.A > 0)
			{
				Color core = coreColor;
				core.A = (byte)Math.Min(core.A, (byte)220);
				float along = tex * coreScale.X * SoftFill;
				float spacing = Math.Max(2f, along * 0.22f);
				for (float d = 0f; d <= len + 0.01f; d += spacing)
				{
					Vector2 pos = from + dir * Math.Min(d, len) - Main.screenPosition;
					Main.spriteBatch.Draw(glow, pos, null, core, rot, origin, coreScale, SpriteEffects.None, 0f);
				}
			}
		}

		/// <summary>水纹流动：沿光束滚动的拉长 SoftGlow 波节。</summary>
		public static void DrawWaterFlowRipples(Vector2 from, Vector2 to, Color colorWithAlpha, float width, float flow01)
		{
			if (colorWithAlpha.A == 0)
				return;
			Vector2 delta = to - from;
			float len = delta.Length();
			if (len < 8f)
				return;
			Texture2D glow = SoftGlow;
			if (glow == null)
				return;
			float rot = delta.ToRotation();
			Vector2 dir = delta / len;
			float w = MathHelper.Clamp(width, 4f, 48f);
			float tex = Math.Max(1f, glow.Width);
			float sy = w / (tex * 0.55f);
			Vector2 scale = new Vector2(sy * 2.4f, sy * 0.85f);
			Vector2 origin = glow.Size() * 0.5f;
			float spacing = Math.Max(10f, w * 0.9f);
			float phase = flow01 * spacing;
			Color c = colorWithAlpha;
			c.A = (byte)Math.Min(c.A, (byte)110);
			for (float d = -spacing + phase; d <= len; d += spacing)
			{
				if (d < 0f || d > len)
					continue;
				float pulse = 0.7f + 0.3f * MathF.Sin(flow01 * MathHelper.TwoPi * 2f + d * 0.045f);
				Vector2 pos = from + dir * d - Main.screenPosition;
				Main.spriteBatch.Draw(glow, pos, null, c * pulse, rot, origin, scale * pulse, SpriteEffects.None, 0f);
			}
		}

		/// <summary>将圆形贴图缩放到指定世界直径（像素）。</summary>
		public static float ScaleForWorldDiameter(Texture2D tex, float diameterPx)
		{
			if (tex == null || tex.Width < 1)
				return 0.01f;
			return Math.Max(0.01f, diameterPx / tex.Width);
		}

		/// <summary>
		/// 装饰链：贴图段密叠。勿用大间距画 LightShot。
		/// </summary>
		public static void DrawBeamChain(Texture2D tex, Vector2 from, Vector2 to, Color colorWithAlpha, float width, float spacing = 14f)
		{
			if (tex == null || colorWithAlpha.A == 0)
				return;
			Vector2 delta = to - from;
			float len = delta.Length();
			if (len < 1.5f)
				return;
			float rot = delta.ToRotation();
			Vector2 dir = delta / len;
			float segLen = MathHelper.Clamp(width * 1.8f, 24f, 48f);
			spacing = Math.Max(4f, Math.Min(spacing, segLen * 0.35f));
			Vector2 size = tex.Size();
			if (size.X < 1f) size.X = 1f;
			if (size.Y < 1f) size.Y = 1f;
			float maxW = MathHelper.Clamp(width, 1f, 48f);
			Vector2 scale = new Vector2(segLen / size.X, Math.Max(0.04f, maxW / size.Y));
			Vector2 origin = size * 0.5f;
			for (float d = 0f; d <= len + 0.01f; d += spacing)
			{
				Vector2 pos = from + dir * Math.Min(d, len) - Main.screenPosition;
				Main.spriteBatch.Draw(tex, pos, null, colorWithAlpha, rot, origin, scale, SpriteEffects.None, 0f);
			}
		}

		/// <summary>短距可贴图拉伸；过长走 SoftGlow 连续束（或可控 MagicPixel）。</summary>
		public static void DrawBeamSegment(Texture2D tex, Vector2 from, Vector2 to, Color colorWithAlpha, float width)
		{
			if (colorWithAlpha.A == 0)
				return;
			Vector2 delta = to - from;
			float len = delta.Length();
			if (len < 1.5f)
				return;
			float w = MathHelper.Clamp(width, 1f, 48f);
			if (len > 96f)
			{
				Color glow = new Color(colorWithAlpha.R, colorWithAlpha.G, colorWithAlpha.B, (byte)(colorWithAlpha.A * 0.55f));
				DrawContinuousBeam(from, to, colorWithAlpha, glow, w * 0.4f, w);
				return;
			}
			if (tex == null)
			{
				DrawContinuousBeam(from, to, colorWithAlpha, colorWithAlpha, w * 0.4f, w);
				return;
			}
			float rot = delta.ToRotation();
			Vector2 mid = (from + to) * 0.5f - Main.screenPosition;
			Vector2 size = tex.Size();
			if (size.X < 1f) size.X = 1f;
			if (size.Y < 1f) size.Y = 1f;
			Main.spriteBatch.Draw(tex, mid, null, colorWithAlpha, rot, size * 0.5f,
				new Vector2(len / size.X, Math.Max(0.04f, w / size.Y)), SpriteEffects.None, 0f);
		}

		public static void DrawCyclone(Vector2 worldPos, Color colorWithAlpha, float scale, float rotation)
		{
			DrawAdditiveCentered(Cyclone, worldPos, colorWithAlpha, scale, rotation);
		}

		/// <summary>带 Alpha 的颜色：rgb × intensity，A = 255×intensity（禁止假 Additive）。</summary>
		public static Color WithAlpha(Color rgb, float intensity)
		{
			intensity = MathHelper.Clamp(intensity, 0f, 1f);
			Color c = rgb * intensity;
			c.A = (byte)(255f * intensity);
			return c;
		}
	}
}
