using System;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1478 · <b>FLIGHT KEYS ARE INERT ON FOOT.</b> Pressing <c>-</c> while landed undocked the ship, fired the
/// pulse drive and flipped the view to the deck plan, because HandleDeckKey does not consume the burn keys
/// and they fell through to the burn switch in OnKeyDown.
/// </summary>
public sealed class FlightKeysAreInertOnFootTests
{
    private const string OnMirandasGround = "/map?dock=the-tilt&site=0&land=1";

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
        bench.Poke("_audioArmed", true);

        object? dock = bench.Peek("_dockedHavenId");
        object? ship = bench.Peek("_ship");
        object? pulses = bench.Peek("_reactionMassPulses");
        ShipDesk desk = bench.ActiveDesk;
        bool deck = bench.DeckMode;

        await bench.TypeAsync(keyboard, key);

        Assert.True(bench.OnSurface, $"'{key}' took the captain off the ground.");
        Assert.Equal(dock, bench.Peek("_dockedHavenId"));
        Assert.Equal(ship, bench.Peek("_ship"));
        Assert.Equal(pulses, bench.Peek("_reactionMassPulses"));
        Assert.Equal(desk, bench.ActiveDesk);
        Assert.Equal(deck, bench.DeckMode);
    }
}
