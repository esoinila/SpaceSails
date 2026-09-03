using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #455 rule 4 · <b>A HOARD OUTLIVES ITS CAPTAIN.</b>
///
/// <para>Owner, live 2026-07-27: <i>"after pirate insurance rebirth you can come see if your loot is still
/// there (probably is if buried), a harder dice roll for treasure just dropped."</i> The issue calls this
/// the missing half of the insurance captain (#398): you die, you come back as somebody new, and
/// <b>your predecessor's cache is still in the ground waiting.</b> The new captain inherits nothing — that
/// is the point of the roster's 🪦 line — <i>except</i> what the old one buried well.</para>
///
/// <h3>Why this is a test and not a paragraph</h3>
/// <para>The audit on #455 BELIEVED the vault already carried hoards through death: <c>BustedResurrect</c>
/// visibly wipes the purse, the hold and every upgrade and never mentions the cache ledger, so the hoard
/// "obviously" survives. That is exactly the reasoning that has cost this project the most — #648 found the
/// discovery watch's own bookmark quietly dying on every Resume for precisely the same "obviously fine"
/// reason, and the chest a captain was promised a dice roll on was never rolled for again. So the claim is
/// DRIVEN through the shipping method on a real booted world rather than read off the source.</para>
///
/// <para>The rebirth is invoked exactly as the wake button invokes it — <c>BustedResurrect</c>, out of the
/// freeze-frame stage the button is gated on — on a <see cref="DeskBench"/>-booted page, and the assertions
/// are the two halves of the promise: everything VISIBLE aboard is gone, and everything IN THE GROUND is
/// not, with its #455 safety terms intact so the successor's return trip rolls the odds his predecessor
/// actually earned.</para>
///
/// <h3>Red proof (watched, quoted in the pull request)</h3>
/// <para>Add <c>_caches.Clear();</c> beside the purse wipe in <c>BustedResurrect</c> and
/// <see cref="ThePredecessorsBuriedChestIsStillInTheGroundAfterTheRebirth"/> fails on the missing chest —
/// which is the shape the bug would have taken had anyone ever "tidied" the rebirth. Drop the safety terms
/// from the mint and the same test fails on the rung, because a chest that comes back reading Exposed is not
/// the safe the dead captain paid for.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AHoardOutlivesItsCaptainTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A berth to die from — the same world the desk sweep boots.</summary>
    private const string ABerth = "/map?dock=selene-gate&body=luna&site=1";

    [Fact]
    public async Task ThePredecessorsBuriedChestIsStillInTheGroundAfterTheRebirth()
    {
        DeskBench bench = await DeskBench.BootAsync(ABerth);
        await bench.RenderAsync(); // the wake ends in StateHasChanged; a page that has never drawn has no handle

        // ── The dead captain's last good decision: a deep carry onto haunted ground. ──
        var caches = (CacheLedger)bench.Peek("_caches")!;
        TreasureCache deep = caches.Bury(
            "phobos", coin: 2400, [new CacheCargo("He3", 4, Hot: true)],
            simTime: 61234.5, owner: "you", playerOwned: true,
            reeverLevel: 3, digX: -6, digY: -232, siteIndex: 1,
            buried: true, padDistance: 205.0);

        Assert.Equal(CacheSafetyRung.Guarded, deep.Safety.Rung);

        // …and a purse and a hold, so "everything visible is gone" is a claim with something behind it.
        bench.Poke("_credits", 7_500);
        bench.Poke("_cargoUnits", 6);

        // ── The death, through the shipping wake. ──
        bench.Poke("_busted", AFreezeFrameDeath());
        bench.CallOnTheDispatcher("BustedResurrect");

        Assert.Empty(bench.EscapedPastTheGate);

        // The visible half: a new captain in an insurance rustbucket, holding nothing of the old one's.
        Assert.NotEqual(7_500, (int)bench.Peek("_credits")!);
        Assert.Equal(0, (int)bench.Peek("_cargoUnits")!);

        // The half this issue is about: the chest is exactly where the dead man left it.
        var after = (CacheLedger)bench.Peek("_caches")!;
        TreasureCache inherited = Assert.Single(after.Caches, c => c.Id == deep.Id);

        Assert.Equal(2400, inherited.Coin);
        Assert.Equal(4, inherited.TotalCargoUnits);
        Assert.Equal(1, inherited.SiteIndex);
        Assert.Equal(-232, inherited.DigY);

        // …and it is still the SAFE he paid for — the three terms, and therefore the roll his successor's
        // return trip will be made against, survive the succession unchanged.
        Assert.Equal(true, inherited.Buried);
        Assert.Equal(205.0, inherited.PadDistance);
        Assert.Equal(3, inherited.ReeverLevel);
        Assert.Equal(deep.Safety, inherited.Safety);
        Assert.Equal(CacheSafetyRung.Guarded, inherited.Safety.Rung);
        Assert.Equal("Nobody sane digs here. That is the whole of the safe.", inherited.Safety.Line);
    }

    /// <summary>THE VACUITY PAIR for the test above: a rebirth that inherited a hoard however the chest was
    /// left would be no promise at all. So the same wake is driven with an EXPOSED chest in the ground, and
    /// the successor inherits it reading exposed — the record comes back as it went in, both ways, which is
    /// what proves the first test is watching the chest rather than a constant.</summary>
    [Fact]
    public async Task AnExposedChestIsInheritedExposed()
    {
        DeskBench bench = await DeskBench.BootAsync(ABerth);
        await bench.RenderAsync(); // the wake ends in StateHasChanged; a page that has never drawn has no handle

        var caches = (CacheLedger)bench.Peek("_caches")!;
        TreasureCache open = caches.Bury(
            "phobos", coin: 300, [], simTime: 61234.5, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: -7, digY: -30, siteIndex: 1,
            buried: false, padDistance: 3.0);

        Assert.Equal(CacheSafetyRung.Exposed, open.Safety.Rung);

        bench.Poke("_busted", AFreezeFrameDeath());
        bench.CallOnTheDispatcher("BustedResurrect");

        Assert.Empty(bench.EscapedPastTheGate);

        var after = (CacheLedger)bench.Peek("_caches")!;
        TreasureCache inherited = Assert.Single(after.Caches, c => c.Id == open.Id);

        Assert.Equal(false, inherited.Buried);
        Assert.Equal(CacheSafetyRung.Exposed, inherited.Safety.Rung);
        Assert.Equal("It lies where anyone can see it, on ground anyone would walk.", inherited.Safety.Line);

        // …and the two hiding places really do price differently, or neither assertion above means anything.
        Assert.True(inherited.Safety.ChancePerMille
            > CacheSafety.Read(205.0, buried: true, 3).ChancePerMille);
    }

    /// <summary>A death at the freeze-frame — the one stage the wake button is gated on. Built by reflection
    /// because <c>BustedEncounter</c> is Map's own private nested state; a renamed stage or a renamed field
    /// fails loudly here with the name it used to have rather than quietly waking nothing.</summary>
    private static object AFreezeFrameDeath()
    {
        Type encounter = typeof(SpaceSails.Client.Pages.Map)
            .GetNestedType("BustedEncounter", BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("Map has no BustedEncounter — the death machinery has moved.");
        Type stage = encounter.GetNestedType("Stage", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BustedEncounter has no Stage — the wake's own gate has moved.");

        object death = Activator.CreateInstance(encounter, nonPublic: true)!;
        Set(encounter, death, "HunterId", "no-such-hunter");
        Set(encounter, death, "HunterCallsign", "The Debt Collector");
        Set(encounter, death, "Heat", 3);
        Set(encounter, death, "Seed", 12345UL);
        Set(encounter, death, "Phase", Enum.Parse(stage, "FreezeFrame"));
        return death;
    }

    private static void Set(Type owner, object target, string property, object? value) =>
        (owner.GetProperty(property, Hidden)
         ?? throw new InvalidOperationException($"BustedEncounter has no {property} any more."))
        .SetValue(target, value);
}
