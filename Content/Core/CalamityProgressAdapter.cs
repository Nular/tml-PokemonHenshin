using System;
using System.Reflection;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 灾厄 DownedBossSystem 反射适配（无编译期 dll 引用）。字段缺失时记日志并当 false。
	/// </summary>
	public static class CalamityProgressAdapter
	{
		private static bool initialized;
		private static Mod calamity;
		private static PropertyInfo[] downedProps;

		// 属性名与灾厄 2.2.x DownedBossSystem 公开静态属性对齐（参考大修 CWRRef）。
		private static readonly string[] PropertyNames =
		{
			"downedDesertScourge",
			"downedCrabulon",
			"downedHiveMind",
			"downedPerforator",
			"downedSlimeGod",
			"downedCryogen",
			"downedAquaticScourge",
			"downedBrimstoneElemental",
			"downedCalamitasClone",
			"downedLeviathan",
			"downedAstrumAureus",
			"downedPlaguebringer",
			"downedRavager",
			"downedAstrumDeus",
			"downedGuardians",
			"downedDragonfolly",
			"downedProvidence",
			"downedCeaselessVoid",
			"downedStormWeaver",
			"downedSignus",
			"downedPolterghast",
			"downedBoomerDuke",
			"downedDoG",
			"downedYharon",
			"downedExoMechs",
			"downedCalamitas",
			"downedPrimordialWyrm"
		};

		public static void EnsureInit()
		{
			if (initialized)
				return;
			initialized = true;
			downedProps = new PropertyInfo[PropertyNames.Length];

			if (!ModLoader.TryGetMod("CalamityMod", out calamity))
			{
				PokemonHenshinMod.Instance?.Logger.Warn("[Progress] CalamityMod 未加载，灾厄 downed 全为 false。");
				return;
			}

			Type type = calamity.Code?.GetType("CalamityMod.DownedBossSystem");
			if (type == null)
			{
				PokemonHenshinMod.Instance?.Logger.Warn("[Progress] 找不到 CalamityMod.DownedBossSystem。");
				return;
			}

			const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
			for (int i = 0; i < PropertyNames.Length; i++)
			{
				PropertyInfo prop = type.GetProperty(PropertyNames[i], flags);
				if (prop == null || prop.PropertyType != typeof(bool))
				{
					PokemonHenshinMod.Instance?.Logger.Warn($"[Progress] 缺失字段 {PropertyNames[i]}");
					continue;
				}
				downedProps[i] = prop;
			}
		}

		public static void Unload()
		{
			initialized = false;
			calamity = null;
			downedProps = null;
		}

		private static bool Get(int index)
		{
			EnsureInit();
			PropertyInfo prop = downedProps?[index];
			if (prop == null)
				return false;
			try
			{
				return (bool)prop.GetValue(null);
			}
			catch
			{
				return false;
			}
		}

		public static bool DownedDesertScourge => Get(0);
		public static bool DownedCrabulon => Get(1);
		public static bool DownedHiveMind => Get(2);
		public static bool DownedPerforator => Get(3);
		public static bool DownedSlimeGod => Get(4);
		public static bool DownedCryogen => Get(5);
		public static bool DownedAquaticScourge => Get(6);
		public static bool DownedBrimstoneElemental => Get(7);
		public static bool DownedCalamitasClone => Get(8);
		public static bool DownedLeviathan => Get(9);
		public static bool DownedAstrumAureus => Get(10);
		public static bool DownedPlaguebringer => Get(11);
		public static bool DownedRavager => Get(12);
		public static bool DownedAstrumDeus => Get(13);
		public static bool DownedGuardians => Get(14);
		public static bool DownedDragonfolly => Get(15);
		public static bool DownedProvidence => Get(16);
		public static bool DownedCeaselessVoid => Get(17);
		public static bool DownedStormWeaver => Get(18);
		public static bool DownedSignus => Get(19);
		public static bool DownedPolterghast => Get(20);
		public static bool DownedBoomerDuke => Get(21);
		public static bool DownedDoG => Get(22);
		public static bool DownedYharon => Get(23);
		public static bool DownedExoMechs => Get(24);
		public static bool DownedCalamitas => Get(25);
		public static bool DownedPrimordialWyrm => Get(26);

		public static bool DownedWorldEvil => DownedHiveMind || DownedPerforator;
	}
}
