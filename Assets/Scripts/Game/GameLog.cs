namespace WordPuzzle
{
    /// <summary>
    /// Release-stripped logging for the Game assembly (v1.0 audit Track 4). The Utils
    /// <c>Logger</c> lives in an assembly this one does not reference, and unqualified
    /// <c>Logger</c> resolves to <c>UnityEngine.Logger</c> here — so the Conditional
    /// wrapper lives locally. Warnings/errors stay on <c>Debug</c> (wanted in release).
    /// </summary>
    internal static class GameLog
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message) => UnityEngine.Debug.Log($"[Game] {message}");
    }
}
