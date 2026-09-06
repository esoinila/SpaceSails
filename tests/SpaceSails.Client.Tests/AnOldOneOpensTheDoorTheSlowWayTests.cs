using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · <b>AN OLD ONE OPENS A DOOR THE SLOW WAY, AND NEVER BREAKS A LOCKED ONE.</b>
///
/// <para>Owner ruling, 2026-09-06 (#563's second question): they still have doors <i>a bit like we still have
/// nostalgic gramophones; for them it is legacy and nostalgia, and it lets them not reveal their true
/// capabilities.</i> Canon in <c>docs/worldbuilding-notes.md</c> §10, arithmetic in Core
/// <see cref="ReeverDoor"/>.</para>
///
/// <para><b>What was wrong.</b> The chase reads stone by law — a door is not collision, because the passage is
/// always walkable for the captain — so an Old One crossed a shut leaf at walking pace, mid-stride, as though
/// the doorway were an empty hole. Every other faculty had already been given the door: the eye (#465), the
/// round (#466), the beam (#1099), the sleeper's lamp (#1154). The legs were the last reader still asking the
/// captain's list.</para>
///
/// <para><b>Why driven and not arithmetic.</b> The law is not in doubt anywhere it is written down; the defect
/// was entirely in WHICH LIST the page hands the legs, and only the page knows that. So every case here drives
/// real <c>StepReevers</c> frames on the shipping wreck deck with a real contact on it, and reads the answer
/// off the plan and off the contact's own fields.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AnOldOneOpensTheDoorTheSlowWayTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const double Dt = 1.0 / 60.0;

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BENCH IS THE CASE IT CLAIMS TO BE, and this is the guard that stops the rest of the file being a
    /// world that cannot tell pass from fail. Four things have to hold of the spot, and every one of them is
    /// a way the cases below could go green for a reason that is not the rule:
    ///
    /// <list type="number">
    /// <item><b>Stone alone lets the look through</b>, so the leaf and nothing else stands between the two —
    /// otherwise "it did not get across" is a bulkhead's doing;</item>
    /// <item><b>the leaf is shut</b> with the captain standing where the bench puts him — otherwise there is
    /// nothing to open and the beat is a beat about nothing;</item>
    /// <item><b>the contact is within arm's length of the leaf</b> and standing on clear ground — otherwise
    /// nothing is hauling and the leaf would sit shut forever, passing the negative cases;</item>
    /// <item><b>the leaf may be worked at all</b> — unlocked, unpartnered, and not a picture in front of
    /// stone — otherwise the positive case is testing the refusal, not the opening.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void ThePremise_OnlyTheLeafStandsBetweenThemAndOnlyTheLeafIsShut()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.Door d = deck.Doors[leaf.Index];

        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        Assert.True(
            SurfaceCollision.HasLineOfSight(leaf.CaptainX, leaf.CaptainY, leaf.TheirX, leaf.TheirY,
                deck.CollisionField),
            "stone stands between the captain and the contact on this bench — the leaf is then not the thing "
            + "under test, and every 'it never got across' below would pass on a bulkhead.");

        Assert.False(
            SurfaceCollision.HasLineOfSight(leaf.CaptainX, leaf.CaptainY, leaf.TheirX, leaf.TheirY,
                SightBlockers(map)),
            "the leaf is not shut with the captain standing here, so there is nothing for anything to open.");

        Assert.False(SurfaceCollision.Blocked(leaf.TheirX, leaf.TheirY, DeckPlan.AvatarRadius,
                deck.CollisionField),
            "the contact's spot is inside stone — it would be extricated on frame one and this bench would "
            + "measure a walk it never took.");

        Assert.True(
            SurfaceCollision.Blocked(leaf.TheirX, leaf.TheirY, ReeverDoor.Reach(DeckPlan.AvatarRadius),
                [new SurfaceCollision.Segment(d.X1, d.Y1, d.X2, d.Y2)]),
            "the contact is out of arm's length of the leaf, so nothing would ever be hauling it and the "
            + "negative cases below would pass with the door untouched.");

        Assert.True(ReeverDoor.MayWork(d.Locked, d.Interlock != 0, deck.DoorwayIsWalledUp(d)),
            "the bench picked a leaf that may never be worked at all — the positive case would then be "
            + "measuring the refusal, which is a different law with its own case below.");
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BEAT: the leaf moves, THEN it comes through — and the leaf stays open.</b> The whole ruling in
    /// one drive. It stands at a shut leaf and does not pass; the leaf slides while it stands there; when the
    /// leaf is over, it comes through; and the leaf is still over long afterwards, with nothing near it and
    /// the captain gone, because <b>they do not close doors behind them</b>.
    ///
    /// <para><b>Proven RED</b> by reverting the legs to <c>_deckPlan.CollisionField</c> — the contact is on
    /// the captain's side of a shut leaf on frame one. See the PR body.</para>
    /// </summary>
    [Fact]
    public void ItStandsAtTheLeafForABeat_ThenComesThrough_AndTheLeafStaysOpen()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        object one = PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        int crossedOn = -1, openedOn = -1;
        double openingHalfWay = 0;
        for (int frame = 1; frame <= 600 && crossedOn < 0; frame++)
        {
            OneFrame(map);
            if (openedOn < 0 && deck.LeafHeldOpen(leaf.Index))
            {
                openedOn = frame;
            }
            if (openedOn < 0)
            {
                openingHalfWay = Math.Max(openingHalfWay, deck.LeafOpening(leaf.Index));
                Assert.False(leaf.OnTheCaptainsSide(Where(one)),
                    $"frame {frame}: it was through the doorway with the leaf only "
                    + $"{deck.LeafOpening(leaf.Index):P0} over. A shut door is a real delay to them "
                    + "(#563) — the legs are reading the captain's wall list again.");
            }
            else if (leaf.OnTheCaptainsSide(Where(one)))
            {
                crossedOn = frame;
            }
        }

        Assert.True(openedOn > 0, "the leaf never came over: it stood at an unlocked door for ten seconds "
            + "and nothing happened, which is a wall, not a door.");
        Assert.True(openingHalfWay > 0,
            "the leaf jumped from shut to open with no beat in between — nothing was ever DRAWN moving, and "
            + "the leaf sliding on its own is the whole telling (canon point 4).");
        Assert.True(crossedOn > openedOn,
            "it never came through a door it had just opened.");

        // …and it stays open. The captain leaves, the thing that opened it leaves, and the doorway keeps
        // standing open — which is the sentence the player is meant to read off an empty room.
        ((IList)Get(map, "_reevers")!).Clear();
        StandTheCaptainAt(map, leaf.CaptainX + 400, leaf.CaptainY + 400);
        for (int i = 0; i < 300; i++)
        {
            OneFrame(map);
        }
        Assert.True(deck.LeafHeldOpen(leaf.Index),
            "the leaf shut itself behind them. They do not close doors — an open doorway on an empty room is "
            + "the only thing this feature ever says.");
        Assert.False((bool)Invoke(map, "IsDoorShut", deck.Doors[leaf.Index], leaf.Index)!,
            "the plan draws the leaf open and the sight list still calls it shut — two answers about one "
            + "leaf is the exact arrangement IsDoorShut's own warning forbids.");
    }

    /// <summary>
    /// <b>AND #1154 HOLDS ALL THE WAY THROUGH THE BEAT.</b> They do not see through a closed door, and a door
    /// that is half open is a closed door. While the leaf is sliding, the captain standing in its line is not
    /// seen and the contact is not drawn; the frame the leaf is over, both change.
    ///
    /// <para>This is what makes the beat worth having: it is a delay <i>and</i> a refuge, not a countdown the
    /// captain is already visible through.</para>
    /// </summary>
    [Fact]
    public void WhileTheLeafIsMoving_NothingSeesThroughIt()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        object one = PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        int looked = 0;
        for (int frame = 1; frame <= 600 && !deck.LeafHeldOpen(leaf.Index); frame++)
        {
            OneFrame(map);
            if (deck.LeafHeldOpen(leaf.Index))
            {
                break;
            }
            (double rx, double ry) = Where(one);
            Assert.False(
                SurfaceCollision.HasLineOfSight(rx, ry, leaf.CaptainX, leaf.CaptainY, SightBlockers(map)),
                $"frame {frame}: the eye got through a leaf that is {deck.LeafOpening(leaf.Index):P0} over. "
                + "#442's ruling is that they do not see through a closed door, and a door being hauled is "
                + "still closed.");
            Assert.False((bool)Get(one, "VisibleOnMap")!,
                $"frame {frame}: the contact is drawn on the deck plan through a leaf that has not finished "
                + "opening — the map is the captain's eyes.");
            looked++;
        }

        Assert.True(looked > 5,
            $"only {looked} frame(s) of beat were measured — a beat this short cannot tell a build that "
            + "holds the ruling from one that opens the door instantly.");
        Assert.True(deck.LeafHeldOpen(leaf.Index), "the leaf never opened, so the second half proves nothing.");

        OneFrame(map);
        (double ox, double oy) = Where(one);
        Assert.True(
            SurfaceCollision.HasLineOfSight(ox, oy, leaf.CaptainX, leaf.CaptainY, SightBlockers(map)),
            "the leaf is standing open and the eye is still stopped by it — the sight list has not learned "
            + "what the plan already knows, which is the two-answers bug again.");
    }

    /// <summary>
    /// <b>THE BEAT IS THE LEAF'S OWN WIDTH AT THEIR OWN PACE — nobody typed it.</b> Measured off the drive
    /// and checked against <c>leaf width ÷ ReeverSpeed</c>, both read from the game's own numbers.
    ///
    /// <para>A guard on a typed feel-number would be worthless: this one goes red if somebody replaces the
    /// derivation with a constant, because a constant cannot also be the width of this particular leaf
    /// divided by this particular shamble.</para>
    /// </summary>
    [Fact]
    public void TheBeatIsTheLeafsOwnWidthOverTheirOwnPace()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        DeckPlan.Door d = deck.Doors[leaf.Index];
        double width = Math.Sqrt(((d.X2 - d.X1) * (d.X2 - d.X1)) + ((d.Y2 - d.Y1) * (d.Y2 - d.Y1)));
        double beat = ReeverDoor.HaulSeconds(width, ReeverSpeed());
        Assert.True(beat > Dt * 4,
            $"the derived beat is {beat:F3} s — shorter than a handful of frames, so no build could be told "
            + "from one with no beat at all.");

        int frames = 0;
        while (!deck.LeafHeldOpen(leaf.Index) && frames < 600)
        {
            OneFrame(map);
            frames++;
        }
        Assert.True(deck.LeafHeldOpen(leaf.Index), "the leaf never opened.");

        int expected = (int)Math.Ceiling(beat / Dt);
        Assert.True(Math.Abs(frames - expected) <= 1,
            $"the leaf took {frames} frame(s) to come over and the game's own numbers say {expected} "
            + $"({width:F2} du of leaf at {ReeverSpeed():F2} du/s = {beat:F3} s). The beat has stopped being "
            + "derived from the door and the walk.");
    }

    /// <summary>
    /// <b>A LOCKED LEAF IS A WALL TO THEM — BY CHOICE.</b> The same bench, the same contact, the same minute
    /// of frames; the one flag flipped. It never opens it, it never breaks it, and it never gets past: it
    /// waits on the far side or works along the wall, which is exactly what the ruling says a walker does.
    ///
    /// <para>The A/B is the point. This is the identical geometry that the case above walks through in under
    /// a second, so "it did not get across" cannot be blamed on the bench.</para>
    /// </summary>
    [Fact]
    public void ALockedLeafIsNeverOpened_AndNeverPassed()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        deck.Doors[leaf.Index] = deck.Doors[leaf.Index] with { Locked = true };

        object one = PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        for (int frame = 1; frame <= 3600; frame++)
        {
            OneFrame(map);
            Assert.False(deck.LeafHeldOpen(leaf.Index),
                $"frame {frame}: a locked leaf came over. They never break a locked one — it is a wall to "
                + "them BY CHOICE, and what they could do to it instead is never shown (#563).");
            Assert.Equal(0.0, deck.LeafOpening(leaf.Index));
            Assert.False(leaf.OnTheCaptainsSide(Where(one)),
                $"frame {frame}: it is through a locked door.");
        }
    }

    /// <summary>
    /// <b>THE LAW READS THE LOCK AND NOTHING ELSE ABOUT THE LEAF.</b> A door in this game carries two
    /// decorative flags — <c>Imported</c> (#592's violet, "somebody shipped this here") and <c>Machined</c>
    /// (#606's heavy pressure leaf) — and the found halls' own leafs are told apart by the material they are
    /// DRAWN in and by nothing else (#716/#1082). Canon point 3 is that a leaf down there opens for the same
    /// reason any other does: legacy. So the beat must be blind to all of it.
    ///
    /// <para>Same bench, same contact, both flags on: byte-for-byte the same frame count and the same
    /// crossing. Goes red the moment anybody special-cases an idiom.</para>
    /// </summary>
    [Fact]
    public void AnIdiomIsNotALock_TheSameLeafOpensTheSameWayInAnyMaterial()
    {
        int plain = FramesToOpen(dress: d => d);
        int dressed = FramesToOpen(dress: d => d with { Imported = true, Machined = true });

        Assert.Equal(plain, dressed);
    }

    /// <summary>
    /// <b>THE SAME DRIVE TWICE IS THE SAME DRIVE.</b> Nothing in the beat is timed off a wall clock, seeded
    /// off a fresh random, or read from anything that moves between runs — so two identical benches produce
    /// the identical frame the leaf comes over on and the identical position the contact ends at.
    /// </summary>
    [Fact]
    public void TheBeatIsDeterministic()
    {
        (int Frames, double X, double Y) first = OneWholeDrive();
        (int Frames, double X, double Y) second = OneWholeDrive();

        Assert.Equal(first.Frames, second.Frames);
        Assert.Equal(first.X, second.X, 12);
        Assert.Equal(first.Y, second.Y, 12);
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One unlocked leaf on the shipping wreck deck, with a pose either side: where the captain
    /// stands to have it shut (well outside its own opening radius), and where the Old One stands with its
    /// hands on it.</summary>
    private readonly record struct Leaf(
        int Index, double CaptainX, double CaptainY, double TheirX, double TheirY,
        double NormalX, double NormalY, double MidX, double MidY, int CaptainSide)
    {
        /// <summary>Signed distance along the leaf's own normal — which side of the doorway a body is on.</summary>
        public double Side(double x, double y) =>
            (((x - MidX) * NormalX) + ((y - MidY) * NormalY)) * CaptainSide;

        public bool OnTheCaptainsSide((double X, double Y) at) => Side(at.X, at.Y) > 0;
    }

    /// <summary>
    /// Find the leaf by ASKING THE DECK, never by typing four numbers off a dump. The wreck's compartments
    /// move with its evidence, salvage and venting, so hand-typed coordinates would end up standing in open
    /// space and every case in this file would go quietly green.
    ///
    /// <para>Swept perpendicular to every workable leaf: the captain far enough back that the leaf is shut in
    /// his face, the Old One a hand's breadth off the other face of it, and the triple accepted only when
    /// stone alone lets the look through, the shut leaf stops it, and both bodies stand on clear ground on
    /// their own sides of the doorway.</para>
    /// </summary>
    private static Leaf TheHatchWithSomethingOnTheFarSideOfIt(Pages.Map map)
    {
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        double keptX = (double)Get(map, "_avatarX")!, keptY = (double)Get(map, "_avatarY")!;
        double radius = DeckPlan.AvatarRadius;
        double reach = ReeverDoor.Reach(radius);

        try
        {
            for (int i = 0; i < deck.Doors.Length; i++)
            {
                DeckPlan.Door d = deck.Doors[i];
                if (!ReeverDoor.MayWork(d.Locked, d.Interlock != 0, deck.DoorwayIsWalledUp(d)))
                {
                    continue;
                }

                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 1e-9)
                {
                    continue;
                }
                double px = -dy / len, py = dx / len;

                foreach (int side in new[] { 1, -1 })
                {
                    // Far enough that the leaf is shut in his face, near enough that the thing on the other
                    // side has a short walk once it is open (and that no case here runs for a minute).
                    for (double back = DeckPlan.DoorOpenRadius + 1.0; back <= DeckPlan.DoorOpenRadius + 4.0;
                         back += 0.5)
                    {
                        double cx = mx + (px * back * side), cy = my + (py * back * side);
                        if (SurfaceCollision.Blocked(cx, cy, radius, deck.CollisionField))
                        {
                            continue;
                        }

                        for (double off = radius + 0.15; off <= reach - 0.05; off += 0.1)
                        {
                            double rx = mx - (px * off * side), ry = my - (py * off * side);
                            if (SurfaceCollision.Blocked(rx, ry, radius, deck.CollisionField))
                            {
                                continue;
                            }
                            if (WreckLayout.PastTheLock(rx, radius) || WreckLayout.PastTheLock(cx, radius))
                            {
                                continue;   // the crew-only clamp would hold it for a reason of its own
                            }
                            if (!SurfaceCollision.HasLineOfSight(cx, cy, rx, ry, deck.CollisionField))
                            {
                                continue;   // a bulkhead is in the way: proves nothing about a leaf
                            }

                            StandTheCaptainAt(map, cx, cy);
                            if (SurfaceCollision.HasLineOfSight(cx, cy, rx, ry, SightBlockers(map)))
                            {
                                continue;   // this leaf is not shut from here, or the line misses it
                            }

                            return new Leaf(i, cx, cy, rx, ry, px, py, mx, my, side);
                        }
                    }
                }
            }
        }
        finally
        {
            StandTheCaptainAt(map, keptX, keptY);
        }

        throw new InvalidOperationException(
            $"no workable leaf on this hull ({deck.Doors.Length} door(s), "
            + $"{deck.CollisionSegments.Length} segment(s)) has a pose either side of it with the leaf and "
            + "nothing else between. Either the wreck deck has stopped having compartments, or a shut door "
            + "has stopped stopping the eye.");
    }

    /// <summary>The whole positive drive, reduced to what the two composite cases compare: the frame the leaf
    /// came over on and where the contact ended up.</summary>
    private static (int Frames, double X, double Y) OneWholeDrive()
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        object one = PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        int frames = 0;
        while (!deck.LeafHeldOpen(leaf.Index) && frames < 600)
        {
            OneFrame(map);
            frames++;
        }
        (double x, double y) = Where(one);
        return (frames, x, y);
    }

    /// <summary>How many frames the leaf takes with the door dressed however the caller likes — the
    /// idiom-blindness case's one moving part.</summary>
    private static int FramesToOpen(Func<DeckPlan.Door, DeckPlan.Door> dress)
    {
        Pages.Map map = OnAHull();
        Leaf leaf = TheHatchWithSomethingOnTheFarSideOfIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        deck.Doors[leaf.Index] = dress(deck.Doors[leaf.Index]);

        PutOneOnTheFarSideOf(map, leaf);
        StandTheCaptainAt(map, leaf.CaptainX, leaf.CaptainY);

        int frames = 0;
        while (!deck.LeafHeldOpen(leaf.Index) && frames < 600)
        {
            OneFrame(map);
            frames++;
        }
        Assert.True(deck.LeafHeldOpen(leaf.Index), "the leaf never opened on this dressing of the door.");
        return frames;
    }

    /// <summary>A live component aboard the shipping derelict, past the arrival grace, with nothing else in
    /// the sim alive. (The same standing-up the #442 sleeper bench uses, for the same reason.)</summary>
    private static Pages.Map OnAHull()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        var wreck = new Derelict.Wreck(
            "doors-bench", "Bench Hull", Derelict.WreckCause.Infested, 250_000, 40.0);
        string bodyId = Derelict.BodyIdFor(wreck.Id);

        Type exType = typeof(Pages.Map).GetNestedType(
            "SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType(
            "ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(bodyId, wreck.ShipName, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        Set(map, "_wreck", wreck);
        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_weaponsTight", true);
        Set(map, "_lastTimestampMs", (double?)(SurfaceArrival.SpotGraceSeconds * 1000.0 * 3));

        Invoke(map, "RebuildSurfaceDeck");
        Assert.True((bool)Get(map, "OnWreck")!, "the bench is not aboard a wreck.");
        Assert.True(((DeckPlan)Get(map, "_deckPlan")!).Doors.Length > 0,
            "the wreck deck has no doors at all, so this whole file is about nothing.");
        return map;
    }

    /// <summary>One awake Old One with its hands on the far face of the leaf, hunting the spot the captain is
    /// standing on. Awake and having seen him, because the ruling is about a thing that WANTS through a door;
    /// a sleeper and an unaware one are both going nowhere and are both their own laws.</summary>
    private static object PutOneOnTheFarSideOf(Pages.Map map, Leaf leaf)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Public)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        Set(one, "X", leaf.TheirX);
        Set(one, "Y", leaf.TheirY);
        Set(one, "AnchorX", leaf.TheirX);
        Set(one, "AnchorY", leaf.TheirY);
        Set(one, "Facing", 0.0);
        Set(one, "Dormant", false);
        Set(one, "Idle", false);
        Set(one, "VisibleOnMap", false);
        Set(one, "EverSeen", true);
        Set(one, "WakeAtMs", 0.0);
        Set(one, "LastSeenX", leaf.CaptainX);
        Set(one, "LastSeenY", leaf.CaptainY);
        Set(one, "JitterSeed", 0xD1B54A32D192ED03UL);
        ((IList)Get(map, "_reevers")!).Add(one);
        return one;
    }

    private static void StandTheCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    /// <summary>One whole surface frame at the shipping rate — the real <c>StepReevers</c>, never a
    /// re-implementation of what it is thought to do.</summary>
    private static void OneFrame(Pages.Map map)
    {
        Set(map, "_lastTimestampMs",
            (double?)((Get(map, "_lastTimestampMs") as double? ?? 0) + (Dt * 1000.0)));
        Invoke(map, "StepReevers", Dt);
    }

    private static (double X, double Y) Where(object one) =>
        ((double)Get(one, "X")!, (double)Get(one, "Y")!);

    private static IReadOnlyList<SurfaceCollision.Segment> SightBlockers(Pages.Map map) =>
        (IReadOnlyList<SurfaceCollision.Segment>)Invoke(map, "SightBlockers")!;

    /// <summary>The shamble's own pace, read off the page's constant so a re-tuning moves this bench with the
    /// game instead of quietly making it prove nothing.</summary>
    private static double ReeverSpeed() =>
        (double)typeof(Pages.Map).GetField("ReeverSpeed", Hidden)!.GetValue(null)!;

    private static object? Get(object o, string name)
    {
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);
}
