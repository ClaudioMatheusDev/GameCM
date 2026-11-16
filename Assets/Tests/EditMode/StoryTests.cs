using NUnit.Framework;

public class StoryTests
{
    [Test]
    public void VisibleCharactersGrowWithTimeAndStopAtTheEnd()
    {
        Assert.AreEqual(0, CutsceneTiming.VisibleCharacters(0f, 40f, 10));
        Assert.AreEqual(4, CutsceneTiming.VisibleCharacters(0.1f, 40f, 10));
        Assert.AreEqual(10, CutsceneTiming.VisibleCharacters(5f, 40f, 10));
    }

    [Test]
    public void VisibleCharactersHandleInvalidInput()
    {
        Assert.AreEqual(0, CutsceneTiming.VisibleCharacters(1f, 40f, 0));
        Assert.AreEqual(0, CutsceneTiming.VisibleCharacters(-1f, 40f, 10));
        Assert.AreEqual(0, CutsceneTiming.VisibleCharacters(1f, 0f, 10));
    }

    [Test]
    public void IntroAndEndingHaveTextEvenWithoutImages()
    {
        var intro = Story.Intro(null);
        var ending = Story.Ending(null);

        Assert.AreEqual(4, intro.Count);
        Assert.AreEqual(2, ending.Count);
        foreach (CutsceneSlide slide in intro)
        {
            Assert.IsNotEmpty(slide.Text);
            Assert.IsNull(slide.Image);
        }
    }

    [Test]
    public void StageNamesFollowTheBackgroundOrder()
    {
        Assert.AreEqual("Rua", Story.StageName(Story.StreetStage));
        Assert.AreEqual("Metrô", Story.StageName(Story.MetroStage));
        Assert.AreEqual("Telhado", Story.StageName(Story.RooftopStage));
        Assert.AreEqual(string.Empty, Story.StageName(99));
        Assert.AreEqual(string.Empty, Story.StageName(-1));
    }
}
