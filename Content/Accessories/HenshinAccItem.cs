using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Accessories
{
	[Autoload(false)]
	public sealed class HenshinAccItem : ModItem
	{
		public AccFamilyId FamilyId { get; private set; }
		public AccPiece Piece { get; private set; }

		private AccFamilyDef Def => HenshinAccCatalog.Get(FamilyId);

		public HenshinAccItem() { }

		public HenshinAccItem(AccFamilyId family, AccPiece piece)
		{
			FamilyId = family;
			Piece = piece;
		}

		public override string Name => Def?.ItemName(Piece) ?? "HenshinAccItem";

		public override string Texture
		{
			get
			{
				AccFamilyDef def = Def;
				if (def == null)
					return "PokemonHenshin/Assets/Accessories/A01";
				if (Piece == AccPiece.Super)
					return def.SuperTexturePath;
				if (Piece != AccPiece.Normal)
					return def.ShardTexturePath;
				return def.TexturePath;
			}
		}

		protected override bool CloneNewInstances => true;

		public override ModItem Clone(Item newEntity)
		{
			HenshinAccItem clone = (HenshinAccItem)base.Clone(newEntity);
			clone.FamilyId = FamilyId;
			clone.Piece = Piece;
			return clone;
		}

		public override void SetStaticDefaults()
		{
			AccFamilyDef def = Def;
			if (def == null)
				return;
			_ = this.GetLocalization(nameof(DisplayName), () => BuildDisplayName(def, Piece));
			_ = this.GetLocalization(nameof(Tooltip), () => BuildTooltip(def, Piece));
			HenshinAccLoader.RegisterType(FamilyId, Piece, Type);
		}

		/// <summary>52poke 袋内图约 140×152，地上按原像素绘制会过大；世界绘制与碰撞盒均为现役一半。</summary>
		private const float WorldSpriteScale = 0.5f;

		public override void SetDefaults()
		{
			AccFamilyDef def = Def;
			Item.width = 14;
			Item.height = 14;
			Item.accessory = true;
			Item.rare = Piece == AccPiece.Super
				? (def?.SuperRarity ?? ItemRarityID.Yellow)
				: Piece == AccPiece.Normal
					? (def?.NormalRarity ?? ItemRarityID.Orange)
					: ItemRarityID.Blue;
			Item.value = Piece == AccPiece.Super
				? Item.sellPrice(gold: 8)
				: Piece == AccPiece.Normal
					? Item.sellPrice(gold: 2)
					: Item.sellPrice(silver: 50);
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			AccFamilyDef def = Def;
			if (def == null)
				return;

			bool transformed = hp.IsTransformed;
			if (!transformed && !def.WorksUntransformed)
				return;

			if (transformed)
				hp.AccActive = true;

			if (def.Resonance != PokemonType.None)
			{
				if (hp.CurrentForm == null)
					return;
				if (hp.CurrentForm.Primary != def.Resonance && hp.CurrentForm.Secondary != def.Resonance)
					return;
			}

			foreach (AccStatLine line in def.Stats(Piece))
				hp.ApplyAccStat(line);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			Player player = Main.LocalPlayer;
			HenshinPlayer hp = player?.GetModPlayer<HenshinPlayer>();
			AccFamilyDef def = Def;
			HenshinAccStatTooltip.AddLines(tooltips, Mod, def, Piece);
			bool transformed = hp != null && hp.IsTransformed;
			bool resOk = def == null || def.Resonance == PokemonType.None
				|| (hp?.CurrentForm != null && (hp.CurrentForm.Primary == def.Resonance || hp.CurrentForm.Secondary == def.Resonance));
			bool everstone = def is { WorksUntransformed: true };
			bool active = everstone
				? resOk
				: transformed && resOk;

			string key = everstone
				? (active ? "Mods.PokemonHenshin.Accessories.AlwaysActiveTag" : "Mods.PokemonHenshin.Accessories.AlwaysInactiveTag")
				: (active ? "Mods.PokemonHenshin.Accessories.ActiveTag" : "Mods.PokemonHenshin.Accessories.InactiveTag");
			tooltips.Add(new TooltipLine(Mod, "HenshinAccGate", Language.GetTextValue(key))
			{
				OverrideColor = active ? Color.LightGreen : Color.OrangeRed
			});

			if (Piece is >= AccPiece.S1 and <= AccPiece.S6)
			{
				tooltips.Add(new TooltipLine(Mod, "HenshinAccShard", Language.GetTextValue("Mods.PokemonHenshin.Accessories.ShardTag", (int)Piece))
				{
					OverrideColor = Color.SkyBlue
				});
			}
		}

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			scale *= WorldSpriteScale;
			return true;
		}

		public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			if (Piece is < AccPiece.S1 or > AccPiece.S6)
				return;
			string mark = ((int)Piece).ToString();
			Vector2 pos = position + new Vector2(frame.Width * scale * 0.55f, frame.Height * scale * 0.55f);
			Utils.DrawBorderString(spriteBatch, mark, pos, Color.White, 0.7f);
		}

		public override void AddRecipes()
		{
			AccFamilyDef def = Def;
			if (def == null)
				return;

			if (Piece is >= AccPiece.S1 and <= AccPiece.S6)
			{
				AccLootSpec loot = def.LootFor(Piece);
				if (loot is { Kind: AccLootKind.Craft, Craft: not null })
				{
					Recipe r = Recipe.Create(Type);
					foreach ((int itemId, int stack) in loot.Craft)
						r.AddIngredient(itemId, stack);
					r.AddTile(loot.CraftTile > 0 ? loot.CraftTile : TileID.WorkBenches);
					r.Register();
				}
				return;
			}

			if (Piece != AccPiece.Normal && Piece != AccPiece.Super)
				return;

			int s1 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S1);
			int s2 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S2);
			int s3 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S3);
			int s4 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S4);
			int s5 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S5);
			int s6 = HenshinAccLoader.ItemType(FamilyId, AccPiece.S6);
			int normal = HenshinAccLoader.ItemType(FamilyId, AccPiece.Normal);
			if (s1 <= 0 || s2 <= 0 || s3 <= 0 || s4 <= 0)
				return;

			int tile = def.SuperMinStage >= 10
				? TileID.LunarCraftingStation
				: def.SuperMinStage >= 7
					? TileID.MythrilAnvil
					: TileID.TinkerersWorkbench;

			if (Piece == AccPiece.Normal)
			{
				Recipe.Create(Type)
					.AddIngredient(s1)
					.AddIngredient(s2)
					.AddIngredient(s3)
					.AddIngredient(s4)
					.AddTile(TileID.TinkerersWorkbench)
					.Register();
			}
			else
			{
				if (normal > 0 && s5 > 0 && s6 > 0)
				{
					Recipe.Create(Type)
						.AddIngredient(normal)
						.AddIngredient(s5)
						.AddIngredient(s6)
						.AddTile(tile)
						.Register();
				}
				if (s5 > 0 && s6 > 0)
				{
					Recipe.Create(Type)
						.AddIngredient(s1)
						.AddIngredient(s2)
						.AddIngredient(s3)
						.AddIngredient(s4)
						.AddIngredient(s5)
						.AddIngredient(s6)
						.AddTile(tile)
						.Register();
				}
			}
		}

		private static string BuildDisplayName(AccFamilyDef def, AccPiece piece)
		{
			string baseZh = def.OfficialZh;
			return piece switch
			{
				AccPiece.Normal => baseZh,
				AccPiece.Super => "超级" + baseZh,
				AccPiece.S1 => baseZh + "碎片一",
				AccPiece.S2 => baseZh + "碎片二",
				AccPiece.S3 => baseZh + "碎片三",
				AccPiece.S4 => baseZh + "碎片四",
				AccPiece.S5 => baseZh + "碎片五",
				AccPiece.S6 => baseZh + "碎片六",
				_ => baseZh
			};
		}

		private static string BuildTooltip(AccFamilyDef def, AccPiece piece)
			=> def.OfficialZh;
	}

	public static class HenshinAccLoader
	{
		private static readonly Dictionary<(AccFamilyId, AccPiece), int> Types = new();

		public static void RegisterAll(Mod mod)
		{
			foreach (AccFamilyDef def in HenshinAccCatalog.All)
			{
				foreach (AccPiece piece in Pieces)
					mod.AddContent(new HenshinAccItem(def.Id, piece));
			}
		}

		public static void RegisterType(AccFamilyId family, AccPiece piece, int type)
			=> Types[(family, piece)] = type;

		public static int ItemType(AccFamilyId family, AccPiece piece)
			=> Types.TryGetValue((family, piece), out int t) ? t : 0;

		public static IEnumerable<AccPiece> Pieces
		{
			get
			{
				yield return AccPiece.S1;
				yield return AccPiece.S2;
				yield return AccPiece.S3;
				yield return AccPiece.S4;
				yield return AccPiece.S5;
				yield return AccPiece.S6;
				yield return AccPiece.Normal;
				yield return AccPiece.Super;
			}
		}
	}
}
