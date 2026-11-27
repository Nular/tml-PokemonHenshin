using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 爪击（抓 Scratch / 龙爪 DragonClaw 共用）：朝鼠标 360° 的三道平行爪痕。
	/// ai0 = reachTiles（默认 3.5；龙爪 20）；ai1 = 朝向弧度 + AimBias（0 = 尚未锁定，拥有者端 AI 首帧写入并 netUpdate）；
	/// ai2 = 外观（0 = 抓·短爪痕；1 = 龙爪·火系 20 格 + HitJagged 帧）。
	/// 贴图 Assets/Fx/Scratch/Scratch.png、Assets/Fx/DragonClaw/DragonClaw.png：5 帧竖排，原点 = 左端中线。
	/// </summary>
	public class ScratchSlashProj : HenshinMoveProj
	{
		private const int Lifetime = 14;
		private const float DefaultReachTiles = 3.5f;
		private const float LineSpacing = 16f;
		/// <summary>ai1 偏移：角度 ∈ (-π, π]，+10 后恒 &gt; 0，用 0 表示未锁定。</summary>
		internal const float AimBias = 10f;
		private const int SheetFrames = 5;

		private static Asset<Texture2D> _scratchSheet;
		private static Asset<Texture2D> _dragonSheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool Dragon => Projectile.ai[2] > 0.5f;
		private bool Aimed => Projectile.ai[1] > 0.5f;
		private float Angle => Projectile.ai[1] - AimBias;
		private Vector2 Dir => Angle.ToRotationVector2();

		private float ReachPx
		{
			get
			{
				float tiles = Projectile.ai[0] > 0.5f ? Projectile.ai[0] : DefaultReachTiles;
				return tiles * 16f;
			}
		}

		private bool _castSfx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			// 360° 朝鼠标：仅拥有者端求一次（OwnerAimDir），弧度写 ai1 并 netUpdate 同步。
			if (!Aimed)
			{
				if (Projectile.owner != Main.myPlayer)
				{
					Projectile.velocity = Vector2.Zero;
					return; // 等拥有者同步
				}
				Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
				Projectile.ai[1] = aim.ToRotation() + AimBias;
				Projectile.netUpdate = true;
				if (Math.Abs(aim.X) > 0.05f)
					owner.ChangeDir(aim.X > 0f ? 1 : -1);
			}

			Vector2 dir = Dir;
			Vector2 normal = new(-dir.Y, dir.X);
			float reach = ReachPx;

			// 施放音：各端首次拿到朝向时播一次（本弹已 ClaimCastSound，之力不再响 Item1）。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.Swing, owner.MountedCenter, 1f, Dragon ? -0.2f : 0.1f);
				if (Dragon)
					HenshinSfx.Play(SfxKey.DragonCast, owner.MountedCenter, 0.7f);
			}

			Projectile.Center = owner.MountedCenter + dir * Math.Max(36f, reach * 0.55f);
			Projectile.velocity = Vector2.Zero;

			if (Dragon)
				Lighting.AddLight(owner.MountedCenter + dir * (reach * 0.5f), 0.9f, 0.45f, 0.12f);

			if (Main.dedServ)
				return;
			// 刮痕贴图已表达轨迹，不再沿线撒尘（粒子会挡住贴图）。
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			Vector2 dir = Dir;
			Vector2 normal = new(-dir.Y, dir.X);
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = origin + dir * ReachPx;
			for (int line = -1; line <= 1; line++)
			{
				Vector2 off = normal * (line * LineSpacing);
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin + off, tip + off, 16f, ref point))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 命中尘交给 ScratchHitFxProj（各端都画）；这里只接打击感档位。抓 = 轻，龙爪 = 重。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, Dragon ? HitTier.Heavy : HitTier.Light),
				Dragon ? new Color(255, 150, 70) : new Color(255, 225, 190), null, Dragon ? 1f : 0.5f);
			if (Projectile.owner != Main.myPlayer || !Aimed)
				return;
			// 纯视觉命中：落在目标中心沿爪线的投影点。
			Player owner = Main.player[Projectile.owner];
			Vector2 dir = Dir;
			float along = MathHelper.Clamp(Vector2.Dot(target.Center - owner.MountedCenter, dir), 0f, ReachPx);
			Vector2 hitPos = owner.MountedCenter + dir * along;
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), hitPos, Vector2.Zero,
				ModContent.ProjectileType<ScratchHitFxProj>(), 0, 0f, Projectile.owner, Angle, Projectile.ai[2]);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			bool dragon = Dragon;
			float rot = Angle;
			Vector2 dir = Dir;
			float reach = ReachPx;
			int age = Lifetime - Projectile.timeLeft;

			Asset<Texture2D> asset = dragon
				? (_dragonSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonClaw/DragonClaw", AssetRequestMode.ImmediateLoad))
				: (_scratchSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Scratch/Scratch", AssetRequestMode.ImmediateLoad));
			Texture2D tex = asset.Value;
			int frameH = tex.Height / SheetFrames;
			// 前 6 tick 快速展开（0/1/2 各 2 tick），3 帧停 4 tick，4 帧碎开淡出。
			int frame = age < 2 ? 0 : age < 4 ? 1 : age < 6 ? 2 : age < 10 ? 3 : 4;
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Vector2 pos = (owner.MountedCenter - Main.screenPosition).Floor();
			// 朝左半圈上下翻转，受光面（贴图上半）始终朝上；贴图上下对称，原点 Y 取半高。
			SpriteEffects fx = Math.Cos(rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
			float opacity = frame == 4 ? Math.Min(1f, Projectile.timeLeft / 3f) : 1f;
			// 抓是物理（mixed）：本体吃爪线中点的光再向白色抬 40%，暗处仍有一点刃光；龙爪是龙能量（glow），保持白。
			Color body = dragon ? Color.White : Color.Lerp(HenshinFxDraw.LightAt(owner.MountedCenter + dir * (reach * 0.5f)), Color.White, 0.15f);
			Main.EntitySpriteDraw(tex, pos, src, body * opacity, rot, new Vector2(0f, frameH * 0.5f), 1f, fx, 0f);

			if (dragon)
			{
				// 龙爪：沿爪线叠 HitJagged 帧（Draw*Frame，禁整图），Additive 保 Alpha。
				float life = Projectile.timeLeft / (float)Lifetime;
				int jf = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
				HenshinFxDraw.BeginAdditive();
				for (int i = 1; i <= 5; i++)
				{
					float u = i / 5.5f;
					HenshinFxDraw.DrawHitJaggedFrame(owner.MountedCenter + dir * (reach * u),
						HenshinFxDraw.WithAlpha(new Color(255, 170, 90), 0.6f * life), 0.7f + u * 0.35f, rot + MathHelper.Pi, (jf + i) % HenshinFxDraw.HitJaggedFrames);
				}
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>爪击命中（纯视觉，0 伤害）：HitJagged 帧 + 迸溅。ai0 = 爪的朝向弧度，ai1 = 外观（1 = 龙爪火系）。</summary>
	public class ScratchHitFxProj : ModProjectile
	{
		private const int Lifetime = 12;

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
			bool dragon = Projectile.ai[1] > 0.5f;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (!Main.dedServ)
				{
					Vector2 d = Projectile.ai[0].ToRotationVector2();
					for (int i = 0; i < 2; i++)
					{
						Vector2 v = d * Main.rand.NextFloat(1f, 4f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
						Dust.NewDustPerfect(Projectile.Center, dragon ? DustID.Torch : DustID.Smoke, v, 80, default, 1.2f).noGravity = true;
					}
					// 命中音（各端，本弹随生成包同步）：抓 = 轻命中，龙爪 = 龙系命中。
					if (dragon)
						HenshinSfx.PlayCooldown(SfxKey.DragonHit, Projectile.Center, 3);
					else
						HenshinSfx.PlayCooldown(SfxKey.HitLight, Projectile.Center, 3);
					HenshinExplosion.SpawnLight(Projectile.Center, dragon ? new Color(255, 150, 70) : new Color(255, 230, 200), BlastSize.S, dragon ? 1f : 0.6f);
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / (float)Lifetime;
			int frame = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			Color c = Projectile.ai[1] > 0.5f ? new Color(255, 170, 90) : new Color(255, 225, 190);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center, HenshinFxDraw.WithAlpha(c, 0.85f * life), 0.8f,
				Projectile.ai[0] + MathHelper.Pi, frame);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
