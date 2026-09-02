using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #537 slice 3 · THE VOID AS A PLACE. Owner, on what a smuggler's hole is actually for:
/// <i>"we smuggle our selves past inspection at that hot ship 😎"</i>, and, filing the lane:
/// <i>"the search tool could create an E-interaction to break into the place or hide."</i>
///
/// <para>Slices 1 and 2 built the deduction and the knock; the plate they found was a container. This is the
/// half that makes it worth finding: <b>a captain can be in there</b>, with the plate pulled to, while
/// somebody else's team works the compartment on the other side of it.</para>
///
/// <h3>Three costs, one press each, all on E</h3>
/// <list type="number">
/// <item><b>Cut it.</b> A channel that dies if you walk off it — the sounding's and the dig's own idiom —
/// and it spends a cut off a <see cref="HullCutter"/> cell you bought. That is the second half of the
/// owner's combi finally having a price.</item>
/// <item><b>Get in.</b> The pocket becomes geometry (<c>WreckLayout.Walls</c> takes the void now) and the
/// captain folds into it, plate back in its hole behind him — so the pressure hull is a wall again and
/// <see cref="InspectionTeam.Sees"/> fails on line of sight rather than on a stealth flag. #324's law pays
/// for the whole feature; the cubicle (#821) hid a captain from a round exactly this way.</item>
/// <item><b>Get out.</b> The plate comes off, the gap reopens, and he is standing in a room again.</item>
/// </list>
///
/// <h3>And what gives him away</h3>
/// <para><see cref="HullStowage.WhatGivesYouAway"/> owns the answer; this file only feeds it facts the sweep
/// already computes. The one worth naming here is <b>the warm cut</b>: for
/// <see cref="HullStowage.CutStaysWarmSeconds"/> the plate is a bright scar on a scarred ship, and a lamp
/// that lands on it inside that window opens it. Cut early and let it cool; cut as their boat mates on, and
/// the hole you made to hide in is the reason they find you.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Whether a sounding has found the plate. Distinct from <c>_voidOpened</c>: finding is not
    /// opening, and the deck draws a different console for each.</summary>
    private bool _plateFound;

    /// <summary>The captain is folded into the pocket with the plate back in its hole. There is no separate
    /// "plate shut" flag on purpose — being in there IS having pulled it to, because a captain who climbs
    /// into a hole and leaves the door hanging open has not made a decision, he has made a mistake.</summary>
    private bool _inTheVoid;

    /// <summary>Where the rig is biting and how long it has been at it. Null when not cutting.</summary>
    private (double X, double Y, double Held)? _cutting;

    /// <summary>How long since the cut went through, in seconds. Infinity before there is a cut, which is the
    /// honest answer to "how warm is it" on a hull with no hole in her.</summary>
    private double _secondsSinceTheCut = double.PositiveInfinity;

    /// <summary>Who had their lamp on the plate as it closed. The cubicle's rule (#821): a professional who
    /// watched the door shut does not have to see through it. Kept per call-sign, because one man seeing it
    /// is not the same as the team knowing it — the two who did not watch still walk their round.</summary>
    private readonly HashSet<string> _sawThePlateClose = [];

    /// <summary>The best moment in the scene is only worth saying once a boarding.</summary>
    private bool _theyWalkedPastOnce;

    /// <summary>How far a captain may drift and still be cutting the same plate — the sounding's own
    /// allowance, because it is the same rule: the clock buys the answer, and you are not moving while it
    /// runs.</summary>
    private const double CutDriftAllowed = 0.6;

    /// <summary>How much is left in the cell the captain is carrying, if any.</summary>
    private int CutsLeftInTheCell => HullCutter.CutsLeft(_satchel);

    // ── The E at the plate ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE PRESS, THREE VERBS, IN THE ORDER THE PLATE HAS LIVES. It is a <c>SecretDoor</c> rather than a
    /// <c>SealedDoor</c> because the expedition lane's sealed doors carry region ids and belong to its own
    /// bookkeeping — same verb to the player, different ledger.
    /// </summary>
    private void WorkTheFalsePlate()
    {
        if (_hullVoid is not { } found)
        {
            return;
        }

        if (!_voidOpened)
        {
            ToggleTheCut(found);
        }
        else if (_inTheVoid)
        {
            ClimbOutOfTheVoid(found);
        }
        else
        {
            ClimbIntoTheVoid(found);
        }
    }

    // ── Cutting ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Start (or abandon) the cut. The noise is made at the START — the sounding's own law: a
    /// captain who begins a cut and thinks better of it has still lit a rig against a bulkhead.</summary>
    private void ToggleTheCut(in HullSounding.HiddenVoid found)
    {
        if (_cutting is not null)
        {
            _cutting = null;
            ShowPulseMessage(HullCutter.AbandonedLine);
            StateHasChanged();
            return;
        }

        if (!HullCutter.InThePocket(_satchel))
        {
            ShowPulseMessage(HullCutter.NoCutterLine);
            StateHasChanged();
            return;
        }

        _cutting = (_avatarX, _avatarY, 0);
        ShowPulseMessage(HullCutter.WorkingLine);
        LogAutopilotEvent(HullCutter.OfferLine(CutsLeftInTheCell));
        MakeNoiseAboard(found.PlateX, found.PlateY, LoudEarshot);
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>Run the rig's clock, and cool the scar. Called once a frame from the sim; the cut dies the
    /// moment the captain steps off the plate.</summary>
    private void AdvanceTheCut(double dtRealSeconds)
    {
        double dt = System.Math.Min(dtRealSeconds, 0.1);

        if (_voidOpened)
        {
            _secondsSinceTheCut += dt;   // the scar cools whether anybody is watching it or not
        }

        if (_cutting is not { } spot || _surface is null || _hullVoid is not { } found)
        {
            return;
        }

        double dx = _avatarX - spot.X, dy = _avatarY - spot.Y;
        if ((dx * dx) + (dy * dy) > CutDriftAllowed * CutDriftAllowed)
        {
            _cutting = null;
            ShowPulseMessage(HullCutter.AbandonedLine);
            StateHasChanged();
            return;
        }

        double held = spot.Held + dt;
        _cutting = (spot.X, spot.Y, held);

        if (held < HullCutter.CutSeconds)
        {
            return;
        }

        _cutting = null;
        ForceTheFalsePlate(found);
    }

    /// <summary>The plate comes out, and the cell is one cut lighter.</summary>
    private void ForceTheFalsePlate(in HullSounding.HiddenVoid found)
    {
        HullCutter.Order order = HullCutter.Force(_satchel);
        if (!order.Cut)
        {
            ShowPulseMessage(order.Line);
            StateHasChanged();
            return;
        }

        _satchel = [.. order.Carried];
        _voidOpened = true;
        _secondsSinceTheCut = 0;

        ShowPulseMessage(HullSounding.FoundItLine(found));
        LogAutopilotEvent(HullSounding.FoundItLine(found));
        LogAutopilotEvent(order.Line);
        RendererInterop.PlayCue("reveal");

        // Loud, again, and permanent: a plate coming off a bulkhead is not a quiet act, and the hole it
        // leaves is on this hull for anybody who reads her afterwards.
        MakeNoiseAboard(found.PlateX, found.PlateY, LoudEarshot);
        RebuildWreckDeck();
        RequestVaultSave();
        StateHasChanged();
    }

    // ── Getting in, and out ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// FOLD IN AND PULL IT TO. The order of operations is the cubicle's, and it matters: ask who was LOOKING
    /// before the wall exists, then close it. Asking afterwards would ask through the plate the captain has
    /// just fitted, and every witness would vanish at the moment of being made.
    /// </summary>
    private void ClimbIntoTheVoid(in HullSounding.HiddenVoid found)
    {
        if (HullStowage.RoomForACaptain(found) != HullStowage.Fit.Fits)
        {
            ShowPulseMessage(HullStowage.TooNarrowLine);
            StateHasChanged();
            return;
        }

        RememberWhoWatchedThePlateClose(found);

        _inTheVoid = true;
        _avatarX = found.PlateX;
        _avatarY = found.Top
            ? (WreckLayout.TopY + WreckLayout.OuterTopY) / 2.0
            : (WreckLayout.BottomY + WreckLayout.OuterBottomY) / 2.0;

        RebuildWreckDeck();
        ShowPulseMessage(HullStowage.ClimbedInLine);
        LogAutopilotEvent(HullStowage.ClimbedInLine);
        RendererInterop.PlayCue("blip");
        StateHasChanged();
    }

    /// <summary>…and back out into a room that is suddenly very wide.</summary>
    private void ClimbOutOfTheVoid(in HullSounding.HiddenVoid found)
    {
        _inTheVoid = false;
        _avatarX = found.PlateX;
        _avatarY = found.Top ? WreckLayout.TopY + 1.2 : WreckLayout.BottomY - 1.2;

        // Everybody forgets a plate they watched close, once it has been opened in front of them — the
        // cubicle's own EverybodyForgetsTheCatch. What they saw was a wall shutting; there is no wall now.
        _sawThePlateClose.Clear();
        _theyWalkedPastOnce = false;

        RebuildWreckDeck();
        ShowPulseMessage(HullStowage.ClimbedOutLine);
        RendererInterop.PlayCue("blip");
        StateHasChanged();
    }

    /// <summary>Who had the plate in their lamp as it went home. Asked of the walls as they are RIGHT NOW,
    /// before the pocket is rebuilt with the plate in it.</summary>
    private void RememberWhoWatchedThePlateClose(in HullSounding.HiddenVoid found)
    {
        _sawThePlateClose.Clear();
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        foreach (Sweeper s in _sweepers)
        {
            InspectionTeam.Member member = new(s.Callsign, s.X, s.Y, s.Facing, s.State, s.StateSeconds);
            if (InspectionTeam.Sees(member, found.PlateX, found.PlateY, sight))
            {
                _sawThePlateClose.Add(s.Callsign);
            }
        }
    }

    // ── What one sweeper makes of a plate ────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ONE QUESTION THE SWEEP ASKS ABOUT A STOWAWAY, and every input is something the sweep already
    /// computes. On a hull with no void this collapses to exactly the sighting test it replaced, which is
    /// the point: hiding is not a special case bolted onto the scene, it is two more facts fed to it.
    /// </summary>
    private HullStowage.Tell WhatGivesTheStowawayAway(
        in InspectionTeam.Member member, IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        bool seesCaptain = InspectionTeam.Sees(member, _avatarX, _avatarY, sight);

        if (_hullVoid is not { } found || !_voidOpened)
        {
            return seesCaptain ? HullStowage.Tell.StandingInTheOpen : HullStowage.Tell.None;
        }

        bool lampOnThePlate = InspectionTeam.Sees(member, found.PlateX, found.PlateY, sight);

        HullStowage.Tell tell = HullStowage.WhatGivesYouAway(
            insideTheVoid: _inTheVoid,
            plateShut: _inTheVoid,
            theyWatchedYouGetIn: _sawThePlateClose.Contains(member.Callsign),
            secondsSinceTheCut: _secondsSinceTheCut,
            theirLampIsOnThePlate: lampOnThePlate,
            theyCanSeeYou: seesCaptain);

        // THE PAYOFF, said once. A lamp crossing the plate with a captain behind it and nothing to show for
        // it is the whole reason the void is a place rather than a chest, and a scene whose best moment is
        // silent is a scene the player never knows they won.
        if (_inTheVoid && lampOnThePlate && !_theyWalkedPastOnce && !HullStowage.Caught(tell))
        {
            _theyWalkedPastOnce = true;
            ShowPulseMessage(HullStowage.TheyWalkedPastLine);
            LogAutopilotEvent(HullStowage.TheyWalkedPastLine);
        }

        return tell;
    }

    // ── The clock strip ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The seconds a cut is committed to, in the same column the pumps, the boat and the sounding
    /// use — because it is the same kind of number.</summary>
    private double? CutSecondsLeft =>
        _cutting is { } spot ? System.Math.Max(0, HullCutter.CutSeconds - spot.Held) : null;

    /// <summary>
    /// AND THE ONE CLOCK THE PLAYER MUST BE ABLE TO SEE. The warm cut is the tell that decides the scene, so
    /// hiding it would make the rule a trap rather than a decision. Shown only while it matters: there is a
    /// hole, it is still bright, and there is somebody aboard who might look at it.
    /// </summary>
    private double? WarmCutSecondsLeft =>
        _voidOpened && SweepersAboard && HullStowage.CutIsWarm(_secondsSinceTheCut)
            ? System.Math.Max(0, HullStowage.CutStaysWarmSeconds - _secondsSinceTheCut)
            : null;
}
