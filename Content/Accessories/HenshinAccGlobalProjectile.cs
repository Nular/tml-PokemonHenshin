using System;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Accessories
{
	/// <summary>子弹继承 Delivery/Homing；诅咒之符穿墙与穿透。</summary>
	public sealed class HenshinAccGlobalProjectile : GlobalProjectile
	{
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (projectile.ModProjectile is not IHenshinMoveProj child)
				return;

			if (source is EntitySource_Parent { Entity: Projectile parent }
				&& parent.ModProjectile is IHenshinMoveProj p)
			{
				child.Homing |= p.Homing;
				child.HomingTurnRate = Math.Max(child.HomingTurnRate, p.HomingTurnRate);
				if (child.Delivery == MoveDelivery.None)
					child.Delivery = p.Delivery;
				child.EasyCrit |= p.EasyCrit;
				child.IgnoreDefensePartial |= p.IgnoreDefensePartial;
			}

			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;
			Player owner = Main.player[projectile.owner];
			if (owner == null || !owner.active)
				return;

			HenshinPlayer hp = owner.GetModPlayer<HenshinPlayer>();
			if (child.Delivery == MoveDelivery.None
				&& owner.HeldItem?.ModItem is HenshinForceItem force)
			{
				MoveSpec move = force.GetMove(hp.LastMoveSlot);
				if (move != null)
					child.Delivery = move.Delivery;
			}

			if (hp.ShouldHoming(child.Delivery))
			{
				child.Homing = true;
				child.HomingTurnRate = Math.Max(child.HomingTurnRate, hp.HomingTurn);
			}

			if (hp.ShouldTilePierce(child.Delivery))
				projectile.tileCollide = false;

			if (hp.PenetrateAdd > 0 && projectile.penetrate > 0)
				projectile.penetrate += hp.PenetrateAdd;
		}
	}
}
