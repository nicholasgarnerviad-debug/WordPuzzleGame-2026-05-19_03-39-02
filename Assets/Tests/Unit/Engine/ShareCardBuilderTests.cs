using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.Game;

[TestFixture]
public class ShareCardBuilderTests
{
    private const string CHANGED = "\U0001F7E9";   // 🟩
    private const string UNCHANGED = "⬛";

    private static ShareCardBuilder.ShareInput BaseInput(params string[] chain)
    {
        return new ShareCardBuilder.ShareInput
        {
            mode = ShareCardBuilder.ModeKind.Classic,
            startWord = chain[0],
            endWord = chain[chain.Length - 1],
            chain = new List<string>(chain),
        };
    }

    [Test]
    public void Build_ReturnsGridWithChangedPositionMarker_PerRow()
    {
        // cat -> bat differs at index 0; bat -> bag differs at index 2.
        var input = BaseInput("cat", "bat", "bag");
        string text = ShareCardBuilder.Build(input);

        string expectedRow1 = CHANGED + UNCHANGED + UNCHANGED;
        string expectedRow2 = UNCHANGED + UNCHANGED + CHANGED;

        StringAssert.Contains(expectedRow1, text);
        StringAssert.Contains(expectedRow2, text);
    }

    [Test]
    public void Build_HeaderAndSubtitle_FormatCorrectly()
    {
        var input = BaseInput("cat", "bat", "bag");
        string text = ShareCardBuilder.Build(input);

        StringAssert.StartsWith("Word Ladder — Classic", text);
        StringAssert.Contains("CAT → BAG", text);
        StringAssert.Contains("2 steps", text);
    }

    [Test]
    public void Build_ClassicMode_OmitsTimeSegment()
    {
        var input = BaseInput("cat", "bat", "bag");
        input.totalTimeSeconds = 42f;   // even with a number, classic is untimed
        string text = ShareCardBuilder.Build(input);

        StringAssert.DoesNotContain("0:42", text);
    }

    [Test]
    public void Build_TimeAttack_IncludesTimeAndModeLabel()
    {
        var input = BaseInput("cat", "bat", "bag");
        input.mode = ShareCardBuilder.ModeKind.TimeAttack;
        input.timeAttackBaseSeconds = 60;
        input.timeAttackSurvival = true;
        input.totalTimeSeconds = 42f;
        string text = ShareCardBuilder.Build(input);

        StringAssert.Contains("Time Attack 60s Survival", text);
        StringAssert.Contains("0:42", text);
    }

    [Test]
    public void Build_DailyMode_IncludesIndexAndStreakFooter()
    {
        var input = BaseInput("cat", "bat", "bag");
        input.mode = ShareCardBuilder.ModeKind.Daily;
        input.dailyIndex = 122;          // 0-based, renders as Daily #123
        input.streakCurrent = 5;
        input.streakBest = 12;

        string text = ShareCardBuilder.Build(input);

        StringAssert.Contains("Daily #123", text);
        StringAssert.Contains("Streak 5 · Best 12", text);
    }

    [Test]
    public void Build_PuzzleShow_IncludesTierLabel()
    {
        var input = BaseInput("cat", "bat", "bag");
        input.mode = ShareCardBuilder.ModeKind.PuzzleShow;
        input.puzzleShowTier = 4;
        string text = ShareCardBuilder.Build(input);

        StringAssert.Contains("Puzzle Show T4", text);
    }

    [Test]
    public void Build_EmptyChain_StillReturnsHeader()
    {
        var input = new ShareCardBuilder.ShareInput
        {
            mode = ShareCardBuilder.ModeKind.Classic,
            startWord = "cat",
            endWord = "bag",
            chain = new List<string> { "cat" },
        };
        string text = ShareCardBuilder.Build(input);

        StringAssert.StartsWith("Word Ladder — Classic", text);
        StringAssert.Contains("0 steps", text);
    }

    [Test]
    public void Build_SingleStep_UsesSingularSteps()
    {
        var input = BaseInput("cat", "bat");
        string text = ShareCardBuilder.Build(input);
        StringAssert.Contains("1 step\n", text + "\n");
    }
}
