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

**Mechanical rules for every popup below:**
- Single tap to dismiss, non-blocking beyond that
- Fires exactly once per popup, ever — track "seen" state per popup id (bool array/set in PlayerState or GameManager)
- Visual treatment: placeholder now, should match the checkerboard/comic-panel aesthetic once real art exists
- Each popup should visually point at / highlight the specific object or UI element it's introducing (arrow, glow, or similar — exact treatment is an implementation detail, but "explains something off-screen with no visual anchor" should be avoided)

**Full ordered sequence for Landing Cove:**

| # | Trigger | Introduces | Placeholder text |
|---|---|---|---|
| 1 | Game first launches, before any input | Island intro | "Shipwrecked! Tuggy dropped your crew here — time to rebuild, one piece at a time." |
| 2 | Immediately after #1 (same beat, or on first look at Tuggy) | Tuggy (light touch only — NOT prestige yet) | "That's Tuggy, your ship. He'll matter more once you've built this place up." |
| 3 | On first game-view, pointing at driftwood | Core tap loop | "Tap the driftwood to collect it. This is how you'll gather most of what you need." |
| 4 | First time Driftwood ≥ crew recruit cost (25) | Directs to Menu for first recruit — CRITICAL, since world-tap doesn't work for an unrecruited crew member | "You can afford to recruit! Open the Menu (top-right) and check Crew — recruiting only works from there the first time." |
| 5 | Immediately on first-ever recruit (level 0→1, any crew member) | What recruiting does | "[Name] joined the crew! They'll now work automatically, even when you're not tapping." (This can be the same beat as the existing "joined the crew" Toast, just extended to fire this longer explanation only the very first time.) |
| 6 | On recruiting the second crew member (BBC, if BBW was first) | BBW+BBC synergy | "BBW and BBC are thick as thieves — having them both working nearby gives a bonus." |
| 7 | First time the roaming hermit crab appears on screen | Hermit crab | "Quick, tap the crab before it scurries off! Miss it and it goes to your Banked Critters instead — nothing's ever truly lost." |
| 8 | First time the Salvage Crate visibly fills partway (or first time it's full) | Salvage Crate meter | "This fills up on its own over time. Tap it once it's full for a bonus." |
| 9 | First time the bottle-cast flag point is visible/reachable | Message in a Bottle | "Cast a bottle out to sea. Check back later — you never know what'll wash back up." |
| 10 | First time the mini-boss trigger is visible/reachable | The serpent encounter | "A serpent guards the way forward. You can try anytime, but you'll need to grow stronger to actually land a hit." |
| 11 | First time a fight actually starts (tapping the serpent while strong enough to matter, or first tap regardless) | Fight mechanics | "Tap the serpent to attack! Damage carries over between attempts — you don't have to win in one go. Your crew will join in too." |
| 12 | Immediately on first mini-boss defeat | Construction reveal | "Defeated! You've discovered what's needed to build onward — gather the materials and tap Build when ready." |
| 13 | Immediately on unlocking cove 2 (Tide Pools) | Fast-travel ribbon | "You can now scroll to Tide Pools! Tap the handle in the bottom-left anytime to jump straight there." |

**Beyond Landing Cove**: each subsequent cove's newly-unlocked mechanic (Tidepooling at cove 2, Foraging at cove 3, Artifacts at cove 4) gets its own single contextual popup the moment that cove is reached, same pattern — not written out here since those systems aren't built yet, but Claude Code should follow the same trigger/dismiss/one-time rules established above when that time comes.

## What did NOT change

- All upgrade trees, currencies, crew roles/mechanics, Cast a Net/Bottle Toss charge system, Captain's Log, Postcards, Companions — untouched
- Landing Cove's existing art, layout, clusters, fight system rebuild (in-scene, crew participation, juice animations) — untouched, still fully valid
- UI philosophy (persistent chrome + contextual world hotspots + menu drawer for abstract systems) — untouched
- The fast-travel ribbon's mechanical design — still valid, just now only ever needs to hold up to 4 slots total instead of scaling toward 18
