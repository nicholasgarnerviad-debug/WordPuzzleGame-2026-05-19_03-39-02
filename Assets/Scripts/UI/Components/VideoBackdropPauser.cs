using UnityEngine;
using UnityEngine.Video;

namespace WordPuzzle.UI.Components
{
    /// <summary>
    /// Task 44 — battery hygiene for the looping space backdrop: pauses the sibling
    /// <see cref="VideoPlayer"/> when the app is backgrounded or loses focus and resumes it on
    /// return. Attached by <c>UIThemeManager.EnsureVideoBackground</c> next to the player; the
    /// backdrop is a muted ambient loop, so blind resume (no position bookkeeping) is correct.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DisallowMultipleComponent]
    public class VideoBackdropPauser : MonoBehaviour
    {
        private VideoPlayer player;

        private void Awake() => player = GetComponent<VideoPlayer>();

        private void OnApplicationPause(bool paused) => SetPaused(paused);

        private void OnApplicationFocus(bool hasFocus) => SetPaused(!hasFocus);

        private void SetPaused(bool paused)
        {
            if (player == null || player.clip == null) return;
            if (paused) { if (player.isPlaying) player.Pause(); }
            else        { if (!player.isPlaying) player.Play(); }
        }
    }
}
