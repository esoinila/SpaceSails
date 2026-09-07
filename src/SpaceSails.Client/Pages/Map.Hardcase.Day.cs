using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// ONE FRAME OF HIS VERY BAD DAY — called once a frame from the surface tick, immediately after the pack
/// has been stepped, so what he does is answered against the ground as it stands this instant.
///
/// <para>Whether he is afoot, taking him off the ground, sending him in, where he comes from, the
/// crossing to the captain, and the post he takes beside him.</para>
///
/// <para>Split out of <c>Map.Hardcase.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── One frame of his very bad day ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once a frame from the surface tick, immediately after the pack has been stepped — so what he
    /// decides about is a field whose Old Ones have already moved this frame, exactly as the rep decides
    /// about a floor whose bodies have.
    /// </summary>
    private void AdvanceTheHardcase(double dtRealSeconds)
    {
        string? ground = TheGroundUnderfootForKolt();
        EnsureKoltsGround(ground);

        if (ground is null || !_hardcaseWorkingHere || !HeCouldBeAfootNow())
        {
            // Off the regolith — a lift ride, a hull, a berth. He does not follow you down a shaft, and a
            // body left in the excursion's band would be drawn standing in a corridor of B1. What he has
            // already DONE on this ground is remembered, because riding a lift is not leaving the crater.
            TakeKoltOffTheGround();
            return;
        }

        if (_surface is not { } ex)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        Walker? afoot = TheHardcaseAfoot(ex.Walkers);

        if (afoot is null)
        {
            if (_hardcaseCard is not null)
            {
                // His card cannot outlive his body.
                CloseTheHardcasesCard();
            }

            _ = SendTheHardcaseIn(ex, walls);
            return;
        }

        StepTheHardcase(ex, afoot, Math.Min(dtRealSeconds, 0.1), walls);
    }

    /// <summary>The walker that is him, if he is on the ground. By errand, because his plate is his own.</summary>
    private static Walker? TheHardcaseAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For is Errand.HardcaseWaiting or Errand.HardcasePitching or Errand.HardcaseFleeing)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>Take him off whatever ground he was on and forget where his feet were. Called the instant
    /// the captain is somewhere he cannot be.</summary>
    private void TakeKoltOffTheGround()
    {
        if (_surface is { } ex && TheHardcaseAfoot(ex.Walkers) is { } who)
        {
            ex.Walkers.Remove(who);
        }

        _hardcaseStandingAt = null;
    }

    /// <summary>
    /// PUT HIM ON THE GROUND, or move him along it. Written as a fall-through rather than a state machine
    /// because each clause is a reason the one under it does not apply: he has bolted, so nothing; he is due
    /// nowhere yet, so nothing; the captain is out here and worth crossing to, so he crosses; otherwise he
    /// stands at his post.
    /// </summary>
    private bool SendTheHardcaseIn(SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_hardcaseFled || ex.Walkers.Count >= WalkerBand || SimTime < _hardcaseMoveOnAt)
        {
            return false;
        }

        if (HeCrossesToTheCaptain(walls)
            && ThePostBesideTheCaptain(walls) is { } beside)
        {
            return PlanKolt(ex, walls, beside, Errand.HardcasePitching, NpcWalk.NoPersonalSpace);
        }

        if (HisPostOn(ex, walls) is not { } post || KoltIsAlreadyStandingAt(post))
        {
            return false;
        }

        // #1061 · HIS FIRST LEG BEGINS OFF TO THE SIDE, not on the spot it ends at. A walk of no length is
        // a teleport with a plate on it and the lattice has nothing to plan for it — so the one walk of a
        // visit that has no feet to start from starts a little further out along the apron, and the captain
        // who looks up sees a man WALKING UP rather than a man who was suddenly standing there.
        _hardcaseStandingAt ??= WhereHeComesFromOn(post, walls);

        return PlanKolt(ex, walls, post, Errand.HardcaseWaiting, NpcWalk.PersonalSpaceInRadii);
    }

    /// <summary>Where he was before he was anywhere: the same apron, further out along it, away from the
    /// tube's own column so his approach reads as coming in off the ground rather than out of the ship.
    /// Falls back to the post itself when the stone allows nowhere else — a man standing at his post is a
    /// worse beat than a man walking to it, and no beat at all is worse than both.</summary>
    private static DeckReachability.Point WhereHeComesFromOn(
        DeckReachability.Point post, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        double outward = post.X >= field.HomeX ? 1 : -1;
        double x = Math.Clamp(
            post.X + (outward * HardcasePostNearDu),
            field.LeftX + SurfaceLayout.EdgeMargin, field.RightX - SurfaceLayout.EdgeMargin);

        SpawnNudge.Result spot = SpawnNudge.Clear(x, post.Y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? post : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Is the captain somebody he can pitch to right now? Out of the tube, on the regolith, not
    /// already pitched at, not already told no — and near enough that crossing to them is a walk rather than
    /// an expedition.</summary>
    private bool HeCrossesToTheCaptain(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        // #621 · THE CAPTAIN IS OUT OF THE TUBE — asked of AwayTeamSide through the page's own answer, and
        // never of the MOON'S top rim. That rim is y > −20, and a derelict's whole deck runs from −9 to +9,
        // so a moon constant asked here would read "safely aboard" on every square of every hull. It is the
        // named bug class this repository has shipped four times, and ADerelictIsNotAMoonTests caught this
        // lane trying to ship it a fifth.
        if (_hardcasePitched || _hardcaseRefused || CaptainBeyondReach)
        {
            return false;
        }

        if (_hardcaseStandingAt is not { } here)
        {
            return false;   // he has not set foot on the ground yet; the post comes first
        }

        double dx = here.X - _avatarX, dy = here.Y - _avatarY;
        return (dx * dx) + (dy * dy) <= HardcaseApproachRangeDu * HardcaseApproachRangeDu
               && SurfaceCollision.HasLineOfSight(here.X, here.Y, _avatarX, _avatarY, walls);
    }

    /// <summary>Where he stands to talk to you: at your elbow, on ground a body fits on. The captain's own
    /// interact reach, offset toward wherever he is coming from, and nudged clear by the same
    /// <see cref="SpawnNudge"/> every placement in this game goes through.</summary>
    private DeckReachability.Point? ThePostBesideTheCaptain(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_hardcaseStandingAt is not { } from)
        {
            return null;
        }

        double dx = from.X - _avatarX, dy = from.Y - _avatarY;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-6)
        {
            return null;
        }

        double reach = DeckPlan.AvatarRadius * NpcWalk.PersonalSpaceInRadii;
        SpawnNudge.Result spot = SpawnNudge.Clear(
            _avatarX + (dx / len * reach), _avatarY + (dy / len * reach), DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>
    /// WHERE HE WAITS. A seeded spot on the fused apron, inside the landing band, off the tube's own column
    /// so he is never standing in the way home.
    ///
    /// <para>Deterministic in the ground: the same crater puts him in the same place on every machine and
    /// after a reload. The envelope is <c>MoonSurface.ExpeditionField</c>'s — the one the generator itself
    /// lays a site inside — so nothing here is a second opinion about where a moon ends.</para>
    /// </summary>
    private DeckReachability.Point? HisPostOn(
        SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        string ground = HardcaseRep.GroundKey(ex.Stop.Body.Id, ex.Site.Index);
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();

        int side = DiceRule.Roll(DiceRule.Seed($"hardcase:post:side:{ground}"), 2).Face == 1 ? -1 : 1;
        int paces = DiceRule.Roll(
            DiceRule.Seed($"hardcase:post:out:{ground}"),
            (int)(HardcasePostFarDu - HardcasePostNearDu) + 1).Face - 1;

        double x = Math.Clamp(
            field.HomeX + (side * (HardcasePostNearDu + paces)),
            field.LeftX + SurfaceLayout.EdgeMargin, field.RightX - SurfaceLayout.EdgeMargin);
        double y = MoonSurface.SurfaceTopY - HardcasePostDepthDu;

        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Is he already where the next stop is? A walk of no length is a teleport with a plate on it.</summary>
    private bool KoltIsAlreadyStandingAt(DeckReachability.Point to) =>
        _hardcaseStandingAt is { } here
        && ((here.X - to.X) * (here.X - to.X)) + ((here.Y - to.Y) * (here.Y - to.Y))
           < DeckPlan.AvatarRadius * DeckPlan.AvatarRadius;

    /// <summary>Plan one of his walks. He sets off from his own feet once he is on the ground, and from his
    /// post the first time — which is what "he was already out here when you came down" means as a
    /// coordinate.</summary>
    private bool PlanKolt(
        SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point to, Errand errand, double berth,
        double pace = NpcWalk.PaceDu)
    {
        DeckReachability.Point from = _hardcaseStandingAt ?? to;
        if (OnFoot(HardcaseRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls, berth, pace)
            is not { } walk)
        {
            // The ground does not connect the two. He is not placed at the far end anyway — that is the
            // reachability audit's own verdict, and the honest answer is that he is not there.
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = -1, For = errand });
        _hardcaseStandingAt = from;
        StateHasChanged();
        return true;
    }
}
