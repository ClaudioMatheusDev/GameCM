using NUnit.Framework;

public class DifficultyProfileTests
{
    [Test]
    public void EasyIsMoreForgivingAndHardMoreDemandingThanNormal()
    {
        DifficultyProfile easy = DifficultyProfile.Get(Difficulty.Easy);
        DifficultyProfile normal = DifficultyProfile.Get(Difficulty.Normal);
        DifficultyProfile hard = DifficultyProfile.Get(Difficulty.Hard);

        Assert.Greater(easy.PlayerHealthDelta, normal.PlayerHealthDelta);
        Assert.Less(hard.PlayerHealthDelta, normal.PlayerHealthDelta);
        Assert.Less(easy.ZombieSpeed, hard.ZombieSpeed);
        Assert.Less(easy.ScoreMultiplier, normal.ScoreMultiplier);
        Assert.Greater(hard.ScoreMultiplier, normal.ScoreMultiplier);
    }

    [Test]
    public void NormalDoesNotChangeAnything()
    {
        DifficultyProfile normal = DifficultyProfile.Get(Difficulty.Normal);

        Assert.AreEqual(0, normal.PlayerHealthDelta);
        Assert.AreEqual(3, normal.ScaleCount(3));
        Assert.AreEqual(100, normal.ScaleScore(100));
    }

    [Test]
    public void ScaleCountNeverDropsBelowOne()
    {
        Assert.AreEqual(1, DifficultyProfile.Get(Difficulty.Easy).ScaleCount(1));
        Assert.AreEqual(4, DifficultyProfile.Get(Difficulty.Hard).ScaleCount(3));
    }
}
