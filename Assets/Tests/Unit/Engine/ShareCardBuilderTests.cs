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

        StringAssert.StartsWith("Star Ladder — Classic", text);
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

        StringAssert.StartsWith("Star Ladder — Classic", text);
        StringAssert.Contains("0 steps", text);
    }

    [Test]
    public void Build_SingleStep_UsesSingularSteps()
    {
        var input = BaseInput("cat", "bat");
        string text = ShareCardBuilder.Build(input);
        StringAssert.Contains("1 step\n", text + "\n");
    }

    // ── Daily 2.0 (Task 36 Phase 4) — path-shape card ──

    private static ShareCardBuilder.ShareInput DailyShapeInput(
        int[] classes, int par, int steps, int stars, bool failed)
    {
        return new ShareCardBuilder.ShareInput
        {
            mode = ShareCardBuilder.ModeKind.Daily,
            startWord = "cat",
            endWord = "dog",
            chain = new List<string> { "cat", "cot", "dot", "dog" },
            dailyIndex = 41,           // renders as Daily #42
            dailyStepClasses = new List<int>(classes),
            par = par, playerSteps = steps, stars = stars, dailyFailed = failed,
            streakCurrent = 7, streakBest = 9,
        };
    }

    [Test]
    public void BuildDaily_RowModel_MapsClassesToGlyphsInOrder()
    {
        var input = DailyShapeInput(new[] { 0, 1, 2, 0 }, par: 3, steps: 3, stars: 2, failed: false);
        string text = ShareCardBuilder.Build(input);

        string expected = ShareCardBuilder.CHANGED_GLYPH + "\n"
                        + ShareCardBuilder.DETOUR_GLYPH + "\n"
                        + ShareCardBuilder.UNCHANGED_GLYPH + "\n"
                        + ShareCardBuilder.CHANGED_GLYPH;
        StringAssert.Contains(expected, text);
    }

    [Test]
    public void BuildDaily_Header_HasParScoreStarsAndStreak()
    {
        var input = DailyShapeInput(new[] { 0, 0, 0 }, par: 3, steps: 3, stars: 3, failed: false);
        string text = ShareCardBuilder.Build(input);

        StringAssert.StartsWith("Star Ladder Daily #42", text);
        StringAssert.Contains("Par 3", text);
        StringAssert.Contains("3/3", text);
        StringAssert.Contains("Streak 7 · Best 9", text);
    }

    [Test]
    public void BuildDaily_Failed_ShowsXScore()
    {
        var input = DailyShapeInput(new[] { 0, 2, 2, 2 }, par: 4, steps: 1, stars: 0, failed: true);
        string text = ShareCardBuilder.Build(input);
        StringAssert.Contains("X/4", text);
    }

    [Test]
    public void BuildDaily_IsShapeOnly_NeverLeaksWords()
    {
        var input = DailyShapeInput(new[] { 0, 1, 0 }, par: 3, steps: 3, stars: 2, failed: false);
        string text = ShareCardBuilder.Build(input).ToUpperInvariant();
        StringAssert.DoesNotContain("CAT", text);
        StringAssert.DoesNotContain("DOG", text);
        StringAssert.DoesNotContain("COT", text);
        StringAssert.DoesNotContain("→", text);
    }

    [Test]
    public void BuildDaily_DifferentPaths_ProduceDifferentShapes()
    {
        string a = ShareCardBuilder.Build(DailyShapeInput(new[] { 0, 0, 0 }, 3, 3, 3, false));
        string b = ShareCardBuilder.Build(DailyShapeInput(new[] { 0, 1, 1, 0 }, 3, 4, 2, false));
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void Build_NormalizesToLfNewlines()
    {
        // The CRLF fix: AppendLine emitted \r\n; the card normalizes to \n for clean clipboard paste.
        string text = ShareCardBuilder.Build(BaseInput("cat", "bat", "bag"));
        StringAssert.DoesNotContain("\r\n", text);
    }
}
