using System.Collections;
using UnityEngine;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Reusable animation utility system for smooth UI feedback animations.
    /// Provides coroutine-based animations for button taps, tile interactions, word additions, and screen transitions.
    /// Set ReduceMotion = true to skip all animations (accessibility gate, Task 7A).
    /// </summary>
    public static class UIAnimations
    {
        /// <summary>Task 7A — when true, all animation coroutines apply end-state instantly and return.</summary>
        public static bool ReduceMotion = false;

        // ============================================================
        //  Task 8D — Motion vocabulary constants
        //  Single source of truth for all animation durations and easing.
        // ============================================================

        /// <summary>Micro-interactions: button taps, letter-tile punches, key presses.</summary>
        public const float MICRO = 0.16f;

        /// <summary>Standard transitions: screen fades, element enter/exit.</summary>
        public const float STANDARD = 0.22f;

        /// <summary>
        /// Ease-out cubic: fast start, decelerates to rest. Used for all Task 8 motions.
        /// Formula: 1 - (1-t)^3
        /// </summary>
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        // ============================================================

        /// <summary>
        /// Animates a button tap with scale down then back up effect.
        /// Animation: 1.0 → 0.95 → 1.0 with ease-out easing.
        /// </summary>
        /// <param name="button">The RectTransform of the button to animate</param>
        /// <param name="duration">Total animation duration in seconds (default MICRO)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator ScaleButtonTap(RectTransform button, float duration = MICRO)
        {
            if (ReduceMotion) { button.localScale = Vector3.one; yield break; }
            Vector3 originalScale = button.localScale;
            float targetScale = 0.95f;
            float elapsedTime = 0f;

            // Scale down phase: 1.0 → 0.95
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float scale = Mathf.Lerp(1f, targetScale, EaseOutCubic(progress));
                button.localScale = originalScale * scale;
                yield return null;
            }

            elapsedTime = 0f;

            // Scale up phase: 0.95 → 1.0
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float scale = Mathf.Lerp(targetScale, 1f, EaseOutCubic(progress));
                button.localScale = originalScale * scale;
                yield return null;
            }

            // Ensure final scale is exactly 1.0
            button.localScale = originalScale;
        }

        /// <summary>
        /// Animates a tile tap with scale up then back down effect.
        /// Animation: 1.0 → 1.1 → 1.0 with ease-out-cubic easing.
        /// </summary>
        /// <param name="tile">The RectTransform of the tile to animate</param>
        /// <param name="duration">Total animation duration in seconds (default MICRO)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator ScaleTileTap(RectTransform tile, float duration = MICRO)
        {
            if (ReduceMotion) { tile.localScale = Vector3.one; yield break; }
            Vector3 originalScale = tile.localScale;
            float targetScale = 1.1f;
            float elapsedTime = 0f;

            // Scale up phase: 1.0 → 1.1
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float scale = Mathf.Lerp(1f, targetScale, EaseOutCubic(progress));
                tile.localScale = originalScale * scale;
                yield return null;
            }

            elapsedTime = 0f;

            // Scale down phase: 1.1 → 1.0
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (duration * 0.5f);
                float scale = Mathf.Lerp(targetScale, 1f, EaseOutCubic(progress));
                tile.localScale = originalScale * scale;
                yield return null;
            }

            // Ensure final scale is exactly 1.0
            tile.localScale = originalScale;
        }

        /// <summary>
        /// Task 8D — calm word-add settle: fade in + gentle scale rise (1.0→1.04→1.0), ease-out, no bounce.
        /// Replaces the former overshoot (1.0→1.2→1.0) which felt cartoon-bouncy.
        /// Starts with alpha = 0, ends with alpha = 1.
        /// </summary>
        /// <param name="word">The CanvasGroup of the word to animate</param>
        /// <param name="duration">Total animation duration in seconds (default STANDARD)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator WordAddAnimation(CanvasGroup word, float duration = STANDARD)
        {
            if (ReduceMotion)
            {
                word.alpha = 1f;
                word.GetComponent<RectTransform>().localScale = Vector3.one;
                yield break;
            }
            RectTransform wordTransform = word.GetComponent<RectTransform>();
            Vector3 originalScale = wordTransform.localScale;
            float elapsedTime = 0f;

            // Initial state: alpha = 0, scale at 1.0 (no pre-squeeze)
            word.alpha = 0f;

            // Rise phase: scale 1.0 → 1.04, fade 0 → 1 (ease-out-cubic)
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float p = EaseOutCubic(Mathf.Clamp01(elapsedTime / (duration * 0.5f)));
                word.alpha = p;
                wordTransform.localScale = originalScale * Mathf.Lerp(1f, 1.04f, p);
                yield return null;
            }

            elapsedTime = 0f;

            // Settle phase: scale 1.04 → 1.0, alpha stays 1 (ease-out-cubic)
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float p = EaseOutCubic(Mathf.Clamp01(elapsedTime / (duration * 0.5f)));
                wordTransform.localScale = originalScale * Mathf.Lerp(1.04f, 1f, p);
                yield return null;
            }

            // Ensure final state
            word.alpha = 1f;
            wordTransform.localScale = originalScale;
        }

        /// <summary>
        /// Animates a fade in or fade out transition.
        /// Fade in: opacity 0 → 1. Fade out: opacity 1 → 0.
        /// Task 8D: default duration changed to STANDARD (0.22s); uses EaseOutCubic.
        /// </summary>
        /// <param name="screen">The CanvasGroup of the screen to animate</param>
        /// <param name="fadeIn">True for fade in, false for fade out</param>
        /// <param name="duration">Total animation duration in seconds (default STANDARD)</param>
        /// <returns>Coroutine for use with StartCoroutine()</returns>
        public static IEnumerator FadeTransition(CanvasGroup screen, bool fadeIn, float duration = STANDARD)
        {
            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsedTime = 0f;

            screen.alpha = startAlpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                screen.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseOutCubic(progress));
                yield return null;
            }

            // Ensure final alpha is exactly the target
            screen.alpha = targetAlpha;
        }

        /// <summary>
        /// Settle-ease for row-accept: weighted ease-out (cubic) over ~180ms.
        /// Non-bouncy — reaches end state smoothly. Task 7A.
        /// </summary>
        /// <param name="rt">The RectTransform to settle.</param>
        /// <param name="duration">Settle duration in seconds (default 0.18s).</param>
        /// <returns>Coroutine for use with StartCoroutine().</returns>
        public static IEnumerator RowAcceptSettle(RectTransform rt, float duration = 0.18f)
        {
            if (ReduceMotion || rt == null) yield break;
            Vector3 start = rt.localScale;
            Vector3 end = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f); // ease-OutCubic
                rt.localScale = Vector3.LerpUnclamped(start, end, eased);
                yield return null;
            }
            rt.localScale = end;
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
