using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 · <b>THE CLAIM IS THE SCENE — DRIVEN.</b> Owner ruling, 2026-09-06 on #525.
///
/// <para><b>Everything here goes through the game's own doors.</b> The hull is lost by turning both keys and
/// letting the clock run out; the kiosk is found by walking to it and pressing [E] through
/// <c>ViewNearbyObject</c>; the three presses are the counter's own rows, taken off <c>TheClaimAsks</c>; and
/// the payout is Harlan Fess's pitch going up. Nothing writes <c>_claimDesk</c>, <c>_claimOwed</c> or
/// <c>_credits</c> by hand. Reading the source is how the sibling issue got three different answers about
/// whether the castaway was even reachable.</para>
///
/// <para><b>And every guard is asked in a world that could answer the other way.</b> The presence law is
/// proved with a hunter that WOULD catch — parked on the captain's own state, so the aboard arm catches on
/// the first frame it is allowed to — and then the same hunter, in the same world, held by each of the three
/// ways of not being on her. The claim is proved against a purse that does not move until a salesman turns
/// up. The death arm is the same scuttle with the captain still on board.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheClaimIsWalkedEndToEndTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The port everything ashore happens at. It is a WORKING BERTH that the deal gave a machine
    /// to — both facts asserted rather than assumed in <see cref="TheKiosksSquare"/>, because a port with no
    /// kiosk would make every press guard below green about a console that is not there.</summary>
    private const string Port = "selene-gate";

    // ══ 1 · THE PRESENCE LAW ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>NOTHING THAT TAKES THE HULL RESOLVES WHILE THE MASTER IS OFF HER</b> — the owner's own sentence,
    /// asked four times in one world.
    ///
    /// <para>A collector is put on her sitting exactly where she is, so on any frame she is allowed to close
    /// she has already closed. Aboard, that is what happens: one catch, one demand panel. Off her — on a
    /// surface, away in the boat, or past the tube on somebody's concourse — a hundred frames go by and she
    /// is still out there, not broken off and not holding anybody, because the process wants a ship and a
    /// captain in one place.</para>
    /// </summary>
    [Theory]
    [InlineData(Posture.Aboard, false)]
    [InlineData(Posture.OnASurface, true)]
    [InlineData(Posture.AwayInTheBoat, true)]
    [InlineData(Posture.AshorePastTheTube, true)]
    public void THE_WRIT_ResolvesOnlyWithTheMasterAboardHer(Posture posture, bool held)
    {
        Pages.Map map = Boot();
        PutHimIn(map, posture);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");

        RunFrames(map, seconds: 10);

        if (!held)
        {
            Assert.NotNull(Read(map, "_busted"));
            Assert.Empty((IEnumerable)Read(map, "_hunters")!);   // caught, and retired off the roster
            return;
        }

        Assert.Null(Read(map, "_busted"));
        var hunters = (IList)Read(map, "_hunters")!;
        Assert.NotEmpty(hunters);
        Assert.False((bool)Get(hunters[0]!, "CaughtPlayer")!, "the writ was served with nobody to serve it on.");
        Assert.False((bool)Get(hunters[0]!, "BrokenOff")!, "the process did not wait, it gave up.");
    }

    /// <summary>
    /// <b>AND WAITING IS VISIBLE.</b> A held writ that nothing on any screen mentions is a pause, not a
    /// process — so the captain's own ledger carries the plate while somebody is out there unable to
    /// proceed, and carries no such row when the master is on his ship.
    /// </summary>
    [Fact]
    public void THE_LEDGER_CarriesThePendingPlateWhileHeIsOffHerAndNotWhenHeIsOnHer()
    {
        Pages.Map map = Boot();
        PutHimIn(map, Posture.OnASurface);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");
        RunFrames(map, seconds: 5);

        object row = TheWritRow(map) ?? throw new InvalidOperationException(
            "a collector is holding station and the ledger says nothing about it.");
        Assert.Equal(NebulaClaims.PendingWritPlate, (string)Get(row, "Title")!);
        Assert.Contains("GRIMHOLD", (IEnumerable<string>)Get(row, "Lines")!);

        // …and the same world with him back on her: no row at all. A plate that was always there would be a
        // standing note, and this is a state of paperwork.
        Set(map, "_surface", null);
        Assert.Null(TheWritRow(map));
    }

    /// <summary>
    /// <b>#1090's BREAK-OFF IS NOT THE END OF THE PROCESS.</b> The castaway ending is untouched — the chase
    /// still stops, the roster still empties, nothing is said — and the contract goes onto the file, waiting
    /// at the port that serves the ground he did it over, which is <c>QuietHands.PortFor</c>'s harbour and
    /// not the ground itself.
    /// </summary>
    [Fact]
    public void THE_PURSUER_StopsChasingAndStartsWaitingAtThePortThatServesTheGround()
    {
        Pages.Map map = Boot();
        PutHimIn(map, Posture.OnASurface);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");
        Assert.Null(Read(map, "_writPending"));

        string ground = TheGroundHeIsOn(map);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody harbour = QuietHands.PortFor(sky, ground)
            ?? throw new InvalidOperationException($"no harbour serves {ground} — the guard has no answer to check.");

        ArmHerCharges(map);
        RunUntilSheGoes(map);

        // The ending itself, unchanged: he lived, and nobody is chasing what is not there.
        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_shipEpitaph"));
        var hunters = (IList)Read(map, "_hunters")!;
        Assert.All(hunters.Cast<object>(), h => Assert.True((bool)Get(h, "BrokenOff")!));

        object writ = Read(map, "_writPending") ?? throw new InvalidOperationException(
            "the pursuers evaporated — nobody is waiting for him anywhere.");
        Assert.Equal("GRIMHOLD", (string)Get(writ, "Callsign")!);
        Assert.Equal(harbour.Id, (string)Get(writ, "HavenId")!);
        Assert.NotEqual(ground, (string)Get(writ, "HavenId")!);   // a ground has no berths to wait at
    }

    // ══ 2 · A DEATH PAYS AS BEFORE, AND ASKS NOBODY FOR ANYTHING ═════════════════════════════════════════

    /// <summary>
    /// <b>A DEATH IS AUTO-PROCESSED.</b> The same scuttle with the captain still standing on her: the death
    /// machinery runs, the insurance seam is consulted exactly where it always was, and no claim exists —
    /// nothing to lodge, nothing owed, no counter touched. The clause the whole feature hangs off.
    /// </summary>
    [Fact]
    public void A_DEATH_IsProcessedAndNeverBecomesAClaim()
    {
        Pages.Map map = Boot();
        Set(map, "_insurance", NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, (double)Read(map, "SimTime")!));

        ArmHerCharges(map);                 // …and he stays aboard: no surface, no boat, no gangway
        RunUntilSheGoes(map);

        Assert.NotNull(Read(map, "_busted"));
        Assert.Null(Read(map, "_shipEpitaph"));
        Assert.Null(Read(map, "_claimOwed"));
        Assert.Equal(0, (int)Read(map, "_claimsLodged")!);
        Assert.Null(Read(map, "_claimDesk"));
    }

    // ══ 3 · THE CASTAWAY, THE COUNTER, AND THE SALESMAN ══════════════════════════════════════════════════

    /// <summary>
    /// <b>THE WHOLE SCENE, END TO END.</b> She goes at a berth with the captain ashore; he is alive, shipless
    /// and paid NOTHING; he walks to the machine on the concourse and presses [E]; the counter refuses the
    /// wrong thing three times and takes the right thing three times; the claim is lodged and still pays
    /// nothing; and then a representative finds him.
    /// </summary>
    [Fact]
    public void THE_CASTAWAY_WalksToAKioskLodgesTheClaimAndIsPaidByTheRep()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);

        double now = (double)Read(map, "SimTime")!;
        PirateInsurance policy = NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, now);
        Set(map, "_insurance", policy);
        int purse = (int)Read(map, "_credits")!;

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);

        // ── HE LIVED, AND HE WAS PAID NOTHING. This is the ruling: only a death is auto-processed.
        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_shipEpitaph"));
        Assert.Equal(purse, (int)Read(map, "_credits")!);
        Assert.Null(Read(map, "_claimOwed"));

        // ── THE RECEIPT IS ON THE WIRE, because the harbour filed it before he was across the concourse.
        var wire = (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;
        NewsWire.NewsEvent receipt = wire.First(e => e.Kind == NewsWire.NewsEventKind.HullLostAtABerth);

        // ── THE WALK, AND THE PRESS. The console is found on the deck plan the port actually built.
        WalkToTheKiosk(map);
        Invoke(map, "ViewNearbyObject");
        object card = Read(map, "_viewObject") ?? throw new InvalidOperationException(
            "[E] at the claims machine raised nothing.");
        Assert.Equal(NebulaClaims.KioskPlate, (string)Get(card, "Label")!);
        Assert.Equal(NebulaClaims.OnApproach, (string?)Get(card, "Caption"));
        Assert.True((bool)Read(map, "TheClaimDeskIsUp")!);

        // ── PRESS ONE. A policy that is not in force is refused; the one in the wallet is taken.
        Assert.Equal(0, PressesTaken(map));
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Policy, "Premium", "Premium"), withPolicy: default);
        Assert.Equal(1, PressesTaken(map));

        // ── PRESS TWO. A name she has not answered to is refused; the name she answers to is taken.
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Hull, "AURORA QUEEN", "AURORA QUEEN"));
        Assert.Equal(1, PressesTaken(map));

        // …and the refusal that matters is the one he can actually make: #1151's glory-name ruling puts HER
        // OWN former name on the counter, so the wrong row is a row he can press. It is pressed here, off the
        // counter's own rows, and the machine does not move. (Slice 1 could only refuse a name that was not
        // on the counter at all — the desk's whole second press was one row and untestable in the shipping
        // world; the ratchet that said so is deleted with this.)
        string trueName = (string)Invoke(map, "ShipNameNow")!;
        NebulaClaims.Ask wasHers = TheRows(map).Single(r => r.Offer != trueName);
        Assert.Equal(ShipHistories.Hers.GloryName, wasHers.Offer);
        Press(map, wasHers);
        Assert.Equal(1, PressesTaken(map));
        Assert.Equal(NebulaClaims.OnApproach, (string?)Get(Read(map, "_viewObject")!, "Caption"));

        NebulaClaims.Ask hers = TheRows(map).Single(r => r.Offer == trueName);
        Press(map, hers);
        Assert.Equal(2, PressesTaken(map));

        // ── PRESS THREE. A headline that is not a receipt is refused; the harbour's filing is taken.
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Wire, "GRIMHOLD", "GRIMHOLD"));
        Assert.Equal(2, PressesTaken(map));
        NebulaClaims.Ask filed = TheRows(map).Single(r => r.Offer == receipt.Subject);
        Assert.Equal(NewsWire.Headline(receipt), filed.Label);      // the row wears the wire's own sentence
        Press(map, filed);

        // ── LODGED. The machine's second line is on the card, and the purse has still not moved.
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.Equal(NebulaClaims.LodgedLine, (string?)Get(Read(map, "_viewObject")!, "Caption"));
        Assert.Equal(purse, (int)Read(map, "_credits")!);

        object owed = Read(map, "_claimOwed") ?? throw new InvalidOperationException("nothing was lodged.");
        int payout = (int)Get(owed, "PayoutCr")!;
        Assert.Equal(InsuranceRule.HullClaimPayoutCr(policy, (double)Read(map, "SimTime")!), payout);
        Assert.True(payout > 0, "a Premium policy's claim is worth nothing — the guard cannot fail.");

        // ── AND A REPRESENTATIVE FINDS HIM. His pitch going up IS his next meeting.
        Invoke(map, "HisPitchGoesUp");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
        Assert.Equal(NebulaClaims.RepAtThePayout, (string?)Read(map, "_repSaid"));
        Assert.Null(Read(map, "_claimOwed"));

        // …and he is not paid twice for one hull.
        Invoke(map, "HisPitchGoesUp");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
    }

    /// <summary>
    /// <b>AN UNINSURED CAPTAIN HAS NOTHING TO PUT ON THE COUNTER.</b> Same loss, same machine, no policy: the
    /// first press has no row at all and the desk goes on asking for one. The other half of press one, and
    /// the reason the purse guard above is not green by accident.
    /// </summary>
    [Fact]
    public void AN_UNINSURED_CaptainCannotLodgeAnythingAtAll()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        Assert.Equal(PirateInsurance.Uninsured, (PirateInsurance)Read(map, "_insurance")!);

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);

        WalkToTheKiosk(map);
        Invoke(map, "ViewNearbyObject");
        Assert.True((bool)Read(map, "TheClaimDeskIsUp")!);

        Assert.Empty(TheRows(map));
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Policy, "None", "None"));
        Assert.Equal(0, PressesTaken(map));
        Assert.Equal(NebulaClaims.OnApproach, (string?)Get(Read(map, "_viewObject")!, "Caption"));
        Assert.Null(Read(map, "_claimOwed"));
    }

    /// <summary>
    /// <b>THE SECOND CLAIM CARRIES THE PAINTING, AND THE FIRST DOES NOT.</b> One claim is a thing that
    /// happened to a captain; two is a pattern. Both lodgings are driven through the same three presses, so
    /// the difference between them is the counter and nothing else.
    /// </summary>
    [Fact]
    public void THE_SECOND_ClaimComesWithTheDeskAndTheFirstDoesNot()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        Set(map, "_insurance",
            NebulaRep.PolicyAfterBuying(InsuranceTier.Basic, (double)Read(map, "SimTime")!));

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);
        WalkToTheKiosk(map);

        LodgeAWholeClaim(map);
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.Null(Read(map, "_storyCard"));

        Invoke(map, "HisPitchGoesUp");            // paid, so the file is clear for the next one
        Invoke(map, "CloseViewObject");

        LodgeAWholeClaim(map);
        Assert.Equal(2, (int)Read(map, "_claimsLodged")!);

        object beat = Read(map, "_storyCard") ?? throw new InvalidOperationException(
            "the second claim was lodged and the captain was shown nothing.");
        // `_storyCard` is a named value tuple, and a tuple's element names are compiler metadata: at runtime
        // the field really is Item1. Asked that way rather than by the name in the source, because the source
        // name is not there to ask for.
        var which = (StoryBeats.Beat)Get(beat, "Item1")!;
        Assert.Equal(StoryBeats.Beat.TheClaim, which);
        Assert.Equal(NebulaClaims.DeskArt, StoryBeats.ArtFile(which));
        Assert.Equal(NebulaClaims.DeskCaption, StoryBeats.Caption(which));
        Assert.True(File.Exists(Path.Combine(
                        RepoRoot(), "src", "SpaceSails.Client", "wwwroot", NebulaClaims.DeskArt)),
                    "the beat names a painting that is not in the folder.");
    }

    // ══ 4 · THE MACHINE'S OWN SQUARE, AND THE FILE ═══════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE KIOSK IS WHERE THE RULE SAYS IT IS</b>, on the deck the port actually builds — swept over every
    /// haven with an interior rather than checked at one, so a tier that stopped being asked shows up here.
    /// And it stands clear of everything else on the concourse: a machine within an interact radius of the
    /// plaque is a machine [E] can grab instead of the plaque.
    /// </summary>
    [Fact]
    public void TheKiosksSquare()
    {
        Pages.Map map = Boot();
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;

        int stood = 0;
        int bare = 0;
        foreach (CelestialBody body in sky.Bodies.Where(b => b.IsHaven && HavenInterior.HasInterior(b.Id)))
        {
            DeckPlan plan = HavenInterior.DockedDeck(body.Id, tier: ArrivalTube.TierFor(sky, body.Id))
                ?? throw new InvalidOperationException($"{body.Id} claims an interior and builds no deck.");
            List<DeckPlan.ConsoleSpot> kiosks =
                [.. plan.Consoles.Where(c => c.Label == NebulaClaims.KioskPlate)];

            bool expected = NebulaClaims.AKioskStands(ArrivalTube.TierFor(sky, body.Id), body.Id);
            Assert.Equal(expected ? 1 : 0, kiosks.Count);
            if (!expected)
            {
                bare++;
                continue;
            }

            stood++;
            DeckPlan.ConsoleSpot kiosk = kiosks[0];
            Assert.Equal(NebulaClaims.OnApproach, kiosk.Caption);

            foreach (DeckPlan.ConsoleSpot other in plan.Consoles)
            {
                if (other.Label == kiosk.Label)
                {
                    continue;
                }

                double dx = other.X - kiosk.X;
                double dy = other.Y - kiosk.Y;
                Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius,
                    $"at {body.Id} the claims machine is inside [E]'s reach of \"{other.Label}\".");
            }
        }

        // The world can answer either way — some ports have one and some do not.
        Assert.True(stood > 0, "no port in the shipping scenario stands a claims machine.");
        Assert.True(bare > 0, "every port stands one, so the placement rule is not being asked anything.");

        // …and the one this file walks to is one of them.
        Assert.True(NebulaClaims.AKioskStands(ArrivalTube.TierFor(sky, Port), Port),
                    $"{Port} has no kiosk, so every press guard in this file is about a console that is not there.");
        Assert.Equal(ArrivalTube.Tier.WorkingBerth, ArrivalTube.TierFor(sky, Port));
    }

    // ══ 5 · THE BEAM IS THE BEAM, WHOEVER IS ON THE OTHER END ════════════════════════════════════════════

    /// <summary>
    /// <b>A CLAIMS CALL COSTS WHAT A LASER PING COSTS.</b> Owner ruling, 2026-09-06: <i>"the tight-beam is
    /// the tight-beam, whoever is on the other end."</i>
    ///
    /// <para><b>The two are compared BY CONSTRUCTION, not by a typed expectation.</b> The laser ping is fired
    /// first and its trace is MEASURED; the trace the claims call must leave is then that same measured
    /// trace with the far end swapped for the port whose machine took the call. So a guard written this way
    /// cannot pass a version that charges the claim its own private price: the only thing it is allowed to
    /// differ in is who was on the other end.</para>
    ///
    /// <para>And the claim buys nothing with the beam it lights itself up with — a laser ping comes home with
    /// a fix on the ledger, a phone call does not.</para>
    /// </summary>
    [Fact]
    public void A_CLAIMS_CallOverTheRemoteCostsTheSameExposureALaserPingDoes()
    {
        Pages.Map lit = Boot();
        ClampAtThePort(lit);
        object post = PlantTheTrackingPost(lit);
        string hull = PutAHullInTheSky(lit);

        Assert.Empty(WhoLearnedWhereWeAre(post));
        Invoke(lit, "LaserRangeTarget", hull);

        IReadOnlyList<string> afterThePing = WhoLearnedWhereWeAre(post);
        Assert.Equal(new[] { hull }, afterThePing);
        Assert.NotEmpty(TheFixesOnTheLedger(post));      // the ping's own return: a track

        // ── THE SAME WORLD, THE SAME BEAM, A COMPANY'S MACHINE ON THE OTHER END.
        Pages.Map handset = Boot();
        ClampAtThePort(handset);
        object handsetPost = PlantTheTrackingPost(handset);

        string farEnd = (string)Invoke(handset, "TheKioskTheRemoteReaches")!;
        Assert.Equal(Port, farEnd);

        List<string> owed = [.. afterThePing
            .Select(who => string.Equals(who, hull, StringComparison.Ordinal) ? farEnd : who)
            .OrderBy(who => who, StringComparer.Ordinal)];

        Invoke(handset, "RaiseTheClaimsDesk");

        Assert.True((bool)Read(handset, "TheClaimDeskIsUp")!, "the switch did not raise the counter.");
        Assert.Equal(owed, WhoLearnedWhereWeAre(handsetPost));
        Assert.Empty(TheFixesOnTheLedger(handsetPost));
    }

    /// <summary>
    /// <b>AND A HANDSET THAT REACHES NOTHING LIGHTS NOTHING UP.</b> Out of range there is no switch, so there
    /// is no call — the charge is paid where the beam is actually keyed and not where the handset asks itself
    /// whether to draw a button. A captain is not lit up by looking at his own remote.
    /// </summary>
    [Fact]
    public void A_HANDSET_OutOfRangeOfEveryKioskKeysNothingAndCostsNothing()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        object post = PlantTheTrackingPost(map);

        var here = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", here with
        {
            Position = here.Position + new Vector2d(ActiveSensors.TightBeamMaxRangeMeters * 40, 0),
        });

        Assert.False((bool)Invoke(map, "TheRemoteReachesAKiosk")!);
        Assert.False((bool)Invoke(map, "TheRemoteReachesAKiosk")!);   // asked twice: asking is not keying

        Invoke(map, "RaiseTheClaimsDesk");
        Assert.Empty(WhoLearnedWhereWeAre(post));
        Assert.Null(Read(map, "_claimDesk"));
    }

    /// <summary>
    /// <b>THE PLATE ON HER OWN BULKHEAD SAYS SHE WAS RENAMED.</b> Owner ruling, 2026-09-06: the builder's
    /// plate stays discoverable and the cover-up varies per hull, dealt from her seed. Read off the deck the
    /// game builds for her, not off Core — the card the captain gets is the console's caption, and Core being
    /// right about the sentence proves nothing about the bulkhead carrying it.
    /// </summary>
    [Fact]
    public void HER_BUILDERS_PlateCarriesTheCoverUpHerSeedDealtHer()
    {
        Pages.Map map = Boot();
        var plan = (DeckPlan)Read(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot plate = plan.Consoles.First(c => c.Label == Core.Interior.Plaques.Ship.ConsoleLabel);

        string card = plate.Caption ?? throw new InvalidOperationException("the builder's plate has no card.");
        Assert.Equal(Core.Interior.Plaques.BuildersPlateLore(ShipHistories.Hers), card);
        Assert.Contains(Core.Interior.Plaques.Ship.Lore, card, StringComparison.Ordinal);
        Assert.NotEqual(Core.Interior.Plaques.Ship.Lore, card);

        // One cover-up, hers, and never the name under it — that name is the claims counter's second press.
        string covered = ShipHistories.HowThePlateIsHidden(ShipHistories.Hers) == ShipHistories.PlateConcealment.Plastered
            ? Core.Interior.Plaques.PlateSkimmedWithFiller
            : Core.Interior.Plaques.PlateBoltedOverAnother;
        string other = ReferenceEquals(covered, Core.Interior.Plaques.PlateSkimmedWithFiller)
            ? Core.Interior.Plaques.PlateBoltedOverAnother
            : Core.Interior.Plaques.PlateSkimmedWithFiller;
        Assert.Contains(covered, card, StringComparison.Ordinal);
        Assert.DoesNotContain(other, card, StringComparison.Ordinal);
        Assert.DoesNotContain(ShipHistories.Hers.GloryName!, card, StringComparison.Ordinal);
    }

    /// <summary>A real tracking post on the page, because the exposure is bookkeeping ON one: with the field
    /// left null the charge is a no-op and every guard above would be green about nothing.</summary>
    private static object PlantTheTrackingPost(Pages.Map map)
    {
        var post = new Pages.Stations.TrackingPost();
        Set(map, "_trackingPost", post);
        return post;
    }

    /// <summary>Who has been told where we are — the tracking post's own aware set, which is what the game
    /// charges an active beam against. Ordered, so two traces can be compared as they are.</summary>
    private static IReadOnlyList<string> WhoLearnedWhereWeAre(object post) =>
        [.. ((HashSet<string>)(post.GetType().GetField("_aware", Hidden)
             ?? throw new InvalidOperationException("the tracking post has no _aware set — the charge has moved."))
            .GetValue(post)!).OrderBy(who => who, StringComparer.Ordinal)];

    /// <summary>The fixes on the ledger — what a laser ping BUYS, as opposed to what it costs.</summary>
    private static IReadOnlyCollection<TrackedTarget> TheFixesOnTheLedger(object post) =>
        (IReadOnlyCollection<TrackedTarget>)Get(post, "Entries")!;

    /// <summary>One hull in the sky, live and observed, for the laser to be pointed at.</summary>
    private static string PutAHullInTheSky(Pages.Map map)
    {
        var eph = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        NpcShip hull = TrafficSchedule.Generate(eph, seed: 42, count: 1)[0];
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden | BindingFlags.Public)!;
        object npc = Activator.CreateInstance(stateType, nonPublic: true)!;
        stateType.GetField("Ship", Hidden)!.SetValue(npc, hull);
        stateType.GetField("State", Hidden)!.SetValue(npc, (ShipState)Read(map, "_ship")!);
        stateType.GetField("Active", Hidden)!.SetValue(npc, true);

        Array roster = Array.CreateInstance(stateType, 1);
        roster.SetValue(npc, 0);
        Set(map, "_npcStates", roster);
        return hull.Id;
    }

    /// <summary>
    /// <b>THE FILE SURVIVES A RELOAD.</b> The counter the unease is measured on, the claim a representative
    /// has not paid yet, and the writ waiting at a port — all three through <c>BuildVault</c> and back, in a
    /// world where each of them is a non-default value, so a round trip that dropped one would go red rather
    /// than agree with a zero.
    /// </summary>
    [Fact]
    public void THE_FILE_RoundTripsThroughTheVault()
    {
        Pages.Map map = Boot();
        Set(map, "_lodgingOfferedFor", "SALT WIDOW");   // #1151 slice 2 · the offer's once-per-loss latch
        Set(map, "_claimsLodged", 3);
        Set(map, "_claimOwed", new LodgedClaimRecord("THIS SHIP", 250, 1234.5));
        Set(map, "_writPending", new PendingWritRecord("GRIMHOLD", Port, 99.5));

        var saved = (Vault)Invoke(map, "BuildVault", "", "")!;
        Assert.Equal(3, saved.Progress?.ClaimsLodged);
        Assert.Equal(250, saved.Progress?.ClaimOwed?.PayoutCr);
        Assert.Equal(Port, saved.Progress?.WritPending?.HavenId);

        // …and through the serializer, which is what a reload actually reads.
        Vault reread = VaultSerializer.Load(VaultSerializer.Save(saved))
            ?? throw new InvalidOperationException("the vault did not survive its own serializer.");

        Pages.Map loaded = Boot();
        Invoke(loaded, "ApplyVault", reread);
        Assert.Equal(3, (int)Read(loaded, "_claimsLodged")!);
        Assert.Equal(250, (int)Get(Read(loaded, "_claimOwed")!, "PayoutCr")!);
        Assert.Equal("GRIMHOLD", (string)Get(Read(loaded, "_writPending")!, "Callsign")!);
        Assert.Equal("SALT WIDOW", (string?)Read(loaded, "_lodgingOfferedFor"));

        // A blank file writes none of the four — the checksum law every row beside them keeps.
        Pages.Map fresh = Boot();
        var blank = (Vault)Invoke(fresh, "BuildVault", "", "")!;
        Assert.Null(blank.Progress?.ClaimsLodged);
        Assert.Null(blank.Progress?.ClaimOwed);
        Assert.Null(blank.Progress?.WritPending);
        Assert.Null(blank.Progress?.LodgingOfferedFor);
    }

    // ══ 4 · #1151 SLICE 2 · LODGING IT WITH HIM, IN PERSON ═══════════════════════════════════════════════

    /// <summary>
    /// <b>THE WHOLE SCENE AT A TABLE.</b> The hull goes at a berth with the captain ashore; he never walks to
    /// a machine; a representative finds him, offers to take it before he pitches, and the SAME three presses
    /// happen on his card. The claim is lodged, still pays nothing, and the money arrives at the next
    /// meeting — the same number the machine would have quoted, because it is the same function quoting it.
    ///
    /// <para>Every step goes through the game's own doors: <c>HisPitchGoesUp</c> is his meeting,
    /// <c>LodgeItWithHim</c> is the button on his card, and the rows are <c>TheClaimAsks</c>' own.</para>
    /// </summary>
    [Fact]
    public void THE_CASTAWAY_TakesTheRepsOfferAndTheThreePressesHappenAtHisTable()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        double now = (double)Read(map, "SimTime")!;
        PirateInsurance policy = NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, now);
        Set(map, "_insurance", policy);
        int purse = (int)Read(map, "_credits")!;

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);

        var wire = (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;
        NewsWire.NewsEvent receipt = wire.First(e => e.Kind == NewsWire.NewsEventKind.HullLostAtABerth);

        // ── HE OFFERS, AND THE COUNTER IS NOT UP YET. The line is on the card; nothing has been pressed.
        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!, "he had a loss on his wire and said nothing.");
        Assert.Null(Read(map, "TheDeskSays"));
        Assert.Null(Read(map, "_claimDesk"));

        // ── ACCEPTED. The counter opens on HIS card and raises no card of its own — a ViewObject over a man
        // standing at your table is the stacked card #777 named.
        Invoke(map, "LodgeItWithHim");
        Assert.Equal(NebulaClaims.OnApproach, (string?)Read(map, "TheDeskSays"));
        Assert.Null(Read(map, "_viewObject"));
        Assert.False((bool)Read(map, "TheClaimDeskIsUp")!, "the kiosk's card claimed a counter it is not hosting.");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he is still offering a form you are filling in.");

        // ── THE THREE PRESSES, on his card, off the same rows the machine deals.
        Assert.Equal(0, PressesTaken(map));
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Hull, "AURORA QUEEN", "AURORA QUEEN"));
        Assert.Equal(0, PressesTaken(map));       // out of order, and refused exactly as at a wall
        PressTheWholeCounter(map);

        // ── LODGED. His card now carries the machine's receipt, and the purse has not moved. And still no
        // ViewObject: three presses at his elbow raise no console card over him, which is the half of the
        // #777 law a check taken before the first press could not see.
        Assert.Null(Read(map, "_viewObject"));
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Assert.Equal(NebulaClaims.LodgedLine, (string?)Read(map, "TheDeskSays"));
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.Equal(purse, (int)Read(map, "_credits")!);
        Assert.Equal(receipt.Subject, (string?)Read(map, "_lodgingOfferedFor"));

        object owed = Read(map, "_claimOwed") ?? throw new InvalidOperationException("nothing was lodged.");
        int payout = (int)Get(owed, "PayoutCr")!;
        Assert.Equal(InsuranceRule.HullClaimPayoutCr(policy, (double)Read(map, "SimTime")!), payout);
        Assert.True(payout > 0, "a Premium policy's claim is worth nothing — the guard cannot fail.");

        // ── AND THE MONEY ARRIVES AT THE NEXT MEETING, not at this one. He watched you fill it in; he did
        // not open the till.
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
        Assert.Equal(NebulaClaims.RepAtThePayout, (string?)Read(map, "_repSaid"));
        Assert.Null(Read(map, "_claimOwed"));

        // …and he does not then offer to file the hull he has just paid for.
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "the offer came back on a hull already paid for.");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
    }

    /// <summary>
    /// <b>THE LINE IS SAID ONCE PER LOSS.</b> He offers at the meeting where the wire is fresh; the captain
    /// walks away without answering; and at the next meeting he does not ask again. The other half is the
    /// same world with the offer TAKEN, which is the guard above — so this one cannot be green because he
    /// never offers at all.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsMadeOncePerLossAndNotAgainAtTheNextMeeting()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();

        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!);

        Invoke(map, "CloseTheRepsCard");           // he is left standing there with the form in his hand
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he asked twice about one hull.");
        Assert.Null(Read(map, "_claimDesk"));
    }

    /// <summary>
    /// <b>AND A CLAIM LODGED AT A MACHINE SPENDS HIS OFFER TOO.</b> The two hosts file the same form, so a
    /// salesman who offered to take a hull already lodged at a wall would be the firm not knowing its own
    /// paperwork — and, since the payout is per claim, a purse that fills by walking to a bar.
    /// </summary>
    [Fact]
    public void A_CLAIM_LodgedAtTheMachineIsNotSomethingHeThenOffersToFile()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(map);
        LodgeAWholeClaim(map);
        Invoke(map, "CloseViewObject");

        Invoke(map, "HisPitchGoesUp");             // paid here, so nothing is owed at the meeting after
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he offered to file a hull already filed and paid for.");
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
    }

    /// <summary>
    /// <b>ONE FORM, TWO HOSTS.</b> Two presses at a machine on the concourse, then a man at a table — and it
    /// is the SAME form: the counter picks up where it was left, the third press lands at his elbow, and one
    /// claim comes out of it. Two implementations would give the captain two counters and two claims here.
    /// </summary>
    [Fact]
    public void A_FORM_BegunAtAMachineIsTheFormHeIsWatchingYouFinish()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(map);
        Invoke(map, "ViewNearbyObject");

        Press(map, TheRows(map)[0]);                                                   // the policy
        Press(map, TheRows(map).Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!));  // her name
        Assert.Equal(2, PressesTaken(map));

        Invoke(map, "CloseViewObject");
        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "LodgeItWithHim");

        // Not restarted. He is holding the form with two presses already on it.
        Assert.Equal(2, PressesTaken(map));
        Assert.Equal(NebulaClaims.OnApproach, (string?)Read(map, "TheDeskSays"));

        Press(map, TheRows(map)[0]);                                                   // the wire entry
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.NotNull(Read(map, "_claimOwed"));
    }

    /// <summary>
    /// <b>THE SAME NUMBER, THE SAME COUNTER, THE SAME NEXT MEETING.</b> Two identical worlds diverging only
    /// in WHERE the third press landed. Everything downstream of it is compared field for field, because
    /// "shared by construction" is a claim about behaviour and this is the only way to ask it.
    /// </summary>
    [Fact]
    public void THE_SAME_NumberAndTheSameNextMeetingWhicheverHostTookThePress()
    {
        Pages.Map atAWall = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(atAWall);
        LodgeAWholeClaim(atAWall);
        Invoke(atAWall, "CloseViewObject");

        Pages.Map atATable = TheCastawayWithALossOnTheWire();
        Invoke(atATable, "HisPitchGoesUp");
        Invoke(atATable, "LodgeItWithHim");
        PressTheWholeCounter(atATable);

        Assert.Equal((int)Read(atAWall, "_claimsLodged")!, (int)Read(atATable, "_claimsLodged")!);
        Assert.Equal(
            (int)Get(Read(atAWall, "_claimOwed")!, "PayoutCr")!,
            (int)Get(Read(atATable, "_claimOwed")!, "PayoutCr")!);
        Assert.Equal(
            (string)Get(Read(atAWall, "_claimOwed")!, "HullName")!,
            (string)Get(Read(atATable, "_claimOwed")!, "HullName")!);

        int wallPurse = (int)Read(atAWall, "_credits")!;
        int tablePurse = (int)Read(atATable, "_credits")!;
        Invoke(atAWall, "HisPitchGoesUp");
        Invoke(atATable, "CloseTheRepsCard");
        Invoke(atATable, "HisPitchGoesUp");
        Assert.Equal((int)Read(atAWall, "_credits")! - wallPurse, (int)Read(atATable, "_credits")! - tablePurse);
        Assert.True((int)Read(atATable, "_credits")! - tablePurse > 0, "neither world paid — the guard cannot fail.");
        Assert.Equal((string?)Read(atAWall, "_repSaid"), (string?)Read(atATable, "_repSaid"));
    }

    /// <summary>
    /// <b>THE UNDERWRITER ON THE REGOLITH MAKES THE SAME OFFER.</b> Brem Kolt is the same firm and takes the
    /// same form — the offer rule asks nothing about WHICH of them is at the table, because a rule about
    /// that would be the mirrored constant said about a person. He does not pay: the money arrives when a
    /// representative next finds you, and that is Harlan Fess's meeting.
    /// </summary>
    [Fact]
    public void KOLT_OffersTheSameFormAndTheMoneyStillArrivesAtFesssMeeting()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();

        Invoke(map, "KoltsPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!, "the underwriter read the wire and said nothing.");
        Invoke(map, "LodgeItWithHim");
        PressTheWholeCounter(map);

        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        int purse = (int)Read(map, "_credits")!;
        Invoke(map, "CloseTheHardcasesCard");
        Assert.Equal(purse, (int)Read(map, "_credits")!);   // he closed his case and paid nobody

        Invoke(map, "HisPitchGoesUp");
        Assert.True((int)Read(map, "_credits")! > purse, "Fess never paid what Kolt took.");
        Assert.Equal(NebulaClaims.RepAtThePayout, (string?)Read(map, "_repSaid"));
    }

    /// <summary>
    /// <b>A FINISHED FORM GOES WITH THE CONVERSATION; A HALF-FILLED ONE DOES NOT.</b> The counter is the
    /// page's state and not the fixture's, so walking away mid-form and coming back — to him or to a wall —
    /// finds the same two presses. A lodged one is business the firm has closed.
    /// </summary>
    [Fact]
    public void A_HALF_FilledFormOutlivesTheMeetingAndALodgedOneDoesNot()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        Invoke(map, "HisPitchGoesUp");
        Invoke(map, "LodgeItWithHim");
        Press(map, TheRows(map)[0]);
        Assert.Equal(1, PressesTaken(map));

        Invoke(map, "CloseTheRepsCard");
        Assert.Equal(1, PressesTaken(map));                 // he keeps the form; so does the machine

        Invoke(map, "HisPitchGoesUp");
        PressTheWholeCounter(map);
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Invoke(map, "CloseTheRepsCard");
        Assert.Null(Read(map, "_claimDesk"));
    }

    /// <summary>
    /// <b>AND THE OFFER IS DRAWN BEFORE THE PITCH, ON BOTH CARDS.</b> Read off the composed page, because
    /// the sim can say the offer stands and the markup can still put it under the buttons that dismiss the
    /// card — the third named bug class exactly, and #761's telling law failing without a symptom a test of
    /// the state could see.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsDrawnAboveEveryLineTheRepsOpenWith()
    {
        string page = MapMarkup.Text;

        int fessOffer = page.IndexOf("NebulaClaims.LodgeWithMe", StringComparison.Ordinal);
        int fessPitch = page.IndexOf("@pitch.Line", StringComparison.Ordinal);
        Assert.True(fessOffer >= 0 && fessPitch > fessOffer,
            "Harlan Fess pitches before he mentions the hull you lost.");

        int koltOffer = page.IndexOf("NebulaClaims.LodgeWithMe", fessOffer + 1, StringComparison.Ordinal);
        int koltOpener = page.IndexOf("HardcaseRep.Opener", StringComparison.Ordinal);
        Assert.True(koltOffer >= 0 && koltOpener > koltOffer,
            "Brem Kolt opens on hazard before he mentions the hull you lost.");

        // …and all three cards reach the counter through the page's OWN members — read off Map.razor itself,
        // where the invocations are, because the composed text above has spliced each surface in over them.
        // This is what makes the two hosts one implementation rather than two that agree today.
        string invocations = File.ReadAllText(MapMarkup.PagePath);
        foreach (string door in new[] { "PressTheClaim=\"@PressTheClaim\"", "TheClaimAsks=\"@TheClaimAsks\"" })
        {
            Assert.Equal(3, CountOf(invocations, door));   // the kiosk's card, Fess's card, Kolt's card
        }

        // …and all three then draw the rows by CALLING those same members, rather than one of them growing a
        // list of its own that happens to look the same.
        Assert.Equal(3, CountOf(page, "in TheClaimAsks())"));
        Assert.Equal(3, CountOf(page, "PressTheClaim("));
    }

    private static int CountOf(string text, string needle)
    {
        int found = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal);
             at >= 0;
             at = text.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }

    /// <summary>A captain who has lost her at a berth, is insured, and has not been near a machine: the
    /// world every slice-2 guard above starts in.</summary>
    private static Pages.Map TheCastawayWithALossOnTheWire()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        Set(map, "_insurance",
            NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, (double)Read(map, "SimTime")!));

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);
        Assert.Contains(
            (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!,
            e => e.Kind == NewsWire.NewsEventKind.HullLostAtABerth);
        return map;
    }

    /// <summary>Every press the counter still wants, correct and in order, off its own rows — wherever it is
    /// standing. One helper for both hosts, because there is one counter.</summary>
    private static void PressTheWholeCounter(Pages.Map map)
    {
        while (NebulaClaims.NextPress(PressesTaken(map)) is { } press)
        {
            IReadOnlyList<NebulaClaims.Ask> rows = TheRows(map);
            Assert.NotEmpty(rows);
            NebulaClaims.Ask take = press == NebulaClaims.Press.Hull
                ? rows.Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!)
                : rows[0];
            Press(map, take);
        }
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where the captain is standing, which is the whole of what the presence law asks.</summary>
    public enum Posture
    {
        /// <summary>On her own deck, in the dark, with nothing between him and the controls.</summary>
        Aboard,

        /// <summary>Walking a moon.</summary>
        OnASurface,

        /// <summary>Away in the boat.</summary>
        AwayInTheBoat,

        /// <summary>Past the tube, on somebody's concourse, with her clamped to their collar.</summary>
        AshorePastTheTube,
    }

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>A live component over the shipping scenario, walking her own deck — the same boot the berth
    /// scuttle's own guards use, so the two files are asking one game.</summary>
    private static Pages.Map Boot()
    {
        var map = new Pages.Map();
        new ARendererThatDrawsNothing().Attach(map);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        Set(map, "_renderer", new CanvasRenderer("claims-canvas"));
        var pen = new APenThatDrawsNothing();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_shuttleView", new ShuttleFlightView(pen));
        Set(map, "_deckMode", true);
        Set(map, "Warp", 1);
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>Put the captain where the posture says, through the same doors the game uses to get him
    /// there — a real excursion, a real shuttle launch, a real walk past the tube.</summary>
    private static void PutHimIn(Pages.Map map, Posture posture)
    {
        switch (posture)
        {
            case Posture.Aboard:
                Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);
                return;

            case Posture.OnASurface:
                PutHimOnAGround(map);
                break;

            case Posture.AwayInTheBoat:
                PutHimInTheShuttle(map);
                break;

            default:
                ClampAtThePort(map);
                WalkHimAshore(map);
                break;
        }

        Assert.False((bool)Invoke(map, "TheMasterIsAboardHer")!,
                     $"{posture} did not actually take the captain off his ship.");
    }

    /// <summary>Tie her up at <see cref="Port"/> through the clamp the game uses, and assert the port has an
    /// interior to walk — a port with no concourse has no machine on it and no gangway to be past.</summary>
    private static void ClampAtThePort(Pages.Map map)
    {
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == Port);
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no concourse.");

        double simTime = (double)Read(map, "SimTime")!;
        Invoke(map, "ClampOntoHaven", dock, sky.Position(Port, simTime), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
    }

    /// <summary>Past the tube, through the page's own <c>RefreshAshore</c> rather than by writing the flag,
    /// so a world where the walk cannot be made goes red instead of quietly proving nothing.</summary>
    private static void WalkHimAshore(Pages.Map map)
    {
        Set(map, "_avatarY", 40.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
    }

    /// <summary>The ground the excursion arms of this file put the captain down on. Luna, because it is a
    /// real moon of a real planet in the shipping scenario and therefore has a harbour that serves it —
    /// which is what the waiting writ is filed against.</summary>
    private const string Ground = "luna";

    /// <summary>On a ground, built the way the frame guards build one: the page's own excursion record and
    /// its own <c>RebuildSurfaceDeck</c>, so the deck under his feet is a deck the game made.</summary>
    private static void PutHimOnAGround(Pages.Map map)
    {
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Ground, Ground, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");
        Assert.NotNull(Read(map, "_surface"));
    }

    /// <summary>Which ground the excursion put him on.</summary>
    private static string TheGroundHeIsOn(Pages.Map map)
    {
        object surface = Read(map, "_surface") ?? throw new InvalidOperationException("he is not on a ground.");
        return (string)Get(Get(Get(surface, "Stop")!, "Body")!, "Id")!;
    }

    /// <summary>The captain away in the boat, launched off the page's own launcher at a live, selected,
    /// authorized target — the berth scuttle's guards' own recipe.</summary>
    private static void PutHimInTheShuttle(Pages.Map map)
    {
        Set(map, "_deckMode", false);

        var eph = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        NpcShip hull = TrafficSchedule.Generate(eph, seed: 42, count: 1)[0];
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden | BindingFlags.Public)!;
        object prey = Activator.CreateInstance(stateType, nonPublic: true)!;
        stateType.GetField("Ship", Hidden)!.SetValue(prey, hull);
        stateType.GetField("State", Hidden)!.SetValue(prey, (ShipState)Read(map, "_ship")!);
        stateType.GetField("Active", Hidden)!.SetValue(prey, true);
        stateType.GetField("CurrentlyObserved", Hidden)!.SetValue(prey, true);

        Array roster = Array.CreateInstance(stateType, 1);
        roster.SetValue(prey, 0);
        Set(map, "_npcStates", roster);
        Set(map, "_selectedTargetId", hull.Id);
        Set(map, "_plunderAuthorizedTargetId", hull.Id);

        Invoke(map, "LaunchShuttleRun", prey);
        Assert.NotNull(Read(map, "_shuttleRun"));
    }

    /// <summary>A collector sitting exactly where she is, already fitted out. On any frame she is allowed to
    /// close she has closed — which is what makes the held arms of the presence law mean something.</summary>
    private static void PutACollectorOnTopOfHer(Pages.Map map, string callsign)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        ((IList)Read(map, "_hunters")!).Add(new HunterState(
            Id: callsign.ToLowerInvariant(),
            Callsign: callsign,
            OriginBodyId: Port,
            SpawnedAtSimTime: (double)Read(map, "SimTime")!,
            ActivationSimTime: 0,
            State: ship,
            CaughtPlayer: false,
            BrokenOff: false));
    }

    /// <summary>The captain's word, the crew's second key, both keys together — the panel's own three verbs,
    /// in the order the panel makes the player press them. Nothing writes the clock.</summary>
    private static void ArmHerCharges(Pages.Map map)
    {
        Invoke(map, "OpenShipScuttlePanel");
        Invoke(map, "GiveTheWordAgainstHer");
        Invoke(map, "AskTheCrewForTheSecondKey");
        Invoke(map, "TurnBothKeys");
        Assert.Equal(Scuttle.OverloadSeconds, (double)Read(map, "_shipChargesSeconds")!);
        Invoke(map, "CloseShipScuttlePanel");
    }

    // ── THE COUNTER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Stand at the machine: the console the port built, found on the deck plan and walked to.</summary>
    private static void WalkToTheKiosk(Pages.Map map)
    {
        var plan = (DeckPlan)Read(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot kiosk = plan.Consoles.First(c => c.Label == NebulaClaims.KioskPlate);

        Set(map, "_avatarX", (double)kiosk.X);
        Set(map, "_avatarY", (double)kiosk.Y);
        Set(map, "_viewObject", null);
    }

    private static IReadOnlyList<NebulaClaims.Ask> TheRows(Pages.Map map) =>
        (IReadOnlyList<NebulaClaims.Ask>)Invoke(map, "TheClaimAsks")!;

    private static void Press(Pages.Map map, NebulaClaims.Ask ask, PirateInsurance withPolicy = default) =>
        Invoke(map, "PressTheClaim", ask);

    private static int PressesTaken(Pages.Map map) =>
        Read(map, "_claimDesk") is { } desk ? (int)Get(desk, "Presses")! : 0;

    /// <summary>Three presses, all correct, through the counter's own rows — the whole lodging, for the
    /// guards that are about what happens AFTER one.</summary>
    private static void LodgeAWholeClaim(Pages.Map map)
    {
        Invoke(map, "ViewNearbyObject");
        Assert.True((bool)Read(map, "TheClaimDeskIsUp")!);

        for (int press = 0; press < NebulaClaims.Presses; press++)
        {
            IReadOnlyList<NebulaClaims.Ask> rows = TheRows(map);
            Assert.NotEmpty(rows);
            NebulaClaims.Ask take = press == 1
                ? rows.Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!)
                : rows[0];
            Press(map, take);
            Assert.Equal(press + 1, PressesTaken(map));
        }
    }

    /// <summary>The pending-writ row on the captain's ledger, or null when there is none.</summary>
    private static object? TheWritRow(Pages.Map map) => Invoke(map, "PendingWritTip");

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    private static void RunUntilSheGoes(Pages.Map map)
    {
        for (int i = 0; i < 4000; i++)
        {
            Frame(map);
            if (Read(map, "_shipChargesSeconds") is null)
            {
                return;
            }
        }

        throw new InvalidOperationException("her ninety-second overload never reached zero in four hundred seconds.");
    }

    private static void RunFrames(Pages.Map map, double seconds)
    {
        for (int i = 0; i < (int)(seconds / FrameSeconds); i++)
        {
            Frame(map);
        }
    }

    private const double FrameSeconds = 0.1;

    private static void Frame(Pages.Map map)
    {
        double at = Convert.ToDouble(Read(map, "_lastTimestampMs") ?? 0.0) + (FrameSeconds * 1000);
        try
        {
            Invoke(map, "OnTick", at);
        }
        catch (PlatformNotSupportedException)
        {
            // The canvas flush — the one line of the frame that crosses into JavaScript.
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new InvalidOperationException("could not find the repository root from the test assembly.");
    }

    private static object? Get(object owner, string name) =>
        owner.GetType().GetProperty(name, Hidden)?.GetValue(owner)
        ?? owner.GetType().GetField(name, Hidden)?.GetValue(owner);

    private static object? Read(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map)
        ?? typeof(Pages.Map).GetProperty(name, Hidden)?.GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        FieldInfo? field = typeof(Pages.Map).GetField(name, Hidden);
        if (field is not null)
        {
            field.SetValue(map, value);
            return;
        }
        typeof(Pages.Map).GetProperty(name, Hidden)!.SetValue(map, value);
    }

    private static object? Invoke(Pages.Map map, string name, params object?[] args)
    {
        try
        {
            return typeof(Pages.Map).GetMethod(name, Hidden)!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>A renderer that records nothing and crosses into no JavaScript.</summary>
    private sealed class APenThatDrawsNothing : IRenderer
    {
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) { }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor color,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) { }

        public int RegisterImage(string url) => 0;

        public void DrawImage(int imageId, float x, float y, float w, float h, float alpha = 1f) { }

        public void DrawImageSlice(int imageId, float sx, float sy, float sw, float sh,
            float dx, float dy, float dw, float dh, float alpha = 1f) { }

        public void EndFrame() { }
    }

#pragma warning disable BL0006 // the framework's own seam: a component needs a renderer to have a dispatcher
    private sealed class ARendererThatDrawsNothing : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        public ARendererThatDrawsNothing()
            : base(NoServices.Instance, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override System.Threading.Tasks.Task UpdateDisplayAsync(
            in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch) =>
            System.Threading.Tasks.Task.CompletedTask;

        private sealed class RightHere : Dispatcher
        {
            public override bool CheckAccess() => true;

            public override System.Threading.Tasks.Task InvokeAsync(Action workItem)
            {
                workItem();
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public override System.Threading.Tasks.Task InvokeAsync(Func<System.Threading.Tasks.Task> workItem) =>
                workItem();

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem) =>
                System.Threading.Tasks.Task.FromResult(workItem());

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(
                Func<System.Threading.Tasks.Task<TResult>> workItem) => workItem();
        }

        private sealed class NoServices : IServiceProvider
        {
            public static readonly NoServices Instance = new();

            public object? GetService(Type serviceType) => null;
        }
    }
#pragma warning restore BL0006
}
