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

        // AssetDatabase.GetBuiltinExtraResource<Sprite>("Sprites/Square.png"/"Sprites/Circle.png")
        // was returning null in this project (verified: Tuggy and Hut's SpriteRenderer.sprite
        // both came back "None" in the Inspector despite CreateWorldSprite assigning them) —
        // reason unconfirmed, not worth chasing further. A Sprite.Create()-generated
        // replacement doesn't work either: it's a session-only in-memory object with no
        // AssetDatabase entry, so it doesn't survive the domain reload Unity does on entering
        // Play mode (visible right after Build, gone the moment Play starts). Loading a real
        // committed texture asset instead — same reliable pattern as the driftwood sprites —
        // is the only approach proven to survive both. 100x100 @ 100 px/unit = exactly 1 world
        // unit, matching the old builtin sprites' size so scale/collider-fit math is unaffected.
        // Both shapes share the one placeholder for now — shape doesn't matter for a
        // solid-color placeholder, only the SpriteRenderer's tint color does.
        private const string PlaceholderSpritePath = "Assets/Art/WorldObjects/Placeholder/placeholder_square.png";

        public static Sprite SquareSprite() => PlaceholderSprite();
        public static Sprite CircleSprite() => PlaceholderSprite();

        private static Sprite PlaceholderSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (sprite == null)
            {
                Debug.LogWarning("PlaceholderSprite: couldn't load '" + PlaceholderSpritePath + "' as a Sprite — " +
                                  "check its import settings (Texture Type must be 'Sprite (2D and UI)').");
            }
            return sprite;
        }

        // Placeholder squares/circles are otherwise indistinguishable at a glance — flip this
        // off once real art starts replacing them and the GameObject name in the Hierarchy is
        // enough context again.
        private const bool ShowDebugLabels = true;

        // showDebugLabel lets callers opt individual objects out once they've got real art —
        // defaults to true so every existing call site keeps labeling until told otherwise.
        public static GameObject CreateWorldSprite(string name, Transform parent, Vector2 position, float uniformScale, Sprite sprite, Color color, int sortingOrder, bool showDebugLabel = true)
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

            if (showDebugLabel) AddDebugNameLabel(go);

            return go;
        }

        // World-space name tag floating above a placeholder object, always upright and
        // consistently sized regardless of the object's own uniformScale (which would
        // otherwise shrink/enlarge a naively-parented child label right along with it).
        // Public so callers building a multi-object hierarchy (e.g. DriftwoodPiece's
        // static root + animated visual child) can attach the label to whichever part
        // should stay put — the root, not whatever's being animated. Gated by
        // ShowDebugLabels internally so every call site stays consistent from one flag.
        public static void AddDebugNameLabel(GameObject go)
        {
            if (!ShowDebugLabels) return;

            var labelGo = new GameObject("DebugLabel");
            Undo.RegisterCreatedObjectUndo(labelGo, "Build Rubrehose UI");
            labelGo.transform.SetParent(go.transform, false);

            float parentScale = go.transform.localScale.x;
            float compensate = Mathf.Approximately(parentScale, 0f) ? 1f : 1f / parentScale;
            labelGo.transform.localPosition = new Vector3(0f, 0.8f * compensate, 0f);
            labelGo.transform.localScale = Vector3.one * compensate;

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = go.name;
            tmp.fontSize = 3;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;

            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 1000; // always draw above every placeholder sprite
        }

        // GameObject.AddComponent<BoxCollider2D>() does NOT auto-fit to the sibling
        // SpriteRenderer's bounds — that convenience only fires when a BoxCollider2D is
        // added via the Inspector's "Add Component" button. From editor script it leaves
        // the collider at Unity's raw default, which serializes out to a near-zero
        // {0.0001, 0.0001} size — an effectively unclickable hitbox. Fit it explicitly.
        public static BoxCollider2D AddFittedBoxCollider2D(GameObject go) =>
            AddFittedBoxCollider2D(go, go.GetComponent<SpriteRenderer>());

        // Overload for hierarchies where the collider's GameObject isn't the one holding
        // the SpriteRenderer — e.g. DriftwoodPiece, where the collider sits on a static
        // root so tap detection is unaffected by the animated visual child shrinking/
        // fading during its collect/wash-up animation.
        public static BoxCollider2D AddFittedBoxCollider2D(GameObject go, SpriteRenderer rendererForBounds)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            if (rendererForBounds != null && rendererForBounds.sprite != null)
            {
                collider.size = rendererForBounds.sprite.bounds.size;
            }
            else
            {
                Debug.LogWarning("AddFittedBoxCollider2D: '" + go.name + "' has no sprite to fit against " +
                                  "(null sprite or missing SpriteRenderer) — collider left at Unity's default " +
                                  "size, which is effectively unclickable. Fix the sprite reference and rebuild.");
            }
            return collider;
        }
    }
}
