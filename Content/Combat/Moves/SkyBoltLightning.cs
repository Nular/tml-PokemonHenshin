using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
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
	/// ai0=0 向前一道；ai0=1 落雷；ai1 宽度倍率，或前进模式下 ≥64 时表示最大长度（像素）；
	/// ai2：&lt;0.5 可挂感电连锁；≥0.5 不挂连锁；≥1.5 为分叉/二次弹（以生成点为起点，且不再分叉）。
	/// </summary>
	public class SkyBoltLightningProj : HenshinMoveProj
	{
		public const float DefaultMaxBeamLength = 64f * 16f;
		public const float AiNoChain = 1f;
		public const float AiNoChainNoFork = 2f;
		public const float ForkSearchRange = 16f * 16f;
		public const int MaxForks = 3;
		public const int PathPoints = 12;
		public const int Life = 26;
		private const float BaseWidth = 34f;

		private static readonly Color ChantColor = HenshinFxDraw.SparkGold;
		private static readonly Color VoltWhite = HenshinFxDraw.SparkGoldCore;

		private static Asset<Texture2D> _trailAsset;
		private static Asset<Texture2D> _glowAsset;
		private static Asset<Texture2D> _shotAsset;

		private Vector2 _from;
		private Vector2 _to;
		private Vector2[] _pts;
		private float _envelope = 1f;
		private float _widthMul = 1f;
		private float _maxBeamLength = DefaultMaxBeamLength;
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

		private bool IsFallMode => Projectile.ai[0] > 0.5f;
		private bool AllowsChain => Projectile.ai[2] < 0.5f;
		private bool AllowsFork => !IsFallMode && Projectile.ai[2] < 1.5f;
		private bool UseSpawnAsOrigin => Projectile.ai[2] >= 1.5f;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			bool fallMode = IsFallMode;

			if (!_inited)
			{
				_inited = true;
				_flickSeed = Projectile.whoAmI * 17;
				EnsureAssets();
				if (!fallMode && Projectile.ai[1] >= 64f)
				{
					_maxBeamLength = Projectile.ai[1];
					_widthMul = 1f;
				}
				else
					_widthMul = Projectile.ai[1] > 0.1f ? Projectile.ai[1] : (fallMode ? 1.85f : 1f);

				if (fallMode)
				{
					_to = Projectile.Center;
					_from = _to - Vector2.UnitY * 320f;
				}
				else if (UseSpawnAsOrigin)
				{
					_from = Projectile.Center;
					Vector2 delta = Projectile.velocity;
					if (delta.LengthSquared() < 1f)
						delta = new Vector2(owner.direction * 64f, 0f);
					float len = MathHelper.Clamp(delta.Length(), 48f, Math.Max(ForkSearchRange, _maxBeamLength));
					_to = _from + Vector2.Normalize(delta) * len;
				}
				else
				{
					_from = owner.MountedCenter;
					if (Projectile.ai[2] > 0.5f && Projectile.velocity.LengthSquared() > 1f)
					{
						float len = MathHelper.Clamp(Projectile.velocity.Length(), 64f, _maxBeamLength);
						_to = _from + Vector2.Normalize(Projectile.velocity) * len;
					}
					else
					{
						Vector2 aim = ResolveAimPoint(owner);
						Vector2 dir = aim - _from;
						if (dir.LengthSquared() < 1f)
							dir = new Vector2(owner.direction, 0f);
						float len = MathHelper.Clamp(dir.Length(), 64f, _maxBeamLength);
						_to = _from + Vector2.Normalize(dir) * len;
					}
				}
				BuildJaggedPath();
				// 落点：电击命中 + 小爆（本端节流：雷暴 20 道 / 分叉 3 道不叠成一团）；旧 Item122 + 10 颗金尘删去。
				// 主雷鸣在 SkyBoltRainUltProj 首帧放。施放音不 claim：技能保留物品 UseSound，皮卡丘大招保留共享 UltCast。
				if (fallMode)
					HenshinSfx.PlayCooldown(SfxKey.BlastL, _to, 30, 1f, -0.2f); // 雷暴主雷鸣兜底（导演弹已放过则被冷却挡掉）
				HenshinSfx.PlayCooldown(SfxKey.ElecHit, _to, fallMode ? 6 : 3);
				// 皮卡丘大招根弹（ThunderboltUlt：ai0=0、ai1=0、ai2=0，随生成包同步）落点用 L 爆 + BlastL（R4 跨组需求）。
				bool ultRoot = !fallMode && Projectile.ai[1] <= 0f && AllowsChain;
				if (ultRoot)
				{
					HenshinSfx.PlayCooldown(SfxKey.BlastL, _to, 30, 1f, -0.1f);
					HenshinExplosion.Spawn(_to, BlastSize.L, ChantColor, 220f, kenneyFrame: false);
				}
				else
				{
					HenshinSfx.PlayCooldown(SfxKey.BlastS, _to, fallMode ? 6 : 4, UseSpawnAsOrigin ? 0.4f : 0.6f, 0.1f);
					HenshinExplosion.Spawn(_to, fallMode ? BlastSize.M : BlastSize.S, ChantColor, fallMode ? 120f : 64f, kenneyFrame: false);
				}
			}

			float lifeT = 1f - Projectile.timeLeft / (float)Life;
			_envelope = lifeT < 0.2f ? 1f : 1f - MathF.Pow((lifeT - 0.2f) / 0.8f, 3f);

			if (Projectile.timeLeft % 3 == 0 && lifeT < 0.55f)
			{
				_flickSeed++;
				BuildJaggedPath();
			}

			// 沿雷撒的金尘（每 2 tick 一颗）删去：雷贴图 + 落点爆炸配方已足够。
			Projectile.Center = Vector2.Lerp(_from, _to, 0.55f);
			// 大招档（皮卡丘十万伏特根弹 / 分叉、雷丘雷暴落雷）：落点光 1.2 → 1.4，落点外缘少量电粒子（Electric 自带光）。
			// 雷暴导演只活 6 tick、落雷活 26 tick：由落雷按 whoAmI 抽样发（根弹 N=1、落雷 N=3、分叉/连锁 N=4）。
			// _to 各端在首帧由同步的生成位置 / ai / 玩家位置 / 同步鼠标算出，不是 owner 私有写入。
			bool ult = HenshinHitFeel.TierOf(Projectile, HitTier.Light) == HitTier.Ultimate;
			Lighting.AddLight(_to, ChantColor.ToVector3() * _envelope * (ult ? 1.4f : 1.2f));
			if (ult && !Main.dedServ && _envelope > 0.3f)
			{
				bool ultRootBolt = !fallMode && Projectile.ai[1] <= 0f && AllowsChain;
				int n = ultRootBolt ? 1 : (fallMode ? 3 : 4);
				HenshinUltGlow.Emit(_to, Projectile.owner, Projectile.whoAmI, 26f * _widthMul, DustID.Electric, Vector3.Zero, sampleN: n);
			}

			if (AllowsChain && !_chainScheduled && Projectile.owner == Main.myPlayer)
			{
				_chainScheduled = true;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<ThunderboltChainDirector>(), Projectile.damage, 0f, Projectile.owner);
				if (id >= 0)
					Main.projectile[id].originalDamage = Projectile.damage;
			}
		}

		private Vector2 ResolveAimPoint(Player owner)
		{
			Vector2 cursor = HenshinProjUtil.OwnerMouseWorld(Projectile);
			HenshinPlayer hp = owner.GetModPlayer<HenshinPlayer>();
			float softTiles = hp.ThunderboltSoftAimTiles;
			if (softTiles <= 0f)
				return cursor;

			float range = softTiles * 16f;
			float rangeSq = range * range;
			NPC best = null;
			float bestSq = rangeSq;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float dSq = Vector2.DistanceSquared(cursor, n.Center);
				if (dSq >= bestSq)
					continue;
				bestSq = dSq;
				best = n;
			}
			return best != null ? best.Center : cursor;
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

			// 出自大招槽（皮卡丘大招 / 雷丘雷暴）→ Ultimate；雷丘技能 → Light。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), ChantColor, target.Center);
			TrySpawnForks(target);
		}

		private void TrySpawnForks(NPC hitTarget)
		{
			if (!AllowsFork || Projectile.owner != Main.myPlayer || hitTarget == null || !hitTarget.active)
				return;

			int dmg = Projectile.damage > 0 ? Projectile.damage : Projectile.originalDamage;
			if (dmg <= 0)
				dmg = 1;

			Span<(int Who, float DistSq)> candidates = stackalloc (int, float)[MaxForks];
			int filled = 0;
			float rangeSq = ForkSearchRange * ForkSearchRange;
			Vector2 origin = hitTarget.Center;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.whoAmI == hitTarget.whoAmI || !n.CanBeChasedBy())
					continue;
				float dSq = Vector2.DistanceSquared(origin, n.Center);
				if (dSq > rangeSq || dSq < 16f)
					continue;

				if (filled < MaxForks)
				{
					candidates[filled++] = (n.whoAmI, dSq);
					continue;
				}

				int worst = 0;
				for (int k = 1; k < MaxForks; k++)
				{
					if (candidates[k].DistSq > candidates[worst].DistSq)
						worst = k;
				}
				if (dSq < candidates[worst].DistSq)
					candidates[worst] = (n.whoAmI, dSq);
			}

			for (int i = 0; i < filled; i++)
			{
				NPC n = Main.npc[candidates[i].Who];
				if (!n.active)
					continue;
				Vector2 toNpc = n.Center - origin;
				if (toNpc.LengthSquared() < 1f)
					continue;
				float dist = MathHelper.Clamp(toNpc.Length(), 48f, ForkSearchRange);
				Vector2 vel = Vector2.Normalize(toNpc) * dist;
				int id = Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					origin,
					vel,
					ModContent.ProjectileType<SkyBoltLightningProj>(),
					dmg,
					1f,
					Projectile.owner,
					0f,
					0f,
					AiNoChainNoFork);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = dmg;
					Main.projectile[id].Center = origin;
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
				}
			}
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

				Color wide = Color.Lerp(ChantColor, VoltWhite, 0.45f) * alpha;
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
			// 各端首帧：一声压住全场的主雷鸣（大爆 + 低音高电击）+ 施法者身边 L 档金白闪光（当"天亮了一下"）；主人端大招震屏。
			// 放在主人门之前；localAI[1] 防重（localAI[0] 归主人生成逻辑）。共享 UltCast 照常（根弹旗）。
			if (Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				Player caster = Main.player[Projectile.owner];
				Vector2 at = caster.active ? caster.MountedCenter : Projectile.Center;
				// 用 30 tick 冷却：第一道落雷也会补这一声（见 SkyBoltLightningProj），这里先放到就挡掉那边。
				HenshinSfx.PlayCooldown(SfxKey.BlastL, at, 30, 1f, -0.2f);
				HenshinSfx.Play(SfxKey.ElecHit, at, 1f, -0.5f);
				HenshinExplosion.Spawn(at - Vector2.UnitY * 160f, BlastSize.L, HenshinFxDraw.SparkGold, 520f, kenneyFrame: false);
				HenshinHitFeel.Shake(Projectile.owner, at, HitTier.Ultimate, Vector2.UnitY);
			}
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
					1f, 1.85f, SkyBoltLightningProj.AiNoChainNoFork);

				count++;
				if (count >= 20)
					break;
			}

			if (count == 0)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), HenshinProjUtil.OwnerMouseWorld(Projectile), Vector2.Zero,
					ModContent.ProjectileType<SkyBoltLightningProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
					1f, 1.85f, SkyBoltLightningProj.AiNoChainNoFork);
			}
			// R4 复查 M1：不再首帧 Kill，活满 timeLeft=6（无伤、不画），远端才收得到根弹、播共享 UltCast。
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}
}
