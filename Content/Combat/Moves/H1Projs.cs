using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>H1 组共用：竖排帧条贴图懒加载（只在 PreDraw 里调用，服务端不会走到）。</summary>
	internal static class H1Tex
	{
		public static Texture2D Get(ref Asset<Texture2D> slot, string path)
		{
			slot ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/" + path, AssetRequestMode.ImmediateLoad);
			return slot.Value;
		}

		/// <summary>绘制竖排条带第 frame 帧，origin 为帧中心。</summary>
		public static void DrawFrame(Texture2D tex, int frames, int frame, Vector2 worldPos, Color c, float rot, Vector2 scale, SpriteEffects fx = SpriteEffects.None)
		{
			int fh = tex.Height / frames;
			Rectangle src = new(0, fh * Math.Clamp(frame, 0, frames - 1), tex.Width, fh);
			Main.EntitySpriteDraw(tex, (worldPos - Main.screenPosition).Floor(), src, c, rot, new Vector2(tex.Width * 0.5f, fh * 0.5f), scale, fx, 0f);
		}
	}

	/// <summary>
	/// 命中特效（无伤害、一次性）。ai0：0=波导弹爆圈（5帧）、1=水炮溅花（5帧）；ai1=缩放；ai2=G1HitSfx 命中音码。
	/// 由所有者生成，tML 自动同步；各端只按 timeLeft 推帧，AI 首帧各端播命中音。
	/// </summary>
	public class H1HitFxProj : ModProjectile
	{
		public const int KindAura = 0;
		public const int KindSplash = 1;
		private const int Frames = 5;
		private const int Tpf = 3;
		private static Asset<Texture2D> _aura;
		private static Asset<Texture2D> _splash;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Frames * Tpf;
			Projectile.ignoreWater = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public static void Spawn(Projectile src, Vector2 at, int kind, float scale = 1f, float sfx = 0f)
		{
			if (src.owner != Main.myPlayer)
				return;
			Projectile.NewProjectile(src.GetSource_FromThis(), at, Vector2.Zero, ModContent.ProjectileType<H1HitFxProj>(), 0, 0f, src.owner, kind, scale, sfx);
		}

		public override void AI()
		{
			int kind = (int)Projectile.ai[0];
			float scale = Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				G1HitSfx.Play(Projectile.ai[2], Projectile.Center);
				// 波导弹爆点：S 档爆炸配方（白热核 + 冲击环），不叠火烟帧。
				if (kind == KindAura)
					HenshinExplosion.Spawn(Projectile.Center, BlastSize.S, new Color(110, 190, 255), 96f * scale, kenneyFrame: false);
			}
			if (kind == KindSplash)
				HenshinExplosion.Pulse(Projectile.Center, new Color(90, 170, 255), BlastSize.S, (Frames * Tpf - Projectile.timeLeft) / (float)(Frames * Tpf), 48f * scale, 0.45f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int kind = (int)Projectile.ai[0];
			float scale = Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
			int frame = Math.Clamp((Frames * Tpf - Projectile.timeLeft) / Tpf, 0, Frames - 1);
			Texture2D tex = kind == KindAura ? H1Tex.Get(ref _aura, "AuraSphere/AuraSphereBurst") : H1Tex.Get(ref _splash, "WaterJet/WaterSplash");
			float s = kind == KindAura ? 1.5f * scale : 1.25f * scale;
			// 波导爆是能量（glow，全亮）；水花是实物（lit，乘光）。
			Color c = kind == KindAura ? Color.White : HenshinFxDraw.LightAt(Projectile.Center);
			H1Tex.DrawFrame(tex, Frames, frame, Projectile.Center, c, 0f, new Vector2(s));
			return false;
		}
	}

	/// <summary>波导弹：自绘光球（4 帧），强自带追踪；命中出 5 帧爆圈，不沿线撒粒子。</summary>
	public class AuraSphereOrbProj : HenshinMoveProj
	{
		private static Asset<Texture2D> _tex;
		private bool _hit;

		public override bool HandlesOwnHoming => true;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
		private static readonly Color AuraGlow = new(90, 170, 255);

		public override void SetStaticDefaults()
		{
			HenshinTrail.Setup(Type, 12, 1);
			HenshinSfx.ClaimCastSound(Type);
		}

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = false;
			Projectile.penetrate = 1;
			Projectile.extraUpdates = 1;
			InherentHoming = true;
			HomingTurnRate = 0.22f;
			HomingRangeTiles = 48f;
		}

		public override void AI()
		{
			if (Projectile.velocity.LengthSquared() < 1f)
			{
				Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - Projectile.Center;
				Projectile.velocity = (aim.LengthSquared() < 1f
					? new Vector2(Main.player[Projectile.owner].direction, 0f)
					: Vector2.Normalize(aim)) * 9f;
			}
			HenshinProjUtil.HomingAI(Projectile, true, HomingTurnRate);
			if (Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				HenshinSfx.Play(SfxKey.PsyCast, Projectile.Center, 0.9f, 0.3f);
			}
			Lighting.AddLight(Projectile.Center, 0.4f, 0.75f, 1.2f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			_hit = true;
			// 命中音由 H1HitFxProj 首帧各端播；旧 Item10 + 3 颗尘删去（爆圈 + 爆炸配方替代）。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Heavy), AuraGlow);
		}

		public override void OnKill(int timeLeft)
		{
			if (_hit)
				H1HitFxProj.Spawn(Projectile, Projectile.Center, H1HitFxProj.KindAura, 1f, G1HitSfx.Code(SfxKey.HitHeavy, 4));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// 蓝→白细尾（约 1.5 倍弹径），球后加一层 SoftGlow 外晕，球体不再发平。
			HenshinTrail.Strip(Projectile, AuraGlow, 30f, new Color(220, 240, 255), 0.36f, 0f, 0.85f);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveCentered(HenshinFxDraw.SoftGlow, Projectile.Center, HenshinFxDraw.WithAlpha(AuraGlow, 0.32f),
				HenshinFxDraw.ScaleForWorldDiameter(HenshinFxDraw.SoftGlow, 56f * 1.8f));
			HenshinFxDraw.EndAdditive();
			Texture2D tex = H1Tex.Get(ref _tex, "AuraSphere/AuraSphere");
			int frame = (int)(Main.GameUpdateCount / 4 % 4);
			H1Tex.DrawFrame(tex, 4, frame, Projectile.Center, Color.White, 0f, new Vector2(1.45f));
			return false;
		}
	}

	/// <summary>
	/// 过热（喷火龙大招）：指针落点 15 格半径大爆（480 hitbox），5 段脉冲（每 6t 开 2t，×同原倍率）。
	/// 自绘 6 帧爆炎贴图（Assets/Fx/Overheat/OverheatBlast，160px 帧 ×3 = 480），每段脉冲只 2 颗火星。ai1=Buff。
	/// </summary>
	public class OverheatBlastProj : HenshinMoveProj
	{
		private const int Life = 30;
		private static Asset<Texture2D> _tex;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 480;
			Projectile.height = 480;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		public override bool ShouldUpdatePosition() => false;

		private static readonly Color HeatTint = new(255, 120, 40);

		public override void AI()
		{
			int age = Life - Projectile.timeLeft;
			// 起爆那一下最亮（L 档光脉冲，光点随冲击环外扩），之后保底一层余热。
			HenshinExplosion.Pulse(Projectile.Center, HeatTint, BlastSize.L, age / (float)Life, 420f);
			Lighting.AddLight(Projectile.Center, 0.6f, 0.3f, 0.1f);
			if (age % 6 == 0 && age / 6 < 5)
			{
				if (age == 0)
				{
					// 第 0 段：大爆 + 大招命中音 + 大招震屏（主人端）；后 4 段只是小声余波。
					HenshinSfx.Play(SfxKey.BlastL, Projectile.Center);
					HenshinSfx.Play(SfxKey.FireHit, Projectile.Center, 1f, -0.3f);
					HenshinHitFeel.Shake(Projectile.owner, Projectile.Center, HitTier.Ultimate);
				}
				else
				{
					HenshinSfx.Play(SfxKey.BlastS, Projectile.Center, 0.55f, -0.1f * (age / 6));
					HenshinExplosion.SpawnLight(Projectile.Center, HeatTint, BlastSize.M, 0.6f);
				}
			}
		}

		public override bool? CanDamage()
		{
			int age = Life - Projectile.timeLeft;
			return age % 6 <= 1 && age / 6 < 5;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] > 0)
				target.AddBuff((int)Projectile.ai[1], 180);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Ultimate), HeatTint, target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = H1Tex.Get(ref _tex, "Overheat/OverheatBlast");
			int age = Life - Projectile.timeLeft;
			int frame = Math.Clamp(age / 5, 0, 5);
			float fade = frame >= 5 ? 0.8f : 1f;
			HenshinFxDraw.BeginAdditive();
			// 爆炸配方 L（起爆白闪 + 双冲击环 + 10 道放射亮条 + Kenney 火烟帧）垫在手绘爆炎下面。
			HenshinExplosion.Draw(Projectile.Center, BlastSize.L, HeatTint, age / (float)Life, 420f);
			HenshinFxDraw.DrawAdditiveSheet(tex, 1, 6, frame, Projectile.Center, HenshinFxDraw.WithAlpha(Color.White, fade), 3f);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 花瓣舞（妙蛙花）：以自身为圆心的旋转花瓣环，半径 15 格（hitbox 480，同原）。
	/// 内外两圈反向旋转的自绘花瓣（4 帧翻转），前 12t 由内向外展开；不再每 tick 撒粒子。
	/// </summary>
	public class PetalDanceH1Proj : HenshinMoveProj
	{
		private const int Life = 72;
		private const int Petals = 8;
		private static Asset<Texture2D> _tex;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

		public override void SetDefaults()
		{
			Projectile.width = 480;
			Projectile.height = 480;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (!p.active)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = p.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.GrassCast, p.Center, 1f, 0.2f);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 没有命中特效弹：命中音只在主人端（节流 12 tick）；旧 2 颗粉尘删去，改白闪 + 命中光。
			HenshinSfx.PlayCooldown(SfxKey.GrassHit, target.Center, 12, 0.8f, 0.25f);
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(255, 140, 200), target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = H1Tex.Get(ref _tex, "PetalDance/Petal");
			int age = Life - Projectile.timeLeft;
			float open = MathHelper.Clamp(age / 12f, 0f, 1f);
			float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
			float spin = age * 0.13f;
			// 花瓣是实物（lit）：每片在自己位置取光（外圈离主人约 13 格），不设亮度下限。
			for (int ring = 0; ring < 2; ring++)
			{
				float r = (ring == 0 ? 120f : 215f) * open;
				float dirSign = ring == 0 ? -1f : 1f;
				for (int i = 0; i < Petals; i++)
				{
					float ang = dirSign * spin + MathHelper.TwoPi * i / Petals + ring * 0.4f;
					Vector2 pos = Projectile.Center + ang.ToRotationVector2() * r;
					int frame = (age / 4 + i) % 4;
					Color c = HenshinFxDraw.LightAt(pos) * fadeOut;
					H1Tex.DrawFrame(tex, 4, frame, pos, c, ang + MathHelper.PiOver2 * dirSign, new Vector2(ring == 0 ? 1.5f : 1.9f));
				}
			}
			return false;
		}
	}

	/// <summary>日光束蓄力：自绘太阳球（4 帧）在身前聚拢，约 0.8s 后在 timeLeft==8 朝鼠标发出 SolarBeamProj。</summary>
	public class SolarBeamChargeProj : HenshinMoveProj
	{
		private const int Life = 55;
		private static Asset<Texture2D> _orb;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		// 蓄力起点自己放 UltCharge（各端），放出光柱时 SolarBeamProj 再放 UltCast；不要共享 UltCast 在蓄力第一帧抢先。
		public override void SetStaticDefaults() => HenshinSfx.ClaimCastSound(Type);

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
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			float charge = 1f - Projectile.timeLeft / (float)Life;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				HenshinSfx.Play(SfxKey.UltCharge, owner.MountedCenter);
			}
			Lighting.AddLight(Projectile.Center, 1.3f * charge, 1.05f * charge, 0.35f * charge);

			if (Projectile.timeLeft == 8 && Projectile.owner == Main.myPlayer)
			{
				Vector2 dir = HenshinProjUtil.OwnerMouseWorld(Projectile) - owner.Center;
				if (dir == Vector2.Zero) dir = new Vector2(owner.direction, 0f);
				dir.Normalize();
				int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter, dir * 16f,
					ModContent.ProjectileType<SolarBeamProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				if (id >= 0 && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
				{
					tagged.EasyCrit = EasyCrit;
					tagged.IgnoreDefensePartial = IgnoreDefensePartial;
				}
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = H1Tex.Get(ref _orb, "SolarBeam/SolarOrb");
			float charge = 1f - Projectile.timeLeft / (float)Life;
			float s = 0.5f + charge * 1.3f;
			int frame = (int)(Main.GameUpdateCount / 3 % 4);
			HenshinFxDraw.BeginAdditive();
			HenshinFxDraw.DrawAdditiveSheet(tex, 1, 4, frame, Projectile.Center, HenshinFxDraw.WithAlpha(Color.White, 0.5f + charge * 0.5f), s);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}

	/// <summary>
	/// 日光束发射段：一道粗金白光柱（原作样子），跟随鼠标 360°，100t，每 4t 一段线伤（同原 SolarPrismBeamProj，伤害总量不变）。
	/// 光柱用 SolarBeamBody 4 帧流动贴图平铺；枪口一颗太阳球；不撒粒子。
	/// </summary>
	public class SolarBeamProj : HenshinMoveProj
	{
		public const float MaxLength = 64f * 16f;
		public const int Life = 100;
		private const float HitWidth = 80f;
		private const float TileScale = 1.5f;
		private static Asset<Texture2D> _body;
		private static Asset<Texture2D> _orb;
		private Vector2 _from;
		private Vector2 _to;
		private bool _inited;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 4;
		}

		public override bool ShouldUpdatePosition() => false;

		private float Env()
		{
			float e = Projectile.timeLeft / (float)Life;
			if (e > 0.94f) return (1f - e) / 0.06f;
			if (e < 0.12f) return e / 0.12f;
			return 1f;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			bool first = !_inited;
			if (first)
			{
				_inited = true;
				// 放出：大招施放音 + 大爆低频（蓄力根弹已 claim，共享 UltCast 不会再响）+ 主人端大招震屏。
				HenshinSfx.Play(SfxKey.UltCast, owner.MountedCenter);
				HenshinSfx.Play(SfxKey.BlastL, owner.MountedCenter, 0.75f, 0.15f);
				HenshinHitFeel.Shake(Projectile.owner, owner.MountedCenter, HitTier.Ultimate);
			}
			_from = owner.MountedCenter;
			Vector2 aim = HenshinProjUtil.OwnerMouseWorld(Projectile) - _from;
			if (aim.LengthSquared() < 1f)
				aim = new Vector2(owner.direction, 0f);
			aim.Normalize();
			_to = _from + aim * MaxLength;
			Projectile.Center = Vector2.Lerp(_from, _to, 0.4f);
			owner.direction = aim.X >= 0f ? 1 : -1;
			if (first)
				HenshinExplosion.Spawn(_from + aim * 24f, BlastSize.L, SolarTint, 200f, kenneyFrame: false); // 枪口白闪
			Lighting.AddLight(Projectile.Center, 1.1f, 0.9f, 0.35f);
			Lighting.AddLight(_from + aim * 40f, 1.2f * Env(), 1.0f * Env(), 0.4f * Env());
		}

		private static readonly Color SolarTint = new(255, 220, 110);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 光柱没有命中特效弹：命中音只在主人端（重击 8 tick 节流 + 每次施放一声 HitUlt）。
			HenshinSfx.PlayCooldown(SfxKey.HitHeavy, target.Center, 8, 0.8f);
			HenshinSfx.PlayCooldown(SfxKey.HitUlt, target.Center, 60);
			// 100 tick 持续光柱每 4 tick 一段：只用 Light 档（白闪 + 命中光），重震只在放出那一下，免得全程抖屏。
			HenshinHitFeel.OnHit(Projectile, target, HitTier.Light, SolarTint, target.Center);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if ((_to - _from).LengthSquared() < 1f)
				return false;
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _from, _to, HitWidth * Math.Max(0.3f, Env()), ref _);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!_inited)
				return false;
			Texture2D body = H1Tex.Get(ref _body, "SolarBeam/SolarBeamBody");
			Texture2D orb = H1Tex.Get(ref _orb, "SolarBeam/SolarOrb");
			float env = Env();
			Vector2 dir = _to - _from;
			float rot = dir.ToRotation();
			Vector2 n = Vector2.Normalize(dir);
			float seg = 24f * 2f * TileScale; // 一块 48px 贴图 × 缩放
			int frame = (int)(Main.GameUpdateCount / 3 % 4);
			int count = (int)(MaxLength / seg);
			Color col = HenshinFxDraw.WithAlpha(Color.White, 0.95f * env);
			HenshinFxDraw.BeginAdditive();
			for (int i = 0; i < count; i++)
				HenshinFxDraw.DrawAdditiveSheet(body, 1, 4, frame, _from + n * (seg * (i + 0.5f)), col, new Vector2(TileScale, TileScale * env), rot);
			HenshinFxDraw.DrawAdditiveSheet(orb, 1, 4, frame, _from, col, 1.7f * (0.6f + 0.4f * env));
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}
