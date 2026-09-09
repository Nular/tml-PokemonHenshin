using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>
	/// 击杀/升级世界字：<c>EXP +X</c> 钉在击杀坐标；<c>LEVEL UP!</c> 跟玩家头顶（连升连弹）。
	/// 画法对齐睡眠 zzZ（MouseText + 描边），不绑 buff、不绑已死 NPC。
	/// </summary>
	public sealed class HenshinXpPopupSystem : ModSystem
	{
		private enum Kind : byte
		{
			Exp = 0,
			BossExp = 1,
			LevelUp = 2
		}

		private struct Popup
		{
			public Kind Kind;
			public Vector2 WorldPos;
			public int FollowPlayer;
			public string Text;
			public int Delay;
			public int Age;
			public int Life;
			public float BaseScale;
			public int StackIndex;
		}

		private const int MaxPopups = 48;
		private const int ExpLife = 90;
		private const int BossLife = 132;
		private const int LevelLife = 108;
		private const int LevelStagger = 22;

		private static readonly List<Popup> popups = new();

		public override void Unload() => popups.Clear();

		public static void ShowExp(Vector2 worldPos, int amount, bool boss)
		{
			if (Main.dedServ || amount <= 0)
				return;
			Push(new Popup
			{
				Kind = boss ? Kind.BossExp : Kind.Exp,
				WorldPos = worldPos,
				FollowPlayer = -1,
				Text = "EXP +" + amount,
				Delay = 0,
				Age = 0,
				Life = boss ? BossLife : ExpLife,
				BaseScale = boss ? 1.55f : 0.95f,
				StackIndex = 0
			});
		}

		public static void ShowLevelUps(Player player, int count)
		{
			if (Main.dedServ || player == null || count <= 0)
				return;
			int n = Math.Min(count, 24);
			for (int i = 0; i < n; i++)
			{
				Push(new Popup
				{
					Kind = Kind.LevelUp,
					WorldPos = player.Top + new Vector2(0f, -10f),
					FollowPlayer = player.whoAmI,
					Text = "LEVEL UP!",
					Delay = i * LevelStagger,
					Age = 0,
					Life = LevelLife,
					BaseScale = 1.25f,
					StackIndex = i
				});
			}
		}

		private static void Push(Popup p)
		{
			if (popups.Count >= MaxPopups)
				popups.RemoveAt(0);
			popups.Add(p);
		}

		public override void PostUpdateEverything()
		{
			if (Main.dedServ)
			{
				popups.Clear();
				return;
			}

			for (int i = popups.Count - 1; i >= 0; i--)
			{
				Popup p = popups[i];
				if (p.Delay > 0)
					p.Delay--;
				else
					p.Age++;

				if (p.FollowPlayer >= 0 && p.FollowPlayer < Main.maxPlayers)
				{
					Player pl = Main.player[p.FollowPlayer];
					if (pl != null && pl.active)
						p.WorldPos = pl.Top + new Vector2(0f, -10f - p.StackIndex * 14f + pl.gfxOffY);
				}

				if (p.Age >= p.Life)
					popups.RemoveAt(i);
				else
					popups[i] = p;
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (index < 0)
				index = layers.Count;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PokemonHenshin: XpPopups",
				delegate
				{
					DrawAll(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.Game));
		}

		private static void DrawAll(SpriteBatch spriteBatch)
		{
			if (popups.Count == 0)
				return;
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			if (font == null)
				return;

			for (int i = 0; i < popups.Count; i++)
			{
				Popup p = popups[i];
				if (p.Delay > 0)
					continue;
				DrawOne(spriteBatch, font, p);
			}
		}

		private static void DrawOne(SpriteBatch spriteBatch, DynamicSpriteFont font, Popup p)
		{
			float t = p.Age / (float)Math.Max(1, p.Life);
			float rise = t * (p.Kind == Kind.BossExp ? 56f : 38f);
			float fadeIn = t < 0.08f ? t / 0.08f : 1f;
			float fadeOut = t < 0.45f ? 1f : MathHelper.Clamp(1f - (t - 0.45f) / 0.55f, 0f, 1f);
			float alpha = MathHelper.Clamp(fadeIn * fadeOut, 0f, 1f);
			float scale = p.BaseScale * MathHelper.Lerp(0.85f, 1.12f, MathHelper.Clamp(t * 2.2f, 0f, 1f));

			Color fill;
			Color outline;
			int outlineR = 1;
			if (p.Kind == Kind.BossExp)
			{
				float pulse = 0.5f + 0.5f * MathF.Sin(p.Age * 0.55f);
				scale *= 1f + 0.14f * pulse;
				fill = Color.Lerp(new Color(255, 230, 120), Color.White, pulse);
				outline = Color.Lerp(new Color(180, 90, 20), new Color(255, 200, 60), pulse);
				outlineR = 2;
			}
			else if (p.Kind == Kind.LevelUp)
			{
				fill = new Color(255, 220, 80);
				outline = new Color(120, 60, 10);
			}
			else
			{
				fill = new Color(120, 220, 255);
				outline = new Color(20, 50, 90);
			}

			fill *= alpha;
			outline *= alpha * 0.9f;

			Vector2 pos = p.WorldPos + new Vector2(0f, -rise) - Main.screenPosition;
			Vector2 size = font.MeasureString(p.Text);
			Vector2 origin = size * 0.5f * scale;
			Vector2 draw = pos - origin;

			for (int ox = -outlineR; ox <= outlineR; ox++)
			{
				for (int oy = -outlineR; oy <= outlineR; oy++)
				{
					if (ox == 0 && oy == 0)
						continue;
					if (outlineR > 1 && Math.Abs(ox) == outlineR && Math.Abs(oy) == outlineR)
						continue;
					DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, p.Text,
						draw + new Vector2(ox, oy), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				}
			}

			DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, p.Text,
				draw, fill, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}
}
