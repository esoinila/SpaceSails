using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #488 · MAKING SURE — the third road off a wreck, for a captain who wants nothing from her except that
/// she stop existing. Owner: <i>"we could have a self-destruct option in the ship, like reactor overload …
/// in cases where we want to be sure."</i>
///
/// <para>The fast method only, for now: the reactor. It is the one the away team is PRESENT for, and the
/// only one that turns disposal into a scene — set it, then walk back down the corridor you have been
/// fighting in all boarding, past the doors you dogged and the rooms you vented. Your own housekeeping is
/// either a clear road out or a maze you built for yourself.</para>
///
/// <para>The rules are Core's (<see cref="Scuttle"/>). This is the panel and the clock. See
/// <c>docs/features/making-sure.md</c>.</para>
/// </summary>
public sealed partial class Map
{
    private bool _showScuttlePanel;

    /// <summary>Seconds left on the overload, or null when nothing is running.</summary>
    private double? _scuttleSecondsLeft;

    /// <summary>The card shown from the shuttle after she goes — the confirmation, which never once says
    /// what it was.</summary>
    private string? _scuttleEpitaph;

    /// <summary>Whether anything the vacuum had not finished was still aboard when she went. Captured at
    /// the moment of departure, because by the time the card is drawn the wreck is gone.</summary>
    private bool _scuttleHeardIt;

    /// <summary>Press E at the scuttling panel.</summary>
    private void OpenScuttlePanel()
    {
        if (_wreck is null)
        {
            return;
        }
        _showScuttlePanel = true;
        RendererInterop.PlayCue("board");
    }

    private void CloseScuttlePanel() => _showScuttlePanel = false;

    /// <summary>The methods this hull can actually do. For now only the reactor is offered — the set-course
    /// methods need the wreck to be dispatched somewhere after the away team is home, which is its own
    /// lane. The others still appear, refused, with Core's reason, so the captain can see the shape of the
    /// choice before it exists.</summary>
    private IReadOnlyList<Scuttle.Method> ScuttleMethods() =>
        _wreck is { } w
            ? Scuttle.Available(w.Cause, rockInReach: false, atmosphereInReach: false, tanksAreGood: false)
            : [];

    /// <summary>Turn both keys. There is no confirmation dialog and there is not going to be one — the
    /// panel has already told the captain what she is worth and that none of it survives.</summary>
    private void ArmTheOverload()
    {
        if (_wreck is null || _scuttleSecondsLeft is not null)
        {
            return;
        }

        _scuttleSecondsLeft = Scuttle.OverloadSeconds;
        _showScuttlePanel = false;
        ShowPulseMessage(Scuttle.ArmedLine);
        LogAutopilotEvent($"☢ Armed the overload aboard the {_wreck.Value.ShipName}.");
        RendererInterop.PlayCue("alarm");
    }

    /// <summary>Back the keys out — while there is still a panel willing to take them.</summary>
    private void AbortTheOverload()
    {
        if (_scuttleSecondsLeft is not { } left)
        {
            return;
        }

        if (!Scuttle.CanAbort(left))
        {
            ShowPulseMessage(Scuttle.PastRecallLine);
            RendererInterop.PlayCue("block");
            return;
        }

        _scuttleSecondsLeft = null;
        _showScuttlePanel = false;
        ShowPulseMessage(Scuttle.AbortedLine);
        RendererInterop.PlayCue("board");
    }

    /// <summary>Run the clock. It does not stop for cards, it does not stop for the valve board, and it
    /// does not care which doors the captain dogged on the way aft.</summary>
    private void AdvanceScuttleClock(double dtSeconds)
    {
        if (_scuttleSecondsLeft is not { } left || _wreck is null)
        {
            return;
        }

        double before = left;
        left -= dtSeconds;

        // She announces it herself. The one system aboard still able to power a speaker is the exact thing
        // that is about to fail, so a hull silent for forty years starts talking — to a crew that is not
        // coming.
        if (Scuttle.PaCall(before, left) is { } call)
        {
            ShowPulseMessage(call);
            RendererInterop.PlayCue("alarm");
        }

        if (left > 0)
        {
            _scuttleSecondsLeft = left;
            return;
        }

        // Still aboard. The overload does not negotiate, and the insurance issues a new captain.
        //
        // #525 · THE CAUSE IS PASSED, NOT ROLLED. This called the trigger with no known cause, so the card
        // fell to DeathNarration.SurfaceEnd and told a captain who had turned two keys ninety seconds
        // earlier that "an Old One's hand is the last straw" — on a drive-failure hull with nothing living
        // in her. The one death in the game the captain unambiguously CHOSE was narrated as being taken by
        // something else, which is the same failure #545 and #609 were filed about, on the worst possible
        // beat for it. The clock knows exactly what killed them; it just was not saying.
        _scuttleSecondsLeft = null;
        if (_surface is { } dying && _busted is null)
        {
            LogAutopilotEvent("☢ The overload ran out with the away team still aboard.");
            TriggerSurfaceOverdrawDeath(dying, nerveRanOut: false, known: DeathCause.Scuttled);
        }
    }

    /// <summary>Whether anything the vacuum had not finished is still alive aboard her. This is the ONLY
    /// input to what the captain hears, and it is deliberately coarse: any compartment still holding an
    /// unfinished infestation, or any of the pack still walking her decks.</summary>
    private bool SomethingStillAliveAboard()
    {
        if (_reevers.Count > 0)
        {
            return true;
        }

        foreach (HullVenting.Space s in _ventSpaces.Values)
        {
            if (s.Infested && !HullVenting.SoakComplete(s))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Called as the shuttle pulls away with the keys turned. Captures what was still aboard —
    /// the wreck is about to stop existing, so the question cannot be asked afterwards — and writes the
    /// card the captain reads from their own cockpit.</summary>
    private void ResolveScuttleOnDeparture()
    {
        if (_scuttleSecondsLeft is null || _wreck is null)
        {
            return;
        }

        _scuttleHeardIt = SomethingStillAliveAboard();
        _scuttleEpitaph = Scuttle.SheGoes(Scuttle.Method.ReactorOverload, _scuttleHeardIt);
        _scuttleSecondsLeft = null;

        // The only relief in this lane that cannot be bought in a bar: the captain is finally allowed to
        // stop wondering. Priced in PIPS by Core, converted here to the raw amount the relief seam takes.
        ApplyNerveRelief(Scuttle.Relief(_scuttleHeardIt) * NervePips.PipUnit);

        LogAutopilotEvent($"☢ {_wreck.Value.ShipName} is gone.");
        RendererInterop.PlayCue("alarm");
        RequestVaultSave();
    }

    private void CloseScuttleEpitaph() => _scuttleEpitaph = null;
}
