using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>H7 组（大比鸟/洛奇亚/烈空坐）自绘贴图懒加载；只在绘制时调用（dedServ 不画）。</summary>
	public static class H7Tex
	{
		private static readonly Dictionary<string, Texture2D> Cache = new();

		public static Texture2D Get(string name)
		{
			if (!Cache.TryGetValue(name, out Texture2D t) || t == null || t.IsDisposed)
			{
				t = ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/" + name + "/" + name, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				Cache[name] = t;
			}
			return t;
		}
	}

	/// <summary>命中闪光（零伤害，由拥有者 NewProjectile 同步给旁观者）。ai0: 0 青白 / 1 橙 / 2 金。</summary>
	public class H7HitFxProj : HenshinMoveProj
	{
		private const int Life = 12;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
		}

		public override bool ShouldUpdatePosition() => false;
		public override bool? CanDamage() => false;

		public static Color Tint(float kind) => kind < 0.5f ? new Color(205, 245, 255) : kind < 1.5f ? new Color(255, 170, 70) : new Color(255, 235, 140);

		/// <param name="scale">闪光倍率；0 = 默认 0.45。</param>
		/// <param name="tier">声光档 0 轻 / 1 重 / 2 大招（ai2，随生成包同步，各端首 tick 播命中音）。</param>
		public static void Spawn(Projectile src, Vector2 at, int kind, float scale = 0f, int tier = 0)
		{
			if (src.owner != Main.myPlayer)
				return;
			int id = Projectile.NewProjectile(src.GetSource_FromThis(), at, Vector2.Zero, ModContent.ProjectileType<H7HitFxProj>(), 0, 0f, src.owner, kind, scale, tier);
			if (id >= 0)
				Main.projectile[id].Center = at;
		}

		public static int TierIndex(HitTier tier) => tier == HitTier.Ultimate ? 2 : tier == HitTier.Heavy ? 1 : 0;

		public override void AI()
		{
			if (Projectile.localAI[0] != 0f)
				return;
			// FX 手感 R3：命中音 + 衰减光放在同步特效弹首 tick（各端）；旧的 3 颗云尘删掉。
			Projectile.localAI[0] = 1f;
			Vector2 at = Projectile.Center;
			int kind = (int)(Projectile.ai[0] + 0.5f);
			int tier = (int)(Projectile.ai[2] + 0.5f);
			bool played = tier >= 2 && HenshinSfx.PlayCooldown(SfxKey.HitUlt, at, HenshinHitFeel.UltimateWindowTicks);
			if (!played && tier == 1)
				played = HenshinSfx.PlayCooldown(SfxKey.HitHeavy, at, 6, 0.85f, kind == 1 ? -0.1f : 0.05f);
			if (!played)
				HenshinSfx.PlayCooldown(kind == 1 ? SfxKey.FireHit : SfxKey.WindHit, at, 3, 1f, kind == 2 ? -0.25f : 0f);
			HenshinExplosion.SpawnLight(at, Tint(Projectile.ai[0]), tier >= 2 ? BlastSize.M : BlastSize.S, tier == 0 ? 0.6f : 1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			float fade = 1f - age / (float)Life;
			float scale = Projectile.ai[1] > 0.01f ? Projectile.ai[1] : 0.45f;
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawFlashImpactFrame(Projectile.Center, HenshinFxDraw.WithAlpha(Tint(Projectile.ai[0]), 0.9f * fade), scale,
				Projectile.whoAmI * 0.7f, Math.Min(HenshinFxDraw.FlashImpactFrames - 1, age / 2));
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>空气斩（大比鸟技能2，取代勇鸟猛攻冲撞）：朝鼠标 360° 飞出一道穿透风刃新月。</summary>
	public class H7AirSlashProj : HenshinMoveProj
	{
		private const float Speed = 17f;
		private const int Life = 54;
		private int _inSolid;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			// R3: wind passes through terrain (hugging the ground no longer cuts it); see AI for the wall limit.
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			Player p = Main.player[Projectile.owner];
			Vector2 v = Projectile.velocity;
			if (v.LengthSquared() < 1f)
			{
				v = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (v.LengthSquared() < 1f)
					v = new Vector2(p.direction, 0f);
			}
			Projectile.velocity = Vector2.Normalize(v) * Speed;
		}

		// R3 手感：施放音改到 AI 首 tick（各端）并声明，之力不再响 UseSound。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.WindCast, Main.player[Projectile.owner].Center, 1f, 0.25f);
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.4f, 0.75f, 0.9f);
			// shortcut: wall limit = centre buried >6 ticks (~100 px) in solid tiles ends the blade; upgrade if it must stop at the first wall.
			if (Collision.SolidCollision(Projectile.Center - new Vector2(4f), 8, 8))
			{
				if (++_inSolid > 6)
				{
					H7HitFxProj.Spawn(Projectile, Projectile.Center, 0);
					Projectile.Kill();
				}
			}
			else
				_inSolid = 0;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// R3: hit sound now plays in H7HitFxProj's first tick (all clients); the 2nd flash is deduped by the sound cooldown.
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(190, 240, 255), target.Center);
			// two offset impact flashes along the blade edge read as a cut
			Vector2 side = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X).SafeNormalize(Vector2.UnitY);
			H7HitFxProj.Spawn(Projectile, target.Center + side * 14f, 0);
			H7HitFxProj.Spawn(Projectile, target.Center - side * 14f, 0);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			float fade = Projectile.timeLeft < 10 ? Projectile.timeLeft / 10f : 1f;
			int frame = age < 3 ? 0 : Projectile.timeLeft < 10 ? 3 : (1 + ((age / 4) & 1));
			Texture2D tex = H7Tex.Get("AirSlashBlade");
			Texture2D streak = H7Tex.Get("AirStreak");
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Vector2 side = new Vector2(-dir.Y, dir.X);
			HenshinFxDraw.BeginAdditive();
			// airflow residue: thin wind ribbons left behind the blade, drifting sideways and fading with distance
			for (int k = 1; k <= 4; k++)
			{
				float back = k * 26f;
				if (back > age * Speed)
					break;
				float sway = MathF.Sin(age * 0.35f + k * 1.7f) * 10f;
				float a = fade * 0.5f / k;
				for (int j = -1; j <= 1; j += 2)
					HenshinFxDraw.DrawAdditiveSheet(streak, 1, 4, ((age / 3) + k) & 3, Projectile.Center - dir * (back + 16f) + side * (j * (16f + k * 5f) + sway),
						HenshinFxDraw.WithAlpha(Color.White, a), 0.9f, Projectile.rotation);
			}
			for (int i = 2; i >= 0; i--) // after-image
			{
				HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, frame, Projectile.Center - Projectile.velocity * (i * 0.9f),
					HenshinFxDraw.WithAlpha(Color.White, fade * (i == 0 ? 1f : 0.28f / i)), 1.35f, Projectile.rotation);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>热风（大比鸟大招，取代自我激励）：控制器朝鼠标连放 3 道扩散热浪，穿透、点燃；每道每敌每 8 tick 一段，单段 = 总伤/8。</summary>
	public class H7HeatWaveDirectorProj : HenshinMoveProj
	{
		private const int Life = 34;
		private Vector2 _dir;
		private int _fired;
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
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			Projectile.Center = p.Center;
			if (Projectile.owner != Main.myPlayer)
				return;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				_dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
				if (_dir.LengthSquared() < 1f)
					_dir = new Vector2(p.direction, 0f);
				_dir.Normalize();
			}
			int age = Life - Projectile.timeLeft;
			if (_fired < 3 && age == _fired * 10)
			{
				_fired++;
				// R3 手感：每波的声音改到 H7HeatWaveProj 首 tick（各端都能听到）；ai0 = 第几波（1..3），一波比一波重。
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.Center + _dir * 30f, _dir.RotatedBy((_fired - 2) * 0.17f) * 13f,
					ModContent.ProjectileType<H7HeatWaveProj>(), Math.Max(1, (int)(Projectile.damage / 10f + 0.5f)), Projectile.knockBack, Projectile.owner, _fired);
				if (id >= 0)
				{
					Main.projectile[id].originalDamage = Math.Max(1, (int)((Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage) / 10f + 0.5f));
					HenshinAccGlobalProjectile.CopyMoveOrigin(Projectile, Main.projectile[id]);
					if (Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
					{
						tagged.Delivery = MoveDelivery.Barrage;
						tagged.EasyCrit = EasyCrit;
					}
				}
			}
		}

		public override bool? CanDamage() => false;
		public override bool PreDraw(ref Color lightColor) => false;
	}

	public class H7HeatWaveProj : HenshinMoveProj
	{
		private const int Life = 50;

		// R4: 热浪自管直线扩散，不吃广角镜追踪（项目既有排除法：HandlesOwnHoming，PostAI 跳过）。
		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 150;
			Projectile.height = 150;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8; // R4: 每道热浪每个敌人每 8 tick 一段（原 60=只 1 段）
		}

		private static readonly Color HeatTint = new(255, 140, 50);

		/// <summary>第几波（1..3，导演弹 ai0 传入并随生成包同步；0 当 1）。</summary>
		private int Wave => Math.Clamp((int)(Projectile.ai[0] + 0.5f), 1, 3);

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			float grow = 1f + 0.75f * (Life - Projectile.timeLeft) / Life; // hitbox 150 -> 262 px
			Projectile.Resize((int)(150 * grow), (int)(150 * grow));
			int w = Wave;
			if (Projectile.localAI[0] == 0f)
			{
				// R3 手感：每波起手 = 火 + 风的"呼"，第 3 波再加中爆；热浪环 M 爆（不叠 Kenney 火烟），第 3 波更大并震大招档。
				Projectile.localAI[0] = 1f;
				Vector2 at = Projectile.Center;
				HenshinSfx.Play(SfxKey.FireCast, at, 1f, -0.35f + 0.12f * (w - 1));
				HenshinSfx.Play(SfxKey.WindCast, at, 0.8f, -0.4f);
				if (w == 3)
					HenshinSfx.Play(SfxKey.BlastM, at, 0.9f, -0.15f);
				HenshinExplosion.Spawn(at, BlastSize.M, HeatTint, 150f + 45f * w);
				HenshinHitFeel.Shake(Projectile.owner, at, w == 3 ? HitTier.Ultimate : HitTier.Heavy, Projectile.velocity);
			}
			float lk = 0.85f + 0.15f * w;
			Lighting.AddLight(Projectile.Center, 0.9f * lk, 0.5f * lk, 0.15f * lk);
			// R3: the wind blows through walls (no tile kill), so it never dies on the ground. 旧的火把尘删掉（风带 + 热晕已够）。
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 240);
			HitTier tier = HenshinHitFeel.TierOf(Projectile, HitTier.Heavy);
			HenshinHitFeel.OnHit(Projectile, target, tier, HeatTint, target.Center);
			H7HitFxProj.Spawn(Projectile, target.Center, 1, 1f, H7HitFxProj.TierIndex(tier));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = Life - Projectile.timeLeft;
			float fade = Projectile.timeLeft < 14 ? Projectile.timeLeft / 14f : 1f;
			float grow = 1f + 0.75f * age / Life;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Vector2 side = new Vector2(-dir.Y, dir.X);
			Texture2D streak = H7Tex.Get("HeatWaveStreak");
			HenshinFxDraw.BeginAdditive();
			// heat haze: wide faint orange glow (stand-in for refraction, no shader)
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center - dir * 30f,
				HenshinFxDraw.WithAlpha(new Color(255, 120, 40), 0.22f * fade), new Vector2(3.2f * grow, 4.4f * grow), Projectile.rotation);
			// wind ribbons: 7 streaks across the front, each lagging and swaying differently
			for (int k = -3; k <= 3; k++)
			{
				float lag = 20f + (k * k) * 9f + ((k & 1) * 14f);
				float sway = MathF.Sin(age * 0.28f + k * 1.3f) * 9f;
				Vector2 pos = Projectile.Center - dir * lag + side * (k * 34f * grow + sway);
				float a = (k == 0 ? 0.95f : 0.8f - 0.07f * Math.Abs(k)) * fade;
				HenshinFxDraw.DrawAdditiveSheet(streak, 1, 4, ((age / 3) + k + 3) & 3, pos,
					HenshinFxDraw.WithAlpha(Color.White, a), 1.35f * (0.85f + 0.15f * grow), Projectile.rotation + sway * 0.004f);
			}
			// leading arc
			int frame = age < 4 ? 0 : Projectile.timeLeft < 12 ? 3 : (1 + ((age / 5) & 1));
			HenshinFxDraw.DrawAdditiveSheet(H7Tex.Get("HeatWaveArc"), 1, 4, frame, Projectile.Center + dir * 36f,
				HenshinFxDraw.WithAlpha(Color.White, 0.75f * fade), 1.3f + 1.0f * age / Life, Projectile.rotation);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>神鸟猛击（洛奇亚技能2）：身前聚光成鸟 → 朝鼠标 360° 射出一只穿透光鸟（原作高暴击）。</summary>
	public class H7SkyAttackProj : HenshinMoveProj
	{
		private const int Charge = 16;
		private const int Total = Charge + 44;
		private const float Speed = 21f;
		private bool _launched;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Total;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 40;
		}

		private int Age => Total - Projectile.timeLeft;
		private bool Flying => Age >= Charge;

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active) { Projectile.Kill(); return; }
			if (!Flying)
			{
				// 聚光：贴在洛奇亚身前，ai0=瞄准角（拥有者每 4 tick 同步）
				if (Projectile.owner == Main.myPlayer)
				{
					Vector2 d = HenshinProjUtil.OwnerMouseWorld(Projectile) - p.Center;
					if (d.LengthSquared() < 1f)
						d = new Vector2(p.direction, 0f);
					Projectile.ai[0] = d.ToRotation();
					if (Age % 4 == 0)
						Projectile.netUpdate = true;
				}
				Projectile.rotation = Projectile.ai[0];
				Projectile.Center = p.Center + Projectile.rotation.ToRotationVector2() * 40f;
				Projectile.velocity = Vector2.Zero;
				if (Age == 0)
					SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.2f }, p.Center);
				Lighting.AddLight(Projectile.Center, 0.9f, 0.6f, 0.2f);
				return;
			}
			if (!_launched)
			{
				_launched = true;
				Projectile.tileCollide = true;
				if (Projectile.owner == Main.myPlayer)
				{
					Projectile.velocity = Projectile.ai[0].ToRotationVector2() * Speed;
					Projectile.netUpdate = true;
				}
				SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.2f }, Projectile.Center);
			}
			if (Projectile.velocity.LengthSquared() > 0.5f)
				Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 1f, 0.7f, 0.25f);
		}

		public override bool? CanDamage() => Flying ? null : false;

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			H7HitFxProj.Spawn(Projectile, Projectile.Center, 2);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.3f }, target.Center);
			H7HitFxProj.Spawn(Projectile, target.Center, 2);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = H7Tex.Get("SkyAttackBird");
			int age = Age;
			float fade = Projectile.timeLeft < 8 ? Projectile.timeLeft / 8f : 1f;
			HenshinFxDraw.BeginAdditive();
			if (!Flying)
			{
				float t = age / (float)Charge;
				HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, (age / 3) & 3, Projectile.Center,
					HenshinFxDraw.WithAlpha(Color.White, 0.35f + 0.6f * t), 0.5f + 0.9f * t, Projectile.rotation);
			}
			else
			{
				for (int i = 3; i >= 0; i--)
					HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, ((age / 3) + i) & 3, Projectile.Center - Projectile.velocity * (i * 0.8f),
						HenshinFxDraw.WithAlpha(Color.White, fade * (i == 0 ? 1f : 0.3f / i)), 1.4f, Projectile.rotation);
			}
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>R3: Sky Attack lunge birds (spawned by SkyAttackLungeProj); small SkyAttackBird fire bird, straight flight.</summary>
	public class H7SkyBirdCrossProj : HenshinMoveProj
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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
		}

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 8);

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				// R3 手感：6 鸟同 tick 出现，共用冷却只响一声（原 Item102 在 OnHitNPC，只有主人听得到）。
				Projectile.localAI[0] = 1f;
				HenshinSfx.PlayCooldown(SfxKey.WindCast, Projectile.Center, 6, 0.9f, 0.2f);
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.7f, 0.4f, 0.12f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(255, 200, 110), target.Center);
			H7HitFxProj.Spawn(Projectile, target.Center, 2, 0.4f, 0);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int age = 36 - Projectile.timeLeft;
			float fade = Projectile.timeLeft < 8 ? Projectile.timeLeft / 8f : 1f;
			Texture2D tex = H7Tex.Get("SkyAttackBird");
			// 火鸟尾焰：橙外晕 + 暖白芯，约 100px 渐细（先画再盖本体）
			HenshinTrail.Strip(Projectile, new Color(255, 130, 40), 12f, new Color(255, 230, 170), 0.35f, 0f, 0.75f * fade);
			HenshinFxDraw.BeginAdditive();
			for (int i = 2; i >= 0; i--)
				HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, ((age / 3) + i) & 3, Projectile.Center - Projectile.velocity * (i * 0.7f),
					HenshinFxDraw.WithAlpha(Color.White, fade * (i == 0 ? 1f : 0.3f / i)), 0.75f, Projectile.rotation);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
