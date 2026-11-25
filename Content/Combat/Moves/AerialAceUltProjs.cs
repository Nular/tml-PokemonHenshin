using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 燕返（波波大招）控制器：拥有者端驱动玩家位置（逐 tick 写 Center，原版 PlayerControls 同步），
	/// 锁定鼠标附近最近敌，绕目标做 5 次“瞬身穿过 → 折返”斩（轴线每次转 144°，连成星形），
	/// 最后一斩后落在目标另一侧并爆开。全程无敌。斩/爆由拥有者生成，伤害与外观靠射弹自身同步。
	/// 无目标：以鼠标方向 320 px 处为虚拟中心，仍斩线 + 爆开。
	/// </summary>
	public class AerialAceUltProj : HenshinMoveProj
	{
		public const int Passes = 5;
		public const float Radius = 150f;
		private const float CursorSearch = 520f;
		private const float PlayerSearch = 1000f;
		private const int HoldTicks = 8;
		private const float StarStep = MathHelper.Pi * 0.8f; // 144°

		private bool _inited;
		private int _pass = -1;
		private int _t;
		private int _dur;
		private int _hold;
		private int _target = -1;
		private Vector2 _c;
		private Vector2 _from;
		private Vector2 _to;
		private Vector2 _origin;
		private float _a0;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
		}

		public override bool? CanDamage() => false;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active || p.dead)
			{
				Projectile.Kill();
				return;
			}

			Projectile.Center = p.Center;
			if (Projectile.owner != Main.myPlayer)
				return;

			if (!_inited)
				Init(p);

			p.immune = true;
			p.immuneTime = Math.Max(p.immuneTime, 20);
			p.velocity = Vector2.Zero;
			p.fallStart = (int)(p.Center.Y / 16f);

			if (_pass >= Passes)
			{
				if (++_hold >= HoldTicks)
					Projectile.Kill();
				return;
			}

			if (_t >= _dur)
			{
				if (_pass >= 0)
					p.Center = _to;
				_pass++;
				if (_pass >= Passes)
				{
					Finish(p);
					return;
				}
				StartPass(p);
			}

			_t++;
			p.Center = Vector2.Lerp(_from, _to, MathHelper.Clamp(_t / (float)_dur, 0f, 1f));
		}

		private void Init(Player p)
		{
			_inited = true;
			_origin = p.Center;
			Vector2 cursor = HenshinProjUtil.OwnerMouseWorld(Projectile);
			float best = CursorSearch * CursorSearch;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.life <= 0 || !n.CanBeChasedBy())
					continue;
				if (Vector2.DistanceSquared(p.Center, n.Center) > PlayerSearch * PlayerSearch)
					continue;
				float d = Vector2.DistanceSquared(cursor, n.Center);
				if (d < best)
				{
					best = d;
					_target = i;
				}
			}

			if (_target >= 0)
				_c = Main.npc[_target].Center;
			else
			{
				Vector2 dir = cursor - p.Center;
				dir = dir.LengthSquared() < 1f ? new Vector2(p.direction, 0f) : Vector2.Normalize(dir);
				_c = p.Center + dir * 320f;
			}

			Vector2 away = p.Center - _c;
			if (away.LengthSquared() < 1f)
				away = new Vector2(-p.direction, 0f);
			_a0 = away.ToRotation();
			_pass = -1;
			_t = 0;
			_dur = 0; // 第一 tick 立刻进入第 0 斩
		}

		/// <summary>目标中心 + dir*半径；落在方块里则逐步缩短半径（≥40px）。</summary>
		private Vector2 FreePoint(Player p, Vector2 dir)
		{
			for (float r = Radius; r > 40f; r -= 10f)
			{
				Vector2 cand = _c + dir * r;
				if (!Collision.SolidCollision(cand - new Vector2(p.width, p.height) * 0.5f, p.width, p.height))
					return cand;
			}
			return _c + dir * 40f;
		}

		private void StartPass(Player p)
		{
			// 目标还活着就跟随它的当前位置；死了/没有就沿用最后的中心。
			if (_target >= 0)
			{
				NPC n = Main.npc[_target];
				if (n.active && n.life > 0)
					_c = n.Center;
				else
					_target = -1;
			}

			float ang = _a0 + _pass * StarStep;
			Vector2 u = ang.ToRotationVector2();
			// 起终点在方块里就沿轴向目标缩短半径到空位（最少 40px），避免瞬身进墙/穿墙斩。
			Vector2 start = _pass == 0 ? p.Center : FreePoint(p, u);
			Vector2 end = _pass == 0
				? FreePoint(p, Vector2.Normalize(_c - start == Vector2.Zero ? -u : _c - start))
				: FreePoint(p, -u);

			_from = start;
			_to = end;
			_dur = _pass == 0 ? (int)MathHelper.Clamp(Vector2.Distance(start, end) / 64f, 6f, 14f) : 5;
			_t = 0;

			p.Center = start; // 折返：瞬身到下一斩起点
			p.direction = end.X >= start.X ? 1 : -1;

			SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.65f, Volume = 0.8f }, p.Center);
			SpawnGhost(p, start, end - start);

			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), start, end - start,
				ModContent.ProjectileType<AerialAceSlashProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
				_target, _dur, _pass);
			if (id >= 0 && id < Main.maxProjectiles)
			{
				Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
				PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
			}
		}

		private void Finish(Player p)
		{
			Vector2 land = p.Center;
			if (Collision.SolidCollision(land - new Vector2(p.width, p.height) * 0.5f, p.width, p.height))
			{
				bool found = false;
				for (int ring = 1; ring <= 2 && !found; ring++)
				{
					for (int i = 0; i < 8; i++)
					{
						Vector2 cand = _c + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (Radius * ring);
						if (!Collision.SolidCollision(cand - new Vector2(p.width, p.height) * 0.5f, p.width, p.height))
						{
							land = cand;
							found = true;
							break;
						}
					}
				}
				if (!found)
					land = _origin;
			}
			p.Center = land;
			p.velocity = Vector2.Zero;
			p.immuneTime = Math.Max(p.immuneTime, 40);

			SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.3f }, _c);
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), _c, Vector2.Zero,
				ModContent.ProjectileType<AerialAceBurstProj>(), Projectile.damage * 2, Projectile.knockBack * 1.5f, Projectile.owner,
				_target);
			if (id >= 0 && id < Main.maxProjectiles)
			{
				Main.projectile[id].Center = _c;
				Main.projectile[id].originalDamage = Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage;
				PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
			}
		}

		/// <summary>复用电光一闪的形态残影（ai0=帧，ai1=移动状态，velocity=路径）。</summary>
		private void SpawnGhost(Player p, Vector2 from, Vector2 delta)
		{
			float frame = 0f;
			float state = 0f;
			HenshinPlayer hp = p.GetModPlayer<HenshinPlayer>();
			if (hp.CurrentForm?.Locomotion != null)
			{
				state = (float)hp.LocomotionState;
				frame = hp.LocomotionFrame;
			}
			int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), from, delta,
				ModContent.ProjectileType<BlinkStrikeGhostProj>(), 0, 0f, Projectile.owner, frame, state);
			if (id >= 0 && id < Main.maxProjectiles)
				Main.projectile[id].Center = from;
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}

	/// <summary>
	/// 一道斩痕。Center=起点，velocity=终点-起点（不自走，联机靠 spawn 包同步）；ai0=锁定目标 whoAmI(-1 无)，ai1=本斩 tick 数，ai2=第几斩。
	/// 判定：起点→刀锋当前位置的线段（宽 28px）；目标在刀锋到达末端时保底必中。外观：连续光束 + 刀锋新月 AerialAceSlash。
	/// </summary>
	public class AerialAceSlashProj : HenshinMoveProj
	{
		private const int Life = 30;
		private static ReLogic.Content.Asset<Texture2D> _sheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life + 10;
		}

		public override bool ShouldUpdatePosition() => false;

		private int Age => Life - Projectile.timeLeft;
		private int Dur => Math.Max(1, (int)Projectile.ai[1]);

		public override bool? CanHitNPC(NPC target)
			=> Age >= 1 && Age <= Dur + 2 ? null : false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			int dur = Dur;
			int who = (int)Projectile.ai[0];
			if (who >= 0 && who < Main.maxNPCs && Age >= dur - 1)
			{
				NPC t = Main.npc[who];
				if (t.active && t.Hitbox == targetHitbox)
					return true;
			}
			float f = MathHelper.Clamp((Age + 1) / (float)dur, 0f, 1f);
			Vector2 head = Projectile.Center + Projectile.velocity * f;
			// 不穿墙斩：起点到目标中心被方块挡住则不命中（锁定目标保底在上面已返回 true）
			if (!Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1))
				return false;
			float pt = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, head, 28f, ref pt);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.5f }, target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Age;
			int dur = Dur;
			Vector2 s = Projectile.Center;
			float f = MathHelper.Clamp((age + 1) / (float)dur, 0f, 1f);
			Vector2 head = s + Projectile.velocity * f;
			float fade = age <= dur ? 1f : MathHelper.Clamp(1f - (age - dur) / (float)(Life - dur), 0f, 1f);
			fade *= fade;
			if (fade <= 0.02f)
				return false;

			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/AerialAce/AerialAceSlash", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;

			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawContinuousBeam(s, head,
				HenshinFxDraw.WithAlpha(new Color(235, 250, 255), fade),
				HenshinFxDraw.WithAlpha(new Color(90, 170, 255), 0.7f * fade),
				4f + 4f * fade, 14f + 8f * fade);

			int frame = age < 2 ? 0 : age < dur ? 1 + ((age / 2) & 1) : 3;
			Color c = HenshinFxDraw.WithAlpha(Color.White, fade);
			HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, frame, head, c, 1.5f, Projectile.velocity.ToRotation());
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>终结爆：ai0=锁定目标 whoAmI。圆形判定（220px），目标保底必中；外观 = 闪光帧 + 4 片向外旋开的风刃新月。</summary>
	public class AerialAceBurstProj : HenshinMoveProj
	{
		private const int Life = 16;
		private static ReLogic.Content.Asset<Texture2D> _sheet;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 220;
			Projectile.height = 220;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Life + 10;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			int who = (int)Projectile.ai[0];
			if (who >= 0 && who < Main.maxNPCs)
			{
				NPC t = Main.npc[who];
				if (t.active && t.Hitbox == targetHitbox)
					return true;
			}
			return null;
		}

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 0.55f, 0.8f, 1f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.4f }, target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			float t = age / (float)Life;
			float fade = 1f - t * t;
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/AerialAce/AerialAceSlash", ReLogic.Content.AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;

			HenshinFxDraw.BeginAdditive();
			int flash = Math.Min(HenshinFxDraw.FlashImpactFrames - 1, age / 2);
			HenshinFxDraw.DrawFlashImpactFrame(Projectile.Center,
				HenshinFxDraw.WithAlpha(new Color(205, 235, 255), 0.85f * fade), 0.5f, 0f, flash);

			int frame = age < 4 ? 1 : age < 10 ? 2 : 3;
			for (int i = 0; i < 4; i++)
			{
				float ang = i * MathHelper.PiOver2 + t * 0.9f;
				Vector2 pos = Projectile.Center + ang.ToRotationVector2() * (22f + t * 110f);
				HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, frame, pos, HenshinFxDraw.WithAlpha(Color.White, fade), 1.3f, ang);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
