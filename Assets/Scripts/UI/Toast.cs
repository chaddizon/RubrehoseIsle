using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Small screen-space toast for one-off notices (e.g. "BBW joined the crew!"). Runtime-
    // spawned like DamagePopup — each instance is transient, nothing for an editor tool to
    // hand-place ahead of time. Finds the shared PersistentUICanvas by name (must match
    // RubrehoseEditorUtils.PersistentCanvasName — that constant lives in Assets/Editor, not
    // reachable from this runtime assembly) since callers don't already carry a canvas ref
    // the way FightController's damage popups do.
    public class Toast : MonoBehaviour
    {
        private const string PersistentCanvasName = "PersistentUICanvas";
        private const float FadeInSeconds = 0.15f;
        private const float HoldSeconds = 1.6f;
        private const float FadeOutSeconds = 0.4f;
        private const float TopOffsetY = -140f; // below the top HUD row, screen-space overlay

        public static void Spawn(string message)
        {
            var canvasGo = GameObject.Find(PersistentCanvasName);
            if (canvasGo == null) return;
            var canvasRect = canvasGo.transform as RectTransform;
            if (canvasRect == null) return;

            var go = new GameObject("Toast", typeof(RectTransform));
            go.transform.SetParent(canvasRect, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, TopOffsetY);
            rt.sizeDelta = new Vector2(360f, 56f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.85f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(rt, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 4f);
            textRt.offsetMax = new Vector2(-16f, -4f);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.color = Palette.Cream;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            var toast = go.AddComponent<Toast>();
            toast.StartCoroutine(toast.LifecycleRoutine(bg, text));
        }

        private IEnumerator LifecycleRoutine(Image bg, TextMeshProUGUI text)
        {
            Color bgFull = bg.color;
            Color textFull = text.color;
            SetAlpha(bg, text, 0f, 0f);

            float t = 0f;
            while (t < FadeInSeconds)
            {
                t += Time.deltaTime;
                SetAlpha(bg, text, bgFull.a * (t / FadeInSeconds), textFull.a * (t / FadeInSeconds));
                yield return null;
            }
            SetAlpha(bg, text, bgFull.a, textFull.a);

            yield return new WaitForSeconds(HoldSeconds);

            t = 0f;
            while (t < FadeOutSeconds)
            {
                t += Time.deltaTime;
                float k = 1f - (t / FadeOutSeconds);
                SetAlpha(bg, text, bgFull.a * k, textFull.a * k);
                yield return null;
            }
            Destroy(gameObject);
        }

        private static void SetAlpha(Image bg, TextMeshProUGUI text, float bgAlpha, float textAlpha)
        {
            Color c = bg.color; c.a = bgAlpha; bg.color = c;
            Color tc = text.color; tc.a = textAlpha; text.color = tc;
        }
    }
}
