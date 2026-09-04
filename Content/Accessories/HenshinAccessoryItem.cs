using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Accessories
{
	/// <summary>仅变身生效的饰品基类（需求 §6）。</summary>
	public abstract class HenshinAccessoryItem : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.accessory = true;
			Item.rare = Terraria.ID.ItemRarityID.Orange;
			Item.value = Item.sellPrice(gold: 1);
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (!hp.IsTransformed)
				return;
			hp.AccActive = true;
			if (!ResonanceOk(hp))
				return;
			ApplyHenshinEffect(hp);
		}

		/// <summary>共鸣条件；通用饰品默认 true。</summary>
		protected virtual bool ResonanceOk(HenshinPlayer hp) => true;

		protected abstract void ApplyHenshinEffect(HenshinPlayer hp);

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			Player player = Main.LocalPlayer;
			HenshinPlayer hp = player?.GetModPlayer<HenshinPlayer>();
			bool active = hp != null && hp.IsTransformed && ResonanceOk(hp);
			string key = active
				? "Mods.PokemonHenshin.Accessories.ActiveTag"
				: "Mods.PokemonHenshin.Accessories.InactiveTag";
			tooltips.Add(new TooltipLine(Mod, "HenshinAccGate", Language.GetTextValue(key))
			{
				OverrideColor = active ? Microsoft.Xna.Framework.Color.LightGreen : Microsoft.Xna.Framework.Color.OrangeRed
			});
		}
	}

	public abstract class TypeResonanceAccessory : HenshinAccessoryItem
	{
		protected abstract PokemonType RequiredType { get; }

		protected override bool ResonanceOk(HenshinPlayer hp)
		{
			if (hp.CurrentForm == null)
				return false;
			return hp.CurrentForm.Primary == RequiredType || hp.CurrentForm.Secondary == RequiredType;
		}
	}
}
