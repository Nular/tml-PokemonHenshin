namespace PokemonHenshin.Content.Core
{
	/// <summary>招式交付类型。广角镜追踪不含 Beam / Field；诅咒符穿墙另加 Beam、不含 Field。</summary>
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
			HomingEligible(d) || d is MoveDelivery.Beam;

		public static bool MeleeShort(MoveDelivery d) =>
			d is MoveDelivery.MeleeArc or MoveDelivery.Lunge or MoveDelivery.StrikeFall;
	}
}
