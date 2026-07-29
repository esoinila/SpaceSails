using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// WHAT THE CREW HAVE TO GO ON. Owner: <i>"let's have a Winningtemp-like crew satisfaction report on the
/// captain's desk … it is the captain's performance as seen by the crew."</i>
///
/// <para>The rules and the words are Core's (<see cref="CrewTemp"/>, pure and tested). This is the wiring:
/// the aggregates of the voyage so far, gathered from state the game ALREADY keeps, so the sheet is a
/// reading of what actually happened rather than a second bookkeeping system nobody remembers to update.</para>
///
/// <para>Some inputs are honest zeroes for now and say so at their line. That is deliberate: a sheet with
/// five real dimensions and two dormant ones is worth having today, and each dormant one is a named hook
/// rather than a number invented to fill a bar.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Wreck causes filed truthfully when a lie would have paid better — money the crew did not get,
    /// and they can count.</summary>
    private int _honestFilings;

    /// <summary>Causes filed falsely, and hulls stripped that were never reported at all.</summary>
    private int _profitableLies;

    /// <summary>
    /// DORMANT, AND THE COMPILER SAID SO. Word given to the crew and honoured, or not — and people who did
    /// not come home.
    ///
    /// <para>These were fields until the build refused them: "never assigned to, and will always have its
    /// default value". It was right, and the refusal is worth keeping rather than silencing. There is nothing
    /// in the game yet that makes a promise TO THE CREW — the tow offer and the rescue are promises to
    /// strangers — and no crewman can die, because the crew are three droids on a rota and one marker.</para>
    ///
    /// <para>So they are constants with their hooks named, and the two dimensions that read them sit at their
    /// baselines honestly. A bar that moved for invented reasons would be worse than a bar that does not
    /// move: the captain cannot act on a number nobody is keeping.</para>
    /// </summary>
    private const int PromisesKept = 0;      // hook: a run ashore offered, a share pledged, a rescue promised
    private const int PromisesBroken = 0;    // hook: the same, not honoured — the line that ends captaincies
    private const int CrewLost = 0;          // hook: needs individual crew before anybody can fail to return

    /// <summary>Times it nearly went wrong and everyone got back. This one IS live — it is the cheapest
    /// honest signal aboard, and it buys tolerance rather than happiness.</summary>
    private int _nearMisses;

    /// <summary>The captain opened one of HER compartments with somebody inside it. Not a shade of anything —
    /// its own fact, and the crew's answer to it is the suit they now keep closer.</summary>
    private bool _ventedOwnCompartment;

    /// <summary>Everything the crew have formed an opinion from, as the sheet reads it.</summary>
    private CrewTemp.Voyage CrewVoyage() => new(
        HonestFilings: _honestFilings,
        ProfitableLies: _profitableLies,

        // The share, approximated by what the voyage has actually earned. A real per-share distribution
        // wants a crew model with individuals in it; until then the hold's takings are what the crew see.
        SharePaid: _credits,

        PromisesKept: PromisesKept,
        PromisesBroken: PromisesBroken,
        CrewLost: CrewLost,
        NearMisses: _nearMisses,

        // The galley's own counter, already kept and already conspicuous when it does not move.
        TotsPoured: _rumTots,

        // DORMANT, honestly: nothing tracks shore leave yet. Zero reads as "recently ashore", which is the
        // kind answer while the input does not exist — better than inventing a number that would make REST
        // move for reasons the captain cannot see or act on.
        DaysSinceShoreLeave: 0,

        Heat: _heat.Level,
        VentedOwnCompartment: _ventedOwnCompartment);

    /// <summary>The crew's standing, for anything that wants to know how close they are to acting.</summary>
    private CrewTemp.Standing CrewStanding() => CrewTemp.StandingOf(CrewVoyage());

    /// <summary>Record a wreck cause filed honestly when the profitable answer was available. Called where
    /// the filing already happens, so nothing new has to be remembered.</summary>
    private void NoteHonestFiling()
    {
        _honestFilings++;
        RequestVaultSave();
    }

    /// <summary>And the other road.</summary>
    private void NoteProfitableLie()
    {
        _profitableLies++;
        RequestVaultSave();
    }

    /// <summary>The captain opened a compartment of his own ship with a man in it. Recorded once — it does
    /// not need a count, because the crew do not forget the first one.</summary>
    private void NoteVentedOwnCompartment()
    {
        if (_ventedOwnCompartment)
        {
            return;
        }

        _ventedOwnCompartment = true;
        LogAutopilotEvent("🌡 The crew have noticed. They all keep a suit closer than they used to.");
        RequestVaultSave();
    }

    /// <summary>A boarding, a soak, a chase that everyone came home from. This is what buys tolerance rather
    /// than happiness — see <see cref="CrewTemp.Cohesion"/>.</summary>
    private void NoteNearMiss()
    {
        _nearMisses++;
        RequestVaultSave();
    }
}
