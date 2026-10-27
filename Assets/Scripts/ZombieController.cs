using System;
using System.Collections.Generic;
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
    public float chargeCooldown = 4f;
    public float chargeWindup = 0.6f;
    public float chargeDuration = 0.7f;
    public float chargeSpeedMultiplier = 3.2f;
    public float knockbackFriction = 12f;
    public float windupPulseSpeed = 10f;
    public float separationDistance = 0.7f;
    public float attackRetryDelay = 0.15f;
    public float attackWindup = 0.25f;
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
    private const float ChargeMinDistance = 2.5f;
    private const float ChargeMaxDistance = 8f;
    private const float SeparationSpeed = 6f;

    private enum State
    {
        Walking,
        Windup,
        Charging,
        Striking,
        Hurt,
        Dying
    }

    private SpriteRenderer spriteRenderer;
    private SpriteFrameAnimator frameAnimator;
    private Health health;
    private BoxCollider2D hitBox;
    private WorldHealthBar healthBar;
    private DifficultyRules rules;
    private ZombieSettings settings;
    private ZombieVariant variant;
    private ZombieFrames frames;
    private PlayerController player;
    private State state;
    private float stateTimer;
    private float attackTimer;
    private float chargeTimer;
    private float chargeDirection;
    private float knockbackVelocity;
    private float minX;
    private float maxX;
    private IReadOnlyList<ZombieController> neighbors;

    public event Action<ZombieController> Died;
    public event Action<ZombieController> Finished;

    public ZombieVariant Variant => variant;
    public Health Health => health;
    public bool IsBoss => variant != null && variant.Type == ZombieType.Boss;
    public bool IsAlive => state != State.Dying;

    private float StopDistance => settings.stopDistance * variant.ScaleMultiplier;
    private float AttackRange => settings.attackRange * variant.ScaleMultiplier;
    private float MoveSpeed => settings.moveSpeed * variant.SpeedMultiplier;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frameAnimator = GetComponent<SpriteFrameAnimator>();
        health = GetComponent<Health>();
        healthBar = WorldHealthBar.Create(spriteRenderer);
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
        ZombieVariant zombieVariant,
        PlayerController target,
        Vector3 position,
        float scale,
        float stageMinX,
        float stageMaxX,
        IReadOnlyList<ZombieController> activeZombies,
        DifficultyRules difficultyRules)
    {
        frames = zombieFrames;
        settings = zombieSettings;
        variant = zombieVariant;
        player = target;
        minX = stageMinX;
        maxX = stageMaxX;
        neighbors = activeZombies;
        rules = difficultyRules;

        transform.position = position;
        transform.localScale = Vector3.one * scale * variant.ScaleMultiplier;
        spriteRenderer.color = variant.Tint;
        spriteRenderer.sprite = frames.Walk[0];

        Bounds bounds = frames.Walk[0].bounds;
        hitBox.size = bounds.size;
        hitBox.offset = bounds.center;
        hitBox.enabled = true;

        health.Initialize(Mathf.Max(1, Mathf.RoundToInt(settings.health * variant.HealthMultiplier * rules.ZombieHealth)), 0f);
        healthBar.Hide();
        attackTimer = 0f;
        chargeTimer = variant.Charges ? settings.chargeCooldown * 0.5f : 0f;
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
        knockbackVelocity = away * knockbackSpeed * (1f - variant.KnockbackResistance);
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
            case State.Windup:
                UpdateWindup(deltaTime);
                break;
            case State.Charging:
                UpdateCharging(deltaTime);
                break;
            case State.Striking:
                UpdateStriking(deltaTime);
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
        spriteRenderer.flipX = dx > 0f;

        if (player.IsDead)
        {
            return;
        }

        if (Mathf.Abs(dx) > StopDistance)
        {
            position.x = Mathf.MoveTowards(position.x, player.transform.position.x, MoveSpeed * deltaTime);
            transform.position = position;
        }

        Separate(deltaTime);

        attackTimer -= deltaTime;
        if (attackTimer <= 0f && InAttackRange() && !player.Health.IsInvulnerable)
        {
            EnterStriking();
            return;
        }

        if (variant.Charges)
        {
            chargeTimer -= deltaTime;
            float distance = Mathf.Abs(dx);
            if (chargeTimer <= 0f && distance >= ChargeMinDistance && distance <= ChargeMaxDistance)
            {
                EnterWindup();
            }
        }
    }

    private void UpdateWindup(float deltaTime)
    {
        stateTimer -= deltaTime;
        float pulse = Mathf.PingPong(Time.time * settings.windupPulseSpeed, 1f);
        spriteRenderer.color = Color.Lerp(variant.Tint, Color.red, pulse);

        if (stateTimer <= 0f)
        {
            EnterCharging();
        }
    }

    private void UpdateCharging(float deltaTime)
    {
        stateTimer -= deltaTime;
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x + chargeDirection * MoveSpeed * settings.chargeSpeedMultiplier * deltaTime, minX, maxX);
        transform.position = position;

        if (!player.IsDead)
        {
            TryContactAttack(deltaTime);
        }

        if (stateTimer <= 0f)
        {
            chargeTimer = settings.chargeCooldown;
            ImpactFx.Play(Fx.BossCharge);
            EnterWalking();
        }
    }

    /// <summary>Golpe instantaneo (usado durante a investida do chefe).</summary>
    private void TryContactAttack(float deltaTime)
    {
        attackTimer -= deltaTime;
        if (attackTimer <= 0f && InAttackRange())
        {
            // Se o jogador estava invulneravel, tenta de novo em breve em vez de a cada frame.
            attackTimer = DealContactDamage() ? settings.attackCooldown : settings.attackRetryDelay;
        }
    }

    /// <summary>Aviso antes do golpe: o zumbi para e pisca em amarelo; se o jogador sair do alcance, erra.</summary>
    private void UpdateStriking(float deltaTime)
    {
        stateTimer -= deltaTime;
        float pulse = Mathf.PingPong(Time.time * settings.windupPulseSpeed, 1f);
        spriteRenderer.color = Color.Lerp(variant.Tint, Color.yellow, pulse);

        if (stateTimer > 0f)
        {
            return;
        }

        bool hit = !player.IsDead && InAttackRange() && DealContactDamage();
        attackTimer = hit ? settings.attackCooldown : settings.attackRetryDelay;
        EnterWalking();
    }

    private bool InAttackRange()
    {
        float dx = player.transform.position.x - transform.position.x;
        return Mathf.Abs(dx) <= AttackRange && Mathf.Abs(player.AirHeight) <= settings.attackHeight;
    }

    private bool DealContactDamage()
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(settings.contactDamage * variant.DamageMultiplier * rules.ZombieDamage));
        return player.ReceiveHit(damage, transform.position.x);
    }

    /// <summary>Afasta este zumbi dos vizinhos vivos para eles nao ficarem empilhados.</summary>
    private void Separate(float deltaTime)
    {
        if (neighbors == null)
        {
            return;
        }

        float minGap = settings.separationDistance * variant.ScaleMultiplier;
        Vector3 position = transform.position;
        float push = 0f;
        for (int i = 0; i < neighbors.Count; i++)
        {
            ZombieController other = neighbors[i];
            if (other == this || !other.IsAlive || !other.gameObject.activeSelf)
            {
                continue;
            }

            float gap = position.x - other.transform.position.x;
            if (Mathf.Abs(gap) >= minGap)
            {
                continue;
            }

            float away = Mathf.Approximately(gap, 0f) ? (GetInstanceID() < other.GetInstanceID() ? -1f : 1f) : Mathf.Sign(gap);
            push += away * (minGap - Mathf.Abs(gap));
        }

        if (push != 0f)
        {
            position.x = Mathf.Clamp(position.x + Mathf.Clamp(push, -1f, 1f) * SeparationSpeed * deltaTime, minX, maxX);
            transform.position = position;
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
        knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, settings.knockbackFriction * deltaTime);
    }

    private void EnterWalking()
    {
        state = State.Walking;
        spriteRenderer.color = variant.Tint;
        frameAnimator.Play(frames.Walk, settings.walkFps, true);
    }

    private void EnterStriking()
    {
        state = State.Striking;
        stateTimer = settings.attackWindup;
    }

    private void EnterWindup()
    {
        state = State.Windup;
        stateTimer = settings.chargeWindup;
        SfxPlayer.Play(Sfx.BossRoar);
    }

    private void EnterCharging()
    {
        state = State.Charging;
        stateTimer = settings.chargeDuration;
        chargeDirection = Mathf.Sign(player.transform.position.x - transform.position.x);
        spriteRenderer.flipX = chargeDirection > 0f;
        spriteRenderer.color = variant.Tint;
    }

    private void OnDamaged(int amount)
    {
        if (health.IsDead)
        {
            return;
        }

        SfxPlayer.Play(Sfx.ZombieHurt);
        if (!IsBoss)
        {
            healthBar.SetFraction(health.Max > 0 ? (float)health.Current / health.Max : 0f);
        }

        // O chefe nao sofre atordoamento enquanto prepara ou executa a investida.
        if (state == State.Windup || state == State.Charging)
        {
            return;
        }

        float duration = settings.hurtDuration * variant.StaggerMultiplier;
        state = State.Hurt;
        stateTimer = duration;
        spriteRenderer.color = Color.Lerp(variant.Tint, Color.red, 0.6f);
        if (frames.Hurt != null && frames.Hurt.Length > 0)
        {
            frameAnimator.Play(frames.Hurt, frames.Hurt.Length / duration, false);
        }
    }

    private void OnDied()
    {
        state = State.Dying;
        healthBar.Hide();
        SfxPlayer.Play(IsBoss ? Sfx.BossRoar : Sfx.ZombieDeath);
        stateTimer = 0f;
        spriteRenderer.color = variant.Tint;
        hitBox.enabled = false;
        if (frames.Death != null && frames.Death.Length > 0)
        {
            frameAnimator.Play(frames.Death, settings.deathFps, false);
        }
        else
        {
            frameAnimator.Stop();
        }

        if (IsBoss)
        {
            ImpactFx.Play(Fx.BossDeath);
        }

        Died?.Invoke(this);
    }
}
