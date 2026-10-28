using UnityEngine;

/// <summary>Acompanha pontuacao, combo, tempo e estrelas da partida.</summary>
public class ScoreTracker : MonoBehaviour
{
    private const float BossParBonusSeconds = 30f;

    private StageManager stage;
    private PlayerController player;
    private float stageTimer;
    private int stageDamage;
    private bool finalized;

    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int TotalStars { get; private set; }
    public float Elapsed { get; private set; }
    public bool NewRecord { get; private set; }
    public int Rank { get; private set; }
    public int WavesCleared { get; private set; }
    public int HighScore { get; private set; }

    public void Configure(StageManager stageManager, PlayerController playerController)
    {
        stage = stageManager;
        player = playerController;
        HighScore = GameProgress.HighScore;

        stage.ZombieKilled += OnZombieKilled;
        stage.StageBegan += OnStageBegan;
        stage.StageCompleted += OnStageCompleted;
        stage.StateChanged += OnStateChanged;
        player.Health.Damaged += OnPlayerDamaged;
    }

    private void OnDestroy()
    {
        if (stage != null)
        {
            stage.ZombieKilled -= OnZombieKilled;
            stage.StageBegan -= OnStageBegan;
            stage.StageCompleted -= OnStageCompleted;
            stage.StateChanged -= OnStateChanged;
        }

        if (player != null && player.Health != null)
        {
            player.Health.Damaged -= OnPlayerDamaged;
        }
    }

    private void Update()
    {
        if (stage == null || stage.State != GameState.Playing)
        {
            return;
        }

        Elapsed += Time.deltaTime;
        if (!stage.StageCleared)
        {
            stageTimer += Time.deltaTime;
        }
    }

    private void OnZombieKilled(ZombieController zombie)
    {
        Kills++;
        Combo++;
        BestCombo = Mathf.Max(BestCombo, Combo);
        Score += DifficultyProfile.Current.ScaleScore(ScoreCalculator.KillPoints(Combo, zombie.Variant.ScoreMultiplier));
    }

    private void OnPlayerDamaged(int amount)
    {
        Combo = 0;
        stageDamage++;
    }

    private void OnStageBegan(int stageIndex)
    {
        stageTimer = 0f;
        stageDamage = 0;
    }

    private void OnStageCompleted(int stageIndex)
    {
        // Modo infinito: cada onda vencida da bonus; nao ha estrelas nem fases a liberar.
        if (stage.IsEndless)
        {
            WavesCleared++;
            Score += DifficultyProfile.Current.ScaleScore(EndlessWaves.WaveBonus(stageIndex));
            return;
        }

        float par = ScoreCalculator.ParTime(stage.ZombiesTotal) + (stage.IsBossStage ? BossParBonusSeconds : 0f);
        int stars = ScoreCalculator.Stars(stageDamage, stageTimer, par);
        Score += DifficultyProfile.Current.ScaleScore(ScoreCalculator.TimeBonus(stageTimer, par));
        TotalStars += stars;
        GameProgress.SubmitStageStars(stageIndex, stars);
        GameProgress.UnlockStage(stageIndex + 1);
        GameProgress.Save();
    }

    private void OnStateChanged(GameState state)
    {
        if (finalized || state == GameState.Playing)
        {
            return;
        }

        finalized = true;
        if (state == GameState.Victory)
        {
            Score += DifficultyProfile.Current.ScaleScore(ScoreCalculator.HealthBonus(player.Health.Current));
        }

        Rank = GameProgress.RegisterScore(Score, stage.IsEndless ? GameMode.Endless : GameMode.Campaign);
        NewRecord = Rank == 1;
        HighScore = GameProgress.HighScore;
        GameProgress.Save();
    }
}
