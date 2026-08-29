using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // One row in the Artifacts panel's tree-node list (Menu -> Artifacts, NEXT_CLAUDE_CODE_PUSH.md
    // §1b) — a permanent upgrade node gated to a serpentLevel milestone and costing appraised
    // Shards of one rarity tier.
    public class ArtifactNodeUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button purchaseButton;

        private string _nodeId;

        public void Bind(string nodeId)
        {
            _nodeId = nodeId;
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => GameManager.Instance.PurchaseArtifactNode(_nodeId));
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            var def = ArtifactNodeCatalog.Find(_nodeId);
            if (def == null) return;

            nameText.text = $"{def.displayName} (+{def.tapPowerBonusPercent:P0} tap power)";

            if (gm.IsArtifactNodePurchased(_nodeId))
            {
                statusText.text = "Recovered.";
                purchaseButton.gameObject.SetActive(false);
                return;
            }

            purchaseButton.gameObject.SetActive(true);
            if (!gm.IsArtifactNodeUnlocked(_nodeId))
            {
                statusText.text = $"Reach serpent level {def.requiredSerpentLevel} to unlock.";
                purchaseButton.interactable = false;
                return;
            }

            statusText.text = $"{def.shardCost} {RarityTier.DisplayName(def.shardTier)} Shards";
            purchaseButton.interactable = gm.CanPurchaseArtifactNode(_nodeId);
        }
    }
}
