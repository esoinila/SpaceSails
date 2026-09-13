using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #325 / #332 · <b>THE CHANDLERY, ON A LIVE PAGE.</b>
///
/// <para>Core owns the prices and the budget law (<c>TheChandlerySellsMarginTests</c>). This bench is the
/// other half: a shipping <see cref="Pages.Map"/> tied to a real berth, a purse, a med-bay cabinet somebody
/// has been eating out of, and the two rows the owner asked for — <c>EXTENDED TANK</c> and
/// <c>MED-KIT REFILL</c>.</para>
///
/// <para>What it is watching for is the thing a shop gets wrong: a row that is live in open space, a debit
/// that does not arrive, a bought bottle that is spent twice or never, and a cabinet that reports one stock
/// and dispenses another.</para>
/// </summary>
public sealed class TheChandleryIsOpenAtTheBerthTests
{
    private const BindingFlags Hidden = TestTree.PrivateOnAnInstance;

    /// <summary>A berth with a bar — the chandlery's own gate, taken off the shipped roster rather than
    /// typed, so a renamed haven cannot make this bench silently stop testing anything.</summary>
    private static Barkeep AHouse => Barkeeps.AllBarkeeps[0];

    private static Pages.Map AtTheBerth(string? havenId, int credits = 10_000)
    {
        var map = new Pages.Map();

        // The same render early-out every page bench in this suite rides: without it the page's verbs
        // queue a render against a renderer that is not there.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Set(map, "_dockedHavenId", havenId);
        Set(map, "_credits", credits);
        return map;
    }

    // ── (a) THE ROWS ARE A BERTH'S ROWS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #325/#332 · <b>THE CHANDLERY IS SHUT IN OPEN SPACE, AND THE PRESSES REFUSE THERE TOO.</b>
    ///
    /// <para>Two claims, because the markup's <c>@if</c> and the verb's own guard are two different
    /// defences and the owner's law is that a row is ABSENT when docked nowhere. The second half matters
    /// separately: a verb that only the markup was stopping is one keyboard shortcut away from selling a
    /// tank in deep space at a price nobody quoted.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>ChandleryHouse is null</c> guard out of
    /// <c>BuyExtendedTank</c>: the purse was debited 0 cr and a bottle appeared in stores four AU from
    /// anywhere.</para>
    /// </summary>
    [Fact]
    public void DockedNowhere_TheRowsAreAbsentAndThePressesDoNothing()
    {
        Pages.Map adrift = AtTheBerth(havenId: null);

        Assert.Null(Get(adrift, "ChandleryHouse"));
        Assert.Equal(0, (int)Get(adrift, "ExtendedTankPrice")!);

        int purse = (int)Get(adrift, "_credits")!;
        Invoke(adrift, "BuyExtendedTank");
        Invoke(adrift, "BuyMedKitRefill");

        Assert.Equal(purse, (int)Get(adrift, "_credits")!);
        Assert.Equal(0, (int)Get(adrift, "_extendedTanks")!);

        // …and at a berth the very same page opens.
        Pages.Map docked = AtTheBerth(AHouse.BodyId);
        Assert.NotNull(Get(docked, "ChandleryHouse"));
        Assert.Equal(Chandlery.ExtendedTankPrice(AHouse), (int)Get(docked, "ExtendedTankPrice")!);
    }

    /// <summary>
    /// #325/#332 · <b>AND THE MARKUP AGREES.</b> Both rows live inside the one <c>@if (ChandleryOpen())</c>
    /// block — read off the shipped <c>.razor</c>, because "the property says shut" is not the same claim as
    /// "the button is not on the screen", and this desk has shipped a row outside its gate before.
    ///
    /// <para><b>Proven RED</b> by moving the two buttons below the closing brace of the gate: both offsets
    /// landed outside the block and the guard named them.</para>
    /// </summary>
    [Fact]
    public void BothRowsSitInsideTheDockedGate()
    {
        string panel = File.ReadAllText(Path.Combine(
            TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "DeskPanels.razor"));

        int gate = panel.IndexOf("@if (ChandleryOpen())", StringComparison.Ordinal);
        Assert.True(gate >= 0, "the chandlery's docked gate is not in DeskPanels.razor at all.");

        // The gate's block, by brace balance from the '{' that opens it — so a row moved one line past the
        // end of the block is outside it, which is the failure this is watching for.
        int open = panel.IndexOf('{', gate);
        Assert.True(open > gate, "the gate has no block.");
        int depth = 0, close = -1;
        for (int i = open; i < panel.Length; i++)
        {
            if (panel[i] == '{') { depth++; }
            else if (panel[i] == '}' && --depth == 0) { close = i; break; }
        }
        Assert.True(close > open, "the gate's block is never closed.");

        foreach (string row in new[] { "chandlery-tank-btn", "chandlery-medkit-btn" })
        {
            int at = panel.IndexOf(row, StringComparison.Ordinal);
            Assert.True(at >= 0, $"{row} is not on the desk at all.");
            Assert.True(at > open && at < close,
                $"{row} is outside the docked gate — that row is live in open space.");
        }
    }

    // ── (b) BUYING ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #325 · <b>A BOUGHT BOTTLE DEBITS THE PURSE AND LANDS IN STORES — AND THEY STACK.</b> Priced off this
    /// house's own card, so the assertion cannot pass against a typed figure.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>_extendedTanks++</c>: the purse went down and stores
    /// stayed at zero, which is the shape of a shop that takes money for nothing.</para>
    /// </summary>
    [Fact]
    public void BuyingATank_DebitsThePurseAndIncrementsStores()
    {
        Pages.Map map = AtTheBerth(AHouse.BodyId, credits: 10_000);
        int price = Chandlery.ExtendedTankPrice(AHouse);
        Assert.True(price > 0, "the house is selling tanks for nothing — this bench cannot tell pass from fail.");

        Invoke(map, "BuyExtendedTank");
        Assert.Equal(10_000 - price, (int)Get(map, "_credits")!);
        Assert.Equal(1, (int)Get(map, "_extendedTanks")!);

        Invoke(map, "BuyExtendedTank");
        Assert.Equal(10_000 - (2 * price), (int)Get(map, "_credits")!);
        Assert.Equal(2, (int)Get(map, "_extendedTanks")!);
    }

    /// <summary>
    /// #325 · <b>AND AN EMPTY PURSE BUYS NOTHING.</b> The existing berth refusal — the row goes disabled
    /// below the price — with the verb's own guard behind it.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>_credits &lt; price</c> check: stores gained a bottle and
    /// the purse went negative.</para>
    /// </summary>
    [Fact]
    public void AShortPurseBuysNothingAndGoesNoFurtherIntoDebt()
    {
        int price = Chandlery.ExtendedTankPrice(AHouse);
        Pages.Map map = AtTheBerth(AHouse.BodyId, credits: price - 1);

        Invoke(map, "BuyExtendedTank");

        Assert.Equal(price - 1, (int)Get(map, "_credits")!);
        Assert.Equal(0, (int)Get(map, "_extendedTanks")!);
    }

    // ── (c) THE CABINET ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #332 · <b>THE CABINET EMPTIES, SAYS THE NEW LINE, AND THE CHANDLERY FILLS IT.</b>
    ///
    /// <para>The whole of what #332 asked for in one walk: swallow the stock, read what the cabinet says
    /// when there is none left (it must no longer be the sentence about the backlog), pay this house's own
    /// per-glass rate for exactly the pills that are missing, and find the cabinet dispensing again.</para>
    ///
    /// <para><b>Proven RED</b> three ways: leaving the old <i>"(Restock is a later lane.)"</i> line in
    /// <c>TakePill</c> (the empty-cabinet assertion named it); charging a flat cabinet price instead of the
    /// shortfall (the debit assertion failed at 48 vs 8); and dropping the <c>_pills += pills</c> (the
    /// cabinet stayed empty after the receipt printed).</para>
    /// </summary>
    [Fact]
    public void TheCabinetEmptiesSaysTheNewLineAndTheChandleryFillsIt()
    {
        Pages.Map map = AtTheBerth(AHouse.BodyId, credits: 10_000);
        Assert.Equal(Chandlery.MedKitFullStock, (int)Get(map, "_pills")!);

        for (int i = 0; i < Chandlery.MedKitFullStock; i++)
        {
            string dispensed = (string)Invoke(map, "TakePill")!;
            Assert.DoesNotContain("empty", dispensed, StringComparison.OrdinalIgnoreCase);
        }

        // EMPTY, and the line is the authored one — not the backlog note it replaced.
        Assert.Equal(0, (int)Get(map, "_pills")!);
        string onEmpty = (string)Invoke(map, "TakePill")!;
        Assert.Equal(Chandlery.CabinetEmptyLine, onEmpty);
        Assert.DoesNotContain("later lane", onEmpty, StringComparison.OrdinalIgnoreCase);

        // THE REFILL: this house's own per-glass rate, times the pills actually missing.
        int expected = Chandlery.MedKitRefillPrice(AHouse, Chandlery.MedKitFullStock);
        Assert.True(expected > 0, "a refill here is free — this bench cannot tell pass from fail.");

        Invoke(map, "BuyMedKitRefill");
        Assert.Equal(10_000 - expected, (int)Get(map, "_credits")!);
        Assert.Equal(Chandlery.MedKitFullStock, (int)Get(map, "_pills")!);

        // …and the cabinet is dispensing again, which is the only proof the count meant anything.
        Assert.NotEqual(Chandlery.CabinetEmptyLine, (string)Invoke(map, "TakePill")!);

        // A cabinet one pill short costs ONE glass, not a cabinet — the honest thing is not priced like the
        // wasteful one.
        int purse = (int)Get(map, "_credits")!;
        Invoke(map, "BuyMedKitRefill");
        Assert.Equal(purse - AHouse.DrinkPrice, (int)Get(map, "_credits")!);
        Assert.Equal(Chandlery.MedKitFullStock, (int)Get(map, "_pills")!);

        // And a FULL cabinet takes nothing at all.
        purse = (int)Get(map, "_credits")!;
        Invoke(map, "BuyMedKitRefill");
        Assert.Equal(purse, (int)Get(map, "_credits")!);
    }

    // ── (d) THE BOTTLE IS SPENT ONCE, AT THE SHUTTLE ─────────────────────────────────────────────────────

    /// <summary>
    /// #325 · <b>A TANK IS CONSUMED ONCE, AND ONLY WHERE A SUIT IS PUT ON.</b>
    ///
    /// <para>Fitting spends exactly one from stores and hands back the one bit the excursion is built with;
    /// asking again spends the next one; asking with empty stores spends nothing and reports nothing
    /// fitted. This is the whole of "consumed at that excursion's start" — the count is touched at one
    /// moment and by one verb, so nothing that runs during the walk can spend a second bottle.</para>
    ///
    /// <para><b>Proven RED</b> by making <c>FitExtendedTankForExcursion</c> decrement before its
    /// <c>&lt;= 0</c> check: stores went to −1 on a ship carrying nothing, and the captain was told a tank
    /// was fitted.</para>
    /// </summary>
    [Fact]
    public void FittingSpendsExactlyOneBottleAndOnlyIfThereIsOne()
    {
        Pages.Map map = AtTheBerth(AHouse.BodyId, credits: 10_000);
        Invoke(map, "BuyExtendedTank");
        Invoke(map, "BuyExtendedTank");
        Assert.Equal(2, (int)Get(map, "_extendedTanks")!);

        Assert.True((bool)Invoke(map, "FitExtendedTankForExcursion")!);
        Assert.Equal(1, (int)Get(map, "_extendedTanks")!);

        Assert.True((bool)Invoke(map, "FitExtendedTankForExcursion")!);
        Assert.Equal(0, (int)Get(map, "_extendedTanks")!);

        Assert.False((bool)Invoke(map, "FitExtendedTankForExcursion")!);
        Assert.Equal(0, (int)Get(map, "_extendedTanks")!);
    }

    /// <summary>
    /// #325 · <b>AND THE EXCURSION IT BUILDS IS TWICE THE WALK.</b> The budget, the suit's starting charge
    /// and the ground it may stand on, all off the one flag — read from a real
    /// <c>Map.SurfaceExcursion</c> rather than from the function that answers for it, because the bug this
    /// is watching for is an excursion that knows about the bottle and an instrument that does not.
    ///
    /// <para><b>Proven RED</b> by giving <c>AirSeconds</c> back its old
    /// <c>= SuitAir.TankSeconds</c> initialiser: the suit stepped out half full onto ground that reached
    /// twice as far.</para>
    /// </summary>
    [Fact]
    public void TheExcursionBuiltWithABottleIsTwiceTheWalk()
    {
        Type exType = typeof(Pages.Map).GetNestedType(
            "SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;

        object plain = Activator.CreateInstance(exType, nonPublic: true)!;
        object fitted = Activator.CreateInstance(exType, nonPublic: true)!;
        exType.GetProperty("ExtendedTank")!.SetValue(fitted, true);

        double plainBudget = (double)exType.GetProperty("AirBudgetSeconds")!.GetValue(plain)!;
        double fittedBudget = (double)exType.GetProperty("AirBudgetSeconds")!.GetValue(fitted)!;

        Assert.Equal(SuitAir.PlayBudget(extendedTank: false), plainBudget, 6);
        Assert.Equal(plainBudget * SuitAir.ExtendedTankFactor, fittedBudget, 6);

        // The suit steps out FULL of whatever it is carrying — not full of a standard bottle.
        Assert.Equal(plainBudget, (double)exType.GetProperty("AirSeconds")!.GetValue(plain)!, 6);
        Assert.Equal(fittedBudget, (double)exType.GetProperty("AirSeconds")!.GetValue(fitted)!, 6);

        // …and the ground under it reaches as far as the suit is allowed to walk.
        Assert.Equal(
            SurfaceTiles.BackstopRadiusDu(plainBudget) * SuitAir.ExtendedTankFactor,
            SurfaceTiles.BackstopRadiusDu(fittedBudget), 6);
    }

    // ── reflection ───────────────────────────────────────────────────────────────────────────────────────

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"{o.GetType().Name} has no field {field}."))
        .SetValue(o, value);

    private static object? Get(object o, string name)
    {
        for (Type? t = o.GetType(); t is not null; t = t.BaseType)
        {
            if (t.GetField(name, Hidden | BindingFlags.Public) is { } f)
            {
                return f.GetValue(o);
            }
            if (t.GetProperty(name, Hidden | BindingFlags.Public) is { } p)
            {
                return p.GetValue(o);
            }
        }
        throw new InvalidOperationException($"{o.GetType().Name} has no {name}.");
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard reads a dead name."))
        .Invoke(map, args);
}
