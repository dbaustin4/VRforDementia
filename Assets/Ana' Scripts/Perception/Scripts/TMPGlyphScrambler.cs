// TMPGlyphScrambler.cs – replaces visible letters/digits with look‑alikes intermittently
using UnityEngine;
using TMPro;
using System.Text;
public class TMPGlyphScrambler : MonoBehaviour
{
    public float scrambleEverySeconds = 1.5f; public float chancePerChar = 0.35f;
    public bool digitsOnly = false; TMP_Text tmp; string original; float t;
    char Sub(char c)
    {
        if (char.IsDigit(c)) return "O0lI"[Random.Range(0, 5)];
        if (digitsOnly) return c;
        string s = "aâăeéèiìo0OQ D B 8 S 5 Z 2 l I 1"; // crude confusions
        if (char.IsLetter(c)) return char.IsUpper(c) ? char.ToUpper(s[Random.Range(0, s.Length)]) : s[Random.Range(0, s.Length)];
        return c;
    }
    void Awake() { tmp = GetComponent<TMP_Text>(); if (tmp) original = tmp.text; }
    void OnEnable() { if (tmp) original = tmp.text; }
    void Update()
    {
        if (!tmp) return; t += Time.deltaTime; if (t > scrambleEverySeconds)
        {
            t = 0; var sb = new StringBuilder(original);
            for (int i = 0; i < sb.Length; i++) if (Random.value < chancePerChar) sb[i] = Sub(sb[i]); tmp.text = sb.ToString();
        }
    }
}