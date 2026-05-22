using NUnit.Framework;
using UnityEngine;
using WordPuzzle.Modes;

public class UIThemeTests
{
    [SetUp]
    public void SetUp()
    {
        // Reset UIThemeManager.Current between tests to ensure test isolation
        UIThemeManager.SetTheme(null);
    }

    [Test]
    public void UIThemeManager_Current_LoadsDefaultTheme()
    {
        // Arrange & Act
        var theme = UIThemeManager.Current;

        // Assert
        Assert.IsNotNull(theme, "UIThemeManager should load the default theme");
    }

    [Test]
    public void UIThemeManager_SetTheme_UpdatesCurrentTheme()
    {
        // Arrange
        var customTheme = ScriptableObject.CreateInstance<UITheme>();

        // Act
        UIThemeManager.SetTheme(customTheme);
        var result = UIThemeManager.Current;

        // Assert
        Assert.AreEqual(customTheme, result, "SetTheme should update UIThemeManager.Current");
    }

    [Test]
    public void UIThemeManager_SetThemeToNull_AllowsReload()
    {
        // Arrange
        UIThemeManager.SetTheme(null);

        // Act
        var theme = UIThemeManager.Current;

        // Assert
        Assert.IsNotNull(theme, "UIThemeManager should load default theme after null reset");
    }

    [TestCase("DarkBackground", 0.1f, 0.1f, 0.18f)] // #1a1a2e
    [TestCase("LightText", 1f, 1f, 1f)] // #ffffff
    [TestCase("SubtleText", 0.8f, 0.8f, 0.8f)] // #cccccc
    [TestCase("AccentGold", 1f, 0.84f, 0f)] // #ffd700
    [TestCase("ErrorRed", 1f, 0.32f, 0.32f)] // #ff5252
    public void ColorProperties_ReturnsExactSpec(string colorPropertyName, float expectedR, float expectedG, float expectedB)
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(expectedR, expectedG, expectedB, 1f);

        // Act
        Color actualColor = colorPropertyName switch
        {
            "DarkBackground" => theme.DarkBackground,
            "LightText" => theme.LightText,
            "SubtleText" => theme.SubtleText,
            "AccentGold" => theme.AccentGold,
            "ErrorRed" => theme.ErrorRed,
            _ => Color.black
        };

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.001f, $"{colorPropertyName} R value must match spec");
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.001f, $"{colorPropertyName} G value must match spec");
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.001f, $"{colorPropertyName} B value must match spec");
    }

    [TestCase(ModeType.Classic, 0f, 0.74f, 0.83f)] // #00bcd4 in RGB
    [TestCase(ModeType.PuzzleShow, 0.91f, 0.12f, 0.39f)] // #e91e63 in RGB
    [TestCase(ModeType.TimeAttack, 1f, 0.42f, 0.42f)] // #ff6b6b in RGB
    public void GetModeColor_ReturnsCorrectColorForMode(ModeType modeType, float expectedR, float expectedG, float expectedB)
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var expectedColor = new Color(expectedR, expectedG, expectedB, 1f);

        // Act
        var actualColor = theme.GetModeColor(modeType);

        // Assert
        Assert.AreEqual(expectedColor.r, actualColor.r, 0.01f, $"GetModeColor({modeType}) R value mismatch");
        Assert.AreEqual(expectedColor.g, actualColor.g, 0.01f, $"GetModeColor({modeType}) G value mismatch");
        Assert.AreEqual(expectedColor.b, actualColor.b, 0.01f, $"GetModeColor({modeType}) B value mismatch");
        Assert.AreEqual(expectedColor.a, actualColor.a, 0.01f, $"GetModeColor({modeType}) A value mismatch");
    }

    [TestCase(ModeType.Classic)]
    [TestCase(ModeType.PuzzleShow)]
    [TestCase(ModeType.TimeAttack)]
    public void GetModeAccentColor_ReturnsValidColor(ModeType modeType)
    {
        // Arrange
        var theme = UIThemeManager.Current;

        // Act
        var accentColor = theme.GetModeAccentColor(modeType);

        // Assert
        Assert.IsNotNull(accentColor, $"GetModeAccentColor({modeType}) should return a valid color");
        Assert.Greater(accentColor.a, 0, $"GetModeAccentColor({modeType}) should have non-zero alpha");
    }

    [Test]
    public void GetModeColor_InvalidMode_ReturnsFallbackColor()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var invalidMode = (ModeType)999; // Non-existent mode

        // Act
        var result = theme.GetModeColor(invalidMode);

        // Assert
        Assert.AreEqual(theme.AccentGold, result, "GetModeColor should return AccentGold as fallback for invalid mode");
    }

    [Test]
    public void GetModeAccentColor_InvalidMode_ReturnsFallbackColor()
    {
        // Arrange
        var theme = UIThemeManager.Current;
        var invalidMode = (ModeType)999; // Non-existent mode

        // Act
        var result = theme.GetModeAccentColor(invalidMode);

        // Assert
        Assert.AreEqual(theme.AccentGold, result, "GetModeAccentColor should return AccentGold as fallback for invalid mode");
    }
}
