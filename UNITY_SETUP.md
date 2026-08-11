# Unity setup — Wreck Beach vertical slice

This repo root *is* the Unity project (that's why `.gitignore` is the standard Unity
root gitignore). Only the parts that need code are scaffolded — `ProjectSettings/`,
`Packages/manifest.json`, and `Assets/Scripts/`. Everything else (the scene, prefabs,
UI hierarchy, imported art) is Editor work, per the "Build split" note in
[`GAME_DESIGN.md`](GAME_DESIGN.md).

## 1. Open the project

1. Unity Hub → **Add** → select this folder (`RubrehoseIsle`).
2. It was scaffolded against Unity **2022.3 LTS**. If you have a different 2022.3.x (or
   newer) installed, Hub will offer to open/upgrade it — that's fine, just let it.
3. First open regenerates `Library/` and the rest of `ProjectSettings/*.asset` with
   Unity's defaults — this can take a few minutes.
4. If prompted to import **TMP Essentials** (TextMeshPro), do it — the UI scripts use
   `TMP_Text`.

## 2. Create the scene

Create `Assets/Scenes/WreckBeach.unity`, then in it:

1. **GameManager** — empty GameObject, attach `GameManager.cs` (`Assets/Scripts/Core`).
   It's a `DontDestroyOnLoad` singleton, so one instance is enough.
2. **Canvas** (Screen Space – Overlay) + **EventSystem** (Unity will offer to create the
   EventSystem automatically the first time you add a Canvas).
3. Under the Canvas, build the HUD roughly following `rubrehose_prototype.html`'s
   layout (it's the validated reference for feel, not a pixel spec):
   - Resource readout: driftwood amount, biome tag, cove name + clears, a progress bar.
   - Tap button showing tap amount.
   - Tap-upgrade row (level + cost).
   - A crew list container (empty `Transform`, e.g. a `Vertical Layout Group`).
   - Fight button.
   - Construction gate section (label + build button).
4. Attach `MainHUDController.cs` (`Assets/Scripts/UI`) to the Canvas or a HUD root, and
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

## 5. Art

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

## 6. Try it

Press Play. Tap should add driftwood, recruiting BBW/BBC should start passive income,
and Fight should open the modal against the current cove's Hatchling/Shoal-back with a
30s timer. Progress autosaves every 60s and on pause/quit to
`Application.persistentDataPath`, with offline crew income (capped at 8h) applied on
next launch.

## What's deliberately not here yet

Prestige, Artifacts, Tidepooling/Foraging/Bottles, and biomes past Wreck Beach are all
designed in `GAME_DESIGN.md` but out of scope for the Phase 1 checklist — add them as
their own biomes unlock, following the same `Data/` + manager + UI-controller pattern.
