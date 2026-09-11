using Terraria;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 进度档 1～12（需求 §4.1）。返回当前满足的最大档。
	/// </summary>
	public static class ProgressStageService
	{
		public static int GetProgressStage()
		{
			uint tick = Main.GameUpdateCount;
			if (cachedTick == tick && cachedStage > 0)
				return cachedStage;

			CalamityProgressAdapter.EnsureInit();
			int stage = ComputeProgressStage();
			cachedStage = stage;
			cachedTick = tick;
			return stage;
		}

		public static void InvalidateCache()
		{
			cachedTick = uint.MaxValue;
			cachedStage = 0;
		}

		private static int cachedStage;
		private static uint cachedTick = uint.MaxValue;

		private static int ComputeProgressStage()
		{
			if (Meets12()) return 12;
			if (Meets11()) return 11;
			if (Meets10()) return 10;
			if (Meets9()) return 9;
			if (Meets8()) return 8;
			if (Meets7()) return 7;
			if (Meets6()) return 6;
			if (Meets5()) return 5;
			if (Meets4()) return 4;
			if (Meets3()) return 3;
			if (Meets2()) return 2;
			return 1;
		}

		public static bool MeetsStage(int stage) => GetProgressStage() >= stage;

		// 档 2：荒漠灾虫 / 克苏鲁之眼 / 蟹叶草
		private static bool Meets2() =>
			CalamityProgressAdapter.DownedDesertScourge
			|| NPC.downedBoss1
			|| CalamityProgressAdapter.DownedCrabulon;

		// 档 3：世界恶 / 蜂后 / 骷髅王
		private static bool Meets3() =>
			CalamityProgressAdapter.DownedWorldEvil
			|| NPC.downedQueenBee
			|| NPC.downedBoss3;

		// 档 4：史莱姆神 / 鹿角怪（不含肉山）
		private static bool Meets4() =>
			CalamityProgressAdapter.DownedSlimeGod
			|| NPC.downedDeerclops;

		// 档 5：困难模式
		private static bool Meets5() => Main.hardMode;

		// 档 6：冰雪之柱 / 任一机械 / 渊海灾虫 / 硫磺火元素
		private static bool Meets6() =>
			CalamityProgressAdapter.DownedCryogen
			|| NPC.downedMechBossAny
			|| CalamityProgressAdapter.DownedAquaticScourge
			|| CalamityProgressAdapter.DownedBrimstoneElemental;

		// 档 7：灾厄之影 / 世纪之花
		private static bool Meets7() =>
			CalamityProgressAdapter.DownedCalamitasClone
			|| NPC.downedPlantBoss;

		// 档 8：石巨人 / 利维坦 / 星体史莱姆 / 瘟疫使者 / 毁灭者等月前 / 邪教徒
		private static bool Meets8() =>
			NPC.downedGolemBoss
			|| CalamityProgressAdapter.DownedLeviathan
			|| CalamityProgressAdapter.DownedAstrumAureus
			|| CalamityProgressAdapter.DownedPlaguebringer
			|| CalamityProgressAdapter.DownedRavager
			|| CalamityProgressAdapter.DownedAstrumDeus
			|| NPC.downedAncientCultist;

		// 档 9：月亮领主
		private static bool Meets9() => NPC.downedMoonlord;

		// 档 10：亵渎天神 / 痴愚金龙 / 亵渎守卫
		private static bool Meets10() =>
			CalamityProgressAdapter.DownedProvidence
			|| CalamityProgressAdapter.DownedDragonfolly
			|| CalamityProgressAdapter.DownedGuardians;

		// 档 11：幽花 / 神明吞噬者 / 老公爵
		private static bool Meets11() =>
			CalamityProgressAdapter.DownedPolterghast
			|| CalamityProgressAdapter.DownedDoG
			|| CalamityProgressAdapter.DownedBoomerDuke;

		// 档 12：犽戎 / 星流巨械 / 至尊灾厄
		private static bool Meets12() =>
			CalamityProgressAdapter.DownedYharon
			|| CalamityProgressAdapter.DownedExoMechs
			|| CalamityProgressAdapter.DownedCalamitas;
	}

	public sealed class ProgressAdapterSystem : ModSystem
	{
		public override void Load() => CalamityProgressAdapter.EnsureInit();
		public override void Unload() => CalamityProgressAdapter.Unload();
		public override void OnWorldLoad() => ProgressStageService.InvalidateCache();
		public override void OnWorldUnload() => ProgressStageService.InvalidateCache();
	}
}
