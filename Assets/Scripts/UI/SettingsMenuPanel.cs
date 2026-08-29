using UnityEngine;
using UnityEngine.UI;

namespace Rubrehose.UI
{
    // Menu -> Settings panel (NEXT_CLAUDE_CODE_PUSH.md §3: "Sound toggle, reset/save stub,
    // credits"). Sound toggle is genuinely wired (AudioListener.volume) since that's harmless
    // and fully reversible; Reset Save deliberately only logs — NOT wired to actually delete
    // save data without an explicit ask, since that's a real destructive/irreversible action
    // this doc didn't ask for. Credits text is static, built by MenuDrawerBuilder.
    public class SettingsMenuPanel : MonoBehaviour
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Button resetSaveButton;

        private void Awake()
        {
            soundToggle.isOn = AudioListener.volume > 0f;
            soundToggle.onValueChanged.AddListener(on => AudioListener.volume = on ? 1f : 0f);
            resetSaveButton.onClick.AddListener(() =>
                Debug.Log("SettingsMenuPanel: Reset Save tapped — not wired to actually delete save data (deliberately, given how destructive that would be)."));
        }
    }
}
