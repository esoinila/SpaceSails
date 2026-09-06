using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #525 · <b>BLOWING HER AT A STATION BERTH IS ITS OWN SCENE, AND ITS OWN CRIME — DRIVEN.</b>
///
/// <para><c>SheGoesWhetherHeIsAboardOrNotTests</c> proved the two rows of the issue's own outcome table in
/// the dark. This is the third case that table never had: she is clamped to somebody's collar, the ninety
/// seconds are not private, and the captain who walks away from it is standing on the ground of the people
/// he did it to.</para>
///
/// <para><b>Everything here is driven the same way and for the same reason.</b> The charges are armed through
/// the panel's own three verbs, the clock is the one <c>OnTick</c> keeps, and nothing writes
/// <c>_shipChargesSeconds</c> or calls <c>SheGoes</c> by hand. Reading the source is how this issue got three
/// different answers about whether the castaway was reachable at all.</para>
///
/// <para><b>And every guard is asked in a world that could answer the other way.</b> The berth is a REAL
/// clamp at a real port with a real roster (twelve slots at a great port, so there are neighbours to move);
/// a hunter is on her, so "nobody breaks off" is a thing this world can fail; the wire starts empty and the
/// port's book starts cold, so both of them moving is a fact and not a default. A guard handed a world that
/// cannot tell pass from fail is this ground's fifth named bug class, and it is the one this file was most
/// at risk of being.</para>
///
/// <para><b>Nothing here asserts anything about insurance.</b> The owner's question on #525 is open and this
/// lane does not answer it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SheGoesAtTheBerthTests
{

    // ══ 1 · ARMING AT A BERTH IS PUBLIC ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE KEYS TURN AND THE HARBOUR FINDS OUT.</b> Three things, in one breath, and each of them is a
    /// thing this world would otherwise not do.
    ///
    /// <para>The port's own line is on the pulse, naming the slot the roster actually tied her up in. The
    /// neighbouring collars are on the record with a reason on them, and his own is not. And the evening's
    /// schedule has been replaced: nobody is coming out of the back, everybody still seated is going, and
    /// running the frames walks them out through leaves the captain's own TRY is refused at.</para>
    /// </summary>
    [Fact]
    public void ARMING_AtABerthIsHeardOnTheWholeConcourse()
    {
        Pages.Map map = Boot();
        (int slot, int berths) = ClampAtThePort(map);

        ArmHerCharges(map);

        // ── THE PORT'S OWN VOICE, once, naming the slot she is actually in.
        object pulse = Read(map, "_pulse")!;
        Assert.Equal(BerthScuttle.PaCall(BerthScuttle.BerthNumber(slot)), (string?)Get(pulse, "Message"));
        Assert.True(((PulseRank)Get(pulse, "Rank")!).IsPlotSignificant(),
            "the loudest thing that has ever happened at a berth may not lose the slot to a fuel price.");

        // ── THE ROSTER, with the reason on it.
        object collar = Read(map, "_collarCleared")
            ?? throw new InvalidOperationException("the harbour did not file the collar at all.");
        Assert.Equal(Port, (string)Get(collar, "HavenId")!);
        Assert.Equal(slot, (int)Get(collar, "Berth")!);
        Assert.Equal(BerthScuttle.Why.DeclaredOverload, Get(collar, "Reason"));

        var neighbours = (IReadOnlyList<int>)Get(collar, "Neighbours")!;
        Assert.Equal(BerthScuttle.CollarCleared(slot, berths), neighbours);
        Assert.NotEmpty(neighbours);                       // a twelve-berth ring HAS neighbours to move
        Assert.DoesNotContain(slot, neighbours);           // …and he is still tied up in his

        // ── THE ROOM. Nobody comes out of the back into a collar being cleared, and everybody still in a
        // chair is on their feet — through #731's own egress, which is why there is no second walk planner.
        Assert.Empty((IEnumerable)Read(map, "_barComing")!);
        var going = (IReadOnlyList<Egress.Move>)Read(map, "_barGoing")!;
        Assert.NotEmpty(going);

        RunFrames(map, 20);

        var left = (IEnumerable<string>)Read(map, "_barLeft")!;
        Assert.NotEmpty(left);
        Assert.All(left, id => Assert.Contains(going, m => m.Plate == id));
    }

    /// <summary>
    /// <b>THE ABORT STOPS THE CLOCK AND NOT THE PORT.</b> The keys come back out inside the recall window,
    /// she is a ship again — and the neighbours stay reassigned, because a harbour does not un-hear a
    /// declared overload and a consequence a change of mind undid would make the whole scene free.
    /// </summary>
    [Fact]
    public void THE_AbortGivesHerBackAndDoesNotGiveThePortBack()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        ArmHerCharges(map);

        object filed = Read(map, "_collarCleared")!;
        Invoke(map, "BackTheKeysOut");

        Assert.Null(Read(map, "_shipChargesSeconds"));
        Assert.Same(filed, Read(map, "_collarCleared"));

        // …and it survives the file, which is the same statement said to a reload.
        var saved = (Vault)Invoke(map, "BuildVault", "", "")!;
        ClearedCollarRecord row = saved.Progress?.CollarCleared
            ?? throw new InvalidOperationException("the cleared collar never reached the vault.");
        Assert.Equal(Port, row.HavenId);
        Assert.Equal(BerthScuttle.Why.DeclaredOverload.ToString(), row.Reason);
    }

    // ══ 2 · SHE GOES AT THE BERTH ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE WHOLE SCENE, END TO END.</b> Armed at the collar, the captain walks the tube ashore, and the
    /// frames run until the ninety seconds are spent.
    ///
    /// <para>He lives — being past the tube on a station's floor is not being aboard her, which is this
    /// lane's one change to <c>CaptainWasAboardHer</c>. And then everything the dark version of this ending
    /// does NOT do: the wire carries it at once, the port's operator remembers it at the top of their meter,
    /// the pursuer who was on her is still on him, and the card carries the port's second line under the
    /// ending it already tells.</para>
    /// </summary>
    [Fact]
    public void SHE_GoesAtTheBerthAndTheStationFilesItBeforeHeIsAcrossTheConcourse()
    {
        Pages.Map map = Boot();
        (int slot, _) = ClampAtThePort(map);
        ArmHerCharges(map);
        WalkHimAshore(map);

        // The world can answer either way: nothing on the wire, nothing on the port's book.
        Assert.Empty((IEnumerable)Read(map, "_newsEvents")!);
        Assert.Equal(0, IllegalHeat.HeatAtSite((ContactLedger)Read(map, "_contacts")!, Port));

        bool askedTheVault = RunUntilSheGoes(map);

        // ── HE LIVED. A castaway, not a death: the berth scene's own signal.
        Assert.Null(Read(map, "_busted"));
        object card = Read(map, "_shipEpitaph")
            ?? throw new InvalidOperationException("the clock ran out and no card was ever raised.");

        // ── …AND THE CARD CARRIES THE PORT'S SECOND LINE, under the three it already told.
        Assert.Equal(ShipScuttle.CastawayLine, (string)Get(card, "Went")!);
        Assert.Equal(BerthScuttle.ThePortHasYourName, (string?)Get(card, "Port"));
        Assert.Contains("castaway.Port", TheCastawayMarkup(), StringComparison.Ordinal);

        // ── THE WIRE, AT ONCE. The inverse of #1126's guard: there the whole point was that nothing ever
        // forms, so there is nothing to delete. Here something is filed before he is across the concourse,
        // and it is a real headline rather than static.
        var wire = (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;
        Assert.NotEmpty(wire);
        Assert.All(wire, e => Assert.NotEqual("Static on the wire.", NewsWire.Headline(e)));

        // The ledger is newest-first (PushNewsEvent inserts at the head), so the harbour's filing — the last
        // thing pushed before he was across the concourse — is the entry at the top of the wire. Taken by
        // position rather than searched for by kind, because "it is the freshest thing on the wire" is
        // itself part of the claim: this is filed AT ONCE, not eventually.
        NewsWire.NewsEvent filed = wire[0];

        // …AND IT IS THE HARBOUR'S OWN KIND, WITH THE BRACES OFF THE RECORD. #1138 left this entry riding
        // HunterDispatched under a `FABLE: line needed`; the canon pass of 2026-09-06 wrote the line, so the
        // beat has its own kind and the two names in the sentence are the two the world can be asked for —
        // the berth number the PA announced (not the roster's zero-based index) and this port.
        Assert.Equal(NewsWire.NewsEventKind.HullLostAtABerth, filed.Kind);
        Assert.Equal(BerthScuttle.BerthNumber(slot).ToString(System.Globalization.CultureInfo.InvariantCulture),
            filed.Subject);
        string port = (string)Invoke(map, "BodyName", Port)!;
        Assert.Equal(port, filed.Detail);
        Assert.Contains($"Berth {BerthScuttle.BerthNumber(slot)}", NewsWire.Headline(filed), StringComparison.Ordinal);
        Assert.Contains(port, NewsWire.Headline(filed), StringComparison.Ordinal);

        // The PA and the wire agree about which slot this was — a harbour that announced berth 9 and then
        // filed berth 3 is this ground's named class of the sim saying one thing and a sentence another.
        Assert.StartsWith($"Berth {BerthScuttle.BerthNumber(slot)}",
            BerthScuttle.PaCall(BerthScuttle.BerthNumber(slot)), StringComparison.Ordinal);

        // ── THE PORT'S OPERATOR, AT THE CEILING BAND — the meter's own top, and what it buys is the round
        // starting every watch at the end of its patience. That IS the fugitive on foot.
        var book = (ContactLedger)Read(map, "_contacts")!;
        Assert.Equal(IllegalHeat.Ceiling, IllegalHeat.HeatAtSite(book, Port));
        Assert.True(BerthScuttle.AFugitiveOnTheirFloor(IllegalHeat.HeatAtSite(book, Port)));

        Assert.True(askedTheVault, "the frame that ended her did not ask the vault for anything.");
    }

    /// <summary>THE MARKER IS GONE, AND THE BORROWED KIND WITH IT. #1138 shipped this beat with a
    /// <c>// FABLE: line needed</c> over a <see cref="NewsWire.NewsEventKind.HunterDispatched"/> push,
    /// honestly flagged as a stand-in. The canon pass of 2026-09-06 wrote the line, so both the marker and
    /// the borrowed kind must be out of the file — deleting the marker while the entry still rode somebody
    /// else's headline would be the flag removed and the problem kept.</summary>
    [Fact]
    public void THE_LINE_THAT_WAS_NEEDED_IsWrittenAndTheStandInIsGone()
    {
        string source = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.BerthScuttle.cs"));

        Assert.DoesNotContain("FABLE: line needed", source, StringComparison.Ordinal);

        // The CODE, with every comment taken off first — the header note explains what the stand-in was and
        // names it, and a guard that read prose would go red on the explanation instead of on the bug.
        string code = string.Join('\n', source.Split('\n')
            .Select(l => l.Split("//", StringSplitOptions.None)[0]));
        Assert.DoesNotContain("NewsWire.NewsEventKind.HunterDispatched", code, StringComparison.Ordinal);
        Assert.Contains("NewsWire.NewsEventKind.HullLostAtABerth", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>NOBODY BREAKS OFF AT A BERTH — AND EVERYBODY DOES IN THE DARK.</b> One guard, both ways, because a
    /// suppression is only a suppression against the thing it suppresses.
    ///
    /// <para>#1090's deterrent rests on one sentence — <i>there is no prize in a ship that has stopped
    /// existing</i> — and at a berth that sentence is false: the prize was never only the hull. So the
    /// pursuer stays on the man who did it on their concourse.</para>
    ///
    /// <para><b>The ending is invoked rather than driven here, and the reason is written down because it is a
    /// real limit of the drive.</b> A pursuer put on the roster at a berth is removed by the world long
    /// before ninety seconds are up — a hull tied up at a haven is exactly where a collector loses the scent,
    /// which is the game working correctly and would make this guard green about an empty list. So the two
    /// worlds are built, the roster is loaded, and <c>SheGoesWithoutHim</c> is called on the frame it would be
    /// called on. Everything else about this scene is proved on the full tick above.</para>
    /// </summary>
    [Fact]
    public void THE_PursuersLetGoInTheDarkAndDoNotLetGoAtABerth()
    {
        Assert.False(BrokeOff(atABerth: true), "the pursuers let go of a man on their own concourse.");
        Assert.True(BrokeOff(atABerth: false),
            "#1090's deterrent stopped firing in the dark, which is the ending it was built for.");
    }

    /// <summary>Both halves of the guard above, built the same way and differing in the one fact that is the
    /// whole scene: whether she is clamped to somebody's collar.</summary>
    private static bool BrokeOff(bool atABerth)
    {
        Pages.Map map = Boot();
        if (atABerth)
        {
            ClampAtThePort(map);
            WalkHimAshore(map);
        }
        else
        {
            PutHimInTheShuttle(map);
        }

        ArmHerCharges(map);
        PutAHunterOnHer(map, "GRIMHOLD");
        Assert.False((bool)Get(((IList)Read(map, "_hunters")!)[0]!, "BrokenOff")!);

        Invoke(map, "SheGoesWithoutHim");

        var hunters = (IList)Read(map, "_hunters")!;
        Assert.NotEmpty(hunters);
        return (bool)Get(hunters[0]!, "BrokenOff")!;
    }

    // ══ 3 · AND IN OPEN SPACE, NONE OF IT ════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>A SCUTTLE IN THE DARK IS UNCHANGED.</b> The same drive, at no berth at all: no PA, no collar filed,
    /// nothing on the wire, no heat on anybody, the pursuer lets go exactly as #1090 built him to, and the
    /// castaway card is the three-caption card it has always been.
    ///
    /// <para>This is the guard that stands between this lane and every other way a captain can end his ship,
    /// and it is deliberately the strictest one in the file: it asserts what did NOT happen.</para>
    /// </summary>
    [Fact]
    public void IN_OpenSpaceNothingOfThisSceneHappensAtAll()
    {
        Pages.Map map = Boot();
        Assert.Null(Read(map, "_dockedHavenId"));

        ArmHerCharges(map);

        Assert.Null(Read(map, "_collarCleared"));
        Assert.NotEqual(
            BerthScuttle.PaCall(BerthScuttle.BerthNumber(0)),
            (string?)Get(Read(map, "_pulse")!, "Message"));

        // The boat goes late and comes home fast, so it is launched where the sibling file launches it — with
        // the clock nearly spent, or she goes with him aboard and this guard is the deck's ending in disguise.
        RunFrames(map, seconds: 75);
        PutHimInTheShuttle(map);
        RunUntilSheGoes(map);

        Assert.Null(Read(map, "_busted"));
        object card = Read(map, "_shipEpitaph")
            ?? throw new InvalidOperationException("the clock ran out and no card was ever raised.");
        Assert.Null(Get(card, "Port"));

        Assert.Empty((IEnumerable)Read(map, "_newsEvents")!);
        Assert.Equal(0, IllegalHeat.HeatAtSite((ContactLedger)Read(map, "_contacts")!, Port));
        Assert.Null(Read(map, "_collarCleared"));
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component over the shipping scenario, walking her own deck — the posture the charge
    /// panel is reached from, because it is a console on her deck plan and nowhere else. The same boot
    /// <c>SheGoesWhetherHeIsAboardOrNotTests</c> and the claim's own guards use, so all three files are
    /// asking one game — it lives in <see cref="CastawayBench"/> now.</summary>
    private static Pages.Map Boot() => CastawayBench.Boot("berth-canvas");

    /// <summary>
    /// TIE HER UP, through the clamp the game actually uses — <c>ClampOntoHaven</c>, which is what welds the
    /// tube, sets the deck and asks the roster for a slot. Hands back the slot she is in and how many the
    /// port keeps.
    ///
    /// <para>Both facts about the port are ASSERTED rather than assumed: an interior to walk (or there is no
    /// concourse to empty) and more than one berth (or there is nobody to move, and every neighbour guard
    /// below would be green about nothing).</para>
    /// </summary>
    private static (int Slot, int Berths) ClampAtThePort(Pages.Map map)
    {
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == Port);

        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no concourse.");
        int berths = DockRoster.BerthsAt(sky, Port);
        Assert.True(berths > 1, $"{Port} keeps {berths} berth(s) — nothing there can be reassigned.");

        double simTime = (double)Read(map, "SimTime")!;
        Invoke(map, "ClampOntoHaven", dock, sky.Position(Port, simTime), null);

        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        int slot = (int)(Read(map, "_berthSlot") ?? throw new InvalidOperationException("no slot was kept."));
        Assert.InRange(slot, 0, berths - 1);
        return (slot, berths);
    }

    /// <summary>One heat-hunter, flying her own intercept — built the way the roster holds them, off the
    /// player's own state, so nothing about it is a special case.</summary>
    private static void PutAHunterOnHer(Pages.Map map, string callsign)
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
        Assert.True((bool)Read(map, "_shipScuttleWordGiven")!, "the captain's own word did not take.");

        Invoke(map, "AskTheCrewForTheSecondKey");
        Assert.NotEqual(ShipScuttle.SecondKey.Refused,
                        (ShipScuttle.SecondKey)Read(map, "_shipScuttleSecondKey")!);

        Invoke(map, "TurnBothKeys");
        Assert.Equal(Scuttle.OverloadSeconds, (double)Read(map, "_shipChargesSeconds")!);
        Invoke(map, "CloseShipScuttlePanel");
    }

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    private static bool RunUntilSheGoes(Pages.Map map)
    {
        for (int i = 0; i < 4000; i++)
        {
            Frame(map);
            if (Read(map, "_shipChargesSeconds") is null)
            {
                return (bool)Read(map, "_autosaveDirty")!;
            }
        }

        throw new InvalidOperationException(
            "four hundred seconds of frames and her ninety-second overload never reached zero — it reads "
            + $"{Read(map, "_shipChargesSeconds") ?? "null"}. The clock is not being spent in this view.");
    }
}
