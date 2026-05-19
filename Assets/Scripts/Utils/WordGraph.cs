using UnityEngine;
using System.Collections.Generic;

public class WordGraph
{
    private Dictionary<char, HashSet<string>> graph;

    public WordGraph(List<string> words)
    {
        graph = new Dictionary<char, HashSet<string>>();
        BuildGraph(words);
    }

    private void BuildGraph(List<string> words)
    {
        foreach (var word in words)
        {
            char firstLetter = char.ToLower(word[0]);
            if (!graph.ContainsKey(firstLetter))
            {
                graph[firstLetter] = new HashSet<string>();
            }
            graph[firstLetter].Add(word.ToLower());
        }
    }

    public bool IsValidWord(string word)
    {
        word = word.ToLower();
        char firstLetter = word[0];

        if (!graph.ContainsKey(firstLetter))
            return false;

        return graph[firstLetter].Contains(word);
    }

    public List<string> GetWordsStartingWith(char letter)
    {
        letter = char.ToLower(letter);
        if (graph.ContainsKey(letter))
        {
            return new List<string>(graph[letter]);
        }
        return new List<string>();
    }
}
