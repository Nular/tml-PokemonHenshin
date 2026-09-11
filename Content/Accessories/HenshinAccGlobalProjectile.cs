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
	/// <summary>子弹继承 Delivery/Homing；诅咒之符穿墙与穿透；广角镜漏网弹在 PostAI 转向。
	/// SourceMoveSlot / CombatEnergyFactor：出弹瞬间钉死，命中不读玩家当前 LastMoveSlot
	///（凯西精神强念等延迟散射在按住技能时也会误充能）。</summary>
	public sealed class HenshinAccGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		/// <summary>原版 Flames/Leaf 等可能每帧把 tileCollide 拨回；PostAI 再保一次。</summary>
		public bool HoldTilePierce { get; set; }

		/// <summary>出弹时的 MoveSlot；命中能量 / UltDamageBonus 读此字段。</summary>
		public MoveSlot SourceMoveSlot { get; set; }

		public bool HasSourceMoveSlot { get; set; }

		/// <summary>出弹时钉死的战斗能量系数；大招为 0。命中优先用此值。</summary>
		public float CombatEnergyFactor { get; set; }

		public bool HasCombatEnergyFactor { get; set; }

		public static void StampMoveOrigin(Projectile projectile, MoveSlot slot, float combatEnergyFactor)
		{
			if (projectile == null || !projectile.active)
				return;
			HenshinAccGlobalProjectile gp = projectile.GetGlobalProjectile<HenshinAccGlobalProjectile>();
			gp.SourceMoveSlot = slot;
			gp.HasSourceMoveSlot = true;
			gp.CombatEnergyFactor = combatEnergyFactor;
			gp.HasCombatEnergyFactor = true;
			if (projectile.ModProjectile is IHenshinMoveProj tagged)
			{
				tagged.SourceMoveSlot = slot;
				tagged.HasSourceMoveSlot = true;
				tagged.CombatEnergyFactor = combatEnergyFactor;
				tagged.HasCombatEnergyFactor = true;
			}
			projectile.netUpdate = true;
		}

		public static void CopyMoveOrigin(Projectile from, Projectile to)
		{
			if (from == null || to == null || !to.active)
				return;
			HenshinAccGlobalProjectile src = from.GetGlobalProjectile<HenshinAccGlobalProjectile>();
			if (src.HasSourceMoveSlot || src.HasCombatEnergyFactor)
			{
				MoveSlot slot = src.HasSourceMoveSlot ? src.SourceMoveSlot : MoveSlot.Skill1;
				float factor = src.HasCombatEnergyFactor
					? src.CombatEnergyFactor
					: (slot == MoveSlot.Ultimate ? 0f : 1f);
				StampMoveOrigin(to, slot, factor);
				return;
			}
			if (from.ModProjectile is IHenshinMoveProj parent && parent.HasSourceMoveSlot)
			{
				float factor = parent.HasCombatEnergyFactor
					? parent.CombatEnergyFactor
					: (parent.SourceMoveSlot == MoveSlot.Ultimate ? 0f : 1f);
				StampMoveOrigin(to, parent.SourceMoveSlot, factor);
			}
		}

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			MoveDelivery delivery = MoveDelivery.None;
			IHenshinMoveProj child = projectile.ModProjectile as IHenshinMoveProj;

			if (source is EntitySource_Parent { Entity: Projectile parent })
			{
				HenshinAccGlobalProjectile parentGp = parent.GetGlobalProjectile<HenshinAccGlobalProjectile>();
				if (parentGp.HasSourceMoveSlot)
				{
					SourceMoveSlot = parentGp.SourceMoveSlot;
					HasSourceMoveSlot = true;
				}
				if (parentGp.HasCombatEnergyFactor)
				{
					CombatEnergyFactor = parentGp.CombatEnergyFactor;
					HasCombatEnergyFactor = true;
				}
				if (parent.ModProjectile is IHenshinMoveProj p)
				{
					if (!HasSourceMoveSlot && p.HasSourceMoveSlot)
					{
						SourceMoveSlot = p.SourceMoveSlot;
						HasSourceMoveSlot = true;
						if (!HasCombatEnergyFactor)
						{
							CombatEnergyFactor = p.SourceMoveSlot == MoveSlot.Ultimate ? 0f : 1f;
							HasCombatEnergyFactor = true;
						}
					}
					if (child != null)
					{
						if (!HenshinProjUtil.BlocksAccessoryHoming(child))
						{
							child.Homing |= p.Homing;
							child.HomingTurnRate = Math.Max(child.HomingTurnRate, p.HomingTurnRate);
							child.HomingRangeTiles = Math.Max(child.HomingRangeTiles, p.HomingRangeTiles);
						}
						child.HomingTargetWhoAmI = -1;
						if (child.Delivery == MoveDelivery.None)
							child.Delivery = p.Delivery;
						child.EasyCrit |= p.EasyCrit;
						child.IgnoreDefensePartial |= p.IgnoreDefensePartial;
						if (!child.HasSourceMoveSlot && HasSourceMoveSlot)
						{
							child.SourceMoveSlot = SourceMoveSlot;
							child.HasSourceMoveSlot = true;
						}
						if (!child.HasCombatEnergyFactor && HasCombatEnergyFactor)
						{
							child.CombatEnergyFactor = CombatEnergyFactor;
							child.HasCombatEnergyFactor = true;
						}
					}
					if (delivery == MoveDelivery.None)
						delivery = p.Delivery;
				}
			}

			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;
			Player owner = Main.player[projectile.owner];
			if (owner == null || !owner.active)
				return;

			HenshinPlayer hp = owner.GetModPlayer<HenshinPlayer>();
			// 有父弹时不要回退 LastMoveSlot：延迟散射出子弹时玩家往往已按住技能，
			// 否则会先被钉成 Skill 能量系数；应由父继承或导演侧 CopyMoveOrigin 负责。
			bool hasProjectileParent = source is EntitySource_Parent { Entity: Projectile };
			if (!hasProjectileParent && !HasSourceMoveSlot && owner.HeldItem?.ModItem is HenshinForceItem forceForSlot)
			{
				SourceMoveSlot = hp.LastMoveSlot;
				HasSourceMoveSlot = true;
				if (!HasCombatEnergyFactor)
				{
					MoveSpec stamped = forceForSlot.GetMove(hp.LastMoveSlot);
					CombatEnergyFactor = hp.LastMoveSlot == MoveSlot.Ultimate
						? 0f
						: (stamped?.GetEnergyGainFactor() ?? 1f);
					HasCombatEnergyFactor = true;
				}
				if (child != null && !child.HasSourceMoveSlot)
				{
					child.SourceMoveSlot = SourceMoveSlot;
					child.HasSourceMoveSlot = true;
				}
				if (child != null && !child.HasCombatEnergyFactor && HasCombatEnergyFactor)
				{
					child.CombatEnergyFactor = CombatEnergyFactor;
					child.HasCombatEnergyFactor = true;
				}
			}

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
			bitWriter.WriteBit(HasSourceMoveSlot);
			if (HasSourceMoveSlot)
				binaryWriter.Write((byte)SourceMoveSlot);
			bitWriter.WriteBit(HasCombatEnergyFactor);
			if (HasCombatEnergyFactor)
				binaryWriter.Write(CombatEnergyFactor);
			if (projectile.ModProjectile is IHenshinMoveProj child)
				binaryWriter.Write(child.HomingTargetWhoAmI);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			HasSourceMoveSlot = bitReader.ReadBit();
			if (HasSourceMoveSlot)
				SourceMoveSlot = (MoveSlot)binaryReader.ReadByte();
			HasCombatEnergyFactor = bitReader.ReadBit();
			if (HasCombatEnergyFactor)
				CombatEnergyFactor = binaryReader.ReadSingle();
			if (projectile.ModProjectile is IHenshinMoveProj child)
				child.HomingTargetWhoAmI = binaryReader.ReadInt32();
		}
	}
}
