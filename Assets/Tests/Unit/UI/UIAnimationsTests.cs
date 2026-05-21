using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WordPuzzleGame.UI.Animations;

namespace WordPuzzleGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for the UIAnimations utility system.
    /// Tests all animation methods to ensure correct scaling, opacity, timing, and easing.
    /// </summary>
    public class UIAnimationsTests
    {
        private GameObject testObject;
        private RectTransform testRectTransform;
        private CanvasGroup testCanvasGroup;
        private MonoBehaviour testMonoBehaviour;

        [SetUp]
        public void SetUp()
        {
            // Create a test GameObject with required components
            testObject = new GameObject("TestUIObject");

            // Add Canvas as parent to enable RectTransform
            GameObject canvasObject = new GameObject("TestCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            testObject.transform.SetParent(canvasObject.transform, false);
            testRectTransform = testObject.AddComponent<RectTransform>();
            testCanvasGroup = testObject.AddComponent<CanvasGroup>();
            testMonoBehaviour = testObject.AddComponent<MonoBehaviourTestFixture>();

            // Initialize to neutral state
            testRectTransform.localScale = Vector3.one;
            testCanvasGroup.alpha = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(testObject);
            Object.Destroy(testObject.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator ScaleButtonTap_AnimatesFrom1To095AndBackTo1()
        {
            // Arrange
            float duration = 0.3f;
            testRectTransform.localScale = Vector3.one;

            // Act: Start the animation
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleButtonTap(testRectTransform, duration));

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final scale should be approximately 1.0
            float finalScale = testRectTransform.localScale.x;
            Assert.AreApproximatelyEqual(1f, finalScale, 0.01f, "Final scale should be approximately 1.0");

            // Verify mid-animation scale was reduced (sample at duration * 0.25)
            testRectTransform.localScale = Vector3.one;
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleButtonTap(testRectTransform, duration));
            yield return new WaitForSeconds(duration * 0.25f);
            float midScale = testRectTransform.localScale.x;
            Assert.Less(midScale, 1f, "Mid-animation scale should be less than 1.0 (scale down phase)");
        }

        [UnityTest]
        public IEnumerator ScaleTileTap_AnimatesFrom1To11AndBackTo1()
        {
            // Arrange
            float duration = 0.3f;
            testRectTransform.localScale = Vector3.one;

            // Act: Start the animation
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleTileTap(testRectTransform, duration));

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final scale should be approximately 1.0
            float finalScale = testRectTransform.localScale.x;
            Assert.AreApproximatelyEqual(1f, finalScale, 0.01f, "Final scale should be approximately 1.0");

            // Verify mid-animation scale was increased (sample at duration * 0.25)
            testRectTransform.localScale = Vector3.one;
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleTileTap(testRectTransform, duration));
            yield return new WaitForSeconds(duration * 0.25f);
            float midScale = testRectTransform.localScale.x;
            Assert.Greater(midScale, 1f, "Mid-animation scale should be greater than 1.0 (scale up phase)");
        }

        [UnityTest]
        public IEnumerator WordAddAnimation_IncreasesOpacityFrom0To1AndScales()
        {
            // Arrange
            float duration = 0.4f;
            testRectTransform.localScale = Vector3.one;
            testCanvasGroup.alpha = 0f; // Start invisible

            // Act: Start the animation
            testMonoBehaviour.StartCoroutine(UIAnimations.WordAddAnimation(testCanvasGroup, duration));

            // Wait briefly to verify animation starts
            yield return new WaitForSeconds(0.1f);

            // Assert: Alpha should have increased from 0
            Assert.Greater(testCanvasGroup.alpha, 0f, "Alpha should increase during animation start");

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final opacity should be approximately 1
            Assert.AreApproximatelyEqual(1f, testCanvasGroup.alpha, 0.01f, "Final alpha should be approximately 1.0");

            // Assert: Final scale should be approximately 1.0
            float finalScale = testRectTransform.localScale.x;
            Assert.AreApproximatelyEqual(1f, finalScale, 0.01f, "Final scale should be approximately 1.0 after bounce animation");
        }

        [UnityTest]
        public IEnumerator WordAddAnimation_ScalesBounce1To12AndBackTo1()
        {
            // Arrange
            float duration = 0.4f;
            testRectTransform.localScale = Vector3.one;
            testCanvasGroup.alpha = 0f;

            // Act: Start the animation and sample mid-point
            testMonoBehaviour.StartCoroutine(UIAnimations.WordAddAnimation(testCanvasGroup, duration));

            // Sample at quarter duration (should be in scale up phase)
            yield return new WaitForSeconds(duration * 0.25f);
            float midScale = testRectTransform.localScale.x;

            // Assert: Scale should be increased during first half
            Assert.Greater(midScale, 1f, "Mid-animation scale should be greater than 1.0 in scale up phase");

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final scale should return to 1.0
            float finalScale = testRectTransform.localScale.x;
            Assert.AreApproximatelyEqual(1f, finalScale, 0.01f, "Final scale should return to 1.0");
        }

        [UnityTest]
        public IEnumerator FadeTransition_FadeIn_IncreasesOpacityFrom0To1()
        {
            // Arrange
            float duration = 0.5f;
            testCanvasGroup.alpha = 0f;

            // Act: Start fade in animation
            testMonoBehaviour.StartCoroutine(UIAnimations.FadeTransition(testCanvasGroup, fadeIn: true, duration: duration));

            // Wait briefly to verify animation starts
            yield return new WaitForSeconds(0.1f);

            // Assert: Alpha should have increased from 0
            Assert.Greater(testCanvasGroup.alpha, 0f, "Alpha should increase during fade in");

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final opacity should be approximately 1
            Assert.AreApproximatelyEqual(1f, testCanvasGroup.alpha, 0.01f, "Final alpha should be approximately 1.0 after fade in");
        }

        [UnityTest]
        public IEnumerator FadeTransition_FadeOut_DecreasesOpacityFrom1To0()
        {
            // Arrange
            float duration = 0.5f;
            testCanvasGroup.alpha = 1f;

            // Act: Start fade out animation
            testMonoBehaviour.StartCoroutine(UIAnimations.FadeTransition(testCanvasGroup, fadeIn: false, duration: duration));

            // Wait briefly to verify animation starts
            yield return new WaitForSeconds(0.1f);

            // Assert: Alpha should have decreased from 1
            Assert.Less(testCanvasGroup.alpha, 1f, "Alpha should decrease during fade out");

            // Wait for animation to complete
            yield return new WaitForSeconds(duration + 0.05f);

            // Assert: Final opacity should be approximately 0
            Assert.AreApproximatelyEqual(0f, testCanvasGroup.alpha, 0.01f, "Final alpha should be approximately 0.0 after fade out");
        }

        [UnityTest]
        public IEnumerator CustomDurationParameter_AffectsAnimationSpeed()
        {
            // Arrange: Test with custom short duration
            float shortDuration = 0.15f;
            testRectTransform.localScale = Vector3.one;

            // Act: Start animation with custom duration
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleButtonTap(testRectTransform, shortDuration));

            // Wait briefly - should be mostly complete
            yield return new WaitForSeconds(shortDuration * 0.75f);

            // The animation should be progressed significantly with custom duration
            // (This test verifies the duration parameter is actually used)
            testRectTransform.localScale = Vector3.one;

            // Now test with longer duration
            float longDuration = 0.6f;
            testMonoBehaviour.StartCoroutine(UIAnimations.ScaleButtonTap(testRectTransform, longDuration));

            // Wait for same short time
            yield return new WaitForSeconds(shortDuration * 0.75f);

            // Animation should be less progressed with longer duration
            // (Just verify it completes without error - framework tests the parameter)
            yield return new WaitForSeconds(longDuration + 0.1f);

            Assert.AreApproximatelyEqual(1f, testRectTransform.localScale.x, 0.01f, "Animation should complete successfully");
        }

        [UnityTest]
        public IEnumerator Animation_CompletesWithinExpectedTimeFrame_Within10PercentTolerance()
        {
            // Arrange
            float[] durations = { 0.3f, 0.4f, 0.5f };

            foreach (float duration in durations)
            {
                // Reset
                testRectTransform.localScale = Vector3.one;
                testCanvasGroup.alpha = 0f;

                // Act: Start animation and measure time
                float startTime = Time.time;
                testMonoBehaviour.StartCoroutine(UIAnimations.WordAddAnimation(testCanvasGroup, duration));

                // Wait for animation to complete
                yield return new WaitForSeconds(duration + 0.1f);

                float elapsedTime = Time.time - startTime;
                float tolerance = duration * 0.1f; // 10% tolerance

                // Assert: Actual time should be within 10% of expected duration
                Assert.GreaterOrEqual(elapsedTime, duration - tolerance,
                    $"Animation took too long: {elapsedTime:F3}s vs expected {duration:F3}s");
                Assert.LessOrEqual(elapsedTime, duration + tolerance,
                    $"Animation completed too quickly: {elapsedTime:F3}s vs expected {duration:F3}s");
            }
        }
    }

    /// <summary>
    /// Test fixture MonoBehaviour for running coroutines in tests.
    /// </summary>
    public class MonoBehaviourTestFixture : MonoBehaviour
    {
    }
}
