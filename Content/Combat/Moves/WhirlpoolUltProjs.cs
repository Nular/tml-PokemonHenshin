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
	/// 潮旋（卡咪龟大招，专属，不与 MouseVortexProj / 流沙地狱共用）。SpawnAtMouse。
	/// 阶段（全由 timeLeft 推出，联机各端一致）：卷起 12t → 旋转吸引 138t → 升起水柱 18t（共 168t）。
	/// 旋转期：每 15t 一段（≈9 段，×0.5）；半径 18 格吸力圈（服务器/单机执行，向心 + 切向环流 + 缠绕减速）。
	/// 水柱期：吸力停、敌人被托起，一次 ×1.4 爆发。贴图 Assets/Fx/Whirlpool（Ring 8 帧 / Spout 6 帧，各 +_Light）。
	/// </summary>
	public class WhirlpoolUltProj : HenshinMoveProj
	{
		private const int Total = 168;
		private const int GrowTicks = 12;
		private const int SpoutStart = 150;
		private const int SpoutTicks = Total - SpoutStart;
		private const float HitRadius = 144f;
		private const float SpoutHitRadius = 112f;
		private const float PullRadius = 288f;
		private const float RingScale = 1.5f;
		private const float SpinDamageMul = 0.5f;
		private const float SpoutDamageMul = 1.4f;

		private static Asset<Texture2D> _ring;
		private static Asset<Texture2D> _ringLight;
		private static Asset<Texture2D> _spout;
		private static Asset<Texture2D> _spoutLight;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private int Age => Total - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 288;
			Projectile.height = 288;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Total;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		// 位置只由出生点 + 首 tick 校正决定；广角镜 PostAI 追踪见此即跳过，漩涡不漂移。
		public override bool ShouldUpdatePosition() => false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			int a = Age;
			// 旋转期：第一段从卷起结束（age 12）开始，水柱前 10t 收口 → 9 段（12,27,…,132）；判定半径随卷起放大。
			if (a < GrowTicks || (a >= SpoutStart - 10 && a < SpoutStart))
				return false;
			float g = Math.Clamp(a / (float)GrowTicks, 0f, 1f);
			g = g * g * (3f - 2f * g);
			float r = a >= SpoutStart ? SpoutHitRadius : HitRadius * (0.25f + 0.75f * g);
			Vector2 c = Projectile.Center;
			float px = Math.Clamp(c.X, targetHitbox.Left, targetHitbox.Right);
			float py = Math.Clamp(c.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.DistanceSquared(c, new Vector2(px, py)) <= r * r;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			int age = Age;
			bool spout = age >= SpoutStart;

			// 一次性逻辑用 localAI 标志：[0]=出生已处理，[1]=水柱已触发
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// SpawnAtMouse：发射端把 Center 校正到鼠标后，再补一次同步
				if (Projectile.owner == Main.myPlayer)
				{
					Projectile.Center = HenshinProjUtil.OwnerMouseWorld(Projectile);
					Projectile.netUpdate = true;
				}
				// 起手 Splash 删掉：大招根弹的共享 UltCast 已在各端响，避免叠两声。
			}
			bool lift = false;
			if (spout && Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				lift = true;
				Projectile.localNPCHitCooldown = SpoutTicks; // 水柱期只吃一次爆发
				// 水柱托起 = 全招高潮（各端）：水爆 + 水系命中 + 原 Item21 低沉水涌，M 号水色爆炸配方（不叠火烟帧），主人端大招震屏。
				SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.7f, Pitch = -0.35f }, Projectile.Center);
				HenshinSfx.Play(SfxKey.BlastM, Projectile.Center, 0.9f, -0.1f);
				HenshinSfx.Play(SfxKey.WaterHit, Projectile.Center, 1f, -0.3f);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, new Color(90, 180, 255), SpoutHitRadius * 2f, kenneyFrame: false, strength: 0.6f);
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate, -Vector2.UnitY);
			}

			// mixed（水，但是大招）：本体吃光，只留一盏 ≤0.2 的淡蓝光。
			float ramp = Math.Clamp(age / (float)GrowTicks, 0f, 1f);
			Lighting.AddLight(Projectile.Center, 0.05f * ramp, 0.11f * ramp, 0.2f * ramp);

			if (Main.netMode != NetmodeID.MultiplayerClient)
				PullNpcs(age, spout, ramp, lift);

			if (!Main.dedServ && age % 6 == 0 && !spout)
			{
				Vector2 off = Main.rand.NextVector2CircularEdge(HitRadius * 0.9f, HitRadius * 0.9f);
				Dust d = Dust.NewDustPerfect(Projectile.Center + off, DustID.Water, new Vector2(-off.Y, off.X) * 0.02f, 90, default, 1.1f);
				d.noGravity = true;
			}
			if (!Main.dedServ && lift)
			{
				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 10f), DustID.Water,
						new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-6f, -3f)), 80, default, 1.2f).noGravity = true;
			}
		}

		/// <summary>服务器/单机：向心 + 环流 + 缠绕减速；水柱起点托起一次。Boss 与击退免疫者不被吸。</summary>
		private void PullNpcs(int age, bool spout, float ramp, bool lift)
		{
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
				float k = Math.Clamp(0.35f + n.knockBackResist, 0f, 1f);
				if (lift)
				{
					if (d <= SpoutHitRadius + 40f)
					{
						n.velocity.Y = -10f * k;
						n.netUpdate = true;
					}
					continue;
				}
				if (spout)
					continue;
				Vector2 dir = to / d;
				Vector2 tangent = new(-dir.Y, dir.X);
				float inward = MathF.Min(d * 0.10f, 8f) * ramp;
				float swirl = 4f * ramp * MathF.Min(d / 40f, 1f);
				Vector2 desired = dir * inward + tangent * swirl;
				n.velocity = Vector2.Lerp(n.velocity, desired, 0.22f * k);
				if (age % 3 == 0)
					n.netUpdate = true;
				if (age % 10 == 0)
					n.AddBuff(BuffID.Slow, 40);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.SourceDamage *= Age >= SpoutStart ? SpoutDamageMul : SpinDamageMul;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 90);
			target.AddBuff(BuffID.Wet, 120);
			// 旋涡期 = 轻档（只白闪 + 光，不连震）；水柱托起那一下 = 大招档（白闪 + 命中星 HitUlt；震屏已在托起帧放过，20 tick 内不重复）。
			bool spoutHit = Age >= SpoutStart;
			HitTier tier = spoutHit ? HenshinHitFeel.TierOf(Projectile, HitTier.Heavy) : HitTier.Light;
			HenshinHitFeel.Flash(target, tier);
			if (spoutHit)
			{
				HenshinHitFeel.Shake(Projectile.owner, target.Center, tier, -Vector2.UnitY);
				TackleHitFxProj.Spawn(Projectile, target.Center, DustID.Water, SfxKey.WaterHit, tier);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_ring ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Whirlpool/WhirlpoolRing", AssetRequestMode.ImmediateLoad);
			_ringLight ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Whirlpool/WhirlpoolRing_Light", AssetRequestMode.ImmediateLoad);
			_spout ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Whirlpool/WhirlpoolSpout", AssetRequestMode.ImmediateLoad);
			_spoutLight ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Whirlpool/WhirlpoolSpout_Light", AssetRequestMode.ImmediateLoad);

			int age = Age;
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();

			// 卷起：smoothstep 放大；水柱期：缩小淡出，被水柱“收走”
			float grow = Math.Clamp(age / (float)GrowTicks, 0f, 1f);
			grow = grow * grow * (3f - 2f * grow);
			float fade = 1f;
			float shrink = 1f;
			if (age >= SpoutStart)
			{
				float t = (age - SpoutStart) / (float)SpoutTicks;
				fade = 1f - t;
				shrink = 1f - 0.3f * t;
			}
			float scale = RingScale * (0.25f + 0.75f * grow) * shrink;
			float op = Math.Clamp(grow * 1.4f, 0f, 1f) * fade;
			if (op > 0.01f)
			{
				const int frames = 8;
				int fr = (int)(Main.GameUpdateCount / 3) % frames;
				Texture2D tex = _ring.Value;
				int fh = tex.Height / frames;
				Vector2 origin = new(tex.Width * 0.5f, fh * 0.5f);
				Rectangle src = new(0, fh * fr, tex.Width, fh);
				Rectangle srcOpp = new(0, fh * ((frames - fr) % frames), tex.Width, fh);

				// 外层：反向旋转（水平翻转 + 倒放帧）的淡圈，扩到吸力圈大小。本体吃光，按格切片逐段取光。
				DrawLitGrid(tex, Projectile.Center, pos, srcOpp, origin, scale * 1.5f, 0.3f * op, 8, 8, SpriteEffects.FlipHorizontally);
				// 主体
				DrawLitGrid(tex, Projectile.Center, pos, src, origin, scale, op, 6, 6, SpriteEffects.None);

				// 发光小部件：_Light 层压到 0.3。
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_ringLight.Value, pos, src, Color.White * (0.3f * op), 0f, origin, scale, SpriteEffects.None, 0f);
				HenshinFxDraw.EndAdditive();
			}

			if (age >= SpoutStart)
			{
				const int frames = 6;
				int fr = Math.Clamp((age - SpoutStart) / 3, 0, frames - 1);
				Texture2D tex = _spout.Value;
				int fh = tex.Height / frames;
				Rectangle src = new(0, fh * fr, tex.Width, fh);
				Vector2 origin = new(tex.Width * 0.5f, fh - 8f); // art (28,88) → px (56,176)
				Vector2 basePos = pos + new Vector2(0f, 36f);
				const float s = 1.5f;
				// 水柱约 16 格高：竖切 6 段逐段取光；_Light 层压到 0.3。
				DrawLitGrid(tex, Projectile.Center + new Vector2(0f, 36f), basePos, src, origin, s, 1f, 1, 6, SpriteEffects.None);
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_spoutLight.Value, basePos, src, Color.White * 0.3f, 0f, origin, s, SpriteEffects.None, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}

		/// <summary>
		/// 大贴图逐格吃光：把 src 切成 cols×rows 块，每块在自己中心的世界坐标取光（<see cref="HenshinFxDraw.Lit"/>），
		/// 拼回原位置。只支持 rot=0；水平翻转时按镜像位置摆块（SpriteBatch 只在块内翻转纹理坐标）。
		/// </summary>
		private static void DrawLitGrid(Texture2D tex, Vector2 worldAnchor, Vector2 screenPos, Rectangle src, Vector2 origin,
			float scale, float opacity, int cols, int rows, SpriteEffects fx)
		{
			bool flipX = (fx & SpriteEffects.FlipHorizontally) != 0;
			for (int gy = 0; gy < rows; gy++)
			{
				int y0 = src.Height * gy / rows;
				int y1 = src.Height * (gy + 1) / rows;
				for (int gx = 0; gx < cols; gx++)
				{
					int x0 = src.Width * gx / cols;
					int x1 = src.Width * (gx + 1) / cols;
					// 块在画面里的左边（以整图左上为 0）；翻转时 [x0,x1] 落在镜像位置 [W-x1, W-x0]。
					float left = flipX ? src.Width - x1 : x0;
					Vector2 cellOrigin = new(origin.X - left, origin.Y - y0);
					Vector2 cellCenter = new(left + (x1 - x0) * 0.5f, (y0 + y1) * 0.5f);
					Color c = HenshinFxDraw.Lit(worldAnchor + (cellCenter - origin) * scale, Color.White) * opacity;
					Rectangle part = new(src.X + x0, src.Y + y0, x1 - x0, y1 - y0);
					Main.EntitySpriteDraw(tex, screenPos, part, c, 0f, cellOrigin, scale, fx, 0f);
				}
			}
		}
	}
}
