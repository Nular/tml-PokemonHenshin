using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Affinity;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.Visual;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.PlayerState
{
	public sealed class HenshinPlayer : ModPlayer
	{
		public const int HotbarSize = 10;
		public const int PhasingMaxTicks = 120;
		public const int PhasingBaseTicks = 90;
		public const int PhasingCooldownTicks = 480;
		public const int BossEngageLockTicks = 180;
		public const int DashBaseCooldown = 90;
		public const float EnergyOnHit = 4f;
		public const float EnergyOnKill = 18f;
		public const float EnergyPassivePerTick = 0.02f;

		public FormDefinition CurrentForm { get; private set; }
		public bool IsTransformed => CurrentForm != null;
		public ushort CurrentFormNetId => CurrentForm?.NetworkId ?? 0;
		public int TransformTicks { get; private set; }

		public bool IsPhasing { get; private set; }
		public int PhasingTimeLeft { get; private set; }
		public int PhasingCooldown { get; private set; }
		public int BossEngageTimer { get; private set; }
		public float PhasingBonusTicks { get; set; }
		public float PhasingCooldownMultiplier { get; set; } = 1f;

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

		// —— 新饰品标志 ——
		public bool AccWideLens { get; set; }
		public bool AccChoiceBand { get; set; }
		public bool AccScopeLens { get; set; }
		public bool AccLifeOrb { get; set; }
		public bool AccShellBell { get; set; }
		public bool AccRockyHelmet { get; set; }
		public float EnergyGainMultiplier { get; set; } = 1f;
		public float UltRetainFraction { get; set; }
		public float UltDamageBonus { get; set; }
		public float HomingTurnRate { get; set; } = 0.08f;

		// —— 被动运行时 ——
		public float TypeMoveBonus { get; set; }
		public bool SynchronizePassive { get; set; }
		public bool RoughSkinPassive { get; set; }
		public bool LevitateFlight { get; set; }
		public bool StaticPassive { get; set; }
		public bool IgnoreRecoil { get; set; }
		public bool SolarPowerDrain { get; set; }
		public int MoxieStacks { get; set; }
		public int AftermathPenaltyTimer { get; set; }
		public float AftermathPenaltyMult { get; set; } = 1f;

		// —— 能量（按 FormId 分存）——
		private readonly Dictionary<string, float> energyByForm = new();
		public float UltimateEnergy { get; private set; }
		public float UltimateEnergyMax => CurrentForm?.EnergyMax ?? 100f;
		public bool UltimateReady => IsTransformed && UltimateEnergy >= UltimateEnergyMax - 0.01f;

		/// <summary>读取指定形态的已存能量（当前变身同 FormId 时用实时条）。</summary>
		public float GetStoredEnergy(string formId, float energyMax = 100f)
		{
			if (string.IsNullOrEmpty(formId))
				return 0f;
			if (IsTransformed && CurrentForm != null && CurrentForm.FormId == formId)
				return UltimateEnergy;
			return energyByForm.TryGetValue(formId, out float e) ? e : 0f;
		}

		public float FlightEnergy { get; private set; }
		public float FlightEnergyMax { get; private set; } = 4f * 60f;

		public int DashCooldown { get; private set; }
		public int ShellBellCooldown { get; private set; }
		public int RockyHelmetCooldown { get; private set; }
		public int LifeOrbGate { get; private set; }
		public int GuardBonusTimer { get; set; }
		/// <summary>撞击类内置 CD（约 0.5s = 30 tick）。</summary>
		public int LungeCooldown { get; private set; }

		public MoveSlot LastMoveSlot { get; set; } = MoveSlot.Skill1;

		private ushort serverFormNetId;
		private ushort lastSyncedNetId;
		private float lastSyncedEnergy;
		private int dashDoubleTapTimer;
		private int lastDashDir;

		private bool IsRemotePlayerOnClient => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer;

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

		private void EnterForm(FormDefinition form)
		{
			CurrentForm = form;
			TransformTicks = 0;
			FlightEnergy = FlightEnergyMax;
			UltimateEnergy = energyByForm.TryGetValue(form.FormId, out float e) ? e : 0f;
			EnforceNoMount();
		}

		private void ExitForm()
		{
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = UltimateEnergy;
			if (IsPhasing)
				EndPhasing(safeTeleport: true);
			CurrentForm = null;
			TransformTicks = 0;
			ClearFrameBonuses();
			FlightEnergy = 0f;
			UltimateEnergy = 0f;
			MoxieStacks = 0;
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
			AccWideLens = false;
			AccChoiceBand = false;
			AccScopeLens = false;
			AccLifeOrb = false;
			AccShellBell = false;
			AccRockyHelmet = false;
			EnergyGainMultiplier = 1f;
			UltRetainFraction = 0f;
			UltDamageBonus = 0f;
			HomingTurnRate = 0.08f;
			TypeMoveBonus = 0f;
			SynchronizePassive = false;
			RoughSkinPassive = false;
			LevitateFlight = false;
			StaticPassive = false;
			IgnoreRecoil = false;
			SolarPowerDrain = false;
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
			if (IsRemotePlayerOnClient)
			{
				IsPhasing = phasing;
				PhasingTimeLeft = timeLeft;
			}
		}

		internal void ApplyServerEnergy(float energy)
		{
			UltimateEnergy = energy;
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = energy;
		}

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

		public void AddUltimateEnergy(float amount, bool fromUltGainPath = true)
		{
			if (!IsTransformed || amount <= 0f)
				return;
			UltimateEnergy = System.Math.Clamp(UltimateEnergy + amount * EnergyGainMultiplier, 0f, UltimateEnergyMax);
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = UltimateEnergy;
		}

		public bool TryConsumeUltimate()
		{
			if (!UltimateReady || AccChoiceBand)
				return false;
			float retain = UltRetainFraction;
			UltimateEnergy = UltimateEnergyMax * retain;
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = UltimateEnergy;
			if (Main.netMode == NetmodeID.MultiplayerClient)
				HenshinNet.SendEnergy(this);
			return true;
		}

		public bool TryDash(int dir)
		{
			if (!IsTransformed || DashCooldown > 0)
				return false;
			dir = dir >= 0 ? 1 : -1;
			Player.velocity.X = dir * 12f;
			Player.velocity.Y = System.Math.Min(Player.velocity.Y, -2f);
			DashCooldown = (int)System.Math.Max(1, DashBaseCooldown * DashCooldownMultiplier);
			for (int i = 0; i < 16; i++)
			{
				Dust d = Dust.NewDustPerfect(Player.Center, DustID.Electric, new Vector2(-dir * 3f, Main.rand.NextFloat(-2f, 2f)), 100, default, 1.3f);
				d.noGravity = true;
			}
			SoundEngine.PlaySound(SoundID.Item10, Player.Center);
			return true;
		}

		public bool CanLunge => LungeCooldown <= 0;

		public void StartLungeCooldown(int ticks = 120)
		{
			LungeCooldown = System.Math.Max(LungeCooldown, ticks);
		}

		public override void ResetEffects() => ClearFrameBonuses();

		public override void PreUpdate()
		{
			ReconcileForm();
			if (IsTransformed)
				TransformTicks++;

			if (PhasingCooldown > 0) PhasingCooldown--;
			if (BossEngageTimer > 0) BossEngageTimer--;
			if (DashCooldown > 0) DashCooldown--;
			if (LungeCooldown > 0) LungeCooldown--;
			if (ShellBellCooldown > 0) ShellBellCooldown--;
			if (RockyHelmetCooldown > 0) RockyHelmetCooldown--;
			if (LifeOrbGate > 0) LifeOrbGate--;
			if (GuardBonusTimer > 0)
			{
				GuardBonusTimer--;
				IncomingDamageMultiplier *= 0.65f;
			}
			if (AftermathPenaltyTimer > 0)
			{
				AftermathPenaltyTimer--;
				if (AftermathPenaltyTimer <= 0)
					AftermathPenaltyMult = 1f;
			}
			if (dashDoubleTapTimer > 0)
				dashDoubleTapTimer--;

			if (IsPhasing && !IsRemotePlayerOnClient)
			{
				PhasingTimeLeft--;
				Player.noBuilding = true;
				Player.noFallDmg = true;
				if (PhasingTimeLeft <= 0)
					EndPhasing(safeTeleport: true);
			}

			if (IsTransformed && !IsRemotePlayerOnClient)
			{
				float passive = EnergyPassivePerTick;
				if (BossEngageTimer > 0 || NearBoss())
					passive *= 3f;
				AddUltimateEnergy(passive, fromUltGainPath: true);
			}
		}

		private bool NearBoss()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (n.active && n.boss && n.Distance(Player.Center) < 80f * 16f)
					return true;
			}
			return false;
		}

		public override void PostUpdateEquips()
		{
			if (!IsTransformed)
				return;
			EnforceNoMount();
			FlightEnergyMax = (LevitateFlight ? 10f : 4f) * 60f + ExtraFlightEnergy * 60f;
			FormPassiveApplier.Apply(this);
			TypePassiveApplier.Apply(this);
		}

		public override void PostUpdate()
		{
			if (!IsTransformed)
				return;

			if (MoveSpeedBonus != 0f)
				Player.moveSpeed += MoveSpeedBonus;

			UpdateFlight();
			TryProcessUltimateKey();
			TryProcessDashInput();
		}

		private void TryProcessUltimateKey()
		{
			if (Player.whoAmI != Main.myPlayer || IsRemotePlayerOnClient)
				return;
			if (HenshinKeybinds.Ultimate == null || !HenshinKeybinds.Ultimate.JustPressed)
				return;
			if (AccChoiceBand)
			{
				Main.NewText(Language.GetTextValue("Mods.PokemonHenshin.Common.ChoiceBandLocked"), Color.Orange);
				return;
			}
			if (!UltimateReady)
			{
				Main.NewText(Language.GetTextValue("Mods.PokemonHenshin.Common.UltimateNotReady"), Color.Orange);
				return;
			}
			if (Player.HeldItem?.ModItem is HenshinForceItem force)
				force.TryFireUltimate(Player);
		}

		private void TryProcessDashInput()
		{
			if (Player.whoAmI != Main.myPlayer || CurrentForm == null)
				return;
			bool electric = CurrentForm.Primary == PokemonType.Electric || CurrentForm.Secondary == PokemonType.Electric;
			if (!electric)
				return;

			int dir = 0;
			if (Player.controlRight && Player.releaseRight)
				dir = 1;
			else if (Player.controlLeft && Player.releaseLeft)
				dir = -1;
			if (dir == 0)
				return;

			if (dashDoubleTapTimer > 0 && lastDashDir == dir)
				TryDash(dir);
			else
			{
				dashDoubleTapTimer = 15;
				lastDashDir = dir;
			}
		}

		private void UpdateFlight()
		{
			if (CurrentForm == null)
				return;
			bool flyingType = LevitateFlight
				|| CurrentForm.Primary == PokemonType.Flying
				|| CurrentForm.Secondary == PokemonType.Flying;
			if (!flyingType)
				return;

			if (Player.velocity.Y == 0f && Player.grappling[0] == -1)
				FlightEnergy = FlightEnergyMax;

			if (Player.controlJump && FlightEnergy > 0f && !Player.mount.Active)
			{
				float lift = LevitateFlight ? -7.5f : -6f;
				if (Player.velocity.Y > lift)
					Player.velocity.Y = lift;
				Player.fallStart = (int)(Player.position.Y / 16f);
				FlightEnergy--;
				Player.wingTime = 0;
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
			OnDamagedCommon(npc);
		}

		public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
		{
			OnDamagedCommon(null);
		}

		private void OnDamagedCommon(NPC source)
		{
			if (!IsTransformed)
				return;
			if (StaticPassive && source != null && source.active)
				source.AddBuff(BuffID.Electrified, 120);
			if (SynchronizePassive && source != null && source.active)
			{
				if (Player.HasBuff(BuffID.Poisoned)) source.AddBuff(BuffID.Poisoned, 180);
				if (Player.HasBuff(BuffID.OnFire)) source.AddBuff(BuffID.OnFire, 180);
				if (Player.HasBuff(BuffID.Electrified)) source.AddBuff(BuffID.Electrified, 120);
			}
			if (RoughSkinPassive && source != null && source.active)
			{
				int dmg = System.Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.15f));
				source.SimpleStrikeNPC(dmg, 0, false, 0f, DamageClass.Generic);
			}
			if (AccRockyHelmet && RockyHelmetCooldown <= 0)
			{
				RockyHelmetCooldown = 45;
				int baseDmg = System.Math.Max(1, Player.GetWeaponDamage(Player.HeldItem));
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (!n.active || n.friendly || n.life <= 0)
						continue;
					if (n.Distance(Player.Center) > 6f * 16f)
						continue;
					n.SimpleStrikeNPC(baseDmg, n.Center.X < Player.Center.X ? -1 : 1, false, 2f, ModContent.GetInstance<Damage.HenshinDamage>());
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!IsTransformed)
				return;
			if (target.boss)
				NotifyBossEngage();
			if (LastMoveSlot != MoveSlot.Ultimate)
				AddUltimateEnergy(EnergyOnHit);

			if (target.life <= 0)
			{
				if (LastMoveSlot != MoveSlot.Ultimate)
					AddUltimateEnergy(EnergyOnKill);
				if (CurrentForm?.Passive == FormPassiveKind.Moxie && MoxieStacks < 2)
					MoxieStacks++;
			}

			ApplyOnHitAccessories(damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			OnHitNPC(target, hit, damageDone);
		}

		private void ApplyOnHitAccessories(int damageDone)
		{
			if (AccLifeOrb && LifeOrbGate <= 0 && Player.statLife > 1)
			{
				LifeOrbGate = 8;
				Player.statLife = System.Math.Max(1, Player.statLife - 1);
				Player.HealEffect(-1, true);
			}
			if (SolarPowerDrain && Player.statLife > 1 && LifeOrbGate <= 0)
			{
				Player.statLife = System.Math.Max(1, Player.statLife - 1);
			}
			if (AccShellBell && ShellBellCooldown <= 0 && damageDone > 0)
			{
				// 灾厄吸血冷却不可靠反射时，使用本模短 CD（约 0.5s）
				ShellBellCooldown = 30;
				Player.statLife = System.Math.Min(Player.statLifeMax2, Player.statLife + 2);
				Player.HealEffect(2);
			}
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

			float mult = 1f + HenshinDamageBonus;
			if (BossDamageBonus > 0f && target.boss)
				mult += BossDamageBonus;
			if (OnFireTargetBonus > 0f && target.onFire)
				mult += OnFireTargetBonus;
			if (TypeMoveBonus > 0f)
				mult += TypeMoveBonus;
			if (AccLifeOrb)
				mult += 0.20f;
			if (AccChoiceBand)
				mult += 0.50f;
			if (LastMoveSlot == MoveSlot.Ultimate)
				mult += UltDamageBonus;
			mult *= AftermathPenaltyMult;
			modifiers.FinalDamage *= mult;

			ApplyCritTier(ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			ModifyHitNPC(target, ref modifiers);
		}

		private void ApplyCritTier(ref NPC.HitModifiers modifiers)
		{
			bool easy = false;
			if (Player.HeldItem?.ModItem is HenshinForceItem force)
			{
				MoveSpec move = force.GetMove(LastMoveSlot);
				easy = move?.EasyCrit == true;
			}

			float upgradeChance = AccScopeLens ? 0.10f : 0f;
			if (easy)
				upgradeChance += 0.25f;

			float critChance = Player.GetTotalCritChance(Damage.HenshinDamage.Instance) / 100f;
			if (easy)
				critChance += 0.35f;

			bool baseCrit = Main.rand.NextFloat() < critChance;
			modifiers.DisableCrit(); // 手动结算档位，避免引擎重复暴击

			if (!baseCrit)
			{
				if (Main.rand.NextFloat() < upgradeChance)
					modifiers.FinalDamage *= 2f; // 升为普通暴击
			}
			else
			{
				modifiers.FinalDamage *= 2f;
				if (Main.rand.NextFloat() < upgradeChance)
					modifiers.FinalDamage *= 2f; // 超暴击 ≈×4
			}
		}

		public void ApplyRecoil(float fraction)
		{
			if (IgnoreRecoil || fraction <= 0f)
				return;
			int dmg = System.Math.Max(1, (int)(Player.statLifeMax2 * fraction));
			Player.statLife = System.Math.Max(1, Player.statLife - dmg);
			Player.HealEffect(-dmg, true);
		}

		public void ApplyAftermath(int ticks, float mult)
		{
			AftermathPenaltyTimer = ticks;
			AftermathPenaltyMult = mult;
		}

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
				if (Player.controlLeft) dir.X -= 1f;
				if (Player.controlRight) dir.X += 1f;
				if (Player.controlUp || Player.controlJump) dir.Y -= 1f;
				if (Player.controlDown) dir.Y += 1f;
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

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
		{
			HenshinNet.SendForm(this, toWho, fromWho);
			HenshinNet.SendPhasing(this, toWho, fromWho);
			HenshinNet.SendEnergy(this, toWho, fromWho);
		}

		public override void CopyClientState(ModPlayer targetCopy)
		{
			var t = (HenshinPlayer)targetCopy;
			t.lastSyncedNetId = CurrentFormNetId;
			t.lastSyncedEnergy = UltimateEnergy;
		}

		public override void SendClientChanges(ModPlayer clientPlayer)
		{
			var c = (HenshinPlayer)clientPlayer;
			if (c.lastSyncedNetId != CurrentFormNetId)
				HenshinNet.SendForm(this, -1, Player.whoAmI);
			if (System.Math.Abs(c.lastSyncedEnergy - UltimateEnergy) > 1f)
				HenshinNet.SendEnergy(this);
		}
	}
}
