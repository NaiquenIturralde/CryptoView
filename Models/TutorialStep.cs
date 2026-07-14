namespace CryptoView.Models
{
    /// <summary>
    /// Represents a single step within a contextual tutorial.
    /// Each step highlights a specific UI element and provides guidance.
    /// </summary>
    public class TutorialStep
    {
        /// <summary>
        /// Localization key for the step title (e.g. "tutorial.dashboard.step1.title").
        /// Resolved at runtime via LocalizacionService.
        /// </summary>
        public string TitleKey { get; set; } = string.Empty;

        /// <summary>
        /// Localization key for the step description.
        /// </summary>
        public string DescriptionKey { get; set; } = string.Empty;

        /// <summary>
        /// CSS selector of the element to spotlight (e.g. ".kpi-grid", "#lineChart").
        /// If null, no spotlight is shown (full-screen welcome step).
        /// </summary>
        public string? TargetSelector { get; set; }

        /// <summary>
        /// Preferred tooltip position relative to the target element.
        /// Actual position may be adjusted if it would overflow the viewport.
        /// </summary>
        public TutorialPosition Position { get; set; } = TutorialPosition.Bottom;

        /// <summary>
        /// If true, this step is skipped on mobile viewports (&lt; 768px).
        /// Useful for elements only visible on desktop (e.g. sidebar spotlight).
        /// </summary>
        public bool SkipOnMobile { get; set; }

        /// <summary>
        /// If true, scroll the target element into view before showing the tooltip.
        /// </summary>
        public bool ScrollIntoView { get; set; } = true;

        /// <summary>
        /// Route to navigate to before showing this step (e.g. "/cryptolist").
        /// If null, the tour stays on the current page.
        /// </summary>
        public string? Route { get; set; }
    }

    public enum TutorialPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Center
    }
}
