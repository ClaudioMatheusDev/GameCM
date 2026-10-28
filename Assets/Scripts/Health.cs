using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 0f;

    private int current;
    private float invulnerabilityTimer;
    private bool initialized;

    public event Action<int> Damaged;
    public event Action Died;
    public event Action<int> Healed;

    public int Max => maxHealth;
    public int Current
    {
        get
        {
            EnsureInitialized();
            return current;
        }
    }

    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;

    public void Initialize(int max, float invulnerability)
    {
        maxHealth = Mathf.Max(1, max);
        invulnerabilityDuration = Mathf.Max(0f, invulnerability);
        current = maxHealth;
        invulnerabilityTimer = 0f;
        initialized = true;
    }

    public bool TakeDamage(int amount)
    {
        EnsureInitialized();
        if (amount <= 0 || IsDead || IsInvulnerable)
        {
            return false;
        }

        current = Mathf.Max(0, current - amount);
        invulnerabilityTimer = invulnerabilityDuration;
        Damaged?.Invoke(amount);

        if (current == 0)
        {
            Died?.Invoke();
        }

        return true;
    }

    /// <summary>Recupera vida ate o maximo. Retorna false se nada foi curado (vida cheia, morto ou valor invalido).</summary>
    public bool Heal(int amount)
    {
        EnsureInitialized();
        if (amount <= 0 || IsDead || current >= maxHealth)
        {
            return false;
        }

        int before = current;
        current = Mathf.Min(maxHealth, current + amount);
        Healed?.Invoke(current - before);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            Initialize(maxHealth, invulnerabilityDuration);
        }
    }
}
