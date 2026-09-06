using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 爪击：身前平行爪痕。ai0=reachTiles（默认 ~3.5；龙爪 20）。
	/// </summary>
	public class ScratchSlashProj : HenshinMoveProj
	{
		private const int Lifetime = 14;
		private const float DefaultReachTiles = 3.5f;
		private const float LineSpacing = 16f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private float ReachPx
		{
			get
			{
				float tiles = Projectile.ai[0] > 0.5f ? Projectile.ai[0] : DefaultReachTiles;
				return tiles * 16f;
			}
		}

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

			if (Projectile.ai[1] == 0f)
				Projectile.ai[1] = owner.direction;
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float reach = ReachPx;

			Projectile.Center = owner.MountedCenter + new Vector2(dir * Math.Max(36f, reach * 0.55f), 0f);
			Projectile.velocity = Vector2.Zero;

			if (Projectile.timeLeft > Lifetime - 10)
			{
				float t = (Lifetime - Projectile.timeLeft) / 10f;
				for (int line = -1; line <= 1; line++)
				{
					float yBase = line * LineSpacing;
					for (int seg = 0; seg < 3; seg++)
					{
						float along = t * reach * 0.7f + seg * 8f;
						Vector2 pos = owner.MountedCenter + new Vector2(dir * along, yBase + t * 6f);
						Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, new Vector2(dir * 2.4f, 0.3f), 80, Color.White, 1.35f);
						d.noGravity = true;
						d.fadeIn = 0.9f;
						Dust ember = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(dir * 1.4f, 0.2f), 100, default, 1.2f);
						ember.noGravity = true;
					}
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float reach = ReachPx;
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = owner.MountedCenter + new Vector2(dir * reach, 0f);
			for (int line = -1; line <= 1; line++)
			{
				Vector2 o = origin + new Vector2(0f, line * LineSpacing);
				Vector2 e = tip + new Vector2(0f, line * LineSpacing);
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), o, e, 16f, ref point))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Blood, hit.HitDirection * 2.5f, -1.2f);
				d.noGravity = false;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float life = Projectile.timeLeft / (float)Lifetime;
			float rot = dir > 0 ? 0.4f : MathHelper.Pi - 0.4f;
			rot += MathHelper.Pi; // HitJagged tip +X → +180° 朝斩击方向
			int frame = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			float reach = ReachPx;
			int segs = reach > 80f ? 5 : 2;
			HenshinFxDraw.BeginAdditive();
			for (int i = 1; i <= segs; i++)
			{
				float u = i / (segs + 0.5f);
				Vector2 pos = owner.MountedCenter + new Vector2(dir * reach * u, 0f);
				HenshinFxDraw.DrawHitJaggedFrame(pos,
					HenshinFxDraw.WithAlpha(new Color(255, 200, 160), 0.7f * life), 0.75f + u * 0.35f, rot, (frame + i) % HenshinFxDraw.HitJaggedFrames);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
