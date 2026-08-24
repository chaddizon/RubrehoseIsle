using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Wires a cove's Hut (HUD_AND_LANDING_COVE_LAYOUT.md Camp cluster) to the mini-boss ->
    // construction-reveal flow (CAMERA_AND_UI_SPEC.md "Cove-gate structure"): rubble until
    // this cove's mini-boss falls, half-built once the crossing is revealed, complete once
    // it's actually paid for. Tap the hut to pay, once affordable.
    [RequireComponent(typeof(Collider2D))]
    public class ConstructionGate : MonoBehaviour
    {
        [SerializeField] private int coveIndex;
        [SerializeField] private HutConstructionState hutState;

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void OnMouseDown()
        {
            var gm = GameManager.Instance;
            if (gm.State.coveIndex == coveIndex) gm.BuildConstruction();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            bool built = gm.IsCoveConstructionComplete(coveIndex);
            bool revealed = coveIndex < gm.State.coveConstructionRevealed.Length && gm.State.coveConstructionRevealed[coveIndex];
            hutState.SetState(built ? 2 : revealed ? 1 : 0);
        }
    }
}
