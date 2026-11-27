using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SharkTank.UI
{
    /// <summary>
    /// Directs the in-scene judge chair and speech feedback sequence.
    /// </summary>
    public sealed class JudgeFeedbackSequence : MonoBehaviour
    {
        private const int JudgeCount = 3;
        private const string Urp2DMeshLitShaderName = "Universal Render Pipeline/2D/Mesh2D-Lit-Default";

        private sealed class RendererFadeState
        {
            public Renderer renderer;
            public Material[] originalMaterials;
            public Material[] fadeMaterials;
            public string[] colorProperties;
            public Color[] originalColors;
        }

        private sealed class JudgeFadeState
        {
            public Transform root;
            public bool wasActive;
            public RendererFadeState[] renderers;
        }

        [Header("Camera")]
        [Tooltip("Existing camera controller on the UserInput screen.")]
        [SerializeField] private UserInputScreen cameraController;
        [Tooltip("Scene pose framing all three judges for the initial wide shot.")]
        [SerializeField] private Transform judgesOverviewPoint;
        [Tooltip("Scene pose used for the top-down judge overview after feedback.")]
        [SerializeField] private Transform judgesTopDownOverviewPoint;
        [Tooltip("Scene pose for the player's next input turn.")]
        [SerializeField] private Transform userInputCameraPose;
        [Tooltip("Existing camera poses for the judges, ordered left to right.")]
        [SerializeField] private Transform[] judgeCameraPoints = new Transform[JudgeCount];
        [Tooltip("Local-right camera offset used only during feedback, shifting each judge left in frame.")]
        [SerializeField] private float judgeFeedbackRightOffset = 1f;

        [Header("Offer indicators")]
        [Tooltip("Exclamation objects for judges 1, 2, and 3, in left-to-right order.")]
        [SerializeField] private Transform[] offerIndicators = new Transform[JudgeCount];
        [Tooltip("How far each visible exclamation moves above and below its starting position.")]
        [Min(0f)]
        [SerializeField] private float offerIndicatorFloatAmplitude = 0.18f;
        [Tooltip("Duration of one full up-and-down float cycle, in seconds.")]
        [Min(0.1f)]
        [SerializeField] private float offerIndicatorFloatPeriod = 2.8f;

        [Header("Feedback")]
        [Tooltip("Controller for the three judge chairs.")]
        [SerializeField] private JudgeChairsController chairsController;
        [Tooltip("Speech panel shown while the camera is on a judge.")]
        [SerializeField] private JudgeSpeechUI speechUI;
        [Tooltip("Pause after the chairs settle, in seconds.")]
        [SerializeField] private float settledPause = 0.6f;
        [Tooltip("Pause on the judges' wide shot before their chairs move, in seconds.")]
        [SerializeField] private float judgesDecisionPause = 1f;
        [Tooltip("Pause on the final top-down judge overview before moving to the player, in seconds.")]
        [SerializeField] private float topDownOverviewPause = 3f;

        [Header("Judge exits")]
        [Tooltip("Judge models in left-to-right order. If empty, the sequence looks for judge_1, judge_2, and judge_3.")]
        [SerializeField] private Transform[] judgeModelRoots = new Transform[JudgeCount];
        [Tooltip("Duration of the model fade when a judge exits, in seconds.")]
        [Min(0f)]
        [SerializeField] private float judgeOutFadeDuration = 1f;

        private Coroutine sequenceCoroutine;
        private bool chairsSettled;
        private bool arrowPressed;
        private bool continuePressed;
        private bool questionAnswered;
        private bool awaitingQuestionResponse;
        private readonly bool[] judgesOut = new bool[JudgeCount];
        private readonly bool[] judgesHaveOffers = new bool[JudgeCount];
        private readonly bool[] judgesAlreadyDecided = new bool[JudgeCount];
        private readonly JudgeFadeState[] judgeFadeStates = new JudgeFadeState[JudgeCount];
        private readonly Vector3[] offerIndicatorBasePositions = new Vector3[JudgeCount];
        private readonly float[] offerIndicatorFloatStartTimes = new float[JudgeCount];
        private Vector3 judgesTopDownOverviewBasePosition;
        private bool hasCapturedTopDownOverviewPosition;

        /// <summary>
        /// Occurs after CONTINUAR is pressed and any applicable end-of-round camera transition completes.
        /// </summary>
        public event Action OnFeedbackFinished;

        /// <summary>
        /// Occurs when the camera arrives at a judge and their feedback begins.
        /// </summary>
        public event Action<int> OnJudgeSpeakingStarted;

        /// <summary>
        /// Occurs when a judge's liked or criticized speech box is shown.
        /// </summary>
        public event Action<int, int> OnSpeechBoxShown;

        /// <summary>
        /// Occurs when a judge asks a question that needs a player response.
        /// </summary>
        public event Action<int, string, string, string> OnJudgeQuestionAsked;

        /// <summary>
        /// Occurs when a judge presents an offer.
        /// </summary>
        public event Action<int> OnJudgeOfferPresented;

        /// <summary>
        /// Occurs when a judge says they are out.
        /// </summary>
        public event Action<int> OnJudgeOutPresented;

        /// <summary>
        /// Occurs when an out judge begins fading away.
        /// </summary>
        public event Action<int> OnJudgeDisappearing;

        /// <summary>
        /// Gets whether a feedback sequence is currently running.
        /// </summary>
        public bool IsPlaying => sequenceCoroutine != null;

        /// <summary>
        /// Gets whether the sequence is waiting for the current judge's question response.
        /// </summary>
        public bool IsWaitingForQuestionResponse => awaitingQuestionResponse;

        /// <summary>
        /// Gets the dialogue UI shared by judge feedback and the final session summary.
        /// </summary>
        public JudgeSpeechUI SpeechUI => speechUI;

        /// <summary>
        /// Gets whether every judge has made an offer or is out.
        /// </summary>
        public bool AreAllJudgesDecided
        {
            get
            {
                for (int i = 0; i < JudgeCount; i++)
                {
                    if (!IsJudgeDecided(i))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            CaptureOfferIndicatorBasePositions();
            SetAllOfferIndicatorsVisible(false);
            CaptureTopDownOverviewPosition();
        }

        private void Update()
        {
            if (offerIndicatorFloatAmplitude <= 0f || offerIndicatorFloatPeriod <= 0f)
            {
                return;
            }

            float angularSpeed = 2f * Mathf.PI / offerIndicatorFloatPeriod;
            for (int i = 0; i < JudgeCount; i++)
            {
                Transform indicator = GetOfferIndicator(i);
                if (indicator == null || !indicator.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 localPosition = offerIndicatorBasePositions[i];
                float elapsed = Time.unscaledTime - offerIndicatorFloatStartTimes[i];
                localPosition.y += Mathf.Sin(elapsed * angularSpeed) * offerIndicatorFloatAmplitude;
                indicator.localPosition = localPosition;
            }
        }

        private void OnEnable()
        {
            if (speechUI != null)
            {
                speechUI.OnArrowPressed += HandleArrowPressed;
                speechUI.OnContinuePressed += HandleContinuePressed;
            }
        }

        private void OnDisable()
        {
            if (speechUI != null)
            {
                speechUI.OnArrowPressed -= HandleArrowPressed;
                speechUI.OnContinuePressed -= HandleContinuePressed;
            }

            if (chairsController != null)
            {
                chairsController.OnChairsSettled -= HandleChairsSettled;
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            for (int i = 0; i < JudgeCount; i++)
            {
                if (!judgesOut[i])
                {
                    RestoreJudgeModel(i);
                }
            }

            questionAnswered = false;
            awaitingQuestionResponse = false;
        }

        /// <summary>
        /// Plays the feedback sequence for three judges in left-to-right order.
        /// </summary>
        /// <param name="feedback">Already-split feedback data in judge order.</param>
        /// <param name="isPitchRound">Whether the feedback follows the initial pitch.</param>
        /// <param name="showTopDownOverview">Whether to show the final overview when all judges have made a decision.</param>
        /// <returns>True when the feedback sequence starts successfully.</returns>
        public bool Play(IList<JudgeFeedbackData> feedback, bool isPitchRound, bool showTopDownOverview = true)
        {
            if (sequenceCoroutine != null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is already playing a feedback sequence.", this);
                return false;
            }

            if (isPitchRound)
            {
                ResetJudgeDecisionStatus();
            }

            if (!ValidateInput(feedback) || !ValidateJudgeOutModels(feedback))
            {
                return false;
            }

            arrowPressed = false;
            continuePressed = false;
            sequenceCoroutine = StartCoroutine(RunSequence(feedback, isPitchRound, showTopDownOverview));
            return true;
        }

        /// <summary>
        /// Resumes the sequence after the player has answered the current judge's question.
        /// </summary>
        public void CompleteQuestionResponse()
        {
            if (!awaitingQuestionResponse)
            {
                Debug.LogWarning("JudgeFeedbackSequence is not waiting for a question response.", this);
                return;
            }

            questionAnswered = true;
            awaitingQuestionResponse = false;
        }

        [ContextMenu("Test: Feedback Round 1")]
        private void TestFeedbackRoundOne()
        {
            StartTestFeedback(false);
        }

        [ContextMenu("Test: Judge 2 Is Out")]
        private void TestJudgeTwoIsOut()
        {
            StartTestFeedback(judgeTwoIsOut: true);
        }

        [ContextMenu("Test: Judge 2 Makes an Offer")]
        private void TestJudgeTwoMakesAnOffer()
        {
            StartTestFeedback(judgeTwoMakesOffer: true);
        }

        [ContextMenu("Test: Reset Judge Decision Status")]
        private void TestResetJudgeDecisionStatus()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Start the scene in Play Mode before resetting judge out status.", this);
                return;
            }

            if (sequenceCoroutine != null)
            {
                Debug.LogWarning("Judge out status cannot be reset while a feedback sequence is running.", this);
                return;
            }

            ResolveJudgeModelRoots();
            ResetJudgeDecisionStatus();
        }

        [ContextMenu("Test: Reset Chairs")]
        private void TestResetChairs()
        {
            if (chairsController == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence cannot reset chairs because JudgeChairsController is not assigned.", this);
                return;
            }

            chairsController.ResetAll();
        }

        [ContextMenu("Test: Top-Down Overview")]
        private void TestTopDownOverview()
        {
            if (sequenceCoroutine != null)
            {
                Debug.LogWarning("JudgeFeedbackSequence cannot test the top-down overview while a feedback sequence is running.", this);
                return;
            }

            if (cameraController == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence cannot test the top-down overview because UserInputScreen is not assigned.", this);
                return;
            }

            if (judgesTopDownOverviewPoint == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence cannot test the top-down overview because its camera point is not assigned.", this);
                return;
            }

            UpdateTopDownOverviewPosition();
            cameraController.CutCameraToPose(judgesTopDownOverviewPoint);
        }

        private IEnumerator RunSequence(IList<JudgeFeedbackData> feedback, bool isPitchRound, bool showTopDownOverview)
        {
            yield return speechUI.Hide();

            cameraController.CutCameraToPose(judgesOverviewPoint);
            if (judgesDecisionPause > 0f)
            {
                yield return new WaitForSecondsRealtime(judgesDecisionPause);
            }

            List<int> attitudes = new List<int>(JudgeCount);
            for (int i = 0; i < JudgeCount; i++)
            {
                attitudes.Add(feedback[i].attitude);
                judgesAlreadyDecided[i] = IsJudgeDecided(i);
            }

            chairsSettled = false;
            chairsController.OnChairsSettled += HandleChairsSettled;
            chairsController.ApplyAttitudes(attitudes, judgesAlreadyDecided);
            yield return new WaitUntil(() => chairsSettled);
            chairsController.OnChairsSettled -= HandleChairsSettled;

            if (settledPause > 0f)
            {
                yield return new WaitForSecondsRealtime(settledPause);
            }

            bool showedJudgeSpeech = false;
            for (int judgeIndex = 0; judgeIndex < JudgeCount; judgeIndex++)
            {
                JudgeFeedbackData judgeFeedback = feedback[judgeIndex];
                if (judgeFeedback.skipFeedback || IsJudgeDecided(judgeIndex))
                {
                    continue;
                }

                Transform judgeCameraPoint = judgeCameraPoints[judgeIndex];
                if (judgeCameraPoint == null)
                {
                    Debug.LogWarning("JudgeFeedbackSequence is missing the camera point for judge index " + judgeIndex + "; showing speech without a camera pan.", this);
                }
                else
                {
                    yield return cameraController.MoveCameraToPose(judgeCameraPoint, judgeFeedbackRightOffset);
                }

                OnJudgeSpeakingStarted?.Invoke(judgeIndex);
                yield return speechUI.Show();

                if (judgeFeedback.isApiResponse)
                {
                    string apiText = !string.IsNullOrWhiteSpace(judgeFeedback.offerText)
                        ? judgeFeedback.offerText
                        : judgeFeedback.isOut
                            ? judgeFeedback.outText
                            : judgeFeedback.likedText;
                    if (string.IsNullOrWhiteSpace(apiText))
                    {
                        Debug.LogWarning("JudgeFeedbackSequence received an empty API response for " + judgeFeedback.judgeName + ".", this);
                        yield return speechUI.Hide();
                        continue;
                    }

                    if (judgeFeedback.isOut)
                    {
                        OnJudgeOutPresented?.Invoke(judgeIndex);
                    }
                    else if (!string.IsNullOrWhiteSpace(judgeFeedback.offerText))
                    {
                        SetOfferIndicatorVisible(judgeIndex, true);
                        OnJudgeOfferPresented?.Invoke(judgeIndex);
                    }

                    string[] speechSegments = judgeFeedback.speechSegments;
                    if (speechSegments == null || speechSegments.Length == 0)
                    {
                        speechSegments = new[] { apiText };
                    }

                    bool showedApiSpeech = false;
                    for (int segmentIndex = 0; segmentIndex < speechSegments.Length; segmentIndex++)
                    {
                        string speechSegment = speechSegments[segmentIndex];
                        if (string.IsNullOrWhiteSpace(speechSegment))
                        {
                            continue;
                        }

                        showedApiSpeech = true;
                        speechUI.ShowText(judgeFeedback.judgeName, speechSegment);
                        OnSpeechBoxShown?.Invoke(judgeIndex, segmentIndex);
                        arrowPressed = false;
                        yield return new WaitUntil(() => arrowPressed);
                    }

                    if (!showedApiSpeech)
                    {
                        speechUI.ShowText(judgeFeedback.judgeName, apiText);
                        OnSpeechBoxShown?.Invoke(judgeIndex, 0);
                        arrowPressed = false;
                        yield return new WaitUntil(() => arrowPressed);
                    }
                }
                else
                {
                    string firstText = judgeFeedback.isOut
                        ? string.IsNullOrWhiteSpace(judgeFeedback.outText)
                            ? "Estoy fuera; no haré una oferta."
                            : judgeFeedback.outText
                        : string.IsNullOrWhiteSpace(judgeFeedback.likedText)
                            ? "Todavía no me convences del todo."
                            : judgeFeedback.likedText;
                    string secondText = judgeFeedback.isOut
                        ? string.IsNullOrWhiteSpace(judgeFeedback.outReason)
                            ? "El proyecto no se ajusta a lo que busco en una inversión."
                            : judgeFeedback.outReason
                        : !string.IsNullOrWhiteSpace(judgeFeedback.offerText)
                            ? judgeFeedback.offerText
                        : string.IsNullOrWhiteSpace(judgeFeedback.criticismText)
                            ? isPitchRound
                                ? "¡Presentaste muy bien tus ideas!"
                                : "¡Respondiste muy bien a mis dudas!"
                            : judgeFeedback.criticismText;

                    speechUI.ShowText(judgeFeedback.judgeName, firstText);
                    OnSpeechBoxShown?.Invoke(judgeIndex, 0);
                    if (judgeFeedback.isOut)
                    {
                        OnJudgeOutPresented?.Invoke(judgeIndex);
                    }

                    arrowPressed = false;
                    yield return new WaitUntil(() => arrowPressed);

                    speechUI.ShowText(judgeFeedback.judgeName, secondText);
                    OnSpeechBoxShown?.Invoke(judgeIndex, 1);
                    if (!judgeFeedback.isOut && !string.IsNullOrWhiteSpace(judgeFeedback.offerText))
                    {
                        SetOfferIndicatorVisible(judgeIndex, true);
                        OnJudgeOfferPresented?.Invoke(judgeIndex);
                    }

                    arrowPressed = false;
                    yield return new WaitUntil(() => arrowPressed);
                }

                if (!string.IsNullOrWhiteSpace(judgeFeedback.question))
                {
                    yield return speechUI.Hide();
                    questionAnswered = false;
                    awaitingQuestionResponse = true;
                    OnJudgeQuestionAsked?.Invoke(
                        judgeIndex,
                        judgeFeedback.judgeName,
                        judgeFeedback.question,
                        judgeFeedback.hint);
                    yield return new WaitUntil(() => questionAnswered);

                    if (judgeCameraPoint != null)
                    {
                        yield return cameraController.MoveCameraToPose(judgeCameraPoint, judgeFeedbackRightOffset);
                    }
                }

                showedJudgeSpeech = true;
                if (judgeFeedback.isOut && PrepareJudgeFadeState(judgeIndex))
                {
                    OnJudgeDisappearing?.Invoke(judgeIndex);
                    yield return FadeOutJudgeModel(judgeIndex);
                    judgesOut[judgeIndex] = true;
                }
                else if (!string.IsNullOrWhiteSpace(judgeFeedback.offerText))
                {
                    judgesHaveOffers[judgeIndex] = true;
                }

                if (HasActiveJudgeAfter(judgeIndex))
                {
                    yield return speechUI.Hide();
                }
            }

            if (!showedJudgeSpeech)
            {
                yield return speechUI.Show();
            }

            speechUI.ShowContinue();
            yield return new WaitUntil(() => continuePressed);
            yield return speechUI.Hide();
            if (showTopDownOverview && AreAllJudgesDecided)
            {
                UpdateTopDownOverviewPosition();
                cameraController.CutCameraToPose(judgesTopDownOverviewPoint);
                if (topDownOverviewPause > 0f)
                {
                    yield return new WaitForSecondsRealtime(topDownOverviewPause);
                }

                yield return cameraController.MoveCameraToPose(userInputCameraPose);
            }

            sequenceCoroutine = null;
            OnFeedbackFinished?.Invoke();
        }

        private void CaptureTopDownOverviewPosition()
        {
            if (hasCapturedTopDownOverviewPosition || judgesTopDownOverviewPoint == null)
            {
                return;
            }

            judgesTopDownOverviewBasePosition = judgesTopDownOverviewPoint.position;
            hasCapturedTopDownOverviewPosition = true;
        }

        private void UpdateTopDownOverviewPosition()
        {
            CaptureTopDownOverviewPosition();
            if (!hasCapturedTopDownOverviewPosition)
            {
                Debug.LogWarning("JudgeFeedbackSequence cannot position the top-down overview because its point is missing.", this);
                return;
            }

            Vector3 overviewPosition = judgesTopDownOverviewBasePosition;
            if (chairsController.TryGetHighestChairVerticalOffset(out float verticalOffset))
            {
                overviewPosition.y += verticalOffset;
            }
            else
            {
                Debug.LogWarning("JudgeFeedbackSequence could not adjust the top-down overview because no active chair movers are assigned.", this);
            }

            judgesTopDownOverviewPoint.position = overviewPosition;
        }

        private bool ValidateInput(IList<JudgeFeedbackData> feedback)
        {
            bool isValid = true;
            if (feedback == null || feedback.Count != JudgeCount)
            {
                Debug.LogWarning("JudgeFeedbackSequence requires feedback for exactly three judges in left-to-right order.", this);
                isValid = false;
            }
            else
            {
                for (int i = 0; i < feedback.Count; i++)
                {
                    if (feedback[i] == null)
                    {
                        Debug.LogWarning("JudgeFeedbackSequence received missing feedback at judge index " + i + ".", this);
                        isValid = false;
                    }
                }
            }

            if (cameraController == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its UserInputScreen camera controller.", this);
                isValid = false;
            }

            if (judgesOverviewPoint == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its judges overview camera point.", this);
                isValid = false;
            }

            if (judgesTopDownOverviewPoint == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its JudgesTopDownOverviewPoint.", this);
                isValid = false;
            }

            if (userInputCameraPose == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its UserInputCameraPose.", this);
                isValid = false;
            }

            if (judgeCameraPoints == null || judgeCameraPoints.Length < JudgeCount)
            {
                Debug.LogWarning("JudgeFeedbackSequence needs three judge camera points ordered from left to right.", this);
                isValid = false;
            }

            if (chairsController == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its JudgeChairsController.", this);
                isValid = false;
            }

            if (speechUI == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing its JudgeSpeechUI.", this);
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateJudgeOutModels(IList<JudgeFeedbackData> feedback)
        {
            ResolveJudgeModelRoots();
            bool isValid = true;

            for (int i = 0; i < JudgeCount; i++)
            {
                if (feedback[i].isOut && !judgesOut[i] && !CanFadeJudgeModel(i))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        private void ResolveJudgeModelRoots()
        {
            if (judgeModelRoots == null || judgeModelRoots.Length < JudgeCount)
            {
                Transform[] resizedRoots = new Transform[JudgeCount];
                if (judgeModelRoots != null)
                {
                    Array.Copy(judgeModelRoots, resizedRoots, judgeModelRoots.Length);
                }

                judgeModelRoots = resizedRoots;
            }

            for (int i = 0; i < JudgeCount; i++)
            {
                if (judgeModelRoots[i] != null)
                {
                    continue;
                }

                GameObject model = GameObject.Find("judge_" + (i + 1));
                if (model != null)
                {
                    judgeModelRoots[i] = model.transform;
                }
            }
        }

        private bool CanFadeJudgeModel(int judgeIndex)
        {
            Transform modelRoot = judgeModelRoots[judgeIndex];
            if (modelRoot == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence could not find the model for judge index " + judgeIndex +
                    ". Assign it in Judge Model Roots or name it judge_" + (judgeIndex + 1) + ".", this);
                return false;
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("JudgeFeedbackSequence found no renderers under the model for judge index " + judgeIndex + ".", modelRoot);
                return false;
            }

            bool foundMaterial = false;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    foundMaterial = true;
                    if (!SupportsFade(material))
                    {
                        Debug.LogWarning("JudgeFeedbackSequence cannot fade material '" + material.name +
                            "' on judge index " + judgeIndex +
                            ". Its shader needs a color or tint property that supports alpha blending.", material);
                        return false;
                    }
                }
            }

            if (!foundMaterial)
            {
                Debug.LogWarning("JudgeFeedbackSequence found no materials under the model for judge index " + judgeIndex + ".", modelRoot);
                return false;
            }

            return true;
        }

        private bool PrepareJudgeFadeState(int judgeIndex)
        {
            if (judgeFadeStates[judgeIndex] != null)
            {
                return true;
            }

            Transform modelRoot = judgeModelRoots[judgeIndex];
            Renderer[] sourceRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            RendererFadeState[] rendererStates = new RendererFadeState[sourceRenderers.Length];
            List<Material> createdMaterials = new List<Material>();

            for (int rendererIndex = 0; rendererIndex < sourceRenderers.Length; rendererIndex++)
            {
                Renderer renderer = sourceRenderers[rendererIndex];
                Material[] originalMaterials = renderer.sharedMaterials;
                Material[] fadeMaterials = new Material[originalMaterials.Length];
                string[] colorProperties = new string[originalMaterials.Length];
                Color[] originalColors = new Color[originalMaterials.Length];

                for (int materialIndex = 0; materialIndex < originalMaterials.Length; materialIndex++)
                {
                    Material original = originalMaterials[materialIndex];
                    if (original == null)
                    {
                        continue;
                    }

                    Material fadeMaterial = new Material(original);
                    if (!TryConfigureFadeMaterial(fadeMaterial, out string colorProperty))
                    {
                        Destroy(fadeMaterial);
                        for (int i = 0; i < createdMaterials.Count; i++)
                        {
                            Destroy(createdMaterials[i]);
                        }

                        Debug.LogWarning("JudgeFeedbackSequence failed to prepare a fade material for judge index " + judgeIndex + ".", this);
                        return false;
                    }

                    createdMaterials.Add(fadeMaterial);
                    fadeMaterials[materialIndex] = fadeMaterial;
                    colorProperties[materialIndex] = colorProperty;
                    originalColors[materialIndex] = fadeMaterial.GetColor(colorProperty);
                }

                rendererStates[rendererIndex] = new RendererFadeState
                {
                    renderer = renderer,
                    originalMaterials = originalMaterials,
                    fadeMaterials = fadeMaterials,
                    colorProperties = colorProperties,
                    originalColors = originalColors
                };
            }

            for (int i = 0; i < rendererStates.Length; i++)
            {
                rendererStates[i].renderer.sharedMaterials = rendererStates[i].fadeMaterials;
            }

            judgeFadeStates[judgeIndex] = new JudgeFadeState
            {
                root = modelRoot,
                wasActive = modelRoot.gameObject.activeSelf,
                renderers = rendererStates
            };
            return true;
        }

        private IEnumerator FadeOutJudgeModel(int judgeIndex)
        {
            JudgeFadeState fadeState = judgeFadeStates[judgeIndex];
            float duration = Mathf.Max(0f, judgeOutFadeDuration);
            float elapsed = 0f;

            if (duration > 0f)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    SetJudgeModelAlpha(fadeState, 1f - Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }

            SetJudgeModelAlpha(fadeState, 0f);
            fadeState.root.gameObject.SetActive(false);
        }

        private static void SetJudgeModelAlpha(JudgeFadeState fadeState, float alpha)
        {
            for (int rendererIndex = 0; rendererIndex < fadeState.renderers.Length; rendererIndex++)
            {
                RendererFadeState rendererState = fadeState.renderers[rendererIndex];
                for (int materialIndex = 0; materialIndex < rendererState.fadeMaterials.Length; materialIndex++)
                {
                    Material material = rendererState.fadeMaterials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    Color color = rendererState.originalColors[materialIndex];
                    color.a *= alpha;
                    material.SetColor(rendererState.colorProperties[materialIndex], color);
                }
            }
        }

        private void RestoreJudgeModel(int judgeIndex)
        {
            JudgeFadeState fadeState = judgeFadeStates[judgeIndex];
            if (fadeState == null)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < fadeState.renderers.Length; rendererIndex++)
            {
                RendererFadeState rendererState = fadeState.renderers[rendererIndex];
                if (rendererState.renderer != null)
                {
                    rendererState.renderer.sharedMaterials = rendererState.originalMaterials;
                }

                for (int materialIndex = 0; materialIndex < rendererState.fadeMaterials.Length; materialIndex++)
                {
                    if (rendererState.fadeMaterials[materialIndex] != null)
                    {
                        Destroy(rendererState.fadeMaterials[materialIndex]);
                    }
                }
            }

            if (fadeState.root != null)
            {
                fadeState.root.gameObject.SetActive(fadeState.wasActive);
            }

            judgeFadeStates[judgeIndex] = null;
        }

        private bool HasActiveJudgeAfter(int judgeIndex)
        {
            for (int i = judgeIndex + 1; i < JudgeCount; i++)
            {
                if (!IsJudgeDecided(i))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsJudgeDecided(int judgeIndex)
        {
            return judgesOut[judgeIndex] || judgesHaveOffers[judgeIndex];
        }

        private void ResetJudgeDecisionStatus()
        {
            for (int i = 0; i < JudgeCount; i++)
            {
                judgesOut[i] = false;
                judgesHaveOffers[i] = false;
                SetOfferIndicatorVisible(i, false);
                RestoreJudgeModel(i);
            }
        }

        private void SetAllOfferIndicatorsVisible(bool isVisible)
        {
            for (int i = 0; i < JudgeCount; i++)
            {
                SetOfferIndicatorVisible(i, isVisible);
            }
        }

        private void SetOfferIndicatorVisible(int judgeIndex, bool isVisible)
        {
            Transform indicator = GetOfferIndicator(judgeIndex);
            if (indicator == null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is missing the offer indicator for judge index " + judgeIndex + ".", this);
                return;
            }

            indicator.localPosition = offerIndicatorBasePositions[judgeIndex];
            if (isVisible)
            {
                offerIndicatorFloatStartTimes[judgeIndex] = Time.unscaledTime;
            }

            indicator.gameObject.SetActive(isVisible);
        }

        private void CaptureOfferIndicatorBasePositions()
        {
            for (int i = 0; i < JudgeCount; i++)
            {
                Transform indicator = GetOfferIndicator(i);
                if (indicator != null)
                {
                    offerIndicatorBasePositions[i] = indicator.localPosition;
                }
            }
        }

        private Transform GetOfferIndicator(int judgeIndex)
        {
            if (offerIndicators == null || judgeIndex < 0 || judgeIndex >= offerIndicators.Length)
            {
                return null;
            }

            return offerIndicators[judgeIndex];
        }

        private static bool SupportsFade(Material material)
        {
            bool hasColor = material.HasProperty("_BaseColor") || material.HasProperty("_Color");
            bool hasTransparentMode = material.HasProperty("_Mode") || material.HasProperty("_Surface");
            bool usesUrp2DMeshLitTint = IsUrp2DMeshLitMaterial(material) && material.HasProperty("_White");
            return usesUrp2DMeshLitTint || (hasColor && hasTransparentMode);
        }

        private static bool TryConfigureFadeMaterial(Material material, out string colorProperty)
        {
            colorProperty = material.HasProperty("_BaseColor")
                ? "_BaseColor"
                : material.HasProperty("_Color")
                    ? "_Color"
                    : IsUrp2DMeshLitMaterial(material) && material.HasProperty("_White")
                        ? "_White"
                        : null;
            if (colorProperty == null || !SupportsFade(material))
            {
                return false;
            }

            if (IsUrp2DMeshLitMaterial(material))
            {
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (material.HasProperty("_AlphaClip"))
                {
                    material.SetFloat("_AlphaClip", 0f);
                }
                if (material.HasProperty("_SrcBlendAlpha"))
                {
                    material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
                }

                if (material.HasProperty("_DstBlendAlpha"))
                {
                    material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
                }
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return true;
        }

        private static bool IsUrp2DMeshLitMaterial(Material material)
        {
            return material.shader != null && material.shader.name == Urp2DMeshLitShaderName;
        }

        private static List<JudgeFeedbackData> CreateTestFeedback(bool judgeTwoIsOut = false, bool judgeTwoMakesOffer = false)
        {
            return new List<JudgeFeedbackData>
            {
                new JudgeFeedbackData(
                    "Juez de Marketing",
                    2,
                    "Tu propuesta tiene una idea clara y un mercado atractivo.",
                    "Explica con más detalle cómo conseguirás tus primeros clientes."),
                new JudgeFeedbackData(
                    "Juez de Finanzas",
                    -3,
                    "La presentación fue fácil de seguir.",
                    "Los costos y las proyecciones todavía no justifican la inversión.",
                    isOut: judgeTwoIsOut,
                    outText: "Estoy fuera; no haré una oferta en este proyecto.",
                    outReason: "El riesgo financiero supera mi tolerancia, aunque la idea tenga potencial.",
                    offerText: judgeTwoMakesOffer
                        ? "Te ofrezco invertir $200,000 por el 12% de tu empresa."
                        : null),
                new JudgeFeedbackData(
                    "Juez de Operaciones",
                    0,
                    "El producto resuelve una necesidad interesante.",
                    string.Empty)
            };
        }

        private void StartTestFeedback(bool judgeTwoIsOut = false, bool judgeTwoMakesOffer = false)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Start the scene in Play Mode before testing judge feedback.", this);
                return;
            }

            OnFeedbackFinished -= LogFeedbackFinished;
            OnFeedbackFinished += LogFeedbackFinished;
            if (!Play(CreateTestFeedback(judgeTwoIsOut, judgeTwoMakesOffer), true))
            {
                OnFeedbackFinished -= LogFeedbackFinished;
            }
        }

        private void HandleChairsSettled()
        {
            chairsSettled = true;
        }

        private void HandleArrowPressed()
        {
            arrowPressed = true;
        }

        private void HandleContinuePressed()
        {
            continuePressed = true;
        }

        private void LogFeedbackFinished()
        {
            OnFeedbackFinished -= LogFeedbackFinished;
            Debug.Log("Judge feedback test sequence finished.", this);
        }
    }
}
