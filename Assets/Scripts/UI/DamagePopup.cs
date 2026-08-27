using System.Collections;
using TMPro;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Floating damage number for the in-scene fight overlay (IN_SCENE_FIGHT_SYSTEM.md
    // "damage number pop-ups on hit"). Spawned entirely at runtime by FightController.Attack()
    // rather than hand-built like the rest of the UI — each instance is a one-shot, transient
    // object, so there's nothing for an editor tool to place ahead of time.
    public class DamagePopup : MonoBehaviour
    {
        private const float FloatDistancePixels = 50f;
        private const float DurationSeconds = 0.7f;

        public static void Spawn(RectTransform parent, Vector2 anchoredPosition, double amount)
        {
            bool hit = amount > 0;

            var go = new GameObject("DamagePopup", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Small random horizontal jitter so a burst of taps doesn't stack numbers exactly
            // on top of each other and read as one frozen digit.
            rt.anchoredPosition = anchoredPosition + new Vector2(Random.Range(-18f, 18f), 0f);
            rt.sizeDelta = new Vector2(160f, 44f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = hit ? Format.Number(amount) : "0";
            text.fontSize = hit ? 30f : 20f;
            text.fontStyle = hit ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = hit ? Palette.Cream : new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.55f);
            text.raycastTarget = false;

            go.AddComponent<DamagePopup>().Begin(rt, text);
        }

        private void Begin(RectTransform rt, TextMeshProUGUI text)
        {
            StartCoroutine(FloatAndFadeRoutine(rt, text));
        }

        private IEnumerator FloatAndFadeRoutine(RectTransform rt, TextMeshProUGUI text)
        {
            Vector2 start = rt.anchoredPosition;
            Color startColor = text.color;
            float t = 0f;
            while (t < DurationSeconds)
            {
                t += Time.deltaTime;
                float k = t / DurationSeconds;
                rt.anchoredPosition = start + Vector2.up * (FloatDistancePixels * k);
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, k);
                text.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
