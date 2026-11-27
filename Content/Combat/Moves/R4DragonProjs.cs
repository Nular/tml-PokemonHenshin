using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>R4 龙组贴图懒加载（只在客户端绘制时取）。</summary>
	public static class R4DragonFx
	{
		private static Asset<Texture2D> _blast, _feather;

		/// <summary>龙之波动命中：Assets/Fx/DragonPulse/PulseBlast.png，6 帧竖排 96x96，一次性。</summary>
		public static Texture2D Blast()
			=> (_blast ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonPulse/PulseBlast", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>羽毛：Assets/Fx/WingAttack/WingFeather.png，4 帧竖排 64x30，朝 +x，原点 (6,14)。</summary>
		public static Texture2D Feather()
			=> (_feather ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/WingAttack/WingFeather", AssetRequestMode.ImmediateLoad)).Value;

		private static Asset<Texture2D> _burst;

		/// <summary>羽毛汇集爆炸：Assets/Fx/WingAttack/WingBurst.png，5 帧竖排 96x96，一次性。</summary>
		public static Texture2D Burst()
			=> (_burst ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/WingAttack/WingBurst", AssetRequestMode.ImmediateLoad)).Value;

		public static void DrawFeather(Vector2 worldPos, int frame, float rot, float scale, Color color)
		{
			Texture2D t = Feather();
			int h = t.Height / 4;
			Main.EntitySpriteDraw(t, (worldPos - Main.screenPosition).Floor(), new Rectangle(0, h * (frame & 3), t.Width, h), color, rot,
				new Vector2(6f, h * 0.5f), scale, SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// 龙之波动命中冲击波：小范围 AoE（120px），自绘 PulseBlast 6 帧 x 3 tick。
	/// 由 NebulaPulseShardProj.OnKill 在拥有者端生成（弹幕自动同步）；能量按碎屑口径（见 HenshinNebulaShardTintGlobal.UsesCrumbHitEnergy）。
	/// </summary>
	public class DragonPulseBlastProj : HenshinMoveProj
	{
		private const int Frames = 6, TicksPerFrame = 3, Lifetime = Frames * TicksPerFrame;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 120;
			Projectile.height = 120;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime; // 每个目标只吃一次
		}

		private static readonly Color PulseTint = new(80, 150, 255);

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			// 各端首帧：龙系命中（4 tick 节流，10 发连射不叠）+ 低沉小爆。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.PlayCooldown(SfxKey.DragonHit, Projectile.Center, 4);
				HenshinSfx.PlayCooldown(SfxKey.BlastS, Projectile.Center, 6, 0.6f, -0.25f);
			}
			// S 档爆炸光（先亮后衰，替代旧 6 tick 平光）。
			HenshinExplosion.Pulse(Projectile.Center, PulseTint, BlastSize.S, (Lifetime - Projectile.timeLeft) / (float)Lifetime, BlastRadius * 2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), PulseTint, target.Center);

		private const float BlastRadius = 60f; // 贴图 96px x 1.25 缩放 = 直径 120px

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 c = Projectile.Center;
			Vector2 nearest = new(MathHelper.Clamp(c.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(c.Y, targetHitbox.Top, targetHitbox.Bottom));
			return Vector2.DistanceSquared(c, nearest) <= BlastRadius * BlastRadius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int frame = (Lifetime - Projectile.timeLeft) / TicksPerFrame;
			// S 档爆炸配方（白热核 + 冲击环）垫在手绘 PulseBlast 下面。
			HenshinFxDraw.BeginAdditive();
			HenshinExplosion.Draw(Projectile.Center, BlastSize.S, PulseTint, (Lifetime - Projectile.timeLeft) / (float)Lifetime, BlastRadius * 2f, 0f, kenneyFrame: false);
			HenshinFxDraw.EndAdditive();
			H4Fx.DrawFrame(R4DragonFx.Blast(), Frames, frame, Projectile.Center, Color.White, 0f, 1.25f);
			return false;
		}
	}

	/// <summary>
	/// 翅膀攻击第二击命中后的羽毛牵引：0 伤害，由拥有者端生成（弹幕同步到服务器）。
	/// 牵引只在服务器 / 单机算（Main.netMode != MultiplayerClient），NPC 位置由服务器同步，所以各端一致。
	/// Boss 不受牵引。绘制：8 片羽毛绕命中点螺旋收拢，按 age 确定性重现。
	/// </summary>
	public class WingFeatherPullProj : ModProjectile
	{
		private const int Life = 40;
		private const int PullTicks = 30;
		private const float Radius = 22f * 16f; // 原羽毛作用范围约 8 格，这里 22 格
		private const int Feathers = 8;
		// R5：汇集结束爆一次，倍率 0.7（翅膀攻击单击弹体按 0.95 出伤，故按 0.7/0.95 换算）。
		private const float BurstMult = 0.7f, WingBaseMult = 0.95f;

		/// <summary>翅膀攻击弹体伤害 → 汇集爆炸伤害（牵引弹自己不伤人，只带着这个数）。</summary>
		public static int BurstDamage(int wingDamage) => Math.Max(1, (int)(wingDamage * BurstMult / WingBaseMult));

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
		}

		public override bool? CanDamage() => false;

		private int Age => Life - Projectile.timeLeft;

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			// 光照 D 组：羽毛 = lit，删自身光（原 0.3/0.4/0.6）
			if (Main.netMode != NetmodeID.MultiplayerClient && Age < PullTicks)
				PullNpcs();
			// 汇集结束：只由拥有者生成爆炸判定弹（弹幕 spawn 包同步到各端）
			if (Age == PullTicks && Projectile.owner == Main.myPlayer && Projectile.damage > 0)
			{
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WingFeatherBurstProj>(), Projectile.damage, 0f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
				}
			}
		}

		private void PullNpcs()
		{
			float ramp = Math.Min(1f, (Age + 1) / 6f);
			Vector2 c = Projectile.Center;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type]
					|| n.dontTakeDamage || n.knockBackResist <= 0f || !n.CanBeChasedBy(Projectile))
					continue;
				Vector2 to = c - n.Center;
				float d = to.Length();
				if (d > Radius || d < 1f)
					continue;
				if (d < 24f)
					continue; // 已到中心：不再牵引也不钉速，让其自然停下，避免叠成一点
				float k = Math.Clamp(0.4f + n.knockBackResist, 0.4f, 1f);
				Vector2 dir = to / d;
				float inward = Math.Clamp(d * 0.1f, 3f, 11f) * ramp;
				n.velocity = Vector2.Lerp(n.velocity, dir * inward, 0.4f * k);
				// 地面怪 AI 每 tick 覆盖速度：再直接位移（防穿墙）
				Vector2 step = dir * MathF.Min(inward * 0.5f, d - 20f) * k;
				n.position += Collision.TileCollision(n.position, step, n.width, n.height, false, false, 1);
				if (Age % 3 == 0)
					n.netUpdate = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float t = Age / (float)Life;
			float fade = Age > Life - 10 ? (Life - Age) / 10f : 1f;
			float r = Radius * 0.55f * (1f - t * t) + 12f;
			for (int i = 0; i < Feathers; i++)
			{
				float a = i * MathHelper.TwoPi / Feathers + t * 3.2f;
				Vector2 pos = Projectile.Center + a.ToRotationVector2() * r;
				// 羽尖朝向收拢方向并带点切向偏转（螺旋）
				float rot = a + MathHelper.Pi - 0.5f;
				R4DragonFx.DrawFeather(pos, Age / 3 + i, rot, 0.9f, HenshinFxDraw.LightAt(pos) * fade); // 光照 D 组：每根羽毛取自己位置的光
			}
			return false;
		}
	}

	/// <summary>
	/// 羽毛汇集爆炸：不加粒子，自绘 WingBurst 5 帧 x 3 tick；半径 90 的圆，每目标一次。
	/// 由 WingFeatherPullProj 在拥有者端于汇集结束时生成（Boss 也会吃爆炸伤害，只是不被牵引）。
	/// </summary>
	public class WingFeatherBurstProj : HenshinMoveProj
	{
		private const int Frames = 5, TicksPerFrame = 3, Lifetime = Frames * TicksPerFrame;
		private const float BlastRadius = 90f, DrawScale = 1.9f; // 96px x 1.9 ≈ 直径 182

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime; // 每个目标只吃一次
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			if (Projectile.timeLeft == Lifetime && !Main.dedServ)
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Pitch = 0.5f, Volume = 0.55f }, Projectile.Center);
			if (Projectile.timeLeft > Lifetime - 6)
				Lighting.AddLight(Projectile.Center, 0.08f, 0.12f, 0.2f); // 光照 D 组：mixed 小光 ≤0.2（原 0.3/0.45/0.8）
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 c = Projectile.Center;
			Vector2 nearest = new(MathHelper.Clamp(c.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(c.Y, targetHitbox.Top, targetHitbox.Bottom));
			return Vector2.DistanceSquared(c, nearest) <= BlastRadius * BlastRadius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int frame = (Lifetime - Projectile.timeLeft) / TicksPerFrame;
			// 光照 D 组：羽爆 mixed = Lerp(光, 白, 0.4)；直径约 11 格只取中心光（有 0.4 白底，不会全黑）
			H4Fx.DrawFrame(R4DragonFx.Burst(), Frames, frame, Projectile.Center, Color.Lerp(lightColor, Color.White, 0.4f), 0f, DrawScale);
			return false;
		}
	}
}
