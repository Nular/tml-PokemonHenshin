using System;
using PokemonHenshin.Content.Core;

internal static class Program
{
	private static int fails;

	private static void Main()
	{
		Expect(50, HenshinStatService.ExpNeeded(1), "ExpNeeded(1)");
		Expect(185, HenshinStatService.ExpNeeded(10), "ExpNeeded(10)");
		Expect(410, HenshinStatService.ExpNeeded(25), "ExpNeeded(25)");
		Expect(785, HenshinStatService.ExpNeeded(50), "ExpNeeded(50)");
		Expect(826, HenshinStatService.ExpNeeded(51), "ExpNeeded(51)");
		Expect(0, HenshinStatService.ExpNeeded(100), "ExpNeeded(100)");

		Expect(1, HenshinStatService.BandForLevel(1), "band L1");
		Expect(1, HenshinStatService.BandForLevel(5), "band L5");
		Expect(2, HenshinStatService.BandForLevel(6), "band L6");
		Expect(4, HenshinStatService.BandForLevel(19), "band L19");
		Expect(12, HenshinStatService.BandForLevel(91), "band L91");
		Expect(12, HenshinStatService.BandForLevel(100), "band L100");

		Expect(5, HenshinStatService.LevelCap(1), "cap S1");
		Expect(100, HenshinStatService.LevelCap(12), "cap S12");
		Expect(19, HenshinStatService.StartingLevelForFormStage(4), "start Charmeleon");
		Expect(91, HenshinStatService.StartingLevelForFormStage(12), "start legend");

		True(HenshinStatService.MeetsEvolution(4, 19, 4), "evo PS+level");
		True(!HenshinStatService.MeetsEvolution(4, 18, 4), "evo level gate");
		True(!HenshinStatService.MeetsEvolution(3, 25, 4), "evo stage gate");
		True(HenshinStatService.CrossedBandMin(18, 19, 4), "cross BandMin 19");
		True(!HenshinStatService.CrossedBandMin(19, 20, 4), "already past BandMin");

		ForceProgress cap = HenshinStatService.AddExperience(5, 0, 9999, 1, out int gained);
		Expect(5, cap.Level, "S1 cap level");
		Expect(HenshinStatService.ExpNeeded(5) - 1, cap.Xp, "S1 cap xp-1");
		True(gained == 0, "already at cap no extra levels from 0 xp");

		ForceProgress climb = HenshinStatService.AddExperience(1, 0, 50, 12, out int lv1);
		Expect(2, climb.Level, "L1 +50 -> L2");
		Expect(1, lv1, "gained 1");

		// 攻防：Level 所在带，而非世界档。L1 小火龙即使世界档 12 仍用档 1 Floor。
		Approx(32 * 0.82f, HenshinStatService.StageAttack(1), 0.01, "atk L1 floor");
		Approx(32 * 1.28f, HenshinStatService.StageAttack(5), 0.01, "atk L5 ceil");
		Approx(48 * 0.82f, HenshinStatService.StageAttack(6), 0.01, "atk L6 S2 floor");

		int charAtk = HenshinStatService.FinalAttack(1, 0.60f, 1f);
		Expect(Math.Max(1, (int)Math.Round(32 * 0.82 * 0.60)), charAtk, "charmander L1 atk");

		int mewtwo = HenshinStatService.FinalAttack(91, 1.45f, 1.15f);
		True(mewtwo >= 1000, "mewtwo L91 atk is large");

		int ksVanilla = HenshinStatService.ComputeBossXp(2000, 10, 1);
		True(ksVanilla >= 15 && ksVanilla <= 35, "king slime vanilla clamped");
		Expect(15, ksVanilla, "vanilla KS hits min clamp");

		int ksCal = HenshinStatService.ComputeBossXp(5000, 10, 1);
		True(ksCal >= 18 && ksCal <= 25, "calamity-ish KS ~20");

		int yharon = HenshinStatService.ComputeBossXp(3_000_000, 150, 12);
		Expect(800, yharon, "absurd HP clamps to S12 max");

		True(FormStatTable.Contains("L01_F01"), "table charmander");
		True(FormStatTable.Contains("L17_F01"), "table rayquaza");
		ExpectF(0.50f, FormStatTable.Get("L09_F01").AttackMod, "magikarp atk");
		ExpectF(1.45f, FormStatTable.Get("L14_F01").AttackMod, "mewtwo atk");

		ExpectF(1f, HenshinStatService.EnergyGainFactor(BalanceTag.Standard, 20), "std factor");
		ExpectF(0.35f, HenshinStatService.EnergyGainFactor(BalanceTag.Standard, 14), "hf by usetime");
		ExpectF(0.25f, HenshinStatService.EnergyGainFactor(BalanceTag.MultiHit, 36), "multi");
		ExpectF(0f, HenshinStatService.EnergyGainFactor(BalanceTag.Ultimate, 36), "ult");

		if (fails > 0)
		{
			Console.Error.WriteLine($"{fails} assertion(s) failed");
			Environment.Exit(1);
		}

		Console.WriteLine("HenshinStatVerify OK");
	}

	private static void Expect(int want, int got, string name)
	{
		if (want != got)
			Fail($"{name}: want {want} got {got}");
	}

	private static void ExpectF(float want, float got, string name)
	{
		if (Math.Abs(want - got) > 0.001f)
			Fail($"{name}: want {want} got {got}");
	}

	private static void Approx(double want, double got, double eps, string name)
	{
		if (Math.Abs(want - got) > eps)
			Fail($"{name}: want {want} got {got}");
	}

	private static void True(bool cond, string name)
	{
		if (!cond)
			Fail(name);
	}

	private static void Fail(string msg)
	{
		fails++;
		Console.Error.WriteLine("FAIL " + msg);
	}
}
