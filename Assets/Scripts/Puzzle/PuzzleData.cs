using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PuzzleData
{
    public List<string> words; // Words to form in this puzzle
    public string centerLetter; // Center letter (for rung-based games)
    public int difficulty; // 1-5 difficulty level

    public PuzzleData()
    {
        words = new List<string>();
        centerLetter = "";
        difficulty = 1;
    }
}
