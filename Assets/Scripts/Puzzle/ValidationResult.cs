namespace WordPuzzle.Puzzle
{
    /// <summary>Typed rejection category set by WordValidator. Lives in Puzzle assembly
    /// (no State dependency) so GameStateManager can switch on it without string parsing.</summary>
    public enum WordRejectReason
    {
        None,
        NotInDictionary,
        NotOneLetterDifferent,
        AlreadyUsed
    }

    public class ValidationResult
    {
        private bool _isValid;
        private string _message;
        private bool _isNextStep;
        private bool _isProgress;
        private int _distanceToStart;
        private int _distanceToEnd;
        private WordRejectReason _rejectReason;

        public bool IsValid
        {
            get => _isValid;
            set => _isValid = value;
        }

        public string Message
        {
            get => _message;
            set => _message = value;
        }

        public bool IsNextStep
        {
            get => _isNextStep;
            set => _isNextStep = value;
        }

        public bool IsProgress
        {
            get => _isProgress;
            set => _isProgress = value;
        }

        public int DistanceToStart
        {
            get => _distanceToStart;
            set => _distanceToStart = value;
        }

        public int DistanceToEnd
        {
            get => _distanceToEnd;
            set => _distanceToEnd = value;
        }

        public WordRejectReason RejectReason
        {
            get => _rejectReason;
            set => _rejectReason = value;
        }

        // Legacy field names for backward compatibility
        public bool isValid
        {
            get => IsValid;
            set => IsValid = value;
        }

        public string message
        {
            get => Message;
            set => Message = value;
        }

        public bool isNextStep
        {
            get => IsNextStep;
            set => IsNextStep = value;
        }

        public bool isProgress
        {
            get => IsProgress;
            set => IsProgress = value;
        }

        public int distanceToStart
        {
            get => DistanceToStart;
            set => DistanceToStart = value;
        }

        public int distanceToEnd
        {
            get => DistanceToEnd;
            set => DistanceToEnd = value;
        }

        public WordRejectReason rejectReason
        {
            get => RejectReason;
            set => RejectReason = value;
        }

        public ValidationResult() { }

        public ValidationResult(bool valid, string msg, bool nextStep,
                               bool progress, int distStart, int distEnd,
                               WordRejectReason rejectReason = WordRejectReason.None)
        {
            IsValid = valid;
            Message = msg;
            IsNextStep = nextStep;
            IsProgress = progress;
            DistanceToStart = distStart;
            DistanceToEnd = distEnd;
            RejectReason = rejectReason;
        }
    }
}
