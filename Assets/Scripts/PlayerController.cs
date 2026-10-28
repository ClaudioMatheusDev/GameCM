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
    public float comboPunchDuration = 0.42f;
    public float comboWindow = 0.5f;
    public int finisherDamageBonus = 1;
    public float finisherKnockbackMultiplier = 1.8f;
    public float finisherRangeMultiplier = 1.2f;
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
    private float dustTimer;
    private bool wasJumping;
    private int comboStep;
    private float comboTimer;
    private bool punchQueued;
    private readonly List<Collider2D> hitBuffer = new List<Collider2D>(16);
    private ContactFilter2D hitFilter;

    public event Action Died;

    public Health Health => health;
    public bool IsDead => health != null && health.IsDead;
    public bool InputLocked { get; set; }
    private Vector3 FeetPosition => new Vector3(transform.position.x, spriteRenderer.bounds.min.y, transform.position.z);
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
        comboStep = 0;
        comboTimer = 0f;
        punchQueued = false;
        if (!IsDead)
        {
            SfxPlayer.Play(Sfx.PlayerHurt);
            ImpactFx.Play(Fx.PlayerHurt);
            ParticleFx.PlayerHurt(spriteRenderer.bounds.center);
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
        float previousX = position.x;

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
        if (attackTimer <= 0f)
        {
            comboTimer = Mathf.Max(0f, comboTimer - deltaTime);
        }

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
        UpdateDust(deltaTime, isJumping, isHurt, Mathf.Abs(position.x - previousX) > 0.0001f, sprinting);

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
            SfxPlayer.Play(Sfx.Jump);
            ParticleFx.Dust(FeetPosition, 6);
        }

        if (attackTimer > 0f)
        {
            // Apertar de novo depois do impacto do soco encadeia o proximo golpe do combo.
            if (!attackIsPower && attackHitDone && input.PunchPressed)
            {
                punchQueued = true;
            }

            return;
        }

        if (input.PowerPressed && powerCooldownTimer <= 0f)
        {
            comboStep = 0;
            comboTimer = 0f;
            punchQueued = false;
            StartAttack("Power", settings.powerDuration, true);
            powerCooldownTimer = settings.powerCooldown;
        }
        else if (input.PunchPressed || punchQueued)
        {
            StartPunch();
        }
    }

    private void UpdateDust(float deltaTime, bool isJumping, bool isHurt, bool moved, bool sprinting)
    {
        if (wasJumping && !isJumping)
        {
            ParticleFx.Dust(FeetPosition, 8);
        }

        wasJumping = isJumping;
        if (isJumping || isHurt || !moved)
        {
            dustTimer = 0f;
            return;
        }

        dustTimer -= deltaTime;
        if (dustTimer <= 0f)
        {
            ParticleFx.Dust(FeetPosition, 2);
            dustTimer = sprinting ? 0.07f : 0.14f;
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

    /// <summary>Combo de tres socos; o terceiro e o finalizador (mais dano, alcance e empurrao).</summary>
    private void StartPunch()
    {
        punchQueued = false;
        comboStep = comboTimer > 0f ? comboStep % 3 + 1 : 1;
        float duration = comboStep == 1 ? settings.punchDuration : settings.comboPunchDuration;
        StartAttack("Punch", duration, false);

        // Reinicia a animacao do soco, que ja estava tocando no golpe anterior.
        if (comboStep > 1)
        {
            animator.Play("Punch", 0, 0f);
        }
    }

    private void StartAttack(string stateName, float duration, bool isPower)
    {
        activeAttackState = stateName;
        attackDuration = duration;
        attackTimer = duration;
        attackIsPower = isPower;
        attackHitDone = false;
        SfxPlayer.Play(isPower ? Sfx.Power : Sfx.Punch);
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
        if (attackTimer <= 0f && !attackIsPower)
        {
            comboTimer = settings.comboWindow;
        }
    }

    private void PerformAttackHit()
    {
        bool finisher = !attackIsPower && comboStep == 3;
        float range = attackIsPower
            ? settings.powerRange
            : settings.punchRange * (finisher ? settings.finisherRangeMultiplier : 1f);
        int damage = attackIsPower
            ? settings.powerDamage
            : settings.punchDamage + (finisher ? settings.finisherDamageBonus : 0);
        float knockback = attackIsPower
            ? settings.powerKnockback
            : settings.punchKnockback * (finisher ? settings.finisherKnockbackMultiplier : 1f);

        Bounds body = spriteRenderer.bounds;
        Vector2 center = new Vector2(transform.position.x + facing * range * 0.5f, body.center.y);
        Vector2 size = new Vector2(range, Mathf.Max(body.size.y, 1.5f));

        if (attackIsPower || finisher)
        {
            ParticleFx.PowerBurst(center);
        }

        Physics2D.SyncTransforms();
        hitBuffer.Clear();
        Physics2D.OverlapBox(center, size, 0f, hitFilter, hitBuffer);
        bool anyHit = false;
        bool anyKill = false;
        for (int i = 0; i < hitBuffer.Count; i++)
        {
            ZombieController zombie = hitBuffer[i].GetComponent<ZombieController>();
            if (zombie != null && zombie.ApplyHit(damage, knockback))
            {
                anyHit = true;
                anyKill |= !zombie.IsAlive && !zombie.IsBoss;
            }
        }

        if (anyKill)
        {
            ImpactFx.Play(Fx.Kill);
        }
        else if (anyHit)
        {
            ImpactFx.Play(attackIsPower || finisher ? Fx.PowerHit : Fx.PunchHit);
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
        SfxPlayer.Play(Sfx.PlayerDeath);
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
