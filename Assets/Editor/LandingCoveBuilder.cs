using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubrehose.CameraControl;
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

        // 96x64px each (down from 120x80 — a deliberate scale reduction, the old size read as
        // too visually dominant), imported at 100px/unit (default) so they render at their true
        // 0.96x0.64-unit footprint. AddFittedBoxCollider2D refits each piece's tap box to this
        // smaller footprint automatically on rebuild, but CapColliderToNearestNeighbor's
        // neighbor-spacing safety cap (driven by DriftwoodAnchors' fixed spacing, not sprite
        // size) still binds tighter than even this smaller content size, so it — not the
        // sprite — remains the actual determinant of the final tap box size. See CapColliderToNearestNeighbor.
        private static readonly string[] DriftwoodSpritePaths =
        {
            "Assets/Art/WorldObjects/Driftwood/driftwood1.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood2.png",
            "Assets/Art/WorldObjects/Driftwood/driftwood3.png",
        };

        // 192x144px each, imported at 100px/unit (default) so they render at their true
        // 1.92x1.44-unit footprint. Now used as the Hut Cove Building's 3 paid-stage sprites
        // (CoveBuildingVisual.stageSprites) rather than construction-progress states — same 3
        // files, reinterpreted (modest -> serious -> grand investment tier instead of
        // rubble -> half-built -> complete).
        private const string HutStage1SpritePath = "Assets/Art/WorldObjects/Hut/stage1hut.png";
        private const string HutStage2SpritePath = "Assets/Art/WorldObjects/Hut/stage2hut.png";
        private const string HutStage3SpritePath = "Assets/Art/WorldObjects/Hut/stage3hut.png";

        // 64x56px each, imported at 100px/unit (default) so they render at their true
        // 0.64x0.56-unit footprint. Content bounds are near-identical across all four frames,
        // so LoopingFrameAnimator needs no per-frame offset.
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

        // 86x140px each, imported at 100px/unit (default) so they render at their true
        // 0.86x1.4-unit footprint — a 2-frame loop (content bounds nearly identical between
        // the two, confirmed safe to loop cleanly), not 3 like the idle/working sets. Loaded
        // and filtered to non-null below, same as every other sprite-path array here, so a
        // still-missing file degrades to an empty array rather than a null entry.
        private static readonly string[] BBWAttackSpritePaths =
        {
            "Assets/Art/Characters/BBW/bbwattacking1.png",
            "Assets/Art/Characters/BBW/bbwattacking2.png",
        };

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

        // 86x140px each, imported at 100px/unit (default) so they render at their true
        // 0.86x1.4-unit footprint — a 3-frame loop, filtered to non-null the same way as
        // BBWAttackSpritePaths above.
        private static readonly string[] BBCAttackSpritePaths =
        {
            "Assets/Art/Characters/BBC/bbcattacking1.png",
            "Assets/Art/Characters/BBC/bbcattacking2.png",
            "Assets/Art/Characters/BBC/bbcattacking3.png",
        };

        // 192x344px, imported at 34.133333 px/unit (Assets/Art/.../landingcove1.png.meta) so it
        // exactly covers the cove's full width (coveScreenWidth) at scale 1, and is a hair
        // taller than the camera's viewport (10.078 vs 2x orthoSize's 10) rather than shorter,
        // so it never leaves a gap at top/bottom. Anchors below map against this actual
        // rendered footprint, not just the camera viewport, so they land exactly where they
        // look right against the art regardless of that small overflow.
        private const string BackgroundSpritePath = "Assets/Art/Backgrounds/WreckBeach/landingcove1.png";
        private const float BackgroundWidth = HalfWidth * 2f;
        private const float BackgroundHeight = 344f / 34.133333f;

        // Anchors matched by eye against landingcove1.png's actual terrain (taller dune
        // composition, replacing the prior background of the same 192x344px footprint).
        // Nudged 2% left (was u=0.18) alongside the 15% size bump below.
        private static readonly Vector2 TuggyAnchor = new Vector2(0.16f, 0.85f);
        // Symmetric around u=0.50 (0.42/0.58 straddle it evenly) so the row centers
        // horizontally as a group.
        private static readonly Vector2[] DriftwoodAnchors =
        {
            new Vector2(0.42f, 0.80f),
            new Vector2(0.50f, 0.82f),
            new Vector2(0.58f, 0.80f),
        };
        // The flag pole marks the cast spot on the sand (always visible, purely decorative) —
        // shifted further left (another ~12% of screen width, left of the island) of its prior
        // spot. The actual bottle is a separate object out in the water to the pole's left,
        // hidden until a cast bottle washes back up — kept at the same relative offset from the
        // pole so the flag stays in the sand while the bottle stays in the water; still not yet
        // visually confirmed against the art the way the other anchors in this file have been.
        private static readonly Vector2 BottleFlagPoleAnchor = new Vector2(0.11f, 0.70f);
        private static readonly Vector2 BottleAnchor = new Vector2(0.04f, 0.72f);

        // Roaming path isn't implemented yet (BuildRoamingCritter placeholder-parks the critter
        // at Start) — End is kept here so it's not lost once that behavior exists. Shifted right
        // and up from its original placement; End is capped at 0.98 (the raw requested shift
        // would land past the background's right edge at u=1.0). Start nudged 2% down since
        // (only Start renders currently); End left as-is.
        private static readonly Vector2 CritterStartAnchor = new Vector2(0.80f, 0.78f);
        private static readonly Vector2 CritterEndAnchor = new Vector2(0.98f, 0.76f);

        // Nudged down 3% and right 2% (was u=0.50/v=0.54; before that v=0.59, before that v=0.62).
        private static readonly Vector2 HutAnchor = new Vector2(0.52f, 0.57f);
        // Centered horizontally on the driftwood row (avg of the three DriftwoodAnchors' u,
        // which is also Driftwood_2's own u) and dropped down to sit just above their tap
        // boxes: Driftwood_2 (the row's frontmost piece, directly below at u=0.50) has its
        // collider capped to 80% of the ~0.49-unit gap to its neighbors (CapColliderToNearestNeighbor),
        // so its box top edge lands around world y=-3.03; campfire's own 64x56 (0.64x0.56-unit)
        // footprint at v=0.74 (world y=-2.42, nudged up 2% from v=0.76) keeps a clear gap above
        // that, no collider overlap.
        private static readonly Vector2 CampfireAnchor = new Vector2(0.50f, 0.74f);
        // Nudged 2% right (was u=0.44).
        private static readonly Vector2 BBWHomeSpotAnchor = new Vector2(0.46f, 0.68f);
        // On the hill, above the rest of the Camp cluster at the dune's base — paired with
        // BBCHomeSpotScale below so it still reads as further back/up via forced-perspective
        // depth (flat 2D with depth layering, not true 3D), consistent with the rest of the cove.
        // Nudged 5% right (was u=0.50).
        private static readonly Vector2 BBCHomeSpotAnchor = new Vector2(0.55f, 0.42f);
        private static readonly Vector2 SerpentAnchor = new Vector2(0.90f, 0.48f);

        // Artifacts tell spots (NEXT_CLAUDE_CODE_PUSH.md §1a) — open sky/dune area, clear of
        // every other cluster's anchors.
        private static readonly Vector2[] TellAnchors =
        {
            new Vector2(0.30f, 0.15f),
            new Vector2(0.70f, 0.20f),
        };

        // Forced-perspective depth cue for BBCHomeSpot's hillside placement: smaller apparent
        // size reads as farther up/back on the hill. CreateWorldSprite's uniformScale param
        // already supports this per-object override — every other anchor in this file just
        // happens to pass 1f.
        private const float BBCHomeSpotScale = 0.6f;

        // Sized down off BBC/BBW's shared native footprint so BBW reads slightly smaller at
        // her Camp-level spot, then bumped back up ~15% off that (0.6 * 1.15) per feedback.
        private const float BBWHomeSpotScale = 0.69f;

        // Sized up ~25% off BBC/BBW's shared native footprint — Tuggy is a background/distance
        // plane, not tied to the forced-perspective hill scaling above. Bumped a further 15%
        // on top of that (1.25 * 1.15) per feedback.
        private const float TuggyScale = 1.4375f;

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
                AnchorToWorld(SerpentAnchor),
                AnchorToWorld(TellAnchors[0]),
                AnchorToWorld(TellAnchors[1]),
            };

            BuildBackground(root.transform);
            BuildDockCluster(root.transform);
            BuildShorelineCluster(root.transform, clickablePositions);
            BuildCampCluster(root.transform, clickablePositions);
            BuildFrontierCluster(root.transform, clickablePositions);
            BuildRoamingCritter(root.transform, clickablePositions);
            BuildArtifactsCluster(root.transform, clickablePositions);

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
            // decorative, never included in clickablePositions.
            var tuggy = CreateWorldSprite("Tuggy", cluster.transform, AnchorToWorld(TuggyAnchor), TuggyScale,
                AssetDatabase.LoadAssetAtPath<Sprite>(TuggySpritePath), Color.white, 0, showDebugLabel: false);
            var idleBob = tuggy.AddComponent<PixelSnappedBob>();

            // TuggyTravelController (CORE_PROGRESSION_RESTRUCTURE.md "Tuggy's travel
            // animation") — used to be a manual post-build Inspector step (UNITY_SETUP.md §7)
            // that a Build Landing Cove rebuild would silently wipe out, since rebuilding
            // deletes and recreates this whole tree. Wired here instead so it can never be
            // forgotten. Its OnEnable immediately snaps Tuggy to restingViewport (screen-
            // relative bottom-left, matching the doc's "resting position at bottom-left") the
            // moment it's added, same as it would after a manual Add Component — TuggyAnchor
            // above only matters as Tuggy's position before that first snap. Cruise Frames is
            // deliberately left unassigned: no real cruising sprites exist yet, so it plays a
            // plain position tween with no frame-swap on top (TuggyTravelController.cs's own
            // "placeholder-safe" comment).
            var travel = tuggy.AddComponent<TuggyTravelController>();
            var coveCamera = Object.FindFirstObjectByType<CoveViewCamera>();
            var travelSo = new SerializedObject(travel);
            travelSo.FindProperty("coveCamera").objectReferenceValue = coveCamera;
            travelSo.FindProperty("spriteRenderer").objectReferenceValue = tuggy.GetComponent<SpriteRenderer>();
            travelSo.FindProperty("idleBob").objectReferenceValue = idleBob;
            travelSo.ApplyModifiedProperties();

            if (coveCamera == null)
            {
                Debug.LogWarning("LandingCoveBuilder: no CoveViewCamera found in the scene (expected on Main " +
                                  "Camera) — TuggyTravelController's cove-settle direction logic won't fire " +
                                  "until one exists; re-run this command once it does to rewire it.");
            }
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

            // The Hut is now a Cove Building (CORE_PROGRESSION_RESTRUCTURE.md "Cove
            // Buildings"), not a construction gate — real, tappable-once-visible, but with
            // ZERO presence (no sprite, no collider) until its Stage 1 is actually paid for
            // via Menu -> Buildings, per the doc's strict "the island should look genuinely
            // bare on arrival" rule. Same hidden-until-earned mechanism CrewHomeSpotAnimator
            // already uses for BBW/BBC (CoveBuildingVisual toggles SpriteRenderer/Collider2D
            // .enabled, not a separate approach), single GameObject (no separate Visual child
            // needed — nothing here shifts position between stages the way the old
            // half-built offset did).
            //
            // Reuses the same 3 hut sprites as before — they read just as well as Stage
            // 1/2/3 investment tiers (modest -> serious -> grand) as they did as construction
            // progress (rubble -> half-built -> complete).
            Vector2 hutPos = AnchorToWorld(HutAnchor);
            var hut = CreateWorldSprite("Hut", cluster.transform, hutPos, 1f,
                AssetDatabase.LoadAssetAtPath<Sprite>(HutStage1SpritePath), Color.white, 0, showDebugLabel: false);
            var hutRenderer = hut.GetComponent<SpriteRenderer>();
            // Mirrored to face left instead of the art's default right-facing orientation —
            // flipX rather than a negative localScale, same reasoning as BBC's flipX below:
            // CoveBuildingVisual only ever reassigns .sprite (never touches flip state), so
            // this sticks across every stage swap.
            hutRenderer.flipX = true;

            var hutCollider = AddFittedBoxCollider2D(hut, hutRenderer);
            CapColliderToNearestNeighbor(hutCollider, hutPos, clickablePositions, hut.transform.localScale.x);

            var hutVisual = hut.AddComponent<CoveBuildingVisual>();
            var hutVisualSo = new SerializedObject(hutVisual);
            hutVisualSo.FindProperty("buildingId").stringValue = "hut"; // CoveBuildingCatalog.Buildings' "hut" entry
            hutVisualSo.FindProperty("spriteRenderer").objectReferenceValue = hutRenderer;
            var hutStagesProp = hutVisualSo.FindProperty("stageSprites");
            hutStagesProp.arraySize = 3;
            hutStagesProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutStage1SpritePath);
            hutStagesProp.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutStage2SpritePath);
            hutStagesProp.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HutStage3SpritePath);
            hutVisualSo.ApplyModifiedProperties();

            // Build hint marker (NEXT_CLAUDE_CODE_PUSH.md §2) — "obvious" variant for Landing
            // Cove specifically (icon + label text), since this is the player's first exposure
            // to the Buildings system. Toggled by BuildHintMarker off the exact same
            // GetBuildingStage/IsBuildingCoveReached state CoveBuildingVisual reads above, not a
            // separate tracked flag, so it appears/disappears in lockstep with the Hut's own
            // zero-presence window.
            var buildHintRoot = new GameObject("BuildHint");
            Undo.RegisterCreatedObjectUndo(buildHintRoot, "Build Landing Cove");
            buildHintRoot.transform.SetParent(hut.transform, false);
            buildHintRoot.transform.localPosition = new Vector3(0f, 0.9f, 0f); // floats above the (currently invisible) Hut

            var buildHintIcon = buildHintRoot.AddComponent<SpriteRenderer>();
            buildHintIcon.sprite = CircleSprite();
            buildHintIcon.color = TealAccent;
            buildHintIcon.sortingOrder = 3; // above the Hut's own sortingOrder (0)

            var buildHintLabelGo = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(buildHintLabelGo, "Build Landing Cove");
            buildHintLabelGo.transform.SetParent(buildHintRoot.transform, false);
            buildHintLabelGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var buildHintLabel = buildHintLabelGo.AddComponent<TextMeshPro>();
            buildHintLabel.text = "Something can be built here!";
            buildHintLabel.fontSize = 3;
            buildHintLabel.color = CreamColor;
            buildHintLabel.alignment = TextAlignmentOptions.Center;
            buildHintLabel.enableWordWrapping = true;
            var buildHintLabelRt = buildHintLabelGo.GetComponent<RectTransform>();
            if (buildHintLabelRt != null) buildHintLabelRt.sizeDelta = new Vector2(2.2f, 1f);
            var buildHintLabelMr = buildHintLabelGo.GetComponent<MeshRenderer>();
            if (buildHintLabelMr != null) buildHintLabelMr.sortingOrder = 3;

            var buildHint = hut.AddComponent<BuildHintMarker>();
            var buildHintSo = new SerializedObject(buildHint);
            buildHintSo.FindProperty("buildingId").stringValue = "hut";
            buildHintSo.FindProperty("iconRoot").objectReferenceValue = buildHintRoot;
            buildHintSo.ApplyModifiedProperties();

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
            var bbw = CreateWorldSprite("BBWHomeSpot", cluster.transform, bbwPos, BBWHomeSpotScale, bbwIdleFrames[0], Color.white, 2, showDebugLabel: false);
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
            var bbwAttackFrames = System.Array.FindAll(
                System.Array.ConvertAll(BBWAttackSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>), s => s != null);
            var bbwAttackFramesProp = bbwAnimatorSo.FindProperty("attackFrames");
            bbwAttackFramesProp.arraySize = bbwAttackFrames.Length;
            for (int f = 0; f < bbwAttackFrames.Length; f++) bbwAttackFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbwAttackFrames[f];
            bbwAnimatorSo.FindProperty("tapReactionFrame").objectReferenceValue = bbwTapReactionSprite;
            // Flanks the serpent from its left so BBW/BBC don't walk to and stack on the exact
            // same point when both are recruited and fighting together.
            bbwAnimatorSo.FindProperty("attackOffset").vector2Value = new Vector2(-0.4f, 0f);
            bbwAnimatorSo.ApplyModifiedProperties();

            Vector2 bbcPos = AnchorToWorld(BBCHomeSpotAnchor);
            var bbcIdleFrames = System.Array.ConvertAll(BBCIdleSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbcWorkingFrames = System.Array.ConvertAll(BBCWorkingSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            var bbcTapReactionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BBCTapReactionSpritePath);
            var bbc = CreateWorldSprite("BBCHomeSpot", cluster.transform, bbcPos, BBCHomeSpotScale, bbcIdleFrames[0], Color.white, 2, showDebugLabel: false);
            // Mirrored left-right so BBC faces the opposite direction from his default art —
            // flipX on the SpriteRenderer rather than a negative localScale, since
            // CrewHomeSpotAnimator only ever reassigns .sprite (never touches flip state or
            // scale), so this sticks across every idle/working/tap-reaction frame swap.
            bbc.GetComponent<SpriteRenderer>().flipX = true;
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
            var bbcAttackFrames = System.Array.FindAll(
                System.Array.ConvertAll(BBCAttackSpritePaths, AssetDatabase.LoadAssetAtPath<Sprite>), s => s != null);
            var bbcAttackFramesProp = bbcAnimatorSo.FindProperty("attackFrames");
            bbcAttackFramesProp.arraySize = bbcAttackFrames.Length;
            for (int f = 0; f < bbcAttackFrames.Length; f++) bbcAttackFramesProp.GetArrayElementAtIndex(f).objectReferenceValue = bbcAttackFrames[f];
            bbcAnimatorSo.FindProperty("tapReactionFrame").objectReferenceValue = bbcTapReactionSprite;
            // Flanks the serpent from its right — see the matching BBW comment above.
            bbcAnimatorSo.FindProperty("attackOffset").vector2Value = new Vector2(0.4f, 0f);
            bbcAnimatorSo.ApplyModifiedProperties();
        }

        // Builds the serpent itself as a persistent world object at the Frontier trigger spot
        // (IN_SCENE_FIGHT_SYSTEM.md "activating the Frontier trigger causes the serpent to
        // appear/activate directly at that position") — FightController and SerpentVisual both
        // live on it directly rather than on a separate screen-space modal. Its overlay UI
        // (HP bar/timer) is built separately by FightOverlayBuilder and wired into this same
        // FightController; see the order note in UNITY_SETUP.md.
        private static void BuildFrontierCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Frontier");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
            cluster.transform.SetParent(parent, false);

            Vector2 serpentPos = AnchorToWorld(SerpentAnchor);
            // White/no tint: the serpent is a character under GAME_DESIGN.md's locked art
            // direction (monochrome cast), same rule already applied to BBW/BBC/the bottle.
            var serpent = CreateWorldSprite("Serpent", cluster.transform, serpentPos, 1f, SquareSprite(), Color.white, 2);
            var serpentCollider = AddFittedBoxCollider2D(serpent);
            CapColliderToNearestNeighbor(serpentCollider, serpentPos, clickablePositions, serpent.transform.localScale.x);

            var serpentVisual = serpent.AddComponent<SerpentVisual>();
            var serpentVisualSo = new SerializedObject(serpentVisual);
            serpentVisualSo.FindProperty("coveIndex").intValue = 0; // Landing Cove
            serpentVisualSo.FindProperty("spriteRenderer").objectReferenceValue = serpent.GetComponent<SpriteRenderer>();
            serpentVisualSo.ApplyModifiedProperties();

            var fightController = serpent.AddComponent<FightController>();
            var fightControllerSo = new SerializedObject(fightController);
            fightControllerSo.FindProperty("coveIndex").intValue = 0; // Landing Cove
            fightControllerSo.FindProperty("serpentVisual").objectReferenceValue = serpentVisual;
            fightControllerSo.ApplyModifiedProperties();

            Debug.Log("LandingCoveBuilder: built 'Serpent' with FightController — run " +
                       "Rubrehose > Build Persistent UI > Fight Overlay (before or after this command) " +
                       "to wire its HP bar/timer overlay.");
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

        // Artifacts tell spots (NEXT_CLAUDE_CODE_PUSH.md §1a) — dormant idle-loop placeholders
        // (Chad's real per-cove art pass isn't done yet) with a placeholder "live glint" overlay
        // child, each wrapped by a TellSpot. One TellSpawner cycles between them on a randomized
        // timer; see TellSpawner.cs's class comment re: this NOT being a reuse of any existing
        // Salvage Crate system (none exists).
        private static void BuildArtifactsCluster(Transform parent, List<Vector2> clickablePositions)
        {
            var cluster = new GameObject("Artifacts");
            Undo.RegisterCreatedObjectUndo(cluster, "Build Landing Cove");
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
            spawnerSo.FindProperty("coveIndex").intValue = 0; // Landing Cove
            var tellsProp = spawnerSo.FindProperty("tells");
            tellsProp.arraySize = tells.Length;
            for (int i = 0; i < tells.Length; i++) tellsProp.GetArrayElementAtIndex(i).objectReferenceValue = tells[i];
            spawnerSo.ApplyModifiedProperties();
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
