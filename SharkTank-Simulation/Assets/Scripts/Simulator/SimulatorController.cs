using System.Collections;
using UnityEngine;
using SharkTank.UI;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Coordinates the local test flow between player input and judge feedback.
    /// </summary>
    public sealed class SimulatorController : MonoBehaviour
    {
        private enum TestStage
        {
            Idle,
            PitchInput,
            PitchFeedback,
            DecisionRound,
            AnswerInput,
            Complete
        }

        [Header("Scene references")]
        [Tooltip("Existing screen used for player pitch and answer input.")]
        [SerializeField] private UserInputScreen userInputScreen;
        [Tooltip("Existing judge feedback sequence.")]
        [SerializeField] private JudgeFeedbackSequence judgeFeedbackSequence;
        [Tooltip("Manages the background music for player and judge turns.")]
        [SerializeField] private BGMManager bgmManager;

        private TestStage currentStage;
        private TestStage questionReturnStage;
        private Coroutine responseCoroutine;
        private string currentQuestionJudgeName;
        private int decisionRoundIndex;

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

            if (responseCoroutine != null)
            {
                StopCoroutine(responseCoroutine);
                responseCoroutine = null;
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

            bgmManager?.PlayPlayerTrack();
            currentStage = TestStage.PitchInput;
            Debug.Log("Simulator test started. Enter a pitch to begin.", this);
            userInputScreen.Show(
                "Presenta tu pitch ante los jueces.",
                "Escribe tu pitch aquí...",
                questionHint: UserInputScreen.PitchHelpHint);
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
            responseCoroutine = null;
            Debug.Log("Respuesta de " + currentQuestionJudgeName + ": " + response, this);

            userInputScreen.ClearQuestionJudgeCamera();
            currentStage = questionReturnStage;
            judgeFeedbackSequence.CompleteQuestionResponse();
        }

        private IEnumerator HideInputAndPlayFeedback(string response)
        {
            yield return userInputScreen.HideAndWait(false);
            responseCoroutine = null;

            if (!judgeFeedbackSequence.Play(CreateTestFeedback(true), true, false))
            {
                bgmManager?.PlayPlayerTrack();
                currentStage = TestStage.PitchInput;
                userInputScreen.Show(
                    "Presenta tu pitch ante los jueces.",
                    "Escribe tu pitch aquí...",
                    questionHint: UserInputScreen.PitchHelpHint);
                yield break;
            }

            Debug.Log("Pitch recibido: " + response, this);
        }

        private void HandleFeedbackFinished()
        {
            switch (currentStage)
            {
                case TestStage.PitchFeedback:
                    currentStage = TestStage.DecisionRound;
                    decisionRoundIndex = 0;
                    Debug.Log("La ronda de preguntas terminó. Los jueces tomarán sus decisiones.", this);
                    if (!judgeFeedbackSequence.Play(CreateTestFeedback(false, decisionRoundIndex), false, true))
                    {
                        currentStage = TestStage.Idle;
                        Debug.LogWarning("The first decision round could not start; the simulation was not completed.", this);
                    }
                    break;

                case TestStage.DecisionRound:
                    if (judgeFeedbackSequence.AreAllJudgesDecided)
                    {
                        currentStage = TestStage.Complete;
                        Debug.Log("Todos los jueces hicieron una oferta o están fuera. La prueba terminó.", this);
                        break;
                    }

                    decisionRoundIndex++;
                    if (!judgeFeedbackSequence.Play(CreateTestFeedback(false, decisionRoundIndex), false, true))
                    {
                        currentStage = TestStage.Idle;
                        Debug.LogWarning("The next decision round could not start; not all judges have made a decision.", this);
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
            bgmManager?.PlayPlayerTrack();
            userInputScreen.SetQuestionJudgeCamera(judgeIndex);
            userInputScreen.Show(
                "Responde la duda del juez: " + judgeName,
                "Escribe tu respuesta aquí...",
                question,
                hint);
        }

        private void HandleJudgeSpeakingStarted(int judgeIndex)
        {
            bgmManager?.PlayJudgeTrack();
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

            return isValid;
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
