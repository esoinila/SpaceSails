using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Combat.FireControl (#251 split; the header note lives in
// Map.Combat.FireControl.cs) — CAN THE SHOT BE TAKEN AT ALL.

/// <summary>
/// #251 · THE HONEST ANSWERS THE FIRE PANEL GIVES BEFORE ANYTHING IS SOLVED: how far the mark is
/// right now, the time-of-flight window a straight shot actually has, what is in the way (owner:
/// <i>"unless there is something blocking the shot in between"</i>), whether a warning shot is on
/// the table, the mark's own state and manoeuvre budget, and the rounds already in flight.
///
/// <para>The blocker walk's design record is the docblock on <c>_fireBlockedBy</c>, which stayed in
/// the opening file because it is attached to the FIELD and no field moved in this cut.</para>
///
/// <para>Split out of <c>Map.Combat.FireControl.cs</c> under #251 with no member renamed, re-scoped
/// or re-ordered.</para>
/// </summary>
public partial class Map
{
    /// <summary>Current straight-line distance to the interest target — the fire panel's honest
    /// "can this round even get there" hint.</summary>
    private double? InterestDistanceNow() =>
        InterestTargetState() is { } state ? (state.Position - _ship.Position).Length : null;

    /// <summary>
    /// The kinematic firing window, closed-form: flight times t where a straight muzzle-speed
    /// shot can cancel the relative drift, |Δr/t + Δv| ≤ v_muzzle. This is the number the
    /// panel must SIGNAL (owner): an 8 km/s gun against 30 km/s orbits means most aims are
    /// infeasible, and the live test showed the naive distance/muzzle hint off by 25×.
    /// </summary>
    private (double MinToF, double MaxToF)? StraightShotWindow()
    {
        if (InterestTargetState() is not { } target)
        {
            return null;
        }

        Vector2d dr = target.Position - _ship.Position;
        Vector2d dv = target.Velocity - _ship.Velocity;
        double a = dr.LengthSquared;
        double b = 2 * dr.Dot(dv);
        double c = dv.LengthSquared - MaxMuzzleSpeed * MaxMuzzleSpeed;
        double disc = b * b - 4 * a * c;
        if (disc < 0 || a <= 0)
        {
            return null; // the drift outruns the muzzle in every direction — no straight shot
        }

        double sq = Math.Sqrt(disc);
        double uHigh = (-b + sq) / (2 * a); // u = 1/t: higher u = shorter flight
        double uLow = (-b - sq) / (2 * a);
        if (uHigh <= 0)
        {
            return null;
        }

        double minToF = 1 / uHigh;
        double maxToF = c < 0 || uLow <= 0 ? double.PositiveInfinity : 1 / uLow;
        return (minToF, maxToF);
    }

    private string? StraightWindowText() => StraightShotWindow() is { } w
        ? $"+{FormatFlightTime(w.MinToF)}{(double.IsPositiveInfinity(w.MaxToF) ? " or later" : $" … +{FormatFlightTime(w.MaxToF)}")}"
        : null;

    private string? FirePlanBlockedBy()
    {
        if (_ephemeris is null || _fireSolutionPath.Count < 2)
        {
            return null;
        }

        for (int i = 1; i < _fireSolutionPath.Count; i++)
        {
            Vector2d a = _fireSolutionPath[i - 1].Position;
            Vector2d b = _fireSolutionPath[i].Position;
            double tMid = (_fireSolutionPath[i - 1].SimTime + _fireSolutionPath[i].SimTime) / 2;
            Vector2d ab = b - a;
            double abLenSq = ab.LengthSquared;
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.BodyRadius <= 0)
                {
                    continue;
                }

                Vector2d center = _ephemeris.Position(body.Id, tMid);
                double t = abLenSq > 0 ? Math.Clamp((center - a).Dot(ab) / abLenSq, 0, 1) : 0;
                if ((a + ab * t - center).LengthSquared <= body.BodyRadius * body.BodyRadius)
                {
                    return body.Name;
                }
            }
        }

        return null;
    }

    /// <summary>Live rounds for the war-room tracker (owner: "shots / missiles away is also
    /// good to track").</summary>
    private IReadOnlyList<Stations.WarRoom.LiveRound> LiveRounds()
    {
        if (_ordnance.Count == 0)
        {
            return [];
        }

        var list = new List<Stations.WarRoom.LiveRound>();
        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            double remaining = OrdnanceRule.LifetimeSeconds(round.Round.Kind)
                - (round.State.SimTime - round.Round.LaunchedAtSimTime);
            list.Add(new Stations.WarRoom.LiveRound(
                round.Round.Kind == OrdnanceKind.Missile ? "missile" : "slug",
                round.Round.TargetId is { } targetId ? NpcName(targetId) : "warning shot",
                Math.Max(0, remaining)));
        }

        return list;
    }

    private bool CanWarnInterest()
    {
        if (_interestTargetId is null)
        {
            return false;
        }

        if (FindNpc(_interestTargetId) is { Active: true, Arrived: false, Disabled: false } npc)
        {
            return !npc.Ship.IsPod && EncounterRule.InWeaponRange(_ship, npc.State);
        }

        // A hunter is a legitimate warning-shot target too — fire near it and its nerve erodes.
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId && !hunter.CaughtPlayer && !hunter.BrokenOff)
            {
                return EncounterRule.InWeaponRange(_ship, hunter.State);
            }
        }

        return false;
    }

    private double? PlannedImpactEta() =>
        _fireSolutionPath.Count > 1 && _fireSolutionPath[^1].SimTime > SimTime
            ? _fireSolutionPath[^1].SimTime - SimTime
            : null;

    /// <summary>The interest target's REAL maneuvering ability, for the dispersion cone — a
    /// depot on rails or a mass-driver pod cannot burn at all, so a 70-day shot at one carries
    /// meters-per-second sigma, not the ±dozens-of-AU the default crewed budget implied (the
    /// long-shot acceptance run caught "±38 AU" on a rails depot).</summary>
    private double InterestManeuverBudget() =>
        _interestTargetId is not null && FindNpc(_interestTargetId) is { } npc
            ? npc.Ship.ManeuverBudget
            : NpcShip.DefaultManeuverBudget;

    private (Vector2d Position, Vector2d Velocity)? InterestTargetState()
    {
        if (_interestTargetId is null)
        {
            return null;
        }

        if (FindNpc(_interestTargetId) is { Active: true, Arrived: false } npc)
        {
            return (npc.State.Position, npc.State.Velocity);
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId)
            {
                return (hunter.State.Position, hunter.State.Velocity);
            }
        }

        return null;
    }
}
