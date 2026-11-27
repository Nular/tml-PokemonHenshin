using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>种子机关枪导演（妙蛙种子大招）：ai1=数量，ai2=射速；施放时锁一次朝鼠标方向，错开发射 SeedGunShotProj。</summary>
	public class SeedGunDirectorProj : HenshinMoveProj
	{
		private int _fired;
		private int _total;
		private float _speed;
		private Vector2 _dir;
		private bool _ready;

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

		private void EnsureReady()
		{
			if (_ready)
				return;
			_ready = true;
			_total = (int)Math.Max(1, Projectile.ai[1]);
			_speed = Projectile.ai[2] > 0f ? Projectile.ai[2] : 16f;
			Player owner = Main.player[Projectile.owner];
			_dir = HenshinProjUtil.OwnerAimDir(owner, owner.MountedCenter);
		}

		public override void AI()
		{
			EnsureReady();
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter;
			const int duration = 48;
			int perTick = Math.Max(1, (_total + duration - 1) / duration);
			// 连射节奏（各端，按弹龄算，不靠只在主人端累加的 _fired）：每 4 tick 一声草系发射。施放由共享 UltCast 负责。
			int fireAge = 90 - Projectile.timeLeft;
			if (fireAge < duration && fireAge % 4 == 0)
				HenshinSfx.PlayCooldown(SfxKey.GrassCast, owner.MountedCenter, 4, 0.55f, 0.3f);

			if (Projectile.owner == Main.myPlayer && _fired < _total)
			{
				int budget = Math.Min(perTick, _total - _fired);
				for (int i = 0; i < budget; i++)
				{
					float spread = (_fired / (float)_total - 0.5f) * 0.12f;
					Vector2 vel = _dir.RotatedBy(spread) * _speed;
					Vector2 spawn = owner.MountedCenter + _dir * 16f + Main.rand.NextVector2Circular(4f, 4f);
					int perShot = Math.Max(1, Projectile.damage / (_total <= 32 ? 4 : 6));
					int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel,
						ModContent.ProjectileType<SeedGunShotProj>(), perShot, Projectile.knockBack * 0.2f, Projectile.owner,
						0f, EasyCrit ? 1f : 0f, 0f);
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

	/// <summary>种子机关枪单发：自绘种子贴图（Assets/Fx/SeedGun/SeedShot），飞行不撒粒子，命中/撞墙仅 2 颗草屑。ai1=EasyCrit。</summary>
	public class SeedGunShotProj : HenshinMoveProj
	{
		public override bool HandlesOwnHoming => true;
		private bool _ready;
		private static Asset<Texture2D> _tex;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

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

		public override void SetStaticDefaults() => HenshinTrail.Setup(Type, 6, 1); // extraUpdates=1

		public override void AI()
		{
			if (!_ready)
			{
				_ready = true;
				EasyCrit = Projectile.ai[1] > 0f; // ai 槽随生成同步，旁观端也能读到
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			HenshinProjUtil.HomingAI(Projectile, Homing, HomingTurnRate);
			Lighting.AddLight(Projectile.Center, 0.18f, 0.45f, 0.14f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 出自大招槽 → 大招档：白闪 + 震屏/顿帧 20 tick 一次（64 发连射成节奏脉冲）。
			HenshinHitFeel.OnHit(Projectile, target, HenshinHitFeel.TierOf(Projectile, HitTier.Light), new Color(120, 230, 90));
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			HenshinTileImpact.FromCollide(Projectile, oldVelocity, TileImpactKind.Seed);
			return true;
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ || timeLeft <= 0)
				return;
			// 各端：草系命中限频（64 发同端 4 tick 内一声）+ 小光脉冲；草屑 2 → 1。
			HenshinSfx.PlayCooldown(SfxKey.GrassHit, Projectile.Center, 4, 0.8f);
			HenshinExplosion.SpawnLight(Projectile.Center, new Color(120, 230, 90), BlastSize.S, 0.5f);
			Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Grass, Main.rand.NextVector2Circular(2f, 2f), 60, default, 1f);
			d.noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_tex ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SeedGun/SeedShot", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _tex.Value;
			HenshinTrail.Strip(Projectile, new Color(110, 220, 80), 8f, new Color(230, 255, 180), 0.4f, 0f, 0.75f);
			Color c = new(Math.Max(lightColor.R, (byte)130), Math.Max(lightColor.G, (byte)130), Math.Max(lightColor.B, (byte)110));
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), null, c, Projectile.rotation,
				new Vector2(tex.Width * 0.5f, tex.Height * 0.5f), 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	/// <summary>种子炸弹爆点：320×320 草系 AoE（数值同旧 MouseAoEBurstProj 草分支：24 tick、同弹 6 tick），自绘 6 帧爆圈，不撒粒子。</summary>
	public class SeedBombBlastProj : HenshinMoveProj
	{
		private const int Frames = 6;
		private const int TicksPerFrame = 4;
		private const int Life = Frames * TicksPerFrame;
		private static Asset<Texture2D> _tex;

		/// <summary>草绿爆炸色（HenshinExplosion / HenshinHitFeel 试点共用）。</summary>
		private static readonly Color BlastTint = new(120, 255, 110);

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 320;
			Projectile.height = 320;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			// 先亮后衰的爆炸光（HenshinExplosion 试点），光点随冲击环外扩。
			HenshinExplosion.Pulse(Projectile.Center, BlastTint, BlastSize.L, (Life - Projectile.timeLeft) / (float)Life, Projectile.width);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 大招命中手感（HenshinHitFeel 试点）：白闪 + 光脉冲 + 主人端震屏与表现顿帧（20 tick 窗口内 1 次）。本弹只由种子炸弹大招生成，直接写 Ultimate。
			HenshinHitFeel.OnHit(Projectile, target, HitTier.Ultimate, BlastTint);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_tex ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/SeedBomb/SeedBombBlast", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _tex.Value;
			int fh = tex.Height / Frames;
			int frame = Math.Clamp((Life - Projectile.timeLeft) / TicksPerFrame, 0, Frames - 1);
			Rectangle src = new(0, fh * frame, tex.Width, fh);
			// 帧 160 px → 世界直径 320：整数倍 2，像素不糊。自发光爆圈，夜里也读得出。
			Color c = new(Math.Max(lightColor.R, (byte)170), Math.Max(lightColor.G, (byte)200), Math.Max(lightColor.B, (byte)150));
			Main.EntitySpriteDraw(tex, (Projectile.Center - Main.screenPosition).Floor(), src, c, 0f,
				new Vector2(tex.Width * 0.5f, fh * 0.5f), 2f, SpriteEffects.None, 0f);
			// 爆炸配方（HenshinExplosion 试点）：白热核心 + 双冲击环 + 放射亮条，叠在手绘爆圈之上；草系不叠 Kenney 火烟帧。
			HenshinFxDraw.BeginAdditive();
			HenshinExplosion.Draw(Projectile.Center, BlastSize.L, BlastTint, (Life - Projectile.timeLeft) / (float)Life, Projectile.width, 0f, kenneyFrame: false);
			HenshinFxDraw.EndAdditive();
			return false;
		}
	}
}