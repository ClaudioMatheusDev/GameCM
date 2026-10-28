public enum Fx
{
    PunchHit,
    PowerHit,
    Kill,
    PlayerHurt,
    BossCharge,
    BossDeath
}

/// <summary>Ponto unico para os efeitos de impacto (tremor de camera e hit-stop).</summary>
public static class ImpactFx
{
    /// <summary>Lido de PlayerPrefs uma vez por partida e atualizado pelas opcoes.</summary>
    public static bool ScreenEffectsEnabled { get; set; } = true;

    public static void Play(Fx fx)
    {
        switch (fx)
        {
            case Fx.PunchHit:
                Apply(0.06f, 0.08f, 0.035f);
                break;
            case Fx.PowerHit:
                Apply(0.16f, 0.18f, 0.07f);
                break;
            case Fx.Kill:
                Apply(0.12f, 0.15f, 0.06f);
                break;
            case Fx.PlayerHurt:
                Apply(0.15f, 0.2f, 0.05f);
                break;
            case Fx.BossCharge:
                Apply(0.25f, 0.3f, 0f);
                break;
            case Fx.BossDeath:
                Apply(0.35f, 0.7f, 0.2f);
                break;
        }
    }

    private static void Apply(float shakeMagnitude, float shakeSeconds, float freezeSeconds)
    {
        if (!ScreenEffectsEnabled)
        {
            return;
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(shakeMagnitude, shakeSeconds);
        }

        if (freezeSeconds > 0f && HitStop.Instance != null)
        {
            HitStop.Instance.Freeze(freezeSeconds);
        }
    }
}
