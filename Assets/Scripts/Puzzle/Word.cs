using UnityEngine;

[System.Serializable]
public class Word
{
    public string text;
    public int length;

    public Word(string text)
    {
        this.text = text.ToLower();
        this.length = text.Length;
    }

    public override string ToString() => text;
}
