using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Playing,
    Victory,
    GameOver
}

[Serializable]
public class StageSettings
{
    public int minZombies = 2;
    public int maxZombies = 3;
    public int extraZombiesPerStage = 1;
    public float firstSpawnDelayMin = 0.4f;
    public float firstSpawnDelayMax = 0.9f;
    public float spawnIntervalMin = 0.6f;
    public float spawnIntervalMax = 1.1f;
    public float spawnMinDistance = 3f;
    public float spawnMargin = 1f;
    public float zombieHeightRatio = 0.65f;
    public float backgroundZoom = 1f;
    public bool bossOnLastStage = true;

    [Header("Cenario com rolagem")]
    public bool scrolling = true;
    public float stageHalfWidth = 22f;
    public float cameraMargin = 2f;
    public float offscreenSpawnMargin = 1.5f;
    public float scrollZombieSpeedScale = 1.8f;

    [Header("Modo infinito")]
    public float intermissionSeconds = 3f;
}

/// <summary>Controla fases ou ondas, fundos, geracao (com pool) de zumbis, itens e o estado geral da partida.</summary>
public class StageManager : MonoBehaviour
{
    private const float EdgeTolerance = 0.05f;
    private const int LowHealthThreshold = 2;
    private const float FadeSeconds = 0.3f;
    private const int MaxZombieSortingOffset = 24;
    private const float BackgroundCoverBuffer = 1f;

    private SpriteRenderer background;
    private ScrollingBackground scrollingBackground;
    private Sprite[] backgrounds;
    private Camera stageCamera;
    private PlayerController player;
    private ZombieFrames frames;
    private ZombieSettings zombieSettings;
    private StageSettings settings;
    private GameMode mode;
    private float startX;
    private float endX;
    private float groundY;
    private float zombieScale;
    private bool configured;

    private int zombieTarget;
    private int zombiesSpawned;
    private int zombiesKilled;
    private float spawnTimer;
    private float intermissionTimer;
    private bool bossPending;
    private bool transitioning;
    private int zombiesCreated;
    private readonly List<HealthPickup> healthPickups = new List<HealthPickup>();
    private readonly List<PowerUpPickup> powerUpPickups = new List<PowerUpPickup>();

    private readonly List<ZombieController> active = new List<ZombieController>();
    private readonly Stack<ZombieController> pool = new Stack<ZombieController>();

    public event Action<GameState> StateChanged;
    public event Action<int> StageBegan;
    public event Action<int> StageCompleted;
    public event Action<ZombieController> ZombieKilled;
    public event Action BossAppeared;

    public ScreenFader Fader { get; set; }
    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>Indice da fase (campanha) ou da onda (modo infinito), sempre a partir de 0.</summary>
    public int StageIndex { get; private set; }

    public int StageCount => backgrounds != null ? backgrounds.Length : 0;
    public bool IsEndless => mode == GameMode.Endless;
    public int WaveNumber => StageIndex + 1;
    public ZombieController ActiveBoss { get; private set; }

    public bool IsBossStage => IsEndless
        ? EndlessWaves.HasBoss(StageIndex)
        : StageCount > 0 && StageIndex == StageCount - 1 && settings.bossOnLastStage;

    public int ZombiesTotal => zombieTarget;
    public int ZombiesRemaining => Mathf.Max(0, zombieTarget - zombiesKilled);
    public bool StageCleared => zombiesSpawned >= zombieTarget && zombiesKilled >= zombieTarget;

    public void Configure(
        SpriteRenderer backgroundRenderer,
        Sprite[] stageBackgrounds,
        Camera camera,
        PlayerController playerController,
        ZombieFrames zombieFrames,
        ZombieSettings zombieConfig,
        StageSettings stageConfig,
        float stageStartX,
        float stageEndX,
        int startStageIndex)
    {
        background = backgroundRenderer;
        scrollingBackground = new ScrollingBackground(background);
        backgrounds = stageBackgrounds;
        stageCamera = camera;
        player = playerController;
        frames = zombieFrames;
        zombieSettings = zombieConfig;
        settings = stageConfig;
        mode = GameSession.Mode;
        startX = stageStartX;
        endX = stageEndX;
        groundY = player.transform.position.y;

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        zombieScale = frames.Walk != null && frames.Walk.Length > 0
            ? playerRenderer.bounds.size.y * settings.zombieHeightRatio / frames.Walk[0].bounds.size.y
            : 1f;

        DifficultyProfile.Refresh();
        ImpactFx.ScreenEffectsEnabled = GameProgress.ScreenEffects;
        player.Died += OnPlayerDied;

        StageIndex = IsEndless ? 0 : Mathf.Clamp(startStageIndex, 0, backgrounds.Length - 1);
        SetBackground(backgrounds[BackgroundFor(StageIndex)]);
        BeginStage();
        configured = true;
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.Died -= OnPlayerDied;
        }
    }

    private void Update()
    {
        if (!configured || State != GameState.Playing)
        {
            return;
        }

        UpdateSpawning(Time.deltaTime);

        if (IsEndless)
        {
            UpdateEndless(Time.deltaTime);
            return;
        }

        bool atExit = player.transform.position.x >= endX - EdgeTolerance;
        if (StageCleared && atExit && !transitioning)
        {
            if (StageIndex < StageCount - 1)
            {
                StartCoroutine(AdvanceRoutine());
            }
            else
            {
                SetState(GameState.Victory);
                player.InputLocked = true;
            }
        }
    }

    private void UpdateEndless(float deltaTime)
    {
        if (!StageCleared)
        {
            return;
        }

        intermissionTimer -= deltaTime;
        if (intermissionTimer <= 0f)
        {
            NextWave();
        }
    }

    private void NextWave()
    {
        int previousBackground = BackgroundFor(StageIndex);
        StageIndex++;
        int nextBackground = BackgroundFor(StageIndex);
        if (nextBackground != previousBackground)
        {
            SetBackground(backgrounds[nextBackground]);
        }

        BeginStage();
    }

    private IEnumerator AdvanceRoutine()
    {
        transitioning = true;
        player.InputLocked = true;
        if (Fader != null)
        {
            yield return Fader.FadeTo(1f, FadeSeconds);
        }

        AdvanceStage();
        if (Fader != null)
        {
            yield return Fader.FadeTo(0f, FadeSeconds);
        }

        transitioning = false;
        if (State == GameState.Playing && Time.timeScale > 0f)
        {
            player.InputLocked = false;
        }
    }

    private void AdvanceStage()
    {
        StageIndex++;
        SetBackground(backgrounds[BackgroundFor(StageIndex)]);
        ClearZombies();
        ClearPickups();
        BeginStage();
        player.ResetToStageStart();
        if (CameraRig.Instance != null)
        {
            CameraRig.Instance.Snap();
        }
    }

    private int BackgroundFor(int index)
    {
        return IsEndless
            ? EndlessWaves.BackgroundIndex(index, backgrounds.Length)
            : Mathf.Clamp(index, 0, backgrounds.Length - 1);
    }

    private void BeginStage()
    {
        bool hasFrames = frames.Walk != null && frames.Walk.Length > 0;
        if (IsEndless)
        {
            zombieTarget = hasFrames ? DifficultyProfile.Current.ScaleCount(EndlessWaves.ZombieCount(StageIndex)) : 0;
        }
        else
        {
            int baseCount = DifficultyProfile.Current.ScaleCount(UnityEngine.Random.Range(settings.minZombies, settings.maxZombies + 1));
            zombieTarget = hasFrames ? baseCount + StageIndex * settings.extraZombiesPerStage : 0;
        }

        bossPending = IsBossStage && zombieTarget > 0;
        if (bossPending)
        {
            zombieTarget++;
        }

        ActiveBoss = null;
        zombiesSpawned = 0;
        zombiesKilled = 0;
        intermissionTimer = 0f;
        spawnTimer = UnityEngine.Random.Range(settings.firstSpawnDelayMin, settings.firstSpawnDelayMax);
        StageBegan?.Invoke(StageIndex);
    }

    private void UpdateSpawning(float deltaTime)
    {
        if (zombiesSpawned >= zombieTarget)
        {
            return;
        }

        spawnTimer -= deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnZombie();
        zombiesSpawned++;
        spawnTimer = UnityEngine.Random.Range(settings.spawnIntervalMin, settings.spawnIntervalMax);
    }

    private void SpawnZombie()
    {
        ZombieController zombie = pool.Count > 0 ? pool.Pop() : CreateZombie();
        bool isBoss = bossPending && zombiesSpawned == zombieTarget - 1;
        int tier = IsEndless ? EndlessWaves.SpawnTier(StageIndex) : StageIndex;
        ZombieType type = isBoss ? ZombieType.Boss : ZombieSpawnTable.Roll(tier, UnityEngine.Random.value);
        float healthScale = IsEndless ? EndlessWaves.HealthScale(StageIndex) : 1f;
        float speedScale = settings.scrolling ? settings.scrollZombieSpeedScale : 1f;

        Vector3 position = new Vector3(PickSpawnX(), groundY, player.transform.position.z);
        zombie.Setup(
            frames,
            zombieSettings,
            ZombieVariant.Get(type),
            player,
            position,
            zombieScale,
            startX,
            endX,
            healthScale,
            speedScale);
        active.Add(zombie);

        if (isBoss)
        {
            ActiveBoss = zombie;
            SfxPlayer.Play(Sfx.BossRoar);
            ImpactFx.Play(Fx.BossCharge);
            BossAppeared?.Invoke();
        }
    }

    private ZombieController CreateZombie()
    {
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        GameObject zombieObject = new GameObject("Zombie");
        SpriteRenderer zombieRenderer = zombieObject.AddComponent<SpriteRenderer>();
        zombieRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        // Ordens distintas evitam que zumbis sobrepostos pisquem um sobre o outro.
        zombieRenderer.sortingOrder = playerRenderer.sortingOrder + 1 + Mathf.Min(zombiesCreated, MaxZombieSortingOffset);
        zombiesCreated++;

        ZombieController zombie = zombieObject.AddComponent<ZombieController>();
        zombie.Died += OnZombieDied;
        zombie.Finished += OnZombieFinished;
        return zombie;
    }

    private float PickSpawnX()
    {
        float playerX = player.transform.position.x;
        float low = startX + settings.spawnMargin;
        float high = endX - settings.spawnMargin;

        // Com rolagem, os zumbis entram pelas bordas da tela (fora da visao) e vao ate o jogador.
        if (settings.scrolling && CameraRig.Instance != null)
        {
            float edge = CameraRig.Instance.HalfWidth + settings.offscreenSpawnMargin;
            float center = CameraRig.Instance.CenterX;
            bool left = UnityEngine.Random.value < 0.5f;
            float x = center + (left ? -edge : edge);
            if (x < low || x > high)
            {
                x = center + (left ? edge : -edge);
            }

            return Mathf.Clamp(x, low, high);
        }

        float leftMax = Mathf.Min(high, playerX - settings.spawnMinDistance);
        float rightMin = Mathf.Max(low, playerX + settings.spawnMinDistance);
        bool canSpawnLeft = leftMax > low;
        bool canSpawnRight = rightMin < high;

        if (canSpawnLeft && canSpawnRight)
        {
            return UnityEngine.Random.value < 0.5f
                ? UnityEngine.Random.Range(low, leftMax)
                : UnityEngine.Random.Range(rightMin, high);
        }

        if (canSpawnRight)
        {
            return UnityEngine.Random.Range(rightMin, high);
        }

        if (canSpawnLeft)
        {
            return UnityEngine.Random.Range(low, leftMax);
        }

        return playerX < (startX + endX) * 0.5f ? high : low;
    }

    private void OnZombieDied(ZombieController zombie)
    {
        zombiesKilled++;
        if (zombie == ActiveBoss)
        {
            ActiveBoss = null;
        }

        TryDropPickup(zombie);
        ZombieKilled?.Invoke(zombie);
        if (StageCleared)
        {
            intermissionTimer = settings.intermissionSeconds;
            StageCompleted?.Invoke(StageIndex);
        }
    }

    private void OnZombieFinished(ZombieController zombie)
    {
        if (active.Remove(zombie))
        {
            pool.Push(zombie);
        }
    }

    private void TryDropPickup(ZombieController zombie)
    {
        ZombieVariant variant = zombie.Variant;
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        int layer = playerRenderer.sortingLayerID;
        int order = playerRenderer.sortingOrder - 1;

        // Com pouca vida a chance de cura dobra.
        float healChance = variant.DropChance * (player.Health.Current <= LowHealthThreshold ? 2f : 1f);
        if (variant.DropChance > 0f && UnityEngine.Random.value < healChance)
        {
            HealthPickup pickup = HealthPickup.Create(zombie.FeetPosition, player, variant.HealAmount, layer, order);
            pickup.Removed += OnHealthPickupRemoved;
            healthPickups.Add(pickup);
            return;
        }

        if (variant.PowerUpChance > 0f && UnityEngine.Random.value < variant.PowerUpChance)
        {
            PowerUpType type = (PowerUpType)UnityEngine.Random.Range(0, PowerUpState.TypeCount);
            PowerUpPickup pickup = PowerUpPickup.Create(zombie.FeetPosition, player, type, layer, order);
            pickup.Removed += OnPowerUpPickupRemoved;
            powerUpPickups.Add(pickup);
        }
    }

    private void OnHealthPickupRemoved(HealthPickup pickup)
    {
        healthPickups.Remove(pickup);
    }

    private void OnPowerUpPickupRemoved(PowerUpPickup pickup)
    {
        powerUpPickups.Remove(pickup);
    }

    private void ClearPickups()
    {
        for (int i = healthPickups.Count - 1; i >= 0; i--)
        {
            healthPickups[i].Removed -= OnHealthPickupRemoved;
            Destroy(healthPickups[i].gameObject);
        }

        healthPickups.Clear();

        for (int i = powerUpPickups.Count - 1; i >= 0; i--)
        {
            powerUpPickups[i].Removed -= OnPowerUpPickupRemoved;
            Destroy(powerUpPickups[i].gameObject);
        }

        powerUpPickups.Clear();
    }

    private void ClearZombies()
    {
        for (int i = 0; i < active.Count; i++)
        {
            active[i].gameObject.SetActive(false);
            pool.Push(active[i]);
        }

        active.Clear();
        ActiveBoss = null;
    }

    private void OnPlayerDied()
    {
        SetState(GameState.GameOver);
    }

    private void SetState(GameState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }

    private void SetBackground(Sprite sprite)
    {
        float targetHeight = stageCamera.orthographicSize * 2f * settings.backgroundZoom;
        float scale = targetHeight / sprite.bounds.size.y;

        // Cenario com rolagem: cobre toda a area visivel pela camera; senao, so a largura da tela.
        float coverMin;
        float coverMax;
        if (settings.scrolling)
        {
            coverMin = startX - settings.cameraMargin - BackgroundCoverBuffer;
            coverMax = endX + settings.cameraMargin + BackgroundCoverBuffer;
        }
        else
        {
            float half = stageCamera.orthographicSize * stageCamera.aspect + BackgroundCoverBuffer;
            coverMin = -half;
            coverMax = half;
        }

        scrollingBackground.Apply(sprite, scale, coverMin, coverMax);
    }
}
