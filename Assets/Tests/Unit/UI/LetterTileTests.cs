using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzleGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for the LetterTile component.
    /// Tests tile interaction, letter display, color management, and event callbacks.
    /// </summary>
    public class LetterTileTests
    {
        private GameObject tileGameObject;
        private LetterTile letterTile;
        private TextMeshProUGUI letterText;
        private Image tileImage;
        private Button tileButton;
        private Canvas canvas;

        [SetUp]
        public void SetUp()
        {
            // Create a Canvas to enable RectTransform
            GameObject canvasObject = new GameObject("TestCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create the tile GameObject
            tileGameObject = new GameObject("LetterTile");
            tileGameObject.transform.SetParent(canvasObject.transform, false);

            // Add Image component for the tile background
            tileImage = tileGameObject.AddComponent<Image>();
            tileImage.color = Color.white;

            // Add Button component for click detection
            tileButton = tileGameObject.AddComponent<Button>();

            // Create a child object for the letter text
            GameObject textChild = new GameObject("Text");
            textChild.transform.SetParent(tileGameObject.transform, false);
            letterText = textChild.AddComponent<TextMeshProUGUI>();
            letterText.text = "";

            // Add the LetterTile component
            letterTile = tileGameObject.AddComponent<LetterTile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(tileGameObject);
            Object.DestroyImmediate(canvas.gameObject);
        }

        [Test]
        public void OnTileClicked_InvokesCallback()
        {
            // Arrange
            bool callbackInvoked = false;
            letterTile.OnTileClicked += () => callbackInvoked = true;

            // Act
            letterTile.OnClick();

            // Assert
            Assert.IsTrue(callbackInvoked, "OnTileClicked callback should be invoked");
        }

        [Test]
        public void SetLetter_GetLetter_StoresAndReturnsLetter()
        {
            // Arrange
            char testLetter = 'Z';

            // Act
            letterTile.SetLetter(testLetter);
            char result = letterTile.GetLetter();

            // Assert
            Assert.AreEqual(testLetter, result, "GetLetter should return the letter set by SetLetter");
        }

        [Test]
        public void SetLetter_DisplaysUppercaseLetter()
        {
            // Arrange
            char testLetter = 'a';

            // Act
            letterTile.SetLetter(testLetter);

            // Assert
            Assert.AreEqual("A", letterText.text, "Letter text should display uppercase letter");
        }

        [Test]
        public void SetColor_ChangesImageColor()
        {
            // Arrange
            Color testColor = Color.red;

            // Act
            letterTile.SetColor(testColor);

            // Assert
            Assert.AreEqual(testColor, tileImage.color, "Tile image color should be set");
        }

        [Test]
        public void ResetColor_ResetsToOriginalColor()
        {
            // Arrange
            Color originalColor = tileImage.color;
            Color newColor = Color.red;
            letterTile.SetColor(newColor);

            // Act
            letterTile.ResetColor();

            // Assert
            Assert.AreEqual(originalColor, tileImage.color, "Color should be reset to original");
        }

        [Test]
        public void OnClick_CallbackReceivesLetter()
        {
            // Arrange
            char capturedLetter = '\0';
            letterTile.OnTileClicked += () =>
            {
                capturedLetter = letterTile.GetLetter();
            };
            letterTile.SetLetter('X');

            // Act
            letterTile.OnClick();

            // Assert
            Assert.AreEqual('X', capturedLetter, "Callback should have access to the current letter");
        }

        [Test]
        public void Awake_FindsMissingComponents()
        {
            // Arrange - Create a fresh tile without setting components
            GameObject freshTile = new GameObject("FreshLetterTile");
            Canvas freshCanvas = new GameObject("FreshCanvas").AddComponent<Canvas>();
            freshTile.transform.SetParent(freshCanvas.transform, false);

            // Add Image but not Button (should auto-find)
            Image freshImage = freshTile.AddComponent<Image>();

            // Add Button on the tile
            Button freshButton = freshTile.AddComponent<Button>();

            // Create child with TextMeshProUGUI
            GameObject freshTextChild = new GameObject("Text");
            freshTextChild.transform.SetParent(freshTile.transform, false);
            TextMeshProUGUI freshText = freshTextChild.AddComponent<TextMeshProUGUI>();

            // Act - Add component which triggers Awake
            LetterTile freshLetterTile = freshTile.AddComponent<LetterTile>();

            // Assert - Components should be found
            Assert.IsNotNull(freshLetterTile, "LetterTile should be created");

            // Clean up
            Object.DestroyImmediate(freshTile);
            Object.DestroyImmediate(freshCanvas.gameObject);
        }
    }
}
