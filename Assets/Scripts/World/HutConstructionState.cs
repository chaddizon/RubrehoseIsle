using UnityEngine;

namespace Rubrehose.World
{
    // Hut's 3 build states (rubble/half-built/complete — HUD_AND_LANDING_COVE_LAYOUT.md
    // §B Camp cluster). Driven by ConstructionGate, which maps the mini-boss/construction-
    // reveal flow (§B2) onto these three states. spriteRenderer lives on a separate "Visual"
    // child (wired by LandingCoveBuilder) rather than the Hut root that holds the
    // Collider2D — halfBuiltVisualOffset below only ever nudges the visual, never the hitbox.
    public class HutConstructionState : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite rubbleSprite;
        [SerializeField] private Sprite halfBuiltSprite;
        [SerializeField] private Sprite completeSprite;

        // stage2hut's content (127px tall) sits below stage3hut's (144px, edge-to-edge) —
        // the structure grows naturally taller through the sequence, so no compensating
        // offset is needed with this art set (unlike the previous hut_stage2_halfbuilt set,
        // which sat ~8px higher than its complete stage and needed one).
        [SerializeField] private Vector2 halfBuiltVisualOffset = Vector2.zero;

        private Vector3 _restLocalPosition;

        private void Awake()
        {
            _restLocalPosition = spriteRenderer.transform.localPosition;
        }

        public void SetState(int state)
        {
            spriteRenderer.sprite = state switch
            {
                0 => rubbleSprite,
                1 => halfBuiltSprite,
                _ => completeSprite,
            };

            Vector3 offset = state == 1 ? (Vector3)halfBuiltVisualOffset : Vector3.zero;
            spriteRenderer.transform.localPosition = _restLocalPosition + offset;
        }
    }
}
