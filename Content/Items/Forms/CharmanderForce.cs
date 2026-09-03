using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	/// <summary>L01_F01 小火龙之力（需求 §9.1 / §9.3：爪击 / 火花弹，碰撞 A，无穿障）。</summary>
	public class CharmanderForce : HenshinForceItem
	{
		public const string FormId = "L01_F01";

		protected override int BaseDamage => 10;

		protected override FormDefinition CreateDefinition() => new()
		{
			FormId = FormId,
			NetworkId = 1,
			DisplayNameKey = "Mods.PokemonHenshin.Items.CharmanderForce.DisplayName",
			TexturePath = "PokemonHenshin/Assets/Forms/L01_F01",
			TextureFacesLeft = true,
			Primary = PokemonType.Fire,
			Stage = 1,
			EvolvesFrom = null,
			Collision = CollisionTier.A,
			GrantsPhasing = false,
			HenshinDamageFactor = 1f,
			Role = FormRole.Combat
		};

		protected override MoveSpec CreateMoveA() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.Scratch",
			ProjectileType = ModContent.ProjectileType<ScratchSlashProj>(),
			DamageMultiplier = 1f,
			UseTime = 18,
			ShootSpeed = 0f,
			Knockback = 3f,
			KeyConflict = KeyConflictLevel.None,
			NetRisk = NetRisk.Low,
			Collision = CollisionTier.A
		};

		protected override MoveSpec CreateMoveB() => new()
		{
			NameKey = "Mods.PokemonHenshin.Moves.Ember",
			ProjectileType = ModContent.ProjectileType<EmberBoltProj>(),
			DamageMultiplier = 1.3f,
			UseTime = 28,
			ShootSpeed = 9f,
			Knockback = 1.5f,
			KeyConflict = KeyConflictLevel.RightClick,
			NetRisk = NetRisk.Low,
			Collision = CollisionTier.A
		};
	}
}
