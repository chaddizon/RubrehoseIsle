using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Data;
using Rubrehose.UI;
using Rubrehose.World;

namespace Rubrehose.Combat
{
    // Drives the 30s serpent fight (GAME_DESIGN.md "Fight Duration = 30 seconds base",
    // "Cooldown = 20 minutes between attempts"), presented in-scene per
    // IN_SCENE_FIGHT_SYSTEM.md rather than as a modal: this component sits directly on the
    // serpent's world GameObject (LandingCoveBuilder's Frontier cluster), tapping the serpent
    // itself both starts and fuels the fight, and HP/timer render as a screen-space overlay
    // that tracks the serpent's world position rather than taking over the screen.
    //
    // Matches Obelisk's exact model (rubrehose_prototype.html): boss HP persists across
    // attempts and only resets when GameManager.BuildConstruction advances to a fresh cove;
    // unlimited attempts gated only by a 20-min real-time cooldown started on timeout (never
    // on defeat); damage below armor deals zero rather than being wasted. World refs wired by
    // LandingCoveBuilder, overlay refs wired by FightOverlayBuilder — see the order note in
    // UNITY_SETUP.md.
    [RequireComponent(typeof(Collider2D))]
    public class FightController : MonoBehaviour
    {
        [Header("World refs (wired by LandingCoveBuilder)")]
        [SerializeField] private SerpentVisual serpentVisual;

        [Header("Overlay UI refs (wired by FightOverlayBuilder)")]
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private TMP_Text serpentNameText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text timerText;

        [Header("Tuning")]
        [SerializeField] private float overlayWorldHeightOffset = 1.1f;
        [SerializeField] private float hpBarAnimationSpeed = 2.5f; // fraction of the bar per second
        [SerializeField] private float timerWarningThresholdSeconds = 5f;
        [SerializeField] private float endLingerSeconds = 1f; // overlay stays up briefly after timeout/defeat

        // Crew (CrewHomeSpotAnimator) reads these to know whether — and where — to head in to
        // fight. A single pair of static fields is enough since only one boss fight can be in
        // progress at a time in this vertical slice (one cove built, one Frontier trigger).
        public static bool IsFightActive { get; private set; }
        public static Transform ActiveSerpent { get; private set; }
        public static event Action OnFightActiveChanged;

        private double _hpMax;
        private double _armor;
        private float _timeLeft;
        private bool _active;
        private float _hpDisplayedFraction;
        private float _hpTargetFraction;

        private void OnDisable()
        {
            if (_active) SetActive(false);
        }

        // Tapping the serpent itself is the only input now — no separate Attack/Retreat
        // buttons (IN_SCENE_FIGHT_SYSTEM.md "consistent with tapping driftwood/every other
        // world object"). Dormant + tappable-again starts a fresh attempt; active + tapped
        // deals damage.
        private void OnMouseDown()
        {
            if (_active) Attack();
            else TryStartFight();
        }

        // Kept public (and idempotent) so MainHUDController's Fight button can still jump
        // straight into an attempt without requiring the player to find/tap the serpent first.
        public void OpenFight() => TryStartFight();

        private void TryStartFight()
        {
            if (_active) return;
            var gm = GameManager.Instance;
            if (!gm.CanAttemptFight) return; // defeated or still on cooldown

            gm.BeginFightAttempt(); // sets full HP only on this cove's first-ever attempt
            _hpMax = gm.CoveHp;
            _armor = gm.CoveArmor;
            _timeLeft = GameFormulas.FightDurationSeconds;
            SetActive(true);

            _hpTargetFraction = Mathf.Clamp01((float)(gm.BossHpRemaining / _hpMax));
            _hpDisplayedFraction = _hpTargetFraction;

            if (serpentNameText != null) serpentNameText.text = WreckBeachData.SerpentNames[gm.State.coveIndex];
            UpdateStatsText(gm);
            if (hpSlider != null) hpSlider.value = _hpDisplayedFraction;
            UpdateTimerDisplay();

            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(true);
                PositionOverlay();
            }

            if (serpentVisual != null) serpentVisual.PlayWakeUp();
        }

        private void Update()
        {
            if (_active)
            {
                _timeLeft -= Time.deltaTime;
                if (_timeLeft <= 0f)
                {
                    _timeLeft = 0f;
                    UpdateTimerDisplay();
                    HandleTimeUp();
                }
                else
                {
                    UpdateTimerDisplay();
                }
            }

            // Keeps animating/tracking through the post-fight linger window (defeat or
            // timeout), not just while _active — so the HP bar can finish draining to 0 and
            // the overlay doesn't jump-cut away from the serpent's position.
            if (overlayRoot != null && overlayRoot.gameObject.activeSelf)
            {
                AnimateHpBar();
                PositionOverlay();
            }
        }

        public void Attack()
        {
            if (!_active) return;

            var gm = GameManager.Instance;
            double dmg = Math.Max(0, gm.TapPower - _armor);

            if (dmg > 0)
            {
                gm.ApplyFightDamage(dmg);
                if (serpentVisual != null) serpentVisual.PlayHitFlash();
                _hpTargetFraction = Mathf.Clamp01((float)(gm.BossHpRemaining / _hpMax));
                UpdateStatsText(gm);
            }

            SpawnDamagePopup(dmg);

            if (gm.CoveMinibossDefeated)
            {
                SetActive(false);
                if (serpentVisual != null) serpentVisual.PlayDefeat();
                Invoke(nameof(CloseOverlay), endLingerSeconds);
            }
        }

        // Timeout only — never called on defeat, so it always starts the real cooldown
        // (GameManager.EndFightAttempt no-ops that if the boss somehow died first anyway).
        private void HandleTimeUp()
        {
            SetActive(false);
            if (serpentVisual != null) serpentVisual.SettleDormant();
            Invoke(nameof(EndAttemptAndCloseOverlay), endLingerSeconds);
        }

        private void EndAttemptAndCloseOverlay()
        {
            GameManager.Instance.EndFightAttempt();
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            if (overlayRoot != null) overlayRoot.gameObject.SetActive(false);
        }

        private void SetActive(bool value)
        {
            if (_active == value) return;
            _active = value;
            IsFightActive = value;
            ActiveSerpent = value ? transform : null;
            OnFightActiveChanged?.Invoke();
        }

        private void UpdateStatsText(GameManager gm)
        {
            if (statsText == null) return;
            statsText.text = $"HP {Format.Number(gm.BossHpRemaining)} / {Format.Number(_hpMax)} · Armor {Format.Number(_armor)}";
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;
            timerText.text = $"{Mathf.Max(0f, _timeLeft):F1}s";
            timerText.color = _timeLeft <= timerWarningThresholdSeconds ? Palette.WarningRed : Palette.Ink;
        }

        private void AnimateHpBar()
        {
            if (hpSlider == null) return;
            _hpDisplayedFraction = Mathf.MoveTowards(_hpDisplayedFraction, _hpTargetFraction, hpBarAnimationSpeed * Time.deltaTime);
            hpSlider.value = _hpDisplayedFraction;
        }

        // Screen-space overlay tracking a world point — standard conversion for a
        // ScreenSpaceOverlay canvas (worldCamera is implicitly null in that render mode, so
        // ScreenPointToLocalPointInRectangle is called with a null camera to match).
        private void PositionOverlay()
        {
            if (overlayRoot == null || canvasRect == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * overlayWorldHeightOffset;
            Vector2 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
            {
                overlayRoot.anchoredPosition = localPoint;
            }
        }

        private void SpawnDamagePopup(double amount)
        {
            if (canvasRect == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * (overlayWorldHeightOffset * 0.5f);
            Vector2 screenPoint = cam.WorldToScreenPoint(worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint)) return;

            DamagePopup.Spawn(canvasRect, localPoint, amount);
        }
    }
}
