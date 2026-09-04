using Terraria;
using Terraria.ID;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>原版射弹 type 解析（无灾厄运行时依赖）。</summary>
	public static class ProjectileBorrow
	{
		public static int ItemShoot(int itemId)
		{
			if (itemId <= 0 || !ContentSamples.ItemsByType.TryGetValue(itemId, out Item sample) || sample == null)
				return ProjectileID.None;
			return sample.shoot;
		}

		public static void RetargetAsHenshin(Projectile proj)
		{
			if (proj == null || !proj.active)
				return;
			proj.friendly = true;
			proj.hostile = false;
			proj.DamageType = Damage.HenshinDamage.Instance;
		}
	}
}
