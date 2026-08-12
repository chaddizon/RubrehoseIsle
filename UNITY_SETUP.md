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
| Rubrehose → Build Persistent UI → Menu Drawer | §C: right-edge slide-in drawer, 6 placeholder entry rows | `MenuDrawerBuilder.cs` |
| Rubrehose → Build Landing Cove | §B: Dock/Shoreline/Camp/Frontier world-space clusters | `LandingCoveBuilder.cs` |

Run all four. Everything's placeholder primitive sprites (Unity's built-in Square/Circle/
Knob) in the ink/cream/purple/teal palette from `rubrehose_prototype.html` — drop real art
onto each object's `Image`/`SpriteRenderer` once it exists, a normal sprite swap, not
something any of these tools need to know about.

**Nothing here replaces §4 below** — the fight modal is still hand-built once; Landing
Cove's Frontier-cluster mini-boss trigger just calls into it once you've wired
`MiniBossTrigger.fightController` (see the flagged follow-ups at the bottom).

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

## 4. Fight modal — still hand-built, still needed

1. Build a modal panel (serpent name, HP/armor stats, an HP `Slider`, a countdown
   timer text, a log text, Attack + Retreat buttons). Start it inactive.
2. Attach `FightController.cs` (`Assets/Scripts/Combat`) to the panel root, wire its
   fields, and hook the Attack/Retreat buttons to `Attack()` / `Retreat()`.
3. Save it as a prefab or keep it inline in the scene.
4. Select `LandingCove/Frontier/MiniBossTrigger` in the Hierarchy and drag this panel's
   `FightController` onto its `Mini Boss Trigger` component's `Fight Controller` field —
   that's the last wire needed for tapping the Frontier cluster to open a fight.

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
cluster should add driftwood; tapping BBW's or BBC's home spot in Camp should recruit
them (once affordable); tapping the Frontier trigger should open the fight modal once
it's wired (§4). The fast-travel handle (bottom-left) should show "Wreck Beach" and
expand to a one-slot ribbon on tap. The menu button (top-right) should slide the drawer
in from the right with 6 rows (Captain's Log/Artifacts hidden until their unlock
conditions are met). Progress autosaves every 60s and on pause/quit, with offline crew
income (capped at 8h) applied on next launch.

## What's deliberately not here yet

Flagged follow-ups from this revision, roughly in the order they'd block real play:

- **Mini-boss fight rework** — `FightController`/`GameManager` still use the old
  repeated-clears model (`coveClears` / `ClearsNeeded`). The revised spec wants unlimited
  attempts and no clear-counter at the cove level; winning should set
  `PlayerState.coveMinibossDefeated[coveIndex] = true` instead. Only the data fields
  (`coveMinibossDefeated[]`, `coveConstructionRevealed[]`) exist on `PlayerState` so far —
  nothing reads or writes them yet.
- **Construction reveal** — per §B2, what's needed to cross to the next cove should only
  be shown after the mini-boss is beaten (`coveConstructionRevealed[coveIndex]`). Not
  implemented; the construction-gate logic in `GameManager` still works off the old model.
- **Hut build-state binding** — `HutConstructionState.SetState(int)` exists but nothing
  calls it yet; wire it to whatever ends up tracking construction progress.
- **Cast a Net/Bottle Toss, Salvage Crate, Banked Critters** — visual-only in §A; no
  backing systems exist. `PersistentHUDController.SetCastNetCharges/SetCrateFill/
  SetBankedCritters` are ready to call once something produces those numbers.
- **Message in a Bottle** — Shoreline's `BottleCastPoint` object exists with a collider
  but no script; the system isn't implemented in Unity yet.
- **Roaming hermit crab** — placed but static; the scripted Shoreline↔Camp path and
  tap-to-catch reward aren't implemented.
- **Tuggy / Prestige** — Dock's `Tuggy` object has no tap handler yet; per §D it should
  open the "Tuggy's Supply Run" screen, which doesn't exist in Unity yet.
- **Menu drawer content** — rows log a debug message on tap; no Crew/Upgrades/Captain's
  Log/Milestones/Settings/Artifacts panels exist behind them yet. `MenuDrawerController.
  artifactsUnlocked` is a placeholder Inspector bool — wire it to real prestige-count
  state once Prestige exists.
- **Debris Field / Low Tide Flats** — only Landing Cove (cove 0) is built. §B2 says the
  same 4-cluster method applies once this one's validated.
- Prestige, Artifacts, Tidepooling/Foraging, and biomes past Wreck Beach are still fully
  out of scope, as before.
