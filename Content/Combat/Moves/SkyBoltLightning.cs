using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 天雷视觉：只读参考 CWR <c>PRT_SkyBolt</c> 路径/包络/宽度；
	/// 贴图为本模 <c>Assets/Fx/ThunderTrail</c> + <c>SoftGlow</c>（拷贝自大修，无运行时依赖）。
	/// 注意：Additive 下勿把 Color.A 置 0（SourceAlpha 会全透明 →「有伤无光」）。
	/// ai0=0 向前一道；ai0=1 落雷；ai1 宽度倍率；ai2≥1 不挂连锁。
	/// </summary>
	public class SkyBoltLightningProj : HenshinMoveProj
	{
		public const float MaxBeamLength = 64f * 16f;
		public const int PathPoints = 12;
		public const int Life = 26;
		private const float BaseWidth = 34f;

		private static readonly Color ChantColor = new(150, 190, 255);
		private static readonly Color VoltWhite = new(226, 240, 255);

		private static Asset<Texture2D> _trailAsset;
		private static Asset<Texture2D> _glowAsset;
		private static Asset<Texture2D> _shotAsset;

		private Vector2 _from;
		private Vector2 _to;
		private Vector2[] _pts;
		private float _envelope = 1f;
		private float _widthMul = 1f;
		private bool _chainScheduled;
		private bool _inited;
		private int _flickSeed;

		public override string Texture => "PokemonHenshin/Assets/Fx/ThunderTrail";

		public override void SetStaticDefaults()
		{
			_trailAsset = ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ThunderTrail", AssetRequestMode.ImmediateLoad);
			_glowAsset = ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SoftGlow", AssetRequestMode.ImmediateLoad);
			_shotAsset = ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/LightShot", AssetRequestMode.ImmediateLoad);
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			bool fallMode = Projectile.ai[0] > 0.5f;
			_widthMul = Projectile.ai[1] > 0.1f ? Projectile.ai[1] : (fallMode ? 1.85f : 1f);

			if (!_inited)
			{
				_inited = true;
				_flickSeed = Projectile.whoAmI * 17;
				EnsureAssets();
				if (fallMode)
				{
					_to = Projectile.Center;
					_from = _to - Vector2.UnitY * 320f;
				}
				else
				{
					_from = owner.MountedCenter;
					if (Projectile.ai[2] > 0.5f && Projectile.velocity.LengthSquared() > 1f)
					{
						float len = MathHelper.Clamp(Projectile.velocity.Length(), 64f, MaxBeamLength);
						_to = _from + Vector2.Normalize(Projectile.velocity) * len;
					}
					else
					{
						Vector2 aim = Main.MouseWorld;
						Vector2 dir = aim - _from;
						if (dir.LengthSquared() < 1f)
							dir = new Vector2(owner.direction, 0f);
						float len = MathHelper.Clamp(dir.Length(), 64f, MaxBeamLength);
						_to = _from + Vector2.Normalize(dir) * len;
					}
				}
				BuildJaggedPath();
				SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = fallMode ? -0.1f : 0.2f }, _to);
				for (int i = 0; i < 10; i++)
					Dust.NewDustPerfect(_to, DustID.Electric, Main.rand.NextVector2Circular(5f, 5f), 40, VoltWhite, 1.5f).noGravity = true;
			}

			float lifeT = 1f - Projectile.timeLeft / (float)Life;
			_envelope = lifeT < 0.2f ? 1f : 1f - MathF.Pow((lifeT - 0.2f) / 0.8f, 3f);

			if (Projectile.timeLeft % 3 == 0 && lifeT < 0.55f)
			{
				_flickSeed++;
				BuildJaggedPath();
			}

			// 沿路径电尘（对齐 CWR 落雷电花，非降级）
			if (_pts != null && Projectile.timeLeft % 2 == 0)
			{
				int idx = Main.rand.Next(_pts.Length);
				Dust.NewDustPerfect(_pts[idx], DustID.Electric, Main.rand.NextVector2Circular(2f, 2f), 60, ChantColor, 1.2f).noGravity = true;
			}

			Projectile.Center = Vector2.Lerp(_from, _to, 0.55f);
			Lighting.AddLight(_to, ChantColor.ToVector3() * _envelope * 1.2f);

			if (!fallMode && Projectile.ai[2] < 0.5f && !_chainScheduled && Projectile.owner == Main.myPlayer)
			{
				_chainScheduled = true;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<ThunderboltChainDirector>(), Projectile.damage, 0f, Projectile.owner);
				if (id >= 0)
					Main.projectile[id].originalDamage = Projectile.damage;
			}
		}

		private static void EnsureAssets()
		{
			_trailAsset ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ThunderTrail", AssetRequestMode.ImmediateLoad);
			_glowAsset ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SoftGlow", AssetRequestMode.ImmediateLoad);
			_shotAsset ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/LightShot", AssetRequestMode.ImmediateLoad);
		}

		private void BuildJaggedPath()
		{
			_pts ??= new Vector2[PathPoints];
			Vector2 dir = _to - _from;
			if (dir.LengthSquared() < 4f)
			{
				for (int i = 0; i < PathPoints; i++)
					_pts[i] = _from;
				return;
			}
			Vector2 side = Vector2.Normalize(dir).RotatedBy(MathHelper.PiOver2);
			for (int i = 0; i < PathPoints; i++)
			{
				float t = i / (float)(PathPoints - 1);
				float swayEnv = MathF.Sin(t * MathHelper.Pi);
				float seed = _flickSeed * 1.71f + i * 12.9898f + Projectile.whoAmI * 7.31f;
				float wobble = (Frac(MathF.Sin(seed) * 43758.5453f) - 0.5f) * 2f;
				_pts[i] = Vector2.Lerp(_from, _to, t) + side * (wobble * 46f * swayEnv);
			}
			_pts[0] = _from;
			_pts[^1] = _to;
		}

		private static float Frac(float x) => x - MathF.Floor(x);

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_pts == null || Projectile.timeLeft < Life / 2)
				return false;
			float _ = 0f;
			float thickness = Math.Max(14f, BaseWidth * 0.55f * _widthMul * _envelope);
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, thickness, ref _);
		}

		public override bool? CanDamage() => Projectile.timeLeft >= Life / 2 && Projectile.damage > 0 ? null : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.rand.NextFloat() < 0.5f)
				target.AddBuff(BuffID.Electrified, 300);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_pts == null || _pts.Length < 2)
				return false;

			EnsureAssets();
			Texture2D trailTex = _trailAsset.Value;
			Texture2D glowTex = _glowAsset.Value;
			Texture2D shotTex = _shotAsset.Value;
			Vector2 trailSize = trailTex.Size();
			Vector2 shotSize = shotTex.Size();
			if (trailSize.X < 1f) trailSize.X = 1f;
			if (trailSize.Y < 1f) trailSize.Y = 1f;
			if (shotSize.X < 1f) shotSize.X = 1f;
			if (shotSize.Y < 1f) shotSize.Y = 1f;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			for (int i = 0; i < _pts.Length - 1; i++)
			{
				Vector2 a = _pts[i] - Main.screenPosition;
				Vector2 b = _pts[i + 1] - Main.screenPosition;
				Vector2 seg = b - a;
				float len = seg.Length();
				if (len < 1.5f)
					continue;
				float rot = seg.ToRotation();
				Vector2 mid = (a + b) * 0.5f;
				float factor = i / (float)(_pts.Length - 1);
				float width = BaseWidth * _widthMul * (0.5f + 0.5f * (1f - factor)) * _envelope;
				float alpha = MathHelper.Clamp(_envelope * (0.55f + 0.45f * factor), 0f, 1f);

				// Additive + SourceAlpha：必须保留 A，禁止 A=0
				Color wide = Color.Lerp(ChantColor, Color.White, 0.35f) * alpha;
				wide.A = (byte)(255f * alpha);
				Color core = VoltWhite * (0.95f * alpha);
				core.A = (byte)(255f * alpha);

				Main.spriteBatch.Draw(trailTex, mid, null, wide, rot, trailSize * 0.5f,
					new Vector2(len / trailSize.X * 1.05f, width / trailSize.Y), SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(shotTex, mid, null, core, rot, shotSize * 0.5f,
					new Vector2(len / shotSize.X, Math.Max(0.04f, width / shotSize.Y * 0.22f)), SpriteEffects.None, 0f);
			}

			Color glow = ChantColor * (_envelope * 0.9f);
			glow.A = (byte)(255f * _envelope);
			Main.spriteBatch.Draw(glowTex, _to - Main.screenPosition, null, glow, 0f,
				glowTex.Size() * 0.5f, new Vector2(0.65f, 0.45f) * _envelope * (0.85f + 0.15f * _widthMul), SpriteEffects.None, 0f);
			Color glowCore = VoltWhite * (_envelope * 0.7f);
			glowCore.A = (byte)(255f * _envelope);
			Main.spriteBatch.Draw(glowTex, _to - Main.screenPosition, null, glowCore, 0f,
				glowTex.Size() * 0.5f, 0.28f * _envelope * _widthMul, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}
	}

	/// <summary>雷丘打雷：每个可见敌对头上生成一道更粗落雷。</summary>
	public class SkyBoltRainUltProj : HenshinMoveProj
	{
		public override string Texture => "PokemonHenshin/Assets/Fx/ThunderTrail";

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 6;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer || Projectile.localAI[0] > 0f)
				return;
			Projectile.localAI[0] = 1f;

			Rectangle screen = new(
				(int)(Main.screenPosition.X - 64),
				(int)(Main.screenPosition.Y - 64),
				Main.screenWidth + 128,
				Main.screenHeight + 128);

			int count = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				if (!screen.Intersects(n.Hitbox))
					continue;

				Vector2 strikeTo = n.Top + new Vector2(0f, 4f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), strikeTo, Vector2.Zero,
					ModContent.ProjectileType<SkyBoltLightningProj>(), Projectile.damage, 4f, Projectile.owner,
					1f, 1.85f, 1f);

				count++;
				if (count >= 20)
					break;
			}

			if (count == 0)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.MouseWorld, Vector2.Zero,
					ModContent.ProjectileType<SkyBoltLightningProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
					1f, 1.85f, 1f);
			}
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}
}
