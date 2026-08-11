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
| 1 | Wreck Beach | Driftwood | Bound Rope | Tap Power, Bottle Toss, BBW/BBC |
| 2 | The Shallows | Shells/Pearls | Coral Fragments | Tidepooling, Lucette |
| 3 | The Green | Forage Tokens | Hardwood | Foraging, BBW/BBC synergy |
| 4 | The Bluffs | Message Fragments | Bluff Stone | Full Message-in-a-Bottle, Lucy |
| 5 | The Hollow | Cave Glimmer | Rare Crystal | Light-management mechanic, The Feline |
| 6 | The Deep Reef | Sea Glass | — (finale) | Full Serpent Combat tree |

Each biome = 3 coves (e.g. Wreck Beach: Landing Cove → Debris Field → Low Tide Flats), each cove gated by repeated serpent-tier clears before advancing.

## Serpent tiers
Hatchling → Shoal-back → Tide-coiled → Bramblefang → Storm-wound → Cave-blind → Abyssal Coil (one per biome, roughly). Cave-blind (The Hollow) uniquely uses a rhythm/timing-based fight rather than visible-target tapping, since it's blind.

## Core formulas
```
Armor(biome, cove) = round(8 × 2.8^(cove_index) × BiomeMultiplier)
HP(biome, cove)    = round(50 × 2.5^(cove_index) × BiomeMultiplier)
BiomeMultiplier: [1, 9.5, 90.25, 857.4, 8145, 77380]  (×9.5 step-jump per biome)

Salvage/Tap Power(level) = (level+1) × (level+2) / 2   [quadratic]

Clears needed(biome, cove) = (cove_index+1) × ClearMult[biome]
ClearMult: [5, 15, 35, 105, 315, 945]  (×~3 harsher per biome)

Fight Duration = 30 seconds base, extendable via rare late-game upgrade
Cooldown = 20 minutes between attempts on same cove's serpent (reducible via upgrades)
```

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
- Monochrome-leaning palette (final call still open — could be full black/white/grayscale, or the black/cream/purple/teal/pink/coral palette seen in reference art)

## Animation approach
No rigged animation initially (solo artist, no animation skill). Code-driven procedural animation only: idle bounce/sway, tap-reaction squash-and-stretch, UI motion (pop-ins, slides), simple pose-swap cycling if multiple static poses are provided per character. Real rigged animation (Rive or Spine) is a possible future upgrade, likely starting with BBW and Lucy if pursued.

## Tech stack
- **Engine**: Unity (C#), for iOS + Android
- **Build split**: Claude writes code via Claude Code (Local session), Chad handles Unity Editor assembly (dragging in art, wiring scenes) and art production
- **Reference files in this repo**: `rubrehose_prototype.html` (playable browser prototype of the full loop, side-systems, and prestige — built to validate mechanics feel, not to be ported directly), `rubrehose_art_checklist.md` (phased art asset list by biome)

## Status as of this doc
All game systems above are fully designed and locked. Browser prototype built and tested. Art checklist created. Next step: scaffold real Unity project structure and begin implementing Wreck Beach (Phase 1 / MVP vertical slice) in C#.
