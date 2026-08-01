using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585 · THE HIVE — the secret lab as a facility, not a two-door apartment.
///
/// <para>Owner, 2026-08-01: <i>"I expect a large space to be discoverable with long tunnels. Maybe we could go
/// underground so that we don't need to go out of the border on normal level. Instead ... show what is on our
/// current 'floor' level. On surface we would only need a camouflaged elevator. I think there are a lot of
/// movie references to masked elevators to underground sites (The Hive in Resident Evil for example). I just
/// don't want the secret lab to be puny 2 door apartment, but look like it could facilitate a large operation
/// with serious funding. We can again use the locked doors to give the illusion of much larger space. And say
/// corridors that lead to somewhere far away where we dare not venture too far into."</i></para>
///
/// <para>What it replaces: <c>SecretLab</c> appended ONE room, 16 x 14 du, for a find the code itself bills as
/// "the veterans' once-a-career" payoff worth five thousand credits. His phrase for it was exact.</para>
///
/// <h3>Why down is the right answer, and not only thematically</h3>
/// <para><b>Each floor reuses the surface's own coordinate envelope.</b> That makes the "don't walk past the
/// border" problem disappear rather than be fought: a complex the size of the whole field costs no new space,
/// because it is not beside the field, it is under it. The renderer shows one floor; the deck-plan swap that
/// does it is the same machinery the ship ↔ haven ↔ surface switch already uses.</para>
///
/// <h3>The three calls</h3>
/// <para>The owner said "go forward" without answering the three open questions, so they are decided here and
/// written down loudly enough to be overruled in one line each:</para>
/// <list type="number">
/// <item><b>You find it by LOOKING.</b> The lift head is a real structure on the surface — a squat blockhouse
/// that reads like any other ruin until you are close enough to see its door, which is
/// <see cref="BodyPalette.Imported"/> violet. On a moon where every hatch is local stone, that is the one door
/// that was flown here, and it is the best possible use of the #592 language. The metal-detector probe still
/// works and still pings; it is no longer the only way, because one square in a 310 x 260 field is a needle in
/// a haystack.</item>
/// <item><b>Three floors down, and the bottom is not a bottom.</b> −1, −2, −3, and the deepest ends at a
/// sealed corridor mouth with a distance painted on it. The world continues past where you are allowed to
/// walk; that is the whole feeling he asked for.</item>
/// <item><b>−1 still holds pressure. Below that it does not.</b> This is the one that decides how the place
/// FEELS, so it gets the answer with a beat in it: the first floor is a refuge — the tank stops, the nerve
/// steadies, you relax — and everything below is dead, so depth is paid for in air and every stair down is a
/// decision about getting back up. A complex that is uniformly safe is a museum; one that is uniformly hostile
/// is a corridor shooter. The lie is what makes it frightening.</item>
/// </list>
///
/// <para>Canon holds absolutely: nothing down here explains what the Old Ones are (owner ruling 2026-07-30).
/// A facility may be enormous, expensive and obviously state-backed, and may never say what it was for.</para>
/// </summary>
public static class UndergroundComplex
{
    /// <summary>#585 · WHAT KIND OF PLACE THIS IS. Owner, extending the brief: <i>"feel free to upgrade the
    /// expanded section into proper literally underground lab space. We can have a lot of those in the sites,
    /// different clandestine sites in the spirit of world building."</i>
    ///
    /// <para>So this is not one rare lab any more — it is a CATEGORY. Clandestine sites are a thing that
    /// happens under moons, plural, and each kind is a different arm of the same unspeakable business. None
    /// of them ever explains the business; they only show you what it costs to run.</para></summary>
    public enum Kind
    {
        /// <summary>Dr Vantar's own — the original #409 find, now with a building around it.</summary>
        Laboratory,
        /// <summary>Where people were counted, graded and moved. The most bureaucratic and the worst.</summary>
        ProcessingDepot,
        /// <summary>Paper. Rooms and rooms of it, and somebody once thought that was the safe option.</summary>
        RecordsAnnex,
        /// <summary>A clinic with no name on the door and no register it appears in.</summary>
        BlackClinic,
        /// <summary>A transfer station: things came in, things went out, and the manifests do not match.</summary>
        TransitStation,
    }

    /// <summary>Which kind hides under this body. Seeded, so a moon has the site it has.</summary>
    public static Kind KindFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return (Kind)(DiceRule.Roll(DiceRule.Seed($"hive:kind:{bodyId}"), 5).Face - 1);
    }

    /// <summary>What the place calls itself, if it calls itself anything.</summary>
    public static string TitleOf(Kind kind) => kind switch
    {
        Kind.Laboratory => "▣ THE LABORATORY",
        Kind.ProcessingDepot => "▣ THE PROCESSING DEPOT",
        Kind.RecordsAnnex => "▣ THE RECORDS ANNEX",
        Kind.BlackClinic => "▣ THE CLINIC",
        _ => "▣ THE TRANSIT STATION",
    };

    /// <summary>The deepest floor the lift will go to. Negative levels are underground; 0 is the surface.</summary>
    public const int DeepestFloor = -3;

    /// <summary>Which floors still hold atmosphere. Owner's biggest open question, answered with a beat:
    /// the top floor lulls you and the rest costs you.</summary>
    public static bool HoldsPressure(int level) => level == -1;

    /// <summary>What the level is called on the lift panel and the plan header.</summary>
    public static string NameOf(int level) => level switch
    {
        0 => "SURFACE",
        -1 => "B1 · ADMINISTRATION",
        -2 => "B2 · LABORATORIES",
        -3 => "B3 · LONG STORAGE",
        _ => $"B{-level}",
    };

    /// <summary>The lift shaft's spot — the SAME (x, y) on every floor, so going down is legible and coming
    /// back up is never a search. Sits on the spine corridor at the field's heart.</summary>
    public static (double X, double Y) ShaftAt(in SurfaceLayout.Field field) =>
        (field.AnchorX + 40, (field.BottomY + field.LandingBandY) / 2.0);

    /// <summary>Half-width of the lift car, and of the shaft cut through every floor.</summary>
    public const double ShaftHalf = 3.0;

    /// <summary>Corridor half-width. Wide enough for the captain and an Old One to pass and for the eye to
    /// read it as a built passage rather than a gap between two walls.</summary>
    public const double CorridorHalf = 3.5;

    /// <summary>One floor, laid out. Walls and doorways in the same shapes <see cref="SurfaceLayout"/> speaks,
    /// so the client lays a floor exactly the way it lays a ground.</summary>
    public readonly record struct FloorPlan(
        int Level,
        string Name,
        bool Pressurised,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Doorway> Doorways,
        IReadOnlyList<LockedDoor> Locked,
        IReadOnlyList<SurfaceLayout.Landmark> Labels,
        IReadOnlyList<(double X, double Y)> RoomCentres);

    /// <summary>A door that never opens. The cheapest illusion of scale there is, and the owner asked for it
    /// by name — <i>"we can again use the locked doors to give the illusion of much larger space"</i>. Each
    /// carries the sign that was on it, which is what does the work: a corridor of shut doors with departments
    /// painted on them is a facility, and the same corridor with blank doors is a wall.</summary>
    public readonly record struct LockedDoor(double X1, double Y1, double X2, double Y2, string Sign);

    /// <summary>Build one floor. Pure and deterministic per (body, level): the same complex every visit, so a
    /// captain can learn it and come back for the door they could not open.</summary>
    public static FloorPlan Build(string bodyId, int level, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var walls = new List<SurfaceLayout.Wall>();
        var doorways = new List<SurfaceLayout.Doorway>();
        var locked = new List<LockedDoor>();
        var labels = new List<SurfaceLayout.Landmark>();
        var rooms = new List<(double X, double Y)>();

        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double shaftX, double shaftY) = ShaftAt(field);

        // ── THE SPINE. One long corridor across the whole field, which is the thing a surface can never
        //    have: a surface is a field, a facility is a PLAN, and corridors are the difference.
        walls.Add(new(left, shaftY + CorridorHalf, right, shaftY + CorridorHalf, true));
        walls.Add(new(left, shaftY - CorridorHalf, right, shaftY - CorridorHalf, true));
        walls.Add(new(left, shaftY - CorridorHalf, left, shaftY + CorridorHalf, true));
        labels.Add(new(shaftX - 26, shaftY + 1.4, NameOf(level)));

        // ── THE SHAFT. Same spot on every floor.
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf, shaftX - ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX + ShaftHalf, shaftY + CorridorHalf, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf + 5, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        labels.Add(new(shaftX, shaftY + CorridorHalf + 6.5, "🛗 LIFT"));

        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them. Seeded so each floor of each
        //    body is its own building, but structured so every floor still reads as the SAME facility.
        int ribs = 5;
        for (int i = 0; i < ribs; i++)
        {
            double t = (i + 0.5) / ribs;
            double x = Lerp(left + 16, right - 16, t);
            if (Math.Abs(x - shaftX) < ShaftHalf + CorridorHalf + 4)
            {
                continue;   // never run a rib through the lift
            }

            bool down = Frac(bodyId, $"hive:{level}:rib-dir:{i}") < 0.62;
            double far = down
                ? Math.Max(field.BottomY + margin, shaftY - 52)
                : Math.Min(field.LandingBandY - margin, shaftY + 52);

            double mouth = down ? shaftY - CorridorHalf : shaftY + CorridorHalf;
            walls.Add(new(x - CorridorHalf, mouth, x - CorridorHalf, far, false));
            walls.Add(new(x + CorridorHalf, mouth, x + CorridorHalf, far, false));

            // The rib's own far end: a door that never opens, with a distance on it. This is the corridor
            // that leads somewhere far away we dare not follow.
            if (Frac(bodyId, $"hive:{level}:rib-far:{i}") < 0.4)
            {
                double km = 0.8 + (Frac(bodyId, $"hive:{level}:rib-km:{i}") * 3.4);
                locked.Add(new(x - CorridorHalf, far, x + CorridorHalf, far,
                    $"⟶ SECTOR {7 + i} · {km:F1} km"));
            }
            else
            {
                walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, false));
            }

            AddRoomsAlong(walls, doorways, locked, rooms, bodyId, level, i, x, mouth, far, down);
        }

        return new FloorPlan(level, NameOf(level), HoldsPressure(level),
            walls, doorways, locked, labels, rooms);
    }

    /// <summary>Rooms down both sides of a rib. About half are locked — the owner's illusion of scale — and a
    /// locked one still gets its sign, because a door that says what is behind it and will not open is doing
    /// far more work than a blank one.</summary>
    private static void AddRoomsAlong(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked,
        List<(double X, double Y)> rooms, string bodyId, int level, int rib,
        double x, double mouth, double far, bool down)
    {
        const double roomW = 15.0, roomH = 12.0;
        double span = Math.Abs(far - mouth);
        int count = Math.Max(1, (int)(span / (roomH + 3)) - 1);

        for (int i = 0; i < count; i++)
        {
            double along = (i + 1) * (span / (count + 1));
            double cy = down ? mouth - along : mouth + along;

            for (int side = -1; side <= 1; side += 2)
            {
                string tag = $"hive:{level}:{rib}:{i}:{side}";
                double cx = x + (side * (CorridorHalf + (roomW / 2)));

                double x1 = cx - (roomW / 2), x2 = cx + (roomW / 2);
                double y1 = cy - (roomH / 2), y2 = cy + (roomH / 2);

                // Three walls and a corridor-facing face with a gap in it.
                walls.Add(new(x1, y1, x2, y1, false));
                walls.Add(new(x1, y2, x2, y2, false));
                walls.Add(new(side < 0 ? x1 : x2, y1, side < 0 ? x1 : x2, y2, false));

                double faceX = side < 0 ? x2 : x1;
                walls.Add(new(faceX, y1, faceX, cy - 2.0, false));
                walls.Add(new(faceX, cy + 2.0, faceX, y2, false));

                if (Frac(bodyId, tag + ":locked") < 0.5)
                {
                    locked.Add(new(faceX, cy - 2.0, faceX, cy + 2.0, SignFor(bodyId, tag)));
                }
                else
                {
                    doorways.Add(new SurfaceLayout.Doorway(faceX, cy - 2.0, faceX, cy + 2.0));
                    rooms.Add((cx, cy));
                }
            }
        }
    }

    /// <summary>What is painted on a door. Institutional, expensive, and never explanatory — the register of
    /// a place with serious funding and nothing to say for itself.</summary>
    public static string SignFor(string bodyId, string tag)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string[] signs = SignsFor(KindFor(bodyId));
        ulong seed = DiceRule.Seed($"hive-sign:{bodyId}:{tag}");
        return signs[(int)(seed % (ulong)signs.Length)];
    }

    /// <summary>Each kind's own door vocabulary. This is most of what makes one clandestine site feel unlike
    /// another, and it costs nothing but words: a corridor of doors reading INTAKE / GRADING / DISPATCH is a
    /// different building from one reading COLD STORE / ASSAY / PATTERN INTEGRITY, even laid on the same
    /// bones. Every list is institutional and none of it explains anything.</summary>
    public static string[] SignsFor(Kind kind) => kind switch
    {
        Kind.Laboratory =>
        [
            "CONTINUITY — AUTHORISED ONLY", "PATTERN INTEGRITY", "COLD STORE 2", "ASSAY",
            "SUBJECT PREP", "CALIBRATION", "POWER — LOCKED OUT", "LONG STORAGE — DO NOT OPEN",
        ],
        Kind.ProcessingDepot =>
        [
            "INTAKE", "GRADING", "DISPATCH", "HOLDING 3", "OCCUPATIONAL REVIEW",
            "SCHEDULING", "PAYROLL", "QUOTA OFFICE", "DO NOT ADMIT UNESCORTED",
        ],
        Kind.RecordsAnnex =>
        [
            "RECORDS — SEALED", "INDEX", "DUPLICATES", "RETENTION 40 YR", "MICROFORM",
            "DESTRUCTION QUEUE", "CLERKS", "AUDIT — NO ADMITTANCE",
        ],
        Kind.BlackClinic =>
        [
            "MEDICAL", "REHABILITATION", "RECOVERY 2", "THEATRE", "PHARMACY — LOCKED",
            "CONSENT FILES", "AFTERCARE", "MORTUARY",
        ],
        _ =>
        [
            "MANIFEST OFFICE", "BONDED HOLD", "CUSTOMS — SEALED", "CREW MUSTER",
            "LOADING 4", "TRANSIT REGISTER", "QUARANTINE", "OUTBOUND — AUTHORISED ONLY",
        ],
    };

    // ── WHAT YOU CARRY OUT ──────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "those sites should have good loot of stuff and information also... like dirt on potential
    // contacts ... the works."
    //
    // The second half is the interesting one and it is the reason these places belong in this game rather
    // than in a shooter. A crate of credits is a number going up. A FILE ON SOMEBODY is a thing you can spend
    // on a person — and this game already has the people: the bar contacts, the barkeeps, the harbourmasters'
    // seconds, the families in #588's kits. A records annex under a moon is where you learn that the man who
    // sets the docking fees at The Tilt has a name in a payroll he should not be in.
    //
    // It is left entirely open whether the captain USES it. That is the whole point of leverage.

    /// <summary>What a room in one of these places holds.</summary>
    public enum Haul
    {
        /// <summary>Stripped. Load-bearing, as everywhere else on this ground.</summary>
        Nothing,
        /// <summary>Hardware worth money — the "good loot of stuff".</summary>
        Equipment,
        /// <summary>Somebody's file. Leverage on a person the captain can actually go and meet.</summary>
        Dirt,
        /// <summary>Operational paper: a manifest, a route, a schedule. Points somewhere else.</summary>
        Records,
        /// <summary>A way through a door somewhere — a code, a card, a countersigned authority.</summary>
        Key,
    }

    /// <summary>What is in this room. Weighted so the place feels stripped but worth walking: about a third
    /// empty, and DIRT is the rarest thing in the building because it is the most valuable.</summary>
    public static Haul InRoom(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DiceRule.Roll(DiceRule.Seed($"hive:haul:{bodyId}:{level}:{roomIndex}"), 9).Face switch
        {
            1 or 2 or 3 => Haul.Nothing,
            4 or 5 => Haul.Equipment,
            6 or 7 => Haul.Records,
            8 => Haul.Key,
            _ => Haul.Dirt,
        };
    }

    /// <summary>Whose file it is, and what is in it. The subject is one of the standing roles a captain
    /// actually deals with, so the leverage has somewhere to be spent.</summary>
    public static string DirtOn(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        string[] subjects =
        [
            "the harbourmaster's second at The Tilt",
            "the man who sets the docking fees at Selene Gate",
            "the yard foreman at Highport Satellite Works",
            "the quiet one who drinks alone at The Rusty Roadstead",
            "the clerk who signs the bonded holds at Ringside Exchange",
            "the duty officer at The Deep",
        ];
        string[] findings =
        [
            "is in a payroll here they have no business being in, at a grade they were never qualified for",
            "signed for eleven consignments that the manifest office says never arrived",
            "was paid a settlement by an office that denies existing, and cashed it",
            "appears in the visitor book four times, always after midnight, always alone",
            "countersigned a transfer order for a person whose file is three rooms from here",
            "is listed as next of kin for somebody they have never once mentioned",
        ];

        ulong seed = DiceRule.Seed($"hive:dirt:{bodyId}:{level}:{roomIndex}");
        string who = subjects[(int)(seed % (ulong)subjects.Length)];
        string what = findings[(int)((seed / 7) % (ulong)findings.Length)];
        return $"🗃 A file, and it is not the file you were expecting: {who} {what}. " +
            "Nobody buried this here by accident. You can hold on to it, or you can never mention it. " +
            "Both of those are decisions.";
    }

    /// <summary>The line for the rest of the hauls.</summary>
    public static string HaulLine(Haul haul, string bodyId, int level, int roomIndex) => haul switch
    {
        Haul.Equipment =>
            "🧪 Bench hardware, crated and never unpacked — the good stuff, bought with somebody's grant and " +
            "abandoned with the lights on. It will fetch a great deal from people who will not ask.",
        Haul.Records =>
            "📋 Operational paper: rosters, routes, a shipping schedule with a column nobody has labelled. It " +
            "does not say what was moved. It says exactly how often, and to where.",
        Haul.Key =>
            "🎫 An authority card, countersigned twice and still active — this building never got the news " +
            "that its owners stopped paying. Something down here will open for this.",
        Haul.Dirt => DirtOn(bodyId, level, roomIndex),
        _ =>
            "🚪 Stripped to the fittings. Whoever cleared this room did it carefully and did it in a hurry, " +
            "which are two different things and both of them are here.",
    };

    /// <summary>What the lift says as it starts down. The one beat of scale before any of the plan is drawn.</summary>
    public const string DescendingLine =
        "🛗 The car takes a moment to decide you are allowed, and then it drops. It keeps dropping. Whatever " +
        "this was, nobody dug it in an afternoon and nobody paid for it out of pocket.";

    /// <summary>Said on stepping out on the top floor — the lie that makes the rest work.</summary>
    public const string PressurisedLine =
        "🫁 The doors part on warm air and standing lights. Your suit stops drawing and the readout holds. " +
        "Somewhere a fan is still turning, on somebody's account, decades after the last invoice.";

    /// <summary>And on every floor below it.</summary>
    public const string DeadAirLine =
        "🫁 The doors part on nothing. No pressure, no lights but yours, and the dust on the floor has not " +
        "been disturbed since it settled. Your tank starts counting again. From here down, depth costs air.";

    /// <summary>What a locked door says when the captain tries it. It never opens, and the game never pretends
    /// it might — a door that teases is a puzzle, and this is meant to be a WALL with a world behind it.</summary>
    public static string LockedLine(string sign) =>
        $"🔒 {sign}. The lock is not a lock you can argue with — it is a decision somebody made, and it is " +
        "still being enforced by a building whose owners stopped answering a long time ago.";

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double Frac(string bodyId, string tag) =>
        (DiceRule.Roll(DiceRule.Seed($"{bodyId}:{tag}"), 4096).Face - 1) / 4095.0;
}
