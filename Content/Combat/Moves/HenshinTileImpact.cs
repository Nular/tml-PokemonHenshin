using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace PokemonHenshin.Content.Combat.Moves
{
	internal enum TileImpactKind
	{
		Electric,
		Water,
		Mud,
		Seed,
		Psychic,
		Fire,
		Wind,
		DragonFire,
		Count
	}

	/// <summary>
	/// 撞墙反馈：材质碎屑（<see cref="Collision.HitTiles"/>）+ 属性尘沿法线喷 + 按 kind 节流的原版音效。
	/// 纯本地表现（各端各自模拟碰撞），不生成射弹、不发包。只挂 OnTileCollide / 线截断点，勿挂 OnKill。
	/// 撞墙须比同招命中弱一档；禁止每发用 FlashImpact / DiffusionCircle 大图。
	/// </summary>
	internal static class HenshinTileImpact
	{
		private static readonly uint[] LastSound = new uint[(int)TileImpactKind.Count];
		private static readonly Color PsychicTint = new(230, 140, 255);
		private static readonly Color WindTint = new(150, 175, 225);
		private static readonly Color DragonTint = new(220, 120, 255);

		public static void FromCollide(Projectile proj, Vector2 oldVelocity, TileImpactKind kind, float scale = 1f, bool sound = true)
		{
			if (Main.dedServ)
				return;
			Vector2 normal = NormalFromCollide(proj.velocity, oldVelocity);
			Vector2 half = new(proj.width * 0.5f, proj.height * 0.5f);
			Vector2 pos = proj.Center - normal * half;
			Collision.HitTiles(proj.position, oldVelocity, proj.width, proj.height);
			Emit(pos, normal, kind, scale, sound);
		}

		public static void At(Vector2 pos, Vector2 normal, TileImpactKind kind, float scale = 1f, bool sound = true, bool material = true)
		{
			if (Main.dedServ)
				return;
			if (normal.LengthSquared() < 0.0001f)
				normal = -Vector2.UnitY;
			else
				normal.Normalize();
			if (material)
				Collision.HitTiles(pos - new Vector2(6f) - normal * 6f, -normal * 8f, 12, 12);
			Emit(pos, normal, kind, scale, sound);
		}

		/// <summary>贴地滚动时脚下卷起材质碎屑（不出属性尘、不出声）。</summary>
		public static void GroundDebris(Projectile proj)
		{
			if (Main.dedServ)
				return;
			Collision.HitTiles(new Vector2(proj.position.X, proj.position.Y + proj.height - 4f), new Vector2(proj.velocity.X * 0.3f, 6f), proj.width, 8);
		}

		/// <summary>X 被碰撞清零 → 侧墙；Y 被清零 → 地面/天花板；都没变则用来向取反。</summary>
		public static Vector2 NormalFromCollide(Vector2 velocity, Vector2 oldVelocity)
		{
			Vector2 n = Vector2.Zero;
			if (Math.Abs(velocity.X - oldVelocity.X) > 0.01f && Math.Abs(oldVelocity.X) > 0.01f)
				n.X = -Math.Sign(oldVelocity.X);
			if (Math.Abs(velocity.Y - oldVelocity.Y) > 0.01f && Math.Abs(oldVelocity.Y) > 0.01f)
				n.Y = -Math.Sign(oldVelocity.Y);
			if (n == Vector2.Zero)
				n = oldVelocity.LengthSquared() > 0.01f ? -Vector2.Normalize(oldVelocity) : -Vector2.UnitY;
			return Vector2.Normalize(n);
		}

		private static bool SoundReady(TileImpactKind kind, int minTicks)
		{
			uint now = Main.GameUpdateCount;
			ref uint last = ref LastSound[(int)kind];
			if (last != 0 && now - last < (uint)minTicks)
				return false;
			last = now == 0 ? 1u : now;
			return true;
		}

		private static void Play(TileImpactKind kind, int minTicks, SoundStyle style, Vector2 pos)
		{
			if (SoundReady(kind, minTicks))
				SoundEngine.PlaySound(style, pos);
		}

		private static float Jitter(float spread = 0.12f) => Main.rand.NextFloat(-spread, spread);

		/// <summary>法线方向 ±halfAngle 的随机速度。</summary>
		private static Vector2 Spray(Vector2 normal, float halfAngle, float minSpeed, float maxSpeed)
			=> normal.RotatedBy(Main.rand.NextFloat(-halfAngle, halfAngle)) * Main.rand.NextFloat(minSpeed, maxSpeed);

		private static int N(int baseCount, float scale) => Math.Max(1, (int)MathF.Round(baseCount * scale));

		private static void Emit(Vector2 pos, Vector2 normal, TileImpactKind kind, float scale, bool sound)
		{
			switch (kind)
			{
				case TileImpactKind.Electric:
				{
					for (int i = 0; i < N(8, scale); i++)
					{
						Dust d = Dust.NewDustPerfect(pos, DustID.Electric, Spray(normal, 1.3f, 2.5f, 5f) * scale, 0, default, 0.8f);
						d.noGravity = true;
					}
					for (int i = 0; i < N(6, scale); i++)
					{
						Dust d = Dust.NewDustPerfect(pos, DustID.YellowTorch, Spray(normal, 1.1f, 1.5f, 3.5f), 60, HenshinFxDraw.SparkGold, 1.3f);
						d.noGravity = true;
					}
					Lighting.AddLight(pos, HenshinFxDraw.SparkGold.ToVector3() * 0.9f * scale);
					if (sound)
						Play(kind, 4, SoundID.Item93 with { Volume = 0.45f, Pitch = 0.3f + Jitter() }, pos);
					break;
				}
				case TileImpactKind.Water:
				{
					for (int i = 0; i < N(10, scale); i++)
					{
						Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(4f, 4f), DustID.Water, Spray(normal, 1.0f, 1.5f, 4f) * scale, 60, default, 1.3f);
						d.noGravity = false;
					}
					Lighting.AddLight(pos, 0.15f * scale, 0.35f * scale, 0.6f * scale);
					if (sound)
						Play(kind, 3, SoundID.SplashWeak with { Volume = 0.5f, Pitch = Jitter(0.2f) }, pos);
					break;
				}
				case TileImpactKind.Mud:
				{
					for (int i = 0; i < N(10, scale); i++)
						Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(4f, 4f), DustID.Mud, Spray(normal, 1.1f, 1f, 3.5f) * scale, 0, default, 1.2f);
					for (int i = 0; i < N(4, scale); i++)
						Dust.NewDustPerfect(pos, DustID.Dirt, Spray(normal, 1.2f, 1f, 2.5f), 0, default, 1.0f);
					if (sound && SoundReady(kind, 3))
					{
						SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f, Pitch = -0.2f + Jitter() }, pos);
						SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.3f, Pitch = -0.6f }, pos);
					}
					break;
				}
				case TileImpactKind.Seed:
				{
					for (int i = 0; i < N(5, scale); i++)
						Dust.NewDustPerfect(pos, DustID.Grass, Spray(normal, 1.2f, 1f, 3f) * scale, 0, default, 1.0f);
					if (sound)
						Play(kind, 4, SoundID.Grass with { Volume = 0.4f, Pitch = 0.3f + Jitter() }, pos);
					break;
				}
				case TileImpactKind.Psychic:
				{
					int ring = N(10, scale);
					for (int i = 0; i < ring; i++)
					{
						Vector2 v = (MathHelper.TwoPi * i / ring).ToRotationVector2() * 2.6f * scale + normal * 1.2f;
						Dust d = Dust.NewDustPerfect(pos, DustID.PinkTorch, v, 80, PsychicTint, 1.35f);
						d.noGravity = true;
					}
					for (int i = 0; i < N(3, scale); i++)
					{
						Dust d = Dust.NewDustPerfect(pos, DustID.CrystalPulse, Spray(normal, 1.0f, 0.5f, 2f), 0, PsychicTint, 0.9f);
						d.noGravity = true;
					}
					Lighting.AddLight(pos, PsychicTint.ToVector3() * 0.6f * scale);
					if (sound)
						Play(kind, 3, SoundID.Item9 with { Volume = 0.5f, Pitch = 0.25f + Jitter() }, pos);
					break;
				}
				case TileImpactKind.Fire:
				{
					int ring = N(18, scale);
					for (int i = 0; i < ring; i++)
					{
						Vector2 v = (MathHelper.TwoPi * i / ring).ToRotationVector2() * Main.rand.NextFloat(2.5f, 4.5f) * scale + normal * 1.5f;
						Dust d = Dust.NewDustPerfect(pos, DustID.Torch, v, 60, default, 1.6f);
						d.noGravity = true;
					}
					for (int i = 0; i < N(6, scale); i++)
						Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(8f, 8f), DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.8f, 1.8f)), 120, default, 1.3f);
					Lighting.AddLight(pos, 1.2f * scale, 0.5f * scale, 0.15f * scale);
					if (sound)
						Play(kind, 6, SoundID.Item20 with { Volume = 0.7f, Pitch = -0.2f + Jitter() }, pos);
					break;
				}
				case TileImpactKind.Wind:
				{
					Vector2 along = new(-normal.Y, normal.X);
					if (along.LengthSquared() < 0.01f)
						along = Vector2.UnitX;
					for (int i = 0; i < N(12, scale); i++)
					{
						float side = i % 2 == 0 ? 1f : -1f;
						Vector2 v = along * side * Main.rand.NextFloat(2f, 5f) * scale + normal * Main.rand.NextFloat(0.3f, 1.4f);
						Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(6f, 3f), DustID.Cloud, v, 100, WindTint, 1.35f);
						d.noGravity = true;
					}
					if (sound)
						Play(kind, 8, SoundID.DD2_BetsyWindAttack with { Volume = 0.35f * MathHelper.Clamp(scale, 0.5f, 1.3f), Pitch = Jitter(0.15f) }, pos);
					break;
				}
				case TileImpactKind.DragonFire:
				{
					for (int i = 0; i < N(2, scale); i++)
					{
						Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Spray(normal, 1.3f, 1.5f, 4f), 100, DragonTint, 1.3f);
						d.noGravity = true;
						Dust f = Dust.NewDustPerfect(pos, DustID.Torch, Spray(normal, 1.2f, 1f, 3f), 100, default, 1.15f);
						f.noGravity = true;
					}
					Lighting.AddLight(pos, 0.8f * scale, 0.35f * scale, 0.75f * scale);
					if (sound)
						Play(kind, 16, SoundID.Item20 with { Volume = 0.35f, Pitch = 0.1f + Jitter() }, pos);
					break;
				}
			}
		}
	}
}
