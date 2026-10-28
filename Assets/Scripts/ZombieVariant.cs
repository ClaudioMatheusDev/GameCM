using UnityEngine;

public enum ZombieType
{
    Normal,
    Fast,
    Tank,
    Boss
}

/// <summary>Valores de cada tipo de zumbi, relativos ao <see cref="ZombieSettings"/> base.</summary>
public sealed class ZombieVariant
{
    public ZombieType Type;
    public float HealthMultiplier = 1f;
    public float SpeedMultiplier = 1f;
    public float ScaleMultiplier = 1f;
    public float DamageMultiplier = 1f;
    public float KnockbackResistance;
    public float StaggerMultiplier = 1f;
    public float ScoreMultiplier = 1f;
    public Color Tint = Color.white;
    public float DropChance = 0.12f;
    public int HealAmount = 1;
    public bool Charges;

    private static readonly ZombieVariant NormalVariant = new ZombieVariant { Type = ZombieType.Normal };

    private static readonly ZombieVariant FastVariant = new ZombieVariant
    {
        Type = ZombieType.Fast,
        HealthMultiplier = 0.5f,
        SpeedMultiplier = 2f,
        ScaleMultiplier = 0.85f,
        ScoreMultiplier = 1.5f,
        DropChance = 0.1f,
        Tint = new Color(0.75f, 1f, 0.75f, 1f)
    };

    private static readonly ZombieVariant TankVariant = new ZombieVariant
    {
        Type = ZombieType.Tank,
        HealthMultiplier = 2.5f,
        SpeedMultiplier = 0.6f,
        ScaleMultiplier = 1.3f,
        DamageMultiplier = 2f,
        KnockbackResistance = 0.5f,
        StaggerMultiplier = 0.6f,
        ScoreMultiplier = 2.5f,
        DropChance = 0.4f,
        HealAmount = 2,
        Tint = new Color(0.8f, 0.7f, 1f, 1f)
    };

    private static readonly ZombieVariant BossVariant = new ZombieVariant
    {
        Type = ZombieType.Boss,
        HealthMultiplier = 10f,
        SpeedMultiplier = 0.9f,
        ScaleMultiplier = 2.2f,
        DamageMultiplier = 2f,
        KnockbackResistance = 0.9f,
        StaggerMultiplier = 0.3f,
        ScoreMultiplier = 10f,
        DropChance = 0f,
        Tint = new Color(1f, 0.65f, 0.65f, 1f),
        Charges = true
    };

    public static ZombieVariant Get(ZombieType type)
    {
        switch (type)
        {
            case ZombieType.Fast:
                return FastVariant;
            case ZombieType.Tank:
                return TankVariant;
            case ZombieType.Boss:
                return BossVariant;
            default:
                return NormalVariant;
        }
    }
}

/// <summary>Sorteio do tipo de zumbi por fase (o chefe nunca sai daqui; ele e gerado a parte).</summary>
public static class ZombieSpawnTable
{
    /// <param name="roll">Valor sorteado entre 0 (inclusive) e 1 (exclusivo).</param>
    public static ZombieType Roll(int stageIndex, float roll)
    {
        float fast;
        float tank;
        switch (Mathf.Max(0, stageIndex))
        {
            case 0:
                fast = 0f;
                tank = 0f;
                break;
            case 1:
                fast = 0.25f;
                tank = 0f;
                break;
            case 2:
                fast = 0.3f;
                tank = 0.2f;
                break;
            default:
                fast = 0.3f;
                tank = 0.3f;
                break;
        }

        if (roll < fast)
        {
            return ZombieType.Fast;
        }

        return roll < fast + tank ? ZombieType.Tank : ZombieType.Normal;
    }
}
