using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.UI.Editor
{
    /// <summary>
    /// Builds the judge speech UI and wires the feedback sequence in the active scene.
    /// </summary>
    [InitializeOnLoad]
    internal static class BuildJudgeFeedbackUI
    {
        private const string SpeechRootName = "JudgeSpeechUI";
        private const string SequenceObjectName = "JudgeFeedbackSequence";
        private const string JudgesTopDownOverviewPointName = "JudgesTopDownOverviewPoint";
        private const string UserInputCameraPoseName = "UserInputCameraPose";
        private const string SampleJudgeName = "Juez de Marketing";
        private const string SampleSpeech = "Tu propuesta tiene una idea clara y un mercado atractivo. ¿Cómo planeas conseguir tus primeros clientes?";
        private const int JudgeCount = 3;
        private static readonly string[] JudgeCameraPointNames =
        {
            "Judge1CameraPose",
            "Judge2CameraPose",
            "Judge3CameraPose"
        };
        private static readonly string[] ChairNames =
        {
            "Chair_Judge-1",
            "Chair_Judge-2",
            "Chair_Judge-3"
        };

        static BuildJudgeFeedbackUI()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.delayCall += PreviewOpenScene;
        }

        private static void HandleSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += PreviewOpenScene;
        }

        private static void PreviewOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            JudgeSpeechUI speechUI = Object.FindAnyObjectByType<JudgeSpeechUI>();
            if (speechUI == null)
            {
                return;
            }

            SerializedObject serializedSpeechUI = new SerializedObject(speechUI);
            CanvasGroup rootGroup = serializedSpeechUI.FindProperty("rootGroup").objectReferenceValue as CanvasGroup;
            if (rootGroup == null)
            {
                return;
            }

            ApplyPreviewState(
                rootGroup,
                serializedSpeechUI.FindProperty("nameLabel").objectReferenceValue as TMP_Text,
                serializedSpeechUI.FindProperty("bodyText").objectReferenceValue as TMP_Text,
                serializedSpeechUI.FindProperty("arrowButton").objectReferenceValue as Button,
                serializedSpeechUI.FindProperty("continueButton").objectReferenceValue as Button,
                serializedSpeechUI.FindProperty("continueGroup").objectReferenceValue as CanvasGroup,
                true);
        }

        [MenuItem("SharkTank/Build Judge Feedback UI")]
        private static void Build()
        {
            UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
            Canvas canvas = BuildUserInputScreenUI.GetSharedCanvas(activeScene);
            if (canvas == null)
            {
                return;
            }

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                Debug.LogError("No default TextMeshPro font asset is configured. Assign one in TMP Settings before building the judge UI.");
                return;
            }

            Transform existingRoot = canvas.transform.Find(SpeechRootName);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot.gameObject);
            }

            Sprite builtInSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            GameObject speechRoot = new GameObject(SpeechRootName, typeof(RectTransform), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(speechRoot, "Build Judge Feedback UI");
            speechRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = speechRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CanvasGroup rootGroup = speechRoot.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            JudgeSpeechUI speechUI = speechRoot.AddComponent<JudgeSpeechUI>();

            RectTransform speechPanel = CreateRect(
                "SpeechPanel",
                speechRoot.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-150f, 30f),
                new Vector2(680f, 430f));
            Image panelImage = speechPanel.gameObject.AddComponent<Image>();
            panelImage.sprite = builtInSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = UITheme.PanelAccent;

            RectTransform panelBackground = CreateRect(
                "PanelBackground",
                speechPanel,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-6f, -6f));
            Image panelBackgroundImage = panelBackground.gameObject.AddComponent<Image>();
            panelBackgroundImage.sprite = builtInSprite;
            panelBackgroundImage.type = Image.Type.Sliced;
            panelBackgroundImage.color = UITheme.PanelBackground;
            panelBackgroundImage.raycastTarget = false;

            Button panelButton = speechPanel.gameObject.AddComponent<Button>();
            ConfigureButton(panelButton, panelImage);
            AddButtonPressColor(
                speechPanel.gameObject,
                panelImage,
                UITheme.PanelAccent,
                UITheme.PanelAccent,
                UITheme.PanelAccent);

            TMP_Text nameLabel = CreateText(
                "NameLabel",
                speechPanel,
                defaultFont,
                "Juez",
                26f,
                UITheme.TextDark,
                TextAlignmentOptions.Left,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(30f, -24f),
                new Vector2(-60f, 52f));
            TMP_Text bodyText = CreateText(
                "BodyText",
                speechPanel,
                defaultFont,
                string.Empty,
                34f,
                UITheme.TextDark,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(30f, -20f),
                new Vector2(-60f, -150f));
            bodyText.gameObject.AddComponent<SpeechTypewriter>();
            SpeechTypewriter typewriter = bodyText.GetComponent<SpeechTypewriter>();
            SerializedObject serializedTypewriter = new SerializedObject(typewriter);
            serializedTypewriter.FindProperty("targetText").objectReferenceValue = bodyText;
            serializedTypewriter.ApplyModifiedPropertiesWithoutUndo();

            RectTransform arrowRect = CreateRect(
                "ArrowButton",
                speechPanel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 24f),
                new Vector2(64f, 64f));
            Image arrowImage = arrowRect.gameObject.AddComponent<Image>();
            arrowImage.sprite = builtInSprite;
            arrowImage.type = Image.Type.Sliced;
            arrowImage.color = UITheme.Accent;
            Button arrowButton = arrowRect.gameObject.AddComponent<Button>();
            ConfigureButton(arrowButton, arrowImage);
            AddButtonPressColor(arrowRect.gameObject, arrowImage, UITheme.Accent, UITheme.ButtonHover, UITheme.ButtonPressed);
            CreateText(
                "ArrowLabel",
                arrowRect,
                defaultFont,
                "›",
                42f,
                UITheme.TextCream,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            arrowRect.gameObject.SetActive(false);

            RectTransform continueRect = CreateRect(
                "ContinueButton",
                speechRoot.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-150f, -220f),
                new Vector2(250f, 72f));
            Image continueImage = continueRect.gameObject.AddComponent<Image>();
            continueImage.sprite = builtInSprite;
            continueImage.type = Image.Type.Sliced;
            continueImage.color = UITheme.Accent;
            Button continueButton = continueRect.gameObject.AddComponent<Button>();
            ConfigureButton(continueButton, continueImage);
            AddButtonPressColor(continueRect.gameObject, continueImage, UITheme.Accent, UITheme.ButtonHover, UITheme.ButtonPressed);
            CanvasGroup continueGroup = continueRect.gameObject.AddComponent<CanvasGroup>();
            continueGroup.alpha = 0f;
            continueGroup.interactable = false;
            continueGroup.blocksRaycasts = false;
            CreateText(
                "ContinueLabel",
                continueRect,
                defaultFont,
                "CONTINUAR",
                24f,
                UITheme.TextCream,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            continueButton.interactable = false;

            SerializedObject serializedSpeechUI = new SerializedObject(speechUI);
            serializedSpeechUI.FindProperty("rootGroup").objectReferenceValue = rootGroup;
            serializedSpeechUI.FindProperty("speechPanelButton").objectReferenceValue = panelButton;
            serializedSpeechUI.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            serializedSpeechUI.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedSpeechUI.FindProperty("typewriter").objectReferenceValue = typewriter;
            serializedSpeechUI.FindProperty("arrowButton").objectReferenceValue = arrowButton;
            serializedSpeechUI.FindProperty("continueButton").objectReferenceValue = continueButton;
            serializedSpeechUI.FindProperty("continueGroup").objectReferenceValue = continueGroup;
            serializedSpeechUI.ApplyModifiedPropertiesWithoutUndo();

            nameLabel.text = SampleJudgeName;
            ApplyPreviewState(rootGroup, nameLabel, bodyText, arrowButton, continueButton, continueGroup, true);
            ConfigureSequence(activeScene, speechUI);
            EditorSceneManager.MarkSceneDirty(activeScene);

            Selection.activeGameObject = bodyText.gameObject;
            EditorGUIUtility.PingObject(bodyText);
            Debug.Log("Judge speech UI preview is visible. Edit BodyText under Canvas/JudgeSpeechUI/SpeechPanel to adjust the speech appearance.");
        }

        [MenuItem("SharkTank/Preview Judge Speech UI")]
        private static void PreviewExisting()
        {
            SetExistingPreview(true);
        }

        [MenuItem("SharkTank/Hide Judge Speech UI Preview")]
        private static void HideExistingPreview()
        {
            SetExistingPreview(false);
        }

        private static void SetExistingPreview(bool visible)
        {
            JudgeSpeechUI speechUI = Object.FindAnyObjectByType<JudgeSpeechUI>();
            if (speechUI == null)
            {
                Debug.LogWarning("No JudgeSpeechUI was found in the open scene. Build the Judge Feedback UI first.");
                return;
            }

            SerializedObject serializedSpeechUI = new SerializedObject(speechUI);
            CanvasGroup rootGroup = serializedSpeechUI.FindProperty("rootGroup").objectReferenceValue as CanvasGroup;
            TMP_Text nameLabel = serializedSpeechUI.FindProperty("nameLabel").objectReferenceValue as TMP_Text;
            TMP_Text bodyText = serializedSpeechUI.FindProperty("bodyText").objectReferenceValue as TMP_Text;
            Button arrowButton = serializedSpeechUI.FindProperty("arrowButton").objectReferenceValue as Button;
            Button continueButton = serializedSpeechUI.FindProperty("continueButton").objectReferenceValue as Button;
            CanvasGroup continueGroup = serializedSpeechUI.FindProperty("continueGroup").objectReferenceValue as CanvasGroup;

            if (rootGroup == null)
            {
                Debug.LogWarning("JudgeSpeechUI needs its Root Group reference assigned before it can be previewed.", speechUI);
                return;
            }

            List<Object> undoTargets = new List<Object> { rootGroup };
            AddUndoTarget(undoTargets, nameLabel);
            AddUndoTarget(undoTargets, bodyText);
            AddUndoTarget(undoTargets, arrowButton != null ? arrowButton.gameObject : null);
            AddUndoTarget(undoTargets, continueButton != null ? continueButton.gameObject : null);
            AddUndoTarget(undoTargets, continueGroup);
            Undo.RecordObjects(undoTargets.ToArray(), visible ? "Preview Judge Speech UI" : "Hide Judge Speech UI Preview");

            ApplyPreviewState(rootGroup, nameLabel, bodyText, arrowButton, continueButton, continueGroup, visible);
            EditorSceneManager.MarkSceneDirty(speechUI.gameObject.scene);

            if (visible)
            {
                GameObject selectionTarget = bodyText != null ? bodyText.gameObject : speechUI.gameObject;
                Selection.activeGameObject = selectionTarget;
                EditorGUIUtility.PingObject(selectionTarget);
                Debug.Log("Judge speech UI preview is visible. Edit BodyText under Canvas/JudgeSpeechUI/SpeechPanel to adjust the speech appearance.");
            }
        }

        private static void ApplyPreviewState(
            CanvasGroup rootGroup,
            TMP_Text nameLabel,
            TMP_Text bodyText,
            Button arrowButton,
            Button continueButton,
            CanvasGroup continueGroup,
            bool visible)
        {
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            if (visible)
            {
                if (nameLabel != null && string.IsNullOrWhiteSpace(nameLabel.text))
                {
                    nameLabel.text = SampleJudgeName;
                }

                if (bodyText != null)
                {
                    if (string.IsNullOrWhiteSpace(bodyText.text))
                    {
                        bodyText.text = SampleSpeech;
                    }

                    bodyText.maxVisibleCharacters = int.MaxValue;
                }
            }

            if (arrowButton != null)
            {
                arrowButton.gameObject.SetActive(visible);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(visible);
            }

            if (continueGroup != null)
            {
                continueGroup.alpha = visible ? 1f : 0f;
                continueGroup.interactable = false;
                continueGroup.blocksRaycasts = false;
            }
        }

        private static void AddUndoTarget(List<Object> targets, Object target)
        {
            if (target != null)
            {
                targets.Add(target);
            }
        }

        private static void ConfigureSequence(UnityEngine.SceneManagement.Scene activeScene, JudgeSpeechUI speechUI)
        {
            List<string> missingReferences = new List<string>();
            GameObject sequenceObject = GameObject.Find(SequenceObjectName);
            if (sequenceObject == null)
            {
                sequenceObject = new GameObject(SequenceObjectName);
                Undo.RegisterCreatedObjectUndo(sequenceObject, "Create Judge Feedback Sequence");
            }

            JudgeFeedbackSequence sequence = sequenceObject.GetComponent<JudgeFeedbackSequence>();
            if (sequence == null)
            {
                sequence = Undo.AddComponent<JudgeFeedbackSequence>(sequenceObject);
            }

            JudgeChairsController chairsController = sequenceObject.GetComponent<JudgeChairsController>();
            if (chairsController == null)
            {
                chairsController = Undo.AddComponent<JudgeChairsController>(sequenceObject);
            }

            UserInputScreen cameraController = Object.FindAnyObjectByType<UserInputScreen>();
            if (cameraController == null)
            {
                missingReferences.Add("Assign the existing UserInputScreen as the camera controller.");
            }

            GameObject overviewObject = GameObject.Find("JudgesOverviewPoint");
            Transform overviewPoint = overviewObject != null ? overviewObject.transform : null;
            if (overviewPoint == null)
            {
                missingReferences.Add("Assign Judges Overview Point: create or select a camera pose framing all three judges, then assign it to Judges Overview Point.");
            }

            GameObject topDownOverviewObject = GameObject.Find(JudgesTopDownOverviewPointName);
            Transform topDownOverviewPoint = topDownOverviewObject != null ? topDownOverviewObject.transform : null;
            if (topDownOverviewPoint == null)
            {
                missingReferences.Add("Assign the existing " + JudgesTopDownOverviewPointName + " transform.");
            }

            GameObject userInputCameraObject = GameObject.Find(UserInputCameraPoseName);
            Transform userInputCameraPose = userInputCameraObject != null ? userInputCameraObject.transform : null;
            if (userInputCameraPose == null)
            {
                missingReferences.Add("Assign the existing " + UserInputCameraPoseName + " transform.");
            }

            Transform[] judgeCameraPoints = new Transform[JudgeCount];
            for (int i = 0; i < JudgeCount; i++)
            {
                GameObject poseObject = GameObject.Find(JudgeCameraPointNames[i]);
                judgeCameraPoints[i] = poseObject != null ? poseObject.transform : null;
                if (judgeCameraPoints[i] == null)
                {
                    missingReferences.Add("Assign the existing " + JudgeCameraPointNames[i] + " transform.");
                }
            }

            List<GameObject> chairObjects = new List<GameObject>();
            for (int i = 0; i < ChairNames.Length; i++)
            {
                GameObject chairObject = GameObject.Find(ChairNames[i]);
                if (chairObject == null)
                {
                    missingReferences.Add("Assign the " + ChairNames[i] + " chair root.");
                    continue;
                }

                chairObjects.Add(chairObject);
            }

            chairObjects.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
            JudgeChairMover[] chairMovers = new JudgeChairMover[JudgeCount];
            for (int i = 0; i < chairMovers.Length && i < chairObjects.Count; i++)
            {
                chairMovers[i] = chairObjects[i].GetComponent<JudgeChairMover>();
                if (chairMovers[i] == null)
                {
                    chairMovers[i] = Undo.AddComponent<JudgeChairMover>(chairObjects[i]);
                }
            }

            SerializedObject serializedChairsController = new SerializedObject(chairsController);
            serializedChairsController.FindProperty("leftChairMover").objectReferenceValue = chairMovers[0];
            serializedChairsController.FindProperty("middleChairMover").objectReferenceValue = chairMovers[1];
            serializedChairsController.FindProperty("rightChairMover").objectReferenceValue = chairMovers[2];
            serializedChairsController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedSequence = new SerializedObject(sequence);
            serializedSequence.FindProperty("cameraController").objectReferenceValue = cameraController;
            serializedSequence.FindProperty("judgesOverviewPoint").objectReferenceValue = overviewPoint;
            serializedSequence.FindProperty("judgesTopDownOverviewPoint").objectReferenceValue = topDownOverviewPoint;
            serializedSequence.FindProperty("userInputCameraPose").objectReferenceValue = userInputCameraPose;
            SerializedProperty cameraPointArray = serializedSequence.FindProperty("judgeCameraPoints");
            cameraPointArray.arraySize = JudgeCount;
            for (int i = 0; i < JudgeCount; i++)
            {
                cameraPointArray.GetArrayElementAtIndex(i).objectReferenceValue = judgeCameraPoints[i];
            }

            serializedSequence.FindProperty("chairsController").objectReferenceValue = chairsController;
            serializedSequence.FindProperty("speechUI").objectReferenceValue = speechUI;
            serializedSequence.ApplyModifiedPropertiesWithoutUndo();

            if (missingReferences.Count > 0)
            {
                StringBuilder message = new StringBuilder("Judge feedback UI built. Assign these scene references by hand:");
                for (int i = 0; i < missingReferences.Count; i++)
                {
                    message.Append("\n - ").Append(missingReferences[i]);
                }

                Debug.LogWarning(message.ToString(), sequenceObject);
            }
            else
            {
                Debug.Log("Judge feedback UI built and scene references assigned.", sequenceObject);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        private static void ConfigureButton(Button button, Graphic target)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private static void AddButtonPressColor(GameObject targetObject, Graphic targetGraphic,
            Color normalColor, Color hoverColor, Color pressedColor)
        {
            ButtonPressColor pressColor = targetObject.AddComponent<ButtonPressColor>();
            SerializedObject serializedPressColor = new SerializedObject(pressColor);
            serializedPressColor.FindProperty("target").objectReferenceValue = targetGraphic;
            serializedPressColor.FindProperty("normalColor").colorValue = normalColor;
            serializedPressColor.FindProperty("hoverColor").colorValue = hoverColor;
            serializedPressColor.FindProperty("pressedColor").colorValue = pressedColor;
            serializedPressColor.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset font, string text,
            float fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform textRect = CreateRect(objectName, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            TMP_Text textComponent = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            return rectTransform;
        }
    }
}
