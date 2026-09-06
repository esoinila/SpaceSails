namespace SpaceSails.Core;

/// <summary>
/// #488 · PRESSURE LOCKS — a door with vacuum behind it does not open, and the gauge beside it is why.
///
/// <para>Owner, on walking straight into a compartment he had just blown: <i>"we need some kind of door
/// locked due to vacuum feature."</i> A hatch with an atmosphere on one side and nothing on the other
/// is held shut by several tonnes of air, and no handle in the game moves it. The way through is to
/// equalise — which is the expensive road, because it empties the rest of the ship to do it.</para>
/// </summary>
public static partial class HullVenting
{
    // ── Pressure locks: a door with vacuum behind it does not open ────────────────────────────────────

    /// <summary>
    /// THE DOOR IS HELD BY THE PRESSURE, NOT BY A LATCH. Owner, on walking straight into a compartment he
    /// had just blown: <i>"we need some kind of door locked due to vacuum feature … maybe a general gauge
    /// that says the door is shut due to pressure difference."</i>
    ///
    /// <para>He is right, and the physics is on his side: one atmosphere across a door is roughly ten
    /// tonnes trying to keep it closed. Nobody opens that by hand. So a compartment you have vented cannot
    /// be walked into — which is what finally makes <see cref="Refill"/> load-bearing rather than
    /// decorative, and turns venting the deep hold into a real trade: kill what is in there, or keep being
    /// able to get in there.</para>
    ///
    /// <para>It takes air on ONE side and vacuum on the other. A hull that has been open to space for forty
    /// years has no differential anywhere and every door on her swings freely — which is exactly right for
    /// <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/>, where the only door that fights you is the
    /// one room somebody kept air in.</para>
    /// </summary>
    public static bool DoorHeldByPressure(in Space space, bool spinePressurised) =>
        !space.Vented != spinePressurised;   // air on exactly one side of it

    /// <summary>The gauge on the door, 0…1 — how hard it is being held. One side against the other; there
    /// is no in-between, so this is 1 when there is a differential and 0 when there is not. Kept as a
    /// fraction rather than a bool because the readout is a DIAL and a dial that only ever reads full or
    /// empty is still a dial.</summary>
    public static double PressureGauge(in Space space, bool spinePressurised) =>
        DoorHeldByPressure(space, spinePressurised) ? 1.0 : 0.0;

    /// <summary>What the captain reads at a door that will not move.</summary>
    public static string PressureLockLine(string name, bool compartmentIsTheVacuumSide) =>
        compartmentIsTheVacuumSide
            ? $"The {name} door will not move. The gauge beside it is hard over: hard vacuum that side, air " +
              "this side, and about ten tonnes of atmosphere holding the door in its frame. It is not " +
              "locked. It is LOADED."
            : $"The {name} door will not move — the gauge reads the differential the wrong way round. There " +
              "is still air in there and nothing at all out here, and the door is being held shut from the " +
              "inside by the last breathable room on the ship.";

    /// <summary>Cracking the equalisation valve on the door itself — the thing every real pressure door
    /// has, and the reason this mechanic can never strand a captain. It costs nothing and it costs
    /// everything: the two volumes even out, which means THE AIR GOES. Whichever side still had an
    /// atmosphere does not have one afterwards.
    ///
    /// <para>So there are two roads into a room you blew, and they are priced differently: crack the valve
    /// and lose the ship's air for free, or spend a <see cref="RefillChargesPerBoarding">charge</see> off
    /// the shuttle and keep it.</para></summary>
    public const string EqualiseLine =
        "You crack the equalisation valve and step back. The gauge falls off its stop over about four " +
        "seconds, and the door comes off its frame the moment there is nothing left holding it. Both sides " +
        "read the same now — which is to say both sides read nothing.";

    /// <summary>What one cracked valve did to the whole ship.</summary>
    /// <param name="SpineVented">The corridor lost its air — which is what makes this a whole-ship move.</param>
    /// <param name="Spaces">Every compartment's new state, in the order they were handed in.</param>
    /// <param name="RoomsOpened">How many compartments went to vacuum along with the spine. These are the
    /// ones whose doors were standing OPEN: one volume, one pressure, one valve.</param>
    /// <param name="SurvivorsLost">People who were behind an OPEN door when the ship equalised. The warning
    /// on the valve says this will happen; the seal is how you stop it.</param>
    public readonly record struct EqualiseResult(
        bool SpineVented, IReadOnlyList<Space> Spaces, int RoomsOpened, int SurvivorsLost);

    /// <summary>
    /// THE CORRIDOR IS NOT REFILLABLE, AND THAT IS THE PRICE. Owner, immediately after venting the ship
    /// through one door: <i>"but can I now undo the venting of the hallway?"</i>
    ///
    /// <para>No. A compartment is a room; the spine is the length of the ship, and the away team's whole
    /// reserve is two compartments' worth. Cracking a valve is FREE and IRREVERSIBLE; refilling a room
    /// COSTS and is reversible. That asymmetry is the only thing that makes the choice at the door weigh
    /// anything — otherwise the equalisation valve is a shortcut with no downside and the valve board might
    /// as well not exist.</para>
    /// </summary>
    public const string SpineRefillRefusal =
        "You cannot fill a corridor from a shuttle. That is not a room — it is the length of the ship, and " +
        "everything you brought out here would not raise it off zero. The air you let out of her is gone.";
}
