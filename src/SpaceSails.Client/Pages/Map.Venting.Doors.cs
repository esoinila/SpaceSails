using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Venting — the hatch itself, at arm's length: what comes out when the sealed one is undogged, which doorways are ten tonnes in a frame, the equalisation valve, the air put back, and the one person who walks out on their own legs.
public sealed partial class Map
{
    /// <summary>Rooms whose sealed door has already been opened once — so what was in there comes out ONCE,
    /// not every time a hatch swings.</summary>
    private readonly HashSet<string> _released = new(System.StringComparer.Ordinal);

    /// <summary>
    /// UNDOGGING A HATCH OPENS IT BOTH WAYS. Owner: <i>"and once unlocked the door starts to open also for
    /// the reevers :-D"</i>
    ///
    /// <para>Which is the entire reason the crew dogged it. A sealed room is a decision with teeth: the
    /// operating log says something in there is warm and moving and will never say what, so the captain
    /// chooses between never knowing and finding out — and finding out is not free. Whatever was shut in
    /// comes out into the corridor, aware, between the away team and nothing at all.</para>
    ///
    /// <para>It fires from the BOARD as readily as from the door, which is worse and correct: throw that
    /// switch from aft in ENGINEERING and you have opened a door you are not standing at.</para>
    /// </summary>
    private void ReleaseWhatWasSealedIn(string name)
    {
        if (_wreck is null
            || !_ventSpaces.TryGetValue(name, out HullVenting.Space s)
            || !s.Infested
            || s.Vented
            || !_released.Add(name))
        {
            return;
        }

        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);

        int came = 1 + DiceRule.Roll(
            DiceRule.Seed("sealed-count", (long)_wreck.Value.Id.GetHashCode(System.StringComparison.Ordinal),
                          name.GetHashCode(System.StringComparison.Ordinal)), 2).Face - 1;
        for (int i = 0; i < came && _reevers.Count < ReeverEngineCeiling; i++)
        {
            _reevers.Add(new Reever
            {
                X = ((x0 + x1) / 2.0) + (i * 2.0) - 1.0,
                Y = top ? -6.0 : 6.0,
                Facing = 0,
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 7UL,
                EverSeen = true,
                LastSeenX = _avatarX,
                LastSeenY = _avatarY,
            });
        }

        // #528 · THE THIRD PLATE IN THIS ROOM'S SET, and the one that was missing. The sealed door is the
        // whole decision the vacuum mechanic exists to make interesting, and throwing it was a pulse line —
        // beside a before-card and an after-card that have both had paintings for weeks. What the picture
        // shows is the INSIDE face of the door, and nothing about what worked at it.
        //
        // #736 · …and the line that says WHICH hatch and that it opens both ways rides the plate. The press
        // that throws this door is on the valve board, so before this the answer went to a pulse behind two
        // layers of blur — the board's and then the card's.
        //
        // #664 · The compartment is the SUBJECT, which is how the stamp goes on saying "🕷 DEEP HOLD — IT
        // OPENS BOTH WAYS" with the two halves still joined in NestPlates rather than here. COOLED rather
        // than once-per-room: throwing three doors on one sweep is one fright, not three, and the next hull
        // is a fresh one. NOT DEFERRABLE — the loop above this comment has already put the pack on the deck,
        // so CaptainIsInDanger() is true at the raise and holding the card would mean explaining the open
        // hatch after whatever came out of it has been dealt with.
        RaiseStoryBeat(StoryBeats.Beat.SealedDoorReleased, name,
            outcome: $"🕷 The {name} hatch comes off its dogs — and it opens BOTH ways. Whatever the last " +
                "crew shut in there has been waiting on the other side of it, and it does not need a second " +
                "invitation.");
        BoardLog($"🕷 Opened the sealed {name} — {came} came out.");
        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit, "you opened the door they sealed");
        RendererInterop.PlayCue("alarm");
    }

    /// <summary>Every compartment whose doorway is currently ten tonnes of atmosphere in a frame. The deck
    /// walls this set, so a room you blew is a room you cannot walk into.</summary>
    private HashSet<string> HeldDoors()
    {
        var held = new HashSet<string>(System.StringComparer.Ordinal);
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                held.Add(name);
            }
        }
        return held;
    }

    /// <summary>Every doorway the captain cannot walk through: loaded by pressure, or dogged shut by hand.</summary>
    private HashSet<string> BlockedDoors()
    {
        var blocked = new HashSet<string>(System.StringComparer.Ordinal);
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (HullVenting.DoorwayBlocked(s, _spinePressurised))
            {
                blocked.Add(name);
            }
        }
        return blocked;
    }

    /// <summary>The compartment whose pressure door the captain is standing at, once the card is up.</summary>
    private string? _pressureDoor;

    /// <summary>Walk up to a door that will not move: the gauge, why, and the two roads through it. Which
    /// door is decided by where the captain is STANDING rather than by a label — the deck dispatches on
    /// console kind alone, and a name parsed back out of display text is a bug waiting for a rename.</summary>
    private void OpenPressureDoorCard()
    {
        string? nearest = null;
        double best = double.MaxValue;

        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s)
                || !HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                continue;
            }

            double dx = VentDoorX(x0, x1) - _avatarX;
            double dy = (top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight) - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
                nearest = name;
            }
        }

        if (nearest is null)
        {
            return;
        }

        _pressureDoor = nearest;
        RendererInterop.PlayCue("block");
    }

    private void ClosePressureDoorCard() => _pressureDoor = null;

    /// <summary>Crack the equalisation valve at the door itself — the thing every real pressure door has,
    /// and the reason this can never strand a captain. Free, and it costs the ship her air: whichever side
    /// still had an atmosphere does not afterwards.</summary>
    private void EqualiseAtDoor(string name)
    {
        if (_wreck is null || !_ventSpaces.TryGetValue(name, out HullVenting.Space s))
        {
            return;
        }

        // One volume, one pressure, one valve: the spine and every room standing OPEN to it empty together.
        // What survives is exactly what somebody dogged a hatch on.
        HullVenting.EqualiseResult r = HullVenting.EqualiseAt(
            name, [.. _ventSpaces.Values], _spinePressurised);

        foreach (HullVenting.Space updated in r.Spaces)
        {
            _ventSpaces[updated.Name] = updated;
        }
        bool spineHadAir = _spinePressurised;
        _spinePressurised = !r.SpineVented && _spinePressurised;
        if (spineHadAir && !_spinePressurised)
        {
            _spineVacuumSeconds = 0.0;
        }

        string extra = r.RoomsOpened > 0
            ? $" {r.RoomsOpened} compartment{(r.RoomsOpened == 1 ? "" : "s")} went with it — every one that " +
              "had its door standing open."
            : "";

        // The warning on the valve was not decoration. Anyone behind an open door stops.
        if (r.SurvivorsLost > 0)
        {
            extra += " And in one of those rooms, something that had been alive for a very long time was not " +
                     "behind a dogged hatch.";
            ApplyNerveShock(HullVenting.VentedSurvivorNerveCost, "you emptied the ship with someone still in it");
            BoardLog($"☠ Equalising took {r.SurvivorsLost} survivor(s) — their doors were open.");
        }

        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);   // a ship equalising is not quiet
        ShowPulseMessage(HullVenting.EqualiseLine + extra);
        BoardLog($"🎚 Cracked the {name} valve — {(r.SpineVented ? "the ship is vacuum now" : "pressures even")}.");
        RendererInterop.PlayCue("alarm");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    /// <summary>Dog a hatch down by hand, at the hatch. The counterplay to the whole-ship valve: a sealed
    /// room keeps its air no matter what anyone does to the pressure two compartments away — and the
    /// infestation has not read the ship's manual, so it will never close one behind itself.</summary>
    private void ToggleSealAtHand(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s)
            || HullVenting.DoorHeldByPressure(s, _spinePressurised))
        {
            return;   // ten tonnes says no; the card offers the valve instead
        }

        bool sealing = !s.DoorShut;
        _ventSpaces[name] = s with { DoorShut = sealing };

        ShowPulseMessage(sealing ? HullVenting.SealLine(name) : HullVenting.UnsealLine(name));
        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, QuietEarshot);   // six dogs, by hand
        if (!sealing)
        {
            ReleaseWhatWasSealedIn(name);   // at the door, and it opens both ways
        }
        RendererInterop.PlayCue("board");
        RebuildWreckDeck();
    }

    /// <summary>Bring a compartment back to pressure — the other half of the board, and the half that
    /// tempts you into being impatient. Air comes back; nothing that went out with it does.</summary>
    private void RefillCompartment(string name)
    {
        if (_wreck is null)
        {
            return;
        }

        HullVenting.Space space = SpaceNow(name);
        HullVenting.RefillOutcome outcome = HullVenting.Refill(space, _refillCharges);
        _ventMessage = outcome.Line;

        if (!outcome.Filled)
        {
            RendererInterop.PlayCue("block");
            return;
        }

        _refillCharges--;
        _ventSpaces[name] = _ventSpaces[name] with { Vented = false, VacuumSeconds = 0.0 };

        // A reading taken on a vacuum compartment says nothing about a pressurised one — and if something
        // in there just took its first breath, the captain is entitled to go and ask the instrument again.
        _ventReads.Remove(name);

        if (outcome.SomethingSurvived)
        {
            ApplyNerveShock(HullVenting.RefilledTooSoonNerveCost, "you gave it the air back before it was finished");
            BoardLog($"🫁 Refilled {name} too early — it was not finished.");
        }
        else
        {
            BoardLog($"🫁 Brought {name} back to pressure ({_refillCharges} left).");
        }

        RendererInterop.PlayCue("board");
        RebuildWreckDeck();   // the door this room shares with the spine is now loaded — or no longer is
        RequestVaultSave();
    }

    /// <summary>Kill the pack standing in a compartment that just went to vacuum.</summary>
    private void ClearReeversIn(string name)
    {
        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);

        for (int i = _reevers.Count - 1; i >= 0; i--)
        {
            Reever r = _reevers[i];
            bool inX = r.X >= x0 && r.X <= x1;
            bool inY = top ? r.Y < -WreckLayout.SpineHalfHeight : r.Y > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                _reevers.RemoveAt(i);
            }
        }
    }

    /// <summary>Rescue whoever is behind a door — the reward for the careful road. Only reachable by
    /// OPENING the compartment rather than blowing it, which is the whole point.</summary>
    private void RescueSurvivor(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s) || !s.HoldsSurvivor || s.Vented)
        {
            return;
        }

        _ventSpaces[name] = s with { HoldsSurvivor = false };
        _survivorsRescued++;
        _credits += HullVenting.SurvivorRescueCr;

        // #736 · The board is still up — it IS the button that was pressed — so the answer is said on it,
        // beside every other thing this board reports (_ventMessage). The one line in the room that names a
        // PERSON and a payment was the one going to a pulse behind the board's own blur.
        SayItWhereTheyAreLooking(
            $"🧑‍🚀 Somebody is alive behind the {name} barricade — and has been for a very long time. " +
            $"They come out on their own legs. ({HullVenting.SurvivorRescueCr:N0} cr, and a witness.)");
        BoardLog($"🧑‍🚀 Rescued a survivor from {name} — {HullVenting.SurvivorRescueCr:N0} cr.");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
    }
}
