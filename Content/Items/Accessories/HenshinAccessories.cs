using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Accessories
{
	public class A01AbilityCapsuleBelt : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A01";
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.MoveCooldownMultiplier *= 0.95f;
	}

	public class A02MuscleBand : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A02";
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.HenshinDamageBonus += 0.06f;
	}

	public class A03SoulDewPendant : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A03";
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.AffinityAmplitudeBonus += 0.20f;
	}

	public class A04FloatStoneAnklet : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A04";
		protected override void ApplyHenshinEffect(HenshinPlayer hp)
		{
			hp.ExtraFlightEnergy += 1f;
			hp.MoveSpeedBonus += 0.05f;
		}
	}

	public class A05FocusSashBadge : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A05";
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.IncomingDamageMultiplier *= 0.94f;
	}

	public class A06LifeOrbCore : HenshinAccessoryItem
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A06";
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.HenshinDamageFactorBonus += 0.05f;
	}

	public class A07CharcoalBag : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A07";
		protected override PokemonType RequiredType => PokemonType.Fire;
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.OnFireTargetBonus += 0.08f;
	}

	public class A08MysticWaterPouch : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A08";
		protected override PokemonType RequiredType => PokemonType.Water;
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.WaterSpeedBonus += 0.10f;
	}

	public class A09MagnetChip : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A09";
		protected override PokemonType RequiredType => PokemonType.Electric;
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.DashCooldownMultiplier *= 0.85f;
	}

	public class A10SharpBeakMembrane : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A10";
		protected override PokemonType RequiredType => PokemonType.Flying;
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.FallDamageReduction = 0.25f; // 0.5+0.25→0.75
	}

	public class A11SpellTagCloth : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A11";
		protected override PokemonType RequiredType => PokemonType.Ghost;
		protected override void ApplyHenshinEffect(HenshinPlayer hp)
		{
			// +0.5s (=30 ticks)；若已满 2.0s 则 CD×0.9
			float room = HenshinPlayer.PhasingMaxTicks - HenshinPlayer.PhasingBaseTicks;
			if (hp.PhasingBonusTicks < room)
				hp.PhasingBonusTicks += 30f;
			else
				hp.PhasingCooldownMultiplier *= 0.9f;
		}
	}

	public class A12DragonFangCharm : TypeResonanceAccessory
	{
		public override string Texture => "PokemonHenshin/Assets/Accessories/A12";
		protected override PokemonType RequiredType => PokemonType.Dragon;
		protected override void ApplyHenshinEffect(HenshinPlayer hp) => hp.BossDamageBonus += 0.06f;
	}
}
