using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.CameraControl;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Fast-travel handle/ribbon (CAMERA_AND_UI_SPEC.md "Fast-travel ribbon"). Screen-anchored
    // bottom-left, independent of world scroll. Collapsed = small handle showing the
    // currently-settled cove; expanded = a ribbon of every unlocked cove, current one
    // highlighted at its true sequence position, tapping one pans the world camera there.
    //
    // Cove-based rather than biome-based (CORE_PROGRESSION_RESTRUCTURE.md: "The fast-travel
    // ribbon's mechanical design — still valid, just now only ever needs to hold up to 4
    // slots total instead of scaling toward 18"). Panning goes through CoveViewCamera.GoToCove
    // rather than a separately-maintained per-slot world-X array, so this stays in sync with
    // CoveViewCamera's own notion of "current cove" (and therefore with Tuggy's travel
    // direction detection, which reads the same OnSettled event) automatically.
    public class FastTravelRibbonController : MonoBehaviour
    {
        [Header("Collapsed handle")]
        [SerializeField] private GameObject collapsedHandle;
        [SerializeField] private Image collapsedThumbnail;
        [SerializeField] private TMP_Text collapsedLabel;
        [SerializeField] private Button collapsedHandleButton;

        [Header("Expanded ribbon")]
        [SerializeField] private GameObject expandedRibbon;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private FastTravelSlotUI slotPrefab;
        [SerializeField] private Button closeButton;

        [Header("World")]
        [SerializeField] private CoveViewCamera worldCamera;

        [Tooltip("Thumbnail sprite per cove, in WreckBeachData.CoveNames order.")]
        [SerializeField] private Sprite[] coveThumbnails = new Sprite[WreckBeachData.CoveNames.Length];

        private readonly List<FastTravelSlotUI> _slots = new List<FastTravelSlotUI>();
        private int _settledCoveIndex;

        private void OnEnable()
        {
            worldCamera.OnSettled += HandleSettled;
            collapsedHandleButton.onClick.AddListener(Expand);
            closeButton.onClick.AddListener(Collapse);

            _settledCoveIndex = worldCamera.CurrentCoveIndex;
            Collapse();
            RefreshCollapsedHandle();
        }

        private void OnDisable()
        {
            if (worldCamera != null) worldCamera.OnSettled -= HandleSettled;
        }

        private void HandleSettled(int previousCoveIndex, int newCoveIndex)
        {
            _settledCoveIndex = newCoveIndex;
            RefreshCollapsedHandle();
        }

        private void RefreshCollapsedHandle()
        {
            collapsedLabel.text = WreckBeachData.CoveNames[_settledCoveIndex];
            if (_settledCoveIndex < coveThumbnails.Length && coveThumbnails[_settledCoveIndex] != null)
                collapsedThumbnail.sprite = coveThumbnails[_settledCoveIndex];
        }

        public void Expand()
        {
            BuildSlots();
            collapsedHandle.SetActive(false);
            expandedRibbon.SetActive(true);
        }

        public void Collapse()
        {
            expandedRibbon.SetActive(false);
            collapsedHandle.SetActive(true);
        }

        private void BuildSlots()
        {
            foreach (Transform child in slotContainer) Destroy(child.gameObject);
            _slots.Clear();

            int unlockedCount = Mathf.Clamp(GameManager.Instance.State.coveIndex + 1, 1, WreckBeachData.CoveNames.Length);
            for (int i = 0; i < unlockedCount; i++)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                var sprite = i < coveThumbnails.Length ? coveThumbnails[i] : null;
                slot.Bind(i, WreckBeachData.CoveNames[i], sprite, i == _settledCoveIndex, FastTravelTo);
                _slots.Add(slot);
            }
        }

        private void FastTravelTo(int coveIndex)
        {
            Collapse();
            worldCamera.GoToCove(coveIndex);
        }
    }
}
