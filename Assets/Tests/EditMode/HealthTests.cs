using NUnit.Framework;
using UnityEngine;

public class HealthTests
{
    private GameObject owner;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("HealthTestOwner");
        health = owner.AddComponent<Health>();
        health.Initialize(3, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void StartsWithFullHealth()
    {
        Assert.AreEqual(3, health.Current);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void TakeDamageReducesCurrentAndRaisesEvent()
    {
        int received = 0;
        health.Damaged += amount => received = amount;

        Assert.IsTrue(health.TakeDamage(1));

        Assert.AreEqual(2, health.Current);
        Assert.AreEqual(1, received);
    }

    [Test]
    public void InvulnerabilityBlocksDamageUntilTimerExpires()
    {
        health.TakeDamage(1);

        Assert.IsTrue(health.IsInvulnerable);
        Assert.IsFalse(health.TakeDamage(1));
        Assert.AreEqual(2, health.Current);

        health.Tick(1.1f);

        Assert.IsFalse(health.IsInvulnerable);
        Assert.IsTrue(health.TakeDamage(1));
        Assert.AreEqual(1, health.Current);
    }

    [Test]
    public void LethalDamageRaisesDiedOnceAndClampsToZero()
    {
        int deaths = 0;
        health.Died += () => deaths++;

        health.TakeDamage(10);
        health.Tick(5f);
        health.TakeDamage(1);

        Assert.AreEqual(0, health.Current);
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual(1, deaths);
    }

    [Test]
    public void NonPositiveDamageIsIgnored()
    {
        Assert.IsFalse(health.TakeDamage(0));
        Assert.IsFalse(health.TakeDamage(-2));
        Assert.AreEqual(3, health.Current);
    }

    [Test]
    public void InitializeResetsAfterDeath()
    {
        health.TakeDamage(3);

        health.Initialize(2, 0f);

        Assert.IsFalse(health.IsDead);
        Assert.AreEqual(2, health.Current);
    }
}
