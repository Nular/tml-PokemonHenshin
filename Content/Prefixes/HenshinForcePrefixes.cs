using System.Collections.Generic;
using PokemonHenshin.Content.Combat;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Prefixes
{
	/// <summary>
	/// 之力专属前缀（Custom）。效果不在 <see cref="SetStats"/> 改射速/攻速/伤害，
	/// 由 <see cref="PlayerState.HenshinPlayer"/> 变身持握时读取并写入本模乘区。
	/// </summary>
	public abstract class HenshinForcePrefix : ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.Custom;

		public override float RollChance(Item item) => 1f;

		public override bool CanRoll(Item item) => item?.ModItem is HenshinForceItem;

		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			// 故意不改原版六维：之力每帧/每次使用会覆盖 damage、useTime、shootSpeed。
		}

		public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
		{
			yield return new TooltipLine(Mod, "HenshinPrefixEffect", EffectTooltip.Value)
			{
				IsModifier = true
			};
		}

		public LocalizedText EffectTooltip => this.GetLocalization(nameof(EffectTooltip));

		public override void SetStaticDefaults()
		{
			_ = EffectTooltip;
		}

		public static bool IsExclusive(int prefix)
		{
			if (prefix <= 0)
				return false;
			return prefix == ModContent.PrefixType<HenshinPrefixStored>()
				|| prefix == ModContent.PrefixType<HenshinPrefixIronwall>()
				|| prefix == ModContent.PrefixType<HenshinPrefixAssault>();
		}

		/// <summary>等权；猛攻略稀有（2/9）。</summary>
		public static int RollExclusive(Terraria.Utilities.UnifiedRandom rand)
		{
			int roll = rand.Next(9);
			if (roll < 3)
				return ModContent.PrefixType<HenshinPrefixStored>();
			if (roll < 6)
				return ModContent.PrefixType<HenshinPrefixIronwall>();
			return ModContent.PrefixType<HenshinPrefixAssault>();
		}
	}

	/// <summary>蓄能：命中/击杀能量 ×1.20。</summary>
	public sealed class HenshinPrefixStored : HenshinForcePrefix
	{
		public const float EnergyMul = 1.20f;

		public override void ModifyValue(ref float valueMult) => valueMult *= 1.15f;
	}

	/// <summary>铁壁：形态防御结算后再 +20%。</summary>
	public sealed class HenshinPrefixIronwall : HenshinForcePrefix
	{
		public const float FormDefenseMul = 0.20f;

		public override void ModifyValue(ref float valueMult) => valueMult *= 1.15f;
	}

	/// <summary>猛攻：变身伤 +15%，招式间隔 ×0.88。</summary>
	public sealed class HenshinPrefixAssault : HenshinForcePrefix
	{
		public const float DamageBonus = 0.15f;
		public const float CooldownMul = 0.88f;

		public override void ModifyValue(ref float valueMult) => valueMult *= 1.25f;
	}
}
