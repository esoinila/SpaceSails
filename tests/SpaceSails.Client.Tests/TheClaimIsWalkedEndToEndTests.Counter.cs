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
/// #1151 slice 1 · <b>THE MACHINE ON THE CONCOURSE</b> — the counter itself: the castaway who walks to
/// it, the three presses it refuses and then takes, the square of deck it stands on, and the beam a claims
/// call lights the ship up with.
///
/// <para>Split out of the end-to-end file under #251's method. What this part owns is the KIOSK arm of the
/// scene — everything the captain can do without meeting anybody. The same claim finished at a table is
/// next door in <c>…Rep.cs</c>, and the writ a collector leaves behind is in <c>…Writ.cs</c>; all three
/// are the same partial class over the one bench in <c>…World.cs</c>, so no two of them can be asking
/// different games.</para>
///
/// <para>The section banners below are the ones the scene wrote for itself and are left exactly as they
/// were, numbering included — this file is a move, not an edit.</para>
/// </summary>
public sealed partial class TheClaimIsWalkedEndToEndTests
{
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
}
