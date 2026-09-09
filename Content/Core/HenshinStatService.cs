using System;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// v1.4 数值真源实现（<c>docs/balance-stats.md</c>）。无 Terraria 依赖，可供独立校验。
	/// 攻防插值用「当前 Level 所在等级带」，世界 <c>ProgressStage</c> 只做等级硬顶。
	/// </summary>
	public static class HenshinStatService
	{
		public const int MinLevel = 1;
		public const int MaxLevel = 100;
		public const int EnergyMaxDefault = 1000;
		public const float EnergyOnHit = 12f;
		public const float EnergyOnKill = 55f;
		public const float EnergyPassivePerTick = 0.15f;
		public const float EnergyCombatSoftCapPerSecond = 90f;

		public static readonly int[] BandMin =
		{
			0,
			1, 6, 12, 19, 26, 34, 43, 52, 62, 71, 81, 91
		};

		public static readonly int[] BandMax =
		{
			0,
			5, 11, 18, 25, 33, 42, 51, 61, 70, 80, 90, 100
		};

		public static readonly int[] MidAtk =
		{
			0,
			32, 48, 70, 100, 140, 200, 280, 400, 560, 780, 1100, 1600
		};

		public static readonly int[] MidDef =
		{
			0,
			10, 14, 18, 24, 30, 38, 46, 54, 64, 74, 86, 100
		};

		public static readonly float[] FloorMult =
		{
			0f,
			0.82f, 0.82f, 0.80f, 0.80f, 0.80f, 0.78f, 0.78f, 0.78f, 0.78f, 0.76f, 0.76f, 0.75f
		};

		public static readonly float[] CeilMult =
		{
			0f,
			1.28f, 1.30f, 1.32f, 1.34f, 1.36f, 1.38f, 1.40f, 1.40f, 1.42f, 1.42f, 1.44f, 1.45f
		};

		public static readonly float[] LevelCurve =
		{
			0f,
			1.00f, 1.00f, 1.00f, 1.05f, 1.05f, 1.10f, 1.10f, 1.15f, 1.15f, 1.20f, 1.20f, 1.25f
		};

		public static readonly int[] BossXpMin =
		{
			0,
			15, 22, 30, 40, 55, 80, 110, 150, 200, 260, 320, 400
		};

		public static readonly int[] BossXpMax =
		{
			0,
			35, 50, 70, 90, 120, 160, 220, 280, 360, 480, 600, 800
		};

		public static int ClampStage(int stage) => Math.Clamp(stage, 1, 12);

		public static int BandMinOf(int stage) => BandMin[ClampStage(stage)];

		public static int BandMaxOf(int stage) => BandMax[ClampStage(stage)];

		/// <summary>当前等级落入的进度档（1～12）。</summary>
		public static int BandForLevel(int level)
		{
			int lv = Math.Clamp(level, MinLevel, MaxLevel);
			for (int s = 12; s >= 1; s--)
			{
				if (lv >= BandMin[s])
					return s;
			}

			return 1;
		}

		public static int LevelCap(int worldStage) => BandMaxOf(worldStage);

		public static int StartingLevelForFormStage(int formStage) => BandMinOf(formStage);

		/// <summary>当前等级 L 升到 L+1 所需经验。满级返回 0。</summary>
		public static int ExpNeeded(int level)
		{
			if (level < MinLevel || level >= MaxLevel)
				return 0;
			if (level <= 50)
				return 50 + 15 * (level - 1);

			double x = level - 50;
			double raw = 785.0 + 40.0 * x + 0.8 * Math.Pow(x, 2.2);
			return (int)Math.Round(raw);
		}

		public static ForceProgress TruncateToCap(int level, int xp, int worldStage)
		{
			int cap = Math.Min(MaxLevel, LevelCap(worldStage));
			int lv = Math.Clamp(level, MinLevel, cap);
			int need = ExpNeeded(lv);
			int nextXp = xp;
			if (lv >= cap || lv >= MaxLevel)
			{
				if (need > 0)
					nextXp = Math.Min(Math.Max(0, xp), need - 1);
				else
					nextXp = 0;
			}
			else
				nextXp = Math.Max(0, xp);

			return new ForceProgress(lv, nextXp);
		}

		/// <summary>叠加经验并处理升级 / 硬顶截断。返回新进度与本段内升了几级。</summary>
		public static ForceProgress AddExperience(int level, int xp, int amount, int worldStage, out int levelsGained)
		{
			levelsGained = 0;
			if (amount < 0)
				amount = 0;

			int cap = Math.Min(MaxLevel, LevelCap(worldStage));
			int nextLevel = Math.Clamp(level, MinLevel, MaxLevel);
			int nextXp = Math.Max(0, xp) + amount;
			if (nextLevel >= MaxLevel)
				return new ForceProgress(MaxLevel, 0);

			while (nextLevel < cap && nextLevel < MaxLevel)
			{
				int need = ExpNeeded(nextLevel);
				if (need <= 0 || nextXp < need)
					break;
				nextXp -= need;
				nextLevel++;
				levelsGained++;
			}

			return TruncateToCap(nextLevel, nextXp, worldStage);
		}

		public static bool CrossedBandMin(int oldLevel, int newLevel, int nextFormStage)
		{
			int need = BandMinOf(nextFormStage);
			return oldLevel < need && newLevel >= need;
		}

		public static bool MeetsEvolution(int worldStage, int level, int nextFormStage)
			=> worldStage >= nextFormStage && level >= BandMinOf(nextFormStage);

		public static float StageAttack(int level)
		{
			int s = BandForLevel(level);
			return InterpolateStage(MidAtk[s], FloorMult[s], CeilMult[s], LevelCurve[s], level, s);
		}

		public static float StageDefense(int level)
		{
			int s = BandForLevel(level);
			return InterpolateStage(MidDef[s], FloorMult[s], CeilMult[s], LevelCurve[s], level, s);
		}

		private static float InterpolateStage(int mid, float floorMul, float ceilMul, float curve, int level, int stage)
		{
			int min = BandMin[stage];
			int max = BandMax[stage];
			float t = (level - min) / (float)Math.Max(1, max - min);
			t = Math.Clamp(t, 0f, 1f);
			float tw = (float)Math.Pow(t, curve);
			float lo = mid * floorMul;
			float hi = mid * ceilMul;
			return lo + (hi - lo) * tw;
		}

		public static int FinalAttack(int level, float attackMod, float henshinDamageFactor)
		{
			double raw = StageAttack(level) * attackMod * henshinDamageFactor;
			return Math.Max(1, (int)Math.Round(raw));
		}

		public static int FinalDefense(int level, float defenseMod)
		{
			double raw = StageDefense(level) * defenseMod;
			return Math.Max(0, (int)Math.Round(raw));
		}

		public static int ComputeBossXp(int lifeMax, int defense, int worldStage, int? whitelistOverride = null)
		{
			if (whitelistOverride.HasValue)
				return Math.Max(1, whitelistOverride.Value);

			int s = ClampStage(worldStage);
			double lifeTerm = Math.Pow(Math.Max(lifeMax, 1) / 2000.0, 0.45);
			double defTerm = 1.0 + defense / 100.0;
			double stageMul = 0.75 + 0.12 * s;
			double raw = 14.0 * lifeTerm * defTerm * stageMul;
			int rounded = (int)Math.Round(raw);
			return Math.Clamp(rounded, BossXpMin[s], BossXpMax[s]);
		}

		public static float EnergyGainFactor(BalanceTag tag, int useTime)
		{
			if (tag == BalanceTag.Ultimate)
				return 0f;
			if (tag == BalanceTag.MultiHit)
				return 0.25f;
			if (tag == BalanceTag.WideAoE)
				return 0.45f;
			if (tag == BalanceTag.HighFrequency || useTime <= 16)
				return 0.35f;
			return 1f;
		}
	}

	/// <summary>招式预算标签（balance-stats §6 / §7）。</summary>
	public enum BalanceTag : byte
	{
		Standard = 0,
		HighFrequency = 1,
		MultiHit = 2,
		WideAoE = 3,
		Ultimate = 4
	}

	public readonly struct ForceProgress
	{
		public ForceProgress(int level, int xp)
		{
			Level = level;
			Xp = xp;
		}

		public int Level { get; }
		public int Xp { get; }
	}
}
