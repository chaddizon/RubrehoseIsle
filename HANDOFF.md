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
validated world geometry; **Tide Pools** (cove 1) and **The Grove** (cove 2) have placeholder
scene content built but not yet visually tuned in Play mode; **The Deep Reef** (cove 3)
doesn't exist in Unity yet.

## The current progression shape (read this before touching cove/fight code)

**Correction (2026-08-29, from Chad directly — supersedes a framing mistake made earlier the
same day): the "tutorial" is unlocking the entirety of Wreck Beach, all 4 coves — not just
coves 1-3.** Cove 4 still unlocks a feature on arrival (Artifacts, per the doc's cove table —
exact name/shape TBD), which makes reaching it just as much an "unlock a mechanic" beat as
coves 1-3, not yet the start of "real" endgame play. Each of the 4 coves is a one-time
mini-boss gate with no payment step anymore — defeating a cove's mini-boss immediately
advances `coveIndex` to the next cove (camera auto-pans to reveal it, a popup announces it).
There is no "gather materials, then build a crossing" step; that system existed briefly and
was **removed entirely** on 2026-08-27 (see below).

**"Real" gameplay begins only once the whole island is unlocked and freely scrollable across
all 4 coves.** At that point the loop becomes: manage resources across all 4 coves
simultaneously, upgrade everything with an upgrade path (characters, Cove Buildings — Chad
wants these deliberately slow/expensive, a major long-term time-and-resource sink, not a
quick side-goal — and whatever else ends up with an upgrade tree), while continuously
attacking the permanent endgame boss that lives in cove 4 (rightmost cove) — a serpent for
now, possibly other monster types at higher levels later, not yet decided. That endless fight
mirrors Idle Obelisk Miner's real Obelisk-fight formula 1:1
(`GameFormulas.SerpentArmorForLevel`, cited armor formula including the ×9.5 jump past level
60) and never truly ends — beating it respawns a tougher version and `serpentLevel` climbs
forever.

**Open design question, not yet decided — don't build around either answer yet:** whether
the cove-4-unlock feature (Artifacts/"artificing", currently table-mapped to cove 4) should
stay there, or move to cove 3 instead (freeing cove 4 to be purely dedicated to the endgame
boss fight, with no other mechanic-unlock attached to arriving there). Chad is still thinking
this through — ask before assuming either shape if it becomes load-bearing for other work.

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
   cove-unlock-with-pan-reveal, and building-stage-complete. One row is honestly NOT wired:
   Salvage Crate fill (no backing state exists anywhere in `PlayerState`/`GameManager` for it).
   Hermit-crab/bottle-flag/mini-boss-visibility rows are treated as unconditionally true from
   cove load, since nothing in this vertical slice actually gates their visibility yet — see
   the class comment in `OnboardingController.cs` for the honest accounting of what's real
   vs. approximated vs. deferred. (Artifacts' trigger was deferred at the time this row was
   written; it's wired for real now, see item 6 below.)
5. **The Grove built** (`GroveBuilder.cs`, new, cove index 2): 2-cluster layout (Canopy/
   Frontier, same "no Dock" reasoning as Tide Pools). Real mini-boss/fight wiring; placeholder
   crew spot and 3 Foraging interaction points (no backing minigame or Grove crew member exist
   yet). `WreckBeachData.CoveNames`/`SerpentNames` cove-3/4 slots renamed to match the doc's
   locked names (`The Grove`, `The Deep Reef`) as part of this — see the naming section above.
6. **Artifacts system built** (`NEXT_CLAUDE_CODE_PUSH.md` §1) — the account-wide, endgame-
   facing counterpart to Cove Buildings' per-cove sink:
   - **Acquisition**: `TellSpot.cs`/`TellSpawner.cs` (new) — 2 dormant "tell" placeholders per
     built cove (Landing Cove, Tide Pools, The Grove), each cove's `TellSpawner` cycling one
     of them "live" (glint overlay + tappable) on a randomized timer, weighted toward rarer
     Compass Shard tiers in later coves / at higher `serpentLevel`
     (`RarityTier`/`CompassShardCatalog.cs`, new). Untapped live tells auto-collect instead of
     expiring the reward. **Important divergence from the spec**: the doc asked to "reuse the
     existing Salvage Crate fill-then-ready timer state machine" — no such system exists in
     code (Salvage Crate is documented elsewhere as visual-only, no backing logic). `TellSpawner`
     is a brand-new timer, not a reuse of anything. Its live/timer state is also NOT persisted
     across app restarts (a scope simplification, not yet raised with Chad) — collected Shards
     themselves ARE saved immediately, only an in-flight unclaimed live tell reverts to dormant
     if the app closes first.
   - **Spending**: Menu → Artifacts panel (`ArtifactsMenuPanel.cs`/`ShardStackItemUI.cs`/
     `ArtifactNodeUI.cs`, new) — browse/appraise Shards by tier, spend appraised Shards on a
     4-node placeholder tree (`ArtifactNodeCatalog.cs`) gated to `serpentLevel` milestones.
     Account-wide, not per-cove, by design. `GameManager.TapPower` now also folds in
     `ArtifactTapPowerBonusSum` as its own separate multiplicative factor from
     `BuildingTapPowerBonusSum` — adds to the same rebalancing gap flagged above, not a new one.
   - **Fast-travel ribbon badges** (`FastTravelSlotUI.cs`/`FastTravelRibbonController.cs`/
     `FastTravelRibbonBuilder.cs`): a small glow dot per expanded-ribbon slot, plus one on the
     collapsed handle when some *other* unlocked cove has a live tell — backed by a
     runtime-only (not persisted) registry on `GameManager` (`CoveHasLiveTell`/
     `SetCoveTellLive`).
   - Previously-deferred onboarding row now wired for real: `GameManager.OnCompassShardFound`
     fires once on the very first Shard ever found, regardless of tier.
7. **Build hints — persistent markers** (`BuildHintMarker.cs`, new) — complements (doesn't
   replace) onboarding popup #13. Wired to the exact same `GetBuildingStage`/
   `IsBuildingCoveReached` state `CoveBuildingVisual` already reads, so it appears/disappears
   in lockstep with a building's zero-presence window. Landing Cove's Hut gets the "obvious"
   variant (icon + floating text, wired in `LandingCoveBuilder.cs`) since it's the player's
   first exposure to Buildings; no other cove has a Cove Building yet to attach the "subtle"
   (icon-only) variant to.
8. **Menu Drawer rebuilt to be row-list-driven** (`MenuDrawerController.cs`/
   `MenuDrawerBuilder.cs`, both substantially rewritten) — a `List<Row>` (id/button/panel)
   instead of one hardcoded field pair per row, since every system now needs a reachable entry
   point. 13 rows total: Crew/Upgrades/Buildings/Artifacts/Stats are real panels; Message in a
   Bottle/Cast a Net, Captain's Log, Milestones, Tidepooling, Foraging, Postcards, Companions
   are shared "Coming soon" stub panels (`MenuDrawerBuilder.BuildStubPanel`); Settings
   (`SettingsMenuPanel.cs`, new) is a partial real panel — a genuinely-wired sound toggle
   (`AudioListener.volume`) plus a deliberately-inert Reset Save button (logs only — NOT wired
   to actually delete save data without an explicit ask) and static credits text. Stats
   (`StatsMenuPanel.cs`, new) is a real simple numbers dump (Driftwood, tap power, coves
   unlocked, serpent level, clears, crew recruited) — all of it already existed in
   `GameManager`/`PlayerState`, nothing new needed there.
   **Manual step needed**: the live scene's `MenuDrawer` GameObject still has the *old*
   per-row serialized fields baked into `WreckBeach.unity` (e.g. `crewRowButton`,
   `artifactsRowButton`) — harmless orphaned data, not a compile error, but re-run
   **Build Persistent UI → Menu Drawer** to get the real rebuilt 13-row hierarchy; don't expect
   the old scene object to already reflect any of this.

## Naming inconsistency — resolved 2026-08-29

`WreckBeachData.CoveNames` now matches `CORE_PROGRESSION_RESTRUCTURE.md`'s locked names
exactly: `{"Landing Cove", "Tide Pools", "The Grove", "The Deep Reef"}`. (This doc previously
flagged this as a known-but-unfixed gap; it's fixed now, done naturally while building The
Grove's scene content.)

## Known gaps / explicitly deferred (don't re-derive these, they're settled)

- **Crew → fight-damage bonus**: still aspirational copy in `IN_SCENE_FIGHT_SYSTEM.md`, not
  real. `TapPower` is `f(tapLevel) × (1 + BuildingTapPowerBonusSum) × (1 + ArtifactTapPowerBonusSum)`,
  no crew term yet.
- **IAP influence on Obelisk's model**: still explicitly out of scope; needs a fresh
  web-enabled Claude session to research separately.
- **Postcards/Companions**: referenced as rewards in `CoveBuildingCatalog.cs`'s Stage 2/3
  flavor text, but neither system is implemented anywhere in code — those rewards are
  currently just descriptive strings shown in the onboarding popup, not real grants. Both also
  have "Coming soon" stub Menu Drawer entries now (see item 8 above).
- **The Deep Reef** (cove 3) has zero scene content — no background art exists for it yet
  either. `GameManager` already handles all 4 coves' state generically — no code changes
  needed to build it, just the same cluster-builder method the other 3 coves already went
  through (copy `GroveBuilder.cs` as the closest/most recent template).
- Tide Pools' and The Grove's own Cove Buildings don't exist yet (`CoveBuildingCatalog.cs`
  only has Landing Cove's Hut) — the doc leaves this "TBD" per-cove. Same for their Artifacts
  tell-spot "live glint" art and per-cove idle-loop base sprites (currently generic placeholder
  circles/squares for all 3 built coves).
- **Tell timer/live state isn't persisted** across app restarts (see item 6 above) — a real
  decision worth making (mirroring `GameManager.ApplyOfflineEarnings`'s elapsed-time approach)
  if it turns out to matter in practice; not done because it wasn't explicitly asked for and
  adds real scope.
- **Rarity-tier weighting formula** (`TellSpawner.RollRarityTier`) and the 4-node
  `ArtifactNodeCatalog` list are both rough, unflagged-as-final placeholders per the doc's own
  "use reasonable placeholders and flag them" instruction — expect both to be redesigned once
  real balance/simulation work happens, same status as `CoveBuildingCatalog.cs`'s numbers.

## Suggested first things to check in a fresh session

1. Read `UNITY_SETUP.md` in full — canonical build/setup doc, kept in sync with all the above.
2. Open Unity, let it recompile, run **Build Landing Cove** → **Build Tide Pools** → **Build
   The Grove** → **Build Persistent UI → Menu Drawer** → **Build Persistent UI → Fight
   Overlay**, in that order (Menu Drawer and Fight Overlay need to run *after* whichever cove
   builders exist, or they won't find everything to wire — Menu Drawer specifically needs a
   fresh rebuild even on an existing scene, since it changed structurally this push, see item
   8 above).
3. If a save from before 2026-08-27 is loaded, several `PlayerState` fields have been
   renamed/added/removed since (`coveConstructionRevealed`/`constructionComplete` ->
   `reachedEndlessCove`, new `shardStacks`/`artifactNodes` lists) — Unity's JSON deserializer
   just drops unknown fields and defaults new/renamed ones, so this is safe, not a migration
   concern.
4. Every new world object this push (tell spots, the build-hint marker) uses eyeballed
   placeholder anchors, same as every prior cove's initial pass — expect a nudging session
   once actually seen in Play mode, especially The Grove (never validated at all yet) and the
   3 coves' newly-added tell-spot positions specifically.
