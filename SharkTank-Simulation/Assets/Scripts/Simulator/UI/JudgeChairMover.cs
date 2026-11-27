using System;
using System.Collections;
using UnityEngine;

namespace SharkTank.UI
{
    /// <summary>
    /// Moves a judge chair vertically according to the judge's attitude.
    /// </summary>
    public sealed class JudgeChairMover : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Vertical distance moved for each attitude point; positive attitudes rise and negative attitudes lower.")]
        [SerializeField] private float unitsPerStep = 1.5f;
        [Tooltip("Duration of the chair movement in seconds.")]
        [SerializeField] private float moveDuration = 1.2f;
        [Tooltip("Ease-out curve used while moving the chair.")]
        [SerializeField] private AnimationCurve easeOutCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 0f),
            new Keyframe(1f, 1f, 0f, 0f));

        private float baseY;
        private bool hasCapturedBaseY;
        private int movementVersion;

        /// <summary>
        /// Gets the chair's world-space starting height captured during initialization.
        /// </summary>
        public float BaseHeight
        {
            get
            {
                CaptureBaseHeight();
                return baseY;
            }
        }

        /// <summary>
        /// Gets the chair's current world-space height.
        /// </summary>
        public float CurrentHeight => transform.position.y;

        /// <summary>
        /// Occurs when the current chair movement finishes.
        /// </summary>
        public event Action OnSettled;

        private void Awake()
        {
            CaptureBaseHeight();
        }

        /// <summary>
        /// Animates the chair upward for positive attitudes and downward for negative attitudes.
        /// </summary>
        /// <param name="attitude">The judge's attitude, clamped to -3 through +3.</param>
        public IEnumerator MoveToAttitude(int attitude)
        {
            CaptureBaseHeight();
            int currentVersion = ++movementVersion;
            int clampedAttitude = Mathf.Clamp(attitude, -3, 3);
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = new Vector3(
                startPosition.x,
                baseY + clampedAttitude * unitsPerStep,
                startPosition.z);

            if (moveDuration <= 0f)
            {
                transform.position = targetPosition;
                OnSettled?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                if (currentVersion != movementVersion)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / moveDuration);
                float easedProgress = easeOutCurve != null && easeOutCurve.length > 0
                    ? Mathf.Clamp01(easeOutCurve.Evaluate(progress))
                    : 1f - Mathf.Pow(1f - progress, 2f);
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedProgress);
                yield return null;
            }

            if (currentVersion == movementVersion)
            {
                transform.position = targetPosition;
                OnSettled?.Invoke();
            }
        }

        /// <summary>
        /// Returns the chair to the height captured when this component initialized.
        /// </summary>
        public void ResetHeight()
        {
            CaptureBaseHeight();
            movementVersion++;
            Vector3 position = transform.position;
            position.y = baseY;
            transform.position = position;
        }

        private void CaptureBaseHeight()
        {
            if (hasCapturedBaseY)
            {
                return;
            }

            baseY = transform.position.y;
            hasCapturedBaseY = true;
        }
    }
}
