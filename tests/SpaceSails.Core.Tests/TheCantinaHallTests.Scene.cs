using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// <b>THE THIN SCENE, AND THE ONE CARVER</b> — sections (e), (i) and (n) of
/// <see cref="TheCantinaHallTests"/>.
///
/// <para>What this part owns is what happens when the captain sits down at a stranger's table — small talk,
/// the round, and your leave, and nothing else — the quiet rule that makes a file loud in the hall and quiet
/// in a cabinet, and the ONESOURCE law that both halls are carved by the one carver rather than by two that
/// happen to agree.</para>
/// </summary>
public sealed partial class TheCantinaHallTests
{
    // ── (e) THE THIN SCENE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A stranger's table offers exactly three moves and Ask-about-work is not one of them.
    ///
    /// <para><b>Proven RED</b> by handing the crowd the named cast's scene — <c>SceneFor(Who.Hand)</c> for a
    /// stranger, the obvious shortcut when a second counterpart kind arrives:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: ["smalltalk", "round", "leave"]
    /// Actual:   ["smalltalk", "smalltalk2", "round", "show", "work", "leave"]
    /// </code>
    /// </summary>
    [Fact]
    public void AStrangersTableIsSmallTalkTheRoundAndYourLeaveAndNothingElse()
    {
        Encounter.Scene scene = CanteenTable.StrangerScene("◈ CAGE CREW, OFF SHIFT");

        Assert.Equal(
            [CanteenTable.SmallTalk, CanteenTable.Round, CanteenTable.Leave],
            scene.Moves.Select(m => m.Id).ToList());

        Assert.DoesNotContain(scene.Moves, m => m.Id == CanteenTable.Work);
        Assert.DoesNotContain(scene.Moves, m => m.Rolled);

        // The framework's own free-exit law applies to it without this file restating it.
        Assert.True(Encounter.CanAlwaysLeave(scene));

        // The round is the ordinary round at the ordinary price, and it buys the ordinary +1: a stranger's
        // table is where you warm up cheap, which is the whole reason it is on the panel at all.
        Encounter.Move round = scene.Moves.Single(m => m.Id == CanteenTable.Round);
        Assert.Equal(Encounter.Requirement.Credits, round.Needs);
        Assert.Equal(CanteenTable.RoundPrice, round.Credits);
        Assert.Equal(
            Encounter.ModifierStep,
            Encounter.Modifiers(new Encounter.Situation(RoundBought: true)).Single().Value);

        // …and it reads as the person the deck drew, in the room the deck drew them in.
        Assert.Equal("◈ CAGE CREW, OFF SHIFT", scene.Counterpart);
        Assert.Equal(CanteenTable.Setting, scene.Setting);
        Assert.Equal(CanteenTable.WaveIn(CanteenTable.Who.Stranger), scene.Opening);

        // Small talk says the bark they were dealt and changes nothing at all.
        CanteenTable.Answer said = CanteenTable.StrangerSaid(CanteenRegulars.Barks[3]);
        Assert.Equal(CanteenRegulars.Barks[3], said.Line);
        Assert.False(said.GrantsChit || said.ClosesTheAsk || said.HardensTable || said.TeachesTheHouse);
        Assert.Equal(0, said.NervePips);
        Assert.Null(said.Note);
    }

    // ── (i) THE QUIET RULE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A file on the table is LOUD in the hall and is not loud in a cabinet — the same slip, the same
    /// person, a different room.
    ///
    /// <para><b>Proven RED</b> by removing the location check (<c>PutOnTheTable</c> ignoring
    /// <c>quiet</c>), which is exactly what the code did before #751 and is the only thing that makes a
    /// cabinet a cabinet:</para>
    /// <code>
    /// the ask closed at a table the counter cannot see.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_QUIET_RULE_AFileIsLoudInTheHallAndNotInACabinet()
    {
        var file = new Satchel.Item(Satchel.Kind.Dirt, "dirt:somebody");

        CanteenTable.Answer loud = CanteenTable.PutOnTheTable(file, CanteenTable.Who.Hand, quiet: false);
        Assert.True(loud.ClosesTheAsk, "the counter has eyes in the hall — the ask must close.");
        Assert.Equal(CanteenTable.DirtLine, loud.Line);

        CanteenTable.Answer quiet = CanteenTable.PutOnTheTable(file, CanteenTable.Who.Hand, quiet: true);
        Assert.False(quiet.ClosesTheAsk, "the ask closed at a table the counter cannot see.");
        Assert.Equal(CanteenTable.QuietLine, quiet.Line);
        Assert.Equal(CanteenTable.CabinetLeverageNote, quiet.Note);

        // The two answers do not merely differ in a flag — the counterpart SAYS something different, which
        // is how the mechanic is taught: by observation, never by tooltip.
        Assert.NotEqual(loud.Line, quiet.Line);

        // …and the rule is about the ROOM and never about the paper: everything else on the table reads the
        // same in both rooms.
        var manifest = new Satchel.Item(Satchel.Kind.Paper, "paper:manifest");
        Assert.Equal(
            CanteenTable.PutOnTheTable(manifest, CanteenTable.Who.Fitter, quiet: false),
            CanteenTable.PutOnTheTable(manifest, CanteenTable.Who.Fitter, quiet: true));
    }

    /// <summary>And a cabinet's own top is the one Core marks quiet — nothing else in the building is. The
    /// fact the rule reads has exactly one source.</summary>
    [Fact]
    public void ONESOURCE_OnlyACabinetsTopIsQuiet()
    {
        int quiet = 0, loud = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    foreach (CanteenRegulars.TableSeat t in CanteenRegulars.Tables(body, level, a))
                    {
                        bool inCabinet = a.Hall is { } h && h.CabinetAt(t.X, t.Y) is not null;
                        Assert.Equal(inCabinet, t.Quiet);
                        Assert.Equal(inCabinet, t.Cabinet > 0);
                        if (t.Quiet)
                        {
                            quiet++;
                        }
                        else
                        {
                            loud++;
                        }
                    }
                }
            }
        }

        Assert.True(quiet > 30, $"only {quiet} cabinet tops swept — this proved little.");
        Assert.True(loud > 300, $"only {loud} ordinary tops swept — this proved little.");
    }

    // ── (n) ONE CARVE, TWO CUSTOMERS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// The B1 hall and the B17 mess come out of ONE implementation, and the only line where they differ is
    /// the seat target.
    ///
    /// <para>Source-shape, and it has to be: a second copy of the carve would agree with the first for
    /// exactly as long as nobody edited either, which is the whole table at the top of
    /// <c>UndergroundComplex.cs</c>. What is under test is that there is only one carve left to change.</para>
    ///
    /// <para><b>Proven RED</b> by adding the overload a "the mess needs its own version" change would
    /// naturally reach for:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 1
    /// Actual:   2
    /// </code>
    /// </summary>
    [Fact]
    public void ONESOURCE_BothHallsAreCarvedByTheOneCarver()
    {
        // #870 · The module is one partial class spread over UndergroundComplex*.cs. Same needles, same
        // code, new paths — the source read here is the concatenation of every part.
        string core = string.Concat(Directory
            .EnumerateFiles(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core"), "UndergroundComplex*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

        Assert.Equal(1, Occurrences(core, "private static HallSite? CarveHall("));

        // #813 · …and TWO callers of it, which is the same law and not a weakening of it.
        //
        // The Manhattan ruling gave the top floor's bar a different piece of GROUND to stand on — the near
        // band of the park's block, chosen out of the block's own segments — while the staff mess two
        // hundred metres down still stands on a rib's room column, because there is no block down there to
        // stand in. Two grounds, two callers, and the thing this guard is actually about is untouched: there
        // is exactly ONE piece of code that knows how to lay out a hall, and both callers hand it the same
        // five numbers. A second carver is the bug; a second address is not.
        //
        // Stated as "no more than the two the file explains" rather than as "one", so a third one still goes
        // red — which is the whole value of the original line.
        Assert.Equal(2, Occurrences(core, "hallSite = CarveHall("));
        Assert.Contains("#813 · …and on the block's floor it is not carved off a rib at all.", core,
            StringComparison.Ordinal);

        // The bill, the pitch and the box are asked for once each — no "if it is the mess" fork anywhere in
        // the geometry. The ONE place the two rooms differ is the seat target they are handed.
        Assert.Equal(1, Occurrences(core, "public static int HallSeatsFor("));
        Assert.Contains("use == Comfort.StaffCanteen ? ImpliedComplement(bodyId) : CantinaHallSeats",
            core, StringComparison.Ordinal);

        // …and behaviourally: both rooms produce the same KIND of object off the same field, so a caller
        // that can read one can read the other without knowing which it has.
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string body in Bodies)
        {
            foreach ((int? level, UndergroundComplex.Comfort use) in new (int?, UndergroundComplex.Comfort)[]
            {
                (UndergroundComplex.TopPressurisedFloor(body), UndergroundComplex.Comfort.UpperCanteen),
                (UndergroundComplex.StaffCanteenFloor(body), UndergroundComplex.Comfort.StaffCanteen),
            })
            {
                if (level is not { } l || HallOn(body, l, use) is not { } a || a.Hall is not { } hall)
                {
                    continue;
                }

                kinds.Add(use.ToString());
                Assert.True(hall.X1 > hall.X0 && hall.Y1 > hall.Y0);
                Assert.True(hall.Contains(a.X, a.Y),
                    $"{body} B{-l}: the {use}'s own fixture console is outside its box.");
                Assert.True(hall.Contains(hall.BoardX, hall.BoardY));
                Assert.True(hall.Contains(hall.PlateX, hall.PlateY));
                Assert.All(a.Tables, t => Assert.True(hall.Contains(t.X, t.Y)));
            }
        }

        Assert.Equal(2, kinds.Count);
    }

    private static int Occurrences(string haystack, string needle)
    {
        int found = 0, at = 0;
        while ((at = haystack.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
        {
            found++;
            at += needle.Length;
        }
        return found;
    }
}
