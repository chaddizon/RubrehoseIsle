using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // One row in the crew list (BBW / BBC). Instantiated by MainHUDController per
    // CrewCatalog.WreckBeachCrew entry — save this GameObject as a prefab in Assets/Prefabs.
    public class CrewListItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rateText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button recruitButton;

        private string _crewId;

        public void Bind(string crewId)
        {
            _crewId = crewId;
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() => GameManager.Instance.RecruitCrew(_crewId));
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            var def = gm.GetCrewDefinition(_crewId);
            var state = gm.GetCrewState(_crewId);
            if (def == null || state == null) return;

            nameText.text = $"{def.displayName} (Lv {state.level})";
            rateText.text = $"+{Format.Number(gm.CrewRate(state))} Driftwood/s";
            costText.text = $"Recruit — {Format.Number(state.currentCost)} Driftwood";
            recruitButton.interactable = gm.CanRecruit(_crewId);
        }
    }
}
