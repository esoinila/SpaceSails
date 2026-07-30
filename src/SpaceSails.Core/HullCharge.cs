namespace SpaceSails.Core;

/// <summary>
/// #523 · THE CHARGE BOARD. Owner, standing in her engine room looking at the one console there that did
/// nothing interesting: <i>"I like you make a PR about how spaceships manage their electric charge build up and
/// make us a panel about it into the rear of the ship. Make an issue, research the topic and make a panel. Now
/// that desk is kind of lame on the ship, so let's add some kind of on automatic there."</i>
///
/// <para>The mechanic was already there and INVISIBLE, which is the whole problem. <see cref="ShipState.Charge"/>
/// is carried through the simulator; <see cref="PlasmaEnvironment.AmbientCharge"/> drives it; and
/// <see cref="SensorModel.ChargeGlowFactor"/> means a charged hull is SEEN FROM THREE TIMES AS FAR AWAY. A
/// pirate has been paying a stealth tax for the whole game with no instrument that mentions it, and the console
/// in the engine room was a joke generator sitting on top of it.</para>
///
/// <para><b>The physics, honestly borrowed.</b> Real spacecraft charging has three parts and each maps onto
/// something this game already does:</para>
/// <list type="number">
/// <item><b>Surface charging</b> — the hull floats to a potential against the ambient plasma. That is
/// <see cref="PlasmaEnvironment"/> exactly: a solar halo, saturated inside a stream.</item>
/// <item><b>Differential charging</b> — parts of a hull sit at different potentials, and that, not the absolute
/// number, is what arcs. Which is why the board reads BANDS rather than a single trusted figure.</item>
/// <item><b>Deep dielectric charging</b> — high-energy electrons bury themselves in insulators over hours, the
/// hull gauge says NOTHING about it, and then something arcs inside and takes a system with it. This game's own
/// law, applied to a second system: <b>the instrument cannot tell you what is in the room.</b></item>
/// </list>
///
/// <para>Mitigation is likewise real: passive grounding and leaky dielectrics, and an active <b>plasma
/// contactor</b> — a hollow cathode that sprays plasma and ties the hull to the ambient. The ISS carries two.
/// That is the owner's "on automatic", and it costs expellant, which is the one currency this game already
/// treats as precious.</para>
/// </summary>
public static class HullCharge
{
    /// <summary>
    /// Where the hull starts arcing across itself.
    ///
    /// <para>MOVED OUT OF THE CLIENT, where it lived as <c>ArcChargeThreshold = 0.9</c> beside the thunder cue.
    /// This repo has now shipped four bugs from geometry and rules kept as client literals — the nest two
    /// compartments from its own name, the valve board in a doorway, the dead bridge panel on the nav post, her
    /// valves on the charge dump. A number the sim acts on and a panel reports MUST be the same number, and only
    /// Core can be asked by a test.</para>
    /// </summary>
    public const double ArcThreshold = 0.9;

    /// <summary>How the board speaks about the hull. Bands rather than a figure, because differential charging
    /// is what actually arcs and a single number would be a promise the instrument cannot keep.</summary>
    public enum Band
    {
        /// <summary>Grounded and dull. Nothing to do.</summary>
        Quiet,

        /// <summary>Climbing. Nothing has happened yet, and something will.</summary>
        Rising,

        /// <summary>She is a lantern. This is the band that costs you the stealth you thought you had.</summary>
        Glowing,

        /// <summary>Arcing across herself. Loud, bright, and it is finding its way into things.</summary>
        Arcing,
    }

    /// <summary>Where a charge level sits. Tuned to the ONE consequence that already existed — being seen —
    /// with the arcing band at the sim's own threshold.</summary>
    public static Band BandOf(double charge) => charge switch
    {
        >= ArcThreshold => Band.Arcing,
        >= 0.55 => Band.Glowing,
        >= 0.2 => Band.Rising,
        _ => Band.Quiet,
    };

    /// <summary>The word on the board.</summary>
    public static string BandWord(Band band) => band switch
    {
        Band.Arcing => "ARCING",
        Band.Glowing => "GLOWING",
        Band.Rising => "RISING",
        _ => "QUIET",
    };

    // ── What it costs you, which is the thing nobody was told ─────────────────────────────────────────

    /// <summary>How much farther a hull at this charge is seen, as a multiple of a cold hull's range. Straight
    /// off <see cref="SensorModel.ChargeGlowFactor"/> — the same arithmetic the sensors already run, so the
    /// board cannot flatter the captain.</summary>
    public static double SeenFartherFactor(double charge) =>
        1.0 + (SensorModel.ChargeGlowFactor * System.Math.Clamp(charge, 0, 1));

    /// <summary>The sentence that has been owed to the player since the Electric Universe layer shipped.</summary>
    public static string VisibilityLine(double charge)
    {
        double factor = SeenFartherFactor(charge);
        return factor <= 1.05
            ? "🔭 Cold hull: you are seen at the range anybody is seen at, and no farther."
            : $"🔭 You are visible {factor:0.0}× farther than a cold hull. Everything with a telescope gets that " +
              "for free, and none of it has to be pointed at you first.";
    }

    /// <summary>What is driving it, said as a place rather than a number: a stream saturates the hull, the inner
    /// system is hot, the outer dark is cheap.</summary>
    public static string DrivenByLine(double ambient) => ambient switch
    {
        >= 0.95 => "⚡ You are inside a plasma stream. The hull will sit at the stream's own potential for as " +
                   "long as you ride it — that is the price of the free push.",
        >= 0.5 => "⚡ Inner system: the halo is dense here and she charges quickly.",
        >= 0.15 => "⚡ Middling space. She charges, slowly, the way a carpet charges a boot.",
        _ => "⚡ Out in the cold and the dark, where there is almost nothing to charge her.",
    };

    // ── The automatic: a plasma contactor ─────────────────────────────────────────────────────────────

    /// <summary>
    /// THE OWNER'S "ON AUTOMATIC". A hollow-cathode plasma contactor: it sprays a little plasma and ties the
    /// hull to the ambient, holding the potential down without anybody watching a gauge. The ISS carries two of
    /// them for exactly this.
    ///
    /// <para>What it holds her to. Not zero — a contactor clamps the hull NEAR the plasma's own potential, and
    /// leaving a little on her is also what keeps the trade honest: running the automatic is cheaper than being
    /// seen, never free.</para>
    /// </summary>
    public const double ContactorHoldsAt = 0.18;

    /// <summary>What it burns to do that, in thrust-pulses per minute of running. Expellant is expellant: the
    /// cathode spends the same reaction mass the engines do, which makes the automatic a real decision in the
    /// one currency this game has always been careful with.</summary>
    /// <remarks>FLAGGED for the owner's tuning. At this rate an hour of contactor costs 12 pulses of a 500-pulse
    /// tank — cheap enough to leave on for a run through the inner system, dear enough to think about on a long
    /// haul where every pulse is the difference between arriving and drifting.</remarks>
    public const double ContactorPulsesPerMinute = 0.2;

    /// <summary>The expellant a stretch of contactor running costs.</summary>
    public static double ContactorPulseCost(double seconds) =>
        ContactorPulsesPerMinute * (seconds / 60.0);

    /// <summary>What the board says when the automatic is holding her.</summary>
    public const string ContactorOnLine =
        "🜁 CONTACTOR RUNNING — the cathode holds her near the plasma's own potential. She stays dark, and it " +
        "costs expellant for as long as it runs.";

    /// <summary>…and when it is off, which is a choice and not a fault.</summary>
    public const string ContactorOffLine =
        "🜁 CONTACTOR OFF — nothing is holding her down. She will take the local potential and wear it, which is " +
        "free, quiet in the fuel ledger, and bright on everybody else's telescope.";

    /// <summary>And the refusal, because a cathode with nothing to spray is a cathode.</summary>
    public const string ContactorDryLine =
        "🜁 The contactor has nothing to spray. Expellant is expellant — buy reaction mass and it will hold her " +
        "again.";

    // ── The one it cannot see ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DEEP DIELECTRIC CHARGING, and why the board must not grow a number for it. High-energy electrons bury
    /// themselves in insulation over hours; the hull gauge reads fine the whole time; and then something arcs
    /// INSIDE and takes a system with it. The contactor does not prevent it — that is a real limitation of a
    /// real device, not a game balance — and neither does dumping the hull.
    ///
    /// <para>Which makes it the second system in this game to obey the law the wreck's life-sign sensor
    /// established: the instrument tells you something is happening, never what. What it gets is a HINT, at the
    /// moment the hint is worth something, and never a bar.</para>
    /// </summary>
    public const double InternalArcSeconds = 20.0 * 60.0;

    /// <summary>How much of the soak a stretch at this ambient is worth. Only the high-energy end of the
    /// environment buries anything, so cold space is free and a stream is expensive.</summary>
    public static double InternalSoakGain(double ambient, double seconds) =>
        ambient < 0.5 ? 0.0 : (ambient - 0.5) * 2.0 * seconds;

    /// <summary>The hint, given once it means something. Never a number, never a countdown.</summary>
    public const string InternalHintLine =
        "📻 The comms hiss carries a tick that is not traffic, and the boards behind the panel smell faintly of " +
        "ozone. Something under the insulation has been collecting a charge nobody can read from out here.";

    /// <summary>And the arc itself, when the buried charge finds its way out. Told as an event aboard rather than
    /// as a stat change, because the captain feels it in what stops working.</summary>
    public const string InternalArcLine =
        "⚡ A crack from behind a panel, and the smell of it fills the corridor. That was not the hull arcing to " +
        "space — it was a board arcing to itself, and it took something with it.";

    /// <summary>Whether the buried charge has reached the point where it will let go.</summary>
    public static bool InternalArcDue(double soakSeconds) => soakSeconds >= InternalArcSeconds;

    /// <summary>Where the hint starts being fair to give: most of the way, so it is a warning and not a
    /// prophecy.</summary>
    public static bool InternalHintDue(double soakSeconds) => soakSeconds >= InternalArcSeconds * 0.6;

    /// <summary>The line under the gauge that admits what the gauge cannot see. Always shown, because the
    /// admission is the honest part — the same reason the wreck's board never pretends to identify what is
    /// warm.</summary>
    public const string WhatTheGaugeCannotSee =
        "This reads the HULL. It cannot read what has soaked into the boards behind it, and neither can the " +
        "contactor — that is shielding and time, not a switch.";
}
