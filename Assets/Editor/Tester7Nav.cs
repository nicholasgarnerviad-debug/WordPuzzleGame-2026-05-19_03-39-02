using UnityEditor;
using UnityEngine;
using WordPuzzle.UI;

public static class Tester7Nav
{
    [MenuItem("Tester7/Show MainMenu")]
    public static void ShowMainMenu()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowMainMenu();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Show Gameplay")]
    public static void ShowGameplay()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowGameplay();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Show Settings")]
    public static void ShowSettings()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowSettings();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Show Library")]
    public static void ShowLibrary()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowLibrary();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Show Results")]
    public static void ShowResults()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowResults();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Show TimeAttackSetup")]
    public static void ShowTimeAttackSetup()
    {
        var ui = UnityEngine.Object.FindObjectOfType<UIManager>();
        if (ui != null) ui.ShowTimeAttackSetup();
        else Debug.LogError("Tester7: UIManager not found");
    }

    [MenuItem("Tester7/Start ClassicMode")]
    public static void StartClassicMode()
    {
        var mm = UnityEngine.Object.FindObjectOfType<MainMenuScreen>(true);
        if (mm != null) mm.SelectClassicMode();
        else Debug.LogError("Tester7: MainMenuScreen not found");
    }

    [MenuItem("Tester7/Start PuzzleShowMode")]
    public static void StartPuzzleShowMode()
    {
        var mm = UnityEngine.Object.FindObjectOfType<MainMenuScreen>(true);
        if (mm != null) mm.SelectPuzzleShowMode();
        else Debug.LogError("Tester7: MainMenuScreen not found");
    }

    [MenuItem("Tester7/Start TimeAttackMode")]
    public static void StartTimeAttackMode()
    {
        var mm = UnityEngine.Object.FindObjectOfType<MainMenuScreen>(true);
        if (mm != null) mm.SelectTimeAttackMode();
        else Debug.LogError("Tester7: MainMenuScreen not found");
    }

    [MenuItem("Tester7/Click TimeAttack 60Timed")]
    public static void ClickTimeAttack60Timed()
    {
        var setup = UnityEngine.Object.FindObjectOfType<TimeAttackSetupScreen>(true);
        if (setup == null) { Debug.LogError("Tester7: TimeAttackSetupScreen not found"); return; }
        var f = typeof(TimeAttackSetupScreen).GetField("btn60Timed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f == null) { Debug.LogError("Tester7: btn60Timed field not found"); return; }
        var btn = f.GetValue(setup) as UnityEngine.UI.Button;
        if (btn == null) { Debug.LogError("Tester7: btn60Timed is null"); return; }
        btn.onClick.Invoke();
    }
}
