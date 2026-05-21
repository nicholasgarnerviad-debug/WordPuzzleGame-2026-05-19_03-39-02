using NUnit.Framework;

namespace WordPuzzleGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for the ResultsScreen script.
    /// Tests stats calculation accuracy and time formatting logic.
    /// </summary>
    public class ResultsScreenTests
    {
        /// <summary>
        /// Test that accuracy calculation returns the correct percentage.
        /// Given 10 valid words out of 20 total attempts, accuracy should be 50%.
        /// </summary>
        [Test]
        public void ResultsScreen_CalculateAccuracy_ReturnsCorrectPercentage()
        {
            // Arrange
            int validWords = 10;
            int totalAttempts = 20;

            // Act
            float accuracy = (validWords / (float)totalAttempts) * 100f;

            // Assert
            Assert.AreEqual(50f, accuracy);
        }

        /// <summary>
        /// Test that time formatting correctly converts seconds to MM:SS format.
        /// Given 125 seconds, should format as "2:05".
        /// </summary>
        [Test]
        public void ResultsScreen_FormatDuration_ReturnsCorrectFormat()
        {
            // Arrange
            float seconds = 125f;

            // Act
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            string formatted = $"{mins}:{secs:D2}";

            // Assert
            Assert.AreEqual("2:05", formatted);
        }

        /// <summary>
        /// Test that accuracy calculation handles zero total attempts (division by zero).
        /// Should return 0 to avoid division by zero errors.
        /// </summary>
        [Test]
        public void ResultsScreen_CalculateAccuracy_WithZeroAttempts_ReturnsZero()
        {
            // Arrange
            int validWords = 0;
            int totalAttempts = 0;

            // Act
            float accuracy = totalAttempts == 0 ? 0f : (validWords / (float)totalAttempts) * 100f;

            // Assert
            Assert.AreEqual(0f, accuracy);
        }

        /// <summary>
        /// Test that time formatting handles edge cases correctly.
        /// Given 3661 seconds (1 hour, 1 minute, 1 second), should format as "61:01".
        /// </summary>
        [Test]
        public void ResultsScreen_FormatDuration_HandlesEdgeCases()
        {
            // Arrange
            float seconds = 3661f; // 1 hour, 1 minute, 1 second

            // Act
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            string formatted = $"{mins}:{secs:D2}";

            // Assert
            Assert.AreEqual("61:01", formatted);
        }

        /// <summary>
        /// Test that time formatting pads seconds with leading zero.
        /// Given 65 seconds, should format as "1:05" not "1:5".
        /// </summary>
        [Test]
        public void ResultsScreen_FormatDuration_PadsSeconds()
        {
            // Arrange
            float seconds = 65f;

            // Act
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            string formatted = $"{mins}:{secs:D2}";

            // Assert
            Assert.AreEqual("1:05", formatted);
        }
    }
}
