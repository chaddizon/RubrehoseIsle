using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Drives a Cove Building's presence AND appearance (CORE_PROGRESSION_RESTRUCTURE.md
    // "Cove Buildings"): entirely absent — no sprite, no collider — until its Stage 1 is
    // paid for, mirroring CrewHomeSpotAnimator's exact "hidden until earned" pattern for
    // BBW/BBC (same SpriteRenderer.enabled/Collider2D.enabled=false-until-earned mechanism,
    // not a separate one). Stage 1 can only ever be paid via Menu -> Buildings
    // (BuildingsMenuPanel), since nothing exists in-world to tap before then; once visible,
    // tapping it in-world pays for the NEXT stage directly (2, then 3) — same "first action
    // menu-only, further action world-tappable too" precedent CrewRecruitSpot/
    // CrewHomeSpotAnimator already established for crew recruiting.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class CoveBuildingVisual : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("index 0 = Stage 1's sprite, index 1 = Stage 2's, index 2 = Stage 3's.")]
        [SerializeField] private Sprite[] stageSprites;

        [Header("Stage-complete pop (juice only, same shape as CrewHomeSpotAnimator's recruit celebration)")]
        [SerializeField] private float stageCompletePopSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] private float stageCompletePopStrength = 0.2f;

        private Collider2D _collider;
        private int _lastKnownStage;
        private bool _popping;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            // Baseline from whatever's already true on load (e.g. a save where this building
            // is already at stage 2) — only a live stage increase during play should trigger
            // the pop below, never a fresh scene load.
            _lastKnownStage = GameManager.Instance.GetBuildingStage(buildingId);
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

        // Can't fire at all pre-Stage-1 (collider disabled) — no separate guard needed for that.
        private void OnMouseDown() => GameManager.Instance.PayNextBuildingStage(buildingId);

        private void Refresh()
        {
            if (_popping) return;
            int stage = GameManager.Instance.GetBuildingStage(buildingId);

            if (stage <= 0)
            {
                spriteRenderer.enabled = false;
                _collider.enabled = false;
                _lastKnownStage = 0;
                return;
            }

            if (stage > _lastKnownStage)
            {
                _lastKnownStage = stage;
                StartCoroutine(StageCompletePop(stage));
                return;
            }

            ApplyStage(stage);
        }

        private void ApplyStage(int stage)
        {
            spriteRenderer.enabled = true;
            _collider.enabled = true;
            spriteRenderer.sprite = stageSprites[Mathf.Clamp(stage - 1, 0, stageSprites.Length - 1)];
        }

        private IEnumerator StageCompletePop(int stage)
        {
            _popping = true;
            ApplyStage(stage);

            Vector3 baseScale = transform.localScale;
            float t = 0f;
            while (t < stageCompletePopSeconds)
            {
                t += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(t / stageCompletePopSeconds * Mathf.PI) * stageCompletePopStrength;
                transform.localScale = baseScale * pulse;
                yield return null;
            }
            transform.localScale = baseScale;

            _popping = false;
        }
    }
}
