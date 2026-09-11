using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 胡地技能1：在鼠标处生成精神强念弹（延迟追踪）。无伤导演。
	/// ai0：发数（默认 3）；ai1≥0.5：弹可穿墙。
	/// </summary>
	public class AlakazamPsychicDirectorProj : HenshinMoveProj
	{
		private bool _spawned;

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

		public override void OnSpawn(IEntitySource source) => TrySpawnBolts();

		public override void AI() => TrySpawnBolts();

		private void TrySpawnBolts()
		{
			if (_spawned || Projectile.owner != Main.myPlayer)
				return;
			_spawned = true;

			int count = Projectile.ai[0] > 0.5f ? (int)Projectile.ai[0] : 3;
			count = Math.Clamp(count, 1, 16);
			bool pierceTiles = Projectile.ai[1] > 0.5f;

			Vector2 mouse = HenshinProjUtil.OwnerMouseWorld(Projectile);
			int boltDmg = Math.Max(1, Projectile.damage);
			for (int i = 0; i < count; i++)
			{
				Vector2 offset = Main.rand.NextVector2Circular(28f, 28f);
				Vector2 spawn = mouse + offset;
				Vector2 vel = Main.rand.NextVector2Circular(2.2f, 2.2f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<AlakazamPsychicBoltProj>(), boltDmg, Projectile.knockBack, Projectile.owner,
					pierceTiles ? 1f : 0f);
				if (id >= 0)
				{
					Main.projectile[id].Center = spawn;
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.EasyCrit = EasyCrit;
						tagged.IgnoreDefensePartial = IgnoreDefensePartial;
					}
				}
			}
			SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = 0.15f }, mouse);
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 胡地/超梦精神强念单发：RainbowRod 壳；默认 tileCollide；ai0≥0.5 穿墙。
	/// </summary>
	public class AlakazamPsychicBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private const int HomingDelay = 26;
		private static readonly Color Tint = new Color(230, 140, 255);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.RainbowRodBullet;

		private bool PierceTiles => Projectile.ai[0] > 0.5f;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
			Projectile.scale = 1.25f;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.RainbowRodBullet);
			Projectile.penetrate = 1;
			Projectile.tileCollide = !PierceTiles;
		}

		public override void AI()
		{
			Projectile.tileCollide = !PierceTiles;
			Projectile.penetrate = 1;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.localAI[0]++;

			if (Projectile.localAI[0] >= HomingDelay)
			{
				Homing = true;
				HomingTurnRate = 0.18f;
				if (Projectile.velocity.LengthSquared() < 1f)
					Projectile.velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
				else if (Projectile.velocity.Length() < 8f)
					Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 8f;
			}

			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);

			if (Main.rand.NextBool())
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Projectile.velocity * 0.1f, 100, Tint, 1.35f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Tint.ToVector3() * 0.4f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.RainbowRodBullet);
			Rectangle frame = tex.Frame();
			Vector2 origin = frame.Size() * 0.5f;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Tint, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>
	/// 胡地大招导演：为屏幕内每个敌对 NPC 生成一发 Nightglow 预知未来弹。
	/// </summary>
	public class FutureSightMarkDirectorProj : HenshinMoveProj
	{
		private bool _spawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 12;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void OnSpawn(IEntitySource source) => TryMark();

		public override void AI() => TryMark();

		private void TryMark()
		{
			if (_spawned || Projectile.owner != Main.myPlayer)
				return;
			_spawned = true;

			Rectangle screen = new Rectangle(
				(int)Main.screenPosition.X,
				(int)Main.screenPosition.Y,
				Main.screenWidth,
				Main.screenHeight);

			int boltDmg = Math.Max(1, Projectile.damage);
			int marked = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.townNPC || npc.life <= 0)
					continue;
				if (!screen.Intersects(npc.Hitbox))
					continue;

				Vector2 spawn = npc.Center + Main.rand.NextVector2Circular(18f, 18f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, Vector2.Zero,
					ModContent.ProjectileType<FutureSightNightglowBoltProj>(), boltDmg, Projectile.knockBack, Projectile.owner,
					npc.whoAmI, npc.type);
				if (id >= 0)
				{
					Main.projectile[id].Center = spawn;
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.EasyCrit = EasyCrit;
						tagged.IgnoreDefensePartial = true;
					}
					marked++;
				}
			}

			if (marked > 0)
				SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = -0.1f }, HenshinProjUtil.OwnerMouseWorld(Projectile));
			Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 预知未来 Nightglow 弹：0–60 tick 淡入无伤；之后追踪标记 NPC；命中紫爆。
	/// ai0 = 标记 whoAmI；ai1 = 标记 type（校验）。
	/// </summary>
	public class FutureSightNightglowBoltProj : HenshinMoveProj
	{
		private const int FadeTicks = 60;
		private const int TotalLife = 120;
		private const float RetargetRange = 960f;
		private const float FlightSpeed = 11f;
		private bool _exploding;
		private int _explodeAge;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenMagicItemShot;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = TotalLife;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.scale = 1.15f;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.FairyQueenMagicItemShot);
			Projectile.timeLeft = TotalLife;
			Projectile.velocity = Vector2.Zero;
		}

		private int Age => TotalLife - Projectile.timeLeft;

		public override bool? CanDamage()
		{
			if (_exploding || Age < FadeTicks)
				return false;
			return null;
		}

		public override void AI()
		{
			if (_exploding)
			{
				_explodeAge++;
				Projectile.velocity = Vector2.Zero;
				Projectile.alpha = 255;
				if (_explodeAge >= 12)
					Projectile.Kill();
				return;
			}

			float fade = MathHelper.Clamp(Age / (float)FadeTicks, 0f, 1f);
			Projectile.alpha = (int)((1f - fade) * 255f);
			Projectile.rotation = Main.GlobalTimeWrappedHourly * 4f + Projectile.whoAmI;

			if (Age < FadeTicks)
			{
				Projectile.velocity *= 0.85f;
				if (Main.rand.NextBool(3))
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
						Main.rand.NextVector2Circular(0.6f, 0.6f), 120, new Color(200, 120, 255), 1.1f);
					d.noGravity = true;
				}
				Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.15f, 0.55f) * fade);
				return;
			}

			NPC target = ResolveTarget();
			if (target != null)
			{
				Vector2 desired = target.Center - Projectile.Center;
				if (desired.LengthSquared() > 0.01f)
				{
					desired.Normalize();
					if (Projectile.velocity.LengthSquared() < 1f)
						Projectile.velocity = desired * FlightSpeed;
					else
					{
						float speed = Math.Max(FlightSpeed, Projectile.velocity.Length());
						Projectile.velocity = Vector2.Normalize(Vector2.Lerp(
							Vector2.Normalize(Projectile.velocity), desired, 0.22f)) * speed;
					}
				}
			}
			else if (Projectile.velocity.LengthSquared() < 0.25f)
			{
				Projectile.velocity = Main.rand.NextVector2CircularEdge(FlightSpeed, FlightSpeed);
			}

			if (Main.rand.NextBool())
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					-Projectile.velocity * 0.15f, 100, new Color(210, 140, 255), 1.25f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0.75f);
		}

		private NPC ResolveTarget()
		{
			int who = (int)Projectile.ai[0];
			int type = (int)Projectile.ai[1];
			if (who >= 0 && who < Main.maxNPCs)
			{
				NPC marked = Main.npc[who];
				if (marked.active && !marked.friendly && marked.life > 0 && marked.type == type)
					return marked;
			}

			NPC best = null;
			float bestDist = RetargetRange * RetargetRange;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.townNPC || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Projectile.DistanceSQ(n.Center);
				if (d < bestDist)
				{
					bestDist = d;
					best = n;
				}
			}
			if (best != null)
			{
				Projectile.ai[0] = best.whoAmI;
				Projectile.ai[1] = best.type;
			}
			return best;
		}

		private void BeginExplode(bool keepAliveForDraw)
		{
			if (_exploding)
				return;
			_exploding = true;
			_explodeAge = 0;
			Projectile.velocity = Vector2.Zero;
			if (keepAliveForDraw)
				Projectile.timeLeft = Math.Max(Projectile.timeLeft, 14);
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.35f }, Projectile.Center);
			for (int i = 0; i < 18; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(5f, 5f);
				Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 80, new Color(190, 100, 255), 1.5f).noGravity = true;
			}
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, Main.rand.NextVector2Circular(3.5f, 3.5f), 100, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => BeginExplode(keepAliveForDraw: true);

		public override void OnKill(int timeLeft)
		{
			if (!_exploding)
				BeginExplode(keepAliveForDraw: false);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_exploding)
			{
				float life = 1f - _explodeAge / 12f;
				float diam = 48f + (1f - life) * 40f;
				HenshinFxDraw.BeginAdditive();
				float circScale = HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.DiffusionCircle, diam);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.DiffusionCircle, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(180, 90, 255), 0.8f * life), circScale);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(210, 140, 255), 0.9f * life), 0.55f + (1f - life) * 0.35f);
				HenshinFxDraw.EndAdditive();
				return false;
			}

			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.FairyQueenMagicItemShot);
			Rectangle frame = tex.Frame();
			Vector2 origin = frame.Size() * 0.5f;
			float a = 1f - Projectile.alpha / 255f;
			Color c = new Color(230, 180, 255, 255) * a;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, c, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>
	/// 胡地真气拳：朝鼠标挥拳；白烟上飘；无石爆。
	/// </summary>
	public class FocusPunchProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private const float Reach = 10f * 16f;
		private const float GloveScale = 2.8f;
		private Vector2 _dir;
		private Vector2 _origin;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BoxingGlove;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.ownerHitCheck = false;
		}

		private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Main.instance.LoadProjectile(ProjectileID.BoxingGlove);
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				_origin = owner.MountedCenter;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, owner.Center);
			}

			float u = MathHelper.SmoothStep(0f, 1f, Progress);
			Projectile.Center = _origin + _dir * (40f + Reach * u);

			for (int i = 0; i < 2; i++)
			{
				Vector2 up = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-4.5f, -1.8f));
				Dust cloud = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 10f),
					DustID.Cloud, up, 120, Color.White, 1.35f);
				cloud.noGravity = true;
				if (Main.rand.NextBool())
				{
					Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 8f),
						DustID.Smoke, up * 0.85f, 140, Color.White, 1.15f);
					smoke.noGravity = true;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float rot = _dir.ToRotation();
			float life = Projectile.timeLeft / (float)Lifetime;

			HenshinFxDraw.BeginAdditive();
			Vector2 fogPos = Projectile.Center + new Vector2(0f, -18f - (1f - life) * 28f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.Fog, fogPos,
				HenshinFxDraw.WithAlpha(Color.White, 0.35f * life), 0.55f + (1f - life) * 0.25f);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(240, 245, 255), 0.55f * life), 0.9f);
			HenshinFxDraw.EndAdditive();

			Texture2D glove = ProjectileBorrow.RequestProjectileTexture(ProjectileID.BoxingGlove);
			Main.EntitySpriteDraw(glove, Projectile.Center - Main.screenPosition, null, Color.White,
				rot + MathHelper.PiOver2, glove.Size() * 0.5f, GloveScale, SpriteEffects.None);
			return false;
		}
	}
}
