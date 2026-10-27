using UnityEngine;

/// <summary>Regras de pontuacao e estrelas, sem dependencia de cena (testavel).</summary>
public static class ScoreCalculator
{
    public const int BaseKillPoints = 100;
    public const int ComboStepPoints = 25;
    public const int MaxComboSteps = 8;
    public const int HealthBonusPerPoint = 200;
    public const int TimeBonusPerSecond = 10;

    /// <summary>Pontos por zumbi morto; combo 1 e o primeiro abate sem levar dano.</summary>
    public static int KillPoints(int combo)
    {
        return BaseKillPoints + ComboStepPoints * Mathf.Clamp(combo - 1, 0, MaxComboSteps);
    }

    public static float ParTime(int zombies)
    {
        return 15f + 12f * Mathf.Max(0, zombies);
    }

    public static int TimeBonus(float seconds, float parSeconds)
    {
        return Mathf.Max(0, Mathf.RoundToInt((parSeconds - seconds) * TimeBonusPerSecond));
    }

    /// <summary>1 estrela por concluir, +1 sem dano, +1 dentro do tempo-alvo.</summary>
    public static int Stars(int damageTaken, float seconds, float parSeconds)
    {
        return 1 + (damageTaken == 0 ? 1 : 0) + (seconds <= parSeconds ? 1 : 0);
    }

    public static int HealthBonus(int remainingHealth)
    {
        return Mathf.Max(0, remainingHealth) * HealthBonusPerPoint;
    }
}
