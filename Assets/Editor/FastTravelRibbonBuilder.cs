using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rubrehose.CameraControl;
using Rubrehose.UI;
using static Rubrehose.EditorTools.RubrehoseEditorUtils;

namespace Rubrehose.EditorTools
{
    // Builds the fast-travel handle/ribbon hierarchy from HUD_AND_LANDING_COVE_LAYOUT.md §A
    // (the "Fast-travel handle" row) with exact RectTransform values, instead of
    // hand-placing it in the Editor. Lives on the shared PersistentUICanvas alongside the
    // other §A elements (PersistentHUDBuilder) — only ever rebuilds its own "FastTravel"
    // child, so running this and the other builder tools in any order is safe.
    public static class FastTravelRibbonBuilder
    {
        private const string RootName = "FastTravel";
        private const string PrefabPath = "Assets/Prefabs/FastTravelSlot.prefab";

        // Master-table geometry (bottom-left anchor, center point at +50,-64, 60x60 / 30px radius).
        private static readonly Vector2 HandleCenter = new Vector2(50, 64);
        private const float HandleRadius = 30f;

        [MenuItem("Rubrehose/Build Persistent UI/Fast-Travel Ribbon")]
        public static void Build()
        {
            WarnIfNoTmpFontAsset();

            var canvas = FindOrCreatePersistentCanvas();
            bool alreadyExists = canvas.transform.Find(RootName) != null;
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            if (alreadyExists || prefabExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Fast-Travel Ribbon",
                    "This deletes and recreates the '" + RootName + "' hierarchy" +
                    (prefabExists ? " and overwrites " + PrefabPath : "") +
                    ". Any manual tweaks will be lost — real thumbnails/positions belong on " +
                    "FastTravelRibbonController's arrays, which this won't touch. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Fast-Travel Ribbon");
            int undoGroup = Undo.GetCurrentGroup();

            DestroyExistingChild(canvas.transform, RootName);

            var rootRt = CreateUIObject(RootName, canvas.transform);
            Anchor(rootRt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var slotPrefab = BuildAndSaveSlotPrefab();
            var handleGO = BuildCollapsedHandle(rootRt, out var handleButton, out var handleThumb, out var handleLabel, out var handleLiveBadge);
            var ribbonGO = BuildExpandedRibbon(rootRt, out var slotContainer, out var closeButton);

            var controller = Undo.AddComponent<FastTravelRibbonController>(rootRt.gameObject);

            var worldCamera = Object.FindFirstObjectByType<CoveViewCamera>();
            if (worldCamera == null)
            {
                Debug.LogWarning("FastTravelRibbonBuilder: no CoveViewCamera found in the scene — " +
                                  "assign FastTravelRibbonController.worldCamera manually.");
            }

            var so = new SerializedObject(controller);
            so.FindProperty("collapsedHandle").objectReferenceValue = handleGO;
            so.FindProperty("collapsedThumbnail").objectReferenceValue = handleThumb;
            so.FindProperty("collapsedLabel").objectReferenceValue = handleLabel;
            so.FindProperty("collapsedHandleButton").objectReferenceValue = handleButton;
            so.FindProperty("collapsedLiveTellBadge").objectReferenceValue = handleLiveBadge;
            so.FindProperty("expandedRibbon").objectReferenceValue = ribbonGO;
            so.FindProperty("slotContainer").objectReferenceValue = slotContainer;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            so.ApplyModifiedProperties();

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("FastTravelRibbonBuilder: built '" + RootName + "' on " + PersistentCanvasName + ". Assign real sprites on " +
                       "FastTravelRibbonController.coveThumbnails as each cove's terrain is built — no world-X array to " +
                       "maintain any more, it pans via CoveViewCamera.GoToCove.");
        }

        // --- Hierarchy pieces ------------------------------------------------

        private static GameObject BuildCollapsedHandle(Transform parent, out Button button, out Image thumbnail, out TMP_Text label, out GameObject liveTellBadge)
        {
            var root = CreateUIObject("CollapsedHandle", parent);
            Anchor(root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var circle = CreateUIObject("Circle", root);
            Anchor(circle, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), HandleCenter, Vector2.one * (HandleRadius * 2f));
            var circleImage = circle.gameObject.AddComponent<Image>();
            circleImage.sprite = KnobSprite();
            circleImage.color = InkColor;
            button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circleImage;

            var thumbRt = CreateUIObject("Thumbnail", circle);
            Stretch(thumbRt, new Vector2(6, 6), new Vector2(-6, -6));
            thumbnail = thumbRt.gameObject.AddComponent<Image>();
            thumbnail.color = PlaceholderThumbColor;
            thumbnail.preserveAspect = true;

            // Small label under the circle, per CAMERA_AND_UI_SPEC.md's fast-travel section.
            var labelRt = CreateUIObject("Label", root);
            Anchor(labelRt, Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f),
                new Vector2(HandleCenter.x, HandleCenter.y - HandleRadius - 4f), new Vector2(84, 24));
            label = AddText(labelRt.gameObject, "Landing Cove", 18, CreamColor, TextAlignmentOptions.Top);

            // "Something's live in another cove" badge (NEXT_CLAUDE_CODE_PUSH.md §1a) — small
            // glow dot, top-right of the circle. Hidden by default; FastTravelRibbonController
            // toggles it.
            var badgeRt = CreateUIObject("LiveTellBadge", circle);
            Anchor(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-2, -2), new Vector2(16, 16));
            var badgeImage = badgeRt.gameObject.AddComponent<Image>();
            badgeImage.sprite = KnobSprite();
            badgeImage.color = TealAccent;
            liveTellBadge = badgeRt.gameObject;
            liveTellBadge.SetActive(false);

            return root.gameObject;
        }

        private static GameObject BuildExpandedRibbon(Transform parent, out Transform slotContainer, out Button closeButton)
        {
            // Same anchor point the collapsed circle occupies (its bottom-left corner) —
            // the ribbon grows outward from there via HorizontalLayoutGroup + ContentSizeFitter.
            var anchorPoint = HandleCenter - Vector2.one * HandleRadius;
            var root = CreateUIObject("ExpandedRibbon", parent);
            Anchor(root, Vector2.zero, Vector2.zero, Vector2.zero, anchorPoint, Vector2.zero);

            var bg = root.gameObject.AddComponent<Image>();
            bg.color = InkColorTranslucent;

            var outerLayout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            outerLayout.childAlignment = TextAnchor.MiddleLeft;
            outerLayout.spacing = 16;
            outerLayout.padding = new RectOffset(16, 16, 12, 12);
            outerLayout.childControlWidth = true;
            outerLayout.childControlHeight = true;
            outerLayout.childForceExpandWidth = false;
            outerLayout.childForceExpandHeight = false;

            var outerFitter = root.gameObject.AddComponent<ContentSizeFitter>();
            outerFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            outerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var slotContainerRt = CreateUIObject("SlotContainer", root);
            var slotLayout = slotContainerRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.childAlignment = TextAnchor.MiddleLeft;
            slotLayout.spacing = 10;
            slotLayout.childControlWidth = true;
            slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;
            slotContainer = slotContainerRt;

            var closeRt = CreateUIObject("CloseButton", root);
            var closeLayoutElement = closeRt.gameObject.AddComponent<LayoutElement>();
            closeLayoutElement.preferredWidth = 32;
            closeLayoutElement.preferredHeight = 32;
            var closeImage = closeRt.gameObject.AddComponent<Image>();
            closeImage.color = InkColor;
            closeButton = closeRt.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;

            var closeLabelRt = CreateUIObject("Label", closeRt);
            Stretch(closeLabelRt, Vector2.zero, Vector2.zero);
            AddText(closeLabelRt.gameObject, "×", 20, CreamColor, TextAlignmentOptions.Center);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static FastTravelSlotUI BuildAndSaveSlotPrefab()
        {
            var root = CreateUIObject("FastTravelSlot", null, registerUndo: false);
            root.sizeDelta = new Vector2(72, 92);

            var rootImage = root.gameObject.AddComponent<Image>();
            rootImage.color = new Color(0, 0, 0, 0); // invisible, just a raycast target for Button
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = rootImage;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 72;
            layoutElement.preferredHeight = 92;

            var ring = CreateUIObject("HighlightRing", root, registerUndo: false);
            Anchor(ring, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(72, 72));
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = KnobSprite();
            ringImage.color = PurpleAccent;
            ring.gameObject.SetActive(false);

            var thumb = CreateUIObject("Thumbnail", root, registerUndo: false);
            Anchor(thumb, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(64, 64));
            var thumbImage = thumb.gameObject.AddComponent<Image>();
            thumbImage.color = PlaceholderThumbColor;
            thumbImage.preserveAspect = true;

            var labelRt = CreateUIObject("Label", root, registerUndo: false);
            Anchor(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(72, 22));
            var labelText = AddText(labelRt.gameObject, "Cove", 14, CreamColor, TextAlignmentOptions.Top);

            // Live-tell badge (NEXT_CLAUDE_CODE_PUSH.md §1a) — small glow dot, top-right of the
            // thumbnail. Hidden by default; FastTravelSlotUI.SetLiveTellBadge toggles it.
            var badgeRt = CreateUIObject("LiveTellBadge", thumb, registerUndo: false);
            Anchor(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            var badgeImage = badgeRt.gameObject.AddComponent<Image>();
            badgeImage.sprite = KnobSprite();
            badgeImage.color = TealAccent;
            badgeRt.gameObject.SetActive(false);

            var slotUI = root.gameObject.AddComponent<FastTravelSlotUI>();
            var so = new SerializedObject(slotUI);
            so.FindProperty("thumbnail").objectReferenceValue = thumbImage;
            so.FindProperty("label").objectReferenceValue = labelText;
            so.FindProperty("highlightRing").objectReferenceValue = ring.gameObject;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("liveTellBadge").objectReferenceValue = badgeRt.gameObject;
            so.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);

            return prefab.GetComponent<FastTravelSlotUI>();
        }
    }
}
