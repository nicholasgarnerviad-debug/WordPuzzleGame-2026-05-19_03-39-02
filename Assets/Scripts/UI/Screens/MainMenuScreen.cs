using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private Button classicModeButton;
        [SerializeField] private Button puzzleShowButton;
        [SerializeField] private Button timeAttackButton;

        public event Action OnClassicModeSelected;
        public event Action OnPuzzleShowSelected;
        public event Action OnTimeAttackSelected;

        private void OnEnable()
        {
            classicModeButton.onClick.AddListener(() => OnClassicModeSelected?.Invoke());
            puzzleShowButton.onClick.AddListener(() => OnPuzzleShowSelected?.Invoke());
            timeAttackButton.onClick.AddListener(() => OnTimeAttackSelected?.Invoke());
        }

        private void OnDisable()
        {
            classicModeButton.onClick.RemoveAllListeners();
            puzzleShowButton.onClick.RemoveAllListeners();
            timeAttackButton.onClick.RemoveAllListeners();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
