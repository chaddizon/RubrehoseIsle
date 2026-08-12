# Rubrehose Isle — Camera & UI Navigation Spec

Supplement to GAME_DESIGN.md. Covers the world camera model and fast-travel UI, decided after the initial Wreck Beach scaffold.

## Camera / world model

The island is ONE continuous 2D side-view scene, not separate screens per biome. Water anchors the left/foreground consistently across the whole strip; terrain silhouette changes per biome to fit its content (flat beach, jungle rise, tall cliffs, cave mouth, mostly-open water for the reef finale). The player scrolls horizontally through this strip; new sections become scrollable as biomes unlock.

Reference proportions for Wreck Beach specifically (the leftmost/starting section): roughly 33% water, 20% beach, 47% buildable land, viewed from a flat 2D side angle — no 3D, no perspective/depth camera.

Interaction split:
- **Horizontal drag anywhere in the scene** = scroll along the island. Deliberately NOT a swipe-from-edge gesture, to avoid conflicting with iOS system gestures.
- Tapping objects in the scene (driftwood, crew, serpents at the frontier) is the primary interaction — mirrors Idle Obelisk Miner's pattern of tapping targets directly in the world rather than through abstract buttons.

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
- Camera: horizontally-constrained 2D orthographic camera, bounded to the unlocked portion of the world strip; extend bounds as biomes unlock
- World strip: likely one long scene (or several stitched sub-scenes with seamless boundaries) rather than scene-per-biome, to support continuous scroll
- Fast-travel ribbon and collapsed handle: UI overlay (Screen Space Canvas), independent of world-scroll position, always screen-anchored bottom-left
- Ribbon slot count and highlighted index are driven by the same `biomeUnlocked` / current-biome state already defined in GAME_DESIGN.md's data model — no new state needed beyond what's already tracked
