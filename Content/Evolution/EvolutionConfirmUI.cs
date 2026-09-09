using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PokemonHenshin.Content.Evolution
{
	/// <summary>进化确认：UserInterface + UIState（可点确认/取消，Esc/背包视同取消）。</summary>
	[Autoload(Side = ModSide.Client)]
	public sealed class EvolutionConfirmSystem : ModSystem
	{
		public static EvolutionConfirmSystem Instance { get; private set; }

		private UserInterface userInterface;
		private EvolutionConfirmState uiState;

		private int pendingSlot = -1;
		private bool pendingMouse;

		public bool IsOpen => userInterface?.CurrentState != null;

		public override void Load()
		{
			Instance = this;
			if (Main.dedServ)
				return;

			uiState = new EvolutionConfirmState();
			uiState.Activate();
			userInterface = new UserInterface();
		}

		public override void Unload()
		{
			Instance = null;
			userInterface = null;
			uiState = null;
			ClearPending();
		}

		public void Open(int slot, bool isMouse, FormDefinition from, FormDefinition to)
		{
			if (from == null || to == null || userInterface == null || uiState == null)
				return;

			pendingSlot = slot;
			pendingMouse = isMouse;
			uiState.Refresh(from, to);
			userInterface.SetState(uiState);
		}

		public void Close()
		{
			userInterface?.SetState(null);
			ClearPending();
		}

		private void ClearPending()
		{
			pendingSlot = -1;
			pendingMouse = false;
		}

		internal void ConfirmFromUI()
		{
			HenshinNet.RequestEvolve(pendingSlot, pendingMouse);
			Close();
		}

		internal void CancelFromUI()
		{
			Main.LocalPlayer.GetModPlayer<EvolutionOfferPlayer>().NotifyCancelled();
			Close();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (userInterface?.CurrentState == null)
				return;

			if (Main.netMode == NetmodeID.Server)
			{
				Close();
				return;
			}

			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
			{
				Close();
				return;
			}

			// Esc / 背包键：视同取消，避免软锁
			if (PlayerInput.Triggers.JustPressed.Inventory)
			{
				CancelFromUI();
				return;
			}

			userInterface.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (index < 0)
				index = layers.Count;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PokemonHenshin: EvolutionConfirm",
				delegate
				{
					if (userInterface?.CurrentState != null)
						userInterface.Draw(Main.spriteBatch, new GameTime());
					return true;
				},
				InterfaceScaleType.UI));
		}
	}

	public sealed class EvolutionConfirmState : UIState
	{
		private UIPanel panel;
		private UIText titleText;
		private UIText bodyText;
		private UITextPanel<string> yesButton;
		private UITextPanel<string> noButton;

		public override void OnInitialize()
		{
			panel = new UIPanel();
			panel.Width.Set(400f, 0f);
			panel.Height.Set(180f, 0f);
			panel.HAlign = 0.5f;
			panel.VAlign = 0.5f;
			panel.BackgroundColor = new Color(30, 30, 40) * 0.92f;
			panel.BorderColor = new Color(90, 90, 110);
			panel.OnUpdate += Panel_OnUpdate;
			Append(panel);

			titleText = new UIText("", 1.1f);
			titleText.HAlign = 0.5f;
			titleText.Top.Set(12f, 0f);
			titleText.TextColor = Color.Gold;
			panel.Append(titleText);

			bodyText = new UIText("", 0.9f)
			{
				IsWrapped = true,
			};
			bodyText.Width.Set(0f, 1f);
			bodyText.Top.Set(48f, 0f);
			bodyText.HAlign = 0.5f;
			bodyText.TextColor = Color.White;
			panel.Append(bodyText);

			yesButton = new UITextPanel<string>("", 0.9f, true);
			yesButton.Width.Set(150f, 0f);
			yesButton.Height.Set(36f, 0f);
			yesButton.Left.Set(24f, 0f);
			yesButton.Top.Set(120f, 0f);
			yesButton.BackgroundColor = new Color(40, 140, 60) * 0.9f;
			yesButton.OnLeftClick += YesClicked;
			yesButton.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
			panel.Append(yesButton);

			noButton = new UITextPanel<string>("", 0.9f, true);
			noButton.Width.Set(150f, 0f);
			noButton.Height.Set(36f, 0f);
			noButton.Left.Set(226f, 0f);
			noButton.Top.Set(120f, 0f);
			noButton.BackgroundColor = new Color(160, 60, 50) * 0.9f;
			noButton.OnLeftClick += NoClicked;
			noButton.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
			panel.Append(noButton);
		}

		public void Refresh(FormDefinition from, FormDefinition to)
		{
			titleText.SetText(Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmTitle"));
			bodyText.SetText(Language.GetTextValue(
				"Mods.PokemonHenshin.Evolution.ConfirmBody",
				Language.GetTextValue(from.DisplayNameKey),
				Language.GetTextValue(to.DisplayNameKey)));
			yesButton.SetText(Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmYes"));
			noButton.SetText(Language.GetTextValue("Mods.PokemonHenshin.Evolution.ConfirmNo"));
			Recalculate();
		}

		private static void Panel_OnUpdate(UIElement affectedElement)
		{
			if (affectedElement.ContainsPoint(Main.MouseScreen))
				Main.LocalPlayer.mouseInterface = true;
		}

		private void YesClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			SoundEngine.PlaySound(SoundID.MenuOpen);
			EvolutionConfirmSystem.Instance?.ConfirmFromUI();
		}

		private void NoClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			EvolutionConfirmSystem.Instance?.CancelFromUI();
		}
	}

	/// <summary>进度档提升（击败对应 Boss 等）时为本机玩家弹出进化确认。</summary>
	public sealed class EvolutionOfferPlayer : ModPlayer
	{
		/// <summary>
		/// 档位从 <paramref name="previousStage"/> 升到 <paramref name="newStage"/> 后调用。
		/// 仅提示「下一形态 Stage 落在 (previous, new]」的进化，避免把更早就能进化的漏网项反复弹。
		/// </summary>
		public static void TryOfferLocalAfterStageUp(int previousStage, int newStage)
		{
			if (Main.dedServ)
				return;
			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;
			player.GetModPlayer<EvolutionOfferPlayer>().TryOffer(previousStage, newStage);
		}

		public void TryOffer(int previousStage, int newStage)
		{
			if (Player.whoAmI != Main.myPlayer)
				return;
			if (Player.GetModPlayer<HenshinPlayer>().EverstoneBlock)
				return;
			if (EvolutionConfirmSystem.Instance == null || EvolutionConfirmSystem.Instance.IsOpen)
				return;

			if (!TryFindNewlyUnlocked(Player, previousStage, newStage, out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next))
				return;

			EvolutionConfirmSystem.Instance.Open(slot, isMouse, current, next);
		}

		public void TryOfferAfterLevelUp(HenshinForceItem force)
		{
			if (Player.whoAmI != Main.myPlayer)
				return;
			if (Player.GetModPlayer<HenshinPlayer>().EverstoneBlock)
				return;
			if (EvolutionConfirmSystem.Instance == null || EvolutionConfirmSystem.Instance.IsOpen)
				return;
			if (force == null)
				return;

			if (!TryFindForceSlot(Player, force, out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next))
				return;
			if (!EvolutionService.MeetsTrigger(Player, current, EvolutionService.GetItemRef(Player, slot, isMouse)))
				return;

			EvolutionConfirmSystem.Instance.Open(slot, isMouse, current, next);
		}

		private static bool TryFindForceSlot(
			Player player, HenshinForceItem force,
			out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next)
		{
			slot = -1;
			isMouse = false;
			current = force?.Definition;
			next = current == null ? null : FormRegistry.FindEvolutionOf(current.FormId);
			if (player == null || force == null || current == null || next == null)
				return false;

			if (!Main.mouseItem.IsAir && Main.mouseItem.ModItem == force)
			{
				isMouse = true;
				return EvolutionService.CanAutoEvolveLocation(player, -1, true);
			}

			for (int i = 0; i < 50; i++)
			{
				if (player.inventory[i]?.ModItem != force)
					continue;
				if (!EvolutionService.CanAutoEvolveLocation(player, i, false))
					return false;
				slot = i;
				return true;
			}

			return false;
		}

		/// <summary>下一形态 Stage 落在 (previousStage, newStage] 且物品在可自动进化位置。</summary>
		private static bool TryFindNewlyUnlocked(
			Player player, int previousStage, int newStage,
			out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next)
		{
			slot = -1;
			isMouse = false;
			current = null;
			next = null;

			if (player == null || !player.active)
				return false;

			int selected = player.selectedItem;
			if (selected >= 0 && selected < HenshinPlayerHotbar.Size)
			{
				if (TryNewlyUnlockedSlot(player, player.inventory[selected], previousStage, newStage, out current, out next))
				{
					slot = selected;
					return true;
				}
			}

			if (!Main.mouseItem.IsAir && TryNewlyUnlockedSlot(player, Main.mouseItem, previousStage, newStage, out current, out next))
			{
				isMouse = true;
				return true;
			}

			for (int i = 0; i < 50; i++)
			{
				if (i == selected)
					continue;
				if (TryNewlyUnlockedSlot(player, player.inventory[i], previousStage, newStage, out current, out next))
				{
					slot = i;
					return true;
				}
			}

			return false;
		}

		private static bool TryNewlyUnlockedSlot(
			Player player, Item item, int previousStage, int newStage,
			out FormDefinition current, out FormDefinition next)
		{
			current = null;
			next = null;
			if (item == null || item.IsAir)
				return false;
			current = FormRegistry.ByItemType(item.type);
			if (current == null)
				return false;
			next = FormRegistry.FindEvolutionOf(current.FormId);
			if (next == null)
				return false;
			// 本档刚解锁：previous < next.Stage <= new
			if (next.Stage <= previousStage || next.Stage > newStage)
				return false;
			return EvolutionService.MeetsTrigger(player, current, item);
		}

		public void NotifyEvolved()
		{
		}

		public void NotifyCancelled()
		{
		}
	}
}
