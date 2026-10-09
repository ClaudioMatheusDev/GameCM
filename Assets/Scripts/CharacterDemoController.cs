using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterDemoController : MonoBehaviour
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private AudioSource themeSource = null;
    [SerializeField] private SpriteRenderer stageBackground = null;
    [SerializeField] private Sprite[] stageBackgrounds = null;
    [SerializeField] private Sprite[] zombieWalkFrames = null;
    [SerializeField] private float backgroundZoom = 1f;
    [SerializeField] private float zombieHeightRatio = 0.65f;
    [SerializeField] private string[] stateNames = { "Idle", "Walk", "Run", "Jump", "Punch", "Power", "Hurt", "Dead" };
    [SerializeField] private float horizontalSpeed = 5f;
    [SerializeField] private float stageStartX = -10.5f;
    [SerializeField] private float stageEndX = 10.5f;
    [SerializeField] private float jumpDuration = 0.55f;
    [SerializeField] private float jumpHeight = 1.15f;
    [SerializeField] private float punchDuration = 0.6f;
    [SerializeField] private float powerDuration = 0.7f;

    private float groundY;
    private float jumpTimer;
    private float attackTimer;
    private float zombieSpawnTimer;
    private int stageIndex;
    private int stageZombieTarget;
    private int stageZombiesSpawned;
    private string activeAttackState;
    private Camera stageCamera;
    private readonly List<ZombieInstance> zombies = new List<ZombieInstance>();

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        groundY = transform.position.y;
        jumpTimer = jumpDuration;
    }

    private void Start()
    {
        if (stageBackground == null || stageBackgrounds == null || stageBackgrounds.Length != 4)
        {
            Debug.LogError("Character Demo precisa de um SpriteRenderer de fundo e dos quatro sprites de fase.", this);
            enabled = false;
            return;
        }

        stageCamera = Camera.main;
        if (stageCamera == null || !stageCamera.orthographic)
        {
            Debug.LogError("Character Demo precisa de uma camera principal ortografica para ajustar os fundos.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < stageBackgrounds.Length; i++)
        {
            if (stageBackgrounds[i] == null)
            {
                Debug.LogError("O sprite de fundo da fase " + (i + 1) + " nao foi atribuido.", this);
                enabled = false;
                return;
            }
        }

        SetStageBackground(stageBackgrounds[stageIndex]);

        if (zombieWalkFrames == null || zombieWalkFrames.Length == 0)
        {
            Debug.LogError("Os quadros da animacao de caminhada do zumbi nao foram atribuidos.", this);
        }
        else
        {
            BeginStageEncounter();
        }

        if (themeSource != null && themeSource.clip != null)
        {
            themeSource.loop = true;
            themeSource.playOnAwake = false;
            themeSource.Play();
        }

        ApplyState("Idle");
    }

    private void Update()
    {
        UpdateZombieSpawning();
        UpdateZombies(Time.deltaTime);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float direction = 0f;
        if (keyboard.aKey.isPressed)
        {
            direction -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            direction += 1f;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && jumpTimer >= jumpDuration)
        {
            jumpTimer = 0f;
        }

        if (attackTimer <= 0f)
        {
            if (keyboard.kKey.wasPressedThisFrame)
            {
                StartAttack("Power", powerDuration);
            }
            else if (keyboard.jKey.wasPressedThisFrame)
            {
                StartAttack("Punch", punchDuration);
            }
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(
            position.x + direction * horizontalSpeed * Time.deltaTime,
            stageStartX,
            stageEndX);

        bool isJumping = jumpTimer < jumpDuration;
        if (isJumping)
        {
            jumpTimer = Mathf.Min(jumpTimer + Time.deltaTime, jumpDuration);
            float jumpPhase = jumpTimer / jumpDuration;
            position.y = groundY + Mathf.Sin(jumpPhase * Mathf.PI) * jumpHeight;
        }
        else
        {
            position.y = groundY;
        }

        transform.position = position;

        if (position.x >= stageEndX && stageIndex < stageBackgrounds.Length - 1)
        {
            AdvanceStage(position);
        }

        if (attackTimer > 0f)
        {
            ApplyState(activeAttackState);
            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
            return;
        }

        ApplyState(isJumping ? "Jump" : direction == 0f ? "Idle" : "Walk");
    }

    private void StartAttack(string stateName, float duration)
    {
        activeAttackState = stateName;
        attackTimer = duration;
    }

    private void AdvanceStage(Vector3 position)
    {
        stageIndex++;
        SetStageBackground(stageBackgrounds[stageIndex]);
        ClearZombies();
        if (zombieWalkFrames != null && zombieWalkFrames.Length > 0)
        {
            BeginStageEncounter();
        }

        position.x = stageStartX;
        transform.position = position;
    }

    private void BeginStageEncounter()
    {
        stageZombieTarget = Random.Range(1, 4);
        stageZombiesSpawned = 0;
        zombieSpawnTimer = Random.Range(0.4f, 0.9f);
    }

    private void UpdateZombieSpawning()
    {
        if (zombieWalkFrames == null || zombieWalkFrames.Length == 0
            || stageZombiesSpawned >= stageZombieTarget)
        {
            return;
        }

        zombieSpawnTimer -= Time.deltaTime;
        if (zombieSpawnTimer > 0f)
        {
            return;
        }

        SpawnZombie();
        stageZombiesSpawned++;
        zombieSpawnTimer = Random.Range(0.6f, 1.1f);
    }

    private void SpawnZombie()
    {
        float playerX = transform.position.x;
        float minDistance = 3f;
        float leftMax = Mathf.Min(stageEndX - 1f, playerX - minDistance);
        float rightMin = Mathf.Max(stageStartX + 1f, playerX + minDistance);
        bool canSpawnLeft = leftMax > stageStartX + 1f;
        bool canSpawnRight = rightMin < stageEndX - 1f;
        float spawnX;

        if (canSpawnLeft && canSpawnRight)
        {
            spawnX = Random.value < 0.5f
                ? Random.Range(stageStartX + 1f, leftMax)
                : Random.Range(rightMin, stageEndX - 1f);
        }
        else if (canSpawnRight)
        {
            spawnX = Random.Range(rightMin, stageEndX - 1f);
        }
        else if (canSpawnLeft)
        {
            spawnX = Random.Range(stageStartX + 1f, leftMax);
        }
        else
        {
            spawnX = playerX < (stageStartX + stageEndX) * 0.5f
                ? stageEndX - 1f
                : stageStartX + 1f;
        }

        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogError("O personagem precisa de um SpriteRenderer para posicionar e desenhar o zumbi.", this);
            return;
        }

        GameObject zombieObject = new GameObject("Zombie");
        zombieObject.transform.position = new Vector3(spawnX, groundY, transform.position.z);
        SpriteRenderer renderer = zombieObject.AddComponent<SpriteRenderer>();
        renderer.sprite = zombieWalkFrames[0];
        renderer.sortingLayerID = playerRenderer.sortingLayerID;
        renderer.sortingOrder = playerRenderer.sortingOrder + 1;

        float targetHeight = playerRenderer.bounds.size.y * zombieHeightRatio;
        float scale = targetHeight / zombieWalkFrames[0].bounds.size.y;
        zombieObject.transform.localScale = Vector3.one * scale;

        zombies.Add(new ZombieInstance(zombieObject.transform, renderer));
    }

    private void UpdateZombies(float deltaTime)
    {
        for (int i = zombies.Count - 1; i >= 0; i--)
        {
            ZombieInstance zombie = zombies[i];
            if (zombie.Transform == null)
            {
                zombies.RemoveAt(i);
                continue;
            }

            Vector3 position = zombie.Transform.position;
            float direction = Mathf.Sign(transform.position.x - position.x);
            position.x = Mathf.MoveTowards(position.x, transform.position.x, 1.25f * deltaTime);
            zombie.Transform.position = position;
            zombie.Renderer.flipX = direction > 0f;

            zombie.AnimationTimer += deltaTime;
            int frameIndex = Mathf.FloorToInt(zombie.AnimationTimer * 8f) % zombieWalkFrames.Length;
            zombie.Renderer.sprite = zombieWalkFrames[frameIndex];
        }
    }

    private void ClearZombies()
    {
        for (int i = 0; i < zombies.Count; i++)
        {
            if (zombies[i].Transform != null)
            {
                Destroy(zombies[i].Transform.gameObject);
            }
        }

        zombies.Clear();
    }

    private void SetStageBackground(Sprite background)
    {
        stageBackground.sprite = background;
        float targetHeight = stageCamera.orthographicSize * 2f * backgroundZoom;
        float scale = targetHeight / background.bounds.size.y;
        Vector3 localScale = stageBackground.transform.localScale;
        localScale.x = scale;
        localScale.y = scale;
        stageBackground.transform.localScale = localScale;
    }

    private void ApplyState(string stateName)
    {
        if (animator == null || stateNames == null)
        {
            return;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            if (stateNames[i] == stateName)
            {
                animator.SetInteger("State", i);
                return;
            }
        }
    }

    private sealed class ZombieInstance
    {
        public readonly Transform Transform;
        public readonly SpriteRenderer Renderer;
        public float AnimationTimer;

        public ZombieInstance(Transform transform, SpriteRenderer renderer)
        {
            Transform = transform;
            Renderer = renderer;
        }
    }
}
