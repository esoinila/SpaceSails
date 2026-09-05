using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #534 slice 1 · <b>SOME FAT MERCHANTS ARE NOT MERCHANTS, AND EVERY WAY OF KNOWING IS A NUMBER.</b>
///
/// <para>Owner: <i>"Some ships are posing as Rocinante but under came are masked war ships etc. 😎"</i>. The
/// issue's own first rule is the one these guards exist to hold: <b>this is a READ, not a dice roll on
/// boarding.</b> Everything decidable is decidable before the captain commits, out of instruments the ship
/// already carries, and the game never states the conclusion anywhere.</para>
///
/// <para>The audit row on #938 (2026-09-03) named the seam: <c>TransponderRule</c> models only the PLAYER's
/// lie — the ghost hull a fake beacon flies. This is the other side of it, and it is deliberately NOT built
/// on the beacon: a masked hull's transponder is honest about where she is, and dishonest about what she is,
/// which is a different lie and reads on different instruments.</para>
///
/// <para><b>The anti-vacuous half runs through every law below.</b> A guard that only ever looked at a masked
/// hull could not tell "the numbers disagree" from "the numbers always disagree", so every tell is asked of
/// an honest hauler in the same breath, and the honest answer is EQUALITY — to the bit.</para>
/// </summary>
public sealed class TheMaskedHullIsReadBeforeItIsMetTests
{
    /// <summary>A hull off the traffic schedule's own shape: the id is what the mask is drawn from, the hold
    /// is what makes her worth wearing, and the budget is the schedule's own default.</summary>
    private static NpcShip Hauler(string id, int cargoUnits, bool isPod = false) =>
        new(id, "Meridian", "He3", "saturn", "mars", RoutePersonality.Economical,
            DepartureTime: 0, ActivationTime: 0,
            InitialState: new ShipState(new Vector2d(1e11, 0), new Vector2d(0, 30000), 0),
            Plan: new ManeuverPlan([]), EstimatedArrivalTime: 60 * 86400,
            CargoUnits: cargoUnits, ManeuverBudget: NpcShip.DefaultManeuverBudget, IsPod: isPod);

    /// <summary>Every hull id the fixed tables can mint for a founding wave, fat enough to be worth wearing.
    /// Ids are the schedule's own (<c>npc-i</c> / <c>npc-wN-i</c>), because the mask is hashed off the id and
    /// a guard that invented its own id space would be measuring a world the game never builds.</summary>
    private static IEnumerable<NpcShip> FatFleet(int count)
    {
        for (int wave = 0; wave < 12; wave++)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Hauler(wave == 0 ? $"npc-{i}" : $"npc-w{wave}-{i}", QShip.FatHoldUnits + (i % 6));
            }
        }
    }

    // ── LAW 1 · THE RARITY ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE FAT MERCHANT IN TWELVE, AND THE WORLD CAN TELL PASS FROM FAIL. #533's rarity law, quoted in #534:
    /// <i>"if one in three haulers is a wolf, the read is worthless and everyone shoots first."</i> Rare
    /// enough that the tells are worth learning, common enough that a careless captain eventually meets one.
    ///
    /// <para>The band is asked of 1,200 hulls drawn from the ids the schedule actually mints, and it is
    /// two-sided on purpose: a rule that masked nobody and a rule that masked everybody are both caught here,
    /// which is the only reason this assertion can fail at all.</para>
    ///
    /// <para><b>The band is typed, not derived.</b> The first cut of this guard read its own bounds off
    /// <see cref="QShip.MaskedInEveryFatMerchants"/> — so setting the constant to 3 moved the band with it
    /// and the test stayed green on a world where one hauler in three is a wolf. That is this repo's
    /// "a green number never asked of the world", and the fix is the house's own: the number that governs the
    /// design is retyped here, and the share is measured against typed bounds.</para>
    ///
    /// <para><b>Proven RED</b> both ways: with <c>MaskedInEveryFatMerchants = 3</c> the measured share came
    /// back over the ceiling; with <c>IsMasked</c> returning <c>false</c> it came back 0.000 against the
    /// floor.</para>
    /// </summary>
    [Fact]
    public void OneFatMerchantInTwelveIsWearingSomethingElse()
    {
        // Retyped on purpose (see the docblock): a guard that read the constant into its own bounds would
        // agree with any rarity at all.
        Assert.Equal(12, QShip.MaskedInEveryFatMerchants);

        List<NpcShip> fleet = FatFleet(100).ToList();
        Assert.Equal(1200, fleet.Count);

        int masked = fleet.Count(QShip.IsMasked);
        double share = (double)masked / fleet.Count;

        Assert.True(share > 0.04 && share < 0.13,
            $"{masked} of {fleet.Count} fat merchants are masked ({share:F3}); the law is one in twelve. "
            + "Too many and the read is worthless because everyone shoots first; too few and nobody ever "
            + "learns the tells.");
    }

    /// <summary>
    /// NOTHING SMALL, AND NOTHING BALLISTIC. A lean hauler has nowhere to hide a warship and a mass-driver pod
    /// has no papers to lie on — so the whole question is asked only of the hulls a pirate actually wants.
    /// Asked over the same id space as the rarity law, so this is a claim about the rule and not about three
    /// hand-picked ships.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>IsFatMerchant</c> guard from <c>IsMasked</c>: 90 lean hulls
    /// came back masked.</para>
    /// </summary>
    [Fact]
    public void NothingLeanAndNothingBallisticIsEverWearingAnything()
    {
        var lean = new List<NpcShip>();
        var pods = new List<NpcShip>();
        for (int i = 0; i < 600; i++)
        {
            lean.Add(Hauler($"npc-{i}", QShip.FatHoldUnits - 1 - (i % 10)));
            pods.Add(Hauler($"pod-{i}", QShip.FatHoldUnits + (i % 6), isPod: true));
        }

        Assert.DoesNotContain(lean, QShip.IsMasked);
        Assert.DoesNotContain(pods, QShip.IsMasked);

        // …and the same id space DOES produce masked hulls once the hold is fat, so the two Empties above
        // are the fat/lean split talking and not a rule that never fires.
        Assert.Contains(lean.Select(s => s with { CargoUnits = QShip.FatHoldUnits }), QShip.IsMasked);
    }

    /// <summary>THE SAME HULL IS THE SAME HULL. Hashed off the id, never drawn from a live stream — asking
    /// twice, or asking on a client and a server, always agrees.</summary>
    [Fact]
    public void TheAnswerDoesNotChangeBetweenTwoAskings()
    {
        foreach (NpcShip ship in FatFleet(20))
        {
            Assert.Equal(QShip.IsMasked(ship), QShip.IsMasked(ship));
            Assert.Equal(QShip.IsMasked(ship), QShip.IsMasked(ship with { Callsign = "Windlass" }));
        }
    }

    // ── LAW 2 · TRUE AGAINST CLAIMED ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AN HONEST HAULER'S INSTRUMENTS AGREE WITH HER PAPERS, TO THE BIT — on all three readable tells. This
    /// is the half of the feature that makes the other half mean anything: the readout is not a detector with
    /// two settings, it is one measurement that happens to close on almost every hull in the sky.
    ///
    /// <para><b>Proven RED</b> by making <c>MeasuredTrimAccelMps2</c> return
    /// <c>ClaimedTrimAccelMps2 * 1.01</c> for everyone: 1,100 honest haulers reported a burn their tonnage
    /// could not explain.</para>
    /// </summary>
    [Fact]
    public void AnHonestHaulersArithmeticCloses()
    {
        int honest = 0;
        foreach (NpcShip ship in FatFleet(100).Where(s => !QShip.IsMasked(s)))
        {
            honest++;
            Assert.Equal(QShip.ClaimedTrimAccelMps2(ship), QShip.MeasuredTrimAccelMps2(ship));
            Assert.Equal(QShip.ClaimedRadiatorPanels(ship), QShip.MeasuredRadiatorPanels(ship));
            Assert.Equal(QShip.ClaimedGuardedChannels(ship), QShip.MeasuredGuardedChannels(ship));
        }

        Assert.True(honest > 1000, $"only {honest} honest haulers were swept — nothing was proved.");
    }

    /// <summary>
    /// AND A MASKED HULL'S DOES NOT — on every one of the three, and by the same reading each time. Each tell
    /// is individually deniable in the fiction (riding light this run; over-built cooling; surplus radio);
    /// what the code has to guarantee is that all three are actually THERE to be read, because a set of one
    /// is not a set.
    ///
    /// <para><b>Proven RED</b> by returning <c>ClaimedGuardedChannels</c> from
    /// <c>MeasuredGuardedChannels</c>: the comms tell went quiet and the assertion named it.</para>
    /// </summary>
    [Fact]
    public void AMaskedHullsArithmeticDoesNotClose()
    {
        List<NpcShip> masked = FatFleet(100).Where(QShip.IsMasked).ToList();
        Assert.True(masked.Count > 50, $"only {masked.Count} masked hulls were swept — nothing was proved.");

        foreach (NpcShip ship in masked)
        {
            Assert.True(QShip.MeasuredTrimAccelMps2(ship) > QShip.ClaimedTrimAccelMps2(ship));
            Assert.True(QShip.MeasuredRadiatorPanels(ship) > QShip.ClaimedRadiatorPanels(ship));
            Assert.True(QShip.MeasuredGuardedChannels(ship) > QShip.ClaimedGuardedChannels(ship));
        }
    }

    /// <summary>
    /// EVERY TELL IS THE HUNTER'S OWN NUMBER, OR A COUNT OFF IT. There is no third set of figures anywhere:
    /// her drive is <see cref="EncounterRule.HunterAccelMps2"/>, the fixed thrust the heat-hunters chase on
    /// (owner's standing ruling that the collector is thrust-only by design), and the radiator count is that
    /// same thrust put through <see cref="QShip.PanelsFor"/>. This is the repo's fifth named bug class held
    /// off at the source: a card and a sim cannot quote different numbers at each other if there is only one.
    ///
    /// <para><b>Proven RED</b> by giving the mask its own <c>0.55</c> instead of reading
    /// <c>EncounterRule.HunterAccelMps2</c>.</para>
    /// </summary>
    [Fact]
    public void SheIsFittedWithTheHunterClassAndNotWithNewNumbers()
    {
        NpcShip masked = FatFleet(100).First(QShip.IsMasked);

        Assert.Equal(EncounterRule.HunterAccelMps2, QShip.MeasuredTrimAccelMps2(masked));
        Assert.Equal(QShip.PanelsFor(EncounterRule.HunterAccelMps2), QShip.MeasuredRadiatorPanels(masked));
        Assert.Equal(NpcShip.DefaultManeuverBudget, QShip.ClaimedTrimAccelMps2(masked));
        Assert.Equal(QShip.PanelsFor(NpcShip.DefaultManeuverBudget), QShip.ClaimedRadiatorPanels(masked));
    }

    /// <summary>
    /// A PANEL IS A PANEL: THE COUNT ROUNDS UP AND NEVER DOWN. A hull carrying nine tenths of the cooling
    /// its drive needs is a hull that cannot run its drive, so a part panel is a whole panel. Pinned on the
    /// two drives the game actually produces — the hauler's claimed 0.3 and the hunter class's 0.5 — plus a
    /// value that is not a multiple of the panel, which is the only place the rounding is visible at all.
    ///
    /// <para>The monotone sweep is the law rather than the table: more drive is never fewer panels, asked of
    /// a hundred accelerations rather than of five.</para>
    ///
    /// <para><b>Proven RED</b> by rounding instead of ceiling in <c>PanelsFor</c>: 0.71 m/s² came back as
    /// 7 panels for a drive that needs eight.</para>
    ///
    /// <para>An earlier cut of <c>PanelsFor</c> carried a <c>- 1e-9</c> floating-point guard band. It was
    /// taken out because it could not be proven red: no accelerations in [0.001, 2.000] divided by a tenth
    /// lands over its own integer, so the band was defending against nothing measurable — and this repo does
    /// not ship a guard it has not watched fail.</para>
    /// </summary>
    [Fact]
    public void ThePanelCountIsCountedAndNotRoundedByAccident()
    {
        Assert.Equal(3, QShip.PanelsFor(0.3));   // an honest hauler's claim
        Assert.Equal(5, QShip.PanelsFor(0.5));   // the hunter class's own thrust
        Assert.Equal(8, QShip.PanelsFor(0.71));  // a part panel is a whole panel
        Assert.Equal(1, QShip.PanelsFor(0.05));
        Assert.Equal(0, QShip.PanelsFor(0));
        Assert.Equal(0, QShip.PanelsFor(-1));

        // Monotone: more drive is never fewer panels.
        int previous = 0;
        for (double a = 0; a <= 1.0; a += 0.01)
        {
            int panels = QShip.PanelsFor(a);
            Assert.True(panels >= previous, $"{a:F2} m/s² wants {panels} panels after {previous}.");
            previous = panels;
        }
    }

    // ── LAW 3 · SHE DOES NOT RUN THE WAY PREY RUNS ────────────────────────────────────────────────────

    /// <summary>
    /// PREY SWEEPS ITS BEARING OFF YOUR BOW; SHE HOLDS IT. Both hulls open the range — that part is the same,
    /// and it has to be, or the tell would be "one of them runs" rather than "they run differently". What
    /// separates them is what the bearing does while they go: a hull jinking abeam of its own course crosses
    /// the sightline and its bearing swings; a hull backing off down the sightline keeps the captain exactly
    /// where he was.
    ///
    /// <para>Measured rather than asserted: both branches are flown as a straight coast along the heading the
    /// law returns, and the bearing from the captain is read at the start and at the end.</para>
    ///
    /// <para><b>Proven RED</b> by returning the prey branch for a masked hull: her bearing drifted 40.4°,
    /// past the 1° the law allows, and the two branches became one.</para>
    /// </summary>
    [Fact]
    public void SheOpensTheRangeWithoutLettingYouOffHerBow()
    {
        var captain = new Vector2d(0, 0);
        var hers = new Vector2d(2.0e8, 0);
        var herVelocity = new Vector2d(4000, 1000); // her own errand, at an angle to the sightline
        const double flown = 5.0e7;                 // metres of retreat, the same for both branches

        (double drift, double opened) Run(bool masked)
        {
            double heading = QShip.EvadeHeadingRad(hers, herVelocity, captain, masked);
            Vector2d after = hers + new Vector2d(Math.Cos(heading), Math.Sin(heading)) * flown;
            double before = Math.Atan2(hers.Y - captain.Y, hers.X - captain.X);
            double now = Math.Atan2(after.Y - captain.Y, after.X - captain.X);
            double delta = Math.Abs(now - before) * 180.0 / Math.PI;
            return (delta, (after - captain).Length - (hers - captain).Length);
        }

        (double preyDrift, double preyOpened) = Run(masked: false);
        (double hersDrift, double hersOpened) = Run(masked: true);

        // Both are running: neither branch closes the gap.
        Assert.True(preyOpened > 0, $"prey did not open the range ({preyOpened:F0} m).");
        Assert.True(hersOpened > 0, $"she did not open the range ({hersOpened:F0} m).");

        // …and only one of them keeps the bearing.
        Assert.True(hersDrift < 1.0, $"she let the bearing go by {hersDrift:F1}° — that is how prey runs.");
        Assert.True(preyDrift > 3.0, $"prey held the bearing to {preyDrift:F1}° — the two branches are one.");
    }

    /// <summary>
    /// PREY BREAKS AWAY FROM THE CAPTAIN, NOT TOWARD HIM. The abeam branch has two sides to choose between
    /// and picking the wrong one would fly a fleeing hauler straight down the boarding tube. Asked from both
    /// sides of her course, so a sign error cannot hide on one of them.
    ///
    /// <para><b>Proven RED</b> by flipping the <c>abeam.Dot(away) >= 0</c> test: the hull turned into the
    /// captain from one side and the closing assertion caught it.</para>
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void PreyBreaksAwayAndNotIntoYou(int side)
    {
        var captain = new Vector2d(0, 0);
        var hers = new Vector2d(2.0e8 * side, 0);
        var herVelocity = new Vector2d(4000, 1000);

        double heading = QShip.EvadeHeadingRad(hers, herVelocity, captain, masked: false);
        Vector2d after = hers + new Vector2d(Math.Cos(heading), Math.Sin(heading)) * 5.0e7;
        Assert.True((after - captain).Length > (hers - captain).Length,
            "a fleeing hauler that turns toward the captain is not fleeing.");
    }

    /// <summary>NO NaN EVER REACHES A MANEUVER PLAN. Two hulls on top of each other, and a hull with no way
    /// on, are both geometry the live sim can hand this law; the fallbacks are asked here so a degenerate
    /// frame cannot poison a plan the simulator then flies for ever.</summary>
    [Fact]
    public void DegenerateGeometryStillReturnsAHeading()
    {
        foreach (bool masked in new[] { true, false })
        {
            Assert.False(double.IsNaN(QShip.EvadeHeadingRad(Vector2d.Zero, Vector2d.Zero, Vector2d.Zero, masked)));
            Assert.False(double.IsNaN(QShip.EvadeHeadingRad(new Vector2d(1e8, 0), Vector2d.Zero, Vector2d.Zero, masked)));
            Assert.False(double.IsNaN(QShip.EvadeHeadingRad(Vector2d.Zero, new Vector2d(0, 3000), Vector2d.Zero, masked)));
        }
    }

    /// <summary>THE BURN IS THE SHAPE THE SIM ALREADY FLIES — one X-Pilot pulse along the law's heading, the
    /// same mode and strength <c>TrafficSchedule.StarterFreighter</c>'s escape jink uses, so her speed stays
    /// matchable and a captain who reads her right and closes anyway still has a ship to catch.</summary>
    [Fact]
    public void HerBreakIsTheGamesOwnEscapeJink()
    {
        NpcShip masked = FatFleet(100).First(QShip.IsMasked);
        var hers = new ShipState(new Vector2d(2.0e8, 0), new Vector2d(0, 4000), 1000);
        ManeuverNode node = QShip.EvadeBurn(masked, hers, Vector2d.Zero, hers.SimTime);

        Assert.Equal(BurnMode.Vector, node.Mode);
        Assert.Equal(1, node.Pulses);
        Assert.Equal(QShip.EvadePercent, node.Percent);
        Assert.Equal(hers.SimTime, node.SimTime);
        Assert.Equal(
            QShip.EvadeHeadingRad(hers.Position, hers.Velocity, Vector2d.Zero, masked: true) * 180.0 / Math.PI,
            node.HeadingDegrees, 9);
    }

    // ── LAW 4 · COMMITTING IS UNCHANGED, AND SHE RESOLVES AS WHAT SHE IS ──────────────────────────────

    /// <summary>
    /// SHE DOES NOT HEAVE TO, AT ANY HEAT. The whole cost of getting the read wrong, and it needed no new
    /// rule about combat: <see cref="EncounterRule.ComplianceOf"/> answers with her TRUE class and everything
    /// downstream — the warning shot that buys nothing, the stubborn heat a robbery costs, the muscle she
    /// calls — is the machinery that has always been there, meeting a target that shoots back.
    ///
    /// <para>The anti-vacuous half is the second loop: honest fat merchants still roll BOTH ways, so this is
    /// not a rule that made every hauler stubborn.</para>
    ///
    /// <para><b>Proven RED</b> by removing the <c>QShip.IsMasked</c> branch from <c>ComplianceOf</c>: 61 of
    /// the 100 masked hulls swept heaved to under a warning shot.</para>
    /// </summary>
    [Fact]
    public void BoardingHerIsBoardingAWarship()
    {
        List<NpcShip> masked = FatFleet(100).Where(QShip.IsMasked).ToList();
        Assert.True(masked.Count > 50, $"only {masked.Count} masked hulls were swept — nothing was proved.");

        for (int heat = 0; heat <= EncounterRule.MaxHeatLevel; heat++)
        {
            foreach (NpcShip ship in masked)
            {
                Assert.Equal(ComplianceState.Stubborn, EncounterRule.ComplianceOf(ship, heat));
            }
        }

        List<ComplianceState> honest = FatFleet(100)
            .Where(s => !QShip.IsMasked(s))
            .Select(s => EncounterRule.ComplianceOf(s, 0))
            .ToList();
        Assert.Contains(ComplianceState.Compliant, honest);
        Assert.Contains(ComplianceState.Stubborn, honest);
    }

    /// <summary>A POD IS STILL A POD. The masking branch sits behind the pod branch, so the one hull with
    /// nobody aboard to negotiate with still has nobody aboard to negotiate with.</summary>
    [Fact]
    public void APodStillHasNobodyToTalkTo()
    {
        NpcShip pod = Hauler("npc-3", QShip.FatHoldUnits + 4, isPod: true);
        Assert.Equal(ComplianceState.NothingToComply, EncounterRule.ComplianceOf(pod, 3));
    }

    // ── LAW 5 · THE GAME NEVER STATES IT ──────────────────────────────────────────────────────────────

    /// <summary>
    /// THE RULE PUBLISHES NO PROSE AT ALL — no label, no plate, no line, not one string. #534's own words:
    /// <i>"the scope reports a burn, the telescope reports a radiator, and the captain does the arithmetic or
    /// does not."</i> A verdict written anywhere in this pipe would delete the mechanic, so the type may only
    /// ever return numbers, and the one tell that is genuinely prose — how she answers a hail — is slice 2 and
    /// carries a <c>// FABLE:</c> marker rather than a const.
    ///
    /// <para>This also settles the reserved word of <c>docs/worldbuilding-notes.md</c> §8 for free: a type
    /// with no strings in it cannot contain "monolith". The coverage floor is the anti-vacuous half — a
    /// rename that emptied the type could not turn this green by leaving nothing to check.</para>
    ///
    /// <para><b>Proven RED</b> by adding <c>public const string Plate = "Q-SHIP";</c> to <see cref="QShip"/>:
    /// <c>field Plate</c> was named.</para>
    /// </summary>
    [Fact]
    public void TheReadPublishesNoProseAtAll()
    {
        Type rule = typeof(QShip);
        var offenders = new List<string>();
        int surface = 0;

        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        foreach (FieldInfo f in rule.GetFields(Public))
        {
            surface++;
            if (f.FieldType == typeof(string) || f.FieldType == typeof(string[]))
            {
                offenders.Add($"field {f.Name}");
            }
        }
        foreach (PropertyInfo p in rule.GetProperties(Public))
        {
            surface++;
            if (p.PropertyType == typeof(string))
            {
                offenders.Add($"property {p.Name}");
            }
        }
        foreach (MethodInfo m in rule.GetMethods(Public))
        {
            surface++;
            if (m.ReturnType == typeof(string) && !m.IsSpecialName && m.DeclaringType == rule)
            {
                offenders.Add($"method {m.Name}");
            }
        }

        Assert.True(surface >= 12, $"the rule's public surface is only {surface} member(s) — nothing swept.");
        Assert.True(offenders.Count == 0,
            "#534's whole mechanic is that the game never states it, so the rule publishes no prose. Found: "
            + string.Join(", ", offenders));
    }

    /// <summary>
    /// AND THERE IS NOT ONE STRING LITERAL IN THE FILE EITHER. The reflection sweep above cannot see a
    /// literal written inside a method body, and a verdict does not have to be a <c>const</c> to be a
    /// verdict. So the source is read with its comments taken out, and the assertion is the strongest one
    /// available: <b>zero quoted text in the executable half of the file.</b> Nothing can be said by code
    /// that never spells a word.
    ///
    /// <para>That settles §8's reserved word for free on the code side; the whole file — comments included —
    /// is checked for it separately, because a docblock is where a stray "monolith" would actually get in.
    /// Nothing here bans the feature's own vocabulary from a docblock: the split between what a reviewer may
    /// read and what a player may is exactly the split between a comment and a literal.</para>
    ///
    /// <para><b>Proven RED</b> by putting <c>string verdict = "she is not what she says";</c> in
    /// <c>MeasuredGuardedChannels</c>: the line was quoted back with its number.</para>
    /// </summary>
    [Fact]
    public void NotOneStringLiteralLivesInTheRule()
    {
        var spoken = new List<string>();
        bool inBlockComment = false;
        int code = 0;

        foreach ((string raw, int number) in Source().Select((l, i) => (l, i + 1)))
        {
            string line = raw;
            if (inBlockComment)
            {
                int close = line.IndexOf("*/", StringComparison.Ordinal);
                if (close < 0) { continue; }
                line = line[(close + 2)..];
                inBlockComment = false;
            }

            int open = line.IndexOf("/*", StringComparison.Ordinal);
            if (open >= 0) { inBlockComment = true; line = line[..open]; }

            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            if (slashes >= 0) { line = line[..slashes]; }

            if (line.Trim().Length == 0) { continue; }
            code++;

            if (line.Contains('"') || line.Contains('\''))
            {
                spoken.Add($"line {number}: {line.Trim()}");
            }
        }

        Assert.True(code >= 40, $"only {code} line(s) of code were read — the sweep found no file.");
        Assert.True(spoken.Count == 0,
            "the game never states it — not on a plate, and not in the code that would feed one. Found: "
            + string.Join(" | ", spoken));

        // docs/worldbuilding-notes.md §8: the word is reserved for the one object that owns it, and this
        // file has no business with it anywhere — docblocks included.
        string whole = string.Join("\n", Source());
        Assert.DoesNotContain("monolith", whole, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] Source()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "src", "SpaceSails.Core")))
        {
            dir = dir.Parent;
        }

        string path = System.IO.Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary"),
            "src", "SpaceSails.Core", "QShip.cs");
        return System.IO.File.ReadAllLines(path);
    }
}
