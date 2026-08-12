# Expanded Upgrade Systems & Balance — Addendum to GAME_DESIGN.md

Addresses two things: (1) bringing our upgrade depth up toward Obelisk's real complexity, (2) making sure that added depth doesn't shorten the game by accident.

## The balance rule (read this before touching any formula below)

**Within a tree: additive. Across trees: multiplicative.**

Every stat node inside ONE tree (e.g. all of Tap Power's sub-stats) sums together as a percentage bonus. The tree's final output then multiplies against every OTHER tree's output. This mirrors Obelisk's actual system directly and is the single most important rule for keeping this expansion from breaking pacing — it lets us add many trees for depth/variety without each one independently exploding player power.

```
FinalDamage = BaseDamage
            × (1 + TapTreeBonusSum)
            × (1 + CrewTreeBonusSum)
            × (1 + BottleTreeBonusSum)
            × (1 + CaptainsLogBonusSum)
            × (1 + PostcardBonusSum)
            × (1 + CompanionBonusSum)
            × CritMultiplier
```

Each "BonusSum" is the SUM of that tree's individual node bonuses (e.g. 5 nodes each giving +8% = +40% = ×1.4), not a product of them.

## Why this needed recalibrating the enemy curve too

Our original Armor/HP formulas (in GAME_DESIGN.md) were tuned assuming ONE multiplicative lever (tap level, quadratic). We're now adding roughly 6 more multiplicative trees. Even with additive-within/multiplicative-across containment, total achievable power at any given point in the game goes up substantially.

**Fix: don't just inflate enemy stats to compensate — gate tree availability over time**, the same way Obelisk staggers Cards (Obelisk 15) and Pets (Obelisk 17) rather than giving you everything on day one. This keeps early-game pacing untouched (new player only has 2-3 trees available, same difficulty curve as before) while late-game players — who've unlocked everything — face content balanced around that fuller power budget.

### Tree unlock gating (mirrors Obelisk's staggered rollout)

| Tree | Unlocks at |
|---|---|
| Tap Power (base) | Start (Wreck Beach) |
| Bottle Toss | Start (Wreck Beach) |
| Crew sub-trees (per character) | Same time each character is recruited |
| Tiered Crit (Splash → Super Splash → Mega Splash) | Debris Field (2nd cove) — a small mid-Wreck-Beach depth bump |
| Captain's Log (permanent skill tree) | End of Wreck Beach (first construction gate) — meant as a slow-burn whole-game goal, so it should start early but grow slowly |
| Postcards (collectible modifiers) | The Shallows |
| Companions (Feline, tamed serpents, etc.) | The Green onward |
| Trade Requests (rotating resource-bundle contracts) | The Bluffs onward |
| Monster-debuff upgrades (weaken serpent stats directly) | The Hollow onward — thematically fits "learning the serpents' weaknesses" as you go deeper |

**Secondary safety valve**: alongside gating, bump the armor exponential base slightly (2.8 → 3.0) starting from The Green onward specifically (not retroactively to Wreck Beach/Shallows, to avoid re-breaking early pacing we already validated in the prototype). This gives extra headroom for the point where 4+ trees are simultaneously active.

## Expanded Tap tree

Mirrors Obelisk's Pickaxe stat block:

- **Tap Power** (existing): `(level+1)(level+2)/2` — base damage per tap
- **Tap Speed**: reduces hold-to-tap interval / increases taps-per-second cap. `+4% per level` (additive within tree)
- **Tap Radius**: chance-based, lets one tap hit 2 adjacent driftwood pieces at once. `+3% chance per level` (additive)
- **Tiered Crit** (unlocks Debris Field onward):
  - Splash: `chance +2%/level, damage +15%/level` (both additive within their own sub-stat)
  - Super Splash (requires a successful Splash roll first): `chance +1%/level, damage +25%/level`
  - Mega Splash (requires a successful Super Splash roll first): `chance +0.5%/level, damage +40%/level`

Cost scaling for all Tap sub-stats: `cost = base × 1.15^level` (matches our existing Tap Power cost curve, kept consistent across the whole tree for predictability)

## Per-crew sub-trees

Instead of one shared "Crew Efficiency" Artifact, each recruited character gets their own small tree:

- **Output** — flat production rate, additive per level (existing)
- **Speed** — reduces the tick interval between production ticks
- **Specialty node** — unique per character, ties to their established personality mechanic:
  - BBW/BBC: Synergy Range — increases the bonus when both are active
  - Lucette: Tantrum Charge Rate — meter fills faster
  - Lucy: Double or Nothing Odds — better risk/reward ratio
  - The Feline: Light Efficiency — further reduces torch consumption

Cost scaling: `cost = base × 1.35^level` (steeper than Tap, matches our existing crew-recruit cost curve — keeps crew from being cheaper to stack than it should be relative to tapping)

## Captain's Log (permanent skill tree)

Survives prestige entirely (unlike Artifacts, which are bought WITH prestige currency but can still be leveled repeatedly — Captain's Log nodes are one-time unlocks). Spent using a new slow-trickle currency: **Log Pages**, earned in small amounts from milestones and serpent kills (not from tapping — this keeps it a long-term goal rather than something maxed via idle grinding).

Should contain 15-20 nodes for Wreck Beach's slice alone (small relative to Obelisk's tens of thousands of skill points, intentionally — ours is one biome's worth, more nodes get added as biomes unlock, same total-game scale as Obelisk reached over years of updates).

## Postcards (collectible modifiers)

Found in bottles (rare drop), duplicates level up the same Postcard rather than granting a new one. Each Postcard is a themed small bonus (e.g. "A Photo of Home" — +5% offline earnings per copy owned, capped at 10 copies = +50%). Additive within the Postcard collection as a whole, multiplies against other trees per the master rule.

## Companions

Distinct from Crew — passive-only, no active role, collected rather than recruited (from rare serpent-kill drops or deep exploration). Each grants one small passive multiplier. Kept deliberately simple (no sub-trees of their own) since their whole appeal is collection breadth, not individual depth — mirrors Obelisk Pets being numerous but individually shallow.

## Validating this before committing formulas permanently

Formulas above are a reasoned first pass, not final — the right move is adding this system into the existing browser prototype (it already has the multiplier/debug-speed tooling built for exactly this kind of testing) and actually playing through with these numbers active, watching whether time-to-clear each cove stays in a reasonable range as trees get added. Recommend treating every exponent/percentage in this doc as adjustable pending that playtest, not as locked numbers to hand to Unity yet.
