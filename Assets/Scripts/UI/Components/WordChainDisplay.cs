using UnityEngine;
using TMPro;

namespace WordPuzzle.UI
{
    public class WordChainDisplay : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI wordPrefab;

    public void UpdateChain(string[] wordChain)
    {
        // Clear existing words
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Display each word in uppercase
        if (wordChain != null)
        {
            foreach (string word in wordChain)
            {
                TextMeshProUGUI wordText = Instantiate(wordPrefab, container);
                wordText.text = word.ToUpper();
            }
        }
    }
}
}
