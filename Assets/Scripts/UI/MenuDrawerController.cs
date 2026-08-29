using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Comic-panel-style drawer (HUD_AND_LANDING_COVE_LAYOUT.md §C), slides in from the
    // right edge. Crew, Upgrades, and Buildings rows open real content panels; Captain's
    // Log/Milestones/Settings/Artifacts still just log until those systems exist. Buildings
    // (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings") is the only way to pay a building's
    // first stage — nothing exists in-world to tap before then (CoveBuildingVisual). All
    // click wiring happens here in OnEnable via serialized references, rather than the
    // builder tool calling AddListener at edit time — edit-time listeners are non-persistent
    // and are silently lost on the next domain reload / Play session.
    public class MenuDrawerController : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float openDurationSeconds = 0.3f;
        [SerializeField] private float closedX = 380f; // off-screen, right of the panel's own width
        [SerializeField] private float openX = 0f;

        [Header("Click targets")]
        [SerializeField] private Button catcherButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button crewRowButton;
        [SerializeField] private Button upgradesRowButton;
        [SerializeField] private Button buildingsRowButton;
        [SerializeField] private Button captainsLogRowButton;
        [SerializeField] private Button milestonesRowButton;
        [SerializeField] private Button settingsRowButton;
        [SerializeField] private Button artifactsRowButton;
        [SerializeField] private Button contentBackButton;

        [Header("Row list vs content panels")]
        [SerializeField] private GameObject rowList;
        [SerializeField] private GameObject crewPanel;
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject buildingsPanel;

        [Header("Conditionally-visible rows")]
        [SerializeField] private GameObject captainsLogRow; // shown once GameManager.State.reachedEndlessCove
        [SerializeField] private GameObject artifactsRow;

        [Tooltip("Placeholder — wire to real prestige-count state once Prestige exists. Artifacts row " +
                 "stays hidden entirely (not shown-but-locked) until this is true.")]
        [SerializeField] private bool artifactsUnlocked;

        private bool _open;
        private GameObject _activeContentPanel;
        private Coroutine _animCoroutine;

        // The MenuDrawer root never gets SetActive(false) (only panel.anchoredPosition
        // animates for open/close), so this fires exactly once — no double-wiring guard
        // needed here, unlike panels that toggle active.
        private void OnEnable()
        {
            catcherButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            contentBackButton.onClick.AddListener(ShowRowList);
            crewRowButton.onClick.AddListener(() => OnEntryTapped("Crew"));
            upgradesRowButton.onClick.AddListener(() => OnEntryTapped("Upgrades"));
            buildingsRowButton.onClick.AddListener(() => OnEntryTapped("Buildings"));
            captainsLogRowButton.onClick.AddListener(() => OnEntryTapped("Captain's Log"));
            milestonesRowButton.onClick.AddListener(() => OnEntryTapped("Milestones"));
            settingsRowButton.onClick.AddListener(() => OnEntryTapped("Settings"));
            artifactsRowButton.onClick.AddListener(() => OnEntryTapped("Artifacts"));

            RefreshConditionalRows();
            ShowRowList();
            SetImmediate(false);
        }

        public void Toggle()
        {
            if (_open) Close();
            else Open();
        }

        public void Open()
        {
            RefreshConditionalRows();
            AnimateTo(true);
        }

        public void Close()
        {
            AnimateTo(false);
            ShowRowList();
        }

        private void RefreshConditionalRows()
        {
            if (captainsLogRow != null) captainsLogRow.SetActive(GameManager.Instance.State.reachedEndlessCove);
            if (artifactsRow != null) artifactsRow.SetActive(artifactsUnlocked);
        }

        private void AnimateTo(bool open)
        {
            _open = open;
            catcherButton.gameObject.SetActive(open);
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateRoutine(open));
        }

        private void SetImmediate(bool open)
        {
            _open = open;
            catcherButton.gameObject.SetActive(open);
            var pos = panel.anchoredPosition;
            pos.x = open ? openX : closedX;
            panel.anchoredPosition = pos;
        }

        private IEnumerator AnimateRoutine(bool open)
        {
            float startX = panel.anchoredPosition.x;
            float targetX = open ? openX : closedX;
            float t = 0f;
            while (t < openDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / openDurationSeconds);
                var pos = panel.anchoredPosition;
                pos.x = Mathf.Lerp(startX, targetX, k);
                panel.anchoredPosition = pos;
                yield return null;
            }
            var final = panel.anchoredPosition;
            final.x = targetX;
            panel.anchoredPosition = final;
        }

        private void OnEntryTapped(string entryName)
        {
            GameObject content = entryName switch
            {
                "Crew" => crewPanel,
                "Upgrades" => upgradesPanel,
                "Buildings" => buildingsPanel,
                _ => null,
            };

            if (content != null) ShowContent(content);
            else Debug.Log("MenuDrawerController: '" + entryName + "' tapped — no panel content yet.");
        }

        private void ShowContent(GameObject content)
        {
            _activeContentPanel = content;
            rowList.SetActive(false);
            content.SetActive(true);
            contentBackButton.gameObject.SetActive(true);
        }

        private void ShowRowList()
        {
            if (_activeContentPanel != null) _activeContentPanel.SetActive(false);
            _activeContentPanel = null;
            rowList.SetActive(true);
            contentBackButton.gameObject.SetActive(false);
        }
    }
}
