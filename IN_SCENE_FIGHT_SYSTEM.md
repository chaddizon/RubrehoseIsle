# In-Scene Fight System — Replaces the Fight Modal

Supersedes the original modal-based fight design. Matches Obelisk's actual model: the boss is a persistent presence in the main scene, fought by tapping it directly, not through a separate screen.

## Core change

The serpent no longer opens in a popup. Instead, activating the Frontier trigger causes the serpent to appear/activate directly at that position in Landing Cove itself. HP bar, timer, and stats render as a **screen-space overlay anchored above the serpent's world position** (like a floating status bar), not a full-screen takeover. The rest of the scene (Camp cluster, water, sky) stays fully visible and unpaused around it.

**Activation beat**: a small "wake up" animation when the fight starts — eyes opening, emerging slightly from the sand/water — replaces the old "transition into fight screen," since there's no screen to transition into anymore.

**Attack input**: tapping the serpent directly (not a separate "Attack" button) deals damage, consistent with tapping driftwood/every other world object.

**Retreat**: simply means the fight timer runs out or the player stops tapping; the serpent deactivates/settles back down until the cooldown ends and it can be re-engaged.

## Crew participation

Every currently-recruited crew member (BBW, BBC, and future recruits) visually joins the fight when active: they walk from their HomeSpot out to the serpent's position (their existing idle frame set doubles as the walk-cycle during transit — it already reads as a walking pose, no separate art needed), switch to their **attack animation** on arrival and hold it for the fight's duration, appearing to strike the serpent alongside the player's taps, then walk the same idle frames back to their HomeSpot once the fight ends and resume idle/working. Each recruit is offset from the serpent's exact position so multiple attackers flank it rather than stack on one point. This makes their existing mechanical contribution (crew bonuses already factor into fight damage via `crewSubBonusSum`) visible rather than an invisible stat.

**New sprite requirement per recruited character**: a 2-3 frame attack loop, same canvas size and reference-image technique already used for idle/working sets (86×140 for BBW and BBC). Suggested prompt pattern (swap in the correct tap-reaction file as Reference image 1):

> Reference image 1 shows the character's face and style in clearest detail — use for design/palette only. Reference image 2 shows a pose to use as the base stance. Generate 2-3 frames showing an attacking motion — a swipe, bite, or pounce toward something off to the side, energetic and aggressive in feel (contrast with the idle/working loops' calmer energy). Camera angle and character scale must stay consistent with the reference. Same canvas size, same monochrome/beige palette, fully transparent background, isolated character only.

## Animation/juice requirements

- **Damage number pop-ups**: a number floats up and fades on every successful hit (player tap or crew attack)
- **Hit-flash/flinch**: the serpent briefly flashes or recoils on each successful hit
- **Smooth HP bar fill**: animates toward the new value over a short duration, doesn't snap instantly
- **Defeat animation**: a distinct death/retreat animation on the serpent when HP reaches 0, not an instant disappear
- **Timer warning**: the countdown visually shifts (e.g. color change) when time is running low, signaling urgency

## What stays the same

All underlying formulas, pacing, and mechanics are unchanged: persistent HP across attempts, 30-second fight duration, 20-minute cooldown, unlimited attempts, defeat immediately reveals the construction requirement. Only the *presentation* moved from modal to in-scene.
