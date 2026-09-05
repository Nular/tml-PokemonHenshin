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
			=> new()
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
				Role = role,
				Passive = passive,
				AcquireHintKey = acquireKey ?? ("Mods.PokemonHenshin.Acquire." + formId)
			};

		public static MoveSpec Slash(string nameKey, float mult = 1f, int use = 18, int dust = DustID.Smoke, bool easyCrit = false)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AoEBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 0f,
				Knockback = 5f,
				Ai0 = dust
			};

		public static MoveSpec Dig(string nameKey, float mult = 1f, int use = 20)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DigBurstProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.RightClick
			};

		public static MoveSpec Sleep(string nameKey)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<RainFieldProj>(),
				DamageMultiplier = 0.2f,
				UseTime = 55,
				ShootSpeed = 0f,
				Ai0 = (byte)tag,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec Scratch(string nameKey, float mult = 1f, int use = 18)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ScratchSlashProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				NetRisk = NetRisk.Low,
				Collision = CollisionTier.A
			};

		public static MoveSpec FlareUlt(string nameKey, float mult = 2.6f)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BlinkStrikeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f,
				NetRisk = NetRisk.Medium,
				RequiresLungeCooldown = true
			};

		public static MoveSpec ThunderboltUlt(string nameKey, float mult = 10f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ThunderboltUltProj>(),
				DamageMultiplier = mult,
				UseTime = 36,
				ShootSpeed = 18f,
				Knockback = 2f,
				IsRangedProjectile = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec RockTomb(string nameKey, float mult = 1.3f, int use = 28)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<GroundCycloneProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 11f,
				Knockback = 2.5f,
				IsRangedProjectile = true
			};

		public static MoveSpec PeckCone(string nameKey, float mult = 1.2f, int use = 18)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<PeckConeProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 3f
			};

		public static MoveSpec ZenHammer(string nameKey, float mult = 5f, int use = 28)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BarrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = use,
				ShootSpeed = 0f,
				Knockback = 1.2f,
				Ai0 = BarrageDirectorProj.ModeBubble,
				Ai1 = count,
				Ai2 = 16f,
				CountsAsWaterMove = true
			};

		public static MoveSpec ThickBeam(string nameKey, float mult, int dust, bool aftermath = false, int selfStun = 0, int onHitBuff = 0, bool ignoreDef = false, bool ult = true)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ThickBeamProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 42 : 24,
				ShootSpeed = 17f,
				Knockback = 2.5f,
				Ai0 = dust,
				Ai1 = onHitBuff,
				IsRangedProjectile = true,
				IgnoreDefensePartial = ignoreDef,
				AftermathDamagePenaltyTicks = aftermath ? 180 : 0,
				AftermathDamagePenalty = aftermath ? 0.5f : 1f,
				SelfStunTicks = selfStun > 0 ? selfStun : (aftermath ? 90 : 0),
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		public static MoveSpec MouseAoE(string nameKey, float mult, int dust, int buff = 0, bool aftermath = false, bool ignoreDef = false)
			=> new()
			{
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
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec ChargeBeamUlt(string nameKey, float mult = 4f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ChargeBeamDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 55,
				ShootSpeed = 0f,
				Ai0 = DustID.ChlorophyteWeapon,
				CountsAsGrassMove = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec RockSlideX(string nameKey, int count, float mult, bool ult = false)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<RockSlideDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 40 : 28,
				ShootSpeed = 0f,
				Knockback = 4f,
				SpawnAtMouse = true,
				Ai1 = count,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		public static MoveSpec MeteorBarrageUlt(string nameKey, int count = 8, float mult = 4.2f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<MeteorBarrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 48,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				Ai1 = count,
				AftermathDamagePenaltyTicks = 300,
				AftermathDamagePenalty = 0.85f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FlameCone(string nameKey, float mult = 1.55f, int count = 10)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FlameConeDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 26,
				ShootSpeed = 0f,
				Ai1 = count,
				CountsAsFireMove = true
			};

		public static MoveSpec LeafSpread(string nameKey, float mult = 1.3f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LeafSpreadProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				CountsAsGrassMove = true
			};

		public static MoveSpec SeedBombUlt(string nameKey, float mult = 3.3f)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<HurricaneFieldProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 40 : 28,
				ShootSpeed = 0f,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.None
			};

		public static MoveSpec ThunderPillarUlt(string nameKey, float mult = 3.8f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<ThunderPillarUltProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec CloseCombatUlt(string nameKey, float mult = 4f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CloseCombatDirectorProj>(),
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<OutrageDirectorProj>(),
				DamageMultiplier = mult,
				UseTime = 50,
				ShootSpeed = 0f,
				AftermathDamagePenaltyTicks = 120,
				AftermathDamagePenalty = 0.7f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FutureSightUlt(string nameKey, float mult = 3.8f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FutureSightProj>(),
				DamageMultiplier = mult,
				UseTime = 50,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				IgnoreDefensePartial = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec DarkPulseCone(string nameKey, float mult = 3.6f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DarkPulseConeProj>(),
				DamageMultiplier = mult,
				UseTime = 30,
				ShootSpeed = 0f,
				EasyCrit = true,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec FlailUlt(string nameKey, float mult = 1.5f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<FlailBarrageProj>(),
				DamageMultiplier = mult,
				UseTime = 40,
				ShootSpeed = 0f,
				KeyConflict = KeyConflictLevel.ModKeybind
			};

		public static MoveSpec BiteArc(string nameKey, float mult = 1.25f, int brokenArmorTicks = 0)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BiteArcProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				Knockback = 3.5f,
				Ai1 = brokenArmorTicks
			};

		public static MoveSpec DragonBreath(string nameKey, float mult = 1.25f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DragonBreathConeProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f
			};

		public static MoveSpec PetalDance(string nameKey, float mult = 1.5f)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<CrossChopShortProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f,
				EasyCrit = true
			};

		public static MoveSpec TripleStab(string nameKey, float mult = 1.3f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<TripleStabProj>(),
				DamageMultiplier = mult,
				UseTime = 20,
				ShootSpeed = 0f
			};

		public static MoveSpec BrickBreak(string nameKey, float mult = 1.4f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<BrickBreakProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 0f
			};

		public static MoveSpec MouseVortex(string nameKey, float mult, int dust, int buff = 0)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<DynamicPunchProj>(),
				DamageMultiplier = mult,
				UseTime = ult ? 32 : 36,
				ShootSpeed = 0f,
				RequiresLungeCooldown = true,
				KeyConflict = ult ? KeyConflictLevel.ModKeybind : KeyConflictLevel.RightClick
			};

		public static MoveSpec MidThunder(string nameKey, float mult = 1.5f)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<LickFanProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f
			};

		public static MoveSpec AirBurst(string nameKey, float mult = 1.9f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<AirBurstProj>(),
				DamageMultiplier = mult,
				UseTime = 22,
				ShootSpeed = 0f,
				SpawnAtMouse = true,
				EasyCrit = true
			};

		public static MoveSpec StoneEdge(string nameKey, float mult = 1.7f)
			=> new()
			{
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StoneEdgeBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 18,
				ShootSpeed = 12f,
				IsRangedProjectile = true,
				EasyCrit = true
			};

		public static MoveSpec DigUlt(string nameKey, float mult = 3f)
			=> new()
			{
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
				NameKey = nameKey,
				ProjectileType = ModContent.ProjectileType<StrongPsychicBoltProj>(),
				DamageMultiplier = mult,
				UseTime = 20,
				ShootSpeed = 10f,
				IsRangedProjectile = true
			};

		public static MoveSpec HyperBeamUlt(string nameKey, float mult = 4.2f)
			=> ThickBeam(nameKey, mult, DustID.PurpleTorch, aftermath: true, selfStun: 120);

		public static MoveSpec FlareBlitzUlt(string nameKey, float mult = 3.2f)
			=> Lunge(nameKey, mult, 36, DustID.Torch, recoil: true, recoilFrac: 0.25f, onHitBuff: BuffID.OnFire, key: KeyConflictLevel.ModKeybind);

		public static int StageDamage(int stage) => 8 + stage * 6;
	}

	// —— 御三家 ——
	public class CharmanderForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F01", 1, "Mods.PokemonHenshin.Items.CharmanderForce.DisplayName", PokemonType.Fire, 1, null, FormPassiveKind.Blaze);
		protected override MoveSpec CreateMoveA() => FormItemUtil.FireBolt("Mods.PokemonHenshin.Moves.Ember");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Scratch("Mods.PokemonHenshin.Moves.Scratch");
		protected override MoveSpec CreateUltimate() => FormItemUtil.FlareUlt("Mods.PokemonHenshin.Moves.FireSpin");
	}

	public class CharmeleonForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F02", 2, "Mods.PokemonHenshin.Items.CharmeleonForce.DisplayName", PokemonType.Fire, 4, "L01_F01", FormPassiveKind.Blaze);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.DragonPulse", 1.35f, DustID.PurpleTorch, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Slash("Mods.PokemonHenshin.Moves.FireFang", 1.3f, 20, DustID.Torch);
		protected override MoveSpec CreateUltimate() => FormItemUtil.FlareBlitzUlt("Mods.PokemonHenshin.Moves.FlareBlitz");
	}

	public class CharizardForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L01_F03", 3, "Mods.PokemonHenshin.Items.CharizardForce.DisplayName", PokemonType.Fire, 7, "L01_F02", FormPassiveKind.SolarPower, secondary: PokemonType.Flying);
		protected override MoveSpec CreateMoveA() => FormItemUtil.FlameCone("Mods.PokemonHenshin.Moves.Flamethrower", 1.55f, 10);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Scratch("Mods.PokemonHenshin.Moves.DragonClaw", 1.4f, 16);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseAoE("Mods.PokemonHenshin.Moves.Overheat", 4.0f, DustID.Torch, BuffID.OnFire, aftermath: true);
	}

	public class SquirtleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F01", 4, "Mods.PokemonHenshin.Items.SquirtleForce.DisplayName", PokemonType.Water, 1, null, FormPassiveKind.Torrent);
		protected override MoveSpec CreateMoveA() => FormItemUtil.AquaGun("Mods.PokemonHenshin.Moves.WaterGun");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.1f, 18, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.BubbleBarrageUlt("Mods.PokemonHenshin.Moves.BubbleBeam");
	}

	public class WartortleForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F02", 5, "Mods.PokemonHenshin.Items.WartortleForce.DisplayName", PokemonType.Water, 4, "L02_F01", FormPassiveKind.Torrent);
		protected override MoveSpec CreateMoveA() => FormItemUtil.BubbleBarrage("Mods.PokemonHenshin.Moves.BubbleBeam", 24, 1.35f, 28);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Bite", 1.25f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.MouseVortex("Mods.PokemonHenshin.Moves.Whirlpool", 3.2f, DustID.Water);
	}

	public class BlastoiseForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L02_F03", 6, "Mods.PokemonHenshin.Items.BlastoiseForce.DisplayName", PokemonType.Water, 7, "L02_F02", FormPassiveKind.RainDish);
		protected override MoveSpec CreateMoveA() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.HydroPump", 1.7f, DustID.Water, ult: false);
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.SkullBash", 1.6f, 34, DustID.Water);
		protected override MoveSpec CreateUltimate() => FormItemUtil.ThickBeam("Mods.PokemonHenshin.Moves.HydroCannon", 4.0f, DustID.Water, aftermath: true, selfStun: 90);
	}

	public class BulbasaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(1);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F01", 7, "Mods.PokemonHenshin.Items.BulbasaurForce.DisplayName", PokemonType.Grass, 1, null, FormPassiveKind.Overgrow, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.VineWhip("Mods.PokemonHenshin.Moves.VineWhip");
		protected override MoveSpec CreateMoveB() => FormItemUtil.Lunge("Mods.PokemonHenshin.Moves.Tackle", 1.1f, 18, DustID.Grass);
		protected override MoveSpec CreateUltimate() => FormItemUtil.SeedBarrageUlt("Mods.PokemonHenshin.Moves.SeedGun");
	}

	public class IvysaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(4);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F02", 8, "Mods.PokemonHenshin.Items.IvysaurForce.DisplayName", PokemonType.Grass, 4, "L03_F01", FormPassiveKind.Overgrow, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.LeafSpread("Mods.PokemonHenshin.Moves.RazorLeaf", 1.3f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.BiteArc("Mods.PokemonHenshin.Moves.Bite", 1.25f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.SeedBombUlt("Mods.PokemonHenshin.Moves.SeedBomb", 3.3f);
	}

	public class VenusaurForce : HenshinForceItem
	{
		protected override int BaseDamage => FormItemUtil.StageDamage(7);
		protected override FormDefinition CreateDefinition() => FormItemUtil.Def("L03_F03", 9, "Mods.PokemonHenshin.Items.VenusaurForce.DisplayName", PokemonType.Grass, 7, "L03_F02", FormPassiveKind.Chlorophyll, secondary: PokemonType.Poison);
		protected override MoveSpec CreateMoveA() => FormItemUtil.SludgeBolt("Mods.PokemonHenshin.Moves.SludgeBomb", 1.45f);
		protected override MoveSpec CreateMoveB() => FormItemUtil.PetalDance("Mods.PokemonHenshin.Moves.PetalDance", 1.5f);
		protected override MoveSpec CreateUltimate() => FormItemUtil.ChargeBeamUlt("Mods.PokemonHenshin.Moves.SolarBeam", 4.0f);
	}
}
