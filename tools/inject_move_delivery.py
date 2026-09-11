"""Inject MoveDelivery into FormItemUtil factories. Idempotent."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "Content" / "Items" / "Forms" / "StarterLines.cs"

DELIVERY = {
	"Slash": "MeleeArc",
	"Scratch": "MeleeArc",
	"VineWhip": "MeleeArc",
	"BiteArc": "MeleeArc",
	"CrossChopUlt": "MeleeArc",
	"CrossChopShort": "MeleeArc",
	"ShadowClawSlash": "MeleeArc",
	"DragonTailWhip": "MeleeArc",
	"IronTailWhip": "MeleeArc",
	"LickFan": "MeleeArc",
	"BrickBreak": "MeleeArc",
	"TripleStab": "MeleeArc",
	"FocusPunch": "MeleeArc",
	"CometPunch": "MeleeArc",
	"TakeDownUlt": "MeleeArc",
	"ZenHammer": "MeleeArc",
	"DynamicPunchUlt": "MeleeArc",
	"Lunge": "Lunge",
	"BraveBirdUlt": "Lunge",
	"AerialAceBlink": "Lunge",
	"BlinkStrike": "Lunge",
	"Dig": "Lunge",
	"DigUlt": "Lunge",
	"DragonDive": "Lunge",
	"DragonAscentUlt": "Lunge",
	"FlareBlitzUlt": "Lunge",
	"CloseCombatUlt": "Lunge",
	"Strike": "StrikeFall",
	"FireBolt": "Bolt",
	"WaterBolt": "Bolt",
	"Bolt": "Bolt",
	"FlareUlt": "Bolt",
	"AquaGun": "Bolt",
	"MudSlap": "Bolt",
	"MidThunder": "Bolt",
	"BigShadowBall": "Bolt",
	"SludgeBolt": "Bolt",
	"StrongPsychic": "Bolt",
	"AlakazamPsychic": "Bolt",
	"MewtwoPsychic": "Bolt",
	"SeedBombUlt": "Bolt",
	"GroundCyclone": "Bolt",
	"PeckCone": "Spread",
	"FlameCone": "Spread",
	"LeafSpread": "Spread",
	"DragonBreath": "Spread",
	"DarkPulseCone": "Spread",
	"AirBurst": "Spread",
	"BubbleBarrageUlt": "Barrage",
	"BubbleBarrage": "Barrage",
	"SeedBarrageUlt": "Barrage",
	"DragonRage": "Barrage",
	"MeteorBarrageUlt": "Barrage",
	"RockSlideX": "Barrage",
	"FlailUlt": "Barrage",
	"MewtwoPsystrikeUlt": "Barrage",
	"ResonanceScatterUlt": "Barrage",
	"OutrageUlt": "Barrage",
	"StoneEdge": "Barrage",
	"Beam": "Beam",
	"ThickBeam": "Beam",
	"SustainedBeam": "Beam",
	"WaterJet": "Beam",
	"ChargeBeamUlt": "Beam",
	"ThunderboltUlt": "Beam",
	"ThunderPillarUlt": "Beam",
	"DragonPulse": "Beam",
	"AoE": "AoEBurst",
	"MouseAoE": "AoEBurst",
	"FutureSightUlt": "AoEBurst",
	"QuakeUlt": "AoEBurst",
	"RockTomb": "AoEBurst",
	"SkyAttack": "AoEBurst",
	"PetalDance": "Field",
	"HurricaneField": "Field",
	"WeatherPainHurricane": "Barrage",
	"Field": "Field",
	"Sleep": "Field",
	"HypnosisUlt": "Field",
	"Vortex": "DoTBind",
	"MouseVortex": "DoTBind",
}

PATTERN = re.compile(r"public static MoveSpec (\w+)\(")


def main() -> None:
	text = PATH.read_text(encoding="utf-8")
	injected = 0
	missing = []
	delegates = []
	out = []
	last = 0
	for m in PATTERN.finditer(text):
		name = m.group(1)
		# Find the method body's first statement after the signature's closing paren.
		paren_end = text.find(")", m.end() - 1)
		arrow = text.find("=>", paren_end)
		if arrow < 0 or arrow > m.end() + 400:
			continue
		body = text[arrow:arrow + 40]
		if "new()" not in body:
			delegates.append(name)
			continue
		brace = text.find("{", arrow)
		if brace < 0:
			continue
		insert_at = brace + 1
		lookahead = text[insert_at:insert_at + 120]
		delivery = DELIVERY.get(name)
		if delivery is None:
			missing.append(name)
			continue
		out.append(text[last:insert_at])
		if "Delivery =" not in lookahead:
			out.append(f"\n\t\t\t\tDelivery = MoveDelivery.{delivery},")
			injected += 1
		last = insert_at
	out.append(text[last:])
	if missing:
		raise SystemExit("unmapped FormItemUtil factories: " + ", ".join(missing))
	PATH.write_text("".join(out), encoding="utf-8")
	print(f"injected Delivery into {injected} factories; delegates={delegates}")


if __name__ == "__main__":
	main()
