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

			// 波纹颜色变体（随弹 ai1 同步）：0 胡地金 / 1 巨金怪青蓝 / 2 超梦紫（穿墙版）。
			// 胡地与巨金怪共用同一 MoveSpec，只能靠施放者当前形态区分。
			float variant = pierceTiles ? 2f : 0f;
			if (!pierceTiles && Main.player[Projectile.owner].GetModPlayer<PokemonHenshin.Content.PlayerState.HenshinPlayer>().CurrentForm?.FormId == "L08_F02")
				variant = 1f;

			Vector2 mouse = HenshinProjUtil.OwnerMouseWorld(Projectile);
			int boltDmg = Math.Max(1, Projectile.damage);
			for (int i = 0; i < count; i++)
			{
				Vector2 offset = Main.rand.NextVector2Circular(28f, 28f);
				Vector2 spawn = mouse + offset;
				Vector2 vel = Main.rand.NextVector2Circular(2.2f, 2.2f);
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
					ModContent.ProjectileType<AlakazamPsychicBoltProj>(), boltDmg, Projectile.knockBack, Projectile.owner,
					pierceTiles ? 1f : 0f, variant);
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
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), mouse, H3Fx.RingKind((int)variant), 0.9f, Projectile.owner);
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

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		private bool PierceTiles => Projectile.ai[0] > 0.5f;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 225;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 1;
			Projectile.scale = 1.25f;
			InherentHoming = true;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Projectile.penetrate = 1;
			Projectile.tileCollide = !PierceTiles;
		}

		public override void AI()
		{
			Projectile.tileCollide = !PierceTiles;
			Projectile.penetrate = 1;
			if (Projectile.velocity.LengthSquared() > 0.25f)
				Projectile.rotation = Projectile.velocity.ToRotation();
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

			Lighting.AddLight(Projectile.Center, H3Fx.VariantLight[Math.Clamp((int)Projectile.ai[1], 0, 2)] * 0.7f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// 撞墙：同色念力环（不再用 TileImpact 的粉色尘）
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.RingKind((int)Projectile.ai[1]), 0.4f, Projectile.owner);
			SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 180);
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.RingKind((int)Projectile.ai[1]), 0.5f, Projectile.owner);
		}

		// 贴图 Assets/Fx/Psychic/PsychicWave{A,M,T}：朝飞行方向推进的同心波纹（ai1：0 金/1 青蓝/2 紫）。出现时 10 tick 内放大。
		public override bool PreDraw(ref Color lightColor)
		{
			float grow = MathHelper.Clamp(Projectile.localAI[0] / 10f, 0.3f, 1f);
			H3Fx.DrawRipple((int)Projectile.ai[1], Projectile.Center, Projectile.rotation, 0.9f * grow, 1f);
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
		private const float FlightSpeed = 16.5f; // R5: 11 -> 16.5 (+50%)
		private bool _exploding;
		private int _explodeAge;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
		public override bool HandlesOwnHoming => true;

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
			InherentHoming = true;
		}

		public override void OnSpawn(IEntitySource source)
		{
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
			// 波纹朝目标（无目标则朝飞行方向）
			NPC aim = ResolveTarget();
			if (aim != null)
				Projectile.rotation = (aim.Center - Projectile.Center).ToRotation();
			else if (Projectile.velocity.LengthSquared() > 0.25f)
				Projectile.rotation = Projectile.velocity.ToRotation();

			if (Age < FadeTicks)
			{
				Projectile.velocity *= 0.85f;
				Lighting.AddLight(Projectile.Center, H3Fx.VariantLight[0] * fade);
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
							Vector2.Normalize(Projectile.velocity), desired, 0.33f)) * speed; // R5: lerp 0.22 -> 0.33 (+50%)
					}
				}
			}
			else if (Projectile.velocity.LengthSquared() < 0.25f)
			{
				Projectile.velocity = Main.rand.NextVector2CircularEdge(FlightSpeed, FlightSpeed);
			}

			Lighting.AddLight(Projectile.Center, H3Fx.VariantLight[0]);
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
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), Projectile.Center, H3Fx.RingGold, 1.1f, Projectile.owner);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => BeginExplode(keepAliveForDraw: true);

		public override void OnKill(int timeLeft)
		{
			if (!_exploding)
				BeginExplode(keepAliveForDraw: false);
		}

		// 预知未来：0–60 tick 在目标身上渐显 PsychicRing 预兆环 + 胡地金色念力波纹（朝目标）；之后飞向目标。爆炸由 H3HitFxProj 播放。
		public override bool PreDraw(ref Color lightColor)
		{
			if (_exploding)
				return false;
			float a = 1f - Projectile.alpha / 255f;
			if (Age < FadeTicks)
				H3Fx.Draw(H3Fx.HitSheet(H3Fx.RingGold), 5, 3, Projectile.Center, Main.GlobalTimeWrappedHourly * 2f,
					0.5f + 0.5f * a, Color.White * (0.8f * a));
			H3Fx.DrawRipple(0, Projectile.Center, Projectile.rotation, Projectile.scale * 1.1f, a);
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
		private const float FistScale = 1.5f;
		private Vector2 _dir;
		private Vector2 _origin;
		private bool _shook;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.MountedCenter;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(owner.direction, 0f);
				_dir.Normalize();
				_origin = owner.MountedCenter;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, owner.Center);
			}

			float u = MathHelper.SmoothStep(0f, 1f, Progress);
			Projectile.Center = _origin + _dir * (40f + Reach * u);

		}

		// 命中冲击：额外击退 +4（真气拳 1.8 倍率不变）。
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			base.ModifyHitNPC(target, ref modifiers);
			modifiers.Knockback += 4f;
		}

		// 命中：破防 4 秒 + 放大的冲击波帧动画 + 屏幕震动（仅拥有者，每拳一次）。
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.BrokenArmor, 240);
			H3Fx.SpawnHit(Projectile.GetSource_FromThis(), target.Center, H3Fx.FocusHit, 1.7f, Projectile.owner);
			if (!_shook && Projectile.owner == Main.myPlayer)
			{
				_shook = true;
				Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
					target.Center, _dir, 6f, 8f, 12, 1000f, "FocusPunch"));
				SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.1f }, target.Center);
			}
		}

		// 贴图 Assets/Fx/FocusPunch：FocusFist（朝右）+ 前 12 tick 向拳头收拢的蓄力光环 FocusAura + 两道拳影；朝左半圈上下翻转。
		public override bool PreDraw(ref Color lightColor)
		{
			float rot = _dir.ToRotation();
			SpriteEffects fx = _dir.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			int frame = (int)(Main.GameUpdateCount / 3) % 4;
			int age = Lifetime - Projectile.timeLeft;
			if (age < 12)
				H3Fx.Draw(H3Fx.FocusAura, 4, age / 3, _origin + _dir * 44f, 0f, 1.3f, Color.White);
			if (Progress < 0.85f)
			{
				for (int i = 2; i >= 1; i--)
					H3Fx.Draw(H3Fx.FocusFist, 4, frame, Projectile.Center - _dir * (14f * i), rot, FistScale, Color.White * (0.35f / i), fx);
			}
			H3Fx.Draw(H3Fx.FocusFist, 4, frame, Projectile.Center, rot, FistScale, Color.White, fx);
			return false;
		}
	}
}
