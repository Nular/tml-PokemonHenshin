using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Buffs;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>臂锤：Kenney 下砸拳头（Game Icons fightFist）抬起→下砸；落地冲击。</summary>
	public class ArmHammerSmashProj : HenshinMoveProj
	{
		private const int Life = 32;
		private const int RaiseTicks = 10;
		private const int HangTicks = 4;
		private const int SlamTicks = 8;
		private Vector2 _dir;
		private Vector2 _fist;
		private Vector2 _impact;
		private bool _smashed;
		private int _smashAge;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.MountedCenter;
				if (_dir.LengthSquared() < 1f) _dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				_impact = p.MountedCenter + _dir * 96f + new Vector2(0f, 24f * (1f - Math.Abs(_dir.Y))); // 360°：指哪砸哪（水平时落在脚边）
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, p.Center);
			}

			int age = Life - Projectile.timeLeft;
			Vector2 raised = _impact + new Vector2(0f, -110f);

			if (age <= RaiseTicks)
			{
				float u = age / (float)RaiseTicks;
				u = u * u;
				_fist = Vector2.Lerp(p.MountedCenter + _dir * 40f, raised, u);
			}
			else if (age <= RaiseTicks + HangTicks)
			{
				_fist = raised;
			}
			else if (age <= RaiseTicks + HangTicks + SlamTicks)
			{
				float u = (age - RaiseTicks - HangTicks) / (float)SlamTicks;
				u = u * u; // ease-in 下砸
				_fist = Vector2.Lerp(raised, _impact, u);
				if (!_smashed && u >= 0.92f)
				{
					_smashed = true;
					_smashAge = 0;
					_fist = _impact;
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.25f, Volume = 0.75f }, _impact);
					for (int i = 0; i < 3; i++)
						Dust.NewDustPerfect(_impact, DustID.Iron, Main.rand.NextVector2Circular(6f, 3f) + new Vector2(0f, -1f), 70, new Color(200, 210, 220), 1.35f).noGravity = true;
				}
			}
			else
			{
				_fist = _impact;
				if (!_smashed)
				{
					_smashed = true;
					_smashAge = 0;
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.25f, Volume = 0.75f }, _impact);
				}
			}

			if (_smashed)
				_smashAge++;
			Projectile.Center = _fist;
			Projectile.rotation = MathHelper.Pi; // 贴图指下时再翻 180 由绘制处理；此处存占位
		}

		public override bool? CanDamage() => _smashed ? null : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(target.Center, DustID.Iron, Main.rand.NextVector2Circular(4f, 4f), 80, new Color(200, 215, 235), 1.1f).noGravity = true;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!_smashed) return false;
			Vector2 closest = Vector2.Clamp(_impact, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, _impact) <= 56f * 56f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			// 自绘 Assets/Fx/ArmHammer：frame0 拳头（朝下，原点=拳底中心），frame1..3 落地冲击环（Alpha 混合，不再 Additive 实体）
			Vector2 fistScale = new Vector2(1.2f);
			Vector2 drawFist = _fist;
			if (_smashed)
			{
				float squashT = MathHelper.Clamp(_smashAge / 10f, 0f, 1f);
				float peak = 1f - squashT;
				peak *= peak;
				fistScale = new Vector2(1.2f + 0.4f * peak, 1.2f - 0.4f * peak);
			}
			float fistAlpha = _smashed ? MathHelper.Clamp(1.2f - _smashAge / 16f, 0f, 1f) : 1f;
			H5Fx.Draw("ArmHammer", 4, 0, drawFist + new Vector2(0f, 20f * fistScale.Y), new Vector2(40f, 58f), 0f, fistScale, fistAlpha);
			if (_smashed)
			{
				int ring = 1 + Math.Min(2, _smashAge / 4);
				float ringA = MathHelper.Clamp(1f - _smashAge / 14f, 0f, 1f);
				H5Fx.Draw("ArmHammer", 4, ring, _impact + new Vector2(0f, 24f), new Vector2(40f, 58f), 0f, 1.5f, ringA);
			}
			return false;
		}
	}

	/// <summary>金属爪：鼠标两侧镜像 scratch，约 16 格范围，渐进绘制并持续伤害。</summary>
	public class MetalClawDirectorProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] != 0f || Projectile.owner != Main.myPlayer)
				return;
			Projectile.localAI[0] = 1f;
			Vector2 at = Projectile.Center;
			SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.2f, Volume = 0.95f }, at);
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
				ModContent.ProjectileType<MetalClawSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			if (id >= 0)
			{
				Main.projectile[id].Center = at;
				Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
				HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
				if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.Delivery = MoveDelivery.MeleeArc;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>金属爪场：钉在鼠标；双侧竖直镜像 scratch 自上而下划出/收回；持续伤。</summary>
	public class MetalClawSlashProj : HenshinMoveProj
	{
		private const int RevealTicks = 14;
		private const int HoldTicks = 20;
		private const int FadeTicks = 14;
		private const int Life = RevealTicks + HoldTicks + FadeTicks;
		private const float RangeTiles = 16f;
		private Vector2 _fixed;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			int size = (int)(RangeTiles * 16f);
			Projectile.width = size;
			Projectile.height = size;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 7;
		}

		/// <summary>出现 0→1，保持 1，消失 1→0（与出现同一套自上而下裁剪）。</summary>
		private static float ClawRevealAmount(int age)
		{
			static float Smooth(float u)
			{
				u = MathHelper.Clamp(u, 0f, 1f);
				return u * u * (3f - 2f * u);
			}

			if (age <= RevealTicks)
				return Smooth(age / (float)RevealTicks);
			if (age <= RevealTicks + HoldTicks)
				return 1f;
			float fadeAge = age - RevealTicks - HoldTicks;
			return 1f - Smooth(fadeAge / (float)FadeTicks);
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_fixed = Projectile.owner == Main.myPlayer
					? HenshinProjUtil.OwnerMouseWorld(Projectile)
					: Projectile.Center;
				Projectile.Center = _fixed;
				SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.1f, Volume = 0.6f }, _fixed);
			}
			Projectile.Center = _fixed;

			int age = Life - Projectile.timeLeft;
			float reveal = ClawRevealAmount(age);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(target.Center, DustID.Iron, Main.rand.NextVector2Circular(3f, 3f), 80, new Color(200, 220, 240), 1.1f).noGravity = true;
		}

		public override bool? CanDamage()
		{
			int age = Life - Projectile.timeLeft;
			float reveal = ClawRevealAmount(age);
			return reveal >= 0.2f ? null : false;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float r = RangeTiles * 16f * 0.5f;
			Vector2 closest = Vector2.Clamp(_fixed, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(closest, _fixed) <= r * r;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			float reveal = ClawRevealAmount(age);
			if (reveal <= 0.02f)
				return false;

			// 自绘 Assets/Fx/MetalClaw（6 帧，右侧爪向内倾；左侧水平镜像）。帧按时间取，不再用 Kenney 渐进裁剪（原擦除 bug 随之消失）。
			int frame;
			if (age < RevealTicks) frame = Math.Min(3, age * 4 / RevealTicks);
			else if (age < RevealTicks + 6) frame = 4;
			else if (age <= RevealTicks + HoldTicks) frame = 3;
			else frame = 5;
			float alpha = age > RevealTicks + HoldTicks ? 1f - (age - RevealTicks - HoldTicks) / (float)FadeTicks : 1f;
			float s = RangeTiles * 16f / 160f;
			Vector2 top = _fixed + new Vector2(0f, -RangeTiles * 8f);
			Vector2 org = new Vector2(56f, 2f);
			H5Fx.Draw("MetalClaw", 6, frame, top + new Vector2(34f, 0f), org, 0f, s, alpha);
			H5Fx.Draw("MetalClaw", 6, frame, top + new Vector2(-34f, 0f), org, 0f, s, alpha, SpriteEffects.FlipHorizontally);
			return false;
		}
	}

	/// <summary>波导弹：大型冲击波球，强自带追踪。</summary>
	public class AuraSphereBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.extraUpdates = 1;
			InherentHoming = true;
			HomingTurnRate = 0.22f;
			HomingRangeTiles = 48f;
		}

		public override void AI()
		{
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - Projectile.Center;
				Projectile.velocity = (aim.LengthSquared() < 1f
					? new Vector2(Main.player[Projectile.owner].direction, 0f)
					: Vector2.Normalize(aim)) * 9f;
			}
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Projectile.rotation += 0.18f;
			Lighting.AddLight(Projectile.Center, 0.25f, 0.55f, 0.95f);
			if (Main.rand.NextBool(2))
			{
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), DustID.DungeonWater,
					-Projectile.velocity * 0.05f, 80, new Color(120, 190, 255), 1.5f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Pitch = 0.2f }, Projectile.Center);
			for (int i = 0; i < 20; i++)
				Dust.NewDustPerfect(target.Center, DustID.DungeonWater, Main.rand.NextVector2Circular(7f, 7f), 60, new Color(140, 210, 255), 1.6f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(80, 160, 255), 0.55f), 1.55f * pulse);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(200, 240, 255), 0.75f), 0.85f * pulse);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>陀螺球：旋转钢环砸向鼠标（玩家不变身）。</summary>
	public class GyroBallSmashProj : HenshinMoveProj
	{
		private float _spin;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Player p = Main.player[Projectile.owner];
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (dir.LengthSquared() < 1f) dir = new Vector2(p.direction, 0f);
				dir.Normalize();
				Projectile.velocity = dir * 16f;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.1f }, Projectile.Center);
			}
			_spin += 0.55f;
			Projectile.rotation = _spin;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item178 with { Pitch = 0.3f, Volume = 0.5f }, target.Center);
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(target.Center, DustID.Iron, Main.rand.NextVector2Circular(4f, 4f), 80, new Color(200, 215, 235), 1.1f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 自绘 Assets/Fx/GyroDisc：4 帧钢齿轮（8 齿，帧间转 11°）+ 代码旋转
			H5Fx.Draw("GyroDisc", 4, (int)(Main.GameUpdateCount / 2) % 4, Projectile.Center, new Vector2(26f, 26f), _spin, 1.2f);
			return false;
		}
	}

	/// <summary>
	/// 隐形岩（圆陆鲨技能2）：鼠标处悬浮一圈尖岩（H6StealthShard，3 种形状，环绕缓转），持续 420 tick。
	/// 判定框 88×88，与可见岩圈（半径约 34px）对齐；倍率不变。SpawnAtMouse：发射端校正 Center 后补同步，
	/// 不再只在拥有者端 LoadProjectile / 只在拥有者端偏移位置（旧版错位来源）。
	/// </summary>
	public class StealthRockFieldProj : HenshinMoveProj
	{
		private const int Total = 420;
		private const int Shards = 5;
		private static ReLogic.Content.Asset<Texture2D> _shard;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 88;
			Projectile.height = 88;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Total;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 24;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (Projectile.owner == Main.myPlayer)
				{
					Projectile.Center = HenshinProjUtil.OwnerMouseWorld(Projectile);
					Projectile.netUpdate = true;
				}
				SoundEngine.PlaySound(SoundID.Item70 with { Pitch = -0.3f, Volume = 0.6f }, Projectile.Center);
			}
			Lighting.AddLight(Projectile.Center, 0.2f, 0.17f, 0.12f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			H6Fx.SpawnBurst(Projectile, target.Center, H6Fx.KindStone, 0.7f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.localAI[0] == 0f)
				return false;
			_shard ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6StealthShard/H6StealthShard", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _shard.Value;
			int fh = tex.Height / 3;
			float age = Total - Projectile.timeLeft;
			float grow = Math.Min(1f, age / 12f);
			float fade = Projectile.timeLeft < 20 ? Projectile.timeLeft / 20f : 1f;
			for (int pass = 0; pass < 2; pass++)
			{
				for (int k = 0; k < Shards; k++)
				{
					float ang = k * MathHelper.TwoPi / Shards + age * 0.025f;
					bool front = MathF.Sin(ang) >= 0f;
					if (front != (pass == 1))
						continue;
					Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang) * 34f * grow, MathF.Sin(ang) * 12f * grow + MathF.Sin(age * 0.08f + k * 1.7f) * 3f);
					Color c = H6Fx.Lit(pos, 120) * (fade * (front ? 1f : 0.7f));
					Rectangle src = new(0, fh * (k % 3), tex.Width, fh);
					Main.EntitySpriteDraw(tex, (pos - Main.screenPosition).Floor(), src, c, MathF.Cos(ang) * 0.45f,
						new Vector2(16f, 20f), grow, SpriteEffects.None, 0f);
				}
			}
			return false;
		}
	}

	/// <summary>子弹拳：短距快速鞭拳。</summary>
	public class BulletPunchProj : HenshinMoveProj
	{
		private const int Life = 10;
		private Vector2 _dir;
		private static ReLogic.Content.Asset<Texture2D> _punchSheet;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			// 判定略扩：原 52×36 → 68×48，覆盖单段 HitJagged
			Projectile.width = 68;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.MountedCenter;
				if (_dir.LengthSquared() < 1f) _dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.35f }, p.Center);
			}
			float t = 1f - Projectile.timeLeft / (float)Life;
			// 前伸略加长：96 → 112
			Projectile.Center = p.MountedCenter + _dir * MathHelper.Lerp(24f, 112f, t);
			Projectile.rotation = _dir.ToRotation();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.localAI[0] == 0f)
				return false;
			// BulletPunch.png：3 帧 96x40，朝右，拳头中心在 (79,20)，尾迹向左；朝左半圈上下翻转保持受光面朝上。
			Texture2D tex = (_punchSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/BulletPunch/BulletPunch", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 3;
			int age = Life - Projectile.timeLeft;
			int frame = age < 3 ? 0 : age < 7 ? 1 : 2;
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), src, Color.White, Projectile.rotation, new Vector2(79f, 20f), 1f, fx, 0f);
			return false;
		}
	}

	/// <summary>自我激励：给自己上 WorkUp buff。</summary>
	public class WorkUpBuffProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 20;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (Projectile.owner == Main.myPlayer)
					p.AddBuff(ModContent.BuffType<HenshinWorkUpBuff>(), HenshinWorkUpBuff.DurationTicks);
				SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.25f }, p.Center);
			}
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(p.Center + Main.rand.NextVector2Circular(40f, 40f), DustID.Cloud,
					new Vector2(0f, -2.5f), 80, new Color(120, 220, 255), 1.4f).noGravity = true;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>舍身冲撞：加长猛撞 + 命中额外爆炸。</summary>
	public class DoubleEdgeLungeProj : TakeDownLungeProj
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			base.OnHitNPC(target, hit, damageDone);
			if (Projectile.owner != Main.myPlayer) return;
			for (int i = 0; i < 8; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(36f, 36f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center + off, Vector2.Zero,
					ModContent.ProjectileType<GraySolarBurstVfxProj>(), 0, 0f, Projectile.owner);
				if (id >= 0) Main.projectile[id].Center = target.Center + off;
			}
			for (int i = 0; i < 22; i++)
				Dust.NewDustPerfect(target.Center, DustID.Smoke, Main.rand.NextVector2Circular(7f, 7f), 70, new Color(180, 170, 150), 1.6f).noGravity = true;
			SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.55f, Pitch = -0.15f }, target.Center);
		}
	}

	/// <summary>龙之俯冲：瞬抬升空 → 砸向鼠标。</summary>
	public class DragonDiveSlamProj : HenshinMoveProj
	{
		private const int Rise = 10, Hang = 6, Slam = 16;
		private int _phase;
		private Vector2 _target, _dir;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Rise + Hang + Slam + 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
				SoundEngine.PlaySound(SoundID.Item60 with { Pitch = 0.2f }, p.Center);
			}
			Projectile.Center = p.Center;
			if (Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 12);
			}

			int age = Rise + Hang + Slam + 8 - Projectile.timeLeft;
			if (age <= Rise)
			{
				_phase = 0;
				if (Projectile.owner == Main.myPlayer) p.velocity = new Vector2(0f, -22f);
			}
			else if (age <= Rise + Hang)
			{
				_phase = 1;
				if (Projectile.owner == Main.myPlayer) p.velocity *= 0.4f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
			}
			else
			{
				if (_phase != 2)
				{
					_phase = 2;
					_dir = _target - p.Center;
					if (_dir.LengthSquared() < 64f) _dir = new Vector2(0f, 1f);
					_dir.Normalize();
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.25f }, p.Center);
				}
				if (Projectile.owner == Main.myPlayer) p.velocity = _dir * 28f;
				Dust.NewDustPerfect(p.Center, DustID.FireworkFountain_Blue, -_dir * 3f + Main.rand.NextVector2Circular(2f, 2f), 80, new Color(80, 160, 255), 1.3f).noGravity = true;
			}
		}

		public override bool? CanDamage() => _phase == 2 ? null : false;

		public override bool PreDraw(ref Color lightColor)
		{
			if (_phase != 2) return false;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(100, 180, 255), 0.55f), new Vector2(0.45f, 1.4f), _dir.ToRotation());
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>画龙点睛：伏特攻击式闪现，落地立刻青色路径光迹；路径上延迟依次反色日耀爆；一次面板伤。</summary>
	public class DragonAscentBlinkProj : HenshinMoveProj
	{
		private const float MaxRange = 1280f;
		private Vector2 _from, _to;
		private bool _blinked;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }

			if (!_blinked && Projectile.owner == Main.myPlayer)
			{
				_blinked = true;
				_from = p.Center;
				Vector2 cursor = HenshinProjUtil.OwnerMouseWorld(Projectile);
				Vector2 delta = cursor - _from;
				_to = delta.LengthSquared() > MaxRange * MaxRange ? _from + Vector2.Normalize(delta) * MaxRange : cursor;
				p.Teleport(_to, -1);
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 20 + p.GetModPlayer<HenshinPlayer>().LungeIFrameBonus);
				SoundEngine.PlaySound(SoundID.Item60 with { Pitch = -0.1f }, p.Center);
				for (int i = 0; i < 16; i++)
				{
					Dust.NewDustPerfect(_from, DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, Color.Black, 1.4f).noGravity = true;
					Dust.NewDustPerfect(p.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 80, Color.Black, 1.4f).noGravity = true;
				}

				// 闪现结束立刻出路径粒子；伤害在路径弹生成时结算；爆炸仍由 Trail 延迟依次触发
				Vector2 trail = _to - _from;
				if (trail.LengthSquared() < 4f) trail = new Vector2(p.direction * 48f, 0f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), _from, trail,
					ModContent.ProjectileType<DragonAscentTrailProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.Delivery = MoveDelivery.Lunge;
				}
				SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.35f, Volume = 0.7f }, _to);
			}

			Projectile.Center = p.Center;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class DragonAscentTrailProj : HenshinMoveProj
	{
		// 延迟依次爆炸手感：路径寿命覆盖 Burst 序列（与改即时铺爆前一致）
		private const int Life = 48;
		private const int BurstCount = 20; // R5：10 → 20（每 2 tick 一发）；单体上限由 DragonAscentBlastProj 的 16 tick 共享冷却锁定
		private const float LayoutBoost = 1.5f; // 散落带宽等布局（上一轮整体 +50%）
		// 蓝色线段个体：相对上一轮再 +100%（粗细/长度 ×2）
		private const float StreakSize = LayoutBoost * 2f;
		private const float StreakThick = 16f / 5f * StreakSize; // ~9.6px
		private const float PathShare = 0.6f;  // 路径一击占比
		private const float BlastShare = 0.12f; // 每次爆炸占比（同怪 14 tick 内共享冷却，最多约 3 次）
		private static readonly Color PathCyan = new(1, 253, 255);
		private Vector2 _from, _to;
		private bool _init;
		private int _burstsSpawned;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // 路径上每个目标只吃一次 1.0× 面板伤
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (_init) return;
			_init = true;
			_from = Projectile.Center;
			_to = _from + Projectile.velocity;
			if ((_to - _from).LengthSquared() < 4f)
				_to = _from + new Vector2(48f, 0f);
			Projectile.Center = Vector2.Lerp(_from, _to, 0.5f);
		}

		public override void AI()
		{
			EnsureInit();
			float life = Projectile.timeLeft / (float)Life;
			for (float u = 0f; u <= 1f; u += 0.08f)
			{
				if (!Main.rand.NextBool(16)) continue;
				Dust.NewDustPerfect(Vector2.Lerp(_from, _to, u), DustID.DungeonWater,
					Main.rand.NextVector2Circular(1.2f * LayoutBoost, 1.2f * LayoutBoost), 100, PathCyan, 1.1f * life * LayoutBoost).noGravity = true;
			}

			// 延迟依次爆炸：沿路径按年龄分批触发（上一版手感）
			int age = Life - Projectile.timeLeft;
			int nextBurst = _burstsSpawned;
			if (Projectile.owner == Main.myPlayer && nextBurst < BurstCount && age >= nextBurst * (Life / BurstCount))
			{
				float u = (nextBurst + 0.5f) / BurstCount;
				SpawnTrailBurst(Vector2.Lerp(_from, _to, u));
				_burstsSpawned++;
			}
		}

		private void SpawnTrailBurst(Vector2 at)
		{
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.SolarWhipSwordExplosion);
			if ((_burstsSpawned & 1) == 0)
				SoundEngine.PlaySound(SoundID.Item10 with { Pitch = -0.1f, Volume = 0.45f }, at);
			for (int i = 0; i < 1; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(22f, 22f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at + off, Vector2.Zero,
					ModContent.ProjectileType<GraySolarBurstVfxProj>(), 0, 0f, Projectile.owner, ai0: 1f);
				if (id >= 0) Main.projectile[id].Center = at + off;
			}
			for (int i = 0; i < 1; i++) // R5：爆点翻倍，每点粒子 3 → 1（总粒子 30 → 20）
				Dust.NewDustPerfect(at, DustID.DungeonWater, Main.rand.NextVector2Circular(4.5f, 4.5f),
					60, PathCyan, 1.35f).noGravity = true;

			// 爆炸伤害：每个爆点生成一枚 0.15 倍伤害的爆炸判定弹（仅拥有者端，spawn 包同步）。
			// 此前只有 GraySolarBurstVfxProj（damage 0），所以只有特效没有爆伤。
			if (Projectile.owner != Main.myPlayer)
				return;
			int blast = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
				ModContent.ProjectileType<DragonAscentBlastProj>(), Math.Max(1, (int)(Projectile.damage * BlastShare)), Projectile.knockBack, Projectile.owner);
			if (blast >= 0)
			{
				Projectile bp = Main.projectile[blast];
				bp.originalDamage = Projectile.originalDamage > 0 ? (int)(Projectile.originalDamage * BlastShare) : bp.damage;
				HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, bp);
				if (bp.ModProjectile is IHenshinMoveProj tagged)
					tagged.Delivery = MoveDelivery.AoEBurst;
			}
		}

		public override bool? CanDamage()
		{
			EnsureInit();
			// 仅前两帧开放碰撞；localNPCHitCooldown=-1 保证每目标只吃一次
			return Life - Projectile.timeLeft < 2;
		}

		/// <summary>路径一击占总倍率的 60%；其余由 10 次延迟爆炸（DragonAscentBlastProj，各 15%）补足，与 5.0 同量级。</summary>
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.FinalDamage *= PathShare;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			EnsureInit();
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, 72f * LayoutBoost, ref _);
		}

		/// <summary>确定性散列，保证联机各端光线铺排一致。</summary>
		private float StreakHash(int i, int salt)
		{
			unchecked
			{
				int n = i * 374761393 + salt * 668265263 + Projectile.whoAmI * 1274126177;
				n = (n ^ (n >> 13)) * 1274126177;
				return (n & 0xFFFF) / 65535f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_init) return false;
			float life = Projectile.timeLeft / (float)Life;
			Vector2 delta = _to - _from;
			float pathLen = delta.Length();
			if (pathLen < 4f) return false;
			float rot = delta.ToRotation();
			int age = Life - Projectile.timeLeft;
			// 画龙：自绘 DragonStreak 沿路径冲出（前 14 tick），后接两道残影；路径只留一条细光束
			float f = MathHelper.Clamp(age / 14f, 0f, 1f);
			Texture2D dragon = H7Tex.Get("DragonStreak");
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawContinuousBeam(_from, Vector2.Lerp(_from, _to, f),
				HenshinFxDraw.WithAlpha(new Color(210, 255, 235), 0.8f * life), HenshinFxDraw.WithAlpha(PathCyan, 0.5f * life), 3f, 12f);
			for (int i = 2; i >= 0; i--)
			{
				float ff = MathHelper.Clamp(f - i * 0.07f, 0f, 1f);
				HenshinFxDraw.DrawAdditiveSheet(dragon, 1, 2, (age / 4) & 1, Vector2.Lerp(_from, _to, ff),
					HenshinFxDraw.WithAlpha(Color.White, life * (i == 0 ? 1f : 0.3f / i)), 1.5f, rot);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>画龙点睛的爆炸判定：不可见，4 tick，半径 88 的圆，每目标一次。外观由 GraySolarBurstVfxProj 负责。</summary>
	public class DragonAscentBlastProj : HenshinMoveProj
	{
		private const float Radius = 88f;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 4;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 16; // R5：14 → 16，路径寿命 48 tick 内同怪最多 3 次（0/16/32），爆炸翻倍后单体上限不变
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
			=> Vector2.Distance(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) <= Radius;

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>神鸟猛击：6 只红色勇鸟残影依次冲刺。</summary>
	public class SkyAttackSextetDirectorProj : HenshinMoveProj
	{
		private int _spawned;
		private Vector2 _target;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 72;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_target = HenshinProjUtil.OwnerMouseWorld(Projectile);
				int npc = FindNear(_target, 40f * 16f);
				if (npc >= 0) _target = Main.npc[npc].Center;
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.15f }, p.Center);
			}
			if (Projectile.owner == Main.myPlayer && _spawned < 6 && Projectile.timeLeft % 8 == 0)
			{
				float ang = MathHelper.TwoPi * _spawned / 6f;
				Vector2 spawn = p.Center + ang.ToRotationVector2() * 64f + new Vector2(0f, -40f);
				Vector2 vel = Vector2.Normalize(_target - spawn) * 18f;
				int per = Math.Max(1, Projectile.damage / 6);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<SkyAttackRedBirdProj>(), per, 1.5f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Math.Max(1, (Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage) / 6);
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.Delivery = MoveDelivery.Lunge;
				}
				_spawned++;
			}
		}

		private static int FindNear(Vector2 at, float range)
		{
			int best = -1;
			float bestD = range * range;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy()) continue;
				float d = Vector2.DistanceSquared(n.Center, at);
				if (d < bestD) { bestD = d; best = i; }
			}
			return best;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class SkyAttackRedBirdProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Raven;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 48;
			Projectile.tileCollide = false;
			Projectile.penetrate = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.scale = 1.25f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
			=> Main.instance.LoadProjectile(ProjectileID.Raven);

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Red, -Projectile.velocity * 0.08f, 80, new Color(255, 60, 40), 1.2f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.Raven].Value;
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Raven]);
			Rectangle frame = tex.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 3) % frames);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(255, 70, 55, 230),
				Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center - Projectile.velocity * 0.4f,
				HenshinFxDraw.WithAlpha(new Color(255, 80, 40), 0.55f), new Vector2(1.4f, 0.45f), Projectile.rotation);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>远距咬碎：钉在鼠标处咬合。</summary>
	public class BiteArcAtMouseProj : HenshinMoveProj
	{
		private const int Life = 18;
		private const float VisualScale = 4f;
		private Vector2 _fixed;
		private float _size = 1f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_fixed = Projectile.owner == Main.myPlayer ? HenshinProjUtil.OwnerMouseWorld(Projectile) : Projectile.Center;
				_size = Projectile.ai[0] > 0.1f ? Projectile.ai[0] : 1f;
				float s = _size * VisualScale;
				Projectile.width = (int)(64 * s);
				Projectile.height = (int)(56 * s);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.35f }, _fixed);
			}
			Projectile.Center = _fixed;
		}

		public override bool? CanDamage()
		{
			int age = Life - Projectile.timeLeft;
			return age <= 6 || (age >= 9 && age <= 15) ? null : false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0) target.AddBuff(BuffID.BrokenArmor, (int)Projectile.ai[1]);
		}

		private static ReLogic.Content.Asset<Texture2D> _jaws, _jawsLight;

		/// <summary>与近身咬碎（BiteArcProj）同一套 Assets/Fx/Bite/BiteJaws(+_Light)，6 帧 × 3 tick，同钢紫色调。</summary>
		public override bool PreDraw(ref Color lightColor)
		{
			_jaws ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteJaws", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			_jawsLight ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bite/BiteJaws_Light", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _jaws.Value;
			const int frames = 6;
			int frameH = tex.Height / frames;
			int age = Life - Projectile.timeLeft;
			int frame = Math.Clamp(age / 3, 0, frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			float scale = _size * VisualScale * 0.5f; // 128 px 贴图 × scale = 碰撞宽 64 × s
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			Vector2 pos = Projectile.Center - Main.screenPosition;
			if (frame == 2 || frame == 4)
				pos += new Vector2((float)Math.Sin(Projectile.timeLeft * 5.1f), (float)Math.Cos(Projectile.timeLeft * 4.3f)) * 3f;
			pos = pos.Floor();
			float opacity = frame == frames - 1 ? Projectile.timeLeft / 3f : 1f;
			Color c = new(Math.Max(lightColor.R, (byte)120) * 185 / 255, Math.Max(lightColor.G, (byte)110) * 175 / 255, Math.Max(lightColor.B, (byte)100) * 220 / 255);
			Main.EntitySpriteDraw(tex, pos, src, c * opacity, 0f, origin, scale, SpriteEffects.None, 0f);
			HenshinFxDraw.BeginAdditive();
			Main.EntitySpriteDraw(_jawsLight.Value, pos, src, new Color(200, 160, 255) * opacity, 0f, origin, scale, SpriteEffects.None, 0f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
