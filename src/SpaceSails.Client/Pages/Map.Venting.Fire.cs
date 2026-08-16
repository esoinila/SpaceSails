using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Venting — #524's fire: what is alight, what it has already eaten off her price, where it spreads to next, and the two handles that put it out.
public sealed partial class Map
{
    // ── #524 · FIRE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long each burning compartment has been alight. Owner: <i>"if a room is on fire then pumping
    /// air out of it or venting it to space is a good way to stop the fire"</i>, and <i>"One of those could have
    /// the fire also"</i> — so the REACTOR CASCADE gets it, and her board stops being only a weapon.</summary>
    private readonly Dictionary<string, double> _burning = [];

    /// <summary>How long each burning compartment has been sealed behind its own hatch, for the slow answer.
    /// Reset whenever the hatch opens again, because a fire that gets its air back is not most of the way to
    /// out — it is back where it started.</summary>
    private readonly Dictionary<string, double> _sealedFor = [];

    /// <summary>Compartment-shares of her value the fire has eaten. Fractional while a room is burning, whole
    /// once it has finished with one — see <see cref="HullFire.ValueAfterFire"/>.</summary>
    private double _ruinedShares;

    /// <summary>Whether the captain has been told about the fire yet, so the first sighting lands once.</summary>
    private bool _fireAnnounced;

    /// <summary>What she is worth after what has burned. The salvage screen and the payout both read this, so a
    /// captain who dithered is paid what is left rather than what was there.</summary>
    private int SalvageValueNow(in Derelict.Wreck wreck) =>
        HullFire.ValueAfterFire(wreck.AssessedValueCr, WreckLayout.Compartments.Length, _ruinedShares);

    /// <summary>
    /// Run the fire. Three jobs, and every one of them reuses machinery that already existed: it SPREADS along
    /// the same door graph a pump uses, it DIES to the same two handles, and it EATS the thing the captain came
    /// for — which is a better reason to hurry than a health bar.
    /// </summary>
    private void AdvanceFire(double dtSeconds)
    {
        if (_wreck is null || _burning.Count == 0)
        {
            return;
        }

        // THE FIRST SIGHTING, once, through the story-card seam (#528) — it earns a card rather than a pulse
        // line because it is a scene, and because the three roads out of it need reading rather than glimpsing.
        if (!_fireAnnounced)
        {
            _fireAnnounced = true;
            BoardLog(HullFire.FoundLine);
            RaiseStoryBeat(StoryBeats.Beat.FireAboard, _wreck?.ShipName);
        }

        foreach (string room in _burning.Keys.ToList())
        {
            HullVenting.Space space = SpaceNow(room);

            // Sealed time is kept per compartment, and it RESETS the moment the hatch is open again.
            _sealedFor[room] = space.DoorShut ? _sealedFor.GetValueOrDefault(room) + dtSeconds : 0.0;

            if (HullFire.IsOut(space, _sealedFor[room]))
            {
                _burning.Remove(room);
                _sealedFor.Remove(room);
                BoardLog(space.Vented
                    ? HullFire.VentedOutLine(room)
                    : HullFire.SmotheredLine(room));
                RendererInterop.PlayCue("reveal");
                continue;
            }

            double before = _burning[room];
            double now = before + dtSeconds;
            _burning[room] = now;

            // WHAT IT IS EATING, priced per compartment so the board stays legible.
            _ruinedShares += HullFire.RuinFraction(now) - HullFire.RuinFraction(before);

            // AND WHERE IT IS GOING. The volume it shares air with is the volume it can reach — one graph for
            // the pump, the flood and the fire, so a hatch you dogged means the same thing to all three.
            if (now >= HullFire.SpreadSeconds)
            {
                foreach (string next in AtmosphereAt(room))
                {
                    if (next == HullVenting.SpineName || _burning.ContainsKey(next))
                    {
                        continue;
                    }
                    if (!HullFire.CanBurn(SpaceNow(next)))
                    {
                        continue;
                    }

                    _burning[next] = 0.0;
                    BoardLog(HullFire.SpreadLine(next));
                    RendererInterop.PlayCue("alarm");
                    break;   // one room at a time: a fire is a creep, not a flood
                }
            }
        }

        // STANDING IN IT. Not forbidden — told. A suit will take it for a while, and the captain should find
        // that out from the suit rather than from a refusal.
        if (CaptainCompartment() is { } here && _burning.ContainsKey(here))
        {
            ApplyNerveShock(HullFire.NervePerSecondInside * dtSeconds, "you are standing in a fire");
        }
    }

    /// <summary>Light her up, on the one cause whose fiction is already fire. Seeded off the wreck, so the same
    /// hull always burns in the same place.</summary>
    private void PrepareFire(in Derelict.Wreck wreck)
    {
        _burning.Clear();
        _sealedFor.Clear();
        _ruinedShares = 0;
        _fireAnnounced = false;

        if (wreck.Cause != Derelict.WreckCause.ReactorCascade)
        {
            return;
        }

        // Aft, where the reactor was, and never the machinery space itself — the board has to be reachable or
        // the fire is a cutscene. (Same reasoning that keeps the nest out of ENGINEERING.)
        var candidates = new List<string>();
        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments)
        {
            if ((x0 + x1) / 2 < 0 && name != HullVenting.ValveCompartment)
            {
                candidates.Add(name);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        ulong seed = DiceRule.Seed("fire", (long)wreck.Id.GetHashCode(StringComparison.Ordinal), 0);
        _burning[candidates[(int)(seed % (ulong)candidates.Count)]] = 0.0;
    }
}
