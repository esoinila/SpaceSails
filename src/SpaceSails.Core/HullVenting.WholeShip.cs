namespace SpaceSails.Core;

/// <summary>
/// #488 · ONE PRESS, THE WHOLE HULL — putting her back, sealing her up, and the equalise that opens a
/// held door by emptying everything else.
///
/// <para>The three ship-wide buttons on the damage-control board, and the prices that keep them from
/// being free: refilling every open room costs a charge a room, dogging every hatch cannot re-dog one
/// the pressure is holding, and equalising is the road that gets you through a pressure-locked door at
/// the cost of everything still breathing behind every other one.</para>
///
/// <para>Every line here is written for a panel rather than a log — the count, the price, and what it
/// cost, said in the board's own voice.</para>
/// </summary>
public static partial class HullVenting
{
    // ── Putting her back: the whole ship at once ──────────────────────────────────────────────────────

    /// <summary>
    /// FLOOD THE SHIP. Owner, standing in a hull he had pumped down end to end: <i>"we should be able to
    /// refill the whole ship since it is in vacuum without closing doors?"</i>
    ///
    /// <para>He is right, and it is the same physics the equalisation valve runs on, played backwards. A
    /// vented corridor and every compartment standing open to it are ONE VOLUME with ONE PRESSURE — there
    /// is no differential to leak across, so there is nothing to shut first. You are not filling eight
    /// rooms; you are filling a ship.</para>
    ///
    /// <para>And this is where the law that was written as "the corridor can never be refilled" turns out
    /// to have been a law about the RESERVE, not about corridors. It was true when the away team carried
    /// two charges and had no pump. Now: YOU CAN PUT BACK EXACTLY THE AIR YOU BANKED. Air you threw out of
    /// an equalisation valve is gone forever and no amount of standing at the board brings it home; air you
    /// took the slow road on is sitting in the shuttle's tanks waiting for you. The asymmetry the original
    /// rule was protecting survives intact, and it lands where it belongs — on the choice between the valve
    /// and the pump, rather than on a flat refusal.</para>
    ///
    /// <para>A DOGGED HATCH STAYS DEAD, which is the whole tactic. The flood follows open doorways, so a
    /// compartment you sealed before flooding stays at hard vacuum with whatever is soaking in it, and the
    /// rest of the ship comes back to pressure around it.</para>
    /// </summary>
    /// <param name="openVentedRooms">Compartments at vacuum whose hatches stand OPEN — the ones the flood
    /// reaches. Sealed rooms are not counted and are not filled.</param>
    public static int WholeShipRefillCost(int openVentedRooms) =>
        SpineRefillCharges + openVentedRooms;

    /// <summary>What the corridor alone costs to bring back. The same figure the pump pays out for it, so a
    /// captain who pumped the spine down can always afford to undo exactly that.</summary>
    public const int SpineRefillCharges = SpinePumpYieldsCharges;

    /// <summary>The flood, as it reads on the board.</summary>
    public static string WholeShipRefillLine(int rooms, int spent, int sealedLeftDead)
    {
        string body =
            $"You open the reserve wide and the whole hull comes back at once — the corridor and {rooms} " +
            $"compartment{(rooms == 1 ? "" : "s")} standing open to it, one volume, one pressure. {spent} " +
            "charge" + (spent == 1 ? "" : "s") + " gone out of the tanks and the gauges climb together.";
        return sealedLeftDead > 0
            ? body + $" {sealedLeftDead} dogged hatch{(sealedLeftDead == 1 ? "" : "es")} did not answer, and " +
                     "whatever is soaking behind them goes on soaking."
            : body;
    }

    /// <summary>Why the flood will not happen. The reserve is the only thing that ever says no.</summary>
    public static string WholeShipRefillRefusal(int cost, int have) =>
        have <= 0
            ? "The tanks are empty. You can only put back the air you PUMPED out of her — what went through " +
              "the equalisation valve went to space, and space does not give it back."
            : $"She needs {cost} charges to come back and you are carrying {have}. Pump another compartment " +
              "down, or fill her one room at a time.";

    /// <summary>Nothing to fill.</summary>
    public const string WholeShipAlreadyFullLine =
        "She has air in her from the transom to the lock. There is nothing here to fill.";

    // ── Sealing her up in one press ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// SEAL THE SHIP. Owner: <i>"lock all doors would be nice also :-D … for that heat of the moment feel
    /// … a kind of rescue thing to do to contain any leaks … and oxygen from going to feed the fire."</i>
    ///
    /// <para>That is damage control as it is actually practised, and it is a different act from anything
    /// else on this board: not a weapon, not a saving, a REFLEX. Every other switch here is a decision you
    /// take your time over. This one is the thing you hit on the way past because something is wrong and
    /// you do not yet know what — and it is right for exactly the reasons he gives. A hull broken open
    /// somewhere loses only the compartment it happened in; a fire gets the oxygen in one room and nothing
    /// more; and whatever is walking the corridor is suddenly walking a much shorter corridor.</para>
    ///
    /// <para>What it cannot do is move a hatch the pressure is holding. Those are the doors the ship has
    /// already decided about.</para>
    /// </summary>
    /// <summary>
    /// AND THE WAY BACK OUT. Owner, standing in a hull he had just pumped end to end: <i>"I want to unlock
    /// all the doors after the ship is in vacuum."</i>
    ///
    /// <para>Of course — because the pump SEALS her to work, and a captain who has just emptied every
    /// compartment is standing in a maze of eight dogged hatches they now have to undo one at a time to walk
    /// their own prize. And once she is uniformly at vacuum there is no differential anywhere, so every one
    /// of those doors is free: the ship is asking to be opened.</para>
    ///
    /// <para>It is also the move that makes the flood reach everything — one open volume takes the reserve
    /// all at once — so the pair of them is the whole cycle: seal to empty her, open to walk her, flood to
    /// bring her back.</para>
    /// </summary>
    public static string UnsealTheShipLine(int opened, int held) => opened switch
    {
        0 when held > 0 => "Not one of them will move. The pressure across them is holding every hatch " +
                           "where it is — even her out the rest of the way, or fill her, and try again.",
        0 => "Every hatch is already standing open.",
        _ => $"You walk the row and undog {opened} hatch{(opened == 1 ? "" : "es")}. Nothing resists — there " +
             "is no difference across any of them worth the name — and she opens up into one long volume " +
             "from the transom to the lock."
             + (held > 0 ? $" {held} stayed shut: there is still air behind them." : string.Empty),
    };

    public static string SealTheShipLine(int dogged, int held) => dogged switch
    {
        0 when held > 0 => "Every hatch is already dogged or held by the pressure across it. She is as shut as she gets.",
        0 => "Every hatch is already dogged.",
        _ => $"You put your hand down the row and {dogged} hatch{(dogged == 1 ? "" : "es")} bang shut along " +
             "the spine, one after another, forward to aft."
             + (held > 0
                 ? $" {held} would not move — the pressure across them is holding them where they are."
                 : " Whatever happens next happens in one compartment."),
    };

    /// <summary>Whether somebody can be walked off this ship alive. You cannot carry a person out through a
    /// vacuum corridor, so a captain who vents the hallway has written off every survivor aboard — which
    /// the valve warned them about before they cracked it.</summary>
    public static bool CanCarrySurvivorOut(bool spinePressurised) => spinePressurised;

    /// <summary>Why the rescue will not happen.</summary>
    public const string NoAirToCarryThemOut =
        "There is nothing to carry them out through. The corridor is vacuum from here to the airlock and " +
        "they have no suit — whatever you were going to do for them, you did it when you cracked that valve.";

    /// <summary>
    /// CRACK ONE VALVE, VENT THE SHIP. Owner, on finding the strategy himself: <i>"I kind of like that a
    /// lot since now just having a single vented space allows venting the ship from that pressure
    /// equalization valve, if the actual controls are too crowded by infestation."</i>
    ///
    /// <para>It works because it is only physics: an open door is not a boundary, it is a hole. The spine
    /// and every compartment standing open to it are ONE VOLUME, so equalising any of them against a vacuum
    /// compartment empties all of it at once. A captain who cannot fight their way aft to the valve board
    /// can still vent the whole hull from a single door — the long way round, for free, and giving up every
    /// breath aboard her to do it.</para>
    ///
    /// <para>What survives it is exactly what somebody shut a door on. That is the counterplay, and the
    /// reason a room-by-room seal is worth having: <b>the infestation has not read the ship's manual.</b>
    /// It does not close doors behind itself, so door discipline is a tool that only the captain holds.</para>
    /// </summary>
    public static EqualiseResult EqualiseAt(
        string doorName, IReadOnlyList<Space> spaces, bool spinePressurised)
    {
        System.ArgumentNullException.ThrowIfNull(spaces);

        var result = new List<Space>(spaces.Count);
        Space cracked = default;
        foreach (Space s in spaces)
        {
            if (string.Equals(s.Name, doorName, System.StringComparison.Ordinal))
            {
                cracked = s;
            }
        }

        // Air out here, vacuum in there: the corridor empties into it, and takes every room standing open
        // to the corridor with it. Sealed rooms keep what they have.
        if (cracked.Vented && spinePressurised)
        {
            int opened = 0;
            int lost = 0;
            foreach (Space s in spaces)
            {
                bool sharesTheVolume = !s.DoorShut && !s.Vented;
                if (sharesTheVolume)
                {
                    opened++;
                    lost += s.HoldsSurvivor ? 1 : 0;
                    result.Add(s with { Vented = true, VacuumSeconds = 0.0, HoldsSurvivor = false });
                }
                else
                {
                    result.Add(s);
                }
            }
            return new EqualiseResult(SpineVented: true, result, opened, lost);
        }

        // Vacuum out here, air in there: the room empties into the ship. Nothing else changes — the spine
        // had nothing to lose.
        int emptied = 0;
        foreach (Space s in spaces)
        {
            bool isTheOne = string.Equals(s.Name, doorName, System.StringComparison.Ordinal) && !s.Vented;
            if (isTheOne)
            {
                emptied += s.HoldsSurvivor ? 1 : 0;
                result.Add(s with { Vented = true, VacuumSeconds = 0.0, DoorShut = false, HoldsSurvivor = false });
            }
            else
            {
                result.Add(s);
            }
        }
        return new EqualiseResult(SpineVented: !spinePressurised, result, cracked.Vented ? 0 : 1, emptied);
    }

    /// <summary>A compartment whose door is dogged shut is a WALL, not a suggestion — you open it at the
    /// door. That is what makes sealing a room a real act: it keeps the captain out, it keeps the ship's
    /// air in, and it is the only thing that survives somebody cracking a valve two compartments away.</summary>
    public static bool DoorwayBlocked(in Space space, bool spinePressurised) =>
        space.DoorShut || DoorHeldByPressure(space, spinePressurised);

    /// <summary>The line for sealing a compartment by hand, at its own door. The counterplay to the
    /// whole-ship valve, and the reason it is worth walking to a door you could have thrown from the
    /// board.</summary>
    public static string SealLine(string name) =>
        $"You dog the {name} hatch down by hand, all six of them, and put your weight on the last one. " +
        "Whatever happens to the pressure in the rest of this ship now happens without it.";

    public static string UnsealLine(string name) =>
        $"You undog the {name} hatch. It swings when you push it — nothing is holding it but the hinges.";
}
