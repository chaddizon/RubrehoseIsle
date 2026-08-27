# Handoff — Rubrehose Isle session summary (through 2026-08-27)

Catch-up doc for a fresh Claude session picking this project back up. Written from inside
`RubrehoseIsle` (Unity project root — see `UNITY_SETUP.md` for how to open/build it).

## Project in one paragraph

Rubrehose Isle is an idle/incremental mobile game mirroring **Idle Obelisk Miner**'s
progression math (see `GAME_DESIGN.md`), themed around a shipwrecked crew rebuilding a
deserted island. `rubrehose_prototype.html` (repo root) is a working single-page browser
prototype that ports Obelisk's actual formulas/state model — it's the ground truth for "does
this match Obelisk," not a design doc. Only **Wreck Beach** (biome 0) is built in Unity so
far, and only its first cove, **Landing Cove**, has real world geometry. `UNITY_SETUP.md` is
the canonical build/setup doc — start there for anything editor-workflow-related.

## ⚠️ Repo state: most of this session's work is UNCOMMITTED

`git status` currently shows a large working-tree diff — the entire in-scene fight rewrite,
crew participation, crew recruit-visibility, and the pacing retune below are all sitting
**uncommitted** in the working tree. Only two commits from this session actually landed:

- `52cd457` — BBW attack animation art (2 frames)
- `def7af4` — BBC attack animation art (3 frames)

Everything else described below is real, working-tree code that compiles (verified via the
Unity Editor's `Logs/Editor.log`, since there's no way to run Unity headless from here) but
has not been committed. Don't assume `git log` reflects the current feature set — read the
working tree.

## Architecture map (new/changed files this session)

| File | Role |
|---|---|
| `Assets/Scripts/Combat/FightController.cs` | Lives directly on the Serpent world GameObject (not a modal). Owns fight state/timer/HP, input (`OnMouseDown`), and a screen-space overlay that tracks the serpent's world position every frame. Static `IsFightActive` / `ActiveSerpent` let other systems (crew) react to a fight without a direct reference. |
| `Assets/Scripts/World/SerpentVisual.cs` | Wake-up / hit-flash / settle-dormant / defeat reactions via transform+color tweens — no dedicated serpent art exists yet, all placeholder-driven. Snaps straight to "defeated" on scene load if already beaten (reads `GameManager.CoveMinibossDefeated`). |
| `Assets/Scripts/UI/DamagePopup.cs` | Runtime-spawned floating damage numbers (including a muted "0" for armor-blocked hits). |
| `Assets/Scripts/UI/Toast.cs` | Runtime-spawned small screen-space notice (currently used only for "X joined the crew!"). Finds `PersistentUICanvas` by name. |
| `Assets/Scripts/Core/Palette.cs` | Runtime-visible mirror of a few palette colors — `RubrehoseEditorUtils`' copies are Editor-only and unreachable from runtime scripts. |
| `Assets/Editor/FightOverlayBuilder.cs` | Replaces the deleted `FightModalBuilder.cs`. Builds the serpent's small floating HP-bar/timer panel (no backdrop, no buttons) and wires it into the existing `FightController` (found via `FindFirstObjectByType`). |
| `Assets/Editor/LandingCoveBuilder.cs` | Frontier cluster now builds a `Serpent` GameObject (white/no-tint, monochrome-character rule) with `SerpentVisual`+`FightController` attached, instead of the old placeholder `MiniBossTrigger`. Also wires BBW/BBC attack frame arrays and per-crew `attackOffset`. |
| `Assets/Scripts/World/CrewHomeSpotAnimator.cs` | Heavily rewritten this session — see below. |
| `Assets/Scripts/World/MiniBossTrigger.cs` | **Deleted** — its job (starting a fight on tap) is now just `FightController.OnMouseDown` on the Serpent itself. |
| `Assets/Scripts/Data/GameFormulas.cs` | Serpent HP/Armor base coefficients retuned this session (see Pacing below). |

## What changed this session (chronological)

### 1. Fight modal → in-scene fight (per `IN_SCENE_FIGHT_SYSTEM.md`)
Replaced the old full-screen fight modal entirely. The serpent is now a persistent world
object at Landing Cove's Frontier spot; tapping it wakes it up and starts the fight, tapping
it again deals damage, an HP/timer overlay floats above its world position (screen-space
overlay, tracks position every frame), and juice was added: damage number pop-ups, hit-
flash/flinch, smooth HP bar animation, a defeat animation, and a timer color-shift when time
is low. All fight *mechanics* (persistent HP across attempts, 30s duration, 20-min cooldown,
unlimited attempts) were kept exactly as they were — this was presentation-only at the time.

### 2. Crew fight participation (`CrewHomeSpotAnimator.cs`)
Initially: recruited crew swapped to an in-place "attack" sprite while a fight was active.
Revised per follow-up request: recruited crew now **physically walk** from their Camp
HomeSpot out to the serpent (reusing their existing idle frames as a walk-cycle at a faster
fps — no new art needed for this), hold their attack loop at a small per-crew offset
(`attackOffset`, BBW flanks left/BBC right so they don't stack) for the fight's duration
(polled directly off `FightController.IsFightActive`, not event-timing-dependent), then walk
the same frames back home and resume normal state. `FightController.ActiveSerpent` (static)
gives them a destination.

BBW's attack art (`bbwattacking1.png`/`bbwattacking2.png`, 2 frames) and BBC's
(`bbcattacking1-3.png`, 3 frames) both landed and are wired and committed. Both needed two
fixups after landing: (1) the filenames guessed in `LandingCoveBuilder.cs` before the art
existed were wrong (`bbwattack1-3` vs the real `bbwattacking1-2`) and had to be corrected;
(2) the new PNGs auto-imported as plain textures, not sprites (`textureType: 0`), so
`AssetDatabase.LoadAssetAtPath<Sprite>` would've silently returned null — fixed by hand-
editing the `.meta` files to match the sibling sprites' import settings.

### 3. Crew visibility + recruit celebration
Discussed whether recruiting should mirror Obelisk more directly — verified against
`rubrehose_prototype.html` that BBW/BBC's cost (25), rate (0.5/s), and lack of any unlock
gating already match Obelisk exactly, and that Obelisk's prototype has **no** toast/animation
for recruiting at all (plain always-visible button). Decided to add a Rubrehose-specific
touch anyway. Final behavior (after one revision):

- BBW/BBC have **no sprite and no collider at all until recruited** — nothing stands there,
  nothing is tappable.
- The instant a crew member's level first goes from 0→1, a one-shot celebration plays (scale
  pop + a `Toast` reading "$Name joined the crew!"), then they become a real, full-color,
  tappable, working presence at that spot.
- **Important consequence**: since the world collider is only enabled *after* the first
  recruit, `CrewRecruitSpot`'s world tap can never perform the *first* recruit anymore — that
  now only works via **Menu → Crew** (`CrewListItemUI`, fully independent of any world
  collider). World tap and menu both work for leveling further, once recruited.

### 4. Pacing retune (this exchange, not yet playtested)
Diagnosed why Landing Cove's serpent was trivially easy (~under a minute with a fresh save):
`GAME_DESIGN.md` documented a "Clears needed = (cove+1) × ClearMult" repeated-defeat
requirement (5 clears for Landing Cove) that was **never implemented** — the actual code
(and the Obelisk prototype it explicitly matches) uses a simpler single-persistent-HP-until-
dead model instead, and the leftover HP/Armor numbers (50 HP / 8 Armor) were far too small
for that model to gate anything meaningful. Decision: keep the existing mechanic (no
clears-counter, stay matched to the prototype), retune the numbers instead. New base
coefficients in `GameFormulas.cs`: **HP 50→36000, Armor 8→15** for Landing Cove (same
per-cove/per-biome growth curves, only the starting magnitude changed). Target: ~50-60 real
attempts to clear, which at the real 20-minute cooldown floors out to several real days even
for a very dedicated player and multiple weeks for a casual 2-3-checks/day player — both
playstyles valid, active is just faster. This is explicitly a first-pass estimate (see the
comment block above `SerpentHp`/`SerpentArmor` in `GameFormulas.cs`), not validated against
real elapsed-time playtesting yet. `GAME_DESIGN.md`'s formulas section and its stale
"repeated serpent-tier clears" language were updated to match and to stop contradicting the
code.

## Known gaps / explicitly deferred (don't re-derive these, they're settled)

- **Crew → fight-damage bonus**: `IN_SCENE_FIGHT_SYSTEM.md` mentions crew bonuses "already
  factor into fight damage via `crewSubBonusSum`" — that's aspirational copy, not real.
  `TapPower` is currently pure `f(tapLevel)`, no crew term. **Deliberately deferred** — user
  says this is planned as part of a separate upgrade tree (discussed in another session), not
  part of the pacing work above.
- **IAP influence on Obelisk's model**: explicitly out of scope for this session; user wants
  a fresh Claude *with web access* to research Obelisk's actual IAP model separately before
  any decision here.
- **BBC's attack sprite naming convention** was a guess before the art landed and turned out
  right this time (`bbcattacking1-3.png`), but treat any similar not-yet-painted asset path
  in `LandingCoveBuilder.cs` as unconfirmed until the file actually exists.
- Only Landing Cove (cove 0 of biome 0) has real world geometry. Debris Field / Low Tide
  Flats (coves 1-2) and every biome past Wreck Beach are unbuilt; `GameManager` already
  handles their state generically (formulas are biome/cove-generic), just no scene content.
- `ConstructionCost`/`SerpentClearReward` (2500/250 driftwood for Landing Cove) were **not**
  touched in the pacing retune — by the time a player realistically clears the retuned
  serpent (days of crew income), these will likely feel trivial. Left alone deliberately
  (construction is meant to be a small formality after the real gate, the serpent fight), but
  flagged in case it turns out to feel wrong once playtested.

## Suggested first things to check in a fresh session

1. Read `UNITY_SETUP.md` in full — it's the maintained source of truth for build order and
   known follow-ups, kept in sync all session.
2. Open the Unity Editor, let it recompile, run **Build Landing Cove** then **Build Persistent
   UI → Fight Overlay** (that exact order matters — see the order note in `UNITY_SETUP.md`).
3. Delete the old orphaned `PersistentUICanvas/FightModal` GameObject if it's still in the
   scene (leftover from before the modal→in-scene switch) — its script reference is now
   missing and it can shadow the real `FightController`.
4. Consider committing the working-tree changes described above before doing anything else —
   right now a huge amount of working code exists only locally.
