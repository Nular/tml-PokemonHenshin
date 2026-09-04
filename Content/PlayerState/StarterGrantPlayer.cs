using PokemonHenshin.Content.Items.Forms;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PokemonHenshin.Content.PlayerState
{
	/// <summary>开局发放御三家各 1（每玩家每世界一次）。满包则掉落脚下。</summary>
	public sealed class StarterGrantPlayer : ModPlayer
	{
		private bool granted;

		public override void SaveData(TagCompound tag)
		{
			if (granted)
				tag["starterGranted"] = true;
		}

		public override void LoadData(TagCompound tag)
		{
			granted = tag.ContainsKey("starterGranted") && tag.GetBool("starterGranted");
		}

		public override void OnEnterWorld()
		{
			if (granted)
				return;
			// 联机：仅服务端发放；单机直接发。
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Give(ModContent.ItemType<CharmanderForce>());
			Give(ModContent.ItemType<SquirtleForce>());
			Give(ModContent.ItemType<BulbasaurForce>());
			granted = true;
		}

		private void Give(int type)
		{
			Player.QuickSpawnItem(Player.GetSource_GiftOrReward("PokemonHenshinStarter"), type, 1);
		}
	}
}
