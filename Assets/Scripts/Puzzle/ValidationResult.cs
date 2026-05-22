public class ValidationResult
{
    public bool isValid;              // Word exists in dictionary
    public string message;
    public bool isNextStep;           // Exactly one letter different
    public bool isProgress;           // Moves closer to solution
    public int distanceToStart;
    public int distanceToEnd;

    public ValidationResult() { }

    public ValidationResult(bool valid, string msg, bool nextStep,
                           bool progress, int distStart, int distEnd)
    {
        isValid = valid;
        message = msg;
        isNextStep = nextStep;
        isProgress = progress;
        distanceToStart = distStart;
        distanceToEnd = distEnd;
    }
}
