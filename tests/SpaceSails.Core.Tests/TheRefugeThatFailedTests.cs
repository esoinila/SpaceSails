using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #619 · <b>THE REFUGE THAT FAILED — ONE, PLACED, AND NEVER THE ONLY ONE ON ITS FLOOR.</b>
///
/// <para>Owner, filing it: <i>"But a SECOND refuge, on one floor, that failed — that is the story. Not a
/// dice roll on every refuge. One, placed, deliberate, and never the only one on its floor, so it can never
/// kill anybody who trusted the instrument."</i> And the constraint in the same breath: <i>"it must be
/// visibly distinguishable from a working refuge BEFORE a captain commits their remaining air to reaching
/// it — the tracker must not paint it as a haven. And canon holds hardest here (§13.8): it explains
/// nothing."</i></para>
///
/// <para>What this file owns is the LAW and the WORDS. The tracker, the suit and the card are the client's
/// half and are guarded in <c>TheSealOnTheRefugeTests</c>.</para>
///
/// <para><b>Every guard here is written so that a world with no failed refuge in it goes RED.</b> That is
/// not decoration: "the plan never promises a dead door" and "the tracker never paints it as a haven" are
/// both satisfied perfectly by a build that generates no failed refuge at all, which is this repository's
/// fifth named bug class — a green test that asserts nothing because the world cannot tell pass from fail.
/// So every sweep counts what it found and demands it in quantity.</para>
/// </summary>
// #251 · NOT [SlowGate]: measured 3.5 s over 7 tests (2026-09-20), under the documented 10 s cut. Four of
// them are full build sweeps of a hundred sites, which is what the gate's roster is for — this one simply
// does not reach it, and a row padded with a harmless name is what THE_GATE_CanTellPassFromFail refuses.
public sealed class TheRefugeThatFailedTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The same net the refuge law is swept over — ten sites the game actually ships and ninety
    /// generated ones, because the law is about the GENERATOR and not about a fixed list.</summary>
    private static IEnumerable<string> ManySites()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
    }

    // ── THE LAW ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFailedRefugeIsNeverTheOnlyRefugeOnItsFloor()
    {
        // THE LAW OF THIS ISSUE, swept over every generated floor of every site. On any floor that carries
        // the room that failed there is ALSO a refuge whose door cycles — so a captain who read REFUGE on
        // the lift panel, walked a tank down a rib and found a welded door has somewhere to go that is not
        // back up the shaft with what is left in the bottle.
        //
        // It is read off the BUILT floor rather than off the law, because the law promising two rooms and
        // the generator producing one is exactly the failure this sweep exists to catch (#608's own lesson:
        // a refuge chosen before the rooms are laid is an index that sometimes names nothing).
        var bad = new List<string>();
        int floorsWithOne = 0, floorsWithout = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                int failed = 0, working = 0;
                foreach (UndergroundComplex.Refuge r in floor.Refuges)
                {
                    if (r.State == UndergroundComplex.RefugeState.Failed) { failed++; } else { working++; }
                }

                if (failed == 0)
                {
                    floorsWithout++;
                    continue;
                }
                floorsWithOne++;

                if (failed > 1)
                {
                    bad.Add($"  {body} B{-level}: {failed} welded refuges on one floor — it is an event.");
                }
                if (working == 0)
                {
                    bad.Add($"  {body} B{-level}: the room that failed is the ONLY refuge on this floor. "
                        + "The plan promised air here and the door will not open.");
                }

                // …and the site agrees it happened here, so the carve, the card, the weld and the plate
                // cannot come to two answers about which room the story is in.
                if (!UndergroundComplex.RefugeThatFailedIsOn(body, level))
                {
                    bad.Add($"  {body} B{-level}: a welded refuge stands on a floor the site says has none.");
                }
            }
        }

        // THE HALF THAT LETS THIS FAIL. Without it, a generator that never carves a second refuge passes
        // every assertion above with a perfect score and this file becomes decoration.
        Assert.True(floorsWithOne > 10,
            $"only {floorsWithOne} floor(s) in a hundred sites carry the refuge that failed — this sweep "
            + "proved nothing, because every assertion in it is vacuously true of a world with none.");
        Assert.True(floorsWithout > 500,
            $"only {floorsWithout} floor(s) have no welded refuge. It is one site in four, on one floor.");
        Assert.True(bad.Count == 0,
            $"the failed refuge is never the only refuge on its floor — {bad.Count} floor(s):\n"
            + string.Join("\n", bad.Take(25)));
    }

    [Fact]
    public void TheFloorsOwnRefugeIsNeverTheOneThatFailed()
    {
        // The law said in the one sentence every other surface in the game asks. The lift panel row, the
        // dead-air card, the suit and the plate all read StateOfTheRefugeOn, and #619's whole safety
        // property is that none of them can be handed FAILED.
        //
        // What makes it able to fail: it counts the floors whose SITE has the story and demands them in
        // quantity, so a build where nothing ever fails cannot hide behind this.
        int storied = 0, dead = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.StateOfTheRefugeOn(body, level) is not { } state)
                {
                    continue;
                }
                dead++;
                Assert.NotEqual(UndergroundComplex.RefugeState.Failed, state);
                if (UndergroundComplex.RefugeThatFailedIsOn(body, level))
                {
                    storied++;

                    // …and on that floor the plan's own refuge still holds or is merely dry: the thing the
                    // captain was promised is still there, one room over from the thing that happened.
                    Assert.True(UndergroundComplex.RefugeStillHolds(state));
                }
            }
        }
        Assert.True(dead > 500, $"only {dead} dead floor(s) swept — this net proves nothing.");
        Assert.True(storied > 10,
            $"only {storied} floor(s) carry the story — a world with no failed refuge satisfies the "
            + "assertion above without ever being asked the question.");
    }

    [Fact]
    public void TheDoorIsWeldedAndTheWeldWearsThePlate()
    {
        // "The door is welded from the inside." In this building a door that never opens with a real wall
        // behind it is a LockedDoor, so the weld is one — which is how the renderer, the walkers, the
        // Reevers and the A* audit all learn about it from a list they already read, instead of each being
        // told separately that one room is special.
        //
        // EVERY way into that chamber, not the first one: a room with two doors and one weld is a room with
        // a way in, and the whole beat is that there is not one.
        var bad = new List<string>();
        int welded = 0, ways = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Refuge r in floor.Refuges)
                {
                    if (r.State != UndergroundComplex.RefugeState.Failed)
                    {
                        // …and nothing else in the building wears this plate. A weld that could turn up on
                        // an ordinary store room would make the renderer swallow that room's sign console.
                        if (string.Equals(r.Sign, UndergroundComplex.RefugeFailedGlyph, StringComparison.Ordinal))
                        {
                            bad.Add($"  {body} B{-level}: a working refuge is signed as out of service.");
                        }
                        continue;
                    }
                    welded++;

                    // The door the plate hangs on and the press is made at is a REAL way out of that room,
                    // not the room's centre: a console inside a welded chamber is an affordance nobody can
                    // reach.
                    bool doorIsAWay = false;
                    int weldsHere = 0;
                    foreach (UndergroundComplex.LockedDoor l in floor.Locked)
                    {
                        if (UndergroundComplex.IsTheWeldedRefugePlate(l.Sign))
                        {
                            weldsHere++;
                            double mx = (l.X1 + l.X2) / 2, my = (l.Y1 + l.Y2) / 2;
                            if (Math.Abs(mx - r.DoorX) < 0.001 && Math.Abs(my - r.DoorY) < 0.001)
                            {
                                doorIsAWay = true;
                            }
                        }
                    }
                    ways += weldsHere;

                    if (weldsHere == 0)
                    {
                        bad.Add($"  {body} B{-level}: the refuge that failed has no weld on it at all.");
                    }
                    if (!doorIsAWay)
                    {
                        bad.Add($"  {body} B{-level}: the plate hangs at ({r.DoorX:F1}, {r.DoorY:F1}), "
                            + "which is not one of the welded ways into that room.");
                    }
                    if (Math.Abs(r.DoorX - r.X) < 0.001 && Math.Abs(r.DoorY - r.Y) < 0.001)
                    {
                        bad.Add($"  {body} B{-level}: the press is at the room's CENTRE, on the far side of "
                            + "the weld — an affordance a captain can see and can never reach.");
                    }
                }
            }
        }

        Assert.True(welded > 10, $"only {welded} welded refuge(s) swept — this guard proved nothing.");
        Assert.True(ways >= welded, "a welded room with fewer welds than rooms.");
        Assert.True(bad.Count == 0,
            $"the weld is not sound on {bad.Count} floor(s):\n" + string.Join("\n", bad.Take(25)));
    }

    [Fact]
    public void TheFailedRefugeIsADetourLikeEveryOtherRefuge()
    {
        // It is a refuge, not a prop: it is taken out of the same pool by the same preference, so it stands
        // down a rib and not beside the lift. A story room set down where the captain trips over it is a
        // game pointing at its own scenery, and #608 paid for this number once already (MinRefugeDetourDu
        // shipped for an hour as a threshold that selected every room in the building).
        int measured = 0, close = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Refuge r in floor.Refuges)
                {
                    if (r.State != UndergroundComplex.RefugeState.Failed)
                    {
                        continue;
                    }
                    measured++;
                    foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                    {
                        double dx = r.X - car.X, dy = r.Y - car.Y;
                        if (Math.Sqrt((dx * dx) + (dy * dy)) < UndergroundComplex.MinRefugeDetourDu * 0.5)
                        {
                            close++;
                        }
                    }
                }
            }
        }
        Assert.True(measured > 10, $"only {measured} welded refuge(s) measured — this guard proved nothing.");
        Assert.Equal(0, close);
    }

    /// <summary>#619 · <b>THE CHEAT ROCK STILL REACHES THE BEAT.</b>
    ///
    /// <para><c>?secretlab=sealed</c> parks a rock whose body id is <c>secret-lab-site-sealed</c>, and a
    /// site's whole shape is seeded off that id — so the URL reaches the refuge that failed by NAME rather
    /// than by overriding anything from the client. The risk that buys is the one the deep rock's own
    /// docblock names: a change to the seeding could quietly move the story off that site, and the cheat
    /// would go on booting a perfectly ordinary building while the testing-links row promised a welded
    /// door.</para>
    ///
    /// <para>The FLOOR is pinned too, because the row in <c>docs/testing-links-the-hive.md</c> says
    /// <c>&amp;floor=3</c> — and a document full of confident URLs that land on the wrong floor is worse
    /// than no document (§13.19's own lesson).</para></summary>
    [Fact]
    public void TheSealedCheatRockStillCarriesTheStoryOnB3()
    {
        const string Rock = "secret-lab-site-sealed";
        Assert.Equal(-3, UndergroundComplex.FailedRefugeFloorOf(Rock));
        Assert.Contains(-3, UndergroundComplex.FloorsOf(Rock));

        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(Rock, -3, Field);
        Assert.Single(floor.Refuges, r => r.State == UndergroundComplex.RefugeState.Failed);
        Assert.Contains(floor.Refuges, r => r.State != UndergroundComplex.RefugeState.Failed);
        Assert.Contains(floor.Locked, l => UndergroundComplex.IsTheWeldedRefugePlate(l.Sign));
    }

    // ── THE WORDS ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheAuthoredLinesAreTheAuthoredLines()
    {
        // Retyped from the canon rather than read off the constants, for ThePapersOwnHeadsTests' reason: a
        // guard asserting RefugeFailedGlyph == RefugeFailedGlyph passes on any sentence anybody ever writes
        // into it.
        Assert.Equal("🫁 REFUGE — OUT OF SERVICE — REPORTED", UndergroundComplex.RefugeFailedGlyph);
        Assert.Equal("🫁 refuge · dark", UndergroundComplex.RefugeDarkCaption);
        Assert.Equal("🫁 THE REFUGE THAT FAILED", StoryBeats.Title(StoryBeats.Beat.RefugeFailed));
        Assert.Equal("art/refuge-failed.jpg", StoryBeats.ArtFile(StoryBeats.Beat.RefugeFailed));
        Assert.Equal(
            "The door is welded from the inside, and the weld is careful. The gauge beside it reads what "
            + "the room has, which is nothing. On the rack outside are more suits than this floor ever had "
            + "staff, and the reservoir on the deck was emptied by somebody who then did not leave.",
            StoryBeats.Caption(StoryBeats.Beat.RefugeFailed));

        // …and the plate the tracker's legend is about is the plate on the door, so the two instruments are
        // speaking about one room.
        Assert.Equal(
            UndergroundComplex.RefugeFailedGlyph,
            UndergroundComplex.RefugeGlyphFor(UndergroundComplex.RefugeState.Failed));
        Assert.True(UndergroundComplex.IsTheWeldedRefugePlate(UndergroundComplex.RefugeFailedGlyph));
        Assert.False(UndergroundComplex.IsTheWeldedRefugePlate(UndergroundComplex.RefugeGlyph));
        Assert.False(UndergroundComplex.IsTheWeldedRefugePlate(UndergroundComplex.RefugeDryGlyph));
    }

    [Fact]
    public void TheFieldBookKeepsThePlaceAndTheAuthoredGist()
    {
        // #741's law: a subject is declared by the AUTHOR of the sentence, and this one is about a PLACE.
        // Not a person — nobody in that room has a name, and a book that invented one for the sake of a
        // thread would be the game doing the detecting.
        int seen = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.RefugeThatFailedIsOn(body, level))
                {
                    continue;
                }
                seen++;
                string floorName = UndergroundComplex.NameOf(body, level);
                Assert.Equal(
                    $"the refuge on {floorName} — welded from the inside, suits for more than the floor, "
                    + "the tank drained by someone who stayed",
                    UndergroundComplex.FailedRefugeNoteLine(body, level));
                Assert.Equal(
                    CaseSubjects.Line(CaseSubjects.Place(floorName)),
                    UndergroundComplex.FailedRefugeSubjects(body, level));
            }
        }
        Assert.True(seen > 10, $"only {seen} note(s) written — this guard proved nothing.");
    }

    [Fact]
    public void NothingAboutItExplainsAnything()
    {
        // §13.8, at the one door in the building where the temptation to explain is worst. The card, the
        // plate, the legend and the book entry are swept for the reserved word and for every other word
        // that would name a cause, a maker or an outcome.
        //
        // The list is deliberately wider than the reserved word alone: "Reever" is the thing that may never
        // be printed, and "they", "it came", "escaped", "experiment" are the ways a line about this room
        // would leak a cause without printing it.
        string[] never =
        [
            "reever", "reevers", "old one", "old ones", "kaamos", "restore", "specimen",
            "escaped", "experiment", "creature", "thing", "monster", "attack", "attacked",
            "murder", "killed", "died", "body", "bodies", "blood", "rescue", "quarantine",
            "containment", "breach", "age", "aged", "perished", "decay", "neglect",
        ];

        var surfaces = new List<(string What, string Text)>
        {
            ("the plate", UndergroundComplex.RefugeFailedGlyph),
            ("the legend", UndergroundComplex.RefugeDarkCaption),
            ("the card title", StoryBeats.Title(StoryBeats.Beat.RefugeFailed)),
            ("the card caption", StoryBeats.Caption(StoryBeats.Beat.RefugeFailed)),
            ("the field book", UndergroundComplex.FailedRefugeNoteLine("luna", -2)),
        };

        var bad = new List<string>();
        foreach ((string what, string text) in surfaces)
        {
            string lower = text.ToLowerInvariant();
            foreach (string word in never)
            {
                // Word-boundary, so "staff" is not "stay" and "storage" is not "age".
                foreach (int at in Occurrences(lower, word))
                {
                    bool leftClear = at == 0 || !char.IsLetter(lower[at - 1]);
                    int end = at + word.Length;
                    bool rightClear = end >= lower.Length || !char.IsLetter(lower[end]);
                    if (leftClear && rightClear)
                    {
                        bad.Add($"  {what} says \"{word}\": {text}");
                    }
                }
            }
        }
        Assert.True(bad.Count == 0,
            "this room explains nothing (§13.8) and these lines explain something:\n"
            + string.Join("\n", bad));

        // The sweep can tell pass from fail: the same machinery finds a planted word.
        Assert.NotEmpty(Occurrences("the seal failed from age", "age"));
    }

    private static IEnumerable<int> Occurrences(string haystack, string needle)
    {
        for (int at = haystack.IndexOf(needle, StringComparison.Ordinal);
            at >= 0;
            at = haystack.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            yield return at;
        }
    }
}
