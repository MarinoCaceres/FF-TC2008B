using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.UI
{
    /// <summary>
    /// Displays judge speech, typewriter progress, and feedback controls.
    /// </summary>
    public sealed class JudgeSpeechUI : MonoBehaviour
    {
        [Header("Speech UI")]
        [Tooltip("Canvas group used to fade the speech UI.")]
        [SerializeField] private CanvasGroup rootGroup;
        [Tooltip("Clickable speech panel used to finish typing instantly.")]
        [SerializeField] private Button speechPanelButton;
        [Tooltip("Label showing the current judge's name.")]
        [SerializeField] private TMP_Text nameLabel;
        [Tooltip("Text body containing the current speech.")]
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("Typewriter attached to the speech body.")]
        [SerializeField] private SpeechTypewriter typewriter;
        [Tooltip("Arrow button that advances after the current text finishes.")]
        [SerializeField] private Button arrowButton;
        [Tooltip("Button shown after all judge feedback.")]
        [SerializeField] private Button continueButton;
        [Tooltip("Canvas group used to fade in the continue button.")]
        [SerializeField] private CanvasGroup continueGroup;

        [Header("Animation")]
        [Tooltip("Duration of the speech UI fade in seconds.")]
        [SerializeField] private float fadeDuration = 0.25f;

        /// <summary>
        /// Occurs when the current speech text has finished appearing.
        /// </summary>
        public event Action OnTextFinished;

        /// <summary>
        /// Occurs when the player advances from a fully displayed speech box.
        /// </summary>
        public event Action OnArrowPressed;

        /// <summary>
        /// Occurs when the player presses the continue button.
        /// </summary>
        public event Action OnContinuePressed;

        private Coroutine continueFadeCoroutine;

        private void Awake()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            if (arrowButton != null)
            {
                arrowButton.gameObject.SetActive(false);
                arrowButton.onClick.AddListener(HandleArrowPressed);
            }

            if (speechPanelButton != null)
            {
                speechPanelButton.onClick.AddListener(HandleSpeechPanelPressed);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinuePressed);
                continueButton.interactable = false;
            }

            if (typewriter != null)
            {
                typewriter.OnFinished += HandleTextFinished;
            }
            else
            {
                Debug.LogWarning("JudgeSpeechUI is missing its SpeechTypewriter reference.", this);
            }
        }

        private void OnDestroy()
        {
            if (arrowButton != null)
            {
                arrowButton.onClick.RemoveListener(HandleArrowPressed);
            }

            if (speechPanelButton != null)
            {
                speechPanelButton.onClick.RemoveListener(HandleSpeechPanelPressed);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinuePressed);
            }

            if (typewriter != null)
            {
                typewriter.OnFinished -= HandleTextFinished;
            }
        }

        /// <summary>
        /// Fades in the speech UI and enables its raycasts.
        /// </summary>
        public IEnumerator Show()
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("JudgeSpeechUI cannot show because its CanvasGroup reference is missing.", this);
                yield break;
            }

            rootGroup.gameObject.SetActive(true);
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            if (continueFadeCoroutine != null)
            {
                StopCoroutine(continueFadeCoroutine);
                continueFadeCoroutine = null;
            }

            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            if (continueGroup != null)
            {
                continueGroup.alpha = 0f;
                continueGroup.interactable = false;
                continueGroup.blocksRaycasts = false;
            }

            yield return FadeRootTo(1f);
        }

        /// <summary>
        /// Fades out the speech UI and disables its raycasts.
        /// </summary>
        public IEnumerator Hide()
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("JudgeSpeechUI cannot hide because its CanvasGroup reference is missing.", this);
                yield break;
            }

            yield return FadeRootTo(0f);
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Shows the judge name and begins typing the supplied speech.
        /// </summary>
        /// <param name="judgeName">The judge's display name.</param>
        /// <param name="text">The speech displayed in the panel.</param>
        public void ShowText(string judgeName, string text)
        {
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrWhiteSpace(judgeName) ? "Juez" : judgeName;
            }
            else
            {
                Debug.LogWarning("JudgeSpeechUI is missing its judge-name label.", this);
            }

            if (arrowButton != null)
            {
                arrowButton.gameObject.SetActive(false);
                arrowButton.interactable = true;
            }

            if (bodyText == null)
            {
                Debug.LogWarning("JudgeSpeechUI is missing its speech body text.", this);
            }

            if (typewriter != null)
            {
                typewriter.Type(text ?? string.Empty);
                return;
            }

            if (bodyText != null)
            {
                bodyText.text = text ?? string.Empty;
            }

            HandleTextFinished();
        }

        /// <summary>
        /// Fades in the continue button after the final speech box.
        /// </summary>
        public void ShowContinue()
        {
            if (arrowButton != null)
            {
                arrowButton.gameObject.SetActive(false);
            }

            if (continueButton == null)
            {
                Debug.LogWarning("JudgeSpeechUI is missing its continue button.", this);
                return;
            }

            continueButton.gameObject.SetActive(true);
            continueButton.interactable = false;

            if (continueGroup == null)
            {
                Debug.LogWarning("JudgeSpeechUI is missing the continue button CanvasGroup.", this);
                continueButton.interactable = true;
                return;
            }

            if (continueFadeCoroutine != null)
            {
                StopCoroutine(continueFadeCoroutine);
            }

            continueFadeCoroutine = StartCoroutine(FadeContinueButton());
        }

        private IEnumerator FadeRootTo(float targetAlpha)
        {
            float startAlpha = rootGroup.alpha;
            float duration = Mathf.Max(0f, fadeDuration);
            float elapsed = 0f;

            if (duration <= 0f || Mathf.Approximately(startAlpha, targetAlpha))
            {
                rootGroup.alpha = targetAlpha;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                rootGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            rootGroup.alpha = targetAlpha;
        }

        private IEnumerator FadeContinueButton()
        {
            continueGroup.alpha = 0f;
            continueGroup.interactable = false;
            continueGroup.blocksRaycasts = false;
            float duration = Mathf.Max(0f, fadeDuration);
            float elapsed = 0f;

            if (duration > 0f)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    continueGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
                    yield return null;
                }
            }

            continueGroup.alpha = 1f;
            continueGroup.interactable = true;
            continueGroup.blocksRaycasts = true;
            continueButton.interactable = true;
            continueFadeCoroutine = null;
        }

        private void HandleTextFinished()
        {
            if (arrowButton != null)
            {
                arrowButton.gameObject.SetActive(true);
            }

            OnTextFinished?.Invoke();
        }

        private void HandleSpeechPanelPressed()
        {
            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.Skip();
            }
        }

        private void HandleArrowPressed()
        {
            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.Skip();
                return;
            }

            if (arrowButton == null || !arrowButton.gameObject.activeSelf || !arrowButton.interactable)
            {
                return;
            }

            arrowButton.gameObject.SetActive(false);
            OnArrowPressed?.Invoke();
        }

        private void HandleContinuePressed()
        {
            if (continueButton == null || !continueButton.interactable)
            {
                return;
            }

            continueButton.interactable = false;
            OnContinuePressed?.Invoke();
        }
    }
}
