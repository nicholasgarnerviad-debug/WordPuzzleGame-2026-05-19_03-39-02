using System.Text;
using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// Debug.Log analytics impl (Task 41B) — the LIVE default until the user's Firebase
    /// project exists. THE FIREBASE SWAP POINT: when google-services.json lands, add a
    /// FirebaseAnalytics : IAnalytics beside this class and swap the single `new LogAnalytics()`
    /// in GameBootstrap. No call sites change — the taxonomy is already Firebase-shaped.
    /// </summary>
    public sealed class LogAnalytics : IAnalytics
    {
        public void Log(string eventName) => Debug.Log($"[Analytics] {eventName}");

        public void Log(string eventName, params (string key, object value)[] p)
        {
            var sb = new StringBuilder("[Analytics] ").Append(eventName);
            if (p != null)
                foreach (var (key, value) in p)
                    sb.Append(' ').Append(key).Append('=').Append(value);
            Debug.Log(sb.ToString());
        }
    }

    /// <summary>No-op analytics (tests / hard-off).</summary>
    public sealed class NullAnalytics : IAnalytics
    {
        public void Log(string eventName) { }
        public void Log(string eventName, params (string key, object value)[] p) { }
    }
}
