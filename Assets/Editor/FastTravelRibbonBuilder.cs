using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rubrehose.CameraControl;
using Rubrehose.UI;

namespace Rubrehose.EditorTools
{
    // Builds the fast-travel handle/ribbon hierarchy from CAMERA_AND_UI_SPEC.md with exact
    // RectTransform values, instead of hand-placing it in the Editor. Re-runnable: rebuilds
    // the scene hierarchy and the slot prefab from scratch each time (after confirming),
    // so any manual tweaks made directly on them get overwritten — real biome thumbnails
    // and per-biome world-X positions belong on FastTravelRibbonController's serialized
    // arrays, not hand-edited onto the generated objects, so they survive a rebuild.
    public static class FastTravelRibbonBuilder
    {
        private const string RootName = "FastTravel";
        private const string CanvasName = "FastTravelCanvas";
        private const string PrefabPath = "Assets/Prefabs/FastTravelSlot.prefab";

        private static readonly Color InkColor = new Color32(26, 26, 26, 255);
        private static readonly Color InkColorTranslucent = new Color32(26, 26, 26, 235);
        private static readonly Color CreamColor = new Color32(244, 239, 224, 255);
        private static readonly Color PlaceholderThumbColor = new Color32(230, 227, 214, 255);
        private static readonly Color PurpleAccent = new Color32(127, 119, 221, 255);

        [MenuItem("Rubrehose/Build Fast-Travel Ribbon")]
        public static void Build()
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("FastTravelRibbonBuilder: no default TMP font asset found — import TMP Essentials " +
                                  "(Window > TextMeshPro > Import TMP Essential Resources), then re-run this command " +
                                  "if labels look blank.");
            }

            var existingCanvas = GameObject.Find(CanvasName);
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            if (existingCanvas != null || prefabExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Fast-Travel Ribbon",
                    "This deletes and recreates the '" + CanvasName + "' hierarchy" +
                    (prefabExists ? " and overwrites " + PrefabPath : "") +
                    ". Any manual tweaks will be lost — real thumbnails/positions belong on " +
                    "FastTravelRibbonController's arrays, which this won't touch. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Fast-Travel Ribbon");
            int undoGroup = Undo.GetCurrentGroup();

            if (existingCanvas != null) Undo.DestroyObjectImmediate(existingCanvas);

            var canvasGO = BuildCanvas();
            var rootRt = CreateUIObject(RootName, canvasGO.transform);
            Anchor(rootRt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var slotPrefab = BuildAndSaveSlotPrefab();
            var handleGO = BuildCollapsedHandle(rootRt, out var handleButton, out var handleThumb, out var handleLabel);
            var ribbonGO = BuildExpandedRibbon(rootRt, out var slotContainer, out var closeButton);

            var controller = Undo.AddComponent<FastTravelRibbonController>(rootRt.gameObject);

            var worldCamera = Object.FindFirstObjectByType<WorldScrollCamera>();
            if (worldCamera == null)
            {
                Debug.LogWarning("FastTravelRibbonBuilder: no WorldScrollCamera found in the scene — " +
                                  "assign FastTravelRibbonController.worldCamera manually.");
            }

            var so = new SerializedObject(controller);
            so.FindProperty("collapsedHandle").objectReferenceValue = handleGO;
            so.FindProperty("collapsedThumbnail").objectReferenceValue = handleThumb;
            so.FindProperty("collapsedLabel").objectReferenceValue = handleLabel;
            so.FindProperty("collapsedHandleButton").objectReferenceValue = handleButton;
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
            Debug.Log("FastTravelRibbonBuilder: built '" + CanvasName + "'. Assign real sprites on " +
                       "FastTravelRibbonController.biomeThumbnails and world-X positions on biomeWorldX " +
                       "as each biome's terrain is built.");
        }

        // --- Hierarchy pieces ------------------------------------------------

        private static GameObject BuildCanvas()
        {
            var go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Build Fast-Travel Ribbon");

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // above the default-order HUD canvas

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }

        private static GameObject BuildCollapsedHandle(Transform parent, out Button button, out Image thumbnail, out TMP_Text label)
        {
            // Bottom-left, clear of the screen edge (avoids the iOS home-gesture zone).
            var root = CreateUIObject("CollapsedHandle", parent);
            Anchor(root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 44), new Vector2(84, 100));

            var circle = CreateUIObject("Circle", root);
            Anchor(circle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(72, 72));
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

            var labelRt = CreateUIObject("Label", root);
            Anchor(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(84, 24));
            label = AddText(labelRt.gameObject, "Wreck Beach", 18, CreamColor, TextAlignmentOptions.Top);

            return root.gameObject;
        }

        private static GameObject BuildExpandedRibbon(Transform parent, out Transform slotContainer, out Button closeButton)
        {
            // Same anchor point the collapsed circle occupies — the ribbon grows outward
            // from there (via HorizontalLayoutGroup + ContentSizeFitter), it isn't a
            // separate panel elsewhere on screen.
            var root = CreateUIObject("ExpandedRibbon", parent);
            Anchor(root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 44), Vector2.zero);

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
            var labelText = AddText(labelRt.gameObject, "Biome", 14, CreamColor, TextAlignmentOptions.Top);

            var slotUI = root.gameObject.AddComponent<FastTravelSlotUI>();
            var so = new SerializedObject(slotUI);
            so.FindProperty("thumbnail").objectReferenceValue = thumbImage;
            so.FindProperty("label").objectReferenceValue = labelText;
            so.FindProperty("highlightRing").objectReferenceValue = ring.gameObject;
            so.FindProperty("button").objectReferenceValue = button;
            so.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);

            return prefab.GetComponent<FastTravelSlotUI>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Build Fast-Travel Ribbon");
        }

        // --- Small RectTransform helpers -------------------------------------

        private static RectTransform CreateUIObject(string name, Transform parent, bool registerUndo = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (registerUndo) Undo.RegisterCreatedObjectUndo(go, "Build Fast-Travel Ribbon");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }
}
