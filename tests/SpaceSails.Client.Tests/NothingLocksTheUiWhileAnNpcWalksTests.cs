using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #992 · <b>NOTHING LOCKS THE UI WHILE AN NPC WALKS.</b> The owner's ruling of 2026-08-24 — <i>"there should
/// not be a pop-up that cannot be closed or minimized"</i> — wearing a different coat, and reported live:
/// <i>"the long walk-in there just blocks UI."</i>
///
/// <h3>What was actually found, which is not quite what was expected</h3>
///
/// <para>The expectation handed to this lane was that <c>Map.WalkIn.cs</c> raises a beat, a plate or an input
/// lock at the START of her approach and holds it for the hundreds of frames she spends crossing the floor.
/// <b>It does not, and the code is clear about it.</b> Read in order:</para>
///
/// <list type="bullet">
/// <item><c>AdvanceTheWalkIn</c> casts her, spends the cadence, calls <c>ApproachTheTable</c> and then pulses
/// ONE line (<c>WalkIn.TheRoomLooks</c>). A pulse is <c>.deck-pulse-toast</c>, which is
/// <c>pointer-events: none</c> and expires on its own — it has never been able to eat a click.</item>
/// <item>Her card is raised in <c>SheReachesYourTable</c>, and that fires from <c>StepAnApproach</c> on the
/// LANDING frame. Between setting off and arriving, <c>_walkInCard</c> is null and the page draws nothing on
/// her account at all.</item>
/// <item><c>StepTheBarsFeet</c> only calls <c>StateHasChanged</c> when somebody LANDS
/// (<c>anybodyLanded</c>); an approach in progress returns false every frame, so her crossing does not even
/// cost the page a re-render.</item>
/// </list>
///
/// <para>So the two surfaces the owner's sentence actually lands on are both pop-ups and both in this lane's
/// register: the great port's arrival PLATE, whose title is literally
/// <c>ArrivalTube.Title(Tier.GreatPort)</c> = <b>"🛬 THE LONG WALK IN"</b> and which shipped with no dismiss
/// at all; and her own card at arrival, a full-viewport scrim with no ✕. Both are answered by #992.</para>
///
/// <h3>Why this file exists anyway</h3>
///
/// <para>Because "it does not lock the UI today" is a fact about today. The law below is a REGRESSION LOCK,
/// and it is honest about being green on arrival — a guard written after the fact is still the guard that
/// stops the next person raising a curtain at the start of a walk instead of at the landing frame. Its red
/// proof is in the PR body: a one-line gate on <c>_walkInWho</c> that raises a surface turns it red at once,
/// naming the surface.</para>
///
/// <h3>The state it drives, and why that state is the crossing</h3>
///
/// <para><c>_walkInWho</c> is set the instant the walk is PLANNED and cleared when the evening ends;
/// <c>_walkInCard</c> is set only when she is standing at the table. So <c>_walkInWho</c> non-null with
/// <c>_walkInCard</c> null is not an approximation of mid-crossing — <b>it is the mid-crossing state</b>, in
/// the shipping fields, exactly as <c>ApproachTheTable</c> leaves them. There is no off-browser way to walk a
/// body across a floor, and there does not need to be: what the room draws is a function of these fields.</para>
/// </summary>
[SlowGate] // #251 · 12 s over 1 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class NothingLocksTheUiWhileAnNpcWalksTests
{
    /// <summary>A great port with a bar behind it — the tier the walk-in's own venue gate asks for.</summary>
    private const string ADockedBar = "/map?dock=selene-gate&body=luna&site=1";

    [Fact]
    public async Task TheRoomGoesOnPlayingWhileSheCrossesTheFloor()
    {
        using DeskBench bench = await DeskBench.BootAsync(ADockedBar);

        // BEFORE: what the page offers with nobody on the floor. Measured rather than assumed, because a
        // law that asserted "the tabs are there" against a page that never draws tabs would pass on a blank
        // screen — this repo's fifth named bug class, in a room with a woman in it.
        await bench.SwitchAsync(ShipDesk.Nav);
        DeskBench.Painted before = await bench.RenderAsync();
        int tabsBefore = before.DeskTabLabels.Count;
        int liveControlsBefore = LiveControls(before);

        Assert.True(tabsBefore > 0,
            "the desk tab bar drew nothing before anybody set off, so this law cannot tell a locked UI from "
            + "a page that never had one.");
        Assert.True(liveControlsBefore > 0,
            "the page carried no pressable control before anybody set off — same problem.");

        // …AND SHE SETS OFF. The two fields ApproachTheTable leaves behind: cast and afoot, card not up.
        bench.Poke("_walkInWho", SpaceSails.Core.WalkIn.Who.Nadia);
        bench.Poke("_walkInCard", null);
        bench.Poke("_walkInAnswered", false);

        DeskBench.Painted during = await bench.RenderAsync();

        // 1 · SHE RAISED NOTHING. Not a card, not a plate, not a scrim — the whole point of her card landing
        // on the arrival frame rather than the departure one.
        //
        // Asked as a DIFFERENCE and not as "the screen is clear", and the difference is the honest question:
        // a docked berth already has its own arrival plate up, and the first build of this law failed on it
        // — naming `story-plate`, which is the berth's establishing shot and, at a great port, the one whose
        // title is literally "🛬 THE LONG WALK IN". That plate is a #992 offender in its own right (it now
        // has a ✕) and it is not hers. What this law is entitled to assert is that HER setting off adds
        // nothing, and asserting more than that would have been a guard blaming a woman for the weather.
        var appeared = SurfacesOn(during).Except(SurfacesOn(before), StringComparer.Ordinal).ToList();

        Assert.True(appeared.Count == 0,
            "somebody crossing the floor put a surface on the screen that was not there before she set off: ["
            + string.Join(" · ", appeared)
            + "]. Her card belongs on the LANDING frame (SheReachesYourTable) and nowhere else — a curtain "
            + "raised when she sets off is the owner's \"the long walk-in there just blocks UI\" (2026-08-24), "
            + "and it is the one shape this file exists to refuse.");

        // 2 · THE DESK SWITCHER STILL ANSWERS — both as markup and as a verb. The bar being drawn proves
        // nothing on its own; a bar of dead buttons draws identically.
        Assert.Equal(tabsBefore, during.DeskTabLabels.Count);

        await bench.SwitchAsync(ShipDesk.Captain);
        Assert.Equal(ShipDesk.Captain, bench.ActiveDesk);
        await bench.SwitchAsync(ShipDesk.Nav);
        Assert.Equal(ShipDesk.Nav, bench.ActiveDesk);

        // 3 · …AND THE NAV CONTROLS ARE STILL PRESSABLE. Counted off live onclick handlers rather than off
        // class names: a toolbar rendered `disabled` keeps every class it had.
        DeskBench.Painted after = await bench.RenderAsync();
        int liveControlsAfter = LiveControls(after);

        Assert.True(liveControlsAfter >= liveControlsBefore,
            $"the helm lost controls while she was walking — {liveControlsBefore} pressable before, "
            + $"{liveControlsAfter} during. The captain keeps his ship while somebody crosses a room.");

        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>Controls the page would actually respond to: an enabled element carrying a live click
    /// handler. <c>disabled</c> is read because that is how a toolbar goes dead without changing shape.</summary>
    /// <summary>Every raised surface on the screen, by root class — the same recogniser the dismissibility
    /// law uses, kept to its structural half so this file needs no register of its own.</summary>
    private static IEnumerable<string> SurfacesOn(DeskBench.Painted painted) =>
        painted.ClassLists
            .SelectMany(list => list.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(css => css.EndsWith("-backdrop", StringComparison.Ordinal)
                          || css.EndsWith("-overlay", StringComparison.Ordinal)
                          || string.Equals(css, "story-plate", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static int LiveControls(DeskBench.Painted painted) =>
        painted.Root.Descendants()
            .Count(n => n.Handlers.ContainsKey("onclick")
                        && !n.Hidden
                        && !n.Attributes.ContainsKey("disabled"));
}
