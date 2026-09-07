namespace SpaceSails.Core;

/// <summary>
/// #488 · PUMPING DOWN — the thrifty way to empty a room, and the atmosphere that turns out to be
/// standing in more rooms than the one you pressed.
///
/// <para>Owner, from the lab bench: <i>"I used to pump vacuum chamber to be empty so that air can be
/// collected instead of venting, when things have power."</i> Pumping takes fifty seconds where the
/// valve takes none, and hands back a refill charge where the valve throws the air away.</para>
///
/// <para>And a pump does not empty a ROOM, it empties a VOLUME: <see cref="SharedAtmosphere"/> walks
/// the open doors out from where you pressed, so pumping a compartment whose hatch is standing open
/// takes the corridor and everything else on it with it. The line that says so is shown BEFORE the
/// pump starts, because that is the surface the captain is looking at.</para>
/// </summary>
public static partial class HullVenting
{
    // ── Pumping down: the thrifty way to empty a room ─────────────────────────────────────────────────

    /// <summary>
    /// PUMP IT DOWN INSTEAD OF BLOWING IT. Owner, from the lab bench: <i>"I used to pump vacuum chamber to
    /// be empty so that air can be collected instead of venting, when things have power."</i>
    ///
    /// <para>Which is the honest third button, and it turns the board's economy into a loop instead of a
    /// one-way drain. Blowing a compartment is instant, free and WASTEFUL — the air is gone into the dark
    /// and you will be buying it back off the shuttle a breath at a time. Pumping the same room down takes
    /// <see cref="PumpDownSeconds"/> and puts what was in it INTO THE RESERVE: one compartment out, one
    /// refill charge in.</para>
    ///
    /// <para>The cost is the clock, and on an infested hull the clock is the whole game — you are standing
    /// at a board listening to a pump for the best part of a minute while something roams a corridor you
    /// have to walk back down. Fast and wasteful, or slow and thrifty. That is a better decision than
    /// either button had on its own.</para>
    /// </summary>
    public const double PumpDownSeconds = 50.0;

    /// <summary>
    /// WHERE THE ROUGHING PUMP FINISHES AND THE LONG TAIL BEGINS. Owner, on how the real thing works: <i>"I
    /// think NASA uses cryopumps as high vacuums and some mechanical pumps for rough vacuum in that
    /// satellite simulation chamber of theirs, though the rough vacuum probably already does 95% of the job
    /// or more, so the rest is not that significant in material savings / pressure wise."</i>
    ///
    /// <para>That asymmetry is a mechanic for free, so it is modelled exactly as he describes it: the
    /// mechanical stage is quick and recovers essentially ALL of the air — the charge is banked here — and
    /// everything after it is the slow pull down to a pressure that will actually kill something, which
    /// returns nothing at all to the tanks.</para>
    ///
    /// <para>So the captain gets a real decision in the middle of a machine cycle: take the air and go, or
    /// stand there through the tail for the vacuum. Walking away at the rough mark is not a failure — it is
    /// the whole saving, and it costs you the kill.</para>
    /// </summary>
    public const double PumpRoughSeconds = 18.0;

    /// <summary>What a pumped-down compartment puts back in the reserve. One room, one breath — banked at
    /// the rough mark, not at the end, because that is where the air actually comes back.</summary>
    public const int PumpDownYieldsCharges = 1;

    /// <summary>How long this pump runs, start to finish. The corridor is a bigger volume and takes
    /// proportionally longer.</summary>
    public static double PumpTotalSeconds(bool spine) =>
        spine ? PumpDownSeconds * SpinePumpMultiplier : PumpDownSeconds;

    // ── One atmosphere, however many rooms it is standing in ──────────────────────────────────────────

    /// <summary>
    /// WHAT ACTUALLY SHARES AIR WITH WHAT. Owner, correcting a rule I had written per-door instead of
    /// per-volume: <i>"if we only want to evacuate it then the doors to it need to be sealed … but if we
    /// evacuate multiple spaces then doors between those do not need to be sealed. The only check we need
    /// is to make sure we don't evacuate a room by accident of leaving its door open."</i>
    ///
    /// <para>That is the whole interlock, correctly stated, and it is one rule instead of three. A pump does
    /// not empty a ROOM — it empties whatever volume it is plumbed into, and that volume is however many
    /// compartments are standing open to each other. So the question was never "is this door shut", it is
    /// "which spaces am I about to empty, and did the captain mean all of them".</para>
    ///
    /// <para>Every compartment opens onto the spine and onto nothing else, so the connected set is simple:
    /// a compartment with a dogged hatch is alone; anything with an open hatch is in one volume with the
    /// corridor and with every other open compartment. Naming it here, once, is what stops the board and the
    /// rules from disagreeing about what a pump is going to do.</para>
    /// </summary>
    /// <param name="name">Any space, compartment or <see cref="SpineName"/>.</param>
    /// <param name="spaces">Every compartment. The spine is not among them; it is implicit.</param>
    /// <remarks>A FLOOD FILL ACROSS OPEN DOORS, written as the search rather than as its answer. Owner: "so
    /// the UI of pumping should use like the A* algorithm to check the doors." Same family — A* is a search
    /// for the CHEAPEST path and this wants EVERY space reachable at all, so it is A* with the heuristic and
    /// the cost thrown away: a breadth-first flood. On this hull the graph happens to be a star (every
    /// compartment opens onto the corridor and onto nothing else) so the fill terminates in one hop and
    /// could have been written as two ifs — but writing the answer instead of the search is how a rule stops
    /// being true the day somebody cuts a hatch between two holds. This asks the doors.</remarks>
    /// <param name="corridorName">What the volume every hatch opens onto is CALLED on this ship. Defaults to
    /// a derelict's <see cref="SpineName"/>; the player's own ship calls hers
    /// <c>ShipLayout.SpineName</c> ("THE CORRIDOR").
    ///
    /// <para>It had to become a parameter the moment a second kind of ship used these rules. The door graph
    /// below compares against the spine's name to know which end of an edge it is looking at, so with the
    /// wreck's name baked in, asking about HER corridor returned a volume of one — the corridor, alone,
    /// connected to nothing — and every readout built on it was quietly wrong. A name is not a rule.</para></param>
    public static IReadOnlyList<string> SharedAtmosphere(
        string name, IReadOnlyList<Space> spaces, string? corridorName = null)
    {
        string corridor = corridorName ?? SpineName;

        var found = new HashSet<string>(System.StringComparer.Ordinal) { name };
        var queue = new Queue<string>();
        queue.Enqueue(name);

        while (queue.Count > 0)
        {
            string at = queue.Dequeue();
            foreach (string next in OpenNeighbours(at, spaces, corridor))
            {
                if (found.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        var volume = new List<string>(found);
        volume.Sort(System.StringComparer.Ordinal);
        return volume;
    }

    /// <summary>Everything one space is standing open to right now. THE DOOR GRAPH, and the only place it is
    /// written down: every compartment has one hatch and it opens onto the corridor — whatever the ship in
    /// question calls its corridor.</summary>
    private static IEnumerable<string> OpenNeighbours(
        string at, IReadOnlyList<Space> spaces, string corridor)
    {
        if (string.Equals(at, corridor, System.StringComparison.Ordinal))
        {
            foreach (Space s in spaces)
            {
                if (!s.DoorShut)
                {
                    yield return s.Name;
                }
            }
            yield break;
        }

        foreach (Space s in spaces)
        {
            // A dogged hatch is its own little world — that is the whole point of dogging it.
            if (string.Equals(s.Name, at, System.StringComparison.Ordinal) && !s.DoorShut)
            {
                yield return corridor;
            }
        }
    }

    /// <summary>How long it takes to pull a whole shared volume down, and what it pays back. Both scale with
    /// what is actually in it — three rooms and the corridor is a lot more air than one locker.</summary>
    public static (double Seconds, int Charges) PumpJob(IReadOnlyList<string> volume)
    {
        double seconds = 0;
        int charges = 0;
        foreach (string s in volume)
        {
            bool spine = string.Equals(s, SpineName, System.StringComparison.Ordinal);
            seconds += PumpTotalSeconds(spine);
            charges += PumpYield(spine);
        }
        return (seconds, charges);
    }

    /// <summary>The one warning the owner asked for, and the only one: you are about to empty more than the
    /// space you pressed. Not a refusal — a captain is allowed to evacuate half a ship on purpose — but it
    /// says exactly which rooms are going, because the accident it guards against is a hatch left open and
    /// forgotten, not a decision.</summary>
    public static string PumpReachesFurtherLine(string pressed, IReadOnlyList<string> volume)
    {
        var others = new List<string>();
        foreach (string s in volume)
        {
            if (!string.Equals(s, pressed, System.StringComparison.Ordinal))
            {
                others.Add(s);
            }
        }

        return others.Count == 0
            ? string.Empty
            : $"That pump is plumbed into more than {pressed}: {string.Join(", ", others)} " +
              $"{(others.Count == 1 ? "is" : "are")} standing open to it and will go down with it. Dog the " +
              "hatches you meant to keep.";
    }

    /// <summary>
    /// The countdown value at which the air lands in the tanks, for this pump.
    ///
    /// <para>PURE, AND TAKING THE ONLY THING IT DEPENDS ON. The client had this as one variable declared
    /// outside its per-pump loop and overwritten when the corridor came up in the enumeration — so every
    /// pump processed after the spine in the same frame was measured against the SPINE's mark, which its own
    /// shorter clock never reaches. Since the crossing is tested on exactly one frame, those compartments
    /// silently never banked: the room emptied, the pump finished, and nothing arrived in the tanks. A
    /// function cannot be left holding another pump's number.</para>
    /// </summary>
    public static double PumpRoughMark(bool spine) => PumpTotalSeconds(spine) - PumpRoughSeconds;

    /// <summary>What this pump pays into the reserve when it crosses its rough mark.</summary>
    public static int PumpYield(bool spine) => spine ? SpinePumpYieldsCharges : PumpDownYieldsCharges;

    /// <summary>The line at the rough mark — the air is home, the room is not yet lethal.</summary>
    public static string PumpRoughDoneLine(string name) =>
        $"The mechanical stage finishes and the note of the pump changes. {name} is down to a few percent " +
        "and effectively all of her air is in the tanks — that is the saving, and it is already yours. " +
        "What is left is the long pull to a pressure that kills, which recovers nothing. You can walk away " +
        "with the air, or stand here and wait for the vacuum.";

    /// <summary>
    /// Whether the pump can be run at all. It needs the same shut hatch a vent does — you cannot pump down a
    /// room that is open to the corridor, you would just be pumping the ship.
    ///
    /// <para>BUT NOT THE SAME INTERLOCK ON YOURSELF, and that difference is the point of the machine. A vent
    /// is a valve: the air leaves in a second and takes everything loose with it, which is why the panel will
    /// not do it to the room you are standing in. A pump is a motor that takes the best part of a minute, and
    /// nothing about standing next to a running motor is fatal. Owner, at the board: <i>"I can not pump down
    /// to vacuum in engineering?"</i> — and he could not, ever, because the board is IN engineering and the
    /// vent interlock was being applied to the pump as well.</para>
    ///
    /// <para>So the pump will run under your feet, and the clock is the warning. Leave before it finishes and
    /// you have emptied the room you were just in. Stay, and the vacuum finds you the same way it finds
    /// anything else — slowly, and with the hatch held shut behind you by the pressure you just removed.
    /// That is a decision with a timer on it, which is exactly what the rest of this panel is made of.</para>
    /// </summary>
    /// <remarks>Asked of the room WITHOUT the captain in it, rather than by rewriting the answer afterwards.
    /// The first cut did the latter and swallowed an open hatch along with the feet, because CaptainInside
    /// is reported before DoorOpen — a pump that would happily drain the whole ship through a doorway so
    /// long as you were standing in the room. Exempt the one objection, keep every other.</remarks>
    public static VentReadiness PumpReadiness(in Space space) =>
        Readiness(space with { CaptainInside = false });

    /// <summary>What the board says when you start a pump in the room you are standing in. It does not stop
    /// you and it does not nag twice — it tells you how long you have.</summary>
    public static string PumpUnderfootLine(string name, double seconds) =>
        $"The {name} pump spins up under your feet. {(int)seconds}s until the room is dead, and the hatch " +
        "will hold itself shut against the difference long before that. Be somewhere else.";

    /// <summary>The whole-ship order, read back. It names the count, and — if the captain is standing in one
    /// of them — it names that too, because an order that quietly excluded their own compartment would be a
    /// board deciding what they meant.</summary>
    public static string WholeShipOrderLine(int rooms, string? captainIn)
    {
        string head = rooms == 1
            ? "Every hatch is dogged and one compartment is on the pumps"
            : $"Every hatch is dogged and {rooms} compartments are on the pumps";
        string spine = ", and the corridor goes on them the moment the last one is sealed";
        string feet = captainIn is null
            ? ". Stay out of the rooms."
            : $". THAT INCLUDES {captainIn}, WHICH YOU ARE STANDING IN.";
        return head + spine + feet;
    }

    /// <summary>The line while the pump runs. Deliberately unglamorous: this is the patient road.</summary>
    public static string PumpRunningLine(string name, double secondsLeft) =>
        $"Pumping {name} down — {System.Math.Max(0, (int)secondsLeft)}s. The reserve is filling as the room " +
        "empties. Whatever is in there has time to notice.";

    public static string PumpDoneLine(string name) =>
        $"{name} reads hard vacuum, and it did not cost you a thing — the air is in the shuttle's tanks " +
        "instead of forty kilometres behind her. The pump winds down. The room is as dead as if you had " +
        "blown it, and you are one breath richer than if you had.";
}
