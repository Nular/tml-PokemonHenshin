using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat
{
	/// <summary>
	/// 「xxx之力」物品基类。持握 = 变身（由 <see cref="PlayerState.HenshinPlayer"/> 判定），
	/// 左键招式 A、右键招式 B（需求 §2.5）。物品本身不做近战碰撞，一切伤害走弹幕。
	/// </summary>
	public abstract class HenshinForceItem : ModItem
	{
		/// <summary>
		/// 仅模板实例（加载期）使用的定义缓存。
		/// 注意：tML 为每个 Item 用默认构造新建 ModItem 实例，实例字段不会从模板复制，
		/// 所以运行期一律走 <see cref="FormRegistry"/> 按 Type 查询，避免实例上的 null。
		/// </summary>
		private FormDefinition templateDefinition;

		/// <summary>形态定义：已注册则取注册表（所有实例共享），否则（加载期）用模板缓存。</summary>
		public FormDefinition Definition => FormRegistry.ByItemType(Type) ?? (templateDefinition ??= CreateDefinition());

		protected abstract FormDefinition CreateDefinition();
		protected abstract MoveSpec CreateMoveA();
		protected abstract MoveSpec CreateMoveB();

		/// <summary>基础伤害（招式倍率与形态系数在其上叠加）。</summary>
		protected abstract int BaseDamage { get; }

		public MoveSpec MoveA => Definition.MoveA;
		public MoveSpec MoveB => Definition.MoveB;

		public override string Texture => Definition.TexturePath;

		public override void SetStaticDefaults()
		{
			FormDefinition def = Definition;
			def.MoveA = CreateMoveA();
			def.MoveB = CreateMoveB();
			FormRegistry.Register(def, Type);
			ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.DamageType = HenshinDamage.Instance;
			Item.damage = BaseDamage;
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item1;
			Item.maxStack = 1;
			Item.shoot = ProjectileID.None;
			Item.shootSpeed = 1f;
		}

		public override bool AltFunctionUse(Player player) => true;

		private MoveSpec CurrentMove(Player player) => player.altFunctionUse == 2 ? MoveB : MoveA;

		public override bool CanUseItem(Player player)
		{
			MoveSpec move = CurrentMove(player);
			if (move == null)
				return false;
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			float cdMul = hp.IsTransformed ? hp.MoveCooldownMultiplier : 1f;
			int use = (int)System.Math.Max(1, System.Math.Round(move.UseTime * cdMul));
			Item.useTime = use;
			Item.useAnimation = use;
			Item.shoot = move.ProjectileType;
			Item.shootSpeed = move.ShootSpeed;
			Item.knockBack = move.Knockback;
			Item.UseSound = player.altFunctionUse == 2 ? SoundID.Item20 : SoundID.Item1;
			return move.ProjectileType > ProjectileID.None || move.GrantsPhasing;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
		{
			damage *= Definition.HenshinDamageFactor;
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.IsTransformed && hp.HenshinDamageFactorBonus != 0f)
				damage *= 1f + hp.HenshinDamageFactorBonus;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			MoveSpec move = CurrentMove(player);
			if (move == null)
				return;
			damage = (int)System.Math.Max(1, System.Math.Round(damage * move.DamageMultiplier));
			if (move.ShootSpeed <= 0f)
				velocity = Vector2.Zero;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			MoveSpec move = CurrentMove(player);
			Vector2 spawn = move != null && move.ShootSpeed <= 0f ? player.MountedCenter : position;
			Projectile.NewProjectile(source, spawn, velocity, type, damage, knockback, player.whoAmI);
			return false;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.Add(new TooltipLine(Mod, "HenshinTransform", Language.GetTextValue("Mods.PokemonHenshin.Common.HoldToTransform")));
			if (MoveA != null && MoveB != null)
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinMoves", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.Moves",
					Language.GetTextValue(MoveA.NameKey),
					Language.GetTextValue(MoveB.NameKey))));
			}
			tooltips.Add(new TooltipLine(Mod, "HenshinNoMount", Language.GetTextValue("Mods.PokemonHenshin.Common.NoMount"))
			{
				OverrideColor = new Color(255, 170, 120)
			});
		}
	}
}
