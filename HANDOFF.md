# Handoff — Rubrehose Isle session summary (through 2026-08-29)

Catch-up doc for a fresh Claude Code session picking this project back up. Written from
inside `RubrehoseIsle` (Unity project root — see `UNITY_SETUP.md` for how to open/build it).
For the higher-level "why" and art-direction history, read `PROJECT_HANDOFF_MASTER.md`
first, then come back here for exact current code state.

## Project in one paragraph

Rubrehose Isle is an idle/incremental mobile game mirroring **Idle Obelisk Miner**'s
progression math, themed around a shipwrecked crew rebuilding a deserted island.
`rubrehose_prototype.html` (repo root) is a working single-page browser prototype that ports
Obelisk's actual formulas/state model — ground truth for "does this match Obelisk," not a
design doc. **The world is no longer "6 biomes"** — per `CORE_PROGRESSION_RESTRUCTURE.md`,
Wreck Beach's 4 coves ARE the entire base game. Only **Landing Cove** (cove 0) has real,
validated world geometry; **Tide Pools** (cove 1) has placeholder scene content built but not
yet visually tuned in Play mode; coves 2-3 don't exist in Unity yet.

## The current progression shape (read this before touching cove/fight code)

**Coves 1-3 are, functionally, the tutorial.** Each is a one-time mini-boss gate with no
payment step anymore — defeating a cove's mini-boss immediately advances `coveIndex` to the
next cove (camera auto-pans to reveal it, a popup announces it). There is no "gather
materials, then build a crossing" step; that system existed briefly and was **removed
entirely** on 2026-08-27 (see below). Once you clear cove 3, you land in **cove 4 (the
endless cove)** — the *actual* core loop this game is built around: a permanent,
endlessly-scaling serpent fight that mirrors Idle Obelisk Miner's real Obelisk-fight formula
1:1 (`GameFormulas.SerpentArmorForLevel`, cited armor formula including the ×9.5 jump past
level 60). Beating it doesn't end anything — it respawns tougher and `serpentLevel` climbs
forever. Everything before that point (recruiting BBW/BBC, tap upgrades, the 3 finite coves)
is onboarding-flavored ramp-up to this one endless grind, matching how Obelisk itself is one
mine with one continuously-scaling boss, not a sequence of discrete zones.

**Cove Buildings** are a second, fully separate system layered on top — NOT part of cove
progression at all. One building per cove (currently only Landing Cove's Hut is designed),
3 paid stages each, granting real tap-power bonuses. A building has zero presence in the
world (no sprite, no collider) until its Stage 1 is paid via **Menu → Buildings** — that
panel is the only way to pay a building's first stage; once visible, it's directly tappable
in-world for Stages 2-3.

## ⚠️ Rebalancing required, not yet executed

Cove Buildings grant real numeric bonuses that stack multiplicatively on the existing
tap/serpent curve (`EXPANDED_UPGRADES_AND_BALANCE.md`'s additive-within/multiplicative-across
rule). `CORE_PROGRESSION_RESTRUCTURE.md` is explicit that the base curve's growth rate needs
to come down to compensate, and that this must be derived via simulation (the same approach
used for the earlier serpent HP/Armor pacing retune — `rubrehose_prototype.html`'s Balance
tab, or an equivalent Unity-side sim), not asserted from formulas by hand. **This has not been
done.** Every cost/reward number in `CoveBuildingCatalog.cs` and every HP/Armor coefficient in
`GameFormulas.cs` should be treated as an unbalanced placeholder until that pass happens.

## What changed 2026-08-27 → 2026-08-29 (chronological, on top of the in-scene-fight-system session before it)

1. **Core progression restructure** (`CORE_PROGRESSION_RESTRUCTURE.md`): retired the
   6-biome/18-cove plan. Wreck Beach's 4 coves are the whole base game now; cove 4 is the
   permanent Obelisk-equivalent fight (`GameManager.IsEndlessCove`/`SerpentLevel`,
   `GameFormulas.SerpentHpForLevel`/`SerpentArmorForLevel`). Tuggy gained a cove-to-cove
   cruise-in animation (`TuggyTravelController.cs`) — now attached and wired automatically by
   `Build Landing Cove` (was a manual Inspector step briefly, automated after that proved
   error-prone).
2. **Tide Pools scaffolded** (`TidePoolsBuilder.cs`, new): 3-cluster layout (Grove/TidePools/
   Frontier, no Dock — Tuggy stays anchored to Landing Cove). Real mini-boss/fight wiring;
   placeholder crew spot and 3 Tidepooling interaction points (no backing minigame exists
   yet). Anchors are eyeballed against the flat background PNG, not yet validated in Play mode.
   `FightOverlayBuilder.cs` was fixed to wire its shared HP-bar overlay to *every*
   `FightController` in the scene (was only wiring the first one found, which would've left
   Tide Pools' serpent with no UI); `FightController.cs` gained an instance-local
   `_showingOverlay` flag so two coves sharing one overlay object don't fight over its position.
3. **Construction gate removed entirely, Cove Buildings added** (this is the big one):
   - Deleted `ConstructionGate.cs`/`HutConstructionState.cs`. Mini-boss defeat
     (`GameManager.RegisterMiniBossDefeat`) now advances `coveIndex` immediately — no more
     `ConstructionCost`/`SerpentClearReward` (`GameFormulas.cs`), no more
     `PlayerState.coveConstructionRevealed`. `constructionComplete` renamed to
     `reachedEndlessCove` (same meaning: true once you reach cove 4).
   - New `CoveBuildingCatalog.cs` (Landing Cove's Hut only so far, 3 stages, placeholder
     numbers), `CoveBuildingVisual.cs` (zero-presence-until-earned, same hidden mechanism
     already used for BBW/BBC), `BuildingsMenuPanel.cs`/`BuildingListItemUI.cs` + a
     `Assets/Prefabs/BuildingListItem.prefab` auto-saved by `MenuDrawerBuilder.cs`.
   - `GameManager.TapPower` now folds in `BuildingTapPowerBonusSum` — see the rebalancing
     warning above.
   - New `GameManager.OnCoveUnlocked` event: `CoveViewCamera` subscribes to auto-pan-reveal
     the newly unlocked cove (reuses the existing swipe-pan machinery, just triggered by the
     event instead of a finger release) instead of a silent cut.
4. **Full 14-popup onboarding sequence** (`OnboardingController.cs`, rewritten): 3
   unconditional intro popups, contextual triggers for afford-recruit/first-recruit/
   both-recruited/building-affordable, and event-driven popups for fight-start,
   cove-unlock-with-pan-reveal, and building-stage-complete. Two rows from the doc's table
   are honestly NOT wired: Salvage Crate fill (no backing state exists anywhere in
   `PlayerState`/`GameManager` for it) and Artifacts (no Compass Shard counter exists yet).
   Hermit-crab/bottle-flag/mini-boss-visibility rows are treated as unconditionally true from
   cove load, since nothing in this vertical slice actually gates their visibility yet — see
   the class comment in `OnboardingController.cs` for the honest accounting of what's real
   vs. approximated vs. deferred.

## Known naming inconsistency (flagged, not fixed)

`CORE_PROGRESSION_RESTRUCTURE.md` has since locked cove names as **The Grove** (cove 2) and
**The Deep Reef** (cove 3) — but `WreckBeachData.cs` still uses the older placeholder names
("Foraging Grounds", "The Deep"). Not fixed here since it wasn't in scope for the sessions
that did the work above; do it as its own small pass (rename the two strings in
`WreckBeachData.CoveNames`) whenever convenient — no other code depends on the literal string
values.

## Known gaps / explicitly deferred (don't re-derive these, they're settled)

- **Crew → fight-damage bonus**: still aspirational copy in `IN_SCENE_FIGHT_SYSTEM.md`, not
  real. `TapPower` is `f(tapLevel) × (1 + BuildingTapPowerBonusSum)`, no crew term yet.
- **IAP influence on Obelisk's model**: still explicitly out of scope; needs a fresh
  web-enabled Claude session to research separately.
- **Postcards/Companions**: referenced as rewards in `CoveBuildingCatalog.cs`'s Stage 2/3
  flavor text, but neither system is implemented anywhere in code — those rewards are
  currently just descriptive strings shown in the onboarding popup, not real grants.
- The Grove (cove 2) and The Deep Reef (cove 3) have zero scene content. `GameManager`
  already handles all 4 coves' state generically — no code changes needed to build them,
  just the same cluster-builder method Landing Cove and Tide Pools already went through.
- Tide Pools' own Cove Building doesn't exist yet (`CoveBuildingCatalog.cs` only has Landing
  Cove's Hut) — the doc leaves this "TBD" per-cove.

## Suggested first things to check in a fresh session

1. Read `UNITY_SETUP.md` in full — canonical build/setup doc, kept in sync with all the above.
2. Open Unity, let it recompile, run **Build Landing Cove** → **Build Tide Pools** → **Build
   Persistent UI → Menu Drawer** → **Build Persistent UI → Fight Overlay**, in that order (the
   Menu Drawer and Fight Overlay builds need to run *after* whichever cove builders exist, or
   they won't find everything to wire).
3. If a save from before 2026-08-27 is loaded, `coveConstructionRevealed`/
   `constructionComplete` no longer exist as field names — Unity's JSON deserializer will just
   drop unknown fields and default the renamed/new ones, so this is safe, not a migration
   concern.
