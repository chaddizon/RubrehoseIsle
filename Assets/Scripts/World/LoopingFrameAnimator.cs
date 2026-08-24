using System.Collections;
using UnityEngine;

namespace Rubrehose.World
{
    // Loops a SpriteRenderer through a fixed set of frames at a slow ambient pace (campfire
    // flicker, flag-pole flutter, etc.) — purely cosmetic, no gameplay state, no collider
    // involvement. Assumes all frames share near-identical content bounds (confirmed per-use
    // by whoever wires it up); if a future frame set doesn't, it'll need its own per-frame
    // offset handling the way HutConstructionState has for its half-built stage.
    public class LoopingFrameAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;

        // 6-8fps reads as a gentle flicker/flutter; anything faster starts looking like a
        // fast strobe rather than an ambient loop.
        [SerializeField] private float framesPerSecond = 7f;

        private void OnEnable()
        {
            if (frames == null || frames.Length == 0) return;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            var wait = new WaitForSeconds(1f / framesPerSecond);
            int index = 0;
            while (true)
            {
                spriteRenderer.sprite = frames[index];
                index = (index + 1) % frames.Length;
                yield return wait;
            }
        }
    }
}
