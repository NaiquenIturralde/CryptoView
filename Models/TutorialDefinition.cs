namespace CryptoView.Models
{
    /// <summary>
    /// Defines a complete contextual tutorial for a specific page/section.
    /// Each tutorial has a unique ID, a version number, and ordered steps.
    /// </summary>
    public class TutorialDefinition
    {
        /// <summary>
        /// Unique identifier for this tutorial (e.g. "dashboard", "cryptos", "stats").
        /// Used as part of the localStorage key.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Version number. Increment when the tutorial content changes significantly.
        /// A user who completed v1 will see v2 as a new tutorial.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Ordered list of steps the user will go through.
        /// </summary>
        public List<TutorialStep> Steps { get; set; } = new();

        /// <summary>
        /// Localization key for the page route where this tutorial applies.
        /// Used for the "View tutorial" button in Ayuda.razor.
        /// </summary>
        public string PageRoute { get; set; } = string.Empty;
    }
}
