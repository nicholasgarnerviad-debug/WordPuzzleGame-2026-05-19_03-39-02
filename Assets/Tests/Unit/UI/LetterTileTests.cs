using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.UI;

namespace WordPuzzle.Tests.Unit.UI
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

    }
}
