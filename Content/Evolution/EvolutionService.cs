using PokemonHenshin.Content.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Evolution
{
	/// <summary>
	/// 进化替换契约（需求 §4.3 / dev-plan §4.5）：前缀+收藏继承；仅背包/热键栏/鼠标。
	/// </summary>
	public static class EvolutionService
	{
		/// <summary>主背包 0..49 + mouseItem（排除钱币/弹药/银行等）。</summary>
		public static bool CanAutoEvolveLocation(Player player, int inventorySlot, bool isMouseItem)
		{
			if (isMouseItem)
				return true;
			// inventory 0..49 = 热键栏+主背包；50+ 为钱币/弹药等。
			return inventorySlot >= 0 && inventorySlot < 50;
		}

		public static bool MeetsTrigger(Player player, FormDefinition current)
		{
			if (current == null)
				return false;
			FormDefinition next = FormRegistry.FindEvolutionOf(current.FormId);
			if (next == null)
				return false;
			return ProgressStageService.MeetsStage(next.Stage);
		}

		public static FormDefinition GetNextForm(FormDefinition current)
			=> current == null ? null : FormRegistry.FindEvolutionOf(current.FormId);

		/// <summary>就地替换物品类型，保留 prefix / favorited / stack。</summary>
		public static bool TryReplace(Item item, int newItemType)
		{
			if (item == null || item.IsAir || newItemType <= 0)
				return false;

			int prefix = item.prefix;
			bool favorited = item.favorited;
			int stack = item.stack;

			item.SetDefaults(newItemType);
			if (prefix > 0)
				item.Prefix(prefix);
			item.favorited = favorited;
			item.stack = stack;
			return true;
		}

		/// <summary>解析玩家身上可进化的槽位（优先热键栏选中，再扫背包与鼠标）。</summary>
		public static bool TryFindEvolvable(Player player, out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next)
		{
			slot = -1;
			isMouse = false;
			current = null;
			next = null;

			if (player == null || !player.active)
				return false;

			// 优先当前选中热键栏。
			int selected = player.selectedItem;
			if (selected >= 0 && selected < HenshinPlayerHotbar.Size)
			{
				if (TrySlot(player.inventory[selected], selected, false, out current, out next))
				{
					slot = selected;
					return true;
				}
			}

			if (!Main.mouseItem.IsAir && TrySlot(Main.mouseItem, -1, true, out current, out next))
			{
				isMouse = true;
				return true;
			}

			for (int i = 0; i < 50; i++)
			{
				if (i == selected)
					continue;
				if (TrySlot(player.inventory[i], i, false, out current, out next))
				{
					slot = i;
					return true;
				}
			}

			return false;
		}

		private static bool TrySlot(Item item, int slot, bool isMouse, out FormDefinition current, out FormDefinition next)
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
			if (!ProgressStageService.MeetsStage(next.Stage))
				return false;
			return true;
		}

		public static Item GetItemRef(Player player, int slot, bool isMouse)
			=> isMouse ? Main.mouseItem : player.inventory[slot];
	}

	/// <summary>避免 PlayerState 循环引用的热键栏常量。</summary>
	internal static class HenshinPlayerHotbar
	{
		public const int Size = 10;
	}
}
