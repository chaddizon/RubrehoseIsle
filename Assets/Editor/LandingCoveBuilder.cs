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
    // Builds Landing Cove's four clusters (HUD_AND_LANDING_COVE_LAYOUT.md §B) as world-space
    // objects — Dock, Shoreline, Camp, Frontier — with depth layering (smaller + higher =
    // background, larger + lower = foreground) via scale and sorting order.
    //
    // Every object is placed via a normalized image-space anchor (u = left-to-right,
    // v = top-to-bottom, both 0-1) matched by eye against Landing Cove's real background art
    // (BackgroundSpritePath) — its actual sand/water/rock layout, not a generic formula. The
    // old water/beach/island percentage-zone system (still describable in world-unit terms:
    // 33/25/42% water/beach/island horizontally per CAMERA_AND_UI_SPEC.md) didn't line up with
    // this specific artwork and was dropping objects in the sky/water.
    public static class LandingCoveBuilder
    {
        private const string RootName = "LandingCove";

        private const float HalfWidth = 2.8125f; // coveScreenWidth/2 (5.625/2) — CoveViewCamera

        private static readonly string[] DriftwoodSpritePaths =
        {
            "Assets/Art/WorldObjects/Driftwood/driftwood1.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood2.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood3.png",
        };

        // 128x96px each, imported at 100px/unit (default) so they render at their true
        // 1.28x0.96-unit footprint — unlike driftwood, these were authored to display at
        // exactly this pixel size, not exported @2x for a smaller intended footprint.
        private const string HutRubbleSpritePath = "Assets/Art/WorldObjects/Hut/hut_stage1_rubble.png";
        private const string HutHalfBuiltSpritePath = "Assets/Art/WorldObjects/Hut/hut_stage2_halfbuilt.png";
        private const string HutCompleteSpritePath = "Assets/Art/WorldObjects/Hut/hut_stage3_complete.png";

        // 64x56px each, imported at 100px/unit (default) so they render at their true
        // 0.64x0.56-unit footprint. Content bounds are near-identical across all four frames
        // (unlike Hut's half-built stage), so LoopingFrameAnimator needs no per-frame offset.
        private static readonly string[] CampfireSpritePaths =
        {
            "Assets/Art/WorldObjects/Campfire/campfire1.png",
            "Assets/Art/WorldObjects/Campfire/campfire2.png",
            "Assets/Art/WorldObjects/Campfire/campfire3.png",
            "Assets/Art/WorldObjects/Campfire/campfire4.png",
        };

        // 128x128px (final art came out square, not the originally-planned 160x112),
        // imported at 100px/unit (default) so it renders at its true 1.28x1.28-unit
        // footprint.
        private const string TuggySpritePath = "Assets/Art/Characters/Tuggy/tuggy.png";

        // 38x48px, imported at 100px/unit (default) so it renders at its true 0.38x0.48-unit
        // footprint. Monochrome — counts as a character under GAME_DESIGN.md's locked
        // art-direction rule, so leave its color untouched (no tint) rather than recoloring.
        private const string BottleSpritePath = "Assets/Art/WorldObjects/BottleCastPoint/bottle.png";

        // 48x64px each, imported at 100px/unit (default) so they render at their true
        // 0.48x0.64-unit footprint. Content bounds are consistent across all five frames, so
        // LoopingFrameAnimator needs no per-frame offset.
        private static readonly string[] BottleFlagPoleSpritePaths =
        {
            "Assets/Art/WorldObjects/BottleCastPoint/bottlecastpointmarker1.png",
            "Assets/Art/WorldObjects/BottleCastPoint/bottlecastpointmarker2.png",
            "Assets/Art/WorldObjects/BottleCastPoint/bottlecastpointmarker3.png",
            "Assets/Art/WorldObjects/BottleCastPoint/bottlecastpointmarker4.png",
            "Assets/Art/WorldObjects/BottleCastPoint/bottlecastpointmarker5.png",
        };

        // 86x140px each (properly-proportioned replacement for the original square set),
        // imported at 100px/unit (default) so they render at their true 0.86x1.4-unit
        // footprint. Monochrome — counts as a character under GAME_DESIGN.md's locked
        // art-direction rule, so leave color untouched (no tint) rather than recoloring.
        private static readonly string[] BBWIdleSpritePaths =
        {
            "Assets/Art/Characters/BBW/bbwidle1.png",
            "Assets/Art/Characters/BBW/bbwidle2.png",
            "Assets/Art/Characters/BBW/bbwidle3.png",
        };
        private static readonly string[] BBWWorkingSpritePaths =
        {
            "Assets/Art/Characters/BBW/bbwworking1.png",
            "Assets/Art/Characters/BBW/bbwworking2.png",
            "Assets/Art/Characters/BBW/bbwworking3.png",
        };

        // 128x128px, imported at 100px/unit (default) so it renders at its true 1.28x1.28-unit
        // footprint — not part of the idle/working replacement, still the original canvas size.
        private const string BBWTapReactionSpritePath = "Assets/Art/Characters/BBW/bbw_tapreaction.png";

        // 86x140px each (properly-proportioned replacement for the original square set,
        // matching BBW's updated canvas), imported at 100px/unit (default) so they render at
        // their true 0.86x1.4-unit footprint. Monochrome — counts as a character under
        // GAME_DESIGN.md's locked art-direction rule, so leave color untouched (no tint)
        // rather than recoloring.
        private static readonly string[] BBCIdleSpritePaths =
        {
            "Assets/Art/Characters/BBC/bbcwalking1.png",
            "Assets/Art/Characters/BBC/bbcwalking2.png",
            "Assets/Art/Characters/BBC/bbcwalking3.png",
        };
        private static readonly string[] BBCWorkingSpritePaths =
        {
            "Assets/Art/Characters/BBC/bbcworking1.png",
            "Assets/Art/Characters/BBC/bbcworking2.png",
            "Assets/Art/Characters/BBC/bbcworking3.png",
        };

        // 128x128px, imported at 100px/unit (default) so it renders at its true 1.28x1.28-unit
        // footprint — not part of the idle/working replacement, still the original canvas size.
        private const string BBCTapReactionSpritePath = "Assets/Art/Characters/BBC/bbc_tapreaction.png";

        // 192x344px, imported at 34.133333 px/unit (Assets/Art/.../landingcove1_final.png.meta)
        // so it exactly covers the cove's full width (coveScreenWidth) at scale 1, and is a
        // hair taller than the camera's viewport (10.078 vs 2x orthoSize's 10) rather than
        // shorter, so it never leaves a gap at top/bottom. Anchors below map against this
        // actual rendered footprint, not just the camera viewport, so they land exactly where
        // they look right against the art regardless of that small overflow.
        private const string BackgroundSpritePath = "Assets/Art/Backgrounds/WreckBeach/landingcove1_final.png";
        private const float BackgroundWidth = HalfWidth * 2f;
        private const float BackgroundHeight = 344f / 34.133333f;

        // Anchors matched by eye against landingcove1_final.png's actual terrain.
        private static readonly Vector2 TuggyAnchor = new Vector2(0.10f, 0.80f);
        private static readonly Vector2[] DriftwoodAnchors =
        {
            new Vector2(0.46f, 0.76f),
            new Vector2(0.50f, 0.78f),
            new Vector2(0.54f, 0.80f),
        };
        // The flag pole marks the cast spot on the sand (always visible, purely decorative).
        // The actual bottle is a separate object out in the water to the pole's left, hidden
        // until a cast bottle washes back up — initial guess at (0.08, 0.72), not yet visually
        // confirmed against the art the way the other anchors in this file have been.
        private static readonly Vector2 BottleFlagPoleAnchor = new Vector2(0.20f, 0.70f);
        private static readonly Vector2 BottleAnchor = new Vector2(0.13f, 0.72f);

        // Roaming path isn't implemented yet (BuildRoamingCritter placeholder-parks the critter
        // at Start) — End is kept here so it's not lost once that behavior exists.
        private static readonly Vector2 CritterStartAnchor = new Vector2(0.74f, 0.78f);
        private static readonly Vector2 CritterEndAnchor = new Vector2(0.88f, 0.80f);

        private static readonly Vector2 HutAnchor = new Vector2(0.40f, 0.51f);
        private static readonly Vector2 CampfireAnchor = new Vector2(0.50f, 0.58f);
        private static readonly Vector2 BBWHomeSpotAnchor = new Vector2(0.27f, 0.64f);
        private static readonly Vector2 BBCHomeSpotAnchor = new Vector2(0.61f, 0.64f);
        private static readonly Vector2 MiniBossTriggerAnchor = new Vector2(0.88f, 0.58f);

        private static Vector2 AnchorToWorld(Vector2 anchor) => new Vector2(
            Mathf.Lerp(-BackgroundWidth / 2f, BackgroundWidth / 2f, anchor.x),
            Mathf.Lerp(BackgroundHeight / 2f, -BackgroundHeight / 2f, anchor.y)); // v=0 is the top of the image (+Y)

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

            // Every anchor-placed object that gets a collider, computed once up front so each
            // one's hitbox can be capped against its single closest neighbor — these anchors
            // are dictated by where sand/terrain actually is in the art, not evenly spaced for
            // hitbox comfort, so several land close together (e.g. Driftwood 1 and
            // BottleCastPoint are only ~0.2 units apart).
            var clickablePositions = new List<Vector2>
            {
                AnchorToWorld(DriftwoodAnchors[0]),
                AnchorToWorld(DriftwoodAnchors[1]),
                AnchorToWorld(DriftwoodAnchors[2]),
                AnchorToWorld(BottleAnchor), // FlagPoleAnchor deliberately excluded — no collider, purely decorative
                AnchorToWorld(CritterStartAnchor),
                AnchorToWorld(HutAnchor),
                AnchorToWorld(BBWHomeSpotAnchor),
                AnchorToWorld(BBCHomeSpotAnchor),
                AnchorToWorld(MiniBossTriggerAnchor),
            };

            BuildBackground(root.transform);
            BuildDockCluster(root.transform);
            BuildShorelineCluster(root.transform, clickablePositions);
            BuildCampCluster(root.transform, clickablePositions);
            BuildFrontierCluster(root.transform, clickablePositions);
            BuildRoamingCritter(root.transform, clickablePositions);

            EnsureEventSystem(); // harmless if a Canvas already added one; OnMouseDown itself doesn't need it
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("LandingCoveBuilder: built '" + RootName + "'. Placeholder primitive sprites throughout — " +
                       "swap each SpriteRenderer's sprite for real art, same drag-and-drop pattern as everywhere else.");
        }

        // Real background art (not a placeholder primitive) — sits directly on the LandingCove
        // root, not inside any cluster, and at the lowest sortingOrder in the scene so it
        // always renders behind every cluster regardless of their own sortingOrder values.
        private static void BuildBackground(Transform parent)
        {
            var background = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(background, "Build Landing Cove");
            background.transform.SetParent(parent, false);
            background.transform.localPosition = Vector3.zero;

            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            renderer.sortingOrder = -1;

            if (renderer.sprite == null)
            {
                Debug.LogWarning("LandingCoveBuilder: couldn't load background sprite at '" + BackgroundSpritePath +
                                  "' — check its import settings (Texture Type must be 'Sprite (2D and UI)').");
            }
        }

        private static void BuildDockCluster(Transform parent)
        {
            var cluster = new GameObject("Dock");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Background plane, implying distance across the water. No collider: purely
            // decorative, never included in clickablePositions. Scale left at 1 (renders at
            // the art's true footprint) as a reasonable default — visual size still to be
            // tuned once it's actually on screen in Play mode.
            var tuggy = CreateWorldSprite("Tuggy", cluster.transform, AnchorToWorld(TuggyAnchor), 1f,
                AssetDatabase.LoadAssetAtPath<Sprite>(TuggySpritePath), Color.white, 0, showDebugLabel: false);
            tuggy.AddComponent<PixelSnappedBob>();
        }

        private static void BuildShorelineCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Shoreline");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Each piece is a static root (collider + DriftwoodPiece, position fixed) wrapping
            // an animated "Visual" child (sprite only) — keeps the wash-up/collect animation's
            // scale/fade from ever touching the collider, so tap detection can't be affected
            // by animation timing. All three driftwood_pixel variants are wired onto every
            // piece so DriftwoodPiece can randomize which one shows after each collect.
            var driftwoodVariants = System.Array.ConvertAll(DriftwoodSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            for (int i = 0; i < DriftwoodAnchors.Length; i++)
            {
                Vector2 worldPos = AnchorToWorld(DriftwoodAnchors[i]);
                var piece = new GameObject($"Driftwood_{i + 1}");
                Undo.RegisterCreatedObjectUndo(piece, "Build Landing Cove");
                piece.transform.SetParent(cluster.transform, false);
                piece.transform.localPosition = new Vector3(worldPos.x, worldPos.y, 0f);

                var visual = new GameObject("Visual");
                Undo.RegisterCreatedObjectUndo(visual, "Build Landing Cove");
                visual.transform.SetParent(piece.transform, false);
                var visualRenderer = visual.AddComponent<SpriteRenderer>();
                visualRenderer.sprite = driftwoodVariants[i % driftwoodVariants.Length];
                visualRenderer.color = Color.white;
                visualRenderer.sortingOrder = 2;

                var driftwoodCollider = AddFittedBoxCollider2D(piece, visualRenderer);
                CapColliderToNearestNeighbor(driftwoodCollider, worldPos, clickablePositions);

                var driftwoodPiece = piece.AddComponent<DriftwoodPiece>();
                var driftwoodSo = new SerializedObject(driftwoodPiece);
                driftwoodSo.FindProperty("spriteRenderer").objectReferenceValue = visualRenderer;
                var variantsProp = driftwoodSo.FindProperty("variantSprites");
                variantsProp.arraySize = driftwoodVariants.Length;
                for (int v = 0; v < driftwoodVariants.Length; v++)
                {
                    variantsProp.GetArrayElementAtIndex(v).objectReferenceValue = driftwoodVariants[v];
                }
                driftwoodSo.ApplyModifiedProperties();
            }

            // Always-visible, purely decorative — just marks the cast spot on the sand.
            // No collider, never included in clickablePositions.
            var flagPoleFrames = System.Array.ConvertAll(BottleFlagPoleSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var flagPole = CreateWorldSprite("FlagPole", cluster.transform, AnchorToWorld(BottleFlagPoleAnchor), 1f,
                flagPoleFrames[0], Color.white, 2, showDebugLabel: false);
            var flagPoleAnimator = flagPole.AddComponent<LoopingFrameAnimator>();
            var flagPoleSo = new SerializedObject(flagPoleAnimator);
            flagPoleSo.FindProperty("spriteRenderer").objectReferenceValue = flagPole.GetComponent<SpriteRenderer>();
            var flagPoleFramesProp = flagPoleSo.FindProperty("frames");
            flagPoleFramesProp.arraySize = flagPoleFrames.Length;
            for (int f = 0; f < flagPoleFrames.Length; f++)
            {
                flagPoleFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = flagPoleFrames[f];
            }
            // Cloth flutter has bigger frame-to-frame shape changes than the campfire's
            // subtle flicker, so the shared 6-8fps default reads as a violent windstorm here
            // instead of a gentle flutter — override to a slower pace just for this instance.
            flagPoleSo.FindProperty("framesPerSecond").floatValue = 3f;
            flagPoleSo.ApplyModifiedProperties();

            // The actual bottle — separate object out in the water, hidden/untappable until
            // BottleCastPoint.UpdateVisual() enables it on wash-up. Monochrome per
            // GAME_DESIGN.md's locked art direction (bottle counts as a character): white,
            // no tint.
            Vector2 bottlePos = AnchorToWorld(BottleAnchor);
            var bottle = CreateWorldSprite("Bottle", cluster.transform, bottlePos, 1f,
                AssetDatabase.LoadAssetAtPath<Sprite>(BottleSpritePath), Color.white, 2, showDebugLabel: false);
            var bottleRenderer = bottle.GetComponent<SpriteRenderer>();
            var bottleCollider = AddFittedBoxCollider2D(bottle);
            CapColliderToNearestNeighbor(bottleCollider, bottlePos, clickablePositions, bottle.transform.localScale.x);
            var bottleBob = bottle.AddComponent<PixelSnappedBob>();

            var castPoint = bottle.AddComponent<BottleCastPoint>();
            var bottleSo = new SerializedObject(castPoint);
            bottleSo.FindProperty("spriteRenderer").objectReferenceValue = bottleRenderer;
            bottleSo.FindProperty("pointCollider").objectReferenceValue = bottleCollider;
            bottleSo.FindProperty("bob").objectReferenceValue = bottleBob;
            bottleSo.ApplyModifiedProperties();
        }

        private static void BuildCampCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Camp");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            // Root (collider + HutConstructionState + ConstructionGate, position fixed) wraps
            // an animated "Visual" child (sprite only) — same split as DriftwoodPiece, so
            // HutConstructionState's halfBuilt offset only ever nudges the visual, never the
            // hitbox, and the collider fits against the currently-real hut art instead of a
            // placeholder.
            Vector2 hutPos = AnchorToWorld(HutAnchor);
            var hut = new GameObject("Hut");
            Undo.RegisterCreatedObjectUndo(hut, "Build Landing Cove");
            hut.transform.SetParent(cluster.transform, false);
            hut.transform.localPosition = new Vector3(hutPos.x, hutPos.y, 0f);

            var hutVisual = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(hutVisual, "Build Landing Cove");
            hutVisual.transform.SetParent(hut.transform, false);
            var hutRenderer = hutVisual.AddComponent<SpriteRenderer>();
            hutRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HutRubbleSpritePath); // starting state
            hutRenderer.color = Color.white;
            hutRenderer.sortingOrder = 0;

            var hutState = hut.AddComponent<HutConstructionState>();
            var hutSo = new SerializedObject(hutState);
            hutSo.FindProperty("spriteRenderer").objectReferenceValue = hutRenderer;
            hutSo.FindProperty("rubbleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutRubbleSpritePath);
            hutSo.FindProperty("halfBuiltSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutHalfBuiltSpritePath);
            hutSo.FindProperty("completeSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutCompleteSpritePath);
            hutSo.ApplyModifiedProperties();

            var hutCollider = AddFittedBoxCollider2D(hut, hutRenderer);
            CapColliderToNearestNeighbor(hutCollider, hutPos, clickablePositions, hut.transform.localScale.x);
            var gate = hut.AddComponent<ConstructionGate>();
            var gateSo = new SerializedObject(gate);
            gateSo.FindProperty("coveIndex").intValue = 0; // Landing Cove
            gateSo.FindProperty("hutState").objectReferenceValue = hutState;
            gateSo.ApplyModifiedProperties();

            // No collider: purely decorative, never included in clickablePositions.
            var campfireSprites = System.Array.ConvertAll(CampfireSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var campfire = CreateWorldSprite("Campfire", cluster.transform, AnchorToWorld(CampfireAnchor), 1f, campfireSprites[0], Color.white, 1, showDebugLabel: false);
            var campfireAnimator = campfire.AddComponent<LoopingFrameAnimator>();
            var campfireSo = new SerializedObject(campfireAnimator);
            campfireSo.FindProperty("spriteRenderer").objectReferenceValue = campfire.GetComponent<SpriteRenderer>();
            var campfireFramesProp = campfireSo.FindProperty("frames");
            campfireFramesProp.arraySize = campfireSprites.Length;
            for (int f = 0; f < campfireSprites.Length; f++)
            {
                campfireFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = campfireSprites[f];
            }
            campfireSo.ApplyModifiedProperties();

            Vector2 bbwPos = AnchorToWorld(BBWHomeSpotAnchor);
            var bbwIdleFrames = System.Array.ConvertAll(BBWIdleSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbwWorkingFrames = System.Array.ConvertAll(BBWWorkingSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbwTapReactionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BBWTapReactionSpritePath);
            var bbw = CreateWorldSprite("BBWHomeSpot", cluster.transform, bbwPos, 1f, bbwIdleFrames[0], Color.white, 2, showDebugLabel: false);
            var bbwCollider = AddFittedBoxCollider2D(bbw);
            CapColliderToNearestNeighbor(bbwCollider, bbwPos, clickablePositions, bbw.transform.localScale.x);
            var bbwSpot = bbw.AddComponent<CrewRecruitSpot>();
            ApplyStringField(bbwSpot, "crewId", "bbw");

            var bbwAnimator = bbw.AddComponent<CrewHomeSpotAnimator>();
            var bbwAnimatorSo = new SerializedObject(bbwAnimator);
            bbwAnimatorSo.FindProperty("crewId").stringValue = "bbw";
            bbwAnimatorSo.FindProperty("spriteRenderer").objectReferenceValue = bbw.GetComponent<SpriteRenderer>();
            var bbwIdleFramesProp = bbwAnimatorSo.FindProperty("idleFrames");
            bbwIdleFramesProp.arraySize = bbwIdleFrames.Length;
            for (int f = 0; f < bbwIdleFrames.Length; f++) bbwIdleFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbwIdleFrames[f];
            var bbwWorkingFramesProp = bbwAnimatorSo.FindProperty("workingFrames");
            bbwWorkingFramesProp.arraySize = bbwWorkingFrames.Length;
            for (int f = 0; f < bbwWorkingFrames.Length; f++) bbwWorkingFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbwWorkingFrames[f];
            bbwAnimatorSo.FindProperty("tapReactionFrame").objectReferenceValue = bbwTapReactionSprite;
            bbwAnimatorSo.ApplyModifiedProperties();

            Vector2 bbcPos = AnchorToWorld(BBCHomeSpotAnchor);
            var bbcIdleFrames = System.Array.ConvertAll(BBCIdleSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbcWorkingFrames = System.Array.ConvertAll(BBCWorkingSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbcTapReactionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BBCTapReactionSpritePath);
            var bbc = CreateWorldSprite("BBCHomeSpot", cluster.transform, bbcPos, 1f, bbcIdleFrames[0], Color.white, 2, showDebugLabel: false);
            var bbcCollider = AddFittedBoxCollider2D(bbc);
            CapColliderToNearestNeighbor(bbcCollider, bbcPos, clickablePositions, bbc.transform.localScale.x);
            var bbcSpot = bbc.AddComponent<CrewRecruitSpot>();
            ApplyStringField(bbcSpot, "crewId", "bbc");

            var bbcAnimator = bbc.AddComponent<CrewHomeSpotAnimator>();
            var bbcAnimatorSo = new SerializedObject(bbcAnimator);
            bbcAnimatorSo.FindProperty("crewId").stringValue = "bbc";
            bbcAnimatorSo.FindProperty("spriteRenderer").objectReferenceValue = bbc.GetComponent<SpriteRenderer>();
            var bbcIdleFramesProp = bbcAnimatorSo.FindProperty("idleFrames");
            bbcIdleFramesProp.arraySize = bbcIdleFrames.Length;
            for (int f = 0; f < bbcIdleFrames.Length; f++) bbcIdleFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbcIdleFrames[f];
            var bbcWorkingFramesProp = bbcAnimatorSo.FindProperty("workingFrames");
            bbcWorkingFramesProp.arraySize = bbcWorkingFrames.Length;
            for (int f = 0; f < bbcWorkingFrames.Length; f++) bbcWorkingFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbcWorkingFrames[f];
            bbcAnimatorSo.FindProperty("tapReactionFrame").objectReferenceValue = bbcTapReactionSprite;
            bbcAnimatorSo.ApplyModifiedProperties();
        }

        private static void BuildFrontierCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Frontier");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            Vector2 triggerPos = AnchorToWorld(MiniBossTriggerAnchor);
            var trigger = CreateWorldSprite("MiniBossTrigger", cluster.transform, triggerPos, 1f, SquareSprite(), PurpleAccent, 2);
            var triggerCollider = AddFittedBoxCollider2D(trigger);
            CapColliderToNearestNeighbor(triggerCollider, triggerPos, clickablePositions, trigger.transform.localScale.x);
            var miniBoss = trigger.AddComponent<MiniBossTrigger>();

            var fightController = Object.FindFirstObjectByType<FightController>();
            if (fightController == null)
            {
                Debug.LogWarning("LandingCoveBuilder: no FightController found in the scene — run " +
                                  "Rubrehose > Build Persistent UI > Fight Modal, then re-run this command, " +
                                  "or assign MiniBossTrigger.fightController manually.");
            }
            var miniBossSo = new SerializedObject(miniBoss);
            miniBossSo.FindProperty("fightController").objectReferenceValue = fightController;
            miniBossSo.ApplyModifiedProperties();
        }

        private static void BuildRoamingCritter(Transform parent, List<Vector2> clickablePositions)
        {
            // Placeholder position only — scripted roaming path (Shoreline <-> Camp) isn't
            // implemented yet; parked at CritterStartAnchor. CritterEndAnchor is defined
            // above for when that behavior exists.
            Vector2 critterPos = AnchorToWorld(CritterStartAnchor);
            var critter = CreateWorldSprite("HermitCrab", parent, critterPos, 0.6f, CircleSprite(), PlaceholderThumbColor, 2);
            var critterCollider = AddFittedBoxCollider2D(critter);
            CapColliderToNearestNeighbor(critterCollider, critterPos, clickablePositions);
        }

        // The artwork-matched anchors above aren't evenly spaced (they're dictated by where
        // sand/terrain actually is in the background art, not hitbox comfort), so a shared
        // per-cluster spacing constant isn't safe anymore — some neighbors land as close as
        // ~0.2 units apart. Caps each object's collider to 80% of the distance to its single
        // closest neighbor among every other clickable object in the cove, so no two hitboxes
        // can ever overlap regardless of which pair ends up tightest.
        private static void CapColliderToNearestNeighbor(BoxCollider2D collider, Vector2 worldPos, List<Vector2> clickablePositions, float objectScale = 1f)
        {
            float nearest = float.MaxValue;
            foreach (var other in clickablePositions)
            {
                float d = Vector2.Distance(worldPos, other);
                if (d > 0.001f && d < nearest) nearest = d;
            }
            if (nearest == float.MaxValue) return; // no other clickable object in the cove

            // Collider.size is in the GameObject's local space, but the safe size computed
            // above is a world-space target — only equivalent when objectScale is 1.
            float safeLocalSize = (nearest * 0.8f) / objectScale;
            collider.size = new Vector2(Mathf.Min(collider.size.x, safeLocalSize), Mathf.Min(collider.size.y, safeLocalSize));
        }

        private static void ApplyStringField(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).stringValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
