namespace SpaceSails.Core;

/// <summary>
/// #538 · RIG FOR SILENT RUNNING. Owner, building the scene where a black-ops team sweeps a hull the captain is
/// hiding inside: <i>"we take our guns and hide to let them pass"</i>, then <i>"the remote to sentries should be
/// in the mobile hud not at captains desk"</i>, then <i>"Maybe small button to open the sentry settings pop-up 😎.
/// Shuttle should also power down to make it less of an anomaly."</i>
///
/// <para>That last sentence is the one that makes this a system rather than a switch. Hiding is not a state of
/// the captain — it is a state of <b>everything the captain brought with them</b>. A folded-up pirate behind a
/// bulkhead is perfectly concealed and perfectly betrayed by a lit shuttle clamped to the lock with its
/// transponder answering and a gun tracking the hatch.</para>
///
/// <para>And it is the same law Lab 43 found for the hull: <b>you are given away by what you EMIT.</b> The lab
/// killed the idea that a charged hull is *seen* — she is heard. This is that finding applied to a boarding: the
/// boat's lights, its transponder, its warm reactor and its automation are all emissions, and an inspection team
/// notices an anomaly long before it notices a man.</para>
///
/// <para><b>Both switches cost something real</b>, which is what keeps this from being a free "win stealth"
/// button: tight guns will not defend you (<see cref="SentryBot.TightIsAlsoUndefendedLine"/>), and a cold boat
/// is not a ride until it has warmed up. Hiding well means being slow to leave, and that is exactly the trade a
/// captain should be making with somebody else's team aboard.</para>
/// </summary>
public static class SilentRunning
{
    /// <summary>
    /// How long a powered-down shuttle takes to be a ride again. Long enough that going dark is a commitment and
    /// running for the boat is a bad plan; short enough that it is not a death sentence when the plan fails.
    /// </summary>
    /// <remarks>FLAGGED for the owner's tuning. Twenty-five seconds is about the walk from amidships to the lock,
    /// which is the honest shape: if you have to run, you arrive at a boat that is still waking up.</remarks>
    public const double SpinUpSeconds = 25.0;

    /// <summary>
    /// How long she takes to go properly cold. Owner: <i>"A timer on the map about the cool down and warm up"</i>
    /// — and asking for the cool-down to be a clock too is the sharper half of the request, because it means
    /// <b>hiding cannot be done at the last second</b>. A captain who hears the boarding tube mate and only then
    /// thumbs the remote has a boat that is still bright, still answering, and still an anomaly.
    /// </summary>
    /// <remarks>Deliberately shorter than <see cref="SpinUpSeconds"/>: dumping a bus is easier than bringing one
    /// up. That asymmetry is what makes panic-hiding <i>almost</i> viable and panic-leaving never.</remarks>
    public const double SpinDownSeconds = 12.0;

    /// <summary>Where the boat is between cold and flying. Four states, because the two transitions are as
    /// interesting as the two ends — which is the whole point of putting clocks on them.</summary>
    public enum BoatState
    {
        /// <summary>Lit, answering, and a ride.</summary>
        Warm,

        /// <summary>Shutting down and not yet quiet. Still an anomaly, and no longer a ride.</summary>
        CoolingDown,

        /// <summary>Dark, silent, hatch dogged. Nothing to find, and nowhere to go.</summary>
        Cold,

        /// <summary>Coming up. The clock a captain watches with something walking towards them.</summary>
        WarmingUp,
    }

    /// <summary>
    /// THE DIAL, and the reason it is a dial rather than two timers: <b>changing your mind keeps your
    /// progress.</b> A captain who orders her down, thinks better of it four seconds later and reverses gets a
    /// boat that is four seconds of cold away from flying — not a fresh twenty-five. Two independent countdowns
    /// would have punished the change of mind, which is exactly the decision this system wants people making.
    /// </summary>
    /// <param name="warmth">1 = warm and flying, 0 = properly cold.</param>
    public static double AdvanceWarmth(double warmth, bool orderedCold, double dtSeconds)
    {
        double per = orderedCold ? -1.0 / SpinDownSeconds : 1.0 / SpinUpSeconds;
        double next = System.Math.Clamp(warmth + (per * System.Math.Max(0, dtSeconds)), 0, 1);

        // SNAP THE LAST HAIR. A dial ticked in fractions arrives at 1e-17 rather than at zero, and 1e-17 is not
        // "cold" — it is a boat that reads COOLING for ever and a hatch that never opens because of rounding. The
        // clock the player is reading says 0:00 long before the double does, so the double has to agree.
        const double hair = 1e-9;
        return next <= hair ? 0 : next >= 1 - hair ? 1 : next;
    }

    /// <summary>Which of the four she is in, from the dial and the standing order.</summary>
    public static BoatState StateOf(double warmth, bool orderedCold) => orderedCold
        ? (warmth <= 0 ? BoatState.Cold : BoatState.CoolingDown)
        : (warmth >= 1 ? BoatState.Warm : BoatState.WarmingUp);

    /// <summary>How long until the standing order is actually achieved — the number that goes on the map.</summary>
    public static double SecondsLeft(double warmth, bool orderedCold) => orderedCold
        ? System.Math.Max(0, warmth) * SpinDownSeconds
        : System.Math.Max(0, 1 - warmth) * SpinUpSeconds;

    /// <summary>Whether the shuttle will actually take the captain anywhere right now.</summary>
    public static bool ReadyToFly(double warmth, bool orderedCold) =>
        StateOf(warmth, orderedCold) == BoatState.Warm;

    /// <summary>
    /// Whether her hatch stands open. Owner: <i>"its doors open once the warm up is complete"</i> — so the hatch
    /// is not a door the captain opens, it is <b>the boat announcing she is ready</b>. Cold, her actuators have no
    /// bus and an open hatch would be one more thing to notice; warm, she opens up and you can get in.
    ///
    /// <para>Which is why the warm-up clock is frightening rather than merely inconvenient: the captain is not
    /// waiting to <i>depart</i>, they are waiting to be <i>let in</i>, in the open, at the lock.</para>
    /// </summary>
    public static bool HatchOpen(BoatState state) => state == BoatState.Warm;

    /// <summary>Whether she is actually hiding you. A boat halfway through her shutdown is still lit enough to
    /// find — hence the cool-down clock, and hence deciding early.</summary>
    public static bool IsQuiet(BoatState state) => state == BoatState.Cold;

    // ── What the map says while the clocks run ────────────────────────────────────────────────────────

    /// <summary>The clock strip's tag — short, because it sits beside the overload and the pumps and must not
    /// out-shout them.</summary>
    public static string ClockTag(BoatState state) => state switch
    {
        BoatState.WarmingUp => "🛸 WARMING",
        BoatState.CoolingDown => "🛸 COOLING",
        BoatState.Cold => "🛸 COLD",
        _ => "🛸 WARM",
    };

    /// <summary>…and what it says about her, in the same column the pumps use for a compartment name.</summary>
    public static string ClockNote(BoatState state) => state switch
    {
        BoatState.WarmingUp => "hatch opens when she is up",
        BoatState.CoolingDown => "still bright, no longer a ride",
        BoatState.Cold => "hatch dogged",
        _ => "hatch open",
    };

    /// <summary>What the panel says she is doing. One line, and it always names the consequence rather than the
    /// state, because a state is not a decision.</summary>
    public static string SpinUpLabel(double warmth, bool orderedCold)
    {
        BoatState state = StateOf(warmth, orderedCold);
        double left = SecondsLeft(warmth, orderedCold);
        return state switch
        {
            BoatState.Warm => "🛸 Boat warm, hatch open, answering. She is a ride and she is an anomaly.",
            BoatState.Cold => "🛸 Boat cold: dark, silent, hatch dogged. Nothing to find — and no way off.",
            BoatState.CoolingDown =>
                $"🛸 Going down — {HullVenting.SoakLabel(left)} before she is quiet. She is neither hidden nor a " +
                "ride until then.",
            _ => $"🛸 Coming up — {HullVenting.SoakLabel(left)}. The hatch opens when she does.",
        };
    }

    // ── What each switch is for, in the panel's own voice ──────────────────────────────────────────────

    /// <summary>
    /// The heading on the pop-up. The owner named it himself — <i>"Captain's remote"</i> — and the name is the
    /// design: this is a handheld thing a captain carries and thumbs while crouched behind a bulkhead, not a
    /// settings menu on a console somewhere else. What it commands is everything of theirs that is still making
    /// decisions without them.
    /// </summary>
    public const string PanelTitle = "📻 CAPTAIN'S REMOTE — RIG FOR SILENT RUNNING";

    /// <summary>Said once at the top, because the panel exists to make one point: concealment is about the whole
    /// away team, not about where the captain is standing.</summary>
    public const string PanelBlurb =
        "Hiding is not something you are; it is something everything you brought is doing. A folded-up captain " +
        "behind a bulkhead is given away by a lit boat, an answering transponder and a gun that tracks the hatch.";

    /// <summary>Ordering her down — which is now an order rather than an event, because it takes time.</summary>
    public const string BoatDarkLine =
        "🛸 Rigging her down: lamps out, transponder silent, the tube gun stood down and the reactor idling towards " +
        "nothing anybody sweeps for. She will not be quiet for a little while yet — and she stops being a ride the " +
        "moment you say it.";

    /// <summary>…and coming back up. Started from the remote now, wherever the captain happens to be crouched.
    /// Owner: <i>"Shuttle warm up should work from remote."</i></summary>
    public const string BoatWakingLine =
        "🛸 Bringing her up from here. Lamps, bus, transponder, hatch — in that order, and none of it is quiet.";

    /// <summary>What the remote says the button will do, given where she is on the dial.</summary>
    public static string BoatButtonLabel(BoatState state) => state switch
    {
        BoatState.Warm => "🛸 Rig the boat down",
        BoatState.Cold => "🛸 BOAT COLD — warm her up",
        BoatState.CoolingDown => "🛸 GOING DOWN — press to bring her back",
        _ => "🛸 COMING UP — press to abort",
    };

    /// <summary>The refusal, when a captain runs for a boat they told to sleep. Not a punishment: the cost of the
    /// thing that hid them — and it names the hatch, because that is the part you can see from the lock.</summary>
    public static string NotARideYetLine(double secondsLeft) =>
        $"🛸 Her hatch is still dogged — {HullVenting.SoakLabel(secondsLeft)} before she opens up. This is what " +
        "going dark bought you, and it is being charged now.";

    /// <summary>
    /// What a captain finds when they bring belts to a sleeping boat. Owner, listing what makes the wait bite:
    /// <i>"As the ammo count runs down and reload place is warming up to allow use and its gun"</i> — so going
    /// dark does not only take away the ride, it takes away the RESUPPLY and the covering gun. Everything a
    /// captain leans on lives in the boat, and the boat is shut.
    /// </summary>
    public static string ReloadNeedsHerAwakeLine(double secondsLeft) =>
        $"📦 The belts are aboard her and her hatch is dogged — {HullVenting.SoakLabel(secondsLeft)}. What you " +
        "hid is also what you were counting on.";

    /// <summary>
    /// The sentence for the shape the owner sketched: <i>"Or say self destruct tics down but shuttle still warms
    /// up 🤭"</i>. Two clocks in the same strip, one of them hers and one of them the hull's, and no way to hurry
    /// either. Said once, when both are running, because it is the moment the scene is about.
    /// </summary>
    public const string TwoClocksLine =
        "⏳ Two clocks now: hers coming up, the keel counting down. Neither of them takes requests.";

    /// <summary>And what the wait actually feels like, which the owner put better than any UI could: <i>"Under a
    /// swarm of reevers with slinged autoguns that wait can feel really long time 😎"</i>.</summary>
    public const string LongWaitLine =
        "🛸 Twenty-five seconds is nothing. It is a great deal of something with company in the corridor.";

    /// <summary>The honest summary a captain deserves before they commit to it.</summary>
    public const string WhatItCostsLine =
        "Tight guns will not defend you. A cold boat will not fly you, arm you or open up for you. Everything " +
        "that makes you hard to find makes you slow to leave.";
}
