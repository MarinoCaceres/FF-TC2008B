using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SharkTank.UI;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Coordinates the simulation intro, player input, and judge feedback flow.
    /// </summary>
    public sealed class SimulatorController : MonoBehaviour
    {
        private enum TestStage
        {
            Idle,
            Intro,
            PitchInput,
            PitchFeedback,
            DecisionRound,
            FinalDecision,
            AnswerInput,
            Summary,
            Complete
        }

        [Header("Scene references")]
        [Tooltip("Existing screen used for player pitch and answer input.")]
        [SerializeField] private UserInputScreen userInputScreen;
        [Tooltip("Existing judge feedback sequence.")]
        [SerializeField] private JudgeFeedbackSequence judgeFeedbackSequence;
        [Tooltip("Manages the background music for player and judge turns.")]
        [SerializeField] private BGMManager bgmManager;
        [Tooltip("Manages the sound effect played while the player platform rises.")]
        [SerializeField] private SFXManager sfxManager;
        [Tooltip("Top-down camera pose shown while the player platform rises.")]
        [SerializeField] private Transform topDownCameraPose;
        [Tooltip("Camera pose used for the player's input turn.")]
        [SerializeField] private Transform playerCameraPose;
        [Tooltip("Platform the player stands on.")]
        [SerializeField] private Transform playerPlatform;
        [Tooltip("Distance the platform starts below its final position, in world units.")]
        [Min(0.1f)]
        [SerializeField] private float platformRiseDistance = 20f;
        [Tooltip("Duration of the platform rise, in seconds.")]
        [Min(0.1f)]
        [SerializeField] private float platformRiseDuration = 3f;

        [Header("Agent API")]
        [SerializeField] private string apiBaseUrl = "https://ff.901290.xyz";
        [Min(1)]
        [SerializeField] private int apiTimeoutSeconds = 45;
        [Tooltip("Writes API request and response details, including bodies, to the Unity Console.")]
        [SerializeField] private bool logApiTraffic = true;
        [Min(1)]
        [SerializeField] private int maxQuestionRounds = 3;
        [SerializeField]
        private SharkTankApiClient.SharkSetup[] agentProfiles =
        {
            new SharkTankApiClient.SharkSetup { name = "Master Shark", modifier1 = 3, modifier2 = 3 },
            new SharkTankApiClient.SharkSetup { name = "Don Capital", modifier1 = 3, modifier2 = 3 },
            new SharkTankApiClient.SharkSetup { name = "Dr. Sabor", modifier1 = 3, modifier2 = 3 }
        };
        [Tooltip("Existing dialogue UI used to present the API's final evaluation.")]
        [SerializeField] private JudgeSpeechUI sessionSummaryUI;

        private TestStage currentStage;
        private TestStage questionReturnStage;
        private Coroutine introCoroutine;
        private Coroutine responseCoroutine;
        private string currentQuestionJudgeName;
        private string currentQuestionText;
        private string currentQuestionHint;
        private int decisionRoundIndex;
        private int questionRoundsCompleted;
        [Tooltip("Use only Judge 1's API profile; Judges 2 and 3 use hardcoded test feedback.")]
        [SerializeField] private bool useSingleAgentTestMode;
        private bool summaryTextFinished;
        private bool summaryContinuePressed;
        private string apiSessionId;
        private JudgeFeedbackData[] pendingApiFeedback;
        private bool[] apiJudgesDecided;
        private SharkTankApiClient apiClient;
        private Vector3 platformFinalPosition;
        private bool hasCapturedPlatformFinalPosition;

        private void Awake()
        {
            if (bgmManager == null)
            {
                bgmManager = GetComponent<BGMManager>();
            }

            if (bgmManager == null)
            {
                Debug.LogWarning("SimulatorController is missing its BGMManager reference.", this);
            }

            if (sfxManager == null)
            {
                sfxManager = GetComponent<SFXManager>();
            }

            apiClient = new SharkTankApiClient(apiBaseUrl, apiTimeoutSeconds, logApiTraffic);
            apiJudgesDecided = new bool[3];
            maxQuestionRounds = Mathf.Max(1, maxQuestionRounds);
            if (sessionSummaryUI == null && judgeFeedbackSequence != null)
            {
                sessionSummaryUI = judgeFeedbackSequence.SpeechUI;
            }

            if (playerPlatform != null)
            {
                platformFinalPosition = playerPlatform.position;
                hasCapturedPlatformFinalPosition = true;
                PositionPlatformAtIntroStart();
            }

            if (userInputScreen != null && topDownCameraPose != null)
            {
                userInputScreen.CutCameraToPose(topDownCameraPose);
            }
        }

        private void Start()
        {
            BeginSimulatorFlow();
        }

        private void OnEnable()
        {
            if (userInputScreen != null)
            {
                userInputScreen.OnSubmitted -= HandleResponseSubmitted;
                userInputScreen.OnSubmitted += HandleResponseSubmitted;
            }

            if (judgeFeedbackSequence != null)
            {
                judgeFeedbackSequence.OnFeedbackFinished -= HandleFeedbackFinished;
                judgeFeedbackSequence.OnFeedbackFinished += HandleFeedbackFinished;
                judgeFeedbackSequence.OnJudgeQuestionAsked -= HandleJudgeQuestionAsked;
                judgeFeedbackSequence.OnJudgeQuestionAsked += HandleJudgeQuestionAsked;
                judgeFeedbackSequence.OnJudgeSpeakingStarted -= HandleJudgeSpeakingStarted;
                judgeFeedbackSequence.OnJudgeSpeakingStarted += HandleJudgeSpeakingStarted;
            }

            if (sessionSummaryUI != null)
            {
                sessionSummaryUI.OnTextFinished -= HandleSummaryTextFinished;
                sessionSummaryUI.OnTextFinished += HandleSummaryTextFinished;
                sessionSummaryUI.OnContinuePressed -= HandleSummaryContinuePressed;
                sessionSummaryUI.OnContinuePressed += HandleSummaryContinuePressed;
            }
        }

        private void OnDisable()
        {
            if (userInputScreen != null)
            {
                userInputScreen.OnSubmitted -= HandleResponseSubmitted;
            }

            if (judgeFeedbackSequence != null)
            {
                judgeFeedbackSequence.OnFeedbackFinished -= HandleFeedbackFinished;
                judgeFeedbackSequence.OnJudgeQuestionAsked -= HandleJudgeQuestionAsked;
                judgeFeedbackSequence.OnJudgeSpeakingStarted -= HandleJudgeSpeakingStarted;
            }

            if (sessionSummaryUI != null)
            {
                sessionSummaryUI.OnTextFinished -= HandleSummaryTextFinished;
                sessionSummaryUI.OnContinuePressed -= HandleSummaryContinuePressed;
            }

            if (responseCoroutine != null)
            {
                StopCoroutine(responseCoroutine);
                responseCoroutine = null;
                currentStage = TestStage.Idle;
            }

            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
                introCoroutine = null;
                sfxManager?.StopPlatformRiseSound();
                currentStage = TestStage.Idle;
            }
        }

        [ContextMenu("Test: Start Simulator Flow")]
        private void TestStartSimulatorFlow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Start the scene in Play Mode before testing the simulator flow.", this);
                return;
            }

            BeginSimulatorFlow();
        }

        [ContextMenu("Test: Judge 1 API, Judges 2-3 Hardcoded")]
        private void TestSingleAgentMode()
        {
            if (!CanChangeAgentMode())
            {
                Debug.LogWarning("Change the agent test mode before a judge API session has been created.", this);
                return;
            }

            useSingleAgentTestMode = true;
            Debug.Log(
                "Single-agent test enabled: only Judge 1 uses the API; Judges 2 and 3 use hardcoded feedback. The test ends after all three judges are in or out.",
                this);
        }

        [ContextMenu("Test: Use All Three API Agents")]
        private void TestAllAgentMode()
        {
            if (!CanChangeAgentMode())
            {
                Debug.LogWarning("Change the agent test mode before a judge API session has been created.", this);
                return;
            }

            useSingleAgentTestMode = false;
            Debug.Log("All three judges will use the API.", this);
        }

        private bool CanChangeAgentMode()
        {
            return Application.isPlaying &&
                   string.IsNullOrEmpty(apiSessionId) &&
                   (currentStage == TestStage.Idle ||
                    currentStage == TestStage.Intro ||
                    currentStage == TestStage.PitchInput ||
                    currentStage == TestStage.Complete);
        }

        private void BeginSimulatorFlow()
        {
            if (currentStage != TestStage.Idle && currentStage != TestStage.Complete)
            {
                Debug.LogWarning("The simulator test flow is already in progress.", this);
                return;
            }

            if (!ValidateReferences())
            {
                return;
            }

            if (judgeFeedbackSequence.IsPlaying)
            {
                Debug.LogWarning("The judge feedback sequence is already playing; the simulator test cannot start.", this);
                return;
            }

            PositionPlatformAtIntroStart();
            userInputScreen.CutCameraToPose(topDownCameraPose);
            currentStage = TestStage.Intro;
            introCoroutine = StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            sfxManager.StartPlatformRiseSound();

            Vector3 startPosition = platformFinalPosition + Vector3.down * platformRiseDistance;
            float elapsed = 0f;
            while (elapsed < platformRiseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / platformRiseDuration);
                playerPlatform.position = Vector3.Lerp(
                    startPosition,
                    platformFinalPosition,
                    Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            playerPlatform.position = platformFinalPosition;
            sfxManager.StopPlatformRiseSound();

            yield return userInputScreen.MoveCameraToPose(playerCameraPose);

            bgmManager.PlayPlayerTrack();
            currentStage = TestStage.PitchInput;
            Debug.Log("Simulator started. Enter a pitch to begin.", this);
            userInputScreen.Show(
                "Presenta tu pitch ante los jueces.",
                "Escribe tu pitch aquí...",
                questionHint: UserInputScreen.PitchHelpHint);
            introCoroutine = null;
        }

        private void PositionPlatformAtIntroStart()
        {
            if (playerPlatform != null && hasCapturedPlatformFinalPosition)
            {
                playerPlatform.position = platformFinalPosition + Vector3.down * platformRiseDistance;
            }
        }

        private void HandleResponseSubmitted(string response)
        {
            if (currentStage == TestStage.AnswerInput)
            {
                if (!judgeFeedbackSequence.IsWaitingForQuestionResponse)
                {
                    Debug.LogWarning("The response was not processed because no judge question is awaiting an answer.", this);
                    return;
                }

                bgmManager?.PlayJudgeTrack();
                userInputScreen.SetInteractable(false);
                responseCoroutine = StartCoroutine(HideQuestionAndContinue(response));
                return;
            }

            if (currentStage != TestStage.PitchInput)
            {
                return;
            }

            if (judgeFeedbackSequence.IsPlaying)
            {
                Debug.LogWarning("The response was not processed because judge feedback is already playing.", this);
                return;
            }

            bgmManager?.PlayJudgeTrack();
            currentStage = TestStage.PitchFeedback;
            userInputScreen.SetInteractable(false);
            responseCoroutine = StartCoroutine(HideInputAndPlayFeedback(response));
        }

        private IEnumerator HideQuestionAndContinue(string response)
        {
            yield return userInputScreen.HideAndWait(false);

            SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput> result =
                new SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput>();
            yield return apiClient.SendMessage(apiSessionId, response, result);
            if (!result.Succeeded)
            {
                Debug.LogError("Could not send the answer to the judge API: " + result.Error, this);
                ShowQuestionRetry();
                responseCoroutine = null;
                yield break;
            }

            questionRoundsCompleted++;
            if (!TryBuildFeedback(result.Value, false, out JudgeFeedbackData[] feedback, out string error))
            {
                questionRoundsCompleted--;
                Debug.LogError("The judge API returned unusable feedback: " + error, this);
                ShowQuestionRetry();
                responseCoroutine = null;
                yield break;
            }

            pendingApiFeedback = feedback;
            Debug.Log("Respuesta de " + currentQuestionJudgeName + " enviada a los agentes: " + response, this);
            userInputScreen.ClearQuestionJudgeCamera();
            currentStage = questionReturnStage;
            judgeFeedbackSequence.CompleteQuestionResponse();
            responseCoroutine = null;
        }

        private IEnumerator HideInputAndPlayFeedback(string response)
        {
            yield return userInputScreen.HideAndWait(false);

            SharkTankApiClient.OperationResult<SharkTankApiClient.SessionOutput> sessionResult =
                new SharkTankApiClient.OperationResult<SharkTankApiClient.SessionOutput>();
            SharkTankApiClient.SharkSetup[] selectedProfiles =
                useSingleAgentTestMode ? new[] { agentProfiles[0] } : agentProfiles;
            Debug.Log(
                useSingleAgentTestMode
                    ? "Creating API session with exactly 1 agent: " + agentProfiles[0].name + ". Judges 2 and 3 use hardcoded feedback."
                    : "Creating API session with all 3 configured agents.",
                this);
            yield return apiClient.CreateSession(
                selectedProfiles,
                sessionResult);
            if (!sessionResult.Succeeded)
            {
                Debug.LogError("Could not create the judge API session: " + sessionResult.Error, this);
                ShowPitchRetry();
                responseCoroutine = null;
                yield break;
            }

            apiSessionId = sessionResult.Value.session_id;
            questionRoundsCompleted = 0;
            decisionRoundIndex = 0;
            Array.Clear(apiJudgesDecided, 0, apiJudgesDecided.Length);
            pendingApiFeedback = null;

            SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput> result =
                new SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput>();
            yield return apiClient.SendMessage(apiSessionId, response, result);
            if (!result.Succeeded)
            {
                Debug.LogError("Could not send the pitch to the judge API: " + result.Error, this);
                ShowPitchRetry();
                responseCoroutine = null;
                yield break;
            }

            if (!TryBuildFeedback(result.Value, false, out JudgeFeedbackData[] feedback, out string error))
            {
                Debug.LogError("The judge API returned unusable pitch feedback: " + error, this);
                ShowPitchRetry();
                responseCoroutine = null;
                yield break;
            }

            if (!judgeFeedbackSequence.Play(feedback, true, false))
            {
                FailApiFlow("Pitch feedback could not start.");
                yield break;
            }

            responseCoroutine = null;
            Debug.Log("Pitch enviado a los agentes.", this);
        }

        private void HandleFeedbackFinished()
        {
            switch (currentStage)
            {
                case TestStage.PitchFeedback:
                    currentStage = TestStage.DecisionRound;
                    decisionRoundIndex = 0;
                    Debug.Log("La ronda del pitch terminó. Continuando con las respuestas de los agentes.", this);
                    if (pendingApiFeedback != null)
                    {
                        PlayPendingFeedback();
                    }
                    else
                    {
                        StartFinalDecisionRequest();
                    }
                    break;

                case TestStage.DecisionRound:
                    if (judgeFeedbackSequence.AreAllJudgesDecided)
                    {
                        StartSummaryRequest();
                        break;
                    }

                    if (pendingApiFeedback != null)
                    {
                        decisionRoundIndex++;
                        PlayPendingFeedback();
                    }
                    else
                    {
                        StartFinalDecisionRequest();
                    }
                    break;

                case TestStage.FinalDecision:
                    if (judgeFeedbackSequence.AreAllJudgesDecided)
                    {
                        StartSummaryRequest();
                    }
                    else
                    {
                        FailApiFlow("The final-decision response did not resolve every active judge.");
                    }
                    break;

                default:
                    break;
            }
        }

        private void HandleJudgeQuestionAsked(int judgeIndex, string judgeName, string question, string hint)
        {
            if (currentStage != TestStage.PitchFeedback && currentStage != TestStage.DecisionRound)
            {
                Debug.LogWarning("SimulatorController received a judge question outside a judge feedback round.", this);
                return;
            }

            questionReturnStage = currentStage;
            currentStage = TestStage.AnswerInput;
            currentQuestionJudgeName = judgeName;
            currentQuestionText = question;
            currentQuestionHint = hint;
            bgmManager?.PlayPlayerTrack();
            userInputScreen.SetQuestionJudgeCamera(judgeIndex);
            string responsePrompt = question.IndexOf('?') < question.LastIndexOf('?')
                ? "Responde todas las dudas del juez: "
                : "Responde la duda del juez: ";
            userInputScreen.Show(
                responsePrompt + judgeName,
                "Escribe tu respuesta aquí...",
                question,
                hint);
        }

        private void HandleJudgeSpeakingStarted(int judgeIndex)
        {
            bgmManager?.PlayJudgeTrack();
        }

        private bool TryBuildFeedback(
            SharkTankApiClient.RoundOutput round,
            bool isFinalDecision,
            out JudgeFeedbackData[] feedback,
            out string error)
        {
            feedback = null;
            error = null;
            if (round == null || round.responses == null)
            {
                error = "The response did not contain a responses list.";
                return false;
            }

            Dictionary<string, SharkTankApiClient.SharkResponse> responsesByName =
                new Dictionary<string, SharkTankApiClient.SharkResponse>(StringComparer.OrdinalIgnoreCase);
            foreach (SharkTankApiClient.SharkResponse response in round.responses)
            {
                if (response == null || string.IsNullOrWhiteSpace(response.shark))
                {
                    error = "A response is missing its shark name.";
                    return false;
                }

                if (responsesByName.ContainsKey(response.shark))
                {
                    error = "The response contains a duplicate entry for " + response.shark + ".";
                    return false;
                }

                responsesByName.Add(response.shark, response);
            }

            if (useSingleAgentTestMode)
            {
                foreach (string responseName in responsesByName.Keys)
                {
                    if (!string.Equals(
                            responseName,
                            agentProfiles[0].name,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        error = "Single-agent test expected only " + agentProfiles[0].name +
                                " but the API also returned " + responseName + ".";
                        return false;
                    }
                }
            }

            JudgeFeedbackData[] combinedFeedback = useSingleAgentTestMode
                ? CreateTestFeedback(isFinalDecision ? false : currentStage == TestStage.PitchFeedback, decisionRoundIndex)
                : new JudgeFeedbackData[3];

            for (int judgeIndex = 0; judgeIndex < 3; judgeIndex++)
            {
                string sharkName = agentProfiles[judgeIndex].name;
                if (useSingleAgentTestMode && judgeIndex > 0)
                {
                    continue;
                }

                if (!responsesByName.TryGetValue(sharkName, out SharkTankApiClient.SharkResponse response))
                {
                    if (apiJudgesDecided[judgeIndex])
                    {
                        combinedFeedback[judgeIndex] = new JudgeFeedbackData(
                            sharkName,
                            0,
                            null,
                            null,
                            skipFeedback: true);
                        continue;
                    }

                    error = "The response is missing active judge " + sharkName + ".";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(response.content))
                {
                    error = "The response from " + sharkName + " has no content.";
                    return false;
                }

                string responseType = response.type?.Trim().ToLowerInvariant();
                bool isOut = responseType == "out";
                bool isOffer = isFinalDecision && responseType == "in";
                bool isQuestion = responseType == "question";
                if (!isOut && !isQuestion && responseType != "in")
                {
                    error = "The response from " + sharkName + " has an unsupported type: " + response.type + ".";
                    return false;
                }

                if (isFinalDecision && isQuestion)
                {
                    error = "The final-decision response from " + sharkName + " was another question.";
                    return false;
                }

                if (isOut || isOffer)
                {
                    apiJudgesDecided[judgeIndex] = true;
                }

                combinedFeedback[judgeIndex] = new JudgeFeedbackData(
                    sharkName,
                    Mathf.Clamp(response.attitude, -3, 3),
                    isOut || isOffer ? null : response.content,
                    null,
                    isOut: isOut,
                    outText: isOut ? response.content : null,
                    question: isQuestion && questionRoundsCompleted < maxQuestionRounds
                        ? BuildQuestionPrompt(response.content)
                        : null,
                    offerText: isOffer ? response.content : null,
                    hint: response.hint,
                    isApiResponse: true,
                    speechSegments: SplitApiSpeech(response.content));
            }

            feedback = combinedFeedback;
            return true;
        }

        private static string[] SplitApiSpeech(string content)
        {
            List<string> segments = new List<string>();
            StringBuilder segment = new StringBuilder();
            for (int i = 0; i < content.Length; i++)
            {
                char character = content[i];
                if (segment.Length == 0 && char.IsWhiteSpace(character))
                {
                    continue;
                }

                segment.Append(character);
                if (character != '?' && !IsSentencePeriod(content, i))
                {
                    continue;
                }

                while (i + 1 < content.Length && IsQuestionClosingCharacter(content[i + 1]))
                {
                    segment.Append(content[++i]);
                }

                AddSpeechSegment(segments, segment);
            }

            AddSpeechSegment(segments, segment);
            return segments.ToArray();
        }

        private static bool IsSentencePeriod(string content, int index)
        {
            if (content[index] != '.')
            {
                return false;
            }

            if (index > 0 &&
                index + 1 < content.Length &&
                char.IsDigit(content[index - 1]) &&
                char.IsDigit(content[index + 1]))
            {
                return false;
            }

            return index + 1 == content.Length ||
                   char.IsWhiteSpace(content[index + 1]) ||
                   IsQuestionClosingCharacter(content[index + 1]);
        }

        private static string BuildQuestionPrompt(string content)
        {
            List<string> questions = new List<string>();
            string[] segments = SplitApiSpeech(content);
            foreach (string speechSegment in segments)
            {
                int questionMarkIndex = speechSegment.LastIndexOf('?');
                if (questionMarkIndex < 0)
                {
                    continue;
                }

                string question = speechSegment.Substring(0, questionMarkIndex + 1).Trim();
                int spanishQuestionStart = question.LastIndexOf('¿');
                if (spanishQuestionStart >= 0)
                {
                    question = question.Substring(spanishQuestionStart);
                }
                else
                {
                    int sentenceBoundary = Math.Max(
                        question.LastIndexOf('.'),
                        Math.Max(question.LastIndexOf('!'), question.LastIndexOf('\n')));
                    if (sentenceBoundary >= 0)
                    {
                        question = question.Substring(sentenceBoundary + 1).Trim();
                    }
                }

                if (!string.IsNullOrWhiteSpace(question))
                {
                    questions.Add(question);
                }
            }

            return questions.Count == 0
                ? content.Trim()
                : string.Join("\n", questions.ToArray());
        }

        private static void AddSpeechSegment(List<string> segments, StringBuilder segment)
        {
            string text = segment.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                segments.Add(text);
            }

            segment.Length = 0;
        }

        private static bool IsQuestionClosingCharacter(char character)
        {
            return character == '"' ||
                   character == '\'' ||
                   character == ')' ||
                   character == ']' ||
                   character == '}';
        }

        private void PlayPendingFeedback()
        {
            JudgeFeedbackData[] feedback = pendingApiFeedback;
            pendingApiFeedback = null;
            if (feedback == null || !judgeFeedbackSequence.Play(feedback, false, true))
            {
                FailApiFlow("The next agent feedback round could not be started.");
            }
        }

        private void StartFinalDecisionRequest()
        {
            if (currentStage == TestStage.FinalDecision || currentStage == TestStage.Summary)
            {
                return;
            }

            currentStage = TestStage.FinalDecision;
            if (useSingleAgentTestMode && apiJudgesDecided[0])
            {
                JudgeFeedbackData[] feedback = CreateTestFeedback(false, decisionRoundIndex);
                feedback[0].skipFeedback = true;
                Debug.Log("Judge 1 is already decided; finishing the single-agent test with the two hardcoded judges.", this);
                if (!judgeFeedbackSequence.Play(feedback, false, true))
                {
                    FailApiFlow("The hardcoded final decisions could not be presented.");
                }

                return;
            }

            responseCoroutine = StartCoroutine(RequestFinalDecisions());
        }

        private IEnumerator RequestFinalDecisions()
        {
            const string finalDecisionPrompt =
                "Toma tu decisión final sobre la propuesta. Si haces una oferta, responde con type 'in' e incluye los términos concretos; si no harás una oferta, responde con type 'out' y explica por qué.";
            SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput> result =
                new SharkTankApiClient.OperationResult<SharkTankApiClient.RoundOutput>();
            yield return apiClient.SendMessage(apiSessionId, finalDecisionPrompt, result);
            if (!result.Succeeded)
            {
                FailApiFlow("Could not request final decisions: " + result.Error);
                yield break;
            }

            if (!TryBuildFeedback(result.Value, true, out JudgeFeedbackData[] feedback, out string error))
            {
                FailApiFlow("The final-decision response was unusable: " + error);
                yield break;
            }

            if (!judgeFeedbackSequence.Play(feedback, false, true))
            {
                FailApiFlow("Final judge decisions could not be presented.");
                yield break;
            }

            responseCoroutine = null;
        }

        private void StartSummaryRequest()
        {
            if (currentStage == TestStage.Summary || currentStage == TestStage.Complete)
            {
                return;
            }

            Debug.Log("All judges are in or out. Requesting the final session summary.", this);
            currentStage = TestStage.Summary;
            responseCoroutine = StartCoroutine(RequestSummaryAndShow());
        }

        private IEnumerator RequestSummaryAndShow()
        {
            SharkTankApiClient.OperationResult<SharkTankApiClient.SummaryOutput> result =
                new SharkTankApiClient.OperationResult<SharkTankApiClient.SummaryOutput>();
            yield return apiClient.GetSummary(apiSessionId, result);
            string summaryText;
            if (!result.Succeeded)
            {
                summaryText = "No se pudo obtener la evaluación final del coach. " + result.Error;
                Debug.LogError(summaryText, this);
            }
            else
            {
                if (!IsValidSummary(result.Value))
                {
                    summaryText = "La evaluación final del coach llegó incompleta y no se puede presentar.";
                    Debug.LogError(summaryText, this);
                }
                else
                {
                    summaryText = FormatSummary(result.Value);
                }
            }

            if (sessionSummaryUI == null)
            {
                Debug.LogError("SimulatorController is missing its final-evaluation dialogue UI.", this);
            }
            else
            {
                yield return sessionSummaryUI.Show();
                summaryTextFinished = false;
                summaryContinuePressed = false;
                sessionSummaryUI.ShowText("Evaluación final", summaryText);
                yield return new WaitUntil(() => summaryTextFinished);
                sessionSummaryUI.ShowContinue();
                yield return new WaitUntil(() => summaryContinuePressed);
                yield return sessionSummaryUI.Hide();
            }

            SharkTankApiClient.OperationResult<bool> deleteResult =
                new SharkTankApiClient.OperationResult<bool>();
            yield return apiClient.DeleteSession(apiSessionId, deleteResult);
            if (!deleteResult.Succeeded)
            {
                Debug.LogWarning("Could not delete the completed judge API session: " + deleteResult.Error, this);
            }

            apiSessionId = null;
            currentStage = TestStage.Complete;
            responseCoroutine = null;
            Debug.Log("La sesión de agentes terminó.", this);
        }

        private void HandleSummaryTextFinished()
        {
            summaryTextFinished = true;
        }

        private void HandleSummaryContinuePressed()
        {
            summaryContinuePressed = true;
        }

        private void ShowQuestionRetry()
        {
            currentStage = TestStage.AnswerInput;
            bgmManager?.PlayPlayerTrack();
            userInputScreen.Show(
                "No se pudo enviar la respuesta. Inténtalo de nuevo: " + currentQuestionJudgeName,
                "Escribe tu respuesta aquí...",
                currentQuestionText,
                currentQuestionHint);
        }

        private void ShowPitchRetry()
        {
            if (!string.IsNullOrEmpty(apiSessionId))
            {
                StartCoroutine(DiscardApiSession(apiSessionId));
                apiSessionId = null;
            }

            currentStage = TestStage.PitchInput;
            bgmManager?.PlayPlayerTrack();
            userInputScreen.Show(
                "No se pudo procesar el pitch. Inténtalo de nuevo.",
                "Escribe tu pitch aquí...",
                questionHint: UserInputScreen.PitchHelpHint);
        }

        private IEnumerator DiscardApiSession(string sessionId)
        {
            SharkTankApiClient.OperationResult<bool> result =
                new SharkTankApiClient.OperationResult<bool>();
            yield return apiClient.DeleteSession(sessionId, result);
            if (!result.Succeeded)
            {
                Debug.LogWarning("Could not discard the incomplete judge API session: " + result.Error, this);
            }
        }

        private void FailApiFlow(string error)
        {
            Debug.LogError(error, this);
            if (!string.IsNullOrEmpty(apiSessionId))
            {
                StartCoroutine(DiscardApiSession(apiSessionId));
                apiSessionId = null;
            }

            currentStage = TestStage.Idle;
            responseCoroutine = null;
        }

        private static bool IsValidSummary(SharkTankApiClient.SummaryOutput summary)
        {
            return summary != null &&
                   IsValidCriterion(summary.coherence) &&
                   IsValidCriterion(summary.formality) &&
                   IsValidCriterion(summary.approach) &&
                   IsValidCriterion(summary.pitch_quality) &&
                   !string.IsNullOrWhiteSpace(summary.overall) &&
                   HasItems(summary.strengths) &&
                   HasItems(summary.improvements);
        }

        private static bool IsValidCriterion(SharkTankApiClient.Criterion criterion)
        {
            return criterion != null &&
                   criterion.score >= 1 &&
                   criterion.score <= 10 &&
                   !string.IsNullOrWhiteSpace(criterion.comment);
        }

        private static bool HasItems(string[] items)
        {
            if (items == null || items.Length == 0)
            {
                return false;
            }

            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatSummary(SharkTankApiClient.SummaryOutput summary)
        {
            StringBuilder text = new StringBuilder();
            AppendCriterion(text, "Coherencia", summary.coherence);
            AppendCriterion(text, "Formalidad", summary.formality);
            AppendCriterion(text, "Manejo de los tiburones", summary.approach);
            AppendCriterion(text, "Calidad del pitch", summary.pitch_quality);
            text.AppendLine();
            text.AppendLine(summary.overall);
            AppendList(text, "Fortalezas", summary.strengths);
            AppendList(text, "Áreas de mejora", summary.improvements);
            return text.ToString();
        }

        private static void AppendCriterion(
            StringBuilder text,
            string name,
            SharkTankApiClient.Criterion criterion)
        {
            if (criterion != null)
            {
                text.AppendLine(name + ": " + criterion.score + "/10 - " + criterion.comment);
            }
        }

        private static void AppendList(StringBuilder text, string heading, string[] items)
        {
            if (items == null || items.Length == 0)
            {
                return;
            }

            text.AppendLine();
            text.AppendLine(heading + ":");
            foreach (string item in items)
            {
                text.AppendLine("- " + item);
            }
        }

        private bool ValidateReferences()
        {
            bool isValid = true;
            if (userInputScreen == null)
            {
                Debug.LogWarning("SimulatorController is missing its UserInputScreen reference.", this);
                isValid = false;
            }

            if (judgeFeedbackSequence == null)
            {
                Debug.LogWarning("SimulatorController is missing its JudgeFeedbackSequence reference.", this);
                isValid = false;
            }

            if (bgmManager == null)
            {
                Debug.LogWarning("SimulatorController is missing its BGMManager reference.", this);
                isValid = false;
            }

            if (sfxManager == null)
            {
                Debug.LogWarning("SimulatorController is missing its SFXManager reference.", this);
                isValid = false;
            }

            if (sessionSummaryUI == null)
            {
                Debug.LogWarning("SimulatorController is missing its final-evaluation dialogue UI.", this);
                isValid = false;
            }

            if (topDownCameraPose == null)
            {
                Debug.LogWarning("SimulatorController is missing its top-down camera pose.", this);
                isValid = false;
            }

            if (playerCameraPose == null)
            {
                Debug.LogWarning("SimulatorController is missing its player camera pose.", this);
                isValid = false;
            }

            if (playerPlatform == null || !hasCapturedPlatformFinalPosition)
            {
                Debug.LogWarning("SimulatorController is missing its player platform reference.", this);
                isValid = false;
            }

            if (agentProfiles == null || agentProfiles.Length != 3)
            {
                Debug.LogWarning("SimulatorController must have exactly three API agent profiles configured.", this);
                isValid = false;
            }
            else
            {
                HashSet<string> configuredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (SharkTankApiClient.SharkSetup profile in agentProfiles)
                {
                    if (profile == null ||
                        string.IsNullOrWhiteSpace(profile.name) ||
                        profile.modifier1 < 1 ||
                        profile.modifier1 > 5 ||
                        profile.modifier2 < 1 ||
                        profile.modifier2 > 5 ||
                        !configuredNames.Add(profile.name))
                    {
                        Debug.LogWarning("SimulatorController has an invalid or duplicate API agent profile.", this);
                        isValid = false;
                        break;
                    }
                }
            }

            return isValid;
        }

        internal sealed class SharkTankApiClient
        {
            [Serializable]
            public sealed class SharkSetup
            {
                public string name;
                [Range(1, 5)]
                public int modifier1 = 3;
                [Range(1, 5)]
                public int modifier2 = 3;
            }

            [Serializable]
            private sealed class SessionInput
            {
                public SharkSetup[] sharks;
            }

            [Serializable]
            private sealed class UserMessage
            {
                public string recipient = "all";
                public string content;
            }

            [Serializable]
            private sealed class MessagesInput
            {
                public UserMessage[] messages;
            }

            [Serializable]
            public sealed class SessionOutput
            {
                public string session_id = string.Empty;
            }

            [Serializable]
            public sealed class RoundOutput
            {
                public SharkResponse[] responses = new SharkResponse[0];
            }

            [Serializable]
            public sealed class SharkResponse
            {
                public string shark = string.Empty;
                public string type = string.Empty;
                public string content = string.Empty;
                public int attitude = 0;
                public string hint = string.Empty;
            }

            [Serializable]
            public sealed class SummaryOutput
            {
                public Criterion coherence = new Criterion();
                public Criterion formality = new Criterion();
                public Criterion approach = new Criterion();
                public Criterion pitch_quality = new Criterion();
                public string overall = string.Empty;
                public string[] strengths = new string[0];
                public string[] improvements = new string[0];
            }

            [Serializable]
            public sealed class Criterion
            {
                public int score = 0;
                public string comment = string.Empty;
            }

            public sealed class OperationResult<T>
            {
                public T Value { get; private set; }
                public string Error { get; private set; }
                public bool Succeeded { get; private set; }

                public void SetSuccess(T value)
                {
                    Value = value;
                    Error = null;
                    Succeeded = true;
                }

                public void SetFailure(string error)
                {
                    Value = default(T);
                    Error = error;
                    Succeeded = false;
                }
            }

            private readonly string baseUrl;
            private readonly int timeoutSeconds;
            private readonly bool logTraffic;

            public SharkTankApiClient(string baseUrl, int timeoutSeconds, bool logTraffic)
            {
                this.baseUrl = baseUrl == null ? string.Empty : baseUrl.TrimEnd('/');
                this.timeoutSeconds = Mathf.Max(1, timeoutSeconds);
                this.logTraffic = logTraffic;
            }

            public IEnumerator CreateSession(
                SharkSetup[] sharks,
                OperationResult<SessionOutput> result)
            {
                if (sharks == null || sharks.Length == 0)
                {
                    result.SetFailure("At least one agent profile is required.");
                    yield break;
                }

                SessionInput payload = new SessionInput { sharks = sharks };
                yield return SendJsonRequest(
                    "POST",
                    "session",
                    payload,
                    result);
                if (result.Succeeded && string.IsNullOrWhiteSpace(result.Value.session_id))
                {
                    result.SetFailure("The API did not return a session_id.");
                }
            }

            public IEnumerator SendMessage(
                string sessionId,
                string content,
                OperationResult<RoundOutput> result)
            {
                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(content))
                {
                    result.SetFailure("A session ID and non-empty message content are required.");
                    yield break;
                }

                MessagesInput payload = new MessagesInput
                {
                    messages = new[]
                    {
                                new UserMessage
                                {
                                    recipient = "all",
                                    content = content
                                }
                            }
                };
                yield return SendJsonRequest(
                    "POST",
                    "session/" + Uri.EscapeDataString(sessionId),
                    payload,
                    result);
            }

            public IEnumerator GetSummary(
                string sessionId,
                OperationResult<SummaryOutput> result)
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    result.SetFailure("A session ID is required to request the summary.");
                    yield break;
                }

                yield return SendJsonRequest(
                    "POST",
                    "session/" + Uri.EscapeDataString(sessionId) + "/summary",
                    null,
                    result);
            }

            public IEnumerator DeleteSession(
                string sessionId,
                OperationResult<bool> result)
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    result.SetFailure("A session ID is required to delete the session.");
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    result.SetFailure("The API base URL is empty.");
                    yield break;
                }

                string url = BuildUrl("session/" + Uri.EscapeDataString(sessionId));
                LogRequest("DELETE", url, null);
                using (UnityWebRequest request = UnityWebRequest.Delete(url))
                {
                    request.timeout = timeoutSeconds;
                    request.downloadHandler = new DownloadHandlerBuffer();
                    yield return request.SendWebRequest();
                    LogResponse("DELETE", url, request);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        result.SetFailure(FormatRequestError(request));
                        yield break;
                    }
                }

                result.SetSuccess(true);
            }

            private IEnumerator SendJsonRequest<T>(
                string method,
                string path,
                object payload,
                OperationResult<T> result)
                where T : class
            {
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    result.SetFailure("The API base URL is empty.");
                    yield break;
                }

                string url = BuildUrl(path);
                string requestBody = payload == null ? null : JsonUtility.ToJson(payload);
                LogRequest(method, url, requestBody);
                using (UnityWebRequest request = new UnityWebRequest(url, method))
                {
                    request.timeout = timeoutSeconds;
                    request.downloadHandler = new DownloadHandlerBuffer();
                    if (requestBody != null)
                    {
                        byte[] body = Encoding.UTF8.GetBytes(requestBody);
                        request.uploadHandler = new UploadHandlerRaw(body);
                        request.SetRequestHeader("Content-Type", "application/json");
                    }

                    yield return request.SendWebRequest();
                    LogResponse(method, url, request);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        result.SetFailure(FormatRequestError(request));
                        yield break;
                    }

                    string responseBody = request.downloadHandler.text;
                    if (string.IsNullOrWhiteSpace(responseBody))
                    {
                        result.SetFailure("The API returned an empty response body.");
                        yield break;
                    }

                    T response;
                    try
                    {
                        response = JsonUtility.FromJson<T>(responseBody);
                    }
                    catch (ArgumentException exception)
                    {
                        result.SetFailure("The API returned invalid JSON: " + exception.Message);
                        yield break;
                    }

                    if (response == null)
                    {
                        result.SetFailure("The API returned a response that could not be parsed.");
                        yield break;
                    }

                    result.SetSuccess(response);
                }
            }

            private void LogRequest(string method, string url, string body)
            {
                if (!logTraffic)
                {
                    return;
                }

                Debug.Log(
                    "[SharkTank API] Request " + method + " " + url +
                    (body == null ? "\nBody: <none>" : "\nBody: " + body));
            }

            private void LogResponse(string method, string url, UnityWebRequest request)
            {
                if (!logTraffic)
                {
                    return;
                }

                string responseBody = request.downloadHandler == null
                    ? string.Empty
                    : request.downloadHandler.text;
                Debug.Log(
                    "[SharkTank API] Response " + method + " " + url +
                    " (HTTP " + request.responseCode + ", " + request.result + ")" +
                    (string.IsNullOrWhiteSpace(responseBody) ? "\nBody: <empty>" : "\nBody: " + responseBody));
            }

            private string BuildUrl(string path)
            {
                return baseUrl + "/" + path.TrimStart('/');
            }

            private static string FormatRequestError(UnityWebRequest request)
            {
                string responseBody = request.downloadHandler == null
                    ? string.Empty
                    : request.downloadHandler.text;
                return "HTTP " + request.responseCode + ": " + request.error +
                       (string.IsNullOrWhiteSpace(responseBody) ? string.Empty : " Response: " + responseBody);
            }
        }

        private static JudgeFeedbackData[] CreateTestFeedback(bool isPitchRound, int decisionRoundIndex = 0)
        {
            if (isPitchRound)
            {
                return new[]
                {
                    new JudgeFeedbackData(
                        "Juez de Marketing",
                        2,
                        "Tu propuesta tiene una idea clara y un mercado atractivo.",
                        "Explica con más detalle cómo conseguirás tus primeros clientes.",
                        question: "¿Cuáles son tus objetivos principales con este proyecto?",
                        hint: "Explica qué problema quieres resolver, para quién y qué objetivos concretos te permitirán medir el avance."),
                    new JudgeFeedbackData(
                        "Juez de Finanzas",
                        -3,
                        "La presentación fue fácil de seguir.",
                        "Los costos y las proyecciones todavía no justifican la inversión.",
                        question: "¿Cómo planeas financiar el crecimiento de tu empresa?",
                        hint: "Comenta tus fuentes de capital, cuánto necesitas, en qué lo invertirás y cómo planeas sostener el crecimiento."),
                    new JudgeFeedbackData(
                        "Juez de Operaciones",
                        0,
                        "El producto resuelve una necesidad interesante.",
                        string.Empty)
                };
            }

            if (decisionRoundIndex > 0)
            {
                return new[]
                {
                    new JudgeFeedbackData(
                        "Juez de Marketing",
                        1,
                        "Tu respuesta aclara tus prioridades para el negocio.",
                        string.Empty,
                        offerText: "Te ofrezco invertir $150,000 por el 10% de tu empresa."),
                    new JudgeFeedbackData(
                        "Juez de Finanzas",
                        1,
                        "Tu respuesta reduce parte del riesgo financiero.",
                        string.Empty,
                        offerText: "Te ofrezco invertir $200,000 por el 12% de tu empresa."),
                    new JudgeFeedbackData(
                        "Juez de Operaciones",
                        1,
                        "Tu plan de ejecución ahora es más claro.",
                        string.Empty,
                        offerText: "Te ofrezco invertir $100,000 por el 8% de tu empresa.")
                };
            }

            return new[]
            {
                new JudgeFeedbackData(
                    "Juez de Marketing",
                    1,
                    "Tu respuesta identifica un canal claro para llegar a los clientes.",
                    "Explica cómo medirás si ese canal está funcionando.",
                    question: "¿Qué resultado considerarías una señal de que debes cambiar de estrategia?",
                    hint: "Menciona métricas verificables y el umbral que te haría mantener o cambiar de estrategia."),
                new JudgeFeedbackData(
                    "Juez de Finanzas",
                    -3,
                    null,
                    null,
                    isOut: true,
                    outText: "Estoy fuera; no haré una oferta en este proyecto.",
                    outReason: "El riesgo financiero supera mi tolerancia, aunque la idea tenga potencial."),
                new JudgeFeedbackData(
                    "Juez de Operaciones",
                    2,
                    "Tu respuesta ayuda a aclarar los riesgos de ejecución.",
                    string.Empty,
                    offerText: "Te ofrezco invertir $250,000 por el 15% de tu empresa.")
            };
        }
    }
}
