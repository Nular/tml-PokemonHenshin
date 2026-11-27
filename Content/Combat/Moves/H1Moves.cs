using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// H1 组（喷火龙/水箭龟/妙蛙花）重做招式的 MoveSpec。字段逐项抄自原工厂（FormItemUtil.MouseAoE / AuraSphere / PetalDance / ChargeBeamUlt），
	/// 仅 ProjectileType 换成新射弹；MoveSpec.ProjectileType 是 init，所以这里整体重建。
	/// </summary>
	internal static class H1Moves
	{
		public static MoveSpec Overheat(string nameKey, float mult)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<OverheatBlastProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				Knockback = 5f,
				Ai0 = DustID.Torch,
				Ai1 = BuffID.OnFire,
				SpawnAtMouse = true,
				AftermathDamagePenaltyTicks = 300,
				AftermathDamagePenalty = 0.5f,
				KeyConflict = KeyConflictLevel.ModKeybind,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec AuraSphere(string nameKey, float mult, int use)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AuraSphereOrbProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 9f,
				Knockback = 3.5f,
				IsRangedProjectile = true
			};

		public static MoveSpec PetalDance(string nameKey, float mult)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<PetalDanceH1Proj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				CountsAsGrassMove = true
			};

		public static MoveSpec SolarBeam(string nameKey, float mult)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SolarBeamChargeProj>(),
				DamageMultiplier = mult,
				UseTime = 55,
				ShootSpeed = 0f,
				Ai0 = DustID.GoldFlame,
				CountsAsGrassMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};
	}
}
