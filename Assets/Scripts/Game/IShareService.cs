using UnityEngine;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Abstracts the OS share surface. Default <see cref="ClipboardShareService"/>
    /// has zero dependencies (writes to GUIUtility.systemCopyBuffer). A
    /// NativeShare-backed implementation can be swapped in via GameBootstrap
    /// AFTER the user approves adding the NativeShare plugin to the project.
    /// </summary>
    public interface IShareService
    {
        /// <summary>
        /// Surface the share payload. Implementations that lack native share
        /// support should write <paramref name="text"/> to the clipboard and
        /// ignore the PNG bytes.
        /// </summary>
        /// <param name="text">Plain text payload (the emoji grid).</param>
        /// <param name="pngOptional">Pre-rendered PNG image, or null.</param>
        /// <returns>true if the share was queued/copied, false on hard failure.</returns>
        bool Share(string text, byte[] pngOptional = null);
    }

    /// <summary>
    /// Default zero-dependency implementation. Copies text to the clipboard.
    /// PNG bytes are ignored. Toast is shown by the caller.
    /// </summary>
    public sealed class ClipboardShareService : IShareService
    {
        public bool Share(string text, byte[] pngOptional = null)
        {
            try
            {
                GUIUtility.systemCopyBuffer = text ?? string.Empty;
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ClipboardShareService] copy failed: {ex.Message}");
                return false;
            }
            // TODO(user-approval): if/when NativeShare (or similar) is added to the
            // project, introduce NativeShareService : IShareService that calls
            // new NativeShare().AddFile(png).SetText(text).Share() and swap the
            // injection in GameBootstrap. See Task 2B in PROJECT history.
        }
    }
}
