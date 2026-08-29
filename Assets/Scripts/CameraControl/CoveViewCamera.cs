using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.CameraControl
{
    // Static-per-cove camera (CAMERA_AND_UI_SPEC.md "REVISED — single screen per cove,
    // bounded scroll"). Replaces the earlier continuous-strip WorldScrollCamera: no
    // drag-scroll within a cove — everything in a cove is visible at once. A bounded
    // left/right swipe pages between already-unlocked coves (now up to all 4 of Wreck
    // Beach's, CORE_PROGRESSION_RESTRUCTURE.md — not just 3 within a biome), animated as a
    // smooth pan rather than an instant cut. Also retains PanTo() so the fast-travel ribbon
    // can jump the camera to a cove, unchanged from before.
    [RequireComponent(typeof(Camera))]
    public class CoveViewCamera : MonoBehaviour
    {
        [Tooltip("World-unit width of one cove 'page' — must match the camera's actual visible width " +
                 "(2 x orthographic size x reference aspect) so coves tile edge-to-edge with no gap or overlap. " +
                 "Default assumes a 1080x1920 reference aspect at orthographic size 5.")]
        [SerializeField] private float coveScreenWidth = 5.625f;

        [SerializeField] private float swipeThresholdPixels = 80f;
        [SerializeField] private float maxSwipeDurationSeconds = 0.6f;
        [SerializeField] private float panDurationSeconds = 0.45f;

        // Fires once a pan finishes settling on a DIFFERENT cove than it started from, per the
        // spec's "Settled" definition — arguments are (previousCoveIndex, newCoveIndex), so
        // listeners (TuggyTravelController) can derive travel direction without tracking cove
        // history themselves. Never fires for a same-cove PanTo (e.g. a re-tap of the already-
        // current fast-travel slot).
        public event Action<int, int> OnSettled;

        private bool _tracking;
        private Vector2 _pointerDownScreenPos;
        private float _pointerDownTime;

        public int CurrentCoveIndex { get; private set; }

        private void Awake()
        {
            SetPositionX(0f);
        }

        // Auto-pan-reveal on cove unlock (CORE_PROGRESSION_RESTRUCTURE.md "New unlock moment":
        // "the instant a cove unlocks, the camera pans across the newly revealed cove... not an
        // instant cut or silent unlock"). Reuses GoToCove/OnSettled exactly as a swipe would —
        // this just calls it in response to GameManager's event instead of a finger release.
        // GameManager.Awake runs first (DefaultExecutionOrder(-1000)), so Instance is safe to
        // read here.
        private void OnEnable() => GameManager.Instance.OnCoveUnlocked += HandleCoveUnlocked;
        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnCoveUnlocked -= HandleCoveUnlocked;
        }

        private void HandleCoveUnlocked(int previousCoveIndex, int newCoveIndex) => GoToCove(newCoveIndex);

        private void Update() => HandleSwipe();

        // Pages to a specific cove among all 4 of Wreck Beach's, clamped to already-unlocked
        // coves (GameManager.State.coveIndex is the frontier reached so far).
        public void GoToCove(int coveIndex)
        {
            coveIndex = Mathf.Clamp(coveIndex, 0, MaxNavigableCoveIndex());
            if (coveIndex == CurrentCoveIndex) return;
            int previous = CurrentCoveIndex;
            CurrentCoveIndex = coveIndex;
            StopAllCoroutines();
            StartCoroutine(PanRoutine(coveIndex * coveScreenWidth, panDurationSeconds, previous, coveIndex));
        }

        // Raw camera pan to an arbitrary world X — doesn't touch CurrentCoveIndex or fire
        // OnSettled (there's no "new cove" to report), so callers that DO mean "go to this
        // cove" (the fast-travel ribbon included) should call GoToCove instead.
        public void PanTo(float worldX, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(PanRoutine(worldX, duration, CurrentCoveIndex, CurrentCoveIndex));
        }

        private void HandleSwipe()
        {
            ReadPointer(out bool down, out bool up, out Vector2 screenPos);

            if (down)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    _tracking = false;
                    return;
                }
                _tracking = true;
                _pointerDownScreenPos = screenPos;
                _pointerDownTime = Time.unscaledTime;
            }
            else if (up && _tracking)
            {
                _tracking = false;
                Vector2 delta = screenPos - _pointerDownScreenPos;
                float elapsed = Time.unscaledTime - _pointerDownTime;
                bool isSwipe = elapsed <= maxSwipeDurationSeconds
                               && Mathf.Abs(delta.x) >= swipeThresholdPixels
                               && Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
                if (isSwipe) GoToCove(CurrentCoveIndex + (delta.x < 0 ? 1 : -1));
            }
        }

        private static void ReadPointer(out bool down, out bool up, out Vector2 screenPos)
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                screenPos = t.position;
                return;
            }
            down = Input.GetMouseButtonDown(0);
            up = Input.GetMouseButtonUp(0);
            screenPos = Input.mousePosition;
        }

        private int MaxNavigableCoveIndex()
        {
            if (GameManager.Instance == null) return 0;
            return Mathf.Clamp(GameManager.Instance.State.coveIndex, 0, WreckBeachData.CoveNames.Length - 1);
        }

        private void SetPositionX(float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        private IEnumerator PanRoutine(float targetX, float duration, int previousCoveIndex, int newCoveIndex)
        {
            float startX = transform.position.x;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, t / duration);
                SetPositionX(Mathf.Lerp(startX, targetX, k));
                yield return null;
            }
            SetPositionX(targetX);
            if (previousCoveIndex != newCoveIndex) OnSettled?.Invoke(previousCoveIndex, newCoveIndex);
        }
    }
}
