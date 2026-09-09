namespace PokemonHenshin.Content.Core
{
	/// <summary>键位冲突等级（需求 §2.5 招式表必填项）。</summary>
	public enum KeyConflictLevel : byte
	{
		None = 0,
		RightClick = 1,
		ModKeybind = 2
	}

	public enum NetRisk : byte
	{
		Low = 0,
		Medium = 1,
		High = 2
	}

	public enum MoveSlot : byte
	{
		Skill1 = 0,
		Skill2 = 1,
		Ultimate = 2
	}

	/// <summary>招式元数据。技能1/2/大招共用。</summary>
	public sealed class MoveSpec
	{
		public string NameKey { get; init; }
		public int ProjectileType { get; init; }
		public float DamageMultiplier { get; init; } = 1f;
		public int UseTime { get; init; } = 20;
		/// <summary>可写：工厂在构造后标记 MultiHit / WideAoE 等。</summary>
		public BalanceTag BalanceTag { get; set; } = BalanceTag.Standard;

		public float GetEnergyGainFactor() => HenshinStatService.EnergyGainFactor(BalanceTag, UseTime);
		public float ShootSpeed { get; init; } = 0f;
		public float Knockback { get; init; } = 2f;

		public KeyConflictLevel KeyConflict { get; init; } = KeyConflictLevel.None;
		public NetRisk NetRisk { get; init; } = NetRisk.Low;
		public CollisionTier Collision { get; init; } = CollisionTier.A;
		public bool GrantsPhasing { get; init; }

		/// <summary>写入弹幕 ai[0]：Dust 或子类型。</summary>
		public float Ai0 { get; init; }
		/// <summary>写入弹幕 ai[1]：BuffID 等。</summary>
		public float Ai1 { get; init; }
		/// <summary>写入弹幕 ai[2]：扩展。</summary>
		public float Ai2 { get; init; }

		public MoveDelivery Delivery { get; init; }
		/// <summary>历史字段。广角镜追踪改看 <see cref="Delivery"/>，不要再用本字段决定 Homing。</summary>
		public bool IsRangedProjectile { get; init; }
		/// <summary>在鼠标世界坐标生成（落点技 / 指针大招）。</summary>
		public bool SpawnAtMouse { get; init; }
		/// <summary>撞击类：0.5s 内置 CD。</summary>
		public bool RequiresLungeCooldown { get; init; }
		public bool EasyCrit { get; init; }
		public bool IgnoreDefensePartial { get; init; }
		public bool RecoilSelf { get; init; }
		public float RecoilFraction { get; init; } = 0.08f;
		public bool CountsAsFireMove { get; init; }
		public bool CountsAsWaterMove { get; init; }
		public bool CountsAsGrassMove { get; init; }
		public int AftermathDamagePenaltyTicks { get; init; }
		public float AftermathDamagePenalty { get; init; }
		public int SelfStunTicks { get; init; }
	}
}
