using UnityEngine;

/// <summary>Regras das ondas do modo infinito (logica pura, testavel). Ondas comecam em 0.</summary>
public static class EndlessWaves
{
    public const int BossEvery = 5;
    public const int WavesPerBackground = 3;
    public const int MaxZombies = 18;

    public static int ZombieCount(int wave)
    {
        return Mathf.Min(3 + Mathf.Max(0, wave) * 2, MaxZombies);
    }

    /// <summary>Chefe a cada 5 ondas (5a, 10a...).</summary>
    public static bool HasBoss(int wave)
    {
        return wave > 0 && (wave + 1) % BossEvery == 0;
    }

    /// <summary>Multiplicador de vida dos zumbis; cresce devagar e para na onda 20.</summary>
    public static float HealthScale(int wave)
    {
        return 1f + Mathf.Min(Mathf.Max(0, wave), 20) * 0.08f;
    }

    /// <summary>Nivel (0 a 3) usado no sorteio de tipos de zumbi.</summary>
    public static int SpawnTier(int wave)
    {
        return Mathf.Clamp(wave / 2, 0, 3);
    }

    public static int BackgroundIndex(int wave, int backgroundCount)
    {
        if (backgroundCount <= 0)
        {
            return 0;
        }

        return Mathf.Max(0, wave) / WavesPerBackground % backgroundCount;
    }

    public static int WaveBonus(int wave)
    {
        return 200 * (Mathf.Max(0, wave) + 1);
    }
}
