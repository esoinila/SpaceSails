using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE HULL — which room is the cause room and whether it is finished, the noise a boarding party
/// makes and the sleepers it rouses, the pack that wakes aboard, and the deck and hold line the wreck is
/// walked on.
///
/// <para>Split out of <c>Map.Wreck.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. The two fields that sat between them (<c>_anythingHasWokenAboard</c>,
/// <c>_wreckTrackerLive</c>) stay in the opening file with every other field of the family. Reading her,
/// the #533 anomaly and the decision stay there too — five guards read that file by path, and one of them
/// allows the #533 reading's type to be named only there.</para>
/// </summary>
public sealed partial class Map
{
    // ── The hull ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Put what got in aboard her — deep aft, around the nest, already aware.
    ///
    /// <para>Deliberate, not ambient: <c>StepTide</c> refuses to run on a wreck because there is no ground
    /// for anything to claw out of, and a hull that quietly filled with Old Ones would tell a different
    /// story than her evidence does. What is aboard a wreck gets put there ON PURPOSE — and this is the
    /// purpose. They know the airlock, which is the only way out, so the walk back is the encounter.</para></summary>
    /// <summary>Whether the compartment the cause's own evidence stands in has been opened to space AND left
    /// there long enough for the vacuum to finish. That is the condition for the after-picture: not "you
    /// pulled the handle", but "you waited."</summary>
    private bool CauseRoomIsFinished()
    {
        if (_wreck is not { } w)
        {
            return false;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(w.Cause);
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = at.X >= x0 && at.X <= x1;
            bool inY = top ? at.Y < -WreckLayout.SpineHalfHeight : at.Y > WreckLayout.SpineHalfHeight;
            if (inX && inY && _ventSpaces.TryGetValue(name, out HullVenting.Space s))
            {
                return s.Vented && HullVenting.SoakComplete(s);
            }
        }
        return false;
    }

    /// <summary>The middle of a named compartment — where a noise made in it comes from.</summary>
    private static (double X, double Y) RoomCentre(string name)
    {
        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);
        return ((x0 + x1) / 2.0, top ? -6.0 : 6.0);
    }

    /// <summary>Which compartment a point on the deck is in, or null for the spine.</summary>
    private static string? RoomAt(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = x >= x0 && x <= x1;
            bool inY = top ? y < -WreckLayout.SpineHalfHeight : y > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                return name;
            }
        }
        return null;
    }

    /// <summary>How long the hull stays quiet before the first one stirs. Long enough that the away team
    /// gets to read a station or two and start believing her. FLAGGED for tuning.</summary>
    private const double WreckPackFirstWakeMs = 35_000;

    /// <summary>And they come round in ones and twos, not as a pack — so the ship wakes AROUND the captain
    /// rather than at them.</summary>
    private const double WreckPackWakeStaggerMs = 22_000;

    /// <summary>How close the lamp has to be for a hibernating one to be drawn at all. Owner: they do not
    /// show on the map "unless they are within our observed vision space" — so what puts a dormant contact
    /// on screen is not the tracker (it is not moving) but the captain's own eyes.</summary>
    private const double DormantSightRange = 9.0;

    /// <summary>
    /// #488 · THE NOISE YOU MAKE. Owner: <i>"I like them to be unaware"</i> — and, crucially, <i>"that
    /// unawareness was a solution to them all charging the player at once… instead they come one or two at
    /// a time as they see you as you explore deeper."</i>
    ///
    /// <para>So this is built to protect that pacing, not to spend it. A pump, a blown compartment, a
    /// cracked valve, an undogged hatch, the overload PA — each one reaches the NEAREST contacts and no more
    /// than <see cref="NoiseRousesAtMost"/> of them. The rest sleep through it. Making a racket can never
    /// summon the ship; it can only ever cost you one or two, which is the same rate exploring deeper costs
    /// you, and that is the rate this hull is tuned to.</para>
    ///
    /// <para>And what they get is a PLACE, never a target — the idiom already in this codebase: "they know
    /// the DIG, not the captain; the ear hands out a place rather than a target." They walk to where the
    /// sound came from. If the captain has moved on, they arrive at nothing, and the tracker's ghost sits
    /// burning over an empty compartment.</para>
    /// </summary>
    private void MakeNoiseAboard(double x, double y, double earshot)
    {
        if (!OnWreck)
        {
            return;
        }

        // #538 · COLLEAGUES HEAR ABOUT IT. The sweep team is alerted FIRST and separately, because the two ears
        // are different instruments: the pack's is capped at the nearest two and does not care who else is aboard,
        // while three professionals share a channel — if one hears it, they all hear about it. And this must run
        // even when the pack is empty, which the old early return quietly prevented.
        AlertSweepersToNoise(x, y);

        if (_reevers.Count == 0)
        {
            return;
        }

        int roused = 0;
        foreach (Reever r in _reevers.OrderBy(r =>
                     ((r.X - x) * (r.X - x)) + ((r.Y - y) * (r.Y - y))))
        {
            if (roused >= NoiseRousesAtMost)
            {
                break;
            }

            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) > earshot * earshot)
            {
                continue;   // out of earshot: it sleeps through whatever you just did
            }

            if (r.Dormant)
            {
                // Loud enough to shorten forty years by a couple of minutes.
                r.WakeAtMs = System.Math.Min(r.WakeAtMs, (_lastTimestampMs ?? 0) + 1_500);
                roused++;
                continue;
            }

            // Awake and idle: it goes to look at the sound. Not at you — at the place.
            r.LastSeenX = x;
            r.LastSeenY = y;
            r.EverSeen = true;
            r.Idle = false;
            roused++;
        }
    }

    /// <summary>The hard cap that keeps noise from undoing what unawareness bought. Two, ever — the same
    /// rate walking deeper costs you.</summary>
    private const int NoiseRousesAtMost = 2;

    /// <summary>How far a working pump or a blown hatch carries through a dead hull.</summary>
    private const double LoudEarshot = 26.0;

    /// <summary>And a quieter piece of work — a hatch dogged by hand, a valve cracked.</summary>
    private const double QuietEarshot = 13.0;

    /// <summary>One comes round. The first is the one that matters: it is the moment the ship stops being a
    /// salvage job, and it arrives as an instrument appearing rather than an announcement.</summary>
    private void WakeTheSleeper(Reever r)
    {
        r.Dormant = false;
        r.VisibleOnMap = true;
        r.Idle = false;

        // WAKING IS NOT KNOWING. It comes round where it lay, with no idea anyone is aboard — it has to
        // find you the same way anything else does, by laying eyes on you. Handing it the captain's exact
        // position here is what made six of them converge from behind closed doors the moment they stirred.

        bool first = !_anythingHasWokenAboard;
        _anythingHasWokenAboard = true;

        ShowPulseMessage(first
            ? "🕷 Something that has not moved in forty years moves. It does not stretch and it does not " +
              "hurry — it simply stops being furniture, and starts being aimed at you."
            : "🕷 Another one comes round, somewhere behind you.");

        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit,
            first ? "she was not empty after all" : "another one is up");
        RendererInterop.PlayCue("alarm");
    }

    private void SpawnWreckPack(int count)
    {
        for (int i = 0; i < count && _reevers.Count < ReeverEngineCeiling; i++)
        {
            // Spread along the aft spine and the deep compartments, so they come up the corridor rather
            // than appearing on top of the away team.
            double x = -28 + (i * 5);
            double y = i % 2 == 0 ? 0 : (i % 4 == 1 ? -5 : 5);

            // NEVER SPAWN ONE INTO A ROOM IT CANNOT LEAVE. Owner, on the cheat now reaching any hull:
            // "will they be in closed room maybe :-D" — and on a vented wreck every hatch starts dogged, so
            // a pack placed in a compartment would sit there forever and the ship would read falsely quiet.
            // Anything whose room is shut at spawn goes into the spine instead.
            //
            // The captain trapping one LATER is a different thing entirely, and a legitimate play: dog the
            // hatch on a room with something in it and it cannot get out — but you cannot get in, and you
            // will never be certain what you shut the door on.
            if (y != 0 && RoomAt(x, y) is { } room
                && _ventSpaces.TryGetValue(room, out HullVenting.Space s)
                && HullVenting.DoorwayBlocked(s, _spinePressurised))
            {
                y = 0;
            }
            _reevers.Add(new Reever
            {
                X = x,
                Y = y,
                Facing = 0,
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,
                // They know where the door is — it is the only one — so they converge on the airlock, and
                // the captain is between them and it.
                // THEY DO NOT KNOW YOU ARE ABOARD. Owner, jumped by all six: "how did they detect me so
                // early so well from behind doors" — because they were spawned already hunting, with the
                // airlock as a known target. The rule this codebase already had is the right one: one that
                // has never laid eyes on the captain keeps its own ground. They have been asleep for forty
                // years; nobody has told them anything.
                EverSeen = false,

                // She reads EMPTY when you board her. Every one of them is folded down somewhere aft,
                // hibernating, and comes round on its own clock — staggered, so the hull wakes in ones and
                // twos rather than all at once. Not drawn, not moving, and therefore not on a motion
                // tracker: the first sign is the fan coming up on a ship you were told was dead.
                Dormant = true,
                VisibleOnMap = false,
                WakeAtMs = (_lastTimestampMs ?? 0)
                           + WreckPackFirstWakeMs
                           + (i * WreckPackWakeStaggerMs)
                           + (DiceRule.Roll(DiceRule.Seed("wake", (long)i, (long)(_surface?.ThreatSeed ?? 0UL)), 20).Face * 900.0),
            });
        }
    }

    /// <summary>Whether the shuttle the captain is about to load is crossing to a DERELICT rather than
    /// going down to a world. The boarding card was written for the regolith and said so in every line —
    /// burying a chest, three seeded landing sites with names like The Quiet Basin, a shovel and a pickaxe
    /// — all offered for a dead ship with a steel deck (owner, on the Cold Harvest: <i>"it shows me three
    /// options on where to board … those are good for Miranda etc moons but … you know"</i>).</summary>
    private bool BoardingAWreck =>
        _boardTarget is { } target
        && SiteRoute.IsHull(target.Body.Id);   // #653: a dead station is a hull too — no regolith card

    /// <summary>Rebuild the derelict's walkable interior — the ✔ marks and the vanished salvage console
    /// are state, so the deck is rebuilt whenever they change.</summary>
    private void RebuildWreckDeck()
    {
        if (_wreck is not { } w || !OnWreck)
        {
            return;
        }

        _deckPlan = WreckInterior.WreckDeck(
            w, _wreckExamined, _wreckSalvaged, 3 + ReeverEngineCeiling, FillSurfaceDroids,
            HeldDoors(), BlockedDoors(), _archiveAboard, _archivePurged,
            // #537 slice 3 · the plate, the hole and the captain behind it are STATE, so the deck is built
            // from them. The plate console used to be appended to the live plan, which meant any rebuild —
            // a dogged hatch, a pump, a purge — silently deleted the one find of the whole search.
            plate: _plateFound ? _hullVoid : null, voidOpen: _voidOpened, plateShut: _inTheVoid,
            // #535 · …and whether the key is still lying in her crew spaces. It is STATE for the plate's own
            // reason one comment up: taking it must not be undone by the next pump or dogged hatch.
            keyAboard: _keyAboard);
    }

    /// <summary>The wreck's own header line, and the loiter promise under it — the reason the away team is
    /// relaxed enough to read a manifest instead of running for the shuttle.</summary>
    private string WreckHoldLine()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(0, 0, 0);
        return q.Line;
    }
}
