using System;
using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // One Artifacts "tell" spawn point (NEXT_CLAUDE_CODE_PUSH.md §1a) — dormant ambient idle
    // loop by default (base sprite, not owned by this script — Chad's normal per-cove art
    // pass), flipped "live" by a TellSpawner at random (adds liveGlintOverlay on top, becomes
    // tappable). Tapping a live tell grants one Compass Shard and returns it to dormant; if
    // left untapped past liveWindowSeconds it auto-collects instead of expiring the reward
    // ("nothing's ever truly lost", same principle as the roaming hermit crab).
    [RequireComponent(typeof(Collider2D))]
    public class TellSpot : MonoBehaviour
    {
        [Tooltip("Placeholder sparkle/glint child, inactive by default — only this overlay is a UI placeholder concern per NEXT_CLAUDE_CODE_PUSH.md §4; the base idle-loop sprite is Chad's normal art.")]
        [SerializeField] private GameObject liveGlintOverlay;
        [SerializeField] private float liveWindowSeconds = 30f;

        private Collider2D _collider;
        private string _liveTier;
        private int _coveIndex;
        private Action<TellSpot> _onCollected;
        private Coroutine _windowRoutine;

        public bool IsLive => _liveTier != null;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.enabled = false; // only tappable while live
            if (liveGlintOverlay != null) liveGlintOverlay.SetActive(false);
        }

        // Called by TellSpawner when this spot is chosen to go live.
        public void GoLive(int coveIndex, string tier, Action<TellSpot> onCollected)
        {
            if (IsLive) return;
            _coveIndex = coveIndex;
            _liveTier = tier;
            _onCollected = onCollected;

            _collider.enabled = true;
            if (liveGlintOverlay != null) liveGlintOverlay.SetActive(true);
            GameManager.Instance.SetCoveTellLive(coveIndex, true);

            if (_windowRoutine != null) StopCoroutine(_windowRoutine);
            _windowRoutine = StartCoroutine(AutoCollectAfterWindow());
        }

        private void OnMouseDown()
        {
            if (IsLive) Collect();
        }

        private IEnumerator AutoCollectAfterWindow()
        {
            yield return new WaitForSeconds(liveWindowSeconds);
            if (IsLive) Collect();
        }

        private void Collect()
        {
            if (_windowRoutine != null)
            {
                StopCoroutine(_windowRoutine);
                _windowRoutine = null;
            }

            GameManager.Instance.AddCompassShard(_liveTier);
            GameManager.Instance.SetCoveTellLive(_coveIndex, false);
            var callback = _onCollected;

            _liveTier = null;
            _onCollected = null;
            _collider.enabled = false;
            if (liveGlintOverlay != null) liveGlintOverlay.SetActive(false);

            callback?.Invoke(this);
        }
    }
}
