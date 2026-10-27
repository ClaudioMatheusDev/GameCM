using System;
using UnityEngine;

[Serializable]
public class ZombieSettings
{
    public int health = 2;
    public float moveSpeed = 1.25f;
    public float stopDistance = 0.9f;
    public int contactDamage = 1;
    public float attackRange = 1.2f;
    public float attackHeight = 0.9f;
    public float attackCooldown = 1.2f;
    public float walkFps = 8f;
    public float hurtDuration = 0.35f;
    public float deathFps = 3f;
    public float deathFadeDelay = 0.6f;
    public float deathFadeDuration = 0.6f;
}

public class ZombieFrames
{
    public Sprite[] Walk;
    public Sprite[] Hurt;
    public Sprite[] Death;
}

[RequireComponent(typeof(SpriteRenderer), typeof(SpriteFrameAnimator), typeof(Health))]
public class ZombieController : MonoBehaviour
{
    private enum State
    {
        Walking,
        Hurt,
        Dying
    }

    private SpriteRenderer spriteRenderer;
    private SpriteFrameAnimator frameAnimator;
    private Health health;
    private BoxCollider2D hitBox;
    private ZombieSettings settings;
    private ZombieFrames frames;
    private PlayerController player;
    private State state;
    private float stateTimer;
    private float attackTimer;
    private float knockbackVelocity;
    private float minX;
    private float maxX;

    public event Action<ZombieController> Died;
    public event Action<ZombieController> Finished;

    public bool IsAlive => state != State.Dying;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frameAnimator = GetComponent<SpriteFrameAnimator>();
        health = GetComponent<Health>();
        hitBox = GetComponent<BoxCollider2D>();
        if (hitBox == null)
        {
            hitBox = gameObject.AddComponent<BoxCollider2D>();
        }

        hitBox.isTrigger = true;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    public void Setup(
        ZombieFrames zombieFrames,
        ZombieSettings zombieSettings,
        PlayerController target,
        Vector3 position,
        float scale,
        float stageMinX,
        float stageMaxX)
    {
        frames = zombieFrames;
        settings = zombieSettings;
        player = target;
        minX = stageMinX;
        maxX = stageMaxX;

        transform.position = position;
        transform.localScale = Vector3.one * scale;
        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = frames.Walk[0];

        Bounds bounds = frames.Walk[0].bounds;
        hitBox.size = bounds.size;
        hitBox.offset = bounds.center;
        hitBox.enabled = true;

        health.Initialize(settings.health, 0f);
        attackTimer = 0f;
        knockbackVelocity = 0f;
        EnterWalking();
        gameObject.SetActive(true);
    }

    /// <summary>Aplica um golpe do jogador. Retorna true se o dano foi aplicado.</summary>
    public bool ApplyHit(int damage, float knockbackSpeed)
    {
        if (!IsAlive)
        {
            return false;
        }

        float away = Mathf.Sign(transform.position.x - player.transform.position.x);
        knockbackVelocity = away * knockbackSpeed;
        return health.TakeDamage(damage);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        ApplyKnockback(deltaTime);

        switch (state)
        {
            case State.Walking:
                UpdateWalking(deltaTime);
                break;
            case State.Hurt:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    EnterWalking();
                }

                break;
            case State.Dying:
                UpdateDying(deltaTime);
                break;
        }
    }

    private void UpdateWalking(float deltaTime)
    {
        Vector3 position = transform.position;
        float dx = player.transform.position.x - position.x;
        float dy = player.transform.position.y - position.y;
        spriteRenderer.flipX = dx > 0f;

        if (player.IsDead)
        {
            return;
        }

        if (Mathf.Abs(dx) > settings.stopDistance)
        {
            position.x = Mathf.MoveTowards(position.x, player.transform.position.x, settings.moveSpeed * deltaTime);
            transform.position = position;
        }

        attackTimer -= deltaTime;
        bool inRange = Mathf.Abs(dx) <= settings.attackRange && Mathf.Abs(dy) <= settings.attackHeight;
        if (attackTimer <= 0f && inRange && player.ReceiveHit(settings.contactDamage, position.x))
        {
            attackTimer = settings.attackCooldown;
        }
    }

    private void UpdateDying(float deltaTime)
    {
        stateTimer += deltaTime;
        float fade = Mathf.Clamp01((stateTimer - settings.deathFadeDelay) / settings.deathFadeDuration);
        Color color = spriteRenderer.color;
        color.a = 1f - fade;
        spriteRenderer.color = color;

        if (fade >= 1f)
        {
            gameObject.SetActive(false);
            Finished?.Invoke(this);
        }
    }

    private void ApplyKnockback(float deltaTime)
    {
        if (Mathf.Approximately(knockbackVelocity, 0f))
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x + knockbackVelocity * deltaTime, minX, maxX);
        transform.position = position;
        knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, 12f * deltaTime);
    }

    private void EnterWalking()
    {
        state = State.Walking;
        frameAnimator.Play(frames.Walk, settings.walkFps, true);
    }

    private void OnDamaged(int amount)
    {
        if (health.IsDead)
        {
            return;
        }

        state = State.Hurt;
        stateTimer = settings.hurtDuration;
        if (frames.Hurt != null && frames.Hurt.Length > 0)
        {
            frameAnimator.Play(frames.Hurt, frames.Hurt.Length / settings.hurtDuration, false);
        }
    }

    private void OnDied()
    {
        state = State.Dying;
        stateTimer = 0f;
        hitBox.enabled = false;
        if (frames.Death != null && frames.Death.Length > 0)
        {
            frameAnimator.Play(frames.Death, settings.deathFps, false);
        }
        else
        {
            frameAnimator.Stop();
        }

        Died?.Invoke(this);
    }
}
