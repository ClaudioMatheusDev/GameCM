using NUnit.Framework;

public class PowerUpStateTests
{
    [Test]
    public void ActivateStartsTheTimerAndTickCountsDown()
    {
        PowerUpState state = new PowerUpState();

        state.Activate(PowerUpType.Haste, 8f);
        state.Tick(3f);

        Assert.IsTrue(state.IsActive(PowerUpType.Haste));
        Assert.AreEqual(5f, state.Remaining(PowerUpType.Haste), 0.0001f);
        Assert.IsFalse(state.IsActive(PowerUpType.Shield));
    }

    [Test]
    public void PowerUpExpiresAndNeverGoesNegative()
    {
        PowerUpState state = new PowerUpState();
        state.Activate(PowerUpType.Strength, 2f);

        state.Tick(5f);

        Assert.IsFalse(state.IsActive(PowerUpType.Strength));
        Assert.AreEqual(0f, state.Remaining(PowerUpType.Strength));
    }

    [Test]
    public void PickingTheSameOneAgainRefreshesInsteadOfStacking()
    {
        PowerUpState state = new PowerUpState();
        state.Activate(PowerUpType.Shield, 8f);
        state.Tick(6f);

        state.Activate(PowerUpType.Shield, 8f);

        Assert.AreEqual(8f, state.Remaining(PowerUpType.Shield), 0.0001f);
    }

    [Test]
    public void DifferentPowerUpsRunIndependently()
    {
        PowerUpState state = new PowerUpState();
        state.Activate(PowerUpType.Haste, 4f);
        state.Activate(PowerUpType.Strength, 8f);

        state.Tick(5f);

        Assert.IsFalse(state.IsActive(PowerUpType.Haste));
        Assert.IsTrue(state.IsActive(PowerUpType.Strength));
    }

    [Test]
    public void ClearRemovesEverything()
    {
        PowerUpState state = new PowerUpState();
        state.Activate(PowerUpType.Haste, 4f);
        state.Activate(PowerUpType.Shield, 4f);

        state.Clear();

        Assert.IsFalse(state.IsActive(PowerUpType.Haste));
        Assert.IsFalse(state.IsActive(PowerUpType.Shield));
    }
}
