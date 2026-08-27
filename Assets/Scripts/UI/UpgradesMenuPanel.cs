using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Menu -> Upgrades panel (HUD_AND_LANDING_COVE_LAYOUT.md §C). Tap Power is the only
    // upgrade with a real backing system so far — Cast a Net/Bottle Toss/Crit trees join
    // this list once those systems exist (UNITY_SETUP.md's deferred follow-ups).
    public class UpgradesMenuPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text tapPowerLabel;
        [SerializeField] private Button tapPowerButton;

        private bool _buttonWired;

        private void OnEnable()
        {
            // Guarded because this panel is (de)activated every time the drawer switches
            // between the row list and this content view — without it, listeners pile up.
            if (!_buttonWired)
            {
                _buttonWired = true;
                tapPowerButton.onClick.AddListener(() => GameManager.Instance.UpgradeTap());
            }
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
            tapPowerLabel.text = $"Tap Power (Lv {gm.State.tapLevel}) — {Format.Number(gm.TapPower)}/tap\n" +
                                  $"Upgrade: {Format.Number(gm.TapUpgradeCost)} Driftwood";
            tapPowerButton.interactable = gm.CanUpgradeTap;
        }
    }
}
