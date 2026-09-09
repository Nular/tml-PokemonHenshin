using System;
using System.Collections.Generic;

namespace PokemonHenshin.Content.Core
{
	/// <summary>36 形态种族修正（balance-stats §5）。键为 FormId。</summary>
	public static class FormStatTable
	{
		public readonly struct Mods
		{
			public Mods(float attackMod, float defenseMod)
			{
				AttackMod = attackMod;
				DefenseMod = defenseMod;
			}

			public float AttackMod { get; }
			public float DefenseMod { get; }
		}

		private static readonly Dictionary<string, Mods> byFormId = new(StringComparer.Ordinal)
		{
			["L01_F01"] = new(0.60f, 0.55f),
			["L01_F02"] = new(0.80f, 0.62f),
			["L01_F03"] = new(1.09f, 0.82f),
			["L02_F01"] = new(0.50f, 0.65f),
			["L02_F02"] = new(0.65f, 0.80f),
			["L02_F03"] = new(0.85f, 1.03f),
			["L03_F01"] = new(0.65f, 0.57f),
			["L03_F02"] = new(0.80f, 0.72f),
			["L03_F03"] = new(1.00f, 0.92f),
			["L04_F01"] = new(0.55f, 0.55f),
			["L04_F02"] = new(0.90f, 0.68f),
			["L05_F01"] = new(0.80f, 0.55f),
			["L05_F02"] = new(1.00f, 0.65f),
			["L05_F03"] = new(1.30f, 0.83f),
			["L06_F01"] = new(1.15f, 0.55f),
			["L06_F02"] = new(1.30f, 0.68f),
			["L07_F01"] = new(0.64f, 0.55f),
			["L07_F02"] = new(0.84f, 0.68f),
			["L07_F03"] = new(1.34f, 0.98f),
			["L08_F01"] = new(0.75f, 0.90f),
			["L08_F02"] = new(1.35f, 1.10f),
			["L09_F01"] = new(0.50f, 0.55f),
			["L09_F02"] = new(1.25f, 0.90f),
			["L10_F01"] = new(0.55f, 0.55f),
			["L10_F02"] = new(1.00f, 0.60f),
			["L11_F01"] = new(0.50f, 0.55f),
			["L11_F02"] = new(0.80f, 0.73f),
			["L12_F01"] = new(1.05f, 0.55f),
			["L12_F02"] = new(1.35f, 0.70f),
			["L13_F01"] = new(0.50f, 1.03f),
			["L13_F02"] = new(0.85f, 1.33f),
			["L14_F01"] = new(1.45f, 0.90f),
			["L15_F01"] = new(0.70f, 0.55f),
			["L15_F02"] = new(1.30f, 0.90f),
			["L16_F01"] = new(0.90f, 1.42f),
			["L17_F01"] = new(1.45f, 0.90f)
		};

		public static int Count => byFormId.Count;

		public static Mods Get(string formId)
			=> formId != null && byFormId.TryGetValue(formId, out Mods mods) ? mods : new Mods(1f, 1f);

		public static bool Contains(string formId) => formId != null && byFormId.ContainsKey(formId);
	}
}
