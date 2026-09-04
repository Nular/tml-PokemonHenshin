namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 单个形态的静态定义。数据驱动：物品、Overlay、招式与被动都从这里读元数据。
	/// </summary>
	public sealed class FormDefinition
	{
		public string FormId { get; init; }
		public ushort NetworkId { get; init; }
		public string DisplayNameKey { get; init; }
		public string TexturePath { get; init; }
		public bool TextureFacesLeft { get; init; } = true;

		public PokemonType Primary { get; init; }
		public PokemonType Secondary { get; init; } = PokemonType.None;

		public int Stage { get; init; } = 1;
		public string EvolvesFrom { get; init; }

		public CollisionTier Collision { get; init; } = CollisionTier.A;
		public bool GrantsPhasing { get; init; }

		public float HenshinDamageFactor { get; init; } = 1f;
		public FormRole Role { get; init; } = FormRole.Combat;

		public FormPassiveKind Passive { get; init; } = FormPassiveKind.None;
		public float EnergyMax { get; init; } = 100f;

		/// <summary>获取条件本地化键（可选）。</summary>
		public string AcquireHintKey { get; init; }

		public MoveSpec Move1 { get; internal set; }
		public MoveSpec Move2 { get; internal set; }
		public MoveSpec Ultimate { get; internal set; }

		/// <summary>兼容旧名。</summary>
		public MoveSpec MoveA { get => Move1; internal set => Move1 = value; }
		public MoveSpec MoveB { get => Move2; internal set => Move2 = value; }

		public int ItemType { get; internal set; }

		public override string ToString() => $"{FormId}(#{NetworkId})";
	}
}
