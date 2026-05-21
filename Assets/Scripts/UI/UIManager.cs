using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly Dictionary<System.Type, MonoBehaviour> screens = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterScreen<T>(T screen) where T : MonoBehaviour
    {
        screens[typeof(T)] = screen;
        screen.gameObject.SetActive(false);
    }

    public void ShowScreen<T>() where T : MonoBehaviour
    {
        HideAllScreens();
        if (screens.TryGetValue(typeof(T), out var screen))
            screen.gameObject.SetActive(true);
        else
            Logger.LogWarning($"[UIManager] Screen {typeof(T).Name} not registered");
    }

    public void HideAllScreens()
    {
        foreach (var screen in screens.Values)
        {
            if (screen != null) screen.gameObject.SetActive(false);
        }
    }
}
