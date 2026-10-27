using NUnit.Framework;
using UnityEngine;

public class GameClockTests
{
    [TearDown]
    public void TearDown()
    {
        GameClock.Reset();
    }

    [Test]
    public void HitStopSlowsTimeAndResetRestoresIt()
    {
        GameClock.HitStopActive = true;
        Assert.Less(Time.timeScale, 0.1f);
        Assert.Greater(Time.timeScale, 0f);

        GameClock.Reset();
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void PauseWinsOverHitStop()
    {
        GameClock.HitStopActive = true;
        GameClock.Paused = true;
        Assert.AreEqual(0f, Time.timeScale);

        GameClock.Paused = false;
        Assert.Less(Time.timeScale, 0.1f);

        GameClock.HitStopActive = false;
        Assert.AreEqual(1f, Time.timeScale);
    }
}
