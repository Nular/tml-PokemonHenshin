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
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L04_F01", 10, "Mods.PokemonHenshin.Items.PikachuForce.DisplayName", PokemonType.Electric, 2, null, FormPassiveKind.Static);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.ThunderShock", ModContent.ProjectileType<ThunderBoltHenshinProj>(), 1.1f, 18, 11f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BlinkStrike("Mods.PokemonHenshin.Moves.QuickAttack");
		protected override MoveSpec CreateUltimate() => FormItemUtil.ThunderboltUlt("Mods.PokemonHenshin.Moves.Thunderbolt");
	}

	public class RaichuForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(5);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L04_F02", 11, "Mods.PokemonHenshin.Items.RaichuForce.DisplayName", PokemonType.Electric, 5, "L04_F01", FormPassiveKind.Static);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.Thunderbolt", ModContent.ProjectileType<ThunderBoltHenshinProj>(), 1.5f, 20, 13f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.VoltTackle", 1.8f, 30, DustID.Electric, recoil: true);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.Thunder", 2.0f, DustID.Electric);
	}

	public class MachopForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(3);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F01", 12, "Mods.PokemonHenshin.Items.MachopForce.DisplayName", PokemonType.Fighting, 3, null, FormPassiveKind.Guts);
		protected override MoveSpec CreateMoveA() => FormItemUtil.RockTomb("Mods.PokemonHenshin.Moves.RockTomb");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.1f, 16, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.CrossChopUlt("Mods.PokemonHenshin.Moves.CrossChop");
	}

	public class MachokeForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F02", 13, "Mods.PokemonHenshin.Items.MachokeForce.DisplayName", PokemonType.Fighting, 6, "L05_F01", FormPassiveKind.Guts);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Strike("Mods.PokemonHenshin.Moves.RockSlide", 1.5f, DustID.Stone);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.BrickBreak", 1.4f, 18, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DynamicPunch", 2.0f, 32, DustID.Blood);
	}

	public class MachampForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F03", 14, "Mods.PokemonHenshin.Items.MachampForce.DisplayName", PokemonType.Fighting, 9, "L05_F02", FormPassiveKind.Guts);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.StoneEdge", ModContent.ProjectileType<GenericBoltProj>(), 1.7f, 20, 11f, ai0: DustID.Stone, easyCrit: true);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.CrossChop", 1.6f, 16, DustID.Blood, easyCrit: true);
		protected override MoveSpec CreateUltimate() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.CloseCombat",
			ProjectileType = ModContent.ProjectileType<AoEBurstProj>(),
			DamageMultiplier = 2.2f,
			UseTime = 30,
			ShootSpeed = 0f,
			Ai0 = DustID.Blood,
			AftermathDamagePenaltyTicks = 300,
			AftermathDamagePenalty = 0.85f,
			KeyConflict = KeyConflictLevel.ModKeybind
		};
	}

	public class HaunterForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F01", 15, "Mods.PokemonHenshin.Items.HaunterForce.DisplayName", PokemonType.Ghost, 8, null, FormPassiveKind.Levitate, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.ShadowBall", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.35f, 24, 9f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Lick", 0.8f, 22, DustID.Shadowflame);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Sleep("Mods.PokemonHenshin.Moves.Hypnosis");
	}

	public class GengarForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F02", 16, "Mods.PokemonHenshin.Items.GengarForce.DisplayName", PokemonType.Ghost, 10, "L06_F01", FormPassiveKind.Levitate, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.SludgeBomb", ModContent.ProjectileType<GenericBoltProj>(), 1.5f, 22, 9f, ai0: DustID.CorruptGibs, ai1: BuffID.Poisoned);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.ShadowClaw", 1.55f, 14, DustID.Shadowflame, easyCrit: true);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DarkPulse", ModContent.ProjectileType<ShadowBallHenshinProj>(), 1.9f, 26, 11f, easyCrit: true);
	}

	public class DratiniForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F01", 17, "Mods.PokemonHenshin.Items.DratiniForce.DisplayName", PokemonType.Dragon, 6, null, FormPassiveKind.ShedSkin);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonBreath", ModContent.ProjectileType<GenericBoltProj>(), 1.25f, 22, 10f, ai0: DustID.Cloud);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.Bite", 1.15f, 18, DustID.Blood);
		protected override MoveSpec CreateUltimate() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonRage", ModContent.ProjectileType<GenericBoltProj>(), 1.6f, 30, 12f, ai0: DustID.Torch);
	}

	public class DragonairForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F02", 18, "Mods.PokemonHenshin.Items.DragonairForce.DisplayName", PokemonType.Dragon, 8, "L07_F01", FormPassiveKind.ShedSkin);
		protected override MoveSpec CreateMoveA() => FormItemUtil.Bolt("Mods.PokemonHenshin.Moves.DragonPulse", ModContent.ProjectileType<GenericBoltProj>(), 1.45f, 22, 11f, ai0: DustID.PurpleTorch);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonTail", 1.35f, 18, DustID.Cloud);
		protected override MoveSpec CreateUltimate() => FormItemUtil.AoE("Mods.PokemonHenshin.Moves.Hurricane", 1.8f, DustID.Cloud);
	}

	public class DragoniteForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F03", 19, "Mods.PokemonHenshin.Items.DragoniteForce.DisplayName", PokemonType.Dragon, 11, "L07_F02", FormPassiveKind.Multiscale, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.AoE("Mods.PokemonHenshin.Moves.Hurricane", 1.7f, DustID.Cloud);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.DragonDive", 1.8f, 28, DustID.Torch);
		protected override MoveSpec CreateUltimate() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.Outrage",
			ProjectileType = ModContent.ProjectileType<AoEBurstProj>(),
			DamageMultiplier = 2.3f,
			UseTime = 20,
			Ai0 = DustID.Torch,
			AftermathDamagePenaltyTicks = 240,
			AftermathDamagePenalty = 0.7f,
			KeyConflict = KeyConflictLevel.ModKeybind
		};
	}
}
