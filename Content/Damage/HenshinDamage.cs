using Terraria.ModLoader;

namespace PokemonHenshin.Content.Damage
{
	/// <summary>
	/// 本模独立伤害类型（需求 §2.6，dev-plan §4.1）。
	/// <list type="bullet">
	/// <item>Generic：完整继承（增伤 / 暴击 / 攻速 / 穿甲 / 击退）。</item>
	/// <item>近战 / 远程 / 魔法 / 召唤 / 投掷 / 灾厄盗贼：按 k = 0.35 折算继承。</item>
	/// <item>其余（含真近战、潜行等专用类）：不继承。</item>
	/// </list>
	/// 禁止「再乘一遍各职业满额」：这里是唯一的职业折算挂点，别处不得再补乘。
	/// </summary>
	public sealed class HenshinDamage : DamageClass
	{
		public const float ClassInheritanceFactor = 0.35f;

		public static HenshinDamage Instance => ModContent.GetInstance<HenshinDamage>();

		private static readonly StatInheritanceData PartialInheritance = new(
			damageInheritance: ClassInheritanceFactor,
			critChanceInheritance: ClassInheritanceFactor,
			attackSpeedInheritance: ClassInheritanceFactor,
			armorPenInheritance: ClassInheritanceFactor,
			knockbackInheritance: ClassInheritanceFactor);

		/// <summary>灾厄盗贼类；强依赖但仍走 TryFind，类名变更时退化为「不继承」而非编译失败。</summary>
		private static DamageClass rogue;

		public override void Load()
		{
			rogue = null;
		}

		public override void Unload()
		{
			rogue = null;
		}

		private static DamageClass Rogue
		{
			get
			{
				if (rogue == null && ModContent.TryFind("CalamityMod", "RogueDamageClass", out DamageClass found))
					rogue = found;
				return rogue;
			}
		}

		public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
		{
			if (damageClass == Generic)
				return StatInheritanceData.Full;

			if (damageClass == Melee || damageClass == Ranged || damageClass == Magic
				|| damageClass == Summon || damageClass == Throwing
				|| (Rogue != null && damageClass == Rogue))
				return PartialInheritance;

			return StatInheritanceData.None;
		}

		/// <summary>只从 Generic 继承效果；不吃潜行偷袭、真近战等职业专用效果。</summary>
		public override bool GetEffectInheritance(DamageClass damageClass) => damageClass == Generic;

		public override bool UseStandardCritCalcs => true;
		public override bool ShowStatTooltipLine(Terraria.Player player, string lineName) => true;
	}
}
