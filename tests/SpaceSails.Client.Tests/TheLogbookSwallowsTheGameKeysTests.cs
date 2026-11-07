using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#1483 · With the logbook (save rack) or the start picker open, 7 and V acted on the voyage behind it.
/// Both modals now swallow every game key; Esc still closes the logbook.</summary>
public sealed class TheLogbookSwallowsTheGameKeysTests
{
    private const string OnTheShipsDeck = "/map?dock=selene-gate&body=luna&site=1";

    private static async Task<(DeskBench bench, ulong keyboard)> BootOnNavAsync()
    {
        DeskBench bench = await DeskBench.BootAsync(OnTheShipsDeck);
        await bench.SwitchAsync(ShipDesk.Nav);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        bench.Poke("_audioArmed", true);
        bench.Poke("_plasma", new PlasmaEnvironment((ICelestialEphemeris)bench.Peek("_ephemeris")!, []));
        bench.Poke("_ship", ((ShipState)bench.Peek("_ship")!) with { Charge = 10 });
        return (bench, keyboard);
    }

    [Theory]
    [InlineData("_showSaveDrawer")]
    [InlineData("_showStartPicker")]
    public async Task SevenAndVDoNothingBehindAnOpenModal(string modalFlag)
    {
        (DeskBench bench, ulong keyboard) = await BootOnNavAsync();
        using (bench)
        {
            bench.Poke(modalFlag, true);
            ShipDesk desk = bench.ActiveDesk;
            bool deckMode = bench.DeckMode;
            object? ship = bench.Peek("_ship");
            object? vented = bench.Peek("_lastVentSimTime");

            await bench.TypeAsync(keyboard, "7");
            await bench.TypeAsync(keyboard, "V");

            Assert.Equal(desk, bench.ActiveDesk);
            Assert.Equal(deckMode, bench.DeckMode);
            Assert.Equal(ship, bench.Peek("_ship"));
            Assert.Equal(vented, bench.Peek("_lastVentSimTime"));
        }
    }

    [Fact]
    public async Task EscapeStillClosesTheLogbook()
    {
        (DeskBench bench, ulong keyboard) = await BootOnNavAsync();
        using (bench)
        {
            bench.Poke("_showSaveDrawer", true);
            await bench.TypeAsync(keyboard, "Escape");
            Assert.False((bool)bench.Peek("_showSaveDrawer")!);
        }
    }

    [Fact]
    public async Task EscapeOverABankSheetCancelsTheSheetAndLeavesTheLogbook()
    {
        (DeskBench bench, ulong keyboard) = await BootOnNavAsync();
        using (bench)
        {
            bench.Poke("_showSaveDrawer", true);
            bench.Poke("_bankPrompt", Pages.Map.BankPromptKind.Bank);
            await bench.TypeAsync(keyboard, "Escape");
            Assert.Null(bench.Peek("_bankPrompt"));
            Assert.True((bool)bench.Peek("_showSaveDrawer")!);
        }
    }
}
