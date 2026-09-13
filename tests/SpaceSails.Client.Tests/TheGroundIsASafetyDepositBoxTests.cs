using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #319 slice 1 · <b>BURY ANYTHING LIGHT — the played half.</b>
///
/// <para>The Core suite proves the rule (one weight threshold off the satchel's own arithmetic, the same
/// <see cref="CacheSafety"/> read, the same vault). This one proves the GAME: the errand walked from the
/// chooser's row to the hole in the ground and back out of it, on the shipping page, through the shipping
/// methods, with the shipping world under the boots.</para>
///
/// <h3>The claim that matters, and how it is held</h3>
/// <para>The issue's clause 2: <i>"A buried item is OFF THE SHIP: an inspection cannot find it, the heat it
/// carried aboard no longer rides the hull … table every reader that asks 'what evidence is aboard' and make
/// sure a buried item is absent from each through the one satchel it left."</i></para>
///
/// <para>There is no flag in this feature and no reader was taught anything, and
/// <see cref="EveryReaderThatAsksWhatHeIsCarryingLosesTheBuriedThing"/> is why that is not a gap: everything
/// in this game that asks what the captain has on him asks <c>_satchel</c>, so a row that has left it is
/// absent from all of them at once. The guard is a TABLE — twelve readers, each driven with the very object
/// it answers about, each asked before the shovel and after it. A reader that had grown its own copy of the
/// pocket, or a bury that took the thing out of the cache instead of out of the coat, fails here.</para>
///
/// <h3>Red proof (watched, quoted in the pull request)</h3>
/// <list type="bullet">
/// <item>Drop the <c>Satchel.Remove</c> out of <c>TakeTheThingOutOfTheCoat</c> — every one of the twelve rows
/// fails: the thing is in the hole AND still on him.</item>
/// <item>Revert <c>SurfaceGroundInteract</c>'s branch to <c>ex.Carrying</c> — the walked scene sinks a PROBE
/// hole instead of a burial.</item>
/// <item>Drop the <c>when !ex.Carrying</c> arm off the press line — the register is never said.</item>
/// <item>Mint the deposit-only hole with <c>buried: null</c> — the ✗ comes back Exposed.</item>
/// <item>Hand the lift's recovery straight to <c>Satchel.Add</c> without <c>CanTake</c> — the refused row is
/// destroyed instead of staying in the ground.</item>
/// <item>Clear <c>_caches</c> in <c>BustedResurrect</c> — the successor inherits nothing, chest or coat.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGroundIsASafetyDepositBoxTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

    /// <summary>Boots docked at The Tilt and lands on Miranda — the surface world the desk sweep's own matrix
    /// names, so this suite is walking ground something else already proves is real.</summary>
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    // ── THE READER TABLE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>One reader that asks what the captain has on him: what to call it, the object it answers
    /// about, and how to ask it.</summary>
    /// <param name="ThroughTheChipsOwnDoor">#233 ships one hand-built case of this verb — the chip becomes a
    /// HOT line on the chest's cargo manifest rather than a row in the coat's list, and it must go on doing
    /// exactly that (TheChipIsListedOnceEvenWhenItIsThePick). The two rows driven with the chip therefore
    /// look for it in the manifest; every other row looks for it in the coat's list. What is identical for
    /// all twelve, which is what this table is about, is that it is NOT ON HIM any more.</param>
    private sealed record Reader(
        string What, Satchel.Item Thing, Func<DeskBench, bool> Sees, bool ThroughTheChipsOwnDoor = false);

    /// <summary>
    /// <b>EVERY READER THAT ASKS WHAT HE IS CARRYING, ASKED BEFORE THE SHOVEL AND AFTER IT.</b>
    ///
    /// <para>Each row is driven with the very object that reader exists to find, so none of them can pass by
    /// being asked a question it never answers — an inspection table full of readers shrugging at a thing they
    /// were never going to see would be this repo's fifth named bug class wearing an audit's clothes. The
    /// "before" half of every row is what makes the "after" half worth anything.</para>
    /// </summary>
    [Fact]
    public async Task EveryReaderThatAsksWhatHeIsCarryingLosesTheBuriedThing()
    {
        foreach (Reader reader in TheReaders())
        {
            DeskBench bench = await AshoreAsync();
            Poke(bench, "_satchel", new List<Satchel.Item> { reader.Thing });

            Assert.True(reader.Sees(bench), $"{reader.What} cannot see it even before the shovel — this row is asking a question it never answers.");

            TreasureCache hole = BuryTheThing(bench, reader.Thing);

            Assert.False(reader.Sees(bench), $"{reader.What} still finds it after it went in the ground.");
            Assert.Empty((List<Satchel.Item>)Peek(bench, "_satchel")!);

            // …and it EXISTS, in the ground. Which half of the chest's manifest it landed in is #233's
            // business (see the param note above); that it is down there is this table's.
            Assert.True(hole.HasContents, $"{reader.What}'s thing left the coat and is in no hole at all.");
            if (reader.ThroughTheChipsOwnDoor)
            {
                Assert.Equal(CompromisingChip.Name, Assert.Single(hole.Cargo).CargoClass);
            }
            else
            {
                Assert.Equal([reader.Thing], hole.Deposit!.ToArray());
            }
            Assert.Empty(bench.EscapedPastTheGate);
        }
    }

    /// <summary>The table. Twelve readers, each with the object it is about.</summary>
    private static IEnumerable<Reader> TheReaders()
    {
        yield return new("the bin's rip list", AFileOnSomebody,
            b => Call<List<Satchel.Item>>(b, "BinnableFinds").Count > 0);

        yield return new("the desk's spread", APaper,
            b => Call<List<Satchel.Item>>(b, "SpreadableFinds").Count > 0);

        yield return new("the blackmail client's pocket read", TheChip,
            b => Peek(b, "_satchel") is List<Satchel.Item> s && CompromisingChip.InThePocket(s) is not null,
            ThroughTheChipsOwnDoor: true);

        yield return new("the black-ops key the wake reaches for", AKey,
            b => (bool)Get(b, "CarryingABlackOpsKey")!);

        yield return new("the inspectorate's own card", Inspectorate.Card,
            b => Inspectorate.Held(Pocket(b)));

        yield return new("the authorities the lift reads off you", AnAuthority,
            b => Call<HashSet<string>>(b, "AuthorityCardIds").Count > 0);

        yield return new("a guard's badge check on his beat", PatrolBeat.Badge("miranda"),
            b => PatrolBeat.BadgeHeld("miranda", Pocket(b)));

        yield return new("the mess-hall cover a chit buys", CanteenTable.Chit(underAnotherName: true),
            b => CanteenTable.Cover.Held(Pocket(b)));

        yield return new("the wallet a locked door is offered", AnAuthority,
            b => Call<IReadOnlyList<Satchel.Item>>(b, "Wallet").Count > 0);

        yield return new("what can go on a table in a bar", TheChip,
            b => Call<IReadOnlyList<Satchel.Item>>(b, "TableShowables").Count > 0,
            ThroughTheChipsOwnDoor: true);

        yield return new("the rounds a hand-load counts", ARound,
            b => Satchel.CountOf(Pocket(b), Satchel.Kind.Rounds, ARound.Id) > 0);

        yield return new("the vault's own satchel section", AFileOnSomebody,
            b => b.CallOnTheDispatcher("BuildVault", "", "") is Vault v && v.Satchel is { Items.Count: > 0 });
    }

    // ── THE SAME SCENE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>IT IS THE SAME SCENE, NOT A PARALLEL PATH.</b> The issue: <i>"the walk, the DIG HERE press, the 2D6
    /// watchdogs, the ✗ filing and the return dig are the SAME scene (no parallel path)."</i>
    ///
    /// <para>So the press is the shipping press — <c>SurfaceGroundInteract</c>, the bare-ground [E] — with
    /// nothing in the sling but a file in the coat, and what must come out of it is a BURY channel with the
    /// watchdogs already rolled at its start, exactly as a chest's is. Before #319 the same press on the same
    /// ground sank a probe hole.</para>
    /// </summary>
    [Fact]
    public async Task ThePressWithNothingButAFileInTheCoatOpensTheSameBuryChannel()
    {
        DeskBench bench = await AshoreAsync();
        Poke(bench, "_satchel", new List<Satchel.Item> { AFileOnSomebody });
        object ex = StandOnDiggableGround(bench);
        Set(ex, "PendingDeposit", (Satchel.Item?)AFileOnSomebody);

        // The premise: no chest. Whatever the press does, it does it for the thing in the coat.
        Assert.False((bool)Get(ex, "Carrying")!);
        Assert.True((bool)Get(ex, "ShovelHasSomethingToBury")!);

        bench.CallOnTheDispatcher("SurfaceGroundInteract");

        object channel = Get(ex, "Channel")!;
        Assert.NotNull(channel);
        Assert.Equal("Bury", Get(channel, "Kind")!.ToString());
        Assert.True((bool)Get(channel, "Rolled")!);          // the 2D6 turned out at channel start, as a chest's does
        Assert.NotNull(Get(channel, "Roll"));
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>THE CAPTAIN'S REGISTER, AT THE PRESS. Fable canon, verbatim — said when the thing going in is
    /// not the chest, and NOT said when it is, because "Not coin this time" over a chest full of coin is the
    /// sentence and the sim disagreeing.</summary>
    [Fact]
    public async Task TheRegisterIsSaidWhenTheThingGoingInIsNotTheChest()
    {
        DeskBench withAFile = await AshoreAsync();
        Poke(withAFile, "_satchel", new List<Satchel.Item> { AFileOnSomebody });
        object coat = StandOnDiggableGround(withAFile);
        Set(coat, "PendingDeposit", (Satchel.Item?)AFileOnSomebody);
        withAFile.CallOnTheDispatcher("SurfaceGroundInteract");

        Assert.Contains(CacheDeposit.NotCoinThisTime, Pulse(withAFile), StringComparison.Ordinal);

        DeskBench withAChest = await AshoreAsync();
        object sling = StandOnDiggableGround(withAChest);
        Set(sling, "PendingCoin", 1200);
        withAChest.CallOnTheDispatcher("SurfaceGroundInteract");

        Assert.DoesNotContain(CacheDeposit.NotCoinThisTime, Pulse(withAChest), StringComparison.Ordinal);
        Assert.Contains("bury the chest", Pulse(withAChest), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>THE ✗ FILES, AND IT IS THE CHEST'S OWN ✗ — the same ledger, the same site, the same three
    /// safety terms, the same odds a chest with that walk behind it would have earned.</summary>
    [Fact]
    public async Task TheHoleFilesTheChestsOwnMarkWithTheChestsOwnTerms()
    {
        DeskBench bench = await AshoreAsync();
        Poke(bench, "_satchel", new List<Satchel.Item> { AFileOnSomebody });

        TreasureCache hole = BuryTheThing(bench, AFileOnSomebody);
        var ledger = (CacheLedger)Peek(bench, "_caches")!;

        Assert.Equal(hole.Id, Assert.Single(ledger.Caches).Id);   // one ledger, one row
        Assert.True(hole.PlayerOwned);
        Assert.Equal(true, hole.Buried);
        Assert.NotNull(hole.PadDistance);
        Assert.NotNull(hole.SiteIndex);
        Assert.True(hole.HasDigSpot);

        // The oracle, called with the hole's own three terms, is what the map card shows and what the watch
        // will roll against — one arithmetic, exactly as a chest gets.
        Assert.Equal(
            CacheSafety.Read(hole.PadDistance, hole.Buried, hole.ReeverLevel),
            hole.Safety);
        Assert.NotEqual(CacheSafetyRung.Exposed, hole.Safety.Rung); // a shovel went in, so it is not lying in the open
    }

    /// <summary>THE RETURN DIG HANDS IT BACK — through the same ✗, the same [E], the same lift.</summary>
    [Fact]
    public async Task TheReturnDigPutsItBackInTheCoat()
    {
        DeskBench bench = await AshoreAsync();
        Poke(bench, "_satchel", new List<Satchel.Item> { AFileOnSomebody });
        TreasureCache hole = BuryTheThing(bench, AFileOnSomebody);
        Assert.Empty(Pocket(bench));

        LiftTheHole(bench, hole);

        Assert.Equal([AFileOnSomebody], Pocket(bench).ToArray());
        Assert.Empty(((CacheLedger)Peek(bench, "_caches")!).Caches);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>…AND A POCKET WITH NO ROOM LEAVES IT IN THE GROUND rather than destroying it — #678's law,
    /// which this repo has already paid for once: <i>"If refused the item should stay where it was
    /// investigated last — not disappear like they do now, or seem to."</i> The refused row goes back into a
    /// hole at the same spot, under the same terms.</summary>
    [Fact]
    public async Task AThingThePocketWillNotTakeStaysInTheGround()
    {
        DeskBench bench = await AshoreAsync();
        var kit = new Satchel.Item(Satchel.Kind.Tool, "a-spare-rig");
        Poke(bench, "_satchel", new List<Satchel.Item> { kit });
        TreasureCache hole = BuryTheThing(bench, kit);

        // Come back with the pockets stuffed: twelve distinct bulky things, which is exactly the cap.
        Poke(bench, "_satchel", new List<Satchel.Item>(
            Enumerable.Range(0, Satchel.PocketCapacity).Select(i => new Satchel.Item(Satchel.Kind.Tool, $"rig-{i}"))));

        LiftTheHole(bench, hole);

        Assert.DoesNotContain(kit, Pocket(bench));
        var ledger = (CacheLedger)Peek(bench, "_caches")!;
        TreasureCache stillDown = Assert.Single(ledger.Caches);
        Assert.Equal([kit], stillDown.Deposit!.ToArray());
        Assert.Equal(hole.DigX, stillDown.DigX);
        Assert.Equal(hole.Safety, stillDown.Safety);   // the same odds it was under a second ago
        Assert.Contains("stays down there", Pulse(bench), StringComparison.Ordinal);
    }

    // ── THE SUCCESSION ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>A REBIRTH CAPTAIN INHERITS IT EXACTLY AS HE INHERITS THE CHEST</b> (#455 rule 4). The dead man's
    /// purse, hold and pocket are gone; what he put in the ground is not, and it is still under the terms he
    /// earned.
    ///
    /// <para>Driven through the shipping wake, on the same freeze-frame the button is gated on, with a chest
    /// and a coat-thing buried side by side so "inherits it EXACTLY as the chest is inherited" is a comparison
    /// and not an assertion about one of them.</para>
    /// </summary>
    [Fact]
    public async Task ThePredecessorsBuriedFileIsInheritedExactlyAsHisChestIs()
    {
        DeskBench bench = await AshoreAsync();
        var caches = (CacheLedger)Peek(bench, "_caches")!;
        TreasureCache chest = caches.Bury(
            "phobos", 2400, [new CacheCargo("He3", 4, Hot: true)], 61234.5, "you", true,
            reeverLevel: 3, digX: -6, digY: -232, siteIndex: 1, buried: true, padDistance: 205.0);
        TreasureCache coat = caches.Bury(
            "phobos", 0, [], 61234.5, "you", true,
            reeverLevel: 3, digX: -6, digY: -232, siteIndex: 1, buried: true, padDistance: 205.0,
            deposit: [AFileOnSomebody]);

        Poke(bench, "_satchel", new List<Satchel.Item> { APaper });
        bench.Poke("_credits", 7_500);
        bench.Poke("_busted", AFreezeFrameDeath(bench));
        bench.CallOnTheDispatcher("BustedResurrect");
        Assert.Empty(bench.EscapedPastTheGate);

        // Everything the dead man had ON him is gone — which is what makes the ground's half mean something.
        Assert.NotEqual(7_500, (int)bench.Peek("_credits")!);

        var after = (CacheLedger)Peek(bench, "_caches")!;
        TreasureCache chestNow = Assert.Single(after.Caches, c => c.Id == chest.Id);
        TreasureCache coatNow = Assert.Single(after.Caches, c => c.Id == coat.Id);

        Assert.Equal(2400, chestNow.Coin);
        Assert.Equal([AFileOnSomebody], coatNow.Deposit!.ToArray());
        // The parity, stated as a parity: the two holes come through the succession reading the same safe.
        Assert.Equal(chestNow.Safety, coatNow.Safety);
        Assert.Equal(true, coatNow.Buried);
        Assert.Equal(205.0, coatNow.PadDistance);
    }

    // ── THE CHOOSER ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE ROW IS OFFERED ONLY WHEN THERE IS SOMETHING TO OFFER, and it is the satchel's own rows in
    /// the satchel's own order. An empty pocket grows no control; a full one is never left unsaid (#212).</summary>
    [Fact]
    public async Task TheChooserOffersTheRowOnlyWhenSomethingLightIsCarried()
    {
        DeskBench bench = await AshoreAsync();

        Assert.Empty((IReadOnlyList<Satchel.Item>)Get(bench, "BuriableInTheSatchel")!);

        List<Satchel.Item> pocket = [AFileOnSomebody, APaper, ARound];
        Poke(bench, "_satchel", pocket);
        Assert.Equal(pocket, ((IReadOnlyList<Satchel.Item>)Get(bench, "BuriableInTheSatchel")!).ToArray());
    }

    /// <summary>PICKING IS UNDOABLE IN THE ROW ITSELF — press it again and it goes back in the coat. One at a
    /// time, deliberately: the hole is a decision about a single object.</summary>
    [Fact]
    public async Task PressingThePickedRowAgainTakesThePickBack()
    {
        DeskBench bench = await AshoreAsync();
        Poke(bench, "_satchel", new List<Satchel.Item> { AFileOnSomebody, APaper });

        bench.CallOnTheDispatcher("ChooseBoardDeposit", AFileOnSomebody);
        Assert.Equal(AFileOnSomebody, (Satchel.Item?)Peek(bench, "_boardDeposit"));

        bench.CallOnTheDispatcher("ChooseBoardDeposit", APaper);   // one at a time
        Assert.Equal(APaper, (Satchel.Item?)Peek(bench, "_boardDeposit"));

        bench.CallOnTheDispatcher("ChooseBoardDeposit", APaper);   // …and pressing it again takes it back
        Assert.Null((Satchel.Item?)Peek(bench, "_boardDeposit"));

        // …and closing the panel does not leave yesterday's answer behind it.
        bench.CallOnTheDispatcher("ChooseBoardDeposit", APaper);
        bench.CallOnTheDispatcher("CancelBoarding");
        Assert.Null((Satchel.Item?)Peek(bench, "_boardDeposit"));
    }

    /// <summary>THE PICK REALLY REACHES THE GROUND. A chooser nobody's boarding reads is a feature that only
    /// exists in a test, so the two shipping seams are asked by name: the boarding panel packs it, and the
    /// excursion carries it.</summary>
    [Fact]
    public void TheBoardingRouteIsWhereThePickJoinsTheWalk()
    {
        string boarding = BodyOf("Map.Docking.Boarding.cs", "private async Task ConfirmBoarding(");
        Assert.Contains("ShuttleExcursion.Pack(_boardCoin, _credits, hold, _boardDeposit)", boarding, StringComparison.Ordinal);

        string descent = File.ReadAllText(ClientFile("Map.Surface.Boarding.cs"));
        Assert.Contains("PendingDeposit = chest.Deposit,", descent, StringComparison.Ordinal);

        // …and the shovel really spends it: the bury calls the one place a satchel row leaves the pocket for
        // a hole, and calls it AFTER the hold has been settled, for the reason #233's chip does.
        string bury = BodyOf("Map.Surface.Dig.cs", "private void BuryChestHere(");
        Assert.Contains("TakeTheThingOutOfTheCoat(ex)", bury, StringComparison.Ordinal);
        Assert.True(
            bury.IndexOf("ShuttleExcursion.HoldAfterBurying(", StringComparison.Ordinal)
            < bury.IndexOf("TakeTheThingOutOfTheCoat(", StringComparison.Ordinal),
            "the coat is emptied before the hold is settled — the hold will be asked to give up a unit it never had.");
    }

    /// <summary>#233'S CHIP IS NOT BURIED TWICE. A captain who picks the chip itself has it taken by the
    /// blackmail path, listed on the manifest by its canon name and flagged hot exactly as that issue ships
    /// it, and the coat is empty when the general path asks. One object, one hole, one entry.</summary>
    [Fact]
    public async Task TheChipIsListedOnceEvenWhenItIsThePick()
    {
        DeskBench bench = await AshoreAsync();
        Poke(bench, "_satchel", new List<Satchel.Item> { TheChip });

        TreasureCache hole = BuryTheThing(bench, TheChip);

        Assert.Null(hole.Deposit);
        CacheCargo line = Assert.Single(hole.Cargo);
        Assert.Equal(CompromisingChip.Name, line.CargoClass);
        Assert.True(line.Hot);
        Assert.Empty(Pocket(bench));
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    private static readonly Satchel.Item AFileOnSomebody = new(Satchel.Kind.Dirt, "a-file-on-somebody");
    private static readonly Satchel.Item APaper = new(Satchel.Kind.Paper, "a-shipping-manifest");
    private static readonly Satchel.Item ARound = new(Satchel.Kind.Rounds, "loose", 6);
    private static readonly Satchel.Item TheChip = CompromisingChip.Found();
    private static readonly Satchel.Item AnAuthority = new(Satchel.Kind.Authority, "hive:miranda:-3:7");
    private static readonly Satchel.Item AKey = new(Satchel.Kind.BlackOpsKey, "wreck-kestrel");

    private static async Task<DeskBench> AshoreAsync()
    {
        DeskBench bench = await DeskBench.BootAsync(Ashore);
        await bench.RenderAsync();
        Assert.NotNull(Peek(bench, "_surface"));
        return bench;
    }

    /// <summary>Put the boots on regolith the shovel will take — out past the landing band, off the fused pad,
    /// asked of the client's own law rather than of a coordinate somebody liked the look of.</summary>
    private static object StandOnDiggableGround(DeskBench bench)
    {
        SurfaceLayout.Field f = SurfaceLayout.DefaultField;
        double x = f.HomeX, y = f.LandingBandY - 40;
        Assert.True(Client.Rendering.MoonSurface.IsDiggableGround(x, y, 0),
            "the bench is standing somewhere the shipping game says no shovel works — this helper has drifted.");
        bench.Poke("_avatarX", x);
        bench.Poke("_avatarY", y);
        object ex = Peek(bench, "_surface")!;
        Set(ex, "Floor", 0);
        return ex;
    }

    /// <summary>Walk the whole burial for a thing in the coat, through the shipping bury.</summary>
    private static TreasureCache BuryTheThing(DeskBench bench, Satchel.Item thing)
    {
        object ex = StandOnDiggableGround(bench);
        Set(ex, "PendingDeposit", (Satchel.Item?)thing);

        SurfaceLayout.Field f = SurfaceLayout.DefaultField;
        bench.CallOnTheDispatcher("BuryChestHere", ex, AQuietRoll(), f.HomeX, f.LandingBandY - 40);

        var ledger = (CacheLedger)Peek(bench, "_caches")!;
        return ledger.Caches.First(c => c.PlayerOwned);
    }

    private static void LiftTheHole(DeskBench bench, TreasureCache hole)
    {
        object ex = StandOnDiggableGround(bench);
        bench.CallOnTheDispatcher("LiftChestHere", ex, hole.Id, AQuietRoll());
    }

    /// <summary>A 2D6 that roused nothing — the watchdogs are not this file's subject, and a pack shambling
    /// in would be noise in every assertion below.</summary>
    private static ReeverRoll AQuietRoll() => new(1, 1, [], 0UL);

    private static IReadOnlyList<Satchel.Item> Pocket(DeskBench bench) =>
        (List<Satchel.Item>)Peek(bench, "_satchel")!;

    /// <summary>The line the game just said. Through the bench's own reader, which is the whole
    /// <c>PulseSlot</c> rendered — a held higher-ranked line is part of what the captain is looking at.</summary>
    private static string Pulse(DeskBench bench) => bench.Pulse;

    private static object? Peek(DeskBench bench, string field) => bench.Peek(field);

    private static void Poke(DeskBench bench, string field, object? value) => bench.Poke(field, value);

    private static T Call<T>(DeskBench bench, string method) => (T)bench.CallOnTheDispatcher(method)!;

    /// <summary>One member off the page by name, property or field, so the helpers above can ask the shipping
    /// page a question without caring which shape the answer is kept in.</summary>
    private static object? Get(DeskBench bench, string member) =>
        typeof(Pages.Map).GetProperty(member, Hidden)?.GetValue(TheMap(bench))
        ?? typeof(Pages.Map).GetField(member, Hidden)?.GetValue(TheMap(bench));

    private static object? Get(object instance, string member) =>
        instance.GetType().GetProperty(member, Hidden)?.GetValue(instance)
        ?? instance.GetType().GetField(member, Hidden)?.GetValue(instance);

    private static void Set(object instance, string member, object? value)
    {
        PropertyInfo? p = instance.GetType().GetProperty(member, Hidden);
        if (p is { CanWrite: true })
        {
            p.SetValue(instance, value);
            return;
        }
        FieldInfo f = instance.GetType().GetField(member, Hidden)
            ?? throw new MissingMemberException(instance.GetType().Name, member);
        f.SetValue(instance, value);
    }

    private static Pages.Map TheMap(DeskBench bench) =>
        (Pages.Map)typeof(DeskBench).GetField("_map", TheBenchsOwn)!.GetValue(bench)!;

    private const BindingFlags TheBenchsOwn = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>A death parked at the freeze frame — the one stage the wake button is gated on.</summary>
    private static object AFreezeFrameDeath(DeskBench bench)
    {
        Type busted = typeof(Pages.Map).GetNestedType("BustedEncounter", Hidden)!;
        object b = Activator.CreateInstance(busted)!;
        Type stage = busted.GetNestedType("Stage", Hidden)!;
        Set(b, "Phase", Enum.Parse(stage, "FreezeFrame"));
        _ = bench;
        return b;
    }

    private static string ClientFile(string name) =>
        Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", name);

    private static string BodyOf(string file, string signature)
    {
        string source = File.ReadAllText(ClientFile(file));
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{signature} has been renamed — this guard has drifted.");
        int ends = source.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.True(ends > at, $"{signature} has no end — this guard has drifted.");
        return source[at..ends];
    }
}
