using System;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1215 · <b>TWO GLASSES LAND ON A COUNTER, SO THERE HAS TO BE A COUNTER.</b>
///
/// <para><b>What was played.</b> At Selene Gate, ashore, the captain walked west out of THE EARTHRISE BAR,
/// across the concourse and into the glass observation tube, stood there, and the ambient scare handed him
/// the whole hero beat of #429: <i>"Two tumblers land on the counter and the spirit in them is still rocking
/// from whatever the rock just did… The other belongs to somebody who had no particular reason to stay in
/// the room, and stayed in the room."</i> — then the cognac line, then <c>1 tot poured</c> on the Galley's
/// sobriety ledger. There is no counter, no barkeep and no stranger in a glass tube over the Moon. Seen
/// three separate times in one session.</para>
///
/// <para><b>Why it happened.</b> <c>TryBond</c>'s gate was <c>_dockedHavenId is not null &amp;&amp;
/// CurrentKeep is not null</c> — <b>a BERTH, which is not a room</b>. Every other beat that belongs to that
/// floor scopes itself by the floor: the rota's own metabolism, the finder (#417), the walk-in (#973 L5b)
/// and the tail's chair reading (#1062) each take <c>TheDockedBar()</c> and ask <c>InTheBar()</c> of it. The
/// bond is raised from the far end of the game — the end of an ambient scare — so it had no floor in its
/// hand and made do with the berth.</para>
///
/// <para><b>The fix is the SAME predicate and not a second one</b> (<c>TheCaptainIsInTheDockedBar</c>, which
/// is those two calls composed), so a room that moves moves for every beat in it at once. This file asks the
/// law from both sides, because a guard that only watched the tube would be satisfied by a bond that never
/// fired anywhere.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBondNeedsAStrangerAndACounterTests
{
    private const string CanvasId = "bond-room-canvas";

    /// <summary>The bar's own south wall, off the room the game draws — never a coordinate typed here.</summary>
    private static double TheBarsFloorY =>
        HavenInterior.BarBand(Port)?.FloorY
        ?? throw new InvalidOperationException($"{Port} draws no bar band, so there is no room to be in.");

    [Fact]
    public void InTheRoomTheScareStillStandsYouACognac()
    {
        // The anti-vacuous half, and it comes first: if the bond could not fire at all, every refusal below
        // would be green about nothing.
        SpaceSails.Client.Pages.Map map = ABarWithAStrangerInIt();
        StandHim(map, inTheBar: true);

        TheHullShudders(map);

        Assert.Equal(1, (int)Read(map, "_rumTots")!);
        Assert.True(TheStrangerIsOnTheLedger(map), "nobody was made a contact by a drink they stood you.");
    }

    [Fact]
    public void OutOnTheConcourseTheSameScareStandsYouNothing()
    {
        SpaceSails.Client.Pages.Map map = ABarWithAStrangerInIt();
        StandHim(map, inTheBar: false);

        TheHullShudders(map);

        Assert.Equal(0, (int)Read(map, "_rumTots")!);
        Assert.False(TheStrangerIsOnTheLedger(map),
            "a stranger became a contact across a concourse he was never on the same floor as.");
    }

    [Fact]
    public void ButTheSCAREItselfHappensWhereverTheCaptainIsStanding()
    {
        // The other half of the ruling, said out loud: what a captain out in the tube loses is the company,
        // never the fright. TryBond is called at the END of the dread beat, so the beat is untouched — and
        // this is the guard that would go red if a future fix "solved" #1215 by refusing the whole scare off
        // the floor.
        SpaceSails.Client.Pages.Map map = ABarWithAStrangerInIt();
        StandHim(map, inTheBar: false);

        TheHullShudders(map);

        Assert.True((bool)Read(map, "_shudderActive")!, "the hull did not shudder out on the concourse.");
        Assert.Equal(1, (int)Read(map, "_shudderIndex")!);
    }

    // ── The room, and the two places to stand in it ──────────────────────────────────────────────────────

    /// <summary>A live page clamped on at Selene Gate, ashore, with the <c>?bond=1</c> cheat armed — which is
    /// the documented way to reach the hero beat on a watch the rota seated nobody, and the only thing in
    /// this file that is not the shipping path.</summary>
    private static SpaceSails.Client.Pages.Map ABarWithAStrangerInIt()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);

        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no bar.");
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));

        Set(map, "_bondForce", true);
        return map;
    }

    /// <summary>Put the captain north of the bar's own south wall, or south of it — the one wall
    /// <c>InTheBar</c> reads, taken off the room the game draws.</summary>
    private static void StandHim(SpaceSails.Client.Pages.Map map, bool inTheBar)
    {
        Set(map, "_avatarY", inTheBar ? TheBarsFloorY + 6.0 : TheBarsFloorY - 6.0);
        Invoke(map, "RefreshAshore");

        // Both places are ASHORE — past the tube, off the ship — which is what makes the pair a fair test:
        // the difference between them is the room and nothing else.
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
    }

    /// <summary>The scare itself, through the page's own <c>FireShudder</c> — the call that ends in
    /// <c>TryBond</c>, so what is under test is the shipping road to the beat and not the beat's own door.</summary>
    private static void TheHullShudders(SpaceSails.Client.Pages.Map map) =>
        Invoke(map, "FireShudder", 10_000.0);

    /// <summary>Has anybody on the bar's roster started dealing with this captain? The bond's only durable
    /// mark besides the pour — goodwill through the real <c>ContactLedger</c>, which is what turns a stranger
    /// into a findable contact.</summary>
    private static bool TheStrangerIsOnTheLedger(SpaceSails.Client.Pages.Map map)
    {
        object contacts = Read(map, "_contacts")!;
        foreach (string regular in PatronRota.Roster)
        {
            object sheet = contacts.GetType().GetMethod("For")!.Invoke(contacts, [regular])!;
            if ((bool)sheet.GetType().GetProperty("HasHistory")!.GetValue(sheet)!)
            {
                return true;
            }
        }

        return false;
    }
}
