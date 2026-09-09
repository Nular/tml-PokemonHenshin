using PokemonHenshin.Content.Items.Forms;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Loot
{
	public sealed class HenshinRecipes : ModSystem
	{
		public override void AddRecipes()
		{
			Recipe.Create(ModContent.ItemType<DiglettForce>())
				.AddIngredient(ItemID.GoldBar, 8)
				.AddIngredient(ItemID.DirtBlock, 50)
				.AddTile(TileID.Anvils)
				.Register();
			Recipe.Create(ModContent.ItemType<DiglettForce>())
				.AddIngredient(ItemID.PlatinumBar, 8)
				.AddIngredient(ItemID.DirtBlock, 50)
				.AddTile(TileID.Anvils)
				.Register();

			Recipe.Create(ModContent.ItemType<MagikarpForce>())
				.AddIngredient(ItemID.Bass, 5)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddTile(TileID.WorkBenches)
				.Register();

			Recipe.Create(ModContent.ItemType<GibleForce>())
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient(ItemID.FragmentSolar, 5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			Recipe.Create(ModContent.ItemType<LugiaForce>())
				.AddIngredient(ItemID.LunarBar, 15)
				.AddIngredient(ItemID.FragmentVortex, 8)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			Recipe.Create(ModContent.ItemType<RayquazaForce>())
				.AddIngredient(ItemID.LunarBar, 20)
				.AddIngredient(ItemID.FragmentNebula, 8)
				.AddIngredient(ItemID.FragmentStardust, 8)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
