using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Buffs
{
	/// <summary>大比鸟大招「自我激励」：变身招式伤害 +20%，持续 12s。</summary>
	public sealed class HenshinWorkUpBuff : ModBuff
	{
		public const int DurationTicks = 720; // 12s
		public const float DamageBonus = 0.20f;

		public override string Texture => "Terraria/Images/Buff_" + BuffID.Wrath;

		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = false;
			Main.buffNoSave[Type] = true;
			Main.pvpBuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.GetModPlayer<HenshinPlayer>().HenshinDamageBonus += DamageBonus;
		}
	}
}
