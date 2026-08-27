using System.Collections;
using UnityEngine;
using Rubrehose.Combat;
using Rubrehose.Core;
using Rubrehose.UI;

namespace Rubrehose.World
{
    // Drives a crew home spot's presence AND position (BBW, BBC): entirely absent — no
    // sprite, no collider — until recruited, so there's nothing standing (or tappable) at
    // an unrecruited spot. The instant it's first recruited, a one-shot celebration (pop +
    // toast) plays and it becomes a real, full-color, tappable presence: a working loop
    // while actively producing, a brief tap-reaction flash on tap, and — while a fight is
    // active — walks out to the serpent (reusing the idle frame set as a walk-cycle, per
    // IN_SCENE_FIGHT_SYSTEM.md "crew participation": no dedicated walk art exists, the idle
    // set already reads as a walking pose), switches to the attack loop on arrival, then
    // walks back to its HomeSpot and resumes working once home. crewId-driven so one
    // component serves both home spots with different wired-up frame sets.
    //
    // Since this GameObject's collider is only ever enabled AFTER the first recruit, the
    // sibling CrewRecruitSpot's world tap can never perform that first recruit — the only
    // way in is Menu -> Crew (CrewListItemUI, unaffected by any of this). CrewRecruitSpot's
    // world tap still works for levelling further, same as before, once this component has
    // enabled the collider.
    //
    // Independent OnMouseDown() from CrewRecruitSpot's (same GameObject, same collider) —
    // Unity calls every component's OnMouseDown on a click, so no cross-referencing is
    // needed: this one only drives the visual flash, CrewRecruitSpot handles the actual
    // recruit/level-up. The recruit celebration below is detected independently, via this
    // component's own OnStateChanged subscription noticing level go from 0 to >0, rather
    // than CrewRecruitSpot calling into this class directly.
    //
    // The working pose is a visually distinct stance from the bipedal walk/tap pose — that
    // transition is expected to look abrupt/stylized rather than smoothly morphing; that's
    // accepted, not a bug.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class CrewHomeSpotAnimator : MonoBehaviour
    {
        [SerializeField] private string crewId;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Walk-cycle frames — not shown while stationary (crew are hidden until " +
                "recruited, so there's no idle pose anymore); reused only for the transit " +
                "walk to/from the serpent during a fight, below")]
        [SerializeField] private Sprite[] idleFrames;

        [Header("Working (while actively producing)")]
        [SerializeField] private Sprite[] workingFrames;
        [SerializeField] private float workingFramesPerSecond = 5f;

        [Header("Attacking (held at the serpent for as long as a fight is active) — art added " +
                "separately per recruit, this just wires the state-swap so it's ready to receive it")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float attackFramesPerSecond = 6f;

        [Header("Walk (out to the serpent and back) — idleFrames played faster, no separate art")]
        [SerializeField] private float walkFramesPerSecond = 4f;
        [SerializeField] private float moveSpeedUnitsPerSecond = 2.5f;
        // World-space offset from the serpent's position to stand at while attacking, so two
        // recruited crew members flank it instead of stacking on the same point — set per
        // instance by LandingCoveBuilder (BBW/BBC get different offsets).
        [SerializeField] private Vector2 attackOffset = Vector2.zero;

        [Header("Tap reaction (brief flash on tap)")]
        [SerializeField] private Sprite tapReactionFrame;
        [SerializeField] private float tapReactionSeconds = 0.4f;

        [Header("Recruit celebration (once, the instant this recruit's level first goes above 0)")]
        [SerializeField] private float recruitCelebrationSeconds = 0.6f;
        [SerializeField, Range(0f, 1f)] private float recruitPopStrength = 0.25f;

        private Collider2D _collider;
        private Coroutine _loopRoutine; // stationary working loop
        private Coroutine _tapRoutine;
        private Coroutine _participationRoutine; // walk out -> attack -> walk back
        private bool _loopActive;
        private bool _participating;
        private bool _celebrating;
        private bool _everRecruited;
        private Vector3 _homePosition;

        private void Awake()
        {
            _homePosition = transform.position;
            _collider = GetComponent<Collider2D>();
            // Baseline from whatever's already true on load (e.g. a save where this crew
            // member is already recruited) — only a live 0->1 transition during play should
            // ever trigger the celebration below, never a fresh scene load.
            _everRecruited = IsRecruited();
        }

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            FightController.OnFightActiveChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
            FightController.OnFightActiveChanged -= Refresh;
        }

        // Suppressed while out fighting — the collider (and this GameObject) has physically
        // moved to stand by the serpent, so a tap there is more likely aimed at the serpent
        // itself than at flashing this recruit's tap-reaction sprite mid-stride/mid-attack.
        // Can't fire at all pre-recruit (collider disabled), no separate guard needed for that.
        private void OnMouseDown()
        {
            if (_participating || _celebrating) return;
            if (_tapRoutine != null) StopCoroutine(_tapRoutine);
            _tapRoutine = StartCoroutine(TapReactionRoutine());
        }

        private bool IsRecruited()
        {
            var crew = GameManager.Instance.GetCrewState(crewId);
            return crew != null && crew.level > 0;
        }

        // Guarded against interrupting an in-progress tap flash, recruit celebration, or fight
        // participation lifecycle — all three manage the sprite/position themselves and call
        // this again once they're done.
        //
        // Also guarded against restarting a loop that's already correct: OnStateChanged fires
        // on every driftwood tap (uncooldowned), not just crew recruit/level/fight changes, so
        // a fast tapper would otherwise retrigger the loop faster than a frame's
        // WaitForSeconds can elapse — killing and restarting the coroutine forever and
        // freezing the sprite on frame 0 for as long as the tapping continues.
        private void Refresh()
        {
            if (_tapRoutine != null || _participating || _celebrating) return;

            bool recruited = IsRecruited();

            if (recruited && !_everRecruited)
            {
                _everRecruited = true;
                BeginCelebration();
                return;
            }

            if (!recruited)
            {
                SetHiddenUntilRecruited();
                return;
            }

            if (FightController.IsFightActive)
            {
                BeginParticipation();
                return;
            }

            if (_loopActive) return;
            PlayWorkingLoop();
        }

        private void SetHiddenUntilRecruited()
        {
            if (_loopRoutine != null) { StopCoroutine(_loopRoutine); _loopRoutine = null; }
            _loopActive = false;
            spriteRenderer.enabled = false;
            _collider.enabled = false;
        }

        private void PlayWorkingLoop()
        {
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            _loopActive = true;
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            _collider.enabled = true;
            _loopRoutine = StartCoroutine(FrameLoopRoutine(workingFrames, workingFramesPerSecond));
        }

        // One-shot: reveals the sprite/collider for the first time, a quick scale pop, and a
        // toast naming the newly-recruited crew member, then hands off to Refresh() for the
        // normal working loop (or straight into fight participation, in the unlikely case a
        // fight is already active at that exact instant).
        private void BeginCelebration()
        {
            if (_loopRoutine != null) { StopCoroutine(_loopRoutine); _loopRoutine = null; }
            _loopActive = false;
            _celebrating = true;
            StartCoroutine(RecruitCelebrationRoutine());
        }

        private IEnumerator RecruitCelebrationRoutine()
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            _collider.enabled = true;
            if (tapReactionFrame != null) spriteRenderer.sprite = tapReactionFrame;

            var def = GameManager.Instance.GetCrewDefinition(crewId);
            Toast.Spawn((def != null ? def.displayName : crewId) + " joined the crew!");

            Vector3 baseScale = transform.localScale;
            float t = 0f;
            while (t < recruitCelebrationSeconds)
            {
                t += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(t / recruitCelebrationSeconds * Mathf.PI) * recruitPopStrength;
                transform.localScale = baseScale * pulse;
                yield return null;
            }
            transform.localScale = baseScale;

            _celebrating = false;
            Refresh();
        }

        private void BeginParticipation()
        {
            if (_participating) return;
            if (_loopRoutine != null) { StopCoroutine(_loopRoutine); _loopRoutine = null; }
            _loopActive = false;
            _participating = true;
            _participationRoutine = StartCoroutine(ParticipationRoutine());
        }

        // Walks out to the serpent (idleFrames at the faster walk pace), holds the attack loop
        // for as long as the fight stays active, then walks the same frames back home and hands
        // control back to Refresh() for the normal working loop. Self-contained: started once
        // by Refresh() when a fight begins, it drives itself to completion off
        // FightController.IsFightActive rather than needing Refresh() to interrupt it (Refresh()
        // is a no-op while _participating is set, since a fight-end OnFightActiveChanged event
        // fires into this same routine's own polling loop below, not into Refresh()).
        private IEnumerator ParticipationRoutine()
        {
            Transform serpent = FightController.ActiveSerpent;
            if (serpent != null)
            {
                yield return WalkRoutine(serpent.position + (Vector3)attackOffset);

                Coroutine attackLoop = StartCoroutine(FrameLoopRoutine(attackFrames, attackFramesPerSecond));
                while (FightController.IsFightActive) yield return null;
                StopCoroutine(attackLoop);

                yield return WalkRoutine(_homePosition);
            }

            _participating = false;
            _participationRoutine = null;
            Refresh(); // resume the working loop now that it's home
        }

        private IEnumerator WalkRoutine(Vector3 target)
        {
            Coroutine walkLoop = StartCoroutine(FrameLoopRoutine(idleFrames, walkFramesPerSecond));
            while (transform.position != target)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeedUnitsPerSecond * Time.deltaTime);
                yield return null;
            }
            StopCoroutine(walkLoop);
        }

        private IEnumerator FrameLoopRoutine(Sprite[] frames, float framesPerSecond)
        {
            if (frames == null || frames.Length == 0) yield break;
            var wait = new WaitForSeconds(1f / framesPerSecond);
            int index = 0;
            while (true)
            {
                spriteRenderer.sprite = frames[index];
                index = (index + 1) % frames.Length;
                yield return wait;
            }
        }

        private IEnumerator TapReactionRoutine()
        {
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            _loopActive = false;
            spriteRenderer.sprite = tapReactionFrame;
            yield return new WaitForSeconds(tapReactionSeconds);
            _tapRoutine = null;
            Refresh(); // resume the working loop
        }
    }
}
