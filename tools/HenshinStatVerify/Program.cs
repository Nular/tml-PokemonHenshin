using System;
using PokemonHenshin.Content.Core;

internal static class Program
{
	private static int fails;

	private static readonly (string Id, float Atk, float Def)[] FormMods =
	{
		("L01_F01", 0.60f, 0.55f),
		("L01_F02", 0.80f, 0.62f),
		("L01_F03", 1.09f, 0.82f),
		("L02_F01", 0.50f, 0.65f),
		("L02_F02", 0.65f, 0.80f),
		("L02_F03", 0.85f, 1.03f),
		("L03_F01", 0.65f, 0.57f),
		("L03_F02", 0.80f, 0.72f),
		("L03_F03", 1.00f, 0.92f),
		("L04_F01", 0.55f, 0.55f),
		("L04_F02", 0.90f, 0.68f),
		("L05_F01", 0.80f, 0.55f),
		("L05_F02", 1.00f, 0.65f),
		("L05_F03", 1.30f, 0.83f),
		("L06_F01", 1.15f, 0.55f),
		("L06_F02", 1.30f, 0.68f),
		("L07_F01", 0.64f, 0.55f),
		("L07_F02", 0.84f, 0.68f),
		("L07_F03", 1.34f, 0.98f),
		("L08_F01", 0.75f, 0.90f),
		("L08_F02", 1.35f, 1.10f),
		("L09_F01", 0.50f, 0.55f),
		("L09_F02", 1.25f, 0.90f),
		("L10_F01", 0.55f, 0.55f),
		("L10_F02", 1.00f, 0.60f),
		("L11_F01", 0.50f, 0.55f),
		("L11_F02", 0.80f, 0.73f),
		("L12_F01", 1.05f, 0.55f),
		("L12_F02", 1.35f, 0.70f),
		("L13_F01", 0.50f, 1.03f),
		("L13_F02", 0.85f, 1.33f),
		("L14_F01", 1.45f, 0.90f),
		("L15_F01", 0.70f, 0.55f),
		("L15_F02", 1.30f, 0.90f),
		("L16_F01", 0.90f, 1.42f),
		("L17_F01", 1.45f, 0.90f)
	};

	private static void Main()
	{
		Expect(50, HenshinStatService.BaseExpNeeded(1), "BaseExpNeeded(1)");
		Expect(185, HenshinStatService.BaseExpNeeded(10), "BaseExpNeeded(10)");
		Expect(410, HenshinStatService.BaseExpNeeded(25), "BaseExpNeeded(25)");
		Expect(785, HenshinStatService.BaseExpNeeded(50), "BaseExpNeeded(50)");
		Expect(826, HenshinStatService.BaseExpNeeded(51), "BaseExpNeeded(51)");
		Expect((int)Math.Round(785.0 + 40.0 * 25 + 0.8 * Math.Pow(25, 2.2)), HenshinStatService.BaseExpNeeded(75), "BaseExpNeeded(75)");
		Expect(0, HenshinStatService.BaseExpNeeded(100), "BaseExpNeeded(100)");

		Expect(50, HenshinStatService.ExpNeeded(1), "ExpNeeded L1 band1");
		Expect(HenshinStatService.BaseExpNeeded(5), HenshinStatService.ExpNeeded(5), "ExpNeeded L5 still band1");
		Expect((int)Math.Round(HenshinStatService.BaseExpNeeded(10) * (double)HenshinStatService.StageXpScale(2)), HenshinStatService.ExpNeeded(10), "ExpNeeded L10 × band2");
		Expect((int)Math.Round(HenshinStatService.BaseExpNeeded(91) * 300.0), HenshinStatService.ExpNeeded(91), "ExpNeeded L91 × band12");

		ExpectF(1f, HenshinStatService.StageXpScale(1), "gain scale S1");
		ExpectF(300f, HenshinStatService.StageXpScale(12), "gain scale S12");
		Expect(1, HenshinStatService.MinionXpMin(1), "minion min S1");
		Expect(3, HenshinStatService.MinionXpMax(1), "minion max S1");
		Expect(300, HenshinStatService.MinionXpMin(12), "minion min S12");
		Expect(900, HenshinStatService.MinionXpMax(12), "minion max S12");

		int ks15 = HenshinStatService.ComputeBossXp(2000, 10, 1);
		Expect(15, HenshinStatService.ScaleWorldXp(ks15, 1), "KS formula × S1");
		Expect(HenshinStatService.ScaleWorldXp(ks15, 1) * 300, HenshinStatService.ScaleWorldXp(ks15, 12), "boss ×300 same ratio");

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

		True(HenshinStatService.CanGainExperience(4, 1), "can gain below S1 cap");
		True(!HenshinStatService.CanGainExperience(5, 1), "S1 cap blocks gain");
		True(!HenshinStatService.CanGainExperience(18, 1), "over world cap cannot gain");
		True(!HenshinStatService.CanGainExperience(100, 12), "max level cannot gain");
		True(HenshinStatService.CanGainExperience(5, 2), "S2 cap allows L5");

		ForceProgress stay = HenshinStatService.AddExperience(5, 12, 9999, 1, out int noGain);
		Expect(5, stay.Level, "already at cap keeps level");
		Expect(12, stay.Xp, "already at cap keeps xp");
		True(noGain == 0, "already at cap no levels");

		ForceProgress over = HenshinStatService.AddExperience(18, 40, 100, 1, out int overG);
		Expect(18, over.Level, "overlevel not truncated");
		Expect(40, over.Xp, "overlevel xp not wiped");
		True(overG == 0, "overlevel no gain");

		ForceProgress fill = HenshinStatService.AddExperience(1, 0, 9999, 1, out int fillLv);
		Expect(5, fill.Level, "S1 climb stops at cap");
		Expect(HenshinStatService.ExpNeeded(5) - 1, fill.Xp, "hit cap leftover need-1");
		True(fillLv > 0, "climb to cap gained levels");

		ForceProgress climb = HenshinStatService.AddExperience(1, 0, 50, 12, out int lv1);
		Expect(2, climb.Level, "L1 +50 -> L2");
		Expect(1, lv1, "gained 1");

		// 世界档 12、物品仍在带 1：一只 300 经验能连升（需求未放大）。
		ForceProgress catchUp = HenshinStatService.AddExperience(1, 0, 300, 12, out int catchLv);
		Expect(5, catchUp.Level, "L1 +300 in S12 -> L5");
		Expect(4, catchLv, "catch-up 4 levels");
		Expect(10, catchUp.Xp, "catch-up leftover");

		ForceProgress inherit = HenshinStatService.AddExperience(19, 40, 0, 12, out _);
		Expect(19, inherit.Level, "evo inherit level");
		Expect(40, inherit.Xp, "evo inherit xp");

		// 攻防：Level 所在带，而非世界档。L1 小火龙即使世界档 12 仍用档 1 Floor。
		Approx(32 * 0.82f, HenshinStatService.StageAttack(1), 0.01, "atk L1 floor");
		Approx(32 * 1.28f, HenshinStatService.StageAttack(5), 0.01, "atk L5 ceil");
		Approx(48 * 0.82f, HenshinStatService.StageAttack(6), 0.01, "atk L6 S2 floor");
		Approx(10 * 0.82f, HenshinStatService.StageDefense(1), 0.01, "def L1 floor");

		int charAtk = HenshinStatService.FinalAttack(1, 0.60f, 1f);
		Expect(Math.Max(1, (int)Math.Round(32 * 0.82 * 0.60)), charAtk, "charmander L1 atk");
		int charDef = HenshinStatService.FinalDefense(1, 0.55f);
		Expect(Math.Max(0, (int)Math.Round(10 * 0.82 * 0.55)), charDef, "charmander L1 def");

		int mewtwo = HenshinStatService.FinalAttack(91, 1.45f, 1.15f);
		True(mewtwo >= 1000, "mewtwo L91 atk is large");
		Approx(1600 * 0.75 * 1.45 * 1.15, mewtwo, 1.5, "mewtwo L91 FinalAttack");

		int ksVanilla = HenshinStatService.ComputeBossXp(2000, 10, 1);
		Expect(15, ksVanilla, "vanilla KS hits min clamp");
		Expect(20, HenshinStatService.ComputeBossXp(2000, 10, 1, HenshinStatService.KingSlimeXpFloor), "KS whitelist floor 20");
		Expect(HenshinStatService.KingSlimeXpFloor, Math.Max(HenshinStatService.KingSlimeXpFloor, ksVanilla), "KS grant path floor");

		int ksCal = HenshinStatService.ComputeBossXp(5000, 10, 1);
		True(ksCal >= 18 && ksCal <= 25, "calamity-ish KS ~20");
		True(Math.Max(HenshinStatService.KingSlimeXpFloor, ksCal) == ksCal, "calamity KS not crushed by floor");

		int yharon = HenshinStatService.ComputeBossXp(3_000_000, 150, 12);
		Expect(800, yharon, "absurd HP clamps to S12 max");

		Expect(36, FormStatTable.Count, "36 forms");
		Expect(36, FormMods.Length, "doc row count");
		foreach ((string id, float atk, float def) in FormMods)
		{
			True(FormStatTable.Contains(id), "table " + id);
			ExpectF(atk, FormStatTable.Get(id).AttackMod, id + " atk");
			ExpectF(def, FormStatTable.Get(id).DefenseMod, id + " def");
		}

		ExpectF(1f, HenshinStatService.EnergyGainFactor(BalanceTag.Standard, 20), "std factor");
		ExpectF(0.35f, HenshinStatService.EnergyGainFactor(BalanceTag.Standard, 14), "hf by usetime");
		ExpectF(0.25f, HenshinStatService.EnergyGainFactor(BalanceTag.MultiHit, 36), "multi");
		ExpectF(0.45f, HenshinStatService.EnergyGainFactor(BalanceTag.WideAoE, 24), "aoe");
		ExpectF(0f, HenshinStatService.EnergyGainFactor(BalanceTag.Ultimate, 36), "ult");
		Expect(1000, HenshinStatService.EnergyMaxDefault, "EnergyMax");
		ExpectF(12f, HenshinStatService.CombatHitEnergy(1f, false), "std hit energy");
		ExpectF(1f, HenshinStatService.CombatHitEnergy(1f, true), "fragment hit 1×factor");
		ExpectF(0.25f, HenshinStatService.CombatHitEnergy(0.25f, true), "fragment × MultiHit");
		ExpectF(0f, HenshinStatService.CombatHitEnergy(0f, true), "ult fragment still 0");
		ExpectF(3f, HenshinStatService.CombatHitEnergy(0.25f, false), "multi main hit");

		// MoveRefRate 门禁：原始公式标准技 = 3 APS；§7 窗 0.85～1.15 是归一化值。
		ExpectF(3f, HenshinStatService.MoveRefRate(20, 1f), "std MoveRefRate");
		ExpectF(1f, HenshinStatService.MoveRefRateNormalized(20, 1f), "std normalized");
		ExpectF(1.25f, HenshinStatService.MoveRefRateNormalized(16, 1f), "HF UseTime 16 ceiling");
		True(HenshinStatService.MoveRefRateNormalized(16, 1f) <= 1.2501f, "HF window");

		// §7.1 点名：皮卡丘大招 4.8 / UseTime 36；凯西技能意念头锤 2.0 / 28。
		Approx(60.0 / 36.0 * 4.8, HenshinStatService.MoveRefRate(36, 4.8f), 0.001, "thunderbolt 4.8");
		Approx(60.0 / 28.0 * 2.0, HenshinStatService.MoveRefRate(28, 2.0f), 0.001, "zen hammer skill 2.0");
		True(4.8f >= 4.5f && 4.8f <= 5.2f, "thunderbolt in 4.5-5.2");
		True(2.0f >= 1.6f && 2.0f <= 2.2f, "zen hammer in 1.6-2.2");

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
