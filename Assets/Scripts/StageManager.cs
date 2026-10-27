using System;
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
}

/// <summary>Controla fases, fundos, geracao (com pool) de zumbis e o estado geral da partida.</summary>
public class StageManager : MonoBehaviour
{
    private const float EdgeTolerance = 0.05f;

    private SpriteRenderer background;
    private Sprite[] backgrounds;
    private Camera stageCamera;
    private PlayerController player;
    private ZombieFrames frames;
    private ZombieSettings zombieSettings;
    private StageSettings settings;
    private float startX;
    private float endX;
    private float groundY;
    private float groundFeetY;
    private float zombieScale;
    private bool configured;

    private int zombieTarget;
    private int zombiesSpawned;
    private int zombiesKilled;
    private float spawnTimer;
    private bool bossPending;
    private DifficultyRules rules;
    private SpriteRenderer playerRenderer;

    private readonly List<ZombieController> active = new List<ZombieController>();
    private readonly Stack<ZombieController> pool = new Stack<ZombieController>();
    private readonly List<HealthPickup> pickups = new List<HealthPickup>();

    public event Action<GameState> StateChanged;
    public event Action<int> StageBegan;
    public event Action<int> StageCompleted;
    public event Action<ZombieController> ZombieKilled;

    public GameState State { get; private set; } = GameState.Playing;
    public int StageIndex { get; private set; }
    public int StageCount => backgrounds != null ? backgrounds.Length : 0;
    public ZombieController ActiveBoss { get; private set; }
    public bool IsBossStage => StageCount > 0 && StageIndex == StageCount - 1 && settings.bossOnLastStage;
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
        backgrounds = stageBackgrounds;
        stageCamera = camera;
        player = playerController;
        frames = zombieFrames;
        zombieSettings = zombieConfig;
        settings = stageConfig;
        startX = stageStartX;
        endX = stageEndX;
        groundY = player.transform.position.y;

        rules = DifficultyRules.Get(GameProgress.Difficulty);
        playerRenderer = player.GetComponent<SpriteRenderer>();
        groundFeetY = playerRenderer.bounds.min.y;
        zombieScale = frames.Walk != null && frames.Walk.Length > 0
            ? playerRenderer.bounds.size.y * settings.zombieHeightRatio / frames.Walk[0].bounds.size.y
            : 1f;

        player.Died += OnPlayerDied;
        StageIndex = Mathf.Clamp(startStageIndex, 0, backgrounds.Length - 1);
        SetBackground(backgrounds[StageIndex]);
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

        bool atExit = player.transform.position.x >= endX - EdgeTolerance;
        if (StageCleared && atExit)
        {
            if (StageIndex < StageCount - 1)
            {
                AdvanceStage();
            }
            else
            {
                SetState(GameState.Victory);
                player.InputLocked = true;
            }
        }
    }

    private void AdvanceStage()
    {
        StageIndex++;
        SetBackground(backgrounds[StageIndex]);
        ClearZombies();
        ClearPickups();
        BeginStage();
        player.ResetToStageStart();
    }

    private void BeginStage()
    {
        int baseCount = UnityEngine.Random.Range(settings.minZombies, settings.maxZombies + 1);
        zombieTarget = frames.Walk != null && frames.Walk.Length > 0
            ? rules.ScaleZombieCount(baseCount + StageIndex * settings.extraZombiesPerStage)
            : 0;
        bossPending = IsBossStage && zombieTarget > 0;
        if (bossPending)
        {
            zombieTarget++;
        }

        ActiveBoss = null;
        zombiesSpawned = 0;
        zombiesKilled = 0;
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
        ZombieType type = isBoss ? ZombieType.Boss : ZombieSpawnTable.Roll(StageIndex, UnityEngine.Random.value);
        Vector3 position = new Vector3(PickSpawnX(), groundY, player.transform.position.z);
        zombie.Setup(frames, zombieSettings, ZombieVariant.Get(type), player, position, zombieScale, startX, endX, active, rules);
        AlignFeetToGround(zombie);
        active.Add(zombie);

        if (isBoss)
        {
            ActiveBoss = zombie;
            SfxPlayer.Play(Sfx.BossRoar);
            ImpactFx.Play(Fx.BossCharge);
        }
    }

    /// <summary>Os pivos dos sprites diferem; alinha os pes do zumbi aos do jogador para nao parecer mais ao fundo.</summary>
    private void AlignFeetToGround(ZombieController zombie)
    {
        Bounds bounds = zombie.GetComponent<SpriteRenderer>().bounds;
        Vector3 position = zombie.transform.position;
        position.y += groundFeetY - bounds.min.y;
        zombie.transform.position = position;
    }

    private ZombieController CreateZombie()
    {
        GameObject zombieObject = new GameObject("Zombie");
        SpriteRenderer zombieRenderer = zombieObject.AddComponent<SpriteRenderer>();
        zombieRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        zombieRenderer.sortingOrder = player.BaseSortingOrder + 1;

        ZombieController zombie = zombieObject.AddComponent<ZombieController>();
        zombie.Died += OnZombieDied;
        zombie.Finished += OnZombieFinished;
        return zombie;
    }

    private float PickSpawnX()
    {
        return SpawnPlacement.Pick(
            player.transform.position.x,
            startX,
            endX,
            settings.spawnMargin,
            settings.spawnMinDistance,
            UnityEngine.Random.value,
            UnityEngine.Random.value);
    }

    private void OnZombieDied(ZombieController zombie)
    {
        // Zumbis ja limpos por uma troca de fase nao podem contar na fase nova.
        if (!active.Contains(zombie))
        {
            return;
        }

        zombiesKilled++;
        if (zombie == ActiveBoss)
        {
            ActiveBoss = null;
        }

        TryDropHealth(zombie);
        ZombieKilled?.Invoke(zombie);
        if (StageCleared)
        {
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

    private void TryDropHealth(ZombieController zombie)
    {
        if (player.IsDead || (!zombie.IsBoss && UnityEngine.Random.value >= rules.HealDropChance))
        {
            return;
        }

        Vector3 position = zombie.transform.position;
        position.y = groundFeetY;
        pickups.RemoveAll(pickup => pickup == null);
        pickups.Add(HealthPickup.Spawn(player, position, playerRenderer.sortingLayerID, player.BaseSortingOrder - 1, 1));
    }

    private void ClearPickups()
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            if (pickups[i] != null)
            {
                Destroy(pickups[i].gameObject);
            }
        }

        pickups.Clear();
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
        background.sprite = sprite;
        float targetHeight = stageCamera.orthographicSize * 2f * settings.backgroundZoom;
        float scale = targetHeight / sprite.bounds.size.y;
        Vector3 localScale = background.transform.localScale;
        localScale.x = scale;
        localScale.y = scale;
        background.transform.localScale = localScale;
    }
}

/// <summary>Escolha da posicao X de nascimento dos zumbis (logica pura, testavel).</summary>
public static class SpawnPlacement
{
    /// <param name="side">Sorteio 0..1 que decide o lado (esquerda/direita) quando ambos cabem.</param>
    /// <param name="t">Sorteio 0..1 que decide a posicao dentro da faixa escolhida.</param>
    public static float Pick(float playerX, float startX, float endX, float margin, float minDistance, float side, float t)
    {
        float low = startX + margin;
        float high = endX - margin;
        float leftMax = Mathf.Min(high, playerX - minDistance);
        float rightMin = Mathf.Max(low, playerX + minDistance);
        bool canSpawnLeft = leftMax > low;
        bool canSpawnRight = rightMin < high;

        if (canSpawnLeft && canSpawnRight)
        {
            return side < 0.5f ? Mathf.Lerp(low, leftMax, t) : Mathf.Lerp(rightMin, high, t);
        }

        if (canSpawnRight)
        {
            return Mathf.Lerp(rightMin, high, t);
        }

        if (canSpawnLeft)
        {
            return Mathf.Lerp(low, leftMax, t);
        }

        return playerX < (startX + endX) * 0.5f ? high : low;
    }
}
