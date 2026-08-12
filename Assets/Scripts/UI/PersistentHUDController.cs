using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;

namespace Rubrehose.UI
{
    // Runtime binding for the persistent screen-space HUD (HUD_AND_LANDING_COVE_LAYOUT.md
    // §A master table) — everything except the fast-travel handle/ribbon, which stays
    // owned by FastTravelRibbonController. Only the driftwood counter and menu button are
    // wired to real state right now; cast-a-net/crate/critters have no backing system yet,
    // so they're visual placeholders with setter methods ready for whichever system ends
    // up feeding them.
    public class PersistentHUDController : MonoBehaviour
    {
        [Header("Currency pill")]
        [SerializeField] private TMP_Text driftwoodText;

        [Header("Menu button")]
        [SerializeField] private Button menuButton;

        [Header("Cast a Net / Bottle Toss")]
        [SerializeField] private TMP_Text castNetChargeText;

        [Header("Salvage Crate meter")]
        [SerializeField] private Image crateFillImage;

        [Header("Banked Critters")]
        [SerializeField] private TMP_Text bankedCrittersText;

        private MenuDrawerController _menuDrawer;

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += Refresh;
            menuButton.onClick.AddListener(OpenMenu);
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void OpenMenu()
        {
            if (_menuDrawer == null) _menuDrawer = Object.FindFirstObjectByType<MenuDrawerController>();
            if (_menuDrawer != null) _menuDrawer.Toggle();
            else Debug.LogWarning("PersistentHUDController: no MenuDrawerController in the scene — run " +
                                   "Rubrehose > Build Persistent UI > Menu Drawer.");
        }

        private void Refresh()
        {
            driftwoodText.text = Format.Number(GameManager.Instance.State.driftwood);
        }

        // No backing system yet for any of these three — callers set them directly.
        public void SetCastNetCharges(int count) => castNetChargeText.text = count.ToString();
        public void SetCrateFill(float normalized01) => crateFillImage.fillAmount = Mathf.Clamp01(normalized01);
        public void SetBankedCritters(int count) => bankedCrittersText.text = count.ToString();
    }
}
