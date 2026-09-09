using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Evolution;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>调试：/henshin stage | evolve | stats | setlevel</summary>
	public sealed class HenshinCommand : ModCommand
	{
		public override string Command => "henshin";
		public override CommandType Type => CommandType.Chat;
		public override string Description => "Pokemon Henshin 调试命令";
		public override string Usage => "/henshin stage\n/henshin evolve\n/henshin stats\n/henshin setlevel <n>";

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
					caller.Reply("当前没有可进化的之力（检查进度档、等级带与背包位置）", Color.Orange);
				return;
			}

			if (args[0].Equals("stats", System.StringComparison.OrdinalIgnoreCase))
			{
				if (caller.Player.HeldItem?.ModItem is HenshinForceItem force)
				{
					force.RefreshDamage();
					caller.Reply(
						$"{force.Definition.FormId} Lv{force.Level} XP {force.Xp}/{HenshinStatService.ExpNeeded(force.Level)}  Atk {force.ComputeFinalAttack()} Def {force.ComputeFinalDefense()}  band {HenshinStatService.BandForLevel(force.Level)}  world {ProgressStageService.GetProgressStage()}",
						Color.LightGreen);
				}
				else
					caller.Reply("请持握一件之力", Color.Orange);
				return;
			}

			if (args[0].Equals("setlevel", System.StringComparison.OrdinalIgnoreCase))
			{
				if (args.Length < 2 || !int.TryParse(args[1], out int lv))
				{
					caller.Reply(Usage, Color.Orange);
					return;
				}

				if (caller.Player.HeldItem?.ModItem is HenshinForceItem force)
				{
					force.SetProgress(lv, 0);
					force.RefreshDamage();
					caller.Reply($"已设为等级 {force.Level}，攻击 {force.ComputeFinalAttack()}，防御 {force.ComputeFinalDefense()}", Color.LightGreen);
				}
				else
					caller.Reply("请持握一件之力", Color.Orange);
				return;
			}

			caller.Reply(Usage, Color.Orange);
		}
	}
}
