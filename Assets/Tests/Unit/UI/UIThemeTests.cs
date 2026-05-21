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
    public void GetModeColor_Classic_ReturnsTeal()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(0f, 0.737f, 0.831f, 1f); // #00bcd4 in RGB

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
        var expectedColor = new Color(0.914f, 0.118f, 0.388f, 1f); // #e91e63 in RGB

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
        var expectedColor = new Color(1f, 0.420f, 0.420f, 1f); // #ff6b6b in RGB

        // Act
        var actualColor = theme.GetModeColor(ModeType.TimeAttack);

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.01f);
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.01f);
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.01f);
        Assert.AreEqual(expectedColor.a, actualColor.a, 0.01f);
    }
}
