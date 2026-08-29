using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rubrehose.UI
{
    // The single dismissible onboarding popup (CORE_PROGRESSION_RESTRUCTURE.md "Onboarding /
    // tutorial system"): one at a time, single-tap dismiss, non-blocking beyond itself — no
    // full-screen catcher, the rest of the HUD stays tappable underneath it, unlike
    // MenuDrawerController's drawer. OnboardingController owns sequencing/queueing/"seen"
    // tracking and just calls Show() for whichever popup is next.
    public class OnboardingPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button dismissButton;
        [SerializeField] private float fadeSeconds = 0.2f;

        public bool IsShowing { get; private set; }

        private Action _onDismissed;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            dismissButton.onClick.AddListener(Dismiss);
            root.SetActive(false);
        }

        public void Show(string title, string body, Action onDismissed)
        {
            _onDismissed = onDismissed;
            titleText.text = title;
            bodyText.text = body;
            IsShowing = true;
            root.SetActive(true);

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, null));
        }

        // The only interaction this popup supports (doc: "dismissible with a single tap").
        private void Dismiss()
        {
            if (!IsShowing) return;
            IsShowing = false;

            Action callback = _onDismissed;
            _onDismissed = null;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, () =>
            {
                root.SetActive(false);
                callback?.Invoke();
            }));
        }

        private IEnumerator FadeRoutine(float from, float to, Action onComplete)
        {
            float t = 0f;
            canvasGroup.alpha = from;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }
    }
}
