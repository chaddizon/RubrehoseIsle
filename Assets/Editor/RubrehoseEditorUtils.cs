using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rubrehose.EditorTools
{
    // Shared helpers for the Rubrehose auto-build tools (FastTravelRibbonBuilder,
    // PersistentHUDBuilder, LandingCoveBuilder, MenuDrawerBuilder) so exact RectTransform
    // placement, placeholder colors/sprites, and canvas ownership rules aren't duplicated
    // across every tool.
    public static class RubrehoseEditorUtils
    {
        public const string PersistentCanvasName = "PersistentUICanvas";

        // Palette from rubrehose_prototype.html — the only validated color reference
        // until GAME_DESIGN.md's "final call still open" palette question is settled.
        public static readonly Color InkColor = new Color32(26, 26, 26, 255);
        public static readonly Color InkColorTranslucent = new Color32(26, 26, 26, 235);
        public static readonly Color CreamColor = new Color32(244, 239, 224, 255);
        public static readonly Color PlaceholderThumbColor = new Color32(230, 227, 214, 255);
        public static readonly Color PurpleAccent = new Color32(127, 119, 221, 255);
        public static readonly Color TealAccent = new Color32(29, 158, 117, 255);

        // --- Screen-space (Canvas / RectTransform) ---------------------------

        // Finds the shared persistent-UI Canvas, or creates it if this is the first
        // builder tool to run. Every screen-space builder targets this same Canvas but
        // only ever destroys/rebuilds its OWN named root child, so tools don't stomp
        // each other's work when run in any order.
        public static Canvas FindOrCreatePersistentCanvas()
        {
            var existing = GameObject.Find(PersistentCanvasName);
            if (existing != null && existing.TryGetComponent<Canvas>(out var canvas)) return canvas;

            var go = new GameObject(PersistentCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Build Rubrehose UI");

            var newCanvas = go.GetComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return newCanvas;
        }

        // Destroys an existing same-named child of parent (if any) so a builder can
        // rebuild only its own subtree without touching siblings other tools own.
        public static void DestroyExistingChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        }

        public static RectTransform CreateUIObject(string name, Transform parent, bool registerUndo = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (registerUndo) Undo.RegisterCreatedObjectUndo(go, "Build Rubrehose UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        public static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static TextMeshProUGUI AddText(GameObject go, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        // Unity's built-in circular slider/scrollbar-handle sprite — used as the round
        // placeholder for UI elements (fast-travel handle, close button, etc).
        public static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // Unity's built-in rounded-rect UI sprite — used as the boxy UI placeholder.
        public static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Idempotent — safe to call from every builder tool regardless of run order.
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Build Rubrehose UI");
        }

        public static void WarnIfNoTmpFontAsset()
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("Rubrehose editor tools: no default TMP font asset found — import TMP Essentials " +
                                  "(Window > TextMeshPro > Import TMP Essential Resources), then re-run this command " +
                                  "if labels look blank.");
            }
        }

        // --- World-space (SpriteRenderer) ------------------------------------

        // Unity's built-in 2D primitive sprites (same ones GameObject > 2D Object > Sprites uses).
        public static Sprite SquareSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Square.png");
        public static Sprite CircleSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Circle.png");

        public static GameObject CreateWorldSprite(string name, Transform parent, Vector2 position, float uniformScale, Sprite sprite, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Rubrehose UI");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = Vector3.one * uniformScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return go;
        }
    }
}
