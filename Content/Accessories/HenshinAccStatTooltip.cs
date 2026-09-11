using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Core;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Accessories
{
	/// <summary>饰品面板数值行：以 Catalog 为真源，不写合成方式。</summary>
	internal static class HenshinAccStatTooltip
	{
		private static readonly Color StatColor = new(170, 220, 255);

		public static void AddLines(List<TooltipLine> tooltips, Mod mod, AccFamilyDef def, AccPiece piece)
		{
			if (def == null)
				return;

			StripRecipeLines(tooltips);

			if (def.Resonance != PokemonType.None)
			{
				string typeName = Language.GetTextValue("Mods.PokemonHenshin.Accessories.Type" + def.Resonance);
				tooltips.Add(new TooltipLine(mod, "HenshinAccResonance",
					Language.GetTextValue("Mods.PokemonHenshin.Accessories.ResonanceLine", typeName))
				{
					OverrideColor = new Color(255, 210, 120)
				});
			}

			AccStatLine[] stats = def.Stats(piece);
			if (stats == null || stats.Length == 0)
				return;

			int i = 0;
			foreach (AccStatLine line in stats)
			{
				string text = Format(line);
				if (string.IsNullOrEmpty(text))
					continue;
				tooltips.Add(new TooltipLine(mod, "HenshinAccStat" + i, text)
				{
					OverrideColor = StatColor
				});
				i++;
			}
		}

		private static void StripRecipeLines(List<TooltipLine> tooltips)
		{
			for (int i = tooltips.Count - 1; i >= 0; i--)
			{
				string t = tooltips[i].Text;
				if (string.IsNullOrEmpty(t))
					continue;
				if (t.Contains("合成：") || t.Contains("Craft:", StringComparison.OrdinalIgnoreCase)
					|| t.Contains("Ⅰ–Ⅳ") || t.Contains("I-IV →") || t.Contains("I–IV"))
					tooltips.RemoveAt(i);
			}
		}

		internal static string Format(AccStatLine line)
		{
			string key = "Mods.PokemonHenshin.Accessories.Stat" + line.Stat;
			return line.Stat switch
			{
				AccStat.HomingBolt or AccStat.HomingSpread or AccStat.HomingBarrage or AccStat.HomingDoTBind
					or AccStat.TilePierceBolt or AccStat.TilePierceSpread or AccStat.TilePierceBarrage
					or AccStat.TilePierceDoTBind or AccStat.TilePierceBeam
					or AccStat.CursedInfernoImmune
					or AccStat.ChoiceLockSkill2 or AccStat.ChoiceLockUlt
					or AccStat.LifeOrbHpDrain or AccStat.FocusSash or AccStat.EverstoneBlock
					=> Language.GetTextValue(key),
				AccStat.FlightEnergySec or AccStat.HomingTurn or AccStat.HomingRange or AccStat.PenetrateAdd
					or AccStat.DashSpeedBonus or AccStat.LungeIFrameBonus
					or AccStat.ShellBellHeal or AccStat.LeftoversHpPerSec or AccStat.LeftoversLowHpBonus
					or AccStat.FocusSashCdSec or AccStat.CursedInfernoSec
					or AccStat.UntransformedDefense or AccStat.AccDefense
					=> Language.GetTextValue(key, Num(line.Value)),
				AccStat.LifeOrbGateTicks or AccStat.ShellBellCdTicks or AccStat.RockyHelmetCdTicks
					or AccStat.FocusSashImmuneTicks
					=> Language.GetTextValue(key, Num(line.Value / 60f)),
				AccStat.EnergyGainMul
					=> Language.GetTextValue(key, Pct(line.Value)),
				_ => Language.GetTextValue(key, Pct(line.Value))
			};
		}

		private static string Pct(float v)
		{
			float p = v * 100f;
			if (Math.Abs(p - MathF.Round(p)) < 0.051f)
				return ((int)MathF.Round(p)).ToString();
			return p.ToString("0.##");
		}

		private static string Num(float v)
		{
			if (Math.Abs(v - MathF.Round(v)) < 0.001f)
				return ((int)MathF.Round(v)).ToString();
			return v.ToString("0.##");
		}
	}
}
