using System.Collections;
using UnityEngine;
using Rubrehose.CameraControl;

namespace Rubrehose.World
{
    // Tuggy's cove-to-cove travel animation (CORE_PROGRESSION_RESTRUCTURE.md "Tuggy's travel
    // animation"): whenever CoveViewCamera settles on a new cove, Tuggy cruises in from
    // whichever screen edge matches the direction of travel (previousCoveIndex < newCoveIndex
    // -> from the left, previousCoveIndex > newCoveIndex -> from the right) and lands at his
    // resting spot, rather than simply being wherever he already was.
    //
    // Positions itself via viewport math (Camera.main.ViewportToWorldPoint) every time it
    // moves, instead of depending on being parented under the camera or under a specific
    // cove's hierarchy — so it works no matter where Tuggy's GameObject actually lives in the
    // scene, without needing to touch Landing Cove's existing build to wire this up.
    //
    // Art status per the doc: placeholder/static for now — cruiseFrames is empty until Chad
    // supplies real cruising sprites, in which case only the position tween plays (still a
    // real "cruise in" motion, just without a frame-swap animation on top). Sequence length is
    // never assumed; whatever's assigned in the Inspector plays back, looped, for the
    // duration of the cruise.
    [RequireComponent(typeof(SpriteRenderer))]
    public class TuggyTravelController : MonoBehaviour
    {
        [SerializeField] private CoveViewCamera coveCamera;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Paused while cruising (both would otherwise fight over this object's position) and resumed once Tuggy settles at rest. Optional.")]
        [SerializeField] private PixelSnappedBob idleBob;

        [Header("Resting position (viewport space, 0-1 of the screen) — bottom-left by default")]
        [SerializeField] private Vector2 restingViewport = new Vector2(0.16f, 0.12f);

        [Header("Cruise-in")]
        [Tooltip("Placeholder-safe: any length, including empty (the position tween still plays either way, just with no frame-swap on top).")]
        [SerializeField] private Sprite[] cruiseFrames;
        [SerializeField] private float cruiseFramesPerSecond = 8f;
        [SerializeField] private float cruiseDurationSeconds = 1f;
        [Tooltip("How far past the screen edge Tuggy starts from, in viewport-width units (1 = one full screen width off-screen).")]
        [SerializeField] private float offscreenViewportMargin = 0.25f;

        private Sprite _restSprite;
        private Coroutine _cruiseRoutine;
        private Coroutine _frameLoopRoutine;

        private void Awake()
        {
            if (spriteRenderer != null) _restSprite = spriteRenderer.sprite;
        }

        private void OnEnable()
        {
            if (coveCamera != null) coveCamera.OnSettled += HandleSettled;
            SnapToRest();
        }

        private void OnDisable()
        {
            if (coveCamera != null) coveCamera.OnSettled -= HandleSettled;
        }

        private void HandleSettled(int previousCoveIndex, int newCoveIndex)
        {
            if (previousCoveIndex == newCoveIndex) return; // CoveViewCamera already guards this, kept as a safe no-op here too
            bool enterFromLeft = previousCoveIndex < newCoveIndex;

            if (_cruiseRoutine != null) StopCoroutine(_cruiseRoutine);
            _cruiseRoutine = StartCoroutine(CruiseInRoutine(enterFromLeft));
        }

        private void SnapToRest()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            transform.position = ViewportPointAtCurrentDepth(cam, restingViewport);
        }

        // Preserves this object's own world Z (2D sprite depth/sorting is driven by
        // SpriteRenderer.sortingOrder, not Z, but keep it stable regardless of whichever way
        // the scene's camera happens to face) rather than assuming a fixed depth convention.
        private Vector3 ViewportPointAtCurrentDepth(Camera cam, Vector2 viewport)
        {
            float depth = transform.position.z - cam.transform.position.z;
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            world.z = transform.position.z;
            return world;
        }

        private IEnumerator CruiseInRoutine(bool enterFromLeft)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            if (idleBob != null) idleBob.enabled = false;

            float offscreenX = enterFromLeft ? -1f - offscreenViewportMargin : 1f + offscreenViewportMargin;
            Vector3 startWorld = ViewportPointAtCurrentDepth(cam, restingViewport + new Vector2(offscreenX, 0f));
            Vector3 restWorld = ViewportPointAtCurrentDepth(cam, restingViewport);

            transform.position = startWorld;
            FaceDirection(enterFromLeft);

            if (cruiseFrames != null && cruiseFrames.Length > 0)
                _frameLoopRoutine = StartCoroutine(FrameLoopRoutine());

            float t = 0f;
            while (t < cruiseDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / cruiseDurationSeconds);
                transform.position = Vector3.Lerp(startWorld, restWorld, k);
                yield return null;
            }
            transform.position = restWorld;

            if (_frameLoopRoutine != null)
            {
                StopCoroutine(_frameLoopRoutine);
                _frameLoopRoutine = null;
            }
            if (spriteRenderer != null && _restSprite != null) spriteRenderer.sprite = _restSprite;

            if (idleBob != null) idleBob.enabled = true; // re-captures its rest position on enable, see PixelSnappedBob
            _cruiseRoutine = null;
        }

        private IEnumerator FrameLoopRoutine()
        {
            var wait = new WaitForSeconds(1f / cruiseFramesPerSecond);
            int index = 0;
            while (true)
            {
                spriteRenderer.sprite = cruiseFrames[index];
                index = (index + 1) % cruiseFrames.Length;
                yield return wait;
            }
        }

        // Cruising left-to-right should face right, right-to-left should face left — flip via
        // localScale.x sign so a single sprite/frame-set serves both directions without needing
        // mirrored art (placeholder-safe; real per-direction frame sets can replace this later).
        private void FaceDirection(bool enterFromLeft)
        {
            Vector3 scale = transform.localScale;
            scale.x = enterFromLeft ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
