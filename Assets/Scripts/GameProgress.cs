using UnityEngine;

public static class GameProgress
{
    private const string DefaultPrefix = "GameCM.";

    public static string KeyPrefix { get; set; } = DefaultPrefix;

    private static string HighScoreKey => KeyPrefix + "HighScore";
    private static string UnlockedStageKey => KeyPrefix + "UnlockedStage";
    private static string VolumeKey => KeyPrefix + "Volume";
    private static string FullscreenKey => KeyPrefix + "Fullscreen";
    private static string EffectsKey => KeyPrefix + "ScreenEffects";
    private static string DifficultyKey => KeyPrefix + "Difficulty";
    private static string TopScoresKey => KeyPrefix + "TopScores";
    private static string StarsKey(int stageIndex) => KeyPrefix + "Stars." + stageIndex;

    private const int MaxStageSlots = 16;

    public static bool ScreenEffects
    {
        get => PlayerPrefs.GetInt(EffectsKey, 1) == 1;
        set => PlayerPrefs.SetInt(EffectsKey, value ? 1 : 0);
    }

    public static Difficulty Difficulty
    {
        get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, (int)Difficulty.Normal), 0, 2);
        set => PlayerPrefs.SetInt(DifficultyKey, (int)value);
    }

    private static string TopScoresKeyFor(GameMode mode)
    {
        return mode == GameMode.Endless ? TopScoresKey + ".Endless" : TopScoresKey;
    }

    public static int[] TopScores => TopScoresFor(GameMode.Campaign);

    public static int[] TopScoresFor(GameMode mode)
    {
        return ScoreBoard.Parse(PlayerPrefs.GetString(TopScoresKeyFor(mode), string.Empty));
    }

    public static int GetStageStars(int stageIndex)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(StarsKey(stageIndex), 0), 0, ScoreCalculator.MaxStars);
    }

    public static void SubmitStageStars(int stageIndex, int stars)
    {
        if (stars > GetStageStars(stageIndex))
        {
            PlayerPrefs.SetInt(StarsKey(stageIndex), Mathf.Min(stars, ScoreCalculator.MaxStars));
        }
    }

    public static int RegisterScore(int score)
    {
        return RegisterScore(score, GameMode.Campaign);
    }

    public static int RegisterScore(int score, GameMode mode)
    {
        int[] updated = ScoreBoard.Insert(TopScoresFor(mode), score, out int rank);
        PlayerPrefs.SetString(TopScoresKeyFor(mode), ScoreBoard.Serialize(updated));
        if (mode == GameMode.Campaign && updated.Length > 0)
        {
            HighScore = updated[0];
        }

        return rank;
    }

    public static int HighScore
    {
        get => PlayerPrefs.GetInt(HighScoreKey, 0);
        set => PlayerPrefs.SetInt(HighScoreKey, Mathf.Max(0, value));
    }

    public static int UnlockedStage => Mathf.Max(0, PlayerPrefs.GetInt(UnlockedStageKey, 0));

    public static float Volume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
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
        PlayerPrefs.DeleteKey(EffectsKey);
        PlayerPrefs.DeleteKey(DifficultyKey);
        PlayerPrefs.DeleteKey(TopScoresKey);
        PlayerPrefs.DeleteKey(TopScoresKey + ".Endless");
        for (int i = 0; i < MaxStageSlots; i++)
        {
            PlayerPrefs.DeleteKey(StarsKey(i));
        }
    }
}
