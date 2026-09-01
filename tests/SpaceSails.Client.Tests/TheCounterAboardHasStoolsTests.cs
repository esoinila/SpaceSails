using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1040 · <b>STOOLS AT THE BOAT'S OWN COUNTER — and the cantina catches up to the haven-bar standard.</b>
///
/// <para><b>Owner, on 7 Deck, with the room in front of him:</b> <i>"Our on ship bar can be upgraded to
/// match the other bars... the UI represents code long time ago."</i> He is describing a room drawn before
/// any of the furniture laws existed: three rings on an empty floor, the galley console standing in the
/// middle of them, and — the thing the sentence is really about — <b>no counter at all</b>, in a room whose
/// own backdrop (<c>art/the-space-bar.jpg</c>) is a photograph of a counter with a row of stools down it.
/// A haven bar has had all of it since #247: a counter that is a real wall you belly up to, a service point
/// on the players' side, seats along it, and tables set away from it.</para>
///
/// <para><b>And the stools are the point, not the decoration.</b> A stool seats the captain on the
/// <see cref="SeatedHud.Seat.BarStool"/> rung, which is where the gumshoe rule lives — the case does not
/// come out at a bar. So the man who cannot work his case at The Stormwatch Bar cannot work it at his own
/// bar either, and the refusal is SAID (#603) rather than being a control that quietly does nothing. That
/// is intended, and the issue says so: it is the existing rung reached by the existing funnel, with zero new
/// predicates.</para>
///
/// <h3>What is guarded, and why each half is a different question</h3>
///
/// <list type="number">
/// <item><b>The row is real furniture.</b> Every stool is inside the CANTINA compartment, on a square a
/// body fits on, exactly one body-width off the counter's own face, and the captain can WALK to it from
/// where he wakes up. Collision and reachability are not the same question (a seat sealed inside a room
/// reads as perfectly clear ground to a radius test), so both are asked.</item>
/// <item><b>The press is the shipped press.</b> [E] at the counter opens a sitting through
/// <c>Seating.TakeThisSeat</c> — the one method — and the sitting it opens answers <c>BarStool</c>.</item>
/// <item><b>The rung does what a rung does.</b> The spread is refused, out loud, in the shipped sentence.</item>
/// <item><b>CABIN 2 has the desk CABIN 1 has</b>, placed by the same rule, and its chair still answers the
/// desk rather than the berth next door.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCounterAboardHasStoolsTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The box the walk is searched in — her hull, with room to spare on every side.</summary>
    private static (double MinX, double MinY, double MaxX, double MaxY) HerBounds => (-26, -12, 32, 16);

    // ── THE ROW IS REAL FURNITURE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HER COUNTER PUBLISHES A ROW OF STOOLS, AND EVERY ONE OF THEM IS SOMEWHERE A BODY CAN BE.
    ///
    /// <para>Four claims about one row, and every number is measured against the room and the counter rather
    /// than against a coordinate this test knows: inside the CANTINA's own bounds, clear of her stone,
    /// exactly a body-width off the counter's face (near enough to be AT it, far enough not to be inside
    /// it), and reachable on foot from the captain's own spawn point.</para>
    ///
    /// <para><b>Proven RED</b> on the pre-change ship — <c>DeckPlan.Ship.Stools</c> was empty, and the first
    /// assertion names the room: <i>"her cantina publishes no stool at all — the counter is back to being a
    /// photograph."</i></para>
    /// </summary>
    [Fact]
    public void HerCounterPublishesAStoolRowABodyCanSitOn()
    {
        DeckPlan ship = DeckPlan.Ship;

        Assert.True(ship.Stools.Length > 0,
            "her cantina publishes no stool at all — the counter is back to being a photograph.");
        Assert.Equal(ShipLayout.CantinaStoolCount, ship.Stools.Length);

        (float cx, float cy0, float _, float cy1) = ShipLayout.CantinaCounter;

        foreach (DeckPlan.StoolSpot stool in ship.Stools)
        {
            // …in the room it says it is in. The compartment lookup is Core's own, so a stool that drifted
            // into the corridor or behind the bridge bulkhead is named as being in the wrong room rather
            // than being merely "a bit off".
            Assert.Equal(ShipLayout.CantinaRoom, ShipLayout.CompartmentAt(stool.X, stool.Y));

            // …on floor. `Blocked` is the very check the captain's own step obeys.
            Assert.False(
                SurfaceCollision.Blocked(stool.X, stool.Y, DeckPlan.AvatarRadius, ship.CollisionField),
                $"the stool at ({stool.X}, {stool.Y}) is inside something solid.");

            // …AT the counter. Measured to the counter's own segment, which is the same two points the wall
            // above it is built from — a stool measured off a second copy of the counter is this ship's
            // named console bug with a seat on it.
            double off = SurfaceCollision.DistanceToSegment(stool.X, stool.Y, cx, cy0, cx, cy1);
            Assert.True(off is > 0.7 and < 2.0,
                $"the stool at ({stool.X}, {stool.Y}) stands {off:0.00} du off the counter — a stool is "
                + "tucked in at a counter, not standing in the middle of the room or inside the woodwork.");

            // …and you can WALK to it. A radius test cannot see a seat that has been sealed into a room, so
            // the flood is asked separately (EverySeatIsSomewhereYouCanSit's own stated lesson).
            Assert.True(
                DeckReachability.CanReach(
                    new DeckReachability.Point(ship.SpawnX, ship.SpawnY),
                    new DeckReachability.Point(stool.X, stool.Y),
                    ship.CollisionField, DeckPlan.AvatarRadius, HerBounds),
                $"the stool at ({stool.X}, {stool.Y}) cannot be walked to from where the captain wakes up.");
        }

        // …and the row is a ROW: no two of them on one square, in the order the row itself publishes.
        for (int i = 1; i < ship.Stools.Length; i++)
        {
            double gap = Math.Abs(ship.Stools[i].Y - ship.Stools[i - 1].Y);
            Assert.True(gap > DeckPlan.AvatarRadius,
                $"stools {i - 1} and {i} are {gap:0.00} du apart — that is one stool drawn twice.");
        }
    }

    /// <summary>
    /// …AND THE SERVERY BEHIND IT IS STILL SOMEWHERE YOU CAN GO. A counter is a wall, and a wall built
    /// across a room can seal half of it off — the never-empty-floor rule with a bar in it. Hers runs from a
    /// body-width clear of the corridor wall into the window wall, so the back of the bar has exactly one
    /// way in, round the near end, and it is a way a body fits through.
    ///
    /// <para><b>Proven RED</b> by running the counter the full depth of the room
    /// (<c>r.Y0</c> instead of <c>r.Y0 + CounterEndGap</c>): the flood cannot reach behind it.</para>
    /// </summary>
    [Fact]
    public void TheBackOfHerBarIsStillWalkable()
    {
        DeckPlan ship = DeckPlan.Ship;
        (float bx0, float by0, float bx1, float by1) = ShipLayout.CantinaBackBar;

        // A point in the servery, in front of the shelving and behind the counter.
        var behind = new DeckReachability.Point(
            bx1 + DeckPlan.AvatarRadius + 0.2, ((by0 + by1) / 2.0) + 1.0);

        Assert.False(
            SurfaceCollision.Blocked(behind.X, behind.Y, DeckPlan.AvatarRadius, ship.CollisionField),
            "the servery is solid — there is nowhere behind her own bar to stand.");
        Assert.True(
            DeckReachability.CanReach(
                new DeckReachability.Point(ship.SpawnX, ship.SpawnY), behind,
                ship.CollisionField, DeckPlan.AvatarRadius, HerBounds),
            "the counter seals the back of the bar off — half the galley is a room nobody can enter.");

        // …and the counter is genuinely a WALL, or none of the above was a claim about anything: a body
        // cannot stand ON its face.
        (float cx, float cy0, float _, float cy1) = ShipLayout.CantinaCounter;
        Assert.True(
            SurfaceCollision.Blocked(cx, (cy0 + cy1) / 2.0, DeckPlan.AvatarRadius, ship.CollisionField),
            "her counter is drawn and does not collide — that is a bar you walk through.");
    }

    /// <summary>
    /// THE ROW IS ONE FIXTURE AND THEREFORE ONE CONSOLE, WITH ITS REACH ALONG THE COUNTER.
    ///
    /// <para>#791's E-bus, from the owner's own B1 complaint — <i>"The Bar desk is really long now, but
    /// there is only one spot to get service on it"</i>. The run is handed down from the counter Core
    /// carved, never measured here, and [E] at every stool in the row lands on this console and not on a
    /// table, the galley card or a hatch control.</para>
    ///
    /// <para><b>Proven RED</b> on the pre-change ship: there is no <c>ShipStool</c> console to find, and
    /// <c>Single</c> throws with the room named.</para>
    /// </summary>
    [Fact]
    public void TheCounterIsOneConsoleWithItsReachAlongItself()
    {
        DeckPlan ship = DeckPlan.Ship;
        DeckPlan.ConsoleSpot counter =
            ship.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.ShipStool);

        Assert.True(counter.IsRun, "the counter answers at one dot — a captain at the far end of it cannot press it.");
        (float cx, float cy0, float _, float cy1) = ShipLayout.CantinaCounter;
        Assert.Equal(((float X, float Y))(cx, cy0), counter.End0);
        Assert.Equal(((float X, float Y))(cx, cy1), counter.End1);
        Assert.Equal(SittingAlone.FreeStoolPlate, counter.Label);

        foreach (DeckPlan.StoolSpot stool in ship.Stools)
        {
            DeckPlan.ConsoleSpot? answering = ship.NearestConsoleSpot(stool.X, stool.Y);
            Assert.NotNull(answering);
            Assert.Equal(DeckPlan.ConsoleKind.ShipStool, answering!.Value.Kind);
        }
    }

    // ── THE PRESS, AND THE RUNG IT LEAVES YOU ON ─────────────────────────────────────────────────────────

    /// <summary>
    /// [E] AT HER COUNTER PUTS THE CAPTAIN ON A STOOL — through the one method every sitting is opened
    /// through, on the shipped <see cref="SeatedHud.Seat.BarStool"/> rung, at the stool he walked up to.
    ///
    /// <para><b>Proven RED</b> two ways: on the pre-change ship there is no console to press at all, and
    /// with <c>Stool = top.Stool</c> dropped from the sitting in <c>Seating.BarTop.cs</c> the press works
    /// and the rung comes back <c>HallTable</c> — a counter the captain can lay a case out on.</para>
    /// </summary>
    [Fact]
    public void PressingHerCounterSitsYouOnTheBarStoolRung()
    {
        Pages.Map map = Aboard();
        Assert.Null(Seated(map));

        IReadOnlyList<DeckReachability.Point> row = ShipLayout.CantinaStools;
        DeckReachability.Point stool = row[^1];
        StandAt(map, stool.X, stool.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "the stool row at her own counter refused [E].");

        object seat = Seated(map)!;
        Assert.True((bool)Get(seat, "Stool")!);
        Assert.True((bool)Get(seat, "Aboard")!);
        Assert.False((bool)Get(seat, "Quiet")!, "a counter has your back to the room; it is not a cabinet.");
        Assert.False((bool)Get(seat, "TheyCameToYou")!);
        Assert.Equal(SittingAlone.OwnStoolPlate, (string)Get(seat, "Plate")!);
        Assert.Equal(ShipLayout.CantinaStoolSeats, (int)Get(seat, "Seats")!);
        Assert.Equal(0, (int)Get(seat, "Free")!);

        // …and it says where it is, in the counter's own words rather than a top's or a canteen's.
        string setting = (string)Get(Get(seat, "Scene")!, "Setting")!;
        Assert.Equal(SittingAlone.ShipCounterSetting, setting);
        Assert.NotEqual(SittingAlone.ShipCantinaSetting, setting);
        Assert.NotEqual(SittingAlone.Setting, setting);

        // The key names the ship, the room and the stool — never a Hive floor's or a berth's.
        Assert.Equal($"ship:cantina:stool:{row.Count - 1}", (string)Get(seat, "Key")!);

        // #820's snap: the body is ON the stool the captain was standing at, and it is the row's own
        // coordinate rather than one this test measured.
        Assert.Equal(stool.X, (double)Field(map, "_avatarX")!, 6);
        Assert.Equal(stool.Y, (double)Field(map, "_avatarY")!, 6);

        // THE RUNG, which is the whole of why a stool is not a top.
        Assert.Equal(SeatedHud.Seat.BarStool, (SeatedHud.Seat?)Invoke(map, "get_SeatedIn"));

        // …and standing up closes it the way every other seat in the game closes.
        Assert.True((bool)Invoke(map, "StandUpBeforeWalking")!);
        Assert.Null(Seated(map));
    }

    /// <summary>
    /// AND YOU GET THE STOOL YOU WALKED UP TO. The console is a RUN, so [E] answers all the way down the
    /// counter; answering with a fixed stool would sit a man at the far end of the bar from where he was
    /// standing, which is the drawn room and the walked room disagreeing.
    ///
    /// <para>Anti-vacuous by construction: it walks the WHOLE row and asserts a different ordinal each
    /// time, so a lookup that always returned the same seat cannot pass.</para>
    /// </summary>
    [Fact]
    public void YouGetTheStoolYouWalkedUpTo()
    {
        IReadOnlyList<DeckReachability.Point> row = ShipLayout.CantinaStools;
        Assert.True(row.Count > 1, "a row of one stool proves nothing about which stool you get.");

        for (int i = 0; i < row.Count; i++)
        {
            Pages.Map map = Aboard();
            StandAt(map, row[i].X, row[i].Y);
            Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
            Assert.Equal(i, (int)Get(Seated(map)!, "Index")!);
        }
    }

    /// <summary>
    /// <b>AND THE GUMSHOE WILL NOT WORK HIS CASE AT HIS OWN BAR EITHER — said out loud.</b>
    ///
    /// <para>Owner's rule, quoted in <see cref="SeatedSpread"/>: <i>"The gumshoe does NOT organize papers at
    /// the bar. They take a shady back-room table."</i> The rung is the shipped rung and the sentence is the
    /// shipped sentence — this lane added no predicate and no second ladder, it put a seat on a rung that
    /// already knew what to say. The refusal being funny aboard is the feature.</para>
    ///
    /// <para>It is proved against the ROOM as well as against the stool: the same captain, four paces away
    /// at one of the same room's tops, is refused nothing. A guard that only showed the counter refusing
    /// would pass just as happily on a ship where the spread was broken everywhere.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>t.Stool</c> arm from <c>SeatedIn</c>: the counter answers
    /// <c>HallTable</c>, the refusal is null, and the case comes out at the bar.</para>
    /// </summary>
    [Fact]
    public void TheCaseDoesNotComeOutAtHerOwnCounter()
    {
        Pages.Map map = OnAStool();
        Invoke(map, "SeedTheSpreadFinds");

        Assert.False((bool)Invoke(map, "get_CanSpreadTheCaseHere")!);
        Assert.Equal(SeatedSpread.NotAtTheBarLine, (string?)Invoke(map, "get_SpreadRefusal"));

        // …and the very same room's tops still open, so this is a fact about the SEAT and not about the boat.
        Pages.Map atATop = Aboard();
        DeckPlan.ConsoleSpot top = ((DeckPlan)Field(atATop, "_deckPlan")!).Consoles
            .First(c => c.Kind == DeckPlan.ConsoleKind.BarTop);
        StandAt(atATop, top.X, top.Y);
        Assert.True((bool)Invoke(atATop, "TryTakeBarTop")!);
        Assert.Null(Invoke(atATop, "get_SpreadRefusal"));
    }

    /// <summary>THE COUNTER'S SILENCE IS THE COUNTER'S. A stool has no chair opposite, and the cantina's own
    /// wait line ends on one — a sentence naming furniture the picture does not have is the fastest lie this
    /// game has ever been caught telling. Both lines of the pool are walked.</summary>
    [Fact]
    public void WaitingAtHerCounterHearsTheCountersOwnSilence()
    {
        Pages.Map map = OnAStool();

        for (int beat = 0; beat < SittingAlone.NobodyCameShipCounter.Count + 1; beat++)
        {
            Wait(map);
            Assert.Equal(
                SittingAlone.NobodyCameAtYourOwnCounter(beat), (string)Get(Seated(map)!, "Outcome")!);
        }

        Assert.NotEqual(
            SittingAlone.NobodyCameAtYourOwnCounter(0), SittingAlone.NobodyCameAboard(cabin: false, 0));
    }

    // ── AND CABIN 2 TAKES A DESK LIKE CABIN 1'S ──────────────────────────────────────────────────────────

    /// <summary>
    /// <b>CABIN 2 HAS A DESK, AND IT IS CABIN 1'S DESK PLACED BY CABIN 1'S RULE.</b>
    ///
    /// <para>Owner, filing #1040: <i>"CABIN 2 could take a desk like CABIN 1's."</i> The two berths are the
    /// same room built twice, so this is one fitting placed by <see cref="ShipLayout.CabinDeskStationIn"/>
    /// in each berth's own corner rather than a second design — and the corner is a decision about the
    /// CHAIR: the hull and the berth divider refuse three of its four sides, so the chair lands inboard and
    /// [E] from it answers the desk instead of putting the captain to bed in the berth next door.</para>
    ///
    /// <para><b>Proven RED</b> by taking CABIN 2 back out of <see cref="ShipLayout.DeskCabins"/>: the deck
    /// publishes one desk and the lookup for the berth's own comes back empty.</para>
    /// </summary>
    [Fact]
    public void CabinTwoTakesADeskLikeCabinOnes()
    {
        DeckPlan ship = DeckPlan.Ship;
        Assert.Contains("CABIN 2", ShipLayout.DeskCabins);

        foreach (string cabin in ShipLayout.DeskCabins)
        {
            DeckReachability.Point at = ShipLayout.CabinDeskStationIn(cabin);

            // The desk is in the berth it claims, and the deck draws one there.
            Assert.Equal(cabin, ShipLayout.CompartmentAt(at.X, at.Y));
            Assert.Contains(ship.Consoles,
                c => c.Kind == DeckPlan.ConsoleKind.ShipDesk
                     && Math.Abs(c.X - at.X) < 1e-6 && Math.Abs(c.Y - at.Y) < 1e-6);

            // …and the chair the stone chooses answers THIS desk, from inside THIS berth.
            DeckReachability.Point chair = HavenInterior.BesideATop(
                at, DeckPlan.AvatarRadius, ship.CollisionField)!.Value;
            Assert.Equal(cabin, ShipLayout.CompartmentAt(chair.X, chair.Y));

            DeckPlan.ConsoleSpot? answering = ship.NearestConsoleSpot(chair.X, chair.Y);
            Assert.NotNull(answering);
            Assert.Equal(DeckPlan.ConsoleKind.ShipDesk, answering!.Value.Kind);
            Assert.Equal(at.X, answering.Value.X, 6);
            Assert.Equal(at.Y, answering.Value.Y, 6);
        }
    }

    /// <summary>
    /// …AND SITTING AT CABIN 2'S DESK IS THE CABINET RUNG, WITH ITS OWN KEY.
    ///
    /// <para>Two desks aboard is two sittings, and a key that could not tell them apart would file both
    /// berths' business in one drawer — which is exactly what "ship:cabin:desk" would have done.</para>
    ///
    /// <para><b>Proven RED</b> by leaving the desk arm keyed on the old constant: both berths answer
    /// <c>ship:cabin:desk</c> and the assertion names the collision.</para>
    /// </summary>
    [Fact]
    public void TheDeskInCabinTwoIsItsOwnSittingOnTheCabinetRung()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        // Anti-vacuous: a key set of one collides with nothing, so this guard only means something while
        // there are two berths to tell apart.
        Assert.True(ShipLayout.DeskCabins.Length >= 2,
            "one desk aboard cannot share a key with another — this guard proves nothing until CABIN 2 has "
            + "one too.");

        foreach (string cabin in ShipLayout.DeskCabins)
        {
            Pages.Map map = Aboard();
            DeckReachability.Point at = ShipLayout.CabinDeskStationIn(cabin);
            StandAt(map, at.X, at.Y);
            Assert.True((bool)Invoke(map, "TryTakeBarTop")!, $"the desk in {cabin} refused [E].");

            object seat = Seated(map)!;
            Assert.True((bool)Get(seat, "Quiet")!);
            Assert.False((bool)Get(seat, "Stool")!);
            Assert.Equal(SittingAlone.OwnDeskPlate, (string)Get(seat, "Plate")!);
            Assert.Equal(SeatedHud.Seat.Cabinet, (SeatedHud.Seat?)Invoke(map, "get_SeatedIn"));
            Assert.True((bool)Invoke(map, "get_CanSpreadTheCaseHere")!,
                $"{cabin} has a door a step away, which is where the papers come out.");

            Assert.Equal(cabin,
                ShipLayout.CompartmentAt((double)Field(map, "_avatarX")!, (double)Field(map, "_avatarY")!));
            Assert.True(keys.Add((string)Get(seat, "Key")!),
                $"{cabin}'s desk shares its key with another berth's — two sittings in one drawer.");
        }

        Assert.Equal(ShipLayout.DeskCabins.Length, keys.Count);
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map Aboard()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_deckMode", true);
        Set(map, "_deckPlan", DeckPlan.Ship);
        return map;
    }

    private static Pages.Map OnAStool()
    {
        Pages.Map map = Aboard();
        DeckReachability.Point stool = ShipLayout.CantinaStools[0];
        StandAt(map, stool.X, stool.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
        return map;
    }

    /// <summary>One press of SIT A WHILE, through the very handler the strip's button is wired to.</summary>
    private static void Wait(Pages.Map map) =>
        ((Task)Invoke(map, "TableMoveClicked", SittingAlone.Wait)!).GetAwaiter().GetResult();

    private static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    private static object? Seated(Pages.Map map) => Invoke(map, "get_SeatedTable");

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name."))
        .GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        if (typeof(Pages.Map).GetField(name, Hidden) is { } field)
        {
            field.SetValue(map, value);
            return;
        }

        (typeof(Pages.Map).GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}`.")).SetValue(map, value);
    }

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden)?.GetValue(o))
        ?? (o.GetType().GetProperty(name, Hidden)
            ?? throw new InvalidOperationException($"{o.GetType().Name} has no `{name}`.")).GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name."))
        .Invoke(map, args);
}
