using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Accessories
{
	public sealed class AccFamilyDef
	{
		public AccFamilyId Id { get; init; }
		public string LegacyTypeName { get; init; }
		public string OfficialZh { get; init; }
		public string OfficialEn { get; init; }
		public string WikiBagFile { get; init; }
		public PokemonType Resonance { get; init; }
		/// <summary>非空时仅该 FormId（如 L04_F01）持握变身生效。</summary>
		public string RequiredFormId { get; init; }
		public bool WorksUntransformed { get; init; }
		public int MinStage { get; init; }
		public int SuperMinStage { get; init; }
		public int NormalRarity { get; init; }
		public int SuperRarity { get; init; }
		public AccStatLine[] Shard1 { get; init; }
		public AccStatLine[] Shard2 { get; init; }
		public AccStatLine[] Shard3 { get; init; }
		public AccStatLine[] Shard4 { get; init; }
		public AccStatLine[] Shard5 { get; init; }
		public AccStatLine[] Shard6 { get; init; }
		public AccStatLine[] NormalStats { get; init; }
		public AccStatLine[] SuperStats { get; init; }
		public AccLootSpec[] Loot { get; init; }

		public string TexturePath => $"PokemonHenshin/Assets/Accessories/A{(int)Id:D2}";
		public string SuperTexturePath => TexturePath + "_Super";
		public string ShardTexturePath => TexturePath + "_Shard";

		public string ItemName(AccPiece piece)
		{
			if (piece == AccPiece.Normal)
				return LegacyTypeName;
			if (piece == AccPiece.Super)
				return LegacyTypeName + "_Super";
			return LegacyTypeName + "_S" + (byte)piece;
		}

		public AccStatLine[] Stats(AccPiece piece) => piece switch
		{
			AccPiece.S1 => Shard1,
			AccPiece.S2 => Shard2,
			AccPiece.S3 => Shard3,
			AccPiece.S4 => Shard4,
			AccPiece.S5 => Shard5,
			AccPiece.S6 => Shard6,
			AccPiece.Normal => NormalStats,
			AccPiece.Super => SuperStats,
			_ => System.Array.Empty<AccStatLine>()
		};

		public AccLootSpec LootFor(AccPiece piece)
		{
			int idx = piece is >= AccPiece.S1 and <= AccPiece.S6 ? (byte)piece - 1 : -1;
			if (idx < 0 || Loot == null || idx >= Loot.Length)
				return null;
			return Loot[idx];
		}
	}

	public static class HenshinAccCatalog
	{
		public static readonly AccFamilyDef[] All;

		static HenshinAccCatalog()
		{
			All = Build();
		}

		public static AccFamilyDef Get(AccFamilyId id)
		{
			int i = (int)id - 1;
			if (i < 0 || i >= All.Length)
				return null;
			return All[i];
		}

		private static AccStatLine S(AccStat stat, float v) => AccStatLine.Of(stat, v);
		private static AccStatLine F(AccStat stat) => AccStatLine.Flag(stat);

		private static AccLootSpec Craft(int tile, params (int id, int stack)[] ings) => new()
		{
			Kind = AccLootKind.Craft,
			CraftTile = tile,
			Craft = ings,
			Chance = 1f
		};

		private static AccLootSpec Crate(int itemId, float chance = 0.10f) => new()
		{
			Kind = AccLootKind.Crate,
			VanillaId = itemId,
			Chance = chance
		};

		private static AccLootSpec Event(int npcId, float chance = 0.02f) => new()
		{
			Kind = AccLootKind.EventNpc,
			VanillaId = npcId,
			Chance = chance
		};

		private static AccLootSpec Boss(int npcId, float chance = 0.25f) => new()
		{
			Kind = AccLootKind.BossVanilla,
			VanillaId = npcId,
			Chance = chance
		};

		private static AccLootSpec CalBoss(string internalName, float chance = 0.25f) => new()
		{
			Kind = AccLootKind.BossCalamity,
			CalamityNpc = internalName,
			Chance = chance
		};

		private static AccFamilyDef Fam(
			AccFamilyId id, string legacy, string zh, string en, string wiki,
			int min, int superMin, PokemonType res = PokemonType.None, bool untransformed = false,
			AccStatLine[] s1 = null, AccStatLine[] s2 = null, AccStatLine[] s3 = null,
			AccStatLine[] s4 = null, AccStatLine[] s5 = null, AccStatLine[] s6 = null,
			AccStatLine[] normal = null, AccStatLine[] super = null, AccLootSpec[] loot = null,
			string requiredFormId = null)
		{
			return new AccFamilyDef
			{
				Id = id,
				LegacyTypeName = legacy,
				OfficialZh = zh,
				OfficialEn = en,
				WikiBagFile = wiki,
				Resonance = res,
				RequiredFormId = requiredFormId,
				WorksUntransformed = untransformed,
				MinStage = min,
				SuperMinStage = superMin,
				NormalRarity = RarityFor(min),
				SuperRarity = RarityFor(superMin + 2),
				Shard1 = s1 ?? System.Array.Empty<AccStatLine>(),
				Shard2 = s2 ?? System.Array.Empty<AccStatLine>(),
				Shard3 = s3 ?? System.Array.Empty<AccStatLine>(),
				Shard4 = s4 ?? System.Array.Empty<AccStatLine>(),
				Shard5 = s5 ?? System.Array.Empty<AccStatLine>(),
				Shard6 = s6 ?? System.Array.Empty<AccStatLine>(),
				NormalStats = normal ?? System.Array.Empty<AccStatLine>(),
				SuperStats = super ?? System.Array.Empty<AccStatLine>(),
				Loot = loot ?? System.Array.Empty<AccLootSpec>()
			};
		}

		private static int RarityFor(int stage)
		{
			if (stage <= 2) return ItemRarityID.Blue;
			if (stage <= 4) return ItemRarityID.Orange;
			if (stage <= 6) return ItemRarityID.LightRed;
			if (stage <= 8) return ItemRarityID.Lime;
			if (stage <= 10) return ItemRarityID.Cyan;
			return ItemRarityID.Red;
		}

		private static AccFamilyDef[] Build() => new[]
		{
			Fam(AccFamilyId.A01, "A01AbilityCapsuleBelt", "特性胶囊", "Ability Capsule", "Bag 特性胶囊 SV Sprite.png", 1, 5,
				s1: new[] { S(AccStat.CooldownCut, 0.0125f) },
				s2: new[] { S(AccStat.CooldownCut, 0.0125f) },
				s3: new[] { S(AccStat.CooldownCut, 0.0125f) },
				s4: new[] { S(AccStat.CooldownCut, 0.0125f) },
				s5: new[] { S(AccStat.PassiveEnergyMul, 1.0f) },
				s6: new[] { S(AccStat.CooldownCut, 0.0125f) },
				normal: new[] { S(AccStat.CooldownCut, 0.05f) },
				super: new[] { S(AccStat.CooldownCut, 0.10f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.LifeCrystal, 1), (ItemID.FallenStar, 1)),
					Crate(ItemID.WoodenCrate),
					Event(NPCID.GoblinWarrior),
					Boss(NPCID.KingSlime),
					Craft(TileID.Anvils, (ItemID.GoldBar, 8)),
					Boss(NPCID.QueenBee)
				}),

			Fam(AccFamilyId.A02, "A02MuscleBand", "力量头带", "Muscle Band", "Bag 力量头带 SV Sprite.png", 3, 6,
				s1: new[] { S(AccStat.DamageBonus, 0.025f) },
				s2: new[] { S(AccStat.DamageBonus, 0.025f) },
				s3: new[] { S(AccStat.DamageBonus, 0.025f) },
				s4: new[] { S(AccStat.DamageBonus, 0.025f) },
				s5: new[] { S(AccStat.DamageBonus, 0.025f) },
				s6: new[] { S(AccStat.CritChance, 0.05f) },
				normal: new[] { S(AccStat.DamageBonus, 0.10f) },
				super: new[] { S(AccStat.DamageBonus, 0.20f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.Shackle, 1), (ItemID.StoneBlock, 20)),
					Crate(ItemID.IronCrate),
					Event(NPCID.BloodZombie),
					Boss(NPCID.EyeofCthulhu),
					Craft(TileID.Anvils, (ItemID.HellstoneBar, 5)),
					Boss(NPCID.EaterofWorldsHead)
				}),

			Fam(AccFamilyId.A03, "A03SoulDewPendant", "心之水滴", "Soul Dew", "Bag 心之水滴 SV Sprite.png", 4, 7,
				s1: new[] { S(AccStat.AffinityPower, 0.025f) },
				s2: new[] { S(AccStat.AffinityPower, 0.025f) },
				s3: new[] { S(AccStat.AffinityPower, 0.025f) },
				s4: new[] { S(AccStat.AffinityPower, 0.025f) },
				s5: new[] { S(AccStat.AffinityPower, 0.025f) },
				s6: new[] { S(AccStat.PsychicDragonDamage, 0.08f) },
				normal: new[] { S(AccStat.AffinityPower, 0.10f) },
				super: new[] { S(AccStat.AffinityPower, 0.20f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.ManaCrystal, 1), (ItemID.FallenStar, 3)),
					Crate(ItemID.FloatingIslandFishingCrate),
					Event(NPCID.MeteorHead),
					Boss(NPCID.SkeletronHead),
					Craft(TileID.MythrilAnvil, (ItemID.SoulofLight, 5)),
					Boss(NPCID.WallofFlesh)
				}),

			Fam(AccFamilyId.A04, "A04FloatStoneAnklet", "轻石", "Float Stone", "Bag 轻石 SV Sprite.png", 5, 8,
				s1: new[] { S(AccStat.MoveSpeedBonus, 0.02f) },
				s2: new[] { S(AccStat.MoveSpeedBonus, 0.02f) },
				s3: new[] { S(AccStat.FlightEnergySec, 0.35f) },
				s4: new[] { S(AccStat.FlightEnergySec, 0.35f) },
				s5: new[] { S(AccStat.MoveSpeedBonus, 0.02f) },
				s6: new[] { S(AccStat.FlightEnergySec, 0.35f) },
				normal: new[] { S(AccStat.FlightEnergySec, 1f), S(AccStat.MoveSpeedBonus, 0.08f) },
				super: new[] { S(AccStat.FlightEnergySec, 2f), S(AccStat.MoveSpeedBonus, 0.16f) },
				loot: new[]
				{
					Craft(TileID.SkyMill, (ItemID.Feather, 10), (ItemID.Cloud, 20)),
					Crate(ItemID.FloatingIslandFishingCrate),
					Event(NPCID.Harpy),
					Boss(NPCID.SkeletronHead),
					Craft(TileID.MythrilAnvil, (ItemID.SoulofFlight, 8), (ItemID.Feather, 10)),
					Boss(NPCID.WyvernHead)
				}),

			Fam(AccFamilyId.A05, "A05FocusSashBadge", "气势头带", "Focus Band", "Bag 气势头带 SV Sprite.png", 7, 10,
				s1: new[] { S(AccStat.IncomingCut, 0.015f) },
				s2: new[] { S(AccStat.IncomingCut, 0.015f) },
				s3: new[] { S(AccStat.IncomingCut, 0.015f) },
				s4: new[] { S(AccStat.IncomingCut, 0.015f) },
				s5: new[] { S(AccStat.IncomingCut, 0.015f) },
				s6: new[] { S(AccStat.IncomingCut, 0.03f) },
				normal: new[] { S(AccStat.IncomingCut, 0.06f) },
				super: new[] { S(AccStat.IncomingCut, 0.12f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.AdhesiveBandage, 1), (ItemID.LifeCrystal, 1)),
					Crate(ItemID.DungeonFishingCrate),
					Event(NPCID.Eyezor),
					Boss(NPCID.WallofFlesh),
					Craft(TileID.MythrilAnvil, (ItemID.HallowedBar, 5)),
					Boss(NPCID.Plantera)
				}),

			Fam(AccFamilyId.A06, "A06LifeOrbCore", "达人带", "Expert Belt", "Bag 达人带 SV Sprite.png", 9, 12,
				s1: new[] { S(AccStat.DamageFactorBonus, 0.02f) },
				s2: new[] { S(AccStat.DamageFactorBonus, 0.02f) },
				s3: new[] { S(AccStat.DamageFactorBonus, 0.02f) },
				s4: new[] { S(AccStat.DamageFactorBonus, 0.02f) },
				s5: new[] { S(AccStat.DamageFactorBonus, 0.02f) },
				s6: new[] { S(AccStat.BossDamageBonus, 0.05f) },
				normal: new[] { S(AccStat.DamageFactorBonus, 0.08f) },
				super: new[] { S(AccStat.DamageFactorBonus, 0.16f) },
				loot: new[]
				{
					Craft(TileID.MythrilAnvil, (ItemID.ChlorophyteBar, 5), (ItemID.SoulofMight, 10)),
					Crate(ItemID.HallowedFishingCrate),
					Event(NPCID.MartianWalker),
					Boss(NPCID.MoonLordCore),
					Craft(TileID.LunarCraftingStation, (ItemID.FragmentVortex, 6)),
					CalBoss("Providence")
				}),

			Fam(AccFamilyId.A07, "A07CharcoalBag", "木炭", "Charcoal", "Bag 木炭 SV Sprite.png", 2, 5, PokemonType.Fire,
				s1: new[] { S(AccStat.OnFireTargetBonus, 0.03f) },
				s2: new[] { S(AccStat.FireMoveDamage, 0.04f) },
				s3: new[] { S(AccStat.OnFireCritUpgrade, 0.05f) },
				s4: new[] { S(AccStat.OnFireTargetBonus, 0.03f) },
				s5: new[] { S(AccStat.OnFireTargetBonus, 0.03f) },
				s6: new[] { S(AccStat.FireMoveDamage, 0.04f) },
				normal: new[] { S(AccStat.OnFireTargetBonus, 0.12f), S(AccStat.FireMoveDamage, 0.08f), S(AccStat.OnFireCritUpgrade, 0.05f) },
				super: new[] { S(AccStat.OnFireTargetBonus, 0.24f), S(AccStat.FireMoveDamage, 0.16f), S(AccStat.OnFireCritUpgrade, 0.10f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.HellstoneBar, 3)),
					Crate(ItemID.LavaCrate),
					Event(NPCID.CreatureFromTheDeep),
					Boss(NPCID.WallofFlesh),
					Crate(ItemID.LavaCrate, 0.10f),
					CalBoss("CalamitasClone")
				}),

			Fam(AccFamilyId.A08, "A08MysticWaterPouch", "神秘水滴", "Mystic Water", "Bag 神秘水滴 SV Sprite.png", 2, 5, PokemonType.Water,
				s1: new[] { S(AccStat.WaterSpeedBonus, 0.025f) },
				s2: new[] { S(AccStat.WaterSpeedBonus, 0.025f) },
				s3: new[] { S(AccStat.WaterSpeedBonus, 0.025f) },
				s4: new[] { S(AccStat.WaterSpeedBonus, 0.025f) },
				s5: new[] { S(AccStat.WaterMoveDamage, 0.05f) },
				s6: new[] { S(AccStat.WaterMoveDamage, 0.05f), S(AccStat.FlightEnergySec, 0.5f) },
				normal: new[] { S(AccStat.WaterSpeedBonus, 0.10f), S(AccStat.WaterMoveDamage, 0.10f) },
				super: new[] { S(AccStat.WaterSpeedBonus, 0.20f), S(AccStat.WaterMoveDamage, 0.20f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.Coral, 8), (ItemID.Seashell, 3)),
					Crate(ItemID.OceanCrate),
					Event(NPCID.PirateDeckhand),
					Boss(NPCID.EyeofCthulhu),
					CalBoss("Leviathan"),
					Boss(NPCID.DukeFishron)
				}),

			Fam(AccFamilyId.A09, "A09MagnetChip", "磁铁", "Magnet", "Bag 磁铁 SV Sprite.png", 4, 7, PokemonType.Electric,
				s1: new[] { S(AccStat.DashCooldownCut, 0.0375f) },
				s2: new[] { S(AccStat.DashCooldownCut, 0.0375f) },
				s3: new[] { S(AccStat.DashCooldownCut, 0.0375f) },
				s4: new[] { S(AccStat.DashCooldownCut, 0.0375f) },
				s5: new[] { S(AccStat.DashCooldownCut, 0.0375f) },
				s6: new[] { S(AccStat.DashSpeedBonus, 3f) },
				normal: new[] { S(AccStat.DashCooldownCut, 0.15f), S(AccStat.DashSpeedBonus, 2f) },
				super: new[] { S(AccStat.DashCooldownCut, 0.30f), S(AccStat.DashSpeedBonus, 4f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.Wire, 20), (ItemID.IronBar, 5)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.MartianWalker),
					Boss(NPCID.SkeletronPrime),
					Craft(TileID.MythrilAnvil, (ItemID.Wire, 50), (ItemID.HallowedBar, 5)),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A10, "A10SharpBeakMembrane", "锐利鸟嘴", "Sharp Beak", "Bag 锐利鸟嘴 SV Sprite.png", 5, 8, PokemonType.Flying,
				s1: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				s2: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				s3: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				s4: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				s5: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				s6: new[] { S(AccStat.FlyingMoveDamage, 0.015f), S(AccStat.FlightEnergyMul, 0.015f) },
				normal: new[] { S(AccStat.FlyingMoveDamage, 0.10f), S(AccStat.FlightEnergyMul, 0.10f) },
				super: new[] { S(AccStat.FlyingMoveDamage, 0.20f), S(AccStat.FlightEnergyMul, 0.20f) },
				loot: new[]
				{
					Craft(TileID.SkyMill, (ItemID.GiantHarpyFeather, 1)),
					Crate(ItemID.FloatingIslandFishingCrate),
					Event(NPCID.Harpy),
					Boss(NPCID.WyvernHead),
					Craft(TileID.MythrilAnvil, (ItemID.SoulofFlight, 8), (ItemID.Feather, 10)),
					Boss(NPCID.Golem)
				}),

			Fam(AccFamilyId.A11, "A11SpellTagCloth", "诅咒之符", "Spell Tag", "Bag 诅咒之符 SV Sprite.png", 6, 9, PokemonType.None,
				s1: new[] { F(AccStat.TilePierceBolt), S(AccStat.CursedInfernoSec, 3f) },
				s2: new[] { F(AccStat.TilePierceSpread), S(AccStat.CursedInfernoSec, 3f) },
				s3: new[] { F(AccStat.TilePierceBarrage), S(AccStat.CursedInfernoSec, 3f) },
				s4: new[] { F(AccStat.TilePierceDoTBind), S(AccStat.CursedInfernoSec, 3f) },
				s5: new[] { F(AccStat.TilePierceBeam), S(AccStat.CursedInfernoSec, 3f) },
				s6: new[] { S(AccStat.PenetrateAdd, 1f), S(AccStat.CursedInfernoSec, 3f) },
				normal: new[] { F(AccStat.TilePierceBolt), F(AccStat.TilePierceSpread), F(AccStat.TilePierceBarrage), F(AccStat.TilePierceDoTBind), F(AccStat.TilePierceBeam), S(AccStat.CursedInfernoSec, 5f) },
				super: new[] { F(AccStat.TilePierceBolt), F(AccStat.TilePierceSpread), F(AccStat.TilePierceBarrage), F(AccStat.TilePierceDoTBind), F(AccStat.TilePierceBeam), S(AccStat.PenetrateAdd, 1f), F(AccStat.CursedInfernoImmune) },
				loot: new[]
				{
					Craft(TileID.MythrilAnvil, (ItemID.SoulofNight, 5), (ItemID.RottenChunk, 10)),
					Crate(ItemID.CorruptFishingCrate),
					Event(NPCID.Eyezor),
					Boss(NPCID.EaterofWorldsHead),
					Boss(NPCID.Plantera),
					CalBoss("Polterghast")
				}),

			Fam(AccFamilyId.A12, "A12DragonFangCharm", "龙之牙", "Dragon Fang", "Bag 龙之牙 SV Sprite.png", 8, 11, PokemonType.Dragon,
				s1: new[] { S(AccStat.BossDamageBonus, 0.025f) },
				s2: new[] { S(AccStat.BossDamageBonus, 0.025f) },
				s3: new[] { S(AccStat.BossDamageBonus, 0.025f) },
				s4: new[] { S(AccStat.BossDamageBonus, 0.025f) },
				s5: new[] { S(AccStat.BossDamageBonus, 0.025f) },
				s6: new[] { S(AccStat.DamageBonus, 0.04f) },
				normal: new[] { S(AccStat.BossDamageBonus, 0.10f) },
				super: new[] { S(AccStat.BossDamageBonus, 0.18f) },
				loot: new[]
				{
					Craft(TileID.MythrilAnvil, (ItemID.SoulofMight, 5), (ItemID.HallowedBar, 6)),
					Crate(ItemID.HallowedFishingCrate),
					Event(NPCID.Eyezor),
					Boss(NPCID.TheDestroyer),
					Boss(NPCID.Golem),
					CalBoss("Yharon")
				}),

			Fam(AccFamilyId.A13, "A13WideLens", "广角镜", "Wide Lens", "Bag 广角镜 SV Sprite.png", 3, 6,
				s1: new[] { F(AccStat.HomingBolt), S(AccStat.HomingTurn, 0.06f), S(AccStat.HomingRange, 8f), S(AccStat.CritChance, 0.05f) },
				s2: new[] { F(AccStat.HomingSpread), S(AccStat.HomingTurn, 0.06f), S(AccStat.HomingRange, 8f), S(AccStat.CritChance, 0.05f) },
				s3: new[] { F(AccStat.HomingBarrage), S(AccStat.HomingTurn, 0.06f), S(AccStat.HomingRange, 8f), S(AccStat.CritChance, 0.05f) },
				s4: new[] { F(AccStat.HomingDoTBind), S(AccStat.HomingTurn, 0.06f), S(AccStat.HomingRange, 8f), S(AccStat.CritChance, 0.05f) },
				s5: new[] { F(AccStat.HomingBolt), S(AccStat.HomingTurn, 0.08f), S(AccStat.HomingRange, 8f), S(AccStat.CritChance, 0.05f) },
				s6: new[] { S(AccStat.DamageBonus, 0.025f), S(AccStat.CritChance, 0.05f) },
				normal: new[] { F(AccStat.HomingBolt), F(AccStat.HomingSpread), F(AccStat.HomingBarrage), F(AccStat.HomingDoTBind), S(AccStat.HomingTurn, 0.12f), S(AccStat.HomingRange, 16f), S(AccStat.DamageBonus, 0.04f), S(AccStat.CritChance, 0.08f) },
				super: new[] { F(AccStat.HomingBolt), F(AccStat.HomingSpread), F(AccStat.HomingBarrage), F(AccStat.HomingDoTBind), S(AccStat.HomingTurn, 0.20f), S(AccStat.HomingRange, 32f), S(AccStat.DamageBonus, 0.08f), S(AccStat.CritChance, 0.12f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.Lens, 5)),
					Crate(ItemID.WoodenCrate),
					Event(NPCID.GoblinArcher),
					Boss(NPCID.EyeofCthulhu),
					Craft(TileID.WorkBenches, (ItemID.BlackLens, 1)),
					Boss(NPCID.Retinazer)
				}),

			Fam(AccFamilyId.A14, "A14ChoiceBand", "讲究头带", "Choice Band", "Bag 讲究头带 SV Sprite.png", 4, 7,
				s1: new[] { S(AccStat.ChoiceDamage, 0.10f) },
				s2: new[] { S(AccStat.ChoiceDamage, 0.10f) },
				s3: new[] { F(AccStat.ChoiceLockSkill2), S(AccStat.ChoiceDamage, 0.25f) },
				s4: new[] { F(AccStat.ChoiceLockUlt), S(AccStat.ChoiceDamage, 0.25f) },
				s5: new[] { S(AccStat.ChoiceDamage, 0.10f) },
				s6: new[] { S(AccStat.ChoiceDamage, 0.10f) },
				normal: new[] { S(AccStat.ChoiceDamage, 0.50f), F(AccStat.ChoiceLockSkill2), F(AccStat.ChoiceLockUlt) },
				super: new[] { S(AccStat.ChoiceDamage, 1.00f), F(AccStat.ChoiceLockSkill2), F(AccStat.ChoiceLockUlt) },
				loot: new[]
				{
					Craft(TileID.TinkerersWorkbench, (ItemID.BandofRegeneration, 1), (ItemID.Leather, 5)),
					Crate(ItemID.IronCrate),
					Event(NPCID.GoblinWarrior),
					Boss(NPCID.QueenBee),
					Craft(TileID.MythrilAnvil, (ItemID.WarriorEmblem, 1)),
					Boss(NPCID.Plantera)
				}),

			Fam(AccFamilyId.A15, "A15ScopeLens", "焦点镜", "Scope Lens", "Bag 焦点镜 SV Sprite.png", 5, 8,
				s1: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				s2: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				s3: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				s4: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				s5: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				s6: new[] { S(AccStat.CritChance, 0.05f), S(AccStat.CritUpgradeChance, 0.05f) },
				normal: new[] { S(AccStat.CritChance, 0.20f), S(AccStat.CritUpgradeChance, 0.10f), S(AccStat.CritDamage, 0.10f) },
				super: new[] { S(AccStat.CritChance, 0.40f), S(AccStat.CritUpgradeChance, 0.20f), S(AccStat.CritDamage, 0.50f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.BlackLens, 1), (ItemID.Lens, 3)),
					Crate(ItemID.DungeonFishingCrate),
					Event(NPCID.Eyezor),
					Boss(NPCID.SkeletronHead),
					Craft(TileID.MythrilAnvil, (ItemID.MechanicalEye, 1)),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A16, "A16LifeOrb", "生命宝珠", "Life Orb", "Bag 生命宝珠 SV Sprite.png", 5, 8,
				s1: new[] { S(AccStat.LifeOrbDamage, 0.04f) },
				s2: new[] { S(AccStat.LifeOrbDamage, 0.04f) },
				s3: new[] { S(AccStat.LifeOrbDamage, 0.10f), F(AccStat.LifeOrbHpDrain), S(AccStat.LifeOrbGateTicks, 12f) },
				s4: new[] { S(AccStat.LifeOrbDamage, 0.10f), F(AccStat.LifeOrbHpDrain), S(AccStat.LifeOrbGateTicks, 12f) },
				s5: new[] { S(AccStat.LifeOrbDamage, 0.04f) },
				s6: new[] { S(AccStat.LifeOrbDamage, 0.10f), F(AccStat.LifeOrbHpDrain), S(AccStat.LifeOrbGateTicks, 10f) },
				normal: new[] { S(AccStat.LifeOrbDamage, 0.20f), F(AccStat.LifeOrbHpDrain), S(AccStat.LifeOrbGateTicks, 8f) },
				super: new[] { S(AccStat.LifeOrbDamage, 0.40f), F(AccStat.LifeOrbHpDrain), S(AccStat.LifeOrbGateTicks, 4f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.LifeCrystal, 3)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.BloodZombie),
					Boss(NPCID.WallofFlesh),
					Craft(TileID.MythrilAnvil, (ItemID.LifeFruit, 3)),
					Boss(NPCID.Plantera)
				}),

			Fam(AccFamilyId.A17, "A17ShellBell", "贝壳之铃", "Shell Bell", "Bag 贝壳之铃 SV Sprite.png", 4, 7,
				s1: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 30f) },
				s2: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 30f) },
				s3: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 25f) },
				s4: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 25f) },
				s5: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 30f) },
				s6: new[] { S(AccStat.ShellBellHeal, 2f), S(AccStat.ShellBellCdTicks, 20f) },
				normal: new[] { S(AccStat.ShellBellHeal, 4f), S(AccStat.ShellBellCdTicks, 30f) },
				super: new[] { S(AccStat.ShellBellHeal, 8f), S(AccStat.ShellBellCdTicks, 15f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.Seashell, 5), (ItemID.Coral, 3)),
					Crate(ItemID.OceanCrate),
					Event(NPCID.PirateDeckhand),
					Boss(NPCID.EyeofCthulhu),
					Boss(NPCID.DukeFishron),
					CalBoss("OldDuke")
				}),

			Fam(AccFamilyId.A18, "A18RockyHelmet", "凸凸头盔", "Rocky Helmet", "Bag 凸凸头盔 SV Sprite.png", 6, 9,
				s1: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.RockyHelmetCdTicks, 45f), S(AccStat.AccDefense, 6f) },
				s2: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.RockyHelmetCdTicks, 45f), S(AccStat.AccDefense, 6f) },
				s3: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.RockyHelmetCdTicks, 45f), S(AccStat.AccDefense, 6f) },
				s4: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.RockyHelmetCdTicks, 45f), S(AccStat.AccDefense, 6f) },
				s5: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.AccDefense, 6f) },
				s6: new[] { S(AccStat.RockyHelmetScale, 0.25f), S(AccStat.RockyHelmetCdTicks, 22f), S(AccStat.AccDefense, 6f) },
				normal: new[] { S(AccStat.RockyHelmetScale, 1.0f), S(AccStat.RockyHelmetCdTicks, 45f), S(AccStat.AccDefense, 12f) },
				super: new[] { S(AccStat.RockyHelmetScale, 2.0f), S(AccStat.RockyHelmetCdTicks, 22f), S(AccStat.AccDefense, 24f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.StoneBlock, 50), (ItemID.Bone, 10)),
					Crate(ItemID.DungeonFishingCrate),
					Event(NPCID.GoblinWarrior),
					Boss(NPCID.SkeletronHead),
					Craft(TileID.MythrilAnvil, (ItemID.TurtleShell, 1)),
					Boss(NPCID.Golem)
				}),

			Fam(AccFamilyId.A19, "A19ChargeBelt", "充电电池", "Cell Battery", "Bag 充电电池 SV Sprite.png", 3, 6,
				s1: new[] { S(AccStat.EnergyGainAdd, 0.075f) },
				s2: new[] { S(AccStat.EnergyGainAdd, 0.075f) },
				s3: new[] { S(AccStat.EnergyGainAdd, 0.075f) },
				s4: new[] { S(AccStat.EnergyGainAdd, 0.075f) },
				s5: new[] { S(AccStat.EnergyGainAdd, 0.075f) },
				s6: new[] { S(AccStat.PassiveEnergyMul, 1.0f) },
				normal: new[] { S(AccStat.EnergyGainAdd, 0.30f) },
				super: new[] { S(AccStat.EnergyGainAdd, 0.60f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.Wire, 30), (ItemID.FallenStar, 3)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.MartianWalker),
					Boss(NPCID.TheDestroyer),
					Craft(TileID.MythrilAnvil, (ItemID.HallowedBar, 5)),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A20, "A20EchoPendant", "光之黏土", "Light Clay", "Bag 光之黏土 SV Sprite.png", 6, 9,
				s1: new[] { S(AccStat.UltRetain, 0.10f) },
				s2: new[] { S(AccStat.UltRetain, 0.10f) },
				s3: new[] { S(AccStat.UltRetain, 0.10f) },
				s4: new[] { S(AccStat.UltRetain, 0.10f) },
				s5: new[] { S(AccStat.UltRetain, 0.10f) },
				s6: new[] { S(AccStat.CooldownCut, 0.10f) },
				normal: new[] { S(AccStat.UltRetain, 0.40f) },
				super: new[] { S(AccStat.UltRetain, 0.80f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.ManaCrystal, 2), (ItemID.DirtBlock, 50)),
					Crate(ItemID.HallowedFishingCrate),
					Event(NPCID.Pixie),
					Boss(NPCID.Plantera),
					Craft(TileID.MythrilAnvil, (ItemID.ChlorophyteBar, 5)),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A21, "A21BurstArmband", "弱点保险", "Weakness Policy", "Bag 弱点保险 SV Sprite.png", 7, 10,
				s1: new[] { S(AccStat.UltDamageBonus, 0.125f) },
				s2: new[] { S(AccStat.UltDamageBonus, 0.125f) },
				s3: new[] { S(AccStat.UltDamageBonus, 0.125f) },
				s4: new[] { S(AccStat.EnergyGainMul, 0.80f), S(AccStat.UltDamageBonus, 0.30f) },
				s5: new[] { S(AccStat.UltDamageBonus, 0.125f) },
				s6: new[] { S(AccStat.EnergyGainMul, 0.90f), S(AccStat.UltDamageBonus, 0.20f) },
				normal: new[] { S(AccStat.UltDamageBonus, 0.50f), S(AccStat.EnergyGainMul, 0.80f) },
				super: new[] { S(AccStat.UltDamageBonus, 1.00f), S(AccStat.EnergyGainMul, 0.60f) },
				loot: new[]
				{
					Craft(TileID.MythrilAnvil, (ItemID.SoulofFright, 5)),
					Crate(ItemID.HallowedFishingCrate),
					Event(NPCID.Scarecrow1),
					Boss(NPCID.Golem),
					Boss(NPCID.MoonLordCore),
					CalBoss("Providence")
				}),

			Fam(AccFamilyId.A22, "A22ExpShare", "学习装置", "Exp. Share", "Bag 学习装置 SV Sprite.png", 2, 5,
				s1: new[] { S(AccStat.XpHotbarShareMul, 0.10f) },
				s2: new[] { S(AccStat.XpHotbarShareMul, 0.10f) },
				s3: new[] { S(AccStat.XpHotbarShareMul, 0.10f) },
				s4: new[] { S(AccStat.XpHotbarShareMul, 0.10f) },
				s5: new[] { S(AccStat.XpHotbarShareMul, 0.10f) },
				s6: new[] { S(AccStat.XpHeldMul, 0.10f) },
				normal: new[] { S(AccStat.XpHotbarShareMul, 0.40f) },
				super: new[] { S(AccStat.XpHotbarShareMul, 0.80f) },
				loot: new[]
				{
					Craft(TileID.Bookcases, (ItemID.Book, 5), (ItemID.Bone, 10)),
					Crate(ItemID.DungeonFishingCrate),
					Event(NPCID.Mimic),
					Boss(NPCID.SkeletronHead),
					Craft(TileID.MythrilAnvil, (ItemID.ChlorophyteBar, 5)),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A23, "A23LuckyEgg", "幸运蛋", "Lucky Egg", "Bag 幸运蛋 SV Sprite.png", 1, 4,
				s1: new[] { S(AccStat.XpHeldMul, 0.125f) },
				s2: new[] { S(AccStat.XpHeldMul, 0.125f) },
				s3: new[] { S(AccStat.XpHeldMul, 0.125f) },
				s4: new[] { S(AccStat.XpHeldMul, 0.125f) },
				s5: new[] { S(AccStat.XpHeldMul, 0.125f) },
				s6: new[] { S(AccStat.XpHeldMul, 0.125f) },
				normal: new[] { S(AccStat.XpHeldMul, 0.50f) },
				super: new[] { S(AccStat.XpHeldMul, 1.00f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.Sunflower, 5), (ItemID.FallenStar, 5)),
					Crate(ItemID.JungleFishingCrate),
					Event(NPCID.SlimeSpiked),
					Boss(NPCID.QueenBee),
					Boss(NPCID.Plantera),
					Crate(ItemID.JungleFishingCrateHard, 0.10f)
				}),

			Fam(AccFamilyId.A24, "A24Everstone", "不变之石", "Everstone", "Bag 不变之石 SV Sprite.png", 1, 4, untransformed: true,
				s1: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				s2: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				s3: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				s4: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				s5: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				s6: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.005f) },
				normal: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.02f) },
				super: new[] { F(AccStat.EverstoneBlock), S(AccStat.IncomingCut, 0.04f), S(AccStat.UntransformedDefense, 3f), S(AccStat.EvioliteDefMul, 0.06f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.StoneBlock, 99), (ItemID.Amber, 1)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.GraniteGolem),
					Boss(NPCID.SkeletronHead),
					Boss(NPCID.Golem),
					CalBoss("Providence")
				}),

			Fam(AccFamilyId.A25, "A25BlackBelt", "黑带", "Black Belt", "Bag 黑带 SV Sprite.png", 3, 6,
				s1: new[] { S(AccStat.MeleeDeliveryDamage, 0.03f), S(AccStat.DodgeChance, 0.05f) },
				s2: new[] { S(AccStat.MeleeDeliveryDamage, 0.03f), S(AccStat.DodgeChance, 0.05f) },
				s3: new[] { S(AccStat.LungeCooldownCut, 0.075f), S(AccStat.DodgeChance, 0.05f) },
				s4: new[] { S(AccStat.LungeCooldownCut, 0.075f), S(AccStat.DodgeChance, 0.05f) },
				s5: new[] { S(AccStat.MeleeDeliveryDamage, 0.03f), S(AccStat.LungeIFrameBonus, 4f), S(AccStat.DodgeChance, 0.05f) },
				s6: new[] { S(AccStat.LungeIFrameBonus, 5f), S(AccStat.MeleeDeliveryDamage, 0.03f), S(AccStat.DodgeChance, 0.05f) },
				normal: new[] { S(AccStat.MeleeDeliveryDamage, 0.12f), S(AccStat.LungeCooldownCut, 0.15f), S(AccStat.LungeIFrameBonus, 8f), S(AccStat.DodgeChance, 0.30f) },
				super: new[] { S(AccStat.MeleeDeliveryDamage, 0.24f), S(AccStat.LungeCooldownCut, 0.30f), S(AccStat.LungeIFrameBonus, 12f), S(AccStat.DodgeChance, 0.60f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.Leather, 5), (ItemID.Shackle, 1)),
					Crate(ItemID.IronCrate),
					Event(NPCID.GoblinWarrior),
					Boss(NPCID.QueenBee),
					Craft(TileID.MythrilAnvil, (ItemID.WarriorEmblem, 1)),
					Boss(NPCID.Plantera)
				}),

			Fam(AccFamilyId.A26, "A26Leftovers", "吃剩的东西", "Leftovers", "Bag 吃剩的东西 SV Sprite.png", 3, 6,
				s1: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				s2: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				s3: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				s4: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				s5: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				s6: new[] { S(AccStat.LeftoversHpPerSec, 1f) },
				normal: new[] { S(AccStat.LeftoversHpPerSec, 3f) },
				super: new[] { S(AccStat.LeftoversHpPerSec, 6f), S(AccStat.LeftoversLowHpBonus, 6f) },
				loot: new[]
				{
					Craft(TileID.CookingPots, (ItemID.CookedFish, 3), (ItemID.Bowl, 1)),
					Crate(ItemID.OasisCrate),
					Event(NPCID.SlimeSpiked),
					Boss(NPCID.KingSlime),
					Boss(NPCID.DukeFishron),
					Event(NPCID.PirateCaptain)
				}),

			Fam(AccFamilyId.A27, "A27FocusSash", "气势披带", "Focus Sash", "Bag 气势披带 SV Sprite.png", 4, 8,
				s1: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f), S(AccStat.IncomingCut, 0.02f) },
				s2: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f), S(AccStat.IncomingCut, 0.02f) },
				s3: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f) },
				s4: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f) },
				s5: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f) },
				s6: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.60f), S(AccStat.FocusSashCdSec, 90f) },
				normal: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.50f), S(AccStat.FocusSashCdSec, 60f), S(AccStat.IncomingCut, 0.04f) },
				super: new[] { F(AccStat.FocusSash), S(AccStat.FocusSashHpPct, 0.30f), S(AccStat.FocusSashCdSec, 30f), S(AccStat.IncomingCut, 0.08f), S(AccStat.FocusSashImmuneTicks, 60f) },
				loot: new[]
				{
					Craft(TileID.WorkBenches, (ItemID.AdhesiveBandage, 2), (ItemID.LifeCrystal, 1)),
					Crate(ItemID.CorruptFishingCrate),
					Event(NPCID.BloodZombie),
					Boss(NPCID.WallofFlesh),
					Boss(NPCID.Golem),
					Boss(NPCID.MoonLordCore)
				}),

			Fam(AccFamilyId.A28, "A28Eviolite", "进化奇石", "Eviolite", "Bag 进化奇石 SV Sprite.png", 4, 7,
				s1: new[] { S(AccStat.EvioliteDefMul, 0.05f) },
				s2: new[] { S(AccStat.EvioliteDefMul, 0.05f) },
				s3: new[] { S(AccStat.EvioliteDefMul, 0.05f) },
				s4: new[] { S(AccStat.EvioliteDefMul, 0.05f) },
				s5: new[] { S(AccStat.EvioliteDefMul, 0.05f) },
				s6: new[] { S(AccStat.EvioliteDamage, 0.08f) },
				normal: new[] { S(AccStat.EvioliteDefMul, 0.20f), S(AccStat.EvioliteDamage, 0.05f) },
				super: new[] { S(AccStat.EvioliteDefMul, 0.40f), S(AccStat.EvioliteDamage, 0.10f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.MeteoriteBar, 5), (ItemID.LifeCrystal, 1)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.MeteorHead),
					Boss(NPCID.Deerclops),
					Craft(TileID.MythrilAnvil, (ItemID.ChlorophyteBar, 8)),
					Boss(NPCID.Plantera)
				}),

			// 仅皮卡丘（L04_F01）；接受永久毕业。攻速走 UseTimeMul，不占 CooldownCut 硬顶。
			Fam(AccFamilyId.A29, "A29LightBall", "电气球", "Light Ball", "Bag 电气球 SV Sprite.png", 2, 5,
				s1: new[] { S(AccStat.FormAtkMul, 0.25f) },
				s2: new[] { S(AccStat.FormDefMul, 0.25f) },
				s3: new[] { S(AccStat.FormAtkMul, 0.25f) },
				s4: new[] { S(AccStat.FormDefMul, 0.25f) },
				s5: new[] { S(AccStat.FormAtkMul, 0.25f) },
				s6: new[] { S(AccStat.FormDefMul, 0.25f) },
				normal: new[] { S(AccStat.FormAtkMul, 1.0f), S(AccStat.FormDefMul, 1.0f), S(AccStat.UseTimeMul, 0.90f) },
				super: new[] { S(AccStat.FormAtkMul, 2.0f), S(AccStat.FormDefMul, 2.0f), S(AccStat.UseTimeMul, 0.80f) },
				loot: new[]
				{
					Craft(TileID.Anvils, (ItemID.Wire, 15), (ItemID.FallenStar, 5)),
					Crate(ItemID.GoldenCrate),
					Event(NPCID.Pixie),
					Boss(NPCID.QueenBee),
					Craft(TileID.MythrilAnvil, (ItemID.Wire, 40), (ItemID.LightShard, 3), (ItemID.SoulofLight, 5)),
					Boss(NPCID.TheDestroyer)
				},
				requiredFormId: "L04_F01")
		};
	}
}
