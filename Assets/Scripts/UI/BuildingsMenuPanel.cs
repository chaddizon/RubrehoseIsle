using System.Collections.Generic;
using UnityEngine;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Menu -> Buildings panel (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings"). Only lists
    // buildings whose cove has actually been reached — matches "the world itself stays silent
    // until something is actually earned": no point listing a cove's building before the
    // player has even arrived there. Re-filters on every refresh since coveIndex can advance
    // while this panel exists in the hierarchy (even though it's only visible while open).
    public class BuildingsMenuPanel : MonoBehaviour
    {
        [SerializeField] private Transform listContainer;
        [SerializeField] private BuildingListItemUI itemPrefab;

        private readonly Dictionary<string, BuildingListItemUI> _items = new Dictionary<string, BuildingListItemUI>();

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= RefreshAll;
        }

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            foreach (var def in CoveBuildingCatalog.Buildings)
            {
                bool reached = gm.State.coveIndex >= def.coveIndex;
                bool exists = _items.TryGetValue(def.id, out var item);

                if (reached && !exists)
                {
                    item = Instantiate(itemPrefab, listContainer);
                    item.Bind(def.id);
                    _items[def.id] = item;
                }
                else if (exists)
                {
                    item.Refresh();
                }
            }
        }
    }
}
