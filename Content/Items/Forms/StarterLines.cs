using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Core;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Items.Forms
{
	internal static class FormItemUtil
	{
		public static FormDefinition Def(
			string formId, ushort netId, string displayKey, PokemonType primary,
			int stage, string evolvesFrom, FormPassiveKind passive,
			FormRole role = FormRole.Combat,
			PokemonType secondary = PokemonType.None, bool phasing = false, float dmgFactor = 1f,
			string acquireKey = null)
		{
			FormStatTable.Mods mods = FormStatTable.Get(formId);
			return new()
			{
				FormId = formId,
				NetworkId = netId,
				DisplayNameKey = displayKey,
				TexturePath = "PokemonHenshin/Assets/Forms/" + formId,
				TextureFacesLeft = true,
				Primary = primary,
				Secondary = secondary,
				Stage = stage,
				EvolvesFrom = evolvesFrom,
				Collision = phasing ? CollisionTier.C : CollisionTier.A,
				GrantsPhasing = phasing,
				HenshinDamageFactor = dmgFactor,
				AttackMod = mods.AttackMod,
				DefenseMod = mods.DefenseMod,
				EnergyMax = HenshinStatService.EnergyMaxDefault,
				Role = role,
				Passive = passive,
				AcquireHintKey = acquireKey ?? ("Mods.PokemonHenshin.Acquire." + formId)
			};
		}

		public static MoveSpec Slash(string nameKey, float mult = 1f, int use = 18, int dust = DustID.Smoke, bool easyCrit = false)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<GenericSlashProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				Ai0 = dust,
				EasyCrit = easyCrit,
				KeyConflict = KeyConflictLevel.None,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec FireBolt(string nameKey, float mult = 1.3f, int use = 28)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<EmberBoltProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 9f,
				Knockback = 1.5f,
				IsRangedProjectile = true,
				CountsAsFireMove = true,
				Ai1 = BuffID.OnFire,
				KeyConflict = KeyConflictLevel.None,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec WaterBolt(string nameKey, float mult = 1.2f, int use = 26, int slowTicks = 0)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<WaterBoltHenshinProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 10f,
				Knockback = 2f,
				IsRangedProjectile = true,
				CountsAsWaterMove = true,
				Ai2 = slowTicks,
				KeyConflict = KeyConflictLevel.None,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec Bolt(string nameKey, int projType, float mult, int use, float speed,
			KeyConflictLevel key = KeyConflictLevel.None, float ai0 = 0, float ai1 = 0, float ai2 = 0,
			bool ranged = true, bool easyCrit = false)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = projType,
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = speed,
				Knockback = 2f,
				Ai0 = ai0,
				Ai1 = ai1,
				Ai2 = ai2,
				IsRangedProjectile = ranged && speed > 0f,
				EasyCrit = easyCrit,
				KeyConflict = key,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec Lunge(string nameKey, float mult, int use, int dust, bool recoil = false, float recoilFrac = 0.08f, int onHitBuff = 0, KeyConflictLevel key = KeyConflictLevel.None)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LungeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 4f,
				Ai0 = dust,
				Ai1 = onHitBuff,
				RecoilSelf = recoil,
				RecoilFraction = recoilFrac,
				RequiresLungeCooldown = true,
				KeyConflict = key
			};

		public static MoveSpec Vortex(string nameKey, float mult, int dust, int buff = 0)
			=> new()
			{
				Delivery = MoveDelivery.DoTBind,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<VortexBindProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 8f,
				Knockback = 1f,
				Ai0 = dust,
				Ai1 = buff,
				IsRangedProjectile = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec Strike(string nameKey, float mult, int dust = DustID.Electric)
			=> new()
			{
				Delivery = MoveDelivery.StrikeFall,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StrikeFallProj>(),
				DamageMultiplier = mult,
				UseTime = 45,
				ShootSpeed = 0f,
				Knockback = 3f,
				Ai1 = dust,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec Beam(string nameKey, float mult, bool aftermath = false, bool ignoreDef = false)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BeamBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 50,
				ShootSpeed = 16f,
				Knockback = 2f,
				IsRangedProjectile = true,
				IgnoreDefensePartial = ignoreDef,
				AftermathDamagePenaltyTicks = aftermath ? 180 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				SelfStunTicks = aftermath ? 60 : 0,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec AoE(string nameKey, float mult, int dust)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AoEBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 5f,
				Ai0 = dust,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec Dig(string nameKey, float mult = 1f, int use = 20)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DigLungeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				RequiresLungeCooldown = true,
				KeyConflict = KeyConflictLevel.RightClick,
				NetRisk = NetRisk.High
			};

		public static MoveSpec Sleep(string nameKey)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SleepWaveProj>(),
				DamageMultiplier = 0.3f,
				UseTime = 50,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec Field(string nameKey, WeatherField.WeatherTag tag)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<RainFieldProj>(),
				DamageMultiplier = 0.2f,
				UseTime = 55,
				ShootSpeed = 0f,
				Ai0 = (byte)tag,
				KeyConflict = KeyConflictLevel.ModKeybind,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec Scratch(string nameKey, float mult = 1f, int use = 18, float reachTiles = 3.5f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ScratchSlashProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				Ai0 = reachTiles,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec FlareUlt(string nameKey, float mult = 2.6f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FlareBoltUltProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 10f,
				Knockback = 3f,
				SpawnAtMouse = false,
				IsRangedProjectile = true,
				CountsAsFireMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec AquaGun(string nameKey, float mult = 1.25f, int use = 22)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AquaScepterShotProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 14f,
				Knockback = 2f,
				IsRangedProjectile = true,
				CountsAsWaterMove = true
			};

		public static MoveSpec BubbleBarrageUlt(string nameKey, float mult = 2.5f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BarrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				Knockback = 1.5f,
				Ai0 = BarrageDirectorProj.ModeBubble,
				Ai1 = 64,
				Ai2 = 18f,
				CountsAsWaterMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec SeedBarrageUlt(string nameKey, float mult = 2.5f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BarrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				Knockback = 1.2f,
				Ai0 = BarrageDirectorProj.ModeSeed,
				Ai1 = 64,
				Ai2 = 17f,
				EasyCrit = true,
				CountsAsGrassMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec VineWhip(string nameKey, float mult = 1.15f, int use = 20)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<GrassWhipProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 2.5f,
				CountsAsGrassMove = true
			};

		public static MoveSpec BlinkStrike(string nameKey, float mult = 1.15f, int use = 14)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BlinkStrikeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				NetRisk = NetRisk.Medium,
				RequiresLungeCooldown = true
			};

		public static MoveSpec AerialAceBlink(string nameKey, float mult = 1.6f, int use = 16)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AerialAceBlinkProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				NetRisk = NetRisk.Medium,
				RequiresLungeCooldown = true
			};

		public static MoveSpec WeatherPainHurricane(string nameKey, float mult = 1.6f, bool ult = false)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<WeatherPainHurricaneProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 40 : 28,
				ShootSpeed = ult ? 15f : 14f,
				Knockback = 3f,
				Ai2 = ult ? 1f : 0f,
				IsRangedProjectile = true,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None,
				BalanceTag = ult ? BalanceTag.Ultimate : BalanceTag.WideAoE
			};

		public static MoveSpec BraveBirdUlt(string nameKey, float mult = 3.8f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BraveBirdLungeProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 4f,
				RecoilSelf = true,
				RecoilFraction = 0.25f,
				RequiresLungeCooldown = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec ThunderboltUlt(string nameKey, float mult = 4.8f)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SkyBoltLightningProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 2f,
				KeyConflict = KeyConflictLevel.ModKeybind,
				BalanceTag = BalanceTag.Ultimate
			};

		public static MoveSpec RockTomb(string nameKey, float mult = 1.3f, int use = 28)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<RockTombDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 4f,
				SpawnAtMouse = true,
				Ai2 = 60
			};

		public static MoveSpec CrossChopUlt(string nameKey, float mult = 2.6f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CrossChopArcProj>(),
				DamageMultiplier = mult,
				UseTime = 30,
				ShootSpeed = 0f,
				Knockback = 4f,
				EasyCrit = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec GroundCyclone(string nameKey, float mult = 1.25f, int use = 24)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<GroundCycloneProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 11f,
				Knockback = 2.5f,
				IsRangedProjectile = true,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec PeckCone(string nameKey, float mult = 1.2f, int use = 18)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<PeckConeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				BalanceTag = BalanceTag.MultiHit
			};

		public static MoveSpec ZenHammer(string nameKey, float mult = 2.0f, int use = 28)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ZenHammerSmashProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 5f,
				SpawnAtMouse = true
			};

		public static MoveSpec ResonanceScatterUlt(string nameKey, float mult = 2f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ResonanceScatterProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				Knockback = 2f,
				SpawnAtMouse = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec BubbleBarrage(string nameKey, int count, float mult, int use = 36)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BarrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 1.2f,
				Ai0 = BarrageDirectorProj.ModeBubble,
				Ai1 = count,
				Ai2 = 16f,
				CountsAsWaterMove = true,
				BalanceTag = BalanceTag.MultiHit
			};

		public static MoveSpec ThickBeam(string nameKey, float mult, int dust, bool aftermath = false, int selfStun = 0, int onHitBuff = 0, bool ignoreDef = false, bool ult = true, int beamMode = 0)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ThickBeamProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 42 : 24,
				ShootSpeed = 17f,
				Knockback = 2.5f,
				Ai0 = dust,
				Ai1 = onHitBuff,
				Ai2 = beamMode,
				IsRangedProjectile = true,
				IgnoreDefensePartial = ignoreDef,
				AftermathDamagePenaltyTicks = aftermath ? 180 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				SelfStunTicks = selfStun > 0 ? selfStun : (aftermath ? 90 : 0),
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		/// <summary>持续瞄准光束：破坏光线等。ShootSpeed=0，AI 跟鼠标。龙之怒请用 <see cref="DragonRage"/>。</summary>
		public static MoveSpec SustainedBeam(string nameKey, float mult, int dust, int beamMode, bool aftermath = false, int selfStun = 0, bool ignoreDef = false, bool ult = true)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SustainedBeamProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 48 : 28,
				ShootSpeed = 0f,
				Knockback = 2.5f,
				Ai0 = dust,
				Ai2 = beamMode,
				IsRangedProjectile = true,
				IgnoreDefensePartial = ignoreDef,
				AftermathDamagePenaltyTicks = aftermath ? 180 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				SelfStunTicks = selfStun > 0 ? selfStun : (aftermath ? 120 : 0),
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		/// <summary>龙之怒：技能 12 / 大招 32 发抖动球体，命中 5 格爆。ai2：0 技能 / 1 大招。</summary>
		public static MoveSpec DragonRage(string nameKey, float mult, bool ult = true)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DragonRageBarrageProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 48 : 28,
				ShootSpeed = 0f,
				Knockback = 2.2f,
				Ai0 = DustID.DungeonWater,
				Ai2 = ult ? DragonRageBarrageProj.ModeUlt : DragonRageBarrageProj.ModeSkill,
				IsRangedProjectile = true,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None,
				BalanceTag = ult ? BalanceTag.Ultimate : BalanceTag.MultiHit
			};

		/// <summary>水柱：ai2=0 水炮（不穿透、命中渐缩）；ai2=1 加农水炮（穿透+每3击爆）。</summary>
		public static MoveSpec WaterJet(string nameKey, float mult, bool cannon = false, bool aftermath = false, int selfStun = 0, bool ult = false)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<WaterJetProj>(),
				DamageMultiplier = mult,
				UseTime = ult || cannon ? 48 : 26,
				ShootSpeed = 0f,
				Knockback = 2.2f,
				Ai0 = DustID.Water,
				Ai2 = cannon ? WaterJetProj.ModeCannon : WaterJetProj.ModePump,
				IsRangedProjectile = true,
				CountsAsWaterMove = true,
				AftermathDamagePenaltyTicks = aftermath ? 180 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				SelfStunTicks = selfStun > 0 ? selfStun : (aftermath ? 90 : 0),
				KeyConflict = ult || cannon ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		public static MoveSpec MouseAoE(string nameKey, float mult, int dust, int buff = 0, bool aftermath = false, bool ignoreDef = false)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MouseAoEBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				Knockback = 5f,
				Ai0 = dust,
				Ai1 = buff,
				SpawnAtMouse = true,
				IgnoreDefensePartial = ignoreDef,
				AftermathDamagePenaltyTicks = aftermath ? 300 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				KeyConflict = KeyConflictLevel.ModKeybind,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec ChargeBeamUlt(string nameKey, float mult = 4f)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ChargeBeamDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 55,
				ShootSpeed = 0f,
				Ai0 = DustID.GoldFlame,
				CountsAsGrassMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec RockSlideX(string nameKey, int count, float mult, bool ult = false)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<RockSlideDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 40 : 28,
				ShootSpeed = 0f,
				Knockback = 4f,
				SpawnAtMouse = true,
				Ai1 = count,
				Ai2 = ult ? 192f : 80f,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None,
				BalanceTag = ult ? BalanceTag.Ultimate : BalanceTag.MultiHit
			};

		public static MoveSpec MeteorBarrageUlt(string nameKey, int count = 8, float mult = 4.2f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DracoMeteorDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				Ai1 = count, // 导演内部固定 64；保留 Ai1 兼容
				AftermathDamagePenaltyTicks = 300,
				AftermathDamagePenalty = 0.85f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FlameCone(string nameKey, float mult = 1.55f, int count = 10)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FlameConeDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 26,
				ShootSpeed = 0f,
				Ai1 = count,
				CountsAsFireMove = true,
				BalanceTag = BalanceTag.MultiHit
			};

		public static MoveSpec LeafSpread(string nameKey, float mult = 1.3f)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LeafSpreadProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				CountsAsGrassMove = true,
				BalanceTag = BalanceTag.MultiHit
			};

		public static MoveSpec SeedBombUlt(string nameKey, float mult = 3.3f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SeedBombProj>(),
				DamageMultiplier = mult,
				UseTime = 32,
				ShootSpeed = 10f,
				IsRangedProjectile = true,
				EasyCrit = true,
				CountsAsGrassMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec HurricaneField(string nameKey, float mult, bool ult = false)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<HurricaneFieldProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 40 : 28,
				ShootSpeed = 0f,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None,
				BalanceTag = ult ? BalanceTag.Ultimate : BalanceTag.WideAoE
			};

		public static MoveSpec ThunderPillarUlt(string nameKey, float mult = 3.8f)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SkyBoltRainUltProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec CloseCombatUlt(string nameKey, float mult = 4f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CloseCombatFuryProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				AftermathDamagePenaltyTicks = 300,
				AftermathDamagePenalty = 0.8f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec OutrageUlt(string nameKey, float mult = 4.2f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<OutrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 190,
				ShootSpeed = 0f,
				AftermathDamagePenaltyTicks = 120,
				AftermathDamagePenalty = 0.7f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FutureSightUlt(string nameKey, float mult = 3.8f)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FutureSightMarkDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 50,
				ShootSpeed = 0f,
				IgnoreDefensePartial = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec DarkPulseCone(string nameKey, float mult = 3.6f)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DarkPulseBarrageProj>(),
				DamageMultiplier = mult,
				UseTime = 30,
				ShootSpeed = 0f,
				EasyCrit = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FlailUlt(string nameKey, float mult = 1.5f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FlailBarrageProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec BiteArc(string nameKey, float mult = 1.25f, int brokenArmorTicks = 0, float size = 1f, int onFireTicks = 0)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BiteArcProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				Knockback = 3.5f,
				Ai0 = size,
				Ai1 = brokenArmorTicks,
				Ai2 = onFireTicks
			};

		/// <summary>龙之波动：10 枚星云奥秘同款弹（70%、不穿透、不追踪、碰撞爆炸）。</summary>
		public static MoveSpec DragonPulse(string nameKey, float mult = 1.45f)
			=> new()
			{
				Delivery = MoveDelivery.Beam,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<NebulaPulseDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 3f,
				IsRangedProjectile = true
			};

		public static MoveSpec DragonBreath(string nameKey, float mult = 1.25f)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DragonBreathConeProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f
			};

		public static MoveSpec PetalDance(string nameKey, float mult = 1.5f)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<PetalDanceFieldProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				CountsAsGrassMove = true
			};

		public static MoveSpec MudSlap(string nameKey, float mult = 1f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MudSlapBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 16,
				ShootSpeed = 10f,
				IsRangedProjectile = true
			};

		public static MoveSpec CrossChopShort(string nameKey, float mult = 1.6f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CrossChopArcXProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				EasyCrit = true
			};

		public static MoveSpec TripleStab(string nameKey, float mult = 1.3f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<TripleStabProj>(),
				DamageMultiplier = mult,
				UseTime = 20,
				ShootSpeed = 0f,
				BalanceTag = BalanceTag.MultiHit
			};

		public static MoveSpec BrickBreak(string nameKey, float mult = 1.4f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BrickBreakProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f
			};

		public static MoveSpec MouseVortex(string nameKey, float mult, int dust, int buff = 0)
			=> new()
			{
				Delivery = MoveDelivery.DoTBind,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MouseVortexProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				Ai0 = dust,
				Ai1 = buff,
				SpawnAtMouse = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec DynamicPunchUlt(string nameKey, float mult = 3.4f, bool ult = true)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DynamicPunchProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 32 : 36,
				ShootSpeed = 0f,
				Ai0 = ult ? 20f : 8f,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.RightClick
			};

		public static MoveSpec MidThunder(string nameKey, float mult = 1.5f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MidThunderBeamProj>(),
				DamageMultiplier = mult,
				UseTime = 20,
				ShootSpeed = 14f,
				IsRangedProjectile = true
			};

		public static MoveSpec BigShadowBall(string nameKey, float mult = 1.35f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BigShadowBallProj>(),
				DamageMultiplier = mult,
				UseTime = 24,
				ShootSpeed = 9f,
				IsRangedProjectile = true
			};

		public static MoveSpec SludgeBolt(string nameKey, float mult = 1.45f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SludgeBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 24,
				ShootSpeed = 9f,
				IsRangedProjectile = true
			};

		public static MoveSpec HypnosisUlt(string nameKey)
			=> new()
			{
				Delivery = MoveDelivery.Field,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<HypnosisWaveProj>(),
				DamageMultiplier = 2.6f,
				UseTime = 50,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec LickFan(string nameKey, float mult = 0.8f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LickTongueProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f
			};

		public static MoveSpec AirBurst(string nameKey, float mult = 1.9f)
			=> new()
			{
				Delivery = MoveDelivery.Spread,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AirBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				EasyCrit = true,
				BalanceTag = BalanceTag.WideAoE
			};

		public static MoveSpec StoneEdge(string nameKey, float mult = 1.7f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StoneEdgeDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				EasyCrit = true
			};

		public static MoveSpec DragonTailWhip(string nameKey, float mult = 1.35f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DragonTailWhipProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				Ai0 = DustID.Cloud
			};

		public static MoveSpec IronTailWhip(string nameKey, float mult = 1.6f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DragonTailWhipProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				Ai0 = DustID.Iron,
				Ai1 = 1f // 铁色遮罩
			};

		public static MoveSpec TakeDownUlt(string nameKey, float mult = 3.2f, float reachTiles = 28f, float recoilFrac = 0.25f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<TakeDownLungeProj>(),
				DamageMultiplier = mult,
				UseTime = 32,
				ShootSpeed = 0f,
				Knockback = 4f,
				Ai0 = DustID.Iron,
				Ai2 = reachTiles,
				RecoilSelf = true,
				RecoilFraction = recoilFrac,
				RequiresLungeCooldown = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec ShadowClawSlash(string nameKey, float mult = 1.55f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ShadowClawSlashProj>(),
				DamageMultiplier = mult,
				UseTime = 16,
				ShootSpeed = 0f,
				Knockback = 3f,
				EasyCrit = true
			};

		public static MoveSpec CometPunch(string nameKey, float mult = 1.8f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CometPunchProj>(),
				DamageMultiplier = mult,
				UseTime = 28,
				ShootSpeed = 0f
			};

		public static MoveSpec DragonDive(string nameKey, float mult = 1.8f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StardustPathLungeProj>(),
				DamageMultiplier = mult,
				UseTime = 28,
				ShootSpeed = 0f,
				Knockback = 4f,
				Ai2 = 0f,
				RequiresLungeCooldown = true
			};

		public static MoveSpec DragonAscentUlt(string nameKey, float mult = 5.0f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StardustPathLungeProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 5f,
				Ai2 = 1f,
				RequiresLungeCooldown = true,
				IgnoreDefensePartial = true,
				AftermathDamagePenaltyTicks = 300,
				AftermathDamagePenalty = 0.8f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec SkyAttack(string nameKey, float mult = 1.8f)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<SkyAttackLungeProj>(),
				DamageMultiplier = mult,
				UseTime = 34,
				ShootSpeed = 0f,
				Knockback = 4f,
				RequiresLungeCooldown = true
			};

		public static MoveSpec AlakazamPsychic(string nameKey, float mult = 1.7f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AlakazamPsychicDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 28,
				ShootSpeed = 0f,
				SpawnAtMouse = true
			};

		public static MoveSpec MewtwoPsychic(string nameKey, float mult = 1.8f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AlakazamPsychicDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 28,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				Ai0 = 6f,
				Ai1 = 1f // 穿墙
			};

		public static MoveSpec MewtwoPsystrikeUlt(string nameKey, float mult = 4.8f)
			=> new()
			{
				Delivery = MoveDelivery.Barrage,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MewtwoPsystrikeDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				IgnoreDefensePartial = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FocusPunch(string nameKey, float mult = 1.8f)
			=> new()
			{
				Delivery = MoveDelivery.MeleeArc,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FocusPunchProj>(),
				DamageMultiplier = mult,
				UseTime = 28,
				ShootSpeed = 0f
			};

		public static MoveSpec DigUlt(string nameKey, float mult = 3f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DigUltBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind,
				NetRisk = NetRisk.High
			};

		public static MoveSpec QuakeUlt(string nameKey, float mult = 3.6f)
			=> new()
			{
				Delivery = MoveDelivery.AoEBurst,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<QuakeWaveProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind,
				NetRisk = NetRisk.High
			};

		public static MoveSpec StrongPsychic(string nameKey, float mult = 1.7f)
			=> new()
			{
				Delivery = MoveDelivery.Bolt,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StrongPsychicBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 20,
				ShootSpeed = 10f,
				IsRangedProjectile = true
			};

		public static MoveSpec HyperBeamUlt(string nameKey, float mult = 4.2f)
			=> SustainedBeam(nameKey, mult, DustID.PurpleTorch, SustainedBeamProj.ModeHyperBeam, aftermath: true, selfStun: 120, ult: true);

		public static MoveSpec FlareBlitzUlt(string nameKey, float mult = 3.2f)
			=> new()
			{
				Delivery = MoveDelivery.Lunge,
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LungeProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 4f,
				Ai0 = DustID.Torch,
				Ai1 = BuffID.OnFire,
				Ai2 = 32f, // 突进约 32 格
				RecoilSelf = true,
				RecoilFraction = 0.25f,
				RequiresLungeCooldown = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};
	}

	// —— 御三家 ——
	public class CharmanderForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F01", 1, "Mods.PokemonHenshin.Items.CharmanderForce.DisplayName", PokemonType.Fire, 1, null, FormPassiveKind.Blaze);
		protected override MoveSpec CreateMoveA() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.Ember");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Scratch("Mods.PokemonHenshin.Moves.Scratch");
		protected override MoveSpec CreateUltimate() => FormItemUtil.FlareUlt("Mods.PokemonHenshin.Moves.FireSpin");
	}

	public class CharmeleonForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F02", 2, "Mods.PokemonHenshin.Items.CharmeleonForce.DisplayName", PokemonType.Fire, 4, "L01_F01", FormPassiveKind.Blaze);
		protected override MoveSpec CreateMoveA() => FormItemUtil.DragonPulse("Mods.PokemonHenshin.Moves.DragonPulse", 1.35f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.FireFang", 1.3f, onFireTicks: 180);
		protected override MoveSpec CreateUltimate() => FormItemUtil.FlareBlitzUlt("Mods.PokemonHenshin.Moves.FlareBlitz");
	}

	public class CharizardForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F03", 3, "Mods.PokemonHenshin.Items.CharizardForce.DisplayName", PokemonType.Fire, 7, "L01_F02", FormPassiveKind.SolarPower, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.FlameCone("Mods.PokemonHenshin.Moves.Flamethrower", 1.55f, 10);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Scratch("Mods.PokemonHenshin.Moves.DragonClaw", 1.4f, 16, reachTiles: 20f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseAoE("Mods.PokemonHenshin.Moves.Overheat", 4.0f, DustID.Torch, BuffID.OnFire, aftermath: true);
	}

	public class SquirtleForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F01", 4, "Mods.PokemonHenshin.Items.SquirtleForce.DisplayName", PokemonType.Water, 1, null, FormPassiveKind.Torrent);
		protected override MoveSpec CreateMoveA() => FormItemUtil.AquaGun("Mods.PokemonHenshin.Moves.WaterGun");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.1f, 18, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.BubbleBarrageUlt("Mods.PokemonHenshin.Moves.BubbleBeam");
	}

	public class WartortleForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F02", 5, "Mods.PokemonHenshin.Items.WartortleForce.DisplayName", PokemonType.Water, 4, "L02_F01", FormPassiveKind.Torrent);
		protected override MoveSpec CreateMoveA() => FormItemUtil.BubbleBarrage("Mods.PokemonHenshin.Moves.BubbleBeam", 32, 2.4f, 28);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Bite", 1.25f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseVortex("Mods.PokemonHenshin.Moves.Whirlpool", 3.2f, DustID.Water);
	}

	public class BlastoiseForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F03", 6, "Mods.PokemonHenshin.Items.BlastoiseForce.DisplayName", PokemonType.Water, 7, "L02_F02", FormPassiveKind.RainDish);
		protected override MoveSpec CreateMoveA() => FormItemUtil.WaterJet("Mods.PokemonHenshin.Moves.HydroPump", 1.7f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.SkullBash", 1.6f, 34, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.WaterJet("Mods.PokemonHenshin.Moves.HydroCannon", 4.0f, cannon: true, aftermath: true, selfStun: 90, ult: true);
	}

	public class BulbasaurForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F01", 7, "Mods.PokemonHenshin.Items.BulbasaurForce.DisplayName", PokemonType.Grass, 1, null, FormPassiveKind.Overgrow, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.VineWhip("Mods.PokemonHenshin.Moves.VineWhip");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.1f, 18, DustID.Grass);
		protected override MoveSpec CreateUltimate() => FormItemUtil.SeedBarrageUlt("Mods.PokemonHenshin.Moves.SeedGun");
	}

	public class IvysaurForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F02", 8, "Mods.PokemonHenshin.Items.IvysaurForce.DisplayName", PokemonType.Grass, 4, "L03_F01", FormPassiveKind.Overgrow, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.LeafSpread("Mods.PokemonHenshin.Moves.RazorLeaf", 1.3f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Bite", 1.25f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.SeedBombUlt("Mods.PokemonHenshin.Moves.SeedBomb", 3.3f);
	}

	public class VenusaurForce : HenshinForceItem
	{
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F03", 9, "Mods.PokemonHenshin.Items.VenusaurForce.DisplayName", PokemonType.Grass, 7, "L03_F02", FormPassiveKind.Chlorophyll, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.SludgeBolt("Mods.PokemonHenshin.Moves.SludgeBomb", 1.45f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.PetalDance("Mods.PokemonHenshin.Moves.PetalDance", 1.5f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.ChargeBeamUlt("Mods.PokemonHenshin.Moves.SolarBeam", 4.0f);
	}
}
