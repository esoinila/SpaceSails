using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #763 · THE KIT THAT HEARS THE BUTTONS — the outlaw end of #760's axis.
///
/// <para>Owner: <i>"you search for signal in the vicinity … find it, and get the secret button pressed, pretty
/// much by just knowing where to go to look"</i>, and the refinement that is the design: <i>"Passive detection
/// is free and automatic … CONNECTING is the first active act — and the first thing that may have
/// consequences."</i></para>
///
/// <para>Five laws, and every one of them was watched going red. The verbatim runs are in the pull
/// request.</para>
/// </summary>
public class TheKitThatHearsTheButtonsTests
{
    // ── THE SWEEP ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Generated site ids, in TheStandingTravelsTests' own idiom, because what is being audited is a rule
    // about OPERATORS and about a one-in-a-hundred band, and a handful of named rocks cannot be relied on to
    // supply either. Every site here is a real site: the same generator, the same seeds, the same depth
    // arithmetic the game runs on.

    private const int SweepSize = 120;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static IEnumerable<string> SweepSites() =>
        Enumerable.Range(0, SweepSize).Select(i => $"sweep-site-{i}");

    /// <summary>Every (site, floor) the sweep can stand on, top to bottom.</summary>
    private static IEnumerable<(string Body, int Level)> SweepFloors() =>
        SweepSites().SelectMany(b => UndergroundComplex.FloorsOf(b).Select(l => (b, l)));

    /// <summary>A site with halls under it, found by walking the world rather than typed in — and asserted
    /// to be real, because every "accepted press" fact in this file is about that band and a fixture that
    /// quietly handed back an ordinary site would turn them into worlds that cannot tell pass from
    /// fail.</summary>
    private static string ASiteWithHalls()
    {
        foreach (string body in SweepSites().Append(UndergroundComplex.FoundBandCheatSiteId))
        {
            if (UndergroundComplex.HasFoundBand(body))
            {
                return body;
            }
        }
        throw new InvalidOperationException(
            "the sweep never found a site with halls — every press this file calls ACCEPTED would be a " +
            "world with nothing in it to accept anything.");
    }

    /// <summary>The top floor of the halls: where a captain steps out of the car into a gallery.</summary>
    private static int InTheHalls(string body) =>
        UndergroundComplex.BandTop(UndergroundComplex.FoundBandOf(body));

    /// <summary>Standing on the cage's own landing, which is where everything on a floor is heard from.</summary>
    private static (double X, double Y) AtTheCage()
    {
        foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
        {
            if (car.Kind == UndergroundComplex.ShaftKind.Cage)
            {
                return car.Landing;
            }
        }
        throw new InvalidOperationException("no cage on the plan — every floor in the game has one.");
    }

    // ══ (a) THE HITS ARE THE FLOOR'S OWN RADIO-ADDRESSED SET, AND THEY CARRY NO PLATE ════════════════════

    /// <summary>
    /// WHAT THE KIT HEARS IS WHAT THE PLAN HAS. Asked of <see cref="UndergroundComplex.ShaftsOn"/> and
    /// <see cref="UndergroundComplex.NextShaftBelow"/> — the plan's own lists — and never against a second
    /// copy of the scanner's arithmetic, so a kit that invented a carrier or missed one goes red.
    /// </summary>
    [Fact]
    public void TheSweepHearsExactlyTheCarsOnThisFloorAndTheGateUnderIt()
    {
        int floors = 0, withHits = 0;
        foreach ((string body, int level) in SweepFloors().Take(400))
        {
            if (SdrScanner.Quiet(body))
            {
                continue;
            }
            floors++;

            // A reach that cannot be the reason anything is missing: the whole floor is inside it.
            IReadOnlyList<SdrScanner.Hit> heard =
                SdrScanner.Hits(body, level, 0, 0, Field, reachDu: 100_000);

            IReadOnlyList<UndergroundComplex.Shaft> cars = UndergroundComplex.ShaftsOn(Field);
            bool wayDown = UndergroundComplex.NextShaftBelow(body, level) is not null;

            Assert.Equal(
                cars.Count + (wayDown ? cars.Count(c => c.RunsTheGate) : 0),
                heard.Count);

            foreach (UndergroundComplex.Shaft car in cars)
            {
                (double lx, double ly) = car.Landing;
                Assert.Equal(1, heard.Count(h =>
                    h.What == SdrScanner.Emitter.LiftCall
                    && Math.Abs(h.X - lx) < 1e-9 && Math.Abs(h.Y - ly) < 1e-9));
            }

            Assert.Equal(
                wayDown,
                heard.Any(h => h.What == SdrScanner.Emitter.Door));

            if (heard.Count > 0)
            {
                withHits++;
            }
        }

        Assert.True(floors > 100, $"only {floors} floor(s) were swept — this proves little.");
        Assert.True(withHits > 0, "no floor in the whole sweep had anything on the air.");
    }

    /// <summary>REACH IS THE ONLY SKILL. A carrier past the kit's reach is not heard, and the rounding is the
    /// kit's own and not the caller's.</summary>
    [Fact]
    public void NothingPastTheKitsReachIsHeard()
    {
        string body = SweepSites().First(b => UndergroundComplex.NextShaftBelow(b, -1) is not null);
        (double cx, double cy) = AtTheCage();

        Assert.NotEmpty(SdrScanner.Hits(body, -1, cx, cy, Field));

        // A pace past the reach, along the corridor, and the cage goes quiet.
        Assert.Empty(SdrScanner.Hits(body, -1, cx + SdrScanner.ScanReachDu + 1.0, cy, Field));

        foreach (SdrScanner.Hit hit in SdrScanner.Hits(body, -1, cx + 41.3, cy, Field))
        {
            Assert.Equal(0.0, hit.RangeDu % SdrScanner.RangeStepDu, 9);
            Assert.Contains(hit.Bearing, SdrScanner.Bearings);
        }
    }

    /// <summary>
    /// A HIT NEVER CARRIES A PLATE. The kit hears a carrier; it does not read a stencil. A line that named
    /// the shaft, the car, the office or the site would hand the captain the one inference this whole
    /// facility is arranged around them making themselves (§13.10).
    /// </summary>
    [Fact]
    public void NoHitEverNamesAShaftACarAnOfficeOrASite()
    {
        var forbidden = new List<string>
        {
            UndergroundComplex.CageSign.ToUpperInvariant(),
            UndergroundComplex.ServiceCarSign.ToUpperInvariant(),
            // The word "lift" itself is not a plate — "a lift call" is the KIND, and the kind is the one
            // thing the kit is allowed to say. What is forbidden is the stencil: the sign painted at the
            // car's mouth, the shaft number, the office, the site.
            "SHAFT", "GOODS CAR", "BAND", "SITE", "B1", "B2", "B3",
        };
        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            forbidden.Add(op.Name.ToUpperInvariant());
            forbidden.Add(op.Id.ToUpperInvariant());
        }
        forbidden.Add(SiteOperator.UnplaceableStandingName.ToUpperInvariant());

        int lines = 0;
        foreach ((string body, int level) in SweepFloors().Take(400))
        {
            foreach (SdrScanner.Hit hit in
                     SdrScanner.Hits(body, level, 0, 0, Field, reachDu: 100_000))
            {
                lines++;
                string said = SdrScanner.HitLine(hit).ToUpperInvariant();
                foreach (string plate in forbidden)
                {
                    Assert.DoesNotContain(plate, said, StringComparison.Ordinal);
                }
                Assert.DoesNotContain(body.ToUpperInvariant(), said, StringComparison.Ordinal);
                Assert.DoesNotContain(
                    BodyNames.Designation(body).ToUpperInvariant(), said, StringComparison.Ordinal);
            }
        }

        Assert.True(lines > 100, $"only {lines} hit line(s) were read — this proves little.");
    }

    // ══ (b) THE PRESS ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A WAKE-WORD WITH NO STANDING IS ACCEPTED ONLY WHERE NO REGISTER COVERS THE DOOR — the halls nobody
    /// dug — and is refused, and CHARGED, everywhere else. Both directions, and the charge is keyed to the
    /// OUTFIT and never to the rock, which is the whole of #715's ruling.
    /// </summary>
    [Fact]
    public void ThePressIsAcceptedInTheHallsAndRefusedAtEveryListedOperatorsDoor()
    {
        string halls = ASiteWithHalls();
        int inTheHalls = InTheHalls(halls);
        (double cx, double cy) = AtTheCage();

        IReadOnlyList<SdrScanner.Hit> inTheGallery = SdrScanner.Hits(halls, inTheHalls, cx, cy, Field);
        Assert.NotEmpty(inTheGallery);

        foreach (SdrScanner.Hit hit in inTheGallery)
        {
            SdrScanner.Pressed pressed = SdrScanner.Press(halls, inTheHalls, hit);
            Assert.True(pressed.Worked, "a wake-word was refused in a hall that no register covers.");
            Assert.Equal(UndergroundComplex.NothingOwed, pressed.Charge);
            Assert.False(string.IsNullOrWhiteSpace(pressed.Line));
        }

        int refused = 0;
        foreach ((string body, int level) in SweepFloors().Take(400))
        {
            if (SdrScanner.Quiet(body) || UndergroundComplex.IsFound(body, level))
            {
                continue;
            }
            foreach (SdrScanner.Hit hit in
                     SdrScanner.Hits(body, level, 0, 0, Field, reachDu: 100_000))
            {
                SdrScanner.Pressed pressed = SdrScanner.Press(body, level, hit);
                refused++;
                Assert.False(pressed.Worked,
                    $"a listed operator's door at {body} B{-level} opened for nobody.");
                Assert.Equal(SiteOperator.Of(body).Id, pressed.Charge.OperatorId);
                Assert.NotEqual(body, pressed.Charge.OperatorId);
                Assert.Equal(UndergroundComplex.RefusedCardHeat, pressed.Charge.Points);
            }
        }

        Assert.True(refused > 100, $"only {refused} press(es) were refused — this proves little.");
    }

    /// <summary>
    /// #590/§13.5 · AND IT NEVER BUYS DEPTH. The one place a press is accepted is the bottom of the world:
    /// there is no band under the halls for an accepted press to open, so the kit's payoff is KNOWING and
    /// never DESCENDING. Written as a law about the world rather than about the scanner, because that is
    /// what would have to change for the kit to become a way past the paper.
    /// </summary>
    [Fact]
    public void NoFloorWhereThePressIsAcceptedHasAGateUnderIt()
    {
        int checkedFloors = 0;
        foreach ((string body, int level) in
                 SweepFloors().Append((ASiteWithHalls(), InTheHalls(ASiteWithHalls()))))
        {
            if (SdrScanner.OperatorOf(body, level) is not null)
            {
                continue;
            }
            checkedFloors++;
            Assert.Null(UndergroundComplex.NextShaftBelow(body, level));
            Assert.DoesNotContain(
                SdrScanner.Hits(body, level, 0, 0, Field, reachDu: 100_000),
                h => h.What == SdrScanner.Emitter.Door);
        }

        Assert.True(checkedFloors > 0, "the sweep never stood on a floor the press is accepted on.");
    }

    // ══ (c) THE SILENCE THAT IS THE POINT ═══════════════════════════════════════════════════════════════

    /// <summary>#649/#672 · THE HEAD OFFICE IS QUIET, and the kit hears nothing there on any floor.</summary>
    [Fact]
    public void TheKitHearsNothingWhateverAtTheHeadOffice()
    {
        string hq = KaamosLore.IceMoonBodyId;
        Assert.True(UndergroundComplex.IsHeadOffice(hq), "the ice moon is not the head office any more.");
        Assert.True(SdrScanner.Quiet(hq));

        int floors = 0;
        foreach (int level in UndergroundComplex.FloorsOf(hq))
        {
            floors++;
            Assert.Empty(SdrScanner.Hits(hq, level, 0, 0, Field, reachDu: 100_000));
        }
        Assert.True(floors > 0, "the head office has no floors — this test swept nothing.");

        // …and nowhere else is. Quiet is the head office's rank difference, not a common condition.
        Assert.All(SweepSites(), b => Assert.False(SdrScanner.Quiet(b),
            "a rolled site went quiet — the silence stops meaning anything the moment two places have it."));
    }

    /// <summary>
    /// THE CANON SWEEP. Not one string this feature can produce — the constants, the hit lines, the press
    /// lines, the receipts — may say, or imply with a word of radio craft, that the watchers or whoever files
    /// them emit ANYTHING, or that the silence at the head office is somebody's doing.
    /// </summary>
    [Fact]
    public void NoStringTheKitCanSayClaimsTheSilenceIsHidingSomething()
    {
        string[] forbidden =
        [
            "ACKNOWLEDG", "REEVER", "OLD ONE", "WATCHER", "JAMM", "JAMMED", "SUPPRESS", "SHIELDED",
            "SOMETHING ANSWERS", "SOMETHING REPLIES", "MASKED", "BEING HIDDEN", "GOES QUIET WHEN",
        ];

        var said = new List<string>();
        foreach (FieldInfo f in typeof(SdrScanner).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string s)
            {
                said.Add(s);
            }
        }
        foreach (PropertyInfo p in typeof(SdrScanner).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (p.PropertyType == typeof(string) && p.GetValue(null) is string s)
            {
                said.Add(s);
            }
        }
        foreach (SdrScanner.Emitter what in Enum.GetValues<SdrScanner.Emitter>())
        {
            said.Add(SdrScanner.AcceptedLine(what));
            said.Add(SdrScanner.RefusedLine(what));
            said.Add(SdrScanner.WhatItIs(what));
        }
        said.Add(SurfaceSalvage.KitLine());
        said.Add(SurfaceSalvage.LabelFor(SurfaceSalvage.Find.Kit));
        said.Add(SdrScanner.Buy(0, null).Line);
        said.Add(SdrScanner.Buy(SdrScanner.PriceCr, null).Line);
        said.Add(SdrScanner.Buy(SdrScanner.PriceCr, [SdrScanner.TheKit]).Line);

        Assert.True(said.Count > 20, $"only {said.Count} string(s) were swept — this proves little.");
        foreach (string line in said)
        {
            string up = line.ToUpperInvariant();
            foreach (string wrong in forbidden)
            {
                Assert.DoesNotContain(wrong, up, StringComparison.Ordinal);
            }
        }
    }

    // ══ (e) THE KIT IS SEEDED, AND IT IS BUYABLE ════════════════════════════════════════════════════════

    /// <summary>#763 · A CAPTAIN WHO NEVER WALKS INTO A RUIN CAN STILL GET ONE, AND SO CAN ONE WHO NEVER
    /// DRINKS. Both routes exist, and both are asserted here rather than assumed.</summary>
    [Fact]
    public void TheKitIsInTheRuinsAndUnderTheRoadsteadsCounter()
    {
        int inRuins = 0;
        foreach (string body in new[] { "miranda", "luna", "phobos", "europa", "titan", "triton" })
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                for (int i = 0; i < 40; i++)
                {
                    if (SurfaceSalvage.WhatIsInside(body, site.LayoutSalt, i) == SurfaceSalvage.Find.Kit)
                    {
                        inRuins++;
                    }
                }
            }
        }
        Assert.True(inRuins > 0, "no ruin in the whole sweep had a kit under a bunk.");
        Assert.False(string.IsNullOrWhiteSpace(SurfaceSalvage.LabelFor(SurfaceSalvage.Find.Kit)));
        Assert.False(string.IsNullOrWhiteSpace(SurfaceSalvage.KitLine()));

        Barkeep roadstead = Barkeeps.For("the-space-bar")
            ?? throw new InvalidOperationException("the Rusty Roadstead has no keep any more.");
        Assert.True(roadstead.BackCounter, "the Roadstead stopped keeping one under the counter.");

        // …and it stays a fact about ONE room. A back counter everywhere is a shop, and a rumour you can buy
        // anywhere is not a rumour.
        Assert.Equal(1, Barkeeps.AllBarkeeps.Count(k => k.BackCounter));
    }

    /// <summary>#763 · BOUGHT FOR COIN, and every refusal says why and costs nothing.</summary>
    [Fact]
    public void TheCounterTakesCoinAndRefusesInWords()
    {
        SdrScanner.Bought sold = SdrScanner.Buy(SdrScanner.PriceCr + 10, null);
        Assert.True(sold.Taken);
        Assert.Equal(SdrScanner.PriceCr, sold.Cost);
        Assert.Equal(10, sold.RemainingCredits);

        SdrScanner.Bought broke = SdrScanner.Buy(SdrScanner.PriceCr - 1, null);
        Assert.False(broke.Taken);
        Assert.Equal(0, broke.Cost);
        Assert.Equal(SdrScanner.PriceCr - 1, broke.RemainingCredits);

        SdrScanner.Bought already = SdrScanner.Buy(SdrScanner.PriceCr * 4, [SdrScanner.TheKit]);
        Assert.False(already.Taken);
        Assert.Equal(SdrScanner.PriceCr * 4, already.RemainingCredits);

        // A pocket with no room says so and takes nothing — the worst refusal in the game is the one that
        // takes the coin and hands over nothing (#678).
        List<Satchel.Item> full = [];
        for (int i = 0; i < Satchel.PocketCapacity; i++)
        {
            full.Add(new Satchel.Item(Satchel.Kind.Relic, $"ballast-{i}"));
        }
        SdrScanner.Bought noRoom = SdrScanner.Buy(SdrScanner.PriceCr * 4, full);
        Assert.False(noRoom.Taken);
        Assert.Equal(SdrScanner.PriceCr * 4, noRoom.RemainingCredits);

        foreach (SdrScanner.Bought answer in new[] { sold, broke, already, noRoom })
        {
            Assert.False(string.IsNullOrWhiteSpace(answer.Line));
        }

        // The kind is a TOOL and it is bulky: a receiver is not a card and the wallet's "never full" rule is
        // not its to borrow (#688).
        Assert.Equal(Satchel.Compartment.Pocket, Satchel.CompartmentOf(Satchel.Kind.Tool));
        Assert.True(SdrScanner.IsTheKit(SdrScanner.TheKit));
        Assert.True(SdrScanner.InThePocket([SdrScanner.TheKit]));
        Assert.False(SdrScanner.InThePocket([new Satchel.Item(Satchel.Kind.Tool, "something-else")]));
    }

    /// <summary>#763 · The kit has a card, because an object with plot in it is one you take home and look at
    /// again (#614) — and the card is the SCREEN the sweep is written onto.</summary>
    [Fact]
    public void TheKitIsWorthLookingAt()
    {
        Assert.True(CarriedObject.WorthLookingAt(SdrScanner.TheKit, "luna"));
        CarriedObject.Reveal card = CarriedObject.Card(SdrScanner.TheKit, "luna")!.Value;
        Assert.Equal(SdrScanner.CardLabel, card.Label);
        Assert.Equal(SdrScanner.CardStory, card.Story);
    }

    // ══ (f) ANTI-VACUOUS ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EVERY ANSWER THE KIT HAS IS REACHABLE IN THE SHIPPED WORLD. A feature whose interesting branch never
    /// occurs is dead content that still cost the walk to look for, and a guard swept over a world with none
    /// of it in it is a guard that cannot tell pass from fail.
    /// </summary>
    [Fact]
    public void EveryAnswerTheKitHasTurnsUpSomewhereInTheShippedWorld()
    {
        int floorsWithHits = 0, quietFloors = 0, accepted = 0, refused = 0, doors = 0, calls = 0;

        IEnumerable<(string Body, int Level)> everywhere =
            SweepFloors()
                .Concat(UndergroundComplex.FloorsOf(KaamosLore.IceMoonBodyId)
                    .Select(l => (KaamosLore.IceMoonBodyId, l)))
                .Concat(UndergroundComplex.FloorsOf(ASiteWithHalls())
                    .Select(l => (ASiteWithHalls(), l)));

        foreach ((string body, int level) in everywhere)
        {
            IReadOnlyList<SdrScanner.Hit> heard =
                SdrScanner.Hits(body, level, 0, 0, Field, reachDu: 100_000);

            if (SdrScanner.Quiet(body))
            {
                quietFloors++;
                Assert.Empty(heard);
                Assert.False(string.IsNullOrWhiteSpace(SdrScanner.QuietLine));
                continue;
            }

            if (heard.Count > 0)
            {
                floorsWithHits++;
            }

            foreach (SdrScanner.Hit hit in heard)
            {
                if (hit.What == SdrScanner.Emitter.Door)
                {
                    doors++;
                }
                else
                {
                    calls++;
                }

                SdrScanner.Pressed pressed = SdrScanner.Press(body, level, hit);
                if (pressed.Worked)
                {
                    accepted++;
                }
                else
                {
                    refused++;
                }
            }
        }

        Assert.True(floorsWithHits > 0, "not one floor in the shipped world has anything on the air.");
        Assert.True(quietFloors > 0, "the sweep never stood anywhere quiet — the silence is untested.");
        Assert.True(accepted > 0, "no press anywhere in the shipped world is ever accepted.");
        Assert.True(refused > 0, "no press anywhere in the shipped world is ever refused.");
        Assert.True(doors > 0, "the kit never once heard a door — only lift calls exist in this world.");
        Assert.True(calls > 0, "the kit never once heard a lift call.");
    }
}
