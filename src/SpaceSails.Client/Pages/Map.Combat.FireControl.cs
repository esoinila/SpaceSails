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

// Subject: the gun deck — the captain's word, the magazine, the Norden solution and the fire plan on the map. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).

/// <summary>
/// #251 · THIS FILE IS THE CAPTAIN'S WORD AND THE MAGAZINE — weapons tight, fire at will, the single
/// authorised shot, what a round costs and how many are left. Every field and every <c>const</c> of
/// the family is declared here, in the order the one file declared them.
///
/// <para>The rest is four siblings: <c>.Window</c> (can the shot be taken at all — the straight-shot
/// window, what is in the way, the mark's own state and the rounds in flight), <c>.Solve</c> (the
/// aim offset, the round, the predicted paths and the Norden solution), <c>.Fire</c> (auto-aim, the
/// deliberate press, the scan, the cancel, and the lock that fires itself), and <c>.Draw</c> (the
/// orrery view: the whole geometry on the live map behind the war room).</para>
///
/// <para><b>The three <c>static readonly RgbaColor</c>s the fire plan is painted with stayed
/// here</b>, with the rest of the family's state — a static field initializer of a partial class runs
/// in the order the compiler reads the FILES, and a colour that travels with its only reader is a
/// colour that can be initialised late with nothing about the build saying so. So is
/// <c>_fireBlockedBy</c>, whose docblock is the design record of the blocker walk in
/// <c>.Window</c>: the comment is attached to the FIELD, so it stayed with the field.</para>
/// </summary>
public partial class Map
{
    private string? FireChipLine() => FireLocked
        ? $"🎖 FIRING in {Math.Max(0, (int)(_fireAtSimTime - SimTime))}s"
        : null;

    // ---- M28 (Sunday PR-C): the Norden moment — gun-deck fire control ----
    // #961: the mass driver's top charge, m/s — CORE's number, not a copy of it. The gun deck, the
    // dossier's "driver reach" and EncounterRule's weapon envelope all descend from that one constant now;
    // they used to be three numbers that merely happened to be typed near each other, and two of them
    // disagreed by 3.5× on the card the owner was reading.
    private const double MaxMuzzleSpeed = OrdnanceRule.MassDriverMuzzleSpeedMps;
    private const int SlugPulseCost = 2;             // reaction mass per shot
    private const int MissilePulseCost = 5;
    private const double FireLockLeadSeconds = 60;   // solution locks T-60 s — the Norden beat

    private double _fireAimOffsetSeconds = 3600;     // where on the prey's track we aim, from now
    private OrdnanceKind _fireKind = OrdnanceKind.Slug;
    private FireControl.Solution? _fireSolution;
    private IReadOnlyList<TrajectorySample> _fireSolutionPath = []; // the planned round's transfer, for the map
    private IReadOnlyList<TrajectorySample> _fireTargetPath = [];   // the prey's predicted track to t_hit
    private double _fireDispersionMeters;
    private Vector2d _fireAimPoint;
    // The captain's word (owner: "captain's panel must authorize pulling the trigger",
    // plus a standing "fire at will"). Warning shots need no authorization — they ARE the
    // way a captain talks without committing.
    private bool _fireAtWill;
    private bool _shotAuthorized;
    private bool WeaponsAuthorized => _fireAtWill || _shotAuthorized;

    private void AuthorizeShot()
    {
        _shotAuthorized = !_shotAuthorized;
        ShowPulseMessage(_shotAuthorized ? "CAPTAIN: next shot authorized" : "CAPTAIN: authorization withdrawn");
        if (_shotAuthorized)
        {
            AdvanceTutorial(StepAuthorizeShot); // second hunt, step 3: the captain's word
        }
        StateHasChanged();
    }

    /// <summary>
    /// WEAPONS TIGHT — the mirror of fire at will, and the order #538's hiding scene cannot work without. A
    /// deployed bot shoots what it sees and the tube gun never runs dry, so concealment is worthless while the
    /// captain's own automation is still making decisions.
    ///
    /// <para>It never disarms the captain: their trigger still works. A captain deciding to shoot and a machine
    /// deciding for them are different acts, which is the distinction the whole authority idiom rests on.</para>
    /// </summary>
    private bool _weaponsTight;

    private void ToggleWeaponsTight()
    {
        _weaponsTight = !_weaponsTight;

        SayItWhereTheyAreLooking(_weaponsTight ? SentryBot.WeaponsTightLine : SentryBot.WeaponsFreeLine);
        LogAutopilotEvent(_weaponsTight
            ? "🤖 WEAPONS TIGHT ordered — bots and the tube gun stand down."
            : "🤖 Weapons free — the bots have their arcs back.");

        // Said once, on the way in: the order that hides you also stops defending you.
        if (_weaponsTight)
        {
            LogAutopilotEvent(SentryBot.TightIsAlsoUndefendedLine);
        }

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private void ToggleFireAtWill()
    {
        _fireAtWill = !_fireAtWill;
        ShowPulseMessage(_fireAtWill ? "CAPTAIN: weapons free — fire at will" : "CAPTAIN: weapons hold");
        if (_fireAtWill)
        {
            AdvanceTutorial(StepAuthorizeShot); // a standing order satisfies the captain's-word step too
        }
        StateHasChanged();
    }

    // The magazine (owner: rounds are BOUGHT once spent). Warning shots burn a slug too.
    private int _slugAmmo = 12;
    private int _missileAmmo = 4;

    private void BuyAmmo(OrdnanceKind kind)
    {
        (int price, int count) = kind == OrdnanceKind.Missile ? (500, 2) : (300, 6);
        if (!_docked || _credits < price)
        {
            return;
        }

        _credits -= price;
        if (kind == OrdnanceKind.Missile)
        {
            _missileAmmo += count;
        }
        else
        {
            _slugAmmo += count;
        }

        ShowPulseMessage($"Dockside resupply: +{count} {(kind == OrdnanceKind.Missile ? "missiles" : "slugs")} ({price} cr)");
        StateHasChanged();
    }

    private double _fireAtSimTime = double.NaN;      // NaN = nothing locked
    private string? _fireTargetId;
    private string? _fireTip;                        // F6: the solver as flight instructor
    private int _revealedIterations;                 // the CALCULATING… reveal cursor
    private double _lastRevealMs;
    private double _slewBearingRad;                  // cosmetic auto-slew after the shot
    private double _slewUntilSimTime = double.NaN;

    private bool FireLocked => !double.IsNaN(_fireAtSimTime);

    /// <summary>Owner: "(unless there is something blocking the shot in between)" — walk the
    /// solved transfer's segments against every body's disc (Sun included: no shooting through
    /// the star). Segment-vs-point with the body at the segment's mid-time; coarse for long
    /// segments, honest enough to name the blocker.</summary>
    private string? _fireBlockedBy;

    private static readonly RgbaColor FirePlanColor = new(255, 120, 120, 200);
    private static readonly RgbaColor FirePlanTargetColor = new(200, 120, 255, 120);
    private static readonly RgbaColor FirePlanDispersionColor = new(255, 120, 120, 60);
}
