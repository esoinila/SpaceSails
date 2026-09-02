using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1068 · <b>THE WORLD DECLINES POLITELY</b> — the second customer of <see cref="DisclosureClock"/>, and the
/// two of the watchers' three manifestation channels that are not people: <b>subtraction</b> and
/// <b>structured instrument failure</b>. The third channel is people who do not know why, and it is already
/// delivered by the burial (<see cref="Burial"/>): a works notice, a mason, a cheerful rag line.
///
/// <para><b>THE SCULLY LAW (#672, binding, owner-blessed 2026-09-01).</b> <i>"We may show wonders, but a
/// Scully must always be able to plausibly say no. Any single watcher act must have a mundane reading that a
/// reasonable person can hold. The moment an act is only explicable by the watchers, it is spent — and we
/// spend exactly one, at the end."</i> Neither act here is that one. A door that is locked today and was not
/// yesterday is the single most ordinary thing that happens in a working building; a telescope pass that
/// brings nothing back is the single most ordinary thing that happens to a telescope.</para>
///
/// <para><b>THE LAWS FROM #672, AND WHERE EACH IS KEPT:</b></para>
/// <list type="bullet">
/// <item><b>No stats.</b> This type publishes no number a captain can read: the register is a list of grounds
/// and the window each declined in, and the only things derived from it are WHICH floor and WHICH door.</item>
/// <item><b>No sensor return.</b> The instrument channel is the strongest form of this law available — the
/// sensor's FAILURE is the manifestation, so there is nothing for a sensor to return. Nothing is logged, no
/// fault is raised, no message is written; the pass simply does not land.</item>
/// <item><b>No art of THEM ever.</b> Nothing here is drawn. The door wears the plate the room already had and
/// the leaf is the building's own <see cref="UndergroundComplex.LockedDoor"/> idiom, drawn by the same code
/// that has drawn forty of them since #585.</item>
/// <item><b>No farmable trigger.</b> Nothing in any signature here is effort: not a visit count, not a die,
/// not rooms searched, not passes ordered. It is a fact about WHEN the captain went, read off the clock's own
/// register — law four of <see cref="DisclosureClock"/>, carried into its second customer.</item>
/// <item><b>No dialog explaining a declined door.</b> <b>This type publishes no prose at all</b> — no label,
/// no line, not one string — which is the strongest available form of that law and settles §8 for free, since
/// a type with no strings in it cannot contain the reserved word. Swept by reflection in
/// <c>TheWorldDeclinesPolitelyTests</c>, exactly as the clock's own guard sweeps the clock.</item>
/// </list>
///
/// <para><b>ONE REGISTER, TWO CHANNELS, ON PURPOSE.</b> A ground has declined or it has not, and both acts
/// are the same declining read in two rooms: the door is what the captain meets standing on it, the blank
/// pass is what he meets looking at it from a hundred million kilometres away. Two registers on one schedule
/// would be the mirrored-constant bug this ground keeps a table of, said about a fact instead of a number.
/// </para>
/// </summary>
public static class PoliteDecline
{
    // ── THE THRESHOLD, AND ITS REASON WRITTEN BESIDE IT ──────────────────────────────────────────────────
    //
    // DisclosureClock's own docblock says what a customer owes it: "every beat that reads it chooses its own
    // threshold and writes that threshold's reason down beside its own words." These are those words.

    /// <summary>#1068 · How many WHOLE world-side windows must have passed since the ground was opened before
    /// the world may decline on it. <b>One</b> — the burial's own number, and deliberately the same one:
    /// <b>the watchers act on the schedule the neighbours do.</b>
    ///
    /// <para>Never on the visit that opened the ground. A door that had stopped opening by the time the
    /// captain walked back up out of the seam he had just crossed would be an answer to what he had just
    /// done, arriving inside the hour, from something that was watching him do it — which is a sensor return
    /// by another name and therefore the one thing #672's instrument law forbids outright. A shift later it
    /// is a maintenance decision, and a maintenance decision is a thing a reasonable person can hold.</para>
    ///
    /// <para>Read off <see cref="DisclosureClock.WindowsSince(DisclosureClock.Opening, long)"/> and never
    /// re-derived — the window is the monolith's own, asked through the clock, so nothing here owns a second
    /// copy of a length this ground has paid for owning twice before.</para></summary>
    public const long WindowsBeforeDeclining = 1;

    /// <summary>#1068 · Is this ground due to decline — <b>at this moment, ignoring where the captain is
    /// standing</b>. One whole window, per <see cref="WindowsBeforeDeclining"/>.</summary>
    public static bool IsDue(DisclosureClock.Opening opening, long window) =>
        DisclosureClock.WindowsSince(opening, window) >= WindowsBeforeDeclining;

    // ── THE REGISTER, AND WHY IT KEEPS A WINDOW ──────────────────────────────────────────────────────────
    //
    // Burial's register is a bare list of ids, because a filled ground is filled and a record that kept WHY
    // would be a record with an opinion in it. This one keeps one number more, and it has to: the door is
    // CHOSEN, and a choice needs something to be stable against. The window the world declined in is that
    // something — it is already in the clock, it never moves once written, and it is the only fact about the
    // decline that is not a fact about THEM.
    //
    // The world declines ONCE and stays declined. A lock that came back off would be an event, and an event
    // is a fact about somebody deciding — which is the Scully law spent on a door.

    /// <summary>#1068 · One ground, declined, and the world-side window it declined in. Nothing else: not who,
    /// not which door (that is derived, below), not what the captain was doing at the time.</summary>
    /// <param name="BodyId">The site the world declined on.</param>
    /// <param name="Window">The world-side window it happened in (<see cref="DisclosureClock.WindowAt"/>).</param>
    public readonly record struct Decline(string BodyId, long Window);

    private static IReadOnlyList<Decline> _declined = [];

    /// <summary>#1068 · The grounds the world has declined on. Empty in every world where nobody has been
    /// past a seam long enough ago, which is almost every world.</summary>
    public static IReadOnlyList<Decline> Declined => _declined;

    /// <summary>#1068 · Install the register — the ONE writer, called by whoever owns the save (the client,
    /// on load and on every descent), exactly as <see cref="Burial.Install"/> is. Null and empty are the same
    /// answer: the world has declined nowhere.
    ///
    /// <para>Tests restore what they installed in a <c>finally</c>. Because the register only ever changes the
    /// answer for the ids IN it, a guard that installs a ground of its OWN cannot move any other guard's
    /// world — which is what makes an ambient safe here rather than merely convenient. <b>And the emphasis on
    /// "its own" is paid for:</b> xUnit runs test classes in parallel, and the first full run of this feature
    /// reddened two audits with nothing to do with it (<c>TheParkIsTheCentreOfTheBlockTests</c> and
    /// <c>TheRingIsWalkableTests</c>) because this feature's guards had declined on the found-band cheat site
    /// while those two were walking the same site's concourse. A guard that installs a SHIPPED id here is
    /// shutting a door under somebody else's floor.</para></summary>
    public static void Install(IReadOnlyList<Decline>? declined) => _declined = declined ?? [];

    /// <summary>#1068 · <b>THE EVENT.</b> Which of the grounds this captain has opened the world has declined
    /// on by now — folded into the register, and the register handed back <b>by reference</b> when there is
    /// nothing to add, so a caller can compare and only then ask for a save.
    ///
    /// <para><b>Two conditions, the burial's own two, for the burial's own reasons:</b> a whole window has
    /// passed since the opening (<see cref="WindowsBeforeDeclining"/>), and <b>the captain is not on that
    /// body</b>. The second is what makes the door a thing he FINDS rather than a thing that happens to him:
    /// a leaf that swung shut while he watched would be an act with a witness, and an act with a witness is a
    /// thing he could describe.</para>
    ///
    /// <para>Nothing here is effort, a die, or a visit count.</para></summary>
    /// <param name="register">The disclosure clock's register of opened grounds.</param>
    /// <param name="declined">What the world has declined on already.</param>
    /// <param name="standingOn">The body the captain is on right now, or null when he is on none.</param>
    /// <param name="simTime">Sim seconds — no clock is read in Core.</param>
    public static IReadOnlyList<Decline> Note(
        IReadOnlyList<DisclosureClock.Opening>? register,
        IReadOnlyList<Decline>? declined,
        string? standingOn,
        double simTime)
    {
        IReadOnlyList<Decline> had = declined ?? [];
        if (register is not { Count: > 0 })
        {
            return had;
        }

        long window = DisclosureClock.WindowAt(simTime);
        List<Decline>? next = null;
        foreach (DisclosureClock.Opening opening in register)
        {
            if (!IsDue(opening, window))
            {
                continue;
            }
            if (string.Equals(opening.BodyId, standingOn, StringComparison.Ordinal))
            {
                continue;   // not while he is standing on it
            }
            if (WindowOn(had, opening.BodyId) is not null
                || (next is not null && WindowOn(next, opening.BodyId) is not null))
            {
                continue;   // already declined, and a ground declines once
            }
            next ??= [.. had];
            next.Add(new Decline(opening.BodyId, window));
        }
        return next ?? had;
    }

    /// <summary>#1068 · Which window this ground declined in, or null where it has not — asked of a register
    /// the caller holds. Null and any number are different answers, exactly as
    /// <see cref="DisclosureClock.OpeningOf"/> has it: a ground that declined in window zero is not a ground
    /// that never declined.</summary>
    public static long? WindowOn(IReadOnlyList<Decline>? declined, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (Decline d in declined ?? [])
        {
            if (string.Equals(d.BodyId, bodyId, StringComparison.Ordinal))
            {
                return d.Window;
            }
        }
        return null;
    }

    /// <summary>#1068 · The same, asked of the register that is installed — the form the generator and the
    /// scanning desk use, because neither of them has any business being handed a save.</summary>
    public static long? WindowOn(string bodyId) => WindowOn(_declined, bodyId);

    /// <summary>#1068 · <b>Has the world declined on this ground?</b> False everywhere in a world where
    /// nobody has been past a seam long enough ago, which is almost every world.</summary>
    public static bool On(string bodyId) => WindowOn(bodyId) is not null;

    // ── CHANNEL 1 · SUBTRACTION — WHICH DOOR ─────────────────────────────────────────────────────────────
    //
    // ONE door, on ONE floor, of ONE site — and the floor is not chosen at all. It is the one floor of this
    // building that has a door to SPARE, which is the concourse round the park: every other floor is ribs of
    // two-way chambers, and a chamber that lost a leaf would be a room with one way out. See
    // UndergroundComplex.Decline.cs for the whole of that argument; the seed's only job is to pick one door
    // out of the list that floor hands it.

    /// <summary>#1068 · Which of this floor's candidate doors the world took, or null where this floor keeps
    /// none — which is every floor of every site the world has not declined on.
    ///
    /// <para><b>The candidate list is the caller's, because only the caller has one</b>, and <b>WHICH floor
    /// is the caller's too</b>. Both are questions about a building and the building is
    /// <see cref="UndergroundComplex.Build(string, int, in SurfaceLayout.Field)"/>'s own business; a seeded
    /// floor number chosen in this file would be an opinion about a topology this file cannot see, which is
    /// §13.15's second cause exactly. See <c>UndergroundComplex.Decline.cs</c> for what makes a door eligible
    /// and, much more to the point, what makes one ineligible.</para>
    ///
    /// <para>The pick is seeded on <b>(ground, floor, the window it declined in)</b> and on nothing else, so
    /// it is the same door on the second visit and the tenth. A captain who comes back a third time finds the
    /// same leaf in the same wall wearing the same plate, which is what a locked door IS — and what a die
    /// would not be.</para></summary>
    public static int? TakenDoor(string bodyId, int level, int candidates)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (candidates <= 0 || WindowOn(bodyId) is not { } window)
        {
            return null;
        }
        return DiceRule.Roll(DiceRule.Seed($"decline:door:{bodyId}:{level}", window), candidates).Face - 1;
    }

    // ── CHANNEL 2 · STRUCTURED INSTRUMENT FAILURE — THE PASS THAT DOES NOT LAND ──────────────────────────
    //
    // #672, in the owner-blessed doctrine's own words: "instruments fail in structured ways — the scope's
    // one-shot never completes on one contact only — THE SENSOR LAW SURVIVES BECAUSE THE SENSOR'S FAILURE
    // IS THE MANIFESTATION." That sentence is the whole design and it is also the whole implementation:
    // there is nothing to return, nothing to log, nothing to say. The captain orders the look, the bar
    // crosses the desk, the job leaves the queue the way a finished one-shot leaves the queue, and no fix
    // arrives. Every other patch of sky he points it at behaves exactly as it always has.
    //
    // A CAPTION WOULD BE A SENSOR RETURN. "Pass returned no data" is the instrument reporting on itself,
    // which is the one thing #672 says it may never do about this — the moment the desk names the failure,
    // the failure is a datum, and a datum about them is the Scully law spent. So the desk says nothing at
    // all, which is also, mundanely, what a telescope says when a slew was mistimed and the sky moved.
    //
    // ONLY THE ONE-SHOT. A standing custody pass is the ledger's own housekeeping and it keeps working:
    // this takes the CAPTAIN'S OWN LOOK and nothing else, so nothing on the desk degrades, no track is lost,
    // and there is no state anywhere that a player could watch tick.

    /// <summary>#1068 · <b>Does this finished pass land?</b> False for every pass in the game except a
    /// one-shot whose swept disc holds a ground the world has declined on — for which the answer is that the
    /// pass simply does not complete.
    ///
    /// <para>The disc is the task's OWN aim and radius, and containment is asked against the body's TRUE
    /// position at the completion instant: the exact predicate the reveal already uses for hidden bodies
    /// (<c>Map.Npc.OnAreaScanCovered</c>), so "the scope was pointed at that moon" means the same thing to
    /// both, by construction rather than by comment.</para>
    ///
    /// <para>A zero-radius pass (a custody pass, a sharpen-fix on a hull) can never satisfy this, which is
    /// the intended shape: what is declined is a GROUND, and the only order that is aimed at a ground is the
    /// one the body menu offers — scan its vicinity.</para></summary>
    public static bool BringsNothingBack(SensorTask task, ICelestialEphemeris ephemeris, double atTime)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(ephemeris);
        if (task.Recurring || task.AreaRadius <= 0)
        {
            return false;
        }

        IReadOnlyList<Decline> declined = _declined;   // read the reference once (Burial.IsFilled's lesson)
        if (declined.Count == 0)
        {
            return false;
        }

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (WindowOn(declined, body.Id) is null)
            {
                continue;
            }
            if ((ephemeris.Position(body.Id, atTime) - task.AreaCenter).Length <= task.AreaRadius)
            {
                return true;
            }
        }
        return false;
    }
}
