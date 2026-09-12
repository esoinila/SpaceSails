using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #535 slice 2 · <b>THE KEY'S OTHER TWO SOURCES — the laws, in Core.</b>
///
/// <para>Slice 1 dealt the key one way and proved that one way. Four more things have to be true or these are
/// not the sources the issue describes: the favour is offered ONLY in the top goodwill band and ONLY to
/// somebody the book marks no lie against, and only once in a captain's life; taking it costs exactly the
/// band that qualified it; the fence's price is three times the BUSTED card's bribe <b>through that card's
/// own function</b>, so a change to the bribe moves both; and the two sources together deal at most ONE key
/// per port per window.</para>
///
/// <para><b>Written so the world can tell pass from fail</b> (this repository's fifth named bug class). The
/// band is asked of a swept range of goodwill values either side of it rather than of the one value that
/// qualifies, so a gate moved by one reports which values changed hands. The price is asserted by CALLING
/// <see cref="BustedRule.BribeDemand"/> over several heats and seeds, so a restated formula that happened to
/// agree today would still be red the day the bribe is retuned. And the joint cap is driven through a real
/// register that both sources write into, not through two booleans a test set.</para>
/// </summary>
public sealed class TheKeyHasOtherSourcesTests
{
    private const string Giver = "madam-coil";

    /// <summary>A book with one contact standing at <paramref name="goodwill"/>, optionally lied to.</summary>
    private static ContactLedger ABookWhere(int goodwill, bool lied = false, bool favourSpent = false)
    {
        var ledger = new ContactLedger();
        ContactHistory h = ContactHistory.New(Giver, "Madam Coil").WithGoodwill(goodwill);
        if (lied)
        {
            h = h.WithLie();
        }

        if (favourSpent)
        {
            h = h.WithFavourSpent();
        }

        ledger.Load(h);
        return ledger;
    }

    // ── THE FAVOUR · WHO IS OFFERED IT ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FAVOUR IS OFFERED IN THE TOP BAND AND NOWHERE BELOW IT.
    ///
    /// <para>Swept across every goodwill from nothing to twice the band, so a gate moved by one does not slip
    /// through: the guard reports the exact set of standings that were offered a key and the exact set that
    /// were not. With <c>&gt;=</c> loosened to <c>&gt;</c>, or the band swapped for
    /// <see cref="ContactDrink.TrustForBusiness"/>, the two sets differ by a named value and this is red.</para>
    /// </summary>
    [Fact]
    public void TheFavourIsOfferedOnlyFromTheTopGoodwillBandUp()
    {
        int band = BlackOpsKey.TopGoodwillBand;
        int[] offered =
        [
            .. Enumerable.Range(0, (band * 2) + 1)
                .Where(g => BlackOpsKey.FavourIsOnTheTable(ABookWhere(g).For(Giver))),
        ];

        Assert.Equal(Enumerable.Range(band, band + 1), offered);

        // …and the band really is the TOP one the ledger's goodwill is read against, not a middling one
        // wearing the name. A contact deep enough to trade with is not deep enough to spend their standing.
        Assert.True(band > ContactDrink.TrustForBusiness,
            $"the 'top' goodwill band ({band}) is at or below the threshold at which a contact will merely do "
            + $"BUSINESS with you ({ContactDrink.TrustForBusiness}). Trading with somebody is not the same as "
            + "spending their own standing on them, and if the two are the same number the favour is not a "
            + "favour, it is a purchase.");
        Assert.False(BlackOpsKey.FavourIsOnTheTable(ABookWhere(ContactDrink.TrustForBusiness).For(Giver)));
        Assert.False(BlackOpsKey.FavourIsOnTheTable(ABookWhere(ContactDrink.WarmThreshold).For(Giver)));
    }

    /// <summary>
    /// AND NEVER TO SOMEBODY THE BOOK MARKS A LIE AGAINST — which is the canon line's own gate, stated out
    /// loud: <i>"You never lied to me."</i>
    ///
    /// <para>Asked at every standing from the band up, so a gate that only checked the lie at exactly the
    /// threshold, or dropped it, is red at several values and says so.</para>
    /// </summary>
    [Fact]
    public void TheFavourIsNeverOfferedBySomebodyWhoWasLiedTo()
    {
        for (int g = BlackOpsKey.TopGoodwillBand; g <= BlackOpsKey.TopGoodwillBand * 3; g++)
        {
            Assert.True(BlackOpsKey.FavourIsOnTheTable(ABookWhere(g).For(Giver)),
                $"at goodwill {g} an honest contact was NOT offered the favour — the world this guard asks "
                + "its question of cannot tell the lie apart from the band.");
            Assert.False(BlackOpsKey.FavourIsOnTheTable(ABookWhere(g, lied: true).For(Giver)),
                $"at goodwill {g} a contact the book marks a lie against still offered the favour. The canon "
                + "line IS the gate: \"You never lied to me.\"");
        }
    }

    /// <summary>ONCE PER CONTACT PER CAPTAIN-LIFETIME. Proved through the book's own write rather than the
    /// flag set by hand: the favour is taken, and the same person at the same standing is not offered a
    /// second one even after the standing has been bought back up past the band.</summary>
    [Fact]
    public void OneFavourPerContactEvenAfterTheStandingIsBoughtBack()
    {
        ContactLedger ledger = ABookWhere(BlackOpsKey.TopGoodwillBand * 3);

        Assert.NotNull(ledger.RecordFavourGiven(Giver, "Madam Coil"));
        Assert.True(ledger.For(Giver).FavourSpent);
        Assert.Null(ledger.RecordFavourGiven(Giver, "Madam Coil"));

        // Buy them all the way back — drinks are cheap and the band is not a wall.
        ledger.AddGoodwill(Giver, "Madam Coil", BlackOpsKey.TopGoodwillBand * 3);
        Assert.True(ledger.For(Giver).Goodwill >= BlackOpsKey.TopGoodwillBand,
            "this guard bought the contact back to below the band, so it would be green against a rule that "
            + "only ever looked at goodwill.");
        Assert.False(BlackOpsKey.FavourIsOnTheTable(ledger.For(Giver)));
        Assert.Null(ledger.RecordFavourGiven(Giver, "Madam Coil"));
    }

    /// <summary>TAKING IT COSTS EXACTLY THE BAND THAT QUALIFIED IT — measured at three standings, so a debit
    /// that clamped at zero, or took a fixed one, or took the whole lot, is red with the number it took.</summary>
    [Fact]
    public void TheFavourCostsExactlyTheBandThatQualifiedIt()
    {
        foreach (int start in new[]
                 {
                     BlackOpsKey.TopGoodwillBand,
                     BlackOpsKey.TopGoodwillBand + 1,
                     BlackOpsKey.TopGoodwillBand * 4,
                 })
        {
            ContactLedger ledger = ABookWhere(start);
            ContactHistory? after = ledger.RecordFavourGiven(Giver, "Madam Coil");

            Assert.NotNull(after);
            Assert.Equal(start - BlackOpsKey.FavourCostsGoodwill, after!.Value.Goodwill);
        }

        // The price IS the band, in both directions — retune one and the other follows rather than drifting.
        Assert.Equal(BlackOpsKey.TopGoodwillBand, BlackOpsKey.FavourCostsGoodwill);

        // …so a contact who was only just close enough is back at the bottom, which is what "spent" means.
        Assert.Equal(0, ABookWhere(BlackOpsKey.TopGoodwillBand)
            .RecordFavourGiven(Giver, "Madam Coil")!.Value.Goodwill);
    }

    /// <summary>DECLINING CHANGES NOTHING — and the offer survives to a later sit. There is no decline call
    /// to make, which is the point: the whole of declining is not pressing, so this guard reads the book
    /// before and after the offer was on the table and asserts it never moved.</summary>
    [Fact]
    public void DecliningTheFavourChangesNothingAndTheOfferStands()
    {
        ContactLedger ledger = ABookWhere(BlackOpsKey.TopGoodwillBand + 2);
        ContactHistory before = ledger.For(Giver);

        Assert.True(BlackOpsKey.FavourIsOnTheTable(before));
        Assert.True(BlackOpsKey.FavourIsOnTheTable(ledger.For(Giver)));      // looked at twice, taken neither

        ContactHistory after = ledger.For(Giver);
        Assert.Equal(before.Goodwill, after.Goodwill);
        Assert.False(after.FavourSpent);
        Assert.True(BlackOpsKey.FavourIsOnTheTable(after));
    }

    /// <summary>A REFUSED FAVOUR DEBITS NOTHING. The book is asked of itself inside
    /// <see cref="ContactLedger.RecordFavourGiven"/>, so a press that raced the state cannot take the
    /// standing without handing over the key.</summary>
    [Fact]
    public void ABookThatRefusesTheFavourIsNotDebited()
    {
        foreach (ContactLedger ledger in new[]
                 {
                     ABookWhere(BlackOpsKey.TopGoodwillBand - 1),
                     ABookWhere(BlackOpsKey.TopGoodwillBand * 2, lied: true),
                     ABookWhere(BlackOpsKey.TopGoodwillBand * 2, favourSpent: true),
                 })
        {
            int before = ledger.For(Giver).Goodwill;
            Assert.Null(ledger.RecordFavourGiven(Giver, "Madam Coil"));
            Assert.Equal(before, ledger.For(Giver).Goodwill);
        }
    }

    /// <summary>ONE KEY, AND IT IS THE SAME OBJECT SLICE 1 DEALS. Two contacts' favours and a fence's key
    /// and a hull's key are FOUR rows rather than one stacked four — only rounds stack, and a shared id
    /// would silently make every later key the first one.</summary>
    [Fact]
    public void EverySourceMintsTheSameObjectUnderAnIdOfItsOwn()
    {
        IReadOnlyList<Satchel.Item> pocket = [];
        pocket = Satchel.Add(pocket, BlackOpsKey.FavourFrom("madam-coil"));
        pocket = Satchel.Add(pocket, BlackOpsKey.FavourFrom("the-fixer"));
        pocket = Satchel.Add(pocket, BlackOpsKey.FromTheFence("the-tilt", 4));
        pocket = Satchel.Add(pocket, BlackOpsKey.FoundOn("lost-9"));

        Assert.Equal(4, BlackOpsKey.CountIn(pocket));
        Assert.Equal(4, pocket.Count(BlackOpsKey.IsTheKey));
        Assert.All(pocket, i => Assert.Equal(Satchel.Kind.BlackOpsKey, i.Kind));

        // …and each spends singly, through slice 1's own one call.
        foreach (Satchel.Item key in pocket.ToList())
        {
            pocket = BlackOpsKey.Spend(pocket, key);
        }

        Assert.Equal(0, BlackOpsKey.CountIn(pocket));
    }

    // ── THE FENCE · WHAT HE ASKS ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PRICE IS THREE BRIBES, AND IT IS THE BUSTED CARD'S OWN ARITHMETIC.
    ///
    /// <para>Asserted by CALLING <see cref="BustedRule.BribeDemand"/> rather than by restating what it
    /// returns, which is the whole point: retune the bribe's bands tomorrow and this guard moves with it. A
    /// price that had been copied into this feature — even one that agreed this afternoon — is red here.</para>
    ///
    /// <para>Swept over every heat rung the bribe has a band for and several ports' seeds, so a formula that
    /// happened to agree at one heat is caught.</para>
    /// </summary>
    [Fact]
    public void TheFenceAsksThreeTimesTheBribeThatCardWouldAsk()
    {
        foreach (string port in new[] { "the-tilt", "luna", "ceres-dock" })
        {
            foreach (long window in new long[] { 0, 3, 77 })
            {
                ulong seed = BlackOpsKey.FenceSeed(port, window);
                foreach (int heat in new[] { 0, 1, 2, 3, 4 })
                {
                    int bribe = BustedRule.BribeDemand(Math.Max(1, heat), seed).Total;

                    Assert.Equal(BlackOpsKey.FenceAsksThisManyBribes * bribe,
                        BlackOpsKey.FencePrice(heat, seed));
                    Assert.True(bribe > 0,
                        "the bribe this guard multiplies is zero, so three times it and one times it and "
                        + "none of it are the same number.");
                }
            }
        }

        // The multiplier is really three, and it is really a multiplier: two neighbours are both wrong.
        Assert.Equal(3, BlackOpsKey.FenceAsksThisManyBribes);
        ulong s = BlackOpsKey.FenceSeed("the-tilt", 1);
        int oneBribe = BustedRule.BribeDemand(2, s).Total;
        Assert.NotEqual(oneBribe * 2, BlackOpsKey.FencePrice(2, s));
        Assert.NotEqual(oneBribe * 4, BlackOpsKey.FencePrice(2, s));
    }

    /// <summary>A COLD CAPTAIN IS QUOTED THE BOTTOM OF THE LADDER, NEVER A FREE ONE — the repo boat's own
    /// <c>Math.Max(1, …)</c> floor, and it has to be there or heat 0 would price the key off a heat band the
    /// bribe has no arm for.</summary>
    [Fact]
    public void AColdCaptainIsQuotedTheBottomRungAndNotNothing()
    {
        ulong seed = BlackOpsKey.FenceSeed("the-tilt", 2);

        Assert.Equal(BlackOpsKey.FencePrice(1, seed), BlackOpsKey.FencePrice(0, seed));
        Assert.Equal(BlackOpsKey.FencePrice(1, seed), BlackOpsKey.FencePrice(-5, seed));
        Assert.True(BlackOpsKey.FencePrice(0, seed) > 0);

        // …and heat still moves the quote, or the floor would be the whole rule.
        Assert.True(BlackOpsKey.FencePrice(3, seed) > BlackOpsKey.FencePrice(1, seed),
            "the fence quotes a hot captain no more than a cold one, so the price is not reading the meter "
            + "the collector reads and the line about what a clean record costs is not true of anything.");
    }

    /// <summary>
    /// THE ROTATION WINDOW IS THE WORLD'S OWN WATCH, AND IT TURNS.
    ///
    /// <para>Both halves matter. Reading <see cref="Interior.PatronRota.WatchIndex"/> is what keeps
    /// <i>"fresh this cycle"</i> meaning the same cycle the rest of the world runs on; the second half is the
    /// one that can fail against a window that never turns, which would be a fence with one key forever.</para>
    /// </summary>
    [Fact]
    public void TheWindowIsTheWorldsOwnWatchAndItActuallyTurns()
    {
        Assert.Equal(Interior.PatronRota.WatchIndex(0), BlackOpsKey.FenceWindow(0));
        Assert.Equal(Interior.PatronRota.WatchIndex(500_000), BlackOpsKey.FenceWindow(500_000));

        double watch = Interior.PatronRota.WatchSeconds;
        Assert.Equal(BlackOpsKey.FenceWindow(0), BlackOpsKey.FenceWindow(watch * 0.99));
        Assert.NotEqual(BlackOpsKey.FenceWindow(0), BlackOpsKey.FenceWindow(watch * 1.01));

        // Six windows to a sim day, which is the rate a captain actually meets one at a port they sit at.
        Assert.Equal(6, BlackOpsKey.FenceWindow(24 * 3600) - BlackOpsKey.FenceWindow(0));
    }

    /// <summary>
    /// AT MOST ONE KEY PER PORT PER WINDOW, COUNTING BOTH NEW SOURCES TOGETHER.
    ///
    /// <para>Driven through a real register — one <c>HashSet</c> standing in for the captain's own
    /// <c>_roomsTurnedOver</c> — so the law is proved as the client actually keeps it: both sources write ONE
    /// tag and both read it. Two sources with two tags is exactly what this is red against.</para>
    /// </summary>
    [Fact]
    public void OnePortDealsOneKeyAWatchHoweverItReachesYou()
    {
        var register = new HashSet<string>(StringComparer.Ordinal);
        const string port = "the-tilt";
        long window = BlackOpsKey.FenceWindow(0);

        // The favour comes first…
        Assert.True(register.Add(BlackOpsKey.ThePortHasDealtOne(port, window)));

        // …and the fence, on the same ground on the same watch, is looking at a port already struck off.
        Assert.Contains(BlackOpsKey.ThePortHasDealtOne(port, window), register);

        // A different port on the same watch is a different tag, and the watch after is a third.
        Assert.DoesNotContain(BlackOpsKey.ThePortHasDealtOne("luna", window), register);
        Assert.DoesNotContain(BlackOpsKey.ThePortHasDealtOne(port, window + 1), register);

        // …and the port and the window are BOTH in the tag: a tag that dropped either would collide here.
        Assert.NotEqual(BlackOpsKey.ThePortHasDealtOne(port, window),
            BlackOpsKey.ThePortHasDealtOne("luna", window));
        Assert.NotEqual(BlackOpsKey.ThePortHasDealtOne(port, window),
            BlackOpsKey.ThePortHasDealtOne(port, window + 1));
    }

    /// <summary>The strike-off cannot be read as a Hive room by the register's own reader, the way slice 1's
    /// hull tag cannot — or a facility would quietly consider a floor emptied because a fence sold a key.</summary>
    [Fact]
    public void TheStrikeOffIsNotAHiveRoomKey()
    {
        Assert.False(KeepOrLeave.TryReadKey(
            BlackOpsKey.ThePortHasDealtOne("the-tilt", 4), "the-tilt", out _, out _));
    }

    // ── DETERMINISM ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SAME PORT ON THE SAME WATCH QUOTES THE SAME PRICE — ACROSS PROCESSES.
    ///
    /// <para>Pinned to literals rather than to a recomputation, because the bug this guard exists for is the
    /// one slice 1 actually shipped and had to take out: a seed folded through <c>string.GetHashCode</c>,
    /// which .NET randomises PER PROCESS. A guard that recomputed the seed the same way would agree with
    /// itself all afternoon and the fence would still quote a different price every time the tab was
    /// reloaded. Only a literal written down on one run and checked on the next can see it.</para>
    /// </summary>
    [Fact]
    public void TheFencesQuoteIsTheSameOnMondayAsItWasOnSunday()
    {
        Assert.Equal(742813423647609888UL, BlackOpsKey.FenceSeed("the-tilt", 4));
        Assert.Equal(1191UL, (ulong)BlackOpsKey.FencePrice(1, BlackOpsKey.FenceSeed("the-tilt", 4)));

        // …and it is stable within a run too, which is what a desk re-opened twice on one watch asks.
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(BlackOpsKey.FenceSeed("the-tilt", 4), BlackOpsKey.FenceSeed("the-tilt", 4));
            Assert.Equal(BlackOpsKey.FencePrice(2, BlackOpsKey.FenceSeed("luna", 9)),
                BlackOpsKey.FencePrice(2, BlackOpsKey.FenceSeed("luna", 9)));
        }

        // Two ports do not quote one price, or the seed is not reading the port at all.
        Assert.NotEqual(BlackOpsKey.FenceSeed("the-tilt", 4), BlackOpsKey.FenceSeed("luna", 4));
    }

    // ── THE PROSE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The four new strings are the canon, word for word, and nothing else was authored. (The
    /// no-stray-prose sweep over every string this feature publishes lives in
    /// <c>TheBlackOpsKeyTests.NothingIsAuthoredBesideTheCanon</c> and now has these four in its list.)</summary>
    [Fact]
    public void TheNewLinesAreTheCanonWordForWord()
    {
        Assert.Equal(
            "You never lied to me. That is rarer than what I'm about to hand you. Don't bring it back.",
            BlackOpsKey.FavourLine);
        Assert.Equal("Take the favour", BlackOpsKey.FavourVerb);
        Assert.Equal(
            "Fresh this cycle. Ask me what it costs and I'll ask you what a clean record costs.",
            BlackOpsKey.FenceRowLine);
        Assert.Equal("Buy the key", BlackOpsKey.FenceVerb);
    }

    /// <summary>…and the audit that reads every line this feature can put on a screen actually reaches them.
    /// <c>EveryLine</c> is what the client-side prose sweeps enumerate; a line published but left out of it
    /// is a line nothing checks.</summary>
    [Fact]
    public void EveryLineEnumeratesTheFourNewOnesAndStaysReservedWordFree()
    {
        string[] lines = [.. BlackOpsKey.EveryLine()];

        Assert.Contains(BlackOpsKey.FavourLine, lines);
        Assert.Contains(BlackOpsKey.FavourVerb, lines);
        Assert.Contains(BlackOpsKey.FenceRowLine, lines);
        Assert.Contains(BlackOpsKey.FenceVerb, lines);
        Assert.Equal(lines.Length, lines.Distinct(StringComparer.Ordinal).Count());

        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
            "issued", "issuer", "navy", "military", "service", "agency", "bureau", "nebula", "mutual",
        ];

        foreach (string line in lines)
        {
            foreach (string bad in forbidden)
            {
                Assert.False(line.Contains(bad, StringComparison.OrdinalIgnoreCase),
                    $"\"{line}\" says \"{bad}\". The key never explains who made it, and nothing in this "
                    + "feature settles what any of it was for.");
            }
        }
    }

    // ── THE BOOK REMEMBERS IT ACROSS A RELOAD ───────────────────────────────────────────────────────────

    /// <summary>THE FAVOUR SURVIVES THE VAULT. A restart that forgot it would be an unlimited supply of a
    /// consumable — the one thing the "few" law cannot survive. Round-tripped through the real mapper, with
    /// the goodwill debit beside it so a section that carried one and dropped the other is red.</summary>
    [Fact]
    public void TheSpentFavourRidesTheVault()
    {
        ContactLedger ledger = ABookWhere(BlackOpsKey.TopGoodwillBand + 3);
        ledger.RecordFavourGiven(Giver, "Madam Coil");
        ContactHistory before = ledger.For(Giver);

        var reloaded = new ContactLedger();
        VaultMapper.Apply(VaultMapper.ToSection(ledger), reloaded);
        ContactHistory after = reloaded.For(Giver);

        Assert.True(after.FavourSpent);
        Assert.Equal(before.Goodwill, after.Goodwill);
        Assert.False(BlackOpsKey.FavourIsOnTheTable(after));
        Assert.True(after.HasHistory);
    }

    /// <summary>…and a vault written before slice 2 loads with everybody's favour unspent, which is what was
    /// true when it was written. The flag defaults rather than throwing or arriving true.</summary>
    [Fact]
    public void AnOldVaultLoadsWithEverybodysFavourStillUnspent()
    {
        var section = new ContactsSection([new ContactRecord
        {
            ContactId = Giver,
            DisplayName = "Madam Coil",
            Goodwill = BlackOpsKey.TopGoodwillBand + 1,
        }]);

        var ledger = new ContactLedger();
        VaultMapper.Apply(section, ledger);

        Assert.False(ledger.For(Giver).FavourSpent);
        Assert.True(BlackOpsKey.FavourIsOnTheTable(ledger.For(Giver)));
    }
}
