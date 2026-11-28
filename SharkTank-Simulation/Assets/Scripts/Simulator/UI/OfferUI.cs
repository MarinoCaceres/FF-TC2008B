using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.UI
{
    /// <summary>
    /// Shows a single judge offer and reports whether the player accepts or declines it.
    /// </summary>
    public sealed class OfferUI : MonoBehaviour
    {
        [Header("Offer UI")]
        [Tooltip("Canvas group used to fade the offer UI.")]
        [SerializeField] private CanvasGroup rootGroup;
        [Tooltip("Label showing the name of the judge making the offer.")]
        [SerializeField] private TMP_Text judgeNameLabel;
        [Tooltip("Text body containing the offer terms.")]
        [SerializeField] private TMP_Text offerText;
        [Tooltip("Button that accepts the shown offer.")]
        [SerializeField] private Button acceptButton;
        [Tooltip("Button that declines the shown offer.")]
        [SerializeField] private Button declineButton;

        [Header("Animation")]
        [Tooltip("Duration of the offer UI fade in seconds.")]
        [SerializeField] private float fadeDuration = 0.25f;

        /// <summary>
        /// Occurs when the player decides on the shown offer. The argument is true when accepted.
        /// </summary>
        public event Action<bool> OnDecision;

        private void Awake()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(HandleAcceptPressed);
                acceptButton.interactable = false;
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(HandleDeclinePressed);
                declineButton.interactable = false;
            }
        }

        private void OnDestroy()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(HandleAcceptPressed);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveListener(HandleDeclinePressed);
            }
        }

        /// <summary>
        /// Fills in the offer, fades in the UI, and enables its buttons.
        /// </summary>
        /// <param name="judgeName">The name of the judge making the offer.</param>
        /// <param name="offer">The offer terms.</param>
        public IEnumerator ShowOffer(string judgeName, string offer)
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("OfferUI cannot show because its CanvasGroup reference is missing.", this);
                yield break;
            }

            if (judgeNameLabel != null)
            {
                judgeNameLabel.text = string.IsNullOrWhiteSpace(judgeName) ? "Juez" : judgeName;
            }

            if (offerText != null)
            {
                offerText.text = offer ?? string.Empty;
            }

            rootGroup.gameObject.SetActive(true);
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
            SetButtonsInteractable(true);

            yield return FadeRootTo(1f);
        }

        /// <summary>
        /// Fades out the offer UI and disables its raycasts.
        /// </summary>
        public IEnumerator Hide()
        {
            if (rootGroup == null)
            {
                Debug.LogWarning("OfferUI cannot hide because its CanvasGroup reference is missing.", this);
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

        private void SetButtonsInteractable(bool value)
        {
            if (acceptButton != null)
            {
                acceptButton.interactable = value;
            }

            if (declineButton != null)
            {
                declineButton.interactable = value;
            }
        }

        private void HandleAcceptPressed()
        {
            Decide(true);
        }

        private void HandleDeclinePressed()
        {
            Decide(false);
        }

        private void Decide(bool accepted)
        {
            if (acceptButton == null || !acceptButton.interactable)
            {
                return;
            }

            SetButtonsInteractable(false);
            OnDecision?.Invoke(accepted);
        }
    }
}