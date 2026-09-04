using PokemonHenshin.Content.Core;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
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
				Main.NewText(Language.GetTextValue("Mods.PokemonHenshin.Common.StageUp", stage), Microsoft.Xna.Framework.Color.LightGreen);
				lastStage = stage;
			}
			else if (stage < lastStage)
				lastStage = stage;
		}
	}
}
