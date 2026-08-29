# Next Claude Code Push — Artifacts, Build Hints, Placeholder UI (as of 2026-08-29)

Paste this whole doc to the Claude Code session as its next instruction. It covers three
new/changed systems agreed on in design chat today, on top of everything in
`PROJECT_HANDOFF_MASTER.md`, `CORE_PROGRESSION_RESTRUCTURE.md`, and `HANDOFF.md`. Read those
three first if this is a fresh session — this doc assumes their context and doesn't repeat it.

---

## 1. Artifacts system (new)

Artifacts is the account-wide, endgame-facing counterpart to Cove Buildings' per-cove wealth
sink. It mirrors Idle Obelisk Miner's real "Archaeology" system: a rarity-tiered currency
(their "Fragments") spent on a permanent upgrade tree gated by progression milestones. Ours is
themed as recovering pieces of the crew's own wrecked ship.

**Two layers — acquisition (world) and spending (menu). Do not conflate them.**

### 1a. Acquisition — scattered spawn-tell sprites, all 4 coves

- Each built cove (Landing Cove and Tide Pools now; The Grove and The Deep Reef once built)
  gets a small pool of dormant "tell" sprites placed around its scene — small, cheap,
  animated-idle-loop objects, thematically matched per cove (e.g. a driftwood pile for Landing
  Cove, a rippling tide pool for Tide Pools, a rustling fern or canopy glint for the Grove,
  something breaking the surface for the Deep Reef). Chad will generate/place the actual
  sprites; this doc only specifies the trigger/state logic.
- **Reuse the existing Salvage Crate fill-then-ready timer state machine** rather than building
  a new one. Each cove runs its own timer; when it completes, pick one sprite at random from
  that cove's tell pool and flip it into a "live" state (an added glint/sparkle/brighten layer
  on top of its base idle loop — the base loop is the ambient "this spot can spawn something"
  tell, the live-state layer is "there's something here right now, tap me").
- Tapping a live tell grants one Compass Shard of some rarity tier (see 1c) and the sprite
  returns to its dormant idle-loop state; the cove's timer restarts.
- **Fast-travel ribbon notification**: since the camera is single-screen-per-cove, add a small
  badge/glow on a cove's fast-travel ribbon icon whenever that cove has a live tell the player
  isn't currently looking at, so players don't have to camp-scroll every cove. Clear it when
  the tell is tapped or its window elapses.
- **No punishing misses.** If a live tell isn't caught in a reasonable window, bank it
  automatically (same "nothing's ever truly lost" principle already used for the hermit crab)
  rather than deleting the reward outright.

### 1b. Spending — Menu → Artifacts panel

- Add an "Artifacts" entry to the Menu Drawer, same tier as Crew/Buildings. Per the
  zero-presence-until-earned pattern already used for Cove Buildings, **this entry doesn't
  appear in the drawer at all until the player's found their first Compass Shard** — don't
  clutter the early-game menu with an empty system.
- The panel itself: browse found/unappraised Shards, appraise them, spend appraised Shards on
  permanent tree nodes (damage/crit/tap power/crew-synergy style bonuses — reuse whatever node
  categories `EXPANDED_UPGRADES_AND_BALANCE.md` already establishes for other trees so the UX
  is consistent).
- **Node unlocks are gated to `serpentLevel` milestones**, not player level or cove index —
  this was already specified in `CORE_PROGRESSION_RESTRUCTURE.md` and still holds. This is
  what ties Artifacts to "defeating the boss over and over": it's the track that keeps the
  endless cove-4 fight feeling winnable as it scales forever.
- Artifacts is **account-wide**, not per-cove — do not scope its tree or its currency to a
  single cove. This is the deliberate split from Cove Buildings (local, Driftwood-funded,
  per-cove) — two wealth sinks with distinct roles, not overlapping ones.

### 1c. Rarity

- Compass Shards come in rarity tiers (reuse Obelisk's own tier count/naming logic loosely —
  common through rare/epic/legendary is fine, exact names TBD, doesn't need to match Obelisk's
  literal tier names). Weight tell spawns in later coves, and tells that go live at higher
  `serpentLevel`, toward rarer tiers. This is what makes pushing further into the island keep
  paying off without needing an explicit number on screen.

### 1d. Explicitly deferred — do not build yet

- The shipwreck sprite (2-3 frame animation, already generated) is being held back for **The
  Deep Reef** specifically — likely either a landmark object in Landing Cove (the actual wreck
  site) or half-submerged in the Deep Reef's water, tying into that cove's existing
  "something large just beneath the surface" background-tease note. Don't place it as a
  generic tell — wait for the cove 4 art pass and a follow-up decision.
- Exact rarity-tier names/count and exact tree node list: not finalized, use reasonable
  placeholders and flag them, same as `CoveBuildingCatalog.cs`'s placeholder numbers are
  already flagged.

---

## 2. Build hints — persistent, not one-time

Popup #13 in the onboarding sequence ("You could build something here...") is a one-time,
tap-to-dismiss popup. That's staying, but it's not enough on its own — add a **persistent
in-world marker** that stays visible for as long as a building's Stage 1 is affordable-or-not-
yet-built, not just a single popup moment.

- **Landing Cove (tutorial-critical): obvious.** A floating indicator at the Hut's (currently
  empty, zero-presence) location — icon plus short text, e.g. "Something can be built here!" —
  visible as soon as the building is unlocked, persists until Stage 1 is paid. This is the
  player's first exposure to the Buildings system, so don't be subtle here.
- **Coves 2-4: subtler, no text.** Once the player's already learned the pattern from Landing
  Cove, later coves' equivalent buildings should use a quieter version of the same marker — a
  soft pulsing glow or small sparkle icon, no floating text bubble — trusting the player already
  knows what it means. Don't re-teach the same lesson four times.
- **Wire this to the same flip already driving `CoveBuildingVisual.cs`'s zero-presence logic.**
  The marker should appear at the same moment the building becomes "known but unbuilt" and
  disappear automatically the moment Stage 1 is paid — it's a visibility state on the same
  object/system, not a separate one-time trigger to track.

---

## 3. Placeholder UI system — mimic Obelisk's menu structure

Chad wants every system in the game to have a reachable placeholder UI now, even systems with
no real backing logic yet (Postcards, Companions, Stats, Options), styled and sized to match
Idle Obelisk Miner's own menu conventions as closely as we can verify them, so the whole game
feels navigable end-to-end even before final art exists. **This may mean rebuilding the current
Menu Drawer, not just adding rows to it — that's expected and fine.**

**Honesty check on "same sizing as Obelisk's": Obelisk's exact pixel/point measurements aren't
publicly documented anywhere I could verify (no dev-facing spec, no screenshots I could pull
real numbers from). What follows is standard mobile-idle-game sizing convention (iOS/Android
Human Interface Guidelines), which is almost certainly close to what a mainstream App Store idle
game like Obelisk actually uses — but treat these as good-default placeholders to eyeball-match
against Obelisk directly on a device, not verified-exact numbers. Chad, if you want, screenshot
a few Obelisk menu screens next to ours once this is built and we can true these up for real.**

**Placeholder sizing convention to use:**

| Element | Size |
|---|---|
| Primary nav icon (Menu Drawer row icon) | 64-72pt square, min 44pt tap target |
| Panel header icon | 48-56pt square |
| Body/list-row text | 15-16pt |
| Header/title text | 20-24pt |
| Button label text | 16-18pt |
| Minimum tap target (any interactive element) | 44×44pt (Apple HIG floor) |

**Menu Drawer — full row list.** Every one of these gets a placeholder entry point even if the
system behind it is stubbed. Use Unity default UI (buttons, panels, plain text) for now —
nothing here needs real art yet, that's what the tracking checklist in section 4 is for.

| Row | Status | Placeholder panel content |
|---|---|---|
| Crew | Real | (existing, keep as-is) |
| Buildings | Real | (existing, keep as-is) |
| Artifacts | New, per section 1 | Shard list + appraisal tree, gated to first-find |
| Message in a Bottle / Cast a Net | Real (verify menu entry exists) | Bottle Toss + Cast a Net charge UI |
| Captain's Log | Stub if not built | Placeholder panel, "Coming soon" is fine |
| Tidepooling | Stub if not built | Placeholder panel |
| Foraging | Stub if not built | Placeholder panel |
| Postcards | Stub — no backing system exists | Placeholder panel, list is empty/locked |
| Companions | Stub — no backing system exists | Placeholder panel, list is empty/locked |
| Stats / Progress | Stub if not built | Raw numbers dump is fine — driftwood total, serpent level, coves unlocked, etc. |
| Options / Settings | Stub if not built | Sound toggle, reset/save stub, credits |

Each panel should follow the same structural pattern regardless of whether it's real or a stub:
header bar with title + close button (top), scrollable content area (middle), consistent with
whatever container/frame component the real panels (Crew, Buildings) already use — don't
invent a second panel style for stubs, reuse the one real pattern everywhere so it's a single
migration later, not two.

---

## 4. UI art-replacement tracking checklist

Keep this list current as placeholder UI gets built — it's the punch list for the final pixel-
art UI pass later. Claude Code: add to this list (in this file, or wherever the repo's own
convention prefers) any placeholder element created during this push that isn't already here.

- [ ] Menu Drawer background/frame (currently Unity default panel)
- [ ] Menu Drawer row icons — one per system row in the table above, now 13 rows built
      (currently none/default — every row shares the same generic knob-sprite icon placeholder)
- [ ] Panel header bar background + close button art
- [ ] Panel content background/frame (shared across all panels per the reuse rule above,
      including the new Artifacts/Stats/Settings panels and every stub panel)
- [ ] Button art (primary action buttons — recruit, appraise, pay-stage, recover-node, reset
      save, etc.) — currently Unity default buttons
- [ ] Currency icons — Driftwood, Compass Shards, any others referenced in panel UI text
- [ ] Body/header text — currently system font, will need Rubrehose's stylized
      treatment/font eventually
- [ ] Build-hint marker icon (obvious version, Landing Cove — built) and subtle version
      (coves 2-4 — not yet buildable, no Cove Building exists for them yet)
- [ ] Fast-travel ribbon notification badge art (currently a plain teal knob-sprite dot, both
      the collapsed-handle and per-slot versions)
- [ ] Artifacts tell-sprite "live state" glint/sparkle overlay (base idle-loop sprites are
      Chad's normal per-cove art pass, not placeholder UI — only the glint overlay is a UI
      placeholder concern; currently a plain teal square, placed on Landing Cove/Tide
      Pools/The Grove's 2 tell spots each)
- [ ] Settings sound-toggle art (currently a default Unity Toggle checkbox)

---

## 5. Cove 3 (The Grove) background art

Background art for The Grove is done and needs to go in following the same integration steps
Tide Pools already went through (per `HANDOFF.md`'s "Suggested first things to check" section):
build via the same cluster-builder pattern Landing Cove and Tide Pools used, wire the mini-boss/
fight system the same way `TidePoolsBuilder.cs` did, and expect an anchor-nudging pass once seen
in Play mode (same follow-up both prior coves needed).

**Composition note for anchor placement once real content goes in**: the canopy is dense
edge-to-edge through the lower two-thirds of the frame — treat that as already-crowded and
avoid stacking new interactive objects there. The one genuinely open area is the upper-left,
where the mountain's rocky flank is exposed against flat sky — that's the natural home for the
Artifacts tell sprite(s) assigned to this cove, and possibly a good spot for anything else that
needs to read clearly without competing with the foliage texture.

**Already committed as of this push** — verified at 192×344, matching the locked portrait
convention. See section 6 below for exact paths.

---

## 6. Repo state as of this push (2026-08-29, ground truth — overrides anything above that references these files more vaguely)

This push changed the on-disk asset layout.

- **Landing Cove background**: overwritten in place at
  `Assets/Art/Backgrounds/WreckBeach/landingcove1.png` (same filename/path as before — GUID and
  every existing scene reference are preserved, nothing needs re-linking in the Inspector).
- **Tide Pools background**: overwritten in place at
  `Assets/Art/Backgrounds/TidePools/tidepool1.png` (same, GUID preserved).
- **Grove background**: brand new. `Assets/Art/Backgrounds/Grove/` did not exist before this
  push — it was created to hold `grove1.png`. **No `GroveBuilder.cs` exists yet** — this is the
  first time Grove needs its own builder script, following the same cluster-builder pattern
  `LandingCoveBuilder.cs` and `TidePoolsBuilder.cs` already established (per `HANDOFF.md`'s note
  that no code changes are needed to build a new cove beyond that pattern).
- **Folder-naming note, not a bug to fix now**: Landing Cove's art lives under a folder named
  `WreckBeach`, not `LandingCove` — a leftover from before the cove restructure, already flagged
  in `HANDOFF.md`'s naming-inconsistency section. Tide Pools and Grove both got proper
  cove-named folders. Don't "fix" WreckBeach's folder name as part of this push — out of scope,
  same as the `WreckBeachData.cs` string rename already flagged elsewhere.
- Since Landing Cove and Tide Pools' backgrounds changed (not just Grove being new), re-check
  both scenes' object anchors in Play mode once this is pulled in — anchors were originally
  eyeballed against the old background PNGs, and any composition shift could leave them
  slightly off.
