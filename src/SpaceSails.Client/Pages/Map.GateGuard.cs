using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #618 / #582 slice 1 · <b>A GUARD, BUT NOT A GOOD ONE</b> — the page's half. Core decides everything that can
/// be decided without a floor under it (<see cref="GateGuard"/>: how the door is kept this window, whether the
/// word works, the paper that passes, what the tide does at the mouth, every word); this family is the man on
/// the floor.
///
/// <h3>Where he stands, and what he keeps</h3>
///
/// <para>Beside the cage's landing on the top pressurised floor, on the corridor side — the floor a captain
/// steps out onto from the surface, and the last floor before the building starts asking for paper. What he
/// keeps is the way DOWN: while he is at the door, neither car on his floor offers a floor below it
/// (<see cref="PastTheManAtTheDoor"/>). He is in front of the door and never instead of it — passing him opens
/// nothing the pad and the card do not already open, and the way UP is never his (the surface row stays, and
/// the stair is the stair).</para>
///
/// <h3>The four ways past</h3>
///
/// <list type="bullet">
/// <item><b>A badge.</b> #605's own verb and label on his card, offered only when the wallet holds a pass the
/// building's own ladder reads as good on this floor.</item>
/// <item><b>A word.</b> TALK YOUR WAY IN, seeded per window. A failure is his remembering your face — a tag on
/// the register — and the move is ABSENT at this ground from then on, never greyed.</item>
/// <item><b>The light.</b> MAKE A NOISE and he comes off the wall and follows you, in the coat's band, until the
/// captain comes out under the lid. At the mouth of the tube the tide decides (<c>Map.GateGuard.Mouth.cs</c>).</item>
/// <item><b>The window.</b> Some windows he is not there; some he is walking his round.</item>
/// </list>
///
/// <para><b>No field on this page.</b> The one seeded state rides the excursion (<see cref="ManAtTheDoor"/>);
/// the face and the book's once ride <c>_roomsTurnedOver</c> as tags; the dev start is read off the address
/// bar. He is a <see cref="Walker"/> in the excursion's own band — Brem Kolt's shape — and he never eats a slot
/// of the room's own feet.</para>
/// </summary>
public partial class Map
{
    /// <summary>Is this walker him? By errand, because his plate is his own.</summary>
    private static bool IsTheManAtTheDoor(Walker w) =>
        w.For is Errand.GateKeeping or Errand.GateOnHisRound or Errand.GateFollowing or Errand.GateAtTheMouth;

    /// <summary>The walker that is him, if he is on his feet in the excursion's band.</summary>
    private static Walker? TheManAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (IsTheManAtTheDoor(w))
            {
                return w;
            }
        }

        return null;
    }

    // ── MEETING THE DOOR ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #618 · The first time this excursion stands on the floor he keeps, the door is seeded: the ground and the
    /// window (the frozen watch the floor was drawn on, so <c>?watch=N</c> reaches him too) go through
    /// <see cref="GateGuard.For"/> once, and the answer rides the excursion for the rest of the trip. Called from
    /// the underground rebuild beside the watch freeze, so the floor that is drawn and the man who keeps it are
    /// decided at the same instant.
    /// </summary>
    private void MeetTheDoorIfThisIsHisFloor(SurfaceExcursion ex)
    {
        if (ex.Gate is not null || ex.Floor >= 0
            || UndergroundComplex.TopPressurisedFloor(ex.Stop.Body.Id) != ex.Floor)
        {
            return;
        }

        string ground = ex.Stop.Body.Id;
        GateGuard.Visit visit = GateGuard.For(ground, ex.CanteenWatch);
        if ((Navigation is { } address ? GateGuard.CheatIn(address.Uri) : null) is { } forced)
        {
            visit = visit with { Posting = forced };
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        ex.Gate = new ManAtTheDoor
        {
            Ground = ground,
            Window = ex.CanteenWatch,
            Visit = visit,
            Floor = ex.Floor,
            Post = HisPostBesideTheCage(field),
            Canteen = TheCanteenEndOfHisRound(ground, ex.Floor, field),
        };
    }

    /// <summary>How far along the corridor from the cage's landing he stands — clear of the doors, near enough
    /// that a captain stepping out of the car is at his elbow.</summary>
    private const double GatePostAlongDu = 3.5;

    /// <summary>…and how far out from the car's face, into the corridor.</summary>
    private const double GatePostOutDu = 1.5;

    /// <summary>Beside the cage's landing on the corridor side: the shaft's own doorstep
    /// (<see cref="HiveInterior.SpawnOn(in SurfaceLayout.Field, UndergroundComplex.ShaftKind)"/>) and a pace along
    /// the spine, so a car that moves takes him with it. The point is Core's geometry and not measured off a
    /// picture; the frame nudges it clear of stone when he is first put on it.</summary>
    private static DeckReachability.Point HisPostBesideTheCage(in SurfaceLayout.Field field)
    {
        (double x, double y) = HiveInterior.SpawnOn(field, UndergroundComplex.ShaftKind.Cage);
        return new DeckReachability.Point(x + GatePostAlongDu, y + GatePostOutDu);
    }

    /// <summary>The canteen end of his round: the counter's own published standing spot on this floor, the
    /// one <c>?counter=1</c> stands a tester at — or null on a floor with no counter, where there is no round to
    /// walk and he keeps the door instead.</summary>
    private static DeckReachability.Point? TheCanteenEndOfHisRound(
        string bodyId, int level, in SurfaceLayout.Field field)
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(bodyId, level, field).Amenities)
        {
            if (CounterService.For(bodyId, a.Use) is not null)
            {
                return new DeckReachability.Point(a.X, a.Y);
            }
        }

        return null;
    }

    // ── IS THE DOOR MANNED ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #618 · <b>DOES THE MAN KEEP THE WAY DOWN, THIS FRAME?</b> On his floor, not passed, not taken, not an
    /// empty chair — and either on your heels (a man who came off the wall at a noise is still the man in front
    /// of the door) or standing at it. A man who could not be put on the floor is not at the door: what is not
    /// drawn does not keep anything.
    /// </summary>
    private bool TheManKeepsTheWayDown(SurfaceExcursion ex) =>
        ex.Gate is { } man && ex.Floor == man.Floor && !man.Passed && !man.Taken
        && man.Visit.Posting != GateGuard.Posting.Absent && man.AtTheMouthFor < 0
        && (man.Following || HeIsAtTheDoor(ex, man));

    /// <summary>Is he standing at the door — within <see cref="GateGuard.MannedWithinDu"/> of it, on his feet?</summary>
    private static bool HeIsAtTheDoor(SurfaceExcursion ex, ManAtTheDoor man) =>
        TheManAfoot(ex.Walkers) is { } him
        && Within(him.Walk.X, him.Walk.Y, man.Post.X, man.Post.Y, GateGuard.MannedWithinDu);

    private static bool Within(double ax, double ay, double bx, double by, double du) =>
        ((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by)) <= du * du;

    /// <summary>
    /// #618 · <b>THE MAN IS IN FRONT OF THE DOOR.</b> While he keeps it, the panel of either car on his floor
    /// offers nothing below the floor it is on: the stops that go down are absent, never refused — there is no
    /// gate to refuse at, there is a man. SURFACE and the floor itself stay, so he is never the reason a captain
    /// cannot leave. Every stop that remains is the building's own, untouched: the pad, the card and the heat
    /// read are exactly what they were (#602, #605, #715).
    /// </summary>
    private IReadOnlyList<UndergroundComplex.LiftStop> PastTheManAtTheDoor(
        SurfaceExcursion ex, IReadOnlyList<UndergroundComplex.LiftStop> stops)
    {
        if (!TheManKeepsTheWayDown(ex))
        {
            return stops;
        }

        var kept = new List<UndergroundComplex.LiftStop>(stops.Count);
        foreach (UndergroundComplex.LiftStop stop in stops)
        {
            if (stop.Level >= ex.Floor)
            {
                kept.Add(stop);
            }
        }

        return kept;
    }
}
