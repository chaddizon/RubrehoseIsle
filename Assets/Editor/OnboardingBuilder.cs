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
    // Builds the onboarding popup (CORE_PROGRESSION_RESTRUCTURE.md "Onboarding / tutorial
    // system") on the shared PersistentUICanvas, styled to match MenuDrawerBuilder's established
    // comic-panel look (Ink panel, Cream text, Teal accent button) rather than a generic system
    // dialog. Deliberately NOT a full-screen catcher like the menu drawer's — this popup sits
    // near the bottom of the screen and everything else stays tappable underneath it, per the
    // doc's "non-blocking beyond" a single dismiss tap.
    //
    // A literal checkerboard-pattern texture (the doc's other named motif, alongside
    // "comic-panel") isn't built here — Assets/Art/UI/ is still an empty placeholder folder
    // waiting on Chad's real "checkerboard menu icon" art (rubrehose_art_checklist.md); baking
    // a procedural stand-in into that same folder would collide with that pending asset.
    public static class OnboardingBuilder
    {
        private const string RootName = "OnboardingPopup";
        private const float PanelWidth = 620f;

        [MenuItem("Rubrehose/Build Persistent UI/Onboarding Popup")]
        public static void Build()
        {
            WarnIfNoTmpFontAsset();

            var canvas = FindOrCreatePersistentCanvas();
            bool alreadyExists = canvas.transform.Find(RootName) != null;
            if (alreadyExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Onboarding Popup",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Onboarding Popup");
            int undoGroup = Undo.GetCurrentGroup();

            DestroyExistingChild(canvas.transform, RootName);

            var rootRt = CreateUIObject(RootName, canvas.transform);
            Anchor(rootRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 48), new Vector2(PanelWidth, 220));
            var canvasGroup = rootRt.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var panelBg = rootRt.gameObject.AddComponent<Image>();
            panelBg.sprite = UISprite();
            panelBg.color = InkColorTranslucent;

            var titleRt = CreateUIObject("Title", rootRt);
            Anchor(titleRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(-40, 32));
            var titleText = AddText(titleRt.gameObject, "Title", 20, CreamColor, TextAlignmentOptions.Center);

            var bodyRt = CreateUIObject("Body", rootRt);
            Anchor(bodyRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-64, 96));
            var bodyText = AddText(bodyRt.gameObject, "Body copy goes here.", 16, PlaceholderThumbColor, TextAlignmentOptions.Top);
            bodyText.enableWordWrapping = true;

            var dismissRt = CreateUIObject("DismissButton", rootRt);
            Anchor(dismissRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(160, 40));
            var dismissBg = dismissRt.gameObject.AddComponent<Image>();
            dismissBg.sprite = UISprite();
            dismissBg.color = TealAccent;
            var dismissButton = dismissRt.gameObject.AddComponent<Button>();
            dismissButton.targetGraphic = dismissBg;
            var dismissLabelRt = CreateUIObject("Label", dismissRt);
            Stretch(dismissLabelRt, Vector2.zero, Vector2.zero);
            AddText(dismissLabelRt.gameObject, "Got it", 16, InkColor, TextAlignmentOptions.Center);

            var popupUI = Undo.AddComponent<OnboardingPopupUI>(rootRt.gameObject);
            var popupSo = new SerializedObject(popupUI);
            popupSo.FindProperty("root").objectReferenceValue = rootRt.gameObject;
            popupSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            popupSo.FindProperty("titleText").objectReferenceValue = titleText;
            popupSo.FindProperty("bodyText").objectReferenceValue = bodyText;
            popupSo.FindProperty("dismissButton").objectReferenceValue = dismissButton;
            popupSo.ApplyModifiedProperties();

            var controllerGo = new GameObject("OnboardingController");
            Undo.RegisterCreatedObjectUndo(controllerGo, "Build Onboarding Popup");
            var controller = controllerGo.AddComponent<OnboardingController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("popupUI").objectReferenceValue = popupUI;
            controllerSo.ApplyModifiedProperties();

            EnsureEventSystem();
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("OnboardingBuilder: built '" + RootName + "' on " + PersistentCanvasName + " plus a standalone " +
                       "'OnboardingController' GameObject wired to it. Starts hidden (OnboardingPopupUI.Awake) — that's " +
                       "correct, not a bug; it only appears once a popup actually queues.");
        }
    }
}
