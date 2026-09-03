namespace PokemonHenshin.Content.Core
{
	/// <summary>宝可梦属性（需求 §3）。仅用于被动框架、招式主题与共鸣饰品，不做对敌克制。</summary>
	public enum PokemonType : byte
	{
		None = 0,
		Normal,
		Fire,
		Water,
		Grass,
		Electric,
		Ice,
		Fighting,
		Poison,
		Ground,
		Flying,
		Psychic,
		Bug,
		Rock,
		Ghost,
		Dragon,
		Dark,
		Steel,
		Fairy
	}

	/// <summary>碰撞级别（需求 §2.4）。M0～M3 仅 A。</summary>
	public enum CollisionTier : byte
	{
		A = 0,
		B = 1,
		C = 2
	}

	/// <summary>形态定位（需求 §9.1「定位」列）。</summary>
	public enum FormRole : byte
	{
		Combat = 0,
		Utility = 1,
		Legendary = 2
	}
}
