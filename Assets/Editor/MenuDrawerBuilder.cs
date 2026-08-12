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
    // Builds the menu drawer shell (HUD_AND_LANDING_COVE_LAYOUT.md §C) — a comic-panel-style
    // slide-in from the right edge with 6 placeholder entry rows. Row taps just log for now;
    // real per-entry panel content (Crew list, Upgrades tree, etc.) doesn't exist yet.
    public static class MenuDrawerBuilder
    {
        private const string RootName = "MenuDrawer";

        // PanelWidth + 20 must match MenuDrawerController's closedX default (380) so the
        // panel starts fully off-screen — if you change one, change the other.
        private const float PanelWidth = 360f;

        [MenuItem("Rubrehose/Build Persistent UI/Menu Drawer")]
        public static void Build()
        {
            WarnIfNoTmpFontAsset();

            var canvas = FindOrCreatePersistentCanvas();
            bool alreadyExists = canvas.transform.Find(RootName) != null;
            if (alreadyExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Menu Drawer",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Menu Drawer");
            int undoGroup = Undo.GetCurrentGroup();

            DestroyExistingChild(canvas.transform, RootName);

            var rootRt = CreateUIObject(RootName, canvas.transform);
            Stretch(rootRt, Vector2.zero, Vector2.zero); // full-screen wrapper for the click-outside catcher + panel

            var catcherRt = CreateUIObject("ClickOutsideCatcher", rootRt);
            Stretch(catcherRt, Vector2.zero, Vector2.zero);
            var catcherImage = catcherRt.gameObject.AddComponent<Image>();
            catcherImage.color = new Color(0, 0, 0, 0);
            var catcherButton = catcherRt.gameObject.AddComponent<Button>();
            catcherButton.targetGraphic = catcherImage;

            var panel = CreateUIObject("Panel", rootRt);
            Anchor(panel, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(PanelWidth + 20f, 0), new Vector2(PanelWidth, 0));
            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.sprite = UISprite();
            panelBg.color = InkColorTranslucent;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8;
            layout.padding = new RectOffset(16, 16, 24, 16);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var crewRow = BuildRow(panel, "Crew");
            var upgradesRow = BuildRow(panel, "Upgrades");
            var captainsLogRow = BuildRow(panel, "Captain's Log");
            var milestonesRow = BuildRow(panel, "Milestones");
            var settingsRow = BuildRow(panel, "Settings");
            var artifactsRow = BuildRow(panel, "Artifacts");

            var closeRt = CreateUIObject("CloseButton", panel);
            var closeLayoutElement = closeRt.gameObject.AddComponent<LayoutElement>();
            closeLayoutElement.ignoreLayout = true; // positioned manually in the panel's corner, not part of the row list
            Anchor(closeRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(28, 28));
            var closeImage = closeRt.gameObject.AddComponent<Image>();
            closeImage.color = InkColor;
            var closeButton = closeRt.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabelRt = CreateUIObject("Label", closeRt);
            Stretch(closeLabelRt, Vector2.zero, Vector2.zero);
            AddText(closeLabelRt.gameObject, "×", 18, CreamColor, TextAlignmentOptions.Center);

            var controller = Undo.AddComponent<MenuDrawerController>(rootRt.gameObject);
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("captainsLogRow").objectReferenceValue = captainsLogRow;
            so.FindProperty("artifactsRow").objectReferenceValue = artifactsRow;
            so.ApplyModifiedProperties();

            catcherButton.onClick.AddListener(controller.Close);
            closeButton.onClick.AddListener(controller.Close);
            WireRow(crewRow, controller, "Crew");
            WireRow(upgradesRow, controller, "Upgrades");
            WireRow(captainsLogRow, controller, "Captain's Log");
            WireRow(milestonesRow, controller, "Milestones");
            WireRow(settingsRow, controller, "Settings");
            WireRow(artifactsRow, controller, "Artifacts");

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("MenuDrawerBuilder: built '" + RootName + "' on " + PersistentCanvasName + " (starts off-screen — " +
                       "that's the closed state, not a bug). Row taps just log for now.");
        }

        private static GameObject BuildRow(Transform panel, string label)
        {
            var row = CreateUIObject(label.Replace(" ", "").Replace("'", ""), panel);
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 56;
            var rowImage = row.gameObject.AddComponent<Image>();
            rowImage.sprite = UISprite();
            rowImage.color = InkColor;
            var rowButton = row.gameObject.AddComponent<Button>();
            rowButton.targetGraphic = rowImage;

            var icon = CreateUIObject("Icon", row);
            Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(32, 32));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = KnobSprite();
            iconImage.color = PlaceholderThumbColor;

            var textRt = CreateUIObject("Label", row);
            Stretch(textRt, new Vector2(56, 4), new Vector2(-12, -4));
            AddText(textRt.gameObject, label, 16, CreamColor, TextAlignmentOptions.MidlineLeft);

            return row.gameObject;
        }

        private static void WireRow(GameObject row, MenuDrawerController controller, string label)
        {
            row.GetComponent<Button>().onClick.AddListener(() => controller.OnEntryTapped(label));
        }
    }
}
