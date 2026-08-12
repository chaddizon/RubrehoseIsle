# HUD & Landing Cove Layout Spec

Exact positions for an Editor-script auto-build, same pattern as `FastTravelRibbonBuilder.cs`. Covers (A) the two permanent screen-space elements and (B) every world-space object in Landing Cove (Wreck Beach's first cove) — later coves follow the same method once this is validated.

## A. Persistent screen-space UI — MASTER TABLE (all elements, exact positions)

**Vertical scene proportions (locked):** water occupies the bottom 30% of screen height; the remaining 70% is island (sky/terrain/characters). This applies as the Wreck Beach default — later biomes may flex this per their content (e.g. The Deep Reef could reasonably take more water) but nothing below the waterline is in scope yet; flagged as an easy future addition, not a current requirement.

Everything below sits on the same Screen Space Canvas tier, built via one auto-build Editor script pass (same method as `FastTravelRibbonBuilder.cs`). This is the complete, locked set for Wreck Beach — six elements, no more, no fewer.

| Element | Anchor | Offset (x,y) | Size | Notes |
|---|---|---|---|---|
| Currency pill (Driftwood) | Top-left | +16, -16 | 140×40px | Icon (32×32) + TMP text |
| Menu button | Top-right | -16, -16 | 44×44px | Checkerboard icon, opens drawer (§D) |
| Fast-travel handle | Bottom-left | +50, -64 (center point) | 60×60px (30px radius) | Matches the already-built ribbon spec exactly |
| Cast a Net / Bottle Toss icon | Bottom-right | -50, -64 (center point) | 60×60px | Mirrors fast-travel handle placement for visual symmetry; shows charge count |
| Salvage Crate meter | Left edge, vertical center | +16, 0 | 44×44px | Ambient fill meter |
| Banked Critters icon | Left edge, below crate meter | +16, +56 (from crate meter's position) | 40×40px | Count badge, tap to collect-all |

No element sits between the fast-travel handle and the Salvage Crate meter's vertical zones — bottom-anchored elements stay below mid-screen, crate/critter icons stay at vertical-center, so nothing overlaps regardless of screen size.

## B. Landing Cove world-object layout (REVISED — single screen, cluster-based)

**Superseding the earlier scrolling-strip fractional model.** Landing Cove is now ONE static screen: 33% water / 25% beach / 42% island horizontally, water at the bottom 30% vertically (per CAMERA_AND_UI_SPEC.md). Objects are grouped into four **clusters**, each internally composed with **depth layering** (background = smaller/higher, foreground = larger/lower) so nothing visually collides even at close quarters.

| Cluster | Screen zone | Contents | Depth layering |
|---|---|---|---|
| **Dock** | Water (left 33%) | Tuggy, docked | Background plane — smaller scale, positioned higher, implying distance across the water |
| **Shoreline** | Beach (middle 25%) | Driftwood ×3 (tappable), Message-in-a-bottle cast point | Foreground plane — larger scale, low on screen, closest to "camera" |
| **Camp** | Island (right 42%), left portion | Hut (construction states), Campfire, BBW home spot, BBC home spot | Three depth planes: hut small/high (back), campfire mid-scale/mid-height (middle), BBW+BBC large/low (front) |
| **Frontier** | Island (right 42%), far-right edge | Mini-boss trigger point | Deliberately isolated, standing apart from Camp — visually signals "this is a different kind of thing" |

**Roaming critter (hermit crab)**: scripted path moving between the Shoreline and Camp clusters (not a fixed point) — tap to catch for bonus loot; misses go to the Banked Critters count (§A).

Note: the Salvage Crate itself is purely a screen-space element (§A) — it's an ambient meter, not a physical object placed in the world.

## B2. Cove-gate mechanic: mini-boss → construction reveal

Applies to every cove, including Landing Cove's Frontier cluster trigger:

1. **Mini-boss fight** (Frontier cluster) — attemptable anytime, but requires enough accumulated power to actually win. No repeated "clears needed" counter — mirrors Obelisk's real Obelisk-fight model (attempt as many times as needed, succeed once strong enough). Landing Cove's mini-boss uses the Hatchling tier.
2. **Construction reveal** — only after the mini-boss is defeated does the actual crossing requirement appear (e.g. "gather Driftwood + Vines to craft rope for a bridge to Debris Field"). Not shown before that point — a deliberate surprise beat, not a known objective from the start.
3. Gathering the revealed materials (via normal tap/crew/Cast a Net play) completes the construction, unlocking Debris Field as the next cove screen (reachable via bounded swipe, per the revised camera model).

**Debris Field and Low Tide Flats** follow this exact same method — 4-cluster single-screen composition, mini-boss → construction reveal — once Landing Cove is built and validated. Low Tide Flats' mini-boss is the biome-defining Shoal-back tier, and its construction reveal unlocks The Shallows (the next BIOME) rather than another cove.



## C. Main menu drawer

Tapping the menu button (top-right, §A) opens a comic-panel-style drawer — per the established Rubrehose UI direction, NOT a generic icon grid. Slides in from the right edge, illustrated panel icons rather than flat symbols.

**Contents (only systems with no physical world location — side-loops like Tidepooling/Foraging/Bottles stay purely spatial and are NOT duplicated here, per the contextual-hotspot rule already established):**

| Entry | Available from | Icon direction |
|---|---|---|
| Crew | Start | Illustrated group portrait or rotating character icon |
| Upgrades (Tap / Cast a Net / Bottle Toss / Crit trees) | Start | A tool or stat-burst icon |
| Captain's Log | End of first construction gate | Open-book/log icon |
| Milestones | Start | Checklist/flag icon |
| Settings | Start | Simple gear — fine to be generic here, it's utility not flavor |
| Artifacts | After first prestige (hidden entirely before then, not shown-but-locked) | Compass/shard icon |

Drawer should be built the same auto-build-script way as everything else — placeholder panel shapes now, real illustrated icons dropped in later.

## D. Remaining world placement

Tuggy is now part of the **Dock cluster** (§B) directly — no separate fractional position needed under the revised single-screen model. Tapping him opens the Prestige ("Tuggy's Supply Run") screen, same as before — no separate menu entry.


## E. UI completeness check — does every system have a home?

| System | Home |
|---|---|
| Tap (core resource) | World — tap driftwood directly |
| Cast a Net / Bottle Toss | Persistent screen icon (§A) |
| Crew recruit/manage | In-world locked spots to recruit (§B); full management in Menu → Crew |
| Tap/Net/Bottle/Crit upgrade trees | Menu → Upgrades |
| Serpent fights | World — Frontier cluster mini-boss trigger (§B, §B2) |
| Construction gates | World — revealed after mini-boss defeat, at cove boundary (§B2) |
| Message in a Bottle | World — shoreline cast point (§B) |
| Tidepooling / Foraging | World — physical location in their respective biomes (not yet in Wreck Beach) |
| Captain's Log | Menu |
| Artifacts | Menu (post-prestige only) |
| Prestige | World — tap Tuggy (§D) |
| Milestones | Menu |
| Salvage Crate / roaming critter | Persistent screen icons, left edge (§A) |
| Fast travel | Persistent screen handle, bottom-left (already built) |
| Settings | Menu |

Every currently-designed system now has exactly one designated home — nothing floating without a UI location, nothing duplicated in two places.

Each object should use a **placeholder primitive sprite** (Unity default shapes are fine — square for hut, circle for driftwood, etc.) so the scene is immediately visualizable and testable before real art exists. Exposed as Inspector-assignable sprite fields (not hardcoded), so dropping in real art is a drag-and-drop swap, not a rebuild.

