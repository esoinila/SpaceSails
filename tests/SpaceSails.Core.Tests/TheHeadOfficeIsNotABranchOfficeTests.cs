namespace SpaceSails.Core.Tests;

/// <summary>
/// Beat 8 (#1406) · R1-F12 and R1-F14. The head office answers in the branch-office vocabulary (#411) but it
/// is not a branch office: its car lists the whole building, and no floor of it holds gate paper.
///
/// <para><b>RED</b> by deleting the head-office arm at the top of the band loop in
/// <c>UndergroundComplex.LiftPanel</c> (the panel is B1–B4 plus an OTHER SHAFT again), and by deleting the
/// <c>rolled == Haul.Key</c> arm in <c>InRoom</c> (a SHAFT 4 countersignature rolls on B12 again).</para>
/// </summary>
public class TheHeadOfficeIsNotABranchOfficeTests
{
    private const string Hq = KaamosLore.IceMoonBodyId;

    [Fact]
    public void TheCarListsEveryFloorOfTheBuildingWhereverItIsAsked()
    {
        int depth = UndergroundComplex.DepthOf(Hq);
        foreach (int from in new[] { 0, -1, -4, -5, -9, -12, -23, -24 })
        {
            IReadOnlyList<UndergroundComplex.LiftStop> stops =
                UndergroundComplex.LiftPanel(Hq, from, []);

            Assert.Equal(depth, stops.Min(s => s.Level));
            Assert.Contains(stops, s => s.Level == 0);
            for (int f = -1; f >= depth; f--)
            {
                UndergroundComplex.LiftStop stop = Assert.Single(stops, s => s.Level == f);
                Assert.Null(stop.Refusal);
            }
            Assert.DoesNotContain(stops, s => s.Name.Contains("OTHER SHAFT", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void NoRoomOfTheHeadOfficeHoldsAGateCardWhateverItRolls()
    {
        int rooms = 0;
        foreach (int level in UndergroundComplex.FloorsOf(Hq))
        {
            for (int room = 0; room < 40; room++)
            {
                rooms++;
                UndergroundComplex.Haul haul = UndergroundComplex.InRoom(Hq, level, room);
                Assert.NotEqual(UndergroundComplex.Haul.Key, haul);
            }
        }
        Assert.True(rooms > 500, "the sweep must be wide enough to have met a Key roll on a branch office");

        // …and the sentence about the owners is a branch office's, whoever hands the line a card.
        string said = UndergroundComplex.KeyLine(Hq, -12, new UndergroundComplex.AuthorityCard(Hq, 3));
        Assert.DoesNotContain("stopped paying", said, StringComparison.Ordinal);
        Assert.Null(UndergroundComplex.CardInRoom(Hq, -12));
    }

    [Fact]
    public void ABranchOfficeStillRollsItsKeys()
    {
        // The sweep above would pass on a world that never rolls a Key at all; prove one that does exists.
        bool any = false;
        foreach (string body in new[] { "ganymede", "io", "europa", "callisto", "titan", "miranda" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (int room = 0; room < 40; room++)
                {
                    any |= UndergroundComplex.InRoom(body, level, room) == UndergroundComplex.Haul.Key;
                }
            }
        }
        Assert.True(any);
    }
}
