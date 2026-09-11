using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.Evolution;
using PokemonHenshin.Content.PlayerState;
using PokemonHenshin.Content.Prefixes;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace PokemonHenshin.Content.Combat
{
	public abstract class HenshinForceItem : ModItem
	{
		[CloneByReference]
		private FormDefinition templateDefinition;

		public int Level { get; private set; }
		public int Xp { get; private set; }

		protected override bool CloneNewInstances => true;

		public FormDefinition Definition => FormRegistry.ByItemType(Type) ?? (templateDefinition ??= CreateDefinition());

		protected abstract FormDefinition CreateDefinition();
		protected abstract MoveSpec CreateMoveA();
		protected abstract MoveSpec CreateMoveB();
		protected abstract MoveSpec CreateUltimate();

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
			Item.knockBack = 2f;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item1;
			Item.maxStack = 1;
			Item.shoot = ProjectileID.None;
			Item.shootSpeed = 1f;
			InitializeNewIfNeeded();
			RefreshDamage();
		}

		public override ModItem Clone(Item newEntity)
		{
			HenshinForceItem clone = (HenshinForceItem)base.Clone(newEntity);
			clone.Level = Level;
			clone.Xp = Xp;
			clone.RefreshDamage();
			return clone;
		}

		public override void SaveData(TagCompound tag)
		{
			tag["level"] = Level;
			tag["xp"] = Xp;
		}

		public override void LoadData(TagCompound tag)
		{
			if (tag.ContainsKey("level"))
			{
				Level = tag.GetInt("level");
				Xp = tag.ContainsKey("xp") ? tag.GetInt("xp") : 0;
			}

			InitializeNewIfNeeded();
			RefreshDamage();
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(Level);
			writer.Write(Xp);
		}

		public override void NetReceive(BinaryReader reader)
		{
			Level = reader.ReadInt32();
			Xp = reader.ReadInt32();
			RefreshDamage();
		}

		public void SetProgress(int level, int xp)
		{
			Level = Math.Clamp(level, HenshinStatService.MinLevel, HenshinStatService.MaxLevel);
			Xp = Math.Max(0, xp);
			RefreshDamage();
		}

		public void InitializeNewIfNeeded()
		{
			if (Level >= HenshinStatService.MinLevel)
				return;
			FormDefinition def = Definition;
			int formStage = def?.Stage ?? 1;
			Level = HenshinStatService.StartingLevelForFormStage(formStage);
			Xp = 0;
		}

		public void RefreshDamage()
		{
			InitializeNewIfNeeded();
			Item.damage = ComputeFinalAttack();
		}

		public int ComputeFinalAttack()
		{
			FormDefinition def = Definition;
			float atkMod = def?.AttackMod ?? 1f;
			float factor = def?.HenshinDamageFactor ?? 1f;
			return HenshinStatService.FinalAttack(Level, atkMod, factor);
		}

		public int ComputeFinalDefense()
		{
			FormDefinition def = Definition;
			float defMod = def?.DefenseMod ?? 1f;
			return HenshinStatService.FinalDefense(Level, defMod);
		}

		public bool TryAddExperience(Player player, int amount, out int levelsGained, out bool crossedEvolveBand, out int xpApplied)
		{
			levelsGained = 0;
			crossedEvolveBand = false;
			xpApplied = 0;
			if (amount <= 0)
				return false;

			InitializeNewIfNeeded();
			int world = SafeWorldStage();
			if (!HenshinStatService.CanGainExperience(Level, world))
				return false;

			int oldLevel = Level;
			ForceProgress next = HenshinStatService.AddExperience(Level, Xp, amount, world, out levelsGained);
			if (next.Level == Level && next.Xp == Xp)
				return false;

			Level = next.Level;
			Xp = next.Xp;
			xpApplied = amount;
			RefreshDamage();

			FormDefinition nextForm = FormRegistry.FindEvolutionOf(Definition?.FormId);
			if (nextForm != null)
				crossedEvolveBand = HenshinStatService.CrossedBandMin(oldLevel, Level, nextForm.Stage);

			if (levelsGained > 0 && nextForm != null && player != null && player.whoAmI == Main.myPlayer
				&& HenshinStatService.MeetsEvolution(world, Level, nextForm.Stage))
				player.GetModPlayer<EvolutionOfferPlayer>().TryOfferAfterLevelUp(this);

			return true;
		}

		private static int SafeWorldStage()
		{
			try
			{
				return ProgressStageService.GetProgressStage();
			}
			catch
			{
				return 1;
			}
		}

		public override bool AltFunctionUse(Player player) => true;

		/// <summary>之力只走本模三专属；不进原版通用武器前缀池。</summary>
		public override bool WeaponPrefix() => false;

		public override bool MeleePrefix() => false;

		public override bool RangedPrefix() => false;

		public override bool MagicPrefix() => false;

		public override int ChoosePrefix(UnifiedRandom rand)
			=> HenshinForcePrefix.RollExclusive(rand);

		public override bool AllowPrefix(int pre)
			=> HenshinForcePrefix.IsExclusive(pre);

		public override bool? PrefixChance(int pre, UnifiedRandom rand)
		{
			// 允许哥布林重铸槽与自然/重铸 roll。
			if (pre == -3 || pre == -2 || pre == -1)
				return true;
			// 加载时剥掉原版（或其它模组）非专属前缀。
			if (pre > 0 && !HenshinForcePrefix.IsExclusive(pre))
				return false;
			return null;
		}

		public MoveSpec GetMove(MoveSlot slot) => slot switch
		{
			MoveSlot.Skill2 => Move2,
			MoveSlot.Ultimate => Ultimate,
			_ => Move1
		};

		private MoveSpec CurrentMove(Player player)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.ChoiceLockSkill2)
				return Move1;
			return player.altFunctionUse == 2 ? Move2 : Move1;
		}

		public override bool CanUseItem(Player player)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.ChoiceLockSkill2 && player.altFunctionUse == 2)
				return false;

			MoveSpec move = CurrentMove(player);
			if (move == null)
				return false;
			if (move.RequiresLungeCooldown && !hp.CanLunge)
				return false;

			float cdMul = hp.IsTransformed ? hp.MoveCooldownMultiplier : 1f;
			if (hp.IsTransformed && MoveDeliverySets.MeleeShort(move.Delivery))
				cdMul *= hp.LungeCooldownMultiplier;
			int use = (int)Math.Max(1, Math.Round(move.UseTime * cdMul));
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
			// HenshinDamageFactor 已计入 Item.damage = FinalAttack（存档等级）；这里只叠饰品乘区。
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (hp.IsTransformed)
			{
				if (hp.HenshinDamageBonus != 0f)
					damage *= 1f + hp.HenshinDamageBonus;
				if (hp.HenshinDamageFactorBonus != 0f)
					damage *= 1f + hp.HenshinDamageFactorBonus;
				if (hp.ChoiceDamage != 0f)
					damage *= 1f + hp.ChoiceDamage;
				if (hp.LifeOrbDamage != 0f)
					damage *= 1f + hp.LifeOrbDamage;
			}
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			MoveSpec move = CurrentMove(player);
			if (move == null)
				return;
			damage = (int)Math.Max(1, Math.Round(damage * move.DamageMultiplier));
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
			if (!hp.IsTransformed || hp.ChoiceLockUlt || Ultimate == null)
				return false;
			if (!hp.TryConsumeUltimate())
				return false;

			hp.LastMoveSlot = MoveSlot.Ultimate;
			hp.BeginUltEnergyLockout();
			var source = player.GetSource_ItemUse(Item);
			Vector2 velocity = Vector2.Zero;
			if (Ultimate.ShootSpeed > 0f)
			{
				velocity = HenshinPlayer.GetMouseWorld(player) - player.MountedCenter;
				if (velocity == Vector2.Zero)
					velocity = new Vector2(player.direction, 0f);
				velocity = Vector2.Normalize(velocity) * Ultimate.ShootSpeed;
			}

			int damage = player.GetWeaponDamage(Item);
			damage = (int)Math.Max(1, Math.Round(damage * Ultimate.DamageMultiplier));
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
				player.immune = true;
				player.immuneTime = Math.Max(player.immuneTime, 15 + hp.LungeIFrameBonus);
			}

			if (move.GrantsPhasing)
				hp.TryStartPhasing();

			if (move.ProjectileType <= ProjectileID.None)
				return;

			Vector2 spawn;
			if (move.SpawnAtMouse)
				spawn = HenshinPlayer.GetMouseWorld(player);
			else if (move.ShootSpeed <= 0f)
				spawn = player.MountedCenter;
			else
				spawn = position;

			Vector2 shootVel = velocity;
			if (move.SpawnAtMouse && move.ShootSpeed > 0f && shootVel == Vector2.Zero)
			{
				shootVel = HenshinPlayer.GetMouseWorld(player) - player.MountedCenter;
				if (shootVel == Vector2.Zero)
					shootVel = new Vector2(player.direction, 0f);
				shootVel = Vector2.Normalize(shootVel) * move.ShootSpeed;
			}

			MoveSlot slot = hp.LastMoveSlot;
			int id = Projectile.NewProjectile(source, spawn, shootVel, move.ProjectileType, damage, knockback, player.whoAmI, move.Ai0, move.Ai1, move.Ai2);
			// NewProjectile 的 position 是左上角；大 AoE 若不校正会偏到鼠标右下。
			if (id >= 0 && id < Main.maxProjectiles && move.SpawnAtMouse)
				Main.projectile[id].Center = HenshinPlayer.GetMouseWorld(player);
			if (id >= 0 && id < Main.maxProjectiles)
			{
				// OnSpawn 可能已按 LastMoveSlot 钉过；此处再显式钉一次，保证导演弹槽位正确（UltDamageBonus）。
				PokemonHenshin.Content.Accessories.HenshinAccGlobalProjectile.StampMoveOrigin(Main.projectile[id], slot);
			}
			if (id >= 0 && id < Main.maxProjectiles && Main.projectile[id].ModProjectile is IHenshinMoveProj tagged)
			{
				tagged.Delivery = move.Delivery;
				tagged.EasyCrit = move.EasyCrit;
				tagged.IgnoreDefensePartial = move.IgnoreDefensePartial;
				HenshinProjUtil.ApplyAccessoryHoming(tagged, hp.ShouldHoming(tagged.Delivery), hp.HomingTurn, hp.HomingRangeTiles);
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
			RefreshDamage();
			// 防御：剥前缀后仍可能残留的原版 Prefix* 行（专属效果由 ModPrefix.GetTooltipLines 提供）。
			tooltips.RemoveAll(static t =>
				t.Mod == "Terraria" && t.Name is "PrefixDamage" or "PrefixSpeed" or "PrefixCritChance"
					or "PrefixUseAnimation" or "PrefixShootSpeed" or "PrefixKnockback" or "PrefixSize"
					or "PrefixManaCost");

			int world = SafeWorldStage();
			int cap = HenshinStatService.LevelCap(world);
			int need = HenshinStatService.ExpNeeded(Level);
			bool capped = !Main.gameMenu && (Level >= cap || Level >= HenshinStatService.MaxLevel);

			tooltips.Add(new TooltipLine(Mod, "HenshinTransform", Language.GetTextValue("Mods.PokemonHenshin.Common.HoldToTransform")));
			tooltips.Add(new TooltipLine(Mod, "HenshinLevel", Language.GetTextValue(
				capped ? "Mods.PokemonHenshin.Common.ForceLevelCapped" : "Mods.PokemonHenshin.Common.ForceLevel",
				Level, cap, world))
			{
				OverrideColor = capped ? new Color(255, 200, 80) : new Color(180, 230, 160)
			});
			if (!capped && need > 0)
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinXp", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.ForceXp", Xp, need)));
			}

			int shownAtk = ComputeFinalAttack();
			int shownDef = ComputeFinalDefense();
			Player local = Main.LocalPlayer;
			HenshinPlayer hp = null;
			if (local != null && local.active)
			{
				hp = local.GetModPlayer<HenshinPlayer>();
				shownAtk = local.GetWeaponDamage(Item);
				if (hp.IsTransformed && hp.EvioliteDefMul > 0f
					&& FormRegistry.FindEvolutionOf(Definition?.FormId) != null)
					shownDef = (int)Math.Round(shownDef * (1f + hp.EvioliteDefMul));
			}

			tooltips.Add(new TooltipLine(Mod, "HenshinStats", Language.GetTextValue(
				"Mods.PokemonHenshin.Common.ForceStats", shownAtk, shownDef)));
			tooltips.Add(new TooltipLine(Mod, "HenshinDefenseNote", Language.GetTextValue("Mods.PokemonHenshin.Common.ForceDefenseNote"))
			{
				OverrideColor = new Color(200, 210, 230)
			});
			if (hp != null && hp.IsTransformed)
			{
				if (hp.MeleeDeliveryDamage > 0f)
				{
					tooltips.Add(new TooltipLine(Mod, "HenshinMeleeAcc", Language.GetTextValue(
						"Mods.PokemonHenshin.Common.ForceMeleeBonus", (int)Math.Round(hp.MeleeDeliveryDamage * 100f)))
					{
						OverrideColor = new Color(220, 200, 160)
					});
				}
				if (hp.BossDamageBonus > 0f)
				{
					tooltips.Add(new TooltipLine(Mod, "HenshinBossAcc", Language.GetTextValue(
						"Mods.PokemonHenshin.Common.ForceBossBonus", (int)Math.Round(hp.BossDamageBonus * 100f)))
					{
						OverrideColor = new Color(220, 200, 160)
					});
				}
			}

			if (Move1 != null && Move2 != null && Ultimate != null)
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinMoves", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.Moves3",
					Language.GetTextValue(Move1.NameKey),
					Language.GetTextValue(Move2.NameKey),
					Language.GetTextValue(Ultimate.NameKey))));
			}

			if (local != null && local.active)
			{
				hp ??= local.GetModPlayer<HenshinPlayer>();
				float max = Definition.EnergyMax;
				float cur = hp.GetStoredEnergy(Definition.FormId, max);
				int curI = (int)Math.Round(cur);
				int maxI = (int)Math.Round(max);
				bool ready = cur >= max - 0.01f;
				tooltips.Add(new TooltipLine(Mod, "HenshinEnergy", Language.GetTextValue(
					"Mods.PokemonHenshin.Common.UltimateEnergy", curI, maxI))
				{
					OverrideColor = ready ? new Color(255, 215, 80) : new Color(120, 180, 255)
				});
			}

			FormDefinition next = FormRegistry.FindEvolutionOf(Definition?.FormId);
			if (next != null)
			{
				int needLv = HenshinStatService.BandMinOf(next.Stage);
				bool readyEvo = HenshinStatService.MeetsEvolution(world, Level, next.Stage);
				string nextName = Language.GetTextValue(next.DisplayNameKey);
				tooltips.Add(new TooltipLine(Mod, "HenshinEvolve", Language.GetTextValue(
					readyEvo ? "Mods.PokemonHenshin.Common.ForceEvolveReady" : "Mods.PokemonHenshin.Common.ForceEvolveNeed",
					nextName, next.Stage, needLv))
				{
					OverrideColor = readyEvo ? new Color(120, 255, 160) : new Color(255, 180, 120)
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

		public static bool TryGet(Item item, out HenshinForceItem force)
		{
			force = item?.ModItem as HenshinForceItem;
			return force != null;
		}
	}

	public interface IHenshinMoveProj
	{
		bool EasyCrit { get; set; }
		bool Homing { get; set; }
		float HomingTurnRate { get; set; }
		float HomingRangeTiles { get; set; }
		int HomingTargetWhoAmI { get; set; }
		bool InherentHoming { get; set; }
		bool IgnoreDefensePartial { get; set; }
		MoveDelivery Delivery { get; set; }
		/// <summary>出弹槽；导演→子弹须显式复制，勿只靠 Global OnSpawn。供 UltDamageBonus 等使用。</summary>
		MoveSlot SourceMoveSlot { get; set; }
		bool HasSourceMoveSlot { get; set; }
	}
}
