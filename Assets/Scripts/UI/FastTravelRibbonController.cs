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
    // currently-settled biome; expanded = a ribbon of every unlocked biome, current one
    // highlighted at its true sequence position, tapping one pans the world camera there.
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
        [SerializeField] private WorldScrollCamera worldCamera;
        [SerializeField] private float panDurationSeconds = 0.8f;

        [Tooltip("World-space X to pan the camera to for each biome, in BiomeCatalog order. Fill in as each biome's terrain is built.")]
        [SerializeField] private float[] biomeWorldX = new float[BiomeCatalog.Names.Length];

        [Tooltip("Thumbnail sprite per biome, in BiomeCatalog order.")]
        [SerializeField] private Sprite[] biomeThumbnails = new Sprite[BiomeCatalog.Names.Length];

        private readonly List<FastTravelSlotUI> _slots = new List<FastTravelSlotUI>();
        private int _settledBiomeIndex;

        private void OnEnable()
        {
            worldCamera.OnSettled += HandleSettled;
            collapsedHandleButton.onClick.AddListener(Expand);
            closeButton.onClick.AddListener(Collapse);

            _settledBiomeIndex = GameManager.Instance.State.biomeUnlocked;
            Collapse();
            RefreshCollapsedHandle();
        }

        private void OnDisable()
        {
            if (worldCamera != null) worldCamera.OnSettled -= HandleSettled;
        }

        private void HandleSettled(int biomeIndex)
        {
            _settledBiomeIndex = biomeIndex;
            RefreshCollapsedHandle();
        }

        private void RefreshCollapsedHandle()
        {
            collapsedLabel.text = BiomeCatalog.Names[_settledBiomeIndex];
            if (_settledBiomeIndex < biomeThumbnails.Length && biomeThumbnails[_settledBiomeIndex] != null)
                collapsedThumbnail.sprite = biomeThumbnails[_settledBiomeIndex];
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

            int unlockedCount = Mathf.Clamp(GameManager.Instance.State.biomeUnlocked + 1, 1, BiomeCatalog.Names.Length);
            for (int i = 0; i < unlockedCount; i++)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                var sprite = i < biomeThumbnails.Length ? biomeThumbnails[i] : null;
                slot.Bind(i, BiomeCatalog.Names[i], sprite, i == _settledBiomeIndex, FastTravelTo);
                _slots.Add(slot);
            }
        }

        private void FastTravelTo(int biomeIndex)
        {
            if (biomeIndex < 0 || biomeIndex >= biomeWorldX.Length) return;
            Collapse();
            worldCamera.PanTo(biomeWorldX[biomeIndex], panDurationSeconds);
        }
    }
}
