using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	/// <summary>形态物品构造辅助：统一 NetworkId / 贴图路径 / 占位招式。</summary>
	internal static class FormItemUtil
	{
		public static FormDefinition Def(
			string formId, ushort netId, string displayKey, PokemonType primary,
			int stage, string evolvesFrom, FormRole role = FormRole.Combat,
			PokemonType secondary = PokemonType.None, bool phasing = false, float dmgFactor = 1f)
			=> new()
			{
				FormId = formId,
				NetworkId = netId,
				DisplayNameKey = displayKey,
				TexturePath = "PokemonHenshin/Assets/Forms/" + formId,
				TextureFacesLeft = true,
				Primary = primary,
				Secondary = secondary,
				Stage = stage,
				EvolvesFrom = evolvesFrom,
				Collision = phasing ? CollisionTier.C : CollisionTier.A,
				GrantsPhasing = phasing,
				HenshinDamageFactor = dmgFactor,
				Role = role
			};

		public static MoveSpec Slash(string nameKey, float mult = 1f, int use = 18, int dust = DustID.Smoke)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<GenericSlashProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				KeyConflict = KeyConflictLevel.None,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec FireBolt(string nameKey, float mult = 1.3f, int use = 28)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<EmberBoltProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 9f,
				Knockback = 1.5f,
				KeyConflict = KeyConflictLevel.RightClick,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec WaterBolt(string nameKey, float mult = 1.2f, int use = 26)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<WaterBoltHenshinProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 10f,
				Knockback = 2f,
				KeyConflict = KeyConflictLevel.RightClick,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec Bolt(string nameKey, int projType, float mult, int use, float speed, KeyConflictLevel key = KeyConflictLevel.RightClick)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = projType,
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = speed,
				Knockback = 2f,
				KeyConflict = key,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec ScratchA() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.Scratch",
			ProjectileType = ModContent.ProjectileType<ScratchSlashProj>(),
			DamageMultiplier = 1f,
			UseTime = 18,
			ShootSpeed = 0f,
			Knockback = 3f
		};

		public static int StageDamage(int stage) => 8 + stage * 6;
	}

	public class CharmanderForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F01", 1, "Mods.PokemonHenshin.Items.CharmanderForce.DisplayName", PokemonType.Fire, 1, null);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ScratchA();
		protected override MoveSpec CreateMoveB() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.Ember");
	}

	public class CharmeleonForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F02", 2, "Mods.PokemonHenshin.Items.CharmeleonForce.DisplayName", PokemonType.Fire, 4, "L01_F01");
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MultiScratch", 1.1f, 14, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.FlameCone", 1.45f, 24);
	}

	public class CharizardForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F03", 3, "Mods.PokemonHenshin.Items.CharizardForce.DisplayName", PokemonType.Fire, 7, "L01_F02", secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonClaw", 1.25f, 16, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.FireBarrage", 1.6f, 20);
	}

	public class SquirtleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F01", 4, "Mods.PokemonHenshin.Items.SquirtleForce.DisplayName", PokemonType.Water, 1, null);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.WaterGun", ModContent.ProjectileType<WaterBoltHenshinProj>(), 1f, 22, 10f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Withdraw", 0.4f, 40, DustID.Water);
	}

	public class WartortleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F02", 5, "Mods.PokemonHenshin.Items.WartortleForce.DisplayName", PokemonType.Water, 4, "L02_F01");
		protected override MoveSpec CreateMoveA() => FormItemUtil.WaterBolt("Mods.PokemonHenshin.Moves.WaterPulse", 1.25f, 18);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.TidePush", 1.1f, 30, DustID.Water);
	}

	public class BlastoiseForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F03", 6, "Mods.PokemonHenshin.Items.BlastoiseForce.DisplayName", PokemonType.Water, 7, "L02_F02");
		protected override MoveSpec CreateMoveA() => FormItemUtil.WaterBolt("Mods.PokemonHenshin.Moves.HydroPump", 1.7f, 32);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.RainField", ModContent.ProjectileType<RainFieldProj>(), 0.1f, 60, 0f);
	}

	public class BulbasaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F01", 7, "Mods.PokemonHenshin.Items.BulbasaurForce.DisplayName", PokemonType.Grass, 1, null, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.VineWhip", 1f, 18, DustID.Grass);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.PoisonPowder", ModContent.ProjectileType<GenericBoltProj>(), 0.6f, 36, 6f);
	}

	public class IvysaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F02", 8, "Mods.PokemonHenshin.Items.IvysaurForce.DisplayName", PokemonType.Grass, 4, "L03_F01", secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.LeechSeed", 0.9f, 24, DustID.Grass);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.SludgeBomb", ModContent.ProjectileType<GenericBoltProj>(), 1.35f, 28, 8f);
	}

	public class VenusaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F03", 9, "Mods.PokemonHenshin.Items.VenusaurForce.DisplayName", PokemonType.Grass, 7, "L03_F02", secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.SolarBeam", 1.8f, 40); // 占位：强力弹
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.SeedGun", ModContent.ProjectileType<GenericBoltProj>(), 1.2f, 12, 11f);
	}
}
