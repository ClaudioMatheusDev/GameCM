using NUnit.Framework;

public class DifficultyTests
{
    private string originalPrefix;

    [SetUp]
    public void SetUp()
    {
        originalPrefix = GameProgress.KeyPrefix;
        GameProgress.KeyPrefix = "GameCM.Tests.";
        GameProgress.ResetAll();
    }

    [TearDown]
    public void TearDown()
    {
        GameProgress.ResetAll();
        GameProgress.KeyPrefix = originalPrefix;
    }

    [Test]
    public void DefaultIsNormal()
    {
        Assert.AreEqual(Difficulty.Normal, GameProgress.Difficulty);
    }

    [Test]
    public void DifficultyIsSavedAndClamped()
    {
        GameProgress.Difficulty = Difficulty.Hard;
        Assert.AreEqual(Difficulty.Hard, GameProgress.Difficulty);

        GameProgress.Difficulty = (Difficulty)99;
        Assert.AreEqual(Difficulty.Hard, GameProgress.Difficulty);
    }

    [Test]
    public void NextCyclesThroughAllLevels()
    {
        Assert.AreEqual(Difficulty.Normal, DifficultyRules.Next(Difficulty.Easy));
        Assert.AreEqual(Difficulty.Hard, DifficultyRules.Next(Difficulty.Normal));
        Assert.AreEqual(Difficulty.Easy, DifficultyRules.Next(Difficulty.Hard));
    }

    [Test]
    public void HarderLevelsAreTougher()
    {
        DifficultyRules easy = DifficultyRules.Get(Difficulty.Easy);
        DifficultyRules normal = DifficultyRules.Get(Difficulty.Normal);
        DifficultyRules hard = DifficultyRules.Get(Difficulty.Hard);

        Assert.Less(easy.ZombieHealth, normal.ZombieHealth);
        Assert.Greater(hard.ZombieHealth, normal.ZombieHealth);
        Assert.Greater(hard.ZombieDamage, normal.ZombieDamage);
        Assert.Greater(easy.PlayerHealthBonus, hard.PlayerHealthBonus);
        Assert.Greater(easy.HealDropChance, hard.HealDropChance);
    }

    [Test]
    public void ScaleZombieCountKeepsAtLeastOne()
    {
        DifficultyRules easy = DifficultyRules.Get(Difficulty.Easy);
        Assert.AreEqual(0, easy.ScaleZombieCount(0));
        Assert.AreEqual(1, easy.ScaleZombieCount(1));
        Assert.AreEqual(4, DifficultyRules.Get(Difficulty.Normal).ScaleZombieCount(4));
        Assert.Greater(DifficultyRules.Get(Difficulty.Hard).ScaleZombieCount(10), 10);
    }
}
