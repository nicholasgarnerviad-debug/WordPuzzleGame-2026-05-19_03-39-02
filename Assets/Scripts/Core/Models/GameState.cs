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
            previousChainLength = previousChainLength
        };
    }
}
