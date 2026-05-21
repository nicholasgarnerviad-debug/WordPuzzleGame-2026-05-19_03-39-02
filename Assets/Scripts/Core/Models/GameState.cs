using UnityEngine;

public class GameState
{
    public string[] wordChain;           // Valid words typed so far
    public string currentInput;          // In-progress word being typed
    public int lives;
    public bool isWon;
    public bool isLost;
    public int? hintedLetterIndex;      // Index of hinted letter (null if none)
    public string[] revealedWord;       // Full word revealed (null if not used)
    public int previousChainLength;     // For undo support
    public string targetWord;            // Target word to reach

    // UI Display Fields
    public int score;                    // Current game score
    public int currentStreak;            // Current consecutive valid word count
    public int wordsRemaining;           // Words remaining (Puzzle Show mode)
    public float timeRemaining;          // Time remaining (Time Attack mode)

    public GameState()
    {
        wordChain = new string[] { };
        currentInput = "";
        lives = 3;
        isWon = false;
        isLost = false;
        hintedLetterIndex = null;
        revealedWord = null;
        previousChainLength = 0;
        score = 0;
        currentStreak = 0;
        wordsRemaining = 0;
        timeRemaining = 0f;
    }

    public GameState Clone()
    {
        return new GameState
        {
            wordChain = (string[])wordChain.Clone(),
            currentInput = currentInput,
            lives = lives,
            isWon = isWon,
            isLost = isLost,
            hintedLetterIndex = hintedLetterIndex,
            revealedWord = revealedWord != null ? (string[])revealedWord.Clone() : null,
            previousChainLength = previousChainLength,
            targetWord = targetWord,
            score = score,
            currentStreak = currentStreak,
            wordsRemaining = wordsRemaining,
            timeRemaining = timeRemaining
        };
    }
}
