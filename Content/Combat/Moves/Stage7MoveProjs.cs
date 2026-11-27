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

	/// <summary>灰日耀爆炸：仅贴图 VFX，无伤害。ai0&gt;0.5 时用反色日耀（青冷）。</summary>
	public class GraySolarBurstVfxProj : HenshinMoveProj
	{
		private const int Life = 18;
		private static Texture2D _invertedSolar;
		private static readonly Color AscentCyan = new(90, 245, 255);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

		private bool Invert => Projectile.ai[0] > 0.5f;

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
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.SolarWhipSwordExplosion);
		}

		/// <summary>反色（画龙点睛）用：ai1 = 绘制倍率（0 = 1）；ai1 &gt; 1.2 视为最后一爆（高潮）。</summary>
		private float BurstScale => Projectile.ai[1] > 0.01f ? Projectile.ai[1] : 1f;

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			if (Invert)
			{
				// R3 手感（只动反色 = 画龙点睛这一路，其它调用方不变）：首 tick 各端爆声（节流）+ 青色衰减光；
				// 最后一爆 = L 爆 + 大爆声 + 大招档震屏（主人端）。
				if (Projectile.localAI[0] == 0f)
				{
					Projectile.localAI[0] = 1f;
					Vector2 at = Projectile.Center;
					if (BurstScale > 1.2f)
					{
						HenshinSfx.Play(SfxKey.BlastL, at, 1f, 0.1f);
						HenshinSfx.Play(SfxKey.DragonHit, at, 0.9f, -0.3f);
						HenshinExplosion.Spawn(at, BlastSize.L, AscentCyan, 300f, kenneyFrame: false);
						HenshinHitFeel.Shake(Projectile.owner, at, HitTier.Ultimate);
					}
					else
					{
						HenshinSfx.PlayCooldown(SfxKey.BlastS, at, 6, 0.7f, 0.1f);
						HenshinExplosion.SpawnLight(at, AscentCyan, BlastSize.S, 0.9f);
					}
				}
				// 光照 D 组：青日耀本体补弱光（glow，随寿命衰减）
				Lighting.AddLight(Projectile.Center, new Vector3(0.07f, 0.19f, 0.2f) * (Projectile.timeLeft / (float)Life));
			}
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

		private static Texture2D EnsureInvertedSolar()
		{
			if (_invertedSolar != null && !_invertedSolar.IsDisposed)
				return _invertedSolar;
			if (Main.dedServ || Main.graphics?.GraphicsDevice == null)
				return null;
			Texture2D src = ProjectileBorrow.RequestProjectileTexture(ProjectileID.SolarWhipSwordExplosion);
			if (src == null)
				return null;
			Color[] data = new Color[src.Width * src.Height];
			src.GetData(data);
			for (int i = 0; i < data.Length; i++)
			{
				Color c = data[i];
				if (c.A == 0)
					continue;
				// RGB 反色，保留 Alpha；暖日耀 → 青冷轮廓
				data[i] = new Color((byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B), c.A);
			}
			_invertedSolar = new Texture2D(Main.graphics.GraphicsDevice, src.Width, src.Height);
			_invertedSolar.SetData(data);
			return _invertedSolar;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int frames = Math.Max(1, Main.projFrames[ProjectileID.SolarWhipSwordExplosion]);
			int frame = Math.Clamp(Projectile.frame, 0, frames - 1);
			float life = Projectile.timeLeft / (float)Life;
			float scale = 0.85f + (1f - life) * 0.25f;
			if (Invert)
				scale *= BurstScale;

			if (Invert)
			{
				Texture2D tex = EnsureInvertedSolar();
				if (tex == null)
					return false;
				Rectangle src = tex.Frame(1, frames, 0, frame);
				// 真反色贴图；轻乘路径青保留冷调
				Color tint = new Color(1, 253, 255, (int)(235 * life));
				Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, tint,
					0f, src.Size() * 0.5f, scale, SpriteEffects.None);
			}
			else
			{
				Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.SolarWhipSwordExplosion);
				Rectangle src = tex.Frame(1, frames, 0, frame);
				Color tint = new Color(160, 160, 165, (int)(220 * life));
				Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, tint,
					0f, src.Size() * 0.5f, scale, SpriteEffects.None);
			}
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

	/// <summary>
	/// 暗影抓导演（MoveSpec 入口）：拥有者客户端施放时用 OwnerAimDir 锁定一次 360° 方向，
	/// 把弧度经 ai1 随生成同步给 ShadowClawStrikeProj，然后自毁（联机安全，不读 Main.MouseWorld）。
	/// </summary>
	public class ShadowClawSlashProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		// 施放音由爪体 ShadowClawStrikeProj 在 AI 首帧各端播；声明后之力不再叠 Item20。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override bool? CanDamage() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.active && !owner.dead && Projectile.owner == Main.myPlayer)
			{
				Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
				if (Math.Abs(aim.X) > 0.05f)
					owner.ChangeDir(aim.X > 0f ? 1 : -1);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<ShadowClawStrikeProj>(), Projectile.damage, Projectile.knockBack,
					Projectile.owner, 0f, aim.ToRotation());
			}
			Projectile.Kill();
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>暗影抓本体：三道紫黑爪痕沿 20 格（ai1 = 朝向弧度，360°）划出，影炎尾，命中 ShadowFlame。</summary>
	public class ShadowClawStrikeProj : HenshinMoveProj
	{
		private const int Lifetime = 14;
		private const float ReachTiles = 20f;
		private const float LineSpacing = 26f; // 与贴图三道爪痕间距一致（13 美术像素）

		// 贴图 Assets/Fx/ShadowClaw/ShadowClaw(+_Light).png：6 帧 × 2 tick，328×92/帧，爪根在 (4,46)，全长约 314 px ≈ 20 格。
		private const int SheetFrames = 6;
		private const int SheetTicksPerFrame = 2;
		private static ReLogic.Content.Asset<Texture2D> _sheet;
		private static ReLogic.Content.Asset<Texture2D> _light;

		private Vector2 Dir => Projectile.ai[1].ToRotationVector2();

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
			Projectile.ownerHitCheck = false;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			Vector2 dir = Dir;
			float reach = ReachTiles * 16f;
			Projectile.Center = owner.MountedCenter + dir * (reach * 0.5f);
			Projectile.velocity = Vector2.Zero;

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.SwingHeavy, owner.Center, 1f, -0.15f);
				HenshinSfx.Play(SfxKey.GhostCast, owner.Center, 0.6f, 0.2f);
			}

			if (Main.dedServ)
				return;

			// 爪痕贴图自带影炎尾，不再沿线撒尘。R3：光沿爪线三点、随划出先亮后收。
			float k = 1f - (Lifetime - Projectile.timeLeft) / (float)Lifetime;
			for (int i = 1; i <= 3; i++)
				Lighting.AddLight(owner.MountedCenter + dir * (reach * 0.25f * i), new Vector3(0.5f, 0.18f, 0.8f) * (0.5f + 0.5f * k));
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 dir = Dir;
			Vector2 side = new(-dir.Y, dir.X);
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = origin + dir * (ReachTiles * 16f);
			for (int line = -1; line <= 1; line++)
			{
				Vector2 o = origin + side * (line * LineSpacing);
				Vector2 e = tip + side * (line * LineSpacing);
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), o, e, 16f, ref point))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.ShadowFlame, 180);
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			Vector2 dir = Dir;
			float along = MathHelper.Clamp(Vector2.Dot(target.Center - owner.MountedCenter, dir), 0f, ReachTiles * 16f);
			Vector2 hitPos = owner.MountedCenter + dir * along;
			// 0 伤害纯视觉命中射弹：落在爪线上最近点，紫焰迸散；ai0 = 朝向弧度。
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), hitPos, Vector2.Zero,
				ModContent.ProjectileType<ShadowClawHitFxProj>(), 0, 0f, Projectile.owner, Projectile.ai[1]);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(170, 70, 255), hitPos);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ShadowClaw/ShadowClaw", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ShadowClaw/ShadowClaw_Light", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / SheetFrames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / SheetTicksPerFrame, 0, SheetFrames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Player owner = Main.player[Projectile.owner];
			Vector2 pos = (owner.MountedCenter - Main.screenPosition).Floor();
			float rot = Projectile.ai[1];
			// 朝左半圈时上下翻转，让贴图上半始终朝上。
			bool left = Math.Cos(rot) < 0;
			SpriteEffects fx = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Vector2 origin = new(4f, left ? frameH - 46f : 46f);
			// 末段淡出（预乘贴图，整色相乘；Additive 层同样乘不透明度，A 随之保留）。
			float opacity = Math.Min(1f, Projectile.timeLeft / 4f);
			Main.EntitySpriteDraw(tex, pos, src, Color.White * opacity, rot, origin, 1f, fx, 0f);
			HenshinFxDraw.BeginAdditive();
			Main.EntitySpriteDraw(_light.Value, pos, src, Color.White * opacity, rot, origin, 1f, fx, 0f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>暗影抓命中：紫焰爪痕迸散（纯视觉，0 伤害）。ai0 = 爪的朝向弧度。贴图 Assets/Fx/ShadowClaw/ShadowClawHit(+_Light)。</summary>
	public class ShadowClawHitFxProj : ModProjectile
	{
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int Lifetime = Frames * TicksPerFrame;
		private static ReLogic.Content.Asset<Texture2D> _sheet;
		private static ReLogic.Content.Asset<Texture2D> _light;

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
			int age = Lifetime - Projectile.timeLeft;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				// R3（G2）：自制幽灵命中音（各端首帧，节流）；删 3 颗影焰尘（贴图 + _Light 已够）。
				HenshinSfx.PlayCooldown(SfxKey.GhostHit, Projectile.Center, 4);
			}
			HenshinExplosion.Pulse(Projectile.Center, new Color(170, 70, 255), BlastSize.S, age / (float)Lifetime, 72f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ShadowClaw/ShadowClawHit", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/ShadowClaw/ShadowClawHit_Light", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			int frame = Math.Clamp(age / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			float rot = Projectile.ai[0];
			SpriteEffects fx = Math.Cos(rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, pos, src, Color.White, rot, origin, 1f, fx, 0f);
			HenshinFxDraw.BeginAdditive();
			Main.EntitySpriteDraw(_light.Value, pos, src, Color.White, rot, origin, 1f, fx, 0f);
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
				// R3（G2）：起手只留共享 UltCast（删 Item8）；喷射环叠 M 号爆炸配方 + 主人端大招震屏。
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), owner.Center, H3Fx.DarkPulseRing, 2.4f, Projectile.owner, 0f, null, BlastSize.M);
				HenshinHitFeel.Shake(Projectile.owner, owner.Center, HitTier.Ultimate);
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
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// R3 手感（G2）：32 发各带短暗紫拖尾 + 弱光；命中音挪到同步特效弹（GhostHit 节流），删每颗命中 / 消失的 Item10。
		private static readonly Color TrailGlow = new(140, 50, 230);

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 9);

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
			InherentHoming = true;
		}

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Lighting.AddLight(Projectile.Center, 0.22f, 0.08f, 0.38f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 每次施放第一颗命中：HitUlt + L 号暗紫大爆；其余每 2 颗一个小暗影爆 + GhostHit。
			if (H3Fx.TryUltBeat(Projectile.owner))
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.ShadowHit, 2.4f, Projectile.owner, 0f, SfxKey.HitUlt, BlastSize.L);
			else if (Projectile.identity % 2 == 0)
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.ShadowHit, 0.7f, Projectile.owner, 0f, SfxKey.GhostHit);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), TrailGlow, target.Center);
			if (Projectile.owner == Main.myPlayer)
			{
				int aoe = Math.Max(1, Projectile.damage / 2);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<DarkPulseBurstAoEProj>(), aoe, 1f, Projectile.owner);
			}
		}

		// 复用 Assets/Fx/ShadowBall 的暗影球贴图，缩小为 0.7。
		public override bool PreDraw(ref Color lightColor)
		{
			HenshinTrail.Strip(Projectile, TrailGlow, 12f, new Color(220, 170, 255), 0.34f, 0f, 0.75f);
			H3Fx.Draw(H3Fx.ShadowBall, 4, (int)(Main.GameUpdateCount / 3) % 4, Projectile.Center, 0f, 0.7f, Color.White);
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
			HenshinExplosion.Pulse(Projectile.Center, new Color(160, 60, 220), BlastSize.S, 1f - Projectile.timeLeft / 8f, 72f, 0.7f);
		}

		// R3（G2）：小爆改 S 号爆炸配方（白芯 + 冲击环，按判定 72px 对齐）+ 衰减光；8 tick 寿命不变。
		public override bool PreDraw(ref Color lightColor)
		{
			float t = 1f - Projectile.timeLeft / 8f;
			HenshinFxDraw.BeginAdditive();
			HenshinExplosion.Draw(Projectile.Center, BlastSize.S, new Color(160, 60, 220), t, 72f, 0f, kenneyFrame: false);
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

		// R3 手感（G2）：出拳 SwingHeavy（各端 AI 首帧）；命中改同步钢火花（光 + S 爆 + SteelHit）+ 重档打击感，删铁屑尘。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				_origin = owner.MountedCenter;
				HenshinSfx.Play(SfxKey.SwingHeavy, owner.Center, 1f, 0.1f);
			}

			float u = MathHelper.SmoothStep(0f, 1f, Progress);
			Projectile.Center = _origin + _dir * (40f + Reach * u);
			// 光照 D 组：拳 mixed，自身光 ≤0.2（原 0.25/0.5/0.7）；彗星星仍 glow
			Lighting.AddLight(Projectile.Center, 0.07f, 0.14f, 0.2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.SteelSpark, 1.6f, Projectile.owner, 0f, SfxKey.SteelHit, BlastSize.S);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(127, 224, 255), target.Center);
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
			// 自绘 Assets/Fx/MeteorFist：钢拳 + 彗尾（3 帧随出拳拉长，朝右，向左半圈上下翻转保持受光面朝上）
			int age = Lifetime - Projectile.timeLeft;
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			float alpha = Math.Min(1f, Projectile.timeLeft / 4f);
			H5Fx.Draw("MeteorFist", 3, Math.Min(2, age / 6), Projectile.Center, new Vector2(72f, 24f), _dir.ToRotation(), 1.15f, alpha, fx,
				Color.Lerp(lightColor, Color.White, 0.4f));
			return false;
		}
	}

	/// <summary>彗星拳星星：自绘钢青彗星 Assets/Fx/CometStar（40×80 单帧，头朝下、彗尾朝上），穿墙下落。</summary>
	public class CometStarFallProj : HenshinMoveProj
	{
		public override string Texture => "PokemonHenshin/Assets/Fx/CometStar/CometStar";

		// R3 手感（G2）：钢青拖尾跟真实弹道；命中 / 落点 S 号爆炸配方 + BlastS（OnKill 各端都跑，飞满寿命的不爆）。
		private static readonly Color StarTint = new(110, 210, 255);

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 12);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), StarTint);
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft <= 0)
				return;
			HenshinExplosion.Spawn(Projectile.Center, BlastSize.S, StarTint, 64f, kenneyFrame: false);
			HenshinSfx.PlayCooldown(SfxKey.BlastS, Projectile.Center, 4);
		}

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

		public override void AI()
		{
			Vector2 aim = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			if (aim.LengthSquared() > 1f)
			{
				Vector2 want = (aim - Projectile.Center).SafeNormalize(Projectile.velocity) * 18f;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.12f);
			}
			// CometStar 贴图头朝下（+Y）；减 Pi/2 使头领路
			Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, 0.15f, 0.4f, 0.6f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 原点放在星头 (21,59)，碰撞箱跟着星头走
			Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			HenshinTrail.Strip(Projectile, StarTint, 16f, new Color(230, 250, 255), 0.36f, 0f, 0.7f);
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), null, Color.White,
				Projectile.rotation, new Vector2(21f, 59f), 1f, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(127, 224, 255), 0.4f), 0.35f);
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
			InherentHoming = true;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			ProjectileBorrow.SafeLoadProjectile(ProjectileID.CultistBossFireBall);
		}

		private static readonly Color OutrageTint = new(255, 90, 45);

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10);

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Projectile.rotation += 0.2f;
			// H4：球体改手绘 OutrageOrb 贴图，不再逐 tick 撒尘。G1：红橙尾 + 飞行光。
			Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.08f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.owner == Main.myPlayer)
			{
				int aoe = Math.Max(1, Projectile.damage / 2);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<OutrageFireBurstAoEProj>(), aoe, 2f, Projectile.owner);
			}
			// 只主人听得到的 Item14 删去：爆声由 OutrageFireBurstAoEProj 首帧各端播。
			// 33 发连射：固定 Heavy 档，大震留给导演弹收尾那一下。
			HenshinHitFeel.OnHit(Projectile, target, HitTier.Heavy, OutrageTint, target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			// 旧 2 颗火尘删去（命中有 OutrageBurst 爆图 + 爆炸配方）。
		}

		public override bool PreDraw(ref Color lightColor)
		{
			HenshinTrail.Strip(Projectile, OutrageTint, 20f, new Color(255, 210, 150), 0.36f, 0f, 0.85f);
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			H4Fx.DrawFrame(H4Fx.Orb(true), 4, frame, Projectile.Center, Color.White, 0f, 1.0f);
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

		private static readonly Color BurstTint = new(255, 90, 45);

		public override void AI()
		{
			// 各端首帧：火系命中 + 小爆（本端节流，33 发不叠成一团）；每次施放补一声 HitUlt。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.PlayCooldown(SfxKey.FireHit, Projectile.Center, 4);
				HenshinSfx.PlayCooldown(SfxKey.BlastS, Projectile.Center, 8, 0.7f);
				if (HenshinHitFeel.TierOf(Projectile, HitTier.Light) == HitTier.Ultimate)
					HenshinSfx.PlayCooldown(SfxKey.HitUlt, Projectile.Center, 60);
			}
			HenshinExplosion.Pulse(Projectile.Center, BurstTint, BlastSize.M, (10 - Projectile.timeLeft) / 10f, 80f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// H4：手绘 OutrageBurst（5 帧 × 2 tick；碰撞 80px，贴图 52px × 1.5 = 78px）。
			int frame = (10 - Projectile.timeLeft) / 2;
			HenshinFxDraw.BeginAdditive();
			HenshinExplosion.Draw(Projectile.Center, BlastSize.M, BurstTint, (10 - Projectile.timeLeft) / 10f, 80f);
			HenshinFxDraw.EndAdditive();
			H4Fx.DrawFrame(H4Fx.Burst(H4Fx.StyleOutrage), 5, frame, Projectile.Center, Color.White, 0f, 1.5f);
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
			if (Projectile.localAI[0] == 0f)
			{
				// R3 手感（流星群起手，各端）：头顶天空一记 M 爆闪光 + 龙系施放低音（共享 UltCast 已是召唤声，这里只补"天裂"）。
				Projectile.localAI[0] = 1f;
				Vector2 sky = Projectile.Center + new Vector2(0f, -340f);
				HenshinExplosion.Spawn(sky, BlastSize.M, new Color(255, 150, 80), 220f);
				HenshinSfx.Play(SfxKey.DragonCast, sky, 0.9f, -0.45f);
			}
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
				// ai2 = 第几颗（1..64，随生成包同步）：每 8 颗一颗重爆（R3 手感高潮点）
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<DracoStarWrathProj>(), Projectile.damage, Projectile.knockBack,
					Projectile.owner, target.X, target.Y, _spawned + 1);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
				_spawned++;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 流星群的单颗流星：自绘贴图 H6Meteor（3 帧闪烁，朝右绘制，按速度方向旋转），落点爆炸 H6MeteorImpact。
	/// 倍率 / 数量（64）/ 追踪沿用旧版。
	/// </summary>
	public class DracoStarWrathProj : HenshinMoveProj
	{
		private static ReLogic.Content.Asset<Texture2D> _meteor;

		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
			InherentHoming = true;
		}

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10);

		/// <summary>每 8 颗一颗重爆（ai2 = 序号，导演弹传入并同步）。</summary>
		private bool HeavyStar => (int)(Projectile.ai[2] + 0.5f) % 8 == 0 && Projectile.ai[2] > 0.5f;

		/// <summary>本端是否命中过（只在主人端有意义；OnKill 的爆点也只在主人端生成）。</summary>
		private bool _hitSomething;

		public override void AI()
		{
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.7f, 0.35f, 0.15f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			_hitSomething = true;
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(255, 140, 60), Projectile.Center);
		}

		public override void OnKill(int timeLeft)
		{
			// 命中（penetrate=1 随后 Kill）和超时都走这里，只爆一次。
			// R3 手感：64 颗各自的 Item14 删掉；落点声音 / 爆炸配方在同步爆点首 tick（BlastS 冷却 4 tick；命中 → HitUlt 20 tick 一次；每 8 颗重爆 BlastM + M 爆 + 重档震屏）。
			bool heavy = HeavyStar;
			int fx = heavy ? H6Fx.FxHeavy : _hitSomething ? H6Fx.FxUlt : H6Fx.FxLight;
			H6Fx.SpawnBurst(Projectile, Projectile.Center, H6Fx.KindMeteor, heavy ? 1.4f : 0.85f, fx);
			if (heavy)
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Heavy, Vector2.UnitY);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 流星亮带：橙外晕 + 暖白芯，先画再盖本体
			HenshinTrail.Strip(Projectile, new Color(255, 120, 40), 18f, new Color(255, 225, 160), 0.36f, 0f, 0.85f);
			_meteor ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/H6Meteor/H6Meteor", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _meteor.Value;
			int fh = tex.Height / 3;
			int frame = (int)((Main.GameUpdateCount / 4 + (uint)Projectile.whoAmI) % 3);
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), new Rectangle(0, fh * frame, tex.Width, fh),
				Color.Lerp(H6Fx.Lit(Projectile.Center, 140), Color.White, 0.6f), Projectile.rotation,
				new Vector2(70f, fh * 0.5f), 1f, SpriteEffects.None, 0f);
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
				// R3 手感：蓄力音改自制 UltCharge（技能版：轻、偏高），已声明 ClaimCastSound。
				HenshinSfx.Play(SfxKey.UltCharge, p.Center, 0.7f, 0.3f);
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
				// 蓄力：每 tick 的云尘删掉，改为随蓄力变亮的金白灯（光晕在 PreDraw）
				float charge = MathHelper.Clamp(Projectile.localAI[1] / ChargeTicks, 0f, 1f);
				Lighting.AddLight(p.Center, (0.3f + 0.6f * charge) * new Vector3(1f, 0.85f, 0.55f));

				if (Projectile.localAI[1]++ >= ChargeTicks)
				{
					_phase = 1;
					_dashLife = (int)MathHelper.Clamp(ReachTiles * 0.75f, 18f, 40f);
					_speed = ReachTiles * 16f / _dashLife;
					_lifetime = _dashLife + BrakeTicks;
					Projectile.timeLeft = _lifetime;
					Projectile.localNPCHitCooldown = _dashLife;
					_cruiseSpeed = Math.Max(p.maxRunSpeed, 6f);
					// 冲刺起手：重挥 + 风声（原 Item1 太轻）+ 重档震屏（主人端）
					HenshinSfx.Play(SfxKey.SwingHeavy, p.Center, 1f, -0.15f);
					HenshinSfx.Play(SfxKey.WindCast, p.Center, 0.9f, -0.25f);
					HenshinHitFeel.Shake(Projectile.owner, p.Center, HitTier.Heavy, _dir);
				}
				return;
			}
			Lighting.AddLight(p.Center, InBrake ? 0.4f : 0.9f, InBrake ? 0.35f : 0.8f, InBrake ? 0.25f : 0.55f);

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

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// R3 手感：每次命中都有重档反馈 + 同步金闪（命中音在 H7HitFxProj 首 tick）；6 鸟仍只出一次。
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, new Color(255, 225, 150), target.Center);
			H7HitFxProj.Spawn(Projectile, target.Center, 2, 0.8f, H7HitFxProj.TierIndex(tier));
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
			// 鸟群出现音改到 H7SkyBirdCrossProj 首 tick（各端，6 鸟共用冷却只响一声）；原 Item102 只有主人听得到。
			int birdDmg = Math.Max(1, Projectile.damage / 4);
			for (int i = 0; i < 6; i++)
			{
				float ang = MathHelper.TwoPi * i / 6f + 0.2f;
				Vector2 vel = ang.ToRotationVector2() * 13f;
				if (i % 2 == 1)
					vel = -vel.RotatedBy(0.35f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at - vel * 8f, vel,
					ModContent.ProjectileType<H7SkyBirdCrossProj>(), birdDmg, 1.5f, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_phase == 1 && _dir != Vector2.Zero)
			{
				// R3 手感：冲刺金色亮带（替掉 4 颗小光点）。路径点沿 _dir 直线反推，不读 oldPos（本弹钉在玩家身上）；长度随已冲距离增长，上限 220px。
				int dashAge = _lifetime - Projectile.timeLeft;
				float len = Math.Min(220f, Math.Max(0, dashAge) * _speed);
				if (len > 12f)
				{
					Span<Vector2> pts = stackalloc Vector2[6];
					for (int i = 0; i < pts.Length; i++)
						pts[i] = Projectile.Center - _dir * (len * i / (pts.Length - 1f));
					HenshinTrail.StripPoints(pts, new Color(255, 200, 110), InBrake ? 22f : 34f, new Color(255, 248, 225), 0.34f, 0f, InBrake ? 0.45f : 0.85f);
				}
			}
			HenshinFxDraw.BeginAdditive();
			if (_phase == 0)
			{
				float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f);
				float charge = MathHelper.Clamp(Projectile.localAI[1] / ChargeTicks, 0f, 1f);
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 235, 190), (0.35f + 0.35f * charge) * (0.6f + 0.4f * pulse)), 0.6f + 0.6f * charge);
			}
			else
			{
				HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
					HenshinFxDraw.WithAlpha(new Color(255, 235, 180), InBrake ? 0.25f : 0.5f), 0.8f);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 超梦精神击破：指针 16 格内最近敌；没有则改选离玩家最近的敌人（60 格内）；都没有则不放（导演自毁，不生成念弹）。
	/// 选敌只在拥有者端算，结果写 ai0（=whoAmI+1）并 netUpdate，其他客户端读 ai0，联机一致。
	/// 0.5s 内在目标身周 8 格渐显 64 枚紫色念力波纹弹，然后齐冲向目标。
	/// </summary>
	public class MewtwoPsystrikeDirectorProj : HenshinMoveProj
	{
		private const int SpawnTicks = 30;
		private const int TotalOrbs = 64;
		private const float SelectRadius = 16f * 16f;
		private const float OrbOrbitRadius = 8f * 16f;
		private const float FallbackRadius = 60f * 16f;

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
			if (Projectile.owner == Main.myPlayer && Projectile.ai[0] < 0.5f && Projectile.localAI[1] == 0f)
			{
				int pick = FindNearestHostile(HenshinProjUtil.OwnerMouseWorld(Projectile), SelectRadius);
				if (pick < 0)
					pick = FindNearestHostile(Main.player[Projectile.owner].Center, FallbackRadius);
				if (pick < 0)
				{
					// R4 复查建议 1：无目标不再首帧 Kill（远端会收不到根弹、听不到共享 UltCast）；
					// 只记一次"已放弃"，不再找目标，空等 timeLeft 自然到期（无伤、不画、不生成子弹）。
					// 施放前 HenshinForceItem 已用 HasTarget 挡住无目标，能量与冷却行为不变。
					Projectile.localAI[1] = 1f;
					return;
				}
				Projectile.ai[0] = pick + 1;
				Projectile.netUpdate = true;
			}
			if (Projectile.ai[0] < 0.5f)
				return; // 非拥有者：等同步来的目标

			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_goAt = (int)Main.GameUpdateCount + SpawnTicks;
				_targetWho = (int)Projectile.ai[0] - 1;
				// R3（G2）：起手只留共享 UltCast（删 Item8）；锁定环不变（加法光层由 H3HitFxProj 统一加）。
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Main.npc[_targetWho].Center, H3Fx.RingPurple, 4.5f, Projectile.owner);
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
						Projectile.owner, _targetWho, Math.Max(1, _goAt - (int)Main.GameUpdateCount), diam);
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

		/// <summary>R4：释放前检查（拥有者端）：鼠标 16 格或玩家 60 格内有敌人。无则 TryFireUltimate 直接返回 false，不扣能量、不进冷却。</summary>
		public static bool HasTarget(Player player)
			=> FindNearestHostile(PokemonHenshin.Content.PlayerState.HenshinPlayer.GetMouseWorld(player), SelectRadius) >= 0
			|| FindNearestHostile(player.Center, FallbackRadius) >= 0;

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
	/// 精神击破念弹（超梦紫波纹）：渐显后齐向标记目标冲刺；目标死亡则改追附近敌人。
	/// ai0=targetWho；ai1=出生后到冲刺的 tick 数（本弹 localAI[1] 自计时，不用各端不同步的 GameUpdateCount）；ai2=直径。
	/// </summary>
	public class MewtwoPsystrikeOrbProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private float _appear;
		private bool _charging;
		private Vector2 _anchor;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// R3 手感（G2）：冲刺段紫色拖尾；齐冲那一下全场只响一声 PsyCast（原 64× Item10）；
		// 第一颗命中 = HitUlt + L 号紫白大爆，之后每 4 颗一个念力环 + PsyHit（节流）；大招档打击感（20 tick 只震一次）。
		private static readonly Color OrbTint = new(190, 90, 255);

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10);

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
			InherentHoming = true;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => EnsureAnchor();

		private bool _anchorReady;

		private void EnsureAnchor()
		{
			if (_anchorReady)
				return;
			_anchorReady = true;
			_anchor = Projectile.Center;
		}

		public override void AI()
		{
			EnsureAnchor();
			Projectile.localAI[1]++;
			_appear = MathHelper.Clamp(_appear + 0.08f, 0f, 1f);

			if (!_charging && Projectile.localAI[1] >= Projectile.ai[1])
			{
				_charging = true;
				Homing = true;
				HenshinSfx.PlayCooldown(SfxKey.PsyCast, Projectile.Center, 20, 1f, -0.3f);
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

			// 波纹朝向：蓄势时朝目标，冲刺时朝飞行方向
			if (_charging && Projectile.velocity.LengthSquared() > 0.25f)
				Projectile.rotation = Projectile.velocity.ToRotation();
			else if (target != null)
				Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
			Lighting.AddLight(Projectile.Center, H3Fx.VariantLight[2] * 0.3f * _appear);

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
			// 64 枚齐中：第一颗给大爆，其余每 4 枚才播一次念力环，避免特效射弹爆量。
			if (H3Fx.TryUltBeat(Projectile.owner))
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.RingPurple, 3f, Projectile.owner, 0f, SfxKey.HitUlt, BlastSize.L);
			else if (Projectile.identity % 4 == 0)
				H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.RingPurple, 0.6f, Projectile.owner, 0f, SfxKey.PsyHit);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), OrbTint, target.Center);
		}

		// 超梦紫念力波纹（与精神强念同款 PsychicWaveT + 扩散回波），按 ai2 直径缩放，渐显。
		public override bool PreDraw(ref Color lightColor)
		{
			float a = _appear;
			if (_charging)
				HenshinTrail.Strip(Projectile, OrbTint, Math.Clamp(Diameter * 0.55f, 9f, 18f), new Color(240, 210, 255), 0.36f, 0f, 0.8f);
			float scale = Math.Max(0.6f, Diameter / 30f) * (0.55f + 0.45f * a);
			H3Fx.DrawRipple(2, Projectile.Center, Projectile.rotation, scale, a);
			return false;
		}
	}
}
