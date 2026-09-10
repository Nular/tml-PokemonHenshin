namespace PokemonHenshin.Content.Accessories
{
	public enum AccFamilyId : byte
	{
		A01 = 1,
		A02,
		A03,
		A04,
		A05,
		A06,
		A07,
		A08,
		A09,
		A10,
		A11,
		A12,
		A13,
		A14,
		A15,
		A16,
		A17,
		A18,
		A19,
		A20,
		A21,
		A22,
		A23,
		A24,
		A25,
		A26,
		A27,
		A28
	}

	public enum AccPiece : byte
	{
		S1 = 1,
		S2 = 2,
		S3 = 3,
		S4 = 4,
		S5 = 5,
		S6 = 6,
		Normal = 10,
		Super = 20
	}

	public enum AccStat : byte
	{
		DamageBonus = 1,
		DamageFactorBonus,
		MeleeDeliveryDamage,
		BossDamageBonus,
		OnFireTargetBonus,
		UltDamageBonus,
		IncomingCut,
		CooldownCut,
		DashCooldownCut,
		LungeCooldownCut,
		MoveSpeedBonus,
		WaterSpeedBonus,
		FlightEnergySec,
		FallDmgTakenMul,
		AffinityAmp,
		EnergyGainAdd,
		EnergyGainMul,
		UltRetain,
		XpHeldMul,
		XpHotbarShareMul,
		HomingTurn,
		HomingBolt,
		HomingSpread,
		HomingBarrage,
		HomingDoTBind,
		TilePierceBolt,
		TilePierceSpread,
		TilePierceBarrage,
		TilePierceDoTBind,
		TilePierceBeam,
		PenetrateAdd,
		ChoiceLockSkill2,
		ChoiceLockUlt,
		ChoiceDamage,
		LifeOrbDamage,
		LifeOrbHpDrain,
		LifeOrbGateTicks,
		ShellBellHeal,
		ShellBellCdTicks,
		RockyHelmetScale,
		RockyHelmetCdTicks,
		LeftoversHpPerSec,
		LeftoversLowHpBonus,
		FocusSash,
		FocusSashHpPct,
		FocusSashCdSec,
		FocusSashImmuneTicks,
		EvioliteDefMul,
		EvioliteDamage,
		EverstoneBlock,
		CritUpgradeChance,
		OnFireCritUpgrade,
		FireMoveDamage,
		PassiveEnergyMul,
		DashSpeedBonus,
		LungeIFrameBonus,
		PsychicDragonDamage,
		GuardCutTimer,
		HomingRange
	}

	public readonly struct AccStatLine
	{
		public AccStat Stat { get; }
		public float Value { get; }

		public AccStatLine(AccStat stat, float value)
		{
			Stat = stat;
			Value = value;
		}

		public static AccStatLine Of(AccStat stat, float value) => new(stat, value);
		public static AccStatLine Flag(AccStat stat) => new(stat, 1f);
	}

	public enum AccLootKind : byte
	{
		Craft = 0,
		Crate = 1,
		EventNpc = 2,
		BossVanilla = 3,
		BossCalamity = 4
	}

	public sealed class AccLootSpec
	{
		public AccLootKind Kind { get; init; }
		public int VanillaId { get; init; }
		public int Stack { get; init; } = 1;
		public float Chance { get; init; } = 1f;
		public string CalamityNpc { get; init; }
		public int CraftTile { get; init; }
		public (int ItemId, int Stack)[] Craft { get; init; }
	}
}
