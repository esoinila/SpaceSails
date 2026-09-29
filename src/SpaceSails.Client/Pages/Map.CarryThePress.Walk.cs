using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #251 · Split from Map.CarryThePress.cs, moved verbatim: SHE WALKS THE GROUND BEHIND HIM — her walker, her
// word read off the recorder, and the plan that keeps her in the coat's band. No field and no const moved;
// every word she says is still Core's (CarryThePress).
public sealed partial class Map
{
    // ── SHE WALKS THE GROUND BEHIND HIM ─────────────────────────────────────────────────────────────────

    /// <summary>The walker that is her, if she is on the ground. By errand, because her plate is her own.</summary>
    private static Walker? TheStringerAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For == Errand.RidingAlong)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// #1202 · One frame of her, called from the surface tick beside Brem Kolt's. Off her ground she is aboard
    /// and nothing is drawn. On it: her word about the tin is said and filed once; she comes down with the
    /// captain; she keeps the band behind him (<see cref="TheTailBehindYou.HoldsHisBand"/>, the coat's own
    /// arithmetic); and the first time he stops with her behind him, she says her one line about the air. She
    /// needs no air model of her own: the line says it — same tank.
    /// </summary>
    private void AdvanceTheStringer(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        Walker? her = TheStringerAfoot(ex.Walkers);
        if (TheStringerOnThisGround(ex) is not { } q)
        {
            if (her is not null)
            {
                ex.Walkers.Remove(her);
            }

            return;
        }

        // Her word about the tin waits until he is standing on the ground and looking — on the pad (and on the
        // descent's warm-up frame, which is on the pad) she has said nothing yet — AND until the slot is free:
        // whatever was said before her (the suit's VACUUM crossing, the shuttle's line) has had its whole dwell.
        // See SheReadsHerWordOffTheRecorder.
        if (!PassageOf(q).Landed && _pulse.Message is null
            && MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor))
        {
            q = SheReadsHerWordOffTheRecorder(ex, q);
        }

        CarryThePress.Passage p = PassageOf(q);

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (her is null)
        {
            if (ex.Walkers.Count < WalkerBand)
            {
                _ = PlanTheStringer(ex, new DeckReachability.Point(_avatarX, _avatarY), walls);
            }

            return;
        }

        if (her.Walk.State != NpcWalk.Doing.Arrived)
        {
            her.Walk.Step(Math.Min(dtRealSeconds, 0.1), walls, _avatarX, _avatarY);
            if (her.Walk.Afoot || her.Walk.State == NpcWalk.Doing.Arrived)
            {
                return;
            }

            // The ground refused her somewhere on the way. She starts again from her own feet.
            ex.Walkers.Remove(her);
            _ = PlanTheStringer(ex, new DeckReachability.Point(her.Walk.X, her.Walk.Y), walls);
            return;
        }

        double dx = her.Walk.X - _avatarX, dy = her.Walk.Y - _avatarY;
        double range = Math.Sqrt((dx * dx) + (dy * dy));

        // …and the first time he stops out on the ground with her behind him, she says it. Once.
        // #1202 · Her line about the tank pays the same courtesy as her word about the tin: it waits for a free
        // slot, and behind the tracker's first stir if that is waiting too, so nothing of hers cuts anything short.
        if (p.Landed && !p.Walked && _pulse.Message is null && !TheFirstStirIsWaiting(ex)
            && TheTailBehindYou.HoldsHisBand(range)
            && MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor))
        {
            SayItWhereTheyAreLooking(CarryThePress.WalkLine);
            RewritePassage(q, p with { Walked = true });
        }

        // Out of the band either way — he has walked off, or walked up to where she stands — and she takes a
        // new place in it from her own feet, the middle of the band first, so a few paces either way keep her.
        if (!TheTailBehindYou.HoldsHisBand(range))
        {
            ex.Walkers.Remove(her);
            _ = PlanTheStringer(ex, new DeckReachability.Point(her.Walk.X, her.Walk.Y), walls);
            return;
        }

        her.Walk.LookTowards(_avatarX, _avatarY);
    }

    /// <summary>
    /// #1202 · HER WORD ABOUT THE TIN, TOLD WHEN HE IS STANDING ON THE GROUND AND LOOKING, AND AFTER WHATEVER
    /// WAS SAID BEFORE HER HAS HAD ITS DWELL (Fable's rulings, 2026-09-29). Said and filed in one call, once
    /// (the contract's <c>Landed</c> bit), from <see cref="AdvanceTheStringer"/> on the first live frame that
    /// finds the captain on the regolith (off the pad) with the pulse slot EMPTY.
    ///
    /// <para><b>Why the empty slot.</b> #1321 said it when the first-ground card closed, and the very next live
    /// frame the suit's crossing — <i>"🫁 VACUUM. The suit seals and the tank cuts in…"</i>
    /// (<see cref="AnnounceAirSupply"/>) — wrote over it at the same rank, so it was still never on screen. The
    /// suit's line is a fact and is said first; hers waits for the slot to come free (the line before it has
    /// expired, <see cref="PulseSlot.Expire"/>), and is then said for her own full dwell. She never outranks
    /// the suit and her line is never dropped: a frame that finds the slot busy simply asks again next frame.
    /// No field: the waiting is the contract's own <c>Landed</c> bit being still false.</para>
    ///
    /// <para>Her line about the tank (<c>Walked</c>) waits for this one, so the two are said in order.</para>
    /// </summary>
    private Quest SheReadsHerWordOffTheRecorder(SurfaceExcursion ex, Quest q)
    {
        CarryThePress.Passage p = PassageOf(q);
        if (p.Landed)
        {
            return q;
        }

        string line = CarryThePress.Landing(TheTinsWords(q, ex));
        FileNote(line, CarryThePress.Glyph);
        ShowPulseMessage(line);
        return RewritePassage(q, p with { Landed = true });
    }

    /// <summary>
    /// Plan her next leg: from her own feet to a standing place behind the captain, inside the band — the
    /// coat's ranges and sides, in the coat's order (<see cref="TheTailBehindYou.TheRangesHeTries"/>,
    /// <see cref="TheTailBehindYou.TheSidesHeSounds"/>): the middle of the band first, so a captain who takes a
    /// few paces either way does not send her walking again.
    /// </summary>
    private bool PlanTheStringer(
        SurfaceExcursion ex, DeckReachability.Point from, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double heading = _avatarHeading;
        foreach (double range in TheTailBehindYou.TheRangesHeTries)
        {
            foreach (double side in TheTailBehindYou.TheSidesHeSounds)
            {
                double x = _avatarX + (Math.Cos(heading + side) * range);
                double y = _avatarY + (Math.Sin(heading + side) * range);
                SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
                if (spot.Failed)
                {
                    continue;
                }

                if (OnFoot(CarryThePress.Plate, new NpcWalk.Bound("", spot.X, spot.Y), from, walls,
                        NpcWalk.NoPersonalSpace) is { } walk)
                {
                    ex.Walkers.Add(new Walker { Walk = walk, Table = -1, For = Errand.RidingAlong });
                    StateHasChanged();
                    return true;
                }
            }
        }

        return false;
    }
}
