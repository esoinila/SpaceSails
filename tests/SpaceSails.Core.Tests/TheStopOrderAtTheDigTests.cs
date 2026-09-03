using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 · THE STOP ORDER AT THE DIG — the enforcement tier of the #672 doctrine, and #1063's sibling on one
/// trigger.
///
/// <para>Owner (2026-09-02): <i>"it seems almost like that governments are told by some entity to stop letting
/// people explore what is down there… all of a sudden Egypt says national security and pull the plug from the
/// research before they can drill through the roof… Then the government people in power do the
/// enforcing."</i></para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names; the revert is quoted
/// on each one, in the shape this ground has used since #587's lesson — a guard that has never failed is a
/// guard nobody has checked.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, and it is deliberately a family of ids
/// no other suite asks about (<see cref="Grounds"/>). Every sweep in this repo walks <c>probe-moon-{i}</c> and
/// the <c>?found=1</c> cheat rock; the stop register only ever changes the answer for the ids IN it, so a
/// guard here that installs a ground of its own cannot move any other guard's world, whatever order xUnit runs
/// them in. Restoring in a <c>finally</c> is belt as well as braces.</para>
/// </summary>
public sealed class TheStopOrderAtTheDigTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls. The band is about one
    /// site in fifty; a ten-site sample tells you nothing about it. Same number and same reasoning as
    /// <c>TheBurialTests</c>' own sweep.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>EVERY GROUND IN THE SWEEP THAT ACTUALLY HAS HALLS, derived off an id family this file owns
    /// alone — asserted to be a real population rather than merely non-empty, because a population of one
    /// proves nothing and an empty one passes every negative law in this file for the wrong reason (the fifth
    /// named bug class).</summary>
    private static List<string> Grounds() => _grounds ??= Sweep();

    // Swept ONCE per class. The sweep is a pure function of a family of ids nothing else in the repo asks
    // about — no register can change what HasFoundBand says about them, because none of them is ever filled
    // in by anybody — so a second sweep would return the same list and cost the fast run another twelve
    // passes over four thousand seeded rocks.
    private static List<string>? _grounds;

    private static List<string> Sweep()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"stop-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated stop grounds had halls — this proves little.");
        return found;
    }

    /// <summary>Close the working on these grounds for the length of one guard, and put the world back
    /// afterwards whatever happens.</summary>
    private static IDisposable Stopped(params string[] bodies)
    {
        StopOrder.Install([.. bodies]);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() => StopOrder.Install([]);
    }

    // ══ LAW 1 · ONE TRIGGER, TWO OUTCOMES, AND NEVER BOTH ════════════════════════════════════════════════

    /// <summary>
    /// #1074/#1063 · A GROUND IS STOPPED OR BURIED AND NEVER BOTH, AND EVERY DUE GROUND IS ONE OF THE TWO.
    ///
    /// <para>The two events are handed the SAME register at the SAME moment — which is exactly how the client
    /// calls them, one after the other out of one descent — and what comes back must partition it. The guard
    /// asserts three things and the third is the one that makes the first two mean anything: no ground is in
    /// both lists, every due ground is in one of them, and <b>both lists are a real population</b>, because a
    /// split that handed everything to one side would satisfy "never both" perfectly and ship a feature
    /// nobody could reach.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>StopOrder.TheOfficeGetsThisOne</c> clause taken out of
    /// <c>Burial.Fill</c> — <i>"Assert.Equal() Failure: Values differ. Expected: 69. Actual: 104"</i>, which
    /// is the partition counted: sixty-nine grounds went to the neighbours AND to the office both.</para>
    /// </summary>
    [Fact]
    public void EveryDueGroundGoesToTheNeighboursOrToTheOfficeAndNeverToBoth()
    {
        List<string> grounds = Grounds();
        var both = new List<string>();
        int toTheOffice = 0, toTheNeighbours = 0, due = 0;

        // Several opening windows, because the split is seeded on the window the ground was opened in: a
        // sweep that only ever asked about window zero would be measuring one coin toss per ground and would
        // say nothing about the seeding varying with the window at all.
        foreach (long opened in new long[] { 0, 1, 2, 3, 7, 11 })
        {
            IReadOnlyList<DisclosureClock.Opening> register =
                [.. grounds.Select(g => new DisclosureClock.Opening(g, opened))];

            // A whole window later, with the captain nowhere near any of them.
            double when = DisclosureClock.WindowSeconds * (opened + 2);
            IReadOnlyList<string> filled = Burial.Fill(register, [], null, when);
            IReadOnlyList<string> stopped = StopOrder.Note(register, [], null, when);

            toTheNeighbours += filled.Count;
            toTheOffice += stopped.Count;
            due += grounds.Count;

            both.AddRange(filled.Intersect(stopped, StringComparer.Ordinal));

            // …and nothing was simply dropped on the floor between them.
            Assert.Equal(grounds.Count, filled.Count + stopped.Count);
        }

        Assert.True(both.Count == 0,
            $"{both.Count} ground(s) were filled in AND closed by order:\n  "
            + string.Join("\n  ", both.Take(5)));

        // A fair coin over a few hundred draws: anything outside a third and two thirds is not a split, it
        // is a switch. Measured rather than asserted at a point, for the reason the found band's own
        // incidence is measured — a rate written down as an equality is a rate nobody may ever re-seed.
        Assert.InRange(toTheOffice, due / 3, due * 2 / 3);
        Assert.InRange(toTheNeighbours, due / 3, due * 2 / 3);
    }

    /// <summary>
    /// #1074 · THE OFFICE WAITS A WHOLE SHIFT, AND NEVER ACTS WITH THE CAPTAIN STANDING ON THE GROUND.
    ///
    /// <para>The two conditions the burial keeps, kept here for the same two reasons: an order posted inside
    /// the window the captain crossed the seam in would be an answer to what he had just done, arriving
    /// inside the hour, from something that was watching him do it (#672's instrument law); and an order
    /// posted while he is standing on the floor would be a thing that happened TO him and therefore a thing
    /// he could describe.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>WindowsBeforeStopping</c> set to 0, and (separately) the
    /// <c>standingOn</c> clause deleted — each gave the same shape of failure from its own line,
    /// <i>"Assert.Empty() Failure: Collection was not empty. Collection: [stop-ground-69]"</i>.</para>
    /// </summary>
    [Fact]
    public void TheOrderComesAfterOneWholeWindowAndNeverWhileHeIsThere()
    {
        string body = TheOfficesGround();
        IReadOnlyList<DisclosureClock.Opening> register = [new(body, 0)];

        // Inside the window it was opened in: nothing.
        Assert.Empty(StopOrder.Note(register, [], null, DisclosureClock.WindowSeconds * 0.99));

        // A whole window later, and he is elsewhere: closed.
        Assert.Equal([body],
            StopOrder.Note(register, [], null, DisclosureClock.WindowSeconds * 1.0));

        // …and the same moment with him standing on it: nothing, and it is the SAME call otherwise, which is
        // what makes this a guard about the clause rather than about the clock.
        Assert.Empty(StopOrder.Note(register, [], body, DisclosureClock.WindowSeconds * 1.0));

        // A working closes once. The register comes back BY REFERENCE when there is nothing to add, so a
        // caller can compare and only then ask for a save.
        IReadOnlyList<string> had = [body];
        Assert.Same(had, StopOrder.Note(register, had, null, DisclosureClock.WindowSeconds * 9.0));
    }

    // ══ LAW 2 · NOTHING IS FILLED — THE HALLS ARE STILL THERE ════════════════════════════════════════════

    /// <summary>
    /// #1074 · A STOPPED GROUND STILL HAS ITS HALLS, AND EVERY BAND PREDICATE SAYS SO. <b>This is not the
    /// burial.</b> Nothing is removed and no mark is erased: the galleries are where the captain left them,
    /// the site's true depth is unchanged, the clock still ticks (a stopped ground is still an opened one),
    /// and <see cref="Burial.IsFilled"/> answers no.
    ///
    /// <para>Every predicate is asked twice — once with nothing stopped and once stopped — and the two
    /// answers must be identical, which is the whole law said in one comparison.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>HasFoundBand</c> taught to answer no on a stopped ground the
    /// way it does on a buried one — <i>"a stop order moved the ground: stop-ground-69: HasFoundBand went
    /// false under the order / stop-ground-69: the true depth moved under the order"</i>.</para>
    /// </summary>
    [Fact]
    public void AStoppedGroundKeepsEveryHallItHad()
    {
        var wrong = new List<string>();
        foreach (string body in Grounds().Take(60))
        {
            bool band = UndergroundComplex.HasFoundBand(body);
            int trueDepth = UndergroundComplex.TrueDepthOf(body);
            List<int> floors = [.. UndergroundComplex.FloorsOf(body)];
            List<int> galleries = [.. floors.Where(l => UndergroundComplex.IsFound(body, l))];
            Assert.NotEmpty(galleries);   // the world can tell pass from fail

            using (Stopped(body))
            {
                if (!UndergroundComplex.HasFoundBand(body))
                {
                    wrong.Add($"  {body}: HasFoundBand went false under the order");
                }
                if (UndergroundComplex.TrueDepthOf(body) != trueDepth)
                {
                    wrong.Add($"  {body}: the true depth moved under the order");
                }
                if (!UndergroundComplex.FloorsOf(body).SequenceEqual(floors))
                {
                    wrong.Add($"  {body}: the floor list changed under the order");
                }
                if (Burial.IsFilled(body))
                {
                    wrong.Add($"  {body}: a stopped ground reads as FILLED IN");
                }
                foreach (int gallery in galleries)
                {
                    if (!UndergroundComplex.IsFound(body, gallery)
                        || !DisclosureClock.OpensOn(body, gallery)
                        || !UndergroundComplex.DeclaresDarkness(body, gallery)
                        || !UndergroundComplex.HoldsPressure(body, gallery))
                    {
                        wrong.Add($"  {body} B{-gallery} stopped answering as a gallery under the order");
                    }
                }
            }

            Assert.Equal(band, UndergroundComplex.HasFoundBand(body));   // and the world is put back
        }

        Assert.True(wrong.Count == 0, "a stop order moved the ground:\n" + string.Join("\n", wrong));
    }

    // ══ LAW 3 · THE WAY DOWN IS SEALED ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE HALLS ARE STILL THERE AND THERE IS NO LONGER A WAY TO THEM — driven from the panel graph
    /// in both directions, on the same ground, with the same wallet.
    ///
    /// <para>The captain is given <b>every authority this site ever issued</b>, which is the strongest form
    /// of the question: the order is not a clearance and no paper answers it. Unstopped, the gate into the
    /// band nobody listed is on the panel and opens; stopped, the row is not there at all — and it is not
    /// there in SILENCE rather than as a refusal, because the building does not admit that band exists and a
    /// row that named it to say no would give the shaft away in the sentence it refused it in (#592).</para>
    ///
    /// <para>The gate BELOW it is deliberately untouched and is asserted so: the order closes one shaft, and
    /// what makes the halls unreachable is that everything under the listed bottom hangs off that one.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>StopSealsTheGateTo</c> clause taken out of
    /// <c>LiftPanel</c> — <i>"Assert.DoesNotContain() Failure: Filter matched in collection"</i>, the row
    /// into the band nobody listed still on the panel with the order posted at the seal.</para>
    /// </summary>
    [Fact]
    public void TheGateIntoTheDeepIsNotOfferedOnAStoppedGroundAndIsOnEveryOther()
    {
        foreach (string body in Grounds().Take(25))
        {
            int listedBottom = UndergroundComplex.DepthOf(body);
            int unlistedHead = UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body));
            int hallsHead = UndergroundComplex.BandTop(UndergroundComplex.FoundBandOf(body));
            string[] wallet =
            [
                new UndergroundComplex.AuthorityCard(body, UndergroundComplex.UnlistedBandOf(body)).Id,
                new UndergroundComplex.AuthorityCard(body, UndergroundComplex.FoundBandOf(body)).Id,
            ];

            // ── Before the order: the row is there, it opens, and the ride is a gate crossing.
            UndergroundComplex.LiftStop open = Assert.Single(
                UndergroundComplex.LiftPanel(body, listedBottom, wallet), s => s.Level == unlistedHead);
            Assert.Null(open.Refusal);

            using (Stopped(body))
            {
                // ── After it: no row, no refusal, nothing said. The panel simply ends at the listed bottom.
                IReadOnlyList<UndergroundComplex.LiftStop> sealedPanel =
                    UndergroundComplex.LiftPanel(body, listedBottom, wallet);
                Assert.DoesNotContain(sealedPanel, s => s.Level == unlistedHead);
                Assert.DoesNotContain(sealedPanel, s => s.Level < listedBottom);
                Assert.All(sealedPanel, s => Assert.Null(s.Refusal));

                // …on every floor of that band, because the gate is the band's and not the floor's.
                for (int level = UndergroundComplex.BandTop(UndergroundComplex.BandOf(listedBottom));
                     level >= listedBottom; level--)
                {
                    Assert.DoesNotContain(
                        UndergroundComplex.LiftPanel(body, level, wallet), s => s.Level == unlistedHead);
                }

                // ONE shaft is closed and not two: the gate from the band nobody listed down into the halls
                // is exactly where it was. It is simply behind the one that is shut.
                Assert.Contains(
                    UndergroundComplex.LiftPanel(body, UndergroundComplex.UnlistedBottomOf(body), wallet),
                    s => s.Level == hallsHead);
            }

            // …and the world is put back: the same wallet, the same floor, the row again.
            Assert.Contains(
                UndergroundComplex.LiftPanel(body, listedBottom, wallet), s => s.Level == unlistedHead);
        }
    }

    /// <summary>
    /// #1074 · THE SEAL SEALS THE SHAFT AND NOTHING ELSE — the A* audit, driven both directions on the floor
    /// the order is posted on.
    ///
    /// <para>A captain is never shut IN by this. Every room on the sealed floor can still be walked to from
    /// the lift and walked back from, and the walk is the same lattice and the same body radius the live
    /// avatar moves by (<see cref="DeckReachability"/>), so the audit and the game agree by construction.
    /// </para>
    ///
    /// <para><b>The wall list includes the LOCKED DOORS</b>, which is what the deck the captain actually
    /// walks is made of (<c>HiveInterior</c> lays a real wall behind every leaf that will not open). An audit
    /// over <c>floor.Walls</c> alone would walk straight through the seal and would be auditing a building
    /// the game does not ship — the drawing-lies-about-the-sim failure, wearing a lab coat.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the mouth left out of <c>alcoveMouths</c> in
    /// <c>CarveStopSeal</c>, which walls the recess shut and takes the plate out of the captain's reach —
    /// <i>"the seal shut somebody in: stop-ground-69 B6: the ground in front of the plate cannot be walked
    /// to"</i>.</para>
    /// </summary>
    [Fact]
    public void NoRoomOnTheSealedFloorIsShutInByTheSeal()
    {
        const double radius = 0.7;   // DeckPlan.AvatarRadius — the captain's own body
        var wrong = new List<string>();
        int walked = 0;

        // TWO grounds, and three targets on each: the ground in front of the seal itself, and two room
        // centres. One A* run floods the whole field the captain can walk, which is the honest bound (a box
        // drawn round the pair would be the test choosing which routes count) and it is the expensive half of
        // this class — so the sample is a sample and it is small on purpose. What it holds is the shape of
        // the law rather than a census: the seal is ONE pocket, and either it shuts somebody in or it does
        // not.
        foreach (string body in Grounds().Take(2))
        {
            using (Stopped(body))
            {
                int level = UndergroundComplex.StopSealFloorOf(body)!.Value;
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

                // The seal is really on this floor, or the walk below proves nothing.
                UndergroundComplex.LockedDoor seal =
                    Assert.Single(floor.Locked, l => StopOrder.IsPlate(l.Sign));

                List<SurfaceCollision.Segment> walls =
                [
                    .. floor.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)),
                    .. floor.Locked.Select(l => new SurfaceCollision.Segment(l.X1, l.Y1, l.X2, l.Y2)),
                ];

                (double cageX, double cageY) = UndergroundComplex.ShaftAt(Field);
                var lift = new DeckReachability.Point(cageX, cageY);
                (double MinX, double MinY, double MaxX, double MaxY) bounds =
                    (Field.LeftX - 4, Field.BottomY - 4, Field.RightX + 4, Field.TopY + 4);

                // THE PLATE IS SOMETHING A CAPTAIN CAN STAND IN FRONT OF. The console spot is the leaf's own
                // midpoint, so the ground that has to be walkable is a pace back from it, inside the recess —
                // and if the mouth were never cut, this is the target that says so.
                var targets = new List<(string What, DeckReachability.Point Where)>
                {
                    ("the ground in front of the plate",
                     new DeckReachability.Point((seal.X1 + seal.X2) / 2.0, ((seal.Y1 + seal.Y2) / 2.0) - 1.5)),
                };
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    if (targets.Count >= 3 || !DeckReachability.Standable(rx, ry, radius, walls))
                    {
                        continue;   // a fixture in the middle of a canteen is somebody else's guard
                    }
                    targets.Add(($"the room at ({rx:F1},{ry:F1})", new DeckReachability.Point(rx, ry)));
                }

                Assert.True(DeckReachability.Standable(lift.X, lift.Y, radius, walls));
                foreach ((string what, DeckReachability.Point where) in targets)
                {
                    Assert.True(DeckReachability.Standable(where.X, where.Y, radius, walls),
                        $"{body} B{-level}: {what} is not ground a captain could stand on");
                    walked++;

                    // BOTH DIRECTIONS. A* on a symmetric lattice is symmetric, and asking it twice is cheap;
                    // what it buys is that "the lift can be reached from everywhere" and "everywhere can be
                    // reached from the lift" are two sentences and this guard has said both.
                    if (!DeckReachability.Path(lift, where, walls, radius, bounds).Reached)
                    {
                        wrong.Add($"  {body} B{-level}: {what} cannot be walked to");
                    }
                    if (!DeckReachability.Path(where, lift, walls, radius, bounds).Reached)
                    {
                        wrong.Add($"  {body} B{-level}: the lift cannot be walked to from {what}");
                    }
                }
            }
        }

        Assert.True(walked >= 6, $"only {walked} spot(s) were walked to — this proves little.");
        Assert.True(wrong.Count == 0, "the seal shut somebody in:\n" + string.Join("\n", wrong));
    }

    // ══ LAW 4 · WHAT IS POSTED, AND WHERE ════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE ORDER IS POSTED AT THE SEAL, VERBATIM, ON EXACTLY ONE FLOOR OF A STOPPED GROUND AND ON NO
    /// FLOOR OF ANY OTHER.
    ///
    /// <para>One plate, on the listed bottom, and it says what an office says: a stamp and no signature. The
    /// sentence a captain reads when he presses it is the order itself and nothing has been composed around
    /// it.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>StopSealFloorOf</c> made unconditional —
    /// <i>"Assert.DoesNotContain() Failure: Filter matched in collection … Sign = AUTHORITY — WORKING
    /// CLOSED"</i>, on a ground nobody had stopped; and the <c>StopOrder.IsPlate</c> arm inverted in
    /// <c>LockedLine</c> — <i>"Assert.Contains() Failure: Sub-string not found. String: AUTHORITY —
    /// WORKING CLOSED. The lock i… Not found: By order of the Authority this working is…"</i>.</para>
    /// </summary>
    [Fact]
    public void TheOrderIsPostedAtTheSealAndNowhereElse()
    {
        foreach (string body in Grounds().Take(3))
        {
            // Before the order: no plate anywhere in the building.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.DoesNotContain(
                    UndergroundComplex.Build(body, level, Field).Locked, l => StopOrder.IsPlate(l.Sign));
            }

            using (Stopped(body))
            {
                int posted = UndergroundComplex.StopSealFloorOf(body)!.Value;
                Assert.Equal(UndergroundComplex.DepthOf(body), posted);

                int plates = 0;
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                    int here = floor.Locked.Count(l => StopOrder.IsPlate(l.Sign));
                    plates += here;
                    Assert.Equal(level == posted ? 1 : 0, here);
                }
                Assert.Equal(1, plates);

                // …and pressing it reads the order out, word for word, with nothing added.
                Assert.Contains(StopOrder.OrderLine, UndergroundComplex.LockedLine(StopOrder.Plate));
            }
        }
    }

    /// <summary>
    /// #1074 · THE ENFORCER IS AN OFFICE AND NEVER A NAME — the doctrine's first law, held over the plate and
    /// the order.
    ///
    /// <para>The plate carries the stamp. Neither string names a department of this building, an office of
    /// the head office, or a person: the only capitalised word in the order that is not the first word of a
    /// sentence is <b>Authority</b>, and there is no signature line of any kind. A stop order that named
    /// somebody would mint a villain official, and the horror is that any office will do.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <i>"Countersigned by Director Vantar."</i> appended to the
    /// order — <i>"Assert.DoesNotContain() Failure: Sub-string found … s published. Countersigned by
    /// Director Va…"</i>; and the plate re-authored as <c>PLANT — WORKING CLOSED</c> —
    /// <i>"Assert.Contains() Failure: Sub-string not found. String: PLANT — WORKING CLOSED. Not found:
    /// AUTHORITY"</i>.</para>
    /// </summary>
    [Fact]
    public void ThePlateCarriesTheStampAndNeitherStringCarriesAName()
    {
        Assert.Contains(StopOrder.Stamp, StopOrder.Plate, StringComparison.Ordinal);

        // No department, from either stock — this building's eight or the head office's twenty-four.
        foreach (string department in
            UndergroundComplex.Departments.Concat(UndergroundComplex.HeadOfficeDepartments))
        {
            Assert.DoesNotContain(department, StopOrder.Plate, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(department, StopOrder.OrderLine, StringComparison.OrdinalIgnoreCase);
        }

        // …and nobody signed it. Not an illegible signature, not an initial: the field does not exist.
        foreach (string signing in new[] { "signed", "signature", "sgd", "on behalf of", "for and on behalf" })
        {
            Assert.DoesNotContain(signing, StopOrder.Plate, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(signing, StopOrder.OrderLine, StringComparison.OrdinalIgnoreCase);
        }

        // THE PROPER-NOUN SWEEP. Every capitalised word in the order is either the first word of a sentence
        // or the office itself. A name planted anywhere in it is a word this catches mechanically, which is
        // the only form of "no name" a test can actually hold.
        var named = new List<string>();
        bool sentenceStart = true;
        foreach (string token in StopOrder.OrderLine.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string word = token.Trim('.', ',', ';', ':', '—', '(', ')', '"');
            if (word.Length > 0 && char.IsUpper(word[0]) && !sentenceStart
                && !string.Equals(word, "Authority", StringComparison.Ordinal))
            {
                named.Add(word);
            }
            sentenceStart = token.EndsWith('.') || token.EndsWith(':');
        }
        Assert.True(named.Count == 0, "the order names somebody: " + string.Join(", ", named));
    }

    /// <summary>
    /// #1074/#602 · THERE IS NO KEYPAD ON THE SEAL, AND NOTHING TO BREAK. The captain cannot open it: not
    /// with the card that opened every other door on the site, not with a sentry.
    ///
    /// <para>Both answers come out of ONE Core predicate (<c>UndergroundComplex.HasNoReader</c>), which is
    /// what stops the gun and the satchel coming to two opinions about what a captain is standing in front
    /// of. The plate is also deliberately not in any door vocabulary, so nothing can mistake it for a room
    /// somebody shut.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>Judge</c> asking <c>IsSealedWay</c> again instead of
    /// <c>HasNoReader</c> — <i>"Assert.Equal() Failure: Values differ. Expected: NoLockToBreak. Actual:
    /// ReadsRatherThanLocks"</i>, which would have had a captain putting rounds through a panel that is not
    /// there.</para>
    /// </summary>
    [Fact]
    public void TheSealHasNoReaderAndNoHasp()
    {
        Assert.True(UndergroundComplex.HasNoReader(StopOrder.Plate));
        Assert.False(UndergroundComplex.IsDoorSign(StopOrder.Plate));
        Assert.False(UndergroundComplex.IsSealedWay(StopOrder.Plate));
        Assert.False(UndergroundComplex.IsFreightShutter(StopOrder.Plate));

        Assert.Equal(ShootTheLock.Verdict.NoLockToBreak, ShootTheLock.Judge(StopOrder.Plate));
        Assert.False(ShootTheLock.IsShootable(StopOrder.Plate));

        // …and an ordinary room door is unmoved by all of this, or the guard above selects everything.
        Assert.False(UndergroundComplex.HasNoReader("MORTUARY"));
        Assert.Equal(ShootTheLock.Verdict.MechanicalLock, ShootTheLock.Judge("MORTUARY"));
    }

    // ══ LAW 5 · THE VALVE-BOOK ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074/#1063 · THE VALVE-BOOK CITES 2231 AND 2233 AND THE LINE BETWEEN THEM CITES NOTHING — the missing
    /// middle, delivered by a clerk's habit rather than by a sentence.
    ///
    /// <para>The three entries are on one paper in the order the clerk wrote them; exactly one of them cites
    /// no instruction; the two that do cite consecutive-but-one numbers, rising; and <b>2232 appears
    /// nowhere</b>, because the whole clue is that the book's own arithmetic names an instruction nobody
    /// wrote. The second tell is the preposition — the unnumbered line says <i>per order</i> where both
    /// others say <i>per instruction</i> — and no sentence anywhere points either of them out.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <i>"per instruction 2232/M, Plant"</i> written into the middle
    /// entry — <i>"Assert.Equal() Failure: Values differ. Expected: 0. Actual: 1"</i>; and the two bracketing
    /// entries swapped on the paper — <i>"the valve-book paper does not carry, in order: Working closed.
    /// Services isolated per order. Review pending."</i>.</para>
    /// </summary>
    [Fact]
    public void TheValveBookNamesAnInstructionNobodyWrote()
    {
        string[] entries = [StopOrder.ValveBookBefore, StopOrder.ValveBookLine, StopOrder.ValveBookAfter];

        // One paper, three entries, in the clerk's own order — and the paper is composed of them and of
        // nothing invented here.
        int at = 0;
        foreach (string entry in entries)
        {
            int found = UndergroundComplex.PlantValveBookLine.IndexOf(entry, at, StringComparison.Ordinal);
            Assert.True(found >= 0, $"the valve-book paper does not carry, in order: \"{entry}\"");
            at = found + entry.Length;
        }

        var cited = new List<int>();
        for (int i = 0; i < entries.Length; i++)
        {
            MatchCollection numbered = Regex.Matches(entries[i], @"[Pp]er instruction (\d+)/M");
            Assert.Equal(i == 1 ? 0 : 1, numbered.Count);
            if (numbered.Count == 1)
            {
                cited.Add(int.Parse(numbered[0].Groups[1].Value, CultureInfo.InvariantCulture));
            }
        }

        Assert.Equal(2, cited.Count);
        Assert.True(cited[1] > cited[0], $"the cited instructions do not rise: {cited[0]} then {cited[1]}");
        Assert.Equal(2, cited[1] - cited[0]);   // exactly one number is missing between them

        // The one line that cites nothing cites an ORDER instead, and it is the only line that does.
        Assert.DoesNotMatch(@"[Pp]er instruction (\d+)", entries[1]);
        Assert.Contains("per order", entries[1], StringComparison.Ordinal);
        Assert.DoesNotContain("per order", entries[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("per order", entries[2], StringComparison.OrdinalIgnoreCase);

        // And the number the arithmetic implies is written down NOWHERE. It is the reader's to work out.
        string missing = (cited[0] + 1).ToString(CultureInfo.InvariantCulture);
        Assert.DoesNotContain(missing, UndergroundComplex.PlantValveBookLine, StringComparison.Ordinal);
        foreach (string s in StopOrder.AllProse())
        {
            Assert.DoesNotContain(missing, s, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// #1074 · …AND THE BOOK IS ON THE FLOOR THE ORDER IS POSTED ON, IN A ROOM THAT IS NOT THE KEY ROOM.
    ///
    /// <para>Designated rather than rolled, for the reason all five of its siblings are: a seeded one-in-nine
    /// paper is a paper that is silently absent forever on some worlds, with nothing on screen ever saying
    /// so. It is on the listed bottom because that is what makes it evidence rather than a document — the
    /// book that goes terse for one line is kept on the floor whose working was closed.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>ValveBookRoomFor</c> returning room 0 —
    /// <i>"Assert.NotEqual() Failure: Values are equal. Expected: Not Tuple (-6, 0). Actual: Tuple (-6,
    /// 0)"</i>, the book standing on the Key room; and the <c>HaulLine</c> arm removed —
    /// <i>"Assert.Equal() Failure: Strings differ"</i>, the room reading as generic operational paper.</para>
    /// </summary>
    [Fact]
    public void TheValveBookIsKeptOnTheFloorTheOrderClosed()
    {
        foreach (string body in Grounds().Take(6))
        {
            Assert.Null(UndergroundComplex.ValveBookRoomFor(body));

            using (Stopped(body))
            {
                (int level, int room) = UndergroundComplex.ValveBookRoomFor(body)!.Value;
                Assert.Equal(UndergroundComplex.StopSealFloorOf(body), level);
                Assert.NotEqual(UndergroundComplex.KeyRoomFor(body), (level, room));
                Assert.NotEqual(UndergroundComplex.RelicRoomFor(body), (level, room));
                Assert.NotEqual(UndergroundComplex.FoundKeyRoomFor(body), (level, room));
                Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(body));   // never both (#1063)

                // The room exists on the field the rest of the suite uses, and it holds the paper.
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                Assert.InRange(room, 0, floor.RoomCentres.Count - 1);
                Assert.Equal(UndergroundComplex.Haul.Records, UndergroundComplex.InRoom(body, level, room));
                Assert.Equal(
                    UndergroundComplex.PlantValveBookLine,
                    UndergroundComplex.HaulLine(UndergroundComplex.Haul.Records, body, level, room, null));
            }
        }
    }

    // ══ LAW 6 · THE ROSTER THAT NEVER DUG ════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE SHIFT IS STILL ON THE BOARD, AND THE WORKS NOTICE IS NOT.
    ///
    /// <para>The canon pass authored no line for this beat and none is written: on a stopped ground the rota
    /// the board has carried since #709 is certainly up, listing a shift for a working nobody can get to any
    /// more — and the resurfacing notice, which is a notice about a job somebody was going to do, comes down,
    /// because on a stopped ground nobody is going to do it. The gap is the sentence.</para>
    ///
    /// <para>It takes one of the four slots and never a fifth, and it is pinned once: a board that grew a row
    /// would say in its own shape that something new had happened here.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the roster pin removed from <c>Pinned</c> —
    /// <i>"Assert.Single() Failure: The collection did not contain any matching items"</i>; and the
    /// <c>StopOrder.On</c> clause taken out of <c>Burial.NoticeIsUp</c> — <i>"Assert.False() Failure.
    /// Expected: False. Actual: True"</i>, a sealed site advertising resurfacing of the galleries.</para>
    /// </summary>
    [Fact]
    public void TheRotaIsUpOnAStoppedGroundAndTheWorksNoticeIsNot()
    {
        foreach (string body in Grounds().Take(4))
        {
            int top = UndergroundComplex.TopPressurisedFloor(body)!.Value;
            UndergroundComplex.Amenity? canteen = TheCanteenOn(body, top);
            if (canteen is not { } bar)
            {
                continue;
            }

            IReadOnlyList<CanteenBoard.Notice> before = CanteenBoard.Pinned(body, top, bar);
            Assert.Equal(CanteenBoard.PinnedAtOnce, before.Count);

            using (Stopped(body))
            {
                // …and the works notice is DOWN, which is checked through the same one predicate the board
                // asks, so this cannot pass because a board happened not to deal it.
                Burial.Install([], [new DisclosureClock.Opening(body, 0)]);
                try
                {
                    Assert.True(Burial.WorksAreOn(body));      // the order exists; the captain went down
                    Assert.False(Burial.NoticeIsUp(body));     // …and nobody is going to do the job

                    IReadOnlyList<CanteenBoard.Notice> up = CanteenBoard.Pinned(body, top, bar);
                    Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);       // four slots, never a fifth
                    Assert.Single(up, n => n.Head == CanteenBoard.RosterHead);
                    Assert.DoesNotContain(up, n => n.Head == Burial.NoticeHead);
                    Assert.Equal(up.Count, up.Select(n => n.Head).Distinct(StringComparer.Ordinal).Count());
                }
                finally
                {
                    Burial.Install([], []);
                }
            }
        }
    }

    // ══ LAW 7 · THE SCULLY LAW, AND §8's RESERVED WORD ═══════════════════════════════════════════════════

    /// <summary>§8 — there is ONE of these and a stop order never borrows the word. Everything else in the
    /// list is a word that would settle the question the feature exists to leave open. The same list
    /// <c>TheBurialTests</c> keeps, because the two beats are one trigger's two outcomes and a word that is
    /// forbidden on one paper is forbidden on the other.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #1074/#672 · NO STRING ON THE STOP ORDER'S PATH CONTAINS THE WORD §8 RESERVES, and none of them
    /// settles which reading of §10 is true. Every one is a piece of ordinary facilities paperwork: an order
    /// about a structural review, and a plant book about riser valves.
    ///
    /// <para>The type that decides WHEN a working is closed also publishes no prose beyond these — swept by
    /// reflection, exactly as the clock's own guard sweeps the clock, so a helpful sentence added later
    /// cannot escape the canon grep by not being in <c>AllProse</c>.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the word planted in the order — <i>"a stop-order string
    /// settles what it must leave open: StopOrder.AllProse: ancient in By order of the Authority this
    /// ancient working is closed pending structural review…"</i>.</para>
    /// </summary>
    [Fact]
    public void NoStringOnTheStopOrdersPathNamesTheReservedThing()
    {
        var strings = new List<(string Where, string Text)>();
        foreach (string s in StopOrder.AllProse())
        {
            strings.Add(("StopOrder.AllProse", s));
        }
        strings.Add(("UndergroundComplex.PlantValveBookLine", UndergroundComplex.PlantValveBookLine));
        strings.Add(("UndergroundComplex.LockedLine", UndergroundComplex.LockedLine(StopOrder.Plate)));

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
        Assert.True(named.Count == 0,
            "a stop-order string settles what it must leave open:\n  " + string.Join("\n  ", named));

        // …and there is no sixth string hiding on the type. Every public const string it publishes is one of
        // the five AllProse lists, or the stamp, which is a substring of the plate.
        var published = new List<string>();
        foreach (System.Reflection.FieldInfo f in
            typeof(StopOrder).GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string value)
            {
                published.Add(value);
            }
        }
        Assert.All(published, s =>
            Assert.True(StopOrder.AllProse().Contains(s, StringComparer.Ordinal)
                || StopOrder.Plate.Contains(s, StringComparison.Ordinal),
                $"StopOrder publishes a string no canon grep can see: \"{s}\""));
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A ground the split hands to the office, opened in window zero — derived off the sweep rather
    /// than typed, so a re-seed moves it instead of breaking it.</summary>
    private static string TheOfficesGround()
    {
        foreach (string body in Grounds())
        {
            if (StopOrder.TheOfficeGetsThisOne(new DisclosureClock.Opening(body, 0)))
            {
                return body;
            }
        }
        throw new InvalidOperationException("no ground in the sweep goes to the office — the split is a switch");
    }

    /// <summary>The upper canteen on this floor, or null where this floor has none.</summary>
    private static UndergroundComplex.Amenity? TheCanteenOn(string body, int level)
    {
        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(body, level, Field).Amenities)
        {
            if (a.Use == UndergroundComplex.Comfort.UpperCanteen)
            {
                return a;
            }
        }
        return null;
    }
}
