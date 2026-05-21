using UnityEngine;
using TMPro;

public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private TimeAttackMode boundMode;

    public void BindToMode(TimeAttackMode mode)
    {
        if (boundMode != null)
            boundMode.TimeChanged -= OnTimeChanged;

        boundMode = mode;

        if (boundMode != null)
            boundMode.TimeChanged += OnTimeChanged;

        gameObject.SetActive(boundMode != null);
    }

    private void OnDestroy()
    {
        if (boundMode != null)
            boundMode.TimeChanged -= OnTimeChanged;
    }

    private void OnTimeChanged(float remaining)
    {
        if (timerText == null) return;
        timerText.text  = $"Time: {remaining:F1}s";
        timerText.color = remaining < 10f ? Color.red
                        : remaining < 30f ? Color.yellow
                        : Color.green;
    }
}
