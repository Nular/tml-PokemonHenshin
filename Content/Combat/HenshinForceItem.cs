using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat
{
	public abstract class HenshinForceItem : ModItem
	{
		private FormDefinition templateDefinition;

		public FormDefinition Definition => FormRegistry.ByItemType(Type) ?? (templateDefinition ??= CreateDefinition());

		protected abstract FormDefinition CreateDefinition();
		protected abstract MoveSpec CreateMoveA();
		protected abstract MoveSpec CreateMoveB();
		protected abstract MoveSpec CreateUltimate();
		protected abstract int BaseDamage { get; }

		public MoveSpec Move1 => Definition.Move1;
		public MoveSpec Move2 => Definition.Move2;
		public MoveSpec Ultimate => Definition.Ultimate;
		public MoveSpec MoveA => Move1;
		public MoveSpec MoveB => Move2;

		public override string Texture => Definition.TexturePath;

		public override void SetStaticDefaults()
		{
			FormDefinition def = Definition;
			def.Move1 = CreateMoveA();
			def.Move2 = CreateMoveB();
			def.Ultimate = CreateUltimate();
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

		public MoveSpec GetMove(MoveSlot slot) => slot switch
		{
			MoveSlot.Skill2 => Move2,
			MoveSlot.Ultimate => Ultimate,
			_ => Move1
		};

		private MoveSpec CurrentMove(Player player)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.AccChoiceBand)
				return Move1;
			return player.altFunctionUse == 2 ? Move2 : Move1;
		}

		public override bool CanUseItem(Player player)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.AccChoiceBand && player.altFunctionUse == 2)
				return false;

			MoveSpec move = CurrentMove(player);
			if (move == null)
				return false;
			if (move.RequiresLungeCooldown && !hp.CanLunge)
				return false;

			float cdMul = hp.IsTransformed ? hp.MoveCooldownMultiplier : 1f;
			int use = (int)System.Math.Max(1, System.Math.Round(move.UseTime * cdMul));
			Item.useTime = use;
			Item.useAnimation = use;
			Item.shoot = move.ProjectileType;
			Item.shootSpeed = move.ShootSpeed;
			Item.knockBack = move.Knockback;
			Item.UseSound = player.altFunctionUse == 2 ? SoundID.Item20 : SoundID.Item1;
			hp.LastMoveSlot = player.altFunctionUse == 2 ? MoveSlot.Skill2 : MoveSlot.Skill1;
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
			FireMove(player, source, CurrentMove(player), position, velocity, damage, knockback);
			return false;
		}

		public bool TryFireUltimate(Player player)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (!hp.IsTransformed || hp.AccChoiceBand || Ultimate == null)
				return false;
			if (!hp.TryConsumeUltimate())
				return false;

			hp.LastMoveSlot = MoveSlot.Ultimate;
			var source = player.GetSource_ItemUse(Item);
			Vector2 velocity = Vector2.Zero;
			if (Ultimate.ShootSpeed > 0f)
			{
				velocity = Main.MouseWorld - player.MountedCenter;
				if (velocity == Vector2.Zero)
					velocity = new Vector2(player.direction, 0f);
				velocity = Vector2.Normalize(velocity) * Ultimate.ShootSpeed;
			}

			int damage = player.GetWeaponDamage(Item);
			damage = (int)System.Math.Max(1, System.Math.Round(damage * Ultimate.DamageMultiplier));
			FireMove(player, source, Ultimate, player.MountedCenter, velocity, damage, Ultimate.Knockback);

			if (Ultimate.RecoilSelf)
				hp.ApplyRecoil(Ultimate.RecoilFraction);
			if (Ultimate.AftermathDamagePenaltyTicks > 0)
				hp.ApplyAftermath(Ultimate.AftermathDamagePenaltyTicks, Ultimate.AftermathDamagePenalty);
			if (Ultimate.SelfStunTicks > 0)
				player.AddBuff(BuffID.Slow, Ultimate.SelfStunTicks);

			return true;
		}

		private void FireMove(Player player, IEntitySource source, MoveSpec move, Vector2 position, Vector2 velocity, int damage, float knockback)
		{
			if (move == null)
				return;
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();

			if (move.RequiresLungeCooldown)
			{
				if (!hp.CanLunge)
					return;
				hp.StartLungeCooldown(120); // 2s
				// 0.25s 无敌帧
				player.immune = true;
				player.immuneTime = System.Math.Max(player.immuneTime, 15);
			}

			if (move.GrantsPhasing)
				hp.TryStartPhasing();

			if (move.ProjectileType <= ProjectileID.None)
				return;

			Vector2 spawn;
			if (move.SpawnAtMouse)
				spawn = Main.MouseWorld;
			else if (move.ShootSpeed <= 0f)
				spawn = player.MountedCenter;
			else
				spawn = position;

			Vector2 shootVel = velocity;
			if (move.SpawnAtMouse && move.ShootSpeed > 0f && shootVel == Vector2.Zero)
			{
				shootVel = Main.MouseWorld - player.MountedCenter;
				if (shootVel == Vector2.Zero)
					shootVel = new Vector2(player.direction, 0f);
				shootVel = Vector2.Normalize(shootVel) * move.ShootSpeed;
			}

			int id = Projectile.NewProjectile(source, spawn, shootVel, move.ProjectileType, damage, knockback, player.whoAmI, move.Ai0, move.Ai1, move.Ai2);
			// NewProjectile 的 position 是左上角；大 AoE 若不校正会偏到鼠标右下。
			if (id >= 0 && id < Main.maxProjectiles && move.SpawnAtMouse)
				Main.projectile[id].Center = Main.MouseWorld;
			if (id >= 0 && id < Main.maxProjectiles && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
			{
				tagged.EasyCrit = move.EasyCrit;
				tagged.Homing = move.IsRangedProjectile && hp.AccWideLens;
				tagged.HomingTurnRate = hp.HomingTurnRate;
				tagged.IgnoreDefensePartial = move.IgnoreDefensePartial;
			}

			if (move.RecoilSelf && !ReferenceEquals(move, Ultimate))
				hp.ApplyRecoil(move.RecoilFraction);
			if (move.AftermathDamagePenaltyTicks > 0 && !ReferenceEquals(move, Ultimate))
				hp.ApplyAftermath(move.AftermathDamagePenaltyTicks, move.AftermathDamagePenalty);
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			Player local = Main.LocalPlayer;
			if (local == null || !local.active)
				return true;

			HenshinPlayer hp = local.GetModPlayer<HenshinPlayer>();
			float max = Definition.EnergyMax;
			float pct = max <= 0f ? 0f : MathHelper.Clamp(hp.GetStoredEnergy(Definition.FormId, max) / max, 0f, 1f);

			int barW = (int)(frame.Width * scale);
			int barH = 3;
			Vector2 barPos = position + new Vector2(-origin.X * scale, frame.Height * scale - origin.Y * scale - barH);
			Rectangle bg = new((int)barPos.X, (int)barPos.Y, barW, barH);
			Rectangle fill = new(bg.X, bg.Y, (int)(barW * pct), barH);
			Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, bg, Color.Black * 0.7f);
			Color bar = pct >= 0.999f ? Color.Gold : new Color(80, 160, 255);
			if (fill.Width > 0)
				spriteBatch.Draw(pixel, fill, bar);
			return true;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.Add(new TooltipLine(Mod, "HenshinTransform", Language.GetTextValue("Mods.PokemonHenshin.Common.HoldToTransform")));
			if (Move1 != null && Move2 != null && Ultimate != null)
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinMoves", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.Moves3",
					Language.GetTextValue(Move1.NameKey),
					Language.GetTextValue(Move2.NameKey),
					Language.GetTextValue(Ultimate.NameKey))));
			}

			Player local = Main.LocalPlayer;
			if (local != null && local.active)
			{
				HenshinPlayer hp = local.GetModPlayer<HenshinPlayer>();
				float max = Definition.EnergyMax;
				float cur = hp.GetStoredEnergy(Definition.FormId, max);
				int curI = (int)System.Math.Round(cur);
				int maxI = (int)System.Math.Round(max);
				bool ready = cur >= max - 0.01f;
				tooltips.Add(new TooltipLine(Mod, "HenshinEnergy", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.UltimateEnergy", curI, maxI))
				{
					OverrideColor = ready ? new Color(255, 215, 80) : new Color(120, 180, 255)
				});
			}

			if (!string.IsNullOrEmpty(Definition.AcquireHintKey))
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinAcquire", Language.GetTextValue(Definition.AcquireHintKey))
				{
					OverrideColor = new Color(180, 220, 255)
				});
			}
			tooltips.Add(new TooltipLine(Mod, "HenshinNoMount", Language.GetTextValue("Mods.PokemonHenshin.Common.NoMount"))
			{
				OverrideColor = new Color(255, 170, 120)
			});
		}
	}

	public interface IHenshinMoveProj
	{
		bool EasyCrit { get; set; }
		bool Homing { get; set; }
		float HomingTurnRate { get; set; }
		bool IgnoreDefensePartial { get; set; }
	}
}
