using UnityEngine;

namespace Rubrehose.World
{
    // Gentle vertical bob for a single sprite sitting on/in water (Tuggy, the cast bottle
    // once washed up) — purely cosmetic, no gameplay state of its own. Toggle via
    // MonoBehaviour.enabled (Unity skips Update() while disabled) rather than
    // GameObject.SetActive() when a caller needs to start/stop it conditionally, so any
    // sibling script driving that condition can keep running on the same object.
    //
    // Pixel-snapped every frame: pixel art shimmers/blurs under smooth sub-pixel movement
    // even with Point filtering applied, since the sprite would sit between texel boundaries
    // for most of the cycle. The raw sine offset is converted to whole pixels (at the
    // project's 100px/unit import convention), rounded, and converted back — never left as a
    // continuous fractional world-unit value.
    public class PixelSnappedBob : MonoBehaviour
    {
        [SerializeField] private float cycleSeconds = 2.5f; // 2-3s full cycle
        [SerializeField] private float amplitudePixels = 3f; // 2-4px max vertical movement
        [SerializeField] private float pixelsPerUnit = 100f; // matches this project's texture import convention

        private Vector3 _restLocalPosition;

        // Captured on every enable, not just Awake — so re-enabling after something else
        // (e.g. TuggyTravelController) has moved this object to a new base position bobs
        // around THAT position, not a stale one from whenever this component first woke up.
        private void OnEnable()
        {
            _restLocalPosition = transform.localPosition;
        }

        private void OnDisable()
        {
            transform.localPosition = _restLocalPosition;
        }

        private void Update()
        {
            float amplitudeUnits = amplitudePixels / pixelsPerUnit;
            float rawOffsetUnits = Mathf.Sin(Time.time / cycleSeconds * Mathf.PI * 2f) * amplitudeUnits;

            float snappedOffsetPixels = Mathf.Round(rawOffsetUnits * pixelsPerUnit);
            float snappedOffsetUnits = snappedOffsetPixels / pixelsPerUnit;

            transform.localPosition = _restLocalPosition + new Vector3(0f, snappedOffsetUnits, 0f);
        }
    }
}
