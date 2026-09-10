using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Evolution;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.Visual;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Consumables
{
	/// <summary>
	/// 神奇糖果：使用后令物品栏第一格（<c>inventory[0]</c>）的之力 +1 级、XP 清零。
	/// 遵守世界档硬顶与 100 级上限。
	/// </summary>
	public sealed class RareCandy : ModItem
	{
		public const string GoldCritterGroup = "PokemonHenshin:GoldCritter";
		public const int TargetSlot = 0;
		public const float BossDropChance = 0.05f;

		private static uint lastFailTick;

		public override string Texture => "PokemonHenshin/Assets/Items/RareCandy";

		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.UseSound = SoundID.Item4;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.sellPrice(gold: 2);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup(GoldCritterGroup, 1)
				.AddTile(TileID.CookingPots)
				.Register();
		}

		public override bool CanUseItem(Player player)
		{
			if (CanApply(player, out string failKey))
				return true;

			if (player != null && player.whoAmI == Main.myPlayer && !Main.dedServ
				&& player.controlUseItem)
			{
				uint now = Main.GameUpdateCount;
				if (now - lastFailTick >= 60)
				{
					lastFailTick = now;
					Main.NewText(Language.GetTextValue(failKey), Color.Orange);
				}
			}

			return false;
		}

		public override bool? UseItem(Player player)
		{
			if (player == null)
				return false;

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				HenshinNet.RequestRareCandy();
				return true;
			}

			if (Main.netMode == NetmodeID.Server && player.whoAmI != Main.myPlayer)
				return true;

			if (!TryApply(player, out _, out _, out int gained, out HenshinForceItem force))
				return false;

			NotifyLocal(player, force, gained);
			return true;
		}

		public static bool CanApply(Player player, out string failKey)
		{
			failKey = "Mods.PokemonHenshin.RareCandy.NoForce";
			if (player?.inventory == null || TargetSlot >= player.inventory.Length)
				return false;

			Item first = player.inventory[TargetSlot];
			if (first == null || first.IsAir || first.ModItem is not HenshinForceItem force)
				return false;

			force.InitializeNewIfNeeded();
			if (force.Level >= HenshinStatService.MaxLevel)
			{
				failKey = "Mods.PokemonHenshin.RareCandy.MaxLevel";
				return false;
			}

			int cap = HenshinStatService.LevelCap(SafeWorldStage());
			if (force.Level >= cap)
			{
				failKey = "Mods.PokemonHenshin.RareCandy.Capped";
				return false;
			}

			failKey = null;
			return true;
		}

		public static bool TryApply(Player player, out int level, out int xp, out int gained, out HenshinForceItem force)
		{
			level = 0;
			xp = 0;
			gained = 0;
			force = null;
			if (!CanApply(player, out _))
				return false;

			force = player.inventory[TargetSlot].ModItem as HenshinForceItem;
			if (force == null)
				return false;

			int oldLevel = force.Level;
			force.SetProgress(oldLevel + 1, 0);
			level = force.Level;
			xp = force.Xp;
			gained = level - oldLevel;
			return gained > 0;
		}

		public static void NotifyLocal(Player player, HenshinForceItem force, int gained)
		{
			if (player == null || player.whoAmI != Main.myPlayer || Main.dedServ)
				return;
			if (gained > 0)
				HenshinXpPopupSystem.ShowLevelUps(player, gained);
			if (force != null)
				player.GetModPlayer<EvolutionOfferPlayer>().TryOfferAfterLevelUp(force);
		}

		private static int SafeWorldStage()
		{
			try
			{
				return ProgressStageService.GetProgressStage();
			}
			catch
			{
				return 1;
			}
		}
	}
}
