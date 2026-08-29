using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubrehose.Combat;
using Rubrehose.World;
using static Rubrehose.EditorTools.RubrehoseEditorUtils;

namespace Rubrehose.EditorTools
{
    // Builds The Grove (Wreck Beach cove index 2, CORE_PROGRESSION_RESTRUCTURE.md's 4-cove
    // table) the same way LandingCoveBuilder.cs/TidePoolsBuilder.cs build coves 0/1: world-space
    // clusters with depth layering, placed via normalized image-space anchors (u = left-to-right,
    // v = top-to-bottom, both 0-1) matched by eye against grove1.png — not a generic formula.
    // NEXT_CLAUDE_CODE_PUSH.md §5 is what this fulfills.
    //
    // Two clusters, same "no Dock here" reasoning as Tide Pools:
    //   Canopy    - the dense palm/fern foliage dominating the lower two-thirds of the frame:
    //               three placeholder interaction points for the not-yet-built Foraging
    //               minigame, and a placeholder crew-recruit spot (no crew member defined for
    //               this cove yet) - same "placed but not yet functional" treatment Tide
    //               Pools' equivalents got. Deliberately sparse per NEXT_CLAUDE_CODE_PUSH.md's
    //               composition note ("the canopy is dense edge-to-edge... avoid stacking new
    //               interactive objects there") - only these 4 objects total, not spread
    //               liberally through the crowded foliage.
    //   Frontier  - the mini-boss trigger (real system, reused as-is), isolated at the
    //               bottom-right shore/rocks, away from both the Canopy cluster and the
    //               upper-left open rocky flank (deliberately left clear here - that's where
    //               this cove's Artifacts tell sprite(s) will go once the Artifacts system
    //               lands, per NEXT_CLAUDE_CODE_PUSH.md §5's composition note; not placed by
    //               this builder).
    //
    // All anchors below are placeholder-precision (matched against the flat PNG, not a
    // rendered/playtested scene) — nudge them once this cove is actually visible in Play mode,
    // same follow-up Landing Cove's and Tide Pools' own anchors already went through.
    public static class GroveBuilder
    {
        private const string RootName = "Grove";

        private const int CoveIndex = 2; // WreckBeachData.CoveNames[2] == "The Grove"
        private const float CoveScreenWidth = 5.625f; // must match CoveViewCamera.coveScreenWidth

        // 192x344px, imported at 34.133333 px/unit (Assets/Art/.../grove1.png.meta) — same
        // footprint and import settings as every other cove's background, so it tiles into the
        // third cove "page" (CoveViewCamera pages coves at coveIndex * coveScreenWidth) with the
        // exact same width/height as coves 0-1's.
        private const string BackgroundSpritePath = "Assets/Art/Backgrounds/Grove/grove1.png";
        private const float BackgroundWidth = CoveScreenWidth;
        private const float BackgroundHeight = 344f / 34.133333f;

        // Anchors matched by eye against grove1.png: sky top ~0-0.20, a single rocky mountain
        // peak upper-center-left (~u=0.10-0.70, v=0.05-0.45) with its flank exposed against flat
        // sky at upper-left (~u=0.05-0.30, v=0.10-0.35 - the one open area, deliberately left
        // clear, see class comment), dense edge-to-edge palm/fern canopy through the middle-to-
        // lower two-thirds (~v=0.35-0.90) with a few rock clusters breaking through it, and a
        // narrow sandy shore strip at the very bottom (~v=0.88-1.0).

        // Sparse and spread rather than clustered, per the "avoid stacking" composition note -
        // left near a rock outcrop, center in a foliage gap, right near the mid-right rocks.
        private static readonly Vector2[] CanopyAnchors =
        {
            new Vector2(0.28f, 0.70f),
            new Vector2(0.50f, 0.78f),
            new Vector2(0.70f, 0.66f),
        };
        // Open sand on the shore strip - placeholder for a future recruit (no crew member
        // defined for this cove yet in CrewCatalog.cs).
        private static readonly Vector2 CrewSpotAnchor = new Vector2(0.42f, 0.88f);
        // Bottom-right shore/rocks - isolated from Canopy and clear of the reserved upper-left
        // Artifacts-tell area.
        private static readonly Vector2 SerpentAnchor = new Vector2(0.90f, 0.80f);

        // Artifacts tell spots (NEXT_CLAUDE_CODE_PUSH.md §1a and §5's composition note) — the
        // one genuinely open area in this cove's art, the mountain's exposed rocky flank
        // against flat sky, upper-left.
        private static readonly Vector2[] TellAnchors =
        {
            new Vector2(0.12f, 0.18f),
            new Vector2(0.24f, 0.28f),
        };

        private static Vector2 AnchorToWorld(Vector2 anchor) => new Vector2(
            Mathf.Lerp(-BackgroundWidth / 2f, BackgroundWidth / 2f, anchor.x),
            Mathf.Lerp(BackgroundHeight / 2f, -BackgroundHeight / 2f, anchor.y)); // v=0 is the top of the image (+Y)

        [MenuItem("Rubrehose/Build The Grove")]
        public static void Build()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild The Grove",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks " +
                    "(hand-placed art, adjusted positions) will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
                Undo.DestroyObjectImmediate(existing);
            }

            Undo.SetCurrentGroupName("Build The Grove");
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build The Grove");
            // Pages into the third cove "slot" — CoveViewCamera pans to
            // CurrentCoveIndex * coveScreenWidth, so this cove's whole hierarchy sits offset by
            // two screen-widths on X; every child below is still positioned via localPosition
            // off this root, same anchor math as Landing Cove/Tide Pools.
            root.transform.position = new Vector3(CoveIndex * CoveScreenWidth, 0f, 0f);

            // Every anchor-placed object that gets a collider, so each one's hitbox can be
            // capped against its single closest neighbor — see CapColliderToNearestNeighbor.
            var clickablePositions = new List<Vector2>
            {
                AnchorToWorld(CanopyAnchors[0]),
                AnchorToWorld(CanopyAnchors[1]),
                AnchorToWorld(CanopyAnchors[2]),
                AnchorToWorld(CrewSpotAnchor),
                AnchorToWorld(SerpentAnchor),
                AnchorToWorld(TellAnchors[0]),
                AnchorToWorld(TellAnchors[1]),
            };

            BuildBackground(root.transform);
            BuildCanopyCluster(root.transform, clickablePositions);
            BuildFrontierCluster(root.transform, clickablePositions);
            BuildArtifactsCluster(root.transform, clickablePositions);

            EnsureEventSystem(); // harmless if a Canvas already added one; OnMouseDown itself doesn't need it
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("GroveBuilder: built '" + RootName + "' at cove index " + CoveIndex + ". Canopy's crew spot " +
                       "and the three Foraging interaction points are unwired placeholders — no Foraging minigame " +
                       "or Grove crew member exists yet to wire them to. Run " +
                       "Rubrehose > Build Persistent UI > Fight Overlay afterward (or it'll already be wired if " +
                       "it was run before) so this cove's Serpent gets an HP bar/timer too.");
        }

        // Real background art (not a placeholder primitive) — sits directly on the Grove root,
        // not inside any cluster, and at the lowest sortingOrder so it always renders behind
        // every cluster regardless of their own sortingOrder values.
        private static void BuildBackground(Transform parent)
        {
            var background = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(background, "Build The Grove");
            background.transform.SetParent(parent, false);
            background.transform.localPosition = Vector3.zero;

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            renderer.sortingOrder = -1;

            if (renderer.sprite == null)
            {
                Debug.LogWarning("GroveBuilder: couldn't load background sprite at '" + BackgroundSpritePath +
                                  "' — check its import settings (Texture Type must be 'Sprite (2D and UI)').");
            }
        }

        // Placeholder only: Foraging has no minigame implementation yet, and no crew member is
        // defined for this cove in CrewCatalog.cs — visual + collider placeholders with no
        // behavior script, ready for future ForagingSpot/CrewRecruitSpot-style components the
        // same way the driftwood pieces got DriftwoodPiece once that system existed.
        private static void BuildCanopyCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Canopy");
            Undo.RegisterCreatedObjectUndo(cluster, "Build The Grove");
            cluster.transform.SetParent(parent, false);

            for (int i = 0; i < CanopyAnchors.Length; i++)
            {
                Vector2 worldPos = AnchorToWorld(CanopyAnchors[i]);
                var spot = CreateWorldSprite($"Foraging_{i + 1}", cluster.transform, worldPos, 1f, CircleSprite(), PlaceholderThumbColor, 2);
                var collider = AddFittedBoxCollider2D(spot);
                CapColliderToNearestNeighbor(collider, worldPos, clickablePositions, spot.transform.localScale.x);
            }

            Vector2 crewSpotPos = AnchorToWorld(CrewSpotAnchor);
            var crewSpot = CreateWorldSprite("CrewSpot", cluster.transform, crewSpotPos, 1f, SquareSprite(), PlaceholderThumbColor, 2);
            var crewSpotCollider = AddFittedBoxCollider2D(crewSpot);
            CapColliderToNearestNeighbor(crewSpotCollider, crewSpotPos, clickablePositions, crewSpot.transform.localScale.x);
        }

        // Builds the serpent itself, same pattern as LandingCoveBuilder/TidePoolsBuilder's
        // Frontier cluster — FightController and SerpentVisual live directly on it; its overlay
        // UI (HP bar/timer) is built separately by FightOverlayBuilder, which wires every
        // FightController in the scene, so this cove's serpent gets the same shared overlay.
        private static void BuildFrontierCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Frontier");
            Undo.RegisterCreatedObjectUndo(cluster, "Build The Grove");
            cluster.transform.SetParent(parent, false);

            Vector2 serpentPos = AnchorToWorld(SerpentAnchor);
            // White/no tint: the serpent is a character under GAME_DESIGN.md's locked art
            // direction (monochrome cast), same rule every other cove's Frontier follows.
            var serpent = CreateWorldSprite("Serpent", cluster.transform, serpentPos, 1f, SquareSprite(), Color.white, 2);
            var serpentCollider = AddFittedBoxCollider2D(serpent);
            CapColliderToNearestNeighbor(serpentCollider, serpentPos, clickablePositions, serpent.transform.localScale.x);

            var serpentVisual = serpent.AddComponent<SerpentVisual>();
            var serpentVisualSo = new SerializedObject(serpentVisual);
            serpentVisualSo.FindProperty("coveIndex").intValue = CoveIndex;
            serpentVisualSo.FindProperty("spriteRenderer").objectReferenceValue = serpent.GetComponent<SpriteRenderer>();
            serpentVisualSo.ApplyModifiedProperties();

            var fightController = serpent.AddComponent<FightController>();
            var fightControllerSo = new SerializedObject(fightController);
            fightControllerSo.FindProperty("coveIndex").intValue = CoveIndex;
            fightControllerSo.FindProperty("serpentVisual").objectReferenceValue = serpentVisual;
            fightControllerSo.ApplyModifiedProperties();
        }

        // Artifacts tell spots (NEXT_CLAUDE_CODE_PUSH.md §1a) — same pattern as
        // LandingCoveBuilder.BuildArtifactsCluster/TidePoolsBuilder.BuildArtifactsCluster.
        private static void BuildArtifactsCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Artifacts");
            Undo.RegisterCreatedObjectUndo(cluster, "Build The Grove");
            cluster.transform.SetParent(parent, false);

            var tells = new TellSpot[TellAnchors.Length];
            for (int i = 0; i < TellAnchors.Length; i++)
            {
                Vector2 pos = AnchorToWorld(TellAnchors[i]);
                var spot = CreateWorldSprite($"Tell_{i + 1}", cluster.transform, pos, 0.6f, CircleSprite(), PlaceholderThumbColor, 1);
                var collider = AddFittedBoxCollider2D(spot);
                CapColliderToNearestNeighbor(collider, pos, clickablePositions, spot.transform.localScale.x);

                var glint = CreateWorldSprite("LiveGlint", spot.transform, Vector2.zero, 1.2f, SquareSprite(), TealAccent, 2, showDebugLabel: false);
                glint.SetActive(false);

                var tellSpot = spot.AddComponent<TellSpot>();
                var tellSo = new SerializedObject(tellSpot);
                tellSo.FindProperty("liveGlintOverlay").objectReferenceValue = glint;
                tellSo.ApplyModifiedProperties();
                tells[i] = tellSpot;
            }

            var spawner = cluster.AddComponent<TellSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("coveIndex").intValue = CoveIndex;
            var tellsProp = spawnerSo.FindProperty("tells");
            tellsProp.arraySize = tells.Length;
            for (int i = 0; i < tells.Length; i++) tellsProp.GetArrayElementAtIndex(i).objectReferenceValue = tells[i];
            spawnerSo.ApplyModifiedProperties();
        }

        // Same neighbor-spacing safety cap as LandingCoveBuilder's/TidePoolsBuilder's — caps
        // each object's collider to 80% of the distance to its single closest neighbor among
        // every other clickable object in this cove, so no two hitboxes can ever overlap.
        private static void CapColliderToNearestNeighbor(BoxCollider2D collider, Vector2 worldPos, List<Vector2> clickablePositions, float objectScale = 1f)
        {
            float nearest = float.MaxValue;
            foreach (var other in clickablePositions)
            {
                float d = Vector2.Distance(worldPos, other);
                if (d > 0.001f && d < nearest) nearest = d;
            }
            if (nearest == float.MaxValue) return; // no other clickable object in this cove

            float safeLocalSize = (nearest * 0.8f) / objectScale;
            collider.size = new Vector2(Mathf.Min(collider.size.x, safeLocalSize), Mathf.Min(collider.size.y, safeLocalSize));
        }
    }
}
