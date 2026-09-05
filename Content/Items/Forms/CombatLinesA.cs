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
		protected override MoveSpec CreateMoveA() => FormItemUtil.MidThunder("Mods.PokemonHenshin.Moves.Thunderbolt", 1.5f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.VoltTackle", 1.8f, 30, DustID.Electric, recoil: true, onHitBuff: BuffID.Electrified);
		protected override MoveSpec CreateUltimate() => FormItemUtil.ThunderPillarUlt("Mods.PokemonHenshin.Moves.Thunder", 3.8f);
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
		protected override MoveSpec CreateMoveA() => FormItemUtil.RockSlideX("Mods.PokemonHenshin.Moves.RockSlide", 3, 1.5f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BrickBreak("Mods.PokemonHenshin.Moves.BrickBreak", 1.4f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.DynamicPunchUlt("Mods.PokemonHenshin.Moves.DynamicPunch", 3.4f);
	}

	public class MachampForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(9);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L05_F03", 14, "Mods.PokemonHenshin.Items.MachampForce.DisplayName", PokemonType.Fighting, 9, "L05_F02", FormPassiveKind.Guts);
		protected override MoveSpec CreateMoveA() => FormItemUtil.StoneEdge("Mods.PokemonHenshin.Moves.StoneEdge", 1.7f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.CrossChopShort("Mods.PokemonHenshin.Moves.CrossChop", 1.6f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.CloseCombatUlt("Mods.PokemonHenshin.Moves.CloseCombat", 4.0f);
	}

	public class HaunterForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F01", 15, "Mods.PokemonHenshin.Items.HaunterForce.DisplayName", PokemonType.Ghost, 8, null, FormPassiveKind.Levitate, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.BigShadowBall("Mods.PokemonHenshin.Moves.ShadowBall", 1.35f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.LickFan("Mods.PokemonHenshin.Moves.Lick", 0.8f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.HypnosisUlt("Mods.PokemonHenshin.Moves.Hypnosis");
	}

	public class GengarForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(10);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L06_F02", 16, "Mods.PokemonHenshin.Items.GengarForce.DisplayName", PokemonType.Ghost, 10, "L06_F01", FormPassiveKind.Levitate, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.SludgeBolt("Mods.PokemonHenshin.Moves.SludgeBomb", 1.5f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.ShadowClaw", 1.55f, 14, DustID.Shadowflame, easyCrit: true);
		protected override MoveSpec CreateUltimate() => FormItemUtil.DarkPulseCone("Mods.PokemonHenshin.Moves.DarkPulse", 3.6f);
	}

	public class DratiniForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(6);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F01", 17, "Mods.PokemonHenshin.Items.DratiniForce.DisplayName", PokemonType.Dragon, 6, null, FormPassiveKind.ShedSkin);
		protected override MoveSpec CreateMoveA() => FormItemUtil.DragonBreath("Mods.PokemonHenshin.Moves.DragonBreath", 1.25f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Bite", 1.15f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonRage", 3.0f, DustID.Torch);
	}

	public class DragonairForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(8);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F02", 18, "Mods.PokemonHenshin.Items.DragonairForce.DisplayName", PokemonType.Dragon, 8, "L07_F01", FormPassiveKind.ShedSkin);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonPulse", 1.45f, DustID.PurpleTorch, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.DragonTail", 1.35f, 18, DustID.Cloud);
		protected override MoveSpec CreateUltimate() => FormItemUtil.HurricaneField("Mods.PokemonHenshin.Moves.Hurricane", 3.4f, ult: true);
	}

	public class DragoniteForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(11);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L07_F03", 19, "Mods.PokemonHenshin.Items.DragoniteForce.DisplayName", PokemonType.Dragon, 11, "L07_F02", FormPassiveKind.Multiscale, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.HurricaneField("Mods.PokemonHenshin.Moves.Hurricane", 1.7f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.DragonDive", 1.8f, 28, DustID.Torch);
		protected override MoveSpec CreateUltimate() => FormItemUtil.OutrageUlt("Mods.PokemonHenshin.Moves.Outrage", 4.2f);
	}
}
