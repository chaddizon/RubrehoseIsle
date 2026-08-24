using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Shoreline's tappable driftwood (HUD_AND_LANDING_COVE_LAYOUT.md §B), reskinned to wash
    // ashore instead of Idle Obelisk Miner's static mining wall — fits Rubrehose's "stranded
    // on an island" setting while keeping tap economy byte-for-byte identical to Obelisk's
    // always-available mining node: GameManager.Tap() fires the instant you tap, with no
    // cooldown gating it. The wash-up/collect loop below is purely cosmetic — the Collider2D
    // lives on THIS (static) root, never on the animated visual child, so a fast tapper is
    // never blocked mid-animation.
    //
    // spriteRenderer/variantSprites live on a separate child (wired by LandingCoveBuilder) so
    // the collect animation's scale/fade never touches the collider's transform.
    [RequireComponent(typeof(Collider2D))]
    public class DriftwoodPiece : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] variantSprites;

        [Header("Collect (on tap)")]
        [SerializeField] private float collectDurationSeconds = 0.35f;
        [SerializeField] private float collectFloatDistance = 0.4f;

        [Header("Wash back up (after collect)")]
        [SerializeField] private float washUpDurationSeconds = 0.35f;

        [Header("Idle (while sitting on shore)")]
        [SerializeField] private float idleBobAmplitude = 0.04f;
        [SerializeField] private float idleBobPeriodSeconds = 2.2f;

        private Vector3 _restLocalPosition;
        private Vector3 _restLocalScale;
        private Color _restColor;
        private Coroutine _activeRoutine;

        private void Awake()
        {
            _restLocalPosition = spriteRenderer.transform.localPosition;
            _restLocalScale = spriteRenderer.transform.localScale;
            _restColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            PickRandomVariant();
            _activeRoutine = StartCoroutine(IdleBobRoutine());
        }

        private void OnDisable()
        {
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        }

        // No debounce/cooldown here on purpose — every tap credits Tap() immediately,
        // regardless of where the visual loop currently is. See class comment.
        private void OnMouseDown()
        {
            GameManager.Instance.Tap();

            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(CollectAndWashUpRoutine());
        }

        private void PickRandomVariant()
        {
            if (variantSprites == null || variantSprites.Length == 0) return;
            spriteRenderer.sprite = variantSprites[Random.Range(0, variantSprites.Length)];
        }

        private IEnumerator CollectAndWashUpRoutine()
        {
            var visual = spriteRenderer.transform;
            Color fadedColor = _restColor;
            fadedColor.a = 0f;

            // Collect: float up, shrink, fade out.
            float t = 0f;
            while (t < collectDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / collectDurationSeconds);
                visual.localPosition = _restLocalPosition + Vector3.up * (collectFloatDistance * k);
                visual.localScale = Vector3.Lerp(_restLocalScale, Vector3.zero, k);
                spriteRenderer.color = Color.Lerp(_restColor, fadedColor, k);
                yield return null;
            }

            // A different piece washes up in its place — reset transform, pick new art, still invisible.
            PickRandomVariant();
            visual.localPosition = _restLocalPosition;
            visual.localScale = Vector3.zero;
            spriteRenderer.color = fadedColor;

            // Wash up: scale in, fade in.
            t = 0f;
            while (t < washUpDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / washUpDurationSeconds);
                visual.localScale = Vector3.Lerp(Vector3.zero, _restLocalScale, k);
                spriteRenderer.color = Color.Lerp(fadedColor, _restColor, k);
                yield return null;
            }
            visual.localScale = _restLocalScale;
            spriteRenderer.color = _restColor;

            _activeRoutine = StartCoroutine(IdleBobRoutine());
        }

        private IEnumerator IdleBobRoutine()
        {
            var visual = spriteRenderer.transform;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                float offset = Mathf.Sin(t / idleBobPeriodSeconds * Mathf.PI * 2f) * idleBobAmplitude;
                visual.localPosition = _restLocalPosition + Vector3.up * offset;
                yield return null;
            }
        }
    }
}
