using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Shoreline's Message-in-a-Bottle cast point (GAME_DESIGN.md "Message in a Bottle",
    // WRECK_BEACH_CHECKLIST.md). A bottle washes up at a random moment within a tide
    // window and must be tapped before it drifts back out; missing it just re-schedules
    // the next wash-up. Tide flips every 20-30 min and reweights the reward pool.
    //
    // This is the bottle itself — sitting in the water, separate from the always-visible
    // flag-pole marker (LandingCoveBuilder's sibling "FlagPole" object) that just marks the
    // spot. Renderer/collider are toggled via .enabled rather than GameObject.SetActive() so
    // Update()'s tide/wash-up timer keeps running the whole time regardless of visibility —
    // SetActive(false) would pause it along with everything else on the object.
    //
    // No dedicated Message Fragments currency yet (that arrives at The Bluffs per
    // GAME_DESIGN.md's biome table) — v1 rewards pay out in Driftwood so the loop is
    // playable now; retarget RollReward's payout once a bottle-specific currency exists.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PixelSnappedBob))]
    public class BottleCastPoint : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D pointCollider;
        [SerializeField] private PixelSnappedBob bob;

        [Header("Wash-up timing (seconds) — random point within the tide window")]
        [SerializeField] private float minWashUpDelay = 45f;
        [SerializeField] private float maxWashUpDelay = 150f;
        [SerializeField] private float driftBackOutAfter = 20f; // must be caught before this

        [Header("Tide window (minutes) — reweights the reward pool while active")]
        [SerializeField] private float minTideMinutes = 20f;
        [SerializeField] private float maxTideMinutes = 30f;

        [Header("Reward tiers (common / uncommon / Captain's Bottle jackpot)")]
        [SerializeField] private double commonReward = 15;
        [SerializeField] private double uncommonReward = 60;
        [SerializeField] private double captainsBottleReward = 400;
        [SerializeField, Range(0f, 1f)] private float uncommonChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float captainsBottleChance = 0.03f;

        private bool _choppyTide; // choppy tide skews the pool toward uncommon finds
        private float _tideTimer;
        private bool _washedUp;
        private float _timer;

        private void OnEnable()
        {
            RollNextTide();
            ScheduleNextWashUp();
            UpdateVisual();
        }

        private void Update()
        {
            _tideTimer -= Time.deltaTime;
            if (_tideTimer <= 0f) RollNextTide();

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_washedUp)
            {
                _washedUp = false; // missed it — drifts back out, try again next window
                ScheduleNextWashUp();
            }
            else
            {
                _washedUp = true;
                _timer = driftBackOutAfter;
            }
            UpdateVisual();
        }

        private void OnMouseDown()
        {
            if (!_washedUp) return;

            _washedUp = false;
            UpdateVisual();
            GameManager.Instance.AddDriftwood(RollReward());
            ScheduleNextWashUp();
        }

        private void RollNextTide()
        {
            _choppyTide = Random.value < 0.5f;
            _tideTimer = Random.Range(minTideMinutes, maxTideMinutes) * 60f;
        }

        private void ScheduleNextWashUp() => _timer = Random.Range(minWashUpDelay, maxWashUpDelay);

        private void UpdateVisual()
        {
            spriteRenderer.enabled = _washedUp;
            pointCollider.enabled = _washedUp;
            bob.enabled = _washedUp;
        }

        private double RollReward()
        {
            float uncommonOdds = _choppyTide ? uncommonChance * 1.5f : uncommonChance;
            float roll = Random.value;
            if (roll < captainsBottleChance) return captainsBottleReward;
            if (roll < captainsBottleChance + uncommonOdds) return uncommonReward;
            return commonReward;
        }
    }
}
