using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Campaign,
    Endless
}

/// <summary>Estado que sobrevive ao recarregamento da cena (fase inicial e se o menu deve aparecer).</summary>
public static class GameSession
{
    public static int StartStage { get; private set; }
    public static bool SkipMenu { get; private set; }
    public static GameMode Mode { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        StartStage = 0;
        SkipMenu = false;
        Mode = GameMode.Campaign;
    }

    public static void Restart(int stageIndex, bool showMenu)
    {
        Restart(stageIndex, showMenu, Mode);
    }

    public static void Restart(int stageIndex, bool showMenu, GameMode mode)
    {
        Mode = mode;
        StartStage = Mathf.Max(0, stageIndex);
        SkipMenu = !showMenu;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
