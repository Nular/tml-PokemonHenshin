"""Generate accessory DisplayName/Tooltip hjson blocks and splice into loc files."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

FAMILIES = [
	("A01AbilityCapsuleBelt", "特性胶囊", "Ability Capsule", "如果用于有着２种特性的宝可梦，就能令其现有特性变为另一种的胶囊。", "A capsule that switches a Pokémon's Ability."),
	("A02MuscleBand", "力量头带", "Muscle Band", "力如泉涌的头带。携带后，物理招式的威力就会少量提高。", "An exciting band that slightly boosts physical moves."),
	("A03SoulDewPendant", "心之水滴", "Soul Dew", "让拉帝欧斯或拉帝亚斯携带后，超能力和龙属性的招式威力就会提高的神奇珠子。", "A wondrous orb that powers Psychic and Dragon moves."),
	("A04FloatStoneAnklet", "轻石", "Float Stone", "非常轻的石头。携带后，宝可梦的体重会变轻。", "An extremely light stone that reduces weight."),
	("A05FocusSashBadge", "气势头带", "Focus Band", "携带后，即便受到可能会导致濒死的招式，也能够以微弱的ＨＰ撑过去。", "An item that may allow the holder to endure a potential KO."),
	("A06LifeOrbCore", "达人带", "Expert Belt", "又结实又厚的带子。携带后，效果绝佳的招式威力就会提高。", "A well-worn belt that boosts super-effective moves."),
	("A07CharcoalBag", "木炭", "Charcoal", "焚烧用的燃料。携带后，火属性的招式威力就会提高。", "Fuel that boosts Fire-type moves. Fire resonance."),
	("A08MysticWaterPouch", "神秘水滴", "Mystic Water", "水滴形状的宝石。携带后，水属性的招式威力就会提高。", "A teardrop gem that boosts Water-type moves."),
	("A09MagnetChip", "磁铁", "Magnet", "强力的磁铁。携带后，电属性的招式威力就会提高。", "A powerful magnet that boosts Electric-type moves."),
	("A10SharpBeakMembrane", "锐利鸟嘴", "Sharp Beak", "又长又尖的鸟嘴。携带后，飞行属性的招式威力就会提高。", "A long, sharp beak that boosts Flying-type moves."),
	("A11SpellTagCloth", "诅咒之符", "Spell Tag", "古怪可怕的咒符。携带后，幽灵属性的招式威力就会提高。", "A sinister tag. Ghost resonance: selected projectiles pierce tiles."),
	("A12DragonFangCharm", "龙之牙", "Dragon Fang", "坚硬锐利的牙齿。携带后，龙属性的招式威力就会提高。", "A hard, sharp fang that boosts Dragon-type moves."),
	("A13WideLens", "广角镜", "Wide Lens", "可以看到远处微小东西的镜片。携带后，招式的命中率就会少量提高。", "A lens for spotting distant things. Grants homing on Bolt/Spread/Barrage/DoTBind (not Beam)."),
	("A14ChoiceBand", "讲究头带", "Choice Band", "带着讲究的头带。虽然攻击会提高，但只能使出相同的招式。", "Boosts attack but locks other combat slots depending on the piece."),
	("A15ScopeLens", "焦点镜", "Scope Lens", "能看清弱点的镜片。携带后，变得容易击中要害。", "A lens for spotting weak points. Raises crit-tier upgrade chance."),
	("A16LifeOrb", "生命宝珠", "Life Orb", "携带后，虽然每次攻击时ＨＰ少量减少，但招式的威力会提高。", "Boosts move power at the cost of some HP."),
	("A17ShellBell", "贝壳之铃", "Shell Bell", "能听到悦耳声音的贝壳。攻击命中时，能回复少量ＨＰ。", "A soothing seashell. Recovers HP when attacks hit."),
	("A18RockyHelmet", "凸凸头盔", "Rocky Helmet", "让对手受伤的头盔。受到直接攻击时，会让对手也受伤。", "Hurts attackers that make contact."),
	("A19ChargeBelt", "充电电池", "Cell Battery", "充满电的电池。受到电属性招式攻击时，攻击会提高。", "A charged battery. Increases energy gained from combat."),
	("A20EchoPendant", "光之黏土", "Light Clay", "能延长墙类招式效果的神奇粘土。", "Strange clay that extends lingering effects. Keeps some ultimate energy."),
	("A21BurstArmband", "弱点保险", "Weakness Policy", "被效果绝佳的招式击中时，攻击和特攻就会大幅提高。", "Raises ultimate damage at the cost of energy gain."),
	("A22ExpShare", "学习装置", "Exp. Share", "能让同行的全体宝可梦获得经验值的装置。", "Shares a copy of XP to other force items on hotbar slots 0-9."),
	("A23LuckyEgg", "幸运蛋", "Lucky Egg", "能带来幸福的蛋。携带后，获得的经验值会少量增加。", "A lucky egg that increases XP gained by the held force."),
	("A24Everstone", "不变之石", "Everstone", "接触到后就会变得无法进化的神奇石头。", "A stone that prevents evolution while worn, even untransformed."),
	("A25BlackBelt", "黑带", "Black Belt", "能振作精神的带子。携带后，格斗属性的招式威力就会提高。", "A belt that powers short-range melee deliveries (no Fighting resonance)."),
	("A26Leftovers", "吃剩的东西", "Leftovers", "宝可梦的吃剩的东西。携带后，能在战斗中慢慢回复ＨＰ。", "Leftovers that slowly restore HP in battle."),
	("A27FocusSash", "气势披带", "Focus Sash", "带着气势的头巾。在ＨＰ全满时，即便受到可能会导致濒死的招式，也能仅以１ＨＰ撑过去１次。", "Endures a would-be KO if HP is above a threshold, then cools down."),
	("A28Eviolite", "进化奇石", "Eviolite", "有点不可思议的进化石。携带后，还能进化的宝可梦的防御和特防就会提高。", "Raises defense of forms that can still evolve."),
]

ZH_NUM = "一二三四五六"
EN_NUM = ("I", "II", "III", "IV", "V", "VI")


def block_zh(legacy: str, zh: str, flavor: str) -> str:
	parts = []
	parts.append(item_zh(legacy, zh, flavor))
	for i, n in enumerate(ZH_NUM, start=1):
		parts.append(item_zh(f"{legacy}_S{i}", f"{zh}碎片{n}", flavor))
	parts.append(item_zh(f"{legacy}_Super", f"超级{zh}", flavor))
	return "\n".join(parts)


def block_en(legacy: str, en: str, flavor: str) -> str:
	parts = []
	parts.append(item_en(legacy, en, flavor))
	for i, n in enumerate(EN_NUM, start=1):
		parts.append(item_en(f"{legacy}_S{i}", f"{en} Shard {n}", flavor))
	parts.append(item_en(f"{legacy}_Super", f"Super {en}", flavor))
	return "\n".join(parts)


def item_zh(name: str, display: str, flavor: str) -> str:
	return (
		f"\t{name}: {{\n"
		f"\t\tDisplayName: {display}\n"
		f"\t\tTooltip:\n"
		f"\t\t\t'''\n"
		f"\t\t\t{flavor}\n"
		f"\t\t\t'''\n"
		f"\t}}\n"
	)


def item_en(name: str, display: str, flavor: str) -> str:
	return (
		f"\t{name}: {{\n"
		f"\t\tDisplayName: {display}\n"
		f"\t\tTooltip:\n"
		f"\t\t\t'''\n"
		f"\t\t\t{flavor}\n"
		f"\t\t\t'''\n"
		f"\t}}\n"
	)


def splice(path: Path, lang: str) -> None:
	text = path.read_text(encoding="utf-8")
	start = text.find("\tA01AbilityCapsuleBelt:")
	end = text.find("\n}\n\nProjectiles:")
	if start < 0 or end < 0:
		raise SystemExit(f"cannot splice {path}")
	blocks = []
	for legacy, zh, en, flavor_zh, flavor_en in FAMILIES:
		blocks.append(block_zh(legacy, zh, flavor_zh) if lang == "zh" else block_en(legacy, en, flavor_en))
	path.write_text(text[:start] + "".join(blocks) + text[end + 1 :], encoding="utf-8")
	print("spliced", path, "families", len(FAMILIES))


ACC_STATS_ZH = """
	ResonanceLine: 共鸣：{0}
	TypeFire: 火
	TypeWater: 水
	TypeElectric: 电
	TypeFlying: 飞行
	TypeGhost: 幽灵
	TypeDragon: 龙
	StatDamageBonus: 招式伤害 +{0}%
	StatDamageFactorBonus: 变身伤害因子 +{0}%
	StatMeleeDeliveryDamage: 近战招式 +{0}%
	StatBossDamageBonus: 对Boss伤害 +{0}%
	StatOnFireTargetBonus: 对燃烧目标 +{0}%
	StatUltDamageBonus: 大招伤害 +{0}%
	StatIncomingCut: 受到伤害 -{0}%
	StatCooldownCut: 招式冷却 -{0}%
	StatDashCooldownCut: 冲刺冷却 -{0}%
	StatLungeCooldownCut: 撞击冷却 -{0}%
	StatMoveSpeedBonus: 移动速度 +{0}%
	StatWaterSpeedBonus: 水中移速 +{0}%
	StatFlightEnergySec: 飞行能量 +{0} 秒
	StatFallDmgTakenMul: 坠落承伤 ×{0}%
	StatAffinityAmp: 属性克制幅度 +{0}%
	StatEnergyGainAdd: 战斗能量获取 +{0}%
	StatEnergyGainMul: 战斗能量获取 ×{0}%
	StatUltRetain: 大招后保留能量 {0}%
	StatXpHeldMul: 持握经验 +{0}%
	StatXpHotbarShareMul: 热键栏其它之力复制经验 {0}%
	StatHomingTurn: 追踪转向 {0}
	StatHomingBolt: 追踪：单体飞行弹
	StatHomingSpread: 追踪：扇形/锥
	StatHomingBarrage: 追踪：连发弹幕
	StatHomingDoTBind: 追踪：缠绕弹
	StatTilePierceBolt: 穿墙：单体飞行弹
	StatTilePierceSpread: 穿墙：扇形/锥
	StatTilePierceBarrage: 穿墙：连发弹幕
	StatTilePierceDoTBind: 穿墙：缠绕弹
	StatTilePierceBeam: 穿墙：直线光束
	StatPenetrateAdd: 穿透 +{0}
	StatChoiceLockSkill2: 锁定技能2
	StatChoiceLockUlt: 锁定大招
	StatChoiceDamage: 招式伤害 +{0}%
	StatLifeOrbDamage: 招式伤害 +{0}%
	StatLifeOrbHpDrain: 攻击时扣除 1 HP
	StatLifeOrbGateTicks: 扣血间隔 {0} 秒
	StatShellBellHeal: 命中回复 {0} HP
	StatShellBellCdTicks: 贝壳之铃冷却 {0} 秒
	StatRockyHelmetScale: 受击反伤 {0}% 持握攻击
	StatRockyHelmetCdTicks: 反伤冷却 {0} 秒
	StatLeftoversHpPerSec: 每秒回复 {0} HP
	StatLeftoversLowHpBonus: 半血以下额外每秒回复 {0} HP
	StatFocusSash: 可抵一次致死伤害
	StatFocusSashHpPct: 触发门槛：当前 HP ≥ {0}%
	StatFocusSashCdSec: 气势披带冷却 {0} 秒
	StatFocusSashImmuneTicks: 触发后无敌 {0} 秒
	StatEvioliteDefMul: 还能进化时防御 +{0}%
	StatEvioliteDamage: 还能进化时伤害 +{0}%
	StatEverstoneBlock: 阻止进化
	StatCritUpgradeChance: 暴击升级率 +{0}%
	StatOnFireCritUpgrade: 对燃烧目标暴击升级 +{0}%
	StatFireMoveDamage: 火招式 +{0}%
	StatPassiveEnergyMul: 被动充能 +{0}%
	StatDashSpeedBonus: 冲刺速度 +{0}
	StatLungeIFrameBonus: 撞击无敌帧 +{0}
	StatPsychicDragonDamage: 超能/龙招式 +{0}%
	StatGuardCutTimer: 受击后 {0} 秒额外减伤
"""

ACC_STATS_EN = """
	ResonanceLine: Resonance: {0}
	TypeFire: Fire
	TypeWater: Water
	TypeElectric: Electric
	TypeFlying: Flying
	TypeGhost: Ghost
	TypeDragon: Dragon
	StatDamageBonus: Move damage +{0}%
	StatDamageFactorBonus: Henshin damage factor +{0}%
	StatMeleeDeliveryDamage: Melee moves +{0}%
	StatBossDamageBonus: Boss damage +{0}%
	StatOnFireTargetBonus: Vs burning targets +{0}%
	StatUltDamageBonus: Ultimate damage +{0}%
	StatIncomingCut: Damage taken -{0}%
	StatCooldownCut: Move cooldown -{0}%
	StatDashCooldownCut: Dash cooldown -{0}%
	StatLungeCooldownCut: Lunge cooldown -{0}%
	StatMoveSpeedBonus: Movement speed +{0}%
	StatWaterSpeedBonus: Water speed +{0}%
	StatFlightEnergySec: Flight energy +{0}s
	StatFallDmgTakenMul: Fall damage taken ×{0}%
	StatAffinityAmp: Type-match amplitude +{0}%
	StatEnergyGainAdd: Combat energy gain +{0}%
	StatEnergyGainMul: Combat energy gain ×{0}%
	StatUltRetain: Keep {0}% ultimate energy
	StatXpHeldMul: Held XP +{0}%
	StatXpHotbarShareMul: Copy {0}% XP to other hotbar forces
	StatHomingTurn: Homing turn {0}
	StatHomingBolt: Homing: bolts
	StatHomingSpread: Homing: spread
	StatHomingBarrage: Homing: barrage
	StatHomingDoTBind: Homing: bind shots
	StatTilePierceBolt: Tile pierce: bolts
	StatTilePierceSpread: Tile pierce: spread
	StatTilePierceBarrage: Tile pierce: barrage
	StatTilePierceDoTBind: Tile pierce: bind shots
	StatTilePierceBeam: Tile pierce: beams
	StatPenetrateAdd: Pierce +{0}
	StatChoiceLockSkill2: Locks skill 2
	StatChoiceLockUlt: Locks ultimate
	StatChoiceDamage: Move damage +{0}%
	StatLifeOrbDamage: Move damage +{0}%
	StatLifeOrbHpDrain: Attacks cost 1 HP
	StatLifeOrbGateTicks: HP drain interval {0}s
	StatShellBellHeal: On-hit heal {0} HP
	StatShellBellCdTicks: Shell Bell cooldown {0}s
	StatRockyHelmetScale: On-hit thorns {0}% of held attack
	StatRockyHelmetCdTicks: Thorns cooldown {0}s
	StatLeftoversHpPerSec: Regen {0} HP/s
	StatLeftoversLowHpBonus: Extra {0} HP/s below half HP
	StatFocusSash: Endure a lethal hit
	StatFocusSashHpPct: Endure if current HP ≥ {0}%
	StatFocusSashCdSec: Focus Sash cooldown {0}s
	StatFocusSashImmuneTicks: I-frames after trigger {0}s
	StatEvioliteDefMul: Defense +{0}% if it can still evolve
	StatEvioliteDamage: Damage +{0}% if it can still evolve
	StatEverstoneBlock: Blocks evolution
	StatCritUpgradeChance: Crit-upgrade chance +{0}%
	StatOnFireCritUpgrade: Crit-upgrade vs burning +{0}%
	StatFireMoveDamage: Fire moves +{0}%
	StatPassiveEnergyMul: Passive energy +{0}%
	StatDashSpeedBonus: Dash speed +{0}
	StatLungeIFrameBonus: Lunge i-frames +{0}
	StatPsychicDragonDamage: Psychic/Dragon moves +{0}%
	StatGuardCutTimer: Extra damage cut for {0}s after being hit
"""


def patch_stats(path: Path, lang: str) -> None:
	text = path.read_text(encoding="utf-8")
	if "StatDamageBonus:" in text:
		return
	block = ACC_STATS_ZH if lang == "zh" else ACC_STATS_EN
	needle = "\tShardTag: 碎片 {0}/6\n" if lang == "zh" else "\tShardTag: Shard {0}/6\n"
	if needle not in text:
		raise SystemExit(f"cannot patch stats in {path}")
	text = text.replace(needle, needle + block, 1)
	path.write_text(text, encoding="utf-8")
	print("stats", path)


def patch_common(path: Path, lang: str) -> None:
	text = path.read_text(encoding="utf-8")
	if "ForceMeleeBonus" not in text:
		if lang == "zh":
			text = text.replace(
				"\tForceDefenseNote:",
				"\tForceMeleeBonus: 近战招式 +{0}%\n\tForceBossBonus: 对Boss命中 +{0}%\n\tEverstoneBlocked: 不变之石阻止了进化\n\tForceDefenseNote:",
			)
			text = text.replace(
				"\tInactiveTag: 【仅变身生效·当前未生效】",
				"\tInactiveTag: 【仅变身生效·当前未生效】\n\tAlwaysActiveTag: 【戴着即生效·当前生效】\n\tAlwaysInactiveTag: 【戴着即生效·当前未生效】\n\tShardTag: 碎片 {0}/6",
			)
		else:
			text = text.replace(
				"\tForceDefenseNote:",
				"\tForceMeleeBonus: Melee moves +{0}%\n\tForceBossBonus: Boss hit +{0}%\n\tEverstoneBlocked: Everstone is blocking evolution\n\tForceDefenseNote:",
			)
			text = text.replace(
				'\tInactiveTag: "[Henshin only - inactive]"',
				'\tInactiveTag: "[Henshin only - inactive]"\n\tAlwaysActiveTag: "[Always on - ACTIVE]"\n\tAlwaysInactiveTag: "[Always on - inactive]"\n\tShardTag: Shard {0}/6',
			)
		path.write_text(text, encoding="utf-8")
	patch_stats(path, lang)


def main() -> None:
	zh = ROOT / "Localization" / "zh-Hans_Mods.PokemonHenshin.hjson"
	en = ROOT / "Localization" / "en-US_Mods.PokemonHenshin.hjson"
	splice(zh, "zh")
	splice(en, "en")
	patch_common(zh, "zh")
	patch_common(en, "en")


if __name__ == "__main__":
	main()
