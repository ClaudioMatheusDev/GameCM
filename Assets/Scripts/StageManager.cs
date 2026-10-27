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
    private float zombieScale;
    private bool configured;

    private int zombieTarget;
    private int zombiesSpawned;
    private int zombiesKilled;
    private float spawnTimer;

    private readonly List<ZombieController> active = new List<ZombieController>();
    private readonly Stack<ZombieController> pool = new Stack<ZombieController>();

    public event Action<GameState> StateChanged;
    public event Action<int> StageBegan;
    public event Action<int> StageCompleted;
    public event Action ZombieKilled;

    public GameState State { get; private set; } = GameState.Playing;
    public int StageIndex { get; private set; }
    public int StageCount => backgrounds != null ? backgrounds.Length : 0;
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

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
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
        BeginStage();
        player.ResetToStageStart();
    }

    private void BeginStage()
    {
        int baseCount = UnityEngine.Random.Range(settings.minZombies, settings.maxZombies + 1);
        zombieTarget = frames.Walk != null && frames.Walk.Length > 0
            ? baseCount + StageIndex * settings.extraZombiesPerStage
            : 0;
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
        Vector3 position = new Vector3(PickSpawnX(), groundY, player.transform.position.z);
        zombie.Setup(frames, zombieSettings, player, position, zombieScale, startX, endX);
        active.Add(zombie);
    }

    private ZombieController CreateZombie()
    {
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        GameObject zombieObject = new GameObject("Zombie");
        SpriteRenderer zombieRenderer = zombieObject.AddComponent<SpriteRenderer>();
        zombieRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        zombieRenderer.sortingOrder = playerRenderer.sortingOrder + 1;

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
        ZombieKilled?.Invoke();
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

    private void ClearZombies()
    {
        for (int i = 0; i < active.Count; i++)
        {
            active[i].gameObject.SetActive(false);
            pool.Push(active[i]);
        }

        active.Clear();
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
