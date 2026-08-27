using System.Collections.Generic;
using UnityEngine;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Menu -> Crew panel (HUD_AND_LANDING_COVE_LAYOUT.md §E "full management in Menu ->
    // Crew"). Landing Cove's Camp home spots (CrewRecruitSpot) handle the first recruit
    // tap in-world; this is where players keep recruiting/checking rates from afterward.
    public class CrewMenuPanel : MonoBehaviour
    {
        [SerializeField] private Transform listContainer;
        [SerializeField] private CrewListItemUI itemPrefab;

        private readonly List<CrewListItemUI> _items = new List<CrewListItemUI>();

        private void OnEnable()
        {
            if (_items.Count == 0) BuildList();
            GameManager.Instance.OnStateChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= RefreshAll;
        }

        private void BuildList()
        {
            foreach (var def in CrewCatalog.WreckBeachCrew)
            {
                var item = Instantiate(itemPrefab, listContainer);
                item.Bind(def.id);
                _items.Add(item);
            }
        }

        private void RefreshAll()
        {
            foreach (var item in _items) item.Refresh();
        }
    }
}
