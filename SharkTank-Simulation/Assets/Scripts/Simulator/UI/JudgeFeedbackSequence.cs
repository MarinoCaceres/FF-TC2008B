using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharkTank.UI
{
    /// <summary>
    /// Directs the in-scene judge chair and speech feedback sequence.
    /// </summary>
    public sealed class JudgeFeedbackSequence : MonoBehaviour
    {
        private const int JudgeCount = 3;

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

        [Header("Feedback")]
        [Tooltip("Controller for the three judge chairs.")]
        [SerializeField] private JudgeChairsController chairsController;
        [Tooltip("Speech panel shown while the camera is on a judge.")]
        [SerializeField] private JudgeSpeechUI speechUI;
        [Tooltip("Pause after the chairs settle, in seconds.")]
        [SerializeField] private float settledPause = 0.6f;
        [Tooltip("Pause on the judges' wide shot before their chairs move, in seconds.")]
        [SerializeField] private float judgesDecisionPause = 1f;
        [Tooltip("Pause on the top-down judge overview before moving to the player, in seconds.")]
        [SerializeField] private float topDownOverviewPause = 3f;

        private Coroutine sequenceCoroutine;
        private bool chairsSettled;
        private bool arrowPressed;
        private bool continuePressed;
        private Vector3 judgesTopDownOverviewBasePosition;
        private bool hasCapturedTopDownOverviewPosition;

        /// <summary>
        /// Occurs after CONTINUAR is pressed, the overview transition completes, and the speech UI has hidden.
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
        /// Gets whether a feedback sequence is currently running.
        /// </summary>
        public bool IsPlaying => sequenceCoroutine != null;

        private void Awake()
        {
            CaptureTopDownOverviewPosition();
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
        }

        /// <summary>
        /// Plays the feedback sequence for three judges in left-to-right order.
        /// </summary>
        /// <param name="feedback">Already-split feedback data in judge order.</param>
        /// <param name="isPitchRound">Whether the feedback follows the initial pitch.</param>
        /// <returns>True when the feedback sequence starts successfully.</returns>
        public bool Play(IList<JudgeFeedbackData> feedback, bool isPitchRound)
        {
            if (sequenceCoroutine != null)
            {
                Debug.LogWarning("JudgeFeedbackSequence is already playing a feedback sequence.", this);
                return false;
            }

            if (!ValidateInput(feedback))
            {
                return false;
            }

            arrowPressed = false;
            continuePressed = false;
            sequenceCoroutine = StartCoroutine(RunSequence(feedback, isPitchRound));
            return true;
        }

        [ContextMenu("Test: Feedback Round 1")]
        private void TestFeedbackRoundOne()
        {
            OnFeedbackFinished -= LogFeedbackFinished;
            OnFeedbackFinished += LogFeedbackFinished;

            Play(new List<JudgeFeedbackData>
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
            }, true);
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

        private IEnumerator RunSequence(IList<JudgeFeedbackData> feedback, bool isPitchRound)
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
            }

            chairsSettled = false;
            chairsController.OnChairsSettled += HandleChairsSettled;
            chairsController.ApplyAttitudes(attitudes);
            yield return new WaitUntil(() => chairsSettled);
            chairsController.OnChairsSettled -= HandleChairsSettled;

            if (settledPause > 0f)
            {
                yield return new WaitForSecondsRealtime(settledPause);
            }

            for (int judgeIndex = 0; judgeIndex < JudgeCount; judgeIndex++)
            {
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

                JudgeFeedbackData judgeFeedback = feedback[judgeIndex];
                string likedText = string.IsNullOrWhiteSpace(judgeFeedback.likedText)
                    ? "Todavía no me convences del todo."
                    : judgeFeedback.likedText;
                string criticismText = string.IsNullOrWhiteSpace(judgeFeedback.criticismText)
                    ? isPitchRound
                        ? "¡Presentaste muy bien tus ideas!"
                        : "¡Respondiste muy bien a mis dudas!"
                    : judgeFeedback.criticismText;

                speechUI.ShowText(judgeFeedback.judgeName, likedText);
                OnSpeechBoxShown?.Invoke(judgeIndex, 0);
                arrowPressed = false;
                yield return new WaitUntil(() => arrowPressed);

                speechUI.ShowText(judgeFeedback.judgeName, criticismText);
                OnSpeechBoxShown?.Invoke(judgeIndex, 1);
                arrowPressed = false;
                yield return new WaitUntil(() => arrowPressed);

                if (judgeIndex < JudgeCount - 1)
                {
                    yield return speechUI.Hide();
                }
            }

            speechUI.ShowContinue();
            yield return new WaitUntil(() => continuePressed);
            yield return speechUI.Hide();
            UpdateTopDownOverviewPosition();
            cameraController.CutCameraToPose(judgesTopDownOverviewPoint);
            if (topDownOverviewPause > 0f)
            {
                yield return new WaitForSecondsRealtime(topDownOverviewPause);
            }

            yield return cameraController.MoveCameraToPose(userInputCameraPose);
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
