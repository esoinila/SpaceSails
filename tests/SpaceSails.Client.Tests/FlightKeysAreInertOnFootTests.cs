using System;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1478 · <b>FLIGHT KEYS ARE INERT ON FOOT.</b> Pressing <c>-</c> while landed undocked the ship, fired the
/// pulse drive and flipped the view to the deck plan, because HandleDeckKey does not consume the burn keys
/// and they fell through to the burn switch in OnKeyDown. V is the one exception on the SHIP's deck, where the
/// charge board (opened in deck mode) promises it vents.
/// </summary>
public sealed class FlightKeysAreInertOnFootTests
{
    private const string OnMirandasGround = "/map?dock=the-tilt&site=0&land=1";
    private const string OnTheShipsDeck = "/map?dock=selene-gate&body=luna&site=1";

    [Theory]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData("+")]
    [InlineData("=")]
    [InlineData("o")]
    [InlineData("O")]
    [InlineData("p")]
    [InlineData("P")]
    [InlineData("v")]
    [InlineData("V")]
    public async Task AFlightKeyOnTheGroundDoesNothing(string key)
    {
        using DeskBench bench = await DeskBench.BootAsync(OnMirandasGround);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        Assert.True(bench.OnSurface, "the bench did not land the captain.");
        GiveItAPlasma(bench);
        bench.Poke("_audioArmed", true);
        var ship = (ShipState)bench.Peek("_ship")!;
        bench.Poke("_ship", ship with { Charge = 10 });

        object? dock = bench.Peek("_dockedHavenId");
        object? shipBefore = bench.Peek("_ship");
        object? pulses = bench.Peek("_reactionMassPulses");
        object? plot = bench.Peek("PlotMode");
        object? vented = bench.Peek("_lastVentSimTime");
        object? armed = bench.Peek("_armedOrbitBodyId");
        string said = bench.Pulse;
        ShipDesk desk = bench.ActiveDesk;
        bool deck = bench.DeckMode;

        await bench.TypeAsync(keyboard, key);

        Assert.True(bench.OnSurface, $"'{key}' took the captain off the ground.");
        Assert.Equal(dock, bench.Peek("_dockedHavenId"));
        Assert.Equal(shipBefore, bench.Peek("_ship"));
        Assert.Equal(pulses, bench.Peek("_reactionMassPulses"));
        Assert.Equal(plot, bench.Peek("PlotMode"));
        Assert.Equal(vented, bench.Peek("_lastVentSimTime"));
        Assert.Equal(armed, bench.Peek("_armedOrbitBodyId"));
        Assert.Equal(said, bench.Pulse);
        Assert.Equal(desk, bench.ActiveDesk);
        Assert.Equal(deck, bench.DeckMode);
    }

    [Theory]
    [InlineData("v")]
    [InlineData("V")]
    public async Task VStillVentsOnTheShipsDeck(string key)
    {
        using DeskBench bench = await DeskBench.BootAsync(OnTheShipsDeck);
        await bench.SwitchAsync(ShipDesk.Deck);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        Assert.True(bench.DeckMode, "the bench is not in deck mode.");
        Assert.False(bench.OnSurface);
        GiveItAPlasma(bench);
        bench.Poke("_audioArmed", true);
        var ship = (ShipState)bench.Peek("_ship")!;
        bench.Poke("_ship", ship with { Charge = 10 });

        await bench.TypeAsync(keyboard, key);

        Assert.Equal(5.0, ((ShipState)bench.Peek("_ship")!).Charge, 6);
    }

    /// <summary>The bench's scenario has no electric universe, so VentCharge would return at once and V could
    /// never fail. Hand the page an environment, so a vent that reaches the drive really spends charge.</summary>
    private static void GiveItAPlasma(DeskBench bench) =>
        bench.Poke("_plasma", new PlasmaEnvironment((ICelestialEphemeris)bench.Peek("_ephemeris")!, []));
}
