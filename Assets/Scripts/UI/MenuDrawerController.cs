using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Comic-panel-style drawer (HUD_AND_LANDING_COVE_LAYOUT.md §C), slides in from the
    // right edge. Rows are placeholder shapes now; entry CONTENT (actual Crew/Upgrades/etc
    // panels) doesn't exist yet — tapping a row just logs which one for now.
    public class MenuDrawerController : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float openDurationSeconds = 0.3f;
        [SerializeField] private float closedX = 380f; // off-screen, right of the panel's own width
        [SerializeField] private float openX = 0f;

        [Header("Conditionally-visible rows")]
        [SerializeField] private GameObject captainsLogRow; // shown once GameManager.State.constructionComplete
        [SerializeField] private GameObject artifactsRow;

        [Tooltip("Placeholder — wire to real prestige-count state once Prestige exists. Artifacts row " +
                 "stays hidden entirely (not shown-but-locked) until this is true.")]
        [SerializeField] private bool artifactsUnlocked;

        private bool _open;
        private Coroutine _animCoroutine;

        private void OnEnable()
        {
            RefreshConditionalRows();
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

        public void Close() => AnimateTo(false);

        private void RefreshConditionalRows()
        {
            if (captainsLogRow != null) captainsLogRow.SetActive(GameManager.Instance.State.constructionComplete);
            if (artifactsRow != null) artifactsRow.SetActive(artifactsUnlocked);
        }

        private void AnimateTo(bool open)
        {
            _open = open;
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateRoutine(open));
        }

        private void SetImmediate(bool open)
        {
            _open = open;
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

        // Placeholder entry-tap handler — real per-entry panels don't exist yet.
        public void OnEntryTapped(string entryName)
        {
            Debug.Log("MenuDrawerController: '" + entryName + "' tapped — no panel content yet.");
        }
    }
}
