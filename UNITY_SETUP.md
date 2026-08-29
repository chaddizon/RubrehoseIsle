# Unity setup — Wreck Beach vertical slice

This repo root *is* the Unity project (that's why `.gitignore` is the standard Unity
root gitignore). Only the parts that need code — safe, mechanical scene edits, or a
`[MenuItem]` tool for a hierarchy exact enough that eyeballing it isn't worth it — are
done here; everything else (imported art, per-entry menu-drawer content) is Editor work,
per the "Build split" note in [`GAME_DESIGN.md`](GAME_DESIGN.md).

**This doc reflects the single-static-screen-per-cove navigation model** from the revised
`CAMERA_AND_UI_SPEC.md` and `HUD_AND_LANDING_COVE_LAYOUT.md` — a rebuild of the original
continuous-scroll version. If you're looking at old notes/screenshots from before, the
camera and HUD sections below supersede them.

## 1. Open the project

1. Unity Hub → **Add** → select this folder (`RubrehoseIsle`).
2. Open with whatever Editor version Hub offers for `ProjectVersion.txt` (it tracks
   whatever version you last opened with) — Hub will upgrade the project in place if
   needed, that's fine.
3. If prompted to import **TMP Essentials** (TextMeshPro), do it — the UI scripts use
   `TMP_Text`.

## 2. `Assets/Scenes/WreckBeach.unity` — already wired

- **Main Camera** — 2D orthographic (`orthographic size: 5`), has `CoveViewCamera.cs`
  attached (`Assets/Scripts/CameraControl`). No drag-scroll — a bounded left/right swipe
  pages between already-unlocked coves (max 4 now — CORE_PROGRESSION_RESTRUCTURE.md's 4
  coves ARE the whole base game, not 3-per-biome), animated as a pan. `coveScreenWidth`
  (5.625) assumes a 1080×1920 reference aspect; retune it once real cove art sets the
  actual per-screen width.
- **GameManager** — empty GameObject with `GameManager.cs` attached, so `Tap()`, crew,
  cove/fight state etc. are live the moment you press Play.

## 3. Auto-build commands — run these, don't hand-build

All under the **Rubrehose** menu. Each is re-runnable (confirms before deleting/rebuilding
its own subtree) and safe to run in any order — they share one Canvas
(`PersistentUICanvas`) but each tool only ever touches its own named child.

| Command | Builds | Script |
|---|---|---|
| Rubrehose → Build Persistent UI → HUD Elements | §A: currency pill, menu button, cast-a-net icon, salvage crate meter, banked critters icon | `PersistentHUDBuilder.cs` |
| Rubrehose → Build Persistent UI → Fast-Travel Ribbon | §A: the 6th persistent element — collapsed handle + expanding ribbon | `FastTravelRibbonBuilder.cs` |
| Rubrehose → Build Persistent UI → Menu Drawer | §C: right-edge slide-in drawer; Crew/Upgrades/Buildings rows open real panels, the rest still log | `MenuDrawerBuilder.cs` |
| Rubrehose → Build Persistent UI → Fight Overlay | §4: the serpent's floating HP bar/timer, screen-space but no backdrop/buttons — wired into `FightController` | `FightOverlayBuilder.cs` |
| Rubrehose → Build Persistent UI → Onboarding Popup | Bottom-anchored, single-tap-dismiss popup (comic-panel style, no full-screen catcher) plus a standalone `OnboardingController` wired to it — CORE_PROGRESSION_RESTRUCTURE.md's onboarding/tutorial system | `OnboardingBuilder.cs` |
| Rubrehose → Build Landing Cove | §B: Dock/Shoreline/Camp/Frontier world-space clusters, including the Serpent itself | `LandingCoveBuilder.cs` |
| Rubrehose → Build Tide Pools | Cove 1's Grove/TidePools/Frontier world-space clusters (CORE_PROGRESSION_RESTRUCTURE.md), including its own Serpent — placeholder positions, same as Landing Cove's before it was validated in Play mode | `TidePoolsBuilder.cs` |

Run all seven. Everything's placeholder primitive sprites (Unity's built-in Square/Circle/
Knob) in the ink/cream/purple/teal palette from `rubrehose_prototype.html` — drop real art
onto each object's `Image`/`SpriteRenderer` once it exists, a normal sprite swap, not
something any of these tools need to know about.

**Order note:** `FightController` now lives on each cove's own Serpent GameObject — one built
by Build Landing Cove, one by Build Tide Pools, and so on as later coves get built. Build
Fight Overlay finds *every* `FightController` currently in the scene and wires the same HP
bar/timer refs into all of them (`FightController` only actually drives the overlay while its
own fight is active — see `_showingOverlay` in `FightController.cs` — so two coves' serpents
sharing one overlay is safe). If a cove is rebuilt afterward (its own confirm-to-rebuild
prompt deletes and recreates that cove's whole tree, Serpent included), that cove's overlay
refs are lost with it — re-run Build Fight Overlay to rewire everyone. Any other order among
these commands is safe; each only ever rebuilds its own named child.

**One-time cleanup:** this revision deletes `FightModalBuilder.cs` (replaced by
`FightOverlayBuilder.cs`) and `MiniBossTrigger.cs` (its job now belongs to `FightController`,
on the Serpent itself). If your scene still has an old `PersistentUICanvas/FightModal` object
from before this change, its script reference is now missing (that class no longer exists) —
delete that `FightModal` GameObject by hand once, then run Build Fight Overlay to create the
new `FightOverlay` in its place. Leaving the old one in the scene isn't just clutter: it still
carries a `FightController` component, and Build Fight Overlay now wires *every*
`FightController` it finds (needed so both Landing Cove's and Tide Pools' serpents get the
overlay) — a stale leftover would get wired too, doing nothing useful since it never starts a
fight, but it's still dead weight worth deleting.

**Second one-time cleanup (2026-08-27 Cove Buildings revision):** `ConstructionGate.cs` and
`HutConstructionState.cs` are deleted, replaced by `CoveBuildingVisual.cs`. If your scene
already has a built `LandingCove` and/or `TidePools` hierarchy from before this change, their
`Hut` object's components will show as "Missing Script" in the Inspector (the classes they
referenced no longer exist) — this is expected, not a bug. Re-run **Build Landing Cove** (and
**Build Tide Pools**, if built) to clear it; each rebuild deletes and recreates its own tree
with the new `CoveBuildingVisual`-based Hut. Also re-run **Build Persistent UI → Menu Drawer**
to pick up the new Buildings row/panel/prefab (`Assets/Prefabs/BuildingListItem.prefab`, saved
automatically by `MenuDrawerBuilder.cs` the same way `CrewListItem.prefab` already was — no
manual prefab-saving step needed).

### About `MainHUDController.cs` / `CrewListItemUI.cs`

These predate this revision and are now **partially superseded**: the old design put a
driftwood counter, cove-progress bar, tap button, crew list, and construction section all
in one HUD panel. Under the revised model, tap happens directly on world objects (Landing
Cove's Shoreline driftwood), crew recruiting happens at world-space home spots (Camp
cluster), and the persistent HUD only shows a small driftwood pill (§A) — there's no
progress-bar or tap-button element in the new master table at all. Don't build the old
HUD layout from earlier notes. The two scripts still compile and aren't deleted (crew
management still needs *some* UI home eventually, per §E — "full management in Menu →
Crew" — so `CrewListItemUI` may get reused there), but treat them as unwired legacy code
for now, not something to attach to the scene.

## 4. Fight system — in-scene, not a modal

Per `IN_SCENE_FIGHT_SYSTEM.md`, the serpent is a persistent world object (`Serpent`, built
by `Build Landing Cove` inside the Frontier cluster) with `FightController.cs`
(`Assets/Scripts/Combat`) and `SerpentVisual.cs` (`Assets/Scripts/World`) attached directly
to it — no full-screen takeover. Tapping the serpent both starts a fight (if off cooldown
and not yet defeated) and, while one's active, deals damage; `SerpentVisual` drives its
wake-up/hit-flash/settle/defeat reactions. `Build Persistent UI → Fight Overlay` builds the
small floating HP-bar/timer panel that tracks the serpent's world position every frame
(`FightController.PositionOverlay`) and wires itself into that same `FightController`.
Recruited crew (BBW, BBC) walk out to the serpent for as long as a fight is active
(`CrewHomeSpotAnimator`, driven by `FightController.IsFightActive`/`ActiveSerpent`) — their
existing idle frames double as a walk-cycle at a faster pace during transit (no separate walk
art needed), they hold their attack loop once they arrive (offset per recruit via
`attackOffset` so BBW/BBC flank the serpent instead of stacking), then walk the same idle
frames back to their HomeSpot and resume idle/working the moment they're home. BBW's attack
art exists (`bbwattacking1.png`/`bbwattacking2.png`, a 2-frame loop) and is wired. BBC's isn't
painted yet, so BBC currently just freezes on his last walk frame for the fight's duration
until `Assets/Art/Characters/BBC/bbcattack{1-3}.png` exist, at which point a `Build Landing
Cove` rebuild picks them up with no further wiring (adjust `BBCAttackSpritePaths` in
`LandingCoveBuilder.cs` first if BBC's files land under a different naming convention than
that guess, the way BBW's did).

## 5. Art

Folders under `Assets/Art/` are pre-organized to match Phase 1 of
`rubrehose_art_checklist.md`:

- `Characters/BBW/`, `Characters/BBC/` — idle / tap-reaction / working poses
- `Characters/Serpents/Hatchling/`, `Characters/Serpents/Shoalback/` — idle + hit-reaction
- `UI/` — checkerboard menu icon, tap button treatment, driftwood icon, currency pill,
  progress bar fill/track, construction gate icon
- `Backgrounds/WreckBeach/` — sky, water, sand, wreckage hull, hut build states, campfire

Import as PNG with transparent background, 2x resolution, separate layers per moving
part (per the checklist's technical prep note).

## 6. Try it

Press Play. Swiping left/right shouldn't move the camera yet (Landing Cove is the only
unlocked cove, so there's nowhere to page to). Tapping a driftwood piece in the Shoreline
cluster should add driftwood. BBW/BBC shouldn't be visible (or tappable) at their Camp home
spots at all until recruited — recruit them from Menu → Crew once affordable (25 driftwood
each; that's currently the only way to recruit for the first time, since there's nothing in
the world to tap yet). Recruiting pops a quick scale-punch plus a toast naming them right at
their Camp spot, and from then on they're a real, full-color, tappable presence there —
both the world tap and Menu → Crew keep working to level them up further. Tapping the
Serpent in the Frontier cluster should wake it up in
place and pop its HP-bar/timer overlay just above it, then keep tapping it directly to
attack — a damage number should float off it per hit, and any recruited BBW/BBC should walk
out from Camp to flank the serpent and attack alongside you for as long as the fight's
running, then walk back to Camp once it ends. HP persists across
attempts (letting the 30s timer run out doesn't reset damage dealt so far, it just starts
a real 20-minute cooldown before the next attempt; tap power below the boss's armor pops a
"0" instead of dealing damage).

**Defeating the Hatchling immediately unlocks Tide Pools** — no construction-gate payment
step anymore (that system was removed in the 2026-08-27 revision, see below). The camera
should auto-pan to reveal Tide Pools right away (CoveViewCamera.OnCoveUnlocked), and the
fast-travel ribbon/handle should show it as a second slot immediately.

The Hut in Camp starts with **zero presence** — no sprite, no collider, nothing to tap —
per the Cove Buildings rework. It only appears once you pay its Stage 1 cost (300 Driftwood)
from **Menu → Buildings**, the only way to trigger a building's first stage. Once visible, it
becomes directly tappable in-world to pay Stage 2 (5,000) and Stage 3 (100,000), same as
Menu → Buildings offers. Each paid stage adds to `GameManager.TapPower` for real (not just
cosmetic) — watch your tap numbers go up after paying.

The Shoreline's bottle prop dims/brightens as it goes idle/washed-up — tap it while bright
for a Driftwood payout. The fast-travel handle (bottom-left) should show "Landing Cove" and
expand to a one-slot ribbon on tap (it grows to more slots as further coves unlock — up to 4
total now, not scaling toward 18). The menu button (top-right) should slide the drawer in
from the right; tapping Crew, Upgrades, or Buildings swaps to a real panel with a back arrow
to return, while Captain's Log/Milestones/Settings/Artifacts still just log (Captain's
Log/Artifacts hidden until their unlock conditions are met). Progress autosaves every 60s
and on pause/quit, with offline crew income (capped at 8h) applied on next launch.

On a fresh save, the full 14-popup onboarding sequence (CORE_PROGRESSION_RESTRUCTURE.md's
"Full ordered sequence for Landing Cove") should queue and show one at a time, bottom-
anchored, dismissible with a single tap — they won't reappear on a later launch once
dismissed. The first 3 (island intro, Tuggy, core tap loop) fire immediately; the rest
trigger contextually as you actually reach each condition (affording a recruit, first
recruit, both crew recruited, a fight starting, a cove unlocking with its pan-reveal beat,
affording a building's Stage 1, completing a building stage). See `OnboardingController.cs`'s
class comment for which rows are wired for real vs. treated as unconditionally-true-from-
cove-load (the hermit crab/bottle-flag/mini-boss-visible rows — nothing gates their visibility
yet) vs. genuinely deferred (the Salvage Crate fill row — no backing state exists for it at
all, unlike the others).

## What's deliberately not here yet

Flagged follow-ups from this revision, roughly in the order they'd block real play:

- **⚠️ Rebalancing not executed — required follow-up, not optional.** Cove Buildings
  (`CoveBuildingCatalog.cs`) grant real numeric tap-power bonuses now, stacking
  multiplicatively on top of the existing tap/serpent curve
  (EXPANDED_UPGRADES_AND_BALANCE.md's additive-within/multiplicative-across rule). Per
  CORE_PROGRESSION_RESTRUCTURE.md's own "Rebalancing requirement" section, this makes
  players stronger than the serpent HP/Armor curve (`GameFormulas.SerpentHp`/`SerpentArmor`/
  `SerpentHpForLevel`/`SerpentArmorForLevel`) was tuned for, and the doc is explicit that the
  fix (lowering the base curve's growth rate to compensate) **is not something to derive by
  hand** — it needs the same simulation approach used for the earlier pacing retune
  (`rubrehose_prototype.html`'s Balance tab, or an equivalent Unity-side simulation), run
  with building bonuses included. Every cost/reward number in `CoveBuildingCatalog.cs` is
  also explicitly a rough, unbalanced placeholder pending that same pass — do not treat
  either as tuned. This is real, tracked follow-up work, not a "someday" note.
- **Cast a Net/Bottle Toss, Salvage Crate, Banked Critters** — visual-only in §A; no
  backing systems exist. `PersistentHUDController.SetCastNetCharges/SetCrateFill/
  SetBankedCritters` are ready to call once something produces those numbers.
- **Roaming hermit crab** — placed but static; the scripted Shoreline↔Camp path and
  tap-to-catch reward aren't implemented.
- **Tuggy / Prestige** — Dock's `Tuggy` object has no tap handler yet; per §D it should
  open the "Tuggy's Supply Run" screen, which doesn't exist in Unity yet.
  `TuggyTravelController.cs` (`Assets/Scripts/World`) handles the cove-to-cove cruise-in
  animation and is now attached and wired automatically by Build Landing Cove (§7 below) —
  no manual step needed anymore, and it survives a rebuild since the builder redoes it every
  time. Still unrelated to the still-missing tap/Prestige handler.
- **Menu drawer content beyond Crew/Upgrades/Buildings** — Captain's Log/Milestones/
  Settings/Artifacts rows still just log on tap; no panels exist behind them yet.
  `MenuDrawerController.artifactsUnlocked` is a placeholder Inspector bool — wire it to
  real prestige-count state once Prestige exists.
- **Message in a Bottle economy** — `BottleCastPoint` implements the wash-up timer/tide
  window/reward-tier mechanics from GAME_DESIGN.md, but pays out in Driftwood since no
  Message-Fragments currency exists yet (that arrives at The Bluffs). Retarget its reward
  once that currency exists; its constants (wash-up delay, tide length, reward amounts,
  odds) are all placeholder tuning, not locked numbers.
- **Foraging Grounds / The Deep** — Landing Cove (cove 0) and now Tide Pools (cove 1,
  `TidePoolsBuilder.cs`) are built; cove 1 uses a 3-cluster layout (Grove/TidePools/Frontier,
  no Dock — Tuggy stays anchored to Landing Cove's) instead of Landing Cove's 4, since it has
  no dock/boat feature of its own. Its Grove crew spot and its three TidePool interaction
  points are placed but fully unwired (no Tidepooling minigame and no Tide-Pools crew member
  exist yet — `CrewCatalog.cs` still only has bbw/bbc) — same "placed, not yet functional"
  treatment Landing Cove's roaming hermit crab got. Cove 2's mini-boss is wired for real
  (defeating it unlocks The Grove immediately, per the removed-construction-gate rework
  below); it has no Cove Building of its own yet — `CoveBuildingCatalog.cs` only defines
  Landing Cove's Hut so far, per CORE_PROGRESSION_RESTRUCTURE.md leaving other coves'
  buildings "TBD, not needed until those coves are actually built."
  CORE_PROGRESSION_RESTRUCTURE.md renamed/restructured the old "Debris Field / Low Tide
  Flats" 3-coves-per-biome plan into these 2 remaining coves of Wreck Beach's fixed 4 (the
  4th, "The Deep," is the new permanent/endlessly-scaling serpent fight — GameManager's
  `IsEndlessCove`/`SerpentLevel`, `GameFormulas.SerpentHpForLevel`/`SerpentArmorForLevel`).
  `GameManager` already handles all 4 coves' state generically (including the endless
  one's fight/respawn logic); no scene content exists for coves 2-3 yet — the same
  cluster method (§B2) applies once each is validated. Every anchor placed so far
  (Landing Cove's originally, Tide Pools' now) was eyeballed against the flat background PNG,
  not a rendered scene — expect a nudging pass on Tide Pools' once it's actually visible in
  Play mode, the same follow-up Landing Cove already went through.
- **Tuggy's real cruise sprites** — `TuggyTravelController.cruiseFrames` is empty
  (placeholder motion only, per CORE_PROGRESSION_RESTRUCTURE.md); drop real frames in once
  painted, no code change needed.
- **Onboarding popups beyond the 3 finite-cove/intro triggers** — `OnboardingController`
  only fires for mechanics that actually exist today (intro tap/crew/mini-boss, reaching
  Tide Pools/Foraging Grounds/The Deep). Tidepooling/Foraging as real minigames and the
  Artifacts/Compass Shard economy don't exist yet, so there's no "first Compass Shard
  earned" trigger wired up — add a call to `OnboardingController`'s enqueue path from
  wherever that system's first-earned event lands once it's built.
- Prestige, Artifacts, Compass Shards, Tidepooling/Foraging as real minigames, and biomes
  past Wreck Beach (now a rare, endgame-only concept, not near-term — see `BiomeCatalog.cs`)
  are still fully out of scope, as before.

## 7. Tuggy's travel animation — now wired automatically

`TuggyTravelController.cs` is generic and self-positioning (it computes its own world
position from `Camera.main`'s viewport each time it moves, so it doesn't need to be
re-parented or otherwise touch Landing Cove's existing build). This used to be a manual
post-build step in the Inspector — Add Component, then drag in `Cove Camera` (the Main
Camera's `CoveViewCamera`), `Sprite Renderer`, and `Idle Bob` — but a `Build Landing Cove`
rebuild deletes and recreates the whole `LandingCove` tree, Tuggy included, which silently
wiped that wiring out every time. `LandingCoveBuilder.cs` now attaches and wires it itself
(`BuildDockCluster`), so it survives every rebuild with no follow-up needed.

`Cruise Frames` is deliberately left unassigned by the builder — no real cruising sprites
exist yet, so the cruise-in still plays as a plain position tween with no frame-swap on top.
Drop frames into that field by hand once real art exists; the builder won't touch it (and
would wipe a hand-assigned value on the next rebuild, so do this after your last rebuild, or
re-assign it again after).

If Main Camera doesn't have a `CoveViewCamera` component yet when Build Landing Cove runs,
you'll get a console warning and Tuggy's cove-to-cove direction logic won't fire — add the
component to Main Camera and re-run Build Landing Cove to rewire it.
