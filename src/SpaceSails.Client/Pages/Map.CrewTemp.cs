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
    /// DORMANT, AND THE COMPILER SAID SO. Word given to the crew and honoured, or not.
    ///
    /// <para>These were fields until the build refused them: "never assigned to, and will always have its
    /// default value". It was right, and the refusal is worth keeping rather than silencing. There is nothing
    /// in the game yet that makes a promise TO THE CREW — the tow offer and the rescue are promises to
    /// strangers.</para>
    ///
    /// <para>So they are constants with their hooks named, and the dimension that reads them sits at its
    /// baseline honestly. A bar that moved for invented reasons would be worse than a bar that does not
    /// move: the captain cannot act on a number nobody is keeping.</para>
    /// </summary>
    private const int PromisesKept = 0;      // hook: a run ashore offered, a share pledged, a rescue promised
    private const int PromisesBroken = 0;    // hook: the same, not honoured — the line that ends captaincies

    /// <summary>
    /// #663 · PEOPLE WHO DID NOT COME HOME — and the constant that had quietly stopped being honest.
    ///
    /// <para>This sat beside the two above as <c>private const int CrewLost = 0</c>, excused as <i>"needs
    /// individual crew before anybody can fail to return"</i>. That was true when it was written and was
    /// false by the time #663 quoted it: the asteroid-deflection gig (#394) puts five of the crew on a rock
    /// falling at the Ringside Exchange, rolls <see cref="DeflectionGig"/>'s crew-bolt complication against
    /// them, docks the fee per body, and prints <i>"N of the crew did not come home."</i> Meanwhile the
    /// report on the captain's desk answered <i>"Nobody has been lost. On a ship like this that is not luck,
    /// it is the captain."</i> — the sim doing one thing while a sentence said another.</para>
    ///
    /// <para>Aggregate by construction, like everything else on this sheet: <see cref="CrewTemp"/> is a
    /// TEMPERATURE and not a roster, so a COUNT is exactly the shape it wants, and it does not need
    /// individual crew after all. Session state, like <see cref="_honestFilings"/> and
    /// <see cref="_nearMisses"/> beside it — this sheet has never been vaulted.</para>
    ///
    /// <para>Deliberately NOT counted here: an away expedition's <c>ExpeditionScientistsLost</c>. Those are
    /// contractors the captain carried, and the crew's report is about the crew. Somebody should decide
    /// whether the galley grieves a scientist; nobody has, so it is not assumed.</para>
    /// </summary>
    private int _crewLost;

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
        CrewLost: _crewLost,
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

    /// <summary>#663 · One of the crew did not come home. Called where the loss is already DECIDED, so the
    /// fee the gig docks and the memory the crew keep come off the same increment — a second place that
    /// decides somebody died would be a second bookkeeping system, and the sheet's whole point is that it is
    /// a reading of what happened rather than one of those.</summary>
    private void NoteCrewDidNotComeHome()
    {
        _crewLost++;
        RequestVaultSave();
    }

    /// <summary>A boarding, a soak, a chase that everyone came home from. This is what buys tolerance rather
    /// than happiness — see <see cref="CrewTemp.Cohesion"/>.</summary>
    private void NoteNearMiss()
    {
        _nearMisses++;
        RequestVaultSave();
    }

    /// <summary>
    /// #663 · <c>/map?crew=petition</c> — the dev door onto the deputation. <c>Map.Sim.World.QueryArcs</c>
    /// carries the reasoning for why it exists; this is what it grants, and it is deliberately only ever
    /// COUNTERS.
    ///
    /// <para>Both halves are needed, which is the design and not a threshold. The bodies are the whole
    /// deflection roster left on the rock — read off <see cref="DeflectionCrewSize"/> rather than typed, so
    /// the cheat cannot drift from the gig that is the only thing in the game that kills a crewman. The
    /// filings are a dozen wreck causes told truthfully: enough that THE SHARE has bottomed out whatever
    /// the purse holds, because a captain who lies and pays well can bury people quietly and the sheet is
    /// right to say so.</para>
    ///
    /// <para>Nothing here writes a standing, raises a beat or pushes a card. The ship's own clock reads the
    /// sheet on the next tick and the deputation arrives through the one door, which is the only way to
    /// test the WIRING rather than the cheat.</para>
    /// </summary>
    private void SeedCrewCheat()
    {
        _crewLost = DeflectionCrewSize;
        _honestFilings = HonestFilingsThatEmptyTheShare;

        ShowPulseMessage(
            "🧑‍🔧 Test: five of the crew did not come home from the rock, and every wreck since has been " +
            "filed honestly. Watch the corridor outside your door — and read the crew sheet on the " +
            "captain's desk for what they are about to ask for.");
    }

    /// <summary>Wreck causes filed honestly — enough that <c>CrewTemp</c>'s PAY line reads zero on any purse
    /// the game can hand a captain. Used by the dev door above; the arithmetic behind it is Core's
    /// (<c>CrewTempTests</c>) and the number is pinned against the shipping sheet in
    /// <c>TheCrewSheetCountsTheDeadTests</c>.</summary>
    private const int HonestFilingsThatEmptyTheShare = 12;

    /// <summary>What the crew had to go on the last time anybody asked. Held so the standing is recomputed
    /// when — and only when — the thing it is computed FROM has moved; a <see cref="CrewTemp.Voyage"/> is a
    /// readonly record struct, so this costs a comparison and allocates nothing on a frame where the crew
    /// have learned nothing new.</summary>
    private CrewTemp.Voyage _crewVoyageLastRead;

    /// <summary>
    /// #663 · THREE OF THEM IN THE CORRIDOR OUTSIDE YOUR DOOR. <c>StoryBeats.Beat.CrewDeputation</c> shipped
    /// with a painted canvas, a cadence and nobody to raise it; the issue's own plan was to raise it on the
    /// <see cref="CrewTemp.Standing.Petition"/> edge — <i>"a deputation, and a specific thing they want
    /// fixed. This is the last cheap moment."</i>
    ///
    /// <para><b>Why here and not at a Note* seam.</b> The standing is a pure function of the whole voyage,
    /// and the inputs that make it worse are written in a dozen places — a filing at a wreck console, heat
    /// from a robbery, a body left on a rock, a purse that shrank. Hooking each of them would be a dozen
    /// chances to forget one, which is what left the beat silent in the first place. So the ship asks the
    /// sheet, on her own clock, and the answer changes only when the sheet's inputs do.</para>
    ///
    /// <para><b>No second seen-flag.</b> The cadence is <see cref="StoryBeats.Cadence.OnceEver"/> and the
    /// seam that owns cadence is <c>RaiseStoryBeat</c>; a "have we told them yet" bool here would be a
    /// mirror of the seen-set with nobody to keep it honest. Raising on every worsened reading and letting
    /// the one door answer is the same law the arc wire is written under.</para>
    ///
    /// <para><b>Petition OR WORSE</b>, deliberately. Today the shipped inputs bottom out exactly at Petition
    /// (<c>TheCrewSheetCountsTheDeadTests</c> sweeps the page's own voyage and pins it), so this reads as
    /// "the Petition edge" — but a standing that ever skips past it must still show the deputation once
    /// rather than nothing at all. <c>CrewMeeting</c>'s Ultimatum edge is NOT wired beside it, because
    /// nothing in the game writes an input that can reach it, and a caller that can never fire is this
    /// house's fifth bug class wearing the fix's clothes.</para>
    /// </summary>
    private void WatchWhereTheCrewStand()
    {
        CrewTemp.Voyage now = CrewVoyage();
        if (now.Equals(_crewVoyageLastRead))
        {
            return; // the crew have learned nothing since the last frame
        }

        _crewVoyageLastRead = now;

        if (CrewTemp.StandingOf(now) >= CrewTemp.Standing.Petition)
        {
            RaiseStoryBeat(StoryBeats.Beat.CrewDeputation);
        }
    }
}
