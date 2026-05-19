using UnityEngine;

public static class Logger
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string message)
    {
        Debug.Log($"[Game] {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(string message)
    {
        Debug.LogError($"[Game] {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning($"[Game] {message}");
    }
}
