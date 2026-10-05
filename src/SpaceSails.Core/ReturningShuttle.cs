using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #1074 beat 5 · <b>THE RETURNING SHUTTLE</b> — the direct-confrontation beat, staged once and late.
///
/// <para>#1074, owner, verbatim: <i>"The US lore about the hill with door that sometimes just disappears the
/// whole party who investigates it... just horse came back… people wonder if they are still alive somewhere
/// deep."</i></para>
///
/// <para><b>THE SHAPE.</b> Once per run, on a PRESERVED site (beat 2) the captain has already stood on, a
/// charter survey hull is on the traffic board bound for that ground. A window later its shuttle has come back
/// up to the parked hull on its own autopilot and the hull does not move again. The wire prints one line; the
/// captain may walk up and board her. What he finds is the airlock, the gun rack and a page — each told once.
/// <b>Nothing anywhere says whether anybody is alive, and nothing ever will.</b> The beat is the second
/// one-act beside #672's Reever crowd, so it is rare and late.</para>
///
/// <para><b>THE TIMING (owner ruling 2026-10-04, the PROPOSED default).</b> Eligible only after the captain has
/// (a) stood on a preserved site and (b) clipped at least one of beat 3's line items OR asked one of beat 4's
/// colleagues; it then fires on his next arrival in that site's system, ONCE per run, never in the first
/// <see cref="FirstSimDays"/> sim-days of a run. Not coupled to #1202; not a rumour row.</para>
///
/// <para><b>THE LAWS OF THE #672 DOCTRINE, kept here.</b> People are subtracted whole — never a body, never a
/// straggler; the enforcer is an OFFICE; every reason is real (a structural notice is a structural notice and
/// a seal is a real hazard); the reserved word is in no string. And <b>the site afterwards is byte-identical</b>
/// (the study never ends): nothing in this file reads, writes or reshapes a preserved-site register — the hull
/// is a separate object on the ground and the beat's own state is one small row of its own.</para>
///
/// <para>Canon lines are Fable's (the 2026-09-30 design draft on #1074), held verbatim. A line the draft does
/// not hold is a TODO on the PR and never a line here.</para>
/// </summary>
public static partial class ReturningShuttle
{
    // ── CANON, VERBATIM ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The board's tag on the hull, before its callsign.</summary>
    public const string BoardTag = "CHARTER SURVEY";

    /// <summary>The wire's one line, once (<see cref="NewsWire.NewsScope.SystemWire"/>).</summary>
    public const string WireLine =
        "A charter survey's shuttle came back to its ship on its own autopilot. The ship has not moved since. "
        + "The Authority has posted a structural notice at the site.";

    /// <summary>The airlock, told once.</summary>
    public const string AirlockLine = "The mooring line is cut clean. Not snapped: cut, by somebody with time.";

    /// <summary>The gun rack, told once.</summary>
    public const string RackLine = "The boarding gun is racked, charged, unfired. Nobody lost a fight.";

    /// <summary>The paper's id — the last page of the survey log.</summary>
    public const string LogPaperId = "survey-log-last";

    /// <summary>The paper's title as the field book reads it away from its room.</summary>
    public const string LogTitle = "A survey log, last page";

    /// <summary>The paper's body.</summary>
    public const string LogDocument =
        "Day four. The seal below the listed bottom opened to the key we were not given. "
        + "Descending with the full party at 06:10. Will report at the—";

    /// <summary>The field book's 📍 line, once.</summary>
    public const string FieldBookLine =
        "Six went down. The shuttle came up. Nothing on this hull says which of those is the strange part.";

    /// <summary>The field book's glyph for the line above.</summary>
    public const string FieldBookGlyph = "📍";

    // ── THE PLATES (chrome, not narrative: a noun on a fixture, in the surface deck's idiom) ─────────────
    // FLAGGED for the inspector: the draft authors no plate text. These are the minimum nouns the three
    // fixtures need to be pressable, in the "▤ SERVICE PANEL" register — they say what the fixture IS and
    // nothing about what happened.

    /// <summary>The airlock's plate.</summary>
    public const string AirlockPlate = "⚓ AIRLOCK";

    /// <summary>The rack's plate.</summary>
    public const string RackPlate = "▤ GUN RACK";

    /// <summary>The log desk's plate.</summary>
    public const string LogPlate = "▤ SURVEY LOG";

    // ── THE TIMING ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Owner 2026-10-04 · never in the first three sim-days of a run. FLAGGED: the count is the
    /// owner's own number.</summary>
    public const double FirstSimDays = 3;

    /// <summary>How many whole world windows pass between the hull's landing and the shuttle's return — the
    /// draft's "one world window later". FLAGGED tuning constant (a window is
    /// <see cref="DisclosureClock.WindowSeconds"/>).</summary>
    public const long WindowsToReturn = 1;

    /// <summary>The beat's whole persisted state, one small row. Null while the beat has not fired — the
    /// vault's null-while-empty law, so no old save's checksum moves.</summary>
    /// <param name="Body">The preserved ground the hull is parked on.</param>
    /// <param name="Window">The world window the hull landed in.</param>
    /// <param name="Wired">Whether the wire has printed its one line.</param>
    public sealed record Row(string Body, long Window, bool Wired = false);

    /// <summary>The phases the row is in at a given window.</summary>
    public enum Phase
    {
        /// <summary>The beat has not fired.</summary>
        None,

        /// <summary>The hull is down and on the board; the shuttle has not come back yet.</summary>
        Landed,

        /// <summary>The shuttle came back. The hull never moves again.</summary>
        Returned,
    }

    /// <summary>Which phase the row is in at <paramref name="window"/>.</summary>
    public static Phase PhaseOf(Row? row, long window) =>
        row is null ? Phase.None
        : window - row.Window >= WindowsToReturn ? Phase.Returned
        : Phase.Landed;

    /// <summary>Does the wire owe its one line — the shuttle is back and the line has not been printed?</summary>
    public static bool WireIsOwed(Row? row, long window) =>
        row is { Wired: false } && PhaseOf(row, window) == Phase.Returned;

    // ── THE ELIGIBILITY PREDICATE ────────────────────────────────────────────────────────────────────────

    /// <summary>Is the field book carrying a touch of beat 3 (a clipped line item) or beat 4 (a colleague
    /// asked)? The book is the register — there is no other record of either — so this reads the notes whose
    /// text is exactly one of beat 3's three line items (or their field-book bodies) or beat 4's two spoken
    /// lines.</summary>
    public static bool TouchedTheTrail(IReadOnlyList<FieldNote>? book)
    {
        if (book is null || book.Count == 0)
        {
            return false;
        }

        foreach (FieldNote note in book)
        {
            if (TrailLines.Contains(note.Text))
            {
                return true;
            }
        }
        return false;
    }

    private static HashSet<string> TrailLines => TrailLinesCache.Value;

    private static class TrailLinesCache
    {
        // A nested holder rather than a static field on the class: ReturningShuttle is partial, and a
        // static readonly in a later file would initialise in file order (the sixth bug class).
        public static readonly HashSet<string> Value = new(StringComparer.Ordinal)
        {
            MoneyTrail.TextOf(MoneyTrail.Item.Rail),
            MoneyTrail.TextOf(MoneyTrail.Item.Rota),
            MoneyTrail.TextOf(MoneyTrail.Item.Pour),
            PaperHeads.RailDocument,
            PaperHeads.RotaDocument,
            PaperHeads.PourDocument,
            CareerCost.ColleagueLine,
            CareerCost.MugLine,
        };
    }

    /// <summary>Which seen preserved site does this arrival belong to — the site itself, or the body it
    /// orbits (its system's berth). Null when the arrival is in no seen site's system.</summary>
    /// <param name="seen">The preserved grounds the captain has stood on, in the order he stood on them.</param>
    /// <param name="arrivalBodyId">Where he has just arrived.</param>
    /// <param name="parentOf">A body's parent id, or null.</param>
    public static string? SiteOfArrival(
        IReadOnlyList<string>? seen, string? arrivalBodyId, Func<string, string?> parentOf)
    {
        ArgumentNullException.ThrowIfNull(parentOf);
        if (seen is null || arrivalBodyId is null)
        {
            return null;
        }

        foreach (string site in seen)
        {
            if (string.Equals(site, arrivalBodyId, StringComparison.Ordinal)
                || string.Equals(parentOf(site), arrivalBodyId, StringComparison.Ordinal))
            {
                return site;
            }
        }
        return null;
    }

    /// <summary>
    /// <b>THE EVENT.</b> Does the beat fire on this arrival — and on which ground. Null is no.
    ///
    /// <para>Every arm is a fact about what the captain DID and when: he has stood on a preserved site
    /// (<paramref name="seen"/> non-empty, and the ground still in care); he has touched the trail
    /// (<see cref="TouchedTheTrail"/>); at least <see cref="FirstSimDays"/> sim-days have passed; the beat has
    /// not already fired this run; and this arrival is in that site's system. No die, no visit count.</para>
    /// </summary>
    public static string? Fires(
        Row? existing, IReadOnlyList<string>? seen, IReadOnlyList<FieldNote>? book,
        double simTime, string? arrivalBodyId, Func<string, string?> parentOf,
        Func<string, bool> inCare)
    {
        ArgumentNullException.ThrowIfNull(inCare);
        if (existing is not null || simTime < FirstSimDays * NewsWire.SecondsPerDay)
        {
            return null;
        }
        if (!TouchedTheTrail(book))
        {
            return null;
        }

        // Only the grounds still in care count as seen sites: the beat is staged on a PRESERVED site.
        IReadOnlyList<string> standing = seen is null ? [] : [.. seen.Where(inCare)];
        return SiteOfArrival(standing, arrivalBodyId, parentOf);
    }

    /// <summary>The hull's callsign — the board's tag, then an ordinary hauler name seeded on the ground.</summary>
    public static string CallsignFor(string bodyId) =>
        BoardTag + " " + TrafficSchedule.Callsigns[
            (int)(DiceRule.Seed($"charter-survey|{bodyId}") % (ulong)TrafficSchedule.Callsigns.Count)];

    /// <summary>The hull's id on the board.</summary>
    public static string ShipIdFor(string bodyId) => "charter-survey-" + bodyId;

    /// <summary>The hull as a ship in the world: a parked, empty-plan fixture on the ground's own orbit —
    /// <see cref="TheOldShip.Berthed"/>'s shape, which is what a ship that never moves again IS.</summary>
    public static NpcShip Parked(ICelestialEphemeris ephemeris, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(bodyId);

        CelestialBody body = ephemeris.Bodies.First(b => b.Id == bodyId);
        double radius = Math.Max(body.BodyRadius * 8, 2e6);
        double phase = DiceRule.Seed($"charter-survey|phase|{bodyId}") % 10000UL / 10000.0 * (Math.PI * 2);
        string id = ShipIdFor(bodyId);

        return new NpcShip(
            Id: id,
            Callsign: CallsignFor(bodyId),
            CargoClass: "Charter survey",
            OriginId: bodyId,
            DestinationId: bodyId,
            Personality: RoutePersonality.Economical,
            DepartureTime: 0,
            ActivationTime: 0,
            InitialState: TrafficSchedule.DepotState(id, bodyId, radius, phase, ephemeris, 0),
            Plan: new ManeuverPlan([]),
            EstimatedArrivalTime: double.MaxValue,
            CargoUnits: 0,
            ManeuverBudget: 0,
            IsPod: false,
            DepotBodyId: bodyId,
            DepotOrbitRadius: radius,
            DepotPhase: phase);
    }

    // ── WHAT IS ABOARD, AND WHAT EACH PRESS TELLS ──────────────────────────────────────────────────────────
    //
    // The field book is the latch (the house pattern: the wreck anomaly's, the clipped story's). A line is told
    // once because the book already holds it; nothing else records that it was told.

    /// <summary>Has the book already been handed this exact line?</summary>
    public static bool Told(IReadOnlyList<FieldNote>? book, string line) =>
        book is not null && book.Any(n => string.Equals(n.Text, line, StringComparison.Ordinal));

    /// <summary>The plates standing aboard right now. Nothing until the shuttle is back; then the airlock and
    /// the rack for good, and the log desk until its page has been taken (the 📍 line is filed in the same
    /// breath the page leaves the desk).</summary>
    public static IReadOnlyList<string> Fixtures(Row? row, long window, IReadOnlyList<FieldNote>? book)
    {
        if (PhaseOf(row, window) != Phase.Returned)
        {
            return [];
        }
        return Told(book, FieldBookLine)
            ? [AirlockPlate, RackPlate]
            : [AirlockPlate, RackPlate, LogPlate];
    }

    /// <summary>What a press at the airlock or the rack tells — the line the first time, null ever after. The
    /// log desk is not a line (it hands over a paper), so it tells nothing here.</summary>
    public static string? TellsOnPress(string plate, IReadOnlyList<FieldNote>? book)
    {
        string? line = plate == AirlockPlate ? AirlockLine : plate == RackPlate ? RackLine : null;
        return line is not null && !Told(book, line) ? line : null;
    }

    // ── THE PAPER ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Is this paper id the survey log's last page?</summary>
    public static bool IsTheLog(string? paperId) =>
        string.Equals(paperId, LogPaperId, StringComparison.Ordinal);

    /// <summary>Every player-facing string this beat publishes — the reserved-word sweep walks it.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return BoardTag;
        yield return WireLine;
        yield return AirlockLine;
        yield return RackLine;
        yield return LogTitle;
        yield return LogDocument;
        yield return FieldBookLine;
        yield return AirlockPlate;
        yield return RackPlate;
        yield return LogPlate;
    }
}
