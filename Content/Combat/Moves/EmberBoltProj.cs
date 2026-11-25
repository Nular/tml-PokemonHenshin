using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	public class EmberBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private const int Frames = 4;
		private const int TicksPerFrame = 4;
		private static Asset<Texture2D> _sheet;
		private static Asset<Texture2D> _light;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 4;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
			Projectile.aiStyle = -1;
			Projectile.light = 0.5f;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (++Projectile.frameCounter >= TicksPerFrame)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Frames;
			}
			Projectile.velocity.Y += 0.08f;
			if (Projectile.velocity.Y > 12f)
				Projectile.velocity.Y = 12f;

			if (Projectile.wet)
			{
				for (int i = 0; i < 8; i++)
					Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -1f, 150, default, 1f).noGravity = true;
				Projectile.Kill();
				return;
			}

			Lighting.AddLight(Projectile.Center, 0.9f, 0.45f, 0.1f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 180);
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
			for (int i = 0; i < 3; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 80, default, 1.3f);
				d.noGravity = true;
				d.velocity = Main.rand.NextVector2Circular(3f, 3f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/EmberBolt/EmberBolt", AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/EmberBolt/EmberBolt_Light", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			Rectangle src = new(0, frameH * Projectile.frame, tex.Width, frameH);
			Rectangle lightSrc = new(0, frameH * Projectile.frame, _light.Value.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(24f, 10f);
			// 贴图朝右；朝左半圈上下翻转，原点 Y 随之镜像。
			bool left = System.Math.Cos(Projectile.rotation) < 0;
			SpriteEffects fx = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
			if (left)
				origin.Y = frameH - origin.Y;
			Main.EntitySpriteDraw(tex, pos, src, Color.White, Projectile.rotation, origin, 1f, fx, 0f);
			HenshinFxDraw.BeginAdditive();
			Main.EntitySpriteDraw(_light.Value, pos, lightSrc, Color.White * 0.7f, Projectile.rotation, origin, 1f, fx, 0f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
