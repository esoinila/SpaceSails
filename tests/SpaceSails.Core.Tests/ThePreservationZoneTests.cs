using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 beat 2 · THE PRESERVATION ZONE — the cheapest hide anybody ever built, and the second stage of the
/// office's paperwork.
///
/// <para>#1074, verbatim: <i>"The cheapest hide is official care: a site fenced, signed, and studied forever
/// — the easiest way to hide a discovery is to build a national park around it."</i></para>
///
/// <para>Every guard below was watched go RED against a revert (or, where a law has no behaviour of its own
/// to revert, against a PLANT of the exact mistake it exists to catch); which one, and the words the runner
/// printed, are quoted on each. A guard that has never failed is a guard nobody has checked — #587's lesson,
/// and this file keeps it.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, and it is deliberately a family of
/// ids no other suite asks about (<see cref="Grounds"/>), for <c>TheStopOrderAtTheDigTests</c>' reason: both
/// registers only ever change the answer for the ids IN them, so a guard here that installs a ground of its
/// OWN cannot move any other guard's world, whatever order xUnit runs them in.</para>
/// </summary>
public sealed class ThePreservationZoneTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls. The band is about one
    /// site in fifty; a ten-site sample tells you nothing about it. Same number and same reasoning as
    /// <c>TheStopOrderAtTheDigTests</c>' own sweep.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>EVERY GROUND IN THE SWEEP THAT ACTUALLY HAS HALLS, derived off an id family this file owns
    /// alone — asserted to be a real population rather than merely non-empty, because a population of one
    /// proves nothing and an empty one passes every negative law in this file for the wrong reason (the
    /// fifth named bug class).</summary>
    private static List<string> Grounds() => _grounds ??= Sweep();

    private static List<string>? _grounds;

    private static List<string> Sweep()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"care-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated care grounds had halls — this proves little.");
        return found;
    }

    /// <summary>Close the working AND take the site into care for the length of one guard, and put the world
    /// back afterwards whatever happens. Both registers move together because that is the only state the
    /// beat has: a zone stands on a closed working and never anywhere else.</summary>
    private static IDisposable InCare(params string[] bodies)
    {
        StopOrder.Install([.. bodies]);
        PreservationZone.Install([.. bodies]);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose()
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    // ══ LAW 1 · TWO WHOLE WINDOWS, AND NEVER WHILE HE IS THERE ═══════════════════════════════════════════

    /// <summary>
    /// #1074 · THE FENCE COMES AFTER TWO WHOLE WINDOWS — one more than the order took — AND NEVER LANDS WITH
    /// THE CAPTAIN STANDING ON THE GROUND.
    ///
    /// <para>The extra shift IS the beat. The order closed the working <i>pending structural review</i> with
    /// no published schedule; a window later there is still no schedule, because the review that was never
    /// scheduled has become a study that never ends. So the guard asks the threshold at three moments and the
    /// middle one is the one that matters: at ONE whole window — the moment the order itself lands — nothing
    /// is fenced, and only at TWO does the site pass into care.</para>
    ///
    /// <para>The off-body clause is asked as the SAME call with one argument changed, which is what makes it
    /// a guard about the clause rather than about the clock.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>WindowsBeforePreserving</c> lowered to 1, so the fence went
    /// up on the order's own shift — <i>"Assert.Empty() Failure: Collection was not empty. Collection: [\"…\"]"</i>
    /// on the one-window call.</para>
    /// </summary>
    [Fact]
    public void TheFenceComesAfterTwoWholeWindowsAndNeverWhileHeIsThere()
    {
        string body = Grounds()[0];
        IReadOnlyList<DisclosureClock.Opening> register = [new(body, 0)];
        IReadOnlyList<string> closed = [body];

        // Inside the window it was opened in: nothing.
        Assert.Empty(PreservationZone.Note(
            register, closed, [], null, DisclosureClock.WindowSeconds * 0.99));

        // ONE whole window — the shift the ORDER lands on. Still nothing: the fence is not the order.
        Assert.Empty(PreservationZone.Note(
            register, closed, [], null, DisclosureClock.WindowSeconds * 1.0));

        // TWO whole windows, and he is elsewhere: fenced.
        Assert.Equal([body], PreservationZone.Note(
            register, closed, [], null, DisclosureClock.WindowSeconds * 2.0));

        // …and the same moment with him standing on it: nothing.
        Assert.Empty(PreservationZone.Note(
            register, closed, [], body, DisclosureClock.WindowSeconds * 2.0));

        // Care begins once, and the register comes back BY REFERENCE when there is nothing to add, so a
        // caller can compare and only then ask for a save.
        IReadOnlyList<string> had = [body];
        Assert.Same(had, PreservationZone.Note(
            register, closed, had, null, DisclosureClock.WindowSeconds * 9.0));
    }

    // ══ LAW 2 · ONLY ON A CLOSED WORKING ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · NOTHING IS FENCED ON A GROUND NOBODY CLOSED — and therefore nothing is ever fenced on a
    /// BURIED one.
    ///
    /// <para>A preservation zone is what a stop order hardens into; it is not a thing that happens to open
    /// ground. So the register of closed workings is the gate, and asking that one question answers two: a
    /// ground is stopped or buried and never both (<see cref="StopOrder.TheOfficeGetsThisOne"/>), so a
    /// ground the split handed to the neighbours can never be in the stop register and can never be fenced.
    /// The guard says both halves out loud — a due, opened, UNSTOPPED ground gets nothing even when the
    /// split says it is the office's, and every ground on the neighbours' side of the split gets nothing
    /// however long it has been.</para>
    ///
    /// <para><b>Both populations are asserted to be real</b>, because a sweep that found no neighbours'
    /// grounds would pass this guard perfectly while proving nothing (the fifth named bug class).</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>Contains(stopped, …)</c> clause taken out of
    /// <c>PreservationZone.Note</c> — <i>"the office fenced ground nobody closed: 104 of 104 open grounds
    /// came back fenced"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingIsFencedOnAGroundNobodyClosed()
    {
        List<string> grounds = Grounds();
        double late = DisclosureClock.WindowSeconds * 9.0;

        // A: due, opened, and the office's by the split — but the working was never closed.
        var openings = new List<DisclosureClock.Opening>();
        var neighbours = new List<string>();
        int offices = 0;
        foreach (string body in grounds)
        {
            var opening = new DisclosureClock.Opening(body, 0);
            openings.Add(opening);
            if (StopOrder.TheOfficeGetsThisOne(opening))
            {
                offices++;
            }
            else
            {
                neighbours.Add(body);
            }
        }
        Assert.True(offices > 10 && neighbours.Count > 10,
            $"the split gave {offices} grounds to the office and {neighbours.Count} to the neighbours — " +
            "one of these populations is too small for this guard to mean anything.");

        IReadOnlyList<string> fenced = PreservationZone.Note(openings, [], [], null, late);
        Assert.True(fenced.Count == 0,
            $"the office fenced ground nobody closed: {fenced.Count} of {openings.Count} open grounds " +
            "came back fenced");

        // B: the neighbours' grounds, handed in as though somebody had tried to close them anyway. The stop
        // register is the gate, so what keeps a buried ground unfenced is that it is never IN that register —
        // which is exactly what Burial and StopOrder partition by construction.
        foreach (string body in neighbours.Take(5))
        {
            Assert.Empty(PreservationZone.Note(
                [new DisclosureClock.Opening(body, 0)], [], [], null, late));
        }
    }

    // ══ LAW 3 · THE RING, AND THE ONE GAP IN IT ══════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE RAIL IS A CLOSED RING WITH EXACTLY ONE GAP IN IT, AND THE GAP FACES THE TUBE.
    ///
    /// <para>This is the law that keeps a captain out of a cage. The lift car comes up in the middle of this
    /// ring; a fence with no gap shuts him in beside his own boat, and a fence whose gap faces a seeded
    /// bearing puts a heritage rail between him and the way home on half the seeds. So the ring is walked
    /// end to end: every rail's far end is another rail's near end except for exactly ONE dangling pair, that
    /// pair is the published gap, and the bearing from the ring's centre to the middle of the gap is the
    /// bearing to the tube mouth to within a rounding error.</para>
    ///
    /// <para>The gap is also measured: it has to be a way through for a BODY, not a hairline. The captain is
    /// 1.4 du across and the rails have no thickness, so anything that clears his diameter with room to walk
    /// it badly is a gate — the same 1.6 du half that <c>SurfaceStructure.DoorwayHalf</c> gives every doorway
    /// in the game.</para>
    ///
    /// <para>Driven over every ground in the sweep, with each site's REAL shed
    /// (<see cref="SecretLab.HeadHut"/>) rather than a hand-built rectangle: the ring is laid around a
    /// building whose size, thickness and angle are all seeded, and a guard that fenced a tidy box of its own
    /// would be auditing a site the game does not ship.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the gap side drawn like every other side (the loop started at
    /// <c>k = 0</c>) — <i>"the ring is not a ring with one gap: care-ground-11: 0 dangling ends, expected
    /// exactly 1"</i>; and the ring laid on a fixed bearing instead of the home bearing —
    /// <i>"care-ground-11: the gap bears 1.9060 rad from the centre and the tube bears -1.4835 rad"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRingHasExactlyOneGapAndItFacesTheTube()
    {
        const double eps = 1e-6;
        var wrong = new List<string>();
        int rung = 0;

        foreach (string body in Grounds())
        {
            SurfaceStructure.Spec hut = SecretLab.HeadHut(body, "", Field);
            PreservationZone.Fence fence = PreservationZone.FenceAround(hut, Field);
            rung++;

            // ONE BREAK IN THE LOOP. Every rail's end must be some other rail's start; count the ones that
            // are not. A closed ring with one gap has exactly one dangling end and exactly one dangling
            // start, and they are the two corners of the gap.
            var dangling = new List<(double X, double Y)>();
            foreach (SurfaceLayout.Wall r in fence.Rails)
            {
                bool joined = fence.Rails.Any(o =>
                    Math.Abs(o.X1 - r.X2) < eps && Math.Abs(o.Y1 - r.Y2) < eps);
                if (!joined)
                {
                    dangling.Add((r.X2, r.Y2));
                }
            }
            if (dangling.Count != 1)
            {
                wrong.Add($"  {body}: {dangling.Count} dangling ends, expected exactly 1");
                continue;
            }

            // …and the dangling end is one corner of the PUBLISHED gap, so the picture and the arithmetic
            // are talking about the same opening rather than about two that happen to be the same size.
            (double dx, double dy) = dangling[0];
            bool matchesGap =
                (Math.Abs(dx - fence.GapX1) < eps && Math.Abs(dy - fence.GapY1) < eps)
                || (Math.Abs(dx - fence.GapX2) < eps && Math.Abs(dy - fence.GapY2) < eps);
            if (!matchesGap)
            {
                wrong.Add($"  {body}: the break in the rail is not where the published gap is");
            }

            // THE GAP FACES THE TUBE.
            double toGap = Math.Atan2(
                fence.GapCentreY - fence.CentreY, fence.GapCentreX - fence.CentreX);
            double toTube = Math.Atan2(Field.TopY - fence.CentreY, Field.HomeX - fence.CentreX);
            double off = Math.Abs(Math.Atan2(Math.Sin(toGap - toTube), Math.Cos(toGap - toTube)));
            if (off > 1e-9)
            {
                wrong.Add(
                    $"  {body}: the gap bears {toGap:F4} rad from the centre and the tube bears " +
                    $"{toTube:F4} rad");
            }

            // …and it is a gate rather than a hairline.
            if (fence.GapWidth < SurfaceStructure.DoorwayHalf * 2)
            {
                wrong.Add($"  {body}: the gap is {fence.GapWidth:F2} du — narrower than a doorway");
            }

            // The ring stands clear of the shed it is fencing, or it is not a fence, it is a wall on a roof.
            if (fence.Radius <= SurfaceStructure.EnvelopeOf(hut).Reach)
            {
                wrong.Add($"  {body}: the rail is inside the building it is fencing");
            }
        }

        Assert.True(rung > 40, $"only {rung} ring(s) were laid — this proves little.");
        Assert.True(wrong.Count == 0,
            "the ring is not a ring with one gap facing the tube:\n" + string.Join("\n", wrong));
    }

    // ══ LAW 4 · WHAT IS WRITTEN ON IT ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE NOTICE IS THE CANON'S SENTENCE, VERBATIM, UNDER AN OFFICE'S STAMP — WITH NO DATE, NO
    /// DEPARTMENT AND NO NAME.
    ///
    /// <para><b>The date is the load-bearing absence.</b> #1074: <i>"It says nothing about 1879, and there is
    /// no date on the study."</i> A study with a start date is a study somebody could ask about the progress
    /// of; a study with none is a door that stays shut for ever, and nothing on the sign says so. So the
    /// guard reads the notice for DIGITS and finds none — not a year, not a file number, not a reference.
    /// </para>
    ///
    /// <para>The stamp is beat 1's own (<see cref="StopOrder.Stamp"/>) and not a second one spelled alike,
    /// so the notice at the gate and the plate at the seal come from the same office by construction. And
    /// nobody signed it: no department out of either stock, no signature field of any kind, and the
    /// proper-noun sweep finds no word that could be a person. All-capital words are exempt from that sweep
    /// and only from that sweep — the notice is SHOUTED, the way every plate in this game is, and shouting is
    /// not naming.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the sign re-authored with the canon's own excluded date
    /// (<i>"…under study since 1879."</i>) — <i>"the notice carries a date: '1', '8', '7', '9'"</i>; and the
    /// stamp swapped for a department (<c>PLANT</c>) — <i>"Assert.Contains() Failure: Sub-string not found.
    /// String: PLANT — THIS SITE IS PRESERVED. Its s… Not found: AUTHORITY"</i>.</para>
    /// </summary>
    [Fact]
    public void TheNoticeIsVerbatimAndStampedAndCarriesNoDateAndNoName()
    {
        // THE SENTENCE, character for character as #1074 authored it. Written out here rather than compared
        // to itself: this is the one place in the repo that says what the canon actually said, so a silent
        // re-authoring upstream has something to fail against.
        Assert.Equal(
            "THIS SITE IS PRESERVED. Its significance is under study.", PreservationZone.Sign);

        // …and what is posted is that sentence under beat 1's own stamp, with nothing else added.
        Assert.Contains(StopOrder.Stamp, PreservationZone.Notice, StringComparison.Ordinal);
        Assert.EndsWith(PreservationZone.Sign, PreservationZone.Notice, StringComparison.Ordinal);
        Assert.True(PreservationZone.IsNotice(PreservationZone.Notice));
        Assert.False(PreservationZone.IsNotice(StopOrder.Plate));

        // NO DATE, ANYWHERE IN IT — and no number of any other kind either, since a reference number is a
        // thing a person could chase and the point is that there is nothing to chase.
        var digits = PreservationZone.Notice.Where(char.IsDigit).ToList();
        Assert.True(digits.Count == 0,
            "the notice carries a date: " + string.Join(", ", digits.Select(d => $"'{d}'")));

        // No department, from either stock — this building's or the head office's.
        foreach (string department in
            UndergroundComplex.Departments.Concat(UndergroundComplex.HeadOfficeDepartments))
        {
            Assert.DoesNotContain(department, PreservationZone.Notice, StringComparison.OrdinalIgnoreCase);
        }

        // …and nobody signed it. Not an illegible signature, not an initial: the field does not exist.
        foreach (string signing in new[] { "signed", "signature", "sgd", "on behalf of", "for and on behalf" })
        {
            Assert.DoesNotContain(signing, PreservationZone.Notice, StringComparison.OrdinalIgnoreCase);
        }

        // THE PROPER-NOUN SWEEP. Every capitalised word is either the first word of a sentence, or shouted
        // whole (the plate idiom), or the office itself. A name planted anywhere is a word this catches
        // mechanically, which is the only form of "no name" a test can actually hold.
        var named = new List<string>();
        bool sentenceStart = true;
        foreach (string token in
            PreservationZone.Notice.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string word = token.Trim('.', ',', ';', ':', '—', '(', ')', '"');
            bool shouted = word.Length > 1 && word.All(c => !char.IsLetter(c) || char.IsUpper(c));
            if (word.Length > 0 && char.IsUpper(word[0]) && !sentenceStart && !shouted)
            {
                named.Add(word);
            }
            sentenceStart = token.EndsWith('.') || token.EndsWith(':') || token.EndsWith('—');
        }
        Assert.True(named.Count == 0, "the notice names somebody: " + string.Join(", ", named));
    }

    // ══ LAW 5 · NOTHING BELOW GROUND MOVES ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · A PRESERVED SITE'S BUILDING IS THE STOPPED SITE'S BUILDING, WALL FOR WALL. The halls stay
    /// sealed by beat 1's order and every band predicate answers what it answered before.
    ///
    /// <para>The zone is <b>surface furniture and persistence and nothing else</b>. There is no mechanic
    /// down there to gain and none to lose: the seal is still on the listed bottom wearing the order's plate,
    /// the lift still declines the band nobody listed, the halls are still found, and the site's true depth
    /// has not moved. Every floor of every sampled ground is built twice — once merely stopped, once stopped
    /// AND in care — and the two buildings are compared segment by segment, which is the whole law said in
    /// one comparison.</para>
    ///
    /// <para><b>Plant that reddened it</b> (there is no behaviour of mine to revert — the law is that a
    /// second beat did NOT reach into a first one's building, so the failure it guards against has to be
    /// planted): <c>UndergroundComplex.StopSealFloorOf</c> taught to return null once
    /// <c>PreservationZone.On</c> is true, i.e. "the fence replaces the seal" —
    /// <i>"care changed the building: care-ground-11 B7: 1 locked door stopped, 0 in care"</i>.</para>
    /// </summary>
    [Fact]
    public void CareChangesNothingBelowGround()
    {
        var wrong = new List<string>();
        int compared = 0;

        foreach (string body in Grounds().Take(4))
        {
            var stoppedOnly = new Dictionary<int, UndergroundComplex.FloorPlan>();
            StopOrder.Install([body]);
            PreservationZone.Install([]);
            try
            {
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    stoppedOnly[level] = UndergroundComplex.Build(body, level, Field);
                }
                Assert.True(UndergroundComplex.HasFoundBand(body));
                int depth = UndergroundComplex.TrueDepthOf(body);
                int? seal = UndergroundComplex.StopSealFloorOf(body);
                Assert.NotNull(seal);

                using (InCare(body))
                {
                    if (!UndergroundComplex.HasFoundBand(body))
                    {
                        wrong.Add($"  {body}: HasFoundBand went false under the fence");
                    }
                    if (UndergroundComplex.TrueDepthOf(body) != depth)
                    {
                        wrong.Add($"  {body}: the true depth moved under the fence");
                    }
                    if (UndergroundComplex.StopSealFloorOf(body) != seal)
                    {
                        wrong.Add($"  {body}: the seal moved under the fence");
                    }
                    if (Burial.IsFilled(body))
                    {
                        wrong.Add($"  {body}: a ground in care came back FILLED IN");
                    }

                    foreach ((int level, UndergroundComplex.FloorPlan before) in stoppedOnly)
                    {
                        UndergroundComplex.FloorPlan after =
                            UndergroundComplex.Build(body, level, Field);
                        compared++;
                        if (before.Walls.Count != after.Walls.Count
                            || before.Locked.Count != after.Locked.Count)
                        {
                            wrong.Add(
                                $"  {body} B{-level}: {before.Walls.Count} walls / " +
                                $"{before.Locked.Count} locked doors stopped, {after.Walls.Count} / " +
                                $"{after.Locked.Count} in care");
                            continue;
                        }
                        for (int i = 0; i < before.Locked.Count; i++)
                        {
                            if (!string.Equals(before.Locked[i].Sign, after.Locked[i].Sign,
                                StringComparison.Ordinal))
                            {
                                wrong.Add($"  {body} B{-level}: locked door {i}'s plate changed");
                            }
                        }
                    }
                }
            }
            finally
            {
                StopOrder.Install([]);
                PreservationZone.Install([]);
            }
        }

        Assert.True(compared > 8, $"only {compared} floor(s) were compared — this proves little.");
        Assert.True(wrong.Count == 0, "care changed the building:\n" + string.Join("\n", wrong));
    }

    // ══ LAW 6 · THE SCULLY LAW, AND §8's RESERVED WORD ═══════════════════════════════════════════════════

    /// <summary>§8 — there is ONE of these and a heritage notice never borrows the word. Everything else in
    /// the list is a word that would settle the question the feature exists to leave open. The same list
    /// <c>TheStopOrderAtTheDigTests</c> and <c>TheBurialTests</c> keep, because a word that is forbidden on
    /// one piece of the office's paper is forbidden on all of them.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #1074/#672 · NO STRING ON THE PRESERVATION ZONE'S PATH CONTAINS THE WORD §8 RESERVES, AND THE TYPE
    /// PUBLISHES EXACTLY ONE SENTENCE.
    ///
    /// <para>A reasonable person reads a heritage notice on a working a structural review closed, and they
    /// are not being fooled: preservation is a real ethic and the sign is true. It is also the ONLY new
    /// player-facing string this beat adds — swept by reflection, exactly as the clock's own guard sweeps the
    /// clock, so a helpful sentence added later cannot escape the canon grep by not being in
    /// <c>AllProse</c>.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the word planted in the sign — <i>"a preservation string
    /// settles what it must leave open: PreservationZone.AllProse: 'ancient' in 'AUTHORITY — THIS SITE IS
    /// PRESERVED. Its ancient significance is under study.'"</i>; and a second sentence added to the type
    /// (<c>public const string Assurance = "Access will be restored in due course.";</c>) —
    /// <i>"PreservationZone publishes a string no canon grep can see: 'Access will be restored in due
    /// course.'"</i>.</para>
    /// </summary>
    [Fact]
    public void NoStringOnThePreservationPathNamesTheReservedThing()
    {
        var named = new List<string>();
        foreach (string text in PreservationZone.AllProse())
        {
            foreach (string word in Forbidden)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    named.Add($"PreservationZone.AllProse: \"{word}\" in \"{text}\"");
                }
            }
        }
        Assert.True(named.Count == 0,
            "a preservation string settles what it must leave open:\n  " + string.Join("\n  ", named));

        // …and there is no second string hiding on the type. Every public const string it publishes is the
        // notice, or a substring of it — which the bare sign is.
        var published = new List<string>();
        foreach (System.Reflection.FieldInfo f in
            typeof(PreservationZone).GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string value)
            {
                published.Add(value);
            }
        }
        Assert.NotEmpty(published);
        Assert.All(published, s =>
            Assert.True(PreservationZone.Notice.Contains(s, StringComparison.Ordinal),
                $"PreservationZone publishes a string no canon grep can see: \"{s}\""));
    }
}
