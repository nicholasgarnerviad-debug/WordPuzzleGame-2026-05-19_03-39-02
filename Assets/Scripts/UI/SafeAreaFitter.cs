using UnityEngine;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Insets an anchored RectTransform by the device's reported safe area
    /// so that UI content clears the notch and home indicator on iOS/Android.
    /// Attach to any full-screen panel that should respect safe area.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;

        private void Awake() => _rect = GetComponent<RectTransform>();

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
                Apply();
        }

        private void Apply()
        {
            if (_rect == null) return;
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);

            var anchorMin = safeArea.position / screenSize;
            var anchorMax = (safeArea.position + safeArea.size) / screenSize;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
        }
    }
}
