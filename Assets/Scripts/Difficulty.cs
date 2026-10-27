using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>Multiplicadores de cada dificuldade, aplicados sobre as configuracoes base.</summary>
public sealed class DifficultyRules
{
    public float ZombieHealth = 1f;
    public float ZombieDamage = 1f;
    public float ZombieCount = 1f;
    public int PlayerHealthBonus;

    /// <summary>Chance de um zumbi derrotado largar um item de cura (o chefe sempre larga).</summary>
    public float HealDropChance = 0.15f;

    private static readonly DifficultyRules EasyRules = new DifficultyRules
    {
        ZombieHealth = 0.7f,
        ZombieCount = 0.75f,
        PlayerHealthBonus = 2,
        HealDropChance = 0.3f
    };

    private static readonly DifficultyRules NormalRules = new DifficultyRules();

    private static readonly DifficultyRules HardRules = new DifficultyRules
    {
        ZombieHealth = 1.5f,
        ZombieDamage = 1.5f,
        ZombieCount = 1.3f,
        PlayerHealthBonus = -1,
        HealDropChance = 0.08f
    };

    public static DifficultyRules Get(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return EasyRules;
            case Difficulty.Hard:
                return HardRules;
            default:
                return NormalRules;
        }
    }

    public static string Label(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return "Facil";
            case Difficulty.Hard:
                return "Dificil";
            default:
                return "Normal";
        }
    }

    public static Difficulty Next(Difficulty difficulty)
    {
        return (Difficulty)(((int)difficulty + 1) % 3);
    }

    /// <summary>Quantidade de zumbis comuns da fase depois de aplicar a dificuldade (no minimo 1 se houver zumbis).</summary>
    public int ScaleZombieCount(int baseCount)
    {
        return baseCount <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseCount * ZombieCount));
    }
}
