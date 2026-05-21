using NUnit.Framework;
using UnityEngine;

public class UIThemeTests
{
    [Test]
    public void UIThemeManager_Current_LoadsDefaultTheme()
    {
        // Arrange & Act
        var theme = UIThemeManager.Current;

        // Assert
        Assert.IsNotNull(theme, "UIThemeManager should load the default theme");
    }

    [Test]
    public void DarkBackground_ReturnsExactSpec()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(0.1f, 0.1f, 0.18f, 1f); // #1a1a2e

        // Act
        var actualColor = theme.DarkBackground;

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, "DarkBackground R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, "DarkBackground G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, "DarkBackground B value must match spec");
    }

    [Test]
    public void LightText_ReturnsExactSpec()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(1f, 1f, 1f, 1f); // #ffffff

        // Act
        var actualColor = theme.LightText;

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, "LightText R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, "LightText G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, "LightText B value must match spec");
    }

    [Test]
    public void SubtleText_ReturnsExactSpec()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(0.8f, 0.8f, 0.8f, 1f); // #cccccc

        // Act
        var actualColor = theme.SubtleText;

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, "SubtleText R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, "SubtleText G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, "SubtleText B value must match spec");
    }

    [Test]
    public void AccentGold_ReturnsExactSpec()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(1f, 0.84f, 0f, 1f); // #ffd700

        // Act
        var actualColor = theme.AccentGold;

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, "AccentGold R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, "AccentGold G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, "AccentGold B value must match spec");
    }

    [Test]
    public void ErrorRed_ReturnsExactSpec()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(1f, 0.32f, 0.32f, 1f); // #ff5252

        // Act
        var actualColor = theme.ErrorRed;

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, "ErrorRed R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, "ErrorRed G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, "ErrorRed B value must match spec");
    }

    [Test]
    public void GetModeColor_Classic_ReturnsTeal()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(0f, 0.74f, 0.83f, 1f); // #00bcd4 in RGB

        // Act
        var actualColor = theme.GetModeColor(ModeType.Classic);

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.01f);
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.01f);
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.01f);
        Assert.AreEqual(expectedColor.a, actualColor.a, 0.01f);
    }

    [Test]
    public void GetModeColor_PuzzleShow_ReturnsPurple()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(0.91f, 0.12f, 0.39f, 1f); // #e91e63 in RGB

        // Act
        var actualColor = theme.GetModeColor(ModeType.PuzzleShow);

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.01f);
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.01f);
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.01f);
        Assert.AreEqual(expectedColor.a, actualColor.a, 0.01f);
    }

    [Test]
    public void GetModeColor_TimeAttack_ReturnsOrange()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(1f, 0.42f, 0.42f, 1f); // #ff6b6b in RGB

        // Act
        var actualColor = theme.GetModeColor(ModeType.TimeAttack);

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.01f);
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.01f);
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.01f);
        Assert.AreEqual(expectedColor.a, actualColor.a, 0.01f);
    }
}
