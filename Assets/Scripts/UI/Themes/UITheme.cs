using UnityEngine;

/// <summary>
/// Centralized color theme system for Word Puzzle Game.
/// Stores all UI aesthetic colors and mode-specific colors.
/// </summary>
public class UITheme : ScriptableObject
{
    [System.Serializable]
    public struct ModeColorPalette
    {
        public ModeType modeType;
        public Color primaryColor;
        public Color accentColor;
    }

    [Header("Backgrounds")]
    [SerializeField] private Color darkBackground = new Color(0.1f, 0.1f, 0.18f); // #1a1a2e

    [Header("Game Mode Colors")]
    [SerializeField] private ModeColorPalette[] modeColors = new ModeColorPalette[3];

    [Header("Text Colors")]
    [SerializeField] private Color lightText = Color.white; // #ffffff
    [SerializeField] private Color subtleText = new Color(0.8f, 0.8f, 0.8f); // #cccccc

    [Header("Accent & Feedback")]
    [SerializeField] private Color accentGold = new Color(1f, 0.84f, 0f); // #ffd700
    [SerializeField] private Color errorRed = new Color(1f, 0.32f, 0.32f); // #ff5252

    /// <summary>
    /// Gets the primary color for a specific game mode.
    /// </summary>
    public Color GetModeColor(ModeType modeType)
    {
        foreach (var palette in modeColors)
        {
            if (palette.modeType == modeType)
            {
                return palette.primaryColor;
            }
        }

        // Fallback if mode not found
        Logger.LogWarning($"Color not found for mode {modeType}, returning default");
        return accentGold;
    }

    /// <summary>
    /// Gets the accent color for a specific game mode.
    /// </summary>
    public Color GetModeAccentColor(ModeType modeType)
    {
        foreach (var palette in modeColors)
        {
            if (palette.modeType == modeType)
            {
                return palette.accentColor;
            }
        }

        // Fallback if mode not found
        return accentGold;
    }

    public Color DarkBackground => darkBackground;
    public Color LightText => lightText;
    public Color SubtleText => subtleText;
    public Color AccentGold => accentGold;
    public Color ErrorRed => errorRed;
}

/// <summary>
/// Static accessor for the default UI theme.
/// Loads the theme from Resources/Themes/DefaultTheme.asset on first access.
/// </summary>
public static class UIThemeManager
{
    private static UITheme instance;

    public static UITheme Current
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UITheme>("Themes/DefaultTheme");

                if (instance == null)
                {
                    Logger.LogError("Failed to load default UI theme from Resources/Themes/DefaultTheme.asset");
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Manually set a theme instance. Useful for testing or runtime theme switching.
    /// </summary>
    public static void SetTheme(UITheme theme)
    {
        instance = theme;
    }
}
