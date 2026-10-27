using UnityEngine;

/// <summary>Unico dono do Time.timeScale: pausa (menus) tem prioridade sobre o hit-stop.</summary>
public static class GameClock
{
    private const float FrozenScale = 0.02f;

    private static bool paused;
    private static bool hitStop;

    public static bool Paused
    {
        get => paused;
        set
        {
            paused = value;
            Apply();
        }
    }

    public static bool HitStopActive
    {
        get => hitStop;
        set
        {
            hitStop = value;
            Apply();
        }
    }

    /// <summary>Volta ao tempo normal e limpa pausa e hit-stop.</summary>
    public static void Reset()
    {
        paused = false;
        hitStop = false;
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        paused = false;
        hitStop = false;
    }

    private static void Apply()
    {
        Time.timeScale = paused ? 0f : hitStop ? FrozenScale : 1f;
    }
}
