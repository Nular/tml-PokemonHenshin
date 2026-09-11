using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 尖石攻击：三根三角石刺戳刺（中根更远，两侧朝中）。
	/// </summary>
	public class StoneEdgeDirectorProj : HenshinMoveProj
	{
		private bool _fired;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 10;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void OnSpawn(IEntitySource source) => Fire();

		public override void AI() => Fire();

		private void Fire()
		{
			if (_fired || Projectile.owner != Main.myPlayer)
				return;
			_fired = true;

			Player owner = Main.player[Projectile.owner];
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();

			// 0=中（最远），±1=侧（略短，朝中夹角）
			SpawnSpike(owner, aim, 0f, 1f);
			SpawnSpike(owner, aim.RotatedBy(-0.28f), -1f, 0.88f);
			SpawnSpike(owner, aim.RotatedBy(0.28f), 1f, 0.88f);

			SoundEngine.PlaySound(SoundID.Item69 with { Pitch = -0.2f }, owner.Center);
			Projectile.Kill();
		}

		private void SpawnSpike(Player owner, Vector2 dir, float side, float reachMul)
		{
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, dir,
				ModContent.ProjectileType<StoneEdgeSpikeProj>(), Projectile.damage, Math.Max(12f, Projectile.knockBack),
				Projectile.owner, side, reachMul);
			if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
			{
				tagged.EasyCrit = EasyCrit;
				tagged.IgnoreDefensePartial = IgnoreDefensePartial;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 三角石刺 jab：TriangleList + Boulder UV；命中/结束爆 + 小落石 50%。
	/// ai0=侧标记；ai1=射程倍率。
	/// </summary>
	public class StoneEdgeSpikeProj : HenshinMoveProj
	{
		private const int Lifetime = 12;
		private const float BaseReach = 20f * 16f;
		private const float BaseHalf = 18f;

		private Vector2 _dir = Vector2.UnitX;
		private float _reach;
		private VertexPositionColorTexture[] _verts = new VertexPositionColorTexture[3];

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Boulder;

		public override void SetDefaults()
		{
			Projectile.width = 32;
			Projectile.height = 32;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime;
			Projectile.knockBack = 12f;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(IEntitySource source) => EnsureSpike();

		private void EnsureSpike()
		{
			if (_reach > 1f)
				return;
			Main.instance.LoadProjectile(ProjectileID.Boulder);
			Player owner = Main.player[Projectile.owner];
			_dir = Projectile.velocity.LengthSquared() > 0.01f
				? Vector2.Normalize(Projectile.velocity)
				: new Vector2(owner.direction, 0f);
			float mul = Projectile.ai[1] > 0.1f ? Projectile.ai[1] : 1f;
			_reach = BaseReach * mul;
			Projectile.knockBack = Math.Max(12f, Projectile.knockBack);
			// 方向留在 velocity 里给旁观端读；ShouldUpdatePosition=false 不会飞走。
		}

		public override void AI()
		{
			EnsureSpike();
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}

			float age = Lifetime - Projectile.timeLeft;
			float jab = MathHelper.Clamp(age / 5f, 0f, 1f);
			float reachNow = MathHelper.Lerp(48f, _reach, jab);
			Projectile.Center = owner.MountedCenter + _dir * reachNow;
			Dust.NewDustPerfect(Projectile.Center, DustID.Stone, _dir * 2f, 80, new Color(160, 120, 80), 1.1f).noGravity = true;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			EnsureSpike();
			Player owner = Main.player[Projectile.owner];
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = origin + _dir * _reach;
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, tip, 28f, ref _);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.velocity += _dir * 8f;
			TryBurst(target.Center);
			// 穿透：不 Kill，尖刺保持到寿命结束
		}

		public override void OnKill(int timeLeft) { }

		private void TryBurst(Vector2 at)
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			int aoe = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
				ModContent.ProjectileType<RockShatterBurstProj>(), Projectile.damage, Projectile.knockBack * 0.6f, Projectile.owner, 1f);
			if (aoe >= 0)
				Main.projectile[aoe].Center = at;

			int n = Main.rand.Next(2, 4);
			int shardDmg = Math.Max(1, Projectile.damage / 2);
			for (int i = 0; i < n; i++)
			{
				Vector2 vel = _dir.RotatedByRandom(0.7f) * Main.rand.NextFloat(3f, 7f) + new Vector2(0f, -2f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, vel,
					ModContent.ProjectileType<FallingBoulderProj>(), shardDmg, 2f, Projectile.owner);
				if (id >= 0)
				{
					Main.projectile[id].scale = 0.28f;
					Main.projectile[id].timeLeft = 36;
					Main.projectile[id].penetrate = 1;
				}
			}
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.15f }, at);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			EnsureSpike();
			Player owner = Main.player[Projectile.owner];
			float age = Lifetime - Projectile.timeLeft;
			float jab = MathHelper.Clamp(age / 5f, 0f, 1f);
			float reachNow = MathHelper.Lerp(48f, _reach, jab);
			Vector2 origin = owner.MountedCenter;
			Vector2 tip = origin + _dir * reachNow;
			Vector2 perp = _dir.RotatedBy(MathHelper.PiOver2);
			float halfBase = MathHelper.Lerp(BaseHalf * 0.45f, BaseHalf, jab);

			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Boulder);
			Color c = new(210, 200, 190, 255);
			_verts[0] = new VertexPositionColorTexture(new Vector3(origin + perp * halfBase - Main.screenPosition, 0f), c, new Vector2(0.1f, 0.1f));
			_verts[1] = new VertexPositionColorTexture(new Vector3(origin - perp * halfBase - Main.screenPosition, 0f), c, new Vector2(0.1f, 0.9f));
			_verts[2] = new VertexPositionColorTexture(new Vector3(tip - Main.screenPosition, 0f), c, new Vector2(0.9f, 0.5f));

			Main.spriteBatch.End();
			var device = Main.graphics.GraphicsDevice;
			_basicEffect ??= new BasicEffect(device)
			{
				TextureEnabled = true,
				VertexColorEnabled = true,
				World = Matrix.Identity,
				View = Matrix.Identity
			};
			_basicEffect.Texture = tex;
			_basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
			bool drew = false;
			try
			{
				foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
				{
					pass.Apply();
					device.DrawUserPrimitives(PrimitiveType.TriangleList, _verts, 0, 1);
				}
				drew = true;
			}
			catch
			{
				drew = false;
			}

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			if (!drew)
			{
				// FALLBACK slender — report to supervisor: TriangleList failed; elongated boulder
				float len = reachNow;
				float rot = _dir.ToRotation();
				Main.EntitySpriteDraw(tex, (origin + tip) * 0.5f - Main.screenPosition, null,
					new Color(200, 190, 180, 230), rot, tex.Size() * 0.5f,
					new Vector2(len / Math.Max(1, tex.Width), halfBase * 2f / Math.Max(1, tex.Height)), SpriteEffects.None);
			}
			return false;
		}

		private static BasicEffect _basicEffect;
	}

	/// <summary>
	/// 十字劈：360° 朝鼠标；两条对角弧组成带弧度的 X（外凸朝瞄准方向）。
	/// </summary>
	public class CrossChopArcXProj : HenshinMoveProj
	{
		private const int Lifetime = 20;
		private const float CrossOffset = 5f * 16f;
		private const float HalfAlong = 5f * 16f;  // 交点前后各 5 格
		private const float HalfAcross = 6f * 16f; // 上下各 6 格 → 高约 12 格
		private const float Bulge = 40f;

		private Vector2 _aim = Vector2.UnitX;
		private bool _aimLocked;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime / 2;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
			{
				Projectile.Kill();
				return;
			}

			if (!_aimLocked)
			{
				_aimLocked = true;
				_aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_aim.LengthSquared() < 1f)
					_aim = new Vector2(owner.direction, 0f);
				_aim.Normalize();
				owner.direction = _aim.X >= 0f ? 1 : -1;
			}

			Projectile.Center = owner.MountedCenter + _aim * CrossOffset;
			if (Projectile.timeLeft == Lifetime - 1 || Projectile.timeLeft == Lifetime / 2)
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, Projectile.Center);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			GetXEnds(1f, out Vector2 a0, out Vector2 a1, out Vector2 b0, out Vector2 b1, out _);
			float unused = 0f;
			if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a0, a1, 28f, ref unused))
				return true;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), b0, b1, 28f, ref unused);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float progress = 1f - Projectile.timeLeft / (float)Lifetime;
			progress = MathHelper.Clamp(progress, 0f, 1f);
			GetXEnds(progress, out Vector2 a0, out Vector2 a1, out Vector2 b0, out Vector2 b1, out _);

			Color col = HenshinFxDraw.WithAlpha(new Color(255, 230, 210), 0.9f * (0.55f + 0.45f * progress));
			HenshinFxDraw.BeginAdditive();
			DrawCurvedSlash(a0, a1, _aim, col, progress);
			DrawCurvedSlash(b0, b1, _aim, col, progress);
			HenshinFxDraw.EndAdditive();
			return false;
		}

		/// <summary>交点身前 CrossOffset；两对角端点构成 X，随瞄准旋转。</summary>
		private void GetXEnds(float progress, out Vector2 a0, out Vector2 a1, out Vector2 b0, out Vector2 b1, out Vector2 cross)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 fwd = _aim.LengthSquared() > 0.01f ? _aim : Vector2.UnitX * owner.direction;
			Vector2 perp = new Vector2(-fwd.Y, fwd.X);
			cross = owner.MountedCenter + fwd * CrossOffset;

			float along = HalfAlong * progress;
			float across = HalfAcross * progress;
			a0 = cross - fwd * along - perp * across;
			a1 = cross + fwd * along + perp * across;
			b0 = cross - fwd * along + perp * across;
			b1 = cross + fwd * along - perp * across;
		}

		/// <summary>单条对角：二次贝塞尔，中点沿弦法线外凸（朝瞄准方向）。</summary>
		private static void DrawCurvedSlash(Vector2 start, Vector2 end, Vector2 bulgeToward, Color col, float progress)
		{
			Vector2 chord = end - start;
			if (chord.LengthSquared() < 1f)
				return;
			Vector2 n = new Vector2(-chord.Y, chord.X);
			n.Normalize();
			if (Vector2.Dot(n, bulgeToward) < 0f)
				n = -n;

			Vector2 mid = (start + end) * 0.5f;
			Vector2 ctrl = mid + n * (Bulge * progress);

			const int samples = 14;
			Vector2 prev = start;
			for (int i = 1; i <= samples; i++)
			{
				float t = i / (float)samples;
				// progress 只缩放几何端点；整段画满，避免半截弧
				Vector2 p = QuadBezier(start, ctrl, end, t);
				HenshinFxDraw.DrawContinuousBeam(prev, p, col,
					HenshinFxDraw.WithAlpha(new Color(255, 200, 160), col.A / 255f * 0.5f), 6f, 14f);
				prev = p;
			}
		}

		private static Vector2 QuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
		{
			float u = 1f - t;
			return u * u * p0 + 2f * u * t * p1 + t * t * p2;
		}
	}

	/// <summary>近身战：极高频、全程无敌、玩家贴身跟随目标、HitJagged 朝向修正。</summary>
	public class CloseCombatFuryProj : HenshinMoveProj
	{
		private const int Lifetime = 60;
		private const float StickDist = 42f;
		private int _lockWho = -1;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 2;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			p.immune = true;
			p.immuneTime = Math.Max(p.immuneTime, Projectile.timeLeft + 2);

			NPC target = FindTarget(p);
			Vector2 aim;
			if (target != null)
			{
				_lockWho = target.whoAmI;
				aim = target.Center - p.MountedCenter;
				if (aim.LengthSquared() < 0.01f)
					aim = new Vector2(p.direction, 0f);
				Vector2 aimN = Vector2.Normalize(aim);
				p.direction = aimN.X >= 0f ? 1 : -1;

				// 玩家贴到目标侧面并跟随其位移
				Vector2 want = target.Center - aimN * StickDist;
				if (Projectile.owner == Main.myPlayer)
				{
					p.velocity = (want - p.Center) * 0.55f;
					p.Center = Vector2.Lerp(p.Center, want, 0.45f);
					p.fallStart = (int)(p.position.Y / 16f);
				}

				Projectile.Center = Vector2.Lerp(p.MountedCenter, target.Center, 0.55f);
			}
			else
			{
				_lockWho = -1;
				int dir = p.direction;
				Projectile.Center = p.MountedCenter + new Vector2(dir * 40f, 0f);
				aim = new Vector2(dir, 0f);
			}
			if (aim.LengthSquared() < 0.01f)
				aim = new Vector2(p.direction, 0f);
			Vector2 aimDir = Vector2.Normalize(aim);

			if (Projectile.timeLeft % 3 == 0)
			{
				SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f, Pitch = Main.rand.NextFloat(-0.1f, 0.3f) }, Projectile.Center);
				for (int i = 0; i < 4; i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(3.5f, 3.5f), 80, default, 1.15f).noGravity = true;

				if (Projectile.owner == Main.myPlayer)
				{
					// 子 slash 停在打击点（ai2=2 → GenericSlash 不贴玩家）
					int slashDir = aimDir.X >= 0f ? 1 : -1;
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aimDir * 0.01f,
						ModContent.ProjectileType<GenericSlashProj>(), Math.Max(1, Projectile.damage / 3), Projectile.knockBack * 0.25f,
						Projectile.owner, DustID.Blood, slashDir);
					if (id >= 0)
					{
						Main.projectile[id].ai[2] = 2f;
						Main.projectile[id].Center = Projectile.Center;
					}
				}
			}
		}

		private NPC FindTarget(Player p)
		{
			if (_lockWho >= 0 && _lockWho < Main.maxNPCs)
			{
				NPC locked = Main.npc[_lockWho];
				if (locked.active && !locked.friendly && locked.life > 0 && !locked.dontTakeDamage
					&& locked.DistanceSQ(p.Center) < 28f * 16f * 28f * 16f)
					return locked;
			}

			NPC best = null;
			float bestD = 22f * 16f * 22f * 16f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage || !n.CanBeChasedBy())
					continue;
				float d = n.DistanceSQ(p.Center);
				if (d < bestD)
				{
					bestD = d;
					best = n;
				}
			}
			return best;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			Vector2 aim = Projectile.Center - p.MountedCenter;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(p.direction, 0f);
			Vector2 aimN = Vector2.Normalize(aim);

			// HitJagged01：rot=0 时尖端朝左 → +Pi 对齐瞄准方向
			float rot = aimN.ToRotation() + MathHelper.Pi;

			int jagged = HenshinFxDraw.AgeFrame(Lifetime, Projectile.timeLeft, 2, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center, HenshinFxDraw.WithAlpha(new Color(255, 220, 200), 0.8f),
				0.95f, rot, jagged, SpriteEffects.None);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
