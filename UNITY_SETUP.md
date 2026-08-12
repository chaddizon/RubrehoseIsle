# Unity setup — Wreck Beach vertical slice

This repo root *is* the Unity project (that's why `.gitignore` is the standard Unity
root gitignore). Only the parts that need code — safe, mechanical scene edits, or a
`[MenuItem]` tool for a hierarchy exact enough that eyeballing it isn't worth it — are
done here; everything else (imported art, remaining UI hierarchy) is Editor work, per
the "Build split" note in [`GAME_DESIGN.md`](GAME_DESIGN.md).

## 1. Open the project

1. Unity Hub → **Add** → select this folder (`RubrehoseIsle`).
2. Open with whatever Editor version Hub offers for `ProjectVersion.txt` (it tracks
   whatever version you last opened with) — Hub will upgrade the project in place if
   needed, that's fine.
3. If prompted to import **TMP Essentials** (TextMeshPro), do it — the UI scripts use
   `TMP_Text`.

## 2. `Assets/Scenes/WreckBeach.unity` — already wired

Two things are already set up directly in the scene file, so you don't need to build
them by hand:

- **Main Camera** — converted to 2D orthographic (`orthographic size: 5`) and has
  `WorldScrollCamera.cs` attached (`Assets/Scripts/CameraControl`). Horizontal drag
  anywhere in the scene now scrolls it, per `CAMERA_AND_UI_SPEC.md`. Its `worldMinX`
  / `worldMaxX` are a **placeholder range (-2..2)** — once the Wreck Beach background
  art is imported, update these on the component to match the strip's real world-unit
  width (roughly 33% water / 20% beach / 47% buildable land per the spec).
- **GameManager** — empty GameObject with `GameManager.cs` attached, so `Tap()`,
  crew, cove/fight state etc. are live the moment you press Play.

What's still yours to build in-Editor:

1. **Canvas** (Screen Space – Overlay) + **EventSystem** (Unity offers to create the
   EventSystem automatically the first time you add a Canvas).
2. Under the Canvas, build the HUD roughly following `rubrehose_prototype.html`'s
   layout (it's the validated reference for feel, not a pixel spec):
   - Resource readout: driftwood amount, biome tag, cove name + clears, a progress bar.
   - Tap button showing tap amount.
   - Tap-upgrade row (level + cost).
   - A crew list container (empty `Transform`, e.g. a `Vertical Layout Group`).
   - Fight button.
   - Construction gate section (label + build button).
3. Attach `MainHUDController.cs` (`Assets/Scripts/UI`) to the Canvas or a HUD root, and
   wire every `[SerializeField]` in the Inspector to the objects you just built.

## 3. Crew list prefab

1. Build one crew row (name+level text, rate text, cost text, recruit button).
2. Attach `CrewListItemUI.cs`, wire its fields, drag the row into `Assets/Prefabs/` to
   make it a prefab, delete the scene instance.
3. Assign that prefab to `MainHUDController.crewItemPrefab` and the list container to
   `crewListContainer`.

## 4. Fight modal

1. Build a modal panel (serpent name, HP/armor stats, an HP `Slider`, a countdown
   timer text, a log text, Attack + Retreat buttons). Start it inactive.
2. Attach `FightController.cs` (`Assets/Scripts/Combat`) to the panel root, wire its
   fields, and hook the Attack/Retreat buttons to `Attack()` / `Retreat()`.
3. Save it as a prefab or keep it inline in the scene — either works, since
   `MainHUDController` only needs a scene reference to call `OpenFight()`.

## 5. Fast-travel handle/ribbon — generated, don't hand-build

Menu: **Rubrehose → Build Fast-Travel Ribbon** (`Assets/Editor/FastTravelRibbonBuilder.cs`).
It builds the whole hierarchy with exact `RectTransform` values instead of you eyeballing
it: a `FastTravelCanvas` (Screen Space – Overlay, independent of the world-scroll Canvas,
always screen-anchored bottom-left per `CAMERA_AND_UI_SPEC.md`) containing the collapsed
handle and expanded ribbon, plus a `FastTravelSlotUI` prefab saved to
`Assets/Prefabs/FastTravelSlot.prefab`. It also attaches `FastTravelRibbonController`,
wires every field (including `worldCamera` → whatever `WorldScrollCamera` it finds in the
scene), and adds an `EventSystem` if the scene doesn't have one yet.

Run it any time after the scene is open. It's re-runnable: if `FastTravelCanvas` or the
slot prefab already exist, it asks before deleting and rebuilding them from scratch — so
don't hand-edit the generated objects or prefab directly, since a rebuild throws those
edits away. Everything you'd actually want to customize lives on
`FastTravelRibbonController`'s own fields instead:

- `biomeThumbnails` / `biomeWorldX` — size-6 arrays in `BiomeCatalog` order (Wreck Beach,
  The Shallows, The Green, The Bluffs, The Hollow, The Deep Reef). Only index 0 matters
  for now; fill in the rest as each biome's terrain gets a real world-X position and a
  thumbnail sprite.

Everything's built with placeholder colors/shapes (the built-in circular "Knob" sprite for
the handle/ring, flat ink/cream/purple fills from `rubrehose_prototype.html`'s palette) —
drop real art onto the `Image` components under `FastTravelCanvas` and the slot prefab
once it exists; that's a normal sprite swap, not something the tool needs to know about.

The ribbon only ever shows biomes up to `GameManager.Instance.State.biomeUnlocked`
(currently always 0 → Wreck Beach only, so you'll see exactly one slot). That field
gets bumped by later biomes' construction gates once those exist — no ribbon changes
needed when that happens.

## 6. Art

Folders under `Assets/Art/` are pre-organized to match Phase 1 of
`rubrehose_art_checklist.md`:

- `Characters/BBW/`, `Characters/BBC/` — idle / tap-reaction / working poses
- `Characters/Serpents/Hatchling/`, `Characters/Serpents/Shoalback/` — idle + hit-reaction
- `UI/` — checkerboard menu icon, tap button treatment, driftwood icon, currency pill,
  progress bar fill/track, construction gate icon
- `Backgrounds/WreckBeach/` — sky, water, sand, wreckage hull, hut build states, campfire

Import as PNG with transparent background, 2x resolution, separate layers per moving
part (per the checklist's technical prep note) — the C# side doesn't care how many
sprites a character is split into; that's purely how you build the GameObject hierarchy
for procedural animation later.

## 7. Try it

Press Play. Tap should add driftwood, recruiting BBW/BBC should start passive income,
Fight should open the modal against the current cove's Hatchling/Shoal-back with a 30s
timer, and horizontal drag anywhere in the scene (not starting on a UI element) should
scroll the camera within the placeholder bounds. The fast-travel handle should show
"Wreck Beach"; tapping it expands the ribbon with one highlighted slot, tapping outside
or the × collapses it. Progress autosaves every 60s and on pause/quit to
`Application.persistentDataPath`, with offline crew income (capped at 8h) applied on
next launch.

## What's deliberately not here yet

Prestige, Artifacts, Tidepooling/Foraging/Bottles, and biomes past Wreck Beach are all
designed in `GAME_DESIGN.md` but out of scope for the Phase 1 checklist — add them as
their own biomes unlock, following the same `Data/` + manager + UI-controller pattern.
Multi-biome world scroll (real per-biome segment bounds and settle detection beyond
"always Wreck Beach") is stubbed in `WorldScrollCamera.SettledBiomeIndex()` for the
same reason — extend it once a second biome's terrain actually exists to scroll into.
