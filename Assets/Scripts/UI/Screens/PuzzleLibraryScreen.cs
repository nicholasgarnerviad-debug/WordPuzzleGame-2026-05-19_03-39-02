using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    public class PuzzleLibraryScreen : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button backButton;

        public event Action OnBackToMenu;

        private void OnEnable()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            PopulateContent();
        }

        public void Hide() => gameObject.SetActive(false);

        private void PopulateContent()
        {
            if (contentRoot == null) return;

            ClearContent();

            var tierData = LoadTierDefinitions();
            if (tierData == null) return;

            foreach (var tier in tierData.tiers)
            {
                CreateTierHeader(tier);
                if (tier.puzzles == null) continue;
                foreach (var puzzle in tier.puzzles)
                    CreatePuzzleRow(puzzle, tier.tierId);
            }
        }

        private void ClearContent()
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        private TierDefinitionsWrapper LoadTierDefinitions()
        {
            var asset = Resources.Load<TextAsset>("Data/tier_definitions");
            if (asset == null)
            {
                Debug.LogError("PuzzleLibraryScreen: tier_definitions.json not found");
                return null;
            }
            return JsonUtility.FromJson<TierDefinitionsWrapper>(asset.text);
        }

        private void CreateTierHeader(TierData tier)
        {
            var go = new GameObject($"TierHeader_{tier.tierId}");
            go.transform.SetParent(contentRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 60f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.18f, 0.3f, 1f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 0f);
            textRt.offsetMax = new Vector2(-20f, 0f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            string lockIcon = tier.isUnlocked ? "" : " [Locked]";
            tmp.text = $"Tier {tier.tierId}{lockIcon}";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = tier.isUnlocked ? new Color(0.9f, 0.8f, 0.3f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void CreatePuzzleRow(PuzzleDefinition puzzle, int tierId)
        {
            var go = new GameObject($"Puzzle_{puzzle.puzzleId}");
            go.transform.SetParent(contentRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 44f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(40f, 0f);
            textRt.offsetMax = new Vector2(-20f, 0f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{puzzle.puzzleId}. {puzzle.startWord} → {puzzle.endWord}  ({puzzle.optimalSteps} steps)";
            tmp.fontSize = 22;
            tmp.color = new Color(0.85f, 0.85f, 0.95f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // JSON wrapper types (mirrors GameBootstrap private types)
        [Serializable]
        private class TierDefinitionsWrapper
        {
            public TierData[] tiers;
        }

        [Serializable]
        private class TierData
        {
            public int tierId;
            public bool isUnlocked;
            public PuzzleDefinition[] puzzles;
        }

        [Serializable]
        private class PuzzleDefinition
        {
            public int puzzleId;
            public string startWord;
            public string endWord;
            public int optimalSteps;
        }
    }
}
