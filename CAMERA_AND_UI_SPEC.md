# Rubrehose Isle — Camera & UI Navigation Spec

Supplement to GAME_DESIGN.md. Covers the world camera model and fast-travel UI, decided after the initial Wreck Beach scaffold.

## Camera / world model (REVISED — single screen per cove, bounded scroll)

**Superseding the original continuous-strip model.** Each cove (Landing Cove, Debris Field, Low Tide Flats, etc.) is its own single, static, fully-composed screen — everything relevant to that cove is visible at once, no scrolling required to check on crew, tap resources, or reach any object. This mirrors how each floor in Idle Obelisk Miner is a self-contained scene, not something you pan through.

**Scrolling exists, but only BETWEEN coves within a biome, and only once unlocked.** A biome is capped at 3 coves, so scrolling is bounded to a maximum of 3 screens — swipe left/right to move between already-unlocked coves. This avoids the "scroll around to do routine tasks" friction flagged earlier, while still preserving some physical sense of traveling deeper into the island.

**Per-cove screen composition (locked):** 33% water (left) / 25% beach (middle) / 42% island (right), viewed from a flat 2D side angle — no 3D, no perspective/depth camera. Vertically, water occupies the bottom 30% of screen height (see HUD_AND_LANDING_COVE_LAYOUT.md for the full rationale).

**Tuggy appears at the far-left of each biome's FIRST cove screen** — not just Wreck Beach. He's the constant "you arrived here by boat" anchor and doubles as the Prestige trigger, present at the start of every biome you unlock.

Interaction split:
- **Tap objects directly in the scene** (driftwood, crew, the mini-boss trigger) — primary interaction, no change from before
- **Swipe left/right** moves between unlocked coves within the current biome (max 3 screens)
- Fast-travel ribbon still handles jumping between whole BIOMES (unchanged from below)

## Cove-gate structure: mini-boss → construction reveal

Each cove ends with a two-stage gate, replacing the earlier repeated-clears-per-cove model:

1. **Mini-boss fight** — attemptable anytime, but the player won't have enough power to win until they've upgraded/tapped enough. Mirrors Obelisk's actual Obelisk-fight model directly: <cite index="3-1">there's no limit to attempts; beating it is the product of accumulated power, not a clear-count.</cite> This REPLACES the earlier `clearsNeeded = cove × biome_multiplier` formula for cove-to-cove gating — that formula no longer applies at the cove level (still fine as the biome-ending finale's difficulty curve, see below).
2. **Construction reveal** — only after beating the mini-boss does the player learn what's needed to physically cross to the next cove (e.g. "gather driftwood + vines to craft rope for a bridge"). This is a deliberate surprise, not shown in advance — gives a reason to explore forward rather than plan the whole path out immediately.

**Cove 3 specifically is the biome's real finale**: a tougher, named serpent (e.g. Shoal-back for Wreck Beach), with its own bigger construction reveal that unlocks the next BIOME rather than the next cove. This preserves per-cove tier escalation (Hatchling-tier for coves 1-2, the named tier for cove 3) already established in the prototype's SERPENT_NAMES structure.

## Zone clustering & depth layering (standing art direction)

Every cove screen is composed as 3-4 deliberate **clusters** (small vignettes — e.g. a "Dock" cluster, a "Camp" cluster, a "Frontier" cluster) rather than objects scattered independently across the screen. Within each cluster, use **depth layering** — background elements smaller/higher (implying distance), foreground elements larger/lower (implying closeness) — so objects can sit close together without visually colliding. This is now the standing method for composing every cove screen, not just Landing Cove.

## UI growth philosophy

Avoid adding new permanent buttons/tabs as mechanics unlock. Two things are permanent, everything else grows within them or appears contextually in-world:
1. **Currency strip** (top) — horizontally scrollable row of pills, new currencies append to it as they unlock (Driftwood → Shells → Tokens → ...).
2. **Menu button** (top-right corner) — opens a drawer whose internal contents grow (Artifacts, Foraging, etc. get added as panels inside it), but its own footprint on screen never multiplies.

New mechanics manifest as **contextual hotspots physically placed in the scene** (e.g., a tappable glint over the tidepool cluster once Tidepooling unlocks) rather than new floating action buttons.

## Fast-travel ribbon

**Collapsed state:**
- Small circular handle, floating with clear margin from the bottom edge (NOT flush/docked to the edge — avoids the iOS home-gesture zone)
- Positioned bottom-left
- Shows a live thumbnail icon of whichever biome the player is currently "settled" in
- Small text label under it with the biome name
- Tap to open (deliberately not a swipe gesture, to stay unambiguous and avoid conflicting with the system's edge-swipe)

**"Settled" definition:** the biome occupying the majority of the visible viewport once scroll motion has stopped (not continuous tracking during an active drag). The thumbnail only updates at that point, or immediately after a fast-travel completes.

**Expanded state:**
- A short, low-profile pill/ribbon grows outward from the same anchor point the collapsed circle occupied (not a separate panel elsewhere on screen)
- Contains ONLY unlocked biomes — no locked/dimmed slots, no teasing future zones. Width scales exactly to unlocked-zone count (1 slot at game start, growing up to 6 by the end)
- Zones appear left-to-right in their real world order (Wreck Beach first, Deep Reef last)
- The player's current biome is highlighted (ring/outline, visually distinct) at its TRUE sequence position — e.g. if the player is in The Shallows (2nd biome), the highlighted thumbnail sits in slot 2, not glued to the collapsed circle's original position. This is a deliberate "bump" — the collapsed circle's fixed screen position and the current-biome slot's ribbon position are not the same thing once expanded.
- Small close (×) affordance in the corner
- Tapping any unlocked zone thumbnail fast-travels there via a smooth camera pan (not an instant cut — preserves the feeling of one continuous place)

## Implementation notes for Unity
- Camera: fixed/static per cove screen (no drag-scroll within a cove); a bounded swipe transitions between unlocked cove screens (max 3 per biome), animated as a smooth pan/slide rather than an instant cut
- World: one scene per cove (or one scene per biome with 3 cove sub-frames) — simpler than the earlier continuous-strip approach, easier to hand-place clusters exactly per the layout doc
- Fast-travel ribbon and collapsed handle: UI overlay (Screen Space Canvas), independent of which cove/biome is showing, always screen-anchored bottom-left. Now handles BOTH jumping between biomes AND jumping directly to a specific cove within the current biome (minor scope increase — ribbon slots may need a secondary cove-select step once inside a biome with 3 unlocked coves)
- Ribbon slot count and highlighted index are driven by the same `biomeUnlocked` / current-biome state already defined in GAME_DESIGN.md's data model — no new state needed beyond what's already tracked
- Mini-boss defeat state and construction-reveal state are new per-cove flags needed in the data model (e.g. `coveMinibossDefeated[]`, `coveConstructionRevealed[]`) — not yet present in PlayerState, flag for Claude Code to add
