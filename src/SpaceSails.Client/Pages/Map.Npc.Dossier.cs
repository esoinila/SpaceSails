using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Npc (the header note lives in Map.Npc.cs) — THE FILE ON ONE CONTACT. M29's course
// opportunities (every known hull whose predicted coast the currently plotted course happens to pass
// near — fly an innocent course, see who drifts conveniently close along it), the dev `?target=` seed
// that puts a named hunter in the sky, and the card itself: `DossierInfo` with its five #534 tells,
// `HuntTerms`, `DossierFor` which fills one in off the live state, and `TermsOfTheHunt`, which hands Core
// the two numbers only the page can know and lets Core compose every sentence beside the rule it
// describes. `SheIsOnHerRouteByNow` closes the section where the file left it: it is the same question
// the dossier asks before it will show a hull at all and `StepNpcs` asks before it will integrate one.
public partial class Map
{
    // M29 (the cover-story seed): every KNOWN contact whose predicted coast the current plotted
    // course happens to pass near — the Sensors desk reads these as targets of opportunity.
    // Fly an innocent course; see who drifts conveniently close along it.
    private readonly List<SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity> _courseOpportunities = [];

    private void UpdateCourseOpportunities()
    {
        _courseOpportunities.Clear();
        if (_simulator is null || _samples.Count < 2)
        {
            return;
        }

        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived || npc.Disabled)
            {
                continue;
            }

            // Honest intel only: the course scan sees what the sensors see.
            bool tracked = _trackingPost is not null && _trackingPost.TryGetTrack(npc.Ship.Id, out _);
            if (!npc.CurrentlyObserved && !tracked)
            {
                continue;
            }

            double horizon = _samples[^1].SimTime - npc.State.SimTime;
            if (horizon <= 0)
            {
                continue;
            }

            IReadOnlyList<TrajectorySample> theirs = _simulator.ProjectAdaptive(npc.State, null, horizon, maxSamples: 400);
            if (InterceptEstimate.Against(_samples, theirs, CaptureRule.CaptureRadiusMeters) is { } pass)
            {
                _courseOpportunities.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity(
                    npc.Ship.Id, npc.Ship.Callsign, pass.MinDistance, pass.MinSimTime, tracked));
            }
        }

        _courseOpportunities.Sort((a, b) => a.MinDistance.CompareTo(b.MinDistance));
        if (_courseOpportunities.Count > 8)
        {
            _courseOpportunities.RemoveRange(8, _courseOpportunities.Count - 8);
        }
    }

    private void CloseDossier()
    {
        // Closing the book stands down both selection layers it can be showing.
        if (_interestTargetId is not null)
        {
            SetInterestTarget(_interestTargetId); // toggle off
        }

        if (_selectedTargetId is not null)
        {
            SelectTarget(_selectedTargetId); // toggle off
        }

        StateHasChanged();
    }

    /// <summary>
    /// #997 wave 10 · <c>/map?target=&lt;contact-id&gt;</c> — THE DOSSIER, REACHABLE FROM A URL AT LAST.
    ///
    /// <para>#960's card is gated on a tactical target, and the two roads to one are a contact in sensor
    /// reach or a collector bought by a robbery. Neither is a field a URL could set, so three waves of the
    /// shell migration measured this card by hand and each said so out loud rather than dressing the
    /// measurement up as a playthrough. This is the road they were missing, and it is the same shape
    /// <c>?reveal=</c> has: the sky already holds her when you arrive.</para>
    ///
    /// <para><b>BOTH ROSTERS ARE WALKED, and that is not tidiness.</b> A hunter is never in
    /// <c>_npcStates</c>, and #962 is the issue that got filed when 📡 <i>sharpen fix</i> resolved its
    /// subject through <c>FindNpc</c> alone: every collector id fell out of the first guard and the button
    /// did nothing, in silence. <see cref="DossierFor"/> reads both; so does this.</para>
    ///
    /// <para><b>What it pays for, rather than pretends.</b> Traffic has to have been SEEN before the dossier
    /// has anything honest to draw — <c>DossierFor</c> returns null on a contact with no observation, and
    /// that refusal is right. So the cheat enters the fix a completed telescope pass would have entered:
    /// her own state, at this instant, through the same <see cref="Observation"/> the sweep and the ledger
    /// take. The next sweep re-decides whether she is still live, exactly as it does for every other
    /// contact — nothing here is pinned true.</para>
    /// </summary>
    private void SeedTargetCheat(string asked)
    {
        if (_ephemeris is null)
        {
            return;
        }

        string id = asked;
        if (string.Equals(asked, "collector", StringComparison.Ordinal))
        {
            // The one contact no scenario ships with: hired muscle. Sent down the SHIPPING road — the same
            // SpawnHunterForHeatEvent a robbery calls, fitting out at the nearest policed body, with its own
            // news wire entry — because a hand-planted hunter would be a dossier about a ship the sim has
            // never heard of. A pure outer-reaches berth has nobody to send, and says so (#212's idiom).
            int before = _hunters.Count;
            SpawnHunterForHeatEvent();
            if (_hunters.Count == before)
            {
                ShowPulseMessage(
                    "🧪 DEV ?target=collector: nothing policed within reach of here to send muscle — there is "
                    + "no cavalry to call. Try &dock=selene-gate, or name a contact id instead.");
                return;
            }

            id = _hunters[^1].Id;
        }

        if (FindNpc(id) is { } contact)
        {
            if (!SheIsOnHerRouteByNow(contact) || contact.Arrived)
            {
                ShowPulseMessage(
                    $"🧪 DEV ?target={asked}: {contact.Ship.Callsign} is not in the sky at this hour — she has "
                    + "either not sailed yet or is already alongside. Add &simhours=N to move the clock.");
                return;
            }

            var fix = new Observation(id, SimTime, contact.State.Position, contact.State.Velocity);
            contact.LastObservation = fix;
            contact.ObservationCount++;
            contact.CurrentlyObserved = true;
            _trackingPost?.ApplyObservation(fix);
        }
        else if (!_hunters.Any(hunter => hunter.Id == id))
        {
            string outThere = string.Join(" · ", _npcStates.Take(6).Select(n => n.Ship.Id)
                .Concat(_hunters.Select(h => h.Id)));
            ShowPulseMessage(
                $"🧪 DEV ?target={asked}: nothing out there answers to that id. This sky holds {outThere}"
                + " — or use ?target=collector to have muscle sent after you.");
            return;
        }

        // InterestFromMenu's own guard, and for its reason: SetInterestTarget is a TOGGLE, so calling it on
        // a target that is already the interest would stand the dossier DOWN.
        if (_interestTargetId != id)
        {
            SetInterestTarget(id);
        }

        // A tucked dossier is not an open one, and this cheat's whole promise is that the card is up.
        _dossierMinimized = false;

        // …and the card is drawn on the Nav and Sensors desks only, so a cheat that pointed the tactical UI
        // at her and left the captain in a station corridor would be a cheat that did nothing. Same idiom as
        // ?ashore=1 walking the walk — and skipped for a captain who is off-ship or on his way down a
        // gravity well, because SwitchDesk rightly refuses that and would answer with its own refusal line.
        if (!_landCheat && _surface is null && _activeDesk is not (ShipDesk.Nav or ShipDesk.Sensors))
        {
            SwitchDesk(ShipDesk.Nav);
        }

        ShowPulseMessage($"🧪 Test: 📖 {ContactCallsign(id)} is the tactical target — her dossier is on the "
                         + "glass, bottom-centre. – tucks it into a tile, ✕ drops the target.");

        // #997 wave 10 · THE ONE THING THE OFF-BROWSER BENCH CANNOT SEE, said out loud where a playtester
        // will read it. `?target=collector&dock=<berth>` boots a dossier that is GONE a tick later, and
        // that is the game being right rather than the cheat being wrong: a haven is precisely where a
        // collector loses the scent (#580 / EncounterRule.ApplyBreakOff), so she breaks off, leaves
        // `_hunters`, and DossierFor has nothing to draw. It cost a browser walk to find, because the
        // bench runs no sim ticks at all — so the warning is the line the captain is left holding.
        if (_dockedHavenId is not null && _hunters.Any(hunter => hunter.Id == id))
        {
            ShowPulseMessage(
                $"🧪 DEV ?target={asked}: her file is up, and it will not stay. You are berthed at a HAVEN, "
                + "which is exactly where a collector loses the scent — she breaks off within a tick or two "
                + "and the dossier goes with her. Cast off, or boot free-flying: /map?start=wreck&target=collector");
        }
    }

    public readonly record struct DossierInfo(
        string Name, string Detail, string StatusLine,
        double Distance, double RelSpeed, double Closing,
        double? TrackQuality, bool InDriverReach, string FireLine,
        bool IsPrey, int BoardReady,
        // #962: is the telescope's own task list carrying a pass on her right now? The card's "not on the
        // telescope ledger" line is otherwise the same sentence before and after the captain presses
        // 📡 sharpen fix, which is what "It says we are not tracking the debt collector and we should ...
        // but really HOW??????" was written under.
        bool ScopeOrdered = false,
        // #962: a hunter's card carries the terms of her contract — who bought it and what ends it, with
        // the clocks running. Null on traffic, which has no contract to state.
        HuntTerms? Terms = null,
        // #534: what the instruments measure of her, beside what her own papers imply. Every hull in the
        // sky carries it and on almost every one the two columns agree — see Map.Npc.QShip. Null on a
        // hunter, who is not pretending to be anything.
        HullReading? Reading = null,
        // #534 tell (e): what she said back the last time the captain keyed the tight-beam at her. Null
        // until he actually has — the fifth tell is something he DID, not something the glass hands him,
        // and an unhailed hull's file is the same file whatever she is.
        string? HailAnswer = null);

    /// <summary>What the collector's own card says about how this ends. Every sentence is built in Core,
    /// beside the rule it describes, so a card and a sim can never quote different numbers at each other
    /// (#962, and this repo's fifth named bug class).</summary>
    public readonly record struct HuntTerms(string Warrant, string Hiding, string Nerve, string Sail);

    private DossierInfo? DossierFor(string id)
    {
        Vector2d position, velocity;
        string name, detail;
        string statusLine;
        bool isPrey = false;
        HuntTerms? terms = null;
        NpcShip? hull = null;
        if (FindNpc(id) is { Active: true, Arrived: false } npc)
        {
            isPrey = true;
            hull = npc.Ship;
            name = npc.Ship.Callsign;
            detail = npc.Ship.IsPod ? "· cargo pod" : $"· {npc.Ship.CargoClass} ({npc.Ship.CargoUnits}u)";
            if (npc.CurrentlyObserved)
            {
                position = npc.State.Position;
                velocity = npc.State.Velocity;
                statusLine = npc.Disabled ? "⚠ adrift — sail holed, easy prey" : "👁 live sensor contact";
            }
            else if (npc.LastObservation is { } lastObs)
            {
                position = lastObs.Position;
                velocity = lastObs.Velocity;
                statusLine = $"👻 last seen {FormatDuration(Math.Max(0, SimTime - lastObs.SimTime))} ago — dead-reckoned";
            }
            else
            {
                return null; // never observed: no honest dossier to show
            }
        }
        else
        {
            HunterState? found = null;
            foreach (HunterState hunter in _hunters)
            {
                if (hunter.Id == id) { found = hunter; break; }
            }

            if (found is not { } h)
            {
                return null;
            }

            (name, detail) = (h.Callsign, "· hired muscle 🐺");
            position = h.State.Position;
            velocity = h.State.Velocity;
            statusLine = h.BrokenOff ? "broke off the hunt" : "⚠ hunting US";
            terms = TermsOfTheHunt(h);
        }

        double distance = (position - _ship.Position).Length;
        double relSpeed = (velocity - _ship.Velocity).Length;
        // #210: one voice — the signed range-rate through the shared Core helper.
        double closing = RelativeMotion.ClosingSpeed(_ship.Position, _ship.Velocity, position, velocity);

        double? quality = null;
        if (_trackingPost is not null && _trackingPost.TryGetTrack(id, out TrackedTarget track))
        {
            quality = track.EffectiveQuality(SimTime);
        }

        bool scopeOrdered = false;
        if (_trackingPost is not null)
        {
            foreach (SensorTask queued in _trackingPost.TaskQueue)
            {
                if (queued.TargetShipId == id)
                {
                    scopeOrdered = true;
                    break;
                }
            }
        }

        // #962 · ONE REACH, AND IT IS THE ONE THE SIM ENFORCES. This line used to quote muzzle × one day
        // (691,200 km) while EncounterRule.InWeaponRange gated every warning shot at 200,000 km — a card
        // and a sim disagreeing by three and a half times, on the card the owner had open while he asked
        // what was happening. Both read EncounterRule.WeaponRangeMeters now, and #961's faster driver is
        // what makes that one number cover her catch envelope at last.
        double reach = EncounterRule.WeaponRangeMeters;
        bool inReach = distance <= reach;
        string fireLine = inReach
            ? $"🎖 inside the driver's reach ({FormatDistance(reach)}) — a firing solution is on the table (war room)"
            : $"driver reach ≈ {FormatDistance(reach)} — close {FormatDistance(distance - reach)} more for a firing solution";

        // The HONEST autosteal criteria (owner: "the box should say the requirements — close enough
        // but too much speed difference"). The encounter-window clock above is DISTANCE-only and
        // coast-assumed; the real CaptureRule.IsInWindow needs BOTH within 5e8 m AND under 5 km/s
        // relative. BoardReady: 2 = both met (boardable), 1 = in range but too fast, 0 = out of range.
        // The popup renders the two checks explicitly from Distance/RelSpeed so nothing is implied.
        int boardReady = 0;
        if (isPrey)
        {
            bool within = distance <= CaptureRule.CaptureRadiusMeters;
            bool slow = relSpeed <= CaptureRule.MaxRelativeSpeed;
            boardReady = within && slow ? 2 : within ? 1 : 0;
        }

        // #534 · the same block every hull carries: what the instruments measure, beside what her papers
        // imply. The radiator count and the comms fit are things the GLASS resolves, so they arrive with a
        // held fix; the burn is read off her observed motion and needs only a contact.
        HullReading? reading = hull is { } ship ? ReadingOf(ship, quality is not null) : null;

        // #534 tell (e) · and the fifth one is hers to say. It arrives only when the captain has keyed the
        // tight-beam at her (Map.Alerts.CommsHail), the same way the two glass rows arrive only with a held
        // fix: a tell may need a completed pass, and a hail is a pass the captain flies himself.
        string? said = _hailAnswers.TryGetValue(id, out string? heard) ? heard : null;

        return new DossierInfo(name, detail, statusLine, distance, relSpeed, closing, quality, inReach, fireLine, isPrey, boardReady, scopeOrdered, terms, reading, said);
    }

    /// <summary>
    /// #962 · WHAT ENDS THIS HUNT, WITH THE CLOCKS RUNNING. Owner, docked at a haven with the heat gauge
    /// reading zero and a collector still inbound: <i>"So we have zero heat and are docked at haven … why is
    /// this still hunting us?"</i>
    ///
    /// <para>The rules that answer him were all in EncounterRule already, and not one of them was ever said
    /// to his face. The contract is bought ONCE — by a robbery, over a named hull — and it does not care what
    /// the heat gauge does afterwards; that is the whole answer to "zero heat, why?". Hiding two unbroken
    /// days at a haven is what makes her lose the scent, and his clock WAS running, he just could not see it.
    /// Warning shots erode her nerve until she voids the contract. A holed sail ends it outright.</para>
    ///
    /// <para>Every sentence is composed in Core beside the rule it describes, so the card cannot drift from
    /// the sim. All this method does is hand Core the two live numbers only the page can know: whether the
    /// haven clock is running, and how long it has run.</para>
    /// </summary>
    private HuntTerms TermsOfTheHunt(HunterState hunter)
    {
        bool hidingNow = !double.IsNaN(_hiddenAtHavenSinceSimTime);
        double hiddenFor = hidingNow ? Math.Max(0, SimTime - _hiddenAtHavenSinceSimTime) : 0;
        return new HuntTerms(
            EncounterRule.WarrantLine(hunter),
            EncounterRule.HidingTerm(hiddenFor, hidingNow),
            EncounterRule.NerveTerm(hunter, _heat.Level),
            EncounterRule.SailTerm);
    }

    // ---- M5: traffic, sensors, prediction ----

    // Keep every active NPC in lockstep with the player: after the player's stepping loop, catch
    // each NPC up to the player's SimTime. NPCs use Core's fixed NpcTimeStep (60 s), not the
    // player's dt=1 s — 8 NPCs × 10000 fine steps per frame at max warp brought interpreted WASM
    // to ~1 fps, and NPC accuracy needs are meters-scale at dt=60. The ≤59 s overshoot past the
    // player's SimTime is subpixel at map zoom. Also heals a mid-frame activation (the ship
    // starts from InitialState and immediately catches up).
    /// <summary>
    /// #997 wave 10 · A SCHEDULED CONTACT IS NOT IN THE SKY UNTIL HER DEPARTURE IS BEHIND US, and this is
    /// the two lines <see cref="StepNpcs"/> has always used to put her there — lifted rather than copied,
    /// because <c>?target=</c> needs the same answer at boot and a second spelling of it is the fourth
    /// named bug class waiting to happen.
    ///
    /// <para>True when she is flying now (already active, or activated by this call); false when her
    /// departure is still in the future, which is the answer that makes the caller skip her.</para>
    /// </summary>
    private bool SheIsOnHerRouteByNow(NpcState npc)
    {
        if (npc.Active)
        {
            return true;
        }

        if (_ship.SimTime < npc.Ship.ActivationTime)
        {
            return false;
        }

        npc.Active = true;
        npc.State = npc.Ship.InitialState;
        return true;
    }
}
