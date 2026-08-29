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
    // slide-in from the right edge, now with a full row list per NEXT_CLAUDE_CODE_PUSH.md §3
    // ("every system in the game" gets a reachable placeholder entry point). Real panels:
    // Crew, Upgrades, Buildings, Artifacts, Stats. Everything else is a shared "Coming soon"
    // stub panel (BuildStubPanel) except Settings, which gets a minimal real one
    // (SettingsMenuPanel). All click wiring lives on MenuDrawerController via its row-list
    // field (assigned here via SerializedProperty, not edit-time AddListener) so it survives
    // domain reload / Play mode.
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
            var crewPanel = BuildCrewPanel(panel);

            var upgradesRow = BuildRow(rowList, "Upgrades");
            var upgradesPanel = BuildUpgradesPanel(panel);

            var buildingsRow = BuildRow(rowList, "Buildings");
            var buildingsPanel = BuildBuildingsPanel(panel);

            var artifactsRow = BuildRow(rowList, "Artifacts");
            var artifactsPanel = BuildArtifactsPanel(panel);

            var bottleRow = BuildRow(rowList, "Message in a Bottle");
            var bottlePanel = BuildStubPanel(panel, "Message in a Bottle",
                "Cast a Net and Bottle Toss charges are tracked on the persistent HUD icons for now — a dedicated menu view is coming soon.");

            var captainsLogRow = BuildRow(rowList, "Captain's Log");
            var captainsLogPanel = BuildStubPanel(panel, "Captain's Log", "Coming soon.");

            var milestonesRow = BuildRow(rowList, "Milestones");
            var milestonesPanel = BuildStubPanel(panel, "Milestones", "Coming soon.");

            var tidepoolingRow = BuildRow(rowList, "Tidepooling");
            var tidepoolingPanel = BuildStubPanel(panel, "Tidepooling", "Coming soon.");

            var foragingRow = BuildRow(rowList, "Foraging");
            var foragingPanel = BuildStubPanel(panel, "Foraging", "Coming soon.");

            var postcardsRow = BuildRow(rowList, "Postcards");
            var postcardsPanel = BuildStubPanel(panel, "Postcards", "Coming soon.");

            var companionsRow = BuildRow(rowList, "Companions");
            var companionsPanel = BuildStubPanel(panel, "Companions", "Coming soon.");

            var statsRow = BuildRow(rowList, "Stats");
            var statsPanel = BuildStatsPanel(panel);

            var settingsRow = BuildRow(rowList, "Settings");
            var settingsPanel = BuildSettingsPanel(panel);

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
            so.FindProperty("contentBackButton").objectReferenceValue = backButton;
            so.FindProperty("rowList").objectReferenceValue = rowList.gameObject;
            so.FindProperty("captainsLogRow").objectReferenceValue = captainsLogRow;
            so.FindProperty("artifactsRow").objectReferenceValue = artifactsRow;

            var rowData = new (GameObject row, GameObject panel)[]
            {
                (crewRow, crewPanel),
                (upgradesRow, upgradesPanel),
                (buildingsRow, buildingsPanel),
                (artifactsRow, artifactsPanel),
                (bottleRow, bottlePanel),
                (captainsLogRow, captainsLogPanel),
                (milestonesRow, milestonesPanel),
                (tidepoolingRow, tidepoolingPanel),
                (foragingRow, foragingPanel),
                (postcardsRow, postcardsPanel),
                (companionsRow, companionsPanel),
                (statsRow, statsPanel),
                (settingsRow, settingsPanel),
            };

            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = rowData.Length;
            for (int i = 0; i < rowData.Length; i++)
            {
                var element = rowsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = rowData[i].row.name;
                element.FindPropertyRelative("button").objectReferenceValue = rowData[i].row.GetComponent<Button>();
                element.FindPropertyRelative("panel").objectReferenceValue = rowData[i].panel;
            }
            so.ApplyModifiedProperties();

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("MenuDrawerBuilder: built '" + RootName + "' on " + PersistentCanvasName + " with " + rowData.Length +
                       " rows (starts off-screen — that's the closed state, not a bug). Crew/Upgrades/Buildings/Artifacts/Stats " +
                       "rows open real panels; the rest are 'Coming soon' stubs (Settings has a minimal real sound toggle).");
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

        // --- Buildings panel (Menu -> Buildings, CORE_PROGRESSION_RESTRUCTURE.md "Cove
        // Buildings") -------------------------------------------------------------------

        private static GameObject BuildBuildingsPanel(Transform panel)
        {
            var root = CreateUIObject("BuildingsPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Buildings", 20, CreamColor, TextAlignmentOptions.Center);

            var listContainerRt = CreateUIObject("ListContainer", root);
            Stretch(listContainerRt, new Vector2(16, 16), new Vector2(-16, -56));
            var listLayout = listContainerRt.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var itemPrefab = BuildAndSaveBuildingItemPrefab();

            var panelController = root.gameObject.AddComponent<BuildingsMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("listContainer").objectReferenceValue = listContainerRt;
            so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }

        private static BuildingListItemUI BuildAndSaveBuildingItemPrefab()
        {
            var root = CreateUIObject("BuildingListItem", null, registerUndo: false);
            root.sizeDelta = new Vector2(0, 76);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UISprite();
            bg.color = InkColor;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 76;

            var nameRt = CreateUIObject("NameText", root, registerUndo: false);
            Stretch(nameRt, new Vector2(12, 44), new Vector2(-12, -4));
            var nameText = AddText(nameRt.gameObject, "Name", 15, CreamColor, TextAlignmentOptions.BottomLeft);

            var statusRt = CreateUIObject("StatusText", root, registerUndo: false);
            Stretch(statusRt, new Vector2(12, 4), new Vector2(-12, -22));
            var statusText = AddText(statusRt.gameObject, "Status", 12, PlaceholderThumbColor, TextAlignmentOptions.TopLeft);
            statusText.enableWordWrapping = true;

            var buttonRt = CreateUIObject("PayButton", root, registerUndo: false);
            Anchor(buttonRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(120, 28));
            var buttonImage = buttonRt.gameObject.AddComponent<Image>();
            buttonImage.sprite = UISprite();
            buttonImage.color = TealAccent;
            var button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var buttonLabelRt = CreateUIObject("Label", buttonRt, registerUndo: false);
            Stretch(buttonLabelRt, Vector2.zero, Vector2.zero);
            AddText(buttonLabelRt.gameObject, "Build", 13, InkColor, TextAlignmentOptions.Center);

            var item = root.gameObject.AddComponent<BuildingListItemUI>();
            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("nameText").objectReferenceValue = nameText;
            itemSo.FindProperty("statusText").objectReferenceValue = statusText;
            itemSo.FindProperty("payButton").objectReferenceValue = button;
            itemSo.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/BuildingListItem.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<BuildingListItemUI>();
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

        // --- Shared stub panel (NEXT_CLAUDE_CODE_PUSH.md §3: "don't invent a second panel
        // style for stubs, reuse the one real pattern everywhere") ----------------------

        private static GameObject BuildStubPanel(Transform panel, string title, string body)
        {
            var root = CreateUIObject(title.Replace(" ", "").Replace("'", "") + "Panel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, title, 20, CreamColor, TextAlignmentOptions.Center);

            var bodyRt = CreateUIObject("Body", root);
            Stretch(bodyRt, new Vector2(24, 24), new Vector2(-24, -56));
            var bodyText = AddText(bodyRt.gameObject, body, 15, PlaceholderThumbColor, TextAlignmentOptions.Top);
            bodyText.enableWordWrapping = true;

            return root.gameObject;
        }

        // --- Artifacts panel (Menu -> Artifacts, NEXT_CLAUDE_CODE_PUSH.md §1b) ----------

        private static GameObject BuildArtifactsPanel(Transform panel)
        {
            var root = CreateUIObject("ArtifactsPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Artifacts", 20, CreamColor, TextAlignmentOptions.Center);

            var shardHeaderRt = CreateUIObject("ShardsHeader", root);
            Anchor(shardHeaderRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-32, 20));
            AddText(shardHeaderRt.gameObject, "Compass Shards", 14, PlaceholderThumbColor, TextAlignmentOptions.MidlineLeft);

            var shardListRt = CreateUIObject("ShardList", root);
            Anchor(shardListRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(-32, 200));
            var shardLayout = shardListRt.gameObject.AddComponent<VerticalLayoutGroup>();
            shardLayout.spacing = 6;
            shardLayout.childControlWidth = true;
            shardLayout.childControlHeight = false;
            shardLayout.childForceExpandWidth = true;
            shardLayout.childForceExpandHeight = false;

            var nodesHeaderRt = CreateUIObject("NodesHeader", root);
            Anchor(nodesHeaderRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -288), new Vector2(-32, 20));
            AddText(nodesHeaderRt.gameObject, "Recovered Pieces", 14, PlaceholderThumbColor, TextAlignmentOptions.MidlineLeft);

            var nodeListRt = CreateUIObject("NodeList", root);
            Stretch(nodeListRt, new Vector2(16, 16), new Vector2(-16, -312));
            var nodeLayout = nodeListRt.gameObject.AddComponent<VerticalLayoutGroup>();
            nodeLayout.spacing = 8;
            nodeLayout.childControlWidth = true;
            nodeLayout.childControlHeight = false;
            nodeLayout.childForceExpandWidth = true;
            nodeLayout.childForceExpandHeight = false;

            var shardItemPrefab = BuildAndSaveShardItemPrefab();
            var nodeItemPrefab = BuildAndSaveArtifactNodeItemPrefab();

            var panelController = root.gameObject.AddComponent<ArtifactsMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("shardListContainer").objectReferenceValue = shardListRt;
            so.FindProperty("shardItemPrefab").objectReferenceValue = shardItemPrefab;
            so.FindProperty("nodeListContainer").objectReferenceValue = nodeListRt;
            so.FindProperty("nodeItemPrefab").objectReferenceValue = nodeItemPrefab;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }

        private static ShardStackItemUI BuildAndSaveShardItemPrefab()
        {
            var root = CreateUIObject("ShardStackItem", null, registerUndo: false);
            root.sizeDelta = new Vector2(0, 48);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UISprite();
            bg.color = InkColor;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 48;

            var nameRt = CreateUIObject("NameText", root, registerUndo: false);
            Stretch(nameRt, new Vector2(12, 24), new Vector2(-100, -4));
            var nameText = AddText(nameRt.gameObject, "Tier", 14, CreamColor, TextAlignmentOptions.BottomLeft);

            var countsRt = CreateUIObject("CountsText", root, registerUndo: false);
            Stretch(countsRt, new Vector2(12, 4), new Vector2(-100, -24));
            var countsText = AddText(countsRt.gameObject, "Counts", 11, PlaceholderThumbColor, TextAlignmentOptions.TopLeft);

            var buttonRt = CreateUIObject("AppraiseButton", root, registerUndo: false);
            Anchor(buttonRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(84, 36));
            var buttonImage = buttonRt.gameObject.AddComponent<Image>();
            buttonImage.sprite = UISprite();
            buttonImage.color = TealAccent;
            var button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonLabelRt = CreateUIObject("Label", buttonRt, registerUndo: false);
            Stretch(buttonLabelRt, Vector2.zero, Vector2.zero);
            AddText(buttonLabelRt.gameObject, "Appraise", 12, InkColor, TextAlignmentOptions.Center);

            var item = root.gameObject.AddComponent<ShardStackItemUI>();
            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("nameText").objectReferenceValue = nameText;
            itemSo.FindProperty("countsText").objectReferenceValue = countsText;
            itemSo.FindProperty("appraiseButton").objectReferenceValue = button;
            itemSo.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/ShardStackItem.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ShardStackItemUI>();
        }

        private static ArtifactNodeUI BuildAndSaveArtifactNodeItemPrefab()
        {
            var root = CreateUIObject("ArtifactNodeItem", null, registerUndo: false);
            root.sizeDelta = new Vector2(0, 76);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UISprite();
            bg.color = InkColor;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 76;

            var nameRt = CreateUIObject("NameText", root, registerUndo: false);
            Stretch(nameRt, new Vector2(12, 44), new Vector2(-12, -4));
            var nameText = AddText(nameRt.gameObject, "Name", 14, CreamColor, TextAlignmentOptions.BottomLeft);

            var statusRt = CreateUIObject("StatusText", root, registerUndo: false);
            Stretch(statusRt, new Vector2(12, 4), new Vector2(-12, -22));
            var statusText = AddText(statusRt.gameObject, "Status", 11, PlaceholderThumbColor, TextAlignmentOptions.TopLeft);
            statusText.enableWordWrapping = true;

            var buttonRt = CreateUIObject("PurchaseButton", root, registerUndo: false);
            Anchor(buttonRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(110, 28));
            var buttonImage = buttonRt.gameObject.AddComponent<Image>();
            buttonImage.sprite = UISprite();
            buttonImage.color = TealAccent;
            var button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonLabelRt = CreateUIObject("Label", buttonRt, registerUndo: false);
            Stretch(buttonLabelRt, Vector2.zero, Vector2.zero);
            AddText(buttonLabelRt.gameObject, "Recover", 12, InkColor, TextAlignmentOptions.Center);

            var item = root.gameObject.AddComponent<ArtifactNodeUI>();
            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("nameText").objectReferenceValue = nameText;
            itemSo.FindProperty("statusText").objectReferenceValue = statusText;
            itemSo.FindProperty("purchaseButton").objectReferenceValue = button;
            itemSo.ApplyModifiedProperties();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/ArtifactNodeItem.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ArtifactNodeUI>();
        }

        // --- Stats panel (Menu -> Stats, NEXT_CLAUDE_CODE_PUSH.md §3: "raw numbers dump is
        // fine") -------------------------------------------------------------------------

        private static GameObject BuildStatsPanel(Transform panel)
        {
            var root = CreateUIObject("StatsPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Stats", 20, CreamColor, TextAlignmentOptions.Center);

            var bodyRt = CreateUIObject("Body", root);
            Stretch(bodyRt, new Vector2(24, 24), new Vector2(-24, -56));
            var bodyText = AddText(bodyRt.gameObject, "Loading...", 15, CreamColor, TextAlignmentOptions.TopLeft);
            bodyText.enableWordWrapping = true;

            var panelController = root.gameObject.AddComponent<StatsMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("bodyText").objectReferenceValue = bodyText;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }

        // --- Settings panel (Menu -> Settings, NEXT_CLAUDE_CODE_PUSH.md §3: "Sound toggle,
        // reset/save stub, credits") --------------------------------------------------------

        private static GameObject BuildSettingsPanel(Transform panel)
        {
            var root = CreateUIObject("SettingsPanel", panel);
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.SetActive(false);

            var titleRt = CreateUIObject("Title", root);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 32));
            AddText(titleRt.gameObject, "Settings", 20, CreamColor, TextAlignmentOptions.Center);

            var soundRowRt = CreateUIObject("SoundRow", root);
            Anchor(soundRowRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-32, 40));
            var soundLabelRt = CreateUIObject("Label", soundRowRt);
            Anchor(soundLabelRt, Vector2.zero, new Vector2(0.6f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            AddText(soundLabelRt.gameObject, "Sound", 15, CreamColor, TextAlignmentOptions.MidlineLeft);

            var toggleRt = CreateUIObject("Toggle", soundRowRt);
            Anchor(toggleRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(40, 24));
            var toggleBg = toggleRt.gameObject.AddComponent<Image>();
            toggleBg.sprite = UISprite();
            toggleBg.color = InkColor;
            var toggle = toggleRt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            var checkRt = CreateUIObject("Checkmark", toggleRt);
            Stretch(checkRt, new Vector2(4, 4), new Vector2(-4, -4));
            var checkImage = checkRt.gameObject.AddComponent<Image>();
            checkImage.color = TealAccent;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            var resetRt = CreateUIObject("ResetSaveButton", root);
            Anchor(resetRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(-32, 40));
            var resetImage = resetRt.gameObject.AddComponent<Image>();
            resetImage.sprite = UISprite();
            resetImage.color = InkColor;
            var resetButton = resetRt.gameObject.AddComponent<Button>();
            resetButton.targetGraphic = resetImage;
            var resetLabelRt = CreateUIObject("Label", resetRt);
            Stretch(resetLabelRt, Vector2.zero, Vector2.zero);
            AddText(resetLabelRt.gameObject, "Reset Save", 14, CreamColor, TextAlignmentOptions.Center);

            var creditsRt = CreateUIObject("Credits", root);
            Stretch(creditsRt, new Vector2(24, 24), new Vector2(-24, -168));
            var creditsText = AddText(creditsRt.gameObject, "Rubrehose Isle\nMade by Chad.\n\"BE BAD!\"", 13, PlaceholderThumbColor, TextAlignmentOptions.Top);
            creditsText.enableWordWrapping = true;

            var panelController = root.gameObject.AddComponent<SettingsMenuPanel>();
            var so = new SerializedObject(panelController);
            so.FindProperty("soundToggle").objectReferenceValue = toggle;
            so.FindProperty("resetSaveButton").objectReferenceValue = resetButton;
            so.ApplyModifiedProperties();

            return root.gameObject;
        }
    }
}
