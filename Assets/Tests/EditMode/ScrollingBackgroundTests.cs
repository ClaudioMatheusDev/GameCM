using NUnit.Framework;

public class ScrollingBackgroundTests
{
    [Test]
    public void NoExtraTilesWhenTheCentralTileAlreadyCoversTheDistance()
    {
        Assert.AreEqual(0, ScrollingBackground.TilesNeeded(10f, 30f));
        Assert.AreEqual(0, ScrollingBackground.TilesNeeded(15f, 30f));
    }

    [Test]
    public void ExtraTilesCoverTheRemainingDistance()
    {
        Assert.AreEqual(1, ScrollingBackground.TilesNeeded(16f, 30f));
        Assert.AreEqual(1, ScrollingBackground.TilesNeeded(45f, 30f));
        Assert.AreEqual(2, ScrollingBackground.TilesNeeded(46f, 30f));
    }

    [Test]
    public void InvalidWidthNeedsNoTiles()
    {
        Assert.AreEqual(0, ScrollingBackground.TilesNeeded(100f, 0f));
    }
}
