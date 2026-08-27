using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Combat;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Top-level HUD for the Wreck Beach scene. Wire every field in the Inspector to the
    // matching UI element, then drop this on the Canvas (or a HUD root under it).
    public class MainHUDController : MonoBehaviour
    {
        [Header("Resource / cove readout")]
        [SerializeField] private TMP_Text driftwoodText;
        [SerializeField] private TMP_Text biomeTagText;
        [SerializeField] private TMP_Text coveLineText;
        [SerializeField] private Slider coveProgressSlider;

        [Header("Tap")]
        [SerializeField] private TMP_Text tapAmountText;
        [SerializeField] private Button tapButton;

        [Header("Tap upgrade")]
        [SerializeField] private TMP_Text tapUpgradeLabel;
        [SerializeField] private Button tapUpgradeButton;

        [Header("Crew")]
        [SerializeField] private Transform crewListContainer;
        [SerializeField] private CrewListItemUI crewItemPrefab;

        [Header("Fight")]
        [SerializeField] private Button fightButton;
        [SerializeField] private FightController fightController;

        [Header("Construction gate")]
        [SerializeField] private GameObject constructionSection;
        [SerializeField] private TMP_Text constructionLabel;
        [SerializeField] private Button constructionButton;

        private readonly List<CrewListItemUI> _crewItems = new List<CrewListItemUI>();

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            gm.OnStateChanged += Refresh;

            tapButton.onClick.AddListener(gm.Tap);
            fightButton.onClick.AddListener(fightController.OpenFight);
            tapUpgradeButton.onClick.AddListener(gm.UpgradeTap);
            constructionButton.onClick.AddListener(gm.BuildConstruction);

            BuildCrewList();
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        private void BuildCrewList()
        {
            foreach (Transform child in crewListContainer) Destroy(child.gameObject);
            _crewItems.Clear();

            foreach (var def in CrewCatalog.WreckBeachCrew)
            {
                var item = Instantiate(crewItemPrefab, crewListContainer);
                item.Bind(def.id);
                _crewItems.Add(item);
            }
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;

            driftwoodText.text = Format.Number(gm.State.driftwood);
            biomeTagText.text = WreckBeachData.BiomeName;
            coveLineText.text = $"{WreckBeachData.CoveNames[gm.State.coveIndex]} · " +
                                 (gm.CoveMinibossDefeated ? "mini-boss defeated" : "mini-boss undefeated");
            coveProgressSlider.value = gm.CoveMinibossDefeated ? 1f : 0f;

            tapAmountText.text = Format.Number(gm.TapPower);

            tapUpgradeLabel.text = $"Tap Power (Lv {gm.State.tapLevel}) — {Format.Number(gm.TapUpgradeCost)} Driftwood";
            tapUpgradeButton.interactable = gm.CanUpgradeTap;

            foreach (var item in _crewItems) item.Refresh();

            constructionSection.SetActive(gm.ConstructionUnlocked);
            if (gm.ConstructionUnlocked)
            {
                constructionLabel.text = gm.IsCoveConstructionComplete(gm.State.coveIndex)
                    ? "Crossing built."
                    : $"Build the crossing — {Format.Number(gm.ConstructionCost)} Driftwood + {WreckBeachData.ConstructionMaterial}";
                constructionButton.interactable = gm.CanBuildConstruction;
            }
        }
    }
}
