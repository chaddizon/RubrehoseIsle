# Rubrehose Isle — Game Design Reference

Idle/incremental mobile game (iOS + Android via Unity), mirroring Idle Obelisk Miner's proven progression structure, themed around a shipwrecked crew rebuilding a deserted island. Rubberhose/1930s cartoon art style, monochrome-leaning palette, brand catchphrase "BE BAD!"

## Cast
- **BBW** (Big Bad Wolf) & **BBC** (Big Bad Coyote) — thick-as-thieves duo, synergy bonus when both active
- **Lucy** (short for Lucifer) — most devilish, risk-mechanic character ("Double or Nothing")
- **Lucette** — shy, paper-bag head, upside-down cross eyes; tantrum meter = burst-damage mechanic
- **Tuggy** — the S.S. Rubrehose, tugboat; prestige mechanic anchor
- **The Feline** — nameless side character, reduces torch consumption in The Hollow
- Side/nameless: sea serpent, fish, skateboarding hand, recurring bottle prop
- Design motifs: disembodied eyes, heart-glasses w/ 3 eyes, checkerboard pattern

## Core loop
Tap to salvage → recruit crew (passive generators) → spend currency on upgrades/construction → beat serpent boss gate → unlock next biome → repeat. Prestige ("Tuggy's Supply Run") resets economic stats for permanent multipliers, keeps all biome/zone progress.

## Structure hierarchy (mirrors Obelisk's Floor > Zone > World)
- **Biome** (= World equivalent): permanent once unlocked, never resets
- **Cove** (= Floor equivalent): 3 per biome, each with repeated serpent encounters, escalating clear requirements
- **Construction gate**: resource-sink between biomes (separate from combat), consumes biome currency + material

## The 6 biomes

| # | Biome | Currency | Construction material | Unlocks |
|---|---|---|---|---|
| 1 | Wreck Beach | Driftwood | Bound Rope | Tap Power, Cast a Net / Bottle Toss, BBW/BBC |
| 2 | The Shallows | Shells/Pearls | Coral Fragments | Tidepooling, Lucette |
| 3 | The Green | Forage Tokens | Hardwood | Foraging, BBW/BBC synergy |
| 4 | The Bluffs | Message Fragments | Bluff Stone | Full Message-in-a-Bottle, Lucy |
| 5 | The Hollow | Cave Glimmer | Rare Crystal | Light-management mechanic, The Feline |
| 6 | The Deep Reef | Sea Glass | — (finale) | Full Serpent Combat tree |

Each biome = 3 coves (e.g. Wreck Beach: Landing Cove → Debris Field → Low Tide Flats), each cove gated by that cove's serpent boss — persistent HP across attempts (never resets except when advancing to a fresh cove), unlimited attempts, gated only by a real-time cooldown between attempts. One defeat is enough to reveal the crossing; see "Pacing" below for why that single fight still takes real days.

## Serpent tiers
Hatchling → Shoal-back → Tide-coiled → Bramblefang → Storm-wound → Cave-blind → Abyssal Coil (one per biome, roughly). Cave-blind (The Hollow) uniquely uses a rhythm/timing-based fight rather than visible-target tapping, since it's blind.

## Core formulas
```
Armor(biome, cove) = round(15 × 2.8^(cove_index) × BiomeMultiplier)
HP(biome, cove)    = round(36000 × 2.5^(cove_index) × BiomeMultiplier)
BiomeMultiplier: [1, 9.5, 90.25, 857.4, 8145, 77380]  (×9.5 step-jump per biome)

Salvage/Tap Power(level) = (level+1) × (level+2) / 2   [quadratic]

ClearMult (construction cost/reward scaling only, NOT a repeat-clear counter — see
"Pacing" below): [5, 15, 35, 105, 315, 945]  (×~3 harsher per biome)

Fight Duration = 30 seconds base, extendable via rare late-game upgrade
Cooldown = 20 minutes between attempts on same cove's serpent (reducible via upgrades)
```

## Pacing (Landing Cove, deliberately slow)

The Armor/HP coefficients above (15 / 36000, up from an earlier 8 / 50) are tuned so a
single defeat of Landing Cove's serpent takes on the order of 50-60 real attempts, not
one. Combined with the real 20-minute cooldown and unlimited attempts (persistent HP
across all of them), that's a floor of several real days even for someone hitting every
cooldown window during all their waking hours, and multiple weeks for a couple-check-ins-
a-day casual player — both playstyles valid, active just faster. This is intentional, not
a placeholder: progression across zones needs to stay slow both for art-production pacing
(new biome art can't be produced as fast as players would otherwise clear zones) and to
make the game a long-term investment in Obelisk's mold rather than something to blow
through in an afternoon. These two coefficients are a first-pass estimate, not validated
against real elapsed-time playtesting — expect to retune them (only them; the per-cove/
per-biome growth curves are intentionally left alone) once real clear times are observed.
A crew→tap-power or crew→fight-damage bonus (referenced but not yet implemented — see
`IN_SCENE_FIGHT_SYSTEM.md`'s "crewSubBonusSum" mention) is planned as part of a separate
upgrade tree, not this pacing pass.

## Side-loops (mirror Obelisk's Archaeology/Fishing/Stargazing)

**Tidepooling** (unlocks: The Shallows) — self-contained sub-loop, runs passively in background. Own stat-point system: Grip (find rate) / Patience (offline accumulation) / Luck (rare-totem chance). Rare finds carved into permanent-buff Totems. "Deep Tide" ascension resets this system specifically for deeper pools.

**Foraging** (unlocks: The Green) — multiple groves, each fills a tick meter over time; harvest chance = Forage Power ÷ Plant Rarity, chances above 100% roll bonus harvests. Recipes trade harvested ingredients for tokens, get pickier as more are completed. Legendary Bloom per grove once overcapped on commons.

**Message in a Bottle** (unlocks: Wreck Beach, expands at The Bluffs) — cast bottles, they wash ashore at random moments within a tide window (not a flat timer); must be actively collected before drifting back out. Tide changes every 20–30 min shift the reward pool. Rare "Captain's Bottle" spawn chance = jackpot layer (big fragment/recruit haul).

## Prestige — "Tuggy's Supply Run"
Resets: tap power, crew levels, current driftwood. Does NOT reset: biome/zone unlocks, Tidepooling/Foraging/Bottle progress, Artifacts. Grants **Compass Shards**, spent on permanent multiplier **Artifacts**:
- Tap Mastery — +15% tap power/level
- Crew Efficiency — +15% crew output/level
- Serpent Slayer — +10% fight damage/level
- Deep Pockets — +20% offline earnings/level

A genuinely new island (fresh palette, harder baseline) is separate, rare, endgame-only content — unlocked after fully maxing the current island. Not part of routine prestige.

## Crew roles

| Character | Recruited at | Role | Signature ability |
|---|---|---|---|
| BBW | Wreck Beach | Auto-scavenger | Synergy bonus w/ BBC |
| BBC | Wreck Beach | Auto-scavenger | Synergy bonus w/ BBW |
| Lucette | The Shallows | Slow tidepool collector | Tantrum meter → burst release |
| Lucy | The Bluffs | Boosts bottle catch rate | Double or Nothing risk mechanic |
| The Feline | The Hollow | Cave Glimmer collector | Reduces torch/light consumption |

## Monetization
Optional IAPs. Optional ads (opt-in, not forced). No mandatory ads — matches Obelisk's trust-building approach.

## UI/art direction
NOT a copy of Obelisk's UI (icon-grid-behind-a-button + Pins system). Full Rubrehose visual identity instead:
- Comic-panel drawer for menus, not a generic icon grid
- Character-illustrated menu icons (e.g. Lucette's bag-head for Tidepooling)
- Hand-lettered signage style for stat readouts, embedded in the scene rather than floating UI chrome
- Disembodied-eyes / heart-glasses motifs as rare-event/critical/jackpot indicators
- "BE BAD!" lettering style applied to action buttons
- Checkerboard pattern as a recurring functional UI element
- **Palette (LOCKED, revised): characters AND backgrounds are monochrome, world-object props stay colorful.** Characters = BBW, BBC, Lucy, Lucette, The Feline, Tuggy, the hermit crab, and the bottle (counts as a character due to its recurring hand-holding-bottle brand imagery) — all black/white/grayscale. Backgrounds (cove scenes, e.g. Landing Cove's) are also monochrome/grayscale now, unifying the backdrop with the cast. Props sitting in the world — the hut, campfire, driftwood, the flag marker — stay full color. This flips the original contrast logic: now it's the colorful, interactive props that pop against a monochrome cast-and-backdrop, rather than a colorful world contrasting a monochrome cast.

## Animation approach
Pivoted from vector illustration to pixel art mid-project (see WRECK_BEACH_CHECKLIST.md note). Procedural animation still applies for position-only movement (e.g. Tuggy's idle float/bob), but position must snap to whole-pixel increments each frame to avoid shimmer/blur — fractional/smooth movement breaks pixel art even with Point-filter texture import. For anything involving shape change (flame flicker, cloth flutter, walk cycles), use short hand-drawn frame loops instead of code-driven scaling/skewing, which blurs pixel art. Established frame-count conventions so far: ambient idle effects (campfire, flag flutter) ~3-5 frames; character walk cycles ~3-4 frames. Real rigged animation (Rive or Spine) remains a possible future upgrade but is now a lower priority given the pixel-art direction's own frame-based conventions are working well.

## Tech stack
- **Engine**: Unity (C#), for iOS + Android
- **Build split**: Claude writes code via Claude Code (Local session), Chad handles Unity Editor assembly (dragging in art, wiring scenes) and art production
- **Reference files in this repo**: `rubrehose_prototype.html` (playable browser prototype of the full loop, side-systems, and prestige — built to validate mechanics feel, not to be ported directly), `rubrehose_art_checklist.md` (phased art asset list by biome)

## Status as of this doc
All game systems above are fully designed and locked. Browser prototype built and tested. Art checklist created. Next step: scaffold real Unity project structure and begin implementing Wreck Beach (Phase 1 / MVP vertical slice) in C#.
