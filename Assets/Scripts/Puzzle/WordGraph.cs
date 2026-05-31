using System.Collections.Generic;
using System.Text;

namespace WordPuzzle.Puzzle
{
    public class WordGraph
    {
        // Word -> Adjacent words (one letter difference)
        private Dictionary<string, HashSet<string>> adjacencyList;
        private HashSet<string> allWords;

        /// <summary>
        /// Test seam: incremented every time a BFS traversal is performed
        /// (GetShortestPath / ComputeDistancesFrom). Tests can snapshot and assert no
        /// BFS occurs during steady-state validation calls.
        /// </summary>
        public int BfsTraversalCount { get; private set; }

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

        /// <summary>
        /// Build one-letter-difference adjacency using wildcard pattern bucketing
        /// (O(n·L) instead of the previous O(n²)). For each word, generate L pattern
        /// keys by replacing position p with '_'. Words sharing a pattern key differ
        /// at exactly that position; mark them mutual neighbors.
        /// </summary>
        public void BuildAdjacencies()
        {
            // pattern -> list of words sharing that pattern
            var buckets = new Dictionary<string, List<string>>(allWords.Count * 4);
            var sb = new StringBuilder();

            foreach (var word in allWords)
            {
                int len = word.Length;
                for (int p = 0; p < len; p++)
                {
                    sb.Length = 0;
                    sb.Append(word);
                    sb[p] = '_';
                    string key = sb.ToString();

                    if (!buckets.TryGetValue(key, out var list))
                    {
                        list = new List<string>(2);
                        buckets[key] = list;
                    }
                    list.Add(word);
                }
            }

            foreach (var kvp in buckets)
            {
                var list = kvp.Value;
                int count = list.Count;
                if (count < 2) continue;

                for (int i = 0; i < count; i++)
                {
                    var wi = list[i];
                    var setI = adjacencyList[wi];
                    for (int j = i + 1; j < count; j++)
                    {
                        var wj = list[j];
                        setI.Add(wj);
                        adjacencyList[wj].Add(wi);
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

            BfsTraversalCount++;

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

        /// <summary>
        /// Single-source BFS that returns the shortest-path distance from
        /// <paramref name="source"/> to every reachable word. Words not in the graph
        /// or unreachable from <paramref name="source"/> are absent from the result.
        /// Used by validators to avoid running a BFS per submission.
        /// </summary>
        public Dictionary<string, int> ComputeDistancesFrom(string source)
        {
            var result = new Dictionary<string, int>();
            if (source == null || !allWords.Contains(source))
                return result;

            BfsTraversalCount++;

            result[source] = 0;
            var queue = new Queue<string>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                int nextDist = result[current] + 1;

                if (!adjacencyList.TryGetValue(current, out var neighbors)) continue;
                foreach (var n in neighbors)
                {
                    if (!result.ContainsKey(n))
                    {
                        result[n] = nextDist;
                        queue.Enqueue(n);
                    }
                }
            }

            return result;
        }

        public bool CanSolve(string start, string end)
        {
            return GetShortestPath(start, end).Count > 0;
        }

        public bool IsValidWord(string word)
        {
            return allWords.Contains(word.ToLower());
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
}
