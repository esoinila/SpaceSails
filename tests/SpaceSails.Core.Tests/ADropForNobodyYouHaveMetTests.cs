using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #711 slice 2 / #319 / #794 · <b>WHAT THE PARCEL IS FOR, HELD TO ITS OWN LAWS.</b>
///
/// <para>Owner, #794: <i>"they would probably like to use a dead drop"</i> — the counterparty never shows a
/// face, the goods are physical, and the drop is the delivery rail. Seven laws:</para>
///
/// <list type="number">
/// <item>a destination is REAL and LANDABLE for every parcel the generator can mint, and it never moves;</item>
/// <item>the right ground is the delivery and one site over is not;</item>
/// <item>the money is one fine's worth at the cold band, through the fine's own function;</item>
/// <item>it is paid EXACTLY ONCE, because the hole is the record and the caller lifts it;</item>
/// <item>…and it survives the vault between the shovel and the desk, because nothing new is saved;</item>
/// <item>a confiscation silences the desk for a bounded, deterministic, absent-not-refused while;</item>
/// <item>four sentences, verbatim, and no word of them names the pattern.</item>
/// </list>
/// </summary>
public sealed class ADropForNobodyYouHaveMetTests
{
    /// <summary>The scenario's own ten moons plus a wide net of generated ids — the discipline slice 1's own
    /// bench keeps: the shipped ten prove the game people play, the generated ninety prove the GENERATOR,
    /// and a law that only holds on the shipped list is not a law. (That the shipped ten really ARE
    /// <c>sol.json</c>'s landable bodies is proved against the file itself in the Client suite, which is
    /// where the scenario is loaded.)</summary>
    private static IReadOnlyList<string> TheShippedMoons =>
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IReadOnlyList<string> ManyMoons()
    {
        var all = new List<string>(TheShippedMoons);
        for (int i = 0; i < 90; i++)
        {
            all.Add($"generated-moon-{i}");
        }
        return all;
    }

    /// <summary>A hundred parcels the DESK could really hand over — the same mint the row uses, across many
    /// havens and many windows, so a law asked here is asked of the objects the game makes.</summary>
    private static IEnumerable<Satchel.Item> ManyParcels()
    {
        string[] havens = ["the-tilt", "the-space-bar", "selene-gate", "cinder-roost", "the-rusty-roadstead"];
        foreach (string haven in havens)
        {
            for (long watch = 0; watch < 20; watch++)
            {
                yield return UnlistedParcel.FromTheDesk(haven, watch);
            }
        }
    }

    /// <summary>A chest with a parcel in it, on the ground named, buried at <paramref name="at"/>.</summary>
    private static TreasureCache AHoleWith(
        Satchel.Item parcel, string bodyId, int? siteIndex, double at, bool playerOwned = true) =>
        new(
            Id: $"cache:{bodyId}:{siteIndex}",
            BodyId: bodyId,
            LandmarkName: "the monolith",
            Bearing: "anti-spinward",
            Paces: 40,
            Coin: 0,
            Cargo: [],
            BuriedSimTime: at,
            Owner: "you",
            PlayerOwned: playerOwned,
            SiteIndex: siteIndex,
            Buried: true,
            Deposit: [parcel]);

    // ── (1) THE GROUND IS REAL, AND IT DOES NOT MOVE ────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY DESTINATION IS A PLACE THAT EXISTS, ON A BOARD THAT HAS IT.</b> A hundred parcels against a
    /// hundred moons: the body handed back is one out of the pool that went in — never an invention, never a
    /// name composed here — and the site is inside that body's own seeded board, so
    /// <see cref="LandingSites.At"/> never has to clamp it and the ground the job names is ground a shuttle
    /// can be set down on.
    ///
    /// <para><b>RED</b> by rolling the site against a constant (<c>LandingSites.MaxSites</c>) instead of
    /// against the body's own <c>Count</c>: <i>a destination names a site the body does not have</i> — a
    /// third of the shipped moons offer two grounds and the job would point at a fourth.</para>
    ///
    /// <para><b>RED</b> by having the generator compose an id of its own
    /// (<c>$"{bodyId}-drop"</c>): <i>a destination names a body that is not in the sky</i>.</para>
    /// </summary>
    [Fact]
    public void EveryDestinationIsRealAndLandable()
    {
        IReadOnlyList<string> pool = ManyMoons();
        var inPool = new HashSet<string>(pool, StringComparer.Ordinal);

        int seen = 0;
        var bodiesNamed = new HashSet<string>(StringComparer.Ordinal);
        foreach (Satchel.Item parcel in ManyParcels())
        {
            ParcelDrop.Destination where = ParcelDrop.For(parcel, pool)
                ?? throw new Xunit.Sdk.XunitException("a real parcel against a real sky produced no ground.");

            Assert.True(inPool.Contains(where.BodyId),
                $"a destination names a body that is not in the sky: {where.BodyId}");

            int board = LandingSites.Count(where.BodyId);
            Assert.True(where.SiteIndex >= 0 && where.SiteIndex < board,
                $"a destination names a site the body does not have: {where.BodyId} site {where.SiteIndex} "
                + $"of {board}");

            // The NAME is the board's own, never a second spelling.
            Assert.Equal(LandingSites.At(where.BodyId, where.SiteIndex).Name, where.SiteName);

            bodiesNamed.Add(where.BodyId);
            seen++;
        }

        Assert.True(seen == 100, $"this sweep is {seen} parcels wide — it is too small to mean anything.");

        // THE WORLD CAN TELL PASS FROM FAIL: the generator really does spread across the sky rather than
        // answering one moon for everything, which is the case a body-membership assert would pass on.
        Assert.True(bodiesNamed.Count > 10,
            $"the generator only ever named {bodiesNamed.Count} bodies — this bench could not see a "
            + "constant answer.");

        // …and a sky with nothing landable in it is the one honest null.
        Assert.Null(ParcelDrop.For(UnlistedParcel.FromTheDesk("the-tilt", 1), []));
        Assert.Null(ParcelDrop.For(UnlistedParcel.FromTheDesk("the-tilt", 1), null));
    }

    /// <summary>
    /// <b>THE SAME PARCEL IS GOING TO THE SAME PLACE FOREVER</b> — the whole of what makes the desk row
    /// honest. Asked twice, asked with the pool in a different order, and asked of a fresh
    /// <see cref="Satchel.Item"/> parsed back out of its saved string, which is the parcel as a reload hands
    /// it over.
    ///
    /// <para><b>RED</b> by dropping <c>pool.Sort</c>: <i>the ground moved when the sky was listed in another
    /// order</i> — the roll is an index into a list, and a list is only a set once it is sorted.</para>
    ///
    /// <para><b>What this guard deliberately does NOT claim.</b> An index roll over a pool moves when the
    /// POOL's membership moves, and there is no seeding trick that does not. That is why the client takes
    /// the pool from the scenario FILE rather than from the built sky, and why the four cheats that append a
    /// landable rock cannot reach it; the claim here is the one that is true — order does not matter, and
    /// the id is the whole of the draw.</para>
    /// </summary>
    [Fact]
    public void TheGroundNeverMoves()
    {
        IReadOnlyList<string> pool = ManyMoons();
        var shuffled = new List<string>(pool);
        shuffled.Reverse();

        foreach (Satchel.Item parcel in ManyParcels())
        {
            ParcelDrop.Destination once = ParcelDrop.For(parcel, pool)!.Value;
            Assert.Equal(once, ParcelDrop.For(parcel, pool)!.Value);
            Assert.Equal(once, ParcelDrop.For(parcel, shuffled)!.Value);

            Assert.True(Satchel.Item.TryParse(parcel.Stored, out Satchel.Item reloaded));
            Assert.Equal(once, ParcelDrop.For(reloaded, pool)!.Value);
        }

        // A thing that is not a parcel is going nowhere, however it is asked.
        Assert.Null(ParcelDrop.For(new Satchel.Item(Satchel.Kind.Paper, "paper:1"), pool));
    }

    // ── (2) THE RIGHT GROUND IS THE DELIVERY ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE GROUND IN THE WHOLE SKY IS THE DELIVERY, AND EVERY OTHER HOLE IS A HOLE.</b> Driven over every
    /// parcel: the named body and site deliver; the SAME body one site over does not; another body does not;
    /// a body-wide (legacy) chest with no site does not; and somebody else's chest does not.
    ///
    /// <para>The wrong-site arm is the one that matters and it is the one #650 had to fix for the ✗ itself:
    /// a body offers two to four grounds, and a delivery that answered by body alone would pay out from
    /// ground the captain was never told about.</para>
    ///
    /// <para><b>RED</b> by dropping the site from <c>Destination.IsTheGround</c> (body only): <i>a hole one
    /// site over was taken as the delivery</i>. <b>RED</b> by dropping the <c>PlayerOwned</c> clause from
    /// <c>IsTheDelivery</c>: <i>a rumour chest paid out</i>.</para>
    /// </summary>
    [Fact]
    public void TheRightGroundDeliversAndNoOtherDoes()
    {
        IReadOnlyList<string> pool = ManyMoons();
        int wrongSitesTried = 0;

        foreach (Satchel.Item parcel in ManyParcels())
        {
            ParcelDrop.Destination where = ParcelDrop.For(parcel, pool)!.Value;

            Assert.True(ParcelDrop.IsTheDelivery(AHoleWith(parcel, where.BodyId, where.SiteIndex, 100.0), pool));
            Assert.True(ParcelDrop.IsTheRightGround(parcel, pool, where.BodyId, where.SiteIndex));

            // The same moon, a different ground of its own.
            int board = LandingSites.Count(where.BodyId);
            for (int site = 0; site < board; site++)
            {
                if (site == where.SiteIndex)
                {
                    continue;
                }
                wrongSitesTried++;
                Assert.False(
                    ParcelDrop.IsTheDelivery(AHoleWith(parcel, where.BodyId, site, 100.0), pool),
                    $"a hole one site over was taken as the delivery: {where.BodyId} site {site}");
                Assert.False(ParcelDrop.IsTheRightGround(parcel, pool, where.BodyId, site));
            }

            // Another moon entirely; a body-wide chest; and somebody else's chest on the right ground.
            string elsewhere = pool.First(b => !string.Equals(b, where.BodyId, StringComparison.Ordinal));
            Assert.False(ParcelDrop.IsTheDelivery(AHoleWith(parcel, elsewhere, where.SiteIndex, 100.0), pool));
            Assert.False(ParcelDrop.IsTheDelivery(AHoleWith(parcel, where.BodyId, null, 100.0), pool));
            Assert.False(ParcelDrop.IsTheDelivery(
                AHoleWith(parcel, where.BodyId, where.SiteIndex, 100.0, playerOwned: false), pool));
        }

        Assert.True(wrongSitesTried > 100,
            $"only {wrongSitesTried} wrong grounds were tried — the world could not tell pass from fail.");

        // …and a hole with no parcel in it is not a delivery however right the ground is.
        ParcelDrop.Destination any = ParcelDrop.For(ManyParcels().First(), pool)!.Value;
        var coinOnly = AHoleWith(ManyParcels().First(), any.BodyId, any.SiteIndex, 1.0) with { Deposit = [] };
        Assert.False(ParcelDrop.IsTheDelivery(coinOnly, pool));
    }

    // ── (3) WHAT IT PAYS ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE FINE'S WORTH, AT THE COLD BAND, THROUGH THE FINE'S OWN FUNCTION.</b> No constant of this
    /// feature's own is in the sum: the number is <see cref="UnlistedParcel.TheFine"/> — which is
    /// <see cref="BustedRule.BribeDemand"/> called — read at <see cref="ParcelDrop.TheColdBand"/>, so
    /// retuning the bribe retunes the job and nothing has to remember to follow.
    ///
    /// <para>And it does NOT move with the captain's heat, which is the faceless clause made mechanical: a
    /// party who has never seen your face cannot read your file. The bench proves the world can tell — the
    /// same seed at a hot band really is a different number.</para>
    ///
    /// <para><b>RED</b> by quoting the hot band instead (<c>TheFine(3, …)</c>): <i>the payment moved with a
    /// meter the payer cannot read</i>. <b>RED</b> by scaling it by a fraction of its own
    /// (<c>… / 2</c>): <i>the payment is not the fine's own number</i>.</para>
    /// </summary>
    [Fact]
    public void ThePaymentIsOneFineAtTheColdBandAndNothingElse()
    {
        int spread = 0;
        int previous = -1;

        foreach (Satchel.Item parcel in ManyParcels())
        {
            ulong seed = DiceRule.Seed($"{ParcelDrop.PaymentTag}:{parcel.Id}");
            int paid = ParcelDrop.ThePayment(parcel.Id);

            Assert.Equal(UnlistedParcel.TheFine(ParcelDrop.TheColdBand, seed), paid);
            Assert.Equal(BustedRule.BribeDemand(ParcelDrop.TheColdBand, seed).Total, paid);
            Assert.True(paid > 0, "a job that pays nothing is not a job.");

            // The payer cannot read a meter: the hot band really is a different number, and this is not it.
            Assert.NotEqual(UnlistedParcel.TheFine(3, seed), paid);

            if (previous >= 0 && previous != paid)
            {
                spread++;
            }
            previous = paid;
        }

        Assert.True(spread > 10,
            $"the payment only moved {spread} times across a hundred parcels — this bench could not tell a "
            + "seeded amount from a constant.");
    }

    /// <summary>
    /// <b>THE LAG IS BOUNDED, SEEDED, AND MEASURED IN THE GAME'S OWN WATCH.</b> Two to four watches after
    /// the shovel, off <see cref="PatronRota.WatchSeconds"/> and no unit of this feature's own — and the
    /// same parcel always waits the same length, so a reload cannot re-roll the wait shorter.
    ///
    /// <para><b>RED</b> by measuring the lag off <i>now</i> rather than off the chest's burial stamp: the
    /// due moment stops being a fact about the hole and a captain who buried it a week ago is told to wait
    /// again.</para>
    /// </summary>
    [Fact]
    public void TheLagIsBoundedAndSeededOffTheParcel()
    {
        var lengths = new HashSet<int>();
        foreach (Satchel.Item parcel in ManyParcels())
        {
            int watches = ParcelDrop.WatchesBeforeItLands(parcel.Id);
            Assert.InRange(watches, ParcelDrop.FewestWatchesBeforeItLands, ParcelDrop.MostWatchesBeforeItLands);
            Assert.Equal(watches, ParcelDrop.WatchesBeforeItLands(parcel.Id));

            Assert.Equal(1000.0 + (watches * PatronRota.WatchSeconds), ParcelDrop.PayableAt(1000.0, parcel.Id));
            lengths.Add(watches);
        }

        Assert.True(lengths.Count > 1, "every parcel waited the same length — this is a constant, not a lag.");
    }

    // ── (4) PAID EXACTLY ONCE, AND ONLY WHEN IT IS DUE ──────────────────────────────────────────────────

    /// <summary>
    /// <b>THE HOLE IS THE RECORD, SO THE MONEY CANNOT COME TWICE.</b> The whole read, driven as the desk
    /// drives it: nothing before the due moment, the payment ON it, and — once the caller has lifted the
    /// chest, which is the one thing the desk does — nothing ever again.
    ///
    /// <para>This is the guard that would catch a pending-payment flag: a flag can be read twice, a hole
    /// cannot be dug twice.</para>
    ///
    /// <para><b>RED</b> by having the client credit the purse without lifting the chest: <i>the desk paid
    /// the same drop twice</i>. <b>RED</b> by dropping the <c>nowSimTime &lt; due</c> clause: <i>the money
    /// was on the desk before anybody had dug</i>.</para>
    /// </summary>
    [Fact]
    public void ThePaymentLandsOnceAndOnlyWhenItIsDue()
    {
        IReadOnlyList<string> pool = ManyMoons();
        int walked = 0;

        foreach (Satchel.Item parcel in ManyParcels())
        {
            ParcelDrop.Destination where = ParcelDrop.For(parcel, pool)!.Value;
            double buried = 4_000.0;
            double due = ParcelDrop.PayableAt(buried, parcel.Id);

            var ledger = new CacheLedger();
            ledger.Load(AHoleWith(parcel, where.BodyId, where.SiteIndex, buried));

            // Not yet — at the burial, and one second short of the moment.
            Assert.Null(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, buried));
            Assert.Null(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, due - 1.0));

            ParcelDrop.Payment paid = ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, due)
                ?? throw new Xunit.Sdk.XunitException("the money never came due.");
            Assert.Equal(ParcelDrop.ThePayment(parcel.Id), paid.Amount);
            Assert.Equal(parcel.Id, paid.ParcelId);
            Assert.Equal(where, paid.Where);

            // …and the desk lifts the chest. Somebody dug: there is nothing left to pay for.
            Assert.NotNull(ledger.Remove(paid.CacheId));
            Assert.Null(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, due));
            Assert.Null(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, due + (10 * PatronRota.WatchSeconds)));
            walked++;
        }

        Assert.True(walked == 100, $"this sweep is {walked} drops wide — too small to mean anything.");
    }

    /// <summary>
    /// <b>ONE DESK VISIT SETTLES ONE DROP, AND THE HOARD IT SEARCHES IS A REAL HOARD.</b> Two deliveries
    /// that came due while the captain was away are two payments on two visits, each with its own sentence —
    /// a loop that paid them both would put one line on the screen for two events.
    ///
    /// <para>An ordinary coin chest sits FIRST in the ledger, because a hoard is mostly not deliveries: a
    /// filter written as a <c>break</c> rather than a <c>continue</c> stops at it and finds nothing, which
    /// is the bug this arrangement exists to catch.</para>
    ///
    /// <para><b>RED</b> by writing the scan's skip as <c>break</c>: <i>the money was never on the desk at
    /// all</i>.</para>
    /// </summary>
    [Fact]
    public void TwoDropsAreTwoVisits()
    {
        IReadOnlyList<string> pool = ManyMoons();
        Satchel.Item first = UnlistedParcel.FromTheDesk("the-tilt", 1);
        Satchel.Item second = UnlistedParcel.FromTheDesk("the-tilt", 2);
        ParcelDrop.Destination a = ParcelDrop.For(first, pool)!.Value;
        ParcelDrop.Destination b = ParcelDrop.For(second, pool)!.Value;

        var ledger = new CacheLedger();
        ledger.Load(
            AHoleWith(first, a.BodyId, a.SiteIndex, 0.0) with { Id = "cache:coin", Coin = 900, Deposit = [] });
        ledger.Load(AHoleWith(first, a.BodyId, a.SiteIndex, 0.0) with { Id = "cache:a" });
        ledger.Load(AHoleWith(second, b.BodyId, b.SiteIndex, 0.0) with { Id = "cache:b" });

        double late = 100 * PatronRota.WatchSeconds;
        ParcelDrop.Payment one = ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, late)!.Value;
        ledger.Remove(one.CacheId);
        ParcelDrop.Payment two = ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, late)!.Value;
        ledger.Remove(two.CacheId);

        Assert.NotEqual(one.CacheId, two.CacheId);
        Assert.Null(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, late));
    }

    // ── (5) AND IT SURVIVES THE FILE ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOTHING NEW IS SAVED, SO NOTHING NEW CAN BE LOST.</b> The pending payment IS the chest: bury,
    /// save, load, and the money is still coming. Driven through the real
    /// <see cref="VaultSerializer"/> round trip rather than through the mapper alone, because the thing that
    /// would actually break this is the deposit not surviving the JSON.
    ///
    /// <para><b>RED</b> by dropping <c>Deposit</c> from either half of <c>VaultMapper</c>'s cache record:
    /// <i>a reload lost the box that was in the hole</i> — and the chest comes back as an ordinary empty
    /// hole nobody is coming for.</para>
    /// </summary>
    [Fact]
    public void ThePendingPaymentSurvivesTheVault()
    {
        IReadOnlyList<string> pool = ManyMoons();
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("selene-gate", 7);
        ParcelDrop.Destination where = ParcelDrop.For(parcel, pool)!.Value;
        double buried = 12_345.0;
        double due = ParcelDrop.PayableAt(buried, parcel.Id);

        var ledger = new CacheLedger();
        ledger.Load(AHoleWith(parcel, where.BodyId, where.SiteIndex, buried));
        Assert.NotNull(ParcelDrop.ThePaymentThatIsThere(ledger.Caches, pool, due));

        var saved = new Vault { Version = Vault.CurrentVersion, Caches = VaultMapper.ToSection(ledger) };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));

        var back = new CacheLedger();
        VaultMapper.Apply(loaded.Caches, back);

        ParcelDrop.Payment paid = ParcelDrop.ThePaymentThatIsThere(back.Caches, pool, due)
            ?? throw new Xunit.Sdk.XunitException("a reload lost the box that was in the hole.");
        Assert.Equal(ParcelDrop.ThePayment(parcel.Id), paid.Amount);
        Assert.Equal(where, paid.Where);

        // …and it was still not due a second early on the other side of the file.
        Assert.Null(ParcelDrop.ThePaymentThatIsThere(back.Caches, pool, due - 1.0));
    }

    // ── (6) AND WHEN A MAN WITH A FORM TOOK IT ──────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE DESK GOES QUIET FOR A BOUNDED, SEEDED, CONTIGUOUS WHILE — AND THEN IT DOES NOT.</b> The
    /// strike-off is one tag per silenced watch, which is what lets it live in the durable SET of strings
    /// the fence's own one-key-per-window already rides; it is bounded by
    /// <see cref="ParcelDrop.MostQuietWatches"/>, so no confiscation can write an unbounded save.
    ///
    /// <para><b>RED</b> by writing only the FIRST watch: <i>the desk was open again four hours after the
    /// box was carried off</i>. <b>RED</b> by seeding the length off the sim clock instead of the parcel:
    /// <i>the same confiscation silenced a different number of watches</i>.</para>
    /// </summary>
    [Fact]
    public void AConfiscationSilencesTheDeskForAWhileAndThenStops()
    {
        var lengths = new HashSet<int>();

        foreach (Satchel.Item parcel in ManyParcels())
        {
            int watches = ParcelDrop.QuietWatches(parcel.Id);
            Assert.InRange(watches, ParcelDrop.FewestQuietWatches, ParcelDrop.MostQuietWatches);
            Assert.Equal(watches, ParcelDrop.QuietWatches(parcel.Id));
            lengths.Add(watches);

            // Taken off him in the middle of a watch, not on its boundary — the case a floor() gets wrong.
            double when = (9 * PatronRota.WatchSeconds) + 137.0;
            long from = PatronRota.WatchIndex(when);

            var struckOff = new HashSet<string>(
                ParcelDrop.TheWatchesWithNothingOnThem(parcel.Id, when), StringComparer.Ordinal);

            Assert.Equal(watches, struckOff.Count);
            for (int i = 0; i < watches; i++)
            {
                Assert.Contains(ParcelDrop.NothingForThisHullOn(from + i), struckOff);
            }

            // The watch before it happened, and the one after it runs out, are ordinary afternoons.
            Assert.DoesNotContain(ParcelDrop.NothingForThisHullOn(from - 1), struckOff);
            Assert.DoesNotContain(ParcelDrop.NothingForThisHullOn(from + watches), struckOff);
        }

        Assert.True(lengths.Count > 1, "every confiscation silenced the same number of watches.");
    }

    // ── (7) WHAT IT SAYS ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>FOUR SENTENCES, VERBATIM, AND THERE IS NO FIFTH.</b> Letter for letter, all four in
    /// <see cref="ParcelDrop.AllProse"/>, and then the slice's OWN FILES are read for string literals —
    /// because reflection catches a new <c>const</c> and does not catch a sentence typed into a method,
    /// which is how prose actually gets into this codebase.
    ///
    /// <para><b>RED</b> by pulsing a line of my own from <c>TheDropIsMade</c>: <i>this slice's files carry a
    /// sentence nobody authored</i>.</para>
    /// </summary>
    [Fact]
    public void TheAuthoredStringsAreVerbatimAndThereIsNoFifth()
    {
        Assert.Equal(
            "No name. A moon, a bearing, a depth. Put it in the ground and leave.",
            ParcelDrop.TheInstruction);
        Assert.Equal(
            "In the ground, where somebody who has never seen your face will know to dig.",
            ParcelDrop.DeliveredLine);
        Assert.Equal("A payment with no sender. Somebody dug.", ParcelDrop.PaymentLine);
        Assert.Equal(
            "a parcel, put in the ground at {0} for nobody you have met", ParcelDrop.FieldBookEntry);
        Assert.Equal(
            "a parcel, put in the ground at Phobos · The Ridge Camp for nobody you have met",
            ParcelDrop.TheFieldBookEntry("Phobos · The Ridge Camp"));

        var prose = ParcelDrop.AllProse().ToList();
        Assert.Equal(4, prose.Count);
        Assert.Contains(ParcelDrop.TheInstruction, prose);
        Assert.Contains(ParcelDrop.DeliveredLine, prose);
        Assert.Contains(ParcelDrop.PaymentLine, prose);
        Assert.Contains(ParcelDrop.FieldBookEntry, prose);

        var authored = new HashSet<string>(prose, StringComparer.Ordinal);
        var sentences = new List<string>();
        foreach (string file in new[]
        {
            SourceOf("src", "SpaceSails.Core", "ParcelDrop.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"),
        })
        {
            foreach (Match m in Regex.Matches(WithoutComments(file), "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                string text = m.Value.Trim('"');
                if (text.Contains(' ', StringComparison.Ordinal)
                    && text.EndsWith('.')
                    && !authored.Contains(text))
                {
                    sentences.Add(m.Value);
                }
            }
        }

        Assert.True(sentences.Count == 0,
            "this slice's files carry a sentence nobody authored: " + string.Join(", ", sentences)
            + " — a beat that wants a line gets a // FABLE: marker rather than one typed by this crew.");
    }

    /// <summary>
    /// <b>THE RESERVED WORDS ARE NOWHERE NEAR IT</b> — slice 1's law, unchanged: <i>"No card names the
    /// pattern."</i> Every sentence this slice can put on a screen, composed the way the game composes them,
    /// with the field-book entry driven over every shipped ground so a site name cannot smuggle one in.
    ///
    /// <para><b>RED</b> by writing the word into any one of them.</para>
    /// </summary>
    [Fact]
    public void NoSentenceNamesThePattern()
    {
        var said = new List<string>(ParcelDrop.AllProse());
        foreach (string body in TheShippedMoons)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                said.Add(ParcelDrop.TheFieldBookEntry(FieldNotes.PlaceLabel(body, site.Name)));
                said.Add(ParcelDrop.DestinationRow(body, site.Name));
            }
        }

        Assert.True(said.Count > 20, "this sweep is too small to mean anything.");
        foreach (string line in said)
        {
            foreach (string reserved in new[] { "cover", "layer", "onion" })
            {
                Assert.DoesNotContain(reserved, line, StringComparison.OrdinalIgnoreCase);
            }
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
        }
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
