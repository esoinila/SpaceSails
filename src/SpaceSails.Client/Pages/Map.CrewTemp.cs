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
/// <para>Some inputs are honest zeroes and say so at their line. That is deliberate: each dormant one is a
/// named hook rather than a number invented to fill a bar. #1066 took one of them live —
/// <c>PromisesBroken</c>, fed by shore leave counted in berths — and left the two beside it dormant with a
/// sharper reason written at each.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Wreck causes filed truthfully when a lie would have paid better — money the crew did not get,
    /// and they can count.</summary>
    private int _honestFilings;

    /// <summary>Causes filed falsely, and hulls stripped that were never reported at all.</summary>
    private int _profitableLies;

    /// <summary>
    /// STILL DORMANT, AND THE COMPILER SAID SO. Word given to the crew and HONOURED.
    ///
    /// <para>This was a field until the build refused it: "never assigned to, and will always have its
    /// default value". It was right, and the refusal is worth keeping rather than silencing. Nothing in the
    /// game yet makes a promise TO THE CREW and then keeps it — the tow offer and the rescue are promises
    /// to strangers, and a run ashore given on time is not a promise kept, it is a promise not broken (which
    /// is what <see cref="_workingStopsSinceShoreLeave"/> going back to zero already says).</para>
    ///
    /// <para>So it is a constant with its hook named, and the half of THE CAPTAIN'S WORD that reads it sits
    /// at its baseline honestly. A bar that moved for invented reasons would be worse than a bar that does
    /// not move: the captain cannot act on a number nobody is keeping.</para>
    /// </summary>
    private const int PromisesKept = 0;      // hook: a share pledged and paid, a rescue promised and flown

    /// <summary>
    /// #1066 · SHORE LEAVE AS A LEDGER LINE — consecutive berths with no run ashore, and the only number
    /// this sheet has ever kept across a save.
    ///
    /// <para><b>The rule, and why it is legible.</b> A clamp is a RUN ASHORE only at a great port
    /// (<see cref="ArrivalTube.Tier.GreatPort"/>); every other berth is a WORKING STOP. Nothing new has to
    /// be explained to the player, because the game already tells him which one he is at, in the picture it
    /// raises at that very clamp: the arrival tube's establishing shot (#541) is a long glazed gangway with
    /// <i>"two streams of people … and nobody who has any idea who you are"</i>, or one rigid tube with
    /// <i>"two dock workers waiting for you to get out of the way"</i>. The first is somewhere a crew can
    /// disappear into for a night. The second is a shift.</para>
    ///
    /// <para>And the tier is DERIVED and never authored — <see cref="ArrivalTube.TierFor"/> reads the
    /// scenario's own scheduled-traffic model — so a new berth in a new scenario gets the right answer
    /// without anybody remembering to tag it, and the ledger line can never disagree with the picture.</para>
    ///
    /// <para>Session state elsewhere on this sheet is honest (a voyage's filings and near-misses are what
    /// this run has done); this one is not, and that is the difference between a mood and a PROMISE. A crew
    /// kept aboard for six berths do not forget it over a reload, so it rides the vault — one integer, in
    /// <see cref="ProgressSection.WorkingStopsSinceShoreLeave"/>, written only when somebody is counting.</para>
    /// </summary>
    private int _workingStopsSinceShoreLeave;

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
    /// <see cref="_nearMisses"/> beside it. (#1066 vaulted the one input on this sheet that is a PROMISE
    /// rather than a mood — <see cref="_workingStopsSinceShoreLeave"/> — and deliberately left the rest of
    /// the voyage's aggregates where they were.)</para>
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

        // #1066 · LIVE AT LAST, and through the hook the dimension names first: THE CAPTAIN'S WORD is made
        // of "a run ashore, a share, a rescue", and this is the run ashore. The arithmetic — where the line
        // is, and that every berth past it breaks the word again — is Core's, so the owner's dial is in one
        // place and this seam only reports the tally.
        PromisesBroken: CrewTemp.ShoreLeavePromisesBroken(_workingStopsSinceShoreLeave),

        CrewLost: _crewLost,
        NearMisses: _nearMisses,

        // The galley's own counter, already kept and already conspicuous when it does not move.
        TotsPoured: _rumTots,

        // STILL DORMANT after #1066, and now for a sharper reason than "nothing tracks shore leave". Shore
        // leave IS tracked — in BERTHS (see _workingStopsSinceShoreLeave), because a berth is the event the
        // game actually has. This field is DAYS, and turning stops into days to make REST move would be the
        // invented number this sheet exists to refuse. REST therefore reads its rum and nothing else, and
        // the overdue run ashore lands on THE CAPTAIN'S WORD where the promise was made.
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

    /// <summary>
    /// #1066 · THE BERTH, AS THE CREW SEE IT. Called from the one clamp
    /// (<c>Map.Docking.ClampOntoHaven</c>) with the tier that clamp has already worked out for the arrival
    /// tube's establishing shot — <b>one tier read, two reporters</b>, which is #1065's law and matters here
    /// for the ordinary reason: a second place deciding what kind of place this is would be a second set of
    /// books, and the picture on the screen and the line on the sheet could then disagree about the same
    /// berth.
    ///
    /// <para>A great port puts the counter back to nothing — the crew went ashore, and what they say
    /// afterwards is different from what they said before, because this sheet is a temperature and not a
    /// court record. Anything else adds one.</para>
    ///
    /// <para>No pulse line, deliberately. The clamp already says its piece and the tube's plate is already
    /// on the screen; a third sentence at the same moment would be the stacked-card failure in the one
    /// channel the player cannot close. Where the counter stands is on the crew sheet, which is where the
    /// captain goes to ask (#761).</para>
    /// </summary>
    private void NoteTheBerthTheCrewGot(ArrivalTube.Tier tier)
    {
        _workingStopsSinceShoreLeave = tier == ArrivalTube.Tier.GreatPort
            ? 0
            : _workingStopsSinceShoreLeave + 1;

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
    ///
    /// <para>#1066 · <c>/map?crew=meeting</c> is the same door, one landing further down. It grants the
    /// deputation's two counters AND a run of working stops long enough that the word is broken — and it is
    /// worth saying that BOTH are still needed, because that is the design: shore leave alone can never
    /// convene the meeting on a ship that pays and brings people home (<c>ShoreLeaveIsALedgerLineTests</c>
    /// sweeps a hundred berths to say so). The meeting is what a captain gets for failing them in several
    /// ways at once.</para>
    /// </summary>
    /// <param name="which">"petition" or "meeting" — which landing of the same staircase.</param>
    private void SeedCrewCheat(string which)
    {
        _crewLost = DeflectionCrewSize;
        _honestFilings = HonestFilingsThatEmptyTheShare;

        if (which == "meeting")
        {
            _workingStopsSinceShoreLeave = WorkingStopsThatBreakTheWordTwice;

            ShowPulseMessage(
                "🕯 Test: five of the crew did not come home from the rock, every wreck since has been filed " +
                $"honestly, and nobody has been ashore in {WorkingStopsThatBreakTheWordTwice} berths. Read " +
                "the crew sheet on the captain's desk — and then wait for the cantina at an odd watch.");
            return;
        }

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

    /// <summary>#1066 · Berths with no run ashore — enough that THE CAPTAIN'S WORD is broken twice over,
    /// which is what the Ultimatum edge costs on top of the bodies and the empty share. Derived from Core's
    /// dial rather than typed, so the owner turning
    /// <see cref="CrewTemp.WorkingStopsBetweenRunsAshore"/> moves the dev door with the game; the claim that
    /// this number really does convene the meeting is pinned in
    /// <c>TheCrewSheetCountsTheStopsAshoreTests</c>.</summary>
    private const int WorkingStopsThatBreakTheWordTwice = CrewTemp.WorkingStopsBetweenRunsAshore + 1;

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
    /// <para><b>Petition OR WORSE</b>, deliberately. A standing that skips past the edge must still show the
    /// deputation once rather than nothing at all.</para>
    ///
    /// <para><b>#1066 · AND THE MEETING, ONE LANDING DOWN.</b> <c>StoryBeats.Beat.CrewMeeting</c> was the
    /// last beat on <c>EveryStoryBeatHasACallerTests.KnownOrphans</c>, excused because its
    /// <see cref="CrewTemp.Standing.Ultimatum"/> edge needed <i>a broken promise TO THE CREW or months
    /// without shore leave</i> — numbers nobody kept. Shore leave is kept now
    /// (<see cref="_workingStopsSinceShoreLeave"/>), it reaches the sheet as a broken promise, and the edge
    /// is crossable, so the excuse is spent and the beat is raised here beside its sibling — one watcher,
    /// one read of the sheet, two edges.</para>
    ///
    /// <para><b>Why the meeting is raised on the CROSSING and the deputation is not.</b> The two beats have
    /// different cadences and the difference is the whole of it. <c>CrewDeputation</c> is
    /// <see cref="StoryBeats.Cadence.OnceEver"/>, so raising it on every worsened reading is free — the one
    /// door answers, and a local "have we told them yet" bool would be a mirror of the seen-set with nobody
    /// to keep it honest. <c>CrewMeeting</c> is <see cref="StoryBeats.Cadence.EveryTime"/>, and it is that
    /// for a stated reason (<c>StoryBeatsTests.OnlyRareMomentsFireEveryTime</c>: only moments RARE BY THEIR
    /// OWN NATURE may): the meeting happening at all is rare, but an ultimatum is a STANDING and a standing
    /// is a state that stays true — a captain sitting at one earns a credit and the sheet moves, and an
    /// unconditional raise here would put a modal on the screen every time his purse changed. So what is
    /// rare is the CROSSING, and the crossing is what is raised.</para>
    ///
    /// <para>That is not a second seen-flag either: it is read off <c>_crewVoyageLastRead</c>, the state
    /// this method already holds for its own short-circuit. Nothing new is remembered, and a captain who
    /// fixes it and breaks it again gets a second meeting, which is right — the crew would hold a second
    /// one.</para>
    /// </summary>
    private void WatchWhereTheCrewStand()
    {
        CrewTemp.Voyage now = CrewVoyage();
        if (now.Equals(_crewVoyageLastRead))
        {
            return; // the crew have learned nothing since the last frame
        }

        CrewTemp.Standing was = CrewTemp.StandingOf(_crewVoyageLastRead);
        _crewVoyageLastRead = now;

        CrewTemp.Standing standing = CrewTemp.StandingOf(now);

        // #1066 · ONE BEAT PER READING, AND IT IS THE WORSE ONE. A crew at an ultimatum are PAST asking —
        // the deputation is "the last cheap moment", hats in hands, and it is not what happens after the
        // meeting has been held. So a standing at Ultimatum takes the meeting and returns: raising both on
        // one reading would put two cards up in one frame, and whichever went second would silently paint
        // over the first. If the captain hauls it back to a Petition, the deputation is there waiting for
        // him, which is the right way round.
        if (standing >= CrewTemp.Standing.Ultimatum)
        {
            if (was < CrewTemp.Standing.Ultimatum)
            {
                // The cantina at an odd watch, and a chair pulled out that nobody is sitting in. What they
                // DO about it — the date they name, the island and the pistol — is #519's scope; this is
                // the moment the captain learns it is being decided without him.
                RaiseStoryBeat(StoryBeats.Beat.CrewMeeting);
            }

            return;
        }

        if (standing >= CrewTemp.Standing.Petition)
        {
            RaiseStoryBeat(StoryBeats.Beat.CrewDeputation);
        }
    }
}
