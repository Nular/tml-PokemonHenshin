using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Evolution;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>调试：/henshin stage | /henshin evolve</summary>
	public sealed class HenshinCommand : ModCommand
	{
		public override string Command => "henshin";
		public override CommandType Type => CommandType.Chat;
		public override string Description => "Pokemon Henshin 调试命令";
		public override string Usage => "/henshin stage\n/henshin evolve";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			if (args.Length == 0 || args[0].Equals("stage", System.StringComparison.OrdinalIgnoreCase))
			{
				int stage = ProgressStageService.GetProgressStage();
				caller.Reply($"ProgressStage = {stage}", Color.LightGreen);
				return;
			}

			if (args[0].Equals("evolve", System.StringComparison.OrdinalIgnoreCase))
			{
				if (EvolutionService.TryFindEvolvable(caller.Player, out int slot, out bool isMouse, out FormDefinition current, out FormDefinition next))
				{
					EvolutionConfirmSystem.Instance?.Open(slot, isMouse, current, next);
					caller.Reply("已打开进化确认", Color.LightGreen);
				}
				else
					caller.Reply("当前没有可进化的之力（检查进度档与背包位置）", Color.Orange);
				return;
			}

			caller.Reply(Usage, Color.Orange);
		}
	}
}
