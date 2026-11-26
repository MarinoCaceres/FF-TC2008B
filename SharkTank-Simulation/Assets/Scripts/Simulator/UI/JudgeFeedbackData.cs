using System;

namespace SharkTank.UI
{
    /// <summary>
    /// One judge's attitude, feedback text, and exit decision.
    /// </summary>
    [Serializable]
    public sealed class JudgeFeedbackData
    {
        /// <summary>
        /// Gets or sets the judge's display name.
        /// </summary>
        public string judgeName;

        /// <summary>
        /// Gets or sets the judge's attitude from -3 to +3.
        /// </summary>
        public int attitude;

        /// <summary>
        /// Gets or sets what the judge liked about the response.
        /// </summary>
        public string likedText;

        /// <summary>
        /// Gets or sets what the judge criticized about the response.
        /// </summary>
        public string criticismText;

        /// <summary>
        /// Gets or sets whether the judge is out and will not make an offer.
        /// </summary>
        public bool isOut;

        /// <summary>
        /// Gets or sets the judge's statement that they are out.
        /// </summary>
        public string outText;

        /// <summary>
        /// Gets or sets the judge's explanation for being out.
        /// </summary>
        public string outReason;

        /// <summary>
        /// Creates a judge feedback record.
        /// </summary>
        /// <param name="judgeName">The judge's display name.</param>
        /// <param name="attitude">The judge's attitude from -3 to +3.</param>
        /// <param name="likedText">What the judge liked.</param>
        /// <param name="criticismText">What the judge criticized.</param>
        /// <param name="isOut">Whether the judge exits the pitch session.</param>
        /// <param name="outText">What the judge says when declaring they are out.</param>
        /// <param name="outReason">Why the judge is out.</param>
        public JudgeFeedbackData(
            string judgeName,
            int attitude,
            string likedText,
            string criticismText,
            bool isOut = false,
            string outText = null,
            string outReason = null)
        {
            this.judgeName = judgeName;
            this.attitude = attitude;
            this.likedText = likedText;
            this.criticismText = criticismText;
            this.isOut = isOut;
            this.outText = outText;
            this.outReason = outReason;
        }
    }
}
