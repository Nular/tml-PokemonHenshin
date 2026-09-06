using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Buffs;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.NPCs
{
	/// <summary>
	/// 睡眠减益消费者：PreAI 停原版 AI、定身、禁接触伤、首伤×2 后清 buff。
	/// 绘制：DrawEffects 强制白 (255,255,255,100) + 错落 z/Z。
	/// </summary>
	public sealed class HenshinNpcGlobalNPC : GlobalNPC
	{
		private const int ZzCount = 3;
		private const float ZzCycle = 50f; // ticks per letter cycle (~0.83s)

		public override bool InstancePerEntity => true;

		/// <summary>本帧减益 Update 点亮，ResetEffects 清。</summary>
		public bool SleepActive;

		private bool _frozen;
		private Vector2 _freezeCenter;
		private bool _wakePending;

		public override void ResetEffects(NPC npc)
		{
			SleepActive = false;
		}

		private bool IsAsleep(NPC npc) => npc.HasBuff<HenshinSleepDebuff>() || SleepActive;

		public override bool PreAI(NPC npc)
		{
			if (!IsAsleep(npc))
			{
				_frozen = false;
				return true;
			}

			if (!_frozen)
			{
				_frozen = true;
				_freezeCenter = npc.Center;
			}

			npc.velocity = Vector2.Zero;
			npc.Center = _freezeCenter;
			return false;
		}

		public override void PostAI(NPC npc)
		{
			if (!IsAsleep(npc))
				return;
			npc.velocity = Vector2.Zero;
			npc.Center = _freezeCenter;
		}

		public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
		{
			if (IsAsleep(npc))
				return false;
			return true;
		}

		public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
		{
			if (!IsAsleep(npc))
				return;
			modifiers.FinalDamage *= 2f;
			_wakePending = true;
		}

		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
			=> TryWake(npc);

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
			=> TryWake(npc);

		private void TryWake(NPC npc)
		{
			if (!_wakePending)
				return;
			_wakePending = false;
			_frozen = false;
			int idx = npc.FindBuffIndex(ModContent.BuffType<HenshinSleepDebuff>());
			if (idx >= 0)
				npc.DelBuff(idx);
			SleepActive = false;
		}

		/// <summary>睡眠本体色：直接指定白半透明，不再叠 PostDraw 克隆贴图。</summary>
		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (!IsAsleep(npc))
				return;
			drawColor = new Color(255, 255, 255, 100);
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!IsAsleep(npc) || Main.dedServ)
				return;
			DrawZzZ(npc, spriteBatch, screenPos);
		}

		private static void DrawZzZ(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos)
		{
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			if (font == null)
				return;

			Vector2 head = npc.Top + new Vector2(0f, -6f);
			float time = (float)Main.GameUpdateCount + npc.whoAmI * 17f;
			string[] glyphs = { "z", "Z", "Zz" };

			for (int i = 0; i < ZzCount; i++)
			{
				float phase = (time + i * (ZzCycle / ZzCount)) % ZzCycle;
				float t = phase / ZzCycle; // 0→1
				float rise = t * 36f; // ~2.25 格
				float scale = MathHelper.Lerp(0.55f, 1.55f, t);
				float alpha = t < 0.15f ? t / 0.15f : (t > 0.7f ? (1f - t) / 0.3f : 1f);
				alpha = MathHelper.Clamp(alpha, 0f, 1f);

				float side = (i - 1) * 10f + MathF.Sin(time * 0.07f + i) * 3f;
				Vector2 pos = head + new Vector2(side + i * 4f, -rise) - screenPos;

				string text = glyphs[i % glyphs.Length];
				Vector2 size = font.MeasureString(text);
				Color outline = new Color(80, 40, 120, (byte)(200 * alpha));
				Color fill = new Color(255, 255, 255, (byte)(255 * alpha));

				for (int ox = -1; ox <= 1; ox++)
				{
					for (int oy = -1; oy <= 1; oy++)
					{
						if (ox == 0 && oy == 0)
							continue;
						DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, text,
							pos + new Vector2(ox, oy) - size * 0.5f * scale, outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
					}
				}
				DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, text,
					pos - size * 0.5f * scale, fill, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
		}
	}
}
