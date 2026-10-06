using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 1 · <b>THE LOSS AND THE FORM.</b> A holed sail's mend window is the loss (the amended cut: loss of
/// USE — the game prices no repair, so no credit moves): the book's 📋 line is written when the window COMPLETES, with
/// the days it actually took; and the ship's own desk, seat-tied, copies the newest unclaimed loss onto the blank
/// claim form. Both halves are driven through the page's own paths — the real <c>CheckSailHole</c> frame check, the
/// real strip button's handler — and judged on state after the whole sequence.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLossAndTheFormTests
{
    private const double Day = 86400.0;

    private static void At(Pages.Map map, double simTime)
    {
        Set(map, "SimTime", simTime);
        var ship = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", ship with { SimTime = simTime });
    }

    private static void TheSailIsHoledByTheCloudTops(Pages.Map map)
    {
        Set(map, "_frameMaxDragDecel", 4.0 * 9.80665);
        Invoke(map, "CheckSailHole");
        Set(map, "_frameMaxDragDecel", 0.0);
    }

    private static IEnumerable<FieldNote> Notes(Pages.Map map) => (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!;

    private static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Read(map, "_roomsTurnedOver")!;

    private static int Credits(Pages.Map map) => (int)Read(map, "_credits")!;

    private static IEnumerable<FieldNote> LossLines(Pages.Map map) =>
        Notes(map).Where(n => n.Text.StartsWith("Hull holed — ", StringComparison.Ordinal));

    // ── THE LOSS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE LOSS LINE IS WRITTEN WHEN THE MEND COMPLETES — NOT WHEN THE SAIL IS HOLED — WITH THE DAYS IT REALLY TOOK,
    /// ONCE, AND NO CREDIT MOVES.</b> Holed in the cloud tops at t0: the window is open and the book has nothing; a day
    /// in, still nothing; the frame the crew clears it (a third of a day late, so the figure is not the constant) writes
    /// exactly one 📋 line under Nebula Mutual and the place, "2.3 days", and the register's loss tag; more frames write
    /// no more; the purse is exactly what it was.
    ///
    /// <para><b>Proven RED</b> by the line written in <c>TheSailIsHoled</c> instead of <c>TheMendIsDone</c> (a line on
    /// the first frame), by the figure taken as the constant (2.0), and by a credit deducted.</para>
    /// </summary>
    [Fact]
    public void TheLossIsWrittenWhenTheMendCompletesWithTheDaysItReallyTook()
    {
        Pages.Map map = Boot("claim-loss-complete");
        double t0 = 10 * Day;
        int purse = Credits(map);
        At(map, t0);

        TheSailIsHoledByTheCloudTops(map);
        Assert.True((bool)Read(map, "_sailHoled")!);
        Assert.Empty(LossLines(map));
        Assert.DoesNotContain(Register(map), t => t.StartsWith("claim:", StringComparison.Ordinal));

        At(map, t0 + Day);
        Invoke(map, "CheckSailHole");
        Assert.True((bool)Read(map, "_sailHoled")!);
        Assert.Empty(LossLines(map));

        double cleared = t0 + (2.3 * Day);
        At(map, cleared);
        Invoke(map, "CheckSailHole");
        Assert.False((bool)Read(map, "_sailHoled")!);

        FieldNote wrote = Assert.Single(LossLines(map));
        Assert.Equal(HullClaim.LossLine(23), wrote.Text);
        Assert.Equal(
            "Hull holed — 2.3 days under sail-mend, and the sky kept its schedule without you. "
            + "The policy calls lost days claimable. Claimable is not the same as paid.",
            wrote.Text);
        Assert.Equal("📋", wrote.Glyph);
        Assert.Equal(CaseSubjects.Line(CaseSubjects.Office("Nebula Mutual"), CaseSubjects.Place(
            (string?)Invoke(map, "TheBooksNameForHere") ?? "")), wrote.Subjects);
        Assert.Equal(
            [HullClaim.LossTag(new HullClaim.Loss((long)cleared, 23))],
            Register(map).Where(t => t.StartsWith("claim:", StringComparison.Ordinal)));

        for (int i = 0; i < 5; i++)
        {
            At(map, cleared + (i * 600));
            Invoke(map, "CheckSailHole");
        }

        Assert.Single(LossLines(map));
        Assert.Equal(purse, Credits(map));
    }

    /// <summary>
    /// <b>A RE-HOLE INSIDE THE WINDOW EXTENDS THE SAME LOSS; A HOLING AFTER COMPLETION STARTS A NEW ONE.</b> Holed at t0,
    /// holed again half a day in (the page's own primitive — the frame check guards a second holing today, so the
    /// extension is pinned where it lives): the window now runs to two days past the SECOND hole and clears with ONE
    /// line reading 2.5 days measured from the FIRST hole. Then a fresh holing after that completion is its own window
    /// and its own line, and the two losses stand side by side in the register, both unclaimed.
    ///
    /// <para><b>Proven RED</b> by <c>HullClaim.Hole</c> replacing an open window (the figure reads 2.0 — the pin names
    /// the elapsed figure against a window whose timer was extended).</para>
    /// </summary>
    [Fact]
    public void AReHoleInsideTheWindowExtendsTheLossAndAFreshHoleAfterItStartsAnother()
    {
        Pages.Map map = Boot("claim-loss-extend");
        double t0 = 20 * Day;
        At(map, t0);
        Invoke(map, "TheSailIsHoled");
        Set(map, "_sailHoled", true);

        At(map, t0 + (0.5 * Day));
        Invoke(map, "TheSailIsHoled");
        Assert.Equal(t0 + (2.5 * Day), (double)Read(map, "_sailRepairedAtSimTime")!);

        At(map, t0 + (2.0 * Day));
        Invoke(map, "CheckSailHole");
        Assert.True((bool)Read(map, "_sailHoled")!, "the extended window cleared at the original time.");
        Assert.Empty(LossLines(map));

        At(map, t0 + (2.5 * Day));
        Invoke(map, "CheckSailHole");
        Assert.False((bool)Read(map, "_sailHoled")!);
        Assert.Equal(HullClaim.LossLine(25), Assert.Single(LossLines(map)).Text);

        double again = t0 + (5 * Day);
        At(map, again);
        TheSailIsHoledByTheCloudTops(map);
        At(map, again + (2 * Day));
        Invoke(map, "CheckSailHole");
        Assert.Equal([HullClaim.LossLine(25), HullClaim.LossLine(20)], LossLines(map).Select(n => n.Text));
        Assert.Equal(2, Register(map).Count(t => t.StartsWith("claim:loss:", StringComparison.Ordinal)));
    }

    // ── THE FORM, AT THE SHIP'S OWN DESK ────────────────────────────────────────────────────────────────

    private static Pages.Map Aboard()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_deckMode", true);
        Set(map, "_deckPlan", DeckPlan.Ship);
        return map;
    }

    private static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    private static Pages.Map SatAtTheDesk()
    {
        Pages.Map map = Aboard();
        DeckReachability.Point at = ShipLayout.CabinDeskStationIn(ShipLayout.DeskCabin);
        StandAt(map, at.X, at.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "the desk refused [E].");
        return map;
    }

    private static Pages.Map SatInTheCantina()
    {
        Pages.Map map = Aboard();
        DeckPlan.ConsoleSpot top = ((DeckPlan)Read(map, "_deckPlan")!).Consoles.First(c => c.Kind == DeckPlan.ConsoleKind.BarTop);
        StandAt(map, top.X, top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "the top refused [E].");
        return map;
    }

    private static List<Satchel.Item> Sleeve(Pages.Map map) => (List<Satchel.Item>)Read(map, "_satchel")!;

    private static bool Fillable(Pages.Map map) => (bool)Read(map, "ClaimFormFillable")!;

    private static void Hold(Pages.Map map, params Satchel.Item[] items) => Sleeve(map).AddRange(items);

    private static void Lose(Pages.Map map, long at, int tenths) => Register(map).Add(HullClaim.LossTag(new(at, tenths)));

    /// <summary>
    /// <b>THE VERB IS DRAWN ONLY AT THE SHIP'S OWN DESK, WITH THE BLANK FORM HELD AND AN UNCLAIMED LOSS — ALL THREE.</b>
    /// The matrix over seat, blank and loss: eight corners, one of them true. (The cantina's top is a seat and not the
    /// desk; standing is not a seat; a filled form is not a blank.) Seat-tied, never place-tied: the same page standing
    /// anywhere else, with both papers in hand, is offered nothing.
    ///
    /// <para><b>Proven RED</b> by dropping the seat clause (offered standing), the blank clause (offered with nothing to
    /// copy onto) and the loss clause (offered with nothing to copy).</para>
    /// </summary>
    [Fact]
    public void TheVerbNeedsTheDeskAndTheBlankAndTheLoss()
    {
        foreach (bool atTheDesk in new[] { true, false })
        {
            foreach (bool blank in new[] { true, false })
            {
                foreach (bool loss in new[] { true, false })
                {
                    Pages.Map map = atTheDesk ? SatAtTheDesk() : Aboard();
                    if (blank)
                    {
                        Hold(map, SideOffices.Adjuster.TheSheet);
                    }

                    if (loss)
                    {
                        Lose(map, 100, 20);
                    }

                    Assert.True(
                        Fillable(map) == (atTheDesk && blank && loss),
                        $"desk={atTheDesk} blank={blank} loss={loss}: the verb is {(Fillable(map) ? "drawn" : "missing")}.");
                }
            }
        }

        Pages.Map cantina = SatInTheCantina();
        Hold(cantina, SideOffices.Adjuster.TheSheet);
        Lose(cantina, 100, 20);
        Assert.False(Fillable(cantina), "offered at a cantina top, which is a seat and not the ship's desk.");

        Pages.Map holdingTheFilled = SatAtTheDesk();
        Hold(holdingTheFilled, new Satchel.Item(Core.Satchel.Kind.Paper, HullClaim.FilledId(new(100, 20))));
        Lose(holdingTheFilled, 200, 20);
        Assert.False(Fillable(holdingTheFilled), "offered with only a filled form held.");
    }

    /// <summary>
    /// <b>THE PRESS COPIES THE NEWEST UNCLAIMED LOSS AND THE LOSS IS THEN CLAIMED — THE VERB GOES.</b> Two losses; the
    /// press makes the blank into the filled form of the NEWER (the id carries its figure), tags that loss filed, keeps
    /// the older unclaimed, and with the blank gone the verb is gone: a second press changes nothing. A second blank, a
    /// second press, and the OLDER loss is copied: one loss per form, never one loss twice.
    ///
    /// <para><b>Proven RED</b> by the fill dropped but the verb left on (the filing tag not written, so the verb persists
    /// and a second blank copies the SAME loss again): the loss is claimed twice — the cut's named mutation.</para>
    /// </summary>
    [Fact]
    public void ThePressFillsTheFormClaimsTheLossAndTheVerbGoesUntilThereIsAnotherBlank()
    {
        Pages.Map map = SatAtTheDesk();
        Hold(map, new Satchel.Item(Core.Satchel.Kind.Paper, "an-unrelated-paper"), SideOffices.Adjuster.TheSheet);
        Lose(map, 100, 20);
        Lose(map, 900, 27);
        Assert.True(Fillable(map));

        Invoke(map, "FillTheClaimForm");

        Assert.Equal(["an-unrelated-paper", "claim-form-filled:900:27"], Sleeve(map).Select(i => i.Id));
        Assert.Contains("claim:filed:900", Register(map));
        Assert.DoesNotContain("claim:filed:100", Register(map));
        Assert.False(Fillable(map), "the verb persists with no blank held.");
        Invoke(map, "FillTheClaimForm");   // the stale handler: nothing happens
        Assert.Equal(["an-unrelated-paper", "claim-form-filled:900:27"], Sleeve(map).Select(i => i.Id));

        Hold(map, SideOffices.Adjuster.TheSheet);   // "a second blank form can be fetched from the cold rooms"
        Assert.True(Fillable(map), "the older, still-unclaimed loss is not on offer for a second form.");
        Invoke(map, "FillTheClaimForm");

        Assert.Equal(
            ["an-unrelated-paper", "claim-form-filled:900:27", "claim-form-filled:100:20"],
            Sleeve(map).Select(i => i.Id));
        Assert.Contains("claim:filed:100", Register(map));
        Assert.False(Fillable(map));

        Hold(map, SideOffices.Adjuster.TheSheet);
        Assert.False(Fillable(map), "both losses are claimed: a third blank has nothing to copy.");
    }

    /// <summary>
    /// <b>THE BUTTON IS WIRED, AND ONLY BEHIND THE GATE.</b> The strip's markup draws the verb under
    /// <c>@if (ClaimFormFillable)</c>, wearing Core's label, wired to the handler the state test above drives; the
    /// page hands it down the rack that carries every other strip button. No new popup, nothing to close.
    ///
    /// <para><b>Proven RED</b> by the <c>@if</c> removed (the verb is drawn at every seat).</para>
    /// </summary>
    [Fact]
    public void TheStripDrawsTheVerbBehindItsGate()
    {
        string strip = MapMarkup.Read(System.IO.Path.Combine(MapMarkup.SurfacesDirectory(), "SeatedDockedStrip.razor"));
        int gate = strip.IndexOf("@if (ClaimFormFillable)", StringComparison.Ordinal);
        Assert.True(gate > 0, "the strip never asks whether the verb is on offer.");
        string block = strip[gate..strip.IndexOf("@* #1052", gate, StringComparison.Ordinal)];
        Assert.Contains("@onclick=\"FillTheClaimForm\"", block, StringComparison.Ordinal);
        Assert.Contains("@SpaceSails.Core.HullClaim.VerbLabel", block, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(strip, @"HullClaim\.VerbLabel"));

        // The page hands both down the rack that carries every other strip button, and the rack hands them to the strip.
        string page = System.IO.File.ReadAllText(MapMarkup.PagePath);
        Assert.Contains("ClaimFormFillable=\"@ClaimFormFillable\"", page, StringComparison.Ordinal);
        Assert.Contains("FillTheClaimForm=\"@FillTheClaimForm\"", page, StringComparison.Ordinal);
        string rack = MapMarkup.Read(System.IO.Path.Combine(MapMarkup.SurfacesDirectory(), "SeatedTableRack.razor"));
        Assert.Contains("ClaimFormFillable=\"@ClaimFormFillable\" FillTheClaimForm=\"@FillTheClaimForm\"", rack, StringComparison.Ordinal);
    }
}
