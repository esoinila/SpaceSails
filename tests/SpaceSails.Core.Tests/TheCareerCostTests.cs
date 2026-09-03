using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 beat 4 · CAREER-COST NPCs — the colleague who kept going is not dead.
///
/// <para>#1074, verbatim: <i>"The colleague who kept going is not dead — 'reassigned where their skills are
/// most needed' (intranet register, pairs with the Lost Property line). Asking colleagues gets sincere
/// blankness or a changed subject; one keeps the missing one's mug on the shelf and will not say why."</i>
/// </para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names, and the revert is
/// quoted on it — a guard that has never failed is a guard nobody has checked (#587's lesson).</para>
///
/// <para><b>THE WORLD IS DERIVED AND NEVER TYPED.</b> The sweeps walk an id family nothing else in the repo
/// asks about (<see cref="Grounds"/>), the canteen is the one the real generator carved, and the population
/// is asserted to be a population — a sweep that found no grounds would pass every negative law in this file
/// for the wrong reason, which is the fifth named bug class exactly.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheCareerCostTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls — about one in fifty, so
    /// a ten-site sample would say nothing. The same number and the same reasoning as the two sibling beats'
    /// suites.</summary>
    private const int Probes = 3000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The watches every sweep asks about. A room is dealt per shift, so a guard that only ever
    /// looked at watch zero would be asserting one deal and calling it a law.</summary>
    private static readonly long[] Watches = [0, 1, 2, 3, 4, 5];

    private static List<string>? _grounds;

    /// <summary>Every ground in the sweep that actually has halls — asserted to be a real population.</summary>
    private static List<string> Grounds() => _grounds ??= Sweep();

    private static List<string> Sweep()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"career-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body) && UndergroundComplex.TopPressurisedFloor(body) is not null)
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 20,
            $"only {found.Count} of {Probes} generated grounds had halls — this proves little.");
        return found;
    }

    /// <summary>Close the working on these grounds for the length of one guard, and put the world back
    /// afterwards whatever happens.</summary>
    private static IDisposable Stopped(params string[] bodies) => new Restore(bodies, care: false);

    /// <summary>…and take them into official care as well, which is what a stopped ground becomes one window
    /// later. It stays in the STOP register while it is in care — <c>PreservationZone.Note</c> only ever adds
    /// grounds that are already closed and nothing ever takes one out — so this is the second of the two
    /// states the beat has to cover, and it is covered by the same one question.</summary>
    private static IDisposable Preserved(params string[] bodies) => new Restore(bodies, care: true);

    private sealed class Restore : IDisposable
    {
        public Restore(string[] bodies, bool care)
        {
            StopOrder.Install([.. bodies]);
            PreservationZone.Install(care ? [.. bodies] : []);
        }

        public void Dispose()
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    /// <summary>The upper canteen on this floor, built by the REAL generator — never a typed-in room. A guard
    /// handed a room nobody carved is a guard that cannot tell pass from fail.</summary>
    private static UndergroundComplex.Amenity? TheCanteenOn(string body, int level)
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
        {
            if (CanteenRegulars.PeopleSitHere(body, level, a))
            {
                return a;
            }
        }
        return null;
    }

    /// <summary>Everything a captain could read in that room this watch, as one string — the plates, the
    /// lines, the coordinates and the board. What "unchanged by one character" is measured on.</summary>
    private static string RoomText(string body, int level, UndergroundComplex.Amenity room, long watch)
    {
        var parts = new List<string>();
        foreach (CanteenRegulars.Seated s in CanteenRegulars.Sitting(body, level, room, watch))
        {
            parts.Add($"{s.X:F4}|{s.Y:F4}|{s.Plate}|{s.Line}");
        }
        foreach (CanteenBoard.Notice n in CanteenBoard.Pinned(body, level, room))
        {
            parts.Add($"{n.Head}|{n.Body}|{n.Pairs}");
        }
        return string.Join("\n", parts);
    }

    // ══ LAW 1 · ONLY WHERE THE OFFICE HAS BEEN ═══════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE ROW AND THE TWO REGULARS EXIST ON A STOPPED OR PRESERVED GROUND AND NOWHERE ELSE.
    ///
    /// <para>The beat is what an order costs a roster, so it may only stand where an order stands. On an
    /// ordinary ground the row is not on the cork and neither of the two is in the room, on any watch; on a
    /// closed one both are, on every watch; and a ground in official care is a closed ground that nobody came
    /// back to argue with, so it answers exactly the same — through the same one question and never a second
    /// condition somebody has to keep agreeing.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the <c>StopOrder.On</c> arm removed from
    /// <c>CanteenRegulars.Seating</c> — <i>"nobody on career-ground-115 B1 is off the closed shift"</i>; and
    /// the <c>CareerCost.RegisterRow</c> pin taken out of <c>CanteenBoard.Pinned</c> — <i>"the register row is
    /// not on the cork of a stopped ground"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRowAndTheTwoRegularsStandOnlyWhereTheOrderStands()
    {
        int stoodOnClosed = 0, stoodInCare = 0;

        foreach (string body in Grounds().Take(12))
        {
            int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
            if (TheCanteenOn(body, top) is not { } room)
            {
                continue;
            }

            // ── ORDINARY. Nothing of this beat is anywhere in that room, on any watch.
            foreach (long watch in Watches)
            {
                Assert.DoesNotContain(
                    CanteenRegulars.Sitting(body, top, room, watch), s => IsOurs(s.Plate));
            }
            Assert.DoesNotContain(CanteenBoard.Pinned(body, top, room), n => n.Head == CareerCost.RegisterHead);

            // ── CLOSED, and then IN CARE. Both, every watch, on the same room.
            foreach (bool care in new[] { false, true })
            {
                using (care ? Preserved(body) : Stopped(body))
                {
                    Assert.True(StopOrder.On(body));
                    Assert.Equal(care, PreservationZone.On(body));

                    IReadOnlyList<CanteenBoard.Notice> up = CanteenBoard.Pinned(body, top, room);
                    Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);          // four slots, never a fifth
                    Assert.Single(up, n => n.Head == CareerCost.RegisterHead);
                    Assert.Single(up, n => n.Head == CanteenBoard.RosterHead);  // the shift is still listed

                    foreach (long watch in Watches)
                    {
                        var plates = CanteenRegulars.Sitting(body, top, room, watch)
                            .Select(s => s.Plate).ToList();
                        Assert.Contains(CareerCost.ColleaguePlate, plates);
                        Assert.Contains(CareerCost.MugPlate, plates);

                        // …and the mason is NOT: a ground gets one of the two outcomes and never both, so the
                        // man whose whole testimony is a resurfacing job is not sitting in a room the office
                        // got to first.
                        Assert.DoesNotContain(Burial.MasonPlate, plates);
                    }

                    if (care)
                    {
                        stoodInCare++;
                    }
                    else
                    {
                        stoodOnClosed++;
                    }
                }
            }
        }

        Assert.True(stoodOnClosed >= 8 && stoodInCare >= 8,
            $"the sweep only reached {stoodOnClosed} closed and {stoodInCare} cared-for canteens.");
    }

    // ══ LAW 2 · THE NAME IS OFF THE SHIFT THE BOARD IS STILL ADVERTISING ═════════════════════════════════

    /// <summary>
    /// #1074 · THE ROW NAMES A HAND, THE NAME IS ONE THE GAME PRINTS, AND THE ROW BELONGS TO THAT SHIFT.
    ///
    /// <para>The canon authored no name and this beat writes none: the hand is dealt off the ground from the
    /// one pool of people-names the game already prints (<see cref="FieldDossier.GivenNames"/> /
    /// <see cref="FieldDossier.FamilyNames"/>, where every stranger reconstructed out of their own kit gets
    /// theirs). A name that existed only here would be a character this beat had minted, and the doctrine's
    /// first law is that nobody in this arc is a character.</para>
    ///
    /// <para><b>"On the working's roster" is asserted in the only form the world can carry it</b>: the row is
    /// pinned on the same cork as the rota that still lists the shift, and its pairing — the board's own
    /// internal consistency law, never rendered — is the colleague who is sitting in that room and who
    /// answers about the transfer. The board never says any of that out loud; the player either joins the two
    /// pieces of paper to the man at the table or does not.</para>
    ///
    /// <para>And it is STABLE: a register row that named a different hand between excursions would be a
    /// register nobody kept.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the hand seeded on the WATCH as well as the ground — <i>"the row
    /// on career-ground-115 named two different hands"</i>; and <c>RegisterBody</c> shortened to the authored
    /// line alone — <i>"the register row names nobody"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRowNamesOneHandOffTheShiftAndTheNameIsOneTheGamePrints()
    {
        var given = new HashSet<string>(FieldDossier.GivenNames, StringComparer.Ordinal);
        var family = new HashSet<string>(FieldDossier.FamilyNames, StringComparer.Ordinal);
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (string body in Grounds().Take(40))
        {
            string hand = CareerCost.HandOn(body);
            Assert.Equal(hand, CareerCost.HandOn(body));            // the same hand on every visit

            string[] parts = hand.Split(' ');
            Assert.Equal(2, parts.Length);
            Assert.Contains(parts[0], given);
            Assert.Contains(parts[1], family);
            named.Add(hand);

            // The row itself: the hand, then the authored line, and nothing else on the paper.
            CanteenBoard.Notice row = CareerCost.RegisterRow(body);
            Assert.Equal(CareerCost.RegisterHead, row.Head);
            Assert.Equal($"{hand}. {CareerCost.ReassignedLine}", row.Body);

            // …and it belongs to somebody in the room, which is the board's own law (#709). The pairing is
            // never rendered and this is the only thing that can hold it.
            Assert.Equal(CareerCost.ColleaguePlate, row.Pairs);
            Assert.Contains(row.Pairs, CanteenRegulars.AllProse());
        }

        // A real population and not one name forty times — a dealer that always answered the same would pass
        // every assertion above and ship one hand for the whole solar system.
        Assert.True(named.Count > 10, $"the sweep only ever named {named.Count} hand(s).");
    }

    // ══ LAW 3 · AN UNSTOPPED WORLD DOES NOT MOVE BY ONE CHARACTER ════════════════════════════════════════

    /// <summary>
    /// #1074/#1063 · THE ROOM AND THE BOARD ON A GROUND NOBODY HAS STOPPED ARE WHAT THEY ALWAYS WERE.
    ///
    /// <para>The mason's own law, one rung along and with two people on it instead of one. Three things
    /// together are the whole of it: the two new regulars are dealt out of a pool that stops SHORT of them so
    /// no ordinary deal can reach them; the register row is not in the board's catalogue at all so no
    /// ordinary deal can reach it either; and <b>the two pool lengths the dice are rolled against are exactly
    /// what they were</b>, because a length that moved would re-deal every canteen in the game while every
    /// assertion about the new people still passed.</para>
    ///
    /// <para>The last clause is the one that could rot silently, so it is also checked from the other end: a
    /// world where OTHER grounds are stopped hands an unstopped ground byte-identical rooms — same people,
    /// same chairs, same cork, character for character, watch by watch.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the deal run against <c>Cast.Length - 1</c> again —
    /// <i>"Assert.Equal() Failure: Expected: 10 Actual: 12"</i>, and then the sweep naming the room on
    /// career-ground-6 as seating a colleague off a shift nobody stopped; and the register row added to the
    /// catalogue's ordinary pool — <i>"REGISTER — PERSONNEL is pinned on a ground nobody has stopped"</i>.
    /// </para>
    /// </summary>
    [Fact]
    public void AGroundNobodyStoppedIsUnchangedByOneCharacter()
    {
        // The lengths the dice are rolled against. These are the numbers that re-deal the world.
        Assert.Equal(10, CanteenRegulars.OrdinaryCastSize);
        Assert.Equal(10, CanteenBoard.OrdinaryNoticeSize);

        List<string> grounds = Grounds();
        var elsewhere = grounds.Skip(20).Take(15).ToArray();
        int compared = 0;

        foreach (string body in grounds.Take(20))
        {
            int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
            if (TheCanteenOn(body, top) is not { } room)
            {
                continue;
            }

            foreach (long watch in Watches)
            {
                string quiet = RoomText(body, top, room, watch);

                Assert.DoesNotContain(CareerCost.ColleaguePlate, quiet, StringComparison.Ordinal);
                Assert.DoesNotContain(CareerCost.MugPlate, quiet, StringComparison.Ordinal);
                Assert.DoesNotContain(CareerCost.RegisterHead, quiet, StringComparison.Ordinal);
                Assert.DoesNotContain(CareerCost.ReassignedLine, quiet, StringComparison.Ordinal);

                // …and it is the SAME room in a world where fifteen other workings have been closed.
                using (Stopped(elsewhere))
                {
                    Assert.False(StopOrder.On(body));
                    Assert.Equal(quiet, RoomText(body, top, room, watch));
                }
                compared++;
            }
        }

        Assert.True(compared >= 60, $"the sweep only compared {compared} room-watches.");
    }

    // ══ LAW 4 · THREE SENTENCES, VERBATIM, AND THEY ARE ALL OF IT ════════════════════════════════════════

    /// <summary>
    /// #1074 · THE THREE AUTHORED LINES ARE CHARACTER FOR CHARACTER WHAT THE CANON PASS WROTE, AND THIS BEAT
    /// PUBLISHES NO OTHER PROSE.
    ///
    /// <para>The strings are pinned against a copy of the canon pass of 2026-09-03 kept in this file, so an
    /// "improved" comma fails the build. The <b>sweep is by reflection</b> over every public string this beat
    /// declares, exactly as <c>TheStopOrderAtTheDigTests</c> sweeps its own, so a helpful sentence added
    /// tomorrow cannot escape the canon grep by not being in <c>AllProse</c>: everything the type publishes
    /// must be one of the three lines, one of the two plates, the register heading, or the glass the canteen
    /// already draws.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> a fourth const added to <c>CareerCost</c> (<c>"He left on the
    /// Thursday car."</c>) — <i>"CareerCost publishes prose the canon pass did not author: Departed"</i>; and
    /// a full stop taken off the mug line — <i>"Assert.Equal() Failure … That stays where it is"</i>.</para>
    /// </summary>
    [Fact]
    public void TheThreeLinesAreVerbatimAndAreTheOnlyProseThisBeatPublishes()
    {
        // ── The canon pass of 2026-09-03, copied here so the constants are checked against the ISSUE and not
        //    against themselves.
        Assert.Equal("Reassigned where their skills are most needed.", CareerCost.ReassignedLine);
        Assert.Equal("Transferred, I think. Administration would know where.", CareerCost.ColleagueLine);
        Assert.Equal("That stays where it is.", CareerCost.MugLine);

        // ── AllProse says what it publishes…
        var prose = CareerCost.AllProse().ToList();
        Assert.Equal(prose.Count, prose.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(CareerCost.ReassignedLine, prose);
        Assert.Contains(CareerCost.ColleagueLine, prose);
        Assert.Contains(CareerCost.MugLine, prose);

        // ── …and reflection says whether that is the truth. Everything this type declares as a string is one
        //    of the six AllProse rows or the canteen's own glass, and nothing else.
        var allowed = new HashSet<string>(prose, StringComparer.Ordinal) { CareerCost.MugGlyph };
        foreach (FieldInfo f in typeof(CareerCost)
                     .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (f.GetValue(null) is string s)
            {
                Assert.True(allowed.Contains(s),
                    $"CareerCost publishes prose the canon pass did not author: {f.Name} = \"{s}\"");
            }
        }

        // ── The two plates are the room's own idiom and are not sentences: shouted, glyphed, and dull.
        foreach (string plate in new[] { CareerCost.ColleaguePlate, CareerCost.MugPlate })
        {
            Assert.StartsWith(CanteenRegulars.Glyph, plate, StringComparison.Ordinal);
            Assert.Equal(plate.ToUpperInvariant(), plate);
            Assert.DoesNotContain('.', plate);
        }
        Assert.Equal(CareerCost.RegisterHead, CareerCost.RegisterHead.ToUpperInvariant());
    }

    // ══ LAW 5 · NOBODY SAYS MISSING, NOBODY SAYS DEAD, NOBODY NAMES THE WORKING ══════════════════════════

    /// <summary>§8's reserved word, and every word that would settle the question this beat exists to leave
    /// open. <i>missing</i> and <i>dead</i> are the two the canon names outright; the working, the dig and the
    /// shaft are the thing none of these three may name; and <i>Authority</i> is the office that signs the
    /// plate at the seal and the sign at the fence and has no business in a canteen.</summary>
    private static readonly string[] Forbidden =
    [
        "missing", "dead", "died", "disappear", "vanish", "body", "authority", "ministry", "minister",
        "working", "dig", "shaft", "gallery", "seal", "excavat", "site",
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon",
    ];

    /// <summary>
    /// #1074/#672 · NOTHING ON THIS BEAT'S PATH SAYS THE THING.
    ///
    /// <para>Nobody says <i>missing</i>. Nobody says <i>dead</i>. Nobody names the working. None of the three
    /// mentions the Authority — the enforcer is an office, and it signs orders, not conversations. A Scully
    /// hears a transfer and a colleague who liked somebody, and is not being fooled: that is what the
    /// paperwork records and what the colleague believes.</para>
    ///
    /// <para>The sweep walks the rendered row for a real ground as well as the constants, because the row is
    /// the one string in this beat that is COMPOSED — a dealt name and an authored sentence — and a
    /// composition is exactly where a forbidden word would arrive without anybody typing it.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the word planted in the colleague's line — <i>"CareerCost says
    /// what it may not: authority in Transferred, I think. The Authority would know where."</i>.</para>
    /// </summary>
    [Fact]
    public void NothingOnThisBeatsPathSaysMissingOrDeadOrNamesTheWorking()
    {
        var strings = new List<(string Where, string Text)>();
        foreach (string s in CareerCost.AllProse())
        {
            strings.Add(("CareerCost.AllProse", s));
        }
        foreach (string body in Grounds().Take(30))
        {
            strings.Add(($"row on {body}", CareerCost.RegisterBody(body)));
        }

        foreach ((string where, string text) in strings)
        {
            foreach (string bad in Forbidden)
            {
                Assert.False(text.Contains(bad, StringComparison.OrdinalIgnoreCase),
                    $"{where} says what it may not: {bad} in {text}");
            }
        }
    }

    // ══ LAW 6 · THE MUG IS A MUG THE ROOM ALREADY OWNS, BEHIND HER CHAIR, INSIDE THE ROOM ════════════════

    /// <summary>
    /// #1074 · THE MUG IS ON THE SHELF BEHIND HER SEAT AND IT IS NOT A NEW PICTURE.
    ///
    /// <para><i>"A mug on the shelf behind one regular's seat… The mug is the whole testimony."</i> So: it is
    /// the glass the canteen already draws itself with (<c>Interior.TheKeep.Glyph</c>) and no art is added;
    /// it stands clear of the chair ring, so it is behind whoever is sitting there rather than on top of
    /// them; and it is INSIDE the room, because a mug drawn through a wall is the drawn world and the built
    /// world disagreeing about a thing a captain can see.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the clamp removed from <c>CareerCost.MugAt</c> (the behind-side
    /// coordinate returned unconditionally) — <i>"the mug on career-ground-115 stands outside its own
    /// room"</i>; and the standoff cut to zero — <i>"the mug is inside the chair ring"</i>.</para>
    /// </summary>
    [Fact]
    public void TheMugStandsBehindHerChairAndInsideTheRoom()
    {
        Assert.Equal(SpaceSails.Core.Interior.TheKeep.Glyph, CareerCost.MugGlyph);
        Assert.DoesNotContain("art/", CareerCost.MugGlyph, StringComparison.OrdinalIgnoreCase);

        int placed = 0;
        foreach (string body in Grounds().Take(15))
        {
            int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
            if (TheCanteenOn(body, top) is not { } room)
            {
                continue;
            }

            using (Stopped(body))
            {
                foreach (long watch in Watches)
                {
                    foreach (CanteenRegulars.TableSeat seat in
                             CanteenRegulars.Tables(body, top, room, watch))
                    {
                        if (seat.Plate != CareerCost.MugPlate)
                        {
                            continue;
                        }

                        (double mx, double my) = CareerCost.MugAt(seat, room);
                        Assert.True(room.Contains(mx, my),
                            $"the mug on {body} B{-top} stands outside its own room ({mx:F2}, {my:F2}).");
                        Assert.Equal(seat.X, mx, 6);
                        Assert.True(Math.Abs(my - seat.Y) > CanteenRegulars.ChairRingDu,
                            $"the mug on {body} B{-top} is inside the chair ring.");
                        placed++;
                    }
                }
            }
        }

        Assert.True(placed >= 30, $"the sweep only placed {placed} mug(s).");
    }

    // ══ LAW 7 · IT SURVIVES THE RELOAD ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · A ROW THAT FORGOT ACROSS A RELOAD WOULD PUT A MAN BACK ON A ROSTER AN OFFICE HAD MOVED HIM OFF.
    ///
    /// <para>The beat keeps no state of its own: it is a pure reading of the stop register, which the vault
    /// already carries (<c>hallsStopped</c> / <c>hallsPreserved</c>). That is exactly what has to be proved
    /// rather than assumed — so a save is written, round-tripped through the real serializer, installed from
    /// what came BACK, and the room is asked again.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the register installed from the ORIGINAL save rather than the
    /// loaded one, with <c>HallsStopped</c> dropped from the payload — <i>"the row is gone after a
    /// reload"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRowAndTheRegularsSurviveTheVault()
    {
        string body = Grounds()[0];
        int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
        UndergroundComplex.Amenity room = Assert.IsType<UndergroundComplex.Amenity>(TheCanteenOn(body, top));

        var save = new Vault
        {
            Version = Vault.CurrentVersion,
            Progress = new ProgressSection { HallsStopped = [body], HallsPreserved = [body] },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(save));
        Assert.Equal([body], loaded.Progress!.HallsStopped);
        Assert.Equal([body], loaded.Progress.HallsPreserved);

        try
        {
            StopOrder.Install(loaded.Progress.HallsStopped);
            PreservationZone.Install(loaded.Progress.HallsPreserved);

            Assert.Single(CanteenBoard.Pinned(body, top, room), n => n.Head == CareerCost.RegisterHead);
            foreach (long watch in Watches)
            {
                var plates = CanteenRegulars.Sitting(body, top, room, watch).Select(s => s.Plate).ToList();
                Assert.Contains(CareerCost.ColleaguePlate, plates);
                Assert.Contains(CareerCost.MugPlate, plates);
            }
        }
        finally
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    private static bool IsOurs(string plate) =>
        plate == CareerCost.ColleaguePlate || plate == CareerCost.MugPlate;
}
