using System;
using System.Collections.Generic;
using System.Text;
using PokemonHenshin.Content.Accessories;
using PokemonHenshin.Content.Affinity;
using PokemonHenshin.Content.Combat;
using PokemonHenshin.Content.Core;
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
	/// 汇总「当前」加成与特性。不是图鉴。
	/// </summary>
	internal static class HenshinStatSheet
	{
		private const string Prefix = "Mods.PokemonHenshin.StatsUI.";

		public static HenshinStatSnapshot Capture(Player player)
		{
			var lines = new List<StatLine>(48);
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
			AppendCombatTotals(lines, hp);
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
				lines.Add(new StatLine(StatLineKind.Body, T("Skill1", Language.GetTextValue(m1.NameKey))));
			if (m2 != null)
			{
				lines.Add(new StatLine(hp.ChoiceLockSkill2 ? StatLineKind.Warn : StatLineKind.Body,
					hp.ChoiceLockSkill2
						? T("Skill2Locked", Language.GetTextValue(m2.NameKey))
						: T("Skill2", Language.GetTextValue(m2.NameKey))));
			}

			if (ult != null)
			{
				lines.Add(new StatLine(hp.ChoiceLockUlt ? StatLineKind.Warn : StatLineKind.Body,
					hp.ChoiceLockUlt
						? T("UltLocked", Language.GetTextValue(ult.NameKey))
						: T("Ult", Language.GetTextValue(ult.NameKey))));
			}
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
			float amp = 1f + hp.AffinityAmplitudeBonus;
			AppendTypePrimary(lines, player, hp, form.Primary, amp);
			if (form.Secondary != PokemonType.None)
				AppendTypeSecondary(lines, player, hp, form.Secondary, amp);
			if (hp.AffinityAmplitudeBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("AmpNote", Pct(hp.AffinityAmplitudeBonus))));
		}

		private static void AppendTypePrimary(List<StatLine> lines, Player player, HenshinPlayer hp, PokemonType type, float amp)
		{
			string typeName = TypeName(type);
			switch (type)
			{
				case PokemonType.Fire:
					lines.Add(Cond(ConditionEvaluator.Evaluate(player, ConditionId.InWater),
						T("TypeFireWet", typeName, Pct(0.10f * amp)),
						T("TypeFireOk", typeName)));
					break;
				case PokemonType.Water:
					lines.Add(Cond(player.wet,
						T("TypeWaterWet", typeName, Pct(0.25f * amp)),
						T("TypeWaterDry", typeName)));
					break;
				case PokemonType.Grass:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGrass", typeName)));
					break;
				case PokemonType.Electric:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeElectric", typeName, Pct(1f - hp.DashCooldownMultiplier))));
					break;
				case PokemonType.Flying:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeFlying", typeName, Pct(Math.Min(0.75f, 0.5f + hp.FallDamageReduction)))));
					break;
				case PokemonType.Ice:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeIce", typeName)));
					break;
				case PokemonType.Ground:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGround", typeName, Pct(0.40f * amp))));
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
					lines.Add(new StatLine(StatLineKind.Body, T("TypeDark", typeName, Num(5f * amp))));
					break;
				case PokemonType.Fighting:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeFighting", typeName, Pct(0.05f * amp))));
					break;
				case PokemonType.Psychic:
					lines.Add(new StatLine(StatLineKind.Body, T("TypePsychic", typeName)));
					break;
				case PokemonType.Bug:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeBug", typeName, Pct(0.10f * amp))));
					break;
				case PokemonType.Poison:
					lines.Add(new StatLine(StatLineKind.Body, T("TypePoison", typeName)));
					break;
				case PokemonType.Normal:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeNormal", typeName, Pct(0.03f * amp))));
					break;
				case PokemonType.Ghost:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeGhost", typeName)));
					break;
				case PokemonType.Fairy:
					lines.Add(new StatLine(StatLineKind.Inactive, T("TypeFairy", typeName)));
					break;
			}
		}

		private static void AppendTypeSecondary(List<StatLine> lines, Player player, HenshinPlayer hp, PokemonType type, float amp)
		{
			string typeName = TypeName(type);
			switch (type)
			{
				case PokemonType.Flying:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecFlying", typeName, Pct(Math.Min(0.75f, 0.5f + hp.FallDamageReduction)))));
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
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecGround", typeName, Pct(0.10f * amp))));
					break;
				case PokemonType.Dragon:
					lines.Add(new StatLine(StatLineKind.Body, T("TypeSecDragon", typeName)));
					break;
				case PokemonType.Water:
					lines.Add(Cond(player.wet,
						T("TypeSecWaterWet", typeName, Pct(0.05f * amp)),
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

		private static void AppendCombatTotals(List<StatLine> lines, HenshinPlayer hp)
		{
			int before = lines.Count;
			lines.Add(new StatLine(StatLineKind.Header, T("SectionCombat")));

			AddSignedPct(lines, "TotDamage", hp.HenshinDamageBonus);
			AddSignedPct(lines, "TotFactor", hp.HenshinDamageFactorBonus);
			AddSignedPct(lines, "TotChoice", hp.ChoiceDamage);
			AddSignedPct(lines, "TotLifeOrb", hp.LifeOrbDamage);
			AddSignedPct(lines, "TotTypeMove", hp.TypeMoveBonus);
			AddSignedPct(lines, "TotBoss", hp.BossDamageBonus);
			AddSignedPct(lines, "TotOnFire", hp.OnFireTargetBonus);
			AddSignedPct(lines, "TotUlt", hp.UltDamageBonus);
			AddSignedPct(lines, "TotMelee", hp.MeleeDeliveryDamage);
			AddSignedPct(lines, "TotFireMove", hp.FireMoveDamage);
			AddSignedPct(lines, "TotPsychicDragon", hp.PsychicDragonDamage);
			AddSignedPct(lines, "TotEvioliteDmg", hp.EvioliteDamage);
			AddSignedPct(lines, "TotEvioliteDef", hp.EvioliteDefMul);
			AddSignedPct(lines, "TotMoveSpeed", hp.MoveSpeedBonus);
			AddSignedPct(lines, "TotWaterSpeed", hp.WaterSpeedBonus);
			AddSignedPct(lines, "TotXpHeld", hp.XpHeldMul);
			AddSignedPct(lines, "TotXpShare", hp.XpHotbarShareMul);
			AddSignedPct(lines, "TotCrit", hp.CritUpgradeChance);
			AddSignedPct(lines, "TotOnFireCrit", hp.OnFireCritUpgrade);
			AddSignedPct(lines, "TotPassiveEnergy", hp.PassiveEnergyMul);

			if (Math.Abs(hp.IncomingDamageMultiplier - 1f) > 0.0005f)
				lines.Add(KindBySign(hp.IncomingDamageMultiplier < 1f, T("TotIncoming", Pct(hp.IncomingDamageMultiplier))));
			if (Math.Abs(hp.MoveCooldownMultiplier - 1f) > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotCooldown", Pct(hp.MoveCooldownMultiplier))));
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
			if (hp.FallDamageReduction > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotFall", Pct(hp.FallDamageReduction))));
			if (hp.PenetrateAdd > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotPenetrate", hp.PenetrateAdd)));
			if (hp.DashSpeedBonus > 0.0005f)
				lines.Add(new StatLine(StatLineKind.Body, T("TotDashSpeed", Num(hp.DashSpeedBonus))));
			if (hp.LungeIFrameBonus > 0)
				lines.Add(new StatLine(StatLineKind.Body, T("TotLungeIFrame", hp.LungeIFrameBonus)));
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

			if (lines.Count == before + 1)
				lines.Add(new StatLine(StatLineKind.Inactive, T("TotNone")));
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
			if (hp.TilePierceField) pierce.Add(T("DelField"));
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
			if (hp.AccGuardActiveTimer > 0)
				lines.Add(new StatLine(StatLineKind.Active, T("RunAccGuard", Secs(hp.AccGuardActiveTimer))));
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
				bool resOk = def == null || def.Resonance == PokemonType.None
					|| (hp.CurrentForm != null && (hp.CurrentForm.Primary == def.Resonance || hp.CurrentForm.Secondary == def.Resonance));
				bool everstone = def is { WorksUntransformed: true };
				bool curseTag = acc.FamilyId == AccFamilyId.A11;
				bool active = everstone ? resOk : transformed && resOk;
				string reason = active
					? string.Empty
					: curseTag && !transformed
						? T("AccReasonCurseTag")
						: !transformed && !everstone
							? T("AccReasonForm")
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
			var sb = new StringBuilder(128);
			sb.Append(transformed ? '1' : '0').Append('|');
			sb.Append(hp.CurrentForm?.FormId ?? "-").Append('|');
			if (player.HeldItem?.ModItem is HenshinForceItem force)
				sb.Append(force.Level).Append('|').Append(force.Xp).Append('|');
			sb.Append((int)hp.UltimateEnergy).Append('|');
			sb.Append((int)hp.FlightEnergy).Append('|');
			sb.Append(player.statLife).Append('|').Append(player.statDefense).Append('|');
			sb.Append(Main.dayTime ? 'D' : 'N').Append(Main.raining ? 'R' : '_');
			sb.Append(player.wet ? 'W' : '_').Append(player.ZoneOverworldHeight ? 'O' : '_').Append('|');
			sb.Append(hp.MoxieStacks).Append('|');
			sb.Append(hp.DashCooldown).Append('|').Append(hp.LungeCooldown).Append('|');
			sb.Append(hp.FocusSashCooldown).Append('|').Append(hp.AftermathPenaltyTimer).Append('|');
			sb.Append(hp.HenshinDamageBonus.ToString("0.###")).Append('|');
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

		private static string Pct(float v)
		{
			float p = v * 100f;
			if (Math.Abs(p - MathF.Round(p)) < 0.051f)
				return ((int)MathF.Round(p)).ToString();
			return p.ToString("0.#");
		}

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
