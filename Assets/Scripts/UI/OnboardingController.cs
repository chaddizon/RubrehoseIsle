using System.Collections.Generic;
using UnityEngine;
using Rubrehose.Combat;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Orchestrates onboarding per CORE_PROGRESSION_RESTRUCTURE.md's "Onboarding / tutorial
    // system" — specifically its "Full ordered sequence for Landing Cove" table (14 rows).
    // Every popup has a stable string id tracked via
    // GameManager.HasSeenOnboarding/MarkOnboardingSeen (PlayerState.seenOnboardingIds, part of
    // the save) — once dismissed it never re-triggers, including across sessions.
    //
    // Three trigger shapes, matching the doc's own trigger column:
    //  - Unconditional intro (rows 1-3): always enqueued in OnEnable, deduped against
    //    HasSeenOnboarding like everything else, so they only actually show on a true first
    //    launch.
    //  - Polled state conditions (rows 4-10, 13, plus the pre-existing "reached cove N" flavor
    //    popups): checked in CheckContextualTriggers, which re-runs on every
    //    GameManager.OnStateChanged.
    //  - One-shot events (rows 11, 12, 14): enqueued directly from the event itself
    //    (FightController.OnFightActiveChanged, GameManager.OnCoveUnlocked,
    //    GameManager.OnBuildingStageCompleted) rather than polled, since polling can't detect
    //    a momentary event after the fact.
    //
    // Rows 7 ("hermit crab first appears") and 9 ("bottle-cast flag first visible/reachable")
    // and 10 ("mini-boss trigger first visible/reachable") are treated as unconditionally true
    // from cove load: nothing in this vertical slice actually gates those objects' visibility
    // yet (HermitCrab/FlagPole/Serpent are all placed and rendered the moment Landing Cove
    // loads, per LandingCoveBuilder — none of them "appear" at a later moment the way the doc's
    // phrasing implies a future, more staged reveal might). This is an honest read of "first
    // time visible" given today's actual implementation, not a faked trigger.
    //
    // Row 8 ("Salvage Crate visibly fills / first time it's full") is NOT implemented — there
    // is no fill-level state anywhere in PlayerState/GameManager for it
    // (PersistentHUDController.SetCrateFill exists but nothing calls it, per UNITY_SETUP.md).
    // Firing this popup on any available signal would misrepresent state that doesn't exist.
    // Wire EnqueueIfUnseen(SalvageCrateId, ...) in CheckContextualTriggers once a real crate
    // fill system exists — Compass Shards below are the resolved counterpart: that system now
    // exists (NEXT_CLAUDE_CODE_PUSH.md §1), so its previously-deferred onboarding trigger is
    // now wired for real, event-driven off GameManager.OnCompassShardFound.
    public class OnboardingController : MonoBehaviour
    {
        [SerializeField] private OnboardingPopupUI popupUI;

        // --- Rows 1-3: unconditional intro ---------------------------------------------
        private const string IntroIslandId = "intro_island";
        private const string IntroTuggyId = "intro_tuggy";
        private const string IntroTapId = "intro_tap";

        // --- Rows 4-10: polled state conditions ------------------------------------------
        private const string AffordFirstRecruitId = "afford_first_recruit";
        private const string FirstRecruitId = "first_recruit";
        private const string CrewSynergyId = "crew_synergy";
        private const string HermitCrabId = "hermit_crab";
        private const string BottleFlagId = "bottle_flag";
        private const string MiniBossVisibleId = "miniboss_visible";

        // --- Pre-existing "beyond Landing Cove" mechanic-flavor popups (doc's own note,
        // unchanged from before this revision) ---
        private const string CoveTidePoolsId = "cove_unlocked_1";
        private const string CoveForagingId = "cove_unlocked_2";
        private const string CoveEndlessId = "cove_unlocked_3";

        // --- Row 11: fight starts (event-driven) -----------------------------------------
        private const string FightStartsId = "fight_starts";

        // --- Artifacts (event-driven, previously deferred — now wired per
        // NEXT_CLAUDE_CODE_PUSH.md §1) --------------------------------------------------
        private const string FirstCompassShardId = "first_compass_shard";

        private readonly Queue<(string id, string title, string body)> _queue = new Queue<(string, string, string)>();
        private readonly HashSet<string> _queuedIds = new HashSet<string>(); // in-flight guard between enqueue and dismiss

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            gm.OnStateChanged += CheckContextualTriggers;
            gm.OnCoveUnlocked += HandleCoveUnlocked;
            gm.OnBuildingStageCompleted += HandleBuildingStageCompleted;
            gm.OnCompassShardFound += HandleCompassShardFound;
            FightController.OnFightActiveChanged += CheckFightStarted;

            EnqueueIfUnseen(IntroIslandId, "Shipwrecked!",
                "Tuggy dropped your crew here — time to rebuild, one piece at a time.");
            EnqueueIfUnseen(IntroTuggyId, "That's Tuggy",
                "Your ship. He'll matter more once you've built this place up.");
            EnqueueIfUnseen(IntroTapId, "Welcome to Wreck Beach",
                "Tap the driftwood on the shore to salvage it — that's your Driftwood, the currency for everything here.");

            CheckContextualTriggers();
            TryShowNext();
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnStateChanged -= CheckContextualTriggers;
            gm.OnCoveUnlocked -= HandleCoveUnlocked;
            gm.OnBuildingStageCompleted -= HandleBuildingStageCompleted;
            gm.OnCompassShardFound -= HandleCompassShardFound;
            FightController.OnFightActiveChanged -= CheckFightStarted;
        }

        private void CheckContextualTriggers()
        {
            var gm = GameManager.Instance;

            // Row 4: first time Driftwood >= the cheapest crew's recruit cost.
            double cheapestRecruitCost = double.MaxValue;
            foreach (var def in CrewCatalog.WreckBeachCrew)
                if (def.baseCost < cheapestRecruitCost) cheapestRecruitCost = def.baseCost;
            if (gm.State.driftwood >= cheapestRecruitCost)
                EnqueueIfUnseen(AffordFirstRecruitId, "You Can Recruit!",
                    "Open the Menu (top-right) and check Crew — recruiting only works from there the first time.");

            // Row 5: first-ever recruit, any crew member.
            bool anyRecruited = false;
            foreach (var c in gm.State.crew) if (c.level >= 1) { anyRecruited = true; break; }
            if (anyRecruited)
                EnqueueIfUnseen(FirstRecruitId, "Crew Recruited",
                    "They'll now work automatically, even when you're not tapping.");

            // Row 6: both crew members recruited (BBW+BBC synergy) — generalized to "every
            // catalog entry recruited" rather than hardcoding a BBW-first assumption, so it
            // fires correctly regardless of recruit order.
            bool allRecruited = gm.State.crew.Count > 0;
            foreach (var c in gm.State.crew) if (c.level < 1) { allRecruited = false; break; }
            if (allRecruited)
                EnqueueIfUnseen(CrewSynergyId, "Thick As Thieves",
                    "BBW and BBC are thick as thieves — having them both working nearby gives a bonus.");

            // Rows 7, 9, 10: unconditionally true from cove load — see class comment above.
            EnqueueIfUnseen(HermitCrabId, "Hermit Crab!",
                "Quick, tap the crab before it scurries off! Miss it and it goes to your Banked Critters instead — nothing's ever truly lost.");
            EnqueueIfUnseen(BottleFlagId, "Message in a Bottle",
                "Cast a bottle out to sea. Check back later — you never know what'll wash back up.");
            EnqueueIfUnseen(MiniBossVisibleId, "The Serpent",
                "A serpent guards the way forward. You can try anytime, but you'll need to grow stronger to actually land a hit.");

            // Row 13: first time enough Driftwood exists to afford ANY building's Stage 1.
            foreach (var def in CoveBuildingCatalog.Buildings)
            {
                if (gm.GetBuildingStage(def.id) != 0) continue; // only relevant pre-Stage-1
                if (!gm.CanAffordNextBuildingStage(def.id)) continue;
                EnqueueIfUnseen($"building_{def.id}_stage1_affordable", def.displayName,
                    $"You could build something here. {def.displayName}, Stage 1 — {Format.Number(def.stages[0].cost)} to start.");
            }

            // Pre-existing "beyond Landing Cove" flavor popups, unchanged.
            if (gm.State.coveIndex >= 1)
                EnqueueIfUnseen(CoveTidePoolsId, WreckBeachData.CoveNames[1],
                    "Tidepooling's open here — a new side-loop for gathering resources beyond the tap-and-crew grind.");

            if (gm.State.coveIndex >= 2)
                EnqueueIfUnseen(CoveForagingId, WreckBeachData.CoveNames[2],
                    "Foraging's open here — another way to gather resources around this cove.");

            if (gm.State.coveIndex >= 3)
                EnqueueIfUnseen(CoveEndlessId, WreckBeachData.CoveNames[3],
                    $"{WreckBeachData.SerpentNames[3]} never stays down for good — beat it and a tougher one rises immediately. " +
                    "This fight never ends; every level you clear pushes you further.");

            TryShowNext();
        }

        // Row 11: first time a fight actually starts. FightController.IsFightActive is a
        // single shared static flag (only one fight can be active at a time), so this fires
        // the first time ANY cove's fight goes active, not per-cove.
        private void CheckFightStarted()
        {
            if (FightController.IsFightActive)
                EnqueueIfUnseen(FightStartsId, "Fight!",
                    "Tap the serpent to attack! Damage carries over between attempts — you don't have to win in one go. Your crew will join in too.");
            TryShowNext();
        }

        // Row 12: fires at EVERY cove transition (1->2, 2->3, 3->4), not just the first —
        // "popup #12 replaces the old 'construction reveal' beat entirely." Cove 1's version
        // additionally mentions the fast-travel handle, since that's the first time it becomes
        // useful. Distinct id per transition (keyed by previousCoveIndex) so each fires exactly
        // once, same "fires exactly once per popup, ever" rule as every other row.
        private void HandleCoveUnlocked(int previousCoveIndex, int newCoveIndex)
        {
            string nextCoveName = WreckBeachData.CoveNames[newCoveIndex];
            string body = $"Defeated! {nextCoveName} is revealed — scroll over anytime.";
            if (previousCoveIndex == 0)
                body += " Or tap the handle in the bottom-left to jump straight there.";

            EnqueueIfUnseen($"cove_unlock_reveal_{previousCoveIndex}", "Onward!", body);
            TryShowNext();
        }

        // Row 14: fires once per (building, stage) — distinct id per stage so each of a
        // building's 3 completions gets its own one-time popup with its own reward summary.
        private void HandleBuildingStageCompleted(string buildingId, int stage)
        {
            var def = CoveBuildingCatalog.Find(buildingId);
            if (def == null || stage < 1 || stage > def.stages.Length) return;

            var completed = def.stages[stage - 1];
            string reward = completed.cosmeticRewardLabel != null
                ? $"+{completed.tapPowerBonusPercent:P0} tap power + {completed.cosmeticRewardLabel}."
                : $"+{completed.tapPowerBonusPercent:P0} tap power.";

            EnqueueIfUnseen($"building_{buildingId}_stage{stage}_complete",
                $"{def.displayName} Stage {stage} Complete!", reward);
            TryShowNext();
        }

        // Previously-deferred Artifacts trigger (NEXT_CLAUDE_CODE_PUSH.md §1), now real —
        // fires once, on the very first Compass Shard ever found, regardless of tier.
        private void HandleCompassShardFound(string tier)
        {
            EnqueueIfUnseen(FirstCompassShardId, "A Compass Shard!",
                "A piece of the wreck, washed up at last. Check Menu → Artifacts to appraise it and start recovering the ship.");
            TryShowNext();
        }

        private void EnqueueIfUnseen(string id, string title, string body)
        {
            var gm = GameManager.Instance;
            if (gm.HasSeenOnboarding(id) || _queuedIds.Contains(id)) return;

            _queuedIds.Add(id);
            _queue.Enqueue((id, title, body));
        }

        private void TryShowNext()
        {
            if (popupUI.IsShowing || _queue.Count == 0) return;

            var next = _queue.Dequeue();
            popupUI.Show(next.title, next.body, () =>
            {
                GameManager.Instance.MarkOnboardingSeen(next.id);
                _queuedIds.Remove(next.id);
                TryShowNext();
            });
        }
    }
}
