using System.IO;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Net
{
	/// <summary>网络操作码（dev-plan §2.4：显式枚举，不自动编号）。后续 SyncPhasing / SyncWeatherField / Evolve 依次追加。</summary>
	public enum NetOp : byte
	{
		/// <summary>载荷：playerId(byte) + formNetId(ushort)。</summary>
		SyncForm = 1
	}

	/// <summary>
	/// 单入口网络层。形态同步遵循服务端权威（需求 §2.1 / §8）：
	/// 客户端本地变身后上报 → 服务端按自己视角的持握状态校验 → 广播权威值；
	/// 客户端收到的值只应用于远端玩家，本地玩家保留预测。
	/// </summary>
	public static class HenshinNet
	{
		private static ModPacket NewPacket(NetOp op)
		{
			ModPacket packet = PokemonHenshinMod.Instance.GetPacket();
			packet.Write((byte)op);
			return packet;
		}

		/// <summary>发送某玩家当前形态。客户端调用即发给服务端；服务端调用按 toWho / fromWho 分发。</summary>
		public static void SendForm(HenshinPlayer mp, int toWho, int fromWho)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;
			ModPacket packet = NewPacket(NetOp.SyncForm);
			packet.Write((byte)mp.Player.whoAmI);
			packet.Write(mp.CurrentFormNetId);
			packet.Send(toWho, fromWho);
		}

		/// <summary>服务端向全体广播某玩家的权威形态。</summary>
		public static void BroadcastForm(HenshinPlayer mp, int ignoreClient)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			SendForm(mp, -1, ignoreClient);
		}

		public static void Handle(BinaryReader reader, int whoAmI)
		{
			NetOp op = (NetOp)reader.ReadByte();
			switch (op)
			{
				case NetOp.SyncForm:
					HandleSyncForm(reader, whoAmI);
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
				// 基础校验：只能上报自己。
				if (playerIndex != whoAmI)
					return;

				// 服务端权威：按自己视角判定，不信客户端声称的值。
				ushort authoritative = mp.ResolveHeldForm()?.NetworkId ?? 0;
				if (authoritative == claimedNetId)
				{
					// 一致：转发给其他客户端（本地状态机同 tick 也会得出相同结果）。
					ModPacket relay = NewPacket(NetOp.SyncForm);
					relay.Write((byte)playerIndex);
					relay.Write(authoritative);
					relay.Send(-1, whoAmI);
				}
				else
				{
					// 不一致：把权威值发回申报者纠正；其他人由服务端状态机变化时广播。
					ModPacket correction = NewPacket(NetOp.SyncForm);
					correction.Write((byte)playerIndex);
					correction.Write(authoritative);
					correction.Send(whoAmI, -1);
				}
				return;
			}

			// 客户端：应用服务端权威值（仅影响远端玩家；本地玩家保留预测）。
			mp.ApplyServerForm(claimedNetId);
		}
	}
}
