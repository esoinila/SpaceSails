using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1349 · <b>A FLOOR CLICKED ON THE CAR'S PANEL HANDS THE KEYS BACK TO THE DECK.</b> QA, 2026-09-30, at
/// <c>/map?dock=the-deep&amp;ashore=1&amp;havenfloor=-1</c>: E opens the car's panel, CONCOURSE clicked with the
/// mouse, and then E — or <c>a</c> held for a second and a half — does nothing at all until the map is clicked.
/// The button the mouse pressed leaves the DOM with the panel, focus falls to <c>&lt;body&gt;</c>, and the
/// deck's key handler hangs off <c>.map-page</c> (Map.razor:14), which is no longer anywhere the keys go.
///
/// <para>The house idiom (#470's <c>Dismiss</c>, Map.Sim.Keys.cs) is that every mouse press that closes or
/// changes the deck hands focus home to that div; the car's floor rows and the keypad's ↵ were the ones that
/// did not. Off a browser there is no DOM focus to read, so this bench does the next honest thing: it gives
/// the renderer an element-reference context whose JS side WRITES DOWN every focus call
/// (<see cref="DeskBench.WatchTheFocus"/>), and asks that the last one after the click named the page's own
/// keyboard host. The press is the renderer's own click on the button the tree drew; the key after it is a
/// real <c>keydown</c> at <c>.map-page</c>'s handler.</para>
///
/// <para><b>Proven RED</b> by putting the floor row back to <c>@onclick="() =&gt; PressLiftButton(stop)"</c>:
/// the car rides, the panel shuts, and nothing hands the keyboard back — the focus log is empty.</para>
/// </summary>
public sealed class AFloorClickedHandsTheKeysBackTests
{
    private const string TheDeepsLowerCar = "/map?dock=the-deep&ashore=1&havenfloor=-1";

    [Fact]
    public async Task ClickingConcourseInTheCarHandsTheKeyboardBackAndEOpensTheCarAgain()
    {
        using DeskBench bench = await DeskBench.BootAsync(TheDeepsLowerCar);
        bench.Poke("_audioArmed", true);
        bench.WatchTheFocus();
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        Assert.True(keyboard != 0, "the page drew no keyboard host — nothing could type at it.");

        int below = (int)bench.Field("_havenFloor")!;
        Assert.NotEqual(HavenLevels.Concourse, below);

        // E in the car: the panel.
        await bench.TypeAsync(keyboard, "e");
        painted = await bench.RenderAsync();
        Assert.True((bool)bench.Field("_showLiftPanel")!, "E in the car at the lower floor opened no panel.");

        // CONCOURSE, clicked — the button the tree drew, pressed through the renderer's own event channel.
        DeskBench.Painted.Node concourse = painted.Root.Descendants()
            .Where(n => n.Element == "button" && n.HasClass("lift-stop") && n.Handlers.ContainsKey("onclick"))
            .FirstOrDefault(n => n.Name.Contains(HavenLevels.NameOf(HavenLevels.Concourse),
                System.StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException("the car's panel drew no CONCOURSE row to click.");
        bench.ForgetTheFocus();
        await bench.PressAsync(concourse.Handlers["onclick"]);
        painted = await bench.RenderAsync();

        Assert.Equal(HavenLevels.Concourse, (int)bench.Field("_havenFloor")!);
        Assert.False((bool)bench.Field("_showLiftPanel")!, "the car rode and left its panel up.");

        // …AND THE KEYS CAME HOME: the last thing focused after the click is the deck's own keyboard host.
        var host = (ElementReference)bench.Field("_focusableDiv")!;
        Assert.True(bench.Focused.Count > 0,
            "a floor was clicked, the panel went away with the button under the mouse, and nothing handed "
            + "focus back to .map-page — in a browser, E and WASD are now dead until the map is clicked.");
        Assert.Equal(host.Id, bench.Focused[^1]);

        // …and the key that arrives there does what the player pressed it for: E at the car opens it again.
        await bench.TypeAsync(keyboard, "e");
        painted = await bench.RenderAsync();
        Assert.True((bool)bench.Field("_showLiftPanel")!,
            "E at the car the captain just rode up in opened nothing.");
        Assert.Empty(bench.EscapedPastTheGate);
    }
}
