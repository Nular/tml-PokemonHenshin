namespace PokemonHenshin.Content.Core
{
	/// <summary>键位冲突等级（需求 §2.5 招式表必填项）。</summary>
	public enum KeyConflictLevel : byte
	{
		/// <summary>默认左键，无冲突。</summary>
		None = 0,
		/// <summary>右键；可能与原版右键交互（开门/开箱等）竞争。</summary>
		RightClick = 1,
		/// <summary>需要独立 Mod 热键。</summary>
		ModKeybind = 2
	}

	/// <summary>联机风险等级（需求 §2.5）。</summary>
	public enum NetRisk : byte
	{
		Low = 0,
		Medium = 1,
		High = 2
	}

	/// <summary>
	/// 招式元数据（dev-plan §4.4 MoveSpec）。数值为占位，M4 迭代。
	/// </summary>
	public sealed class MoveSpec
	{
		public string NameKey { get; init; }

		/// <summary>发射的弹幕类型；由物品在 SetDefaults 之后解析。</summary>
		public int ProjectileType { get; init; }

		/// <summary>相对物品基础伤害的倍率。</summary>
		public float DamageMultiplier { get; init; } = 1f;
		public int UseTime { get; init; } = 20;
		public float ShootSpeed { get; init; } = 0f;
		public float Knockback { get; init; } = 2f;

		public KeyConflictLevel KeyConflict { get; init; } = KeyConflictLevel.None;
		public NetRisk NetRisk { get; init; } = NetRisk.Low;
		public CollisionTier Collision { get; init; } = CollisionTier.A;
		public bool GrantsPhasing { get; init; }
	}
}
