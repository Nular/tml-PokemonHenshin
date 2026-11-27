using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// H6 组（三地鼠 / 圆陆鲨 / 烈咬陆鲨）共用小工具：贴图懒加载（只在 PreDraw 调用，dedServ 不会走到）、
	/// 命中/落点特效射弹生成（仅主人端生成，随射弹同步给其它端）、简易地面检测。
	/// </summary>
	public static class H6Fx
	{
		public const int KindDirt = 0, KindStone = 1, KindMeteor = 2, KindSand = 3;

		/// <summary>
		/// 爆点的声光档（ai2，随生成包同步；声音在 H6BurstFxProj 首 tick 各端播）：
		/// Light = 属性命中音；Heavy = HitHeavy（陨石 = BlastM + M 爆）；Ult = HitUlt（20 tick 一次，其余回落属性音）；
		/// Blast = BlastS + S 爆（挖洞出口）；Quiet = 只有光（入口，施放音已响）。
		/// </summary>
		public const int FxLight = 0, FxHeavy = 1, FxUlt = 2, FxBlast = 3, FxQuiet = 4;

		public static int FxOf(HitTier tier) => tier == HitTier.Ultimate ? FxUlt : tier == HitTier.Heavy ? FxHeavy : FxLight;

		/// <summary>各 kind 的光 / 爆炸色。</summary>
		public static Color Tint(int kind) => kind switch
		{
			KindStone => new Color(205, 190, 165),
			KindMeteor => new Color(255, 140, 60),
			KindSand => new Color(235, 200, 120),
			_ => new Color(210, 160, 100)
		};

		private static Asset<Texture2D> _dirt, _stone, _impact;

		public static Texture2D Dirt => (_dirt ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6DirtBurst/H6DirtBurst", AssetRequestMode.ImmediateLoad)).Value;
		public static Texture2D Stone => (_stone ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6StoneBurst/H6StoneBurst", AssetRequestMode.ImmediateLoad)).Value;
		public static Texture2D Impact => (_impact ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6MeteorImpact/H6MeteorImpact", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>只在主人端生成纯视觉爆点（伤害 0，不可命中）。kind 见常量，scale 为贴图倍率。</summary>
		public static void SpawnBurst(Projectile src, Vector2 at, int kind, float scale, int fx = FxLight)
		{
			if (src.owner != Main.myPlayer || Main.netMode == NetmodeID.Server)
				return;
			Projectile.NewProjectile(src.GetSource_FromThis(), at, Vector2.Zero,
				ModContent.ProjectileType<H6BurstFxProj>(), 0, 0f, src.owner, kind, scale, fx);
		}

		public static bool Solid(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 5))
				return false;
			Tile t = Framing.GetTileSafely(x, y);
			return t.HasTile && !t.IsActuated && Main.tileSolid[t.TileType];
		}

		/// <summary>从 startRow 起向下找第一块实心砖的顶面世界 Y；maxDown 格内找不到返回 false。</summary>
		public static bool FindGround(float worldX, int startRow, int maxDown, out float groundY)
		{
			int tx = (int)(worldX / 16f);
			for (int r = startRow; r <= startRow + maxDown; r++)
			{
				if (Solid(tx, r))
				{
					groundY = r * 16f;
					return true;
				}
			}
			groundY = 0f;
			return false;
		}

		/// <summary>受光但抬亮暗处（夜里 / 洞里也看得出）。</summary>
		public static Color Lit(Vector2 world, int floor = 110)
		{
			Color l = Lighting.GetColor((int)(world.X / 16f), (int)(world.Y / 16f));
			return new Color(Math.Max(l.R, (byte)floor), Math.Max(l.G, (byte)floor), Math.Max(l.B, (byte)floor));
		}
	}

	/// <summary>
	/// 一次性爆点（挖洞出入口 / 命中碎土 / 地震命中 / 流星落点）。ai0=kind（H6Fx.Kind*），ai1=倍率。
	/// 贴图：H6DirtBurst / H6StoneBurst（5 帧 × 3 tick，52×52 px，原点中心），H6MeteorImpact（帧数见贴图，64×64）。
	/// </summary>
	public class H6BurstFxProj : ModProjectile
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = Frames * TicksPerFrame;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = Lifetime;
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
				// FX 手感 R3：命中音 + 衰减光 / 爆炸配方都在这里（本弹随生成包同步，各端首 tick 各跑一次）。碎屑尘删掉，靠贴图。
				int kind = (int)Projectile.ai[0];
				int fx = (int)Projectile.ai[2];
				Vector2 at = Projectile.Center;
				PlaySound(kind, fx, at);
				Color tint = H6Fx.Tint(kind);
				bool big = fx == H6Fx.FxHeavy || fx == H6Fx.FxUlt;
				if (kind == H6Fx.KindMeteor)
					HenshinExplosion.Spawn(at, fx == H6Fx.FxHeavy ? BlastSize.M : BlastSize.S, tint, fx == H6Fx.FxHeavy ? 150f : 0f);
				else if (fx == H6Fx.FxBlast)
					HenshinExplosion.Spawn(at, BlastSize.S, tint, 80f);
				else
					HenshinExplosion.SpawnLight(at, tint, fx == H6Fx.FxUlt ? BlastSize.M : BlastSize.S, big ? 1f : 0.7f);
			}
		}

		private static void PlaySound(int kind, int fx, Vector2 at)
		{
			switch (fx)
			{
				case H6Fx.FxQuiet:
					return;
				case H6Fx.FxBlast:
					HenshinSfx.PlayCooldown(SfxKey.BlastS, at, 4, 0.9f, -0.2f);
					return;
				case H6Fx.FxUlt:
					if (HenshinSfx.PlayCooldown(SfxKey.HitUlt, at, HenshinHitFeel.UltimateWindowTicks))
						return;
					break;
				case H6Fx.FxHeavy:
					if (kind == H6Fx.KindMeteor)
					{
						HenshinSfx.PlayCooldown(SfxKey.BlastM, at, 6);
						return;
					}
					if (HenshinSfx.PlayCooldown(SfxKey.HitHeavy, at, 6, 0.9f, -0.1f))
						return;
					break;
			}
			// 属性音（多发共用冷却 4 tick）：土低一点、沙高一点更"沙沙"；陨石落地 = 小爆。
			if (kind == H6Fx.KindMeteor)
				HenshinSfx.PlayCooldown(SfxKey.BlastS, at, 4, 0.8f, 0.1f);
			else
				HenshinSfx.PlayCooldown(SfxKey.RockHit, at, 4, kind == H6Fx.KindSand ? 0.7f : 1f,
					kind == H6Fx.KindSand ? 0.3f : kind == H6Fx.KindDirt ? -0.15f : 0f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int kind = (int)Projectile.ai[0];
			Texture2D tex = kind == H6Fx.KindStone ? H6Fx.Stone : kind == H6Fx.KindMeteor ? H6Fx.Impact : H6Fx.Dirt;
			int frameH = tex.Height / Frames;
			int frame = Math.Clamp((Lifetime - Projectile.timeLeft) / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			float scale = Projectile.ai[1] > 0.01f ? Projectile.ai[1] : 1f;
			Color c = kind == H6Fx.KindMeteor ? Color.White : H6Fx.Lit(Projectile.Center);
			if (kind == H6Fx.KindSand)
				c = new Color((int)(c.R * 0.95f), (int)(c.G * 0.85f), (int)(c.B * 0.6f));
			float rot = kind == H6Fx.KindMeteor ? 0f : Projectile.whoAmI * 0.7f;
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), src, c, rot,
				new Vector2(tex.Width * 0.5f, frameH * 0.5f), scale, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>
	/// 流沙地狱（圆陆鲨大招，SpawnAtMouse；不再借 MouseVortexProj / Typhoon）：
	/// 旋转沙涡 180t，判定半径 176px（直径 352px），每 18t 一段伤害；
	/// 服务器/单机对圈内非 Boss 敌人做向心 + 环流轻拉并缠绕减速（沙地狱的「困住」）。
	/// 贴图 Assets/Fx/H6SandTomb（8 帧 × 3 tick，228×228 px，原点中心）。
	/// </summary>
	public class H6SandTombProj : HenshinMoveProj
	{
		private const int Total = 180;
		private const int GrowTicks = 12;
		private const int FadeTicks = 20;
		private const float HitRadius = 176f; // R3：128→176（直径 352px）；贴图 228×228，绘制比例 2*HitRadius/(fh-4)，圈 = 判定
		private const float PullRadius = 400f; // R3：牵引半径 224→400
		private const int Frames = 8;
		private static Asset<Texture2D> _ring;
		private static readonly Color SandTint = new(235, 195, 115);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private int Age => Total - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 352;
			Projectile.height = 352;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Total;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 18;
		}

		public override bool ShouldUpdatePosition() => false;

		private float Grow()
		{
			float g = Math.Clamp(Age / (float)GrowTicks, 0f, 1f);
			return g * g * (3f - 2f * g);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float r = HitRadius * (0.25f + 0.75f * Grow());
			Vector2 c = Projectile.Center;
			float px = Math.Clamp(c.X, targetHitbox.Left, targetHitbox.Right);
			float py = Math.Clamp(c.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.DistanceSquared(c, new Vector2(px, py)) <= r * r;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// SpawnAtMouse：发射端把 Center 校正到鼠标后再同步一次（同 WhirlpoolUltProj）
				if (Projectile.owner == Main.myPlayer)
				{
					Projectile.Center = HenshinProjUtil.OwnerMouseWorld(Projectile);
					Projectile.netUpdate = true;
				}
				// R3 手感：原 Item34（火焰声）删掉；施放由共享 UltCast 负责，落地重击放到本地第 2 tick（远端此时已收到校正后的 Center）。
			}
			// localAI[1] = 本端年龄；localAI[2] = 收束爆已放
			Projectile.localAI[1]++;
			if (Projectile.localAI[1] == 2f)
			{
				HenshinSfx.Play(SfxKey.BlastM, Projectile.Center, 1f, -0.35f);
				HenshinSfx.Play(SfxKey.RockCast, Projectile.Center, 0.9f, -0.3f);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.L, SandTint, 2f * HitRadius, kenneyFrame: false);
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate, Vector2.UnitY);
			}
			if (Projectile.timeLeft <= FadeTicks && Projectile.localAI[2] == 0f)
			{
				Projectile.localAI[2] = 1f;
				HenshinSfx.Play(SfxKey.BlastS, Projectile.Center, 0.9f, -0.3f);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, SandTint, HitRadius * 1.2f, kenneyFrame: false);
			}
			float gl = Grow();
			Lighting.AddLight(Projectile.Center, 0.6f * gl, 0.45f * gl, 0.2f * gl);
			if (!Main.dedServ)
			{
				// 沙涡边缘 4 盏随涡旋转的灯，地形被照出圈的大小
				float spin = Age * 0.06f;
				for (int i = 0; i < 4; i++)
					Lighting.AddLight(Projectile.Center + (spin + i * MathHelper.PiOver2).ToRotationVector2() * HitRadius * 0.85f * gl,
						0.3f * gl, 0.22f * gl, 0.1f * gl);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft > FadeTicks)
				PullNpcs();

			if (!Main.dedServ && Age % 8 == 0)
			{
				Vector2 off = Main.rand.NextVector2CircularEdge(HitRadius * 0.9f, HitRadius * 0.9f);
				Dust.NewDustPerfect(Projectile.Center + off, DustID.Sand, new Vector2(-off.Y, off.X) * 0.02f, 100, default, 1f).noGravity = true;
			}
		}

		private void PullNpcs()
		{
			float ramp = Grow();
			Vector2 c = Projectile.Center;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.boss || n.dontTakeDamage || n.knockBackResist <= 0f
					|| !n.CanBeChasedBy(Projectile))
					continue;
				Vector2 to = c - n.Center;
				float d = to.Length();
				if (d > PullRadius || d < 1f)
					continue;
				// R3：向心速度上限 14；地面怪 AI 每 tick 覆盖速度，所以再直接位移（Collision.TileCollision 防穿墙）
				float k = Math.Clamp(0.5f + n.knockBackResist, 0.5f, 1f);
				Vector2 dir = to / d;
				Vector2 tangent = new(-dir.Y, dir.X);
				float inward = Math.Clamp(d * 0.12f, 3f, 14f) * ramp;
				float swirl = 2.5f * ramp * MathF.Min(d / 60f, 1f);
				if (d < 28f)
				{
					n.velocity *= 0.8f; // 已贴近中心：减速停住，避免来回抖动
					continue;
				}
				n.velocity = Vector2.Lerp(n.velocity, dir * inward + tangent * swirl, 0.4f * k);
				Vector2 step = dir * MathF.Min(inward * 0.5f, d - 20f) * k;
				n.position += Collision.TileCollision(n.position, step, n.width, n.height, false, false, 1);
				if (Age % 3 == 0)
					n.netUpdate = true;
				if (Age % 10 == 0)
					n.AddBuff(BuffID.Slow, 40);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 90);
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 120);
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, SandTint, target.Center);
			H6Fx.SpawnBurst(Projectile, target.Center, H6Fx.KindSand, 0.7f, H6Fx.FxOf(tier));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_ring ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6SandTomb/H6SandTomb", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _ring.Value;
			int fh = tex.Height / Frames;
			float fade = Projectile.timeLeft < FadeTicks ? Projectile.timeLeft / (float)FadeTicks : 1f;
			float g = Grow();
			if (g * fade < 0.02f)
				return false;
			Vector2 origin = new(tex.Width * 0.5f, fh * 0.5f);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Color lit = H6Fx.Lit(Projectile.Center, 150);
			int f0 = (int)(Main.GameUpdateCount / 3) % Frames;
			float s = 2f * HitRadius / (fh - 4f) * (0.25f + 0.75f * g);
			// 外层大涡 + 内层反向小涡（同一张图，转相位错开，省一张贴图）
			Main.EntitySpriteDraw(tex, pos, new Rectangle(0, fh * f0, tex.Width, fh), lit * (0.9f * fade), 0f, origin, s, SpriteEffects.None, 0f);
			int f1 = (Frames - 1 - f0 + 4) % Frames;
			Main.EntitySpriteDraw(tex, pos, new Rectangle(0, fh * f1, tex.Width, fh), lit * (0.75f * fade), 0f, origin, s * 0.62f, SpriteEffects.FlipHorizontally, 0f);
			return false;
		}
	}
}
