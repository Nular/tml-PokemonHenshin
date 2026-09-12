using System;
using System.IO;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Evolution;
using PokemonHenshin.Content.Items.Consumables;
using PokemonHenshin.Content.PlayerState;
using PokemonHenshin.Content.TerrainEdit;
using PokemonHenshin.Content.WeatherField;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Net
{
	/// <summary>网络操作码（docs/engineering.md：显式枚举，一次锁死）。</summary>
	public enum NetOp : byte
	{
		SyncForm = 1,
		RequestEvolve = 2,
		ApplyEvolve = 3,
		SyncPhasing = 4,
		SyncWeatherField = 5,
		TerrainBudgetReject = 6,
		SyncEnergy = 7,
		RequestRareCandy = 8,
		ApplyForceProgress = 9,
		/// <summary>变身时同步主人鼠标世界坐标，供旁观端画鞭/束等指向效果。</summary>
		SyncAim = 10
	}

	/// <summary>单入口网络层。形态同步遵循服务端权威。</summary>
	public static class HenshinNet
	{
		private static ModPacket NewPacket(NetOp op)
		{
			ModPacket packet = PokemonHenshinMod.Instance.GetPacket();
			packet.Write((byte)op);
			return packet;
		}

		public static void SendForm(HenshinPlayer mp, int toWho, int fromWho)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;
			ModPacket packet = NewPacket(NetOp.SyncForm);
			packet.Write((byte)mp.Player.whoAmI);
			packet.Write(mp.CurrentFormNetId);
			packet.Send(toWho, fromWho);
		}

		public static void BroadcastForm(HenshinPlayer mp, int ignoreClient)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			SendForm(mp, -1, ignoreClient);
		}

		/// <summary>客户端请求进化：slot(-1=鼠标) + isMouse(byte)。</summary>
		public static void RequestEvolve(int slot, bool isMouse)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = NewPacket(NetOp.RequestEvolve);
				packet.Write((short)slot);
				packet.Write(isMouse);
				packet.Send();
				return;
			}

			// 单机：直接服务端路径。
			ApplyEvolveLocal(Main.LocalPlayer, slot, isMouse);
		}

		public static void SendPhasing(HenshinPlayer mp, int toWho, int fromWho)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;
			ModPacket packet = NewPacket(NetOp.SyncPhasing);
			packet.Write((byte)mp.Player.whoAmI);
			packet.Write(mp.IsPhasing);
			packet.Write(mp.PhasingTimeLeft);
			packet.Send(toWho, fromWho);
		}

		public static void BroadcastPhasing(HenshinPlayer mp, int ignoreClient = -1)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			SendPhasing(mp, -1, ignoreClient);
		}

		public static void SendEnergy(HenshinPlayer mp, int toWho = -1, int fromWho = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;
			ModPacket packet = NewPacket(NetOp.SyncEnergy);
			packet.Write((byte)mp.Player.whoAmI);
			packet.Write(mp.UltimateEnergy);
			int level = 0;
			int xp = 0;
			Item held = mp.Player.HeldItem;
			if (held?.ModItem is HenshinForceItem force)
			{
				level = force.Level;
				xp = force.Xp;
			}
			packet.Write(level);
			packet.Write(xp);
			packet.Send(toWho, fromWho);
		}

		public static void BroadcastWeatherField(WeatherFieldState field, bool remove)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			ModPacket packet = NewPacket(NetOp.SyncWeatherField);
			packet.Write(remove);
			packet.Write(field.Id);
			if (!remove)
			{
				packet.Write(field.Center.X);
				packet.Write(field.Center.Y);
				packet.Write(field.Radius);
				packet.Write(field.TimeLeft);
				packet.Write((byte)field.Tag);
				packet.Write((byte)field.Owner);
			}
			packet.Send();
		}

		public static void SendTerrainReject(int playerId)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			ModPacket packet = NewPacket(NetOp.TerrainBudgetReject);
			packet.Write((byte)playerId);
			packet.Send(playerId);
		}

		/// <summary>客户端请求对物品栏第一格之力使用神奇糖果。单机走本地。</summary>
		public static void RequestRareCandy()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NewPacket(NetOp.RequestRareCandy).Send();
				return;
			}

			if (!RareCandy.TryApply(Main.LocalPlayer, out _, out _, out int gained, out HenshinForceItem force))
				return;
			RareCandy.NotifyLocal(Main.LocalPlayer, force, gained);
		}

		/// <summary>
		/// 同步瞄准点。客户端只发给服务器；服务器广播（<paramref name="fromWho"/> 为忽略端）。
		/// Listen 主机的本地玩家由调用方直接广播，不经客户端包。
		/// </summary>
		public static void SendAim(HenshinPlayer mp, int toWho = -1, int fromWho = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			Vector2 mouse = mp.SerializeAimWorld();
			ModPacket packet = NewPacket(NetOp.SyncAim);
			packet.Write((byte)mp.Player.whoAmI);
			packet.Write(mouse.X);
			packet.Write(mouse.Y);

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				packet.Send();
				return;
			}

			packet.Send(toWho, fromWho);
		}

		public static void SendForceProgress(int playerId, int slot, int level, int xp, int levelsGained)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			ModPacket packet = NewPacket(NetOp.ApplyForceProgress);
			packet.Write((byte)playerId);
			packet.Write((short)slot);
			packet.Write(level);
			packet.Write(xp);
			packet.Write((byte)System.Math.Clamp(levelsGained, 0, 255));
			packet.Send(playerId);
		}

		public static void Handle(BinaryReader reader, int whoAmI)
		{
			NetOp op = (NetOp)reader.ReadByte();
			switch (op)
			{
				case NetOp.SyncForm:
					HandleSyncForm(reader, whoAmI);
					break;
				case NetOp.RequestEvolve:
					HandleRequestEvolve(reader, whoAmI);
					break;
				case NetOp.ApplyEvolve:
					HandleApplyEvolve(reader, whoAmI);
					break;
				case NetOp.SyncPhasing:
					HandleSyncPhasing(reader, whoAmI);
					break;
				case NetOp.SyncWeatherField:
					HandleSyncWeatherField(reader, whoAmI);
					break;
				case NetOp.TerrainBudgetReject:
					HandleTerrainReject(reader, whoAmI);
					break;
				case NetOp.SyncEnergy:
					HandleSyncEnergy(reader, whoAmI);
					break;
				case NetOp.RequestRareCandy:
					HandleRequestRareCandy(whoAmI);
					break;
				case NetOp.ApplyForceProgress:
					HandleApplyForceProgress(reader, whoAmI);
					break;
				case NetOp.SyncAim:
					HandleSyncAim(reader, whoAmI);
					break;
				default:
					PokemonHenshinMod.Instance.Logger.Warn($"未知 NetOp {(byte)op}，来自 {whoAmI}");
					break;
			}
		}

		private static void HandleSyncForm(BinaryReader reader, int whoAmI)
		{
			int playerIndex = reader.ReadByte();
			ushort claimedNetId = reader.ReadUInt16();

			if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
				return;
			Player player = Main.player[playerIndex];
			if (player == null || !player.active)
				return;
			HenshinPlayer mp = player.GetModPlayer<HenshinPlayer>();

			if (Main.netMode == NetmodeID.Server)
			{
				if (playerIndex != whoAmI)
					return;

				ushort authoritative = mp.ResolveHeldForm()?.NetworkId ?? 0;
				if (authoritative == claimedNetId)
				{
					ModPacket relay = NewPacket(NetOp.SyncForm);
					relay.Write((byte)playerIndex);
					relay.Write(authoritative);
					relay.Send(-1, whoAmI);
				}
				else
				{
					ModPacket correction = NewPacket(NetOp.SyncForm);
					correction.Write((byte)playerIndex);
					correction.Write(authoritative);
					correction.Send(whoAmI, -1);
				}
				return;
			}

			mp.ApplyServerForm(claimedNetId);
		}

		private static void HandleRequestEvolve(BinaryReader reader, int whoAmI)
		{
			short slot = reader.ReadInt16();
			bool isMouse = reader.ReadBoolean();

			if (Main.netMode != NetmodeID.Server)
				return;

			Player player = Main.player[whoAmI];
			if (player == null || !player.active)
				return;

			if (!ApplyEvolveLocal(player, slot, isMouse))
				return;

			// 广播结果给所有客户端（含发起者）。
			ModPacket packet = NewPacket(NetOp.ApplyEvolve);
			packet.Write((byte)whoAmI);
			packet.Write(slot);
			packet.Write(isMouse);
			Item item = EvolutionService.GetItemRef(player, slot, isMouse);
			packet.Write(item.type);
			packet.Write((byte)item.prefix);
			packet.Write(item.favorited);
			int level = 1;
			int xp = 0;
			if (item.ModItem is HenshinForceItem force)
			{
				level = force.Level;
				xp = force.Xp;
			}
			packet.Write(level);
			packet.Write(xp);
			packet.Send();
		}

		private static void HandleApplyEvolve(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			int playerIndex = reader.ReadByte();
			short slot = reader.ReadInt16();
			bool isMouse = reader.ReadBoolean();
			int itemType = reader.ReadInt32();
			byte prefix = reader.ReadByte();
			bool favorited = reader.ReadBoolean();
			int level = reader.ReadInt32();
			int xp = reader.ReadInt32();

			Player player = Main.player[playerIndex];
			if (player == null || !player.active)
				return;

			Item item = EvolutionService.GetItemRef(player, slot, isMouse);
			if (item == null)
				return;

			item.SetDefaults(itemType);
			if (prefix > 0)
				item.Prefix(prefix);
			item.favorited = favorited;
			if (item.ModItem is HenshinForceItem force)
				force.SetProgress(level, xp);

			if (playerIndex == Main.myPlayer)
				player.GetModPlayer<EvolutionOfferPlayer>().NotifyEvolved();
		}

		internal static bool ApplyEvolveLocal(Player player, int slot, bool isMouse)
		{
			if (!EvolutionService.CanAutoEvolveLocation(player, slot, isMouse))
				return false;

			Item item = EvolutionService.GetItemRef(player, slot, isMouse);
			if (item == null || item.IsAir)
				return false;

			FormDefinition current = FormRegistry.ByItemType(item.type);
			if (!EvolutionService.MeetsTrigger(player, current, item))
				return false;

			FormDefinition next = EvolutionService.GetNextForm(current);
			if (next == null || next.ItemType <= 0)
				return false;

			if (!EvolutionService.TryReplace(item, next.ItemType))
				return false;

			if (player.whoAmI == Main.myPlayer)
				player.GetModPlayer<EvolutionOfferPlayer>()?.NotifyEvolved();

			return true;
		}

		private static void HandleSyncPhasing(BinaryReader reader, int whoAmI)
		{
			int playerIndex = reader.ReadByte();
			bool phasing = reader.ReadBoolean();
			int timeLeft = reader.ReadInt32();

			if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
				return;
			Player player = Main.player[playerIndex];
			if (player == null || !player.active)
				return;

			if (Main.netMode == NetmodeID.Server)
			{
				if (playerIndex != whoAmI)
					return;
				// 服务端权威由 HenshinPlayer 自己维护；此处仅转发广播由 BroadcastPhasing 负责。
				return;
			}

			player.GetModPlayer<HenshinPlayer>().ApplyServerPhasing(phasing, timeLeft);
		}

		private static void HandleSyncWeatherField(BinaryReader reader, int whoAmI)
		{
			bool remove = reader.ReadBoolean();
			int id = reader.ReadInt32();
			if (remove)
			{
				WeatherFieldSystem.ApplyRemoteRemove(id);
				return;
			}

			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			float radius = reader.ReadSingle();
			int timeLeft = reader.ReadInt32();
			WeatherTag tag = (WeatherTag)reader.ReadByte();
			byte owner = reader.ReadByte();
			WeatherFieldSystem.ApplyRemoteUpsert(id, new Microsoft.Xna.Framework.Vector2(x, y), radius, timeLeft, tag, owner);
		}

		private static void HandleTerrainReject(BinaryReader reader, int whoAmI)
		{
			int playerIndex = reader.ReadByte();
			if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex == Main.myPlayer)
				TerrainBudgetPlayer.NotifyRejected();
		}

		private static void HandleSyncEnergy(BinaryReader reader, int whoAmI)
		{
			int playerIndex = reader.ReadByte();
			float energy = reader.ReadSingle();
			int level = reader.ReadInt32();
			int xp = reader.ReadInt32();
			if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
				return;
			Player player = Main.player[playerIndex];
			if (player == null || !player.active)
				return;

			HenshinPlayer mp = player.GetModPlayer<HenshinPlayer>();
			if (Main.netMode == NetmodeID.Server)
			{
				if (playerIndex != whoAmI)
					return;
				mp.ApplyServerEnergy(energy);
				if (level >= HenshinStatService.MinLevel && player.HeldItem?.ModItem is HenshinForceItem owned)
				{
					int lv = Math.Max(owned.Level, level);
					int x = lv > owned.Level ? xp : Math.Max(owned.Xp, xp);
					owned.SetProgress(lv, x);
				}
				SendEnergy(mp, -1, whoAmI);
				return;
			}

			mp.ApplyServerEnergy(energy);
			if (player.whoAmI != Main.myPlayer && player.HeldItem?.ModItem is HenshinForceItem remoteForce)
				remoteForce.SetProgress(level, xp);
		}

		private static void HandleRequestRareCandy(int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;
			Player player = Main.player[whoAmI];
			if (player == null || !player.active)
				return;
			if (!RareCandy.TryApply(player, out int level, out int xp, out int gained, out _))
				return;
			SendForceProgress(whoAmI, RareCandy.TargetSlot, level, xp, gained);
		}

		private static void HandleSyncAim(BinaryReader reader, int whoAmI)
		{
			int playerIndex = reader.ReadByte();
			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
				return;
			Player player = Main.player[playerIndex];
			if (player == null || !player.active)
				return;
			if (!float.IsFinite(x) || !float.IsFinite(y))
				return;

			x = MathHelper.Clamp(x, 0f, Main.maxTilesX * 16f);
			y = MathHelper.Clamp(y, 0f, Main.maxTilesY * 16f);
			Vector2 mouse = new(x, y);

			if (Main.netMode == NetmodeID.Server)
			{
				if (playerIndex != whoAmI)
					return;
				player.GetModPlayer<HenshinPlayer>().ApplyRemoteAim(mouse);
				ModPacket relay = NewPacket(NetOp.SyncAim);
				relay.Write((byte)playerIndex);
				relay.Write(x);
				relay.Write(y);
				relay.Send(-1, whoAmI);
				return;
			}

			if (playerIndex == Main.myPlayer)
				return;
			player.GetModPlayer<HenshinPlayer>().ApplyRemoteAim(mouse);
		}

		private static void HandleApplyForceProgress(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			int playerIndex = reader.ReadByte();
			short slot = reader.ReadInt16();
			int level = reader.ReadInt32();
			int xp = reader.ReadInt32();
			int gained = reader.ReadByte();
			if (playerIndex != Main.myPlayer)
				return;
			if (slot < 0 || slot >= Main.LocalPlayer.inventory.Length)
				return;

			Item item = Main.LocalPlayer.inventory[slot];
			if (item?.ModItem is not HenshinForceItem force)
				return;
			force.SetProgress(level, xp);
			RareCandy.NotifyLocal(Main.LocalPlayer, force, gained);
		}
	}
}
