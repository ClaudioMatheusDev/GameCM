using UnityEngine;

/// <summary>Congela o jogo por instantes ao acertar golpes. Nao interfere quando o jogo esta pausado.</summary>
public class HitStop : MonoBehaviour
{
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
        // So congela com o jogo rodando (nao durante pausa/menus).
        if (GameClock.Paused)
        {
            return;
        }

        endTime = Mathf.Max(endTime, Time.unscaledTime + seconds);
        active = true;
        GameClock.HitStopActive = true;
    }

    /// <summary>Devolve o tempo ao normal; chamado antes de pausar ou trocar de tela.</summary>
    public void Cancel()
    {
        if (active)
        {
            active = false;
            GameClock.HitStopActive = false;
        }
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        if (GameClock.Paused || Time.unscaledTime >= endTime)
        {
            Cancel();
        }
    }
}
