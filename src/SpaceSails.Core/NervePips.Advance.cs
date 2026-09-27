namespace SpaceSails.Core;

/// <summary>
/// #251 · THE ONE-PER-FRAME ADVANCE AND THE BANK — the frame and step records, <c>Advance</c>, and the
/// sub-pip shock and relief.
///
/// <para>Split out of <c>NervePips.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. <c>NoEvents</c>, the class's one initialised static, sat between the
/// two runs and stays in the opening file (#1163).</para>
/// </summary>
public static partial class NervePips
{
    // ── The one-per-frame advance ─────────────────────────────────────────────────────────────────────

    /// <summary>What the client reads off the live world this frame. Mirrors <see cref="NerveModel.Frame"/>
    /// but carries the beat clock, because in a quantized world the clock IS the state.</summary>
    /// <param name="OnExcursion">A surface excursion is live at all — the gauge shows only then.</param>
    /// <param name="OnRegolith">Actually out on the surface. False once back up through the airlock, where
    /// the ship's safety beats pips back instead.</param>
    /// <param name="SeesMonolith">The monolith is in sight this frame.</param>
    /// <param name="FreshSightings">Fresh contacts cresting the tracker this frame (#379's rise).</param>
    /// <param name="Touched">A Reever landed a hand this frame (already debounced by the client's catch cadence).</param>
    /// <param name="HealthPipsLeft">Blows the captain can still take (#453's condition marker). Drives the
    /// low-health exception to the once-per-encounter touch latch. Defaults to <see cref="int.MaxValue"/> —
    /// "not hurt / not known" — so a caller that has not been taught about health keeps the plain latch.</param>
    /// <param name="InArchiveField">THE ARCHIVE NODE'S DWELL (<c>docs/features/the-archive-node.md</c> §3).
    /// The captain is standing inside <see cref="ArchiveNode.FieldRadius"/> of the one warm thing on a dead
    /// ship. It is the only sustained pressure that is NOT a regolith pressure — a derelict's interior reads
    /// as "aboard" to every other rule in this file — so it is carried as its own flag rather than smuggled
    /// through <see cref="NerveModel.Stressors"/>.
    ///
    /// <para>While it applies the captain is NOT safe, which is the whole point: without that, the airlock's
    /// give-back beat would run at the same time as the archive's take beat and the two would very nearly
    /// cancel. The gauge would sit still while the ledger printed "you have stood too long beside the thing
    /// in the hold" — the sentence saying one thing and the sim doing another, which is the most expensive
    /// bug class this project has.</para></param>
    /// <param name="SafeGroundHoldsPressure">#867 · WHAT KIND OF SAFE. The gauge does not change — a safe
    /// captain gets pips back on the <see cref="Cause.Airlock"/> beat whichever way they got safe — but the
    /// LINE does: this is true only when the safety underfoot is a floor that holds its own air
    /// (<see cref="UndergroundComplex.HoldsPressure"/>), and false for every door-shaped safety in the game.
    /// Defaults to false, so a caller that has not been taught about the Hive keeps the airlock's own
    /// sentence exactly as it has always read. See <see cref="Name(Cause, bool)"/>.</param>
    public readonly record struct Frame(
        bool OnExcursion,
        bool OnRegolith,
        bool SeesMonolith,
        NerveModel.Stressors Stressors,
        int FreshSightings,
        bool Touched,
        double DtSeconds,
        int HealthPipsLeft = int.MaxValue,
        bool InArchiveField = false,
        bool SafeGroundHoldsPressure = false);

    /// <summary>The result of one frame: the new (still pip-aligned) nerve, the latched monolith flag, the
    /// advanced beat clock, every named event that fired THIS frame — in the order they happened, for the
    /// flash and the ledger — and whether the gauge should be visible at all.</summary>
    public readonly record struct Step(
        double Nerve,
        bool MonolithSeen,
        Beats Beats,
        IReadOnlyList<Event> Events,
        bool GaugeVisible);

    /// <summary>Advance the nerve one frame, in whole pips, naming everything.
    /// <list type="bullet">
    /// <item>Out on the regolith: each sustained pressure runs its beat and spends a pip when it completes;
    /// a fresh sighting costs its pip once per spell; a hand on you and the monolith land as lumps.</item>
    /// <item>Anywhere safe — up the tube mid-excursion, or off-planet entirely — the airlock beats pips
    /// back, one at a time, each one saying so.</item>
    /// </list>
    /// Sustained clocks for causes that are NOT applying are cleared, so nothing banks between encounters.
    /// Pure: same nerve + same flag + same clock + same frame → same <see cref="Step"/>, every time.</summary>
    public static Step Advance(double nerve, bool monolithSeen, Beats beats, in Frame f)
    {
        double n = Snap(nerve);
        List<Event>? events = null;
        void Fire(Cause c, int pips, string? label = null)
        {
            if (pips == 0)
            {
                return;
            }
            double before = n;
            n = NerveModel.Clamp(n - (pips * PipUnit));
            int moved = PipsOf(before) - PipsOf(n);
            if (moved != 0)
            {
                (events ??= []).Add(new Event(c, -moved, label ?? Name(c)));
            }
        }

        // THE DWELL IS NOT A REGOLITH PRESSURE. A derelict's interior is "aboard" to every other rule here,
        // so standing beside an archive node would otherwise be scored as SAFE and hand pips BACK. It does
        // not merely add a cost — it takes safety away, which is what makes the gauge agree with the line.
        bool archive = f.OnExcursion && f.InArchiveField;
        bool safe = (!f.OnExcursion || !f.OnRegolith) && !archive;
        double dread = NerveModel.Dread(f.Stressors.NearestContactRange);

        // Proximity GATES the beat (it no longer scales an amount): beyond the dread range an Old One is
        // scenery and costs nothing at all, so the clock does not even run.
        bool close = !safe && f.Stressors.ChaseActive && dread > 0.0;
        bool cornered = !safe && f.Stressors.Cornered;
        bool digging = !safe && f.Stressors.Digging && dread > 0.0
                       && (f.Stressors.ChaseActive || f.Stressors.MovingContacts > 0);

        (double closeCarry, int closeBeats) = Tick(Cause.Close, beats.Close, close, f.DtSeconds);
        (double cornerCarry, int cornerBeats) = Tick(Cause.Cornered, beats.Cornered, cornered, f.DtSeconds);
        (double digCarry, int digBeats) = Tick(Cause.DigUnderThreat, beats.Dig, digging, f.DtSeconds);
        (double airCarry, int airBeats) = Tick(Cause.Airlock, beats.Airlock, safe, f.DtSeconds);
        (double archiveCarry, int archiveBeats) = Tick(Cause.Archive, beats.Archive, archive, f.DtSeconds);

        // The touch latch re-arms the moment the captain is CLEAR — safe up the tube, or with nothing near
        // enough to frighten them. Then the next ambush is a fresh shock again.
        bool touchSpent = beats.TouchSpent && !safe && dread > 0.0;

        if (!safe)
        {
            // Lumps first — a hand on you and the monolith are the loudest things that can happen in a
            // frame, and the ledger should read in the order the captain felt them.
            if (f.Touched)
            {
                // Owner's rule: the FIRST hand of an encounter costs its pip; further strikes cost no more
                // SANITY (the blow pips already charge for the mauling) — unless the captain is nearly
                // gone, when every hand is terror again.
                bool nearlyGone = f.HealthPipsLeft <= LowHealthPips;
                if (!touchSpent || nearlyGone)
                {
                    Fire(Cause.Touch, TouchPips,
                        nearlyGone ? "it has you and you are nearly gone" : Name(Cause.Touch));
                }
                touchSpent = true;
            }
            if (!monolithSeen && f.SeesMonolith)
            {
                monolithSeen = true;
                Fire(Cause.Monolith, MonolithPips);
            }
            if (f.FreshSightings > 0)
            {
                Fire(Cause.Sighting, SightingPips); // once per spell — habituation is free
            }

            for (int i = 0; i < closeBeats; i++)
            {
                Fire(Cause.Close, BeatPips);
            }
            for (int i = 0; i < cornerBeats; i++)
            {
                Fire(Cause.Cornered, BeatPips);
            }
            for (int i = 0; i < digBeats; i++)
            {
                Fire(Cause.DigUnderThreat, BeatPips);
            }
            // The slowest sustained beat in the game, and the only one you can walk out of by taking your
            // salvage and leaving. Crossing the compartment costs effectively nothing; working in it counts.
            for (int i = 0; i < archiveBeats; i++)
            {
                Fire(Cause.Archive, BeatPips);
            }
        }
        else
        {
            for (int i = 0; i < airBeats; i++)
            {
                // …and says so IN THE REGISTER THE GROUND EARNS (#867). The beat, the pip and the cause are
                // identical on both grounds; only the sentence asks where the captain is standing.
                Fire(Cause.Airlock, -BeatPips, Name(Cause.Airlock, f.SafeGroundHoldsPressure));
            }
        }

        var clock = new Beats(closeCarry, cornerCarry, digCarry, airCarry, touchSpent, archiveCarry);
        return new Step(n, monolithSeen, clock, (IReadOnlyList<Event>?)events ?? NoEvents, f.OnExcursion);
    }

    // ── Sub-pip pressure: the bank ────────────────────────────────────────────────────────────────────
    //
    // Not every horror in the game is pip-sized. The hull's cold shudder (HullShudder.ChillNerveTick) is a
    // prickle worth a fraction of a pip — "mostly it IS nothing; this is the rare time it isn't quite."
    // Under a whole-pip law there are only two dishonest ways to price that: round it UP and a prickle
    // suddenly hits as hard as a beat of being hunted, or round it DOWN and the feature quietly stops
    // existing.
    //
    // So small shocks BANK. Each one adds to a pressure carry; when the carry owes a whole pip, the pip is
    // spent and the event that tipped it over is the one that gets named. Nothing is lost, nothing is
    // inflated, and the player still only ever sees whole pips move with a reason attached.

    /// <summary>Add a one-off shock of <paramref name="rawAmount"/> (storage units, the same scale the old
    /// float lumps used) to the pressure bank and spend whatever whole pips it now owes.
    /// <paramref name="label"/> names it for the flash and the ledger. Returns the new nerve, the carried
    /// pressure to store, and the event if a pip actually moved — a prickle that only banks returns none,
    /// which is correct: nothing visible happened yet.</summary>
    public static (double Nerve, double Carry, Event? Event) ApplyShock(
        double nerve, double carry, double rawAmount, string label)
    {
        double banked = System.Math.Max(0.0, carry) + System.Math.Max(0.0, rawAmount);
        int pips = (int)System.Math.Floor(banked / PipUnit);
        double rest = banked - (pips * PipUnit);
        if (pips <= 0)
        {
            return (Snap(nerve), rest, null);
        }

        double n = Snap(nerve);
        double after = NerveModel.Clamp(n - (pips * PipUnit));
        int moved = PipsOf(n) - PipsOf(after);
        return moved == 0
            ? (after, rest, null)                       // already on the floor — nothing left to spend
            : (after, rest, new Event(Cause.Shock, -moved, label));
    }

    /// <summary>Apply a relief (a drink, a pill, a bunk, a shared glass) as whole pips, named. The relief
    /// seam still prices its own magnitude — this rounds that price onto the pip lattice so the gauge moves
    /// in the same units as everything else, and returns the event so the flash and the ledger can speak
    /// it. A relief too small to buy a whole pip returns no event and changes nothing, which is honest:
    /// "barely a flicker" should not silently move the gauge.</summary>
    public static (double Nerve, Event? Event) ApplyRelief(double nerve, double rawRestore)
    {
        double n = Snap(nerve);
        int pips = (int)System.Math.Floor(System.Math.Max(0.0, rawRestore) / PipUnit);
        if (pips <= 0)
        {
            return (n, null);
        }

        double after = NerveModel.Clamp(n + (pips * PipUnit));
        int moved = PipsOf(after) - PipsOf(n);
        return moved == 0 ? (after, null) : (after, new Event(Cause.Relief, moved));
    }
}
