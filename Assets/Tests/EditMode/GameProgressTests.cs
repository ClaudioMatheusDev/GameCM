using NUnit.Framework;

public class GameProgressTests
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
    public void DefaultsAreFirstStageAndFullVolume()
    {
        Assert.AreEqual(0, GameProgress.UnlockedStage);
        Assert.AreEqual(0, GameProgress.HighScore);
        Assert.AreEqual(1f, GameProgress.Volume);
        Assert.IsFalse(GameProgress.HasFullscreenPreference);
    }

    [Test]
    public void UnlockStageOnlyMovesForward()
    {
        GameProgress.UnlockStage(2);
        GameProgress.UnlockStage(1);

        Assert.AreEqual(2, GameProgress.UnlockedStage);
    }

    [Test]
    public void SubmitScoreReportsNewRecordOnlyWhenHigher()
    {
        Assert.IsTrue(GameProgress.SubmitScore(500));
        Assert.IsFalse(GameProgress.SubmitScore(500));
        Assert.IsFalse(GameProgress.SubmitScore(100));
        Assert.IsTrue(GameProgress.SubmitScore(900));
        Assert.AreEqual(900, GameProgress.HighScore);
    }

    [Test]
    public void VolumeIsClamped()
    {
        GameProgress.Volume = 5f;
        Assert.AreEqual(1f, GameProgress.Volume);

        GameProgress.Volume = -1f;
        Assert.AreEqual(0f, GameProgress.Volume);
    }
}
