using UnityEngine;

/// <summary>Progresso e preferencias salvos em PlayerPrefs.</summary>
public static class GameProgress
{
    private const string DefaultPrefix = "GameCM.";

    /// <summary>Prefixo das chaves de PlayerPrefs; os testes trocam por outro para nao apagar o progresso real.</summary>
    public static string KeyPrefix { get; set; } = DefaultPrefix;

    private static string HighScoreKey => KeyPrefix + "HighScore";
    private static string UnlockedStageKey => KeyPrefix + "UnlockedStage";
    private static string VolumeKey => KeyPrefix + "Volume";
    private static string FullscreenKey => KeyPrefix + "Fullscreen";
    private static string DifficultyKey => KeyPrefix + "Difficulty";

    public static int HighScore
    {
        get => PlayerPrefs.GetInt(HighScoreKey, 0);
        set => PlayerPrefs.SetInt(HighScoreKey, Mathf.Max(0, value));
    }

    /// <summary>Maior indice de fase liberado (0 = so a primeira).</summary>
    public static int UnlockedStage => Mathf.Max(0, PlayerPrefs.GetInt(UnlockedStageKey, 0));

    public static float Volume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
    }

    public static Difficulty Difficulty
    {
        get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, (int)Difficulty.Normal), 0, 2);
        set => PlayerPrefs.SetInt(DifficultyKey, Mathf.Clamp((int)value, 0, 2));
    }

    public static bool HasFullscreenPreference => PlayerPrefs.HasKey(FullscreenKey);

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        set => PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
    }

    public static void UnlockStage(int stageIndex)
    {
        if (stageIndex > UnlockedStage)
        {
            PlayerPrefs.SetInt(UnlockedStageKey, stageIndex);
        }
    }

    /// <summary>Registra a pontuacao e devolve true se for um novo recorde.</summary>
    public static bool SubmitScore(int score)
    {
        if (score <= HighScore)
        {
            return false;
        }

        HighScore = score;
        return true;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.DeleteKey(UnlockedStageKey);
        PlayerPrefs.DeleteKey(VolumeKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(DifficultyKey);
    }
}
