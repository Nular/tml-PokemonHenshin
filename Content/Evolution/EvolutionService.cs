using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Evolution
{
	/// <summary>
	/// 进化替换契约（需求 §4.3）：前缀+收藏+Level/Xp 继承；仅背包/热键栏/鼠标。
	/// 触发须 ProgressStage 与 Level>=BandMin[next.Stage] 同时满足。
	/// </summary>
	public static class EvolutionService
	{
		/// <summary>主背包 0..49 + mouseItem（排除钱币/弹药/银行等）。</summary>
		public static bool CanAutoEvolveLocation(Player player, int inventorySlot, bool isMouseItem)
		{
			if (isMouseItem)
				return true;
			return inventorySlot >= 0 && inventorySlot < 50;
		}

		public static bool MeetsTrigger(Player player, FormDefinition current, Item item)
		{
			if (player?.GetModPlayer<HenshinPlayer>().EverstoneBlock == true)
				return false;
			if (current == null)
				return false;
			FormDefinition next = FormRegistry.FindEvolutionOf(current.FormId);
			if (next == null)
				return false;
			int level = HenshinStatService.MinLevel;
			if (item?.ModItem is HenshinForceItem force)
			{
				force.InitializeNewIfNeeded();
				level = force.Level;
			}

			int world = ProgressStageService.GetProgressStage();
			return HenshinStatService.MeetsEvolution(world, level, next.Stage);
		}

		public static FormDefinition GetNextForm(FormDefinition current)
			=> current == null ? null : FormRegistry.FindEvolutionOf(current.FormId);

		/// <summary>就地替换物品类型，保留 prefix / favorited / stack / Level / Xp。</summary>
		public static bool TryReplace(Item item, int newItemType)
		{
			if (item == null || item.IsAir || newItemType <= 0)
				return false;

			int prefix = item.prefix;
			bool favorited = item.favorited;
			int stack = item.stack;
			int level = HenshinStatService.MinLevel;
			int xp = 0;
			if (item.ModItem is HenshinForceItem src)
			{
				src.InitializeNewIfNeeded();
				level = src.Level;
				xp = src.Xp;
			}

			item.SetDefaults(newItemType);
			if (prefix > 0)
				item.Prefix(prefix);
			item.favorited = favorited;
			item.stack = stack;
			if (item.ModItem is HenshinForceItem dst)
				dst.SetProgress(level, xp);
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

			int selected = player.selectedItem;
			if (selected >= 0 && selected < HenshinPlayerHotbar.Size)
			{
				if (TrySlot(player, player.inventory[selected], selected, false, out current, out next))
				{
					slot = selected;
					return true;
				}
			}

			if (!Main.mouseItem.IsAir && TrySlot(player, Main.mouseItem, -1, true, out current, out next))
			{
				isMouse = true;
				return true;
			}

			for (int i = 0; i < 50; i++)
			{
				if (i == selected)
					continue;
				if (TrySlot(player, player.inventory[i], i, false, out current, out next))
				{
					slot = i;
					return true;
				}
			}

			return false;
		}

		private static bool TrySlot(Player player, Item item, int slot, bool isMouse, out FormDefinition current, out FormDefinition next)
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
			return MeetsTrigger(player, current, item);
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
