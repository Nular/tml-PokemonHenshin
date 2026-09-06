using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Buffs
{
	/// <summary>
	/// 催眠术睡眠：停 AI、定身、禁接触伤；首次受伤翻倍后移除。
	/// 消费者见 <see cref="NPCs.HenshinNpcGlobalNPC"/>。
	/// </summary>
	public sealed class HenshinSleepDebuff : ModBuff
	{
		public const int DefaultDuration = 300; // 5s

		/// <summary>复用原版混乱图标，避免缺 Content/Buffs/*.png 导致 MissingResourceException。</summary>
		public override string Texture => "Terraria/Images/Buff_" + BuffID.Confused;

		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			Main.pvpBuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<NPCs.HenshinNpcGlobalNPC>().SleepActive = true;
		}
	}
}
