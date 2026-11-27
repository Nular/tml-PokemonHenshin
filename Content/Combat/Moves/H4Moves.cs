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
	/// <summary>H4 组（迷你龙 / 哈克龙 / 快龙）共用：贴图懒加载 + 命中特效生成。</summary>
	public static class H4Fx
	{
		public const int StyleDragon = 0;
		public const int StyleOutrage = 1;
		public const int StylePulse = 2;

		private static Asset<Texture2D> _orb, _outrageOrb, _burst, _outrageBurst, _wing, _pulseHit, _pulseBolt, _pulseShard, _gust, _tail;

		/// <summary>龙之波动弹体：Assets/Fx/DragonPulse/PulseBolt.png，4 帧竖排 48x32，朝 +x。</summary>
		public static Texture2D PulseBolt()
			=> (_pulseBolt ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonPulse/PulseBolt", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>龙之波动碎片：PulseShard.png，单帧 16x16。</summary>
		public static Texture2D PulseShard()
			=> (_pulseShard ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonPulse/PulseShard", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>龙尾身节：Assets/Fx/DragonTailWhip/DragonTail.png，4 帧竖排 24x20。</summary>
		public static Texture2D Tail()
			=> (_tail ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonTailWhip/DragonTail", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>翅膀攻击风刃/羽毛：WingGust.png，2 帧竖排 32x40（0 风刃，1 羽毛）。</summary>
		public static Texture2D Gust()
			=> (_gust ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/WingAttack/WingGust", AssetRequestMode.ImmediateLoad)).Value;

		public static Texture2D Orb(bool outrage)
			=> (outrage
				? (_outrageOrb ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonOrb/OutrageOrb", AssetRequestMode.ImmediateLoad))
				: (_orb ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonOrb/DragonOrb", AssetRequestMode.ImmediateLoad))).Value;

		public static Texture2D Burst(int style)
			=> style == StylePulse
				? (_pulseHit ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonPulse/PulseHit", AssetRequestMode.ImmediateLoad)).Value
				: (style == StyleOutrage
				? (_outrageBurst ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonBurst/OutrageBurst", AssetRequestMode.ImmediateLoad))
				: (_burst ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/DragonBurst/DragonBurst", AssetRequestMode.ImmediateLoad))).Value;

		public static Texture2D Wing()
			=> (_wing ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/WingAttack/WingSwipe", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>竖排 N 帧贴图画一帧，原点取帧中心。</summary>
		public static void DrawFrame(Texture2D tex, int frames, int frame, Vector2 worldPos, Color color, float rot, float scale)
		{
			int h = tex.Height / frames;
			Rectangle src = new(0, h * Math.Clamp(frame, 0, frames - 1), tex.Width, h);
			Main.EntitySpriteDraw(tex, (worldPos - Main.screenPosition).Floor(), src, color, rot,
				new Vector2(tex.Width * 0.5f, h * 0.5f), scale, SpriteEffects.None, 0f);
		}

		/// <summary>拥有者端生成 0 伤害命中特效（弹幕自动联机同步）。</summary>
		public static void Hit(Projectile src, Vector2 pos, int style, float scale = 1.2f)
		{
			if (src.owner != Main.myPlayer)
				return;
			Projectile.NewProjectile(src.GetSource_FromThis(), pos, Vector2.Zero,
				ModContent.ProjectileType<H4HitFxProj>(), 0, 0f, src.owner, style, 0f, scale);
		}
	}

	/// <summary>H4 命中特效（纯视觉，0 伤害）：ai0 = 风格（0 龙系蓝紫 / 1 逆鳞红橙），ai2 = 缩放。贴图 Assets/Fx/DragonBurst/，5 帧 × 3 tick。</summary>
	public class H4HitFxProj : ModProjectile
	{
		private const int Frames = 5, TicksPerFrame = 3, Lifetime = Frames * TicksPerFrame;

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
			bool red = (int)Projectile.ai[0] == H4Fx.StyleOutrage;
			if (Projectile.timeLeft > Lifetime - 6)
				Lighting.AddLight(Projectile.Center, red ? 0.8f : 0.45f, red ? 0.3f : 0.3f, red ? 0.1f : 0.85f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int style = (int)Projectile.ai[0];
			int frame = (Lifetime - Projectile.timeLeft) / TicksPerFrame;
			float scale = Projectile.ai[2] > 0.1f ? Projectile.ai[2] : 1.2f;
			H4Fx.DrawFrame(H4Fx.Burst(style), Frames, frame, Projectile.Center, Color.White, 0f, scale);
			return false;
		}
	}

	/// <summary>
	/// 翅膀攻击（快龙技能2，取代龙之俯冲）：朝鼠标 360°，双翼先后各扫一次（正手 + 反手），非身体冲撞。
	/// ai1 = 朝向弧度 + AimBias（0 = 未锁定，拥有者端首帧写入并 netUpdate；与 ScratchSlashProj 同模式）。
	/// 贴图 Assets/Fx/WingAttack/WingSwipe.png：5 帧竖排，152x120，原点 = 左端中线 (0,60)。
	/// 命中判定：3 条平行线段（Reach 9 格）；每击 11 tick 冷却，生命 24 tick 内 2 击。
	/// </summary>
	public class WingAttackProj : HenshinMoveProj
	{
		private const int Lifetime = 24;
		private const int Stroke2Start = 12;
		private const float Reach = 9f * 16f;
		private const float LineSpacing = 40f;
		private const float AimBias = ScratchSlashProj.AimBias;
		// 总倍率 1.9 不变：弹体伤害按 0.95 每击出，这里把第一击压到 0.8、第二击抬到 1.1（0.8 + 1.1 = 1.9）。
		private const float Stroke1Scale = 0.8f / 0.95f;
		private const float Stroke2Scale = 1.1f / 0.95f;
		private const float Stroke2ReachMul = 1.2f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool Aimed => Projectile.ai[1] > 0.5f;
		private float Angle => Projectile.ai[1] - AimBias;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Stroke2Start;
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

			if (!Aimed)
			{
				if (Projectile.owner != Main.myPlayer)
				{
					Projectile.velocity = Vector2.Zero;
					return;
				}
				Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
				Projectile.ai[1] = aim.ToRotation() + AimBias;
				Projectile.netUpdate = true;
				if (Math.Abs(aim.X) > 0.05f)
					owner.ChangeDir(aim.X > 0f ? 1 : -1);
			}

			Projectile.Center = owner.MountedCenter + Angle.ToRotationVector2() * Reach * 0.5f;
			Projectile.velocity = Vector2.Zero;

			int age = Lifetime - Projectile.timeLeft;
			if ((age == 0 || age == Stroke2Start) && !Main.dedServ)
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = age == 0 ? 0.1f : 0.3f, Volume = 0.8f }, owner.MountedCenter);
			Lighting.AddLight(Projectile.Center, 0.35f, 0.45f, 0.6f);
		}

		private bool _pullSpawned;

		private bool SecondStroke => Lifetime - Projectile.timeLeft >= Stroke2Start;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			if (SecondStroke)
			{
				modifiers.FinalDamage *= Stroke2Scale; // 第二击更强 + 击退
				modifiers.Knockback *= 1.8f;
				modifiers.Knockback += 3f; // 顺挥向推力折进同步的击退
			}
			else
				modifiers.FinalDamage *= Stroke1Scale;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			float reachMul = SecondStroke ? Stroke2ReachMul : 1f;
			Vector2 dir = Angle.ToRotationVector2();
			Vector2 normal = new(-dir.Y, dir.X);
			Vector2 origin = owner.MountedCenter;
			for (int line = -1; line <= 1; line++)
			{
				Vector2 off = normal * (line * LineSpacing * (line == 0 ? 0f : 1f));
				float reach = (line == 0 ? Reach : Reach * 0.7f) * reachMul;
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin + off, origin + off + dir * reach, 40f, ref point))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!Aimed)
				return;
			Player owner = Main.player[Projectile.owner];
			float along = MathHelper.Clamp(Vector2.Dot(target.Center - owner.MountedCenter, Angle.ToRotationVector2()), 0f, Reach * Stroke2ReachMul);
			bool second = SecondStroke;
			if (second)
			{
				// 第二击：顺挥向再推一把 + 短暂减速（Slow 1.5 秒）
				target.AddBuff(BuffID.Slow, 90);
				// R4：羽毛牵引——整场第二击只在首个命中点生成一次，把 22 格内的敌人（非 Boss）往命中点聚拢。
				if (!_pullSpawned && Projectile.owner == Main.myPlayer)
				{
					_pullSpawned = true;
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
						ModContent.ProjectileType<WingFeatherPullProj>(), WingFeatherPullProj.BurstDamage(Projectile.damage), 0f, Projectile.owner);
				}
			}
			H4Fx.Hit(Projectile, owner.MountedCenter + Angle.ToRotationVector2() * along, H4Fx.StyleDragon, second ? 1.3f : 1.0f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			int age = Lifetime - Projectile.timeLeft;
			bool second = age >= Stroke2Start;
			int sAge = second ? age - Stroke2Start : age;
			if (sAge > 9)
				return false; // 一击展示 10 tick（5 帧 × 2 tick），之间留空
			DrawGust(owner, sAge, second);
			Texture2D tex = H4Fx.Wing();
			int frames = 5;
			int h = tex.Height / frames;
			Rectangle src = new(0, h * Math.Clamp(sAge / 2, 0, frames - 1), tex.Width, h);
			float rot = Angle;
			// 正手 / 反手：第二击上下翻转；朝左半圈再翻一次，保持受光面朝上。
			bool flip = second ^ (Math.Cos(rot) < 0);
			Main.EntitySpriteDraw(tex, (owner.MountedCenter + rot.ToRotationVector2() * 8f - Main.screenPosition).Floor(), src, Color.White, rot,
				new Vector2(0f, h * 0.5f), 1f, flip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
			return false;
		}

		/// <summary>扫出的风刃（每击一道）；第二击再甩出两片羽毛。纯绘制，全端按 age 确定性重现，不产生弹幕。</summary>
		private void DrawGust(Player owner, int sAge, bool second)
		{
			Texture2D g = H4Fx.Gust();
			int fh = g.Height / 2;
			float t = sAge / 9f;
			float rot = Angle;
			Color c = Color.White * (1f - t * t);
			float dist = Reach * (second ? Stroke2ReachMul : 1f) * (0.25f + 0.7f * t);
			Vector2 o = owner.MountedCenter;
			Main.EntitySpriteDraw(g, (o + rot.ToRotationVector2() * dist - Main.screenPosition).Floor(), new Rectangle(0, 0, g.Width, fh), c, rot,
				new Vector2(4f, fh * 0.5f), second ? 1.4f : 1.0f, SpriteEffects.None, 0f);
			if (!second)
				return;
			for (int s = -1; s <= 1; s += 2)
			{
				float fr = rot + s * (0.25f + 0.35f * t);
				// R4：羽毛换新贴图 WingFeather（4 帧翻转飘动）
				R4DragonFx.DrawFeather(o + fr.ToRotationVector2() * dist * 0.85f, sAge / 3 + (s > 0 ? 2 : 0), fr + s * t * 0.6f, 0.9f, c);
			}
		}
	}
}
