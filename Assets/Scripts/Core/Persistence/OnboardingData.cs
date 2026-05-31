namespace WordPuzzle.Persistence
{
    [System.Serializable]
    public class OnboardingData
    {
        public bool completed = false;
        public bool skipped = false;
        public int version = 1;

        public OnboardingData() { }
    }
}
