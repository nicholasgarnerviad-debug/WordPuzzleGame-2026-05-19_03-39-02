using UnityEngine;
using System.Threading.Tasks;
using WordPuzzle.Persistence;

namespace WordPuzzle.Persistence
{
    public class TierDataLoader
{
    public async Task<TierData> LoadTierAsync(int tierId)
    {
        // Load from Resources/Data/tier_definitions.json
        TextAsset tierFile = Resources.Load<TextAsset>("Data/tier_definitions");

        if (tierFile == null)
        {
            Debug.LogError("tier_definitions.json not found in Resources/Data/");
            return CreateDefaultTier(tierId);
        }

        TierDefinitionsWrapper wrapper = JsonUtility.FromJson<TierDefinitionsWrapper>(tierFile.text);

        if (wrapper.tiers == null || wrapper.tiers.Length < tierId)
        {
            return CreateDefaultTier(tierId);
        }

        await Task.Delay(0);
        return wrapper.tiers[tierId - 1];
    }

    private TierData CreateDefaultTier(int tierId)
    {
        return new TierData
        {
            tierId = tierId,
            puzzles = new PuzzleDefinition[] { },
            isUnlocked = tierId == 1,
            unlockedTimestamp = tierId == 1 ? System.DateTime.UtcNow.Ticks : 0
        };
    }
}

    [System.Serializable]
    public class TierDefinitionsWrapper
    {
        public TierData[] tiers;
    }
}
