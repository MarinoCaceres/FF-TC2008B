using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.UI
{
    /// <summary>
    /// Controls the pitch and answer text input screen.
    /// </summary>
    public sealed class UserInputScreen : MonoBehaviour
    {
        private const float TransitionDuration = 0.25f;
        private const float PanelSlideDistance = 18f;
        private static readonly string[] JudgeCameraPoseNames =
        {
            "Judge1CameraPose",
            "Judge2CameraPose",
            "Judge3CameraPose"
        };

        [Header("Screen references")]
        [Tooltip("Canvas group used to show and hide the screen.")]
        [SerializeField] private CanvasGroup rootGroup;
        [Tooltip("Text describing the current task.")]
        [SerializeField] private TMP_Text currentTaskLabel;
        [Tooltip("Question the user is currently answering.")]
        [SerializeField] private TMP_Text questionTextLabel;
        [Tooltip("Multiline field for the user's response.")]
        [SerializeField] private TMP_InputField inputField;
        [Tooltip("Placeholder shown while the response is empty.")]
        [SerializeField] private TMP_Text placeholderLabel;
        [Tooltip("Button that submits the response.")]
        [SerializeField] private Button enterButton;
        [Tooltip("Bottom panel animated while the input screen is shown.")]
        [SerializeField] private RectTransform bottomPanel;

        [Header("Camera framing")]
        [Tooltip("Camera moved to the input-screen pose while this screen is visible.")]
        [SerializeField] private Camera presentationCamera;
        [Tooltip("Scene transform defining the camera position and rotation for this screen.")]
        [SerializeField] private Transform inputCameraPose;
        [Tooltip("Duration of the camera move in seconds.")]
        [SerializeField] private float cameraTransitionDuration = 0.65f;
        [Tooltip("Duration of each one-way camera pan in seconds.")]
        [Min(0.1f)]
        [SerializeField] private float cameraPanDuration = 3f;
        [Tooltip("Maximum angle of the subtle camera arc, in degrees.")]
        [Range(0f, 10f)]
        [SerializeField] private float cameraPanAngle = 3.5f;

        [Header("Judge cutaways")]
        [Tooltip("Camera transforms for the three judge views.")]
        [SerializeField] private Transform[] judgeCameraPoses = new Transform[3];
        [Tooltip("Random range of player-view time before a judge cutaway, in seconds.")]
        [SerializeField] private Vector2 playerViewDurationRange = new Vector2(8f, 14f);
        [Tooltip("How long the camera stays on each judge, in seconds.")]
        [Min(0.1f)]
        [SerializeField] private float judgeViewDuration = 5f;
        [Tooltip("Maximum vertical camera tilt during a judge view, in degrees.")]
        [Range(0f, 10f)]
        [SerializeField] private float judgeVerticalPanAngle = 2.5f;
        [Tooltip("Duration of one full up-and-down judge camera tilt, in seconds.")]
        [Min(0.1f)]
        [SerializeField] private float judgeVerticalPanCycleDuration = 4.5f;

        private Vector2 bottomPanelVisiblePosition;
        private RectTransform taskPanel;
        private float taskPanelMinimumHeight;
        private float taskLabelVerticalPadding;
        private Coroutine transitionCoroutine;
        private Coroutine cameraTransitionCoroutine;
        private Vector3 previousCameraPosition;
        private Quaternion previousCameraRotation;
        private bool hasPreviousCameraPose;
        private bool warnedAboutMissingJudgePoses;
        private bool canInteract = true;

        /// <summary>
        /// Occurs when the user submits a non-empty response.
        /// </summary>
        public event Action<string> OnSubmitted;

        private void OnValidate()
        {
            if (!Application.isPlaying && rootGroup != null)
            {
                rootGroup.alpha = 1f;
            }
        }

        private void Awake()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            EnsureQuestionTextLabel();
            if (questionTextLabel != null)
            {
                questionTextLabel.text = string.Empty;
                questionTextLabel.gameObject.SetActive(false);
            }

            if (inputField != null)
            {
                inputField.characterLimit = 800;
                inputField.onValueChanged.AddListener(HandleInputChanged);
            }

            if (enterButton != null)
            {
                enterButton.onClick.AddListener(HandleSubmit);
            }

            if (bottomPanel == null && rootGroup != null)
            {
                Transform panelTransform = rootGroup.transform.Find("BottomPanel");
                if (panelTransform != null)
                {
                    bottomPanel = panelTransform as RectTransform;
                }
            }

            if (bottomPanel != null)
            {
                bottomPanelVisiblePosition = bottomPanel.anchoredPosition;
            }

            if (currentTaskLabel != null)
            {
                taskPanel = currentTaskLabel.transform.parent as RectTransform;
                if (taskPanel != null)
                {
                    taskPanelMinimumHeight = taskPanel.sizeDelta.y;
                    RectTransform taskLabelRect = currentTaskLabel.rectTransform;
                    taskLabelVerticalPadding = taskLabelRect.offsetMin.y - taskLabelRect.offsetMax.y;
                }
            }

            canInteract = false;
            UpdateEnterButton();
        }

        private void OnDestroy()
        {
            if (inputField != null)
            {
                inputField.onValueChanged.RemoveListener(HandleInputChanged);
            }

            if (enterButton != null)
            {
                enterButton.onClick.RemoveListener(HandleSubmit);
            }
        }

        /// <summary>
        /// Sets the task and placeholder, clears the response, and fades in the screen.
        /// </summary>
        /// <param name="taskText">The task shown above the response field.</param>
        /// <param name="placeholderText">The hint shown in the empty response field.</param>
        /// <param name="questionText">The question shown above the response field.</param>
        public void Show(string taskText, string placeholderText, string questionText = null)
        {
            if (currentTaskLabel != null)
            {
                currentTaskLabel.text = taskText ?? string.Empty;
                ResizeTaskPanelToFitText();
            }

            if (questionTextLabel != null)
            {
                questionTextLabel.text = questionText ?? string.Empty;
                questionTextLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(questionText));
            }

            if (placeholderLabel != null)
            {
                placeholderLabel.text = placeholderText ?? string.Empty;
            }

            if (inputField != null)
            {
                inputField.SetTextWithoutNotify(string.Empty);
            }

            canInteract = true;

            if (rootGroup != null)
            {
                rootGroup.gameObject.SetActive(true);
                rootGroup.alpha = 0f;
                rootGroup.interactable = true;
                rootGroup.blocksRaycasts = true;
            }

            if (bottomPanel != null)
            {
                bottomPanel.anchoredPosition = bottomPanelVisiblePosition + Vector2.down * PanelSlideDistance;
            }

            UpdateEnterButton();

            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            }

            MoveCameraToInputPose();
            StartTransition(true);
        }

        private void ResizeTaskPanelToFitText()
        {
            if (taskPanel == null || currentTaskLabel == null)
            {
                return;
            }

            RectTransform taskLabelRect = currentTaskLabel.rectTransform;
            float labelWidth = taskLabelRect.rect.width;
            if (labelWidth <= 0f)
            {
                return;
            }

            float preferredHeight = currentTaskLabel.GetPreferredValues(currentTaskLabel.text, labelWidth, 0f).y;
            Vector2 panelSize = taskPanel.sizeDelta;
            panelSize.y = Mathf.Max(taskPanelMinimumHeight, preferredHeight + taskLabelVerticalPadding);
            taskPanel.sizeDelta = panelSize;
        }

        private void EnsureQuestionTextLabel()
        {
            if (questionTextLabel != null || rootGroup == null)
            {
                return;
            }

            Transform existingLabel = rootGroup.transform.Find("QuestionLabel");
            if (existingLabel != null)
            {
                questionTextLabel = existingLabel.GetComponent<TMP_Text>();
                if (questionTextLabel != null)
                {
                    return;
                }
            }

            GameObject labelObject = new GameObject("QuestionLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rootGroup.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 278f);
            labelRect.sizeDelta = new Vector2(1580f, 110f);

            questionTextLabel = labelObject.GetComponent<TextMeshProUGUI>();
            questionTextLabel.font = currentTaskLabel != null ? currentTaskLabel.font : TMP_Settings.defaultFontAsset;
            questionTextLabel.fontSize = 34f;
            questionTextLabel.fontStyle = FontStyles.Bold;
            questionTextLabel.color = Color.white;
            questionTextLabel.alignment = TextAlignmentOptions.Center;
            questionTextLabel.textWrappingMode = TextWrappingModes.Normal;
            questionTextLabel.outlineColor = Color.black;
            questionTextLabel.outlineWidth = 0.25f;
            questionTextLabel.raycastTarget = false;
        }

        /// <summary>
        /// Fades out the screen and disables its interaction when the fade completes.
        /// </summary>
        public void Hide()
        {
            ReturnCameraToPreviousPose();
            StartTransition(false);
        }

        /// <summary>
        /// Hides the input screen and waits for its fade-out transition to complete.
        /// </summary>
        /// <param name="restoreCameraPose">Whether to move the camera back to its saved pose while hiding.</param>
        public IEnumerator HideAndWait(bool restoreCameraPose = true)
        {
            if (restoreCameraPose)
            {
                ReturnCameraToPreviousPose();
            }
            else
            {
                StopCameraMotion();
                hasPreviousCameraPose = false;
            }

            StartTransition(false);
            while (transitionCoroutine != null)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Immediately cuts the presentation camera to an existing scene pose.
        /// </summary>
        /// <param name="targetPose">The transform defining the camera position and rotation.</param>
        public void CutCameraToPose(Transform targetPose)
        {
            if (targetPose == null)
            {
                Debug.LogWarning("UserInputScreen cannot cut the camera to a missing pose.", this);
                return;
            }

            if (presentationCamera == null)
            {
                presentationCamera = Camera.main;
            }

            if (presentationCamera == null)
            {
                Debug.LogWarning("UserInputScreen cannot cut the camera because the presentation camera is missing.", this);
                return;
            }

            StopCameraMotion();
            hasPreviousCameraPose = false;
            presentationCamera.transform.SetPositionAndRotation(targetPose.position, targetPose.rotation);
        }

        /// <summary>
        /// Smoothly moves the presentation camera to an existing scene pose.
        /// </summary>
        /// <param name="targetPose">The transform defining the camera position and rotation.</param>
        public IEnumerator MoveCameraToPose(Transform targetPose)
        {
            return MoveCameraToPose(targetPose, 0f);
        }

        /// <summary>
        /// Smoothly moves the presentation camera to an existing scene pose with a local-right offset.
        /// </summary>
        /// <param name="targetPose">The transform defining the camera position and rotation.</param>
        /// <param name="rightOffset">World-unit offset along the pose's right direction.</param>
        public IEnumerator MoveCameraToPose(Transform targetPose, float rightOffset)
        {
            if (targetPose == null)
            {
                Debug.LogWarning("UserInputScreen cannot move the camera to a missing pose.", this);
                yield break;
            }

            if (presentationCamera == null)
            {
                presentationCamera = Camera.main;
            }

            if (presentationCamera == null)
            {
                Debug.LogWarning("UserInputScreen cannot move the camera because the presentation camera is missing.", this);
                yield break;
            }

            StopCameraMotion();
            Vector3 targetPosition = targetPose.position + targetPose.right * rightOffset;
            yield return MoveCameraToPose(targetPosition, targetPose.rotation, cameraTransitionDuration);
        }

        /// <summary>
        /// Enables or disables the response field and submit button.
        /// </summary>
        /// <param name="value">Whether the user may interact with the screen.</param>
        public void SetInteractable(bool value)
        {
            canInteract = value;

            if (inputField != null)
            {
                inputField.interactable = value;
            }

            UpdateEnterButton();
        }

        [ContextMenu("Test: Show Pitch")]
        private void TestShowPitch()
        {
            OnSubmitted -= LogTestSubmission;
            OnSubmitted += LogTestSubmission;
            Show("Presenta tu pitch", "Escribe tu pitch aquí...");
        }

        [ContextMenu("Test: Show Answer")]
        private void TestShowAnswer()
        {
            OnSubmitted -= LogTestSubmission;
            OnSubmitted += LogTestSubmission;
            Show("Responde a los jueces", "Escribe tu respuesta aquí...");
        }

        [ContextMenu("Test: Hide")]
        private void TestHide()
        {
            Hide();
        }

        private void StartTransition(bool show)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            if (rootGroup == null)
            {
                if (!show)
                {
                    SetInteractable(false);
                }

                return;
            }

            transitionCoroutine = StartCoroutine(AnimateTransition(show));
        }

        private void MoveCameraToInputPose()
        {
            if (!ResolveCameraReferences())
            {
                return;
            }

            ResolveJudgeCameraPoses();

            if (!hasPreviousCameraPose)
            {
                previousCameraPosition = presentationCamera.transform.position;
                previousCameraRotation = presentationCamera.transform.rotation;
                hasPreviousCameraPose = true;
            }

            Vector3 focusPoint = GetPlayerFocusPoint();
            GetPannedCameraPose(focusPoint, cameraPanAngle, out Vector3 leftPosition, out Quaternion leftRotation);
            StartCameraTransition(leftPosition, leftRotation, false, true, focusPoint);
        }

        private void ReturnCameraToPreviousPose()
        {
            if (!hasPreviousCameraPose || presentationCamera == null)
            {
                return;
            }

            StartCameraTransition(previousCameraPosition, previousCameraRotation, true);
        }

        private bool ResolveCameraReferences()
        {
            if (presentationCamera == null)
            {
                presentationCamera = Camera.main;
            }

            if (inputCameraPose == null)
            {
                GameObject poseObject = GameObject.Find("UserInputCameraPose");
                if (poseObject != null)
                {
                    inputCameraPose = poseObject.transform;
                }
            }

            if (presentationCamera == null)
            {
                Debug.LogError("UserInputScreen requires a Main Camera to move to the input view.", this);
                return false;
            }

            if (inputCameraPose == null)
            {
                Debug.LogError("UserInputScreen could not find the UserInputCameraPose scene object.", this);
                return false;
            }

            return true;
        }

        private void StartCameraTransition(Vector3 targetPosition, Quaternion targetRotation, bool returning,
            bool panAfterArrival = false, Vector3 focusPoint = default)
        {
            if (cameraTransitionCoroutine != null)
            {
                StopCoroutine(cameraTransitionCoroutine);
            }

            if (cameraTransitionDuration <= 0f)
            {
                presentationCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
                cameraTransitionCoroutine = null;
                if (returning)
                {
                    hasPreviousCameraPose = false;
                }
                else if (panAfterArrival)
                {
                    StartCameraPan(focusPoint);
                }

                return;
            }

            cameraTransitionCoroutine = StartCoroutine(AnimateCamera(targetPosition, targetRotation, returning, panAfterArrival, focusPoint));
        }

        private void StopCameraMotion()
        {
            if (cameraTransitionCoroutine == null)
            {
                return;
            }

            StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = null;
        }

        private IEnumerator AnimateCamera(Vector3 targetPosition, Quaternion targetRotation, bool returning,
            bool panAfterArrival, Vector3 focusPoint)
        {
            Transform cameraTransform = presentationCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float elapsed = 0f;

            while (elapsed < cameraTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / cameraTransitionDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                cameraTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, easedProgress),
                    Quaternion.Slerp(startRotation, targetRotation, easedProgress));
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
            cameraTransitionCoroutine = null;
            if (returning)
            {
                hasPreviousCameraPose = false;
            }
            else if (panAfterArrival)
            {
                StartCameraPan(focusPoint);
            }
        }

        private void StartCameraPan(Vector3 focusPoint)
        {
            cameraTransitionCoroutine = StartCoroutine(AnimateCameraCycle(focusPoint));
        }

        private IEnumerator AnimateCameraCycle(Vector3 playerFocusPoint)
        {
            int previousJudgeIndex = -1;

            while (true)
            {
                float playerViewDuration = GetRandomPlayerViewDuration();
                yield return AnimatePlayerPan(playerFocusPoint, playerViewDuration);

                if (!TryGetRandomJudgePose(previousJudgeIndex, out int judgeIndex, out Transform judgePose))
                {
                    continue;
                }

                previousJudgeIndex = judgeIndex;
                yield return MoveCameraToPose(judgePose.position, judgePose.rotation, cameraTransitionDuration);
                yield return AnimateJudgePan(judgePose);

                GetPannedCameraPose(playerFocusPoint, cameraPanAngle, out Vector3 playerPosition, out Quaternion playerRotation);
                yield return MoveCameraToPose(playerPosition, playerRotation, cameraTransitionDuration);
            }
        }

        private IEnumerator AnimatePlayerPan(Vector3 focusPoint, float duration)
        {
            Transform cameraTransform = presentationCamera.transform;
            float elapsed = 0f;
            float sweepElapsed = 0f;
            bool movingRight = true;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (cameraPanDuration > 0f && cameraPanAngle > 0f)
                {
                    sweepElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(sweepElapsed / cameraPanDuration);
                    float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                    float startAngle = movingRight ? cameraPanAngle : -cameraPanAngle;
                    float angle = Mathf.Lerp(startAngle, -startAngle, easedProgress);
                    GetPannedCameraPose(focusPoint, angle, out Vector3 position, out Quaternion rotation);
                    cameraTransform.SetPositionAndRotation(position, rotation);

                    if (progress >= 1f)
                    {
                        movingRight = !movingRight;
                        sweepElapsed = 0f;
                    }
                }

                yield return null;
            }
        }

        private IEnumerator AnimateJudgePan(Transform judgePose)
        {
            Transform cameraTransform = presentationCamera.transform;
            float elapsed = 0f;

            while (elapsed < judgeViewDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float phase = elapsed / Mathf.Max(0.1f, judgeVerticalPanCycleDuration) * Mathf.PI * 2f;
                float tilt = Mathf.Sin(phase) * judgeVerticalPanAngle;
                Quaternion rotation = judgePose.rotation * Quaternion.Euler(tilt, 0f, 0f);
                cameraTransform.SetPositionAndRotation(judgePose.position, rotation);
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(judgePose.position, judgePose.rotation);
        }

        private IEnumerator MoveCameraToPose(Vector3 position, Quaternion rotation, float duration)
        {
            Transform cameraTransform = presentationCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                cameraTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, position, easedProgress),
                    Quaternion.Slerp(startRotation, rotation, easedProgress));
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(position, rotation);
        }

        private float GetRandomPlayerViewDuration()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(playerViewDurationRange.x, playerViewDurationRange.y));
            float maximum = Mathf.Max(minimum, playerViewDurationRange.x > playerViewDurationRange.y
                ? playerViewDurationRange.x
                : playerViewDurationRange.y);
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private void ResolveJudgeCameraPoses()
        {
            if (judgeCameraPoses == null || judgeCameraPoses.Length != JudgeCameraPoseNames.Length)
            {
                Array.Resize(ref judgeCameraPoses, JudgeCameraPoseNames.Length);
            }

            int configuredCount = 0;
            string missingNames = string.Empty;
            for (int i = 0; i < JudgeCameraPoseNames.Length; i++)
            {
                if (judgeCameraPoses[i] == null)
                {
                    GameObject poseObject = GameObject.Find(JudgeCameraPoseNames[i]);
                    if (poseObject != null)
                    {
                        judgeCameraPoses[i] = poseObject.transform;
                    }
                }

                if (judgeCameraPoses[i] != null)
                {
                    configuredCount++;
                }
                else
                {
                    missingNames += (missingNames.Length > 0 ? ", " : string.Empty) + JudgeCameraPoseNames[i];
                }
            }

            if (configuredCount < JudgeCameraPoseNames.Length && !warnedAboutMissingJudgePoses)
            {
                Debug.LogWarning(
                    configuredCount == 0
                        ? "No judge camera points were found. Add Judge1CameraPose, Judge2CameraPose, and Judge3CameraPose to enable judge cutaways."
                        : "Some judge camera points are missing: " + missingNames,
                    this);
                warnedAboutMissingJudgePoses = true;
            }
        }

        private bool TryGetRandomJudgePose(int previousIndex, out int selectedIndex, out Transform selectedPose)
        {
            int availableCount = 0;
            for (int i = 0; i < judgeCameraPoses.Length; i++)
            {
                if (judgeCameraPoses[i] != null)
                {
                    availableCount++;
                }
            }

            if (availableCount == 0)
            {
                selectedIndex = -1;
                selectedPose = null;
                return false;
            }

            bool excludePrevious = availableCount > 1 &&
                previousIndex >= 0 &&
                previousIndex < judgeCameraPoses.Length &&
                judgeCameraPoses[previousIndex] != null;
            int candidatesWithoutPrevious = excludePrevious ? availableCount - 1 : availableCount;
            int selectedCandidate = UnityEngine.Random.Range(0, candidatesWithoutPrevious);
            for (int i = 0; i < judgeCameraPoses.Length; i++)
            {
                if (judgeCameraPoses[i] == null || (excludePrevious && i == previousIndex))
                {
                    continue;
                }

                if (selectedCandidate-- == 0)
                {
                    selectedIndex = i;
                    selectedPose = judgeCameraPoses[i];
                    return true;
                }
            }

            selectedIndex = -1;
            selectedPose = null;
            return false;
        }

        private Vector3 GetPlayerFocusPoint()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return inputCameraPose.position + inputCameraPose.forward * 6f;
            }

            Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>();
            Bounds playerBounds = default;
            bool hasVisibleBounds = false;

            foreach (Renderer playerRenderer in renderers)
            {
                if (playerRenderer == null || !playerRenderer.enabled)
                {
                    continue;
                }

                if (!hasVisibleBounds)
                {
                    playerBounds = playerRenderer.bounds;
                    hasVisibleBounds = true;
                }
                else
                {
                    playerBounds.Encapsulate(playerRenderer.bounds);
                }
            }

            return hasVisibleBounds
                ? playerBounds.center
                : playerObject.transform.position + Vector3.up * 1.5f;
        }

        private void GetPannedCameraPose(Vector3 focusPoint, float angle, out Vector3 position, out Quaternion rotation)
        {
            Quaternion orbitRotation = Quaternion.AngleAxis(angle, Vector3.up);
            position = focusPoint + orbitRotation * (inputCameraPose.position - focusPoint);
            rotation = orbitRotation * inputCameraPose.rotation;
        }

        private IEnumerator AnimateTransition(bool show)
        {
            float startAlpha = rootGroup.alpha;
            float targetAlpha = show ? 1f : 0f;
            Vector2 startPosition = bottomPanel != null ? bottomPanel.anchoredPosition : Vector2.zero;
            Vector2 targetPosition = show && bottomPanel != null ? bottomPanelVisiblePosition : startPosition;
            float elapsed = 0f;

            while (elapsed < TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / TransitionDuration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);

                rootGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
                if (bottomPanel != null && show)
                {
                    bottomPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
                }

                yield return null;
            }

            rootGroup.alpha = targetAlpha;
            if (bottomPanel != null && show)
            {
                bottomPanel.anchoredPosition = targetPosition;
            }

            transitionCoroutine = null;
            if (!show)
            {
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
                SetInteractable(false);
            }
        }

        private void HandleInputChanged(string value)
        {
            UpdateEnterButton();
        }

        private void HandleSubmit()
        {
            if (!canInteract || inputField == null)
            {
                return;
            }

            string submittedText = inputField.text.Trim();
            if (submittedText.Length == 0)
            {
                UpdateEnterButton();
                return;
            }

            OnSubmitted?.Invoke(submittedText);
        }

        private void UpdateEnterButton()
        {
            if (enterButton == null)
            {
                return;
            }

            bool hasText = inputField != null && !string.IsNullOrWhiteSpace(inputField.text);
            bool isEnabled = canInteract && hasText;
            enterButton.interactable = isEnabled;

            Graphic targetGraphic = enterButton.targetGraphic;
            if (targetGraphic != null)
            {
                targetGraphic.color = isEnabled ? UITheme.Accent : UITheme.Disabled;
            }
        }

        private void LogTestSubmission(string submittedText)
        {
            Debug.Log(submittedText);
        }
    }
}
