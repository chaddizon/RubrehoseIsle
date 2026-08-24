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

        // hut_stage2_halfbuilt's content sits ~8px higher in its canvas than
        // hut_stage3_complete's (taller roof peak from exposed rafter framing) — without
        // this, the roofline would visually shift/shrink downward the moment construction
        // finishes. World units at the project's 100px/unit import convention: 8px/100 = 0.08.
        [SerializeField] private Vector2 halfBuiltVisualOffset = new Vector2(0f, -0.08f);

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
