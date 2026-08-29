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
    // Builds Tide Pools (Wreck Beach cove index 1, CORE_PROGRESSION_RESTRUCTURE.md's 4-cove
    // table) the same way LandingCoveBuilder.cs builds cove 0: world-space clusters with depth
    // layering (smaller + higher = background, larger + lower = foreground), placed via
    // normalized image-space anchors (u = left-to-right, v = top-to-bottom, both 0-1) matched
    // by eye against this cove's actual background art (BackgroundSpritePath) — not a generic
    // formula. UNITY_SETUP.md's "no scene content exists for coves 1-3 yet ... the same
    // 4-cluster method applies once each is validated" is what this fulfills for cove 1.
    //
    // Three clusters instead of Landing Cove's four — there's no Dock/Tuggy equivalent here
    // (Tuggy is anchored to Landing Cove's Dock specifically, per HUD_AND_LANDING_COVE_LAYOUT.md
    // §D):
    //   Grove     - midground sand/palm terrain: a placeholder crew-recruit spot (no crew
    //               member is defined for this cove yet in CrewCatalog.cs, so it's an unwired
    //               placeholder only - same "placed but not yet functional" treatment as
    //               Landing Cove's roaming hermit crab). No Cove Building here yet either
    //               (CoveBuildingCatalog.cs) - CORE_PROGRESSION_RESTRUCTURE.md leaves each
    //               cove's building concept "TBD, not needed until those coves are actually
    //               built"; add one for Tide Pools once designed, same
    //               CoveBuildingVisual/GameManager plumbing LandingCoveBuilder's Hut uses.
    //   TidePools - the rocky tide-pool basin dominating the art's foreground: three
    //               placeholder interaction points for the not-yet-built Tidepooling minigame
    //               (GAME_DESIGN.md/UNITY_SETUP.md - no backing system exists yet, so like the
    //               crew spot these are visual + collider only, no behavior script).
    //   Frontier  - the mini-boss trigger (real system, reused as-is), isolated on the art's
    //               rocky right edge near the mountain, same "stands apart" isolation Landing
    //               Cove's Frontier used.
    //
    // All anchors below are placeholder-precision (matched against the flat PNG, not a
    // rendered/playtested scene) — nudge them once this cove is actually visible in Play mode,
    // same follow-up Landing Cove's own anchors went through.
    public static class TidePoolsBuilder
    {
        private const string RootName = "TidePools";

        private const int CoveIndex = 1; // WreckBeachData.CoveNames[1] == "Tide Pools"
        private const float CoveScreenWidth = 5.625f; // must match CoveViewCamera.coveScreenWidth
        private const float HalfWidth = CoveScreenWidth / 2f;

        // 192x344px, imported at 34.133333 px/unit (Assets/Art/.../tidepool1.png.meta) — same
        // footprint and import settings as Landing Cove's background, so it tiles into the
        // adjacent cove "page" (CoveViewCamera pages coves at coveIndex * coveScreenWidth) with
        // the exact same width/height as cove 0's.
        private const string BackgroundSpritePath = "Assets/Art/Backgrounds/TidePools/tidepool1.png";
        private const float BackgroundWidth = HalfWidth * 2f;
        private const float BackgroundHeight = 344f / 34.133333f;

        // Anchors matched by eye against tidepool1.png: sky/clouds top ~0-0.32, twin-peaked
        // mountain backdrop ~0.20-0.55, a sandy dune across the middle band (~0.45-0.75) with
        // three palm-tree/rock clusters (left ~u=0.10-0.30, center ~u=0.40-0.55, right
        // ~u=0.68-0.90), and a large dark rock-pool basin dominating the bottom band
        // (~v=0.72-0.92) right above the waterline.

        // Open sand between the center and right palm clusters - placeholder for a future
        // recruit (Lucette, GAME_DESIGN.md's old "Slow tidepool collector" — not in
        // CrewCatalog.cs yet, so left unwired).
        private static readonly Vector2 CrewSpotAnchor = new Vector2(0.62f, 0.68f);
        // Spread across the rock-pool basin, symmetric around u=0.50 like Landing Cove's
        // driftwood row.
        private static readonly Vector2[] TidePoolAnchors =
        {
            new Vector2(0.28f, 0.82f),
            new Vector2(0.50f, 0.86f),
            new Vector2(0.70f, 0.82f),
        };
        // Rocks at the frame's right edge, toward the mountain - isolated from the Grove/
        // TidePools clusters the same way Landing Cove's Frontier stands apart from its Camp.
        private static readonly Vector2 SerpentAnchor = new Vector2(0.87f, 0.60f);

        private static Vector2 AnchorToWorld(Vector2 anchor) => new Vector2(
            Mathf.Lerp(-BackgroundWidth / 2f, BackgroundWidth / 2f, anchor.x),
            Mathf.Lerp(BackgroundHeight / 2f, -BackgroundHeight / 2f, anchor.y)); // v=0 is the top of the image (+Y)

        [MenuItem("Rubrehose/Build Tide Pools")]
        public static void Build()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Tide Pools",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks " +
                    "(hand-placed art, adjusted positions) will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
                Undo.DestroyObjectImmediate(existing);
            }

            Undo.SetCurrentGroupName("Build Tide Pools");
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Tide Pools");
            // Pages into the second cove "slot" — CoveViewCamera pans to
            // CurrentCoveIndex * coveScreenWidth, so this cove's whole hierarchy sits offset
            // by one screen-width on X; every child below is still positioned via localPosition
            // off this root, same anchor math as Landing Cove (whose root sits at cove 0 = x=0).
            root.transform.position = new Vector3(CoveIndex * CoveScreenWidth, 0f, 0f);

            // Every anchor-placed object that gets a collider, so each one's hitbox can be
            // capped against its single closest neighbor — see CapColliderToNearestNeighbor.
            var clickablePositions = new List<Vector2>
            {
                AnchorToWorld(CrewSpotAnchor),
                AnchorToWorld(TidePoolAnchors[0]),
                AnchorToWorld(TidePoolAnchors[1]),
                AnchorToWorld(TidePoolAnchors[2]),
                AnchorToWorld(SerpentAnchor),
            };

            BuildBackground(root.transform);
            BuildGroveCluster(root.transform, clickablePositions);
            BuildTidePoolCluster(root.transform, clickablePositions);
            BuildFrontierCluster(root.transform, clickablePositions);

            EnsureEventSystem(); // harmless if a Canvas already added one; OnMouseDown itself doesn't need it
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("TidePoolsBuilder: built '" + RootName + "' at cove index " + CoveIndex + ". Grove's crew " +
                       "spot and the three TidePool interaction points are unwired placeholders — no Tidepooling " +
                       "minigame or Tide-Pools crew member exists yet to wire them to. Run " +
                       "Rubrehose > Build Persistent UI > Fight Overlay afterward (or it'll already be wired if " +
                       "it was run before) so this cove's Serpent gets an HP bar/timer too.");
        }

        // Real background art (not a placeholder primitive) — sits directly on the TidePools
        // root, not inside any cluster, and at the lowest sortingOrder so it always renders
        // behind every cluster regardless of their own sortingOrder values.
        private static void BuildBackground(Transform parent)
        {
            var background = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(background, "Build Tide Pools");
            background.transform.SetParent(parent, false);
            background.transform.localPosition = Vector3.zero;

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            renderer.sortingOrder = -1;

            if (renderer.sprite == null)
            {
                Debug.LogWarning("TidePoolsBuilder: couldn't load background sprite at '" + BackgroundSpritePath +
                                  "' — check its import settings (Texture Type must be 'Sprite (2D and UI)').");
            }
        }

        private static void BuildGroveCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Grove");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Tide Pools");
            cluster.transform.SetParent(parent, false);

            // No Cove Building placed here yet — CoveBuildingCatalog.cs only defines Landing
            // Cove's Hut so far (see this file's class comment). Add one for Tide Pools the
            // same way once its concept is designed: CreateWorldSprite + AddFittedBoxCollider2D
            // + a CoveBuildingVisual with buildingId matching a new CoveBuildingCatalog entry,
            // same as LandingCoveBuilder.BuildCampCluster's Hut.

            // Placeholder only: no crew member is defined for Tide Pools in CrewCatalog.cs yet
            // (only bbw/bbc exist, both Wreck-Beach-general per GAME_DESIGN.md), so this is a
            // visual + collider placeholder with no CrewRecruitSpot/CrewHomeSpotAnimator
            // attached — same treatment LandingCoveBuilder gives its roaming hermit crab
            // (placed, but nothing wired behind it yet). Wire it up once a real crew
            // definition exists to recruit here.
            Vector2 crewSpotPos = AnchorToWorld(CrewSpotAnchor);
            var crewSpot = CreateWorldSprite("CrewSpot", cluster.transform, crewSpotPos, 1f, SquareSprite(), PlaceholderThumbColor, 2);
            var crewSpotCollider = AddFittedBoxCollider2D(crewSpot);
            CapColliderToNearestNeighbor(crewSpotCollider, crewSpotPos, clickablePositions, crewSpot.transform.localScale.x);
        }

        // Placeholder only: Tidepooling has no minigame implementation yet (UNITY_SETUP.md
        // "Tidepooling/Foraging as real minigames ... don't exist yet"), so these are visual +
        // collider placeholders with no behavior script, ready for a future TidepoolSpot-style
        // component the same way the driftwood pieces got DriftwoodPiece once that system
        // existed.
        private static void BuildTidePoolCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("TidePools");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Tide Pools");
            cluster.transform.SetParent(parent, false);

            for (int i = 0; i < TidePoolAnchors.Length; i++)
            {
                Vector2 worldPos = AnchorToWorld(TidePoolAnchors[i]);
                var spot = CreateWorldSprite($"TidePool_{i + 1}", cluster.transform, worldPos, 1f, CircleSprite(), PlaceholderThumbColor, 2);
                var collider = AddFittedBoxCollider2D(spot);
                CapColliderToNearestNeighbor(collider, worldPos, clickablePositions, spot.transform.localScale.x);
            }
        }

        // Builds the serpent itself, same pattern as LandingCoveBuilder.BuildFrontierCluster —
        // FightController and SerpentVisual live directly on it; its overlay UI (HP bar/timer)
        // is built separately by FightOverlayBuilder, which now wires every FightController in
        // the scene (not just one), so this cove's serpent gets the same overlay Landing
        // Cove's does.
        private static void BuildFrontierCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Frontier");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Tide Pools");
            cluster.transform.SetParent(parent, false);

            Vector2 serpentPos = AnchorToWorld(SerpentAnchor);
            // White/no tint: the serpent is a character under GAME_DESIGN.md's locked art
            // direction (monochrome cast), same rule LandingCoveBuilder applies.
            var serpent = CreateWorldSprite("Serpent", cluster.transform, serpentPos, 1f, SquareSprite(), Color.white, 2);
            var serpentCollider = AddFittedBoxCollider2D(serpent);
            CapColliderToNearestNeighbor(serpentCollider, serpentPos, clickablePositions, serpent.transform.localScale.x);

            var serpentVisual = serpent.AddComponent<SerpentVisual>();
            var serpentVisualSo = new SerializedObject(serpentVisual);
            serpentVisualSo.FindProperty("spriteRenderer").objectReferenceValue = serpent.GetComponent<SpriteRenderer>();
            serpentVisualSo.ApplyModifiedProperties();

            var fightController = serpent.AddComponent<FightController>();
            var fightControllerSo = new SerializedObject(fightController);
            fightControllerSo.FindProperty("serpentVisual").objectReferenceValue = serpentVisual;
            fightControllerSo.ApplyModifiedProperties();
        }

        // Same neighbor-spacing safety cap as LandingCoveBuilder's — caps each object's
        // collider to 80% of the distance to its single closest neighbor among every other
        // clickable object in this cove, so no two hitboxes can ever overlap.
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
