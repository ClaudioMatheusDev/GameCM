using System.Collections.Generic;
using System.Globalization;

/// <summary>Ranking local das melhores pontuacoes (logica pura, sem PlayerPrefs).</summary>
public static class ScoreBoard
{
    public const int MaxEntries = 5;

    public static int[] Parse(string raw)
    {
        List<int> scores = new List<int>();
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (string part in raw.Split(','))
            {
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0)
                {
                    scores.Add(value);
                }
            }
        }

        scores.Sort((a, b) => b.CompareTo(a));
        if (scores.Count > MaxEntries)
        {
            scores.RemoveRange(MaxEntries, scores.Count - MaxEntries);
        }

        return scores.ToArray();
    }

    public static string Serialize(int[] scores)
    {
        return string.Join(",", scores);
    }

    /// <summary>Insere a pontuacao; rank e a posicao (1 = melhor) ou 0 se nao entrou. Empates ficam abaixo.</summary>
    public static int[] Insert(int[] scores, int score, out int rank)
    {
        List<int> list = new List<int>(scores);
        rank = 0;
        if (score <= 0)
        {
            return list.ToArray();
        }

        int index = 0;
        while (index < list.Count && list[index] >= score)
        {
            index++;
        }

        if (index >= MaxEntries)
        {
            return list.ToArray();
        }

        list.Insert(index, score);
        if (list.Count > MaxEntries)
        {
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        }

        rank = index + 1;
        return list.ToArray();
    }
}
