using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>连发导演：ai0=模式(1泡泡 2种子 3共鸣)，ai1=数量，ai2=射速。</summary>
	public class BarrageDirectorProj : HenshinMoveProj
	{
		public const float ModeBubble = 1f;
		public const float ModeSeed = 2f;
		public const float ModeResonance = 3f;

		private int _fired;
		private int _total;
		private float _speed;
		private Vector2 _dir;
		private int _childType;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void OnSpawn(IEntitySource source)
		{
			_total = (int)System.Math.Max(1, Projectile.ai[1]);
			_speed = Projectile.ai[2] > 0f ? Projectile.ai[2] : 16f;
			Player owner = Main.player[Projectile.owner];
			_dir = Main.MouseWorld - owner.MountedCenter;
			if (_dir == Vector2.Zero)
				_dir = new Vector2(owner.direction, 0f);
			_dir.Normalize();

			_childType = Projectile.ai[0] switch
			{
				ModeBubble => ProjectileBorrow.ItemShoot(ItemID.BubbleGun),
				ModeSeed => ProjectileID.Seed,
				ModeResonance => ProjectileBorrow.ItemShoot(ItemID.PrincessWeapon),
				_ => ProjectileID.Seed
			};
			if (_childType <= 0)
				_childType = Projectile.ai[0] == ModeBubble ? ProjectileID.Bubble : ProjectileID.Seed;
			// 泡沫光线：提前 Load Bubble 贴图（勿等玩家先用泡泡枪）
			if (Projectile.ai[0] == ModeBubble)
				Main.instance.LoadProjectile(ProjectileID.Bubble);
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			Projectile.Center = owner.MountedCenter;
			bool scatter = Projectile.ai[0] == ModeResonance;
			bool bubble = Projectile.ai[0] == ModeBubble;
			int duration = scatter ? 36 : (bubble ? 36 : 48);
			int perTick = System.Math.Max(1, (_total + duration - 1) / duration);
			if (bubble)
				perTick = System.Math.Max(perTick, 2);

			if (Projectile.owner == Main.myPlayer && _fired < _total)
			{
				int budget = System.Math.Min(perTick, _total - _fired);
				for (int i = 0; i < budget; i++)
				{
					Vector2 vel;
					Vector2 spawn;
					if (scatter)
					{
						spawn = Main.MouseWorld + Main.rand.NextVector2Circular(48f, 48f);
						vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
					}
					else if (bubble)
					{
						// 窄直线束：速度随机，横向几乎不散开；飞行保持直线（子弹无摆动）
						float speedJitter = _speed * Main.rand.NextFloat(0.75f, 1.2f);
						vel = _dir * speedJitter;
						Vector2 perp = new Vector2(-_dir.Y, _dir.X);
						spawn = owner.MountedCenter + _dir * 16f
							+ perp * Main.rand.NextFloat(-4f, 4f);
					}
					else
					{
						float spread = (_fired / (float)_total - 0.5f) * 0.12f;
						vel = _dir.RotatedBy(spread) * _speed;
						spawn = owner.MountedCenter + _dir * 16f + Main.rand.NextVector2Circular(4f, 4f);
					}

					int perShot = System.Math.Max(1, Projectile.damage / (_total <= 32 ? 4 : 6));
					int id;
					if (Projectile.ai[0] == ModeResonance)
					{
						id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel * 0.15f,
							ModContent.ProjectileType<BorrowedVisualBoltProj>(), System.Math.Max(1, (int)(Projectile.damage * 0.35f)), Projectile.knockBack * 0.4f, Projectile.owner,
							ProjectileID.RainbowRodBullet, 0f, 1f);
					}
					else
					{
						// 泡泡强制用可见 Bubble 贴图；种子用 Seed。不直接 NewProjectile 原版弹：其 AI/伤害类不可控。
						int texId = Projectile.ai[0] == ModeBubble ? ProjectileID.Bubble : ProjectileID.Seed;
						id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
							ModContent.ProjectileType<BorrowedVisualBoltProj>(), perShot, Projectile.knockBack * 0.2f, Projectile.owner,
							texId, EasyCrit ? 1f : 0f, 0f);
					}

					if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.EasyCrit = EasyCrit;
						tagged.IgnoreDefensePartial = IgnoreDefensePartial;
					}
					_fired++;
				}
			}

			if (_fired >= _total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>借用原版贴图与基础运动的伤害弹。ai0=贴图用 Projectile type。</summary>
	public class BorrowedVisualBoltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private int _texType;
		private Color _tint = Color.White;
		private Vector2 _baseVel;
		private bool _popped;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
		}

		public override void OnSpawn(IEntitySource source)
		{
			_texType = (int)Projectile.ai[0];
			if (_texType <= 0)
				_texType = ProjectileID.Seed;
			// 精神强念等：强制可见彩虹杖贴图 + 延迟追踪标记（ai2）
			if (Projectile.ai[2] > 0.5f || _texType == ProjectileBorrow.ItemShoot(ItemID.PrincessWeapon))
			{
				_texType = ProjectileID.RainbowRodBullet;
				_tint = new Color(230, 140, 255);
			}
			// 泡泡：始终用 ProjectileID.Bubble，并立刻 Load（勿等玩家先用泡泡枪）
			if (_texType == ProjectileID.Bubble || _texType == ProjectileBorrow.ItemShoot(ItemID.BubbleGun))
			{
				_texType = ProjectileID.Bubble;
				Main.instance.LoadProjectile(ProjectileID.Bubble);
				Projectile.scale = Main.rand.NextFloat(0.85f, 1.25f);
			}
			else
				Main.instance.LoadProjectile(_texType);

			EasyCrit = Projectile.ai[1] > 0f;
			Projectile.penetrate = _texType == ProjectileID.RainbowRodBullet ? 3 : 1;
			if (_texType == ProjectileID.Typhoon)
				_tint = new Color(255, 120, 40);
			if (_texType == ProjectileID.RainbowRodBullet)
			{
				Projectile.tileCollide = false;
				Projectile.scale = 1.35f;
				Projectile.timeLeft = 180;
			}
			_baseVel = Projectile.velocity;
		}

		public void SetTint(Color c) => _tint = c;

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (_texType == ProjectileID.Seed || _texType == ProjectileID.Bubble)
				Projectile.tileCollide = true;
			else if (_texType != ProjectileID.RainbowRodBullet)
				Projectile.tileCollide = false;

			if (_texType == ProjectileID.Bubble)
				Projectile.localAI[0]++; // 拖尾摆动相位用

			// 共鸣弹：生成 0.5s 后开始追踪
			if (Projectile.ai[2] > 0.5f)
			{
				Projectile.localAI[1]++;
				if (Projectile.localAI[1] >= 30f)
				{
					Homing = true;
					HomingTurnRate = 0.18f;
					if (Projectile.velocity.LengthSquared() < 1f)
						Projectile.velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
					else if (Projectile.velocity.Length() < 8f)
						Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 8f;
				}
			}
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);

			if (_texType == ProjectileID.Bubble)
			{
				// 不再刷蓝水尘；小泡拖尾在 PreDraw 画
			}
			else
			{
				int dust = _texType switch
				{
					_ when _texType == ProjectileID.Seed => DustID.Grass,
					_ when _texType == ProjectileID.RainbowRodBullet => DustID.PinkTorch,
					_ when _texType == ProjectileID.Typhoon => DustID.Torch,
					_ => DustID.Smoke
				};
				if (Main.rand.NextBool(2) || _texType == ProjectileID.RainbowRodBullet)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, dust, Projectile.velocity * 0.1f, 100, _tint, _texType == ProjectileID.RainbowRodBullet ? 1.4f : 1.15f);
					d.noGravity = true;
				}
			}
			Lighting.AddLight(Projectile.Center, _tint.ToVector3() * 0.35f);
		}

		private void PopBubbles()
		{
			if (_popped || _texType != ProjectileID.Bubble)
				return;
			_popped = true;
			SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.55f, Pitch = Main.rand.NextFloat(-0.15f, 0.25f) }, Projectile.Center);
			if (Projectile.owner != Main.myPlayer)
				return;
			for (int i = 0; i < 4; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(3.5f, 3.5f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
					ModContent.ProjectileType<BubblePopVisualProj>(), 0, 0f, Projectile.owner,
					Main.rand.NextFloat(0.25f, 0.55f), 14f);
			}
		}

		public override void OnKill(int timeLeft) => PopBubbles();

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			PopBubbles();
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => PopBubbles();

		public override bool PreDraw(ref Color lightColor)
		{
			int drawId = _texType > 0 ? _texType : ProjectileID.Seed;
			if (drawId == ProjectileBorrow.ItemShoot(ItemID.PrincessWeapon) || drawId <= 0)
				drawId = ProjectileID.RainbowRodBullet;
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(drawId);
			Rectangle frame = tex.Frame();
			Vector2 origin = frame.Size() * 0.5f;
			if (_texType == ProjectileID.Bubble)
			{
				// 身后两枚缩小泡当粒子（确定性偏移，避免 PreDraw 闪烁）
				Vector2 back = Projectile.velocity.LengthSquared() > 0.01f
					? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
				Vector2 perp = new Vector2(-back.Y, back.X);
				for (int i = 1; i <= 2; i++)
				{
					float sway = (float)System.Math.Sin(Projectile.localAI[0] * 0.15f + i) * 3f;
					Vector2 pos = Projectile.Center - back * (10f * i) + perp * sway;
					float s = Projectile.scale * (0.35f - i * 0.08f);
					Color c = Color.White * (0.55f / i);
					Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, c, 0f, origin, s, SpriteEffects.None, 0);
				}
			}
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, _tint, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>无伤小泡视觉（拖尾粒子 / 破裂散片）。ai0=scale，ai1=寿命。</summary>
	public class BubblePopVisualProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bubble;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.damage = 0;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.Bubble);
			float life = Projectile.ai[1] > 1f ? Projectile.ai[1] : 14f;
			Projectile.timeLeft = (int)life;
			Projectile.scale = Projectile.ai[0] > 0.05f ? Projectile.ai[0] : 0.4f;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.92f;
			Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / (float)System.Math.Max(1, Projectile.ai[1] > 1f ? Projectile.ai[1] : 14f)));
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ProjectileBorrow.RequestProjectileTexture(ProjectileID.Bubble);
			Rectangle frame = tex.Frame();
			float fade = 1f - Projectile.alpha / 255f;
			Color c = Color.White * fade;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, c, 0f, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>小火龙大招：指针生成，原版利刃台风橙红染色，接触首敌后锁定追踪。</summary>
	public class FlareBoltUltProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private int _lockNpc = -1;
		private bool _halted;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void OnSpawn(IEntitySource source)
		{
			if (Projectile.velocity.LengthSquared() < 0.01f)
				Projectile.velocity = new Vector2(0f, -6f);
			else
				Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 10f;
		}

		public override void AI()
		{
			Projectile.rotation += 0.25f;
			if (_halted)
			{
				Projectile.velocity = Vector2.Zero;
			}
			else if (_lockNpc >= 0)
			{
				NPC n = Main.npc[_lockNpc];
				if (!n.active || n.life <= 0)
				{
					_lockNpc = -1;
				}
				else
				{
					Vector2 desired = n.Center - Projectile.Center;
					if (desired != Vector2.Zero)
					{
						desired.Normalize();
						Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired * 16f, 0.2f);
					}
				}
			}
			else
			{
				HenshinProjUtil.HomingAI(Projectile, true, 0.06f);
			}

			Color c = new(255, 100, 30);
			for (int i = 0; i < 2; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.Torch, -Projectile.velocity * 0.1f, 80, c, 1.4f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 1f, 0.4f, 0.1f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			_halted = true;
			_lockNpc = -1;
			Projectile.velocity = Vector2.Zero;
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!_halted && _lockNpc < 0)
				_lockNpc = target.whoAmI;
			target.AddBuff(BuffID.OnFire, 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.Typhoon].Value;
			int frames = System.Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			if (frames > 1)
			{
				Projectile.frameCounter++;
				if (Projectile.frameCounter >= 4)
				{
					Projectile.frameCounter = 0;
					Projectile.frame = (Projectile.frame + 1) % frames;
				}
			}
			Rectangle frame = tex.Frame(1, frames, 0, Projectile.frame);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(255, 110, 40), Projectile.rotation, frame.Size() * 0.5f, 1.15f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>海蓝权杖式水枪。</summary>
	public class AquaScepterShotProj : HenshinMoveProj
	{
		private int _texType;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WaterStream;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 2;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void OnSpawn(IEntitySource source)
		{
			_texType = ProjectileBorrow.ItemShoot(ItemID.AquaScepter);
			if (_texType <= 0)
				_texType = ProjectileID.WaterStream;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Water, Projectile.velocity * 0.2f, 100, default, 1.3f);
			d.noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Wet, 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int drawId = _texType > 0 ? _texType : ProjectileID.WaterStream;
			Texture2D tex = TextureAssets.Projectile[drawId].Value;
			if (tex.Width <= 1)
				tex = TextureAssets.Projectile[ProjectileID.WaterBolt].Value;
			Rectangle frame = tex.Frame();
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(120, 200, 255), Projectile.rotation, frame.Size() * 0.5f, 1.2f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>草系鞭：皮鞭级射程，草尘段点。</summary>
	public class GrassWhipProj : HenshinMoveProj
	{
		private const float Range = 176f; // ~11 tiles, leather-whip-like
		private const int Lifetime = 28;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			float t = 1f - Projectile.timeLeft / (float)Lifetime;
			float reach = Range * MathHelper.Clamp(t * 1.6f, 0f, 1f);
			Vector2 aim = Main.MouseWorld - owner.MountedCenter;
			if (aim == Vector2.Zero)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			float swing = (float)System.Math.Sin(t * MathHelper.Pi) * 0.35f;
			Vector2 tip = owner.MountedCenter + aim.RotatedBy(swing) * reach;
			Projectile.Center = tip;

			for (float s = 0.15f; s <= 1f; s += 0.12f)
			{
				Vector2 p = Vector2.Lerp(owner.MountedCenter, tip, s);
				Dust d = Dust.NewDustPerfect(p, DustID.Grass, aim * 0.5f, 80, new Color(80, 200, 80), 1.25f);
				d.noGravity = true;
				if (Main.rand.NextBool(3))
					Dust.NewDustPerfect(p, DustID.ChlorophyteWeapon, Vector2.Zero, 120, default, 0.9f).noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			float t = 1f - Projectile.timeLeft / (float)Lifetime;
			float reach = Range * MathHelper.Clamp(t * 1.6f, 0f, 1f);
			Vector2 aim = Main.MouseWorld - owner.MountedCenter;
			if (aim == Vector2.Zero)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			Vector2 tip = owner.MountedCenter + aim * reach;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), owner.MountedCenter, tip, 22f, ref point);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>电光一闪：闪现至指针最近敌人，在敌人处电光爆炸造成伤害（非接触）。</summary>
	public class BlinkStrikeProj : HenshinMoveProj
	{
		private const float SearchRange = 960f;

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
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.localAI[0] == 0f && Projectile.owner == Main.myPlayer)
			{
				Projectile.localAI[0] = 1f;
				Vector2 cursor = Main.MouseWorld;
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

				Vector2 from = p.Center;
				if (target != null)
				{
					Vector2 dest = target.Center - new Vector2(p.direction * 28f, 0f);
					p.Teleport(dest, -1);
					p.immune = true;
					p.immuneTime = System.Math.Max(p.immuneTime, 15);
					for (int i = 0; i < 20; i++)
					{
						Dust.NewDustPerfect(from, DustID.Electric, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.5f).noGravity = true;
						Dust.NewDustPerfect(p.Center, DustID.Electric, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.5f).noGravity = true;
					}
					SoundEngine.PlaySound(SoundID.Item8, p.Center);
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
						ModContent.ProjectileType<BlinkStrikeBurstProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				}
				else
				{
					// 无目标：短距闪到指针，无伤害
					Vector2 dest = cursor;
					Vector2 delta = dest - p.Center;
					if (delta.Length() > SearchRange)
						dest = p.Center + Vector2.Normalize(delta) * SearchRange;
					p.Teleport(dest, -1);
					SoundEngine.PlaySound(SoundID.Item8, p.Center);
				}
			}

			Projectile.Center = p.Center;
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>电光一闪落点爆炸。</summary>
	public class BlinkStrikeBurstProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 12;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);
			}
			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), DustID.Electric, Main.rand.NextVector2Circular(6f, 6f), 60, default, 1.6f);
				d.noGravity = true;
				Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.3f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.9f, 0.9f, 0.3f);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>十万伏特大招：粗穿透电光；命中 50% 感电；0.5s 后对感电目标再射。</summary>
	public class ThunderboltUltProj : HenshinMoveProj
	{
		private bool _scheduled;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagnetSphereBolt;

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = false;
			Projectile.extraUpdates = 3;
			Projectile.scale = 2.2f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			for (int i = 0; i < 4; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.Electric, -Projectile.velocity * 0.05f, 40, default, 1.9f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.8f, 0.85f, 1.1f);

			if (Projectile.ai[2] < 0.5f && !_scheduled && Projectile.owner == Main.myPlayer)
			{
				_scheduled = true;
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.player[Projectile.owner].MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<ThunderboltChainDirector>(), Projectile.damage, 0f, Projectile.owner);
				if (id >= 0)
					Main.projectile[id].originalDamage = Projectile.damage;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.rand.NextFloat() < 0.5f)
				target.AddBuff(BuffID.Electrified, 300);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.MagnetSphereBolt].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.LightYellow, Projectile.rotation, tex.Size() * 0.5f, 2.8f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * 0.5f, Projectile.rotation, tex.Size() * 0.5f, 2.0f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>0.5 秒后从玩家向所有感电敌再放十万伏特。</summary>
	public class ThunderboltChainDirector : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 4;
			Projectile.height = 4;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 30;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Projectile.Center = Main.player[Projectile.owner].MountedCenter;
			if (Projectile.timeLeft > 1 || Projectile.owner != Main.myPlayer)
				return;

			Player p = Main.player[Projectile.owner];
			int dmg = Projectile.damage > 0 ? Projectile.damage : Projectile.originalDamage;
			if (dmg <= 0)
				dmg = 1;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.HasBuff(BuffID.Electrified))
					continue;
				Vector2 toNpc = n.Center - p.MountedCenter;
				if (toNpc == Vector2.Zero)
					toNpc = Vector2.UnitX;
				float dist = MathHelper.Clamp(toNpc.Length(), 48f, 64f * 16f);
				Vector2 vel = Vector2.Normalize(toNpc) * dist;
				// ai0=0 光束；velocity 长度=劈距；ai2=1 不再挂导演
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.MountedCenter, vel,
					ModContent.ProjectileType<SkyBoltLightningProj>(), dmg, 1f, Projectile.owner, 0f, 0f, 1f);
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>岩石封锁：十字四石向内收拢碎裂。SpawnAtMouse。</summary>
	public class RockTombDirectorProj : HenshinMoveProj
	{
		private Vector2 _center;
		private readonly Vector2[] _offsets = new Vector2[4];
		private bool _spawned;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void OnSpawn(IEntitySource source)
		{
			_center = Projectile.Center;
			_offsets[0] = new Vector2(0f, -56f);
			_offsets[1] = new Vector2(0f, 56f);
			_offsets[2] = new Vector2(-56f, 0f);
			_offsets[3] = new Vector2(56f, 0f);
		}

		public override void AI()
		{
			if (!_spawned && Projectile.owner == Main.myPlayer)
			{
				_spawned = true;
				for (int i = 0; i < 4; i++)
				{
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), _center + _offsets[i], Vector2.Zero,
						ModContent.ProjectileType<RockTombShardProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
						_offsets[i].X, _offsets[i].Y, ProjectileID.Boulder);
				}
			}

			float t = 1f - Projectile.timeLeft / 40f;
			Projectile.Center = _center;
			// 伤害盒在收拢后半段扩大
			if (t > 0.55f)
			{
				Projectile.width = 72;
				Projectile.height = 72;
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class RockTombShardProj : HenshinMoveProj
	{
		private Vector2 _startOff;
		private bool _shattered;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Boulder;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.scale = 0.5f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void OnSpawn(IEntitySource source)
		{
			_startOff = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			Projectile.scale = 0.5f;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = Projectile.Center.X - _startOff.X;
				Projectile.localAI[1] = Projectile.Center.Y - _startOff.Y;
			}
			Vector2 center = new(Projectile.localAI[0], Projectile.localAI[1]);
			float t = 1f - Projectile.timeLeft / 36f;
			float pull = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(t * 1.4f, 0f, 1f));
			Projectile.Center = center + _startOff * (1f - pull);
			Projectile.rotation += 0.12f;

			Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, new Color(140, 100, 60), 1.1f);
			trail.noGravity = true;

			if (t > 0.85f && !_shattered)
			{
				_shattered = true;
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
				for (int i = 0; i < 18; i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(6f, 6f), 80, new Color(140, 100, 60), 1.4f);
					d.noGravity = Main.rand.NextBool();
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.Boulder].Value;
			Rectangle frame = tex.Frame();
			Color brown = new Color(160, 110, 60);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, brown, Projectile.rotation, frame.Size() * 0.5f, 0.55f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>十字劈：身前 X 形剑气，随后前飞 64 格沿途伤害。</summary>
	public class CrossChopArcProj : HenshinMoveProj
	{
		private const int Lifetime = 20;
		private Vector2 _dir;
		private bool _spawnedDash;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 90;
			Projectile.height = 90;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

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
				_dir = Main.MouseWorld - owner.MountedCenter;
				if (_dir == Vector2.Zero)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}

			Projectile.Center = owner.MountedCenter + _dir * 40f;
			float t = 1f - Projectile.timeLeft / (float)Lifetime;

			// X 形十字：两条对角线
			float arm = 42f + t * 20f;
			for (int s = -1; s <= 1; s += 2)
			{
				Vector2 a = _dir.RotatedBy(MathHelper.PiOver4 * s);
				for (float u = -1f; u <= 1f; u += 0.1f)
				{
					Vector2 pos = Projectile.Center + a * (u * arm);
					Dust d = Dust.NewDustPerfect(pos, DustID.Blood, a * s * 1.5f, 50, new Color(255, 210, 210), 1.45f);
					d.noGravity = true;
				}
			}

			if (!_spawnedDash && Projectile.timeLeft <= Lifetime - 8 && Projectile.owner == Main.myPlayer)
			{
				_spawnedDash = true;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, _dir * 22f,
					ModContent.ProjectileType<CrossChopDashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>十字劈前冲：飞行约 64 格，沿途伤害。</summary>
	public class CrossChopDashProj : HenshinMoveProj
	{
		private const float MaxDist = 64f * 16f;
		private float _traveled;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			_traveled += Projectile.velocity.Length();
			if (_traveled >= MaxDist)
			{
				Projectile.Kill();
				return;
			}

			Vector2 dir = Projectile.velocity;
			if (dir != Vector2.Zero)
				dir.Normalize();
			for (int s = -1; s <= 1; s += 2)
			{
				Vector2 a = dir.RotatedBy(MathHelper.PiOver4 * s);
				Dust.NewDustPerfect(Projectile.Center + a * 12f, DustID.Blood, -dir * 2f, 60, new Color(255, 200, 200), 1.3f).noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>贴地旋风：单团深蓝风旋（尘+单帧台风贴图，避免整表三连）。</summary>
	public class GroundCycloneProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 8;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void OnSpawn(IEntitySource source)
		{
			if (Projectile.velocity.LengthSquared() < 0.01f)
				Projectile.velocity = new Vector2(Main.player[Projectile.owner].direction * 10f, 0f);
			else
			{
				float dir = System.Math.Sign(Projectile.velocity.X);
				if (dir == 0) dir = Main.player[Projectile.owner].direction;
				Projectile.velocity = new Vector2(dir * 11f, 0f);
			}
		}

		public override void AI()
		{
			int frames = System.Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= 4)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % frames;
			}

			Projectile.rotation += 0.28f;
			int tileX = (int)(Projectile.Center.X / 16f);
			int startY = (int)(Projectile.Center.Y / 16f);
			bool found = false;
			for (int y = startY; y < startY + 40 && y < Main.maxTilesY; y++)
			{
				Tile tile = Framing.GetTileSafely(tileX, y);
				if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				{
					// 贴地：碰撞盒底贴物块顶，与绘制中心对齐
					Projectile.position.Y = y * 16f - Projectile.height;
					found = true;
					break;
				}
			}
			if (!found)
				Projectile.velocity.Y = 6f;
			else
				Projectile.velocity.Y = 0f;

			for (int i = 0; i < 3; i++)
			{
				Vector2 off = Main.rand.NextVector2CircularEdge(18f, 18f);
				Dust d = Dust.NewDustPerfect(Projectile.Center + off, DustID.Cloud, off.RotatedBy(0.6f) * 0.08f, 100, new Color(60, 90, 200), 1.25f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.Typhoon].Value;
			int frames = System.Math.Max(1, Main.projFrames[ProjectileID.Typhoon]);
			Rectangle frame = tex.Frame(1, frames, 0, Projectile.frame);
			// 同步碰撞盒到单帧尺寸，避免贴图与 hitbox 错位
			if (frame.Width > 8 && frame.Height > 8)
			{
				Vector2 c = Projectile.Center;
				Projectile.width = frame.Width;
				Projectile.height = frame.Height;
				Projectile.Center = c;
			}
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(70, 100, 220), Projectile.rotation, frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>啄：约 16 格尖角前向范围伤。</summary>
	public class PeckConeProj : HenshinMoveProj
	{
		private const float Length = 16f * 16f; // 16 tiles
		private Vector2 _dir;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 16;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 16;
		}

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
				_dir = Main.MouseWorld - owner.MountedCenter;
				if (_dir == Vector2.Zero)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}
			Projectile.Center = owner.MountedCenter + _dir * (Length * 0.45f);

			for (float s = 0.05f; s <= 1f; s += 0.08f)
			{
				float width = (1f - s) * 28f;
				Vector2 along = owner.MountedCenter + _dir * (Length * s);
				Vector2 perp = _dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-width, width);
				Dust d = Dust.NewDustPerfect(along + perp, DustID.Cloud, _dir * 3f, 80, default, 1.2f);
				d.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 tip = owner.MountedCenter + _dir * Length;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), owner.MountedCenter, tip, 36f, ref point);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>意念头锤：鼠标处幽灵锤下砸范围伤（放大）。</summary>
	public class ZenHammerSmashProj : HenshinMoveProj
	{
		private float _raise;
		private bool _smashed;
		private const float DrawScale = 2.15f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PaladinsHammerFriendly;

		public override void SetDefaults()
		{
			Projectile.width = 120;
			Projectile.height = 120;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 36;
		}

		public override void AI()
		{
			float t = 1f - Projectile.timeLeft / 36f;
			if (t < 0.35f)
			{
				_raise = MathHelper.Lerp(64f, -28f, t / 0.35f);
				Projectile.rotation = -0.8f;
			}
			else
			{
				float smashT = (t - 0.35f) / 0.65f;
				_raise = MathHelper.Lerp(-28f, 12f, MathHelper.Clamp(smashT * 2f, 0f, 1f));
				Projectile.rotation = MathHelper.Lerp(-0.8f, 0.4f, MathHelper.Clamp(smashT * 2f, 0f, 1f));
				if (!_smashed && smashT > 0.25f)
				{
					_smashed = true;
					SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
					if (Projectile.owner == Main.myPlayer)
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 5f, 8f, 14, 1000f, "ZenHammer"));
					for (int i = 0; i < 36; i++)
					{
						Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, Main.rand.NextVector2Circular(9f, 6f), 80, new Color(200, 180, 255), 1.6f);
						d.noGravity = true;
						Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(7f, 4f), 100, default, 1.35f).noGravity = true;
					}
				}
			}

			Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, _raise), DustID.PurpleTorch, Vector2.Zero, 120, default, 1.25f).noGravity = true;
		}

		public override bool? CanDamage() => _smashed;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[ProjectileID.PaladinsHammerFriendly].Value;
			Rectangle frame = tex.Frame();
			Vector2 pos = Projectile.Center + new Vector2(0f, _raise) - Main.screenPosition;
			Color ghost = new Color(180, 160, 255, 200);
			Main.EntitySpriteDraw(tex, pos, frame, ghost, Projectile.rotation, frame.Size() * 0.5f, DrawScale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>精神强念：鼠标处散落 32 发；0.5s 后开始追踪。</summary>
	public class ResonanceScatterProj : HenshinMoveProj
	{
		private int _fired;
		private int _shotDamage;

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

		public override void OnSpawn(IEntitySource source)
		{
			_shotDamage = System.Math.Max(1, Projectile.damage);
		}

		public override void AI()
		{
			if (Projectile.owner != Main.myPlayer)
				return;

			const int total = 32;
			int perTick = 2;
			if (_fired < total)
			{
				int budget = System.Math.Min(perTick, total - _fired);
				for (int i = 0; i < budget; i++)
				{
					Vector2 spawn = Projectile.Center + Main.rand.NextVector2Circular(56f, 56f);
					Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
						ModContent.ProjectileType<BorrowedVisualBoltProj>(), _shotDamage, Projectile.knockBack, Projectile.owner,
						ProjectileID.RainbowRodBullet, 0f, 1f); // ai2=1 → 共鸣：延迟追踪
					if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.EasyCrit = EasyCrit;
						tagged.IgnoreDefensePartial = IgnoreDefensePartial;
					}
					_fired++;
				}
			}
			if (_fired >= total)
				Projectile.Kill();
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}
}
