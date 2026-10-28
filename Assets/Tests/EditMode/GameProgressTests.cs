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

    [Test]
    public void StageStarsOnlyImprove()
    {
        GameProgress.SubmitStageStars(1, 2);
        GameProgress.SubmitStageStars(1, 1);
        GameProgress.SubmitStageStars(2, 9);

        Assert.AreEqual(2, GameProgress.GetStageStars(1));
        Assert.AreEqual(ScoreCalculator.MaxStars, GameProgress.GetStageStars(2));
        Assert.AreEqual(0, GameProgress.GetStageStars(0));
    }

    [Test]
    public void RegisterScoreBuildsRankingAndHighScore()
    {
        Assert.AreEqual(1, GameProgress.RegisterScore(500));
        Assert.AreEqual(1, GameProgress.RegisterScore(900));
        Assert.AreEqual(2, GameProgress.RegisterScore(600));
        Assert.AreEqual(0, GameProgress.RegisterScore(0));

        CollectionAssert.AreEqual(new[] { 900, 600, 500 }, GameProgress.TopScores);
        Assert.AreEqual(900, GameProgress.HighScore);
    }

    [Test]
    public void OptionsDefaultToNormalWithEffectsOn()
    {
        Assert.IsTrue(GameProgress.ScreenEffects);
        Assert.AreEqual(Difficulty.Normal, GameProgress.Difficulty);

        GameProgress.ScreenEffects = false;
        GameProgress.Difficulty = Difficulty.Hard;

        Assert.IsFalse(GameProgress.ScreenEffects);
        Assert.AreEqual(Difficulty.Hard, GameProgress.Difficulty);
    }
}
