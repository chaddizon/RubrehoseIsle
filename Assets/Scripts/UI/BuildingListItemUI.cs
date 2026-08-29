using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // One row in the Buildings list (Menu -> Buildings, CORE_PROGRESSION_RESTRUCTURE.md
    // "Cove Buildings"). Same shape as CrewListItemUI, but for CoveBuildingCatalog entries —
    // this is the ONLY way to pay a building's Stage 1 (CoveBuildingVisual's in-world tap
    // only works once Stage 1 already exists).
    public class BuildingListItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button payButton;

        private string _buildingId;

        public void Bind(string buildingId)
        {
            _buildingId = buildingId;
            payButton.onClick.RemoveAllListeners();
            payButton.onClick.AddListener(() => GameManager.Instance.PayNextBuildingStage(_buildingId));
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            var def = CoveBuildingCatalog.Find(_buildingId);
            if (def == null) return;

            int stage = gm.GetBuildingStage(_buildingId);
            nameText.text = $"{def.displayName} (Stage {stage}/{def.stages.Length})";

            if (stage >= def.stages.Length)
            {
                statusText.text = "Fully built.";
                payButton.gameObject.SetActive(false);
                return;
            }

            payButton.gameObject.SetActive(true);
            var next = def.stages[stage];
            string reward = next.cosmeticRewardLabel != null
                ? $"+{next.tapPowerBonusPercent:P0} tap power + {next.cosmeticRewardLabel}"
                : $"+{next.tapPowerBonusPercent:P0} tap power";
            statusText.text = $"Stage {stage + 1} — {Format.Number(next.cost)} Driftwood — {reward}";
            payButton.interactable = gm.CanAffordNextBuildingStage(_buildingId);
        }
    }
}
