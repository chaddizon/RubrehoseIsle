# Wreck Beach — Full Completion Checklist

Everything needed for Wreck Beach to be a complete, polished, playable vertical slice — combining Unity/code tasks (for the Claude Code session) with art tasks (for you, with dimensions specified).

## Already done ✅
- [x] Unity project scaffolded, GameManager singleton
- [x] Core data model (PlayerState, BiomeCatalog) with biomeUnlocked tracking
- [x] WorldScrollCamera — horizontal drag/bounds/pan, settles-and-fires-event behavior
- [x] Fast-travel ribbon — collapsed handle, expanding ribbon, auto-built via Editor menu command
- [x] Git repo tracking all of the above

## A. Unity / code tasks remaining

### Core HUD
- [ ] Build the actual driftwood counter, biome tag, cove progress bar (was scaffolded conceptually but not finished — confirm current state with Claude Code)
- [ ] Tap-power upgrade row (level + cost + upgrade button)
- [ ] Crew list (BBW, BBC) with recruit buttons, live rate display

### World interactivity (the "whole screen is the setting" layer)
- [ ] Tappable driftwood objects placed IN the scene (not a UI button) — each needs a script that adds Driftwood on tap, then respawns/repositions after a short delay
- [ ] Crew "home spots" — BBW and BBC appear physically in the scene once recruited, at fixed world positions, playing an idle animation
- [ ] Hut construction states — visual swap (rubble → half-built → complete) triggered by construction-gate spend, not just a menu number changing
- [ ] Serpent encounter trigger — tapping/approaching the frontier edge of the currently-unlocked cove opens the fight modal

### Fight system
- [ ] Fight modal UI (HP bar, timer, attack button) — logic exists conceptually from the prototype; needs real Unity UI built
- [ ] Wire fight outcome to cove-clear counting and cove progression

### Cove progression (the 3 sub-sections within Wreck Beach)
- [ ] Landing Cove → Debris Field → Low Tide Flats as sequential sections of the same horizontal strip, each revealed/scrollable once the prior cove's clear requirement is met
- [ ] Construction gate UI at the far right edge (Low Tide Flats → build the Reef Bridge → Shallows unlocks)

### Message in a Bottle (unlocks at Wreck Beach per spec)
- [ ] Cast-bottle interaction point near the shoreline
- [ ] Bottle wash-up random timer + collect interaction
- [ ] Reward tiers (common/uncommon/Captain's Bottle) wired to currencies

### Prestige
- [ ] "Tuggy's Supply Run" prestige button/screen
- [ ] Compass Shards → Artifact spending screen (can be simple for v1)

### Systems / persistence
- [ ] Save/load to `Application.persistentDataPath` (mentioned as already planned — confirm it's actually implemented, not just described)
- [ ] Offline progress calculation on relaunch
- [ ] Milestone tracking (at least the Wreck-Beach-relevant ones)

## B. Art assets needed, with exact specs

All exports: PNG, transparent background, @2x resolution (i.e. export at 2x the listed size so it stays crisp), separate files per listed pose (not one sheet) unless noted.

### Characters
| Asset | Size (@1x, export @2x) | Notes |
|---|---|---|
| BBW — idle pose | 200×260px | Standing, arms loose, matches existing reference art scale |
| BBW — tap-reaction pose | 200×260px | The "rock on" pose you already have works |
| BBW — working/scavenging pose | 200×260px | For the "home spot" idle-work animation |
| BBC — idle pose | 200×260px | |
| BBC — tap-reaction pose | 200×260px | |
| BBC — working/scavenging pose | 200×260px | |
| Hatchling serpent — idle/threat | 300×220px | Wider than tall, serpent silhouette |
| Hatchling serpent — hit-reaction | 300×220px | |
| Shoal-back serpent — idle/threat | 320×240px | Slightly larger than Hatchling |
| Shoal-back serpent — hit-reaction | 320×240px | |

### World objects
| Asset | Size (@1x) | Notes |
|---|---|---|
| Driftwood piece (tappable), variant 1 | 60×40px | Small, scattered on beach |
| Driftwood piece, variant 2 | 60×40px | For visual variety when multiple spawn |
| Driftwood piece, variant 3 | 50×35px | |
| Hut — rubble state | 140×110px | Starting/unbuilt look |
| Hut — half-built state | 140×110px | Same footprint, more complete |
| Hut — complete state | 140×110px | Same footprint, fully built |
| Campfire — unlit | 70×60px | |
| Campfire — lit | 70×60px | Can be a 2-frame flicker if you want simple animation later |
| Wrecked hull (background decoration) | 300×220px | Sits near the start of Landing Cove |
| Bottle (message-in-a-bottle prop) | 40×60px | Needs a "floating/bobbing" look for the cast/collect point |

### UI chrome
| Asset | Size | Notes |
|---|---|---|
| Driftwood currency icon | 32×32px | For the top currency pill |
| Generic currency pill background | 9-slice or 120×32px | If not doing 9-slice, keep it simple rectangle+rounded corners |
| Progress bar fill + track | 300×12px each | Cove-clear progress |
| Tap button background | 9-slice or 340×60px | "BE BAD!"-style lettering treatment |
| Menu button icon (checkerboard) | 32×32px | |
| Fast-travel handle biome thumbnail (Wreck Beach) | 44×44px | Matches the ribbon spec's circular frame |

## C. Reference docs already in the repo
- `GAME_DESIGN.md` — full system spec
- `CAMERA_AND_UI_SPEC.md` — camera + fast-travel behavior
- `rubrehose_art_checklist.md` — original phased list (this doc supersedes its Phase 1 section specifically, with exact dimensions added)
