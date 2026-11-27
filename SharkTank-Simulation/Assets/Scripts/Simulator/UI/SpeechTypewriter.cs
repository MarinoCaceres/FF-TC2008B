using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SharkTank.UI
{
    /// <summary>
    /// Reveals TextMeshPro text progressively and supports instant completion.
    /// </summary>
    public sealed class SpeechTypewriter : MonoBehaviour
    {
        [Header("Typewriter")]
        [Tooltip("Text component whose characters are revealed.")]
        [SerializeField] private TMP_Text targetText;
        [Tooltip("Characters revealed per second.")]
        [Min(0f)]
        [SerializeField] private float charactersPerSecond = 40f;

        private Coroutine typingCoroutine;

        /// <summary>
        /// Occurs once the current text has been fully revealed.
        /// </summary>
        public event Action OnFinished;

        /// <summary>
        /// Occurs when the current text begins revealing.
        /// </summary>
        public event Action OnTypingStarted;

        /// <summary>
        /// Gets whether the current text is still being revealed.
        /// </summary>
        public bool IsTyping { get; private set; }

        /// <summary>
        /// Replaces the text and begins revealing it at the configured speed.
        /// </summary>
        /// <param name="text">The text to reveal.</param>
        public void Type(string text)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (targetText == null)
            {
                Debug.LogWarning("SpeechTypewriter needs a TextMeshPro text reference to reveal speech.", this);
                IsTyping = false;
                OnFinished?.Invoke();
                return;
            }

            targetText.text = text ?? string.Empty;
            targetText.ForceMeshUpdate();
            targetText.maxVisibleCharacters = 0;
            IsTyping = true;

            if (targetText.textInfo.characterCount == 0 || charactersPerSecond <= 0f)
            {
                FinishTyping();
                return;
            }

            OnTypingStarted?.Invoke();
            typingCoroutine = StartCoroutine(RevealCharacters());
        }

        /// <summary>
        /// Reveals the complete current text immediately.
        /// </summary>
        public void Skip()
        {
            if (IsTyping)
            {
                FinishTyping();
            }
        }

        private IEnumerator RevealCharacters()
        {
            int characterCount = targetText.textInfo.characterCount;
            float elapsed = 0f;

            while (targetText.maxVisibleCharacters < characterCount)
            {
                elapsed += Time.unscaledDeltaTime;
                targetText.maxVisibleCharacters = Mathf.Min(
                    characterCount,
                    Mathf.FloorToInt(elapsed * charactersPerSecond));
                yield return null;
            }

            typingCoroutine = null;
            FinishTyping();
        }

        private void FinishTyping()
        {
            if (!IsTyping)
            {
                return;
            }

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (targetText != null)
            {
                targetText.maxVisibleCharacters = int.MaxValue;
            }

            IsTyping = false;
            OnFinished?.Invoke();
        }
    }
}
