using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Net;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PokemonHenshin.Content.Evolution
{
	/// <summary>简易进化确认层（不依赖外部 UI 库）。</summary>
	public sealed class EvolutionConfirmSystem : ModSystem
	{
		public static EvolutionConfirmSystem Instance { get; private set; }

		private bool visible;
		private int pendingSlot = -1;
		private bool pendingMouse;
		private FormDefinition fromForm;
		private FormDefinition toForm;

		public bool IsOpen => visible;

		public override void Load() => Instance = this;

		public override void Unload()
		{
			Instance = null;
			visible = false;
		}

		public void Open(int slot, bool isMouse, FormDefinition from, FormDefinition to)
		{
			if (from == null || to == null)
				return;
			pendingSlot = slot;
			pendingMouse = isMouse;
			fromForm = from;
			toForm = to;
			visible = true;
		}

		public void Close()
		{
			visible = false;
			pendingSlot = -1;
			pendingMouse = false;
			fromForm = null;
			toForm = null;
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (!visible || Main.netMode == NetmodeID.Server)
				return;

			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
			{
				Close();
				return;
			}

			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				Rectangle yes = YesRect();
				Rectangle no = NoRect();
				Point mouse = new(Main.mouseX, Main.mouseY);
				if (yes.Contains(mouse))
				{
					Confirm();
					return;
				}
				if (no.Contains(mouse))
				{
					Main.LocalPlayer.GetModPlayer<EvolutionOfferPlayer>().NotifyCancelled();
					Close();
					return;
				}
			}

			// Esc 取消
			if (PlayerInput.Triggers.JustPressed.Inventory)
			{
				// 不抢背包键；仅检测 Escape 风格：用取消键
			}
		}

		private void Confirm()
		{
			HenshinNet.RequestEvolve(pendingSlot, pendingMouse);
			Close();
		}

		public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
			if (index < 0)
				index = layers.Count;
			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PokemonHenshin: EvolutionConfirm",
				delegate
				{
					if (visible)
						Draw();
					return true;
				},
				InterfaceScaleType.UI));
		}

		private static Rectangle PanelRect()
		{
			int w = 360;
			int h = 160;
			return new Rectangle((Main.screenWidth - w) / 2, (Main.screenHeight - h) / 2, w, h);
		}

		private static Rectangle YesRect()
		{
			Rectangle p = PanelRect();
			return new Rectangle(p.X + 40, p.Bottom - 48, 120, 32);
		}

		private static Rectangle NoRect()
		{
			Rectangle p = PanelRect();
			return new Rectangle(p.Right - 160, p.Bottom - 48, 120, 32);
		}

		private void Draw()
		{
			Main.LocalPlayer.mouseInterface = true;
			Rectangle panel = PanelRect();
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, panel, Color.Black * 0.75f);

			string title = Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmTitle");
			string body = Language.GetTextValue(
				"Mods.PokemonHenshin.Evolution.ConfirmBody",
				Language.GetTextValue(fromForm.DisplayNameKey),
				Language.GetTextValue(toForm.DisplayNameKey));

			DynamicSpriteFont font = FontAssets.MouseText.Value;
			Vector2 titleSize = font.MeasureString(title);
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, title,
				new Vector2(panel.X + (panel.Width - titleSize.X) / 2f, panel.Y + 16f), Color.Gold, 0f, Vector2.Zero, Vector2.One);

			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, body,
				new Vector2(panel.X + 24f, panel.Y + 56f), Color.White, 0f, Vector2.Zero, Vector2.One);

			DrawButton(YesRect(), Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmYes"), Color.LimeGreen);
			DrawButton(NoRect(), Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmNo"), Color.OrangeRed);
		}

		private static void DrawButton(Rectangle rect, string text, Color tint)
		{
			bool hover = rect.Contains(Main.mouseX, Main.mouseY);
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, (hover ? tint : tint * 0.7f) * 0.85f);
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			Vector2 size = font.MeasureString(text);
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text,
				new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f),
				Color.White, 0f, Vector2.Zero, Vector2.One);
		}
	}

	/// <summary>每 tick 检测可进化并弹出确认（本地玩家）。</summary>
	public sealed class EvolutionOfferPlayer : ModPlayer
	{
		private int cooldown;

		public override void PostUpdate()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Player.whoAmI != Main.myPlayer)
				return;
			if (EvolutionConfirmSystem.Instance == null || EvolutionConfirmSystem.Instance.IsOpen)
				return;

			if (cooldown > 0)
			{
				cooldown--;
				return;
			}

			if (!EvolutionService.TryFindEvolvable(Player, out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next))
			{
				return;
			}

			// 冷却期内不重复弹；取消后冷却结束可再弹。
			cooldown = 180;
			EvolutionConfirmSystem.Instance.Open(slot, isMouse, current, next);
		}

		public void NotifyEvolved()
		{
			cooldown = 30;
		}

		public void NotifyCancelled()
		{
			cooldown = 300; // 取消后 5 秒再提示
		}
	}
}
