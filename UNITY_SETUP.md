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
  pages between already-unlocked coves (max 3), animated as a pan. `coveScreenWidth`
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
| Rubrehose → Build Persistent UI → Menu Drawer | §C: right-edge slide-in drawer; Crew/Upgrades rows open real panels, the rest still log | `MenuDrawerBuilder.cs` |
| Rubrehose → Build Persistent UI → Fight Overlay | §4: the serpent's floating HP bar/timer, screen-space but no backdrop/buttons — wired into `FightController` | `FightOverlayBuilder.cs` |
| Rubrehose → Build Landing Cove | §B: Dock/Shoreline/Camp/Frontier world-space clusters, including the Serpent itself | `LandingCoveBuilder.cs` |

Run all five. Everything's placeholder primitive sprites (Unity's built-in Square/Circle/
Knob) in the ink/cream/purple/teal palette from `rubrehose_prototype.html` — drop real art
onto each object's `Image`/`SpriteRenderer` once it exists, a normal sprite swap, not
something any of these tools need to know about.

**Order note:** `FightController` now lives on the Serpent GameObject, built by Build
Landing Cove; Build Fight Overlay separately finds that `FightController` and wires its HP
bar/timer refs into it. If Landing Cove is rebuilt afterward (its own confirm-to-rebuild
prompt deletes and recreates the whole `LandingCove` tree, Serpent included), those overlay
refs are lost with it — re-run Build Fight Overlay to rewire them. Any other order among the
five commands is safe; each only ever rebuilds its own named child.

**One-time cleanup:** this revision deletes `FightModalBuilder.cs` (replaced by
`FightOverlayBuilder.cs`) and `MiniBossTrigger.cs` (its job now belongs to `FightController`,
on the Serpent itself). If your scene still has an old `PersistentUICanvas/FightModal` object
from before this change, its script reference is now missing (that class no longer exists) —
delete that `FightModal` GameObject by hand once, then run Build Fight Overlay to create the
new `FightOverlay` in its place. Leaving the old one in the scene isn't just clutter: it still
carries a `FightController` component, and with two `FightController`s present,
`FindFirstObjectByType<FightController>()` (used by Build Fight Overlay) could wire the
overlay to the stale one instead of the real one on the Serpent.

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
"0" instead of dealing damage). Beat the
Hatchling and the Hut in Camp should flip from rubble to half-built, then tap the Hut to
pay its crossing cost once affordable (it swaps to complete and Debris Field would be
next, once that cove exists). The Shoreline's bottle prop dims/brightens as it goes
idle/washed-up — tap it while bright for a Driftwood payout. The fast-travel handle
(bottom-left) should show "Wreck Beach" and expand to a one-slot ribbon on tap. The menu
button (top-right) should slide the drawer in from the right; tapping Crew or Upgrades
swaps to a real panel (recruit BBW/BBC, or spend on Tap Power) with a back arrow to
return, while Captain's Log/Milestones/Settings/Artifacts still just log (Captain's
Log/Artifacts hidden until their unlock conditions are met). Progress autosaves every 60s
and on pause/quit, with offline crew income (capped at 8h) applied on next launch.

## What's deliberately not here yet

Flagged follow-ups from this revision, roughly in the order they'd block real play:

- **Cast a Net/Bottle Toss, Salvage Crate, Banked Critters** — visual-only in §A; no
  backing systems exist. `PersistentHUDController.SetCastNetCharges/SetCrateFill/
  SetBankedCritters` are ready to call once something produces those numbers.
- **Roaming hermit crab** — placed but static; the scripted Shoreline↔Camp path and
  tap-to-catch reward aren't implemented.
- **Tuggy / Prestige** — Dock's `Tuggy` object has no tap handler yet; per §D it should
  open the "Tuggy's Supply Run" screen, which doesn't exist in Unity yet.
- **Menu drawer content beyond Crew/Upgrades** — Captain's Log/Milestones/Settings/
  Artifacts rows still just log on tap; no panels exist behind them yet.
  `MenuDrawerController.artifactsUnlocked` is a placeholder Inspector bool — wire it to
  real prestige-count state once Prestige exists.
- **Message in a Bottle economy** — `BottleCastPoint` implements the wash-up timer/tide
  window/reward-tier mechanics from GAME_DESIGN.md, but pays out in Driftwood since no
  Message-Fragments currency exists yet (that arrives at The Bluffs). Retarget its reward
  once that currency exists; its constants (wash-up delay, tide length, reward amounts,
  odds) are all placeholder tuning, not locked numbers.
- **Debris Field / Low Tide Flats** — only Landing Cove (cove 0) is built. §B2 says the
  same 4-cluster method applies once this one's validated; `GameManager.BuildConstruction`
  already advances `coveIndex` generically, so wiring a second cove's clusters is the main
  remaining piece.
- Prestige, Artifacts, Tidepooling/Foraging, and biomes past Wreck Beach are still fully
  out of scope, as before.
