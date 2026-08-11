# Rubrehose Island — Art Asset Checklist

Organized by build priority. Everything in Phase 1 is enough for a playable Wreck Beach vertical slice. Later phases unlock as we build further biomes.

**Technical prep note (applies to everything below):** export as PNG with transparent background, separate layers per moving part where animation is planned (head / body / arm / leg as individual files) rather than one flattened image, at 2x resolution minimum for crisp scaling on modern phone screens.

---

## Phase 1 — Wreck Beach vertical slice (MVP)

### Characters
- [ ] **BBW** — idle pose (standing, arms loose)
- [ ] **BBW** — tap-reaction pose (the "rock on" hand pose you already have works great)
- [ ] **BBW** — working/scavenging pose
- [ ] **BBC** — idle pose
- [ ] **BBC** — tap-reaction pose
- [ ] **BBC** — working/scavenging pose
- [ ] **Hatchling serpent** — idle/threat pose
- [ ] **Hatchling serpent** — hit-reaction pose
- [ ] **Shoal-back serpent** — idle/threat pose
- [ ] **Shoal-back serpent** — hit-reaction pose

### UI chrome
- [ ] Menu button icon (checkerboard motif, per earlier direction)
- [ ] Tap-target button treatment ("BE BAD!"-lettering style action button)
- [ ] Driftwood resource icon
- [ ] Generic currency pill/badge background
- [ ] Progress bar fill + track (cove clear progress)
- [ ] Construction gate icon (Bound Rope material)

### Background — Wreck Beach
- [ ] Sky layer
- [ ] Water layer
- [ ] Sand/beach layer
- [ ] Wreckage hull (broken ship piece, background decoration)
- [ ] First hut — 3 build states (rubble to half-built to complete), since huts should visibly progress as you spend resources
- [ ] Campfire — unlit / lit states

---

## Phase 2 — The Shallows

### Characters
- [ ] **Lucette** — idle pose (patient/quiet)
- [ ] **Lucette** — tantrum-release pose (the "explosive" burst state)
- [ ] **Tide-coiled serpent** — idle + hit-reaction poses
- [ ] The Feline (if introduced early) — idle pose, cameo/easter-egg only at this stage

### UI chrome
- [ ] Shells/Pearls currency icon
- [ ] Tidepooling tab icon
- [ ] Totem icons (Shellwork Totem, Patient Tide Totem — 2 to start)
- [ ] Stat point icons (Grip / Patience / Luck — could reuse your eye motifs here)

### Background — The Shallows
- [ ] Reef edge / low-tide terrain layer
- [ ] Tidepool cluster (3 individual pool graphics, ideally with subtle "glint" bonus-event variant)
- [ ] Coral break decoration
- [ ] Reef Bridge — construction states (matches the biome-transition gate)

---

## Phase 3 — The Green

### Characters
- [ ] **Bramblefang serpent** — idle + hit-reaction poses
- [ ] BBW + BBC synergy pose (special art for when both are active together — worth a unique piece since it's a named mechanic)

### UI chrome
- [ ] Forage Tokens currency icon
- [ ] Foraging tab icon
- [ ] Grove tick-meter fill treatment (Palm / Fern / Vine — could be color-coded via your palette even in monochrome via value shifts)
- [ ] Recipe icons (Woven Basket, Herbal Wrap)

### Background — The Green
- [ ] Jungle canopy layer
- [ ] Cleared path decoration
- [ ] Three grove backdrops (Palm Grove, Fern Thicket, Vine Tangle)
- [ ] Jungle Path — construction states

---

## Phase 4 — The Bluffs

### Characters
- [ ] **Lucy** — idle pose
- [ ] **Lucy** — "Double or Nothing" action pose
- [ ] **Storm-wound serpent** — idle + hit-reaction poses

### UI chrome
- [ ] Message Fragments currency icon
- [ ] Bottle-cast button treatment
- [ ] Bottle-in-flight / washed-ashore states (the bottle prop, animated bobbing)
- [ ] Captain's Bottle jackpot variant (should read as visually special — bright/high-contrast even in mono via stark value contrast)

### Background — The Bluffs
- [ ] Cliff/lookout terrain layer
- [ ] Lookout Tower — construction states
- [ ] Signal fire / flag decoration

---

## Phase 5 — The Hollow

### Characters
- [ ] **The Feline** — full character (idle, work poses) if not done in Phase 2
- [ ] **Cave-blind serpent** — idle + hit-reaction poses (note: this fight is screen-dimmed/rhythm-based per design, so this serpent may need a more minimal or silhouette-only treatment)

### UI chrome
- [ ] Cave Glimmer currency icon
- [ ] Torch/lantern resource meter
- [ ] Light-radius visual effect (whatever represents shrinking/growing light)

### Background — The Hollow
- [ ] Cave mouth exterior
- [ ] Cave interior layers (glowing fungus/crystal accents)
- [ ] Deep Passage — construction states

---

## Phase 6 — The Deep Reef (finale, this island)

### Characters
- [ ] **Abyssal Coil serpent** — idle + hit-reaction poses (this is the showpiece boss — probably worth the most detail/largest scale of any single asset)

### Background — The Deep Reef
- [ ] Final zone backdrop, most visually "complete" state of the island
- [ ] Full populated camp scene (all recruited crew visible at once)

---

## Cross-cutting / anytime

- [ ] Compass Shard icon (prestige currency)
- [ ] Artifact icons (4 to start: Tap Mastery, Crew Efficiency, Serpent Slayer, Deep Pockets)
- [ ] Tuggy — full ship art for the prestige "set sail" moment/screen
- [ ] Disembodied-eyes motif as rare-event/bonus indicator
- [ ] Heart-glasses motif as critical-hit/jackpot indicator
- [ ] Checkerboard pattern as a reusable UI tile/background element
- [ ] "BE BAD!" lettering treatment as a reusable button/label style

---

## Notes
- Idle/tap-reaction/work poses per character are enough for the first pass of the animation plan (code-driven procedural motion — squash/stretch, bounce, position swap). No walk cycles or rigged animation needed yet.
- If you commission real rigged animation later, start with BBW and Lucy — your two most mechanically prominent characters (Wreck Beach lead + Bluffs unlock).
