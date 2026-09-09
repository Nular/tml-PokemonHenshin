using System.Collections.Generic;
using PokemonHenshin.Content.Accessories;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Loot
{
	/// <summary>按 Catalog 掉碎片：匣 10%、事件 2%（RollLuck）、Boss 25%（专家再 Roll）。</summary>
	public sealed class HenshinAccLootGlobalNPC : GlobalNPC
	{
		private static readonly List<(int NpcType, int ItemType, float Chance)> EventDrops = new();
		private static readonly List<(int NpcType, int ItemType, float Chance)> BossDrops = new();
		private static readonly List<(string CalamityNpc, int ItemType, float Chance)> CalamityDrops = new();
		private static bool built;

		private static void EnsureBuilt()
		{
			if (built)
				return;
			built = true;
			foreach (AccFamilyDef def in HenshinAccCatalog.All)
			{
				foreach (AccPiece piece in HenshinAccLoader.Pieces)
				{
					if (piece is < AccPiece.S1 or > AccPiece.S6)
						continue;
					AccLootSpec spec = def.LootFor(piece);
					if (spec == null)
						continue;
					int itemType = HenshinAccLoader.ItemType(def.Id, piece);
					if (itemType <= 0)
						continue;
					switch (spec.Kind)
					{
						case AccLootKind.EventNpc:
							EventDrops.Add((spec.VanillaId, itemType, spec.Chance));
							break;
						case AccLootKind.BossVanilla:
							BossDrops.Add((spec.VanillaId, itemType, spec.Chance));
							break;
						case AccLootKind.BossCalamity:
							if (!string.IsNullOrEmpty(spec.CalamityNpc))
								CalamityDrops.Add((spec.CalamityNpc, itemType, spec.Chance));
							break;
					}
				}
			}
		}

		public override void OnKill(NPC npc)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
				return;

			EnsureBuilt();
			Player lucky = ResolveLuckyPlayer(npc);

			if (!npc.boss && !NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
			{
				foreach ((int npcType, int itemType, float chance) in EventDrops)
				{
					if (npc.type != npcType)
						continue;
					int denom = chance <= 0f ? 50 : (int)System.Math.Round(1f / chance);
					bool hit = lucky == null
						? Main.rand.NextFloat() < chance
						: lucky.RollLuck(System.Math.Max(1, denom)) == 0;
					if (hit)
						Drop(npc, itemType);
				}
				return;
			}

			bool expertExtra = Main.expertMode || Main.masterMode;
			foreach ((int npcType, int itemType, float chance) in BossDrops)
			{
				if (npc.type != npcType)
					continue;
				TryBossRoll(npc, itemType, chance, expertExtra);
			}

			if (npc.ModNPC?.Mod?.Name == "CalamityMod")
			{
				string name = npc.ModNPC.Name;
				foreach ((string calamityNpc, int itemType, float chance) in CalamityDrops)
				{
					if (calamityNpc != name)
						continue;
					TryBossRoll(npc, itemType, chance, expertExtra);
				}
			}
		}

		private static void TryBossRoll(NPC npc, int itemType, float chance, bool expertExtra)
		{
			if (Main.rand.NextFloat() < chance)
				Drop(npc, itemType);
			if (expertExtra && Main.rand.NextFloat() < chance)
				Drop(npc, itemType);
		}

		private static Player ResolveLuckyPlayer(NPC npc)
		{
			int who = npc.lastInteraction;
			if (who >= 0 && who < Main.maxPlayers && Main.player[who].active)
				return Main.player[who];
			int closest = Player.FindClosest(npc.Center, 1, 1);
			if (closest >= 0 && closest < Main.maxPlayers)
				return Main.player[closest];
			return null;
		}

		private static void Drop(NPC npc, int itemType)
			=> Item.NewItem(npc.GetSource_Loot(), npc.getRect(), itemType);
	}

	public sealed class HenshinAccCrateLoot : GlobalItem
	{
		private static readonly List<(int CrateType, int ItemType, float Chance)> CrateDrops = new();
		private static bool built;

		private static void EnsureBuilt()
		{
			if (built)
				return;
			built = true;
			foreach (AccFamilyDef def in HenshinAccCatalog.All)
			{
				foreach (AccPiece piece in HenshinAccLoader.Pieces)
				{
					if (piece is < AccPiece.S1 or > AccPiece.S6)
						continue;
					AccLootSpec spec = def.LootFor(piece);
					if (spec is not { Kind: AccLootKind.Crate })
						continue;
					int itemType = HenshinAccLoader.ItemType(def.Id, piece);
					if (itemType <= 0)
						continue;
					CrateDrops.Add((spec.VanillaId, itemType, spec.Chance));
				}
			}
		}

		public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
		{
			EnsureBuilt();
			foreach ((int crateType, int itemType, float chance) in CrateDrops)
			{
				if (item.type != crateType)
					continue;
				int denom = chance <= 0f ? 10 : (int)System.Math.Round(1f / chance);
				itemLoot.Add(ItemDropRule.Common(itemType, System.Math.Max(1, denom)));
			}
		}
	}
}
