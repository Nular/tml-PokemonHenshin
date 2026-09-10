using System.Collections.Generic;
using PokemonHenshin.Content.Items.Forms;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PokemonHenshin.Content.PlayerState
{
	/// <summary>
	/// 开局发放御三家各 1。标识绑在角色档上（与大修任务书 <c>QuestLogBookPlayer.Change</c> 同类）。
	/// 不走 <see cref="ModPlayer.OnEnterWorld"/>：该钩子仅本地客户端，联机客户端再早退则专用服上谁都不发。
	/// 建角走 <see cref="AddStartingItems"/>（铜镐同款，物品已在 .plr 里再进服）；
	/// 旧角 / 建角时模组未加载则在 <see cref="PostUpdateMiscEffects"/> 补发，直接入背包。
	/// </summary>
	public sealed class StarterGrantPlayer : ModPlayer
	{
		private bool granted;

		public override void Initialize()
		{
			granted = false;
		}

		public override void SaveData(TagCompound tag)
		{
			if (granted)
				tag["starterGranted"] = true;
		}

		public override void LoadData(TagCompound tag)
		{
			granted = tag.ContainsKey("starterGranted") && tag.GetBool("starterGranted");
		}

		public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
		{
			if (mediumCoreDeath)
				yield break;

			granted = true;
			yield return NewStarter(ModContent.ItemType<CharmanderForce>());
			yield return NewStarter(ModContent.ItemType<SquirtleForce>());
			yield return NewStarter(ModContent.ItemType<BulbasaurForce>());
		}

		public override void PostUpdateMiscEffects()
		{
			if (granted || Player.whoAmI != Main.myPlayer)
				return;
			if (Player.inventory == null)
				return;

			GiveIfMissing(ModContent.ItemType<CharmanderForce>());
			GiveIfMissing(ModContent.ItemType<SquirtleForce>());
			GiveIfMissing(ModContent.ItemType<BulbasaurForce>());
			granted = true;
		}

		private void GiveIfMissing(int type)
		{
			if (HasItem(type))
				return;

			Item gift = NewStarter(type);
			gift.position = Player.Center;
			Item overflow = Player.GetItem(Player.whoAmI, gift, GetItemSettings.NPCEntityToPlayerInventorySettings);
			if (!overflow.IsAir && overflow.stack > 0)
				Player.QuickSpawnItem(Player.GetSource_GiftOrReward("PokemonHenshinStarter"), overflow, overflow.stack);
		}

		private bool HasItem(int type)
		{
			Item[] inv = Player.inventory;
			for (int i = 0; i < inv.Length; i++)
			{
				Item item = inv[i];
				if (item != null && item.type == type && item.stack > 0)
					return true;
			}

			return false;
		}

		private static Item NewStarter(int type)
		{
			var item = new Item();
			item.SetDefaults(type);
			return item;
		}
	}
}
