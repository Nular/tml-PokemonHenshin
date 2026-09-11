using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Affinity;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.Net;
using PokemonHenshin.Content.Prefixes;
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
		public const float EnergyOnHit = HenshinStatService.EnergyOnHit;
		public const float EnergyOnKill = HenshinStatService.EnergyOnKill;
		public const float EnergyPassivePerTick = HenshinStatService.EnergyPassivePerTick;

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
		public float LungeCooldownMultiplier { get; set; } = 1f;
		public float FallDamageReduction { get; set; }
		public float BossDamageBonus { get; set; }
		public bool AccActive { get; set; }

		public float IncomingCut { get; set; }
		public float CooldownCut { get; set; }
		public float DashCooldownCut { get; set; }
		public float LungeCooldownCut { get; set; }
		public float FallDmgTakenMul { get; set; } = 1f;
		public float EnergyGainAdd { get; set; }
		public float EnergyGainMulProduct { get; set; } = 1f;
		public float EnergyGainMultiplier { get; set; } = 1f;
		public float UltRetainFraction { get; set; }
		public float UltDamageBonus { get; set; }
		public float HomingTurn { get; set; }
		public float HomingTurnRate { get; set; } = 0.08f;
		public float HomingRangeTiles { get; set; }
		public bool HomingBolt { get; set; }
		public bool HomingSpread { get; set; }
		public bool HomingBarrage { get; set; }
		public bool HomingDoTBind { get; set; }
		public bool TilePierceBolt { get; set; }
		public bool TilePierceSpread { get; set; }
		public bool TilePierceBarrage { get; set; }
		public bool TilePierceDoTBind { get; set; }
		public bool TilePierceBeam { get; set; }
		public bool TilePierceField { get; set; }
		public int PenetrateAdd { get; set; }
		public bool CurseTagSuperImmune { get; set; }
		public int CurseTagBurnTicks { get; set; }
		private int _curseTagPulseCd;
		public bool ChoiceLockSkill2 { get; set; }
		public bool ChoiceLockUlt { get; set; }
		public float ChoiceDamage { get; set; }
		public float LifeOrbDamage { get; set; }
		public bool LifeOrbHpDrain { get; set; }
		public float LifeOrbGateTicks { get; set; } = float.MaxValue;
		public float ShellBellHeal { get; set; }
		public float ShellBellCdTicks { get; set; } = float.MaxValue;
		public float RockyHelmetScale { get; set; }
		public float RockyHelmetCdTicks { get; set; } = float.MaxValue;
		public float LeftoversHpPerSec { get; set; }
		public float LeftoversLowHpBonus { get; set; }
		public bool FocusSash { get; set; }
		public float FocusSashHpPct { get; set; } = 1f;
		public float FocusSashCdSec { get; set; } = 999f;
		public float FocusSashImmuneTicks { get; set; }
		public int FocusSashCooldown { get; private set; }
		public float EvioliteDefMul { get; set; }
		public float EvioliteDamage { get; set; }
		public bool EverstoneBlock { get; set; }
		public float XpHeldMul { get; set; }
		public float XpHotbarShareMul { get; set; }
		public float CritUpgradeChance { get; set; }
		public float OnFireCritUpgrade { get; set; }
		public float FireMoveDamage { get; set; }
		public float MeleeDeliveryDamage { get; set; }
		public float PassiveEnergyMul { get; set; }
		public float DashSpeedBonus { get; set; }
		public int LungeIFrameBonus { get; set; }
		public float PsychicDragonDamage { get; set; }
		public int AccGuardCutTicks { get; set; }
		public int AccGuardActiveTimer { get; set; }
		/// <summary>铁壁前缀：形态防御结算后再乘的额外比例（如 0.20）。</summary>
		public float PrefixFormDefenseMul { get; set; }

		private static readonly Color SuperCritCombatColor = new(255, 55, 20);
		private const float SuperCritCombatScale = 1.45f;
		private bool _superCritPending;

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
		public float UltimateEnergyMax => CurrentForm?.EnergyMax ?? HenshinStatService.EnergyMaxDefault;
		public bool UltimateReady => IsTransformed && UltimateEnergy >= UltimateEnergyMax - 0.01f;

		/// <summary>读取指定形态的已存能量（当前变身同 FormId 时用实时条）。</summary>
		public float GetStoredEnergy(string formId, float energyMax = HenshinStatService.EnergyMaxDefault)
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

		private readonly float[] combatEnergyRing = new float[60];
		private int combatEnergyIndex;

		private ushort serverFormNetId;
		private ushort lastSyncedNetId;
		private float lastSyncedEnergy;
		private int lastSyncedLevel;
		private int lastSyncedXp;
		private int dashDoubleTapTimer;
		private int lastDashDir;

		// 联机瞄准：对齐大修 HalibutPlayer.MouseWorld（InnoVault PlayerNetwork），本模用 NetOp.SyncAim。
		private Vector2 _syncedMouseWorld;
		private bool _aimReceived;
		private Vector2 _lastSentAim;
		private int _aimMoveCd;
		private int _aimHeartCd;
		private const float AimSyncMinMoveSq = 16f;
		private const int AimSyncMoveInterval = 2;
		private const int AimSyncHeartbeat = 12;

		private bool IsRemotePlayerOnClient => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer;

		/// <summary>该玩家的瞄准世界坐标。本地用 <see cref="Main.MouseWorld"/>；旁观/服务器用已同步近似值。</summary>
		public Vector2 MouseWorld => GetMouseWorld(Player);

		public static Vector2 GetMouseWorld(Player player)
		{
			if (player == null || !player.active)
				return Main.dedServ ? Vector2.Zero : Main.MouseWorld;
			if (player.whoAmI == Main.myPlayer && !Main.dedServ)
				return Main.MouseWorld;
			HenshinPlayer mp = player.GetModPlayer<HenshinPlayer>();
			if (mp._aimReceived)
				return mp._syncedMouseWorld;
			return player.MountedCenter + new Vector2(player.direction * 176f, 0f);
		}

		internal Vector2 SerializeAimWorld() => GetMouseWorld(Player);

		internal void ApplyRemoteAim(Vector2 world)
		{
			_syncedMouseWorld = world;
			_aimReceived = true;
		}

		private void TickAimSync()
		{
			if (Player.whoAmI != Main.myPlayer || Main.dedServ)
				return;

			Vector2 now = Main.MouseWorld;
			_syncedMouseWorld = now;
			_aimReceived = true;

			if (Main.netMode == NetmodeID.SinglePlayer || !IsTransformed)
				return;

			if (_aimMoveCd > 0)
				_aimMoveCd--;
			if (_aimHeartCd > 0)
				_aimHeartCd--;

			bool moved = Vector2.DistanceSquared(now, _lastSentAim) > AimSyncMinMoveSq;
			if (moved && _aimMoveCd <= 0)
			{
				HenshinNet.SendAim(this);
				_lastSentAim = now;
				_aimMoveCd = AimSyncMoveInterval;
				_aimHeartCd = AimSyncHeartbeat;
			}
			else if (_aimHeartCd <= 0)
			{
				HenshinNet.SendAim(this);
				_lastSentAim = now;
				_aimHeartCd = AimSyncHeartbeat;
			}
		}

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
			_aimMoveCd = 0;
			_aimHeartCd = 0;
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

		private float leftoversAcc;
		private readonly HashSet<int> _killXpGranted = new();

		public bool ShouldHoming(MoveDelivery d) => d switch
		{
			MoveDelivery.Bolt => HomingBolt,
			MoveDelivery.Spread => HomingSpread,
			MoveDelivery.Barrage => HomingBarrage,
			MoveDelivery.DoTBind => HomingDoTBind,
			_ => false
		};

		public bool ShouldTilePierce(MoveDelivery d) => d switch
		{
			MoveDelivery.Bolt => TilePierceBolt,
			MoveDelivery.Spread => TilePierceSpread,
			MoveDelivery.Barrage => TilePierceBarrage,
			MoveDelivery.DoTBind => TilePierceDoTBind,
			MoveDelivery.Beam => TilePierceBeam,
			MoveDelivery.Field => TilePierceField,
			_ => false
		};

		public const int CurseTagPulseInterval = 1800;

		public void NotifyCurseTagWorn(AccPiece piece)
		{
			if (piece == AccPiece.Super)
			{
				CurseTagSuperImmune = true;
				return;
			}
			int ticks = piece == AccPiece.Normal ? 300 : 180;
			if (ticks > CurseTagBurnTicks)
				CurseTagBurnTicks = ticks;
		}

		public void ApplyAccStat(AccStatLine line)
		{
			switch (line.Stat)
			{
				case AccStat.DamageBonus: HenshinDamageBonus += line.Value; break;
				case AccStat.DamageFactorBonus: HenshinDamageFactorBonus += line.Value; break;
				case AccStat.MeleeDeliveryDamage: MeleeDeliveryDamage += line.Value; break;
				case AccStat.BossDamageBonus: BossDamageBonus += line.Value; break;
				case AccStat.OnFireTargetBonus: OnFireTargetBonus += line.Value; break;
				case AccStat.UltDamageBonus: UltDamageBonus += line.Value; break;
				case AccStat.IncomingCut: IncomingCut += line.Value; break;
				case AccStat.CooldownCut: CooldownCut += line.Value; break;
				case AccStat.DashCooldownCut: DashCooldownCut += line.Value; break;
				case AccStat.LungeCooldownCut: LungeCooldownCut += line.Value; break;
				case AccStat.MoveSpeedBonus: MoveSpeedBonus += line.Value; break;
				case AccStat.WaterSpeedBonus: WaterSpeedBonus += line.Value; break;
				case AccStat.FlightEnergySec: ExtraFlightEnergy += line.Value; break;
				case AccStat.FallDmgTakenMul:
					FallDmgTakenMul = Math.Min(FallDmgTakenMul, line.Value);
					break;
				case AccStat.AffinityAmp: AffinityAmplitudeBonus += line.Value; break;
				case AccStat.EnergyGainAdd: EnergyGainAdd += line.Value; break;
				case AccStat.EnergyGainMul: EnergyGainMulProduct *= line.Value; break;
				case AccStat.UltRetain: UltRetainFraction = Math.Max(UltRetainFraction, line.Value); break;
				case AccStat.XpHeldMul: XpHeldMul += line.Value; break;
				case AccStat.XpHotbarShareMul: XpHotbarShareMul += line.Value; break;
				case AccStat.HomingTurn: HomingTurn = Math.Max(HomingTurn, line.Value); break;
				case AccStat.HomingRange: HomingRangeTiles = Math.Max(HomingRangeTiles, line.Value); break;
				case AccStat.HomingBolt: HomingBolt = true; break;
				case AccStat.HomingSpread: HomingSpread = true; break;
				case AccStat.HomingBarrage: HomingBarrage = true; break;
				case AccStat.HomingDoTBind: HomingDoTBind = true; break;
				case AccStat.TilePierceBolt: TilePierceBolt = true; break;
				case AccStat.TilePierceSpread: TilePierceSpread = true; break;
				case AccStat.TilePierceBarrage: TilePierceBarrage = true; break;
				case AccStat.TilePierceDoTBind: TilePierceDoTBind = true; break;
				case AccStat.TilePierceBeam: TilePierceBeam = true; break;
				case AccStat.TilePierceField: TilePierceField = true; break;
				case AccStat.CursedInfernoImmune: CurseTagSuperImmune = true; break;
				case AccStat.CursedInfernoSec:
					CurseTagBurnTicks = Math.Max(CurseTagBurnTicks, (int)(line.Value * 60f));
					break;
				case AccStat.PenetrateAdd: PenetrateAdd += (int)line.Value; break;
				case AccStat.ChoiceLockSkill2: ChoiceLockSkill2 = true; break;
				case AccStat.ChoiceLockUlt: ChoiceLockUlt = true; break;
				case AccStat.ChoiceDamage: ChoiceDamage += line.Value; break;
				case AccStat.LifeOrbDamage: LifeOrbDamage += line.Value; break;
				case AccStat.LifeOrbHpDrain: LifeOrbHpDrain = true; break;
				case AccStat.LifeOrbGateTicks: LifeOrbGateTicks = Math.Min(LifeOrbGateTicks, line.Value); break;
				case AccStat.ShellBellHeal: ShellBellHeal += line.Value; break;
				case AccStat.ShellBellCdTicks: ShellBellCdTicks = Math.Min(ShellBellCdTicks, line.Value); break;
				case AccStat.RockyHelmetScale: RockyHelmetScale += line.Value; break;
				case AccStat.RockyHelmetCdTicks: RockyHelmetCdTicks = Math.Min(RockyHelmetCdTicks, line.Value); break;
				case AccStat.LeftoversHpPerSec: LeftoversHpPerSec += line.Value; break;
				case AccStat.LeftoversLowHpBonus: LeftoversLowHpBonus += line.Value; break;
				case AccStat.FocusSash: FocusSash = true; break;
				case AccStat.FocusSashHpPct: FocusSashHpPct = Math.Min(FocusSashHpPct, line.Value); break;
				case AccStat.FocusSashCdSec: FocusSashCdSec = Math.Min(FocusSashCdSec, line.Value); break;
				case AccStat.FocusSashImmuneTicks: FocusSashImmuneTicks = Math.Max(FocusSashImmuneTicks, line.Value); break;
				case AccStat.EvioliteDefMul: EvioliteDefMul += line.Value; break;
				case AccStat.EvioliteDamage: EvioliteDamage += line.Value; break;
				case AccStat.EverstoneBlock: EverstoneBlock = true; break;
				case AccStat.CritUpgradeChance: CritUpgradeChance += line.Value; break;
				case AccStat.OnFireCritUpgrade: OnFireCritUpgrade += line.Value; break;
				case AccStat.FireMoveDamage: FireMoveDamage += line.Value; break;
				case AccStat.PassiveEnergyMul: PassiveEnergyMul += line.Value; break;
				case AccStat.DashSpeedBonus: DashSpeedBonus += line.Value; break;
				case AccStat.LungeIFrameBonus: LungeIFrameBonus += (int)line.Value; break;
				case AccStat.PsychicDragonDamage: PsychicDragonDamage += line.Value; break;
				case AccStat.GuardCutTimer: AccGuardCutTicks = Math.Max(AccGuardCutTicks, (int)line.Value); break;
			}
		}

		public void FinalizeAccStats()
		{
			IncomingDamageMultiplier *= 1f - Math.Min(0.30f, IncomingCut);
			MoveCooldownMultiplier *= 1f - Math.Min(0.20f, CooldownCut);
			DashCooldownMultiplier *= 1f - Math.Min(0.30f, DashCooldownCut);
			LungeCooldownMultiplier *= 1f - Math.Min(0.30f, LungeCooldownCut);
			EnergyGainMultiplier = (1f + EnergyGainAdd) * EnergyGainMulProduct;
			if (FallDmgTakenMul < 1f)
				FallDamageReduction = Math.Max(FallDamageReduction, 1f - FallDmgTakenMul);
			if (HomingTurn > 0f)
				HomingTurnRate = HomingTurn;
			if (AccGuardActiveTimer > 0)
				IncomingDamageMultiplier *= 0.97f;
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
			LungeCooldownMultiplier = 1f;
			FallDamageReduction = 0f;
			BossDamageBonus = 0f;
			PhasingBonusTicks = 0f;
			PhasingCooldownMultiplier = 1f;
			AccActive = false;
			IncomingCut = 0f;
			CooldownCut = 0f;
			DashCooldownCut = 0f;
			LungeCooldownCut = 0f;
			FallDmgTakenMul = 1f;
			EnergyGainAdd = 0f;
			EnergyGainMulProduct = 1f;
			EnergyGainMultiplier = 1f;
			UltRetainFraction = 0f;
			UltDamageBonus = 0f;
			HomingTurn = 0f;
			HomingTurnRate = 0.08f;
			HomingRangeTiles = 0f;
			HomingBolt = HomingSpread = HomingBarrage = HomingDoTBind = false;
			TilePierceBolt = TilePierceSpread = TilePierceBarrage = TilePierceDoTBind = TilePierceBeam = TilePierceField = false;
			PenetrateAdd = 0;
			CurseTagSuperImmune = false;
			CurseTagBurnTicks = 0;
			ChoiceLockSkill2 = false;
			ChoiceLockUlt = false;
			ChoiceDamage = 0f;
			LifeOrbDamage = 0f;
			LifeOrbHpDrain = false;
			LifeOrbGateTicks = float.MaxValue;
			ShellBellHeal = 0f;
			ShellBellCdTicks = float.MaxValue;
			RockyHelmetScale = 0f;
			RockyHelmetCdTicks = float.MaxValue;
			LeftoversHpPerSec = 0f;
			LeftoversLowHpBonus = 0f;
			FocusSash = false;
			FocusSashHpPct = 1f;
			FocusSashCdSec = 999f;
			FocusSashImmuneTicks = 0f;
			EvioliteDefMul = 0f;
			EvioliteDamage = 0f;
			EverstoneBlock = false;
			XpHeldMul = 0f;
			XpHotbarShareMul = 0f;
			CritUpgradeChance = 0f;
			OnFireCritUpgrade = 0f;
			FireMoveDamage = 0f;
			MeleeDeliveryDamage = 0f;
			PassiveEnergyMul = 0f;
			DashSpeedBonus = 0f;
			LungeIFrameBonus = 0;
			PsychicDragonDamage = 0f;
			AccGuardCutTicks = 0;
			PrefixFormDefenseMul = 0f;
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
			AddPassiveEnergy(amount);
		}

		public void AddPassiveEnergy(float amount)
		{
			if (!IsTransformed || amount <= 0f)
				return;
			UltimateEnergy = System.Math.Clamp(UltimateEnergy + amount, 0f, UltimateEnergyMax);
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = UltimateEnergy;
		}

		public void AddCombatEnergy(float amount)
		{
			if (!IsTransformed || amount <= 0f)
				return;
			float scaled = amount * EnergyGainMultiplier;
			float used = 0f;
			for (int i = 0; i < combatEnergyRing.Length; i++)
				used += combatEnergyRing[i];
			float allowed = System.Math.Max(0f, HenshinStatService.EnergyCombatSoftCapPerSecond - used);
			float add = System.Math.Min(scaled, allowed);
			if (add <= 0f)
				return;
			combatEnergyRing[combatEnergyIndex] += add;
			UltimateEnergy = System.Math.Clamp(UltimateEnergy + add, 0f, UltimateEnergyMax);
			if (CurrentForm != null)
				energyByForm[CurrentForm.FormId] = UltimateEnergy;
		}

		private void TickCombatEnergyWindow()
		{
			combatEnergyIndex = (combatEnergyIndex + 1) % combatEnergyRing.Length;
			combatEnergyRing[combatEnergyIndex] = 0f;
		}

		public bool TryConsumeUltimate()
		{
			if (!UltimateReady || ChoiceLockUlt)
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
			Player.velocity.X = dir * (12f + DashSpeedBonus);
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
			int scaled = (int)Math.Max(1, Math.Round(ticks * LungeCooldownMultiplier));
			LungeCooldown = Math.Max(LungeCooldown, scaled);
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
			if (FocusSashCooldown > 0) FocusSashCooldown--;
			if (AccGuardActiveTimer > 0)
				AccGuardActiveTimer--;
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
				TickCombatEnergyWindow();

			TickAimSync();
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
			FinalizeAccStats();
			ApplyHeldForcePrefix();
			if (!IsTransformed)
				return;
			EnforceNoMount();
			FlightEnergyMax = (LevitateFlight ? 10f : 4f) * 60f + ExtraFlightEnergy * 60f;
			FormPassiveApplier.Apply(this);
			TypePassiveApplier.Apply(this);
			ApplyFormDefense();
			if (!IsRemotePlayerOnClient)
			{
				float passive = EnergyPassivePerTick * (1f + PassiveEnergyMul);
				if (BossEngageTimer > 0 || NearBoss())
					passive *= 3f;
				AddPassiveEnergy(passive);
			}
		}

		/// <summary>持握之力专属前缀：仅变身生效。</summary>
		private void ApplyHeldForcePrefix()
		{
			if (!IsTransformed)
				return;
			if (Player.HeldItem?.ModItem is not HenshinForceItem)
				return;

			int prefix = Player.HeldItem.prefix;
			if (prefix == ModContent.PrefixType<HenshinPrefixStored>())
			{
				EnergyGainMultiplier *= HenshinPrefixStored.EnergyMul;
			}
			else if (prefix == ModContent.PrefixType<HenshinPrefixIronwall>())
			{
				PrefixFormDefenseMul += HenshinPrefixIronwall.FormDefenseMul;
			}
			else if (prefix == ModContent.PrefixType<HenshinPrefixAssault>())
			{
				HenshinDamageBonus += HenshinPrefixAssault.DamageBonus;
				MoveCooldownMultiplier *= HenshinPrefixAssault.CooldownMul;
			}
		}

		private void ApplyFormDefense()
		{
			int armorDef = 0;
			for (int i = 0; i < 3; i++)
			{
				Item piece = Player.armor[i];
				if (piece != null && !piece.IsAir)
					armorDef += piece.defense;
			}

			Player.statDefense -= armorDef;
			int formDef = 0;
			if (Player.HeldItem?.ModItem is HenshinForceItem force)
				formDef = force.ComputeFinalDefense();
			else if (CurrentForm != null)
			{
				FormStatTable.Mods mods = FormStatTable.Get(CurrentForm.FormId);
				int level = HenshinStatService.StartingLevelForFormStage(CurrentForm.Stage);
				if (Player.HeldItem?.ModItem is HenshinForceItem held)
					level = held.Level;
				formDef = HenshinStatService.FinalDefense(level, mods.DefenseMod);
			}

			Player.statDefense += formDef;
			if (EvioliteDefMul > 0f && CurrentForm != null
				&& FormRegistry.FindEvolutionOf(CurrentForm.FormId) != null)
			{
				int extra = (int)Math.Round(formDef * EvioliteDefMul);
				Player.statDefense += extra;
			}
			if (PrefixFormDefenseMul > 0f && formDef > 0)
			{
				int prefixExtra = (int)Math.Round(formDef * PrefixFormDefenseMul);
				Player.statDefense += prefixExtra;
			}
		}

		public override void PostUpdate()
		{
			CleanupKillXp();
			TickCurseTagBurn();
			if (!IsTransformed)
				return;

			if (MoveSpeedBonus != 0f)
				Player.moveSpeed += MoveSpeedBonus;

			TickLeftovers();
			UpdateFlight();
			TryProcessUltimateKey();
			TryProcessDashInput();
			TrySpawnFullChargeDust();
		}

		private void TickCurseTagBurn()
		{
			bool tax = CurseTagBurnTicks > 0 && !CurseTagSuperImmune;
			if (!tax)
			{
				_curseTagPulseCd = 0;
				return;
			}
			if (_curseTagPulseCd <= 0)
			{
				Player.AddBuff(BuffID.CursedInferno, CurseTagBurnTicks);
				_curseTagPulseCd = CurseTagPulseInterval;
			}
			else
				_curseTagPulseCd--;
		}

		private void TickLeftovers()
		{
			if (LeftoversHpPerSec <= 0f && LeftoversLowHpBonus <= 0f)
			{
				leftoversAcc = 0f;
				return;
			}
			if (Player.statLife >= Player.statLifeMax2)
			{
				leftoversAcc = 0f;
				return;
			}
			leftoversAcc += LeftoversHpPerSec / 60f;
			if (LeftoversLowHpBonus > 0f && Player.statLife < Player.statLifeMax2 * 0.5f)
				leftoversAcc += LeftoversLowHpBonus / 60f;
			int heal = (int)leftoversAcc;
			if (heal <= 0)
				return;
			leftoversAcc -= heal;
			Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + heal);
			Player.HealEffect(heal);
		}

		/// <summary>大招满充：角色周围稀疏金色发散尘，路径约 1 格，整体向上，营造「充满电」感。</summary>
		private void TrySpawnFullChargeDust()
		{
			if (Main.dedServ || !UltimateReady)
				return;
			// 约每 10 tick 一粒，数量克制
			if (!Main.rand.NextBool(10))
				return;

			Vector2 origin = Player.Center + Main.rand.NextVector2Circular(Player.width * 0.55f, Player.height * 0.45f);
			Vector2 outward = (origin - Player.Center).SafeNormalize(-Vector2.UnitY);
			// 轻微外散 + 主导向上；|vel|≈0.9、无重力短火花 ≈ 1 格行程
			Vector2 vel = outward * Main.rand.NextFloat(0.2f, 0.45f)
				+ new Vector2(Main.rand.NextFloat(-0.15f, 0.15f), Main.rand.NextFloat(-1.05f, -0.7f));
			Dust d = Dust.NewDustPerfect(
				origin,
				DustID.GoldCoin,
				vel,
				100,
				new Color(255, 220, 90),
				Main.rand.NextFloat(0.7f, 1.05f));
			d.noGravity = true;
			d.fadeIn = 0.4f;
			d.velocity = vel;
		}

		private void TryProcessUltimateKey()
		{
			if (Player.whoAmI != Main.myPlayer || IsRemotePlayerOnClient)
				return;
			if (HenshinKeybinds.Ultimate == null || !HenshinKeybinds.Ultimate.JustPressed)
				return;
			if (ChoiceLockUlt)
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
			if (RockyHelmetScale > 0f && RockyHelmetCooldown <= 0)
			{
				int cd = RockyHelmetCdTicks < float.MaxValue / 4f ? (int)RockyHelmetCdTicks : 45;
				RockyHelmetCooldown = Math.Max(1, cd);
				int baseDmg = Math.Max(1, Player.GetWeaponDamage(Player.HeldItem));
				int dmg = Math.Max(1, (int)Math.Round(baseDmg * RockyHelmetScale));
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (!n.active || n.friendly || n.life <= 0)
						continue;
					if (n.Distance(Player.Center) > 6f * 16f)
						continue;
					n.SimpleStrikeNPC(dmg, n.Center.X < Player.Center.X ? -1 : 1, false, 2f, ModContent.GetInstance<Damage.HenshinDamage>());
				}
			}
			if (AccGuardCutTicks > 0)
				AccGuardActiveTimer = Math.Max(AccGuardActiveTimer, AccGuardCutTicks);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			TrySpawnSuperCritCombatText(target, damageDone);
			ProcessHenshinNpcHit(target, damageDone, proj: null);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!CountsAsHenshinMoveHit(proj))
				return;
			TrySpawnSuperCritCombatText(target, damageDone);
			ProcessHenshinNpcHit(target, damageDone, proj);
		}

		private void TrySpawnSuperCritCombatText(NPC target, int damageDone)
		{
			if (!_superCritPending)
				return;
			_superCritPending = false;
			if (Main.netMode == NetmodeID.Server || damageDone <= 0 || target == null)
				return;

			int id = CombatText.NewText(target.Hitbox, SuperCritCombatColor, damageDone, dramatic: true);
			if (id < 0 || id >= Main.combatText.Length)
				return;
			CombatText text = Main.combatText[id];
			text.color = SuperCritCombatColor;
			text.scale = SuperCritCombatScale;
			text.crit = true;
		}

		/// <summary>优先读弹上钉死的出弹槽；近战无弹或未钉槽时回退 LastMoveSlot。</summary>
		private MoveSlot ResolveHitMoveSlot(Projectile proj)
		{
			if (proj != null)
			{
				if (proj.ModProjectile is IHenshinMoveProj tagged && tagged.HasSourceMoveSlot)
					return tagged.SourceMoveSlot;
				HenshinAccGlobalProjectile gp = proj.GetGlobalProjectile<HenshinAccGlobalProjectile>();
				if (gp.HasSourceMoveSlot)
					return gp.SourceMoveSlot;
			}
			return LastMoveSlot;
		}

		private static bool TryGetStampedCombatEnergyFactor(Projectile proj, out float factor)
		{
			factor = 0f;
			if (proj == null)
				return false;
			HenshinAccGlobalProjectile gp = proj.GetGlobalProjectile<HenshinAccGlobalProjectile>();
			if (!gp.HasCombatEnergyFactor)
				return false;
			factor = gp.CombatEnergyFactor;
			return true;
		}

		private void ProcessHenshinNpcHit(NPC target, int damageDone, Projectile proj)
		{
			if (!IsTransformed)
				return;
			if (target.boss)
				NotifyBossEngage();

			MoveSlot slot = ResolveHitMoveSlot(proj);
			HenshinForceItem force = Player.HeldItem?.ModItem as HenshinForceItem;
			MoveSpec move = force?.GetMove(slot);
			float factor;
			if (TryGetStampedCombatEnergyFactor(proj, out float stamped))
				factor = stamped;
			else
				factor = slot == MoveSlot.Ultimate ? 0f : (move?.GetEnergyGainFactor() ?? 1f);

			// 优先信钉死的 CombatEnergyFactor（大招=0）；槽位 Ultimate 再兜底。
			bool grantsCombatEnergy = factor > 0f && slot != MoveSlot.Ultimate;
			if (grantsCombatEnergy)
			{
				bool fragment = HenshinNebulaShardTintGlobal.UsesCrumbHitEnergy(proj);
				AddCombatEnergy(HenshinStatService.CombatHitEnergy(factor, fragment));
			}

			if (target.life <= 0)
			{
				if (ShouldGrantKillRewards(target) && TryClaimKillXp(target))
				{
					if (grantsCombatEnergy)
						AddCombatEnergy(EnergyOnKill * factor);
					if (force != null)
						GrantKillExperience(force, target);
					if (CurrentForm?.Passive == FormPassiveKind.Moxie && MoxieStacks < 2)
						MoxieStacks++;
				}
			}

			ApplyOnHitAccessories(damageDone);
		}

		private static bool ShouldGrantKillRewards(NPC npc)
		{
			if (npc == null)
				return false;
			if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
			{
				NPC real = Main.npc[npc.realLife];
				if (real != null && real.active && real.life > 0)
					return false;
			}

			return true;
		}

		private static int KillXpKey(NPC npc)
		{
			if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
				return npc.realLife;
			return npc.whoAmI;
		}

		private bool TryClaimKillXp(NPC npc)
		{
			int key = KillXpKey(npc);
			return _killXpGranted.Add(key);
		}

		private void CleanupKillXp()
		{
			if (_killXpGranted.Count == 0)
				return;
			_killXpGranted.RemoveWhere(id =>
				id < 0 || id >= Main.maxNPCs || !Main.npc[id].active || Main.npc[id].life > 0);
		}

		private static bool IsBossForXp(NPC npc)
		{
			if (npc.boss)
				return true;
			if (NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
				return true;
			return false;
		}

		private void GrantKillExperience(HenshinForceItem force, NPC target)
		{
			int world = ProgressStageService.GetProgressStage();
			int amount;
			bool boss = IsBossForXp(target);
			if (boss)
			{
				int? over = HenshinBossXpOverrides.TryGet(target);
				amount = HenshinStatService.ScaleWorldXp(
					HenshinStatService.ComputeBossXp(target.lifeMax, target.defense, world, over),
					world);
			}
			else
			{
				int lo = HenshinStatService.MinionXpMin(world);
				int hi = HenshinStatService.MinionXpMax(world);
				amount = Main.rand.Next(lo, hi + 1);
			}

			Vector2 popupAt = target.Center;
			int held = Math.Max(0, (int)Math.Round(amount * (1f + XpHeldMul)));
			force.TryAddExperience(Player, held, out int levelsGained, out _, out int applied);
			if (XpHotbarShareMul > 0f)
			{
				int share = Math.Max(0, (int)Math.Round(amount * XpHotbarShareMul));
				if (share > 0)
				{
					for (int i = 0; i < HotbarSize; i++)
					{
						if (i == Player.selectedItem)
							continue;
						if (Player.inventory[i]?.ModItem is HenshinForceItem other)
							other.TryAddExperience(Player, share, out _, out _, out _);
					}
				}
			}
			if (Main.netMode != NetmodeID.SinglePlayer)
				HenshinNet.SendEnergy(this);

			if (Player.whoAmI != Main.myPlayer)
				return;

			if (applied > 0)
				HenshinXpPopupSystem.ShowExp(popupAt, applied, boss);
			if (levelsGained > 0)
				HenshinXpPopupSystem.ShowLevelUps(Player, levelsGained);
		}

		/// <summary>持握 + 本模招式（含本模弹幕 / HenshinDamage）。原版壳碎片若未改 DamageType 则不计。</summary>
		private static bool CountsAsHenshinMoveHit(Projectile proj)
		{
			if (proj == null)
				return false;
			if (proj.DamageType == HenshinDamage.Instance)
				return true;
			if (proj.ModProjectile is IHenshinMoveProj)
				return true;
			return proj.ModProjectile?.Mod == PokemonHenshinMod.Instance;
		}

		private void ApplyOnHitAccessories(int damageDone)
		{
			if (LifeOrbHpDrain && LifeOrbGate <= 0 && Player.statLife > 1)
			{
				int gate = LifeOrbGateTicks < float.MaxValue / 4f ? (int)LifeOrbGateTicks : 8;
				LifeOrbGate = Math.Max(1, gate);
				Player.statLife = Math.Max(1, Player.statLife - 1);
				Player.HealEffect(-1, true);
			}
			if (SolarPowerDrain && Player.statLife > 1 && LifeOrbGate <= 0)
			{
				Player.statLife = Math.Max(1, Player.statLife - 1);
			}
			if (ShellBellHeal > 0f && ShellBellCooldown <= 0 && damageDone > 0)
			{
				int cd = ShellBellCdTicks < float.MaxValue / 4f ? (int)ShellBellCdTicks : 30;
				ShellBellCooldown = Math.Max(1, cd);
				int heal = Math.Max(1, (int)Math.Round(ShellBellHeal));
				Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + heal);
				Player.HealEffect(heal);
			}
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (IncomingDamageMultiplier != 1f)
				modifiers.FinalDamage *= IncomingDamageMultiplier;

			if (!IsTransformed || !FocusSash || FocusSashCooldown > 0)
				return;
			int need = (int)Math.Ceiling(Player.statLifeMax2 * FocusSashHpPct);
			if (Player.statLife < need)
				return;
			modifiers.ModifyHurtInfo += ApplyFocusSashHurt;
		}

		private void ApplyFocusSashHurt(ref Player.HurtInfo info)
		{
			if (!FocusSash || FocusSashCooldown > 0 || !IsTransformed)
				return;
			int need = (int)Math.Ceiling(Player.statLifeMax2 * FocusSashHpPct);
			if (Player.statLife < need)
				return;
			if (Player.statLife - info.Damage > 0)
				return;
			int cap = Math.Max(1, Player.statLife - 1);
			if (info.Damage > cap)
				info.Damage = cap;
			FocusSashCooldown = Math.Max(1, (int)Math.Round(FocusSashCdSec * 60f));
			if (FocusSashImmuneTicks > 0f)
				Player.immuneTime = Math.Max(Player.immuneTime, (int)FocusSashImmuneTicks);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			ModifyHenshinHit(target, null, ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			ModifyHenshinHit(target, proj, ref modifiers);
		}

		/// <summary>EasyCrit 招式：抬高本模伤害类暴击率，交给原版一次判定。</summary>
		public override void ModifyWeaponCrit(Item item, ref float crit)
		{
			if (!IsTransformed || item?.ModItem is not HenshinForceItem force)
				return;
			MoveSpec move = force.GetMove(LastMoveSlot);
			if (move?.EasyCrit == true)
				crit += 35f;
		}

		private void ModifyHenshinHit(NPC target, Projectile proj, ref NPC.HitModifiers modifiers)
		{
			if (!IsTransformed)
				return;

			if (proj != null)
			{
				if (!CountsAsHenshinMoveHit(proj))
					return;
			}
			else if (Player.HeldItem?.ModItem is not HenshinForceItem)
			{
				return;
			}

			MoveSlot slot = ResolveHitMoveSlot(proj);
			HenshinForceItem force = Player.HeldItem?.ModItem as HenshinForceItem;
			MoveSpec move = force?.GetMove(slot);
			MoveDelivery delivery = MoveDelivery.None;
			if (proj?.ModProjectile is IHenshinMoveProj tagged)
				delivery = tagged.Delivery;
			else if (move != null)
				delivery = move.Delivery;

			float mult = 1f;
			if (BossDamageBonus > 0f && target.boss)
				mult += BossDamageBonus;
			if (OnFireTargetBonus > 0f && target.onFire)
				mult += OnFireTargetBonus;
			if (TypeMoveBonus > 0f)
				mult += TypeMoveBonus;
			if (slot == MoveSlot.Ultimate)
				mult += UltDamageBonus;
			if (FireMoveDamage > 0f && move?.CountsAsFireMove == true)
				mult += FireMoveDamage;
			if (MeleeDeliveryDamage > 0f && MoveDeliverySets.MeleeShort(delivery))
				mult += MeleeDeliveryDamage;
			if (PsychicDragonDamage > 0f && CurrentForm != null
				&& (CurrentForm.Primary == PokemonType.Psychic || CurrentForm.Secondary == PokemonType.Psychic
					|| CurrentForm.Primary == PokemonType.Dragon || CurrentForm.Secondary == PokemonType.Dragon))
				mult += PsychicDragonDamage;
			if (EvioliteDamage > 0f && CurrentForm != null && FormRegistry.FindEvolutionOf(CurrentForm.FormId) != null)
				mult += EvioliteDamage;
			mult *= AftermathPenaltyMult;
			if (mult != 1f)
				modifiers.FinalDamage *= mult;

			ApplyCritTier(target, ref modifiers);
		}

		/// <summary>
		/// 基底暴击走原版 Crit；A15/着火升档仅在已 Crit 时再 ×2（合计约 ×4），并刷超暴击飘字。
		/// </summary>
		private void ApplyCritTier(NPC target, ref NPC.HitModifiers modifiers)
		{
			float upgradeChance = CritUpgradeChance;
			if (OnFireCritUpgrade > 0f && target.onFire)
				upgradeChance += OnFireCritUpgrade;
			if (upgradeChance <= 0f)
				return;

			modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
			{
				if (!info.Crit)
					return;
				if (Main.rand.NextFloat() >= upgradeChance)
					return;

				long doubled = (long)info.Damage * 2L;
				info.Damage = (int)Math.Clamp(doubled, 1L, int.MaxValue);
				info.HideCombatText = true;
				_superCritPending = true;
			};
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
			// 世界：只留 Overlay。地图头像：只留 IsHeadLayer 的缩小形态图（否则藏掉 Head 后 RT 全透明）。
			PlayerDrawLayer keep = drawInfo.headOnlyRender
				? ModContent.GetInstance<HenshinMapHeadLayer>()
				: ModContent.GetInstance<HenshinOverlayLayer>();
			foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
			{
				if (!ReferenceEquals(layer, keep))
					layer.Hide();
			}
		}

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
		{
			HenshinNet.SendForm(this, toWho, fromWho);
			HenshinNet.SendPhasing(this, toWho, fromWho);
			HenshinNet.SendEnergy(this, toWho, fromWho);
			if (IsTransformed)
				HenshinNet.SendAim(this, toWho, fromWho);
		}

		public override void CopyClientState(ModPlayer targetCopy)
		{
			var t = (HenshinPlayer)targetCopy;
			t.lastSyncedNetId = CurrentFormNetId;
			t.lastSyncedEnergy = UltimateEnergy;
			ReadHeldProgress(out int lv, out int xp);
			t.lastSyncedLevel = lv;
			t.lastSyncedXp = xp;
		}

		public override void SendClientChanges(ModPlayer clientPlayer)
		{
			var c = (HenshinPlayer)clientPlayer;
			if (c.lastSyncedNetId != CurrentFormNetId)
				HenshinNet.SendForm(this, -1, Player.whoAmI);
			ReadHeldProgress(out int lv, out int xp);
			if (System.Math.Abs(c.lastSyncedEnergy - UltimateEnergy) > 1f || c.lastSyncedLevel != lv || c.lastSyncedXp != xp)
				HenshinNet.SendEnergy(this);
		}

		private void ReadHeldProgress(out int level, out int xp)
		{
			level = 0;
			xp = 0;
			if (Player.HeldItem?.ModItem is HenshinForceItem force)
			{
				level = force.Level;
				xp = force.Xp;
			}
		}
	}
}
