using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.Visual;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.PlayerState
{
	/// <summary>
	/// 变身状态机——本模唯一的形态权威状态（dev-plan §4.2）。
	/// <para>
	/// 判定规则（需求 §2.1）：存活 + 热键栏（inventory[0..9]）当前选中物品为本模「之力」。
	/// 服务端 / 单机按持握判定；客户端对本地玩家做预测，对远端玩家只信 <see cref="HenshinNet"/> 下发的形态。
	/// </para>
	/// <para>清理契约：<see cref="ExitForm"/> 只碰本模字段（后续里程碑加入的被动 / Buff 白名单也只在这里清）。</para>
	/// </summary>
	public sealed class HenshinPlayer : ModPlayer
	{
		public const int HotbarSize = 10;

		/// <summary>当前形态；null = 未变身。</summary>
		public FormDefinition CurrentForm { get; private set; }

		public bool IsTransformed => CurrentForm != null;
		public ushort CurrentFormNetId => CurrentForm?.NetworkId ?? 0;

		/// <summary>变身持续 tick 数，供 Overlay 动画使用。</summary>
		public int TransformTicks { get; private set; }

		/// <summary>服务端下发的形态（仅对远端玩家生效）。</summary>
		private ushort serverFormNetId;

		/// <summary>上一 tick 的形态，用于 SendClientChanges 判脏。</summary>
		private ushort lastSyncedNetId;

		private bool IsLocalClientPlayer => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;
		private bool IsRemotePlayerOnClient => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer;

		#region 判定

		/// <summary>按持握规则解析应处于的形态；不满足返回 null。</summary>
		public FormDefinition ResolveHeldForm()
		{
			if (!Player.active || Player.dead || Player.ghost)
				return null;
			int slot = Player.selectedItem;
			if (slot < 0 || slot >= HotbarSize)
				return null;
			Item held = Player.inventory[slot];
			if (held == null || held.IsAir)
				return null;
			return FormRegistry.ByItemType(held.type);
		}

		private FormDefinition ResolveDesiredForm()
		{
			if (IsRemotePlayerOnClient)
				return FormRegistry.ByNetworkId(serverFormNetId);
			return ResolveHeldForm();
		}

		#endregion

		#region 状态迁移

		private void EnterForm(FormDefinition form)
		{
			CurrentForm = form;
			TransformTicks = 0;
			EnforceNoMount();
		}

		/// <summary>退出变身：只清本模字段；同 tick 内 Overlay 即不可见。</summary>
		private void ExitForm()
		{
			CurrentForm = null;
			TransformTicks = 0;
		}

		private void ReconcileForm()
		{
			FormDefinition desired = ResolveDesiredForm();
			if (ReferenceEquals(desired, CurrentForm))
				return;

			// 切换另一种之力：先完整退出，再进入（需求 §12.2）。
			if (IsTransformed)
				ExitForm();
			if (desired != null)
				EnterForm(desired);

			if (Main.netMode == NetmodeID.Server)
				HenshinNet.BroadcastForm(this, ignoreClient: -1);
		}

		/// <summary>由网络层调用：应用服务端权威形态。</summary>
		internal void ApplyServerForm(ushort netId)
		{
			serverFormNetId = netId;
			if (IsRemotePlayerOnClient)
				ReconcileForm();
		}

		#endregion

		#region 每 tick

		public override void PreUpdate()
		{
			ReconcileForm();
			if (IsTransformed)
				TransformTicks++;
		}

		public override void UpdateDead()
		{
			// 死亡：立刻解除（复活后需重新持握）。
			if (IsTransformed)
			{
				ExitForm();
				if (Main.netMode == NetmodeID.Server)
					HenshinNet.BroadcastForm(this, ignoreClient: -1);
			}
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			if (IsTransformed)
				ExitForm();
		}

		#endregion

		#region 禁坐骑（需求 §2.2）

		public override void SetControls()
		{
			if (IsTransformed)
				Player.controlMount = false;
		}

		public override void PreUpdateMovement()
		{
			if (IsTransformed)
				EnforceNoMount();
		}

		public override void PostUpdateEquips()
		{
			if (IsTransformed)
				EnforceNoMount();
		}

		private void EnforceNoMount()
		{
			Player.controlMount = false;
			if (!Player.mount.Active)
				return;
			// 只由本地或服务端执行下马，避免客户端替远端玩家改状态造成分歧。
			if (Main.netMode == NetmodeID.Server || Player.whoAmI == Main.myPlayer)
				Player.mount.Dismount(Player);
		}

		#endregion

		#region 绘制

		public override void HideDrawLayers(PlayerDrawSet drawInfo)
		{
			if (!IsTransformed)
				return;
			HenshinOverlayLayer overlay = ModContent.GetInstance<HenshinOverlayLayer>();
			foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
			{
				if (!ReferenceEquals(layer, overlay))
					layer.Hide();
			}
		}

		#endregion

		#region 联机

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
		{
			HenshinNet.SendForm(this, toWho, fromWho);
		}

		public override void CopyClientState(ModPlayer targetCopy)
		{
			((HenshinPlayer)targetCopy).lastSyncedNetId = CurrentFormNetId;
		}

		public override void SendClientChanges(ModPlayer clientPlayer)
		{
			if (((HenshinPlayer)clientPlayer).lastSyncedNetId != CurrentFormNetId)
				HenshinNet.SendForm(this, -1, Player.whoAmI);
		}

		#endregion
	}
}
