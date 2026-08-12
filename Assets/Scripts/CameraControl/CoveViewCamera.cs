using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Rubrehose.Core;

namespace Rubrehose.CameraControl
{
    // Static-per-cove camera (CAMERA_AND_UI_SPEC.md "REVISED — single screen per cove,
    // bounded scroll"). Replaces the earlier continuous-strip WorldScrollCamera: no
    // drag-scroll within a cove — everything in a cove is visible at once. A bounded
    // left/right swipe pages between already-unlocked coves (max 3 per biome), animated
    // as a smooth pan rather than an instant cut. Also retains PanTo() so the fast-travel
    // ribbon can jump the camera to a biome, unchanged from before.
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

        // Fires once a pan finishes settling, per the spec's "Settled" definition —
        // argument is the biome index occupying the viewport.
        public event Action<int> OnSettled;

        private bool _tracking;
        private Vector2 _pointerDownScreenPos;
        private float _pointerDownTime;

        public int CurrentCoveIndex { get; private set; }

        private void Awake()
        {
            SetPositionX(0f);
        }

        private void Update() => HandleSwipe();

        // Pages to a specific cove within the current biome, clamped to already-unlocked
        // coves (GameManager.State.coveIndex is the frontier reached so far).
        public void GoToCove(int coveIndex)
        {
            coveIndex = Mathf.Clamp(coveIndex, 0, MaxNavigableCoveIndex());
            if (coveIndex == CurrentCoveIndex) return;
            CurrentCoveIndex = coveIndex;
            PanTo(coveIndex * coveScreenWidth, panDurationSeconds);
        }

        // Kept for the fast-travel ribbon's biome jump (CAMERA_AND_UI_SPEC.md fast-travel section).
        public void PanTo(float worldX, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(PanRoutine(worldX, duration));
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
            return Mathf.Clamp(GameManager.Instance.State.coveIndex, 0, 2);
        }

        private void SetPositionX(float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        private IEnumerator PanRoutine(float targetX, float duration)
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
            OnSettled?.Invoke(SettledBiomeIndex());
        }

        // Phase 1: Wreck Beach is the only biome built, so this always resolves to 0.
        // Extend once a second biome's terrain/scene actually exists to jump into.
        private int SettledBiomeIndex() => 0;
    }
}
