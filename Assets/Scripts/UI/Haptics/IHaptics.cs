namespace WordPuzzle.UI
{
    /// <summary>
    /// Abstraction for device haptic feedback (Task 7B).
    /// Implement with HandheldHaptics on-device; inject NullHaptics in tests/editor.
    /// </summary>
    public interface IHaptics
    {
        /// <summary>Short, light tap — letter placed.</summary>
        void LightTap();

        /// <summary>Medium tap — word accepted.</summary>
        void MediumTap();

        /// <summary>Full-device buzz — rejection or win.</summary>
        void Buzz();
    }
}
