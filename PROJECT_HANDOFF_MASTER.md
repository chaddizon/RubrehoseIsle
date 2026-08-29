# Rubrehose Isle — Master Project Handoff (as of 2026-08-29)

For a fresh Claude chat picking this project up with zero prior context. This complements (doesn't replace) the technical docs already in the repo — read this first for the *why* and *how we got here*, then the linked docs for exact specs.

## What this project is

An idle/incremental mobile game (Unity, iOS/Android) mirroring Idle Obelisk Miner's real progression math, themed around a shipwrecked crew rebuilding a deserted island. Solo developer (Chad), doing art himself, using Claude (this chat) for game design/art-direction help and Claude Code for actual Unity implementation. Brand name: Rubrehose (rubberhose/1930s-cartoon art style). In-game brand catchphrase: "BE BAD!"

## Repo doc index — what's where

All in the `RubrehoseIsle` repo root unless noted:

- **`GAME_DESIGN.md`** — original full system spec (currencies, formulas, crew roles). Mostly still valid; progression *structure* section is superseded by CORE_PROGRESSION_RESTRUCTURE.md.
- **`CAMERA_AND_UI_SPEC.md`** — camera model, single-screen-per-cove, fast-travel ribbon. Still valid.
- **`HUD_AND_LANDING_COVE_LAYOUT.md`** — persistent UI master table, menu drawer contents, Landing Cove's original cluster layout (positions since iterated many times in Unity directly, this doc has the *method*, not current exact numbers).
- **`EXPANDED_UPGRADES_AND_BALANCE.md`** — tiered crit, per-crew sub-trees, Captain's Log, Bottle Toss/Cast a Net charge system. Still valid.
- **`IN_SCENE_FIGHT_SYSTEM.md`** — fight system rebuilt from modal to in-scene (matches Obelisk's real model: boss lives in the world, tap it directly). Still valid.
- **`CORE_PROGRESSION_RESTRUCTURE.md`** — **the most recent major structural change.** Retired the original 6-biome/18-cove plan. Now: 4 coves total = the entire base game (not a tutorial into more biomes). Read this before assuming anything from GAME_DESIGN.md's old structure section.
- **`WRECK_BEACH_CHECKLIST.md`** — art asset checklist, has a note flagging it predates the pixel-art pivot (some dimensions are stale, check before trusting).
- **`HANDOFF.md`** — Claude Code's own session-state summary (what's committed vs uncommitted, architecture map of recent script changes). Check this for the actual current code state, since it's more current than any doc here for that specific question.
- **`rubrehose_prototype.html`** — browser prototype implementing Obelisk's real formulas. Ground truth for "does this match Obelisk," not just a mockup.

## The 4-cove structure (current, per CORE_PROGRESSION_RESTRUCTURE.md)

**The core framing to hold onto (corrected 2026-08-29 — an earlier version of this doc got
this wrong): the tutorial is unlocking the entirety of Wreck Beach, all 4 coves — not just
coves 1-3.** Cove 4 still unlocks a feature on arrival (Artifacts/"artificing" — exact
name/shape TBD), so reaching it is just as much an onboarding beat as coves 1-3. Each cove is
a one-time mini-boss gate — defeat it and you're immediately in the next cove, no payment/
construction step (that existed briefly and was removed 2026-08-27, see below).

**"Real" gameplay begins once the whole island is unlocked and freely scrollable across all
4 coves.** From there the loop is: manage resources across all 4 coves at once, upgrade
everything with an upgrade path — characters, Cove Buildings (deliberately meant to be slow
and expensive, a major long-term sink, not a quick side-goal), and whatever else gets an
upgrade tree — while continuously fighting the permanent endgame boss that lives in cove 4
(rightmost cove): a serpent for now, possibly other monster types at higher levels later
(not decided). That fight mirrors Idle Obelisk Miner's real Obelisk fight 1:1 and never
truly ends.

**Open question, not decided — don't build around either answer:** whether the cove-4-unlock
feature (Artifacts) stays on cove 4, or moves to cove 3 instead, freeing cove 4 to be purely
the endgame-boss screen with no other mechanic tied to arriving there. Chad is still thinking
this through.

| # | Cove | Unlocks | Boss |
|---|---|---|---|
| 1 | Landing Cove (built) | Basics, Message in a Bottle, Tuggy/Prestige | One-time defeat |
| 2 | Tide Pools (scaffolded, placeholder objects, not yet visually tuned) | Tidepooling | One-time defeat |
| 3 | The Grove (locked name; unbuilt) | Foraging (and maybe Artifacts, see open question above) | One-time defeat |
| 4 | The Deep Reef (locked name; unbuilt) | Artifacts (placement not final, see above) | **Permanent, endlessly-scaling — this is our "Obelisk," the real endgame boss** |

A separate, fully independent system — **Cove Buildings** — was added 2026-08-27: one
building per cove (only Landing Cove's Hut is designed so far), 3 paid stages each, granting
real tap-power bonuses. Zero presence in the world until Stage 1 is paid via a new Menu →
Buildings panel. This has NO bearing on cove-unlock progression — it's a parallel wealth
sink, same role as Obelisk's Construct/Monument system, and per Chad's framing above this is
exactly the kind of system the *post-tutorial* real game loop revolves around. **Its bonuses
stack on top of the existing tap/serpent curve and the base curve has not yet been rebalanced
to compensate** — flagged in `CORE_PROGRESSION_RESTRUCTURE.md`'s "Rebalancing requirement"
section as required follow-up, not done.

## Art direction — the important part not fully captured elsewhere

### Locked palette (exact hex, use for EVERY asset going forward)
```
#FFFFFF, #DFDFDF, #8D8E8D, #868685, #9B9895, #636363, #1D1D1D, #010101  (neutrals)
#9F9A87, #96917E, #6D6B61, #605C53  (warm beige/brown accents)
```
Pulled directly from sampling Tuggy/BBW/BBC's actual pixel data. **The whole game uses this one palette now** — the original "characters monochrome, environment colorful" split was abandoned; backgrounds, hut, driftwood, everything is now this same warm monochrome. GAME_DESIGN.md's art-direction section reflects the *old* split — needs a doc update, hasn't happened yet.

### Canvas size conventions (established through trial and error)
- **Characters** (BBW, BBC): 86×140px for animation frame sets (idle/working/attacking), though the very first tap-reaction pieces were 128×128 — some inconsistency exists, not fully reconciled
- **Tuggy**: 128×128px
- **Hut**: 192×144px (bumped up from an original 128×96 for a bigger, more "achievement-feeling" build-up)
- **Driftwood**: 96×64px (deliberately shrunk from 120×80 — the larger size felt too visually dominant in-scene, even though it was originally sized generously for tap ergonomics; 96×64 still comfortably exceeds minimum tap-target guidelines)
- **Campfire**: 64×56px
- **Flag/bottle-cast marker**: 48×64px pole, 38×48px bottle
- **Backgrounds**: 192×344px portrait, per-cove

### Pixel-art generation lessons (Pixellab.ai specifically) — save yourself the trouble we went through
- **The tool's canvas is landscape-locked** at roughly 344×192 for some generations — for portrait background art, the working method is: describe the scene pre-rotated (e.g. "the LEFT portion of this raw canvas becomes the BOTTOM of the final image"), generate, then manually rotate the result 90° afterward. Chad rotates **right (clockwise)** after generating — meaning the prompt should ask for the composition to be rotated **left** beforehand.
- **It frequently ignores requested exact pixel dimensions** — always verify actual output size programmatically before trusting it, never assume the requested size was honored.
- **"Remove Background" toggle in Pixellab was silently on for a while**, causing transparent-sky bugs that looked like the tool ignoring "fully opaque" instructions — it wasn't ignoring anything, that setting was just overriding it. Worth checking that toggle if opacity problems recur.
- **For animation frame sets, always use the "Reference image 1/2/3/4" labeling Pixellab expects**, and always reference a *specific single pose* explicitly as "the pose to keep" — early attempts that referenced multiple poses loosely produced frames from completely different camera angles (a real bug: a hut's half-built stage came out as a diagonal corner-view while the finished stage was front-facing — cost real regeneration credits to fix). The fix that worked: generate the "hero"/final version first, then feed *that* back as Reference image 1 for earlier construction stages.
- **Always verify content-bounds consistency across animation frames** before wiring them up in Unity — check actual pixel bounding-box height/width match across frames (a quick Python/PIL `getbbox()` check), not just eyeballing. This caught a real bug once (a hut's half-built stage was 8px taller than its finished stage, which would've made it visually "shrink" when construction completed) and confirmed several other sets were clean (near-identical bounds = safe to loop, campfire and BBC's working-loop were pixel-perfect examples).
- **Simple, readable silhouettes beat detailed "hero" art for frequently-repeated tap targets** — the first driftwood batch was too detailed (individual rope/barnacle/moss texture) and had to be redone simpler, since a tap target needs instant at-a-glance recognition, not admiration.

### Current asset inventory (pushed to Unity as of last message)
- Tuggy (final monochrome version, with idle float animation)
- BBW: tap-reaction, idle loop (2 frames), working loop (4 frames), attack loop (2 frames)
- BBC/"Big Bad 'Coon": tap-reaction, idle loop (4 frames), working loop (3 frames), attack loop (3 frames)
- Hut: 3-stage set (originally rubble/half-built/complete construction art; reused as-is for
  the Cove Building system's 3 paid stages after the construction-gate mechanic they were
  drawn for was removed), redone once for a more elaborate Stage 3
- Campfire: 4-frame idle flicker loop
- Driftwood: 3 pieces, redone once for size (96×64) and once for detail-simplification
- Flag/bottle-cast marker: 5-frame flutter loop + separate bobbing bottle
- Backgrounds: Landing Cove (redone twice), Tide Pools (new, matching style)

## Immediate next steps (as of 2026-08-29)
1. Tide Pools' object positions need a nudging pass once actually seen in Play mode (anchors
   were eyeballed against the flat background PNG, same follow-up Landing Cove's own anchors
   already went through once).
2. The Grove (cove 3) and The Deep Reef (cove 4) have no background art or scene content yet.
3. **Rebalancing pass required, not yet done**: Cove Buildings' tap-power bonuses stack on
   top of the existing tap/serpent curve — the base curve's growth rate needs to come down to
   compensate, via simulation (`rubrehose_prototype.html`'s Balance tab or equivalent), not
   hand-derived math. See `HANDOFF.md` for exactly which formulas/files are affected.
4. Neither The Grove nor The Deep Reef has a Cove Building designed yet (`CoveBuildingCatalog.cs`
   only has Landing Cove's Hut) — design one per cove once each cove itself is built, per
   CORE_PROGRESSION_RESTRUCTURE.md's "make it make sense per cove" note (e.g. Tide Pools'
   building should boost Tidepooling yield specifically, not generic tap power).
5. `WreckBeachData.cs`'s cove name strings still say "Foraging Grounds"/"The Deep" — the doc
   has since locked "The Grove"/"The Deep Reef." Small rename, not yet done.
6. Cove 4's endless-serpent formula and coves 1-3's fast-opening pacing retune both need real
   playtesting once implemented — not yet validated against actual elapsed time.
