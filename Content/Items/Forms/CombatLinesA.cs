using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	public class PikachuForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(2);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L04_F01", 10, "Mods.PokemonHenshin.Items.PikachuForce.DisplayName", PokemonType.Electric, 2, null);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.ElectroBall", ModContent.ProjectileType<ThunderBoltHenshinProj>(), 1.1f, 20, 11f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.QuickAttack", 0.9f, 12, DustID.Electric);
	}

	public class RaichuForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L04_F02", 11, "Mods.PokemonHenshin.Items.RaichuForce.DisplayName", PokemonType.Electric, 5, "L04_F01");
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Thunderbolt", ModContent.ProjectileType<ThunderBoltHenshinProj>(), 1.5f, 22, 13f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Thunder", ModContent.ProjectileType<ThunderBoltHenshinProj>(), 1.8f, 40, 0f);
	}

	public class MachopForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F01", 12, "Mods.PokemonHenshin.Items.MachopForce.DisplayName", PokemonType.Fighting, 3, null);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.KarateChop", 1.15f, 16, DustID.Blood);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.SeismicToss", 1.4f, 32, DustID.Blood);
	}

	public class MachokeForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F02", 13, "Mods.PokemonHenshin.Items.MachokeForce.DisplayName", PokemonType.Fighting, 6, "L05_F01");
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Submission", 1.3f, 15, DustID.Blood);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DynamicPunch", 1.7f, 28, DustID.Blood);
	}

	public class MachampForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F03", 14, "Mods.PokemonHenshin.Items.MachampForce.DisplayName", PokemonType.Fighting, 9, "L05_F02");
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.CrossChop", 1.5f, 12, DustID.Blood);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.CloseCombat", 2f, 22, DustID.Blood);
	}

	public class HaunterForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F01", 15, "Mods.PokemonHenshin.Items.HaunterForce.DisplayName", PokemonType.Ghost, 8, null, secondary: PokemonType.Poison, phasing: true);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.ShadowBall", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.35f, 24, 9f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.Phase",
			ProjectileType = ModContent.ProjectileType<PhaseTriggerProj>(),
			DamageMultiplier = 0f,
			UseTime = 30,
			ShootSpeed = 0f,
			GrantsPhasing = true,
			KeyConflict = KeyConflictLevel.RightClick,
			Collision = CollisionTier.C
		};
	}

	public class GengarForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F02", 16, "Mods.PokemonHenshin.Items.GengarForce.DisplayName", PokemonType.Ghost, 10, "L06_F01", secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.ShadowBurst", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.5f, 16, 10f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.NightShade", 1.2f, 36, DustID.Shadowflame);
	}

	public class DratiniForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F01", 17, "Mods.PokemonHenshin.Items.DratiniForce.DisplayName", PokemonType.Dragon, 6, null);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonPulse", ModContent.ProjectileType<GenericBoltProj>(), 1.3f, 24, 10f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Twister", 1.1f, 28, DustID.Cloud);
	}

	public class DragonairForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F02", 18, "Mods.PokemonHenshin.Items.DragonairForce.DisplayName", PokemonType.Dragon, 8, "L07_F01");
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonBreath", ModContent.ProjectileType<GenericBoltProj>(), 1.45f, 20, 11f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonDance", 0.5f, 40, DustID.Cloud);
	}

	public class DragoniteForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F03", 19, "Mods.PokemonHenshin.Items.DragoniteForce.DisplayName", PokemonType.Dragon, 11, "L07_F02", secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Outrage", 1.8f, 14, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.HyperBeam", ModContent.ProjectileType<GenericBoltProj>(), 2.2f, 45, 14f);
	}
}
