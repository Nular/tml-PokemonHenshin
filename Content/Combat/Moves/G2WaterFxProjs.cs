using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 撞击命中：白色冲击星（纯视觉，0 伤害）。ai0 = 招式 Dust ID，用来给白星染色（水蓝/草绿/其余奶白）。
	/// 贴图 Assets/Fx/TackleHit(+_Light)，5 帧 × 3 tick，24×24 art px（48×48 px），原点居中。
	/// </summary>
	public class TackleHitFxProj : ModProjectile
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = Frames * TicksPerFrame;
		private static Asset<Texture2D> _sheet;
		private static Asset<Texture2D> _light;

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
				if (!Main.dedServ)
				{
					int dust = (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : DustID.Smoke;
					for (int i = 0; i < 3; i++)
						Dust.NewDustPerfect(Projectile.Center, dust, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.1f).noGravity = true;
				}
			}
			if (Lifetime - Projectile.timeLeft < 6)
				Lighting.AddLight(Projectile.Center, 0.3f, 0.3f, 0.34f);
		}

		private static Color Tint(int dust)
		{
			if (dust == DustID.Water)
				return new Color(150, 215, 255);
			if (dust == DustID.Grass)
				return new Color(170, 235, 130);
			if (dust == DustID.Torch)
				return new Color(255, 175, 90);
			return new Color(255, 245, 225);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/TackleHit/TackleHit", AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/TackleHit/TackleHit_Light", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			Color tint = Tint((int)Projectile.ai[0]);
			// 抬亮：夜里也看得见（暗色抬亮）
			Color c = new(Math.Max(lightColor.R, (byte)150), Math.Max(lightColor.G, (byte)150), Math.Max(lightColor.B, (byte)150));
			c = new Color(c.R * tint.R / 255, c.G * tint.G / 255, c.B * tint.B / 255);
			Main.EntitySpriteDraw(tex, pos, src, c, 0f, origin, 1f, SpriteEffects.None, 0f);
			if (frame <= 1)
			{
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_light.Value, pos, src, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>
	/// 跃起（鲤鱼王）：指针处水柱冲天。判定与 StrikeFallProj 相同（40×80，18 tick，同冷却/免疫）。
	/// ai1 = Dust（保留给接口一致，不再每 tick 撒粒子）。贴图 Assets/Fx/SplashColumn(+_Light)，5 帧 × 3 tick，20×44 art px，原点 (10,42)。
	/// </summary>
	public class SplashFallProj : HenshinMoveProj
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = 18;
		private static Asset<Texture2D> _sheet;
		private static Asset<Texture2D> _light;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 80;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
		}

		public override void AI()
		{
			if (Projectile.ai[0] == 0f && Projectile.owner == Main.myPlayer)
			{
				Projectile.Center = HenshinProjUtil.OwnerMouseWorld(Projectile);
				Projectile.ai[0] = 1f;
				Projectile.netUpdate = true;
			}
			if (Projectile.localAI[0] == 0f && Projectile.ai[0] != 0f)
			{
				Projectile.localAI[0] = 1f;
				SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.7f }, Projectile.Center);
				if (!Main.dedServ)
				{
					for (int i = 0; i < 3; i++)
						Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-14f, 14f), 36f), DustID.Water,
							new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-3.5f, -1.5f)), 80, default, 1.2f);
				}
			}
			if (Projectile.timeLeft > 4)
				Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.45f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 联机端：位置尚未同步（ai0==0 且非拥有者）时不画，避免在玩家脚下闪一帧。
			if (Projectile.ai[0] == 0f)
				return false;
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SplashColumn/SplashColumn", AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SplashColumn/SplashColumn_Light", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			// 原点 = 水柱脚底（art 10,42 → px 20,84）；落在判定盒底边。
			Vector2 pos = (new Vector2(Projectile.Center.X, Projectile.Center.Y + Projectile.height * 0.5f) - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH - 4f);
			Color c = new(Math.Max(lightColor.R, (byte)130), Math.Max(lightColor.G, (byte)140), Math.Max(lightColor.B, (byte)160));
			Main.EntitySpriteDraw(tex, pos, src, c, 0f, origin, 1f, SpriteEffects.None, 0f);
			if (frame is 2 or 3)
			{
				HenshinFxDraw.BeginAdditive();
				Main.EntitySpriteDraw(_light.Value, pos, src, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}
}
