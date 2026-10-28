using NUnit.Framework;

public class ZombieVariantTests
{
    [Test]
    public void FirstStageOnlySpawnsNormalZombies()
    {
        for (float roll = 0f; roll < 1f; roll += 0.05f)
        {
            Assert.AreEqual(ZombieType.Normal, ZombieSpawnTable.Roll(0, roll));
        }
    }

    [Test]
    public void LaterStagesIntroduceFastThenTankZombies()
    {
        Assert.AreEqual(ZombieType.Fast, ZombieSpawnTable.Roll(1, 0.1f));
        Assert.AreEqual(ZombieType.Normal, ZombieSpawnTable.Roll(1, 0.9f));
        Assert.AreEqual(ZombieType.Fast, ZombieSpawnTable.Roll(2, 0.1f));
        Assert.AreEqual(ZombieType.Tank, ZombieSpawnTable.Roll(2, 0.4f));
        Assert.AreEqual(ZombieType.Normal, ZombieSpawnTable.Roll(2, 0.9f));
    }

    [Test]
    public void RollNeverReturnsBoss()
    {
        for (int stage = -1; stage < 6; stage++)
        {
            for (float roll = 0f; roll < 1f; roll += 0.01f)
            {
                Assert.AreNotEqual(ZombieType.Boss, ZombieSpawnTable.Roll(stage, roll));
            }
        }
    }

    [Test]
    public void VariantsMatchTheirRoles()
    {
        ZombieVariant normal = ZombieVariant.Get(ZombieType.Normal);
        ZombieVariant fast = ZombieVariant.Get(ZombieType.Fast);
        ZombieVariant tank = ZombieVariant.Get(ZombieType.Tank);
        ZombieVariant boss = ZombieVariant.Get(ZombieType.Boss);

        Assert.Greater(fast.SpeedMultiplier, normal.SpeedMultiplier);
        Assert.Less(fast.HealthMultiplier, normal.HealthMultiplier);
        Assert.Greater(tank.HealthMultiplier, normal.HealthMultiplier);
        Assert.Less(tank.SpeedMultiplier, normal.SpeedMultiplier);
        Assert.Greater(boss.HealthMultiplier, tank.HealthMultiplier);
        Assert.IsTrue(boss.Charges);
        Assert.IsFalse(normal.Charges);
    }

    [Test]
    public void KillPointsApplyScoreMultiplier()
    {
        Assert.AreEqual(150, ScoreCalculator.KillPoints(1, 1.5f));
        Assert.AreEqual(1000, ScoreCalculator.KillPoints(1, ZombieVariant.Get(ZombieType.Boss).ScoreMultiplier));
    }

    [Test]
    public void DropChancesFavorTougherZombiesAndSkipTheBoss()
    {
        ZombieVariant normal = ZombieVariant.Get(ZombieType.Normal);
        ZombieVariant tank = ZombieVariant.Get(ZombieType.Tank);
        ZombieVariant boss = ZombieVariant.Get(ZombieType.Boss);

        Assert.Greater(tank.DropChance, normal.DropChance);
        Assert.Greater(tank.HealAmount, normal.HealAmount);
        Assert.AreEqual(0f, boss.DropChance);
        Assert.IsTrue(normal.DropChance > 0f && normal.DropChance < 1f);
    }
}
