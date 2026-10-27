using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Estado que sobrevive ao recarregamento da cena (fase inicial e se o menu deve aparecer).</summary>
public static class GameSession
{
    public static int StartStage { get; private set; }
    public static bool SkipMenu { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        StartStage = 0;
        SkipMenu = false;
    }

    public static void Restart(int stageIndex, bool showMenu)
    {
        StartStage = Mathf.Max(0, stageIndex);
        SkipMenu = !showMenu;
        GameClock.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
