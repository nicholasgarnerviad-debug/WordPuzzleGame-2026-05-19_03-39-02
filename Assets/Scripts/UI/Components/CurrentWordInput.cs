using UnityEngine;
using TMPro;

public class CurrentWordInput : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private TextMeshProUGUI targetText;

    public void UpdateInput(string currentInput, string targetWord)
    {
        inputText.text = currentInput.ToUpper();
        targetText.text = "Target: " + (targetWord != null ? targetWord.ToUpper() : "");
    }

    public void Clear()
    {
        inputText.text = "";
    }
}
