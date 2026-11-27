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

		// 导演弹出生即死（远端可能跑不到 AI），施放音改由中根石刺首帧各端放。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			// 中根（ai0=0）首帧各端放施放音：岩石施放 + 一层重挥。
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				if (Math.Abs(Projectile.ai[0]) < 0.5f)
				{
					HenshinSfx.Play(SfxKey.RockCast, owner.MountedCenter, 1f, -0.15f);
					HenshinSfx.Play(SfxKey.SwingHeavy, owner.MountedCenter, 0.6f, -0.2f);
				}
			}
			// 石刺是实物（lit）：不自己发光（原刺尖暖光已删）。
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
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(220, 170, 110), target.Center, 0.5f);
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
			// 只主人听得到的 Item14 删去：石爆首帧各端放岩石碎声 + S 档爆炸配方（RockShatterBurstProj ai0=1）。
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 尖石：自绘 StoneSpike 长石刺（原点=根部），按 jab 从短到长伸出；不再用 Boulder TriangleList。
			EnsureSpike();
			Player owner = Main.player[Projectile.owner];
			float age = Lifetime - Projectile.timeLeft;
			float jab = MathHelper.Clamp(age / 5f, 0f, 1f);
			float reachNow = MathHelper.Lerp(48f, _reach, jab);
			Texture2D tex = (_spikeTex ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/StoneSpike/StoneSpike", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			float rot = _dir.ToRotation();
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			float fade = Math.Min(1f, Projectile.timeLeft / 3f);
			Vector2 scale = new(reachNow / (tex.Width - 4f), MathHelper.Lerp(0.6f, 1f, jab));
			Main.EntitySpriteDraw(tex, (owner.MountedCenter - Main.screenPosition).Floor(), null, Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * fade,
				rot, new Vector2(4f, tex.Height * 0.5f), scale, fx, 0f);
			return false;
		}

		private static ReLogic.Content.Asset<Texture2D> _spikeTex;
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

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			// 两刀：第一刀轻挥，第二刀重挥（各端 AI）。
			if (Projectile.timeLeft == Lifetime - 1)
				HenshinSfx.Play(SfxKey.Swing, Projectile.Center, 1f, 0.15f);
			else if (Projectile.timeLeft == Lifetime / 2)
				HenshinSfx.Play(SfxKey.SwingHeavy, Projectile.Center, 1f, 0.1f);
			int age = Lifetime - Projectile.timeLeft;
			// mixed：AddLight ≤ 0.2（原 0.55/0.45/0.3 等比缩）。
			if (age >= 2 && age < 15)
				Lighting.AddLight(Projectile.Center, 0.2f, 0.16f, 0.11f);
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
			// X 形剑气用 CrossSlash 贴图（同十字劈 Machop 版），中心在交点；不再画贝塞尔光线。
			int age = Lifetime - Projectile.timeLeft;
			int frame = age < 2 ? 0 : age < 4 ? 1 : age < 10 ? 2 : age < 15 ? 3 : 4;
			float opacity = frame == 4 ? Math.Min(1f, Projectile.timeLeft / 3f) : 1f;
			// mixed：X 刃 60% 吃光、40% 自亮（DrawCross 的 color 参数由 B 组加）。
			Color crossC = Color.Lerp(HenshinFxDraw.LightAt(Projectile.Center), Color.White, 0.15f);
			CrossChopArcProj.DrawCross(Projectile.Center, _aim.ToRotation(), frame, 1.6f, opacity, crossC);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 命中星首帧各端放重击声；Heavy 档白闪 + 中震（十字高光由命中星承担，不涂面）。
			H2HitFxProj.Spawn(Projectile, target.Center, 0.7f, Main.rand.NextFloat(MathHelper.TwoPi), G1HitSfx.Code(SfxKey.HitHeavy, 4));
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(255, 200, 150), target.Center);
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

			// 连击期间身前暖白光；最后 1 tick 收尾重击（纯表现：大爆声 + 大招命中音 + M 档爆 + 主人端大震）。
			// mixed：连击光 ≤ 0.2（原 0.6/0.5/0.35 等比缩）；收尾 M 爆炸是大招，保持 glow。
			Lighting.AddLight(Projectile.Center, 0.2f, 0.17f, 0.12f);
			if (Projectile.timeLeft == 1)
			{
				HenshinSfx.Play(SfxKey.BlastM, Projectile.Center, 1f, -0.2f);
				HenshinSfx.PlayCooldown(SfxKey.HitUlt, Projectile.Center, HenshinHitFeel.UltimateWindowTicks);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, new Color(255, 170, 90), 150f);
				HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate, aimDir);
			}

			if (Projectile.timeLeft % 3 == 0)
			{
				// 旧：每 3 tick 同一声 Item1 共 20 声 + 1 颗血尘。现：轻 / 重挥交替（本端节流），血尘删去；命中声由命中星放。
				bool heavy = Projectile.timeLeft % 9 == 0;
				HenshinSfx.PlayCooldown(heavy ? SfxKey.SwingHeavy : SfxKey.Swing, Projectile.Center, 3, heavy ? 0.7f : 0.55f);

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

		private uint _lastHitFx;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 连击每击用 Heavy 档（白闪 + 8 tick 一次小震），大震只留给收尾那一下（钉身近战不做顿帧）。
			HenshinHitFeel.OnHit(Projectile, target, HitTier.Heavy, new Color(255, 190, 120), target.Center);
			// 命中星 + 重击声：本端每 6 tick 至多一个（hitCooldown 2，不节流会刷 30 个特效弹）。
			uint now = Main.GameUpdateCount;
			if (now - _lastHitFx >= 6)
			{
				_lastHitFx = now;
				H2HitFxProj.Spawn(Projectile, target.Center + Main.rand.NextVector2Circular(16f, 16f), 0.9f, Main.rand.NextFloat(MathHelper.TwoPi),
					G1HitSfx.Code(SfxKey.HitHeavy, 5));
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
			// 近身战：每 4 tick 换一组随机位置的拳脚命中星（FlurryHit 4 帧），不再用 HitJagged 叠层。
			int step = Projectile.timeLeft / 4;
			int phase = 3 - Projectile.timeLeft % 4;
			var rng = new Terraria.Utilities.UnifiedRandom(step * 131 + Projectile.identity * 17);
			Texture2D tex = (_flurrySheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/FlurryHit/FlurryHit", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 4;
			Rectangle src = new(0, fh * phase, tex.Width, fh);
			for (int i = 0; i < 3; i++)
			{
				float a = rng.NextFloat() * MathHelper.TwoPi;
				float r = 10f + rng.NextFloat() * 34f;
				Vector2 pos = Projectile.Center + a.ToRotationVector2() * r;
				// 拳影 mixed：60% 吃光、40% 自亮。
				Color fistC = Color.Lerp(HenshinFxDraw.LightAt(pos), Color.White, 0.15f);
				Main.EntitySpriteDraw(tex, (pos - Main.screenPosition).Floor(), src, fistC, rng.NextFloat() * MathHelper.TwoPi,
					src.Size() * 0.5f, 0.7f + rng.NextFloat() * 0.4f, SpriteEffects.None, 0f);
			}
			return false;
		}

		private static ReLogic.Content.Asset<Texture2D> _flurrySheet;
	}
}
