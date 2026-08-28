# Core Progression Restructure — supersedes prior biome/cove model

This is a major structural change agreed on 2026-08-27, replacing the "6 biomes × 3 coves" plan described in GAME_DESIGN.md and CAMERA_AND_UI_SPEC.md. Read this doc as the current source of truth for progression structure; the older docs' *systems* (upgrade trees, currencies, crew roles, fight mechanics, UI layout method) are all still valid and untouched — only the *shape of the world and unlock order* changed.

## The core change

**Wreck Beach's 4 coves now ARE the entire base game**, not a tutorial leading into 5 more biomes. Once all 4 are unlocked, the player scrolls freely between them for the rest of normal play. This mirrors Idle Obelisk Miner's real structure much more closely than the original plan did — Obelisk is one mine with one continuously-scaling boss, not dozens of discrete zones each needing their own art and boss design.

**Terminology going forward:**
- **Cove** = one of the 4 scrollable zones making up the entire base game (unchanged term, matches what's already built — `LandingCoveBuilder.cs` etc. don't need renaming)
- **Biome** = retired as a near-term concept. Repurposed to mean a future, rare, endgame-only full island expansion (see GAME_DESIGN.md's existing note: "a genuinely new island... unlocked after fully maxing the current island... not part of routine prestige" — that decision now applies to biomes generally, not just a second island specifically)

## The 4 coves

| # | Cove | Unlocks | Boss behavior |
|---|---|---|---|
| 1 | Landing Cove (built) | Tap/crew/combat basics, Message in a Bottle, Tuggy/Prestige | One-time defeat gate (existing mini-boss model, currently being retuned for fast early pacing) |
| 2 | Tide Pools (built) | Tidepooling | One-time defeat gate |
| 3 | The Grove | Foraging | One-time defeat gate |
| 4 | The Deep Reef | Artifacts | **Permanent, endlessly-scaling fight — this is our Obelisk** |

Coves 1-3 keep the exact mini-boss model already built (persistent HP, 30s duration, 20-min cooldown, unlimited attempts, single defeat unlocks the next cove). Only cove 4's serpent is different in kind, not just degree.

**The Deep Reef's visual identity**: deliberately water-dominant, distinct from the other three sandy-island coves — much less exposed land, jagged reef rocks breaking the surface instead of a beach, deep dark turquoise water filling most of the frame, optionally the silhouette of something large and coiled just beneath the surface as a background tease. This is the one cove that should look and feel like the permanent endgame, not another stop along the way.

## Serpent tier flavor names (cosmetic labels on top of `serpentLevel`)

The endless Deep Reef serpent shouldn't just show a bare level number — reuse the tier names originally designed for the old 6-biome plan as flavor labels at specific `serpentLevel` milestones, giving the grind a sense of earned narrative escalation:

Hatchling → Shoal-back → Tide-coiled → Bramblefang → Storm-wound → Cave-blind → Abyssal Coil

Exact level thresholds for each name are not yet set — needs a milestone table once real pacing data exists (see the Balance-tab simulation work referenced elsewhere in this project). For now, treat this as an ordered list to map onto level ranges later, not a final schedule.

## Cove 4's serpent — the permanent "Obelisk" fight

Not a one-time boss. Defeating it immediately respawns a tougher version and increments a persistent `serpentLevel` counter — this is the never-ending late-game grind, and it should use Idle Obelisk Miner's actual real formulas directly, since this is now our closest 1:1 equivalent to their core loop:

```
Armor(level) = round(10 × 2.8^(level-1))   for level 1-61
             then armor(60) × 9.5^(level-60)   for level 61+
```
(mirrors the real cited Obelisk formula exactly, including the ×9.5 growth-rate jump after level 61)

HP should use a comparable exponential shape (reuse the existing `SerpentHp`/`SerpentArmor` structure in `GameFormulas.cs`, replacing its per-cove/per-biome inputs with this single continuously-incrementing `serpentLevel`).

Artifact unlocks (from Compass Shards) should map to specific `serpentLevel` milestones, mirroring how Obelisk gates Stargazing/Challenges/etc. to specific Obelisk levels.

## Tuggy's travel animation

When the player's scroll/swipe gesture settles on a new cove (finger released, camera motion stops), Tuggy animates cruising in from the left or right edge of the screen to his resting position at bottom-left, rather than simply appearing there.

**Direction logic**: Tuggy enters from whichever side matches the direction of travel — if the player scrolled from a cove further left, he cruises in from the left; if from a cove further right, from the right. This needs the cove-scroll system to expose "previous cove index" vs "new cove index" so the direction can be derived (previous < new → enter from left; previous > new → enter from right).

**Art status**: placeholder/static for now. Chad will provide dedicated Tuggy movement/cruising sprites separately — build the animation system to receive a sprite sequence via the same Inspector-field pattern used everywhere else, don't hardcode frame count assumptions.

## Onboarding / tutorial system

Currently the game launches directly into Landing Cove with zero introduction — needs fixing before this feels like a polished, released game.

**Design pattern, matching Obelisk's real approach**: Obelisk does not use one large upfront tutorial. (cite index="10-1">Features unlock progressively tied to specific levels, each presumably announced as it happens.</cite> Note: no verbatim record of Obelisk's actual popup text/wording exists publicly — this is a faithful mechanical match to their pattern, not a literal copy.

**Our implementation:**
- A short, simple intro sequence on first launch only — a few lightweight popups introducing the absolute basics (tap to collect driftwood, recruit crew, the goal of defeating the mini-boss) before the player is set loose
- After that, **no more upfront tutorial** — instead, a short contextual popup fires the moment each new mechanic actually becomes available: first cove-4 serpent encounter explains the endless-fight concept, unlocking Tide Pools explains Tidepooling right there at that cove, unlocking Foraging explains itself at that cove, Artifacts explained on first Compass Shard earned, etc.
- Popups should be dismissible with a single tap, non-blocking beyond that, and should never re-trigger once dismissed (track "seen" state per popup, likely a bool array or set in `PlayerState`/`GameManager`)
- Visual treatment: match the established checkerboard/comic-panel aesthetic already used for the menu drawer, not a generic system dialog box

## What did NOT change

- All upgrade trees, currencies, crew roles/mechanics, Cast a Net/Bottle Toss charge system, Captain's Log, Postcards, Companions — untouched
- Landing Cove's existing art, layout, clusters, fight system rebuild (in-scene, crew participation, juice animations) — untouched, still fully valid
- UI philosophy (persistent chrome + contextual world hotspots + menu drawer for abstract systems) — untouched
- The fast-travel ribbon's mechanical design — still valid, just now only ever needs to hold up to 4 slots total instead of scaling toward 18
