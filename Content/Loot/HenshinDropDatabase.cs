using System.Collections.Generic;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Items.Forms;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Loot
{
	internal readonly struct HenshinNpcDrop
	{
		public readonly int ItemType;
		public readonly float Chance;
		public readonly int[] Related;
		public readonly bool HasBag;

		public HenshinNpcDrop(int itemType, float chance, int[] related, bool hasBag)
		{
			ItemType = itemType;
			Chance = chance;
			Related = related;
			HasBag = hasBag;
		}
	}

	internal readonly struct HenshinItemDrop
	{
		public readonly int ItemType;
		public readonly float Chance;
		public readonly bool ExtraExpertRoll;

		public HenshinItemDrop(int itemType, float chance, bool extraExpertRoll)
		{
			ItemType = itemType;
			Chance = chance;
			ExtraExpertRoll = extraExpertRoll;
		}
	}

	/// <summary>饰品碎片 + 之力 Boss 掉落：NPC / 宝藏袋 / 匣，全部走掉落规则（图鉴与合成浏览器可见）。</summary>
	internal static class HenshinDropDatabase
	{
		private static readonly Dictionary<int, List<HenshinNpcDrop>> Npc = new();
		private static readonly Dictionary<int, List<HenshinItemDrop>> Items = new();
		private static bool built;

		public static void Ensure()
		{
			if (built)
				return;
			built = true;
			RegisterFormBosses();
			RegisterAccessoryShards();
		}

		public static void ApplyNpcLoot(int npcType, NPCLoot npcLoot)
		{
			Ensure();
			if (!Npc.TryGetValue(npcType, out List<HenshinNpcDrop> list))
				return;
			foreach (HenshinNpcDrop drop in list)
				HenshinDropRules.AddNpcDrop(npcLoot, drop.ItemType, drop.Chance, drop.Related, drop.HasBag);
		}

		public static void ApplyItemLoot(int itemType, ItemLoot itemLoot)
		{
			Ensure();
			if (!Items.TryGetValue(itemType, out List<HenshinItemDrop> list))
				return;
			foreach (HenshinItemDrop drop in list)
				HenshinDropRules.AddBagDrop(itemLoot, drop.ItemType, drop.Chance, drop.ExtraExpertRoll);
		}

		private static void RegisterFormBosses()
		{
			AddBossNpc(NPCID.EyeofCthulhu, ModContent.ItemType<PikachuForce>(), 0.35f);
			AddBossNpc(NPCID.QueenBee, ModContent.ItemType<MachopForce>(), 0.35f);
			AddBossNpc(NPCID.SkeletronHead, ModContent.ItemType<PidgeyForce>(), 0.30f);
			AddBossNpc(NPCID.SkeletronHead, ModContent.ItemType<AbraForce>(), 0.30f);
			AddBossNpc(NPCID.WallofFlesh, ModContent.ItemType<OnixForce>(), 0.40f);
			AddBossNpc(NPCID.TheDestroyer, ModContent.ItemType<DratiniForce>(), 0.25f);
			AddBossNpc(NPCID.SkeletronPrime, ModContent.ItemType<DratiniForce>(), 0.25f);
			AddBossNpc(NPCID.Retinazer, ModContent.ItemType<DratiniForce>(), 0.25f);
			AddBossNpc(NPCID.Spazmatism, ModContent.ItemType<DratiniForce>(), 0.25f);
			AddBossNpc(NPCID.Plantera, ModContent.ItemType<HaunterForce>(), 0.35f);
			AddBossNpc(NPCID.Golem, ModContent.ItemType<MetangForce>(), 0.35f);
			AddBossNpc(NPCID.MoonLordCore, ModContent.ItemType<MewtwoForce>(), 0.50f);
			AddBossNpc(NPCID.MoonLordCore, ModContent.ItemType<AlakazamForce>(), 0.40f);
		}

		private static void RegisterAccessoryShards()
		{
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
							foreach (int npc in ExpandEventNpc(spec.VanillaId))
								AddEventNpc(npc, itemType, spec.Chance);
							break;
						case AccLootKind.BossVanilla:
							AddBossNpc(spec.VanillaId, itemType, spec.Chance);
							break;
						case AccLootKind.BossCalamity:
							AddCalamityBoss(spec.CalamityNpc, itemType, spec.Chance);
							break;
						case AccLootKind.Crate:
							AddCrate(spec.VanillaId, itemType, spec.Chance);
							break;
					}
				}
			}
		}

		private static IEnumerable<int> ExpandEventNpc(int npcType)
		{
			yield return npcType;
			if (npcType == NPCID.Scarecrow1)
			{
				for (int id = NPCID.Scarecrow2; id <= NPCID.Scarecrow10; id++)
					yield return id;
			}
		}

		private static void AddEventNpc(int npcType, int itemType, float chance)
			=> AddNpcUnique(npcType, new HenshinNpcDrop(itemType, chance, null, false));

		private static void AddBossNpc(int npcType, int itemType, float chance)
		{
			int[] related = HenshinDropRules.SegmentGroup(npcType);
			int bag = HenshinDropRules.VanillaBossBag(npcType);
			int[] targets = related ?? new[] { npcType };
			foreach (int t in targets)
				AddNpcUnique(t, new HenshinNpcDrop(itemType, chance, related, bag > 0));
			if (bag > 0)
				AddItemUnique(bag, new HenshinItemDrop(itemType, chance, true));
		}

		private static void AddCalamityBoss(string npcName, int itemType, float chance)
		{
			if (string.IsNullOrEmpty(npcName) || !ModLoader.TryGetMod("CalamityMod", out Mod calamity))
				return;
			if (!calamity.TryFind(npcName, out ModNPC modNpc))
				return;
			int npcType = modNpc.Type;
			int[] related = CalamityRelated(calamity, npcName, npcType);
			int bag = FindCalamityBag(calamity, npcName);
			int[] targets = related ?? new[] { npcType };
			foreach (int t in targets)
				AddNpcUnique(t, new HenshinNpcDrop(itemType, chance, related, bag > 0));
			if (bag > 0)
				AddItemUnique(bag, new HenshinItemDrop(itemType, chance, true));
		}

		private static int[] CalamityRelated(Mod calamity, string npcName, int npcType)
		{
			if (npcName != "Leviathan")
				return null;
			if (!calamity.TryFind("Anahita", out ModNPC anahita))
				return null;
			return new[] { npcType, anahita.Type };
		}

		private static int FindCalamityBag(Mod calamity, string npcName)
		{
			string[] names = npcName switch
			{
				"CalamitasClone" => new[] { "CalamitasCloneBag" },
				"Leviathan" => new[] { "LeviathanBag" },
				"Providence" => new[] { "ProvidenceBag" },
				"Polterghast" => new[] { "PolterghastBag" },
				"Yharon" => new[] { "YharonBag" },
				"OldDuke" => new[] { "OldDukeBag" },
				_ => new[] { npcName + "Bag" }
			};
			foreach (string n in names)
			{
				if (calamity.TryFind(n, out ModItem bag))
					return bag.Type;
			}
			return 0;
		}

		private static void AddCrate(int crateType, int itemType, float chance)
		{
			AddItemUnique(crateType, new HenshinItemDrop(itemType, chance, false));
			int hard = HenshinDropRules.HardmodeCrate(crateType);
			if (hard > 0)
				AddItemUnique(hard, new HenshinItemDrop(itemType, chance, false));
		}

		private static void AddNpcUnique(int npcType, HenshinNpcDrop drop)
		{
			if (!Npc.TryGetValue(npcType, out List<HenshinNpcDrop> list))
			{
				list = new List<HenshinNpcDrop>();
				Npc[npcType] = list;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].ItemType == drop.ItemType)
					return;
			}
			list.Add(drop);
		}

		private static void AddItemUnique(int itemType, HenshinItemDrop drop)
		{
			if (!Items.TryGetValue(itemType, out List<HenshinItemDrop> list))
			{
				list = new List<HenshinItemDrop>();
				Items[itemType] = list;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].ItemType == drop.ItemType)
					return;
			}
			list.Add(drop);
		}
	}

	public sealed class HenshinLootGlobalNPC : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
			=> HenshinDropDatabase.ApplyNpcLoot(npc.type, npcLoot);
	}

	public sealed class HenshinLootGlobalItem : GlobalItem
	{
		public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
			=> HenshinDropDatabase.ApplyItemLoot(item.type, itemLoot);
	}
}
