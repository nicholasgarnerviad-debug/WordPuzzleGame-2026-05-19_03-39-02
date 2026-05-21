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
    /// Event invoked when the tile is clicked.
    /// </summary>
    public event TileClickedCallback OnTileClicked;

    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image tileImage;
    [SerializeField] private Button tileButton;

    private char currentLetter = ' ';
    private Color originalColor = Color.white;

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
    /// </summary>
    /// <param name="color">The color to apply</param>
    public void SetColor(Color color)
    {
        if (tileImage != null)
        {
            tileImage.color = color;
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
        }
    }

    /// <summary>
    /// Handles click event: plays tap animation and invokes the OnTileClicked callback.
    /// Called by the button's onClick event.
    /// </summary>
    private void OnClick()
    {
        // Play tap animation
        StartCoroutine(UIAnimations.ScaleTileTap((RectTransform)transform));

        // Invoke the callback
        OnTileClicked?.Invoke();
    }
}
}