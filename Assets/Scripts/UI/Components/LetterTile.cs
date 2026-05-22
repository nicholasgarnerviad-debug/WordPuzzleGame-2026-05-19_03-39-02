using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using WordPuzzleGame.UI.Animations;

namespace WordPuzzleGame.UI
{
    /// <summary>
    /// Reusable UI button component representing an individual letter tile in the gameplay grid.
    /// Handles letter display, color management, click animations, and tile interaction callbacks.
    /// </summary>
    public class LetterTile : MonoBehaviour
{
    /// <summary>
    /// Delegate for tile click callbacks. No parameters - caller can access tile state via GetLetter().
    /// </summary>
    public delegate void TileClickedCallback();

    /// <summary>
    /// Delegate for letter press callbacks with the letter as a parameter.
    /// </summary>
    public delegate void LetterPressedCallback(char letter);

    /// <summary>
    /// Event invoked when the tile is clicked.
    /// </summary>
    public event TileClickedCallback OnTileClicked;

    /// <summary>
    /// Event invoked when a letter is pressed, passing the letter character.
    /// </summary>
    public event LetterPressedCallback OnLetterPressed;

    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image tileImage;
    [SerializeField] private Button tileButton;

    private char currentLetter = ' ';
    private Color originalColor = Color.white;
    private Color lastColor = Color.white;

    private void Awake()
    {
        // Auto-find letterText from children if not assigned
        if (letterText == null)
        {
            letterText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Auto-find tileImage from self if not assigned
        if (tileImage == null)
        {
            tileImage = GetComponent<Image>();
        }

        // Auto-find tileButton from self if not assigned
        if (tileButton == null)
        {
            tileButton = GetComponent<Button>();
        }

        // Null checks for all components
        if (letterText == null)
        {
            Debug.LogWarning($"LetterTile on {gameObject.name}: letterText component not found");
        }
        if (tileImage == null)
        {
            Debug.LogWarning($"LetterTile on {gameObject.name}: tileImage component not found");
        }
        if (tileButton == null)
        {
            Debug.LogWarning($"LetterTile on {gameObject.name}: tileButton component not found");
        }

        // Store original color from tileImage
        if (tileImage != null)
        {
            originalColor = tileImage.color;
        }

        // Hook tileButton.onClick to OnClick()
        if (tileButton != null)
        {
            tileButton.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (tileButton != null)
        {
            tileButton.onClick.RemoveListener(OnClick);
        }
    }

    /// <summary>
    /// Initializes the tile with a letter. Convenience method for SetLetter.
    /// </summary>
    /// <param name="letter">The letter character to display</param>
    public void Initialize(char letter)
    {
        SetLetter(letter);
    }

    /// <summary>
    /// Sets the letter to display on this tile (stored as uppercase).
    /// </summary>
    /// <param name="letter">The letter character to display</param>
    public void SetLetter(char letter)
    {
        currentLetter = letter;

        if (letterText != null)
        {
            letterText.text = letter.ToString().ToUpper();
        }
    }

    /// <summary>
    /// Gets the current letter displayed on this tile.
    /// </summary>
    /// <returns>The letter character</returns>
    public char GetLetter()
    {
        return currentLetter;
    }

    /// <summary>
    /// Changes the tile's background color.
    /// Cached to avoid redundant property assignments if the color hasn't changed.
    /// </summary>
    /// <param name="color">The color to apply</param>
    public void SetColor(Color color)
    {
        if (tileImage != null && lastColor != color)
        {
            tileImage.color = color;
            lastColor = color;
        }
    }

    /// <summary>
    /// Resets the tile's color to its original color (stored during Awake).
    /// </summary>
    public void ResetColor()
    {
        if (tileImage != null)
        {
            tileImage.color = originalColor;
            lastColor = originalColor;
        }
    }

    /// <summary>
    /// Gets the current letter as a property for easy access.
    /// </summary>
    public char Letter
    {
        get { return currentLetter; }
    }

    /// <summary>
    /// Enables or disables interaction with this tile.
    /// </summary>
    /// <param name="enabled">Whether the tile should be interactable</param>
    public void SetEnabled(bool enabled)
    {
        if (tileButton != null)
        {
            tileButton.interactable = enabled;
        }
    }

    /// <summary>
    /// Highlights the tile to show it's a hint letter.
    /// </summary>
    public void HighlightHint()
    {
        SetColor(Color.yellow);
    }

    /// <summary>
    /// Handles click event: plays tap animation and invokes callbacks.
    /// Called by the button's onClick event.
    /// </summary>
    public void OnClick()
    {
        // Play tap animation
        StartCoroutine(UIAnimations.ScaleTileTap((RectTransform)transform));

        // Invoke the callbacks
        OnTileClicked?.Invoke();
        OnLetterPressed?.Invoke(currentLetter);
    }
}
}