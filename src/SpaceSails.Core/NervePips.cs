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
public static partial class NervePips
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

        /// <summary>Standing inside the field of an archive node — the one warm thing on a dead ship.
        /// Beats while it holds, and NOTHING about it is announced: no prompt, no dialog, just the pip row
        /// ticking while the captain decides whether they are finished in this compartment. The salvage is
        /// deliberately in the same room, so the dose is the player's own to set.</summary>
        Archive,

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
        c is Cause.Close or Cause.Cornered or Cause.DigUnderThreat or Cause.Airlock or Cause.Archive;

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
        Cause.Close or Cause.Cornered or Cause.DigUnderThreat or Cause.Archive => BeatPips,
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

    /// <summary>The archive node's field, and the SLOWEST beat in the game on purpose. It has to be slow
    /// enough that crossing the compartment is genuinely free and working in it is genuinely not — if it
    /// bit as fast as being cornered, the room would just be a wall and there would be no decision in it.
    /// FLAGGED for the owner's tuning.</summary>
    public const double ArchiveBeatSeconds = 6.0;

    /// <summary>The beat length for a sustained cause.</summary>
    public static double BeatSeconds(Cause c) => c switch
    {
        Cause.Cornered => CorneredBeatSeconds,
        Cause.Archive => ArchiveBeatSeconds,
        Cause.DigUnderThreat => DigBeatSeconds,
        Cause.Airlock => AirlockBeatSeconds,
        _ => CloseBeatSeconds,
    };

    /// <summary>The words the player reads when this pip moves — the actual deliverable of #480. Present
    /// tense, in the house voice, no numbers (the pip itself is the number).
    ///
    /// <para><b>These are now read in TWO WORLDS.</b> Until #637 the gauge could only ever run on a moon —
    /// a derelict's whole deck satisfied the moon's "you are safely aboard" rule, so nothing inside a hull
    /// ever cost a pip and nothing inside a hull ever printed one of these lines. Fixing that made every
    /// label here reachable on a steel deck in vacuum, where there is no regolith, no tube and no sky. A
    /// label may only name a fixture BOTH places have.</para>
    ///
    /// <para><b>#867 · AND IN A THIRD, WHICH HAS AIR IN IT.</b> This overload is the vacuum reading, kept so
    /// every existing caller and every existing sentence is untouched byte for byte. Where the ground's own
    /// pressure is known, ask <see cref="Name(Cause, bool)"/> instead — see its remarks for why the airlock
    /// stopped closing behind a captain standing in a park.</para></summary>
    public static string Name(Cause c) => Name(c, groundHoldsPressure: false);

    /// <summary>#867 · THE SAME PIP, IN THE REGISTER THE GROUND EARNS.
    ///
    /// <para>Owner, 2026-08-13, strolling the B1 park: <i>"Our mood texts still assume the vacuum of the
    /// surface btw :-D"</i> — his ledger, on a lawn under grow-lights, repeating <i>"the airlock closes
    /// behind you +1"</i> with no airlock anywhere in the building.</para>
    ///
    /// <para>Nothing about the MECHANICS was wrong. <see cref="Cause.Airlock"/> is "you are somewhere safe
    /// and the safety is giving pips back", and a pressurised floor of the Hive has been exactly that since
    /// #585 — warm, lit, and with doors nothing outside can work. It is the SENTENCE that assumed the only
    /// way to be safe was to have cycled through a lock. Same mechanics, different sentence: the law the
    /// lift panel's rows learned in #802, which is that a line is a claim about the room and has to ask the
    /// room (<see cref="UndergroundComplex.HoldsPressure"/>) before it makes one.</para>
    ///
    /// <para><paramref name="groundHoldsPressure"/> is true only when the safety underfoot IS the floor's
    /// own air. Coming back up a tube, or in through a shelter's door, is still an airlock closing behind
    /// you, and still says so.</para></summary>
    public static string Name(Cause c, bool groundHoldsPressure) => c switch
    {
        Cause.Close => "it is right there",
        // Was "cornered — no lane to the tube". A wreck has no tube: the way home is the shuttle's own
        // lock, and #637 made this line reachable aboard one for the first time.
        Cause.Cornered => "cornered — no lane back",
        Cause.DigUnderThreat => "you cannot stop digging",
        Cause.Sighting => "something crests the tracker",
        Cause.Touch => "it laid hands on you",
        Cause.Monolith => "you have seen the monolith",
        Cause.Archive => "you have stood too long beside the thing in the hold",
        Cause.Airlock => groundHoldsPressure ? PressurisedEaseName : AirlockEaseName,
        Cause.Relief => "the hands remember how to be still",
        Cause.Shock => "something you will not be able to unsee",
        _ => "",
    };

    /// <summary>The ease's words when the safety is a DOOR you came through — her tube, a wreck's lock, a
    /// shelter's cycle. Unchanged since #480 and pinned byte for byte: #867 forked the sentence, it did not
    /// edit this one.</summary>
    public const string AirlockEaseName = "the airlock closes behind you";

    /// <summary>#867 · The ease's words when the safety is the FLOOR — a pressurised gallery of the Hive,
    /// which has been "safe" to this model since #585 and had no airlock in it the whole time.
    /// Owner-authored on the issue, per lore discipline.</summary>
    public const string PressurisedEaseName = "warm air and standing lights - the floor is holding";

    private static readonly Event[] NoEvents = [];
}
