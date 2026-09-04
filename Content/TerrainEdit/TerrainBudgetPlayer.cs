using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.TerrainEdit
{
	/// <summary>地形编辑预算（需求 §7.2）。</summary>
	public sealed class TerrainBudgetPlayer : ModPlayer
	{
		private int tilesThisSecond;
		private int tilesThisMinute;
		private int secondTimer;
		private int minuteTimer;

		public override void ResetEffects()
		{
			// 计数器在 PostUpdate 衰减
		}

		public override void PostUpdate()
		{
			secondTimer++;
			minuteTimer++;
			if (secondTimer >= 60)
			{
				secondTimer = 0;
				tilesThisSecond = 0;
			}
			if (minuteTimer >= 3600)
			{
				minuteTimer = 0;
				tilesThisMinute = 0;
			}
		}

		public bool TryMineTile(int x, int y)
		{
			HenshinPlayer hp = Player.GetModPlayer<HenshinPlayer>();
			if (hp.IsPhasing)
				return Reject();

			if (IsBlacklisted(x, y))
				return Reject();

			int stage = ProgressStageService.GetProgressStage();
			GetLimits(stage, out int radiusTiles, out int perSec, out int perMin);

			int px = (int)(Player.Center.X / 16f);
			int py = (int)(Player.Center.Y / 16f);
			if (System.Math.Abs(x - px) > radiusTiles || System.Math.Abs(y - py) > radiusTiles)
				return Reject();

			if (tilesThisSecond >= perSec || tilesThisMinute >= perMin)
				return Reject();

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				// 客户端预测：真正挖砖仍由服务端权威路径执行。
			}

			if (Main.tile[x, y] == null || !Main.tile[x, y].HasTile)
				return false;

			WorldGen.KillTile(x, y);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, x, y);

			tilesThisSecond++;
			tilesThisMinute++;
			return true;
		}

		private bool Reject()
		{
			if (Main.netMode == NetmodeID.Server)
				HenshinNet.SendTerrainReject(Player.whoAmI);
			else if (Player.whoAmI == Main.myPlayer)
				NotifyRejected();
			return false;
		}

		public static void NotifyRejected()
		{
			Main.NewText(Language.GetTextValue("Mods.PokemonHenshin.Terrain.BudgetReject"), 255, 180, 80);
		}

		private static void GetLimits(int stage, out int radius, out int perSec, out int perMin)
		{
			if (stage <= 6)
			{
				radius = 3;
				perSec = 8;
				perMin = 60;
			}
			else if (stage <= 9)
			{
				radius = 6;
				perSec = 15;
				perMin = 120;
			}
			else
			{
				radius = 12;
				perSec = 30;
				perMin = 300;
			}
		}

		private static bool IsBlacklisted(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			if (tile == null || !tile.HasTile)
				return false;

			ushort type = tile.TileType;
			if (TileID.Sets.BasicChest[type] || TileID.Sets.BasicChestFake[type])
				return true;
			if (type == TileID.DemonAltar || type == TileID.Containers || type == TileID.Containers2)
				return true;
			if (type == TileID.Heart || type == TileID.LifeFruit || type == TileID.Beds)
				return true;
			if (TileEntity.ByPosition.ContainsKey(new(x, y)))
				return true;
			return false;
		}
	}
}
