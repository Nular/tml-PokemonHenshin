namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 单个形态的静态定义（dev-plan §4.4）。数据驱动：物品、Overlay、招式与被动都从这里读元数据。
	/// </summary>
	public sealed class FormDefinition
	{
		/// <summary>内部 ID，形如 "L01_F01"（链/形态）。</summary>
		public string FormId { get; init; }

		/// <summary>稳定网络 ID，联机用；0 保留为「无形态」。</summary>
		public ushort NetworkId { get; init; }

		/// <summary>显示名本地化键（需求 §2.7：显示名必须可替换）。</summary>
		public string DisplayNameKey { get; init; }

		/// <summary>Overlay 贴图路径（不含扩展名），可配置覆盖。</summary>
		public string TexturePath { get; init; }

		/// <summary>贴图原图是否朝左；为 true 时玩家朝右需水平翻转。</summary>
		public bool TextureFacesLeft { get; init; } = true;

		public PokemonType Primary { get; init; }
		public PokemonType Secondary { get; init; } = PokemonType.None;

		/// <summary>解锁所需进度档 1～12（需求 §4.1）。</summary>
		public int Stage { get; init; } = 1;

		/// <summary>进化自哪个 FormId；null 表示链首。</summary>
		public string EvolvesFrom { get; init; }

		public CollisionTier Collision { get; init; } = CollisionTier.A;
		public bool GrantsPhasing { get; init; }

		/// <summary>每形态伤害微调系数（需求 §2.6，默认 1.0）。</summary>
		public float HenshinDamageFactor { get; init; } = 1f;

		public FormRole Role { get; init; } = FormRole.Combat;

		/// <summary>招式 A（默认左键）/ B（默认右键）。由物品在 SetStaticDefaults 中填入，随定义一起注册。</summary>
		public MoveSpec MoveA { get; internal set; }
		public MoveSpec MoveB { get; internal set; }

		/// <summary>对应「之力」物品的 ItemType，由注册表在物品加载后回填。</summary>
		public int ItemType { get; internal set; }

		public override string ToString() => $"{FormId}(#{NetworkId})";
	}
}
