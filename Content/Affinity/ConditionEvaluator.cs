using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using PokemonHenshin.Content.WeatherField;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Affinity
{
	public enum ConditionId : byte
	{
		BiomeHell,
		NearLava,
		TargetOnFire,
		BiomeOcean,
		WeatherRain,
		WeatherThunder,
		TargetWet,
		BiomeJungle,
		SurfaceDay,
		LushArea,
		HighAltitude,
		BiomeSpace,
		OpenSurface,
		BiomeSnow,
		BiomeIce,
		WeatherSnow,
		LayerUnderground,
		LayerCave,
		BiomeDesert,
		BiomeHill,
		WeatherSandstorm,
		SelfLowLife,
		BiomeDungeon,
		Corrupt,
		Crimson,
		Hallow,
		Mushroom,
		NightDark,
		TargetBoss,
		TargetHighLife,
		EnemiesNearby,
		MeleeRange,
		SelfHasDebuff,
		TargetPoisoned,
		InWater,
		InLava
	}

	public static class ConditionEvaluator
	{
		public static bool Evaluate(Player player, ConditionId id, NPC target = null)
		{
			return id switch
			{
				ConditionId.BiomeHell => player.ZoneUnderworldHeight,
				ConditionId.NearLava => NearLiquid(player, 1, 8), // LiquidID.Lava = 1
				ConditionId.TargetOnFire => target != null && target.onFire,
				ConditionId.BiomeOcean => player.ZoneBeach,
				ConditionId.WeatherRain => WeatherFieldSystem.IsInAnyRain(player.Center),
				ConditionId.WeatherThunder => Main.IsItRaining && !Main.dayTime || WeatherFieldSystem.IsInField(player.Center, WeatherTag.Thunder),
				ConditionId.TargetWet => target != null && (target.wet || target.HasBuff(BuffID.Wet)),
				ConditionId.BiomeJungle => player.ZoneJungle,
				ConditionId.SurfaceDay => player.ZoneOverworldHeight && Main.dayTime,
				ConditionId.HighAltitude => player.ZoneSkyHeight,
				ConditionId.BiomeSpace => player.ZoneSkyHeight,
				ConditionId.OpenSurface => player.ZoneOverworldHeight && !player.ZoneDirtLayerHeight,
				ConditionId.BiomeSnow => player.ZoneSnow,
				ConditionId.BiomeIce => player.ZoneSnow && player.ZoneRockLayerHeight,
				ConditionId.WeatherSnow => Main.IsItRaining && player.ZoneSnow || WeatherFieldSystem.IsInField(player.Center, WeatherTag.Snow),
				ConditionId.LayerUnderground => player.ZoneDirtLayerHeight,
				ConditionId.LayerCave => player.ZoneRockLayerHeight,
				ConditionId.BiomeDesert => player.ZoneDesert,
				ConditionId.SelfLowLife => player.statLife <= player.statLifeMax2 * 0.5f,
				ConditionId.BiomeDungeon => player.ZoneDungeon,
				ConditionId.Corrupt => player.ZoneCorrupt,
				ConditionId.Crimson => player.ZoneCrimson,
				ConditionId.Hallow => player.ZoneHallow,
				ConditionId.Mushroom => player.ZoneGlowshroom,
				ConditionId.NightDark => !Main.dayTime && player.townNPCs < 2,
				ConditionId.TargetBoss => target != null && target.boss,
				ConditionId.TargetHighLife => target != null && target.lifeMax >= 5000,
				ConditionId.InWater => player.wet && !player.lavaWet,
				ConditionId.InLava => player.lavaWet,
				ConditionId.LushArea => CountGrass(player, 12) >= 20,
				ConditionId.EnemiesNearby => CountEnemies(player, 18) >= 5,
				ConditionId.MeleeRange => target != null && target.Distance(player.Center) <= 6f * 16f,
				ConditionId.SelfHasDebuff => HasAnyDebuff(player),
				ConditionId.TargetPoisoned => target != null && (target.poisoned || target.venom),
				ConditionId.BiomeHill => player.ZoneOverworldHeight && player.position.Y < Main.worldSurface * 0.35f * 16f,
				ConditionId.WeatherSandstorm => (player.ZoneDesert && Main.windSpeedCurrent > 0.6f) || WeatherFieldSystem.IsInField(player.Center, WeatherTag.Sand),
				_ => false
			};
		}

		public static bool AnyBossNearby(Player player)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && npc.boss && npc.DistanceSQ(player.Center) < 80f * 16f * 80f * 16f)
					return true;
			}
			return false;
		}

		private static bool NearLiquid(Player player, int liquidId, int radiusTiles)
		{
			int cx = (int)(player.Center.X / 16f);
			int cy = (int)(player.Center.Y / 16f);
			for (int x = cx - radiusTiles; x <= cx + radiusTiles; x++)
			{
				for (int y = cy - radiusTiles; y <= cy + radiusTiles; y++)
				{
					if (!WorldGen.InWorld(x, y))
						continue;
					Tile t = Main.tile[x, y];
					if (t != null && t.LiquidAmount > 0 && t.LiquidType == liquidId)
						return true;
				}
			}
			return false;
		}

		private static int CountGrass(Player player, int radius)
		{
			int cx = (int)(player.Center.X / 16f);
			int cy = (int)(player.Center.Y / 16f);
			int n = 0;
			for (int x = cx - radius; x <= cx + radius; x++)
			{
				for (int y = cy - radius; y <= cy + radius; y++)
				{
					if (!WorldGen.InWorld(x, y))
						continue;
					Tile t = Main.tile[x, y];
					if (t == null || !t.HasTile)
						continue;
					ushort type = t.TileType;
					if (type == TileID.Grass || type == TileID.JungleGrass || type == TileID.Vines
						|| type == TileID.JungleVines || type == TileID.BloomingHerbs || type == TileID.MatureHerbs
						|| type == TileID.Plants || type == TileID.Plants2)
						n++;
				}
			}
			return n;
		}

		private static int CountEnemies(Player player, int radiusTiles)
		{
			float r2 = radiusTiles * 16f * radiusTiles * 16f;
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && !npc.friendly && npc.damage > 0 && npc.DistanceSQ(player.Center) <= r2)
					n++;
			}
			return n;
		}

		private static bool HasAnyDebuff(Player player)
		{
			for (int i = 0; i < Player.MaxBuffs; i++)
			{
				int id = player.buffType[i];
				if (id > 0 && Main.debuff[id])
					return true;
			}
			return false;
		}
	}

	/// <summary>按形态主属性套用被动。</summary>
	public static class TypePassiveApplier
	{
		public static void Apply(HenshinPlayer hp)
		{
			Player player = hp.Player;
			FormDefinition form = hp.CurrentForm;
			if (form == null)
				return;

			float amp = 1f + hp.AffinityAmplitudeBonus;

			ApplyPrimary(hp, player, form.Primary, amp);
			if (form.Secondary != PokemonType.None)
				ApplySecondaryWeak(hp, player, form.Secondary, amp);
		}

		private static void ApplyPrimary(HenshinPlayer hp, Player player, PokemonType type, float amp)
		{
			switch (type)
			{
				case PokemonType.Fire:
					player.lavaImmune = true;
					player.fireWalk = true;
					player.buffImmune[BuffID.OnFire] = true;
					player.buffImmune[BuffID.Burning] = true;
					if (ConditionEvaluator.Evaluate(player, ConditionId.InWater))
						hp.HenshinDamageBonus -= 0.10f * amp;
					break;
				case PokemonType.Water:
					player.gills = true;
					player.accMerman = true;
					if (player.wet)
						player.moveSpeed += 0.25f * amp + hp.WaterSpeedBonus;
					break;
				case PokemonType.Flying:
				{
					float fallRed = System.Math.Min(0.75f, 0.5f + hp.FallDamageReduction);
					if (fallRed >= 0.75f)
						player.noFallDmg = true;
					else
						player.extraFall += (int)(25 * fallRed);
					break;
				}
				case PokemonType.Grass:
					if (player.statLife < player.statLifeMax2 && player.miscCounter % 60 == 0)
						player.statLife += 1;
					break;
				case PokemonType.Electric:
					// 短 CD 冲刺占位：提升跑速
					player.moveSpeed += 0.08f * amp;
					break;
				case PokemonType.Ground:
					player.pickSpeed -= 0.40f * amp;
					player.kbBuff = true;
					break;
				case PokemonType.Rock:
					hp.IncomingDamageMultiplier *= 0.90f;
					player.noFallDmg = true;
					break;
				case PokemonType.Steel:
					if (ConditionEvaluator.Evaluate(player, ConditionId.SelfLowLife) || ConditionEvaluator.AnyBossNearby(player))
						player.buffImmune[BuffID.Confused] = true;
					break;
				case PokemonType.Dragon:
					player.kbBuff = true;
					break;
				case PokemonType.Dark:
					player.GetCritChance(DamageClass.Generic) += 5f * amp;
					break;
				case PokemonType.Fighting:
					hp.HenshinDamageBonus += 0.05f * amp;
					break;
				case PokemonType.Psychic:
					player.slowFall = true;
					break;
				case PokemonType.Bug:
					player.moveSpeed += 0.10f * amp;
					break;
				case PokemonType.Poison:
					player.buffImmune[BuffID.Poisoned] = true;
					break;
				case PokemonType.Fairy:
					// 减益缩短占位
					break;
				case PokemonType.Normal:
					hp.HenshinDamageBonus += 0.03f * amp;
					break;
				case PokemonType.Ghost:
					// 穿障只来自招式
					break;
				case PokemonType.Ice:
					player.buffImmune[BuffID.Chilled] = true;
					player.buffImmune[BuffID.Frozen] = true;
					break;
			}
		}

		private static void ApplySecondaryWeak(HenshinPlayer hp, Player player, PokemonType type, float amp)
		{
			switch (type)
			{
				case PokemonType.Flying:
					player.extraFall += 5;
					break;
				case PokemonType.Poison:
					player.buffImmune[BuffID.Poisoned] = true;
					break;
				case PokemonType.Psychic:
					player.slowFall = true;
					break;
				case PokemonType.Steel:
					hp.IncomingDamageMultiplier *= 0.97f;
					break;
				case PokemonType.Ground:
					player.pickSpeed -= 0.10f * amp;
					break;
				case PokemonType.Dragon:
					player.kbBuff = true;
					break;
				case PokemonType.Water:
					if (player.wet)
						player.moveSpeed += 0.05f * amp;
					break;
			}
		}
	}
}
