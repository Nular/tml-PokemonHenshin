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

			// 原版史莱姆王 lifeMax=2000 公式夹到 15；锚点约 20。灾厄加血后公式若已 ≥20 则不压低。
			if (npc.type == NPCID.KingSlime)
			{
				int world = ProgressStageService.GetProgressStage();
				int computed = HenshinStatService.ComputeBossXp(npc.lifeMax, npc.defense, world);
				if (computed < HenshinStatService.KingSlimeXpFloor)
					return HenshinStatService.KingSlimeXpFloor;
			}

			return null;
		}
	}
}
