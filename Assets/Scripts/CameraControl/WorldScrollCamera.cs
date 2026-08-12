using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rubrehose.CameraControl
{
    // Horizontally-constrained 2D orthographic camera (CAMERA_AND_UI_SPEC.md "Camera / world
    // model"). Horizontal drag anywhere in the scene scrolls; taps on in-world objects still
    // work since a drag isn't confirmed until the pointer moves past dragThresholdPixels, and
    // drags starting over UI are ignored entirely.
    [RequireComponent(typeof(Camera))]
    public class WorldScrollCamera : MonoBehaviour
    {
        [Tooltip("Placeholder — replace once the Wreck Beach background art defines the strip's real world-unit width.")]
        [SerializeField] private float worldMinX = -2f;
        [SerializeField] private float worldMaxX = 2f;

        [SerializeField] private float dragThresholdPixels = 12f;
        [SerializeField] private float settleDelaySeconds = 0.15f;

        // Fires once scroll motion has stopped (or immediately after a fast-travel pan
        // completes), per the spec's "Settled" definition. Argument is the biome index
        // occupying the majority of the viewport.
        public event Action<int> OnSettled;

        private Camera _camera;
        private bool _dragging;
        private bool _dragConfirmed;
        private Vector3 _dragStartScreenPos;
        private float _camStartWorldX;
        private float _idleTimer;
        private bool _wasMoving;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ClampPosition();
        }

        private void Update()
        {
            HandlePointer();
            HandleSettleDetection();
        }

        // Call as later biomes unlock to extend the scrollable range.
        public void SetWorldBounds(float minX, float maxX)
        {
            worldMinX = minX;
            worldMaxX = maxX;
            ClampPosition();
        }

        public void PanTo(float worldX, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(PanRoutine(worldX, duration));
        }

        private void HandlePointer()
        {
            ReadPointer(out bool down, out bool held, out bool up, out Vector3 screenPos);

            if (down)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    _dragging = false;
                    return;
                }
                _dragging = true;
                _dragConfirmed = false;
                _dragStartScreenPos = screenPos;
                _camStartWorldX = transform.position.x;
            }
            else if (held && _dragging)
            {
                Vector3 delta = screenPos - _dragStartScreenPos;
                if (!_dragConfirmed)
                {
                    if (delta.magnitude < dragThresholdPixels) return;
                    _dragConfirmed = true;
                }

                float worldPerPixel = _camera.orthographicSize * 2f / Screen.height;
                SetPositionX(_camStartWorldX - delta.x * worldPerPixel);
            }
            else if (up)
            {
                _dragging = false;
            }
        }

        private static void ReadPointer(out bool down, out bool held, out bool up, out Vector3 screenPos)
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                screenPos = t.position;
                return;
            }
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            screenPos = Input.mousePosition;
        }

        private void SetPositionX(float x)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(x, worldMinX, worldMaxX);
            transform.position = pos;
        }

        private void ClampPosition() => SetPositionX(transform.position.x);

        private void HandleSettleDetection()
        {
            if (_dragging && _dragConfirmed)
            {
                _idleTimer = 0f;
                _wasMoving = true;
                return;
            }
            if (!_wasMoving) return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer < settleDelaySeconds) return;

            _wasMoving = false;
            OnSettled?.Invoke(SettledBiomeIndex());
        }

        // Phase 1: Wreck Beach is the only segment built, so this always resolves to 0.
        // Extend with real per-biome world-X segment boundaries as later biomes are added.
        private int SettledBiomeIndex() => 0;

        private IEnumerator PanRoutine(float targetX, float duration)
        {
            float startX = transform.position.x;
            float clampedTarget = Mathf.Clamp(targetX, worldMinX, worldMaxX);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, t / duration);
                SetPositionX(Mathf.Lerp(startX, clampedTarget, k));
                yield return null;
            }
            SetPositionX(clampedTarget);

            // Trigger a fresh settle check so the ribbon's collapsed thumbnail
            // updates immediately after a fast-travel completes, per the spec.
            _wasMoving = true;
            _idleTimer = 0f;
        }
    }
}
