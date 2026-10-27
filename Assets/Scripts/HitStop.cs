using UnityEngine;

/// <summary>Congela o jogo por instantes ao acertar golpes. Nao interfere quando o jogo esta pausado.</summary>
public class HitStop : MonoBehaviour
{
    private const float FrozenScale = 0.02f;

    private float endTime;
    private bool active;

    public static HitStop Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Cancel();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Freeze(float seconds)
    {
        // So congela com o jogo rodando normalmente (nao durante pausa/menus).
        if (!active && !Mathf.Approximately(Time.timeScale, 1f))
        {
            return;
        }

        endTime = Mathf.Max(endTime, Time.unscaledTime + seconds);
        active = true;
        Time.timeScale = FrozenScale;
    }

    /// <summary>Devolve o tempo ao normal; chamado antes de pausar ou trocar de tela.</summary>
    public void Cancel()
    {
        if (active)
        {
            active = false;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        if (!Mathf.Approximately(Time.timeScale, FrozenScale))
        {
            active = false;
            return;
        }

        if (Time.unscaledTime >= endTime)
        {
            Cancel();
        }
    }
}
