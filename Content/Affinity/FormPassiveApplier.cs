using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Affinity
{
	/// <summary>Excel 持握被动（优先于情境弱加成）。</summary>
	public static class FormPassiveApplier
	{
		public static void Apply(HenshinPlayer hp)
		{
			if (hp.CurrentForm == null)
				return;
			Player player = hp.Player;
			switch (hp.CurrentForm.Passive)
			{
				case FormPassiveKind.Blaze:
				case FormPassiveKind.Torrent:
				case FormPassiveKind.Overgrow:
					if (player.statLife <= player.statLifeMax2 * 0.5f)
						hp.TypeMoveBonus = 0.20f;
					break;
				case FormPassiveKind.Chlorophyll:
					if (Main.dayTime && player.ZoneOverworldHeight)
						player.moveSpeed += 0.35f;
					break;
				case FormPassiveKind.RainDish:
					if ((Main.raining || !Main.dayTime) && player.miscCounter % 90 == 0 && player.statLife < player.statLifeMax2)
						player.statLife = System.Math.Min(player.statLifeMax2, player.statLife + 2);
					break;
				case FormPassiveKind.Synchronize:
					hp.SynchronizePassive = true;
					break;
				case FormPassiveKind.ShedSkin:
					if (Main.rand.NextBool(300))
						ClearDebuffs(player);
					break;
				case FormPassiveKind.Multiscale:
					if (player.statLife >= player.statLifeMax2)
						hp.IncomingDamageMultiplier *= 0.20f;
					break;
				case FormPassiveKind.ClearBody:
					ClearDebuffs(player);
					player.buffImmune[BuffID.Poisoned] = true;
					player.buffImmune[BuffID.OnFire] = true;
					player.buffImmune[BuffID.Confused] = true;
					player.buffImmune[BuffID.Electrified] = true;
					player.buffImmune[BuffID.Slow] = true;
					break;
				case FormPassiveKind.RoughSkin:
					hp.RoughSkinPassive = true;
					break;
				case FormPassiveKind.Levitate:
					hp.LevitateFlight = true;
					player.noFallDmg = true;
					break;
				case FormPassiveKind.SwiftSwim:
					if (Main.raining || !Main.dayTime)
						player.moveSpeed += 0.25f;
					break;
				case FormPassiveKind.Moxie:
					hp.HenshinDamageBonus += 0.20f * hp.MoxieStacks;
					break;
				case FormPassiveKind.Guts:
					if (HasAnyDebuff(player))
						hp.HenshinDamageBonus += 0.25f;
					break;
				case FormPassiveKind.KeenEye:
					hp.HenshinDamageBonus += 0.20f;
					break;
				case FormPassiveKind.Static:
					hp.StaticPassive = true;
					break;
				case FormPassiveKind.RockHead:
					hp.IgnoreRecoil = true;
					hp.IncomingDamageMultiplier *= 0.85f; // ~防御感 1.2
					break;
				case FormPassiveKind.Pressure:
					hp.HenshinDamageBonus += 0.50f;
					break;
				case FormPassiveKind.AirLock:
					hp.HenshinDamageBonus += 0.70f;
					break;
				case FormPassiveKind.SolarPower:
					if (Main.dayTime)
					{
						hp.HenshinDamageBonus += 0.20f;
						hp.SolarPowerDrain = true;
					}
					break;
				case FormPassiveKind.SandVeil:
					player.pickSpeed -= 0.25f;
					if (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)
						player.moveSpeed += 0.15f;
					break;
			}
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

		private static void ClearDebuffs(Player player)
		{
			for (int i = 0; i < Player.MaxBuffs; i++)
			{
				int id = player.buffType[i];
				if (id > 0 && Main.debuff[id] && !BuffID.Sets.NurseCannotRemoveDebuff[id])
					player.DelBuff(i--);
			}
		}
	}
}
