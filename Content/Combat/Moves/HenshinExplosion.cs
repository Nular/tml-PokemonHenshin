using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	public enum BlastSize { S, M, L }

	/// <summary>
	/// 共享爆炸配方：白热核心闪 + 冲击环 + 放射亮条（M/L）+ 第二环与 Kenney 爆炸帧（L）+ 先亮后衰的真光照。
	/// <list type="bullet">
	/// <item><see cref="Draw"/>：在 FxProj 的 PreDraw 里、<b>Additive 批次内</b>按 t=age/life 画一帧。</item>
	/// <item><see cref="Pulse"/>：在 AI 里每 tick 调用（各端都跑），t 同上。</item>
	/// <item><see cref="Spawn"/> / <see cref="SpawnLight"/>：不想新开弹幕时用，纯本地队列（画在弹幕层之上，自带光照），须在<b>各端都会跑</b>的代码里调（AI / OnKill）。</item>
	/// </list>
	/// 全部 Additive 保 Alpha（WithAlpha），暗色抬亮；贴图为运行时生成的软光 / 环（绘制路径懒建，服务器不触发），
	/// Kenney 帧走 <see cref="HenshinFxDraw.DrawKenneyExplosionFrame"/>（按帧，非整图）。不发包，不碰伤害。
	/// </summary>
	public static class HenshinExplosion
	{
		private const int MaxQueued = 64;
		/// <summary>第 1 层起爆白闪的直径上限（px）；其它层仍按真实直径。</summary>
		private const float FlashMaxDiameter = 300f;

		/// <summary>推荐持续 tick：S 12 / M 18 / L 28。</summary>
		public static int Duration(BlastSize s) => s switch { BlastSize.S => 12, BlastSize.M => 18, _ => 28 };

		/// <summary>推荐世界直径：S 56 / M 112 / L 224。</summary>
		public static float Diameter(BlastSize s) => s switch { BlastSize.S => 56f, BlastSize.M => 112f, _ => 224f };

		/// <summary>光照峰值：S 0.9 / M 1.5 / L 2.2（AddLight 乘数，&gt;1 照得更远）。</summary>
		public static float LightIntensity(BlastSize s) => s switch { BlastSize.S => 0.9f, BlastSize.M => 1.5f, _ => 2.2f };

		/// <summary>
		/// 画一帧爆炸。须在 Additive 批次内（<see cref="HenshinFxDraw.BeginAdditive"/> 之后）。
		/// </summary>
		/// <param name="t">0..1 进度（age / life）。</param>
		/// <param name="diameter">世界直径；0 = <see cref="Diameter"/>。</param>
		/// <param name="seed">放射亮条角度种子；0 = 由 center 推导（同位置各端一致）。</param>
		/// <param name="kenneyFrame">L 档是否叠 Kenney 爆炸帧（火 / 烟感）；水、电、念力类可关。</param>
		/// <param name="strength">0..1 整体亮度，乘到每层 alpha 上（Additive 下实际亮度约 strength²）。1 = 旧行为；
		/// 爆炸算 glow 例外，lit 招式（水 / 石 / 土 / 草 / 风）调用时建议 0.5。</param>
		public static void Draw(Vector2 center, BlastSize size, Color tint, float t, float diameter = 0f, float seed = 0f, bool kenneyFrame = true,
			float strength = 1f)
		{
			if (Main.dedServ || Main.instance == null || t < 0f || t >= 1f)
				return;
			strength = MathHelper.Clamp(strength, 0f, 1f);
			if (strength <= 0f)
				return;
			Color W(Color c, float k) => WithA(c, k * strength);
			float d = diameter > 0f ? diameter : Diameter(size);
			Color baseC = Lift(tint);
			Color hot = Color.Lerp(baseC, Color.White, 0.72f);
			Texture2D glow = Glow;
			Texture2D ring = Ring;
			float e = EaseOutCubic(t);
			float fade = 1f - t;

			// 1. 起爆白闪：前 22% 迅速熄灭，大而软，给"砰"的一下。
			if (t < 0.22f)
			{
				float f = 1f - t / 0.22f;
				// R4 复查建议 5：大直径（风场 563 / 打雷 520 / 过热 420）时白闪夹到 300px，免得盖半屏。
				DrawTex(glow, center, W(hot, 0.95f * f), Math.Min(d * 1.3f, FlashMaxDiameter));
			}

			// 2. 外热晕：主体颜色，随扩散略涨。
			DrawTex(glow, center, W(baseC, 0.8f * MathF.Pow(fade, 0.9f)), d * (0.7f + 0.45f * e));

			// 3. 白热核心：先最亮、半程熄灭；最里再叠一层纯白小芯。
			DrawTex(glow, center, W(hot, MathF.Pow(fade, 1.4f)), d * 0.5f * (1f - 0.4f * t));
			if (t < 0.5f)
			{
				float f = 1f - t / 0.5f;
				DrawTex(glow, center, W(Color.White, f), d * 0.22f);
			}

			// 4. 冲击环：亮边 + 内侧淡尾；先快后慢扩到直径 d。
			float r1 = 0.2f + 0.8f * e;
			float ringA = size == BlastSize.S ? 0.7f : 0.95f;
			DrawTex(ring, center, W(Color.Lerp(baseC, Color.White, 0.45f), ringA * MathF.Pow(fade, 0.65f)), d * r1);

			// 5. 放射亮条（M 6 / L 10）：前 55% 向外冲出后熄灭。
			int rays = size == BlastSize.S ? 0 : size == BlastSize.M ? 6 : 10;
			if (rays > 0 && t < 0.55f)
			{
				float rt = t / 0.55f;
				float ra = MathF.Pow(1f - rt, 0.9f) * 0.9f;
				float s0 = seed != 0f ? seed : (center.X * 0.0137f + center.Y * 0.0291f);
				float tip = d * 0.5f * (0.35f + 0.85f * EaseOutCubic(rt));
				float root = d * 0.08f + tip * 0.35f;
				float w = d * (size == BlastSize.L ? 0.04f : 0.05f);
				for (int i = 0; i < rays; i++)
				{
					float ang = s0 + i * MathHelper.TwoPi / rays + 0.35f * MathF.Sin(s0 * 3.1f + i * 1.7f);
					float lenMul = 0.75f + 0.25f * (0.5f + 0.5f * MathF.Sin(s0 * 5.3f + i * 2.3f));
					float tipI = tip * lenMul;
					Vector2 dir = ang.ToRotationVector2();
					Vector2 mid = center + dir * (root + tipI) * 0.5f;
					float len = Math.Max(2f, tipI - root);
					DrawStretched(glow, mid, W(hot, ra), len * 1.6f, w, ang);
				}
			}

			if (size != BlastSize.L)
				return;

			// 6. L：Kenney 爆炸帧（火 / 烟体积感）。
			if (kenneyFrame)
			{
				int frame = Math.Clamp((int)(t * HenshinFxDraw.KenneyExplosionFrames), 0, HenshinFxDraw.KenneyExplosionFrames - 1);
				HenshinFxDraw.DrawKenneyExplosionFrame(center, W(Color.Lerp(baseC, Color.White, 0.2f), 0.9f * MathF.Pow(fade, 0.7f)), d * 0.8f, frame);
			}

			// 7. L：第二环，晚 18% 出发、更宽更慢。
			float t2 = (t - 0.18f) / 0.82f;
			if (t2 > 0f)
			{
				float r2 = 0.15f + 0.85f * EaseOutCubic(t2);
				DrawTex(ring, center, W(baseC, 0.75f * MathF.Pow(1f - t2, 0.8f)), d * 1.15f * r2);
			}
		}

		/// <summary>
		/// 光照脉冲：前 10% 满亮，之后 (1-t)^2 衰减。M/L 额外在环上放 4/6 个光点，让光跟着环往外走。
		/// 在 AI 里每 tick 调（各端），t = age / life。
		/// </summary>
		public static void Pulse(Vector2 center, Color tint, BlastSize size, float t, float diameter = 0f, float scale = 1f)
		{
			if (Main.dedServ || t < 0f || t >= 1f)
				return;
			float curve = t < 0.1f ? 1f : MathF.Pow(1f - (t - 0.1f) / 0.9f, 2f);
			Vector3 rgb = Lift(tint).ToVector3() * LightIntensity(size) * curve * scale;
			Lighting.AddLight(center, rgb);
			int ringPts = size == BlastSize.S ? 0 : size == BlastSize.M ? 4 : 6;
			if (ringPts == 0)
				return;
			float d = diameter > 0f ? diameter : Diameter(size);
			float r = d * 0.5f * (0.2f + 0.8f * EaseOutCubic(t));
			for (int i = 0; i < ringPts; i++)
				Lighting.AddLight(center + (i * MathHelper.TwoPi / ringPts).ToRotationVector2() * r, rgb * 0.45f);
		}

		/// <summary>本地队列爆炸（画 + 光照，持续 <see cref="Duration"/>）。须在各端都跑的代码里调；服务器直接返回。
		/// <paramref name="strength"/> 同时乘画面亮度（见 <see cref="Draw"/>）和光强；1 = 旧行为，lit 招式建议 0.5。</summary>
		public static void Spawn(Vector2 center, BlastSize size, Color tint, float diameter = 0f, bool kenneyFrame = true, float strength = 1f)
			=> Enqueue(center, size, tint, diameter, strength, draw: true, kenneyFrame, strength);

		/// <summary>本地队列光照（不画），用于 OnKill 等只亮 1 tick 的旧写法。scale 乘光强。</summary>
		public static void SpawnLight(Vector2 center, Color tint, BlastSize size, float scale = 1f)
			=> Enqueue(center, size, tint, 0f, scale, draw: false, kenneyFrame: false, 1f);

		/// <summary>暗色抬亮：最高通道 &lt; 220 时等比拉到 220。</summary>
		public static Color Lift(Color c)
		{
			int max = Math.Max(c.R, Math.Max(c.G, c.B));
			if (max <= 0)
				return new Color(255, 255, 255, 255);
			if (max >= 220)
				return new Color(c.R, c.G, c.B, (byte)255);
			float k = 220f / max;
			return new Color((int)Math.Min(255, c.R * k), (int)Math.Min(255, c.G * k), (int)Math.Min(255, c.B * k), 255);
		}

		// —— 队列 ——

		private struct Blast
		{
			public Vector2 Center;
			public BlastSize Size;
			public Color Tint;
			public float Diameter;
			public float LightScale;
			public int Age;
			public int Life;
			public bool Draw;
			public bool Kenney;
			public float Seed;
			public float Strength;
		}

		private static readonly List<Blast> Queue = new();

		private static void Enqueue(Vector2 center, BlastSize size, Color tint, float diameter, float lightScale, bool draw, bool kenneyFrame, float strength)
		{
			if (Main.dedServ)
				return;
			if (Queue.Count >= MaxQueued)
				Queue.RemoveAt(0);
			Queue.Add(new Blast
			{
				Center = center,
				Size = size,
				Tint = tint,
				Diameter = diameter,
				LightScale = lightScale,
				Life = draw ? Duration(size) : Math.Max(6, Duration(size) / 2),
				Draw = draw,
				Kenney = kenneyFrame,
				Seed = Main.rand.NextFloat(0.1f, MathHelper.TwoPi),
				Strength = strength
			});
		}

		internal static void UpdateQueue()
		{
			for (int i = Queue.Count - 1; i >= 0; i--)
			{
				Blast b = Queue[i];
				Pulse(b.Center, b.Tint, b.Size, b.Age / (float)b.Life, b.Diameter, b.LightScale);
				b.Age++;
				if (b.Age >= b.Life)
					Queue.RemoveAt(i);
				else
					Queue[i] = b;
			}
		}

		internal static void DrawQueue()
		{
			bool any = false;
			foreach (Blast b in Queue)
			{
				if (b.Draw)
				{
					any = true;
					break;
				}
			}
			if (!any)
				return;
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			foreach (Blast b in Queue)
			{
				if (b.Draw)
					Draw(b.Center, b.Size, b.Tint, b.Age / (float)b.Life, b.Diameter, b.Seed, b.Kenney, b.Strength);
			}
			Main.spriteBatch.End();
		}

		internal static void ClearQueue() => Queue.Clear();

		// —— 贴图（运行时生成，绘制路径懒建） ——

		private static Texture2D _glow;
		private static Texture2D _ring;

		/// <summary>128px 软光：高斯核 + 长尾，白 RGB、Alpha 为亮度。</summary>
		internal static Texture2D Glow => _glow != null && !_glow.IsDisposed ? _glow : (_glow = Make(128, r =>
			0.72f * MathF.Exp(-r * r * 9f) + 0.28f * MathF.Exp(-r * r * 3f) * (1f - r)));

		/// <summary>256px 冲击环：r≈0.9 处亮边，内侧淡尾，外缘硬收。</summary>
		internal static Texture2D Ring => _ring != null && !_ring.IsDisposed ? _ring : (_ring = Make(256, r =>
		{
			float edge = MathF.Exp(-MathF.Pow((r - 0.88f) / 0.055f, 2f));
			float inner = r < 0.88f ? 0.22f * MathF.Pow(r / 0.88f, 3f) : 0f;
			return r > 1f ? 0f : edge + inner;
		}));

		private static Texture2D Make(int size, Func<float, float> alphaOfRadius)
		{
			var data = new Color[size * size];
			float half = size * 0.5f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = (x + 0.5f - half) / half;
					float dy = (y + 0.5f - half) / half;
					float r = MathF.Sqrt(dx * dx + dy * dy);
					float a = r >= 1f ? 0f : MathHelper.Clamp(alphaOfRadius(r), 0f, 1f);
					data[y * size + x] = new Color(255, 255, 255, (byte)(a * 255f));
				}
			}
			var tex = new Texture2D(Main.instance.GraphicsDevice, size, size);
			tex.SetData(data);
			return tex;
		}

		internal static void UnloadTextures()
		{
			Texture2D g = _glow;
			Texture2D r = _ring;
			_glow = null;
			_ring = null;
			if (g != null || r != null)
				Main.QueueMainThreadAction(() => { g?.Dispose(); r?.Dispose(); });
		}

		private static Color WithA(Color c, float intensity) => HenshinFxDraw.WithAlpha(c, intensity);

		private static float EaseOutCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			float u = 1f - t;
			return 1f - u * u * u;
		}

		private static void DrawTex(Texture2D tex, Vector2 worldCenter, Color c, float worldDiameter)
		{
			if (tex == null || c.A == 0 || worldDiameter <= 0.5f)
				return;
			Main.spriteBatch.Draw(tex, worldCenter - Main.screenPosition, null, c, 0f, tex.Size() * 0.5f,
				worldDiameter / tex.Width, SpriteEffects.None, 0f);
		}

		private static void DrawStretched(Texture2D tex, Vector2 worldCenter, Color c, float worldLength, float worldWidth, float rotation)
		{
			if (tex == null || c.A == 0)
				return;
			Main.spriteBatch.Draw(tex, worldCenter - Main.screenPosition, null, c, rotation, tex.Size() * 0.5f,
				new Vector2(worldLength / tex.Width, worldWidth / tex.Height), SpriteEffects.None, 0f);
		}
	}

	/// <summary>驱动 <see cref="HenshinExplosion"/> 本地队列：PostUpdateProjectiles 推进光照，弹幕层之后加色绘制。</summary>
	internal sealed class HenshinExplosionSystem : ModSystem
	{
		public override void Load()
		{
			if (!Main.dedServ)
				Terraria.On_Main.DrawProjectiles += DrawAfterProjectiles;
		}

		public override void Unload()
		{
			if (!Main.dedServ)
				Terraria.On_Main.DrawProjectiles -= DrawAfterProjectiles;
			HenshinExplosion.ClearQueue();
			HenshinExplosion.UnloadTextures();
		}

		public override void OnWorldUnload() => HenshinExplosion.ClearQueue();

		public override void PostUpdateProjectiles()
		{
			if (!Main.dedServ)
				HenshinExplosion.UpdateQueue();
		}

		private static void DrawAfterProjectiles(Terraria.On_Main.orig_DrawProjectiles orig, Main self)
		{
			orig(self);
			HenshinExplosion.DrawQueue();
		}
	}
}
