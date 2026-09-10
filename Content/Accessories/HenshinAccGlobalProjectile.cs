using System;
using System.IO;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PokemonHenshin.Content.Accessories
{
	/// <summary>子弹继承 Delivery/Homing；诅咒之符穿墙与穿透；广角镜漏网弹在 PostAI 转向。</summary>
	public sealed class HenshinAccGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		/// <summary>原版 Flames/Leaf 等可能每帧把 tileCollide 拨回；PostAI 再保一次。</summary>
		public bool HoldTilePierce { get; set; }

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			MoveDelivery delivery = MoveDelivery.None;
			IHenshinMoveProj child = projectile.ModProjectile as IHenshinMoveProj;

			if (source is EntitySource_Parent { Entity: Projectile parent }
				&& parent.ModProjectile is IHenshinMoveProj p)
			{
				if (child != null)
				{
					child.Homing |= p.Homing;
					child.HomingTurnRate = Math.Max(child.HomingTurnRate, p.HomingTurnRate);
					child.HomingRangeTiles = Math.Max(child.HomingRangeTiles, p.HomingRangeTiles);
					child.HomingTargetWhoAmI = -1;
					if (child.Delivery == MoveDelivery.None)
						child.Delivery = p.Delivery;
					child.EasyCrit |= p.EasyCrit;
					child.IgnoreDefensePartial |= p.IgnoreDefensePartial;
				}
				if (delivery == MoveDelivery.None)
					delivery = p.Delivery;
			}

			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;
			Player owner = Main.player[projectile.owner];
			if (owner == null || !owner.active)
				return;

			HenshinPlayer hp = owner.GetModPlayer<HenshinPlayer>();
			if (child != null && child.Delivery == MoveDelivery.None
				&& owner.HeldItem?.ModItem is HenshinForceItem force)
			{
				MoveSpec move = force.GetMove(hp.LastMoveSlot);
				if (move != null)
					child.Delivery = move.Delivery;
			}

			if (child != null && child.Delivery != MoveDelivery.None)
				delivery = child.Delivery;

			if (child != null)
				HenshinProjUtil.ApplyAccessoryHoming(child, hp.ShouldHoming(child.Delivery), hp.HomingTurn, hp.HomingRangeTiles);

			if (hp.ShouldTilePierce(delivery))
			{
				projectile.tileCollide = false;
				HoldTilePierce = true;
			}

			if (child != null && hp.PenetrateAdd > 0 && projectile.penetrate > 0)
				projectile.penetrate += hp.PenetrateAdd;
		}

		public override void PostAI(Projectile projectile)
		{
			if (HoldTilePierce)
				projectile.tileCollide = false;

			if (projectile.ModProjectile is not IHenshinMoveProj child || !child.Homing)
				return;
			if (projectile.ModProjectile is HenshinMoveProj self && self.HandlesOwnHoming)
				return;
			if (projectile.ModProjectile is ModProjectile mp && !mp.ShouldUpdatePosition())
				return;
			HenshinProjUtil.HomingAI(projectile, true, child.HomingTurnRate);
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
		{
			if (projectile.ModProjectile is IHenshinMoveProj child)
				binaryWriter.Write(child.HomingTargetWhoAmI);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			if (projectile.ModProjectile is IHenshinMoveProj child)
				child.HomingTargetWhoAmI = binaryReader.ReadInt32();
		}
	}
}
