using PokemonHenshin.Content.Evolution;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>进度档提升提示；档位上升时（通常由击败对应 Boss）尝试弹出进化确认。</summary>
	public sealed class ProgressAnnounceSystem : ModSystem
	{
		private int lastStage = -1;

		public override void PostUpdatePlayers()
		{
			if (Main.gameMenu)
				return;
			int stage = ProgressStageService.GetProgressStage();
			if (lastStage < 0)
			{
				lastStage = stage;
				return;
			}
			if (stage > lastStage)
			{
				int previous = lastStage;
				lastStage = stage;
				// 含单人与听服务器主机；专用服务器无本地 UI
				if (!Main.dedServ)
				{
					Main.NewText(Language.GetTextValue("Mods.PokemonHenshin.Common.StageUp", stage), Microsoft.Xna.Framework.Color.LightGreen);
					// Boss/事件刚解锁新档：对本机玩家弹出进化确认（不再每 tick 扫世界状态）。
					EvolutionOfferPlayer.TryOfferLocalAfterStageUp(previous, stage);
				}
			}
			else if (stage < lastStage)
				lastStage = stage;
		}
	}
}
