using UnityEngine;

public enum PowerUpType
{
    Haste,
    Strength,
    Shield
}

/// <summary>Tempo restante dos power-ups ativos (logica pura, sem dependencia de cena).</summary>
public sealed class PowerUpState
{
    public const int TypeCount = 3;

    private readonly float[] remaining = new float[TypeCount];

    /// <summary>Ativa o power-up; pegar o mesmo de novo reinicia a duracao (nao soma).</summary>
    public void Activate(PowerUpType type, float seconds)
    {
        int index = (int)type;
        remaining[index] = Mathf.Max(remaining[index], seconds);
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] > 0f)
            {
                remaining[i] = Mathf.Max(0f, remaining[i] - deltaTime);
            }
        }
    }

    public bool IsActive(PowerUpType type)
    {
        return remaining[(int)type] > 0f;
    }

    public float Remaining(PowerUpType type)
    {
        return remaining[(int)type];
    }

    public void Clear()
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            remaining[i] = 0f;
        }
    }

    public static string Label(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Haste:
                return "Velocidade";
            case PowerUpType.Strength:
                return "Força";
            default:
                return "Escudo";
        }
    }

    public static Color Tint(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Haste:
                return new Color(1f, 0.95f, 0.45f, 1f);
            case PowerUpType.Strength:
                return new Color(1f, 0.55f, 0.2f, 1f);
            default:
                return new Color(0.45f, 0.75f, 1f, 1f);
        }
    }
}
