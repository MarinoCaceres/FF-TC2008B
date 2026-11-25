using UnityEngine;

namespace SharkTank.UI
{
    /// <summary>
    /// Shared warm colors for the user input screen.
    /// </summary>
    public static class UITheme
    {
        /// <summary>
        /// Gets the translucent warm panel background color.
        /// </summary>
        public static readonly Color PanelBackground = new Color(43f / 255f, 28f / 255f, 22f / 255f, 0.92f);

        /// <summary>
        /// Gets the accent and border color.
        /// </summary>
        public static readonly Color Accent = new Color32(0xF2, 0x8C, 0x28, 0xFF);

        /// <summary>
        /// Gets the cream text color.
        /// </summary>
        public static readonly Color TextCream = new Color32(0xFF, 0xF1, 0xDC, 0xFF);

        /// <summary>
        /// Gets the button hover color.
        /// </summary>
        public static readonly Color ButtonHover = new Color32(0xFF, 0xA6, 0x4D, 0xFF);

        /// <summary>
        /// Gets the button pressed color.
        /// </summary>
        public static readonly Color ButtonPressed = new Color32(0xB8, 0x66, 0x1A, 0xFF);

        /// <summary>
        /// Gets the cream color dimmed to 40 percent alpha.
        /// </summary>
        public static readonly Color Disabled = new Color(1f, 241f / 255f, 220f / 255f, 0.4f);
    }
}
