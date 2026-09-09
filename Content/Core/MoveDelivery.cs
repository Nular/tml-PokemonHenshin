namespace PokemonHenshin.Content.Core
{
	/// <summary>招式交付类型。饰品按集合过滤（广角镜不含 Beam）。</summary>
	public enum MoveDelivery : byte
	{
		None = 0,
		MeleeArc,
		Lunge,
		StrikeFall,
		Bolt,
		Spread,
		Barrage,
		Beam,
		AoEBurst,
		DoTBind,
		Field,
		Dig,
		Blink
	}

	public static class MoveDeliverySets
	{
		public static bool HomingEligible(MoveDelivery d) =>
			d is MoveDelivery.Bolt or MoveDelivery.Spread or MoveDelivery.Barrage or MoveDelivery.DoTBind;

		public static bool TilePierceEligible(MoveDelivery d) =>
			HomingEligible(d) || d == MoveDelivery.Beam;

		public static bool MeleeShort(MoveDelivery d) =>
			d is MoveDelivery.MeleeArc or MoveDelivery.Lunge or MoveDelivery.StrikeFall;
	}
}
