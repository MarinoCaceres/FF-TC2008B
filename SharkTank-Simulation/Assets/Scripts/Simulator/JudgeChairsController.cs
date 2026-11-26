using System;
using System.Collections.Generic;
using UnityEngine;

namespace SharkTank.UI
{
    /// <summary>
    /// Coordinates attitude-driven movement for the three judge chairs.
    /// </summary>
    public sealed class JudgeChairsController : MonoBehaviour
    {
        [Header("Chairs, left to right")]
        [Tooltip("Mover for the left judge's chair.")]
        [SerializeField] private JudgeChairMover leftChairMover;
        [Tooltip("Mover for the middle judge's chair.")]
        [SerializeField] private JudgeChairMover middleChairMover;
        [Tooltip("Mover for the right judge's chair.")]
        [SerializeField] private JudgeChairMover rightChairMover;

        private readonly JudgeChairMover[] chairMovers = new JudgeChairMover[3];
        private int remainingChairs;
        private bool isWaitingForSettled;

        /// <summary>
        /// Occurs after all available chairs have settled.
        /// </summary>
        public event Action OnChairsSettled;

        private void Awake()
        {
            RefreshChairCache();
        }

        /// <summary>
        /// Gets the vertical adjustment between the highest current chair and the highest starting chair.
        /// </summary>
        /// <param name="verticalOffset">The world-space vertical adjustment, or zero when no chair is available.</param>
        /// <returns>True when at least one active chair mover is available.</returns>
        public bool TryGetHighestChairVerticalOffset(out float verticalOffset)
        {
            RefreshChairCache();
            float highestCurrentHeight = float.NegativeInfinity;
            float highestBaseHeight = float.NegativeInfinity;
            bool foundChair = false;

            for (int i = 0; i < chairMovers.Length; i++)
            {
                JudgeChairMover mover = chairMovers[i];
                if (mover == null || !mover.isActiveAndEnabled)
                {
                    continue;
                }

                highestCurrentHeight = Mathf.Max(highestCurrentHeight, mover.CurrentHeight);
                highestBaseHeight = Mathf.Max(highestBaseHeight, mover.BaseHeight);
                foundChair = true;
            }

            verticalOffset = foundChair ? highestCurrentHeight - highestBaseHeight : 0f;
            return foundChair;
        }

        /// <summary>
        /// Starts all three chair movements in the same frame.
        /// </summary>
        /// <param name="attitudes">Attitudes in left-to-right judge order.</param>
        /// <param name="judgesOut">Judges whose chairs must remain at their current heights.</param>
        public void ApplyAttitudes(IList<int> attitudes, IList<bool> judgesOut = null)
        {
            RefreshChairCache();
            UnsubscribeFromMovers();
            remainingChairs = 0;
            isWaitingForSettled = true;

            if (attitudes == null)
            {
                Debug.LogWarning("JudgeChairsController received no attitudes; all configured chairs will return to neutral.");
            }

            for (int i = 0; i < chairMovers.Length; i++)
            {
                JudgeChairMover mover = chairMovers[i];
                if (mover == null)
                {
                    Debug.LogWarning("JudgeChairsController is missing the chair mover at left-to-right index " + i + ".");
                    continue;
                }

                if (judgesOut != null && i < judgesOut.Count && judgesOut[i])
                {
                    continue;
                }

                if (!mover.isActiveAndEnabled)
                {
                    Debug.LogWarning("The chair mover at left-to-right index " + i + " is inactive and will not move.", mover);
                    continue;
                }

                remainingChairs++;
                mover.OnSettled += HandleChairSettled;
            }

            if (remainingChairs == 0)
            {
                CompleteChairMovement();
                return;
            }

            for (int i = 0; i < chairMovers.Length; i++)
            {
                JudgeChairMover mover = chairMovers[i];
                if (mover == null || !mover.isActiveAndEnabled ||
                    (judgesOut != null && i < judgesOut.Count && judgesOut[i]))
                {
                    continue;
                }

                int attitude = attitudes != null && i < attitudes.Count ? attitudes[i] : 0;
                StartCoroutine(mover.MoveToAttitude(attitude));
            }
        }

        /// <summary>
        /// Immediately returns every configured chair to its captured starting height.
        /// </summary>
        public void ResetAll()
        {
            RefreshChairCache();
            UnsubscribeFromMovers();
            remainingChairs = 0;
            isWaitingForSettled = false;

            for (int i = 0; i < chairMovers.Length; i++)
            {
                if (chairMovers[i] != null)
                {
                    chairMovers[i].ResetHeight();
                }
            }
        }

        private void HandleChairSettled()
        {
            if (!isWaitingForSettled)
            {
                return;
            }

            remainingChairs--;
            if (remainingChairs <= 0)
            {
                CompleteChairMovement();
            }
        }

        private void CompleteChairMovement()
        {
            isWaitingForSettled = false;
            UnsubscribeFromMovers();
            OnChairsSettled?.Invoke();
        }

        private void RefreshChairCache()
        {
            chairMovers[0] = leftChairMover;
            chairMovers[1] = middleChairMover;
            chairMovers[2] = rightChairMover;
        }

        private void UnsubscribeFromMovers()
        {
            for (int i = 0; i < chairMovers.Length; i++)
            {
                if (chairMovers[i] != null)
                {
                    chairMovers[i].OnSettled -= HandleChairSettled;
                }
            }
        }
    }
}
