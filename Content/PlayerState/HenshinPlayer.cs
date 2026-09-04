using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Affinity;
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
	/// </summary>
	public sealed class HenshinPlayer : ModPlayer
	{
		public const int HotbarSize = 10;
		public const int PhasingMaxTicks = 120; // 2.0s
		public const int PhasingBaseTicks = 90; // 1.5s
		public const int PhasingCooldownTicks = 480; // 8.0s
		public const int BossEngageLockTicks = 180; // 3s

		public FormDefinition CurrentForm { get; private set; }

		public bool IsTransformed => CurrentForm != null;
		public ushort CurrentFormNetId => CurrentForm?.NetworkId ?? 0;
		public int TransformTicks { get; private set; }

		// —— 穿障 ——
		public bool IsPhasing { get; private set; }
		public int PhasingTimeLeft { get; private set; }
		public int PhasingCooldown { get; private set; }
		public int BossEngageTimer { get; private set; }
		public float PhasingBonusTicks { get; set; } // 饰品加成，封顶后改 CD
		public float PhasingCooldownMultiplier { get; set; } = 1f;

		// —— 饰品/招式修正（每帧由饰品写入，Exit 清）——
		public float MoveCooldownMultiplier { get; set; } = 1f;
		public float HenshinDamageBonus { get; set; }
		public float AffinityAmplitudeBonus { get; set; }
		public float ExtraFlightEnergy { get; set; }
		public float MoveSpeedBonus { get; set; }
		public float IncomingDamageMultiplier { get; set; } = 1f;
		public float HenshinDamageFactorBonus { get; set; }
		public float OnFireTargetBonus { get; set; }
		public float WaterSpeedBonus { get; set; }
		public float DashCooldownMultiplier { get; set; } = 1f;
		public float FallDamageReduction { get; set; }
		public float BossDamageBonus { get; set; }
		public bool AccActive { get; set; }

		// —— 飞行能量 ——
		public float FlightEnergy { get; private set; }
		public float FlightEnergyMax { get; private set; } = 4f * 60f;

		private ushort serverFormNetId;
		private ushort lastSyncedNetId;
		private bool serverPhasing;
		private int serverPhasingTimeLeft;

		private bool IsRemotePlayerOnClient => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer;

		#region 判定

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
			FlightEnergy = FlightEnergyMax;
			EnforceNoMount();
		}

		private void ExitForm()
		{
			if (IsPhasing)
				EndPhasing(safeTeleport: true);
			CurrentForm = null;
			TransformTicks = 0;
			ClearFrameBonuses();
			FlightEnergy = 0f;
		}

		private void ClearFrameBonuses()
		{
			MoveCooldownMultiplier = 1f;
			HenshinDamageBonus = 0f;
			AffinityAmplitudeBonus = 0f;
			ExtraFlightEnergy = 0f;
			MoveSpeedBonus = 0f;
			IncomingDamageMultiplier = 1f;
			HenshinDamageFactorBonus = 0f;
			OnFireTargetBonus = 0f;
			WaterSpeedBonus = 0f;
			DashCooldownMultiplier = 1f;
			FallDamageReduction = 0f;
			BossDamageBonus = 0f;
			PhasingBonusTicks = 0f;
			PhasingCooldownMultiplier = 1f;
			AccActive = false;
		}

		private void ReconcileForm()
		{
			FormDefinition desired = ResolveDesiredForm();
			if (ReferenceEquals(desired, CurrentForm))
				return;

			if (IsTransformed)
				ExitForm();
			if (desired != null)
				EnterForm(desired);

			if (Main.netMode == NetmodeID.Server)
				HenshinNet.BroadcastForm(this, ignoreClient: -1);
		}

		internal void ApplyServerForm(ushort netId)
		{
			serverFormNetId = netId;
			if (IsRemotePlayerOnClient)
				ReconcileForm();
		}

		internal void ApplyServerPhasing(bool phasing, int timeLeft)
		{
			serverPhasing = phasing;
			serverPhasingTimeLeft = timeLeft;
			if (IsRemotePlayerOnClient)
			{
				IsPhasing = phasing;
				PhasingTimeLeft = timeLeft;
			}
		}

		#endregion

		#region 穿障

		public bool TryStartPhasing()
		{
			if (!IsTransformed || CurrentForm == null || !CurrentForm.GrantsPhasing)
				return false;
			if (IsPhasing || PhasingCooldown > 0 || BossEngageTimer > 0)
				return false;
			if (IsRemotePlayerOnClient)
				return false;

			int duration = (int)System.Math.Min(PhasingMaxTicks, PhasingBaseTicks + PhasingBonusTicks);
			IsPhasing = true;
			PhasingTimeLeft = duration;
			Player.noBuilding = true;
			if (Main.netMode == NetmodeID.Server)
				HenshinNet.BroadcastPhasing(this);
			return true;
		}

		private void EndPhasing(bool safeTeleport)
		{
			IsPhasing = false;
			PhasingTimeLeft = 0;
			int cd = (int)(PhasingCooldownTicks * PhasingCooldownMultiplier);
			if (cd < PhasingCooldownTicks && PhasingBonusTicks >= (PhasingMaxTicks - PhasingBaseTicks))
				cd = (int)(PhasingCooldownTicks * 0.9f); // A11：已满时长则 CD×0.9，仍≥8s → 最低仍 480*0.9
			if (cd < (int)(PhasingCooldownTicks * 0.9f))
				cd = (int)(PhasingCooldownTicks * 0.9f);
			PhasingCooldown = System.Math.Max(cd, (int)(PhasingCooldownTicks * 0.9f));

			if (safeTeleport && Collision.SolidCollision(Player.position, Player.width, Player.height))
				TryRescueFromWall();

			if (Main.netMode == NetmodeID.Server)
				HenshinNet.BroadcastPhasing(this);
		}

		private void TryRescueFromWall()
		{
			Point origin = Player.Center.ToTileCoordinates();
			for (int r = 1; r <= 16; r++)
			{
				for (int dy = -r; dy <= r; dy++)
				{
					for (int dx = -r; dx <= r; dx++)
					{
						if (System.Math.Abs(dx) != r && System.Math.Abs(dy) != r)
							continue;
						Vector2 pos = new((origin.X + dx) * 16f, (origin.Y + dy) * 16f);
						if (!Collision.SolidCollision(pos, Player.width, Player.height))
						{
							Player.position = pos;
							Player.velocity = Vector2.Zero;
							return;
						}
					}
				}
			}
			Player.velocity = Vector2.Zero;
			Player.AddBuff(BuffID.Stoned, 60);
		}

		public void NotifyBossEngage()
		{
			BossEngageTimer = BossEngageLockTicks;
			if (IsPhasing)
				EndPhasing(safeTeleport: true);
		}

		#endregion

		#region 每 tick

		public override void ResetEffects()
		{
			ClearFrameBonuses();
		}

		public override void PreUpdate()
		{
			ReconcileForm();
			if (IsTransformed)
				TransformTicks++;

			if (PhasingCooldown > 0)
				PhasingCooldown--;
			if (BossEngageTimer > 0)
				BossEngageTimer--;

			if (IsPhasing && !IsRemotePlayerOnClient)
			{
				PhasingTimeLeft--;
				Player.noBuilding = true;
				Player.noItems = false;
				// 穿墙：短暂无碰撞
				Player.noFallDmg = true;
				if (PhasingTimeLeft <= 0)
					EndPhasing(safeTeleport: true);
			}
		}

		public override void PostUpdateEquips()
		{
			if (IsTransformed)
			{
				EnforceNoMount();
				FlightEnergyMax = 4f * 60f + ExtraFlightEnergy * 60f;
				TypePassiveApplier.Apply(this);
			}
		}

		public override void PostUpdate()
		{
			if (!IsTransformed)
				return;

			if (MoveSpeedBonus != 0f)
				Player.moveSpeed += MoveSpeedBonus;

			UpdateFlight();
		}

		private void UpdateFlight()
		{
			if (CurrentForm == null)
				return;
			bool flyingType = CurrentForm.Primary == PokemonType.Flying || CurrentForm.Secondary == PokemonType.Flying;
			if (!flyingType)
				return;

			if (Player.velocity.Y == 0f && Player.grappling[0] == -1)
			{
				FlightEnergy = FlightEnergyMax; // 落地回满（3s 需求简化为落地即回，与能量池配合）
			}

			if (Player.controlJump && FlightEnergy > 0f && !Player.mount.Active)
			{
				if (Player.velocity.Y > -6f)
					Player.velocity.Y = -6f;
				Player.fallStart = (int)(Player.position.Y / 16f);
				FlightEnergy--;
				Player.wingTime = 0; // 独立能量，不叠原版翅膀时长滥用
			}
		}

		public override void UpdateDead()
		{
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

		public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
		{
			if (npc.boss)
				NotifyBossEngage();
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (target.boss)
				NotifyBossEngage();
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (IsTransformed && IncomingDamageMultiplier != 1f)
				modifiers.FinalDamage *= IncomingDamageMultiplier;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (!IsTransformed)
				return;
			float mult = 1f + HenshinDamageBonus + HenshinDamageFactorBonus;
			if (BossDamageBonus > 0f && target.boss)
				mult += BossDamageBonus;
			if (OnFireTargetBonus > 0f && target.onFire)
				mult += OnFireTargetBonus;
			modifiers.FinalDamage *= mult;
		}

		public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
		{
			return true;
		}

		#endregion

		#region 禁坐骑

		public override void SetControls()
		{
			if (IsTransformed)
				Player.controlMount = false;
		}

		public override void PreUpdateMovement()
		{
			if (IsTransformed)
				EnforceNoMount();

			if (IsPhasing && !IsRemotePlayerOnClient)
			{
				Vector2 dir = Vector2.Zero;
				if (Player.controlLeft)
					dir.X -= 1f;
				if (Player.controlRight)
					dir.X += 1f;
				if (Player.controlUp || Player.controlJump)
					dir.Y -= 1f;
				if (Player.controlDown)
					dir.Y += 1f;
				if (dir != Vector2.Zero)
				{
					dir.Normalize();
					Player.position += dir * 6.5f;
					Player.velocity = dir * 0.1f;
				}
			}
		}

		private void EnforceNoMount()
		{
			Player.controlMount = false;
			if (!Player.mount.Active)
				return;
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
			HenshinNet.SendPhasing(this, toWho, fromWho);
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
