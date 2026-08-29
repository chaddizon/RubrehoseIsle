using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Comic-panel-style drawer (HUD_AND_LANDING_COVE_LAYOUT.md §C), slides in from the right
    // edge. Rebuilt to be row-list-driven (NEXT_CLAUDE_CODE_PUSH.md §3: "this may mean
    // rebuilding the current Menu Drawer, not just adding rows to it") rather than one
    // hardcoded Button+GameObject field pair per row — every system in the game now needs a
    // reachable entry point here, even a stub, and a fixed field per row didn't scale past a
    // handful.
    //
    // Real panels: Crew, Upgrades, Buildings, Artifacts (conditionally hidden until first
    // Compass Shard is found), Stats. Everything else (Message in a Bottle/Cast a Net,
    // Captain's Log, Milestones, Tidepooling, Foraging, Postcards, Companions) is a "Coming
    // soon" stub panel built by the same MenuDrawerBuilder.BuildStubPanel helper — per the
    // doc's "don't invent a second panel style for stubs" rule, every panel (real or stub)
    // shares the same title-then-content structure. Settings is the one partial exception: a
    // genuinely-wired sound toggle plus a deliberately-inert Reset Save button (see
    // SettingsMenuPanel.cs).
    public class MenuDrawerController : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public string id;
            public Button button;
            public GameObject panel;
        }

        [SerializeField] private RectTransform panel;
        [SerializeField] private float openDurationSeconds = 0.3f;
        [SerializeField] private float closedX = 380f; // off-screen, right of the panel's own width
        [SerializeField] private float openX = 0f;

        [Header("Click targets")]
        [SerializeField] private Button catcherButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button contentBackButton;

        [Header("Row list vs content panels")]
        [SerializeField] private GameObject rowList;
        [SerializeField] private List<Row> rows = new List<Row>();

        [Header("Conditionally-visible rows (the row's own GameObject, not a Row list entry)")]
        [SerializeField] private GameObject captainsLogRow; // shown once GameManager.State.reachedEndlessCove
        [SerializeField] private GameObject artifactsRow; // shown once GameManager.HasFoundAnyShard

        private bool _open;
        private GameObject _activeContentPanel;
        private Coroutine _animCoroutine;

        private void OnEnable()
        {
            catcherButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            contentBackButton.onClick.AddListener(ShowRowList);

            foreach (var row in rows)
            {
                var panelToShow = row.panel; // explicit per-iteration capture
                row.button.onClick.AddListener(() => ShowContent(panelToShow));
            }

            GameManager.Instance.OnStateChanged += RefreshConditionalRows;
            RefreshConditionalRows();
            ShowRowList();
            SetImmediate(false);
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= RefreshConditionalRows;
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
            if (artifactsRow != null) artifactsRow.SetActive(GameManager.Instance.HasFoundAnyShard);
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

        private void ShowContent(GameObject content)
        {
            if (content == null) return;
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
