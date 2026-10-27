using NUnit.Framework;

public class SpawnPlacementTests
{
    private const float Start = -10f;
    private const float End = 10f;
    private const float Margin = 1f;
    private const float MinDistance = 3f;

    [Test]
    public void SpawnStaysInsideStageAndAwayFromPlayer()
    {
        for (float playerX = Start; playerX <= End; playerX += 1f)
        {
            for (float side = 0f; side < 1f; side += 0.25f)
            {
                for (float t = 0f; t <= 1f; t += 0.25f)
                {
                    float x = SpawnPlacement.Pick(playerX, Start, End, Margin, MinDistance, side, t);
                    Assert.GreaterOrEqual(x, Start + Margin - 0.001f);
                    Assert.LessOrEqual(x, End - Margin + 0.001f);
                    Assert.GreaterOrEqual(System.Math.Abs(x - playerX), MinDistance - 0.001f, "playerX=" + playerX);
                }
            }
        }
    }

    [Test]
    public void SpawnUsesOnlyTheFreeSideWhenPlayerIsNearAnEdge()
    {
        Assert.Greater(SpawnPlacement.Pick(-9f, Start, End, Margin, MinDistance, 0f, 0.5f), -9f);
        Assert.Less(SpawnPlacement.Pick(9f, Start, End, Margin, MinDistance, 0.9f, 0.5f), 9f);
    }

    [Test]
    public void SpawnFallsBackToFarthestEdgeWhenNothingFits()
    {
        Assert.AreEqual(1f, SpawnPlacement.Pick(-1f, -2f, 2f, 1f, 10f, 0.5f, 0.5f));
        Assert.AreEqual(-1f, SpawnPlacement.Pick(1f, -2f, 2f, 1f, 10f, 0.5f, 0.5f));
    }
}
