using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rubrehose.UI
{
    // One thumbnail in the expanded fast-travel ribbon. Save as a prefab in Assets/Prefabs
    // once built (same pattern as CrewListItemUI).
    public class FastTravelSlotUI : MonoBehaviour
    {
        [SerializeField] private Image thumbnail;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject highlightRing;
        [SerializeField] private Button button;

        [Tooltip("Small badge/glow shown when this cove has a live Artifacts tell the player isn't currently looking at (NEXT_CLAUDE_CODE_PUSH.md §1a).")]
        [SerializeField] private GameObject liveTellBadge;

        private int _coveIndex;
        private Action<int> _onSelected;

        public void Bind(int coveIndex, string coveName, Sprite thumbnailSprite, bool isCurrent, bool hasLiveTell, Action<int> onSelected)
        {
            _coveIndex = coveIndex;
            _onSelected = onSelected;

            label.text = coveName;
            if (thumbnailSprite != null) thumbnail.sprite = thumbnailSprite;
            highlightRing.SetActive(isCurrent);
            SetLiveTellBadge(hasLiveTell);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_coveIndex));
        }

        // Called independently of Bind so the ribbon can refresh badges live while expanded,
        // without rebuilding the whole slot list.
        public void SetLiveTellBadge(bool hasLiveTell)
        {
            if (liveTellBadge != null) liveTellBadge.SetActive(hasLiveTell);
        }
    }
}
