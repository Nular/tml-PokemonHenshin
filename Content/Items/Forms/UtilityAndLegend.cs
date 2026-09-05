using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	public class MetangForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F01", 20, "Mods.PokemonHenshin.Items.MetangForce.DisplayName", PokemonType.Steel, 7, null, FormPassiveKind.ClearBody, secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Confusion", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.2f, 22, 8f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.2f, 16, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.TakeDown", 3.2f, 30, DustID.Iron, recoil: true, recoilFrac: 0.25f, key: KeyConflictLevel.ModKeybind);
	}

	public class MetagrossForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L08_F02", 21, "Mods.PokemonHenshin.Items.MetagrossForce.DisplayName", PokemonType.Steel, 10, "L08_F01", FormPassiveKind.ClearBody, secondary: PokemonType.Psychic);
		protected override MoveSpec CreateMoveA() => FormItemUtil.StrongPsychic("Mods.PokemonHenshin.Moves.Psychic", 1.7f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.MeteorMash", 1.8f, 18, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.HyperBeamUlt("Mods.PokemonHenshin.Moves.HyperBeam", 4.2f);
	}

	public class MagikarpForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F01", 22, "Mods.PokemonHenshin.Items.MagikarpForce.DisplayName", PokemonType.Water, 1, null, FormPassiveKind.SwiftSwim, role: FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.Splash", 0.25f, DustID.Water);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 0.6f, 22, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.FlailUlt("Mods.PokemonHenshin.Moves.Flail", 1.5f);
	}

	public class GyaradosForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L09_F02", 23, "Mods.PokemonHenshin.Items.GyaradosForce.DisplayName", PokemonType.Water, 9, "L09_F01", FormPassiveKind.Moxie, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.HydroPump", 1.6f, DustID.Water, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Crunch", 1.5f, brokenArmorTicks: 180);
		protected override MoveSpec CreateUltimate() => FormItemUtil.HyperBeamUlt("Mods.PokemonHenshin.Moves.HyperBeam", 4.0f);
	}

	public class DiglettForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F01", 24, "Mods.PokemonHenshin.Items.DiglettForce.DisplayName", PokemonType.Ground, 3, null, FormPassiveKind.SandVeil, role: FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.MudSlap("Mods.PokemonHenshin.Moves.MudSlap");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1f, 18, DustID.Dirt);
		protected override MoveSpec CreateUltimate() => FormItemUtil.DigUlt("Mods.PokemonHenshin.Moves.Dig", 3.0f);
	}

	public class DugtrioForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L10_F02", 25, "Mods.PokemonHenshin.Items.DugtrioForce.DisplayName", PokemonType.Ground, 6, "L10_F01", FormPassiveKind.SandVeil, role: FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.TripleStab("Mods.PokemonHenshin.Moves.TripleDig", 1.3f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Dig("Mods.PokemonHenshin.Moves.Dig", 1.3f, 12);
		protected override MoveSpec CreateUltimate() => FormItemUtil.QuakeUlt("Mods.PokemonHenshin.Moves.Earthquake", 3.6f);
	}

	public class PidgeyForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(2);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F01", 26, "Mods.PokemonHenshin.Items.PidgeyForce.DisplayName", PokemonType.Flying, 2, null, FormPassiveKind.KeenEye, secondary: PokemonType.Normal, role: FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.GroundCyclone("Mods.PokemonHenshin.Moves.Gust");
		protected override MoveSpec CreateMoveB() => FormItemUtil.PeckCone("Mods.PokemonHenshin.Moves.Peck");
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.AerialAce", 2.8f, 18, DustID.Cloud, key: KeyConflictLevel.ModKeybind);
	}

	public class PidgeotForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L11_F02", 27, "Mods.PokemonHenshin.Items.PidgeotForce.DisplayName", PokemonType.Flying, 7, "L11_F01", FormPassiveKind.KeenEye, secondary: PokemonType.Normal, role: FormRole.Utility);
		protected override MoveSpec CreateMoveA() => FormItemUtil.HurricaneField("Mods.PokemonHenshin.Moves.Hurricane", 1.6f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BlinkStrike("Mods.PokemonHenshin.Moves.AerialAce", 1.6f, 16);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.BraveBird", 3.8f, 28, DustID.Cloud, recoil: true, key: KeyConflictLevel.ModKeybind);
	}

	public class AbraForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F01", 28, "Mods.PokemonHenshin.Items.AbraForce.DisplayName", PokemonType.Psychic, 4, null, FormPassiveKind.Synchronize);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Confusion", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.1f, 22, 8f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.ZenHammer("Mods.PokemonHenshin.Moves.ZenHeadbutt");
		protected override MoveSpec CreateUltimate() => FormItemUtil.ResonanceScatterUlt("Mods.PokemonHenshin.Moves.Psychic");
	}

	public class AlakazamForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L12_F02", 29, "Mods.PokemonHenshin.Items.AlakazamForce.DisplayName", PokemonType.Psychic, 9, "L12_F01", FormPassiveKind.Synchronize);
		protected override MoveSpec CreateMoveA() => FormItemUtil.StrongPsychic("Mods.PokemonHenshin.Moves.Psychic", 1.7f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.DynamicPunchUlt("Mods.PokemonHenshin.Moves.FocusPunch", 1.8f, ult: false);
		protected override MoveSpec CreateUltimate() => FormItemUtil.FutureSightUlt("Mods.PokemonHenshin.Moves.FutureSight", 3.8f);
	}

	public class OnixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F01", 30, "Mods.PokemonHenshin.Items.OnixForce.DisplayName", PokemonType.Rock, 5, null, FormPassiveKind.RockHead, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.RockTomb("Mods.PokemonHenshin.Moves.RockTomb");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.15f, 18, DustID.Stone);
		protected override MoveSpec CreateUltimate() => FormItemUtil.RockSlideX("Mods.PokemonHenshin.Moves.RockSlide", 6, 3.2f, ult: true);
	}

	public class SteelixForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L13_F02", 31, "Mods.PokemonHenshin.Items.SteelixForce.DisplayName", PokemonType.Steel, 9, "L13_F01", FormPassiveKind.RockHead, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.RockSlideX("Mods.PokemonHenshin.Moves.RockSlide", 3, 1.5f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.IronTail", 1.6f, 16, DustID.Iron);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.TakeDown", 3.6f, 28, DustID.Iron, recoil: true, key: KeyConflictLevel.ModKeybind);
	}

	public class MewtwoForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L14_F01", 32, "Mods.PokemonHenshin.Items.MewtwoForce.DisplayName", PokemonType.Psychic, 12, null, FormPassiveKind.Pressure, role: FormRole.Legendary, dmgFactor: 1.15f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.StrongPsychic("Mods.PokemonHenshin.Moves.Psychic", 1.8f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.ZenHammer("Mods.PokemonHenshin.Moves.ZenHeadbutt", 5f, 24);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseAoE("Mods.PokemonHenshin.Moves.Psystrike", 4.8f, DustID.PurpleTorch, ignoreDef: true);
	}

	public class GibleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F01", 33, "Mods.PokemonHenshin.Items.GibleForce.DisplayName", PokemonType.Dragon, 6, null, FormPassiveKind.RoughSkin, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonRage", 1.4f, DustID.Torch, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.3f, 16, DustID.Dirt);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseVortex("Mods.PokemonHenshin.Moves.SandTomb", 3.2f, DustID.Sand);
	}

	public class GarchompForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L15_F02", 34, "Mods.PokemonHenshin.Items.GarchompForce.DisplayName", PokemonType.Dragon, 11, "L15_F01", FormPassiveKind.RoughSkin, secondary: PokemonType.Ground);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonPulse", 1.7f, DustID.PurpleTorch, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Crunch", 1.7f, brokenArmorTicks: 200);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MeteorBarrageUlt("Mods.PokemonHenshin.Moves.DracoMeteor", 8, 4.2f);
	}

	public class LugiaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L16_F01", 35, "Mods.PokemonHenshin.Items.LugiaForce.DisplayName", PokemonType.Psychic, 12, null, FormPassiveKind.Pressure, secondary: PokemonType.Flying, role: FormRole.Legendary, dmgFactor: 1.15f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.AirBurst("Mods.PokemonHenshin.Moves.Aeroblast", 1.9f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.SkyAttack", 1.8f, 34, DustID.Cloud);
		protected override MoveSpec CreateUltimate() => FormItemUtil.HurricaneField("Mods.PokemonHenshin.Moves.Aeroblast", 4.5f, ult: true);
	}

	public class RayquazaForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(12);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L17_F01", 36, "Mods.PokemonHenshin.Items.RayquazaForce.DisplayName", PokemonType.Dragon, 12, null, FormPassiveKind.AirLock, secondary: PokemonType.Flying, role: FormRole.Legendary, dmgFactor: 1.2f);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonPulse", 1.9f, DustID.Torch, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Crunch", 1.8f, brokenArmorTicks: 200);
		protected override MoveSpec CreateUltimate() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.DragonAscent",
			ProjectileType = ModContent.ProjectileType<LungeProj>(),
			DamageMultiplier = 5.0f,
			UseTime = 36,
			ShootSpeed = 0f,
			Knockback = 5f,
			Ai0 = DustID.Torch,
			RequiresLungeCooldown = true,
			IgnoreDefensePartial = true,
			AftermathDamagePenaltyTicks = 300,
			AftermathDamagePenalty = 0.8f,
			KeyConflict = KeyConflictLevel.ModKeybind
		};
	}
}
