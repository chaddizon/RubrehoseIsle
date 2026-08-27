using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Drives the serpent's in-scene reactions for IN_SCENE_FIGHT_SYSTEM.md's "juice"
    // requirements — hit-flash/flinch, wake-up, settle-back-down, and a distinct defeat
    // animation. No dedicated serpent art exists yet (placeholder sprite, same technique
    // as every other unfinished object in LandingCoveBuilder), so every beat here is a
    // transform/color tween rather than a frame swap; swap in real animated sprite sets
    // later without changing FightController's calls into this class.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SerpentVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Dormant (asleep, not yet engaged)")]
        [SerializeField] private float dormantScale = 0.8f;
        [SerializeField] private float dormantAlpha = 0.6f;

        [Header("Wake-up (fight start)")]
        [SerializeField] private float wakeUpDurationSeconds = 0.4f;

        [Header("Hit-flash (per successful hit)")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDurationSeconds = 0.15f;
        [SerializeField] private float hitRecoilDistance = 0.08f;

        [Header("Defeat")]
        [SerializeField] private float defeatDurationSeconds = 0.9f;

        private Vector3 _restLocalScale;
        private Vector3 _restLocalPosition;
        private Coroutine _routine;
        private bool _defeated;

        private void Awake()
        {
            _restLocalScale = transform.localScale;
            _restLocalPosition = transform.localPosition;
        }

        // Checked fresh on every enable (not just Awake) so a scene reload after an
        // already-persisted defeat (PlayerState.coveMinibossDefeated) snaps straight to the
        // defeated pose instead of dormant — GameManager.Instance is guaranteed set by now via
        // its DefaultExecutionOrder(-1000).
        private void OnEnable()
        {
            _defeated = GameManager.Instance.CoveMinibossDefeated;
            if (_defeated) SetDefeatedImmediate();
            else SetDormantImmediate();
        }

        public void PlayWakeUp()
        {
            if (_defeated) return;
            StartRoutine(WakeUpRoutine());
        }

        public void PlayHitFlash()
        {
            if (_defeated) return;
            StartRoutine(HitFlashRoutine());
        }

        public void SettleDormant()
        {
            if (_defeated) return;
            StartRoutine(SettleRoutine());
        }

        public void PlayDefeat()
        {
            if (_defeated) return;
            _defeated = true;
            StartRoutine(DefeatRoutine());
        }

        private void StartRoutine(IEnumerator routine)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(routine);
        }

        private void SetDormantImmediate()
        {
            transform.localScale = _restLocalScale * dormantScale;
            SetAlpha(dormantAlpha);
        }

        private IEnumerator WakeUpRoutine()
        {
            float startAlpha = spriteRenderer.color.a;
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < wakeUpDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / wakeUpDurationSeconds);
                transform.localScale = Vector3.Lerp(startScale, _restLocalScale, k);
                SetAlpha(Mathf.Lerp(startAlpha, 1f, k));
                yield return null;
            }
            transform.localScale = _restLocalScale;
            SetAlpha(1f);
        }

        private IEnumerator HitFlashRoutine()
        {
            Color baseColor = spriteRenderer.color;
            Vector3 recoilTarget = _restLocalPosition + Vector3.left * hitRecoilDistance;
            float t = 0f;
            while (t < hitFlashDurationSeconds)
            {
                t += Time.deltaTime;
                float k = t / hitFlashDurationSeconds;
                // Out and back in one short beat rather than a lerp across the whole
                // duration — reads as a flinch, not a slide.
                float recoilK = Mathf.Sin(k * Mathf.PI);
                spriteRenderer.color = Color.Lerp(hitFlashColor, baseColor, k);
                transform.localPosition = Vector3.Lerp(_restLocalPosition, recoilTarget, recoilK);
                yield return null;
            }
            spriteRenderer.color = baseColor;
            transform.localPosition = _restLocalPosition;
        }

        private IEnumerator SettleRoutine()
        {
            Vector3 startScale = transform.localScale;
            float startAlpha = spriteRenderer.color.a;
            Vector3 dormantTargetScale = _restLocalScale * dormantScale;
            float t = 0f;
            while (t < wakeUpDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / wakeUpDurationSeconds);
                transform.localScale = Vector3.Lerp(startScale, dormantTargetScale, k);
                SetAlpha(Mathf.Lerp(startAlpha, dormantAlpha, k));
                yield return null;
            }
            transform.localScale = dormantTargetScale;
            SetAlpha(dormantAlpha);
        }

        private IEnumerator DefeatRoutine()
        {
            Vector3 startScale = transform.localScale;
            float startAlpha = spriteRenderer.color.a;
            Vector3 sunkPosition = _restLocalPosition + Vector3.down * 0.3f;
            float t = 0f;
            while (t < defeatDurationSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / defeatDurationSeconds);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
                transform.localPosition = Vector3.Lerp(_restLocalPosition, sunkPosition, k);
                SetAlpha(Mathf.Lerp(startAlpha, 0f, k));
                yield return null;
            }
            SetDefeatedImmediate();
        }

        private void SetDefeatedImmediate()
        {
            transform.localScale = Vector3.zero;
            SetAlpha(0f);

            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false; // defeated for good — no re-tapping a beaten cove's boss
        }

        private void SetAlpha(float alpha)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
