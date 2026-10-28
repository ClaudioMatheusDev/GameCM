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
    public float chargeCooldown = 4f;
    public float chargeWindup = 0.6f;
    public float chargeDuration = 0.7f;
    public float chargeSpeedMultiplier = 3.2f;
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

    private enum State
    {
        Walking,
        Windup,
        Charging,
        Hurt,
        Dying
    }

    private SpriteRenderer spriteRenderer;
    private SpriteFrameAnimator frameAnimator;
    private Health health;
    private BoxCollider2D hitBox;
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
    private float chargeDustTimer;
    private float minX;
    private float maxX;

    public event Action<ZombieController> Died;
    public event Action<ZombieController> Finished;

    public ZombieVariant Variant => variant;
    public Health Health => health;
    public bool IsBoss => variant != null && variant.Type == ZombieType.Boss;
    public bool IsAlive => state != State.Dying;
    public Vector3 FeetPosition => new Vector3(transform.position.x, spriteRenderer.bounds.min.y, transform.position.z);

    private float StopDistance => settings.stopDistance * variant.ScaleMultiplier;
    private float AttackRange => settings.attackRange * variant.ScaleMultiplier;
    private float MoveSpeed => settings.moveSpeed * variant.SpeedMultiplier;

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
        ZombieVariant zombieVariant,
        PlayerController target,
        Vector3 position,
        float scale,
        float stageMinX,
        float stageMaxX)
    {
        frames = zombieFrames;
        settings = zombieSettings;
        variant = zombieVariant;
        player = target;
        minX = stageMinX;
        maxX = stageMaxX;

        transform.position = position;
        transform.localScale = Vector3.one * scale * variant.ScaleMultiplier;
        spriteRenderer.color = variant.Tint;
        spriteRenderer.sprite = frames.Walk[0];

        Bounds bounds = frames.Walk[0].bounds;
        hitBox.size = bounds.size;
        hitBox.offset = bounds.center;
        hitBox.enabled = true;

        health.Initialize(Mathf.Max(1, Mathf.RoundToInt(settings.health * variant.HealthMultiplier)), 0f);
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
        bool applied = health.TakeDamage(damage);
        if (applied)
        {
            Vector3 center = transform.TransformPoint(hitBox.offset);
            if (IsAlive)
            {
                ParticleFx.Hit(center, away);
            }
            else
            {
                ParticleFx.Kill(center, away);
            }
        }

        return applied;
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

        TryContactAttack(deltaTime);

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
        float pulse = Mathf.PingPong(Time.time * 10f, 1f);
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

        chargeDustTimer -= deltaTime;
        if (chargeDustTimer <= 0f)
        {
            ParticleFx.Dust(FeetPosition, 3);
            chargeDustTimer = 0.05f;
        }

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

    private void TryContactAttack(float deltaTime)
    {
        attackTimer -= deltaTime;
        Vector3 position = transform.position;
        float dx = player.transform.position.x - position.x;
        float dy = player.transform.position.y - position.y;
        bool inRange = Mathf.Abs(dx) <= AttackRange && Mathf.Abs(dy) <= settings.attackHeight;
        int damage = Mathf.Max(1, Mathf.RoundToInt(settings.contactDamage * variant.DamageMultiplier));

        if (attackTimer <= 0f && inRange && player.ReceiveHit(damage, position.x))
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
        spriteRenderer.color = variant.Tint;
        frameAnimator.Play(frames.Walk, settings.walkFps, true);
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
            ParticleFx.BossDeath(transform.TransformPoint(hitBox.offset));
        }

        Died?.Invoke(this);
    }
}
