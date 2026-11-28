using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.UI
{
    /// <summary>
    /// Shows the final stats and an END button once the session is over.
    /// </summary>
    public sealed class FinishUI : MonoBehaviour
    {
        [Header("Finish UI")]
        [Tooltip("Canvas group used to fade the finish UI.")]
        [SerializeField] private CanvasGroup rootGroup;
        [Tooltip("Text body containing the final stats.")]
        [SerializeField] private TMP_Text statsText;
        [Tooltip("Button that ends the session.")]
        [SerializeField] private Button endButton;

        [Header("Animation")]
        [Tooltip("Duration of the finish UI fade in seconds.")]
        [SerializeField] private float fadeDuration = 0.25f;

        /// <summary>
        /// Occurs when the player presses the END button.
        /// </summary>
        public event Action OnEndPressed;

        private void Awake()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            if (endButton != null)
            {
                endButton.onClick.AddListener(HandleEndPressed);
                endButton.interactable = false;
            }
        }

        private void OnDestroy()
        {
            if (endButton != null)
            {
                endButton.onClick.RemoveListener(HandleEndPressed);
            }
        }

        /// <summary>
        /// Fills in the stats, fades in the UI, and enables the END button.
        /// </summary>
        /// <param name="stats">The stats text shown in the panel.</param>
        public IEnumerator ShowStats(string stats)
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("FinishUI cannot show because its CanvasGroup reference is missing.", this);
                yield break;
            }

            if (statsText != null)
            {
                statsText.text = stats ?? string.Empty;
            }

            rootGroup.gameObject.SetActive(true);
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
            if (endButton != null)
            {
                endButton.interactable = true;
            }

            yield return FadeRootTo(1f);
        }

        /// <summary>
        /// Fades out the finish UI and disables its raycasts.
        /// </summary>
        public IEnumerator Hide()
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("FinishUI cannot hide because its CanvasGroup reference is missing.", this);
                yield break;
            }

            yield return FadeRootTo(0f);
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
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

        private void HandleEndPressed()
        {
            if (endButton == null || !endButton.interactable)
            {
                return;
            }

            endButton.interactable = false;
            OnEndPressed?.Invoke();
        }
    }
}