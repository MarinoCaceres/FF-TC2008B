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
        private sealed class TestJudgeQuestion
        {
            public readonly string judgeName;
            public readonly string question;

            public TestJudgeQuestion(string judgeName, string question)
            {
                this.judgeName = judgeName;
                this.question = question;
            }
        }

        private static readonly TestJudgeQuestion[] TestQuestions =
        {
            new TestJudgeQuestion("Juez de Marketing", "¿Cuáles son tus objetivos principales con este proyecto?"),
            new TestJudgeQuestion("Juez de Finanzas", "¿Cómo planeas financiar el crecimiento de tu empresa?")
        };

        private enum TestStage
        {
            Idle,
            PitchInput,
            PitchFeedback,
            AnswerInput,
            AnswerFeedback,
            Complete
        }

        [Header("Scene references")]
        [Tooltip("Existing screen used for player pitch and answer input.")]
        [SerializeField] private UserInputScreen userInputScreen;
        [Tooltip("Existing judge feedback sequence.")]
        [SerializeField] private JudgeFeedbackSequence judgeFeedbackSequence;

        private TestStage currentStage;
        private int currentQuestionIndex;
        private Coroutine responseCoroutine;

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

            currentStage = TestStage.PitchInput;
            currentQuestionIndex = 0;
            Debug.Log("Simulator test started. Enter a pitch to begin.", this);
            ShowCurrentPrompt();
        }

        private void HandleResponseSubmitted(string response)
        {
            if (currentStage == TestStage.AnswerInput)
            {
                if (judgeFeedbackSequence.IsPlaying)
                {
                    Debug.LogWarning("The response was not processed because judge feedback is already playing.", this);
                    return;
                }

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

            currentStage = TestStage.PitchFeedback;
            userInputScreen.SetInteractable(false);
            responseCoroutine = StartCoroutine(HideInputAndPlayFeedback(response, true));
        }

        private IEnumerator HideQuestionAndContinue(string response)
        {
            TestJudgeQuestion answeredQuestion = TestQuestions[currentQuestionIndex];
            yield return userInputScreen.HideAndWait(false);
            responseCoroutine = null;
            Debug.Log("Respuesta de " + answeredQuestion.judgeName + ": " + response, this);

            currentQuestionIndex++;
            if (currentQuestionIndex < TestQuestions.Length)
            {
                ShowCurrentPrompt();
                yield break;
            }

            currentStage = TestStage.AnswerFeedback;
            if (!judgeFeedbackSequence.Play(CreateTestFeedback(false), false))
            {
                currentStage = TestStage.AnswerInput;
                currentQuestionIndex = TestQuestions.Length - 1;
                ShowCurrentPrompt();
            }
        }

        private IEnumerator HideInputAndPlayFeedback(string response, bool isPitchRound)
        {
            yield return userInputScreen.HideAndWait(false);
            responseCoroutine = null;

            if (!judgeFeedbackSequence.Play(CreateTestFeedback(isPitchRound), isPitchRound))
            {
                currentStage = isPitchRound ? TestStage.PitchInput : TestStage.AnswerInput;
                ShowCurrentPrompt();
                yield break;
            }

            Debug.Log((isPitchRound ? "Pitch" : "Respuesta") + " recibida: " + response, this);
        }

        private void HandleFeedbackFinished()
        {
            switch (currentStage)
            {
                case TestStage.PitchFeedback:
                    currentStage = TestStage.AnswerInput;
                    currentQuestionIndex = 0;
                    Debug.Log("La retroalimentación del pitch terminó. Responde las preguntas de los jueces.", this);
                    ShowCurrentPrompt();
                    break;

                case TestStage.AnswerFeedback:
                    currentStage = TestStage.Complete;
                    Debug.Log("La prueba de flujo entre jugador y jueces terminó.", this);
                    break;

                default:
                    break;
            }
        }

        private void ShowCurrentPrompt()
        {
            switch (currentStage)
            {
                case TestStage.PitchInput:
                    userInputScreen.Show("Presenta tu pitch ante los jueces.", "Escribe tu pitch aquí...");
                    break;

                case TestStage.AnswerInput:
                    if (currentQuestionIndex >= 0 && currentQuestionIndex < TestQuestions.Length)
                    {
                        TestJudgeQuestion question = TestQuestions[currentQuestionIndex];
                        userInputScreen.Show(
                            "Responde la duda del juez: " + question.judgeName,
                            "Escribe tu respuesta aquí...",
                            question.question);
                    }
                    else
                    {
                        Debug.LogWarning("SimulatorController has no test question to show for the current index.", this);
                    }
                    break;

                default:
                    Debug.LogWarning("SimulatorController cannot show input for the current test stage.", this);
                    break;
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

            return isValid;
        }

        private static JudgeFeedbackData[] CreateTestFeedback(bool isPitchRound)
        {
            if (isPitchRound)
            {
                return new[]
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
                        "Los costos y las proyecciones todavía no justifican la inversión."),
                    new JudgeFeedbackData(
                        "Juez de Operaciones",
                        0,
                        "El producto resuelve una necesidad interesante.",
                        string.Empty)
                };
            }

            return new[]
            {
                new JudgeFeedbackData(
                    "Juez de Marketing",
                    1,
                    "Tu respuesta identifica un canal claro para llegar a los clientes.",
                    "Incluye cómo medirás si ese canal está funcionando."),
                new JudgeFeedbackData(
                    "Juez de Finanzas",
                    -1,
                    "La explicación de los costos fue más concreta.",
                    "Aclara cuándo esperas recuperar la inversión."),
                new JudgeFeedbackData(
                    "Juez de Operaciones",
                    2,
                    "El plan parece más fácil de poner en marcha.",
                    string.Empty)
            };
        }
    }
}
