using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 猛撞：闪焰式加减速冲刺（无火）；命中刷灰日耀爆炸 VFX + 土黄尘。
	/// ai2 = 射程格数（默认 28）。
	/// </summary>
	public class TakeDownLungeProj : HenshinMoveProj
	{
		private const int BrakeTicks = 10;
		private const float DefaultReach = 28f;

		private Vector2 _dir;
		private int _dashLife;
		private int _lifetime;
		private float _speed;
		private float _cruiseSpeed;
		private Vector2 _lastTrail;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		private bool InBrake => Projectile.timeLeft <= BrakeTicks;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				float reach = Projectile.ai[2] > 0.5f ? Projectile.ai[2] : DefaultReach;
				_dashLife = (int)MathHelper.Clamp(reach * 0.75f, 18f, 40f);
				_speed = reach * 16f / _dashLife;
				_lifetime = _dashLife + BrakeTicks;
				Projectile.timeLeft = _lifetime;
				Projectile.localNPCHitCooldown = _dashLife;
				_cruiseSpeed = Math.Max(p.maxRunSpeed, 6f);
				_lastTrail = p.Center;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.15f }, p.Center);
			}

			if (Projectile.owner == Main.myPlayer)
			{
				if (InBrake)
				{
					float t = 1f - Projectile.timeLeft / (float)BrakeTicks;
					float targetSpd = MathHelper.Lerp(_speed, _cruiseSpeed, MathHelper.SmoothStep(0f, 1f, t));
					p.velocity = Vector2.Lerp(p.velocity, _dir * targetSpd, 0.35f);
				}
				else
					p.velocity = _dir * _speed;
			}

			Projectile.Center = p.Center;
			if (!InBrake && Vector2.DistanceSquared(_lastTrail, p.Center) > 20f * 20f)
			{
				_lastTrail = p.Center;
				Vector2 perp = new Vector2(-_dir.Y, _dir.X);
				for (int lane = -2; lane <= 2; lane++)
				{
					Dust d = Dust.NewDustPerfect(p.Center + perp * (lane * 8f), DustID.Iron,
						-_dir * 1.2f, 80, new Color(180, 150, 90), 1.25f);
					d.noGravity = true;
				}
			}

			int immuneGate = BrakeTicks + Math.Max(8, _dashLife * 5 / 8);
			if (Projectile.timeLeft >= immuneGate && Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 15);
			}
		}

		public override bool? CanDamage() => InBrake ? false : null;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnGraySolarBursts(target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			Player p = Main.player[Projectile.owner];
			if (p.active && Projectile.owner == Main.myPlayer && p.velocity.Length() > _cruiseSpeed * 1.15f)
				p.velocity = _dir * _cruiseSpeed;
		}

		private void SpawnGraySolarBursts(Vector2 at)
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			Main.instance.LoadProjectile(ProjectileID.SolarWhipSwordExplosion);
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.2f }, at);
			int n = Main.rand.Next(4, 7);
			for (int i = 0; i < n; i++)
			{
				Vector2 off = Main.rand.NextVector2Circular(28f, 28f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at + off, Vector2.Zero,
					ModContent.ProjectileType<GraySolarBurstVfxProj>(), 0, 0f, Projectile.owner);
				if (id >= 0)
					Main.projectile[id].Center = at + off;
			}
			for (int i = 0; i < 14; i++)
			{
				Dust d = Dust.NewDustPerfect(at, DustID.Sand, Main.rand.NextVector2Circular(5f, 5f),
					80, new Color(200, 160, 70), 1.4f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = InBrake
				? Projectile.timeLeft / (float)BrakeTicks * 0.45f
				: Math.Min(1f, Projectile.timeLeft / (float)Math.Max(1, _dashLife));
			HenshinFxDraw.BeginAdditive();
			for (int i = 1; i <= 4; i++)
			{
				Vector2 pos = Projectile.Center - _dir * (i * 14f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos,
					HenshinFxDraw.WithAlpha(new Color(200, 190, 160), 0.4f * life * (1f - i * 0.12f)), 0.3f);
			}
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(220, 210, 180), 0.55f * life), 0.5f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>灰日耀爆炸：仅贴图 VFX，无伤害。</summary>
	public class GraySolarBurstVfxProj : HenshinMoveProj
	{
		private const int Life = 18;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.damage = 0;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.SolarWhipSwordExplosion);
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			Projectile.frameCounter++;
			int frames = Math.Max(1, Main.projFrames[ProjectileID.SolarWhipSwordExplosion]);
			if (Projectile.frameCounter >= 2)
			{
				Projectile.frameCounter = 0;
				Projectile.frame++;
				if (Projectile.frame >= frames)
					Projectile.Kill();
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.SolarWhipSwordExplosion);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.SolarWhipSwordExplosion]);
			int frame = Math.Clamp(Projectile.frame, 0, frames - 1);
			Rectangle src = tex.Frame(1, frames, 0, frame);
			float life = Projectile.timeLeft / (float)Life;
			Color tint = new Color(160, 160, 165, (int)(220 * life));
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, tint,
				0f, src.Size() * 0.5f, 0.85f + (1f - life) * 0.25f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// 龙之俯冲 / 画龙点睛：闪焰式冲刺 + 路径星尘龙持续伤。
	/// ai2：0=俯冲（半透明短龙）；1=画龙点睛（更长、全黑不透明）。
	/// </summary>
	public class StardustPathLungeProj : HenshinMoveProj
	{
		private const int BrakeTicks = 10;
		private const int MaxSamples = 48;

		private Vector2 _dir;
		private int _dashLife;
		private int _lifetime;
		private float _speed;
		private float _cruiseSpeed;
		private float _reachTiles;
		private readonly Vector2[] _path = new Vector2[MaxSamples];
		private int _pathCount;
		private Vector2 _lastSample;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.StardustDragon4;

		private bool IsAscent => Projectile.ai[2] > 0.5f;
		private bool InBrake => Projectile.timeLeft <= BrakeTicks;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Main.instance.LoadProjectile(ProjectileID.StardustDragon2);
				Main.instance.LoadProjectile(ProjectileID.StardustDragon3);
				Main.instance.LoadProjectile(ProjectileID.StardustDragon4);

				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				_reachTiles = IsAscent ? 48f : 28f;
				_dashLife = (int)MathHelper.Clamp(_reachTiles * 0.7f, 22f, 48f);
				_speed = _reachTiles * 16f / _dashLife;
				_lifetime = _dashLife + BrakeTicks;
				Projectile.timeLeft = _lifetime;
				Projectile.localNPCHitCooldown = 8;
				_cruiseSpeed = Math.Max(p.maxRunSpeed, 6f);
				_lastSample = p.Center;
				PushSample(p.Center);
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, p.Center);
			}

			if (Projectile.owner == Main.myPlayer)
			{
				if (InBrake)
				{
					float t = 1f - Projectile.timeLeft / (float)BrakeTicks;
					float targetSpd = MathHelper.Lerp(_speed, _cruiseSpeed, MathHelper.SmoothStep(0f, 1f, t));
					p.velocity = Vector2.Lerp(p.velocity, _dir * targetSpd, 0.35f);
				}
				else
					p.velocity = _dir * _speed;
			}

			Projectile.Center = p.Center;
			if (!InBrake && Vector2.DistanceSquared(_lastSample, p.Center) > 14f * 14f)
			{
				_lastSample = p.Center;
				PushSample(p.Center);
			}

			int immuneGate = BrakeTicks + Math.Max(8, _dashLife * 5 / 8);
			if (Projectile.timeLeft >= immuneGate && Projectile.owner == Main.myPlayer)
			{
				p.immune = true;
				p.immuneTime = Math.Max(p.immuneTime, 15);
			}
		}

		private void PushSample(Vector2 pos)
		{
			if (_pathCount < MaxSamples)
			{
				_path[_pathCount++] = pos;
				return;
			}
			for (int i = 1; i < MaxSamples; i++)
				_path[i - 1] = _path[i];
			_path[MaxSamples - 1] = pos;
		}

		public override bool? CanDamage() => InBrake ? false : null;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_pathCount < 2)
				return projHitbox.Intersects(targetHitbox);
			float width = IsAscent ? 36f : 28f;
			for (int i = 0; i < _pathCount - 1; i++)
			{
				float _ = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
					    _path[i], _path[i + 1], width, ref _))
					return true;
			}
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			Player p = Main.player[Projectile.owner];
			if (p.active && Projectile.owner == Main.myPlayer && p.velocity.Length() > _cruiseSpeed * 1.15f)
				p.velocity = _dir * _cruiseSpeed;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_pathCount < 2)
				return false;

			int[] segTypes =
			{
				ProjectileID.StardustDragon4,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon2,
				ProjectileID.StardustDragon3,
				ProjectileID.StardustDragon4
			};

			float life = InBrake ? Projectile.timeLeft / (float)BrakeTicks : 1f;
			int drawCount = Math.Min(_pathCount - 1, IsAscent ? MaxSamples : 28);
			int start = Math.Max(0, _pathCount - 1 - drawCount);

			for (int i = start; i < _pathCount - 1; i++)
			{
				Vector2 a = _path[i];
				Vector2 b = _path[i + 1];
				Vector2 mid = (a + b) * 0.5f;
				Vector2 delta = b - a;
				float rot = delta.LengthSquared() > 0.01f ? delta.ToRotation() : _dir.ToRotation();
				int segIdx = (i - start) % segTypes.Length;
				int texId = segTypes[segIdx];
				Texture2D tex = ProjectileBorrow.RequestProjectileTexture(texId);
				int frames = Math.Max(1, Main.projFrames[texId]);
				Rectangle src = tex.Frame(1, frames, 0, 0);
				float scale = IsAscent ? 1.05f : 0.9f;

				Color tint;
				if (IsAscent)
				{
					// 画龙点睛：全骨节纯黑不透明
					tint = new Color(0, 0, 0, 255);
				}
				else
				{
					tint = new Color(160, 200, 255, (int)(140 * life));
				}

				Main.EntitySpriteDraw(tex, mid - Main.screenPosition, src, tint,
					rot + MathHelper.PiOver2, src.Size() * 0.5f, scale, SpriteEffects.None);
			}

			if (!IsAscent)
			{
				HenshinFxDraw.BeginAdditive();
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(140, 190, 255), 0.4f * life), 0.55f);
				HenshinFxDraw.EndAdditive();
			}
			return false;
		}
	}

	/// <summary>暗影抓：龙爪式 20 格紫爪 + 影炎。</summary>
	public class ShadowClawSlashProj : HenshinMoveProj
	{
		private const int Lifetime = 14;
		private const float ReachTiles = 20f;
		private const float LineSpacing = 16f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
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
			float reach = ReachTiles * 16f;
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
						Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame, new Vector2(dir * 2.4f, 0.3f),
							80, new Color(180, 80, 255), 1.35f);
						d.noGravity = true;
					}
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float reach = ReachTiles * 16f;
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = origin + new Vector2(dir * reach, 0f);
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
			target.AddBuff(BuffID.ShadowFlame, 180);
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(4f, 4f),
					60, new Color(180, 80, 255), 1.4f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			int dir = Projectile.ai[1] >= 0f ? 1 : -1;
			float life = Projectile.timeLeft / (float)Lifetime;
			float rot = dir > 0 ? 0.4f : MathHelper.Pi - 0.4f;
			rot += MathHelper.Pi;
			int frame = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 3, HenshinFxDraw.HitJaggedFrames);
			float reach = ReachTiles * 16f;
			HenshinFxDraw.BeginAdditive();
			for (int i = 1; i <= 5; i++)
			{
				float u = i / 5.5f;
				Vector2 pos = owner.MountedCenter + new Vector2(dir * reach * u, 0f);
				HenshinFxDraw.DrawHitJaggedFrame(pos,
					HenshinFxDraw.WithAlpha(new Color(200, 100, 255), 0.75f * life), 0.75f + u * 0.35f, rot,
					(frame + i) % HenshinFxDraw.HitJaggedFrames);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>恶之波动：身周一次喷出 32 枚追踪暗影球。</summary>
	public class DarkPulseBarrageProj : HenshinMoveProj
	{
		private const int Count = 32;

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
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.Center;
			if (Projectile.localAI[0] == 0f && Projectile.owner == Main.myPlayer)
			{
				Projectile.localAI[0] = 1f;
				SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.4f }, owner.Center);
				int boltDmg = Math.Max(1, Projectile.damage);
				for (int i = 0; i < Count; i++)
				{
					float ang = MathHelper.TwoPi * i / Count;
					Vector2 vel = ang.ToRotationVector2() * 9f;
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, vel,
						ModContent.ProjectileType<DarkPulseHomingBallProj>(), boltDmg, Projectile.knockBack * 0.5f,
						Projectile.owner);
					if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						tagged.EasyCrit = EasyCrit;
				}
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>恶波动子弹：穿墙追踪，命中紫爆 AoE。</summary>
	public class DarkPulseHomingBallProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private const float BaseDiameter = 22f;
		private static readonly Color Fill = new(0x22, 0x00, 0x33, 255);
		private static readonly Color Border = new(0x33, 0x00, 0x66, 255);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamHostile;

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
			Homing = true;
			HomingTurnRate = 0.14f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.ShadowBeamHostile);
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			if (Main.rand.NextBool(2))
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Vector2.Zero, 100,
					new Color(160, 60, 220), 1.1f).noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnBurst(target.Center);
			if (Projectile.owner == Main.myPlayer)
			{
				int aoe = Math.Max(1, Projectile.damage / 2);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<DarkPulseBurstAoEProj>(), aoe, 1f, Projectile.owner);
			}
		}

		public override void OnKill(int timeLeft) => SpawnBurst(Projectile.Center);

		private static void SpawnBurst(Vector2 at)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Pitch = -0.3f, Volume = 0.55f }, at);
			for (int i = 0; i < 12; i++)
				Dust.NewDustPerfect(at, DustID.Shadowflame, Main.rand.NextVector2Circular(4f, 4f),
					60, new Color(180, 80, 255), 1.3f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.DrawOpaqueDisk(Projectile.Center, BaseDiameter, Fill, Border);
			return false;
		}
	}

	public class DarkPulseBurstAoEProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			if (Projectile.timeLeft == 7)
			{
				Vector2 c = Projectile.Center;
				Projectile.position = c - Projectile.Size * 0.5f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 8f;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(160, 60, 220), 0.55f * life), 0.7f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>彗星拳：真气拳式拳套 + 命中 3 枚星星吉他从天而降。</summary>
	public class CometPunchProj : HenshinMoveProj
	{
		private const int Lifetime = 18;
		private const float Reach = 10f * 16f;
		private const float GloveScale = 2.8f;
		private Vector2 _dir;
		private Vector2 _origin;
		private bool _starsSpawned;

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
				Main.instance.LoadProjectile(ProjectileID.StarWrath);
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				_origin = owner.MountedCenter;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.1f }, owner.Center);
			}

			float u = MathHelper.SmoothStep(0f, 1f, Progress);
			Projectile.Center = _origin + _dir * (40f + Reach * u);

			Dust iron = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 10f),
				DustID.Iron, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-3f, -1f)),
				100, new Color(200, 200, 210), 1.2f);
			iron.noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnStars(target.Center);
		}

		private void SpawnStars(Vector2 target)
		{
			if (_starsSpawned || Projectile.owner != Main.myPlayer)
				return;
			_starsSpawned = true;
			int starDmg = Math.Max(1, Projectile.damage * 2 / 3);
			for (int i = 0; i < 3; i++)
			{
				Vector2 spawn = target + new Vector2(Main.rand.NextFloat(-48f, 48f), -320f - i * 40f);
				Vector2 vel = (target - spawn).SafeNormalize(Vector2.UnitY) * 18f;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<CometStarFallProj>(), starDmg, 2f, Projectile.owner,
					target.X, target.Y);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float rot = _dir.ToRotation();
			float life = Projectile.timeLeft / (float)Lifetime;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(200, 210, 230), 0.55f * life), 0.9f);
			HenshinFxDraw.EndAdditive();
			Texture2D glove = ProjectileBorrow.RequestProjectileTexture(ProjectileID.BoxingGlove);
			Color gloveTint = new Color(210, 215, 230, 255);
			Main.EntitySpriteDraw(glove, Projectile.Center - Main.screenPosition, null, gloveTint,
				rot + MathHelper.PiOver2, glove.Size() * 0.5f, GloveScale, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>彗星拳星星：StarWrath 壳，穿墙下落（朝下）。</summary>
	public class CometStarFallProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.StarWrath;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.StarWrath);
		}

		public override void AI()
		{
			Vector2 aim = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			if (aim.LengthSquared() > 1f)
			{
				Vector2 want = (aim - Projectile.Center).SafeNormalize(Projectile.velocity) * 18f;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.12f);
			}
			// StarWrath 贴图默认朝上；速度朝下时减 Pi/2 使头领路
			Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
			Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero, 100, default, 1.1f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.StarWrath);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.StarWrath]);
			Rectangle src = tex.Frame(1, frames, 0, 0);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, Color.White,
				Projectile.rotation, src.Size() * 0.5f, 1.1f, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 180, 220), 0.4f), 0.35f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	public class OutrageFireballProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CultistBossFireBall;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 100;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
			Homing = true;
			HomingTurnRate = 0.12f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.CultistBossFireBall);
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Projectile.rotation += 0.2f;
			Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Vector2.Zero, 80, default, 1.2f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.owner == Main.myPlayer)
			{
				int aoe = Math.Max(1, Projectile.damage / 2);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<OutrageFireBurstAoEProj>(), aoe, 2f, Projectile.owner);
			}
			SoundEngine.PlaySound(SoundID.Item14, target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(4f, 4f),
					60, default, 1.3f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.CultistBossFireBall);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.CultistBossFireBall]);
			Rectangle src = tex.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 3) % frames);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, Color.White,
				Projectile.rotation, src.Size() * 0.5f, 1.1f, SpriteEffects.None);
			return false;
		}
	}

	public class OutrageFireBurstAoEProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 80;
			Projectile.height = 80;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 10;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / 10f;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 140, 40), 0.65f * life), 0.85f);
			int fireFrame = HenshinFxDraw.AgeFrame(10, Projectile.timeLeft, 2, HenshinFxDraw.FireFrames);
			HenshinFxDraw.DrawFireFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 180, 60), 0.8f * life), 0.7f, 0f, fireFrame);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>流星群：64 枚 StarWrath 壳自天而降追踪。</summary>
	public class DracoMeteorDirectorProj : HenshinMoveProj
	{
		private const int Total = 64;
		private int _spawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			if (_spawned >= Total)
			{
				if (Projectile.timeLeft > 20)
					Projectile.timeLeft = 20;
				return;
			}
			// 约每 1~2 tick 一发，尽快洒满
			int perTick = 2;
			for (int n = 0; n < perTick && _spawned < Total; n++)
			{
				Vector2 target = Projectile.Center + Main.rand.NextVector2Circular(160f, 80f);
				Vector2 spawn = target + new Vector2(Main.rand.NextFloat(-80f, 80f), -280f - Main.rand.NextFloat(0f, 120f));
				Vector2 vel = (target - spawn).SafeNormalize(Vector2.UnitY) * 16f;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<DracoStarWrathProj>(), Projectile.damage, Projectile.knockBack,
					Projectile.owner, target.X, target.Y);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_spawned++;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class DracoStarWrathProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.StarWrath;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 100;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
			Homing = true;
			HomingTurnRate = 0.1f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.StarWrath);
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			// 贴图朝下领路（修正原先 +Pi/2 导致头尾颠倒）
			Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
			Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero, 100, default, 1.15f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnGoldPinkBurst(target.Center);
		}

		public override void OnKill(int timeLeft) => SpawnGoldPinkBurst(Projectile.Center);

		private static void SpawnGoldPinkBurst(Vector2 at)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Pitch = 0.25f }, at);
			for (int i = 0; i < 16; i++)
			{
				Color c = Main.rand.NextBool() ? new Color(255, 200, 80) : new Color(255, 120, 200);
				Dust.NewDustPerfect(at, DustID.Enchanted_Gold, Main.rand.NextVector2Circular(5f, 5f),
					60, c, 1.4f).noGravity = true;
			}
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(at, DustID.PinkTorch, Main.rand.NextVector2Circular(4f, 4f),
					80, default, 1.25f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.StarWrath);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.StarWrath]);
			Rectangle src = tex.Frame(1, frames, 0, 0);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, Color.White,
				Projectile.rotation, src.Size() * 0.5f, 1.1f, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(255, 180, 220), 0.45f), 0.4f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>神鸟猛击：0.5s 吟唱（无敌+减速）后冲刺；命中 6 鸟交叉。</summary>
	public class SkyAttackLungeProj : HenshinMoveProj
	{
		private const int ChargeTicks = 30;
		private const int BrakeTicks = 10;
		private const float ReachTiles = 32f;

		private Vector2 _dir;
		private int _dashLife;
		private int _lifetime;
		private float _speed;
		private float _cruiseSpeed;
		private int _phase; // 0 charge, 1 dash
		private bool _birdsSpawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		private bool InBrake => _phase == 1 && Projectile.timeLeft <= BrakeTicks;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
				_phase = 0;
				Projectile.timeLeft = ChargeTicks + 50;
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.2f }, p.Center);
			}

			Projectile.Center = p.Center;

			if (_phase == 0)
			{
				if (Projectile.owner == Main.myPlayer)
				{
					p.velocity *= 0.85f;
					p.AddBuff(BuffID.Slow, 15);
					p.immune = true;
					p.immuneTime = Math.Max(p.immuneTime, 20);
				}
				Dust.NewDustPerfect(p.Center + Main.rand.NextVector2Circular(28f, 28f), DustID.Cloud,
					Vector2.Zero, 100, new Color(220, 240, 255), 1.3f).noGravity = true;

				if (Projectile.localAI[1]++ >= ChargeTicks)
				{
					_phase = 1;
					_dashLife = (int)MathHelper.Clamp(ReachTiles * 0.75f, 18f, 40f);
					_speed = ReachTiles * 16f / _dashLife;
					_lifetime = _dashLife + BrakeTicks;
					Projectile.timeLeft = _lifetime;
					Projectile.localNPCHitCooldown = _dashLife;
					_cruiseSpeed = Math.Max(p.maxRunSpeed, 6f);
					SoundEngine.PlaySound(SoundID.Item1, p.Center);
				}
				return;
			}

			if (Projectile.owner == Main.myPlayer)
			{
				if (InBrake)
				{
					float t = 1f - Projectile.timeLeft / (float)BrakeTicks;
					float targetSpd = MathHelper.Lerp(_speed, _cruiseSpeed, MathHelper.SmoothStep(0f, 1f, t));
					p.velocity = Vector2.Lerp(p.velocity, _dir * targetSpd, 0.35f);
				}
				else
					p.velocity = _dir * _speed;

				int immuneGate = BrakeTicks + Math.Max(8, _dashLife * 5 / 8);
				if (Projectile.timeLeft >= immuneGate)
				{
					p.immune = true;
					p.immuneTime = Math.Max(p.immuneTime, 15);
				}
			}
		}

		public override bool? CanDamage() => _phase != 1 || InBrake ? false : null;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SpawnCrossBirds(target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			Player p = Main.player[Projectile.owner];
			if (p.active && Projectile.owner == Main.myPlayer && _phase == 1 && p.velocity.Length() > _cruiseSpeed + 1f)
				p.velocity = _dir * _cruiseSpeed;
			if (!_birdsSpawned && Projectile.owner == Main.myPlayer && _phase == 1)
				SpawnCrossBirds(Projectile.Center);
		}

		private void SpawnCrossBirds(Vector2 at)
		{
			if (_birdsSpawned)
				return;
			_birdsSpawned = true;
			Main.instance.LoadProjectile(ProjectileID.Raven);
			SoundEngine.PlaySound(SoundID.Item102, at);
			int birdDmg = Math.Max(1, Projectile.damage / 4);
			for (int i = 0; i < 6; i++)
			{
				float ang = MathHelper.TwoPi * i / 6f + 0.2f;
				Vector2 vel = ang.ToRotationVector2() * 13f;
				if (i % 2 == 1)
					vel = -vel.RotatedBy(0.35f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at - vel * 8f, vel,
					ModContent.ProjectileType<BraveBirdCrossProj>(), birdDmg, 1.5f, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinFxDraw.BeginAdditive();
			if (_phase == 0)
			{
				float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(200, 230, 255), 0.55f * pulse), 0.9f);
			}
			else
			{
				float life = InBrake ? 0.35f : 0.7f;
				for (int i = 1; i <= 4; i++)
				{
					Vector2 pos = Projectile.Center - _dir * (i * 16f);
					HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, pos,
						HenshinFxDraw.WithAlpha(new Color(230, 245, 255), 0.4f * life), 0.35f);
				}
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 超梦精神击破：指针 16 格内最近敌；0.5s 内在其身周 8 格渐显 64 暗影球，然后齐冲向目标。
	/// </summary>
	public class MewtwoPsystrikeDirectorProj : HenshinMoveProj
	{
		private const int SpawnTicks = 30;
		private const int TotalOrbs = 64;
		private const float SelectRadius = 16f * 16f;
		private const float OrbOrbitRadius = 8f * 16f;

		private int _targetWho = -1;
		private int _spawned;
		private int _goAt;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = SpawnTicks + 8;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_goAt = (int)Main.GameUpdateCount + SpawnTicks;
				_targetWho = FindNearestHostile(HenshinProjUtil.OwnerMouseWorld(Projectile), SelectRadius);
				if (_targetWho < 0)
				{
					if (Projectile.owner == Main.myPlayer)
						Projectile.Kill();
					return;
				}
				SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f }, Main.npc[_targetWho].Center);
			}

			if (_targetWho < 0 || !Main.npc[_targetWho].active)
			{
				_targetWho = FindNearestHostile(Projectile.Center, SelectRadius * 1.5f);
				if (_targetWho < 0)
				{
					if (Projectile.owner == Main.myPlayer)
						Projectile.Kill();
					return;
				}
			}

			NPC target = Main.npc[_targetWho];
			Projectile.Center = target.Center;

			if (Projectile.owner == Main.myPlayer && _spawned < TotalOrbs)
			{
				int age = SpawnTicks - Math.Max(0, Projectile.timeLeft - 8);
				int shouldHave = Math.Clamp((int)(TotalOrbs * (age / (float)SpawnTicks)), 0, TotalOrbs);
				if (Projectile.timeLeft <= 8)
					shouldHave = TotalOrbs;

				while (_spawned < shouldHave)
				{
					float ang = MathHelper.TwoPi * _spawned / TotalOrbs + Main.rand.NextFloat(-0.08f, 0.08f);
					float rad = Main.rand.NextFloat(OrbOrbitRadius * 0.25f, OrbOrbitRadius);
					Vector2 spawn = target.Center + ang.ToRotationVector2() * rad;
					float diam = Main.rand.NextFloat(14f, 36f);
					int orbDmg = Math.Max(1, Projectile.damage);
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, Vector2.Zero,
						ModContent.ProjectileType<MewtwoPsystrikeOrbProj>(), orbDmg, Projectile.knockBack * 0.4f,
						Projectile.owner, _targetWho, _goAt, diam);
					if (id >= 0)
					{
						Main.projectile[id].Center = spawn;
						if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						{
							tagged.EasyCrit = EasyCrit;
							tagged.IgnoreDefensePartial = IgnoreDefensePartial;
						}
					}
					_spawned++;
				}
			}
		}

		private static int FindNearestHostile(Vector2 from, float rangePx)
		{
			int best = -1;
			float bestD = rangePx * rangePx;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Vector2.DistanceSquared(n.Center, from);
				if (d < bestD)
				{
					bestD = d;
					best = i;
				}
			}
			return best;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 精神击破暗影球：渐显后齐向标记目标冲刺；目标死亡则改追附近敌人。
	/// ai0=targetWho；ai1=goAt(GameUpdateCount)；ai2=直径。
	/// </summary>
	public class MewtwoPsystrikeOrbProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private static readonly Color Fill = new(0x22, 0x00, 0x33, 255);
		private static readonly Color Border = new(0x33, 0x00, 0x66, 255);

		private float _appear;
		private bool _charging;
		private Vector2 _anchor;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamHostile;

		private float Diameter => Projectile.ai[2] > 1f ? Projectile.ai[2] : 22f;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.ignoreWater = true;
			HomingTurnRate = 0.16f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => EnsureAnchor();

		private bool _anchorReady;

		private void EnsureAnchor()
		{
			if (_anchorReady)
				return;
			_anchorReady = true;
			Main.instance.LoadProjectile(ProjectileID.ShadowBeamHostile);
			_anchor = Projectile.Center;
		}

		public override void AI()
		{
			EnsureAnchor();
			int goAt = (int)Projectile.ai[1];
			_appear = MathHelper.Clamp(_appear + 0.08f, 0f, 1f);

			if (!_charging && Main.GameUpdateCount >= goAt)
			{
				_charging = true;
				Homing = true;
				SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.25f, Pitch = -0.2f }, Projectile.Center);
			}

			int targetWho = (int)Projectile.ai[0];
			NPC target = targetWho >= 0 && targetWho < Main.maxNPCs ? Main.npc[targetWho] : null;
			if (target == null || !target.active || target.life <= 0 || !target.CanBeChasedBy())
			{
				int next = FindNearest(Projectile.Center, 48f * 16f);
				if (next >= 0)
				{
					Projectile.ai[0] = next;
					target = Main.npc[next];
				}
				else
					target = null;
			}

			if (!_charging)
			{
				Projectile.velocity *= 0.9f;
				// 轻微环绕漂浮，保持在锚点附近
				if (target != null)
				{
					Vector2 want = _anchor;
					Projectile.Center = Vector2.Lerp(Projectile.Center, want, 0.08f);
				}
				return;
			}

			if (target != null)
			{
				Vector2 to = target.Center - Projectile.Center;
				if (to.LengthSquared() > 1f)
				{
					to.Normalize();
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, to * 16f, HomingTurnRate);
				}
			}
			else
			{
				HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			}

			if (Main.rand.NextBool(3))
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Vector2.Zero, 100,
					new Color(160, 60, 220), 1.05f).noGravity = true;
			}
		}

		private static int FindNearest(Vector2 from, float rangePx)
		{
			int best = -1;
			float bestD = rangePx * rangePx;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Vector2.DistanceSquared(n.Center, from);
				if (d < bestD)
				{
					bestD = d;
					best = i;
				}
			}
			return best;
		}

		public override bool? CanDamage() => _charging ? null : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(target.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(3.5f, 3.5f),
					60, new Color(180, 80, 255), 1.25f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float a = _appear;
			Color fill = Fill;
			Color border = Border;
			fill.A = (byte)(Fill.A * a);
			border.A = (byte)(Border.A * a);
			HenshinFxDraw.DrawOpaqueDisk(Projectile.Center, Diameter * (0.55f + 0.45f * a), fill, border);
			return false;
		}
	}
}
