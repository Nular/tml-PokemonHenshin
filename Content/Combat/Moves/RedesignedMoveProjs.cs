using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
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

		public override void OnSpawn(IEntitySource source) => EnsureDirector();

		private bool _directorReady;

		private void EnsureDirector()
		{
			if (_directorReady)
				return;
			_directorReady = true;

			_total = (int)System.Math.Max(1, Projectile.ai[1]);
			_speed = Projectile.ai[2] > 0f ? Projectile.ai[2] : 16f;
			Player owner = Main.player[Projectile.owner];
			_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
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

		private bool _castSfx;
		private bool _ultSfx;

		// 本弹既是卡咪龟技能（16 发）又是杰尼龟大招根弹（64 发）：claim 掉通用 UseSound，大招时自己补共享 UltCast。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			EnsureDirector();
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

			// 施放音（各端 AI 首次运行）；大招根弹旗可能晚一包到，所以每 tick 看一次、只播一次。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(scatter ? SfxKey.PsyCast : bubble ? SfxKey.WaterCast : SfxKey.GrassCast, owner.MountedCenter);
			}
			if (!_ultSfx && Projectile.TryGetGlobalProjectile(out PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile ultGp) && ultGp.UltRootCast)
			{
				_ultSfx = true;
				HenshinSfx.Play(SfxKey.UltCast, owner.MountedCenter);
			}
			// 连射节奏：每 6 tick 一声轻水声（不每发都响）。
			int fireAge = 90 - Projectile.timeLeft;
			if (bubble && fireAge > 0 && fireAge < duration && fireAge % 6 == 0)
				HenshinSfx.PlayCooldown(SfxKey.WaterCast, owner.MountedCenter, 6, 0.45f, 0.25f);
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
						spawn = HenshinProjUtil.OwnerMouseWorld(Projectile) + Main.rand.NextVector2Circular(48f, 48f);
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

					if (id >= 0)
					{
						PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
						if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						{
							tagged.EasyCrit = EasyCrit;
							tagged.IgnoreDefensePartial = IgnoreDefensePartial;
						}
					}
					_fired++;
				}
			}

			if (Projectile.owner == Main.myPlayer && _fired >= _total)
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
		private bool _visualsReady;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 135;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
		}

		public override void OnSpawn(IEntitySource source) => EnsureVisuals();

		/// <summary>
		/// OnSpawn 只在 NewProjectile 那一端调用；旁观端必须从已同步的 ai0 再解析贴图。
		/// 未初始化时 PreDraw 会把 tex=0 画成种子，泡沫光线在别人屏幕上就会长成种子机关枪。
		/// </summary>
		private void EnsureVisuals()
		{
			if (_visualsReady)
				return;
			_visualsReady = true;

			_texType = (int)Projectile.ai[0];
			if (_texType <= 0)
				_texType = ProjectileID.Seed;
			// 精神强念等：强制可见彩虹杖贴图 + 延迟追踪标记（ai2）
			if (Projectile.ai[2] > 0.5f || _texType == ProjectileBorrow.ItemShoot(ItemID.PrincessWeapon))
			{
				_texType = ProjectileID.RainbowRodBullet;
				_tint = new Color(230, 140, 255);
				InherentHoming = true;
			}
			// 泡泡：始终用 ProjectileID.Bubble，并立刻 Load（勿等玩家先用泡泡枪）
			if (_texType == ProjectileID.Bubble || _texType == ProjectileBorrow.ItemShoot(ItemID.BubbleGun))
			{
				_texType = ProjectileID.Bubble;
				ProjectileBorrow.SafeLoadProjectile(ProjectileID.Bubble);
				Projectile.scale = 0.85f + (Projectile.identity % 9) * 0.05f;
			}
			else
				ProjectileBorrow.SafeLoadProjectile(_texType);

			EasyCrit = Projectile.ai[1] > 0f;
			Projectile.penetrate = _texType == ProjectileID.RainbowRodBullet ? 3 : 1;
			if (_texType == ProjectileID.Typhoon)
				_tint = new Color(255, 120, 40);
			if (_texType == ProjectileID.RainbowRodBullet)
			{
				Projectile.tileCollide = false;
				Projectile.scale = 1.35f;
				Projectile.timeLeft = 270;
			}
			_baseVel = Projectile.velocity;
		}

		public void SetTint(Color c) => _tint = c;

		public override void AI()
		{
			EnsureVisuals();
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
				// 精神强念彩虹弹改画拖尾，不再每 tick 撒粉尘。
				if (_texType != ProjectileID.RainbowRodBullet && Main.rand.NextBool(2))
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
			// 多发限频：64 发泡泡同端 4 tick 内只响一声（OnKill 各端都跑）。
			HenshinSfx.PlayCooldown(SfxKey.WaterHit, Projectile.Center, 4, 0.8f, 0.2f);
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
			if (_texType == ProjectileID.Seed)
				HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Seed);
			return true;
		}

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 10, 1); // extraUpdates=1；只有彩虹弹画条带

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			PopBubbles();
			bool rainbow = _texType == ProjectileID.RainbowRodBullet;
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Light);
			HenshinHitFeel.OnHit(Projectile, target, tier, rainbow ? new Color(200, 120, 255) : new Color(110, 190, 255));
			// 精神强念：命中光圈（同步特效弹，命中音在它 AI 首帧播，ai0=1 表示大招档）。
			if (rainbow && Projectile.owner == Main.myPlayer)
			{
				int ring = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<PsychicHitRingProj>(), 0, 0f, Projectile.owner, tier == HitTier.Ultimate ? 1f : 0f);
				if (ring >= 0 && ring < Main.maxProjectiles)
					Main.projectile[ring].Center = target.Center;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			EnsureVisuals();
			int drawId = _texType > 0 ? _texType : ProjectileID.Seed;
			if (drawId == ProjectileBorrow.ItemShoot(ItemID.PrincessWeapon) || drawId <= 0)
				drawId = ProjectileID.RainbowRodBullet;
			if (drawId == ProjectileID.RainbowRodBullet)
				HenshinTrail.Strip(Projectile, new Color(190, 90, 255), 13f, new Color(255, 200, 255), 0.36f, 0f, 0.8f);
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

		public override void OnSpawn(IEntitySource source) => EnsurePopVisuals();

		private bool _popReady;

		private void EnsurePopVisuals()
		{
			if (_popReady)
				return;
			_popReady = true;
			Main.instance.LoadProjectile(ProjectileID.Bubble);
			float life = Projectile.ai[1] > 1f ? Projectile.ai[1] : 14f;
			Projectile.timeLeft = (int)life;
			Projectile.scale = Projectile.ai[0] > 0.05f ? Projectile.ai[0] : 0.4f;
		}

		public override void AI()
		{
			EnsurePopVisuals();
			Projectile.velocity *= 0.92f;
			Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / (float)System.Math.Max(1, Projectile.ai[1] > 1f ? Projectile.ai[1] : 14f)));
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			EnsurePopVisuals();
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
		private Vector2 _wallNormal = -Vector2.UnitY;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 360;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
			InherentHoming = true;
		}

		// 施放音：大招根弹，共享 UltCast 自动播（不 claim）；这里只接火焰长拖尾。extraUpdates=1。
		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 16, 1);

		private static readonly Color FlareTint = new(255, 110, 30);

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
				if (!Main.dedServ && Projectile.timeLeft % 12 == 0)
				{
					Vector2 contact = Projectile.Center - _wallNormal * (Projectile.width * 0.5f);
					for (int i = 0; i < 2; i++)
					{
						Vector2 v = _wallNormal.RotatedBy(Main.rand.NextFloat(-1.2f, 1.2f)) * Main.rand.NextFloat(1.5f, 3.5f);
						Dust.NewDustPerfect(contact, DustID.Torch, v, 60, default, 1.3f).noGravity = true;
					}
				}
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

			Lighting.AddLight(Projectile.Center, 1f, 0.4f, 0.1f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (!_halted)
			{
				_wallNormal = HenshinTileImpact.NormalFromCollide(Projectile.velocity, oldVelocity);
				HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Fire, 1.2f);
			}
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
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, FlareTint);
			TackleHitFxProj.Spawn(Projectile, Vector2.Lerp(target.Center, Projectile.Center, 0.35f), DustID.Torch, SfxKey.FireHit, tier);
		}

		public override void OnKill(int timeLeft)
		{
			// 火焰漩涡收场：大号火爆（各端 OnKill 都跑），给大招一个结尾。
			HenshinSfx.PlayCooldown(SfxKey.BlastM, Projectile.Center, 8);
			HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, FlareTint, 120f);
		}

		private static ReLogic.Content.Asset<Texture2D> _sheet;
		private static ReLogic.Content.Asset<Texture2D> _light;

		public override bool PreDraw(ref Color lightColor)
		{
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/FireSpin/FireSpin", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			_light ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/FireSpin/FireSpin_Light", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			const int Frames = 4;
			int frameH = tex.Height / Frames;
			int frame = (Projectile.timeLeft / 3) % Frames;
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			Rectangle lightSrc = new(0, frameH * frame, _light.Value.Width, frameH);
			Vector2 pos = (Projectile.Center - Main.screenPosition).Floor();
			Vector2 origin = new(tex.Width * 0.5f, frameH * 0.5f);
			// 拖尾：橙外晕 + 暖白芯，比火花粗一档（大招）；停在墙上时 oldPos 重合不画。
			if (!_halted)
				HenshinTrail.Strip(Projectile, FlareTint, 24f, new Color(255, 225, 150), 0.34f, 0f, 0.9f);
			Main.EntitySpriteDraw(tex, pos, src, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
			HenshinFxDraw.BeginAdditive();
			Main.EntitySpriteDraw(_light.Value, pos, lightSrc, Color.White * 0.5f, 0f, origin, 1f, SpriteEffects.None, 0f);
			HenshinFxDraw.EndAdditive();
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
			Projectile.timeLeft = 90;
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

		private static readonly Color WaterTint = new(90, 180, 255);
		private bool _castSfx;

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 9, 2); // extraUpdates=2
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void AI()
		{
			if (_texType <= 0)
				_texType = ProjectileID.WaterStream;
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.WaterCast, Projectile.Center);
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			// 水弹本体是可见贴图 + 淡蓝拖尾；粒子纪律：飞行中不撒水滴。
			Lighting.AddLight(Projectile.Center, 0.06f, 0.16f, 0.3f);
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft > 0)
				HenshinExplosion.SpawnLight(Projectile.Center, WaterTint, BlastSize.S, 0.6f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Water);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Wet, 180);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), WaterTint);
			TackleHitFxProj.Spawn(Projectile, Vector2.Lerp(target.Center, Projectile.Center, 0.4f), DustID.Water, SfxKey.WaterHit, HitTier.Light);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int drawId = _texType > 0 ? _texType : ProjectileID.WaterStream;
			Texture2D tex = TextureAssets.Projectile[drawId].Value;
			if (tex.Width <= 1)
				tex = TextureAssets.Projectile[ProjectileID.WaterBolt].Value;
			HenshinTrail.Strip(Projectile, WaterTint, 12f, new Color(215, 240, 255), 0.4f, 0f, 0.75f);
			Rectangle frame = tex.Frame();
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, new Color(120, 200, 255), Projectile.rotation, frame.Size() * 0.5f, 1.2f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>草系鞭（藤鞭）：真·鞭子。蓄(0-8)向后卷起 → 甩(8-16)加速挥向鼠标 → 抽(16-28)鞭梢滞后 5 tick 才到最远点并「啪」(tick 21，Item153 + 闪光) → 收(28-40)。13 个控制点，每点读延迟的挥角，自然形成行进的波形。方向锁一次：拥有者写 ai1=弧度+10（NetOp.SyncAim 外再经 netUpdate 同步，360°）。挥动期间不撒粒子。</summary>
	public class GrassWhipProj : HenshinMoveProj
	{
		private const float Range = 176f; // 同旧版，~11 格
		private const int Lifetime = 40;
		private const int CrackTick = 21;
		private const int Segs = 12;
		private const float TipLag = 5f;
		private const float AimBias = 10f;
		private static ReLogic.Content.Asset<Texture2D> _vine;
		private static ReLogic.Content.Asset<Texture2D> _crack;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime + 30; // 手动按 localAI0 结束，等待同步期间不倒计时消失
			Projectile.tileCollide = false;
			Projectile.ownerHitCheck = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		// 控制点缓存：AI 每 tick 算一次（含上一 tick 的点用于扫掠判定），Colliding/PreDraw 同 tick 复用。
		private Vector2[] _pts;
		private Vector2[] _prevPts;
		private int _ptsAge = -1;

		private Vector2[] Pts(Player owner, int age)
		{
			if (_pts == null || _ptsAge != age)
			{
				_prevPts = _ptsAge == age - 1 ? _pts : null;
				_pts = ControlPoints(owner, age);
				_ptsAge = age;
			}
			return _pts;
		}

		private bool Aimed => Projectile.ai[1] > 0.5f;
		private float Angle => Projectile.ai[1] - AimBias;
		private int Age => (int)Projectile.localAI[0];

		// 后摆角 φ(τ)：0→Φ 蓄，Φ→0 甩（先慢后快），之后 0。
		private static float Phi(float tau)
		{
			const float Max = 2.0f;
			if (tau <= 0f)
				return 0f;
			if (tau < 8f)
				return Max * (1f - (1f - tau / 8f) * (1f - tau / 8f));
			if (tau < 16f)
			{
				float u = (tau - 8f) / 8f;
				return Max * (1f - u * u);
			}
			return 0f;
		}

		private static float LenFactor(float tau)
		{
			if (tau < 8f)
				return 0.55f;
			if (tau < 16f)
				return 0.55f + 0.45f * (tau - 8f) / 8f;
			if (tau < 28f)
				return 1f;
			return 1f - 0.7f * (tau - 28f) / 12f;
		}

		/// <summary>13 个控制点（0=手，12=鞭梢）。每点读延迟 s*TipLag tick 的挥角，收鞭时叠一点松弛波。</summary>
		private Vector2[] ControlPoints(Player owner, float tau)
		{
			Vector2[] pts = new Vector2[Segs + 1];
			float baseAng = Angle;
			float side = System.Math.Cos(baseAng) >= 0f ? -1f : 1f;
			float segLen = Range / Segs * LenFactor(tau);
			float slack = tau > 28f ? (tau - 28f) / 12f : 0f;
			pts[0] = owner.MountedCenter;
			for (int i = 1; i <= Segs; i++)
			{
				float s = (i - 0.5f) / Segs;
				float a = baseAng + side * Phi(tau - s * TipLag) + side * 0.45f * slack * (float)System.Math.Sin(s * 6.2832f + tau * 0.4f);
				pts[i] = pts[i - 1] + new Vector2((float)System.Math.Cos(a), (float)System.Math.Sin(a)) * segLen;
			}
			return pts;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			// 360° 朝鼠标：仅拥有者求一次，弧度写 ai1 并 netUpdate；其它端等同步后才开始计时。
			if (!Aimed)
			{
				if (Projectile.owner != Main.myPlayer)
					return;
				Vector2 aim = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
				Projectile.ai[1] = aim.ToRotation() + AimBias;
				Projectile.netUpdate = true;
				if (System.Math.Abs(aim.X) > 0.05f)
					owner.ChangeDir(aim.X > 0f ? 1 : -1);
			}

			Projectile.localAI[0] += 1f;
			int age = Age;
			if (age >= Lifetime)
			{
				Projectile.Kill();
				return;
			}
			if (age == 1)
				HenshinSfx.Play(SfxKey.Swing, owner.MountedCenter, 0.8f, -0.15f); // 蓄鞭起手；本弹已 claim，之力不再响 Item1
			Vector2[] pts = Pts(owner, age);
			Projectile.Center = pts[Segs];
			if (age == CrackTick && !Main.dedServ)
			{
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item153, pts[Segs]);
			}
			if (age >= CrackTick - 2 && age <= CrackTick + 6)
				Lighting.AddLight(pts[Segs], 0.35f, 0.8f, 0.3f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Aimed)
				return false;
			int age = Age;
			if (age < 10 || age > 32)
				return false;
			Player owner = Main.player[Projectile.owner];
			Vector2[] pts = Pts(owner, age);
			Vector2[] prev = _prevPts;
			float point = 0f;
			for (int i = 1; i <= Segs; i++)
			{
				float w = i >= Segs - 2 ? 28f : 20f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), pts[i - 1], pts[i], w, ref point))
					return true;
				// 扫掠：同序号控制点上一 tick → 本 tick 的轨迹，防止快速甩动时整段穿过窄敌人
				if (prev != null && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), prev[i], pts[i], w, ref point))
					return true;
			}
			return false;
		}

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 2 颗草屑 → 同步命中星（草绿，带 GrassHit 命中音，各端都响）。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(120, 230, 90));
			TackleHitFxProj.Spawn(Projectile, Vector2.Clamp(Projectile.Center, target.position, target.BottomRight), DustID.Grass, SfxKey.GrassHit, HitTier.Light);
		}

		// VineWhip.png：4 帧 24×20（0 粗藤段、1 带叶藤段、2 细藤段、3 鞭梢叶苞），朝右，藤中心 (12,10)。
		// VineWhipCrack.png：4 帧 32×32，「啪」闪光→叶片迸散，居中。
		public override bool PreDraw(ref Color lightColor)
		{
			if (!Aimed)
				return false;
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;
			int age = Age;
			_vine ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/VineWhip/VineWhip", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _vine.Value;
			int fh = tex.Height / 4;
			Vector2[] pts = Pts(owner, age);
			float opacity = System.Math.Min(1f, (Lifetime - age) / 6f);
			Color c = new(System.Math.Max(lightColor.R, (byte)110), System.Math.Max(lightColor.G, (byte)120), System.Math.Max(lightColor.B, (byte)100));
			c *= opacity;
			Vector2 origin = new(12f, 10f);
			for (int i = 1; i <= Segs; i++)
			{
				Vector2 d = pts[i] - pts[i - 1];
				float len = d.Length();
				if (len < 0.5f)
					continue;
				float rot = d.ToRotation();
				float taper = 1f - 0.45f * (i / (float)Segs); // 根粗梢细
				int frame = i == Segs ? 3 : (i >= Segs - 3 ? 2 : (i % 3 == 2 ? 1 : 0));
				SpriteEffects fx = System.Math.Cos(rot) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
				Vector2 mid = (pts[i] + pts[i - 1]) * 0.5f;
				Vector2 scale = new(System.Math.Max(0.35f, len * 1.2f / 24f), taper);
				if (i == Segs)
				{
					mid = pts[i];
					scale = new Vector2(0.9f, 0.9f);
				}
				Main.EntitySpriteDraw(tex, (mid - Main.screenPosition).Floor(), new Rectangle(0, fh * frame, tex.Width, fh), c, rot,
					i == Segs ? new Vector2(10f, 10f) : origin, scale, fx, 0f);
			}

			int cf = (age - CrackTick) / 3;
			if (cf >= 0 && cf < 4)
			{
				_crack ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/VineWhip/VineWhipCrack", ReLogic.Content.AssetRequestMode.ImmediateLoad);
				Texture2D ct = _crack.Value;
				int ch = ct.Height / 4;
				float grow = cf == 0 ? 1.15f : 1f;
				Main.EntitySpriteDraw(ct, (pts[Segs] - Main.screenPosition).Floor(), new Rectangle(0, ch * cf, ct.Width, ch), Color.White,
					0f, new Vector2(ct.Width * 0.5f, ch * 0.5f), grow, SpriteEffects.None, 0f);
			}
			return false;
		}
	}
	/// <summary>电光一闪：闪现至指针最近敌人；身周 Kenney 电火花；伤害在落点爆发上打三下。</summary>
	public class BlinkStrikeProj : HenshinMoveProj
	{
		private const float SearchRange = 960f;
		private const int AuraLife = 28;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = AuraLife;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
		}

		private bool _castSfx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}

			// 施放音各端都播（原 Item8 只在拥有者端）：电系起手 + 一声快挥。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.ElecCast, p.Center, 0.8f, 0.2f);
				HenshinSfx.Play(SfxKey.Swing, p.Center, 0.7f, 0.3f);
			}

			if (Projectile.localAI[0] == 0f && Projectile.owner == Main.myPlayer)
			{
				Projectile.localAI[0] = 1f;
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

				Vector2 from = p.Center;
				if (target != null)
				{
					Vector2 dest = target.Center - new Vector2(p.direction * 28f, 0f);
					p.Teleport(dest, -1);
					p.immune = true;
					p.immuneTime = System.Math.Max(p.immuneTime, 15);
					for (int i = 0; i < 2; i++)
					{
						Dust.NewDustPerfect(from, DustID.YellowTorch, Main.rand.NextVector2Circular(4f, 4f), 80, HenshinFxDraw.SparkGold, 1.1f).noGravity = true;
						Dust.NewDustPerfect(p.Center, DustID.YellowTorch, Main.rand.NextVector2Circular(4f, 4f), 80, HenshinFxDraw.SparkGold, 1.1f).noGravity = true;
					}
					SpawnGhostTrail(Projectile, p, from);
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
						ModContent.ProjectileType<BlinkStrikeBurstProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
						target.whoAmI);
					if (id >= 0)
					{
						Main.projectile[id].Center = target.Center;
						Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
						PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					}
				}
				else
				{
					// 无目标：短距闪到指针，无伤害
					Vector2 dest = cursor;
					Vector2 delta = dest - p.Center;
					if (delta.Length() > SearchRange)
						dest = p.Center + Vector2.Normalize(delta) * SearchRange;
					p.Teleport(dest, -1);
					SpawnGhostTrail(Projectile, p, from);
				}
			}

			Projectile.Center = p.Center;
		}

		private static void SpawnGhostTrail(Projectile parent, Player p, Vector2 from)
		{
			Vector2 delta = p.Center - from;
			if (delta.LengthSquared() < 4f)
				delta = new Vector2(p.direction * 48f, 0f);

			float frame = 0f;
			float state = 0f;
			HenshinPlayer hp = p.GetModPlayer<HenshinPlayer>();
			if (hp.CurrentForm?.Locomotion != null)
			{
				state = (float)hp.LocomotionState;
				frame = hp.LocomotionFrame;
			}

			int id = Projectile.NewProjectile(parent.GetSource_FromThis(), from, delta,
				ModContent.ProjectileType<BlinkStrikeGhostProj>(), 0, 0f, parent.owner, frame, state);
			if (id >= 0)
				Main.projectile[id].Center = from;
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
				return false;
			int age = AuraLife - Projectile.timeLeft;
			float fade = Projectile.timeLeft < 6 ? Projectile.timeLeft / 6f : 1f;
			BlinkStrikeBurstProj.DrawBodySparks(p.Center, age, fade, pulse: false);
			return false;
		}
	}

	/// <summary>电光一闪残影：沿闪现路径铺形态贴图，不造成伤害。ai0=帧，ai1=移动状态。</summary>
	public class BlinkStrikeGhostProj : HenshinMoveProj
	{
		public const int Life = 48;
		public const int GhostCount = 8;

		private Vector2 _from;
		private Vector2 _to;
		private float _envelope = 1f;
		private bool _inited;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			if (!_inited)
			{
				_inited = true;
				_from = Projectile.Center;
				Vector2 delta = Projectile.velocity;
				if (delta.LengthSquared() < 4f)
					delta = new Vector2(32f, 0f);
				_to = _from + delta;
			}

			float lifeT = 1f - Projectile.timeLeft / (float)Life;
			_envelope = lifeT < 0.12f ? 1f : 1f - System.MathF.Pow((lifeT - 0.12f) / 0.88f, 2f);
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_inited || _envelope < 0.02f)
				return false;

			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;

			HenshinPlayer hp = owner.GetModPlayer<HenshinPlayer>();
			FormDefinition form = hp.CurrentForm;
			if (form == null)
				return false;

			Texture2D tex;
			Rectangle? frame = null;
			float scale = 1f;
			bool facesLeft = form.TextureFacesLeft;
			Vector2 origin;

			FormLocomotionSpec loco = form.Locomotion;
			FormAnimClip clip = loco?.GetClip((FormLocomotionState)(int)Projectile.ai[1]);
			if (clip != null && !string.IsNullOrEmpty(clip.TexturePath) && clip.FrameCount > 0)
			{
				tex = ModContent.Request<Texture2D>(clip.TexturePath).Value;
				int f = (int)Projectile.ai[0];
				if (f < 0 || f >= clip.FrameCount)
					f = 0;
				frame = new Rectangle(f * clip.FrameWidth, 0, clip.FrameWidth, clip.FrameHeight);
				scale = FormLocomotionSpec.TargetDrawHeight / System.Math.Max(1, clip.FrameHeight);
				facesLeft = clip.FacesLeft;
				origin = new Vector2(clip.FrameWidth * 0.5f, clip.FrameHeight);
			}
			else
			{
				tex = ModContent.Request<Texture2D>(form.TexturePath).Value;
				origin = new Vector2(tex.Width * 0.5f, tex.Height);
			}

			if (tex == null || tex.Width < 2)
				return false;

			Vector2 travel = _to - _from;
			bool dashRight = travel.X >= 0f;
			SpriteEffects fx = dashRight == facesLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float footDrop = owner.height * 0.5f;

			for (int i = 0; i < GhostCount; i++)
			{
				float t = (i + 0.5f) / GhostCount;
				Vector2 pos = Vector2.Lerp(_from, _to, t) + new Vector2(0f, footDrop);
				float alpha = (0.14f + 0.28f * t) * _envelope;
				if (alpha < 0.02f)
					continue;
				Color c = HenshinFxDraw.SparkGold * alpha;
				c.A = (byte)MathHelper.Clamp(alpha * 180f, 10f, 170f);
				Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, c, 0f, origin, scale, fx, 0);
			}

			return false;
		}
	}

	/// <summary>电光一闪落点：贴住目标，同一单位最多三下；身周 Kenney spark。</summary>
	public class BlinkStrikeBurstProj : HenshinMoveProj
	{
		public const int HitGap = 8;
		public const int HitCount = 3;
		public const int Life = 28;

		private byte[] _hitCount;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = HitGap;
		}

		public override void AI()
		{
			int who = (int)Projectile.ai[0];
			if (who >= 0 && who < Main.maxNPCs)
			{
				NPC n = Main.npc[who];
				if (n.active)
					Projectile.Center = n.Center;
			}

			int age = Life - Projectile.timeLeft;
			// 三段电击节拍（AI 各端都跑）：电系命中 + 轻命中，每段一个短光脉冲；随机火花尘去掉（BlinkSpark 贴图已够）。
			if (age % HitGap == 0 && age <= HitGap * (HitCount - 1))
			{
				HenshinSfx.Play(SfxKey.ElecHit, Projectile.Center, 1f, 0.1f * (age / HitGap));
				HenshinSfx.Play(SfxKey.HitLight, Projectile.Center, 0.8f);
				HenshinExplosion.SpawnLight(Projectile.Center, HenshinFxDraw.SparkGold, BlastSize.S);
			}
			Lighting.AddLight(Projectile.Center, HenshinFxDraw.SparkGold.ToVector3() * 0.85f);
		}

		public override bool? CanHitNPC(NPC target)
		{
			if (target == null)
				return false;
			_hitCount ??= new byte[Main.maxNPCs];
			int who = target.whoAmI;
			if (who >= 0 && who < _hitCount.Length && _hitCount[who] >= HitCount)
				return false;
			return null;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			_hitCount ??= new byte[Main.maxNPCs];
			int who = target.whoAmI;
			if (who >= 0 && who < _hitCount.Length && _hitCount[who] < byte.MaxValue)
				_hitCount[who]++;
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), HenshinFxDraw.SparkGold, target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			bool pulse = age % HitGap <= 2 && age <= HitGap * (HitCount - 1) + 2;
			float fade = Projectile.timeLeft < 6 ? Projectile.timeLeft / 6f : 1f;
			DrawBodySparks(Projectile.Center, age, fade, pulse);
			return false;
		}

		private static ReLogic.Content.Asset<Texture2D> _sparkSheet;

		/// <summary>自绘 BlinkSpark（Assets/Fx/BlinkSpark，4 帧 26×26/帧，中心 (13,13)）绕身体一圈；自发光，不吃环境光。</summary>
		public static void DrawBodySparks(Vector2 worldCenter, int age, float fade, bool pulse)
		{
			fade = MathHelper.Clamp(fade, 0f, 1f);
			if (fade <= 0.02f)
				return;
			_sparkSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/BlinkSpark/BlinkSpark", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D sheet = _sparkSheet.Value;
			int sparkH = sheet.Height / 4;
			const int count = 4;
			float spin = age * 0.32f;
			float boost = pulse ? 1.22f : 1f;
			for (int i = 0; i < count; i++)
			{
				float ang = spin + i * (MathHelper.TwoPi / count);
				float radius = (30f + 8f * System.MathF.Sin(age * 0.5f + i)) * (pulse ? 1.08f : 1f);
				Vector2 pos = worldCenter + ang.ToRotationVector2() * radius;
				int frame = (age / 2 + i) % 4;
				float scale = (1.5f + 0.4f * (i % 2)) * boost;
				Color c = Color.White * ((pulse ? 1f : 0.85f) * fade);
				Main.EntitySpriteDraw(sheet, (pos - Main.screenPosition).Floor(), new Rectangle(0, sparkH * frame, sheet.Width, sparkH), c,
					0f, new Vector2(sheet.Width * 0.5f, sparkH * 0.5f), scale, SpriteEffects.None, 0f);
			}
		}
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
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.YellowTorch, -Projectile.velocity * 0.05f, 40, HenshinFxDraw.SparkGold, 1.9f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 1f, 0.9f, 0.4f);

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
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, HenshinFxDraw.SparkGold, Projectile.rotation, tex.Size() * 0.5f, 2.8f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, HenshinFxDraw.SparkGoldCore * 0.55f, Projectile.rotation, tex.Size() * 0.5f, 2.0f, SpriteEffects.None, 0);
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
				// ai0=0 光束；velocity 长度=劈距；ai2=2 不再挂导演、不再分叉
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.MountedCenter, vel,
					ModContent.ProjectileType<SkyBoltLightningProj>(), dmg, 1f, Projectile.owner, 0f, 0f,
					SkyBoltLightningProj.AiNoChainNoFork);
				if (id >= 0)
					Main.projectile[id].Center = p.MountedCenter;
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

		public override void OnSpawn(IEntitySource source) => EnsureTomb();

		private bool _tombReady;

		private void EnsureTomb()
		{
			if (_tombReady)
				return;
			_tombReady = true;
			_center = Projectile.Center;
			_offsets[0] = new Vector2(0f, -56f);
			_offsets[1] = new Vector2(0f, 56f);
			_offsets[2] = new Vector2(-56f, 0f);
			_offsets[3] = new Vector2(56f, 0f);
		}

		private bool _castSfx;
		private bool _blastFx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			EnsureTomb();
			// 施放：岩系起手（各端）。收拢碎裂：四石同一帧合成一次中爆（声 + 光 + 冲击环），替掉 4 次 Item14。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.RockCast, Projectile.Center);
			}
			if (!_blastFx && 40 - Projectile.timeLeft >= 32)
			{
				_blastFx = true;
				HenshinSfx.Play(SfxKey.BlastM, Projectile.Center, 0.8f);
				HenshinSfx.Play(SfxKey.RockHit, Projectile.Center);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, new Color(220, 150, 80), 100f, kenneyFrame: false);
			}
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

		public override void OnSpawn(IEntitySource source) => EnsureShard();

		private bool _offReady;

		private void EnsureShard()
		{
			if (_offReady)
				return;
			_offReady = true;
			_startOff = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			Projectile.scale = 0.5f;
		}

		public override void AI()
		{
			EnsureShard();
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

			if (t > 0.85f && !_shattered)
			{
				_shattered = true;
				// 碎裂声与爆光由 RockTombDirectorProj 统一放一次；每石只留 1 颗碎石。
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(6f, 6f), 80, new Color(140, 100, 60), 1.4f);
				d.noGravity = Main.rand.NextBool();
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(220, 150, 80), target.Center);
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
		private static ReLogic.Content.Asset<Texture2D> _crossSheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		/// <summary>CrossSlash 5 帧 112x112；0..4 帧，原点取帧中心。懒加载，dedServ 不会走到 PreDraw。</summary>
		internal static void DrawCross(Vector2 center, float rot, int frame, float scale, float opacity)
		{
			Texture2D tex = (_crossSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/CrossSlash/CrossSlash", ReLogic.Content.AssetRequestMode.ImmediateLoad)).Value;
			int fh = tex.Height / 5;
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			Main.EntitySpriteDraw(tex, (center - Main.screenPosition).Floor(), src, Color.White * opacity, rot, src.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}

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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir == Vector2.Zero)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}

			Projectile.Center = owner.MountedCenter + _dir * 40f;
			float t = 1f - Projectile.timeLeft / (float)Lifetime;

			// 大招根弹：共享 UltCast 已在施放时响；X 剑气展开那帧（age 3）补一记重斩，前冲出手（age 8）补一记挥空。
			int age = Lifetime - Projectile.timeLeft;
			if (age == 3)
			{
				HenshinSfx.Play(SfxKey.SwingHeavy, Projectile.Center);
				HenshinExplosion.SpawnLight(Projectile.Center, new Color(255, 150, 70), BlastSize.M);
			}
			else if (age == 8)
				HenshinSfx.Play(SfxKey.Swing, Projectile.Center, 1f, -0.2f);

			// X 形剑气由 PreDraw 的 CrossSlash 贴图承担；出招期间不撒粒子。
			Lighting.AddLight(Projectile.Center, 0.9f, 0.4f, 0.2f * (1f - t));

			if (!_spawnedDash && Projectile.timeLeft <= Lifetime - 8 && Projectile.owner == Main.myPlayer)
			{
				_spawnedDash = true;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, _dir * 22f,
					ModContent.ProjectileType<CrossChopDashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => CrossChopDashProj.HitFeel(Projectile, target);

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.localAI[0] == 0f)
				return false;
			int age = Lifetime - Projectile.timeLeft;
			int frame = age < 3 ? 0 : age < 6 ? 1 : age < 12 ? 2 : age < 16 ? 3 : 4;
			float opacity = frame == 4 ? System.Math.Min(1f, Projectile.timeLeft / 3f) : 1f;
			DrawCross(Projectile.Center, _dir.ToRotation(), frame, 1f, opacity);
			return false;
		}
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

			Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.1f);
		}

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 12, 1); // extraUpdates=1

		/// <summary>十字劈两段共用：大招档（TierOf）打击感 + 同步命中星（重命中音，大招档叠 HitUlt）。</summary>
		internal static void HitFeel(Projectile p, NPC target)
		{
			HitTier tier = HenshinHitFeel.TierOf(p, HitTier.Heavy);
			Vector2 at = Vector2.Clamp(p.Center, target.position, target.BottomRight);
			HenshinHitFeel.OnHit(p, target, tier, new Color(255, 150, 70), at);
			TackleHitFxProj.Spawn(p, at, DustID.Torch, SfxKey.HitHeavy, tier);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitFeel(Projectile, target);

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.velocity == Vector2.Zero)
				return false;
			// 前飞：缩小的 X 剑气（帧 2 最亮帧）随行，身后一道橙白剑气拖尾，不撒粒子。
			float fade = System.Math.Min(1f, Projectile.timeLeft / 10f);
			HenshinTrail.Strip(Projectile, new Color(255, 130, 50), 30f, new Color(255, 235, 200), 0.3f, 0f, 0.7f * fade);
			CrossChopArcProj.DrawCross(Projectile.Center, Projectile.velocity.ToRotation(), 2, 0.6f, fade);
			return false;
		}
	}

	/// <summary>
	/// 起风：从角色朝瞄准方向飞出，落地后按旧逻辑水平贴地滚动。
	/// 默认 tileCollide；诅咒符 Bolt 穿墙则不贴地。自管位移，广角镜不弯。
	/// </summary>
	public class GroundCycloneProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;

		private const float FlightSpeed = 11f;
		private const int ImpactCooldown = 20;
		private bool _grounded;
		private int _impactCooldown;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Typhoon;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 8;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Main.instance.LoadProjectile(ProjectileID.Typhoon);
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

			if (_impactCooldown > 0)
				_impactCooldown--;
			if (_grounded && Projectile.tileCollide)
			{
				SnapToGround();
				if (Projectile.timeLeft % 5 == 0 && Projectile.velocity.Y == 0f)
					HenshinTileImpact.GroundDebris(Projectile);
			}

			// 粒子纪律：Typhoon 本体 + 风带拖尾已够显眼，飞行云尘去掉。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.WindCast, Projectile.Center);
			}
			Lighting.AddLight(Projectile.Center, 0.12f, 0.18f, 0.32f);
		}

		private bool _castSfx;

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 10);
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(150, 190, 255));
			TackleHitFxProj.Spawn(Projectile, Vector2.Clamp(Projectile.Center, target.position, target.BottomRight), DustID.Cloud, SfxKey.WindHit, HitTier.Light);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			bool wallTurn = System.Math.Abs(Projectile.velocity.X) < 0.01f && System.Math.Abs(oldVelocity.X) > 0.01f;
			if (!_grounded)
			{
				HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Wind);
				_impactCooldown = ImpactCooldown;
			}
			else if (_impactCooldown <= 0 && (wallTurn || oldVelocity.Y > 3f))
			{
				HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Wind, 0.6f);
				_impactCooldown = ImpactCooldown;
			}
			_grounded = true;
			float sx = System.Math.Abs(Projectile.velocity.X) > 0.01f
				? System.Math.Sign(Projectile.velocity.X)
				: System.Math.Sign(oldVelocity.X);
			if (sx == 0)
				sx = Main.player[Projectile.owner].direction;
			// 撞墙时改向，保持水平贴地滚动
			if (wallTurn)
				sx = -System.Math.Sign(oldVelocity.X);
			Projectile.velocity.X = sx * FlightSpeed;
			Projectile.velocity.Y = 0f;
			return false;
		}

		/// <summary>旧贴地滚动：碰撞盒底贴物块顶。</summary>
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
			if (System.Math.Abs(Projectile.velocity.X) < 0.1f)
				Projectile.velocity.X = Main.player[Projectile.owner].direction * FlightSpeed;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(ProjectileID.Typhoon);
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
			// 风带拖尾：冷白淡蓝，半透明，宽同龙卷一半。
			HenshinTrail.Strip(Projectile, new Color(110, 150, 255), 22f, new Color(225, 240, 255), 0.3f, 0f, 0.5f);
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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir == Vector2.Zero)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
			}
			Projectile.Center = owner.MountedCenter + _dir * (Length * 0.45f);
			// 出招期间不沿线撒粒子；刺尖那颗风尘换成一点白光 + 一声快挥（各端）。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.Swing, owner.MountedCenter, 0.9f, 0.35f);
			}
			if (Projectile.timeLeft >= 8)
				Lighting.AddLight(owner.MountedCenter + _dir * Length, 0.2f, 0.22f, 0.26f);
		}

		private bool _castSfx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player owner = Main.player[Projectile.owner];
			float along = MathHelper.Clamp(Vector2.Dot(target.Center - owner.MountedCenter, _dir), 0f, Length);
			Vector2 at = owner.MountedCenter + _dir * along;
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(255, 240, 215), at);
			TackleHitFxProj.Spawn(Projectile, at, 0, SfxKey.HitLight, HitTier.Light);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 tip = owner.MountedCenter + _dir * Length;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), owner.MountedCenter, tip, 36f, ref point);
		}

		// 贴图 Assets/Fx/PeckBeak/PeckBeak.png：4 帧 × 4 tick，268×40/帧，朝右，刺根在 (6,20)，全长 254 px ≈ 16 格。
		private const int PeckFrames = 4;
		private const int PeckTicksPerFrame = 4;
		private static ReLogic.Content.Asset<Texture2D> _beakSheet;

		public override bool PreDraw(ref Color lightColor)
		{
			if (_dir == Vector2.Zero)
				return false;
			Player owner = Main.player[Projectile.owner];
			if (!owner.active)
				return false;
			_beakSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/PeckBeak/PeckBeak", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _beakSheet.Value;
			int frameH = tex.Height / PeckFrames;
			int age = 16 - Projectile.timeLeft;
			int frame = System.Math.Clamp(age / PeckTicksPerFrame, 0, PeckFrames - 1);
			Rectangle src = new(0, frameH * frame, tex.Width, frameH);
			float rot = _dir.ToRotation();
			// 朝左半圈上下翻转，让受光面（贴图上半）始终朝上。
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Color c = new(System.Math.Max(lightColor.R, (byte)130), System.Math.Max(lightColor.G, (byte)120), System.Math.Max(lightColor.B, (byte)110));
			float opacity = frame == PeckFrames - 1 ? 0.5f + 0.5f * (Projectile.timeLeft / (float)PeckTicksPerFrame) : 1f;
			Main.EntitySpriteDraw(tex, (owner.MountedCenter - Main.screenPosition).Floor(), src, c * opacity, rot, new Vector2(6f, frameH * 0.5f), 1f, fx, 0f);
			return false;
		}
	}

	/// <summary>
	/// 燕返：沿用 LungeProj 的位移/判定/无敌（不改），只换外观：自绘风刃锥拖尾 + 命中只留 3 颗云尘。
	/// 贴图 Assets/Fx/AerialAceDash/AerialAceDash.png：3 帧 × 3 tick，96×44/帧，朝右，头在 (92,22)。
	/// </summary>
	public class AerialAceLungeProj : LungeProj
	{
		private static ReLogic.Content.Asset<Texture2D> _dashSheet;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f }, target.Center);
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(target.Center, DustID.Cloud, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.2f).noGravity = true;
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player p = Main.player[Projectile.owner];
			Vector2 v = p.velocity;
			if (!p.active || v.LengthSquared() < 4f)
				return false;
			_dashSheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/AerialAceDash/AerialAceDash", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _dashSheet.Value;
			int frameH = tex.Height / 3;
			int frame = (Projectile.timeLeft / 3) % 3;
			// 刹车段淡出；贴图已预乘，用 Color.White * 不透明度（自发光）。
			float fade = Projectile.timeLeft < 10 ? Projectile.timeLeft / 10f : 1f;
			Main.EntitySpriteDraw(tex, (p.Center - Main.screenPosition).Floor(), new Rectangle(0, frameH * frame, tex.Width, frameH),
				Color.White * (0.9f * fade), v.ToRotation(), new Vector2(86f, frameH * 0.5f), 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>意念头锤：鼠标处幽灵锤下砸范围伤。自绘贴图 Assets/Fx/ZenHammer/ZenHammer.png（60×76 单帧，头朝上）；落锤放念力环。</summary>
	public class ZenHammerSmashProj : HenshinMoveProj
	{
		private float _raise;
		private bool _smashed;
		private const float DrawScale = 1.6f;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
					// 落锤（AI 各端）：重命中 + 念力命中两层声，紫光衰减；震屏迁到共享 Shake（保留竖直轴向，主人端）。
					HenshinSfx.Play(SfxKey.HitHeavy, Projectile.Center);
					HenshinSfx.Play(SfxKey.PsyHit, Projectile.Center, 0.9f, -0.2f);
					HenshinExplosion.SpawnLight(Projectile.Center, new Color(190, 100, 255), BlastSize.M);
					HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Heavy, Vector2.UnitY);
					H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.RingPurple, 1.6f, Projectile.owner); // R4：Abra/Mewtwo 都按「其他形态用紫」
				}
			}

		}

		public override bool? CanDamage() => _smashed;

		private bool _castSfx;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void PostAI()
		{
			// 举锤起手（各端首 tick）；本弹已 claim，之力不再响 Item20。
			if (!_castSfx)
			{
				_castSfx = true;
				HenshinSfx.Play(SfxKey.SwingHeavy, Projectile.Center, 0.8f, 0.1f);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), new Color(190, 100, 255), target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 贴图 alpha 只有 0/255；「幽灵」感用整体 0.9 透明度。
			H3Fx.Draw(H3Fx.ZenHammer, 1, 0, Projectile.Center + new Vector2(0f, _raise), Projectile.rotation, DrawScale, Color.White * 0.9f);
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
			// 散落点一次性大闪（各端；第 2 tick 等 SpawnAtMouse 的位置同步到）。共享 UltCast 已在施放时响，这里只补蓄力回响。
			if (72 - Projectile.timeLeft == 2)
			{
				HenshinSfx.Play(SfxKey.PsyCast, Projectile.Center, 0.9f, -0.15f);
				HenshinExplosion.Spawn(Projectile.Center, BlastSize.M, new Color(200, 110, 255), 150f, kenneyFrame: false);
			}
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
					if (id >= 0)
					{
						// 延迟散射：按住技能时 LastMoveSlot 早已不是 Ultimate，必须从导演弹显式复制。
						PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
						if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
						{
							tagged.EasyCrit = EasyCrit;
							tagged.IgnoreDefensePartial = IgnoreDefensePartial;
						}
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
