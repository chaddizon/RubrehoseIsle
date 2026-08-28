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

**REMOVED: the "construction gate" step.** Coves 1-3 previously required defeating the mini-boss, then separately gathering Driftwood to "build the crossing" before the next cove unlocked. That step is gone — it existed to justify physically bridging separate landmasses, which no longer applies now that Wreck Beach is one continuous scrollable island, not disconnected zones. **Defeating a cove's mini-boss now unlocks the next cove immediately, no intermediate resource payment.** `ConstructionCost`/`SerpentClearReward` values in the existing code are obsolete and should be removed, not retuned.

**New unlock moment**: the instant a cove unlocks, the camera pans across the newly revealed cove (a deliberate reveal beat, not an instant cut or silent unlock) while a notification announces what's new there — see the Onboarding section below for exact popup content per cove.

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

## Cove Buildings — separate from cove-unlock progression entirely

A second, fully independent progression track. **Nothing about advancing between coves depends on this system** — it's a parallel, optional wealth sink for players who want a deeper long-term goal, similar to how Obelisk's Construct/Monument system exists alongside, not gating, floor progression.

**One building per cove** (Landing Cove's Hut is built; equivalents for Tide Pools, The Grove, and The Deep Reef follow the same system, reskinned per cove theme — exact per-cove building concept TBD, not needed until those coves are actually built).

**Visibility rule — stricter than earlier drafts**: a building has **zero presence of any kind — no sprite, no rubble, nothing — until its first stage is actually paid for.** Not even a placeholder "something could go here" object. The island should look genuinely bare on arrival. The *only* signal that a building is buildable is a notification (see Onboarding section) — the world itself stays silent until something is actually earned.

**Cost/reward shape**: 3 stages per building, cost escalating steeply — Stage 1 relatively easy (a real but achievable early goal), Stage 2 a serious investment, Stage 3 a genuine long-term achievement plausibly not reached until after all of Wreck Beach is unlocked and the player has real accumulated wealth.

**Rewards are real, not purely cosmetic** — reversing an earlier draft of this doc. Players sinking that much into a building will expect it to matter. Each stage grants a mix of:
- A **numeric progression bonus** (tap power, resource yield, etc. — see rebalancing note below), thematically tied to the specific building/cove
- A cosmetic and/or lore element (ambient scene detail, a Postcard, a Companion, per the existing Postcards/Companions systems)

**Worked example — the Hut (Landing Cove)**:
- Stage 1: modest cost. Reward: small Driftwood tap-power bonus (thematically — a proper home base sharpens focus)
- Stage 2: serious cost. Reward: larger tap-power bonus + a Postcard
- Stage 3: major long-term cost. Reward: further tap-power bonus + a Companion + a cosmetic crew accent

Other coves' buildings should tie their numeric bonus to something thematically local once designed (e.g., Tide Pools' building boosting Tidepooling yield, The Grove's boosting Foraging yield) rather than every building just granting generic tap power — "make it make sense per cove."

### Rebalancing requirement — important, not yet executed

Since building stages now grant real numeric bonuses, they become an additional multiplicative lever on top of the existing tap/crew/artifact stack (per the additive-within/multiplicative-across rule in EXPANDED_UPGRADES_AND_BALANCE.md). Left unaddressed, this would make players stronger than the serpent curve was tuned for, breaking the Obelisk-matched pacing work already done.

**Required fix**: lower the baseline growth rate of the core tap-power/serpent-curve formulas so that total power — base formula + building bonuses combined — lands back on the originally-targeted Obelisk-matched pacing curve, rather than simply stacking building bonuses on top of an already-tuned curve. **Exact numbers are not derivable by hand reliably** — this needs the same simulation approach used for the earlier pacing retune (rubrehose_prototype.html's Balance tab, or an equivalent Unity-side simulation) run with building bonuses included, not asserted from formulas alone.

## Narrative notes (not yet implemented — logged for future work)

An opening story beat has been decided in concept, not yet built: Tuggy and the crew are shipwrecked/stranded together, and characters are "recruited" one by one as the player finds each of them a job on the island — the recruit moment doubles as a narrative beat, not just a mechanical unlock. Chad will continue developing this narrative and provide more detail over time; treat this as a placeholder note for a future onboarding/opening-sequence pass, not something to build now.

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
| 12 | Immediately on any cove's mini-boss defeat, ALL coves (1→2, 2→3, 3→4) | Cove unlock — camera pans across the newly revealed cove during/after this popup, not a silent unlock | "Defeated! [Next Cove Name] is revealed — scroll over anytime." (Cove 1's version can additionally mention the fast-travel handle, since that's the first time it becomes useful: "...or tap the handle in the bottom-left to jump straight there.") |
| 13 | First time enough Driftwood exists to afford a building's Stage 1 (any cove) | Cove Building system | "You could build something here. [Building name], Stage 1 — [cost] to start." |
| 14 | On completing any building stage | That stage's specific reward | "[Building name] Stage [N] complete! [reward summary]." |

**Note**: popup #12 replaces the old "construction reveal" beat entirely (that system no longer exists) and now fires at every cove transition, not just once.

**Beyond Landing Cove**: each subsequent cove's newly-unlocked mechanic (Tidepooling at cove 2, Foraging at cove 3, Artifacts at cove 4) gets its own single contextual popup the moment that cove is reached, same pattern — not written out here since those systems aren't built yet, but Claude Code should follow the same trigger/dismiss/one-time rules established above when that time comes.

## What did NOT change

- Core upgrade trees, currencies, crew roles/mechanics, Cast a Net/Bottle Toss charge system, Captain's Log, Postcards, Companions — untouched
- Landing Cove's existing art, layout, clusters, fight system rebuild (in-scene, crew participation, juice animations) — untouched, still fully valid
- UI philosophy (persistent chrome + contextual world hotspots + menu drawer for abstract systems) — untouched
- The fast-travel ribbon's mechanical design — still valid, just now only ever needs to hold up to 4 slots total instead of scaling toward 18

## What changed in THIS revision, summarized

- Construction gate (mini-boss → gather materials → build crossing → unlock) is **removed entirely**. Mini-boss defeat now unlocks the next cove directly.
- Cove Buildings (Hut, and per-cove equivalents) are now a **separate, optional system** with no bearing on cove-unlock progression, granting real numeric bonuses (not purely cosmetic) — see the Cove Buildings section above.
- Building visibility is stricter than earlier drafts: **zero presence until Stage 1 is paid for**, not even a rubble placeholder.
- Every cove unlock now gets a camera pan-reveal + notification (popup #12), not just the first one.
- Base progression formulas will need rebalancing once building bonuses are added, to keep the overall curve matched to Obelisk's real pacing — not yet executed, flagged as a required follow-up.
