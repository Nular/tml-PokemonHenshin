using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 重踏（地鼠技能2）：身前一排土浪沿地面依次掀起并向前推进，左右方向取鼠标所在侧，命中减速。
	/// 状态全由「出手方向（出生速度，随生成同步）+ 世界地形 + 弹幕年龄」推出，联机各端自行算同一排土浪，不依赖私有字段同步。
	/// 贴图 Assets/Fx/Bulldoze/Bulldoze.png：5 帧 × 3 tick，36×40/帧，原点 (18,38)。
	/// </summary>
	public class BulldozeProj : HenshinMoveProj
	{
		private const int Crests = 9;
		private const float Spacing = 30f;
		private const float FirstOffset = 30f;
		private const int Step = 3;
		private const int Frames = 5;
		private const int TicksPerFrame = 3;
		private const int CrestLife = Frames * TicksPerFrame;
		private const int Lifetime = Step * (Crests - 1) + CrestLife;
		private const int HitFrames = 4;
		private const int ClimbMax = 3;
		private const int DropMax = 10;

		private static Asset<Texture2D> _sheet;

		// 每 tick 由 AI 重算（只读世界 + 玩家位置），不是需要同步的状态。
		private readonly float[] _cx = new float[Crests];
		private readonly float[] _cy = new float[Crests];
		private int _n;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = HenshinDamage.Instance;
			Projectile.timeLeft = Lifetime;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Lifetime + 2;
			Projectile.ownerHitCheck = false;
		}

		// 位置每 tick 由 AI 直接写（起点锁定后只随 front 推进），不让速度再推一次。
		public override bool ShouldUpdatePosition() => false;

		private float Dir(Player owner)
		{
			// 出生速度 = 出手时指向鼠标的方向（随生成包同步），AI 不改它。
			float vx = Projectile.velocity.X;
			if (Math.Abs(vx) > 0.05f)
				return Math.Sign(vx);
			return owner.direction >= 0 ? 1f : -1f;
		}

		private static bool Solid(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 5))
				return false;
			Tile t = Framing.GetTileSafely(x, y);
			return t.HasTile && !t.IsActuated && Main.tileSolid[t.TileType];
		}

		/// <summary>沿地面逐段取表面：遇 1~3 格台阶爬上去，遇坑最多下探 10 格，再深或撞墙就止步。</summary>
		private void ComputeCrests(Player owner)
		{
			float dir = Dir(owner);
			float ox = Projectile.localAI[0]; // 首 tick 锁定的起点（见 AI），之后不再读 owner 位置
			int row = (int)(Projectile.localAI[1] / 16f);
			_n = 0;
			for (int i = 0; i < Crests; i++)
			{
				float x = ox + dir * (FirstOffset + i * Spacing);
				int tx = (int)(x / 16f);
				int r = row;
				if (Solid(tx, r))
				{
					int climbed = 0;
					while (Solid(tx, r - 1) && climbed < ClimbMax)
					{
						r--;
						climbed++;
					}
					if (Solid(tx, r - 1))
						break;
				}
				else
				{
					int dropped = 0;
					while (!Solid(tx, r) && dropped < DropMax)
					{
						r++;
						dropped++;
					}
					if (!Solid(tx, r))
						break;
				}
				row = r;
				_cx[i] = x;
				_cy[i] = r * 16f;
				_n++;
			}
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}

			int age = Lifetime - Projectile.timeLeft;
			if (Projectile.localAI[2] == 0f)
			{
				// 起点只取一次：出生中心 X（随生成包同步）+ 主人脚底 Y，之后土浪不再跟随主人。
				Projectile.localAI[0] = Projectile.Center.X;
				Projectile.localAI[1] = owner.Bottom.Y;
				Projectile.localAI[2] = 1f;
			}
			ComputeCrests(owner);
			if (age == 0)
				SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.35f, Volume = 0.9f }, owner.Center);

			int front = Math.Min(_n - 1, age / Step);
			if (front >= 0)
				Projectile.Center = new Vector2(_cx[front], _cy[front] - 20f);
			else
				Projectile.Center = new Vector2(Projectile.localAI[0], Projectile.localAI[1] - 20f);

			// 每道土浪破土那一刻掉一点土（粒子只做点缀）
			if (!Main.dedServ && age % Step == 0)
			{
				int i = age / Step;
				if (i < _n)
				{
					Dust d = Dust.NewDustPerfect(new Vector2(_cx[i], _cy[i] - 4f), DustID.Dirt,
						new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-2.4f, -1f)), 0, default, 1f);
					d.noGravity = false;
					if (i % 3 == 0)
						SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.2f, Volume = 0.35f }, new Vector2(_cx[i], _cy[i]));
				}
			}
		}

		private Rectangle CrestRect(int i) => new((int)_cx[i] - 18, (int)_cy[i] - 40, 36, 42);

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			int age = Lifetime - Projectile.timeLeft;
			for (int i = 0; i < _n; i++)
			{
				int t = age - i * Step;
				if (t < 0 || t >= HitFrames * TicksPerFrame)
					continue;
				if (CrestRect(i).Intersects(targetHitbox))
					return true;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 90);
			if (Main.dedServ)
				return;
			for (int i = 0; i < 2; i++)
			{
				Vector2 v = new(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-2.5f, -0.8f));
				Dust.NewDustPerfect(target.Center + new Vector2(Main.rand.NextFloat(-8f, 8f), target.height * 0.4f), i == 0 ? DustID.Dirt : DustID.Stone, v, 0, default, 1f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (_n <= 0)
				return false;
			_sheet ??= ModContent.Request<Texture2D>("PokemonHenshin/Assets/Fx/Bulldoze/Bulldoze", AssetRequestMode.ImmediateLoad);
			Texture2D tex = _sheet.Value;
			int frameH = tex.Height / Frames;
			int age = Lifetime - Projectile.timeLeft;
			Player owner = Main.player[Projectile.owner];
			SpriteEffects fx = Dir(owner) < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 origin = new(tex.Width * 0.5f, frameH - 2f);
			for (int i = 0; i < _n; i++)
			{
				int t = age - i * Step;
				if (t < 0 || t >= CrestLife)
					continue;
				int frame = Math.Clamp(t / TicksPerFrame, 0, Frames - 1);
				Rectangle src = new(0, frameH * frame, tex.Width, frameH);
				Vector2 pos = (new Vector2(_cx[i], _cy[i] + 2f) - Main.screenPosition).Floor();
				// 受光但抬亮暗处（夜里 / 洞里也读得出土浪）；末帧淡出。贴图已预乘，整色相乘即可。
				Color l = Lighting.GetColor((int)(_cx[i] / 16f), (int)((_cy[i] - 8f) / 16f));
				Color c = new(Math.Max(l.R, (byte)120), Math.Max(l.G, (byte)110), Math.Max(l.B, (byte)100));
				if (frame == Frames - 1)
					c *= 0.45f + 0.55f * (1f - (t - (Frames - 1) * TicksPerFrame) / (float)TicksPerFrame);
				Main.EntitySpriteDraw(tex, pos, src, c, 0f, origin, 1f, fx, 0f);
			}
			return false;
		}
	}
}
