using System.Collections;
using UnityEngine;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Reusable animation utility system for smooth UI feedback animations.
    /// Provides coroutine-based animations for button taps, tile interactions, word additions, and screen transitions.
    /// </summary>
    public static class UIAnimations
    {
        /// <summary>
        /// Animates a button tap with scale down then back up effect.
        /// Animation: 1.0 → 0.95 → 1.0 with ease-out easing.
        /// </summary>
        /// <param name="button">The RectTransform of the button to animate</param>
        /// <param name="duration">Total animation duration in seconds (default 0.3s)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator ScaleButtonTap(RectTransform button, float duration = 0.3f)
        {
            Vector3 originalScale = button.localScale;
            float targetScale = 0.95f;
            float elapsedTime = 0f;

            // Scale down phase: 1.0 → 0.95
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedProgress = EaseOut(progress);
                float scale = Mathf.Lerp(1f, targetScale, easedProgress);
                button.localScale = originalScale * scale;
                yield return null;
            }

            elapsedTime = 0f;

            // Scale up phase: 0.95 → 1.0
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedProgress = EaseOut(progress);
                float scale = Mathf.Lerp(targetScale, 1f, easedProgress);
                button.localScale = originalScale * scale;
                yield return null;
            }

            // Ensure final scale is exactly 1.0
            button.localScale = originalScale;
        }

        /// <summary>
        /// Animates a tile tap with scale up then back down effect.
        /// Animation: 1.0 → 1.1 → 1.0 with ease-out easing.
        /// </summary>
        /// <param name="tile">The RectTransform of the tile to animate</param>
        /// <param name="duration">Total animation duration in seconds (default 0.3s)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator ScaleTileTap(RectTransform tile, float duration = 0.3f)
        {
            Vector3 originalScale = tile.localScale;
            float targetScale = 1.1f;
            float elapsedTime = 0f;

            // Scale up phase: 1.0 → 1.1
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedProgress = EaseOut(progress);
                float scale = Mathf.Lerp(1f, targetScale, easedProgress);
                tile.localScale = originalScale * scale;
                yield return null;
            }

            elapsedTime = 0f;

            // Scale down phase: 1.1 → 1.0
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedProgress = EaseOut(progress);
                float scale = Mathf.Lerp(targetScale, 1f, easedProgress);
                tile.localScale = originalScale * scale;
                yield return null;
            }

            // Ensure final scale is exactly 1.0
            tile.localScale = originalScale;
        }

        /// <summary>
        /// Animates a word addition with bounce scale and fade in effect.
        /// Animation: Bounce scale (1.0 → 1.2 → 1.0) + Fade in (0 → 1 opacity).
        /// Starts with alpha = 0, ends with alpha = 1.
        /// </summary>
        /// <param name="word">The CanvasGroup of the word to animate</param>
        /// <param name="duration">Total animation duration in seconds (default 0.4s)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator WordAddAnimation(CanvasGroup word, float duration = 0.4f)
        {
            RectTransform wordTransform = word.GetComponent<RectTransform>();
            Vector3 originalScale = wordTransform.localScale;
            float elapsedTime = 0f;

            // Initial state: alpha = 0
            word.alpha = 0f;

            // Scale up phase: 1.0 → 1.2, fade in: 0 → 1
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedScaleProgress = EaseOut(progress);
                float easedFadeProgress = EaseInOut(progress);

                float scale = Mathf.Lerp(1f, 1.2f, easedScaleProgress);
                word.alpha = Mathf.Lerp(0f, 1f, easedFadeProgress);
                wordTransform.localScale = originalScale * scale;
                yield return null;
            }

            elapsedTime = 0f;

            // Scale down phase: 1.2 → 1.0, maintain alpha at 1
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float easedProgress = EaseOut(progress);

                float scale = Mathf.Lerp(1.2f, 1f, easedProgress);
                wordTransform.localScale = originalScale * scale;
                yield return null;
            }

            // Ensure final state
            word.alpha = 1f;
            wordTransform.localScale = originalScale;
        }

        /// <summary>
        /// Animates a fade in or fade out transition.
        /// Fade in: opacity 0 → 1.
        /// Fade out: opacity 1 → 0.
        /// </summary>
        /// <param name="screen">The CanvasGroup of the screen to animate</param>
        /// <param name="fadeIn">True for fade in, false for fade out</param>
        /// <param name="duration">Total animation duration in seconds (default 0.5s)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator FadeTransition(CanvasGroup screen, bool fadeIn, float duration = 0.5f)
        {
            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsedTime = 0f;

            screen.alpha = startAlpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                float easedProgress = EaseInOut(progress);
                screen.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
                yield return null;
            }

            // Ensure final alpha is exactly the target
            screen.alpha = targetAlpha;
        }

        /// <summary>
        /// Ease-out easing function: smooth deceleration.
        /// Formula: t = 1 - (1-t)^2
        /// </summary>
        /// <param name="t">Normalized time 0-1</param>
        /// <returns>Eased value</returns>
        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// Ease-in-out easing function: acceleration then deceleration.
        /// Formula: t < 0.5 ? 2*t*t : -1 + (4-2*t)*t
        /// </summary>
        /// <param name="t">Normalized time 0-1</param>
        /// <returns>Eased value</returns>
        private static float EaseInOut(float t)
        {
            if (t < 0.5f)
            {
                return 2f * t * t;
            }
            else
            {
                return -1f + (4f - 2f * t) * t;
            }
        }
    }
}
