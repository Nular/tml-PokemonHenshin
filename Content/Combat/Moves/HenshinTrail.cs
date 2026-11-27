using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 共享拖尾：读 <see cref="Projectile.oldPos"/>，零额外实体、零联机包（各端本地记录）。
	/// <list type="bullet">
	/// <item><see cref="Strip(Projectile, Color, float, Color?, float, float, float, int)"/>：Catmull-Rom 平滑后的三角带，宽度与亮度按弧长渐细，外晕 + 亮芯两层，Additive 保 Alpha。</item>
	/// <item><see cref="Afterimage(Projectile, Action{Vector2, float, float}, int, int, float)"/>：沿 oldPos 画 N 份递减残影。</item>
	/// </list>
	/// 条带贴图为运行时生成的横截面软边（不读 Fire / Flashimpact / HitJagged，不调 LoadProjectile）；只在绘制路径创建，服务器永不触发。
	/// </summary>
	public static class HenshinTrail
	{
		public const int MaxCache = 40;
		private const int MaxPoints = 192;
		private const float TeleportGap = 220f;

		private static readonly Vector2[] Raw = new Vector2[MaxCache + 1];
		private static readonly Vector2[] Pts = new Vector2[MaxPoints];
		private static readonly float[] Arc = new float[MaxPoints];
		private static readonly VertexPositionColorTexture[] Verts = new VertexPositionColorTexture[MaxPoints * 4];
		private static readonly short[] Indices = new short[MaxPoints * 12];

		private static BasicEffect _effect;
		private static Texture2D _profile;

		/// <summary>
		/// 在 SetStaticDefaults 调用：开 oldPos 缓存（TrailingMode=2，带 oldRot）。
		/// <paramref name="length"/> 按"每 tick 一个点"写；弹幕有 extraUpdates 时把同一个值传进来，缓存自动乘 (extraUpdates+1)，尾巴长度不变。上限 40。
		/// <b>钉在玩家身上的近战弹不要用</b>（oldPos 画出来的是玩家的移动轨迹）；近战画沿判定外缘的细弧。
		/// </summary>
		public static void Setup(int projType, int length = 12, int extraUpdates = 0)
		{
			ProjectileID.Sets.TrailCacheLength[projType] = Math.Clamp(length * (1 + Math.Max(0, extraUpdates)), 2, MaxCache);
			ProjectileID.Sets.TrailingMode[projType] = 2;
		}

		/// <summary>
		/// 渐细 Additive 条带。须在普通 AlphaBlend 批次内调用（PreDraw 默认批次；<b>不要</b>夹在 BeginAdditive/EndAdditive 之间），
		/// 本方法会结束当前批次、直接画三角带、再以 <see cref="HenshinFxDraw.EndAdditive"/> 同款参数重开 AlphaBlend 批次。
		/// 先画拖尾再画本体，本体会盖住条带头部。
		/// </summary>
		/// <param name="glow">外晕颜色（暗色会被抬亮）。</param>
		/// <param name="width">头部世界像素宽度（外晕）。</param>
		/// <param name="core">亮芯颜色；null = 外晕向白 60%；<see cref="Color.Transparent"/> = 不画芯。</param>
		/// <param name="coreRatio">亮芯宽度 / 外晕宽度。</param>
		/// <param name="tailWidth">尾端宽度（0 = 尖尾）。</param>
		/// <param name="opacity">整体强度 0–1（走 WithAlpha，A 同比）。</param>
		/// <param name="points">只用最近 N 个 oldPos；0 = 全部缓存。</param>
		public static void Strip(Projectile p, Color glow, float width, Color? core = null, float coreRatio = 0.38f,
			float tailWidth = 0f, float opacity = 1f, int points = 0)
		{
			if (Main.dedServ || p == null || !p.active)
				return;
			int n = Gather(p, points);
			if (n < 2)
				return;
			StripPoints(new ReadOnlySpan<Vector2>(Raw, 0, n), glow, width, core, coreRatio, tailWidth, opacity);
		}

		/// <summary>
		/// 自定义路径版（头 → 尾的世界坐标，如鞭身、回旋轨迹）。批次要求同 <see cref="Strip(Projectile, Color, float, Color?, float, float, float, int)"/>。
		/// </summary>
		public static void StripPoints(ReadOnlySpan<Vector2> worldPoints, Color glow, float width, Color? core = null,
			float coreRatio = 0.38f, float tailWidth = 0f, float opacity = 1f)
		{
			if (Main.dedServ || Main.instance == null || worldPoints.Length < 2 || opacity <= 0f || width <= 0f)
				return;
			int count = Smooth(worldPoints);
			if (count < 2 || Arc[count - 1] < 2f)
				return;

			Color glowC = Lift(glow);
			Color coreC = core ?? Color.Lerp(glowC, Color.White, 0.6f);
			opacity = MathHelper.Clamp(opacity, 0f, 1f);

			int v = 0;
			int idx = 0;
			Append(count, glowC, width, tailWidth, opacity, ref v, ref idx);
			if (coreC.A > 0 && coreRatio > 0f)
				Append(count, coreC, width * coreRatio, tailWidth * coreRatio, opacity, ref v, ref idx);
			if (idx > 0)
				Flush(v, idx);
		}

		/// <summary>
		/// 残影：从最远到最近回调 drawOne(世界中心, alpha 0–1, oldRot)。在调用方当前批次里画（Additive 或 AlphaBlend 都可，颜色乘 alpha 即可）。
		/// </summary>
		public static void Afterimage(Projectile p, Action<Vector2, float, float> drawOne, int count = 4, int step = 1, float alpha0 = 0.5f)
		{
			if (Main.dedServ || p == null || drawOne == null || count <= 0)
				return;
			step = Math.Max(1, step);
			Vector2 half = p.Size * 0.5f;
			int last = Math.Min(p.oldPos.Length, count * step);
			// 瞬移断开（与 Gather 同一阈值）：从近到远查相邻两点，超过阈值处之后（更远）的残影都不画。
			// 例：SpawnAtMouse 的弹首帧在鼠标处、AI 里才拉回玩家身边，oldPos 里会留鼠标位置。
			float gap = TeleportGapOf(p);
			Vector2 prev = p.Center;
			for (int i = 0; i < last; i++)
			{
				Vector2 op = p.oldPos[i];
				if (op == Vector2.Zero)
					continue;
				Vector2 c = op + half;
				if (Vector2.DistanceSquared(c, prev) > gap * gap)
				{
					last = i;
					break;
				}
				prev = c;
			}
			for (int i = last - 1; i >= 0; i -= 1)
			{
				if ((i + 1) % step != 0)
					continue;
				Vector2 op = p.oldPos[i];
				if (op == Vector2.Zero)
					continue;
				int k = (i + 1) / step; // 1..count
				float a = alpha0 * (1f - (k - 1f) / count);
				if (a <= 0.01f)
					continue;
				float rot = i < p.oldRot.Length ? p.oldRot[i] : p.rotation;
				drawOne(op + half, a, rot);
			}
		}

		/// <summary>残影便捷版：同一帧贴图画在 oldPos 处，color × alpha。</summary>
		public static void Afterimage(Projectile p, Texture2D tex, Rectangle? src, Vector2 origin, Color color, float scale = 1f,
			SpriteEffects effects = SpriteEffects.None, int count = 4, int step = 1, float alpha0 = 0.5f)
		{
			if (tex == null)
				return;
			Afterimage(p, (pos, a, rot) =>
				Main.spriteBatch.Draw(tex, (pos - Main.screenPosition).Floor(), src, color * a, rot, origin, scale, effects, 0f),
				count, step, alpha0);
		}

		internal static void Unload()
		{
			BasicEffect fx = _effect;
			Texture2D tex = _profile;
			_effect = null;
			_profile = null;
			if (fx != null || tex != null)
				Main.QueueMainThreadAction(() => { fx?.Dispose(); tex?.Dispose(); });
		}

		// —— 内部 ——

		private static Color Lift(Color c)
		{
			int max = Math.Max(c.R, Math.Max(c.G, c.B));
			if (max <= 0)
				return new Color(255, 255, 255, c.A);
			if (max < 220)
			{
				float k = 220f / max;
				c = new Color((int)Math.Min(255, c.R * k), (int)Math.Min(255, c.G * k), (int)Math.Min(255, c.B * k), c.A);
			}
			return c;
		}

		/// <summary>相邻两点超过"约 2.5 步 + 1 个弹身"（下限 32、上限 220）视为瞬移（SpawnAtMouse 改 Center、传送），从这里断开不画。</summary>
		private static float TeleportGapOf(Projectile p)
			=> Math.Min(TeleportGap, Math.Max(32f, p.velocity.Length() * 2.5f + Math.Max(p.width, p.height)));

		private static int Gather(Projectile p, int limit)
		{
			int n = 0;
			Vector2 half = p.Size * 0.5f;
			Raw[n++] = p.Center;
			int len = p.oldPos.Length;
			if (limit > 0)
				len = Math.Min(len, limit * (1 + Math.Max(0, p.extraUpdates)));
			float gap = TeleportGapOf(p);
			for (int i = 0; i < len && n < Raw.Length; i++)
			{
				Vector2 op = p.oldPos[i];
				if (op == Vector2.Zero)
					break;
				Vector2 c = op + half;
				float d2 = Vector2.DistanceSquared(c, Raw[n - 1]);
				if (d2 < 0.36f)
					continue; // 停顿 / 顿帧时重复点
				if (d2 > gap * gap)
					break; // 瞬移：截断
				Raw[n++] = c;
			}
			return n;
		}

		/// <summary>Catmull-Rom 细分，段长 ~5px 一点；同时写累计弧长。</summary>
		private static int Smooth(ReadOnlySpan<Vector2> src)
		{
			int n = src.Length;
			int count = 0;
			Pts[count] = src[0];
			Arc[count] = 0f;
			count++;
			for (int i = 0; i < n - 1 && count < MaxPoints; i++)
			{
				Vector2 p1 = src[i];
				Vector2 p2 = src[i + 1];
				Vector2 p0 = i > 0 ? src[i - 1] : p1 * 2f - p2;
				Vector2 p3 = i + 2 < n ? src[i + 2] : p2 * 2f - p1;
				float seg = Vector2.Distance(p1, p2);
				int sub = Math.Clamp((int)MathF.Ceiling(seg / 5f), 1, 10);
				for (int s = 1; s <= sub && count < MaxPoints; s++)
				{
					Vector2 q = Vector2.CatmullRom(p0, p1, p2, p3, s / (float)sub);
					Arc[count] = Arc[count - 1] + Vector2.Distance(Pts[count - 1], q);
					Pts[count] = q;
					count++;
				}
			}
			return count;
		}

		private static void Append(int count, Color color, float headW, float tailW, float opacity, ref int v, ref int idx)
		{
			float total = Math.Max(1f, Arc[count - 1]);
			int baseV = v;
			for (int k = 0; k < count; k++)
			{
				Vector2 tangent = Pts[Math.Max(0, k - 1)] - Pts[Math.Min(count - 1, k + 1)];
				if (tangent.LengthSquared() < 0.0001f)
					tangent = Vector2.UnitX;
				tangent.Normalize();
				Vector2 normal = new(-tangent.Y, tangent.X);

				float t = Arc[k] / total; // 0 头 → 1 尾
				float shape = MathF.Pow(1f - t, 0.7f);
				float half = (tailW + (headW - tailW) * shape) * 0.5f;
				float a = opacity * MathF.Pow(1f - t, 0.75f); // WithAlpha 在 Additive 下按平方生效，实际约 (1-t)^1.5
				Color c = HenshinFxDraw.WithAlpha(color, a);

				Vector2 s = Pts[k] - Main.screenPosition;
				Verts[v++] = new VertexPositionColorTexture(new Vector3(s + normal * half, 0f), c, new Vector2(t, 0f));
				Verts[v++] = new VertexPositionColorTexture(new Vector3(s - normal * half, 0f), c, new Vector2(t, 1f));
			}
			for (int k = 0; k < count - 1; k++)
			{
				short a0 = (short)(baseV + k * 2);
				Indices[idx++] = a0;
				Indices[idx++] = (short)(a0 + 1);
				Indices[idx++] = (short)(a0 + 2);
				Indices[idx++] = (short)(a0 + 1);
				Indices[idx++] = (short)(a0 + 3);
				Indices[idx++] = (short)(a0 + 2);
			}
		}

		private static Texture2D Profile
		{
			get
			{
				if (_profile == null || _profile.IsDisposed)
				{
					// 横截面：白色，Alpha = 平滑钟形（中心 1 → 边 0），沿长度不变。
					const int H = 64;
					var data = new Color[4 * H];
					for (int y = 0; y < H; y++)
					{
						float d = Math.Abs((y + 0.5f) / H - 0.5f) * 2f; // 0 中心 → 1 边
						float s = 1f - d;
						float bell = s * s * (3f - 2f * s);
						float a = MathHelper.Clamp(0.6f * bell + 0.4f * MathF.Exp(-d * d * 6f), 0f, 1f);
						for (int x = 0; x < 4; x++)
							data[y * 4 + x] = new Color(255, 255, 255, (byte)(a * 255f));
					}
					_profile = new Texture2D(Main.instance.GraphicsDevice, 4, H);
					_profile.SetData(data);
				}
				return _profile;
			}
		}

		private static void Flush(int vertexCount, int indexCount)
		{
			GraphicsDevice gd = Main.instance.GraphicsDevice;
			Texture2D tex = Profile;
			_effect ??= new BasicEffect(gd) { VertexColorEnabled = true, TextureEnabled = true, LightingEnabled = false };

			Main.spriteBatch.End();
			Viewport vp = gd.Viewport;
			_effect.Texture = tex;
			_effect.World = Matrix.Identity;
			_effect.View = Main.GameViewMatrix.TransformationMatrix;
			_effect.Projection = Matrix.CreateOrthographicOffCenter(0f, vp.Width, vp.Height, 0f, -1f, 1f);
			gd.BlendState = BlendState.Additive;
			gd.SamplerStates[0] = SamplerState.LinearClamp;
			gd.RasterizerState = RasterizerState.CullNone;
			gd.DepthStencilState = DepthStencilState.None;
			foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Verts, 0, vertexCount, Indices, 0, indexCount / 3);
			}
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
		}
	}

	internal sealed class HenshinTrailSystem : ModSystem
	{
		public override void Unload() => HenshinTrail.Unload();
	}
}
