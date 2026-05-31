namespace WordPuzzle.UI
{
    /// <summary>
    /// No-op IHaptics — used in Editor / headless test environments (Task 7B).
    /// </summary>
    public sealed class NullHaptics : IHaptics
    {
        public void LightTap()  { }
        public void MediumTap() { }
        public void Buzz()      { }
    }
}
