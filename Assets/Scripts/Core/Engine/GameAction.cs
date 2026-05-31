using WordPuzzle.Puzzle;
using WordPuzzleModel = WordPuzzle.Puzzle.WordPuzzle;

namespace WordPuzzle.State
{
    public abstract class GameAction { }

    public class PressLetterAction : GameAction
    {
        public char letter;

        public PressLetterAction(char letter)
        {
            this.letter = letter;
        }
    }

    public class DeleteLetterAction : GameAction { }

    public class SubmitWordAction : GameAction
    {
        public string word;

        public SubmitWordAction(string word)
        {
            this.word = word;
        }
    }

    public class UseHintAction : GameAction
    {
        public int letterIndex;

        public UseHintAction(int letterIndex)
        {
            this.letterIndex = letterIndex;
        }
    }

    public class UseRevealAction : GameAction { }

    /// <summary>
    /// TimeAttack mode AddTime power-up: grants the player a configured number of bonus
    /// seconds while a TimeAttack run is active. Handled by GameStateManager.HandleUseAddTime,
    /// which decrements addTimesRemaining and raises OnTimeAdded with the granted amount.
    /// </summary>
    public class UseAddTimeAction : GameAction { }

    public class UndoStepAction : GameAction { }

    public class ResetGameAction : GameAction
    {
        public WordPuzzleModel puzzle;

        public ResetGameAction(WordPuzzleModel puzzle)
        {
            this.puzzle = puzzle;
        }
    }

    public class WinGameAction : GameAction { }

    public class LoseGameAction : GameAction { }
}
