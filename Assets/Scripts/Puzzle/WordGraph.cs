using System.Collections.Generic;

public class WordGraph
{
    // Word -> Adjacent words (one letter difference)
    private Dictionary<string, HashSet<string>> adjacencyList;
    private HashSet<string> allWords;

    public WordGraph()
    {
        adjacencyList = new Dictionary<string, HashSet<string>>();
        allWords = new HashSet<string>();
    }

    public void AddWord(string word)
    {
        word = word.ToLower();
        if (!allWords.Contains(word))
        {
            allWords.Add(word);
            if (!adjacencyList.ContainsKey(word))
                adjacencyList[word] = new HashSet<string>();
        }
    }

    public void BuildAdjacencies()
    {
        // Build one-letter-difference connections
        var wordList = new List<string>(allWords);

        for (int i = 0; i < wordList.Count; i++)
        {
            for (int j = i + 1; j < wordList.Count; j++)
            {
                if (HaveOneLetterDifference(wordList[i], wordList[j]))
                {
                    adjacencyList[wordList[i]].Add(wordList[j]);
                    adjacencyList[wordList[j]].Add(wordList[i]);
                }
            }
        }
    }

    public List<string> GetShortestPath(string start, string end)
    {
        if (!allWords.Contains(start) || !allWords.Contains(end))
            return new List<string>();

        if (start == end)
            return new List<string> { start };

        // BFS for shortest path
        var queue = new Queue<string>();
        var visited = new HashSet<string>();
        var parent = new Dictionary<string, string>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            if (current == end)
            {
                // Reconstruct path
                var path = new List<string>();
                string node = end;
                while (node != null)
                {
                    path.Add(node);
                    parent.TryGetValue(node, out node);
                }
                path.Reverse();
                return path;
            }

            if (adjacencyList.ContainsKey(current))
            {
                foreach (string neighbor in adjacencyList[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return new List<string>();  // No path
    }

    public bool CanSolve(string start, string end)
    {
        return GetShortestPath(start, end).Count > 0;
    }

    public bool IsValidWord(string word)
    {
        return allWords.Contains(word.ToLower());
    }

    private bool HaveOneLetterDifference(string word1, string word2)
    {
        if (word1.Length != word2.Length)
            return false;

        int differences = 0;
        for (int i = 0; i < word1.Length; i++)
        {
            if (word1[i] != word2[i])
                differences++;
            if (differences > 1)
                return false;
        }

        return differences == 1;
    }

    public int GetDistance(string word1, string word2)
    {
        List<string> path = GetShortestPath(word1, word2);
        return path.Count > 0 ? path.Count - 1 : -1;
    }

    public List<string> GetWordsOfLength(int length)
    {
        var result = new List<string>();
        foreach (string word in allWords)
        {
            if (word.Length == length)
                result.Add(word);
        }
        return result;
    }
}
