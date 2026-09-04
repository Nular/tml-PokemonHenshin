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
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F01", 20, "Mods.PokemonHenshin.Items.MetangForce.DisplayName", PokemonType.Steel, 9, null, FormPassiveKind.ClearBody, secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Confusion", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.2f, 22, 8f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Tackle", 1.2f, 16, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.TakeDown", 1.9f, 30, DustID.Iron, recoil: true);
	}

	public class MetagrossForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F02", 21, "Mods.PokemonHenshin.Items.MetagrossForce.DisplayName", PokemonType.Steel, 12, "L08_F01", FormPassiveKind.ClearBody, secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Psychic", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.7f, 20, 10f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MeteorMash", 1.8f, 18, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Beam("Mods.PokemonHenshin.Moves.HyperBeam", 2.4f, aftermath: true);
	}

	public class MagikarpForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(2);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F01", 22, "Mods.PokemonHenshin.Items.MagikarpForce.DisplayName", PokemonType.Water, 2, null, FormPassiveKind.SwiftSwim, FormRole.Utility, PokemonType.Normal);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Splash", 0.25f, 30, DustID.Water);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Tackle", 0.6f, 22, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Flail", 0.8f, 16, DustID.Water);
	}

	public class GyaradosForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F02", 23, "Mods.PokemonHenshin.Items.GyaradosForce.DisplayName", PokemonType.Water, 6, "L09_F01", FormPassiveKind.Moxie, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.WaterBolt("Mods.PokemonHenshin.Moves.HydroPump", 1.6f, 28);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Crunch", 1.5f, 20, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Beam("Mods.PokemonHenshin.Moves.HyperBeam", 2.2f, aftermath: true);
	}

	public class DiglettForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(2);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F01", 24, "Mods.PokemonHenshin.Items.DiglettForce.DisplayName", PokemonType.Ground, 2, null, FormPassiveKind.SandVeil, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MudSlap", 1f, 16, DustID.Dirt);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Tackle", 1f, 18, DustID.Dirt);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Dig("Mods.PokemonHenshin.Moves.Dig", 1.2f, 14);
	}

	public class DugtrioForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F02", 25, "Mods.PokemonHenshin.Items.DugtrioForce.DisplayName", PokemonType.Ground, 5, "L10_F01", FormPassiveKind.SandVeil, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.TripleDig", 1.3f, 12, DustID.Dirt);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Dig("Mods.PokemonHenshin.Moves.Dig", 1.3f, 12);
		protected override MoveSpec CreateUltimate() => FormItemUtil.AoE("Mods.PokemonHenshin.Moves.Earthquake", 2.0f, DustID.Dirt);
	}

	public class PidgeyForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F01", 26, "Mods.PokemonHenshin.Items.PidgeyForce.DisplayName", PokemonType.Normal, 3, null, FormPassiveKind.KeenEye, FormRole.Utility, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.GroundCyclone("Mods.PokemonHenshin.Moves.Gust");
		protected override MoveSpec CreateMoveB() => FormItemUtil.PeckCone("Mods.PokemonHenshin.Moves.Peck");
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.AerialAce", 1.5f, 22, DustID.Cloud);
	}

	public class PidgeotForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F02", 27, "Mods.PokemonHenshin.Items.PidgeotForce.DisplayName", PokemonType.Normal, 8, "L11_F01", FormPassiveKind.KeenEye, FormRole.Utility, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.AoE("Mods.PokemonHenshin.Moves.Hurricane", 1.6f, DustID.Cloud);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.AerialAce", 1.6f, 18, DustID.Cloud);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.BraveBird", 2.1f, 28, DustID.Cloud, recoil: true);
	}

	public class AbraForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F01", 28, "Mods.PokemonHenshin.Items.AbraForce.DisplayName", PokemonType.Psychic, 3, null, FormPassiveKind.Synchronize, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Confusion", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.1f, 22, 8f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.ZenHammer("Mods.PokemonHenshin.Moves.ZenHeadbutt");
		protected override MoveSpec CreateUltimate() => FormItemUtil.ResonanceScatterUlt("Mods.PokemonHenshin.Moves.Psychic");
	}

	public class AlakazamForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F02", 29, "Mods.PokemonHenshin.Items.AlakazamForce.DisplayName", PokemonType.Psychic, 9, "L12_F01", FormPassiveKind.Synchronize, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Psychic", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.7f, 20, 10f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.FocusPunch", 1.8f, 36, DustID.MagicMirror);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Beam("Mods.PokemonHenshin.Moves.FutureSight", 2.0f, ignoreDef: true);
	}

	public class OnixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F01", 30, "Mods.PokemonHenshin.Items.OnixForce.DisplayName", PokemonType.Rock, 5, null, FormPassiveKind.RockHead, FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.RockTomb", ModContent.ProjectileType<GenericBoltProj>(), 1.2f, 20, 8f, ai0: DustID.Stone);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Tackle", 1.15f, 18, DustID.Stone);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.RockSlide", 1.7f, DustID.Stone);
	}

	public class SteelixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F02", 31, "Mods.PokemonHenshin.Items.SteelixForce.DisplayName", PokemonType.Rock, 10, "L13_F01", FormPassiveKind.RockHead, FormRole.Utility, PokemonType.Steel);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.RockSlide", 1.5f, DustID.Stone);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.IronTail", 1.6f, 16, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.TakeDown", 2.0f, 28, DustID.Iron, recoil: true);
	}

	public class MewtwoForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L14_F01", 32, "Mods.PokemonHenshin.Items.MewtwoForce.DisplayName", PokemonType.Psychic, 10, null, FormPassiveKind.Pressure, FormRole.Legendary);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Psychic", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.8f, 18, 11f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.ZenHeadbutt", 1.6f, 18, DustID.MagicMirror);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Beam("Mods.PokemonHenshin.Moves.Psystrike", 2.3f, ignoreDef: true);
	}

	public class GibleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F01", 33, "Mods.PokemonHenshin.Items.GibleForce.DisplayName", PokemonType.Dragon, 11, null, FormPassiveKind.RoughSkin, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonRage", ModContent.ProjectileType<GenericBoltProj>(), 1.4f, 22, 10f, ai0: DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Tackle", 1.3f, 16, DustID.Dirt);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Vortex("Mods.PokemonHenshin.Moves.SandTomb", 1.6f, DustID.Sand);
	}

	public class GarchompForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F02", 34, "Mods.PokemonHenshin.Items.GarchompForce.DisplayName", PokemonType.Dragon, 12, "L15_F01", FormPassiveKind.RoughSkin, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonPulse", ModContent.ProjectileType<GenericBoltProj>(), 1.7f, 18, 12f, ai0: DustID.PurpleTorch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Crunch", 1.7f, 16, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.DracoMeteor", 2.4f, DustID.Torch);
	}

	public class LugiaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L16_F01", 35, "Mods.PokemonHenshin.Items.LugiaForce.DisplayName", PokemonType.Psychic, 11, null, FormPassiveKind.Pressure, FormRole.Legendary, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Aeroblast", ModContent.ProjectileType<GenericBoltProj>(), 1.9f, 20, 13f, ai0: DustID.Cloud, easyCrit: true);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.SkyAttack", 1.8f, 34, DustID.Cloud);
		protected override MoveSpec CreateUltimate() => FormItemUtil.AoE("Mods.PokemonHenshin.Moves.Aeroblast", 2.2f, DustID.Cloud);
	}

	public class RayquazaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L17_F01", 36, "Mods.PokemonHenshin.Items.RayquazaForce.DisplayName", PokemonType.Dragon, 12, null, FormPassiveKind.AirLock, FormRole.Legendary, PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonPulse", ModContent.ProjectileType<GenericBoltProj>(), 1.9f, 16, 13f, ai0: DustID.Torch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Crunch", 1.8f, 14, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Beam("Mods.PokemonHenshin.Moves.DragonAscent", 2.6f, ignoreDef: true);
	}
}
