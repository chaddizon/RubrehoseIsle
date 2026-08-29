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
        [Tooltip("Which cove this serpent belongs to — must match FightController.coveIndex on the same object.")]
        [SerializeField] private int coveIndex;
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

        [Header("Endless-cove respawn (cove 4 only — see FightController.IsEndlessCove)")]
        [SerializeField] private float respawnDelaySeconds = 0.5f;

        private Vector3 _restLocalScale;
        private Vector3 _restLocalPosition;
        private Coroutine _routine;
        private bool _defeated; // permanent — coves 1-3 only, never set for the endless cove
        private bool _respawning; // mid defeat->respawn tween — endless cove only

        private void Awake()
        {
            _restLocalScale = transform.localScale;
            _restLocalPosition = transform.localPosition;
        }

        // Checked fresh on every enable (not just Awake) so a scene reload after an
        // already-persisted defeat (PlayerState.coveMinibossDefeated) snaps straight to the
        // defeated pose instead of dormant — GameManager.Instance is guaranteed set by now via
        // its DefaultExecutionOrder(-1000). Reads THIS serpent's own coveIndex, not
        // GameManager.State.coveIndex (the navigation frontier) — every built cove's serpent
        // stays loaded simultaneously once the player has moved on, so this has to check its
        // own cove's flag specifically or an already-defeated serpent would appear alive again
        // the moment the frontier moves past it (2026-08-29 bug fix).
        private void OnEnable()
        {
            _defeated = GameManager.Instance.CoveMinibossDefeated(coveIndex);
            if (_defeated) SetDefeatedImmediate();
            else SetDormantImmediate();
        }

        public void PlayWakeUp()
        {
            if (_defeated || _respawning) return;
            StartRoutine(WakeUpRoutine());
        }

        public void PlayHitFlash()
        {
            if (_defeated || _respawning) return;
            StartRoutine(HitFlashRoutine());
        }

        public void SettleDormant()
        {
            if (_defeated || _respawning) return;
            StartRoutine(SettleRoutine());
        }

        // Permanent — coves 1-3 only. Collider disabled for good afterward (SetDefeatedImmediate).
        public void PlayDefeat()
        {
            if (_defeated) return;
            _defeated = true;
            StartRoutine(DefeatRoutine());
        }

        // Endless cove only (FightController.IsEndlessCove) — plays the same kill tween, then
        // resets back to dormant (collider re-enabled) instead of vanishing forever, ready for
        // the next, tougher level's encounter.
        public void PlayDefeatAndRespawn()
        {
            if (_respawning) return;
            _respawning = true;
            StartRoutine(DefeatAndRespawnRoutine());
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
            yield return DefeatTween();
            SetDefeatedImmediate();
        }

        private IEnumerator DefeatAndRespawnRoutine()
        {
            yield return DefeatTween();
            yield return new WaitForSeconds(respawnDelaySeconds);

            transform.localPosition = _restLocalPosition; // undo the tween's sink offset
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true; // re-tappable for the next level's encounter
            SetDormantImmediate();
            _respawning = false;
        }

        // Shared shrink/sink/fade tween used by both the permanent (PlayDefeat) and
        // endless-respawn (PlayDefeatAndRespawn) defeat flows — only what happens after it
        // differs between the two.
        private IEnumerator DefeatTween()
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
