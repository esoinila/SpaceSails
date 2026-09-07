using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// STEPPING HIM, HIS EYES, AND THE RUNNING — one leg of his walk, whether one of THEM is in his
/// sightline (range and a clear line, both quoted rather than guessed), and what he does about it.
///
/// <para>A man in a suit and bad shoes, well out of his way, running from things that only ever come
/// straight at you is the picture; nothing in this family explains it.</para>
///
/// <para>Split out of <c>Map.Hardcase.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── Stepping him ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One frame of him. Two of his three errands end standing up (he arrives and then he is THERE); the
    /// third ends with him off the ground and nothing said about it.
    ///
    /// <para><b>The break is asked FIRST, on every frame, whatever he is doing.</b> A man who finished
    /// crossing the ground before he was allowed to notice what had crested the rim would be the sim waiting
    /// its turn to be frightened.</para>
    /// </summary>
    private void StepTheHardcase(
        SurfaceExcursion ex, Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (who.For != Errand.HardcaseFleeing && OneIsInHisSight(who.Walk.X, who.Walk.Y, walls))
        {
            HeBreaksAndRuns(ex, who, walls);
            return;
        }

        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            HeIsStandingAt(who);
            if (who.Walk.Afoot)
            {
                return;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived || who.For == Errand.HardcaseFleeing)
            {
                // The ground refused him, or the run is over. Either way he comes off it — and for the run
                // there is deliberately nothing else: no line, no card, no note.
                ex.Walkers.Remove(who);
                _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
                StateHasChanged();
                return;
            }

            if (who.For == Errand.HardcasePitching)
            {
                HeReachesYou();
            }
            else
            {
                _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
            }

            who.Walk.LookTowards(_avatarX, _avatarY);
            StateHasChanged();
            return;
        }

        // Standing. He is taken off the list the moment he is due somewhere else and put back on it by the
        // planner — the rep's own idiom, which is what keeps the next leg beginning at his feet instead of
        // out on the apron he first walked in from.
        if (SimTime >= _hardcaseMoveOnAt && HeIsDueSomewhereElse(who, walls))
        {
            HeIsStandingAt(who);
            ex.Walkers.Remove(who);
            StateHasChanged();
            return;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
    }

    /// <summary>Is the man standing there finished standing there? Two answers and they are the two halves of
    /// his day: at his post he is due to cross the moment the captain is out on the ground worth crossing to,
    /// and at the captain's elbow he is due back at his post the moment the card is down — because a man who
    /// has said his piece and been answered has no further business at your shoulder.</summary>
    private bool HeIsDueSomewhereElse(Walker who, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        who.For switch
        {
            Errand.HardcaseWaiting => HeCrossesToTheCaptain(walls),
            // …and BOTH halves are asked, because a card the arbiter is still holding behind somebody else's
            // scrim (#1052) has not gone up yet. A man who walked off while his own pitch was queued would
            // leave the beat to be raised over an empty patch of regolith.
            Errand.HardcasePitching => _hardcasePitched && _hardcaseCard is null,
            _ => false,
        };

    /// <summary>Remember where his feet are, so the next leg begins at them.</summary>
    private void HeIsStandingAt(Walker who) =>
        _hardcaseStandingAt = new DeckReachability.Point(who.Walk.X, who.Walk.Y);

    // ── The eyes, and the running ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>IS ONE OF THEM IN HIS SIGHTLINE?</b> Range and a clear line, and both of them are quoted rather
    /// than chosen: <see cref="HardcaseRep.SeesOneAtDu"/> is Core's own ruling about when an Old One stops
    /// being scenery, and the line is <see cref="SurfaceCollision.HasLineOfSight"/> — the identical call
    /// <c>StepReevers</c> makes in the other direction, over the identical collision field.
    ///
    /// <para>A DORMANT one counts. It is folded down in the dust and it has not moved in forty years, and
    /// that is exactly the thing you run from if you know what it is — which is the whole tell, and it is
    /// why no line may ever explain it.</para>
    /// </summary>
    private bool OneIsInHisSight(double x, double y, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double r2 = HardcaseRep.SeesOneAtDu * HardcaseRep.SeesOneAtDu;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) <= r2
                && SurfaceCollision.HasLineOfSight(x, y, r.X, r.Y, walls))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>HE BREAKS.</b> The papers go, and he goes — flat out, on the lattice, at
    /// <see cref="HardcaseRep.DespairPaceDu"/>, to whichever end of the ground is furthest from the thing he
    /// has just seen.
    ///
    /// <para><b>Nothing is said and nothing is filed.</b> Not here, not at the far end of the run, not when
    /// he comes off the ground. The only record of it in the whole game is the sheet lying in the dust, and
    /// that sheet is about prices.</para>
    ///
    /// <para>If the lattice cannot get him anywhere he still drops the papers and still comes off the ground.
    /// A man who could not find a way out is not a man who stood politely still.</para>
    /// </summary>
    private void HeBreaksAndRuns(
        SurfaceExcursion ex, Walker who, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _hardcaseFled = true;
        if (_hardcaseCard is not null)
        {
            CloseTheHardcasesCard();
        }

        TheScheduleFalls(ex, who.Walk.X, who.Walk.Y);

        DeckReachability.Point from = new(who.Walk.X, who.Walk.Y);
        ex.Walkers.Remove(who);
        _hardcaseStandingAt = from;

        if (TheFarEndOfTheGround(walls) is { } away
            && PlanKolt(ex, walls, away, Errand.HardcaseFleeing,
                        NpcWalk.NoPersonalSpace, HardcaseRep.DespairPaceDu))
        {
            return;
        }

        _hardcaseStandingAt = null;
        StateHasChanged();
    }

    /// <summary>Which way is away. The two ends of the landing band, and the one further from the nearest Old
    /// One wins — cheap, deterministic, and the same answer the captain would give looking at the same
    /// field.</summary>
    private DeckReachability.Point? TheFarEndOfTheGround(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        double y = MoonSurface.SurfaceTopY - HardcasePostDepthDu;
        double left = field.LeftX + SurfaceLayout.EdgeMargin;
        double right = field.RightX - SurfaceLayout.EdgeMargin;

        double nearest = double.PositiveInfinity;
        double nearestX = field.HomeX;
        foreach (Reever r in _reevers)
        {
            double d = Math.Abs(r.X - field.HomeX);
            if (d < nearest)
            {
                nearest = d;
                nearestX = r.X;
            }
        }

        double pick = Math.Abs(left - nearestX) >= Math.Abs(right - nearestX) ? left : right;
        SpawnNudge.Result spot = SpawnNudge.Clear(pick, y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }
}
