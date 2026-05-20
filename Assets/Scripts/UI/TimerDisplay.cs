using UnityEngine;
using TMPro;

public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private TimeAttackMode timeAttackMode;

    private void Start()
    {
        timeAttackMode = FindObjectOfType<TimeAttackMode>();
    }

    private void Update()
    {
        if (timeAttackMode != null)
        {
            float timeRemaining = timeAttackMode.GetTimeRemaining();
            timerText.text = $"Time: {timeRemaining:F1}s";

            // Change color based on time
            if (timeRemaining < 10f)
                timerText.color = Color.red;
            else if (timeRemaining < 30f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.green;
        }
    }
}
