using UnityEngine;

public static class EndlessWaves
{
    public const int BossEvery = 5;
    public const int WavesPerBackground = 3;
    public const int MaxZombies = 18;

    public static int ZombieCount(int wave)
    {
        return Mathf.Min(3 + Mathf.Max(0, wave) * 2, MaxZombies);
    }

    public static bool HasBoss(int wave)
    {
        return wave > 0 && (wave + 1) % BossEvery == 0;
    }

    public static float HealthScale(int wave)
    {
        return 1f + Mathf.Min(Mathf.Max(0, wave), 20) * 0.08f;
    }

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
