namespace SpaceSails.Core;

/// <summary>
/// #480 · THE NERVE IS QUANTIZED — every point of sanity is a whole pip with a name on it.
///
/// <para>Owner, 2026-07-28, after playtesting the Reever exchange: <i>"I think the sanity events should be
/// quantized. Why and when it drops should be made clear to the player. Not this float stuff we have now.
/// What caused the sanity loss and what we did to regain it. Now it is vague and wishy-washy."</i></para>
///
/// <para><b>What was wrong.</b> Half the model was already discrete and legible (the monolith's lump, a
/// Reever's touch, the diminishing sighting jolts). The other half — the half that actually did the
/// killing — was a float: <c>ChaseDrainPerSecond</c> × a smooth <c>Dread(range)</c> ramp, ticking every
/// frame. The bar slid, nothing named the cause, and the rate depended on a distance the player could not
/// see. You lost nerve without learning anything, which is the opposite of what a fear meter is for.</para>
///
/// <para><b>The law now.</b> The track is <see cref="MaxPips"/> whole pips (owner's ruling: ten). Every
/// change is an integer number of pips and every change carries a <see cref="Cause"/> that can be spoken
/// out loud. Sustained pressure does not drain — it BEATS: while a stressor holds, a beat clock runs, and
/// each time it completes it spends exactly one pip and emits one named event. Proximity
/// (<see cref="NerveModel.Dread"/>) no longer scales an amount; it gates whether the beat runs at all,
/// which keeps #446's ruling ("they should not lower sanity unless they get REALLY close") while making
/// every loss a discrete, attributable thing.</para>
///
/// <para><b>Storage.</b> The canonical nerve stays a 0..<see cref="NerveModel.Max"/> double so saved
/// voyages and every already-tuned relief constant keep working — but it is ALWAYS a whole multiple of
/// <see cref="PipUnit"/>, enforced by construction here. <see cref="PipsOf"/> is therefore always a whole
/// number, and the gauge draws ten pips, not a bar.</para>
///
/// <para><b>Deterministic.</b> Pure arithmetic on the current nerve + the situation + the beat clock, so a
/// test pins an exact pip for an exact set of stressors over an exact dt. Determinism is law in Core.
/// Costs and beat lengths are FLAGGED for the owner's tuning.</para>
/// </summary>
public static class NervePips
{
    /// <summary>One pip, in the storage scale. The whole quantum: nothing may move the nerve by less.</summary>
    public const double PipUnit = NerveModel.Max / MaxPips;

    /// <summary>The pips a steady captain carries (owner's ruling, #480: ten — coarse enough that every
    /// single pip is an event you notice, fine enough that a close call still has texture).</summary>
    public const int MaxPips = 10;

    /// <summary>The floor: nerves shot.</summary>
    public const int MinPips = 0;

    /// <summary>How many whole pips a stored nerve value stands at.</summary>
    public static int PipsOf(double nerve) =>
        (int)System.Math.Round(NerveModel.Clamp(nerve) / PipUnit, System.MidpointRounding.AwayFromZero);

    /// <summary>The stored nerve value for a whole number of pips — the inverse of <see cref="PipsOf"/>.</summary>
    public static double FromPips(int pips) => System.Math.Clamp(pips, MinPips, MaxPips) * PipUnit;

    /// <summary>Snap any stored value onto the pip lattice. Belt-and-braces for values arriving from an
    /// older save written before #480, so a legacy 63.4 reads as a clean 6 pips and never drifts again.</summary>
    public static double Snap(double nerve) => FromPips(PipsOf(nerve));

    // ── The named causes ──────────────────────────────────────────────────────────────────────────────
    //
    // Every pip that moves names one of these. If a thing can change the nerve and is not in this list, it
    // is a bug: the whole point of #480 is that the player is never told "you feel worse" without being
    // told why.

    /// <summary>Why a pip moved. The name is the feature — see <see cref="Name"/> for the words the player
    /// actually reads, and <see cref="Cost"/> for what each one spends.</summary>
    public enum Cause
    {
        /// <summary>An Old One is close enough that the walk back is no longer a walk. Beats while it holds.</summary>
        Close,

        /// <summary>A net wedged between the captain and the tube mouth — the escape itself is contested.
        /// The sharpest routine pressure, so it beats fastest.</summary>
        Cornered,

        /// <summary>Shovel-work you cannot abandon with the tide inbound. Beats while it holds.</summary>
        DigUnderThreat,

        /// <summary>A fresh contact crests the tracker. Once per spell — habituation is free (#379: "seeing
        /// one reever after already seeing one more does not make you that much faster more nuts").</summary>
        Sighting,

        /// <summary>A Reever laid hands on you. A lump, never a beat, and habituation never dulls it.</summary>
        Touch,

        /// <summary>First sight of the monolith. Once in a captain's life.</summary>
        Monolith,

        /// <summary>Back aboard through the airlock — or flying, or docked. The ship is safety, and it gives
        /// pips back one beat at a time so the recovery is as legible as the loss.</summary>
        Airlock,

        /// <summary>A drink, a pill, a bunk, a glass shared across a table — the relief seam (#308/#321).
        /// The amount is the relief's own business (<see cref="NerveModel.RestoreAmount"/>); this names it.</summary>
        Relief,

        /// <summary>A one-off horror from somewhere other than the regolith: a charge fired on a falling
        /// mountain, what the secret lab reveals, an away-gig turning bad, a cold breath through the hull.
        /// These name themselves via <see cref="Event.Label"/> rather than growing this enum per event.</summary>
        Shock,
    }

    /// <summary>Whether a cause is sustained pressure (spends a pip per beat while it holds) rather than a
    /// one-off lump. Only these consult the beat clock.</summary>
    public static bool IsSustained(Cause c) =>
        c is Cause.Close or Cause.Cornered or Cause.DigUnderThreat or Cause.Airlock;

    // ── Costs, in whole pips (FLAGGED for the owner's tuning) ─────────────────────────────────────────

    /// <summary>A hand on you: ONE pip, ONCE per encounter — and that is the ruling that closes #469.
    ///
    /// <para><b>Repeated strikes cost no more sanity</b> (owner, 2026-07-28: <i>"repeated strikes should
    /// not cost more of sanity … we already take medical hit from reever"</i>). This supersedes the
    /// Evening-wind #19 reading that habituation never dulls being grabbed. The reasoning is the one that
    /// set the pip at one: the BLOWS already charge for a mauling (#453's five-pip condition marker).
    /// Nerve is what being CAUGHT does to you — a novelty, spent the moment the first hand lands. While
    /// they keep hold of you the fear is already paid; getting clear re-arms it.</para>
    ///
    /// <para><b>Except when you are nearly gone</b> (same ruling: <i>"but first one yes and running low on
    /// health more"</i>). Below <see cref="LowHealthPips"/> blows remaining, every hand costs its pip again.
    /// Fear tracks MORTAL DANGER, not novelty: the fourth grab is routine when you can take five, and it is
    /// the end of the world when you can take one.</para>
    ///
    /// <para>The old cost was 12 of 100, on top of proximity dread running at full rate while they were on
    /// you. Nerve emptied around the third or fourth blow, so the overdraw death always claimed the kill
    /// and the five-blow condition marker (#453) was close to decorative: <i>"the nerve bar is the real
    /// health bar"</i>. At one pip a captain can absorb ten grabs' worth of fear, so the FIVE BLOWS land
    /// first and the marker finally decides. Two distinct deaths for two distinct failures: mauled (the
    /// blow pips) and broken (nerve — fled, the monolith, the deep, without a hand ever landing).</para>
    ///
    /// <para>Owner's ruling, 2026-07-28, choosing it over the more dramatic three-pip pricing precisely
    /// because it lets the health bar he asked for actually kill someone. FLAGGED for tuning.</para></summary>
    public const int TouchPips = 1;

    /// <summary>At or below this many blows remaining (#453's condition marker), the captain is close
    /// enough to death that every further hand costs its pip again — the once-per-encounter latch stops
    /// protecting you exactly when it would matter most. FLAGGED for the owner's tuning.</summary>
    public const int LowHealthPips = 2;

    /// <summary>First sight of the monolith: the biggest single fright in the game, and it only ever
    /// happens once in a captain's life, so it is allowed to dwarf a hand on you. Three of ten keeps it
    /// close to the old 24-of-100 while landing as a lump you feel. FLAGGED for tuning.</summary>
    public const int MonolithPips = 3;

    /// <summary>A fresh contact cresting the tracker — the first of a spell. Later contacts in the same
    /// spell are free, which is the whole-pip form of #379's diminishing series.</summary>
    public const int SightingPips = 1;

    /// <summary>Every sustained beat spends exactly one pip. The pressure differs in CADENCE, not in size —
    /// that is what makes "cornered" feel sharper than "it is right there" without a second number.</summary>
    public const int BeatPips = 1;

    /// <summary>The pips a named cause spends (positive = a loss). <see cref="Cause.Relief"/> returns 0
    /// because the relief seam prices its own restore; the ledger records what it actually gave.</summary>
    public static int Cost(Cause c) => c switch
    {
        Cause.Touch => TouchPips,
        Cause.Monolith => MonolithPips,
        Cause.Sighting => SightingPips,
        Cause.Close or Cause.Cornered or Cause.DigUnderThreat => BeatPips,
        Cause.Airlock => -BeatPips, // gives one back
        _ => 0,
    };

    // ── Beat cadences, in seconds (FLAGGED for the owner's tuning) ────────────────────────────────────

    /// <summary>How long one of them has to be close before it costs a pip.</summary>
    public const double CloseBeatSeconds = 3.0;

    /// <summary>Cornered beats fastest — the sharpest routine pressure in the game.</summary>
    public const double CorneredBeatSeconds = 2.0;

    /// <summary>Digging with the tide inbound.</summary>
    public const double DigBeatSeconds = 2.5;

    /// <summary>The airlock gives a pip back this often. Deliberately slower than the fastest loss, so
    /// running for the tube is a real decision and not a reset button.</summary>
    public const double AirlockBeatSeconds = 2.5;

    /// <summary>The beat length for a sustained cause.</summary>
    public static double BeatSeconds(Cause c) => c switch
    {
        Cause.Cornered => CorneredBeatSeconds,
        Cause.DigUnderThreat => DigBeatSeconds,
        Cause.Airlock => AirlockBeatSeconds,
        _ => CloseBeatSeconds,
    };

    /// <summary>The words the player reads when this pip moves — the actual deliverable of #480. Present
    /// tense, in the house voice, no numbers (the pip itself is the number).</summary>
    public static string Name(Cause c) => c switch
    {
        Cause.Close => "it is right there",
        Cause.Cornered => "cornered — no lane to the tube",
        Cause.DigUnderThreat => "you cannot stop digging",
        Cause.Sighting => "something crests the tracker",
        Cause.Touch => "it laid hands on you",
        Cause.Monolith => "you have seen the monolith",
        Cause.Airlock => "the airlock closes behind you",
        Cause.Relief => "the hands remember how to be still",
        Cause.Shock => "something you will not be able to unsee",
        _ => "",
    };

    /// <summary>One pip moving, with the reason attached. <paramref name="Delta"/> is signed: negative
    /// spends pips, positive gives them back. <paramref name="Label"/> is the words the player reads —
    /// normally <see cref="Name"/> of the cause, but a one-off horror (a charge fired on a falling
    /// mountain, what the lab reveals) names itself, so the enum never has to grow a member per event.
    /// This is what the flash speaks and the ledger keeps.</summary>
    public readonly record struct Event(Cause Cause, int Delta, string Label)
    {
        /// <summary>An event that speaks its cause's standard words.</summary>
        public Event(Cause cause, int delta) : this(cause, delta, Name(cause))
        {
        }

        /// <summary>The line the player reads: the cause, and what it cost or gave.</summary>
        public string Line => $"{Label}  {(Delta >= 0 ? "+" : "−")}{System.Math.Abs(Delta)}";
    }

    // ── The beat clock ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long each sustained pressure has been building toward its next pip, plus the
    /// once-per-encounter touch latch. Carried by the client across frames and advanced by
    /// <see cref="Advance"/>; a cause that stops applying has its clock cleared, so pressure never banks
    /// silently between encounters.</summary>
    /// <param name="TouchSpent">Whether the shock of being caught has already been paid this encounter
    /// (owner: <i>"repeated strikes should not cost more of sanity"</i>). Re-arms the moment the captain
    /// is clear — safe up the tube, or with nothing close enough to frighten them.</param>
    public readonly record struct Beats(
        double Close, double Cornered, double Dig, double Airlock, bool TouchSpent = false)
    {
        /// <summary>Nothing building, and the next hand will be a fresh shock.</summary>
        public static Beats Fresh => new(0, 0, 0, 0, false);

        /// <summary>This cause's accumulated seconds.</summary>
        public double For(Cause c) => c switch
        {
            Cause.Cornered => Cornered,
            Cause.DigUnderThreat => Dig,
            Cause.Airlock => Airlock,
            _ => Close,
        };

        /// <summary>This clock with one cause's accumulated seconds replaced.</summary>
        public Beats With(Cause c, double seconds) => c switch
        {
            Cause.Cornered => this with { Cornered = seconds },
            Cause.DigUnderThreat => this with { Dig = seconds },
            Cause.Airlock => this with { Airlock = seconds },
            _ => this with { Close = seconds },
        };
    }

    /// <summary>Run one sustained cause's clock for <paramref name="dtSeconds"/>. Returns the carried-over
    /// seconds and how many WHOLE beats completed (normally 0 or 1; more only if a frame ran long, and
    /// they are all reported so a stutter never silently eats a pip). A cause that is not applying this
    /// frame resets to zero — pressure does not bank between encounters.</summary>
    public static (double Carried, int Beats) Tick(Cause c, double carried, bool applying, double dtSeconds)
    {
        if (!applying)
        {
            return (0.0, 0);
        }

        double period = BeatSeconds(c);
        double t = System.Math.Max(0.0, carried) + System.Math.Max(0.0, dtSeconds);
        if (period <= 0.0)
        {
            return (0.0, 0);
        }

        int beats = (int)System.Math.Floor(t / period);
        return (t - (beats * period), System.Math.Max(0, beats));
    }

    // ── The ledger ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many events the Captain desk keeps. Enough to reconstruct a bad minute, short enough
    /// to read at a glance — and the death card shows exactly this: <i>this is what broke you</i>.</summary>
    public const int LedgerDepth = 8;

    /// <summary>Fold new events onto a ledger, newest FIRST, bounded to <see cref="LedgerDepth"/>. Pure:
    /// returns a new list and never mutates the one handed in.</summary>
    public static IReadOnlyList<Event> Record(IReadOnlyList<Event> ledger, IReadOnlyList<Event> fresh)
    {
        if (fresh.Count == 0)
        {
            return ledger;
        }

        var next = new List<Event>(System.Math.Min(LedgerDepth, ledger.Count + fresh.Count));
        // Newest first: this frame's events (latest last in `fresh`, so walk it backwards), then history.
        for (int i = fresh.Count - 1; i >= 0 && next.Count < LedgerDepth; i--)
        {
            next.Add(fresh[i]);
        }
        for (int i = 0; i < ledger.Count && next.Count < LedgerDepth; i++)
        {
            next.Add(ledger[i]);
        }
        return next;
    }

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
    public readonly record struct Frame(
        bool OnExcursion,
        bool OnRegolith,
        bool SeesMonolith,
        NerveModel.Stressors Stressors,
        int FreshSightings,
        bool Touched,
        double DtSeconds,
        int HealthPipsLeft = int.MaxValue);

    /// <summary>The result of one frame: the new (still pip-aligned) nerve, the latched monolith flag, the
    /// advanced beat clock, every named event that fired THIS frame — in the order they happened, for the
    /// flash and the ledger — and whether the gauge should be visible at all.</summary>
    public readonly record struct Step(
        double Nerve,
        bool MonolithSeen,
        Beats Beats,
        IReadOnlyList<Event> Events,
        bool GaugeVisible);

    private static readonly Event[] NoEvents = [];

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

        bool safe = !f.OnExcursion || !f.OnRegolith;
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
        }
        else
        {
            for (int i = 0; i < airBeats; i++)
            {
                Fire(Cause.Airlock, -BeatPips); // gives one back, and says so
            }
        }

        var clock = new Beats(closeCarry, cornerCarry, digCarry, airCarry, touchSpent);
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
