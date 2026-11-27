using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SharkTank.UI;

namespace SharkTank.UI.Editor
{
    /// <summary>
    /// Creates the user input screen UI in the active scene.
    /// </summary>
    internal static class BuildUserInputScreenUI
    {
        private const string CanvasName = "Canvas";
        private const string UserInputUIName = "UserInputUI";
        private const string LegacyUserInputUIName = "UserInput";
        private const string ControllerObjectName = "UserInputController";
        private const string LegacyControllerObjectName = "UserInputScreenController";
        private const string SampleJudgeQuestion = "¿Cuáles son tus objetivos principales con este proyecto?";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly string[] JudgeCameraPoseNames =
        {
            "Judge1CameraPose",
            "Judge2CameraPose",
            "Judge3CameraPose"
        };

        [MenuItem("SharkTank/Build User Input Screen")]
        private static void Build()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                Debug.LogError("No default TextMeshPro font asset is configured. Assign one in TMP Settings before building the UI.");
                return;
            }

            UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
            Canvas canvas = GetSharedCanvas(activeScene);
            if (canvas == null)
            {
                return;
            }

            EnsureEventSystem();

            Transform existingUserInput = canvas.transform.Find(UserInputUIName);
            if (existingUserInput == null)
            {
                existingUserInput = canvas.transform.Find(LegacyUserInputUIName);
                if (existingUserInput != null)
                {
                    Undo.RecordObject(existingUserInput.gameObject, "Rename User Input UI");
                    existingUserInput.name = UserInputUIName;
                }
            }

            GameObject userInputRoot;
            TMP_Text taskText;
            TMP_Text questionText;
            TMP_InputField inputField;
            TMP_Text placeholder;
            Button enterButton;
            if (existingUserInput != null)
            {
                userInputRoot = existingUserInput.gameObject;
                taskText = FindComponent<TMP_Text>(existingUserInput, "TaskPanel/CurrentTaskLabel");
                questionText = FindComponent<TMP_Text>(existingUserInput, "QuestionLabel");
                inputField = FindComponent<TMP_InputField>(existingUserInput, "BottomPanel/ResponseInputField");
                placeholder = FindComponent<TMP_Text>(existingUserInput, "BottomPanel/ResponseInputField/Viewport/Placeholder");
                enterButton = FindComponent<Button>(existingUserInput, "BottomPanel/EnterButton");
                if (questionText == null)
                {
                    questionText = BuildQuestionLabel(userInputRoot.transform, defaultFont);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(questionText.text))
                    {
                        questionText.text = SampleJudgeQuestion;
                    }

                    questionText.gameObject.SetActive(true);
                }
            }
            else
            {
                userInputRoot = new GameObject(UserInputUIName, typeof(RectTransform), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(userInputRoot, "Build User Input Screen");
                userInputRoot.transform.SetParent(canvas.transform, false);
                RectTransform userInputRect = userInputRoot.GetComponent<RectTransform>();
                userInputRect.anchorMin = Vector2.zero;
                userInputRect.anchorMax = Vector2.one;
                userInputRect.offsetMin = Vector2.zero;
                userInputRect.offsetMax = Vector2.zero;
                userInputRoot.GetComponent<CanvasGroup>().alpha = 1f;

                Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                taskText = BuildTaskPanel(userInputRoot.transform, defaultFont, roundedSprite);
                BuildBottomPanel(userInputRoot.transform, defaultFont, roundedSprite, out inputField, out placeholder, out enterButton);
                questionText = BuildQuestionLabel(userInputRoot.transform, defaultFont);
            }

            CanvasGroup rootGroup = userInputRoot.GetComponent<CanvasGroup>();
            if (rootGroup == null)
            {
                rootGroup = Undo.AddComponent<CanvasGroup>(userInputRoot);
            }
            rootGroup.alpha = 1f;

            RectTransform bottomPanel = userInputRoot.transform.Find("BottomPanel") as RectTransform;
            Button helpButton = FindComponent<Button>(userInputRoot.transform, "HelpButton");
            if (helpButton == null)
            {
                helpButton = BuildHelpButton(userInputRoot.transform, defaultFont);
            }

            GameObject helpPopupRoot = userInputRoot.transform.Find("HelpPopup")?.gameObject;
            if (helpPopupRoot == null)
            {
                BuildHelpPopup(userInputRoot.transform, defaultFont);
                helpPopupRoot = userInputRoot.transform.Find("HelpPopup").gameObject;
            }
            helpPopupRoot.SetActive(false);

            Button helpOverlayButton = FindComponent<Button>(userInputRoot.transform, "HelpPopup");
            Button helpPanelBlockerButton = FindComponent<Button>(userInputRoot.transform, "HelpPopup/HelpPanel");
            Button closeHelpButton = FindComponent<Button>(userInputRoot.transform, "HelpPopup/HelpPanel/CloseHelpButton");
            TMP_Text helpHintLabel = FindComponent<TMP_Text>(userInputRoot.transform, "HelpPopup/HelpPanel/HelpHintText");
            TMP_Text helpTitleLabel = FindComponent<TMP_Text>(userInputRoot.transform, "HelpPopup/HelpPanel/HelpTitle");

            UserInputScreen oldScreen = userInputRoot.GetComponent<UserInputScreen>();
            UserInputScreen screen = GetOrCreateController(activeScene, canvas, oldScreen);

            SerializedObject serializedScreen = new SerializedObject(screen);
            serializedScreen.FindProperty("rootGroup").objectReferenceValue = rootGroup;
            serializedScreen.FindProperty("currentTaskLabel").objectReferenceValue = taskText;
            serializedScreen.FindProperty("questionTextLabel").objectReferenceValue = questionText;
            serializedScreen.FindProperty("inputField").objectReferenceValue = inputField;
            serializedScreen.FindProperty("placeholderLabel").objectReferenceValue = placeholder;
            serializedScreen.FindProperty("enterButton").objectReferenceValue = enterButton;
            serializedScreen.FindProperty("bottomPanel").objectReferenceValue = bottomPanel;
            serializedScreen.FindProperty("helpButton").objectReferenceValue = helpButton;
            serializedScreen.FindProperty("helpPopupRoot").objectReferenceValue = helpPopupRoot;
            serializedScreen.FindProperty("helpOverlayButton").objectReferenceValue = helpOverlayButton;
            serializedScreen.FindProperty("helpPanelBlockerButton").objectReferenceValue = helpPanelBlockerButton;
            serializedScreen.FindProperty("closeHelpButton").objectReferenceValue = closeHelpButton;
            serializedScreen.FindProperty("helpHintLabel").objectReferenceValue = helpHintLabel;
            serializedScreen.FindProperty("helpTitleLabel").objectReferenceValue = helpTitleLabel;
            AssignCameraReferences(serializedScreen);
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();

            AssignJudgeSequenceCameraController(activeScene, screen);
            EditorSceneManager.MarkSceneDirty(activeScene);

            GameObject selectionTarget = helpButton != null
                ? helpButton.gameObject
                : questionText != null ? questionText.gameObject : userInputRoot;
            Selection.activeGameObject = selectionTarget;
            EditorGUIUtility.PingObject(selectionTarget);
            Debug.Log("The judge response UI preview is visible. Edit HelpButton or HelpPopup under Canvas/UserInputUI in the Inspector.");
        }

        private static UserInputScreen GetOrCreateController(
            UnityEngine.SceneManagement.Scene scene,
            Canvas canvas,
            UserInputScreen oldScreen)
        {
            GameObject controllerObject = FindSceneObject(scene, ControllerObjectName);
            if (controllerObject == null)
            {
                controllerObject = FindSceneObject(scene, LegacyControllerObjectName);
                if (controllerObject != null)
                {
                    Undo.RecordObject(controllerObject, "Rename User Input Controller");
                    controllerObject.name = ControllerObjectName;
                }
            }

            if (controllerObject == null)
            {
                controllerObject = new GameObject(ControllerObjectName);
                Undo.RegisterCreatedObjectUndo(controllerObject, "Create User Input Screen Controller");
            }
            else if (controllerObject.transform.IsChildOf(canvas.transform))
            {
                Undo.SetTransformParent(controllerObject.transform, null, "Move User Input Controller Outside Canvas");
            }

            UserInputScreen controller = controllerObject.GetComponent<UserInputScreen>();
            if (controller == null)
            {
                controller = Undo.AddComponent<UserInputScreen>(controllerObject);
            }

            if (oldScreen != null && oldScreen != controller)
            {
                Undo.RecordObject(controller, "Move User Input Screen Controller");
                EditorUtility.CopySerialized(oldScreen, controller);
                Undo.DestroyObjectImmediate(oldScreen);
            }

            return controller;
        }

        private static void AssignJudgeSequenceCameraController(
            UnityEngine.SceneManagement.Scene scene,
            UserInputScreen controller)
        {
            JudgeFeedbackSequence[] sequences = Resources.FindObjectsOfTypeAll<JudgeFeedbackSequence>();
            for (int i = 0; i < sequences.Length; i++)
            {
                if (sequences[i].gameObject.scene != scene)
                {
                    continue;
                }

                SerializedObject serializedSequence = new SerializedObject(sequences[i]);
                serializedSequence.FindProperty("cameraController").objectReferenceValue = controller;
                serializedSequence.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject FindSceneObject(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                if (sceneObjects[i].name == objectName && sceneObjects[i].scene == scene)
                {
                    return sceneObjects[i];
                }
            }

            return null;
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static void AssignCameraReferences(SerializedObject serializedScreen)
        {
            serializedScreen.FindProperty("presentationCamera").objectReferenceValue = Camera.main;
            GameObject poseObject = GameObject.Find("UserInputCameraPose");
            serializedScreen.FindProperty("inputCameraPose").objectReferenceValue =
                poseObject != null ? poseObject.transform : null;

            SerializedProperty judgeCameraPoses = serializedScreen.FindProperty("judgeCameraPoses");
            judgeCameraPoses.arraySize = JudgeCameraPoseNames.Length;
            for (int i = 0; i < JudgeCameraPoseNames.Length; i++)
            {
                GameObject judgePoseObject = GameObject.Find(JudgeCameraPoseNames[i]);
                judgeCameraPoses.GetArrayElementAtIndex(i).objectReferenceValue =
                    judgePoseObject != null ? judgePoseObject.transform : null;
            }
        }

        internal static Canvas GetSharedCanvas(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject sceneObject in sceneObjects)
            {
                if (sceneObject.name == CanvasName && sceneObject.scene == scene)
                {
                    Canvas namedCanvas = sceneObject.GetComponent<Canvas>();
                    if (namedCanvas != null)
                    {
                        ConfigureSharedCanvas(namedCanvas);
                        return namedCanvas;
                    }
                }
            }

            Debug.LogError("Could not find a Canvas object named exactly \"Canvas\" in the active scene. No Canvas was created.");
            return null;
        }

        private static void ConfigureSharedCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Event System");
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<BaseInputModule>() != null)
            {
                return;
            }

            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null && typeof(BaseInputModule).IsAssignableFrom(inputSystemModuleType))
            {
                Undo.AddComponent(eventSystem.gameObject, inputSystemModuleType);
            }
            else
            {
                Undo.AddComponent<StandaloneInputModule>(eventSystem.gameObject);
            }
        }

        private static TMP_Text BuildTaskPanel(Transform parent, TMP_FontAsset font, Sprite roundedSprite)
        {
            RectTransform panel = CreateRect("TaskPanel", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -54f), new Vector2(520f, 110f));
            CreatePanelBackground(panel, roundedSprite, UITheme.PanelAccent, Vector2.zero, Vector2.zero);
            CreatePanelBackground(panel, roundedSprite, UITheme.PanelBackground, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            CreateText("TaskHeading", panel, font, "TAREA ACTUAL", 18f, UITheme.Accent, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(-44f, 28f));

            TMP_Text taskText = CreateText("CurrentTaskLabel", panel, font, "Presenta tu pitch", 28f, UITheme.TextCream, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(22f, 14f), new Vector2(-44f, -42f));
            taskText.textWrappingMode = TextWrappingModes.Normal;
            return taskText;
        }

        private static TMP_Text BuildQuestionLabel(Transform parent, TMP_FontAsset font)
        {
            TMP_Text questionText = CreateText("QuestionLabel", parent, font, SampleJudgeQuestion, 34f, Color.white,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 278f), new Vector2(1580f, 110f));
            questionText.fontStyle = FontStyles.Bold;
            questionText.outlineColor = Color.black;
            questionText.outlineWidth = 0.25f;
            questionText.gameObject.SetActive(true);
            return questionText;
        }

        private static Button BuildHelpButton(Transform parent, TMP_FontAsset font)
        {
            RectTransform buttonRect = CreateRect(
                "HelpButton",
                parent,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-48f, -48f),
                new Vector2(190f, 68f));
            Undo.RegisterCreatedObjectUndo(buttonRect.gameObject, "Build User Input Help Button");

            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = roundedSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = UITheme.Accent;

            Button button = buttonRect.gameObject.AddComponent<Button>();
            ConfigureHelpButton(button, buttonImage);

            CreateText(
                "HelpLabel",
                buttonRect,
                font,
                "? AYUDA",
                25f,
                UITheme.TextCream,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            return button;
        }

        private static void BuildHelpPopup(Transform parent, TMP_FontAsset font)
        {
            GameObject popupObject = new GameObject(
                "HelpPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(popupObject, "Build Question Help Popup");
            popupObject.transform.SetParent(parent, false);

            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            Image overlayImage = popupObject.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
            overlayImage.raycastTarget = true;
            Button overlayButton = popupObject.GetComponent<Button>();
            ConfigureHelpButton(overlayButton, overlayImage);

            CanvasGroup popupGroup = popupObject.GetComponent<CanvasGroup>();
            popupGroup.alpha = 1f;
            popupGroup.interactable = true;
            popupGroup.blocksRaycasts = true;

            RectTransform panelRect = CreateRect(
                "HelpPanel",
                popupObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1080f, 590f));
            Image panelImage = panelRect.gameObject.AddComponent<Image>();
            panelImage.color = UITheme.PanelAccent;
            panelImage.raycastTarget = true;
            Button panelButton = panelRect.gameObject.AddComponent<Button>();
            ConfigureHelpButton(panelButton, panelImage);

            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            CreatePanelBackground(panelRect, roundedSprite, UITheme.PanelBackground, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            CreateText(
                "HelpTitle",
                panelRect,
                font,
                "AYUDA DEL JUEZ",
                34f,
                UITheme.TextDark,
                TextAlignmentOptions.Left,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(54f, -46f),
                new Vector2(-150f, 72f));

            RectTransform closeButtonRect = CreateRect(
                "CloseHelpButton",
                panelRect,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-30f, -26f),
                new Vector2(64f, 64f));
            Image closeButtonImage = closeButtonRect.gameObject.AddComponent<Image>();
            closeButtonImage.sprite = roundedSprite;
            closeButtonImage.type = Image.Type.Sliced;
            closeButtonImage.color = UITheme.Accent;
            Button closeButton = closeButtonRect.gameObject.AddComponent<Button>();
            ConfigureHelpButton(closeButton, closeButtonImage);
            CreateText(
                "CloseLabel",
                closeButtonRect,
                font,
                "X",
                28f,
                UITheme.TextCream,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            TMP_Text hintLabel = CreateText(
                "HelpHintText",
                panelRect,
                font,
                string.Empty,
                30f,
                UITheme.TextDark,
                TextAlignmentOptions.TopLeft,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(58f, 54f),
                new Vector2(-116f, -150f));
            hintLabel.textWrappingMode = TextWrappingModes.Normal;

            popupObject.SetActive(false);
        }

        private static void ConfigureHelpButton(Button button, Image image)
        {
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = UITheme.Accent;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.disabledColor = UITheme.Disabled;
            button.colors = colors;
        }

        private static void BuildBottomPanel(Transform parent, TMP_FontAsset font, Sprite roundedSprite,
            out TMP_InputField inputField, out TMP_Text placeholder, out Button enterButton)
        {
            RectTransform bottomPanel = CreateRect("BottomPanel", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(1700f, 220f));
            CreatePanelBackground(bottomPanel, roundedSprite, UITheme.PanelAccent, Vector2.zero, Vector2.zero);
            CreatePanelBackground(bottomPanel, roundedSprite, UITheme.PanelBackground, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            RectTransform inputRect = CreateRect("ResponseInputField", bottomPanel, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(32f, 40f), new Vector2(1352f, 140f));
            Image inputBackground = inputRect.gameObject.AddComponent<Image>();
            inputBackground.sprite = roundedSprite;
            inputBackground.type = Image.Type.Sliced;
            inputBackground.color = new Color(24f / 255f, 16f / 255f, 13f / 255f, 0.96f);

            RectTransform viewport = CreateRect("Viewport", inputRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(18f, 12f), new Vector2(-36f, -24f));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform textRect = CreateRect("Text", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 180f));
            TMP_Text inputText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            inputText.font = font;
            inputText.fontSize = 24f;
            inputText.color = UITheme.TextCream;
            inputText.textWrappingMode = TextWrappingModes.Normal;
            inputText.alignment = TextAlignmentOptions.TopLeft;
            inputText.raycastTarget = false;
            textRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform placeholderRect = CreateRect("Placeholder", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 180f));
            placeholder = placeholderRect.gameObject.AddComponent<TextMeshProUGUI>();
            placeholder.font = font;
            placeholder.fontSize = 24f;
            placeholder.color = new Color(UITheme.TextCream.r, UITheme.TextCream.g, UITheme.TextCream.b, 0.55f);
            placeholder.text = "Escribe tu pitch aquí...";
            placeholder.textWrappingMode = TextWrappingModes.Normal;
            placeholder.alignment = TextAlignmentOptions.TopLeft;
            placeholder.raycastTarget = false;

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = textRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            inputField = inputRect.gameObject.AddComponent<TMP_InputField>();
            inputField.textViewport = viewport;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.characterLimit = 800;
            inputField.targetGraphic = inputBackground;
            inputField.shouldHideMobileInput = false;

            RectTransform buttonRect = CreateRect("EnterButton", bottomPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-32f, 40f), new Vector2(260f, 80f));
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = roundedSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = UITheme.Accent;

            enterButton = buttonRect.gameObject.AddComponent<Button>();
            enterButton.targetGraphic = buttonImage;
            enterButton.transition = Selectable.Transition.None;
            enterButton.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = enterButton.colors;
            colors.normalColor = UITheme.Accent;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.disabledColor = UITheme.Disabled;
            enterButton.colors = colors;

            ButtonPressColor pressColor = buttonRect.gameObject.AddComponent<ButtonPressColor>();
            SerializedObject serializedPressColor = new SerializedObject(pressColor);
            serializedPressColor.FindProperty("target").objectReferenceValue = buttonImage;
            serializedPressColor.FindProperty("normalColor").colorValue = UITheme.Accent;
            serializedPressColor.FindProperty("hoverColor").colorValue = UITheme.ButtonHover;
            serializedPressColor.FindProperty("pressedColor").colorValue = UITheme.ButtonPressed;
            serializedPressColor.ApplyModifiedPropertiesWithoutUndo();

            CreateText("EnterLabel", buttonRect, font, "ENTRAR", 26f, UITheme.TextCream, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void CreatePanelBackground(Transform parent, Sprite sprite, Color color, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = offsetMin;
            backgroundRect.offsetMax = offsetMax;

            Image image = backgroundObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        private static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset font, string text, float fontSize,
            Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
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
