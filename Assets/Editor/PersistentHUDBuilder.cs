using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rubrehose.UI;
using static Rubrehose.EditorTools.RubrehoseEditorUtils;

namespace Rubrehose.EditorTools
{
    // Builds the 5 remaining §A master-table elements (HUD_AND_LANDING_COVE_LAYOUT.md) —
    // Currency pill, Menu button, Cast a Net/Bottle Toss icon, Salvage Crate meter, Banked
    // Critters icon. The 6th element (fast-travel handle) is FastTravelRibbonBuilder's,
    // both share PersistentUICanvas but each only ever rebuilds its own named child, so
    // running these in either order is safe.
    //
    // Offset signs below are translated from the doc's plain-English margins (always
    // "inward from the named edge") rather than copied literally — the source table's
    // y-sign isn't self-consistent between top- and bottom-anchored rows.
    public static class PersistentHUDBuilder
    {
        private const string RootName = "PersistentHUD";

        private const float CrateMeterY = 0f; // anchored at vertical-center, so this offset is 0
        private const float CrittersBelowCrate = 56f;

        [MenuItem("Rubrehose/Build Persistent UI/HUD Elements")]
        public static void Build()
        {
            WarnIfNoTmpFontAsset();

            var canvas = FindOrCreatePersistentCanvas();
            bool alreadyExists = canvas.transform.Find(RootName) != null;
            if (alreadyExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Persistent HUD",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Persistent HUD");
            int undoGroup = Undo.GetCurrentGroup();

            DestroyExistingChild(canvas.transform, RootName);

            var rootRt = CreateUIObject(RootName, canvas.transform);
            Stretch(rootRt, Vector2.zero, Vector2.zero);

            var driftwoodText = BuildCurrencyPill(rootRt);
            BuildMenuButton(rootRt, out var menuButton);
            var castNetText = BuildCastNetIcon(rootRt);
            var crateImage = BuildCrateMeter(rootRt);
            var crittersText = BuildBankedCritters(rootRt);

            var controller = Undo.AddComponent<PersistentHUDController>(rootRt.gameObject);
            var so = new SerializedObject(controller);
            so.FindProperty("driftwoodText").objectReferenceValue = driftwoodText;
            so.FindProperty("menuButton").objectReferenceValue = menuButton;
            so.FindProperty("castNetChargeText").objectReferenceValue = castNetText;
            so.FindProperty("crateFillImage").objectReferenceValue = crateImage;
            so.FindProperty("bankedCrittersText").objectReferenceValue = crittersText;
            so.ApplyModifiedProperties();

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("PersistentHUDBuilder: built '" + RootName + "' on " + PersistentCanvasName + ". " +
                       "Cast-a-Net/Crate/Critters have no backing system yet — see UNITY_SETUP.md.");
        }

        private static TMP_Text BuildCurrencyPill(Transform parent)
        {
            var root = CreateUIObject("CurrencyPill", parent);
            Anchor(root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(140, 40));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UISprite();
            bg.color = InkColor;

            var icon = CreateUIObject("Icon", root);
            Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(32, 32));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = KnobSprite();
            iconImage.color = PlaceholderThumbColor;

            var textRt = CreateUIObject("Text", root);
            Stretch(textRt, new Vector2(48, 4), new Vector2(-8, -4));
            return AddText(textRt.gameObject, "0", 16, CreamColor, TextAlignmentOptions.MidlineLeft);
        }

        private static void BuildMenuButton(Transform parent, out Button button)
        {
            var root = CreateUIObject("MenuButton", parent);
            Anchor(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(44, 44));
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = UISprite();
            image.color = InkColor;
            button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Checkerboard motif placeholder — swap for the real icon per GAME_DESIGN.md's UI direction.
            var glyphRt = CreateUIObject("Glyph", root);
            Stretch(glyphRt, new Vector2(10, 10), new Vector2(-10, -10));
            var glyph = glyphRt.gameObject.AddComponent<Image>();
            glyph.sprite = KnobSprite();
            glyph.color = CreamColor;
        }

        private static TMP_Text BuildCastNetIcon(Transform parent)
        {
            // Mirrors the fast-travel handle's placement for visual symmetry (bottom-right vs bottom-left).
            var center = new Vector2(-50, 64);
            const float radius = 30f;

            var root = CreateUIObject("CastNetIcon", parent);
            Anchor(root, Vector2.right, Vector2.right, Vector2.right, Vector2.zero, Vector2.zero);

            var circle = CreateUIObject("Circle", root);
            Anchor(circle, Vector2.right, Vector2.right, new Vector2(0.5f, 0.5f), center, Vector2.one * (radius * 2f));
            var circleImage = circle.gameObject.AddComponent<Image>();
            circleImage.sprite = KnobSprite();
            circleImage.color = InkColor;
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circleImage;

            var glyphRt = CreateUIObject("Glyph", circle);
            Stretch(glyphRt, new Vector2(6, 6), new Vector2(-6, -6));
            var glyph = glyphRt.gameObject.AddComponent<Image>();
            glyph.color = PlaceholderThumbColor;

            var labelRt = CreateUIObject("ChargeCount", root);
            Anchor(labelRt, Vector2.right, Vector2.right, new Vector2(0.5f, 1f),
                new Vector2(center.x, center.y - radius - 4f), new Vector2(84, 24));
            return AddText(labelRt.gameObject, "0", 18, CreamColor, TextAlignmentOptions.Top);
        }

        private static Image BuildCrateMeter(Transform parent)
        {
            var root = CreateUIObject("SalvageCrateMeter", parent);
            Anchor(root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, CrateMeterY), new Vector2(44, 44));

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = KnobSprite();
            bg.color = InkColorTranslucent;

            var fillRt = CreateUIObject("Fill", root);
            Stretch(fillRt, Vector2.zero, Vector2.zero);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = KnobSprite();
            fill.color = TealAccent;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillAmount = 0.5f; // placeholder — ambient meter, no backing system yet

            return fill;
        }

        private static TMP_Text BuildBankedCritters(Transform parent)
        {
            var root = CreateUIObject("BankedCritters", parent);
            Anchor(root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(16, CrateMeterY - CrittersBelowCrate), new Vector2(40, 40));

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = KnobSprite();
            bg.color = PlaceholderThumbColor;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            var badgeRt = CreateUIObject("CountBadge", root);
            Anchor(badgeRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(4, -4), new Vector2(22, 16));
            var badgeBg = badgeRt.gameObject.AddComponent<Image>();
            badgeBg.sprite = KnobSprite();
            badgeBg.color = InkColor;

            var badgeTextRt = CreateUIObject("Text", badgeRt);
            Stretch(badgeTextRt, Vector2.zero, Vector2.zero);
            return AddText(badgeTextRt.gameObject, "0", 12, CreamColor, TextAlignmentOptions.Center);
        }
    }
}
