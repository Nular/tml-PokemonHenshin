using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	public class MetangForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F01", 20, "Mods.PokemonHenshin.Items.MetangForce.DisplayName", PokemonType.Steel, 9, null, secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MetalClaw", 1.4f, 16, DustID.Iron);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.BulletPunch", ModContent.ProjectileType<GenericBoltProj>(), 1.2f, 10, 12f);
	}

	public class MetagrossForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F02", 21, "Mods.PokemonHenshin.Items.MetagrossForce.DisplayName", PokemonType.Steel, 12, "L08_F01", secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MeteorMash", 1.9f, 18, DustID.Iron);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.GravityWell", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.6f, 36, 6f);
	}

	public class MagikarpForce : HenshinForceItem
	{
		protected override int BaseDamage => 4;
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F01", 22, "Mods.PokemonHenshin.Items.MagikarpForce.DisplayName", PokemonType.Water, 2, null, FormRole.Utility, PokemonType.Normal, dmgFactor: 0.5f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Splash", 0.2f, 30, DustID.Water);
		protected override MoveSpec CreateMoveB() => FormItemUtil.WaterBolt("Mods.PokemonHenshin.Moves.Bubble", 0.5f, 28);
	}

	public class GyaradosForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F02", 23, "Mods.PokemonHenshin.Items.GyaradosForce.DisplayName", PokemonType.Water, 6, "L09_F01", secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.WaterBolt("Mods.PokemonHenshin.Moves.AquaTail", 1.5f, 22);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Crunch", 1.6f, 26, DustID.Blood);
	}

	public class DiglettForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(2);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F01", 24, "Mods.PokemonHenshin.Items.DiglettForce.DisplayName", PokemonType.Ground, 2, null, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MudSlap", 1f, 16, DustID.Dirt);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Dig", ModContent.ProjectileType<DigBurstProj>(), 0.8f, 20, 0f);
	}

	public class DugtrioForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F02", 25, "Mods.PokemonHenshin.Items.DugtrioForce.DisplayName", PokemonType.Ground, 5, "L10_F01", FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.TripleDig", 1.3f, 12, DustID.Dirt);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Dig", ModContent.ProjectileType<DigBurstProj>(), 1.1f, 14, 0f);
	}

	public class PidgeyForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F01", 26, "Mods.PokemonHenshin.Items.PidgeyForce.DisplayName", PokemonType.Normal, 3, null, FormRole.Utility, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Gust", ModContent.ProjectileType<GenericBoltProj>(), 1f, 20, 9f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.WingAttack", 1.1f, 22, DustID.Cloud);
	}

	public class PidgeotForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F02", 27, "Mods.PokemonHenshin.Items.PidgeotForce.DisplayName", PokemonType.Normal, 8, "L11_F01", FormRole.Utility, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.AirSlash", ModContent.ProjectileType<GenericBoltProj>(), 1.5f, 18, 12f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.SkyAttack", 1.8f, 36, DustID.Cloud);
	}

	public class AbraForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F01", 28, "Mods.PokemonHenshin.Items.AbraForce.DisplayName", PokemonType.Psychic, 3, null, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Confusion", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.1f, 22, 8f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Teleport", 0.3f, 50, DustID.MagicMirror);
	}

	public class AlakazamForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F02", 29, "Mods.PokemonHenshin.Items.AlakazamForce.DisplayName", PokemonType.Psychic, 9, "L12_F01", FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Psychic", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.7f, 20, 10f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.FutureSight", 1.5f, 40, DustID.MagicMirror);
	}

	public class OnixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F01", 30, "Mods.PokemonHenshin.Items.OnixForce.DisplayName", PokemonType.Rock, 5, null, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.RockThrow", 1.2f, 18, DustID.Stone);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Dig", ModContent.ProjectileType<DigBurstProj>(), 1f, 16, 0f);
	}

	public class SteelixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F02", 31, "Mods.PokemonHenshin.Items.SteelixForce.DisplayName", PokemonType.Rock, 10, "L13_F01", FormRole.Utility, PokemonType.Steel);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.IronTail", 1.6f, 16, DustID.Iron);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Dig", ModContent.ProjectileType<DigBurstProj>(), 1.3f, 12, 0f);
	}

	public class MewtwoForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L14_F01", 32, "Mods.PokemonHenshin.Items.MewtwoForce.DisplayName", PokemonType.Psychic, 10, null, FormRole.Legendary, dmgFactor: 1.15f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Psystrike", ModContent.ProjectileType<ShadowBallHenshinProj>(), 2f, 18, 12f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Barrier", 0.5f, 40, DustID.MagicMirror);
	}

	public class GibleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F01", 33, "Mods.PokemonHenshin.Items.GibleForce.DisplayName", PokemonType.Dragon, 11, null, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonClaw", 1.5f, 15, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.SandField", ModContent.ProjectileType<RainFieldProj>(), 0.2f, 50, 0f); // 占位：复用场逻辑标签仍为雨，后续可换沙
	}

	public class GarchompForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F02", 34, "Mods.PokemonHenshin.Items.GarchompForce.DisplayName", PokemonType.Dragon, 12, "L15_F01", secondary: PokemonType.Ground, dmgFactor: 1.1f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Outrage", 2f, 12, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Dig", ModContent.ProjectileType<DigBurstProj>(), 1.5f, 10, 0f);
	}

	public class LugiaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L16_F01", 35, "Mods.PokemonHenshin.Items.LugiaForce.DisplayName", PokemonType.Psychic, 11, null, FormRole.Legendary, PokemonType.Flying, dmgFactor: 1.2f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Aeroblast", ModContent.ProjectileType<GenericBoltProj>(), 2.1f, 22, 13f, KeyConflictLevel.None);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.RainField", ModContent.ProjectileType<RainFieldProj>(), 0.2f, 55, 0f);
	}

	public class RayquazaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L17_F01", 36, "Mods.PokemonHenshin.Items.RayquazaForce.DisplayName", PokemonType.Dragon, 12, null, FormRole.Legendary, PokemonType.Flying, dmgFactor: 1.25f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonAscent", 2.2f, 14, DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.HyperBeam", ModContent.ProjectileType<GenericBoltProj>(), 2.5f, 36, 15f);
	}
}
