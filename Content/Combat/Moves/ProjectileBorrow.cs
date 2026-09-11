using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Damage;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 原版射弹 type 解析（无灾厄 / CWR 运行时依赖）。
	///
	/// 为何很多招式「看起来没用上原版/大修弹」：
	/// 1. 仓库硬约束：禁止运行时依赖 CalamityOverhaul；手感补丁后也不再 NewProjectile 灾厄弹。
	/// 2. 多数 Item.shoot 弹的贴图是空壳或自绘（海蓝权杖水流、大地法杖滚石变体、共鸣权杖等），
	///    直接画 TextureAssets.Projectile[shoot] 会「有伤无图」。
	/// 3. 原版弹 AI 强绑 Magic/Summon 与持有武器状态，直接生成会导致伤害类型错误、联机不同步。
	/// 因此标准做法是：本模壳弹 + 固定可见贴图（Bubble/Seed/Boulder/RainbowRod/Typhoon）+ 自写 AI，
	/// 参数参考原版/CWR（只读），视觉对标而非 type 克隆。
	/// </summary>
	public static class ProjectileBorrow
	{
		public static int ItemShoot(int itemId)
		{
			if (itemId <= 0 || !ContentSamples.ItemsByType.TryGetValue(itemId, out Item sample) || sample == null)
				return ProjectileID.None;
			return sample.shoot;
		}

		public static void SafeLoadProjectile(int projectileType)
		{
			if (Main.dedServ || Main.instance == null || projectileType <= 0)
				return;
			Main.instance.LoadProjectile(projectileType);
		}

		/// <summary>
		/// 强制加载原版弹贴图。TextureAssets.Projectile 懒加载：
		/// 未用过泡泡枪时 Bubble 仍是 1×1 占位 → Borrow 壳弹「无图」；
		/// 真正 NewProjectile(Leaf) 会顺带加载，故飞叶正常。
		/// </summary>
		public static Texture2D RequestProjectileTexture(int projectileType)
		{
			if (projectileType <= 0)
				projectileType = ProjectileID.WoodenArrowFriendly;
			SafeLoadProjectile(projectileType);
			Asset<Texture2D> asset = TextureAssets.Projectile[projectileType];
			return asset.Value;
		}

		/// <summary>将可见贴图回退到已知非空 ID（避免 Item.shoot 空贴图）。</summary>
		public static int VisibleTextureFallback(int preferredShoot, int modeHint = 0)
		{
			if (preferredShoot == ItemShoot(ItemID.BubbleGun) || preferredShoot == ProjectileID.Bubble)
				return ProjectileID.Bubble;
			if (preferredShoot == ProjectileID.Seed || preferredShoot == ProjectileID.SeedlerThorn)
				return preferredShoot > 0 ? preferredShoot : ProjectileID.Seed;
			if (preferredShoot == ItemShoot(ItemID.PrincessWeapon) || preferredShoot <= 0)
				return ProjectileID.RainbowRodBullet;
			if (preferredShoot == ItemShoot(ItemID.StaffofEarth))
				return ProjectileID.Boulder;
			if (preferredShoot == ItemShoot(ItemID.AquaScepter))
				return ProjectileID.WaterStream;
			return preferredShoot;
		}

		public static void RetargetAsHenshin(Projectile proj)
		{
			if (proj == null || !proj.active)
				return;
			proj.friendly = true;
			proj.hostile = false;
			proj.DamageType = HenshinDamage.Instance;
		}

		/// <summary>从父弹复制出弹槽后再 Retarget（UltDamageBonus 等）。</summary>
		public static void RetargetAsHenshinFrom(Projectile parent, Projectile proj)
		{
			RetargetAsHenshin(proj);
			if (parent != null && proj != null)
				HenshinAccGlobalProjectile.CopyMoveOrigin(parent, proj);
		}
	}
}
