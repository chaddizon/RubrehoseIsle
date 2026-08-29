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

        private int _coveIndex;
        private Action<int> _onSelected;

        public void Bind(int coveIndex, string coveName, Sprite thumbnailSprite, bool isCurrent, Action<int> onSelected)
        {
            _coveIndex = coveIndex;
            _onSelected = onSelected;

            label.text = coveName;
            if (thumbnailSprite != null) thumbnail.sprite = thumbnailSprite;
            highlightRing.SetActive(isCurrent);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_coveIndex));
        }
    }
}
