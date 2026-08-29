using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Persistent in-world marker for a Cove Building that's "known but unbuilt"
    // (NEXT_CLAUDE_CODE_PUSH.md §2) — complements, doesn't replace, onboarding popup #13's
    // one-time tap-to-dismiss notice. Appears the instant its cove is reached (while the
    // building's still at stage 0) and disappears the instant Stage 1 is paid — wired to the
    // exact same state CoveBuildingVisual already reads (GameManager.GetBuildingStage/
    // IsBuildingCoveReached), not a separate tracked flag.
    //
    // Lives as a sibling component on the same GameObject as CoveBuildingVisual, with its own
    // child visuals (iconRoot) rather than touching CoveBuildingVisual's SpriteRenderer, which
    // stays disabled for this exact same window (the building itself has zero presence until
    // Stage 1 — this marker is what fills that visual gap intentionally).
    //
    // Two variants, both driven by the same iconRoot/label fields: Landing Cove's is "obvious"
    // (icon + label text, wired by LandingCoveBuilder), later coves' should be "subtle" (icon
    // only — build with label left unassigned/no text child at all, per NEXT_CLAUDE_CODE_PUSH.md
    // §2's "don't re-teach the same lesson four times").
    public class BuildHintMarker : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private GameObject iconRoot; // icon (+ optional label child) — toggled as one unit
        [SerializeField] private float pulseSeconds = 1.2f;
        [SerializeField, Range(0f, 1f)] private float pulseStrength = 0.15f;

        private Vector3 _iconBaseScale;
        private bool _active;

        private void Awake()
        {
            if (iconRoot != null) _iconBaseScale = iconRoot.transform.localScale;
        }

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void Update()
        {
            if (!_active || iconRoot == null) return;
            float pulse = 1f + Mathf.Sin(Time.time / pulseSeconds * Mathf.PI * 2f) * pulseStrength;
            iconRoot.transform.localScale = _iconBaseScale * pulse;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            _active = gm.IsBuildingCoveReached(buildingId) && gm.GetBuildingStage(buildingId) == 0;
            if (iconRoot != null) iconRoot.SetActive(_active);
        }
    }
}
