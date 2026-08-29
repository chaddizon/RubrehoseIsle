# Rubrehose Isle — Master Project Handoff (as of 2026-08-27)

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

| # | Cove | Unlocks | Boss |
|---|---|---|---|
| 1 | Landing Cove (built) | Basics, Message in a Bottle, Tuggy/Prestige | One-time defeat |
| 2 | Tide Pools (in progress) | Tidepooling | One-time defeat |
| 3 | Foraging Grounds (unnamed cove, prompt written, not yet generated as of last message) | Foraging | One-time defeat |
| 4 | Unnamed — needs a name | Artifacts | **Permanent, endlessly-scaling — this is our "Obelisk"** |

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
- Hut: 3-stage construction set (rubble/half-built/complete), redone once for a more elaborate Stage 3
- Campfire: 4-frame idle flicker loop
- Driftwood: 3 pieces, redone once for size (96×64) and once for detail-simplification
- Flag/bottle-cast marker: 5-frame flutter loop + separate bobbing bottle
- Backgrounds: Landing Cove (redone twice), Tide Pools (new, matching style)

## Immediate next steps (as of the last message in this chat)
1. Cove 3 (Foraging Grounds) background prompt was just written, not yet generated/pushed
2. Cove 4 needs a name
3. Landing Cove's object positions likely need re-tuning against its latest background redo (same pattern every prior background swap has required)
4. Per `HANDOFF.md`: verify the working-tree restructure work (4-cove system, Tuggy travel animation, onboarding system) has actually been committed, not just implemented
5. Cove 4's endless-serpent formula and cove 1-3's fast-opening pacing retune both need real playtesting once implemented — not yet validated against actual elapsed time
