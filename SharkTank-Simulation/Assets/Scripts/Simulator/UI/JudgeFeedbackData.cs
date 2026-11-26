using System;

namespace SharkTank.UI
{
    /// <summary>
    /// One judge's attitude, feedback, question, offer, and exit decision.
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
        /// Gets or sets the question the player must answer before the next judge speaks.
        /// </summary>
        public string question;

        /// <summary>
        /// Gets or sets the hint generated for the question.
        /// </summary>
        public string hint;

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
        /// Gets or sets the offer the judge makes, if any.
        /// </summary>
        public string offerText;

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
        /// <param name="question">A question for the player, if any.</param>
        /// <param name="offerText">The judge's offer, if any.</param>
        /// <param name="hint">A hint for answering the question, if any.</param>
        public JudgeFeedbackData(
            string judgeName,
            int attitude,
            string likedText,
            string criticismText,
            bool isOut = false,
            string outText = null,
            string outReason = null,
            string question = null,
            string offerText = null,
            string hint = null)
        {
            this.judgeName = judgeName;
            this.attitude = attitude;
            this.likedText = likedText;
            this.criticismText = criticismText;
            this.question = question;
            this.isOut = isOut;
            this.outText = outText;
            this.outReason = outReason;
            this.offerText = offerText;
            this.hint = hint;
        }
    }
}
