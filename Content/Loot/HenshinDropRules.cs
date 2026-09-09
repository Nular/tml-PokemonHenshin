using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Loot
{
	/// <summary>多节 Boss / 双子：只在最后一节（或最后一只）结算掉落，图鉴仍显示。</summary>
	public sealed class HenshinLastOfTypesCondition : IItemDropRuleCondition
	{
		private readonly int[] _types;

		public HenshinLastOfTypesCondition(params int[] types)
			=> _types = types ?? Array.Empty<int>();

		public bool CanDrop(DropAttemptInfo info)
		{
			NPC self = info.npc;
			if (self == null || _types.Length == 0)
				return true;
			int who = self.whoAmI;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (i == who)
					continue;
				NPC n = Main.npc[i];
				if (!n.active || n.life <= 0)
					continue;
				for (int t = 0; t < _types.Length; t++)
				{
					if (n.type == _types[t])
						return false;
				}
			}
			return true;
		}

		public bool CanShowItemDropInUI() => true;

		public string GetConditionDescription() => null;
	}

	public sealed class HenshinExpertCondition : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) => Main.expertMode || Main.masterMode;
		public bool CanShowItemDropInUI() => true;
		public string GetConditionDescription() => null;
	}

	internal static class HenshinDropRules
	{
		public static IItemDropRule Chance(int itemType, float chance)
		{
			chance = Math.Clamp(chance, 0f, 1f);
			if (chance >= 0.999f)
				return ItemDropRule.Common(itemType);
			int denom = 1000;
			int num = Math.Max(1, (int)Math.Round(chance * denom));
			if (num >= denom)
				return ItemDropRule.Common(itemType);
			int g = Gcd(num, denom);
			return new CommonDrop(itemType, denom / g, 1, 1, num / g);
		}

		public static void AddNpcDrop(NPCLoot npcLoot, int itemType, float chance, int[] related, bool hasTreasureBag)
		{
			IItemDropRule Make()
			{
				IItemDropRule inner = Chance(itemType, chance);
				if (related == null || related.Length <= 1)
					return inner;
				var last = new LeadingConditionRule(new HenshinLastOfTypesCondition(related));
				last.OnSuccess(inner);
				return last;
			}

			if (hasTreasureBag)
			{
				var notExpert = new LeadingConditionRule(new Conditions.NotExpert());
				notExpert.OnSuccess(Make());
				npcLoot.Add(notExpert);
				return;
			}

			npcLoot.Add(Make());
			var expert = new LeadingConditionRule(new HenshinExpertCondition());
			expert.OnSuccess(Make());
			npcLoot.Add(expert);
		}

		public static void AddBagDrop(ItemLoot itemLoot, int itemType, float chance, bool extraExpertRoll)
		{
			itemLoot.Add(Chance(itemType, chance));
			if (extraExpertRoll)
				itemLoot.Add(Chance(itemType, chance));
		}

		public static int[] SegmentGroup(int npcType)
		{
			switch (npcType)
			{
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
					return new int[] { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail };
				case NPCID.TheDestroyer:
				case NPCID.TheDestroyerBody:
				case NPCID.TheDestroyerTail:
					return new int[] { NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail };
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
					return new int[] { NPCID.Retinazer, NPCID.Spazmatism };
				case NPCID.WyvernHead:
				case NPCID.WyvernLegs:
				case NPCID.WyvernBody:
				case NPCID.WyvernBody2:
				case NPCID.WyvernBody3:
				case NPCID.WyvernTail:
					return new int[]
					{
						NPCID.WyvernHead, NPCID.WyvernLegs, NPCID.WyvernBody,
						NPCID.WyvernBody2, NPCID.WyvernBody3, NPCID.WyvernTail
					};
				default:
					return null;
			}
		}

		public static int VanillaBossBag(int npcType)
		{
			switch (npcType)
			{
				case NPCID.KingSlime: return ItemID.KingSlimeBossBag;
				case NPCID.EyeofCthulhu: return ItemID.EyeOfCthulhuBossBag;
				case NPCID.EaterofWorldsHead:
				case NPCID.EaterofWorldsBody:
				case NPCID.EaterofWorldsTail:
					return ItemID.EaterOfWorldsBossBag;
				case NPCID.QueenBee: return ItemID.QueenBeeBossBag;
				case NPCID.SkeletronHead: return ItemID.SkeletronBossBag;
				case NPCID.Deerclops: return ItemID.DeerclopsBossBag;
				case NPCID.WallofFlesh: return ItemID.WallOfFleshBossBag;
				case NPCID.TheDestroyer:
				case NPCID.TheDestroyerBody:
				case NPCID.TheDestroyerTail:
					return ItemID.DestroyerBossBag;
				case NPCID.Retinazer:
				case NPCID.Spazmatism:
					return ItemID.TwinsBossBag;
				case NPCID.SkeletronPrime: return ItemID.SkeletronPrimeBossBag;
				case NPCID.Plantera: return ItemID.PlanteraBossBag;
				case NPCID.Golem: return ItemID.GolemBossBag;
				case NPCID.DukeFishron: return ItemID.FishronBossBag;
				case NPCID.MoonLordCore: return ItemID.MoonLordBossBag;
				default: return 0;
			}
		}

		public static int HardmodeCrate(int crateType)
		{
			if (crateType == ItemID.WoodenCrate) return ItemID.WoodenCrateHard;
			if (crateType == ItemID.IronCrate) return ItemID.IronCrateHard;
			if (crateType == ItemID.GoldenCrate) return ItemID.GoldenCrateHard;
			if (crateType == ItemID.JungleFishingCrate) return ItemID.JungleFishingCrateHard;
			if (crateType == ItemID.FloatingIslandFishingCrate) return ItemID.FloatingIslandFishingCrateHard;
			if (crateType == ItemID.CorruptFishingCrate) return ItemID.CorruptFishingCrateHard;
			if (crateType == ItemID.HallowedFishingCrate) return ItemID.HallowedFishingCrateHard;
			if (crateType == ItemID.DungeonFishingCrate) return ItemID.DungeonFishingCrateHard;
			if (crateType == ItemID.OceanCrate) return ItemID.OceanCrateHard;
			if (crateType == ItemID.OasisCrate) return ItemID.OasisCrateHard;
			if (crateType == ItemID.LavaCrate) return ItemID.LavaCrateHard;
			return 0;
		}

		private static int Gcd(int a, int b)
		{
			while (b != 0)
			{
				int t = a % b;
				a = b;
				b = t;
			}
			return a < 0 ? -a : a;
		}
	}
}
