using System;
using System.Collections.Generic;
using System.Text;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Affinity;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.Damage;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace PokemonHenshin.Content.Visual
{
	internal enum StatLineKind : byte
	{
		Header,
		Body,
		Active,
		Inactive,
		Warn
	}

	internal readonly struct StatLine
	{
		public StatLineKind Kind { get; }
		public string Text { get; }

		public StatLine(StatLineKind kind, string text)
		{
			Kind = kind;
			Text = text ?? string.Empty;
		}
	}

	internal sealed class HenshinStatSnapshot
	{
		public string Fingerprint { get; init; }
		public string FormTexturePath { get; init; }
		public bool Transformed { get; init; }
		public List<StatLine> Lines { get; init; }
	}

	/// <summary>
	/// 变身属性面板的数据层：从 <see cref="HenshinPlayer"/> / 形态定义 / 已装备本模饰品
	/// 汇总「当前」加成与特性。不是图鉴。按真实出伤乘区分组展示。
	/// </summary>
	internal static class HenshinStatSheet
	{
		private const string Prefix = "Mods.PokemonHenshin.StatsUI.";
		private const float EasyCritBonus = 35f;

		public static HenshinStatSnapshot Capture(Player player)
		{
			var lines = new List<StatLine>(64);
			if (player == null || !player.active)
			{
				return new HenshinStatSnapshot
				{
					Fingerprint = "none",
					FormTexturePath = null,
					Transformed = false,
					Lines = lines
				};
			}

			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			List<AccRow> accs = CollectAccessories(player, hp);

			if (!hp.IsTransformed || hp.CurrentForm == null)
			{
				BuildUntransformed(lines, hp, accs);
				return new HenshinStatSnapshot
				{
					Fingerprint = Fingerprint(player, hp, accs, transformed: false),
					FormTexturePath = null,
					Transformed = false,
					Lines = lines
				};
			}

			BuildTransformed(lines, player, hp, accs);
			return new HenshinStatSnapshot
			{
				Fingerprint = Fingerprint(player, hp, accs, transformed: true),
				FormTexturePath = hp.CurrentForm.TexturePath,
				Transformed = true,
				Lines = lines
			};
		}

		private static void BuildUntransformed(List<StatLine> lines, HenshinPlayer hp, List<AccRow> accs)
		{
			lines.Add(new StatLine(StatLineKind.Warn, T("NotTransformed")));
			if (hp.EverstoneBlock)
				lines.Add(new StatLine(StatLineKind.Active, T("EverstoneOn")));
			if (hp.CurseTagSuperImmune)
				lines.Add(new StatLine(StatLineKind.Active, T("CurseTagImmuneOn")));
			else if (hp.CurseTagBurnTicks > 0)
				lines.Add(new StatLine(StatLineKind.Warn, T("CurseTagBurnOn", Num(hp.CurseTagBurnTicks / 60f))));
			AppendAccessories(lines, accs);
		}

		private static void BuildTransformed(List<StatLine> lines, Player player, HenshinPlayer hp, List<AccRow> accs)
		{
			FormDefinition form = hp.CurrentForm;
			HenshinForceItem force = player.HeldItem?.ModItem as HenshinForceItem;
			int world = ProgressStageService.GetProgressStage();
			int level = force?.Level ?? HenshinStatService.StartingLevelForFormStage(form.Stage);
			int xp = force?.Xp ?? 0;
			int cap = HenshinStatService.LevelCap(world);
			int need = HenshinStatService.ExpNeeded(level);
			bool capped = level >= cap || level >= HenshinStatService.MaxLevel;
			int formAtk = force?.ComputeFinalAttack() ?? 0;
			int formDef = force?.ComputeFinalDefense() ?? 0;
			if (hp.EvioliteDefMul > 0f && FormRegistry.FindEvolutionOf(form.FormId) != null)
				formDef = (int)Math.Round(formDef * (1f + hp.EvioliteDefMul));
			int panelAtk = force != null ? player.GetWeaponDamage(force.Item) : formAtk;

			string types = TypeName(form.Primary);
			if (form.Secondary != PokemonType.None)
				types += " / " + TypeName(form.Secondary);

			lines.Add(new StatLine(StatLineKind.Header, T("SectionForm")));
			lines.Add(new StatLine(StatLineKind.Body, Language.GetTextValue(form.DisplayNameKey)));
			lines.Add(new StatLine(StatLineKind.Body, T("TypesLine", types)));
			lines.Add(new StatLine(StatLineKind.Body, T(
				capped ? "LevelCapped" : "LevelLine",
				level, cap, world)));
			if (!capped && need > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("XpLine", xp, need)));
			lines.Add(new StatLine(StatLineKind.Body, T("AttackLine", formAtk, panelAtk)));
			lines.Add(new StatLine(StatLineKind.Body, T("DefenseLine", formDef, player.statDefense)));
			lines.Add(new StatLine(StatLineKind.Body, T("EnergyLine",
				(int)Math.Round(hp.UltimateEnergy), (int)Math.Round(hp.UltimateEnergyMax))));
			if (ShowsFlight(form, hp))
			{
				lines.Add(new StatLine(StatLineKind.Body, T("FlightLine",
					Secs(hp.FlightEnergy), Secs(hp.FlightEnergyMax))));
			}

			lines.Add(new StatLine(StatLineKind.Warn, T("NoMount")));

			FormDefinition next = FormRegistry.FindEvolutionOf(form.FormId);
			if (hp.EverstoneBlock)
				lines.Add(new StatLine(StatLineKind.Warn, T("EverstoneOn")));
			else if (next != null)
			{
				int needLv = HenshinStatService.BandMinOf(next.Stage);
				bool ready = HenshinStatService.MeetsEvolution(world, level, next.Stage);
				string nextName = Language.GetTextValue(next.DisplayNameKey);
				lines.Add(new StatLine(ready ? StatLineKind.Active : StatLineKind.Inactive,
					T(ready ? "EvolveReady" : "EvolveNeed", nextName, next.Stage, needLv)));
			}

			AppendMoves(lines, force, form, hp);
			AppendPassive(lines, player, hp, form);
			AppendTypeEffects(lines, player, hp, form);
			AppendCrit(lines, player, hp, force, form);
			AppendCombatZones(lines, hp);
			AppendAccessories(lines, accs);
			AppendRuntime(lines, hp);
		}

		private static void AppendMoves(List<StatLine> lines, HenshinForceItem force, FormDefinition form, HenshinPlayer hp)
		{
			MoveSpec m1 = force?.Move1 ?? form.Move1;
			MoveSpec m2 = force?.Move2 ?? form.Move2;
			MoveSpec ult = force?.Ultimate ?? form.Ultimate;
			if (m1 == null && m2 == null && ult == null)
				return;

			lines.Add(new StatLine(StatLineKind.Header, T("SectionMoves")));
			if (m1 != null)
			{
				lines.Add(new StatLine(StatLineKind.Body, T("Skill1", Language.GetTextValue(m1.NameKey))));
				lines.Add(new StatLine(StatLineKind.Body, FormatMoveDetail(m1, ultimate: false)));
			}

			if (m2 != null)
			{
				lines.Add(new StatLine(hp.ChoiceLockSkill2 ? StatLineKind.Warn : StatLineKind.Body,
					hp.ChoiceLockSkill2
						? T("Skill2Locked", Language.GetTextValue(m2.NameKey))
						: T("Skill2", Language.GetTextValue(m2.NameKey))));
				lines.Add(new StatLine(StatLineKind.Body, FormatMoveDetail(m2, ultimate: false)));
			}

			if (ult != null)
			{
				lines.Add(new StatLine(hp.ChoiceLockUlt ? StatLineKind.Warn : StatLineKind.Body,
					hp.ChoiceLockUlt
						? T("UltLocked", Language.GetTextValue(ult.NameKey))
						: T("Ult", Language.GetTextValue(ult.NameKey))));
				lines.Add(new StatLine(StatLineKind.Body, FormatMoveDetail(ult, ultimate: true)));
			}
		}

		private static string FormatMoveDetail(MoveSpec move, bool ultimate)
		{
			string power = T("MovePower", MultPct(move.DamageMultiplier));
			string second = ultimate
				? T("MoveUltCost")
				: T("MoveInterval", move.UseTime);
			string line = power + T("MoveSep") + second;
			if (move.EasyCrit)
				line += T("MoveSep") + T("MoveEasyCrit", Pct(EasyCritBonus / 100f));
			if (move.RecoilSelf)
				line += T("MoveSep") + T("MoveRecoil");
			if (move.AftermathDamagePenaltyTicks > 0)
				line += T("MoveSep") + T("MoveAftermath");
			return "　　" + line;
		}

		private static void AppendPassive(List<StatLine> lines, Player player, HenshinPlayer hp, FormDefinition form)
		{
			if (form.Passive == FormPassiveKind.None)
				return;

			lines.Add(new StatLine(StatLineKind.Header, T("SectionPassive")));
			string name = T("Passive." + form.Passive + ".Name");
			string desc = T("Passive." + form.Passive + ".Desc");
			bool? active = PassiveConditionMet(player, hp, form.Passive);
			if (active == null)
				lines.Add(new StatLine(StatLineKind.Body, T("PassiveAlways", name, desc)));
			else if (active.Value)
				lines.Add(new StatLine(StatLineKind.Active, T("PassiveOn", name, desc)));
			else
				lines.Add(new StatLine(StatLineKind.Inactive, T("PassiveOff", name, desc)));
		}

		private static void AppendTypeEffects(List<StatLine> lines, Player player, HenshinPlayer hp, FormDefinition form)
		{
			lines.Add(new StatLine(StatLineKind.Header, T("SectionType")));
			AppendTypePrimary(lines, player, hp, form.Primary);
			if (form.Secondary != PokemonType.None)
				AppendTypeSecondary(lines, player, hp, form.Secondary);
		}

		private static void AppendTypePrimary(List<StatLine> lines, Player player, HenshinPlayer hp, PokemonType type)
		{
			string typeName = TypeName(type);
			switch (type)
			{
				case PokemonType.Fire:
					lines.Add(Cond(ConditionEvaluator.Evaluate(player, ConditionId.InWater),
						T("TypeFireWet", typeName, Pct(0.10f)),
						T("TypeFireOk", typeName)));
					break;
				case PokemonType.Water:
					lines.Add(Cond(player.wet,
						T("TypeWaterWet", typeName, Pct(0.25f)),
						T("TypeWaterDry", typeName)));
					break;
				case PokemonType.Grass:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGrass", typeName)));
					break;
				case PokemonType.Electric:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeElectric", typeName, Pct(1f - hp.DashCooldownMultiplier))));
					break;
				case PokemonType.Flying:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeFlying", typeName, Pct(0.5f))));
					break;
				case PokemonType.Ice:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeIce", typeName)));
					break;
				case PokemonType.Ground:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGround", typeName, Pct(0.40f))));
					break;
				case PokemonType.Rock:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeRock", typeName)));
					break;
				case PokemonType.Steel:
					bool steelOn = ConditionEvaluator.Evaluate(player, ConditionId.SelfLowLife) || ConditionEvaluator.AnyBossNearby(player);
					lines.Add(Cond(steelOn, T("TypeSteelOn", typeName), T("TypeSteelOff", typeName)));
					break;
				case PokemonType.Dragon:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeDragon", typeName)));
					break;
				case PokemonType.Dark:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeDark", typeName, Num(5f))));
					break;
				case PokemonType.Fighting:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeFighting", typeName, Pct(0.05f))));
					break;
				case PokemonType.Psychic:
					lines.Add(new StatLine(StatLineKind.Body, T("TypePsychic", typeName)));
					break;
				case PokemonType.Bug:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeBug", typeName, Pct(0.10f))));
					break;
				case PokemonType.Poison:
					lines.Add(new StatLine(StatLineKind.Body, T("TypePoison", typeName)));
					break;
				case PokemonType.Normal:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeNormal", typeName, Pct(0.03f))));
					break;
				case PokemonType.Ghost:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGhost", typeName)));
					break;
				case PokemonType.Fairy:
					lines.Add(new StatLine(StatLineKind.Inactive, T("TypeFairy", typeName)));
					break;
			}
		}

		private static void AppendTypeSecondary(List<StatLine> lines, Player player, HenshinPlayer hp, PokemonType type)
		{
			string typeName = TypeName(type);
			switch (type)
			{
				case PokemonType.Flying:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecFlying", typeName, Pct(0.5f))));
					break;
				case PokemonType.Poison:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecPoison", typeName)));
					break;
				case PokemonType.Psychic:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecPsychic", typeName)));
					break;
				case PokemonType.Steel:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecSteel", typeName)));
					break;
				case PokemonType.Ground:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecGround", typeName, Pct(0.10f))));
					break;
				case PokemonType.Dragon:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecDragon", typeName)));
					break;
				case PokemonType.Water:
					lines.Add(Cond(player.wet,
						T("TypeSecWaterWet", typeName, Pct(0.05f)),
						T("TypeSecWaterDry", typeName)));
					break;
				case PokemonType.Electric:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeElectric", typeName, Pct(1f - hp.DashCooldownMultiplier))));
					break;
				default:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecNone", typeName)));
					break;
			}
		}

		private static void AppendCrit(List<StatLine> lines, Player player, HenshinPlayer hp, HenshinForceItem force, FormDefinition form)
		{
			lines.Add(new StatLine(StatLineKind.Header, T("SectionCrit")));
			float crit = player.GetTotalCritChance(HenshinDamage.Instance);
			lines.Add(new StatLine(StatLineKind.Body, T("CritChance", Pct(crit / 100f))));

			bool anyEasy = (force?.Move1 ?? form.Move1)?.EasyCrit == true
				|| (force?.Move2 ?? form.Move2)?.EasyCrit == true
				|| (force?.Ultimate ?? form.Ultimate)?.EasyCrit == true;
			if (anyEasy)
				lines.Add(new StatLine(StatLineKind.Body, T("CritEasyNote", Pct(EasyCritBonus / 100f))));

			if (hp.CritUpgradeChance > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("CritUpgrade", Pct(hp.CritUpgradeChance))));
			else
				lines.Add(new StatLine(StatLineKind.Inactive, T("CritUpgradeNone")));

			if (hp.OnFireCritUpgrade > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("CritOnFire", Pct(hp.OnFireCritUpgrade))));

			if (hp.CritDamageBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("CritDamageBonus", Pct(hp.CritDamageBonus))));

			lines.Add(new StatLine(StatLineKind.Body, T("CritDamageNote")));
		}

		private static void AppendCombatZones(List<StatLine> lines, HenshinPlayer hp)
		{
			lines.Add(new StatLine(StatLineKind.Header, T("SectionCombat")));
			AppendWeaponZone(lines, hp);
			AppendHitZone(lines, hp);
			AppendOtherZone(lines, hp);
		}

		private static void AppendWeaponZone(List<StatLine> lines, HenshinPlayer hp)
		{
			lines.Add(new StatLine(StatLineKind.Body, T("ZoneWeapon")));
			int before = lines.Count;
			AddSignedPct(lines, "TotDamage", hp.HenshinDamageBonus);
			AddSignedPct(lines, "TotFactor", hp.HenshinDamageFactorBonus);
			AddSignedPct(lines, "TotChoice", hp.ChoiceDamage);
			AddSignedPct(lines, "TotLifeOrb", hp.LifeOrbDamage);
			AddSignedPct(lines, "TotFormAtk", hp.FormAtkMul);
			if (lines.Count == before)
			{
				lines.Add(new StatLine(StatLineKind.Inactive, T("ZoneEmpty")));
				return;
			}

			float product = 1f;
			product *= 1f + hp.HenshinDamageBonus;
			product *= 1f + hp.HenshinDamageFactorBonus;
			product *= 1f + hp.ChoiceDamage;
			product *= 1f + hp.LifeOrbDamage;
			product *= 1f + hp.FormAtkMul;
			lines.Add(new StatLine(StatLineKind.Active, T("ZoneWeaponTotal", MultPct(product))));
		}

		private static void AppendHitZone(List<StatLine> lines, HenshinPlayer hp)
		{
			lines.Add(new StatLine(StatLineKind.Body, T("ZoneHit")));
			int before = lines.Count;
			AddSignedPct(lines, "TotBoss", hp.BossDamageBonus);
			AddSignedPct(lines, "TotOnFire", hp.OnFireTargetBonus);
			AddSignedPct(lines, "TotTypeMove", hp.TypeMoveBonus);
			AddSignedPct(lines, "TotUlt", hp.UltDamageBonus);
			AddSignedPct(lines, "TotFireMove", hp.FireMoveDamage);
			AddSignedPct(lines, "TotMelee", hp.MeleeDeliveryDamage);
			AddSignedPct(lines, "TotPsychicDragon", hp.PsychicDragonDamage);
			AddSignedPct(lines, "TotEvioliteDmg", hp.EvioliteDamage);
			if (hp.AftermathPenaltyTimer > 0 && Math.Abs(hp.AftermathPenaltyMult - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Warn, T("TotAftermathNow", Pct(hp.AftermathPenaltyMult))));

			if (lines.Count == before)
				lines.Add(new StatLine(StatLineKind.Inactive, T("ZoneEmpty")));
		}

		private static void AppendOtherZone(List<StatLine> lines, HenshinPlayer hp)
		{
			lines.Add(new StatLine(StatLineKind.Body, T("ZoneOther")));
			int before = lines.Count;

			AddSignedPct(lines, "TotEvioliteDef", hp.EvioliteDefMul);
			AddSignedPct(lines, "TotFormDef", hp.FormDefMul);
			AddSignedPct(lines, "TotMoveSpeed", hp.MoveSpeedBonus);
			AddSignedPct(lines, "TotWaterSpeed", hp.WaterSpeedBonus);
			AddSignedPct(lines, "TotXpHeld", hp.XpHeldMul);
			AddSignedPct(lines, "TotXpShare", hp.XpHotbarShareMul);
			AddSignedPct(lines, "TotPassiveEnergy", hp.PassiveEnergyMul);

			if (Math.Abs(hp.IncomingDamageMultiplier - 1f) > 0.0005f)
				lines.Add(KindBySign(hp.IncomingDamageMultiplier < 1f, T("TotIncoming", Pct(hp.IncomingDamageMultiplier))));
			float moveIntervalMul = hp.MoveCooldownMultiplier * hp.UseTimeMul;
			if (Math.Abs(moveIntervalMul - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotCooldown", Pct(moveIntervalMul))));
			if (Math.Abs(hp.DashCooldownMultiplier - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotDashCd", Pct(hp.DashCooldownMultiplier))));
			if (Math.Abs(hp.LungeCooldownMultiplier - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotLungeCd", Pct(hp.LungeCooldownMultiplier))));
			if (Math.Abs(hp.EnergyGainMultiplier - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotEnergyGain", Pct(hp.EnergyGainMultiplier))));
			if (hp.UltRetainFraction > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotUltRetain", Pct(hp.UltRetainFraction))));
			if (hp.ExtraFlightEnergy > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotFlightSec", Num(hp.ExtraFlightEnergy))));
			if (hp.FlightEnergyMul > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotFlightMul", Pct(hp.FlightEnergyMul))));
			if (hp.PenetrateAdd > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotPenetrate", hp.PenetrateAdd)));
			if (hp.DashSpeedBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotDashSpeed", Num(hp.DashSpeedBonus))));
			if (hp.LungeIFrameBonus > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotLungeIFrame", hp.LungeIFrameBonus)));
			if (hp.DodgeChance > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotDodge", Pct(hp.DodgeChance))));
			if (hp.AccDefense > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotAccDef", Num(hp.AccDefense))));
			if (hp.WaterMoveDamage > 0.0005f)
				AddSignedPct(lines, "TotWaterMove", hp.WaterMoveDamage);
			if (hp.FlyingMoveDamage > 0.0005f)
				AddSignedPct(lines, "TotFlyingMove", hp.FlyingMoveDamage);
			if (hp.CritChanceBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotCritChance", Pct(hp.CritChanceBonus))));
			if (hp.CritDamageBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotCritDamage", Pct(hp.CritDamageBonus))));
			if (hp.LeftoversHpPerSec > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotLeftovers", Num(hp.LeftoversHpPerSec))));
			if (hp.LeftoversLowHpBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotLeftoversLow", Num(hp.LeftoversLowHpBonus))));
			if (hp.ShellBellHeal > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotShellBell", Num(hp.ShellBellHeal))));
			if (hp.RockyHelmetScale > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotRocky", Pct(hp.RockyHelmetScale))));
			if (hp.LifeOrbHpDrain)
				lines.Add(new StatLine(StatLineKind.Warn, T("TotLifeOrbDrain")));
			if (hp.FocusSash)
				lines.Add(new StatLine(StatLineKind.Body, T("TotSash", Pct(hp.FocusSashHpPct), Num(hp.FocusSashCdSec))));
			if (hp.CurseTagSuperImmune)
				lines.Add(new StatLine(StatLineKind.Active, T("TotCurseImmune")));
			else if (hp.CurseTagBurnTicks > 0)
				lines.Add(new StatLine(StatLineKind.Warn, T("TotCurseBurn", Num(hp.CurseTagBurnTicks / 60f))));

			AppendDeliveryFlags(lines, hp);

			if (lines.Count == before)
				lines.Add(new StatLine(StatLineKind.Inactive, T("ZoneEmpty")));
		}

		private static void AppendDeliveryFlags(List<StatLine> lines, HenshinPlayer hp)
		{
			var homing = new List<string>(4);
			if (hp.HomingBolt) homing.Add(T("DelBolt"));
			if (hp.HomingSpread) homing.Add(T("DelSpread"));
			if (hp.HomingBarrage) homing.Add(T("DelBarrage"));
			if (hp.HomingDoTBind) homing.Add(T("DelDoT"));
			if (homing.Count > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotHoming", string.Join("/", homing))));

			var pierce = new List<string>(6);
			if (hp.TilePierceBolt) pierce.Add(T("DelBolt"));
			if (hp.TilePierceSpread) pierce.Add(T("DelSpread"));
			if (hp.TilePierceBarrage) pierce.Add(T("DelBarrage"));
			if (hp.TilePierceDoTBind) pierce.Add(T("DelDoT"));
			if (hp.TilePierceBeam) pierce.Add(T("DelBeam"));
			if (pierce.Count > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotPierce", string.Join("/", pierce))));
		}

		private static void AppendAccessories(List<StatLine> lines, List<AccRow> accs)
		{
			lines.Add(new StatLine(StatLineKind.Header, T("SectionAcc")));
			if (accs.Count == 0)
			{
				lines.Add(new StatLine(StatLineKind.Inactive, T("AccNone")));
				return;
			}

			foreach (AccRow row in accs)
			{
				string text = row.Active
					? T("AccOn", row.Name)
					: T("AccOff", row.Name, row.Reason);
				lines.Add(new StatLine(row.Active ? StatLineKind.Active : StatLineKind.Inactive, text));
			}
		}

		private static void AppendRuntime(List<StatLine> lines, HenshinPlayer hp)
		{
			int before = lines.Count;
			lines.Add(new StatLine(StatLineKind.Header, T("SectionRuntime")));
			if (hp.MoxieStacks > 0)
				lines.Add(new StatLine(StatLineKind.Active, T("RunMoxie", hp.MoxieStacks, Pct(0.20f * hp.MoxieStacks))));
			if (hp.DashCooldown > 0)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunDashCd", Secs(hp.DashCooldown))));
			if (hp.LungeCooldown > 0)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunLungeCd", Secs(hp.LungeCooldown))));
			if (hp.FocusSash && hp.FocusSashCooldown > 0)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunSashCd", Secs(hp.FocusSashCooldown))));
			else if (hp.FocusSash)
				lines.Add(new StatLine(StatLineKind.Active, T("RunSashReady")));
			if (hp.ShellBellCooldown > 0)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunShellCd", Secs(hp.ShellBellCooldown))));
			if (hp.RockyHelmetCooldown > 0)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunRockyCd", Secs(hp.RockyHelmetCooldown))));
			if (hp.AftermathPenaltyTimer > 0)
				lines.Add(new StatLine(StatLineKind.Warn, T("RunAftermath", Secs(hp.AftermathPenaltyTimer), Pct(hp.AftermathPenaltyMult))));
			if (hp.GuardBonusTimer > 0)
				lines.Add(new StatLine(StatLineKind.Active, T("RunGuard", Secs(hp.GuardBonusTimer))));
			if (hp.SolarPowerDrain)
				lines.Add(new StatLine(StatLineKind.Warn, T("RunSolarDrain")));
			if (lines.Count == before + 1)
				lines.Add(new StatLine(StatLineKind.Inactive, T("RunNone")));
		}

		private readonly struct AccRow
		{
			public string Name { get; init; }
			public bool Active { get; init; }
			public string Reason { get; init; }
			public int Type { get; init; }
			public AccFamilyId Family { get; init; }
			public AccPiece Piece { get; init; }
		}

		private static List<AccRow> CollectAccessories(Player player, HenshinPlayer hp)
		{
			var rows = new List<AccRow>(8);
			var seen = new HashSet<int>();

			void TryAdd(Item item)
			{
				if (item == null || item.IsAir)
					return;
				if (item.ModItem is not HenshinAccItem acc)
					return;
				if (!seen.Add(item.type))
					return;

				AccFamilyDef def = HenshinAccCatalog.Get(acc.FamilyId);
				bool transformed = hp.IsTransformed;
				bool gateOk = HenshinAccItem.GateOk(def, hp);
				bool everstone = def is { WorksUntransformed: true };
				bool curseTag = acc.FamilyId == AccFamilyId.A11;
				bool active = everstone ? gateOk : transformed && gateOk;
				string reason = active
					? string.Empty
					: curseTag && !transformed
						? T("AccReasonCurseTag")
						: !transformed && !everstone
							? T("AccReasonForm")
							: !string.IsNullOrEmpty(def?.RequiredFormId) && (hp.CurrentForm == null || hp.CurrentForm.FormId != def.RequiredFormId)
								? T("AccReasonRequiredForm")
								: T("AccReasonResonance");

				rows.Add(new AccRow
				{
					Name = item.Name,
					Active = active,
					Reason = reason,
					Type = item.type,
					Family = acc.FamilyId,
					Piece = acc.Piece
				});
			}

			for (int i = 3; i <= 9; i++)
			{
				if (!player.IsItemSlotUnlockedAndUsable(i))
					continue;
				TryAdd(player.armor[i]);
			}

			AccessorySlotLoader loader = LoaderManager.Get<AccessorySlotLoader>();
			ModAccessorySlotPlayer extra = player.GetModPlayer<ModAccessorySlotPlayer>();
			for (int i = 0; i < extra.SlotCount; i++)
			{
				if (!loader.ModdedIsItemSlotUnlockedAndUsable(i, player))
					continue;
				ModAccessorySlot slot = loader.Get(i, player);
				if (slot == null)
					continue;
				TryAdd(slot.FunctionalItem);
			}

			return rows;
		}

		private static bool? PassiveConditionMet(Player player, HenshinPlayer hp, FormPassiveKind kind)
		{
			switch (kind)
			{
				case FormPassiveKind.Blaze:
				case FormPassiveKind.Torrent:
				case FormPassiveKind.Overgrow:
					return player.statLife <= player.statLifeMax2 * 0.5f;
				case FormPassiveKind.Chlorophyll:
					return Main.dayTime && player.ZoneOverworldHeight;
				case FormPassiveKind.RainDish:
				case FormPassiveKind.SwiftSwim:
					return Main.raining || !Main.dayTime;
				case FormPassiveKind.Multiscale:
					return player.statLife >= player.statLifeMax2;
				case FormPassiveKind.Guts:
					return HasAnyDebuff(player);
				case FormPassiveKind.SolarPower:
					return Main.dayTime;
				case FormPassiveKind.Moxie:
					return hp.MoxieStacks > 0 ? true : null;
				default:
					return null;
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

		private static bool ShowsFlight(FormDefinition form, HenshinPlayer hp)
			=> hp.LevitateFlight
				|| form.Primary == PokemonType.Flying
				|| form.Secondary == PokemonType.Flying;

		private static string Fingerprint(Player player, HenshinPlayer hp, List<AccRow> accs, bool transformed)
		{
			var sb = new StringBuilder(160);
			sb.Append(transformed ? '1' : '0').Append('|');
			sb.Append(hp.CurrentForm?.FormId ?? "-").Append('|');
			if (player.HeldItem?.ModItem is HenshinForceItem force)
			{
				sb.Append(force.Level).Append('|').Append(force.Xp).Append('|');
				sb.Append(force.Move1?.DamageMultiplier.ToString("0.###") ?? "-").Append('|');
				sb.Append(force.Move2?.DamageMultiplier.ToString("0.###") ?? "-").Append('|');
				sb.Append(force.Ultimate?.DamageMultiplier.ToString("0.###") ?? "-").Append('|');
			}

			sb.Append((int)hp.UltimateEnergy).Append('|');
			sb.Append((int)hp.FlightEnergy).Append('|');
			sb.Append(player.statLife).Append('|').Append(player.statDefense).Append('|');
			sb.Append(player.GetTotalCritChance(HenshinDamage.Instance).ToString("0.#")).Append('|');
			sb.Append(Main.dayTime ? 'D' : 'N').Append(Main.raining ? 'R' : '_');
			sb.Append(player.wet ? 'W' : '_').Append(player.ZoneOverworldHeight ? 'O' : '_').Append('|');
			sb.Append(hp.MoxieStacks).Append('|');
			sb.Append(hp.DashCooldown).Append('|').Append(hp.LungeCooldown).Append('|');
			sb.Append(hp.FocusSashCooldown).Append('|').Append(hp.AftermathPenaltyTimer).Append('|');
			sb.Append(hp.HenshinDamageBonus.ToString("0.###")).Append('|');
			sb.Append(hp.HenshinDamageFactorBonus.ToString("0.###")).Append('|');
			sb.Append(hp.ChoiceDamage.ToString("0.###")).Append('|');
			sb.Append(hp.LifeOrbDamage.ToString("0.###")).Append('|');
			sb.Append(hp.FormAtkMul.ToString("0.###")).Append('|');
			sb.Append(hp.FormDefMul.ToString("0.###")).Append('|');
			sb.Append(hp.UseTimeMul.ToString("0.###")).Append('|');
			sb.Append(hp.BossDamageBonus.ToString("0.###")).Append('|');
			sb.Append(hp.CritUpgradeChance.ToString("0.###")).Append('|');
			sb.Append(hp.OnFireCritUpgrade.ToString("0.###")).Append('|');
			sb.Append(hp.CritChanceBonus.ToString("0.###")).Append('|');
			sb.Append(hp.CritDamageBonus.ToString("0.###")).Append('|');
			sb.Append(hp.DodgeChance.ToString("0.###")).Append('|');
			sb.Append(hp.IncomingDamageMultiplier.ToString("0.###")).Append('|');
			foreach (AccRow row in accs)
			{
				sb.Append((int)row.Family).Append((int)row.Piece);
				sb.Append(row.Active ? '+' : '-');
			}

			return sb.ToString();
		}

		private static void AddSignedPct(List<StatLine> lines, string key, float value)
		{
			if (Math.Abs(value) < 0.0005f)
				return;
			lines.Add(KindBySign(value > 0f, T(key, SignedPct(value))));
		}

		private static StatLine KindBySign(bool good, string text)
			=> new(good ? StatLineKind.Body : StatLineKind.Warn, text);

		private static StatLine Cond(bool on, string onText, string offText)
			=> new(on ? StatLineKind.Active : StatLineKind.Inactive, on ? onText : offText);

		private static string TypeName(PokemonType type)
			=> T("Type." + type);

		private static string T(string key)
			=> Language.GetTextValue(Prefix + key);

		private static string T(string key, object arg0)
			=> Language.GetTextValue(Prefix + key, arg0);

		private static string T(string key, object arg0, object arg1)
			=> Language.GetTextValue(Prefix + key, arg0, arg1);

		private static string T(string key, object arg0, object arg1, object arg2)
			=> Language.GetTextValue(Prefix + key, arg0, arg1, arg2);

		/// <summary>小数比例 → 百分数文案（0.25 → 25）。</summary>
		private static string Pct(float v)
		{
			float p = v * 100f;
			if (Math.Abs(p - MathF.Round(p)) < 0.051f)
				return ((int)MathF.Round(p)).ToString();
			return p.ToString("0.#");
		}

		/// <summary>倍率 → 百分数（1.5 → 150）。</summary>
		private static string MultPct(float multiplier)
			=> Pct(multiplier);

		private static string SignedPct(float v)
		{
			string n = Pct(v);
			return v > 0f ? "+" + n : n;
		}

		private static string Num(float v)
		{
			if (Math.Abs(v - MathF.Round(v)) < 0.001f)
				return ((int)MathF.Round(v)).ToString();
			return v.ToString("0.##");
		}

		private static string Secs(float ticks)
			=> (ticks / 60f).ToString("0.#");
	}
}
