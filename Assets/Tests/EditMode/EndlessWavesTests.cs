using NUnit.Framework;

public class EndlessWavesTests
{
    [Test]
    public void ZombieCountGrowsAndIsCapped()
    {
        Assert.AreEqual(3, EndlessWaves.ZombieCount(0));
        Assert.AreEqual(5, EndlessWaves.ZombieCount(1));
        Assert.AreEqual(EndlessWaves.MaxZombies, EndlessWaves.ZombieCount(100));
    }

    [Test]
    public void BossAppearsEveryFifthWave()
    {
        Assert.IsFalse(EndlessWaves.HasBoss(0));
        Assert.IsTrue(EndlessWaves.HasBoss(4));
        Assert.IsFalse(EndlessWaves.HasBoss(5));
        Assert.IsTrue(EndlessWaves.HasBoss(9));
    }

    [Test]
    public void HealthScaleGrowsSlowlyAndStopsAtWaveTwenty()
    {
        Assert.AreEqual(1f, EndlessWaves.HealthScale(0), 0.0001f);
        Assert.Greater(EndlessWaves.HealthScale(10), EndlessWaves.HealthScale(5));
        Assert.AreEqual(EndlessWaves.HealthScale(20), EndlessWaves.HealthScale(500), 0.0001f);
    }

    [Test]
    public void SpawnTierRisesFromZeroToThree()
    {
        Assert.AreEqual(0, EndlessWaves.SpawnTier(0));
        Assert.AreEqual(1, EndlessWaves.SpawnTier(2));
        Assert.AreEqual(3, EndlessWaves.SpawnTier(6));
        Assert.AreEqual(3, EndlessWaves.SpawnTier(99));
    }

    [Test]
    public void BackgroundCyclesEveryThreeWaves()
    {
        Assert.AreEqual(0, EndlessWaves.BackgroundIndex(0, 4));
        Assert.AreEqual(0, EndlessWaves.BackgroundIndex(2, 4));
        Assert.AreEqual(1, EndlessWaves.BackgroundIndex(3, 4));
        Assert.AreEqual(0, EndlessWaves.BackgroundIndex(12, 4));
        Assert.AreEqual(0, EndlessWaves.BackgroundIndex(5, 0));
    }

    [Test]
    public void WaveBonusGrowsWithTheWave()
    {
        Assert.AreEqual(200, EndlessWaves.WaveBonus(0));
        Assert.AreEqual(1000, EndlessWaves.WaveBonus(4));
    }
}
