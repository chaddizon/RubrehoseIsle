using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubrehose.World;
using static Rubrehose.EditorTools.RubrehoseEditorUtils;

namespace Rubrehose.EditorTools
{
    // Builds Landing Cove's four clusters (HUD_AND_LANDING_COVE_LAYOUT.md §B) as world-space
    // placeholder objects — Dock, Shoreline, Camp, Frontier — with depth layering (smaller +
    // higher = background, larger + lower = foreground) via scale and sorting order.
    //
    // World-unit zone math assumes cove 0 sits at world X=0 (CoveViewCamera.Awake) with the
    // camera's default orthographic size 5 and coveScreenWidth 5.625 (both CoveViewCamera
    // fields) — if either changes, these placements go stale along with it. 33/25/42% water/
    // beach/island horizontally, waterline at the bottom-30%-of-height mark, per
    // CAMERA_AND_UI_SPEC.md's locked per-cove composition.
    public static class LandingCoveBuilder
    {
        private const string RootName = "LandingCove";

        private const float HalfWidth = 2.8125f; // coveScreenWidth/2 (5.625/2)
        private const float HalfHeight = 5f;     // orthographic size
        private const float WaterlineY = -2f;    // -HalfHeight + 0.3 * (2*HalfHeight)

        // Zone boundaries (world X), left to right: water / beach / island.
        private const float WaterMinX = -HalfWidth;
        private const float BeachMinX = -HalfWidth + 0.33f * (2f * HalfWidth); // -0.956
        private const float IslandMinX = BeachMinX + 0.25f * (2f * HalfWidth); // 0.449
        private const float IslandMaxX = HalfWidth;

        private static readonly string[] DriftwoodSpritePaths =
        {
            "Assets/Art/WorldObjects/Driftwood/driftwood_pixel_1.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood_pixel_2.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood_pixel_3.png",
        };

        [MenuItem("Rubrehose/Build Landing Cove")]
        public static void Build()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Landing Cove",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks " +
                    "(hand-placed art, adjusted positions) will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
                Undo.DestroyObjectImmediate(existing);
            }

            Undo.SetCurrentGroupName("Build Landing Cove");
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Landing Cove");

            BuildDockCluster(root.transform);
            BuildShorelineCluster(root.transform);
            BuildCampCluster(root.transform);
            BuildFrontierCluster(root.transform);
            BuildRoamingCritter(root.transform);

            EnsureEventSystem(); // harmless if a Canvas already added one; OnMouseDown itself doesn't need it
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("LandingCoveBuilder: built '" + RootName + "'. Placeholder primitive sprites throughout — " +
                       "swap each SpriteRenderer's sprite for real art, same drag-and-drop pattern as everywhere else.");
        }

        private static void BuildDockCluster(Transform parent)
        {
            var cluster = new GameObject("Dock");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Background plane — smaller scale, positioned higher, implying distance across the water.
            float x = Mathf.Lerp(WaterMinX, BeachMinX, 0.35f);
            CreateWorldSprite("Tuggy", cluster.transform, new Vector2(x, -1.2f), 0.7f, SquareSprite(), InkColor, 0);
        }

        private static void BuildShorelineCluster(Transform parent)
        {
            var cluster = new GameObject("Shoreline");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Foreground plane — larger scale, low on screen, closest to "camera".
            float[] driftwoodX = { Mathf.Lerp(BeachMinX, IslandMinX, 0.2f), Mathf.Lerp(BeachMinX, IslandMinX, 0.5f), Mathf.Lerp(BeachMinX, IslandMinX, 0.8f) };
            for (int i = 0; i < driftwoodX.Length; i++)
            {
                var driftwoodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DriftwoodSpritePaths[i % DriftwoodSpritePaths.Length]);
                var piece = CreateWorldSprite($"Driftwood_{i + 1}", cluster.transform,
                    new Vector2(driftwoodX[i], -1.7f + (i % 2 == 0 ? 0f : -0.1f)), 1f, driftwoodSprite, Color.white, 2);
                piece.AddComponent<BoxCollider2D>();
                piece.AddComponent<TappableDriftwood>();
            }

            var bottle = CreateWorldSprite("BottleCastPoint", cluster.transform,
                new Vector2(Mathf.Lerp(BeachMinX, IslandMinX, 0.95f), -1.9f), 0.8f, CircleSprite(), TealAccent, 2);
            bottle.AddComponent<BoxCollider2D>();
            // No CastBottle script yet — Message in a Bottle isn't implemented in Unity yet (WRECK_BEACH_CHECKLIST.md).
        }

        private static void BuildCampCluster(Transform parent)
        {
            var cluster = new GameObject("Camp");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Left portion of the island zone. Three depth planes: hut back, campfire mid, BBW+BBC front.
            float campX = Mathf.Lerp(IslandMinX, IslandMaxX, 0.2f);

            var hut = CreateWorldSprite("Hut", cluster.transform, new Vector2(campX, 1.5f), 0.6f, SquareSprite(), PlaceholderThumbColor, 0);
            var hutState = hut.AddComponent<HutConstructionState>();
            var hutSo = new SerializedObject(hutState);
            hutSo.FindProperty("spriteRenderer").objectReferenceValue = hut.GetComponent<SpriteRenderer>();
            hutSo.ApplyModifiedProperties();

            CreateWorldSprite("Campfire", cluster.transform, new Vector2(campX + 0.2f, 0.3f), 0.8f, CircleSprite(), TealAccent, 1);

            var bbw = CreateWorldSprite("BBWHomeSpot", cluster.transform, new Vector2(campX - 0.1f, -1.3f), 1.1f, SquareSprite(), InkColor, 2);
            bbw.AddComponent<BoxCollider2D>();
            var bbwSpot = bbw.AddComponent<CrewRecruitSpot>();
            ApplyStringField(bbwSpot, "crewId", "bbw");

            var bbc = CreateWorldSprite("BBCHomeSpot", cluster.transform, new Vector2(campX + 0.4f, -1.3f), 1.1f, SquareSprite(), InkColor, 2);
            bbc.AddComponent<BoxCollider2D>();
            var bbcSpot = bbc.AddComponent<CrewRecruitSpot>();
            ApplyStringField(bbcSpot, "crewId", "bbc");
        }

        private static void BuildFrontierCluster(Transform parent)
        {
            var cluster = new GameObject("Frontier");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Far-right edge, deliberately isolated from Camp.
            float x = Mathf.Lerp(IslandMinX, IslandMaxX, 0.92f);
            var trigger = CreateWorldSprite("MiniBossTrigger", cluster.transform, new Vector2(x, -1f), 1f, SquareSprite(), PurpleAccent, 2);
            trigger.AddComponent<BoxCollider2D>();
            trigger.AddComponent<MiniBossTrigger>();
            // fightController left unassigned — wire it to the scene's FightController once the fight modal (UNITY_SETUP.md §4) exists.
        }

        private static void BuildRoamingCritter(Transform parent)
        {
            // Placeholder position only — scripted roaming path (Shoreline <-> Camp) isn't implemented yet.
            float x = Mathf.Lerp(BeachMinX, IslandMinX, 0.6f);
            var critter = CreateWorldSprite("HermitCrab", parent, new Vector2(x, -1.7f), 0.6f, CircleSprite(), PlaceholderThumbColor, 2);
            critter.AddComponent<BoxCollider2D>();
        }

        private static void ApplyStringField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).stringValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
