using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // One row in the Artifacts panel's Shard list (Menu -> Artifacts, NEXT_CLAUDE_CODE_PUSH.md
    // §1b) — browse found/unappraised Shards of one rarity tier, appraise them into spendable
    // currency for that tier's Artifact tree nodes below.
    public class ShardStackItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countsText;
        [SerializeField] private Button appraiseButton;

        private string _tier;

        public void Bind(string tier)
        {
            _tier = tier;
            appraiseButton.onClick.RemoveAllListeners();
            appraiseButton.onClick.AddListener(() => GameManager.Instance.AppraiseShard(_tier));
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            nameText.text = RarityTier.DisplayName(_tier);
            int unappraised = gm.GetUnappraisedShardCount(_tier);
            int appraised = gm.GetAppraisedShardCount(_tier);
            countsText.text = $"{unappraised} found, unappraised · {appraised} appraised";
            appraiseButton.interactable = unappraised > 0;
        }
    }
}
