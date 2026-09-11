using System;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>线/自管弹的实心块探测；诅咒之符用 <see cref="OwnerPierces"/> 跳过。</summary>
	internal static class HenshinTileRay
	{
		public static bool OwnerPierces(Projectile proj, MoveDelivery fallback = MoveDelivery.None)
		{
			if (proj.owner < 0 || proj.owner >= Main.maxPlayers)
				return false;
			Player owner = Main.player[proj.owner];
			if (owner == null || !owner.active)
				return false;
			MoveDelivery d = fallback;
			if (proj.ModProjectile is IHenshinMoveProj tagged && tagged.Delivery != MoveDelivery.None)
				d = tagged.Delivery;
			return d != MoveDelivery.None && owner.GetModPlayer<HenshinPlayer>().ShouldTilePierce(d);
		}

		public static float FirstSolidDistance(Vector2 from, Vector2 dir, float maxLen, float step = 8f)
		{
			if (maxLen <= 0f)
				return 0f;
			if (dir.LengthSquared() < 0.0001f)
				return maxLen;
			dir.Normalize();
			int n = Math.Max(1, (int)MathF.Ceiling(maxLen / step));
			for (int i = 1; i <= n; i++)
			{
				float d = Math.Min(i * step, maxLen);
				if (Collision.IsWorldPointSolid(from + dir * d, treatPlatformsAsNonSolid: true))
					return d;
			}
			return maxLen;
		}
	}
}
