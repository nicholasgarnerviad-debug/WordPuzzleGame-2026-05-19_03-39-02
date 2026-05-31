using System.IO;
using UnityEditor;
using UnityEngine;

namespace WordPuzzleGame.EditorTools
{
    public static class DailyVerify
    {
        [MenuItem("Tools/DailyVerify/Snap MainMenu")]
        public static void SnapMainMenu()
        {
            string path = "Assets/Screenshots/daily_mainmenu.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[DailyVerify] Capture queued: {path}");
        }
    }
}
