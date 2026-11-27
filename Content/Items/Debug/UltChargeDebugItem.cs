using Microsoft.Xna.Framework;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Debug
{
	/// <summary>Debug：左键切换「无限大招」。只影响使用者本人，不存档。</summary>
	public sealed class UltChargeDebugItem : ModItem
	{
		public override string Texture => "PokemonHenshin/Assets/Items/RareCandy";

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 20;
			Item.useTime = 20;
			Item.UseSound = SoundID.Item4;
			Item.rare = ItemRarityID.Red;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;
			UltChargeDebugPlayer mp = player.GetModPlayer<UltChargeDebugPlayer>();
			mp.Infinite = !mp.Infinite;
			string key = mp.Infinite ? "Mods.PokemonHenshin.UltChargeDebug.On" : "Mods.PokemonHenshin.UltChargeDebug.Off";
			string text = Language.GetTextValue(key);
			CombatText.NewText(player.getRect(), mp.Infinite ? Color.Gold : Color.LightGray, text);
			Main.NewText(text, mp.Infinite ? Color.Gold : Color.LightGray);
			return true;
		}
	}

	/// <summary>无限大招状态（本机，不存档）。能量同步走 HenshinPlayer.SendClientChanges。</summary>
	public sealed class UltChargeDebugPlayer : ModPlayer
	{
		public bool Infinite;

		public override void PostUpdate()
		{
			if (!Infinite || Player.whoAmI != Main.myPlayer)
				return;
			Player.GetModPlayer<HenshinPlayer>().DebugFillUltimateEnergy();
		}
	}
}
