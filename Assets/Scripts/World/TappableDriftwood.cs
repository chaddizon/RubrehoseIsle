using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Attach to a driftwood piece's Collider2D. Tapping it adds driftwood via the existing
    // tap-power formula — the Shoreline cluster's tappable driftwood
    // (HUD_AND_LANDING_COVE_LAYOUT.md §B), same math as the old abstract tap button.
    [RequireComponent(typeof(Collider2D))]
    public class TappableDriftwood : MonoBehaviour
    {
        private void OnMouseDown()
        {
            GameManager.Instance.Tap();
        }
    }
}
