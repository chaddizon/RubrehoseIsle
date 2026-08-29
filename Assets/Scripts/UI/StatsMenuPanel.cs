using TMPro;
using UnityEngine;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Menu -> Stats/Progress panel (NEXT_CLAUDE_CODE_PUSH.md §3: "raw numbers dump is fine").
    // Real (not a stub) — every number below already exists in GameManager/PlayerState.
    public class StatsMenuPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            bool endless = gm.IsEndlessCove(gm.State.coveIndex);

            bodyText.text =
                $"Driftwood: {Format.Number(gm.State.driftwood)}\n" +
                $"Total earned all-time: {Format.Number(gm.State.totalEarnedAllTime)}\n" +
                $"Tap power: {Format.Number(gm.TapPower)} (level {gm.State.tapLevel})\n" +
                $"Coves unlocked: {gm.State.coveIndex + 1} / {WreckBeachData.CoveNames.Length}\n" +
                (endless ? $"Serpent level: {gm.SerpentLevel}\n" : "") +
                $"Total mini-boss/serpent clears: {gm.State.totalClears}\n" +
                $"Crew recruited: {CountRecruited(gm)} / {gm.State.crew.Count}";
        }

        private static int CountRecruited(GameManager gm)
        {
            int count = 0;
            foreach (var c in gm.State.crew) if (c.level >= 1) count++;
            return count;
        }
    }
}
