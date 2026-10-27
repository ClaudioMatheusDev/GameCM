using NUnit.Framework;

public class ScoreCalculatorTests
{
    [Test]
    public void KillPointsGrowWithComboAndAreCapped()
    {
        Assert.AreEqual(100, ScoreCalculator.KillPoints(1));
        Assert.AreEqual(125, ScoreCalculator.KillPoints(2));
        Assert.AreEqual(300, ScoreCalculator.KillPoints(9));
        Assert.AreEqual(300, ScoreCalculator.KillPoints(50));
    }

    [Test]
    public void KillPointsIgnoreInvalidCombo()
    {
        Assert.AreEqual(100, ScoreCalculator.KillPoints(0));
        Assert.AreEqual(100, ScoreCalculator.KillPoints(-3));
    }

    [Test]
    public void ParTimeScalesWithZombieCount()
    {
        Assert.AreEqual(15f, ScoreCalculator.ParTime(0));
        Assert.AreEqual(51f, ScoreCalculator.ParTime(3));
    }

    [Test]
    public void TimeBonusIsZeroWhenOverPar()
    {
        Assert.AreEqual(100, ScoreCalculator.TimeBonus(20f, 30f));
        Assert.AreEqual(0, ScoreCalculator.TimeBonus(40f, 30f));
    }

    [Test]
    public void StarsRewardNoDamageAndSpeed()
    {
        Assert.AreEqual(3, ScoreCalculator.Stars(0, 10f, 30f));
        Assert.AreEqual(2, ScoreCalculator.Stars(0, 40f, 30f));
        Assert.AreEqual(2, ScoreCalculator.Stars(2, 10f, 30f));
        Assert.AreEqual(1, ScoreCalculator.Stars(2, 40f, 30f));
    }

    [Test]
    public void HealthBonusNeverNegative()
    {
        Assert.AreEqual(600, ScoreCalculator.HealthBonus(3));
        Assert.AreEqual(0, ScoreCalculator.HealthBonus(-1));
    }
}
