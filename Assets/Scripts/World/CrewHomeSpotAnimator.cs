using System.Collections;
using UnityEngine;
using Rubrehose.Core;

namespace Rubrehose.World
{
    // Drives a crew home spot's pose (BBW, BBC): idle loop by default, working loop while
    // the crew member is actually producing (level > 0, i.e. recruited), a brief tap-reaction
    // flash on tap before returning to whichever of those two is now correct. crewId-driven
    // so one component serves both home spots with different wired-up frame sets.
    //
    // Independent OnMouseDown() from CrewRecruitSpot's (same GameObject, same collider) —
    // Unity calls every component's OnMouseDown on a click, so no cross-referencing is
    // needed: this one only drives the visual flash, CrewRecruitSpot handles the actual
    // recruit.
    //
    // The working pose is a genuine on-all-fours stance, visually distinct from the bipedal
    // idle/tap poses — the idle<->working transition is expected to look abrupt/stylized
    // rather than smoothly morphing; that's accepted, not a bug.
    [RequireComponent(typeof(SpriteRenderer))]
    public class CrewHomeSpotAnimator : MonoBehaviour
    {
        [SerializeField] private string crewId;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Idle (default) — slow alternation")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFramesPerSecond = 0.6f;

        [Header("Working (while actively producing)")]
        [SerializeField] private Sprite[] workingFrames;
        [SerializeField] private float workingFramesPerSecond = 5f;

        [Header("Tap reaction (brief flash on tap)")]
        [SerializeField] private Sprite tapReactionFrame;
        [SerializeField] private float tapReactionSeconds = 0.4f;

        private Coroutine _loopRoutine;
        private Coroutine _tapRoutine;

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void OnMouseDown()
        {
            if (_tapRoutine != null) StopCoroutine(_tapRoutine);
            _tapRoutine = StartCoroutine(TapReactionRoutine());
        }

        private bool IsWorking()
        {
            var crew = GameManager.Instance.GetCrewState(crewId);
            return crew != null && crew.level > 0;
        }

        // Guarded against interrupting an in-progress tap flash — OnStateChanged fires the
        // instant the recruit lands (mid-flash, since the tap that triggered it is also what
        // recruits), but the flash's own routine re-checks and resumes the correct loop when
        // it ends, so no state changes are lost.
        private void Refresh()
        {
            if (_tapRoutine != null) return;
            PlayLoop(IsWorking() ? workingFrames : idleFrames, IsWorking() ? workingFramesPerSecond : idleFramesPerSecond);
        }

        private void PlayLoop(Sprite[] frames, float framesPerSecond)
        {
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            _loopRoutine = StartCoroutine(LoopRoutine(frames, framesPerSecond));
        }

        private IEnumerator LoopRoutine(Sprite[] frames, float framesPerSecond)
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
            spriteRenderer.sprite = tapReactionFrame;
            yield return new WaitForSeconds(tapReactionSeconds);
            _tapRoutine = null;
            Refresh(); // resume whichever loop (idle/working) is now correct
        }
    }
}
