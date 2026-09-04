using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Items.Accessories;
using PokemonHenshin.Content.Items.Forms;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Loot
{
	/// <summary>Boss 掉落占位：每档至少一条非创造获取。</summary>
	public sealed class HenshinLootGlobalNPC : GlobalNPC
	{
		public override void OnKill(NPC npc)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (npc.boss)
				TryBossDrop(npc);
		}

		private static void TryBossDrop(NPC npc)
		{
			// 克眼 → 皮卡丘
			if (npc.type == NPCID.EyeofCthulhu)
				Drop(npc, ModContent.ItemType<PikachuForce>(), 0.35f);
			// 蜂后 → 腕力
			else if (npc.type == NPCID.QueenBee)
				Drop(npc, ModContent.ItemType<MachopForce>(), 0.35f);
			// 骷髅王 → 波波 / 凯西
			else if (npc.type == NPCID.SkeletronHead)
			{
				Drop(npc, ModContent.ItemType<PidgeyForce>(), 0.30f);
				Drop(npc, ModContent.ItemType<AbraForce>(), 0.30f);
			}
			// 肉山 → 雷丘线入口已由进化；掉金属怪前阶材料感：大岩蛇
			else if (npc.type == NPCID.WallofFlesh)
				Drop(npc, ModContent.ItemType<OnixForce>(), 0.40f);
			// 任一机械
			else if (npc.type == NPCID.TheDestroyer || npc.type == NPCID.SkeletronPrime || npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
				Drop(npc, ModContent.ItemType<DratiniForce>(), 0.25f);
			// 世花
			else if (npc.type == NPCID.Plantera)
				Drop(npc, ModContent.ItemType<HaunterForce>(), 0.35f);
			// 石巨人
			else if (npc.type == NPCID.Golem)
				Drop(npc, ModContent.ItemType<MetangForce>(), 0.35f);
			// 月总
			else if (npc.type == NPCID.MoonLordCore)
			{
				Drop(npc, ModContent.ItemType<MewtwoForce>(), 0.50f);
				Drop(npc, ModContent.ItemType<AlakazamForce>(), 0.40f);
			}
		}

		private static void Drop(NPC npc, int itemType, float chance)
		{
			if (Main.rand.NextFloat() <= chance)
				Item.NewItem(npc.GetSource_Loot(), npc.getRect(), itemType);
		}
	}

	public sealed class HenshinRecipes : ModSystem
	{
		public override void AddRecipes()
		{
			// 地鼠：金/铂矿早期合成
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

			// 鲤鱼王：钓鱼向
			Recipe.Create(ModContent.ItemType<MagikarpForce>())
				.AddIngredient(ItemID.Bass, 5)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddTile(TileID.WorkBenches)
				.Register();

			// 圆陆鲨：后期
			Recipe.Create(ModContent.ItemType<GibleForce>())
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient(ItemID.FragmentSolar, 5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			// 洛奇亚
			Recipe.Create(ModContent.ItemType<LugiaForce>())
				.AddIngredient(ItemID.LunarBar, 15)
				.AddIngredient(ItemID.FragmentVortex, 8)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			// 烈空坐
			Recipe.Create(ModContent.ItemType<RayquazaForce>())
				.AddIngredient(ItemID.LunarBar, 20)
				.AddIngredient(ItemID.FragmentNebula, 8)
				.AddIngredient(ItemID.FragmentStardust, 8)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			// 饰品简易合成（占位）
			AddAcc(ModContent.ItemType<A01AbilityCapsuleBelt>(), ItemID.LifeCrystal, 1);
			AddAcc(ModContent.ItemType<A02MuscleBand>(), ItemID.Shackle, 1);
			AddAcc(ModContent.ItemType<A03SoulDewPendant>(), ItemID.ManaCrystal, 1);
			AddAcc(ModContent.ItemType<A04FloatStoneAnklet>(), ItemID.Feather, 10);
			AddAcc(ModContent.ItemType<A05FocusSashBadge>(), ItemID.Bezoar, 1);
			AddAcc(ModContent.ItemType<A06LifeOrbCore>(), ItemID.AvengerEmblem, 1);
			AddAcc(ModContent.ItemType<A07CharcoalBag>(), ItemID.HellstoneBar, 5);
			AddAcc(ModContent.ItemType<A08MysticWaterPouch>(), ItemID.Coral, 10);
			AddAcc(ModContent.ItemType<A09MagnetChip>(), ItemID.Wire, 20);
			AddAcc(ModContent.ItemType<A10SharpBeakMembrane>(), ItemID.GiantHarpyFeather, 1);
			AddAcc(ModContent.ItemType<A11SpellTagCloth>(), ItemID.SoulofNight, 8);
			AddAcc(ModContent.ItemType<A12DragonFangCharm>(), ItemID.SoulofMight, 8);
		}

		private static void AddAcc(int result, int ing, int stack)
		{
			Recipe.Create(result)
				.AddIngredient(ing, stack)
				.AddIngredient(ItemID.FallenStar, 3)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
