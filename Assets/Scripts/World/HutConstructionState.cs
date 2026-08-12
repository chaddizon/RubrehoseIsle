using UnityEngine;

namespace Rubrehose.World
{
    // Hut's 3 build states (rubble/half-built/complete — HUD_AND_LANDING_COVE_LAYOUT.md
    // §B Camp cluster). Nothing calls SetState yet — wire it once the construction-reveal
    // mechanic (§B2) is implemented; for now it's a placeholder sprite swapper.
    public class HutConstructionState : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite rubbleSprite;
        [SerializeField] private Sprite halfBuiltSprite;
        [SerializeField] private Sprite completeSprite;

        public void SetState(int state)
        {
            spriteRenderer.sprite = state switch
            {
                0 => rubbleSprite,
                1 => halfBuiltSprite,
                _ => completeSprite,
            };
        }
    }
}
