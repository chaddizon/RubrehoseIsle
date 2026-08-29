using System.Collections.Generic;
using UnityEngine;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.UI
{
    // Menu -> Artifacts panel (NEXT_CLAUDE_CODE_PUSH.md §1b) — two lists: Shard stacks (browse/
    // appraise, one row per RarityTier) and the Artifact tree (one row per
    // ArtifactNodeCatalog.Nodes entry, gated to serpentLevel milestones). Both lists are built
    // once and just refreshed thereafter, unlike BuildingsMenuPanel's cove-gated incremental
    // build — every tier/node is always relevant here regardless of cove progress (Artifacts is
    // account-wide, not per-cove).
    public class ArtifactsMenuPanel : MonoBehaviour
    {
        [SerializeField] private Transform shardListContainer;
        [SerializeField] private ShardStackItemUI shardItemPrefab;
        [SerializeField] private Transform nodeListContainer;
        [SerializeField] private ArtifactNodeUI nodeItemPrefab;

        private readonly List<ShardStackItemUI> _shardItems = new List<ShardStackItemUI>();
        private readonly List<ArtifactNodeUI> _nodeItems = new List<ArtifactNodeUI>();

        private void OnEnable()
        {
            if (_shardItems.Count == 0) BuildLists();
            GameManager.Instance.OnStateChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= RefreshAll;
        }

        private void BuildLists()
        {
            foreach (var tier in RarityTier.All)
            {
                var item = Instantiate(shardItemPrefab, shardListContainer);
                item.Bind(tier);
                _shardItems.Add(item);
            }

            foreach (var def in ArtifactNodeCatalog.Nodes)
            {
                var item = Instantiate(nodeItemPrefab, nodeListContainer);
                item.Bind(def.id);
                _nodeItems.Add(item);
            }
        }

        private void RefreshAll()
        {
            foreach (var item in _shardItems) item.Refresh();
            foreach (var item in _nodeItems) item.Refresh();
        }
    }
}
