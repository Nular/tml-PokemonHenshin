using Terraria;
using Terraria.ID;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 个别离谱 Boss 的 XP 白名单（balance-stats §3.2）。键为 NPC.type。
	/// </summary>
	public static class HenshinBossXpOverrides
	{
		public static int? TryGet(NPC npc)
		{
			if (npc == null)
				return null;
			return npc.type switch
			{
				_ => null
			};
		}
	}
}
