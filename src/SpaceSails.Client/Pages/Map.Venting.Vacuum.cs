using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Venting — what the vacuum is doing while the away team works: the clocks a blown room keeps, the nest that goes on producing until somebody cuts it, what a finished room leaves for whoever walks in, and the exposure that makes an open compartment a weapon.
public sealed partial class Map
{
    /// <summary>Run the vacuum clocks. Owner: <i>"there might be a counter on how long the room has been in
    /// vacuum … so it needs certain time for certain infestations."</i> A vented compartment counts up for
    /// as long as the away team is aboard, which is what turns venting from a button into a decision about
    /// WHEN — blow the hold, go and read the log, come back to a room that has been open four minutes.</summary>
    private void AdvanceVacuumClocks(double dtSeconds)
    {
        if (_wreck is null || _ventSpaces.Count == 0)
        {
            return;
        }

        // THE CORRIDOR KEEPS TIME TOO. Owner: "the spine should also show the vacuum time etc." It was the
        // one volume on the board with no instruments — a bare true/false — even though it is the volume the
        // captain spends the most time standing in and the only one they cannot shut a door against.
        if (!_spinePressurised)
        {
            _spineVacuumSeconds += dtSeconds;
        }

        foreach (string name in _ventSpaces.Keys.ToList())
        {
            HullVenting.Space s = _ventSpaces[name];
            if (!s.Vented)
            {
                continue;
            }

            bool wasDone = HullVenting.SoakComplete(s);
            s = s with { VacuumSeconds = s.VacuumSeconds + dtSeconds };
            _ventSpaces[name] = s;

            // The edge where the vacuum finishes the job — the moment the handle used to deliver.
            if (!wasDone && HullVenting.SoakComplete(s) && s.Infested)
            {
                _ventSpaces[name] = s with { Infested = false };
                ClearReeversIn(name);
                BoardLog($"💨 {name} has been open to space long enough. Whatever was in there is finished.");

                // WHAT YOU GOT FOR THE WAIT — SHOWN WHEN YOU LOOK, NEVER BEFORE. Owner: "that should be
                // told with GEN AI images … what did we get from venting", and then immediately: "I got
                // the what the vacuum left even though I had not yet gone to the room."
                //
                // Quite right, and it broke the rule the rest of this hull now runs on: THE MAP IS YOUR
                // EYES. A card firing off a clock hands the captain a photograph of a compartment they have
                // never entered — the same sin as drawing contacts through bulkheads, dressed as a reward.
                // So the soak only ever leaves the room WORTH LOOKING AT; the looking is still the
                // captain's to do, at the station, on their own feet.
                _ventPayoff.Add(name);
            }
        }
    }

    /// <summary>
    /// #488 · THE NEST IS A SOURCE. Owner, after the pack walked out into the corridor: <i>"kind of a pity
    /// there was nothing left to vent in the reactor room. How is that logical, are we now venting an empty
    /// room or could there be a reever spawn point there :-D … all the reevers came out to the spine, none
    /// left to vent."</i>
    ///
    /// <para>He is right that it read as pointless, and the honest fix is the one he proposes. A nest that
    /// has kept something alive aboard a dead ship for forty years is not a decoration the pack happens to
    /// stand near — it is where they come FROM. So while it is intact it keeps producing, slowly, and
    /// venting the room is not "killing an empty compartment": it is cutting the supply.</para>
    ///
    /// <para>That is also what finally makes the vacuum soak worth the wait rather than a chore. Clear the
    /// corridor and you have bought minutes. Vent the nest and you have bought the ship.</para>
    /// </summary>
    private void AdvanceNests(double dtSeconds)
    {
        if (_wreck is not { Cause: Derelict.WreckCause.Infested } || _ventSpaces.Count == 0)
        {
            return;
        }

        // ONE NEST. Owner, watching them come out of half the ship: "I thought there was only one nest?"
        // There is — the deep hold, where her station stands and where every line of text has always said
        // it was. Every aft compartment being flagged Infested is a statement about what LIVES in them and
        // how long the vacuum needs to finish it; it was never meant to mean each one produces more.
        string nest = WreckLayout.NestCompartment;
        if (!_ventSpaces.TryGetValue(nest, out HullVenting.Space s) || !s.Infested || s.Vented)
        {
            return;   // vented, or this hull never had one: the supply is cut
        }

        // NOTHING IS EVER BORN IN THE ROOM THE CAPTAIN IS STANDING IN. Owner, ringed by six of them at the
        // valve board with a dogged hatch and a loaded sentry: "I am surrounded by reevers though I was in
        // a closed space?" Nothing came through anything — they were BORN there. A nest is a place you can
        // walk away from, or vent, or decide to live with. It is not an ambush that materialises inside
        // your personal space. It holds while you are in the room.
        if (CaptainCompartment() == nest)
        {
            return;
        }

        _nestClocks.TryGetValue(nest, out double t);
        t += dtSeconds;
        if (t < NestBroodSeconds)
        {
            _nestClocks[nest] = t;
            return;
        }
        _nestClocks[nest] = 0;

        // The pacing law this hull is tuned to (owner: "they come one or two at a time"). A nest is a slow
        // drip and never a wave — and it stops entirely at the ceiling, so leaving one alive costs you a
        // steady trickle rather than an unwinnable ship.
        if (_reevers.Count >= NestPackCeiling)
        {
            return;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(Derelict.WreckCause.Infested);

        _reevers.Add(new Reever
        {
            // Out of the nest itself, not out of thin air in the middle of the room.
            X = at.X + 1.2,
            Y = at.Y,
            Facing = 0,
            JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)_reevers.Count + 31UL,
            // It comes out of the nest knowing nothing — same rule as the sleepers. It has to find you.
            EverSeen = false,
            VisibleOnMap = false,
        });

        BoardLog($"🕷 Something else pulls itself out of the nest in the {nest}.");
    }

    /// <summary>How long an intact nest takes to put another one on its feet. Slow on purpose: the drip has
    /// to be survivable, and the point of it is to make VENTING urgent, not to overrun the captain.
    /// FLAGGED for the owner's tuning.</summary>
    private const double NestBroodSeconds = 55.0;

    /// <summary>The most a nest will ever have walking at once. Past this it holds — a hull you refuse to
    /// vent gets steadily worse, never impossible.</summary>
    private const int NestPackCeiling = 9;

    /// <summary>Per-compartment brood timers.</summary>
    private readonly Dictionary<string, double> _nestClocks = [];

    /// <summary>Rooms the vacuum has finished with, waiting for the captain to walk in and see it. The
    /// payoff is EARNED BY LOOKING — the soak leaves something worth looking at, and nothing more.</summary>
    private readonly HashSet<string> _ventPayoff = new(System.StringComparer.Ordinal);

    /// <summary>Whether this compartment is holding a "what the vacuum left" card for whenever the captain
    /// gets there.</summary>
    private bool HasVentPayoff(string name) => _ventPayoff.Contains(name);

    /// <summary>Has this away team already been shown the nest as it stands? Once per boarding — the room
    /// is a shock the first time and scenery after that, and a card that re-fired every time the captain
    /// crossed the compartment would turn the loudest room on the hull into a nuisance.</summary>
    private bool _liveNestSeen;

    /// <summary>Called as the captain walks. Two cards live here, and they are the same story twice:
    ///
    /// <para><b>Before</b> — the first time they stand in the nest while it is still alive and the room
    /// still holds air, it shows them what grew in there. This did not exist until #528, and its painting
    /// did: <c>vented-nest-intact.jpg</c> shipped with nothing pointing at it. Which meant the setup for
    /// this hull's best payoff was missing while the picture of it sat on disk. #380's law — the fiction
    /// arrives one beat early — and "what the vacuum left" only lands because you saw what was there.</para>
    ///
    /// <para><b>After</b> — the first time they set foot in a room the vacuum has finished with, the room
    /// shows them what it left. Not a clock, not a notification: you went and looked.</para>
    ///
    /// <para>Both texts come from Core (<see cref="NestPlates"/>); the after-card's copy used to be typed
    /// here.</para></summary>
    private void CheckVentPayoffUnderfoot()
    {
        if (_wreckLook is not null || CaptainCompartment() is not { } here)
        {
            return;
        }

        // The after-card wins if the room has one waiting: a vented nest is not a live one, and the payoff
        // is what the captain came back for.
        if (_ventPayoff.Remove(here))
        {
            _wreckLook = new WreckLook(
                NestPlates.DeadTitle(here), NestPlates.Dead.ArtFile, NestPlates.Dead.Caption);
            RendererInterop.PlayCue("reveal");
            return;
        }

        // The before-card. Guarded on the SIM's own state, not on a room name: it shows only while the
        // space is genuinely infested and genuinely unvented, so a hull whose nest was already cleared
        // never claims there is one, and the card can never contradict what the compartment is doing.
        if (_liveNestSeen
            || !string.Equals(here, WreckLayout.NestCompartment, StringComparison.Ordinal)
            || !_ventSpaces.TryGetValue(here, out HullVenting.Space space)
            || !space.Infested
            || space.Vented)
        {
            return;
        }

        _liveNestSeen = true;
        _wreckLook = new WreckLook(NestPlates.Live.Title, NestPlates.Live.ArtFile, NestPlates.Live.Caption);
        RendererInterop.PlayCue("reveal");
    }

    /// <summary>
    /// #488 · VACUUM IS GROUND, NOT AN EVENT. A room that is open to space keeps being open to space, so
    /// anything standing in it keeps dying — including whatever wanders in ten minutes later.
    ///
    /// <para>The old rule killed only at the instant a compartment's soak completed, which left a hull full
    /// of hard vacuum that was mechanically scenery (owner: "I pumped the near hold to vacuum but there are
    /// still reevers in it?"). Exposure is per-contact now: a walker takes <see cref="HullVenting.Infestation.Motile"/>
    /// seconds of it — lungs are the fastest thing to lose — and the clock resets the moment it steps back
    /// into air.</para>
    ///
    /// <para>Which turns a vented compartment into a WEAPON you can leave lying around: blow a room, back
    /// through it, and the corridor behind you is lethal ground for anything that follows.</para>
    /// </summary>
    private void AdvanceVacuumExposure(double dtSeconds)
    {
        if (_wreck is null || _reevers.Count == 0)
        {
            return;
        }

        double lethal = HullVenting.SoakRequired(HullVenting.Infestation.Motile);

        for (int i = _reevers.Count - 1; i >= 0; i--)
        {
            Reever r = _reevers[i];

            // Which volume is it standing in? A compartment's own state, or the corridor's.
            bool inVacuum = RoomAt(r.X, r.Y) is { } room
                ? _ventSpaces.TryGetValue(room, out HullVenting.Space s) && s.Vented
                : !_spinePressurised;

            if (!inVacuum)
            {
                r.VacuumSeconds = 0;   // back in air: whatever it took, it is over
                continue;
            }

            r.VacuumSeconds += dtSeconds;

            // And it FAILS while it does it (owner: "yeah they really should slow down there :-D"). The
            // drag is read off the same exposure, so the vacuum is legible long before it is fatal: a thing
            // that charged into a blown compartment is visibly labouring halfway across it, and the captain
            // can watch the room doing the work. See VacuumDrag.
            if (r.VacuumSeconds >= lethal)
            {
                _reevers.RemoveAt(i);
                BoardLog("💨 One of them stopped moving in the vacuum.");
            }
        }
    }

    /// <summary>How much of its speed a contact still has, given how long it has been in vacuum. Full pace
    /// on the way in, down to a crawl as it runs out — so the room's work is something the captain watches
    /// happen rather than a body that abruptly stops.</summary>
    private static double VacuumDrag(Reever r)
    {
        double lethal = HullVenting.SoakRequired(HullVenting.Infestation.Motile);
        if (r.VacuumSeconds <= 0 || lethal <= 0)
        {
            return 1.0;
        }
        double spent = System.Math.Clamp(r.VacuumSeconds / lethal, 0, 1);
        return 1.0 - (spent * VacuumSlowdown);
    }

    /// <summary>How much of its pace the vacuum has taken by the time it kills. Not to zero — a thing that
    /// freezes solid before it drops reads as a bug rather than as suffocation. FLAGGED for tuning.</summary>
    private const double VacuumSlowdown = 0.72;

    /// <summary>Whether any room is still counting toward being certainly dead — the clock worth watching
    /// from the corridor. A hull that arrived vented decades ago is not "soaking"; it is finished.</summary>
    private bool AnyRoomSoaking
    {
        get
        {
            foreach (HullVenting.Space s in _ventSpaces.Values)
            {
                if (s.Vented && s.Infested && s.VacuumSeconds < YearsOfVacuumSeconds)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>The rooms whose vacuum clocks are worth showing while the captain is out in the corridor
    /// deciding whether the sentry can hold the lane long enough.</summary>
    private IEnumerable<(string Room, double Seconds)> RoomsSoaking =>
        _ventSpaces.Values
            .Where(s => s.Vented && s.Infested && s.VacuumSeconds < YearsOfVacuumSeconds)
            .OrderByDescending(s => s.VacuumSeconds)
            .Select(s => (s.Name, s.VacuumSeconds));
}
