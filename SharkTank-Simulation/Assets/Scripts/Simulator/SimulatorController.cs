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
            Debug.Log("Simulator test started. Enter a pitch to begin.", this);
            ShowCurrentPrompt();
        }

        private void HandleResponseSubmitted(string response)
        {
            bool isPitchRound = currentStage == TestStage.PitchInput;
            if (!isPitchRound && currentStage != TestStage.AnswerInput)
            {
                return;
            }

            if (judgeFeedbackSequence.IsPlaying)
            {
                Debug.LogWarning("The response was not processed because judge feedback is already playing.", this);
                return;
            }

            currentStage = isPitchRound ? TestStage.PitchFeedback : TestStage.AnswerFeedback;
            userInputScreen.SetInteractable(false);
            responseCoroutine = StartCoroutine(HideInputAndPlayFeedback(response, isPitchRound));
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
                    Debug.Log("La retroalimentación del pitch terminó. Escribe tu respuesta para continuar.", this);
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
                    userInputScreen.Show("Responde a las dudas de los jueces.", "Escribe tu respuesta aquí...");
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
