using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1063 · THE BURIAL — the first customer of #677's disclosure clock, and the watcher reaction that is not
/// the world declining but PEOPLE hiding the truth themselves.
///
/// <para>Owner (2026-09-01): <i>"suppose the people them selves were told really convincingly that they need
/// to hide the truth… all the people suddenly were told and obeyed like automatons them selves hiding the
/// truth and then just forgot they did it… all that mud."</i> And the register: <i>"the cheerful… of course
/// we do a ton of work to raise our streets for no apparent purpose is the cherry on the cake."</i></para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names; the revert is quoted
/// on each one, in the shape this ground has used since #587's lesson — a guard that has never failed is a
/// guard nobody has checked.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, and it is deliberately a family of
/// ids no other suite asks about (<see cref="Grounds"/>). Every sweep in this repo walks <c>probe-moon-{i}</c>
/// and the <c>?found=1</c> cheat rock; the burial register only ever changes the answer for the ids IN it, so
/// a guard here that installs a ground of its own cannot move any other guard's world, whatever order xUnit
/// runs them in. Restoring in a <c>finally</c> is belt as well as braces.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheBurialTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls. The band is about one
    /// site in fifty; a ten-site sample tells you nothing about it. Same number, same reasoning, as
    /// <c>TheFoundBandTests</c>' own sweep.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>EVERY GROUND IN THE SWEEP THAT ACTUALLY HAS HALLS, derived off an id family this file owns
    /// alone — asserted to be a real population rather than merely non-empty, because a population of one
    /// proves nothing and an empty one passes every negative law in this file for the wrong reason (the fifth
    /// named bug class).</summary>
    private static List<string> Grounds()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"burial-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated burial grounds had halls — this proves little.");
        return found;
    }

    /// <summary>…and the same sweep's grounds with NO halls, which is what almost every site is. The negative
    /// population every vacuity pair is run over.</summary>
    private static List<string> Plain(int want)
    {
        var plain = new List<string>();
        for (int i = 0; i < Probes && plain.Count < want; i++)
        {
            string body = $"burial-ground-{i}";
            if (!UndergroundComplex.HasFoundBand(body))
            {
                plain.Add(body);
            }
        }
        Assert.True(plain.Count >= want, $"only {plain.Count} plain grounds — the sample IS the population.");
        return plain;
    }

    /// <summary>#1074 · THE GROUNDS THE NEIGHBOURS GET, of those opened in a given world-side window.
    ///
    /// <para>#1074's stop order is the OTHER outcome of this same trigger — one opened ground, one whole
    /// window, the captain off the body — and roughly half of the due grounds are the office's, which
    /// <c>Fill</c> therefore leaves alone. Every guard below that asks whether a ground is filled in has to
    /// ask it of a ground the neighbours get, and it derives that list off the split's own function rather
    /// than typing an id, so a re-seed moves the sample instead of reddening a law about burials.</para>
    /// </summary>
    private static List<string> NeighbourGrounds(long openedInWindow) =>
        [.. Grounds().Where(
            g => !StopOrder.TheOfficeGetsThisOne(new DisclosureClock.Opening(g, openedInWindow)))];

    /// <summary>Install a burial (and the opening that had to come before it) for the length of one guard,
    /// and put the world back afterwards whatever happens.</summary>
    private static IDisposable Buried(params string[] bodies) =>
        Installed([.. bodies], [.. bodies.Select(b => new DisclosureClock.Opening(b, 0))]);

    /// <summary>The works are ON here but the job is not done — the window the notice is up in.</summary>
    private static IDisposable Opened(params string[] bodies) =>
        Installed([], [.. bodies.Select(b => new DisclosureClock.Opening(b, 0))]);

    private static IDisposable Installed(
        IReadOnlyList<string> filled, IReadOnlyList<DisclosureClock.Opening> opened)
    {
        Burial.Install(filled, opened);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() => Burial.Install([], []);
    }

    /// <summary>Which floors of this ground are galleries — asked while nothing is buried, which is the only
    /// time the question has an answer.</summary>
    private static List<int> GalleriesOf(string body) =>
        [.. UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsFound(body, l))];

    // ══ LAW 1 · REMOVE THE ELEMENT, AND REMOVE ITS MARKS ═════════════════════════════════════════════════

    /// <summary>
    /// #1063 · A BURIED GROUND ANSWERS NO FOUND BAND FROM EVERY PUBLIC PREDICATE — the erasure procedure's
    /// clauses (1) and (2), which are one clause in code because the marks are all downstream of the element.
    ///
    /// <para>Every one of these is asked on a REAL gallery-carrying ground, twice: once with nothing buried,
    /// where it must answer yes and thereby prove the guard is holding a world that can tell pass from fail,
    /// and once buried, where it must answer exactly what it answers for a site that never had halls.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>HasFoundBand</c> with the <c>Burial.IsFilled</c> clause taken
    /// out — <i>"Assert.False() Failure … burial-ground-116 B13 still answers IsFound after the fill"</i>.</para>
    /// </summary>
    [Fact]
    public void ABuriedGroundAnswersNoFoundBandFromEveryPublicPredicate()
    {
        var wrong = new List<string>();
        foreach (string body in Grounds().Take(60))
        {
            List<int> galleries = GalleriesOf(body);
            Assert.NotEmpty(galleries);   // the world can tell pass from fail

            int unlistedBottom = UndergroundComplex.UnlistedBottomOf(body);
            int listedBottom = UndergroundComplex.DepthOf(body);

            using (Buried(body))
            {
                if (UndergroundComplex.HasFoundBand(body)) { wrong.Add($"{body}: HasFoundBand"); }
                if (UndergroundComplex.FoundKeyRoomFor(body) is not null)
                {
                    wrong.Add($"{body}: FoundKeyRoomFor");
                }
                if (UndergroundComplex.TrueDepthOf(body) != unlistedBottom)
                {
                    wrong.Add($"{body}: TrueDepthOf {UndergroundComplex.TrueDepthOf(body)} != {unlistedBottom}");
                }
                if (UndergroundComplex.NextShaftBelow(body, unlistedBottom) is not null)
                {
                    wrong.Add($"{body}: NextShaftBelow past the unlisted bottom");
                }
                foreach (int level in galleries)
                {
                    if (UndergroundComplex.IsFound(body, level)) { wrong.Add($"{body} B{-level}: IsFound"); }
                    if (UndergroundComplex.DeclaresDarkness(body, level))
                    {
                        wrong.Add($"{body} B{-level}: DeclaresDarkness");
                    }
                    if (Math.Abs(UndergroundComplex.RoomScaleOn(body, level) - 1.0) > 1e-9)
                    {
                        wrong.Add($"{body} B{-level}: RoomScaleOn");
                    }
                    if (DisclosureClock.OpensOn(body, level)) { wrong.Add($"{body} B{-level}: OpensOn"); }
                    if (UndergroundComplex.FloorsOf(body).Contains(level))
                    {
                        wrong.Add($"{body} B{-level}: still in FloorsOf");
                    }
                }

                // …and the shaft ends at the listed building's own bottom band, which is where it ended
                // before anybody ever dug past it.
                Assert.Equal(unlistedBottom, UndergroundComplex.TrueDepthOf(body));
                Assert.True(listedBottom > unlistedBottom, "the listed bottom is above the unlisted one");
            }
        }

        Assert.True(wrong.Count == 0, "a filled ground still answers for its halls:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// #1063 · …AND THE LIST ABOVE IS COMPLETE. Every public way of asking <c>UndergroundComplex</c> about
    /// the band nobody dug is named in this file, so a predicate added tomorrow cannot quietly become a way
    /// to see a buried ground's halls with every guard still green.
    ///
    /// <para>Swept by reflection rather than typed twice: the assertion is that the set of public members
    /// whose name mentions the found band is EXACTLY the set this file checks or deliberately exempts.</para>
    ///
    /// <para><b>Revert that reddened it:</b> a new <c>public static bool HasFoundBandAnyway(string)</c> added
    /// to the partial — <i>"Assert.Equal() Failure … +HasFoundBandAnyway"</i>.</para>
    /// </summary>
    [Fact]
    public void EveryPublicWayOfAskingAboutTheHallsIsAccountedFor()
    {
        string[] asked =
        [
            // Gated by the burial, and each one exercised in the guard above.
            nameof(UndergroundComplex.HasFoundBand),
            nameof(UndergroundComplex.IsFound),
            nameof(UndergroundComplex.FoundKeyRoomFor),
            // Derived from those two and exercised through them.
            nameof(UndergroundComplex.FoundBandOf),
        ];

        // Deliberate exemptions, each with a reason that has to survive being read out loud.
        string[] exempt =
        [
            // Prose and dice, not questions about a site: they say what a gallery SAYS and how often one
            // holds a record. Neither can be asked of a site at all, so neither can leak one.
            nameof(UndergroundComplex.FoundEmptyRoomLine),
            nameof(UndergroundComplex.FoundRecordFindLine),
            nameof(UndergroundComplex.FoundRecordGist),
            nameof(UndergroundComplex.FoundRecordCard),
            nameof(UndergroundComplex.FoundRecordCardLabel),
            nameof(UndergroundComplex.FoundArrivalLine),
            nameof(UndergroundComplex.FoundRecordOneInN),
            nameof(UndergroundComplex.FoundOneInN),
            nameof(UndergroundComplex.FoundGrowthPerFloor),
            nameof(UndergroundComplex.FoundBandCheatSiteId),
        ];

        var seen = typeof(UndergroundComplex)
            .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            // A property is its own member AND a get_ method; counting both would name every constant twice
            // and would make the roster below a list of compiler artefacts rather than of the game's own API.
            .Where(m => m is not MethodInfo { IsSpecialName: true })
            .Select(m => m.Name)
            .Where(n => n.Contains("Found", StringComparison.Ordinal))
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var accounted = asked.Concat(exempt).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(accounted, seen);
    }

    /// <summary>
    /// #1063 · THE VACUITY TWIN — a ground nobody has filled in is untouched, and so is every ground in a
    /// world where nothing has been buried at all. Clause (3): <b>the town above keeps living.</b>
    ///
    /// <para><b>Revert that reddened it:</b> <c>IsFilled</c> returning true for everything —
    /// <i>"Assert.True() Failure … burial-ground-116 lost its halls with nothing buried"</i>.</para>
    /// </summary>
    [Fact]
    public void WithNothingBuriedNotOneGroundInTheWorldChanges()
    {
        Assert.Empty(Burial.Filled);
        foreach (string body in Grounds().Take(60))
        {
            Assert.True(UndergroundComplex.HasFoundBand(body));
            Assert.NotNull(UndergroundComplex.FoundKeyRoomFor(body));
            Assert.NotEmpty(GalleriesOf(body));
            Assert.Null(UndergroundComplex.SpecimenFloorOf(body));
            Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(body));
        }

        // …and burying one ground does not touch its neighbour, which is what "one work order" means.
        List<string> two = Grounds().Take(2).ToList();
        using (Buried(two[0]))
        {
            Assert.False(UndergroundComplex.HasFoundBand(two[0]));
            Assert.True(UndergroundComplex.HasFoundBand(two[1]));
        }
    }

    // ══ LAW 2 · ONE SPECIMEN IS KEPT ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · CLAUSE (4): the specimen is on the LISTED BOTTOM of a buried ground and on no other floor of
    /// no other site — a short recess off the corridor with one old door at the back of it that does not open,
    /// drawn in the found band's own no-texture idiom.
    ///
    /// <para><b>Reverts that reddened it:</b> <c>SpecimenFloorOf</c> answering <c>UnlistedBottomOf</c> —
    /// <i>"the specimen is on B13 and the listed bottom is B7"</i>; and <c>HasSpecimenOn</c> dropping its
    /// <c>IsFilled</c> clause — <i>"a ground nobody buried is keeping a souvenir"</i>.</para>
    /// </summary>
    [Fact]
    public void TheSpecimenIsOnTheListedBottomOfABuriedGroundAndNowhereElse()
    {
        List<string> grounds = Grounds().Take(30).ToList();
        int kept = 0;

        foreach (string body in grounds)
        {
            int listedBottom = UndergroundComplex.DepthOf(body);

            using (Buried(body))
            {
                Assert.Equal(listedBottom, UndergroundComplex.SpecimenFloorOf(body));

                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                    bool wanted = level == listedBottom;
                    Assert.Equal(wanted, UndergroundComplex.HasSpecimenOn(body, level));
                    Assert.Equal(wanted, floor.TheSpecimen is not null);

                    if (floor.TheSpecimen is { } leaf)
                    {
                        kept++;
                        // A leaf and not a point: it is drawn across the back of the recess, at the shaft
                        // box's own width, which is the width every alcove in this building is cut to.
                        Assert.Equal(2 * UndergroundComplex.ShaftHalf, leaf.X2 - leaf.X1, 6);
                        Assert.Equal(leaf.Y1, leaf.Y2, 6);
                        // …and it is NOT in the ordinary wall list, because the list a segment arrives in is
                        // what decides its ink (#759's law). A leaf drawn as poured concrete would say the
                        // facility built it, which is the one thing it must never say.
                        Assert.DoesNotContain(floor.Walls, w =>
                            Math.Abs(w.X1 - leaf.X1) < 1e-6 && Math.Abs(w.Y1 - leaf.Y1) < 1e-6
                            && Math.Abs(w.X2 - leaf.X2) < 1e-6 && Math.Abs(w.Y2 - leaf.Y2) < 1e-6);
                    }
                }
            }

            // …and it is gone again the moment the ground is not a buried one.
            Assert.Null(UndergroundComplex.SpecimenFloorOf(body));
            Assert.Null(UndergroundComplex.Build(body, listedBottom, Field).TheSpecimen);
        }

        Assert.True(kept >= 30, $"only {kept} specimens over {grounds.Count} buried grounds — this proves little.");

        // And no ground WITHOUT halls ever keeps one, however hard somebody edits a save.
        foreach (string plain in Plain(40))
        {
            using (Buried(plain))
            {
                Assert.Null(UndergroundComplex.SpecimenFloorOf(plain));
                foreach (int level in UndergroundComplex.FloorsOf(plain))
                {
                    Assert.Null(UndergroundComplex.Build(plain, level, Field).TheSpecimen);
                }
            }
        }
    }

    /// <summary>
    /// #1063 · The recess is a RECESS and not a room: it is reachable off the spine, it takes nothing away
    /// from the floor's own rooms, and the fire code has nothing to ask it — exactly as #822's own recess and
    /// the lift's alcove are, and for the reason they are.
    ///
    /// <para><b>Revert that reddened it:</b> <c>CarveSpecimen</c> not handing its mouth to
    /// <c>alcoveMouths</c> — <i>"the recess is sealed behind an unbroken spine face"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRecessOpensOntoTheCorridorAndTakesNoRoomAwayFromTheFloor()
    {
        foreach (string body in Grounds().Take(20))
        {
            int bottom = UndergroundComplex.DepthOf(body);
            UndergroundComplex.FloorPlan before = UndergroundComplex.Build(body, bottom, Field);

            using (Buried(body))
            {
                UndergroundComplex.FloorPlan after = UndergroundComplex.Build(body, bottom, Field);
                UndergroundComplex.Specimen leaf = Assert.IsType<UndergroundComplex.Specimen>(after.TheSpecimen);

                // The town above keeps living: the same rooms, in the same places.
                Assert.Equal(before.TheRooms.Count, after.TheRooms.Count);
                Assert.Equal(before.RoomCentres, after.RoomCentres);
                Assert.DoesNotContain(after.TheRooms, r => r.Contains(leaf.X, leaf.Y - 0.5));

                // The mouth is open: the spine's own face is cut at the recess's width, so there is a gap in
                // the wall run where the pocket meets the corridor and a captain can walk into it.
                (double _, double shaftY) = UndergroundComplex.ShaftAt(Field);
                double mouthY = shaftY + UndergroundComplex.CorridorHalf;
                bool sealed_ = after.Walls.Any(w =>
                    Math.Abs(w.Y1 - mouthY) < 1e-6 && Math.Abs(w.Y2 - mouthY) < 1e-6
                    && w.X1 < leaf.X - 0.5 && w.X2 > leaf.X + 0.5);
                Assert.False(sealed_, $"{body}: the recess mouth is walled over on B{-bottom}");
            }
        }
    }

    // ══ LAW 3 · THE THRESHOLD ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · NO BURIAL BEFORE ONE WHOLE WORLD WINDOW. The reason, written beside the threshold as
    /// <see cref="DisclosureClock"/>'s own docblock requires: a work order takes a shift, and a crew already
    /// standing there with the trucks running would be a fact about THEM — which spends the Scully law.
    ///
    /// <para>The window is the monolith's own, asked through the clock and never re-derived.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>WindowsBeforeFilling = 0</c> — <i>"a ground opened this very
    /// window is already filled"</i>; and <c>IsDue</c> reading <c>&gt;=</c> against a window it computed
    /// itself — <i>"the burial ran on its own clock"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingIsFilledInBeforeAWholeWindowHasPassed()
    {
        double window = DisclosureClock.WindowSeconds;
        Assert.Equal(Monolith.EpochSeconds, window);   // one clock, and it is not this file's

        // #1074 · a ground the NEIGHBOURS get, opened in window four — see NeighbourGrounds.
        string body = NeighbourGrounds(4)[0];

        int gallery = GalleriesOf(body)[0];
        IReadOnlyList<DisclosureClock.Opening> register =
            DisclosureClock.Note([], DisclosureClock.Open(body, gallery, window * 4.0)!.Value);

        // Inside the window it was opened in: nothing, however many times it is asked.
        for (int i = 0; i < 20; i++)
        {
            double t = (window * 4.0) + (window * i / 20.0);
            Assert.Empty(Burial.Fill(register, [], standingOn: null, t));
        }

        // The instant the next window begins, and every window after it.
        Assert.Equal([body], Burial.Fill(register, [], null, window * 5.0));
        Assert.Equal([body], Burial.Fill(register, [], null, window * 50.0));

        // And a ground nobody ever opened is never filled, whatever the clock says.
        Assert.Empty(Burial.Fill([], [], null, window * 500.0));
        Assert.Equal(1, Burial.WindowsBeforeFilling);
    }

    /// <summary>
    /// #1063 · AND NEVER WHILE THE CAPTAIN IS ON THAT BODY. The neighbours do not fill a hall while the
    /// captain is standing in it — a crew that walked past him with the trucks would be a thing that happened
    /// TO him and therefore a thing he could describe.
    ///
    /// <para><b>Revert that reddened it:</b> the <c>standingOn</c> clause dropped from <c>Fill</c> —
    /// <i>"Assert.Empty() Failure … the ground closed under a captain who was standing on it"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingIsFilledInWhileTheCaptainIsStandingOnIt()
    {
        List<string> grounds = NeighbourGrounds(0).Take(3).ToList();   // #1074 · see NeighbourGrounds
        double window = DisclosureClock.WindowSeconds;

        IReadOnlyList<DisclosureClock.Opening> register = [];
        foreach (string body in grounds)
        {
            register = DisclosureClock.Note(
                register, DisclosureClock.Open(body, GalleriesOf(body)[0], 0.0)!.Value);
        }

        // He is on the first one: it stays, and the two he is not on go.
        IReadOnlyList<string> filled = Burial.Fill(register, [], grounds[0], window * 9.0);
        Assert.DoesNotContain(grounds[0], filled);
        Assert.Contains(grounds[1], filled);
        Assert.Contains(grounds[2], filled);

        // He leaves: it goes too, and the ones already filled are not filled twice.
        IReadOnlyList<string> later = Burial.Fill(register, filled, null, window * 10.0);
        Assert.Equal(3, later.Count);
        Assert.Equal(later.Distinct().Count(), later.Count);

        // …and nothing to add hands the register back BY REFERENCE, so a caller can ask for a save only when
        // something really happened.
        Assert.Same(later, Burial.Fill(register, later, null, window * 11.0));
    }

    // ══ LAW 4 · THE BOOK NEVER LIES ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · <b>THE BOOK NEVER LIES.</b> Nothing the burial does removes or rewrites one field-book entry,
    /// one clipped story, one red thread or one satchel row. Without one fixed point the player has no floor,
    /// and the horror requires the book to be sacred.
    ///
    /// <para>Asked of the two things that carry a find out of a gallery: the id it was minted with, and every
    /// reading taken off that id. Both are pure functions of the id, which is WHY the law holds — but a law
    /// that holds by construction is a law one refactor away from not holding, and nothing on screen would
    /// ever say so.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>IsHallRecord</c> re-derived through <c>IsFound</c> instead of
    /// off the id's own prefix — <i>"a record carried out of a gallery stopped being a record when the
    /// gallery was filled in"</i>.</para>
    /// </summary>
    [Fact]
    public void TheBookNeverLies()
    {
        foreach (string body in Grounds().Take(20))
        {
            int gallery = GalleriesOf(body)[0];
            string findId = UndergroundComplex.FindId(body, gallery, 0);
            Assert.True(UndergroundComplex.IsHallRecord(findId));

            CarriedObject.Reveal before = CarriedObject.RelicReveal(findId);

            using (Buried(body))
            {
                // The id still says what it said, and every reading off it is the same reading.
                Assert.True(UndergroundComplex.IsHallRecord(findId));
                Assert.Equal(before, CarriedObject.RelicReveal(findId));
            }
        }

        // And the event itself only ever ADDS. A register is never shortened, never reordered, and never
        // rewritten — the same monotone promise the clock makes about its own reading.
        string first = NeighbourGrounds(0)[0], second = NeighbourGrounds(0)[1];   // #1074
        IReadOnlyList<string> had = [first];
        IReadOnlyList<DisclosureClock.Opening> register =
            [new(first, 0), new(second, 0)];
        IReadOnlyList<string> next = Burial.Fill(register, had, null, DisclosureClock.WindowSeconds * 9.0);
        Assert.Equal([first, second], next);
    }

    // ══ LAW 5 · THE SCULLY LAW, AND §8's RESERVED WORD ══════════════════════════════════════════════════

    /// <summary>§8 — there is ONE of these and a hall never borrows the word. Everything else in the list is
    /// a word that would settle the question the feature exists to leave open. Held in one place because two
    /// guards sweep it now: the whole burial path, and the ledger paper's three entries in particular.
    /// </summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #1063/#672 · NO STRING ANYWHERE ON THE BURIAL PATH CONTAINS THE WORD §8 RESERVES, and none of them
    /// settles which reading of §10 is true. Every one is a piece of ordinary facilities paperwork.
    ///
    /// <para><b>Revert that reddened it:</b> the word planted in the rag's line —
    /// <i>"a burial string names the reserved object: RagLine"</i>.</para>
    /// </summary>
    [Fact]
    public void NoStringOnTheBurialPathNamesTheReservedThing()
    {
        var strings = new List<(string Where, string Text)>();
        foreach (string s in Burial.AllProse())
        {
            strings.Add(("Burial.AllProse", s));
        }
        strings.Add(("UndergroundComplex.MaintenanceLedgerLine", UndergroundComplex.MaintenanceLedgerLine));
        foreach (string s in CanteenBoard.AllProse())
        {
            strings.Add(("CanteenBoard.AllProse", s));
        }
        foreach (string s in CanteenRegulars.AllProse())
        {
            strings.Add(("CanteenRegulars.AllProse", s));
        }

        var named = new List<string>();
        foreach ((string where, string text) in strings)
        {
            foreach (string word in Forbidden)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    named.Add($"{where}: \"{word}\" in \"{text}\"");
                }
            }
        }
        Assert.True(named.Count == 0, "a burial string settles what it must leave open:\n  " + string.Join("\n  ", named));

        // …and the type that decides WHEN a ground is filled publishes no prose beyond those eight — the six
        // of slice 1 plus the canon pass's two bracketing ledger entries — which is the same shape #677 gave
        // the clock: a mechanism with nothing to say cannot say the wrong thing.
        Assert.Equal(8, Burial.AllProse().Count());
    }

    // ══ LAW 6 · THE THREE AUTHORED LINES, VERBATIM AND REACHABLE ════════════════════════════════════════

    /// <summary>
    /// #1063 · The evidence lifecycle, character for character. These are the issue's own sentences and an
    /// implementer may not reword one of them; the way this feature dies is one helpful sentence written to
    /// fill a gap.
    ///
    /// <para><b>Revert that reddened it:</b> a full stop moved in the ledger line — <i>"Assert.Equal()
    /// Failure … Expected: Sub-level access no longer required."</i>.</para>
    /// </summary>
    [Fact]
    public void TheAuthoredLinesAreVerbatim()
    {
        Assert.Equal(
            "Resurfacing of the lower galleries begins Monday. Please use the upper walks.",
            Burial.NoticeLine);
        Assert.Equal(
            "Sub-level access no longer required. Filled and remediated per instruction.",
            Burial.LedgerLine);
        Assert.Equal(
            "The concourse reopens a full meter higher, and the drainage is much improved. "
            + "The old kerbs make a handsome course of masonry in the new wall.",
            Burial.RagLine);
        Assert.Equal("pre-existing masonry, origin undetermined", Burial.MasonLine);

        // The ledger line carries NO instruction number, which is the whole of the evidence.
        Assert.DoesNotContain(Burial.LedgerLine, c => char.IsDigit(c));
    }

    /// <summary>
    /// #1063 · THE PAPER IS THREE ENTRIES AND THE CLUE IS THE ARITHMETIC BETWEEN THEM. The canon pass of
    /// 2026-09-02 brackets the anomalous entry with two cited ones, and what the pair buys is not atmosphere:
    /// it is that the omission becomes MEASURABLE. 2211, then a job citing nothing, then 2213 — the ledger's
    /// own numbering says an instruction 2212 was issued and that its line is the line with no number on it.
    /// Nobody ever writes that down, and this guard holds that nobody ever does: 2212 appears nowhere.
    ///
    /// <para>Every word of all three is authored, so all three are compared character for character and in
    /// the clerk's own order; a reworded entry is a piece of canon an implementer wrote.</para>
    ///
    /// <para><b>Reverts that reddened it (watched, 2026-09-02):</b> the first entry dropped from the
    /// composition — <i>"the ledger paper does not carry, in order, the entry "B3 sump: seal and impeller
    /// renewed…""</i>; and the three composed in reverse, which is the revert that matters most because a
    /// shuffled paper still CONTAINS all three — <i>"the ledger paper does not carry, in order, the entry
    /// "Sub-level access no longer required…""</i>.</para>
    /// </summary>
    [Fact]
    public void TheLedgerPaperCarriesThreeEntriesAndTheOmittedNumberIsTheOnlyTell()
    {
        string[] entries = [Burial.LedgerLineBefore, Burial.LedgerLine, Burial.LedgerLineAfter];

        // ── VERBATIM. The issue's own sentences, and an implementer may not reword one of them.
        Assert.Equal(
            "B3 sump: seal and impeller renewed, six hours, two hands. Per instruction 2211/M, Plant.",
            entries[0]);
        Assert.Equal(
            "Sub-level access no longer required. Filled and remediated per instruction.",
            entries[1]);
        Assert.Equal(
            "Upper walks: handrail re-fixed, bays four to nine, three hours, one hand. "
            + "Per instruction 2213/M, Plant.",
            entries[2]);

        // ── ONE PAPER, IN ORDER. Checked as rising positions rather than as three Contains, because three
        //    Contains pass on a paper that carries the entries shuffled — and shuffled, there is no clue.
        string paper = UndergroundComplex.MaintenanceLedgerLine;
        int at = -1;
        foreach (string entry in entries)
        {
            int next = paper.IndexOf(entry, StringComparison.Ordinal);
            Assert.True(next > at, $"the ledger paper does not carry, in order, the entry \"{entry}\"");
            at = next;
        }
        Assert.Equal("📋 " + string.Join(" ", entries), paper);

        // ── THE HOUSE STYLE, AND THE ONE BREAK IN IT. Every entry files under "per instruction"; exactly the
        //    middle one names no number after it.
        var cited = new List<int>();
        foreach (string entry in entries)
        {
            Assert.Contains("er instruction", entry, StringComparison.Ordinal);
            MatchCollection numbered = Regex.Matches(entry, @"[Pp]er instruction (\d+)");
            Assert.True(numbered.Count <= 1, $"an entry cites two instructions: \"{entry}\"");
            if (numbered.Count == 1)
            {
                cited.Add(int.Parse(
                    numbered[0].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        Assert.Equal(2, cited.Count);
        Assert.DoesNotMatch(@"[Pp]er instruction (\d+)", entries[1]);

        // ── THE ARITHMETIC. Consecutive but for one, and the one is never written anywhere on the paper.
        Assert.Equal([2211, 2213], cited);
        Assert.Equal(2, cited[1] - cited[0]);
        Assert.DoesNotContain("2212", paper, StringComparison.Ordinal);

        // ── AND IT IS STILL FACILITIES PAPERWORK. §8's reserved word, and every word that would settle §10,
        //    are absent from all three — the same sweep the whole burial path gets, aimed at the new prose.
        foreach (string entry in entries)
        {
            foreach (string word in Forbidden)
            {
                Assert.DoesNotContain(word, entry, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// #1063 · …AND EVERY ONE OF THEM IS REACHABLE. A verbatim string nothing in the world can show a player
    /// is a canon review of a constant.
    ///
    /// <para><b>Reverts that reddened it:</b> the works notice dealt out of the ordinary pool (so it is up on
    /// every board) — <i>"a ground nobody opened is advertising resurfacing"</i>; and the <c>HaulLine</c> arm
    /// removed — <i>"the ledger room reads as generic operational paper"</i>.</para>
    /// </summary>
    [Fact]
    public void TheAuthoredLinesAreReachableAndOnlyWhereTheyBelong()
    {
        string body = Grounds()[0];
        int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
        UndergroundComplex.Amenity canteen = TheCanteenOn(body, top);

        // ── BEFORE. The works are on and the job is not done: the notice is up, and the mason is in.
        using (Opened(body))
        {
            IReadOnlyList<CanteenBoard.Notice> up = CanteenBoard.Pinned(body, top, canteen);
            Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);
            Assert.Contains(up, n => n.Body == Burial.NoticeLine);
            Assert.Equal(up.Count, up.Select(n => n.Body).Distinct().Count());

            Assert.Contains(
                CanteenRegulars.Sitting(body, top, canteen), s => s.Line == Burial.MasonLine);
        }

        // ── DURING/AFTER. The job is done: the notice comes down, the mason is still on the works, and the
        //    ledger is in the room the ledger is kept in.
        using (Buried(body))
        {
            IReadOnlyList<CanteenBoard.Notice> up = CanteenBoard.Pinned(body, top, canteen);
            Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);
            Assert.DoesNotContain(up, n => n.Body == Burial.NoticeLine);

            Assert.Contains(
                CanteenRegulars.Sitting(body, top, canteen), s => s.Line == Burial.MasonLine);

            (int level, int room) = UndergroundComplex.MaintenanceLedgerRoomFor(body)!.Value;
            Assert.Equal(top, level);
            Assert.Equal(UndergroundComplex.Haul.Records, UndergroundComplex.InRoom(body, level, room));
            string said = UndergroundComplex.HaulLine(
                UndergroundComplex.Haul.Records, body, level, room, null);
            Assert.Contains(Burial.LedgerLine, said, StringComparison.Ordinal);
        }

        // ── AFTER, on the wire. The subject IS the headline for an arc break, so the rag prints the authored
        //    sentence and nothing else, filed under the site's own operator.
        var evt = new NewsWire.NewsEvent(
            NewsWire.NewsEventKind.ArcBeatBreaks, 1234.0, Burial.RagLine, Burial.RagOffice(body));
        Assert.Equal(Burial.RagLine, NewsWire.Headline(evt));
        Assert.NotEqual("", NewsWire.SubjectsFor(evt));
        Assert.Equal(SiteOperator.Of(body).Name, Burial.RagOffice(body));

        // ── AND NOWHERE ELSE. With nothing opened and nothing buried, none of the three exists anywhere.
        foreach (string plain in Plain(20).Concat(Grounds().Take(20)))
        {
            if (UndergroundComplex.TopPressurisedFloor(plain) is not { } floor)
            {
                continue;
            }
            UndergroundComplex.Amenity room = TheCanteenOn(plain, floor);
            Assert.DoesNotContain(CanteenBoard.Pinned(plain, floor, room), n => n.Body == Burial.NoticeLine);
            Assert.DoesNotContain(CanteenRegulars.Sitting(plain, floor, room), s => s.Line == Burial.MasonLine);
            Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(plain));
        }
    }

    /// <summary>
    /// #1063 · THE BOARD AND THE ROOM DID NOT MOVE. The works notice and the mason are dealt out of pools
    /// that stop one short of them, so a ground nobody has opened seats exactly the people it always seated
    /// and pins exactly the notices it always pinned — a room that changed while a captain was away would be
    /// the world saying something happened, and the whole beat is that nothing did.
    ///
    /// <para><b>Revert that reddened it:</b> the deals run against <c>Catalog.Length</c> / <c>Cast.Length</c>
    /// again — <i>"the board on a ground nobody opened moved three notices"</i> (measured against the pinned
    /// expectation below, which was read off the base build).</para>
    /// </summary>
    [Fact]
    public void TheOrdinaryBoardAndTheOrdinaryRotaAreUntouched()
    {
        // #1074 beat 4 · The two catalogues stopped being the same length the day a regular arrived who pins
        // no paper, so the pool LENGTHS are pinned instead — which is the number that actually matters here,
        // because it is what the dice are rolled against.
        Assert.Equal(10, CanteenRegulars.OrdinaryCastSize);
        Assert.Equal(10, CanteenBoard.OrdinaryNoticeSize);

        foreach (string body in Plain(25))
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } top)
            {
                continue;
            }
            UndergroundComplex.Amenity canteen = TheCanteenOn(body, top);

            IReadOnlyList<CanteenBoard.Notice> up = CanteenBoard.Pinned(body, top, canteen);
            Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);
            Assert.Equal(up.Count, up.Select(n => n.Head).Distinct().Count());
            Assert.DoesNotContain(up, n => n.Head == Burial.NoticeHead);

            foreach (CanteenRegulars.Seated s in CanteenRegulars.Sitting(body, top, canteen))
            {
                Assert.NotEqual(Burial.MasonPlate, s.Plate);
            }
        }
    }

    /// <summary>The upper canteen on this floor, built by the real generator — never a typed-in room. A guard
    /// handed a room nobody carved is a guard that cannot tell pass from fail (the fifth named bug class).</summary>
    private static UndergroundComplex.Amenity TheCanteenOn(string bodyId, int level)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, Field);
        UndergroundComplex.Amenity? canteen = null;
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            if (CanteenRegulars.PeopleSitHere(bodyId, level, a))
            {
                canteen = a;
            }
        }
        return Assert.IsType<UndergroundComplex.Amenity>(canteen);
    }

    // ══ LAW 7 · THE DESIGNATIONS DO NOT COLLIDE ═════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · FIVE DESIGNATIONS, FIVE FLOORS. The ledger's room may never be the Key's, the relic's, the
    /// halls' key's or the standing order's — a second designation on one room is a find silently replaced by
    /// another with every test still green.
    ///
    /// <para><b>Revert that reddened it:</b> <c>MaintenanceLedgerRoomFor</c> pointed at <c>DepthOf</c> —
    /// <i>"the ledger and the way down are the same room on burial-ground-116"</i>.</para>
    /// </summary>
    [Fact]
    public void TheLedgerNeverSharesARoomWithAnotherDesignation()
    {
        foreach (string body in Grounds().Take(60))
        {
            using (Buried(body))
            {
                (int Level, int RoomIndex)? ledger = UndergroundComplex.MaintenanceLedgerRoomFor(body);
                Assert.NotNull(ledger);

                (int Level, int RoomIndex)?[] others =
                [
                    UndergroundComplex.KeyRoomFor(body),
                    UndergroundComplex.RelicRoomFor(body),
                    UndergroundComplex.StandingOrderRoomFor(body),
                    UndergroundComplex.FoundKeyRoomFor(body),
                ];
                foreach ((int Level, int RoomIndex)? other in others)
                {
                    if (other is { } o)
                    {
                        Assert.NotEqual(o, ledger!.Value);
                    }
                }
            }
        }
    }
}
