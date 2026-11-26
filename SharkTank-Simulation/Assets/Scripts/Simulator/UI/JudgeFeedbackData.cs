using System;

namespace SharkTank.UI
{
    /// <summary>
    /// Feedback already split into a judge's positive and critical observations.
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
        /// Creates a judge feedback record.
        /// </summary>
        /// <param name="judgeName">The judge's display name.</param>
        /// <param name="attitude">The judge's attitude from -3 to +3.</param>
        /// <param name="likedText">What the judge liked.</param>
        /// <param name="criticismText">What the judge criticized.</param>
        public JudgeFeedbackData(string judgeName, int attitude, string likedText, string criticismText)
        {
            this.judgeName = judgeName;
            this.attitude = attitude;
            this.likedText = likedText;
            this.criticismText = criticismText;
        }
    }
}
