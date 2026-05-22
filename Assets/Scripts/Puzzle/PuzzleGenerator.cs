using System.Collections.Generic;

public class PuzzleGenerator : IPuzzleGenerator
{
    private WordGraph wordGraph;
    private Dictionary<int, TierData> tierCache;
    private System.Random random;

    public PuzzleGenerator(WordGraph wordGraph, Dictionary<int, TierData> tierCache = null)
    {
        this.wordGraph = wordGraph;
        this.tierCache = tierCache ?? new Dictionary<int, TierData>();
        this.random = new System.Random();
    }

    public PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
    {
        if (!tierCache.ContainsKey(tierId))
        {
            return null;
        }

        TierData tier = tierCache[tierId];

        if (puzzleIndex >= tier.puzzles.Length)
        {
            return null;
        }

        return tier.puzzles[puzzleIndex];
    }

    public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Get words of appropriate length based on difficulty
            int wordLength = GetWordLengthForDifficulty(difficulty);
            string startWord = GetRandomWordOfLength(wordLength);

            if (string.IsNullOrEmpty(startWord))
                continue;

            int targetDistance = GetTargetDistance(difficulty);

            // Find end word at approximately targetDistance away
            var path = FindPathOfLength(startWord, targetDistance);

            if (path.Count > 0 && path.Count > 1)
            {
                string endWord = path[path.Count - 1];
                var puzzle = new PuzzleDefinition
                {
                    puzzleId = random.Next(10000, 99999),
                    startWord = startWord,
                    endWord = endWord,
                    optimalSteps = path.Count - 1,
                    solution = path.ToArray(),
                    seedValue = random.Next()
                };

                sw.Stop();
                return puzzle;
            }
        }

        // Fallback: return a simple puzzle if generation fails
        sw.Stop();
        return CreateFallbackPuzzle();
    }

    private int GetWordLengthForDifficulty(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 3,
            Difficulty.Medium => 4,
            Difficulty.Hard => 5,
            _ => 3
        };
    }

    private string GetRandomWordOfLength(int length)
    {
        // This would be populated from an actual word list
        // For now, return test words
        return length switch
        {
            3 => "cat",
            4 => "word",
            5 => "world",
            _ => "cat"
        };
    }

    private int GetTargetDistance(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 2,
            Difficulty.Medium => 4,
            Difficulty.Hard => 6,
            _ => 3
        };
    }

    private List<string> FindPathOfLength(string start, int targetLength)
    {
        // BFS to find path of approximately targetLength
        var queue = new Queue<(string word, List<string> path)>();
        var visited = new HashSet<string>();

        queue.Enqueue((start, new List<string> { start }));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();

            // If we reach target distance, return path
            if (path.Count - 1 == targetLength)
                return path;

            // Don't go beyond target distance
            if (path.Count - 1 > targetLength)
                continue;

            // Get neighbors from word graph
            if (wordGraph != null)
            {
                // This would use actual word graph neighbors
                // For now, simple demonstration
                var neighbors = GetMockNeighbors(current);

                foreach (string neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<string>(path);
                        newPath.Add(neighbor);
                        queue.Enqueue((neighbor, newPath));
                    }
                }
            }
        }

        // Return any valid path if we didn't reach exact target
        return new List<string>();
    }

    private List<string> GetMockNeighbors(string word)
    {
        // Mock neighbors for demo purposes
        // In production, this would query wordGraph
        var neighbors = new List<string>();

        // Simple mock data for testing
        if (word == "cat") neighbors = new List<string> { "bat", "hat", "mat" };
        if (word == "bat") neighbors = new List<string> { "cat", "hat", "mat", "bag" };
        if (word == "hat") neighbors = new List<string> { "cat", "bat", "mat", "map" };
        if (word == "mat") neighbors = new List<string> { "cat", "bat", "hat", "map" };
        if (word == "bag") neighbors = new List<string> { "bat", "map", "dag" };
        if (word == "map") neighbors = new List<string> { "hat", "mat", "bag", "nap" };

        return neighbors;
    }

    private PuzzleDefinition CreateFallbackPuzzle()
    {
        return new PuzzleDefinition
        {
            puzzleId = 1,
            startWord = "cat",
            endWord = "dog",
            optimalSteps = 3,
            solution = new[] { "cat", "bat", "bag", "dog" },
            seedValue = 0
        };
    }
}

public interface IPuzzleGenerator
{
    PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex);
    PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty);
}
