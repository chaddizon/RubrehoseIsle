using UnityEngine;
using Rubrehose.Combat;

namespace Rubrehose.World
{
    // Frontier cluster's mini-boss trigger point (HUD_AND_LANDING_COVE_LAYOUT.md §B/§B2).
    // Opens the existing fight modal. FightController's underlying clear-counting logic
    // still needs reworking to the real mini-boss model — unlimited attempts, no
    // clear-counter, defeat sets PlayerState.coveMinibossDefeated[coveIndex] — that rework
    // isn't done here, this only wires the new trigger point to what already exists.
    [RequireComponent(typeof(Collider2D))]
    public class MiniBossTrigger : MonoBehaviour
    {
        [SerializeField] private FightController fightController;

        private void OnMouseDown()
        {
            if (fightController != null) fightController.OpenFight();
        }
    }
}
