using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>Ajustes de dificuldade aplicados no inicio de cada partida.</summary>
public sealed class DifficultyProfile
{
    public string Label;
    public int PlayerHealthDelta;
    public float ZombieSpeed = 1f;
    public float ZombieCount = 1f;
    public float ScoreMultiplier = 1f;

    private static readonly DifficultyProfile EasyProfile = new DifficultyProfile
    {
        Label = "Fácil",
        PlayerHealthDelta = 2,
        ZombieSpeed = 0.85f,
        ZombieCount = 0.8f,
        ScoreMultiplier = 0.75f
    };

    private static readonly DifficultyProfile NormalProfile = new DifficultyProfile { Label = "Normal" };

    private static readonly DifficultyProfile HardProfile = new DifficultyProfile
    {
        Label = "Difícil",
        PlayerHealthDelta = -1,
        ZombieSpeed = 1.2f,
        ZombieCount = 1.3f,
        ScoreMultiplier = 1.5f
    };

    public static DifficultyProfile Current { get; private set; } = NormalProfile;

    public static DifficultyProfile Get(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return EasyProfile;
            case Difficulty.Hard:
                return HardProfile;
            default:
                return NormalProfile;
        }
    }

    /// <summary>Le a dificuldade salva; chamado uma vez ao iniciar a cena.</summary>
    public static void Refresh()
    {
        Current = Get(GameProgress.Difficulty);
    }

    public int ScaleCount(int baseCount)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * ZombieCount));
    }

    public int ScaleScore(int points)
    {
        return Mathf.RoundToInt(points * ScoreMultiplier);
    }
}
