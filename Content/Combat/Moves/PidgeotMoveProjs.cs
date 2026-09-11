using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 暴风天候棒：Barrage 穿透飞弹（不是 Field）。直立帧、不自旋。
	/// 默认撞实心贴地飞；诅咒符 Barrage 穿墙。自管位移，广角镜不弯。
	/// ai2≥0.5=大招（发射时多 4 伴随风）。ai1≥0.5=伴随弹（不二次分裂）。
	/// </summary>
	public class WeatherPainHurricaneProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;

		private const float FlightSpeed = 14f;
		private const float SuckRange = 112f;
		private const int FlightLife = 100;
		private bool _spawnedCompanions;
		private bool _grounded;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WeatherPainShot;

		private bool IsUlt => Projectile.ai[2] > 0.5f;
		private bool IsCompanion => Projectile.ai[1] > 0.5f;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = FlightLife;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1; // 穿透
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.WeatherPainShot);
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Player p = Main.player[Projectile.owner];
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (dir.LengthSquared() < 1f)
					dir = new Vector2(p.direction, 0f);
				Projectile.velocity = Vector2.Normalize(dir) * FlightSpeed;
			}
			else
				Projectile.velocity = Vector2.Normalize(Projectile.velocity) * FlightSpeed;

			// 大招主弹：四道伴随风（扇形）
			if (IsUlt && !IsCompanion && !_spawnedCompanions && Projectile.owner == Main.myPlayer)
			{
				_spawnedCompanions = true;
				Vector2 baseVel = Projectile.velocity;
				float[] spreads = { -0.28f, -0.12f, 0.12f, 0.28f };
				int companionDmg = Math.Max(1, Projectile.damage * 2 / 3);
				foreach (float ang in spreads)
				{
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, baseVel.RotatedBy(ang),
						ModContent.ProjectileType<WeatherPainHurricaneProj>(), companionDmg, Projectile.knockBack * 0.85f,
						Projectile.owner, 0f, 1f, 1f); // ai1=companion, ai2=ult
					if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.EasyCrit = EasyCrit;
						tagged.Delivery = Delivery == MoveDelivery.None ? MoveDelivery.Barrage : Delivery;
					}
				}
			}
		}

		public override void AI()
		{
			// 天候棒：直立，靠帧动画；不绕圆心自旋
			Projectile.rotation = 0f;
			Projectile.frameCounter++;
			int frames = Math.Max(1, Main.projFrames[ProjectileID.WeatherPainShot]);
			if (Projectile.frameCounter >= 3)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % frames;
			}

			Lighting.AddLight(Projectile.Center, 0.35f, 0.45f, 0.6f);
			Dust.NewDustPerfect(Projectile.Center, DustID.Cloud,
				-Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(1.5f, 1.5f), 100,
				new Color(160, 190, 230), 1.2f).noGravity = true;

			if (_grounded)
				SnapToGround();

			float pullStr = IsUlt ? 8f : 6f;
			float carry = IsUlt ? 0.65f : 0.55f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Vector2.Distance(n.Center, Projectile.Center);
				if (d > SuckRange)
					continue;
				Vector2 pull = Projectile.Center - n.Center;
				if (pull.LengthSquared() > 1f)
				{
					pull.Normalize();
					n.velocity = Vector2.Lerp(n.velocity, pull * pullStr + Projectile.velocity * carry, IsUlt ? 0.42f : 0.35f);
				}
				else
					n.Center = Vector2.Lerp(n.Center, Projectile.Center, 0.25f);
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			_grounded = true;
			float sx = Math.Abs(Projectile.velocity.X) > 0.01f
				? Math.Sign(Projectile.velocity.X)
				: Math.Sign(oldVelocity.X);
			if (sx == 0)
				sx = Main.player[Projectile.owner].direction;
			if (Math.Abs(Projectile.velocity.X) < 0.01f && Math.Abs(oldVelocity.X) > 0.01f)
				sx = -Math.Sign(oldVelocity.X);
			Projectile.velocity.X = sx * FlightSpeed;
			Projectile.velocity.Y = 0f;
			return false;
		}

		private void SnapToGround()
		{
			int tileX = (int)(Projectile.Center.X / 16f);
			int startY = (int)(Projectile.Center.Y / 16f);
			bool found = false;
			for (int y = startY; y < startY + 40 && y < Main.maxTilesY; y++)
			{
				Tile tile = Framing.GetTileSafely(tileX, y);
				if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				{
					Projectile.position.Y = y * 16f - Projectile.height;
					found = true;
					break;
				}
			}
			Projectile.velocity.Y = found ? 0f : 6f;
			if (Math.Abs(Projectile.velocity.X) < 0.1f)
				Projectile.velocity.X = Main.player[Projectile.owner].direction * FlightSpeed;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, IsUlt ? 40 : 24);
			// 穿透：不因命中结束；仅首次命中锚定环绕风（主弹，非伴随）
			if (IsCompanion || Projectile.owner != Main.myPlayer)
				return;
			if (Projectile.localAI[1] > 0.5f)
				return;
			Projectile.localAI[1] = 1f;
			SpawnSideWinds(target);
		}

		public override void OnKill(int timeLeft)
		{
			if (IsCompanion || Projectile.localAI[1] > 0.5f || Projectile.owner != Main.myPlayer)
				return;
			NPC nearest = null;
			float best = SuckRange * SuckRange * 1.5f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
				if (d < best)
				{
					best = d;
					nearest = n;
				}
			}
			if (nearest != null)
				SpawnSideWinds(nearest);
		}

		private void SpawnSideWinds(NPC target)
		{
			SoundEngine.PlaySound(SoundID.Item66, target.Center);
			int dmg = Math.Max(1, Projectile.damage * 2 / 3);
			int count = IsUlt ? 4 : 2;
			for (int i = 0; i < count; i++)
			{
				float phase = i / (float)count;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<WeatherPainOrbitProj>(), dmg, Projectile.knockBack * 0.4f, Projectile.owner,
					target.whoAmI, phase, IsUlt ? 1f : 0f);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					tagged.EasyCrit = EasyCrit;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.WeatherPainShot);
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.WeatherPainShot);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.WeatherPainShot]);
			int frame = Projectile.frame % frames;
			Rectangle src = tex.Frame(1, frames, 0, frame);
			float scale = IsCompanion ? 0.95f : 1.2f;
			// 直立天候棒动画，不绕圆心转
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, Color.White,
				0f, src.Size() * 0.5f, scale, SpriteEffects.None);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(140, 180, 230), 0.4f), 0.5f * scale);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 命中后左右摆动的天候棒（直立帧动画，不绕圆心自旋）。
	/// ai0=npc；ai1=相位 0~1；ai2=ult。
	/// </summary>
	public class WeatherPainOrbitProj : HenshinMoveProj
	{
		private const int Life = 110;
		private const float SideAmp = 56f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WeatherPainShot;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			int npcId = (int)Projectile.ai[0];
			if (npcId < 0 || npcId >= Main.maxNPCs || !Main.npc[npcId].active)
			{
				Projectile.Kill();
				return;
			}
			NPC target = Main.npc[npcId];
			float age = Life - Projectile.timeLeft;
			float phase = Projectile.ai[1] * MathHelper.TwoPi;
			bool ult = Projectile.ai[2] > 0.5f;
			// 左右往复（类似天候棒贴敌悬浮），不绕圆心公转
			float swing = MathF.Sin(age * 0.2f + phase) * SideAmp * (ult ? 1.15f : 1f);
			float bob = MathF.Cos(age * 0.15f + phase) * 10f;
			Projectile.Center = target.Center + new Vector2(swing, bob - 8f);
			Projectile.rotation = 0f;

			int frames = Math.Max(1, Main.projFrames[ProjectileID.WeatherPainShot]);
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= 3)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % frames;
			}

			// 轻牵引贴向目标
			if (Vector2.DistanceSquared(target.Center, Projectile.Center) < 80f * 80f)
			{
				Vector2 pull = Projectile.Center - target.Center;
				if (pull.LengthSquared() > 4f)
					target.velocity = Vector2.Lerp(target.velocity, Vector2.Normalize(pull) * (ult ? 3.5f : 2.2f), 0.2f);
			}

			Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, Main.rand.NextVector2Circular(2f, 2f), 120,
				new Color(150, 185, 230), 1.1f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Vector2 knock = target.Center - Projectile.Center;
			if (knock.LengthSquared() > 1f)
				target.velocity += Vector2.Normalize(knock) * 1.8f;
			target.AddBuff(BuffID.Slow, 20);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.WeatherPainShot);
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.WeatherPainShot);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.WeatherPainShot]);
			Rectangle src = tex.Frame(1, frames, 0, Projectile.frame % frames);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, Color.White * 0.95f,
				0f, src.Size() * 0.5f, 1.05f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>燕返：闪现双切后回起点；白羽弧径；禁电光尘。</summary>
	public class AerialAceBlinkProj : HenshinMoveProj
	{
		private const float SearchRange = 960f;
		private const int PhaseSlash1 = 1;
		private const int PhaseSlash2 = 2;
		private const int PhaseReturn = 3;

		private Vector2 _origin;
		private Vector2 _dest;
		private int _phase;
		private int _phaseTimer;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
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
				_origin = p.Center;
				Vector2 cursor = HenshinProjUtil.OwnerMouseWorld(Projectile);
				NPC target = null;
				float best = SearchRange * SearchRange;
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
						continue;
					float d = Vector2.DistanceSquared(cursor, n.Center);
					if (d < best)
					{
						best = d;
						target = n;
					}
				}

				if (target != null)
				{
					_dest = target.Center - new Vector2(p.direction * 28f, 0f);
					if (Projectile.owner == Main.myPlayer)
					{
						SpawnArcTrail(_origin, _dest, go: true);
						p.Teleport(_dest, -1);
						p.immune = true;
						p.immuneTime = Math.Max(p.immuneTime, 18);
					}
					SoundEngine.PlaySound(SoundID.Item1, _dest);
					_phase = PhaseSlash1;
					_phaseTimer = 6;
				}
				else
				{
					Vector2 dest = cursor;
					Vector2 delta = dest - p.Center;
					if (delta.Length() > SearchRange)
						dest = p.Center + Vector2.Normalize(delta) * SearchRange;
					_dest = dest;
					if (Projectile.owner == Main.myPlayer)
					{
						SpawnArcTrail(_origin, _dest, go: true);
						p.Teleport(_dest, -1);
					}
					SoundEngine.PlaySound(SoundID.Item1, _dest);
					_phase = PhaseReturn;
					_phaseTimer = 4;
				}
			}

			Projectile.Center = p.Center;
			SpawnFeatherDust(p.Center);

			_phaseTimer--;
			if (_phaseTimer > 0)
				return;

			switch (_phase)
			{
				case PhaseSlash1:
					_phase = PhaseSlash2;
					_phaseTimer = 6;
					break;
				case PhaseSlash2:
					_phase = PhaseReturn;
					_phaseTimer = 2;
					if (Projectile.owner == Main.myPlayer)
					{
						SpawnArcTrail(_dest, _origin, go: false);
						p.Teleport(_origin, -1);
						p.immune = true;
						p.immuneTime = Math.Max(p.immuneTime, 10);
					}
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, _origin);
					break;
				case PhaseReturn:
					Projectile.Kill();
					break;
			}
		}

		public override bool? CanDamage()
		{
			if (_phase == PhaseSlash1 || _phase == PhaseSlash2)
				return null;
			return false;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (_phase != PhaseSlash1 && _phase != PhaseSlash2)
				return false;
			return targetHitbox.Intersects(projHitbox);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 6; i++)
				Dust.NewDustPerfect(target.Center, DustID.Cloud, Main.rand.NextVector2Circular(4f, 4f), 80,
					new Color(230, 235, 245), 1.3f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_phase != PhaseSlash1 && _phase != PhaseSlash2)
				return false;
			float rot = (_dest - _origin).SafeNormalize(Vector2.UnitX).ToRotation();
			int jagged = HenshinFxDraw.AgeFrame(6, Math.Max(1, _phaseTimer), 2, HenshinFxDraw.HitJaggedFrames);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawHitJaggedFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(240, 245, 255), 0.85f), 0.9f, rot + MathHelper.Pi, jagged);
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(220, 230, 245), 0.45f), 0.4f);
			HenshinFxDraw.EndAdditive();
			return false;
		}

		private static void SpawnFeatherDust(Vector2 at)
		{
			if (!Main.rand.NextBool(2))
				return;
			Dust d = Dust.NewDustPerfect(at + Main.rand.NextVector2Circular(12f, 12f), DustID.Cloud,
				Main.rand.NextVector2Circular(2f, 2f), 100, new Color(235, 240, 250), 1.15f);
			d.noGravity = true;
		}

		private void SpawnArcTrail(Vector2 from, Vector2 to, bool go)
		{
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), from, Vector2.Zero,
				ModContent.ProjectileType<AerialAceArcTrailProj>(), 0, 0f, Projectile.owner,
				to.X, to.Y, go ? 1f : -1f);
			if (id >= 0)
				Main.projectile[id].Center = from;
		}
	}

	/// <summary>燕返弧形粒子径：从 Center 到 (ai0,ai1)；ai2 符号控制弧向。</summary>
	public class AerialAceArcTrailProj : HenshinMoveProj
	{
		private const int Life = 18;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Vector2 from = Projectile.Center;
			Vector2 to = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			float t = 1f - Projectile.timeLeft / (float)Life;
			float arcSign = Projectile.ai[2] >= 0 ? 1f : -1f;
			Vector2 mid = Vector2.Lerp(from, to, 0.5f);
			Vector2 delta = to - from;
			Vector2 perp = new Vector2(-delta.Y, delta.X);
			if (perp.LengthSquared() > 1f)
				perp = Vector2.Normalize(perp) * (delta.Length() * 0.22f * arcSign);
			// 二次贝塞尔采样
			for (int i = 0; i < 4; i++)
			{
				float u = MathHelper.Clamp(t + i * 0.04f, 0f, 1f);
				Vector2 a = Vector2.Lerp(from, mid + perp, u);
				Vector2 b = Vector2.Lerp(mid + perp, to, u);
				Vector2 pos = Vector2.Lerp(a, b, u);
				Dust d = Dust.NewDustPerfect(pos, DustID.Cloud, Vector2.Zero, 80, new Color(230, 235, 250), 1.35f);
				d.noGravity = true;
				d.velocity = perp * 0.02f;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>勇鸟猛攻：白拖尾梭形冲（闪焰式加速）+ 命中交叉鸟弹。</summary>
	public class BraveBirdLungeProj : HenshinMoveProj
	{
		private const int BrakeTicks = 10;
		private const float ReachTiles = 28f;

		private Vector2 _dir;
		private int _dashLife;
		private int _lifetime;
		private float _speed;
		private float _cruiseSpeed;
		private Vector2 _lastTrail;
		private bool _birdsSpawned;

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
				_dashLife = (int)MathHelper.Clamp(ReachTiles * 0.75f, 18f, 40f);
				_speed = ReachTiles * 16f / _dashLife;
				_lifetime = _dashLife + BrakeTicks;
				Projectile.timeLeft = _lifetime;
				Projectile.localNPCHitCooldown = _dashLife;
				_cruiseSpeed = Math.Max(p.maxRunSpeed, 6f);
				_lastTrail = p.Center;
				SoundEngine.PlaySound(SoundID.Item1, p.Center);
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
			Projectile.rotation = _dir.ToRotation();

			if (!InBrake)
			{
				Vector2 perp = new Vector2(-_dir.Y, _dir.X);
				// 断续白色多线拖尾
				if (Vector2.DistanceSquared(_lastTrail, p.Center) > 22f * 22f)
				{
					_lastTrail = p.Center;
					for (int lane = -2; lane <= 2; lane++)
					{
						if (lane != 0 && Main.rand.NextBool(3))
							continue; // 断续
						float side = lane * 9f + Main.rand.NextFloat(-2f, 2f);
						Vector2 basePos = p.Center + perp * side;
						for (int i = 0; i < 2; i++)
						{
							Dust trail = Dust.NewDustPerfect(basePos - _dir * (i * 8f), DustID.Cloud,
								-_dir * Main.rand.NextFloat(0.3f, 1.0f) + perp * Main.rand.NextFloat(-0.3f, 0.3f),
								70, new Color(245, 248, 255), Main.rand.NextFloat(1.2f, 1.7f));
							trail.noGravity = true;
						}
					}
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
			SpawnCrossBirds(target.Center);
		}

		public override void OnKill(int timeLeft)
		{
			Player p = Main.player[Projectile.owner];
			if (p.active && Projectile.owner == Main.myPlayer && p.velocity.Length() > _cruiseSpeed + 1f)
				p.velocity = _dir * _cruiseSpeed;
			if (!_birdsSpawned && Projectile.owner == Main.myPlayer)
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
				// 交叉：半数反向
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
			// 梭形包裹：沿瞄准方向拉长 SoftGlow
			HenshinFxDraw.BeginAdditive();
			float rot = _dir.ToRotation();
			Color spindle = HenshinFxDraw.WithAlpha(new Color(245, 248, 255), InBrake ? 0.35f : 0.7f);
			Texture2D glow = HenshinFxDraw.SoftGlow;
			if (glow != null)
			{
				Vector2 origin = glow.Size() * 0.5f;
				Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, spindle, rot,
					origin, new Vector2(1.8f, 0.55f), SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null,
					HenshinFxDraw.WithAlpha(Color.White, InBrake ? 0.2f : 0.45f), rot,
					origin, new Vector2(1.2f, 0.35f), SpriteEffects.None, 0f);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>勇鸟交叉鸟弹：Raven 壳自管直线。</summary>
	public class BraveBirdCrossProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Raven;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
			Projectile.scale = 1.1f;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.Raven);
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, -Projectile.velocity * 0.05f, 120,
					new Color(240, 245, 255), 1.0f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Raven);
			int frames = Math.Max(1, Main.projFrames[ProjectileID.Raven]);
			Rectangle frame = tex.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 4) % frames);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White,
				Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
			return false;
		}
	}
}
