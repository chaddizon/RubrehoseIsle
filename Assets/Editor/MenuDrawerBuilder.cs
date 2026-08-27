using System.IO;
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
    // Builds the menu drawer (HUD_AND_LANDING_COVE_LAYOUT.md §C) — a comic-panel-style
    // slide-in from the right edge with 6 entry rows. Crew and Upgrades rows open real
    // content panels (core-loop critical); the rest still just log until those systems
    // exist. All click wiring lives on MenuDrawerController (assigned here via serialized
    // fields, not edit-time AddListener) so it survives domain reload / Play mode.
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

            var rowList = CreateUIObject("RowList", panel);
            Stretch(rowList, Vector2.zero, Vector2.zero);
            var layout = rowList.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8;
            layout.padding = new RectOffset(16, 16, 24, 16);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var crewRow = BuildRow(rowList, "Crew");
            var upgradesRow = BuildRow(rowList, "Upgrades");
            var captainsLogRow = BuildRow(rowList, "Captain's Log");
            var milestonesRow = BuildRow(rowList, "Milestones");
            var settingsRow = BuildRow(rowList, "Settings");
            var artifactsRow = BuildRow(rowList, "Artifacts");

            var crewPanel = BuildCrewPanel(panel);
            var upgradesPanel = BuildUpgradesPanel(panel);

            var closeRt = CreateUIObject("CloseButton", panel);
            Anchor(closeRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(28, 28));
            var closeImage = closeRt.gameObject.AddComponent<Image>();
            closeImage.color = InkColor;
            var closeButton = closeRt.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabelRt = CreateUIObject("Label", closeRt);
            Stretch(closeLabelRt, Vector2.zero, Vector2.zero);
            AddText(closeLabelRt.gameObject, "×", 18, CreamColor, TextAlignmentOptions.Center);

            var backRt = CreateUIObject("BackButton", panel);
            Anchor(backRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -8), new Vector2(28, 28));
            var backImage = backRt.gameObject.AddComponent<Image>();
            backImage.color = InkColor;
            var backButton = backRt.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            var backLabelRt = CreateUIObject("Label", backRt);
            Stretch(backLabelRt, Vector2.zero, Vector2.zero);
            AddText(backLabelRt.gameObject, "‹", 20, CreamColor, TextAlignmentOptions.Center);
            backRt.gameObject.SetActive(false);

            var controller = Undo.AddComponent<MenuDrawerController>(rootRt.gameObject);
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("catcherButton").objectReferenceValue = catcherButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("crewRowButton").objectReferenceValue = crewRow.GetComponent<Button>();
            so.FindProperty("upgradesRowButton").objectReferenceValue = upgradesRow.GetComponent<Button>();
            so.FindProperty("captainsLogRowButton").objectReferenceValue = captainsLogRow.GetComponent<Button>();
            so.FindProperty("milestonesRowButton").objectReferenceValue = milestonesRow.GetComponent<Button>();
            so.FindProperty("settingsRowButton").objectReferenceValue = settingsRow.GetComponent<Button>();
            so.FindProperty("artifactsRowButton").objectReferenceValue = artifactsRow.GetComponent<Button>();
            so.FindProperty("contentBackButton").objectReferenceValue = backButton;
            so.FindProperty("rowList").objectReferenceValue = rowList.gameObject;
            so.FindProperty("crewPanel").objectReferenceValue = crewPanel;
            so.FindProperty("upgradesPanel").objectReferenceValue = upgradesPanel;
            so.FindProperty("captainsLogRow").objectReferenceValue = captainsLogRow;
            so.FindProperty("artifactsRow").objectReferenceValue = artifactsRow;
            so.ApplyModifiedProperties();

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("MenuDrawerBuilder: built '" + RootName + "' on " + PersistentCanvasName + " (starts off-screen — " +
                       "that's the closed state, not a bug). Crew/Upgrades rows open real panels; the rest still log.");
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

        // --- Crew panel (Menu -> Crew, HUD_AND_LANDING_COVE_LAYOUT.md §E) --------------

        private static GameObject BuildCrewPanel(Transform panel)
        {
            var root = CreateUIObject("CrewPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Crew", 20, CreamColor, TextAlignmentOptions.Center);

            var listContainerRt = CreateUIObject("ListContainer", root);
            Stretch(listContainerRt, new Vector2(16, 16), new Vector2(-16, -56));
            var listLayout = listContainerRt.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var itemPrefab = BuildAndSaveCrewItemPrefab();

            var panelController = root.gameObject.AddComponent<CrewMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("listContainer").objectReferenceValue = listContainerRt;
            so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }

        private static CrewListItemUI BuildAndSaveCrewItemPrefab()
        {
            var root = CreateUIObject("CrewListItem", null, registerUndo: false);
            root.sizeDelta = new Vector2(0, 60);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UISprite();
            bg.color = InkColor;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 60;

            var nameRt = CreateUIObject("NameText", root, registerUndo: false);
            Stretch(nameRt, new Vector2(12, 32), new Vector2(-110, -4));
            var nameText = AddText(nameRt.gameObject, "Name", 15, CreamColor, TextAlignmentOptions.BottomLeft);

            var rateRt = CreateUIObject("RateText", root, registerUndo: false);
            Stretch(rateRt, new Vector2(12, 6), new Vector2(-110, -32));
            var rateText = AddText(rateRt.gameObject, "Rate", 13, PlaceholderThumbColor, TextAlignmentOptions.TopLeft);

            var buttonRt = CreateUIObject("RecruitButton", root, registerUndo: false);
            Anchor(buttonRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(96, 48));
            var buttonImage = buttonRt.gameObject.AddComponent<Image>();
            buttonImage.sprite = UISprite();
            buttonImage.color = TealAccent;
            var button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var costRt = CreateUIObject("Label", buttonRt, registerUndo: false);
            Stretch(costRt, new Vector2(4, 2), new Vector2(-4, -2));
            var costText = AddText(costRt.gameObject, "Cost", 10, InkColor, TextAlignmentOptions.Center);

            var item = root.gameObject.AddComponent<CrewListItemUI>();
            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("nameText").objectReferenceValue = nameText;
            itemSo.FindProperty("rateText").objectReferenceValue = rateText;
            itemSo.FindProperty("costText").objectReferenceValue = costText;
            itemSo.FindProperty("recruitButton").objectReferenceValue = button;
            itemSo.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/CrewListItem.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<CrewListItemUI>();
        }

        // --- Upgrades panel (Menu -> Upgrades, HUD_AND_LANDING_COVE_LAYOUT.md §C) -------

        private static GameObject BuildUpgradesPanel(Transform panel)
        {
            var root = CreateUIObject("UpgradesPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Upgrades", 20, CreamColor, TextAlignmentOptions.Center);

            var tapRowRt = CreateUIObject("TapPowerRow", root);
            Anchor(tapRowRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-32, 72));
            var tapRowBg = tapRowRt.gameObject.AddComponent<Image>();
            tapRowBg.sprite = UISprite();
            tapRowBg.color = InkColor;

            var tapLabelRt = CreateUIObject("Label", tapRowRt);
            Stretch(tapLabelRt, new Vector2(12, 40), new Vector2(-12, -8));
            var tapLabel = AddText(tapLabelRt.gameObject, "Tap Power", 13, CreamColor, TextAlignmentOptions.TopLeft);

            var tapButtonRt = CreateUIObject("UpgradeButton", tapRowRt);
            Anchor(tapButtonRt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 8), new Vector2(140, 32));
            var tapButtonBg = tapButtonRt.gameObject.AddComponent<Image>();
            tapButtonBg.sprite = UISprite();
            tapButtonBg.color = TealAccent;
            var tapButton = tapButtonRt.gameObject.AddComponent<Button>();
            tapButton.targetGraphic = tapButtonBg;
            var tapButtonLabelRt = CreateUIObject("Label", tapButtonRt);
            Stretch(tapButtonLabelRt, Vector2.zero, Vector2.zero);
            AddText(tapButtonLabelRt.gameObject, "Upgrade", 14, InkColor, TextAlignmentOptions.Center);

            var panelController = root.gameObject.AddComponent<UpgradesMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("tapPowerLabel").objectReferenceValue = tapLabel;
            so.FindProperty("tapPowerButton").objectReferenceValue = tapButton;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }
    }
}
