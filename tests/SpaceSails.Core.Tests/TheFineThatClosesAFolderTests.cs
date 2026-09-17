using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #711 slice 1 · <b>THE PARCEL THE SEARCH IS MEANT TO STOP AT, HELD TO ITS OWN LAWS.</b>
///
/// <para>Canon design (head coder, 2026-08-09): getting caught at the sacrifice <i>"COSTS something real …
/// and PAYS something real (that entity's suspicion is spent; the challenge/inspection outcome for them is
/// settled until new cause)"</i>. Eight laws, and every one of them is asked of a world wide enough to tell
/// pass from fail:</para>
///
/// <list type="number">
/// <item>a parcel is CARGO and is not evidence to any evidence reader;</item>
/// <item>the fine is <see cref="BustedRule.BribeDemand"/>'s, called rather than restated — and the world
/// really contains more than one answer;</item>
/// <item>a find writes ONE record and settles that outfit's next outcome;</item>
/// <item>the find's OWN heat can never re-open the folder it just closed;</item>
/// <item>new cause re-opens it, through the band function and through nothing else — and hours never
/// do;</item>
/// <item>the exception is exactly one outfit per world seed, deterministic, and never the first;</item>
/// <item>the two arms are shaped as the design says: a fine ends the read, the tell does not;</item>
/// <item>the three lines are verbatim, the reserved words are nowhere, and the folder survives the
/// vault.</item>
/// </list>
/// </summary>
public sealed class TheFineThatClosesAFolderTests
{
    /// <summary>The scenario's own moons plus a wide net of generated ids — the discipline
    /// <c>TheSafetyInspectorsCardTests</c> keeps: the scenario ten prove the game people play, the generated
    /// ninety prove the GENERATOR, and a law that only holds on the shipped list is not a law.</summary>
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

    /// <summary>A body whose operator is the one the caller asked for — so a test about ONE outfit can put
    /// the captain on that outfit's ground rather than on whichever moon happened to be first.</summary>
    private static string ABodyRunBy(string operatorId)
    {
        foreach (string body in ManySites())
        {
            if (string.Equals(SiteOperator.Of(body).Id, operatorId, StringComparison.Ordinal))
            {
                return body;
            }
        }
        Assert.Fail($"no site in this world is run by {operatorId} — the bench cannot pose the question.");
        return string.Empty;
    }

    // ── (1) A PARCEL IS CARGO, AND NOTHING READS IT AS EVIDENCE ─────────────────────────────────────────

    /// <summary>
    /// <b>THE PARCEL IS NOT EVIDENCE TO ANY EVIDENCE READER.</b> Driven through the two functions that
    /// actually decide it — <see cref="RipAndBin.IsEvidence"/> and <see cref="Satchel.CompartmentOf"/> —
    /// and asked of EVERY kind in the enum rather than of the new one, so the law is "exactly the sleeve's
    /// two kinds are evidence" rather than "this one happens not to be today".
    ///
    /// <para>It is the separation the whole object rests on: a sacrifice has to look like petty smuggling
    /// and not like a man carrying somebody else's paperwork, and three systems (the bin picker, the seated
    /// spread, the left-behind gist) decide what a thing IS by asking these two questions.</para>
    ///
    /// <para><b>RED</b> by adding <c>Satchel.Kind.Parcel</c> to <see cref="RipAndBin.IsEvidence"/>'s list:
    /// <i>Assert.False() Failure — a parcel reads as evidence</i>. And <b>RED the other way</b> by listing
    /// <c>Kind.Parcel</c> beside <c>Paper</c>/<c>Dirt</c> in <see cref="Satchel.CompartmentOf"/>:
    /// <i>a parcel rides the document sleeve</i>.</para>
    /// </summary>
    [Fact]
    public void AParcelIsCargoAndNoEvidenceReaderThinksOtherwise()
    {
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("luna", 3);

        Assert.False(RipAndBin.IsEvidence(parcel.Kind), "a parcel reads as evidence.");
        Assert.NotEqual(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(parcel.Kind));
        Assert.Equal(Satchel.Compartment.Pocket, Satchel.CompartmentOf(parcel.Kind));

        // …and the law stated over the WHOLE enum, so it cannot be true by coincidence: the kinds that are
        // evidence are exactly the kinds that ride the sleeve, and there are exactly two of them.
        var evidence = new List<Satchel.Kind>();
        var sleeve = new List<Satchel.Kind>();
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            if (RipAndBin.IsEvidence(kind))
            {
                evidence.Add(kind);
            }
            if (Satchel.CompartmentOf(kind) == Satchel.Compartment.Sleeve)
            {
                sleeve.Add(kind);
            }
        }
        Assert.Equal(evidence, sleeve);
        Assert.Equal(2, evidence.Count);
        Assert.DoesNotContain(Satchel.Kind.Parcel, evidence);

        // …and the sleeve itself, driven: a wallet holding the parcel and one real sheet has ONE thing in
        // the sleeve and one in the pocket. A count is the thing the bin picker walks.
        IReadOnlyList<Satchel.Item> carried =
            Satchel.Add(Satchel.Add([], parcel), new Satchel.Item(Satchel.Kind.Paper, "some-sheet"));
        Assert.Equal(1, Satchel.Used(carried, Satchel.Compartment.Sleeve));
        Assert.Equal(1, Satchel.Used(carried, Satchel.Compartment.Pocket));

        // It is also not a thing a guard reads out of a wallet: a box is not a paper.
        Assert.False(WalletChoice.AGuardWouldReadIt(parcel.Kind));
    }

    // ── (2) THE FINE IS THE BRIBE'S OWN FUNCTION, CALLED ────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FINE IS <see cref="BustedRule.BribeDemand"/>, CALLED, NEVER RESTATED</b> — asked over the
    /// whole of the meter's range and a spread of seeds, against the function itself rather than against a
    /// number typed here.
    ///
    /// <para>And the anti-vacuity half, which is the part that matters: the world really contains more than
    /// one answer. The bribe's band table steps at heat 2 and again above it, so a fine at the meter's
    /// ceiling has to be strictly dearer than one at nothing — a guard that only checked equality would be
    /// green against a <c>TheFine</c> that returned a constant, which is the fifth named bug class.</para>
    ///
    /// <para><b>RED</b> by doubling the call (<c>2 * BustedRule.BribeDemand(...).Total</c>):
    /// <i>Assert.Equal() Failure: Expected 312, Actual 624</i>.</para>
    /// </summary>
    [Fact]
    public void TheFineIsTheBribesOwnFunctionAndMovesWithTheMeter()
    {
        int seen = 0;
        for (int heat = 0; heat <= IllegalHeat.Ceiling; heat++)
        {
            for (ulong seed = 1; seed < 24; seed++)
            {
                Assert.Equal(BustedRule.BribeDemand(heat, seed).Total, UnlistedParcel.TheFine(heat, seed));
                seen++;
            }
        }
        Assert.True(seen > 100, "this sweep is too small to mean anything.");

        // The world can tell pass from fail: the quote really moves, and it moves the way the bribe's own
        // ladder moves. Averaged over seeds so the assertion is about the BAND and not about one roll.
        static double Average(int heat)
        {
            double sum = 0;
            for (ulong seed = 1; seed < 200; seed++)
            {
                sum += UnlistedParcel.TheFine(heat, seed);
            }
            return sum / 199.0;
        }

        Assert.True(Average(IllegalHeat.Ceiling) > Average(2), "the ceiling is not dearer than heat 2.");
        Assert.True(Average(2) > Average(0), "heat 2 is not dearer than a clean captain.");
        Assert.True(Average(0) > 0, "a clean captain is quoted nothing at all.");
    }

    // ── (3) ONE RECORD, AND IT SETTLES THE NEXT OUTCOME ─────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FIND WRITES ONE RECORD, AND THAT OUTFIT'S NEXT OUTCOME IS SETTLED.</b> Before: nothing on the
    /// book and nothing settled. After: one row for that outfit, its folder closed, the crossing's own
    /// weight banked and not a point more, and <see cref="UnlistedParcel.TheFolderIsClosed"/> true — while
    /// every OTHER outfit in the world is exactly where it was.
    ///
    /// <para>That last clause is #715's law and is asserted here because this feature is the first thing to
    /// write a second kind of fact into that book: a folder that leaked to a competitor would be the Vegas
    /// cheaters' list the owner ruled out.</para>
    ///
    /// <para><b>RED</b> by deleting the <c>book.CloseTheFolder(...)</c> call from
    /// <see cref="UnlistedParcel.TheParcelIsWhatIsFound"/>: <i>Assert.True() Failure — the folder did not
    /// close</i>.</para>
    /// </summary>
    [Fact]
    public void OneFindWritesOneRecordAndSettlesThatOutfitOnly()
    {
        var book = new ContactLedger();
        string here = "luna";
        string outfit = SiteOperator.Of(here).Id;

        Assert.False(UnlistedParcel.TheFolderIsClosed(book, here));
        Assert.Equal(0, IllegalHeat.HeatAtSite(book, here));

        UnlistedParcel.Found found = UnlistedParcel.TheParcelIsWhatIsFound(book, here, 4242UL, 100.0, 7UL);

        Assert.True(found.Fined, "the first find is always a fine — the exception may not open the account.");
        Assert.Equal(UnlistedParcel.FineLine, found.Line);
        Assert.False(found.TheReadGoesOn);
        Assert.True(found.Fine > 0, "a fine of nothing is not a cost.");

        Assert.True(UnlistedParcel.TheFolderIsClosed(book, here), "the folder did not close.");
        Assert.True(book.For(IllegalHeat.LedgerId(outfit)).FolderClosed);
        Assert.Equal(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.AnUnlistedParcelAboard),
            IllegalHeat.HeatAtSite(book, here));

        // ONE row, and it is theirs. Nobody else in the world has heard a thing.
        Assert.Equal(1, book.Entries.Values.Count(h => h.FolderClosed));
        int strangers = 0;
        foreach (string other in ManySites())
        {
            if (string.Equals(SiteOperator.Of(other).Id, outfit, StringComparison.Ordinal))
            {
                continue;
            }
            Assert.False(UnlistedParcel.TheFolderIsClosed(book, other));
            Assert.Equal(0, IllegalHeat.HeatAtSite(book, other));
            strangers++;
        }
        Assert.True(strangers > 5, "this world has too few outfits to prove anything about leakage.");
    }

    // ── (4) THE FIND'S OWN HEAT NEVER RE-OPENS WHAT IT CLOSED ───────────────────────────────────────────

    /// <summary>
    /// <b>THE POINT THE FIND ITSELF BANKS CAN NEVER READ AS THE NEW CAUSE THAT RE-OPENS IT.</b> Asked at
    /// EVERY standing the meter can hold before the find — including the ones sitting exactly one point
    /// under a band edge, which is where an order bug would actually bite and where it would be invisible
    /// at any other starting heat.
    ///
    /// <para>This is the ordering law inside <see cref="UnlistedParcel.TheParcelIsWhatIsFound"/>, and it is
    /// the reason that method exists instead of three calls in a client.</para>
    ///
    /// <para><b>RED</b> by reading the rung BEFORE the crossing is banked (hoisting the
    /// <c>IllegalHeat.StartingRung</c> call above <c>IllegalHeat.Bank</c>): the run fails at the standings
    /// one under a band edge — <i>heat 3: the find re-opened its own folder</i> — and is green at every
    /// other one, which is exactly why the sweep is over the whole range.</para>
    /// </summary>
    [Fact]
    public void TheFindsOwnHeatNeverReOpensTheFolderItClosed()
    {
        int edges = 0;
        for (int standing = 0; standing < IllegalHeat.Ceiling; standing++)
        {
            var book = new ContactLedger();
            string here = "luna";

            // Put the meter exactly where we want it using the meter's own seam, never a typed field.
            for (int i = 0; i < standing; i++)
            {
                IllegalHeat.Bank(
                    book, IllegalHeat.Charge(here, IllegalHeat.Crossing.RefusedCardAtAGate), 10.0);
            }
            Assert.Equal(standing, IllegalHeat.HeatAtSite(book, here));

            int rungBefore = IllegalHeat.StartingRung(standing);
            UnlistedParcel.TheParcelIsWhatIsFound(book, here, 11UL, 50.0, 3UL);

            Assert.True(
                UnlistedParcel.TheFolderIsClosed(book, here),
                $"heat {standing}: the find re-opened its own folder.");

            if (IllegalHeat.StartingRung(IllegalHeat.HeatAtSite(book, here)) > rungBefore)
            {
                edges++;
            }
        }

        Assert.True(
            edges > 0,
            "no starting heat in this sweep sat on a band edge — the guard never met the case it exists for.");
    }

    // ── (5) NEW CAUSE, AND ONLY NEW CAUSE ───────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NEW CAUSE IS A BAND OF THEIR OWN METER, MEASURED BY THE ONE FUNCTION THAT SAYS SO.</b> After a
    /// find, crossings are banked one at a time; the folder holds for every one of them until
    /// <see cref="IllegalHeat.StartingRung"/> answers higher than it did at the filing, and re-opens on
    /// exactly that one.
    ///
    /// <para>Both halves are asserted, because a guard that only watched it re-open would pass on a folder
    /// that never held at all — and the whole payoff of the mechanic is the holding.</para>
    ///
    /// <para>And HOURS never re-open it: <see cref="IllegalHeat.Cool"/> is run over a year of sim time with
    /// the captain nowhere near their ground, which walks the meter to nothing, and the answer is still on
    /// file. An answer does not go stale on its own; only the captain can stale it.</para>
    ///
    /// <para><b>RED</b> by comparing raw heat instead of the band (<c>filed.HeatOwed &lt;=
    /// filed.FolderClosedAtRung</c> in <see cref="UnlistedParcel.TheFolderIsClosedFor"/>): the folder then
    /// re-opens on the FIRST crossing after the fine — <i>the folder re-opened before a band moved: rung 0
    /// then, rung 0 now</i>.</para>
    /// </summary>
    [Fact]
    public void OnlyABandOfTheirOwnMeterReOpensIt()
    {
        var book = new ContactLedger();
        string here = "luna";
        string outfit = SiteOperator.Of(here).Id;

        UnlistedParcel.TheParcelIsWhatIsFound(book, here, 99UL, 0.0, 5UL);
        int filedAt = book.For(IllegalHeat.LedgerId(outfit)).FolderClosedAtRung;
        Assert.True(UnlistedParcel.TheFolderIsClosed(book, here));

        bool everReOpened = false;
        int heldThrough = 0;
        for (int i = 0; i < IllegalHeat.Ceiling * 2; i++)
        {
            IllegalHeat.Bank(book, IllegalHeat.Charge(here, IllegalHeat.Crossing.TheEscort), 10.0 + i);
            int rungNow = IllegalHeat.StartingRung(IllegalHeat.HeatAtSite(book, here));
            bool closed = UnlistedParcel.TheFolderIsClosed(book, here);

            if (rungNow > filedAt)
            {
                Assert.False(closed, $"a band moved ({filedAt} → {rungNow}) and the folder is still shut.");
                everReOpened = true;
                break;
            }

            Assert.True(
                closed,
                $"the folder re-opened before a band moved: rung {filedAt} then, rung {rungNow} now.");
            heldThrough++;
        }

        Assert.True(everReOpened, "no amount of crossing ever re-opened it — the meter cannot reach a band.");
        Assert.True(heldThrough > 0, "the folder never held through a single crossing — nothing was proved.");

        // …and HOURS are not new cause. A fresh find, then a year away from their ground.
        var second = new ContactLedger();
        UnlistedParcel.TheParcelIsWhatIsFound(second, here, 7UL, 0.0, 5UL);
        Assert.True(UnlistedParcel.TheFolderIsClosed(second, here));

        IllegalHeat.Cool(second, operatorIdUnderfoot: null, simTime: 365.0 * 24.0 * 3600.0);
        Assert.Equal(0, IllegalHeat.HeatAtSite(second, here));
        Assert.True(
            UnlistedParcel.TheFolderIsClosed(second, here),
            "a year of absence un-filed a form in somebody's drawer.");
    }

    // ── (6) THE ONE WHO KEEPS LOOKING ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EXACTLY ONE OUTFIT PER WORLD SEED, DETERMINISTIC, AND NEVER THE FIRST.</b> Three claims, each
    /// asked of a spread wide enough to tell pass from fail:
    ///
    /// <list type="bullet">
    /// <item><b>Exactly one.</b> For a given seed, precisely one member of <see cref="SiteOperator.All"/>
    /// answers — counted by walking the whole register, not by asking the function twice.</item>
    /// <item><b>Deterministic, and not a constant.</b> The same seed answers the same outfit a hundred times
    /// over; and across many seeds the world really produces MORE THAN ONE distinct answer, which is what
    /// stops this passing against a function that returns the first row.</item>
    /// <item><b>Never the first.</b> With an empty book every outfit reaches for the fine book — the chosen
    /// one included — because the mechanic is taught by the fine that closes a folder and there is no rule
    /// yet for the exception to break.</item>
    /// </list>
    ///
    /// <para><b>RED</b> by dropping the <see cref="UnlistedParcel.AFolderHasBeenClosedBefore"/> clause from
    /// <see cref="UnlistedParcel.HeReachesForTheFineBook"/>: <i>Assert.True() Failure — the exception took
    /// the captain's very first inspection</i>. And <b>RED the other way</b> by returning
    /// <c>world[0].Id</c> unseeded: <i>every world draws the same outfit — this is not a fact about a
    /// universe</i>.</para>
    /// </summary>
    [Fact]
    public void TheExceptionIsOneOutfitPerSeedAndNeverTheFirst()
    {
        var drawn = new HashSet<string>(StringComparer.Ordinal);

        for (ulong worldSeed = 1; worldSeed < 200; worldSeed++)
        {
            string? chosen = UnlistedParcel.TheOneWhoKeepsLooking(worldSeed);
            Assert.NotNull(chosen);
            drawn.Add(chosen!);

            // EXACTLY ONE, counted over the register rather than asked twice.
            int matches = SiteOperator.All.Count(
                op => string.Equals(op.Id, chosen, StringComparison.Ordinal));
            Assert.Equal(1, matches);

            // DETERMINISTIC.
            Assert.Equal(chosen, UnlistedParcel.TheOneWhoKeepsLooking(worldSeed));
        }

        Assert.True(
            drawn.Count > 1,
            "every world draws the same outfit — this is not a fact about a universe, it is a constant.");
        Assert.True(
            drawn.Count <= SiteOperator.All.Count,
            "the draw produced an outfit this register does not list.");

        // NEVER THE FIRST. An empty book, and every outfit in the world reaches for the fine book.
        const ulong seed = 41UL;
        string keepsLooking = UnlistedParcel.TheOneWhoKeepsLooking(seed)!;
        var fresh = new ContactLedger();
        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            Assert.True(
                UnlistedParcel.HeReachesForTheFineBook(op.Id, fresh, seed),
                $"{op.Id}: the exception took the captain's very first inspection.");
        }

        // …and once ANYBODY has closed a folder, that one — and only that one — stops reaching for it.
        string somewhereElse = ABodyRunBy(
            SiteOperator.All.First(op => !string.Equals(op.Id, keepsLooking, StringComparison.Ordinal)).Id);
        UnlistedParcel.TheParcelIsWhatIsFound(fresh, somewhereElse, 3UL, 10.0, seed);
        Assert.True(UnlistedParcel.AFolderHasBeenClosedBefore(fresh));

        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            bool reaches = UnlistedParcel.HeReachesForTheFineBook(op.Id, fresh, seed);
            Assert.Equal(!string.Equals(op.Id, keepsLooking, StringComparison.Ordinal), reaches);
        }
    }

    // ── (7) THE TWO ARMS, SHAPED AS THE DESIGN SAYS ─────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FINE ENDS THE READ AND CLOSES A FOLDER; THE TELL DOES NEITHER, AND THE PARCEL GOES EITHER
    /// WAY.</b> The exception is posed by standing the captain on the chosen outfit's own ground after
    /// somebody else has already fined him — the only way the world produces that arm — and then everything
    /// about it is compared with the ordinary arm at the same site.
    ///
    /// <para><b>RED</b> by closing the folder on both arms (moving <c>book.CloseTheFolder</c> outside the
    /// <c>if (fined)</c>): <i>Assert.False() Failure — the man who wrote nothing down filed something</i>.
    /// And <b>RED</b> by charging a fine on the exception: <i>Expected 0, Actual 312</i>.</para>
    /// </summary>
    [Fact]
    public void TheFineEndsTheReadAndTheTellDoesNot()
    {
        const ulong seed = 41UL;
        string keepsLooking = UnlistedParcel.TheOneWhoKeepsLooking(seed)!;
        string theirGround = ABodyRunBy(keepsLooking);
        string elsewhere = ABodyRunBy(
            SiteOperator.All.First(op => !string.Equals(op.Id, keepsLooking, StringComparison.Ordinal)).Id);

        var book = new ContactLedger();

        // The ordinary arm first, somewhere else — which is also what qualifies the exception.
        UnlistedParcel.Found ordinary =
            UnlistedParcel.TheParcelIsWhatIsFound(book, elsewhere, 8UL, 10.0, seed);
        Assert.True(ordinary.Fined);
        Assert.False(ordinary.TheReadGoesOn);
        Assert.Equal(UnlistedParcel.FineLine, ordinary.Line);

        int theirHeatBefore = IllegalHeat.HeatAtSite(book, theirGround);
        UnlistedParcel.Found tell =
            UnlistedParcel.TheParcelIsWhatIsFound(book, theirGround, 8UL, 20.0, seed);

        Assert.False(tell.Fined);
        Assert.Equal(0, tell.Fine);
        Assert.Equal(UnlistedParcel.TellLine, tell.Line);
        Assert.True(tell.TheReadGoesOn, "the inspection did not continue.");
        Assert.False(
            UnlistedParcel.TheFolderIsClosed(book, theirGround),
            "the man who wrote nothing down filed something.");

        // …and the heat rises anyway. He still found a box; he simply did not write a number on a form.
        Assert.Equal(
            theirHeatBefore + IllegalHeat.WeightOf(IllegalHeat.Crossing.AnUnlistedParcelAboard),
            IllegalHeat.HeatAtSite(book, theirGround));

        // THE PARCEL GOES EITHER WAY — the one thing the two arms agree about, driven through the satchel.
        IReadOnlyList<Satchel.Item> wallet = Satchel.Add(
            Satchel.Add([], UnlistedParcel.FromTheDesk("luna", 1)),
            PatrolBeat.Badge("luna"));
        Assert.True(UnlistedParcel.Held(wallet));

        IReadOnlyList<Satchel.Item> after = UnlistedParcel.Confiscated(wallet);
        Assert.False(UnlistedParcel.Held(after), "the box was handed back.");
        Assert.True(PatrolBeat.BadgeHeld("luna", after), "the confiscation took the wallet too.");

        // ONE PER HULL: a second desk has nothing to offer a captain already carrying one, whichever haven
        // the first came from.
        Assert.True(UnlistedParcel.Held([UnlistedParcel.FromTheDesk("phobos", 9)]));

        // The card the fine is told on is the ROUND'S own, and it is over: satisfied, no consequence.
        PatrolBeat.Read told = UnlistedParcel.TheFineIsTold("A ROUND");
        Assert.True(told.Satisfied);
        Assert.Null(told.Consequence);
        Assert.Equal(PatrolBeat.ChallengeLabel, told.Label);
        Assert.Equal(UnlistedParcel.FineLine, told.Told);

        // …and the tell's card is the wallet's own read with one sentence in front of it, and everything
        // under it — the verdict, the consequence — exactly as it reads on any other afternoon.
        PatrolBeat.Read refused =
            PatrolBeat.TheGuardReads("luna", 1, 0, "A ROUND", null, inspectionRunning: false);
        PatrolBeat.Read continued = UnlistedParcel.TheReadGoesOnAfterIt(refused);

        Assert.Equal(refused.Satisfied, continued.Satisfied);
        Assert.Equal(refused.Consequence, continued.Consequence);
        Assert.Equal(refused.Card, continued.Card);
        Assert.StartsWith(UnlistedParcel.TellLine, continued.Told, StringComparison.Ordinal);
        Assert.Contains(refused.Line, continued.Told, StringComparison.Ordinal);
        Assert.Contains(refused.Consequence!, continued.Told, StringComparison.Ordinal);
    }

    // ── (8) THE PROSE, THE RESERVED WORDS, AND THE VAULT ────────────────────────────────────────────────

    /// <summary>
    /// <b>THREE SENTENCES, VERBATIM, AND THERE IS NO FOURTH.</b> The canon pass authored a plate, a look
    /// card, a fine and a tell; this asserts them letter for letter, asserts they are all in
    /// <see cref="UnlistedParcel.AllProse"/>, and then reads the SLICE'S OWN FILES for string literals —
    /// because reflection catches a new <c>const</c> and does not catch a sentence typed straight into a
    /// method, which is how prose actually gets into this codebase.
    ///
    /// <para><b>RED</b> by pulsing a line of my own from <c>TakeTheUnlistedParcel</c>: <i>the parcel's own
    /// files carry a sentence nobody authored: "He does not ask what is in it."</i></para>
    /// </summary>
    [Fact]
    public void TheAuthoredStringsAreVerbatimAndThereIsNoFourth()
    {
        Assert.Equal("UNLISTED PARCEL", UnlistedParcel.Plate);
        Assert.Equal(
            "Somebody's small parcel with nobody's name on it. The kind of thing a hauler carries and does "
            + "not list.",
            UnlistedParcel.LookCardLine);
        Assert.Equal(
            "An unlisted parcel. Fined, filed, and the inspector is already looking at the next hull.",
            UnlistedParcel.FineLine);
        Assert.Equal(
            "An unlisted parcel. The inspector notes it, does not reach for the fine book, and keeps looking.",
            UnlistedParcel.TellLine);

        var prose = UnlistedParcel.AllProse().ToList();
        Assert.Contains(UnlistedParcel.Plate, prose);
        Assert.Contains(UnlistedParcel.LookCardLine, prose);
        Assert.Contains(UnlistedParcel.FineLine, prose);
        Assert.Contains(UnlistedParcel.TellLine, prose);

        // …and the game really says them: the two arms are composed, not read off the constants.
        Assert.Equal(UnlistedParcel.FineLine, UnlistedParcel.TheFineIsTold("A ROUND").Told);

        var authored = new HashSet<string>(prose, StringComparer.Ordinal);
        var sentences = new List<string>();
        foreach (string file in new[]
        {
            SourceOf("src", "SpaceSails.Core", "UnlistedParcel.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.UnlistedParcel.cs"),
        })
        {
            foreach (Match m in Regex.Matches(WithoutComments(file), "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                string text = m.Value.Trim('"');
                if (text.Contains(' ', StringComparison.Ordinal)
                    && text.EndsWith('.')
                    && !authored.Contains(text)
                    && !UnlistedParcel.FineLine.Contains(text, StringComparison.Ordinal)
                    && !UnlistedParcel.TellLine.Contains(text, StringComparison.Ordinal)
                    && !UnlistedParcel.LookCardLine.Contains(text, StringComparison.Ordinal)
                    && !UnlistedParcel.FineNote.Contains(text, StringComparison.Ordinal)
                    && !UnlistedParcel.TellNote.Contains(text, StringComparison.Ordinal))
                {
                    sentences.Add(m.Value);
                }
            }
        }

        Assert.True(sentences.Count == 0,
            "the parcel's own files carry a sentence nobody authored: "
            + string.Join(", ", sentences)
            + " — a beat that wants a line gets a // FABLE: marker rather than one typed by this crew.");
    }

    /// <summary>
    /// <b>THE RESERVED WORDS ARE NOWHERE NEAR IT</b> — canon's own law: <i>"No card names the pattern."</i>
    /// Asked of every sentence this feature can put on a screen, composed the way the game composes them
    /// rather than read off the constants, plus the satchel row and the look card the client draws.
    ///
    /// <para><b>RED</b> by writing the word into any one of them — e.g. renaming the desk verb to
    /// <c>"📦 Take it as cover"</c>: <i>Assert.DoesNotContain() Failure</i>.</para>
    /// </summary>
    [Fact]
    public void NoSentenceNamesThePattern()
    {
        var book = new ContactLedger();
        PatrolBeat.Read refused =
            PatrolBeat.TheGuardReads("luna", 1, 0, "A ROUND", null, inspectionRunning: false);
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("luna", 2);

        var said = new List<string>(UnlistedParcel.AllProse())
        {
            UnlistedParcel.CardLabel,
            UnlistedParcel.TheFineIsTold("A ROUND").Told,
            UnlistedParcel.TheReadGoesOnAfterIt(refused).Told,
            CarriedObject.Card(parcel, "luna")!.Value.Label,
            CarriedObject.Card(parcel, "luna")!.Value.Story,
        };

        // …and the whole of what IllegalHeat can say, because this feature adds a crossing to that ladder
        // and the meter's own three sentences are as player-facing as anything here.
        said.AddRange(IllegalHeat.EveryLine());

        Assert.True(said.Count > 10, "this sweep is too small to mean anything.");
        foreach (string line in said)
        {
            foreach (string reserved in new[] { "cover", "layer", "onion" })
            {
                Assert.DoesNotContain(reserved, line, StringComparison.OrdinalIgnoreCase);
            }

            // §8's own reserved word, swept here for the reason every feature sweeps it.
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
        }

        // The book is untouched by a sweep — nothing above may write.
        Assert.Empty(book.Entries);
    }

    /// <summary>
    /// <b>THE FOLDER SURVIVES THE VAULT, AND AN OLD SAVE LOADS WITH EVERY DRAWER EMPTY.</b> A folder that
    /// evaporated when the tab was closed would quietly re-open every answer the captain paid a fine for.
    ///
    /// <para><b>RED</b> by dropping <c>FolderClosedAtRung</c> from either half of <c>VaultMapper</c>: the
    /// band comes back as 0 and a captain who was filed at a hot site is re-opened by his own standing —
    /// <i>Assert.True() Failure — a reload re-opened a closed folder</i>.</para>
    /// </summary>
    [Fact]
    public void TheFolderSurvivesTheVault()
    {
        var book = new ContactLedger();
        string here = "luna";

        // Fine him at a site his meter is already hot at, so the filed BAND is not zero and a mapper that
        // dropped it would be caught rather than agreeing by accident.
        for (int i = 0; i < IllegalHeat.HeatPerRung * 2; i++)
        {
            IllegalHeat.Bank(book, IllegalHeat.Charge(here, IllegalHeat.Crossing.RefusedCardAtAGate), 10.0);
        }
        UnlistedParcel.TheParcelIsWhatIsFound(book, here, 5UL, 20.0, 2UL);

        int filedAt = book.For(IllegalHeat.LedgerId(SiteOperator.Of(here).Id)).FolderClosedAtRung;
        Assert.True(filedAt > 0, "the bench filed at band zero — this proves nothing about the band.");
        Assert.True(UnlistedParcel.TheFolderIsClosed(book, here));

        var saved = new Vault
        {
            Version = Vault.CurrentVersion,
            Contacts = VaultMapper.ToSection(book),
        };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));

        var back = new ContactLedger();
        VaultMapper.Apply(loaded.Contacts, back);

        Assert.True(
            UnlistedParcel.TheFolderIsClosed(back, here), "a reload re-opened a closed folder.");
        Assert.Equal(filedAt, back.For(IllegalHeat.LedgerId(SiteOperator.Of(here).Id)).FolderClosedAtRung);
        Assert.Equal(IllegalHeat.HeatAtSite(book, here), IllegalHeat.HeatAtSite(back, here));

        // An OLD save — a contacts section with no folder in it at all — loads with every drawer empty.
        var old = new ContactLedger();
        old.ApplyHeat(IllegalHeat.LedgerId(SiteOperator.Of(here).Id), "SOMEBODY", 3, 1.0);
        var oldBack = new ContactLedger();
        VaultMapper.Apply(VaultMapper.ToSection(old), oldBack);
        Assert.False(UnlistedParcel.TheFolderIsClosed(oldBack, here));

        // …and the parcel itself round-trips the satchel's own storage, like every other carried thing.
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("phobos", 12);
        Assert.True(Satchel.Item.TryParse(parcel.Stored, out Satchel.Item read));
        Assert.Equal(parcel, read);
        Assert.True(UnlistedParcel.IsAParcel(read));
    }

    // ── the bench's plumbing ────────────────────────────────────────────────────────────────────────────

    private static string WithoutComments(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, @"//[^\n]*", " ");
    }

    private static string SourceOf(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        Assert.True(dir is not null, $"no repo root above {AppContext.BaseDirectory}");

        string path = Path.Combine([dir!.FullName, .. parts]);
        Assert.True(File.Exists(path), $"{path} is gone — this guard is watching a file that moved.");
        return File.ReadAllText(path);
    }
}
