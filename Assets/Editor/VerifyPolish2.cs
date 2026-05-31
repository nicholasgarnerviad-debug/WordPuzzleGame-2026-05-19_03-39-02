#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class VerifyPolish2
{
    private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Tools/Verify Polish/A Keyboard")]
    public static void Keyboard()
    {
        var krs = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.name == "KeyboardRoot").ToArray();
        var sb = new StringBuilder();
        sb.Append("[VERIFY-2] KeyboardRoots=").Append(krs.Length);
        foreach (var kr in krs)
        {
            int btns = kr.GetComponentsInChildren<Button>(true).Length;
            sb.Append(" | children=").Append(kr.childCount)
              .Append(" buttons=").Append(btns)
              .Append(" active=").Append(kr.gameObject.activeInHierarchy)
              .Append(" pos=").Append(kr.anchoredPosition)
              .Append(" size=").Append(kr.sizeDelta);
        }
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Verify Polish/B Alphas And Score")]
    public static void AlphasAndScore()
    {
        var gpType = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
            .FirstOrDefault(t => t.Name == "GameplayScreen");
        if (gpType == null) { Debug.LogError("[VERIFY-2] no GameplayScreen type"); return; }
        var gps = Object.FindObjectsByType(gpType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (gps.Length == 0) { Debug.LogError("[VERIFY-2] no GameplayScreen instance"); return; }
        var gs = gps[0] as MonoBehaviour;
        var sb = new StringBuilder();
        sb.Append("[VERIFY-2]");
        AppendAlpha(sb, gpType, gs, "puzzleDisplayText");
        AppendAlpha(sb, gpType, gs, "currentInputText");
        AppendAlpha(sb, gpType, gs, "wordChainText");

        var stF = gpType.GetField("scoreText", BF);
        var st = stF?.GetValue(gs) as TMP_Text;
        if (st != null)
        {
            var rt = st.rectTransform;
            sb.Append(" | scoreText pos=").Append(rt.anchoredPosition)
              .Append(" font=").Append(st.fontSize)
              .Append(" align=").Append(st.alignment)
              .Append(" text='").Append(st.text).Append("'");
        }
        else sb.Append(" | scoreText=NULL");
        Debug.Log(sb.ToString());
    }

    private static void AppendAlpha(StringBuilder sb, System.Type t, MonoBehaviour gs, string field)
    {
        var f = t.GetField(field, BF);
        var tmp = f?.GetValue(gs) as TMP_Text;
        if (tmp == null) { sb.Append(" | ").Append(field).Append("=NULL"); return; }
        sb.Append(" | ").Append(field).Append(" alpha=").Append(tmp.color.a.ToString("F3"))
          .Append(" text='").Append(tmp.text).Append("'");
    }

    [MenuItem("Tools/Verify Polish/C Screenshot Polish v2")]
    public static void ScreenshotV2()
    {
        System.IO.Directory.CreateDirectory("Assets/Screenshots");
        string path = "Assets/Screenshots/polish_v2_play.png";
        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log("[VERIFY-2] screenshot requested -> " + path);
    }
}
#endif
