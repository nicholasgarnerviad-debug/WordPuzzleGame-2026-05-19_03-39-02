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

public class UndoStepAction : GameAction { }

public class ResetGameAction : GameAction
{
    public WordPuzzle puzzle;

    public ResetGameAction(WordPuzzle puzzle)
    {
        this.puzzle = puzzle;
    }
}

public class WinGameAction : GameAction { }

public class LoseGameAction : GameAction { }
