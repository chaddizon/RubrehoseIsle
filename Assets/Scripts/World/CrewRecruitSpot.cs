using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // In-world locked recruit spot (HUD_AND_LANDING_COVE_LAYOUT.md §E: "Crew recruit/manage
    // | In-world locked spots to recruit"). Tapping recruits the given crew member via the
    // existing GameManager API — the Camp cluster's BBW/BBC home spots.
    [RequireComponent(typeof(Collider2D))]
    public class CrewRecruitSpot : MonoBehaviour
    {
        [SerializeField] private string crewId;

        private void OnMouseDown()
        {
            GameManager.Instance.RecruitCrew(crewId);
        }
    }
}
