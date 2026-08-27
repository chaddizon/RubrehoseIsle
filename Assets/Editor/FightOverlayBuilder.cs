using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rubrehose.Combat;
using static Rubrehose.EditorTools.RubrehoseEditorUtils;

namespace Rubrehose.EditorTools
{
    // Builds the serpent's screen-space fight overlay (IN_SCENE_FIGHT_SYSTEM.md) on the
    // shared PersistentUICanvas — a small floating panel (serpent name/stats, an HP bar, a
    // countdown timer) with NO backdrop and NO buttons, since the fight no longer takes over
    // the screen: the panel tracks the serpent's world position every frame
    // (FightController.PositionOverlay) instead of sitting fixed in the middle of it.
    //
    // This only builds the UI half — FightController itself lives on the Serpent GameObject
    // built by LandingCoveBuilder. Run that tool first or after this one (order doesn't
    // matter); this tool finds the existing FightController via FindFirstObjectByType and
    // wires the overlay refs into it.
    public static class FightOverlayBuilder
    {
        private const string RootName = "FightOverlay";
        private static readonly Vector2 PanelSize = new Vector2(240, 110);

        [MenuItem("Rubrehose/Build Persistent UI/Fight Overlay")]
        public static void Build()
        {
            WarnIfNoTmpFontAsset();

            var canvas = FindOrCreatePersistentCanvas();
            bool alreadyExists = canvas.transform.Find(RootName) != null;
            if (alreadyExists)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Rebuild Fight Overlay",
                    "This deletes and recreates the '" + RootName + "' hierarchy. Any manual tweaks will be lost. Continue?",
                    "Rebuild", "Cancel");
                if (!confirmed) return;
            }

            Undo.SetCurrentGroupName("Build Fight Overlay");
            int undoGroup = Undo.GetCurrentGroup();

            DestroyExistingChild(canvas.transform, RootName);

            // Anchored center/pivot-bottom so FightController.PositionOverlay can drive it
            // purely via anchoredPosition (the standard world-point-tracking-UI conversion) —
            // the pivot at the bottom edge means the panel floats just above whatever world
            // point it's aimed at, per the "anchored above the serpent's world position" spec.
            var rootRt = CreateUIObject(RootName, canvas.transform);
            Anchor(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, PanelSize);

            var panelBg = rootRt.gameObject.AddComponent<Image>();
            panelBg.sprite = UISprite();
            panelBg.color = InkColorTranslucent;

            var nameRt = CreateUIObject("SerpentName", rootRt);
            Anchor(nameRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(0, 22));
            var nameText = AddText(nameRt.gameObject, "Serpent", 16, CreamColor, TextAlignmentOptions.Center);

            var statsRt = CreateUIObject("Stats", rootRt);
            Anchor(statsRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(0, 18));
            var statsText = AddText(statsRt.gameObject, "HP 0 · Armor 0", 11, CreamColor, TextAlignmentOptions.Center);

            var sliderRt = CreateUIObject("HpSlider", rootRt);
            Anchor(sliderRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-24, 16));
            var sliderBg = sliderRt.gameObject.AddComponent<Image>();
            sliderBg.color = InkColorTranslucent;
            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var fillAreaRt = CreateUIObject("FillArea", sliderRt);
            Stretch(fillAreaRt, Vector2.zero, Vector2.zero);
            var fillRt = CreateUIObject("Fill", fillAreaRt);
            Anchor(fillRt, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.color = TealAccent;
            slider.fillRect = fillRt;

            var timerRt = CreateUIObject("Timer", rootRt);
            Anchor(timerRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(0, 20));
            var timerText = AddText(timerRt.gameObject, "30.0s", 14, InkColor, TextAlignmentOptions.Center);

            var fightController = Object.FindFirstObjectByType<FightController>();
            if (fightController == null)
            {
                Debug.LogWarning("FightOverlayBuilder: no FightController found in the scene — run " +
                                  "Rubrehose > Build Landing Cove first (it builds the Serpent/FightController), " +
                                  "then re-run this command, or wire the overlay fields onto it manually.");
            }
            else
            {
                var so = new SerializedObject(fightController);
                so.FindProperty("canvasRect").objectReferenceValue = canvas.GetComponent<RectTransform>();
                so.FindProperty("overlayRoot").objectReferenceValue = rootRt;
                so.FindProperty("serpentNameText").objectReferenceValue = nameText;
                so.FindProperty("statsText").objectReferenceValue = statsText;
                so.FindProperty("hpSlider").objectReferenceValue = slider;
                so.FindProperty("timerText").objectReferenceValue = timerText;
                so.ApplyModifiedProperties();
            }

            EnsureEventSystem();
            rootRt.gameObject.SetActive(false); // FightController.TryStartFight()/CloseOverlay() toggle it
            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = rootRt.gameObject;
            Debug.Log("FightOverlayBuilder: built '" + RootName + "' on " + PersistentCanvasName + ".");
        }
    }
}
