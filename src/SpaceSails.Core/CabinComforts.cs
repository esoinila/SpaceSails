namespace SpaceSails.Core;

/// <summary>
/// The ship's cabin comforts — a sanity-restoring SLEEP in a bunk and a flavour-rich TOILET visit
/// (owner, live playtest 2026-07-19). Two rulings shape this file, quoted here in the house style:
///
/// <para><b>SLEEP.</b> "Let's have a sanity restoring sleep action in one of the cabins." This lands the
/// REST half of Evening-wind item #21 (docs/SaturdayPlan/EveningWindNotes.md) — the drink half shipped
/// in #339, the med-bay pill in #343. A good night's bunk is the biggest single restore in the game, but
/// honest: a short WELL-RESTED satiety window means you cannot lie down twice in a row to grind steady
/// hands — you have to actually be tired. The restore itself reuses the EXISTING #339 relief seam
/// (<see cref="NerveModel.DrinkRestore"/> with the new <see cref="NerveModel.DrinkKind.Sleep"/>), never a
/// parallel one.</para>
///
/// <para><b>TOILET.</b> "I tested the toilet :-D. There could be like randomized comments on visiting the
/// toilet … little variability. I think toilet visit could also restore sanity … usually with rare
/// exceptions of you're scared of what came out :-D" So a visit rolls a SEEDED flavour line (sim-time
/// salted, so each visit differs but is reproducible) from a Larry-adjacent pool — including one that
/// riffs on the LOCAL bar's house special when docked somewhere with a bar. It USUALLY restores a small
/// dab of nerve (smaller than a drink), with a RARE ~1-in-12 scare on the same seeded roll that COSTS a
/// dab instead, with an alarmed line ("you're scared of what came out").</para>
///
/// <para>Everything here is PURE and DETERMINISTIC — determinism is law in Core. Same nerve + same seed
/// (sim time) + same dock context always yields the same outcome, so a test pins every band. Magnitudes
/// are MODEST and FLAGGED for the owner's tuning.</para>
/// </summary>
public static class CabinComforts
{
    // ─────────────────────────────── SLEEP 🛏 ───────────────────────────────

    /// <summary>A night's bunk advances the sim clock this much — a MODEST fixed cost (the ship's night
    /// compresses to a sim-hour), not a cinematic. The client re-stamps the loitering ship at the new time
    /// and re-pins to any dock. FLAGGED for tuning.</summary>
    public const double SleepSimSeconds = 3600.0; // one sim-hour

    /// <summary>How long a bunk leaves you WELL-RESTED. Lie down again inside this window and you just toss
    /// and turn — the honest satiety that stops sleep being the steady-hands grind. Comfortably longer than
    /// <see cref="SleepSimSeconds"/>, so one sleep genuinely blocks the next for a while. FLAGGED.</summary>
    public const double WellRestedWindowSeconds = 6.0 * 3600.0; // six sim-hours

    /// <summary>Whether the captain is still well-rested from a bunk <paramref name="secondsSinceSleep"/>
    /// ago — true (and so sleep does nothing) inside the window. A negative gap (no sleep yet, or a clock
    /// that jumped backwards on a load) reads as NOT rested, so the first sleep of a session always lands.</summary>
    public static bool StillRested(double secondsSinceSleep) =>
        secondsSinceSleep >= 0.0 && secondsSinceSleep < WellRestedWindowSeconds;

    /// <summary>The outcome of a bunk: the nerve after, how much it rose, whether the captain was still
    /// rested (and so slept for nothing), and the in-voice line the pulse shows.</summary>
    public readonly record struct SleepResult(double Nerve, double Restored, bool WasRested, string Line);

    // The rested-and-refreshed lines, rotated by sim time (the same no-wall-clock idiom the rum lines use).
    private static readonly string[] RestedLines =
    [
        "You bunk down in the tidy cabin. A whole sim-hour of nothing at all — you wake steadier, the shakes long gone. 🛏",
        "The cabin's quiet swallows you whole. You surface from dreamless dark with your hands still and your head clear. 🛏",
        "You sleep like the dead, minus the drifting. Reveille finds a captain remade — nerves knit back together. 🛏",
        "Boots off, brain off. You come to with the ceiling right where you left it and the world a size smaller. 🛏",
    ];

    // Too-soon: the owner's honest satiety spoken. You were just here; there's nothing to sleep off yet.
    private const string NotTiredLine =
        "You lie down, stare at the bulkhead, and give it up — you're not tired, you were just here. Nothing to sleep off yet. 🛏";

    /// <summary>Take to the bunk. Inside the well-rested window it does nothing but say so; otherwise it
    /// returns a solid flat chunk of nerve through the SAME #339 relief seam the drink and the pill ride
    /// (<see cref="NerveModel.DrinkKind.Sleep"/>, tot 1 → un-diminished, never "drunk"). Pure: same nerve +
    /// same gap + same sim time → same result. The client owns advancing the clock and the last-slept stamp.</summary>
    public static SleepResult Sleep(double nerve, double secondsSinceSleep, double simTime)
    {
        if (StillRested(secondsSinceSleep))
        {
            return new SleepResult(NerveModel.Clamp(nerve), 0.0, WasRested: true, NotTiredLine);
        }

        double after = NerveModel.DrinkRestore(nerve, NerveModel.DrinkKind.Sleep, totNumber: 1);
        double restored = after - nerve;
        string line = RestedLines[(int)(((long)(simTime / 60) % RestedLines.Length + RestedLines.Length) % RestedLines.Length)];
        return new SleepResult(after, restored, WasRested: false, line);
    }

    // ─────────────────────────────── TOILET 🚽 ───────────────────────────────

    /// <summary>The small dab of nerve a normal visit returns — RELIEF, smaller than any drink (a lone
    /// galley tot is 10). The mundane comfort of a locked door and a clear conscience. FLAGGED.</summary>
    public const double ToiletReliefNerve = 3.0;

    /// <summary>The dab a SCARE visit costs instead — "you're scared of what came out". A cost, kept small
    /// and cheeky; on a par with the relief so a scare merely undoes a good visit, never wrecks you. FLAGGED.</summary>
    public const double ToiletScareNerve = 4.0;

    /// <summary>The rare-scare band on the seeded roll: 1-in-<see cref="ScareOneIn"/> visits go wrong. The
    /// owner's "rare exceptions". FLAGGED.</summary>
    public const int ScareOneIn = 12;

    /// <summary>The outcome of a toilet visit: the SIGNED nerve delta (positive relief, negative scare), the
    /// line to show, and whether this was the rare scare. The client applies the delta through
    /// <see cref="NerveModel.Clamp"/> — this is a tiny nerve nudge like the ship's own ease-off, not a drink,
    /// so it does not ride the drink seam.</summary>
    public readonly record struct ToiletVisit(double NerveDelta, string Line, bool Scare);

    // The usual pool — the little variability the owner asked for, in the loving Leisure-Suit-Larry key
    // (homage, never reproduction). Nine mundane comforts a spacer takes where they can get them.
    private static readonly string[] ReliefLines =
    [
        "You emerge lighter of conscience and cargo alike. The universe can wait; you feel almost human. 🚽",
        "A moment's peace behind a locked door — the only real privacy on a pirate ship. You'll take it.",
        "The vacuum flush roars like a de-orbit burn. You salute it, and step out a steadier spacer.",
        "Whatever that was, it's the recycler's problem now. You feel oddly accomplished.",
        "V-1K left a courtesy magazine: a 2387 thruster-parts catalogue. You read it cover to cover. Bliss. 🚽",
        "The recycler chirps a cheerful little tune. You elect not to wonder what it's cheerful about. Nerves settle.",
        "You catch your own reflection mid-flush and give yourself a small, weary nod. Still captain. Still standing.",
        "Ten quiet minutes and a clean slate. Piracy can resume at your leisure. 🚽",
        "Something unclenches — shoulders, jaw, the knot behind your eyes. Command is 90% plumbing, it turns out.",
    ];

    // The docked-only line: it riffs on the LOCAL bar's house special (Barkeep/DrinkMenu know the names).
    // Only ever offered when a special is in reach — otherwise the generic pool carries the visit.
    private const string SpecialRiffTemplate =
        "After what just went down that pipe, you are swearing off the {0} at {1} — permanently. You feel strangely purified. 🚽";

    // The rare scare — "you're scared of what came out", house voice, cheeky not gross.
    private static readonly string[] ScareLines =
    [
        "You look down. You immediately wish you hadn't — something in there looked back. 😨",
        "That is not a colour. That is a WARNING. You back away slowly, nerves jangling. 🚽",
        "The bowl gurgles a word, and it sounds like your name. You leave at speed and do NOT flush twice.",
        "Whatever left you has opinions and possibly a pulse. Even the recycler refuses it. So do you. 😨",
    ];

    /// <summary>Visit the head. One seeded roll (sim-time salted, so each visit differs yet replays exactly)
    /// decides BOTH the rare scare and which line you get. USUALLY a small relief; 1-in-<see cref="ScareOneIn"/>
    /// it's the scare that costs you instead. When docked at a bar, <paramref name="barSpecial"/> and
    /// <paramref name="barName"/> let one line swear you off the neighbourhood's own pour; pass null/blank
    /// (not docked, or a berth with no bar) and only the generic lines are drawn. Pure and deterministic.</summary>
    public static ToiletVisit VisitToilet(double simTime, string? barSpecial, string? barName)
    {
        // One roll per visit, salted by the sim second — a fresh face each time you press E, replayable.
        ulong seed = DiceRule.Seed("toilet-visit", (long)simTime);
        var rng = new DeterministicRandom(seed);

        // The rare band comes off the SAME seeded roll (owner's "~1-in-12 band on the same seeded roll").
        bool scare = rng.NextInt(0, ScareOneIn) == 0;
        if (scare)
        {
            string scareLine = ScareLines[rng.NextInt(0, ScareLines.Length)];
            return new ToiletVisit(-ToiletScareNerve, scareLine, Scare: true);
        }

        bool docked = !string.IsNullOrWhiteSpace(barSpecial) && !string.IsNullOrWhiteSpace(barName);

        // Docked, the special-riff line joins the pool as one extra candidate at the end; undocked it isn't
        // offered and the generic pool stands alone.
        int poolSize = ReliefLines.Length + (docked ? 1 : 0);
        int pick = rng.NextInt(0, poolSize);
        string line = pick < ReliefLines.Length
            ? ReliefLines[pick]
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, SpecialRiffTemplate, barSpecial, barName);

        return new ToiletVisit(ToiletReliefNerve, line, Scare: false);
    }
}
