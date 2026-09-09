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
RECIPE_ZH = "合成：碎片一至四→成品；成品+五+六或一至六→超级。仅变身生效（不变之石除外）。"
RECIPE_EN = "Craft: shards I-IV → item; item+V+VI or I-VI → Super. Henshin only (Everstone excepted)."


def block_zh(legacy: str, zh: str, flavor: str) -> str:
	parts = []
	parts.append(item_zh(legacy, zh, flavor, "普通成品。"))
	for i, n in enumerate(ZH_NUM, start=1):
		parts.append(item_zh(f"{legacy}_S{i}", f"{zh}碎片{n}", flavor, f"碎片{n}：可装备，约成品四分之一强度。"))
	parts.append(item_zh(f"{legacy}_Super", f"超级{zh}", flavor, "超级形态，效果加倍。"))
	return "\n".join(parts)


def block_en(legacy: str, en: str, flavor: str) -> str:
	parts = []
	parts.append(item_en(legacy, en, flavor, "Finished accessory."))
	for i, n in enumerate(EN_NUM, start=1):
		parts.append(item_en(f"{legacy}_S{i}", f"{en} Shard {n}", flavor, f"Shard {n}: equippable, about 1/4 of the finished item."))
	parts.append(item_en(f"{legacy}_Super", f"Super {en}", flavor, "Super form; effects doubled."))
	return "\n".join(parts)


def item_zh(name: str, display: str, flavor: str, extra: str) -> str:
	return (
		f"\t{name}: {{\n"
		f"\t\tDisplayName: {display}\n"
		f"\t\tTooltip:\n"
		f"\t\t\t'''\n"
		f"\t\t\t{flavor}\n"
		f"\t\t\t{extra}\n"
		f"\t\t\t{RECIPE_ZH}\n"
		f"\t\t\t'''\n"
		f"\t}}\n"
	)


def item_en(name: str, display: str, flavor: str, extra: str) -> str:
	return (
		f"\t{name}: {{\n"
		f"\t\tDisplayName: {display}\n"
		f"\t\tTooltip:\n"
		f"\t\t\t'''\n"
		f"\t\t\t{flavor}\n"
		f"\t\t\t{extra}\n"
		f"\t\t\t{RECIPE_EN}\n"
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


def main() -> None:
	zh = ROOT / "Localization" / "zh-Hans_Mods.PokemonHenshin.hjson"
	en = ROOT / "Localization" / "en-US_Mods.PokemonHenshin.hjson"
	splice(zh, "zh")
	splice(en, "en")
	patch_common(zh, "zh")
	patch_common(en, "en")


if __name__ == "__main__":
	main()
