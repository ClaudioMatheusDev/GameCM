using NUnit.Framework;

public class ScoreBoardTests
{
    [Test]
    public void InsertKeepsDescendingOrderAndReportsRank()
    {
        int[] board = { 900, 500, 100 };

        int[] updated = ScoreBoard.Insert(board, 600, out int rank);

        CollectionAssert.AreEqual(new[] { 900, 600, 500, 100 }, updated);
        Assert.AreEqual(2, rank);
    }

    [Test]
    public void InsertTrimsToMaxEntries()
    {
        int[] board = { 500, 400, 300, 200, 100 };

        int[] updated = ScoreBoard.Insert(board, 250, out int rank);

        CollectionAssert.AreEqual(new[] { 500, 400, 300, 250, 200 }, updated);
        Assert.AreEqual(4, rank);
    }

    [Test]
    public void ScoreBelowTheFullBoardDoesNotEnter()
    {
        int[] board = { 500, 400, 300, 200, 100 };

        int[] updated = ScoreBoard.Insert(board, 50, out int rank);

        CollectionAssert.AreEqual(board, updated);
        Assert.AreEqual(0, rank);
    }

    [Test]
    public void TiesGoBelowExistingScores()
    {
        int[] updated = ScoreBoard.Insert(new[] { 500 }, 500, out int rank);

        CollectionAssert.AreEqual(new[] { 500, 500 }, updated);
        Assert.AreEqual(2, rank);
    }

    [Test]
    public void ZeroOrNegativeScoresAreIgnored()
    {
        int[] updated = ScoreBoard.Insert(new int[0], 0, out int rank);

        Assert.IsEmpty(updated);
        Assert.AreEqual(0, rank);
    }

    [Test]
    public void FirstScoreIsRankOne()
    {
        ScoreBoard.Insert(new int[0], 300, out int rank);

        Assert.AreEqual(1, rank);
    }

    [Test]
    public void ParseSortsDropsInvalidEntriesAndRoundTrips()
    {
        int[] parsed = ScoreBoard.Parse("100,abc,900,,500,-4");

        CollectionAssert.AreEqual(new[] { 900, 500, 100 }, parsed);
        CollectionAssert.AreEqual(parsed, ScoreBoard.Parse(ScoreBoard.Serialize(parsed)));
        Assert.IsEmpty(ScoreBoard.Parse(null));
    }
}
