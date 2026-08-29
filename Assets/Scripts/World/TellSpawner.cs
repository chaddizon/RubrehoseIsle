using System.Collections;
using UnityEngine;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.World
{
    // Per-cove Artifacts tell-spawn timer (NEXT_CLAUDE_CODE_PUSH.md §1a).
    //
    // NOTE on the doc's instruction to "reuse the existing Salvage Crate fill-then-ready timer
    // state machine": no such system actually exists in code. UNITY_SETUP.md documents Salvage
    // Crate as "visual-only... no backing systems exist" — there's nothing to reuse. This is a
    // NEW timer built fresh; flagging the mismatch rather than silently building something that
    // contradicts the actual repo state.
    //
    // Also NOT persisted across app restarts (which tell is live, how much of the current
    // interval has elapsed) — a deliberate scope simplification, not raised with Chad yet. Any
    // shard actually collected IS persisted immediately (GameManager.AddCompassShard), so
    // nothing already earned is lost; only an in-flight, unclaimed live tell reverts to
    // dormant if the app closes before it's tapped or its window elapses. Worth a real decision
    // (mirroring GameManager.ApplyOfflineEarnings' elapsed-time approach) if this turns out to
    // matter in practice.
    public class TellSpawner : MonoBehaviour
    {
        [SerializeField] private int coveIndex;
        [SerializeField] private TellSpot[] tells;
        [SerializeField] private float minIntervalSeconds = 180f;
        [SerializeField] private float maxIntervalSeconds = 420f;

        private Coroutine _cycleRoutine;

        private void OnEnable() => _cycleRoutine = StartCoroutine(CycleRoutine());

        private void OnDisable()
        {
            if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
        }

        private IEnumerator CycleRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minIntervalSeconds, maxIntervalSeconds));
                if (tells == null || tells.Length == 0) continue;

                var dormant = System.Array.FindAll(tells, t => !t.IsLive);
                if (dormant.Length == 0) continue; // one tell is already live in this cove — wait for it to be collected

                var chosen = dormant[Random.Range(0, dormant.Length)];
                chosen.GoLive(coveIndex, RollRarityTier(), null);
            }
        }

        // Placeholder weighting (NEXT_CLAUDE_CODE_PUSH.md §1c: "weight tell spawns in later
        // coves, and tells that go live at higher serpentLevel, toward rarer tiers") — not
        // tuned, flagged same as every other placeholder number in this push.
        private string RollRarityTier()
        {
            int serpentLevel = GameManager.Instance.SerpentLevel;
            float rarityScore = coveIndex + Mathf.Log(serpentLevel + 1, 4f); // 0 at cove 0 / level 0, grows slowly
            float roll = Random.value + rarityScore * 0.15f;

            if (roll >= 1.8f) return RarityTier.Legendary;
            if (roll >= 1.2f) return RarityTier.Epic;
            if (roll >= 0.7f) return RarityTier.Rare;
            return RarityTier.Common;
        }
    }
}
