using NUnit.Framework;
using UnityEngine;
using WordPuzzle.UI;
using WordPuzzle.Persistence;

// Task 9E (UI half) — AccessiblePalette adapts gameplay tile hues to colorblind /
// high-contrast settings, and exposes a clamped text-scale multiplier.
// NOTE: AccessiblePalette holds STATIC state; [TearDown] restores the Off defaults.
[TestFixture]
public class AccessiblePaletteTests
{
    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }

    private static void AssertColor(Color expected, Color actual, string what)
    {
        Assert.AreEqual(expected.r, actual.r, 1e-3f, $"{what} R");
        Assert.AreEqual(expected.g, actual.g, 1e-3f, $"{what} G");
        Assert.AreEqual(expected.b, actual.b, 1e-3f, $"{what} B");
    }

    [TearDown]
    public void Teardown()
    {
        // Restore the default (Off) palette so cross-test static state can't leak.
        AccessiblePalette.Apply(new SettingsData());
    }

    [Test]
    public void Apply_OffMode_UsesDefaultGreenRedPalette()
    {
        AccessiblePalette.Apply(new SettingsData { colorBlindMode = ColorBlindMode.Off, highContrast = false });

        AssertColor(Hex("#6AAA64"), AccessiblePalette.Correct, "Correct");
        AssertColor(Hex("#D9534F"), AccessiblePalette.Error, "Error");
    }

    [Test]
    public void Apply_Deuteranopia_UsesColorblindSafePalette()
    {
        AccessiblePalette.Apply(new SettingsData { colorBlindMode = ColorBlindMode.Deuteranopia });

        AssertColor(Hex("#3A7CA5"), AccessiblePalette.Correct, "Correct");
        AssertColor(Hex("#E08214"), AccessiblePalette.Error, "Error");
    }

    [Test]
    public void Apply_HighContrast_UsesColorblindSafePalette()
    {
        AccessiblePalette.Apply(new SettingsData { colorBlindMode = ColorBlindMode.Off, highContrast = true });

        AssertColor(Hex("#3A7CA5"), AccessiblePalette.Correct, "Correct");
        AssertColor(Hex("#E08214"), AccessiblePalette.Error, "Error");
    }

    [Test]
    public void Hint_IsGold_RegardlessOfMode()
    {
        AccessiblePalette.Apply(new SettingsData { colorBlindMode = ColorBlindMode.Off });
        AssertColor(Hex("#C9B458"), AccessiblePalette.Hint, "Hint(Off)");

        AccessiblePalette.Apply(new SettingsData { colorBlindMode = ColorBlindMode.Deuteranopia });
        AssertColor(Hex("#C9B458"), AccessiblePalette.Hint, "Hint(Deuteranopia)");

        AccessiblePalette.Apply(new SettingsData { highContrast = true });
        AssertColor(Hex("#C9B458"), AccessiblePalette.Hint, "Hint(HighContrast)");
    }

    [Test]
    public void TextScale_DefaultsToOne()
    {
        AccessiblePalette.Apply(new SettingsData()); // textScale field defaults to 1.0
        Assert.AreEqual(1.0f, AccessiblePalette.TextScale, 1e-3f);
    }

    [Test]
    public void TextScale_ReflectsSetting_WhenLargeText()
    {
        AccessiblePalette.Apply(new SettingsData { textScale = 1.3f });
        Assert.AreEqual(1.3f, AccessiblePalette.TextScale, 1e-3f);
    }
}
