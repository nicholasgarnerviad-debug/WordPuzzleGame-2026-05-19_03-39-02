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

    [Header("Mode Colors")]
    [SerializeField] private ModeColorPalette[] modeColors = new ModeColorPalette[3];

    [Header("UI Base Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color textColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color accentColor = new Color(0.2f, 0.8f, 0.9f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f, 1f);

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
        return accentColor;
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
        return accentColor;
    }

    public Color BackgroundColor => backgroundColor;
    public Color TextColor => textColor;
    public Color AccentColor => accentColor;
    public Color WarningColor => warningColor;
    public Color SuccessColor => successColor;
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
