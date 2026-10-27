using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerTuning
{
    public float horizontalSpeed = 5f;
    public float sprintMultiplier = 1.6f;
    public float stageStartX = -10.5f;
    public float stageEndX = 10.5f;
    public float jumpDuration = 0.55f;
    public float jumpHeight = 1.15f;
    public float punchDuration = 0.6f;
    public float powerDuration = 0.7f;
    public int punchDamage = 1;
    public int powerDamage = 2;
    public float punchRange = 1.4f;
    public float powerRange = 2.4f;
    public float punchKnockback = 4f;
    public float powerKnockback = 9f;
    public float powerCooldown = 2f;
    public float attackHitMoment = 0.4f;
    public float attackMoveFactor = 0.5f;
    public int maxHealth = 5;
    public float invulnerabilityDuration = 1.2f;
    public float hurtDuration = 0.4f;
    public float hurtKnockback = 6f;
}

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    private const float MoveDeadZone = 0.2f;
    private const float BlinkPeriod = 0.16f;

    private PlayerTuning settings;
    private string[] stateNames;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private GameInput input;
    private bool configured;

    private float groundY;
    private float jumpTimer;
    private float attackTimer;
    private float attackDuration;
    private bool attackHitDone;
    private bool attackIsPower;
    private string activeAttackState;
    private float powerCooldownTimer;
    private float hurtTimer;
    private float knockbackVelocity;
    private float facing = 1f;
    private readonly List<Collider2D> hitBuffer = new List<Collider2D>(16);
    private ContactFilter2D hitFilter;

    public event Action Died;

    public Health Health => health;
    public bool IsDead => health != null && health.IsDead;
    public bool InputLocked { get; set; }
    public float PowerCooldownRemaining => powerCooldownTimer;
    public bool RestartPressed => input != null && input.RestartPressed;
    public bool BackPressed => input != null && input.BackPressed;

    public void Configure(PlayerTuning playerSettings, string[] animatorStates)
    {
        settings = playerSettings;
        stateNames = animatorStates;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        health.Initialize(settings.maxHealth, settings.invulnerabilityDuration);
        health.Died += OnHealthDied;

        hitFilter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
        input = new GameInput();
        groundY = transform.position.y;
        jumpTimer = settings.jumpDuration;
        configured = true;
        ApplyState("Idle");
    }

    /// <summary>Recebe dano de um inimigo em sourceX. Retorna true se o dano foi aplicado.</summary>
    public bool ReceiveHit(int damage, float sourceX)
    {
        if (!configured || IsDead || !health.TakeDamage(damage))
        {
            return false;
        }

        attackTimer = 0f;
        if (!IsDead)
        {
            hurtTimer = settings.hurtDuration;
            knockbackVelocity = Mathf.Sign(transform.position.x - sourceX) * settings.hurtKnockback;
        }

        return true;
    }

    /// <summary>Posiciona o personagem no inicio da fase, no chao.</summary>
    public void ResetToStageStart()
    {
        Vector3 position = transform.position;
        position.x = settings.stageStartX;
        position.y = groundY;
        transform.position = position;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnHealthDied;
        }

        input?.Dispose();
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        Vector3 position = transform.position;

        if (IsDead)
        {
            UpdateJump(ref position, deltaTime);
            transform.position = position;
            spriteRenderer.color = Color.white;
            ApplyState("Dead");
            return;
        }

        powerCooldownTimer = Mathf.Max(0f, powerCooldownTimer - deltaTime);
        hurtTimer = Mathf.Max(0f, hurtTimer - deltaTime);
        bool isHurt = hurtTimer > 0f;
        bool canAct = !InputLocked && !isHurt;

        float direction = 0f;
        bool sprinting = false;
        if (canAct)
        {
            float axis = input.Horizontal;
            direction = Mathf.Abs(axis) < MoveDeadZone ? 0f : Mathf.Sign(axis);
            sprinting = direction != 0f && input.SprintHeld;
            HandleActionInput();
        }

        bool attacking = attackTimer > 0f;
        float speed = settings.horizontalSpeed * (sprinting ? settings.sprintMultiplier : 1f);
        if (attacking)
        {
            speed *= settings.attackMoveFactor;
        }

        if (isHurt)
        {
            position.x += knockbackVelocity * deltaTime;
            knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, 15f * deltaTime);
        }
        else
        {
            position.x += direction * speed * deltaTime;
        }

        position.x = Mathf.Clamp(position.x, settings.stageStartX, settings.stageEndX);
        bool isJumping = UpdateJump(ref position, deltaTime);
        transform.position = position;

        if (direction != 0f && !attacking)
        {
            facing = direction;
            spriteRenderer.flipX = facing < 0f;
        }

        if (attacking)
        {
            UpdateAttack(deltaTime);
        }

        UpdateBlink();
        ApplyState(ChooseState(isHurt, attacking, isJumping, direction, sprinting));
    }

    private void HandleActionInput()
    {
        if (input.JumpPressed && jumpTimer >= settings.jumpDuration)
        {
            jumpTimer = 0f;
        }

        if (attackTimer > 0f)
        {
            return;
        }

        if (input.PowerPressed && powerCooldownTimer <= 0f)
        {
            StartAttack("Power", settings.powerDuration, true);
            powerCooldownTimer = settings.powerCooldown;
        }
        else if (input.PunchPressed)
        {
            StartAttack("Punch", settings.punchDuration, false);
        }
    }

    private bool UpdateJump(ref Vector3 position, float deltaTime)
    {
        if (jumpTimer >= settings.jumpDuration)
        {
            position.y = groundY;
            return false;
        }

        jumpTimer = Mathf.Min(jumpTimer + deltaTime, settings.jumpDuration);
        float phase = jumpTimer / settings.jumpDuration;
        position.y = groundY + Mathf.Sin(phase * Mathf.PI) * settings.jumpHeight;
        return jumpTimer < settings.jumpDuration;
    }

    private string ChooseState(bool isHurt, bool attacking, bool isJumping, float direction, bool sprinting)
    {
        if (isHurt)
        {
            return "Hurt";
        }

        if (attacking)
        {
            return activeAttackState;
        }

        if (isJumping)
        {
            return "Jump";
        }

        if (direction == 0f)
        {
            return "Idle";
        }

        return sprinting ? "Run" : "Walk";
    }

    private void StartAttack(string stateName, float duration, bool isPower)
    {
        activeAttackState = stateName;
        attackDuration = duration;
        attackTimer = duration;
        attackIsPower = isPower;
        attackHitDone = false;
    }

    private void UpdateAttack(float deltaTime)
    {
        float elapsed = attackDuration - attackTimer;
        if (!attackHitDone && elapsed >= attackDuration * settings.attackHitMoment)
        {
            attackHitDone = true;
            PerformAttackHit();
        }

        attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
    }

    private void PerformAttackHit()
    {
        float range = attackIsPower ? settings.powerRange : settings.punchRange;
        int damage = attackIsPower ? settings.powerDamage : settings.punchDamage;
        float knockback = attackIsPower ? settings.powerKnockback : settings.punchKnockback;

        Bounds body = spriteRenderer.bounds;
        Vector2 center = new Vector2(transform.position.x + facing * range * 0.5f, body.center.y);
        Vector2 size = new Vector2(range, Mathf.Max(body.size.y, 1.5f));

        Physics2D.SyncTransforms();
        hitBuffer.Clear();
        Physics2D.OverlapBox(center, size, 0f, hitFilter, hitBuffer);
        for (int i = 0; i < hitBuffer.Count; i++)
        {
            ZombieController zombie = hitBuffer[i].GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.ApplyHit(damage, knockback);
            }
        }
    }

    private void UpdateBlink()
    {
        Color color = Color.white;
        if (health.IsInvulnerable && Mathf.Repeat(Time.time, BlinkPeriod) < BlinkPeriod * 0.5f)
        {
            color.a = 0.35f;
        }

        spriteRenderer.color = color;
    }

    private void OnHealthDied()
    {
        attackTimer = 0f;
        Died?.Invoke();
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
}
